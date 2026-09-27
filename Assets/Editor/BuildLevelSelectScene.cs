// Assets/Editor/BuildLevelSelectScene.cs
//
// One-click scene builder for the "Ang Iyong Paglalakbay" (Your Journey)
// level-select map, assembled from the team's Canva assets (Assets/Sprites/):
//   journey_bg, journey_title, btn_back,
//   node_bahay(_locked), node_paaralan(_locked), node_palaruan(_locked), node_palengke(_locked)
//
// Node positions were measured directly off the Canva reference mockup
// (2000x1125) with a pixel grid overlay, then scaled to the 1920x1080 canvas
// used here (scale = 1920/2000 = 0.96). To tweak anything, move the object's
// RectTransform in the Scene view - no code changes needed.
//
// Only the Bahay node has a level behind it so far (Chapter1_Level1_UmagaNa),
// so it's the only one unlocked by default via LevelProgress. The other three
// point at scene names that don't exist yet - build them and they'll just work.
//
// Also inserts LevelSelect into Build Settings, between MainMenu and
// Chapter1_Level1_UmagaNa.
//
// Run via: Tools > SALINLAHI > Build Level Select Scene

using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class BuildLevelSelectScene
{
    private const string SpriteFolder = "Assets/Sprites/";
    private const string MainMenuScenePath = "Assets/Scenes/MainMenu.unity";
    private const string LevelSelectScenePath = "Assets/Scenes/LevelSelect.unity";
    private const string FirstLevelPath = "Assets/Scenes/Chapter1_Level1_UmagaNa.unity";

    [MenuItem("Tools/SALINLAHI/Build Level Select Scene")]
    public static void BuildScene()
    {
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

        // --- Background: covers the whole screen without stretching ---
        GameObject bg = CreateImage(canvasT, "Background", "journey_bg", Vector2.zero, new Vector2(1920, 1080), false);
        RectTransform bgRect = bg.GetComponent<RectTransform>();
        bgRect.anchorMin = bgRect.anchorMax = new Vector2(0.5f, 0.5f);
        AspectRatioFitter fitter = bg.AddComponent<AspectRatioFitter>();
        fitter.aspectMode = AspectRatioFitter.AspectMode.EnvelopeParent;
        fitter.aspectRatio = 2000f / 1125f;
        bg.GetComponent<Image>().raycastTarget = false;

        // --- Title ("Ang Iyong Paglalakbay / Your Journey / A Journey Through Language") ---
        GameObject title = CreateImage(canvasT, "Title", "journey_title", new Vector2(0, 345), new Vector2(1100, 435), true);
        title.GetComponent<Image>().raycastTarget = false;

        // --- Back button, top-left ---
        GameObject backBtn = CreateSpriteButton(canvasT, "BackButton", "btn_back", new Vector2(-859, 434), new Vector2(163, 154));

        // --- Journey nodes, bottom-left to top-right along the path ---
        LevelSelectController.LevelNode bahay = CreateNode(canvasT, "Node_Bahay",
            "node_bahay", "node_bahay_locked", new Vector2(-619, -331), new Vector2(221, 293),
            1, "Chapter1_Level1_UmagaNa");

        LevelSelectController.LevelNode paaralan = CreateNode(canvasT, "Node_Paaralan",
            "node_paaralan", "node_paaralan_locked", new Vector2(-216, -129), new Vector2(230, 293),
            2, "Chapter2_Level1_Paaralan");

        LevelSelectController.LevelNode palaruan = CreateNode(canvasT, "Node_Palaruan",
            "node_palaruan", "node_palaruan_locked", new Vector2(204, 34), new Vector2(226, 293),
            3, "Chapter3_Level1_Palaruan");

        LevelSelectController.LevelNode palengke = CreateNode(canvasT, "Node_Palengke",
            "node_palengke", "node_palengke_locked", new Vector2(638, 120), new Vector2(230, 293),
            4, "Chapter4_Level1_Palengke");

        // --- Wire controller ---
        LevelSelectController controller = canvasGO.AddComponent<LevelSelectController>();
        controller.mainMenuSceneName = "MainMenu";
        controller.backButton = backBtn.GetComponent<Button>();
        controller.nodes = new[] { bahay, paaralan, palaruan, palengke };

        // --- Save + insert into Build Settings ---
        System.IO.Directory.CreateDirectory("Assets/Scenes");
        EditorSceneManager.SaveScene(scene, LevelSelectScenePath);
        SetBuildOrder();

        Debug.Log("[SALINLAHI] Level Select map built from Canva assets and saved to " + LevelSelectScenePath +
                   ". Build Settings now runs MainMenu -> LevelSelect -> Chapter 1 Level 1. " +
                   "Only Bahay is unlocked until the other chapters' scenes exist and call LevelProgress.MarkComplete().");
    }

    /// <summary>MainMenu, then LevelSelect, then Chapter 1 Level 1, then anything else already listed.</summary>
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

    // ---------- helpers ----------

    private static LevelSelectController.LevelNode CreateNode(Transform parent, string name,
        string unlockedSpriteFile, string lockedSpriteFile, Vector2 anchoredPos, Vector2 size,
        int levelIndex, string sceneName)
    {
        GameObject go = CreateImage(parent, name, unlockedSpriteFile, anchoredPos, size, true);
        Image icon = go.GetComponent<Image>();

        Button button = go.AddComponent<Button>();
        button.targetGraphic = icon;
        ColorBlock colors = button.colors;
        colors.normalColor = Color.white;
        colors.highlightedColor = new Color(1f, 0.97f, 0.9f);
        colors.pressedColor = new Color(0.8f, 0.75f, 0.7f);
        colors.disabledColor = Color.white; // locked look comes from the grayscale sprite, not a tint
        colors.fadeDuration = 0.08f;
        button.colors = colors;

        return new LevelSelectController.LevelNode
        {
            button = button,
            icon = icon,
            unlockedSprite = LoadSprite(unlockedSpriteFile),
            lockedSprite = LoadSprite(lockedSpriteFile),
            levelIndex = levelIndex,
            sceneName = sceneName,
        };
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

    /// <summary>A button whose whole look is the Canva sprite. Darkens slightly when pressed.</summary>
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

    private static Sprite LoadSprite(string spriteFile)
    {
        string path = SpriteFolder + spriteFile + ".png";
        Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
        if (sprite == null)
            Debug.LogWarning("[SALINLAHI] Could not load sprite at '" + path + "'. Make sure it's imported as Sprite (2D and UI).");
        return sprite;
    }
}
