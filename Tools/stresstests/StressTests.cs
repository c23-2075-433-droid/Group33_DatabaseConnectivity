// Tests for SALINLAHI's stress-aware answer checking (tayô "stand up" against
// táyo "we", and every other pair WordStress marks).
//
// These compile the GAME'S OWN files - PronunciationChecker, WordVariants,
// WordStress, StressPattern, StressAnalyzer, RawStressAnalyzer, both
// reference tables, AnswerJudge and the editor's WavLevelStream /
// StressReferenceBuilder - not copies of them, and run without Unity.
// Run with Tools/test_stress_rules.sh.
//
//   generate  re-measures Assets/Audio/VO and rewrites all three reference files
//   debug     prints what the raw-audio analyzer hears in each recording
//   labelled  judges real recordings sorted by what was said (see Labelled)
//   (none)    runs the tests; exit status 0 means every test passed
//
// WHAT THE AUDIO TESTS USE
//
// Teacher Rachel's real recordings. The raw-audio path hears them as a phone
// microphone might - room noise at three levels, and the voice raised the way
// a child's is. The loudness-reading path hears them as a recognizer's level
// callback would, at three report rates and three loudnesses.
//
// For the WRONG stress there is no recording of anyone saying táyo, so it is
// made from her own tayô (StressReferenceBuilder.MoveStress): the first
// syllable held longer, louder and higher, the last shortened, softened and
// lowered - how Tagalog moves stress. The cut-off is calibrated on that full
// change, so the tests also try what it was NOT calibrated on: half the
// change, the length change alone, the pitch change alone, and tayô said
// faster and slower.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

internal static class StressTests
{
    private static int failed;
    private static int passed;
    private static string vo;

    private static void Check(string name, bool ok, string detail = "")
    {
        if (ok) { passed++; Console.WriteLine("  PASS  " + name); }
        else { failed++; Console.WriteLine("  FAIL  " + name + (detail.Length > 0 ? "  -> " + detail : "")); }
    }

    private static int Main(string[] args)
    {
        string root = args.Length > 1 ? args[1] : Directory.GetCurrentDirectory();
        vo = Path.Combine(root, "Assets", "Audio", "VO");
        string mode = args.Length > 0 ? args[0] : "test";

        if (mode == "generate") return Generate(root);
        if (mode == "debug") return Debug();
        if (mode == "file") return Files(args.Skip(2).ToArray());
        if (mode == "balanced") { BalancedTests(); Console.WriteLine(passed + " passed, " + failed + " failed"); return failed == 0 ? 0 : 1; }
        if (mode == "strict") { StrictEveryWord(); Console.WriteLine(passed + " passed, " + failed + " failed"); return failed == 0 ? 0 : 1; }
        if (mode == "labelled") return Labelled(args.Length > 2 ? args[2] : Path.Combine(root, "Recordings", "Labelled"));

        PatternTests();
        ContextTests();
        ReferencesAreCurrent(root);
        RawAudioTests();
        LevelTests();
        RequiredCases();
        StrictEveryWord();
        BandedLevels();
        BalancedTests();
        Informational();

        Console.WriteLine();
        Console.WriteLine(passed + " passed, " + failed + " failed");
        return failed == 0 ? 0 : 1;
    }

    private static int Generate(string root)
    {
        StressReferenceBuilder.Result r = StressReferenceBuilder.Build(vo);
        string levelFile = Path.Combine(root, "Assets", "Scripts", "StressReferenceData.cs");
        File.WriteAllText(levelFile, StressReferenceBuilder.Generate(r));
        Console.WriteLine("wrote " + levelFile);
        Print(r);
        Console.WriteLine("  average first-syllable " + r.averageInitial.ToString("0.000") +
                          ", last-syllable " + r.averageFinal.ToString("0.000"));

        StressReferenceBuilder.Result raw = StressReferenceBuilder.BuildRaw(vo);
        string rawFile = Path.Combine(root, "Assets", "Scripts", "RawStressReferenceData.cs");
        File.WriteAllText(rawFile, StressReferenceBuilder.GenerateRaw(raw));
        Console.WriteLine("wrote " + rawFile);
        Print(raw);

        PronunciationReferenceBuilder.Result pr = PronunciationReferenceBuilder.Build(vo);
        string prFile = Path.Combine(root, "Assets", "Scripts", "PronunciationReferenceData.cs");
        File.WriteAllText(prFile, PronunciationReferenceBuilder.Generate(pr));
        Console.WriteLine("wrote " + prFile);
        foreach (var row in pr.rows)
            Console.WriteLine("  " + row.Key.PadRight(8) + " Rachel correct " + row.Value.ownCorrect + "/" + row.Value.conditions +
                              "  moved caught " + row.Value.movedWrong + "  moved accepted " + row.Value.movedAccepted +
                              "  half accepted " + row.Value.halfAccepted + (row.Value.entry.trusted ? "  TRUSTED" : ""));
        if (pr.missing.Count > 0) Console.WriteLine("  no recording: " + string.Join(", ", pr.missing));
        if (pr.unmeasurable.Count > 0) Console.WriteLine("  syllables not found in Rachel's recording: " + string.Join(", ", pr.unmeasurable));
        return 0;
    }

    private static void Print(StressReferenceBuilder.Result r)
    {
        foreach (var row in r.rows)
            Console.WriteLine("  " + row.Key.PadRight(6) + " Rachel " + row.Value.reference.ToString("0.000") +
                              "  moved " + row.Value.moved.ToString("0.000") +
                              "  own ok " + row.Value.ownCorrect + "/" + row.Value.settings +
                              "  moved rejected " + row.Value.movedRejected + "/" + row.Value.settings +
                              (row.Value.enforced ? "  ENFORCED" : "  measured only"));
        if (r.missing.Count > 0) Console.WriteLine("  no recording: " + string.Join(", ", r.missing));
    }

    /// <summary>Measures WAV files as answers to a word: file WORD a.wav b.wav ...</summary>
    private static int Files(string[] rest)
    {
        if (rest.Length < 2) { Console.WriteLine("usage: file WORD a.wav [b.wav ...]"); return 2; }
        StressPattern p = StressPattern.For(rest[0]);
        if (p == null) { Console.WriteLine(rest[0] + " is not a stress-pair word"); return 2; }
        foreach (string path in rest.Skip(1))
        {
            float[] x = WavLevelStream.ReadMono(path, out int rate);
            var m = RawStressAnalyzer.Measure(x, rate, p.SyllableCount);
            var j = RawStressAnalyzer.Judge(x, rate, p);
            var strict = AnswerJudge.Judge(p.Word.ToLowerInvariant(), p.Word, null, new AnswerJudge.Evidence { audio = x, audioSampleRate = rate }, AnswerJudge.Settings.StrictOnTrustedWords);
            var bal = AnswerJudge.Judge(p.Word.ToLowerInvariant(), p.Word, null, new AnswerJudge.Evidence { audio = x, audioSampleRate = rate }, AnswerJudge.Settings.Default);
            Console.WriteLine(Path.GetFileName(path) + "  BALANCED -> " + bal.outcome + "   strict -> " + strict.outcome + "  " + strict.reason);
            Console.WriteLine("    old analyzer alone: " + (x.Length / (float)rate).ToString("0.00") + " s  -> " + j.verdict +
                (m.ok ? "  score " + m.ScoreFor(p.StressedSyllable).ToString("0.00") + " vs line " + j.threshold.ToString("0.00") +
                        "  " + RawStressAnalyzer.Describe(m)
                      : "  (" + m.whyNot + ")"));
        }
        return 0;
    }

    private static int Debug()
    {
        {
            StressPattern lp = StressPattern.For("Lakad");
            float[] la = Load("lakad", out int lr);
            foreach (var (label, audio) in new[] { ("own", la), ("moved", MoveRaw(la, lr, lp, 1f)) })
                foreach (float noise in StressReferenceBuilder.RawNoiseDb)
                    foreach (float raise in StressReferenceBuilder.RawVoiceRaise)
                    {
                        var m = RawStressAnalyzer.Measure(StressReferenceBuilder.RawCondition(audio, lr, noise, raise), lr, 2);
                        Console.WriteLine("lakad " + label + " " + noise + " x" + raise + ": " +
                            (m.ok ? m.ScoreFor(1).ToString("0.00") + " " + RawStressAnalyzer.Describe(m) : m.whyNot));
                    }
        }
        foreach (string word in WordStress.WordsWithStress.OrderBy(w => w))
        {
            float[] a = Load(word, out int rate);
            StressPattern p = StressPattern.For(word);
            foreach (var (label, audio) in new[] { ("own  ", a), ("moved", MoveRaw(a, rate, p, 1f)) })
            {
                var m = RawStressAnalyzer.Measure(StressReferenceBuilder.RawCondition(audio, rate, -60f, 1f), rate);
                Console.WriteLine(p.Spoken.PadRight(8) + label + (m.ok
                    ? " score " + m.score.ToString("0.00") + "  len " + m.first.lengthMs + "/" + m.second.lengthMs +
                      "  dB " + m.first.peakDb.ToString("0.0") + "/" + m.second.peakDb.ToString("0.0") +
                      "  Hz " + m.first.pitchHz.ToString("0") + "/" + m.second.pitchHz.ToString("0")
                    : " -- " + m.whyNot));
            }
        }
        return 0;
    }

    // ---- Stress patterns read off WordStress -------------------------------

    private static void PatternTests()
    {
        Console.WriteLine("Stress patterns (from WordStress marks)");
        Expect("Tayo", 2, 1, "ta-YÔ");
        Expect("Upo", 2, 1, "u-PÔ");
        Expect("Bukas", 2, 1, "bu-KÁS");
        Expect("Talon", 2, 1, "ta-LÓN");
        Expect("Basa", 2, 0, "BÁ-sa");
        Expect("Bati", 2, 0, "BÁ-tì");
        Expect("Bangon", 2, 0, "BÁ-ngon");
        Expect("Sapatos", 3, 1, "sa-PÁ-tos");
        Expect("Ibahagi", 4, 2, "i-ba-HÁ-gì");
        Expect("Paalam", 3, 1, "pa-Á-lam");
        Check("one-syllable Bag has no stress check", StressPattern.For("Bag") == null);
        Check("Kinig (stress not decided) has no stress check", StressPattern.For("Kinig") == null);
        Check("Tayo and Lakad are pairs, Bangon is not",
              StressPattern.For("Tayo").IsMinimalPair && StressPattern.For("Lakad").IsMinimalPair && !StressPattern.For("Bangon").IsMinimalPair);
        foreach (string w in WordStress.WordsWithStress)
            if (StressPattern.For(w) == null) Check(w + " has a readable stress", false);
        Check("lookup ignores case and spaces", StressPattern.For("  tAyO ") != null);
        StressPattern three = StressPattern.Parse("Sapatos", "Sapátos");
        Check("generic: Sapátos parses as 3 syllables, stress on 2nd",
              three != null && three.SyllableCount == 3 && three.StressedSyllable == 1, three == null ? "null" : three.Spoken);
        StressPattern malumay = StressPattern.Parse("Kanan", "Kanan");
        Check("generic: no mark means second-to-last syllable", malumay != null && malumay.StressedSyllable == 0);
    }

    private static void Expect(string word, int syllables, int stressed, string spoken)
    {
        StressPattern p = StressPattern.For(word);
        Check(word + " -> " + spoken,
              p != null && p.SyllableCount == syllables && p.StressedSyllable == stressed && p.Spoken == spoken,
              p == null ? "null" : p.SyllableCount + " syllables, stressed " + p.StressedSyllable + ", " + p.Spoken);
    }

    // ---- How the word sat in the sentence ----------------------------------

    private static void ContextTests()
    {
        Console.WriteLine("Word context");
        Ctx("tayo", "Tayo", PronunciationChecker.WordContext.Alone);
        Ctx("Tayo!", "Tayo", PronunciationChecker.WordContext.Alone);
        Ctx("ta yo", "Tayo", PronunciationChecker.WordContext.Alone);
        Ctx("tayo po", "Tayo", PronunciationChecker.WordContext.WithFiller);
        Ctx("kain tayo", "Tayo", PronunciationChecker.WordContext.InSentence);
        Ctx("tara tayo", "Tayo", PronunciationChecker.WordContext.InSentence);
        Ctx("tayong", "Tayo", PronunciationChecker.WordContext.NotSaid);
        Ctx("bangon", "Tayo", PronunciationChecker.WordContext.NotSaid);
        Ctx("bukas na", "Bukas", PronunciationChecker.WordContext.WithFiller);
    }

    private static void Ctx(string said, string word, PronunciationChecker.WordContext expected)
    {
        var got = PronunciationChecker.ContextOf(said, word);
        Check("\"" + said + "\" for " + word + " is " + expected, got == expected, "got " + got);
    }

    // ---- Generated data is in step with the recordings ---------------------

    private static void ReferencesAreCurrent(string root)
    {
        Console.WriteLine("Generated references");
        string level = StressReferenceBuilder.Generate(StressReferenceBuilder.Build(vo));
        string raw = StressReferenceBuilder.GenerateRaw(StressReferenceBuilder.BuildRaw(vo));
        Check("StressReferenceData.cs matches the recordings (else run: generate)",
              Normalize(level) == Normalize(File.ReadAllText(Path.Combine(root, "Assets", "Scripts", "StressReferenceData.cs"))));
        Check("RawStressReferenceData.cs matches the recordings (else run: generate)",
              Normalize(raw) == Normalize(File.ReadAllText(Path.Combine(root, "Assets", "Scripts", "RawStressReferenceData.cs"))));
        Check("Tayo is enforced from raw audio", RawStressReferences.For(StressPattern.For("Tayo")).enforced);
        string pron = PronunciationReferenceBuilder.Generate(PronunciationReferenceBuilder.Build(vo));
        Check("PronunciationReferenceData.cs matches the recordings (else run: generate)",
              Normalize(pron) == Normalize(File.ReadAllText(Path.Combine(root, "Assets", "Scripts", "PronunciationReferenceData.cs"))));
        Check("Tayo's pronunciation reference is trusted", PronunciationReferences.For("Tayo").trusted);
        Console.WriteLine("  pronunciation references trusted: " + string.Join(", ", WordStress.WordsWithStress.Where(w => PronunciationReferences.For(w).trusted)));
        Console.WriteLine("  raw audio enforced: " + string.Join(", ", WordStress.WordsWithStress.Where(w => RawStressReferences.For(StressPattern.For(w)).enforced)) +
                          "; loudness readings enforced: " + string.Join(", ", WordStress.WordsWithStress.Where(w => StressReferences.For(StressPattern.For(w)).enforced)));
    }

    private static string Normalize(string s) { return s.Replace("\r\n", "\n").Trim(); }

    // ---- Raw audio: the primary check ---------------------------------------

    private static float[] MoveRaw(float[] x, int sr, StressPattern p, float strength,
                                   bool lengthOnly = false, bool pitchOnly = false)
    {
        // Only the pitch moves: lengths and loudness untouched.
        if (pitchOnly) return PitchOnly(x, sr, p.IsFinal, StressReferenceBuilder.MovedPitchSemitones * strength);

        return StressReferenceBuilder.MoveStressRaw(x, sr, p, strength, lengthAndLoudnessOnly: lengthOnly);
    }

    private static float[] PitchOnly(float[] x, int sr, bool toFirst, float semis)
    {
        RawStressAnalyzer.Measurement m = RawStressAnalyzer.Measure(x, sr);
        int split = m.ok ? Math.Min(x.Length, (int)(m.splitSeconds * sr)) : x.Length / 2;
        float[] a = x.Take(split).ToArray(), b = x.Skip(split).ToArray();
        if (toFirst) { a = StressReferenceBuilder.PitchShift(a, sr, semis); b = StressReferenceBuilder.PitchShift(b, sr, -semis); }
        else { a = StressReferenceBuilder.PitchShift(a, sr, -semis); b = StressReferenceBuilder.PitchShift(b, sr, semis); }
        return a.Concat(b).ToArray();
    }

    /// <summary>How many of the 9 raw conditions give the expected verdict.</summary>
    private static int RawScore(float[] audio, int sr, StressPattern p, StressAnalyzer.Verdict expected, out string misses)
    {
        int ok = 0;
        misses = "";
        foreach (float noise in StressReferenceBuilder.RawNoiseDb)
            foreach (float raise in StressReferenceBuilder.RawVoiceRaise)
            {
                var j = RawStressAnalyzer.Judge(StressReferenceBuilder.RawCondition(audio, sr, noise, raise, 99), sr, p);
                if (j.verdict == expected) ok++;
                else misses += " [noise " + noise + "dB voice x" + raise + ": " + j.measured.ToString("0.00") +
                               " vs cut " + j.threshold.ToString("0.00") + "]";
            }
        return ok;
    }

    private static void RawAudioTests()
    {
        Console.WriteLine("Raw audio - every ENFORCED word in Bahay, Paaralan and Parke, under 9 microphone conditions");
        foreach (string word in WordStress.WordsWithStress.OrderBy(w => w))
        {
            StressPattern p = StressPattern.For(word);
            if (!RawStressReferences.For(p).enforced) continue;
            float[] audio = Load(word, out int sr);

            int own = RawScore(audio, sr, p, StressAnalyzer.Verdict.Matches, out string m0);
            Check(p.Spoken + ": Rachel's own -> correct on " + own + "/9", own == 9, m0);
            int accepted = RawScore(MoveRaw(audio, sr, p, 1f), sr, p, StressAnalyzer.Verdict.Matches, out _);
            Check(p.Spoken + ": stress moved -> never accepted (accepted on " + accepted + "/9)", accepted <= 1);
        }

        Console.WriteLine("Raw audio - Tayo, on changes the cut-off was NOT calibrated on");
        StressPattern tp = StressPattern.For("Tayo");
        float[] tayo = Load("tayo", out int rate);
        // Reported, not pass/fail: these are deliberately weaker than a real
        // táyo, and they show where the line sits rather than whether it works.
        int half = RawScore(MoveRaw(tayo, rate, tp, 0.5f), rate, tp, StressAnalyzer.Verdict.WrongStress, out _);
        int len = RawScore(MoveRaw(tayo, rate, tp, 1f, lengthOnly: true), rate, tp, StressAnalyzer.Verdict.WrongStress, out _);
        int pitch = RawScore(MoveRaw(tayo, rate, tp, 1f, pitchOnly: true), rate, tp, StressAnalyzer.Verdict.WrongStress, out _);
        Console.WriteLine("  (for information) wrong on: half the stress change " + half + "/9, length+loudness only " +
                          len + "/9, pitch only " + pitch + "/9");
        foreach (float tempo in new[] { 0.8f, 1.25f })
        {
            int ok = RawScore(StressReferenceBuilder.Stretch(tayo, rate, tempo), rate, tp, StressAnalyzer.Verdict.Matches, out string t);
            Check("tayô said at " + tempo + "x length -> correct on " + ok + "/9", ok >= 8, t);
        }
        // Far from the phone in a noisy room the word may be too faint to
        // judge; that must come back as "say it again", never as wrong.
        int quietWrong = 9 - RawScore(tayo.Select(v => v * 0.05f).ToArray(), rate, tp, StressAnalyzer.Verdict.WrongStress, out _);
        int quietOk = RawScore(tayo.Select(v => v * 0.05f).ToArray(), rate, tp, StressAnalyzer.Verdict.Matches, out _);
        Check("tayô 26 dB quieter (far from the phone) is never marked wrong (correct on " + quietOk + "/9, rest unclear)",
              quietWrong == 9);
    }

    // ---- Loudness readings: the fallback ------------------------------------

    private static readonly float[] Hops = { 20f, 50f, 100f };
    private static readonly float[] Peaks = { 6f, 9f, 12f };

    private static int LevelScore(float[] audio, int sr, StressPattern p, StressAnalyzer.Verdict expected)
    {
        int ok = 0;
        foreach (float hop in Hops)
            foreach (float peak in Peaks)
                if (StressAnalyzer.Judge(WavLevelStream.FromSamples(audio, sr, hop, peak), hop,
                                         StressAnalyzer.LevelScale.AndroidPower, p).verdict == expected) ok++;
        return ok;
    }

    private static void LevelTests()
    {
        Console.WriteLine("Loudness readings (fallback when no audio) - every ENFORCED word");
        foreach (string word in WordStress.WordsWithStress.OrderBy(w => w))
        {
            StressPattern p = StressPattern.For(word);
            if (!StressReferences.For(p).enforced) continue;
            float[] audio = Load(word, out int sr);
            int own = LevelScore(audio, sr, p, StressAnalyzer.Verdict.Matches);
            Check(p.Spoken + ": Rachel's own -> correct on " + own + "/9", own == 9);
            // Unmeasurable is "say it again", never a pass, so it counts with wrong.
            int accepted = LevelScore(StressReferenceBuilder.MoveStress(audio, sr, p.IsFinal, 1f), sr, p, StressAnalyzer.Verdict.Matches);
            Check(p.Spoken + ": stress moved -> never accepted (accepted on " + accepted + "/9)", accepted <= 1);
        }
    }

    // ---- The five cases the fix was asked to handle ------------------------

    private static AnswerJudge.Evidence Audio(float[] samples, int rate)
    {
        return new AnswerJudge.Evidence { audio = StressReferenceBuilder.RawCondition(samples, rate, -50f, 1.25f), audioSampleRate = rate };
    }

    private static void RequiredCases()
    {
        Console.WriteLine("Required cases, through AnswerJudge (the code VoiceCommand runs), with recorded audio");
        float[] tayo = Load("tayo", out int sr);
        StressPattern tp = StressPattern.For("Tayo");
        var standUp = Audio(tayo, sr);
        var we = Audio(MoveRaw(tayo, sr, tp, 1f), sr);
        var s = AnswerJudge.Settings.StrictOnTrustedWords;

        var r1 = AnswerJudge.Judge("tayo", "Tayo", null, standUp, s);
        Check("1. Tayô (stand up) -> Correct", r1.outcome == AnswerJudge.Outcome.Correct, r1.outcome + ": " + r1.reason);

        var r2 = AnswerJudge.Judge("tayo", "Tayo", null, we, s);
        Check("2. Táyo (we) -> Wrong (wrong stress)", r2.outcome == AnswerJudge.Outcome.WrongStress, r2.outcome + ": " + r2.reason);
        var we2 = Audio(PronunciationReferenceBuilder.MoveStress(tayo, sr, tp, 0, 1f), sr);
        var r2c = AnswerJudge.Judge("tayo", "Tayo", null, we2, s);
        Check("2c. Táyo made the other way (syllables cut by the aligner) -> Wrong (wrong stress)",
              r2c.outcome == AnswerJudge.Outcome.WrongStress, r2c.outcome + ": " + r2c.reason);
        var r2b = AnswerJudge.Judge("kain tayo", "Tayo", null, standUp, s);
        Check("2b. \"kain tayo\" (let's eat) -> Wrong", r2b.outcome == AnswerJudge.Outcome.Wrong, r2b.outcome.ToString());

        var r3 = AnswerJudge.Judge("tayong", "Tayo", null, standUp, s);
        var r3b = AnswerJudge.Judge("kayo", "Tayo", null, standUp, s);
        Check("3. Incorrect pronunciation (\"tayong\", \"kayo\") -> Wrong",
              r3.outcome == AnswerJudge.Outcome.Wrong && r3b.outcome == AnswerJudge.Outcome.Wrong, r3.outcome + "/" + r3b.outcome);

        var silent = new AnswerJudge.Evidence { audio = StressReferenceBuilder.RawCondition(new float[sr], sr, -60f, 1f), audioSampleRate = sr };
        var r4 = AnswerJudge.Judge("tayo", "Tayo", null, silent, s);
        Check("4. Recording holds no voice -> Unclear (asked again, not scored)",
              r4.outcome == AnswerJudge.Outcome.Unclear, r4.outcome + ": " + r4.reason);
        var r4b = AnswerJudge.Judge("tayo po", "Tayo", null, standUp, s);
        Check("4b. \"tayo po\" -> Unclear (say the word on its own)", r4b.outcome == AnswerJudge.Outcome.Unclear, r4b.outcome.ToString());

        var first = AnswerJudge.Judge("tayo", "Tayo", null, we, s);
        var second = AnswerJudge.Judge("tayo", "Tayo", null, standUp, s);
        Check("5. Wrong stress, then correct on second attempt -> Correct",
              first.outcome == AnswerJudge.Outcome.WrongStress && second.outcome == AnswerJudge.Outcome.Correct,
              first.outcome + " then " + second.outcome);

        Console.WriteLine("Fallbacks");
        var levelsOnly = new AnswerJudge.Evidence
        {
            levels = WavLevelStream.FromSamples(StressReferenceBuilder.MoveStress(tayo, sr, true, 1f), sr),
            levelIntervalMs = 50f,
            levelScale = StressAnalyzer.LevelScale.AndroidPower,
        };
        var lv = AnswerJudge.Judge("tayo", "Tayo", null, levelsOnly, s);
        Check("No audio, loudness readings only: táyo -> Wrong (from readings)",
              lv.outcome == AnswerJudge.Outcome.WrongStress && lv.source == AnswerJudge.StressSource.VoiceLevels, lv.outcome + " / " + lv.source);

        var silentWithLevels = silent;
        silentWithLevels.levels = WavLevelStream.FromSamples(tayo, sr);
        silentWithLevels.levelIntervalMs = 50f;
        var sl = AnswerJudge.Judge("tayo", "Tayo", null, silentWithLevels, s);
        Check("Phone kept the mic for the recognizer (silent recording) -> readings decide: tayô Correct",
              sl.outcome == AnswerJudge.Outcome.Correct && sl.source == AnswerJudge.StressSource.VoiceLevels, sl.outcome + " / " + sl.source);

        // The older, lenient rules (strict mode off), still selectable in the Inspector.
        var lenient = AnswerJudge.Settings.StrictOnTrustedWords;
        lenient.requireRachelsStress = false;

        var noneStrictDefault = AnswerJudge.Judge("tayo", "Tayo", null, new AnswerJudge.Evidence(), s);
        Check("Strict (default): no evidence at all -> Unclear, never passed on its letters",
              noneStrictDefault.outcome == AnswerJudge.Outcome.Unclear, noneStrictDefault.outcome + ": " + noneStrictDefault.reason);
        var none = AnswerJudge.Judge("tayo", "Tayo", null, new AnswerJudge.Evidence(), lenient);
        Check("Lenient: no evidence at all -> letters decide, flagged unverified", none.outcome == AnswerJudge.Outcome.Correct && none.stressUnverified);
        var lenientNoFallback = lenient;
        lenientNoFallback.acceptOnLettersWhenNoEvidence = false;
        var noneStrict = AnswerJudge.Judge("tayo", "Tayo", null, new AnswerJudge.Evidence(), lenientNoFallback);
        Check("...and Unclear when that fallback is switched off", noneStrict.outcome == AnswerJudge.Outcome.Unclear);

        // Every word strict (verifyOnlyTrustedWords off): the full guarantees.
        var full = s;
        full.verifyOnlyTrustedWords = false;

        Console.WriteLine("Fully strict: never Correct unless the stress was measured and matched Rachel");
        int strictWords = 0, ownPassed = 0, movedRejected = 0, unchecked_ = 0;
        foreach (string word in WordStress.WordsWithStress.OrderBy(w => w))
        {
            StressPattern p = StressPattern.For(word);
            if (p == null || !File.Exists(StressReferenceBuilder.WavPath(vo, word))) continue;
            float[] a = Load(word, out int r);
            var own = AnswerJudge.Judge(word.ToLowerInvariant(), word, null, Audio(a, r), full);
            var moved = AnswerJudge.Judge(word.ToLowerInvariant(), word, null,
                                          Audio(StressReferenceBuilder.MoveStressRaw(a, r, p, 1f), r), full);
            var nothing = AnswerJudge.Judge(word.ToLowerInvariant(), word, null, new AnswerJudge.Evidence(), full);
            strictWords++;
            if (own.outcome == AnswerJudge.Outcome.Correct) ownPassed++;
            if (moved.outcome != AnswerJudge.Outcome.Correct) movedRejected++;
            foreach (var res in new[] { own, moved, nothing })
                if (res.outcome == AnswerJudge.Outcome.Correct && !(res.stressChecked && res.stressEnforced)) unchecked_++;
        }
        Check("Strict: no word with a known stress is ever Correct without a measured, acted-on stress",
              unchecked_ == 0, unchecked_ + " unchecked pass(es)");
        Console.WriteLine("  (for information: of " + strictWords + " recorded words, Rachel's own recording passes " +
                          ownPassed + ", the stress-moved one is refused " + movedRejected + ")");

        var lfill = AnswerJudge.Judge("bangon po", "Bangon", null, Audio(Load("bangon", out int bsr), bsr), full);
        Check("Fully strict: \"bangon po\" -> Unclear (say it on its own)", lfill.outcome == AnswerJudge.Outcome.Unclear, lfill.outcome.ToString());
        var lsent = AnswerJudge.Judge("bangon na ako", "Bangon", null, Audio(Load("bangon", out bsr), bsr), full);
        Check("Fully strict: \"bangon na ako\" -> Unclear (say it on its own)", lsent.outcome == AnswerJudge.Outcome.Unclear, lsent.outcome.ToString());

        Console.WriteLine("Default: strict on the trusted words only");
        Check("Bangon is not a trusted reference (so the next checks mean something)", !PronunciationReferences.For("Bangon").trusted);
        var dfill = AnswerJudge.Judge("bangon po", "Bangon", null, Audio(Load("bangon", out bsr), bsr), s);
        Check("untrusted word, \"bangon po\" -> Correct on its letters, flagged stress-unverified",
              dfill.outcome == AnswerJudge.Outcome.Correct && dfill.stressUnverified, dfill.outcome + ": " + dfill.reason);
        var dnone = AnswerJudge.Judge("bangon", "Bangon", null, new AnswerJudge.Evidence(), s);
        Check("untrusted word, no evidence -> Correct on its letters, flagged stress-unverified",
              dnone.outcome == AnswerJudge.Outcome.Correct && dnone.stressUnverified, dnone.outcome.ToString());
        var tfill = AnswerJudge.Judge("tayo po", "Tayo", null, Audio(tayo, sr), s);
        var tnone = AnswerJudge.Judge("tayo", "Tayo", null, new AnswerJudge.Evidence(), s);
        Check("trusted word (Tayo) stays fully strict: \"tayo po\" and no evidence -> Unclear",
              tfill.outcome == AnswerJudge.Outcome.Unclear && tnone.outcome == AnswerJudge.Outcome.Unclear, tfill.outcome + "/" + tnone.outcome);
        int unverifiedPasses = 0, untrustedWrongCaught = 0, untrustedWords = 0;
        foreach (string word in WordStress.WordsWithStress.OrderBy(w => w))
        {
            StressPattern p = StressPattern.For(word);
            if (p == null || !File.Exists(StressReferenceBuilder.WavPath(vo, word))) continue;
            float[] a = Load(word, out int r);
            var res = AnswerJudge.Judge(word.ToLowerInvariant(), word, null, Audio(a, r), s);
            if (res.outcome == AnswerJudge.Outcome.Correct && !(res.stressChecked && res.stressEnforced) && !res.stressUnverified) unverifiedPasses++;
            if (!PronunciationReferences.For(word).trusted)
            {
                untrustedWords++;
                var mv = AnswerJudge.Judge(word.ToLowerInvariant(), word, null, Audio(StressReferenceBuilder.MoveStressRaw(a, r, p, 1f), r), s);
                if (mv.outcome == AnswerJudge.Outcome.WrongStress) untrustedWrongCaught++;
            }
        }
        Check("every pass without a verified stress is flagged stress-unverified (" + unverifiedPasses + " unflagged)", unverifiedPasses == 0);
        Console.WriteLine("  (for information: on the " + untrustedWords + " untrusted words, the stress moved is still caught as wrong on " +
                          untrustedWrongCaught + "; the rest pass on their letters)");

        foreach (string word in WordStress.WordsWithStress.OrderBy(w => w))
        {
            StressPattern p = StressPattern.For(word);
            if (RawStressReferences.For(p).enforced) continue;
            if (!File.Exists(StressReferenceBuilder.WavPath(vo, word))) continue;
            float[] a = Load(word, out int r);
            var res = AnswerJudge.Judge(word.ToLowerInvariant(), word, null,
                                        Audio(StressReferenceBuilder.MoveStressRaw(a, r, p, 1f), r), lenient);
            Check(p.Spoken + " (not enforced, lenient): letters decide, stress still measured",
                  res.outcome == AnswerJudge.Outcome.Correct && res.stressChecked && !res.stressEnforced, res.outcome + ": " + res.reason);
        }

        var plain = AnswerJudge.Judge("bag", "Bag", null, new AnswerJudge.Evidence(), s);
        Check("One-syllable word (Bag) decided by its letters", plain.outcome == AnswerJudge.Outcome.Correct);

        Console.WriteLine("Pairs and other words");
        float[] lakad = Load("lakad", out int lr);
        var walk = Audio(lakad, lr);
        var lk = AnswerJudge.Judge("lakad tayo", "Lakad", null, walk, s);
        Check("\"lakad tayo\" for Lakad (a pair, inside a sentence) -> Wrong", lk.outcome == AnswerJudge.Outcome.Wrong, lk.outcome.ToString());
        float[] bangon = Load("bangon", out int br);
        var bp = AnswerJudge.Judge("bangon po", "Bangon", null, Audio(bangon, br), lenient);
        Check("Lenient: \"bangon po\" (not a pair, filler) -> Correct, as before", bp.outcome == AnswerJudge.Outcome.Correct, bp.outcome.ToString());
        var bs = AnswerJudge.Judge("bangon na ako", "Bangon", null, Audio(bangon, br), lenient);
        Check("Lenient: \"bangon na ako\" (not a pair, in a sentence) -> Correct, as before", bs.outcome == AnswerJudge.Outcome.Correct, bs.outcome.ToString());
        var bWrong = AnswerJudge.Judge("bangon", "Bangon", null,
            Audio(StressReferenceBuilder.MoveStressRaw(bangon, br, StressPattern.For("Bangon"), 1f), br), full);
        Check("fully strict: bangón (stress moved, said alone) -> never Correct (wrong stress, or asked again)",
              bWrong.outcome != AnswerJudge.Outcome.Correct, bWrong.outcome + ": " + bWrong.reason);
    }

    // ---- Strict mode, every word: the guarantees ------------------------------

    private static AnswerJudge.Evidence Raw(float[] x, int sr) { return new AnswerJudge.Evidence { audio = x, audioSampleRate = sr }; }

    /// <summary>
    /// For every word with a recording of Rachel, through AnswerJudge exactly
    /// as VoiceCommand calls it, under every simulated phone and speaker
    /// (noise, child-like voices, slow and fast speech):
    ///   - the stress moved (made two different ways) is NEVER Correct;
    ///   - Rachel's own delivery is NEVER WrongStress (at worst asked again);
    ///   - the same recording judged twice gets the same verdict;
    ///   - the same delivery with different room noise never gets Correct on
    ///     one try and WrongStress on another.
    /// </summary>
    /// <summary>
    /// Phones and speakers the references were NOT calibrated on: other room
    /// noise (and other random noise), other voice heights and other speeds.
    /// </summary>
    private static readonly PronunciationReferenceBuilder.Condition[] HeldOut =
    {
        new PronunciationReferenceBuilder.Condition { name = "noise -52 dB, voice x1.15", noiseDb = -52f, raise = 1.15f, tempo = 1f },
        new PronunciationReferenceBuilder.Condition { name = "noise -42 dB, voice x1.35, slower x1.1", noiseDb = -42f, raise = 1.35f, tempo = 1.1f },
        new PronunciationReferenceBuilder.Condition { name = "noise -48 dB, faster x0.85", noiseDb = -48f, raise = 1f, tempo = 0.85f },
        new PronunciationReferenceBuilder.Condition { name = "noise -40 dB, voice x1.2, slower x1.2", noiseDb = -40f, raise = 1.2f, tempo = 1.2f },
        new PronunciationReferenceBuilder.Condition { name = "noise -56 dB, voice x1.5", noiseDb = -56f, raise = 1.5f, tempo = 1f },
    };

    /// <summary>
    /// For every word with a recording of Rachel, through AnswerJudge exactly
    /// as VoiceCommand calls it, on phones, speakers AND wrong stresses the
    /// references were not calibrated on:
    ///   - a wrong stress (moved 75% or 130% of the way, or moved by the other
    ///     method at 75%) is NEVER Correct;
    ///   - Rachel's own delivery is NEVER WrongStress (at worst asked again);
    ///   - the same recording judged twice gets the same verdict;
    ///   - the same delivery in different room noise is never Correct on one
    ///     try and WrongStress on another.
    /// </summary>
    private static void StrictEveryWord()
    {
        Console.WriteLine("Strict mode, every recorded word, on " + HeldOut.Length +
                          " held-out phones/speakers and held-out wrong stresses (none used in calibration)");
        var s = AnswerJudge.Settings.StrictOnTrustedWords;
        int words = 0, falseAccepts = 0, falseRejects = 0, ownCorrect = 0, ownTotal = 0, nondeterministic = 0, flips = 0;
        int wrongCaught = 0, wrongTotal = 0, wrongPassedUnverified = 0, untrustedWrongFlips = 0;
        List<string> report = new List<string>();
        foreach (string word in WordStress.WordsWithStress.OrderBy(w => w))
        {
            StressPattern p = StressPattern.For(word);
            if (p == null || !File.Exists(StressReferenceBuilder.WavPath(vo, word))) continue;
            float[] a = Load(word, out int sr);
            string said = word.ToLowerInvariant();
            words++;

            List<float[]> wrongs = new List<float[]> { StressReferenceBuilder.MoveStressRaw(a, sr, p, 0.75f) };
            foreach (int other in PronunciationAssessor.Neighbours(p))
                foreach (float k in new[] { 0.75f, 1.3f })
                {
                    float[] m = PronunciationReferenceBuilder.MoveStress(a, sr, p, other, k);
                    if (m != null) wrongs.Add(m);
                }
            bool trusted = PronunciationReferences.For(word).trusted;
            int wordOwnOk = 0, wordFalseAcc = 0, wordFalseRej = 0, wordCaught = 0, wordWrong = 0;

            for (int ci = 0; ci < HeldOut.Length; ci++)
            {
                var c = HeldOut[ci];
                float[] x = PronunciationReferenceBuilder.Apply(a, sr, c, 23 + ci);
                var own = AnswerJudge.Judge(said, word, null, Raw(x, sr), s);
                ownTotal++;
                if (own.outcome == AnswerJudge.Outcome.Correct) { ownCorrect++; wordOwnOk++; }
                if (own.outcome == AnswerJudge.Outcome.WrongStress || own.outcome == AnswerJudge.Outcome.Wrong)
                {
                    falseRejects++; wordFalseRej++;
                    Console.WriteLine("    false reject: " + word + " (" + c.name + "): " + own.reason);
                }

                foreach (float[] m in wrongs)
                {
                    var mv = AnswerJudge.Judge(said, word, null, Raw(PronunciationReferenceBuilder.Apply(m, sr, c, 23 + ci), sr), s);
                    wrongTotal++; wordWrong++;
                    if (mv.outcome == AnswerJudge.Outcome.Correct && mv.stressUnverified) wrongPassedUnverified++;
                    if (mv.outcome == AnswerJudge.Outcome.Correct && !mv.stressUnverified)
                    {
                        falseAccepts++; wordFalseAcc++;
                        Console.WriteLine("    false accept: " + word + " (" + c.name + "): " + mv.reason);
                    }
                    if (mv.outcome == AnswerJudge.Outcome.WrongStress) { wrongCaught++; wordCaught++; }
                }

                if (ci % 2 == 0)
                {
                    var again = AnswerJudge.Judge(said, word, null, Raw(x, sr), s);
                    if (again.outcome != own.outcome || !SameNumber(again.stress.measured, own.stress.measured)) nondeterministic++;
                    foreach (float[] src in new[] { a, wrongs[wrongs.Count - 1] })
                    {
                        bool anyCorrect = false, anyWrong = false;
                        for (int seed = 41; seed < 45; seed++)
                        {
                            var rep = AnswerJudge.Judge(said, word, null, Raw(PronunciationReferenceBuilder.Apply(src, sr, c, seed), sr), s);
                            anyCorrect |= rep.outcome == AnswerJudge.Outcome.Correct;
                            anyWrong |= rep.outcome == AnswerJudge.Outcome.WrongStress;
                        }
                        if (anyCorrect && anyWrong)
                        {
                            // On an untrusted word a wrong stress may pass on its letters
                            // (verifyOnlyTrustedWords), so it can be caught on one try and
                            // pass on the next: reported, not failed. Everything else must hold.
                            bool allowed = !trusted && src != a;
                            if (allowed) untrustedWrongFlips++; else flips++;
                            Console.WriteLine("    flip: " + word + " (" + c.name + ", " + (src == a ? "Rachel's own" : "wrong stress") + ")" +
                                              (allowed ? " - untrusted word, wrong stress: allowed" : ""));
                        }
                    }
                }
            }
            report.Add(word + (trusted ? "*" : "") + " " + wordOwnOk + "/" + HeldOut.Length + " ok, wrong caught " + wordCaught + "/" + wordWrong +
                       (wordFalseAcc > 0 ? " FALSE-ACCEPT " + wordFalseAcc : "") + (wordFalseRej > 0 ? " FALSE-REJECT " + wordFalseRej : ""));
        }
        Check("a wrong stress is never accepted as VERIFIED, for any word, phone or speaker (" + falseAccepts + " of " + wrongTotal + " accepted)", falseAccepts == 0);
        Check("Rachel's own delivery is never marked wrong (" + falseRejects + " of " + ownTotal + " marked wrong)", falseRejects == 0);
        Check("the same recording always gets the same verdict (" + nondeterministic + " differed)", nondeterministic == 0);
        Check("the same delivery in different room noise is never Correct on one try and WrongStress on another - " +
              "for Rachel's own delivery of any word, and for anything said on a trusted word (" + flips + " flips)", flips == 0);
        Console.WriteLine("  (for information: a wrong stress on an untrusted word was caught on one try and passed unverified on another " +
                          untrustedWrongFlips + " time(s))");
        Console.WriteLine("  (for information: Rachel's own delivery accepted on " + ownCorrect + "/" + ownTotal +
                          ", the rest asked again; wrong stress marked wrong on " + wrongCaught + "/" + wrongTotal +
                          ", the rest asked again; * = trusted reference)");
        Console.WriteLine("  (for information: on untrusted words, " + wrongPassedUnverified + " of the wrong stresses passed on their letters, flagged stress-unverified)");
        foreach (string line in report) Console.WriteLine("    " + line);
    }

    private static bool SameNumber(float a, float b) { return (float.IsNaN(a) && float.IsNaN(b)) || a == b; }

    /// <summary>The loudness-reading path in strict mode: bands, never a single line.</summary>
    private static void BandedLevels()
    {
        Console.WriteLine("Loudness readings, strict mode (banded) - every recorded two-syllable word");
        int falseAcc = 0, falseRej = 0, ownOk = 0, total = 0;
        List<string> unreachable = new List<string>();
        foreach (string word in WordStress.WordsWithStress.OrderBy(w => w))
        {
            StressPattern p = StressPattern.For(word);
            if (p == null || p.SyllableCount != 2 || !File.Exists(StressReferenceBuilder.WavPath(vo, word))) continue;
            float[] a = Load(word, out int sr);
            float[] moved = StressReferenceBuilder.MoveStress(a, sr, p.IsFinal, 1f);
            int wordOk = 0;
            foreach (float hop in Hops)
                foreach (float peak in Peaks)
                {
                    total++;
                    var own = StressAnalyzer.JudgeBanded(WavLevelStream.FromSamples(a, sr, hop, peak), hop, StressAnalyzer.LevelScale.AndroidPower, p);
                    var mv = StressAnalyzer.JudgeBanded(WavLevelStream.FromSamples(moved, sr, hop, peak), hop, StressAnalyzer.LevelScale.AndroidPower, p);
                    if (own.verdict == StressAnalyzer.Verdict.Matches) { ownOk++; wordOk++; }
                    if (own.verdict == StressAnalyzer.Verdict.WrongStress) falseRej++;
                    if (mv.verdict == StressAnalyzer.Verdict.Matches) falseAcc++;
                }
            if (wordOk == 0) unreachable.Add(word);
        }
        Check("readings: the stress moved is never accepted (" + falseAcc + ")", falseAcc == 0);
        Check("readings: Rachel's own is never marked wrong (" + falseRej + ")", falseRej == 0);
        Console.WriteLine("  (for information: Rachel's own accepted from readings on " + ownOk + "/" + total +
                          "; words her own delivery never passes from readings alone - only a clearer stress than hers would: " +
                          (unreachable.Count == 0 ? "none" : string.Join(", ", unreachable)) + ")");
    }

    // ---- Balanced mode (the default): the word first, stress second -----------

    /// <summary>
    /// The default the game plays with. Bangon (an ordinary word) must be
    /// accepted whatever its stress, speed or voice, and refused when it is
    /// another word; a same-spelling pair is refused only on a clearly
    /// different stress; Rachel's own delivery of every word always passes.
    /// </summary>
    private static void BalancedTests()
    {
        Console.WriteLine("Balanced mode (default): the word first, stress only where it changes the word");
        var b = AnswerJudge.Settings.Default;
        Check("the default is balanced", b.balanced);

        // ---- Bangon, the example in the request ----
        StressPattern bp = StressPattern.For("Bangon");
        float[] bangon = Load("bangon", out int bsr);
        List<(string, float[])> deliveries = new List<(string, float[])>();
        deliveries.Add(("natural (Rachel's own)", bangon));
        deliveries.Add(("stress moved to the other syllable", StressReferenceBuilder.MoveStressRaw(bangon, bsr, bp, 1f)));
        float[] moved2 = PronunciationReferenceBuilder.MoveStress(bangon, bsr, bp, 1, 1f);
        if (moved2 != null) deliveries.Add(("stress moved, other method", moved2));
        float[] half = PronunciationReferenceBuilder.MoveStress(bangon, bsr, bp, 1, 0.5f);
        if (half != null) deliveries.Add(("slightly different stress", half));
        deliveries.Add(("pitch moved only", PitchOnly(bangon, bsr, false, 3f)));
        deliveries.Add(("slower x1.3", StressReferenceBuilder.Stretch(bangon, bsr, 1.3f)));
        deliveries.Add(("faster x0.75", StressReferenceBuilder.Stretch(bangon, bsr, 0.75f)));
        var allConds = PronunciationReferenceBuilder.Conditions.Concat(HeldOut).ToArray();
        foreach (var (label, audio) in deliveries)
        {
            int ok = 0;
            foreach (var c in allConds)
                if (AnswerJudge.Judge("bangon", "Bangon", null, Raw(PronunciationReferenceBuilder.Apply(audio, bsr, c), bsr), b).outcome == AnswerJudge.Outcome.Correct) ok++;
            Check("Bangon, " + label + " -> accepted on every phone and voice (" + ok + "/" + allConds.Length + ")", ok == allConds.Length);
        }
        var noAudio = AnswerJudge.Judge("bangon", "Bangon", null, new AnswerJudge.Evidence(), b);
        Check("Bangon with no audio at all (a phone that gives none) -> accepted on the word", noAudio.outcome == AnswerJudge.Outcome.Correct);
        var withPo = AnswerJudge.Judge("bangon po", "Bangon", null, Raw(bangon, bsr), b);
        Check("\"bangon po\" -> accepted", withPo.outcome == AnswerJudge.Outcome.Correct, withPo.outcome.ToString());
        foreach (string said in new[] { "bagon", "bango", "bangga", "bang", "bangin", "tayo", "kanan", "lakad", "mali", "" })
        {
            var r = AnswerJudge.Judge(said, "Bangon", null, Raw(bangon, bsr), b);
            Check("Bangon target, heard \"" + said + "\" -> rejected (another word), even with the right audio", r.outcome == AnswerJudge.Outcome.Wrong, r.outcome.ToString());
        }
        foreach (string said in new[] { "bangon", "Bangón", "BANGON", "bangun", "bangon po", "ang bangon" })
        {
            var r = AnswerJudge.Judge(said, "Bangon", null, Raw(bangon, bsr), b);
            Check("Bangon target, heard \"" + said + "\" -> accepted (spelling the recognizer may give)", r.outcome == AnswerJudge.Outcome.Correct, r.outcome.ToString());
        }

        // ---- Every taught word: never confused with another ----
        int confused = 0;
        foreach (string target in WordVariants.Words)
            foreach (string said in WordVariants.Words)
                if (!string.Equals(target, said, StringComparison.OrdinalIgnoreCase) &&
                    AnswerJudge.Judge(said.ToLowerInvariant(), target, null, new AnswerJudge.Evidence(), b).outcome != AnswerJudge.Outcome.Wrong) confused++;
        Check("no taught word is ever accepted for another (" + confused + " confusions)", confused == 0);

        // ---- Every recorded word: Rachel's own always passes; ordinary words never fail on stress ----
        int ownFail = 0, ownTotal = 0, ordinaryStressFail = 0, ordinaryTotal = 0, pairCaught = 0, pairWrongTotal = 0, pairFlips = 0;
        List<string> pairReport = new List<string>();
        foreach (string word in WordStress.WordsWithStress.OrderBy(w => w))
        {
            StressPattern p = StressPattern.For(word);
            if (p == null || !File.Exists(StressReferenceBuilder.WavPath(vo, word))) continue;
            float[] a = Load(word, out int sr);
            string said = word.ToLowerInvariant();
            List<float[]> wrongs = new List<float[]> { StressReferenceBuilder.MoveStressRaw(a, sr, p, 1f) };
            foreach (int other in PronunciationAssessor.Neighbours(p))
            {
                float[] m = PronunciationReferenceBuilder.MoveStress(a, sr, p, other, 1f);
                if (m != null) wrongs.Add(m);
            }
            int caught = 0, total = 0;
            for (int ci = 0; ci < allConds.Length; ci++)
            {
                var c = allConds[ci];
                ownTotal++;
                if (AnswerJudge.Judge(said, word, null, Raw(PronunciationReferenceBuilder.Apply(a, sr, c), sr), b).outcome != AnswerJudge.Outcome.Correct) ownFail++;
                foreach (float[] m in wrongs)
                {
                    var r = AnswerJudge.Judge(said, word, null, Raw(PronunciationReferenceBuilder.Apply(m, sr, c), sr), b);
                    if (p.IsMinimalPair) { total++; pairWrongTotal++; if (r.outcome == AnswerJudge.Outcome.WrongStress) { caught++; pairCaught++; } }
                    else { ordinaryTotal++; if (r.outcome != AnswerJudge.Outcome.Correct) ordinaryStressFail++; }
                }
                if (p.IsMinimalPair && ci % 3 == 0)
                {
                    bool anyCorrect = false, anyWrong = false;
                    for (int seed = 61; seed < 65; seed++)
                    {
                        var r = AnswerJudge.Judge(said, word, null, Raw(PronunciationReferenceBuilder.Apply(wrongs[wrongs.Count - 1], sr, c, seed), sr), b);
                        anyCorrect |= r.outcome == AnswerJudge.Outcome.Correct;
                        anyWrong |= r.outcome == AnswerJudge.Outcome.WrongStress;
                    }
                    if (anyCorrect && anyWrong) pairFlips++;
                }
            }
            if (p.IsMinimalPair) pairReport.Add(p.Spoken + " " + caught + "/" + total);
        }
        Check("Rachel's own delivery of every word is accepted on every phone and voice (" + ownFail + " of " + ownTotal + " refused)", ownFail == 0);
        Check("an ordinary word is never refused for its stress (" + ordinaryStressFail + " of " + ordinaryTotal + " refused)", ordinaryStressFail == 0);

        // ---- The pairs, where stress changes the word ----
        float[] tayo = Load("tayo", out int tsr);
        StressPattern tp = StressPattern.For("Tayo");
        var standUp = AnswerJudge.Judge("tayo", "Tayo", null, Raw(StressReferenceBuilder.RawCondition(tayo, tsr, -50f, 1.25f), tsr), b);
        var we = AnswerJudge.Judge("tayo", "Tayo", null, Raw(StressReferenceBuilder.RawCondition(PronunciationReferenceBuilder.MoveStress(tayo, tsr, tp, 0, 1f), tsr, -50f, 1.25f), tsr), b);
        var kainTayo = AnswerJudge.Judge("kain tayo", "Tayo", null, Raw(tayo, tsr), b);
        Check("pair: tayô (stand up) -> accepted", standUp.outcome == AnswerJudge.Outcome.Correct, standUp.outcome + ": " + standUp.reason);
        Check("pair: táyo (we), clearly the other word -> refused as wrong stress", we.outcome == AnswerJudge.Outcome.WrongStress, we.outcome + ": " + we.reason);
        Check("pair: \"kain tayo\" (let's eat) -> refused", kainTayo.outcome == AnswerJudge.Outcome.Wrong);
        Console.WriteLine("  (for information: on the same-spelling pairs a clearly moved stress was refused on " + pairCaught + "/" + pairWrongTotal +
                          " - the rest passed, as the stress was not clear enough to call it the other word: " + string.Join(", ", pairReport) + ")");
        Console.WriteLine("  (for information: a moved stress on a pair was refused on one try and accepted on another " + pairFlips + " time(s))");
    }

    // ---- Real recordings, sorted by what was said -----------------------------

    /// <summary>
    /// Judges real recordings - the team's own voices, a child's, anyone's -
    /// that a person has sorted by what was actually said:
    ///
    ///   DIR/&lt;Word&gt;/correct/*.wav        the word, stressed as Rachel does
    ///   DIR/&lt;Word&gt;/wrong_stress/*.wav   the right sounds, stress on the wrong syllable
    ///   DIR/&lt;Word&gt;/wrong_sound/*.wav    a syllable said wrong (judged on audio only)
    ///
    /// The Editor writes them there: hold R, say the word, let go, then press
    /// F5 (correct), F6 (wrong stress) or F7 (wrong sound) - see VoiceCommand.
    /// Exit status 1 if anything labelled wrong was accepted.
    /// </summary>
    private static int Labelled(string dir)
    {
        if (!Directory.Exists(dir))
        {
            Console.WriteLine("No labelled recordings in " + dir + " yet. In the Editor: hold R, say the word, let go,");
            Console.WriteLine("then press F5 (said correctly), F6 (wrong stress) or F7 (wrong sound).");
            return 2;
        }
        var s = AnswerJudge.Settings.Default;
        int falseAccept = 0, correctAccepted = 0, correctTotal = 0, wrongCaught = 0, wrongTotal = 0, asked = 0;
        foreach (string wordDir in Directory.GetDirectories(dir).OrderBy(d => d))
        {
            string word = Path.GetFileName(wordDir);
            foreach (string labelDir in Directory.GetDirectories(wordDir).OrderBy(d => d))
            {
                string label = Path.GetFileName(labelDir).ToLowerInvariant();
                foreach (string f in Directory.GetFiles(labelDir, "*.wav").OrderBy(x => x))
                {
                    float[] x = WavLevelStream.ReadMono(f, out int sr);
                    var r = AnswerJudge.Judge(word.ToLowerInvariant(), word, null, Raw(x, sr), s);
                    bool accepted = r.outcome == AnswerJudge.Outcome.Correct;
                    if (label == "correct") { correctTotal++; if (accepted) correctAccepted++; }
                    else { wrongTotal++; if (accepted) falseAccept++; else if (r.outcome != AnswerJudge.Outcome.Unclear) wrongCaught++; }
                    if (r.outcome == AnswerJudge.Outcome.Unclear) asked++;
                    Console.WriteLine((label == "correct" ? (accepted ? "  ok    " : "  miss  ") : (accepted ? "  FALSE " : "  ok    ")) +
                                      word + "/" + label + "/" + Path.GetFileName(f) + " -> " + r.outcome + "  " + r.reason);
                }
            }
        }
        Console.WriteLine();
        Console.WriteLine("labelled correct: accepted " + correctAccepted + "/" + correctTotal +
                          "; labelled wrong: caught " + wrongCaught + "/" + wrongTotal + ", ACCEPTED " + falseAccept +
                          "; asked again " + asked + " in all");
        return falseAccept == 0 ? 0 : 1;
    }

    // ---- Not pass/fail -------------------------------------------------------

    private static void Informational()
    {
        Console.WriteLine();
        Console.WriteLine("Every word with a known stress (raw audio; Rachel accepted / moved not accepted, of 9):");
        string[][] areas =
        {
            new[] { "Bahay", "Bangon Tayo Lakad Kaliwa Kanan Bukas Ilaw Tuwalya Sabon Maligo Sipilyo Suklay Damit Medyas Sapatos Bag Kunin Upo Pagkain Kain Inom Linis" },
            new[] { "Paaralan", "Lakad Bukas Bati Pasok Takbo Upo Kinig Kuha Lapis Aklat Basa Sulat Sagot Ligpit Kain Inom Tapon Tayo Kunin Paalam Alis" },
            new[] { "Parke", "Lakad Takbo Tingin Tuwa Pasok Akyat Dausdos Sakay Tulak Talon Kumusta Ngiti Tanong Laro Bati Upo Kain Ibahagi Inom Salamat Paalam Pagod Uwi" },
        };
        foreach (string[] area in areas)
        {
            Console.WriteLine("  " + area[0] + ":");
            foreach (string w in area[1].Split(' ').Distinct())
            {
                StressPattern p = StressPattern.For(w);
                if (p == null) { Console.WriteLine("    " + w.PadRight(9) + " -            no stress check (" + (w == "Bag" ? "one syllable" : "stress not decided") + ")"); continue; }
                string path = StressReferenceBuilder.WavPath(vo, w);
                if (!File.Exists(path)) { Console.WriteLine("    " + w.PadRight(9) + p.Spoken.PadRight(13) + "no recording of Rachel - letters decide"); continue; }
                float[] a = Load(w, out int r);
                int own = RawScore(a, r, p, StressAnalyzer.Verdict.Matches, out _);
                int notAcc = 9 - RawScore(MoveRaw(a, r, p, 1f), r, p, StressAnalyzer.Verdict.Matches, out _);
                bool enf = RawStressReferences.For(p).enforced;
                Console.WriteLine("    " + w.PadRight(9) + p.Spoken.PadRight(13) + own + "/9  " + notAcc + "/9  " +
                                  (enf ? "ENFORCED" : "measured only") + (p.IsMinimalPair ? "   (same-spelling pair)" : ""));
            }
        }

        Console.WriteLine();
        Console.WriteLine("For information (not pass/fail): raw-audio score of Tayo as the stress moves");
        Console.WriteLine("  (0 = Rachel's tayô, 1 = the clear táyo above; noise -50 dB, child-like voice x1.25):");
        float[] tayo = Load("tayo", out int rate);
        StressPattern tp = StressPattern.For("Tayo");
        string line = "   ";
        float cut = 0f;
        foreach (float k in new[] { 0f, 0.1f, 0.2f, 0.3f, 0.4f, 0.5f, 0.7f, 1f })
        {
            var j = RawStressAnalyzer.Judge(StressReferenceBuilder.RawCondition(MoveRaw(tayo, rate, tp, k), rate, -50f, 1.25f), rate, tp);
            cut = j.threshold;
            line += " " + k.ToString("0.0") + "=" + (j.verdict == StressAnalyzer.Verdict.Matches ? "ok" : "WRONG") +
                    "(" + j.measured.ToString("0.0") + ")";
        }
        Console.WriteLine(line + "   cut-off " + cut.ToString("0.0"));

        Console.WriteLine();
        Console.WriteLine("For information: the strict verdict on Tayo as the stress moves (same conditions; position on");
        Console.WriteLine("  the line from the moved stress -1 to Rachel's +1, Correct from " + PronunciationReferences.For("Tayo").acceptFrom.ToString("0.00") +
                          ", WrongStress from " + PronunciationReferences.For("Tayo").rejectFrom.ToString("0.00") + "):");
        line = "   ";
        foreach (float k in new[] { 0f, 0.1f, 0.2f, 0.3f, 0.4f, 0.5f, 0.7f, 1f })
        {
            float[] x = PronunciationReferenceBuilder.MoveStress(tayo, rate, tp, 0, k);
            var r = AnswerJudge.Judge("tayo", "Tayo", null, Raw(StressReferenceBuilder.RawCondition(x, rate, -50f, 1.25f), rate), AnswerJudge.Settings.StrictOnTrustedWords);
            line += " " + k.ToString("0.0") + "=" + r.outcome + "(" + r.assessment.position.ToString("0.00") + ")";
        }
        Console.WriteLine(line);
    }

    private static float[] Load(string word, out int sr)
    {
        return WavLevelStream.ReadMono(StressReferenceBuilder.WavPath(vo, word.ToLowerInvariant()), out sr);
    }
}
