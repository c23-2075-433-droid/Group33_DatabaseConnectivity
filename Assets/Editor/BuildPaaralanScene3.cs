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
// are. So every placement below is Scene 2's, unchanged, and Kylo starts
// seated with no standing pose in the scene at all.
//
// WHAT EACH WORD DOES. Four of the five are things rather than actions, and
// the one pose available is a seated profile, so the feedback is carried by
// what lights up rather than by what Kylo does:
//
//   Aklat    the textbook appears on the desk
//   Basa     it lights up - he is reading it
//   Sulat    the notebook and pencil light up - he is writing
//   Tanong   Kylo lights up: he is the one putting his hand up
//   Sagot    Teacher Rachel lights up, answering him - "Tama! Magaling!"
//
// Tanong and Sagot are both Kylo's in the design document. Lighting him twice
// in a row reads as nothing happening, so the answer is shown landing on the
// teacher instead, which is also what the document's dialogue describes.
//
// Sprites expected in Assets/Sprites/:
//   menu_bg (or klasrum_background), sitting_desk_uniform, char_teacher,
//   klasrum_desk_front, prop_aklat, prop_kuwaderno, prop_lapis, fx_gleam,
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

    // Scene 2's seat, unchanged. See BuildPaaralanScene2 for where these came
    // from - a student desk measures 1.7 world units in this painting and a
    // school desk is about 0.70 m, which fixes the scale of everything else.
    private const float SitX = -5.20f;
    private const float SitFeetY = -4.52f;            // under the desk, never seen
    private const float SeatedHeight = 3.10f;

    // The desk surface. The notebook and pencil are already out - the child
    // took them from the bag in Scene 2 - so only the textbook arrives here.
    // Nearer the camera than Scene 2 put them. Higher up the desk, the left
    // half is taken by the stack of textbooks painted into the room - an open
    // book placed there disappeared behind Filipino, Matematika and Araling
    // Panlipunan. This band is clear desk top across its whole width.
    private const float AklatX = -5.60f;
    private const float AklatBaseY = -4.15f;
    private const float AklatHeight = 1.00f;
    private const float NotebookX = -4.20f;
    private const float NotebookBaseY = -3.95f;
    private const float NotebookHeight = 0.70f;
    // In front of the open book rather than beside the notebook: at the same
    // x as the notebook the two overlapped almost exactly and the pencil was
    // invisible under it.
    private const float PencilX = -5.60f;
    private const float PencilBaseY = -4.62f;
    private const float PencilHeight = 0.28f;

    // Teacher Rachel, behind her desk at the front. char_teacher is WAIST-UP,
    // so her "floor" is her hip line and the desk top hides that cut.
    private const float TeacherX = 2.60f;
    private const float TeacherHipY = -0.70f;
    private const float TeacherHeight = 1.95f;

    private const int OrderKylo = 0;
    private const int OrderDeskFront = 10;            // the desk layer, in front of him
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
            "sitting_desk_uniform", "char_teacher", "klasrum_desk_front",
            "prop_aklat", "prop_kuwaderno", "prop_lapis", "fx_gleam",
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
        Sprite roomSprite = LoadSpriteOptional("klasrum_background");
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

        GameObject teacher = CreateStandingOptional("Char_Teacher", "char_teacher",
                                                     TeacherX, TeacherHipY, TeacherHeight);

        // --- Kylo, already seated ---
        // The listening rig stays switched on and separate from the pose, the
        // lesson Bahay Scene 5 taught: with VoiceCommand on an object that
        // gets switched off, its Update stops and the Editor's Enter key dies
        // while real speech keeps working.
        GameObject rig = new GameObject("Kylo_Rig");
        rig.tag = "Player";
        rig.transform.position = new Vector3(SitX, SitFeetY, 0f);

        GameObject seatGO = CreateFigure("Kylo_Seated", "sitting_desk_uniform",
                                          SitX, SitFeetY, SeatedHeight);

        // The desk, drawn over his lap so he reads as sitting AT it.
        GameObject deskFront = new GameObject("Foreground_Desk");
        SpriteRenderer deskSr = deskFront.AddComponent<SpriteRenderer>();
        deskSr.sprite = LoadSpriteOptional("klasrum_desk_front");
        deskSr.sortingLayerName = "Player";
        deskSr.sortingOrder = OrderDeskFront;
        if (deskSr.sprite == null) deskFront.SetActive(false);
        else deskFront.transform.localScale = bg.transform.localScale;

        // --- What is on the desk ---
        // The notebook and pencil are already out; the textbook is not, and
        // "Aklat" is what puts it there.
        GameObject propAklat = CreatePropOptional("Prop_Aklat", "prop_aklat",
                                                   AklatX, AklatHeight, AklatBaseY);
        GameObject propNotebook = CreatePropOptional("Prop_Kuwaderno", "prop_kuwaderno",
                                                      NotebookX, NotebookHeight, NotebookBaseY);
        GameObject propPencil = CreatePropOptional("Prop_Lapis", "prop_lapis",
                                                    PencilX, PencilHeight, PencilBaseY);
        if (propNotebook != null) propNotebook.SetActive(true);
        if (propPencil != null) propPencil.SetActive(true);

        // Nothing walks in this scene, so Kylo needs no physics. PlayerMovement
        // is here because VoiceCommand needs one and the objective controller
        // reads it to know when an action has finished.
        PlayerMovement player = rig.AddComponent<PlayerMovement>();
        player.standingSprite = LoadSprite("sitting_desk_uniform");
        player.currentWakeStage = PlayerMovement.WakeStage.Standing;
        rig.GetComponent<Rigidbody2D>().bodyType = RigidbodyType2D.Kinematic;

        VoiceCommand voiceCommand = rig.AddComponent<VoiceCommand>();
        voiceCommand.player = player;

        // --- What lights up for each word ---
        TimedOverlay gleamAklat = CreateGleamAt("Gleam_Aklat", AklatX,
                                                 AklatBaseY + AklatHeight * 0.5f, 1.5f);
        TimedOverlay gleamBasa = CreateGleamAt("Gleam_Basa", AklatX,
                                                AklatBaseY + AklatHeight * 0.5f, 1.9f);
        TimedOverlay gleamSulat = CreateGleamAt("Gleam_Sulat", (NotebookX + PencilX) * 0.5f,
                                                 NotebookBaseY + NotebookHeight * 0.5f, 1.6f);
        TimedOverlay gleamTanong = CreateGleamAt("Gleam_Tanong", SitX, SitFeetY + SeatedHeight * 0.72f, 2.4f);
        TimedOverlay gleamSagot = teacher != null
            ? CreateGleam("Gleam_Sagot", teacher, 2.4f)
            : CreateGleamAt("Gleam_Sagot", TeacherX, TeacherHipY + TeacherHeight * 0.5f, 2.4f);

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

        if (Object.FindFirstObjectByType<UnityEngine.EventSystems.EventSystem>() == null)
        {
            GameObject es = new GameObject("EventSystem");
            es.AddComponent<UnityEngine.EventSystems.EventSystem>();
            es.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
        }

        // Only one is on screen at a time, so they overlap freely - each sits
        // over the thing its word acts on.
        GameObject pAklat = CreatePromptUI(uiRoot.transform, "Prompt_Aklat", "Aklat", new Vector3(-700, -230, 0));
        GameObject pBasa = CreatePromptUI(uiRoot.transform, "Prompt_Basa", "Basa", new Vector3(-700, -150, 0));
        GameObject pSulat = CreatePromptUI(uiRoot.transform, "Prompt_Sulat", "Sulat", new Vector3(-425, -220, 0));
        GameObject pTanong = CreatePromptUI(uiRoot.transform, "Prompt_Tanong", "Tanong", new Vector3(-520, -40, 0));
        GameObject pSagot = CreatePromptUI(uiRoot.transform, "Prompt_Sagot", "Sagot", new Vector3(260, 150, 0));

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
        SceneFader fader = SceneFaderBuilder.Build();

        // --- A middle scene of Level 2 now: Scene 4 ends it ---
        // The counter is added by hand here, because DatabaseDemoBuilder used
        // to bring one along with the result panel and that panel has moved on.
        // Without it the five words said in this lesson leave the level total.
        GameObject runScoreGO = new GameObject("RunScore");
        RunScoreCounter runScore = runScoreGO.AddComponent<RunScoreCounter>();
        runScore.voiceCommand = voiceCommand;
        runScore.objectiveController = controller;
        runScore.resetOnStart = false;   // Scene 1 starts the run

        // The bell goes and the lesson breaks for recess.
        UnityEventTools.AddStringPersistentListener(
            controller.onAllObjectivesComplete, fader.FadeOutAndLoad, NextScene);

        System.IO.Directory.CreateDirectory("Assets/Scenes");
        EditorSceneManager.SaveScene(scene, ScenePath);
        AddToBuildSettings();

        Debug.Log("[SALINLAHI] Paaralan Scene 3 built and saved to " + ScenePath +
                   ". Words: Aklat, Basa, Sulat, Tanong, Sagot - all five new to the game. " +
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

    /// <summary>A character sized by world height; null when its art is absent.</summary>
    private static GameObject CreateStandingOptional(string name, string spriteFile,
                                                      float x, float floorY, float worldHeight)
    {
        Sprite sprite = LoadSpriteOptional(spriteFile);
        if (sprite == null) return null;

        GameObject go = new GameObject(name);
        SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = sprite;
        sr.sortingLayerName = "Props";
        sr.sortingOrder = 5;

        float s = sprite.bounds.size.y > 0.0001f ? worldHeight / sprite.bounds.size.y : 1f;
        go.transform.localScale = new Vector3(s, s, 1f);
        go.transform.position = new Vector3(x, floorY + worldHeight * 0.5f, 0f);
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

    private static TimedOverlay CreateGleam(string name, GameObject target, float worldSize)
    {
        return CreateGleamAt(name, target.transform.position.x, target.transform.position.y, worldSize);
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
