using System.Collections.Generic;

/// <summary>
/// How a taught word is SPELLED ON SCREEN, for the handful of words whose
/// ordinary spelling is two different words.
///
/// WHY THIS EXISTS
///
/// Filipino is written without stress marks, and for almost every word in this
/// game that is the right thing to show a child: BANGON is BANGON. But a few of
/// the words are minimal pairs, where the letters alone do not say which word
/// is meant and the stress does:
///
///     tayô   to stand up        táyo   we, us
///     upô    to sit down        úpo    the bottle gourd
///     bukás  to open            búkas  tomorrow
///     bása   to read            basâ   wet
///     talón  to jump            tálon  a waterfall
///     bátì   a greeting         batî   beaten, as an egg is
///
/// This game asks the child to SAY the word, so an unmarked spelling on those
/// six is not a small imprecision - it is the game failing to specify what it
/// is asking for. An adult reading over the child's shoulder would be none the
/// wiser either. So those six carry their marks on screen and the rest do not,
/// which also keeps the marks meaningful: a mark appears exactly where it is
/// doing work.
///
/// DISPLAY ONLY
///
/// Nothing here reaches recognition. PronunciationChecker, WordVariants and
/// NarratorScript all go on using the plain spelling that SceneObjective.word
/// carries, so matching, the accepted-variant table and the voice-line file
/// names are untouched. This is the label and only the label, which is why it
/// can be a lookup rather than a field on every objective: no scene has to be
/// rebuilt for it to take effect.
///
/// STILL TO BE RULED ON
///
/// Two more of the 53 are arguably pairs and are deliberately NOT marked here
/// until the validating teacher says one way or the other:
///
///     kuha   kúha (to get) against kuhà (a photograph, something obtained)
///     sulat  súlat (a letter) against sulát (written)
///
/// Both are weaker than the six above, and marking a word that does not need it
/// teaches a distinction that is not there. Add them only on the validator's
/// say-so - and have her check the six marks themselves while she is at it,
/// since they are the group's reading and not hers.
/// </summary>
public static class WordStress
{
    /// <summary>
    /// Keyed by the taught word as SceneObjective spells it. Lookup is
    /// case-insensitive and trims, so "Upo", "upo" and " Upo " all find the
    /// same row. The value is written in ordinary case; the call sites
    /// uppercase it themselves, as they already did with the plain spelling.
    /// </summary>
    private static readonly Dictionary<string, string> Table =
        new Dictionary<string, string>(System.StringComparer.OrdinalIgnoreCase)
    {
        { "Tayo",  "Tayô"  },   // against táyo, we
        { "Upo",   "Upô"   },   // against úpo, the gourd
        { "Bukas", "Bukás" },   // against búkas, tomorrow
        { "Basa",  "Bása"  },   // against basâ, wet
        { "Talon", "Talón" },   // against tálon, a waterfall
        { "Bati",  "Bátì"  },   // against batî, beaten
    };

    /// <summary>
    /// The spelling to put on screen: the marked form for one of the pairs
    /// above, otherwise the word exactly as it was given. Never null for a
    /// non-null argument, so a caller can use it straight.
    /// </summary>
    public static string Display(string word)
    {
        if (string.IsNullOrEmpty(word)) return word;

        string trimmed = word.Trim();
        string marked;
        return Table.TryGetValue(trimmed, out marked) ? marked : trimmed;
    }

    /// <summary>True if this word is one of the marked pairs.</summary>
    public static bool IsMarked(string word)
    {
        return !string.IsNullOrEmpty(word) && Table.ContainsKey(word.Trim());
    }

    /// <summary>Every word with a marked spelling, for the editor self-check tool.</summary>
    public static IEnumerable<string> Words { get { return Table.Keys; } }
}
