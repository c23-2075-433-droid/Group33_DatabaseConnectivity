// Assets/Editor/PronunciationCheckTool.cs
//
// Tools > SALINLAHI > Check Pronunciation Matching
//
// Proves that voice answers are judged correctly, without needing a phone. Run
// it after editing WordVariants or PronunciationChecker; it prints a pass/fail
// report to the Console.
//
// A menu item rather than NUnit tests, deliberately. The Unity Test Framework
// IS available here (1.8.0, builtin, pulled in by com.unity.ide.visualstudio),
// so that is not the reason. The reason is that this project has no .asmdef
// anywhere, and a test assembly would be the first one - which changes how
// every script under Assets is compiled and referenced. That is not a change
// worth making in order to land a bug fix. If assembly definitions arrive
// later, these checks port to EditMode tests close to as they stand.
//
// The check that matters most is #3: every taught word against every OTHER
// taught word, all 53x52 of them, none of which may be accepted. That is the
// regression guard for the bug this tool was written for - "kaha" being scored
// tama for "Kuha" - and for its worse cousin, "Kanin" being accepted for
// "Kain" when both are taught in the same breakfast scene.

using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;

public static class PronunciationCheckTool
{
    /// <summary>One expected outcome, for the fixed regression table below.</summary>
    private struct Case
    {
        public string Spoken;
        public string Target;
        public bool ShouldBeCorrect;
        public string Why;

        public Case(string spoken, string target, bool shouldBeCorrect, string why)
        {
            Spoken = spoken; Target = target; ShouldBeCorrect = shouldBeCorrect; Why = why;
        }
    }

    private static readonly Case[] RegressionCases =
    {
        // The reported bug, and the same mistake on every other short word.
        new Case("kaha",  "Kuha",  false, "wrong vowel - the reported bug"),
        new Case("kanin", "Kain",  false, "a DIFFERENT taught word, same scene (Almusal)"),
        new Case("kain",  "Kanin", false, "the same collision the other way round"),
        new Case("tuwa",  "Tawa",  false, "wrong vowel, and both are taught"),
        new Case("tawa",  "Tuwa",  false, "wrong vowel, and both are taught"),
        new Case("tabo",  "Takbo", false, "a near-miss on a real Filipino word"),
        new Case("kanin", "Kanan", false, "a different taught word"),
        new Case("bapon", "Sabon", false, "two consonants wrong on a short word"),
        new Case("sapatas","Sapatos", false, "wrong vowel on a long word - still wrong"),

        // Correct answers must keep working, including the ways the recognizer
        // mangles a one-word reply.
        new Case("kuha",      "Kuha",  true, "said plainly"),
        new Case("Kuha mo",   "Kuha",  true, "recognizer added a filler word"),
        new Case("KUHA!",     "Kuha",  true, "caps and punctuation"),
        new Case("  kuha  ",  "Kuha",  true, "surrounding whitespace"),
        new Case("po kuha",   "Kuha",  true, "filler in front"),
        new Case("ang lakad na", "Lakad", true, "filler on both sides"),
        new Case("midyas",    "Medyas", true, "e/i alternation, which is free in Filipino"),
        new Case("inum",      "Inom",   true, "o/u alternation, also free"),
        new Case("twalya",    "Tuwalya", true, "the uw/w glide"),
        new Case("u po",      "Upo",    true, "recognizer split one word in two"),
        new Case("oppo",      "Upo",    true, "doubled letter"),
        new Case("kamusta",   "Kumusta", true, "the other standard spelling"),
        new Case("i love",    "Ilaw",   true, "listed English mistranscription"),
        new Case("lines",     "Linis",  true, "listed English mistranscription"),
        new Case("sipilio",   "Sipilyo", true, "listed variant"),
        new Case("sapatol",   "Sapatos", true, "one consonant garbled on a 7+ letter word, vowels intact"),

        // Nothing at all, and things that are not the word.
        new Case("mali",        "Kuha", false, "an unrelated word"),
        new Case("hello world", "Kain", false, "what the in-editor recognizer stub returns"),
        new Case("",            "Kuha", false, "empty"),
        new Case("   ",         "Kuha", false, "whitespace only"),
    };

    [MenuItem("Tools/SALINLAHI/Check Pronunciation Matching")]
    public static void Run()
    {
        string[] words = WordVariants.Words.OrderBy(w => w).ToArray();
        StringBuilder log = new StringBuilder();
        int failures = 0;

        log.AppendLine("SALINLAHI pronunciation matching check");
        log.AppendLine("======================================");
        log.AppendLine(words.Length + " taught words");
        log.AppendLine();

        failures += CheckCanonicalFormsAreDistinct(words, log);
        failures += CheckEachWordMatchesItself(words, log);
        failures += CheckNoWordMatchesAnotherWord(words, log);
        failures += CheckVariantsAreNotOtherWords(words, log);
        failures += CheckVariantsMatchTheirOwnWord(words, log);
        failures += CheckRegressionCases(log);

        ReportObjectiveWordsWithoutVariants(log);

        log.AppendLine();
        log.AppendLine(failures == 0
            ? "RESULT: all checks passed."
            : "RESULT: " + failures + " failure(s) - see the lines marked FAIL above.");

        if (failures == 0) Debug.Log(log.ToString());
        else Debug.LogError(log.ToString());
    }

    // 1 -----------------------------------------------------------------------
    // Exact matching on the canonical form is only safe if no two taught words
    // canonicalize to the same thing. If this ever fails, a fold in
    // PronunciationChecker.CanonicalForm is too aggressive for this vocabulary.
    private static int CheckCanonicalFormsAreDistinct(string[] words, StringBuilder log)
    {
        Dictionary<string, List<string>> byCanonical = new Dictionary<string, List<string>>();
        foreach (string word in words)
        {
            string canonical = PronunciationChecker.Canonicalize(word);
            if (!byCanonical.ContainsKey(canonical)) byCanonical[canonical] = new List<string>();
            byCanonical[canonical].Add(word);
        }

        List<string> collisions = byCanonical
            .Where(pair => pair.Value.Count > 1)
            .Select(pair => pair.Key + " <- " + string.Join(", ", pair.Value.ToArray()))
            .ToList();

        if (collisions.Count == 0)
        {
            log.AppendLine("ok   1. " + words.Length + " words give " + byCanonical.Count + " distinct canonical forms");
            return 0;
        }

        log.AppendLine("FAIL 1. two taught words share a canonical form, so they cannot be told apart:");
        foreach (string collision in collisions) log.AppendLine("        " + collision);
        return 1;
    }

    // 2 -----------------------------------------------------------------------
    private static int CheckEachWordMatchesItself(string[] words, StringBuilder log)
    {
        List<string> broken = words.Where(w => !PronunciationChecker.IsCorrect(w, w)).ToList();

        if (broken.Count == 0)
        {
            log.AppendLine("ok   2. every word is accepted when said exactly");
            return 0;
        }

        log.AppendLine("FAIL 2. a word is not accepted even when said perfectly: " + string.Join(", ", broken.ToArray()));
        return 1;
    }

    // 3 -----------------------------------------------------------------------
    // The important one. Every word against every other word.
    private static int CheckNoWordMatchesAnotherWord(string[] words, StringBuilder log)
    {
        List<string> accepted = new List<string>();

        foreach (string spoken in words)
            foreach (string target in words)
            {
                if (spoken == target) continue;
                if (PronunciationChecker.IsCorrect(spoken, target))
                    accepted.Add("said \"" + spoken + "\" -> accepted as \"" + target + "\"");
            }

        int pairs = words.Length * (words.Length - 1);
        if (accepted.Count == 0)
        {
            log.AppendLine("ok   3. " + pairs + " cross pairs checked, no taught word is accepted for another");
            return 0;
        }

        log.AppendLine("FAIL 3. one taught word is accepted for another (" + accepted.Count + " of " + pairs + "):");
        foreach (string line in accepted) log.AppendLine("        " + line);
        return 1;
    }

    // 4 -----------------------------------------------------------------------
    // A variant must never BE another taught word. Listing "kanen" under
    // "Kanan" would score a child's correct "Kanin" as tama for Kanan.
    private static int CheckVariantsAreNotOtherWords(string[] words, StringBuilder log)
    {
        List<string> clashes = new List<string>();

        foreach (string word in words)
        {
            string[] variants = WordVariants.For(word);
            if (variants == null) continue;

            foreach (string variant in variants)
            {
                string canonicalVariant = PronunciationChecker.Canonicalize(variant);
                foreach (string other in words)
                {
                    if (string.Equals(other, word, System.StringComparison.OrdinalIgnoreCase)) continue;
                    if (canonicalVariant == PronunciationChecker.Canonicalize(other))
                        clashes.Add("\"" + variant + "\" is listed under \"" + word + "\" but IS the taught word \"" + other + "\"");
                }
            }
        }

        if (clashes.Count == 0)
        {
            log.AppendLine("ok   4. no variant is itself another taught word");
            return 0;
        }

        log.AppendLine("FAIL 4. remove these entries from WordVariants:");
        foreach (string clash in clashes) log.AppendLine("        " + clash);
        return 1;
    }

    // 5 -----------------------------------------------------------------------
    // Catches a typo'd entry that silently does nothing.
    private static int CheckVariantsMatchTheirOwnWord(string[] words, StringBuilder log)
    {
        List<string> dead = new List<string>();
        int total = 0;

        foreach (string word in words)
        {
            string[] variants = WordVariants.For(word);
            if (variants == null) continue;

            foreach (string variant in variants)
            {
                total++;
                if (!PronunciationChecker.IsCorrect(variant, word))
                    dead.Add("\"" + variant + "\" under \"" + word + "\"");
            }
        }

        if (dead.Count == 0)
        {
            log.AppendLine("ok   5. all " + total + " listed variants are accepted for their own word");
            return 0;
        }

        log.AppendLine("FAIL 5. these variants are listed but never match anything:");
        foreach (string entry in dead) log.AppendLine("        " + entry);
        return 1;
    }

    // 6 -----------------------------------------------------------------------
    private static int CheckRegressionCases(StringBuilder log)
    {
        List<string> wrong = new List<string>();

        foreach (Case c in RegressionCases)
        {
            bool actual = PronunciationChecker.IsCorrect(c.Spoken, c.Target);
            if (actual != c.ShouldBeCorrect)
            {
                wrong.Add("said \"" + c.Spoken + "\" for \"" + c.Target + "\" -> " +
                          (actual ? "tama" : "mali") + ", expected " +
                          (c.ShouldBeCorrect ? "tama" : "mali") + "  (" + c.Why + ")");
            }
        }

        if (wrong.Count == 0)
        {
            log.AppendLine("ok   6. all " + RegressionCases.Length + " named cases behave as expected");
            return 0;
        }

        log.AppendLine("FAIL 6. " + wrong.Count + " of " + RegressionCases.Length + " named cases are wrong:");
        foreach (string line in wrong) log.AppendLine("        " + line);
        return 1;
    }

    // Informational ----------------------------------------------------------
    // Only sees the scene that happens to be open, so this is a note rather
    // than a failure - a word with no entry still works, it just gets no
    // curated mistranscriptions.
    private static void ReportObjectiveWordsWithoutVariants(StringBuilder log)
    {
        SceneObjectiveController[] controllers = Object.FindObjectsByType<SceneObjectiveController>();

        if (controllers == null || controllers.Length == 0) return;

        List<string> missing = new List<string>();
        foreach (SceneObjectiveController controller in controllers)
        {
            if (controller.objectives == null) continue;
            foreach (SceneObjectiveController.SceneObjective objective in controller.objectives)
            {
                if (objective == null || string.IsNullOrEmpty(objective.word)) continue;
                if (WordVariants.For(objective.word) == null && !missing.Contains(objective.word))
                    missing.Add(objective.word);
            }
        }

        log.AppendLine();
        log.AppendLine(missing.Count == 0
            ? "note  every objective word in the open scene has a WordVariants entry"
            : "note  no WordVariants entry yet for: " + string.Join(", ", missing.ToArray()) +
              " (they still work, they just get no curated mistranscriptions)");
    }
}
