using System.Collections.Generic;

/// <summary>
/// The mistranscriptions each taught word actually comes back as from the
/// device's speech recognizer, listed per word.
///
/// WHY THIS EXISTS
///
/// SceneObjective has a "variants" field for exactly this (see
/// SceneObjectiveController), and nothing ever filled it - so the game was
/// leaning entirely on blind fuzzy matching, which is how "kaha" came to be
/// accepted for "Kuha". Curated per-word data is both STRICTER and MORE
/// ACCURATE than fuzzy distance: it accepts the handful of spellings the
/// recognizer really produces, and nothing else.
///
/// PronunciationChecker consults this table on every check, in addition to
/// whatever an objective carries in its Inspector field. That means the table
/// works in scenes that were generated before it existed - no scene has to be
/// rebuilt or hand-edited for a new entry to take effect.
///
/// HOW TO GROW IT - this is the important part
///
/// The entries below are SEEDS: likely Filipino spelling variants, plus the
/// English words the recognizer tends to substitute ("i love" for "Ilaw",
/// "lines" for "Linis", "oppo" for "Upo"). They are informed guesses, not
/// measurements. The real list comes from playtesting:
///
///   1. Play with Log Attempts on (VoiceCommand).
///   2. Pull the CSV - see VoiceAttemptLog for the adb command.
///   3. Read the rawSpokenText column for rows whose verdict is "mali" but
///      where the child actually said the word right.
///   4. Add those exact strings here.
///
/// THE ONE RULE WHEN ADDING AN ENTRY
///
/// A variant must never be another taught word. If "kanen" were listed under
/// "Kanan", then a child correctly saying "Kanin" would be scored TAMA for
/// Kanan. The editor tool Tools > SALINLAHI > Check Pronunciation Matching
/// checks this for you and names any entry that breaks it, so run it after
/// editing this file.
/// </summary>
public static class WordVariants
{
    /// <summary>
    /// Keyed by the taught word. Lookup is case-insensitive and trims, so
    /// "Kuha", "kuha" and " Kuha " all find the same row.
    /// </summary>
    private static readonly Dictionary<string, string[]> Table =
        new Dictionary<string, string[]>(System.StringComparer.OrdinalIgnoreCase)
    {
        // --- Bahay: waking up, the bathroom, getting dressed, breakfast ---
        // No "Tabo": the dipper was dropped from the word list, and this
        // table is keyed by the words the game actually teaches.
        { "Bangon",  new[] { "ba ngon", "bangun", "bangong" } },
        { "Tayo",    new[] { "ta yo", "tayu", "thai yo" } },
        { "Lakad",   new[] { "la kad", "lacad", "lagad" } },
        { "Bukas",   new[] { "bu kas", "bucas", "books" } },
        { "Ilaw",    new[] { "i love", "i law", "elao", "hello" } },
        { "Sabon",   new[] { "sa bon", "sabun", "savon" } },
        { "Sipilyo", new[] { "sipilio", "si pilyo", "sepilyo", "cipilio" } },
        { "Tuwalya", new[] { "twalya", "tualya", "tuwalia", "to walya" } },
        { "Maligo",  new[] { "ma ligo", "malego", "my ligo" } },
        { "Suklay",  new[] { "su klay", "sucklay", "soklay" } },
        { "Damit",   new[] { "da mit", "damet", "dammit" } },
        { "Medyas",  new[] { "medias", "mediyas", "me dyas" } },
        { "Sapatos", new[] { "sa patos", "zapatos", "sapatus" } },
        { "Bag",     new[] { "back", "beg", "bak" } },
        { "Kanin",   new[] { "ka nin", "canin", "kanen" } },
        { "Itlog",   new[] { "it log", "etlog", "it lock" } },
        { "Kain",    new[] { "ka in", "cain", "kayin" } },
        { "Inom",    new[] { "i nom", "enom", "e nom" } },

        // --- Paaralan: arriving, the classroom, recess, going home ---
        { "Kumusta", new[] { "kamusta", "komusta", "como esta", "ka musta" } },
        { "Pasok",   new[] { "pa sok", "pasock", "pasuk" } },
        { "Takbo",   new[] { "tak bo", "tagbo", "takbu" } },
        { "Upo",     new[] { "oppo", "u po", "oh po", "upu" } },
        { "Kuha",    new[] { "kua", "koha", "cooha", "ku ha" } },
        { "Lapis",   new[] { "lapiz", "la pis", "lappis" } },
        { "Aklat",   new[] { "ak lat", "aclat", "oclat" } },
        { "Basa",    new[] { "ba sa", "bassa", "bosa" } },
        { "Sulat",   new[] { "su lat", "soolat", "sue lat" } },
        { "Tanong",  new[] { "ta nong", "tanung", "ta noong" } },
        { "Sagot",   new[] { "sa got", "sagut", "sa gut" } },
        { "Tingin",  new[] { "ti ngin", "tingen", "tingnan" } },
        { "Linis",   new[] { "lines", "li nis", "lenis" } },
        { "Kunin",   new[] { "ku nin", "cumin", "coonin" } },
        { "Kaliwa",  new[] { "ka liwa", "caliwa", "kaleewa" } },
        { "Kanan",   new[] { "ka nan", "canon", "kanann" } },
        { "Labas",   new[] { "la bas", "lavas", "la vas" } },
        { "Uwi",     new[] { "u wi", "oowi", "ui" } },

        // --- Parke and friends: playing, feelings, being kind ---
        { "Laro",    new[] { "la ro", "laru", "larow" } },
        { "Akyat",   new[] { "ak yat", "akiyat", "acyat" } },
        { "Dausdos", new[] { "daus dos", "daosdos", "dausdus" } },
        { "Sakay",   new[] { "sa kay", "sakai", "sa kai" } },
        { "Tulak",   new[] { "tu lak", "tolak", "too lak" } },
        { "Talon",   new[] { "ta lon", "talun", "tallon" } },
        { "Tuwa",    new[] { "towa", "tu wa", "to wa" } },
        { "Tawa",    new[] { "ta wa", "tawah", "ta wah" } },
        { "Ngiti",   new[] { "nge te", "ingiti", "ngitee" } },
        { "Pagod",   new[] { "pa god", "pagud", "pagot" } },
        { "Bati",    new[] { "ba ti", "bate", "bathe" } },
        { "Tulong",  new[] { "tu long", "tolong", "tulung" } },
        { "Ibahagi", new[] { "iba hagi", "i bahagi", "ibahage" } },
        { "Salamat", new[] { "sa lamat", "slamat", "salamat po" } },
        { "Paalam",  new[] { "pa alam", "palam", "paalam po" } },
        { "Alis",    new[] { "a lis", "ales", "alice" } },
    };

    /// <summary>
    /// The known mistranscriptions for a word, or null if it has none listed.
    /// Null rather than an empty array, because PronunciationChecker skips a
    /// null list without allocating.
    /// </summary>
    public static string[] For(string word)
    {
        if (string.IsNullOrEmpty(word)) return null;

        string[] variants;
        return Table.TryGetValue(word.Trim(), out variants) ? variants : null;
    }

    /// <summary>Every word with an entry, for the editor self-check tool.</summary>
    public static IEnumerable<string> Words { get { return Table.Keys; } }
}
