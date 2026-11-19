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
//   Kunin    DRAWN. His bag is standing on the strip of desk Ligpit
//            cleared - lit by this room's window and with its own shadow
//            under it, so it is an object on the wood rather than a sticker
//            on the glass. His arm comes off his side, out and down to it,
//            his fist closes on its handle, he LIFTS it clear of the desk,
//            brings it in and up across his body, and it goes on his back;
//            then he pulls the strap down to sit, which is what everybody
//            does. He ends the level WEARING it, which is how Kylo is drawn
//            for the whole of Paaralan Scene 2. kunin_reach_1..2,
//            kunin_grip, kunin_lift, kunin_carry_1..2, kunin_settle,
//            kunin_strap_1..2 and kunin_stand.
//   Paalam   DRAWN. His arm comes up with the elbow bending, his open palm
//            arrives beside his head, and it WAVES - out, back, out, three
//            times over - and comes down again, with the bag on his back
//            for all of it. paalam_lift_1..3, paalam_wave_in/mid/out and
//            paalam_down.
//   Alis     DRAWN. He turns, walks the whole width of the room and off the
//            left of the picture on his own legs, and the classroom fades to
//            black behind him. alis_turn and alis_walk_1..4, plus
//            WalkOffAndFade, which carries him and takes the light out.
//
// ALL THREE ARE BUILT BY Tools/make_uwian_frames.py AND NONE OF THEM IS NEW
// ART. The note this replaces said the opposite, and was right at the time:
// every reaching, waving and walking pose in the project was either SEATED -
// kuha_frame_*, reach_bag_*, aklat_* - or a full-body wide-shot figure built
// for a scene with a floor in it - bati_*_with_bag, walk_frame_*_with_bag,
// standing_uniform_with_bag. He is STANDING from "Tayo" on, and
// klasrum_interior_background has no floor. A wide-shot figure dropped in
// here stands on nothing at the wrong scale, and a seated reach cannot be
// played by a boy on his feet; Tools/make_sagot_frames.py is where "A SEATED
// DRAWING AND A STANDING ONE DO NOT JOIN" is written out at length, and it
// is about this exact shot.
//
// WHAT CHANGED IS WHERE HE IS CUT. make_tayo_frames builds the standing
// figure by grafting, and make_kunin_frames then bent the invented part of
// his forearm to reach for the bag - but the invented part starts at the
// DESK LINE, which on a standing boy is the top of his shorts, so "fifty-two
// degrees of reach" was fifty-two degrees of wrist. His hand moved twenty
// pixels, the bag slid sideways and down behind the desk, and the next frame
// had the backpack on his shoulders.
//
// make_uwian_frames cuts him at the joints instead and turns each piece
// WHOLE, which is Tools/make_bati_front_frames.py's technique brought over
// to this desk: nothing inside a rigidly rotated piece can deform, so the
// hand in every frame is the hand that was drawn. Legs that turn about the
// hems of his shorts walk.
//
// AND THE ARM HAS TWO BONES IN IT NOW. The first pass of that file had one -
// the whole bare limb turning about a single point - and one point is one
// bone however it is labelled: a stiff stick from the armpit to the
// fingertips. It was enough for a reach, because a child reaching across a
// desk does straighten his arm, and it was hopeless for the wave, which
// arrived as that same stick pointing at the ceiling with a RELAXED HANGING
// HAND upside down on the end of it. So the arm is cut at the sleeve's hem
// and again at the elbow, each piece turns about its own joint, and each
// joint is a disc that a rotation about its own centre cannot move - which
// is what lets the two mate at any angle with nothing to hide.
//
// AND THE WAVE IS A DIFFERENT DRAWING, not an angle of this one.
// talking_uniform is the same boy at the same scale with one arm already
// up - drawn sleeve, drawn upper arm, drawn bent elbow, drawn open palm -
// and mirrored it lands on this figure's shoulder within a few pixels,
// because make_tayo_frames already grafts that drawing's lower body on at
// the waist. Its arm is grafted on too, and the wave is then one rotation of
// a forearm about an elbow somebody drew.
//
// That is the whole of why three gleams became three drawn beats, and it
// still cost no new drawings.
//
// AND HIS LEGS NOW REACH THE BOTTOM OF THE PICTURE. The tayo_* canvas ends
// half a unit above this camera's bottom edge, which is invisible while the
// desk is in front of him and a flat cut across his shins the moment he
// walks out from behind it. The uwian_* canvas is taller and his shins are
// carried out of frame; he leaves the picture at the side with his legs
// running off the bottom of it, which is what a close-up does.
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
    // would catch. prop_bag_uniform is that navy bag lifted straight out of
    // standing_uniform_with_bag_back by Tools/make_kunin_frames.py, where it
    // is drawn square on with nothing in front of it.
    //
    // ITS HEIGHT IS THE HEIGHT HE WEARS IT AT. 221 rows of a drawing where he
    // is 745 rows for this desk's 5.609 world units, which is 1.66 - so the
    // bag on the desk and the bag on his back are the same size, because they
    // are the same bag.
    //
    // HIS FIST LANDS ON ITS HANDLE, and that is what decides x rather than
    // the strip. Tools/make_uwian_frames.py measures the fist on every frame
    // it draws and prints it; on the frame he closes on the bag it is at
    // 4.90, -2.48, so a centre of 4.90 puts the drawn handle under the drawn
    // hand. Rule 5 of Docs/AnimationStandard.md - objects meet his hands - is
    // this one number.
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
    // on it, and is then replaced by the carried bag, which has no shadow
    // because it is in the air. One sprite is one base line to get right
    // instead of three.
    private const float BagDeskHeight = BagHeight * 1.08597f;
    private const float BagDeskBaseY = BagBaseY - BagHeight * 0.08597f;

    // WHERE THE BAG IS RELATIVE TO THE FIST HOLDING IT, and it CHANGES over
    // the beat, which is the whole of what makes this a bag being put on
    // rather than a bag being towed about.
    //
    // A bag lifted by its handle hangs below the hand - half its own height,
    // near enough, which is BagGripDY. A bag being SWUNG ONTO A BACK does
    // not: the hand goes to the strap and the bag comes UP as it goes round,
    // until by the time the straps are on his shoulders the bag is level
    // with his hand rather than under it. So the offset is interpolated from
    // the first number to nearly nothing across the carry.
    //
    // It also has to be. His fist at his hip is at y -2.60; a bag hung the
    // full 0.51 below that has its base at -3.95, which is under the desk's
    // own top edge, and the last third of the beat played out behind the
    // wood in the one scene whose job is to teach the word for taking it.
    private const float BagGripDX = BagX - 4.90f;
    private const float BagGripDY = (BagBaseY + BagHeight * 0.5f) - -2.48f;

    // The fist on each drawn frame of "Kunin" from the grip on, printed by
    // Tools/make_uwian_frames.py, with how much of BagGripDY still applies.
    //
    // READ THEM AS THE SHAPE OF THE BEAT: his hand closes on the handle where
    // the bag already is, the bag comes UP 0.56 of a unit as he takes its
    // weight - clear of the wood, which is what makes this a lift rather than
    // a slide - then in across his body, rising against his hand as it goes
    // round, and onto his back.
    //
    // THE HAND-OVER IS ON THE LIFT, NOT ON THE GRIP, and that is what the
    // first waypoint being kunin_lift's rather than kunin_grip's means. Only
    // the bag standing on the desk has a shadow drawn under it - the one in
    // the air must not have one - so the frame the two are swapped on is the
    // frame the shadow disappears, and that has to be the frame the bag
    // leaves the wood. Swapped a frame earlier, while it is still standing
    // there with his fist closed on the handle, the shadow blinks out from
    // under a bag that has not moved.
    private static readonly Vector3[] KuninFist =
    {
        new Vector3(5.34f, -1.92f, 1.00f),   // lift     clear of the desk
        new Vector3(4.28f, -2.73f, 0.95f),   // carry_1  swinging in, past the desk
        new Vector3(2.68f, -1.97f, 0.15f),   // carry_2  up at his chest
        new Vector3(2.65f, -1.99f, 0.00f),   // settle   round onto his back
    };

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

    // TAKING A BAG OFF A DESK IS SLOWER AGAIN. Ten frames of arm at eight a
    // second is about 1.9 seconds, which is a reach, a grip, a lift, a swing
    // and a tug at the strap rather than a swipe. It is also the rate the
    // bag's carry is given, so the prop and the hand share one clock.
    //
    // IT WAS SEVEN FRAMES AT SEVEN. The three that were added are the grip -
    // a beat of its own, because a bag lifted on the same frame the hand
    // arrives is a bag nobody saw him take hold of - and the two where he
    // pulls the strap down to sit on his shoulder, which is what everybody
    // does and what makes the bag his rather than a sprite that landed on
    // him. The rate went up with the count so the beat is the same length a
    // child's patience was measured against.
    private const float KuninFps = 8f * Pace;

    // A WAVE IS FASTER THAN A LIFT and has to be, or it is a hand being held
    // up and moved rather than a wave. The hand goes up at the scene's
    // ordinary rate and then sweeps at eleven a second, which carries it
    // across and back in about three-quarters of a second - a child's wave.
    private const float PaalamFps = 8f * Pace;
    private const float PaalamWaveFps = 11f * Pace;
    private const int PaalamWaves = 3;

    // AND A WALK IS SLOWER THAN EITHER. Four drawn frames are two steps, so
    // at this rate he takes about two and a half steps a second - the pace a
    // six-year-old leaves a classroom at, and slow enough that the legs read
    // as legs. AlisWalk is how long the crossing itself takes and the two are
    // tied together below: the loop is given as many cycles as fill it, so
    // the legs stop when the boy does however either is retuned.
    private const float AlisFps = 7f * Pace;
    private const float AlisWalk = 3.2f;
    // ONE FRAME OF STANDING STILL AND TURNING, which is what alis_turn is, so
    // he sets off exactly as the first frame of the walk cycle comes up. It
    // is written as a frame rather than as a number of seconds for that
    // reason: change AlisFps and this follows it.
    private const float AlisTurn = 1f / AlisFps;

    // OFF THE SIDE OF THE PICTURE, not level with it. The view is 19.2 wide,
    // so its left edge is at -9.6; his sprite is 1063 px on a canvas that
    // draws 921 of them to 5.609 units, which is 6.5 units wide, and half of
    // that past the edge is -12.8. He is gone before the fade begins rather
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
            // "Kunin", "Paalam" and "Alis": the bag, and the arm, legs and
            // turn that handle it, all built by Tools/make_uwian_frames.py -
            // the arm in TWO BONES about a shoulder and an elbow, the legs
            // about their hems, and the wave on a raised arm grafted whole
            // out of talking_uniform.
            //
            // TWO BAGS, ONE DRAWING. prop_bag_desk_uniform is the one
            // standing on the wood, with this room's light and its own
            // shadow on it; prop_bag_uniform is the same cut-out in the air,
            // riding his fist, where a shadow would be wrong. Both come out
            // of Tools/make_desk_bag.py in one run.
            "prop_bag_uniform", "prop_bag_desk_uniform",
            "kunin_reach_1_uniform", "kunin_reach_2_uniform",
            "kunin_grip_uniform", "kunin_lift_uniform",
            "kunin_carry_1_uniform", "kunin_carry_2_uniform",
            "kunin_settle_uniform",
            "kunin_strap_1_uniform", "kunin_strap_2_uniform",
            "kunin_stand_uniform",
            "paalam_lift_1_uniform", "paalam_lift_2_uniform",
            "paalam_lift_3_uniform",
            "paalam_wave_in_uniform", "paalam_wave_mid_uniform",
            "paalam_wave_out_uniform", "paalam_down_uniform",
            "alis_turn_uniform",
            "alis_walk_1_uniform", "alis_walk_2_uniform",
            "alis_walk_3_uniform", "alis_walk_4_uniform",
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
        //   tayo_stand        where Tayo left him, so the join is silent
        //   kunin_reach_1     his arm starts out from his side
        //   kunin_reach_2     out over the desk, the hand turning down
        //   kunin_grip        his fist closes on the handle
        //   kunin_lift        and lifts it clear of the wood
        //   kunin_carry_1     coming back in with it
        //   kunin_carry_2     at his hip, elbow well bent
        //   kunin_settle      and it is on his back
        //   kunin_strap_1     his hand up at the strap
        //   kunin_strap_2     pulling it down to sit
        //   kunin_stand       his idle again, wearing it
        //
        // THIS USED TO BE A PROP AND A GLEAM. The bag was switched on upon
        // the desk and nothing about the boy changed, and the note at the top
        // of this file said why: he is standing, and every reaching pose in
        // the project is either seated or a wide-shot figure for a scene with
        // a floor in it. It also said what the undrawn words were waiting
        // for - a standing pose at this desk to start and end on - and
        // "Tayo" delivered one.
        //
        // SO THE ARM IS DRAWN. Tools/make_kunin_frames.py has the whole of
        // how, and it is make_tayo_frames' own trick carried one step: that
        // file had to INVENT his forearms to get him out of the chair,
        // because the seated drawing stops flat at the desk's far edge, and a
        // limb this project draws for itself can be drawn going somewhere
        // else. The bend starts inside the seated drawing's own arm and is
        // eased in, so nothing is cut and nothing tears; the hand on the end
        // is standing_uniform's, turned whole.
        //
        // AND THE BAG HE PUTS ON IS THE DIFFERENCE BETWEEN TWO DRAWINGS -
        // talking_uniform and talking_uniform_with_bag, which are one pose
        // with and without it. That is what makes him match the Kylo of
        // Paaralan Scene 2, who wears that backpack for a whole level: it is
        // the same drawn bag, not a new one.
        //
        // PLAYED ONCE. Picking something up happens once, and
        // Docs/AnimationStandard.md lists it among the things that must not
        // loop.
        PoseSequence kuninFrames = AddPose(seatGO, seatSr, new[]
        {
            "tayo_stand_uniform",       // where Tayo left him
            "kunin_reach_1_uniform",    // the arm comes off his side
            "kunin_reach_2_uniform",    // out over the desk, hand turning down
            "kunin_grip_uniform",       // and his fist closes on the handle
            "kunin_lift_uniform",       // and lifts it clear of the desk
            "kunin_carry_1_uniform",    // back in with it
            "kunin_carry_2_uniform",    // at his hip, elbow well bent
            "kunin_settle_uniform",     // on his back - the straps are drawn
            "kunin_strap_1_uniform",    // his hand up at the strap
            "kunin_strap_2_uniform",    // and pulling it down to sit
        }, "kunin_stand_uniform", KuninFps);
        kuninFrames.tailFrames = Sprites("kunin_stand_uniform");

        // --- The bag, from the desk to his hand ---
        // The same component the pencil rides in "Ligpit", for the same
        // reason: one waypoint per drawn frame and no clock of its own, so
        // the bag is where the hand is however the scene's Pace changes. The
        // first waypoint is exactly where the bag has been sitting, which is
        // what makes the hand-over invisible.
        Vector3[] bagPath = new Vector3[KuninFist.Length];
        for (int i = 0; i < KuninFist.Length; i++)
            bagPath[i] = new Vector3(KuninFist[i].x + BagGripDX,
                                     KuninFist[i].y + BagGripDY * KuninFist[i].z,
                                     0f);
        PropCarry bagCarry = CreateCarry("Carry_Bag", "prop_bag_uniform", BagHeight,
                                          bagPath, 0f);
        bagCarry.framesPerSecond = KuninFps;
        // AND IT TURNS AS IT COMES ROUND. A bag lifted straight up off a desk
        // hangs level under the hand; a bag swung in across a body tips,
        // because the hand leading it is going sideways faster than the
        // weight below it is. Fourteen degrees is the most it can take
        // before it reads as a bag being spun rather than carried, and
        // PropCarry turns it steadily over the whole path, so the tip
        // arrives with the swing.
        bagCarry.endAngle = -14f;
        // IT GOES ON HIS BACK, IT DOES NOT GO INTO ANYTHING. PropCarry's
        // let-go is written for a thing dropped into a bag - down, away and
        // into shadow - and a backpack being swung on goes ROUND rather than
        // down. So the drop is a twentieth of what the pencil's is and the
        // shrink is slight: what the beat needs from it is the fade, and it
        // is timed to land on kunin_settle, which is the frame where the
        // straps are drawn on his shoulders. The bag in his hand goes as the
        // bag on his back arrives, and for one frame they overlap - which is
        // what swinging a bag round onto your own back looks like.
        bagCarry.insertDuration = 1.2f / KuninFps;
        bagCarry.insertDrop = 0.05f;
        bagCarry.insertScale = 0.80f;
        // IN FRONT, where CreateCarry leaves it, and that is deliberate. The
        // first version put it behind Kylo and behind the desk, on the
        // reasoning that a bag being swung on goes round his side - and the
        // desk ate it: the whole middle of the beat was a bag you could not
        // see, in the one scene whose job is to teach the word for taking
        // it. The desk's own bag is drawn in front of the desk with the rest
        // of his things, so the bag he is holding is too, and the two are
        // the same object to look at across the hand-over.

        // The hand-over: the drawn bag on the desk goes out and the carried
        // one comes in, both on the frame where his fist closes on it.
        kuninFrames.fireOnFrame = 4;                       // kunin_lift
        UnityEventTools.AddPersistentListener(kuninFrames.onFrameReached, bagCarry.Play);
        if (propBag != null)
            UnityEventTools.AddBoolPersistentListener(kuninFrames.onFrameReached,
                                                       propBag.SetActive, false);

        // ================= "Paalam": he waves goodbye ======================
        //
        //   kunin_stand       where Kunin left him, wearing the bag
        //   paalam_lift_1..2  the hand on its way up, so it goes up rather
        //                     than appears up
        //   wave_in/mid/out   THE SWEEP, looped - the part that is the wave
        //   paalam_down       and the hand comes back down
        //
        // IT IS A LOOP AND THAT IS THE POINT. A hand raised and held is not a
        // goodbye; what makes one read is the hand going BACK, and back
        // again. PoseSequence has a looping body for exactly this - the
        // project's rules list waving among the things that repeat - so the
        // three drawn positions of the sweep are its loopFrames and it runs
        // round three times, which is about two seconds of waving.
        //
        // The middle frame appears TWICE in the loop, once on the way out and
        // once on the way back, so the hand passes through the middle rather
        // than jumping across it. Four frames a cycle at eleven a second is a
        // sweep and a return in three-quarters of a second.
        //
        // HE IS WEARING THE BAG THROUGHOUT. Every frame here is built on the
        // figure with the backpack on, so the straps are drawn on his
        // shoulders in all of them and the bag cannot drift, vanish or lag
        // behind the boy: there is no bag object in this beat at all. The one
        // in his hand went when Kunin's PropCarry faded it.
        PoseSequence paalamFrames = AddPose(seatGO, seatSr, new[]
        {
            "kunin_stand_uniform",      // where Kunin left him
            "paalam_lift_1_uniform",    // the arm comes up, elbow bent
            "paalam_lift_2_uniform",
            "paalam_lift_3_uniform",    // hand level with his shoulder
        }, "kunin_stand_uniform", PaalamFps);
        paalamFrames.loopFrames = Sprites("paalam_wave_in_uniform",
                                           "paalam_wave_mid_uniform",
                                           "paalam_wave_out_uniform",
                                           "paalam_wave_mid_uniform");
        paalamFrames.loopCycles = PaalamWaves;
        paalamFrames.loopFramesPerSecond = PaalamWaveFps;
        // DOWN IN ORDER. The tail runs back through the lift in reverse and
        // then past it - lift_3, lift_2, lift_1, down - before it hands back
        // to kunin_stand, which is the hanging arm the other mechanism
        // draws. The first version of this put lift_1 before down, so his
        // hand fell to his hip, came back up to his waist and then dropped
        // again, which is a flinch rather than an arm being lowered.
        paalamFrames.tailFrames = Sprites("paalam_lift_3_uniform",
                                           "paalam_lift_2_uniform",
                                           "paalam_lift_1_uniform",
                                           "paalam_down_uniform",
                                           "kunin_stand_uniform");

        // ================= "Alis": he goes ==================================
        //
        //   kunin_stand   standing at his desk, wearing it
        //   alis_turn     he turns to go - one frame, narrowed
        //   walk_1..4     the walk, looped, while WalkOffAndFade carries him
        //                 across the room and off the side of it
        //
        // TWO COMPONENTS, ONE BEAT. The legs are a PoseSequence like every
        // other drawn action in the project; the crossing is a
        // WalkOffAndFade, which moves the transform and nothing else. They
        // are started by the same word and tuned to the same length, which is
        // the whole of why it reads as walking rather than as sliding: the
        // feet change while the boy moves. Give the loop too few cycles and
        // he glides the last stretch with his legs together; too many and
        // they go on after he has gone.
        //
        // SO THE CYCLES ARE COUNTED RATHER THAN CHOSEN. The walk lasts
        // AlisWalk seconds after AlisTurn seconds of turning, the loop runs
        // at AlisFps, and four frames make a cycle - so retuning the pace or
        // the distance keeps them in step without anyone remembering to.
        int alisCycles = Mathf.Max(1, Mathf.RoundToInt(AlisWalk * AlisFps / 4f));
        PoseSequence alisFrames = AddPose(seatGO, seatSr, new[]
        {
            "kunin_stand_uniform",
            "alis_turn_uniform",        // he turns toward the left
        }, null, AlisFps);
        alisFrames.loopFrames = Sprites("alis_walk_1_uniform",
                                         "alis_walk_2_uniform",
                                         "alis_walk_3_uniform",
                                         "alis_walk_4_uniform");
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
        alisWalk.startDelay = AlisTurn;
        alisWalk.duration = AlisWalk;
        alisWalk.fadeImage = fadeImage;

        // THE WALK STARTS FROM THE ANIMATION, NOT FROM THE WORD, and the
        // difference only shows in the one case that matters. `interrupts`
        // above can hold the pose back while the wave lowers his hand; a
        // Begin() wired straight to the objective would not wait with it, and
        // he would set off across the room still waving. Hung on the turn
        // frame instead, the two cannot come apart however long the wave
        // takes to land - he turns, and on that frame he starts moving.
        alisFrames.fireOnFrame = 1;                        // alis_turn
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
        // the loop above. It used to be a gleam and nothing else, on the
        // reasoning that the only drawn wave in the project is a wide-shot
        // figure that does not belong in this shot - which was true of the
        // DRAWINGS and stopped being the answer once Tools/make_uwian_frames
        // could turn this figure's own arm about its own elbow.
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
                   "Level 2. ALL FIVE WORDS ARE DRAWN and none of them cost new art - " +
                   "Scene 4's pack-away chain, a rise built by Tools/make_tayo_frames.py, " +
                   "and a reach, a wave and a walk built by Tools/make_uwian_frames.py, " +
                   "which cuts that rise's own figure at the elbow and at the hem of his " +
                   "shorts and turns each piece whole. " +
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
