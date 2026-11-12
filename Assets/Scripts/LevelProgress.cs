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
///
/// The number itself, and each scene's star rating, now live on the playing
/// profile rather than in PlayerPrefs - see PlayerProfileStore, which also
/// carries an older PlayerPrefs save over the first time it runs. Nothing
/// that calls this class had to change: the API is the one it always was.
/// </summary>
public static class LevelProgress
{
    /// <summary>Index into JourneyMap.Steps of the furthest step unlocked.</summary>
    public static int HighestUnlockedStep
    {
        get
        {
            PlayerProfile profile = PlayerProfileStore.Active;
            if (profile.highestUnlockedStep >= 0) return profile.highestUnlockedStep;

            // Never recorded. A save old enough to predate the step count
            // knows a level number instead; translate it into that level's
            // first scene so nobody is sent back to the beginning.
            int legacyLevel = PlayerPrefs.GetInt("SALINLAHI_HighestUnlockedLevel", 1);
            int step = FirstStepOfLevel(legacyLevel);
            HighestUnlockedStep = step;
            return step;
        }
        private set
        {
            PlayerProfileStore.Active.highestUnlockedStep =
                Mathf.Clamp(value, 0, JourneyMap.Steps.Length - 1);
            PlayerProfileStore.Save();
        }
    }

    public static bool IsStepUnlocked(int stepIndex) => stepIndex <= HighestUnlockedStep;

    // How well each scene has been done, kept per scene on the profile.
    // Apart from the step count on purpose: progress is one number about the
    // journey, while this is a verdict on each scene, and a child who goes
    // back and does better should keep the better one without the journey
    // moving.

    /// <summary>The best a scene can be done. The summary panel rates out of this.</summary>
    public const int MaxStars = 5;

    /// <summary>Best rating a scene has been finished with, 0..MaxStars, 0 if never.</summary>
    public static int StarsFor(string sceneName) => PlayerProfileStore.StarsFor(sceneName);

    /// <summary>
    /// The same rating in however many stars a screen has room for.
    ///
    /// The summary panel rates out of five; the level select's tiles have
    /// three printed under them. Rounding up keeps the two agreeing about the
    /// only thing that matters at tile size - a scene that earned something
    /// never shows empty, and only a perfect run fills all three.
    /// </summary>
    public static int StarsScaledTo(string sceneName, int slots)
    {
        int stars = StarsFor(sceneName);
        if (stars <= 0 || slots <= 0) return 0;
        return Mathf.Min(slots, Mathf.CeilToInt(stars * slots / (float)MaxStars));
    }

    /// <summary>
    /// Remembers how a scene went, keeping the best run rather than the last.
    /// Replaying a scene the child has already three-starred, and stumbling
    /// this time, should not take the stars off the level select.
    /// </summary>
    public static void RecordStars(string sceneName, int stars)
    {
        PlayerProfileStore.RecordStars(sceneName, Mathf.Clamp(stars, 0, MaxStars));
    }

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
        PlayerProfileStore.MarkSceneComplete(sceneName);

        int index = JourneyMap.IndexOfScene(sceneName);
        if (index < 0)
        {
            Debug.LogWarning("LevelProgress: '" + sceneName + "' is not on the journey map, " +
                             "so finishing it unlocks nothing. Add it to JourneyMap.Steps.");
            return;
        }
        Open(JourneyMap.StepAfter(index));
    }

    /// <summary>
    /// Older call, kept so the scenes already wired to it keep working: a
    /// level being finished means everything up to its last step is passed.
    ///
    /// This has to step over the unenterable just as MarkSceneComplete does.
    /// A level's last step is followed by the next level's marker, so opening
    /// the step straight after it would open a marker and leave that level's
    /// first scene locked - the same dam, reached by the other door. It went
    /// unnoticed because the only caller runs MarkSceneComplete four lines
    /// later and progress never falls, so the right answer arrived anyway.
    /// </summary>
    public static void MarkComplete(int levelIndex)
    {
        int last = LastStepOfLevel(levelIndex);
        if (last < 0) return;
        Open(JourneyMap.StepAfter(last));
    }

    /// <summary>Moves progress forward to this step, never back.</summary>
    private static void Open(int stepIndex)
    {
        if (stepIndex > HighestUnlockedStep) HighestUnlockedStep = stepIndex;
    }

    /// <summary>
    /// The level's first SCENE, not its marker.
    ///
    /// Every level begins with a marker, which is not a place the player can
    /// go. Returning that left a fresh install sitting on step 0 with the
    /// first scene one step beyond it, so the whole game read as locked on
    /// any device that had never been played before - which is every device
    /// but the one it was developed on.
    /// </summary>
    private static int FirstStepOfLevel(int levelIndex)
    {
        for (int i = 0; i < JourneyMap.Steps.Length; i++)
        {
            JourneyMap.Step step = JourneyMap.Steps[i];
            if (step.levelIndex == levelIndex && step.kind == JourneyMap.StepKind.Scene)
                return i;
        }

        // A level with no scenes at all - Palengke, so far. Its marker is as
        // far as anyone can get.
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
