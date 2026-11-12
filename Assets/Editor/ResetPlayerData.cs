// Assets/Editor/ResetPlayerData.cs
//
// Puts this device back to how it was before anyone played: no profile, no
// name, no progress. For testing the first-run path, which you only get to
// see once per machine otherwise.
//
// Two stores have to go, and they are in different places:
//
//   players.json in Application.persistentDataPath - the profiles themselves,
//   written by PlayerProfileStore.
//
//   the PlayerPrefs keys that predate it - the old nickname, journey position
//   and per-scene stars, plus the flag saying they have already been carried
//   over. Deleting the save file alone would leave those behind, and they are
//   exactly what would be read back in if the flag were ever cleared.
//
// Volume and mute are deliberately NOT touched. They are settings for the
// device, not progress for a player, and nobody resetting a save wants the
// music to come back on.
//
// This has to be a menu item rather than something done to the files from
// outside, because the Editor keeps PlayerPrefs in memory while it is open
// and writes them out on its own schedule - edit the plist underneath it and
// the change is simply overwritten.
//
// Run via: Tools > SALINLAHI > Reset Player Data

using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

public class ResetPlayerData
{
    private const string SaveFile = "players.json";

    /// <summary>The keys PlayerProfileStore replaced, which it leaves in place
    /// on purpose so a rolled-back build still finds its save.</summary>
    private static readonly string[] LegacyKeys =
    {
        "SALINLAHI_Nickname",
        "SALINLAHI_HighestUnlockedStep",
        "SALINLAHI_HighestUnlockedLevel",
        "SALINLAHI_ProfilesMigrated",
    };

    private const string StarsPrefix = "SALINLAHI_Stars_";

    [MenuItem("Tools/SALINLAHI/Reset Player Data")]
    public static void Reset()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            Debug.LogError("[SALINLAHI] Stop Play mode first - the game has the save file open.");
            return;
        }

        string path = Path.Combine(Application.persistentDataPath, SaveFile);
        List<string> found = Describe(path);

        if (found.Count == 0)
        {
            Debug.Log("[SALINLAHI] Nothing to reset - this device has no saved players.");
            return;
        }

        if (!EditorUtility.DisplayDialog(
                "Reset player data?",
                "This device will forget:\n\n  " + string.Join("\n  ", found) +
                "\n\nThe next launch asks for a nickname as if the game had just been " +
                "installed. Volume and mute are kept.\n\nThis cannot be undone.",
                "Reset", "Cancel"))
        {
            return;
        }

        int keys = 0;
        foreach (string key in LegacyKeys)
        {
            if (!PlayerPrefs.HasKey(key)) continue;
            PlayerPrefs.DeleteKey(key);
            keys++;
        }
        // Stars were keyed by scene name, and the journey knows every scene
        // there is - the same way PlayerProfileStore.Migrate() reads them.
        foreach (JourneyMap.Step step in JourneyMap.Steps)
        {
            if (!step.IsPlayable) continue;
            string key = StarsPrefix + step.sceneName;
            if (!PlayerPrefs.HasKey(key)) continue;
            PlayerPrefs.DeleteKey(key);
            keys++;
        }
        PlayerPrefs.Save();

        bool removedFile = File.Exists(path);
        if (removedFile) File.Delete(path);

        // Drop the copy this Editor session is holding, so anything that asks
        // for a profile next reads an empty device rather than the one that
        // was just deleted.
        PlayerProfileStore.Unload();

        Debug.Log("[SALINLAHI] Player data reset" +
                   (removedFile ? " - deleted " + path : "") +
                   (keys > 0 ? " and cleared " + keys + " old PlayerPrefs key(s)" : "") +
                   ". Volume settings kept. Press Play and the menu will ask for a nickname.");

        // Worth saying out loud rather than leaving to be discovered: the
        // board keys on a player id, and resetting makes a new one. The row
        // this device registered stays where it is, and the token that could
        // have updated it has just been deleted, so it can never be changed
        // again - only left or removed by hand in the Supabase dashboard.
        if (!string.IsNullOrEmpty(PlayerPrefs.GetString("SALINLAHI_Nickname", "")))
        {
            Debug.Log("[SALINLAHI] If this player had already reached the leaderboard, their " +
                       "row is still on it under the old player id. Delete it in the Supabase " +
                       "dashboard if you want the board empty too.");
        }
    }

    /// <summary>What is actually on this device, in words, for the dialog.</summary>
    private static List<string> Describe(string path)
    {
        List<string> found = new List<string>();

        if (File.Exists(path))
        {
            foreach (PlayerProfile profile in PlayerProfileStore.All)
            {
                found.Add("\"" + profile.DisplayName() + "\" - " +
                           profile.completedScenes.Count + " scene(s) played, " +
                           profile.TotalStars() + " star(s), " +
                           profile.totalPoints + " point(s)");
            }
            if (found.Count == 0) found.Add("an empty save file");
        }

        foreach (string key in LegacyKeys)
        {
            if (!PlayerPrefs.HasKey(key)) continue;
            found.Add("older progress saved before profiles existed");
            break;
        }
        return found;
    }
}
