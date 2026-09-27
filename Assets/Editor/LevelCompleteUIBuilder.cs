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

    // Panel art is 1014x793 (~1.28:1); the size below keeps that ratio so the
    // bamboo frame isn't stretched.
    private const float PanelWidth = 1100f;
    private const float PanelHeight = PanelWidth / (1014f / 793f);

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
        panelImg.sprite = LoadSprite("ui_results_panel");
        panelImg.raycastTarget = false;
        RectTransform panelRect = panel.GetComponent<RectTransform>();
        panelRect.anchoredPosition = Vector2.zero;
        panelRect.sizeDelta = new Vector2(PanelWidth, PanelHeight);

        // The bamboo frame eats the outer edges of the sprite, so everything
        // below is laid out inside the measured interior rather than the full
        // panel rect (insets measured from the art: 11.2% / 11.4% / 17.5% / 14.2%).
        float interiorTop = PanelHeight * 0.5f - PanelHeight * 0.175f;
        float interiorBottom = -PanelHeight * 0.5f + PanelHeight * 0.142f;
        float interiorWidth = PanelWidth * (1f - 0.112f - 0.114f);

        // Cream text: the panel interior is mid-brown wood, so dark text would
        // not read against it (same pairing as the menu title).
        Color cream = new Color(0.99f, 0.96f, 0.89f);

        // Headline message.
        CreateText(panel.transform, "TitleText", message,
            new Vector2(0, interiorTop - 62), new Vector2(interiorWidth, 120),
            44, FontStyle.Bold, TextAnchor.MiddleCenter, cream);

        // This session's result - filled in at runtime by SessionScoreTracker.
        CreateText(panel.transform, "ScoreText", "",
            new Vector2(0, interiorTop - 160), new Vector2(interiorWidth, 80),
            34, FontStyle.Bold, TextAnchor.MiddleCenter, cream);

        // Records read back OUT of the database - the "display retrieved data"
        // half of the Activity 5 requirement.
        CreateText(panel.transform, "RecordsText", "",
            new Vector2(0, interiorTop - 340), new Vector2(interiorWidth, 280),
            26, FontStyle.Normal, TextAnchor.UpperCenter, cream);

        // OKAY returns to the journey map, so the player isn't stranded on a
        // dead-end panel (and the demo can loop straight into another run).
        GameObject okayGO = new GameObject("OkayButton", typeof(RectTransform));
        okayGO.transform.SetParent(panel.transform, false);
        Image okayImg = okayGO.AddComponent<Image>();
        okayImg.sprite = LoadSprite("btn_okay");
        RectTransform okayRect = okayGO.GetComponent<RectTransform>();
        okayRect.sizeDelta = new Vector2(360, 360f / (1733f / 407f));
        okayRect.anchoredPosition = new Vector2(0, interiorBottom + 58);
        Button okayButton = okayGO.AddComponent<Button>();
        okayButton.targetGraphic = okayImg;
        ColorBlock colors = okayButton.colors;
        colors.highlightedColor = new Color(1f, 0.97f, 0.9f);
        colors.pressedColor = new Color(0.8f, 0.75f, 0.7f);
        colors.fadeDuration = 0.08f;
        okayButton.colors = colors;
        okayGO.AddComponent<LoadSceneOnClick>().sceneName = "LevelSelect";

        canvasGO.SetActive(false); // SessionScoreTracker/ExitDoorTrigger turn this on when the player finishes
        return canvasGO;
    }

    private static Text CreateText(Transform parent, string name, string content,
        Vector2 anchoredPos, Vector2 size, int fontSize, FontStyle style, TextAnchor alignment, Color color)
    {
        GameObject go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        Text text = go.AddComponent<Text>();
        text.text = content;
        text.font = UIFont.Get();
        text.fontSize = fontSize;
        text.fontStyle = style;
        text.alignment = alignment;
        text.color = color;
        text.horizontalOverflow = HorizontalWrapMode.Wrap;
        text.verticalOverflow = VerticalWrapMode.Overflow;
        text.raycastTarget = false;
        RectTransform rect = go.GetComponent<RectTransform>();
        rect.anchoredPosition = anchoredPos;
        rect.sizeDelta = size;
        return text;
    }

    private static Sprite LoadSprite(string spriteFile)
    {
        string path = "Assets/Sprites/" + spriteFile + ".png";
        Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
        if (sprite == null)
            Debug.LogWarning("[SALINLAHI] Could not load sprite at '" + path + "'. Make sure it's imported as Sprite (2D and UI).");
        return sprite;
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
