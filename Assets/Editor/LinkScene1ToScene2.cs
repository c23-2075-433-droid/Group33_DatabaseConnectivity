// Assets/Editor/LinkScene1ToScene2.cs
//
// Joins Bahay Scene 1 to Scene 2 and moves the "level finished" moment to
// the end of Scene 2, which is now where the playable content stops.
//
// Scene 1 (bedroom):
//   * exit trigger loads Chapter1_Level2_Banyo instead of showing a panel
//   * the placeholder complete panel and the database demo are removed
//   * gains a fader so leaving the room fades instead of cutting
//
// Scene 2 (hallway + bathroom):
//   * gains the database demo - score saved, recent records read back and
//     shown on the level-complete panel once all five words are done
//   * its OKAY button returns to the journey map
//   * gains a fader so arriving fades in
//
// Safe to re-run.
//
// Run via: Tools > SALINLAHI > Link Scene 1 To Scene 2

using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public class LinkScene1ToScene2
{
    private const string Scene1Path = "Assets/Scenes/Chapter1_Level1_UmagaNa.unity";
    private const string Scene2Path = "Assets/Scenes/Chapter1_Level2_Banyo.unity";
    private const string Scene2Name = "Chapter1_Level2_Banyo";
    private const string MapSceneName = "LevelSelect";

    [MenuItem("Tools/SALINLAHI/Link Scene 1 To Scene 2")]
    public static void Link()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            Debug.LogError("[SALINLAHI] Stop Play mode first - this edits scenes, which Unity doesn't allow while the game is running.");
            return;
        }

        if (!System.IO.File.Exists(Scene2Path))
        {
            Debug.LogError("[SALINLAHI] " + Scene2Path + " does not exist yet - run Build Bahay Scene 2 first.");
            return;
        }

        // ---- Scene 1: hand off to Scene 2, drop the ending ----
        Scene s1 = EditorSceneManager.OpenScene(Scene1Path, OpenSceneMode.Single);

        ExitDoorTrigger exit1 = Object.FindFirstObjectByType<ExitDoorTrigger>(FindObjectsInactive.Include);
        if (exit1 == null)
        {
            Debug.LogError("[SALINLAHI] No ExitDoorTrigger in " + Scene1Path +
                            " - run Add Exit Trigger To Level 1 first.");
            return;
        }

        DatabaseDemoBuilder.Remove();          // panel + Database object
        exit1.nextSceneName = Scene2Name;      // now a doorway, not an ending
        exit1.delayBeforeLoad = 0.2f;          // brief beat before the fade
        SceneFaderBuilder.Build();

        EditorSceneManager.SaveScene(s1);
        Debug.Log("[SALINLAHI] Scene 1 now leads to " + Scene2Name +
                   ", with the complete panel and database demo removed.");

        // ---- Scene 2: becomes the ending ----
        Scene s2 = EditorSceneManager.OpenScene(Scene2Path, OpenSceneMode.Single);

        SceneFaderBuilder.Build();
        bool ok = DatabaseDemoBuilder.Build("Bahay - Scene 2: Punta sa Banyo", MapSceneName);

        EditorSceneManager.SaveScene(s2);

        if (ok)
        {
            Debug.Log("[SALINLAHI] Scene 2 now saves the session and shows the recent scores when " +
                       "all five words are finished. OKAY returns to " + MapSceneName + ". " +
                       "Both scenes fade in and out.");
        }
    }
}
