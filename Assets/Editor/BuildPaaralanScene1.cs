// Assets/Editor/BuildPaaralanScene1.cs
//
// One-click scene builder for Paaralan Scene 1 ("Arriving at School").
// Teaches three words: Kumusta, Pasok, Takbo.
//
// This is Level 2's FIRST scene and, for now, its only one - so it both
// starts the run (RunScoreCounter.resetOnStart) and ends it (the database
// demo, the Paaralan badge, unlocking Parke). When Scene 2 is built, move the
// database demo on to whichever scene becomes last and set resetOnStart false
// there, exactly as Bahay's scenes do.
//
// Kylo arrives already in his school uniform with his bag, carried over from
// the end of Bahay. The three words are the arrival in order: greet, go in,
// hurry to class.
//
// The background is scaled from its own bounds rather than by a fixed number,
// so it fills the camera whatever resolution and pixels-per-unit the art
// imports at. Bahay's scenes each hardcode a scale and two of them had to be
// corrected when the art turned out not to be 100 PPU.
//
// Sprites expected in Assets/Sprites/:
//   paaralan_background, char_guard, fx_gleam, standing_uniform_with_bag,
//   walk_frame_1..4_uniform_with_bag, talking_uniform_with_bag,
//   ui_arrow, ui_word_badge, badge_paaralan
//
// Run via: Tools > SALINLAHI > Build Paaralan Scene 1

using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class BuildPaaralanScene1
{
    private const string SpriteFolder = "Assets/Sprites/";
    private const string ScenePath = "Assets/Scenes/Chapter2_Level1_Paaralan.unity";

    private const float ViewHeight = 10.8f;           // camera is orthographic 5.4
    private const float ViewWidth = 19.2f;
    private const float FloorY = -3.20f;              // the paved yard inside the gate

    // Where the path meets the school steps in paaralan_background, measured
    // off the art against the gate and the guard. "Takbo" ends here.
    private const float SchoolEntranceX = -1.10f;

    // The character art imports at 120 pixels per unit, and its feet sit this
    // far below the pivot - see BuildBahayScene2 for where these come from.
    private const float SpritePPU = 120f;
    // Smaller than Bahay's 0.83 on purpose. This is a wide establishing shot
    // across a schoolyard, not a room you are standing in: at 0.83 Kylo is
    // taller than the gate and nearly as tall as the school building. 0.45
    // puts him at 2.8 units, which reads correctly against both.
    private const float PlayerScale = 0.45f;
    private const float FeetBelowPivot = (743.5f - 745f / 2f) / SpritePPU;
    private const float PlayerFeetY = FloorY + FeetBelowPivot * PlayerScale;

    [MenuItem("Tools/SALINLAHI/Build Paaralan Scene 1")]
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
        cam.orthographicSize = ViewHeight * 0.5f;
        cam.transform.position = new Vector3(0, 0, -10);
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = Color.black;
        camGO.tag = "MainCamera";
        camGO.AddComponent<AudioListener>();

        // --- The schoolyard, scaled from its own bounds to fill the view ---
        GameObject bg = new GameObject("Background_School");
        SpriteRenderer bgSr = bg.AddComponent<SpriteRenderer>();
        bgSr.sprite = LoadSprite("paaralan_background");
        bgSr.sortingLayerName = "Background";
        bgSr.sortingOrder = 0;
        if (bgSr.sprite != null && bgSr.sprite.bounds.size.y > 0.0001f)
        {
            // Cover the view: whichever axis is short decides the scale, so
            // there is never a black band down a side.
            float s = Mathf.Max(ViewHeight / bgSr.sprite.bounds.size.y,
                                ViewWidth / bgSr.sprite.bounds.size.x);
            bg.transform.localScale = new Vector3(s, s, 1f);
        }

        // --- The guard on the gate ---
        // Kumusta needs someone to greet. Without him the child is told to
        // say hello to an empty yard, which teaches the word in a vacuum.
        // Sized against Kylo: an adult at 3.9 units to his 2.8.
        GameObject guard = CreateStanding("Char_Guard", "char_guard", -4.3f, 3.90f);

        // --- Player, arriving at the gate on the left ---
        // Not further left than this: the art puts planters and a raised kerb
        // against the wall out there, and they are painted nearer the camera
        // than the ground the gate stands on, so a character placed over them
        // reads as standing on top of the pots rather than on the path.
        GameObject playerGO = new GameObject("player_character");
        playerGO.tag = "Player";
        playerGO.transform.position = new Vector3(-6.1f, PlayerFeetY, 0f);
        playerGO.transform.localScale = new Vector3(PlayerScale, PlayerScale, 1f);

        SpriteRenderer playerSr = playerGO.AddComponent<SpriteRenderer>();
        playerSr.sortingLayerName = "Player";
        // Already dressed: Bahay ended with him in uniform, carrying his bag.
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

        // The journey across this yard is two words long, so each one has to
        // cover its own half of it. "Pasok" is a step through the gate, not the
        // whole walk to the building - at the default 1.2s it carried Kylo all
        // the way to the door and left "Takbo" with nowhere to run but past the
        // flagpole and into the right-hand wall.
        player.voiceWalkDuration = 0.6f;                 // through the gate: -7.0 -> -4.0

        // "Takbo" then runs the rest, stopping at the foot of the steps rather
        // than after a fixed time. Marker rather than a number so moving the
        // building in the art means moving this, not re-deriving a duration.
        GameObject entrance = new GameObject("SchoolEntrance");
        entrance.transform.position = new Vector3(SchoolEntranceX, FloorY, 0f);
        player.walkTarget = entrance.transform;
        player.walkTargetThreshold = 0.35f;

        VoiceCommand voiceCommand = playerGO.AddComponent<VoiceCommand>();
        voiceCommand.player = player;

        // "Kumusta" turns him to face the child and wave. A greeting has
        // nothing to pick up and nothing to light up, so he is the feedback.
        GameObject greetGO = new GameObject("Pose_Greet");
        PoseSwitch greet = greetGO.AddComponent<PoseSwitch>();
        greet.player = player;
        greet.standingSprite = LoadSprite("talking_uniform_with_bag");

        // The guard lights up when he is greeted, so the child can see the
        // hello land on someone rather than just watching Kylo wave.
        TimedOverlay guardGleam = CreateGleam("Gleam_Guard", guard, 4.4f);

        // "Pasok" turns him side-on again before he walks in.
        GameObject walkPoseGO = new GameObject("Pose_Walk");
        PoseSwitch facingWalk = walkPoseGO.AddComponent<PoseSwitch>();
        facingWalk.player = player;
        facingWalk.standingSprite = LoadSprite("standing_uniform_with_bag");
        facingWalk.walkFrames = UniformWalkFrames();

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

        // Kylo's head reaches y=-0.4 at this scale, so the prompts sit just
        // above that rather than up in the treetops.
        GameObject pKumusta = CreatePromptUI(uiRoot.transform, "Prompt_Kumusta", "Kumusta", new Vector3(-650, 80, 0));
        GameObject pPasok   = CreatePromptUI(uiRoot.transform, "Prompt_Pasok",   "Pasok",   new Vector3(-300, 80, 0));
        GameObject pTakbo   = CreatePromptUI(uiRoot.transform, "Prompt_Takbo",   "Takbo",   new Vector3(250, 80, 0));

        // --- Objectives: greet, go in, hurry to class ---
        var kumusta = MakeObjective("Kumusta", pKumusta);
        UnityEventTools.AddPersistentListener(kumusta.onCorrect, greet.Apply);
        UnityEventTools.AddPersistentListener(kumusta.onCorrect, guardGleam.Play);

        var pasok = MakeObjective("Pasok", pPasok);
        UnityEventTools.AddPersistentListener(pasok.onCorrect, facingWalk.Apply);
        UnityEventTools.AddPersistentListener(pasok.onCorrect, player.WalkForward);

        // Takbo is a real run, not a relabelled walk: PlayerMovement moves and
        // animates faster for its duration, so it needs no separate art.
        var takbo = MakeObjective("Takbo", pTakbo);
        UnityEventTools.AddPersistentListener(takbo.onCorrect, player.Takbo);

        SceneObjectiveController controller = uiRoot.AddComponent<SceneObjectiveController>();
        controller.voiceCommand = voiceCommand;
        controller.objectives = new[] { kumusta, pasok, takbo };

        VoiceUIBuilder.BuildVoiceUI(voiceCommand, controller);
        SceneFaderBuilder.Build();

        // --- Level 2's only scene, so it both starts and ends the run ---
        GameObject runScoreGO = new GameObject("RunScore");
        RunScoreCounter runScore = runScoreGO.AddComponent<RunScoreCounter>();
        runScore.voiceCommand = voiceCommand;
        runScore.resetOnStart = true;   // a fresh level, so a fresh total

        DatabaseDemoBuilder.Build("Paaralan - Level 2", "LevelSelect", 2, "badge_paaralan");

        System.IO.Directory.CreateDirectory("Assets/Scenes");
        EditorSceneManager.SaveScene(scene, ScenePath);
        AddToBuildSettings();

        Debug.Log("[SALINLAHI] Paaralan Scene 1 built and saved to " + ScenePath +
                   ". Words: Kumusta, Pasok, Takbo, greeting the guard on the gate. " +
                   "Finishing it earns the Paaralan badge " +
                   "and unlocks Parke. It is Level 2's only scene for now, so it both starts " +
                   "and ends the run - when Scene 2 exists, move the database demo there.");
    }

    /// <summary>
    /// A character or object standing on the floor, sized by the world height
    /// it should be rather than by a scale factor, so the art's resolution
    /// does not decide how big it looks.
    /// </summary>
    private static GameObject CreateStanding(string name, string spriteFile, float x, float worldHeight)
    {
        GameObject go = new GameObject(name);
        SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = LoadSprite(spriteFile);
        sr.sortingLayerName = "Props";
        sr.sortingOrder = 5;

        float s = 1f;
        if (sr.sprite != null && sr.sprite.bounds.size.y > 0.0001f)
            s = worldHeight / sr.sprite.bounds.size.y;
        go.transform.localScale = new Vector3(s, s, 1f);
        go.transform.position = new Vector3(x, FloorY + worldHeight * 0.5f, 0f);
        return go;
    }

    /// <summary>A gleam centred on something, for a word that points at it.</summary>
    private static TimedOverlay CreateGleam(string name, GameObject target, float worldSize)
    {
        GameObject go = new GameObject(name);
        go.transform.position = target.transform.position;
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

    private static void CreateWall(string name, float x)
    {
        GameObject go = new GameObject(name);
        go.transform.position = new Vector3(x, 0f, 0f);
        go.AddComponent<BoxCollider2D>().size = new Vector2(1f, 20f);
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
        if (sprite == null)
            Debug.LogWarning("[SALINLAHI] Could not load sprite at '" + path + "'.");
        return sprite;
    }
}
