// Assets/Editor/RebuildLaterScenes.cs
//
// Runs the scene builders for Almusal, the five Paaralan scenes and the five
// Parke scenes, in journey order.
//
// Those eleven saved scenes had fallen behind their builders. The builders
// have asked for a summary panel on every scene since 67c96a6, and Almusal's
// goes through DatabaseDemoBuilder, but none of the scenes had been
// regenerated since - so in the game they still faded straight into the next
// scene while the code said otherwise.
//
// Not a general "rebuild everything": Bahay 2 to 4 already carry the current
// panel, and Chapter 1 Level 1 has no builder at all - its stale one was
// deleted in 3469503 - so it is maintained by hand and nothing here can
// regenerate it.
//
// This rebuilds whole scenes, not just their panels. Anything moved by hand
// in them since they were last generated is lost; RestyleSceneResultPanels is
// the gentler tool when only the summary needs swapping.
//
// Run via: Tools > SALINLAHI > Rebuild Paaralan And Parke Scenes

using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public static class RebuildLaterScenes
{
    [MenuItem("Tools/SALINLAHI/Rebuild Paaralan And Parke Scenes")]
    public static void RebuildAll()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            Debug.LogError("[SALINLAHI] Stop Play mode first - these rebuild scenes, which Unity doesn't allow while the game is running.");
            return;
        }

        // In journey order, so the console reads the way the game plays.
        Action[] builders =
        {
            BuildBahayScene5.BuildScene,
            BuildPaaralanScene1.BuildScene,
            BuildPaaralanScene2.BuildScene,
            BuildPaaralanScene3.BuildScene,
            BuildPaaralanScene4.BuildScene,
            BuildPaaralanScene5.BuildScene,
            BuildParkeScene1.BuildScene,
            BuildParkeScene2.BuildScene,
            BuildParkeScene3.BuildScene,
            BuildParkeScene4.BuildScene,
            BuildParkeScene5.BuildScene,
        };

        List<string> failed = new List<string>();
        foreach (Action build in builders)
        {
            string name = build.Method.DeclaringType != null
                ? build.Method.DeclaringType.Name : "a builder";
            try
            {
                build();
            }
            catch (Exception e)
            {
                // One scene failing should not leave the other ten unbuilt,
                // and which one it was matters more than the stack.
                failed.Add(name);
                Debug.LogError("[SALINLAHI] " + name + " threw while rebuilding: " + e.Message);
            }
        }

        Debug.Log("[SALINLAHI] Rebuilt " + (builders.Length - failed.Count) + " of " +
                  builders.Length + " scenes." +
                  (failed.Count > 0 ? " Failed: " + string.Join(", ", failed) + "." : ""));
    }
}
