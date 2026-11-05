using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Drives the two screens behind ADVENTURE: first a board per place, then the
/// levels inside the place that was picked.
///
/// This used to be one screen with arrows either side of the board, flipped
/// through place by place. A child who wanted the park had to know it was
/// three presses to the right. Showing the places first makes that one press
/// and gives each one a picture to recognise it by, which matters more than
/// compactness for a reader who is six.
///
/// A place is open once the player has reached its first step. A locked one
/// keeps its board but wears the stone face and the padlock, and cannot be
/// pressed - same answer the tiles give, in the same language.
///
/// A tile can be entered once the player has reached its step AND the scene
/// behind it has been built. Under each are the stars it was last finished
/// with, read back from LevelProgress - the one thing on this screen that
/// rewards going back to a scene that is already open.
///
/// Built by BuildLevelSelectScene.cs, which lays both screens out and fills in
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

    /// <summary>
    /// One place: its board on the picker, and the page of tiles it opens.
    /// </summary>
    [System.Serializable]
    public class Place
    {
        [Tooltip("Holds this place's tiles on the levels screen. Only the open one is active.")]
        public GameObject tilesRoot;

        [Tooltip("The board on the picker. Switched off when the place is locked.")]
        public Button boardButton;

        [Tooltip("The board's own art - wood when open, stone with a padlock when not.")]
        public Image boardImage;
        public Sprite openBoard;
        [Tooltip("Leave empty for a place that is never locked, such as the first one.")]
        public Sprite lockedBoard;

        [Tooltip("The picture inside the board - the house, the school, the park.")]
        public Image illustration;
        public Sprite openIllustration;
        public Sprite lockedIllustration;

        [Tooltip("The banner over the levels screen: HOME LEVELS, SCHOOL LEVELS...")]
        public Sprite gridTitle;

        [Tooltip("The place's name. Used in the console line only.")]
        public string label;

        [Tooltip("First step of this place in JourneyMap, marker included. " +
                 "The place is open once the player has reached it.")]
        public int firstStep;
    }

    [Tooltip("Scene loaded by the notebook button, and by Back from the picker.")]
    public string mainMenuSceneName = "MainMenu";

    [Tooltip("Up one screen: levels to places, places to the main menu.")]
    public Button backButton;
    [Tooltip("Straight out to the main menu, from either screen.")]
    public Button menuButton;

    [Header("Screens")]
    [Tooltip("The place picker - YOUR ADVENTURE AWAITS.")]
    public GameObject pickerRoot;
    [Tooltip("The levels of whichever place was picked.")]
    public GameObject gridRoot;
    [Tooltip("The banner over the levels screen. Its sprite is the place's own.")]
    public Image gridTitleImage;

    public StepNode[] nodes;
    public Place[] places;

    [Header("Stars")]
    public Sprite starOnSprite;
    public Sprite starOffSprite;

    void Start()
    {
        if (backButton != null) backButton.onClick.AddListener(OnBackPressed);
        if (menuButton != null) menuButton.onClick.AddListener(GoToMainMenu);

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
                    node.button.onClick.AddListener(() => Go(scene));
                }
            }
        }

        for (int i = 0; i < (places != null ? places.Length : 0); i++)
        {
            Place place = places[i];
            if (place == null) continue;

            bool open = LevelProgress.IsStepUnlocked(place.firstStep);
            ShowPlace(place, open);

            if (place.boardButton != null)
            {
                place.boardButton.interactable = open;
                if (open)
                {
                    int index = i;
                    place.boardButton.onClick.AddListener(() => OpenPlace(index));
                }
            }
        }

        ShowPicker();
    }

    /// <summary>
    /// Dresses one board as open or locked. The padlock is painted into the
    /// locked board, and the picture inside it has a grey twin of its own, so
    /// a locked place is drawn rather than tinted - a greyed-out button says
    /// "broken" to a child where a padlock says "not yet".
    /// </summary>
    private void ShowPlace(Place place, bool open)
    {
        if (place.boardImage != null)
        {
            Sprite face = open ? place.openBoard : place.lockedBoard;
            if (face != null) place.boardImage.sprite = face;
        }
        if (place.illustration != null)
        {
            Sprite picture = open ? place.openIllustration : place.lockedIllustration;
            if (picture != null) place.illustration.sprite = picture;
        }
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

    private void ShowPicker()
    {
        if (pickerRoot != null) pickerRoot.SetActive(true);
        if (gridRoot != null) gridRoot.SetActive(false);
    }

    /// <summary>
    /// Swaps to the levels of one place: its banner over the board, its tiles
    /// on it, everyone else's put away.
    /// </summary>
    private void OpenPlace(int index)
    {
        if (places == null || index < 0 || index >= places.Length) return;

        for (int i = 0; i < places.Length; i++)
        {
            Place place = places[i];
            if (place != null && place.tilesRoot != null) place.tilesRoot.SetActive(i == index);
        }

        if (gridTitleImage != null && places[index].gridTitle != null)
            gridTitleImage.sprite = places[index].gridTitle;

        if (pickerRoot != null) pickerRoot.SetActive(false);
        if (gridRoot != null) gridRoot.SetActive(true);
    }

    /// <summary>
    /// Back goes up one screen, not straight out: from the levels to the
    /// places, and only from the places to the main menu. A child who opened
    /// the wrong place should land back where they chose it.
    /// </summary>
    private void OnBackPressed()
    {
        if (gridRoot != null && gridRoot.activeSelf)
        {
            ShowPicker();
            return;
        }
        GoToMainMenu();
    }

    private void GoToMainMenu() => Go(mainMenuSceneName);

    private void Go(string sceneName)
    {
        if (string.IsNullOrEmpty(sceneName)) return;

        SceneFader fader = Object.FindAnyObjectByType<SceneFader>();
        if (fader != null) fader.FadeOutAndLoad(sceneName);
        else SceneManager.LoadScene(sceneName);
    }
}
