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
// taught word, every pair of them, none of which may be accepted. That is the
// regression guard for the bug this tool was written for - "kaha" being scored
// tama for "Kuha" - and for its worse cousin, "Kanin" being accepted for
// "Kain" when both are taught in the same breakfast scene.
//
// Check #7 is the guard for the second bug found here: a curated variant that
// is a split word ("sa got", "ba ti", "u po") used to be canonicalized with
// its filler-looking syllable thrown away, leaving a bare "gut", "ti" or "u"
// that any single word the child said would match. #7 says every piece of
// every variant outright, and requires all of them to be scored mali.
//
// Check #8 is the other half of that: a variant must be the WHOLE utterance,
// so the editor's "Hello world" stub can no longer pass as Ilaw on the
// strength of the "hello" listed under it.

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

        // Pagkain and Kain are asked one after the other in the breakfast
        // scene, and "kain" is literally inside "pagkain" - so the recognizer
        // splitting the longer word must not answer the shorter one.
        new Case("pagkain",  "Kain",    false, "the whole word is not the half of it"),
        new Case("pag kain", "Kain",    false, "the recognizer split Pagkain, same scene, next word"),
        new Case("kain",     "Pagkain", false, "half the word is not the whole of it"),
        new Case("pag kain", "Pagkain", true,  "the same split, read as the word it came from"),
        new Case("pakain",   "Pagkain", true,  "one consonant dropped on a 7-letter word, vowels intact"),
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
        new Case("ka nin",    "Kunin",  false, "a split 'Kanin' is not 'Kunin'"),
        new Case("ti ngin",   "Bati",   false, "a split 'Tingin' is not 'Bati'"),
        new Case("oppo",      "Upo",    true, "doubled letter"),
        new Case("kamusta",   "Kumusta", true, "the other standard spelling"),
        new Case("i love",    "Ilaw",   true, "listed English mistranscription"),
        new Case("lines",     "Linis",  true, "listed English mistranscription"),
        new Case("sipilio",   "Sipilyo", true, "listed variant"),
        new Case("sapatol",   "Sapatos", true, "one consonant garbled on a 7+ letter word, vowels intact"),

        // One syllable of the word is not the word. Each of these was scored
        // tama before variants were canonicalized whole: the variant "sa got"
        // lost its "sa" to the filler list and became a bare "gut".
        new Case("u",       "Upo",    false, "the head of the 'u po' variant, alone"),
        new Case("ti",      "Bati",   false, "the tail of 'ba ti', alone"),
        new Case("ha",      "Kuha",   false, "the tail of 'ku ha', alone"),
        new Case("nin",     "Kunin",  false, "the tail of 'ku nin', alone"),
        new Case("gut",     "Sagot",  false, "the tail of 'sa got', alone"),
        new Case("kay",     "Sakay",  false, "the tail of 'sa kay', alone"),
        new Case("bun",     "Sabon",  false, "the tail of 'sa bon', alone"),
        new Case("ngun",    "Bangon", false, "the tail of 'ba ngon', alone"),
        new Case("ku",      "Kuha",   false, "the head of 'ku ha', alone"),
        new Case("u wi",    "Upo",    false, "Uwi's variant is not an answer to Upo"),
        new Case("u wi",    "Uwi",    true,  "but it IS Uwi, split in two"),
        new Case("u ano",   "Upo",    false, "a fragment with a filler after it"),
        new Case("ha ha",   "Kuha",   false, "laughing is not Kuha"),
        new Case("si ti",   "Bati",   false, "a filler and a fragment"),

        // The flip side: trimming filler only off the ENDS means a split word
        // still counts when the recognizer wraps it in one.
        new Case("ba ngon po", "Bangon",  true, "split word, and polite"),
        new Case("ang sa got", "Sagot",   true, "filler first, then the split word"),
        new Case("ku ha mo",   "Kuha",    true, "split word, filler after"),
        new Case("u po po",    "Upo",     true, "split word plus a stray filler"),
        new Case("sa patol po","Sapatos", true, "split long word, one consonant garbled"),
        new Case("ngon po",    "Bangon",  false, "the tail of the word, even so"),
        new Case("patos po",   "Sapatos", false, "the tail of a long word, even so"),

        // A curated mistranscription has to be the whole thing the recognizer
        // returned, not one word inside it. "hello" on its own is a misheard
        // Ilaw and is listed as one; "hello world" is the in-editor stub, and
        // it was being scored tama for Ilaw off its first token alone - twice
        // in the attempt log, both in Banyo.
        new Case("hello",       "Ilaw", true,  "listed mistranscription, said alone"),
        new Case("hello po",    "Ilaw", true,  "the same, with a filler after it"),
        new Case("hello world", "Ilaw", false, "the editor stub is not an answer"),
        new Case("world hello", "Ilaw", false, "nor in the other order"),
        new Case("books",       "Bukas", true,  "listed mistranscription, said alone"),
        new Case("books are fun", "Bukas", false, "but not as one word of a sentence"),

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
        failures += CheckNoVariantPieceIsAccepted(words, log);
        failures += CheckVariantsNeedTheWholeUtterance(words, log);

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

    // 7 -----------------------------------------------------------------------
    // Most curated variants are split words - "sa got", "ba ti", "u po" - and
    // the child has to say the WHOLE word, not one syllable of it. This takes
    // every variant apart and insists that each piece on its own is mali.
    //
    // A piece that happens to be the taught word itself is skipped, because it
    // is a correct answer: "salamat po" has "salamat" in it.
    private static int CheckNoVariantPieceIsAccepted(string[] words, StringBuilder log)
    {
        List<string> accepted = new List<string>();
        int pieces = 0;

        foreach (string word in words)
        {
            string[] variants = WordVariants.For(word);
            if (variants == null) continue;

            string wholeWord = PronunciationChecker.Canonicalize(word);

            foreach (string variant in variants)
            {
                string[] parts = PronunciationChecker.Canonicalize(variant).Split(' ');
                if (parts.Length < 2) continue;

                foreach (string part in parts)
                {
                    if (part.Length == 0 || part == wholeWord) continue;
                    pieces++;
                    if (PronunciationChecker.IsCorrect(part, word))
                        accepted.Add("\"" + part + "\" (one piece of \"" + variant +
                                     "\") is accepted for \"" + word + "\"");
                }
            }
        }

        if (accepted.Count == 0)
        {
            log.AppendLine("ok   7. none of the " + pieces + " variant syllables is accepted on its own");
            return 0;
        }

        log.AppendLine("FAIL 7. a syllable is being accepted as the whole word:");
        foreach (string line in accepted) log.AppendLine("        " + line);
        return 1;
    }

    // 8 -----------------------------------------------------------------------
    // A variant describes a whole recognizer result, so padding one with an
    // extra word must not still be accepted. Checked by appending a word that
    // is in no table at all: if "hello" is listed under Ilaw, then
    // "hello zzgrunt" must be mali.
    //
    // A variant that CONTAINS the taught word is skipped - "salamat po" has
    // "salamat" in it, and a child who says the word in the middle of a
    // sentence is answering correctly whatever else the recognizer heard.
    // That allowance belongs to the taught word, and #3 is what guards it.
    private static int CheckVariantsNeedTheWholeUtterance(string[] words, StringBuilder log)
    {
        const string Padding = "zzgrunt";
        List<string> accepted = new List<string>();
        int tested = 0;

        foreach (string word in words)
        {
            string[] variants = WordVariants.For(word);
            if (variants == null) continue;

            string wholeWord = PronunciationChecker.Canonicalize(word);

            foreach (string variant in variants)
            {
                if (System.Array.IndexOf(
                        PronunciationChecker.Canonicalize(variant).Split(' '), wholeWord) >= 0)
                    continue;

                tested++;
                if (PronunciationChecker.IsCorrect(variant + " " + Padding, word))
                    accepted.Add("\"" + variant + " " + Padding + "\" is accepted for \"" + word + "\"");
            }
        }

        if (accepted.Count == 0)
        {
            log.AppendLine("ok   8. none of the " + tested + " variants is accepted with a stray word beside it");
            return 0;
        }

        log.AppendLine("FAIL 8. a variant is matching one word inside a longer result:");
        foreach (string line in accepted) log.AppendLine("        " + line);
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
