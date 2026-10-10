using UnityEngine;

/// <summary>
/// How far along the journey the player has reached.
///
/// Progress is counted in steps of JourneyMap, not in levels: the map now
/// shows every scene, so finishing one scene has to open the next one rather
/// than waiting for a whole level to be completed. The stored value is the
/// index of the furthest step unlocked, and everything else is derived from
/// it, so there is one number to get wrong instead of several.
///
/// Levels still earn badges. A level counts as complete once the player has
/// unlocked something past its last step, which falls out of the same number.
/// </summary>
public static class LevelProgress
{
    private const string StepKey = "SALINLAHI_HighestUnlockedStep";

    // Kept so an older save still starts the player somewhere sensible rather
    // than at the beginning: whoever had reached level N keeps level N.
    private const string LegacyLevelKey = "SALINLAHI_HighestUnlockedLevel";

    /// <summary>Index into JourneyMap.Steps of the furthest step unlocked.</summary>
    public static int HighestUnlockedStep
    {
        get
        {
            if (PlayerPrefs.HasKey(StepKey)) return PlayerPrefs.GetInt(StepKey, 0);

            // First run under the new map. An older save knows a level number;
            // translate it into the first step of that level.
            int legacyLevel = PlayerPrefs.GetInt(LegacyLevelKey, 1);
            int step = FirstStepOfLevel(legacyLevel);
            HighestUnlockedStep = step;
            return step;
        }
        private set
        {
            PlayerPrefs.SetInt(StepKey, Mathf.Clamp(value, 0, JourneyMap.Steps.Length - 1));
            PlayerPrefs.Save();
        }
    }

    public static bool IsStepUnlocked(int stepIndex) => stepIndex <= HighestUnlockedStep;

    /// <summary>True once every step of this level has been passed.</summary>
    public static bool IsLevelComplete(int levelIndex)
    {
        int last = LastStepOfLevel(levelIndex);
        return last >= 0 && HighestUnlockedStep > last;
    }

    public static bool IsUnlocked(int levelIndex) =>
        IsStepUnlocked(FirstStepOfLevel(levelIndex));

    /// <summary>
    /// Call when a scene is finished. Opens the next step along the journey.
    /// Named by scene rather than by number so a scene does not have to know
    /// where it sits in the order - JourneyMap already does.
    /// </summary>
    public static void MarkSceneComplete(string sceneName)
    {
        int index = JourneyMap.IndexOfScene(sceneName);
        if (index < 0)
        {
            Debug.LogWarning("LevelProgress: '" + sceneName + "' is not on the journey map, " +
                             "so finishing it unlocks nothing. Add it to JourneyMap.Steps.");
            return;
        }
        if (index + 1 > HighestUnlockedStep) HighestUnlockedStep = index + 1;
    }

    /// <summary>
    /// Older call, kept so the scenes already wired to it keep working: a
    /// level being finished means everything up to its last step is passed.
    /// </summary>
    public static void MarkComplete(int levelIndex)
    {
        int last = LastStepOfLevel(levelIndex);
        if (last >= 0 && last + 1 > HighestUnlockedStep) HighestUnlockedStep = last + 1;
    }

    private static int FirstStepOfLevel(int levelIndex)
    {
        for (int i = 0; i < JourneyMap.Steps.Length; i++)
            if (JourneyMap.Steps[i].levelIndex == levelIndex) return i;
        return 0;
    }

    private static int LastStepOfLevel(int levelIndex)
    {
        int last = -1;
        for (int i = 0; i < JourneyMap.Steps.Length; i++)
            if (JourneyMap.Steps[i].levelIndex == levelIndex) last = i;
        return last;
    }
}
