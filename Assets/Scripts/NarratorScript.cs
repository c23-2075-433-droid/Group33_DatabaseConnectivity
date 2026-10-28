using System.Collections.Generic;

/// <summary>
/// The narrator's script, compiled in: the id of every line the prompting
/// system asks for, and the Filipino text of the short, reusable ones.
///
/// This exists so the game prompts correctly with NO recordings and NO
/// generated voice bank - a fresh checkout shows "Sabihin mo: LAKAD" on
/// screen and paces itself as though Rachel had said it. Once
/// Assets/Resources/NarratorVoiceBank.asset is generated, its text and clips
/// take precedence over everything here.
///
/// The ids are the recording file names in Docs/VoiceOverScript.csv without
/// their extension. Keep the two in step: if a line is renamed there, rename
/// it here.
/// </summary>
public static class NarratorScript
{
    // --- Prompt frames: played straight before a word clip, heard as one
    //     sentence ("Sabihin mo:" + "LAKAD"). ---
    public const string FrameSay      = "rachel_frame_sabihin";
    public const string FrameSayAgain = "rachel_frame_ngayon";
    public const string FrameListen   = "rachel_frame_ulitin";

    // --- Listening ---
    public const string CueListening = "rachel_cue_listening";
    public const string MicBlocked   = "rachel_mic_blocked";

    // --- Answers ---
    public const string Correct1  = "rachel_feedback_correct_1";
    public const string Correct2  = "rachel_feedback_correct_2";
    public const string Correct3  = "rachel_feedback_correct_3";
    public const string Retry1    = "rachel_feedback_incorrect_1";
    public const string Retry2    = "rachel_feedback_incorrect_2";
    public const string NotHeard1 = "rachel_feedback_nothear_1";
    public const string NotHeard2 = "rachel_feedback_nothear_2";

    // --- Help ---
    public const string HintListen = "rachel_hint_pakinggan";
    public const string HintReplay = "rachel_hint_replay";
    public const string IdleNudge  = "rachel_nudge_idle";

    /// <summary>The three correct-answer cues, rotated in order so the same one never lands twice running.</summary>
    public static readonly string[] CorrectCues = { Correct1, Correct2, Correct3 };

    /// <summary>The id of a word's own recording, e.g. "lakad" -> "word_lakad".</summary>
    public static string WordId(string word)
    {
        if (string.IsNullOrEmpty(word)) return null;
        return "word_" + word.Trim().ToLowerInvariant();
    }

    /// <summary>
    /// The id of a scene's intro or outro line, worked out from the scene
    /// name: Chapter2_Level3_Aralin -> rachel_l2_s3_intro. The game's
    /// "Chapter" is the VoiceOverScript's "Level", and its "Level" is that
    /// script's "Scene", which is why the two numbers swap names here.
    /// Returns null for a scene that is not a chapter level (MainMenu,
    /// LevelSelect).
    /// </summary>
    public static string SceneLineId(string sceneName, bool intro)
    {
        if (string.IsNullOrEmpty(sceneName)) return null;
        if (!sceneName.StartsWith("Chapter")) return null;

        string[] parts = sceneName.Split('_');
        if (parts.Length < 2) return null;

        string chapter = parts[0].Substring("Chapter".Length);
        if (!parts[1].StartsWith("Level")) return null;
        string level = parts[1].Substring("Level".Length);

        if (chapter.Length == 0 || level.Length == 0) return null;
        return "rachel_l" + chapter + "_s" + level + (intro ? "_intro" : "_outro");
    }

    /// <summary>
    /// Built-in text for the short reusable lines, used when the voice bank
    /// has no entry for an id. Scene intros and outros are deliberately absent
    /// - they are long, per-scene and already written in the CSV, so they
    /// stay silent until the bank is generated.
    /// </summary>
    private static readonly Dictionary<string, string> Defaults = new Dictionary<string, string>
    {
        { FrameSay,      "Sabihin mo:" },
        { FrameSayAgain, "Ngayon naman, sabihin mo:" },
        { FrameListen,   "Pakinggan mo ulit:" },

        { CueListening,  "Nakikinig ako." },
        { MicBlocked,    "Pindutin ang mikropono para magsalita." },

        { Correct1,      "Tama!" },
        { Correct2,      "Magaling!" },
        { Correct3,      "Ang galing mo!" },

        { Retry1,        "Hindi pa tama. Subukan mo ulit." },
        { Retry2,        "Malapit na! Ulitin mo." },

        { NotHeard1,     "Hindi kita narinig. Pakiulit." },
        { NotHeard2,     "Pakilakasan mo ang boses mo." },

        { HintListen,    "Pakinggan mong mabuti." },
        { HintReplay,    "Pindutin ang asul na buton para marinig ulit." },
        { IdleNudge,     "Sabihin mo na. Kaya mo 'yan!" },
    };

    /// <summary>
    /// The built-in text for an id, or null. A word id falls through to the
    /// word itself in capitals, so every one of the 53 vocabulary words has a
    /// caption without being listed here.
    /// </summary>
    public static string DefaultText(string id)
    {
        if (string.IsNullOrEmpty(id)) return null;

        string text;
        if (Defaults.TryGetValue(id, out text)) return text;

        if (id.StartsWith("word_") && id.Length > 5)
            return id.Substring(5).ToUpperInvariant();

        return null;
    }
}
