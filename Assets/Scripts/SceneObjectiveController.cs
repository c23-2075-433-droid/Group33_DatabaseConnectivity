using System.Collections;
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Drives an ordered sequence of spoken-word objectives for a scene (e.g.
/// Level 1's Bangon -> Tayo -> Lakad wake-up sequence). Generalizes what
/// Level1PromptSequencer used to do with a fixed 4-word array: each objective
/// is now DATA (word + accepted variants + prompt UI + instruction audio +
/// what happens when it's answered correctly), so new scenes/levels are built
/// by adding entries in the Inspector, not writing new code or new
/// VoiceCommand fields.
///
/// How it works:
///   1. Shows the current objective's prompt UI and sets it as
///      VoiceCommand.currentTargetWord - VoiceCommand does the actual speech
///      recognition and word-matching (see PronunciationChecker).
///   2. Listens for VoiceCommand.OnAnswerChecked. When the player says the
///      current objective's word correctly, hides its prompt, invokes its
///      onCorrect UnityEvent (wire this to PlayerMovement.Bangon(), a door
///      animation, anything - visible and editable in the Inspector), and
///      advances to the next objective.
///   3. Once every objective is done, clears currentTargetWord (so general,
///      always-on words like "Talon" -> Jump keep working) and fires
///      onAllObjectivesComplete.
/// </summary>
public class SceneObjectiveController : MonoBehaviour
{
    [System.Serializable]
    public class SceneObjective
    {
        [Tooltip("The Filipino word the player must say (e.g. 'Bangon').")]
        public string word;

        [Tooltip("Extra accepted spellings/mishearings for this word (optional).")]
        public string[] variants;

        [Tooltip("The prompt UI GameObject for this objective (badge + arrow + text), shown while it's active.")]
        public GameObject promptRoot;

        [Tooltip("Spoken instruction clip for this word (e.g. a recording of \"Sabihin: " +
                 "Bangon\"). Replayed by the Replay/Speaker button - see VoiceInteractionUI.")]
        public AudioClip instructionClip;

        [Tooltip("Invoked when the player says this word correctly - wire to PlayerMovement.Bangon(), a door-open animation, etc.")]
        public UnityEvent onCorrect = new UnityEvent();
    }

    [Tooltip("The VoiceCommand that recognizes speech for this scene.")]
    public VoiceCommand voiceCommand;

    [Tooltip("Objectives in order, e.g. Bangon, Tayo, Lakad.")]
    public SceneObjective[] objectives;

    [Tooltip("Invoked once every objective in this list has been completed.")]
    // Constructed here, not left to Unity. A component created with
    // AddComponent - which every scene builder does - gets null for a
    // UnityEvent field, and UnityEventTools.AddPersistentListener throws on
    // it. Loading a saved scene replaces this instance, so it costs nothing.
    public UnityEvent onAllObjectivesComplete = new UnityEvent();

    [Header("Pacing")]
    [Tooltip("Finish the action a word started - the walk, the run, the pick-up - " +
             "before the next word is asked or the level is completed. Without this " +
             "the next prompt appears, or the results panel opens, while Kylo is " +
             "still moving, so the result is shown before the action it describes " +
             "has happened.")]
    public bool waitForActionToFinish = true;

    [Tooltip("The character whose movement is waited on. Left empty, one is found " +
             "in the scene.")]
    public PlayerMovement playerMovement;

    [Tooltip("Safety cap in seconds, so an action that never ends cannot stall the " +
             "sequence.")]
    public float maxActionWait = 4f;

    [Tooltip("Beat held after the action finishes, so the next prompt does not appear " +
             "the instant the character stops.")]
    public float settlePause = 0.25f;

    private int currentIndex = -1;
    private AudioSource audioSource;
    private Coroutine advanceRoutine;

    // Set while something else is mid-animation and the player should not be
    // asked anything yet - walking through the bathroom door, for instance.
    // Without it the next word goes live the instant the previous one is
    // answered, so the game asks for "Ilaw" while Kylo is still walking and
    // the screen is fading, and that word can be answered behind the fade.
    private bool isPaused = false;
    private bool completionPending = false;

    /// <summary>True while the sequence is held, see SetPaused.</summary>
    public bool IsPaused => isPaused;

    /// <summary>
    /// Holds the sequence: hides the prompt and stops matching answers, so
    /// nothing can be answered during a cutaway. Releasing it puts the
    /// current objective back up where it left off.
    /// </summary>
    public void SetPaused(bool paused)
    {
        if (isPaused == paused) return;
        isPaused = paused;

        if (paused)
        {
            HideAllPrompts();
            ClearTargetWord();
            return;
        }

        if (completionPending)
        {
            completionPending = false;
            onAllObjectivesComplete?.Invoke();
            return;
        }
        ShowObjective(currentIndex);
    }

    private void ClearTargetWord()
    {
        if (voiceCommand == null) return;
        voiceCommand.currentTargetWord = "";
        voiceCommand.currentTargetWordVariants = null;
    }

    void Awake()
    {
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null) audioSource = gameObject.AddComponent<AudioSource>();
    }

    void OnEnable()
    {
        if (voiceCommand != null) voiceCommand.OnAnswerChecked += HandleAnswerChecked;
    }

    void OnDisable()
    {
        if (voiceCommand != null) voiceCommand.OnAnswerChecked -= HandleAnswerChecked;
    }

    void Start()
    {
        HideAllPrompts();
        ShowObjective(0);
    }

    // ---- Advancing through the objective list ----

    private void ShowObjective(int index)
    {
        currentIndex = index;
        HideAllPrompts();

        if (index < 0 || index >= objectives.Length)
        {
            // Sequence finished - stop checking answers against a specific
            // word so general listening (e.g. "Talon") keeps working normally.
            ClearTargetWord();

            // If the last word started a transition, the scene is still
            // mid-move; hold the completion until it lands, or the results
            // panel appears over the fade.
            if (isPaused)
            {
                completionPending = true;
                return;
            }
            onAllObjectivesComplete?.Invoke();
            return;
        }

        // While held, remember where we are but ask for nothing. SetPaused
        // puts this back up when the transition finishes.
        if (isPaused)
        {
            ClearTargetWord();
            return;
        }

        SceneObjective objective = objectives[index];
        if (objective.promptRoot != null) objective.promptRoot.SetActive(true);
        if (voiceCommand != null)
        {
            voiceCommand.currentTargetWord = objective.word;
            voiceCommand.currentTargetWordVariants = objective.variants;
        }
    }

    // Called whenever VoiceCommand finishes checking a spoken answer against
    // currentTargetWord - this is the single place a correct answer advances
    // the scene, regardless of which word it was.
    private void HandleAnswerChecked(string spokenText, bool isCorrect)
    {
        if (!isCorrect) return;
        if (isPaused) return;
        if (currentIndex < 0 || currentIndex >= objectives.Length) return;

        SceneObjective objective = objectives[currentIndex];
        if (objective.promptRoot != null) objective.promptRoot.SetActive(false);
        objective.onCorrect?.Invoke();

        if (!waitForActionToFinish)
        {
            ShowObjective(currentIndex + 1);
            return;
        }

        if (advanceRoutine != null) StopCoroutine(advanceRoutine);
        advanceRoutine = StartCoroutine(AdvanceWhenActionFinishes(currentIndex + 1));
    }

    /// <summary>
    /// Holds the sequence until the action the answered word started has
    /// finished, then asks the next word - or completes the scene.
    ///
    /// The word is answered the moment it is recognised, but the thing it does
    /// takes time: Lakad walks to the door, Takbo runs across the yard. Moving
    /// straight on put the next prompt on screen, or the results panel up,
    /// while the character was still mid-stride.
    /// </summary>
    private IEnumerator AdvanceWhenActionFinishes(int nextIndex)
    {
        // Nothing is asked while the action plays out, so the next word cannot
        // be answered over the top of the one still running.
        ClearTargetWord();

        // The answered word is finished with, even though the next one is not
        // up yet. Without this a transition resuming mid-wait would put the
        // word just answered back on screen.
        currentIndex = nextIndex;

        if (playerMovement == null) playerMovement = FindFirstObjectByType<PlayerMovement>();

        // One frame first: the action is raised this frame and has not started
        // moving anything yet.
        yield return null;

        float waited = 0f;
        while (playerMovement != null && playerMovement.IsVoiceWalking && waited < maxActionWait)
        {
            waited += Time.deltaTime;
            yield return null;
        }

        if (settlePause > 0f) yield return new WaitForSeconds(settlePause);

        advanceRoutine = null;
        ShowObjective(currentIndex);
    }

    /// <summary>
    /// Replays the instruction audio for the currently active objective.
    /// Called by the Replay/Speaker button (VoiceInteractionUI) - never
    /// touches the microphone or speech recognition.
    /// </summary>
    public void PlayCurrentInstruction()
    {
        if (currentIndex < 0 || currentIndex >= objectives.Length)
        {
            Debug.LogWarning("SceneObjectiveController: no active objective to replay.");
            return;
        }

        AudioClip clip = objectives[currentIndex].instructionClip;
        if (clip != null)
        {
            audioSource.PlayOneShot(clip);
        }
        else
        {
            Debug.LogWarning("SceneObjectiveController: no instructionClip assigned for '" + objectives[currentIndex].word + "' yet.");
        }
    }

    /// <summary>Hides every objective's prompt UI. Called by ExitDoorTrigger when the player leaves the scene mid-sequence.</summary>
    public void HideAllPrompts()
    {
        foreach (var o in objectives)
        {
            if (o.promptRoot != null) o.promptRoot.SetActive(false);
        }
    }
}
