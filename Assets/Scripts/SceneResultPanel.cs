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

    [Tooltip("The banner across the top of the board.")]
    public Image titleImage;

    [Tooltip("Shown on any rating above the lowest.")]
    public Sprite completeSprite;

    [Tooltip("Shown on the lowest rating. The scene is still finished and the " +
             "next one still opens - this is a nudge to go again, not a loss.")]
    public Sprite tryAgainSprite;

    [Tooltip("This scene's result, e.g. \"5 / 6\". The board prints the " +
             "SCORE: label itself, so only the figures go here.")]
    public Text scoreText;

    [Tooltip("How long the scene took, e.g. \"1:48\". The board prints TIME:.")]
    public Text timeText;

    /// <summary>
    /// One of the five stars. Each carries its own pair of sprites because the
    /// row is a drawn arrangement, not one star repeated - it swells towards
    /// the middle, so no two slots can share art.
    /// </summary>
    [System.Serializable]
    public class StarSlot
    {
        public Image image;
        public Sprite earned;
        public Sprite empty;
    }

    [Header("Stars")]
    [Tooltip("Five slots, left to right.")]
    public StarSlot[] stars;

    [Header("Buttons")]
    public Button nextButton;
    public Button replayButton;

    [Tooltip("The cross in the corner. Closing a scene that is already " +
             "finished means going back to the map, so it goes where Home did.")]
    public Button closeButton;

    [Header("Where the buttons go")]
    [Tooltip("Scene the Next button loads. Empty means this scene ends the " +
             "level, and Next goes back to the level select instead.")]
    public string nextSceneName = "";

    public string levelSelectSceneName = "LevelSelect";

    [Header("Rating")]
    [Tooltip("Share of attempts that must be correct for the 2nd star, then " +
             "the 3rd, 4th and 5th. Finishing the scene at all earns the " +
             "first, which is the floor: the child did get to the end, so it " +
             "is never worth none.")]
    public float[] starAccuracy = { 0.5f, 0.7f, 0.85f, 0.95f };

    [Header("Level endings")]
    [Tooltip("Tick in a scene that ends a level. The panel then shows nothing " +
             "on its own and waits for SessionScoreTracker to hand it the " +
             "whole level's totals, which are the ones worth showing there.")]
    public bool waitForExternalTotals = false;

    [Tooltip("Tick when something other than the last objective decides the " +
             "scene is over - Level 1's door. The panel then waits to be told " +
             "with Show() rather than opening itself.")]
    public bool waitForCue = false;

    [Header("Timing")]
    [Tooltip("Beat between the last word landing and the panel arriving, so " +
             "the child sees the thing they just did finish happening.")]
    public float showDelay = 0.9f;

    private int correct;
    private int attempts;
    private bool shown;
    private float startedAt;

    void Awake()
    {
        // The clock starts when the scene does. This component lives on the
        // panel's canvas, which is never switched off - only its content is -
        // so Awake here is the scene opening.
        startedAt = Time.time;

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
        // A level's last scene shows the level's totals, not this scene's, and
        // only SessionScoreTracker has them - so there, this does nothing and
        // ShowWithTotals is what opens the panel.
        if (waitForExternalTotals) return;

        // And in a scene whose ending is not its last objective, something
        // else says when. Level 1 is the one: its objectives are Bangon,
        // Tayo and Lakad, all of them done while the child still has to walk
        // out of the room, so opening here would hand them a summary with the
        // door still ahead of them.
        if (waitForCue) return;

        Show();
    }

    /// <summary>
    /// Opens the panel on the tally it has kept itself.
    ///
    /// For a scene that ends somewhere other than its last objective - Level
    /// 1 ends when the child reaches the door, which ExitDoorTrigger knows
    /// about and the objectives do not.
    /// </summary>
    public void Show()
    {
        // The objective sequence can re-fire if a scene replays its last step;
        // the panel should not stack up or restart its delay.
        if (shown) return;
        shown = true;
        StartCoroutine(ShowAfterBeat());
    }

    /// <summary>
    /// Opens the panel on figures worked out elsewhere - the whole level's,
    /// gathered across all of its scenes.
    ///
    /// They have to be handed in rather than read from RunScore here, because
    /// SessionScoreTracker clears RunScore as soon as it has saved the record,
    /// which happens before this panel's opening beat has finished. Reading it
    /// afterwards would show nothing but zeros.
    /// </summary>
    public void ShowWithTotals(int levelCorrect, int levelAttempts)
    {
        if (shown) return;
        shown = true;
        correct = levelCorrect;
        attempts = levelAttempts;
        StartCoroutine(ShowAfterBeat());
    }

    private IEnumerator ShowAfterBeat()
    {
        if (showDelay > 0f) yield return new WaitForSeconds(showDelay);

        int earned = EarnedStars();

        // The lowest rating swaps the banner for TRY AGAIN!. Nothing else
        // changes: the scene is finished, the next one opens, and REPLAY is
        // there for a child who wants another go at it.
        if (titleImage != null)
        {
            Sprite banner = earned <= 1 && tryAgainSprite != null ? tryAgainSprite : completeSprite;
            if (banner != null) titleImage.sprite = banner;
        }

        if (scoreText != null)
            scoreText.text = correct + " / " + Mathf.Max(attempts, correct);

        if (timeText != null) timeText.text = Elapsed();

        // The level select reads these back, so the rating has to outlive the
        // panel. On a level's last scene the figures are the whole level's
        // rather than this scene's, and the rating is saved against the scene
        // the child is standing in either way - that is the tile it appears on.
        LevelProgress.RecordStars(SceneManager.GetActiveScene().name, earned);

        ShowStars(earned);
        WireButtons();

        if (panelRoot != null) panelRoot.SetActive(true);
    }

    /// <summary>How many of the stars this scene earned. Never zero.</summary>
    public int EarnedStars()
    {
        if (attempts <= 0) return LevelProgress.MaxStars;   // finished without a miss counted
        float accuracy = correct / (float)attempts;

        int earned = 1;
        if (starAccuracy != null)
            foreach (float needed in starAccuracy)
                if (accuracy >= needed) earned++;
        return Mathf.Clamp(earned, 1, LevelProgress.MaxStars);
    }

    /// <summary>
    /// How long this scene took, as m:ss.
    ///
    /// Timed from the scene loading rather than from the first word, because
    /// a child who sits and looks around for a while did spend that time in
    /// the scene. On a level's last scene this is still that scene's time and
    /// not the level's, the same way its stars are.
    /// </summary>
    private string Elapsed()
    {
        int seconds = Mathf.Max(0, Mathf.RoundToInt(Time.time - startedAt));
        return (seconds / 60) + ":" + (seconds % 60).ToString("00");
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

        if (closeButton != null)
        {
            closeButton.onClick.RemoveAllListeners();
            closeButton.onClick.AddListener(() => Go(levelSelectSceneName));
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
