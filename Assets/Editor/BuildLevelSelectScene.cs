// Assets/Editor/BuildLevelSelectScene.cs
//
// One-click builder for the two screens behind ADVENTURE.
//
// First a board per place - HOME, SCHOOL, PARK - each with the picture the
// journey map already uses for it. Pick one and the same canvas turns into
// that place's levels: the big board, the place's own banner over it, and a
// numbered tile per scene with its stars underneath.
//
// This replaces the single paged screen with arrows either side. A child who
// wanted the park had to know it was three presses to the right; now it is
// one press, onto a picture they can recognise.
//
// The order still comes from JourneyMap.Steps, which LevelProgress also counts
// along, so the screen and the save file cannot disagree about what comes
// next. Adding a scene there adds a tile here; nothing in this file changes.
// A place with no scenes built at all gets no board - the kit draws three.
//
// A tile can be entered once the player has reached it AND the scene behind it
// has been built. Anything else wears the padlock rather than a dimmed number.
//
// Positions were measured off the composed mockups, kept beside the pieces as
// Art/adventure_layout_reference.png and Art/levels_layout_reference.png
// (2000x1125), and scaled by 0.96 to the 1920x1080 canvas.
//
// Sprites expected in Assets/Sprites/. The boards, banners and the two corner
// buttons are cut by Tools/import_adventure_art.py; the tiles, stars and the
// place pictures were already here, and the backdrop is the main menu's own
// classroom, blurred (see MenuBlurBackdrop).
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

    /// <summary>
    /// The art for one place on the picker. Keyed by its level number on the
    /// journey, so renaming or reordering places cannot hand HOME the park's
    /// picture. lockedBoard is empty for a place that is never locked.
    /// </summary>
    private class PlaceArt
    {
        public string board, lockedBoard, picture, lockedPicture, title;
    }

    private static readonly Dictionary<int, PlaceArt> Art = new Dictionary<int, PlaceArt>
    {
        // Bahay shows the modern cottage, not the journey map's nipa hut: next
        // to the school and the park the hut read as the odd one out. See
        // Tools/cut_modern_house.py, which is where it comes from and why.
        { 1, new PlaceArt { board = "ui_place_home",      lockedBoard = "",
                            picture = "map_bahay_modern",
                            lockedPicture = "map_bahay_modern_locked",
                            title = "ui_title_home" } },
        { 2, new PlaceArt { board = "ui_place_school", lockedBoard = "ui_place_school_locked",
                            picture = "map_paaralan",  lockedPicture = "map_paaralan_locked",
                            title = "ui_title_school" } },
        { 3, new PlaceArt { board = "ui_place_park",   lockedBoard = "ui_place_park_locked",
                            picture = "map_parke",     lockedPicture = "map_parke_locked",
                            title = "ui_title_park" } },
    };

    // --- Shared furniture, on screen for both ---
    private static readonly Vector2 BackPos = new Vector2(-852f, 429f);
    private static readonly Vector2 BackSize = new Vector2(132f, 152f);
    private static readonly Vector2 MenuPos = new Vector2(829f, 429f);
    private static readonly Vector2 MenuSize = new Vector2(144f, 153f);

    // --- The place picker ---
    private static readonly Vector2 PickerTitlePos = new Vector2(-12f, 278f);
    private static readonly Vector2 PickerTitleSize = new Vector2(1271f, 153f);
    private static readonly Vector2 PlaceBoardSize = new Vector2(580f, 479f);
    private const float PlaceBoardY = -114f;
    private const float PlaceBoardPitch = 612f;

    // The picture sits in the board's inner panel, inside the pale rim:
    // measured on ui_place_home as x 36-574 of 604 and y 30-401 of 499, then
    // pulled in so nothing touches the frame and, on a locked board, nothing
    // runs under the padlock in the top right corner.
    private static readonly Vector2 PictureOffset = new Vector2(-6f, 26f);
    private static readonly Vector2 PictureSize = new Vector2(424f, 292f);

    // --- The levels of one place ---
    private static readonly Vector2 BoardPos = new Vector2(-9f, -27f);
    private static readonly Vector2 BoardSize = new Vector2(1264f, 929f);
    private static readonly Vector2 GridTitlePos = new Vector2(-23f, 415f);
    private static readonly Vector2 GridTitleSize = new Vector2(866f, 151f);

    private const int Columns = 4;
    private const float TilePitch = 247f;
    private const float TopRowY = 150f;        // y of a tile's centre, top row
    private const float RowPitch = 267f;
    private const float OneRowY = 30f;         // a place short enough for one row
    private static readonly Vector2 TileSize = new Vector2(163f, 153f);

    private const float StarDrop = 119f;       // below the tile's centre
    private const float StarPitch = 58f;
    private static readonly Vector2 StarSize = new Vector2(48f, 47f);

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
        List<string> needed = new List<string>
        {
            "menu_classroom_bg_blur", "ui_levels_board", "ui_title_adventure",
            "btn_back_arrow", "btn_levels_menu",
            "btn_step_locked", "ui_step_star_on", "ui_step_star_off",
            "btn_step_1", "btn_step_2", "btn_step_3",
            "btn_step_4", "btn_step_5", "btn_step_6",
        };
        foreach (PlaceArt art in Art.Values)
        {
            needed.Add(art.board);
            needed.Add(art.picture);
            needed.Add(art.lockedPicture);
            needed.Add(art.title);
            if (!string.IsNullOrEmpty(art.lockedBoard)) needed.Add(art.lockedBoard);
        }
        SpriteImportFixer.Verify(needed.ToArray(), "Level Select Scene");

        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        GameObject camGO = new GameObject("Main Camera");
        Camera cam = camGO.AddComponent<Camera>();
        cam.orthographic = true;
        cam.orthographicSize = 5.4f;
        cam.transform.position = new Vector3(0, 0, -10);
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = Color.black;
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

        // --- The same classroom the menu shows, already out of focus. Both
        //     screens are things laid on top of the menu, so they keep its
        //     backdrop rather than shipping a dimmed copy of the room. ---
        GameObject background = CreateImage(canvasT, "Background", "menu_classroom_bg_blur",
                                            Vector2.zero, new Vector2(1920f, 1080f), false);
        RectTransform backgroundRect = background.GetComponent<RectTransform>();
        backgroundRect.anchorMin = backgroundRect.anchorMax = new Vector2(0.5f, 0.5f);
        AspectRatioFitter fitter = background.AddComponent<AspectRatioFitter>();
        fitter.aspectMode = AspectRatioFitter.AspectMode.EnvelopeParent;
        fitter.aspectRatio = 2000f / 1125f;
        background.GetComponent<Image>().raycastTarget = false;

        // --- The two screens, as roots that are switched on and off ---
        GameObject pickerRoot = CreateStretched(canvasT, "PlacePicker");
        GameObject gridRoot = CreateStretched(canvasT, "LevelGrid");

        GameObject pickerTitle = CreateImage(pickerRoot.transform, "Title", "ui_title_adventure",
                                             PickerTitlePos, PickerTitleSize, true);
        pickerTitle.GetComponent<Image>().raycastTarget = false;

        GameObject board = CreateImage(gridRoot.transform, "Board", "ui_levels_board",
                                       BoardPos, BoardSize, true);
        board.GetComponent<Image>().raycastTarget = false;

        // Its sprite is replaced per place at runtime; HOME is only what the
        // saved scene happens to show.
        GameObject gridTitle = CreateImage(gridRoot.transform, "Title", Art[1].title,
                                           GridTitlePos, GridTitleSize, true);
        gridTitle.GetComponent<Image>().raycastTarget = false;

        // --- Corner buttons, outside both roots: they are on either screen ---
        GameObject back = CreateSpriteButton(canvasT, "Button_Back", "btn_back_arrow",
                                             BackPos, BackSize);
        GameObject menu = CreateSpriteButton(canvasT, "Button_Menu", "btn_levels_menu",
                                             MenuPos, MenuSize);

        // --- A board on the picker and a page of tiles for each place ---
        List<LevelSelectController.StepNode> nodes = new List<LevelSelectController.StepNode>();
        List<LevelSelectController.Place> places = new List<LevelSelectController.Place>();
        Sprite lockedTile = LoadSprite("btn_step_locked");
        int overflowing = 0, skipped = 0;

        foreach (int levelIndex in LevelsInOrder())
        {
            List<int> sceneSteps = SceneStepsOf(levelIndex);
            string placeName = PlaceName(levelIndex);

            // A place with nothing built, or with no board drawn for it, is
            // left off rather than given an empty board to press. Palengke is
            // both: it is on the journey as somewhere the game is going.
            if (sceneSteps.Count == 0 || !Art.ContainsKey(levelIndex))
            {
                skipped++;
                continue;
            }
            PlaceArt art = Art[levelIndex];

            GameObject tilesRoot = CreateStretched(gridRoot.transform, "Tiles_" + placeName);

            if (sceneSteps.Count > HighestPrintedTile) overflowing++;
            for (int i = 0; i < sceneSteps.Count; i++)
            {
                int stepIndex = sceneSteps[i];
                nodes.Add(CreateTile(tilesRoot.transform, stepIndex,
                                     JourneyMap.Steps[stepIndex].sceneName, i + 1,
                                     lockedTile, SlotPosition(i, sceneSteps.Count)));
            }

            // Switched off here rather than left for Start, so the saved scene
            // holds one readable page instead of three stacked on top of each
            // other, and nothing flashes on the first frame.
            tilesRoot.SetActive(places.Count == 0);

            float x = (places.Count - 1) * PlaceBoardPitch;   // three boards, middle on centre
            GameObject boardGO = CreateSpriteButton(pickerRoot.transform, "Place_" + placeName,
                                                    art.board, new Vector2(x, PlaceBoardY),
                                                    PlaceBoardSize);

            GameObject picture = CreateImage(boardGO.transform, "Picture", art.picture,
                                             PictureOffset, PictureSize, true);
            picture.GetComponent<Image>().raycastTarget = false;

            places.Add(new LevelSelectController.Place
            {
                tilesRoot = tilesRoot,
                boardButton = boardGO.GetComponent<Button>(),
                boardImage = boardGO.GetComponent<Image>(),
                openBoard = LoadSprite(art.board),
                lockedBoard = string.IsNullOrEmpty(art.lockedBoard) ? null : LoadSprite(art.lockedBoard),
                illustration = picture.GetComponent<Image>(),
                openIllustration = LoadSprite(art.picture),
                lockedIllustration = LoadSprite(art.lockedPicture),
                gridTitle = LoadSprite(art.title),
                label = placeName,
                firstStep = FirstStepOf(levelIndex),
            });
        }

        // The picker is where the screen opens, so the grid starts switched
        // off - same reason the pages do.
        pickerRoot.SetActive(true);
        gridRoot.SetActive(false);

        LevelSelectController controller = canvasGO.AddComponent<LevelSelectController>();
        controller.mainMenuSceneName = "MainMenu";
        controller.backButton = back.GetComponent<Button>();
        controller.menuButton = menu.GetComponent<Button>();
        controller.pickerRoot = pickerRoot;
        controller.gridRoot = gridRoot;
        controller.gridTitleImage = gridTitle.GetComponent<Image>();
        controller.nodes = nodes.ToArray();
        controller.places = places.ToArray();
        controller.starOnSprite = LoadSprite("ui_step_star_on");
        controller.starOffSprite = LoadSprite("ui_step_star_off");

        SceneFaderBuilder.Build();

        System.IO.Directory.CreateDirectory("Assets/Scenes");
        EditorSceneManager.SaveScene(scene, LevelSelectScenePath);
        SetBuildOrder();

        int playable = 0;
        foreach (LevelSelectController.StepNode n in nodes)
            if (!string.IsNullOrEmpty(n.sceneName)) playable++;

        Debug.Log("[SALINLAHI] Level select built and saved to " + LevelSelectScenePath +
                  ". " + places.Count + " places, " + nodes.Count + " tiles, " + playable +
                  " of them with a scene behind them." +
                  (skipped > 0
                      ? " " + skipped + " place(s) on the journey have no board here, either " +
                        "because nothing is built in them yet or because the kit draws no board " +
                        "for them - add one to Art in this file."
                      : "") +
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
        colors.disabledColor = Color.white;   // the locked face is drawn, not dimmed
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

    /// <summary>A full-screen, invisible holder for one screen's contents.</summary>
    private static GameObject CreateStretched(Transform parent, string name)
    {
        GameObject go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        Stretch(go.GetComponent<RectTransform>());
        return go;
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
