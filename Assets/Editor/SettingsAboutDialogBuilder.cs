// Assets/Editor/SettingsAboutDialogBuilder.cs
//
// Builds the Settings and About popups in the main menu scene.
//
// Art pieces (Assets/Sprites/):
//   ui_settings_panel        - the settings board; its four rows, their icons
//                              and their labels are painted into the art
//   btn_settings_close       - round wooden X, closes the settings popup
//   ui_settings_toggle_on    - green ON plate
//   ui_settings_toggle_off   - grey OFF plate
//   ui_settings_slider_track - grey capsule with its dark groove
//   ui_settings_slider_fill  - the orange bar, 9-sliced
//   ui_settings_slider_knob  - round dark handle
//   ui_results_panel         - empty bamboo frame, still the About background
//   btn_okay                 - "OKAY", still closes About
//
// The two popups no longer share a look. Settings is a drawn board with its own
// lettering baked in, so it has no title and no captions of its own; About is
// still the bamboo frame with text laid on it. What they do share is BuildShell,
// which is now only the backdrop and the panel image - the title and the close
// button moved out, because About still wants OKAY under a heading and Settings
// wants an X in its corner.
//
// Everything inside a panel is positioned as a fraction of the panel size, so
// changing PanelWidth rescales the whole popup.
//
// Used by AddSettingsAboutToMainMenu.cs (patches the built menu) and
// BuildMainMenuScene.cs (fresh rebuild).

using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public static class SettingsAboutDialogBuilder
{
    public const string SettingsRootName = "SettingsPanel";
    public const string AboutRootName = "AboutPanel";

    private const float PanelWidth = 1000f;
    private const float AboutAspect = 1014f / 793f;      // ui_results_panel
    private const float SettingsAspect = 1224f / 946f;   // ui_settings_panel

    // Cream, matching the lettering painted into btn_okay and the menu buttons.
    private static readonly Color CreamText = new Color(0.99f, 0.95f, 0.86f);

    // The slider's proportions and its tap-target height live in UITheme,
    // measured off the art, so this screen and the in-game one cannot drift.

    public class Dialog
    {
        public GameObject root;
        public Button close;
        public Button soundToggle;       // settings only
        public Button musicToggle;       // settings only
        public Slider effectsSlider;     // settings only
        public Slider narrationSlider;   // settings only
    }

    /// <summary>Settings popup: two toggles, two sliders and a close X, on the drawn board.</summary>
    public static Dialog BuildSettings(Transform canvas)
    {
        Vector2 board = new Vector2(PanelWidth, PanelWidth / SettingsAspect);
        Dialog d = BuildShell(canvas, SettingsRootName, "ui_settings_panel", board);
        Transform panel = d.root.transform.Find("Panel");

        // No title and no captions: the board carries all of its own lettering.
        d.soundToggle = BuildToggle(panel, "SoundToggle", board,
            0.6716f, 0.8529f, 0.2273f, 0.3288f);
        d.musicToggle = BuildToggle(panel, "MusicToggle", board,
            0.6724f, 0.8521f, 0.3816f, 0.4820f);
        d.effectsSlider = BuildSlider(panel, "EffectsSlider", board,
            0.5204f, 0.8644f, 0.5751f, 0.6237f);
        d.narrationSlider = BuildSlider(panel, "NarrationSlider", board,
            0.5212f, 0.8644f, 0.7347f, 0.7833f);

        GameObject closeGO = CreateImage(panel, "CloseButton", "btn_settings_close",
            Vector2.zero, Vector2.one);
        closeGO.GetComponent<Image>().preserveAspect = true;
        FillFraction(closeGO.GetComponent<RectTransform>(), board,
            0.8766f, 0.9657f, 0.0772f, 0.1935f);
        d.close = MakeButton(closeGO);

        return d;
    }

    /// <summary>About popup: the build's version, the capstone credits and an OKAY button.</summary>
    public static Dialog BuildAbout(Transform canvas)
    {
        Vector2 board = new Vector2(PanelWidth, PanelWidth / AboutAspect);
        float h = board.y;
        Dialog d = BuildShell(canvas, AboutRootName, "ui_results_panel", board);
        Transform panel = d.root.transform.Find("Panel");

        CreateText(panel, "Title", "About", new Vector2(0, h * 0.27f),
            new Vector2(PanelWidth * 0.7f, h * 0.14f), 58, FontStyle.Bold);

        // The version sits in the gap between the title and the credits, which
        // is the only clear band on this board: the credits run to about
        // -0.2h and OKAY starts at -0.3h, so there is nothing below them to
        // use. A tester reading a bug report can now name the build it came
        // from. The number itself is filled in at run time by VersionLabel, so
        // it tracks Player Settings rather than whenever this builder last ran.
        Text version = CreateText(panel, "Version", "v" + Application.version,
            new Vector2(0, h * 0.198f), new Vector2(PanelWidth * 0.5f, h * 0.06f),
            26, FontStyle.Bold);
        version.gameObject.AddComponent<VersionLabel>();

        CreateText(panel, "AboutBody",
            "A game for learning Filipino words by speaking them out loud.\n\n" +
            "BSIT Capstone Project\nUniversity of Perpetual Help System Laguna\n\n" +
            "Daquis, Jhesza Mhei G.\nLacida, Kylo Bryan\nManzanero, Kyla Samantha",
            new Vector2(0, -h * 0.02f), new Vector2(PanelWidth * 0.66f, h * 0.44f), 30, FontStyle.Normal);

        // OKAY button, same proportions as the nickname dialog's (1733x407 art).
        GameObject closeGO = CreateImage(panel, "CloseButton", "btn_okay",
            new Vector2(0, -h * 0.3f),
            new Vector2(PanelWidth * 0.38f, PanelWidth * 0.38f * 407f / 1733f));
        d.close = MakeButton(closeGO);

        return d;
    }

    /// <summary>What both popups share: the dimmed backdrop and the panel image.</summary>
    private static Dialog BuildShell(Transform canvas, string rootName, string panelSprite, Vector2 board)
    {
        foreach (GameObject stale in FindAllInScene(rootName)) Object.DestroyImmediate(stale);

        // Full-screen root, which also blocks clicks on the menu behind it.
        GameObject root = new GameObject(rootName, typeof(RectTransform));
        root.transform.SetParent(canvas, false);
        RectTransform rootRect = root.GetComponent<RectTransform>();
        rootRect.anchorMin = Vector2.zero;
        rootRect.anchorMax = Vector2.one;
        rootRect.offsetMin = Vector2.zero;
        rootRect.offsetMax = Vector2.zero;
        // Matches the in-game panel's dim, so the two read as one dialog.
        root.AddComponent<Image>().color = new Color(0f, 0f, 0f, 0.6f);

        GameObject panel = CreateImage(root.transform, "Panel", panelSprite, Vector2.zero, board);
        panel.GetComponent<Image>().raycastTarget = false;

        root.SetActive(false); // MainMenuController shows it when the button is pressed

        return new Dialog { root = root };
    }

    /// <summary>
    /// A two-state control. A Button with a swapped sprite rather than a
    /// UnityEngine.UI.Toggle: the behaviour is the same for two states, it
    /// reuses the button factory both screens already share, and it needs no
    /// new component type in the saved scene. The plate shown here is only a
    /// starting look - MainMenuController pushes the real one from the saved
    /// setting every time the popup opens.
    /// </summary>
    private static Button BuildToggle(Transform panel, string name, Vector2 board,
        float left, float right, float top, float bottom)
    {
        GameObject go = CreateImage(panel, name, "ui_settings_toggle_off", Vector2.zero, Vector2.one);
        go.GetComponent<Image>().preserveAspect = true;
        FillFraction(go.GetComponent<RectTransform>(), board, left, right, top, bottom);
        return MakeButton(go);
    }

    /// <summary>A working Unity Slider dressed in the settings-board art.</summary>
    private static Slider BuildSlider(Transform parent, string name, Vector2 board,
        float left, float right, float top, float bottom)
    {
        GameObject go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        RectTransform rect = go.GetComponent<RectTransform>();
        FillFraction(rect, board, left, right, top, bottom);
        Vector2 size = rect.sizeDelta;
        Slider slider = go.AddComponent<Slider>();

        // First child, so it sits behind the art. Transparent but raycastable:
        // a press anywhere in the row reaches the Slider through it.
        GameObject pad = new GameObject("TouchPad", typeof(RectTransform));
        pad.transform.SetParent(go.transform, false);
        Image padImage = pad.AddComponent<Image>();
        padImage.color = new Color(0f, 0f, 0f, 0f);
        padImage.raycastTarget = true;
        float grow = Mathf.Max(0f, (UITheme.BarMinTouchHeight - size.y) * 0.5f);
        Stretch(pad.GetComponent<RectTransform>(), new Vector2(0f, -grow), new Vector2(0f, grow));

        // The capsule is inset top and bottom, because the box is knob-tall.
        GameObject track = new GameObject("Background", typeof(RectTransform));
        track.transform.SetParent(go.transform, false);
        Image trackImg = track.AddComponent<Image>();
        trackImg.sprite = LoadSprite("ui_settings_slider_track");
        float capsuleInset = size.y * (1f - UITheme.BarCapsuleFraction) * 0.5f;
        Stretch(track.GetComponent<RectTransform>(),
            new Vector2(0f, capsuleInset), new Vector2(0f, -capsuleInset));

        // The orange rides in the groove, so it never spills over the rim.
        GameObject fillArea = new GameObject("Fill Area", typeof(RectTransform));
        fillArea.transform.SetParent(go.transform, false);
        float sideInset = size.x * UITheme.BarGrooveInset;
        float orangeInset = size.y * (1f - UITheme.BarOrangeFraction) * 0.5f;
        Stretch(fillArea.GetComponent<RectTransform>(),
            new Vector2(sideInset, orangeInset), new Vector2(-sideInset, -orangeInset));

        GameObject fill = new GameObject("Fill", typeof(RectTransform));
        fill.transform.SetParent(fillArea.transform, false);
        Image fillImg = fill.AddComponent<Image>();
        fillImg.sprite = LoadSprite("ui_settings_slider_fill");
        RectTransform fillRect = fill.GetComponent<RectTransform>();
        fillRect.anchorMin = Vector2.zero;
        fillRect.anchorMax = Vector2.one;
        fillRect.sizeDelta = Vector2.zero;

        // The one 9-sliced image in the project. Stretched instead, its rounded
        // end collapses into a sliver near zero, and the quiet end of a volume
        // slider is exactly where a child leaves it. Unity measures the border
        // in the sprite's OWN pixels, so without the multiplier a 14 px cap
        // lands on a bar a few units tall and swallows the whole bar.
        float orangeHeight = size.y * UITheme.BarOrangeFraction;
        if (fillImg.sprite != null && orangeHeight > 0.01f)
        {
            fillImg.type = Image.Type.Sliced;
            fillImg.pixelsPerUnitMultiplier = UITheme.BarFillSourceHeight / orangeHeight;
        }

        GameObject handleArea = new GameObject("Handle Slide Area", typeof(RectTransform));
        handleArea.transform.SetParent(go.transform, false);
        float halfKnob = size.y * UITheme.BarKnobFraction * 0.5f;
        Stretch(handleArea.GetComponent<RectTransform>(),
            new Vector2(halfKnob, 0f), new Vector2(-halfKnob, 0f));

        GameObject handle = new GameObject("Handle", typeof(RectTransform));
        handle.transform.SetParent(handleArea.transform, false);
        Image handleImg = handle.AddComponent<Image>();
        handleImg.sprite = LoadSprite("ui_settings_slider_knob");
        RectTransform handleRect = handle.GetComponent<RectTransform>();
        handleRect.sizeDelta = new Vector2(size.y * UITheme.BarKnobFraction, size.y * UITheme.BarKnobFraction);

        slider.fillRect = fillRect;
        slider.handleRect = handleRect;
        slider.targetGraphic = handleImg;
        slider.direction = Slider.Direction.LeftToRight;
        slider.minValue = 0f;
        slider.maxValue = 1f;
        slider.value = 1f;
        return slider;
    }

    // ---------- helpers (same shapes as NicknameDialogBuilder) ----------

    /// <summary>
    /// Positions a child by fractions of the panel art it sits on, so it tracks
    /// any resize. The Editor twin of GameplayHUDLayout.FillFraction - the two
    /// settings screens are laid out from the same measured numbers.
    /// </summary>
    private static void FillFraction(RectTransform rect, Vector2 panel,
        float left, float right, float top, float bottom)
    {
        float w = (right - left) * panel.x;
        float h = (bottom - top) * panel.y;
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = new Vector2(w, h);
        rect.anchoredPosition = new Vector2(
            ((left + right) * 0.5f - 0.5f) * panel.x,
            (0.5f - (top + bottom) * 0.5f) * panel.y);
    }

    private static void Stretch(RectTransform rect, Vector2 offsetMin, Vector2 offsetMax)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = offsetMin;
        rect.offsetMax = offsetMax;
    }

    private static Text CreateText(Transform parent, string name, string content,
        Vector2 anchoredPos, Vector2 size, int fontSize, FontStyle style)
    {
        GameObject go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        Text text = go.AddComponent<Text>();
        text.text = content;
        text.font = UIFont.Get();
        text.fontSize = fontSize;
        text.fontStyle = style;
        text.alignment = TextAnchor.MiddleCenter;
        text.color = CreamText;
        text.horizontalOverflow = HorizontalWrapMode.Wrap;
        text.verticalOverflow = VerticalWrapMode.Overflow;
        text.raycastTarget = false;
        RectTransform rect = go.GetComponent<RectTransform>();
        rect.anchoredPosition = anchoredPos;
        rect.sizeDelta = size;
        return text;
    }

    private static Button MakeButton(GameObject go)
    {
        Button button = go.AddComponent<Button>();
        button.targetGraphic = go.GetComponent<Image>();
        ColorBlock colors = button.colors;
        colors.normalColor = Color.white;
        colors.highlightedColor = new Color(1f, 0.97f, 0.9f);
        colors.pressedColor = new Color(0.8f, 0.75f, 0.7f);
        // On a touch screen nothing clears the selection, so Unity's dimmed
        // default leaves the last thing tapped looking half-pressed.
        colors.selectedColor = Color.white;
        colors.fadeDuration = 0.08f;
        button.colors = colors;
        return button;
    }

    private static GameObject CreateImage(Transform parent, string name, string spriteFile,
        Vector2 anchoredPos, Vector2 size)
    {
        GameObject go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        Image img = go.AddComponent<Image>();
        img.sprite = LoadSprite(spriteFile);
        RectTransform rect = go.GetComponent<RectTransform>();
        rect.anchoredPosition = anchoredPos;
        rect.sizeDelta = size;
        return go;
    }

    private static Sprite LoadSprite(string spriteFile)
    {
        string path = "Assets/Sprites/" + spriteFile + ".png";
        Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
        if (sprite == null)
            Debug.LogWarning("[SALINLAHI] Could not load sprite at '" + path + "'. Make sure it's imported as Sprite (2D and UI).");
        return sprite;
    }

    /// <summary>Finds GameObjects by name in the open scene, including inactive ones.</summary>
    private static List<GameObject> FindAllInScene(string name)
    {
        List<GameObject> matches = new List<GameObject>();
        foreach (GameObject root in SceneManager.GetActiveScene().GetRootGameObjects())
            Collect(root.transform, name, matches);
        return matches;
    }

    private static void Collect(Transform t, string name, List<GameObject> matches)
    {
        if (t.name == name) matches.Add(t.gameObject);
        for (int i = 0; i < t.childCount; i++) Collect(t.GetChild(i), name, matches);
    }
}
