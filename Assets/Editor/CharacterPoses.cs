using UnityEditor;
using UnityEngine;

/// <summary>
/// Picks the right version of Kylo's art for a scene.
///
/// He is barefoot all morning: he wakes up, crosses the bedroom, washes and
/// comes out in a towel without shoes on. The socks and shoes only go on in
/// Scene 4, one word at a time, which is the whole point of that scene - so
/// every scene before it asks for the barefoot art through here.
///
/// A missing barefoot pose falls back to the shod one rather than leaving the
/// character invisible. That matters while the art is still being drawn: a
/// scene built halfway through just keeps its shoes until the file arrives.
/// </summary>
public static class CharacterPoses
{
    private const string Folder = "Assets/Sprites/";

    /// <summary>The barefoot version of a pose, or the pose itself if there is none yet.</summary>
    public static Sprite Barefoot(string pose)
    {
        Sprite bare = AssetDatabase.LoadAssetAtPath<Sprite>(Folder + pose + "_barefoot.png");
        if (bare != null) return bare;

        Sprite shod = AssetDatabase.LoadAssetAtPath<Sprite>(Folder + pose + ".png");
        if (shod == null)
            Debug.LogWarning("[SALINLAHI] No sprite named '" + pose + "' in " + Folder + ".");
        else
            Debug.Log("[SALINLAHI] No " + pose + "_barefoot yet - using " + pose + " for now.");
        return shod;
    }

    /// <summary>The four walk frames, barefoot.</summary>
    public static Sprite[] BarefootWalk()
    {
        return new[]
        {
            Barefoot("walk_frame_1"), Barefoot("walk_frame_2"),
            Barefoot("walk_frame_3"), Barefoot("walk_frame_4"),
        };
    }
}
