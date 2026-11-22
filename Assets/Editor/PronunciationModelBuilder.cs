using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;

/// <summary>
/// Trains and evaluates PronunciationModel from the group's recorded dataset,
/// and writes PronunciationModelData.cs.
///
/// THE DATASET
///
/// Recordings/Dataset/SPEAKER/WORD_CLASS_N.wav, made by
/// Tools/pronunciation_dataset/segment.py from one recording per speaker
/// (CLASS is "correct" - the pronunciation the game teaches - or
/// "alternate"). Recordings/ is git-ignored: voices stay on the computer that
/// has them, and only the trained numbers are committed.
///
/// TRAINING
///
/// Every take is also heard under the twelve simulated phones and voices of
/// PronunciationReferenceBuilder.Conditions (room noise, a voice raised x1.25
/// and x1.45 the way a child's is, slower and faster speech), so the model
/// learns what does NOT change with the voice. One logistic regression per
/// word, on standardised features, with a strong L2 penalty: there are few
/// speakers, so the weights must stay small and smooth. The template
/// feature of a training take is always measured against the OTHER
/// speakers' recordings, never its own speaker's, so training sees the same
/// situation a new child is in.
///
/// EVALUATION
///
/// Leave one speaker out: train on all speakers but one, test on the one
/// left out, for each speaker in turn. No test take's speaker is ever in the
/// training data or the template references. Results are given for the
/// clean takes and for the same takes under the simulated conditions.
///
/// Plain C#: runs in Tools/stresstests (Tools/test_stress_rules.sh
/// dataset-eval / dataset-train) without Unity.
/// </summary>
public static class PronunciationModelBuilder
{
    public sealed class Take
    {
        public string speaker, word, file;
        public bool taught;
        public float[] audio;
        public int rate;
        /// <summary>Template frames at each voice height (null when the word cannot be found in it).</summary>
        public List<PronunciationTemplate.Frames> frames;
    }

    /// <summary>
    /// Penalty on the weights (per unit of the mean loss). Larger = flatter,
    /// less confident model; the training rows are mostly simulated copies of
    /// a few takes, so this must not be small.
    /// </summary>
    public static double L2 = 0.05;
    /// <summary>
    /// The confidence lines, applied to the MEDIAN p (PronunciationModel.Decide).
    /// 2026-11-22: 0.7 / 0.3 with three exaggerating speakers made no error but
    /// asked again on most natural takes from a fourth speaker (kl); with the
    /// median rule, 0.6 / 0.3 accepted the most natural takes while still
    /// refusing or asking again on every alternate one. Revisit with every new speaker.
    /// </summary>
    public const float DefaultAccept = 0.6f, DefaultReject = 0.3f;

    /// <summary>Features used by the trained model: length, pitch, loudness, fall, template.</summary>
    public static bool[] DefaultFeatures = { true, true, true, true, true };

    public static string DatasetDirectory(string root) { return Path.Combine(root, "Recordings", "Dataset"); }

    // ---- Loading ------------------------------------------------------------------

    public static List<Take> Load(string dir, List<string> problems)
    {
        List<Take> takes = new List<Take>();
        if (!Directory.Exists(dir)) return takes;
        HashSet<string> words = new HashSet<string>(AmbiguousWords.All.Select(e => e.word), StringComparer.OrdinalIgnoreCase);
        foreach (string spkDir in Directory.GetDirectories(dir).OrderBy(d => d, StringComparer.Ordinal))
        {
            string speaker = Path.GetFileName(spkDir);
            foreach (string f in Directory.GetFiles(spkDir, "*.wav").OrderBy(x => x, StringComparer.Ordinal))
            {
                string[] parts = Path.GetFileNameWithoutExtension(f).Split('_');
                if (parts.Length != 3 || !words.Contains(parts[0]) || (parts[1] != "correct" && parts[1] != "alternate"))
                {
                    problems.Add("skipped " + speaker + "/" + Path.GetFileName(f) + ": name is not WORD_correct|alternate_N.wav");
                    continue;
                }
                AmbiguousWords.Entry e = AmbiguousWords.For(parts[0]);
                Take t = new Take { speaker = speaker, word = e.word, file = f, taught = parts[1] == "correct" };
                t.audio = WavLevelStream.ReadMono(f, out t.rate);
                t.frames = FramesAtHeights(t.audio, t.rate);
                if (t.frames == null) problems.Add("no word found for the template in " + speaker + "/" + Path.GetFileName(f));
                takes.Add(t);
            }
        }
        return takes;
    }

    private static List<PronunciationTemplate.Frames> FramesAtHeights(float[] x, int rate)
    {
        List<PronunciationTemplate.Frames> list = new List<PronunciationTemplate.Frames>();
        foreach (float h in PronunciationTemplateBuilder.VoiceHeights)
        {
            string why;
            PronunciationTemplate.Frames f = PronunciationTemplate.Extract(
                SpeechFeatures.Of(h == 1f ? x : StressReferenceBuilder.Resample(x, h), rate), out why);
            if (f == null) return null;
            list.Add(f);
        }
        return list;
    }

    // ---- Training -----------------------------------------------------------------

    private struct Row { public float[] f; public bool y; public string group; }

    private static void Refs(IEnumerable<Take> takes, List<PronunciationTemplate.Frames> taught, List<PronunciationTemplate.Frames> other)
    {
        foreach (Take t in takes)
        {
            if (t.frames == null) continue;
            (t.taught ? taught : other).AddRange(t.frames);
        }
    }

    /// <summary>The training rows of one take: as recorded and under every simulated condition.</summary>
    private static List<Row> RowsOf(Take t, StressPattern pattern, List<Take> others, bool template)
    {
        List<PronunciationTemplate.Frames> taught = new List<PronunciationTemplate.Frames>(), other = new List<PronunciationTemplate.Frames>();
        if (template) Refs(others.Where(o => o.speaker != t.speaker), taught, other);
        List<Row> rows = new List<Row>();
        List<float[]> versions = new List<float[]> { t.audio };
        int seed = 11;
        foreach (PronunciationReferenceBuilder.Condition c in PronunciationReferenceBuilder.Conditions)
            versions.Add(PronunciationReferenceBuilder.Apply(t.audio, t.rate, c, seed++));
        foreach (float[] x in versions)
        {
            SpeechFeatures s = SpeechFeatures.Of(x, t.rate);
            string why;
            float[] f = PronunciationModel.Measure(s, pattern, null, out why);
            if (f == null) continue;
            if (template && taught.Count > 0 && other.Count > 0) f[PronunciationModel.Template] = PronunciationModel.TemplateScore(s, taught, other);
            rows.Add(new Row { f = f, y = t.taught, group = t.speaker + (t.taught ? "/correct" : "/alternate") });
        }
        return rows;
    }

    /// <summary>Trains one word's model on the given takes (all of them the same word).</summary>
    public static PronunciationModel.Model Train(string word, List<Take> takes, bool[] use, out int rowCount)
    {
        StressPattern pattern = StressPattern.For(word);
        bool template = use[PronunciationModel.Template];
        // A speaker teaches the model a word only with BOTH of its
        // pronunciations: correct takes alone would only teach that their
        // voice means "correct".
        takes = takes.Where(t => takes.Any(o => o.speaker == t.speaker && o.word == t.word && o.taught != t.taught)).ToList();
        List<Row> rows = new List<Row>();
        foreach (Take t in takes) rows.AddRange(RowsOf(t, pattern, takes, template));
        rowCount = rows.Count;

        PronunciationModel.Model m = new PronunciationModel.Model
        {
            word = word,
            acceptFrom = DefaultAccept,
            rejectFrom = DefaultReject,
        };
        Array.Copy(use, m.use, PronunciationModel.FeatureCount);
        for (int i = 0; i < PronunciationModel.FeatureCount; i++)
        {
            List<float> v = rows.Select(r => r.f[i]).Where(x => !float.IsNaN(x)).ToList();
            if (v.Count < 4) { m.use[i] = false; m.mean[i] = 0; m.scale[i] = 1; continue; }
            double mean = v.Average();
            double sd = Math.Sqrt(v.Select(x => (x - mean) * (x - mean)).Average());
            m.mean[i] = (float)mean;
            m.scale[i] = (float)Math.Max(sd, 1e-3);
        }

        // Logistic regression, full-batch gradient descent with an L2 penalty.
        int n = rows.Count, d = PronunciationModel.FeatureCount;
        double[][] z = new double[n][];
        for (int k = 0; k < n; k++)
        {
            z[k] = new double[d];
            for (int i = 0; i < d; i++) z[k][i] = m.use[i] ? PronunciationModel.Standardise(m, i, rows[k].f[i]) : 0;
        }
        // Every speaker's correct takes, and every speaker's alternate takes,
        // weigh the same in total, however many takes each group has. Without
        // this, a speaker who recorded many correct takes and few alternate
        // ones teaches the model "this voice means correct" - a voice bias.
        // The two classes then weigh the same overall.
        Dictionary<string, int> groupCount = rows.GroupBy(r => r.group).ToDictionary(g => g.Key, g => g.Count());
        int posGroups = groupCount.Keys.Count(k => k.EndsWith("/correct")), negGroups = groupCount.Count - posGroups;
        double[] rowWeight = new double[n];
        for (int k = 0; k < n; k++)
            rowWeight[k] = 0.5 * n / Math.Max(1, rows[k].y ? posGroups : negGroups) / groupCount[rows[k].group];
        double[] w = new double[d];
        double b = 0;
        for (int it = 0; it < 3000; it++)
        {
            double[] g = new double[d];
            double gb = 0;
            for (int k = 0; k < n; k++)
            {
                double s = b;
                for (int i = 0; i < d; i++) s += w[i] * z[k][i];
                double p = 1 / (1 + Math.Exp(-s));
                double err = (p - (rows[k].y ? 1 : 0)) * rowWeight[k];
                for (int i = 0; i < d; i++) g[i] += err * z[k][i];
                gb += err;
            }
            for (int i = 0; i < d; i++) w[i] -= 0.3 * (g[i] / n + L2 * w[i]);
            b -= 0.3 * gb / n;
        }
        for (int i = 0; i < d; i++) m.weight[i] = m.use[i] ? (float)w[i] : 0f;
        m.bias = (float)b;

        if (template) Refs(takes, m.taughtRefs, m.otherRefs);
        List<string> speakers = takes.Select(t => t.speaker).Distinct().OrderBy(s => s, StringComparer.Ordinal).ToList();
        m.trainedOn = speakers.Count + " speakers (" + string.Join(", ", speakers) + "), " + takes.Count + " takes";
        return m;
    }

    // ---- Evaluation ---------------------------------------------------------------

    /// <summary>Counts of one evaluation.</summary>
    public sealed class Tally
    {
        public int taughtAccepted, taughtRejected, taughtUncertain, otherAccepted, otherRejected, otherUncertain;
        public int Taught { get { return taughtAccepted + taughtRejected + taughtUncertain; } }
        public int Other { get { return otherAccepted + otherRejected + otherUncertain; } }
        public int Total { get { return Taught + Other; } }
        public int Right { get { return taughtAccepted + otherRejected; } }
        public int Errors { get { return taughtRejected + otherAccepted; } }
        public int Uncertain { get { return taughtUncertain + otherUncertain; } }

        public void Add(bool taught, PronunciationModel.Verdict v)
        {
            if (taught)
            {
                if (v == PronunciationModel.Verdict.Correct) taughtAccepted++;
                else if (v == PronunciationModel.Verdict.Incorrect) taughtRejected++;
                else taughtUncertain++;
            }
            else
            {
                if (v == PronunciationModel.Verdict.Correct) otherAccepted++;
                else if (v == PronunciationModel.Verdict.Incorrect) otherRejected++;
                else otherUncertain++;
            }
        }

        public void Add(Tally t)
        {
            taughtAccepted += t.taughtAccepted; taughtRejected += t.taughtRejected; taughtUncertain += t.taughtUncertain;
            otherAccepted += t.otherAccepted; otherRejected += t.otherRejected; otherUncertain += t.otherUncertain;
        }

        public override string ToString()
        {
            CultureInfo inv = CultureInfo.InvariantCulture;
            double decided = Right + Errors;
            return "correct accepted " + taughtAccepted + "/" + Taught +
                   ", alternate rejected " + otherRejected + "/" + Other +
                   ", alternate ACCEPTED " + otherAccepted +
                   ", correct REJECTED " + taughtRejected +
                   ", uncertain " + Uncertain + "/" + Total +
                   ", accuracy " + (Total > 0 ? (100.0 * Right / Total).ToString("0", inv) : "-") + "% of all" +
                   " (" + (decided > 0 ? (100.0 * Right / decided).ToString("0", inv) : "-") + "% of decided)";
        }
    }

    public struct Outcome
    {
        public string speaker, word, file, condition;
        public bool taught;
        public float pLow, pMid, pHigh;
        public int measured;
        public bool allMeasured;
        public string reason;
    }

    /// <summary>Leave-one-speaker-out: every take judged by a model that never heard its speaker.</summary>
    public static List<Outcome> LeaveOneSpeakerOut(List<Take> takes, bool[] use, bool withConditions)
    {
        List<Outcome> outcomes = new List<Outcome>();
        List<string> speakers = takes.Select(t => t.speaker).Distinct().OrderBy(s => s, StringComparer.Ordinal).ToList();
        foreach (AmbiguousWords.Entry e in AmbiguousWords.All)
        {
            StressPattern pattern = StressPattern.For(e.word);
            foreach (string held in speakers)
            {
                List<Take> train = takes.Where(t => t.word == e.word && t.speaker != held).ToList();
                List<Take> test = takes.Where(t => t.word == e.word && t.speaker == held).ToList();
                if (train.Count == 0 || test.Count == 0) continue;
                int rows;
                PronunciationModel.Model m = Train(e.word, train, use, out rows);
                foreach (Take t in test)
                {
                    outcomes.Add(Judge(t, t.audio, "as recorded", pattern, m));
                    if (!withConditions) continue;
                    int seed = 501;
                    foreach (PronunciationReferenceBuilder.Condition c in PronunciationReferenceBuilder.Conditions)
                        outcomes.Add(Judge(t, PronunciationReferenceBuilder.Apply(t.audio, t.rate, c, seed++), c.name, pattern, m));
                }
            }
        }
        return outcomes;
    }

    private static Outcome Judge(Take t, float[] x, string condition, StressPattern pattern, PronunciationModel.Model m)
    {
        PronunciationModel.Result r = PronunciationModel.Assess(x, t.rate, pattern, m);
        return new Outcome
        {
            speaker = t.speaker, word = t.word, file = Path.GetFileName(t.file), condition = condition, taught = t.taught,
            pLow = r.pLow, pMid = r.pMid, pHigh = r.pHigh, measured = r.measured, allMeasured = r.measured == r.measurements, reason = r.reason,
        };
    }

    /// <summary>When true, VerdictAt uses the rule before 2026-11-22: all six measured, all six past the line.</summary>
    public static bool OldRule;

    public static PronunciationModel.Verdict VerdictAt(Outcome o, float accept, float reject)
    {
        if (OldRule)
        {
            if (!o.allMeasured || float.IsNaN(o.pLow)) return PronunciationModel.Verdict.Uncertain;
            if (o.pLow >= accept) return PronunciationModel.Verdict.Correct;
            if (o.pHigh <= reject) return PronunciationModel.Verdict.Incorrect;
            return PronunciationModel.Verdict.Uncertain;
        }
        return PronunciationModel.Decide(o.pLow, o.pMid, o.pHigh, o.measured, accept, reject);
    }

    public static Tally Count(IEnumerable<Outcome> outcomes, float accept, float reject)
    {
        Tally t = new Tally();
        foreach (Outcome o in outcomes) t.Add(o.taught, VerdictAt(o, accept, reject));
        return t;
    }

    // ---- Generating PronunciationModelData.cs -------------------------------------

    public static string Generate(List<PronunciationModel.Model> models, List<string> notes)
    {
        CultureInfo inv = CultureInfo.InvariantCulture;
        StringBuilder sb = new StringBuilder();
        sb.AppendLine("// GENERATED by PronunciationModelBuilder from Recordings/Dataset (see Tools/pronunciation_dataset).");
        sb.AppendLine("// Do not edit by hand: re-run Tools/test_stress_rules.sh dataset-train. See PronunciationModel.cs.");
        foreach (string n in notes) sb.AppendLine("// " + n);
        sb.AppendLine();
        sb.AppendLine("public static partial class PronunciationModel");
        sb.AppendLine("{");
        sb.AppendLine("    // word, features used (" + string.Join(" ", PronunciationModel.FeatureNames) + "), mean, scale, weight, bias,");
        sb.AppendLine("    // Correct from, Incorrect below, trained on, taught reference frames, other reference frames");
        sb.AppendLine("    private static readonly Row[] Rows =");
        sb.AppendLine("    {");
        foreach (PronunciationModel.Model m in models)
        {
            Func<float[], string> arr = a => "new[] { " + string.Join(", ", a.Select(v => v.ToString("0.0000", inv) + "f")) + " }";
            Func<List<PronunciationTemplate.Frames>, string> frames = l => l.Count == 0 ? "new string[0]"
                : "new[] {\n            " + string.Join(",\n            ", l.Select(f => "\"" + PronunciationTemplate.Encode(f) + "\"")) + " }";
            sb.AppendLine("        new Row(\"" + m.word + "\", \"" + string.Concat(m.use.Select(u => u ? "1" : "0")) + "\",");
            sb.AppendLine("            " + arr(m.mean) + ", " + arr(m.scale) + ", " + arr(m.weight) + ", " + m.bias.ToString("0.0000", inv) + "f,");
            sb.AppendLine("            " + m.acceptFrom.ToString("0.00", inv) + "f, " + m.rejectFrom.ToString("0.00", inv) + "f, \"" + m.trainedOn + "\",");
            sb.AppendLine("            " + frames(m.taughtRefs) + ",");
            sb.AppendLine("            " + frames(m.otherRefs) + "),");
        }
        sb.AppendLine("    };");
        sb.AppendLine("}");
        return sb.ToString();
    }
}
