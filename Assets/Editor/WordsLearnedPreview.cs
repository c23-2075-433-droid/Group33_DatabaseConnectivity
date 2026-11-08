// Assets/Editor/WordsLearnedPreview.cs
//
// Tools > SALINLAHI > Preview Words Learned Board
//
// Puts the WORDS LEARNED board on screen with sample words in it, so the
// lettering can be looked at without playing a level to the end.
//
// It exists because the board is invisible in a saved scene twice over: the
// panel is switched off, and the five rows are empty until WordsLearnedPanel
// fills them at run time. Opening a level scene and hunting for it in the
// hierarchy shows a blank board.
//
// The board is built by WordsLearnedPanelBuilder - the same call the real
// scenes are built with - so what this shows is what the game draws, not a
// mock-up of it.
//
// It builds into a NEW UNTITLED SCENE, loaded additively. Nothing is written
// to any level scene and nothing of yours is closed or saved. When you are
// done, right-click "WordsLearned_Preview" in the Hierarchy and Remove Scene,
// or just use the menu item again - it replaces the old preview.
//
// The words are the awkward ones on purpose: the longest Filipino word in the
// game and the three glosses long enough to make the auto-fit shrink them.

using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public static class WordsLearnedPreview
{
    private const string SceneName = "WordsLearned_Preview";

    // Worst cases first, so anything that clips does it where you can see it.
    // "Hello / how are you" is the longest gloss GlossFor can return.
    private static readonly string[,] Samples =
    {
        { "KUMUSTA", "HELLO / HOW ARE YOU" },
        { "DAMIT",   "CLOTHES / UNIFORM" },
        { "ILAW",    "LIGHT (TURN ON)" },
        { "BANGON",  "GET UP" },
        { "LAKAD",   "WALK" },
    };

    [MenuItem("Tools/SALINLAHI/Preview Words Learned Board")]
    public static void Preview()
    {
        // Replace an earlier preview rather than stacking them up.
        for (int i = SceneManager.sceneCount - 1; i >= 0; i--)
        {
            Scene open = SceneManager.GetSceneAt(i);
            if (open.name == SceneName)
                EditorSceneManager.CloseScene(open, true);
        }

        Scene preview = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
        preview.name = SceneName;

        // The board is a Screen Space - Overlay canvas and would draw without
        // one, but the Game view says "No cameras rendering" and shows nothing
        // else, which reads as a broken preview.
        GameObject camGO = new GameObject("Preview Camera");
        SceneManager.MoveGameObjectToScene(camGO, preview);
        Camera cam = camGO.AddComponent<Camera>();
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = new Color(0.17f, 0.17f, 0.17f);
        cam.orthographic = true;

        // Same canvas the summary builds, so the board is scaled exactly as it
        // is in game. See SceneResultPanelBuilder.
        GameObject canvasGO = new GameObject("WordsLearned_PreviewCanvas");
        SceneManager.MoveGameObjectToScene(canvasGO, preview);
        Canvas canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 50;
        CanvasScaler scaler = canvasGO.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.Expand;
        canvasGO.AddComponent<GraphicRaycaster>();

        // null controller: nothing collects words here, they are written in
        // below. Build leaves the panel switched off, as it does in a scene.
        WordsLearnedPanel panel = WordsLearnedPanelBuilder.Build(canvasGO.transform, null);

        for (int i = 0; i < panel.rows.Length; i++)
        {
            WordsLearnedPanel.Row row = panel.rows[i];
            if (row == null) continue;

            bool used = i < Samples.GetLength(0);
            if (row.root != null) row.root.SetActive(used);
            if (!used) continue;

            if (row.word != null) row.word.text = Samples[i, 0];
            if (row.meaning != null) row.meaning.text = Samples[i, 1];
        }

        if (panel.panelRoot != null) panel.panelRoot.SetActive(true);

        Selection.activeGameObject = canvasGO;
        Debug.Log("[SALINLAHI] WORDS LEARNED preview built in an untitled additive scene. " +
                  "Look at the GAME view, not the Scene view - the board is a Screen Space " +
                  "Overlay canvas and only lays out properly there. Remove Scene when done; " +
                  "nothing was written to any level scene.");
    }
}
