// Assets/Editor/BuildBahayScene4.cs
//
// One-click scene builder for Bahay Scene 4 ("Getting Dressed").
// Teaches five words: Damit, Medyas, Sapatos, Bag, Kunin.
//
// Reuses the bedroom, so it needs no new background. The bedroom's furniture
// is painted INTO bedroom_background.png - bed, dresser, nightstand and door
// are not separate objects - so everything the child names is a sprite laid
// on top of that painting. That also means the room has no mirror of its own.
//
// THE CLOTHES ARE ON THE BED, THE REST ON THE FLOOR. Rachel's line here is
// "His uniform is folded on the bed", and everything used to be laid out on
// the floorboards, including the uniform - so the one line of English in the
// scene described something the child could see was not there. The uniform
// and the socks now sit on the painted blanket, at the world positions the
// bed occupies (see BedDamit/BedMedyas below); the shoes and the bag stay on
// the floor, where shoes and bags go.
//
// prop_damit_folded and prop_medyas_folded are both cut out of
// Art/bed_clothes_raw.png by Tools/cut_folded_uniform.py - this room with
// the clothes painted onto the bed. They are cut rather than drawn because
// a drawn prop has to earn the painted room's lighting and perspective from
// scratch, and these are painted into it already. The old prop_damit is a
// shirt on a coat hanger, which cannot be laid down flat, and prop_medyas
// is a pair of socks standing upright, which is not how socks lie on a bed.
//
// Kylo faces the player here rather than standing side-on, so the child can
// see him being dressed. The walk frames stay side-on, since he only turns to
// walk and this scene has no walking word.
//
// Four of the five words visibly dress him, in order:
//
//   start    a towel                  (he walks in from the bath wearing it)
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
// Scene 3 fades into this one, and this one now fades on into Scene 5
// (breakfast), which owns the level's result. Until Scene 5 was built this
// scene ended the level and carried the database demo itself.
//
// Sprites expected in Assets/Sprites/:
//   bedroom_background, talking_towel, prop_damit_folded,
//   prop_medyas_folded, prop_sapatos, prop_bag, fx_gleam, standing(_with_bag),
//   walk_frame_1..4(_with_bag), ui_arrow_point, ui_word_badge
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
    private const string NextScene = "Chapter1_Level5_Almusal";

    // The bedroom art imports at 169 pixels per unit, not the 100 the other
    // backgrounds use, so at Scene 1's scale of 1.9458 it comes out 19.3 x
    // 10.9 world units - almost exactly one camera view. Check the .meta
    // before changing any of this.
    private const float BgScale = 1.9458f;
    private const float FloorY = -3.32f;              // the painted floorboards

    // Where the clothes sit on the painted bed, in world units.
    //
    // THE MATTRESS'S FLAT TOP IS ONLY ABOUT 50 BACKGROUND PIXELS DEEP. That
    // is the one measurement everything here turns on, and it was wrong
    // twice. The blanket's back edge against the wall is at y 500 and it
    // FOLDS at y 550 - below that line it is hanging down the front of the
    // bed, all the way to the border at y 650. The whole bright lower half
    // of the blanket is the front of the bed, not the top of it, which is
    // why clothes placed by eye kept ending up draped over the edge.
    //
    // So what is placed is not the sprite's centre but its BOTTOM, the point
    // where it touches cloth, and that has to land between 500 and 550. The
    // uniform rests at y 532 and the socks just forward of it at 537, which
    // leaves most of the band showing in front of them. Both are small
    // enough to sit in that band: the raw art drew them bigger than it is.
    //
    // They stand up above the back edge, over the wainscoting, the same way
    // the painted pillow does. That is what a thing standing on a bed in
    // front of a wall looks like, and it is not a mistake.
    //
    //   uniform  bottom at background pixel (290, 532), 62 px tall
    //   socks    bottom at (384, 537), 45 px tall
    private const float BedDamitX = -6.332f;
    private const float BedDamitY = -0.334f;
    private const float BedDamitH = 0.714f;
    private const float BedMedyasX = -5.250f;
    private const float BedMedyasY = -0.489f;
    private const float BedMedyasH = 0.518f;

    // The word prompts, copied from Bahay Scene 3 - see the long note on
    // CreatePromptUI below for why the arrow had to change. PromptLift is how
    // far above an object's TOP EDGE its badge floats, in canvas units (world
    // units x100), and it is what it is because the arrow has to fit in the
    // gap underneath.
    private const float PromptLift = 110f;
    private const float ArrowSize = 76f;              // the arrow's long side
    private const float ArrowReach = 16f;             // how far the jab travels
    private const float ArrowClear = 18f;             // tip-to-object gap at rest

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

        // --- The things he has to find ---
        // Each is sized by the world height it should be, not by a scale
        // factor, so it comes out right whatever resolution the art is.
        //
        // The uniform and the socks are folded on the bed, because that is
        // what Rachel says. They are smaller than the floor props: a shoe
        // seen on the floorboards and a folded shirt seen lying on a blanket
        // are not the same size on screen, and sizing them like the floor
        // props made the bed look like a shop display.
        GameObject propDamit   = CreatePropAt("Prop_Damit",  "prop_damit_folded",
                                              BedDamitX, BedDamitY, BedDamitH);
        GameObject propMedyas  = CreatePropAt("Prop_Medyas", "prop_medyas_folded",
                                              BedMedyasX, BedMedyasY, BedMedyasH);
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
        playerSr.sprite = LoadSprite("talking_towel");

        Rigidbody2D playerRb = playerGO.AddComponent<Rigidbody2D>();
        playerRb.gravityScale = 3f;
        playerRb.freezeRotation = true;

        BoxCollider2D playerCol = playerGO.AddComponent<BoxCollider2D>();
        playerCol.size = new Vector2(1.2f, FeetBelowPivot * 2f);

        PlayerMovement player = playerGO.AddComponent<PlayerMovement>();
        player.lyingDownSprite = CharacterPoses.Barefoot("lying_down");
        player.sittingUpSprite = CharacterPoses.Barefoot("sitting_up");
        player.standingSprite = LoadSprite("talking_towel");
        player.walkFrames = CharacterPoses.BarefootWalk();
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
        // "Salamin" has NOTHING OF ITS OWN TO POINT AT. This bedroom is drawn
        // without a mirror - see the note at the top of this file - so the
        // word lands on Kylo himself, which is at least who he is checking.
        // It wants a mirror drawn into the room and the gleam moved onto it;
        // until then this is the honest staging rather than the finished one.
        TimedOverlay gleamSalamin = CreateGleam("Gleam_Salamin", playerGO, 3.4f);
        // It lands ON Kylo, and CreateGleam leaves a gleam on the "Props"
        // layer, which this project sorts BELOW "Player". Left there it would
        // be drawn behind the boy it is pointing at.
        gleamSalamin.overlay.sortingLayerName = "Player";
        gleamSalamin.overlay.sortingOrder = 40;

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

        if (Object.FindAnyObjectByType<UnityEngine.EventSystems.EventSystem>() == null)
        {
            GameObject es = new GameObject("EventSystem");
            es.AddComponent<UnityEngine.EventSystems.EventSystem>();
            es.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
        }

        // Each prompt sits PromptLift above the top of the thing it names,
        // rather than at a height picked by eye, so the arrow underneath it
        // always has the same gap to point across. Over() does that sum.
        //
        // ONLY THE THINGS HE HAS TO FIND GET AN ARROW. Damit, Medyas,
        // Sapatos and Bag are objects in the room; Salamin is not - the room
        // has no mirror, so that word lands on Kylo himself and there is
        // nothing for an arrow to point at. An arrow on everything is an
        // arrow nobody looks at.
        GameObject pDamit   = CreatePromptUI(uiRoot.transform, "Prompt_Damit",   "Damit",
                                             Over(BedDamitX, BedDamitY + BedDamitH / 2f), true);
        GameObject pMedyas  = CreatePromptUI(uiRoot.transform, "Prompt_Medyas",  "Medyas",
                                             Over(BedMedyasX, BedMedyasY + BedMedyasH / 2f), true);
        GameObject pSapatos = CreatePromptUI(uiRoot.transform, "Prompt_Sapatos", "Sapatos",
                                             Over(-2.0f, FloorY + 0.95f), true);
        GameObject pSalamin = CreatePromptUI(uiRoot.transform, "Prompt_Salamin", "Salamin",
                                             new Vector3(360, 60, 0), false);
        GameObject pBag     = CreatePromptUI(uiRoot.transform, "Prompt_Bag",     "Bag",
                                             Over(0.2f, FloorY + 2.10f), true);

        // --- Objectives, in the order you would actually get dressed ---
        var damit = MakeObjective("Damit", pDamit);
        UnityEventTools.AddPersistentListener(damit.onCorrect, takeDamit.PickUp);

        var medyas = MakeObjective("Medyas", pMedyas);
        UnityEventTools.AddPersistentListener(medyas.onCorrect, takeMedyas.PickUp);

        var sapatos = MakeObjective("Sapatos", pSapatos);
        UnityEventTools.AddPersistentListener(sapatos.onCorrect, takeSapatos.PickUp);

        // He checks himself before leaving.
        var salamin = MakeObjective("Salamin", pSalamin);
        UnityEventTools.AddPersistentListener(salamin.onCorrect, gleamSalamin.Play);

        // "Bag" BOTH names the bag and takes it now. It used to only gleam,
        // with a second word - "Kunin" - to pick it up; the restructure drops
        // Kunin, so the pickup moves here rather than being lost. Without
        // this the bag would stay on the floor and Kylo would leave for
        // school without it.
        var bag = MakeObjective("Bag", pBag);
        UnityEventTools.AddPersistentListener(bag.onCorrect, gleamBag.Play);
        UnityEventTools.AddPersistentListener(bag.onCorrect, takeBag.PickUp);

        SceneObjectiveController controller = uiRoot.AddComponent<SceneObjectiveController>();
        controller.voiceCommand = voiceCommand;
        controller.objectives = new[] { damit, medyas, sapatos, salamin, bag };

        VoiceUIBuilder.BuildVoiceUI(voiceCommand, controller);
        // Still needed in the scene: SceneResultPanel looks the fader up at
        // run time to fade out of whichever button is pressed.
        SceneFaderBuilder.Build();

        // No longer the last scene of the level: breakfast follows. A middle
        // scene carries its own RunScoreCounter, or the words said here are
        // dropped from the result shown at the end.
        GameObject runScoreGO = new GameObject("RunScore");
        RunScoreCounter runScore = runScoreGO.AddComponent<RunScoreCounter>();
        runScore.voiceCommand = voiceCommand;
        runScore.resetOnStart = false;   // Scene 1 starts the run

        // --- The scene's summary, whose Next button hands on to Scene 5 ---
        SceneResultPanelBuilder.Build(controller, voiceCommand, NextScene);

        System.IO.Directory.CreateDirectory("Assets/Scenes");
        EditorSceneManager.SaveScene(scene, ScenePath);
        AddToBuildSettings();

        Debug.Log("[SALINLAHI] Bahay Scene 4 built and saved to " + ScenePath +
                   ". Words: Damit, Medyas, Sapatos, Bag, Kunin - he starts barefoot " +
                   "and each word dresses him one stage further. Its last word now fades " +
                   "on into Scene 5 (breakfast), which owns the level result.");
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

    /// <summary>
    /// A prop at a place of its own rather than standing on the floor - the
    /// clothes folded on the bed. Same sizing rule as CreateProp: the world
    /// height it should be, not a scale factor.
    /// </summary>
    private static GameObject CreatePropAt(string name, string spriteFile,
                                           float x, float y, float worldHeight)
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
        go.transform.position = new Vector3(x, y, 0f);
        return go;
    }

    /// <summary>
    /// Where a word badge goes for an object whose top edge is at topY:
    /// PromptLift above it, in canvas units, which are world units x100.
    /// </summary>
    private static Vector3 Over(float x, float topY)
    {
        return new Vector3(x * 100f, topY * 100f + PromptLift, 0f);
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

    private static GameObject CreatePromptUI(Transform parent, string name, string word,
                                             Vector3 localPos, bool withArrow)
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

        if (!withArrow)
        {
            root.SetActive(false);
            return root;
        }

        // The arrow points DOWN, from under the badge at the object.
        //
        // It used to sit above the badge pointing up, which pointed at the
        // ceiling: every badge in this scene floats above the thing it names,
        // so up is away from it. That is the same fix Bahay Scene 3 already
        // had, and this scene is where it matters most - after the move to
        // the bed the uniform and the socks are no longer in the row of
        // things on the floor, so a child who cannot read the badge has no
        // way to guess where to look.
        //
        // ui_arrow_point rather than ui_arrow: a solid cartoon arrow with a
        // shadow, which reads against a painted blanket. The old one is a
        // thin chevron. It is drawn pointing RIGHT, hence the quarter turn,
        // and preserveAspect because it is not square.
        //
        // And it moves - see ArrowBob.
        GameObject arrowGO = new GameObject("Arrow", typeof(RectTransform));
        arrowGO.transform.SetParent(root.transform, false);
        Image arrowImg = arrowGO.AddComponent<Image>();
        arrowImg.sprite = LoadSprite("ui_arrow_point");
        arrowImg.preserveAspect = true;
        RectTransform arrowRect = arrowGO.GetComponent<RectTransform>();
        arrowRect.sizeDelta = new Vector2(ArrowSize, ArrowSize);
        // Hangs in the gap between badge and object, and at full stretch its
        // tip stops just short of the object rather than covering it: the tip
        // at rest is PromptLift - ArrowReach - ArrowClear below the badge,
        // and the jab spends ArrowReach of that.
        arrowRect.anchoredPosition = new Vector2(
            0, -(PromptLift - ArrowReach - ArrowClear) + ArrowSize / 2f);
        arrowRect.localRotation = Quaternion.Euler(0, 0, -90);

        ArrowBob bob = arrowGO.AddComponent<ArrowBob>();
        bob.direction = Vector2.down;
        bob.distance = ArrowReach;

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
