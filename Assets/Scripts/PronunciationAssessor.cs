using System;
using System.Collections.Generic;
using System.Globalization;

/// <summary>
/// Judges WHERE the player put the stress in a word from the sound of their
/// own voice, by comparing it with how Teacher Rachel says the same word.
/// Replaces RawStressAnalyzer as the judge of a recorded answer.
///
/// WHAT IS COMPARED, AND WHAT IS NOT
///
/// Only the SHAPE of the word: for the stressed syllable against each syllable
/// next to it,
///
///   length    how much longer its vowel is held       (ln of the ratio)
///   loudness  how much louder it is said               (dB difference)
///   pitch     how much higher the voice goes on it     (semitones)
///
/// Each of these is a difference WITHIN one utterance, so the player's voice
/// itself never enters: a child an octave above Rachel, a deep voice, a quiet
/// voice, a far phone, a fast or slow speaker all give the same three numbers
/// for the same way of stressing the word. Voice identity, gender, accent and
/// Rachel's exact pitch are never compared.
///
/// HOW THE DECISION IS MADE
///
/// For each word the references (PronunciationReferences, built from
/// Rachel's own word_*.wav by PronunciationReferenceBuilder) hold two points
/// in that three-number space:
///
///   R  Rachel's own delivery of the word - what the child is asked to copy;
///   M  the same recording with the stress moved to the neighbouring syllable
///      (that syllable held longer, louder and higher; hers shortened,
///      softened, lowered) - táyo for tayô, the other word or the mistake.
///
/// The answer is placed on the line from M to R: t = +1 is exactly Rachel's
/// stress, t = -1 exactly the moved one, 0 halfway. Each cue is weighed by
/// how much one speaker's repetitions of the same stress naturally vary, so
/// a cue that separates R and M by little counts for little.
///
///   t >= acceptFrom   the stress is Rachel's: Correct
///   t <= rejectFrom   the stress is on the other syllable: WrongStress
///   in between        not clear either way: Unclear (asked again, never scored)
///
/// Both lines are CALIBRATED PER WORD from examples of right and wrong
/// stress (PronunciationReferenceBuilder): acceptFrom sits above the highest
/// position any wrong-stress version of Rachel's word reached on any
/// simulated phone or speaker - the stress moved two different ways, and
/// moved only halfway - and never below AcceptAt; rejectFrom sits below the
/// lowest position her own delivery ever reached, and never above RejectAt.
/// So nothing that was ever measured off a wrong stress is in the Correct
/// band, and nothing measured off her right one is in the WrongStress band.
///
/// WHY IT IS CONSISTENT
///
/// The old check had one line and nothing around it, so an answer near the
/// line could fall either side on two near-identical tries. Here:
///
///   - there is a band between Correct and WrongStress; a borderline answer is
///     asked again, never decided by chance;
///   - every answer is measured several times over (the 10 ms frames laid at
///     different offsets, a whisper of noise added), and a verdict only
///     stands if EVERY measurement gives it - otherwise Unclear;
///   - syllables are found with the word's spelling (SyllableAligner) rather
///     than by guessing at the deepest dip, so the same sound is cut the same
///     way each time.
///
/// It also refuses to pass a recording that does not look like the word at
/// all: too few or too many syllables, or no clear vowel where one should be.
///
/// WHAT IT CANNOT DO
///
/// It hears stress and syllable structure, not every sound: telling t from k,
/// or a from o, needs a trained speech model. Those are left to the device
/// recognizer and PronunciationChecker's strict letter matching. See the class
/// comment of AnswerJudge for how the two are combined.
/// </summary>
public static class PronunciationAssessor
{
    /// <summary>The lowest a word's Correct line may be: past the halfway point, toward Rachel.</summary>
    public const float AcceptAt = 0.25f;
    /// <summary>The highest a word's WrongStress line may be: past the halfway point, toward the moved stress.</summary>
    public const float RejectAt = -0.15f;

    /// <summary>
    /// How much one speaker's repetitions of the same stress vary, per cue:
    /// the unit each cue is measured in. Ratios of vowel length wander by
    /// about a quarter, loudness differences by 2-3 dB, pitch differences by
    /// about 2 semitones.
    /// </summary>
    public const float SpreadLengthLn = 0.25f, SpreadLoudnessDb = 2.5f, SpreadPitchSt = 2.0f;

    /// <summary>The three cues of one syllable against another.</summary>
    public struct Cues
    {
        public float lengthLn, loudnessDb, pitchSt;
        public bool hasPitch;

        public override string ToString()
        {
            CultureInfo c = CultureInfo.InvariantCulture;
            return "len " + lengthLn.ToString("+0.00;-0.00", c) + ", loud " + loudnessDb.ToString("+0.0;-0.0", c) +
                   " dB, pitch " + (hasPitch ? pitchSt.ToString("+0.0;-0.0", c) + " st" : "n/a");
        }
    }

    /// <summary>One reference pair: the stressed syllable against one neighbour.</summary>
    public struct Contrast
    {
        /// <summary>The neighbouring syllable the stress might wrongly move to.</summary>
        public int other;
        public Cues rachel;
        public Cues moved;
    }

    public enum Verdict { Matches, WrongStress, Unclear }

    public struct Assessment
    {
        public Verdict verdict;
        /// <summary>The lowest position on the M-to-R line over every neighbour and every measurement (NaN if none).</summary>
        public float position;
        /// <summary>The highest, for the log: a wide spread means the measurements disagreed.</summary>
        public float positionHigh;
        /// <summary>How many measurements were taken, and how many could be made.</summary>
        public int measurements, measured;
        public bool referenceTrusted;
        public string reason;
        public SyllableAligner.Alignment alignment;
    }

    /// <summary>The three cues of syllable a against syllable b.</summary>
    public static Cues Compare(SyllableAligner.Syllable a, SyllableAligner.Syllable b)
    {
        Cues c = new Cues();
        c.lengthLn = (float)Math.Log(Math.Max(10f, a.lengthMs) / Math.Max(10f, b.lengthMs));
        c.loudnessDb = a.loudDb - b.loudDb;
        c.hasPitch = a.pitchHz > 0 && b.pitchHz > 0;
        c.pitchSt = c.hasPitch ? (float)(12 * Math.Log(a.pitchHz / b.pitchHz, 2)) : 0f;
        return c;
    }

    /// <summary>
    /// Where x sits on the line from moved (-1) to rachel (+1), each cue in
    /// units of its natural spread. A cue missing from x (no pitch heard) is
    /// left out of both the measurement and the line.
    /// </summary>
    public static float Position(Cues x, Cues rachel, Cues moved)
    {
        double[] xs = { x.lengthLn / SpreadLengthLn, x.loudnessDb / SpreadLoudnessDb, x.pitchSt / SpreadPitchSt };
        double[] rs = { rachel.lengthLn / SpreadLengthLn, rachel.loudnessDb / SpreadLoudnessDb, rachel.pitchSt / SpreadPitchSt };
        double[] ms = { moved.lengthLn / SpreadLengthLn, moved.loudnessDb / SpreadLoudnessDb, moved.pitchSt / SpreadPitchSt };
        bool usePitch = x.hasPitch && rachel.hasPitch && moved.hasPitch;

        double dot = 0, len2 = 0;
        for (int i = 0; i < 3; i++)
        {
            if (i == 2 && !usePitch) continue;
            double d = rs[i] - ms[i];
            double mid = 0.5 * (rs[i] + ms[i]);
            dot += (xs[i] - mid) * d;
            len2 += d * d;
        }
        if (len2 < 1e-6) return 0f;
        return (float)(2 * dot / len2);
    }

    /// <summary>Framings each answer is measured at (samples at 16 kHz): 0, 3 and 6 ms in.</summary>
    private static readonly int[] Offsets = { 0, 48, 96 };

    /// <summary>
    /// Judges one recording of the given word against the word's references.
    /// samples: mono, -1..1, any rate.
    /// </summary>
    public static Assessment Assess(float[] samples, int sampleRate, StressPattern pattern)
    {
        return Assess(samples, sampleRate, pattern, PronunciationReferences.For(pattern != null ? pattern.Word : null));
    }

    /// <summary>The same, against a given reference (the builder calibrates with this).</summary>
    public static Assessment Assess(float[] samples, int sampleRate, StressPattern pattern, PronunciationReferences.Entry entry)
    {
        Assessment r = new Assessment { position = float.NaN, positionHigh = float.NaN };
        if (pattern == null || pattern.SyllableCount < 2)
        {
            r.verdict = Verdict.Unclear;
            r.reason = "a one-syllable word has no stress to check";
            return r;
        }

        r.referenceTrusted = entry.trusted;
        if (!entry.measured)
        {
            r.verdict = Verdict.Unclear;
            r.reason = "no reference recording of " + pattern.Spoken + " to compare with";
            return r;
        }
        if (samples == null || samples.Length < sampleRate / 5)
        {
            r.verdict = Verdict.Unclear;
            r.reason = "no audio was recorded";
            return r;
        }

        float low = float.MaxValue, high = float.MinValue;
        string firstProblem = null;
        float[] resampled = SpeechFeatures.Resample(samples, sampleRate, SpeechFeatures.SampleRate);

        for (int pass = 0; pass < Offsets.Length * 2; pass++)
        {
            int offset = Offsets[pass % Offsets.Length];
            float[] x = resampled;
            if (pass >= Offsets.Length) x = WithNoise(resampled, -58f, 1000 + pass);
            r.measurements++;

            SpeechFeatures f = SpeechFeatures.Of(x, SpeechFeatures.SampleRate, offset);
            SyllableAligner.Alignment a = SyllableAligner.Align(f, pattern.Word);
            if (pass == 0) r.alignment = a;
            string problem = Problem(a, pattern);
            if (problem != null) { if (firstProblem == null) firstProblem = problem; continue; }

            r.measured++;
            foreach (Contrast c in entry.contrasts)
            {
                Cues cues = Compare(a.syllables[pattern.StressedSyllable], a.syllables[c.other]);
                float t = Position(cues, c.rachel, c.moved);
                low = Math.Min(low, t);
                high = Math.Max(high, t);
            }
        }

        if (r.measured == 0)
        {
            r.verdict = Verdict.Unclear;
            r.reason = firstProblem ?? "could not measure the word";
            return r;
        }
        r.position = low;
        r.positionHigh = high;

        CultureInfo inv = CultureInfo.InvariantCulture;
        string where = " (position " + low.ToString("0.00", inv) + ".." + high.ToString("0.00", inv) +
                       " on the line from the moved stress -1 to Rachel's +1, Correct from " +
                       entry.acceptFrom.ToString("0.00", inv) + ", wrong from " + entry.rejectFrom.ToString("0.00", inv) +
                       "; " + SyllableAligner.Describe(r.alignment) + ")";

        if (r.measured < r.measurements)
        {
            // Some measurements of this very answer could not even find its
            // syllables: it is not clear enough to judge either way.
            r.verdict = Verdict.Unclear;
            r.reason = "the syllables could not be heard clearly every time (" + firstProblem + ")" + where;
        }
        else if (low >= entry.acceptFrom)
        {
            r.verdict = Verdict.Matches;
            r.reason = "stress where Rachel puts it" + where;
        }
        else if (high <= entry.rejectFrom)
        {
            r.verdict = Verdict.WrongStress;
            r.reason = "stress on the wrong syllable" + where;
        }
        else
        {
            r.verdict = Verdict.Unclear;
            r.reason = (high >= entry.acceptFrom && low <= entry.rejectFrom ? "the measurements disagree" : "not clearly either stress") + where;
        }
        return r;
    }

    /// <summary>Why an alignment does not look like the word, or null when it does.</summary>
    public static string Problem(SyllableAligner.Alignment a, StressPattern pattern)
    {
        if (!a.ok) return a.whyNot;
        if (a.syllables.Length != pattern.SyllableCount) return "wrong number of syllables";
        if (a.extraVowelMs >= ExtraVowelLimitMs) return "more syllables than " + pattern.Spoken + " (another word, or the word said twice?)";
        if (a.weakestVowel < WeakestVowelLimit) return "a syllable of " + pattern.Spoken + " is missing or swallowed";
        float loudest = float.MinValue;
        foreach (SyllableAligner.Syllable sy in a.syllables) loudest = Math.Max(loudest, sy.loudDb);
        foreach (SyllableAligner.Syllable sy in a.syllables)
            if (sy.lengthMs < MinSyllableMs || sy.loudDb < loudest - MaxSyllableDropDb)
                return "a syllable of " + pattern.Spoken + " is missing or swallowed";
        return null;
    }

    /// <summary>A vowel this long in a consonant slot means the recording holds more syllables than the word.</summary>
    public const float ExtraVowelLimitMs = 120f;
    /// <summary>A syllable shorter than this (soft length), or this much quieter than the loudest, was not really heard.</summary>
    public const float MinSyllableMs = 25f, MaxSyllableDropDb = 20f;
    /// <summary>A vowel segment this un-vowel-like means a syllable is missing.</summary>
    public const float WeakestVowelLimit = 0.3f;

    private static float[] WithNoise(float[] x, float db, int seed)
    {
        Random rng = new Random(seed);
        double amp = Math.Pow(10, db / 20) * Math.Sqrt(3);
        float[] y = new float[x.Length];
        for (int i = 0; i < x.Length; i++) y[i] = x[i] + (float)((rng.NextDouble() * 2 - 1) * amp);
        return y;
    }

    /// <summary>
    /// The neighbours the stress of a word could wrongly move to: the syllable
    /// before and the syllable after the stressed one, where they exist.
    /// </summary>
    public static List<int> Neighbours(StressPattern pattern)
    {
        List<int> n = new List<int>();
        if (pattern.StressedSyllable > 0) n.Add(pattern.StressedSyllable - 1);
        if (pattern.StressedSyllable < pattern.SyllableCount - 1) n.Add(pattern.StressedSyllable + 1);
        return n;
    }
}
