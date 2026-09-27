// Assets/Editor/BuildMainMenuScene.cs
//
// One-click scene builder for the SALINLAHI main menu, assembled from the
// team's Canva assets (Assets/Sprites/):
//   menu_bg, menu_title, menu_boy, btn_play, btn_settings, btn_about
//
// Positions/sizes were measured from the Canva reference layout (2000x1125)
// and scaled to the 1920x1080 canvas used here. To tweak anything, move the
// object's RectTransform in the Scene view - no code changes needed.
//
// No nickname field for now (NicknameManager just uses its default name).
//
// Also puts MainMenu first in Build Settings so the game boots into it,
// followed by the LevelSelect journey map and then Chapter 1 Level 1.
//
// Run via: Tools > SALINLAHI > Build Main Menu Scene

using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class BuildMainMenuScene
{
    private const string SpriteFolder = "Assets/Sprites/";
    private const string MenuScenePath = "Assets/Scenes/MainMenu.unity";
    private const string LevelSelectPath = "Assets/Scenes/LevelSelect.unity";
    private const string FirstLevelPath = "Assets/Scenes/Chapter1_Level1_UmagaNa.unity";

    [MenuItem("Tools/SALINLAHI/Build Main Menu Scene")]
    public static void BuildScene()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            Debug.LogError("[SALINLAHI] Stop Play mode first - this rebuilds the scene, which Unity doesn't allow while the game is running.");
            return;
        }

        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        // --- Camera ---
        GameObject camGO = new GameObject("Main Camera");
        Camera cam = camGO.AddComponent<Camera>();
        cam.orthographic = true;
        cam.orthographicSize = 5.4f;
        cam.transform.position = new Vector3(0, 0, -10);
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = Color.black;
        camGO.tag = "MainCamera";
        camGO.AddComponent<AudioListener>();

        // --- Canvas: 1920x1080 reference, landscape ---
        GameObject canvasGO = new GameObject("MainMenu_Canvas");
        Canvas canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        CanvasScaler scaler = canvasGO.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.Expand; // never crop the buttons on odd aspect ratios
        canvasGO.AddComponent<GraphicRaycaster>();

        GameObject eventSystemGO = new GameObject("EventSystem");
        eventSystemGO.AddComponent<UnityEngine.EventSystems.EventSystem>();
        eventSystemGO.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();

        Transform canvasT = canvasGO.transform;

        // --- Background: always covers the whole screen without stretching
        //     (wider phones like 19.5:9 crop a little top/bottom instead of
        //     showing black bars or squashing the art). ---
        GameObject bg = CreateImage(canvasT, "Background", "menu_bg", Vector2.zero, new Vector2(1920, 1080), false);
        RectTransform bgRect = bg.GetComponent<RectTransform>();
        bgRect.anchorMin = bgRect.anchorMax = new Vector2(0.5f, 0.5f);
        AspectRatioFitter fitter = bg.AddComponent<AspectRatioFitter>();
        fitter.aspectMode = AspectRatioFitter.AspectMode.EnvelopeParent;
        fitter.aspectRatio = 2000f / 1125f;
        bg.GetComponent<Image>().raycastTarget = false;

        // --- Title (drawn before the boy: his hair overlaps the left leaf) ---
        GameObject title = CreateImage(canvasT, "Title", "menu_title", new Vector2(43, 282), new Vector2(1233, 516), true);
        title.GetComponent<Image>().raycastTarget = false;

        // --- Boy, anchored to the bottom-left so he stays on the left edge
        //     on wider screens. Cut off at the knees by the screen bottom,
        //     same as the Canva layout. ---
        GameObject boy = CreateImage(canvasT, "Boy", "menu_boy", Vector2.zero, new Vector2(381, 994), true);
        RectTransform boyRect = boy.GetComponent<RectTransform>();
        boyRect.anchorMin = boyRect.anchorMax = new Vector2(0, 0);
        boyRect.anchoredPosition = new Vector2(334, 372); // centre = (-626,-168) from screen centre
        boy.GetComponent<Image>().raycastTarget = false;

        // --- Buttons ---
        GameObject playBtn = CreateSpriteButton(canvasT, "PlayButton", "btn_play", new Vector2(81, -86), new Vector2(412, 258));
        GameObject settingsBtn = CreateSpriteButton(canvasT, "SettingsButton", "btn_settings", new Vector2(-88, -319), new Vector2(356, 199));
        GameObject aboutBtn = CreateSpriteButton(canvasT, "AboutButton", "btn_about", new Vector2(274, -312), new Vector2(350, 192));

        // --- Settings popup (volume) ---
        GameObject settingsPanel = CreatePanel(canvasT, "SettingsPanel", new Vector2(700, 500));
        CreateText(settingsPanel.transform, "SettingsTitle", "Settings",
            new Vector2(0, 170), new Vector2(500, 70), 48, FontStyle.Bold, Color.white);
        CreateText(settingsPanel.transform, "VolumeLabel", "Volume",
            new Vector2(0, 40), new Vector2(300, 50), 30, FontStyle.Normal, Color.white);
        GameObject sliderGO = CreateSlider(settingsPanel.transform, "VolumeSlider", new Vector2(0, -30), new Vector2(500, 60));
        GameObject settingsCloseGO = CreateTextButton(settingsPanel.transform, "CloseButton", "Close",
            new Vector2(0, -170), new Vector2(260, 80));

        // --- About popup (credits) ---
        GameObject aboutPanel = CreatePanel(canvasT, "AboutPanel", new Vector2(760, 560));
        CreateText(aboutPanel.transform, "AboutTitle", "About SALINLAHI",
            new Vector2(0, 200), new Vector2(600, 70), 44, FontStyle.Bold, Color.white);
        CreateText(aboutPanel.transform, "AboutBody",
            "A voice-interactive Filipino vocabulary learning game.\n\n" +
            "BSIT Capstone Project\nUniversity of Perpetual Help System Laguna\n\n" +
            "Daquis, Jhesza Mhei G.\nLacida, Kylo Bryan\nManzanero, Kyla Samantha",
            new Vector2(0, 10), new Vector2(660, 320), 28, FontStyle.Normal, Color.white);
        GameObject aboutCloseGO = CreateTextButton(aboutPanel.transform, "CloseButton", "Close",
            new Vector2(0, -220), new Vector2(260, 80));

        settingsPanel.SetActive(false);
        aboutPanel.SetActive(false);

        // --- Wire controller (no nickname field, no mute icon in this design) ---
        MainMenuController controller = canvasGO.AddComponent<MainMenuController>();
        controller.playButton = playBtn.GetComponent<Button>();
        controller.settingsButton = settingsBtn.GetComponent<Button>();
        controller.aboutButton = aboutBtn.GetComponent<Button>();
        controller.settingsPanel = settingsPanel;
        controller.aboutPanel = aboutPanel;
        controller.settingsCloseButton = settingsCloseGO.GetComponent<Button>();
        controller.aboutCloseButton = aboutCloseGO.GetComponent<Button>();
        controller.volumeSlider = sliderGO.GetComponent<Slider>();
        controller.levelSelectSceneName = "LevelSelect";

        // --- Save + put MainMenu first in Build Settings ---
        System.IO.Directory.CreateDirectory("Assets/Scenes");
        EditorSceneManager.SaveScene(scene, MenuScenePath);
        SetBuildOrder();

        Debug.Log("[SALINLAHI] Main Menu built from Canva assets and saved to " + MenuScenePath +
                   ". Build Settings now boots MainMenu first, then Chapter 1 Level 1.");
    }

    /// <summary>MainMenu at index 0, then LevelSelect, then Chapter 1 Level 1, then any other scenes already listed.</summary>
    private static void SetBuildOrder()
    {
        var scenes = new List<EditorBuildSettingsScene>
        {
            new EditorBuildSettingsScene(MenuScenePath, true)
        };
        if (System.IO.File.Exists(LevelSelectPath))
            scenes.Add(new EditorBuildSettingsScene(LevelSelectPath, true));
        if (System.IO.File.Exists(FirstLevelPath))
            scenes.Add(new EditorBuildSettingsScene(FirstLevelPath, true));

        foreach (var s in EditorBuildSettings.scenes)
        {
            if (s.path == MenuScenePath || s.path == LevelSelectPath || s.path == FirstLevelPath) continue;
            scenes.Add(s);
        }
        EditorBuildSettings.scenes = scenes.ToArray();
    }

    // ---------- helpers ----------

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

    /// <summary>A button whose whole look is the Canva sprite (label included). Darkens slightly when pressed.</summary>
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
        colors.fadeDuration = 0.08f;
        button.colors = colors;
        return go;
    }

    private static GameObject CreateText(Transform parent, string name, string content,
        Vector2 anchoredPos, Vector2 size, int fontSize, FontStyle style, Color color)
    {
        GameObject go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        Text text = go.AddComponent<Text>();
        text.text = content;
        text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        text.fontSize = fontSize;
        text.fontStyle = style;
        text.alignment = TextAnchor.MiddleCenter;
        text.color = color;
        text.horizontalOverflow = HorizontalWrapMode.Wrap;
        text.verticalOverflow = VerticalWrapMode.Overflow;
        text.raycastTarget = false;
        RectTransform rect = go.GetComponent<RectTransform>();
        rect.anchoredPosition = anchoredPos;
        rect.sizeDelta = size;
        return go;
    }

    private static GameObject CreateTextButton(Transform parent, string name, string label,
        Vector2 anchoredPos, Vector2 size)
    {
        GameObject go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        Image img = go.AddComponent<Image>();
        // Plain solid colour - no sprite. Unity 6 no longer serves the old
        // built-in "UI/Skin/*.psd" sprites via GetBuiltinResource; asking for
        // them only logs an error and returns null.
        img.color = new Color(0.55f, 0.33f, 0.18f); // wood brown to match the menu buttons
        go.AddComponent<Button>();
        RectTransform rect = go.GetComponent<RectTransform>();
        rect.anchoredPosition = anchoredPos;
        rect.sizeDelta = size;
        CreateText(go.transform, "Text", label, Vector2.zero, size, 40, FontStyle.Bold, Color.white);
        return go;
    }

    private static GameObject CreateSlider(Transform parent, string name, Vector2 anchoredPos, Vector2 size)
    {
        GameObject go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        RectTransform rect = go.GetComponent<RectTransform>();
        rect.anchoredPosition = anchoredPos;
        rect.sizeDelta = size;
        Slider slider = go.AddComponent<Slider>();

        GameObject background = new GameObject("Background", typeof(RectTransform));
        background.transform.SetParent(go.transform, false);
        Image bgImg = background.AddComponent<Image>();
        bgImg.color = new Color(1, 1, 1, 0.3f); // solid colour, no built-in sprite (see CreateTextButton)
        RectTransform bgRect = background.GetComponent<RectTransform>();
        bgRect.anchorMin = new Vector2(0, 0.25f);
        bgRect.anchorMax = new Vector2(1, 0.75f);
        bgRect.offsetMin = Vector2.zero;
        bgRect.offsetMax = Vector2.zero;

        GameObject fillArea = new GameObject("Fill Area", typeof(RectTransform));
        fillArea.transform.SetParent(go.transform, false);
        RectTransform fillAreaRect = fillArea.GetComponent<RectTransform>();
        fillAreaRect.anchorMin = new Vector2(0, 0.25f);
        fillAreaRect.anchorMax = new Vector2(1, 0.75f);
        fillAreaRect.offsetMin = new Vector2(5, 0);
        fillAreaRect.offsetMax = new Vector2(-5, 0);

        GameObject fill = new GameObject("Fill", typeof(RectTransform));
        fill.transform.SetParent(fillArea.transform, false);
        Image fillImg = fill.AddComponent<Image>();
        fillImg.color = new Color(0.95f, 0.65f, 0.25f); // solid colour, no built-in sprite
        RectTransform fillRect = fill.GetComponent<RectTransform>();
        fillRect.anchorMin = Vector2.zero;
        fillRect.anchorMax = Vector2.one;
        fillRect.sizeDelta = Vector2.zero;

        GameObject handleArea = new GameObject("Handle Slide Area", typeof(RectTransform));
        handleArea.transform.SetParent(go.transform, false);
        RectTransform handleAreaRect = handleArea.GetComponent<RectTransform>();
        handleAreaRect.anchorMin = Vector2.zero;
        handleAreaRect.anchorMax = Vector2.one;
        handleAreaRect.offsetMin = new Vector2(10, 0);
        handleAreaRect.offsetMax = new Vector2(-10, 0);

        GameObject handle = new GameObject("Handle", typeof(RectTransform));
        handle.transform.SetParent(handleArea.transform, false);
        Image handleImg = handle.AddComponent<Image>();
        handleImg.color = Color.white; // square handle - the built-in round Knob sprite is gone in Unity 6
        RectTransform handleRect = handle.GetComponent<RectTransform>();
        handleRect.sizeDelta = new Vector2(30, 30);

        slider.fillRect = fillRect;
        slider.handleRect = handleRect;
        slider.targetGraphic = handleImg;
        slider.direction = Slider.Direction.LeftToRight;
        slider.minValue = 0f;
        slider.maxValue = 1f;
        slider.value = 1f;
        return go;
    }

    private static GameObject CreatePanel(Transform parent, string name, Vector2 size)
    {
        GameObject go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        Image img = go.AddComponent<Image>();
        img.color = new Color(0.25f, 0.15f, 0.08f, 0.96f); // dark wood, solid colour (no built-in sprite)
        RectTransform rect = go.GetComponent<RectTransform>();
        rect.anchoredPosition = Vector2.zero;
        rect.sizeDelta = size;
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
