using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

/// <summary>
/// The save file: every profile on this device, and which one is playing.
///
/// Progress used to live in PlayerPrefs under one key per thing remembered -
/// SALINLAHI_Nickname, SALINLAHI_HighestUnlockedStep, SALINLAHI_Stars_&lt;scene&gt;.
/// That was fine while there was one unnamed player, but it cannot hold a
/// second child's record, and it has nowhere to keep a player id. So the whole
/// lot moved into one JSON file written to Application.persistentDataPath,
/// which on Android is inside the app's own private storage.
///
/// An existing save is NOT thrown away. The first time this runs on a device
/// that has played before, the old PlayerPrefs values are read once and become
/// profile one, keeping the nickname, the journey progress and every scene's
/// stars. See Migrate().
///
/// Three profiles, plus a row to make a new one: that is what the WHO ARE YOU?
/// board has slots for, and a cap that matches the art is better than one that
/// silently runs off the bottom of it.
///
/// Nothing here needs a GameObject - it is a file and a list - so it is static
/// for the same reason RunScore and VocabularyMemory are.
/// </summary>
public static class PlayerProfileStore
{
    /// <summary>Slots on the WHO ARE YOU? board, before the "create new" row.</summary>
    public const int MaxProfiles = 3;

    private const string FileName = "players.json";
    private const int CurrentVersion = 1;

    // The PlayerPrefs keys this replaces. Read once by Migrate(), never written.
    private const string LegacyNicknameKey = "SALINLAHI_Nickname";
    private const string LegacyStepKey = "SALINLAHI_HighestUnlockedStep";
    private const string LegacyLevelKey = "SALINLAHI_HighestUnlockedLevel";
    private const string LegacyStarsPrefix = "SALINLAHI_Stars_";
    private const string MigratedKey = "SALINLAHI_ProfilesMigrated";

    [Serializable]
    private class Book
    {
        public int version = CurrentVersion;
        public string activePlayerId = "";
        public List<PlayerProfile> players = new List<PlayerProfile>();
    }

    private static Book book;

    private static string Path => System.IO.Path.Combine(Application.persistentDataPath, FileName);

    // ---------- reading ----------

    /// <summary>
    /// The profile playing right now. Never null: a device with no save yet
    /// gets a blank one, so progress recorded before the child has been asked
    /// their name still has somewhere to go. Its nickname is empty until they
    /// answer, which is how the menu knows to ask - see NicknameManager.HasNickname.
    /// </summary>
    public static PlayerProfile Active
    {
        get
        {
            Load();
            PlayerProfile found = Find(book.activePlayerId);
            if (found != null) return found;
            if (book.players.Count > 0)
            {
                book.activePlayerId = book.players[0].playerId;
                Save();
                return book.players[0];
            }
            return Create("");
        }
    }

    /// <summary>Every profile on this device, oldest first.</summary>
    public static IList<PlayerProfile> All
    {
        get { Load(); return book.players.AsReadOnly(); }
    }

    /// <summary>True once at least one profile carries a name the child chose.</summary>
    public static bool HasNamedProfile
    {
        get
        {
            Load();
            foreach (PlayerProfile p in book.players)
            {
                if (!string.IsNullOrWhiteSpace(p.nickname)) return true;
            }
            return false;
        }
    }

    public static bool CanCreate
    {
        get { Load(); return book.players.Count < MaxProfiles; }
    }

    private static PlayerProfile Find(string playerId)
    {
        if (string.IsNullOrEmpty(playerId)) return null;
        foreach (PlayerProfile p in book.players)
        {
            if (p.playerId == playerId) return p;
        }
        return null;
    }

    // ---------- changing ----------

    /// <summary>
    /// Makes a profile and switches to it. The id is a fresh UUID and is the
    /// one thing about a profile that never changes - not the nickname, which
    /// is exactly why the nickname is not the identity.
    /// </summary>
    public static PlayerProfile Create(string nickname)
    {
        Load();
        PlayerProfile profile = new PlayerProfile
        {
            playerId = Guid.NewGuid().ToString(),
            nickname = Clean(nickname),
            createdAt = Now(),
            lastPlayedAt = Now(),
        };
        book.players.Add(profile);
        book.activePlayerId = profile.playerId;
        Save();
        return profile;
    }

    /// <summary>
    /// Renames a profile. Points, stars, levels and words are untouched: they
    /// hang off the player id, and the id is not what changed.
    /// </summary>
    public static void Rename(PlayerProfile profile, string nickname)
    {
        if (profile == null) return;
        Load();
        profile.nickname = Clean(nickname);
        Save();
    }

    public static void SetActive(PlayerProfile profile)
    {
        if (profile == null) return;
        Load();
        book.activePlayerId = profile.playerId;
        profile.lastPlayedAt = Now();
        Save();
    }

    /// <summary>
    /// Forgets a profile and everything it had played. The leaderboard row it
    /// made is left alone: it belongs to the child, not to this install, and
    /// deleting it from here would mean trusting the client to delete other
    /// people's rows too.
    /// </summary>
    public static void Delete(PlayerProfile profile)
    {
        if (profile == null) return;
        Load();
        book.players.Remove(profile);
        if (book.activePlayerId == profile.playerId)
        {
            book.activePlayerId = book.players.Count > 0 ? book.players[0].playerId : "";
        }
        Save();
    }

    // ---------- what the game records ----------

    /// <summary>Best rating a scene has been finished with, 0 if never.</summary>
    public static int StarsFor(string sceneName)
    {
        if (string.IsNullOrEmpty(sceneName)) return 0;
        foreach (SceneStars entry in Active.sceneStars)
        {
            if (entry.scene == sceneName) return entry.stars;
        }
        return 0;
    }

    /// <summary>Keeps the best run rather than the last. Returns true if this
    /// actually raised the rating, which is what makes it worth saving.</summary>
    public static bool RecordStars(string sceneName, int stars)
    {
        if (string.IsNullOrEmpty(sceneName)) return false;
        PlayerProfile profile = Active;
        foreach (SceneStars entry in profile.sceneStars)
        {
            if (entry.scene != sceneName) continue;
            if (stars <= entry.stars) return false;
            entry.stars = stars;
            Save();
            return true;
        }
        profile.sceneStars.Add(new SceneStars { scene = sceneName, stars = stars });
        Save();
        return true;
    }

    public static void MarkSceneComplete(string sceneName)
    {
        if (string.IsNullOrEmpty(sceneName)) return;
        PlayerProfile profile = Active;
        if (profile.completedScenes.Contains(sceneName)) return;
        profile.completedScenes.Add(sceneName);
        Save();
    }

    /// <summary>
    /// Adds one finished run to the running totals: the points it scored and
    /// the words that were said correctly. Called once per level, by
    /// SessionScoreTracker, at the same moment the result is saved.
    /// </summary>
    public static void RecordRun(int points, IEnumerable<string> wordsPractised)
    {
        PlayerProfile profile = Active;
        if (points > 0) profile.totalPoints += points;
        if (wordsPractised != null)
        {
            foreach (string word in wordsPractised)
            {
                if (string.IsNullOrWhiteSpace(word)) continue;
                string trimmed = word.Trim();
                bool seen = false;
                foreach (string known in profile.wordsLearned)
                {
                    if (string.Equals(known, trimmed, StringComparison.OrdinalIgnoreCase))
                    {
                        seen = true;
                        break;
                    }
                }
                if (!seen) profile.wordsLearned.Add(trimmed);
            }
        }
        profile.lastPlayedAt = Now();
        Save();
    }

    /// <summary>Records a mini game result. Nothing calls this yet; it is here
    /// so the first mini game does not need the save file to change shape.</summary>
    public static void RecordMiniGame(string game, int score)
    {
        if (string.IsNullOrWhiteSpace(game)) return;
        PlayerProfile profile = Active;
        foreach (MiniGameScore entry in profile.miniGames)
        {
            if (entry.game != game) continue;
            entry.timesPlayed++;
            if (score > entry.bestScore) entry.bestScore = score;
            Save();
            return;
        }
        profile.miniGames.Add(new MiniGameScore { game = game, bestScore = score, timesPlayed = 1 });
        Save();
    }

    /// <summary>Unlocks an Almanac entry. Same story as RecordMiniGame: the
    /// book on the menu is decoration until there is an Almanac behind it.</summary>
    public static void UnlockAlmanacEntry(string key)
    {
        if (string.IsNullOrWhiteSpace(key)) return;
        PlayerProfile profile = Active;
        if (profile.almanacEntries.Contains(key)) return;
        profile.almanacEntries.Add(key);
        Save();
    }

    // ---------- the file ----------

    /// <summary>Writes the file. Public because the leaderboard client changes
    /// a profile's sync bookkeeping and has to be able to keep it.</summary>
    public static void Save()
    {
        if (book == null) return;
        try
        {
            File.WriteAllText(Path, JsonUtility.ToJson(book, true));
        }
        catch (Exception e)
        {
            // A save that cannot be written is bad, but a crash on the results
            // screen is worse: the child keeps playing and this run is lost.
            Debug.LogWarning("PlayerProfileStore: could not write " + Path + " - " + e.Message);
        }
    }

    private static void Load()
    {
        if (book != null) return;

        book = new Book();
        try
        {
            if (File.Exists(Path))
            {
                Book read = JsonUtility.FromJson<Book>(File.ReadAllText(Path));
                if (read != null && read.players != null) book = read;
            }
        }
        catch (Exception e)
        {
            // A corrupt file is treated as no file. Overwriting it loses a
            // save, but refusing to start loses the game.
            Debug.LogWarning("PlayerProfileStore: could not read " + Path +
                              ", starting fresh - " + e.Message);
            book = new Book();
        }

        Repair();
        Migrate();
    }

    /// <summary>Fills in anything an older or hand-edited file left out, so the
    /// rest of this class never has to test a list for null.</summary>
    private static void Repair()
    {
        if (book.players == null) book.players = new List<PlayerProfile>();
        foreach (PlayerProfile p in book.players)
        {
            if (string.IsNullOrEmpty(p.playerId)) p.playerId = Guid.NewGuid().ToString();
            if (p.nickname == null) p.nickname = "";
            if (p.completedScenes == null) p.completedScenes = new List<string>();
            if (p.sceneStars == null) p.sceneStars = new List<SceneStars>();
            if (p.wordsLearned == null) p.wordsLearned = new List<string>();
            if (p.miniGames == null) p.miniGames = new List<MiniGameScore>();
            if (p.almanacEntries == null) p.almanacEntries = new List<string>();
            if (p.leaderboardToken == null) p.leaderboardToken = "";
            if (p.syncedNickname == null) p.syncedNickname = "";
        }
    }

    /// <summary>
    /// Carries a pre-profile save over, once.
    ///
    /// Someone who has played this game before has a nickname, a journey
    /// position and a star rating per scene sitting in PlayerPrefs. Updating
    /// the menu must not cost them any of it, so the first run after the
    /// update reads those keys and builds profile one out of them.
    ///
    /// The old keys are left where they are rather than deleted: a flag says
    /// the move has happened, which means an install that is rolled back still
    /// finds its save, and running this twice cannot duplicate a profile.
    /// </summary>
    private static void Migrate()
    {
        if (PlayerPrefs.GetInt(MigratedKey, 0) == 1) return;
        PlayerPrefs.SetInt(MigratedKey, 1);
        PlayerPrefs.Save();

        if (book.players.Count > 0) return;   // a save file already exists

        bool hadNickname = PlayerPrefs.HasKey(LegacyNicknameKey);
        bool hadProgress = PlayerPrefs.HasKey(LegacyStepKey) || PlayerPrefs.HasKey(LegacyLevelKey);
        if (!hadNickname && !hadProgress) return;    // nothing played here before

        PlayerProfile profile = new PlayerProfile
        {
            playerId = Guid.NewGuid().ToString(),
            nickname = Clean(PlayerPrefs.GetString(LegacyNicknameKey, "")),
            createdAt = Now(),
            lastPlayedAt = Now(),
            highestUnlockedStep = PlayerPrefs.HasKey(LegacyStepKey)
                ? PlayerPrefs.GetInt(LegacyStepKey, -1) : -1,
        };

        // Stars were keyed by scene name, and the journey knows every scene
        // there is, so the whole lot can be read back without a key list.
        foreach (JourneyMap.Step step in JourneyMap.Steps)
        {
            if (!step.IsPlayable) continue;
            int stars = PlayerPrefs.GetInt(LegacyStarsPrefix + step.sceneName, 0);
            if (stars <= 0) continue;
            profile.sceneStars.Add(new SceneStars { scene = step.sceneName, stars = stars });
            profile.completedScenes.Add(step.sceneName);
        }

        book.players.Add(profile);
        book.activePlayerId = profile.playerId;
        Save();
        Debug.Log("PlayerProfileStore: carried an existing save over into a profile - \"" +
                   profile.DisplayName() + "\", step " + profile.highestUnlockedStep +
                   ", " + profile.sceneStars.Count + " scene(s) rated.");
    }

    private static string Clean(string nickname) =>
        string.IsNullOrWhiteSpace(nickname) ? "" : nickname.Trim();

    private static string Now() =>
        DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ");

    /// <summary>Forgets the loaded copy so the next read comes off disk.
    /// Only for the editor tools and tests - the game never needs it.</summary>
    public static void Unload()
    {
        book = null;
    }
}
