// Assets/Editor/BuildParkeScene3.cs
//
// One-click scene builder for Parke Scene 3 ("Meeting New Friends").
// Teaches five words: Kumusta, Ngiti, Tanong, Laro, Bati.
//
// Ngiti and Bati are new. Kumusta, Tanong and Laro return from the school
// gate, the vocabulary lesson and recess - and they return CHANGED, which is
// the point of them being here. Kumusta at the gate was said to a guard on
// duty; here it is said to a stranger his own age. Tanong in the lesson was
// putting a hand up; here it is asking somebody their name.
//
// A MIDDLE SCENE OF LEVEL 3. Scene 2 fades into this one; this one fades into
// Scene 4, the picnic, which ends the level.
//
// THE WIDE SHOT IS BACK. Scene 2 had to zoom to 2.8 because its words were
// about equipment painted as distant scenery. This scene is two children
// standing on the near lawn talking to each other, so the ordinary
// orthographic 5.4 frames it and both of them are large enough to read.
//
// WHAT EACH WORD DOES:
//
//   Kumusta   Kylo turns to greet him; the other boy lights up
//   Ngiti     Kylo lights up, still in the greeting pose - he is smiling
//   Tanong    the other boy lights up: he is the one being asked
//   Laro      BOTH throw their arms up - they are playing
//   Bati      both settle back, and the space between them lights up
//
// Ngiti is the weakest of the five and it is worth being plain about why. A
// smile is a face, and the character set has no face that is only a smile -
// its one smiling pose is the talking pose, already spent on Kumusta a word
// earlier. So Ngiti holds that pose and lights Kylo instead. A drawn smiling
// pose would fix it; nothing in code will.
//
// THE OTHER BOY is the recoloured classmate from Paaralan's recess, made by
// Tools/make_classmate.py - now in two poses, greeting and cheering, so he is
// the same child in both rather than two different-looking ones.
//
// Sprites expected in Assets/Sprites/:
//   park_background, standing_uniform_with_bag, talking_uniform_with_bag,
//   celebrate_uniform_with_bag, walk_frame_1..4_uniform_with_bag,
//   char_classmate, char_classmate_cheer, fx_gleam, ui_arrow, ui_word_badge,
//   badge_parke
//
// Run via: Tools > SALINLAHI > Build Parke Scene 3

using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class BuildParkeScene3
{
    private const string SpriteFolder = "Assets/Sprites/";
    private const string ScenePath = "Assets/Scenes/Chapter3_Level3_Kaibigan.unity";
    private const string NextScene = "Chapter3_Level4_Piknik";

    private const float ViewHeight = 10.8f;           // camera is orthographic 5.4
    private const float ViewWidth = 19.2f;

    // Scene 1's near lawn and Scene 1's scale - the two scenes are the same
    // shot of the same park, so they should not differ.
    private const float FloorY = -4.00f;
    private const float SpritePPU = 120f;
    private const float PlayerScale = 0.50f;
    private const float FeetBelowPivot = (743.5f - 745f / 2f) / SpritePPU;
    private const float PlayerFeetY = FloorY + FeetBelowPivot * PlayerScale;

    private const float KyloX = -2.30f;
    private const float FriendX = 1.70f;
    private const float FriendHeight = 3.05f;
    // The cheering art is 880 px tall against the greeting pose's 745, because
    // his arms are over his head. Sizing BOTH to the same world height would
    // shrink his body when he cheers, so the cheer is given the taller figure
    // that keeps the same scale: 3.05 x 880 / 745.
    private const float FriendCheerHeight = FriendHeight * (880f / 745f);

    [MenuItem("Tools/SALINLAHI/Build Parke Scene 3")]
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
            "celebrate_uniform_with_bag", "char_classmate", "char_classmate_cheer",
            "fx_gleam", "ui_arrow", "ui_word_badge", "badge_parke",
        }, "Parke Scene 3");

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

        // --- The other boy, in his two poses ---
        GameObject friend = CreateFigure("Char_Friend", "char_classmate",
                                          FriendX, FloorY, FriendHeight);
        GameObject friendCheer = CreateFigure("Char_Friend_Cheer", "char_classmate_cheer",
                                               FriendX, FloorY, FriendCheerHeight);
        friendCheer.SetActive(false);

        // --- Kylo ---
        GameObject playerGO = new GameObject("player_character");
        playerGO.tag = "Player";
        playerGO.transform.position = new Vector3(KyloX, PlayerFeetY, 0f);
        playerGO.transform.localScale = new Vector3(PlayerScale, PlayerScale, 1f);

        SpriteRenderer playerSr = playerGO.AddComponent<SpriteRenderer>();
        playerSr.sprite = LoadSprite("standing_uniform_with_bag");
        playerSr.sortingLayerName = "Player";
        playerSr.sortingOrder = 0;

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

        // PoseSwitch rather than separate figures for Kylo, so the cheering
        // art inherits his scale instead of needing its own height worked out.
        GameObject greetGO = new GameObject("Pose_Greet");
        PoseSwitch greet = greetGO.AddComponent<PoseSwitch>();
        greet.player = player;
        greet.standingSprite = LoadSprite("talking_uniform_with_bag");

        GameObject cheerGO = new GameObject("Pose_Cheer");
        PoseSwitch cheer = cheerGO.AddComponent<PoseSwitch>();
        cheer.player = player;
        cheer.standingSprite = LoadSprite("celebrate_uniform_with_bag");

        GameObject groundGO = new GameObject("Ground");
        groundGO.transform.position = new Vector3(0f, FloorY - 0.25f, 0f);
        groundGO.AddComponent<BoxCollider2D>().size = new Vector2(ViewWidth * 2f, 0.5f);

        CreateWall("Wall_Left", -9.3f);
        CreateWall("Wall_Right", 9.3f);

        // --- What lights up ---
        TimedOverlay gleamKumusta = CreateGleamAt("Gleam_Kumusta", FriendX, FloorY + 1.6f, 3.4f);
        TimedOverlay gleamNgiti = CreateGleamAt("Gleam_Ngiti", KyloX, FloorY + 2.4f, 2.2f);
        TimedOverlay gleamTanong = CreateGleamAt("Gleam_Tanong", FriendX, FloorY + 2.4f, 2.4f);
        TimedOverlay gleamLaro = CreateGleamAt("Gleam_Laro", (KyloX + FriendX) * 0.5f,
                                                FloorY + 1.8f, 6.2f);
        // Between them, where two hands would meet.
        TimedOverlay gleamBati = CreateGleamAt("Gleam_Bati", (KyloX + FriendX) * 0.5f,
                                                FloorY + 2.0f, 2.2f);

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

        GameObject pKumusta = CreatePromptUI(uiRoot.transform, "Prompt_Kumusta", "Kumusta", new Vector3(170, -60, 0));
        GameObject pNgiti = CreatePromptUI(uiRoot.transform, "Prompt_Ngiti", "Ngiti", new Vector3(-230, 30, 0));
        GameObject pTanong = CreatePromptUI(uiRoot.transform, "Prompt_Tanong", "Tanong", new Vector3(170, 40, 0));
        GameObject pLaro = CreatePromptUI(uiRoot.transform, "Prompt_Laro", "Laro", new Vector3(-30, 150, 0));
        GameObject pBati = CreatePromptUI(uiRoot.transform, "Prompt_Bati", "Bati", new Vector3(-30, -20, 0));

        // --- Objectives: greet, smile, ask, play, high-five ---
        var kumusta = MakeObjective("Kumusta", pKumusta);
        UnityEventTools.AddPersistentListener(kumusta.onCorrect, greet.Apply);
        UnityEventTools.AddPersistentListener(kumusta.onCorrect, gleamKumusta.Play);

        // Holds the greeting pose - see the note at the top about why there is
        // no smiling pose of its own.
        var ngiti = MakeObjective("Ngiti", pNgiti);
        UnityEventTools.AddPersistentListener(ngiti.onCorrect, gleamNgiti.Play);

        var tanong = MakeObjective("Tanong", pTanong);
        UnityEventTools.AddPersistentListener(tanong.onCorrect, gleamTanong.Play);

        var laro = MakeObjective("Laro", pLaro);
        UnityEventTools.AddPersistentListener(laro.onCorrect, cheer.Apply);
        UnityEventTools.AddBoolPersistentListener(laro.onCorrect, friend.SetActive, false);
        UnityEventTools.AddBoolPersistentListener(laro.onCorrect, friendCheer.SetActive, true);
        UnityEventTools.AddPersistentListener(laro.onCorrect, gleamLaro.Play);

        var bati = MakeObjective("Bati", pBati);
        UnityEventTools.AddPersistentListener(bati.onCorrect, greet.Apply);
        UnityEventTools.AddBoolPersistentListener(bati.onCorrect, friendCheer.SetActive, false);
        UnityEventTools.AddBoolPersistentListener(bati.onCorrect, friend.SetActive, true);
        UnityEventTools.AddPersistentListener(bati.onCorrect, gleamBati.Play);

        SceneObjectiveController controller = uiRoot.AddComponent<SceneObjectiveController>();
        controller.voiceCommand = voiceCommand;
        controller.playerMovement = player;
        controller.objectives = new[] { kumusta, ngiti, tanong, laro, bati };

        VoiceUIBuilder.BuildVoiceUI(voiceCommand, controller);
        // Still needed in the scene: SceneResultPanel looks the fader up at
        // run time to fade out of whichever button is pressed.
        SceneFaderBuilder.Build();

        // --- A middle scene of Level 3 now: Scene 4 ends it ---
        // The counter is added by hand, because DatabaseDemoBuilder used to
        // bring one with the result panel and that panel has moved on.
        GameObject runScoreGO = new GameObject("RunScore");
        RunScoreCounter runScore = runScoreGO.AddComponent<RunScoreCounter>();
        runScore.voiceCommand = voiceCommand;
        runScore.objectiveController = controller;
        runScore.resetOnStart = false;   // Scene 1 starts the run

        // --- The scene's summary, whose Next button hands on ---
        // The same panel the Bahay scenes stop on, so a child sees how the
        // scene went instead of the next room arriving unannounced.
        SceneResultPanelBuilder.Build(controller, voiceCommand, NextScene);

        System.IO.Directory.CreateDirectory("Assets/Scenes");
        EditorSceneManager.SaveScene(scene, ScenePath);
        AddToBuildSettings();

        Debug.Log("[SALINLAHI] Parke Scene 3 built and saved to " + ScenePath +
                   ". Words: Kumusta, Ngiti, Tanong, Laro, Bati - Ngiti and Bati new. " +
                   "It fades into " + NextScene + ", which ends the level - so build Parke " +
                   "Scene 4 as well, or the level runs into a scene that is not there.");
    }

    private static GameObject CreateFigure(string name, string spriteFile,
                                            float x, float feetY, float worldHeight)
    {
        GameObject go = new GameObject(name);
        SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = LoadSprite(spriteFile);
        sr.sortingLayerName = "Player";
        sr.sortingOrder = 0;

        float s = 1f;
        if (sr.sprite != null && sr.sprite.bounds.size.y > 0.0001f)
            s = worldHeight / sr.sprite.bounds.size.y;
        go.transform.localScale = new Vector3(s, s, 1f);
        go.transform.position = new Vector3(x, feetY + worldHeight * 0.5f, 0f);
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
