// Assets/Editor/LevelCompleteUIBuilder.cs
//
// Shared helper that builds a simple "Scene Complete!" panel (Screen Space
// Overlay canvas: dim backdrop + dark wood-tinted panel + centered text),
// wired to ExitDoorTrigger.levelCompleteUI and shown when the player reaches
// the end of the scene.
//
// This is a PLACEHOLDER: Level 1 (Bahay) only has its first scene ("Waking
// Up") built so far, so ExitDoorTrigger has nowhere real to send the player
// yet - nextSceneName is left empty and this panel just says so, instead of
// trying to load a scene that doesn't exist. Once Scene 2 (the bathroom)
// exists, swap this out for a real transition - ExitDoorTrigger doesn't care
// what levelCompleteUI looks like, only that it exists.
//
// Used by both AddExitTrigger.cs (patches the already-built Level 1 scene)
// and BuildLevel1Scene.cs (fresh full rebuild).

using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public static class LevelCompleteUIBuilder
{
    private const string CanvasName = "LevelComplete_Canvas";

    /// <summary>
    /// Builds the "Scene Complete!" canvas (inactive by default -
    /// SessionScoreTracker / ExitDoorTrigger turn it on when the player
    /// finishes). Any existing copies are destroyed and rebuilt, so the panel
    /// always matches the current code - this is generated placeholder UI, so
    /// there's nothing hand-authored to preserve, and rebuilding avoids stale
    /// panels missing newly added text fields.
    ///
    /// Children are named TitleText / ScoreText / RecordsText so callers can
    /// find and wire them.
    /// </summary>
    public static GameObject BuildPanel(string message)
    {
        // NOTE: GameObject.Find() can't be used here - this canvas is created
        // INACTIVE, and Find() only searches active objects, so it would never
        // see an existing one.
        foreach (GameObject stale in FindAllInScene(CanvasName))
        {
            Debug.Log("[SALINLAHI] Replacing existing " + CanvasName + ".");
            Object.DestroyImmediate(stale);
        }

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

        // Dim backdrop so the panel reads clearly over the level behind it.
        GameObject dim = new GameObject("Dim", typeof(RectTransform));
        dim.transform.SetParent(canvasT, false);
        Image dimImg = dim.AddComponent<Image>();
        dimImg.color = new Color(0, 0, 0, 0.5f);
        RectTransform dimRect = dim.GetComponent<RectTransform>();
        dimRect.anchorMin = Vector2.zero;
        dimRect.anchorMax = Vector2.one;
        dimRect.offsetMin = Vector2.zero;
        dimRect.offsetMax = Vector2.zero;

        GameObject panel = new GameObject("Panel", typeof(RectTransform));
        panel.transform.SetParent(canvasT, false);
        Image panelImg = panel.AddComponent<Image>();
        // Plain solid colour - deliberately no sprite. Unity 6 no longer serves
        // the old built-in "UI/Skin/Background.psd" via GetBuiltinResource, and
        // asking for it just logs an error and returns null.
        panelImg.color = new Color(0.25f, 0.15f, 0.08f, 0.96f); // dark wood, matches the main menu's popups
        RectTransform panelRect = panel.GetComponent<RectTransform>();
        panelRect.anchoredPosition = Vector2.zero;
        panelRect.sizeDelta = new Vector2(1000, 620);

        // Headline message.
        CreateText(panel.transform, "TitleText", message,
            new Vector2(0, 215), new Vector2(920, 150), 48, FontStyle.Bold, TextAnchor.MiddleCenter);

        // This session's result - filled in at runtime by SessionScoreTracker.
        CreateText(panel.transform, "ScoreText", "",
            new Vector2(0, 95), new Vector2(920, 90), 36, FontStyle.Bold, TextAnchor.MiddleCenter);

        // Records read back OUT of the database - the "display retrieved data"
        // half of the Activity 5 requirement.
        CreateText(panel.transform, "RecordsText", "",
            new Vector2(0, -110), new Vector2(920, 300), 28, FontStyle.Normal, TextAnchor.UpperCenter);

        canvasGO.SetActive(false); // SessionScoreTracker/ExitDoorTrigger turn this on when the player finishes
        return canvasGO;
    }

    private static Text CreateText(Transform parent, string name, string content,
        Vector2 anchoredPos, Vector2 size, int fontSize, FontStyle style, TextAnchor alignment)
    {
        GameObject go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        Text text = go.AddComponent<Text>();
        text.text = content;
        text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        text.fontSize = fontSize;
        text.fontStyle = style;
        text.alignment = alignment;
        text.color = Color.white;
        text.horizontalOverflow = HorizontalWrapMode.Wrap;
        text.verticalOverflow = VerticalWrapMode.Overflow;
        text.raycastTarget = false;
        RectTransform rect = go.GetComponent<RectTransform>();
        rect.anchoredPosition = anchoredPos;
        rect.sizeDelta = size;
        return text;
    }

    /// <summary>Finds every GameObject with this name in the open scene, including INACTIVE ones (unlike GameObject.Find).</summary>
    private static List<GameObject> FindAllInScene(string name)
    {
        List<GameObject> matches = new List<GameObject>();
        foreach (GameObject root in SceneManager.GetActiveScene().GetRootGameObjects())
        {
            CollectByName(root.transform, name, matches);
        }
        return matches;
    }

    private static void CollectByName(Transform t, string name, List<GameObject> matches)
    {
        if (t.name == name) matches.Add(t.gameObject);
        for (int i = 0; i < t.childCount; i++)
        {
            CollectByName(t.GetChild(i), name, matches);
        }
    }
}
