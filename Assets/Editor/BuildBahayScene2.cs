// Assets/Editor/BuildBahayScene2.cs
//
// One-click scene builder for Bahay Scene 2 ("Punta sa Banyo" - walking to
// the bathroom). Teaches five words: Lakad, Kaliwa, Kanan, Bukas, Ilaw.
//
// Layout: the hallway and the bathroom are two rooms STACKED at the same
// position, each scaled to exactly fill one 16:9 camera view (19.2 x 10.8
// world units), and only one is ever active. Saying "Bukas" opens the door,
// walks Kylo into it, and fades across to the bathroom (see RoomTransition).
//
// They are stacked rather than placed side by side on purpose: with two rooms
// laid out along X the camera inevitably shows half of each at once, so the
// player could see into the dark bathroom from the lit hallway. Stacking them
// means the camera never has to move, and the room change is a deliberate
// moment instead of a pan. The bathroom starts under a dark overlay until
// "Ilaw".
//
// Sprites expected in Assets/Sprites/:
//   hallway_background, bathroom_background, door_open (optional for now),
//   lying_down / sitting_up / standing / walk_frame_1..4 - all _barefoot,
//   because the shoes do not go on until Scene 4 - ui_arrow, ui_word_badge
//
// Run via: Tools > SALINLAHI > Build Bahay Scene 2

using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class BuildBahayScene2
{
    private const string SpriteFolder = "Assets/Sprites/";
    private const string ScenePath = "Assets/Scenes/Chapter1_Level2_Banyo.unity";
    private const string WhitePixelPath = SpriteFolder + "white_pixel.png";

    // Backgrounds are 1536x864 at 100 pixels-per-unit = 15.36 x 8.64 world
    // units. The camera (orthographic size 5.4) shows 10.8 x 19.2, so each
    // background is scaled up to fill exactly one screen with no gaps.
    private const float BgScale = 10.8f / 8.64f;      // 1.25
    private const float ScreenWidth = 19.2f;          // 15.36 * 1.25
    private const float FloorY = -2.98f;              // top of the floor planks

    // The character art imports at 120 pixels per unit (not the usual 100 -
    // check the .meta before changing any of these numbers), so the 745px
    // frames are 6.21 world units tall unscaled.
    private const float SpritePPU = 120f;

    // Scene 1 stands Kylo at 0.83, which is 5.15 units. Scene 2 has to use the
    // same number or he visibly shrinks on walking through the bedroom door.
    private const float PlayerScale = 0.83f;

    // The lowest opaque pixel of the character art is row 743 of 745, and the
    // pivot is the texture centre, so his feet sit this far below the
    // transform. Standing him on the floor means offsetting by this, NOT by
    // half the collider - that was what left him hovering above the boards.
    private const float FeetBelowPivot = (743.5f - 745f / 2f) / SpritePPU;   // 3.09
    private const float PlayerFeetY = FloorY + FeetBelowPivot * PlayerScale;

    // Centre of the bathroom door painted into the hallway art.
    private const float DoorX = 7.31f;

    // Where the level carries on once the bathroom's words are done.
    private const string NextScene = "Chapter1_Level3_Maligo";

    [MenuItem("Tools/SALINLAHI/Build Bahay Scene 2")]
    public static void BuildScene()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            Debug.LogError("[SALINLAHI] Stop Play mode first - this rebuilds the scene, which Unity doesn't allow while the game is running.");
            return;
        }

        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        // --- Camera ---
        GameObject camGO = new GameObject("Main Camera");
        Camera cam = camGO.AddComponent<Camera>();
        cam.orthographic = true;
        cam.orthographicSize = 5.4f;
        cam.transform.position = new Vector3(0, 0, -10);
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = Color.black;
        camGO.tag = "MainCamera";
        camGO.AddComponent<AudioListener>();

        // --- Rooms: both at x=0, only one active at a time ---
        GameObject hallway = new GameObject("Room_Hallway");
        CreateBackground("Background_Hallway", "hallway_background", hallway.transform);

        GameObject bathroom = new GameObject("Room_Bathroom");
        CreateBackground("Background_Bathroom", "bathroom_background", bathroom.transform);

        // --- The loose things in the bathroom ---
        // Each is its own sprite rather than part of the background painting,
        // so a spoken word can take it away (see PickUpItem). They were cut
        // out of the original artwork by Tools/cut_bathroom_items.py and the
        // gaps repainted by Tools/fill_bathroom_background.py, so these
        // positions are exactly where they were painted and the room looks
        // unchanged until something is picked up.
        GameObject itemTabo    = CreateBathroomItem(bathroom.transform, "Item_Tabo",    "item_tabo",    new Vector2(-7.056f, 0.738f), 5);
        GameObject itemSabon   = CreateBathroomItem(bathroom.transform, "Item_Sabon",   "item_sabon",   new Vector2( 1.556f, 1.025f), 5);
        CreateBathroomItem(bathroom.transform, "Item_Suklay",  "item_suklay",  new Vector2( 2.750f, 0.988f), 5);
        // The brush stands in the cup, so the cup has to draw over it.
        GameObject itemSipilyo = CreateBathroomItem(bathroom.transform, "Item_Sipilyo", "item_sipilyo", new Vector2( 4.225f, 1.694f), 4);
        CreateBathroomItem(bathroom.transform, "Item_Baso",    "item_baso",    new Vector2( 3.931f, 1.188f), 5);
        GameObject itemTuwalya = CreateBathroomItem(bathroom.transform, "Item_Tuwalya", "item_tuwalya", new Vector2( 7.262f, 1.019f), 5);

        // Saying one of these words takes that object off the wall or shelf.
        // Suklay and Baso have no word yet - combing belongs with getting
        // dressed - so they stay as scenery.
        PickUpItem pickTabo    = AddPickUp(itemTabo);
        PickUpItem pickSabon   = AddPickUp(itemSabon);
        PickUpItem pickSipilyo = AddPickUp(itemSipilyo);
        PickUpItem pickTuwalya = AddPickUp(itemTuwalya);

        // --- Darkness over the bathroom only; the hallway stays lit ---
        GameObject darkGO = new GameObject("BathroomDarkness");
        darkGO.transform.SetParent(bathroom.transform, false);
        SpriteRenderer darkSr = darkGO.AddComponent<SpriteRenderer>();
        darkSr.sprite = GetOrCreateWhitePixel();
        darkSr.color = new Color(0.05f, 0.08f, 0.25f, 0.88f); // deep night blue
        darkSr.sortingLayerName = "Props";
        darkSr.sortingOrder = 50;                             // above props, below the player
        darkSr.drawMode = SpriteDrawMode.Sliced;
        darkSr.size = new Vector2(ScreenWidth, 10.8f);

        LightSwitchOverlay lightSwitch = darkGO.AddComponent<LightSwitchOverlay>();
        lightSwitch.overlay = darkSr;
        lightSwitch.darkAlpha = 0.88f;

        // The bathroom only exists once Kylo walks through the door.
        bathroom.SetActive(false);

        // --- Bathroom door: an OPEN door laid over the closed one painted into
        //     the hallway art, hidden until the player says "Bukas". ---
        GameObject doorOpen = new GameObject("Door_Open");
        doorOpen.transform.SetParent(hallway.transform, false);
        doorOpen.transform.localPosition = new Vector3(DoorX, 0.18f, 0f);
        SpriteRenderer doorSr = doorOpen.AddComponent<SpriteRenderer>();
        doorSr.sprite = LoadSprite("door_open");   // may be null until the art is imported
        doorSr.sortingLayerName = "Props";
        doorSr.sortingOrder = 10;
        // Scale to roughly the height of the painted door, keeping the art's
        // own aspect ratio - stretching it to a fixed size would distort it.
        if (doorSr.sprite != null)
        {
            float s = 6.4f / doorSr.sprite.bounds.size.y;
            doorOpen.transform.localScale = new Vector3(s, s, 1f);
        }
        doorOpen.SetActive(false);                 // "Bukas" switches this on

        // --- Player ---
        GameObject playerGO = new GameObject("player_character");
        playerGO.tag = "Player";
        playerGO.transform.position = new Vector3(-6.5f, PlayerFeetY, 0f);
        playerGO.transform.localScale = new Vector3(PlayerScale, PlayerScale, 1f);

        SpriteRenderer playerSr = playerGO.AddComponent<SpriteRenderer>();
        playerSr.sortingLayerName = "Player";
        playerSr.sprite = CharacterPoses.Barefoot("standing");  // this scene opens already awake

        Rigidbody2D playerRb = playerGO.AddComponent<Rigidbody2D>();
        playerRb.gravityScale = 3f;
        playerRb.freezeRotation = true;

        // Collider is in LOCAL units, so it is scaled by the transform. Its
        // height matches the art's own 6.21 units; making it taller than the
        // sprite (it used to be 7.0) props the sprite up off the ground.
        BoxCollider2D playerCol = playerGO.AddComponent<BoxCollider2D>();
        playerCol.size = new Vector2(1.2f, FeetBelowPivot * 2f);

        PlayerMovement player = playerGO.AddComponent<PlayerMovement>();
        player.lyingDownSprite = CharacterPoses.Barefoot("lying_down");
        player.sittingUpSprite = CharacterPoses.Barefoot("sitting_up");
        player.standingSprite = CharacterPoses.Barefoot("standing");
        player.walkFrames = CharacterPoses.BarefootWalk();
        // The player is already up and about in this scene, so movement is
        // unlocked from the start rather than gated behind Bangon/Tayo.
        player.currentWakeStage = PlayerMovement.WakeStage.Standing;

        VoiceCommand voiceCommand = playerGO.AddComponent<VoiceCommand>();
        voiceCommand.player = player;

        // --- Walking through the door into the bathroom ---
        GameObject transitionGO = new GameObject("RoomTransition");
        RoomTransition transition = transitionGO.AddComponent<RoomTransition>();
        transition.fromRoom = hallway;
        transition.toRoom = bathroom;
        transition.player = playerGO.transform;
        transition.playerMovement = player;
        // Kylo comes out of the doorway on the left-hand side of the bathroom.
        transition.arrivalPosition = new Vector2(-5f, PlayerFeetY);
        transition.doorX = DoorX;

        // With both rooms stacked at x=0 the camera never needs to move. It
        // still follows the player so the component stays useful if the rooms
        // are ever widened, but it is pinned to the room centre for now -
        // an unconstrained follow is what made the camera show past the
        // artwork and leave a black band down the side.
        CameraFollow follow = camGO.AddComponent<CameraFollow>();
        follow.target = playerGO.transform;
        follow.offset = new Vector3(0f, 0f, -10f);
        follow.clampHorizontally = true;
        follow.minX = 0f;
        follow.maxX = 0f;
        follow.lockVertically = true;
        follow.fixedY = 0f;

        // --- Floor ---
        GameObject groundGO = new GameObject("Ground");
        groundGO.transform.position = new Vector3(0f, FloorY - 0.25f, 0f);
        BoxCollider2D groundCol = groundGO.AddComponent<BoxCollider2D>();
        groundCol.size = new Vector2(ScreenWidth * 2f, 0.5f);

        // --- Invisible side walls ---
        // The camera is fixed, so without these a few "Kanan"s in a row would
        // walk Kylo straight off the edge of the picture and out of sight.
        CreateWall("Wall_Left", -9.3f);
        CreateWall("Wall_Right", 9.3f);

        // --- Word prompt canvas (world space, same pattern as Level 1) ---
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

        // Positions are in canvas units (world units x100).
        // Kylo's head now reaches y=2.15 world (215 in canvas units), so the
        // prompts that sit over him have to clear that.
        GameObject pLakad  = CreatePromptUI(uiRoot.transform, "Prompt_Lakad",  "Lakad",  new Vector3(-650, 300, 0));
        GameObject pKaliwa = CreatePromptUI(uiRoot.transform, "Prompt_Kaliwa", "Kaliwa", new Vector3(-850, 300, 0));
        GameObject pKanan  = CreatePromptUI(uiRoot.transform, "Prompt_Kanan",  "Kanan",  new Vector3(300, 300, 0));
        GameObject pBukas  = CreatePromptUI(uiRoot.transform, "Prompt_Bukas",  "Bukas",  new Vector3(731, 400, 0));
        // "Ilaw" is asked inside the bathroom, which now occupies the same
        // screen space as the hallway, so this sits top-centre over the room
        // rather than one screen to the right.
        GameObject pIlaw   = CreatePromptUI(uiRoot.transform, "Prompt_Ilaw",   "Ilaw",   new Vector3(0, 400, 0));

        // The four objects in the bathroom. Each prompt sits just above the
        // thing it names, worked out from the sprite's own height.
        GameObject pTabo    = CreatePromptUI(uiRoot.transform, "Prompt_Tabo",    "Tabo",    new Vector3(-706, 195, 0));
        GameObject pSabon   = CreatePromptUI(uiRoot.transform, "Prompt_Sabon",   "Sabon",   new Vector3( 156, 185, 0));
        GameObject pSipilyo = CreatePromptUI(uiRoot.transform, "Prompt_Sipilyo", "Sipilyo", new Vector3( 422, 255, 0));
        GameObject pTuwalya = CreatePromptUI(uiRoot.transform, "Prompt_Tuwalya", "Tuwalya", new Vector3( 726, 390, 0));

        // --- Objectives, in order. Each onCorrect is a persistent listener so
        //     it shows up and stays editable in the Inspector. ---
        var lakad  = MakeObjective("Lakad",  pLakad);
        UnityEventTools.AddPersistentListener(lakad.onCorrect, player.WalkForward);

        var kaliwa = MakeObjective("Kaliwa", pKaliwa);
        UnityEventTools.AddPersistentListener(kaliwa.onCorrect, player.WalkLeft);

        var kanan  = MakeObjective("Kanan",  pKanan);
        UnityEventTools.AddPersistentListener(kanan.onCorrect, player.WalkRight);

        // "Bukas" reveals the open-door sprite over the painted closed one,
        // then RoomTransition walks Kylo in and fades across to the bathroom.
        // Both run from the same event, in this order.
        var bukas  = MakeObjective("Bukas",  pBukas);
        UnityEventTools.AddBoolPersistentListener(bukas.onCorrect, doorOpen.SetActive, true);
        UnityEventTools.AddPersistentListener(bukas.onCorrect, transition.EnterRoom);

        // "Ilaw" fades the darkness off the bathroom.
        var ilaw   = MakeObjective("Ilaw",   pIlaw);
        UnityEventTools.AddPersistentListener(ilaw.onCorrect, lightSwitch.TurnOnLight);

        // Now the room is lit, the things in it can be named and taken. The
        // order follows the routine: water, soap, teeth, then the towel to
        // dry off with. No voice-over for these yet - the prompts carry them.
        var tabo    = MakeObjective("Tabo",    pTabo);
        UnityEventTools.AddPersistentListener(tabo.onCorrect, pickTabo.PickUp);

        var sabon   = MakeObjective("Sabon",   pSabon);
        UnityEventTools.AddPersistentListener(sabon.onCorrect, pickSabon.PickUp);

        var sipilyo = MakeObjective("Sipilyo", pSipilyo);
        UnityEventTools.AddPersistentListener(sipilyo.onCorrect, pickSipilyo.PickUp);

        var tuwalya = MakeObjective("Tuwalya", pTuwalya);
        UnityEventTools.AddPersistentListener(tuwalya.onCorrect, pickTuwalya.PickUp);

        SceneObjectiveController controller = uiRoot.AddComponent<SceneObjectiveController>();
        controller.voiceCommand = voiceCommand;
        controller.objectives = new[] { lakad, kaliwa, kanan, bukas, ilaw,
                                        tabo, sabon, sipilyo, tuwalya };

        // The transition holds the sequence while Kylo walks through the door,
        // so "Ilaw" is not asked until the bathroom has actually faded in.
        transition.objectiveController = controller;

        // --- Voice UI (mic + replay buttons) ---
        VoiceUIBuilder.BuildVoiceUI(voiceCommand, controller);

        // --- Fade in on arrival from Scene 1 ---
        SceneFader fader = SceneFaderBuilder.Build();

        // --- This scene's words count towards the level's total ---
        // DatabaseDemoBuilder used to add this on its way past. It now runs
        // in Scene 3, so without this the bathroom's nine words would be
        // dropped from the result the player is shown.
        GameObject runScoreGO = new GameObject("RunScore");
        RunScoreCounter runScore = runScoreGO.AddComponent<RunScoreCounter>();
        runScore.voiceCommand = voiceCommand;
        runScore.resetOnStart = false;   // Scene 1 starts the run, not this one

        // --- Scene 2 hands on to Scene 3 (the bath) ---
        // The level's result is saved and shown in whichever scene ends the
        // level, and that is no longer this one. Fading out of the last word
        // rather than stopping here keeps the score running across both.
        UnityEventTools.AddStringPersistentListener(
            controller.onAllObjectivesComplete, fader.FadeOutAndLoad, NextScene);

        System.IO.Directory.CreateDirectory("Assets/Scenes");
        EditorSceneManager.SaveScene(scene, ScenePath);
        AddToBuildSettings();

        Debug.Log("[SALINLAHI] Bahay Scene 2 built and saved to " + ScenePath + ". " +
                   "Words: Lakad, Kaliwa, Kanan, Bukas, Ilaw, Tabo, Sabon, Sipilyo, Tuwalya. " +
                   (LoadSprite("door_open") == null
                       ? "NOTE: door_open sprite not found - the Door_Open object exists but has no art yet. " +
                         "Import it and re-run this command."
                       : "Door art wired.") +
                   " Saying \"Bukas\" opens the door and fades across into the bathroom. " +
                   "The last word now fades on into " + NextScene + " - build that scene too, " +
                   "or the level stops at a black screen.");
    }

    private static SceneObjectiveController.SceneObjective MakeObjective(string word, GameObject prompt)
    {
        return new SceneObjectiveController.SceneObjective
        {
            word = word,
            promptRoot = prompt,
            // Attached by name, not by hand - a rebuild would wipe anything
            // assigned in the Inspector. See VoiceClipLibrary.
            instructionClip = VoiceClipLibrary.ForWord(word),
            onCorrect = new UnityEngine.Events.UnityEvent(),
        };
    }

    /// <summary>
    /// Makes an object something a spoken word can take away. No sprite swap
    /// is set, so PickUpItem just removes it - nothing is carried on Kylo.
    /// </summary>
    private static PickUpItem AddPickUp(GameObject item)
    {
        PickUpItem pick = item.AddComponent<PickUpItem>();
        pick.itemInScene = item;
        return pick;
    }

    /// <summary>
    /// One of the loose bathroom objects, laid over the emptied background at
    /// the spot it was painted. Same scale as the background, because the
    /// sprites were cut from it at the same 100 pixels per unit, and below
    /// the darkness overlay so the room's objects stay dark until "Ilaw".
    /// </summary>
    private static GameObject CreateBathroomItem(Transform parent, string name,
                                                 string spriteFile, Vector2 pos, int order)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.transform.localPosition = new Vector3(pos.x, pos.y, 0f);
        go.transform.localScale = new Vector3(BgScale, BgScale, 1f);
        SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = LoadSprite(spriteFile);
        sr.sortingLayerName = "Props";
        sr.sortingOrder = order;
        return go;
    }

    private static GameObject CreateBackground(string name, string spriteFile, Transform parent)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);
        SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = LoadSprite(spriteFile);
        sr.sortingLayerName = "Background";
        sr.sortingOrder = 0;
        go.transform.localPosition = Vector3.zero;
        go.transform.localScale = new Vector3(BgScale, BgScale, 1f);
        return go;
    }

    /// <summary>An invisible collider that stops the player leaving the view.</summary>
    private static GameObject CreateWall(string name, float x)
    {
        GameObject go = new GameObject(name);
        go.transform.position = new Vector3(x, 0f, 0f);
        BoxCollider2D col = go.AddComponent<BoxCollider2D>();
        col.size = new Vector2(1f, 20f);
        return go;
    }

    /// <summary>
    /// A plain white 4x4 sprite, used as a tintable quad for the darkness
    /// overlay. Created on first use because Unity 6 no longer ships an
    /// accessible built-in white sprite.
    /// </summary>
    private static Sprite GetOrCreateWhitePixel()
    {
        Sprite existing = AssetDatabase.LoadAssetAtPath<Sprite>(WhitePixelPath);
        if (existing != null) return existing;

        Texture2D tex = new Texture2D(4, 4);
        Color[] pixels = new Color[16];
        for (int i = 0; i < pixels.Length; i++) pixels[i] = Color.white;
        tex.SetPixels(pixels);
        tex.Apply();
        System.IO.File.WriteAllBytes(WhitePixelPath, tex.EncodeToPNG());
        Object.DestroyImmediate(tex);

        AssetDatabase.ImportAsset(WhitePixelPath, ImportAssetOptions.ForceUpdate);
        TextureImporter importer = (TextureImporter)AssetImporter.GetAtPath(WhitePixelPath);
        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Single;
        importer.mipmapEnabled = false;

        // SpriteDrawMode.Sliced (used to stretch this quad over the room)
        // requires a FullRect mesh; the default Tight mesh makes Unity warn
        // and ignore the size.
        TextureImporterSettings settings = new TextureImporterSettings();
        importer.ReadTextureSettings(settings);
        settings.spriteMeshType = SpriteMeshType.FullRect;
        importer.SetTextureSettings(settings);

        importer.SaveAndReimport();

        return AssetDatabase.LoadAssetAtPath<Sprite>(WhitePixelPath);
    }

    private static void AddToBuildSettings()
    {
        var scenes = new System.Collections.Generic.List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
        foreach (var s in scenes) if (s.path == ScenePath) return;
        scenes.Add(new EditorBuildSettingsScene(ScenePath, true));
        EditorBuildSettings.scenes = scenes.ToArray();
    }

    /// <summary>Arrow + badge + word label, matching Level 1's prompts.</summary>
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

        root.SetActive(false); // SceneObjectiveController shows the first one
        return root;
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
