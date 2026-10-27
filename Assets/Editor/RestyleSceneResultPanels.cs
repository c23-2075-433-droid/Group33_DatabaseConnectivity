// Assets/Editor/RestyleSceneResultPanels.cs
//
// Rebuilds the summary panel in every scene that already has one, and nothing
// else in those scenes.
//
// The panel is built into each gameplay scene by that scene's own builder, so
// a change to SceneResultPanelBuilder reaches the game only once every scene
// is rebuilt. Running fourteen scene builders would do it, but each of those
// rebuilds its whole scene from scratch and would throw away anything anyone
// has since moved by hand. This opens each scene, swaps the one canvas, and
// puts back the handful of references that pointed at it.
//
// What a scene needs in order to be rebuilt - where Continue goes, and
// whether it ends a level - is read off the panel already in it rather than
// from the scene builders, so this file does not have to repeat what they
// know and cannot disagree with them.
//
// Run via: Tools > SALINLAHI > Restyle Scene Result Panels

using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public static class RestyleSceneResultPanels
{
    [MenuItem("Tools/SALINLAHI/Restyle Scene Result Panels")]
    public static void Restyle()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            Debug.LogError("[SALINLAHI] Stop Play mode first - this reopens every scene, which Unity doesn't allow while the game is running.");
            return;
        }

        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            return;

        List<string> done = new List<string>();
        List<string> skipped = new List<string>();

        foreach (string guid in AssetDatabase.FindAssets("t:Scene", new[] { "Assets/Scenes" }))
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            Scene scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Single);

            SceneResultPanel old = Object.FindFirstObjectByType<SceneResultPanel>(FindObjectsInactive.Include);
            if (old == null)
            {
                skipped.Add(System.IO.Path.GetFileNameWithoutExtension(path));
                continue;
            }

            // Everything the rebuild needs, taken off the panel being replaced.
            SceneObjectiveController controller = old.objectiveController;
            VoiceCommand voiceCommand = old.voiceCommand;
            string nextSceneName = old.nextSceneName;
            string levelSelectSceneName = old.levelSelectSceneName;
            bool levelEnd = old.waitForExternalTotals;

            SceneResultPanelBuilder.Built rebuilt =
                SceneResultPanelBuilder.Build(controller, voiceCommand, nextSceneName, levelEnd);
            rebuilt.panel.levelSelectSceneName = levelSelectSceneName;

            // The two things outside the panel that hold on to a piece of it.
            // Both are wired by DatabaseDemoBuilder on a level's last scene and
            // would otherwise be left pointing at the canvas just destroyed.
            SessionScoreTracker tracker = Object.FindFirstObjectByType<SessionScoreTracker>(FindObjectsInactive.Include);
            if (tracker != null && levelEnd)
            {
                tracker.resultPanel = rebuilt.panel;
                tracker.recordsText = rebuilt.recordsText;
            }

            ExitDoorTrigger exit = Object.FindFirstObjectByType<ExitDoorTrigger>(FindObjectsInactive.Include);
            if (exit != null && levelEnd) exit.levelCompleteUI = rebuilt.content;

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            done.Add(System.IO.Path.GetFileNameWithoutExtension(path) + (levelEnd ? " (level end)" : ""));
        }

        Debug.Log("[SALINLAHI] Restyled the summary panel in " + done.Count + " scene(s): " +
                  string.Join(", ", done) +
                  (skipped.Count > 0
                      ? ". " + skipped.Count + " scene(s) have no summary panel and were left alone: " +
                        string.Join(", ", skipped) + "."
                      : "."));
    }
}
