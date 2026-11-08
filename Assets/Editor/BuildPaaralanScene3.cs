// Assets/Editor/BuildPaaralanScene3.cs
//
// One-click scene builder for Paaralan Scene 3 ("Vocabulary Lesson").
// Teaches five words: Aklat, Basa, Sulat, Tanong, Sagot.
//
// All five are NEW - no other scene in the game uses any of them, which makes
// this the largest single vocabulary gain left in the design document. It is
// also the scene that document calls the core learning scene: the one where
// the child is not fetching objects but taking part in a lesson.
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
//   Tanong   Kylo's upper body lights up: he is putting his hand up
//   Sagot    his head lights up: he is the one answering aloud
//
// Tanong and Sagot are both Kylo's in the design document, and with the
// teacher gone they both land on him. Two different spots at two different
// sizes keep them from reading as the same event, but only just: what should
// really tell them apart is DRAWN poses - a hand raised, a mouth open - the
// way Scene 2's beats are carried by drawn frames rather than by gleams.
// Until those exist this is the honest staging, not the finished one.
//
// Sprites expected in Assets/Sprites/:
//   klasrum_interior_background (or menu_bg), sitting_front_uniform,
//   prop_aklat, prop_notebook_open, prop_lapis, fx_gleam,
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

    // The textbook, which "Aklat" puts on the desk. To the RIGHT of the
    // notebook rather than in front of him: at 0.83 units tall the notebook is
    // 3.91 wide and spans x 0.91..4.81, which is the whole of the desk in
    // front of him, and the left-hand strip is both narrower and bitten into
    // by the desk's far edge. Centred at 5.75 the book spans 5.15..6.36, and
    // its top at -3.12 clears the far edge, which by that x has dropped to
    // about -3.07.
    private const float AklatX = 5.75f;
    private const float AklatBaseY = -3.82f;
    private const float AklatHeight = 0.70f;

    // The pencil, already out of the bag too. On the LEFT strip, which has
    // room for something this small and nothing else to hide it: at x 0.25 it
    // spans -0.13..0.63, clear of both the notebook and of him. Parked at
    // -3.40 rather than at the desk's near edge so it reads as lying on the
    // wood rather than balanced on the rim.
    private const float PencilX = 0.25f;
    private const float PencilBaseY = -3.40f;
    private const float PencilHeight = 0.26f;

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
            "prop_lapis", "fx_gleam",
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
        // lockers. "Sagot" lands on Kylo instead, which is also whose line it
        // is in the design document.

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
        // The notebook and pencil are already out - he took them from his bag
        // in Scene 2 - so they are switched on below. The textbook is not, and
        // "Aklat" is what puts it there.
        GameObject propAklat = CreatePropOptional("Prop_Aklat", "prop_aklat",
                                                   AklatX, AklatHeight, AklatBaseY);
        // prop_notebook_open, not prop_kuwaderno: the shut blue hardback is
        // the wrong object for a lesson being written in, and the open book is
        // what Scene 2 leaves lying on this desk.
        GameObject propNotebook = CreatePropOptional("Prop_Kuwaderno", "prop_notebook_open",
                                                      NotebookX, NotebookHeight, NotebookBaseY);
        GameObject propPencil = CreatePropOptional("Prop_Lapis", "prop_lapis",
                                                    PencilX, PencilHeight, PencilBaseY);
        if (propNotebook != null) propNotebook.SetActive(true);
        if (propPencil != null) propPencil.SetActive(true);

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
        TimedOverlay gleamAklat = CreateGleamAt("Gleam_Aklat", AklatX,
                                                 AklatBaseY + AklatHeight * 0.5f, 1.5f);
        TimedOverlay gleamBasa = CreateGleamAt("Gleam_Basa", AklatX,
                                                AklatBaseY + AklatHeight * 0.5f, 1.9f);
        // On the notebook itself, which is what he writes in. This used to be
        // the midpoint of the notebook and the pencil; with the pencil moved
        // to the far side of him, that midpoint is now his own stomach.
        TimedOverlay gleamSulat = CreateGleamAt("Gleam_Sulat", NotebookX,
                                                 NotebookBaseY + NotebookHeight * 0.5f, 2.6f);
        // Both of the last two land on Kylo now. Sized and placed apart on
        // purpose: a wide one over the upper body for putting a hand up, a
        // tight one on the head for speaking. See the note at the top - drawn
        // poses are what should carry these beats, and these are the stand-in.
        TimedOverlay gleamTanong = CreateGleamAt("Gleam_Tanong", SitX,
                                                  SitFeetY + SeatedHeight * 0.64f, 3.2f);
        TimedOverlay gleamSagot = CreateGleamAt("Gleam_Sagot", SitX,
                                                 SitFeetY + SeatedHeight * 0.78f, 2.0f);

        // The desk props sit above the desk layer, so their gleams must too.
        foreach (TimedOverlay fx in new[] { gleamAklat, gleamBasa, gleamSulat })
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
        GameObject pAklat = CreatePromptUI(uiRoot.transform, "Prompt_Aklat", "Aklat", new Vector3(590, -160, 0));
        GameObject pBasa = CreatePromptUI(uiRoot.transform, "Prompt_Basa", "Basa", new Vector3(590, -40, 0));
        GameObject pSulat = CreatePromptUI(uiRoot.transform, "Prompt_Sulat", "Sulat", new Vector3(-35, -190, 0));
        GameObject pTanong = CreatePromptUI(uiRoot.transform, "Prompt_Tanong", "Tanong", new Vector3(290, 230, 0));
        GameObject pSagot = CreatePromptUI(uiRoot.transform, "Prompt_Sagot", "Sagot", new Vector3(-35, 60, 0));

        // --- Objectives: take the book out, read, write, ask, answer ---
        var aklat = MakeObjective("Aklat", pAklat);
        if (propAklat != null)
            UnityEventTools.AddBoolPersistentListener(aklat.onCorrect, propAklat.SetActive, true);
        UnityEventTools.AddPersistentListener(aklat.onCorrect, gleamAklat.Play);

        var basa = MakeObjective("Basa", pBasa);
        UnityEventTools.AddPersistentListener(basa.onCorrect, gleamBasa.Play);

        var sulat = MakeObjective("Sulat", pSulat);
        UnityEventTools.AddPersistentListener(sulat.onCorrect, gleamSulat.Play);

        var tanong = MakeObjective("Tanong", pTanong);
        UnityEventTools.AddPersistentListener(tanong.onCorrect, gleamTanong.Play);

        var sagot = MakeObjective("Sagot", pSagot);
        UnityEventTools.AddPersistentListener(sagot.onCorrect, gleamSagot.Play);

        SceneObjectiveController controller = uiRoot.AddComponent<SceneObjectiveController>();
        controller.voiceCommand = voiceCommand;
        controller.playerMovement = player;
        controller.objectives = new[] { aklat, basa, sulat, tanong, sagot };

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
                   ". Words: Aklat, Basa, Sulat, Tanong, Sagot - all five new to the game. " +
                   "Staged in Scene 2's locker corner now: Scene 2's seat, Scene 2's tee, no " +
                   "foreground desk layer and no teacher - see the notes at the top of " +
                   "BuildPaaralanScene3 for why each of those went. Tanong and Sagot both " +
                   "gleam Kylo until drawn poses exist for them. " +
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
