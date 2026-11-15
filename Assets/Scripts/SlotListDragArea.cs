using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// Lets the rows themselves be dragged, so the list scrolls the way a child
/// expects it to on a tablet.
///
/// The bar in the margin is thin - the board's art leaves about twenty page
/// pixels between the rows and the frame, and widening it would mean drawing
/// over the plates. That is enough to SHOW where in the list you are, which is
/// what it is really for, and not enough to be a comfortable target for a small
/// finger. So the whole row area is draggable as well, and the wheel works for
/// whoever is at a desk.
///
/// This sits on the rect the rows are parented to rather than on the rows. A
/// drag that starts on one of the profile buttons is not handled by the button -
/// Button has no drag handler - so Unity walks up the hierarchy looking for one
/// and finds this.
/// </summary>
[RequireComponent(typeof(RectTransform))]
public class SlotListDragArea : MonoBehaviour, IBeginDragHandler, IDragHandler, IScrollHandler
{
    [Tooltip("The window this drags.")]
    public SlotListScroller scroller;

    [Tooltip("The distance from one painted row to the next, in canvas units. " +
              "A drag of this much moves the list by one entry.")]
    public float rowPitch = 92.1f;

    private float grabbedAt;
    private int grabbedFirst;

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (scroller == null) return;
        // Where the list was when the finger went down. Measuring every frame
        // against this, rather than adding up per-frame deltas, means the list
        // follows the finger back exactly when a drag is changed its mind about.
        grabbedAt = LocalY(eventData);
        grabbedFirst = scroller.First;
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (scroller == null || rowPitch <= 0f) return;
        // Dragging up pulls later entries into view, like every other list.
        float moved = LocalY(eventData) - grabbedAt;
        scroller.SetFirst(grabbedFirst + Mathf.RoundToInt(moved / rowPitch));
    }

    public void OnScroll(PointerEventData eventData)
    {
        if (scroller == null) return;
        float wheel = eventData.scrollDelta.y;
        if (Mathf.Abs(wheel) < 0.01f) return;
        scroller.Nudge(wheel > 0f ? -1 : 1);
    }

    /// <summary>
    /// The pointer in this rect's own units, so a drag is measured in the same
    /// units the rows are spaced in and the sum works out whatever the screen
    /// the canvas has been scaled onto.
    /// </summary>
    private float LocalY(PointerEventData eventData)
    {
        Vector2 local;
        bool inside = RectTransformUtility.ScreenPointToLocalPointInRectangle(
            (RectTransform)transform, eventData.position, eventData.pressEventCamera, out local);
        return inside ? local.y : grabbedAt;
    }
}
