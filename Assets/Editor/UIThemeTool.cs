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
        theme.settingsPanel  = Need("ui_results_panel",   missing);

        theme.backButton = Need("btn_back",        missing);
        theme.menuButton = Need("btn_menu_round",  missing);
        theme.okayButton = Need("btn_okay",        missing);

        theme.sliderTrack = Need("ui_slider_track", missing);
        theme.sliderFill  = Need("ui_slider_fill",  missing);
        theme.sliderKnob  = Need("ui_slider_knob",  missing);

        theme.micIcon = Need("mic_icon_wood", missing);

        // Optional on purpose: the ring frames the room without her, so a
        // missing portrait is a plainer panel, not a broken one.
        theme.rachelPortrait = Load("char_rachel_portrait");

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
                  (missing.Count > 0
                      ? "\n  MISSING: " + string.Join(", ", missing) +
                        "\n  Those panels will draw as plain tinted rectangles. Run " +
                        "python3 Tools/cut_hud_assets.py ~/Downloads/Robo to cut them."
                      : "\n  All panel and button art found."));

        Selection.activeObject = theme;
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
