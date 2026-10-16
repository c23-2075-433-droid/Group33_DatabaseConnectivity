using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Drives "Ang Iyong Paglalakbay": the whole journey as one winding path, a
/// node per step of JourneyMap - each level marker and every scene between
/// them - rather than four level nodes with everything inside them hidden.
///
/// A node is open when the player has reached its step. Scenes that are
/// designed but not built have no scene behind them, so they show as locked
/// however far the player gets; the path still shows where the journey goes.
///
/// Built by BuildLevelSelectScene.cs, which lays the nodes out and fills in
/// the references below.
/// </summary>
public class LevelSelectController : MonoBehaviour
{
    [System.Serializable]
    public class StepNode
    {
        public Button button;

        [Tooltip("The node's own picture - a stepping stone, or a level's illustration.")]
        public Image icon;

        public Sprite unlockedSprite;
        public Sprite lockedSprite;

        [Tooltip("Padlock shown over a step the player has not reached.")]
        public GameObject lockOverlay;

        [Tooltip("The number drawn on the tile. Always shown - a locked tile is " +
                 "dimmed rather than covered, so the journey can be counted " +
                 "end to end whether or not it has been reached.")]
        public GameObject numberLabel;

        [Tooltip("Index into JourneyMap.Steps.")]
        public int stepIndex;

        [Tooltip("Scene to load. Empty for a level marker, or a scene not built yet.")]
        public string sceneName;
    }

    [Tooltip("Scene loaded by the Back button.")]
    public string mainMenuSceneName = "MainMenu";

    public Button backButton;
    public StepNode[] nodes;

    [Tooltip("Kylo's portrait, moved to the furthest step the player has reached.")]
    public RectTransform playerMarker;

    [Tooltip("How far above a node the portrait sits.")]
    public float markerLift = 86f;

    [Tooltip("Number colour on a tile that can be entered.")]
    public Color numberColor = new Color(0.99f, 0.95f, 0.85f);

    [Tooltip("Number colour on a tile that cannot. The button dims the tile " +
             "itself; the number is a child of it, so it has to be dimmed to " +
             "match or it sits bright on a greyed tile.")]
    public Color lockedNumberColor = new Color(0.56f, 0.51f, 0.45f);

    void Start()
    {
        if (backButton != null) backButton.onClick.AddListener(OnBackPressed);

        foreach (StepNode node in nodes)
        {
            if (node == null) continue;

            bool reached = LevelProgress.IsStepUnlocked(node.stepIndex);
            bool playable = reached && !string.IsNullOrEmpty(node.sceneName);

            // The padlock tile stands for "you cannot go in here", which covers
            // both a step the player has not reached and a scene that has not
            // been built. From the child's side those are the same thing, and a
            // numbered tile that does nothing when tapped is worse than a lock.
            if (node.icon != null && node.unlockedSprite != null && node.lockedSprite != null)
                node.icon.sprite = playable ? node.unlockedSprite : node.lockedSprite;

            // Only for a node whose art has no padlock of its own. The wooden
            // tiles carry theirs, so the grid leaves this null and the sprite
            // swap above does the whole job.
            if (node.lockOverlay != null) node.lockOverlay.SetActive(!reached);

            // Every tile keeps its number, so the levels can be counted straight
            // through. A tile that cannot be entered is dimmed instead.
            if (node.numberLabel != null)
            {
                Text numberText = node.numberLabel.GetComponent<Text>();
                if (numberText != null)
                    numberText.color = playable ? numberColor : lockedNumberColor;
            }

            if (node.button != null)
            {
                node.button.interactable = playable;
                if (playable)
                {
                    string scene = node.sceneName;
                    node.button.onClick.AddListener(() => OnNodePressed(scene));
                }
            }
        }

        PlaceMarker();
    }

    /// <summary>Puts Kylo's portrait on the furthest step reached.</summary>
    private void PlaceMarker()
    {
        if (playerMarker == null || nodes == null || nodes.Length == 0) return;

        StepNode here = null;
        foreach (StepNode node in nodes)
        {
            if (node == null || node.button == null) continue;
            if (!LevelProgress.IsStepUnlocked(node.stepIndex)) continue;
            if (here == null || node.stepIndex > here.stepIndex) here = node;
        }
        if (here == null || here.button == null) return;

        RectTransform target = here.button.GetComponent<RectTransform>();
        if (target == null) return;

        playerMarker.SetParent(target.parent, false);
        playerMarker.anchoredPosition = target.anchoredPosition + new Vector2(0f, markerLift);
        playerMarker.SetAsLastSibling();
    }

    private void OnNodePressed(string sceneName)
    {
        if (string.IsNullOrEmpty(sceneName)) return;

        SceneFader fader = Object.FindFirstObjectByType<SceneFader>();
        if (fader != null) fader.FadeOutAndLoad(sceneName);
        else SceneManager.LoadScene(sceneName);
    }

    private void OnBackPressed()
    {
        if (string.IsNullOrEmpty(mainMenuSceneName)) return;

        SceneFader fader = Object.FindFirstObjectByType<SceneFader>();
        if (fader != null) fader.FadeOutAndLoad(mainMenuSceneName);
        else SceneManager.LoadScene(mainMenuSceneName);
    }
}
