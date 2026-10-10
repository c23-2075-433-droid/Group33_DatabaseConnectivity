// Assets/Editor/BuildPaaralanScene5.cs
//
// One-click scene builder for Paaralan Scene 5 ("Afternoon Class & Dismissal").
// Teaches five words: Pasok, Upo, Linis, Bag, Alis.
//
// Only Linis and Alis are new. The other three are deliberate returns - Pasok
// from the gate in Scene 1, Upo from sitting down in Scene 2, Bag from
// getting dressed in Bahay Scene 4 - and they come back here in a different
// situation, which is what the design document's reuse column is for. Pasok
// at the gate is arriving at school; Pasok here is coming back in from the
// yard.
//
// THIS SCENE ENDS LEVEL 2, and it is the last one in the level, so nothing
// will take that job from it later. Building it also means re-running Build
// Paaralan Scene 4, which now fades into this scene instead of showing the
// result panel.
//
// STAGING. The classroom of Scenes 2 and 3, and the same seat - he comes back
// from break to the desk he left. Every position below is Scene 2's.
//
// WHAT EACH WORD DOES:
//
//   Pasok   the classroom door lights up - he is back inside
//   Upo     standing Kylo swaps for seated Kylo, at his own desk
//   Linis   the book, notebook and pencil go - the desk is cleared
//   Bag     his bag comes up onto the cleared desk
//   Alis    he stands with the bag on, and the level ends on the result panel
//
// Linis is the one word in the whole level that takes several things away at
// once, which is exactly what tidying looks like. It is also why the desk
// starts covered: the lesson's things are still lying there from Scene 3.
//
// Sprites expected in Assets/Sprites/:
//   menu_bg (or klasrum_background), standing_uniform, standing_uniform_with_bag,
//   sitting_desk_uniform, char_teacher, klasrum_desk_front, prop_aklat,
//   prop_kuwaderno, prop_lapis, prop_bag, fx_gleam, ui_arrow, ui_word_badge,
//   badge_paaralan
//
// Run via: Tools > SALINLAHI > Build Paaralan Scene 5

using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class BuildPaaralanScene5
{
    private const string SpriteFolder = "Assets/Sprites/";
    private const string ScenePath = "Assets/Scenes/Chapter2_Level5_Uwian.unity";

    private const float ViewHeight = 10.8f;           // camera is orthographic 5.4
    private const float ViewWidth = 19.2f;

    // Scene 2's classroom, unchanged. See BuildPaaralanScene2 for where these
    // came from - a student desk measures 1.7 world units in this painting.
    private const float FloorY = -3.15f;
    private const float StandX = -0.90f;
    private const float PlayerHeight = 2.85f;
    private const float SitX = -5.20f;
    private const float SitFeetY = -4.52f;
    private const float SeatedHeight = 3.10f;

    // The lesson's things, still out from Scene 3 - Linis is what clears them.
    private const float AklatX = -5.60f;
    private const float AklatBaseY = -4.15f;
    private const float AklatHeight = 1.00f;
    private const float NotebookX = -4.20f;
    private const float NotebookBaseY = -3.95f;
    private const float NotebookHeight = 0.70f;
    private const float PencilX = -5.60f;
    private const float PencilBaseY = -4.62f;
    private const float PencilHeight = 0.28f;

    // The bag comes up onto the desk once it is clear, nearest the camera so
    // it does not sit behind what it replaced.
    private const float BagX = -4.30f;
    private const float BagBaseY = -4.70f;
    private const float BagHeight = 0.95f;

    private const float TeacherX = 2.60f;
    private const float TeacherHipY = -0.70f;
    private const float TeacherHeight = 1.95f;

    private const float DoorX = 8.30f;
    private const float DoorY = 0.60f;

    private const int OrderKylo = 0;
    private const int OrderDeskFront = 10;
    private const int OrderDeskProps = 20;

    [MenuItem("Tools/SALINLAHI/Build Paaralan Scene 5")]
    public static void BuildScene()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            Debug.LogError("[SALINLAHI] Stop Play mode first - this rebuilds the scene, which Unity doesn't allow while the game is running.");
            return;
        }

        SpriteImportFixer.Verify(new[]
        {
            "standing_uniform", "standing_uniform_with_bag", "sitting_desk_uniform", "char_teacher",
            "klasrum_desk_front", "prop_aklat", "prop_kuwaderno", "prop_lapis",
            "prop_bag", "fx_gleam", "ui_arrow", "ui_word_badge", "badge_paaralan",
        }, "Paaralan Scene 5");

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

        GameObject bg = new GameObject("Background_Klasrum");
        SpriteRenderer bgSr = bg.AddComponent<SpriteRenderer>();
        Sprite roomSprite = LoadSpriteOptional("klasrum_background");
        if (roomSprite == null) roomSprite = LoadSprite("menu_bg");
        bgSr.sprite = roomSprite;
        bgSr.sortingLayerName = "Background";
        bgSr.sortingOrder = 0;
        if (bgSr.sprite != null && bgSr.sprite.bounds.size.y > 0.0001f)
        {
            float s = Mathf.Max(ViewHeight / bgSr.sprite.bounds.size.y,
                                ViewWidth / bgSr.sprite.bounds.size.x);
            bg.transform.localScale = new Vector3(s, s, 1f);
        }

        GameObject teacher = CreateStandingOptional("Char_Teacher", "char_teacher",
                                                     TeacherX, TeacherHipY, TeacherHeight);

        // The listening rig stays switched on and separate from the poses -
        // Bahay Scene 5's lesson: a VoiceCommand on a pose that gets switched
        // off stops updating, and the Editor's Enter key dies with it.
        GameObject rig = new GameObject("Kylo_Rig");
        rig.tag = "Player";
        rig.transform.position = new Vector3(SitX, SitFeetY, 0f);

        // Three states, because the bag has to be in exactly one place at a
        // time. He comes back from the yard WITHOUT it - Scene 4 has him
        // bagless, it was on his desk - packs it on "Bag", and is wearing it
        // again by the time he leaves. With one standing pose he stood there
        // with a bag on his back while packing a second one.
        GameObject standGO = CreateFigure("Kylo_Standing", "standing_uniform",
                                           StandX, FloorY, PlayerHeight);
        GameObject seatGO = CreateFigure("Kylo_Seated", "sitting_desk_uniform",
                                          SitX, SitFeetY, SeatedHeight);
        GameObject leaveGO = CreateFigure("Kylo_Leaving", "standing_uniform_with_bag",
                                           StandX, FloorY, PlayerHeight);
        seatGO.SetActive(false);
        leaveGO.SetActive(false);

        GameObject deskFront = new GameObject("Foreground_Desk");
        SpriteRenderer deskSr = deskFront.AddComponent<SpriteRenderer>();
        deskSr.sprite = LoadSpriteOptional("klasrum_desk_front");
        deskSr.sortingLayerName = "Player";
        deskSr.sortingOrder = OrderDeskFront;
        if (deskSr.sprite == null) deskFront.SetActive(false);
        else deskFront.transform.localScale = bg.transform.localScale;

        // --- The desk starts covered, and ends clear ---
        GameObject propAklat = CreatePropOptional("Prop_Aklat", "prop_aklat",
                                                   AklatX, AklatHeight, AklatBaseY);
        GameObject propNotebook = CreatePropOptional("Prop_Kuwaderno", "prop_kuwaderno",
                                                      NotebookX, NotebookHeight, NotebookBaseY);
        GameObject propPencil = CreatePropOptional("Prop_Lapis", "prop_lapis",
                                                    PencilX, PencilHeight, PencilBaseY);
        GameObject propBag = CreatePropOptional("Prop_Bag", "prop_bag",
                                                 BagX, BagHeight, BagBaseY);
        foreach (GameObject lying in new[] { propAklat, propNotebook, propPencil })
        {
            if (lying != null) lying.SetActive(true);
        }

        PlayerMovement player = rig.AddComponent<PlayerMovement>();
        player.standingSprite = LoadSprite("standing_uniform");
        player.currentWakeStage = PlayerMovement.WakeStage.Standing;
        rig.GetComponent<Rigidbody2D>().bodyType = RigidbodyType2D.Kinematic;

        VoiceCommand voiceCommand = rig.AddComponent<VoiceCommand>();
        voiceCommand.player = player;

        // --- What lights up ---
        TimedOverlay gleamPasok = CreateGleamAt("Gleam_Pasok", DoorX, DoorY, 3.4f);
        TimedOverlay gleamUpo = CreateGleamAt("Gleam_Upo", SitX, SitFeetY + SeatedHeight * 0.6f, 2.4f);
        TimedOverlay gleamLinis = CreateGleamAt("Gleam_Linis", -4.90f, -4.10f, 2.8f);
        TimedOverlay gleamBag = CreateGleamAt("Gleam_Bag", BagX, BagBaseY + BagHeight * 0.5f, 1.6f);
        TimedOverlay gleamAlis = teacher != null
            ? CreateGleam("Gleam_Alis", teacher, 2.4f)
            : CreateGleamAt("Gleam_Alis", TeacherX, TeacherHipY + TeacherHeight * 0.5f, 2.4f);

        foreach (TimedOverlay fx in new[] { gleamLinis, gleamBag })
        {
            fx.overlay.sortingLayerName = "Player";
            fx.overlay.sortingOrder = OrderDeskProps + 1;
        }

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

        GameObject pPasok = CreatePromptUI(uiRoot.transform, "Prompt_Pasok", "Pasok", new Vector3(830, 190, 0));
        GameObject pUpo = CreatePromptUI(uiRoot.transform, "Prompt_Upo", "Upo", new Vector3(-520, -60, 0));
        GameObject pLinis = CreatePromptUI(uiRoot.transform, "Prompt_Linis", "Linis", new Vector3(-490, -280, 0));
        GameObject pBag = CreatePromptUI(uiRoot.transform, "Prompt_Bag", "Bag", new Vector3(-430, -350, 0));
        GameObject pAlis = CreatePromptUI(uiRoot.transform, "Prompt_Alis", "Alis", new Vector3(260, 150, 0));

        // --- Objectives: come back in, sit, tidy, pack, leave ---
        var pasok = MakeObjective("Pasok", pPasok);
        UnityEventTools.AddPersistentListener(pasok.onCorrect, gleamPasok.Play);

        var upo = MakeObjective("Upo", pUpo);
        UnityEventTools.AddBoolPersistentListener(upo.onCorrect, standGO.SetActive, false);
        UnityEventTools.AddBoolPersistentListener(upo.onCorrect, seatGO.SetActive, true);
        UnityEventTools.AddPersistentListener(upo.onCorrect, gleamUpo.Play);

        // Tidying takes everything off the desk at once.
        var linis = MakeObjective("Linis", pLinis);
        foreach (GameObject lying in new[] { propAklat, propNotebook, propPencil })
        {
            if (lying != null)
                UnityEventTools.AddBoolPersistentListener(linis.onCorrect, lying.SetActive, false);
        }
        UnityEventTools.AddPersistentListener(linis.onCorrect, gleamLinis.Play);

        var bag = MakeObjective("Bag", pBag);
        if (propBag != null)
            UnityEventTools.AddBoolPersistentListener(bag.onCorrect, propBag.SetActive, true);
        UnityEventTools.AddPersistentListener(bag.onCorrect, gleamBag.Play);

        // He gets up to go, wearing the bag he just packed - so it comes off
        // the desk at the same moment. The result panel follows straight
        // after, which is what leaving looks like from the player's side.
        var alis = MakeObjective("Alis", pAlis);
        UnityEventTools.AddBoolPersistentListener(alis.onCorrect, seatGO.SetActive, false);
        UnityEventTools.AddBoolPersistentListener(alis.onCorrect, leaveGO.SetActive, true);
        if (propBag != null)
            UnityEventTools.AddBoolPersistentListener(alis.onCorrect, propBag.SetActive, false);
        UnityEventTools.AddPersistentListener(alis.onCorrect, gleamAlis.Play);

        SceneObjectiveController controller = uiRoot.AddComponent<SceneObjectiveController>();
        controller.voiceCommand = voiceCommand;
        controller.playerMovement = player;
        controller.objectives = new[] { pasok, upo, linis, bag, alis };

        VoiceUIBuilder.BuildVoiceUI(voiceCommand, controller);
        SceneFaderBuilder.Build();

        // --- Last scene of Level 2, and the last one there will be ---
        // Also adds this scene's RunScoreCounter with resetOnStart = false, so
        // the words from Scenes 1 to 4 are still in the total.
        DatabaseDemoBuilder.Build("Paaralan - Level 2", "LevelSelect", 2, "badge_paaralan");

        System.IO.Directory.CreateDirectory("Assets/Scenes");
        EditorSceneManager.SaveScene(scene, ScenePath);
        AddToBuildSettings();

        Debug.Log("[SALINLAHI] Paaralan Scene 5 built and saved to " + ScenePath +
                   ". Words: Pasok, Upo, Linis, Bag, Alis - Linis and Alis new. " +
                   "This completes Level 2 and ENDS it, so re-run Build Paaralan Scene 4 " +
                   "as well - it fades into this scene instead of showing the result itself.");
    }

    private static GameObject CreateFigure(string name, string spriteFile,
                                            float x, float feetY, float worldHeight)
    {
        GameObject go = new GameObject(name);
        SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = LoadSprite(spriteFile);
        sr.sortingLayerName = "Player";
        sr.sortingOrder = OrderKylo;

        float s = 1f;
        if (sr.sprite != null && sr.sprite.bounds.size.y > 0.0001f)
            s = worldHeight / sr.sprite.bounds.size.y;
        go.transform.localScale = new Vector3(s, s, 1f);
        go.transform.position = new Vector3(x, feetY + worldHeight * 0.5f, 0f);
        return go;
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
        sr.sortingOrder = 5;

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
        sr.sortingLayerName = "Player";
        sr.sortingOrder = OrderDeskProps;

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
