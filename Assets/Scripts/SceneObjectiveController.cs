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

    /// <summary>
    /// Raised with an objective's own word each time one is answered
    /// correctly, for anything that needs to know WHICH word rather than just
    /// that a word landed - RunScoreCounter uses it to build the list the
    /// level-complete panel shows.
    /// </summary>
    public event System.Action<string> OnObjectiveCompleted;

    /// <summary>
    /// Raised each time an objective goes live - the prompt is up, the word
    /// is set, and the player is expected to say it. VoicePromptDirector
    /// listens for this to speak the prompt, which is why nothing narrates
    /// while Kylo is mid-walk: no objective is raised until the action she
    /// started has finished.
    /// </summary>
    public event System.Action<SceneObjective> OnObjectiveShown;

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

    [Header("Holding a new word while Rachel introduces it")]
    [Tooltip("Show a new word faint while the narrator is still saying it, and brighten it " +
             "into the player's turn when she finishes. Without this the badge and arrow go " +
             "up at full strength while the child is still being told about them.")]
    public bool holdPromptUntilNarrationEnds = true;

    [Tooltip("How faint the badge is while Rachel is still speaking (1 = no dimming).")]
    [Range(0f, 1f)]
    public float dimmedPromptAlpha = 0.45f;

    [Tooltip("Safety cap in seconds, so narration that never finishes cannot leave a word " +
             "dimmed and unanswerable forever.")]
    public float maxNarrationWait = 15f;

    private int currentIndex = -1;
    private bool objectiveLive = false;
    private Coroutine revealRoutine;
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
    /// The word the player is being asked for right now, or null if nothing is
    /// being asked - between words, during an action, while paused, and once
    /// the scene is finished.
    /// </summary>
    public SceneObjective CurrentObjective
    {
        get
        {
            if (!objectiveLive) return null;
            if (currentIndex < 0 || currentIndex >= objectives.Length) return null;
            return objectives[currentIndex];
        }
    }

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
            CancelReveal();
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
        objectiveLive = false;
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
        objectiveLive = false;
        CancelReveal();
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

        // The word goes up faint first: Rachel is still introducing it, so it
        // is not the player's turn yet. GoLive brightens it and starts
        // accepting the answer once she has finished.
        if (objective.promptRoot != null)
        {
            objective.promptRoot.SetActive(true);
            SetPromptAlpha(objective.promptRoot, dimmedPromptAlpha);
        }

        // Raised BEFORE the word is set, so the narrator has already been
        // asked to speak by the time we check below - VoiceNarrator.Enqueue
        // marks itself speaking synchronously inside this call.
        if (OnObjectiveShown != null) OnObjectiveShown(objective);

        VoiceNarrator narrator = VoiceNarrator.Instance;
        if (holdPromptUntilNarrationEnds && narrator != null && narrator.IsSpeaking)
            revealRoutine = StartCoroutine(RevealWhenNarrationEnds(index));
        else
            GoLive(index);
    }

    /// <summary>
    /// The word is now the player's to say: full brightness, and the
    /// recognizer starts matching against it.
    /// </summary>
    private void GoLive(int index)
    {
        if (index != currentIndex) return;
        if (index < 0 || index >= objectives.Length) return;

        SceneObjective objective = objectives[index];
        if (objective.promptRoot != null) SetPromptAlpha(objective.promptRoot, 1f);

        if (voiceCommand != null)
        {
            voiceCommand.currentTargetWord = objective.word;
            voiceCommand.currentTargetWordVariants = objective.variants;
        }

        objectiveLive = true;
    }

    /// <summary>
    /// Waits for the narrator to stop talking, then hands the word to the
    /// player. The cap means a line that never ends cannot strand a word
    /// dimmed and unanswerable; a narrator destroyed mid-line reads as null
    /// and ends the wait the same way.
    /// </summary>
    private IEnumerator RevealWhenNarrationEnds(int index)
    {
        float waited = 0f;
        VoiceNarrator narrator = VoiceNarrator.Instance;

        while (narrator != null && narrator.IsSpeaking && waited < maxNarrationWait)
        {
            waited += Time.deltaTime;
            yield return null;
        }

        revealRoutine = null;

        // The scene may have moved on or been paused while she was speaking.
        if (isPaused || index != currentIndex) yield break;

        GoLive(index);
    }

    private void CancelReveal()
    {
        if (revealRoutine == null) return;
        StopCoroutine(revealRoutine);
        revealRoutine = null;
    }

    /// <summary>
    /// Fades a prompt's badge, word and arrow together. A CanvasGroup is
    /// added on demand rather than built into CreatePromptUI, so none of the
    /// fifteen scene builders has to change and no scene has to be rebuilt.
    /// </summary>
    private void SetPromptAlpha(GameObject promptRoot, float alpha)
    {
        CanvasGroup group = promptRoot.GetComponent<CanvasGroup>();
        if (group == null) group = promptRoot.AddComponent<CanvasGroup>();
        group.alpha = alpha;
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

        // Reported from here, not read off VoiceCommand.currentTargetWord by
        // the listener, because both branches below clear or advance that word
        // synchronously - so whether a listener saw the answered word or the
        // next one came down to which of them subscribed first.
        OnObjectiveCompleted?.Invoke(objective.word);

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
        Replay(false);
    }

    /// <summary>
    /// The same replay, with the word said slowly. The replay button calls
    /// this when it is pressed twice in quick succession - see
    /// VoiceInteractionUI.
    /// </summary>
    public void PlayCurrentInstructionSlowly()
    {
        Replay(true);
    }

    private void Replay(bool slowly)
    {
        // Only while a word is actually being asked. Replaying between words
        // used to play the NEXT word's clip, giving away a word the player had
        // not been asked for yet.
        SceneObjective objective = CurrentObjective;
        if (objective == null)
        {
            Debug.Log("SceneObjectiveController: nothing is being asked right now, so there is nothing to replay.");
            return;
        }

        VoiceNarrator narrator = VoiceNarrator.Instance;
        if (narrator != null)
        {
            narrator.SpeakReplay(objective.word, objective.instructionClip, slowly);
            return;
        }

        // No narrator (shouldn't happen - one is created on demand), so fall
        // back to the plain clip.
        if (objective.instructionClip != null) audioSource.PlayOneShot(objective.instructionClip);
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
