using System;
using System.Collections.Generic;

/// <summary>
/// Checks WHERE the player put the stress in a word, from the loudness of
/// their voice over time.
///
/// WHY TEXT IS NOT ENOUGH
///
/// The device recognizer only ever hands back letters. "Tayô" (stand up) and
/// "táyo" (we) are both written t-a-y-o, so PronunciationChecker - which works
/// on letters - can never tell them apart, and the game used to pass a child
/// who said "we" when it asked for "stand up". The difference between the two
/// is not in the letters at all. It is in the sound: in Tagalog the stressed
/// syllable is held longer and said louder, so in táyo the voice sits on the
/// first syllable and in tayô it pushes on to the last.
///
/// WHAT IT LISTENS TO
///
/// The speech plugin gives the game one piece of the audio itself: the voice
/// level, reported many times a second while the recognizer listens
/// (ISpeechToTextListener.OnVoiceLevelChanged). VoiceCommand keeps every
/// reading of one listening session, and this class turns that loudness curve
/// into a single number:
///
///   the loudness centroid - how far through the word, from 0 (start) to 1
///   (end), the weight of the voice sits.
///
/// Holding a syllable longer and saying it louder both pull the centroid
/// toward it, which is exactly what stress does. A final-stressed word sits
/// later than the same word stressed on its first syllable.
///
/// HOW IT DECIDES
///
/// Every word has its own natural centroid (its consonants and vowels shape the
/// curve too), so one cut-off for every word would be wrong. Instead each
/// checked word is held against two measurements of Teacher Rachel's own
/// recording of it, both kept in StressReferences:
///
///   reference  word_tayo.wav as she recorded it - tayô, stressed right;
///   moved      the same recording with its stress moved to the other
///              syllable (that syllable held longer and louder, the other
///              shortened and softened) - táyo, the other word.
///
/// The cut-off sits halfway between the two. An answer on Rachel's side of it
/// is the word that was asked for; on the far side it is the other word.
///
/// /// The comparison only uses where the voice sits RELATIVE to the rest of the
/// same utterance, so it does not care how loud the child is, how close the
/// phone is, or how often the device reports a level.
///
/// WHAT IT CANNOT DO
///
/// The plugin passes on loudness only, not pitch, and a child does not say a
/// word the way an adult narrator does. So this is a real check of the stress,
/// but not a perfect one: the attempt log records the measured centroid and the
/// cut-off on every checked attempt, so the tolerance can be tuned from real
/// playtests the same way WordVariants is.
/// </summary>
public static class StressAnalyzer
{
    /// <summary>The outcome of one stress check.</summary>
    public enum Verdict
    {
        /// <summary>The stress is where the word needs it.</summary>
        Matches,
        /// <summary>The stress is on the wrong syllable - the other word was said.</summary>
        WrongStress,
        /// <summary>There was not enough of a voice in the readings to judge.</summary>
        Unmeasurable,
        /// <summary>
        /// Measured, but neither clearly Rachel's stress nor clearly the moved
        /// one: between the two bands (see JudgeBanded). Asked again.
        /// </summary>
        Borderline,
    }

    /// <summary>How the plugin scaled the level readings it reported.</summary>
    public enum LevelScale
    {
        /// <summary>
        /// Android: Clamp01(0.1 * 10^(rmsdB / 10)), straight from the plugin's
        /// STTInteractionCallbackAndroid.
        /// </summary>
        AndroidPower,
        /// <summary>iOS: a straight line from 130 dB (0) to 150 dB (1).</summary>
        LinearDecibels,
    }

    /// <summary>The result of one check, with the numbers behind it for the attempt log.</summary>
    public struct Judgement
    {
        public Verdict verdict;
        /// <summary>The player's loudness centroid, or NaN when unmeasurable.</summary>
        public float measured;
        /// <summary>Rachel's centroid for this word.</summary>
        public float reference;
        /// <summary>Rachel's centroid for this word with the stress moved.</summary>
        public float moved;
        /// <summary>
        /// False when Rachel's recording does not separate the two stresses
        /// clearly enough to judge a child by (see StressReferences.Enforced):
        /// the verdict is still measured and logged, but not acted on.
        /// </summary>
        public bool enforced;
        /// <summary>The cut-off the measurement was held against.</summary>
        public float threshold;
        /// <summary>Why the check came out the way it did, in plain words.</summary>
        public string reason;
    }

    /// <summary>
    /// Only readings within this many decibels of the loudest one carry
    /// weight. Small enough to sit inside the narrow range an Android
    /// recognizer reports (about 12 dB from silence to a loud voice).
    /// </summary>
    private const float TopWindowDb = 7f;

    /// <summary>Gaps in the voice up to this long are inside one word (a stop consonant).</summary>
    private const float MaxGapInsideWordMs = 250f;

    /// <summary>The quietest-to-loudest range a session needs before it contains a voice at all.</summary>
    private const float MinDynamicRangeDb = 3f;

    /// <summary>Fewest readings that can describe a word of two syllables.</summary>
    private const int MinVoicedReadings = 3;

    /// <summary>
    /// Judges one attempt. tolerance moves the cut-off: 1 puts it halfway
    /// between Rachel's stress and the moved one; smaller is stricter (the
    /// answer has to come closer to Rachel), larger is more forgiving.
    /// </summary>
    public static Judgement Judge(
        IList<float> levels,
        float sampleIntervalMs,
        LevelScale scale,
        StressPattern pattern,
        float tolerance = 1f)
    {
        Judgement j = new Judgement();
        j.measured = float.NaN;
        StressReferences.Entry entry = StressReferences.For(pattern);
        j.reference = entry.reference;
        j.moved = entry.moved;
        j.enforced = entry.enforced;
        j.threshold = j.reference + 0.5f * Math.Max(0f, tolerance) * (j.moved - j.reference);

        string why;
        float? c = LoudnessCentroid(levels, sampleIntervalMs, scale, out why);
        if (!c.HasValue)
        {
            j.verdict = Verdict.Unmeasurable;
            j.reason = why;
            return j;
        }

        j.measured = c.Value;

        // Right when the answer is on Rachel's side of the cut-off. The moved
        // stress pulls the centroid one way; the answer must not go that way
        // past the halfway mark.
        bool movedIsEarlier = j.moved < j.reference;
        bool ok = movedIsEarlier ? c.Value >= j.threshold : c.Value <= j.threshold;

        j.verdict = ok ? Verdict.Matches : Verdict.WrongStress;
        j.reason = ok
            ? "stress heard where " + pattern.Spoken + " needs it"
            : "stress heard on the wrong syllable - the word asked for is " + pattern.Spoken;
        return j;
    }

    /// <summary>Clear space kept between each band and the other stress's measurements.</summary>
    public const float BandMargin = 0.02f;

    /// <summary>
    /// The judgement strict mode acts on. Rather than one cut-off with
    /// nothing around it - where an answer near the line falls either side
    /// by chance - there are two bands, both taken from Rachel's recording as
    /// every simulated phone hears it (StressReferences):
    ///
    ///   Matches      past EVERY reading the moved stress gave, and past the
    ///                halfway mark: nothing that sounds like the other word
    ///                was ever measured there.
    ///   WrongStress  past every reading her own stress gave, the other way.
    ///   Borderline   in between: asked again, never scored.
    ///
    /// Where the two stresses overlap on some phone, the Matches band starts
    /// beyond the overlap - so a word whose loudness readings barely tell its
    /// stresses apart can still be passed, but only by a stress at least as
    /// clear as Rachel's, and never by one that sounds like the moved stress.
    /// </summary>
    public static Judgement JudgeBanded(IList<float> levels, float sampleIntervalMs, LevelScale scale, StressPattern pattern)
    {
        Judgement j = Judge(levels, sampleIntervalMs, scale, pattern, 1f);
        if (j.verdict == Verdict.Unmeasurable) return j;

        StressReferences.Entry e = StressReferences.For(pattern);
        bool movedIsEarlier = e.moved < e.reference;
        float mid = 0.5f * (e.reference + e.moved);
        float c = j.measured;
        bool right, wrong;
        if (movedIsEarlier)
        {
            float accept = Math.Max(e.movedMax + BandMargin, mid);
            float reject = Math.Min(e.ownMin - BandMargin, mid);
            j.threshold = accept;
            right = c >= accept;
            wrong = c <= reject;
        }
        else
        {
            float accept = Math.Min(e.movedMin - BandMargin, mid);
            float reject = Math.Max(e.ownMax + BandMargin, mid);
            j.threshold = accept;
            right = c <= accept;
            wrong = c >= reject;
        }
        j.verdict = right ? Verdict.Matches : wrong ? Verdict.WrongStress : Verdict.Borderline;
        j.reason = (right ? "stress heard where " + pattern.Spoken + " needs it"
                   : wrong ? "stress heard on the wrong syllable - the word asked for is " + pattern.Spoken
                   : "not clearly either stress in the loudness readings") +
                   " (centroid " + c.ToString("0.000") + ", passes from " + j.threshold.ToString("0.000") + ")";
        return j;
    }

    /// <summary>
    /// Where in the spoken word the weight of the voice sits, from 0 (its
    /// start) to 1 (its end); null when the readings hold no clear voice.
    /// </summary>
    public static float? LoudnessCentroid(
        IList<float> levels, float sampleIntervalMs, LevelScale scale, out string whyNot)
    {
        whyNot = null;
        if (levels == null || levels.Count < MinVoicedReadings + 2)
        {
            whyNot = "no voice level readings from the recognizer";
            return null;
        }

        int n = levels.Count;
        float[] db = new float[n];
        for (int i = 0; i < n; i++) db[i] = ToDecibels(levels[i], scale);

        float[] sorted = (float[])db.Clone();
        Array.Sort(sorted);
        float floor = sorted[(int)(0.10f * (n - 1))];
        float top = sorted[n - 1];
        if (top - floor < MinDynamicRangeDb)
        {
            whyNot = "the readings never rose above the background";
            return null;
        }

        // Everything is measured down from the LOUDEST reading, in plain
        // decibels: only the loud part of each syllable - where stress lives -
        // counts. That makes it indifferent to how much of the quiet end a
        // device clamps away and where its floor sits.
        float window = Math.Min(TopWindowDb, 0.85f * (top - floor));
        float level = top - window;

        int loudest = 0;
        for (int i = 1; i < n; i++) if (db[i] > db[loudest]) loudest = i;

        // The word is the run of loud readings around the loudest one. Short
        // dips (a stop consonant like the k in bukas) stay inside it; a longer
        // silence ends it, so a cough or a bump before the child spoke does
        // not stretch the word.
        float interval = sampleIntervalMs > 0f ? sampleIntervalMs : 50f;
        int maxGap = Math.Max(1, (int)Math.Round(MaxGapInsideWordMs / interval));

        int start = loudest, end = loudest;
        for (int i = loudest - 1, gap = 0; i >= 0; i--)
        {
            if (db[i] > level) { start = i; gap = 0; }
            else if (++gap > maxGap) break;
        }
        for (int i = loudest + 1, gap = 0; i < n; i++)
        {
            if (db[i] > level) { end = i; gap = 0; }
            else if (++gap > maxGap) break;
        }

        if (end - start + 1 < MinVoicedReadings)
        {
            whyNot = "the voice was too short to hear its syllables";
            return null;
        }

        // Where the voice crossed the line, between readings rather than on
        // them: at a slow report rate a word is only a handful of readings
        // long, and rounding its ends to whole readings would move the
        // centroid more than the stress does.
        double tStart = start + 0.5, tEnd = end + 0.5;
        if (start > 0 && db[start] > db[start - 1])
            tStart -= Math.Min(1.0, (db[start] - level) / (db[start] - db[start - 1]));
        else tStart -= 0.5;
        if (end < n - 1 && db[end] > db[end + 1])
            tEnd += Math.Min(1.0, (db[end] - level) / (db[end] - db[end + 1]));
        else tEnd += 0.5;

        double sum = 0, weighted = 0;
        for (int i = start; i <= end; i++)
        {
            double above = db[i] - level;
            if (above <= 0) continue;
            double weight = above / window;
            sum += weight;
            weighted += weight * (i + 0.5);
        }

        if (sum <= 0)
        {
            whyNot = "the readings never rose above the background";
            return null;
        }
        double centroid = (weighted / sum - tStart) / (tEnd - tStart);

        // Length, measured directly: where the dip between syllables falls.
        // Tagalog carries stress mostly by holding the stressed vowel longer,
        // so a long first syllable pushes the dip late (táyo) and a short one
        // leaves it early (tayô). Taken as the deepest reading in the middle
        // of the word, relative to the louder of its two sides.
        int lo = start + (end - start + 2) / 5, hi = end - (end - start + 2) / 5;
        double split = 0.5;
        double bestDepth = 0;
        for (int i = Math.Max(start + 1, lo); i <= Math.Min(end - 1, hi); i++)
        {
            float left = db[start], right = db[end];
            for (int k = start; k < i; k++) left = Math.Max(left, db[k]);
            for (int k = i + 1; k <= end; k++) right = Math.Max(right, db[k]);
            double depth = Math.Min(left, right) - db[i];
            if (depth > bestDepth) { bestDepth = depth; split = (i + 0.5 - tStart) / (tEnd - tStart); }
        }

        // Late stress = late centroid = EARLY dip, so the dip enters as 1 - split.
        double position = 0.5 * centroid + 0.5 * (1.0 - split);
        return (float)Math.Max(0.0, Math.Min(1.0, position));
    }

    /// <summary>Undoes the plugin's scaling, back to decibels (any fixed offset is fine).</summary>
    public static float ToDecibels(float level, LevelScale scale)
    {
        if (scale == LevelScale.LinearDecibels) return 20f * Clamp01(level);

        // AndroidPower: level = 0.1 * 10^(dB/10), so dB = 10 log10(10 * level).
        // Zero means the device reported something quieter than the plugin can
        // express; treat it as a deep floor rather than minus infinity.
        float l = Clamp01(level);
        if (l <= 1e-5f) return -40f;
        return 10f * (float)Math.Log10(10.0 * l);
    }

    private static float Clamp01(float v)
    {
        return v < 0f ? 0f : (v > 1f ? 1f : v);
    }
}
