// Assets/Editor/CreateSupabaseConfig.cs
//
// Creates the local, git-ignored SupabaseConfig asset that SupabaseClient
// reads its credentials from. Each group member runs this once after cloning
// the repo, then pastes their project URL + anon key into the Inspector.
//
// The asset lives in Assets/Resources/ and is listed in .gitignore, so
// credentials never get committed (Activity 5 security reminder).
//
// Run via: Tools > SALINLAHI > Create Supabase Config

using System.IO;
using UnityEditor;
using UnityEngine;

public class CreateSupabaseConfig
{
    private const string FolderPath = "Assets/Resources";
    private const string AssetPath = FolderPath + "/SupabaseConfig.asset";

    [MenuItem("Tools/SALINLAHI/Create Supabase Config")]
    public static void CreateConfig()
    {
        SupabaseConfig existing = AssetDatabase.LoadAssetAtPath<SupabaseConfig>(AssetPath);
        if (existing != null)
        {
            Debug.Log("[SALINLAHI] SupabaseConfig already exists at " + AssetPath +
                       " - selecting it. Fill in the Project URL and anon key in the Inspector.");
            Selection.activeObject = existing;
            EditorGUIUtility.PingObject(existing);
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

        Selection.activeObject = config;
        EditorGUIUtility.PingObject(config);

        Debug.Log("[SALINLAHI] Created " + AssetPath + ". Paste your Supabase Project URL and " +
                   "anon key into it (Project Settings > API in the Supabase dashboard). " +
                   "This asset is git-ignored, so your keys stay out of the repository. " +
                   "Full steps: Docs/SupabaseSetup.md");
    }
}
