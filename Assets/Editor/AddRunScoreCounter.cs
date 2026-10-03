// Assets/Editor/AddRunScoreCounter.cs
//
// Gives Level 1's bedroom scene a RunScoreCounter, so the words said there
// (Bangon, Tayo, Lakad) count towards the result the player is shown at the
// end of the level. Without it the bathroom scene saved only its own five
// words and the bedroom's three were quietly dropped.
//
// Scene 2 gets its counter from DatabaseDemoBuilder when it is built, so this
// only has to patch Scene 1. Level 1 has no builder of its own: the bedroom is
// hand-tuned and Chapter1_Level1_UmagaNa.unity is the only copy of it, so
// everything that scene needs is added by a patch like this one.
//
// Safe to run more than once: an existing counter is reconfigured rather than
// duplicated.
//
// Run via: Tools > SALINLAHI > Add Run Score Counter To Level 1

using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public class AddRunScoreCounter
{
    private const string ScenePath = "Assets/Scenes/Chapter1_Level1_UmagaNa.unity";

    [MenuItem("Tools/SALINLAHI/Add Run Score Counter To Level 1")]
    public static void AddCounter()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            Debug.LogError("[SALINLAHI] Stop Play mode first - this edits the scene, which Unity doesn't allow while the game is running.");
            return;
        }

        Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);

        VoiceCommand voiceCommand = Object.FindFirstObjectByType<VoiceCommand>(FindObjectsInactive.Include);
        if (voiceCommand == null)
        {
            Debug.LogError("[SALINLAHI] No VoiceCommand in " + ScenePath +
                            " - nothing to count. Open the scene and check the player object.");
            return;
        }

        RunScoreCounter counter = Object.FindFirstObjectByType<RunScoreCounter>(FindObjectsInactive.Include);
        bool isNew = counter == null;
        if (isNew)
        {
            GameObject go = new GameObject("RunScore");
            counter = go.AddComponent<RunScoreCounter>();
        }

        counter.voiceCommand = voiceCommand;
        // This is the level's first scene, so starting it clears whatever the
        // previous playthrough left behind.
        counter.resetOnStart = true;

        EditorUtility.SetDirty(counter);
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);

        Debug.Log("[SALINLAHI] " + (isNew ? "Added" : "Updated") +
                   " RunScoreCounter in Level 1. The bedroom's words now count towards " +
                   "the result shown at the end of the bathroom scene.");
    }
}
