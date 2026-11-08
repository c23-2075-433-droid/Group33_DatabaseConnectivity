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

    // ---- SPRITES, cut by Tools/cut_result_ui.py ----
    private const string PanelSprite = "ui_result_board";
    private const string TitleCompleteSprite = "ui_result_title_complete";
    private const string TitleTryAgainSprite = "ui_result_title_tryagain";
    private const string NextSprite = "btn_result_continue";
    private const string ReplaySprite = "btn_result_replay";
    private const string CloseSprite = "btn_result_close";

    // ui_result_board is 793x941; keeping the ratio stops the frame
    // stretching. TOTAL POINTS: and WORDS COMPLETED: are printed into it, so
    // this builder places only the figures that follow them.
    private const float PanelWidth = 740f;
    private const float PanelHeight = 878f;

    // Everything below was measured off the composed mockup, kept beside the
    // pieces as Art/result_layout_new_reference.png (2000x1125), as fractions
    // of the board - so the whole panel can be resized by PanelWidth alone.
    private const float TitleFrac = 0.062f;
    private const float GreetingFrac = 0.4416f;
    private const float PlaceFrac = 0.5005f;
    private const float PointsFrac = 0.6307f;
    private const float WordsFrac = 0.7508f;
    private const float RecordsFrac = 0.818f;
    private const float ButtonFrac = 0.875f;

    // Fractions of the board, all checked against the mockup by compositing
    // the cut sprites at these numbers and diffing the result by eye.
    private const float TitleWidthFrac = 0.63f;
    private const float TitleXFrac = -0.015f;   // the banner sits a hair left of centre
    private const float TitleAspect = 193f / 661f;

    private const float CloseXFrac = 0.395f;    // from the board's centre
    private const float CloseFrac = 0.105f;
    private const float CloseSizeFrac = 0.130f;

    // The figures are right-aligned near the plates' right edge, because the
    // labels printed on the board are long - WORDS COMPLETED: reaches most of
    // the way across its own plate - and what follows them is short.
    //
    // 0.92 put them ON the rim: the two plates end at 0.918 and 0.916 of the
    // board, measured off ui_result_board.png, so the last digit was touching
    // the moulding. Held off it by the same amount as the vocabulary rows.
    // Only the right edge moves - the box keeps its width, so the figures
    // have exactly as much room to shrink into as before.
    private const float ValueRightFrac = 0.92f - WordsLearnedPanelBuilder.PlateInset;
    private const float ValueWidthFrac = 0.30f;

    // "WORDS COMPLETED:" is the longer label by a wide margin. Measured off
    // ui_result_board.png, its last pixel is at 0.730 of the board against
    // 0.571 for "TOTAL POINTS:" - so where the points figure has 237px of
    // bare plate to fill, the words figure has 120. This is that 120 less a
    // gap, as a fraction of the board.
    private const float WordsValueWidthFrac = 0.1405f;

    // Sized against the lettering printed beside them: those capitals are
    // 44px in the 941px-tall art, 41.1px at the size the panel is built, and
    // this font's capitals are 0.695 of its point size - so 59 would match
    // exactly. 52 is deliberately a little under that.
    //
    // 52 is the largest ceiling at which the two rows agree. The words box
    // is narrow, so a short score tops out near 53 however high the ceiling
    // goes; at 59 the points figure took the full 59 and the words figure
    // sat at 53 beside it, close enough to look like a mistake rather than a
    // decision. At 52 a typical "60" and "3 / 5" are both 52.
    private const int ValueMaxSize = 52;

    // 60 is not spare room, it is the ceiling that stops best fit WRAPPING.
    //
    // Best fit picks the largest size that fits the box, and with wrapping
    // allowed a two-line layout is one of the things that can fit: at 76px
    // tall, "12 / 20" fit better as two 38px lines than as one 35px line, so
    // the score stacked itself. A box 60 tall can hold two lines only up to
    // 30px, and every score shape the game can produce sets larger than that
    // on one line, so one line always wins. The matching Truncate is below -
    // with the vertical Overflow this builder uses elsewhere, height is not
    // a constraint at all and the wrap is free.
    private const float ValueHeight = 60f;

    private const float ButtonWidthFrac = 0.26f;
    private const float ButtonXFrac = 0.19f;

    private static readonly Vector2 RecordsSize = new Vector2(580f, 86f);

    // Where each star's own centre sits relative to the row's, in the art's
    // own pixels, and how wide that row is there. The row is drawn rather
    // than tiled - it swells towards the middle and the outer pairs sit
    // lower - so no slot can be worked out from the one beside it.
    //
    // Only the centres are listed. Each star's SIZE is read from its sprite at
    // build time, so re-cutting the row in Tools/cut_result_ui.py changes the
    // panel without anyone editing a table here. They share a baseline,
    // because the arc lives in the stars' top edges and not their bottoms.
    private static readonly float[] StarCentres = { -336.5f, -198f, 2f, 199f, 335.5f };
    private const float StarRowSourceWidth = 804f;
    private const float StarRowWidth = 0.82f;
    private const float StarBaselineFrac = 0.378f;

    /// <summary>A fraction of the way down the board, as a Y from its centre.</summary>
    private static float FromTop(float frac)
    {
        return (0.5f - frac) * PanelHeight;
    }

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

        if (Object.FindAnyObjectByType<EventSystem>() == null)
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
        titleRect.anchoredPosition = new Vector2(PanelWidth * TitleXFrac, FromTop(TitleFrac));
        titleRect.sizeDelta = new Vector2(PanelWidth * TitleWidthFrac,
                                          PanelWidth * TitleWidthFrac * TitleAspect);

        SceneResultPanel.StarSlot[] stars = CreateStars(panel.transform);

        // The two lines the board speaks to the child with. Neither existed
        // before: the panel knew the run but never the player.
        Text greetingText = CreateText(panel.transform, "GreetingText", "",
            new Vector2(0f, FromTop(GreetingFrac)),
            new Vector2(PanelWidth * 0.82f, 62f), 44, TextAnchor.MiddleCenter);

        // This line carries a name somebody typed, and the box takes 16
        // characters. "GREAT JOB, JIRO!" is 333px of a 607px box at 44, but
        // "GREAT JOB, JUAN MIGUEL JOSE!" is 607 exactly and a name of wide
        // letters runs to 882 - and with no best fit at all it had nothing to
        // do but wrap and hang off the board. Same shape of fault as the
        // score stacking, just waiting on a longer nickname.
        greetingText.resizeTextForBestFit = true;
        greetingText.resizeTextMinSize = 28;
        greetingText.resizeTextMaxSize = 44;

        Text placeText = CreateText(panel.transform, "PlaceText", "",
            new Vector2(0f, FromTop(PlaceFrac)),
            new Vector2(PanelWidth * 0.90f, 44f), 26, TextAnchor.MiddleCenter);
        placeText.resizeTextForBestFit = true;
        placeText.resizeTextMinSize = 16;
        placeText.resizeTextMaxSize = 26;

        // The greeting and the place line are set in the display face too.
        // Everything the child reads on this board now letters the same way
        // as the lettering painted into it; the records strip underneath is
        // the one thing left in the UI font, because it is a column of
        // dates and scores rather than a sentence addressed to them.
        //
        // Truncate for the same reason the figures have it - see the note
        // there. Longest place line is "YOU SUCCESSFULLY COMPLETED
        // PAARALAN: LEVEL 2!" at 589px of a 666px box, so it holds 26 and
        // stays on one line.
        foreach (Text line in new[] { greetingText, placeText })
        {
            line.verticalOverflow = VerticalWrapMode.Truncate;
            VocabularyFont.Apply(line);
        }

        // Right-aligned against the plates, because the labels printed on the
        // board are long and the figures that follow them are short.
        //
        // The two boxes share a right edge and nothing else. Each one reaches
        // left only as far as its own printed label allows, because best fit
        // measures the BOX and knows nothing about the lettering in the art -
        // give the words figure the room the points figure has and a run of
        // "12 / 20" sets itself large enough to park on top of the D of
        // COMPLETED.
        float valueRight = PanelWidth * (ValueRightFrac - 0.5f);

        float pointsWidth = PanelWidth * ValueWidthFrac;
        Text pointsText = CreateText(panel.transform, "PointsText", "",
            new Vector2(valueRight - pointsWidth * 0.5f, FromTop(PointsFrac)),
            new Vector2(pointsWidth, ValueHeight), ValueMaxSize, TextAnchor.MiddleRight);

        float wordsWidth = PanelWidth * WordsValueWidthFrac;
        Text wordsText = CreateText(panel.transform, "WordsText", "",
            new Vector2(valueRight - wordsWidth * 0.5f, FromTop(WordsFrac)),
            new Vector2(wordsWidth, ValueHeight), ValueMaxSize, TextAnchor.MiddleRight);

        // A four-figure score, or "12 / 15", must not grow into its label.
        //
        // These two, and ONLY these two, are set in the display face.
        // "TOTAL POINTS:" and "WORDS COMPLETED:" are painted into
        // ui_result_board in the same chunky distressed capitals as the
        // vocabulary board, so the figures that follow them sit on the same
        // plate, an inch away, in a different typeface - which is the one
        // place on this board where the mismatch is impossible to miss. The
        // greeting, the place line and the records strip are sentences to
        // read rather than lettering to match, and stay in the UI font.
        foreach (Text figure in new[] { pointsText, wordsText })
        {
            figure.resizeTextForBestFit = true;
            figure.resizeTextMinSize = 20;
            figure.resizeTextMaxSize = ValueMaxSize;

            // A score is one line or it is wrong. Everything else on this
            // board overflows downwards on purpose - the records strip grows
            // to however many rows came back - but that setting makes height
            // free, and free height is what lets best fit answer "too wide"
            // by stacking "3 / 5" into "3 /" over "5". Truncate puts the
            // height back in play, and ValueHeight above keeps two lines
            // from ever being the better fit.
            figure.verticalOverflow = VerticalWrapMode.Truncate;

            VocabularyFont.Apply(figure);
        }

        // Only on a level's last scene: the scores read back out of the
        // database, which is the half of the Activity 5 requirement that has
        // to be visible on screen rather than merely fetched.
        Text recordsText = null;
        if (levelEnd)
        {
            recordsText = CreateText(panel.transform, "RecordsText", "",
                new Vector2(0f, FromTop(RecordsFrac)), RecordsSize, 18,
                TextAnchor.UpperCenter);
            recordsText.lineSpacing = 1.1f;
            recordsText.resizeTextForBestFit = true;
            recordsText.resizeTextMinSize = 12;
            recordsText.resizeTextMaxSize = 18;
        }

        // One row whether or not the records strip is there - the new board
        // has a place for both, so the buttons no longer move to make room.
        float row = FromTop(ButtonFrac);

        float buttonX = PanelWidth * ButtonXFrac;
        float buttonWidth = PanelWidth * ButtonWidthFrac;

        Button replay = CreateButton(panel.transform, "ReplayButton", ReplaySprite,
                                     new Vector2(-buttonX, row), buttonWidth);
        Button next = CreateButton(panel.transform, "NextButton", NextSprite,
                                   new Vector2(buttonX, row), buttonWidth);
        Button close = CreateButton(panel.transform, "CloseButton", CloseSprite,
                                    new Vector2(PanelWidth * CloseXFrac, FromTop(CloseFrac)),
                                    PanelWidth * CloseSizeFrac);

        SceneResultPanel result = canvasGO.AddComponent<SceneResultPanel>();
        result.objectiveController = controller;
        result.voiceCommand = voiceCommand;
        result.panelRoot = content;
        result.titleImage = titleImage;
        result.completeSprite = LoadSprite(TitleCompleteSprite);
        result.tryAgainSprite = LoadSprite(TitleTryAgainSprite);
        result.pointsText = pointsText;
        result.wordsText = wordsText;
        result.greetingText = greetingText;
        result.placeText = placeText;
        result.wordsLearned = WordsLearnedPanelBuilder.Build(canvasGO.transform, controller);
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

        float scale = PanelWidth * StarRowWidth / StarRowSourceWidth;
        float baseline = FromTop(StarBaselineFrac);

        for (int i = 0; i < StarCentres.Length; i++)
        {
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

            // Measured off the unlit art, which is the one always present: the
            // two are drawn a touch different in size and a slot that changed
            // size as it lit would make the row twitch.
            Sprite measure = empty != null ? empty : earned;
            float w = measure != null ? measure.rect.width * scale : 70f;
            float h = measure != null ? measure.rect.height * scale : 72f;

            RectTransform rect = go.GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(w, h);
            rect.anchoredPosition = new Vector2(StarCentres[i] * scale, baseline + h * 0.5f);

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
