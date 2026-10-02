// Assets/Editor/SettingsAboutDialogBuilder.cs
//
// Builds the Settings and About popups in the same bamboo style as the
// nickname dialog, replacing the plain grey Unity panels the menu shipped with.
//
// Art pieces (Assets/Sprites/):
//   ui_results_panel  - empty bamboo frame, used as the popup background
//   btn_okay          - "OKAY", used to close each popup
//   ui_slider_track   - empty bamboo bar
//   ui_slider_fill    - golden filled bar
//   ui_slider_knob    - round wooden handle
//
// Everything inside a panel is positioned as a fraction of the panel size, so
// changing PanelWidth rescales the whole popup. To nudge anything afterwards,
// move the RectTransforms in the Scene view - no code change needed.
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

    // Panel art is 1014x793 (~1.28:1); everything else is sized relative to it.
    private const float PanelWidth = 1000f;
    private const float PanelAspect = 1014f / 793f;

    // Cream, matching the lettering painted into btn_okay and the menu buttons.
    private static readonly Color CreamText = new Color(0.99f, 0.95f, 0.86f);

    public class Dialog
    {
        public GameObject root;
        public Button close;
        public Slider slider;   // settings only
    }

    /// <summary>Settings popup: a volume slider and an OKAY button.</summary>
    public static Dialog BuildSettings(Transform canvas)
    {
        float h = PanelWidth / PanelAspect;
        Dialog d = BuildShell(canvas, SettingsRootName, "Mga Setting");
        Transform panel = d.root.transform.Find("Panel");

        CreateText(panel, "VolumeLabel", "Lakas ng Tunog", new Vector2(0, h * 0.06f),
            new Vector2(PanelWidth * 0.7f, h * 0.1f), 42, FontStyle.Normal);

        d.slider = BuildSlider(panel, new Vector2(0, -h * 0.07f),
            new Vector2(PanelWidth * 0.62f, h * 0.11f));

        return d;
    }

    /// <summary>About popup: the capstone credits and an OKAY button.</summary>
    public static Dialog BuildAbout(Transform canvas)
    {
        float h = PanelWidth / PanelAspect;
        Dialog d = BuildShell(canvas, AboutRootName, "Tungkol Dito");
        Transform panel = d.root.transform.Find("Panel");

        CreateText(panel, "AboutBody",
            "Isang laro para matuto ng salitang Filipino sa pamamagitan ng boses.\n\n" +
            "BSIT Capstone Project\nUniversity of Perpetual Help System Laguna\n\n" +
            "Daquis, Jhesza Mhei G.\nLacida, Kylo Bryan\nManzanero, Kyla Samantha",
            new Vector2(0, -h * 0.02f), new Vector2(PanelWidth * 0.66f, h * 0.44f), 30, FontStyle.Normal);

        return d;
    }

    /// <summary>The parts both popups share: dimmed backdrop, bamboo panel, title, OKAY button.</summary>
    private static Dialog BuildShell(Transform canvas, string rootName, string title)
    {
        foreach (GameObject stale in FindAllInScene(rootName)) Object.DestroyImmediate(stale);

        float panelHeight = PanelWidth / PanelAspect;

        // Full-screen root, which also blocks clicks on the menu behind it.
        GameObject root = new GameObject(rootName, typeof(RectTransform));
        root.transform.SetParent(canvas, false);
        RectTransform rootRect = root.GetComponent<RectTransform>();
        rootRect.anchorMin = Vector2.zero;
        rootRect.anchorMax = Vector2.one;
        rootRect.offsetMin = Vector2.zero;
        rootRect.offsetMax = Vector2.zero;
        root.AddComponent<Image>().color = new Color(0f, 0f, 0f, 0.55f);

        GameObject panel = CreateImage(root.transform, "Panel", "ui_results_panel",
            Vector2.zero, new Vector2(PanelWidth, panelHeight));
        panel.GetComponent<Image>().raycastTarget = false;

        CreateText(panel.transform, "Title", title, new Vector2(0, panelHeight * 0.27f),
            new Vector2(PanelWidth * 0.7f, panelHeight * 0.14f), 58, FontStyle.Bold);

        // OKAY button, same proportions as the nickname dialog's (1733x407 art).
        GameObject closeGO = CreateImage(panel.transform, "CloseButton", "btn_okay",
            new Vector2(0, -panelHeight * 0.3f),
            new Vector2(PanelWidth * 0.38f, PanelWidth * 0.38f * 407f / 1733f));

        root.SetActive(false); // MainMenuController shows it when the button is pressed

        return new Dialog { root = root, close = MakeButton(closeGO) };
    }

    /// <summary>A working Unity Slider dressed in the bamboo track/fill/knob art.</summary>
    private static Slider BuildSlider(Transform parent, Vector2 anchoredPos, Vector2 size)
    {
        GameObject go = new GameObject("VolumeSlider", typeof(RectTransform));
        go.transform.SetParent(parent, false);
        RectTransform rect = go.GetComponent<RectTransform>();
        rect.anchoredPosition = anchoredPos;
        rect.sizeDelta = size;
        Slider slider = go.AddComponent<Slider>();

        // Track art, filling the whole slider.
        GameObject track = new GameObject("Background", typeof(RectTransform));
        track.transform.SetParent(go.transform, false);
        Image trackImg = track.AddComponent<Image>();
        trackImg.sprite = LoadSprite("ui_slider_track");
        Stretch(track.GetComponent<RectTransform>(), Vector2.zero, Vector2.zero);

        // Fill sits inside the track's hollow groove, so it doesn't spill over
        // the bamboo rim at either end.
        GameObject fillArea = new GameObject("Fill Area", typeof(RectTransform));
        fillArea.transform.SetParent(go.transform, false);
        Stretch(fillArea.GetComponent<RectTransform>(),
            new Vector2(size.x * 0.07f, size.y * 0.26f),
            new Vector2(-size.x * 0.07f, -size.y * 0.26f));

        GameObject fill = new GameObject("Fill", typeof(RectTransform));
        fill.transform.SetParent(fillArea.transform, false);
        Image fillImg = fill.AddComponent<Image>();
        fillImg.sprite = LoadSprite("ui_slider_fill");
        RectTransform fillRect = fill.GetComponent<RectTransform>();
        fillRect.anchorMin = Vector2.zero;
        fillRect.anchorMax = Vector2.one;
        fillRect.sizeDelta = Vector2.zero;

        GameObject handleArea = new GameObject("Handle Slide Area", typeof(RectTransform));
        handleArea.transform.SetParent(go.transform, false);
        Stretch(handleArea.GetComponent<RectTransform>(),
            new Vector2(size.y * 0.5f, 0f), new Vector2(-size.y * 0.5f, 0f));

        GameObject handle = new GameObject("Handle", typeof(RectTransform));
        handle.transform.SetParent(handleArea.transform, false);
        Image handleImg = handle.AddComponent<Image>();
        handleImg.sprite = LoadSprite("ui_slider_knob");
        RectTransform handleRect = handle.GetComponent<RectTransform>();
        handleRect.sizeDelta = new Vector2(size.y * 1.35f, size.y * 1.35f); // knob overhangs the bar a little

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
