// Assets/Editor/AddVoiceUIToLevel1.cs
//
// Adds the microphone + replay/speaker buttons to the ALREADY-BUILT Level 1
// scene WITHOUT regenerating it - unlike BuildLevel1Scene.cs (which recreates
// the whole scene from scratch via NewScene), this opens
// Chapter1_Level1_UmagaNa.unity exactly as it is on disk, adds the new
// VoiceUI_Canvas, and saves. Anything you've manually tweaked since the scene
// was last built (prop positions, prompt text, etc.) is left untouched.
//
// Requires the scene to already have a SceneObjectiveController (Level 1's
// wake-up scene got one via "Tools > SALINLAHI > Migrate Level 1 To Objective
// System" - see MigrateLevel1ObjectiveSystem.cs). Safe to run more than once:
// if VoiceUI_Canvas already exists it's reused, not duplicated (see
// VoiceUIBuilder.cs).
//
// Run via: Tools > SALINLAHI > Add Voice UI To Level 1

using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public class AddVoiceUIToLevel1
{
    private const string ScenePath = "Assets/Scenes/Chapter1_Level1_UmagaNa.unity";

    [MenuItem("Tools/SALINLAHI/Add Voice UI To Level 1")]
    public static void AddVoiceUI()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            Debug.LogError("[SALINLAHI] Stop Play mode first - this edits the scene, which Unity doesn't allow while the game is running.");
            return;
        }

        Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);

        VoiceCommand voiceCommand = Object.FindFirstObjectByType<VoiceCommand>();
        SceneObjectiveController controller = Object.FindFirstObjectByType<SceneObjectiveController>();

        if (voiceCommand == null || controller == null)
        {
            Debug.LogError("[SALINLAHI] Could not find VoiceCommand / SceneObjectiveController in " + ScenePath +
                            " - run Tools > SALINLAHI > Migrate Level 1 To Objective System first.");
            return;
        }

        VoiceUIBuilder.BuildVoiceUI(voiceCommand, controller);

        EditorSceneManager.SaveScene(scene);

        Debug.Log("[SALINLAHI] Voice UI (mic + replay buttons) added to " + ScenePath + ". " +
                   "Nothing else in the scene was touched. Assign each objective's Instruction Clip " +
                   "on the SceneObjectiveController component in the Inspector when you have the audio.");
    }
}
