// Assets/Editor/AddDatabaseDemoToLevel1.cs
//
// Wires the Activity 5 database-connectivity demo into the already-built
// Level 1 scene, without disturbing the gameplay that's already there:
//
//   * Adds a "Database" GameObject carrying SupabaseClient (REST calls) and
//     SessionScoreTracker (counts the session, saves it, reads records back).
//   * Rebuilds the level-complete panel so it has the TitleText / ScoreText /
//     RecordsText fields the tracker writes into.
//   * Wires every reference, including ExitDoorTrigger's pointer to the
//     rebuilt panel.
//
// Safe to re-run: existing objects are reused and re-wired rather than
// duplicated.
//
// Requires the Supabase credentials asset to exist for the demo to actually
// reach the database - create it with Tools > SALINLAHI > Create Supabase
// Config (see Docs/SupabaseSetup.md). Wiring still succeeds without it; the
// game just reports that it isn't configured.
//
// Run via: Tools > SALINLAHI > Add Database Demo To Level 1

using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class AddDatabaseDemoToLevel1
{
    private const string ScenePath = "Assets/Scenes/Chapter1_Level1_UmagaNa.unity";
    private const string ConfigPath = "Assets/Resources/SupabaseConfig.asset";
    private const string HostName = "Database";

    [MenuItem("Tools/SALINLAHI/Add Database Demo To Level 1")]
    public static void AddDemo()
    {
        Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);

        VoiceCommand voiceCommand = Object.FindFirstObjectByType<VoiceCommand>(FindObjectsInactive.Include);
        SceneObjectiveController objectiveController = Object.FindFirstObjectByType<SceneObjectiveController>(FindObjectsInactive.Include);

        if (voiceCommand == null || objectiveController == null)
        {
            Debug.LogError("[SALINLAHI] Need VoiceCommand + SceneObjectiveController in " + ScenePath +
                            " - run 'Migrate Level 1 To Objective System' first. " +
                            "(VoiceCommand: " + (voiceCommand != null) +
                            ", SceneObjectiveController: " + (objectiveController != null) + ")");
            return;
        }

        // Rebuild the completion panel so it definitely has the score/records
        // text fields this demo writes into.
        GameObject panel = LevelCompleteUIBuilder.BuildPanel("Great job! You helped Kylo wake up!");
        Transform panelRoot = panel.transform.Find("Panel");
        Text scoreText = FindText(panelRoot, "ScoreText");
        Text recordsText = FindText(panelRoot, "RecordsText");

        // Point the exit trigger at the rebuilt panel - its old reference died
        // with the panel it used to point at.
        ExitDoorTrigger exitTrigger = Object.FindFirstObjectByType<ExitDoorTrigger>(FindObjectsInactive.Include);
        if (exitTrigger != null) exitTrigger.levelCompleteUI = panel;

        // Host object for the two database components.
        GameObject host = GameObject.Find(HostName);
        if (host == null) host = new GameObject(HostName);

        SupabaseClient client = host.GetComponent<SupabaseClient>();
        if (client == null) client = host.AddComponent<SupabaseClient>();

        SupabaseConfig config = AssetDatabase.LoadAssetAtPath<SupabaseConfig>(ConfigPath);
        client.config = config;

        SessionScoreTracker tracker = host.GetComponent<SessionScoreTracker>();
        if (tracker == null) tracker = host.AddComponent<SessionScoreTracker>();

        tracker.voiceCommand = voiceCommand;
        tracker.objectiveController = objectiveController;
        tracker.supabaseClient = client;
        tracker.levelCompletePanel = panel;
        tracker.scoreText = scoreText;
        tracker.recordsText = recordsText;
        tracker.levelName = "Bahay - Scene 1: Umaga na!";

        EditorSceneManager.SaveScene(scene);

        Debug.Log("[SALINLAHI] Database demo wired into " + ScenePath + ". " +
                   "Finishing the Bangon/Tayo/Lakad sequence now saves a record, reads recent " +
                   "records back, and shows them on the completion panel. " +
                   (config != null
                       ? "Using credentials from " + ConfigPath + "."
                       : "NOTE: " + ConfigPath + " not found - run 'Create Supabase Config' and fill " +
                         "in your project URL + anon key, then re-run this command."));
    }

    private static Text FindText(Transform parent, string name)
    {
        if (parent == null) return null;
        Transform t = parent.Find(name);
        return t != null ? t.GetComponent<Text>() : null;
    }
}
