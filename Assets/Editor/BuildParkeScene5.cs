// Assets/Editor/BuildParkeScene5.cs
//
// One-click scene builder for Parke Scene 5 ("Heading Home").
// Teaches five words: Paalam, Lakad, Tingin, Pagod, Uwi.
//
// Paalam, Pagod and Uwi are new. Lakad and Tingin return, and both return
// reversed, which is the quiet point of the scene: Lakad brought him INTO the
// park in Scene 1 and now takes him out of it, and Tingin was looking at the
// playground he was about to run to - here it is looking back at it.
//
// THIS IS THE LAST SCENE OF LEVEL 3, AND THE LAST SCENE IN THE GAME. Nothing
// will take the ending from it later, so unlike every other scene that has
// held it, this one keeps it for good. Building it also means re-running
// Build Parke Scene 4, which now fades into this one.
//
// WHAT EACH WORD DOES:
//
//   Paalam   Kylo turns and waves; his friend lights up
//   Lakad    he walks back towards the gate
//   Tingin   the whole park lights up behind him - a last look
//   Pagod    Kylo lights up: tired, at the end of a long day
//   Uwi      he walks on to the gate, and the game ends on the result panel
//
// Pagod has the same difficulty Ngiti has in Scene 3, for the same reason:
// tiredness is a posture, and the character set has no tired one. It holds
// his standing pose and lights him instead. That is two words in this level
// carried by a glow because the art cannot carry them, and both are honest
// candidates for the first new poses anybody draws.
//
// The banig is still on the grass from Scene 4 - they are leaving, not having
// never been there.
//
// Sprites expected in Assets/Sprites/:
//   park_background, standing_uniform_with_bag, talking_uniform_with_bag,
//   walk_frame_1..4_uniform_with_bag, char_classmate, prop_banig, fx_gleam,
//   ui_arrow, ui_word_badge, badge_parke
//
// Run via: Tools > SALINLAHI > Build Parke Scene 5

using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class BuildParkeScene5
{
    private const string SpriteFolder = "Assets/Sprites/";
    private const string ScenePath = "Assets/Scenes/Chapter3_Level5_Uwi.unity";

    private const float ViewHeight = 10.8f;           // camera is orthographic 5.4
    private const float ViewWidth = 19.2f;

    // Parke's near lawn and Parke's scale, unchanged across all five scenes.
    private const float FloorY = -4.00f;
    private const float SpritePPU = 120f;
    private const float PlayerScale = 0.50f;
    private const float FeetBelowPivot = (743.5f - 745f / 2f) / SpritePPU;
    private const float PlayerFeetY = FloorY + FeetBelowPivot * PlayerScale;

    private const float StartX = -2.00f;
    private const float FriendX = -4.50f;
    private const float FriendHeight = 3.05f;
    private const float BanigX = -0.30f;
    private const float BanigHeight = 1.70f;
    private const float GateX = 7.20f;                // the stone arch, right of frame

    // Two walks, both ended by their own clock. There is deliberately NO
    // walkTarget in this scene: PlayerMovement has only one, and a walk ends
    // the instant it is within reach of it, so a target set for Lakad would
    // make Uwi finish before it started. Durations keep both honest.
    private const float WalkDuration = 0.75f;         // about 3.75 units each

    [MenuItem("Tools/SALINLAHI/Build Parke Scene 5")]
    public static void BuildScene()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            Debug.LogError("[SALINLAHI] Stop Play mode first - this rebuilds the scene, which Unity doesn't allow while the game is running.");
            return;
        }

        SpriteImportFixer.Verify(new[]
        {
            "park_background", "standing_uniform_with_bag", "talking_uniform_with_bag",
            "char_classmate", "prop_banig", "fx_gleam", "ui_arrow", "ui_word_badge",
            "badge_parke",
        }, "Parke Scene 5");

        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        GameObject camGO = new GameObject("Main Camera");
        Camera cam = camGO.AddComponent<Camera>();
        cam.orthographic = true;
        cam.orthographicSize = ViewHeight * 0.5f;
        cam.transform.position = new Vector3(0, 0, -10);
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = Color.black;
        camGO.tag = "MainCamera";
        camGO.AddComponent<AudioListener>();

        GameObject bg = new GameObject("Background_Parke");
        SpriteRenderer bgSr = bg.AddComponent<SpriteRenderer>();
        bgSr.sprite = LoadSprite("park_background");
        bgSr.sortingLayerName = "Background";
        bgSr.sortingOrder = 0;
        if (bgSr.sprite != null && bgSr.sprite.bounds.size.y > 0.0001f)
        {
            float s = Mathf.Max(ViewHeight / bgSr.sprite.bounds.size.y,
                                ViewWidth / bgSr.sprite.bounds.size.x);
            bg.transform.localScale = new Vector3(s, s, 1f);
        }

        CreateProp("Prop_Banig", "prop_banig", BanigX, BanigHeight, FloorY, 3);
        GameObject friend = CreateFigure("Char_Friend", "char_classmate",
                                          FriendX, FloorY, FriendHeight);

        // --- Kylo ---
        GameObject playerGO = new GameObject("player_character");
        playerGO.tag = "Player";
        playerGO.transform.position = new Vector3(StartX, PlayerFeetY, 0f);
        playerGO.transform.localScale = new Vector3(PlayerScale, PlayerScale, 1f);

        SpriteRenderer playerSr = playerGO.AddComponent<SpriteRenderer>();
        playerSr.sprite = LoadSprite("standing_uniform_with_bag");
        playerSr.sortingLayerName = "Player";
        playerSr.sortingOrder = 5;

        Rigidbody2D playerRb = playerGO.AddComponent<Rigidbody2D>();
        playerRb.gravityScale = 3f;
        playerRb.freezeRotation = true;

        BoxCollider2D playerCol = playerGO.AddComponent<BoxCollider2D>();
        playerCol.size = new Vector2(1.2f, FeetBelowPivot * 2f);

        PlayerMovement player = playerGO.AddComponent<PlayerMovement>();
        player.standingSprite = LoadSprite("standing_uniform_with_bag");
        player.walkFrames = UniformWalkFrames();
        player.currentWakeStage = PlayerMovement.WakeStage.Standing;
        player.voiceWalkDuration = WalkDuration;

        VoiceCommand voiceCommand = playerGO.AddComponent<VoiceCommand>();
        voiceCommand.player = player;

        GameObject waveGO = new GameObject("Pose_Wave");
        PoseSwitch wave = waveGO.AddComponent<PoseSwitch>();
        wave.player = player;
        wave.standingSprite = LoadSprite("talking_uniform_with_bag");

        // Puts him back side-on before he walks. Without it the waving pose
        // stays as his standing art and he moonwalks to the gate facing front.
        GameObject walkPoseGO = new GameObject("Pose_Walk");
        PoseSwitch walkPose = walkPoseGO.AddComponent<PoseSwitch>();
        walkPose.player = player;
        walkPose.standingSprite = LoadSprite("standing_uniform_with_bag");
        walkPose.walkFrames = UniformWalkFrames();

        CameraFollow follow = camGO.AddComponent<CameraFollow>();
        follow.target = playerGO.transform;
        follow.offset = new Vector3(0f, 0f, -10f);
        follow.clampHorizontally = true;
        follow.minX = 0f;
        follow.maxX = 0f;
        follow.lockVertically = true;
        follow.fixedY = 0f;

        GameObject groundGO = new GameObject("Ground");
        groundGO.transform.position = new Vector3(0f, FloorY - 0.25f, 0f);
        groundGO.AddComponent<BoxCollider2D>().size = new Vector2(ViewWidth * 2f, 0.5f);

        CreateWall("Wall_Left", -9.3f);
        CreateWall("Wall_Right", 9.3f);

        // --- What lights up ---
        TimedOverlay gleamPaalam = CreateGleamAt("Gleam_Paalam", FriendX, FloorY + 1.6f, 3.4f);
        // Wide: the whole park at once, which is what a last look is.
        TimedOverlay gleamTingin = CreateGleamAt("Gleam_Tingin", -0.50f, -2.40f, 8.0f);
        TimedOverlay gleamPagod = CreateGleamAt("Gleam_Pagod", StartX + 3.75f, FloorY + 1.8f, 2.6f);
        TimedOverlay gleamUwi = CreateGleamAt("Gleam_Uwi", GateX, -1.80f, 3.6f);

        // --- Word prompts ---
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

        GameObject pPaalam = CreatePromptUI(uiRoot.transform, "Prompt_Paalam", "Paalam", new Vector3(-450, -60, 0));
        GameObject pLakad = CreatePromptUI(uiRoot.transform, "Prompt_Lakad", "Lakad", new Vector3(-200, -120, 0));
        GameObject pTingin = CreatePromptUI(uiRoot.transform, "Prompt_Tingin", "Tingin", new Vector3(-50, 60, 0));
        GameObject pPagod = CreatePromptUI(uiRoot.transform, "Prompt_Pagod", "Pagod", new Vector3(175, -40, 0));
        GameObject pUwi = CreatePromptUI(uiRoot.transform, "Prompt_Uwi", "Uwi", new Vector3(720, -110, 0));

        // --- Objectives: wave, walk, look back, be tired, go home ---
        var paalam = MakeObjective("Paalam", pPaalam);
        UnityEventTools.AddPersistentListener(paalam.onCorrect, wave.Apply);
        UnityEventTools.AddPersistentListener(paalam.onCorrect, gleamPaalam.Play);

        var lakad = MakeObjective("Lakad", pLakad);
        UnityEventTools.AddPersistentListener(lakad.onCorrect, walkPose.Apply);
        UnityEventTools.AddPersistentListener(lakad.onCorrect, player.WalkForward);

        var tingin = MakeObjective("Tingin", pTingin);
        UnityEventTools.AddPersistentListener(tingin.onCorrect, gleamTingin.Play);

        // Holds his standing pose - see the note at the top about there being
        // no tired one.
        var pagod = MakeObjective("Pagod", pPagod);
        UnityEventTools.AddPersistentListener(pagod.onCorrect, gleamPagod.Play);

        var uwi = MakeObjective("Uwi", pUwi);
        UnityEventTools.AddPersistentListener(uwi.onCorrect, player.WalkForward);
        UnityEventTools.AddPersistentListener(uwi.onCorrect, gleamUwi.Play);

        SceneObjectiveController controller = uiRoot.AddComponent<SceneObjectiveController>();
        controller.voiceCommand = voiceCommand;
        controller.playerMovement = player;
        controller.objectives = new[] { paalam, lakad, tingin, pagod, uwi };

        VoiceUIBuilder.BuildVoiceUI(voiceCommand, controller);
        SceneFaderBuilder.Build();

        // --- The last scene of the last level ---
        DatabaseDemoBuilder.Build("Parke - Level 3", "LevelSelect", 3, "badge_parke");

        System.IO.Directory.CreateDirectory("Assets/Scenes");
        EditorSceneManager.SaveScene(scene, ScenePath);
        AddToBuildSettings();

        Debug.Log("[SALINLAHI] Parke Scene 5 built and saved to " + ScenePath +
                   ". Words: Paalam, Lakad, Tingin, Pagod, Uwi - Paalam, Pagod and Uwi new. " +
                   "This is the last scene of the last level, so re-run Build Parke Scene 4 " +
                   "as well - it fades into this one instead of showing the result itself.");
    }

    private static GameObject CreateFigure(string name, string spriteFile,
                                            float x, float feetY, float worldHeight)
    {
        GameObject go = new GameObject(name);
        SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = LoadSprite(spriteFile);
        sr.sortingLayerName = "Player";
        sr.sortingOrder = 4;

        float s = 1f;
        if (sr.sprite != null && sr.sprite.bounds.size.y > 0.0001f)
            s = worldHeight / sr.sprite.bounds.size.y;
        go.transform.localScale = new Vector3(s, s, 1f);
        go.transform.position = new Vector3(x, feetY + worldHeight * 0.5f, 0f);
        return go;
    }

    private static GameObject CreateProp(string name, string spriteFile, float x,
                                          float worldHeight, float baseY, int order)
    {
        GameObject go = new GameObject(name);
        SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = LoadSprite(spriteFile);
        sr.sortingLayerName = "Player";
        sr.sortingOrder = order;

        float s = 1f;
        if (sr.sprite != null && sr.sprite.bounds.size.y > 0.0001f)
            s = worldHeight / sr.sprite.bounds.size.y;
        go.transform.localScale = new Vector3(s, s, 1f);
        go.transform.position = new Vector3(x, baseY + worldHeight * 0.5f, 0f);
        return go;
    }

    private static TimedOverlay CreateGleamAt(string name, float x, float y, float worldSize)
    {
        GameObject go = new GameObject(name);
        go.transform.position = new Vector3(x, y, 0f);
        SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = LoadSprite("fx_gleam");
        sr.sortingLayerName = "Props";
        sr.sortingOrder = 40;
        sr.color = new Color(1f, 1f, 1f, 0f);

        float s = 1f;
        if (sr.sprite != null && sr.sprite.bounds.size.y > 0.0001f)
            s = worldSize / sr.sprite.bounds.size.y;
        go.transform.localScale = new Vector3(s, s, 1f);

        TimedOverlay fx = go.AddComponent<TimedOverlay>();
        fx.overlay = sr;
        fx.peakAlpha = 0.9f;
        fx.fadeInDuration = 0.25f;
        fx.holdDuration = 0.5f;
        fx.fadeOutDuration = 0.6f;
        return fx;
    }

    private static Sprite[] UniformWalkFrames()
    {
        return new[]
        {
            LoadSprite("walk_frame_1_uniform_with_bag"), LoadSprite("walk_frame_2_uniform_with_bag"),
            LoadSprite("walk_frame_3_uniform_with_bag"), LoadSprite("walk_frame_4_uniform_with_bag"),
        };
    }

    private static void CreateWall(string name, float x)
    {
        GameObject go = new GameObject(name);
        go.transform.position = new Vector3(x, 0f, 0f);
        go.AddComponent<BoxCollider2D>().size = new Vector2(1f, 20f);
    }

    private static SceneObjectiveController.SceneObjective MakeObjective(string word, GameObject prompt)
    {
        return new SceneObjectiveController.SceneObjective
        {
            word = word,
            promptRoot = prompt,
            instructionClip = VoiceClipLibrary.ForWord(word),
            onCorrect = new UnityEngine.Events.UnityEvent(),
        };
    }

    private static void AddToBuildSettings()
    {
        var scenes = new System.Collections.Generic.List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
        foreach (var s in scenes) if (s.path == ScenePath) return;
        scenes.Add(new EditorBuildSettingsScene(ScenePath, true));
        EditorBuildSettings.scenes = scenes.ToArray();
    }

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

        root.SetActive(false);
        return root;
    }

    private static Sprite LoadSprite(string spriteFile)
    {
        string path = SpriteFolder + spriteFile + ".png";
        Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
        if (sprite == null) Debug.LogWarning("[SALINLAHI] Could not load sprite at " + path);
        return sprite;
    }
}
