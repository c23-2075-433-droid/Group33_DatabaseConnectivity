using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

/// <summary>
/// Wires up the Main Menu screen: nickname entry, Play/Settings/About buttons.
/// Attached to the menu's Canvas by BuildMainMenuScene.cs.
/// </summary>
public class MainMenuController : MonoBehaviour
{
    [Header("Nickname")]
    public InputField nicknameInput;

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
    private const string VolumePrefsKey = "SALINLAHI_MasterVolume";

    [Tooltip("Name of the scene to load when Play is pressed (the journey map).")]
    public string levelSelectSceneName = "LevelSelect";

    private const string MutedPrefsKey = "SALINLAHI_Muted";
    private bool isMuted = false;
    private float volumeBeforeMute = 1f;

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

        if (volumeSlider != null)
        {
            volumeSlider.value = PlayerPrefs.GetFloat(VolumePrefsKey, 1f);
            volumeSlider.onValueChanged.AddListener(OnVolumeChanged);
            AudioListener.volume = volumeSlider.value;
        }

        if (settingsPanel != null) settingsPanel.SetActive(false);
        if (aboutPanel != null) aboutPanel.SetActive(false);

        isMuted = PlayerPrefs.GetInt(MutedPrefsKey, 0) == 1;
        volumeBeforeMute = AudioListener.volume > 0f ? AudioListener.volume : 1f;
        if (isMuted) AudioListener.volume = 0f;
        if (muteButton != null) muteButton.onClick.AddListener(OnMutePressed);
    }

    private void OnMutePressed()
    {
        isMuted = !isMuted;
        if (isMuted)
        {
            volumeBeforeMute = AudioListener.volume > 0f ? AudioListener.volume : volumeBeforeMute;
            AudioListener.volume = 0f;
        }
        else
        {
            AudioListener.volume = volumeBeforeMute;
        }
        if (volumeSlider != null) volumeSlider.value = AudioListener.volume;
        PlayerPrefs.SetInt(MutedPrefsKey, isMuted ? 1 : 0);
    }

    private void OnPlayPressed()
    {
        string nickname = nicknameInput != null ? nicknameInput.text : "";
        NicknameManager.SetNickname(nickname);
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

    private void OnVolumeChanged(float value)
    {
        AudioListener.volume = value;
        PlayerPrefs.SetFloat(VolumePrefsKey, value);
    }
}
