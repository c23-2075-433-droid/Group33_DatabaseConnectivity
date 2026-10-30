using System.Collections.Generic;

/// <summary>
/// The narrator's script, compiled in: the id of every line the prompting
/// system asks for, and the text of the short, reusable ones.
///
/// Each word is introduced as a story beat rather than a drill - what is
/// happening (story), what the word means in English (teach, or a short
/// recall on a later meeting), then what to say (ask) - and each beat is a
/// separate line, so each becomes its own subtitle card.
///
/// This exists so the game prompts correctly with NO recordings and NO
/// generated voice bank - a fresh checkout still shows "Sabihin mo: LAKAD"
/// and paces itself as though Rachel had said it. Once
/// Assets/Resources/NarratorVoiceBank.asset is generated, its text and clips
/// take precedence over everything here, and the story beats replace that
/// bare frame.
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

    // --- The story beats around a word ------------------------------------
    //
    // Each one is a separate line, so each becomes its own subtitle card: the
    // child is told what is happening, then what the word means, then what to
    // say - three short captions rather than one paragraph nobody can read.

    /// <summary>What is happening in the story right now, e.g. "rachel_l1_s1_bangon_story".</summary>
    public static string StoryId(string sceneName, string word)
    {
        return BeatId(sceneName, word, "story");
    }

    /// <summary>
    /// The instruction, e.g. "rachel_l1_s1_bangon_ask". Its text holds a
    /// {word} placeholder which VoiceNarrator replaces with the word itself,
    /// spoken from word_&lt;word&gt;.wav - see VoiceNarrator.SpeakAsk.
    /// </summary>
    public static string AskId(string sceneName, string word)
    {
        return BeatId(sceneName, word, "ask");
    }

    /// <summary>Praise once the word has been said, e.g. "rachel_l1_s1_bangon_done".</summary>
    public static string DoneId(string sceneName, string word)
    {
        return BeatId(sceneName, word, "done");
    }

    /// <summary>
    /// The full English meaning, said the first time a word comes up in a
    /// session, e.g. "rachel_word_bangon_teach". Not tied to a scene: the same
    /// explanation serves wherever the word is first met.
    /// </summary>
    public static string TeachId(string word)
    {
        if (string.IsNullOrEmpty(word)) return null;
        return "rachel_word_" + word.Trim().ToLowerInvariant() + "_teach";
    }

    /// <summary>
    /// The short reminder used every time after the first, e.g.
    /// "rachel_word_bangon_recall". Thirteen of the words come round again in
    /// later levels; re-teaching them in full each time would stall the game.
    /// </summary>
    public static string RecallId(string word)
    {
        if (string.IsNullOrEmpty(word)) return null;
        return "rachel_word_" + word.Trim().ToLowerInvariant() + "_recall";
    }

    private static string BeatId(string sceneName, string word, string beat)
    {
        string scene = SceneToken(sceneName);
        if (scene == null || string.IsNullOrEmpty(word)) return null;
        return "rachel_" + scene + "_" + word.Trim().ToLowerInvariant() + "_" + beat;
    }

    /// <summary>
    /// What a vocabulary word means in English - "Bangon" -> "Get up" - read
    /// from the English Gloss column of Docs/VoiceOverScript.csv by way of the
    /// voice bank.
    ///
    /// Empty rather than null when the bank has not been generated yet, or the
    /// word has no gloss: the panel that shows this simply leaves the meaning
    /// line out, which is the right answer for a word nobody has glossed.
    /// </summary>
    public static string GlossFor(string word)
    {
        string id = WordId(word);
        if (string.IsNullOrEmpty(id)) return "";

        NarratorVoiceBank bank = NarratorVoiceBank.Load();
        NarratorLine line = bank != null ? bank.Find(id) : null;
        return line != null && !string.IsNullOrEmpty(line.gloss) ? line.gloss : "";
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
        string scene = SceneToken(sceneName);
        if (scene == null) return null;
        return "rachel_" + scene + (intro ? "_intro" : "_outro");
    }

    /// <summary>
    /// The script's name for a scene: Chapter2_Level3_Aralin -> "l2_s3". Null
    /// for a scene that is not a chapter level (MainMenu, LevelSelect).
    /// </summary>
    private static string SceneToken(string sceneName)
    {
        if (string.IsNullOrEmpty(sceneName)) return null;
        if (!sceneName.StartsWith("Chapter")) return null;

        string[] parts = sceneName.Split('_');
        if (parts.Length < 2) return null;

        string chapter = parts[0].Substring("Chapter".Length);
        if (!parts[1].StartsWith("Level")) return null;
        string level = parts[1].Substring("Level".Length);

        if (chapter.Length == 0 || level.Length == 0) return null;
        return "l" + chapter + "_s" + level;
    }

    /// <summary>
    /// Built-in text for the short reusable lines, used when the voice bank
    /// has no entry for an id. The story beats - scene intros and outros, and
    /// every word's story/teach/ask/praise line - are deliberately absent:
    /// they are long, per-scene and already written in the CSV, so they stay
    /// silent until the bank is generated rather than holding the caption bar
    /// for a line nobody wrote here.
    /// </summary>
    private static readonly Dictionary<string, string> Defaults = new Dictionary<string, string>
    {
        { FrameSay,      "Sabihin mo:" },
        { FrameSayAgain, "Ngayon naman, sabihin mo:" },
        { FrameListen,   "Pakinggan mo ulit:" },

        { CueListening,  "Nakikinig ako. I am listening!" },
        { MicBlocked,    "Press the microphone button so Kylo can hear you." },

        { Correct1,      "Tama! That's right!" },
        { Correct2,      "Magaling! Well done!" },
        { Correct3,      "Ang galing mo! You did it!" },

        { Retry1,        "Hindi pa tama. Let's try that one again." },
        { Retry2,        "Malapit na! You are very close. One more try." },

        { NotHeard1,     "Hindi kita narinig. I did not hear you - try once more." },
        { NotHeard2,     "Pakilakasan mo ang boses mo. A little louder, please!" },

        { HintListen,    "Pakinggan mong mabuti. Listen carefully." },
        { HintReplay,    "Press the blue button to hear the word again." },
        { IdleNudge,     "Kaya mo 'yan! Go on, you can do it." },
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
