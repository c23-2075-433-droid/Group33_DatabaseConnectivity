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
        => Build(levelName, returnSceneName, 0, null);

    /// <summary>
    /// levelIndex is the 1-based number on the journey map. Passing it lets
    /// finishing the level unlock the next node and award its badge; 0 means
    /// "not a level ending", and neither happens.
    /// </summary>
    public static bool Build(string levelName, string returnSceneName,
                             int levelIndex, string badgeSpriteFile)
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

        // The same summary every other scene ends on, so a level's last scene
        // does not look like a different game. LevelCompleteUIBuilder and its
        // ui_summary_* artwork are left in the project but no longer called.
        SceneResultPanelBuilder.Built summary = SceneResultPanelBuilder.Build(
            controller, voiceCommand, "", "LEVEL COMPLETE", levelEnd: true);
        GameObject panel = summary.canvas;

        // Next has nowhere further to go at the end of a level, so it returns
        // here - the journey map, usually.
        summary.panel.levelSelectSceneName = returnSceneName;

        ExitDoorTrigger exitTrigger = Object.FindFirstObjectByType<ExitDoorTrigger>(FindObjectsInactive.Include);
        if (exitTrigger != null) exitTrigger.levelCompleteUI = summary.content;

        GameObject host = GameObject.Find(HostName);
        if (host == null) host = new GameObject(HostName);

        SupabaseClient client = host.GetComponent<SupabaseClient>();
        if (client == null) client = host.AddComponent<SupabaseClient>();
        client.config = AssetDatabase.LoadAssetAtPath<SupabaseConfig>(ConfigPath);

        SessionScoreTracker tracker = host.GetComponent<SessionScoreTracker>();
        if (tracker == null) tracker = host.AddComponent<SessionScoreTracker>();
        tracker.objectiveController = controller;
        tracker.supabaseClient = client;
        // The summary opens itself once it is handed the level's totals, so the
        // tracker is given no panel of its own to show - otherwise both would,
        // and the tracker's would appear instantly, skipping the beat the
        // summary leaves for the last word to land.
        tracker.levelCompletePanel = null;
        tracker.resultPanel = summary.panel;
        tracker.scoreText = null;          // the summary writes its own score line
        tracker.recordsText = summary.recordsText;
        tracker.levelName = levelName;
        tracker.levelIndex = levelIndex;

        // The three statistic rows and the tracker's own star handling belong
        // to LevelCompleteUIBuilder's board, which this no longer builds. The
        // summary rates its own stars, and every one of these fields is guarded
        // against being empty.

        // Counting lives here, not on the tracker, so every scene of a level
        // adds to the same running total (RunScore). This is a later scene of
        // the level, so it must not reset it - the first scene does that.
        RunScoreCounter counter = host.GetComponent<RunScoreCounter>();
        if (counter == null) counter = host.AddComponent<RunScoreCounter>();
        counter.voiceCommand = voiceCommand;
        counter.resetOnStart = false;

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
