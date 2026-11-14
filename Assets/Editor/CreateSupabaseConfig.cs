// Assets/Editor/CreateSupabaseConfig.cs
//
// Creates the local, git-ignored SupabaseConfig asset that SupabaseClient and
// LeaderboardClient read their credentials from. Each group member runs this
// once after cloning the repo, then pastes their project URL + anon key into
// the Inspector.
//
// The asset lives in Assets/Resources/ and is listed in .gitignore, so
// credentials never get committed (Activity 5 security reminder).
//
// The asset is also given a FIXED GUID. See SharedGuid below - without it the
// scenes cannot find the thing this tool just made.
//
// Run via: Tools > SALINLAHI > Create Supabase Config

using System.IO;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

public class CreateSupabaseConfig
{
    private const string FolderPath = "Assets/Resources";
    private const string AssetPath = FolderPath + "/SupabaseConfig.asset";
    private const string MetaPath = AssetPath + ".meta";

    /// <summary>
    /// The GUID this asset always gets, on every machine.
    ///
    /// Unity wires a reference in a scene by the target's GUID, not by its
    /// path, and three committed scenes hold a reference to this asset:
    /// MainMenu (SupabaseClient and LeaderboardClient both),
    /// Chapter1_Level5_Almusal and Chapter2_Level5_Uwian. The asset itself is
    /// git-ignored, so it is the one thing those scenes point at that does
    /// not arrive with a clone.
    ///
    /// Left alone, Unity gives a newly created asset a random GUID. The
    /// member who ran this tool would then have a perfectly good config that
    /// no scene refers to: every reference stays null, every client says it
    /// is "not configured", and nothing anywhere says why - the asset is
    /// right there in the Project window. Pinning the GUID is what makes the
    /// config this tool creates the config those scenes were built against.
    ///
    /// The value is the one the committed scenes already carry, so it must
    /// not be changed. If it ever has to be, the three scenes have to be
    /// rebuilt or re-wired in the same commit.
    /// </summary>
    private const string SharedGuid = "169aa69e25ca247ba9ca3f28c6179c71";

    [MenuItem("Tools/SALINLAHI/Create Supabase Config")]
    public static void CreateConfig()
    {
        SupabaseConfig existing = AssetDatabase.LoadAssetAtPath<SupabaseConfig>(AssetPath);
        if (existing != null)
        {
            // An asset made before this tool pinned the GUID is the case worth
            // catching here: it holds credentials that work and is still
            // invisible to the scenes. Repointing it touches the .meta only,
            // so whatever is typed into it survives.
            bool repointed = PinGuid();
            Selection.activeObject = AssetDatabase.LoadAssetAtPath<SupabaseConfig>(AssetPath);
            EditorGUIUtility.PingObject(Selection.activeObject);
            Debug.Log("[SALINLAHI] SupabaseConfig already exists at " + AssetPath +
                       (repointed
                           ? " - and has been repointed to the GUID the scenes expect, so the " +
                             "clients will find it now. Whatever was typed into it is unchanged."
                           : " - selecting it. Fill in the Project URL and anon key in the Inspector."));
            return;
        }

        if (!Directory.Exists(FolderPath))
        {
            Directory.CreateDirectory(FolderPath);
            AssetDatabase.Refresh();
        }

        SupabaseConfig config = ScriptableObject.CreateInstance<SupabaseConfig>();
        AssetDatabase.CreateAsset(config, AssetPath);
        AssetDatabase.SaveAssets();
        PinGuid();

        // Re-loaded rather than reused: the instance from before the reimport
        // belongs to the asset under its old GUID.
        Selection.activeObject = AssetDatabase.LoadAssetAtPath<SupabaseConfig>(AssetPath);
        EditorGUIUtility.PingObject(Selection.activeObject);

        Debug.Log("[SALINLAHI] Created " + AssetPath + ". Paste your Supabase Project URL and " +
                   "anon key into it (Project Settings > API in the Supabase dashboard). " +
                   "This asset is git-ignored, so your keys stay out of the repository. " +
                   "Full steps: Docs/SupabaseSetup.md");
    }

    /// <summary>
    /// Gives the asset SharedGuid, by rewriting the one line of its .meta that
    /// carries the GUID and importing it again. Returns true if it had to
    /// change anything.
    ///
    /// The .meta is edited rather than the asset because the GUID is the only
    /// thing being changed: the asset's own contents, credentials included,
    /// are never read or written here.
    ///
    /// Anything that goes wrong is reported rather than swallowed. A silent
    /// failure here produces exactly the symptom this method exists to
    /// prevent - a config that looks right and that no scene can see.
    /// </summary>
    private static bool PinGuid()
    {
        if (!File.Exists(MetaPath))
        {
            Debug.LogWarning("[SALINLAHI] No " + MetaPath + " to pin, so the config keeps " +
                              "whatever GUID Unity gave it. The scenes will not find it until " +
                              "it is dragged onto the clients by hand.");
            return false;
        }

        string meta = File.ReadAllText(MetaPath);
        if (meta.Contains("guid: " + SharedGuid)) return false;   // already right

        string pinned = Regex.Replace(meta, @"^guid: [0-9a-fA-F]{32}\s*$",
                                       "guid: " + SharedGuid, RegexOptions.Multiline);
        if (pinned == meta)
        {
            Debug.LogWarning("[SALINLAHI] Could not find a guid line in " + MetaPath +
                              " to pin. The config keeps the GUID Unity gave it, and the " +
                              "scenes will not find it until it is dragged onto the clients " +
                              "by hand.");
            return false;
        }

        File.WriteAllText(MetaPath, pinned);
        // ForceUpdate, because only the .meta changed: without it Unity sees
        // an asset whose own bytes are untouched and leaves the import alone.
        AssetDatabase.Refresh(ImportAssetOptions.ForceUpdate);

        // Said out loud either way. Unity can refuse a GUID it considers
        // taken, and the whole point of this is that the failure is loud.
        string landed = AssetDatabase.AssetPathToGUID(AssetPath);
        if (landed != SharedGuid)
        {
            Debug.LogError("[SALINLAHI] Tried to give " + AssetPath + " the GUID the scenes " +
                            "expect (" + SharedGuid + ") but it came back as '" + landed +
                            "'. Until that is sorted the clients will say the leaderboard is " +
                            "not configured; dragging the asset onto SupabaseClient and " +
                            "LeaderboardClient in MainMenu, Chapter1_Level5_Almusal and " +
                            "Chapter2_Level5_Uwian is the way round it.");
            return false;
        }

        return true;
    }
}
