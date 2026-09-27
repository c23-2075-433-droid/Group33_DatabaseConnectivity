using System;
using System.Text;
using System.Text.RegularExpressions;

/// <summary>
/// Decides whether what the player SAID matches the Tagalog word they were
/// asked to say. This is the missing piece the vocabulary system needs:
/// VoiceCommand.cs currently only checks for a handful of hardcoded command
/// words (bangon/tayo/lakad/talon) with a simple "contains" check — there's
/// no notion of "right" vs "wrong" for a word the player is being taught.
///
/// This is a plain C# utility (no MonoBehaviour, no scene wiring needed) so
/// it can be called from anywhere: VoiceCommand's OnResultReceived,
/// SceneObjectiveController, a future NarratorController, etc.
///
/// Matching is deliberately forgiving, because on-device speech recognition
/// for Tagalog is noisy (see the "Risks" section of the production plan):
///   1. Normalize both strings (lowercase, trim, strip punctuation/extra spaces).
///   2. Exact match after normalization -> correct.
///   3. Otherwise check against any accepted alternate spellings/mishearings
///      you supply for that word (e.g. "opo" vs "oho").
///   4. Otherwise allow small edit-distance (Levenshtein) typos/mishearings,
///      scaled to the word's length, so a 3-letter word needs a near-exact
///      match but a longer word tolerates one or two garbled letters.
/// </summary>
public static class PronunciationChecker
{
    /// <summary>
    /// Returns true if spokenText is close enough to targetWord (or one of
    /// acceptedVariants) to count as the player having said it correctly.
    /// </summary>
    /// <param name="spokenText">Raw text returned by SpeechToText.</param>
    /// <param name="targetWord">The Tagalog word the player was asked to say.</param>
    /// <param name="acceptedVariants">
    /// Optional extra spellings/near-misses that should also count as correct
    /// (e.g. common recognizer mistranscriptions for that specific word).
    /// </param>
    /// <param name="maxEditDistanceOverride">
    /// Optional fixed tolerance. Leave -1 to auto-scale from word length
    /// (short words need to be closer; longer words get more slack).
    /// </param>
    public static bool IsCorrect(
        string spokenText,
        string targetWord,
        string[] acceptedVariants = null,
        int maxEditDistanceOverride = -1)
    {
        if (string.IsNullOrWhiteSpace(spokenText) || string.IsNullOrWhiteSpace(targetWord))
            return false;

        string spoken = Normalize(spokenText);
        string target = Normalize(targetWord);

        // A phrase like "opo po" should still count if it CONTAINS the exact
        // target word as a whole token, not just a fuzzy match on the whole phrase.
        if (ContainsWholeWord(spoken, target))
            return true;

        int tolerance = maxEditDistanceOverride >= 0
            ? maxEditDistanceOverride
            : AutoTolerance(target.Length);

        if (LevenshteinDistance(spoken, target) <= tolerance)
            return true;

        // Also try matching against each individual word in the spoken phrase,
        // in case the recognizer picked up extra filler words around it.
        foreach (string token in spoken.Split(' '))
        {
            if (token.Length == 0) continue;
            if (token == target) return true;
            if (LevenshteinDistance(token, target) <= tolerance) return true;
        }

        if (acceptedVariants != null)
        {
            foreach (string variant in acceptedVariants)
            {
                if (string.IsNullOrWhiteSpace(variant)) continue;
                string normVariant = Normalize(variant);
                if (ContainsWholeWord(spoken, normVariant)) return true;

                int variantTolerance = maxEditDistanceOverride >= 0
                    ? maxEditDistanceOverride
                    : AutoTolerance(normVariant.Length);
                if (LevenshteinDistance(spoken, normVariant) <= variantTolerance) return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Lowercases, trims, collapses whitespace, and strips punctuation so
    /// "Bahay!" ", bahay" and "  Bahay " all normalize the same way.
    /// </summary>
    private static string Normalize(string text)
    {
        string lowered = text.ToLowerInvariant().Trim();
        string noPunctuation = Regex.Replace(lowered, @"[^\p{L}\p{N}\s]", "");
        string collapsed = Regex.Replace(noPunctuation, @"\s+", " ").Trim();
        return collapsed;
    }

    private static bool ContainsWholeWord(string haystack, string word)
    {
        if (word.Length == 0) return false;
        return Regex.IsMatch(haystack, $@"(?:^|\s){Regex.Escape(word)}(?:$|\s)");
    }

    /// <summary>
    /// Shorter words must be matched more strictly (a 1-letter slip on a
    /// 3-letter word changes the word entirely); longer words can absorb a
    /// couple of garbled letters from a noisy recognizer.
    /// </summary>
    private static int AutoTolerance(int wordLength)
    {
        if (wordLength <= 3) return 0;
        if (wordLength <= 6) return 1;
        return 2;
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
