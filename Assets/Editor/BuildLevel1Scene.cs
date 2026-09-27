// Assets/Editor/BuildLevel1Scene.cs
//
// One-click scene builder for SALINLAHI Chapter 1, Level 1 ("Umaga na!").
// Creates the bedroom background + prop layout, plus a Player_Character
// GameObject wired to your real PlayerMovement/VoiceCommand scripts, and a
// sprite-free ExitTrigger (no door art yet - see the comment where it's built).
//
// Sorting layers (Background, Props, Player, UI) are already set up in
// ProjectSettings/TagManager.asset - no manual step needed for those.
//
// Sprites expected in Assets/Sprites/ (already imported):
//   bedroom_background, bed_mosquito_net, dresser_mirror_clothes,
//   nightstand_clock_slippers, chair_backpack,
//   lying_down, sitting_up, standing, walk_frame_1
//
// Run via: Tools > SALINLAHI > Build Level 1 Scene

using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public class BuildLevel1Scene
{
    private const string SpriteFolder = "Assets/Sprites/";

    [MenuItem("Tools/SALINLAHI/Build Level 1 Scene")]
    public static void BuildScene()
    {
        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        // --- Camera ---
        GameObject camGO = new GameObject("Main Camera");
        Camera cam = camGO.AddComponent<Camera>();
        cam.orthographic = true;
        cam.orthographicSize = 5.4f;
        cam.transform.position = new Vector3(0, 0, -10);
        camGO.tag = "MainCamera";
        camGO.AddComponent<AudioListener>();

        // --- Background ---
        CreateSprite("Background_Bedroom", "bedroom_background", "Background", 0,
            new Vector3(0, 0, 0), Vector3.one);

        // --- Props, spaced out along the room so nothing overlaps ---
        // Left wall: nightstand, well clear of the bed
        CreateSprite("Prop_Nightstand", "nightstand_clock_slippers", "Props", 2,
            new Vector3(-4.4f, -1.55f, 0), new Vector3(0.55f, 0.55f, 1));

        // Bed, centered-left
        CreateSprite("Prop_Bed", "bed_mosquito_net", "Props", 1,
            new Vector3(-2.0f, -0.5f, 0), new Vector3(0.85f, 0.85f, 1));

        // Dresser, center-right, shrunk so its mirror doesn't clip the bed net
        CreateSprite("Prop_Dresser", "dresser_mirror_clothes", "Props", 1,
            new Vector3(0.9f, -1.05f, 0), new Vector3(0.65f, 0.65f, 1));

        // Chair + backpack, between the dresser and the door
        CreateSprite("Prop_Chair_Backpack", "chair_backpack", "Props", 2,
            new Vector3(2.3f, -1.75f, 0), new Vector3(0.5f, 0.5f, 1));

        // --- Player Character, wired to your real scripts ---
        GameObject playerGO = new GameObject("player_character");
        playerGO.tag = "Player"; // make sure the 'Player' tag exists (Project Settings > Tags and Layers)
        playerGO.transform.position = new Vector3(-2.0f, -0.3f, 0);

        SpriteRenderer playerSr = playerGO.AddComponent<SpriteRenderer>();
        playerSr.sortingLayerName = "Player";
        playerSr.sortingOrder = 0;
        playerSr.sprite = LoadSprite("lying_down"); // level opens with the character asleep

        Rigidbody2D playerRb = playerGO.AddComponent<Rigidbody2D>();
        playerRb.gravityScale = 3f;
        playerRb.freezeRotation = true;

        BoxCollider2D playerCol = playerGO.AddComponent<BoxCollider2D>();
        playerCol.size = new Vector2(0.6f, 1.4f);

        PlayerMovement playerMovement = playerGO.AddComponent<PlayerMovement>();
        playerMovement.lyingDownSprite = LoadSprite("lying_down");
        playerMovement.sittingUpSprite = LoadSprite("sitting_up");
        playerMovement.standingSprite = LoadSprite("standing");
        playerMovement.walkFrames = new[] { LoadSprite("walk_frame_1") };

        VoiceCommand voiceCommand = playerGO.AddComponent<VoiceCommand>();
        voiceCommand.player = playerMovement;
        // Bangon/Tayo/Lakad are wired below as SceneObjective entries, not
        // fixed VoiceCommand fields - see the objectives array further down.

        // --- Camera follow, same as MainScene ---
        CameraFollow follow = camGO.AddComponent<CameraFollow>();
        follow.target = playerGO.transform;

        // --- Ground so the player doesn't fall through the floor ---
        GameObject groundGO = new GameObject("Ground");
        groundGO.transform.position = new Vector3(0, -2.1f, 0);
        BoxCollider2D groundCol = groundGO.AddComponent<BoxCollider2D>();
        groundCol.size = new Vector2(20f, 0.5f);

        // --- Exit trigger: no door sprite yet, so this is a plain invisible
        //     BoxCollider2D zone (visible as an orange gizmo box in the Scene
        //     view - see ExitDoorTrigger.OnDrawGizmos). Add a SpriteRenderer
        //     with real door art here later; no code changes needed. ---
        GameObject exitTriggerGO = new GameObject("ExitTrigger");
        exitTriggerGO.transform.position = new Vector3(3.8f, -0.35f, 0);
        BoxCollider2D doorCol = exitTriggerGO.AddComponent<BoxCollider2D>();
        doorCol.isTrigger = true;
        doorCol.size = new Vector2(1f, 2f);
        ExitDoorTrigger exitTrigger = exitTriggerGO.AddComponent<ExitDoorTrigger>();
        // No further scenes exist yet (Bahay's bathroom/bath/dressing/breakfast
        // scenes aren't built) - leave nextSceneName empty so LoadNextScene()
        // just no-ops instead of trying to load something that doesn't exist.
        // Swap this out once Scene 2 exists.
        exitTrigger.nextSceneName = "";
        exitTrigger.levelCompleteUI = LevelCompleteUIBuilder.BuildPanel(
            "Great job! You helped Kylo wake up!\n(More of Level 1 coming soon...)");

        // --- UI: word-prompt canvas, World Space so each prompt can just sit
        //     at a world position above its matching object (bed, door, etc.)
        //     instead of needing screen-space conversion math. ---
        GameObject uiRoot = new GameObject("UI_WordPrompts_Canvas");
        Canvas canvas = uiRoot.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        // Without this, a World Space canvas defaults to the "Default" sorting
        // layer (order 0), which renders BEHIND Background/Props/Player - the
        // prompts would be invisible even when active. Put it on "UI", the
        // topmost layer, so it always draws on top.
        canvas.overrideSorting = true;
        canvas.sortingLayerName = "UI";
        canvas.sortingOrder = 10;
        uiRoot.transform.position = Vector3.zero;
        uiRoot.transform.localScale = new Vector3(0.01f, 0.01f, 0.01f); // world-units-friendly UI scale
        RectTransform canvasRect = uiRoot.GetComponent<RectTransform>();
        canvasRect.sizeDelta = new Vector2(2000, 1000);
        uiRoot.AddComponent<UnityEngine.UI.GraphicRaycaster>();

        // A World Space canvas' children need an EventSystem in the scene for
        // any future interactivity (not strictly required just to render).
        if (Object.FindFirstObjectByType<UnityEngine.EventSystems.EventSystem>() == null)
        {
            GameObject eventSystemGO = new GameObject("EventSystem");
            eventSystemGO.AddComponent<UnityEngine.EventSystems.EventSystem>();
            eventSystemGO.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
        }

        // Positions are in canvas units (world units x100, since the canvas is
        // scaled to 0.01) - matched to the new prop positions above.
        GameObject prompt_Bangon = CreatePromptUI(uiRoot.transform, "Prompt_Bangon", "Bangon",
            new Vector3(-200, 130, 0)); // above the bed
        GameObject prompt_Tayo = CreatePromptUI(uiRoot.transform, "Prompt_Tayo", "Tayo",
            new Vector3(-100, 60, 0)); // beside the bed, where the player stands up
        GameObject prompt_Lakad = CreatePromptUI(uiRoot.transform, "Prompt_Lakad", "Lakad",
            new Vector3(120, 20, 0)); // along the walk path toward the door
        // "Alis" isn't a spoken objective (there's no voice check for it) -
        // it's just a static "go here next" hint above the door, left inactive
        // until a future pass wires it to something. Not part of the
        // objectives array below.
        GameObject prompt_Alis = CreatePromptUI(uiRoot.transform, "Prompt_Alis", "Alis",
            new Vector3(380, 80, 0)); // above the door

        // --- Objectives: Bangon -> Tayo -> Lakad, in order. Each one's
        //     onCorrect is wired (as a persistent, Inspector-visible listener)
        //     to the exact same PlayerMovement call the old hardcoded
        //     VoiceCommand branch used to make - see SceneObjectiveController.cs. ---
        SceneObjectiveController.SceneObjective bangonObjective = new SceneObjectiveController.SceneObjective
        {
            word = "Bangon", promptRoot = prompt_Bangon, onCorrect = new UnityEngine.Events.UnityEvent()
        };
        UnityEventTools.AddPersistentListener(bangonObjective.onCorrect, playerMovement.Bangon);

        SceneObjectiveController.SceneObjective tayoObjective = new SceneObjectiveController.SceneObjective
        {
            word = "Tayo", promptRoot = prompt_Tayo, onCorrect = new UnityEngine.Events.UnityEvent()
        };
        UnityEventTools.AddPersistentListener(tayoObjective.onCorrect, playerMovement.TayoUp);

        SceneObjectiveController.SceneObjective lakadObjective = new SceneObjectiveController.SceneObjective
        {
            word = "Lakad", promptRoot = prompt_Lakad, onCorrect = new UnityEngine.Events.UnityEvent()
        };
        UnityEventTools.AddPersistentListener(lakadObjective.onCorrect, playerMovement.WalkForward);

        SceneObjectiveController controller = uiRoot.AddComponent<SceneObjectiveController>();
        controller.voiceCommand = voiceCommand;
        controller.objectives = new[] { bangonObjective, tayoObjective, lakadObjective };

        // Wire the controller into the scripts that need it.
        exitTrigger.objectiveController = controller;

        // --- Voice UI: mic (listening indicator) + replay/speaker buttons.
        //     See Assets/Editor/VoiceUIBuilder.cs and VoiceInteractionUI.cs. ---
        VoiceUIBuilder.BuildVoiceUI(voiceCommand, controller);

        // --- Save scene ---
        string scenePath = "Assets/Scenes/Chapter1_Level1_UmagaNa.unity";
        System.IO.Directory.CreateDirectory("Assets/Scenes");
        EditorSceneManager.SaveScene(scene, scenePath);

        Debug.Log("[SALINLAHI] Level 1 scene built and saved to " + scenePath +
                   ". Player has PlayerMovement + VoiceCommand attached and wired. " +
                   "Check the Player tag exists, and nudge prop positions to taste.");
    }

    private static GameObject CreateSprite(string name, string spriteFile, string sortingLayer,
        int orderInLayer, Vector3 position, Vector3 scale)
    {
        GameObject go = new GameObject(name);
        SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = LoadSprite(spriteFile);
        sr.sortingLayerName = sortingLayer;
        sr.sortingOrder = orderInLayer;
        go.transform.position = position;
        go.transform.localScale = scale;
        return go;
    }

    /// <summary>
    /// Builds one "arrow + badge + word text" prompt, parented under the
    /// world-space UI canvas at a local position (in canvas units, matching
    /// the mockup's rough layout). Starts inactive except where the
    /// sequencer's Start() turns "Bangon" on.
    /// </summary>
    private static GameObject CreatePromptUI(Transform parent, string name, string word, Vector3 localPos)
    {
        GameObject root = new GameObject(name, typeof(RectTransform));
        root.transform.SetParent(parent, false);
        RectTransform rootRect = root.GetComponent<RectTransform>();
        rootRect.anchoredPosition3D = localPos;
        rootRect.sizeDelta = new Vector2(260, 100);

        // Badge background
        GameObject badgeGO = new GameObject("Badge", typeof(RectTransform));
        badgeGO.transform.SetParent(root.transform, false);
        UnityEngine.UI.Image badgeImg = badgeGO.AddComponent<UnityEngine.UI.Image>();
        badgeImg.sprite = LoadSprite("ui_word_badge");
        RectTransform badgeRect = badgeGO.GetComponent<RectTransform>();
        badgeRect.sizeDelta = new Vector2(220, 70);
        badgeRect.anchoredPosition = Vector2.zero;

        // Word text, centered on the badge
        GameObject textGO = new GameObject("Text", typeof(RectTransform));
        textGO.transform.SetParent(badgeGO.transform, false);
        UnityEngine.UI.Text text = textGO.AddComponent<UnityEngine.UI.Text>();
        text.text = word;
        text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        text.fontSize = 36;
        text.fontStyle = FontStyle.Bold;
        text.alignment = TextAnchor.MiddleCenter;
        text.color = Color.white;
        RectTransform textRect = textGO.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;

        // Arrow, above the badge, pointing up
        GameObject arrowGO = new GameObject("Arrow", typeof(RectTransform));
        arrowGO.transform.SetParent(root.transform, false);
        UnityEngine.UI.Image arrowImg = arrowGO.AddComponent<UnityEngine.UI.Image>();
        arrowImg.sprite = LoadSprite("ui_arrow");
        RectTransform arrowRect = arrowGO.GetComponent<RectTransform>();
        arrowRect.sizeDelta = new Vector2(50, 50);
        arrowRect.anchoredPosition = new Vector2(0, 55);

        root.SetActive(false); // SceneObjectiveController.Start() turns "Bangon" on
        return root;
    }

    private static Sprite LoadSprite(string spriteFile)
    {
        string path = SpriteFolder + spriteFile + ".png";
        Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
        if (sprite == null)
        {
            Debug.LogWarning("[SALINLAHI] Could not load sprite at '" + path +
                              "'. Make sure it's imported as Sprite (2D and UI).");
        }
        return sprite;
    }
}
