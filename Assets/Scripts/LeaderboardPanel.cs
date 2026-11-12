using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// The LEADERBOARDS board: every registered SALINLAHI player on one list,
/// best first.
///
/// The board is drawn with six slots, and the design asks for the top ten plus
/// this child's own place. Six will not hold both, so the rule is the one
/// every leaderboard this size uses: the top five, and then you. A child
/// already inside the top six simply sees six places and no repeat of
/// themselves. The top ten is still what gets fetched, so the rank shown is
/// the real one and not a rank within the page.
///
/// Reading is a plain select on the leaderboard table - the ordering, points
/// descending then stars, is the database's. Nothing is written from here:
/// see LeaderboardClient for why a score goes through an Edge Function.
///
/// Offline is an ordinary state, not an error. The board says so in a line a
/// child can read and the menu carries on.
/// </summary>
public class LeaderboardPanel : MonoBehaviour
{
    [Header("Board")]
    [Tooltip("The whole modal, scrim included. Starts inactive.")]
    public GameObject root;
    [Tooltip("The full-screen catcher behind the board. Tapping it closes the board.")]
    public Button scrim;
    [Tooltip("The OKAY plate under the board. A tap outside works too, but a child " +
              "should be able to see the way out.")]
    public Button okayButton;

    [Header("Slots")]
    [Tooltip("The six painted rows, top to bottom.")]
    public Text[] rowLabels;

    [Header("Wiring")]
    public LeaderboardClient client;
    [Tooltip("Optional. Throws the menu out of focus while the board is up.")]
    public MenuBlurBackdrop menuBlur;

    [Header("Look")]
    public Color rowColor = new Color(1f, 0.92f, 0.78f);
    [Tooltip("The row belonging to whoever is playing.")]
    public Color youColor = new Color(1f, 0.82f, 0.35f);
    [Tooltip("\"Loading...\", \"No one has played yet\", and the offline line.")]
    public Color noticeColor = new Color(0.82f, 0.70f, 0.54f);

    void Start()
    {
        if (scrim != null) scrim.onClick.AddListener(Close);
        if (okayButton != null) okayButton.onClick.AddListener(Close);
        if (root != null) root.SetActive(false);
    }

    public void Open()
    {
        if (menuBlur != null) menuBlur.SetBlurred(true);
        if (root != null) root.SetActive(true);

        Clear();
        Notice(0, "Loading...");

        if (client == null || !client.IsConfigured)
        {
            Notice(0, "The leaderboard is not set up on this device.");
            return;
        }

        client.Fetch((rows, yourRank, error) =>
        {
            // The board can be closed while the call is still out.
            if (root == null || !root.activeSelf) return;

            Clear();
            if (error != null)
            {
                Notice(0, "No connection.");
                Notice(1, "Your progress is safe on this device");
                Notice(2, "and will be sent when you are online.");
                return;
            }
            Fill(rows, yourRank);
        });
    }

    public void Close()
    {
        if (root != null) root.SetActive(false);
        if (menuBlur != null) menuBlur.SetBlurred(false);
    }

    private void Fill(List<LeaderboardEntry> rows, int yourRank)
    {
        if (rows == null || rows.Count == 0)
        {
            Notice(0, "No one has played yet.");
            Notice(1, "Finish a level to be the first!");
            return;
        }

        int slots = rowLabels != null ? rowLabels.Length : 0;
        if (slots == 0) return;

        PlayerProfile you = PlayerProfileStore.Active;

        // Is this child already on the page that fits? Then the board is just
        // the top of the list, with their row picked out.
        bool youAreShown = false;
        for (int i = 0; i < rows.Count && i < slots; i++)
        {
            if (rows[i].isYou) youAreShown = true;
        }

        int listed = youAreShown ? slots : slots - 1;
        int row = 0;
        for (int i = 0; i < rows.Count && row < listed; i++, row++)
        {
            Write(row, rows[i].rank, rows[i].nickname, rows[i].total_points,
                  rows[i].total_stars, rows[i].isYou);
        }

        if (youAreShown) return;

        // A slot is kept for whoever is holding the phone, wherever they are
        // on the list: the last one on a full board, and otherwise straight
        // after the players above them rather than below a gap. A player with
        // no row yet is told so rather than shown a blank line.
        int yourSlot = Mathf.Min(row, slots - 1);
        if (yourRank > 0)
        {
            Write(yourSlot, yourRank, you.DisplayName(), you.totalPoints, you.TotalStars(), true);
        }
        else
        {
            Notice(yourSlot, "Finish a level to join the board!");
        }
    }

    private void Write(int slot, int rank, string nickname, int points, int stars, bool isYou)
    {
        if (rowLabels == null || slot < 0 || slot >= rowLabels.Length) return;
        Text label = rowLabels[slot];
        if (label == null) return;

        string name = string.IsNullOrWhiteSpace(nickname) ? NicknameManager.DefaultNickname : nickname;
        label.text = rank + ".  " + name.ToUpperInvariant() + "   " + points + " PTS   " + stars + " *";
        label.color = isYou ? youColor : rowColor;
    }

    private void Notice(int slot, string message)
    {
        if (rowLabels == null || slot < 0 || slot >= rowLabels.Length) return;
        if (rowLabels[slot] == null) return;
        rowLabels[slot].text = message;
        rowLabels[slot].color = noticeColor;
    }

    private void Clear()
    {
        if (rowLabels == null) return;
        foreach (Text label in rowLabels)
        {
            if (label != null) label.text = "";
        }
    }
}
