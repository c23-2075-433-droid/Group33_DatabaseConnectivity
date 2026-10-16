// Assets/Editor/BuildLevelSelectScene.cs
//
// One-click builder for the level select screen.
//
// This used to be a winding journey map with the house, the school and the
// market drawn as landmarks along a road. It is now a plain grid: one row per
// level, the level's name beside it, and a numbered wooden tile for each of
// its scenes. Nothing to read but the numbers.
//
// The order still comes from JourneyMap.Steps, which LevelProgress also counts
// along, so the screen and the save file cannot disagree about what comes
// next. Adding a scene there adds a tile here; nothing in this file needs
// changing.
//
// The tiles are numbered straight through the whole journey rather than
// restarting inside each level: Bahay is 1 to 6, Paaralan carries on at 7, and
// so on to 16. The sheet only prints 1 to 7, so the numbers are drawn by Unity
// onto a blank tile that slice_level_buttons.py makes by painting the printed
// number off one.
//
// A tile is enterable only when the player has reached it AND the scene has
// actually been built. Anything else is dimmed, keeping its number: the sheet's
// padlock tile would hide the number, and a numbering that runs 1 to 16 is only
// worth having if all of it can be read.
//
// Sprites expected in Assets/Sprites/:
//   btn_level_blank, btn_level_blank_leaf, ui_select_levels
//     - cut by Tools/slice_level_buttons.py
//   levelselect_bg - made by Tools/make_levelselect_bg.py
//   btn_back
//
// Run via: Tools > SALINLAHI > Build Level Select Scene

using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class BuildLevelSelectScene
{
    private const string SpriteFolder = "Assets/Sprites/";
    private const string MainMenuScenePath = "Assets/Scenes/MainMenu.unity";
    private const string LevelSelectScenePath = "Assets/Scenes/LevelSelect.unity";
    private const string FirstLevelPath = "Assets/Scenes/Chapter1_Level1_UmagaNa.unity";

    // The sheet's own backdrop, lifted a little so the tiles' dark wood frames
    // still read against it.
    private static readonly Color Backdrop = new Color(0.106f, 0.122f, 0.071f);

    // Sized for a child's finger on a tablet rather than for fitting the most
    // tiles on screen: six across a row is the worst case and still leaves a
    // margin either side.
    private const float TileWidth = 196f;      // tile heights follow their art
    private const float TilePitch = 224f;

    private const float FirstRowY = 60f;       // bottom edge of the top row
    private const float RowPitch = 230f;

    private const string BlankTile = "btn_level_blank";
    private const string BlankLeafTile = "btn_level_blank_leaf";

    // Every fourth tile sprouts a leaf. One picture repeated sixteen times
    // reads as a spreadsheet; this is the variety the sheet itself has.
    private const int LeafEvery = 4;

    [MenuItem("Tools/SALINLAHI/Build Level Select Scene")]
    public static void BuildScene()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            Debug.LogError("[SALINLAHI] Stop Play mode first - this rebuilds the scene, which Unity doesn't allow while the game is running.");
            return;
        }

        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        GameObject camGO = new GameObject("Main Camera");
        Camera cam = camGO.AddComponent<Camera>();
        cam.orthographic = true;
        cam.orthographicSize = 5.4f;
        cam.transform.position = new Vector3(0, 0, -10);
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = Backdrop;
        camGO.tag = "MainCamera";
        camGO.AddComponent<AudioListener>();

        GameObject canvasGO = new GameObject("LevelSelect_Canvas");
        Canvas canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        CanvasScaler scaler = canvasGO.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.Expand;
        canvasGO.AddComponent<GraphicRaycaster>();

        GameObject eventSystemGO = new GameObject("EventSystem");
        eventSystemGO.AddComponent<UnityEngine.EventSystems.EventSystem>();
        eventSystemGO.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();

        Transform canvasT = canvasGO.transform;

        // A flat panel rather than a picture: the camera's clear colour does
        // not reach a Screen Space Overlay canvas, so the backdrop has to be
        // drawn as the first thing on the canvas itself.
        GameObject backdrop = new GameObject("Backdrop", typeof(RectTransform));
        backdrop.transform.SetParent(canvasT, false);
        Image backdropImage = backdrop.AddComponent<Image>();
        backdropImage.color = Backdrop;
        backdropImage.raycastTarget = false;
        Stretch(backdrop.GetComponent<RectTransform>());

        // On top of the flat colour, not instead of it: an Image with no sprite
        // draws a white rectangle, so a missing file would white out the screen
        // rather than fall back to something sensible.
        Sprite backgroundSprite = LoadSprite("levelselect_bg");
        if (backgroundSprite != null)
        {
            GameObject background = new GameObject("Background", typeof(RectTransform));
            background.transform.SetParent(canvasT, false);
            Image backgroundImage = background.AddComponent<Image>();
            backgroundImage.sprite = backgroundSprite;
            backgroundImage.raycastTarget = false;
            Stretch(background.GetComponent<RectTransform>());
        }

        GameObject title = CreateImage(canvasT, "Title", "ui_select_levels",
                                       new Vector2(0f, 380f), new Vector2(820f, 124f), true);
        title.GetComponent<Image>().raycastTarget = false;

        // --- One row per level, one tile per scene ---
        List<LevelSelectController.StepNode> built = new List<LevelSelectController.StepNode>();
        int row = 0;
        int skipped = 0;
        int number = 1;            // runs straight through every level

        foreach (int levelIndex in LevelsInOrder())
        {
            List<int> sceneSteps = SceneStepsOf(levelIndex);
            if (sceneSteps.Count == 0)
            {
                // A level with nothing behind it would be an empty row. Give it
                // scenes in JourneyMap and it appears.
                skipped++;
                continue;
            }

            float baseline = FirstRowY - row * RowPitch;

            // Rows hold different numbers of scenes, so each is centred on its
            // own. Starting them all at one x would leave the short rows
            // hanging off to the left.
            float firstX = -(sceneSteps.Count - 1) * TilePitch * 0.5f;

            for (int i = 0; i < sceneSteps.Count; i++)
            {
                int stepIndex = sceneSteps[i];
                float x = firstX + i * TilePitch;
                built.Add(CreateTile(canvasT, stepIndex, JourneyMap.Steps[stepIndex],
                                     number, x, baseline));
                number++;
            }

            row++;
        }

        GameObject back = CreateSpriteButton(canvasT, "Button_Back", "btn_back",
                                             new Vector2(-860f, 430f), new Vector2(120f, 120f));

        LevelSelectController controller = canvasGO.AddComponent<LevelSelectController>();
        controller.mainMenuSceneName = "MainMenu";
        controller.backButton = back.GetComponent<Button>();
        controller.nodes = built.ToArray();
        controller.playerMarker = null;   // no map to walk along any more

        SceneFaderBuilder.Build();

        System.IO.Directory.CreateDirectory("Assets/Scenes");
        EditorSceneManager.SaveScene(scene, LevelSelectScenePath);
        SetBuildOrder();

        int playable = 0;
        foreach (LevelSelectController.StepNode n in built)
            if (!string.IsNullOrEmpty(n.sceneName)) playable++;

        Debug.Log("[SALINLAHI] Level select built and saved to " + LevelSelectScenePath +
                  ". " + row + " level rows, " + built.Count + " tiles numbered 1-" +
                  (number - 1) + ", " + playable +
                  " of them with a scene behind them. " +
                  (skipped > 0
                      ? skipped + " level(s) have no scenes in JourneyMap yet and were left out."
                      : "Every level has scenes."));
    }

    /// <summary>Level numbers in the order JourneyMap lists them, without repeats.</summary>
    private static List<int> LevelsInOrder()
    {
        List<int> order = new List<int>();
        foreach (JourneyMap.Step s in JourneyMap.Steps)
            if (!order.Contains(s.levelIndex)) order.Add(s.levelIndex);
        return order;
    }

    /// <summary>Indices into JourneyMap.Steps of one level's scenes, in order.</summary>
    private static List<int> SceneStepsOf(int levelIndex)
    {
        List<int> steps = new List<int>();
        for (int i = 0; i < JourneyMap.Steps.Length; i++)
        {
            JourneyMap.Step s = JourneyMap.Steps[i];
            if (s.levelIndex == levelIndex && s.kind == JourneyMap.StepKind.Scene)
                steps.Add(i);
        }
        return steps;
    }

    /// <summary>Pin a RectTransform to every edge of its parent.</summary>
    private static void Stretch(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }

    private static LevelSelectController.StepNode CreateTile(
        Transform parent, int stepIndex, JourneyMap.Step step, int number,
        float x, float baseline)
    {
        bool leafed = number % LeafEvery == 1;
        string unlocked = leafed ? BlankLeafTile : BlankTile;
        Sprite unlockedSprite = LoadSprite(unlocked);

        // Each tile keeps its own proportions. The leafed one is taller than
        // the bare one, and forcing them into one box would squash exactly it.
        float height = TileWidth;
        if (unlockedSprite != null && unlockedSprite.rect.width > 0f)
            height = TileWidth * (unlockedSprite.rect.height / unlockedSprite.rect.width);

        string name = "Tile_" + number + "_L" + step.levelIndex + "_S" + step.label;
        GameObject go = CreateSpriteButton(parent, name, unlocked,
                                           new Vector2(x, baseline), new Vector2(TileWidth, height));

        // Sitting the tiles on a shared baseline keeps the numbers in a line
        // whether or not a tile has a leaf on its head.
        RectTransform rect = go.GetComponent<RectTransform>();
        rect.pivot = new Vector2(0.5f, 0f);
        rect.anchoredPosition = new Vector2(x, baseline);

        GameObject numberGO = CreateNumber(go.transform, number, leafed);

        return new LevelSelectController.StepNode
        {
            button = go.GetComponent<Button>(),
            icon = go.GetComponent<Image>(),
            unlockedSprite = unlockedSprite,
            // The same tile either way. The padlock tile would cover the number,
            // and the numbering is only useful if the whole journey can be read
            // off the screen, so a tile that cannot be entered is dimmed instead.
            lockedSprite = unlockedSprite,
            lockOverlay = null,
            numberLabel = numberGO,
            stepIndex = stepIndex,
            sceneName = step.sceneName,
        };
    }

    /// <summary>The number painted on a blank tile, sitting on the panel.</summary>
    private static GameObject CreateNumber(Transform tile, int number, bool leafed)
    {
        GameObject go = new GameObject("Number", typeof(RectTransform));
        go.transform.SetParent(tile, false);

        Text text = go.AddComponent<Text>();
        text.text = number.ToString();
        text.font = UIFont.Get();
        text.fontSize = 86;
        text.fontStyle = FontStyle.Bold;
        text.alignment = TextAnchor.MiddleCenter;
        // Best-fit so 16 sits on the panel as comfortably as 1 does, rather
        // than two digits running into the frame.
        text.resizeTextForBestFit = true;
        text.resizeTextMinSize = 28;
        text.resizeTextMaxSize = 86;
        text.color = new Color(0.99f, 0.95f, 0.85f);
        text.raycastTarget = false;

        Outline outline = go.AddComponent<Outline>();
        outline.effectColor = new Color(0.36f, 0.21f, 0.09f, 0.85f);
        outline.effectDistance = new Vector2(2f, -2f);

        RectTransform rect = go.GetComponent<RectTransform>();
        Stretch(rect);
        // The panel is inset inside the wooden frame, and on a leafed tile the
        // leaf takes a slice off the top, so the number has to come down with
        // it or it sits on the frame instead of the panel.
        float inset = TileWidth * 0.17f;
        float leafPad = leafed ? LeafAllowance() : 0f;
        rect.offsetMin = new Vector2(inset, inset * 0.9f);
        rect.offsetMax = new Vector2(-inset, -(inset * 0.9f + leafPad));

        return go;
    }

    /// <summary>How much taller the leafed tile is, in layout units.</summary>
    private static float LeafAllowance()
    {
        Sprite plain = LoadSprite(BlankTile);
        Sprite leaf = LoadSprite(BlankLeafTile);
        if (plain == null || leaf == null || plain.rect.width <= 0f || leaf.rect.width <= 0f)
            return 0f;

        float plainRatio = plain.rect.height / plain.rect.width;
        float leafRatio = leaf.rect.height / leaf.rect.width;
        return TileWidth * (leafRatio - plainRatio);
    }

    private static void SetBuildOrder()
    {
        var scenes = new List<EditorBuildSettingsScene>();
        if (System.IO.File.Exists(MainMenuScenePath))
            scenes.Add(new EditorBuildSettingsScene(MainMenuScenePath, true));
        scenes.Add(new EditorBuildSettingsScene(LevelSelectScenePath, true));
        if (System.IO.File.Exists(FirstLevelPath))
            scenes.Add(new EditorBuildSettingsScene(FirstLevelPath, true));

        foreach (var s in EditorBuildSettings.scenes)
        {
            if (s.path == MainMenuScenePath || s.path == LevelSelectScenePath || s.path == FirstLevelPath) continue;
            scenes.Add(s);
        }
        EditorBuildSettings.scenes = scenes.ToArray();
    }

    private static GameObject CreateImage(Transform parent, string name, string spriteFile,
        Vector2 anchoredPos, Vector2 size, bool preserveAspect)
    {
        GameObject go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        Image img = go.AddComponent<Image>();
        img.sprite = LoadSprite(spriteFile);
        img.preserveAspect = preserveAspect;
        RectTransform rect = go.GetComponent<RectTransform>();
        rect.anchoredPosition = anchoredPos;
        rect.sizeDelta = size;
        return go;
    }

    private static GameObject CreateSpriteButton(Transform parent, string name, string spriteFile,
        Vector2 anchoredPos, Vector2 size)
    {
        GameObject go = CreateImage(parent, name, spriteFile, anchoredPos, size, true);
        Button button = go.AddComponent<Button>();
        button.targetGraphic = go.GetComponent<Image>();
        ColorBlock colors = button.colors;
        colors.normalColor = Color.white;
        colors.highlightedColor = new Color(1f, 0.97f, 0.9f);
        colors.pressedColor = new Color(0.8f, 0.75f, 0.7f);
        // Carries the whole locked/unlocked signal now that no padlock does, so
        // it has to be darker than a normal greyed-out control.
        colors.disabledColor = new Color(0.46f, 0.43f, 0.40f, 1f);
        colors.fadeDuration = 0.08f;
        button.colors = colors;
        return go;
    }

    private static Sprite LoadSprite(string spriteFile)
    {
        string path = SpriteFolder + spriteFile + ".png";
        Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
        if (sprite == null)
            Debug.LogWarning("[SALINLAHI] Could not load sprite at '" + path + "'. Make sure it's imported as Sprite (2D and UI).");
        return sprite;
    }
}
