// Assets/Editor/BuildBahayScene4.cs
//
// One-click scene builder for Bahay Scene 4 ("Getting Dressed").
// Teaches five words: Damit, Medyas, Sapatos, Bag, Kunin.
//
// Reuses the bedroom, so it needs no new background. The bedroom's furniture
// is painted INTO bedroom_background.png - bed, dresser, nightstand and door
// are not separate objects - so everything the child names is laid out on the
// floor in front of it. That also means the room has no mirror of its own;
// The bedroom's furniture is painted INTO bedroom_background.png - bed,
// dresser, nightstand and door are not separate objects - so everything the
// child names is laid out on the floor in front of it.
//
// Kylo faces the player here rather than standing side-on, so the child can
// see him being dressed. The walk frames stay side-on, since he only turns to
// walk and this scene has no walking word.
//
// Four of the five words visibly dress him, in order:
//
//   start    barefoot, green tee      (he has just come out of the bath)
//   Damit    barefoot, white uniform
//   Medyas   socks, uniform
//   Sapatos  shoes, uniform
//   Kunin    shoes, uniform, bag
//
// Only "Bag" leaves him unchanged - it names the thing, and Kunin takes it.
//
// The barefoot and socks poses were redrawn from the front-facing art; the
// uniform versions of each are Tools/make_uniform.py recolouring the green
// shirt, so the shirt is identical across every stage. Only the front-facing
// pose has these stages: the walk frames are still shod, which is why he is
// only barefoot in this scene and not in Scenes 1-3.
//
// This is now the LAST playable scene of Level 1, so the level's result is
// saved and shown here. Scene 3 fades into this one. When Scene 5 is built,
// move the database demo on to it the same way.
//
// Sprites expected in Assets/Sprites/:
//   bedroom_background, prop_damit, prop_medyas, prop_sapatos,
//   prop_bag, fx_gleam, standing(_with_bag), walk_frame_1..4(_with_bag),
//   ui_arrow, ui_word_badge
//
// Run via: Tools > SALINLAHI > Build Bahay Scene 4

using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class BuildBahayScene4
{
    private const string SpriteFolder = "Assets/Sprites/";
    private const string ScenePath = "Assets/Scenes/Chapter1_Level4_Damit.unity";

    // The bedroom art imports at 169 pixels per unit, not the 100 the other
    // backgrounds use, so at Scene 1's scale of 1.9458 it comes out 19.3 x
    // 10.9 world units - almost exactly one camera view. Check the .meta
    // before changing any of this.
    private const float BgScale = 1.9458f;
    private const float FloorY = -3.32f;              // the painted floorboards

    // The character art, which imports at 120 - see BuildBahayScene2.
    private const float SpritePPU = 120f;
    private const float PlayerScale = 0.83f;
    private const float FeetBelowPivot = (743.5f - 745f / 2f) / SpritePPU;
    private const float PlayerFeetY = FloorY + FeetBelowPivot * PlayerScale;

    [MenuItem("Tools/SALINLAHI/Build Bahay Scene 4")]
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
        cam.orthographicSize = 5.4f;
        cam.transform.position = new Vector3(0, 0, -10);
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = Color.black;
        camGO.tag = "MainCamera";
        camGO.AddComponent<AudioListener>();

        GameObject bg = new GameObject("Background_Bedroom");
        SpriteRenderer bgSr = bg.AddComponent<SpriteRenderer>();
        bgSr.sprite = LoadSprite("bedroom_background");
        bgSr.sortingLayerName = "Background";
        bgSr.sortingOrder = 0;
        bg.transform.localScale = new Vector3(BgScale, BgScale, 1f);

        // --- The things laid out on the floor ---
        // Each is sized by the world height it should stand, not by a scale
        // factor, so it comes out right whatever resolution the art is.
        GameObject propDamit   = CreateProp("Prop_Damit",   "prop_damit",   -6.0f, 1.85f);
        GameObject propMedyas  = CreateProp("Prop_Medyas",  "prop_medyas",  -3.8f, 0.85f);
        GameObject propSapatos = CreateProp("Prop_Sapatos", "prop_sapatos", -2.0f, 0.95f);
        GameObject propBag     = CreateProp("Prop_Bag",     "prop_bag",      0.2f, 2.10f);

        PickUpItem takeDamit   = AddPickUp(propDamit);
        PickUpItem takeMedyas  = AddPickUp(propMedyas);
        PickUpItem takeSapatos = AddPickUp(propSapatos);

        // --- Player ---
        GameObject playerGO = new GameObject("player_character");
        playerGO.tag = "Player";
        playerGO.transform.position = new Vector3(5.0f, PlayerFeetY, 0f);
        playerGO.transform.localScale = new Vector3(PlayerScale, PlayerScale, 1f);

        SpriteRenderer playerSr = playerGO.AddComponent<SpriteRenderer>();
        playerSr.sortingLayerName = "Player";
        // Face the child, not side-on: they are dressing him, so they should
        // see the uniform go on from the front. 'talking' is the same height
        // and foot position as 'standing', so nothing else has to move.
        playerSr.sprite = LoadSprite("talking_barefoot");

        Rigidbody2D playerRb = playerGO.AddComponent<Rigidbody2D>();
        playerRb.gravityScale = 3f;
        playerRb.freezeRotation = true;

        BoxCollider2D playerCol = playerGO.AddComponent<BoxCollider2D>();
        playerCol.size = new Vector2(1.2f, FeetBelowPivot * 2f);

        PlayerMovement player = playerGO.AddComponent<PlayerMovement>();
        player.lyingDownSprite = LoadSprite("lying_down");
        player.sittingUpSprite = LoadSprite("sitting_up");
        player.standingSprite = LoadSprite("talking_barefoot");
        player.walkFrames = new[]
        {
            LoadSprite("walk_frame_1"), LoadSprite("walk_frame_2"),
            LoadSprite("walk_frame_3"), LoadSprite("walk_frame_4"),
        };
        player.currentWakeStage = PlayerMovement.WakeStage.Standing;

        VoiceCommand voiceCommand = playerGO.AddComponent<VoiceCommand>();
        voiceCommand.player = player;

        // Each clothing word moves him on one stage. The order is fixed, so
        // each sprite already carries everything put on before it.
        takeDamit.player = player;
        takeDamit.carryingStandingSprite = LoadSprite("talking_barefoot_uniform");
        takeDamit.carryingWalkFrames = new[]
        {
            LoadSprite("walk_frame_1_uniform"), LoadSprite("walk_frame_2_uniform"),
            LoadSprite("walk_frame_3_uniform"), LoadSprite("walk_frame_4_uniform"),
        };

        takeMedyas.player = player;
        takeMedyas.carryingStandingSprite = LoadSprite("talking_socks_uniform");

        takeSapatos.player = player;
        takeSapatos.carryingStandingSprite = LoadSprite("talking_uniform");

        // "Kunin" puts the bag on OVER the uniform. These are the uniform
        // frames with the bag, not the green-shirt ones - the words are
        // answered in a fixed order, so by the time this runs he is already
        // dressed, and reusing the green set here would undress him again.
        PickUpItem takeBag = AddPickUp(propBag);
        takeBag.player = player;
        takeBag.carryingStandingSprite = LoadSprite("talking_uniform_with_bag");
        takeBag.carryingWalkFrames = new[]
        {
            LoadSprite("walk_frame_1_uniform_with_bag"), LoadSprite("walk_frame_2_uniform_with_bag"),
            LoadSprite("walk_frame_3_uniform_with_bag"), LoadSprite("walk_frame_4_uniform_with_bag"),
        };

        // The room is one screen wide, so the camera never moves.
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
        groundGO.AddComponent<BoxCollider2D>().size = new Vector2(40f, 0.5f);

        CreateWall("Wall_Left", -9.3f);
        CreateWall("Wall_Right", 9.3f);

        // --- A gleam, for the one word that names something rather than takes it ---
        TimedOverlay gleamBag = CreateGleam("Gleam_Bag", propBag, 2.8f);

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

        // Each prompt sits above the thing it names.
        GameObject pDamit   = CreatePromptUI(uiRoot.transform, "Prompt_Damit",   "Damit",   new Vector3(-600, -60, 0));
        GameObject pMedyas  = CreatePromptUI(uiRoot.transform, "Prompt_Medyas",  "Medyas",  new Vector3(-380, -150, 0));
        GameObject pSapatos = CreatePromptUI(uiRoot.transform, "Prompt_Sapatos", "Sapatos", new Vector3(-200, -140, 0));
        GameObject pBag     = CreatePromptUI(uiRoot.transform, "Prompt_Bag",     "Bag",     new Vector3(20, -30, 0));
        GameObject pKunin   = CreatePromptUI(uiRoot.transform, "Prompt_Kunin",   "Kunin",   new Vector3(20, 60, 0));

        // --- Objectives, in the order you would actually get dressed ---
        var damit = MakeObjective("Damit", pDamit);
        UnityEventTools.AddPersistentListener(damit.onCorrect, takeDamit.PickUp);

        var medyas = MakeObjective("Medyas", pMedyas);
        UnityEventTools.AddPersistentListener(medyas.onCorrect, takeMedyas.PickUp);

        var sapatos = MakeObjective("Sapatos", pSapatos);
        UnityEventTools.AddPersistentListener(sapatos.onCorrect, takeSapatos.PickUp);

        // "Bag" names the thing instead of taking it, so it gleams; "Kunin"
        // below is what actually picks it up.
        var bag = MakeObjective("Bag", pBag);
        UnityEventTools.AddPersistentListener(bag.onCorrect, gleamBag.Play);

        // ...and then Kunin takes it, and Kylo is wearing it from here on.
        var kunin = MakeObjective("Kunin", pKunin);
        UnityEventTools.AddPersistentListener(kunin.onCorrect, takeBag.PickUp);

        SceneObjectiveController controller = uiRoot.AddComponent<SceneObjectiveController>();
        controller.voiceCommand = voiceCommand;
        controller.objectives = new[] { damit, medyas, sapatos, bag, kunin };

        VoiceUIBuilder.BuildVoiceUI(voiceCommand, controller);
        SceneFaderBuilder.Build();

        // Last playable scene of the level, so the result lands here. This
        // also adds the scene's RunScoreCounter.
        // Level 1 on the map, so finishing here earns the Bahay badge and
        // unlocks Paaralan.
        DatabaseDemoBuilder.Build("Bahay - Level 1", "LevelSelect", 1, "badge_bahay");

        System.IO.Directory.CreateDirectory("Assets/Scenes");
        EditorSceneManager.SaveScene(scene, ScenePath);
        AddToBuildSettings();

        Debug.Log("[SALINLAHI] Bahay Scene 4 built and saved to " + ScenePath +
                   ". Words: Damit, Medyas, Sapatos, Bag, Kunin - he starts barefoot " +
                   "and each word dresses him one stage further. Level 1 now ends " +
                   "here, so re-run Build Bahay Scene 3 as well - it fades into this scene " +
                   "instead of showing the result itself.");
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

    /// <summary>
    /// A prop standing on the floor, sized by the world height it should be
    /// rather than by a scale factor. The art for these comes from several
    /// places at different resolutions, so a fixed scale would make them
    /// wildly different sizes.
    /// </summary>
    private static GameObject CreateProp(string name, string spriteFile, float x, float worldHeight)
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

    private static PickUpItem AddPickUp(GameObject item)
    {
        PickUpItem pick = item.AddComponent<PickUpItem>();
        pick.itemInScene = item;
        return pick;
    }

    /// <summary>A gleam centred on a prop, for a word that points rather than takes.</summary>
    private static TimedOverlay CreateGleam(string name, GameObject target, float worldSize)
    {
        GameObject go = new GameObject(name);
        go.transform.position = target.transform.position;
        SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = LoadSprite("fx_gleam");
        sr.sortingLayerName = "Props";
        sr.sortingOrder = 40;                    // over the prop, under the player
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
