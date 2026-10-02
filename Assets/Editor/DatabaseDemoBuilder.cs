// Assets/Editor/DatabaseDemoBuilder.cs
//
// Adds (or removes) the Activity 5 database demo in whichever scene is open:
// the Database object carrying SupabaseClient and SessionScoreTracker, plus
// the level-complete panel the retrieved records are displayed on.
//
// This used to live inside AddDatabaseDemoToLevel1 and was hardcoded to
// Level 1. It was pulled out so the demo can follow the end of the game as
// more scenes are added - it belongs on whichever scene the player finishes
// on, not permanently on the first one.

using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using System.Collections.Generic;

public static class DatabaseDemoBuilder
{
    private const string ConfigPath = "Assets/Resources/SupabaseConfig.asset";
    public const string HostName = "Database";

    /// <summary>
    /// Wires the save/retrieve/display demo into the open scene. Returns false
    /// if the scene is missing the voice or objective components it needs.
    /// </summary>
    public static bool Build(string levelName, string returnSceneName)
    {
        VoiceCommand voiceCommand = Object.FindFirstObjectByType<VoiceCommand>(FindObjectsInactive.Include);
        SceneObjectiveController controller = Object.FindFirstObjectByType<SceneObjectiveController>(FindObjectsInactive.Include);

        if (voiceCommand == null || controller == null)
        {
            Debug.LogError("[SALINLAHI] Scene needs a VoiceCommand and a SceneObjectiveController before " +
                            "the database demo can be added. (VoiceCommand: " + (voiceCommand != null) +
                            ", SceneObjectiveController: " + (controller != null) + ")");
            return false;
        }

        GameObject panel = LevelCompleteUIBuilder.BuildPanel("Magaling!");
        Transform panelRoot = panel.transform.Find("Panel");
        Text scoreText = FindText(panelRoot, "ScoreText");
        Text recordsText = FindText(panelRoot, "RecordsText");

        // The panel's OKAY button goes back to whatever scene this one should
        // return to (usually the journey map).
        Transform okay = panelRoot != null ? panelRoot.Find("OkayButton") : null;
        if (okay != null)
        {
            LoadSceneOnClick loader = okay.GetComponent<LoadSceneOnClick>();
            if (loader != null) loader.sceneName = returnSceneName;
        }

        ExitDoorTrigger exitTrigger = Object.FindFirstObjectByType<ExitDoorTrigger>(FindObjectsInactive.Include);
        if (exitTrigger != null) exitTrigger.levelCompleteUI = panel;

        GameObject host = GameObject.Find(HostName);
        if (host == null) host = new GameObject(HostName);

        SupabaseClient client = host.GetComponent<SupabaseClient>();
        if (client == null) client = host.AddComponent<SupabaseClient>();
        client.config = AssetDatabase.LoadAssetAtPath<SupabaseConfig>(ConfigPath);

        SessionScoreTracker tracker = host.GetComponent<SessionScoreTracker>();
        if (tracker == null) tracker = host.AddComponent<SessionScoreTracker>();
        tracker.voiceCommand = voiceCommand;
        tracker.objectiveController = controller;
        tracker.supabaseClient = client;
        tracker.levelCompletePanel = panel;
        tracker.scoreText = scoreText;
        tracker.recordsText = recordsText;
        tracker.levelName = levelName;

        if (client.config == null)
        {
            Debug.LogWarning("[SALINLAHI] " + ConfigPath + " not found - the demo is wired but will " +
                              "report that it isn't configured. Run Tools > SALINLAHI > Create Supabase Config.");
        }
        return true;
    }

    /// <summary>
    /// Strips the demo out of the open scene: the Database object and the
    /// level-complete panel, and clears the exit trigger's reference to it.
    /// Used when the end of the game moves to a later scene.
    /// </summary>
    public static void Remove()
    {
        ExitDoorTrigger exitTrigger = Object.FindFirstObjectByType<ExitDoorTrigger>(FindObjectsInactive.Include);
        if (exitTrigger != null) exitTrigger.levelCompleteUI = null;

        int removed = 0;
        foreach (GameObject go in FindAllInScene("LevelComplete_Canvas")) { Object.DestroyImmediate(go); removed++; }
        foreach (GameObject go in FindAllInScene(HostName)) { Object.DestroyImmediate(go); removed++; }
        Debug.Log("[SALINLAHI] Removed " + removed + " database-demo object(s) from the open scene.");
    }

    private static Text FindText(Transform parent, string name)
    {
        if (parent == null) return null;
        Transform t = parent.Find(name);
        return t != null ? t.GetComponent<Text>() : null;
    }

    private static List<GameObject> FindAllInScene(string name)
    {
        List<GameObject> matches = new List<GameObject>();
        foreach (GameObject root in SceneManager.GetActiveScene().GetRootGameObjects())
            Collect(root.transform, name, matches);
        return matches;
    }

    private static void Collect(Transform t, string name, List<GameObject> matches)
    {
        if (t.name == name) matches.Add(t.gameObject);
        for (int i = 0; i < t.childCount; i++) Collect(t.GetChild(i), name, matches);
    }
}
