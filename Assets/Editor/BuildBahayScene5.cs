// Assets/Editor/BuildBahayScene5.cs
//
// One-click scene builder for Bahay Scene 5 ("Eating Breakfast").
// Teaches five words: Upo, Pagkain, Kain, Inom, Linis.
//
// The five words are the meal in order, and every one of them changes the
// table:
//
//   Upo      Kylo sits down at the table, which is still bare
//   Pagkain  the food arrives - rice, egg and cup come out together
//   Kain     he eats - the rice and egg go, the bowl and plate stay, empty
//   Inom     he drinks - the cup stays and lights up; it is opaque, so an
//            empty cup and a full one are the same picture
//   Linis    he clears up - the empty dishes and the cup are taken away and
//            the table is bare again, the way it started
//
// "Pagkain" is what earns the empty table. The scene now opens with nothing
// laid out, so the word is not naming food that is already sitting there -
// it is what puts it there. That is also why Kanin and Itlog are no longer
// spoken objectives: with one word for the food as a whole, naming each dish
// separately was three words for one idea. The rice and the egg are still
// drawn, because they are what Kain empties, and the scene would be a boy
// eating nothing without them.
//
// Hugas has left this scene for a plainer reason: there is no sink drawn in
// this kitchen, so washing could only ever be a gleam on Kylo himself. It is
// still taught at recess in Paaralan Scene 4, where there is one.
//
// Washing hands has not left, though - only the asking has. Rachel says it in
// the scene's first story line instead, so the child is told to wash before
// eating without being made to earn a word for a sink that is not there. A
// habit worth teaching does not have to be a spoken objective to be taught.
//
// This scene was in the design document from the start and was never built -
// Level 1 ran Scene 1 -> 2 -> 3 -> 4 and then ended, so Kylo got dressed and
// left for school without eating. Scene 6 ("Leaving for School") is still
// missing after this one.
//
// The scene is staged from Nanay's seat: the camera is her point of view
// across the breakfast table, with Kylo facing her from the chair opposite.
//
// That needs him to sit BEHIND the table, which a single painted background
// cannot do - anything drawn over it appears in front of the table. So the one
// generated painting is split at the table's far edge (row 737 of 1080, world
// y -1.97) into two sprites:
//
//   kusina_pov_background   the whole painting, drawn behind everything
//   kusina_pov_table        the table alone, drawn IN FRONT of Kylo
//
// Kylo sits between them, so the table hides his lap and the chair seat and he
// reads as sitting at it. The food is drawn in front of the table layer again,
// because it rests on top of the table. All three share the Player sorting
// layer and are ordered by sortingOrder, so no new sorting layer is needed.
//
// Sitting down moves the same front-facing pose down rather than swapping
// sprites - from the far side of a table, standing and sitting differ in how
// much of him clears the table edge, nothing else. PoseSettle now lowers him
// the last half unit so he lands on the chair instead of starting on it, and
// a PoseSequence is wired for the three drawn in-betweens this scene still
// needs; see the note beside it for why Paaralan Scene 2's cannot be reused.
//
// This is now the LAST playable scene of Level 1, so the level's result is
// saved and shown here - Scene 4 fades into this one instead. When Scene 6 is
// built, move the database demo on to it the same way.
//
// Sprites expected in Assets/Sprites/:
//   kusina_pov_background, kusina_pov_table, talking_uniform_with_bag,
//   prop_kanin, prop_itlog, prop_kanin_empty, prop_itlog_empty, item_baso,
//   fx_gleam, ui_arrow, ui_word_badge
//
// Run via: Tools > SALINLAHI > Build Bahay Scene 5

using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class BuildBahayScene5
{
    private const string SpriteFolder = "Assets/Sprites/";
    private const string ScenePath = "Assets/Scenes/Chapter1_Level5_Almusal.unity";

    // The POV art is 1920x1080 at 100 pixels per unit, so at scale 1 it is
    // exactly 19.2 x 10.8 world units - one camera view, no scaling needed.
    private const float BgScale = 1f;

    // Where the table's far edge falls in the painting, measured off the art.
    // Everything below this is the table layer drawn in front of Kylo.
    private const float TableEdgeY = -1.97f;

    // Where the food rests on the near side of the table, in front of him.
    private const float PlateY = -2.90f;

    // Drawn in the Player sorting layer, ordered back to front.
    private const int OrderKylo = 0;
    private const int OrderTable = 10;
    private const int OrderFood = 20;

    // The character art imports at 120 pixels per unit - see BuildBahayScene2.
    private const float SpritePPU = 120f;
    // Bigger than the side-on scenes use. Those frame a whole room; this one
    // is a face across a table, so Kylo is much nearer the camera. At 0.85 he
    // cleared the table edge by under 3 units and read as a doll at the far
    // end of the room.
    private const float PlayerScale = 1.27f;

    // Where he is across the table, and how far standing clears the edge
    // compared with sitting. Only the difference matters: the table hides
    // everything below its edge either way.
    // Seated, the table edge cuts him at the hip and his head reaches y +2.2,
    // which fills about 39% of the frame height. Standing lifts him by roughly
    // the 40cm a child gains getting up, at this scene's scale.
    // Standing, he waits beside the table on the right; "Upo" puts him in the
    // chair opposite Nanay. The table layer hides his legs either way.
    private const float StandX = 5.2f;
    private const float SitX   = -0.2f;
    private const float SitPivotY   = -1.73f;
    private const float StandPivotY = SitPivotY + 1.9f;

    [MenuItem("Tools/SALINLAHI/Build Bahay Scene 5")]
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

        GameObject bg = new GameObject("Background_Kusina");
        SpriteRenderer bgSr = bg.AddComponent<SpriteRenderer>();
        bgSr.sprite = LoadSprite("kusina_pov_background");
        bgSr.sortingLayerName = "Background";
        bgSr.sortingOrder = 0;
        bg.transform.localScale = new Vector3(BgScale, BgScale, 1f);

        // --- Kylo ---
        // The listening rig is its own object and stays switched on. The two
        // poses below are swapped by "Upo", and anything living on a switched
        // off object stops running with it: with VoiceCommand on the standing
        // pose, sitting down killed its Update, and with it the Editor's Enter
        // key, from "Kanin" onwards. Speech still arrived, because the plugin
        // calls its listener directly, which made the failure look like a
        // recognition problem rather than a disabled component.
        GameObject rig = new GameObject("Kylo_Rig");
        rig.tag = "Player";
        rig.transform.position = new Vector3(SitX, SitPivotY, 0f);

        GameObject standGO = new GameObject("Kylo_Standing");
        standGO.transform.position = new Vector3(StandX, StandPivotY, 0f);
        standGO.transform.localScale = new Vector3(PlayerScale, PlayerScale, 1f);
        SpriteRenderer standSr = standGO.AddComponent<SpriteRenderer>();
        standSr.sprite = LoadSprite("talking_uniform_with_bag");
        standSr.sortingLayerName = "Player";
        standSr.sortingOrder = OrderKylo;

        GameObject seatGO = new GameObject("Kylo_Seated");
        seatGO.transform.position = new Vector3(SitX, SitPivotY, 0f);
        seatGO.transform.localScale = new Vector3(PlayerScale, PlayerScale, 1f);
        SpriteRenderer seatSr = seatGO.AddComponent<SpriteRenderer>();
        seatSr.sprite = LoadSprite("talking_uniform_with_bag");
        seatSr.sortingLayerName = "Player";
        seatSr.sortingOrder = OrderKylo;
        seatGO.SetActive(false);                   // "Upo" turns him on

        // ...and when it does, he should LOWER into the chair rather than
        // appear in it. Rule 1 of Docs/AnimationStandard.md: no instant pose
        // changes.
        //
        // The drawn descent is wired but not yet drawn. Paaralan Scene 2 has
        // its three "Upo" in-betweens (sit_frame_1..3_uniform) and they do
        // not fit here: they are a near-portrait for the close desk shot and
        // have no backpack, while this scene is a full figure across a table
        // with the bag still on. So this scene needs its own three, plus a
        // seated pose to rest on - four drawings, framed to match
        // talking_uniform_with_bag. Until they exist, LoadSpriteOptional
        // returns null for each and PoseSequence skips a missing drawing
        // instead of stalling, so the scene behaves exactly as it did.
        PoseSequence sitFrames = seatGO.AddComponent<PoseSequence>();
        sitFrames.target = seatSr;
        sitFrames.frames = new[]
        {
            LoadSpriteOptional("sit_frame_1_uniform_with_bag"),
            LoadSpriteOptional("sit_frame_2_uniform_with_bag"),
            LoadSpriteOptional("sit_frame_3_uniform_with_bag"),
        };
        // Sitting happens once - it is on the standard's play-once list - so
        // loopFrames stays empty and it runs straight through.
        sitFrames.destinationSprite = LoadSpriteOptional("sitting_uniform_with_bag");
        sitFrames.framesPerSecond = 9f;
        sitFrames.playOnEnable = true;    // "Upo" switches this object on

        // The cheap nine-tenths, and the only part that works TODAY. The
        // seated pose arrives half a unit high and settles, so he drops onto
        // the chair instead of being already on it.
        //
        // No driftFrom, deliberately. He stands at x 5.2 and the chair is at
        // -0.2, and Paaralan Scene 2 slides that gap while its drawn frames
        // play over the top. Here there are no frames yet, and the seated
        // sprite is the STANDING drawing lowered - so a slide would be a
        // standing boy gliding across the kitchen, which is worse than the
        // cut it replaces. Add "sit.driftFrom = SitX - StandX;" at the same
        // time as the drawings, not before.
        PoseSettle sit = seatGO.AddComponent<PoseSettle>();
        sit.dropFrom = 0.5f;
        sit.duration = 0.4f;

        // --- The table, drawn in front of him so he sits behind it ---
        GameObject table = new GameObject("Foreground_Table");
        SpriteRenderer tableSr = table.AddComponent<SpriteRenderer>();
        tableSr.sprite = LoadSprite("kusina_pov_table");
        tableSr.sortingLayerName = "Player";
        tableSr.sortingOrder = OrderTable;
        table.transform.localScale = new Vector3(BgScale, BgScale, 1f);

        // --- Breakfast, on the table in front of him ---
        // Placed where it will land but switched OFF, because the scene opens
        // on a bare table and "Pagkain" is what brings the meal out. Built
        // here rather than spawned at the word: a persistent UnityEvent can
        // switch an existing object on, and cannot create one.
        GameObject propItlog = CreateProp("Prop_Itlog", "prop_itlog", -2.3f, 1.05f, PlateY);
        GameObject propKanin = CreateProp("Prop_Kanin", "prop_kanin", -0.2f, 1.40f, PlateY);
        GameObject propBaso  = CreateProp("Prop_Baso",  "item_baso",   1.9f, 1.30f, PlateY);
        propItlog.SetActive(false);
        propKanin.SetActive(false);
        propBaso.SetActive(false);

        // Eating empties the dishes rather than clearing the table: the same
        // bowl and plate stay where they were, with the food gone. Each is a
        // second object at the same spot, swapped in by "Kain", because a
        // persistent UnityEvent can switch an object on but cannot set a
        // sprite. They are shorter than the full ones - what is removed is the
        // mound of rice and the egg, not the crockery.
        GameObject emptyItlog = CreateProp("Prop_Itlog_Empty", "prop_itlog_empty", -2.3f, 0.50f, PlateY);
        GameObject emptyKanin = CreateProp("Prop_Kanin_Empty", "prop_kanin_empty", -0.2f, 0.95f, PlateY);
        emptyItlog.SetActive(false);
        emptyKanin.SetActive(false);
        // CreateProp leaves these on the "Props" layer, which draws BELOW
        // "Player" - so the table layer hid them whatever their order was.
        // The food rests on top of the table, so it has to share the table's
        // layer and sit above it.
        foreach (GameObject food in new[] { propItlog, propKanin, propBaso,
                                            emptyItlog, emptyKanin })
        {
            SpriteRenderer fsr = food.GetComponent<SpriteRenderer>();
            fsr.sortingLayerName = "Player";
            fsr.sortingOrder = OrderFood;
        }

        // Nothing walks or falls in this scene, so Kylo needs no physics - he
        // is placed, not dropped. PlayerMovement is still here because
        // VoiceCommand needs one and the objective controller reads it to know
        // when an action has finished.
        PlayerMovement player = rig.AddComponent<PlayerMovement>();
        player.standingSprite = LoadSprite("talking_uniform_with_bag");
        player.currentWakeStage = PlayerMovement.WakeStage.Standing;
        rig.GetComponent<Rigidbody2D>().bodyType = RigidbodyType2D.Kinematic;

        VoiceCommand voiceCommand = rig.AddComponent<VoiceCommand>();
        voiceCommand.player = player;

        // Something has to light up when a word only names a thing, or when
        // what it does would otherwise happen in silence.
        TimedOverlay gleamBaso    = CreateGleam("Gleam_Baso",    propBaso,   2.0f);
        // Wide, because "Pagkain" brings out all three dishes at once: the
        // gleam has to cover the spread from the egg to the cup, not one plate.
        TimedOverlay gleamPagkain = CreateGleam("Gleam_Pagkain", propKanin,  4.4f);
        // "Linis" is clearing up, so it lands on the plates he has just
        // emptied, in the moment before they are taken away.
        TimedOverlay gleamLinis   = CreateGleam("Gleam_Linis",   emptyKanin, 2.6f);
        foreach (TimedOverlay fx in new[] { gleamBaso, gleamPagkain, gleamLinis })
        {
            fx.overlay.sortingLayerName = "Player";
            fx.overlay.sortingOrder = OrderFood + 1;
        }

        // --- Word prompts ---
        GameObject uiRoot = new GameObject("UI_WordPrompts_Canvas");
        Canvas canvas = uiRoot.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
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

        // Canvas units are world units x100. Each prompt sits above the thing
        // it names; "Upo" sits over the chair Kylo is about to use.
        GameObject pUpo     = CreatePromptUI(uiRoot.transform, "Prompt_Upo",     "Upo",     new Vector3(520, 250, 0));
        GameObject pPagkain = CreatePromptUI(uiRoot.transform, "Prompt_Pagkain", "Pagkain", new Vector3(-20, -110, 0));
        GameObject pKain    = CreatePromptUI(uiRoot.transform, "Prompt_Kain",    "Kain",    new Vector3(-20, 300, 0));
        GameObject pInom    = CreatePromptUI(uiRoot.transform, "Prompt_Inom",    "Inom",    new Vector3(190, -235, 0));
        GameObject pLinis   = CreatePromptUI(uiRoot.transform, "Prompt_Linis",   "Linis",   new Vector3(-20, -340, 0));

        // --- Objectives, in order ---
        var upo = MakeObjective("Upo", pUpo);
        UnityEventTools.AddBoolPersistentListener(upo.onCorrect, standGO.SetActive, false);
        UnityEventTools.AddBoolPersistentListener(upo.onCorrect, seatGO.SetActive, true);

        // The meal arrives. All three dishes come out on the one word rather
        // than one at a time: "Pagkain" is the food as a whole, and a table
        // that filled in three steps would need three words to say so.
        var pagkain = MakeObjective("Pagkain", pPagkain);
        UnityEventTools.AddPersistentListener(pagkain.onCorrect, gleamPagkain.Play);
        UnityEventTools.AddBoolPersistentListener(pagkain.onCorrect, propKanin.SetActive, true);
        UnityEventTools.AddBoolPersistentListener(pagkain.onCorrect, propItlog.SetActive, true);
        UnityEventTools.AddBoolPersistentListener(pagkain.onCorrect, propBaso.SetActive, true);

        // Eating swaps the full dishes for the empty ones.
        var kain = MakeObjective("Kain", pKain);
        UnityEventTools.AddBoolPersistentListener(kain.onCorrect, propKanin.SetActive, false);
        UnityEventTools.AddBoolPersistentListener(kain.onCorrect, propItlog.SetActive, false);
        UnityEventTools.AddBoolPersistentListener(kain.onCorrect, emptyKanin.SetActive, true);
        UnityEventTools.AddBoolPersistentListener(kain.onCorrect, emptyItlog.SetActive, true);

        // The cup is opaque, so drinking from it cannot change how it looks and
        // taking it away would be the same mistake as clearing the plates. It
        // stays on the table and lights up instead.
        var inom = MakeObjective("Inom", pInom);
        UnityEventTools.AddPersistentListener(inom.onCorrect, gleamBaso.Play);

        SceneObjectiveController controller = uiRoot.AddComponent<SceneObjectiveController>();
        controller.voiceCommand = voiceCommand;
        controller.playerMovement = player;
        // Clearing up comes last, and only makes sense after "Kain" has
        // swapped the full plates for the empty ones. The gleam lands on them
        // and then they go, along with the cup, so the word visibly does
        // something rather than only pointing: the table ends as bare as
        // "Pagkain" found it.
        var linis = MakeObjective("Linis", pLinis);
        UnityEventTools.AddPersistentListener(linis.onCorrect, gleamLinis.Play);
        UnityEventTools.AddBoolPersistentListener(linis.onCorrect, emptyKanin.SetActive, false);
        UnityEventTools.AddBoolPersistentListener(linis.onCorrect, emptyItlog.SetActive, false);
        UnityEventTools.AddBoolPersistentListener(linis.onCorrect, propBaso.SetActive, false);

        controller.objectives = new[] { upo, pagkain, kain, inom, linis };

        VoiceUIBuilder.BuildVoiceUI(voiceCommand, controller);
        SceneFaderBuilder.Build();

        // Last playable scene of the level, so the result lands here. This
        // also adds the scene's RunScoreCounter. Finishing Level 1 earns the
        // Bahay badge and unlocks Paaralan on the map.
        DatabaseDemoBuilder.Build("Bahay - Level 1", "LevelSelect", 1, "badge_bahay");

        System.IO.Directory.CreateDirectory("Assets/Scenes");
        EditorSceneManager.SaveScene(scene, ScenePath);
        AddToBuildSettings();

        Debug.Log("[SALINLAHI] Bahay Scene 5 built and saved to " + ScenePath +
                   ". Words: Upo, Pagkain, Kain, Inom, Linis. The table starts bare - " +
                   "\"Pagkain\" brings the meal out and \"Linis\" clears it away again. " +
                   "Kanin and Itlog are no longer spoken objectives; the rice and the egg " +
                   "are still drawn, because they are what \"Kain\" empties. Level 1 ends " +
                   "here, so re-run Build Bahay Scene 4 as well - it fades into this scene " +
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

    private static GameObject CreateProp(string name, string spriteFile, float x, float worldHeight, float baseY)
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
        go.transform.position = new Vector3(x, baseY + worldHeight * 0.5f, 0f);
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

    /// <summary>
    /// A sprite that may not have been drawn yet. Silent when missing, unlike
    /// LoadSprite - the sit frames are wired ahead of the art on purpose, and
    /// a warning per frame per build would be noise rather than news.
    /// </summary>
    private static Sprite LoadSpriteOptional(string spriteFile)
    {
        return AssetDatabase.LoadAssetAtPath<Sprite>(SpriteFolder + spriteFile + ".png");
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
