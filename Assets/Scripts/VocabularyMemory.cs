using System.Collections.Generic;

/// <summary>
/// Which Filipino words have already been explained to the child in this
/// sitting.
///
/// Thirteen of the fifty-three words come round again in a later scene -
/// Lakad is met in the bedroom, the hallway, the road to the park and the walk
/// home. Explaining "Lakad means to walk" in full every one of those times
/// stops being teaching and starts being a delay, so the first meeting gets
/// the whole English explanation and every one after it gets a one-line
/// reminder. VoicePromptDirector asks this which of the two to play.
///
/// Static rather than a DontDestroyOnLoad object, for the same reason as
/// RunScore: there is nothing to keep alive, only a list that has to outlive
/// a scene load.
///
/// The memory lasts one sitting. Closing the game forgets everything, which is
/// deliberate - a child coming back after a few days should be taught the word
/// properly again, not handed a reminder of something they no longer remember.
/// </summary>
public static class VocabularyMemory
{
    private static readonly HashSet<string> taught =
        new HashSet<string>(System.StringComparer.OrdinalIgnoreCase);

    /// <summary>How many distinct words have been introduced so far, for a log line.</summary>
    public static int Count => taught.Count;

    /// <summary>
    /// True once this word has been introduced in this sitting, so later
    /// meetings can use the short reminder instead of the full explanation.
    /// </summary>
    public static bool HasBeenTaught(string word)
    {
        if (string.IsNullOrWhiteSpace(word)) return false;
        return taught.Contains(word.Trim());
    }

    /// <summary>
    /// Notes that a word has been introduced. Called when the word is first
    /// ASKED, not when it is first said correctly: the explanation was played
    /// either way, and a child who needed six tries has heard it six times
    /// over - they do not need it again in the next scene.
    /// </summary>
    public static void MarkTaught(string word)
    {
        if (string.IsNullOrWhiteSpace(word)) return;
        taught.Add(word.Trim());
    }

    /// <summary>
    /// Forgets everything, so the next run teaches from scratch. Called when a
    /// fresh playthrough is started from the main menu.
    ///
    /// Deliberately NOT called from RunScoreCounter.resetOnStart: that fires at
    /// the start of every level, which would re-explain Lakad, Upo and Kain at
    /// the top of each chapter.
    /// </summary>
    public static void Reset()
    {
        taught.Clear();
    }
}
