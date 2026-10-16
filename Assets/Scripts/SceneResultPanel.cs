using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// The summary shown when a scene is finished, instead of dropping the child
/// straight into the next one.
///
/// Every scene used to hand on the moment its last word was said - the screen
/// faded and the next room appeared, with nothing to say how it went. A child
/// who said every word perfectly and one who needed six tries at each saw the
/// same thing: the next room.
///
/// The score here is this SCENE's, not the level's. RunScore adds up across
/// all of a level's scenes so the saved database record covers the whole run,
/// which is right for the record and wrong for this panel - reading it would
/// show the same growing total at the end of every scene, and the stars would
/// only ever climb. So this counts its own.
///
/// Built by SceneResultPanelBuilder, which wires the references below.
/// </summary>
public class SceneResultPanel : MonoBehaviour
{
    [Header("Sources")]
    [Tooltip("Finishing this is what opens the panel.")]
    public SceneObjectiveController objectiveController;

    [Tooltip("Where the attempts are counted from. If empty, one is looked up.")]
    public VoiceCommand voiceCommand;

    [Header("Panel")]
    [Tooltip("Switched on when the scene is finished. Starts off.")]
    public GameObject panelRoot;

    public Text titleText;

    [Tooltip("This scene's result, e.g. \"5 / 6 tama\".")]
    public Text scoreText;

    /// <summary>
    /// One of the three stars. Each carries its own pair of sprites because
    /// the trio is a drawn arrangement, not one star repeated - the outer two
    /// are smaller and lean away from the middle, so they cannot share art.
    /// </summary>
    [System.Serializable]
    public class StarSlot
    {
        public Image image;
        public Sprite earned;
        public Sprite empty;
    }

    [Header("Stars")]
    [Tooltip("Three slots, left to right.")]
    public StarSlot[] stars;

    [Header("Buttons")]
    public Button nextButton;
    public Button replayButton;
    public Button homeButton;

    [Header("Where the buttons go")]
    [Tooltip("Scene the Next button loads. Empty means this scene ends the " +
             "level, and Next goes back to the level select instead.")]
    public string nextSceneName = "";

    public string levelSelectSceneName = "LevelSelect";

    [Header("Rating")]
    [Tooltip("Share of attempts that must be correct for three stars.")]
    [Range(0f, 1f)] public float threeStarAccuracy = 0.9f;

    [Tooltip("...and for two. Below this earns one, which is the floor: the " +
             "scene was finished, so it is never worth none.")]
    [Range(0f, 1f)] public float twoStarAccuracy = 0.7f;

    [Header("Timing")]
    [Tooltip("Beat between the last word landing and the panel arriving, so " +
             "the child sees the thing they just did finish happening.")]
    public float showDelay = 0.9f;

    private int correct;
    private int attempts;
    private bool shown;

    void Awake()
    {
        if (voiceCommand == null) voiceCommand = FindFirstObjectByType<VoiceCommand>();
        if (objectiveController == null) objectiveController = FindFirstObjectByType<SceneObjectiveController>();
        // Never the object this component is on. A deactivated GameObject runs
        // no coroutines and its components hear no events, so switching itself
        // off would cost it the listener that is supposed to switch it back on
        // - the panel would simply never appear. The builder puts the hidden
        // content in a child for this reason; this is here so a hand-wired
        // scene says so loudly instead of failing in silence.
        if (panelRoot == gameObject)
        {
            Debug.LogError("[SALINLAHI] SceneResultPanel.panelRoot points at its own " +
                           "GameObject. It would switch itself off and never hear the " +
                           "scene finish. Point it at a child instead.");
            panelRoot = null;
        }

        if (panelRoot != null) panelRoot.SetActive(false);
    }

    void OnEnable()
    {
        if (voiceCommand != null) voiceCommand.OnAnswerChecked += HandleAnswerChecked;
        if (objectiveController != null)
            objectiveController.onAllObjectivesComplete.AddListener(HandleSceneComplete);
    }

    void OnDisable()
    {
        if (voiceCommand != null) voiceCommand.OnAnswerChecked -= HandleAnswerChecked;
        if (objectiveController != null)
            objectiveController.onAllObjectivesComplete.RemoveListener(HandleSceneComplete);
    }

    private void HandleAnswerChecked(string spokenText, bool wasCorrect)
    {
        attempts++;
        if (wasCorrect) correct++;
    }

    private void HandleSceneComplete()
    {
        // The objective sequence can re-fire if a scene replays its last step;
        // the panel should not stack up or restart its delay.
        if (shown) return;
        shown = true;
        StartCoroutine(ShowAfterBeat());
    }

    private IEnumerator ShowAfterBeat()
    {
        if (showDelay > 0f) yield return new WaitForSeconds(showDelay);

        if (titleText != null && string.IsNullOrEmpty(titleText.text))
            titleText.text = "TAPOS NA!";

        if (scoreText != null)
            scoreText.text = correct + " / " + Mathf.Max(attempts, correct) + " tama";

        ShowStars(EarnedStars());
        WireButtons();

        if (panelRoot != null) panelRoot.SetActive(true);
    }

    /// <summary>How many of the three stars this scene earned. Never zero.</summary>
    public int EarnedStars()
    {
        if (attempts <= 0) return 3;        // finished without a single miss counted
        float accuracy = correct / (float)attempts;
        if (accuracy >= threeStarAccuracy) return 3;
        if (accuracy >= twoStarAccuracy) return 2;
        return 1;
    }

    private void ShowStars(int earned)
    {
        if (stars == null) return;
        for (int i = 0; i < stars.Length; i++)
        {
            StarSlot slot = stars[i];
            if (slot == null || slot.image == null) continue;

            Sprite wanted = i < earned ? slot.earned : slot.empty;
            if (wanted != null) slot.image.sprite = wanted;

            // A slot with no art for the state it is in is hidden rather than
            // left showing the other one - a full star where an empty one
            // belongs would claim a star that was not earned.
            slot.image.gameObject.SetActive(wanted != null);
        }
    }

    private void WireButtons()
    {
        if (nextButton != null)
        {
            string target = string.IsNullOrEmpty(nextSceneName) ? levelSelectSceneName : nextSceneName;
            nextButton.onClick.RemoveAllListeners();
            nextButton.onClick.AddListener(() => Go(target));
        }

        if (replayButton != null)
        {
            replayButton.onClick.RemoveAllListeners();
            replayButton.onClick.AddListener(() => Go(SceneManager.GetActiveScene().name));
        }

        if (homeButton != null)
        {
            homeButton.onClick.RemoveAllListeners();
            homeButton.onClick.AddListener(() => Go(levelSelectSceneName));
        }
    }

    private void Go(string sceneName)
    {
        if (string.IsNullOrEmpty(sceneName)) return;

        // Through the fader, which is also what marks this scene complete on
        // the level select - so the next tile opens whichever button is used,
        // including Replay and Home. The child did finish the scene; which
        // button they pressed afterwards should not decide that.
        SceneFader fader = Object.FindFirstObjectByType<SceneFader>();
        if (fader != null) fader.FadeOutAndLoad(sceneName);
        else SceneManager.LoadScene(sceneName);
    }
}
