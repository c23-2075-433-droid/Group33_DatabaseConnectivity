using System;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// The "WHO ARE YOU?" board, opened by CHANGE PROFILE.
///
/// The board's art has three slots and a "(CREATE NEW USER)" row painted into
/// it. Three rows is how much is on screen at once, not how many children can
/// share the tablet: the list runs past those three rows on a scroll bar in the
/// board's right-hand margin, and the rows can be swiped as well, so a class
/// going one at a time all fit. PlayerProfileStore.MaxProfiles is the real cap.
/// The widgets here are laid over the painted rows and plates rather than
/// drawn: see ProfileUIBuilder.
///
/// Everything this class counts in - chosen, and what DELETE and RENAME act on -
/// is an index into the whole list of profiles, never into the three slots. The
/// slots are a window onto it, and SlotListScroller.First says where that window
/// starts; slot i is showing profile First + i.
///
/// Renaming is the thing this screen is really for, and the one rule that
/// matters is that it changes a name and nothing else. Points, stars, levels
/// and words hang off the profile's UUID (PlayerProfileStore), so none of them
/// notice.
///
/// DELETE throws away everything a child has played, and the art has no
/// "are you sure?" board to put in front of it. So it asks in place: the first
/// press turns the row into a question and only the second press acts.
/// </summary>
public class ProfilePickerPanel : MonoBehaviour
{
    [Header("Board")]
    [Tooltip("The whole modal, scrim included. Starts inactive.")]
    public GameObject root;
    [Tooltip("The full-screen catcher behind the board. Tapping it backs out, like CANCEL.")]
    public Button scrim;

    [Header("Slots")]
    [Tooltip("The three painted rows, top to bottom. A window onto the profile list.")]
    public Text[] slotLabels;
    [Tooltip("Invisible rects over the same three rows.")]
    public Button[] slotButtons;
    [Tooltip("The '(CREATE NEW USER)' row. Its words are painted on, so there is no label here.")]
    public Button createButton;
    [Tooltip("Runs the profile list past the three painted rows. Its bar hides " +
              "itself while there are three profiles or fewer.")]
    public SlotListScroller scroller;

    [Header("Plates")]
    public Button renameButton;
    public Button deleteButton;
    public Button okayButton;
    public Button cancelButton;

    [Header("Wiring")]
    [Tooltip("Where the nickname is typed. The same dialog the game asks a new player with - " +
              "there is deliberately only one nickname screen in this project.")]
    public MainMenuController menu;
    [Tooltip("Optional. Throws the menu out of focus while the board is up.")]
    public MenuBlurBackdrop menuBlur;

    [Header("Look")]
    [Tooltip("An occupied row that is not the chosen one.")]
    public Color nameColor = new Color(1f, 0.92f, 0.78f);
    [Tooltip("The chosen row.")]
    public Color chosenColor = new Color(1f, 0.82f, 0.35f);
    [Tooltip("An empty slot, and the DELETE question.")]
    public Color quietColor = new Color(0.78f, 0.64f, 0.48f);

    private Action onClosed;
    private int chosen;
    private bool deleteArmed;

    void Start()
    {
        if (scrim != null) scrim.onClick.AddListener(Cancel);
        if (cancelButton != null) cancelButton.onClick.AddListener(Cancel);
        if (okayButton != null) okayButton.onClick.AddListener(Accept);
        if (renameButton != null) renameButton.onClick.AddListener(RenameChosen);
        if (deleteButton != null) deleteButton.onClick.AddListener(DeleteChosen);
        if (createButton != null) createButton.onClick.AddListener(CreateNew);

        // Scrolling does not change who is chosen, only which three of them are
        // on the board, so it is a repaint and nothing more.
        if (scroller != null) scroller.onScrolled = Refresh;

        if (slotButtons != null)
        {
            for (int i = 0; i < slotButtons.Length; i++)
            {
                if (slotButtons[i] == null) continue;
                int slot = i;                        // captured per row, not per loop
                slotButtons[i].onClick.AddListener(() => Choose(slot));
            }
        }

        if (root != null) root.SetActive(false);
    }

    /// <summary>Shows the board. <paramref name="whenClosed"/> runs however it is
    /// closed, so the holder can repaint itself with whoever is playing now.</summary>
    public void Open(Action whenClosed)
    {
        onClosed = whenClosed;
        deleteArmed = false;
        chosen = IndexOfActive();
        if (menuBlur != null) menuBlur.SetBlurred(true);
        if (root != null) root.SetActive(true);
        // Opens on whoever is playing rather than at the top of the list: with
        // ten profiles the child holding the tablet may be well down it.
        ShowChosen();
    }

    private static int IndexOfActive()
    {
        string active = PlayerProfileStore.Active.playerId;
        for (int i = 0; i < PlayerProfileStore.All.Count; i++)
        {
            if (PlayerProfileStore.All[i].playerId == active) return i;
        }
        return 0;
    }

    /// <summary>The profile showing in the top slot. Nothing without a scroll
    /// bar wired up can scroll, so it is showing the top of the list.</summary>
    private int FirstShown => scroller != null ? scroller.First : 0;

    /// <summary>Scrolls the chosen profile into view and repaints. Used after
    /// anything that moves it - opening the board, creating, deleting.</summary>
    private void ShowChosen()
    {
        if (scroller != null)
        {
            scroller.SetTotal(PlayerProfileStore.All.Count);
            scroller.Reveal(chosen);
        }
        Refresh();
    }

    private void Refresh()
    {
        int count = PlayerProfileStore.All.Count;
        if (chosen >= count) chosen = Mathf.Max(0, count - 1);
        if (scroller != null) scroller.SetTotal(count);

        int first = FirstShown;
        if (slotLabels != null)
        {
            for (int slot = 0; slot < slotLabels.Length; slot++)
            {
                if (slotLabels[slot] == null) continue;
                int index = first + slot;
                if (index >= count)
                {
                    slotLabels[slot].text = "- - -";
                    slotLabels[slot].color = quietColor;
                    continue;
                }
                if (deleteArmed && index == chosen)
                {
                    slotLabels[slot].text = "DELETE? PRESS AGAIN";
                    slotLabels[slot].color = quietColor;
                    continue;
                }
                slotLabels[slot].text = PlayerProfileStore.All[index].DisplayName().ToUpperInvariant();
                slotLabels[slot].color = index == chosen ? chosenColor : nameColor;
            }
        }

        bool real = chosen < count;
        if (renameButton != null) renameButton.interactable = real;
        if (deleteButton != null) deleteButton.interactable = real;
        if (okayButton != null) okayButton.interactable = real;
        // A board with no scroll bar wired up can only ever show its painted
        // rows, so on one it will not make a profile it could not then reach.
        // That is the state a scene saved before the bar existed is in, and a
        // profile nobody can select is worse than one they cannot make.
        bool canReach = scroller != null
                        || slotLabels == null
                        || count < slotLabels.Length;
        if (createButton != null)
            createButton.interactable = PlayerProfileStore.CanCreate && canReach;
    }

    /// <summary>A press on one of the three painted rows. The row is a slot, so
    /// which profile it is depends on how far the list has been scrolled.</summary>
    private void Choose(int slot)
    {
        // Touching anything else takes the finger off the trigger.
        deleteArmed = false;
        int index = FirstShown + slot;
        // An empty row below the end of a short list is scenery, not a choice:
        // leave the child on the profile they already had.
        if (index < PlayerProfileStore.All.Count) chosen = index;
        Refresh();
    }

    private void CreateNew()
    {
        if (!PlayerProfileStore.CanCreate) return;
        deleteArmed = false;
        Ask("", typed =>
        {
            PlayerProfileStore.Create(typed);           // also makes it the playing profile
            chosen = PlayerProfileStore.All.Count - 1;
            // The new profile is on the end of the list, which with ten of them
            // is off the bottom of the board. Scroll down to it, or the press
            // on CREATE looks as though it did nothing.
            ShowChosen();
        });
    }

    private void RenameChosen()
    {
        if (chosen >= PlayerProfileStore.All.Count) return;
        deleteArmed = false;
        PlayerProfile profile = PlayerProfileStore.All[chosen];
        Ask(profile.nickname, typed =>
        {
            // A rename, not a new profile: everything this child has earned
            // stays exactly where it is.
            PlayerProfileStore.Rename(profile, typed);
            Refresh();
        });
    }

    /// <summary>
    /// Opens the nickname dialog, hiding this board while it is up - they are
    /// both modals, and two boards stacked read as one broken one.
    /// </summary>
    private void Ask(string initial, Action<string> accepted)
    {
        if (menu == null) return;
        if (root != null) root.SetActive(false);
        menu.AskForNickname(initial,
            typed =>
            {
                Reappear();
                accepted(typed);
            },
            Reappear);
    }

    /// <summary>
    /// Brings the board back once the nickname dialog is done with. The blur
    /// goes back on because closing that dialog took it off, and the menu
    /// behind this board should still be out of focus.
    /// </summary>
    private void Reappear()
    {
        if (root != null) root.SetActive(true);
        if (menuBlur != null) menuBlur.SetBlurred(true);
        Refresh();
    }

    private void DeleteChosen()
    {
        if (chosen >= PlayerProfileStore.All.Count) return;

        if (!deleteArmed)
        {
            deleteArmed = true;
            Refresh();
            return;
        }

        deleteArmed = false;
        PlayerProfileStore.Delete(PlayerProfileStore.All[chosen]);
        chosen = IndexOfActive();
        // The list is a row shorter, so the window may now be hanging off the
        // end of it. ShowChosen pulls it back.
        ShowChosen();
    }

    private void Accept()
    {
        if (chosen < PlayerProfileStore.All.Count)
        {
            PlayerProfileStore.SetActive(PlayerProfileStore.All[chosen]);
        }
        CloseBoard();
    }

    private void Cancel() => CloseBoard();

    private void CloseBoard()
    {
        deleteArmed = false;
        if (root != null) root.SetActive(false);
        if (menuBlur != null) menuBlur.SetBlurred(false);
        onClosed?.Invoke();
    }
}
