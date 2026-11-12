using System;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// The "WHO ARE YOU?" board, opened by CHANGE PROFILE.
///
/// The board's art has three slots and a "(CREATE NEW USER)" row painted into
/// it, which is also the cap on how many children can share a device - a limit
/// that matches the drawing is better than one that quietly runs off the
/// bottom of it. The widgets here are laid over the painted rows and plates
/// rather than drawn: see ProfileUIBuilder.
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
    [Tooltip("The three painted rows, top to bottom. One per profile on this device.")]
    public Text[] slotLabels;
    [Tooltip("Invisible rects over the same three rows.")]
    public Button[] slotButtons;
    [Tooltip("The '(CREATE NEW USER)' row. Its words are painted on, so there is no label here.")]
    public Button createButton;

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

        if (slotButtons != null)
        {
            for (int i = 0; i < slotButtons.Length; i++)
            {
                if (slotButtons[i] == null) continue;
                int index = i;                       // captured per row, not per loop
                slotButtons[i].onClick.AddListener(() => Choose(index));
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
        Refresh();
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

    private void Refresh()
    {
        int count = PlayerProfileStore.All.Count;
        if (chosen >= count) chosen = Mathf.Max(0, count - 1);

        if (slotLabels != null)
        {
            for (int i = 0; i < slotLabels.Length; i++)
            {
                if (slotLabels[i] == null) continue;
                if (i >= count)
                {
                    slotLabels[i].text = "- - -";
                    slotLabels[i].color = quietColor;
                    continue;
                }
                if (deleteArmed && i == chosen)
                {
                    slotLabels[i].text = "DELETE? PRESS AGAIN";
                    slotLabels[i].color = quietColor;
                    continue;
                }
                slotLabels[i].text = PlayerProfileStore.All[i].DisplayName().ToUpperInvariant();
                slotLabels[i].color = i == chosen ? chosenColor : nameColor;
            }
        }

        bool real = chosen < count;
        if (renameButton != null) renameButton.interactable = real;
        if (deleteButton != null) deleteButton.interactable = real;
        if (okayButton != null) okayButton.interactable = real;
        if (createButton != null) createButton.interactable = PlayerProfileStore.CanCreate;
    }

    private void Choose(int index)
    {
        // Touching anything else takes the finger off the trigger.
        deleteArmed = false;
        chosen = index;
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
            Refresh();
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
        Refresh();
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
