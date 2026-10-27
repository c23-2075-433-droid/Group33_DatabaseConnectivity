// Assets/Editor/SceneResultPanelBuilder.cs
//
// Builds the summary panel a scene ends on, and wires SceneResultPanel to it.
//
// Scenes used to fade straight into the next one the moment the last word was
// said. This puts a panel in between: how the scene went, then Replay and
// Continue, with a cross in the corner back to the map.
//
// The LEVEL COMPLETE sheet has landed, so the stand-ins are gone. The board
// prints SCORE: and TIME: itself, which is why this builder places only the
// figures that follow them, and why the scene is timed at all - the label
// came with the art. The banner is a sprite rather than text, and which of
// the two it shows is SceneResultPanel's call once it knows the rating.
//
// Positions were measured off the composed mockup, kept beside the pieces as
// Art/result_layout_reference.png, and scaled by 0.96 to the 1920x1080 canvas.
//
// Used by the scene builders in place of their old
//   onAllObjectivesComplete -> SceneFader.FadeOutAndLoad(NextScene)
// wiring. The fade still happens; the Next button is what starts it.

using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public static class SceneResultPanelBuilder
{
    private const string CanvasName = "SceneResult_Canvas";

    // ---- SPRITES, cut by Tools/import_result_panel_art.py ----
    private const string PanelSprite = "ui_result_board";
    private const string TitleCompleteSprite = "ui_result_title_complete";
    private const string TitleTryAgainSprite = "ui_result_title_tryagain";
    private const string NextSprite = "btn_result_continue";
    private const string ReplaySprite = "btn_result_replay";
    private const string CloseSprite = "btn_result_close";

    // ui_result_board is 712x1061; keeping the ratio stops the frame
    // stretching. SCORE: and TIME: are printed into it, so this builder places
    // only the figures that follow them.
    private const float PanelWidth = 680f;
    private const float PanelHeight = 1015f;

    // Everything below was measured off the composed mockup, kept beside the
    // pieces as Art/result_layout_reference.png (2000x1125), and scaled by
    // 0.96 to the 1920x1080 canvas.
    private static readonly Vector2 TitlePos = new Vector2(0f, 244f);
    private static readonly Vector2 TitleSize = new Vector2(680f, 235f);
    private static readonly Vector2 ClosePos = new Vector2(252f, 378f);
    private const float CloseSize = 106f;

    // Where the figures sit: left-aligned in a column beside the two printed
    // labels, whose right edges the mockup puts at -16 and -55.
    private const float ValueLeft = 10f;
    private const float ValueWidth = 290f;
    private const float ScoreY = -77f;
    private const float TimeY = -160f;

    private const float ButtonWidth = 246f;   // height follows the plank's own ratio
    private const float ButtonX = 130f;
    private const float ButtonRowY = -290f;

    // A level's last scene also shows the saved scores, which needs the room
    // between TIME: and the buttons - so the buttons drop towards the board's
    // empty lower panel and the strip takes the space they leave.
    private const float ButtonRowYLevelEnd = -370f;
    private static readonly Vector2 RecordsPos = new Vector2(0f, -250f);
    private static readonly Vector2 RecordsSize = new Vector2(560f, 150f);

    /// <summary>
    /// Where each star sits and how big it is. The row is drawn rather than
    /// tiled - it swells towards the middle and the outer pairs sit lower,
    /// following the arc in the art - so no slot can be worked out from the
    /// one beside it.
    /// </summary>
    private struct StarPlace
    {
        public float x, y, w, h;
    }

    private static readonly StarPlace[] StarPlaces =
    {
        new StarPlace { x = -215f, y = 36f, w = 70f,  h = 72f  },
        new StarPlace { x = -128f, y = 55f, w = 106f, h = 108f },
        new StarPlace { x = 0f,    y = 82f, w = 158f, h = 163f },
        new StarPlace { x = 127f,  y = 55f, w = 105f, h = 108f },
        new StarPlace { x = 214f,  y = 36f, w = 70f,  h = 72f  },
    };

    private static readonly Color Cream = new Color(0.99f, 0.96f, 0.89f);

    /// <summary>What a caller needs to wire the panel up afterwards.</summary>
    public class Built
    {
        /// <summary>The canvas. Stays active; Content is what is shown and hidden.</summary>
        public GameObject canvas;

        /// <summary>The part that is switched on when the scene is finished.</summary>
        public GameObject content;

        public SceneResultPanel panel;

        /// <summary>The records strip, or null on a scene that does not end a level.</summary>
        public Text recordsText;
    }

    /// <summary>
    /// Adds the panel to the open scene. nextSceneName empty means this scene
    /// ends its level, and Next returns to the level select.
    ///
    /// levelEnd adds the strip of saved scores read back from the database,
    /// and leaves the panel waiting to be handed the whole level's totals
    /// rather than showing this one scene's.
    /// </summary>
    public static Built Build(SceneObjectiveController controller,
                              VoiceCommand voiceCommand,
                              string nextSceneName,
                              bool levelEnd = false)
    {
        // An Image handed a null sprite draws a white box, with nothing but a
        // console warning to say why. Make the art loadable first.
        SpriteImportFixer.Verify(new[]
        {
            PanelSprite, TitleCompleteSprite, TitleTryAgainSprite,
            NextSprite, ReplaySprite, CloseSprite,
            "star_gold_1", "star_gold_2", "star_gold_3", "star_gold_4", "star_gold_5",
            "star_grey_1", "star_grey_2", "star_grey_3", "star_grey_4", "star_grey_5",
        }, "Scene Result Panel");

        // GameObject.Find skips inactive objects and this canvas is created
        // inactive, so a stale one would never be found and the scene would
        // quietly collect a second panel on every rebuild.
        foreach (GameObject stale in FindAllInScene(CanvasName))
            Object.DestroyImmediate(stale);

        GameObject canvasGO = new GameObject(CanvasName);
        Canvas canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        // Above the voice UI, which sits on the default 0.
        canvas.sortingOrder = 50;
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

        Transform canvasT = canvasGO.transform;

        // Everything that gets hidden lives under here rather than on the
        // canvas itself. SceneResultPanel sits on the canvas and has to keep
        // running for the whole scene to hear it finish; if it switched off
        // the object it is on, it would lose its listener at that moment and
        // could never switch anything back on.
        GameObject content = new GameObject("Content", typeof(RectTransform));
        content.transform.SetParent(canvasT, false);
        Stretch(content.GetComponent<RectTransform>());
        Transform contentT = content.transform;

        GameObject dim = new GameObject("Dim", typeof(RectTransform));
        dim.transform.SetParent(contentT, false);
        Image dimImage = dim.AddComponent<Image>();
        dimImage.color = new Color(0f, 0f, 0f, 0.55f);
        // Raycast target left on: it swallows taps on the scene behind, so the
        // child cannot talk to props through the panel.
        Stretch(dim.GetComponent<RectTransform>());

        GameObject panel = new GameObject("Panel", typeof(RectTransform));
        panel.transform.SetParent(contentT, false);
        Image panelImage = panel.AddComponent<Image>();
        panelImage.sprite = LoadSprite(PanelSprite);
        panelImage.raycastTarget = false;
        RectTransform panelRect = panel.GetComponent<RectTransform>();
        panelRect.anchoredPosition = Vector2.zero;
        panelRect.sizeDelta = new Vector2(PanelWidth, PanelHeight);

        // The banner. Which of the two it shows is SceneResultPanel's call, at
        // the moment it knows the rating; this only puts it there.
        GameObject titleGO = new GameObject("TitleBanner", typeof(RectTransform));
        titleGO.transform.SetParent(panel.transform, false);
        Image titleImage = titleGO.AddComponent<Image>();
        titleImage.sprite = LoadSprite(TitleCompleteSprite);
        titleImage.preserveAspect = true;
        titleImage.raycastTarget = false;
        RectTransform titleRect = titleGO.GetComponent<RectTransform>();
        titleRect.anchoredPosition = TitlePos;
        titleRect.sizeDelta = TitleSize;

        SceneResultPanel.StarSlot[] stars = CreateStars(panel.transform);

        // Left-aligned beside the labels printed on the board, so the two
        // figures line up as a column however long they are.
        Text scoreText = CreateText(panel.transform, "ScoreText", "",
            new Vector2(ValueLeft + ValueWidth * 0.5f, ScoreY),
            new Vector2(ValueWidth, 72f), 46, TextAnchor.MiddleLeft);

        Text timeText = CreateText(panel.transform, "TimeText", "",
            new Vector2(ValueLeft + ValueWidth * 0.5f, TimeY),
            new Vector2(ValueWidth, 72f), 46, TextAnchor.MiddleLeft);

        // Only on a level's last scene: the scores read back out of the
        // database, which is the half of the Activity 5 requirement that has
        // to be visible on screen rather than merely fetched.
        Text recordsText = null;
        if (levelEnd)
        {
            recordsText = CreateText(panel.transform, "RecordsText", "",
                RecordsPos, RecordsSize, 22, TextAnchor.UpperCenter);
            recordsText.lineSpacing = 1.15f;
        }

        float row = levelEnd ? ButtonRowYLevelEnd : ButtonRowY;

        Button replay = CreateButton(panel.transform, "ReplayButton", ReplaySprite,
                                     new Vector2(-ButtonX, row), ButtonWidth);
        Button next = CreateButton(panel.transform, "NextButton", NextSprite,
                                   new Vector2(ButtonX, row), ButtonWidth);
        Button close = CreateButton(panel.transform, "CloseButton", CloseSprite,
                                    ClosePos, CloseSize);

        SceneResultPanel result = canvasGO.AddComponent<SceneResultPanel>();
        result.objectiveController = controller;
        result.voiceCommand = voiceCommand;
        result.panelRoot = content;
        result.titleImage = titleImage;
        result.completeSprite = LoadSprite(TitleCompleteSprite);
        result.tryAgainSprite = LoadSprite(TitleTryAgainSprite);
        result.scoreText = scoreText;
        result.timeText = timeText;
        result.stars = stars;
        result.nextButton = next;
        result.replayButton = replay;
        result.closeButton = close;
        result.nextSceneName = nextSceneName;
        result.waitForExternalTotals = levelEnd;

        // Hidden here rather than left for Awake, so the panel is already off
        // in the saved scene and never flashes up on the first frame.
        content.SetActive(false);

        return new Built
        {
            canvas = canvasGO,
            content = content,
            panel = result,
            recordsText = recordsText,
        };
    }

    private static SceneResultPanel.StarSlot[] CreateStars(Transform parent)
    {
        List<SceneResultPanel.StarSlot> stars = new List<SceneResultPanel.StarSlot>();

        for (int i = 0; i < StarPlaces.Length; i++)
        {
            StarPlace place = StarPlaces[i];
            Sprite earned = LoadSprite("star_gold_" + (i + 1));
            Sprite empty = LoadSprite("star_grey_" + (i + 1));

            GameObject go = new GameObject("Star_" + (i + 1), typeof(RectTransform));
            go.transform.SetParent(parent, false);
            Image image = go.AddComponent<Image>();
            // Starts unlit. The panel lights as many as were earned when it
            // opens, so a scene that is replayed badly cannot leave a star on.
            image.sprite = empty;
            image.preserveAspect = true;
            image.raycastTarget = false;

            RectTransform rect = go.GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(place.w, place.h);
            rect.anchoredPosition = new Vector2(place.x, place.y);

            stars.Add(new SceneResultPanel.StarSlot
            {
                image = image,
                earned = earned,
                empty = empty,
            });
        }
        return stars.ToArray();
    }

    private static Text CreateText(Transform parent, string name, string content,
        Vector2 pos, Vector2 size, int fontSize, TextAnchor align)
    {
        GameObject go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        Text text = go.AddComponent<Text>();
        text.text = content;
        text.font = UIFont.Get();
        text.fontSize = fontSize;
        text.fontStyle = FontStyle.Bold;
        text.alignment = align;
        text.color = Cream;
        text.horizontalOverflow = HorizontalWrapMode.Wrap;
        text.verticalOverflow = VerticalWrapMode.Overflow;
        text.raycastTarget = false;

        // Cream on mid-brown woodgrain is weak on its own - the grain runs
        // straight through the letters.
        Outline outline = go.AddComponent<Outline>();
        outline.effectColor = new Color(0.16f, 0.09f, 0.04f, 0.95f);
        outline.effectDistance = new Vector2(2.2f, -2.2f);

        RectTransform rect = go.GetComponent<RectTransform>();
        rect.anchoredPosition = pos;
        rect.sizeDelta = size;
        return text;
    }

    private static Button CreateButton(Transform parent, string name, string spriteFile,
                                       Vector2 pos, float width)
    {
        GameObject go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        Image image = go.AddComponent<Image>();
        Sprite sprite = LoadSprite(spriteFile);
        image.sprite = sprite;
        image.preserveAspect = true;

        float height = width;
        if (sprite != null && sprite.rect.width > 0f)
            height = width * (sprite.rect.height / sprite.rect.width);

        RectTransform rect = go.GetComponent<RectTransform>();
        rect.sizeDelta = new Vector2(width, height);
        rect.anchoredPosition = pos;

        Button button = go.AddComponent<Button>();
        button.targetGraphic = image;
        ColorBlock colors = button.colors;
        colors.highlightedColor = new Color(1f, 0.97f, 0.9f);
        colors.pressedColor = new Color(0.8f, 0.75f, 0.7f);
        colors.fadeDuration = 0.08f;
        button.colors = colors;
        return button;
    }

    private static void Stretch(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }

    private static Sprite LoadSprite(string spriteFile)
    {
        if (string.IsNullOrEmpty(spriteFile)) return null;
        string path = "Assets/Sprites/" + spriteFile + ".png";
        Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
        if (sprite == null)
            Debug.LogWarning("[SALINLAHI] Could not load sprite at '" + path + "'.");
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
