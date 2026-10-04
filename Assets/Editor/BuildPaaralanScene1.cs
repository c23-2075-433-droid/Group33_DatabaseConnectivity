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
//   paaralan_background, standing_uniform_with_bag,
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
    private const float FloorY = -2.70f;              // the path across the schoolyard

    // The character art imports at 120 pixels per unit, and its feet sit this
    // far below the pivot - see BuildBahayScene2 for where these come from.
    private const float SpritePPU = 120f;
    private const float PlayerScale = 0.83f;
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

        // --- Player, arriving at the gate on the left ---
        GameObject playerGO = new GameObject("player_character");
        playerGO.tag = "Player";
        playerGO.transform.position = new Vector3(-6.5f, PlayerFeetY, 0f);
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

        VoiceCommand voiceCommand = playerGO.AddComponent<VoiceCommand>();
        voiceCommand.player = player;

        // "Kumusta" turns him to face the child and wave. A greeting has
        // nothing to pick up and nothing to light up, so he is the feedback.
        GameObject greetGO = new GameObject("Pose_Greet");
        PoseSwitch greet = greetGO.AddComponent<PoseSwitch>();
        greet.player = player;
        greet.standingSprite = LoadSprite("talking_uniform_with_bag");

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

        GameObject pKumusta = CreatePromptUI(uiRoot.transform, "Prompt_Kumusta", "Kumusta", new Vector3(-650, 300, 0));
        GameObject pPasok   = CreatePromptUI(uiRoot.transform, "Prompt_Pasok",   "Pasok",   new Vector3(-300, 300, 0));
        GameObject pTakbo   = CreatePromptUI(uiRoot.transform, "Prompt_Takbo",   "Takbo",   new Vector3(200, 300, 0));

        // --- Objectives: greet, go in, hurry to class ---
        var kumusta = MakeObjective("Kumusta", pKumusta);
        UnityEventTools.AddPersistentListener(kumusta.onCorrect, greet.Apply);

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
                   ". Words: Kumusta, Pasok, Takbo. Finishing it earns the Paaralan badge " +
                   "and unlocks Parke. It is Level 2's only scene for now, so it both starts " +
                   "and ends the run - when Scene 2 exists, move the database demo there.");
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
