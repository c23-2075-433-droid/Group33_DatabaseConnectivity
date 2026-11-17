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
//   Tayo     DRAWN. He pushes off the desk and comes up out of the chair -
//            sagot_rise_1..3 and sagot_stand, Scene 3's rise, which is the
//            only standing drawing in the project fitted to THIS desk at
//            THIS scale. See the note at TayoFps for what it cost Scene 3
//            and why nothing is grafted.
//   Kunin    PROP. His bag comes up onto the desk he has just cleared.
//   Paalam   GLEAM ONLY. Said to the room on his way out.
//   Alis     GLEAM, then the room is empty and the level ends.
//
// THE THREE THAT ARE NOT DRAWN ARE NOT DRAWN FOR A REASON, and it is the
// same reason for all three: every reaching, waving and walking pose in this
// project is either SEATED - kuha_frame_*, reach_bag_*, aklat_* - or a
// full-body wide-shot figure built for a scene with a floor in it -
// bati_*_with_bag, walk_frame_*_with_bag, standing_uniform_with_bag. He is
// STANDING from "Tayo" on, and klasrum_interior_background has no floor:
// the shot is cut at the lockers' lower rows and the desk's legs leave the
// picture the same way. A wide-shot figure dropped in here stands on nothing
// at the wrong scale, and a seated reach cannot be played by a boy on his
// feet. sagot_stand_uniform is the one standing drawing that was ever fitted
// to this desk, and it was fitted by hand - see Tools/make_sagot_frames.py,
// which is also where "A SEATED DRAWING AND A STANDING ONE DO NOT JOIN" is
// written out at length.
//
// So Kunin moves an object, Paalam lights him up, and Alis empties the room.
// All three are honest about what the art can show, and all three are the
// first candidates if anyone generates a standing-at-the-desk strip later:
// a lift, a wave and a turn away, in that order.
//
// ALIS EMPTIES THE ROOM. He and his bag go together - he has taken it with
// him - and the level-complete panel comes up over an empty desk. That is
// not a shortcut around a missing walk-off: an empty classroom at the end of
// the day is the image dismissal actually leaves, and the panel is on screen
// a beat later.
//
// WHAT THE WORDS LOOK LIKE ON SCREEN is not decided here. SceneObjective
// carries the plain spelling, which is what recognition, WordVariants and the
// word_*.wav names all use; WordStress decides the label, and it marks
// "Tayo" as "Tayô" because tayô (to stand) and táyo (we) are a minimal pair.
// "Paalam" and "Alis" are not pairs and so are shown unmarked, which is that
// file's rule - a mark appears exactly where it is doing work.
//
// ONE WORD IS NOT YET VOICED. word_ligpit.wav has never been recorded (see
// Docs/VoiceOverScript.csv, which lists it and says so), so Ligpit is taught
// by caption here as it is in Scene 4. Everything else in the list has its
// word clip. The narration around each word - story, ask, done - comes from
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

    // --- His bag, once "Kunin" has put it on the desk ---
    // ON THE RIGHT STRIP, where the textbook was. His own sprite covers
    // x 0.84..4.88 and a bag in the middle of the desk would be behind his
    // body; the right strip is clear from the moment Ligpit empties it, which
    // is three words before this one needs it.
    private const float BagX = 5.30f;
    private const float BagBaseY = -3.84f;
    private const float BagHeight = 1.60f;

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
    // Scene 3's finding for the same four frames: nine a second, not eleven,
    // or he is on his feet before the child has finished saying the word.
    private const float TayoFps = 9f * Pace;

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
            // "Tayo": Scene 3's rise out of the chair. Also no new art.
            "sagot_rise_1_uniform", "sagot_rise_2_uniform", "sagot_rise_3_uniform",
            "sagot_stand_uniform",
            // "Kunin": the bag itself.
            "prop_bag",
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
        // sagot_* frames are 663x1521 - the same canvas with 300 rows added
        // at EACH end, so the figure sits in the same place in it - and at
        // this scale they draw him standing, risen 0.89 units, with the
        // desk's far edge crossing him at the top of his shorts. That is
        // Tools/make_sagot_frames.py's whole job and why the padding is
        // symmetric.
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
        GameObject propBag = CreatePropOptional("Prop_Bag", "prop_bag",
                                                 BagX, BagHeight, BagBaseY);

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
        //   place_frame_3     both hands flat on the desk - he pushes off it
        //   sagot_rise_1..3   up out of the seat, over three drawn heights
        //   sagot_stand       on his feet
        //
        // THE POSE CHANGES ONCE, on the first frame of the rise, which is the
        // fastest moment in the beat and where a change is least seen. That is
        // Scene 3's choice for the same four frames and it is not a detail:
        // the seated drawings and the standing one disagree about where his
        // arms are, and the earlier attempt to ease one into the other gave
        // him a white band round his waist like a sash and arms that ended in
        // mid-air. Tools/make_sagot_frames.py has the whole of it.
        //
        // NO HAND-RAISE LEAD-IN. Scene 3 opens this run with
        // tanong_frame_1..3, his hand going up, because there the rise is the
        // back half of "Sagot" - he stands to ANSWER. Here the word is just
        // standing up to go home, so the lead-in is the one frame that shows
        // him taking his weight on the desk.
        //
        // HE IS LEFT STANDING. destinationSprite is sagot_stand_uniform, so
        // Kunin, Paalam and Alis all happen with him on his feet. The one
        // thing to know about that drawing is that it is talking_uniform
        // entire - hands up by his chest, mouth open - because that is the
        // only standing figure anyone ever fitted to this desk. It reads as a
        // boy who has just got up and is about to speak, which is near enough
        // to what he is doing, and it is the honest limit of the art.
        //
        // PLAYED ONCE, not looped. Standing up happens once.
        PoseSequence tayoFrames = AddPose(seatGO, seatSr, new[]
        {
            "place_frame_3_uniform",    // hands flat - he takes his weight on the desk
            "sagot_rise_1_uniform",     // and comes up
            "sagot_rise_2_uniform",
            "sagot_rise_3_uniform",
            "sagot_stand_uniform",      // on his feet
        }, "sagot_stand_uniform", TayoFps);

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
        // second one holds while he is STANDING too: the sagot_* frames are
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

        // PROP ONLY, and the note at the top of this file says why: he is on
        // his feet by now and there is no standing pickup drawn for this desk.
        // The bag arriving on the strip he cleared, with a gleam on it, is a
        // boy who has lifted his bag up out from under the desk.
        var kunin = MakeObjective("Kunin", pKunin);
        if (propBag != null)
            UnityEventTools.AddBoolPersistentListener(kunin.onCorrect, propBag.SetActive, true);
        UnityEventTools.AddPersistentListener(kunin.onCorrect, gleamKunin.Play);

        // GLEAM ONLY. A goodbye is a thing said, not a thing done to an
        // object, and the one drawn wave in the project is a full-body
        // wide-shot figure that does not belong in this shot.
        var paalam = MakeObjective("Paalam", pPaalam);
        UnityEventTools.AddPersistentListener(paalam.onCorrect, gleamPaalam.Play);

        // HE GOES, AND HIS BAG GOES WITH HIM. Both at once, because he has
        // taken it: a bag left behind on the desk after he has gone is a bag
        // he has forgotten. The level-complete panel comes up a beat later
        // over the empty desk, which is what dismissal leaves behind.
        var alis = MakeObjective("Alis", pAlis);
        UnityEventTools.AddBoolPersistentListener(alis.onCorrect, seatGO.SetActive, false);
        if (propBag != null)
            UnityEventTools.AddBoolPersistentListener(alis.onCorrect, propBag.SetActive, false);
        UnityEventTools.AddPersistentListener(alis.onCorrect, gleamAlis.Play);

        SceneObjectiveController controller = uiRoot.AddComponent<SceneObjectiveController>();
        controller.voiceCommand = voiceCommand;
        controller.playerMovement = player;
        controller.objectives = new[] { ligpit, tayo, kunin, paalam, alis };
        // The default four seconds is not enough for this scene's first word.
        //
        // It is a safety cap on how long the sequence waits for an action to
        // finish before asking the next word anyway. "Ligpit" is four chained
        // sequences and twenty-seven frames at about six a second - a little
        // under five seconds - so it would be cut off at four and "Tayo"
        // asked for while he still had a pencil in his hand. Eight gives it
        // room without leaving a child waiting if a sequence ever fails to
        // land. Scene 4 carries the same number for the same word.
        controller.maxActionWait = 8f;

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
                   "brings him up out of the chair; Kunin puts his bag on the cleared desk; " +
                   "Paalam lights him up as he says goodbye; Alis takes him and the bag out " +
                   "of the room and ends Level 2. Ligpit and Tayo are DRAWN and cost no new " +
                   "art - Scene 4's pack-away chain and Scene 3's rise. Kunin, Paalam and " +
                   "Alis are prop-and-gleam: see the note at the top of " +
                   "BuildPaaralanScene5.cs for what the art cannot show yet. " +
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
