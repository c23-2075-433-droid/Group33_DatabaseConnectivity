// Assets/Editor/BuildPaaralanScene1.cs
//
// One-click scene builder for Paaralan Scene 1 ("Arriving at School").
// Teaches five words: Lakad, Bukas, Bati, Pasok, Takbo.
//
// This is Level 2's FIRST scene: it starts the run
// (RunScoreCounter.resetOnStart) and fades into Scene 2, which ends it. The
// database demo, the Paaralan badge and unlocking Parke now live at the end of
// Scene 2 - see BuildPaaralanScene2 - exactly as Bahay's scenes do. Until
// Scene 2 existed this scene did both jobs itself.
//
// Kylo arrives already in his school uniform with his bag, carried over from
// the end of Bahay. The five words are the arrival in order: walk up to the
// shut gate, have it opened, greet the guard who opened it, go in, hurry to
// class. Each word is the reason the next one can happen, which is the point
// of the sequence - the child is not reciting a list, they are getting in.
//
// ART THAT IS STILL MISSING, and what happens without it. Both are optional
// by design so the scene runs today:
//   paaralan_gate_background_closed - the same gate, shut, same camera. Until
//     it exists the scene opens on an open gate and "Bukas" changes nothing.
//   char_guard_idle - the guard with his hand down, same canvas size as
//     char_guard. Until it exists he is waving before he is greeted.
// The builder's log line at the bottom says which of the two is absent.
//
// The background is scaled from its own bounds rather than by a fixed number,
// so it fills the camera whatever resolution and pixels-per-unit the art
// imports at. Bahay's scenes each hardcode a scale and two of them had to be
// corrected when the art turned out not to be 100 PPU.
//
// Sprites expected in Assets/Sprites/:
//   paaralan_gate_background, char_guard, fx_gleam, standing_uniform_with_bag,
//   walk_frame_1..4_uniform_with_bag, talking_uniform_with_bag,
//   ui_arrow, ui_word_badge, badge_paaralan
//   paaralan_gate_background_closed, char_guard_idle   (optional, see above)
//   back_walk_1/2/3_uniform_with_bag, back_run_1/2_uniform_with_bag
//     the back-view cycles for walking and running into the school; all
//     optional, and the log line says what is missing and how it degrades
//
// Run via: Tools > SALINLAHI > Build Paaralan Scene 1

using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class BuildPaaralanScene1
{
    private const string SpriteFolder = "Assets/Sprites/";
    private const string ScenePath = "Assets/Scenes/Chapter2_Level1_Paaralan.unity";
    private const string NextScene = "Chapter2_Level2_Klasrum";

    private const float ViewHeight = 10.8f;           // camera is orthographic 5.4
    private const float ViewWidth = 19.2f;
    private const float FloorY = -4.45f;              // the near pavement, outside the gate

    // The middle of the gate opening in paaralan_gate_background, measured off
    // the art against the two pillars. "Takbo" ends here.
    //
    // The school steps are no longer the target. This art looks THROUGH the
    // gate at a building set well back, so the steps sit near the vanishing
    // point: walking to them along a flat FloorY would carry Kylo past the
    // gate at full size, which reads as sliding rather than approaching.
    private const float SchoolEntranceX = 0.00f;

    // The character art imports at 120 pixels per unit, and its feet sit this
    // far below the pivot - see BuildBahayScene2 for where these come from.
    private const float SpritePPU = 120f;
    // Smaller than Bahay's 0.83, because this is a wide establishing shot and
    // at 0.83 Kylo is taller than the gate itself. But not as small as the
    // 0.45 it started at: once "Pasok" became a walk INTO the picture he had
    // to survive being shrunk twice and still read as a boy rather than a
    // speck, and 0.45 left him at 1.7 units by the time he was through the
    // gate. 0.56 puts him at 3.5 units here and 2.9 inside, against a gate
    // post of about 4.9.
    private const float PlayerScale = 0.56f;
    private const float FeetBelowPivot = (743.5f - 745f / 2f) / SpritePPU;
    private const float PlayerFeetY = FloorY + FeetBelowPivot * PlayerScale;

    [MenuItem("Tools/SALINLAHI/Build Paaralan Scene 1")]
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

        // --- The school gate, scaled from its own bounds to fill the view ---
        // Its own sprite rather than paaralan_background: that one is the yard
        // seen from inside, and Scene 4 (Recess) still plays in it.
        // Two pictures of the same gate from the same camera: shut, and open.
        // "Bukas" swaps one for the other. The gate is painted INTO the art
        // rather than being a panel that can swing, so opening it is a change
        // of picture, not an animation. Until the shut version is drawn the
        // scene starts open and "Bukas" simply has nothing to change.
        Sprite gateShut = LoadSpriteOptional("paaralan_gate_background_closed");
        Sprite gateOpen = LoadSprite("paaralan_gate_background");

        GameObject bg = new GameObject("Background_School");
        SpriteRenderer bgSr = bg.AddComponent<SpriteRenderer>();
        bgSr.sprite = gateShut != null ? gateShut : gateOpen;
        bgSr.sortingLayerName = "Background";
        bgSr.sortingOrder = 0;
        if (bgSr.sprite != null && bgSr.sprite.bounds.size.y > 0.0001f)
        {
            // Cover the view: whichever axis is short decides the scale, so
            // there is never a black band down a side.
            float s = Mathf.Max(ViewHeight / bgSr.sprite.bounds.size.y,
                                ViewWidth / bgSr.sprite.bounds.size.x);
            bg.transform.localScale = new Vector3(s, s, 1f);
        }

        SpriteSwap gateSwap = bg.AddComponent<SpriteSwap>();
        gateSwap.target = bgSr;
        gateSwap.beforeSprite = gateShut;
        gateSwap.afterSprite = gateOpen;

        // --- The guard on the gate ---
        // Bati needs someone to greet. Without him the child is told to
        // say hello to an empty yard, which teaches the word in a vacuum.
        // Sized against Kylo: an adult at 3.6 units to his 3.5. A grown man is
        // not much taller than a child in this art style - the difference is
        // build and uniform, not height - and anything more beside the near
        // pillar made him taller than the gate itself, which is where 3.9
        // against the old background went wrong.
        GameObject guard = CreateStanding("Char_Guard", "char_guard", 3.10f, 3.60f);

        // Turned to face Kylo. char_guard is drawn facing RIGHT - the cap brim
        // and the waving hand both point that way - and he stands to the RIGHT
        // of the gate, so unflipped he waves at the wall. Kylo needs no flip:
        // standing_uniform_with_bag is a right-facing profile and he arrives
        // from the left, so the two already look at each other once the guard
        // is turned. flipX rather than a negative scale, which CreateStanding
        // uses to size him.
        SpriteRenderer guardSr = guard.GetComponent<SpriteRenderer>();
        guardSr.flipX = true;

        // char_guard is the WAVING pose, and it is the only one that exists.
        // The greeting needs him standing still first, or he is already waving
        // before the child has said anything and "Bati" changes nothing.
        // char_guard_idle is that still pose; until it is drawn he starts the
        // scene waving and the swap is a no-op. Draw it on the same canvas
        // size as char_guard: CreateStanding sizes him from the sprite bounds,
        // so a different canvas would change his height.
        Sprite guardIdle = LoadSpriteOptional("char_guard_idle");
        Sprite guardWave = LoadSprite("char_guard");
        if (guardIdle != null) guardSr.sprite = guardIdle;

        SpriteSwap guardSwap = guard.AddComponent<SpriteSwap>();
        guardSwap.target = guardSr;
        guardSwap.beforeSprite = guardIdle;
        guardSwap.afterSprite = guardWave;

        // --- Player, arriving at the gate on the left ---
        // Not further left than this: the flowering bushes along the left wall
        // are painted nearer the camera than the pavement, so a character out
        // there reads as standing in the planting rather than on the path.
        GameObject playerGO = new GameObject("player_character");
        playerGO.tag = "Player";
        playerGO.transform.position = new Vector3(-5.80f, PlayerFeetY, 0f);
        playerGO.transform.localScale = new Vector3(PlayerScale, PlayerScale, 1f);

        SpriteRenderer playerSr = playerGO.AddComponent<SpriteRenderer>();
        playerSr.sortingLayerName = "Player";
        // Already dressed: Bahay ended with him in uniform, carrying his bag.
        playerSr.sprite = LoadSprite("standing_uniform_with_bag");

        Rigidbody2D playerRb = playerGO.AddComponent<Rigidbody2D>();
        playerRb.gravityScale = 3f;
        playerRb.freezeRotation = true;

        BoxCollider2D playerCol = playerGO.AddComponent<BoxCollider2D>();
        playerCol.size = new Vector2(1.2f, FeetBelowPivot * 2f);

        PlayerMovement player = playerGO.AddComponent<PlayerMovement>();
        player.standingSprite = LoadSprite("standing_uniform_with_bag");
        player.walkFrames = UniformWalkFrames();
        player.currentWakeStage = PlayerMovement.WakeStage.Standing;

        // Three words move him now, not two, so each gets a shorter share of
        // the same ground: "Lakad" up to the shut gate, "Pasok" through it,
        // "Takbo" the run that remains. One duration covers both timed walks -
        // PlayerMovement has a single voiceWalkDuration - so they are equal
        // halves by construction, and Takbo is given the rest.
        player.voiceWalkDuration = 0.40f;                // each walk: about 1.9 units

        // "Takbo" then runs the rest, stopping at the foot of the steps rather
        // than after a fixed time. Marker rather than a number so moving the
        // building in the art means moving this, not re-deriving a duration.
        GameObject entrance = new GameObject("SchoolEntrance");
        entrance.transform.position = new Vector3(SchoolEntranceX, FloorY, 0f);
        player.walkTarget = entrance.transform;
        player.walkTargetThreshold = 0.35f;

        VoiceCommand voiceCommand = playerGO.AddComponent<VoiceCommand>();
        voiceCommand.player = player;

        // "Bati" turns him to face the child and wave. A greeting has
        // nothing to pick up and nothing to light up, so he is the feedback.
        GameObject greetGO = new GameObject("Pose_Greet");
        PoseSwitch greet = greetGO.AddComponent<PoseSwitch>();
        greet.player = player;
        greet.standingSprite = LoadSprite("talking_uniform_with_bag");

        // The guard lights up when he is greeted, so the child can see the
        // hello land on someone rather than just watching Kylo wave.
        TimedOverlay guardGleam = CreateGleam("Gleam_Guard", guard, 4.00f);

        // "Bukas" lights the gateway itself. A marker carries the gleam rather
        // than the background object, whose origin is the middle of the screen
        // and well above the opening once the pavement is accounted for.
        GameObject gateSpot = new GameObject("GateSpot");
        gateSpot.transform.position = new Vector3(0f, FloorY + 2.20f, 0f);
        TimedOverlay gateGleam = CreateGleam("Gleam_Bukas", gateSpot, 4.60f);

        // "Pasok" turns him side-on again before he walks in.
        GameObject walkPoseGO = new GameObject("Pose_Walk");
        PoseSwitch facingWalk = walkPoseGO.AddComponent<PoseSwitch>();
        facingWalk.player = player;
        facingWalk.standingSprite = LoadSprite("standing_uniform_with_bag");
        facingWalk.walkFrames = UniformWalkFrames();

        // --- Going IN, which on this painting is away from the camera ---
        //
        // "Pasok" and "Takbo" are not sideways walks. The gate is drawn in one
        // -point perspective, so entering it means going up the frame and
        // getting smaller - the rule every other object in the art is drawn
        // by. PlayerMovement cannot do that: it is a Rigidbody2D on a ground
        // collider and only moves along X. WalkIntoScene takes him over.
        //
        // Both destinations are measured off the art as where his FEET land,
        // then converted to the pivot, which is what a transform position is.
        // How far he goes is limited by how small he is allowed to get. The
        // building is drawn a long way back, so standing him at its doors
        // would shrink him to about a unit tall - correct perspective, and
        // far too small to watch. He stops partway up the courtyard instead,
        // heading for the building rather than arriving at it, which the
        // result panel covers a moment later anyway.
        // The gate line - where the outside pavement meets the courtyard - is
        // at y -3.70, measured off the art rather than judged by eye. "Pasok"
        // has to finish BEYOND it or he stops short of the gate he was just
        // told to walk through, which is what -4.10 did.
        const float InsideGateFeetY = -3.40f;   // past the gate line, in the courtyard
        const float SchoolDoorFeetY = -2.50f;   // further up the path, still legible
        const float InsideGateShrink = 0.76f;   // of the size he arrives at the gate
        const float SchoolDoorShrink = 0.62f;   // of the size he is once inside

        float scaleInside = PlayerScale * InsideGateShrink;
        float scaleAtDoor = scaleInside * SchoolDoorShrink;

        // Drawn from behind for both moves. The profile art is a side-on walk
        // cycle, and a side-on stride heading up the frame reads as sliding -
        // the legs say one direction and the movement says another.
        //
        // Two cycles, and they are different GAITS rather than the same steps
        // at two speeds. Each is built from half as many drawings as it has
        // frames: seen from directly behind, a mirror swaps which leg leads,
        // so one stride covers both halves of the stride.
        //
        //   walk  together, left, together, right   (2 drawings)
        //   run   stride, opposite stride           (1 drawing)
        //
        // All optional. With the legs-together frame missing the walk is just
        // the two strides, which still animates; with nothing at all he walks
        // in side-on as he used to.
        Sprite backPass  = LoadSpriteOptional("back_walk_2_uniform_with_bag");
        Sprite backLeft  = LoadSpriteOptional("back_walk_1_uniform_with_bag");
        Sprite backRight = LoadSpriteOptional("back_walk_3_uniform_with_bag");
        Sprite backRunA  = LoadSpriteOptional("back_run_1_uniform_with_bag");
        Sprite backRunB  = LoadSpriteOptional("back_run_2_uniform_with_bag");

        Sprite[] backWalk = null;
        if (backLeft != null && backRight != null)
            backWalk = backPass != null
                ? new[] { backPass, backLeft, backPass, backRight }
                : new[] { backLeft, backRight };

        Sprite[] backRun = (backRunA != null && backRunB != null)
            ? new[] { backRunA, backRunB }
            : backWalk;   // no run art yet: the walk, taken faster

        GameObject enterGO = new GameObject("Walk_Pasok");
        WalkIntoScene enter = enterGO.AddComponent<WalkIntoScene>();
        enter.player = player;
        enter.destination = new Vector2(SchoolEntranceX,
                                        InsideGateFeetY + FeetBelowPivot * scaleInside);
        enter.endScaleMultiplier = InsideGateShrink;
        enter.duration = 1.6f;
        // Derived rather than fixed, so the cadence survives the cycle growing
        // from two frames to four when the legs-together pose is drawn. What
        // should stay constant is how long a full cycle takes - half a second,
        // which is an unhurried walk - not how many frames go by per second.
        enter.framesPerSecond = backWalk != null ? backWalk.Length * 2f : 8f;
        enter.facingAwayFrames = backWalk;

        GameObject runGO = new GameObject("Walk_Takbo");
        WalkIntoScene runToDoor = runGO.AddComponent<WalkIntoScene>();
        runToDoor.player = player;
        runToDoor.destination = new Vector2(SchoolEntranceX,
                                            SchoolDoorFeetY + FeetBelowPivot * scaleAtDoor);
        runToDoor.endScaleMultiplier = SchoolDoorShrink;
        runToDoor.duration = 1.0f;          // a run: the same path, covered quicker
        // Six, not fourteen. Each frame here is one STEP, so the rate is a
        // cadence: two frames at 14 is seven strides a second, which is a
        // vibration rather than a run. Six gives three strides a second, about
        // what a child sprinting actually does. The number only looks low
        // next to the profile walk's 8 because that cycle has four frames to
        // get through for every two of these.
        runToDoor.framesPerSecond = 6f;
        runToDoor.facingAwayFrames = backRun;

        CameraFollow follow = camGO.AddComponent<CameraFollow>();
        follow.target = playerGO.transform;
        follow.offset = new Vector3(0f, 0f, -10f);
        follow.clampHorizontally = true;
        follow.minX = 0f;
        follow.maxX = 0f;
        follow.lockVertically = true;
        follow.fixedY = 0f;

        GameObject groundGO = new GameObject("Ground");
        groundGO.transform.position = new Vector3(0f, FloorY - 0.25f, 0f);
        groundGO.AddComponent<BoxCollider2D>().size = new Vector2(ViewWidth * 2f, 0.5f);

        CreateWall("Wall_Left", -9.3f);
        CreateWall("Wall_Right", 9.3f);

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

        // Kylo's head reaches y=-0.4 at this scale, so the prompts sit just
        // above that rather than up in the treetops.
        GameObject pLakad   = CreatePromptUI(uiRoot.transform, "Prompt_Lakad",   "Lakad",   new Vector3(-760, 80, 0));
        GameObject pBukas   = CreatePromptUI(uiRoot.transform, "Prompt_Bukas",   "Bukas",   new Vector3(-420, 80, 0));
        GameObject pBati = CreatePromptUI(uiRoot.transform, "Prompt_Bati", "Bati", new Vector3(-80, 80, 0));
        GameObject pPasok   = CreatePromptUI(uiRoot.transform, "Prompt_Pasok",   "Pasok",   new Vector3(260, 80, 0));
        GameObject pTakbo   = CreatePromptUI(uiRoot.transform, "Prompt_Takbo",   "Takbo",   new Vector3(600, 80, 0));

        // --- Objectives: arrive, open, greet, go in, hurry to class ---
        //
        // The order is the story. He cannot be let in before the gate opens,
        // and greeting the guard before he has reached him is a hello shouted
        // across a car park. Each word is the reason the next one can happen.

        var lakad = MakeObjective("Lakad", pLakad);
        UnityEventTools.AddPersistentListener(lakad.onCorrect, player.WalkForward);

        // The gate is the whole picture, so a marker carries the gleam instead
        // of the background object, whose origin is the centre of the screen.
        //
        // The GUARD lights up as well, and that is the point of the beat: the
        // gate does not open by itself. Swapping the picture alone would show
        // a gate that opens on its own while a man stands next to it doing
        // nothing, and the word a child is learning is the one that asked HIM
        // to open it. Lighting both at the same moment is what ties the two
        // together without a second pose for him to be drawn in.
        var bukas = MakeObjective("Bukas", pBukas);
        UnityEventTools.AddPersistentListener(bukas.onCorrect, guardGleam.Play);
        UnityEventTools.AddPersistentListener(bukas.onCorrect, gateSwap.Apply);
        UnityEventTools.AddPersistentListener(bukas.onCorrect, gateGleam.Play);

        // Both of them move: Kylo turns to wave, and the guard waves back.
        // A greeting answered by a figure that does not move reads as the
        // child talking to scenery.
        var bati = MakeObjective("Bati", pBati);
        UnityEventTools.AddPersistentListener(bati.onCorrect, greet.Apply);
        UnityEventTools.AddPersistentListener(bati.onCorrect, guardSwap.Apply);
        UnityEventTools.AddPersistentListener(bati.onCorrect, guardGleam.Play);

        // He turns side-on again, then walks INTO the gate rather than past
        // it - up the frame and smaller, which is what the perspective asks.
        var pasok = MakeObjective("Pasok", pPasok);
        UnityEventTools.AddPersistentListener(pasok.onCorrect, facingWalk.Apply);
        UnityEventTools.AddPersistentListener(pasok.onCorrect, enter.Begin);

        // Takbo is a real run, not a relabelled walk - but it is now a run
        // further INTO the scene, to the foot of the school steps. The legs
        // carry it: at this distance he is too small for speed to read any
        // other way, so the frame rate rather than the distance says "run".
        var takbo = MakeObjective("Takbo", pTakbo);
        UnityEventTools.AddPersistentListener(takbo.onCorrect, runToDoor.Begin);

        SceneObjectiveController controller = uiRoot.AddComponent<SceneObjectiveController>();
        controller.voiceCommand = voiceCommand;
        controller.objectives = new[] { lakad, bukas, bati, pasok, takbo };

        VoiceUIBuilder.BuildVoiceUI(voiceCommand, controller);
        // Still needed in the scene: SceneResultPanel looks the fader up at
        // run time to fade out of whichever button is pressed.
        SceneFaderBuilder.Build();

        // --- Level 2's first scene, so it starts the run; Scene 2 ends it ---
        GameObject runScoreGO = new GameObject("RunScore");
        RunScoreCounter runScore = runScoreGO.AddComponent<RunScoreCounter>();
        runScore.voiceCommand = voiceCommand;
        runScore.resetOnStart = true;   // a fresh level, so a fresh total

        // --- The scene's summary, whose Next button hands on ---
        // The same panel the Bahay scenes stop on, so a child sees how the
        // scene went instead of the next room arriving unannounced.
        SceneResultPanelBuilder.Build(controller, voiceCommand, NextScene);

        System.IO.Directory.CreateDirectory("Assets/Scenes");
        EditorSceneManager.SaveScene(scene, ScenePath);
        AddToBuildSettings();

        string missing = "";
        if (gateShut == null) missing += "\n  paaralan_gate_background_closed - the scene opens on an OPEN gate, so \"Bukas\" changes nothing.";
        if (guardIdle == null) missing += "\n  char_guard_idle - the guard is waving before he is greeted, so \"Bati\" changes nothing.";
        if (backWalk == null) missing += "\n  back_walk_1/3_uniform_with_bag - Kylo walks into the school side-on, which reads as sliding rather than going away.";
        else if (backPass == null) missing += "\n  back_walk_2_uniform_with_bag - the legs-together frame. The walk is two strides with no in-between, so it reads as a shuffle.";
        if (backRunA == null || backRunB == null) missing += "\n  back_run_1/2_uniform_with_bag - \"Takbo\" uses the walk cycle taken faster, so it is quicker but not a different gait.";

        Debug.Log("[SALINLAHI] Paaralan Scene 1 built and saved to " + ScenePath +
                   ". Words: Lakad, Bukas, Bati, Pasok, Takbo - walking up to the gate, " +
                   "having it opened, greeting the guard, going in, running for class. " +
                   "It starts Level 2's run and fades into " + NextScene + ", which ends it " +
                   "and awards the Paaralan badge - so build Paaralan Scene 2 as well, or " +
                   "the level runs into a scene that is not there." +
                   (missing.Length == 0
                        ? " All of its art is present."
                        : "\n\nStill to be drawn, and what the scene does meanwhile:" + missing));
    }

    /// <summary>
    /// A character or object standing on the floor, sized by the world height
    /// it should be rather than by a scale factor, so the art's resolution
    /// does not decide how big it looks.
    /// </summary>
    private static GameObject CreateStanding(string name, string spriteFile, float x, float worldHeight)
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
        go.transform.position = new Vector3(x, FloorY + worldHeight * 0.5f, 0f);
        return go;
    }

    /// <summary>A gleam centred on something, for a word that points at it.</summary>
    private static TimedOverlay CreateGleam(string name, GameObject target, float worldSize)
    {
        GameObject go = new GameObject(name);
        go.transform.position = target.transform.position;
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
    /// A sprite that is allowed not to exist yet, returned as null without a
    /// warning. Art that is still to be drawn - the shut gate, the guard's
    /// still pose - would otherwise fill the console every rebuild with
    /// warnings about files nobody has made yet.
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
