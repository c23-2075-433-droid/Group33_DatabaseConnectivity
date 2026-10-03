// Assets/Editor/AddExitTrigger.cs
//
// Ensures the Level 1 scene has a plain, sprite-free exit trigger, correctly
// configured - creates one if it's missing, or just fixes up an existing
// one's fields if it's already there (so re-running this after a later code
// change, e.g. adding the completion panel, actually applies it instead of
// silently skipping). This is a plain invisible BoxCollider2D zone with
// ExitDoorTrigger attached (shown as an orange gizmo box in the Scene view -
// see ExitDoorTrigger.OnDrawGizmos - but nothing renders in Game view). Drop
// a SpriteRenderer with real door art onto it later; no code changes needed.
//
// Run this BEFORE "Migrate Level 1 To Objective System" if you haven't run
// that yet - it wires this trigger's objectiveController for you once the
// SceneObjectiveController exists.
//
// Run via: Tools > SALINLAHI > Add Exit Trigger To Level 1

using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public class AddExitTrigger
{
    private const string ScenePath = "Assets/Scenes/Chapter1_Level1_UmagaNa.unity";

    [MenuItem("Tools/SALINLAHI/Add Exit Trigger To Level 1")]
    public static void AddTrigger()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            Debug.LogError("[SALINLAHI] Stop Play mode first - this edits the scene, which Unity doesn't allow while the game is running.");
            return;
        }

        Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);

        ExitDoorTrigger trigger = Object.FindFirstObjectByType<ExitDoorTrigger>(FindObjectsInactive.Include);
        bool isNew = trigger == null;

        if (isNew)
        {
            GameObject triggerGO = new GameObject("ExitTrigger");
            // Same spot the old door prop used to sit, near the room's right wall.
            triggerGO.transform.position = new Vector3(3.8f, -0.35f, 0);

            BoxCollider2D col = triggerGO.AddComponent<BoxCollider2D>();
            col.isTrigger = true;
            col.size = new Vector2(1f, 2f);

            trigger = triggerGO.AddComponent<ExitDoorTrigger>();
        }

        // Applied whether the trigger is brand new or already existed, so a
        // later change to this configuration (like adding the completion
        // panel below) always takes effect on re-run.
        //
        // No further scenes exist yet (Bahay's bathroom/bath/dressing/
        // breakfast scenes aren't built) - leave nextSceneName empty so
        // LoadNextScene() just no-ops instead of trying to load something
        // that doesn't exist. Swap this out once Scene 2 exists.
        trigger.nextSceneName = "";
        trigger.levelCompleteUI = LevelCompleteUIBuilder.BuildPanel("Magaling!");

        SceneObjectiveController controller = Object.FindFirstObjectByType<SceneObjectiveController>(FindObjectsInactive.Include);
        if (controller != null) trigger.objectiveController = controller;

        EditorSceneManager.SaveScene(scene);

        Debug.Log("[SALINLAHI] " + (isNew ? "Added" : "Updated") + " the ExitTrigger in " + ScenePath +
                   " at " + trigger.transform.position + ". It's invisible in-game (orange gizmo box in the " +
                   "Scene view only). Reaching it shows a placeholder 'Scene Complete' panel and does not " +
                   "load another scene. " +
                   (controller != null
                       ? "Wired to the existing SceneObjectiveController."
                       : "No SceneObjectiveController found yet - run 'Migrate Level 1 To Objective System' next; it wires this trigger automatically."));
    }
}
