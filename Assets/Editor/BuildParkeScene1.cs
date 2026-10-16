// Assets/Editor/BuildParkeScene1.cs
//
// One-click scene builder for Parke Scene 1 ("Walking to the Park").
// Teaches five words: Lakad, Takbo, Tingin, Tuwa, Pasok.
//
// Only Tingin and Tuwa are new. The other three are deliberate returns in a
// third setting - Lakad from the bedroom, Takbo from the schoolyard, Pasok
// from the school gate - which is what the design document's reuse column is
// for. Tuwa is also the first word in the game that names a FEELING rather
// than an object or an action, which is why it is the one that changes how
// Kylo stands rather than lighting something up.
//
// LEVEL 3's FIRST SCENE: it starts the run (RunScoreCounter.resetOnStart) and
// fades into Scene 2, the playground, which ends the level and shows the
// result. It held that ending itself until Scene 2 existed.
//
// STAGING. He arrives from the left along the front lawn and comes to a stop
// at the playground, which sits right of centre in the painting.
//
// A NOTE ON THE WALKING, because it constrains the order. PlayerMovement has
// ONE walkTarget, shared by every walk in a scene, and a walk ends the moment
// it is within reach of that target. So exactly one word here can be given a
// destination - Takbo, which has to stop at the playground rather than run
// out of the frame - and any walk AFTER it would end instantly, the target
// already being underfoot. That is why Pasok lights the play area up instead
// of walking into it: the walking is done by then, and he is standing in it.
//
// Sprites expected in Assets/Sprites/:
//   park_background, standing_uniform_with_bag, walk_frame_1..4_uniform_with_bag,
//   celebrate_uniform_with_bag, fx_gleam, ui_arrow, ui_word_badge, badge_parke
//
// Run via: Tools > SALINLAHI > Build Parke Scene 1

using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class BuildParkeScene1
{
    private const string SpriteFolder = "Assets/Sprites/";
    private const string ScenePath = "Assets/Scenes/Chapter3_Level1_Parke.unity";
    private const string NextScene = "Chapter3_Level2_Palaruan";

    private const float ViewHeight = 10.8f;           // camera is orthographic 5.4
    private const float ViewWidth = 19.2f;

    // The near lawn, in front of the swings and the slide. Standing him on the
    // far grass would put him behind the play equipment he is walking towards.
    private const float FloorY = -4.00f;

    // The character art imports at 120 pixels per unit, and its feet sit this
    // far below the pivot - see BuildBahayScene2 for where these come from.
    private const float SpritePPU = 120f;
    // A shade larger than Paaralan Scene 1's 0.45. That was a yard seen across
    // its whole width; here he walks along the front edge of the lawn, nearer
    // the camera than the playground behind him.
    private const float PlayerScale = 0.50f;
    private const float FeetBelowPivot = (743.5f - 745f / 2f) / SpritePPU;
    private const float PlayerFeetY = FloorY + FeetBelowPivot * PlayerScale;

    private const float StartX = -7.00f;              // in from the left, past the bench
    private const float PlaygroundX = 2.20f;          // where "Takbo" stops him
    private const float SlideX = 3.40f;               // the play structure, for Tingin
    private const float SlideY = -2.00f;

    [MenuItem("Tools/SALINLAHI/Build Parke Scene 1")]
    public static void BuildScene()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            Debug.LogError("[SALINLAHI] Stop Play mode first - this rebuilds the scene, which Unity doesn't allow while the game is running.");
            return;
        }

        SpriteImportFixer.Verify(new[]
        {
            "park_background", "standing_uniform_with_bag", "celebrate_uniform_with_bag",
            "fx_gleam", "ui_arrow", "ui_word_badge", "badge_parke",
        }, "Parke Scene 1");

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

        // --- The park, scaled from its own bounds to fill the view ---
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

        // --- Kylo, still in uniform with his bag: this is the walk home ---
        GameObject playerGO = new GameObject("player_character");
        playerGO.tag = "Player";
        playerGO.transform.position = new Vector3(StartX, PlayerFeetY, 0f);
        playerGO.transform.localScale = new Vector3(PlayerScale, PlayerScale, 1f);

        SpriteRenderer playerSr = playerGO.AddComponent<SpriteRenderer>();
        playerSr.sortingLayerName = "Player";
        playerSr.sortingOrder = 0;
        playerSr.sprite = LoadSprite("standing_uniform_with_bag");

        Rigidbody2D playerRb = playerGO.AddComponent<Rigidbody2D>();
        playerRb.gravityScale = 3f;
        playerRb.freezeRotation = true;

        BoxCollider2D playerCol = playerGO.AddComponent<BoxCollider2D>();
        playerCol.size = new Vector2(1.2f, FeetBelowPivot * 2f);

        PlayerMovement player = playerGO.AddComponent<PlayerMovement>();
        player.standingSprite = LoadSprite("standing_uniform_with_bag");
        player.walkFrames = UniformWalkFrames();
        player.currentWakeStage = PlayerMovement.WakeStage.Standing;

        // "Lakad" is one stroll along the lawn, ended by its own clock. The
        // marker below belongs to "Takbo" - see the note at the top of the file
        // about there being only one walkTarget to go round.
        player.voiceWalkDuration = 0.62f;                // -7.0 -> about -4.0

        GameObject spot = new GameObject("PlaygroundSpot");
        spot.transform.position = new Vector3(PlaygroundX, FloorY, 0f);
        player.walkTarget = spot.transform;
        player.walkTargetThreshold = 0.35f;

        VoiceCommand voiceCommand = playerGO.AddComponent<VoiceCommand>();
        voiceCommand.player = player;

        // "Tuwa" is a feeling, so it has to be carried by the character. He
        // throws his arms up - the one pose in the set that reads as delight
        // rather than as speech. The UNIFORM version of it, made by
        // Tools/make_celebrate_uniform.py: the original celebrate pose is from
        // before the uniform existed and still wears the green tee, so he
        // changed clothes the moment he was pleased.
        GameObject cheerGO = new GameObject("Pose_Tuwa");
        PoseSwitch cheer = cheerGO.AddComponent<PoseSwitch>();
        cheer.player = player;
        cheer.standingSprite = LoadSprite("celebrate_uniform_with_bag");

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
        TimedOverlay gleamTingin = CreateGleamAt("Gleam_Tingin", SlideX, SlideY, 4.0f);
        TimedOverlay gleamTuwa = CreateGleamAt("Gleam_Tuwa", PlaygroundX, FloorY + 1.6f, 3.2f);
        TimedOverlay gleamPasok = CreateGleamAt("Gleam_Pasok", 0.80f, -2.40f, 6.4f);

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

        GameObject pLakad = CreatePromptUI(uiRoot.transform, "Prompt_Lakad", "Lakad", new Vector3(-700, -170, 0));
        GameObject pTakbo = CreatePromptUI(uiRoot.transform, "Prompt_Takbo", "Takbo", new Vector3(-380, -170, 0));
        GameObject pTingin = CreatePromptUI(uiRoot.transform, "Prompt_Tingin", "Tingin", new Vector3(330, -20, 0));
        GameObject pTuwa = CreatePromptUI(uiRoot.transform, "Prompt_Tuwa", "Tuwa", new Vector3(220, -150, 0));
        GameObject pPasok = CreatePromptUI(uiRoot.transform, "Prompt_Pasok", "Pasok", new Vector3(60, -250, 0));

        // --- Objectives: walk, run, look, be glad, go in ---
        var lakad = MakeObjective("Lakad", pLakad);
        UnityEventTools.AddPersistentListener(lakad.onCorrect, player.WalkForward);

        // Takbo is a real run, not a relabelled walk: PlayerMovement moves and
        // animates faster for its duration, and stops at the marker rather
        // than after a fixed time.
        var takbo = MakeObjective("Takbo", pTakbo);
        UnityEventTools.AddPersistentListener(takbo.onCorrect, player.Takbo);

        var tingin = MakeObjective("Tingin", pTingin);
        UnityEventTools.AddPersistentListener(tingin.onCorrect, gleamTingin.Play);

        var tuwa = MakeObjective("Tuwa", pTuwa);
        UnityEventTools.AddPersistentListener(tuwa.onCorrect, cheer.Apply);
        UnityEventTools.AddPersistentListener(tuwa.onCorrect, gleamTuwa.Play);

        // The whole lawn lights up rather than a walk - he is already standing
        // in the park by now, and the one walkTarget has been spent on Takbo.
        var pasok = MakeObjective("Pasok", pPasok);
        UnityEventTools.AddPersistentListener(pasok.onCorrect, gleamPasok.Play);

        SceneObjectiveController controller = uiRoot.AddComponent<SceneObjectiveController>();
        controller.voiceCommand = voiceCommand;
        controller.playerMovement = player;
        controller.objectives = new[] { lakad, takbo, tingin, tuwa, pasok };

        VoiceUIBuilder.BuildVoiceUI(voiceCommand, controller);
        // Still needed in the scene: SceneResultPanel looks the fader up at
        // run time to fade out of whichever button is pressed.
        SceneFaderBuilder.Build();

        // --- Level 3's first scene, so it starts the run ---
        GameObject runScoreGO = new GameObject("RunScore");
        RunScoreCounter runScore = runScoreGO.AddComponent<RunScoreCounter>();
        runScore.voiceCommand = voiceCommand;
        runScore.objectiveController = controller;
        runScore.resetOnStart = true;   // a fresh level, so a fresh total

        // --- The scene's summary, whose Next button hands on ---
        // The same panel the Bahay scenes stop on, so a child sees how the
        // scene went instead of the next room arriving unannounced.
        SceneResultPanelBuilder.Build(controller, voiceCommand, NextScene);

        System.IO.Directory.CreateDirectory("Assets/Scenes");
        EditorSceneManager.SaveScene(scene, ScenePath);
        AddToBuildSettings();

        Debug.Log("[SALINLAHI] Parke Scene 1 built and saved to " + ScenePath +
                   ". Words: Lakad, Takbo, Tingin, Tuwa, Pasok - Tingin and Tuwa new. " +
                   "It starts Level 3's run and fades into " + NextScene + ", which ends it - " +
                   "so build Parke Scene 2 as well, or the level runs into a scene that is " +
                   "not there.");
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
