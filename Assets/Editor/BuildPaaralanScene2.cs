// Assets/Editor/BuildPaaralanScene2.cs
//
// One-click scene builder for Paaralan Scene 2 ("Entering the Classroom").
// Teaches four words: Lakad, Upo, Kuha, Lapis.
//
// Only Kuha and Lapis are new. Bukas comes from Bahay Scene 2, Kumusta from
// Paaralan Scene 1, Upo from Bahay Scene 5 - they are here because the design
// document puts them here, and because a word is learned by meeting it again
// rather than by being taught once. Paaralan Scene 1 took the opposite view
// and dropped Lakad and Bukas from its own list. If the group settles on one
// rule, this is the scene to change, not that one: here the repeats carry the
// story - you open the door, you greet, you sit - rather than padding it.
//
// A MIDDLE SCENE OF LEVEL 2. Scene 1 starts the run and fades into this one;
// this one fades into Scene 3, which ends the level and shows the result.
// It held that ending itself until Scene 3 existed.
//
// Staging: a fixed shot, no camera follow and no walking. The classroom art is
// a perspective interior whose floor recedes, so a side-on walk across it
// reads as sliding rather than walking - the same reason Bahay Scene 5 is
// staged rather than driven. Kylo is placed, he greets, he sits.
//
// Four sprites were made for this scene and are produced by scripts in Tools/,
// so they can be regenerated rather than hand-edited:
//
//   char_teacher         Tools/cut_teacher.py - traced out of
//                        menu_background.png, the main-menu mockup, which
//                        already had a teacher drawn in this style waving. She
//                        is WAIST-UP, because that mockup crops her at the hip,
//                        which is why she stands behind her desk here.
//   klasrum_desk_front   Tools/cut_classroom_desk.py - NO LONGER USED HERE. It
//                        is a row of desks cut from the old wide classroom and
//                        belongs to that camera; the locker corner has its own
//                        desk in the background. Left on disk for whichever
//                        scene goes back to the wide shot.
//   prop_kuwaderno       Tools/make_classroom_props.py - drawn, not cut. A
//   prop_lapis           pencil is a thin object lying on wood of nearly its
//                        own colour and does not separate the way the bathroom
//                        items did.
//
// Each is still loaded optionally: delete any of them and the scene builds and
// plays, with the word gleaming its place instead.
//
// Background: menu_bg.png is the classroom interior. It is ALSO the main menu's
// background (BuildMainMenuScene.cs), so the menu and this room currently look
// the same. menu_background.png is not an alternative - it is a mockup with the
// title and buttons painted into it. Either generate a new menu background or
// accept the repeat; nothing in this scene depends on which.
//
// Sprites expected in Assets/Sprites/:
//   menu_bg, standing_uniform_with_bag, sitting_desk_uniform,
//   walk_frame_1..4_uniform_with_bag, fx_gleam, ui_arrow, ui_word_badge,
//   badge_paaralan
//
// Both of Kylo's poses here are PROFILES facing right, because that is where
// Teacher Rachel stands. talking_uniform_with_bag is not used in this scene:
// it is a front-facing pose, and it turned him away from her.
//
// Run via: Tools > SALINLAHI > Build Paaralan Scene 2

using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class BuildPaaralanScene2
{
    private const string SpriteFolder = "Assets/Sprites/";
    private const string ScenePath = "Assets/Scenes/Chapter2_Level2_Klasrum.unity";
    private const string NextScene = "Chapter2_Level3_Aralin";

    private const float ViewHeight = 10.8f;           // camera is orthographic 5.4
    private const float ViewWidth = 19.2f;

    // Sizes here are taken off the room rather than guessed. A student desk in
    // menu_bg.png measures 1.7 world units from its top surface to the floor,
    // and a school desk is about 0.70 m, so a metre is roughly 2.4 units
    // anywhere in the middle of this room. That puts a 1.2 m child at 2.85
    // units and a 1.6 m adult at 3.7 - which is what every height below is
    // derived from. Positions are read off the painting in world units.

    // Standing: on the open tiled aisle, facing the front of the class.
    // The first build stood him at x 6.3, y -2.8. That is inside the
    // right-hand desk cluster, where the painting has a desk top and no floor,
    // so he appeared to be standing on the furniture.
    // Measured off klasrum_background: the corridor tiles he stands on when
    // the scene opens, not the classroom floor seen through the doorway. The
    // old -3.15 was fitted to menu_bg, the main menu's room, which stood in
    // here until the classroom art existed.
    private const float FloorY = -5.40f;              // the bottom edge of the frame: this shot has no floor
    private const float StandX = -5.60f;              // he starts at the far left, by the lockers
    private const float PlayerHeight = 5.40f;               // a close shot - he fills most of the frame

    // Seated: at the desk in the background art. Nothing is drawn over his lap
    // - the seated poses are already cropped at the desk line, so the crop
    // itself is the desk edge. He is much nearer the camera
    // there than in the aisle, so he is drawn larger - the same jump between
    // standing and seated spots that Bahay Scene 5 makes. 3.10 is a little
    // above life size; at a strictly correct 2.6 his head barely cleared the
    // desk and the pose was unreadable.
    // The near desk in klasrum_interior_background, measured off the art: its
    // top surface is at y -1.67, the floor under it at -4.21. He sits at that
    // desk once "Upo" has moved the view inside; the old -5.20 was fitted to
    // menu_bg and lands on the open door in the doorway view.
    private const float SitX = 2.86f;                // the chair behind the desk
    private const float SitFeetY = -3.009f;            // the desk's far edge, measured off the art
    // Taller than he looks standing, on purpose: he is nearer the camera here
    // than out by the lockers, so reading larger is correct rather than a
    // cheat.
    //
    // This number is not chosen, it is measured. All three seated poses are
    // cut to one 663x921 canvas whose BOTTOM edge is the desk's far edge and
    // whose top leaves room for the raised pencil in "Lapis". 921 pixels at
    // the 164.2 px-per-world the poses were fitted at is 5.609 world units,
    // and the canvas bottom sits at SitFeetY. Change either without the other
    // and he floats above the desk or sinks through it.
    private const float SeatedHeight = 5.609f;

    // The surface of the desk he sits at, where his things end up. Both sit
    // just below the desk's far edge, so they read as lying on it.
    // Both sit ON the desk top, not on the floor, so their base is the desk
    // surface at -1.67 rather than a height above the ground.
    private const float NotebookX = 1.20f;
    private const float NotebookBaseY = -3.01f;
    private const float NotebookHeight = 0.70f;
    private const float PencilX = 4.60f;
    private const float PencilBaseY = -3.01f;
    private const float PencilHeight = 0.30f;

    // Where the pencil actually is when "Lapis" lands: in his raised hand, not
    // on the desk. The prop above is still built and left switched off, so the
    // gleam cannot follow it.
    //
    // Not estimated - found. The barrel is the only strong yellow anywhere on
    // him, so it can be picked out of pencil_ready_uniform.png by colour: it
    // centres on canvas pixel (119, 418) of 663x921, which at the seated
    // sprite's placement is world (1.56, 0.05), and it stands 1.11 units tall.
    // Re-measure if that pose is ever redrawn.
    private const float PencilInHandX = 1.56f;
    private const float PencilInHandY = 0.05f;
    private const float PencilInHandSize = 1.30f;   // covers the 1.11-unit barrel

    // Teacher Rachel, behind her own desk at the front. char_teacher is a
    // WAIST-UP sprite - the mockup it was cut from crops her at the hip - so
    // her "floor" is her hip line, and the desk top at y -0.6 hides that cut.
    // Standing her anywhere else puts a sliced-off waist in open air.
    // Full body now, so she is measured feet to crown and stands on the floor
    // like anyone else. The old sprite was cropped at the hip - its "floor"
    // was her hip line at -0.70 and 1.95 was hip to crown - and the teacher's
    // desk had to be positioned to hide the cut.
    //
    // 3.58 units comes off the art: the chalkboard behind her is 235px tall
    // and a classroom chalkboard is about 1.2 m, which puts the room at
    // ~196 px per metre, so a 1.6 m adult is ~313 px.
    //
    // She stands partway into the room rather than at the front. That is a
    // compromise: she has to read through the doorway in the first half of
    // the scene AND beside her desk in the second, and one position has to
    // serve both because she is placed once.
    private const float TeacherX = 0.70f;
    private const float TeacherFeetY = -2.00f;
    private const float TeacherHeight = 3.58f;        // feet to crown


    // Nothing here converts pixels to units by hand. CreateFigure sizes each
    // sprite from its own bounds, so the standing art (214x745) and the seated
    // art (295x580) are both placed by the world height they should be and the
    // ground they stand on, and neither a different pixels-per-unit nor a
    // redrawn pose can put a character through the floor. The Bahay builders
    // hardcode a scale per sprite, and two of them had to be corrected when
    // the art turned out not to import at 100 PPU.

    private const int OrderKylo = 0;
    private const int OrderDeskProps = 20;            // notebook and pencil, on top of the desk

    [MenuItem("Tools/SALINLAHI/Build Paaralan Scene 2")]
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
        cam.orthographicSize = ViewHeight * 0.5f;
        cam.transform.position = new Vector3(0, 0, -10);
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = Color.black;
        camGO.tag = "MainCamera";
        camGO.AddComponent<AudioListener>();

        // No CameraFollow: this is a staged shot and nothing walks out of it.

        // --- The classroom, scaled from its own bounds to fill the view ---
        // klasrum_background is the classroom drawn for this scene; menu_bg,
        // the main menu's room, is the stand-in until that exists. Produce the
        // first with Tools/import_klasrum_background.py and this picks it up.
        GameObject bg = new GameObject("Background_Klasrum");
        SpriteRenderer bgSr = bg.AddComponent<SpriteRenderer>();
        // Not "a ?? b": ?? ignores Unity's overloaded null, so a missing asset
        // can slip through it. Ask explicitly.
        // Two views of the same room, and the scene moves between them. The
        // five words split in two: "Bukas" and "Kumusta" happen at the
        // threshold, where a doorway shot is right; "Upo", "Kuha" and "Lapis"
        // happen at a desk, which that shot has no room for. So "Upo" swaps
        // the picture - he sits down AND the view goes in with him, which is
        // what makes the word read as entering rather than just crouching.
        // The locker corner, and only that. The scene used to open outside the
        // room and swap pictures twice; with "Bukas" and "Kumusta" gone there
        // is one room in one state.
        Sprite roomSprite = LoadSpriteOptional("klasrum_interior_background");
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


        // --- Teacher Rachel, behind her desk at the front ---
        // Facing LEFT, because she stands to the right of where Kylo comes in
        // and the greeting is aimed at him. char_teacher_side_left is the
        // right-facing profile mirrored, so it costs no extra drawing.
        // The old waist-up char_teacher is kept as the fallback.
        // No teacher here. She belonged to the version that showed the whole
        // classroom with her at the chalkboard; in a corner of lockers she had
        // nowhere to stand and floated against the doors. The storyboard has
        // only Kylo, which is right: these four words are him getting ready.

        // --- Kylo ---
        // The listening rig is its own object and stays switched on. Bahay
        // Scene 5 learned this the hard way: with VoiceCommand living on the
        // standing pose, sitting down switched that object off and took the
        // component's Update with it, so the Editor's Enter key died from the
        // next word onwards while real speech kept working - which made a
        // disabled component look like a recognition bug.
        GameObject rig = new GameObject("Kylo_Rig");
        rig.transform.position = new Vector3(SitX, SitFeetY, 0f);

        // Both poses are PROFILES facing right, which is where the teacher is.
        // The obvious pick for a word like "Kumusta" would be
        // talking_uniform_with_bag, but that pose faces the VIEWER - using it
        // here turns Kylo's back on the person he is greeting. Scene 1 uses it
        // deliberately, because there the greeting is aimed at the child
        // playing; in this scene it is aimed at Teacher Rachel, so the profile
        // art is the right one and "Kumusta" is answered by her instead.
        GameObject standGO = CreateFigure("Kylo_Standing", "standing_uniform_with_bag",
                                           StandX, FloorY, PlayerHeight);
        standGO.tag = "Player";                        // the body, not the ear

        // sitting_desk_uniform is a real seated pose - knees bent, hands
        // forward, sitting on a chair - so "Upo" no longer has to fake sitting
        // by sliding a standing boy downwards behind the furniture.
        // Facing the CAMERA: this desk faces the viewer, so he must too. The
        // back and side-on seated poses were for scenes where he faced away
        // or across. sitting_desk_uniform stays as the fallback.
        string seatedSprite = LoadSpriteOptional("sitting_front_uniform") != null
            ? "sitting_front_uniform"
            : "sitting_desk_uniform";
        GameObject seatGO = CreateFigure("Kylo_Seated", seatedSprite,
                                          SitX, SitFeetY, SeatedHeight);
        seatGO.SetActive(false);                       // "Upo" turns him on

        // ...and when it does, he lowers into the chair rather than appearing
        // in it. PoseSettle runs on enable, so nothing extra has to be wired
        // to the word itself.
        PoseSettle sit = seatGO.AddComponent<PoseSettle>();
        sit.dropFrom = 0.5f;
        sit.driftFrom = -0.55f;        // he came from the left, down the aisle
        sit.duration = 0.3f;

        // "Kuha" and "Lapis" change his POSE, not a prop on the desk - that
        // is what the storyboard shows. Two swaps on the seated renderer:
        // reaching down into the bag, then sitting up with a pencil. Neither
        // carries a beforeSprite, because CreateFigure has already set the
        // sprite and SpriteSwap.Awake would otherwise fight over it.
        SpriteRenderer seatSr = seatGO.GetComponent<SpriteRenderer>();

        // Both cross-fade rather than cut. These are three drawings of one boy
        // a moment apart, so a hard cut between them reads as a flicker; a
        // short dip through the change reads as him moving.
        SpriteSwap reachSwap = seatGO.AddComponent<SpriteSwap>();
        reachSwap.target = seatSr;
        reachSwap.afterSprite = LoadSpriteOptional("reach_bag_uniform");
        reachSwap.fadeSeconds = 0.22f;

        SpriteSwap pencilSwap = seatGO.AddComponent<SpriteSwap>();
        pencilSwap.target = seatSr;
        pencilSwap.afterSprite = LoadSpriteOptional("pencil_ready_uniform");
        pencilSwap.fadeSeconds = 0.22f;

        // --- No foreground desk layer here ---
        // klasrum_desk_front was cut from the OLD wide classroom shot: a whole
        // front row of desks, spanning the frame. It existed to hide Kylo's lap
        // when he sat in an open aisle. The locker corner has its own desk
        // painted into the background, so that row now lies across the bottom
        // third of a picture it does not belong to and cuts him off at the
        // neck. The occlusion it provided is already handled: the seated poses
        // are cropped at the desk line, so the crop IS the desk edge.

        // --- What comes out of the bag, onto the desk in front of him ---
        // Still optional: if either sprite is missing the word just gleams the
        // desktop, and the scene is playable either way.
        GameObject propKuwaderno = CreatePropOptional("Prop_Kuwaderno", "prop_kuwaderno",
                                                       NotebookX, NotebookHeight, NotebookBaseY);
        GameObject propLapis = CreatePropOptional("Prop_Lapis", "prop_lapis",
                                                   PencilX, PencilHeight, PencilBaseY);

        // PlayerMovement goes on the object that HAS the picture.
        //
        // It used to live on the rig, which carries no SpriteRenderer. That
        // looked harmless - the rig is the thing that moves - but it meant
        // "Lakad" slid an invisible object across the room while the drawing of
        // Kylo stood still at the lockers, and UpdateWalkAnimation returned on
        // its first line because spriteRenderer was null. The four-frame walk
        // cycle that Scene 1 plays had never once run here.
        //
        // Scene 1 has it right: one object carries the renderer, the body and
        // the movement. The rig stays, because VoiceCommand must keep listening
        // after he sits down and this object is switched off at "Upo" - but the
        // rig is now purely the ear, not the body.
        PlayerMovement player = standGO.AddComponent<PlayerMovement>();
        player.standingSprite = LoadSprite("standing_uniform_with_bag");
        player.walkFrames = UniformWalkFrames();
        player.currentWakeStage = PlayerMovement.WakeStage.Standing;
        // All five poses are drawn on the same 745px-tall canvas, so the scale
        // CreateFigure worked out for the standing sprite fits every walk frame
        // and he does not grow or shrink mid-stride.
        //
        // Kinematic: a kinematic Rigidbody2D moves perfectly well when
        // PlayerMovement sets its velocity - what it skips is gravity, which is
        // what we want in a shot whose "floor" is just the bottom of the frame.
        player.GetComponent<Rigidbody2D>().bodyType = RigidbodyType2D.Kinematic;

        // He walks at a child's pace across a close-up shot. The default 5
        // units/second crosses this whole room in under a second.
        player.moveSpeed = 2.2f;
        player.walkFramesPerSecond = 7f;

        // Ease to a stop instead of halting on the spot - see the field's note
        // in PlayerMovement. Only this scene asks for it; everywhere else the
        // default of 0 keeps the old hard stop.
        player.arrivalEaseDistance = 1.1f;

        // Breathing while he waits, so the opening frame is a boy standing
        // there rather than a sticker on the lockers. Idle only: it stops the
        // moment he walks and never fights the walk cycle.
        standGO.AddComponent<IdleBreath>();

        // A marker, not a duration: he walks until he arrives, however far
        // that is.
        //
        // Where he stops is a trade between two things. He sits at x 2.86, so
        // every unit short of that is a sideways jump the player sees the
        // instant he sits; but standing any further right than this draws his
        // legs over the front panel of the desk and he reads as standing
        // inside it. 0.40 is the last position where his body is still clear
        // of the desktop - he arrives at its left edge, which is where a child
        // walking up an aisle would stop before stepping round to the chair.
        GameObject chairSpot = new GameObject("ChairSpot");
        chairSpot.transform.position = new Vector3(0.40f, FloorY, 0f);
        player.walkTarget = chairSpot.transform;
        player.walkTargetThreshold = 0.35f;

        // With a walkTarget set this is only a safety cap - arriving is what
        // ends the walk - so it is deliberately generous. The journey is 6.0
        // units: about 4.6 of them at the full 2.2 units/second, then the last
        // 1.1 easing down to a fifth of that, which comes to roughly 2.9
        // seconds. The old 2.4 was sized for the default speed and would now
        // time out and strand him short of the desk.
        player.voiceWalkDuration = 4.5f;

        VoiceCommand voiceCommand = rig.AddComponent<VoiceCommand>();
        voiceCommand.player = player;

        // --- Something has to light up for every word that only names a thing ---
        TimedOverlay gleamKuha = CreateGleamAt("Gleam_Kuwaderno", NotebookX,
                                                NotebookBaseY + NotebookHeight * 0.5f, 1.2f);
        TimedOverlay gleamLapis = CreateGleamAt("Gleam_Lapis", PencilInHandX,
                                                PencilInHandY, PencilInHandSize);
        // The desk props rest on top of the desk layer, so they and their
        // gleams share the Player layer and sort above it.
        foreach (TimedOverlay fx in new[] { gleamKuha, gleamLapis })
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

        // Canvas units are world units x100. Each prompt sits above the thing
        // it names rather than in a row along the top: the badge is half the
        // teaching, so it has to point at what the word means.
        // Only one is ever on screen at a time, so they can overlap each other
        // freely - each sits directly over the thing its word acts on.
        GameObject pLakad = CreatePromptUI(uiRoot.transform, "Prompt_Lakad", "Lakad", new Vector3(-700, 60, 0));
        GameObject pUpo = CreatePromptUI(uiRoot.transform, "Prompt_Upo", "Upo", new Vector3(260, 120, 0));
        GameObject pKuha = CreatePromptUI(uiRoot.transform, "Prompt_Kuha", "Kuha", new Vector3(120, -150, 0));
        GameObject pLapis = CreatePromptUI(uiRoot.transform, "Prompt_Lapis", "Lapis", new Vector3(420, -40, 0));

        // --- Objectives: open the door, greet, sit, unpack ---
        // Four words now. The scene begins with Kylo already inside, by the
        // lockers, and is about crossing to his seat and getting ready.
        var lakad = MakeObjective("Lakad", pLakad);
        UnityEventTools.AddPersistentListener(lakad.onCorrect, player.WalkForward);

        // Kylo stays in profile, facing her, and SHE answers the greeting - she
        // is drawn mid-wave, so the gleam reads as the wave landing.

        // The view goes inside with him. Swapped before the poses change so
        // the room is already the classroom in the frame he first appears
        // seated in, rather than him sitting down in the doorway and the
        // walls changing round him a frame later.
        var upo = MakeObjective("Upo", pUpo);
        UnityEventTools.AddBoolPersistentListener(upo.onCorrect, standGO.SetActive, false);
        UnityEventTools.AddBoolPersistentListener(upo.onCorrect, seatGO.SetActive, true);

        var kuha = MakeObjective("Kuha", pKuha);
        UnityEventTools.AddPersistentListener(kuha.onCorrect, reachSwap.Apply);
        UnityEventTools.AddPersistentListener(kuha.onCorrect, gleamKuha.Play);
        if (propKuwaderno != null)
            UnityEventTools.AddBoolPersistentListener(kuha.onCorrect, propKuwaderno.SetActive, true);

        // He holds the pencil himself now, so no pencil prop appears on the
        // desk - two pencils would be one too many. propLapis stays built but
        // switched off, for whichever scene needs one lying there.
        var lapis = MakeObjective("Lapis", pLapis);
        UnityEventTools.AddPersistentListener(lapis.onCorrect, pencilSwap.Apply);
        UnityEventTools.AddPersistentListener(lapis.onCorrect, gleamLapis.Play);

        SceneObjectiveController controller = uiRoot.AddComponent<SceneObjectiveController>();
        controller.voiceCommand = voiceCommand;
        controller.playerMovement = player;
        controller.objectives = new[] { lakad, upo, kuha, lapis };

        VoiceUIBuilder.BuildVoiceUI(voiceCommand, controller);
        // Still needed in the scene: SceneResultPanel looks the fader up at
        // run time to fade out of whichever button is pressed.
        SceneFaderBuilder.Build();

        // --- A middle scene of Level 2 now: Scene 3 ends it ---
        // The counter has to be added by hand here. DatabaseDemoBuilder used to
        // bring one along with the result panel, and that panel has moved on to
        // Scene 3 - without this, the five words said in this classroom would
        // vanish from the level's total.
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

        Debug.Log("[SALINLAHI] Paaralan Scene 2 built and saved to " + ScenePath +
                   ". Words: Lakad, Upo, Kuha, Lapis - walking to his chair, sitting, and " +
                   "getting his things out. It fades into " + NextScene +
                   ", which ends the level - so build Paaralan Scene 3 as well, or the " +
                   "level runs into a scene that is not there. Missing art is optional and is " +
                   "picked up automatically: prop_kuwaderno, prop_lapis - see the " +
                   "notes at the top of this file.");
    }

    /// <summary>
    /// One of Kylo's poses, placed by the ground it sits or stands on and
    /// sized by the world height it should be. Every pose is a different
    /// number of pixels tall - standing is 745, seated is 580 - so sizing from
    /// the sprite's own bounds is what lets the two be swapped without either
    /// of them floating or sinking.
    /// </summary>
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

    /// <summary>
    /// A character standing on the floor, sized by the world height it should
    /// be rather than by a scale factor, so the art's resolution does not
    /// decide how big it looks. Returns null when the sprite does not exist
    /// yet, so a scene can be built before all of its cast has been drawn.
    /// </summary>
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

    /// <summary>
    /// A prop resting on a surface, switched off until the word that produces
    /// it. Returns null when its art does not exist yet.
    /// </summary>
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

    /// <summary>A gleam centred on something, for a word that points at it.</summary>
    private static TimedOverlay CreateGleam(string name, GameObject target, float worldSize)
    {
        return CreateGleamAt(name, target.transform.position.x, target.transform.position.y, worldSize);
    }

    /// <summary>
    /// A gleam at a place rather than on an object, for a word that points at
    /// something painted into the background.
    /// </summary>
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
        if (sprite == null)
            Debug.LogWarning("[SALINLAHI] Could not load sprite at " + path);
        return sprite;
    }

    /// <summary>
    /// Like LoadSprite, but silent when the file is absent - for the art this
    /// scene is designed to gain later rather than to require now.
    /// </summary>
    private static Sprite LoadSpriteOptional(string spriteFile)
    {
        return AssetDatabase.LoadAssetAtPath<Sprite>(SpriteFolder + spriteFile + ".png");
    }
}
