// The recorded pronunciation dataset: evaluation and training of
// PronunciationModel (see Assets/Editor/PronunciationModelBuilder.cs).
//
//   Tools/test_stress_rules.sh dataset-eval  [DIR]   leave-one-speaker-out report, writes nothing
//   Tools/test_stress_rules.sh dataset-train [DIR]   the same report, then trains on every
//                                                    speaker and writes PronunciationModelData.cs
//
// DIR defaults to Recordings/Dataset (made by Tools/pronunciation_dataset/segment.py).

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;

internal static class DatasetTool
{
    private static readonly CultureInfo Inv = CultureInfo.InvariantCulture;

    public static int Run(string root, bool train, string[] args)
    {
        string dir = args.Length > 0 ? args[0] : PronunciationModelBuilder.DatasetDirectory(root);
        List<string> problems = new List<string>();
        List<PronunciationModelBuilder.Take> takes = PronunciationModelBuilder.Load(dir, problems);
        foreach (string p in problems) Console.WriteLine("  note: " + p);
        List<string> speakers = takes.Select(t => t.speaker).Distinct().OrderBy(s => s, StringComparer.Ordinal).ToList();
        if (speakers.Count < 2)
        {
            Console.WriteLine("Need recordings from at least 2 speakers in " + dir + " (found " + speakers.Count + ").");
            Console.WriteLine("Make them with: python Tools/pronunciation_dataset/segment.py SPEAKER_FILE.mp3");
            return 2;
        }
        Console.WriteLine("Dataset: " + takes.Count + " takes from " + speakers.Count + " speakers (" + string.Join(", ", speakers) + ")");
        foreach (AmbiguousWords.Entry e in AmbiguousWords.All)
        {
            int c = takes.Count(t => t.word == e.word && t.taught), a = takes.Count(t => t.word == e.word && !t.taught);
            if (c == 0 || a == 0) Console.WriteLine("  WARNING " + e.word + ": " + c + " correct, " + a + " alternate takes");
        }

        if (Environment.GetEnvironmentVariable("PM_ONLY_TIME") == "1") { Timing(takes); return 0; }
        if (Environment.GetEnvironmentVariable("PM_ONLY_CROP") == "1") { CropCheck(takes); return 0; }

        bool[] use = PronunciationModelBuilder.DefaultFeatures;
        string l2 = Environment.GetEnvironmentVariable("PM_L2");
        if (!string.IsNullOrEmpty(l2)) PronunciationModelBuilder.L2 = double.Parse(l2, Inv);
        Console.WriteLine("L2 penalty " + PronunciationModelBuilder.L2.ToString(Inv));
        bool quick = Environment.GetEnvironmentVariable("PM_QUICK") == "1";
        Console.WriteLine();
        Console.WriteLine("Feature sets, leave one speaker out, takes as recorded (Correct from " +
                          PronunciationModelBuilder.DefaultAccept.ToString("0.00", Inv) + ", Incorrect below " +
                          PronunciationModelBuilder.DefaultReject.ToString("0.00", Inv) + "):");
        var sets = new List<KeyValuePair<string, bool[]>>
        {
            new KeyValuePair<string, bool[]>("length+pitch+loudness", new[] { true, true, true, false, false }),
            new KeyValuePair<string, bool[]>("+fall", new[] { true, true, true, true, false }),
            new KeyValuePair<string, bool[]>("template only", new[] { false, false, false, false, true }),
            new KeyValuePair<string, bool[]>("all five (used)", use),
        };
        foreach (var set in sets)
        {
            if (quick) break;
            var o = PronunciationModelBuilder.LeaveOneSpeakerOut(takes, set.Value, false);
            Console.WriteLine("  " + set.Key.PadRight(22) + PronunciationModelBuilder.Count(o, PronunciationModelBuilder.DefaultAccept, PronunciationModelBuilder.DefaultReject));
        }

        Console.WriteLine();
        string only = Environment.GetEnvironmentVariable("PM_ONLY_SPLIT");
        if (!string.IsNullOrEmpty(only)) { SplitHalf(takes, only, use); return 0; }

        Console.WriteLine("Leave one speaker out with the features used, every take also under 12 simulated phones/voices:");
        List<PronunciationModelBuilder.Outcome> all = PronunciationModelBuilder.LeaveOneSpeakerOut(takes, use, true);
        List<PronunciationModelBuilder.Outcome> clean = all.Where(o => o.condition == "as recorded").ToList();
        List<PronunciationModelBuilder.Outcome> simulated = all.Where(o => o.condition != "as recorded").ToList();
        float acc = PronunciationModelBuilder.DefaultAccept, rej = PronunciationModelBuilder.DefaultReject;

        Console.WriteLine("  Per word (as recorded | simulated):");
        PronunciationModelBuilder.Tally totalClean = new PronunciationModelBuilder.Tally();
        foreach (AmbiguousWords.Entry e in AmbiguousWords.All)
        {
            var tc = PronunciationModelBuilder.Count(clean.Where(o => o.word == e.word), acc, rej);
            var ts = PronunciationModelBuilder.Count(simulated.Where(o => o.word == e.word), acc, rej);
            totalClean.Add(tc);
            Console.WriteLine("    " + e.word.PadRight(6) + " " + tc);
            Console.WriteLine("    " + "".PadRight(6) + " " + ts);
        }
        Console.WriteLine("  Per held-out speaker (as recorded):");
        foreach (string s in speakers)
            Console.WriteLine("    " + s.PadRight(6) + " " + PronunciationModelBuilder.Count(clean.Where(o => o.speaker == s), acc, rej));
        Console.WriteLine("  ALL as recorded:  " + totalClean);
        Console.WriteLine("  ALL simulated:    " + PronunciationModelBuilder.Count(simulated, acc, rej));
        int flips = all.GroupBy(o => o.speaker + "/" + o.word + "/" + o.file)
                       .Count(g => g.Any(o => PronunciationModelBuilder.VerdictAt(o, acc, rej) == PronunciationModel.Verdict.Correct) &&
                                   g.Any(o => PronunciationModelBuilder.VerdictAt(o, acc, rej) == PronunciationModel.Verdict.Incorrect));
        Console.WriteLine("  Takes accepted under one condition and rejected under another: " + flips);

        Console.WriteLine();
        Console.WriteLine("Rules and confidence lines, held-out takes as recorded (left) and simulated (right):");
        foreach (bool old in new[] { true, false })
        {
            PronunciationModelBuilder.OldRule = old;
            Console.WriteLine(old ? "  OLD rule (all six measured, all six past the line):" : "  NEW rule (median p, at least 4 of 6 measured, none past the other line):");
            foreach (float[] line in new[] { new[] { 0.7f, 0.3f }, new[] { 0.6f, 0.3f }, new[] { 0.55f, 0.3f }, new[] { 0.6f, 0.25f }, new[] { 0.5f, 0.25f } })
            {
                Console.WriteLine("    Correct from " + line[0].ToString("0.00", Inv) + ", Incorrect below " + line[1].ToString("0.00", Inv) + ":");
                foreach (string s in speakers)
                    Console.WriteLine("      " + s.PadRight(6) + PronunciationModelBuilder.Count(clean.Where(o => o.speaker == s), line[0], line[1]));
                Console.WriteLine("      ALL   " + PronunciationModelBuilder.Count(clean, line[0], line[1]));
                Console.WriteLine("      sim   " + PronunciationModelBuilder.Count(simulated, line[0], line[1]));
            }
        }
        PronunciationModelBuilder.OldRule = false;

        Console.WriteLine();
        Console.WriteLine("Every held-out take as recorded (p = probability of the taught pronunciation):");
        foreach (var o in clean)
        {
            var v = PronunciationModelBuilder.VerdictAt(o, acc, rej);
            bool error = (o.taught && v == PronunciationModel.Verdict.Incorrect) || (!o.taught && v == PronunciationModel.Verdict.Correct);
            Console.WriteLine("  " + (error ? "ERROR " : "      ") + o.speaker.PadRight(5) + " " + o.file.PadRight(22) + " -> " +
                              v.ToString().PadRight(9) + " p " + Fmt(o.pMid) + " (" + Fmt(o.pLow) + ".." + Fmt(o.pHigh) + ", " + o.measured + "/6)");
        }

        string split = Environment.GetEnvironmentVariable("PM_SPLIT");
        if (!string.IsNullOrEmpty(split)) SplitHalf(takes, split, use);
        if (quick && !string.IsNullOrEmpty(split) && !train) return 0;

        Integrated(takes, speakers, use, true);
        Integrated(takes, speakers, use, false);

        if (!train) return 0;

        Console.WriteLine();
        List<PronunciationModel.Model> models = new List<PronunciationModel.Model>();
        foreach (AmbiguousWords.Entry e in AmbiguousWords.All)
        {
            List<PronunciationModelBuilder.Take> w = takes.Where(t => t.word == e.word).ToList();
            if (!w.Any(t => t.taught) || !w.Any(t => !t.taught)) { Console.WriteLine("  " + e.word + ": not trained (needs both classes)"); continue; }
            int rows;
            PronunciationModel.Model m = PronunciationModelBuilder.Train(e.word, w, use, out rows);
            models.Add(m);
            Console.WriteLine("  " + e.word.PadRight(6) + " trained on " + m.trainedOn + " (" + rows + " rows with simulated conditions); weights " +
                              string.Join(" ", Enumerable.Range(0, PronunciationModel.FeatureCount)
                                  .Select(i => PronunciationModel.FeatureNames[i] + "=" + (m.use[i] ? m.weight[i].ToString("0.00", Inv) : "off"))));
        }
        List<string> notes = new List<string>
        {
            "Trained " + DateTime.Now.ToString("yyyy-MM-dd", Inv) + " on " + takes.Count + " takes from " + speakers.Count + " speakers (" + string.Join(", ", speakers) + ").",
            "Leave-one-speaker-out, as recorded: " + totalClean,
            "Leave-one-speaker-out, simulated phones/voices: " + PronunciationModelBuilder.Count(simulated, acc, rej),
        };
        string file = Path.Combine(root, "Assets", "Scripts", "PronunciationModelData.cs");
        File.WriteAllText(file, PronunciationModelBuilder.Generate(models, notes));
        Console.WriteLine("wrote " + file + " (" + (new FileInfo(file).Length / 1024) + " KB)");
        return 0;
    }

    /// <summary>
    /// The whole game check (AnswerJudge, balanced settings, the letters
    /// already right) on every held-out take, with the classifier trained
    /// without that take's speaker - what a new child would meet.
    /// </summary>
    private static void Integrated(List<PronunciationModelBuilder.Take> takes, List<string> speakers, bool[] use, bool withModel)
    {
        Console.WriteLine();
        Console.WriteLine(withModel ? "The game's answer check (AnswerJudge: model + safety net), leave one speaker out:"
                                    : "BASELINE - the earlier Rachel-only hybrid check (AnswerJudge, model off), same takes:");
        AnswerJudge.useLearnedModel = withModel;
        int[,] clean = new int[2, 3], sim = new int[2, 3];   // [taught?, Correct/WrongStress/Unclear]
        int vetoed = 0;
        List<string> errors = new List<string>();
        foreach (string held in speakers)
        {
            foreach (AmbiguousWords.Entry e in AmbiguousWords.All)
            {
                if (!withModel) break;
                List<PronunciationModelBuilder.Take> train = takes.Where(t => t.word == e.word && t.speaker != held).ToList();
                int rows;
                PronunciationModel.Override(e.word, PronunciationModelBuilder.Train(e.word, train, use, out rows));
            }
            foreach (PronunciationModelBuilder.Take t in takes.Where(t => t.speaker == held))
            {
                List<float[]> versions = new List<float[]> { t.audio };
                int seed = 901;
                foreach (PronunciationReferenceBuilder.Condition c in PronunciationReferenceBuilder.Conditions)
                    versions.Add(PronunciationReferenceBuilder.Apply(t.audio, t.rate, c, seed++));
                for (int k = 0; k < versions.Count; k++)
                {
                    AnswerJudge.Evidence ev = new AnswerJudge.Evidence { audio = versions[k], audioSampleRate = t.rate };
                    AnswerJudge.Result r = AnswerJudge.Judge(t.word.ToLowerInvariant(), t.word, null, ev, AnswerJudge.Settings.Default);
                    int o = r.outcome == AnswerJudge.Outcome.Correct ? 0 : r.outcome == AnswerJudge.Outcome.WrongStress ? 1 : 2;
                    if (r.outcome == AnswerJudge.Outcome.Wrong) o = 2;
                    (k == 0 ? clean : sim)[t.taught ? 1 : 0, o]++;
                    if (r.outcome == AnswerJudge.Outcome.Unclear && r.model.verdict != PronunciationModel.Verdict.Uncertain) vetoed++;
                    bool error = (t.taught && o == 1) || (!t.taught && o == 0);
                    if (error && k == 0) errors.Add(t.speaker + "/" + Path.GetFileName(t.file) + " -> " + r.outcome + ": " + r.reason);
                }
            }
        }
        PronunciationModel.Reload();
        AnswerJudge.useLearnedModel = true;
        Console.WriteLine("  as recorded: " + Line(clean));
        Console.WriteLine("  simulated:   " + Line(sim));
        if (withModel) Console.WriteLine("  model verdicts turned into 'ask again' by the safety net: " + vetoed);
        foreach (string e in errors) Console.WriteLine("  ERROR " + e);
    }

    private static string Line(int[,] c)
    {
        int taught = c[1, 0] + c[1, 1] + c[1, 2], other = c[0, 0] + c[0, 1] + c[0, 2], all = taught + other;
        int right = c[1, 0] + c[0, 1];
        return "correct accepted " + c[1, 0] + "/" + taught + ", alternate rejected " + c[0, 1] + "/" + other +
               ", alternate ACCEPTED " + c[0, 0] + ", correct REJECTED " + c[1, 1] + ", asked again " + (c[1, 2] + c[0, 2]) + "/" + all +
               ", accuracy " + (all > 0 ? (100.0 * right / all).ToString("0", Inv) : "-") + "%";
    }

    /// <summary>
    /// One speaker's takes split in two: train on every other speaker plus
    /// half of this speaker's takes, test on the other half, then swap. How
    /// much a few of a speaker's own natural takes help with the rest.
    /// </summary>
    private static void SplitHalf(List<PronunciationModelBuilder.Take> takes, string speaker, bool[] use)
    {
        Console.WriteLine();
        Console.WriteLine("Speaker " + speaker + " split in half (train on the other speakers + one half, test on the other half):");
        var outcomes = new List<PronunciationModelBuilder.Outcome>();
        foreach (AmbiguousWords.Entry e in AmbiguousWords.All)
        {
            List<PronunciationModelBuilder.Take> mine = takes.Where(t => t.word == e.word && t.speaker == speaker)
                                                             .OrderBy(t => t.taught).ThenBy(t => t.file, StringComparer.Ordinal).ToList();
            if (mine.Count < 2) continue;
            StressPattern pattern = StressPattern.For(e.word);
            for (int fold = 0; fold < 2; fold++)
            {
                List<PronunciationModelBuilder.Take> test = mine.Where((t, i) => i % 2 == fold).ToList();
                List<PronunciationModelBuilder.Take> train = takes.Where(t => t.word == e.word && t.speaker != speaker).ToList();
                train.AddRange(mine.Where((t, i) => i % 2 != fold));
                int rows;
                PronunciationModel.Model m = PronunciationModelBuilder.Train(e.word, train, use, out rows);
                foreach (var t in test)
                {
                    PronunciationModel.Result r = PronunciationModel.Assess(t.audio, t.rate, pattern, m);
                    outcomes.Add(new PronunciationModelBuilder.Outcome
                    {
                        speaker = t.speaker, word = t.word, file = Path.GetFileName(t.file), condition = "as recorded", taught = t.taught,
                        pLow = r.pLow, pMid = r.pMid, pHigh = r.pHigh, measured = r.measured, allMeasured = r.measured == r.measurements, reason = r.reason,
                    });
                }
            }
        }
        foreach (float[] line in new[] { new[] { 0.7f, 0.3f }, new[] { 0.6f, 0.3f }, new[] { 0.55f, 0.3f } })
        {
            Console.WriteLine("  Correct from " + line[0].ToString("0.00", Inv) + ", Incorrect below " + line[1].ToString("0.00", Inv) + ":");
            foreach (AmbiguousWords.Entry e in AmbiguousWords.All)
                Console.WriteLine("    " + e.word.PadRight(6) + PronunciationModelBuilder.Count(outcomes.Where(o => o.word == e.word), line[0], line[1]));
            Console.WriteLine("    ALL   " + PronunciationModelBuilder.Count(outcomes, line[0], line[1]));
        }
        foreach (var o in outcomes)
            Console.WriteLine("    " + o.file.PadRight(22) + " -> " + PronunciationModelBuilder.VerdictAt(o, 0.6f, 0.3f).ToString().PadRight(9) +
                              " p " + Fmt(o.pMid) + " (" + Fmt(o.pLow) + ".." + Fmt(o.pHigh) + ", " + o.measured + "/6)");
    }

    /// <summary>How long each part of the check takes on one answer padded to 8 s, as the phone records it.</summary>
    private static void Timing(List<PronunciationModelBuilder.Take> takes)
    {
        Console.WriteLine();
        Console.WriteLine("Time to judge one answer (8 s of audio, this PC; a budget phone is several times slower):");
        foreach (AmbiguousWords.Entry e in AmbiguousWords.All)
        {
            var t = takes.First(x => x.word == e.word && x.taught);
            float[] x8 = new float[t.rate * 8];
            Random rng = new Random(1);
            for (int i = 0; i < x8.Length; i++) x8[i] = (float)((rng.NextDouble() - 0.5) * 0.002);
            Array.Copy(t.audio, 0, x8, t.rate, Math.Min(t.audio.Length, x8.Length - t.rate));
            StressPattern p = StressPattern.For(e.word);
            var sw = System.Diagnostics.Stopwatch.StartNew();
            PronunciationModel.Assess(x8, t.rate, e.word); long model = sw.ElapsedMilliseconds; sw.Restart();
            PronunciationTemplate.Assess(x8, t.rate, e.word); long template = sw.ElapsedMilliseconds; sw.Restart();
            PronunciationAssessor.Assess(x8, t.rate, p); long assessor = sw.ElapsedMilliseconds; sw.Restart();
            RawStressAnalyzer.Judge(x8, t.rate, p, 1f); long raw = sw.ElapsedMilliseconds; sw.Restart();
            AnswerJudge.Judge(e.word.ToLowerInvariant(), e.word, null, new AnswerJudge.Evidence { audio = x8, audioSampleRate = t.rate }, AnswerJudge.Settings.Default);
            long all = sw.ElapsedMilliseconds;
            var m = PronunciationModel.For(e.word);
            Console.WriteLine("  " + e.word.PadRight(6) + " whole check " + all + " ms  (model " + model + ", template " + template +
                              ", syllables " + assessor + ", second " + raw + ")  reference takes " + (m.taughtRefs.Count + m.otherRefs.Count));
        }
    }

    /// <summary>
    /// Every take judged as recorded and inside 8 s of quiet (as the phone's
    /// audio bridge delivers it, which AnswerJudge then crops to the word):
    /// the verdicts should be the same.
    /// </summary>
    private static void CropCheck(List<PronunciationModelBuilder.Take> takes)
    {
        int same = 0, differ = 0;
        foreach (var t in takes)
        {
            float[] x8 = new float[t.rate * 8];
            Random rng = new Random(3);
            for (int i = 0; i < x8.Length; i++) x8[i] = (float)((rng.NextDouble() - 0.5) * 0.002);
            Array.Copy(t.audio, 0, x8, t.rate * 2, Math.Min(t.audio.Length, x8.Length - t.rate * 2));
            var a = AnswerJudge.Judge(t.word.ToLowerInvariant(), t.word, null, new AnswerJudge.Evidence { audio = t.audio, audioSampleRate = t.rate }, AnswerJudge.Settings.Default);
            var b = AnswerJudge.Judge(t.word.ToLowerInvariant(), t.word, null, new AnswerJudge.Evidence { audio = x8, audioSampleRate = t.rate }, AnswerJudge.Settings.Default);
            if (a.outcome == b.outcome) same++;
            else { differ++; Console.WriteLine("  differs: " + t.speaker + "/" + Path.GetFileName(t.file) + "  as recorded " + a.outcome + ", in 8 s " + b.outcome); }
        }
        Console.WriteLine("Crop check: " + same + " same, " + differ + " different");
    }

    private static string Fmt(float v) { return float.IsNaN(v) ? " -  " : v.ToString("0.00", Inv); }
}
