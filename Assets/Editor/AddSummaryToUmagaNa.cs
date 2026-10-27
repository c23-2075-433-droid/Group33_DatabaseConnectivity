// Assets/Editor/AddSummaryToUmagaNa.cs
//
// Gives Chapter 1 Level 1 the summary panel every other scene ends on.
//
// It was the last scene without one, and the only one that could not simply
// be rebuilt: its builder was deleted in 3469503, so the scene is kept by
// hand. This is a patcher rather than a builder - it adds the panel and the
// one wire it needs, and touches nothing else in the scene.
//
// The scene also ends differently from the rest. Its objectives are Bangon,
// Tayo and Lakad, all finished while the child still has to walk out of the
// room, and the door is what ends it - the only ExitDoorTrigger in the game.
// So the panel is set to wait for a cue and the door gives it, instead of
// the door loading the next scene itself. CONTINUE now does that, the same
// as everywhere else.
//
// Safe to run twice: SceneResultPanelBuilder destroys any panel it finds
// before building, and the wiring below is set from scratch each time.
//
// Run via: Tools > SALINLAHI > Add Summary To Umaga Na

using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class AddSummaryToUmagaNa
{
    private const string ScenePath = "Assets/Scenes/Chapter1_Level1_UmagaNa.unity";

    [MenuItem("Tools/SALINLAHI/Add Summary To Umaga Na")]
    public static void AddSummary()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            Debug.LogError("[SALINLAHI] Stop Play mode first - this reopens the scene, which Unity doesn't allow while the game is running.");
            return;
        }

        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            return;

        Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);

        SceneObjectiveController controller =
            Object.FindFirstObjectByType<SceneObjectiveController>(FindObjectsInactive.Include);
        VoiceCommand voiceCommand =
            Object.FindFirstObjectByType<VoiceCommand>(FindObjectsInactive.Include);
        ExitDoorTrigger door =
            Object.FindFirstObjectByType<ExitDoorTrigger>(FindObjectsInactive.Include);

        if (controller == null || door == null)
        {
            Debug.LogError("[SALINLAHI] " + ScenePath + " needs a SceneObjectiveController and " +
                           "an ExitDoorTrigger before it can be given a summary. (controller: " +
                           (controller != null) + ", door: " + (door != null) + ")");
            return;
        }

        // Where CONTINUE goes is where the door used to go.
        SceneResultPanelBuilder.Built built =
            SceneResultPanelBuilder.Build(controller, voiceCommand, door.nextSceneName);

        // The objectives finish well before the scene does, so the panel must
        // not open itself; the door says when.
        built.panel.waitForCue = true;

        door.resultPanel = built.panel;
        // The old one-second "level complete" flash has nothing to show any
        // more, and leaving it set would put an empty object on screen behind
        // the panel.
        door.levelCompleteUI = null;

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);

        Debug.Log("[SALINLAHI] Umaga na! now ends on the summary panel. The door opens it; " +
                  "CONTINUE goes to '" + door.nextSceneName + "'.");
    }
}
