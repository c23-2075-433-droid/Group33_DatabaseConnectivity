using System;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Runs a long list past a short row of painted slots.
///
/// Both boards on this menu are drawn complete: the plates the rows sit on are
/// part of the picture, three of them on WHO ARE YOU? and six on LEADERBOARDS.
/// That is a cap on how much is on screen at once, but it does not have to be a
/// cap on how much there is. This holds the index of the entry showing in the
/// top slot, and the panel paints slot i with entry First + i.
///
/// It moves a whole slot at a time, never half of one. A free-scrolling list
/// would slide text off the painted plates and leave a name straddling the gap
/// between two of them, which on art like this reads as a bug rather than as
/// scrolling. So the bar is given numberOfSteps and the window lands on rows.
///
/// The bar hides itself whenever everything fits, which is the ordinary case:
/// one child, one profile, nothing to scroll.
/// </summary>
public class SlotListScroller : MonoBehaviour
{
    [Tooltip("The bar in the board's right-hand margin. Hidden while everything fits.")]
    public Scrollbar scrollbar;

    [Tooltip("How many painted rows the board has to show entries in.")]
    public int visibleSlots = 3;

    /// <summary>Runs when the window moves, however it was moved - the bar, a
    /// swipe or the wheel. The panel repaints its slots. Not serialised: Unity
    /// does not store a delegate, and this is wired in code either way.</summary>
    [NonSerialized] public Action onScrolled;

    private int total;
    private int first;
    private bool writingValue;
    private bool wired;

    /// <summary>The entry showing in the top slot.</summary>
    public int First => first;

    /// <summary>How far down the list the window can go before it runs out.</summary>
    public int Last => Mathf.Max(0, total - Mathf.Max(1, visibleSlots));

    void Awake() => Wire();

    /// <summary>
    /// Listens to the bar. Done on demand as well as in Awake, because a panel
    /// can be handed a count before this component has woken: both boards are
    /// built switched off, and a switched-off object gets no Awake.
    /// </summary>
    private void Wire()
    {
        if (wired || scrollbar == null) return;
        scrollbar.onValueChanged.AddListener(BarMoved);
        wired = true;
    }

    /// <summary>How many entries there are to run past the slots. Keeps the
    /// window where it is if it still fits, and pulls it back if it does not -
    /// deleting the last profile must not leave the board looking empty.</summary>
    public void SetTotal(int count)
    {
        Wire();
        total = Mathf.Max(0, count);
        Move(first, false);
    }

    /// <summary>Puts a given entry in the top slot, as far as the list allows.</summary>
    public void SetFirst(int index) => Move(index, true);

    /// <summary>Moves the window by whole rows - one wheel notch, one swipe.</summary>
    public void Nudge(int rows) => Move(first + rows, true);

    /// <summary>
    /// Moves the window the least it can to bring an entry into view, and not
    /// at all if it is already there. This is what opens the profile board on
    /// whoever is playing rather than always at the top of the list.
    /// </summary>
    public void Reveal(int index)
    {
        int slots = Mathf.Max(1, visibleSlots);
        int want = first;
        if (index < first) want = index;
        else if (index >= first + slots) want = index - slots + 1;
        Move(want, false);
    }

    /// <summary>
    /// The one place the window moves. <paramref name="notify"/> is off when
    /// the caller is about to repaint anyway, so a press on CREATE does not
    /// draw the board twice.
    /// </summary>
    private void Move(int index, bool notify)
    {
        int clamped = Mathf.Clamp(index, 0, Last);
        bool moved = clamped != first;
        first = clamped;
        Apply();
        if (moved && notify) onScrolled?.Invoke();
    }

    /// <summary>Puts the window on the bar: how big the handle is, how many
    /// places it can stop at, and where it sits now.</summary>
    private void Apply()
    {
        if (scrollbar == null) return;

        int slots = Mathf.Max(1, visibleSlots);
        bool needed = total > slots;
        if (scrollbar.gameObject.activeSelf != needed) scrollbar.gameObject.SetActive(needed);
        if (!needed) return;

        // Writing the value raises onValueChanged like a press would, and the
        // answer to it would be the figure that was just written. The flag is
        // how the handler tells the two apart.
        writingValue = true;
        scrollbar.numberOfSteps = Last + 1;
        // A handle that is the window's share of the list, but never so small
        // that a child cannot find it.
        scrollbar.size = Mathf.Clamp((float)slots / Mathf.Max(1, total), 0.12f, 1f);
        scrollbar.value = Last > 0 ? (float)first / Last : 0f;
        writingValue = false;
    }

    private void BarMoved(float value)
    {
        if (writingValue) return;
        int index = Mathf.Clamp(Mathf.RoundToInt(value * Last), 0, Last);
        if (index == first) return;
        first = index;
        onScrolled?.Invoke();
    }
}
