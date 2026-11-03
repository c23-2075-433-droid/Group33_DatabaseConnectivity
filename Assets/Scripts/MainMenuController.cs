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

    [Header("Panels")]
    [Tooltip("Shown when Settings is pressed, hidden otherwise.")]
    public GameObject settingsPanel;
    [Tooltip("Shown when About is pressed, hidden otherwise.")]
    public GameObject aboutPanel;
    public Button settingsCloseButton;
    public Button aboutCloseButton;

    [Header("Settings")]
    [Tooltip("SOUND row - on means not muted. The plate art is swapped, not tinted.")]
    public Button soundToggle;
    [Tooltip("MUSIC row. Remembered, but nothing plays music yet.")]
    public Button musicToggle;
    [Tooltip("SOUND EFFECTS row. Remembered, but the game has no effects yet.")]
    public Slider effectsSlider;
    [Tooltip("MS. RACHEL row - the narrator's own volume.")]
    public Slider narrationSlider;

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

        if (effectsSlider != null) effectsSlider.onValueChanged.AddListener(OnEffectsChanged);
        if (narrationSlider != null) narrationSlider.onValueChanged.AddListener(OnNarrationChanged);
        if (soundToggle != null) soundToggle.onClick.AddListener(OnSoundToggled);
        if (musicToggle != null) musicToggle.onClick.AddListener(OnMusicToggled);
        RefreshSettingsWidgets();

        if (settingsPanel != null) settingsPanel.SetActive(false);
        if (aboutPanel != null) aboutPanel.SetActive(false);
        if (nicknamePanel != null) nicknamePanel.SetActive(false);
    }

    /// <summary>
    /// Mute no longer has to stash the volume before silencing it: muting and
    /// the master level are two separate saved values now, so unmuting simply
    /// stops forcing silence and the level is still whatever it was.
    /// </summary>
    private void OnSoundToggled()
    {
        VolumeSettings.Muted = !VolumeSettings.Muted;
        RefreshSettingsWidgets();
    }

    private void OnMusicToggled()
    {
        VolumeSettings.MusicOn = !VolumeSettings.MusicOn;
        RefreshSettingsWidgets();
    }

    /// <summary>
    /// Pushes all four controls from the saved settings, without re-triggering
    /// their own handlers. Called when the popup opens as well as on start,
    /// because the in-game panel writes the same values - a volume changed
    /// mid-game must show here when the player comes back to the menu.
    ///
    /// The toggles hold no state of their own: the saved setting is the only
    /// truth and the plate is derived from it every time.
    /// </summary>
    private void RefreshSettingsWidgets()
    {
        if (effectsSlider != null) effectsSlider.SetValueWithoutNotify(VolumeSettings.Effects);
        if (narrationSlider != null) narrationSlider.SetValueWithoutNotify(VolumeSettings.Narration);

        UITheme theme = UITheme.Load();
        GameplayHUDLayout.SetToggle(soundToggle, theme, !VolumeSettings.Muted);
        GameplayHUDLayout.SetToggle(musicToggle, theme, VolumeSettings.MusicOn);
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
        SceneManager.LoadScene(levelSelectSceneName);
    }

    private void OnSettingsPressed()
    {
        RefreshSettingsWidgets();
        if (settingsPanel != null) settingsPanel.SetActive(true);
        if (aboutPanel != null) aboutPanel.SetActive(false);
    }

    private void OnAboutPressed()
    {
        if (aboutPanel != null) aboutPanel.SetActive(true);
        if (settingsPanel != null) settingsPanel.SetActive(false);
    }

    private void OnEffectsChanged(float value)
    {
        VolumeSettings.Effects = value;
    }

    /// <summary>
    /// Moving a slider no longer lifts mute, which the old volume slider did.
    /// SOUND is now a toggle the player can see one row up, so flipping it
    /// under them would be the surprising thing, not the helpful one.
    /// </summary>
    private void OnNarrationChanged(float value)
    {
        VolumeSettings.Narration = value;
    }
}
