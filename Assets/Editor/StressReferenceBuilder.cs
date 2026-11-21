using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using Syllable = RawStressAnalyzer.Syllable;

/// <summary>
/// Measures Teacher Rachel's recordings and writes StressReferenceData.cs:
/// for every stress-checked word, her loudness centroid, the centroid of the
/// same recording with the stress moved, and whether the two are far enough
/// apart to judge a child by. See StressReferences for what each one means.
///
/// Plain C# so Tools/stresstests can run it without Unity; the menu item in
/// StressReferenceTool is a thin wrapper.
/// </summary>
public static class StressReferenceBuilder
{
    /// <summary>
    /// Two-syllable words with a recording, by where the stress falls. Only
    /// used for the fallback averages of a stress-checked word that has no
    /// recording yet. Ordinary dictionary stress; like the marks in WordStress
    /// they are the group's reading, for the validating teacher to confirm.
    /// </summary>
    public static readonly string[] CalibrationInitial =
    {
        "lakad", "kanan", "ilaw", "tabo", "suklay", "medyas", "basa", "bati",
        "lapis", "inom", "labas", "tulak", "pagod", "pasok", "laro", "kain", "tawa", "kuha",
    };

    public static readonly string[] CalibrationFinal =
    {
        "tayo", "upo", "bukas", "talon", "sabon", "damit", "kanin", "alis", "akyat",
        "sakay", "itlog", "sagot", "tingin", "takbo", "ligpit", "linis", "aklat", "tulong", "bangon", "sulat",
    };

    /// <summary>
    /// Phones report levels at different rates and place a voice at different
    /// heights in their range, and both nudge the centroid. So a recording is
    /// measured the way each combination of these would hear it.
    /// </summary>
    public static readonly float[] HopsMs = { 20f, 50f, 100f };
    public static readonly float[] PeaksDb = { 6f, 9f, 12f };

    public sealed class WordResult
    {
        public float reference;
        public float moved;
        public bool enforced;
        public int ownCorrect;     // settings where her own recording passed
        public int movedRejected;  // settings where the moved one was NOT accepted (wrong, or unmeasurable)
        public int settings;
    }

    public sealed class Result
    {
        public SortedDictionary<string, WordResult> rows =
            new SortedDictionary<string, WordResult>(StringComparer.OrdinalIgnoreCase);
        public float averageInitial;
        public float averageFinal;
        public List<string> missing = new List<string>();
        /// <summary>Words this source does not cover (the loudness fallback: three or more syllables).</summary>
        public List<string> notApplicable = new List<string>();
    }

    public static string WavPath(string voDirectory, string word)
    {
        return Path.Combine(voDirectory, "word_" + word.ToLowerInvariant() + ".wav");
    }

    /// <summary>Centroid of mono samples under every device setting (NaN where unmeasurable).</summary>
    public static List<float> MeasureAll(float[] samples, int sampleRate)
    {
        List<float> values = new List<float>();
        foreach (float hop in HopsMs)
            foreach (float peak in PeaksDb)
            {
                string why;
                float? c = StressAnalyzer.LoudnessCentroid(
                    WavLevelStream.FromSamples(samples, sampleRate, hop, peak), hop,
                    StressAnalyzer.LevelScale.AndroidPower, out why);
                values.Add(c.HasValue ? c.Value : float.NaN);
            }
        return values;
    }

    public static float Median(List<float> values)
    {
        List<float> v = values.Where(x => !float.IsNaN(x)).OrderBy(x => x).ToList();
        return v.Count == 0 ? float.NaN : v[v.Count / 2];
    }

    public static Result Build(string voDirectory)
    {
        Result result = new Result();

        foreach (string word in WordStress.WordsWithStress)
        {
            StressPattern pattern = StressPattern.For(word);
            if (pattern == null) continue;
            // The loudness readings cannot be cut into syllables, only weighed
            // early against late - which tells first from last stress in a
            // two-syllable word and nothing in a longer one.
            if (pattern.SyllableCount != 2) { result.notApplicable.Add(word); continue; }
            string path = WavPath(voDirectory, word);
            if (!File.Exists(path)) { result.missing.Add(word); continue; }

            int sr;
            float[] audio = WavLevelStream.ReadMono(path, out sr);
            List<float> own = MeasureAll(audio, sr);
            List<float> moved = MeasureAll(MoveStress(audio, sr, pattern.IsFinal, 1f), sr);

            WordResult w = new WordResult();
            w.reference = Median(own);
            w.moved = Median(moved);
            w.settings = own.Count;
            if (float.IsNaN(w.reference) || float.IsNaN(w.moved)) { result.missing.Add(word); continue; }

            float cut = 0.5f * (w.reference + w.moved);
            bool movedIsEarlier = w.moved < w.reference;
            foreach (float c in own)
                if (!float.IsNaN(c) && (movedIsEarlier ? c >= cut : c <= cut)) w.ownCorrect++;
            foreach (float c in moved)
                // Unmeasurable counts too: it is asked again, never accepted.
                if (float.IsNaN(c) || (movedIsEarlier ? c < cut : c > cut)) w.movedRejected++;

            // Enforced only when her own recording passes on EVERY simulated
            // device - a child must never be failed for saying a word the way
            // the narrator does - and the moved one fails on all but one.
            w.enforced = w.ownCorrect == w.settings && w.movedRejected >= w.settings - 1;
            result.rows[word] = w;
        }

        result.averageInitial = Average(voDirectory, CalibrationInitial);
        result.averageFinal = Average(voDirectory, CalibrationFinal);
        return result;
    }

    private static float Average(string voDirectory, string[] words)
    {
        List<float> values = new List<float>();
        foreach (string w in words)
        {
            string path = WavPath(voDirectory, w);
            if (!File.Exists(path)) continue;
            int sr;
            float m = Median(MeasureAll(WavLevelStream.ReadMono(path, out sr), sr));
            if (!float.IsNaN(m)) values.Add(m);
        }
        return values.Count == 0 ? 0.5f : values.Average();
    }

    // ---- Moving the stress of a recording ----------------------------------

    /// <summary>
    /// Moves the stress of a two-syllable recording onto the other syllable,
    /// the way Tagalog stress is carried: the newly stressed syllable is held
    /// longer and said louder, the other shortened and softened. toFirst:
    /// tayô -> táyo. strength 1 is an ordinary, clear stress (x1.6 / x0.7
    /// length, +2 / -4 dB); 0 changes nothing. durationOnly leaves loudness
    /// alone, for testing the length cue by itself.
    /// </summary>
    public static float[] MoveStress(float[] x, int sampleRate, bool toFirst, float strength, bool durationOnly = false,
                                     float pitchSemitones = 0f)
    {
        // With pitch (the raw-audio path) the syllables are split exactly where
        // RawStressAnalyzer splits them, so the change lands on the syllables
        // the analyzer will measure.
        int split = SyllableSplit(x, sampleRate);
        float existingRise = 0f;   // semitones the second syllable already sits above the first
        if (pitchSemitones != 0f)
        {
            RawStressAnalyzer.Measurement m = RawStressAnalyzer.Measure(x, sampleRate);
            if (m.ok)
            {
                split = Math.Min(x.Length, (int)(m.splitSeconds * sampleRate));
                if (m.first.pitchHz > 0 && m.second.pitchHz > 0)
                    existingRise = (float)(12 * Math.Log(m.second.pitchHz / m.first.pitchHz, 2));
            }
        }
        float[] a = x.Take(split).ToArray(), b = x.Skip(split).ToArray();
        float longer = 1f + 0.6f * strength, shorter = 1f - 0.3f * Math.Min(strength, 1.5f);
        float up = durationOnly ? 1f : Db(2f * strength), down = durationOnly ? 1f : Db(-4f * strength);

        // Pitch moves with the stress: the newly stressed syllable ends up
        // pitchSemitones ABOVE the other. Whatever rise or fall the recording
        // already has is undone first - Rachel's tayô climbs about seven
        // semitones onto its last syllable, and a táyo that kept that climb
        // would still sound final-stressed. The change is split evenly
        // between the two syllables. With no pitch asked for, nothing moves.
        float change = 0f;
        if (pitchSemitones != 0f)
            change = toFirst ? existingRise + pitchSemitones : pitchSemitones - existingRise;
        change = Math.Max(change, Math.Abs(pitchSemitones) * 1.5f) * strength;
        if (pitchSemitones == 0f) change = 0f;
        float rise = 0.5f * change, fall = -0.5f * change;
        if (toFirst)
        {
            a = Gain(Stretch(PitchShift(a, sampleRate, rise), sampleRate, longer), up);
            b = Gain(Stretch(PitchShift(b, sampleRate, fall), sampleRate, shorter), down);
        }
        else
        {
            a = Gain(Stretch(PitchShift(a, sampleRate, fall), sampleRate, shorter), down);
            b = Gain(Stretch(PitchShift(b, sampleRate, rise), sampleRate, longer), up);
        }
        return a.Concat(b).ToArray();
    }

    /// <summary>
    /// Shifts pitch, keeping length: resample (which moves pitch and length
    /// together) and then time-stretch back with overlap-add, whose grains keep
    /// their new pitch.
    /// </summary>
    public static float[] PitchShift(float[] x, int sampleRate, float semitones)
    {
        if (Math.Abs(semitones) < 0.01f) return x;
        float ratio = (float)Math.Pow(2, semitones / 12.0);
        return Stretch(Resample(x, ratio), sampleRate, ratio);
    }

    /// <summary>Plays x back ratio times faster (linear interpolation): pitch up, length down.</summary>
    public static float[] Resample(float[] x, float ratio)
    {
        int n = (int)(x.Length / ratio);
        float[] y = new float[n];
        for (int i = 0; i < n; i++)
        {
            double pos = i * ratio;
            int k = (int)pos;
            double f = pos - k;
            float s0 = k < x.Length ? x[k] : 0f, s1 = k + 1 < x.Length ? x[k + 1] : s0;
            y[i] = (float)(s0 + (s1 - s0) * f);
        }
        return y;
    }

    // ---- The raw-audio references --------------------------------------------

    /// <summary>
    /// How a phone microphone might hear a word: room noise at three levels,
    /// crossed with the voice as recorded and raised the way a child's is
    /// (x1.25 and x1.45 faster and higher, like a smaller voice).
    /// </summary>
    public static readonly float[] RawNoiseDb = { -60f, -45f, -38f };
    public static readonly float[] RawVoiceRaise = { 1f, 1.25f, 1.45f };

    /// <summary>A recording as one raw-audio condition would capture it.</summary>
    public static float[] RawCondition(float[] x, int sampleRate, float noiseDb, float raise, int seed = 7)
    {
        float[] y = raise != 1f ? Resample(x, raise) : (float[])x.Clone();
        int pad = sampleRate / 4;
        float[] z = new float[y.Length + 2 * pad];
        Array.Copy(y, 0, z, pad, y.Length);
        Random rng = new Random(seed);
        double amp = Math.Pow(10, noiseDb / 20) * Math.Sqrt(3);
        for (int i = 0; i < z.Length; i++) z[i] += (float)((rng.NextDouble() * 2 - 1) * amp);
        return z;
    }

    /// <summary>
    /// Raw stress scores of one recording under every condition (NaN where
    /// unmeasurable): how far the word's stressed syllable stands out.
    /// </summary>
    public static List<float> MeasureAllRaw(float[] x, int sampleRate, StressPattern pattern)
    {
        List<float> values = new List<float>();
        foreach (float noise in RawNoiseDb)
            foreach (float raise in RawVoiceRaise)
            {
                RawStressAnalyzer.Measurement m = RawStressAnalyzer.Measure(
                    RawCondition(x, sampleRate, noise, raise), sampleRate, pattern.SyllableCount);
                values.Add(m.ok ? m.ScoreFor(pattern.StressedSyllable) : float.NaN);
            }
        return values;
    }

    /// <summary>
    /// The syllable a mis-stressed word most likely puts the stress on: the
    /// one before, for a word stressed on its last syllable (tayô -> táyo),
    /// otherwise the one after (bángon -> bangón, sapátos -> sapatós).
    /// </summary>
    public static int MovedSyllable(StressPattern pattern)
    {
        return pattern.IsFinal ? pattern.SyllableCount - 2 : pattern.StressedSyllable + 1;
    }

    /// <summary>
    /// Moves a recording's stress off the word's stressed syllable onto
    /// MovedSyllable, for a word of any length: that syllable is held longer,
    /// said louder and raised in pitch, the old one shortened, softened and
    /// lowered (strength 1: x1.6 / x0.7 length, +2 / -4 dB, and the new one
    /// ends up pitchSemitones above the old, whatever the recording's own
    /// pitch was). The syllables are cut exactly where RawStressAnalyzer cuts
    /// them, so the change lands on the syllables it measures. Others are
    /// left alone. lengthAndLoudnessOnly leaves pitch as recorded.
    /// </summary>
    public static float[] MoveStressRaw(float[] x, int sampleRate, StressPattern pattern, float strength,
                                        bool lengthAndLoudnessOnly = false, float pitchSemitones = MovedPitchSemitones)
    {
        int n = pattern.SyllableCount;
        RawStressAnalyzer.Measurement m = RawStressAnalyzer.Measure(x, sampleRate, n);
        if (!m.ok) return x;

        List<float[]> parts = new List<float[]>();
        int from = 0;
        for (int i = 0; i < n; i++)
        {
            int to = i < n - 1 ? Math.Min(x.Length, (int)(m.splitsSeconds[i] * sampleRate)) : x.Length;
            parts.Add(x.Skip(from).Take(Math.Max(0, to - from)).ToArray());
            from = to;
        }

        int oldStress = pattern.StressedSyllable, newStress = MovedSyllable(pattern);
        float change = 0f;
        if (!lengthAndLoudnessOnly && pitchSemitones != 0f)
        {
            Syllable oldS = m.syllables[oldStress], newS = m.syllables[newStress];
            float above = oldS.pitchHz > 0 && newS.pitchHz > 0
                ? (float)(12 * Math.Log(oldS.pitchHz / newS.pitchHz, 2)) : 0f;
            change = Math.Max(above + pitchSemitones, pitchSemitones * 1.5f) * strength;
        }

        float longer = 1f + 0.6f * strength, shorter = 1f - 0.3f * Math.Min(strength, 1.5f);
        parts[newStress] = Gain(Stretch(PitchShift(parts[newStress], sampleRate, 0.5f * change), sampleRate, longer), Db(2f * strength));
        parts[oldStress] = Gain(Stretch(PitchShift(parts[oldStress], sampleRate, -0.5f * change), sampleRate, shorter), Db(-4f * strength));
        return parts.SelectMany(p => p).ToArray();
    }

    /// <summary>
    /// How much lower the moved-stress score must be than Rachel's before a
    /// word is enforced. One unit is, for instance, a syllable held 2.7 times
    /// as long, or 6 dB louder, or 3 semitones higher.
    /// </summary>
    public const float MinRawSeparation = 1.0f;

    /// <summary>Semitones of pitch rise the moved-stress reference gives the newly stressed syllable.</summary>
    public const float MovedPitchSemitones = 2f;

    public static Result BuildRaw(string voDirectory)
    {
        Result result = new Result();
        foreach (string word in WordStress.WordsWithStress)
        {
            StressPattern pattern = StressPattern.For(word);
            if (pattern == null) continue;
            string path = WavPath(voDirectory, word);
            if (!File.Exists(path)) { result.missing.Add(word); continue; }

            int sr;
            float[] audio = WavLevelStream.ReadMono(path, out sr);
            List<float> own = MeasureAllRaw(audio, sr, pattern);
            List<float> moved = MeasureAllRaw(MoveStressRaw(audio, sr, pattern, 1f), sr, pattern);

            WordResult w = new WordResult();
            w.reference = Median(own);
            w.moved = Median(moved);
            w.settings = own.Count;
            if (float.IsNaN(w.reference) || float.IsNaN(w.moved)) { result.missing.Add(word); continue; }

            float cut = RawStressAnalyzer.Cutoff(w.reference, w.moved);
            bool movedIsLower = w.moved < w.reference;
            foreach (float c in own)
                if (!float.IsNaN(c) && (movedIsLower ? c >= cut : c <= cut)) w.ownCorrect++;
            foreach (float c in moved)
                // Unmeasurable counts too: it is asked again, never accepted.
                if (float.IsNaN(c) || (movedIsLower ? c < cut : c > cut)) w.movedRejected++;

            // The raw score is how far the stressed syllable stands out, so
            // moving the stress away MUST lower it, and by a clear margin. If
            // it does not, the analyzer is not finding this word's syllables
            // (a sonorant like the n in kanan leaves no dip to cut at), and
            // whatever it measures is not the stress: never enforce that.
            bool measuresStress = w.reference - w.moved >= MinRawSeparation;
            w.enforced = measuresStress && w.ownCorrect == w.settings && w.movedRejected >= w.settings - 1;
            result.rows[word] = w;
        }
        return result;
    }

    public static string GenerateRaw(Result r)
    {
        CultureInfo inv = CultureInfo.InvariantCulture;
        StringBuilder sb = new StringBuilder();
        sb.AppendLine("// GENERATED by StressReferenceBuilder.BuildRaw from Assets/Audio/VO/word_*.wav.");
        sb.AppendLine("// Do not edit by hand: re-run Tools > SALINLAHI > Rebuild Stress References");
        sb.AppendLine("// (or Tools/test_stress_rules.sh generate) after re-recording a word.");
        sb.AppendLine("// See RawStressReferences.cs for what the columns mean.");
        sb.AppendLine();
        sb.AppendLine("public static partial class RawStressReferences");
        sb.AppendLine("{");
        sb.AppendLine("    // Score: how far the word's stressed syllable stands out (positive = it is the most prominent).");
        sb.AppendLine("    //            word, Rachel's score, stress moved, enforced   (own ok / moved not accepted, of conditions)");
        sb.AppendLine("    private static readonly Row[] Rows =");
        sb.AppendLine("    {");
        foreach (KeyValuePair<string, WordResult> row in r.rows)
        {
            WordResult w = row.Value;
            sb.AppendLine("        new Row(\"" + row.Key + "\", " + F(w.reference, inv) + ", " + F(w.moved, inv) + ", " +
                          (w.enforced ? "true" : "false") + "),   // " + w.ownCorrect + "/" + w.settings + ", " +
                          w.movedRejected + "/" + w.settings);
        }
        sb.AppendLine("    };");
        sb.AppendLine("}");
        return sb.ToString();
    }

    private static float Db(float db) { return (float)Math.Pow(10, db / 20); }

    private static float[] Gain(float[] x, float g)
    {
        float[] y = new float[x.Length];
        for (int i = 0; i < x.Length; i++) y[i] = x[i] * g;
        return y;
    }

    /// <summary>The deepest dip in loudness between the two syllables (middle half of the voiced part).</summary>
    public static int SyllableSplit(float[] x, int sampleRate)
    {
        int hop = Math.Max(1, sampleRate / 100);
        List<double> e = new List<double>();
        for (int s = 0; s + hop <= x.Length; s += hop)
        {
            double q = 0;
            for (int i = s; i < s + hop; i++) q += x[i] * x[i];
            e.Add(10 * Math.Log10(q / hop + 1e-12));
        }
        if (e.Count < 4) return x.Length / 2;

        double max = e.Max();
        int first = e.FindIndex(v => v > max - 30), last = e.FindLastIndex(v => v > max - 30);
        int lo = first + (last - first) / 4, hi = first + 3 * (last - first) / 4;
        int best = (first + last) / 2;
        double bestDepth = double.MinValue;
        for (int i = lo; i <= hi; i++)
        {
            double left = double.MinValue, right = double.MinValue;
            for (int k = first; k < i; k++) left = Math.Max(left, e[k]);
            for (int k = i; k <= last; k++) right = Math.Max(right, e[k]);
            if (left == double.MinValue) left = e[i];
            double depth = Math.Min(left, right) - e[i];
            if (depth > bestDepth) { bestDepth = depth; best = i; }
        }
        return Math.Min(x.Length, best * hop);
    }

    /// <summary>Overlap-add time stretch with 40 ms Hann grains: plain, but keeps the loudness shape honest.</summary>
    public static float[] Stretch(float[] x, int sampleRate, float factor)
    {
        int grain = Math.Max(4, sampleRate / 25), synHop = grain / 2;
        int outLen = (int)(x.Length * factor);
        float[] y = new float[outLen + grain];
        float[] norm = new float[outLen + grain];
        for (int o = 0; o < outLen; o += synHop)
        {
            int src = (int)(o / factor);
            for (int i = 0; i < grain; i++)
            {
                if (src + i >= x.Length || o + i >= y.Length) break;
                float w = 0.5f - 0.5f * (float)Math.Cos(2 * Math.PI * i / grain);
                y[o + i] += x[src + i] * w;
                norm[o + i] += w;
            }
        }
        for (int i = 0; i < y.Length; i++) if (norm[i] > 1e-3f) y[i] /= norm[i];
        float[] result = new float[outLen];
        Array.Copy(y, result, outLen);
        return result;
    }

    // ---- Output --------------------------------------------------------------

    /// <summary>The C# source of StressReferenceData.cs for a result.</summary>
    public static string Generate(Result r)
    {
        CultureInfo inv = CultureInfo.InvariantCulture;
        StringBuilder sb = new StringBuilder();
        sb.AppendLine("// GENERATED by StressReferenceBuilder from Assets/Audio/VO/word_*.wav.");
        sb.AppendLine("// Do not edit by hand: re-run Tools > SALINLAHI > Rebuild Stress References");
        sb.AppendLine("// (or Tools/test_stress_rules.sh generate) after re-recording a word.");
        sb.AppendLine("// See StressReferences.cs for what the columns mean.");
        sb.AppendLine();
        sb.AppendLine("public static partial class StressReferences");
        sb.AppendLine("{");
        sb.AppendLine("    //            word, Rachel's stress, stress moved, enforced   (own ok / moved rejected, of settings)");
        sb.AppendLine("    private static readonly Row[] Rows =");
        sb.AppendLine("    {");
        foreach (KeyValuePair<string, WordResult> row in r.rows)
        {
            WordResult w = row.Value;
            sb.AppendLine("        new Row(\"" + row.Key + "\", " + F(w.reference, inv) + ", " + F(w.moved, inv) + ", " +
                          (w.enforced ? "true" : "false") + "),   // " + w.ownCorrect + "/" + w.settings + ", " +
                          w.movedRejected + "/" + w.settings);
        }
        sb.AppendLine("    };");
        sb.AppendLine();
        sb.AppendLine("    /// <summary>Average centroid of Rachel's two-syllable words stressed on the first syllable.</summary>");
        sb.AppendLine("    public const float AverageInitial = " + F(r.averageInitial, inv) + ";");
        sb.AppendLine();
        sb.AppendLine("    /// <summary>Average centroid of Rachel's two-syllable words stressed on the last syllable.</summary>");
        sb.AppendLine("    public const float AverageFinal = " + F(r.averageFinal, inv) + ";");
        sb.AppendLine("}");
        return sb.ToString();
    }

    private static string F(float v, CultureInfo inv)
    {
        return v.ToString("0.0000", inv) + "f";
    }
}
