// Assets/Editor/BuildBahayScene3.cs
//
// One-click scene builder for Bahay Scene 3 ("Maligo" - taking a bath).
// Teaches three words: Kaliwa (reused from Scene 2), Maligo, Suklay.
//
// Reuses the bathroom Scene 2 ends in, so it needs no new background. The
// water jar and the comb are the two things Kylo uses, placed back where the
// artwork had them; saying their word uses them up.
//
// "Maligo" leaves him in a towel. The bath should visibly change him, not
// just wash water across the screen - and it pays off the towel he took in
// Scene 2, which he is now wearing.
//
// A middle scene: Scene 2 fades into it and it fades on into Scene 4, which
// is where the level now ends and where the result is saved and shown.
//
// Sprites expected in Assets/Sprites/:
//   bathroom_background, item_tabo, item_suklay, item_baso, fx_bath,
//   standing_towel,
//   standing / walk_frame_1..4 - all _barefoot, because the shoes do not go
//   on until Scene 4 - ui_arrow, ui_word_badge
//
// Run via: Tools > SALINLAHI > Build Bahay Scene 3

using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class BuildBahayScene3
{
    private const string SpriteFolder = "Assets/Sprites/";
    private const string ScenePath = "Assets/Scenes/Chapter1_Level3_Maligo.unity";

    // Same numbers as Scene 2 - see BuildBahayScene2 for where they come from.
    private const float BgScale = 10.8f / 8.64f;      // 1.25
    private const float ScreenWidth = 19.2f;
    private const float FloorY = -2.98f;
    private const float SpritePPU = 120f;             // the character art, not the backgrounds
    private const float PlayerScale = 0.83f;
    private const float FeetBelowPivot = (743.5f - 745f / 2f) / SpritePPU;   // 3.09
    private const float PlayerFeetY = FloorY + FeetBelowPivot * PlayerScale;

    // Where the level carries on once the bath is done.
    private const string NextScene = "Chapter1_Level4_Damit";

    [MenuItem("Tools/SALINLAHI/Build Bahay Scene 3")]
    public static void BuildScene()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            Debug.LogError("[SALINLAHI] Stop Play mode first - this rebuilds the scene, which Unity doesn't allow while the game is running.");
            return;
        }

        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        // --- Camera, fixed: the room is exactly one screen wide ---
        GameObject camGO = new GameObject("Main Camera");
        Camera cam = camGO.AddComponent<Camera>();
        cam.orthographic = true;
        cam.orthographicSize = 5.4f;
        cam.transform.position = new Vector3(0, 0, -10);
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = Color.black;
        camGO.tag = "MainCamera";
        camGO.AddComponent<AudioListener>();

        // --- The bathroom, already lit: Scene 2 ended with the light on ---
        GameObject bg = new GameObject("Background_Bathroom");
        SpriteRenderer bgSr = bg.AddComponent<SpriteRenderer>();
        bgSr.sprite = LoadSprite("bathroom_background");
        bgSr.sortingLayerName = "Background";
        bgSr.sortingOrder = 0;
        bg.transform.localScale = new Vector3(BgScale, BgScale, 1f);

        // --- The two things Kylo uses, plus the cup as scenery ---
        GameObject itemTabo = CreateItem("Item_Tabo", "item_tabo", new Vector2(-7.056f, 0.738f), 5);
        GameObject itemSuklay = CreateItem("Item_Suklay", "item_suklay", new Vector2(2.750f, 0.988f), 5);
        CreateItem("Item_Baso", "item_baso", new Vector2(3.931f, 1.188f), 5);

        PickUpItem useTabo = itemTabo.AddComponent<PickUpItem>();
        useTabo.itemInScene = itemTabo;
        PickUpItem useSuklay = itemSuklay.AddComponent<PickUpItem>();
        useSuklay.itemInScene = itemSuklay;

        // --- Water and suds, for the moment the bath happens ---
        GameObject fxGO = new GameObject("FX_Bath");
        SpriteRenderer fxSr = fxGO.AddComponent<SpriteRenderer>();
        fxSr.sprite = LoadSprite("fx_bath");
        fxSr.sortingLayerName = "Props";
        fxSr.sortingOrder = 100;                      // over the player too - he is in it
        fxSr.color = new Color(1f, 1f, 1f, 0f);
        // Sized from its own art, not from BgScale. The bathroom objects were
        // cut out of the background and share its scale, but this was drawn
        // separately and is smaller, so borrowing that number left the water
        // as a rectangle floating in the middle of the room with visible
        // edges. Cover the whole camera instead, whatever size the art is.
        float fxScale = 1f;
        if (fxSr.sprite != null && fxSr.sprite.bounds.size.x > 0.0001f)
            fxScale = Mathf.Max(ScreenWidth / fxSr.sprite.bounds.size.x,
                                cam.orthographicSize * 2f / fxSr.sprite.bounds.size.y);
        fxGO.transform.localScale = new Vector3(fxScale, fxScale, 1f);

        TimedOverlay bathFx = fxGO.AddComponent<TimedOverlay>();
        bathFx.overlay = fxSr;
        bathFx.peakAlpha = 0.85f;

        // --- Player, starting across the room from the water jar ---
        GameObject playerGO = new GameObject("player_character");
        playerGO.tag = "Player";
        playerGO.transform.position = new Vector3(-3.2f, PlayerFeetY, 0f);
        playerGO.transform.localScale = new Vector3(PlayerScale, PlayerScale, 1f);

        SpriteRenderer playerSr = playerGO.AddComponent<SpriteRenderer>();
        playerSr.sortingLayerName = "Player";
        playerSr.sprite = CharacterPoses.Barefoot("standing");

        Rigidbody2D playerRb = playerGO.AddComponent<Rigidbody2D>();
        playerRb.gravityScale = 3f;
        playerRb.freezeRotation = true;

        BoxCollider2D playerCol = playerGO.AddComponent<BoxCollider2D>();
        playerCol.size = new Vector2(1.2f, FeetBelowPivot * 2f);

        PlayerMovement player = playerGO.AddComponent<PlayerMovement>();
        player.lyingDownSprite = CharacterPoses.Barefoot("lying_down");
        player.sittingUpSprite = CharacterPoses.Barefoot("sitting_up");
        player.standingSprite = CharacterPoses.Barefoot("standing");
        player.walkFrames = CharacterPoses.BarefootWalk();
        player.currentWakeStage = PlayerMovement.WakeStage.Standing;

        VoiceCommand voiceCommand = playerGO.AddComponent<VoiceCommand>();
        voiceCommand.player = player;

        // Out of the bath and into a towel. He does not walk again in this
        // scene, so only the standing pose needs a towel version.
        GameObject towelGO = new GameObject("Pose_Towel");
        PoseSwitch towel = towelGO.AddComponent<PoseSwitch>();
        towel.player = player;
        towel.standingSprite = LoadSprite("standing_towel");

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
        groundGO.AddComponent<BoxCollider2D>().size = new Vector2(ScreenWidth * 2f, 0.5f);

        CreateWall("Wall_Left", -9.3f);
        CreateWall("Wall_Right", 9.3f);

        // --- Word prompts (world space, same pattern as the other scenes) ---
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

        GameObject pKaliwa = CreatePromptUI(uiRoot.transform, "Prompt_Kaliwa", "Kaliwa", new Vector3(-320, 300, 0));
        GameObject pMaligo = CreatePromptUI(uiRoot.transform, "Prompt_Maligo", "Maligo", new Vector3(-700, 300, 0));
        GameObject pSuklay = CreatePromptUI(uiRoot.transform, "Prompt_Suklay", "Suklay", new Vector3(275, 200, 0));

        // --- Objectives, in order ---
        // "Kaliwa" walks Kylo across to the water jar. It is a reused word, so
        // under the easy-to-hard plan it carries less support than a new one.
        var kaliwa = MakeObjective("Kaliwa", pKaliwa);
        UnityEventTools.AddPersistentListener(kaliwa.onCorrect, player.WalkLeft);

        // "Maligo" is the bath itself: the tabo is used up and water washes
        // over the screen.
        var maligo = MakeObjective("Maligo", pMaligo);
        UnityEventTools.AddPersistentListener(maligo.onCorrect, useTabo.PickUp);
        UnityEventTools.AddPersistentListener(maligo.onCorrect, bathFx.Play);
        UnityEventTools.AddPersistentListener(maligo.onCorrect, towel.Apply);

        var suklay = MakeObjective("Suklay", pSuklay);
        UnityEventTools.AddPersistentListener(suklay.onCorrect, useSuklay.PickUp);

        SceneObjectiveController controller = uiRoot.AddComponent<SceneObjectiveController>();
        controller.voiceCommand = voiceCommand;
        controller.objectives = new[] { kaliwa, maligo, suklay };

        VoiceUIBuilder.BuildVoiceUI(voiceCommand, controller);
        SceneFader fader = SceneFaderBuilder.Build();

        // --- This scene's words count towards the level's total ---
        // The scene that ends a level gets a counter from DatabaseDemoBuilder;
        // a middle scene has to carry its own, or its words are dropped from
        // the result the player is shown.
        GameObject runScoreGO = new GameObject("RunScore");
        RunScoreCounter runScore = runScoreGO.AddComponent<RunScoreCounter>();
        runScore.voiceCommand = voiceCommand;
        runScore.resetOnStart = false;   // Scene 1 starts the run

        // --- Hand on to Scene 4 (getting dressed) ---
        UnityEventTools.AddStringPersistentListener(
            controller.onAllObjectivesComplete, fader.FadeOutAndLoad, NextScene);

        System.IO.Directory.CreateDirectory("Assets/Scenes");
        EditorSceneManager.SaveScene(scene, ScenePath);
        AddToBuildSettings();

        Debug.Log("[SALINLAHI] Bahay Scene 3 built and saved to " + ScenePath +
                   ". Words: Kaliwa, Maligo, Suklay. Its last word fades on into " +
                   NextScene + " - build that scene too, or the level stops at a black screen.");
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

    /// <summary>One of the loose bathroom objects, at the spot it was painted.</summary>
    private static GameObject CreateItem(string name, string spriteFile, Vector2 pos, int order)
    {
        GameObject go = new GameObject(name);
        go.transform.position = new Vector3(pos.x, pos.y, 0f);
        go.transform.localScale = new Vector3(BgScale, BgScale, 1f);
        SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = LoadSprite(spriteFile);
        sr.sortingLayerName = "Props";
        sr.sortingOrder = order;
        return go;
    }

    /// <summary>An invisible collider that stops the player leaving the view.</summary>
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

    /// <summary>Arrow + badge + word label, matching the other scenes.</summary>
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

        root.SetActive(false);   // SceneObjectiveController shows the first one
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
