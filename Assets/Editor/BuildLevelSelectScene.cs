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
// A scene's tile carries its number within its level - scene 3 of Bahay gets
// the tile with a 3 on it - which is why the artwork's baked-in numbers can be
// used directly instead of drawing text over blank tiles. No level has more
// than the seven the sheet provides.
//
// A tile is enterable only when the player has reached it AND the scene has
// actually been built. Anything else shows the padlock, because from the
// child's side "not yet" and "not made yet" are the same thing.
//
// Sprites expected in Assets/Sprites/ (cut by Tools/slice_level_buttons.py):
//   btn_level_1 .. btn_level_7, btn_level_locked, ui_select_levels, btn_back
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
    private const float FirstTileX = -390f;    // centre of the first tile
    private const float LabelRightX = -528f;   // where the level name ends

    private const float FirstRowY = 60f;       // bottom edge of the top row
    private const float RowPitch = 230f;

    private const int HighestNumberedTile = 7; // what the artwork provides

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
        RectTransform backdropRect = backdrop.GetComponent<RectTransform>();
        backdropRect.anchorMin = Vector2.zero;
        backdropRect.anchorMax = Vector2.one;
        backdropRect.offsetMin = Vector2.zero;
        backdropRect.offsetMax = Vector2.zero;

        GameObject title = CreateImage(canvasT, "Title", "ui_select_levels",
                                       new Vector2(0f, 380f), new Vector2(820f, 124f), true);
        title.GetComponent<Image>().raycastTarget = false;

        // --- One row per level, one tile per scene ---
        List<LevelSelectController.StepNode> built = new List<LevelSelectController.StepNode>();
        int row = 0;
        int skipped = 0;

        foreach (int levelIndex in LevelsInOrder())
        {
            List<int> sceneSteps = SceneStepsOf(levelIndex);
            if (sceneSteps.Count == 0)
            {
                // A level with nothing behind it would be a heading over an
                // empty row. Give it scenes in JourneyMap and it appears.
                skipped++;
                continue;
            }

            float baseline = FirstRowY - row * RowPitch;
            CreateRowLabel(canvasT, LevelNameOf(levelIndex), baseline);

            for (int i = 0; i < sceneSteps.Count; i++)
            {
                int stepIndex = sceneSteps[i];
                float x = FirstTileX + i * TilePitch;
                built.Add(CreateTile(canvasT, stepIndex, JourneyMap.Steps[stepIndex], x, baseline));
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
                  ". " + row + " level rows, " + built.Count + " tiles, " + playable +
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

    /// <summary>The level marker's label - "Bahay" - or a number if there isn't one.</summary>
    private static string LevelNameOf(int levelIndex)
    {
        foreach (JourneyMap.Step s in JourneyMap.Steps)
            if (s.levelIndex == levelIndex && s.kind == JourneyMap.StepKind.Level)
                return s.label;
        return "Level " + levelIndex;
    }

    private static void CreateRowLabel(Transform parent, string text, float baseline)
    {
        GameObject go = new GameObject("Label_" + text, typeof(RectTransform));
        go.transform.SetParent(parent, false);

        Text label = go.AddComponent<Text>();
        label.text = text;
        label.font = UIFont.Get();
        label.fontSize = 56;
        label.fontStyle = FontStyle.Bold;
        label.alignment = TextAnchor.MiddleRight;
        label.color = new Color(0.98f, 0.88f, 0.64f);
        label.raycastTarget = false;

        Outline outline = go.AddComponent<Outline>();
        outline.effectColor = new Color(0.22f, 0.13f, 0.05f, 0.95f);
        outline.effectDistance = new Vector2(2f, -2f);

        RectTransform rect = go.GetComponent<RectTransform>();
        rect.pivot = new Vector2(1f, 0f);
        rect.sizeDelta = new Vector2(520f, 80f);
        rect.anchoredPosition = new Vector2(LabelRightX, baseline + 44f);
    }

    private static LevelSelectController.StepNode CreateTile(
        Transform parent, int stepIndex, JourneyMap.Step step, float x, float baseline)
    {
        string unlocked = TileSpriteFor(step.label);
        Sprite unlockedSprite = LoadSprite(unlocked);

        // Each tile keeps its own proportions. The ones with a leaf sprouting
        // over the top are taller than the bare ones, and forcing them all
        // into one box would squash exactly those.
        float height = TileWidth;
        if (unlockedSprite != null && unlockedSprite.rect.width > 0f)
            height = TileWidth * (unlockedSprite.rect.height / unlockedSprite.rect.width);

        string name = "Tile_L" + step.levelIndex + "_S" + step.label;
        GameObject go = CreateSpriteButton(parent, name, unlocked,
                                           new Vector2(x, baseline), new Vector2(TileWidth, height));

        // Sitting the tiles on a shared baseline keeps the numbers in a line
        // whether or not a tile has a leaf on its head.
        RectTransform rect = go.GetComponent<RectTransform>();
        rect.pivot = new Vector2(0.5f, 0f);
        rect.anchoredPosition = new Vector2(x, baseline);

        return new LevelSelectController.StepNode
        {
            button = go.GetComponent<Button>(),
            icon = go.GetComponent<Image>(),
            unlockedSprite = unlockedSprite,
            lockedSprite = LoadSprite("btn_level_locked"),
            lockOverlay = null,          // the locked tile has its own padlock
            stepIndex = stepIndex,
            sceneName = step.sceneName,
        };
    }

    /// <summary>The numbered tile for a scene, or the padlock if it runs past the set.</summary>
    private static string TileSpriteFor(string label)
    {
        int number;
        if (int.TryParse(label, out number) && number >= 1 && number <= HighestNumberedTile)
            return "btn_level_" + number;

        Debug.LogWarning("[SALINLAHI] No numbered tile for scene label '" + label +
                         "'. The artwork goes up to " + HighestNumberedTile +
                         "; using the padlock tile instead.");
        return "btn_level_locked";
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
        colors.disabledColor = new Color(0.78f, 0.78f, 0.78f, 1f);
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
