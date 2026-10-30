using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Builds the HUD's objects and hands them back. Every rectangle in the
/// interface is a number in this file and nowhere else, so moving a panel is
/// a one-line edit rather than a hunt through the script that drives it.
///
/// This is the runtime twin of Assets/Editor/VoiceUIBuilder.cs, and it exists
/// at run time for the reason set out in Docs/VoicePromptingDesign.md 6.3: the
/// fifteen chapter scenes are GENERATED, so anything added to a saved scene is
/// destroyed the next time someone runs Build Bahay Scene 2. Nothing built
/// here is saved anywhere, so nothing here can be lost.
///
/// Coordinates are in the 1920x1080 reference space every canvas in this
/// project is scaled to, measured from the corner each piece is anchored to.
/// Inside a panel, positions are FRACTIONS of that panel's own art, so the
/// writing stays where it belongs on the wood if a panel is resized.
/// </summary>
public static class GameplayHUDLayout
{
    // ---- the pieces, as the HUD refers to them afterwards ----

    public class Parts
    {
        public RectTransform safeArea;

        public Button backButton;
        public Button menuButton;

        public GameObject narrationPanel;
        public CanvasGroup narrationFade;
        public Text narrationText;
        public Image rachelPortrait;
        public RachelPortraitAnimator rachelMouth;

        public GameObject wordSign;
        public CanvasGroup wordFade;
        public Text wordText;
        public Text glossText;

        public GameObject placePlank;
        public Text placeText;
        public GameObject statePlank;
        public Text stateText;

        public GameObject settingsPanel;
        public Button settingsClose;
        public Slider masterSlider;
        public Slider narrationSlider;
    }

    // ---- corners ----

    private const float Margin = 46f;          // from the safe-area edge
    private static readonly Vector2 BackSize = new Vector2(110f, 102f);   // btn_back is 487x454
    private static readonly Vector2 MenuSize = new Vector2(112f, 114f);   // btn_menu_round is square-ish

    // ---- Teacher Rachel's plaque, across the top ----

    private static readonly Vector2 NarrationSize = new Vector2(1100f, 277f);   // art is 1782x449
    private const float NarrationTop = -24f;
    private const float NarrationShiftX = 10f;

    // The writing starts clear of the branch ring and stops short of the
    // plaque's right-hand scroll. Fractions of the panel.
    private const float TextLeft = 0.25f;
    private const float TextRight = 0.95f;
    private const float TextTop = 0.22f;
    private const float TextBottom = 0.82f;

    // ---- the word signpost, down the right ----

    private static readonly Vector2 SignSize = new Vector2(360f, 459f);    // art is 674x859
    private const float SignCentreY = -230f;

    // Where the board is on the post, as fractions of the sprite.
    private const float BoardTop = 0.16f;
    private const float BoardBottom = 0.52f;
    private const float BoardInset = 0.09f;

    // ---- the two planks along the bottom ----

    private static readonly Vector2 PlaceSize = new Vector2(330f, 68f);    // art is 792x162
    private static readonly Vector2 StateSize = new Vector2(300f, 62f);
    private const float PlankBottom = 42f;

    // ---- the microphone the scene already has, moved to suit ----

    public static readonly Vector2 MicPosition = new Vector2(0f, -310f);
    public static readonly Vector2 MicSize = new Vector2(130f, 220f);      // art is 292x493
    public static readonly Vector2 ReplayPosition = new Vector2(175f, -310f);
    public static readonly Vector2 ReplaySize = new Vector2(110f, 110f);

    // ---- colour ----

    // Cream, the lettering painted into the project's own button art.
    public static readonly Color Cream = new Color(0.99f, 0.95f, 0.86f);
    private static readonly Color TextShadow = new Color(0.18f, 0.09f, 0.03f, 0.75f);

    // ------------------------------------------------------------------

    public static Parts Build(Transform canvas, UITheme theme, Font display, Font body)
    {
        Parts p = new Parts();

        // Everything hangs off one rect that the HUD insets to the safe area,
        // so a notch or a gesture bar moves the whole interface in rather than
        // each corner having to know about it.
        GameObject safe = Rect(canvas, "SafeArea", Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f));
        p.safeArea = safe.GetComponent<RectTransform>();
        Stretch(p.safeArea);

        BuildCorners(p, safe.transform, theme);
        BuildNarration(p, safe.transform, theme, body);
        BuildWordSign(p, safe.transform, theme, display, body);
        BuildPlanks(p, safe.transform, theme, display);

        return p;
    }

    private static void BuildCorners(Parts p, Transform parent, UITheme theme)
    {
        GameObject back = Piece(parent, "BackButton", theme != null ? theme.backButton : null,
            new Vector2(0f, 1f), new Vector2(Margin, -Margin), BackSize);
        p.backButton = Clickable(back);

        GameObject menu = Piece(parent, "MenuButton", theme != null ? theme.menuButton : null,
            new Vector2(1f, 1f), new Vector2(-Margin, -Margin), MenuSize);
        p.menuButton = Clickable(menu);
    }

    private static void BuildNarration(Parts p, Transform parent, UITheme theme, Font body)
    {
        // A bare container, not the plaque itself. Rachel has to be drawn
        // BEHIND the branch so the twigs cross in front of her, and a child
        // always draws over its parent's own Image - so the plaque cannot be
        // the parent. Portrait first, plaque over it, words over that.
        GameObject panel = Rect(parent, "NarrationPanel",
            new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f));
        RectTransform panelRect = panel.GetComponent<RectTransform>();
        panelRect.anchoredPosition = new Vector2(NarrationShiftX, NarrationTop);
        panelRect.sizeDelta = NarrationSize;

        p.narrationPanel = panel;
        p.narrationFade = panel.AddComponent<CanvasGroup>();
        p.narrationFade.alpha = 0f;
        p.narrationFade.blocksRaycasts = false;
        p.narrationFade.interactable = false;

        // Her circle is measured off the art itself by
        // Tools/cut_rachel_portrait.py - see UITheme's Ring constants.
        float ring = UITheme.RingDiameter * NarrationSize.x;
        GameObject face = Rect(panel.transform, "RachelPortrait",
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f));
        p.rachelPortrait = face.AddComponent<Image>();
        p.rachelPortrait.sprite = theme != null ? theme.rachelPortrait : null;
        p.rachelPortrait.preserveAspect = true;
        p.rachelPortrait.raycastTarget = false;
        // No portrait yet: leave the hole empty rather than draw a white disc.
        p.rachelPortrait.enabled = p.rachelPortrait.sprite != null;
        RectTransform faceRect = face.GetComponent<RectTransform>();
        faceRect.sizeDelta = new Vector2(ring, ring);
        faceRect.anchoredPosition = new Vector2(
            (UITheme.RingCentreX - 0.5f) * NarrationSize.x,
            (0.5f - UITheme.RingCentreY) * NarrationSize.y);

        // Her mouth. The frames are optional in the same way the portrait is -
        // with none loaded this is a still face, not a missing one. GameplayHUD
        // hands it the narrator and the panel's fade once it has both.
        p.rachelMouth = face.AddComponent<RachelPortraitAnimator>();
        p.rachelMouth.portrait = p.rachelPortrait;
        p.rachelMouth.idle   = theme != null ? theme.rachelPortrait : null;
        p.rachelMouth.talk1  = theme != null ? theme.rachelTalk1 : null;
        p.rachelMouth.talk2  = theme != null ? theme.rachelTalk2 : null;
        p.rachelMouth.talk3  = theme != null ? theme.rachelTalk3 : null;
        p.rachelMouth.blink  = theme != null ? theme.rachelBlink : null;
        p.rachelMouth.visibility = p.narrationFade;

        GameObject plaque = Rect(panel.transform, "Plaque",
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f));
        Image plaqueImage = plaque.AddComponent<Image>();
        plaqueImage.sprite = theme != null ? theme.narrationPanel : null;
        plaqueImage.preserveAspect = true;
        plaqueImage.raycastTarget = false;
        if (plaqueImage.sprite == null) plaqueImage.color = new Color(0.36f, 0.21f, 0.09f, 0.92f);
        plaque.GetComponent<RectTransform>().sizeDelta = NarrationSize;

        p.narrationText = Label(panel.transform, "NarrationText", "", body, 34,
            FontStyle.Bold, TextAnchor.MiddleLeft, Cream);
        FillFraction(p.narrationText.rectTransform, NarrationSize,
            TextLeft, TextRight, TextTop, TextBottom);
        p.narrationText.horizontalOverflow = HorizontalWrapMode.Wrap;
        p.narrationText.verticalOverflow = VerticalWrapMode.Truncate;
        // A two-word "Tama!" and a full scene intro both have to sit on the
        // same plank without it changing size, so the type fits itself.
        p.narrationText.resizeTextForBestFit = true;
        p.narrationText.resizeTextMinSize = 24;
        p.narrationText.resizeTextMaxSize = 40;
        Shade(p.narrationText);

        panel.SetActive(false);
    }

    private static void BuildWordSign(Parts p, Transform parent, UITheme theme, Font display, Font body)
    {
        GameObject sign = Piece(parent, "WordSign", theme != null ? theme.wordSignpost : null,
            new Vector2(1f, 0.5f), new Vector2(-40f, SignCentreY), SignSize);
        p.wordSign = sign;
        p.wordFade = sign.AddComponent<CanvasGroup>();
        p.wordFade.alpha = 1f;
        p.wordFade.blocksRaycasts = false;
        p.wordFade.interactable = false;

        // The three lines share the board at the top of the post; the post
        // itself is the lower half of the sprite and carries nothing.
        float boardH = (BoardBottom - BoardTop) * SignSize.y;
        float boardMidY = (0.5f - (BoardTop + BoardBottom) * 0.5f) * SignSize.y;
        float width = SignSize.x * (1f - 2f * BoardInset);

        Text heading = Label(sign.transform, "Heading", "WORD TO SAY:", display, 24,
            FontStyle.Bold, TextAnchor.MiddleCenter, Cream);
        Place(heading.rectTransform, new Vector2(0f, boardMidY + boardH * 0.33f),
            new Vector2(width, boardH * 0.24f));
        Shade(heading);

        p.wordText = Label(sign.transform, "Word", "", display, 54,
            FontStyle.Bold, TextAnchor.MiddleCenter, Cream);
        Place(p.wordText.rectTransform, new Vector2(0f, boardMidY + boardH * 0.02f),
            new Vector2(width, boardH * 0.44f));
        p.wordText.resizeTextForBestFit = true;
        p.wordText.resizeTextMinSize = 28;
        p.wordText.resizeTextMaxSize = 58;
        Shade(p.wordText);

        p.glossText = Label(sign.transform, "Gloss", "", body, 22,
            FontStyle.Normal, TextAnchor.MiddleCenter, Cream);
        Place(p.glossText.rectTransform, new Vector2(0f, boardMidY - boardH * 0.3f),
            new Vector2(width, boardH * 0.22f));
        p.glossText.resizeTextForBestFit = true;
        p.glossText.resizeTextMinSize = 14;
        p.glossText.resizeTextMaxSize = 24;
        Shade(p.glossText);

        sign.SetActive(false);
    }

    private static void BuildPlanks(Parts p, Transform parent, UITheme theme, Font display)
    {
        Sprite plank = theme != null ? theme.statusPlank : null;

        GameObject place = Piece(parent, "PlacePlank", plank,
            new Vector2(0f, 0f), new Vector2(Margin, PlankBottom), PlaceSize);
        p.placePlank = place;
        p.placeText = Label(place.transform, "PlaceText", "", display, 30,
            FontStyle.Bold, TextAnchor.MiddleCenter, Cream);
        Place(p.placeText.rectTransform, new Vector2(PlaceSize.x * 0.03f, 0f),
            new Vector2(PlaceSize.x * 0.82f, PlaceSize.y * 0.6f));
        p.placeText.resizeTextForBestFit = true;
        p.placeText.resizeTextMinSize = 16;
        p.placeText.resizeTextMaxSize = 32;
        Shade(p.placeText);

        GameObject state = Piece(parent, "StatePlank", plank,
            new Vector2(0.5f, 0f), new Vector2(0f, PlankBottom), StateSize);
        p.statePlank = state;
        p.stateText = Label(state.transform, "StateText", "", display, 30,
            FontStyle.Bold, TextAnchor.MiddleCenter, Cream);
        Place(p.stateText.rectTransform, new Vector2(StateSize.x * 0.03f, 0f),
            new Vector2(StateSize.x * 0.82f, StateSize.y * 0.6f));
        p.stateText.resizeTextForBestFit = true;
        p.stateText.resizeTextMinSize = 14;
        p.stateText.resizeTextMaxSize = 30;
        Shade(p.stateText);
    }

    /// <summary>
    /// The settings popup, built the first time it is asked for rather than
    /// with the rest of the HUD - most plays never open it, and it is the one
    /// piece here heavy enough for that to be worth it.
    ///
    /// Deliberately the same bamboo frame, slider and OKAY button the main
    /// menu's Settings popup uses (Assets/Editor/SettingsAboutDialogBuilder.cs),
    /// so the two read as one dialog in two places.
    /// </summary>
    public static void BuildSettings(Parts p, Transform parent, UITheme theme, Font display)
    {
        if (p.settingsPanel != null) return;

        const float W = 900f;
        float h = W / (1014f / 793f);   // ui_results_panel is 1014x793

        // Full-screen, and raycast-blocking, so a child adjusting a slider
        // cannot also press the microphone behind it.
        GameObject root = Rect(parent, "SettingsPanel", Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f));
        Stretch(root.GetComponent<RectTransform>());
        Image dim = root.AddComponent<Image>();
        dim.color = new Color(0f, 0f, 0f, 0.6f);
        dim.raycastTarget = true;
        p.settingsPanel = root;

        GameObject panel = Piece(root.transform, "Panel",
            theme != null ? theme.settingsPanel : null,
            new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(W, h));

        Text title = Label(panel.transform, "Title", "SETTINGS", display, 52,
            FontStyle.Bold, TextAnchor.MiddleCenter, Cream);
        Place(title.rectTransform, new Vector2(0f, h * 0.27f), new Vector2(W * 0.7f, h * 0.13f));
        Shade(title);

        p.masterSlider = SettingRow(panel.transform, theme, display,
            "Master", "SOUND", new Vector2(0f, h * 0.06f), W, h);
        p.narrationSlider = SettingRow(panel.transform, theme, display,
            "Narration", "TEACHER RACHEL", new Vector2(0f, -h * 0.12f), W, h);

        Text note = Label(panel.transform, "MicNote",
            "These change what you hear, not the microphone.", display, 22,
            FontStyle.Normal, TextAnchor.MiddleCenter, Cream);
        Place(note.rectTransform, new Vector2(0f, -h * 0.25f), new Vector2(W * 0.74f, h * 0.08f));
        note.resizeTextForBestFit = true;
        note.resizeTextMinSize = 14;
        note.resizeTextMaxSize = 22;
        Shade(note);

        GameObject close = Piece(panel.transform, "CloseButton",
            theme != null ? theme.okayButton : null,
            new Vector2(0.5f, 0.5f), new Vector2(0f, -h * 0.37f),
            new Vector2(W * 0.34f, W * 0.34f * 407f / 1733f));   // btn_okay is 1733x407
        p.settingsClose = Clickable(close);

        root.SetActive(false);
    }

    /// <summary>A labelled bamboo slider, the same build as the main menu's.</summary>
    private static Slider SettingRow(Transform panel, UITheme theme, Font display,
        string name, string caption, Vector2 position, float w, float h)
    {
        Text label = Label(panel, name + "Label", caption, display, 32,
            FontStyle.Bold, TextAnchor.MiddleCenter, Cream);
        Place(label.rectTransform, position + new Vector2(0f, h * 0.075f),
            new Vector2(w * 0.7f, h * 0.09f));
        label.resizeTextForBestFit = true;
        label.resizeTextMinSize = 18;
        label.resizeTextMaxSize = 32;
        Shade(label);

        Vector2 size = new Vector2(w * 0.62f, h * 0.11f);
        GameObject go = Rect(panel, name + "Slider", new Vector2(0.5f, 0.5f),
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f));
        RectTransform rect = go.GetComponent<RectTransform>();
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
        Slider slider = go.AddComponent<Slider>();

        GameObject track = Rect(go.transform, "Background", Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f));
        Image trackImage = track.AddComponent<Image>();
        trackImage.sprite = theme != null ? theme.sliderTrack : null;
        if (trackImage.sprite == null) trackImage.color = new Color(0.30f, 0.18f, 0.08f);
        Stretch(track.GetComponent<RectTransform>());

        // The fill sits inside the bamboo groove so it does not spill over the
        // rim at either end - the inset the main menu's slider uses.
        GameObject fillArea = Rect(go.transform, "Fill Area", Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f));
        RectTransform fillAreaRect = fillArea.GetComponent<RectTransform>();
        fillAreaRect.offsetMin = new Vector2(size.x * 0.07f, size.y * 0.26f);
        fillAreaRect.offsetMax = new Vector2(-size.x * 0.07f, -size.y * 0.26f);

        GameObject fill = Rect(fillArea.transform, "Fill", Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f));
        Image fillImage = fill.AddComponent<Image>();
        fillImage.sprite = theme != null ? theme.sliderFill : null;
        if (fillImage.sprite == null) fillImage.color = new Color(0.85f, 0.62f, 0.20f);
        RectTransform fillRect = fill.GetComponent<RectTransform>();
        fillRect.sizeDelta = Vector2.zero;

        GameObject handleArea = Rect(go.transform, "Handle Slide Area", Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f));
        RectTransform handleAreaRect = handleArea.GetComponent<RectTransform>();
        handleAreaRect.offsetMin = new Vector2(size.y * 0.5f, 0f);
        handleAreaRect.offsetMax = new Vector2(-size.y * 0.5f, 0f);

        GameObject handle = Rect(handleArea.transform, "Handle", new Vector2(0.5f, 0.5f),
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f));
        Image handleImage = handle.AddComponent<Image>();
        handleImage.sprite = theme != null ? theme.sliderKnob : null;
        if (handleImage.sprite == null) handleImage.color = new Color(0.62f, 0.40f, 0.18f);
        RectTransform handleRect = handle.GetComponent<RectTransform>();
        handleRect.sizeDelta = new Vector2(size.y * 1.35f, size.y * 1.35f);

        slider.fillRect = fillRect;
        slider.handleRect = handleRect;
        slider.targetGraphic = handleImage;
        slider.direction = Slider.Direction.LeftToRight;
        slider.minValue = 0f;
        slider.maxValue = 1f;
        return slider;
    }

    // ------------------------------------------------------------------
    // small helpers - the same shapes the Editor builders use, minus
    // AssetDatabase, which does not exist in a player
    // ------------------------------------------------------------------

    private static GameObject Rect(Transform parent, string name, Vector2 anchorMin,
        Vector2 anchorMax, Vector2 pivot)
    {
        GameObject go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        RectTransform rect = go.GetComponent<RectTransform>();
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.pivot = pivot;
        return go;
    }

    /// <summary>A sprite anchored to one corner or edge of the screen.</summary>
    private static GameObject Piece(Transform parent, string name, Sprite sprite,
        Vector2 anchor, Vector2 position, Vector2 size)
    {
        GameObject go = Rect(parent, name, anchor, anchor, anchor);
        Image image = go.AddComponent<Image>();
        image.sprite = sprite;
        image.preserveAspect = true;
        image.raycastTarget = false;
        // No art is better than a white box: a tinted plate still reads as a
        // panel, and the refresh tool has already said which sprite is missing.
        if (sprite == null) image.color = new Color(0.36f, 0.21f, 0.09f, 0.92f);

        RectTransform rect = go.GetComponent<RectTransform>();
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
        return go;
    }

    private static Button Clickable(GameObject go)
    {
        Image image = go.GetComponent<Image>();
        image.raycastTarget = true;
        Button button = go.AddComponent<Button>();
        button.targetGraphic = image;
        ColorBlock colors = button.colors;
        colors.highlightedColor = new Color(1f, 0.97f, 0.9f);
        colors.pressedColor = new Color(0.8f, 0.75f, 0.7f);
        colors.fadeDuration = 0.08f;
        button.colors = colors;
        return button;
    }

    private static Text Label(Transform parent, string name, string content, Font font,
        int size, FontStyle style, TextAnchor align, Color colour)
    {
        GameObject go = Rect(parent, name, new Vector2(0.5f, 0.5f),
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f));
        Text text = go.AddComponent<Text>();
        text.text = content;
        text.font = font;
        text.fontSize = size;
        text.fontStyle = style;
        text.alignment = align;
        text.color = colour;
        text.horizontalOverflow = HorizontalWrapMode.Wrap;
        text.verticalOverflow = VerticalWrapMode.Truncate;
        text.raycastTarget = false;
        return text;
    }

    /// <summary>Cream on mid-brown wood is readable; a shadow makes it readable on the light grain too.</summary>
    private static void Shade(Text text)
    {
        Shadow shadow = text.gameObject.AddComponent<Shadow>();
        shadow.effectColor = TextShadow;
        shadow.effectDistance = new Vector2(2f, -2f);
    }

    private static void Place(RectTransform rect, Vector2 position, Vector2 size)
    {
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
    }

    /// <summary>Positions a child by fractions of its parent's art, so it moves with a resize.</summary>
    private static void FillFraction(RectTransform rect, Vector2 panel,
        float left, float right, float top, float bottom)
    {
        float w = (right - left) * panel.x;
        float h = (bottom - top) * panel.y;
        rect.sizeDelta = new Vector2(w, h);
        rect.anchoredPosition = new Vector2(
            ((left + right) * 0.5f - 0.5f) * panel.x,
            (0.5f - (top + bottom) * 0.5f) * panel.y);
    }

    private static void Stretch(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }
}
