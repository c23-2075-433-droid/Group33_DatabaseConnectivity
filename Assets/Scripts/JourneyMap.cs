using System.Collections.Generic;

/// <summary>
/// The whole journey, in order, as one list: every level marker and every
/// scene between them.
///
///   Bahay - s1 - s2 - s3 - s4 - s5 - s6 - Paaralan - s1 - ... - Palengke
///
/// This is the single place the order is written down. The map scene lays its
/// nodes out from this list, and LevelProgress counts how far along it the
/// player has reached, so the two can never disagree about what comes next.
///
/// Scenes that are designed but not built yet carry an empty sceneName. They
/// still appear on the map - the journey is meant to show where the child is
/// going, not only where they have been - but they can never be entered, so a
/// node that leads nowhere is impossible rather than merely unlikely.
/// </summary>
public static class JourneyMap
{
    public enum StepKind { Level, Scene }

    public class Step
    {
        /// <summary>A level milestone, or one scene inside a level.</summary>
        public StepKind kind;

        /// <summary>"Bahay", or the scene's number within its level as text.</summary>
        public string label;

        /// <summary>Scene to load. Empty means designed but not built yet.</summary>
        public string sceneName;

        /// <summary>Which level this belongs to, 1-based. Picks the badge art.</summary>
        public int levelIndex;

        /// <summary>Sprite stem for a level marker, e.g. "map_bahay".</summary>
        public string nodeSprite;

        public bool IsPlayable => !string.IsNullOrEmpty(sceneName);
    }

    private static Step Level(string label, int levelIndex, string nodeSprite) =>
        new Step { kind = StepKind.Level, label = label, levelIndex = levelIndex,
                   nodeSprite = nodeSprite, sceneName = "" };

    private static Step Scene(string label, int levelIndex, string sceneName) =>
        new Step { kind = StepKind.Scene, label = label, levelIndex = levelIndex,
                   sceneName = sceneName };

    /// <summary>
    /// The journey in order. Twenty steps: four level markers and the sixteen
    /// scenes the design document lays out. Six of those scenes are built.
    /// </summary>
    public static readonly Step[] Steps = new[]
    {
        Level("Bahay", 1, "map_bahay"),
        Scene("1", 1, "Chapter1_Level1_UmagaNa"),
        Scene("2", 1, "Chapter1_Level2_Banyo"),
        Scene("3", 1, "Chapter1_Level3_Maligo"),
        Scene("4", 1, "Chapter1_Level4_Damit"),
        Scene("5", 1, "Chapter1_Level5_Almusal"),
        Scene("6", 1, ""),                               // Leaving for School

        Level("Paaralan", 2, "map_paaralan"),
        Scene("1", 2, "Chapter2_Level1_Paaralan"),
        Scene("2", 2, ""),                               // Entering the Classroom
        Scene("3", 2, ""),                               // Vocabulary Lesson
        Scene("4", 2, ""),                               // Recess
        Scene("5", 2, ""),                               // Afternoon / Dismissal

        Level("Parke", 3, "map_parke"),
        Scene("1", 3, ""),                               // Walking to the Park
        Scene("2", 3, ""),                               // Playground
        Scene("3", 3, ""),                               // Meeting New Friends
        Scene("4", 3, ""),                               // Picnic
        Scene("5", 3, ""),                               // Heading Home

        Level("Palengke", 4, "map_palengke"),
    };

    /// <summary>Where a scene sits in the journey, or -1 if it is not on it.</summary>
    public static int IndexOfScene(string sceneName)
    {
        if (string.IsNullOrEmpty(sceneName)) return -1;
        for (int i = 0; i < Steps.Length; i++)
            if (Steps[i].sceneName == sceneName) return i;
        return -1;
    }

    /// <summary>
    /// The next step a player can actually enter after this one, skipping
    /// level markers and scenes that have no scene behind them yet.
    /// </summary>
    public static int NextPlayable(int fromIndex)
    {
        for (int i = fromIndex + 1; i < Steps.Length; i++)
            if (Steps[i].IsPlayable) return i;
        return -1;
    }

    /// <summary>Every step belonging to one level, markers included.</summary>
    public static IEnumerable<Step> InLevel(int levelIndex)
    {
        foreach (Step s in Steps)
            if (s.levelIndex == levelIndex) yield return s;
    }
}
