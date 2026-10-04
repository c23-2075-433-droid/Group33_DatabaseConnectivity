using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Takes Kylo's shoes off for the morning scenes at home.
///
/// He wakes up barefoot, crosses the bedroom, washes, and comes out in a
/// towel. The socks and shoes go on in Scene 4, one word at a time - that is
/// the whole point of that scene, and it falls flat if he is already wearing
/// them. Scene 2, 3 and 4 get this from their builders; Scene 1 has no builder,
/// so it is edited in place here.
///
/// This walks every component in the scene rather than looking for
/// PlayerMovement, because the walk frames are also held by PickUpItem and
/// PoseSwitch, and each one that kept its shoes would put them back on mid-step.
/// Only the five poses listed below are touched: a uniform or towel pose has no
/// barefoot twin and is left exactly as it is.
///
///     Tools > SALINLAHI > Use Barefoot Poses (Bahay)
/// </summary>
public class UseBarefootPoses
{
    private static readonly string[] Scenes =
    {
        "Assets/Scenes/Chapter1_Level1_UmagaNa.unity",
        "Assets/Scenes/Chapter1_Level2_Banyo.unity",
        "Assets/Scenes/Chapter1_Level3_Maligo.unity",
        "Assets/Scenes/Chapter1_Level4_Damit.unity",
    };

    private static readonly string[] Poses =
    {
        "lying_down", "sitting_up", "standing",
        "walk_frame_1", "walk_frame_2", "walk_frame_3", "walk_frame_4",
    };

    [MenuItem("Tools/SALINLAHI/Use Barefoot Poses (Bahay)")]
    public static void Run()
    {
        Dictionary<Sprite, Sprite> swap = BuildSwapTable();
        if (swap.Count == 0)
        {
            Debug.LogWarning("[SALINLAHI] No *_barefoot sprites found. "
                             + "Run python3 Tools/make_barefoot.py first.");
            return;
        }

        int total = 0;
        foreach (string path in Scenes)
        {
            Scene scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
            int changed = 0;
            foreach (GameObject root in scene.GetRootGameObjects())
                foreach (Component component in root.GetComponentsInChildren<Component>(true))
                    changed += Retarget(component, swap);

            if (changed > 0)
            {
                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene);
            }
            Debug.Log("[SALINLAHI] " + System.IO.Path.GetFileNameWithoutExtension(path)
                      + ": " + changed + " sprite reference(s) now barefoot.");
            total += changed;
        }

        AssetDatabase.SaveAssets();
        Debug.Log("[SALINLAHI] Barefoot poses applied: " + total + " reference(s) in "
                  + Scenes.Length + " scenes.");
    }

    private static Dictionary<Sprite, Sprite> BuildSwapTable()
    {
        var swap = new Dictionary<Sprite, Sprite>();
        foreach (string pose in Poses)
        {
            Sprite shod = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/" + pose + ".png");
            Sprite bare = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/" + pose + "_barefoot.png");
            if (shod != null && bare != null) swap[shod] = bare;
            else if (bare == null)
                Debug.Log("[SALINLAHI] No " + pose + "_barefoot yet - leaving " + pose + " alone.");
        }
        return swap;
    }

    private static int Retarget(Component component, Dictionary<Sprite, Sprite> swap)
    {
        if (component == null) return 0;

        var so = new SerializedObject(component);
        SerializedProperty p = so.GetIterator();
        int changed = 0;
        while (p.NextVisible(true))
        {
            if (p.propertyType != SerializedPropertyType.ObjectReference) continue;
            Sprite sprite = p.objectReferenceValue as Sprite;
            Sprite bare;
            if (sprite == null || !swap.TryGetValue(sprite, out bare)) continue;
            p.objectReferenceValue = bare;
            changed++;
        }
        if (changed > 0) so.ApplyModifiedPropertiesWithoutUndo();
        return changed;
    }
}
