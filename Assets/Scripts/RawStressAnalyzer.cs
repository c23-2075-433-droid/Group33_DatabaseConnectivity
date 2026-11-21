using System;
using System.Collections.Generic;

/// <summary>
/// Hears WHICH syllable of a two-syllable word the player stressed, from the
/// recorded sound of their voice - its length, loudness and pitch.
///
/// WHY THIS EXISTS
///
/// The device recognizer returns letters only, and tayô ("stand up") and
/// táyo ("we") are the same letters. The plugin also reports a loudness level
/// while it listens, and StressAnalyzer uses that when nothing better is
/// available - but loudness alone separated the two too unreliably to judge a
/// child by. This class works on the actual audio, which StressAudioCapture
/// records alongside the recognizer, so it can use the cues Tagalog stress is
/// really carried by:
///
///   length    the stressed vowel is held longer - the main cue in Tagalog;
///   loudness  it is said louder;
///   pitch     the voice rises on it.
///
/// HOW
///
///   1. The audio is cut into 10 ms frames. Each frame gets its loudness (dB)
///      and, if it is voiced, its pitch (YIN, 75-500 Hz, so children's voices
///      are covered).
///   2. The word is the run of voiced, loud frames around the loudest one.
///   3. It is split into its two syllables at the deepest dip in the middle of
///      the word (an unvoiced consonant counts as a deep dip).
///   4. For each syllable: how long its vowel lasts (voiced frames within
///      10 dB of that syllable's own peak), its peak loudness, its high pitch.
///   5. One score: positive when the SECOND syllable is the prominent one.
///
///        score = ln(length2 / length1)            length
///              + (loudness2 - loudness1) / 6 dB   loudness
///              + (pitch2 - pitch1) / 3 semitones  pitch
///
/// The score is compared with Teacher Rachel's own recording of the word and
/// with the same recording with the stress moved (RawStressReferences), and
/// the cut-off sits halfway between them - the same scheme as StressAnalyzer,
/// on much better evidence. Because every part of the score is a RATIO within
/// one utterance, it does not care how loud the child is, how far the phone
/// is, or how high their voice is.
///
/// Words of any number of syllables are handled: each syllable is measured,
/// and the score is how far the stressed one stands out over the strongest
/// of the others (for two syllables, exactly the score described above).
/// </summary>
public static class RawStressAnalyzer
{
    public struct Syllable
    {
        public float lengthMs;
        public float peakDb;
        public float pitchHz;   // 0 when no voiced frame was found
    }

    public struct Measurement
    {
        public bool ok;
        public string whyNot;
        /// <summary>Two-syllable words only: second syllable's prominence over the first (positive = final stress).</summary>
        public float score;
        public Syllable first;
        public Syllable second;
        /// <summary>Where the first two syllables meet, in seconds from the start of the audio.</summary>
        public float splitSeconds;
        /// <summary>Every syllable, in order.</summary>
        public Syllable[] syllables;
        /// <summary>Where each syllable ends and the next begins, in seconds (one fewer than syllables).</summary>
        public float[] splitsSeconds;

        /// <summary>
        /// How far syllable k stands out over the strongest of the others:
        /// the smallest of its prominence differences to each of them. Positive
        /// means k is the most prominent syllable - the stressed one.
        /// </summary>
        public float ScoreFor(int k)
        {
            float best = float.MaxValue;
            for (int j = 0; j < syllables.Length; j++)
                if (j != k) best = Math.Min(best, Prominence(syllables[k], syllables[j]));
            return best == float.MaxValue ? 0f : best;
        }
    }

    /// <summary>
    /// How much more prominent syllable a is than syllable b:
    ///   ln(length a / length b) + (loudness a - b) / 6 dB + (pitch a - b) / 3 semitones,
    /// the pitch part only when both have a pitch.
    /// </summary>
    public static float Prominence(Syllable a, Syllable b)
    {
        double length = Math.Log(a.lengthMs / b.lengthMs);
        double loud = (a.peakDb - b.peakDb) / 6.0;
        double pitch = 0;
        if (a.pitchHz > 0 && b.pitchHz > 0) pitch = 12 * Math.Log(a.pitchHz / b.pitchHz, 2) / 3.0;
        return (float)(length + loud + pitch);
    }

    public const int FrameMs = 10;
    private const float MinPitchHz = 75f;
    private const float MaxPitchHz = 500f;
    private const float YinThreshold = 0.4f;     // permissive: voicing matters more than a clean pitch here
    private const float WordWindowDb = 30f;      // frames this far below the peak still belong to the word
    private const float VowelWindowDb = 10f;     // a syllable's vowel: within this of its own peak
    private const float MaxGapInsideWordMs = 200f;
    private const float MinWordMs = 150f;
    private const float MinRangeDb = 12f;        // loudest frame vs quietest: below this, no voice

    /// <summary>Measures a recording that should hold one word of the given number of syllables.</summary>
    public static Measurement Measure(float[] samples, int sampleRate, int syllableCount = 2)
    {
        if (syllableCount < 2) syllableCount = 2;
        Measurement m = new Measurement();
        if (samples == null || sampleRate < 8000)
        {
            m.whyNot = "no audio was recorded";
            return m;
        }

        // Pitch tracking is the costly part; 16 kHz is plenty for it and keeps
        // a three-second answer to a few tens of milliseconds on a phone.
        if (sampleRate > 16000)
        {
            samples = Downsample(samples, sampleRate, 16000);
            sampleRate = 16000;
        }

        int hop = sampleRate * FrameMs / 1000;
        int win = hop * 4;
        int frames = (samples.Length - win) / hop;
        if (frames < MinWordMs / FrameMs)
        {
            m.whyNot = "the recording was too short";
            return m;
        }

        float[] db = new float[frames];
        float[] f0 = new float[frames];
        for (int f = 0; f < frames; f++)
        {
            int s = f * hop;
            double sq = 0;
            for (int i = s + hop; i < s + 2 * hop + hop; i++) sq += samples[i] * samples[i];
            db[f] = (float)(10 * Math.Log10(sq / (2 * hop) + 1e-12));
            f0[f] = Pitch(samples, s, win, sampleRate);
        }

        int loudest = 0;
        float quietest = float.MaxValue;
        for (int f = 0; f < frames; f++)
        {
            if (db[f] > db[loudest]) loudest = f;
            quietest = Math.Min(quietest, db[f]);
        }
        if (db[loudest] - quietest < MinRangeDb)
        {
            m.whyNot = "no voice in the recording";
            return m;
        }

        float wordLevel = db[loudest] - WordWindowDb;
        // An unvoiced frame counts as voice for the extent only if it is loud
        // (a fricative like the s in basa); quiet unvoiced frames are gaps.
        Func<int, bool> inWord = f => db[f] > wordLevel && (f0[f] > 0 || db[f] > db[loudest] - 15f);

        int maxGap = (int)(MaxGapInsideWordMs / FrameMs);
        int start = loudest, end = loudest;
        for (int f = loudest - 1, gap = 0; f >= 0; f--)
        {
            if (inWord(f)) { start = f; gap = 0; } else if (++gap > maxGap) break;
        }
        for (int f = loudest + 1, gap = 0; f < frames; f++)
        {
            if (inWord(f)) { end = f; gap = 0; } else if (++gap > maxGap) break;
        }

        if ((end - start + 1) * FrameMs < MinWordMs)
        {
            m.whyNot = "the word was too short to hear two syllables";
            return m;
        }

        // Octave errors: a pitch tracker sometimes locks onto double or half
        // the real pitch, which would read as a huge rise or fall. Fold every
        // frame into the octave around the word's median pitch.
        List<float> voiced = new List<float>();
        for (int f = start; f <= end; f++) if (f0[f] > 0) voiced.Add(f0[f]);
        if (voiced.Count > 0)
        {
            voiced.Sort();
            float median = voiced[voiced.Count / 2];
            for (int f = start; f <= end; f++)
            {
                if (f0[f] <= 0) continue;
                // Only near-doublings: a real rise on a stressed syllable can
                // be well over half an octave, and must survive this.
                while (f0[f] > median * 1.8f) f0[f] *= 0.5f;
                while (f0[f] < median / 1.8f) f0[f] *= 2f;
            }
        }

        // The splits between syllables: the deepest dips in the word, one at a
        // time. Each is looked for in the middle of the piece it divides
        // (never within 20% - or 40 ms - of either end), and a quiet unvoiced
        // frame counts 10 dB deeper (a consonant), while a LOUD frame with no
        // pitch is the tracker missing a vowel, not a gap. For a two-syllable
        // word this is the single deepest dip in the middle of the word.
        // Every syllable has a vowel, so a split only counts with voiced frames
        // on both sides of it - otherwise a final s or t (sulat, linis) gets
        // cut off as a "syllable" of its own.
        int[] voicedBefore = new int[frames + 1];
        for (int f = 0; f < frames; f++) voicedBefore[f + 1] = voicedBefore[f] + (f0[f] > 0 ? 1 : 0);
        const int MinVoicedFrames = 3;

        List<int[]> pieces = new List<int[]> { new[] { start, end } };
        List<int> splits = new List<int>();
        for (int n = 1; n < syllableCount; n++)
        {
            int bestPiece = -1, bestFrame = -1;
            double bestDepth = double.MinValue;
            for (int p = 0; p < pieces.Count; p++)
            {
                int a0 = pieces[p][0], b0 = pieces[p][1];
                int margin = Math.Max((b0 - a0) / 5, 4);
                for (int f = a0 + margin; f <= b0 - margin; f++)
                {
                    if (voicedBefore[f + 1] - voicedBefore[a0] < MinVoicedFrames ||
                        voicedBefore[b0 + 1] - voicedBefore[f + 1] < MinVoicedFrames) continue;
                    float left = float.MinValue, right = float.MinValue;
                    for (int k = a0; k < f; k++) left = Math.Max(left, db[k]);
                    for (int k = f + 1; k <= b0; k++) right = Math.Max(right, db[k]);
                    bool consonant = f0[f] <= 0 && db[f] < db[loudest] - 15f;
                    double here = db[f] - (consonant ? 10 : 0);
                    double depth = Math.Min(left, right) - here;
                    if (depth > bestDepth) { bestDepth = depth; bestPiece = p; bestFrame = f; }
                }
            }
            if (bestPiece < 0)
            {
                m.whyNot = "the word was too short to hear all " + syllableCount + " syllables";
                return m;
            }
            int[] piece = pieces[bestPiece];
            pieces.RemoveAt(bestPiece);
            pieces.Add(new[] { piece[0], bestFrame });
            pieces.Add(new[] { bestFrame + 1, piece[1] });
            splits.Add(bestFrame);
        }
        splits.Sort();

        m.syllables = new Syllable[syllableCount];
        m.splitsSeconds = new float[syllableCount - 1];
        int from = start;
        for (int i = 0; i < syllableCount; i++)
        {
            int to = i < syllableCount - 1 ? splits[i] : end;
            m.syllables[i] = Describe(db, f0, from, to);
            if (m.syllables[i].lengthMs <= 0)
            {
                m.whyNot = "could not hear all " + syllableCount + " syllables";
                return m;
            }
            if (i < syllableCount - 1) m.splitsSeconds[i] = (to * hop + hop + hop) / (float)sampleRate;
            from = to + 1;
        }

        m.first = m.syllables[0];
        m.second = m.syllables[1];
        m.splitSeconds = m.splitsSeconds[0];
        m.score = Prominence(m.second, m.first);
        m.ok = true;
        return m;
    }

    private static Syllable Describe(float[] db, float[] f0, int from, int to)
    {
        Syllable s = new Syllable();
        if (to < from) return s;

        // The syllable's loudness is its VOWEL's: the loudest voiced frame. A
        // consonant burst (the k of lakad) can be louder than the vowel, and
        // measured against it a short vowel would vanish entirely. Only a
        // syllable with no voiced frame at all falls back to every frame.
        float peak = float.MinValue;
        for (int f = from; f <= to; f++) if (f0[f] > 0) peak = Math.Max(peak, db[f]);
        if (peak == float.MinValue)
            for (int f = from; f <= to; f++) peak = Math.Max(peak, db[f]);
        s.peakDb = peak;

        int vowel = 0;
        List<float> pitches = new List<float>();
        for (int f = from; f <= to; f++)
        {
            if (f0[f] <= 0) continue;
            if (db[f] >= peak - VowelWindowDb) vowel++;
            if (db[f] >= peak - 10f) pitches.Add(f0[f]);
        }
        s.lengthMs = vowel * FrameMs;

        if (pitches.Count > 0)
        {
            // The 80th percentile rather than the maximum: one octave error in
            // the pitch tracker should not decide the answer.
            pitches.Sort();
            s.pitchHz = pitches[(int)(0.8f * (pitches.Count - 1))];
        }
        return s;
    }

    /// <summary>Box-filtered linear-interpolation resampling down to a lower rate.</summary>
    public static float[] Downsample(float[] x, int from, int to)
    {
        double ratio = (double)from / to;
        int taps = Math.Max(1, (int)Math.Round(ratio));
        int n = (int)(x.Length / ratio);
        float[] y = new float[n];
        for (int i = 0; i < n; i++)
        {
            double pos = i * ratio;
            int c = (int)pos;
            double sum = 0;
            int count = 0;
            for (int k = c - taps / 2; k <= c + taps / 2; k++)
            {
                if (k < 0 || k >= x.Length) continue;
                sum += x[k];
                count++;
            }
            y[i] = count > 0 ? (float)(sum / count) : 0f;
        }
        return y;
    }

    /// <summary>Each syllable's length, loudness and pitch, for the console: "120/180 ms, -25/-24 dB, 149/176 Hz".</summary>
    public static string Describe(Measurement m)
    {
        if (m.syllables == null) return "";
        string[] len = new string[m.syllables.Length], db = new string[m.syllables.Length], hz = new string[m.syllables.Length];
        for (int i = 0; i < m.syllables.Length; i++)
        {
            len[i] = m.syllables[i].lengthMs.ToString("0");
            db[i] = m.syllables[i].peakDb.ToString("0");
            hz[i] = m.syllables[i].pitchHz.ToString("0");
        }
        return "length " + string.Join("/", len) + " ms, loudness " + string.Join("/", db) + " dB, pitch " + string.Join("/", hz) + " Hz";
    }

    /// <summary>YIN pitch of one window, or 0 when it is not voiced.</summary>
    private static float Pitch(float[] x, int start, int window, int sampleRate)
    {
        int minLag = (int)(sampleRate / MaxPitchHz);
        int maxLag = Math.Min(window / 2, (int)(sampleRate / MinPitchHz));
        int len = window - maxLag;
        if (len <= 0 || start + window > x.Length) return 0f;

        double energy = 0;
        for (int i = 0; i < window; i++) energy += x[start + i] * x[start + i];
        if (energy < 1e-7 * window) return 0f;

        double[] d = new double[maxLag + 1];
        for (int lag = 1; lag <= maxLag; lag++)
        {
            double sum = 0;
            for (int i = 0; i < len; i++)
            {
                double diff = x[start + i] - x[start + i + lag];
                sum += diff * diff;
            }
            d[lag] = sum;
        }

        double running = 0;
        for (int lag = 1; lag <= maxLag; lag++)
        {
            running += d[lag];
            double cmnd = running > 0 ? d[lag] * lag / running : 1;
            d[lag] = cmnd;
        }

        for (int lag = minLag; lag < maxLag; lag++)
        {
            if (d[lag] < YinThreshold)
            {
                while (lag + 1 < maxLag && d[lag + 1] < d[lag]) lag++;
                // Parabolic interpolation around the dip for a finer lag.
                double a = d[lag - 1], b = d[lag], c = d[lag + 1];
                double shift = (a - 2 * b + c) != 0 ? 0.5 * (a - c) / (a - 2 * b + c) : 0;
                return (float)(sampleRate / (lag + shift));
            }
        }
        return 0f;
    }

    /// <summary>
    /// Where the line between the right stress and the wrong one sits.
    ///
    /// The score is 0 where neither syllable stands out, positive when the
    /// second does and negative when the first does - so 0 IS the boundary
    /// between first and last stress, and it is used whenever it lies between
    /// Rachel's recording and the moved one. It used to be the halfway point
    /// between those two, which made Rachel's very emphatic delivery the
    /// yardstick: her tayô rises seven semitones on the last syllable, and an
    /// ordinary tayô said in the Editor (a clear rise, equal lengths) landed
    /// short of the halfway mark and was marked wrong. The midpoint remains
    /// only as a fallback for a word whose two references do not straddle 0.
    ///
    /// tolerance scales the distance from Rachel's value to the line: 1 as
    /// above, lower stricter, higher more forgiving.
    /// </summary>
    public static float Cutoff(float reference, float moved, float tolerance = 1f)
    {
        bool straddles = (reference > 0f && moved < 0f) || (reference < 0f && moved > 0f);
        // Not 0 itself: an answer where neither syllable really stands out
        // (score 0.4 against Rachel's 3.4) is not said the way she says it.
        // It has to come MarginTowardRachel of the way from 0 to her.
        float line = straddles ? MarginTowardRachel * reference : 0.5f * (reference + moved);
        return reference + Math.Max(0f, tolerance) * (line - reference);
    }

    /// <summary>
    /// How far from "neither syllable stands out" toward Rachel's own score an
    /// answer must come before its stress counts as hers.
    /// </summary>
    public const float MarginTowardRachel = 0.3f;

    /// <summary>Rachel's own score from which the cue rule (see IsRight) applies.</summary>
    public const float CueRuleMinReference = 1.0f;

    // A cue counts for or against the stressed syllable only past these:
    // smaller differences are within what one voice does by chance.
    private const double CueLengthLn = 0.1;      // ~10 % longer or shorter
    private const double CueLoudnessDb = 1.5;
    private const double CuePitchSemitones = 1.0;

    /// <summary>
    /// How many of the three cues (length, loudness, pitch) put the stressed
    /// syllable above its strongest rival, and how many put it below.
    /// </summary>
    public static void CountCues(Measurement m, int stressed, out int forIt, out int against)
    {
        forIt = 0; against = 0;
        if (m.syllables == null || stressed < 0 || stressed >= m.syllables.Length) return;

        // The rival is the syllable the stressed one stands out least over -
        // the same one the score is taken against.
        int rival = -1;
        float least = float.MaxValue;
        for (int k = 0; k < m.syllables.Length; k++)
        {
            if (k == stressed) continue;
            float p = Prominence(m.syllables[stressed], m.syllables[k]);
            if (p < least) { least = p; rival = k; }
        }
        if (rival < 0) return;

        Syllable a = m.syllables[stressed], b = m.syllables[rival];
        double length = Math.Log(a.lengthMs / b.lengthMs);
        double loud = a.peakDb - b.peakDb;
        if (length > CueLengthLn) forIt++; else if (length < -CueLengthLn) against++;
        if (loud > CueLoudnessDb) forIt++; else if (loud < -CueLoudnessDb) against++;
        if (a.pitchHz > 0 && b.pitchHz > 0)
        {
            double pitch = 12 * Math.Log(a.pitchHz / b.pitchHz, 2);
            if (pitch > CuePitchSemitones) forIt++; else if (pitch < -CuePitchSemitones) against++;
        }
    }

    /// <summary>
    /// The decision itself, shared by Judge and by the reference builder (so
    /// a word is only enforced when the rule a child faces passes Rachel).
    /// Right when the score is past the cut-off on Rachel's side AND more of
    /// the cues speak for the stressed syllable than against it: a score
    /// carried by one cue alone - typically a question's rising pitch over a
    /// first syllable held longer - is not the word said her way.
    /// </summary>
    public static bool IsRight(Measurement m, StressPattern pattern, float reference, float moved, float tolerance,
                               out float score, out float threshold, out string why)
    {
        threshold = Cutoff(reference, moved, tolerance);
        score = m.ScoreFor(pattern.StressedSyllable);
        bool movedIsLower = moved < reference;
        bool pastLine = movedIsLower ? score >= threshold : score <= threshold;

        int forIt, against;
        CountCues(m, pattern.StressedSyllable, out forIt, out against);
        // Only where Rachel herself makes the syllable clearly stand out: in
        // her Bangon, Ngiti or Ligpit the two syllables are nearly level, so
        // demanding cues there would refuse her own way of saying them.
        bool cuesAgree = reference < CueRuleMinReference || !movedIsLower || forIt > against;

        why = !pastLine ? "not past the line" : !cuesAgree
            ? "only " + forIt + " of length/loudness/pitch for the stressed syllable, " + against + " against"
            : "past the line, " + forIt + " cue(s) for, " + against + " against";
        return pastLine && cuesAgree;
    }

    /// <summary>The verdict for one measurement against a word's references.</summary>
    public static StressAnalyzer.Judgement Judge(float[] samples, int sampleRate, StressPattern pattern, float tolerance = 1f)
    {
        StressAnalyzer.Judgement j = new StressAnalyzer.Judgement();
        j.measured = float.NaN;

        StressReferences.Entry e = RawStressReferences.For(pattern);
        j.reference = e.reference;
        j.moved = e.moved;
        j.enforced = e.enforced;
        j.threshold = Cutoff(e.reference, e.moved, tolerance);

        if (pattern == null || pattern.SyllableCount < 2)
        {
            j.verdict = StressAnalyzer.Verdict.Unmeasurable;
            j.reason = "a one-syllable word has no stress to check";
            return j;
        }

        Measurement m = Measure(samples, sampleRate, pattern.SyllableCount);
        if (!m.ok)
        {
            j.verdict = StressAnalyzer.Verdict.Unmeasurable;
            j.reason = m.whyNot;
            return j;
        }

        float score, threshold;
        string why;
        bool right = IsRight(m, pattern, e.reference, e.moved, tolerance, out score, out threshold, out why);
        j.measured = score;
        j.threshold = threshold;
        j.verdict = right ? StressAnalyzer.Verdict.Matches : StressAnalyzer.Verdict.WrongStress;
        j.reason = (right ? "stress heard where " + pattern.Spoken + " needs it"
                          : "stress not where " + pattern.Spoken + " needs it") +
                   " (score " + score.ToString("0.00") + " vs line " + threshold.ToString("0.00") + ", " + why +
                   "; " + Describe(m) + ")";
        return j;
    }
}
