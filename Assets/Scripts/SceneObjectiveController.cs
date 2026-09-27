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
        public UnityEvent onCorrect;
    }

    [Tooltip("The VoiceCommand that recognizes speech for this scene.")]
    public VoiceCommand voiceCommand;

    [Tooltip("Objectives in order, e.g. Bangon, Tayo, Lakad.")]
    public SceneObjective[] objectives;

    [Tooltip("Invoked once every objective in this list has been completed.")]
    public UnityEvent onAllObjectivesComplete;

    private int currentIndex = -1;
    private AudioSource audioSource;

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
            if (voiceCommand != null)
            {
                voiceCommand.currentTargetWord = "";
                voiceCommand.currentTargetWordVariants = null;
            }
            onAllObjectivesComplete?.Invoke();
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
        if (currentIndex < 0 || currentIndex >= objectives.Length) return;

        SceneObjective objective = objectives[currentIndex];
        if (objective.promptRoot != null) objective.promptRoot.SetActive(false);
        objective.onCorrect?.Invoke();
        ShowObjective(currentIndex + 1);
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
