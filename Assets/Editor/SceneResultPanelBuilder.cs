// Assets/Editor/SceneResultPanelBuilder.cs
//
// Builds the summary panel a scene ends on, and wires SceneResultPanel to it.
//
// Scenes used to fade straight into the next one the moment the last word was
// said. This puts a panel in between: how the scene went, then Home, Replay
// and Next.
//
// THE PANEL AND BUTTON ART IS STILL PROVISIONAL - the bamboo results panel and
// the okay, replay and back buttons are stand-ins, because the purchased LEVEL
// COMPLETE sheet is not in the project yet. The SPRITES block below is the
// whole swap: nothing outside this file names a sprite, and SceneResultPanel
// does not care what it is pointed at. The stars are real.
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

    // ---- SPRITES: the whole of the swap when the new sheet lands ----
    private const string PanelSprite = "ui_results_panel";
    private const string NextSprite = "btn_okay";
    private const string ReplaySprite = "btn_replay";
    private const string HomeSprite = "btn_back";
    // The trio is a drawn arrangement, so each slot has its own pair rather
    // than one star scaled three times. Cut by Tools/slice_stars.py.
    private static readonly string[] StarSlots = { "left", "mid", "right" };
    private const float StarWidthSide = 112f;
    private const float StarWidthMid = 152f;
    private const float StarSpacing = 148f;
    private const float StarMidLift = 24f;

    // ui_results_panel is 1014x793; keeping the ratio stops the bamboo frame
    // stretching.
    private const float PanelWidth = 1000f;
    private const float PanelHeight = PanelWidth / (1014f / 793f);

    // The frame eats the outer edges, so the contents sit inside the measured
    // interior rather than the full rect.
    private const float InsetTop = 0.175f;
    private const float InsetBottom = 0.142f;
    private const float InsetSide = 0.113f;

    private const float SmallButton = 104f;
    private const float NextButtonWidth = 300f;
    private const float ButtonGap = 40f;

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
                              string title = "WELL DONE!",
                              bool levelEnd = false)
    {
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

        float interiorTop = PanelHeight * (0.5f - InsetTop);
        float interiorBottom = -PanelHeight * (0.5f - InsetBottom);
        float interiorWidth = PanelWidth * (1f - InsetSide * 2f);

        Text titleText = CreateText(panel.transform, "TitleText", title,
            new Vector2(0f, interiorTop - 50f), new Vector2(interiorWidth, 96f),
            52, TextAnchor.MiddleCenter);

        SceneResultPanel.StarSlot[] stars = CreateStars(panel.transform, interiorTop - 170f);

        // Pulled up when a records strip follows, so the two do not collide.
        Text scoreText = CreateText(panel.transform, "ScoreText", "",
            new Vector2(0f, interiorTop - (levelEnd ? 330f : 300f)),
            new Vector2(interiorWidth, 80f), 40, TextAnchor.MiddleCenter);

        // Only on a level's last scene: the scores read back out of the
        // database, which is the half of the Activity 5 requirement that has
        // to be visible on screen rather than merely fetched.
        Text recordsText = null;
        if (levelEnd)
        {
            recordsText = CreateText(panel.transform, "RecordsText", "",
                new Vector2(0f, interiorTop - 408f), new Vector2(interiorWidth, 190f),
                26, TextAnchor.UpperCenter);
            recordsText.lineSpacing = 1.15f;
        }

        // Home, Replay, Next - in that order, so the one that carries on sits
        // under the thumb on the right. Laid out from the row's total width so
        // the three stay centred as a group; spacing them by eye left the set
        // sitting off to one side.
        float row = interiorBottom + 110f;
        float total = SmallButton * 2f + NextButtonWidth + ButtonGap * 2f;
        float x = -total * 0.5f + SmallButton * 0.5f;

        Button home = CreateButton(panel.transform, "HomeButton", HomeSprite,
                                   new Vector2(x, row), SmallButton);
        x += SmallButton * 0.5f + ButtonGap + SmallButton * 0.5f;
        Button replay = CreateButton(panel.transform, "ReplayButton", ReplaySprite,
                                     new Vector2(x, row), SmallButton);
        x += SmallButton * 0.5f + ButtonGap + NextButtonWidth * 0.5f;
        Button next = CreateButton(panel.transform, "NextButton", NextSprite,
                                   new Vector2(x, row), NextButtonWidth);

        SceneResultPanel result = canvasGO.AddComponent<SceneResultPanel>();
        result.objectiveController = controller;
        result.voiceCommand = voiceCommand;
        result.panelRoot = content;
        result.titleText = titleText;
        result.scoreText = scoreText;
        result.stars = stars;
        result.nextButton = next;
        result.replayButton = replay;
        result.homeButton = home;
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

    private static SceneResultPanel.StarSlot[] CreateStars(Transform parent, float y)
    {
        List<SceneResultPanel.StarSlot> stars = new List<SceneResultPanel.StarSlot>();

        for (int i = 0; i < StarSlots.Length; i++)
        {
            string slot = StarSlots[i];
            bool middle = i == 1;

            Sprite earned = LoadSprite("star_gold_" + slot);
            Sprite empty = LoadSprite("star_grey_" + slot);

            GameObject go = new GameObject("Star_" + slot, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            Image image = go.AddComponent<Image>();
            image.sprite = earned;
            image.preserveAspect = true;
            image.raycastTarget = false;

            // Each star keeps its own proportions; the sheet's outer two are
            // not the same shape as the middle, so one size would squash them.
            float width = middle ? StarWidthMid : StarWidthSide;
            float height = width;
            if (earned != null && earned.rect.width > 0f)
                height = width * (earned.rect.height / earned.rect.width);

            RectTransform rect = go.GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(width, height);
            rect.anchoredPosition = new Vector2((i - 1) * StarSpacing,
                                                y + (middle ? StarMidLift : 0f));

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
