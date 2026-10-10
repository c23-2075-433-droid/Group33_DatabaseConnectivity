// Assets/Editor/LevelCompleteUIBuilder.cs
//
// Builds the level-summary screen from the group's UI kit, cut into sprites by
// Tools/cut_summary_kit.py.
//
// The kit supersedes the finished mock-up this screen was first built from.
// That version had to undo a picture: painted labels blended off the board,
// stars prised off the wood they sat on, medallions masked by assuming they
// were circles. Here the board arrives empty and every piece is separate, so
// the layout below is an arrangement rather than a repair.
//
// The three statistics are SALINLAHI's own. The kit's labels belong to a
// different game, so its word art for them is left unused and the labels are
// set as text:
//
//   WORDS SAID        Filipino words said correctly, out of those asked
//   VOICE TRIES       attempts it took
//   WORDS PRACTISED   which words they were
//
// Buttons are assembled rather than composed: one wooden frame sprite with a
// glyph laid over it, so Replay and Main Menu share a frame and a glyph can be
// swapped without recutting art.
//
// LAYOUT is in board-relative units - the board is BoardW x BoardH and
// everything on it is placed from its centre. Nothing is measured off a
// source picture any more.
//
// Used by AddExitTrigger.cs and DatabaseDemoBuilder.cs.

using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public static class LevelCompleteUIBuilder
{
    private const string CanvasName = "LevelComplete_Canvas";

    // The board, and the canvas it sits on (1920x1080 reference).
    private const float BoardW = 980f, BoardH = 566f;
    private const float BoardY = 55f;

    // Left of the board: the badge plaque, with the star rating beneath it.
    private const float PlaqueX = -235f, PlaqueY = 66f;
    private const float PlaqueW = 294f, PlaqueH = 245f;
    private const float StarY = -140f, StarGap = 118f, StarSize = 92f;

    // Right of the board: three rows, each a symbol with its figure beside it.
    // The rows sit higher than an even split would put them, so the last one -
    // the list of words practised - has the whole lower third of the board to
    // wrap into. A full level can be nine words long.
    // The symbol sits well left of its figure: at 58 the "8 of 8" started
    // almost against the medallion's rim.
    private const float IconX = 20f, IconSize = 86f;
    private const float TextX = 300f, TextW = 320f;
    private const float RowWordsY = 160f, RowTriesY = 30f, RowLearnedY = -100f;

    private static readonly Color LabelBrown = new Color(0.33f, 0.16f, 0.03f);
    private static readonly Color ValueCream = new Color(0.99f, 0.91f, 0.74f);

    public static GameObject BuildPanel(string message) => BuildPanel(message, null);

    /// <summary>
    /// Builds the summary canvas, inactive. SessionScoreTracker turns it on
    /// and fills in the figures.
    ///
    /// Children are named so DatabaseDemoBuilder can find them:
    ///   Panel/Board/StatWords/WordsValue   (and Tries, Learned)
    ///   Panel/Board/StarRow/Star1..3
    ///   Panel/Board/Plaque/ScoreText
    ///   Panel/NextButton
    /// </summary>
    public static GameObject BuildPanel(string message, string badgeSpriteFile)
    {
        // Before anything else: this project does not import PNGs as sprites
        // by default, and an Image handed a null sprite draws a white box. The
        // whole screen came out as white rectangles once for exactly that
        // reason, so the art is made loadable and checked up front.
        SpriteImportFixer.Verify(new[]
        {
            "ui_summary_board", "ui_summary_plaque", "ui_summary_title",
            "ui_summary_icon_chest", "ui_summary_icon_clock", "ui_summary_icon_gem",
            "ui_summary_btn_frame", "ui_summary_glyph_replay", "ui_summary_glyph_home",
            "btn_summary_next", "ui_summary_word_replay", "ui_summary_word_home",
            "ui_summary_word_next", "ui_summary_star_on", "ui_summary_star_off",
            badgeSpriteFile,
        }, "Level-complete panel");

        // GameObject.Find() can't be used - this canvas is created INACTIVE,
        // and Find() only searches active objects.
        foreach (GameObject stale in FindAllInScene(CanvasName))
        {
            Debug.Log("[SALINLAHI] Replacing existing " + CanvasName + ".");
            Object.DestroyImmediate(stale);
        }

        GameObject canvasGO = new GameObject(CanvasName);
        Canvas canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        CanvasScaler scaler = canvasGO.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.Expand;
        canvasGO.AddComponent<GraphicRaycaster>();

        if (Object.FindFirstObjectByType<EventSystem>() == null)
        {
            GameObject es = new GameObject("EventSystem");
            es.AddComponent<EventSystem>();
            es.AddComponent<StandaloneInputModule>();
        }

        GameObject dim = new GameObject("Dim", typeof(RectTransform));
        dim.transform.SetParent(canvasGO.transform, false);
        Image dimImg = dim.AddComponent<Image>();
        dimImg.color = new Color(0, 0, 0, 0.55f);
        RectTransform dimRect = dim.GetComponent<RectTransform>();
        dimRect.anchorMin = Vector2.zero;
        dimRect.anchorMax = Vector2.one;
        dimRect.offsetMin = Vector2.zero;
        dimRect.offsetMax = Vector2.zero;

        // "Panel" is a container, not the board: the title and the buttons sit
        // outside the board but belong to the same screen. The name is kept
        // because the rest of the project looks the panel up by it.
        GameObject root = new GameObject("Panel", typeof(RectTransform));
        root.transform.SetParent(canvasGO.transform, false);
        root.GetComponent<RectTransform>().sizeDelta = new Vector2(1400, 1040);
        Transform rootT = root.transform;

        AddImage(rootT, "Title", "ui_summary_title", new Vector2(0f, 430f), new Vector2(760f, 98f));

        GameObject board = AddImage(rootT, "Board", "ui_summary_board",
            new Vector2(0f, BoardY), new Vector2(BoardW, BoardH));
        Transform boardT = board.transform;

        BuildPlaque(boardT, badgeSpriteFile);
        BuildStars(boardT);

        // The medallion icons, not the kit's loose glyphs. The kit draws its
        // symbols for a light background - the chest is dark brown with a
        // cream outline - so dropping one on a dark disc leaves dark on dark.
        // These came off the finished mock-up with the disc already around
        // them, which is what the reference actually shows.
        BuildStatRow(boardT, "Words", "ui_summary_icon_chest", "WORDS SAID", RowWordsY, 40);
        BuildStatRow(boardT, "Tries", "ui_summary_icon_clock", "VOICE TRIES", RowTriesY, 40);
        // A list rather than a figure: set smaller, and given the room below
        // it to run onto as many lines as the level's words need.
        BuildStatRow(boardT, "Learned", "ui_summary_icon_gem", "WORDS PRACTISED", RowLearnedY, 25);

        // No recent-scores list: the group asked for it off the screen.
        // SessionScoreTracker still saves the record and still reads the
        // recent ones back - its Text references simply go unassigned, which
        // SetText tolerates - so the Activity 5 round trip still happens and
        // still logs to the Console. Only the display is gone.

        BuildButtons(rootT);

        canvasGO.SetActive(false);
        return canvasGO;
    }

    /// <summary>
    /// The plaque, holding the level's badge with the player's name beneath.
    /// The kit's "7" is not used: a painted level number cannot serve a game
    /// with three levels, and the badge is what this screen actually awards.
    /// </summary>
    private static void BuildPlaque(Transform board, string badgeSpriteFile)
    {
        GameObject plaque = AddImage(board, "Plaque", "ui_summary_plaque",
            new Vector2(PlaqueX, PlaqueY), new Vector2(PlaqueW, PlaqueH));

        Sprite badge = string.IsNullOrEmpty(badgeSpriteFile) ? null : LoadSprite(badgeSpriteFile);
        if (badge != null)
        {
            GameObject badgeGO = AddImage(plaque.transform, "Badge", null,
                new Vector2(0f, 22f), new Vector2(136f, 136f));
            Image img = badgeGO.GetComponent<Image>();
            img.sprite = badge;
            img.preserveAspect = true;
        }

        // Clear of the plaque's inner moulding, which is about a fifth of its
        // height - higher up, the name loses its descenders to the frame.
        AddText(plaque.transform, "ScoreText", "",
            new Vector2(0f, -72f), new Vector2(250f, 44f),
            24, FontStyle.Bold, TextAnchor.MiddleCenter, LabelBrown);
    }

    /// <summary>Three stars, all starting unearned. SessionScoreTracker lights them.</summary>
    private static void BuildStars(Transform board)
    {
        GameObject row = new GameObject("StarRow", typeof(RectTransform));
        row.transform.SetParent(board, false);
        RectTransform rowRect = row.GetComponent<RectTransform>();
        rowRect.anchoredPosition = new Vector2(PlaqueX, StarY);
        rowRect.sizeDelta = new Vector2(StarGap * 3f, StarSize);

        for (int i = 0; i < 3; i++)
        {
            GameObject star = AddImage(row.transform, "Star" + (i + 1), "ui_summary_star_off",
                new Vector2((i - 1) * StarGap, 0f), new Vector2(StarSize, StarSize));
            star.GetComponent<Image>().preserveAspect = true;
        }
    }

    /// <summary>
    /// One statistic: the kit's symbol, with its label above the figure.
    /// The value Text is named "&lt;key&gt;Value" so DatabaseDemoBuilder can find it.
    /// </summary>
    private static void BuildStatRow(Transform board, string key, string iconSprite,
                                      string label, float y, int valueFontSize)
    {
        GameObject row = new GameObject("Stat" + key, typeof(RectTransform));
        row.transform.SetParent(board, false);
        RectTransform rowRect = row.GetComponent<RectTransform>();
        rowRect.anchoredPosition = new Vector2(0f, y);
        rowRect.sizeDelta = new Vector2(BoardW, 120f);

        GameObject icon = AddImage(row.transform, "Icon", iconSprite,
            new Vector2(IconX, 0f), new Vector2(IconSize, IconSize));
        icon.GetComponent<Image>().preserveAspect = true;

        AddText(row.transform, "Label", label,
            new Vector2(TextX, 30f), new Vector2(TextW, 32f),
            25, FontStyle.Bold, TextAnchor.MiddleLeft, LabelBrown);

        Text value = AddText(row.transform, key + "Value", "-",
            new Vector2(TextX, -24f), new Vector2(TextW, 48f),
            valueFontSize, FontStyle.Bold, TextAnchor.MiddleLeft, ValueCream);
        value.horizontalOverflow = HorizontalWrapMode.Wrap;
        value.lineSpacing = 0.92f;
    }

    /// <summary>
    /// Replay, Main Menu and Next Level. The first two are the kit's wooden
    /// frame with a glyph over it; Next Level is its own arrow with the kit's
    /// word art on top. Captions use the kit's word art rather than text, so
    /// they match the rest of the screen.
    /// </summary>
    private static void BuildButtons(Transform root)
    {
        string thisScene = SceneManager.GetActiveScene().name;
        // Moved up now that nothing sits between them and the board.
        const float ButtonY = -340f, CaptionY = -449f;

        GameObject replay = MakeButton(root, "ReplayButton", "ui_summary_btn_frame",
            new Vector2(-330f, ButtonY), new Vector2(190f, 159f), thisScene);
        AddImage(replay.transform, "Glyph", "ui_summary_glyph_replay",
            new Vector2(0f, 6f), new Vector2(84f, 80f)).GetComponent<Image>().preserveAspect = true;

        GameObject home = MakeButton(root, "HomeButton", "ui_summary_btn_frame",
            new Vector2(0f, ButtonY), new Vector2(190f, 159f), "MainMenu");
        AddImage(home.transform, "Glyph", "ui_summary_glyph_home",
            new Vector2(0f, 6f), new Vector2(88f, 78f)).GetComponent<Image>().preserveAspect = true;

        GameObject next = MakeButton(root, "NextButton", "btn_summary_next",
            new Vector2(340f, ButtonY), new Vector2(250f, 127f), "LevelSelect");
        // Sat left of centre: the arrowhead takes the right third of the sprite.
        AddImage(next.transform, "Caption", "ui_summary_word_next",
            new Vector2(-30f, 2f), new Vector2(132f, 29f)).GetComponent<Image>().preserveAspect = true;

        AddImage(root, "ReplayCaption", "ui_summary_word_replay",
            new Vector2(-330f, CaptionY), new Vector2(124f, 46f)).GetComponent<Image>().preserveAspect = true;
        AddImage(root, "HomeCaption", "ui_summary_word_home",
            new Vector2(0f, CaptionY), new Vector2(196f, 38f)).GetComponent<Image>().preserveAspect = true;
    }

    private static GameObject MakeButton(Transform parent, string name, string sprite,
                                          Vector2 position, Vector2 size, string sceneName)
    {
        GameObject go = AddImage(parent, name, sprite, position, size);
        Image img = go.GetComponent<Image>();
        img.raycastTarget = true;
        img.preserveAspect = true;

        Button button = go.AddComponent<Button>();
        button.targetGraphic = img;
        ColorBlock colors = button.colors;
        colors.highlightedColor = new Color(1f, 0.97f, 0.9f);
        colors.pressedColor = new Color(0.8f, 0.75f, 0.7f);
        colors.fadeDuration = 0.08f;
        button.colors = colors;

        go.AddComponent<LoadSceneOnClick>().sceneName = sceneName;
        return go;
    }

    private static GameObject AddImage(Transform parent, string name, string sprite,
                                        Vector2 position, Vector2 size)
    {
        GameObject go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        Image img = go.AddComponent<Image>();
        if (!string.IsNullOrEmpty(sprite)) img.sprite = LoadSprite(sprite);
        img.raycastTarget = false;
        RectTransform rect = go.GetComponent<RectTransform>();
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
        return go;
    }

    private static Text AddText(Transform parent, string name, string content,
        Vector2 position, Vector2 size, int fontSize, FontStyle style,
        TextAnchor alignment, Color color)
    {
        GameObject go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        Text text = go.AddComponent<Text>();
        text.text = content;
        text.font = UIFont.Get();
        text.fontSize = fontSize;
        text.fontStyle = style;
        text.alignment = alignment;
        text.color = color;
        text.supportRichText = false;
        text.raycastTarget = false;
        text.horizontalOverflow = HorizontalWrapMode.Overflow;
        text.verticalOverflow = VerticalWrapMode.Overflow;
        RectTransform rect = go.GetComponent<RectTransform>();
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
        return text;
    }

    private static Sprite LoadSprite(string spriteFile)
    {
        string path = "Assets/Sprites/" + spriteFile + ".png";
        Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
        if (sprite == null) Debug.LogWarning("[SALINLAHI] Could not load sprite at " + path);
        return sprite;
    }

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
