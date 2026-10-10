// Assets/Editor/BuildPaaralanScene4.cs
//
// One-click scene builder for Paaralan Scene 4 ("Recess").
// Teaches five words: Labas, Kain, Laro, Tawa, Tulong.
//
// Four of the five are new - only Kain has been met before, at breakfast in
// Bahay Scene 5, where it was eating at a table rather than sharing baon in a
// schoolyard. Meeting a word again somewhere else is the point of the design
// document's reuse column, not an accident.
//
// A MIDDLE SCENE OF LEVEL 2. Scene 3 fades into this one; this one fades into
// Scene 5, which ends the level. It held that ending until Scene 5 existed.
//
// STAGING. Back out in the schoolyard from Scene 1, because that is where
// break happens - so the yard's geometry is Scene 1's, unchanged: the same
// floor line, the same wide-shot character scale. Kylo wears the no-bag
// uniform here; his bag is on his desk in the classroom he just left.
//
// WHAT EACH WORD DOES:
//
//   Labas    he walks out into the yard
//   Kain     his baon appears, and lights up
//   Laro     the classmate lights up - they are playing
//   Tawa     Kylo switches to his laughing pose
//   Tulong   a dropped book on the ground lights up, then is gone - picked up
//
// THE CLASSMATE IS A RECOLOUR of Kylo, made by Tools/make_classmate.py: brown
// hair, maroon PE shorts, mirrored. Three of these five words are about
// another child and the project has none, and Scene 1's comment already
// settled the principle - a word aimed at nobody "teaches the word in a
// vacuum". At this scale, a fifth of the frame across a yard, it reads. It
// would not in close-up, and a drawn classmate is the real answer.
//
// Sprites expected in Assets/Sprites/:
//   paaralan_background, standing_uniform, talking_uniform,
//   walk_frame_1..4_uniform, char_classmate, prop_kanin, prop_aklat,
//   fx_gleam, ui_arrow, ui_word_badge, badge_paaralan
//
// Run via: Tools > SALINLAHI > Build Paaralan Scene 4

using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class BuildPaaralanScene4
{
    private const string SpriteFolder = "Assets/Sprites/";
    private const string ScenePath = "Assets/Scenes/Chapter2_Level4_Recess.unity";
    private const string NextScene = "Chapter2_Level5_Uwian";

    private const float ViewHeight = 10.8f;           // camera is orthographic 5.4
    private const float ViewWidth = 19.2f;

    // Scene 1's yard, unchanged - same paved floor, same wide-shot scale.
    // A character here is deliberately small: this is an establishing shot of
    // a schoolyard, not a room you stand in.
    private const float FloorY = -3.20f;
    private const float SpritePPU = 120f;
    private const float PlayerScale = 0.45f;
    private const float FeetBelowPivot = (743.5f - 745f / 2f) / SpritePPU;
    private const float PlayerFeetY = FloorY + FeetBelowPivot * PlayerScale;

    // He comes out of the school on the left and stops short of his friend.
    private const float PlayerStartX = -3.40f;
    private const float PlayLineX = -0.60f;           // where "Labas" walks him to
    private const float ClassmateX = 2.80f;
    private const float ClassmateHeight = 2.80f;      // the same child height as Kylo

    // On the ground between them.
    private const float BaonX = -1.60f;
    private const float BaonHeight = 0.62f;
    private const float DroppedBookX = 1.20f;
    // Bigger than it would really be. At a true 0.42 the book was four percent
    // of the frame and read as a smudge on the path at phone size.
    private const float DroppedBookHeight = 0.60f;

    private const int OrderKylo = 0;
    private const int OrderProps = 5;

    [MenuItem("Tools/SALINLAHI/Build Paaralan Scene 4")]
    public static void BuildScene()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            Debug.LogError("[SALINLAHI] Stop Play mode first - this rebuilds the scene, which Unity doesn't allow while the game is running.");
            return;
        }

        // A SpriteRenderer handed a null sprite renders NOTHING, so missing
        // art here is invisible rather than loud. Make it loadable first.
        SpriteImportFixer.Verify(new[]
        {
            "paaralan_background", "standing_uniform", "talking_uniform",
            "char_classmate", "prop_kanin", "prop_aklat", "fx_gleam",
            "ui_arrow", "ui_word_badge", "badge_paaralan",
        }, "Paaralan Scene 4");

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

        // --- The yard, scaled from its own bounds to fill the view ---
        GameObject bg = new GameObject("Background_Schoolyard");
        SpriteRenderer bgSr = bg.AddComponent<SpriteRenderer>();
        bgSr.sprite = LoadSprite("paaralan_background");
        bgSr.sortingLayerName = "Background";
        bgSr.sortingOrder = 0;
        if (bgSr.sprite != null && bgSr.sprite.bounds.size.y > 0.0001f)
        {
            float s = Mathf.Max(ViewHeight / bgSr.sprite.bounds.size.y,
                                ViewWidth / bgSr.sprite.bounds.size.x);
            bg.transform.localScale = new Vector3(s, s, 1f);
        }

        GameObject classmate = CreateStandingOptional("Char_Classmate", "char_classmate",
                                                       ClassmateX, FloorY, ClassmateHeight);

        // --- Kylo, no bag: it is on his desk in the room he just left ---
        GameObject playerGO = new GameObject("player_character");
        playerGO.tag = "Player";
        playerGO.transform.position = new Vector3(PlayerStartX, PlayerFeetY, 0f);
        playerGO.transform.localScale = new Vector3(PlayerScale, PlayerScale, 1f);

        SpriteRenderer playerSr = playerGO.AddComponent<SpriteRenderer>();
        playerSr.sortingLayerName = "Player";
        playerSr.sortingOrder = OrderKylo;
        playerSr.sprite = LoadSprite("standing_uniform");

        Rigidbody2D playerRb = playerGO.AddComponent<Rigidbody2D>();
        playerRb.gravityScale = 3f;
        playerRb.freezeRotation = true;

        BoxCollider2D playerCol = playerGO.AddComponent<BoxCollider2D>();
        playerCol.size = new Vector2(1.2f, FeetBelowPivot * 2f);

        PlayerMovement player = playerGO.AddComponent<PlayerMovement>();
        player.standingSprite = LoadSprite("standing_uniform");
        player.walkFrames = UniformWalkFrames();
        player.currentWakeStage = PlayerMovement.WakeStage.Standing;

        // "Labas" is one walk out into the yard, stopping beside his friend
        // rather than after a fixed time - a marker, so moving the friend
        // moves where he stops instead of needing a new duration.
        GameObject spot = new GameObject("PlaySpot");
        spot.transform.position = new Vector3(PlayLineX, FloorY, 0f);
        player.walkTarget = spot.transform;
        player.walkTargetThreshold = 0.35f;
        player.voiceWalkDuration = 1.4f;

        VoiceCommand voiceCommand = playerGO.AddComponent<VoiceCommand>();
        voiceCommand.player = player;

        // "Tawa" turns him to face the player, laughing. Unlike Scene 2, the
        // front-facing pose is the RIGHT one here: a laugh is shared with
        // whoever is watching, and his friend is beside him, not in front.
        GameObject laughGO = new GameObject("Pose_Laugh");
        PoseSwitch laugh = laughGO.AddComponent<PoseSwitch>();
        laugh.player = player;
        laugh.standingSprite = LoadSprite("talking_uniform");

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

        // --- On the ground ---
        // The baon arrives with "Kain"; the dropped book is already lying
        // there, waiting to be noticed, and goes when it is picked up.
        GameObject baon = CreatePropOptional("Prop_Baon", "prop_kanin",
                                              BaonX, BaonHeight, FloorY);
        GameObject dropped = CreatePropOptional("Prop_DroppedBook", "prop_aklat",
                                                 DroppedBookX, DroppedBookHeight, FloorY);
        if (dropped != null) dropped.SetActive(true);

        // --- What lights up ---
        TimedOverlay gleamLabas = CreateGleamAt("Gleam_Labas", PlayLineX, FloorY + 1.2f, 2.6f);
        TimedOverlay gleamKain = CreateGleamAt("Gleam_Kain", BaonX, FloorY + BaonHeight * 0.5f, 1.3f);
        TimedOverlay gleamLaro = classmate != null
            ? CreateGleam("Gleam_Laro", classmate, 3.4f)
            : CreateGleamAt("Gleam_Laro", ClassmateX, FloorY + 1.4f, 3.4f);
        TimedOverlay gleamTawa = CreateGleamAt("Gleam_Tawa", PlayLineX, FloorY + 1.6f, 3.0f);
        TimedOverlay gleamTulong = CreateGleamAt("Gleam_Tulong", DroppedBookX,
                                                  FloorY + DroppedBookHeight * 0.5f, 1.4f);

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

        GameObject pLabas = CreatePromptUI(uiRoot.transform, "Prompt_Labas", "Labas", new Vector3(-330, 60, 0));
        GameObject pKain = CreatePromptUI(uiRoot.transform, "Prompt_Kain", "Kain", new Vector3(-160, -150, 0));
        GameObject pLaro = CreatePromptUI(uiRoot.transform, "Prompt_Laro", "Laro", new Vector3(280, 80, 0));
        GameObject pTawa = CreatePromptUI(uiRoot.transform, "Prompt_Tawa", "Tawa", new Vector3(-60, 110, 0));
        GameObject pTulong = CreatePromptUI(uiRoot.transform, "Prompt_Tulong", "Tulong", new Vector3(120, -150, 0));

        // --- Objectives: go out, eat, play, laugh, help ---
        var labas = MakeObjective("Labas", pLabas);
        UnityEventTools.AddPersistentListener(labas.onCorrect, player.WalkForward);
        UnityEventTools.AddPersistentListener(labas.onCorrect, gleamLabas.Play);

        var kain = MakeObjective("Kain", pKain);
        if (baon != null) UnityEventTools.AddBoolPersistentListener(kain.onCorrect, baon.SetActive, true);
        UnityEventTools.AddPersistentListener(kain.onCorrect, gleamKain.Play);

        var laro = MakeObjective("Laro", pLaro);
        UnityEventTools.AddPersistentListener(laro.onCorrect, gleamLaro.Play);

        var tawa = MakeObjective("Tawa", pTawa);
        UnityEventTools.AddPersistentListener(tawa.onCorrect, laugh.Apply);
        UnityEventTools.AddPersistentListener(tawa.onCorrect, gleamTawa.Play);

        // Helping takes the dropped book away - the one word here with a
        // visible consequence rather than a glow.
        var tulong = MakeObjective("Tulong", pTulong);
        UnityEventTools.AddPersistentListener(tulong.onCorrect, gleamTulong.Play);
        if (dropped != null)
            UnityEventTools.AddBoolPersistentListener(tulong.onCorrect, dropped.SetActive, false);

        SceneObjectiveController controller = uiRoot.AddComponent<SceneObjectiveController>();
        controller.voiceCommand = voiceCommand;
        controller.playerMovement = player;
        controller.objectives = new[] { labas, kain, laro, tawa, tulong };

        VoiceUIBuilder.BuildVoiceUI(voiceCommand, controller);
        SceneFader fader = SceneFaderBuilder.Build();

        // --- A middle scene of Level 2 now: Scene 5 ends it ---
        // The counter is added by hand, because DatabaseDemoBuilder used to
        // bring one with the result panel and that panel has moved on. Without
        // it, the words said at recess leave the level total.
        GameObject runScoreGO = new GameObject("RunScore");
        RunScoreCounter runScore = runScoreGO.AddComponent<RunScoreCounter>();
        runScore.voiceCommand = voiceCommand;
        runScore.objectiveController = controller;
        runScore.resetOnStart = false;   // Scene 1 starts the run

        // Break is over and the class goes back in.
        UnityEventTools.AddStringPersistentListener(
            controller.onAllObjectivesComplete, fader.FadeOutAndLoad, NextScene);

        System.IO.Directory.CreateDirectory("Assets/Scenes");
        EditorSceneManager.SaveScene(scene, ScenePath);
        AddToBuildSettings();

        Debug.Log("[SALINLAHI] Paaralan Scene 4 built and saved to " + ScenePath +
                   ". Words: Labas, Kain, Laro, Tawa, Tulong - four of them new. " +
                   "It fades into " + NextScene + ", which ends the level - so build " +
                   "Paaralan Scene 5 as well, or the level runs into a scene that is not there.");
    }

    private static GameObject CreateStandingOptional(string name, string spriteFile,
                                                      float x, float floorY, float worldHeight)
    {
        Sprite sprite = LoadSpriteOptional(spriteFile);
        if (sprite == null) return null;

        GameObject go = new GameObject(name);
        SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = sprite;
        sr.sortingLayerName = "Props";
        sr.sortingOrder = OrderProps;

        float s = sprite.bounds.size.y > 0.0001f ? worldHeight / sprite.bounds.size.y : 1f;
        go.transform.localScale = new Vector3(s, s, 1f);
        go.transform.position = new Vector3(x, floorY + worldHeight * 0.5f, 0f);
        return go;
    }

    private static GameObject CreatePropOptional(string name, string spriteFile,
                                                  float x, float worldHeight, float baseY)
    {
        Sprite sprite = LoadSpriteOptional(spriteFile);
        if (sprite == null) return null;

        GameObject go = new GameObject(name);
        SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = sprite;
        sr.sortingLayerName = "Props";
        sr.sortingOrder = OrderProps + 2;

        float s = sprite.bounds.size.y > 0.0001f ? worldHeight / sprite.bounds.size.y : 1f;
        go.transform.localScale = new Vector3(s, s, 1f);
        go.transform.position = new Vector3(x, baseY + worldHeight * 0.5f, 0f);
        go.SetActive(false);
        return go;
    }

    private static TimedOverlay CreateGleam(string name, GameObject target, float worldSize)
    {
        return CreateGleamAt(name, target.transform.position.x, target.transform.position.y, worldSize);
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

    private static Sprite LoadSpriteOptional(string spriteFile)
    {
        return AssetDatabase.LoadAssetAtPath<Sprite>(SpriteFolder + spriteFile + ".png");
    }
}
