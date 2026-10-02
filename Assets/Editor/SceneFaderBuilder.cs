// Assets/Editor/SceneFaderBuilder.cs
//
// Adds the full-screen fade overlay (SceneFader) used for scene-to-scene
// transitions. Every scene that is entered or left should have one, so the
// hand-off fades instead of cutting.
//
// The canvas sorts above everything else, including the level-complete panel
// and the voice UI.

using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using System.Collections.Generic;
using UnityEngine.SceneManagement;

public static class SceneFaderBuilder
{
    public const string CanvasName = "SceneFader_Canvas";

    /// <summary>Creates the fader canvas, replacing any existing one.</summary>
    public static SceneFader Build()
    {
        foreach (GameObject stale in FindAllInScene(CanvasName)) Object.DestroyImmediate(stale);

        GameObject canvasGO = new GameObject(CanvasName);
        Canvas canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 999; // above every other canvas in the scene
        CanvasScaler scaler = canvasGO.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.Expand;
        canvasGO.AddComponent<GraphicRaycaster>();

        if (Object.FindFirstObjectByType<EventSystem>() == null)
        {
            GameObject es = new GameObject("EventSystem");
            es.AddComponent<EventSystem>();
            es.AddComponent<StandaloneInputModule>();
        }

        GameObject fadeGO = new GameObject("Fade", typeof(RectTransform));
        fadeGO.transform.SetParent(canvasGO.transform, false);
        Image img = fadeGO.AddComponent<Image>();
        img.color = Color.black;
        RectTransform rect = fadeGO.GetComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;

        SceneFader fader = canvasGO.AddComponent<SceneFader>();
        fader.fadeImage = img;
        return fader;
    }

    private static List<GameObject> FindAllInScene(string name)
    {
        List<GameObject> matches = new List<GameObject>();
        foreach (GameObject root in SceneManager.GetActiveScene().GetRootGameObjects())
            Collect(root.transform, name, matches);
        return matches;
    }

    private static void Collect(Transform t, string name, List<GameObject> matches)
    {
        if (t.name == name) matches.Add(t.gameObject);
        for (int i = 0; i < t.childCount; i++) Collect(t.GetChild(i), name, matches);
    }
}
