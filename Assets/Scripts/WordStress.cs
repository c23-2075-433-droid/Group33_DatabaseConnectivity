using System.Collections.Generic;

/// <summary>
/// How a taught word is SPELLED ON SCREEN and STRESSED when said: every word
/// with a known stress is shown with its stress marks, as the Level script
/// writes it (LÁKAD, BÁNGON, TAYÔ).
///
/// WHY THIS EXISTS
///
/// Filipino is usually written without stress marks, but this game asks the
/// child to SAY the word, so the screen shows where the stress goes - the
/// same marks the group's script uses. A few of the words are also minimal
/// pairs, where the letters alone do not say which word
/// is meant and the stress does:
///
///     tayô   to stand up        táyo   we, us
///     upô    to sit down        úpo    the bottle gourd
///     bukás  to open            búkas  tomorrow
///     bása   to read            basâ   wet
///     talón  to jump            tálon  a waterfall
///     bátì   a greeting         batî   beaten, as an egg is
///     lákad  to walk            lakád  on foot
///     pások  to enter           pasók  inserted, included
///     kúha   to get, to take    kuhà   a shot, something taken
///     sulát  to write           súlat  a letter, writing
///     línis  to clean           linís  cleaned
///
/// This game asks the child to SAY the word, so an unmarked spelling on those
/// eleven is not a small imprecision - it is the game failing to specify what it
/// is asking for. An adult reading over the child's shoulder would be none the
/// wiser either. Those eleven are also judged more strictly (see below).
///
/// The last five (lákad, pások, kúha, sulát, línis) were added when stress
/// checking was extended to every word; kúha and súlat had been held back
/// for the validating teacher, and all eleven marks are still the group's
/// reading for her to confirm.
///
/// TWO TABLES
///
///   Pairs           the words above. They are judged strictly: said inside a sentence they are
///                   taken as the other word (AnswerJudge).
///   Pronunciations  EVERY taught word's stress, written with Filipino stress
///                   marks, as the group's Vocabulary Validation Form gives
///                   it (Docs/Vocabulary_Validation_Form_ToSend.md) and the
///                   Level script. Shown ON SCREEN (Display), and
///                   StressPattern reads it, so every
///                   word's stress is checked from the child's voice, not only
///                   the pairs'.
///
/// Neither reaches the letters: PronunciationChecker, WordVariants and
/// NarratorScript go on using the plain spelling that SceneObjective.word
/// carries, so matching, the accepted-variant table and the voice-line file
/// names are untouched, and no scene has to be rebuilt.
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
        { "Lakad", "Lákad" },   // against lakád, on foot
        { "Pasok", "Pások" },   // against pasók, inserted
        { "Kuha",  "Kúha"  },   // against kuhà, a shot / something taken
        { "Sulat", "Sulát" },   // against súlat, a letter
        { "Linis", "Línis" },   // against linís, cleaned
    };

    /// <summary>
    /// Every taught word's stress, keyed by the taught word. From the group's
    /// Vocabulary Validation Form and the Level 1-2 script (the script wins
    /// where they differed: Lákad, Malígo, Sulát), except the words marked "not on the form",
    /// which the form does not list and are the group's own reading - those
    /// especially want the validating teacher's eye. A word missing here
    /// (Bag has one syllable; Kinig and Tapon are not yet decided) has no
    /// stress check, and its letters decide alone.
    /// </summary>
    private static readonly Dictionary<string, string> Pronunciations =
        new Dictionary<string, string>(System.StringComparer.OrdinalIgnoreCase)
    {
        // Bahay
        { "Bangon",  "Bángon"  }, { "Tayo",    "Tayô"    }, { "Lakad",   "Lákad"   },
        { "Kaliwa",  "Kaliwâ"  }, { "Kanan",   "Kánan"   }, { "Bukas",   "Bukás"   },
        { "Ilaw",    "Ílaw"    }, { "Tuwalya", "Tuwálya" }, { "Sabon",   "Sabón"   },
        { "Maligo",  "Malígo"  }, { "Sipilyo", "Sipílyo" }, { "Suklay",  "Sukláy"  },
        { "Damit",   "Damít"   }, { "Medyas",  "Médyas"  }, { "Sapatos", "Sapátos" },
        { "Kunin",   "Kúnin"   },   // not on the form; Bángon and Kúnin corrected by the group, 2026-11-21
        { "Upo",     "Upô"     },
        { "Pagkain", "Pagkáin" },   // not on the form
        { "Kain",    "Káin"    }, { "Inom",    "Inóm"    }, { "Linis",   "Línis"   },
        // Paaralan
        { "Pasok",   "Pások"   }, { "Takbo",   "Takbó"   }, { "Bati",    "Bátì"    },
        { "Kuha",    "Kúha"    }, { "Lapis",   "Lápis"   }, { "Aklat",   "Aklát"   },
        { "Basa",    "Bása"    }, { "Sulat",   "Sulát"   }, { "Sagot",   "Sagót"   },
        { "Ligpit",  "Ligpít"  },   // not on the form
        { "Paalam",  "Paálam"  }, { "Alis",    "Alís"    },
        // Parke
        { "Tingin",  "Tingín"  },
        { "Tuwa",    "Tuwâ"    },   // not on the form
        { "Akyat",   "Akyát"   }, { "Dausdos", "Dausdós" }, { "Sakay",   "Sakáy"   },
        { "Tulak",   "Túlak"   }, { "Talon",   "Talón"   },
        { "Kumusta", "Kumustá" },   // not on the form
        { "Ngiti",   "Ngitî"   }, { "Tanong",  "Tanóng"  }, { "Laro",    "Larô"    },
        { "Ibahagi", "Ibahágì" }, { "Salamat", "Salámat" }, { "Pagod",   "Pagód"   },
        { "Uwi",     "Uwî"     },
    };

    /// <summary>
    /// The word written with its stress marks ("Bángon", "Tayô"), or null when
    /// its stress has not been decided. Used for checking; Display shows it.
    /// </summary>
    public static string PronunciationOf(string word)
    {
        if (string.IsNullOrEmpty(word)) return null;
        string marked;
        return Pronunciations.TryGetValue(word.Trim(), out marked) ? marked : null;
    }

    /// <summary>Every word with a known stress, for the reference builder and the tests.</summary>
    public static IEnumerable<string> WordsWithStress { get { return Pronunciations.Keys; } }

    /// <summary>
    /// The spelling to put on screen: the word with its stress marks, as the
    /// Level script writes it ("Lákad", "Bángon"), for every word whose stress
    /// is known; otherwise the word exactly as it was given (Bag, Kinig,
    /// Tapon). Never null for a non-null argument, so a caller can use it
    /// straight.
    /// </summary>
    public static string Display(string word)
    {
        if (string.IsNullOrEmpty(word)) return word;

        string trimmed = word.Trim();
        string marked;
        if (Table.TryGetValue(trimmed, out marked)) return marked;
        if (Pronunciations.TryGetValue(trimmed, out marked)) return marked;
        return trimmed;
    }

    /// <summary>True if this word is one of the pairs: same letters, another word when the stress moves.</summary>
    public static bool IsMarked(string word)
    {
        return !string.IsNullOrEmpty(word) && Table.ContainsKey(word.Trim());
    }

    /// <summary>Every word with a marked spelling, for the editor self-check tool.</summary>
    public static IEnumerable<string> Words { get { return Table.Keys; } }
}
