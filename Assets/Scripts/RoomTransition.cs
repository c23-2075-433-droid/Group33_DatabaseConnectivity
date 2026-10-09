using System.Collections;
using UnityEngine;

/// <summary>
/// Moves the player from one room to another INSIDE the same scene, hidden
/// behind a fade to black.
///
/// This is what happens when Kylo walks through the bathroom door. The two
/// rooms sit on top of each other at the same position rather than side by
/// side, so the camera never shows one room while the player is standing in
/// the other — only the active room is ever visible.
///
/// Sequence when EnterRoom() is called:
///   1. the player walks forward a little, into the doorway
///   2. the screen fades to black
///   3. the old room is switched off, the new one on, and the player is
///      placed at the new room's entrance
///   4. the screen fades back in
///
/// Hooked up from the "Bukas" objective, after the open-door sprite is shown.
/// </summary>
public class RoomTransition : MonoBehaviour
{
    [Header("Rooms")]
    [Tooltip("The room being left (hallway). Switched off while the screen is black.")]
    public GameObject fromRoom;

    [Tooltip("The room being entered (bathroom, including its darkness overlay). Switched on while the screen is black.")]
    public GameObject toRoom;

    [Header("Player")]
    [Tooltip("The player object to walk in and reposition.")]
    public Transform player;

    [Tooltip("The player's PlayerMovement, so the walk-into-the-door step uses the existing walk animation.")]
    public PlayerMovement playerMovement;

    [Tooltip("Where the player stands once the new room appears.")]
    public Vector2 arrivalPosition = new Vector2(-5f, -2.98f);

    [Header("Doorway")]
    [Tooltip("World X of the door being walked through. The walk-in step heads " +
             "toward this, so it works wherever the player happens to be standing " +
             "when the word is said.")]
    public float doorX = 7.31f;

    [Header("Timing")]
    [Tooltip("Longest the walk to the door may take, in seconds. The walk now ends " +
             "as soon as the player actually reaches doorX, so this is only a safety " +
             "cap that stops a mispositioned door hanging the transition. 0 skips the " +
             "walk. It used to be a fixed duration, which left Kylo short of the door " +
             "whenever he was further away than this many seconds of walking.")]
    public float walkInDuration = 3.0f;

    [Tooltip("How close to doorX counts as having arrived, in world units.")]
    public float arriveThreshold = 0.35f;

    [Tooltip("Seconds to wait after the door opens before the player starts walking in, so the player sees the door open first.")]
    public float doorPause = 0.6f;

    [Header("Objectives")]
    [Tooltip("Optional. If empty, one is looked up in the scene. The objective " +
             "sequence is held while the transition runs, so the next word is " +
             "not asked - or answered - while Kylo is still walking through the door.")]
    public SceneObjectiveController objectiveController;

    [Header("Fade")]
    [Tooltip("Optional. If empty, one is looked up in the scene. Without a fader the swap is instant.")]
    public SceneFader fader;

    // A second "Bukas" (or a mistimed tap) must not restart the sequence
    // half-way through and leave both rooms switched on.
    private bool hasEntered = false;

    void Awake()
    {
        if (fader == null) fader = FindFirstObjectByType<SceneFader>();
        if (objectiveController == null) objectiveController = FindFirstObjectByType<SceneObjectiveController>();
    }

    /// <summary>
    /// Starts the walk-through-the-door transition. Safe to call more than
    /// once; only the first call does anything.
    /// </summary>
    public void EnterRoom()
    {
        if (hasEntered) return;
        hasEntered = true;

        // Hold the objectives straight away, before anything yields. The word
        // after this one is armed the moment this one is answered, so without
        // this the next word is live for the whole walk and fade and can be
        // answered behind the black screen - which finished the scene early
        // and had the results panel already up on arrival.
        if (objectiveController != null) objectiveController.SetPaused(true);

        StartCoroutine(EnterRoomRoutine());
    }

    private IEnumerator EnterRoomRoutine()
    {
        // Let the player see the door swing open before Kylo moves.
        if (doorPause > 0f) yield return new WaitForSeconds(doorPause);

        // Walk into the doorway, using the same walk the voice commands use.
        // The direction is worked out from where the player actually is, since
        // the word can be said from either side of the door.
        if (playerMovement != null && walkInDuration > 0f)
        {
            float direction = (player != null && player.position.x > doorX) ? -1f : 1f;
            playerMovement.WalkInDirection(direction, walkInDuration);

            // Walk until the doorway is actually reached rather than for a
            // fixed time. "Bukas" can be said from anywhere in the hallway, so
            // the distance left to cover is not known in advance - a fixed
            // duration faded the screen while Kylo was still short of the door.
            float elapsed = 0f;
            while (elapsed < walkInDuration)
            {
                if (player != null && Mathf.Abs(player.position.x - doorX) <= arriveThreshold) break;
                elapsed += Time.deltaTime;
                yield return null;
            }

            // Stop him at the door instead of letting the walk run on behind
            // the fade.
            playerMovement.StopVoiceWalk();
        }

        if (fader != null)
        {
            fader.FadeOutAndThen(SwapRooms, Resume);
        }
        else
        {
            // No fader in the scene: still correct, just without the fade.
            SwapRooms();
            Resume();
        }
    }

    private void Resume()
    {
        if (objectiveController != null) objectiveController.SetPaused(false);
    }

    private void SwapRooms()
    {
        if (fromRoom != null) fromRoom.SetActive(false);
        if (toRoom != null) toRoom.SetActive(true);

        if (player != null)
        {
            player.position = new Vector3(arrivalPosition.x, arrivalPosition.y, player.position.z);
        }
    }
}
