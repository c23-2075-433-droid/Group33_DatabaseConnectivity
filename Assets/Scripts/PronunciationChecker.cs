using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

/// <summary>
/// Decides whether what the player SAID matches the Tagalog word they were
/// asked to say. This is the right/wrong judgement the vocabulary system runs
/// on: VoiceCommand hands it every recognizer result while an objective is
/// live, and SceneObjectiveController advances on the answer.
///
/// This is a plain C# utility (no MonoBehaviour, no scene wiring needed) so
/// it can be called from anywhere: VoiceCommand's OnResultReceived,
/// SceneObjectiveController, the editor self-check tool, etc.
///
/// WHY IT WORKS THE WAY IT DOES
///
/// The obvious approach - allow a small edit distance, scaled to the word's
/// length - is what this used to do, and it was wrong for Filipino. Tagalog
/// words are short and are told apart by single vowels, so "one edit of slack"
/// on a 4-6 letter word accepts every possible vowel error:
///
///     "kaha"  for "Kuha"   - one substitution, was scored CORRECT
///     "kanin" for "Kain"   - one insertion, and BOTH are taught in the
///                            Almusal scene, so each was accepted for the other
///     "tuwa"/"Tawa", "tabo"/"Takbo", "kanin"/"Kanan" - the same story
///
/// A game that teaches pronunciation cannot reward a wrong vowel. So instead of
/// tolerating arbitrary edits, this canonicalizes both sides - folding ONLY the
/// variation that is genuinely free in Filipino - and then requires an exact
/// match:
///
///   1. e/i and o/u are folded together. These really do alternate
///      (babae/babai, noon/nuon) and the recognizer returns either spelling.
///      a/u never alternates, which is exactly why "kaha" now fails.
///   2. Spanish-era spellings are mapped to their Filipino sounds (c/qu -> k,
///      f -> p, v -> b, j -> h, z -> s, ll -> ly, x -> ks), since the
///      recognizer may write a Filipino word the Spanish way.
///   3. The uw/w glide is folded, so "tuwalya" and "twalya" are one word.
///   4. Doubled letters are collapsed ("oppo" -> "upu" matches "Upo").
///   5. Filler words the recognizer tacks on ("po", "mo", "na"...) are trimmed
///      off the ENDS of the utterance, never out of its middle - see
///      WholeFormsOf. Half the filler list is also a Filipino first syllable
///      (sa, si, ku, ba, mo, po, na), so dropping them anywhere would leave a
///      fragment behind, and fragments are what this checker exists to reject.
///   6. Each word's known mistranscriptions are accepted outright - see
///      WordVariants, which is the layer that actually buys accuracy on a real
///      device, and which grows from the attempt log rather than from guesses.
///      A variant is canonicalized WHOLE, with every one of its tokens kept:
///      the curated list is full of split-word forms ("sa got", "ku ha",
///      "ba ti"), and filler-stripping those collapsed them to a bare
///      syllable - "gut", "ha", "ti" - which then matched any single word the
///      child said. Saying one syllable of the word is not saying the word.
///   7. A reply the recognizer split into pieces is read as ONE word when
///      running it together spells another taught word: "pag kain" is
///      "Pagkain" badly split, so the "kain" in it is not an answer to
///      "Kain". Pagkain and Kain are asked one after the other in the
///      breakfast scene, which is where this would otherwise be felt.
///
/// Folding the taught words this way leaves every one of them with a DISTINCT
/// canonical form, so exact matching can never make two of them ambiguous. The
/// editor tool Tools > SALINLAHI > Check Pronunciation Matching proves that
/// every time it runs, by testing every cross pair of the words in
/// WordVariants - and, since the second bug found here, every syllable of every
/// variant on its own. No count is written down on purpose: it moves when the
/// word list does, so read it off the tool's own output (or the scenes, via
/// Tools/completeness_report.py).
///
/// The one remaining piece of fuzziness is deliberate and narrow: on a word of
/// 7+ canonical letters, ONE consonant may be garbled, as long as every vowel
/// still lines up. That absorbs genuine recognizer noise on the long words
/// (Ibahagi, Kumusta, Salamat, Sapatos, Sipilyo, Dausdos) while leaving a vowel
/// error impossible to pass.
/// </summary>
public static class PronunciationChecker
{
    /// <summary>
    /// Shortest canonical word that may absorb one garbled consonant. Below
    /// this length a single consonant IS the word, so no slack is given.
    /// </summary>
    private const int ConsonantSlipMinLength = 7;

    /// <summary>
    /// Words the recognizer sprinkles around a one-word answer. Trimmed off
    /// the ends of the utterance before matching, so "Kuha mo" and "ang lakad
    /// na" still count. Canonicalized on startup, so this list is written the
    /// way people spell it.
    /// </summary>
    private static readonly string[] RawFillerWords =
    {
        "po", "mo", "na", "ang", "ay", "yung", "ano", "nga", "ba", "daw",
        "eh", "oh", "ok", "okay", "uh", "um", "hmm", "si", "ni", "sa", "ko",
    };

    // Declared after RawFillerWords on purpose: static initializers run in
    // declaration order, and this one canonicalizes that list.
    private static readonly HashSet<string> FillerWords = BuildFillerWords();

    /// <summary>
    /// Returns true if spokenText is the player having said targetWord.
    /// </summary>
    /// <param name="spokenText">Raw text returned by SpeechToText.</param>
    /// <param name="targetWord">The Tagalog word the player was asked to say.</param>
    /// <param name="acceptedVariants">
    /// Extra mistranscriptions to accept for this word, from the objective's
    /// Inspector field. WordVariants is consulted as well, so a scene built
    /// before that table existed still gets its entries.
    /// </param>
    public static bool IsCorrect(
        string spokenText,
        string targetWord,
        string[] acceptedVariants = null)
    {
        if (string.IsNullOrWhiteSpace(spokenText) || string.IsNullOrWhiteSpace(targetWord))
            return false;

        Utterance said = Utterance.Of(spokenText);
        if (said.IsEmpty) return false;

        if (said.Matches(Canonicalize(targetWord)))
            return true;

        // A variant is just another spelling that counts as the word, so it is
        // matched exactly like the target itself - and canonicalized exactly
        // like it, whole, keeping every token it was written with.
        if (MatchesAnyVariant(said, acceptedVariants)) return true;
        if (MatchesAnyVariant(said, WordVariants.For(targetWord))) return true;

        return false;
    }

    /// <summary>
    /// The canonical form of a word or phrase - what matching actually compares.
    /// Every token is kept, filler words included, because this is the form a
    /// target word and its variants are compared AS: a two-token variant like
    /// "u po" stays two tokens, and so is answered only by the whole thing.
    ///
    /// Public because the attempt log records it next to the raw recognizer
    /// text: seeing "kaha" vs "kuha" side by side is how a mishearing gets
    /// spotted and added to WordVariants.
    /// </summary>
    public static string Canonicalize(string text)
    {
        return CanonicalForm(text);
    }

    private static bool MatchesAnyVariant(Utterance said, string[] variants)
    {
        if (variants == null) return false;

        foreach (string variant in variants)
        {
            if (string.IsNullOrWhiteSpace(variant)) continue;
            if (said.Matches(Canonicalize(variant))) return true;
        }
        return false;
    }

    /// <summary>
    /// One recognizer result, canonicalized and held in the shapes matching
    /// compares against: the whole utterance with filler words trimmed off its
    /// ends (each window also run together, for a word the recognizer split),
    /// and the individual content words.
    /// </summary>
    private sealed class Utterance
    {
        /// <summary>Every canonical token, filler words included.</summary>
        private string[] allTokens;

        /// <summary>The tokens that are not filler words.</summary>
        private string[] contentTokens;

        /// <summary>
        /// Whole-utterance candidates: every edge-trimmed window, spaced and
        /// run together. A canonical form must equal one of these outright.
        /// </summary>
        private List<string> wholeForms;

        public bool IsEmpty { get { return allTokens.Length == 0; } }

        public static Utterance Of(string text)
        {
            Utterance said = new Utterance();
            Tokenize(text, out said.allTokens, out said.contentTokens);
            said.wholeForms = WholeFormsOf(said.allTokens);
            return said;
        }

        /// <summary>
        /// Compares this utterance against one canonical form (the target
        /// word, or one of its accepted variants).
        /// </summary>
        public bool Matches(string canonicalForm)
        {
            if (string.IsNullOrEmpty(canonicalForm)) return false;

            // The utterance as a whole, allowing the recognizer's filler words
            // to be trimmed off either end and allowing a word it split in two
            // to be run back together ("u po" -> "upu" for Upo).
            foreach (string form in wholeForms)
                if (form == canonicalForm) return true;

            // Any single word of it: the recognizer often returns a short
            // sentence when the child said one word. Only ever a whole word of
            // the utterance against a whole single-token form - a piece of
            // either side is not an answer.
            //
            // Unless the utterance run together IS some OTHER taught word, in
            // which case its pieces are halves of that word rather than an
            // answer. "pag kain" is the recognizer splitting "Pagkain", and
            // the "kain" inside it is not the child saying "Kain" - but both
            // are taught in the breakfast scene, one straight after the other,
            // so without this the second word could be answered by repeating
            // the first. The same guard stops "ka nin" passing for "Kunin"
            // and "ti ngin" for "Bati".
            if (canonicalForm.IndexOf(' ') < 0 &&
                !IsSomeOtherTaughtWord(string.Concat(allTokens), canonicalForm))
            {
                foreach (string token in contentTokens)
                    if (token == canonicalForm) return true;
            }

            // Long words only: one consonant garbled by the recognizer, with
            // every vowel still in place. This is what keeps "kaha" out of
            // "kuha" while still forgiving a smudged "Sapatos".
            if (canonicalForm.Length >= ConsonantSlipMinLength)
            {
                foreach (string token in contentTokens)
                    if (IsConsonantSlip(token, canonicalForm)) return true;

                foreach (string form in wholeForms)
                    if (form.IndexOf(' ') < 0 && IsConsonantSlip(form, canonicalForm)) return true;
            }

            return false;
        }

        /// <summary>
        /// Every way of reading the utterance as a single answer: the full run
        /// of tokens, and the runs left after dropping filler words from the
        /// FRONT and the BACK, each one both spaced and run together.
        ///
        /// Trimming only at the ends is the whole point. "ba", "sa", "si",
        /// "ku", "mo", "po" and "na" are filler words AND Filipino first
        /// syllables, so a rule that removed them wherever they appeared would
        /// turn "ba ngon" into "ngun" and "sa got" into "gut". Here the
        /// untrimmed run is always a candidate too, so "ba ngon po" still
        /// reads as Bangon - once with "ba" kept and "po" dropped.
        /// </summary>
        private static List<string> WholeFormsOf(string[] tokens)
        {
            List<string> forms = new List<string>();

            int afterLeadingFiller = 0;
            while (afterLeadingFiller < tokens.Length && FillerWords.Contains(tokens[afterLeadingFiller]))
                afterLeadingFiller++;

            int beforeTrailingFiller = tokens.Length;
            while (beforeTrailingFiller > 0 && FillerWords.Contains(tokens[beforeTrailingFiller - 1]))
                beforeTrailingFiller--;

            for (int start = 0; start <= afterLeadingFiller; start++)
            {
                for (int end = tokens.Length; end >= beforeTrailingFiller; end--)
                {
                    int count = end - start;
                    if (count < 1) continue;

                    Add(forms, string.Join(" ", tokens, start, count));
                    Add(forms, string.Join("", tokens, start, count));
                }
            }

            return forms;
        }

        private static void Add(List<string> forms, string form)
        {
            if (form.Length > 0 && !forms.Contains(form)) forms.Add(form);
        }
    }

    /// <summary>
    /// The canonical form of every taught word, for the fragment guard above.
    /// Built once from WordVariants, which is the project's list of the words
    /// the game actually asks for.
    /// </summary>
    private static readonly HashSet<string> TaughtForms = BuildTaughtForms();

    private static HashSet<string> BuildTaughtForms()
    {
        HashSet<string> set = new HashSet<string>();
        foreach (string word in WordVariants.Words)
        {
            string canonical = Canonicalize(word);
            if (canonical.Length > 0) set.Add(canonical);
        }
        return set;
    }

    /// <summary>
    /// True when a run-together utterance is a taught word OTHER than the one
    /// being matched - i.e. the child said a different word of the course, and
    /// the recognizer happened to split it across a token that looks like this
    /// one.
    /// </summary>
    private static bool IsSomeOtherTaughtWord(string joined, string canonicalForm)
    {
        if (string.IsNullOrEmpty(joined) || joined == canonicalForm) return false;
        return TaughtForms.Contains(joined);
    }

    /// <summary>
    /// True when two canonical forms differ by at most one letter AND their
    /// vowels are identical - i.e. the single difference is a consonant.
    /// </summary>
    private static bool IsConsonantSlip(string spoken, string target)
    {
        if (spoken.Length == 0) return false;
        if (VowelSkeleton(spoken) != VowelSkeleton(target)) return false;
        return LevenshteinDistance(spoken, target) <= 1;
    }

    /// <summary>
    /// Just the vowels, in order. After canonicalization only a, i and u are
    /// left, so this is the word's vowel pattern - the part the player has to
    /// get right.
    /// </summary>
    private static string VowelSkeleton(string canonical)
    {
        StringBuilder sb = new StringBuilder(canonical.Length);
        foreach (char c in canonical)
            if (c == 'a' || c == 'i' || c == 'u') sb.Append(c);
        return sb.ToString();
    }

    /// <summary>
    /// Splits an utterance into canonical tokens. allTokens keeps everything;
    /// contentTokens has the recognizer's filler words removed, and is only
    /// ever used to pick ONE whole word out of a longer sentence.
    /// </summary>
    private static void Tokenize(string text, out string[] allTokens, out string[] contentTokens)
    {
        string canonical = CanonicalForm(text);
        if (canonical.Length == 0)
        {
            allTokens = new string[0];
            contentTokens = allTokens;
            return;
        }

        allTokens = canonical.Split(' ');

        List<string> content = new List<string>(allTokens.Length);
        foreach (string token in allTokens)
            if (token.Length > 0 && !FillerWords.Contains(token)) content.Add(token);

        // If the player said nothing BUT filler, judge them on what they
        // actually said rather than on an empty string - otherwise a target
        // that happens to look like a filler word could never be answered.
        contentTokens = content.Count > 0 ? content.ToArray() : allTokens;
    }

    /// <summary>
    /// Folds away everything that varies freely in written Filipino, so that
    /// two spellings of the same word come out identical and two different
    /// words never do. See the class comment for why each rule is here.
    /// </summary>
    private static string CanonicalForm(string text)
    {
        if (string.IsNullOrEmpty(text)) return "";

        // Strip accents, so "ó" folds the same way as "o".
        string decomposed = text.ToLowerInvariant().Trim().Normalize(NormalizationForm.FormD);
        StringBuilder unaccented = new StringBuilder(decomposed.Length);
        foreach (char c in decomposed)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(c) == UnicodeCategory.NonSpacingMark) continue;
            unaccented.Append(c);
        }

        string s = Regex.Replace(unaccented.ToString(), @"[^\p{L}\p{N}\s]", "");

        // Spanish-era spellings for sounds Filipino writes differently; the
        // recognizer may emit either. Digraphs first, so "ll" doesn't lose its
        // second letter to the c/z/f/v/j pass or the doubled-letter pass below.
        s = s.Replace("qu", "k").Replace("ll", "ly").Replace("x", "ks");
        s = s.Replace('c', 'k').Replace('z', 's').Replace('f', 'p').Replace('v', 'b').Replace('j', 'h');

        // e/i and o/u genuinely alternate in Filipino and the recognizer picks
        // either. a/u never does, which is why "kaha" fails for "kuha".
        s = s.Replace('e', 'i').Replace('o', 'u');

        // The uw/w glide: "tuwalya" and "twalya" are the same word.
        s = s.Replace("uw", "w");

        // Doubled letters, which the recognizer adds ("oppo" for "Upo").
        s = Regex.Replace(s, @"(.)\1+", "$1");

        return Regex.Replace(s, @"\s+", " ").Trim();
    }

    private static HashSet<string> BuildFillerWords()
    {
        HashSet<string> set = new HashSet<string>();
        foreach (string filler in RawFillerWords)
        {
            string canonical = CanonicalForm(filler);
            if (canonical.Length > 0) set.Add(canonical);
        }
        return set;
    }

    /// <summary>Standard Levenshtein (single-character insert/delete/substitute) edit distance.</summary>
    private static int LevenshteinDistance(string a, string b)
    {
        int[,] d = new int[a.Length + 1, b.Length + 1];

        for (int i = 0; i <= a.Length; i++) d[i, 0] = i;
        for (int j = 0; j <= b.Length; j++) d[0, j] = j;

        for (int i = 1; i <= a.Length; i++)
        {
            for (int j = 1; j <= b.Length; j++)
            {
                int cost = a[i - 1] == b[j - 1] ? 0 : 1;
                d[i, j] = Math.Min(
                    Math.Min(d[i - 1, j] + 1, d[i, j - 1] + 1),
                    d[i - 1, j - 1] + cost);
            }
        }

        return d[a.Length, b.Length];
    }
}
