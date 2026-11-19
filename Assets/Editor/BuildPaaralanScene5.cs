// Assets/Editor/BuildPaaralanScene5.cs
//
// One-click scene builder for Paaralan Scene 5 ("Afternoon Class & Dismissal").
// Teaches five words: Ligpit, Tayo, Kunin, Paalam, Alis.
//
// THE SAME ROOM AND THE SAME DESK AS SCENE 4, which is the whole point of
// this version. The old Scene 5 was staged in klasrum_background - Scene 2's
// wide shot of the whole room, with a floor, a door and a teacher in it - and
// Scene 4 ends on klasrum_interior_background, the close locker-corner shot of
// one desk. Fading from one into the other cut from a boy at his desk to a
// boy across the room, and nothing in the story had moved him. So every
// placement below is Scene 4's, copied rather than re-derived, exactly as
// Scene 4 copied Scene 3's:
//
//     background      klasrum_interior_background
//     desk in front   klasrum_desk_front_near
//     the seat        SitX / SitFeetY / SeatedHeight
//     the lesson      NotebookX, AklatX, PencilX and their bases
//     the camera      a fixed orthographic 5.4, nothing follows
//
// Change one of them here and not in BuildPaaralanScene3 / 4 and the three
// scenes at this desk come apart.
//
// WHAT CHANGED FROM THE OLD SCENE 5, besides the room:
//
//   "Pasok"  gone already, and still gone - the scene opens with him in his
//            seat, so a word for coming in has nothing to do.
//   "Upo"    became "Tayo". He is SITTING when this scene opens, because
//            Scene 4 left him sitting; asking him to sit down again was the
//            one beat the old staging could not justify. Standing up to go
//            home is what actually happens at dismissal.
//   order    Ligpit first, not third. The desk has to be cleared before he
//            gets up - a child does not stand and then tidy sitting down.
//
// WHAT EACH WORD DOES, and which of them are DRAWN:
//
//   Ligpit   DRAWN. He packs the afternoon lesson away - the notebook, then
//            the textbook, then the pencil, each one picked up off the desk
//            and put in his bag, one at a time. This is Scene 4's Ligpit
//            entire: four chained PoseSequences and a PropCarry for the
//            pencil. Docs/AnimationStandard says in as many words that
//            "Paaralan 5's Ligpit and any later 'put in' wants this one",
//            so it is that one and not a re-invention.
//   Tayo     DRAWN. His hands come off the desk and he rises out of the
//            chair into HIS IDLE, FACING FORWARD - arms down at his sides,
//            mouth closed - and stands like that for the rest of the level.
//            tayo_rise_1..3, tayo_over and tayo_stand, built by
//            Tools/make_tayo_frames.py out of drawings this desk already
//            had. The same figure at five heights, so the pose never
//            changes while he rises; see the block at "Tayo" below.
//   Kunin    DRAWN. He looks down at his bag, standing on the strip of
//            desk Ligpit cleared, leans out and puts his hand on it, lifts
//            it by its handle at his side, swings it up over his left
//            shoulder, gets both straps on and pulls them to fit, and
//            stands wearing it. kunin_look, kunin_reach, kunin_lift,
//            kunin_shoulder, kunin_straps and kunin_wear (all _canva).
//   Paalam   DRAWN. His right arm comes up with the elbow bending, the open
//            palm arrives beside his head and WAVES - out, up, in, up, three
//            times over - and comes down again, the other arm hanging
//            relaxed and the bag on his back throughout. paalam_raise,
//            paalam_up, paalam_out and paalam_in.
//   Alis     DRAWN. He turns to his right through three-quarters into
//            profile, facing the left of the picture, takes a first step,
//            and walks the width of the room and off the left edge on a
//            four-frame cycle while the classroom fades to black behind
//            him. alis_turn, alis_side, alis_step and alis_walk_1..4, plus
//            WalkOffAndFade, which carries him and takes the light out.
//
// ALL THREE ARE CANVA DRAWINGS, cut by Tools/make_uwian_canva_frames.py.
// The version before this one made them by cutting the front-on standing
// figure at its shoulder and elbow and turning the pieces, and painting the
// straps on in code: an arm pasted on at a seam, strap ends standing up off
// his shoulders like wings, and a walk that was two legs swinging under a
// boy who never turned round. Moving pieces of one drawing cannot put a bag
// on a back or turn a body. So every pose is now a drawing - six Canva
// generations of four poses each, generated from the Paaralan sprites as
// the design reference so he is the boy the other Paaralan scenes draw, and
// all four poses of an action drawn in ONE pass so they match each other.
// That file says how the full-size pixels get out of Canva, and how the
// frames are registered: one scale per strip, his head at the tayo_*
// frames' size, and every pose stood on one floor and centred by his
// waistband, which is the part of him above the desk that has to hold still.
//
// THE CANVAS IS WIDER, 1263 against the tayo_* frames' 663, for an arm
// that leaves his body and a bag held out beside him - padded equally each
// side, because a sprite draws centred on its object. His crown is the same
// distance above its centre as in every frame before Kunin, so the scene
// can cut between them. His legs run off the bottom of it, which is what a
// close-up does when he walks out from behind the desk.
//
// HIS HEAD AND SKIN ARE HIS OWN. Canva drew the bodies; on every frame its
// head is taken off and Kylo's own put back at the neck - tayo_stand's front
// head, the one he has a moment before Kunin, and Paaralan 2's profile for
// the walk - and the skin is matched to his. Tools/kylo_own_head.py. So the
// join from Tayo into Kunin is the same face, hair and colour.
//
// WHAT THE WORDS LOOK LIKE ON SCREEN is not decided here. SceneObjective
// carries the plain spelling, which is what recognition, WordVariants and the
// word_*.wav names all use; WordStress decides the label, and it marks
// "Tayo" as "Tayô" because tayô (to stand) and táyo (we) are a minimal pair.
// "Paalam" and "Alis" are not pairs and so are shown unmarked, which is that
// file's rule - a mark appears exactly where it is doing work.
//
// EVERY WORD IS VOICED, Ligpit included: word_ligpit.wav and Ligpit's own
// teach and recall lines exist now. A recorded _ask clip SAYS its word, so
// renaming a word means re-voicing its _ask line, not renaming the file -
// rachel_l2_s5_ligpit_ask was Linis's clip renamed, and told the child to
// say Linis until it was re-voiced. The narration around each word - story, ask, done - comes from
// NarratorVoiceBank keyed on this scene's name and the word, so renaming
// Upo to Tayo renames the lines it looks for; Docs/VoiceOverScript.csv has
// been updated to match and the bank needs refreshing after this builds.
//
// THIS SCENE ENDS LEVEL 2 and nothing will take that job from it later.
//
// Sprites expected in Assets/Sprites/: see the SpriteImportFixer.Verify call
// in BuildScene, which is the list that is actually checked.
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

    // --- Scene 4's staging, which is Scene 3's, copied exactly ---
    // The desk's far edge was measured off klasrum_interior_background for
    // Paaralan Scene 2 and has been the seat of every scene at this desk
    // since. BuildPaaralanScene3 has the full working.
    private const float SitX = 2.86f;                 // the chair behind the near desk
    private const float SitFeetY = -3.009f;           // the desk's far edge
    private const float SeatedHeight = 5.609f;

    // --- What is on the desk when the scene opens ---
    // THE AFTERNOON LESSON, in the places the morning one had. Scene 4's
    // numbers, which are Scene 3's: same x, same base, same height, so the
    // first frame of this scene is the last frame of the one before it.
    // "Ligpit" is what clears them.
    private const float NotebookX = 2.86f;
    private const float NotebookBaseY = -3.84f;
    private const float NotebookHeight = 0.83f;

    private const float AklatX = 5.30f;
    private const float AklatBaseY = -3.84f;
    private const float AklatHeight = 1.00f;

    // THE PENCIL IS UNDER THE FIST THAT PICKS IT UP. lapis_frame_1_uniform,
    // the frame "Ligpit" grips on, draws his fingers closed round a pencil at
    // world x 2.54, so the resting pencil is put there and the pickup is a
    // straight lift with no sideways skid. It is PencilGripX below.
    // BuildPaaralanScene3 and 4 carry the same value; the three must agree or
    // the pencil hops as one scene becomes the next.
    private const float PencilX = 2.54f;
    private const float PencilBaseY = -3.35f;
    private const float PencilHeight = 0.30f;

    // --- The pencil's journey into the bag, frame by frame ---
    // Scene 4's measurements, unchanged, because it is the same boy in the
    // same chair putting the same pencil into the same drawn bag. The long
    // version of every number here - fitted to the ERASER, not the barrel;
    // the midpoint under his own forearm rather than an arc through the air;
    // the bag in kuha_frame_3 rather than the one in reach_bag_uniform - is
    // at the top of BuildPaaralanScene4.cs and is worth reading before any of
    // them is touched.
    private const float PencilGripX = 2.54f;
    private const float PencilGripY = -2.77f;
    private const float PencilGripAngle = 133f;       // the drawn barrel's own angle
    private const float PencilCarryMidX = 2.87f;
    private const float PencilCarryMidY = -2.59f;
    private const float BagHandInX = 3.19f;           // where his hand goes in, kuha_frame_3
    private const float BagHandInY = -2.40f;
    private const float PencilDropAngle = 90f;        // point down as it goes in

    // The mouth of his bag, drawn into the bottom right of reach_bag_uniform.
    // Nothing is marked here now that "Kuha" has gone, but the bag is still
    // the place three of Ligpit's legs reach into, so the number stays with
    // the rest of the staging rather than being rediscovered later.
    private const float BagMouthX = 4.01f;
    private const float BagMouthY = -2.58f;

    // --- His bag, which "Kunin" takes off the desk and puts on ---
    // ON THE RIGHT STRIP, where the textbook was. His own sprite covers
    // x 0.84..4.88 and a bag in the middle of the desk would be behind his
    // body; the right strip is clear from the moment Ligpit empties it, which
    // is three words before this one needs it.
    //
    // IT IS HIS OWN BAG, NOT prop_bag. prop_bag is the GREEN canvas satchel
    // drawn for Bahay Scene 4; Kylo's bag, the one he wears for the whole of
    // Paaralan Scene 2 and the one he is wearing by the end of this scene, is
    // NAVY. Picking up one and standing up wearing the other is a cut a child
    // would catch. prop_bag_desk_uniform is that navy bag, by
    // Tools/make_desk_bag.py, with this room's light and a shadow on it - and
    // it is the same design as the one Canva drew in his hands and on his
    // back for "Kunin": front zip, front pocket, top handle.
    //
    // THE DRAWN ONE IS BIGGER. The generator drew the backpack about 2.9
    // units tall, the size a real one is on a boy this size, and this one is
    // 1.66. It is not made bigger, because Ligpit has just packed the lesson
    // into it at this size and the pencil's carry is aimed at its mouth.
    // Instead kunin_lift, the frame that replaces this bag with his, draws
    // his at 2.0, and the shoulder frame after it takes it the rest of the
    // way - a bag growing a fifth a frame while it is swinging is not seen.
    // Tools/make_uwian_canva_frames.py, LIFT_BAG_UNITS.
    //
    // HIS HAND LANDS ON IT, and the reach was drawn before the bag was put
    // here: that file prints kunin_reach's fingertips at 5.51, -2.70, on the
    // top right of the bag at this x, where its strap is. Rule 5 of
    // Docs/AnimationStandard.md - objects meet his hands - is met there.
    private const float BagX = 4.90f;
    private const float BagBaseY = -3.84f;
    private const float BagHeight = 1.66f;

    // AND THE ONE ON THE DESK IS A BIGGER SPRITE THAN THE BAG, because its
    // shadow is drawn into it. Tools/make_desk_bag.py prints both of these
    // and they are the only two numbers that have to come across: the sprite
    // is 1.086 of the bag's height and its base sits 0.086 of that height
    // lower, so the BAG inside it is still BagHeight tall with its feet on
    // BagBaseY and the extra is wood with a shadow on it.
    //
    // A SHADOW IS PART OF THE OBJECT HERE AND NOT A PROP OF ITS OWN. It
    // could be a second sprite under the first, and for a thing that moves
    // it would have to be - but this one stands still until his fist closes
    // on it, and is then replaced by the bag drawn in his hand, which has no
    // shadow because it is in the air. One sprite is one base line to get right
    // instead of three.
    private const float BagDeskHeight = BagHeight * 1.08597f;
    private const float BagDeskBaseY = BagBaseY - BagHeight * 0.08597f;

    // WHERE HIS HAND ARRIVES, printed by Tools/make_uwian_canva_frames.py:
    // kunin_reach's fingertips at (5.51, -2.70), on the top right of the
    // bag where its shoulder strap is. That is close enough to read as
    // taking hold of it, so the bag stays where Ligpit left it and the
    // drawing is not bent to meet it.

    // One knob for the speed of every drawn action in this scene, the same
    // way Paaralan 2, 3 and 4 do it. Anything in SECONDS divides by Pace,
    // anything PER SECOND multiplies by it. Matched to Scene 4's 0.65: it is
    // the same boy at the same desk an afternoon later.
    private const float Pace = 0.65f;

    // The rate every leg of "Ligpit" runs at, as one number rather than the
    // same literal in four places. The pencil's PropCarry is given THIS, not
    // a duration, which is what keeps the prop on his hand: change the Pace
    // and the pose and the pencil slow down together.
    private const float LigpitFps = 9f * Pace;

    // GETTING OUT OF A CHAIR IS SLOWER THAN REACHING ACROSS A DESK, which is
    // Scene 3's finding for its own rise: nine a second, not eleven, or he is
    // on his feet before the child has finished saying the word. Seven frames
    // at this rate is about 1.2 seconds, which is also how long a child takes
    // to stand up without being told to hurry.
    private const float TayoFps = 9f * Pace;

    // TAKING A BAG OFF A DESK IS SLOWER AGAIN. Nine frames at six a second
    // is about 1.5 seconds: a look, a reach, a beat with his hand on it, a
    // lift, a swing, both straps and a tug at them. The look and the hand on
    // the bag are each held for two frames, because a bag lifted on the
    // frame the hand arrives is a bag nobody saw him take hold of.
    private const float KuninFps = 9f * Pace;

    // A WAVE IS FASTER THAN A LIFT and has to be, or it is a hand being held
    // up and moved rather than a wave. The hand goes up at the scene's
    // ordinary rate and then sweeps at nine a second: out, up, in, up is a
    // full wave in about seven tenths of a second - a child's wave.
    private const float PaalamFps = 8f * Pace;
    private const float PaalamWaveFps = 9f * Pace;
    private const int PaalamWaves = 3;

    // AND A WALK IS SLOWER THAN EITHER. Four drawn frames are two steps, so
    // at this rate he takes about two and a half steps a second - the pace a
    // six-year-old leaves a classroom at, and slow enough that the legs read
    // as legs. AlisWalk is how long the crossing itself takes and the two are
    // tied together below: the loop is given as many cycles as fill it, so
    // the legs stop when the boy does however either is retuned.
    private const float AlisFps = 7f * Pace;
    private const float AlisWalk = 3.2f;
    // HE TURNS ON THE SPOT AND MOVES ON HIS FIRST STEP. The turn is two
    // drawn frames - three-quarters, then profile - and WalkOffAndFade is
    // started on the frame after them, alis_step, where his front foot goes
    // out. So the body does not move while it is turning and starts moving
    // the moment a leg does, and nothing has to be timed in seconds for it.

    // OFF THE SIDE OF THE PICTURE, not level with it. The view is 19.2 wide,
    // so its left edge is at -9.6; the widest walking frame's ink is about
    // 720 px on a canvas that draws 921 of them to 5.609 units, 4.4 units
    // wide, so his trailing heel clears the edge at about -11.8, and -12.8
    // leaves a margin. He is gone before the fade begins rather
    // than dissolving with one arm still in shot.
    private const float AlisEndX = -12.8f;

    private const int OrderKylo = 0;
    private const int OrderDeskFront = 10;            // the desk, drawn over him
    private const int OrderDeskProps = 20;            // his things, on the desk
    private const int OrderFlying = 24;               // in front of everything, mid-air

    [MenuItem("Tools/SALINLAHI/Build Paaralan Scene 5")]
    public static void BuildScene()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            Debug.LogError("[SALINLAHI] Stop Play mode first - this rebuilds the scene, which Unity doesn't allow while the game is running.");
            return;
        }

        // This project does not import PNGs as sprites by default, and a
        // SpriteRenderer handed a null sprite renders NOTHING - so missing art
        // here is invisible rather than loud. Make it loadable, and say so if
        // it still is not.
        SpriteImportFixer.Verify(new[]
        {
            // The room and the boy, both Scene 4's.
            "klasrum_interior_background", "klasrum_desk_front_near",
            "sitting_front_uniform", "fx_gleam",
            // What is on the desk when it opens - the lesson, where it was left.
            "prop_notebook_open", "prop_aklat_closed", "prop_lapis",
            // "Ligpit": Paaralan 2's take-it-out movement, reversed. No new art.
            "place_frame_1_uniform", "place_frame_2_uniform", "place_frame_3_uniform",
            "reach_bag_uniform", "kuha_frame_1_uniform", "kuha_frame_2_uniform",
            "kuha_frame_3_uniform",
            "aklat_frame_1_uniform", "aklat_frame_2_uniform", "aklat_bag_uniform",
            "lapis_frame_1_uniform", "lapis_frame_2_uniform",
            // "Tayo": his own rise, built by Tools/make_tayo_frames.py out of
            // the drawings this desk already had. Also no new art.
            "tayo_rise_1_uniform", "tayo_rise_2_uniform", "tayo_rise_3_uniform",
            "tayo_over_uniform", "tayo_stand_uniform",
            // "Kunin", "Paalam" and "Alis": Canva drawings, cut and
            // registered by Tools/make_uwian_canva_frames.py. The bag on the
            // desk is Tools/make_desk_bag.py's, the one Ligpit packs.
            "prop_bag_desk_uniform",
            "kunin_look_canva", "kunin_reach_canva", "kunin_lift_canva",
            "kunin_shoulder_canva", "kunin_straps_canva", "kunin_wear_canva",
            "paalam_raise_canva", "paalam_up_canva",
            "paalam_out_canva", "paalam_in_canva",
            "alis_turn_canva", "alis_side_canva", "alis_step_canva",
            "alis_walk_1_canva", "alis_walk_2_canva",
            "alis_walk_3_canva", "alis_walk_4_canva",
            "ui_arrow", "ui_word_badge", "badge_paaralan",
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
        // NO CameraFollow. Nothing walks in this scene and the camera is
        // Scene 4's, which is a fixed shot of one desk. A follow component
        // with a stationary target does nothing visible and is one more thing
        // that could move the frame away from the one the lesson used.

        // --- The classroom: Scene 4's background sprite, scaled its way ---
        GameObject bg = new GameObject("Background_Klasrum");
        SpriteRenderer bgSr = bg.AddComponent<SpriteRenderer>();
        // klasrum_interior_background is the locker corner every placement in
        // this file, in Scene 3 and in Scene 4 was measured in. menu_bg is the
        // old wide shot and every number here lands somewhere else on it, so
        // it is a last resort that keeps the scene playable rather than one
        // that looks right. klasrum_background - what this scene used to open
        // on - is deliberately NOT the fallback: it is a different room at a
        // different scale and that is the bug this version fixes.
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

        // --- The desk, drawn IN FRONT of him ---
        // The seated poses are cut off flat below the sleeve so his arms read
        // as running down behind the desk, and that only works if something
        // covers the cut. klasrum_desk_front_near is this room's own desk cut
        // out of its own painting and written to the background's full
        // canvas, so it takes the background's transform and the two register
        // pixel for pixel. BuildPaaralanScene3 has the whole story.
        GameObject deskFront = new GameObject("Foreground_Desk");
        SpriteRenderer deskFrontSr = deskFront.AddComponent<SpriteRenderer>();
        deskFrontSr.sprite = LoadSpriteOptional("klasrum_desk_front_near");
        deskFrontSr.sortingLayerName = "Player";
        deskFrontSr.sortingOrder = OrderDeskFront;
        if (deskFrontSr.sprite == null) deskFront.SetActive(false);
        else deskFront.transform.localScale = bg.transform.localScale;

        // --- Kylo, already seated ---
        // The listening rig stays switched on and separate from the pose, the
        // lesson Bahay Scene 5 taught: with VoiceCommand on an object that
        // gets switched off, its Update stops and the Editor's Enter key dies
        // while real speech keeps working. It matters more here than it did in
        // Scene 4, because "Alis" switches the figure off for good.
        GameObject rig = new GameObject("Kylo_Rig");
        rig.tag = "Player";
        rig.transform.position = new Vector3(SitX, SitFeetY, 0f);

        // ONE FIGURE, NOT THREE. The old Scene 5 had a seated Kylo, a
        // standing one and a standing-with-bag one, and switched between
        // whole GameObjects. At this desk that cannot work: the three
        // drawings disagree about scale and about where the floor is, and
        // only one of them was ever fitted to this shot. So there is a single
        // SpriteRenderer and PoseSequence swaps its sprite, which is what
        // every other scene at this desk does.
        //
        // The scale comes off sitting_front_uniform's 663x921 canvas. The
        // tayo_* frames are 663x1521 - the same canvas with 300 rows added
        // at EACH end, so the figure sits in the same place in it - and at
        // this scale they draw him standing, risen 0.89 units, with the
        // desk's far edge crossing him at the top of his shorts. That is
        // Tools/make_tayo_frames.py's whole job and why the padding is
        // symmetric: a sprite draws centred on its object, so padding one
        // end only would slide him sideways.
        GameObject seatGO = CreateFigure("Kylo_Seated", "sitting_front_uniform",
                                          SitX, SitFeetY, SeatedHeight);
        SpriteRenderer seatSr = seatGO.GetComponent<SpriteRenderer>();

        // --- What is on the desk ---
        GameObject propNotebook = CreatePropOptional("Prop_Kuwaderno", "prop_notebook_open",
                                                      NotebookX, NotebookHeight, NotebookBaseY);
        GameObject propAklat = CreatePropOptional("Prop_Aklat", "prop_aklat_closed",
                                                   AklatX, AklatHeight, AklatBaseY);
        GameObject propPencil = CreatePropOptional("Prop_Lapis", "prop_lapis",
                                                    PencilX, PencilHeight, PencilBaseY);
        foreach (GameObject prop in new[] { propNotebook, propAklat, propPencil })
            if (prop != null) prop.SetActive(true);
        // The pencil lies on the notebook, so it has to beat the notebook.
        if (propPencil != null)
            propPencil.GetComponent<SpriteRenderer>().sortingOrder = OrderDeskProps + 1;

        // Switched off until "Kunin" puts it there.
        GameObject propBag = CreatePropOptional("Prop_Bag", "prop_bag_desk_uniform",
                                                 BagX, BagDeskHeight, BagDeskBaseY);
        // BEHIND THE LESSON, IN FRONT OF THE DESK. Its shadow lies on the
        // wood, so it has to beat the desk; the textbook it shares the right
        // strip with is cleared by Ligpit before this word is ever asked, so
        // nothing of its own order matters beyond that.
        if (propBag != null)
            propBag.GetComponent<SpriteRenderer>().sortingOrder = OrderDeskProps;

        // --- The pencil, which is CARRIED rather than thrown ---
        // Its own GameObject, separate from the one lying on the desk,
        // because PropCarry owns its target's position and puts it back where
        // it started when it is done - sharing one object with the desk would
        // leave the desk's pencil hanging in mid-air on a replay.
        //
        // A carry has a WAYPOINT PER DRAWN FRAME and no duration of its own,
        // which is the whole reason it is not a PropToss: a toss is given a
        // time and arrives when its own clock says to, which in Scene 4's
        // first pass was after his hand had been in the bag and come back out.
        PropCarry pencilCarry = CreateCarry("Carry_Lapis", "prop_lapis", PencilHeight,
                                             new[]
                                             {
                                                 // kuha_frame_1: still where
                                                 // lapis_frame_1 drew it.
                                                 new Vector3(PencilGripX, PencilGripY, 0f),
                                                 // kuha_frame_2: under his
                                                 // reaching forearm.
                                                 new Vector3(PencilCarryMidX, PencilCarryMidY, 0f),
                                                 // kuha_frame_3: at the mouth
                                                 // of the bag, as his hand
                                                 // goes into it.
                                                 new Vector3(BagHandInX, BagHandInY, 0f),
                                             }, PencilGripAngle);
        pencilCarry.framesPerSecond = LigpitFps;
        pencilCarry.endAngle = PencilDropAngle;
        // A frame and a half to go in, which lands it out of sight while his
        // hand is still drawn inside the bag - the next frame after that is
        // the hand coming back out, and it has to come out empty.
        pencilCarry.insertDuration = 1.5f / LigpitFps;
        pencilCarry.insertDrop = 0.30f;
        pencilCarry.insertScale = 0.65f;

        // ================= "Ligpit": the lesson goes in the bag =============
        //
        // Four PoseSequences, chained end to end, because PoseSequence fires
        // ONE frame event and three things have to leave the desk at three
        // different moments:
        //
        //   A  the notebook   place_frame_*  +  reach_bag  +  kuha_frame_*
        //   B  the textbook   aklat_frame_*  in place of three of those
        //   C1 the pencil     reached for, gripped and lifted off the desk
        //   C2               carried to the bag, put in, let go, hand back
        //
        // Legs A and B are Paaralan Scene 2's "Kuha" with the frame list
        // written the other way round, and that is the whole trick: moving a
        // flat object between a desk and a bag is the same movement in both
        // directions. The join between legs is seamless because PoseSequence
        // sets running = true synchronously inside Play(), so
        // SceneObjectiveController never sees a gap and never asks for the
        // next word early.
        PoseSequence ligpitA = AddPose(seatGO, seatSr, new[]
        {
            "place_frame_3_uniform",    // hands flat on the desk
            "place_frame_2_uniform",    // the notebook comes up into both hands
            "place_frame_1_uniform",    // held up beside his shoulder
            "reach_bag_uniform",        // and down into the bag
            "kuha_frame_3_uniform",     // his hand is in the bag, empty
            "kuha_frame_2_uniform",     // coming back out
            "kuha_frame_1_uniform",     // and down by his side
            "place_frame_3_uniform",    // hands back on the desk for the next one
        }, "place_frame_3_uniform", LigpitFps);
        // Frame 1 is place_frame_2, where the notebook is IN HIS HANDS. That
        // is the frame it has to leave the desk on - one either side and it
        // is either in two places or in none.
        if (propNotebook != null)
        {
            ligpitA.fireOnFrame = 1;
            UnityEventTools.AddBoolPersistentListener(
                ligpitA.onFrameReached, propNotebook.SetActive, false);
        }

        PoseSequence ligpitB = AddPose(seatGO, seatSr, new[]
        {
            "place_frame_3_uniform",
            "aklat_frame_2_uniform",    // the textbook, square in front of him
            "aklat_frame_1_uniform",    // up beside his shoulder
            "aklat_bag_uniform",        // gripped at the mouth of the bag
            "kuha_frame_3_uniform",
            "kuha_frame_2_uniform",
            "kuha_frame_1_uniform",
            "place_frame_3_uniform",
        }, "place_frame_3_uniform", LigpitFps);
        if (propAklat != null)
        {
            ligpitB.fireOnFrame = 1;
            UnityEventTools.AddBoolPersistentListener(
                ligpitB.onFrameReached, propAklat.SetActive, false);
        }

        // C1: HE REACHES FOR IT, CLOSES HIS FINGERS ON IT, AND LIFTS IT.
        // All three beats are drawn and the pencil is in the drawing for two
        // of them, so there is nothing for a prop to do here at all.
        PoseSequence ligpitC1 = AddPose(seatGO, seatSr, new[]
        {
            "place_frame_3_uniform",    // he reaches - both hands onto the desk
            "lapis_frame_1_uniform",    // fingers closed round the pencil, on the desk
            "lapis_frame_2_uniform",    // and up off the desk, held at his chest
        }, "lapis_frame_2_uniform", LigpitFps);
        // Frame 1 is lapis_frame_1, where the pencil is drawn IN HIS FINGERS.
        // That is the frame the desk's own pencil has to go on.
        if (propPencil != null)
        {
            ligpitC1.fireOnFrame = 1;
            UnityEventTools.AddBoolPersistentListener(
                ligpitC1.onFrameReached, propPencil.SetActive, false);
        }

        // C2: HE TAKES IT TO THE BAG, PUTS IT IN, AND LETS GO INSIDE.
        //
        // IT OPENS ON lapis_frame_1 AGAIN, and that frame is the hinge of the
        // whole beat. C1 left him holding the pencil up at his chest; this
        // brings it back DOWN to the desk, where it is still drawn in his
        // fingers, and that is the last place the drawing can put it. The
        // prop is handed over from exactly there on the next frame, so the
        // pencil is never in two places and never in none.
        //
        // NO kuha_frame_1 ON THE WAY OUT. It is the frame he turns to the bag
        // on and the right drawing for the beat, but neither of his hands is
        // anywhere near the pencil in it, so the prop sat on his shirt with
        // nothing holding it. The reach out is kuha_frame_2 held for the two
        // frames the pencil takes to cross, which is a boy reaching into a
        // bag. It is still there on the way BACK, where his hand is empty and
        // it does not matter.
        PoseSequence ligpitC2 = AddPose(seatGO, seatSr, new[]
        {
            "lapis_frame_1_uniform",    // down to the desk again, pencil still drawn in his hand
            "kuha_frame_2_uniform",     // he reaches for the bag - and the prop takes the pencil on
            "kuha_frame_2_uniform",     // held: the pencil crosses, along the underside of his arm
            "kuha_frame_3_uniform",     // his hand goes IN, and the pencil is at the mouth with it
            "kuha_frame_3_uniform",     // held: it goes down inside, and he lets go of it there
            "kuha_frame_2_uniform",     // only now does the hand come back out, empty
            "kuha_frame_1_uniform",     // and he turns back
            "place_frame_3_uniform",    // and down on the desk
        }, "sitting_front_uniform", LigpitFps);
        // Frame 1, the first frame whose drawing has no pencil in it. The
        // carry's first waypoint is PencilGrip, which is exactly where
        // lapis_frame_1 drew the pencil on the frame before, so the drawn
        // pencil and the carried one are the same pencil in the same place on
        // consecutive frames. None of the timings are durations anyone chose:
        // they fall out of LigpitFps, which is this pose's own rate.
        ligpitC2.fireOnFrame = 1;
        UnityEventTools.AddPersistentListener(ligpitC2.onFrameReached, pencilCarry.Play);

        // The chain. onFinished runs after the idle pose is set and after
        // running has gone false, so the next leg's Play() is free to take
        // the renderer.
        UnityEventTools.AddPersistentListener(ligpitA.onFinished, ligpitB.Play);
        UnityEventTools.AddPersistentListener(ligpitB.onFinished, ligpitC1.Play);
        UnityEventTools.AddPersistentListener(ligpitC1.onFinished, ligpitC2.Play);

        // ================= "Tayo": he comes up out of the chair =============
        //
        //   place_frame_3     both hands flat on the desk, where Ligpit left
        //                     him - the starting pose, so the join is silent
        //   sitting_front     his hands come off the desk to his sides
        //   tayo_rise_1..3    up out of the seat, eased: 0.22, 0.56, 0.86
        //   tayo_over         a little past where he ends up
        //   tayo_stand        and he settles onto his feet
        //
        // THE POSE DOES NOT CHANGE WHILE HE RISES, and that is the whole
        // difference between this and the version it replaces. Scene 5 used
        // to borrow Scene 3's "Sagot" rise, which had to swap drawings on the
        // first frame of the movement - a seated boy, then a different,
        // standing one - and hide the swap inside the fastest moment of the
        // beat. Every frame here is the SAME figure at a different height, so
        // there is nothing to hide: he comes up, and more of him appears from
        // behind the desk, which is what standing up looks like.
        //
        // HE IS LEFT IN HIS IDLE, FACING FORWARD. destinationSprite is
        // tayo_stand_uniform - arms down at his sides, mouth closed - so
        // Kunin, Paalam and Alis all happen with him standing calmly rather
        // than holding the pose of someone mid-sentence. The old destination
        // was sagot_stand_uniform, which is talking_uniform entire: hands up
        // by his chest and his mouth open, right for answering a question in
        // Scene 3 and wrong for going home. IdleBreath, below, is what keeps
        // that rest alive.
        //
        // THE FIGURE IS BUILT, NOT DRAWN, and Tools/make_tayo_frames.py has
        // the whole of how: sitting_front_uniform from the crown to
        // mid-forearm, talking_uniform's shirt hem and shorts grafted on at
        // the SHIRT where the two drawings measure the same, his own forearms
        // carried down, and standing_uniform's hanging hand on the end of
        // each. His fingertips finish behind the desk, which is where a boy
        // standing at his own desk has them.
        //
        // PLAYED ONCE, not looped. Standing up happens once. The settle is a
        // tail frame rather than a sixth frame of the rise, so the sequence
        // comes to rest on the drawing it rests on.
        PoseSequence tayoFrames = AddPose(seatGO, seatSr, new[]
        {
            "place_frame_3_uniform",    // hands flat - where Ligpit left him
            "sitting_front_uniform",    // and off the desk, down by his sides
            "tayo_rise_1_uniform",      // up out of the chair
            "tayo_rise_2_uniform",
            "tayo_rise_3_uniform",
            "tayo_over_uniform",        // a little past it
        }, "tayo_stand_uniform", TayoFps);
        tayoFrames.tailFrames = Sprites("tayo_stand_uniform");   // and down onto his feet

        // ================= "Kunin": he takes his bag and puts it on =========
        //
        //   tayo_stand        where Tayo left him
        //   kunin_look  x2    he looks down at the bag beside him
        //   kunin_reach x2    leans out and his hand is on it - held, so the
        //                     child sees him take hold before it moves
        //   kunin_lift        up off the desk by its handle, at his side
        //   kunin_shoulder    swung up over his left shoulder
        //   kunin_straps x2   both straps on, pulled to fit
        //   kunin_wear        standing in it, arms at his sides
        //
        // EVERY POSE IS A DRAWING. The bag in his hand, on his shoulder and
        // on his back is drawn into the frames, so it cannot lag his hand,
        // drift off his shoulder or stick up past it, which is everything
        // the cut-and-turned version and its carried prop got wrong. There
        // is no bag object in this beat but the one on the desk.
        //
        // PLAYED ONCE. Picking something up happens once, and
        // Docs/AnimationStandard.md lists it among the things that must not
        // loop.
        PoseSequence kuninFrames = AddPose(seatGO, seatSr, new[]
        {
            "tayo_stand_uniform",       // where Tayo left him
            "kunin_look_canva",         // he looks at his bag
            "kunin_look_canva",
            "kunin_reach_canva",        // leans out, hand on it
            "kunin_reach_canva",
            "kunin_lift_canva",         // and up off the desk
            "kunin_shoulder_canva",     // over his shoulder
            "kunin_straps_canva",       // both straps, pulled to fit
            "kunin_straps_canva",
        }, "kunin_wear_canva", KuninFps);
        kuninFrames.tailFrames = Sprites("kunin_wear_canva");

        // THE HAND-OVER IS ON THE LIFT. The bag on the desk has a shadow and
        // the one in his hand must not, so the frame they swap on is the
        // frame it leaves the wood - a frame earlier and the shadow blinks
        // out from under a bag that has not moved.
        kuninFrames.fireOnFrame = 5;                       // kunin_lift
        if (propBag != null)
            UnityEventTools.AddBoolPersistentListener(kuninFrames.onFrameReached,
                                                       propBag.SetActive, false);

        // ================= "Paalam": he waves goodbye ======================
        //
        //   kunin_wear        where Kunin left him, wearing the bag
        //   paalam_raise      the arm coming up, elbow bent, hand at his
        //                     shoulder - so it goes up rather than appears up
        //   paalam_up         the open palm beside his head
        //   out/up/in/up      THE WAVE, looped three times
        //   paalam_raise      and the hand comes back down
        //
        // IT IS A LOOP AND THAT IS THE POINT. A hand raised and held is not a
        // goodbye; what makes one read is the hand going BACK, and back
        // again. PoseSequence has a looping body for exactly this, so the
        // drawn positions of the sweep are its loopFrames. paalam_up appears
        // twice in the loop, on the way out and on the way back, so the hand
        // passes through the middle rather than jumping across it.
        //
        // HIS OTHER ARM HANGS STILL and the bag is drawn on his back in every
        // frame, so nothing but the waving arm moves.
        PoseSequence paalamFrames = AddPose(seatGO, seatSr, new[]
        {
            "kunin_wear_canva",         // where Kunin left him
            "paalam_raise_canva",       // the arm comes up, elbow bent
            "paalam_up_canva",          // palm beside his head
        }, "kunin_wear_canva", PaalamFps);
        paalamFrames.loopFrames = Sprites("paalam_out_canva",
                                           "paalam_up_canva",
                                           "paalam_in_canva",
                                           "paalam_up_canva");
        paalamFrames.loopCycles = PaalamWaves;
        paalamFrames.loopFramesPerSecond = PaalamWaveFps;
        // DOWN IN ORDER: back through the raise to the hanging arm.
        //
        // AND IT DOES NOT BEGIN ON paalam_up, which is what it did and what
        // made the wave hitch at the end of its last sweep. The loop's LAST
        // frame is ALREADY paalam_up - out, up, in, up - so a tail starting
        // there drew the same open palm twice running, once at the loop's
        // rate and once at the lead-in's: 0.363 seconds of a hand held
        // perfectly still, half a sweep's worth, in the middle of the one
        // movement whose whole point is that the hand keeps going back.
        //
        // The tail is the LEAD-IN REVERSED MINUS whatever frame the loop has
        // already left on screen. Write it the other way round - reverse the
        // lead-in and stop - and this is the frame that comes back, because
        // the lead-in ends where the loop ends. The same is true of any
        // sequence here whose loop passes back through its own lead-in.
        paalamFrames.tailFrames = Sprites("paalam_raise_canva",
                                           "kunin_wear_canva");

        // ================= "Alis": he goes ==================================
        //
        //   kunin_wear    standing at his desk, wearing it
        //   alis_turn     turned three-quarters toward the left - head,
        //                 shoulders, body and feet together
        //   alis_side     in profile, facing the way out
        //   alis_step     his first step, and the moment he starts moving
        //   walk_1..4     the walk, looped, while WalkOffAndFade carries him
        //                 across the room and off the side of it
        //
        // TWO COMPONENTS, ONE BEAT. The legs are a PoseSequence like every
        // other drawn action in the project; the crossing is a
        // WalkOffAndFade, which moves the transform and nothing else. They
        // are started by the same word and tuned to the same length, which is
        // the whole of why it reads as walking rather than as sliding: the
        // feet change while the boy moves.
        //
        // SO THE CYCLES ARE COUNTED RATHER THAN CHOSEN. The walk lasts
        // AlisWalk seconds, the loop runs at AlisFps, and four frames make a
        // cycle - so retuning the pace or the distance keeps them in step
        // without anyone remembering to.
        int alisCycles = Mathf.Max(1, Mathf.RoundToInt(AlisWalk * AlisFps / 4f));
        PoseSequence alisFrames = AddPose(seatGO, seatSr, new[]
        {
            "kunin_wear_canva",
            "alis_turn_canva",          // he turns toward the left
            "alis_side_canva",          // and faces the way out
            "alis_step_canva",          // and steps
        }, null, AlisFps);
        alisFrames.loopFrames = Sprites("alis_walk_1_canva",
                                         "alis_walk_2_canva",
                                         "alis_walk_3_canva",
                                         "alis_walk_4_canva");
        alisFrames.loopCycles = alisCycles;
        // NO TAIL AND NO DESTINATION. He does not arrive anywhere and he does
        // not settle: he is off the side of the picture by the time the loop
        // ends, and the last thing anyone sees of him is a frame of the walk.
        // A destination sprite here would stand him up again somewhere
        // outside the view, which costs nothing and means nothing - except on
        // a replay, where the scene puts him back at his desk itself.

        // THE WAVE IS INTERRUPTED RATHER THAN CUT. If the child says "Alis"
        // while he is still waving, Play() on the same renderer would take it
        // mid-sweep with his hand in the air. `interrupts` ends the wave
        // first - Stop(), so its own lowering tail plays - and starts the
        // walk once it has landed. It is the same wiring Paaralan Scene 3
        // uses to close the book before the pencil comes out.
        alisFrames.interrupts = paalamFrames;

        // --- The room goes dark behind him ---
        // ITS OWN OVERLAY, not SceneFader's. SceneFader sorts at 999, above
        // the level-complete panel, so fading with it and leaving it black
        // would black out the summary as well - and its FadeOutAndLoad is for
        // leaving for another scene, which this is not. This one sorts BELOW
        // the panels and above the room, so the classroom goes out and the
        // summary comes up over it.
        GameObject fadeCanvasGO = new GameObject("UI_UwianFade_Canvas");
        Canvas fadeCanvas = fadeCanvasGO.AddComponent<Canvas>();
        fadeCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        fadeCanvas.sortingOrder = -1;
        CanvasScaler fadeScaler = fadeCanvasGO.AddComponent<CanvasScaler>();
        fadeScaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        fadeScaler.referenceResolution = new Vector2(1920, 1080);
        GameObject fadeGO = new GameObject("Fade", typeof(RectTransform));
        fadeGO.transform.SetParent(fadeCanvasGO.transform, false);
        Image fadeImage = fadeGO.AddComponent<Image>();
        fadeImage.color = new Color(0f, 0f, 0f, 0f);
        fadeImage.raycastTarget = false;
        RectTransform fadeRect = fadeGO.GetComponent<RectTransform>();
        fadeRect.anchorMin = Vector2.zero;
        fadeRect.anchorMax = Vector2.one;
        fadeRect.offsetMin = Vector2.zero;
        fadeRect.offsetMax = Vector2.zero;

        WalkOffAndFade alisWalk = seatGO.AddComponent<WalkOffAndFade>();
        alisWalk.figure = seatGO.transform;
        alisWalk.endX = AlisEndX;
        alisWalk.startDelay = 0f;
        alisWalk.duration = AlisWalk;
        alisWalk.fadeImage = fadeImage;

        // THE WALK STARTS FROM THE ANIMATION, NOT FROM THE WORD, and the
        // difference only shows in the one case that matters. `interrupts`
        // above can hold the pose back while the wave lowers his hand; a
        // Begin() wired straight to the objective would not wait with it, and
        // he would set off across the room still waving. Hung on his first
        // step instead, the two cannot come apart however long the wave
        // takes to land - he turns, steps, and on that frame he starts moving.
        alisFrames.fireOnFrame = 3;                        // alis_step
        UnityEventTools.AddPersistentListener(alisFrames.onFrameReached, alisWalk.Begin);

        // He is never completely still between words.
        seatGO.AddComponent<IdleBreath>();

        // Nothing walks in this scene, so Kylo needs no physics. PlayerMovement
        // is here because VoiceCommand needs one and the objective controller
        // reads it to know when an action has finished.
        PlayerMovement player = rig.AddComponent<PlayerMovement>();
        player.standingSprite = LoadSprite("sitting_front_uniform");
        player.currentWakeStage = PlayerMovement.WakeStage.Standing;
        rig.GetComponent<Rigidbody2D>().bodyType = RigidbodyType2D.Kinematic;

        VoiceCommand voiceCommand = rig.AddComponent<VoiceCommand>();
        voiceCommand.player = player;

        // --- What lights up for each word ---
        // Centred on the DRAWN object, not on the middle of its padded sprite
        // box - prop_lapis in particular is 440x150 with its ink in a band
        // half that high.
        TimedOverlay gleamLigpit = CreateGleamAt("Gleam_Ligpit", NotebookX,
                                                  NotebookBaseY + NotebookHeight * 0.5f, 3.0f);
        // On Kylo, because the word is about him and not about an object.
        // Scene 3's "Sagot" gleam, at the same fraction of his seated height.
        TimedOverlay gleamTayo = CreateGleamAt("Gleam_Tayo", SitX,
                                                SitFeetY + SeatedHeight * 0.72f, 2.8f);
        // On the bag, on the strip Ligpit cleared.
        TimedOverlay gleamKunin = CreateGleamAt("Gleam_Kunin", BagX,
                                                 BagBaseY + BagHeight * 0.5f, 2.2f);
        // On Kylo again, and higher, because by now he is standing: the rise
        // is 0.89 world units and this clears his chest at the new height.
        // There is nothing else in this view for a goodbye to land on - no
        // door, no teacher, no window - which is the same corner Scene 3's
        // "Sagot" was in.
        TimedOverlay gleamPaalam = CreateGleamAt("Gleam_Paalam", SitX,
                                                  SitFeetY + SeatedHeight * 0.72f + 0.89f, 2.8f);
        // On the desk he leaves behind, wide enough to read as the whole of
        // it rather than as a mark on one object.
        TimedOverlay gleamAlis = CreateGleamAt("Gleam_Alis", NotebookX,
                                                NotebookBaseY + 0.40f, 3.4f);

        // CreateGleamAt leaves them on the "Props" layer, which this project
        // sorts BELOW "Player" - any left there is drawn behind Kylo and
        // behind the desk in front of him. Every one of these marks something
        // on the desk or on Kylo, so every one is moved up.
        foreach (TimedOverlay fx in new[] { gleamLigpit, gleamTayo, gleamKunin,
                                            gleamPaalam, gleamAlis })
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

        if (Object.FindAnyObjectByType<UnityEngine.EventSystems.EventSystem>() == null)
        {
            GameObject es = new GameObject("EventSystem");
            es.AddComponent<UnityEngine.EventSystems.EventSystem>();
            es.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
        }

        // Only one is on screen at a time, so they overlap freely - each sits
        // over the thing its word acts on. The canvas is scaled 0.01, so
        // these are world units x 100.
        //
        // Two things are out of bounds, both of them Scene 3's rules because
        // it is Scene 3's frame: nothing above y 265, where the narration
        // plaque is, and nothing between x 84 and 488, which is the width of
        // his own sprite - a badge there is a label pinned to his chest. The
        // second one holds while he is STANDING too: the tayo_* frames are
        // the same 663 pixels wide at the same scale, so he occupies the same
        // column, just more of it.
        GameObject pLigpit = CreatePromptUI(uiRoot.transform, "Prompt_Ligpit", "Ligpit", new Vector3(-150, 120, 0));
        GameObject pTayo = CreatePromptUI(uiRoot.transform, "Prompt_Tayo", "Tayo", new Vector3(610, 60, 0));
        GameObject pKunin = CreatePromptUI(uiRoot.transform, "Prompt_Kunin", "Kunin", new Vector3(610, -140, 0));
        GameObject pPaalam = CreatePromptUI(uiRoot.transform, "Prompt_Paalam", "Paalam", new Vector3(-150, -60, 0));
        GameObject pAlis = CreatePromptUI(uiRoot.transform, "Prompt_Alis", "Alis", new Vector3(-150, -230, 0));

        // --- Objectives: tidy, stand, take the bag, say goodbye, go ---
        //
        // IN THE ORDER DISMISSAL ACTUALLY HAPPENS IN, which is also the order
        // the objects need to be in: the desk has to be cleared before he can
        // get up from it, he has to be on his feet before his bag can come up
        // onto the clear desk, and the room cannot empty until he has said
        // goodbye to it.
        //
        // EVERY WORD DRIVES ITS OWN POSE SEQUENCE AND NOTHING ELSE'S, which
        // is what keeps the beats from overlapping: Play() takes the renderer
        // off any other action running on the same figure. The only crossings
        // are the Ligpit chain's own, and those wait rather than cut.
        //
        // The prop switching for Ligpit is NOT wired here. It belongs to the
        // frame of the animation where the object changes hands, which is why
        // it is on fireOnFrame up with each sequence instead.
        var ligpit = MakeObjective("Ligpit", pLigpit);
        UnityEventTools.AddPersistentListener(ligpit.onCorrect, ligpitA.Play);
        UnityEventTools.AddPersistentListener(ligpit.onCorrect, gleamLigpit.Play);

        var tayo = MakeObjective("Tayo", pTayo);
        UnityEventTools.AddPersistentListener(tayo.onCorrect, tayoFrames.Play);
        UnityEventTools.AddPersistentListener(tayo.onCorrect, gleamTayo.Play);

        // DRAWN, and the bag is on the desk BEFORE the word is said.
        //
        // onShown is what makes the difference between a bag he takes and a
        // bag that appears in his hand: it fires as the badge goes up and
        // Rachel introduces the word, so the thing is on the strip Ligpit
        // cleared while the child is being told what it is called, and the
        // reach that follows is a reach for something that was already there.
        // Bahay Scene 4 brings its bag in the same way and for the same
        // reason. It can be called twice if the word is put back up, and
        // switching an object on twice is safe.
        var kunin = MakeObjective("Kunin", pKunin);
        if (propBag != null)
            UnityEventTools.AddBoolPersistentListener(kunin.onShown, propBag.SetActive, true);
        UnityEventTools.AddPersistentListener(kunin.onCorrect, kuninFrames.Play);
        UnityEventTools.AddPersistentListener(kunin.onCorrect, gleamKunin.Play);

        // DRAWN, and it is a wave rather than a raised hand: three sweeps of
        // the loop above, on four Canva poses drawn for this shot.
        var paalam = MakeObjective("Paalam", pPaalam);
        UnityEventTools.AddPersistentListener(paalam.onCorrect, paalamFrames.Play);
        UnityEventTools.AddPersistentListener(paalam.onCorrect, gleamPaalam.Play);

        // HE GOES, AND HIS BAG GOES WITH HIM - which is true of its own
        // accord, because he is WEARING it: every frame of the walk has the
        // straps drawn on his shoulders, so there is nothing to keep attached
        // and nothing that can be left behind.
        //
        // HE IS NOT SWITCHED OFF ANY MORE, and that is the change. The old
        // "Alis" turned the figure off on the word and the desk was empty on
        // the next frame, which is a boy vanishing rather than a boy leaving.
        // Now the walk carries him off the side of the picture and the room
        // fades behind him; the level-complete panel comes up over a dark,
        // empty classroom, and SceneObjectiveController waits on the
        // WalkOffAndFade so it cannot come up early.
        //
        // The desk's own bag is still switched off here, for the one run that
        // needs it: a child who never answered "Kunin" would otherwise leave
        // a bag standing on an empty desk.
        var alis = MakeObjective("Alis", pAlis);
        UnityEventTools.AddPersistentListener(alis.onCorrect, alisFrames.Play);
        if (propBag != null)
            UnityEventTools.AddBoolPersistentListener(alis.onCorrect, propBag.SetActive, false);
        UnityEventTools.AddPersistentListener(alis.onCorrect, gleamAlis.Play);

        SceneObjectiveController controller = uiRoot.AddComponent<SceneObjectiveController>();
        controller.voiceCommand = voiceCommand;
        controller.playerMovement = player;
        controller.objectives = new[] { ligpit, tayo, kunin, paalam, alis };
        // The default four seconds is not enough for this scene's first word,
        // and it is nowhere near enough for its last.
        //
        // It is a safety cap on how long the sequence waits for an action to
        // finish before asking the next word anyway. "Ligpit" is four chained
        // sequences and twenty-seven frames at about six a second - a little
        // under five seconds - so at four it would be cut off and "Tayo"
        // asked for while he still had a pencil in his hand. "Alis" is longer
        // still: a turn, a crossing of the whole room and a fade, which come
        // to about four and a half seconds, and it is the word the
        // level-complete panel waits behind. Nine gives both room without
        // leaving a child waiting if a sequence ever fails to land.
        controller.maxActionWait = 9f;

        // OFF THE DESK AND TO THE LEFT, exactly as Scenes 3 and 4 do it and
        // for the same reason: the desk fills the bottom of this frame and
        // everything the child has to watch is right of centre, so the left
        // is empty lockers and the far end of the desk. The buttons stay at
        // the same height, which is what keeps them reachable.
        VoiceUIBuilder.BuildVoiceUI(voiceCommand, controller,
                                     new Vector2(-620, -400), new Vector2(-370, -400));
        // Still needed in the scene: SceneResultPanel looks the fader up at
        // run time to fade out of whichever button is pressed.
        SceneFaderBuilder.Build();

        // --- Last scene of Level 2, and the last one there will be ---
        // Brings the level-complete summary, unlocks the next node on the
        // journey map, awards the Paaralan badge, and adds this scene's
        // RunScoreCounter with resetOnStart = false - so the words from
        // Scenes 1 to 4 are still in the level total.
        DatabaseDemoBuilder.Build("Paaralan - Level 2", "LevelSelect", 2, "badge_paaralan");

        System.IO.Directory.CreateDirectory("Assets/Scenes");
        EditorSceneManager.SaveScene(scene, ScenePath);
        AddToBuildSettings();

        Debug.Log("[SALINLAHI] Paaralan Scene 5 built and saved to " + ScenePath +
                   ". Words: Ligpit, Tayo, Kunin, Paalam, Alis. Staged at SCENE 4'S DESK, " +
                   "in Scene 4's room, at Scene 4's scale: the end of the day happens where " +
                   "recess did, so Scene 4 no longer cuts to another classroom. Ligpit puts " +
                   "the notebook, the textbook and the pencil in his bag one at a time; Tayo " +
                   "brings him up out of the chair into his idle, facing forward; Kunin " +
                   "reaches out for the bag on the cleared desk, lifts it clear and puts " +
                   "it on his back; Paalam waves goodbye with it on; Alis turns him, walks " +
                   "him off the left of the picture and fades the room to black, ending " +
                   "Level 2. ALL FIVE WORDS ARE DRAWN: Scene 4's pack-away chain, a rise " +
                   "built by Tools/make_tayo_frames.py, and Kunin, Paalam and Alis on Canva " +
                   "drawings cut by Tools/make_uwian_canva_frames.py - he picks the bag up " +
                   "and wears it, waves, turns to the left and walks out. " +
                   "NOTE: the narration ids follow the words, so run Tools > SALINLAHI > " +
                   "Refresh Narrator Voice Bank after this - Docs/VoiceOverScript.csv has " +
                   "the renamed and rewritten l2_s5 lines.");
    }

    /// <summary>
    /// One of Kylo's drawn actions, as a PoseSequence on the seated figure.
    ///
    /// Every one in this scene has the same shape - a list of frame names, an
    /// idle pose to land on and a rate - so they are built here rather than
    /// five times over. playOnEnable is always off: the figure is on screen
    /// from the first frame, so a sequence that played on enable would fire
    /// the whole action the moment the scene loaded.
    /// </summary>
    private static PoseSequence AddPose(GameObject go, SpriteRenderer target,
                                         string[] frames, string destination,
                                         float fps)
    {
        PoseSequence pose = go.AddComponent<PoseSequence>();
        pose.target = target;
        pose.frames = Sprites(frames);
        pose.destinationSprite = LoadSpriteOptional(destination);
        pose.framesPerSecond = fps;
        pose.playOnEnable = false;
        return pose;
    }

    private static Sprite[] Sprites(params string[] names)
    {
        Sprite[] sprites = new Sprite[names.Length];
        for (int i = 0; i < names.Length; i++) sprites[i] = LoadSpriteOptional(names[i]);
        return sprites;
    }

    /// <summary>
    /// An object that is CARRIED: its own GameObject, drawn in front of
    /// everything, switched off until a hand picks it up.
    ///
    /// It is laid at `startAngle` from the start, so it is already lying the
    /// way the drawing it takes over from has it; and it is given the
    /// renderer as well as the transform, because PropCarry fades it out as
    /// the bag closes over it.
    ///
    /// The component lives on a PARENT that stays switched on and moves the
    /// child, because a component cannot run a coroutine on an object it has
    /// just switched off.
    /// </summary>
    private static PropCarry CreateCarry(string name, string spriteFile,
                                          float worldHeight, Vector3[] path,
                                          float startAngle)
    {
        GameObject go = new GameObject(name);
        SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = LoadSpriteOptional(spriteFile);
        sr.sortingLayerName = "Player";
        sr.sortingOrder = OrderFlying;

        float s = 1f;
        if (sr.sprite != null && sr.sprite.bounds.size.y > 0.0001f)
            s = worldHeight / sr.sprite.bounds.size.y;
        go.transform.localScale = new Vector3(s, s, 1f);
        go.transform.position = path[0];
        go.transform.rotation = Quaternion.Euler(0f, 0f, startAngle);

        GameObject holder = new GameObject(name + "_Driver");
        PropCarry carry = holder.AddComponent<PropCarry>();
        carry.prop = go.transform;
        carry.propRenderer = sr;
        carry.waypoints = path;
        carry.startAngle = startAngle;
        go.SetActive(false);
        return carry;
    }

    /// <summary>
    /// One of Kylo's poses, placed by the ground it sits on and sized by the
    /// world height it should be, read from the sprite's own bounds.
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

    /// <summary>A prop on a surface, switched off until its word. Null when art is absent.</summary>
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
        // THE PLAIN SPELLING, as every other scene builder bakes it. The
        // marks are put back at run time, not here: GameplayHUD's signpost
        // and WordsLearnedPanel both run the word through WordStress, which
        // is what makes "Tayo" read as tayô (to stand) rather than táyo (we).
        // Baking the marked form into this badge as well would make Scene 5
        // the one scene out of fifteen whose saved prompt text differs from
        // its objective's word, for no gain - change it in all of them or in
        // none.
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
