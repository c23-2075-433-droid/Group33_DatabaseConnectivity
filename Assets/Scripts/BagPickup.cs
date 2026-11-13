using System.Collections;
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// The bag half of Bahay Scene 4's last word: it brings the bag out when the
/// word is asked, and it keeps the bag on the floor after the word is said
/// until the player actually picks it up.
///
/// WHY THIS IS SEPARATE FROM SAYING THE WORD. Every other word in this game
/// is answered and done: say "Medyas" and the socks are on. The bag is the
/// one thing the child is meant to DO something with, and until now saying
/// "Bag" made it vanish mid-sentence - the word and the action were the same
/// instant, so there was nothing to watch and nothing to reach for. Here the
/// word only arms the pickup. PronunciationChecker and VoiceCommand are not
/// involved past that point and nothing in this file touches them.
///
/// Four things happen, in order:
///
///   Appear()            the bag is put on the floor beside Kylo. Wired to
///                       the objective's onShown, so it arrives WITH the
///                       word rather than sitting in the room all level.
///   Arm()               wired to onCorrect. The bag gleams and becomes
///                       tappable; nothing else has happened to it yet.
///   StartBagPickup()    the player tapped it (or the wait ran out). This is
///                       the one to call from a button, a test, or anything
///                       else that should start the pickup.
///   OnBagGrabbed()      the hand reaches the bag. The bag goes, Kylo's art
///                       changes to the set with it on his back.
///
/// THE ANIMATION DOES NOT EXIST YET, AND THIS DOES NOT WAIT FOR ONE THAT
/// NEVER COMES. If an Animator is wired up AND it really has the trigger
/// named below, the trigger is set and the bag is left alone until an
/// Animation Event calls OnBagGrabbed - put that event on the frame where
/// his hand closes on the strap. Both halves are checked, so wiring an
/// Animator that has no such parameter does not strand the level; and even
/// with the parameter, grabTimeout ends the wait if the event is missing
/// from the clip. Without an Animator at all it simply waits
/// fallbackGrabDelay and grabs. Writing the animation later means adding the
/// clip and its event, and changing nothing here.
///
/// The sequence holds PlayerMovement.SetPosing(true) from the moment it is
/// armed until it has finished. That is the flag SceneObjectiveController
/// already waits on before it asks the next word or ends the scene, so the
/// results panel cannot open over a bag still sitting on the floor. Its
/// maxActionWait has to be longer than the wait for a tap, or that cap -
/// not the pickup - decides when the scene moves on.
/// </summary>
public class BagPickup : MonoBehaviour
{
    [Header("What is picked up")]
    [Tooltip("The bag prop in the room. Hidden at Awake, shown by Appear().")]
    public GameObject bag;

    [Tooltip("The tappable area. A trigger collider on the bag, so tapping it " +
             "cannot push the player around.")]
    public Collider2D tapArea;

    [Tooltip("Does the actual taking: hides the prop and swaps Kylo's art for " +
             "the set with the bag on his back. The same component every other " +
             "word in this scene uses.")]
    public PickUpItem pickUp;

    [Header("Who picks it up")]
    [Tooltip("Held 'posing' for the length of the sequence, which is what stops " +
             "the scene moving on. Found in the scene if left empty.")]
    public PlayerMovement player;

    [Tooltip("Optional, and there is no such animation yet. If this has the " +
             "trigger below, the bag waits for an Animation Event calling " +
             "OnBagGrabbed instead of a timer.")]
    public Animator animator;

    [Tooltip("The trigger to set. Nothing happens unless the Animator really " +
             "has a trigger parameter with this name.")]
    public string animatorTrigger = "PickUpBag";

    [Header("Catching the eye")]
    [Tooltip("Optional gleam played when the bag becomes tappable.")]
    public TimedOverlay gleam;

    [Header("Timing")]
    [Tooltip("Extra world units around the collider that still count as a tap. " +
             "Children miss.")]
    public float tapPadding = 0.4f;

    [Tooltip("How long to wait for that tap before picking the bag up anyway. " +
             "A five-year-old who does not realise they are being asked to tap " +
             "must not be able to dead-end the level. 0 waits for ever.")]
    public float waitForTapSeconds = 6f;

    [Tooltip("With no animation, how long the grab is pretended to take.")]
    public float fallbackGrabDelay = 0.4f;

    [Tooltip("Longest wait for an Animation Event before grabbing anyway, so a " +
             "clip missing its event cannot strand the level.")]
    public float grabTimeout = 2.5f;

    [Tooltip("A beat after the bag is in his hand, before the scene moves on.")]
    public float settleAfterGrab = 0.35f;

    [Header("Events")]
    [Tooltip("After the bag is his and the sequence has finished.")]
    public UnityEvent onPickedUp;

    /// <summary>True from Arm() until the pickup has finished.</summary>
    public bool IsBusy { get; private set; }

    /// <summary>True while the bag is tappable and nothing has started yet.</summary>
    public bool IsWaitingForTap { get; private set; }

    /// <summary>True once the bag has actually been taken.</summary>
    public bool HasBeenTaken { get; private set; }

    private Camera eye;
    private float waited;
    private bool grabbed;

    void Awake()
    {
        // Hidden from the first frame, not hidden later: the bag belongs to
        // the last word, and a backpack standing in the room through the
        // first three words is a thing to say "Bag" at before being asked.
        if (bag != null) bag.SetActive(false);
        if (player == null) player = FindAnyObjectByType<PlayerMovement>();
    }

    /// <summary>
    /// Puts the bag on the floor. Wired to the Bag objective's onShown, so it
    /// appears as the word is introduced. Safe to call more than once - the
    /// objective is shown again whenever a transition releases the sequence.
    /// </summary>
    public void Appear()
    {
        if (HasBeenTaken) return;
        if (bag != null && !bag.activeSelf) bag.SetActive(true);
    }

    /// <summary>
    /// The word has been said correctly. The bag gleams, becomes tappable,
    /// and the scene is held here until it has been picked up.
    /// </summary>
    public void Arm()
    {
        if (HasBeenTaken || IsBusy) return;

        Appear();
        IsBusy = true;
        IsWaitingForTap = true;
        waited = 0f;

        if (player != null) player.SetPosing(true);
        if (gleam != null) gleam.Play();

        Debug.Log("BagPickup: armed - waiting for the player to pick the bag up.");
    }

    /// <summary>
    /// Starts the pickup. Called by a tap on the bag, by the wait running out,
    /// or by anything else that should begin it - a button, a test, a later
    /// cutscene. Does nothing unless the bag is armed and waiting.
    /// </summary>
    public void StartBagPickup()
    {
        if (!IsWaitingForTap) return;
        IsWaitingForTap = false;
        StartCoroutine(Run());
    }

    /// <summary>
    /// The moment his hand closes on the bag. Put this on an Animation Event
    /// in the pickup clip when that clip exists; until then the coroutine
    /// below calls it on a timer.
    /// </summary>
    public void OnBagGrabbed()
    {
        if (grabbed || HasBeenTaken) return;
        grabbed = true;

        HasBeenTaken = true;
        if (pickUp != null) pickUp.PickUp();        // hides the bag, dresses him
        else if (bag != null) bag.SetActive(false); // nothing wired: still go

        Debug.Log("BagPickup: bag taken.");
    }

    private IEnumerator Run()
    {
        bool animated = HasTrigger();
        if (animated)
        {
            animator.SetTrigger(animatorTrigger);

            float waitedForEvent = 0f;
            while (!grabbed && waitedForEvent < grabTimeout)
            {
                waitedForEvent += Time.deltaTime;
                yield return null;
            }
            if (!grabbed)
                Debug.LogWarning("BagPickup: '" + animatorTrigger + "' played but no " +
                                 "Animation Event called OnBagGrabbed within " +
                                 grabTimeout + "s - taking the bag anyway.");
        }
        else if (fallbackGrabDelay > 0f)
        {
            yield return new WaitForSeconds(fallbackGrabDelay);
        }

        OnBagGrabbed();

        if (settleAfterGrab > 0f) yield return new WaitForSeconds(settleAfterGrab);

        // Released last. Until this, SceneObjectiveController is still waiting.
        if (player != null) player.SetPosing(false);
        IsBusy = false;

        onPickedUp?.Invoke();
    }

    /// <summary>
    /// Whether there is really an animation to wait for. An Animator wired up
    /// in hope, with no such parameter, must not hold the level: SetTrigger
    /// on a name the controller does not have does nothing at all, and the
    /// wait for an Animation Event that can never fire would then be the only
    /// thing happening.
    /// </summary>
    private bool HasTrigger()
    {
        if (animator == null || !animator.isActiveAndEnabled) return false;
        if (animator.runtimeAnimatorController == null) return false;
        if (string.IsNullOrEmpty(animatorTrigger)) return false;

        foreach (AnimatorControllerParameter p in animator.parameters)
            if (p.type == AnimatorControllerParameterType.Trigger && p.name == animatorTrigger)
                return true;
        return false;
    }

    void Update()
    {
        if (!IsWaitingForTap) return;

        if (TappedTheBag()) { StartBagPickup(); return; }

        if (waitForTapSeconds <= 0f) return;
        waited += Time.deltaTime;
        if (waited >= waitForTapSeconds)
        {
            Debug.Log("BagPickup: nobody tapped the bag, picking it up so the " +
                      "level can carry on.");
            StartBagPickup();
        }
    }

    /// <summary>
    /// A tap or click that landed on the bag. Reads the old Input Manager,
    /// which is what this project is set to, and where a touch arrives as
    /// mouse button 0.
    /// </summary>
    private bool TappedTheBag()
    {
        if (!Input.GetMouseButtonDown(0)) return false;
        if (tapArea == null) return false;

        if (eye == null) eye = Camera.main;
        if (eye == null) return false;

        Vector3 p = eye.ScreenToWorldPoint(Input.mousePosition);
        Vector2 point = new Vector2(p.x, p.y);
        if (tapArea.OverlapPoint(point)) return true;

        // Padded, because the thing being aimed at is small and the finger
        // doing the aiming belongs to a five-year-old.
        return tapPadding > 0f &&
               tapArea.bounds.SqrDistance(new Vector3(point.x, point.y, tapArea.bounds.center.z))
                   <= tapPadding * tapPadding;
    }
}
