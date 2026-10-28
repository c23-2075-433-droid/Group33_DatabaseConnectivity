using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Teacher Rachel's voice: the single place anything spoken comes out of, so
/// two lines can never overlap and the microphone is never listening while
/// she talks.
///
/// Two things make this usable before the voice-over is recorded:
///
///   1. A line with no clip is not skipped - its text is shown as a caption
///      and the pause the clip would have taken is still held. The prompting
///      flow, its pacing and its on-screen Filipino are all testable today;
///      dropping the .wav files in later only adds the sound.
///   2. Captions default to appearing only for lines that have no recording
///      (see captionMode), so they fade out of the game by themselves as the
///      recordings arrive.
///
/// Nothing has to be wired in the Inspector and no scene has to be rebuilt:
/// VoicePromptDirector asks for Instance, which finds or creates one.
/// </summary>
public class VoiceNarrator : MonoBehaviour
{
    public enum CaptionMode
    {
        /// <summary>Caption only lines that have not been recorded yet. The default.</summary>
        WhenNoAudio,
        /// <summary>Always caption - subtitles, and useful on a muted phone.</summary>
        Always,
        /// <summary>Never caption.</summary>
        Never,
    }

    [Header("Captions")]
    public CaptionMode captionMode = CaptionMode.WhenNoAudio;

    [Tooltip("Height of the caption bar across the top of the screen, at the 1920x1080 reference size.")]
    public float captionBarHeight = 88f;

    [Tooltip("Caption text size at the 1920x1080 reference size.")]
    public int captionFontSize = 34;

    [Header("Pacing")]
    [Tooltip("Silence between the parts of one line, e.g. between \"Sabihin mo:\" and the word itself.")]
    public float gapBetweenParts = 0.12f;

    [Tooltip("Silence after a line finishes, before the microphone starts listening again.")]
    public float tailPause = 0.2f;

    [Tooltip("Playback speed for the slow repeat of a word after a second miss.")]
    [Range(0.5f, 1f)]
    public float slowPitch = 0.85f;

    [Tooltip("Seconds a caption is held per word when the line has no recording yet, so unrecorded prompts are paced like recorded ones.")]
    public float secondsPerWordWhenSilent = 0.32f;

    [Tooltip("Shortest a silent line is held for, however few words it has.")]
    public float minSilentDuration = 1f;

    /// <summary>True from the moment a line is queued until the last one has finished.</summary>
    public bool IsSpeaking { get; private set; }

    /// <summary>Raised when the queue empties - the moment the player is expected to answer.</summary>
    public event System.Action OnFinishedSpeaking;

    // ---------------------------------------------------------------------
    // Instance - scene-scoped, created on demand
    // ---------------------------------------------------------------------

    private static VoiceNarrator instance;
    private static bool isQuitting;

    /// <summary>
    /// The scene's narrator, created if the scene does not have one. Scene
    /// builders therefore need no changes: nothing in a saved scene refers to
    /// the narrator, so nothing is lost when a scene is rebuilt.
    /// </summary>
    public static VoiceNarrator Instance
    {
        get
        {
            if (isQuitting) return null;
            if (instance != null) return instance;

            instance = FindFirstObjectByType<VoiceNarrator>();
            if (instance == null)
            {
                GameObject go = new GameObject("VoiceNarrator");
                instance = go.AddComponent<VoiceNarrator>();
            }
            return instance;
        }
    }

    // ---------------------------------------------------------------------

    /// <summary>One piece of a line: a clip, its caption text, or both.</summary>
    private struct Part
    {
        public AudioClip clip;
        public string text;
        public float pitch;
    }

    private class Utterance
    {
        public List<Part> parts = new List<Part>();
        /// <summary>
        /// True only when every part has a recording. A prompt whose frame is
        /// recorded but whose word is not is NOT fully recorded - the silent
        /// half still needs its caption, or the player gets "Sabihin mo:"
        /// followed by a second of nothing.
        /// </summary>
        public bool IsFullyRecorded
        {
            get
            {
                foreach (Part p in parts) if (p.clip == null) return false;
                return parts.Count > 0;
            }
        }
        public string Caption
        {
            get
            {
                string caption = "";
                foreach (Part p in parts)
                {
                    if (string.IsNullOrEmpty(p.text)) continue;
                    if (caption.Length > 0) caption += " ";
                    caption += p.text;
                }
                return caption;
            }
        }
    }

    private readonly Queue<Utterance> queue = new Queue<Utterance>();
    private NarratorVoiceBank bank;
    private AudioSource audioSource;
    private VoiceCommand voiceCommand;
    private Coroutine speaking;
    private int correctCueIndex;

    private Canvas captionCanvas;
    private Text captionText;
    private Image captionPanel;

    void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }
        instance = this;

        bank = NarratorVoiceBank.Load();
        if (bank != null) bank.Invalidate();

        audioSource = GetComponent<AudioSource>();
        if (audioSource == null) audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.playOnAwake = false;

        BuildCaptionUI();
    }

    void OnApplicationQuit()
    {
        isQuitting = true;
    }

    void OnDestroy()
    {
        if (instance == this) instance = null;
    }

    /// <summary>The scene's VoiceCommand, looked up once and re-looked-up if the scene changed under us.</summary>
    private VoiceCommand Voice
    {
        get
        {
            if (voiceCommand == null) voiceCommand = FindFirstObjectByType<VoiceCommand>();
            return voiceCommand;
        }
    }

    // ---------------------------------------------------------------------
    // What the director asks for
    // ---------------------------------------------------------------------

    /// <summary>
    /// "Sabihin mo: LAKAD" - the prompt frame joined to the word's own clip.
    /// A whole recorded sentence in overrideClip (an objective's
    /// instructionClip) is played on its own instead.
    /// </summary>
    public void SpeakPrompt(string word, AudioClip overrideClip = null, bool laterInScene = false)
    {
        if (overrideClip != null)
        {
            Utterance whole = new Utterance();
            whole.parts.Add(MakePart(overrideClip, TextForClip(overrideClip, word)));
            Enqueue(whole);
            return;
        }

        Utterance u = new Utterance();
        AddLine(u, laterInScene ? NarratorScript.FrameSayAgain : NarratorScript.FrameSay);
        AddWord(u, word);
        Enqueue(u);
    }

    /// <summary>
    /// "Pakinggan mo ulit: LAKAD" - what the replay button says. Pressed a
    /// second time in quick succession it says the word slowly instead, which
    /// needs no extra recording.
    /// </summary>
    public void SpeakReplay(string word, AudioClip overrideClip = null, bool slowly = false)
    {
        float pitch = slowly ? slowPitch : 1f;

        Utterance u = new Utterance();
        AddLine(u, NarratorScript.FrameListen);

        if (overrideClip != null)
            u.parts.Add(MakePart(overrideClip, TextForClip(overrideClip, word), pitch));
        else
            AddWord(u, word, pitch);

        Enqueue(u);
    }

    /// <summary>"Pakinggan mong mabuti. Pakinggan mo ulit: LAKAD" - said slowly, after a second miss.</summary>
    public void SpeakHint(string word)
    {
        Utterance u = new Utterance();
        AddLine(u, NarratorScript.HintListen);
        AddLine(u, NarratorScript.FrameListen);
        AddWord(u, word, slowPitch);
        Enqueue(u);
    }

    /// <summary>"Tama!" / "Magaling!" / "Ang galing mo!", in rotation.</summary>
    public void SpeakCorrect()
    {
        string id = NarratorScript.CorrectCues[correctCueIndex % NarratorScript.CorrectCues.Length];
        correctCueIndex++;
        SpeakLine(id);
    }

    /// <summary>Any single line, by id.</summary>
    public void SpeakLine(string id)
    {
        Utterance u = new Utterance();
        AddLine(u, id);
        if (u.parts.Count == 0) return;
        Enqueue(u);
    }

    /// <summary>
    /// True if a line has either a recording or text, i.e. saying it would
    /// produce something. Lets the director skip a scene intro that has not
    /// been written or recorded rather than holding a silent pause for it.
    /// </summary>
    public bool CanSay(string id)
    {
        if (string.IsNullOrEmpty(id)) return false;
        NarratorLine line = bank != null ? bank.Find(id) : null;
        if (line != null && (line.clip != null || !string.IsNullOrEmpty(line.text))) return true;
        return !string.IsNullOrEmpty(NarratorScript.DefaultText(id));
    }

    /// <summary>Drops everything queued and stops mid-clip. Used for barge-in and scene changes.</summary>
    public void StopSpeaking()
    {
        queue.Clear();
        if (speaking != null)
        {
            StopCoroutine(speaking);
            speaking = null;
        }
        if (audioSource != null) audioSource.Stop();
        ShowCaption(null);
        FinishSpeaking();
    }

    // ---------------------------------------------------------------------
    // Building lines
    // ---------------------------------------------------------------------

    private Part MakePart(AudioClip clip, string text, float pitch = 1f)
    {
        Part p;
        p.clip = clip;
        p.text = text;
        p.pitch = pitch;
        return p;
    }

    private void AddLine(Utterance u, string id, float pitch = 1f)
    {
        if (string.IsNullOrEmpty(id)) return;

        NarratorLine line = bank != null ? bank.Find(id) : null;
        AudioClip clip = line != null ? line.clip : null;
        string text = line != null && !string.IsNullOrEmpty(line.text)
            ? line.text
            : NarratorScript.DefaultText(id);

        if (clip == null && string.IsNullOrEmpty(text)) return;   // nothing to say
        u.parts.Add(MakePart(clip, text, pitch));
    }

    private void AddWord(Utterance u, string word, float pitch = 1f)
    {
        if (string.IsNullOrEmpty(word)) return;

        string id = NarratorScript.WordId(word);
        NarratorLine line = bank != null ? bank.Find(id) : null;
        AudioClip clip = line != null ? line.clip : null;

        // The word is always shown in capitals, whatever case the bank or the
        // objective spells it in - it is the thing the child has to read and
        // repeat, so it should look the same in the caption and on the badge.
        string text = line != null && !string.IsNullOrEmpty(line.text) ? line.text : word;
        u.parts.Add(MakePart(clip, text.Trim().ToUpperInvariant(), pitch));
    }

    /// <summary>Caption for a whole recorded sentence, from the bank if it knows the clip.</summary>
    private string TextForClip(AudioClip clip, string fallbackWord)
    {
        if (clip != null && bank != null)
        {
            NarratorLine line = bank.Find(clip.name);
            if (line != null && !string.IsNullOrEmpty(line.text)) return line.text;
        }
        return string.IsNullOrEmpty(fallbackWord) ? null : fallbackWord.Trim().ToUpperInvariant();
    }

    // ---------------------------------------------------------------------
    // Playback
    // ---------------------------------------------------------------------

    private void Enqueue(Utterance u)
    {
        if (u == null || u.parts.Count == 0) return;

        queue.Enqueue(u);
        if (speaking == null)
        {
            IsSpeaking = true;
            // The microphone goes deaf for the whole run of queued lines, not
            // per line, so a prompt made of a frame plus a word is one silence
            // rather than two.
            SuspendListening();
            speaking = StartCoroutine(SpeakQueue());
        }
    }

    private IEnumerator SpeakQueue()
    {
        // The outer loop exists because a line can be queued DURING the tail
        // pause below. Without it that line would sit in the queue with no
        // coroutine left to play it.
        while (true)
        {
            while (queue.Count > 0)
            {
                yield return Say(queue.Dequeue());
            }

            if (tailPause > 0f) yield return new WaitForSeconds(tailPause);
            if (queue.Count == 0) break;
        }

        speaking = null;
        FinishSpeaking();
    }

    private IEnumerator Say(Utterance u)
    {
        bool caption = captionMode == CaptionMode.Always ||
                       (captionMode == CaptionMode.WhenNoAudio && !u.IsFullyRecorded);
        ShowCaption(caption ? u.Caption : null);

        for (int i = 0; i < u.parts.Count; i++)
        {
            Part part = u.parts[i];

            if (part.clip != null)
            {
                audioSource.pitch = part.pitch;
                audioSource.clip = part.clip;
                audioSource.Play();

                // Timed rather than polled: a clip can report itself finished
                // a frame early, and its length is what the pacing was
                // designed around either way.
                yield return new WaitForSeconds(part.clip.length / Mathf.Max(0.1f, part.pitch));

                audioSource.Stop();
                audioSource.pitch = 1f;
            }
            else
            {
                yield return new WaitForSeconds(SilentDuration(part.text));
            }

            if (i < u.parts.Count - 1 && gapBetweenParts > 0f)
                yield return new WaitForSeconds(gapBetweenParts);
        }

        ShowCaption(null);
    }

    /// <summary>How long an unrecorded line is held for - roughly how long it would take to say.</summary>
    private float SilentDuration(string text)
    {
        if (string.IsNullOrEmpty(text)) return minSilentDuration;
        int words = text.Split(' ').Length;
        return Mathf.Max(minSilentDuration, words * secondsPerWordWhenSilent + 0.4f);
    }

    private void FinishSpeaking()
    {
        if (!IsSpeaking) return;
        IsSpeaking = false;
        ResumeListening();
        if (OnFinishedSpeaking != null) OnFinishedSpeaking();
    }

    private void SuspendListening()
    {
        VoiceCommand vc = Voice;
        if (vc != null) vc.SuspendListening();
    }

    private void ResumeListening()
    {
        VoiceCommand vc = Voice;
        if (vc != null) vc.ResumeListening();
    }

    // ---------------------------------------------------------------------
    // Caption bar - built in code so no scene has to be rebuilt for it
    // ---------------------------------------------------------------------

    private void BuildCaptionUI()
    {
        GameObject canvasGO = new GameObject("NarratorCaption_Canvas");
        canvasGO.transform.SetParent(transform, false);

        captionCanvas = canvasGO.AddComponent<Canvas>();
        captionCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        // Above the word badges and the mic buttons: a caption that is behind
        // a prompt badge is worse than no caption.
        captionCanvas.sortingOrder = 500;

        CanvasScaler scaler = canvasGO.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.Expand;

        GameObject panelGO = new GameObject("CaptionBar", typeof(RectTransform));
        panelGO.transform.SetParent(canvasGO.transform, false);
        captionPanel = panelGO.AddComponent<Image>();
        captionPanel.color = new Color(0f, 0f, 0f, 0.78f);
        captionPanel.raycastTarget = false;

        // Stretched across the top edge, clear of the word badges (which sit
        // no higher than y=400 in the scene builders) and of the mic and
        // replay buttons at the bottom.
        RectTransform panelRect = panelGO.GetComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0f, 1f);
        panelRect.anchorMax = new Vector2(1f, 1f);
        panelRect.pivot = new Vector2(0.5f, 1f);
        panelRect.offsetMin = new Vector2(0f, -captionBarHeight);
        panelRect.offsetMax = Vector2.zero;

        GameObject textGO = new GameObject("CaptionText", typeof(RectTransform));
        textGO.transform.SetParent(panelGO.transform, false);
        captionText = textGO.AddComponent<Text>();
        captionText.font = ResolveCaptionFont();
        captionText.fontSize = captionFontSize;
        captionText.fontStyle = FontStyle.Bold;
        captionText.alignment = TextAnchor.MiddleCenter;
        captionText.color = Color.white;
        captionText.horizontalOverflow = HorizontalWrapMode.Wrap;
        captionText.verticalOverflow = VerticalWrapMode.Overflow;
        captionText.raycastTarget = false;

        RectTransform textRect = textGO.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = new Vector2(40f, 0f);
        textRect.offsetMax = new Vector2(-40f, 0f);

        canvasGO.SetActive(false);
    }

    private Font ResolveCaptionFont()
    {
        if (bank != null && bank.captionFont != null) return bank.captionFont;

        // Unity renamed the built-in font; try both rather than guessing.
        foreach (string builtin in new[] { "LegacyRuntime.ttf", "Arial.ttf" })
        {
            try
            {
                Font font = Resources.GetBuiltinResource<Font>(builtin);
                if (font != null) return font;
            }
            catch (System.Exception)
            {
                // Not in this Unity version - try the next one.
            }
        }

        Debug.LogWarning("VoiceNarrator: no caption font found. Run " +
                         "Tools > SALINLAHI > Refresh Narrator Voice Bank to assign one.");
        return null;
    }

    private void ShowCaption(string text)
    {
        if (captionCanvas == null) return;

        if (string.IsNullOrEmpty(text))
        {
            captionCanvas.gameObject.SetActive(false);
            return;
        }

        captionText.text = text;
        captionCanvas.gameObject.SetActive(true);
    }
}
