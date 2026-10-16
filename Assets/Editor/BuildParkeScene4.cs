// Assets/Editor/BuildParkeScene4.cs
//
// One-click scene builder for Parke Scene 4 ("Picnic").
// Teaches five words: Upo, Kain, Ibahagi, Inom, Salamat.
//
// Ibahagi and Salamat are new, and they are the two most interesting words in
// the game to stage. Every other word so far names a thing or a movement:
// something appears, something lights up, somebody walks. These two name a
// relationship between two people. Ibahagi is the only word in SALINLAHI that
// puts food in front of SOMEBODY ELSE, and Salamat is the only one answered
// by another character rather than by the player.
//
// A MIDDLE SCENE OF LEVEL 3. Scene 3 fades into this one; this one fades into
// Scene 5, heading home, which ends the level and the game.
//
// WHAT EACH WORD DOES:
//
//   Upo       both boys sit down on the banig
//   Kain      Kylo's food appears in front of him
//   Ibahagi   a second helping appears in front of his FRIEND - the sharing
//   Inom      the cup appears
//   Salamat   the friend lights up, thanking him
//
// THE BANIG is drawn by Tools/make_park_props.py. Two children sitting on
// open grass read as two children sitting down; the mat is what names the
// scene as a picnic, and a banig is what a Filipino family actually spreads.
//
// Sprites expected in Assets/Sprites/:
//   park_background, standing_uniform_with_bag, sitting_desk_uniform,
//   char_classmate, char_classmate_seated, prop_banig, prop_kanin, item_baso,
//   fx_gleam, ui_arrow, ui_word_badge, badge_parke
//
// Run via: Tools > SALINLAHI > Build Parke Scene 4

using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class BuildParkeScene4
{
    private const string SpriteFolder = "Assets/Sprites/";
    private const string ScenePath = "Assets/Scenes/Chapter3_Level4_Piknik.unity";
    private const string NextScene = "Chapter3_Level5_Uwi";

    private const float ViewHeight = 10.8f;           // camera is orthographic 5.4
    private const float ViewWidth = 19.2f;

    // The same near lawn and the same scale as Parke Scenes 1 and 3.
    private const float FloorY = -4.00f;
    private const float KyloHeight = 3.10f;
    private const float FriendHeight = 3.05f;

    // Seated, they sit ON the mat, so their feet are above the grass line.
    private const float SeatY = -3.70f;
    private const float SeatedHeight = 2.45f;

    private const float KyloX = -1.75f;
    private const float FriendX = 1.15f;

    private const float BanigX = -0.30f;
    private const float BanigHeight = 1.70f;

    // On the mat between them. The shared helping sits on the friend's side -
    // that offset is the whole of what "Ibahagi" means on screen.
    private const float FoodX = -0.60f;
    private const float SharedFoodX = 0.30f;
    private const float CupX = -0.05f;
    private const float FoodBaseY = -3.55f;

    [MenuItem("Tools/SALINLAHI/Build Parke Scene 4")]
    public static void BuildScene()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            Debug.LogError("[SALINLAHI] Stop Play mode first - this rebuilds the scene, which Unity doesn't allow while the game is running.");
            return;
        }

        SpriteImportFixer.Verify(new[]
        {
            "park_background", "standing_uniform_with_bag", "sitting_desk_uniform",
            "char_classmate", "char_classmate_seated", "prop_banig", "prop_kanin",
            "item_baso", "fx_gleam", "ui_arrow", "ui_word_badge", "badge_parke",
        }, "Parke Scene 4");

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

        // The mat is spread before the scene opens - it is what they are
        // walking over to, not something a word produces.
        CreateProp("Prop_Banig", "prop_banig", BanigX, BanigHeight, FloorY, 3, true);

        // --- The two of them, standing and seated ---
        GameObject kyloStand = CreateFigure("Kylo_Standing", "standing_uniform_with_bag",
                                             KyloX, FloorY, KyloHeight);
        GameObject kyloSeat = CreateFigure("Kylo_Seated", "sitting_desk_uniform",
                                            KyloX, SeatY, SeatedHeight);
        GameObject friendStand = CreateFigure("Char_Friend", "char_classmate",
                                               FriendX, FloorY, FriendHeight);
        GameObject friendSeat = CreateFigure("Char_Friend_Seated", "char_classmate_seated",
                                              FriendX, SeatY, SeatedHeight);
        kyloSeat.SetActive(false);
        friendSeat.SetActive(false);

        // The listening rig is separate from the poses, so switching one off
        // does not take VoiceCommand's Update with it - Bahay Scene 5's lesson.
        GameObject rig = new GameObject("Kylo_Rig");
        rig.tag = "Player";
        rig.transform.position = new Vector3(KyloX, FloorY, 0f);

        PlayerMovement player = rig.AddComponent<PlayerMovement>();
        player.standingSprite = LoadSprite("standing_uniform_with_bag");
        player.currentWakeStage = PlayerMovement.WakeStage.Standing;
        rig.GetComponent<Rigidbody2D>().bodyType = RigidbodyType2D.Kinematic;

        VoiceCommand voiceCommand = rig.AddComponent<VoiceCommand>();
        voiceCommand.player = player;

        // --- What goes on the mat ---
        GameObject food = CreateProp("Prop_Kanin", "prop_kanin", FoodX, 0.45f, FoodBaseY, 8, false);
        GameObject shared = CreateProp("Prop_Kanin_Shared", "prop_kanin", SharedFoodX, 0.42f, FoodBaseY, 8, false);
        GameObject cup = CreateProp("Prop_Baso", "item_baso", CupX, 0.40f, FoodBaseY, 8, false);

        // --- What lights up ---
        TimedOverlay gleamUpo = CreateGleamAt("Gleam_Upo", BanigX, FloorY + 0.5f, 3.4f);
        TimedOverlay gleamKain = CreateGleamAt("Gleam_Kain", FoodX, FoodBaseY + 0.25f, 1.2f);
        TimedOverlay gleamIbahagi = CreateGleamAt("Gleam_Ibahagi", SharedFoodX, FoodBaseY + 0.25f, 1.5f);
        TimedOverlay gleamInom = CreateGleamAt("Gleam_Inom", CupX, FoodBaseY + 0.25f, 1.1f);
        TimedOverlay gleamSalamat = CreateGleamAt("Gleam_Salamat", FriendX, SeatY + 1.3f, 2.6f);

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

        GameObject pUpo = CreatePromptUI(uiRoot.transform, "Prompt_Upo", "Upo", new Vector3(-30, -210, 0));
        GameObject pKain = CreatePromptUI(uiRoot.transform, "Prompt_Kain", "Kain", new Vector3(-60, -290, 0));
        GameObject pIbahagi = CreatePromptUI(uiRoot.transform, "Prompt_Ibahagi", "Ibahagi", new Vector3(30, -290, 0));
        GameObject pInom = CreatePromptUI(uiRoot.transform, "Prompt_Inom", "Inom", new Vector3(-5, -290, 0));
        GameObject pSalamat = CreatePromptUI(uiRoot.transform, "Prompt_Salamat", "Salamat", new Vector3(115, -150, 0));

        // --- Objectives: sit, eat, share, drink, thank ---
        var upo = MakeObjective("Upo", pUpo);
        UnityEventTools.AddBoolPersistentListener(upo.onCorrect, kyloStand.SetActive, false);
        UnityEventTools.AddBoolPersistentListener(upo.onCorrect, kyloSeat.SetActive, true);
        UnityEventTools.AddBoolPersistentListener(upo.onCorrect, friendStand.SetActive, false);
        UnityEventTools.AddBoolPersistentListener(upo.onCorrect, friendSeat.SetActive, true);
        UnityEventTools.AddPersistentListener(upo.onCorrect, gleamUpo.Play);

        var kain = MakeObjective("Kain", pKain);
        UnityEventTools.AddBoolPersistentListener(kain.onCorrect, food.SetActive, true);
        UnityEventTools.AddPersistentListener(kain.onCorrect, gleamKain.Play);

        // The second helping appears on the friend's side of the mat. That
        // offset is the whole word: nothing else on screen says "shared".
        var ibahagi = MakeObjective("Ibahagi", pIbahagi);
        UnityEventTools.AddBoolPersistentListener(ibahagi.onCorrect, shared.SetActive, true);
        UnityEventTools.AddPersistentListener(ibahagi.onCorrect, gleamIbahagi.Play);

        var inom = MakeObjective("Inom", pInom);
        UnityEventTools.AddBoolPersistentListener(inom.onCorrect, cup.SetActive, true);
        UnityEventTools.AddPersistentListener(inom.onCorrect, gleamInom.Play);

        // Answered by the other child rather than by Kylo - the only word in
        // the game that works that way round.
        var salamat = MakeObjective("Salamat", pSalamat);
        UnityEventTools.AddPersistentListener(salamat.onCorrect, gleamSalamat.Play);

        SceneObjectiveController controller = uiRoot.AddComponent<SceneObjectiveController>();
        controller.voiceCommand = voiceCommand;
        controller.playerMovement = player;
        controller.objectives = new[] { upo, kain, ibahagi, inom, salamat };

        VoiceUIBuilder.BuildVoiceUI(voiceCommand, controller);
        // Still needed in the scene: SceneResultPanel looks the fader up at
        // run time to fade out of whichever button is pressed.
        SceneFaderBuilder.Build();

        // --- A middle scene of Level 3 now: Scene 5 ends it ---
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

        Debug.Log("[SALINLAHI] Parke Scene 4 built and saved to " + ScenePath +
                   ". Words: Upo, Kain, Ibahagi, Inom, Salamat - Ibahagi and Salamat new. " +
                   "It fades into " + NextScene + ", which ends the level - so build Parke " +
                   "Scene 5 as well, or the level runs into a scene that is not there.");
    }

    private static GameObject CreateFigure(string name, string spriteFile,
                                            float x, float feetY, float worldHeight)
    {
        GameObject go = new GameObject(name);
        SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = LoadSprite(spriteFile);
        sr.sortingLayerName = "Player";
        sr.sortingOrder = 5;

        float s = 1f;
        if (sr.sprite != null && sr.sprite.bounds.size.y > 0.0001f)
            s = worldHeight / sr.sprite.bounds.size.y;
        go.transform.localScale = new Vector3(s, s, 1f);
        go.transform.position = new Vector3(x, feetY + worldHeight * 0.5f, 0f);
        return go;
    }

    private static GameObject CreateProp(string name, string spriteFile, float x,
                                          float worldHeight, float baseY, int order, bool visible)
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
        go.SetActive(visible);
        return go;
    }

    private static TimedOverlay CreateGleamAt(string name, float x, float y, float worldSize)
    {
        GameObject go = new GameObject(name);
        go.transform.position = new Vector3(x, y, 0f);
        SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = LoadSprite("fx_gleam");
        sr.sortingLayerName = "Player";
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
