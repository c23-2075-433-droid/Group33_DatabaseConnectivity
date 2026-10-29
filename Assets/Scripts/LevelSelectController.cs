using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Drives the level select: one page per place, flipped through with the
/// arrows either side of the board.
///
/// The screen used to show all sixteen scenes at once, numbered straight
/// through. A page per place numbers them the way a child would say it -
/// Bahay one to six, then Paaralan one to five - and leaves the tiles big
/// enough for a small finger. The dots under the board say how many places
/// there are and which one is open.
///
/// A tile can be entered once the player has reached its step AND the scene
/// behind it has been built. Anything else wears the padlock: from the
/// child's side "not yet" and "not made" are the same answer.
///
/// Under each tile are the stars it was last finished with, read back from
/// LevelProgress - the one thing on this screen that rewards going back to a
/// scene that is already open.
///
/// Built by BuildLevelSelectScene.cs, which lays the pages out and fills in
/// the references below.
/// </summary>
public class LevelSelectController : MonoBehaviour
{
    [System.Serializable]
    public class StepNode
    {
        public Button button;

        [Tooltip("The tile itself - the numbered wood, or the padlock.")]
        public Image icon;

        [Tooltip("The tile printed with this scene's number within its place.")]
        public Sprite unlockedSprite;

        [Tooltip("The padlock tile, shown when the scene cannot be entered.")]
        public Sprite lockedSprite;

        [Tooltip("The three stars under the tile, left to right.")]
        public Image[] stars;

        [Tooltip("Index into JourneyMap.Steps.")]
        public int stepIndex;

        [Tooltip("Scene to load. Empty for a place with nothing built yet.")]
        public string sceneName;
    }

    [System.Serializable]
    public class Page
    {
        [Tooltip("Holds this page's tiles. Only the open page is active.")]
        public GameObject root;

        [Tooltip("This page's dot in the strip under the board.")]
        public Image dot;

        [Tooltip("The place this page shows. Used in the console line only.")]
        public string label;

        [Tooltip("First step of this place in JourneyMap, marker included. " +
                 "The screen opens on the last page the player has reached.")]
        public int firstStep;
    }

    [Tooltip("Scene loaded by the Back button.")]
    public string mainMenuSceneName = "MainMenu";

    public Button backButton;

    [Tooltip("The arrows either side of the board.")]
    public Button previousPageButton;
    public Button nextPageButton;

    public StepNode[] nodes;
    public Page[] pages;

    [Header("Stars")]
    public Sprite starOnSprite;
    public Sprite starOffSprite;

    [Header("Page dots")]
    public Sprite dotOnSprite;
    public Sprite dotOffSprite;

    private int openPage;

    void Start()
    {
        if (backButton != null) backButton.onClick.AddListener(OnBackPressed);
        if (previousPageButton != null) previousPageButton.onClick.AddListener(() => Turn(-1));
        if (nextPageButton != null) nextPageButton.onClick.AddListener(() => Turn(1));

        foreach (StepNode node in nodes)
        {
            if (node == null) continue;

            bool reached = LevelProgress.IsStepUnlocked(node.stepIndex);
            bool playable = reached && !string.IsNullOrEmpty(node.sceneName);

            if (node.icon != null)
            {
                Sprite face = playable ? node.unlockedSprite : node.lockedSprite;
                if (face != null) node.icon.sprite = face;
            }

            ShowStars(node);

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

        // Open where the child left off rather than at the beginning, so
        // carrying on is the thing that needs no taps.
        ShowPage(FurthestPage());
    }

    /// <summary>Lights as many stars as this scene has been finished with.</summary>
    private void ShowStars(StepNode node)
    {
        if (node.stars == null) return;

        // The summary panel rates out of five and these tiles hold three, so
        // the rating is scaled rather than read straight.
        int earned = LevelProgress.StarsScaledTo(node.sceneName, node.stars.Length);
        for (int i = 0; i < node.stars.Length; i++)
        {
            Image star = node.stars[i];
            if (star == null) continue;
            Sprite face = i < earned ? starOnSprite : starOffSprite;
            if (face != null) star.sprite = face;
        }
    }

    /// <summary>
    /// The last page the player has reached. Pages are places in journey
    /// order, so this is the last one that begins at or before the furthest
    /// step unlocked.
    /// </summary>
    private int FurthestPage()
    {
        if (pages == null || pages.Length == 0) return 0;

        int reached = LevelProgress.HighestUnlockedStep;
        int best = 0;
        for (int i = 0; i < pages.Length; i++)
            if (pages[i] != null && pages[i].firstStep <= reached) best = i;
        return best;
    }

    private void Turn(int direction) => ShowPage(openPage + direction);

    private void ShowPage(int index)
    {
        if (pages == null || pages.Length == 0) return;
        openPage = Mathf.Clamp(index, 0, pages.Length - 1);

        for (int i = 0; i < pages.Length; i++)
        {
            Page page = pages[i];
            if (page == null) continue;
            if (page.root != null) page.root.SetActive(i == openPage);
            if (page.dot != null)
            {
                Sprite face = i == openPage ? dotOnSprite : dotOffSprite;
                if (face != null) page.dot.sprite = face;
            }
        }

        // Dimmed rather than hidden at the ends: an arrow that disappears
        // moves everything else, and a child who was aiming at it has to look
        // for where the screen went.
        if (previousPageButton != null) previousPageButton.interactable = openPage > 0;
        if (nextPageButton != null) nextPageButton.interactable = openPage < pages.Length - 1;
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
