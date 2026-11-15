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
//   ui_about_panel           - the whole About board: its heading, its three
//                              sections, its credits and its corner X are all
//                              painted into the art
//
// Both popups are drawn boards that carry their own lettering, so neither has
// a title or captions of its own. All they share now is the dimmed backdrop -
// each builder adds its own board, because the two are sized differently.
// Settings is landscape and takes a fixed size. About is PORTRAIT, taller than
// the canvas is at 16:9, so a fixed size would hang off the top and bottom of
// the screen; it is fitted to the space instead, and the two things laid on it
// are anchored by fractions of the art rather than given a size in units, so
// they follow the board wherever it lands.
//
// Everything inside the settings panel is positioned as a fraction of the panel
// size, so changing PanelWidth rescales that popup.
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
    private const float SettingsAspect = 1224f / 946f;   // ui_settings_panel
    private const float AboutAspect = 794f / 941f;       // ui_about_panel

    // How much room the About board leaves itself on its tight axis - the top
    // and bottom of a landscape screen, the sides of a portrait one.
    private const float AboutScreenMargin = 32f;

    // Measured off ui_about_panel, as fractions of the art with the origin at
    // its top-left, the way an image editor reads it: the painted X in the
    // corner, and the strip of bare wood along the bottom rail, which is the
    // only band the artwork leaves clear.
    private const float CloseLeft = 0.846f, CloseRight = 0.979f;
    private const float CloseTop = 0.064f, CloseBottom = 0.171f;
    private const float VersionLeft = 0.30f, VersionRight = 0.70f;
    private const float VersionTop = 0.953f, VersionBottom = 0.987f;

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
        Dialog d = new Dialog { root = BuildBackdrop(canvas, SettingsRootName) };
        GameObject boardGO = CreateImage(d.root.transform, "Panel", "ui_settings_panel",
            Vector2.zero, board);
        boardGO.GetComponent<Image>().raycastTarget = false;
        Transform panel = boardGO.transform;

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

    /// <summary>
    /// About popup. The board is one piece of drawn art - the heading, the
    /// three sections and the credits are all painted into it - so the only
    /// things built here are a hit area over the X it already shows and the
    /// build version, which is the one line the art cannot carry.
    /// </summary>
    public static Dialog BuildAbout(Transform canvas)
    {
        Dialog d = new Dialog { root = BuildBackdrop(canvas, AboutRootName) };

        // Two nodes, one for the margin and one for the board: FitInParent
        // writes the board's own anchors and offsets on every layout pass, so
        // an inset put on the board directly would not survive the first one.
        GameObject area = new GameObject("Fit", typeof(RectTransform));
        area.transform.SetParent(d.root.transform, false);
        Stretch(area.GetComponent<RectTransform>(),
            new Vector2(AboutScreenMargin, AboutScreenMargin),
            new Vector2(-AboutScreenMargin, -AboutScreenMargin));

        GameObject board = CreateImage(area.transform, "Panel", "ui_about_panel",
            Vector2.zero, Vector2.zero);
        board.GetComponent<Image>().raycastTarget = false;
        AspectRatioFitter fitter = board.AddComponent<AspectRatioFitter>();
        fitter.aspectMode = AspectRatioFitter.AspectMode.FitInParent;
        fitter.aspectRatio = AboutAspect;

        // The build version, so a tester reading a bug report can name the
        // build it came from. VersionLabel fills in the number at run time,
        // which keeps it tracking Player Settings rather than whenever this
        // builder last ran. Best-fit rather than a point size: the board's
        // size follows the screen, and a fixed size would not follow with it.
        Text version = CreateText(board.transform, "Version", "v" + Application.version,
            Vector2.zero, Vector2.zero, 22, FontStyle.Bold);
        version.resizeTextForBestFit = true;
        version.resizeTextMinSize = 8;
        version.resizeTextMaxSize = 26;
        version.color = new Color(CreamText.r, CreamText.g, CreamText.b, 0.8f);
        AnchorFraction(version.rectTransform,
            VersionLeft, VersionRight, VersionTop, VersionBottom);
        version.gameObject.AddComponent<VersionLabel>();

        // The close button is transparent, because the X it closes on is part
        // of the board. A sprite laid over it could only be the same pixels
        // again, and tinting those on a press would flash a square patch of
        // the wood around them. Nothing is lost by leaving it bare: the press
        // still clicks, since UIClickSound watches for presses rather than
        // being wired to each button, and the panel shutting is the rest of
        // the answer.
        GameObject closeGO = new GameObject("CloseButton", typeof(RectTransform));
        closeGO.transform.SetParent(board.transform, false);
        Image closeImage = closeGO.AddComponent<Image>();
        closeImage.color = new Color(0f, 0f, 0f, 0f);
        closeImage.raycastTarget = true;
        AnchorFraction(closeGO.GetComponent<RectTransform>(),
            CloseLeft, CloseRight, CloseTop, CloseBottom);
        d.close = MakeButton(closeGO);

        return d;
    }

    /// <summary>
    /// What both popups share: the full-screen dimmed backdrop, which also
    /// swallows the presses meant for the menu behind it. The board goes on
    /// top of this, and each builder adds its own - they are no longer the
    /// same shape or sized the same way.
    /// </summary>
    private static GameObject BuildBackdrop(Transform canvas, string rootName)
    {
        foreach (GameObject stale in FindAllInScene(rootName)) Object.DestroyImmediate(stale);

        GameObject root = new GameObject(rootName, typeof(RectTransform));
        root.transform.SetParent(canvas, false);
        Stretch(root.GetComponent<RectTransform>(), Vector2.zero, Vector2.zero);
        // Matches the in-game panel's dim, so the two read as one dialog.
        root.AddComponent<Image>().color = new Color(0f, 0f, 0f, 0.6f);

        root.SetActive(false); // MainMenuController shows it when the button is pressed

        return root;
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

    /// <summary>
    /// Pins a child to a rectangle given as fractions of its PARENT, measured
    /// with the origin at the top-left. FillFraction's sibling, and the one to
    /// reach for when the parent resizes: it sets anchors rather than a size,
    /// so the child is re-laid out with the parent instead of staying the size
    /// the builder happened to compute.
    /// </summary>
    private static void AnchorFraction(RectTransform rect,
        float left, float right, float top, float bottom)
    {
        rect.anchorMin = new Vector2(left, 1f - bottom);
        rect.anchorMax = new Vector2(right, 1f - top);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
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
