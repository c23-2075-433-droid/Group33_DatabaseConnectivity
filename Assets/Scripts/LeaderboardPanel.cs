using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// The LEADERBOARDS board: every registered SALINLAHI player on one list,
/// best first.
///
/// The board is drawn with six slots and the design asks for the top ten plus
/// this child's own place. Six does not hold eleven, so the list runs past the
/// six painted rows on a scroll bar in the board's right-hand margin, and the
/// rows can be swiped too - see SlotListScroller. The board opens at the top,
/// where first place is, and a child who is not in the ten finds their own row
/// on the end of the list, picked out in gold like every other row of theirs.
///
/// The rank shown is always the real one, worked out from the whole table
/// rather than from the page: a player off the end of the top ten is ranked by
/// the server. See LeaderboardClient.
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
    [Tooltip("The wooden X in the board's top right corner. A tap outside works " +
              "too, but a child should be able to see the way out.")]
    public Button closeButton;

    [Header("Slots")]
    [Tooltip("The six painted rows, top to bottom. A window onto the fetched list.")]
    public Text[] rowLabels;
    [Tooltip("Runs the list past the six painted rows. Its bar hides itself " +
              "while there are six players or fewer.")]
    public SlotListScroller scroller;

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

    /// <summary>The list as it is to be read down, which is the fetched rows
    /// and, when this child is not among them, their own row after them.</summary>
    private readonly List<LeaderboardEntry> listed = new List<LeaderboardEntry>();

    /// <summary>A line to put after the last row, or null. It is what a player
    /// with no row yet is told instead of being shown a blank one.</summary>
    private string tail;

    void Start()
    {
        if (scrim != null) scrim.onClick.AddListener(Close);
        if (closeButton != null) closeButton.onClick.AddListener(Close);
        if (scroller != null) scroller.onScrolled = Paint;
        if (root != null) root.SetActive(false);
    }

    public void Open()
    {
        if (menuBlur != null) menuBlur.SetBlurred(true);
        if (root != null) root.SetActive(true);

        Empty();
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

            Empty();
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

    /// <summary>Works out what there is to read, then paints the first six of
    /// it. Scrolling after this only repaints.</summary>
    private void Fill(List<LeaderboardEntry> rows, int yourRank)
    {
        if (rows == null || rows.Count == 0)
        {
            Notice(0, "No one has played yet.");
            Notice(1, "Finish a level to be the first!");
            return;
        }

        bool youAreListed = false;
        foreach (LeaderboardEntry row in rows)
        {
            listed.Add(row);
            if (row.isYou) youAreListed = true;
        }

        if (!youAreListed)
        {
            // A row is kept for whoever is holding the tablet, wherever they
            // are on the table: on the end of what was fetched, at their real
            // rank. The totals are this device's, which are what was last sent
            // up, so the row reads the same as the one on the server.
            PlayerProfile you = PlayerProfileStore.Active;
            if (yourRank > 0)
            {
                listed.Add(new LeaderboardEntry
                {
                    nickname = you.DisplayName(),
                    total_points = you.totalPoints,
                    total_stars = you.TotalStars(),
                    rank = yourRank,
                    isYou = true,
                });
            }
            else
            {
                tail = "Finish a level to join the board!";
            }
        }

        if (scroller != null)
        {
            scroller.SetTotal(listed.Count + (tail != null ? 1 : 0));
            scroller.SetFirst(0);       // first place is what the board opens on
        }
        Paint();
    }

    /// <summary>Writes the six slots from wherever the list is scrolled to.</summary>
    private void Paint()
    {
        if (rowLabels == null) return;

        Clear();
        int first = scroller != null ? scroller.First : 0;
        for (int slot = 0; slot < rowLabels.Length; slot++)
        {
            int index = first + slot;
            if (index < listed.Count)
            {
                LeaderboardEntry entry = listed[index];
                Write(slot, entry.rank, entry.nickname, entry.total_points,
                      entry.total_stars, entry.isYou);
            }
            else if (index == listed.Count && tail != null)
            {
                Notice(slot, tail);
            }
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

    /// <summary>Back to a board with nothing on it and nothing to scroll:
    /// what "Loading..." and the offline line are written onto.</summary>
    private void Empty()
    {
        listed.Clear();
        tail = null;
        if (scroller != null) scroller.SetTotal(0);
        Clear();
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
