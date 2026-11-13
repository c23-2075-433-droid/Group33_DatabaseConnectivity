// Assets/Editor/BuildBahayScene3.cs
//
// One-click scene builder for Bahay Scene 3 ("Maligo" - taking a bath).
// Teaches five words, in the order the routine actually happens:
// Tuwalya, Sabon, Maligo, Sipilyo, Suklay.
//
// THE WORDS FOLLOW THE STORY. Kylo fetches the two things he needs first -
// his towel and the soap - then bathes, and only once he is clean and dry
// does he brush his teeth and comb his hair. Saying "Maligo" in the middle
// rather than at the top is the whole point of the order: the bath is
// something the first two words prepare for and the last two follow on
// from, instead of an opening move with four errands hung off it.
//
// Sabon, Sipilyo and Tuwalya used to be said in Scene 2, which is the scene
// about finding the bathroom and putting the light on; naming soap and a
// toothbrush there meant the words happened before the bath rather than
// around it. They moved, and so did their objects, back to the positions
// the artwork paints them at.
//
// Kaliwa is gone, and so is the walk it used to do. Nothing moves Kylo in
// this scene any more: he stands where the scene puts him, gathers what he
// needs and bathes on the spot.
//
// Tabo is gone too - the word and the object both. The water jar is still
// painted into the bathroom and the bath still happens at it; there is just
// no dipper sitting on its lid any more, and nothing the child has to name
// to use it.
//
// Reuses the bathroom Scene 2 ends in, so it needs no new background. The
// objects are placed back where the artwork had them; saying a word uses
// that one up.
//
// "Maligo" leaves him in a towel. The bath should visibly change him, not
// just wash water across the screen - and it pays off the towel he took in
// Scene 2, which he is now wearing.
//
// A middle scene: Scene 2 fades into it and it fades on into Scene 4, which
// is where the level now ends and where the result is saved and shown.
//
// Sprites expected in Assets/Sprites/:
//   ui_arrow_point - the prompt arrow, bold enough to see,
//   bathroom_background, item_sabon, item_suklay, item_baso,
//   item_sipilyo, item_tuwalya, fx_bath,
//   standing_towel_messy, talking_towel,
//   standing_towel_combed if it has ever been drawn - "Suklay" falls back
//   to talking_towel without it, sipilyo_frame_1..6,
//
// Sounds expected in Assets/Resources/Sfx/:
//   brush_teeth - made by Tools/make_brush_sfx.py
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

    // How far above an object's top edge its word badge floats, in canvas
    // units (world units x100). Big enough that the arrow fits underneath.
    private const float PromptLift = 110f;
    private const float ArrowSize = 76f;      // the arrow's long side
    private const float ArrowReach = 16f;     // how far the jab travels
    private const float ArrowClear = 18f;     // tip-to-object gap at rest

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

        // --- Everything Kylo uses in here, plus the cup as scenery ---
        // Soap, toothbrush and towel moved in from Bahay 2 with their words:
        // that scene's job is getting into the bathroom and putting the light
        // on, and this is where the washing happens. The positions are the
        // ones they are painted at - both scenes draw the same
        // bathroom_background at the same BgScale, so they transfer exactly
        // and the room looks unchanged until something is used.
        // No Item_Tabo: the dipper left with its word. Everything else is
        // where the painting puts it.
        GameObject itemSabon = CreateItem("Item_Sabon", "item_sabon", new Vector2(1.556f, 1.025f), 5);
        GameObject itemSuklay = CreateItem("Item_Suklay", "item_suklay", new Vector2(2.750f, 0.988f), 5);
        // The brush stands in the cup, so the cup has to draw over it.
        GameObject itemSipilyo = CreateItem("Item_Sipilyo", "item_sipilyo", new Vector2(4.225f, 1.694f), 4);
        CreateItem("Item_Baso", "item_baso", new Vector2(3.931f, 1.188f), 5);
        GameObject itemTuwalya = CreateItem("Item_Tuwalya", "item_tuwalya", new Vector2(7.262f, 1.019f), 5);

        PickUpItem useSabon = itemSabon.AddComponent<PickUpItem>();
        useSabon.itemInScene = itemSabon;
        PickUpItem useSuklay = itemSuklay.AddComponent<PickUpItem>();
        useSuklay.itemInScene = itemSuklay;
        PickUpItem useSipilyo = itemSipilyo.AddComponent<PickUpItem>();
        useSipilyo.itemInScene = itemSipilyo;
        PickUpItem useTuwalya = itemTuwalya.AddComponent<PickUpItem>();
        useTuwalya.itemInScene = itemTuwalya;

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
        // 0.95 rather than 0.85, because the pose change now happens behind
        // this at its peak and 0.85 leaves enough of him showing to see it.
        bathFx.peakAlpha = 0.95f;

        PlaySoundEffect bathSound = fxGO.AddComponent<PlaySoundEffect>();
        bathSound.id = "bath_water";   // 2.6s, the same length as the fade

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

        // Out of the bath and into a towel, turned to face the player.
        //
        // talking_towel, not standing_towel: it is the FRONT-FACING towel
        // art, and it is already Kylo's standing pose for the whole of
        // Scene 4, where he walks into the bedroom still wearing it. Using
        // it here means he comes out of the bath looking exactly as he does
        // in the scene that follows, instead of turning side-on for one
        // scene and back to the player for the next. The side-on
        // standing_towel is what the bath used to leave him in, and it is
        // the reason he spent the rest of this scene facing away.
        //
        // standing_towel_messy is the front pose with his arms down at his
        // sides instead of raised mid-sentence, and his hair stuck up in
        // every direction from the water - which is what Rachel says a
        // word later: "Kylo is clean, but his hair is sticking up in every
        // direction."
        GameObject towelGO = new GameObject("Pose_Towel");
        PoseSwitch towel = towelGO.AddComponent<PoseSwitch>();
        towel.player = player;
        towel.standingSprite = LoadSpriteOr("standing_towel_messy", "talking_towel");

        // ...and the comb puts it back down again, which is what makes the
        // messy hair work: a before only reads as a before if there is an
        // after, and "Suklay" was otherwise the one word in the scene that
        // changed nothing about Kylo.
        //
        // With no combed art drawn this lands on talking_towel - his hair
        // neat again, but his hands back up. That is a deliberate trade.
        // It keeps Rachel honest ("Kylo's hair is neat and smooth") without
        // another drawing, and it leaves him in the exact pose Scene 4
        // opens him in, so the hand-off between the two scenes is seamless.
        // The arms are the price; standing_towel_combed removes it if it
        // is ever drawn.
        // "Sipilyo" is DRAWN rather than snapped - the first action in this
        // scene that obeys the animation standard's rule 1. He lifts the
        // brush to his mouth, brushes, and lowers it again, landing back on
        // the pose he came out of the bath in, because his hair is not
        // combed until the next word.
        //
        // player, not target: PlayerMovement rewrites its renderer from
        // standingSprite every frame it is not moving, so a sequence driving
        // the renderer directly is overwritten before anyone sees it.
        //
        // The loop is the brushing motion itself - the brush travelling
        // across his teeth and then tilting up and down - not his hand
        // swapping sides of his mouth. Four positions: left, right, upper
        // teeth, lower teeth, with the brush in the same hand throughout.
        //
        // The numbers add up to the length of the sound deliberately:
        //   lead-in   2 frames / 4 fps = 0.50 s   he lifts the brush
        //   loop      4 x 4   / 4 fps = 4.00 s   eight strokes
        //   tail      1 frame / 4 fps = 0.25 s   he lowers it
        //                              = 4.75 s, which is brush_teeth.wav.
        // Change one and change the other, or he goes on brushing in silence.
        GameObject brushGO = new GameObject("Pose_Sipilyo");
        PoseSequence brush = brushGO.AddComponent<PoseSequence>();
        brush.player = player;
        brush.playOnEnable = false;
        brush.framesPerSecond = 4f;
        brush.loopFramesPerSecond = 4f;
        brush.loopCycles = 4;
        brush.frames = new[] { LoadSprite("sipilyo_frame_1"), LoadSprite("sipilyo_frame_2") };
        brush.loopFrames = new[]
        {
            LoadSprite("sipilyo_frame_2"),   // brush at the left of his mouth
            LoadSprite("sipilyo_frame_3"),   // across to the right
            LoadSprite("sipilyo_frame_4"),   // tilted up, upper teeth
            LoadSprite("sipilyo_frame_5"),   // tilted down, lower teeth
        };
        brush.tailFrames = new[] { LoadSprite("sipilyo_frame_6") };
        brush.destinationSprite = LoadSpriteOr("standing_towel_messy", "talking_towel");

        // The bristles. Static SoundEffects.Play cannot be a persistent
        // UnityEvent listener, so the scene gets a component that can be.
        PlaySoundEffect brushSound = brushGO.AddComponent<PlaySoundEffect>();
        brushSound.id = "brush_teeth";

        GameObject combedGO = new GameObject("Pose_TowelCombed");
        PoseSwitch combed = combedGO.AddComponent<PoseSwitch>();
        combed.player = player;
        combed.standingSprite = LoadSpriteOr("standing_towel_combed", "talking_towel");

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

        if (Object.FindAnyObjectByType<UnityEngine.EventSystems.EventSystem>() == null)
        {
            GameObject es = new GameObject("EventSystem");
            es.AddComponent<UnityEngine.EventSystems.EventSystem>();
            es.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
        }

        // Canvas units are world units x100, so each badge sits over the
        // object it names.
        //
        // Each one is now PromptLift above the TOP of its object rather than
        // just clear of it. The old spacing left 62 to 87 units between the
        // badge and the object, and an arrow big enough for a child to see
        // is taller than that - which is why the first attempt landed on
        // top of the soap and hid the thing it was pointing at. The arrow
        // has to fit in the gap, so the gap is what changed.
        //
        // ONLY THE THINGS HE HAS TO FIND GET AN ARROW. Sabon, Sipilyo and
        // Tuwalya are small objects on a busy painted wall, and a child who
        // cannot read the badge has no way to know which one is meant. The
        // other two do not have that problem: Maligo is an action rather
        // than an object, and the comb is not something the child has to
        // hunt for by the time Suklay comes round. An arrow on everything
        // is an arrow nobody looks at.
        //
        // Object tops, measured from the sprites: Sabon 119, Sipilyo 193,
        // Tuwalya 326. The three with arrows are lifted to PromptLift above
        // those so the arrow has room underneath; the other two keep the
        // heights they were painted to.
        GameObject pMaligo  = CreatePromptUI(uiRoot.transform, "Prompt_Maligo",  "Maligo",  new Vector3(-700, 300, 0), false);
        GameObject pSabon   = CreatePromptUI(uiRoot.transform, "Prompt_Sabon",   "Sabon",   new Vector3( 156, 119 + PromptLift, 0), true);
        GameObject pSuklay  = CreatePromptUI(uiRoot.transform, "Prompt_Suklay",  "Suklay",  new Vector3( 275, 200, 0), false);
        GameObject pSipilyo = CreatePromptUI(uiRoot.transform, "Prompt_Sipilyo", "Sipilyo", new Vector3( 422, 193 + PromptLift, 0), true);
        GameObject pTuwalya = CreatePromptUI(uiRoot.transform, "Prompt_Tuwalya", "Tuwalya", new Vector3( 726, 326 + PromptLift, 0), true);

        // --- Objectives, in the order of the routine ---
        // Getting ready, then the bath, then tidying up afterwards.
        //

        // 1-2. What he needs before he can start. The towel comes off its
        // hook first because it is the thing he will be wearing at the end.
        var tuwalya = MakeObjective("Tuwalya", pTuwalya);
        UnityEventTools.AddPersistentListener(tuwalya.onCorrect, useTuwalya.PickUp);

        var sabon = MakeObjective("Sabon", pSabon);
        UnityEventTools.AddPersistentListener(sabon.onCorrect, useSabon.PickUp);

        // 3. The bath itself. He does NOT walk anywhere for it: he is
        // already standing in the room with his towel and his soap, and the
        // water washes over the whole screen wherever he is. The walk was
        // left over from when "Kaliwa" sent him to the jar and the tabo was
        // the thing he picked up there - with both of those gone, crossing
        // the room buys nothing and turns him to face away from the player
        // for the rest of the scene.
        var maligo = MakeObjective("Maligo", pMaligo);
        UnityEventTools.AddPersistentListener(maligo.onCorrect, bathFx.Play);
        UnityEventTools.AddPersistentListener(maligo.onCorrect, bathSound.Play);
        // The pose change hangs off the overlay's PEAK, not off the word.
        // Wired to the word it fired instantly: Kylo popped into a towel in
        // plain sight and THEN water washed over him, which is two things
        // happening rather than one thing happening. Behind the water he
        // goes in dry and comes out dressed, which is what a transition is.
        UnityEventTools.AddPersistentListener(bathFx.onPeak, towel.Apply);

        // 4-5. Clean and dry, and now he can see to his teeth and his hair.
        var sipilyo = MakeObjective("Sipilyo", pSipilyo);
        UnityEventTools.AddPersistentListener(sipilyo.onCorrect, useSipilyo.PickUp);
        UnityEventTools.AddPersistentListener(sipilyo.onCorrect, brush.Play);
        UnityEventTools.AddPersistentListener(sipilyo.onCorrect, brushSound.Play);

        var suklay = MakeObjective("Suklay", pSuklay);
        UnityEventTools.AddPersistentListener(suklay.onCorrect, useSuklay.PickUp);
        UnityEventTools.AddPersistentListener(suklay.onCorrect, combed.Apply);

        SceneObjectiveController controller = uiRoot.AddComponent<SceneObjectiveController>();
        controller.voiceCommand = voiceCommand;
        controller.objectives = new[] { tuwalya, sabon, maligo, sipilyo, suklay };

        VoiceUIBuilder.BuildVoiceUI(voiceCommand, controller);
        // Still needed in the scene: SceneResultPanel looks the fader up at
        // run time to fade out of whichever button is pressed.
        SceneFaderBuilder.Build();

        // --- This scene's words count towards the level's total ---
        // The scene that ends a level gets a counter from DatabaseDemoBuilder;
        // a middle scene has to carry its own, or its words are dropped from
        // the result the player is shown.
        GameObject runScoreGO = new GameObject("RunScore");
        RunScoreCounter runScore = runScoreGO.AddComponent<RunScoreCounter>();
        runScore.voiceCommand = voiceCommand;
        runScore.resetOnStart = false;   // Scene 1 starts the run

        // --- The scene's summary, whose Next button hands on to Scene 4 ---
        SceneResultPanelBuilder.Build(controller, voiceCommand, NextScene);

        System.IO.Directory.CreateDirectory("Assets/Scenes");
        EditorSceneManager.SaveScene(scene, ScenePath);
        AddToBuildSettings();

        Debug.Log("[SALINLAHI] Bahay Scene 3 built and saved to " + ScenePath +
                   ". Words, in story order: Tuwalya, Sabon, Maligo, Sipilyo, Suklay - get ready, " +
                   "bathe, then tidy up. No tabo: the word and the dipper are both gone. Its last word fades on into " +
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
        // ceiling: every badge in this scene already floats above the thing
        // it names, by 0.6 to 0.9 world units, so up is away from it.
        //
        // ui_arrow_point rather than ui_arrow: a solid cartoon arrow with a
        // shadow, which a child can see against a painted bathroom wall.
        // The old one is a thin chevron. It is drawn pointing RIGHT, hence
        // the quarter turn clockwise, and preserveAspect because it is not
        // square and a square sizeDelta would squash it.
        //
        // And it moves - see ArrowBob. This is the only part of the prompt
        // that is any use to a child who cannot read the badge, which in
        // this game is most of them.
        GameObject arrowGO = new GameObject("Arrow", typeof(RectTransform));
        arrowGO.transform.SetParent(root.transform, false);
        Image arrowImg = arrowGO.AddComponent<Image>();
        arrowImg.sprite = LoadSprite("ui_arrow_point");
        arrowImg.preserveAspect = true;
        RectTransform arrowRect = arrowGO.GetComponent<RectTransform>();
        arrowRect.sizeDelta = new Vector2(ArrowSize, ArrowSize);
        // Hangs in the gap between badge and object, and at full stretch its
        // tip stops just short of the object rather than covering it. That
        // is the whole arithmetic: tip at rest is PromptLift - ArrowReach -
        // ArrowClear below the badge, and the jab spends ArrowReach of it.
        arrowRect.anchoredPosition = new Vector2(
            0, -(PromptLift - ArrowReach - ArrowClear) + ArrowSize / 2f);
        arrowRect.localRotation = Quaternion.Euler(0, 0, -90);

        ArrowBob bob = arrowGO.AddComponent<ArrowBob>();
        bob.direction = Vector2.down;
        bob.distance = ArrowReach;

        root.SetActive(false);   // SceneObjectiveController shows the first one
        return root;
    }

    /// <summary>
    /// The preferred sprite, or a stand-in while it is still being drawn.
    /// Same idea as CharacterPoses.Barefoot: a pose that has not been drawn
    /// yet leaves the scene playable instead of leaving the player invisible.
    /// </summary>
    private static Sprite LoadSpriteOr(string preferred, string fallback)
    {
        Sprite first = AssetDatabase.LoadAssetAtPath<Sprite>(SpriteFolder + preferred + ".png");
        if (first != null) return first;

        Debug.Log("[SALINLAHI] No " + preferred + " yet - using " + fallback + ".");
        return LoadSprite(fallback);
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
