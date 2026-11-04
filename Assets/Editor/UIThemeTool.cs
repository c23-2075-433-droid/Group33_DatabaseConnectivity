// Assets/Editor/UIThemeTool.cs
//
// Builds Assets/Resources/UITheme.asset: the fonts and sprites the in-game
// HUD draws itself from.
//
// Run it after importing new HUD art:
//     Tools > SALINLAHI > Refresh UI Theme
//
// Why an asset at all. Every other piece of UI in this project is built by an
// Editor script, which can say AssetDatabase.LoadAssetAtPath and UIFont.Get()
// because it only ever runs inside Unity. The HUD is built at RUN TIME - that
// is what keeps it out of the fifteen scene builders and makes it survive a
// rebuild - and neither of those exists in an Android player. A ScriptableObject
// sitting in Resources is how a runtime script gets hold of an asset by name,
// and NarratorVoiceBank already does exactly this for the voice-over clips.
//
// The asset holds REFERENCES. Nothing is copied into Resources, so the sprites
// stay where they are and are not duplicated in the build.

using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

public static class UIThemeTool
{
    private const string AssetPath = "Assets/Resources/UITheme.asset";
    private const string SpriteFolder = "Assets/Sprites/";
    private const string FontFolder = "Assets/Fonts";

    [MenuItem("Tools/SALINLAHI/Refresh UI Theme")]
    public static void Refresh()
    {
        // New PNGs arrive from the Tools/ Python scripts as plain textures, and
        // a UI Image handed a null sprite draws a solid white box. Same guard
        // every scene builder runs before it loads anything.
        SpriteImportFixer.Fix();

        UITheme theme = AssetDatabase.LoadAssetAtPath<UITheme>(AssetPath);
        bool isNew = theme == null;
        if (isNew)
        {
            Directory.CreateDirectory(Application.dataPath + "/Resources");
            theme = ScriptableObject.CreateInstance<UITheme>();
            AssetDatabase.CreateAsset(theme, AssetPath);
        }

        List<string> missing = new List<string>();

        theme.narrationPanel = Need("ui_narration_panel", missing);
        theme.wordSignpost   = Need("ui_word_signpost",   missing);
        theme.statusPlank    = Need("ui_status_plank",    missing);
        // Repointed, not duplicated: this field's only reader is the settings
        // panel, so it follows the redesign. The ui_results_panel PNG itself is
        // untouched and still used by the summary screens.
        theme.settingsPanel  = Need("ui_settings_panel",  missing);

        theme.backButton    = Need("btn_back",            missing);
        theme.menuButton    = Need("btn_menu_round",      missing);
        theme.okayButton    = Need("btn_okay",            missing);
        theme.settingsClose = Need("btn_settings_close",  missing);

        theme.toggleOn  = Need("ui_settings_toggle_on",  missing);
        theme.toggleOff = Need("ui_settings_toggle_off", missing);

        // The settings redesign replaced these three. Nothing outside the two
        // settings screens ever used them, so they are repointed rather than
        // duplicated; ui_slider_track/fill/knob are now unreferenced.
        theme.sliderTrack = Need("ui_settings_slider_track", missing);
        theme.sliderFill  = Need("ui_settings_slider_fill",  missing);
        theme.sliderKnob  = Need("ui_settings_slider_knob",  missing);

        // The fill is the only 9-sliced sprite in the project. Its border has to
        // live in the importer, and sliced rendering needs a full-rect mesh.
        SliceBorder("ui_settings_slider_fill", new Vector4(14f, 0f, 14f, 0f));

        theme.micIcon = Need("mic_icon_wood", missing);

        // Optional on purpose: the ring frames the room without her, so a
        // missing portrait is a plainer panel, not a broken one.
        theme.rachelPortrait = Load("char_rachel_idle");

        // Three of the mouth shapes, smallest opening first, standing in as
        // the talking cycle until the recordings arrive and real lip-sync
        // picks the shape per sound. Load, not Need: the panel is supposed to
        // survive without them, as a still portrait.
        theme.rachelTalk1 = Load("char_rachel_e");
        theme.rachelTalk2 = Load("char_rachel_o");
        theme.rachelTalk3 = Load("char_rachel_ai");
        theme.rachelBlink = Load("char_rachel_blink");

        // The lip-sync set. Same files, now addressed by sound rather than by
        // how far open the mouth is.
        theme.rachelAI  = Load("char_rachel_ai");
        theme.rachelE   = Load("char_rachel_e");
        theme.rachelO   = Load("char_rachel_o");
        theme.rachelU   = Load("char_rachel_u");
        theme.rachelMBP = Load("char_rachel_mbp");
        theme.rachelFV  = Load("char_rachel_fv");
        theme.rachelL   = Load("char_rachel_l");

        theme.displayFont = FindFont("Bold");
        theme.bodyFont    = FindFont("Medium") ?? theme.displayFont;

        EditorUtility.SetDirty(theme);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        UITheme.Invalidate();

        Debug.Log("[SALINLAHI] UI theme " + (isNew ? "created" : "refreshed") + " at " + AssetPath +
                  "\n  display font: " + Name(theme.displayFont) +
                  ", body font: " + Name(theme.bodyFont) +
                  "\n  Teacher Rachel portrait: " +
                  (theme.rachelPortrait != null
                      ? theme.rachelPortrait.name
                      : "none yet - the ring will frame the room behind it") +
                  "\n  Her talking frames: " + Mouths(theme) +
                  (missing.Count > 0
                      ? "\n  MISSING: " + string.Join(", ", missing) +
                        "\n  Those panels will draw as plain tinted rectangles. Cut them with " +
                        "python3 Tools/cut_hud_assets.py ~/Downloads/Robo, or, for anything " +
                        "named ui_settings_* or btn_settings_*, python3 Tools/cut_settings_ui.py."
                      : "\n  All panel and button art found."));

        Selection.activeObject = theme;
    }

    /// <summary>
    /// How much of her mouth animation is actually here. Worth printing: a
    /// still Rachel is a legitimate state, so nothing else would ever say
    /// that a frame failed to import.
    /// </summary>
    private static string Mouths(UITheme theme)
    {
        int found = 0;
        if (theme.rachelTalk1 != null) found++;
        if (theme.rachelTalk2 != null) found++;
        if (theme.rachelTalk3 != null) found++;

        if (found == 0) return "none - she will not move while she speaks";
        return found + " of 3" +
               (theme.rachelBlink != null ? ", and the blink" : ", no blink");
    }

    /// <summary>
    /// Gives a sprite a 9-slice border, so its rounded ends survive being drawn
    /// at a width the artwork was not cut at. Unity needs a full-rect mesh for
    /// sliced rendering - the project's usual tight mesh silently refuses to
    /// slice - and re-importing is slow, so nothing is written when it already
    /// matches.
    /// </summary>
    private static void SliceBorder(string file, Vector4 border)
    {
        string path = SpriteFolder + file + ".png";
        TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
        if (importer == null) return;

        TextureImporterSettings settings = new TextureImporterSettings();
        importer.ReadTextureSettings(settings);

        if (settings.spriteBorder == border &&
            settings.spriteMeshType == SpriteMeshType.FullRect) return;

        settings.spriteBorder = border;
        settings.spriteMeshType = SpriteMeshType.FullRect;
        importer.SetTextureSettings(settings);
        importer.SaveAndReimport();
    }

    private static Sprite Need(string file, List<string> missing)
    {
        Sprite sprite = Load(file);
        if (sprite == null) missing.Add(file);
        return sprite;
    }

    private static Sprite Load(string file)
    {
        return AssetDatabase.LoadAssetAtPath<Sprite>(SpriteFolder + file + ".png");
    }

    /// <summary>
    /// A Fredoka weight by name. Goes through the folder rather than a fixed
    /// path so dropping a different family in Assets/Fonts still resolves -
    /// the same rule UIFont.Get() follows for the Editor-built UI, kept in
    /// step so the HUD and the rest of the game letter the same.
    ///
    /// Matched on "-Bold", not "Bold", because "Fredoka-SemiBold" contains
    /// "Bold" too and FindAssets returns no particular order - so a loose
    /// match picked whichever of the two Unity happened to hand back first,
    /// and the headings came out a different weight on different machines.
    /// </summary>
    private static Font FindFont(string weight)
    {
        if (!AssetDatabase.IsValidFolder(FontFolder)) return null;

        string suffix = "-" + weight + ".";
        Font fallback = null;
        foreach (string guid in AssetDatabase.FindAssets("t:Font", new[] { FontFolder }))
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);

            // Condensed and Expanded are the same weights drawn narrower or
            // wider; picking one by accident makes the HUD letter differently
            // from every other screen.
            if (path.Contains("Condensed") || path.Contains("Expanded")) continue;

            Font font = AssetDatabase.LoadAssetAtPath<Font>(path);
            if (font == null) continue;

            if (path.Contains(suffix)) return font;
            fallback ??= font;
        }
        return fallback;
    }

    private static string Name(Font font)
    {
        return font != null ? font.name : "none - falling back to Unity's built-in";
    }
}
