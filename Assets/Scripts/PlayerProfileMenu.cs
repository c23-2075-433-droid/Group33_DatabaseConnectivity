using System;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// The nickname holder in the top-right corner of the main menu, and the two
/// signs that swing down from it when it is tapped.
///
/// The holder shows whoever is playing. Tapping it drops CHANGE PROFILE and
/// LEADERBOARDS out from under it on their ropes; tapping it again, or
/// anywhere else on the menu, puts them away. Nothing else on the menu moves
/// while they are down, which is why the catcher behind them only closes the
/// signs rather than swallowing the press entirely.
///
/// The book in the opposite corner is NOT this. It is decoration - no Button,
/// no raycast - and it stays that way until there is an Almanac behind it.
/// </summary>
public class PlayerProfileMenu : MonoBehaviour
{
    [Header("Holder")]
    [Tooltip("The wooden plaque itself. Tapping it opens and closes the signs.")]
    public Button holderButton;
    [Tooltip("The name painted on the plaque.")]
    public Text nicknameLabel;

    [Header("Signs")]
    [Tooltip("The ropes and the two signs, as one piece. Hidden until the holder is tapped.")]
    public GameObject dropdown;
    [Tooltip("Opens the WHO ARE YOU? board.")]
    public Button changeProfileButton;
    [Tooltip("Opens the global leaderboard.")]
    public Button leaderboardsButton;
    [Tooltip("Full-screen catcher under the signs, so a tap anywhere else puts them away.")]
    public Button outsideCatcher;

    [Header("Boards")]
    public ProfilePickerPanel picker;
    public LeaderboardPanel leaderboard;

    [Header("Leaderboard")]
    [Tooltip("Optional. Sends this child's totals up when the menu opens, if they have " +
              "moved since the last time. Leave empty and the board is read-only.")]
    public LeaderboardClient leaderboardClient;

    void Start()
    {
        if (holderButton != null) holderButton.onClick.AddListener(Toggle);
        if (outsideCatcher != null) outsideCatcher.onClick.AddListener(Close);
        if (changeProfileButton != null) changeProfileButton.onClick.AddListener(OpenPicker);
        if (leaderboardsButton != null) leaderboardsButton.onClick.AddListener(OpenLeaderboard);

        Close();
        Refresh();

        // A run finished on a tablet with no signal still counts; it reaches
        // the board the next time the menu is opened with one. Nothing waits
        // on this and nothing breaks when it fails.
        if (leaderboardClient != null) leaderboardClient.PushIfNeeded();
    }

    /// <summary>Writes the playing child's name onto the plaque. Called after
    /// anything that could have changed who is playing, or what they are called.</summary>
    public void Refresh()
    {
        if (nicknameLabel == null) return;
        nicknameLabel.text = NicknameManager.HasNickname
            ? PlayerProfileStore.Active.nickname.ToUpperInvariant()
            : "";
    }

    public void Toggle()
    {
        if (dropdown == null) return;
        SetOpen(!dropdown.activeSelf);
    }

    public void Close() => SetOpen(false);

    private void SetOpen(bool open)
    {
        if (dropdown != null) dropdown.SetActive(open);
        // The catcher only exists while the signs are down - left on, it would
        // sit over the whole menu and eat every press.
        if (outsideCatcher != null) outsideCatcher.gameObject.SetActive(open);
    }

    private void OpenPicker()
    {
        Close();
        if (picker == null) return;
        picker.Open(() =>
        {
            Refresh();
            // A renamed profile, or a different child now playing, is a
            // different thing for the board to show. Nothing waits on it.
            if (leaderboardClient != null) leaderboardClient.PushIfNeeded();
        });
    }

    private void OpenLeaderboard()
    {
        Close();
        if (leaderboard == null) return;

        // Send before reading, so a child who has just finished a level sees
        // themselves where they now belong rather than where they were.
        if (leaderboardClient != null)
        {
            leaderboardClient.PushIfNeeded((ok, error) => leaderboard.Open());
            return;
        }
        leaderboard.Open();
    }
}
