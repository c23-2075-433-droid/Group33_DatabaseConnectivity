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
    // BELOW the bottom edge, deliberately. The storyboard this scene is being
    // matched to frames him as a near shot - he walks in cropped at the shin,
    // the way a camera at a child's eye level in a real classroom would catch
    // him. Standing him ON the bottom edge is what forced him small enough to
    // fit the whole body in, which is the thing that made him look like a
    // visitor from a smaller room. 1.75 units of him are below the frame.
    private const float FloorY = -7.15f;
    private const float StandX = -5.60f;              // he starts at the far left, by the lockers
    // Sized against his OWN seated pose, not guessed. sitting_front_uniform is
    // already fitted to this desk - shoulders just clearing the desktop, head
    // against the chair back - so whatever height makes the standing head the
    // same size as the seated head is the height that belongs in this room.
    //
    // A head is the one measurement that cannot change between standing up and
    // sitting down, which is what makes it the right thing to match. The
    // seated canvas is 921px for 5.609 units and the head fills 420 of them,
    // so the head is 2.56 world units. The standing canvas is 745px with the
    // head in the top 193, and 2.56 units of head over that fraction comes to
    // 9.60 units of boy.
    //
    // The old 5.40 put his head at under half the size of his own head when he
    // sat down at the same desk two words later. It read as a small child in a
    // room built for larger ones, and the desk and chair looked like furniture
    // he could not use.
    //
    // 9.80 is what the storyboard asks for, and it only fits because FloorY
    // now sits below the frame. Two things fix it from above and below:
    //
    //   Teacher Rachel's narration plaque is a fixed-height box across the
    //   top of the screen - measured off a running build, it ends 24% of the
    //   way down, at world y +2.79. His crown has to stay under it or his
    //   hair disappears the moment she speaks. That caps the crown at ~+2.65.
    //
    //   The storyboard crops him at the shin, about 18% of his body below the
    //   frame. On this canvas the shoes end 9% up and the socks 13%, so 18%
    //   cuts bare leg - a clean place to lose him rather than through a shoe.
    //
    // Crown at +2.65 with 18% below the bottom edge leaves 9.80 units of boy,
    // and lands his waist on the desk top at -3.12, which is where the
    // storyboard puts it. That last part is the real test of the size: the
    // desk reads as HIS desk rather than as furniture he has wandered past.
    private const float PlayerHeight = 9.80f;

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

    // Where the walk ends: beside the chair, clear of it. The chair back runs
    // from world +0.90 to +4.10 and he is 1.41 wide from centre to leading
    // edge, so -0.51 is as close as he gets without standing on it.
    private const float StandBesideChairX = -0.51f;
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
    //
    // The notebook is now an OPEN book, directly in front of him, because
    // that is what the storyboard's "Lapis" panel shows: ruled pages flat on
    // the desk with the pencil raised over them. prop_kuwaderno was the wrong
    // object for the beat - a shut blue hardback, and parked off at x 1.20
    // where it read as somebody else's book left on the far end of the desk.
    // prop_notebook_open is drawn by Tools/make_open_notebook.py.
    //
    // 2.86 is his own x, so it lands in front of him. The height is the part
    // that matters and it is not a free choice: the seated sprite's BOTTOM
    // EDGE is the desk's far edge at -3.01, so anything that reaches above
    // that line is drawn across his shirt. At 1.30 the book's far edge sat at
    // -2.48, half a unit into his chest, and he wore it like a bib.
    //
    // -3.84 + 0.83 spans the desk top: its far edge lands on -3.01, flush
    // with the bottom of him, and its near edge on -3.84, where the wooden
    // top meets the metal rim. Both edges were read off the painting - the
    // desk's back edge is at -2.92 and its front at about -3.83 - rather
    // than guessed.
    //
    // These numbers only mean anything because prop_notebook_open is cropped
    // to its own ink. It used to carry 28% transparent padding at the top and
    // 17% at the bottom, so the DRAWN book came out far smaller than the
    // world height given here and sat marooned in the middle of the desk with
    // bare wood above and below it.
    //
    // Height is capped by his sprite, which ends at -3.01: anything taller
    // reaches up behind him and gets drawn across his shirt. That cap is why
    // the book is drawn wide and shallow - width is the only room left to
    // give it presence, and a book seen at this angle genuinely is wide.
    private const float NotebookX = 2.86f;
    private const float NotebookBaseY = -3.84f;
    private const float NotebookHeight = 0.83f;
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

    // HOW FAST THE WHOLE SCENE MOVES. 1.0 is the pace this scene was first
    // built at; below 1.0 is slower. Every timing below is derived from it
    // rather than written out, because they are not independent - halve the
    // walk speed without halving the walk's frame rate and his legs cycle at
    // the old rate while he travels at the new one, which is the classic
    // skating foot. Anything measured in SECONDS divides by this; anything
    // measured per second multiplies by it.
    private const float Pace = 0.65f;

    private const int OrderKylo = 0;

    // In front, which is what a thing lying on the desk nearer the camera
    // should be. This was briefly -1, to stop the book covering his hand -
    // but that cured the symptom and not the cause, and left him drawn across
    // the book instead. The real fix was sizing the book so the two do not
    // overlap at all (see NotebookHeight), after which the order is free
    // again.
    private const int OrderDeskProps = 20;

    // The gleams stay on top of everything they point at, so they cannot
    // follow the props behind him.
    private const int OrderGleam = 40;

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
        // in it. Both of these run on enable, so neither has to be wired to
        // the word itself.
        //
        // The drawn descent comes first: three frames of him bending his
        // knees and dropping, generated from the group's storyboard and cut
        // to this canvas by Tools/make_sit_frames.py. His crown steps down
        // +2.55, +2.28, +2.00 and the seated pose catches it at +1.735, with
        // the desk hiding his legs throughout - which is what reads as
        // sitting rather than as one picture replacing another.
        PoseSequence sitFrames = seatGO.AddComponent<PoseSequence>();
        sitFrames.target = seatGO.GetComponent<SpriteRenderer>();
        sitFrames.frames = new[]
        {
            LoadSpriteOptional("sit_frame_1_uniform"),
            LoadSpriteOptional("sit_frame_2_uniform"),
            LoadSpriteOptional("sit_frame_3_uniform"),
        };
        sitFrames.framesPerSecond = 11f * Pace;
        sitFrames.playOnEnable = true;        // "Upo" switches this object on

        // PoseSettle stays, but small. It was carrying the whole illusion
        // before - half a unit of drop and over half a unit of drift - and
        // on top of a real descent that would read as a second dip. What is
        // left is the compression of a body taking its own weight as it
        // lands, which is the one part the drawings do not cover.
        PoseSettle sit = seatGO.AddComponent<PoseSettle>();
        sit.dropFrom = 0.12f;
        // The drift is doing real work now, not decoration. He finishes his
        // walk beside the chair at -0.51 and the seated pose lives at 2.86,
        // so the seated figure starts exactly where he was standing and
        // slides the 3.37 units into the seat while the sitting frames play
        // over it. Without this he would cover that distance in one frame.
        sit.driftFrom = StandBesideChairX - SitX;     // -3.37
        sit.duration = 0.45f / Pace;

        // Both words change his POSE. Two swaps on the seated renderer.
        //
        // reach_bag_uniform was rebuilt for this. The drawing that used to be
        // there was about 81% the scale of the other two seated poses and
        // doubled right over, so pinned to the desk edge it put his chin on
        // the desk and hid the rest of him - and it reached for a bag this
        // scene did not have. The replacement was generated through Canva
        // from sitting_front_uniform (for his design) and the group's
        // storyboard panel (for the pose), and is fitted to this canvas by
        // Tools/make_reach_bag_pose.py, which matches its head to the base
        // pose so he does not change size mid-scene. The backpack is drawn
        // INTO that sprite rather than placed as a prop, so his hand and the
        // bag cannot drift apart.
        SpriteRenderer seatSr = seatGO.GetComponent<SpriteRenderer>();

        // Cross-fades rather than cuts. These are drawings of one boy a moment
        // apart, so a hard cut reads as a flicker; a short dip reads as him
        // moving.
        // "Kuha" now has drawn frames too, so it is a PoseSequence rather
        // than a cross-fade: he turns his shoulders, reaches down, puts his
        // hand in the bag, and lands on reach_bag_uniform holding the
        // notepad. playOnEnable is OFF - this object is already visible by
        // then, so the word drives it.
        // The whole of "Kuha" in one run of frames: he turns to the bag,
        // reaches in, lifts the notepad clear, carries it round in front of
        // him and lays it on the desk - then sits back with his hands down.
        //
        // reach_bag_uniform is one FRAME here rather than the destination it
        // used to be. Ending on it left him frozen holding the notepad over
        // an empty desk, and the book then materialised on the desk a word
        // later when "Lapis" was said. The placing is the part that joins the
        // two beats together, so it belongs inside this one.
        PoseSequence reachFrames = seatGO.AddComponent<PoseSequence>();
        reachFrames.target = seatSr;
        reachFrames.frames = new[]
        {
            LoadSpriteOptional("kuha_frame_1_uniform"),
            LoadSpriteOptional("kuha_frame_2_uniform"),
            LoadSpriteOptional("kuha_frame_3_uniform"),
            LoadSpriteOptional("reach_bag_uniform"),
            LoadSpriteOptional("place_frame_1_uniform"),
            LoadSpriteOptional("place_frame_2_uniform"),
            LoadSpriteOptional("place_frame_3_uniform"),
        };
        reachFrames.destinationSprite = LoadSpriteOptional(seatedSprite);
        reachFrames.framesPerSecond = 11f * Pace;
        reachFrames.playOnEnable = false;
        // Frame 6 is place_frame_3: his arms are down at the desk and the
        // notepad has left his hands. That is the frame the book has to
        // arrive on, so it passes from him to the desk in one step. Firing
        // after the run instead left a frame with the notepad nowhere -
        // gone from his hands and not yet on the desk - which is the seam
        // these drawings exist to remove.
        reachFrames.fireOnFrame = 6;

        // "Lapis" has drawn frames too now: he picks the pencil up low, lifts
        // it past his chest and raises it, landing on pencil_ready_uniform.
        // Like "Kuha" this acts on an object that is already visible, so the
        // word drives it rather than an enable.
        PoseSequence pencilFrames = seatGO.AddComponent<PoseSequence>();
        pencilFrames.target = seatSr;
        pencilFrames.frames = new[]
        {
            LoadSpriteOptional("lapis_frame_1_uniform"),
            LoadSpriteOptional("lapis_frame_2_uniform"),
            LoadSpriteOptional("lapis_frame_3_uniform"),
        };
        pencilFrames.destinationSprite = LoadSpriteOptional("pencil_ready_uniform");
        pencilFrames.framesPerSecond = 11f * Pace;
        pencilFrames.playOnEnable = false;

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
        GameObject propKuwaderno = CreatePropOptional("Prop_Kuwaderno", "prop_notebook_open",
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
        player.moveSpeed = 2.2f * Pace;
        player.walkFramesPerSecond = 7f * Pace;   // scales WITH moveSpeed, or he skates

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
        // He stops BESIDE his chair, not on top of it.
        //
        // Measured off the painting: the chair back occupies world x +0.90 to
        // +4.10. A 9.80-unit Kylo is 2.81 wide, so his leading edge is 1.41
        // ahead of his centre, and -0.51 is the furthest right he can stand
        // with that edge still clear of the chair. He ends up in front of the
        // left end of the desk, which is correct - his feet are below the
        // frame so he is nearer the camera than the furniture - with the
        // whole chair visible beside him.
        //
        // This has now been wrong in both directions. It used to stop at
        // -2.20, well short, leaving a 394px teleport into the seat. Walking
        // him the whole way to 2.86 killed the teleport but parked him
        // standing on the chair he was about to sit in. -0.51 is the position
        // that is actually being described: beside it, about to sit.
        //
        // The 3.37 units still between him and the seat are not jumped - see
        // PoseSettle's drift where the seated pose is built. He slides into
        // the chair as he lowers, which is what sitting down sideways into a
        // school chair looks like.
        GameObject chairSpot = new GameObject("ChairSpot");
        chairSpot.transform.position = new Vector3(StandBesideChairX, FloorY, 0f);
        player.walkTarget = chairSpot.transform;
        player.walkTargetThreshold = 0.35f;

        // With a walkTarget set this is only a safety cap - arriving is what
        // ends the walk - so it is deliberately generous. The journey is now
        // 8.46 units, from -5.60 to 2.86: about 7.4 of them at the full 2.2
        // units/second, then the last 1.1 easing down to a fifth of that,
        // which comes to roughly 4.2 seconds. 4.5 was cutting that far too
        // fine - a cap that expires mid-walk strands him short of the chair,
        // and then he sits somewhere he never reached.
        player.voiceWalkDuration = 6.0f / Pace;   // only a cap; must outlast the walk

        VoiceCommand voiceCommand = rig.AddComponent<VoiceCommand>();
        voiceCommand.player = player;

        // --- Something has to light up for every word that only names a thing ---
        TimedOverlay gleamKuha = CreateGleamAt("Gleam_Kuwaderno", NotebookX,
                                                NotebookBaseY + NotebookHeight * 0.5f, 1.2f);
        TimedOverlay gleamLapis = CreateGleamAt("Gleam_Lapis", PencilInHandX,
                                                PencilInHandY, PencilInHandSize);
        // The gleams sit above everything - above Kylo and above the props -
        // so whatever they are marking is visible through them.
        foreach (TimedOverlay fx in new[] { gleamKuha, gleamLapis })
        {
            fx.overlay.sortingLayerName = "Player";
            fx.overlay.sortingOrder = OrderGleam;
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

        // "Kuha" is him pulling the notepad OUT of the bag - the notepad is
        // in his hand in that drawing, so nothing lands on the desk yet. The
        // open notebook belongs to "Lapis", where the storyboard has it.
        var kuha = MakeObjective("Kuha", pKuha);
        UnityEventTools.AddPersistentListener(kuha.onCorrect, reachFrames.Play);

        // The book arrives on the desk ON the frame where he lays it there,
        // not when the word is said and not after the run has ended. Same
        // for the gleam, which marks the book rather than the word. Before
        // this the book appeared a whole beat later, at "Lapis", with
        // nothing on screen having put it down.
        if (propKuwaderno != null)
            UnityEventTools.AddBoolPersistentListener(reachFrames.onFrameReached,
                                                       propKuwaderno.SetActive, true);
        UnityEventTools.AddPersistentListener(reachFrames.onFrameReached, gleamKuha.Play);

        // He holds the pencil himself now, so no pencil prop appears on the
        // desk - two pencils would be one too many. propLapis stays built but
        // switched off, for whichever scene needs one lying there.
        var lapis = MakeObjective("Lapis", pLapis);
        UnityEventTools.AddPersistentListener(lapis.onCorrect, pencilFrames.Play);
        UnityEventTools.AddPersistentListener(lapis.onCorrect, gleamLapis.Play);
        // The book is already on the desk by now - he put it there himself at
        // the end of "Kuha" - so this word only has to find the pencil.

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
                   "picked up automatically: prop_notebook_open, prop_lapis - see the " +
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
