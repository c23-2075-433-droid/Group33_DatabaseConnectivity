using System;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

/// <summary>
/// Wires up the Main Menu screen: nickname entry, Adventure/Mini Game/
/// Settings/About buttons. Attached to the menu's Canvas by
/// BuildMainMenuScene.cs.
///
/// The nickname is asked for once, on the first launch after the game is
/// installed, and never again - a returning child's profile is loaded and
/// ADVENTURE takes them straight to the journey. Changing it afterwards is
/// the nickname holder's job, through CHANGE PROFILE (PlayerProfileMenu).
///
/// This screen still owns the one nickname dialog there is. AskForNickname()
/// is how anything else borrows it, which is why there is no second name-entry
/// screen anywhere in the project.
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
    [Tooltip("Optional. Throws the menu out of focus while the nickname dialog is open. " +
              "Leave empty and the menu simply stays sharp behind it.")]
    public MenuBlurBackdrop menuBlur;

    [Header("Buttons")]
    [Tooltip("ADVENTURE - the story levels. Opens the journey map straight away; " +
              "the nickname is asked for once, on the first launch after install.")]
    public Button adventureButton;
    [Tooltip("MINI GAME - drawn and clickable, but deliberately does nothing yet. " +
              "There are no mini games to open, and a button that loads a stub " +
              "screen would be a worse lie than one that sits still.")]
    public Button miniGamesButton;
    public Button settingsButton;
    public Button aboutButton;

    [Header("Player")]
    [Tooltip("The nickname holder in the top-right corner. Repainted after a first-run " +
              "name is given. Leave empty and the menu simply has no holder.")]
    public PlayerProfileMenu profileMenu;

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
        if (adventureButton != null) adventureButton.onClick.AddListener(OnAdventurePressed);
        // miniGamesButton is left unwired on purpose - see its tooltip.
        if (settingsButton != null) settingsButton.onClick.AddListener(OnSettingsPressed);
        if (aboutButton != null) aboutButton.onClick.AddListener(OnAboutPressed);
        if (settingsCloseButton != null) settingsCloseButton.onClick.AddListener(() => settingsPanel.SetActive(false));
        if (aboutCloseButton != null) aboutCloseButton.onClick.AddListener(() => aboutPanel.SetActive(false));

        if (nicknameOkayButton != null) nicknameOkayButton.onClick.AddListener(OnNicknameConfirmed);
        if (nicknameCancelButton != null) nicknameCancelButton.onClick.AddListener(OnNicknameCancelled);
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

        AskOnFirstRun();
    }

    /// <summary>
    /// A fresh install has no profile with a name on it, so the child is asked
    /// for one here and the answer is saved. Every launch after that finds the
    /// name already there and this does nothing - which is the whole point:
    /// the question is asked once, not every time ADVENTURE is pressed.
    ///
    /// Backing out leaves the profile unnamed, so the question comes back next
    /// launch rather than trapping a child who pressed the wrong plate.
    /// </summary>
    private void AskOnFirstRun()
    {
        if (NicknameManager.HasNickname) return;

        AskForNickname("", typed =>
        {
            NicknameManager.SetNickname(typed);
            if (profileMenu != null) profileMenu.Refresh();
        });
    }

    /// <summary>
    /// Mute no longer has to stash the volume before silencing it: muting and
    /// the master level are two separate saved values now, so unmuting simply
    /// stops forcing silence and the level is still whatever it was.
    ///
    /// It does restore a very low Master, though. The panel this replaced had
    /// a SOUND slider driving Master, so a quiet level can be saved with
    /// nothing left on screen able to raise it - and SOUND on that showed no
    /// effect would look broken. On means audible.
    /// </summary>
    private void OnSoundToggled()
    {
        bool turningOn = VolumeSettings.Muted;
        VolumeSettings.Muted = !VolumeSettings.Muted;
        if (turningOn && VolumeSettings.Master < 0.5f) VolumeSettings.Master = 1f;
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
    /// ADVENTURE starts the game. It used to open the nickname dialog first,
    /// because the name was asked for once per play session; it is asked once
    /// per install now, so there is nothing left to stop for.
    /// </summary>
    private void OnAdventurePressed()
    {
        StartGame();
    }

    // ---- the one nickname dialog ----

    private Action<string> nicknameAccepted;
    private Action nicknameCancelled;

    /// <summary>
    /// Opens the nickname dialog on behalf of whoever asked, and calls back
    /// once the name is settled. The same dialog, the same "already taken"
    /// check and the same plates, whether it is a first-run player being asked
    /// their name or the WHO ARE YOU? board renaming a profile.
    ///
    /// <paramref name="accepted"/> is handed the name that was typed - it does
    /// NOT save it, because what the name is for differs by caller: one makes
    /// a profile, another renames one.
    /// </summary>
    public void AskForNickname(string initial, Action<string> accepted, Action cancelled = null)
    {
        nicknameAccepted = accepted;
        nicknameCancelled = cancelled;

        if (nicknamePanel == null)
        {
            // No dialog in this scene (an older one, say). Better to take the
            // name as it stands than to leave the caller waiting forever.
            Settle(ref nicknameAccepted, initial);
            return;
        }

        if (menuBlur != null) menuBlur.SetBlurred(true);
        nicknamePanel.SetActive(true);
        if (settingsPanel != null) settingsPanel.SetActive(false);
        if (aboutPanel != null) aboutPanel.SetActive(false);
        // Otherwise a reopened dialog still shows the last "already taken"
        // line, about a name the child may since have changed.
        if (nicknameAvailability != null) nicknameAvailability.ResetStatus();

        if (nicknameInput != null)
        {
            nicknameInput.text = initial ?? "";
            nicknameInput.caretPosition = nicknameInput.text.Length;
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
    /// OKAY does not accept the name outright. It goes to NicknameAvailability
    /// first, which calls back only once it is settled - so a taken name
    /// leaves the dialog open with a free one filled in, and the child presses
    /// OKAY again. With no checker wired, this is the straight-through
    /// behaviour it always had.
    /// </summary>
    private void OnNicknameConfirmed()
    {
        if (nicknameAvailability == null)
        {
            AcceptNickname();
            return;
        }

        nicknameAvailability.Check(nicknameInput != null ? nicknameInput.text : "", AcceptNickname);
    }

    private void AcceptNickname()
    {
        string typed = nicknameInput != null ? nicknameInput.text : "";
        CloseNicknameDialog();
        nicknameCancelled = null;
        Settle(ref nicknameAccepted, typed);
    }

    private void OnNicknameCancelled()
    {
        CloseNicknameDialog();
        nicknameAccepted = null;
        Action backedOut = nicknameCancelled;
        nicknameCancelled = null;
        backedOut?.Invoke();
    }

    private void CloseNicknameDialog()
    {
        if (nicknamePanel != null) nicknamePanel.SetActive(false);
        if (menuBlur != null) menuBlur.SetBlurred(false);
    }

    /// <summary>Clears the callback before running it, so a handler that opens
    /// the dialog again is not immediately answered by its own request.</summary>
    private static void Settle(ref Action<string> callback, string value)
    {
        Action<string> settled = callback;
        callback = null;
        settled?.Invoke(value);
    }

    private void StartGame()
    {
        // A fresh run teaches every word from scratch. Without this, someone
        // who played a level, came back to the menu and started again would
        // only ever get the short reminders.
        VocabularyMemory.Reset();

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
