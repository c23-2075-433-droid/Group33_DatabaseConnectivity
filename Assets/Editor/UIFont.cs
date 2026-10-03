// Assets/Editor/UIFont.cs
//
// Single source of truth for the font all generated UI text uses, so the
// on-screen text matches the chunky rounded lettering painted into the art
// (btn_okay, btn_cancel, "Enter Nickname:", the journey title) instead of
// falling back to Arial.
//
// Drop any .ttf/.otf into Assets/Fonts/ and every builder picks it up on the
// next run - no code change. If a font named PreferredFont is there it wins;
// otherwise the first font found is used. With the folder empty it falls back
// to Unity's built-in font and says so once, so nothing breaks.
//
// See Assets/Fonts/README.txt for where to get a matching font.

using UnityEditor;
using UnityEngine;

public static class UIFont
{
    private const string FontFolder = "Assets/Fonts";
    private const string PreferredFont = "Fredoka";

    private static Font cached;
    private static bool warned;

    /// <summary>The game's UI font, or Unity's built-in fallback if none is installed yet.</summary>
    public static Font Get()
    {
        if (cached != null) return cached;

        if (AssetDatabase.IsValidFolder(FontFolder))
        {
            string[] guids = AssetDatabase.FindAssets("t:Font", new[] { FontFolder });

            // Prefer the named font, otherwise take whatever is in the folder.
            string chosen = null;
            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                if (path.ToLowerInvariant().Contains(PreferredFont.ToLowerInvariant()))
                {
                    chosen = path;
                    break;
                }
                chosen ??= path;
            }

            if (chosen != null)
            {
                cached = AssetDatabase.LoadAssetAtPath<Font>(chosen);
                if (cached != null)
                {
                    Debug.Log("[SALINLAHI] UI font: " + chosen);
                    return cached;
                }
            }
        }

        if (!warned)
        {
            warned = true;
            Debug.LogWarning("[SALINLAHI] No font found in " + FontFolder + " - falling back to Arial, " +
                              "which won't match the lettering in the art. See Assets/Fonts/README.txt.");
        }
        return Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
    }
}
