using System.Collections.Generic;

/// <summary>
/// Works out whether a nickname is already in use and, when it is, what to
/// offer instead - "Kylo" taken, so try "Kylo01".
///
/// Deliberately has no Unity and no network in it. Everything here is a plain
/// function of the name the child typed and the list of names already in the
/// database, so the rule can be read, reasoned about and tested on its own;
/// NicknameAvailability is the part that fetches the list and drives the UI.
///
/// Matching ignores case throughout. "kylo" and "Kylo" are the same child as
/// far as a six-year-old is concerned, and letting both exist would make the
/// recent-scores list look broken.
/// </summary>
public static class NicknameSuggester
{
    /// <summary>Matches InputField.characterLimit on the nickname box.</summary>
    public const int MaxLength = 16;

    /// <summary>Numbered suffixes are tried from here up, two digits wide.</summary>
    private const int FirstSuffix = 1;
    private const int LastSuffix = 99;

    /// <summary>True when something in <paramref name="taken"/> matches, ignoring case.</summary>
    public static bool IsTaken(string candidate, IEnumerable<string> taken)
    {
        if (string.IsNullOrWhiteSpace(candidate) || taken == null) return false;

        string needle = candidate.Trim();
        foreach (string name in taken)
        {
            if (name == null) continue;
            if (string.Equals(name.Trim(), needle, System.StringComparison.OrdinalIgnoreCase))
                return true;
        }
        return false;
    }

    /// <summary>
    /// The name with any trailing digits removed, which is what suggestions
    /// are built from. Without this, a child who is already "Kylo01" and
    /// collides would be offered "Kylo0101" rather than "Kylo02".
    /// A name that is nothing but digits keeps them - "123" has no stem, and
    /// returning "" there would lose the child's choice entirely.
    /// </summary>
    public static string StemOf(string nickname)
    {
        if (string.IsNullOrWhiteSpace(nickname)) return "";

        string trimmed = nickname.Trim();
        int end = trimmed.Length;
        while (end > 0 && char.IsDigit(trimmed[end - 1])) end--;

        return end == 0 ? trimmed : trimmed.Substring(0, end);
    }

    /// <summary>
    /// Up to <paramref name="count"/> free names close to the one the child
    /// wanted, best first. Returns an empty list only if there is nothing
    /// sensible to offer, which in practice means every suffix is used.
    /// </summary>
    public static List<string> Suggest(string desired, IEnumerable<string> taken, int count)
    {
        List<string> suggestions = new List<string>();
        if (count < 1) return suggestions;

        string stem = StemOf(desired);
        if (string.IsNullOrEmpty(stem)) stem = desired == null ? "" : desired.Trim();
        if (string.IsNullOrEmpty(stem)) return suggestions;

        // Leave room for the suffix. A 16-character name plus "01" would be
        // silently cut off by the input field, and the child would be handed a
        // suggestion the game then refuses to store as shown.
        string body = stem.Length > MaxLength - 2 ? stem.Substring(0, MaxLength - 2) : stem;

        for (int n = FirstSuffix; n <= LastSuffix && suggestions.Count < count; n++)
        {
            string candidate = body + n.ToString("00");
            if (!IsTaken(candidate, taken) && !IsTaken(candidate, suggestions))
                suggestions.Add(candidate);
        }

        return suggestions;
    }

    /// <summary>
    /// The sentence shown under the input box. Kept here beside the rule that
    /// produced the names so the wording and the logic cannot drift apart.
    /// </summary>
    public static string TakenMessage(string desired, List<string> suggestions)
    {
        string wanted = desired == null ? "" : desired.Trim();

        if (suggestions == null || suggestions.Count == 0)
            return "\"" + wanted + "\" is taken. Try another name.";

        if (suggestions.Count == 1)
            return "\"" + wanted + "\" is taken. Try " + suggestions[0] + ".";

        string list = string.Join(", ", suggestions.GetRange(0, suggestions.Count - 1).ToArray());
        return "\"" + wanted + "\" is taken. Try " + list +
               " or " + suggestions[suggestions.Count - 1] + ".";
    }
}
