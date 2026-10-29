// Assets/Editor/BuildLevelSelectScene.cs
//
// One-click builder for the level select screen.
//
// A page per place, flipped through with the arrows: Bahay, Paaralan, Parke,
// Palengke, four pages and four dots. It used to be all sixteen scenes at
// once, numbered straight through to 16. Paging them lets the tiles be big
// enough for a small finger and lets the numbers restart inside each place,
// which is how a child would say them - Bahay one to six, not nine to
// fourteen.
//
// The order still comes from JourneyMap.Steps, which LevelProgress also counts
// along, so the screen and the save file cannot disagree about what comes
// next. Adding a scene there adds a tile here; nothing in this file changes.
//
// A tile can be entered once the player has reached it AND the scene behind it
// has been built. Anything else wears the padlock rather than a dimmed number:
// with the numbering restarted per place there is no longer a run of sixteen
// to read off the screen, so hiding an unreachable number costs nothing.
//
// Positions were measured off the composed mockup, kept beside the pieces as
// Art/levelselect_layout_reference.png (2000x1125), and scaled by 0.96 to the
// 1920x1080 canvas.
//
// Sprites expected in Assets/Sprites/, all cut by
// Tools/import_level_select_art.py:
//   ui_level_board, ui_select_levels, btn_page_left, btn_page_right,
//   btn_step_1..6, btn_step_locked, ui_step_star_on, ui_step_star_off,
//   ui_page_dot_on, ui_page_dot_off
// plus btn_back, and menu_room_bg from the main menu.
//
// There is no backdrop sprite of its own. The mockup's dimmed classroom is
// menu_room_bg under a brown wash, so that colour is laid over the menu's own
// background rather than shipping a second copy of the same room.
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

    // Fitted against the mockup's backdrop: alpha 0.81 over this colour turns
    // menu_room_bg into the dimmed room it shows, within 6/255 a channel.
    private static readonly Color Wash = new Color(0.286f, 0.157f, 0.106f, 0.81f);

    private static readonly Vector2 BoardPos = new Vector2(0f, -50f);
    private static readonly Vector2 BoardSize = new Vector2(1340f, 898f);
    private static readonly Vector2 TitlePos = new Vector2(0f, 362f);
    private static readonly Vector2 TitleSize = new Vector2(791f, 273f);
    private static readonly Vector2 BackPos = new Vector2(-859f, 434f);
    private static readonly Vector2 BackSize = new Vector2(114f, 108f);
    private static readonly Vector2 ArrowSize = new Vector2(175f, 164f);
    private const float ArrowX = 648f;
    private const float ArrowY = -38f;

    private const int Columns = 4;
    private const float TilePitch = 247f;
    private const float TopRowY = 107f;        // y of a tile's centre, top row
    private const float RowPitch = 266f;
    private const float OneRowY = -26f;        // a place short enough for one row
    private static readonly Vector2 TileSize = new Vector2(163f, 153f);

    private const float StarDrop = 108f;       // below the tile's centre
    private const float StarPitch = 58f;
    private static readonly Vector2 StarSize = new Vector2(48f, 47f);

    private const float DotsY = -354f;
    private const float DotPitch = 72f;
    private static readonly Vector2 DotSize = new Vector2(34f, 34f);

    // The sheet prints tiles 1..6, which covers the longest place. A place
    // with more scenes than this would ask for a tile that does not exist.
    private const int HighestPrintedTile = 6;

    [MenuItem("Tools/SALINLAHI/Build Level Select Scene")]
    public static void BuildScene()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            Debug.LogError("[SALINLAHI] Stop Play mode first - this rebuilds the scene, which Unity doesn't allow while the game is running.");
            return;
        }

        // An Image handed a null sprite draws a white box, with nothing but a
        // console warning to say why. Make the art loadable first.
        SpriteImportFixer.Verify(new[]
        {
            "menu_room_bg", "ui_level_board", "ui_select_levels", "btn_back",
            "btn_page_left", "btn_page_right", "btn_step_locked",
            "ui_step_star_on", "ui_step_star_off",
            "ui_page_dot_on", "ui_page_dot_off",
            "btn_step_1", "btn_step_2", "btn_step_3",
            "btn_step_4", "btn_step_5", "btn_step_6",
        }, "Level Select Scene");

        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        GameObject camGO = new GameObject("Main Camera");
        Camera cam = camGO.AddComponent<Camera>();
        cam.orthographic = true;
        cam.orthographicSize = 5.4f;
        cam.transform.position = new Vector3(0, 0, -10);
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = new Color(Wash.r, Wash.g, Wash.b, 1f);
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

        // --- The room, the wash over it, then the board ---
        GameObject background = CreateImage(canvasT, "Background", "menu_room_bg",
                                            Vector2.zero, new Vector2(1920f, 1080f), false);
        RectTransform backgroundRect = background.GetComponent<RectTransform>();
        backgroundRect.anchorMin = backgroundRect.anchorMax = new Vector2(0.5f, 0.5f);
        AspectRatioFitter fitter = background.AddComponent<AspectRatioFitter>();
        fitter.aspectMode = AspectRatioFitter.AspectMode.EnvelopeParent;
        fitter.aspectRatio = 2000f / 1125f;
        background.GetComponent<Image>().raycastTarget = false;

        GameObject wash = new GameObject("Wash", typeof(RectTransform));
        wash.transform.SetParent(canvasT, false);
        Image washImage = wash.AddComponent<Image>();
        washImage.color = Wash;
        washImage.raycastTarget = false;
        Stretch(wash.GetComponent<RectTransform>());

        GameObject board = CreateImage(canvasT, "Board", "ui_level_board", BoardPos, BoardSize, true);
        board.GetComponent<Image>().raycastTarget = false;

        GameObject title = CreateImage(canvasT, "Title", "ui_select_levels", TitlePos, TitleSize, true);
        title.GetComponent<Image>().raycastTarget = false;

        GameObject back = CreateSpriteButton(canvasT, "Button_Back", "btn_back", BackPos, BackSize);
        GameObject pageLeft = CreateSpriteButton(canvasT, "Button_PageLeft", "btn_page_left",
                                                 new Vector2(-ArrowX, ArrowY), ArrowSize);
        GameObject pageRight = CreateSpriteButton(canvasT, "Button_PageRight", "btn_page_right",
                                                  new Vector2(ArrowX, ArrowY), ArrowSize);

        // --- A page per place ---
        List<LevelSelectController.StepNode> nodes = new List<LevelSelectController.StepNode>();
        List<LevelSelectController.Page> pages = new List<LevelSelectController.Page>();
        Sprite lockedSprite = LoadSprite("btn_step_locked");
        int overflowing = 0;

        foreach (int levelIndex in LevelsInOrder())
        {
            List<int> sceneSteps = SceneStepsOf(levelIndex);
            string placeName = PlaceName(levelIndex);

            GameObject pageRoot = new GameObject("Page_" + pages.Count + "_" + placeName,
                                                 typeof(RectTransform));
            pageRoot.transform.SetParent(canvasT, false);
            Stretch(pageRoot.GetComponent<RectTransform>());

            if (sceneSteps.Count == 0)
            {
                // A place with nothing built yet still gets its page, so the
                // dots count the journey rather than what happens to be
                // finished. One padlock says what the empty board would not.
                nodes.Add(CreateTile(pageRoot.transform, FirstStepOf(levelIndex),
                                     "", 0, lockedSprite, new Vector2(0f, OneRowY)));
            }
            else
            {
                if (sceneSteps.Count > HighestPrintedTile) overflowing++;
                for (int i = 0; i < sceneSteps.Count; i++)
                {
                    int stepIndex = sceneSteps[i];
                    nodes.Add(CreateTile(pageRoot.transform, stepIndex,
                                         JourneyMap.Steps[stepIndex].sceneName, i + 1,
                                         lockedSprite, SlotPosition(i, sceneSteps.Count)));
                }
            }

            // Switched off here rather than left for Start, so the saved scene
            // holds one readable page instead of four stacked on top of each
            // other, and nothing flashes on the first frame.
            pageRoot.SetActive(pages.Count == 0);

            pages.Add(new LevelSelectController.Page
            {
                root = pageRoot,
                label = placeName,
                firstStep = FirstStepOf(levelIndex),
            });
        }

        // --- The dots, one per page, under the board ---
        GameObject dots = new GameObject("Dots", typeof(RectTransform));
        dots.transform.SetParent(canvasT, false);
        Stretch(dots.GetComponent<RectTransform>());
        float firstDotX = -(pages.Count - 1) * DotPitch * 0.5f;
        for (int i = 0; i < pages.Count; i++)
        {
            GameObject dot = CreateImage(dots.transform, "Dot_" + i, "ui_page_dot_off",
                                         new Vector2(firstDotX + i * DotPitch, DotsY), DotSize, true);
            dot.GetComponent<Image>().raycastTarget = false;
            pages[i].dot = dot.GetComponent<Image>();
        }

        LevelSelectController controller = canvasGO.AddComponent<LevelSelectController>();
        controller.mainMenuSceneName = "MainMenu";
        controller.backButton = back.GetComponent<Button>();
        controller.previousPageButton = pageLeft.GetComponent<Button>();
        controller.nextPageButton = pageRight.GetComponent<Button>();
        controller.nodes = nodes.ToArray();
        controller.pages = pages.ToArray();
        controller.starOnSprite = LoadSprite("ui_step_star_on");
        controller.starOffSprite = LoadSprite("ui_step_star_off");
        controller.dotOnSprite = LoadSprite("ui_page_dot_on");
        controller.dotOffSprite = LoadSprite("ui_page_dot_off");

        SceneFaderBuilder.Build();

        System.IO.Directory.CreateDirectory("Assets/Scenes");
        EditorSceneManager.SaveScene(scene, LevelSelectScenePath);
        SetBuildOrder();

        int playable = 0;
        foreach (LevelSelectController.StepNode n in nodes)
            if (!string.IsNullOrEmpty(n.sceneName)) playable++;

        Debug.Log("[SALINLAHI] Level select built and saved to " + LevelSelectScenePath +
                  ". " + pages.Count + " pages, " + nodes.Count + " tiles, " + playable +
                  " of them with a scene behind them." +
                  (overflowing > 0
                      ? " " + overflowing + " place(s) have more than " + HighestPrintedTile +
                        " scenes, which is more than the sheet prints - cut the missing tiles " +
                        "in Tools/import_level_select_art.py."
                      : ""));
    }

    /// <summary>
    /// Where the i-th of n tiles sits. Four to a row; a place short enough for
    /// one row gets it centred on the board instead of hanging along the top.
    /// Each row is centred on its own, so a row of two sits in the middle
    /// rather than tucked to the left under the row above.
    /// </summary>
    private static Vector2 SlotPosition(int i, int count)
    {
        if (count <= Columns)
            return new Vector2((i - (count - 1) * 0.5f) * TilePitch, OneRowY);

        int row = i / Columns;
        int inRow = i % Columns;
        int rowCount = Mathf.Min(Columns, count - row * Columns);
        return new Vector2((inRow - (rowCount - 1) * 0.5f) * TilePitch, TopRowY - row * RowPitch);
    }

    private static LevelSelectController.StepNode CreateTile(
        Transform parent, int stepIndex, string sceneName, int number,
        Sprite lockedSprite, Vector2 position)
    {
        // number 0 stands for a place with no scenes yet: there is no tile to
        // print, so the padlock is the whole face.
        Sprite unlockedSprite = number > 0 ? LoadSprite("btn_step_" + number) : lockedSprite;

        string name = "Tile_" + (number > 0 ? number.ToString() : "Locked");
        GameObject go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);

        Image image = go.AddComponent<Image>();
        image.sprite = unlockedSprite;
        image.preserveAspect = true;
        RectTransform rect = go.GetComponent<RectTransform>();
        rect.anchoredPosition = position;
        rect.sizeDelta = TileSize;

        Button button = go.AddComponent<Button>();
        button.targetGraphic = image;
        button.colors = TileColors();

        // Siblings of the tile rather than children of it, so the button's
        // press tint stays on the wood and does not wash over the stars.
        Image[] stars = new Image[3];
        for (int i = 0; i < stars.Length; i++)
        {
            Vector2 at = position + new Vector2((i - 1) * StarPitch, -StarDrop);
            GameObject star = CreateImage(parent, name + "_Star_" + i, "ui_step_star_off",
                                          at, StarSize, true);
            star.GetComponent<Image>().raycastTarget = false;
            stars[i] = star.GetComponent<Image>();
        }

        return new LevelSelectController.StepNode
        {
            button = button,
            icon = image,
            unlockedSprite = unlockedSprite,
            lockedSprite = lockedSprite,
            stars = stars,
            stepIndex = stepIndex,
            sceneName = sceneName,
        };
    }

    private static ColorBlock TileColors()
    {
        ColorBlock colors = ColorBlock.defaultColorBlock;
        colors.normalColor = Color.white;
        colors.highlightedColor = new Color(1f, 0.97f, 0.9f);
        colors.pressedColor = new Color(0.8f, 0.75f, 0.7f);
        colors.disabledColor = new Color(0.46f, 0.43f, 0.4f, 1f);
        colors.fadeDuration = 0.08f;
        return colors;
    }

    /// <summary>The place's name, taken from its marker on the journey.</summary>
    private static string PlaceName(int levelIndex)
    {
        foreach (JourneyMap.Step s in JourneyMap.Steps)
            if (s.levelIndex == levelIndex && s.kind == JourneyMap.StepKind.Level)
                return s.label;
        return "Level" + levelIndex;
    }

    /// <summary>First step of a place, marker included. Where its page begins.</summary>
    private static int FirstStepOf(int levelIndex)
    {
        for (int i = 0; i < JourneyMap.Steps.Length; i++)
            if (JourneyMap.Steps[i].levelIndex == levelIndex) return i;
        return 0;
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

    /// <summary>A button whose whole look is its sprite. Darkens when pressed.</summary>
    private static GameObject CreateSpriteButton(Transform parent, string name, string spriteFile,
        Vector2 anchoredPos, Vector2 size)
    {
        GameObject go = CreateImage(parent, name, spriteFile, anchoredPos, size, true);
        Button button = go.AddComponent<Button>();
        button.targetGraphic = go.GetComponent<Image>();
        button.colors = TileColors();
        return go;
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
            if (s.path == MainMenuScenePath || s.path == LevelSelectScenePath ||
                s.path == FirstLevelPath) continue;
            scenes.Add(s);
        }
        EditorBuildSettings.scenes = scenes.ToArray();
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
