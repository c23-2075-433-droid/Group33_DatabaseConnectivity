using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

/// <summary>
/// Wires up the Main Menu screen: nickname entry, Play/Settings/About buttons.
/// Attached to the menu's Canvas by BuildMainMenuScene.cs.
/// </summary>
public class MainMenuController : MonoBehaviour
{
    [Header("Nickname Dialog")]
    [Tooltip("The 'Enter Nickname' popup shown when Play is pressed.")]
    public GameObject nicknamePanel;
    public InputField nicknameInput;
    [Tooltip("Confirms the name and starts the game.")]
    public Button nicknameOkayButton;
    [Tooltip("Closes the popup without starting the game.")]
    public Button nicknameCancelButton;
    [Tooltip("Optional. Checks the name is not already in use and offers a free one instead. " +
              "Leave empty and any name is accepted, which is how older scenes behave.")]
    public NicknameAvailability nicknameAvailability;

    [Header("Buttons")]
    public Button playButton;
    public Button settingsButton;
    public Button aboutButton;
    [Tooltip("Top-right speaker icon in the art - toggles mute on/off.")]
    public Button muteButton;

    [Header("Panels")]
    [Tooltip("Shown when Settings is pressed, hidden otherwise.")]
    public GameObject settingsPanel;
    [Tooltip("Shown when About is pressed, hidden otherwise.")]
    public GameObject aboutPanel;
    public Button settingsCloseButton;
    public Button aboutCloseButton;

    [Header("Settings")]
    [Tooltip("Optional master volume slider inside the Settings panel.")]
    public Slider volumeSlider;

    [Tooltip("Name of the scene to load when Play is pressed (the journey map).")]
    public string levelSelectSceneName = "LevelSelect";

    // Volume and mute live in VolumeSettings now, because the in-game settings
    // panel changes the same two values and the keys cannot be private to this
    // screen any more.

    void Start()
    {
        // Pre-fill with the last-used nickname, if any, so returning players
        // don't have to retype it every time.
        if (nicknameInput != null)
        {
            nicknameInput.text = NicknameManager.Nickname;
        }

        if (playButton != null) playButton.onClick.AddListener(OnPlayPressed);
        if (settingsButton != null) settingsButton.onClick.AddListener(OnSettingsPressed);
        if (aboutButton != null) aboutButton.onClick.AddListener(OnAboutPressed);
        if (settingsCloseButton != null) settingsCloseButton.onClick.AddListener(() => settingsPanel.SetActive(false));
        if (aboutCloseButton != null) aboutCloseButton.onClick.AddListener(() => aboutPanel.SetActive(false));

        if (nicknameOkayButton != null) nicknameOkayButton.onClick.AddListener(OnNicknameConfirmed);
        if (nicknameCancelButton != null) nicknameCancelButton.onClick.AddListener(CloseNicknameDialog);
        // Enter also confirms. onEndEdit fires on focus loss too, so only treat
        // it as a submit when Enter was actually pressed.
        if (nicknameInput != null) nicknameInput.onEndEdit.AddListener(OnNicknameEndEdit);

        VolumeSettings.Apply();

        if (volumeSlider != null)
        {
            volumeSlider.SetValueWithoutNotify(VolumeSettings.Master);
            volumeSlider.onValueChanged.AddListener(OnVolumeChanged);
        }

        if (settingsPanel != null) settingsPanel.SetActive(false);
        if (aboutPanel != null) aboutPanel.SetActive(false);
        if (nicknamePanel != null) nicknamePanel.SetActive(false);

        if (muteButton != null) muteButton.onClick.AddListener(OnMutePressed);
    }

    /// <summary>
    /// Mute no longer has to stash the volume before silencing it: muting and
    /// the master level are two separate saved values now, so unmuting simply
    /// stops forcing silence and the level is still whatever it was.
    /// </summary>
    private void OnMutePressed()
    {
        VolumeSettings.Muted = !VolumeSettings.Muted;
        RefreshVolumeSlider();
    }

    /// <summary>Shows the saved level without re-triggering OnVolumeChanged.</summary>
    private void RefreshVolumeSlider()
    {
        if (volumeSlider == null) return;
        volumeSlider.SetValueWithoutNotify(VolumeSettings.Muted ? 0f : VolumeSettings.Master);
    }

    /// <summary>
    /// Play no longer starts the game directly - it opens the nickname dialog
    /// first, so each play session is saved under a real player name instead
    /// of NicknameManager's fallback. OKAY starts the game; CANCEL backs out.
    /// </summary>
    private void OnPlayPressed()
    {
        if (nicknamePanel == null)
        {
            // No dialog wired up (e.g. an older scene) - keep the original
            // behaviour rather than trapping the player on the menu.
            StartGame();
            return;
        }

        nicknamePanel.SetActive(true);
        if (settingsPanel != null) settingsPanel.SetActive(false);
        if (aboutPanel != null) aboutPanel.SetActive(false);
        // Otherwise a reopened dialog still shows the last "already taken"
        // line, about a name the child may since have changed.
        if (nicknameAvailability != null) nicknameAvailability.ResetStatus();

        if (nicknameInput != null)
        {
            nicknameInput.Select();
            nicknameInput.ActivateInputField();
        }
    }

    private void OnNicknameEndEdit(string value)
    {
        if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter))
        {
            OnNicknameConfirmed();
        }
    }

    /// <summary>
    /// OKAY no longer starts the game outright. The name goes to
    /// NicknameAvailability first, which calls back only once it is settled -
    /// so a taken name leaves the dialog open with a free one filled in, and
    /// the child presses OKAY again. With no checker wired, this is the
    /// straight-through behaviour it always had.
    /// </summary>
    private void OnNicknameConfirmed()
    {
        if (nicknameAvailability == null)
        {
            StartGame();
            return;
        }

        nicknameAvailability.Check(nicknameInput != null ? nicknameInput.text : "", StartGame);
    }

    private void CloseNicknameDialog()
    {
        if (nicknamePanel != null) nicknamePanel.SetActive(false);
    }

    private void StartGame()
    {
        // Blank input is fine - NicknameManager falls back to a default name.
        string nickname = nicknameInput != null ? nicknameInput.text : "";
        NicknameManager.SetNickname(nickname);

        // A fresh run teaches every word from scratch. Without this, someone
        // who played a level, came back to the menu and started again would
        // only ever get the short reminders.
        VocabularyMemory.Reset();

        SceneManager.LoadScene(levelSelectSceneName);
    }

    private void OnSettingsPressed()
    {
        if (settingsPanel != null) settingsPanel.SetActive(true);
        if (aboutPanel != null) aboutPanel.SetActive(false);
    }

    private void OnAboutPressed()
    {
        if (aboutPanel != null) aboutPanel.SetActive(true);
        if (settingsPanel != null) settingsPanel.SetActive(false);
    }

    /// <summary>
    /// Moving the slider also lifts mute - reaching for the volume while the
    /// game is silenced means "I want to hear it", and leaving mute on would
    /// make the slider look broken.
    /// </summary>
    private void OnVolumeChanged(float value)
    {
        if (VolumeSettings.Muted) VolumeSettings.Muted = false;
        VolumeSettings.Master = value;
    }
}
