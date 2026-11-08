// Assets/Editor/BuildPaaralanScene3.cs
//
// One-click scene builder for Paaralan Scene 3 ("Vocabulary Lesson").
// Teaches four words: Sulat, Basa, Tanong, Inom.
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
// WHAT EACH WORD DOES. Four of the five are things rather than actions, and
// the only pose drawn so far is the seated one, so the feedback is carried by
// what lights up rather than by what Kylo does:
//
//   Aklat    the textbook appears on the desk
//   Basa     it lights up - he is reading it
//   Sulat    the open notebook lights up - he is writing in it
//   Basa     the textbook lights up - he is reading it
//   Tanong   Kylo's upper body lights up: he is putting his hand up
//   Inom     the glass lights up - he is taking a drink
//
// NOTHING APPEARS ANY MORE. With "Aklat" gone there is no word whose job is
// to put an object on the desk, so the desk is laid out from the start -
// notebook, pencil, textbook and glass - and all four words gleam something
// already there. That is the right staging for a list of actions, and it is
// also why this scene is not finished: a gleam can say WHICH thing a word is
// about, but it cannot show reading, writing, asking or drinking. All four
// want drawn poses, the way Scene 2's beats are carried by drawn frames.
// Until those exist this is the honest staging, not the final one.
//
// Sprites expected in Assets/Sprites/:
//   klasrum_interior_background (or menu_bg), sitting_front_uniform,
//   prop_aklat, prop_notebook_open, prop_lapis, item_baso, fx_gleam,
//   ui_arrow, ui_word_badge, badge_paaralan
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
    // height; prop_lapis is 440x150 with ink 424x74 - 49%; item_baso is 54x61
    // with ink 48x55 - 90%. The numbers below are chosen so that the DRAWN
    // object lands where it should, and the drawn spans are written out with
    // each one so the next person does not have to re-derive them.
    //
    // Not cropped to their ink the way prop_notebook_open was, because all
    // three are shared: prop_aklat is also in Paaralan 4 and 5, prop_lapis in
    // Paaralan 2 and 5, item_baso in four Bahay and Parke scenes. Cropping
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

    // The pencil, lying ON the open notebook's right-hand page rather than
    // beside it. It used to be on the left strip; the glass needs that strip,
    // and a pencil resting on the book he is writing in is where a pencil
    // actually is. Drawn span: x 3.18..4.02, y -3.31..-3.16, inside the
    // notebook's own -3.84..-3.01. Drawn one sorting order above the notebook
    // so it is not a coin-flip which of the two wins.
    private const float PencilX = 3.60f;
    private const float PencilBaseY = -3.35f;
    private const float PencilHeight = 0.30f;

    // The glass, for "Inom". On the LEFT strip and STANDING, which is the one
    // thing on this desk that is allowed to break the -3.01 line: that line
    // matters because his sprite starts there, and at x -0.05 the glass is
    // clear of him (he spans 0.84..4.88), so its top rises against the
    // lockers instead of across his shirt. Drawn span: x -0.36..0.26,
    // y -3.50..-2.78; the desk's left edge on that bottom row is -0.84, so it
    // stands a third of a unit inside the wood rather than on the lip.
    //
    // Sized DOWN from a first pass at 1.05, which drew it 1.03 units tall -
    // taller than the open notebook is deep, which on a school desk reads as
    // a jug rather than a mug.
    private const float BasoX = -0.05f;
    private const float BasoBaseY = -3.54f;
    private const float BasoHeight = 0.80f;

    private const int OrderKylo = 0;
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
            "prop_lapis", "item_baso", "fx_gleam",
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
        // Everything, from the start. He took the notebook and pencil out of
        // his bag in Scene 2, and with "Aklat" no longer in the word list
        // there is nothing left whose job is to make an object appear. So the
        // desk is simply laid out for a lesson and the four words light up
        // what they are about. CreatePropOptional builds them switched OFF,
        // for the old appear-on-the-word behaviour, so each is switched on.
        GameObject propAklat = CreatePropOptional("Prop_Aklat", "prop_aklat",
                                                   AklatX, AklatHeight, AklatBaseY);
        // prop_notebook_open, not prop_kuwaderno: the shut blue hardback is
        // the wrong object for a lesson being written in, and the open book is
        // what Scene 2 leaves lying on this desk.
        GameObject propNotebook = CreatePropOptional("Prop_Kuwaderno", "prop_notebook_open",
                                                      NotebookX, NotebookHeight, NotebookBaseY);
        GameObject propPencil = CreatePropOptional("Prop_Lapis", "prop_lapis",
                                                    PencilX, PencilHeight, PencilBaseY);
        GameObject propBaso = CreatePropOptional("Prop_Baso", "item_baso",
                                                  BasoX, BasoHeight, BasoBaseY);
        foreach (GameObject prop in new[] { propAklat, propNotebook, propPencil, propBaso })
            if (prop != null) prop.SetActive(true);
        // The pencil lies on the notebook, so it has to beat the notebook.
        if (propPencil != null)
            propPencil.GetComponent<SpriteRenderer>().sortingOrder = OrderDeskProps + 1;

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
        TimedOverlay gleamBasa = CreateGleamAt("Gleam_Basa", AklatX, -3.44f, 2.0f);
        // The only one that lands on Kylo: wide, over the upper body, for a
        // hand going up. A drawn pose should replace it.
        TimedOverlay gleamTanong = CreateGleamAt("Gleam_Tanong", SitX,
                                                  SitFeetY + SeatedHeight * 0.64f, 3.2f);
        TimedOverlay gleamInom = CreateGleamAt("Gleam_Inom", BasoX, -3.14f, 1.3f);

        // The desk props sit above the desk layer, so their gleams must too.
        foreach (TimedOverlay fx in new[] { gleamSulat, gleamBasa, gleamInom })
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
        GameObject pSulat = CreatePromptUI(uiRoot.transform, "Prompt_Sulat", "Sulat", new Vector3(-35, -190, 0));
        GameObject pBasa = CreatePromptUI(uiRoot.transform, "Prompt_Basa", "Basa", new Vector3(610, -60, 0));
        GameObject pTanong = CreatePromptUI(uiRoot.transform, "Prompt_Tanong", "Tanong", new Vector3(290, 230, 0));
        GameObject pInom = CreatePromptUI(uiRoot.transform, "Prompt_Inom", "Inom", new Vector3(-35, 60, 0));

        // --- Objectives: write, read, ask, drink ---
        // In the order the lesson asks for them. Nothing switches a prop on
        // any more: the desk is laid out from the start, so each word gleams
        // what it is about and the sequence is purely what Kylo does.
        var sulat = MakeObjective("Sulat", pSulat);
        UnityEventTools.AddPersistentListener(sulat.onCorrect, gleamSulat.Play);

        var basa = MakeObjective("Basa", pBasa);
        UnityEventTools.AddPersistentListener(basa.onCorrect, gleamBasa.Play);

        var tanong = MakeObjective("Tanong", pTanong);
        UnityEventTools.AddPersistentListener(tanong.onCorrect, gleamTanong.Play);

        var inom = MakeObjective("Inom", pInom);
        UnityEventTools.AddPersistentListener(inom.onCorrect, gleamInom.Play);

        SceneObjectiveController controller = uiRoot.AddComponent<SceneObjectiveController>();
        controller.voiceCommand = voiceCommand;
        controller.playerMovement = player;
        controller.objectives = new[] { sulat, basa, tanong, inom };

        VoiceUIBuilder.BuildVoiceUI(voiceCommand, controller);
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
                   ". Words: Sulat, Basa, Tanong, Inom - four actions. Three are new; INOM IS " +
                   "NOT, it is also taught in Bahay Scene 5 and Parke Scene 4, so do not count " +
                   "it twice in the level total. " +
                   "Staged in Scene 2's locker corner: Scene 2's seat, Scene 2's tee, no " +
                   "foreground desk layer and no teacher - see the notes at the top of " +
                   "BuildPaaralanScene3 for why each of those went. The desk is laid out from " +
                   "the start and every word gleams something already on it; all four want " +
                   "drawn poses before this scene is finished. " +
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
