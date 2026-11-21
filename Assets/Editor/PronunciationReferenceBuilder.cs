using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using Cues = PronunciationAssessor.Cues;

/// <summary>
/// Measures Teacher Rachel's word recordings and writes
/// PronunciationReferenceData.cs: for every stress-checked word, the cues of
/// her own delivery, the cues of the same recording with the stress moved to
/// each neighbouring syllable, and whether the word can be trusted - her own
/// delivery judged Correct and the moved one WrongStress under every
/// simulated phone, by the same PronunciationAssessor a child's answer meets.
///
/// Plain C# so Tools/stresstests can run it without Unity.
/// </summary>
public static class PronunciationReferenceBuilder
{
    /// <summary>A recording as one simulated phone would hear it.</summary>
    public struct Condition
    {
        public string name;
        public float noiseDb;
        /// <summary>Played this much faster and higher: a smaller, child-like voice (formants and pitch both rise).</summary>
        public float raise;
        /// <summary>Time-stretched by this much, pitch kept: a slower (above 1) or faster speaker.</summary>
        public float tempo;
    }

    public static readonly Condition[] Conditions = BuildConditions();

    private static Condition[] BuildConditions()
    {
        List<Condition> c = new List<Condition>();
        foreach (float noise in new[] { -60f, -45f, -38f })
            foreach (float raise in new[] { 1f, 1.25f, 1.45f })
                c.Add(new Condition { name = "noise " + noise + " dB, voice x" + raise, noiseDb = noise, raise = raise, tempo = 1f });
        c.Add(new Condition { name = "slow speaker x1.3", noiseDb = -50f, raise = 1f, tempo = 1.3f });
        c.Add(new Condition { name = "fast speaker x0.75", noiseDb = -50f, raise = 1f, tempo = 0.75f });
        c.Add(new Condition { name = "slow child", noiseDb = -50f, raise = 1.35f, tempo = 1.3f });
        return c.ToArray();
    }

    public static float[] Apply(float[] x, int sampleRate, Condition c, int seed = 7)
    {
        float[] y = c.tempo != 1f ? StressReferenceBuilder.Stretch(x, sampleRate, c.tempo) : x;
        return StressReferenceBuilder.RawCondition(y, sampleRate, c.noiseDb, c.raise, seed);
    }

    public sealed class WordResult
    {
        public PronunciationReferences.Entry entry;
        public int ownCorrect, movedWrong, movedAccepted, halfAccepted, conditions;
        /// <summary>The highest position a wrong stress reached, and the lowest Rachel's own did.</summary>
        public float wrongHighest, ownLowest;
        public string ownFailures = "";
    }

    public sealed class Result
    {
        public SortedDictionary<string, WordResult> rows = new SortedDictionary<string, WordResult>(StringComparer.OrdinalIgnoreCase);
        public List<string> missing = new List<string>();
        public List<string> unmeasurable = new List<string>();
    }

    /// <summary>Where each syllable of a recording starts and ends, in samples, from the aligner.</summary>
    public static int[] SyllableSpans(float[] x, int sampleRate, StressPattern pattern, out SyllableAligner.Alignment a)
    {
        a = SyllableAligner.Align(SpeechFeatures.Of(x, sampleRate), pattern.Word);
        if (PronunciationAssessor.Problem(a, pattern) != null) return null;
        int n = pattern.SyllableCount;
        int[] edges = new int[n + 1];
        edges[0] = 0;
        edges[n] = x.Length;
        for (int k = 1; k < n; k++)
        {
            // Halfway through the consonants between the two vowels.
            float mid = 0.5f * (SpeechFeatures.FrameSeconds(a.syllables[k - 1].to + 1) + SpeechFeatures.FrameSeconds(a.syllables[k].from));
            // The aligner's frame f describes the 20 ms from 10 ms after its start.
            edges[k] = Math.Min(x.Length, Math.Max(edges[k - 1], (int)((mid + 0.015f) * sampleRate)));
        }
        return edges;
    }

    /// <summary>
    /// Moves a recording's stress from the word's stressed syllable onto
    /// syllable 'to': that one held longer, said louder and raised, the old one
    /// shortened, softened and lowered (strength 1: x1.6 / x0.7 length, +2 / -4
    /// dB, and the new one ends up 2 semitones above the old whatever the
    /// recording's own pitch was). Returns null if the syllables cannot be found.
    /// </summary>
    public static float[] MoveStress(float[] x, int sampleRate, StressPattern pattern, int to, float strength)
    {
        SyllableAligner.Alignment a;
        int[] edges = SyllableSpans(x, sampleRate, pattern, out a);
        if (edges == null) return null;

        int from = pattern.StressedSyllable;
        List<float[]> parts = new List<float[]>();
        for (int k = 0; k < pattern.SyllableCount; k++)
            parts.Add(x.Skip(edges[k]).Take(edges[k + 1] - edges[k]).ToArray());

        SyllableAligner.Syllable oldS = a.syllables[from], newS = a.syllables[to];
        float above = oldS.pitchHz > 0 && newS.pitchHz > 0 ? (float)(12 * Math.Log(oldS.pitchHz / newS.pitchHz, 2)) : 0f;
        float change = Math.Max(above + 2f, 3f) * strength;
        float longer = 1f + 0.6f * strength, shorter = 1f - 0.3f * Math.Min(strength, 1.5f);

        parts[to] = Gain(StressReferenceBuilder.Stretch(StressReferenceBuilder.PitchShift(parts[to], sampleRate, 0.5f * change), sampleRate, longer), 2f * strength);
        parts[from] = Gain(StressReferenceBuilder.Stretch(StressReferenceBuilder.PitchShift(parts[from], sampleRate, -0.5f * change), sampleRate, shorter), -4f * strength);
        return parts.SelectMany(p => p).ToArray();
    }

    private static float[] Gain(float[] x, float db)
    {
        float g = (float)Math.Pow(10, db / 20);
        float[] y = new float[x.Length];
        for (int i = 0; i < x.Length; i++) y[i] = x[i] * g;
        return y;
    }

    /// <summary>The cues (stressed syllable against 'other') of a recording, or null if it cannot be aligned.</summary>
    public static Cues? Measure(float[] x, int sampleRate, StressPattern pattern, int other)
    {
        SyllableAligner.Alignment a = SyllableAligner.Align(SpeechFeatures.Of(x, sampleRate), pattern.Word);
        if (PronunciationAssessor.Problem(a, pattern) != null) return null;
        return PronunciationAssessor.Compare(a.syllables[pattern.StressedSyllable], a.syllables[other]);
    }

    private static Cues MedianOf(List<Cues> list)
    {
        Func<Func<Cues, float>, float> med = sel =>
        {
            List<float> v = list.Select(sel).OrderBy(q => q).ToList();
            return v[v.Count / 2];
        };
        List<Cues> pitched = list.Where(c => c.hasPitch).ToList();
        Cues m = new Cues { lengthLn = med(c => c.lengthLn), loudnessDb = med(c => c.loudnessDb) };
        m.hasPitch = pitched.Count * 2 > list.Count;
        if (m.hasPitch)
        {
            List<float> p = pitched.Select(c => c.pitchSt).OrderBy(q => q).ToList();
            m.pitchSt = p[p.Count / 2];
        }
        return m;
    }

    public static Result Build(string voDirectory)
    {
        Result result = new Result();
        foreach (string word in WordStress.WordsWithStress.OrderBy(w => w, StringComparer.OrdinalIgnoreCase))
        {
            StressPattern pattern = StressPattern.For(word);
            if (pattern == null) continue;
            string path = StressReferenceBuilder.WavPath(voDirectory, word);
            if (!File.Exists(path)) { result.missing.Add(word); continue; }

            int sr;
            float[] audio = WavLevelStream.ReadMono(path, out sr);
            WordResult w = BuildWord(audio, sr, pattern);
            if (w == null) { result.unmeasurable.Add(word); continue; }
            result.rows[word] = w;
        }
        return result;
    }

    public static WordResult BuildWord(float[] audio, int sr, StressPattern pattern)
    {
        List<PronunciationAssessor.Contrast> contrasts = new List<PronunciationAssessor.Contrast>();
        Dictionary<int, float[]> movedAudio = new Dictionary<int, float[]>();
        foreach (int other in PronunciationAssessor.Neighbours(pattern))
        {
            float[] moved = MoveStress(audio, sr, pattern, other, 1f);
            if (moved == null) return null;
            movedAudio[other] = moved;

            List<Cues> own = new List<Cues>(), mov = new List<Cues>();
            foreach (Condition c in Conditions)
            {
                Cues? o = Measure(Apply(audio, sr, c), sr, pattern, other);
                Cues? m = Measure(Apply(moved, sr, c), sr, pattern, other);
                if (o.HasValue) own.Add(o.Value);
                if (m.HasValue) mov.Add(m.Value);
            }
            if (own.Count * 2 <= Conditions.Length || mov.Count * 2 <= Conditions.Length) return null;
            Cues r = MedianOf(own), mm = MedianOf(mov);
            bool pitch = r.hasPitch && mm.hasPitch;
            r.hasPitch = mm.hasPitch = pitch;
            contrasts.Add(new PronunciationAssessor.Contrast { other = other, rachel = r, moved = mm });
        }

        WordResult w = new WordResult();
        w.entry = new PronunciationReferences.Entry
        {
            measured = true, trusted = false, contrasts = contrasts.ToArray(),
            acceptFrom = PronunciationAssessor.AcceptAt, rejectFrom = PronunciationAssessor.RejectAt,
        };

        // ---- Calibrate this word's two lines from examples ----------------
        // Wrong: the stress moved onto each neighbour two different ways (the
        // syllables cut by this aligner, and by RawStressAnalyzer's dips), and
        // moved only halfway. Right: Rachel's own delivery.
        List<float[]> wrong = new List<float[]>(movedAudio.Values);
        wrong.Add(StressReferenceBuilder.MoveStressRaw(audio, sr, pattern, 1f));
        List<float[]> half = new List<float[]>();
        foreach (int other in movedAudio.Keys)
        {
            float[] h = MoveStress(audio, sr, pattern, other, 0.5f);
            if (h != null) half.Add(h);
        }

        float wrongHighest = float.MinValue, halfHighest = float.MinValue, ownLowest = float.MaxValue;
        List<PronunciationAssessor.Assessment> ownA = new List<PronunciationAssessor.Assessment>();
        foreach (Condition c in Conditions)
        {
            PronunciationAssessor.Assessment o = PronunciationAssessor.Assess(Apply(audio, sr, c), sr, pattern, w.entry);
            ownA.Add(o);
            if (o.measured == o.measurements && !float.IsNaN(o.positionHigh)) ownLowest = Math.Min(ownLowest, o.positionHigh);
            foreach (float[] x in wrong)
            {
                PronunciationAssessor.Assessment m = PronunciationAssessor.Assess(Apply(x, sr, c), sr, pattern, w.entry);
                if (!float.IsNaN(m.position)) wrongHighest = Math.Max(wrongHighest, m.position);
            }
            foreach (float[] x in half)
            {
                PronunciationAssessor.Assessment m = PronunciationAssessor.Assess(Apply(x, sr, c), sr, pattern, w.entry);
                if (!float.IsNaN(m.position)) halfHighest = Math.Max(halfHighest, m.position);
            }
        }
        w.entry.acceptFrom = Math.Max(PronunciationAssessor.AcceptAt,
                                      Math.Max(wrongHighest + WrongMargin, halfHighest + HalfMargin));
        w.entry.rejectFrom = Math.Min(PronunciationAssessor.RejectAt, ownLowest - WrongMargin);
        w.wrongHighest = wrongHighest;
        w.ownLowest = ownLowest;
        PronunciationReferences.Override(pattern.Word, w.entry);

        // ---- Judge Rachel and the wrong stresses with those lines -----------
        foreach (Condition c in Conditions)
        {
            w.conditions++;
            PronunciationAssessor.Assessment own = PronunciationAssessor.Assess(Apply(audio, sr, c), sr, pattern, w.entry);
            if (own.verdict == PronunciationAssessor.Verdict.Matches) w.ownCorrect++;
            else w.ownFailures += (w.ownFailures.Length > 0 ? "; " : "") + c.name + ": " + own.verdict;

            bool allWrong = true, anyAccepted = false;
            foreach (float[] x in wrong)
            {
                PronunciationAssessor.Assessment m = PronunciationAssessor.Assess(Apply(x, sr, c), sr, pattern, w.entry);
                if (m.verdict != PronunciationAssessor.Verdict.WrongStress) allWrong = false;
                if (m.verdict == PronunciationAssessor.Verdict.Matches) anyAccepted = true;
            }
            if (allWrong) w.movedWrong++;
            if (anyAccepted) w.movedAccepted++;
            foreach (float[] x in half)
                if (PronunciationAssessor.Assess(Apply(x, sr, c), sr, pattern, w.entry).verdict == PronunciationAssessor.Verdict.Matches)
                { w.halfAccepted++; break; }
        }

        // Trusted: Rachel's own delivery passes on every simulated phone and
        // speaker, and the wrong stress is caught on all but one.
        w.entry.trusted = w.ownCorrect == w.conditions && w.movedAccepted == 0 && w.halfAccepted == 0 &&
                          w.movedWrong >= w.conditions - 1;
        PronunciationReferences.Override(pattern.Word, w.entry);
        return w;
    }

    /// <summary>Clear space kept above the highest wrong-stress position, and above the highest half-moved one.</summary>
    public const float WrongMargin = 0.15f, HalfMargin = 0.05f;

    public static string Generate(Result r)
    {
        CultureInfo inv = CultureInfo.InvariantCulture;
        Func<float, string> F = v => v.ToString("0.000", inv) + "f";
        StringBuilder sb = new StringBuilder();
        sb.AppendLine("// GENERATED by PronunciationReferenceBuilder from Assets/Audio/VO/word_*.wav.");
        sb.AppendLine("// Do not edit by hand: re-run Tools/test_stress_rules.sh generate (or Tools > SALINLAHI >");
        sb.AppendLine("// Rebuild Stress References) after re-recording a word. See PronunciationReferences.cs.");
        sb.AppendLine();
        sb.AppendLine("public static partial class PronunciationReferences");
        sb.AppendLine("{");
        sb.AppendLine("    // Cues of the stressed syllable against syllable 'other': ln length ratio, dB, semitones.");
        sb.AppendLine("    //            word, other,  Rachel: len, loud, pitch,  moved: len, loud, pitch,  pitch?, trusted, Correct from, wrong from");
        sb.AppendLine("    //            (Rachel correct / moved caught / moved accepted / half-moved accepted, of conditions)");
        sb.AppendLine("    private static readonly Row[] Rows =");
        sb.AppendLine("    {");
        foreach (KeyValuePair<string, WordResult> kv in r.rows)
        {
            WordResult w = kv.Value;
            foreach (PronunciationAssessor.Contrast c in w.entry.contrasts)
                sb.AppendLine("        new Row(\"" + kv.Key + "\", " + c.other + ", " +
                              F(c.rachel.lengthLn) + ", " + F(c.rachel.loudnessDb) + ", " + F(c.rachel.pitchSt) + ", " +
                              F(c.moved.lengthLn) + ", " + F(c.moved.loudnessDb) + ", " + F(c.moved.pitchSt) + ", " +
                              (c.rachel.hasPitch ? "true" : "false") + ", " + (w.entry.trusted ? "true" : "false") + ", " +
                              F(w.entry.acceptFrom) + ", " + F(w.entry.rejectFrom) + "),   // " +
                              w.ownCorrect + "/" + w.conditions + ", " + w.movedWrong + "/" + w.conditions + ", " +
                              w.movedAccepted + "/" + w.conditions + ", " + w.halfAccepted + "/" + w.conditions);
        }
        sb.AppendLine("    };");
        sb.AppendLine("}");
        return sb.ToString();
    }
}
