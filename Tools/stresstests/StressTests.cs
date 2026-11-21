// Tests for SALINLAHI's stress-aware answer checking (tayô "stand up" against
// táyo "we", and every other pair WordStress marks).
//
// These compile the GAME'S OWN files - PronunciationChecker, WordVariants,
// WordStress, StressPattern, StressAnalyzer, RawStressAnalyzer, both
// reference tables, AnswerJudge and the editor's WavLevelStream /
// StressReferenceBuilder - not copies of them, and run without Unity.
// Run with Tools/test_stress_rules.sh.
//
//   generate  re-measures Assets/Audio/VO and rewrites both reference files
//   debug     prints what the raw-audio analyzer hears in each recording
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

        PatternTests();
        ContextTests();
        ReferencesAreCurrent(root);
        RawAudioTests();
        LevelTests();
        RequiredCases();
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
            Console.WriteLine(Path.GetFileName(path) + "  " + (x.Length / (float)rate).ToString("0.00") + " s  -> " + j.verdict +
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
        Expect("Bangon", 2, 1, "ba-NGÓN");
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
        var s = AnswerJudge.Settings.Default;

        var r1 = AnswerJudge.Judge("tayo", "Tayo", null, standUp, s);
        Check("1. Tayô (stand up) -> Correct", r1.outcome == AnswerJudge.Outcome.Correct, r1.outcome + ": " + r1.reason);

        var r2 = AnswerJudge.Judge("tayo", "Tayo", null, we, s);
        Check("2. Táyo (we) -> Wrong (wrong stress)", r2.outcome == AnswerJudge.Outcome.WrongStress, r2.outcome + ": " + r2.reason);
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

        var none = AnswerJudge.Judge("tayo", "Tayo", null, new AnswerJudge.Evidence(), s);
        Check("No evidence at all -> letters decide, flagged unverified", none.outcome == AnswerJudge.Outcome.Correct && none.stressUnverified);
        var strict = AnswerJudge.Settings.Default;
        strict.acceptOnLettersWhenNoEvidence = false;
        var noneStrict = AnswerJudge.Judge("tayo", "Tayo", null, new AnswerJudge.Evidence(), strict);
        Check("...and Unclear when that fallback is switched off", noneStrict.outcome == AnswerJudge.Outcome.Unclear);

        foreach (string word in WordStress.WordsWithStress.OrderBy(w => w))
        {
            StressPattern p = StressPattern.For(word);
            if (RawStressReferences.For(p).enforced) continue;
            if (!File.Exists(StressReferenceBuilder.WavPath(vo, word))) continue;
            float[] a = Load(word, out int r);
            var res = AnswerJudge.Judge(word.ToLowerInvariant(), word, null,
                                        Audio(StressReferenceBuilder.MoveStressRaw(a, r, p, 1f), r), s);
            Check(p.Spoken + " (not enforced): letters decide, stress still measured",
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
        var bp = AnswerJudge.Judge("bangon po", "Bangon", null, Audio(bangon, br), s);
        Check("\"bangon po\" (not a pair, filler) -> Correct, as before", bp.outcome == AnswerJudge.Outcome.Correct, bp.outcome.ToString());
        var bs = AnswerJudge.Judge("bangon na ako", "Bangon", null, Audio(bangon, br), s);
        Check("\"bangon na ako\" (not a pair, in a sentence) -> Correct, as before", bs.outcome == AnswerJudge.Outcome.Correct, bs.outcome.ToString());
        if (RawStressReferences.For(StressPattern.For("Bangon")).enforced)
        {
            var bWrong = AnswerJudge.Judge("bangon", "Bangon", null,
                Audio(StressReferenceBuilder.MoveStressRaw(bangon, br, StressPattern.For("Bangon"), 1f), br), s);
            Check("bangón (stress moved, said alone) -> wrong stress", bWrong.outcome == AnswerJudge.Outcome.WrongStress, bWrong.outcome.ToString());
        }
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
    }

    private static float[] Load(string word, out int sr)
    {
        return WavLevelStream.ReadMono(StressReferenceBuilder.WavPath(vo, word.ToLowerInvariant()), out sr);
    }
}
