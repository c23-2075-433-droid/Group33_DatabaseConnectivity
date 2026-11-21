using System.Globalization;
using System.Text;

/// <summary>
/// Which syllable of a taught word carries the stress, for the words whose
/// meaning depends on it (tayô "stand up" against táyo "we").
///
/// WHERE THE ANSWER COMES FROM
///
/// Nothing is hardcoded per word here. The stress is read off the marked
/// spelling WordStress keeps for every taught word (from the group's
/// Vocabulary Validation Form), using the ordinary rules of Filipino stress
/// marks:
///
///     á  (pahilís)   this vowel is stressed
///     â  (pakupyâ)   this vowel is stressed, and ends in a glottal stop
///     à  (paiwà)     the word ends in a glottal stop but the stress is on
///                    the syllable BEFORE it - so it marks no stressed vowel
///     no mark        malumay: stress on the second-to-last syllable
///
/// So "Tayô" says final stress, "Bása" first-syllable stress and "Bátì" first
/// syllable too (the grave on the i only marks the glottal stop). Adding a word
/// to WordStress's pronunciation table is all it takes for the game to start
/// checking its stress.
///
/// Syllables are counted as vowels, one each. That is how Filipino spelling
/// works: two vowels side by side are two syllables with a glottal stop between
/// them (ka-in, ba-it), and y and w are consonants (ta-yo, tu-wal-ya).
///
/// A word whose stress WordStress does not give (or with one syllable) gets
/// null from For: its stress is not checked, and its letters decide alone.
/// </summary>
public sealed class StressPattern
{
    /// <summary>The taught word, as SceneObjective spells it.</summary>
    public string Word { get; private set; }

    /// <summary>The marked spelling the pattern was read from, e.g. "Tayô".</summary>
    public string MarkedSpelling { get; private set; }

    /// <summary>How many syllables the word has.</summary>
    public int SyllableCount { get; private set; }

    /// <summary>Zero-based index of the stressed syllable.</summary>
    public int StressedSyllable { get; private set; }

    /// <summary>True when the stress is on the last syllable (tayô, upô, bukás, talón).</summary>
    public bool IsFinal { get { return StressedSyllable == SyllableCount - 1; } }

    /// <summary>True when the stress is on the first syllable (bása, bátì).</summary>
    public bool IsInitial { get { return StressedSyllable == 0; } }

    /// <summary>
    /// The word split into syllables with the stressed one in capitals:
    /// "ta-YÔ", "BÁ-sa". Used for the on-screen hint and the console, so a
    /// tester can see what the game was listening for.
    /// </summary>
    public string Spoken { get; private set; }

    /// <summary>
    /// The stress pattern of a word whose meaning depends on its stress, or
    /// null for every other word (whose stress the game does not check).
    /// </summary>
    public static StressPattern For(string word)
    {
        string marked = WordStress.PronunciationOf(word);
        if (marked == null) return null;
        StressPattern p = Parse(word.Trim(), marked);
        if (p != null) p.IsMinimalPair = WordStress.IsMarked(word);
        return p;
    }

    /// <summary>
    /// True for the words that become ANOTHER word when the stress moves
    /// (tayô / táyo). Their meaning is at stake, so AnswerJudge treats them
    /// more strictly than a word whose wrong stress is only a mispronunciation.
    /// </summary>
    public bool IsMinimalPair { get; private set; }

    /// <summary>
    /// Reads the pattern off any marked spelling. Public for the editor
    /// self-check and the stand-alone tests; the game goes through For.
    /// Returns null if the spelling has fewer than two syllables, since a
    /// one-syllable word has no stress to put in the wrong place.
    /// </summary>
    public static StressPattern Parse(string word, string marked)
    {
        if (string.IsNullOrEmpty(marked)) return null;

        string decomposed = marked.Normalize(NormalizationForm.FormD);

        int syllables = 0;
        int stressed = -1;
        bool lastWasVowel = false;
        // Each vowel starts a new syllable; the consonants before it belong to it.
        var syllableText = new System.Collections.Generic.List<StringBuilder>();
        StringBuilder pendingOnset = new StringBuilder();

        for (int i = 0; i < decomposed.Length; i++)
        {
            char c = decomposed[i];
            UnicodeCategory cat = CharUnicodeInfo.GetUnicodeCategory(c);

            if (cat == UnicodeCategory.NonSpacingMark)
            {
                // A mark belongs to the vowel just before it.
                if (lastWasVowel && (c == '́' || c == '̂')) stressed = syllables - 1;
                if (syllableText.Count > 0) syllableText[syllableText.Count - 1].Append(c);
                continue;
            }

            if (!char.IsLetter(c)) { lastWasVowel = false; continue; }

            if (IsVowel(c))
            {
                syllables++;
                StringBuilder s = new StringBuilder();
                s.Append(pendingOnset).Append(c);
                pendingOnset.Length = 0;
                syllableText.Add(s);
                lastWasVowel = true;
            }
            else
            {
                // A consonant is the coda of the syllable before it unless a
                // vowel follows, in which case it is the onset of the next.
                bool vowelNext = NextLetterIsVowel(decomposed, i + 1);
                // "ng" is one letter in Filipino, and starts a syllable as one:
                // ba-ngon, ti-ngin, ta-nong - not ban-gon.
                if (!vowelNext && char.ToLowerInvariant(c) == 'n' && i + 1 < decomposed.Length &&
                    char.ToLowerInvariant(decomposed[i + 1]) == 'g' && NextLetterIsVowel(decomposed, i + 2))
                    vowelNext = true;
                if (vowelNext || syllableText.Count == 0) pendingOnset.Append(c);
                else syllableText[syllableText.Count - 1].Append(c);
                lastWasVowel = false;
            }
        }

        if (pendingOnset.Length > 0 && syllableText.Count > 0)
            syllableText[syllableText.Count - 1].Append(pendingOnset);

        if (syllables < 2) return null;

        // No acute or circumflex anywhere: malumay or malumi, both of which put
        // the stress on the second-to-last syllable.
        if (stressed < 0) stressed = syllables - 2;

        StringBuilder spoken = new StringBuilder();
        for (int i = 0; i < syllableText.Count; i++)
        {
            if (i > 0) spoken.Append('-');
            string part = syllableText[i].ToString().Normalize(NormalizationForm.FormC);
            spoken.Append(i == stressed ? part.ToUpperInvariant() : part.ToLowerInvariant());
        }

        return new StressPattern
        {
            Word = word,
            MarkedSpelling = marked,
            SyllableCount = syllables,
            StressedSyllable = stressed,
            Spoken = spoken.ToString(),
        };
    }

    private static bool IsVowel(char c)
    {
        switch (char.ToLowerInvariant(c))
        {
            case 'a': case 'e': case 'i': case 'o': case 'u': return true;
            default: return false;
        }
    }

    private static bool NextLetterIsVowel(string s, int from)
    {
        for (int i = from; i < s.Length; i++)
        {
            char c = s[i];
            if (CharUnicodeInfo.GetUnicodeCategory(c) == UnicodeCategory.NonSpacingMark) continue;
            if (!char.IsLetter(c)) return false;
            return IsVowel(c);
        }
        return false;
    }

    public override string ToString()
    {
        return Word + " (" + Spoken + ")";
    }
}
