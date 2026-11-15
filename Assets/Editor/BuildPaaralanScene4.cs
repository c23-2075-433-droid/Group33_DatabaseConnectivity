// Assets/Editor/BuildPaaralanScene4.cs
//
// One-click scene builder for Paaralan Scene 4 ("Recess").
// Teaches five words: Ligpit, Kuha, Kain, Inom, Tapon.
//
// THE SAME ROOM AS SCENE 3, AND THAT IS THE POINT. The bell has gone and he
// is still at the desk he had the lesson at: same background sprite, same
// camera, same seat, same scale, same desk layer in front of him. Every
// constant below that places something was copied from BuildPaaralanScene3
// rather than re-derived, so the two scenes cannot drift apart - a child
// should see the lesson end and break begin, not a cut to another classroom.
//
// Scene 3 is NOT touched by this file. It is read from, in the sense that
// these numbers came out of it; nothing here writes to it.
//
// WHAT CHANGES, which is only what the words need: his baon, a water bottle,
// a wrapper and a bin. The room, the light, the furniture and the boy are
// Scene 3's and are left alone.
//
// WHAT EACH WORD DOES. All five are drawn, and all five move objects:
//
//   Ligpit   he packs the lesson away - the notebook, then the textbook,
//            then the pencil, each one picked up off the desk and put in
//            his bag, in that order, one at a time.
//   Kuha     he reaches into the same bag and takes his baon out, and puts
//            it on the desk he has just cleared.
//   Kain     he takes a sandwich out of the box and eats it, and KEEPS
//            EATING until the next word is said.
//   Inom     he picks his water bottle up off the desk, tips it to his
//            mouth, drinks, and puts it back.
//   Tapon    he picks up the wrapper, cocks his hand, and throws it - and
//            the wrapper itself crosses the room and drops into the bin.
//
// LIGPIT IS SCENE 2'S "KUHA" RUN BACKWARDS, and that is why it could be
// drawn at all. Taking a flat object out of a bag and putting it on a desk
// is, frame for frame, putting one from a desk into a bag with the list
// reversed - so the notebook leg is place_frame_3, place_frame_2,
// place_frame_1, reach_bag, kuha_frame_3, _2, _1 and back to the desk, all
// of them drawings this project already had. The textbook leg is the same
// seven with Paaralan 3's aklat_* frames in place of three of them. Not one
// new sprite between them.
//
// THREE ITEMS AND ONE fireOnFrame. PoseSequence fires a single frame event,
// and Ligpit has to take three things off the desk at three different
// moments. So it is FOUR PoseSequences chained by onFinished rather than one
// long one: each leg owns its own object and its own frame event, and the
// scene reads as three deliberate trips to the bag instead of one blur. The
// join is seamless because PoseSequence sets running = true synchronously
// inside Play(), so SceneObjectiveController never sees a gap between legs
// and never asks for the next word early.
//
// THE PENCIL IS THE ONE THAT DOES NOT FIT, and it is worth saying why rather
// than hiding it. reach_bag_uniform is the only frame in the project with a
// hand at the mouth of the bag, and it has the NOTEPAD drawn into it - a
// pencil cannot be swapped over that footprint, because a pencil drawn big
// enough to cover a notepad is not a pencil. So the pencil is picked up
// (lapis_frame_1, which has his fingers closed round one because that is the
// beat it was drawn for), raised (lapis_frame_2), and then DROPPED IN: the
// prop leaves his drawn hand at the exact point the drawing had it and
// travels into the bag on PropToss. The object is visible the whole way.
//
// TAPON IS THE SAME MACHINERY DOING THE THING IT WAS WRITTEN FOR. The
// wrapper is in his hand for three drawn frames, and then it is a prop on an
// arc that ends inside the bin and sinks out of sight. See PropToss.
//
// THE BIN IS NEW ART with no lid, drawn by Tools/make_recess_props.py, and
// it is placed so the camera looks INTO it - see BinX and the note there.
//
// Sprites expected in Assets/Sprites/: see the SpriteImportFixer.Verify call
// in BuildScene, which is the list that is actually checked.
//
// Run via: Tools > SALINLAHI > Build Paaralan Scene 4

using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class BuildPaaralanScene4
{
    private const string SpriteFolder = "Assets/Sprites/";
    private const string ScenePath = "Assets/Scenes/Chapter2_Level4_Recess.unity";
    private const string NextScene = "Chapter2_Level5_Uwian";

    private const float ViewHeight = 10.8f;           // camera is orthographic 5.4
    private const float ViewWidth = 19.2f;

    // --- Scene 3's staging, copied exactly ---
    // These four numbers are what make this the same room as the lesson. The
    // desk's far edge was measured off klasrum_interior_background for
    // Paaralan Scene 2 and has been the seat of every scene at this desk
    // since; BuildPaaralanScene3 has the full working. Changing any of them
    // here and not there is how the two scenes come apart.
    private const float SitX = 2.86f;                 // the chair behind the near desk
    private const float SitFeetY = -3.009f;           // the desk's far edge
    private const float SeatedHeight = 5.609f;

    // --- Where his drawn hands are, in the world ---
    //
    // Every pose of Kylo's is 663 x 921 pixels and this scene draws it 4.038
    // wide by 5.609 tall at x 0.84..4.88, y -3.009..2.600. So a pixel in any
    // of the frames has a world position, and these are the three that
    // matter: the places an object leaves his hand and has to carry on from
    // under its own power.
    //
    // They are not guesses and they are not tuned by eye. Tools/
    // make_recess_frames.py lists the same fists in pixels at the top of its
    // frame table, and these are those numbers through the conversion above.
    // If a pose is redrawn, change them together or the object will jump at
    // the moment it is released - which is the one seam this whole scene is
    // built to avoid.
    private const float HandHighX = 1.77f;            // lapis_frame_3: up beside his face
    private const float HandHighY = -1.09f;
    private const float HandChestX = 2.09f;           // lapis_frame_2: at his chest
    private const float HandChestY = -1.59f;
    // The mouth of his bag, drawn into the bottom right of reach_bag_uniform.
    private const float BagMouthX = 4.01f;
    private const float BagMouthY = -2.58f;

    // --- What is on the desk when the scene opens ---
    //
    // THE LESSON, EXACTLY WHERE SCENE 3 LEFT IT. Same x, same base, same
    // height for all three, so the first frame of this scene is the last
    // frame of the one before it. "Ligpit" is what clears them.
    private const float NotebookX = 2.86f;
    private const float NotebookBaseY = -3.84f;
    private const float NotebookHeight = 0.83f;

    private const float AklatX = 5.30f;
    private const float AklatBaseY = -3.84f;
    private const float AklatHeight = 1.00f;

    private const float PencilX = 3.60f;
    private const float PencilBaseY = -3.35f;
    private const float PencilHeight = 0.30f;

    // The water bottle, out from the start - a child does not fetch a drink
    // at break, it has been on the desk since the morning. On the LEFT strip,
    // which is the only clear desk this view has: his own sprite covers
    // x 0.84..4.88 and the textbook has the right strip. Narrow, so an
    // upright bottle is the one object that fits there.
    private const float BottleX = 0.52f;
    private const float BottleBaseY = -3.62f;
    private const float BottleHeight = 1.55f;

    // The baon, once "Kuha" has put it there: the middle of the desk he has
    // just cleared, which is where the notebook was.
    private const float BaonX = 2.86f;
    private const float BaonBaseY = -3.86f;
    private const float BaonHeight = 1.45f;

    // The wrapper, left on the desk when he has finished eating. To the
    // right of the box, on the strip the textbook has vacated.
    private const float BasuraX = 4.45f;
    private const float BasuraBaseY = -3.60f;
    private const float BasuraHeight = 0.70f;

    // --- The bin ---
    //
    // ON HIS LEFT, WHICH IS THE SIDE HE CAN THROW TO. The three poses the
    // throw is built from hold the wrapper in the hand drawn at x 1.77 -
    // left of his own centre at 2.86 - so the arm is already pointing this
    // way and the wrapper leaves travelling left. A bin on the right would
    // need the wrapper to cross his body, which the drawing cannot show.
    //
    // It also has somewhere to stand there. The desk fills x 0.9..7.4 of the
    // bottom of this frame and the lockers run down behind it; -1.05 is clear
    // of the desk and against the lockers, which is where a classroom bin is.
    //
    // RUNNING OFF THE BOTTOM OF THE FRAME IS DELIBERATE. There is no floor in
    // klasrum_interior_background - the shot is cut at the lockers' lower
    // rows and the desk's legs leave the picture the same way - so a bin
    // drawn complete, with its base in view, would be a bin standing on
    // nothing. Its foot is put below -5.4 and the frame crops it, exactly as
    // the desk is cropped.
    //
    // WHAT THE SIZE IS FOR. The rim has to be big enough to SEE INTO at
    // phone size and small enough not to become the subject of the shot.
    // 2.95 wide against his own 4.04 is a chunky bin and a deliberate one:
    // the first pass had it at 2.01 and on screen it read as a cup on the
    // floor. The opening comes out 2.51 across against a wrapper of 0.62, so
    // a throw that is a little off still plainly goes in.
    private const float BinX = -1.30f;
    private const float BinFootY = -6.55f;
    private const float BinHeight = 4.10f;
    // Where the opening is, as a fraction of the sprite's height from its
    // foot. prop_basurahan is 460 x 640 with the centre of its mouth on row
    // 112, so the mouth is 0.825 of the way up. Written as the fraction
    // rather than as a world number so that resizing the bin moves the
    // target with it.
    private const float BinMouthUp = 0.825f;

    // One knob for the speed of every drawn action in this scene, the same
    // way Paaralan 2 and 3 do it. Anything in SECONDS divides by Pace,
    // anything PER SECOND multiplies by it. Matched to Scene 3's 0.65, since
    // it is the same boy at the same desk ten minutes later.
    private const float Pace = 0.65f;

    private const int OrderKylo = 0;
    private const int OrderDeskFront = 10;            // the desk, drawn over him
    private const int OrderBin = 14;                  // in front of the desk, beside it
    private const int OrderDeskProps = 20;            // his things, on the desk
    private const int OrderFlying = 24;               // in front of everything, mid-air

    [MenuItem("Tools/SALINLAHI/Build Paaralan Scene 4")]
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
            // The room and the boy, both Scene 3's.
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
            // "Kuha": the same movement forwards, with his baon in it.
            "kuha_baon_bag_uniform", "kuha_baon_2_uniform",
            // "Kain", "Inom", "Tapon": one arm, three things in it.
            "kain_hold_uniform", "kain_lift_uniform", "kain_bite_1_uniform",
            "kain_bite_2_uniform", "kain_last_uniform",
            "inom_hold_uniform", "inom_lift_uniform",
            "inom_drink_1_uniform", "inom_drink_2_uniform",
            "tapon_hold_uniform", "tapon_lift_uniform", "tapon_cock_uniform",
            // The recess objects themselves.
            "prop_baon", "prop_baon_open", "prop_baon_empty",
            "prop_bote", "prop_basura", "prop_basurahan",
            "ui_arrow", "ui_word_badge", "badge_paaralan",
        }, "Paaralan Scene 4");

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
        // Scene 3's, which is a fixed shot of one desk. A follow component
        // with a stationary target does nothing visible and is one more thing
        // that could move the frame away from the one the lesson used.

        // --- The classroom: Scene 3's background sprite, scaled its way ---
        GameObject bg = new GameObject("Background_Klasrum");
        SpriteRenderer bgSr = bg.AddComponent<SpriteRenderer>();
        // klasrum_interior_background is the locker corner every placement in
        // this file and in Scene 3 was measured in. menu_bg is the old wide
        // shot and every number here lands somewhere else on it, so it is a
        // last resort that keeps the scene playable rather than one that
        // looks right.
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

        // --- The bin ---
        // Furniture: it is there from the first frame, not conjured by the
        // word. A bin that appears when "Tapon" is recognised teaches that
        // the word summons a bin.
        GameObject bin = CreateBin();

        // --- Kylo, already seated ---
        // The listening rig stays switched on and separate from the pose, the
        // lesson Bahay Scene 5 taught: with VoiceCommand on an object that
        // gets switched off, its Update stops and the Editor's Enter key dies
        // while real speech keeps working.
        GameObject rig = new GameObject("Kylo_Rig");
        rig.tag = "Player";
        rig.transform.position = new Vector3(SitX, SitFeetY, 0f);

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
        GameObject propBottle = CreatePropOptional("Prop_Bote", "prop_bote",
                                                    BottleX, BottleHeight, BottleBaseY);
        foreach (GameObject prop in new[] { propNotebook, propAklat, propPencil, propBottle })
            if (prop != null) prop.SetActive(true);
        // The pencil lies on the notebook, so it has to beat the notebook.
        if (propPencil != null)
            propPencil.GetComponent<SpriteRenderer>().sortingOrder = OrderDeskProps + 1;

        // Switched off until their words put them there.
        GameObject propBaon = CreatePropOptional("Prop_Baon", "prop_baon_open",
                                                  BaonX, BaonHeight, BaonBaseY);
        GameObject propBaonEmpty = CreatePropOptional("Prop_Baon_Empty", "prop_baon_empty",
                                                       BaonX, BaonHeight, BaonBaseY);
        GameObject propBasura = CreatePropOptional("Prop_Basura", "prop_basura",
                                                    BasuraX, BasuraHeight, BasuraBaseY);

        // --- The two objects that travel ---
        // Separate GameObjects from the ones that sit on the desk, because
        // PropToss owns its target's position and puts it back where the
        // throw started when it is done. Sharing one object with the desk
        // would leave the desk's pencil hanging in mid-air on a replay.
        PropToss pencilToss = CreateToss("Toss_Lapis", "prop_lapis", PencilHeight,
                                          new Vector3(HandChestX, HandChestY, 0f),
                                          new Vector3(BagMouthX, BagMouthY, 0f));
        // Into a bag rather than across a room: a short arc, barely turning,
        // which is a thing being put down rather than thrown.
        pencilToss.arcHeight = 0.40f;
        pencilToss.duration = 0.50f / Pace;
        pencilToss.spin = 45f;
        pencilToss.settleDrop = 0.30f;
        pencilToss.settleScale = 0.75f;

        PropToss basuraToss = CreateToss("Toss_Basura", "prop_basura", BasuraHeight,
                                          new Vector3(HandHighX, HandHighY, 0f),
                                          BinMouth());
        basuraToss.arcHeight = 1.25f;
        basuraToss.duration = 0.62f / Pace;
        basuraToss.spin = 210f;
        basuraToss.settleDrop = 0.42f;
        basuraToss.settleScale = 0.55f;

        // ================= "Ligpit": the lesson goes in the bag =============
        //
        // Four PoseSequences, chained end to end. See the note at the top for
        // why it is four and not one: each leg takes ONE thing off the desk
        // and owns the frame event that does it.
        //
        //   A  the notebook   place_frame_*  +  reach_bag  +  kuha_frame_*
        //   B  the textbook   aklat_frame_*  in place of three of those
        //   C1 the pencil     picked up off the desk and raised
        //   C2               his hand goes to the bag and the pencil drops in
        //
        // Legs A and B are Paaralan Scene 2's "Kuha" with the frame list
        // written the other way round, and that is the whole trick: the
        // movement of a child moving a flat object between a desk and a bag
        // is the same movement in both directions.
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
        }, "place_frame_3_uniform", 9f * Pace);
        // Frame 1 is place_frame_2, where the notebook is IN HIS HANDS. That
        // is the frame it has to leave the desk on - one either side and it
        // is either in two places or in none, which is rule 5.
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
        }, "place_frame_3_uniform", 9f * Pace);
        if (propAklat != null)
        {
            ligpitB.fireOnFrame = 1;
            UnityEventTools.AddBoolPersistentListener(
                ligpitB.onFrameReached, propAklat.SetActive, false);
        }

        PoseSequence ligpitC1 = AddPose(seatGO, seatSr, new[]
        {
            "place_frame_3_uniform",
            "lapis_frame_1_uniform",    // fingers closed round the pencil, on the desk
            "lapis_frame_2_uniform",    // raised to his chest
        }, "lapis_frame_2_uniform", 9f * Pace);
        if (propPencil != null)
        {
            ligpitC1.fireOnFrame = 1;
            UnityEventTools.AddBoolPersistentListener(
                ligpitC1.onFrameReached, propPencil.SetActive, false);
        }

        PoseSequence ligpitC2 = AddPose(seatGO, seatSr, new[]
        {
            "kuha_frame_1_uniform",     // he turns to the bag...
            "kuha_frame_2_uniform",
            "kuha_frame_3_uniform",     // ...hand in
            "kuha_frame_2_uniform",     // ...and out
            "kuha_frame_1_uniform",
            "place_frame_3_uniform",
        }, "sitting_front_uniform", 9f * Pace);
        // Frame 0, the instant his drawn hand stops holding a pencil. The
        // toss starts at HandChest, which is exactly where lapis_frame_2 had
        // it, so the drawn pencil and the flying one are the same pencil in
        // the same place on consecutive frames.
        ligpitC2.fireOnFrame = 0;
        UnityEventTools.AddPersistentListener(ligpitC2.onFrameReached, pencilToss.Play);

        // The chain. onFinished runs after the idle pose is set and after
        // running has gone false, so the next leg's Play() is free to take
        // the renderer.
        UnityEventTools.AddPersistentListener(ligpitA.onFinished, ligpitB.Play);
        UnityEventTools.AddPersistentListener(ligpitB.onFinished, ligpitC1.Play);
        UnityEventTools.AddPersistentListener(ligpitC1.onFinished, ligpitC2.Play);

        // ================= "Kuha": his baon comes out ========================
        //
        // Paaralan Scene 2's "Kuha" forwards, with the lunchbox swapped in
        // for the notepad by Tools/make_recess_frames.py. SIX FRAMES RATHER
        // THAN SEVEN: that file explains why there is no lifted-beside-the-
        // shoulder frame for a lunchbox - the short version is that the
        // notepad's footprint there is tall and thin and a box scaled to
        // cover it comes out wider than his head.
        PoseSequence kuhaFrames = AddPose(seatGO, seatSr, new[]
        {
            "kuha_frame_1_uniform",     // he turns to the bag
            "kuha_frame_2_uniform",
            "kuha_frame_3_uniform",     // hand in
            "kuha_baon_bag_uniform",    // closed on the box
            "kuha_baon_2_uniform",      // carried in both hands at his chest
            "place_frame_3_uniform",    // laid down, hands back on the desk
        }, "sitting_front_uniform", 10f * Pace);
        // Frame 5 is place_frame_3: his hands are flat and the box has left
        // them. That is the frame the prop arrives on.
        if (propBaon != null)
        {
            kuhaFrames.fireOnFrame = 5;
            UnityEventTools.AddBoolPersistentListener(
                kuhaFrames.onFrameReached, propBaon.SetActive, true);
        }

        // ================= "Kain": he eats, and KEEPS eating =================
        //
        //   lead-in   place_frame_3   hands to the desk
        //             kain_hold       a sandwich out of the box
        //             kain_lift       raised
        //             kain_bite_1     at his mouth, head down to meet it
        //   loop      bite_2, bite_1, lift, bite_1   - ENDLESS
        //   tail      kain_last       the last of it, coming down
        //             place_frame_3   hands back on the desk
        //
        // LOOPCYCLES OF ZERO, the same as Scene 3's reading. The project's
        // rule 10 is that only what repeats in life should loop, and eating
        // repeats until the food is gone or something interrupts - here, the
        // next word. Zero means "until Stop()", and Inom below is what calls
        // it. So he eats for as long as the child takes.
        //
        // THE LOOP GOES BACK DOWN TO kain_lift EACH TIME ROUND, which is the
        // difference between eating and a hand vibrating at a face: bite,
        // chew, hand down, bite again.
        PoseSequence kainFrames = AddPose(seatGO, seatSr, new[]
        {
            "place_frame_3_uniform",
            "kain_hold_uniform",
            "kain_lift_uniform",
            "kain_bite_1_uniform",
        }, "sitting_front_uniform", 10f * Pace);
        kainFrames.loopFrames = Sprites(
            "kain_bite_2_uniform",      // chewing, head back up
            "kain_bite_1_uniform",      // another bite
            "kain_lift_uniform",        // hand down between mouthfuls
            "kain_bite_1_uniform");
        kainFrames.loopCycles = 0;                     // until Stop() - see above
        // He finishes what is in his hand on the way down, so the sandwich
        // does not vanish whole. kain_last is the same grip with half the
        // bread in it.
        kainFrames.tailFrames = Sprites("kain_last_uniform", "place_frame_3_uniform");
        // Slow. This may be on screen for half a minute while the scene waits
        // for the next word, and a fast chew becomes the thing being watched
        // instead of the word being learned.
        kainFrames.loopFramesPerSecond = 6f * Pace;
        // The food leaves the box on the frame it appears in his hand, and
        // what is left on the desk is the same box, open and empty.
        if (propBaon != null && propBaonEmpty != null)
        {
            kainFrames.fireOnFrame = 1;
            UnityEventTools.AddBoolPersistentListener(
                kainFrames.onFrameReached, propBaon.SetActive, false);
            UnityEventTools.AddBoolPersistentListener(
                kainFrames.onFrameReached, propBaonEmpty.SetActive, true);
        }
        // And the wrapper is on the desk once he has finished - which, with
        // an endless loop, is when "Inom" interrupts it and its tail has
        // played. That is one word before "Tapon" needs it.
        if (propBasura != null)
            UnityEventTools.AddBoolPersistentListener(
                kainFrames.onFinished, propBasura.SetActive, true);

        // ================= "Inom": he drinks =================================
        //
        // Played through once with two slow gulps in the middle rather than
        // looped endlessly: rule 10 lists drinking among the things that
        // happen once, and the scene has a word after this one.
        PoseSequence inomFrames = AddPose(seatGO, seatSr, new[]
        {
            "place_frame_3_uniform",
            "inom_hold_uniform",        // the bottle comes off the desk
            "inom_lift_uniform",        // raised, tipping
            "inom_drink_1_uniform",     // at his mouth, head back
        }, "sitting_front_uniform", 10f * Pace);
        inomFrames.loopFrames = Sprites("inom_drink_2_uniform", "inom_drink_1_uniform");
        inomFrames.loopCycles = 2;
        inomFrames.loopFramesPerSecond = 5f * Pace;    // a swallow is slower than a reach
        inomFrames.tailFrames = Sprites(
            "inom_lift_uniform", "inom_hold_uniform", "place_frame_3_uniform");
        // END THE EATING FIRST, rather than cutting it off. Without this,
        // Play() would take the renderer off "Kain" wherever its loop
        // happened to be - the sandwich still up at his mouth - and start the
        // bottle coming up through it. With it, the eating plays its own
        // tail: he finishes the last of it and puts his hands down, and only
        // then does the bottle come off the desk. See PoseSequence.interrupts.
        inomFrames.interrupts = kainFrames;
        // The bottle cannot be on the desk and at his mouth at once.
        if (propBottle != null)
        {
            inomFrames.fireOnFrame = 1;
            UnityEventTools.AddBoolPersistentListener(
                inomFrames.onFrameReached, propBottle.SetActive, false);
            UnityEventTools.AddBoolPersistentListener(
                inomFrames.onFinished, propBottle.SetActive, true);
        }

        // ================= "Tapon": the wrapper goes in the bin ==============
        //
        // Two sequences again, for the same reason Ligpit has four: the
        // wrapper has to leave the DESK on one frame and leave his HAND on
        // another, and a PoseSequence fires one frame event.
        //
        //   pick   place_frame_3, tapon_hold, tapon_lift
        //   throw  tapon_cock, place_frame_3
        //
        // THE THROW IS TWO FRAMES AND THE REST IS THE PROP. A seated boy
        // drawn from the front cannot wind up and follow through - there is
        // no arm drawn at those angles, and Docs/AnimationStandard is blunt
        // that a pose he is not in is a generation. What the drawing CAN do
        // is hold the wrapper up beside his head and then have empty hands on
        // the desk; what carries the meaning of the word is the wrapper
        // itself, which leaves the exact point his drawn hand had it, arcs
        // across the room and drops into the bin. See PropToss.
        PoseSequence taponPick = AddPose(seatGO, seatSr, new[]
        {
            "place_frame_3_uniform",
            "tapon_hold_uniform",       // the wrapper comes off the desk
            "tapon_lift_uniform",       // raised
        }, "tapon_lift_uniform", 10f * Pace);
        if (propBasura != null)
        {
            taponPick.fireOnFrame = 1;
            UnityEventTools.AddBoolPersistentListener(
                taponPick.onFrameReached, propBasura.SetActive, false);
        }

        PoseSequence taponThrow = AddPose(seatGO, seatSr, new[]
        {
            "tapon_cock_uniform",       // back by his head, head tipped at the bin
            "place_frame_3_uniform",    // and it has gone
        }, "sitting_front_uniform", 9f * Pace);
        // HE WATCHES IT LAND, and that is a timing fix as much as a piece of
        // acting. The throw is two frames - a third of a second - and the
        // wrapper is in the air for a second and a half: flight, then the
        // sink into the bin. SceneObjectiveController waits for POSES, not
        // for PropToss, so without this the word is the last of the scene,
        // the pose ends, and the result panel fades up over a wrapper still
        // crossing the room.
        //
        // One frame in the loop, played slowly three times, is a hold: his
        // hands stay flat on the desk where the throw left them while the
        // wrapper finishes its journey. A hold rather than a longer tail
        // because there is no movement owed - the action is over and the
        // object is what is still happening.
        taponThrow.loopFrames = Sprites("place_frame_3_uniform");
        taponThrow.loopCycles = 3;
        taponThrow.loopFramesPerSecond = 3f * Pace;
        // Frame 1: the frame his hand is empty on. The toss starts at
        // HandHigh, which is where tapon_cock drew the wrapper, so it leaves
        // from where it was rather than appearing somewhere else.
        taponThrow.fireOnFrame = 1;
        UnityEventTools.AddPersistentListener(taponThrow.onFrameReached, basuraToss.Play);
        UnityEventTools.AddPersistentListener(taponPick.onFinished, taponThrow.Play);

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
        // On the bag, which is drawn into his own sprite at the bottom right.
        TimedOverlay gleamKuha = CreateGleamAt("Gleam_Kuha", BagMouthX, BagMouthY, 2.4f);
        TimedOverlay gleamKain = CreateGleamAt("Gleam_Kain", BaonX,
                                                BaonBaseY + BaonHeight * 0.5f, 2.0f);
        TimedOverlay gleamInom = CreateGleamAt("Gleam_Inom", BottleX,
                                                BottleBaseY + BottleHeight * 0.5f, 1.8f);
        // On the BIN, not on the wrapper: the word is where the rubbish goes.
        TimedOverlay gleamTapon = CreateGleamAt("Gleam_Tapon", BinX,
                                                 BinMouth().y, 2.6f);

        // CreateGleamAt leaves them on the "Props" layer, which this project
        // sorts BELOW "Player" - any left there is drawn behind Kylo and
        // behind the desk in front of him. Every one of these marks something
        // on or above the desk, so every one is moved up.
        foreach (TimedOverlay fx in new[] { gleamLigpit, gleamKuha, gleamKain,
                                            gleamInom, gleamTapon })
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
        // his own sprite - a badge there is a label pinned to his chest.
        GameObject pLigpit = CreatePromptUI(uiRoot.transform, "Prompt_Ligpit", "Ligpit", new Vector3(-150, 120, 0));
        GameObject pKuha = CreatePromptUI(uiRoot.transform, "Prompt_Kuha", "Kuha", new Vector3(610, -60, 0));
        GameObject pKain = CreatePromptUI(uiRoot.transform, "Prompt_Kain", "Kain", new Vector3(610, -180, 0));
        GameObject pInom = CreatePromptUI(uiRoot.transform, "Prompt_Inom", "Inom", new Vector3(-150, 10, 0));
        GameObject pTapon = CreatePromptUI(uiRoot.transform, "Prompt_Tapon", "Tapon", new Vector3(-150, -230, 0));

        // --- Objectives: pack up, get the food, eat, drink, throw away ---
        // In the order break actually happens in, which is also the order the
        // objects need to be in: the desk has to be clear before the baon can
        // go on it, the box has to be open before he can eat out of it, and
        // the wrapper does not exist until he has.
        //
        // EVERY WORD DRIVES ITS OWN POSE SEQUENCE AND NOTHING ELSE'S, which
        // is what keeps the five beats from overlapping: Play() takes the
        // renderer off any other action running on the same figure. The only
        // crossings are the ones that are wanted - the Ligpit and Tapon
        // chains, and inomFrames.interrupts - and those wait rather than cut.
        //
        // The prop switching is NOT wired here. It belongs to the frame of
        // the animation where the object changes hands, which is why it is on
        // fireOnFrame / onFinished up with each sequence instead.
        var ligpit = MakeObjective("Ligpit", pLigpit);
        UnityEventTools.AddPersistentListener(ligpit.onCorrect, ligpitA.Play);
        UnityEventTools.AddPersistentListener(ligpit.onCorrect, gleamLigpit.Play);

        var kuha = MakeObjective("Kuha", pKuha);
        UnityEventTools.AddPersistentListener(kuha.onCorrect, kuhaFrames.Play);
        UnityEventTools.AddPersistentListener(kuha.onCorrect, gleamKuha.Play);

        var kain = MakeObjective("Kain", pKain);
        UnityEventTools.AddPersistentListener(kain.onCorrect, kainFrames.Play);
        UnityEventTools.AddPersistentListener(kain.onCorrect, gleamKain.Play);

        var inom = MakeObjective("Inom", pInom);
        UnityEventTools.AddPersistentListener(inom.onCorrect, inomFrames.Play);
        UnityEventTools.AddPersistentListener(inom.onCorrect, gleamInom.Play);

        var tapon = MakeObjective("Tapon", pTapon);
        UnityEventTools.AddPersistentListener(tapon.onCorrect, taponPick.Play);
        UnityEventTools.AddPersistentListener(tapon.onCorrect, gleamTapon.Play);

        SceneObjectiveController controller = uiRoot.AddComponent<SceneObjectiveController>();
        controller.voiceCommand = voiceCommand;
        controller.playerMovement = player;
        controller.objectives = new[] { ligpit, kuha, kain, inom, tapon };
        // The default four seconds is nowhere near enough for this scene.
        //
        // It is a safety cap on how long the sequence waits for an action to
        // finish before asking the next word anyway. "Ligpit" is four chained
        // sequences and twenty-five frames at about six a second - a little
        // over four seconds - and "Inom" has to end the eating before it can
        // start. Eight gives both of them room without leaving a child
        // waiting if a sequence ever fails to land.
        //
        // The endless eating loop does NOT eat this budget: PoseSequence
        // reports it through IsHoldingLoop and SceneObjectiveController steps
        // over it, so "Inom" is asked for while he is still eating rather
        // than eight seconds later.
        controller.maxActionWait = 8f;

        // OFF THE DESK AND TO THE LEFT, exactly as Scene 3 does it and for
        // the same reason: the desk fills the bottom of this frame and the
        // things the child has to watch are all right of centre. The bin is
        // the one new thing on the left, and it starts at canvas x -205,
        // which these clear.
        VoiceUIBuilder.BuildVoiceUI(voiceCommand, controller,
                                     new Vector2(-620, -400), new Vector2(-370, -400));
        // Still needed in the scene: SceneResultPanel looks the fader up at
        // run time to fade out of whichever button is pressed.
        SceneFaderBuilder.Build();

        // --- A middle scene of Level 2: Scene 5 ends it ---
        // The counter is added by hand here, because DatabaseDemoBuilder used
        // to bring one along with the result panel and that panel has moved
        // on. Without it the five words said at recess leave the level total.
        GameObject runScoreGO = new GameObject("RunScore");
        RunScoreCounter runScore = runScoreGO.AddComponent<RunScoreCounter>();
        runScore.voiceCommand = voiceCommand;
        runScore.objectiveController = controller;
        runScore.resetOnStart = false;   // Scene 1 starts the run

        // --- The scene's summary, whose Next button hands on ---
        SceneResultPanelBuilder.Build(controller, voiceCommand, NextScene);

        System.IO.Directory.CreateDirectory("Assets/Scenes");
        EditorSceneManager.SaveScene(scene, ScenePath);
        AddToBuildSettings();

        Debug.Log("[SALINLAHI] Paaralan Scene 4 built and saved to " + ScenePath +
                   ". Words: Ligpit, Kuha, Kain, Inom, Tapon - all five drawn. Staged at " +
                   "SCENE 3'S DESK, in Scene 3's room, at Scene 3's scale: break happens " +
                   "where the lesson did. Ligpit puts the notebook, the textbook and the " +
                   "pencil in his bag one at a time; Kuha takes his baon out of it; Kain " +
                   "eats until Inom is said, however long that takes; Inom drinks and puts " +
                   "the bottle back; Tapon throws the wrapper, and the wrapper itself flies " +
                   "into the open bin beside him. It fades into " + NextScene + ". " +
                   "NOTE: Paaralan Scene 5 also teaches Ligpit - see the comment at the " +
                   "top of this file.");
    }

    /// <summary>
    /// One of Kylo's drawn actions, as a PoseSequence on the seated figure.
    ///
    /// Every one in this scene has the same shape - a list of frame names, an
    /// idle pose to land on and a rate - so they are built here rather than
    /// eight times over. playOnEnable is always off: the seated Kylo is on
    /// screen from the first frame, so a sequence that played on enable would
    /// fire the whole action the moment the scene loaded.
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

    /// <summary>Where the middle of the bin's opening is, in world space.</summary>
    private static Vector3 BinMouth()
    {
        return new Vector3(BinX, BinFootY + BinHeight * BinMouthUp, 0f);
    }

    /// <summary>
    /// The bin, standing beside the desk with its foot below the frame.
    ///
    /// Drawn at OrderBin, which is in front of the desk layer and behind the
    /// things on the desk. It is nearer the camera than the desk is, and
    /// nothing on the desk is ever in front of it, so either would do - this
    /// way round is simply what it looks like.
    /// </summary>
    private static GameObject CreateBin()
    {
        Sprite sprite = LoadSpriteOptional("prop_basurahan");
        if (sprite == null) return null;

        GameObject go = new GameObject("Prop_Basurahan");
        SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = sprite;
        sr.sortingLayerName = "Player";
        sr.sortingOrder = OrderBin;

        float s = sprite.bounds.size.y > 0.0001f ? BinHeight / sprite.bounds.size.y : 1f;
        go.transform.localScale = new Vector3(s, s, 1f);
        go.transform.position = new Vector3(BinX, BinFootY + BinHeight * 0.5f, 0f);
        return go;
    }

    /// <summary>
    /// An object that travels: its own GameObject, drawn in front of
    /// everything, switched off until it is thrown.
    /// </summary>
    private static PropToss CreateToss(string name, string spriteFile,
                                        float worldHeight, Vector3 from, Vector3 to)
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
        go.transform.position = from;

        // The component lives on a PARENT that stays switched on, and moves
        // the child. PropToss switches its target's GameObject off when the
        // throw lands, and a component cannot run a coroutine on an object it
        // has just switched off - put them together and the second half of
        // the flight never happens.
        GameObject holder = new GameObject(name + "_Driver");
        PropToss toss = holder.AddComponent<PropToss>();
        toss.prop = go.transform;
        toss.from = from;
        toss.to = to;
        go.SetActive(false);
        return toss;
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
