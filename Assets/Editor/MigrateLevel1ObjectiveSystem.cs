// Assets/Editor/MigrateLevel1ObjectiveSystem.cs
//
// ONE-TIME migration for the ALREADY-BUILT Level 1 scene: swaps its old
// Level1PromptSequencer (now deleted - the scene will show it as a "Missing
// Script" until this runs) for the new, data-driven SceneObjectiveController,
// reusing the exact same Prompt_Bangon/Prompt_Tayo/Prompt_Lakad GameObjects
// that are already in the scene. Nothing else (props, positions, art) is
// touched.
//
// Also re-wires ExitDoorTrigger's objectiveController reference, and - if
// you've already run "Add Voice UI To Level 1" - VoiceInteractionUI's
// objectiveController reference, which both went null when the old script
// was deleted.
//
// Safe to run only once per scene in its "broken" (missing-script) state;
// running it again after that is a no-op (it finds a real
// SceneObjectiveController already there and stops).
//
// Run via: Tools > SALINLAHI > Migrate Level 1 To Objective System

using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public class MigrateLevel1ObjectiveSystem
{
    private const string ScenePath = "Assets/Scenes/Chapter1_Level1_UmagaNa.unity";
    private const string CanvasName = "UI_WordPrompts_Canvas";

    [MenuItem("Tools/SALINLAHI/Migrate Level 1 To Objective System")]
    public static void Migrate()
    {
        Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        Debug.Log("[SALINLAHI] Opened scene '" + scene.name + "' at " + scene.path +
                   " (isLoaded=" + scene.isLoaded + ", rootCount=" + scene.rootCount + ")");

        if (Object.FindFirstObjectByType<SceneObjectiveController>(FindObjectsInactive.Include) != null)
        {
            Debug.Log("[SALINLAHI] " + ScenePath + " already has a SceneObjectiveController - nothing to migrate.");
            return;
        }

        // GameObject.Find() only searches ACTIVE objects - use a manual
        // (active-or-inactive) search of the scene's hierarchy instead, in
        // case the canvas or an ancestor is disabled.
        GameObject canvasGO = FindInScene(scene, CanvasName);
        PlayerMovement playerMovement = Object.FindFirstObjectByType<PlayerMovement>(FindObjectsInactive.Include);
        VoiceCommand voiceCommand = Object.FindFirstObjectByType<VoiceCommand>(FindObjectsInactive.Include);
        ExitDoorTrigger exitTrigger = Object.FindFirstObjectByType<ExitDoorTrigger>(FindObjectsInactive.Include);

        // Log each one individually so a failure says exactly what's missing,
        // instead of a single all-or-nothing error.
        Debug.Log("[SALINLAHI] Lookup results - " + CanvasName + ": " + (canvasGO != null) +
                   ", PlayerMovement: " + (playerMovement != null) +
                   ", VoiceCommand: " + (voiceCommand != null) +
                   ", ExitDoorTrigger: " + (exitTrigger != null));

        if (canvasGO == null || playerMovement == null || voiceCommand == null || exitTrigger == null)
        {
            Debug.LogError("[SALINLAHI] One or more required objects are missing from " + ScenePath +
                            " - see the lookup results logged just above for exactly which one(s).");
            return;
        }

        Transform promptBangon = canvasGO.transform.Find("Prompt_Bangon");
        Transform promptTayo = canvasGO.transform.Find("Prompt_Tayo");
        Transform promptLakad = canvasGO.transform.Find("Prompt_Lakad");

        if (promptBangon == null || promptTayo == null || promptLakad == null)
        {
            Debug.LogError("[SALINLAHI] Could not find Prompt_Bangon/Prompt_Tayo/Prompt_Lakad under " +
                            CanvasName + " - is this the right scene?");
            return;
        }

        // Remove the old Level1PromptSequencer's now-broken "Missing Script"
        // placeholder before adding the new component.
        int removed = GameObjectUtility.RemoveMonoBehavioursWithMissingScript(canvasGO);
        Debug.Log("[SALINLAHI] Removed " + removed + " missing-script component(s) from " + CanvasName + ".");

        SceneObjectiveController.SceneObjective bangonObjective = new SceneObjectiveController.SceneObjective
        {
            word = "Bangon", promptRoot = promptBangon.gameObject, onCorrect = new UnityEngine.Events.UnityEvent()
        };
        UnityEventTools.AddPersistentListener(bangonObjective.onCorrect, playerMovement.Bangon);

        SceneObjectiveController.SceneObjective tayoObjective = new SceneObjectiveController.SceneObjective
        {
            word = "Tayo", promptRoot = promptTayo.gameObject, onCorrect = new UnityEngine.Events.UnityEvent()
        };
        UnityEventTools.AddPersistentListener(tayoObjective.onCorrect, playerMovement.TayoUp);

        SceneObjectiveController.SceneObjective lakadObjective = new SceneObjectiveController.SceneObjective
        {
            word = "Lakad", promptRoot = promptLakad.gameObject, onCorrect = new UnityEngine.Events.UnityEvent()
        };
        UnityEventTools.AddPersistentListener(lakadObjective.onCorrect, playerMovement.WalkForward);

        SceneObjectiveController controller = canvasGO.AddComponent<SceneObjectiveController>();
        controller.voiceCommand = voiceCommand;
        controller.objectives = new[] { bangonObjective, tayoObjective, lakadObjective };

        exitTrigger.objectiveController = controller;

        // If the mic/replay buttons were already added, their reference to
        // the old (now-destroyed) sequencer went null - point it at the new
        // controller instead.
        VoiceInteractionUI voiceUI = Object.FindFirstObjectByType<VoiceInteractionUI>(FindObjectsInactive.Include);
        if (voiceUI != null)
        {
            voiceUI.objectiveController = controller;
            Debug.Log("[SALINLAHI] Re-wired VoiceInteractionUI's objectiveController to the new SceneObjectiveController.");
        }

        EditorSceneManager.SaveScene(scene);

        Debug.Log("[SALINLAHI] Migrated " + ScenePath + " to SceneObjectiveController. " +
                   "Bangon/Tayo/Lakad now work exactly as before, driven by data instead of hardcoded " +
                   "VoiceCommand fields. Nothing else in the scene was touched.");
    }

    /// <summary>Depth-first search of the scene's hierarchy by name, including inactive GameObjects (unlike GameObject.Find).</summary>
    private static GameObject FindInScene(Scene scene, string name)
    {
        foreach (GameObject root in scene.GetRootGameObjects())
        {
            GameObject match = FindInChildren(root.transform, name);
            if (match != null) return match;
        }
        return null;
    }

    private static GameObject FindInChildren(Transform t, string name)
    {
        if (t.name == name) return t.gameObject;
        for (int i = 0; i < t.childCount; i++)
        {
            GameObject match = FindInChildren(t.GetChild(i), name);
            if (match != null) return match;
        }
        return null;
    }
}
