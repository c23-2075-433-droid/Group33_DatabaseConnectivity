// Assets/Editor/VoiceUIBuilder.cs
//
// Shared helper that builds the persistent microphone + replay/speaker
// button UI (Assets/Sprites/mic_icon.png, btn_replay.png), wired to
// VoiceInteractionUI. Used by both:
//   - AddVoiceUIToLevel1.cs (patches the already-built Level 1 scene in
//     place, without touching anything else)
//   - BuildLevel1Scene.cs (so a fresh full rebuild includes it too)
//
// Positioned bottom-center of the screen so it stays in the same comfortable,
// thumb-reachable spot regardless of which word prompt is currently showing.
// To tweak size/position, move the MicButton/ReplayButton RectTransforms in
// the Scene view - no code changes needed.

using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public static class VoiceUIBuilder
{
    private const string SpriteFolder = "Assets/Sprites/";
    private const string CanvasName = "VoiceUI_Canvas";

    /// <summary>
    /// Creates the VoiceUI_Canvas (mic + replay buttons) wired to the given
    /// VoiceCommand/SceneObjectiveController, or reuses it if one already
    /// exists in the currently open scene (safe to call more than once).
    /// </summary>
    public static VoiceInteractionUI BuildVoiceUI(VoiceCommand voiceCommand, SceneObjectiveController controller)
    {
        GameObject existingCanvasGO = GameObject.Find(CanvasName);
        if (existingCanvasGO != null)
        {
            Debug.Log("[SALINLAHI] " + CanvasName + " already exists - reusing it instead of duplicating. " +
                       "Delete it first if you want a clean rebuild.");
            return existingCanvasGO.GetComponent<VoiceInteractionUI>();
        }

        // --- Canvas: same 1920x1080 Screen Space Overlay setup as the rest of the UI ---
        GameObject canvasGO = new GameObject(CanvasName);
        Canvas canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        CanvasScaler scaler = canvasGO.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.Expand;
        canvasGO.AddComponent<GraphicRaycaster>();

        if (Object.FindFirstObjectByType<EventSystem>() == null)
        {
            GameObject eventSystemGO = new GameObject("EventSystem");
            eventSystemGO.AddComponent<EventSystem>();
            eventSystemGO.AddComponent<StandaloneInputModule>();
        }

        Transform canvasT = canvasGO.transform;

        // --- Microphone button: prominent, centered icon; VoiceInteractionUI
        //     pulses it while VoiceCommand reports it's actively listening. ---
        GameObject micGO = CreateImage(canvasT, "MicButton", "mic_icon", new Vector2(-110, -420), new Vector2(220, 220));
        Button micButton = micGO.AddComponent<Button>();
        micButton.targetGraphic = micGO.GetComponent<Image>();

        // --- Replay/speaker button: re-plays the current word's instruction
        //     audio (SceneObjectiveController.PlayCurrentInstruction()). ---
        GameObject replayGO = CreateImage(canvasT, "ReplayButton", "btn_replay", new Vector2(140, -420), new Vector2(150, 150));
        Button replayButton = replayGO.AddComponent<Button>();
        replayButton.targetGraphic = replayGO.GetComponent<Image>();

        VoiceInteractionUI ui = canvasGO.AddComponent<VoiceInteractionUI>();
        ui.voiceCommand = voiceCommand;
        ui.objectiveController = controller;
        ui.micIcon = micGO.GetComponent<Image>();
        ui.micButton = micButton;
        ui.replayButton = replayButton;

        return ui;
    }

    private static GameObject CreateImage(Transform parent, string name, string spriteFile, Vector2 anchoredPos, Vector2 size)
    {
        GameObject go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        Image img = go.AddComponent<Image>();
        img.sprite = LoadSprite(spriteFile);
        img.preserveAspect = true;
        RectTransform rect = go.GetComponent<RectTransform>();
        rect.anchoredPosition = anchoredPos;
        rect.sizeDelta = size;
        return go;
    }

    private static Sprite LoadSprite(string spriteFile)
    {
        string path = SpriteFolder + spriteFile + ".png";
        Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
        if (sprite == null)
            Debug.LogWarning("[SALINLAHI] Could not load sprite at '" + path + "'. Make sure it's imported as Sprite (2D and UI).");
        return sprite;
    }
}
