// Assets/Editor/BuildPaaralanScene3.cs
//
// One-click scene builder for Paaralan Scene 3 ("Vocabulary Lesson").
// Teaches four words: Aklat, Basa, Sulat, Sagot.
//
// ALL FOUR ARE ACTIONS, which is the point of the list and worth not undoing.
// The scene used to teach Aklat, Basa, Sulat, Tanong and Sagot - a list that
// mixed a THING (Aklat, a book) in with things Kylo does, and the thing was
// the only one the scene could really show, because making an object appear
// is easy and drawing a child reading is not. Writing, reading, putting a
// hand up and taking a drink are four things he does, and every one of them
// needs a drawn pose before it is really taught. That is a harder scene to
// finish and a better one.
//
// Sulat is WRITE and Basa is READ. They are easy to swap by accident, and
// swapping them would teach the wrong word with the right animation.
//
// INOM IS NOT A NEW WORD. Bahay Scene 5 and Parke Scene 4 both teach it
// already - the design document's largest-vocabulary-gain note no longer
// applies to this scene. Three words are new here, not four. Meeting a word
// again in a different room is reinforcement rather than waste, but it IS a
// repeat, and the level's word count should not be added up as though it were
// not.
//
// This is the scene the design document calls the core learning scene: the
// one where the child is not fetching objects but taking part in a lesson.
//
// A MIDDLE SCENE OF LEVEL 2. Scene 2 fades into this one; this one fades into
// Scene 4, which ends the level and shows the result. It held that ending
// itself until Scene 4 existed.
//
// STAGING. The same classroom, the same desk, the same seat as Scene 2 - the
// child sat down at the end of that scene and the lesson begins where they
// are. Scene 2 has since MOVED: it is now the locker corner of
// klasrum_interior_background, with Kylo nearly six world units tall, seated
// at the near desk at x 2.86 and cropped at that desk's far edge. This scene
// follows it there. Every placement below is Scene 2's own, copied rather
// than re-derived, so that leaving Scene 2 and arriving here does not move
// the furniture.
//
// Three things followed from that move:
//
//   - No foreground desk layer. klasrum_desk_front was cut from the OLD wide
//     classroom shot and lies across the bottom third of a picture it does
//     not belong to, cutting Kylo off at the neck. The occlusion it gave is
//     already handled: the seated poses are cropped at the desk line, so the
//     crop IS the desk edge.
//   - No Teacher Rachel. char_teacher is a waist-up drawing made for the
//     chalkboard view; in a corner of lockers she has nowhere to stand and
//     floats against the doors. Scene 2 cut her for that reason, and this
//     scene cannot keep her while sharing its background.
//   - The tee, not the polo. sitting_front_uniform is the pose Scene 2 ends
//     on, so his shirt does not change between one scene and the next.
//
// WHAT EACH WORD DOES. All four are drawn now - this scene was the one the
// animation standard listed as "in progress", and it is not any more:
//
//   Aklat    he turns to his bag, reaches in, closes his hand on the
//            textbook, lifts it clear, carries it round in front of him and
//            lays it SHUT on the desk, then puts his hands down.
//   Basa     he reaches to the desk, picks the shut book up, OPENS it in
//            both hands, looks down at it - and KEEPS READING, with no end
//            of its own, until the next word is said.
//   Sulat    he puts his hands on the desk, takes up the pencil, and
//            writes with his fingers closed round it and his head down -
//            the point ON the page, tracking left to right across it.
//   Sagot    his hand goes up off the desk, he gets to his feet, and leans
//            towards the front of the class and nods as he answers.
//
// THE READING IS THE ONE THAT IS DIFFERENT, and it is worth knowing why
// before changing anything here. Every other beat in the game is a run of
// frames that ends. "Basa" is a loop with loopCycles of 0, which
// PoseSequence runs until Stop() - and the thing that calls Stop is "Sulat"
// being recognised. So the length of the reading is however long the child
// takes, and the join between the two words is a hand-over rather than a
// cut: the book goes back on the desk before the pencil comes out. Two
// small pieces of machinery carry that, both documented where they live -
// PoseSequence.interrupts and PoseSequence.IsHoldingLoop.
//
// WHERE THE ART CAME FROM. Three of the four actions are built out of poses
// this project already had, which is the cheap path Docs/AnimationStandard
// lays out and the reason this scene could be finished at all:
//
//   Aklat    Paaralan Scene 2's "Kuha" - the same reach into the same bag -
//            with the notepad in his hands swapped for a textbook.
//            Tools/make_aklat_book.py draws the book from a Canva
//            generation; Tools/make_aklat_frames.py puts it in his hands.
//   Basa     the frame he carries the book in, with the shut book swapped
//            for an open one and his head and eyes brought down onto it.
//            Tools/make_aklat_open_book.py and make_basa_frames.py.
//   Sulat    lapis_frame_1 - Scene 2's pencil pick-up, the ONLY pose in
//            the project with his fingers closed round a pencil - looking
//            down at the page, with his hand travelling, plus the point of
//            the pencil on a layer in front of the desk so that it can
//            reach the paper at all. Tools/make_sulat_hand_frames.py and
//            make_sulat_point.py.
//   Sagot    "Tanong"'s hand-raise, which was drawn for a word this scene
//            no longer teaches, and then talking_uniform whole and
//            untouched, head-matched to the seated poses and raised until
//            the desk crosses him at the shorts.
//            Tools/make_sagot_frames.py.
//
// Sprites expected in Assets/Sprites/: see the SpriteImportFixer.Verify call
// in BuildScene, which is the list that is actually checked.
//
// Run via: Tools > SALINLAHI > Build Paaralan Scene 3

using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class BuildPaaralanScene3
{
    private const string SpriteFolder = "Assets/Sprites/";
    private const string ScenePath = "Assets/Scenes/Chapter2_Level3_Aralin.unity";
    private const string NextScene = "Chapter2_Level4_Recess";

    private const float ViewHeight = 10.8f;           // camera is orthographic 5.4
    private const float ViewWidth = 19.2f;

    // Scene 2's seat, copied exactly. These are measured values, not fitted
    // ones: the near desk in klasrum_interior_background has its far edge at
    // y -3.01 and its near edge at about -3.84, read off the painting, and the
    // seated sprite is pinned to that far edge so its crop reads as the desk.
    // The old -5.20 / -4.52 / 3.10 belonged to menu_bg and land on the open
    // door of the doorway view. See BuildPaaralanScene2 for the full working.
    private const float SitX = 2.86f;                 // the chair behind the near desk
    private const float SitFeetY = -3.009f;           // the desk's far edge
    private const float SeatedHeight = 5.609f;

    // What is on the desk, and why each thing is where it is.
    //
    // The desk top in this view was measured the same way as everything else
    // in these two scenes: scanning klasrum_interior_background for its wood
    // gives a surface running from y -3.00 down to about -3.85, spreading from
    // x -0.5..+5.1 at the far edge to x +0.9..+7.4 at the near one - wider
    // towards the camera, as a receding surface is.
    //
    // His own sprite covers x 0.84..4.88 of that, so the clear desk is a strip
    // either side of him, and the two props take one strip each.
    //
    // THE NOTEBOOK IS SCENE 2'S, UNMOVED. Same x, same base, same height, so
    // the book he took out of his bag in the last scene is still lying where
    // he put it. The height is not a free choice: his sprite's bottom edge IS
    // the desk's far edge at -3.01, so anything reaching above that line gets
    // drawn across his shirt.
    private const float NotebookX = 2.86f;
    private const float NotebookBaseY = -3.84f;
    private const float NotebookHeight = 0.83f;

    // BEWARE: THESE THREE SPRITES CARRY TRANSPARENT PADDING, so the world
    // height given here is the height of the BOX, not of the drawing inside
    // it. prop_aklat is 520x300 with its ink in a 481x218 box - 73% of the
    // height; prop_lapis is 440x150 with ink 424x74 - 49%. The numbers
    // below are chosen so that the DRAWN
    // object lands where it should, and the drawn spans are written out with
    // each one so the next person does not have to re-derive them.
    //
    // Not cropped to their ink the way prop_notebook_open was, because all
    // three are shared: prop_aklat is also in Paaralan 4 and 5, prop_lapis in
    // Paaralan 2 and 5. Cropping
    // would silently grow them in every one of those.

    // The textbook he reads from. On the desk from the start now - no word
    // puts it there any more. To the RIGHT of the notebook, because at 0.83
    // units tall the notebook is 3.91 wide and spans x 0.91..4.81, which is
    // the whole of the desk in front of him. Overlapping HIS x range would be
    // allowed - the notebook does it, and anything lying flat below -3.01 is
    // in front of him rather than behind - but the right strip is free, so it
    // may as well use it.
    //
    // Drawn span: x 4.50..6.10, y -3.80..-3.07. Sized UP from a first pass at
    // 0.83, which drew the book 1.33 wide and read as a second, smaller
    // notebook rather than as the textbook he is reading from. The cap is the
    // desk's right edge, which is at about +6.20 on the row this book's far
    // corner reaches - so this is as large as it goes without hanging off.
    private const float AklatX = 5.30f;
    private const float AklatBaseY = -3.84f;
    private const float AklatHeight = 1.00f;

    // The pencil, lying ON the open notebook and UNDER THE FIST THAT PICKS
    // IT UP, which is the whole of why it is at this x and not a prettier one.
    //
    // lapis_frame_1_uniform is the only drawing in the project with his
    // fingers closed round a pencil, and the pencil it draws there is centred
    // on world x 2.54 - fitted by putting prop_lapis' own pink eraser on the
    // drawn pink eraser at the drawn barrel's angle, which is the only part
    // of that pencil fully in view. The instant the pose changes to that
    // frame the drawn pencil is THERE, wherever the resting one happens to
    // be, so the distance between the two is a pencil skidding across the
    // desk as he picks it up and skidding back as he puts it down. This used
    // to be 3.60, on the notebook's right-hand page, and that gap was 1.06
    // units - a quarter of his own width, twice per "Sulat". At 2.54 the
    // pickup is a straight lift with no sideways component at all.
    //
    // STILL ON THE NOTEBOOK, just left of the spine instead of on the right
    // page. prop_notebook_open is 965x205 drawn 0.83 tall, so it runs x
    // 0.91..4.81; the pencil's drawn span of x 2.12..2.96, y -3.31..-3.16
    // sits inside it and inside its own -3.84..-3.01. Drawn one sorting order
    // above the notebook so it is not a coin-flip which of the two wins.
    //
    // THE SAME NUMBER IS IN BuildPaaralanScene4, which is this desk ten
    // minutes later and opens on the frame this scene closes on. Move one and
    // not the other and the pencil hops as the scene changes - the same drift
    // the staging constants above are written out to prevent.
    private const float PencilX = 2.54f;
    private const float PencilBaseY = -3.35f;
    private const float PencilHeight = 0.30f;

    // One knob for the speed of every drawn action in this scene, the same
    // way Paaralan Scene 2 does it. Anything measured in SECONDS divides by
    // Pace; anything measured PER SECOND multiplies by it. Below 1 is slower.
    //
    // It was left out while this scene had no animation to slow - a constant
    // multiplying nothing is just a thing to get wrong later. Sulat is drawn
    // now, so it earns its place, and it matches Scene 2's 0.65 so the two
    // scenes move at the same speed.
    private const float Pace = 0.65f;

    private const int OrderKylo = 0;

    // Between Kylo and the things on the desk: it has to cover the bottom of
    // him, and the notebook has to sit on top of it.
    private const int OrderDeskFront = 10;
    private const int OrderDeskProps = 20;            // his things, on top of the desk

    [MenuItem("Tools/SALINLAHI/Build Paaralan Scene 3")]
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
            "sitting_front_uniform", "prop_aklat", "prop_notebook_open",
            "prop_lapis", "klasrum_desk_front_near", "fx_gleam",
            // "Aklat": Paaralan Scene 2's reach into the bag, with the
            // textbook in his hands instead of the notepad.
            "kuha_frame_1_uniform", "kuha_frame_2_uniform", "kuha_frame_3_uniform",
            "aklat_bag_uniform", "aklat_frame_1_uniform", "aklat_frame_2_uniform",
            "prop_aklat_closed",
            // "Basa": he opens it, and looks down at it.
            "aklat_open_a_uniform", "aklat_open_b_uniform",
            "basa_frame_1_uniform", "basa_frame_2_uniform",
            "basa_frame_3_uniform", "basa_frame_4_uniform",
            // "Sulat": Paaralan Scene 2's pencil pick-up, which is the only
            // pose in the project with his fingers closed round a pencil.
            "place_frame_3_uniform", "lapis_frame_1_uniform",
            "sulat_hand_1_uniform", "sulat_hand_2_uniform", "sulat_hand_3_uniform",
            "sulat_point_uniform",
            // "Sagot": his hand going up, and him getting to his feet.
            "tanong_frame_1_uniform", "tanong_frame_2_uniform", "tanong_frame_3_uniform",
            "sagot_rise_1_uniform", "sagot_rise_2_uniform", "sagot_rise_3_uniform",
            "sagot_stand_uniform",
            "sagot_answer_1_uniform", "sagot_answer_2_uniform",
            "ui_arrow", "ui_word_badge", "badge_paaralan",
        }, "Paaralan Scene 3");

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

        // --- The classroom ---
        GameObject bg = new GameObject("Background_Klasrum");
        SpriteRenderer bgSr = bg.AddComponent<SpriteRenderer>();
        // Scene 2's room, and the same corner of it. klasrum_background is the
        // old wide shot; klasrum_interior_background is the locker corner this
        // desk was measured in, and every placement in this file assumes it.
        // So it is asked for first, and menu_bg is a last resort that keeps
        // the scene playable rather than one that looks right.
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
        //
        // The seated poses are cut off flat at the bottom of their canvas -
        // fix_seated_hands.py does it deliberately, replacing his hands with
        // the forearm cross-section so his arms run down "behind the desk".
        // That only works if something covers the cut, and nothing did: Kylo
        // draws at order 0 over the background, so the slice landed ON the
        // painted wood. "Upo" showed his forearms ending in flat stumps, and
        // "Lapis" showed the sliced shirt hem as two white slabs either side
        // of his shorts. One cut, seen in two poses.
        //
        // There was nowhere to move him to. The slice sits at world -2.967
        // and the desk's painted top edge runs between -2.92 and -3.00
        // depending where it is measured, because the surface recedes. Lower
        // him and the slice is drawn further down the wood; raise him and
        // bare background shows underneath. He is in front of the desk
        // either way.
        //
        // So the desk is cut out of THIS room's own painting by
        // Tools/cut_klasrum_desk_front.py and laid over him. It matches by
        // construction - same artwork, same perspective, same light - and it
        // is written to the background's full canvas with everything above
        // the desk transparent, so it takes the background's own transform
        // and the two register pixel for pixel.
        //
        // NOT klasrum_desk_front, which is the old wide classroom's whole
        // front row and cuts him off at the neck in this view.
        GameObject deskFront = new GameObject("Foreground_Desk");
        SpriteRenderer deskFrontSr = deskFront.AddComponent<SpriteRenderer>();
        deskFrontSr.sprite = LoadSpriteOptional("klasrum_desk_front_near");
        deskFrontSr.sortingLayerName = "Player";
        deskFrontSr.sortingOrder = OrderDeskFront;
        if (deskFrontSr.sprite == null) deskFront.SetActive(false);
        else deskFront.transform.localScale = bg.transform.localScale;

        // --- No Teacher Rachel ---
        // See the note at the top of this file: she is a waist-up drawing made
        // for the chalkboard view and has nowhere to stand in a corner of
        // lockers. Nothing in the word list needs her now - "Sagot", the one
        // word that pointed at her, is no longer taught here.

        // --- Kylo, already seated ---
        // The listening rig stays switched on and separate from the pose, the
        // lesson Bahay Scene 5 taught: with VoiceCommand on an object that
        // gets switched off, its Update stops and the Editor's Enter key dies
        // while real speech keeps working.
        GameObject rig = new GameObject("Kylo_Rig");
        rig.tag = "Player";
        rig.transform.position = new Vector3(SitX, SitFeetY, 0f);

        // sitting_front_uniform, the pose Scene 2 ends on: the tee rather than
        // the polo, hands cut off below the sleeve so the arms run down behind
        // the desk, and the canvas cropped at the desk's far edge.
        // sitting_desk_uniform is the old polo drawing, and using it here
        // changes his shirt between one scene and the next.
        GameObject seatGO = CreateFigure("Kylo_Seated", "sitting_front_uniform",
                                          SitX, SitFeetY, SeatedHeight);

        // --- No foreground desk layer ---
        // See the note at the top. The seated pose is cropped at the desk
        // line, so that crop is the occlusion klasrum_desk_front used to give.

        // --- What is on the desk ---
        // The notebook and pencil are out already - he took them from his bag
        // in Scene 2 - so they are switched on below. The TEXTBOOK is not:
        // "Aklat" is the word that puts it there, which is why that word
        // leads the lesson. CreatePropOptional builds a prop switched off,
        // so only the two he already owns are turned on here.
        //
        // prop_aklat_CLOSED, not prop_aklat. A book arrives on a desk shut,
        // and opening it is what the NEXT word does - leaving the open one
        // here meant "Basa" had nothing left to do but light up a book that
        // was already open before he had touched it. Same box as prop_aklat
        // (520x300) so AklatX and AklatHeight still mean what they meant,
        // with a smaller drawing inside it, because a shut book covers less
        // desk than an open one. Tools/make_aklat_open_book.py draws both.
        GameObject propAklat = CreatePropOptional("Prop_Aklat", "prop_aklat_closed",
                                                   AklatX, AklatHeight, AklatBaseY);
        // prop_notebook_open, not prop_kuwaderno: the shut blue hardback is
        // the wrong object for a lesson being written in, and the open book is
        // what Scene 2 leaves lying on this desk.
        GameObject propNotebook = CreatePropOptional("Prop_Kuwaderno", "prop_notebook_open",
                                                      NotebookX, NotebookHeight, NotebookBaseY);
        GameObject propPencil = CreatePropOptional("Prop_Lapis", "prop_lapis",
                                                    PencilX, PencilHeight, PencilBaseY);
        // No mug here any more. It was put on this desk for "Inom", and the
        // restructure moves that word to Paaralan 4 and Parke 4, where a
        // drink belongs to the scene rather than to a lesson.
        foreach (GameObject prop in new[] { propNotebook, propPencil })
            if (prop != null) prop.SetActive(true);
        // The pencil lies on the notebook, so it has to beat the notebook.
        if (propPencil != null)
            propPencil.GetComponent<SpriteRenderer>().sortingOrder = OrderDeskProps + 1;

        SpriteRenderer seatSr = seatGO.GetComponent<SpriteRenderer>();

        // --- "Aklat": he takes the textbook out of his bag ---
        //
        //   lead-in   kuha_frame_1..3   he turns to the bag and reaches in
        //             aklat_bag         his hand closes on the book
        //             aklat_frame_1     it comes up clear of the bag
        //             aklat_frame_2     carried in both hands at his chest
        //             place_frame_3     laid down, hands back on the desk
        //
        // NONE OF THESE SEVEN FRAMES IS NEW ART. They are Paaralan Scene 2's
        // "Kuha" - the same boy taking his NOTEPAD out of the same bag - with
        // the object in his hands changed by Tools/make_aklat_frames.py. The
        // movement of a child lifting a flat rectangular thing from a bag to
        // a desk is the same movement whichever flat rectangular thing it is,
        // and reusing it means his hands, his lean and his proportions cannot
        // drift. See that file for how the swap is made and what it cannot do.
        //
        // The book itself is drawn by Tools/make_aklat_book.py from a Canva
        // generation kept at Art/aklat_book_raw.png - deep red cover, cream
        // pages, the shut version of the open prop_aklat that ends up on the
        // desk.
        PoseSequence aklatFrames = seatGO.AddComponent<PoseSequence>();
        aklatFrames.target = seatSr;
        aklatFrames.frames = new[]
        {
            LoadSpriteOptional("kuha_frame_1_uniform"),
            LoadSpriteOptional("kuha_frame_2_uniform"),
            LoadSpriteOptional("kuha_frame_3_uniform"),
            LoadSpriteOptional("aklat_bag_uniform"),
            LoadSpriteOptional("aklat_frame_1_uniform"),
            LoadSpriteOptional("aklat_frame_2_uniform"),
            LoadSpriteOptional("place_frame_3_uniform"),
        };
        aklatFrames.destinationSprite = LoadSpriteOptional("sitting_front_uniform");
        aklatFrames.framesPerSecond = 11f * Pace;
        aklatFrames.playOnEnable = false;
        // Frame 6 is place_frame_3: his hands are flat on the desk and the
        // book has left them. That is the frame the prop has to arrive on, so
        // it passes from him to the desk in a single step. One frame either
        // side and it either hangs in the air or vanishes and reappears -
        // which is rule 5, and the whole reason fireOnFrame exists.
        if (propAklat != null)
        {
            aklatFrames.fireOnFrame = 6;
            UnityEventTools.AddBoolPersistentListener(
                aklatFrames.onFrameReached, propAklat.SetActive, true);
        }

        // --- "Basa": he picks the book up and reads it, and KEEPS READING ---
        //
        //   lead-in   place_frame_3     his hands go to the desk
        //             aklat_frame_2     the book comes up into both hands
        //             basa_frame_1      his head and eyes come down to it
        //   loop      basa_frame_2/3/4  reading - ENDLESS
        //   tail      basa_frame_1      he looks up
        //             aklat_frame_2     squares the book
        //             place_frame_3     and lays it back on the desk
        //
        // LOOPCYCLES OF ZERO IS THE POINT OF THIS BEAT. Reading is one of the
        // things Docs/AnimationStandard rule 10 lists as repeating in life,
        // and a child does not stop reading after a tidy three cycles - they
        // stop when something else happens. Zero means "until Stop()", and
        // the thing that calls Stop is the next word being recognised. He
        // will read for as long as the child takes to say "Sulat", whether
        // that is four seconds or forty.
        //
        // Two things had to change elsewhere for that to work, and both are
        // worth knowing about before touching this:
        //
        //   PoseSequence.IsHoldingLoop tells the scene that this particular
        //   kind of "still playing" must not be waited for, or
        //   SceneObjectiveController would hold the next word back for its
        //   whole safety cap while waiting for a loop whose end IS that word.
        //
        //   PoseSequence.interrupts, set on "Sulat" below, is what ends it
        //   cleanly - Stop() rather than a cut, so the tail above plays and
        //   the book is back on the desk before the pencil comes out.
        PoseSequence basaFrames = seatGO.AddComponent<PoseSequence>();
        basaFrames.target = seatSr;
        // HE OPENS IT. "Aklat" leaves a SHUT book on the desk, so the first
        // thing reading needs is the cover coming back - he picks the shut
        // book up (aklat_frame_2, the same frame he carried it in), the
        // covers come apart (aklat_open_a), and it is open in both hands
        // (aklat_open_b) before his head goes down. Three frames, and they
        // are the difference between a boy reading a book and a boy holding
        // one that was open when he found it.
        basaFrames.frames = new[]
        {
            LoadSpriteOptional("place_frame_3_uniform"),
            LoadSpriteOptional("aklat_frame_2_uniform"),
            LoadSpriteOptional("aklat_open_a_uniform"),
            LoadSpriteOptional("aklat_open_b_uniform"),
            LoadSpriteOptional("basa_frame_1_uniform"),
        };
        basaFrames.loopFrames = new[]
        {
            LoadSpriteOptional("basa_frame_2_uniform"),
            LoadSpriteOptional("basa_frame_3_uniform"),
            LoadSpriteOptional("basa_frame_2_uniform"),
            LoadSpriteOptional("basa_frame_4_uniform"),
        };
        basaFrames.loopCycles = 0;                 // until Stop() - see above
        // And he shuts it again on the way out, in reverse, so the book that
        // goes back on the desk is the same shut book that came off it.
        basaFrames.tailFrames = new[]
        {
            LoadSpriteOptional("basa_frame_1_uniform"),
            LoadSpriteOptional("aklat_open_b_uniform"),
            LoadSpriteOptional("aklat_open_a_uniform"),
            LoadSpriteOptional("aklat_frame_2_uniform"),
            LoadSpriteOptional("place_frame_3_uniform"),
        };
        basaFrames.destinationSprite = LoadSpriteOptional("sitting_front_uniform");
        basaFrames.framesPerSecond = 11f * Pace;
        // Slow. Four frames of a head rocking two pixels is not a motion that
        // wants speed, and this one may be on screen for half a minute.
        basaFrames.loopFramesPerSecond = 6f * Pace;
        basaFrames.playOnEnable = false;

        // The book cannot be on the desk and in his hands at once. It leaves
        // the desk on frame 1, where it appears in them, and comes back when
        // he has put it down - which, because of the tail above, is also what
        // happens when "Sulat" interrupts the reading.
        if (propAklat != null)
        {
            basaFrames.fireOnFrame = 1;
            UnityEventTools.AddBoolPersistentListener(
                basaFrames.onFrameReached, propAklat.SetActive, false);
            UnityEventTools.AddBoolPersistentListener(
                basaFrames.onFinished, propAklat.SetActive, true);
        }

        // --- "Sulat": the writing action, in four parts ---
        //
        //   lead-in   place_frame_3  hands come onto the desk
        //             lapis_frame_1  he takes the pencil up, point down
        //   loop      sulat_hand_1..3, the point tracking across the page
        //   tail      lapis_frame_1  he looks up, pencil still in hand
        //             place_frame_3  and puts it back
        //   idle      sitting_front_uniform
        //
        // NOT sulat_frame_1..3, WHICH WERE DRAWN FOR THIS AND DO NOT WORK.
        // They have the one thing no other pose in the project has - the
        // pencil point on the bottom edge of the canvas, which is the
        // notebook's far edge - and that is why they were generated. But
        // there is NO HAND in them. Tools/fix_seated_hands.py cuts every
        // seated pose off below the sleeve and replaces the hands with a
        // forearm cross-section, so the arms read as running down behind the
        // desk, and these three inherited it. What is on screen is a pencil
        // hanging in the air beside his forearm with nothing holding it, and
        // at the size this scene draws him that is plainly what it looks
        // like. Dropping his head onto it did not help; nothing was going to.
        //
        // lapis_frame_1_uniform has what they do not: his fingers CLOSED
        // ROUND the pencil, drawn, point down and leaning right - a writing
        // grip, not the point-up "here is a pencil" grip of lapis_frame_2
        // and _3 - and already at the bottom of the canvas around x 320,
        // which is world 2.79, the middle of the desk in front of him. He
        // has been holding a pencil over this notebook in a drawing the
        // project has had since Scene 2.
        //
        // Tools/make_sulat_hand_frames.py adds the two things it was
        // missing: his head and eyes down on the page, and the hand
        // travelling fourteen pixels each way with the forearm easing into
        // the movement behind it.
        PoseSequence sulatFrames = seatGO.AddComponent<PoseSequence>();
        sulatFrames.target = seatSr;
        // END THE READING FIRST. Without this, Play() would take the renderer
        // off "Basa" wherever its loop happened to be - the book still up in
        // his hands - and start the pencil coming down over it, and the book
        // prop would then reappear on the desk from underneath. With it, the
        // reading plays its own tail: he looks up, squares the book, lays it
        // down, and only then does this begin. See PoseSequence.interrupts.
        sulatFrames.interrupts = basaFrames;
        // Two steps, not three. lapis_frame_2 is the pencil up at his chest -
        // it belonged here while the loop had the point on the desk edge and
        // something had to bridge the descent. The loop IS lapis_frame_1 now,
        // so going through chest height and back down again would be him
        // raising the pencil in order to lower it.
        sulatFrames.frames = new[]
        {
            LoadSpriteOptional("place_frame_3_uniform"),
            LoadSpriteOptional("lapis_frame_1_uniform"),
        };
        // FOUR ENTRIES FROM THREE DRAWINGS. The point travels 1-2-3 across
        // the page and 2 brings it back, so the stroke returns instead of
        // snapping to the start of the line. The frames carry a degree and a
        // half of head tilt each way with it, so his head follows his hand.
        sulatFrames.loopFrames = new[]
        {
            LoadSpriteOptional("sulat_hand_1_uniform"),
            LoadSpriteOptional("sulat_hand_2_uniform"),
            LoadSpriteOptional("sulat_hand_3_uniform"),
            LoadSpriteOptional("sulat_hand_2_uniform"),
        };
        // Four times round, which is about three seconds of writing. Three
        // cycles of three frames was a line and a half and read as a sample
        // of writing rather than as writing.
        sulatFrames.loopCycles = 4;
        // His head comes up off the page with the pencil still in his hand,
        // and then he puts it down. The same two frames as the lead-in, in
        // reverse.
        sulatFrames.tailFrames = new[]
        {
            LoadSpriteOptional("lapis_frame_1_uniform"),
            LoadSpriteOptional("place_frame_3_uniform"),
        };
        sulatFrames.destinationSprite = LoadSpriteOptional("sitting_front_uniform");
        sulatFrames.framesPerSecond = 11f * Pace;
        // A little quicker than the lead-in. The three frames differ only by
        // where the point is along one line, so the motion is small and needs
        // the rate to carry it; too slow and it reads as three still poses.
        sulatFrames.loopFramesPerSecond = 8f * Pace;
        // OFF, so nothing runs until the child says the word and the voice
        // recogniser accepts it. The seated Kylo is already on screen, so
        // playOnEnable here would fire the whole action the moment the scene
        // loaded.
        sulatFrames.playOnEnable = false;

        // --- The point of the pencil, on a layer that may touch the page ---
        //
        // Kylo draws at order 0 and the near desk is laid over him at 10,
        // which is what lets every seated pose be cut off flat at the desk's
        // far edge and still read as a boy behind a desk. The notebook is a
        // prop at 20, on the wood below that edge. So the lowest pixel of
        // him that is ever SEEN is the desk's own painted top edge, about
        // -2.94 here, and the page begins at -3.05: there is a tenth of a
        // unit of desk between his hand and the paper, and nothing drawn
        // into his sprite can cross it. That is why the writing frames had
        // him holding a pencil over a notebook he never touched.
        //
        // Lowering him is not available - the desk would stop hiding the cut
        // - and raising the notebook is not either, because it is in front
        // of him and would be drawn over his hand rather than under it.
        //
        // So the last two centimetres of the pencil get their own renderer,
        // on his exact transform but at order 22, in front of the desk AND
        // in front of the notebook. It picks the pencil up where the drawn
        // one goes behind the desk's edge and carries it down onto the page.
        // Nothing of HIM is duplicated: see Tools/make_sulat_point.py.
        //
        // It is driven by sulatFrames itself rather than by a second
        // PoseSequence, because two components would be two coroutines, and
        // one frame of drift at five frames a second is a pencil point a
        // fifth of a second behind the hand holding it.
        GameObject pointGO = new GameObject("Kylo_PencilPoint");
        SpriteRenderer pointSr = pointGO.AddComponent<SpriteRenderer>();
        pointSr.sortingLayerName = "Player";
        pointSr.sortingOrder = OrderDeskProps + 2;
        // HIS transform, not its own: the point sprites are drawn on a
        // canvas padded equally at both ends of the seated one, so sharing
        // his position and scale is what puts every pixel of them exactly
        // where the same pixel of his own sprite would have been.
        pointGO.transform.position = seatGO.transform.position;
        pointGO.transform.localScale = seatGO.transform.localScale;
        pointSr.sprite = null;                 // blank until the loop runs

        sulatFrames.companion = pointSr;
        // ONE SPRITE, four times. His arm does not move - see the note on
        // the loop frames above - so neither does the point. The entry is
        // repeated rather than the array shortened, because this list is
        // read frame for frame against loopFrames and a short one would
        // simply blank the pencil for the rest of the cycle.
        Sprite point = LoadSpriteOptional("sulat_point_uniform");
        sulatFrames.companionLoopFrames = new[] { point, point, point, point };

        // The pencil cannot be in two places. It leaves the desk on the frame
        // where it appears in his hand - frame 1, lapis_frame_1 - and comes
        // back when he has finished and his hands are down again.
        if (propPencil != null)
        {
            sulatFrames.fireOnFrame = 1;
            UnityEventTools.AddBoolPersistentListener(
                sulatFrames.onFrameReached, propPencil.SetActive, false);
            UnityEventTools.AddBoolPersistentListener(
                sulatFrames.onFinished, propPencil.SetActive, true);
        }

        // --- No "Tanong" here any more ---
        // The restructure moves it to Parke 3, which also teaches it. The
        // three tanong_frame sprites stay in the project for that scene; they
        // are drawn, fitted and committed, and nothing about them is specific
        // to this room - which is why "Sagot" below can use them. A hand
        // going up is a hand going up whichever word asked for it.

        // --- "Sagot": his hand goes up and he stands to answer ---
        //
        //   lead-in   tanong_frame_1..3   his hand rises off the desk, to
        //                                 head height, to straight up
        //             sagot_rise_1..3     he comes up out of the chair
        //             sagot_stand         on his feet, answering
        //   loop      sagot_answer_1/2    a lean towards the front of the
        //                                 class and a nod - he is answering
        //   tail      none                he stays standing
        //
        // HE DOES NOT SIT BACK DOWN, and that is deliberate: this is the last
        // word of the lesson, and the scene ends with the result panel over a
        // boy who has just answered his teacher.
        //
        // HE DOES NOT STAND WITH HIS HAND STILL UP, and that one is a limit
        // rather than a choice. Tools/make_sagot_frames.py has the whole of
        // it; the short version is that the raised arm cuts out of
        // tanong_frame_3 cleanly but talking_uniform's own left arm will not
        // come off to make room for it - the bare arm takes the shirt's
        // outline with it and the SLEEVE cannot come off at all, so he ended
        // up with a ragged side and an empty sleeve hanging off his
        // shoulder. A torn shirt is worse than a hand already down.
        //
        // So the hand-raise stays in the chair, where it is drawn, and
        // standing up is him getting to his feet to answer. The pose changes
        // once, on the first frame of the rise, which is the fastest moment
        // in the beat and where a change is least seen.
        //
        // THE STANDING FRAMES ARE A WHOLE DRAWING, not a construction, and
        // the version before this one was the other thing. It grafted
        // talking_uniform's hips onto the seated torso at the desk line and
        // eased the two widths together; in the game that gave him a white
        // band round his waist like a sash and arms that ended in mid-air
        // above it. The seated figure is 365 pixels across at that line
        // because his ARMS are in the number, and the shirt being stretched
        // to meet it was a bare 270.
        //
        // A SEATED DRAWING AND A STANDING ONE DO NOT JOIN. They disagree
        // about where his arms are, and no easing invents the arm that
        // should be in the gap. Worth remembering for Paaralan 1 and 4 and
        // the Parke scenes, which all still have a character who sits.
        //
        // So the body is talking_uniform entire, and nothing is added to it
        // - the same boy, the same tee and shorts, standing, front on, hands
        // up by his chest and mouth open, which is a child answering a
        // question. It is scaled until its head matches the seated poses'
        // head and placed so the desk crosses him at the top of his shorts.
        //
        // He rises 146 pixels, which is 0.89 world units. That number is not
        // anatomy, it is the frame: the narration plaque owns the top of the
        // shot from about +2.79, and this puts his crown at +2.62.
        //
        // PLAYED ONCE, not looped - rule 10 is explicit that putting a hand
        // up and standing up happen once. Only the answering in the middle
        // repeats, and only twice.
        PoseSequence sagotFrames = seatGO.AddComponent<PoseSequence>();
        sagotFrames.target = seatSr;
        sagotFrames.frames = new[]
        {
            LoadSpriteOptional("tanong_frame_1_uniform"),
            LoadSpriteOptional("tanong_frame_2_uniform"),
            LoadSpriteOptional("tanong_frame_3_uniform"),
            LoadSpriteOptional("sagot_rise_1_uniform"),
            LoadSpriteOptional("sagot_rise_2_uniform"),
            LoadSpriteOptional("sagot_rise_3_uniform"),
            LoadSpriteOptional("sagot_stand_uniform"),
        };
        sagotFrames.loopFrames = new[]
        {
            LoadSpriteOptional("sagot_answer_1_uniform"),
            LoadSpriteOptional("sagot_answer_2_uniform"),
            LoadSpriteOptional("sagot_answer_1_uniform"),
            LoadSpriteOptional("sagot_stand_uniform"),
        };
        sagotFrames.loopCycles = 2;
        sagotFrames.loopFramesPerSecond = 6f * Pace;      // a nod is slower than a reach
        // No tail. See above: he is left standing with his hand up, which is
        // where the loop already is, so there is no ending movement owed.
        sagotFrames.destinationSprite = LoadSpriteOptional("sagot_stand_uniform");
        // Slower than the other three. Getting out of a chair takes longer
        // than reaching across a desk, and nine frames at eleven would have
        // him up before the child had finished saying the word.
        sagotFrames.framesPerSecond = 9f * Pace;
        sagotFrames.playOnEnable = false;

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
        // Each gleam is centred on the DRAWN object, not on the middle of its
        // padded sprite box - see the constants above for why those differ.
        TimedOverlay gleamSulat = CreateGleamAt("Gleam_Sulat", NotebookX,
                                                 NotebookBaseY + NotebookHeight * 0.5f, 2.6f);
        // Two on the textbook: a small one as it arrives, a wider one when
        // he reads it.
        TimedOverlay gleamAklat = CreateGleamAt("Gleam_Aklat", AklatX, -3.44f, 1.6f);
        TimedOverlay gleamBasa = CreateGleamAt("Gleam_Basa", AklatX, -3.44f, 2.2f);
        // The only one that lands on Kylo. "Sagot" is him answering aloud,
        // and with no teacher in this view there is nothing else for it to
        // land on - see the note at the top. A drawn pose should replace it.
        TimedOverlay gleamSagot = CreateGleamAt("Gleam_Sagot", SitX,
                                                 SitFeetY + SeatedHeight * 0.72f, 2.8f);

        // The desk props sit above the desk layer, so their gleams must too.
        // EVERY gleam, not just the ones on the desk. CreateGleamAt leaves
        // them on the "Props" layer, which sorts BELOW "Player", so any left
        // out is drawn behind Kylo and behind the desk in front of him -
        // gleamSagot lands on Kylo himself and would have been invisible.
        foreach (TimedOverlay fx in new[] { gleamAklat, gleamBasa, gleamSulat, gleamSagot })
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
        // over the thing its word acts on.
        // The canvas is scaled 0.01, so these are world units x 100. The old
        // numbers pointed at a desk on the LEFT of the frame, which is where
        // menu_bg put it. Each badge now sits over the thing its word acts on,
        // lifted clear of the desk so the label is against the room rather
        // than on the wood. Nothing goes above y 265: the narration plaque
        // occupies the top of the frame from about world +2.79 up, and a badge
        // placed there disappears behind it. Nor does anything sit between
        // x 0.84 and 4.88, which is the width of his own sprite - a badge
        // there is a label pinned to his chest.
        GameObject pAklat = CreatePromptUI(uiRoot.transform, "Prompt_Aklat", "Aklat", new Vector3(610, -160, 0));
        GameObject pBasa = CreatePromptUI(uiRoot.transform, "Prompt_Basa", "Basa", new Vector3(610, -60, 0));
        GameObject pSulat = CreatePromptUI(uiRoot.transform, "Prompt_Sulat", "Sulat", new Vector3(-35, -190, 0));
        GameObject pSagot = CreatePromptUI(uiRoot.transform, "Prompt_Sagot", "Sagot", new Vector3(-35, 60, 0));

        // --- Objectives: take the book out, read, write, answer ---
        // In the order the lesson asks for them, which is also the order they
        // make sense in: the book has to be on the desk before he can read
        // from it, and he writes what he has read before he answers aloud.
        // EVERY WORD DRIVES ITS OWN POSE SEQUENCE AND NOTHING ELSE'S. That is
        // what keeps the four beats from overlapping: a word says Play on one
        // component, and PoseSequence.Play takes the renderer off any other
        // action running on it. The only crossing between two of them is the
        // one that is wanted - sulatFrames.interrupts, set above - and it
        // waits rather than cuts.
        //
        // The prop switching is NOT wired here. It belongs to the frame of
        // the animation where the object changes hands, which is why it is on
        // fireOnFrame / onFinished up with each sequence instead.
        var aklat = MakeObjective("Aklat", pAklat);
        UnityEventTools.AddPersistentListener(aklat.onCorrect, aklatFrames.Play);
        UnityEventTools.AddPersistentListener(aklat.onCorrect, gleamAklat.Play);

        var basa = MakeObjective("Basa", pBasa);
        UnityEventTools.AddPersistentListener(basa.onCorrect, basaFrames.Play);
        UnityEventTools.AddPersistentListener(basa.onCorrect, gleamBasa.Play);

        var sulat = MakeObjective("Sulat", pSulat);
        UnityEventTools.AddPersistentListener(sulat.onCorrect, sulatFrames.Play);
        UnityEventTools.AddPersistentListener(sulat.onCorrect, gleamSulat.Play);

        var sagot = MakeObjective("Sagot", pSagot);
        UnityEventTools.AddPersistentListener(sagot.onCorrect, sagotFrames.Play);
        UnityEventTools.AddPersistentListener(sagot.onCorrect, gleamSagot.Play);

        SceneObjectiveController controller = uiRoot.AddComponent<SceneObjectiveController>();
        controller.voiceCommand = voiceCommand;
        controller.playerMovement = player;
        controller.objectives = new[] { aklat, basa, sulat, sagot };
        // The default four seconds is not enough for this scene any more.
        //
        // It is a safety cap on how long the sequence will wait for an action
        // to finish before asking the next word anyway, and it was set when
        // the longest thing in any scene was a walk. "Sulat" here is the end
        // of the reading (about half a second), then the pencil coming down,
        // then four cycles of writing, then the pencil going back - about
        // four and a half seconds all told, and "Sagot" is not far behind it.
        // Left at four, the next word went up while he was still writing.
        //
        // The endless reading loop does NOT eat this budget: PoseSequence
        // reports it through IsHoldingLoop and SceneObjectiveController steps
        // over it, so "Sulat" is asked for while he reads rather than four
        // seconds later.
        controller.maxActionWait = 7f;

        // OFF THE DESK, in this scene only.
        //
        // The mic and the replay button live bottom-centre in every scene,
        // which is where a thumb is. In this one the desk fills the bottom
        // of the frame and the notebook lies across the middle of it: the
        // replay button sits at canvas x 140, and the notebook spans x 91 to
        // 481, so the button came down squarely on the page he is supposed
        // to be writing in.
        //
        // Moved left instead of up. Everything the child has to look at is
        // to the RIGHT of centre - Kylo is at world 2.86 and covers canvas
        // 84..488, the notebook 91..481, the textbook further right still -
        // so the left of the frame is empty lockers and the far end of the
        // desk, and nothing is lost by putting the buttons there. They stay
        // at the same height, which is what makes them reachable.
        VoiceUIBuilder.BuildVoiceUI(voiceCommand, controller,
                                     new Vector2(-620, -400), new Vector2(-370, -400));
        // Still needed in the scene: SceneResultPanel looks the fader up at
        // run time to fade out of whichever button is pressed.
        SceneFaderBuilder.Build();

        // --- A middle scene of Level 2 now: Scene 4 ends it ---
        // The counter is added by hand here, because DatabaseDemoBuilder used
        // to bring one along with the result panel and that panel has moved on.
        // Without it the five words said in this lesson leave the level total.
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

        Debug.Log("[SALINLAHI] Paaralan Scene 3 built and saved to " + ScenePath +
                   ". Words: Aklat, Basa, Sulat, Sagot - all four drawn. Aklat takes the " +
                   "textbook out of his bag and lays it on the desk; Basa picks it up and " +
                   "reads until Sulat is said, however long that takes; Sulat finishes the " +
                   "reading, puts the book down and writes with his head on the page; Sagot " +
                   "puts his hand up and stands. He is left standing - it is the last word. " +
                   "Staged in Scene 2's locker corner: Scene 2's seat, Scene 2's tee and no " +
                   "teacher - see the notes at the top of BuildPaaralanScene3 for why. " +
                   "It fades into " + NextScene + ", which ends the level - so build Paaralan " +
                   "Scene 4 as well, or the level runs into a scene that is not there.");
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
