// Assets/Editor/SpriteImportFixer.cs
//
// Makes sure every PNG in Assets/Sprites is imported AS A SPRITE.
//
// This project's textures do not default to Sprite - new PNGs arrive as
// textureType 0 (plain Texture) with spriteImportMode 0 (None). A plain
// texture is not a Sprite, so AssetDatabase.LoadAssetAtPath<Sprite>() returns
// null for it, and a UI Image handed a null sprite draws a plain WHITE BOX.
//
// That is how the level-summary screen came out as a row of white rectangles:
// nothing was wrong with the layout or the art, the builder simply could not
// load any of it. The warnings were there in the Console, but white boxes on
// screen look like a layout bug, which sends you looking in the wrong place.
//
// The art pipeline here writes PNGs from Python scripts in Tools/, outside
// Unity, so this will keep happening every time a new batch lands. The scene
// builders call Fix() before they load anything, and the menu item is there
// for fixing a folder by hand.
//
// Run via: Tools > SALINLAHI > Fix Sprite Import Settings

using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public static class SpriteImportFixer
{
    public const string SpriteFolder = "Assets/Sprites";

    [MenuItem("Tools/SALINLAHI/Fix Sprite Import Settings")]
    public static void FixMenu()
    {
        int fixedCount = Fix();
        Debug.Log(fixedCount == 0
            ? "[SALINLAHI] All PNGs in " + SpriteFolder + " are already imported as sprites."
            : "[SALINLAHI] Reimported " + fixedCount + " texture(s) in " + SpriteFolder +
              " as sprites. Anything that was drawing as a white box should now show its art.");
    }

    /// <summary>
    /// Converts any texture in the sprite folder that is not a Sprite, and
    /// returns how many needed it. Cheap to call: assets already set up are
    /// skipped without touching them, so a builder can call it every time.
    /// </summary>
    public static int Fix()
    {
        int changed = 0;
        foreach (string guid in AssetDatabase.FindAssets("t:Texture2D", new[] { SpriteFolder }))
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null) continue;

            bool wrongType = importer.textureType != TextureImporterType.Sprite;
            bool wrongMode = importer.spriteImportMode != SpriteImportMode.Single;
            if (!wrongType && !wrongMode) continue;

            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            // Keep the soft edges the cut-out scripts spend effort producing.
            importer.alphaIsTransparency = true;
            importer.mipmapEnabled = false;
            importer.SaveAndReimport();
            changed++;
        }
        return changed;
    }

    /// <summary>
    /// Loads sprites by name, fixing the folder first, and reports everything
    /// that is still missing in ONE error rather than a warning per sprite.
    /// A builder that cannot load its art should say so loudly - the failure
    /// mode otherwise is a screen of white rectangles.
    /// </summary>
    public static bool Verify(IEnumerable<string> spriteNames, string who)
    {
        Fix();

        List<string> missing = new List<string>();
        foreach (string name in spriteNames)
        {
            if (string.IsNullOrEmpty(name)) continue;
            string path = SpriteFolder + "/" + name + ".png";
            if (AssetDatabase.LoadAssetAtPath<Sprite>(path) == null) missing.Add(name);
        }

        if (missing.Count > 0)
        {
            Debug.LogError("[SALINLAHI] " + who + " cannot load " + missing.Count +
                            " sprite(s): " + string.Join(", ", missing.ToArray()) +
                            ". They will draw as white boxes. Check the files exist in " +
                            SpriteFolder + " and re-run.");
            return false;
        }
        return true;
    }
}

/// <summary>
/// Catches new art as it lands, so a PNG dropped into Assets/Sprites is a
/// sprite from its first import and never has to be fixed afterwards.
///
/// Only touches assets with no .meta yet (importSettingsMissing), so anything
/// already in the project keeps whatever settings it was given - including
/// sprites somebody has tuned by hand. The explicit fixer above is for the
/// files that came in before this existed.
/// </summary>
public class SpriteFolderPostprocessor : AssetPostprocessor
{
    private void OnPreprocessTexture()
    {
        if (!assetPath.StartsWith(SpriteImportFixer.SpriteFolder + "/")) return;

        TextureImporter importer = assetImporter as TextureImporter;
        if (importer == null || !importer.importSettingsMissing) return;

        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Single;
        importer.alphaIsTransparency = true;
        importer.mipmapEnabled = false;
    }
}
