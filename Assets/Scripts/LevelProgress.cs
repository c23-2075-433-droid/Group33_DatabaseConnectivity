using UnityEngine;

/// <summary>
/// Tracks how far the player has gotten on the "Ang Iyong Paglalakbay" journey
/// map. Static + PlayerPrefs-backed, same pattern as NicknameManager, so
/// LevelSelectController can read it without any Inspector wiring.
///
/// Level 1 (Bahay) is always unlocked. Wire future levels' completion
/// triggers (see ExitDoorTrigger / GoalTrigger) to call MarkComplete(n) so
/// the next node on the map unlocks.
/// </summary>
public static class LevelProgress
{
    private const string PlayerPrefsKey = "SALINLAHI_HighestUnlockedLevel";

    /// <summary>1-based index of the furthest level the player has unlocked.</summary>
    public static int HighestUnlockedLevel
    {
        get => PlayerPrefs.GetInt(PlayerPrefsKey, 1);
        private set
        {
            PlayerPrefs.SetInt(PlayerPrefsKey, value);
            PlayerPrefs.Save();
        }
    }

    public static bool IsUnlocked(int levelIndex) => levelIndex <= HighestUnlockedLevel;

    /// <summary>Call when a level is finished, to unlock the next node on the map.</summary>
    public static void MarkComplete(int levelIndex)
    {
        if (levelIndex + 1 > HighestUnlockedLevel)
            HighestUnlockedLevel = levelIndex + 1;
    }
}
