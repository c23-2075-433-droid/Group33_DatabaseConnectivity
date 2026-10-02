// Assets/Editor/BuildBahayScene2.cs
//
// One-click scene builder for Bahay Scene 2 ("Punta sa Banyo" - walking to
// the bathroom). Teaches five words: Lakad, Kaliwa, Kanan, Bukas, Ilaw.
//
// Layout: the hallway and the bathroom sit side by side along X, each scaled
// to exactly fill one 16:9 camera view (19.2 x 10.8 world units), so the
// player walks right out of the hallway and into the bathroom while the
// camera follows. The bathroom starts under a dark overlay until "Ilaw".
//
// Sprites expected in Assets/Sprites/:
//   hallway_background, bathroom_background, door_open (optional for now),
//   lying_down / sitting_up / standing / walk_frame_1, ui_arrow, ui_word_badge
//
// Run via: Tools > SALINLAHI > Build Bahay Scene 2

using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class BuildBahayScene2
{
    private const string SpriteFolder = "Assets/Sprites/";
    private const string ScenePath = "Assets/Scenes/Chapter1_Level2_Banyo.unity";
    private const string WhitePixelPath = SpriteFolder + "white_pixel.png";

    // Backgrounds are 1536x864 at 100 pixels-per-unit = 15.36 x 8.64 world
    // units. The camera (orthographic size 5.4) shows 10.8 x 19.2, so each
    // background is scaled up to fill exactly one screen with no gaps.
    private const float BgScale = 10.8f / 8.64f;      // 1.25
    private const float ScreenWidth = 19.2f;          // 15.36 * 1.25
    private const float FloorY = -2.98f;              // top of the floor planks

    [MenuItem("Tools/SALINLAHI/Build Bahay Scene 2")]
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

        // --- Rooms, side by side: hallway at x=0, bathroom one screen right ---
        CreateBackground("Background_Hallway", "hallway_background", 0f);
        CreateBackground("Background_Bathroom", "bathroom_background", ScreenWidth);

        // --- Darkness over the bathroom only; the hallway stays lit ---
        GameObject darkGO = new GameObject("BathroomDarkness");
        darkGO.transform.position = new Vector3(ScreenWidth, 0f, 0f);
        SpriteRenderer darkSr = darkGO.AddComponent<SpriteRenderer>();
        darkSr.sprite = GetOrCreateWhitePixel();
        darkSr.color = new Color(0.05f, 0.08f, 0.25f, 0.88f); // deep night blue
        darkSr.sortingLayerName = "Props";
        darkSr.sortingOrder = 50;                             // above props, below the player
        darkSr.drawMode = SpriteDrawMode.Sliced;
        darkSr.size = new Vector2(ScreenWidth, 10.8f);

        LightSwitchOverlay lightSwitch = darkGO.AddComponent<LightSwitchOverlay>();
        lightSwitch.overlay = darkSr;
        lightSwitch.darkAlpha = 0.88f;

        // --- Bathroom door: an OPEN door laid over the closed one painted into
        //     the hallway art, hidden until the player says "Bukas". ---
        GameObject doorOpen = new GameObject("Door_Open");
        doorOpen.transform.position = new Vector3(7.31f, 0.18f, 0f);
        SpriteRenderer doorSr = doorOpen.AddComponent<SpriteRenderer>();
        doorSr.sprite = LoadSprite("door_open");   // may be null until the art is imported
        doorSr.sortingLayerName = "Props";
        doorSr.sortingOrder = 10;
        // Scale to roughly the height of the painted door, keeping the art's
        // own aspect ratio - stretching it to a fixed size would distort it.
        if (doorSr.sprite != null)
        {
            float s = 6.4f / doorSr.sprite.bounds.size.y;
            doorOpen.transform.localScale = new Vector3(s, s, 1f);
        }
        doorOpen.SetActive(false);                 // "Bukas" switches this on

        // --- Player ---
        GameObject playerGO = new GameObject("player_character");
        playerGO.tag = "Player";
        playerGO.transform.position = new Vector3(-6.5f, FloorY + 0.75f, 0f);

        SpriteRenderer playerSr = playerGO.AddComponent<SpriteRenderer>();
        playerSr.sortingLayerName = "Player";
        playerSr.sprite = LoadSprite("standing");  // this scene opens already awake

        Rigidbody2D playerRb = playerGO.AddComponent<Rigidbody2D>();
        playerRb.gravityScale = 3f;
        playerRb.freezeRotation = true;

        BoxCollider2D playerCol = playerGO.AddComponent<BoxCollider2D>();
        playerCol.size = new Vector2(0.6f, 1.4f);

        PlayerMovement player = playerGO.AddComponent<PlayerMovement>();
        player.lyingDownSprite = LoadSprite("lying_down");
        player.sittingUpSprite = LoadSprite("sitting_up");
        player.standingSprite = LoadSprite("standing");
        player.walkFrames = new[]
        {
            LoadSprite("walk_frame_1"), LoadSprite("walk_frame_2"),
            LoadSprite("walk_frame_3"), LoadSprite("walk_frame_4"),
        };
        // The player is already up and about in this scene, so movement is
        // unlocked from the start rather than gated behind Bangon/Tayo.
        player.currentWakeStage = PlayerMovement.WakeStage.Standing;

        VoiceCommand voiceCommand = playerGO.AddComponent<VoiceCommand>();
        voiceCommand.player = player;

        CameraFollow follow = camGO.AddComponent<CameraFollow>();
        follow.target = playerGO.transform;

        // --- Floor, spanning both rooms ---
        GameObject groundGO = new GameObject("Ground");
        groundGO.transform.position = new Vector3(ScreenWidth * 0.5f, FloorY - 0.25f, 0f);
        BoxCollider2D groundCol = groundGO.AddComponent<BoxCollider2D>();
        groundCol.size = new Vector2(ScreenWidth * 3f, 0.5f);

        // --- Word prompt canvas (world space, same pattern as Level 1) ---
        GameObject uiRoot = new GameObject("UI_WordPrompts_Canvas");
        Canvas canvas = uiRoot.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        canvas.overrideSorting = true;
        canvas.sortingLayerName = "UI";
        canvas.sortingOrder = 10;
        uiRoot.transform.position = Vector3.zero;
        uiRoot.transform.localScale = new Vector3(0.01f, 0.01f, 0.01f);
        uiRoot.GetComponent<RectTransform>().sizeDelta = new Vector2(4000, 1200);
        uiRoot.AddComponent<GraphicRaycaster>();

        if (Object.FindFirstObjectByType<UnityEngine.EventSystems.EventSystem>() == null)
        {
            GameObject es = new GameObject("EventSystem");
            es.AddComponent<UnityEngine.EventSystems.EventSystem>();
            es.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
        }

        // Positions are in canvas units (world units x100).
        GameObject pLakad  = CreatePromptUI(uiRoot.transform, "Prompt_Lakad",  "Lakad",  new Vector3(-650, 100, 0));
        GameObject pKaliwa = CreatePromptUI(uiRoot.transform, "Prompt_Kaliwa", "Kaliwa", new Vector3(-850, 100, 0));
        GameObject pKanan  = CreatePromptUI(uiRoot.transform, "Prompt_Kanan",  "Kanan",  new Vector3(300, 100, 0));
        GameObject pBukas  = CreatePromptUI(uiRoot.transform, "Prompt_Bukas",  "Bukas",  new Vector3(731, 380, 0));
        GameObject pIlaw   = CreatePromptUI(uiRoot.transform, "Prompt_Ilaw",   "Ilaw",   new Vector3(1920, 150, 0));

        // --- Objectives, in order. Each onCorrect is a persistent listener so
        //     it shows up and stays editable in the Inspector. ---
        var lakad  = MakeObjective("Lakad",  pLakad);
        UnityEventTools.AddPersistentListener(lakad.onCorrect, player.WalkForward);

        var kaliwa = MakeObjective("Kaliwa", pKaliwa);
        UnityEventTools.AddPersistentListener(kaliwa.onCorrect, player.WalkLeft);

        var kanan  = MakeObjective("Kanan",  pKanan);
        UnityEventTools.AddPersistentListener(kanan.onCorrect, player.WalkRight);

        // "Bukas" reveals the open-door sprite over the painted closed one.
        var bukas  = MakeObjective("Bukas",  pBukas);
        UnityEventTools.AddBoolPersistentListener(bukas.onCorrect, doorOpen.SetActive, true);

        // "Ilaw" fades the darkness off the bathroom.
        var ilaw   = MakeObjective("Ilaw",   pIlaw);
        UnityEventTools.AddPersistentListener(ilaw.onCorrect, lightSwitch.TurnOnLight);

        SceneObjectiveController controller = uiRoot.AddComponent<SceneObjectiveController>();
        controller.voiceCommand = voiceCommand;
        controller.objectives = new[] { lakad, kaliwa, kanan, bukas, ilaw };

        // --- Voice UI (mic + replay buttons) ---
        VoiceUIBuilder.BuildVoiceUI(voiceCommand, controller);

        System.IO.Directory.CreateDirectory("Assets/Scenes");
        EditorSceneManager.SaveScene(scene, ScenePath);
        AddToBuildSettings();

        Debug.Log("[SALINLAHI] Bahay Scene 2 built and saved to " + ScenePath + ". " +
                   "Words: Lakad, Kaliwa, Kanan, Bukas, Ilaw. " +
                   (LoadSprite("door_open") == null
                       ? "NOTE: door_open sprite not found - the Door_Open object exists but has no art yet. " +
                         "Import it and re-run this command."
                       : "Door art wired.") +
                   " Point Scene 1's ExitTrigger at this scene to link them.");
    }

    private static SceneObjectiveController.SceneObjective MakeObjective(string word, GameObject prompt)
    {
        return new SceneObjectiveController.SceneObjective
        {
            word = word,
            promptRoot = prompt,
            onCorrect = new UnityEngine.Events.UnityEvent(),
        };
    }

    private static GameObject CreateBackground(string name, string spriteFile, float x)
    {
        GameObject go = new GameObject(name);
        SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = LoadSprite(spriteFile);
        sr.sortingLayerName = "Background";
        sr.sortingOrder = 0;
        go.transform.position = new Vector3(x, 0f, 0f);
        go.transform.localScale = new Vector3(BgScale, BgScale, 1f);
        return go;
    }

    /// <summary>
    /// A plain white 4x4 sprite, used as a tintable quad for the darkness
    /// overlay. Created on first use because Unity 6 no longer ships an
    /// accessible built-in white sprite.
    /// </summary>
    private static Sprite GetOrCreateWhitePixel()
    {
        Sprite existing = AssetDatabase.LoadAssetAtPath<Sprite>(WhitePixelPath);
        if (existing != null) return existing;

        Texture2D tex = new Texture2D(4, 4);
        Color[] pixels = new Color[16];
        for (int i = 0; i < pixels.Length; i++) pixels[i] = Color.white;
        tex.SetPixels(pixels);
        tex.Apply();
        System.IO.File.WriteAllBytes(WhitePixelPath, tex.EncodeToPNG());
        Object.DestroyImmediate(tex);

        AssetDatabase.ImportAsset(WhitePixelPath, ImportAssetOptions.ForceUpdate);
        TextureImporter importer = (TextureImporter)AssetImporter.GetAtPath(WhitePixelPath);
        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Single;
        importer.mipmapEnabled = false;

        // SpriteDrawMode.Sliced (used to stretch this quad over the room)
        // requires a FullRect mesh; the default Tight mesh makes Unity warn
        // and ignore the size.
        TextureImporterSettings settings = new TextureImporterSettings();
        importer.ReadTextureSettings(settings);
        settings.spriteMeshType = SpriteMeshType.FullRect;
        importer.SetTextureSettings(settings);

        importer.SaveAndReimport();

        return AssetDatabase.LoadAssetAtPath<Sprite>(WhitePixelPath);
    }

    private static void AddToBuildSettings()
    {
        var scenes = new System.Collections.Generic.List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
        foreach (var s in scenes) if (s.path == ScenePath) return;
        scenes.Add(new EditorBuildSettingsScene(ScenePath, true));
        EditorBuildSettings.scenes = scenes.ToArray();
    }

    /// <summary>Arrow + badge + word label, matching Level 1's prompts.</summary>
    private static GameObject CreatePromptUI(Transform parent, string name, string word, Vector3 localPos)
    {
        GameObject root = new GameObject(name, typeof(RectTransform));
        root.transform.SetParent(parent, false);
        RectTransform rootRect = root.GetComponent<RectTransform>();
        rootRect.anchoredPosition3D = localPos;
        rootRect.sizeDelta = new Vector2(260, 100);

        GameObject badgeGO = new GameObject("Badge", typeof(RectTransform));
        badgeGO.transform.SetParent(root.transform, false);
        Image badgeImg = badgeGO.AddComponent<Image>();
        badgeImg.sprite = LoadSprite("ui_word_badge");
        badgeGO.GetComponent<RectTransform>().sizeDelta = new Vector2(220, 70);

        GameObject textGO = new GameObject("Text", typeof(RectTransform));
        textGO.transform.SetParent(badgeGO.transform, false);
        Text text = textGO.AddComponent<Text>();
        text.text = word;
        text.font = UIFont.Get();
        text.fontSize = 36;
        text.fontStyle = FontStyle.Bold;
        text.alignment = TextAnchor.MiddleCenter;
        text.color = Color.white;
        RectTransform textRect = textGO.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;

        GameObject arrowGO = new GameObject("Arrow", typeof(RectTransform));
        arrowGO.transform.SetParent(root.transform, false);
        Image arrowImg = arrowGO.AddComponent<Image>();
        arrowImg.sprite = LoadSprite("ui_arrow");
        RectTransform arrowRect = arrowGO.GetComponent<RectTransform>();
        arrowRect.sizeDelta = new Vector2(50, 50);
        arrowRect.anchoredPosition = new Vector2(0, 55);

        root.SetActive(false); // SceneObjectiveController shows the first one
        return root;
    }

    private static Sprite LoadSprite(string spriteFile)
    {
        string path = SpriteFolder + spriteFile + ".png";
        Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
        if (sprite == null)
            Debug.LogWarning("[SALINLAHI] Could not load sprite at '" + path + "'.");
        return sprite;
    }
}
