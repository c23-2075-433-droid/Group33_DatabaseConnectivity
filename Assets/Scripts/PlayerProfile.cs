using System;
using System.Collections.Generic;

/// <summary>
/// Everything the game remembers about one player on this device.
///
/// The child is identified by <see cref="playerId"/> - a UUID made once, when
/// the profile is created - and never by the nickname. That is what lets a
/// name be changed without the progress underneath it moving, and it is what
/// the leaderboard keys its single row per player on.
///
/// Plain public fields with no properties, because Unity's JsonUtility is what
/// writes this to disk and it only sees fields. For the same reason the
/// per-scene star ratings are a list of pairs rather than a dictionary.
///
/// Nothing in here ever leaves the device except the three things the
/// leaderboard needs: the id, the nickname and the two totals. The words
/// practised, which scenes were played and how well - that is a child's
/// learning record and it stays local. See Docs/SupabaseSetup.md.
/// </summary>
[Serializable]
public class PlayerProfile
{
    /// <summary>UUID, made once when the profile is created. The identity.</summary>
    public string playerId;

    /// <summary>What the child typed. Empty until they have been asked.</summary>
    public string nickname = "";

    public string createdAt = "";
    public string lastPlayedAt = "";

    // ---- journey progress ----

    /// <summary>Index into JourneyMap.Steps of the furthest step unlocked.
    /// -1 means "never recorded", which is how the old PlayerPrefs save is
    /// told apart from a genuine start at step 0.</summary>
    public int highestUnlockedStep = -1;

    /// <summary>Scenes finished at least once, by scene name.</summary>
    public List<string> completedScenes = new List<string>();

    /// <summary>Best star rating per scene. LevelProgress keeps the best run,
    /// never the last, so this only ever goes up.</summary>
    public List<SceneStars> sceneStars = new List<SceneStars>();

    // ---- what the leaderboard shows ----

    /// <summary>Every point earned, over every run. Only ever added to.</summary>
    public int totalPoints;

    // ---- learning record ----

    /// <summary>Filipino words this child has said correctly, without repeats.
    ///
    /// Not the same thing as VocabularyMemory, which forgets at the end of a
    /// sitting on purpose so a word met again days later is taught properly
    /// rather than reminded. This is the lasting record: what has been
    /// learned, which is what a parent or a teacher would ask about.</summary>
    public List<string> wordsLearned = new List<string>();

    /// <summary>One entry per mini game. Nothing writes to this yet - there are
    /// no mini games - but the shape is here so the first one does not need a
    /// save-file migration to record a score.</summary>
    public List<MiniGameScore> miniGames = new List<MiniGameScore>();

    /// <summary>Almanac entries unlocked, by key. Same story as miniGames: the
    /// book on the menu is decoration until there is an Almanac behind it.</summary>
    public List<string> almanacEntries = new List<string>();

    // ---- leaderboard bookkeeping ----

    /// <summary>
    /// The secret the server handed back when this profile first registered,
    /// and which every later score submission has to present.
    ///
    /// A UUID the client made up is not authentication - anyone who learns it
    /// could post scores as this player. The token is what actually binds the
    /// row to this installation: it is made server-side, only ever stored here
    /// and as a hash in the database, and it is why a stolen player_id alone
    /// buys nothing. See Docs/SupabaseSetup.md.
    /// </summary>
    public string leaderboardToken = "";

    /// <summary>Totals the server has already been told about. When these are
    /// behind the real ones there is a submission waiting to be made, which is
    /// how a run finished on a plane reaches the board once there is signal.</summary>
    public int syncedPoints = -1;
    public int syncedStars = -1;
    public string syncedNickname = "";

    /// <summary>Stars earned across every scene - the leaderboard's tiebreak.</summary>
    public int TotalStars()
    {
        int total = 0;
        if (sceneStars != null)
        {
            foreach (SceneStars entry in sceneStars) total += entry.stars;
        }
        return total;
    }

    /// <summary>True when the board is out of date with what has been played.</summary>
    public bool NeedsLeaderboardSync()
    {
        return syncedPoints != totalPoints
            || syncedStars != TotalStars()
            || syncedNickname != nickname;
    }

    /// <summary>The name to show. Blank profiles fall back the way
    /// NicknameManager always has.</summary>
    public string DisplayName()
    {
        return string.IsNullOrWhiteSpace(nickname) ? NicknameManager.DefaultNickname : nickname;
    }

    /// <summary>
    /// True when this looks like the UUID the game made, and not something
    /// else that ended up in the save file.
    ///
    /// The id is written by Create() and never typed, so in ordinary play this
    /// is always true. It is checked anyway because the file it is read from is
    /// ordinary JSON on the device and can be hand-edited: the save file is
    /// input, not something the game knows. Everything the id is then used for
    /// - the row the leaderboard keys on, the argument the rank function is
    /// asked for - is better off refusing a value of the wrong shape than
    /// passing it on and finding out what the other end does with it.
    /// </summary>
    public bool HasValidId() => IsValidId(playerId);

    /// <summary>The UUID shape Create() writes: 8-4-4-4-12 hex digits.
    /// NicknameRules holds the check so a test can run it without Unity.</summary>
    public static bool IsValidId(string id) => NicknameRules.IsValidPlayerId(id);
}

/// <summary>One scene's best star rating. A pair, because JsonUtility cannot
/// write a dictionary.</summary>
[Serializable]
public class SceneStars
{
    public string scene;
    public int stars;
}

/// <summary>One mini game's record.</summary>
[Serializable]
public class MiniGameScore
{
    public string game;
    public int bestScore;
    public int timesPlayed;
}
