// Assets/Editor/BuildParkeScene2.cs
//
// One-click scene builder for Parke Scene 2 ("Playground").
// Teaches five words: Akyat, Dausdos, Sakay, Tulak, Talon.
//
// All five are NEW. Together with Scene 1 that takes Parke to seven new words
// and the game past three quarters of its vocabulary.
//
// A MIDDLE SCENE OF LEVEL 3. Scene 1 fades into this one; this one fades into
// Scene 3, which ends the level. It held that ending until Scene 3 existed.
//
// THE CAMERA IS ZOOMED IN, and that is the whole reason this scene works.
// park_background paints the swings and the slide as DISTANT scenery: the
// swing frame measures 1.1 world units from its top bar to the grass, so a
// correctly sized child standing at it is about 0.6 units - six per cent of
// the screen at the usual orthographic 5.4, far too small to read and far too
// small to tell which word he just obeyed. So this scene drops the camera to
// 2.8, which is as tight as it goes while still holding BOTH the swings at
// x -5.2 and the slide tower at x +4.2 in one frame. Scene 1 keeps the wide shot,
// because there he is crossing the whole park rather than using it.
//
// There is no CameraFollow here for the same reason. A camera that tracked
// him would swing between the slide and the swings and lose the framing that
// makes the scene legible.
//
// HE HAS PUT HIS BAG DOWN. Scene 1 ends with it on his back; a child does not
// go down a slide wearing one. It lies on the grass for the whole scene,
// which also lets every pose here use the no-bag uniform - including the
// seated one, which has no bagged version.
//
// WHAT EACH WORD DOES:
//
//   Akyat     he appears at the top of the slide; the ladder lights up
//   Dausdos   he appears at the foot of it; the chute lights up
//   Sakay     he is on the swing, seated
//   Tulak     the swing lights up harder - the push that sets it going
//   Talon     he is back on the grass, and actually jumps
//
// Talon is the only one of the five with real physics behind it:
// PlayerMovement.Jump() is the same hop the Space bar performs, so he leaves
// the ground properly rather than being swapped for a picture of jumping.
// That is why the figure on the grass is the real player object and the three
// at the equipment are plain sprites switched on and off.
//
// Sprites expected in Assets/Sprites/:
//   park_background, standing_uniform, sitting_desk_uniform, prop_bag,
//   walk_frame_1..4_uniform, fx_gleam, ui_arrow, ui_word_badge, badge_parke
//
// Run via: Tools > SALINLAHI > Build Parke Scene 2

using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class BuildParkeScene2
{
    private const string SpriteFolder = "Assets/Sprites/";
    private const string ScenePath = "Assets/Scenes/Chapter3_Level2_Palaruan.unity";
    private const string NextScene = "Chapter3_Level3_Kaibigan";

    // Zoomed in on the equipment band - see the note at the top of the file.
    private const float OrthoSize = 2.80f;
    private const float CamX = -0.50f, CamY = -1.10f;

    // The background is still scaled to cover the full 19.2 x 10.8 view, so a
    // tighter camera simply shows less of it. These two stay at the wide-shot
    // values for that reason.
    private const float FullViewHeight = 10.8f;
    private const float FullViewWidth = 19.2f;

    // The grass line where the equipment stands, measured off the painting.
    private const float GroundY = -1.45f;

    // Deliberately a little larger than the painting's own scale. At the
    // equipment's true depth a child is 0.6 units; 0.80 keeps him shorter than
    // the swing frame, which is what matters, while staying legible on a phone.
    private const float KyloHeight = 0.92f;
    private const float SeatedHeight = 0.70f;

    private const float GrassX = -1.20f;              // where he stands and jumps
    private const float SlideTopX = 3.40f;
    private const float SlideTopY = -0.44f;           // the tower deck
    private const float SlideFootX = 1.80f;
    private const float LadderX = 3.65f;
    private const float ChuteX = 2.47f;
    private const float SwingX = -4.03f;      // the RED seat; the blue one is at -5.1
    private const float SwingSeatY = -1.30f;  // so his hip lands on the seat at -0.99
    private const float BagX = -2.30f;

    [MenuItem("Tools/SALINLAHI/Build Parke Scene 2")]
    public static void BuildScene()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            Debug.LogError("[SALINLAHI] Stop Play mode first - this rebuilds the scene, which Unity doesn't allow while the game is running.");
            return;
        }

        SpriteImportFixer.Verify(new[]
        {
            "park_background", "standing_uniform", "sitting_desk_uniform",
            "prop_bag", "fx_gleam", "ui_arrow", "ui_word_badge", "badge_parke",
        }, "Parke Scene 2");

        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        GameObject camGO = new GameObject("Main Camera");
        Camera cam = camGO.AddComponent<Camera>();
        cam.orthographic = true;
        cam.orthographicSize = OrthoSize;
        cam.transform.position = new Vector3(CamX, CamY, -10);
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = Color.black;
        camGO.tag = "MainCamera";
        camGO.AddComponent<AudioListener>();
        // No CameraFollow on purpose - a tracking camera would lose the framing.

        GameObject bg = new GameObject("Background_Parke");
        SpriteRenderer bgSr = bg.AddComponent<SpriteRenderer>();
        bgSr.sprite = LoadSprite("park_background");
        bgSr.sortingLayerName = "Background";
        bgSr.sortingOrder = 0;
        if (bgSr.sprite != null && bgSr.sprite.bounds.size.y > 0.0001f)
        {
            float s = Mathf.Max(FullViewHeight / bgSr.sprite.bounds.size.y,
                                FullViewWidth / bgSr.sprite.bounds.size.x);
            bg.transform.localScale = new Vector3(s, s, 1f);
        }

        // --- His bag, down on the grass for the whole scene ---
        CreateProp("Prop_Bag", "prop_bag", BagX, 0.30f, GroundY, true);

        // --- The real player: on the grass, and the only one that can jump ---
        GameObject playerGO = new GameObject("player_character");
        playerGO.tag = "Player";
        playerGO.transform.position = new Vector3(GrassX, GroundY, 0f);

        SpriteRenderer playerSr = playerGO.AddComponent<SpriteRenderer>();
        playerSr.sprite = LoadSprite("standing_uniform");
        playerSr.sortingLayerName = "Player";
        playerSr.sortingOrder = 0;
        FitHeight(playerGO, playerSr, KyloHeight, GroundY);

        Rigidbody2D playerRb = playerGO.AddComponent<Rigidbody2D>();
        playerRb.gravityScale = 3f;
        playerRb.freezeRotation = true;

        BoxCollider2D playerCol = playerGO.AddComponent<BoxCollider2D>();
        playerCol.size = new Vector2(1.0f, 1.9f);     // in the figure's own local units

        PlayerMovement player = playerGO.AddComponent<PlayerMovement>();
        player.standingSprite = LoadSprite("standing_uniform");
        player.walkFrames = UniformWalkFrames();
        player.currentWakeStage = PlayerMovement.WakeStage.Standing;
        // A smaller hop than the default: at this zoom the usual 9 would carry
        // him most of the way up the slide tower.
        player.jumpForce = 4.2f;

        VoiceCommand voiceCommand = playerGO.AddComponent<VoiceCommand>();
        voiceCommand.player = player;

        // --- The three places he turns up, as plain sprites ---
        GameObject atTop = CreateFigure("Kylo_SlideTop", "standing_uniform",
                                         SlideTopX, SlideTopY, KyloHeight * 0.92f);
        GameObject atFoot = CreateFigure("Kylo_SlideFoot", "standing_uniform",
                                          SlideFootX, GroundY, KyloHeight);
        GameObject onSwing = CreateFigure("Kylo_OnSwing", "sitting_desk_uniform",
                                           SwingX, SwingSeatY, SeatedHeight);
        atTop.SetActive(false);
        atFoot.SetActive(false);
        onSwing.SetActive(false);

        GameObject groundGO = new GameObject("Ground");
        groundGO.transform.position = new Vector3(0f, GroundY - 0.25f, 0f);
        groundGO.AddComponent<BoxCollider2D>().size = new Vector2(FullViewWidth * 2f, 0.5f);

        // --- What lights up ---
        TimedOverlay gleamAkyat = CreateGleamAt("Gleam_Akyat", LadderX, -0.90f, 1.2f);
        TimedOverlay gleamDausdos = CreateGleamAt("Gleam_Dausdos", ChuteX, -0.78f, 1.7f);
        TimedOverlay gleamSakay = CreateGleamAt("Gleam_Sakay", SwingX, -1.00f, 1.1f);
        TimedOverlay gleamTulak = CreateGleamAt("Gleam_Tulak", SwingX, -1.00f, 1.7f);
        TimedOverlay gleamTalon = CreateGleamAt("Gleam_Talon", GrassX, GroundY + 0.5f, 1.3f);

        // --- Word prompts ---
        GameObject uiRoot = new GameObject("UI_WordPrompts_Canvas");
        Canvas canvas = uiRoot.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        canvas.overrideSorting = true;
        canvas.sortingLayerName = "UI";
        canvas.sortingOrder = 10;
        uiRoot.transform.position = Vector3.zero;
        // Smaller than the other scenes': the camera is zoomed in, so a badge
        // built at the usual size would fill a third of the screen.
        uiRoot.transform.localScale = new Vector3(0.0045f, 0.0045f, 0.0045f);
        uiRoot.GetComponent<RectTransform>().sizeDelta = new Vector2(4000, 1200);
        uiRoot.AddComponent<GraphicRaycaster>();

        if (Object.FindFirstObjectByType<UnityEngine.EventSystems.EventSystem>() == null)
        {
            GameObject es = new GameObject("EventSystem");
            es.AddComponent<UnityEngine.EventSystems.EventSystem>();
            es.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
        }

        GameObject pAkyat = CreatePromptUI(uiRoot.transform, "Prompt_Akyat", "Akyat", new Vector3(960, -60, 0));
        GameObject pDausdos = CreatePromptUI(uiRoot.transform, "Prompt_Dausdos", "Dausdos", new Vector3(580, -70, 0));
        GameObject pSakay = CreatePromptUI(uiRoot.transform, "Prompt_Sakay", "Sakay", new Vector3(-1000, -130, 0));
        GameObject pTulak = CreatePromptUI(uiRoot.transform, "Prompt_Tulak", "Tulak", new Vector3(-1000, -40, 0));
        GameObject pTalon = CreatePromptUI(uiRoot.transform, "Prompt_Talon", "Talon", new Vector3(-266, -90, 0));

        // --- Objectives: climb, slide, ride, push, jump ---
        var akyat = MakeObjective("Akyat", pAkyat);
        UnityEventTools.AddBoolPersistentListener(akyat.onCorrect, playerGO.SetActive, false);
        UnityEventTools.AddBoolPersistentListener(akyat.onCorrect, atTop.SetActive, true);
        UnityEventTools.AddPersistentListener(akyat.onCorrect, gleamAkyat.Play);

        var dausdos = MakeObjective("Dausdos", pDausdos);
        UnityEventTools.AddBoolPersistentListener(dausdos.onCorrect, atTop.SetActive, false);
        UnityEventTools.AddBoolPersistentListener(dausdos.onCorrect, atFoot.SetActive, true);
        UnityEventTools.AddPersistentListener(dausdos.onCorrect, gleamDausdos.Play);

        var sakay = MakeObjective("Sakay", pSakay);
        UnityEventTools.AddBoolPersistentListener(sakay.onCorrect, atFoot.SetActive, false);
        UnityEventTools.AddBoolPersistentListener(sakay.onCorrect, onSwing.SetActive, true);
        UnityEventTools.AddPersistentListener(sakay.onCorrect, gleamSakay.Play);

        // Tulak keeps him on the swing - the push is what sets it moving, so
        // taking him off it here would read as getting down again.
        var tulak = MakeObjective("Tulak", pTulak);
        UnityEventTools.AddPersistentListener(tulak.onCorrect, gleamTulak.Play);

        // Back to the real player, who then actually leaves the ground.
        var talon = MakeObjective("Talon", pTalon);
        UnityEventTools.AddBoolPersistentListener(talon.onCorrect, onSwing.SetActive, false);
        UnityEventTools.AddBoolPersistentListener(talon.onCorrect, playerGO.SetActive, true);
        UnityEventTools.AddPersistentListener(talon.onCorrect, player.Jump);
        UnityEventTools.AddPersistentListener(talon.onCorrect, gleamTalon.Play);

        SceneObjectiveController controller = uiRoot.AddComponent<SceneObjectiveController>();
        controller.voiceCommand = voiceCommand;
        controller.playerMovement = player;
        controller.objectives = new[] { akyat, dausdos, sakay, tulak, talon };

        VoiceUIBuilder.BuildVoiceUI(voiceCommand, controller);
        SceneFader fader = SceneFaderBuilder.Build();

        // --- A middle scene of Level 3 now: Scene 3 ends it ---
        // The counter is added by hand, because DatabaseDemoBuilder used to
        // bring one with the result panel and that panel has moved on. Without
        // it the playground's five words leave the level total.
        GameObject runScoreGO = new GameObject("RunScore");
        RunScoreCounter runScore = runScoreGO.AddComponent<RunScoreCounter>();
        runScore.voiceCommand = voiceCommand;
        runScore.objectiveController = controller;
        runScore.resetOnStart = false;   // Scene 1 starts the run

        UnityEventTools.AddStringPersistentListener(
            controller.onAllObjectivesComplete, fader.FadeOutAndLoad, NextScene);

        System.IO.Directory.CreateDirectory("Assets/Scenes");
        EditorSceneManager.SaveScene(scene, ScenePath);
        AddToBuildSettings();

        Debug.Log("[SALINLAHI] Parke Scene 2 built and saved to " + ScenePath +
                   ". Words: Akyat, Dausdos, Sakay, Tulak, Talon - all five new. " +
                   "It fades into " + NextScene + ", which ends the level - so build Parke " +
                   "Scene 3 as well, or the level runs into a scene that is not there.");
    }

    /// <summary>
    /// Scales a figure so it stands worldHeight tall with its feet on feetY.
    /// Sizing from the sprite's own bounds rather than from a scale factor is
    /// what lets the standing art (745 px) and the seated art (580 px) share a
    /// scene without one of them sinking into the grass.
    /// </summary>
    private static void FitHeight(GameObject go, SpriteRenderer sr, float worldHeight, float feetY)
    {
        float s = 1f;
        if (sr.sprite != null && sr.sprite.bounds.size.y > 0.0001f)
            s = worldHeight / sr.sprite.bounds.size.y;
        go.transform.localScale = new Vector3(s, s, 1f);
        go.transform.position = new Vector3(go.transform.position.x,
                                             feetY + worldHeight * 0.5f, 0f);
    }

    private static GameObject CreateFigure(string name, string spriteFile,
                                            float x, float feetY, float worldHeight)
    {
        GameObject go = new GameObject(name);
        go.transform.position = new Vector3(x, 0f, 0f);
        SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = LoadSprite(spriteFile);
        sr.sortingLayerName = "Player";
        sr.sortingOrder = 0;
        FitHeight(go, sr, worldHeight, feetY);
        return go;
    }

    private static GameObject CreateProp(string name, string spriteFile,
                                          float x, float worldHeight, float baseY, bool visible)
    {
        GameObject go = new GameObject(name);
        go.transform.position = new Vector3(x, 0f, 0f);
        SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = LoadSprite(spriteFile);
        sr.sortingLayerName = "Props";
        sr.sortingOrder = 6;
        FitHeight(go, sr, worldHeight, baseY);
        go.SetActive(visible);
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
            LoadSprite("walk_frame_1_uniform"), LoadSprite("walk_frame_2_uniform"),
            LoadSprite("walk_frame_3_uniform"), LoadSprite("walk_frame_4_uniform"),
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
