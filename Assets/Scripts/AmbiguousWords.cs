using System.Collections.Generic;

/// <summary>
/// The taught words whose MEANING depends on how they are pronounced, and
/// that the group chose to judge strictly: the same letters, two established
/// Filipino words. These six are the only words
/// judged by the strict hybrid check (AnswerJudge.JudgeAmbiguous); every
/// other word gets the balanced check, where the right word is enough.
///
/// Each entry says which pronunciation the game teaches - the one Teacher
/// Rachel demonstrates in word_WORD.wav - and which one it must not accept.
///
/// ALTERNATIVE RECORDINGS
///
/// The hybrid check compares the player's answer with Rachel's recording AND
/// with recordings of the other pronunciation. Until real ones exist it makes
/// the other pronunciation from Rachel's own (her stress moved). Real ones
/// are better: put them in Assets/Audio/VO/alt/ as word_WORD_alt*.wav (any
/// voice; one word, said the OTHER way, e.g. "táyo" as in "kain tayo") and
/// run Tools/test_stress_rules.sh generate. They are reference data only;
/// the game never plays them.
///
/// Adding a word: add a row here (and its marked spelling in WordStress),
/// then run generate.
/// </summary>
public static class AmbiguousWords
{
    public sealed class Entry
    {
        /// <summary>The word as SceneObjective spells it.</summary>
        public string word;
        /// <summary>The pronunciation the game teaches (Rachel's), written with stress marks.</summary>
        public string intended;
        public string intendedMeaning;
        /// <summary>The other pronunciation of the same letters, which must be refused.</summary>
        public string alternative;
        public string alternativeMeaning;
    }

    private static readonly Entry[] Entries =
    {
        E("Tayo",  "Tayô",  "stand up",          "Táyo",  "we, us"),
        E("Upo",   "Upô",   "sit down",          "Úpo",   "bottle gourd"),
        E("Bukas", "Bukás", "open",              "Búkas", "tomorrow"),
        E("Basa",  "Bása",  "read",              "Basâ",  "wet"),
        E("Bati",  "Báti",  "greet",             "Batî",  "beaten (as an egg)"),
        E("Pasok", "Pások", "enter",             "Pasók", "inserted, included"),
        // Talon, Lakad, Kuha, Sulat and Linis also have a second pronunciation,
        // but the group chose (2026-11-22) to judge them as ordinary words.
    };

    private static Entry E(string w, string i, string im, string a, string am)
    {
        return new Entry { word = w, intended = i, intendedMeaning = im, alternative = a, alternativeMeaning = am };
    }

    private static Dictionary<string, Entry> table;

    /// <summary>The entry for a taught word, or null when its meaning does not depend on its pronunciation.</summary>
    public static Entry For(string word)
    {
        if (table == null)
        {
            Dictionary<string, Entry> t = new Dictionary<string, Entry>(System.StringComparer.OrdinalIgnoreCase);
            foreach (Entry e in Entries) t[e.word] = e;
            table = t;
        }
        Entry found;
        return !string.IsNullOrEmpty(word) && table.TryGetValue(word.Trim(), out found) ? found : null;
    }

    public static bool IsAmbiguous(string word) { return For(word) != null; }

    public static IEnumerable<Entry> All { get { return Entries; } }
}
