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
/// <summary>
/// Something that can show Rachel's words. The narrator builds a plain bar of
/// its own and uses that unless a presenter is registered, so the prompting
/// system works on its own and a nicer panel - GameplayHUD's - is a skin over
/// it rather than a second subtitle system.
/// </summary>
public interface ICaptionPresenter
{
    /// <summary>Show this line, or hide the caption when text is null/empty.</summary>
    void ShowCaption(string text);
}

public class VoiceNarrator : MonoBehaviour
{
    public enum CaptionMode
    {
        /// <summary>Subtitle every line, recorded or not. The default.</summary>
        Always,
        /// <summary>Caption only lines that have not been recorded yet.</summary>
        WhenNoAudio,
        /// <summary>Never caption.</summary>
        Never,
    }

    [Header("Subtitles")]
    [Tooltip("Always: subtitle every line, so the child can read along with Rachel and the game still works on a muted phone. " +
             "WhenNoAudio: caption only the lines that have not been recorded yet.")]
    public CaptionMode captionMode = CaptionMode.Always;

    [Tooltip("Height of the caption bar across the top of the screen, at the 1920x1080 reference size.")]
    public float captionBarHeight = 88f;

    [Tooltip("Caption text size at the 1920x1080 reference size.")]
    public int captionFontSize = 34;

    [Tooltip("Longest caption put on screen at once. A longer line is broken at sentence ends " +
             "and shown a sentence at a time, so Rachel's panel never has to shrink the text " +
             "away or cut its tail off. The script keeps lines under this anyway; this is the " +
             "safety net for one that slips through.")]
    public int maxCaptionChars = 110;

    /// <summary>
    /// Where captions are drawn. Set by GameplayHUD so Rachel's lines appear in
    /// her own wooden panel; left null the narrator draws its own plain bar
    /// across the top, exactly as before. Registering and clearing this is the
    /// presenter's job - see GameplayHUD.Awake/OnDestroy.
    ///
    /// Taking over hands the presenter whatever is on screen at that moment.
    /// Without that, whoever ran first would decide: VoicePromptDirector queues
    /// the scene intro in its own Awake, and the director and the HUD are
    /// installed by two bootstraps in the same pass with no order between them,
    /// so Rachel can already be mid-line when the panel arrives. Replaying the
    /// current caption makes the order stop mattering.
    /// </summary>
    public ICaptionPresenter CaptionPresenter
    {
        get => captionPresenter;
        set
        {
            if (ReferenceEquals(captionPresenter, value)) return;
            captionPresenter = value;

            if (value == null) return;
            if (captionCanvas != null) captionCanvas.gameObject.SetActive(false);
            value.ShowCaption(CurrentCaption);
        }
    }

    private ICaptionPresenter captionPresenter;

    /// <summary>The caption on screen right now, or null between lines.</summary>
    public string CurrentCaption { get; private set; }

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

    /// <summary>
    /// Where a word falls inside an instruction, e.g. "Say {word} to make him
    /// put on his clothes!". Matches the convention Docs/VoiceOverScript.csv
    /// uses in its _ask rows.
    /// </summary>
    private const string Placeholder = "{word}";

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
        ApplyNarrationVolume();

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
    /// The story-based instruction, e.g. "Let's help Kylo get dressed!" +
    /// "DAMIT" + "to make him put on his clothes!" - one line, three parts,
    /// one caption.
    ///
    /// The line's text carries a {word} placeholder saying where the word
    /// falls, so an instruction can lead into the word or carry on past it;
    /// the word itself is still spoken from its own word_&lt;word&gt;.wav, so the
    /// child hears the Filipino pronounced by the same recording everywhere.
    ///
    /// Falls back to the plain "Sabihin mo:" frame when the script has no
    /// instruction for this word yet, so a half-written CSV still prompts.
    /// </summary>
    public void SpeakAsk(string askLineId, string word, AudioClip overrideClip = null)
    {
        NarratorLine line = bank != null ? bank.Find(askLineId) : null;
        AudioClip asked = line != null ? line.clip : null;
        string text = line != null && !string.IsNullOrEmpty(line.text)
            ? line.text
            : NarratorScript.DefaultText(askLineId);

        if (asked == null && string.IsNullOrEmpty(text))
        {
            SpeakPrompt(word, overrideClip);
            return;
        }

        // The word is always shown in capitals - it is the thing the child has
        // to read and repeat, so it looks the same here as on the badge.
        string spelled = string.IsNullOrEmpty(word) ? "" : word.Trim().ToUpperInvariant();
        string caption = string.IsNullOrEmpty(text) ? spelled : text.Replace(Placeholder, spelled);

        // Recorded, the instruction says the word itself, so it plays whole
        // rather than being joined to a second copy of the word.
        if (asked != null)
        {
            Utterance whole = new Utterance();
            whole.parts.Add(MakePart(asked, caption));
            Enqueue(whole);
            return;
        }

        Utterance u = new Utterance();
        int at = text.IndexOf(Placeholder, System.StringComparison.Ordinal);
        if (at < 0)
        {
            // No placeholder written: the word goes on the end, exactly as the
            // old "Sabihin mo:" frame did.
            u.parts.Add(MakePart(null, text));
            AddAskedWord(u, word, overrideClip, "");
        }
        else
        {
            string before = text.Substring(0, at).TrimEnd();
            string after = text.Substring(at + Placeholder.Length).TrimStart();

            // Punctuation straight after the word belongs to the word:
            // "say TAYO!", not "say TAYO !" with a second of silence for the
            // exclamation mark.
            int punctuation = 0;
            while (punctuation < after.Length && IsTrailingPunctuation(after[punctuation]))
                punctuation++;
            string tail = after.Substring(0, punctuation);
            after = after.Substring(punctuation).TrimStart();

            if (before.Length > 0) u.parts.Add(MakePart(null, before));
            AddAskedWord(u, word, overrideClip, tail);
            if (after.Length > 0) u.parts.Add(MakePart(null, after));
        }
        Enqueue(u);
    }

    private static bool IsTrailingPunctuation(char c)
    {
        return c == '!' || c == '.' || c == '?' || c == ',' || c == ';' || c == ':';
    }

    /// <summary>
    /// The word part of an instruction: the clip the scene baked in if it has
    /// one, otherwise whatever the bank knows, captioned in capitals with any
    /// punctuation that followed it in the written line.
    /// </summary>
    private void AddAskedWord(Utterance u, string word, AudioClip overrideClip, string suffix)
    {
        string spelled = string.IsNullOrEmpty(word) ? "" : word.Trim().ToUpperInvariant();
        AudioClip clip = overrideClip;

        if (clip == null)
        {
            NarratorLine line = bank != null ? bank.Find(NarratorScript.WordId(word)) : null;
            if (line != null)
            {
                clip = line.clip;
                if (spelled.Length == 0 && !string.IsNullOrEmpty(line.text))
                    spelled = line.text.Trim().ToUpperInvariant();
            }
        }

        if (spelled.Length == 0 && clip != null) spelled = clip.name;
        if (spelled.Length == 0 && clip == null) return;   // nothing to say

        u.parts.Add(MakePart(clip, spelled + suffix));
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

    /// <summary>
    /// Takes Rachel's volume from the saved settings. Her voice has its own
    /// slider because it is the one sound a parent or a classroom is likely to
    /// want quieter on its own - the instruction still reads on screen.
    /// Called by VolumeSettings.Apply() whenever the slider moves.
    /// </summary>
    public void ApplyNarrationVolume()
    {
        if (audioSource != null) audioSource.volume = VolumeSettings.Narration;
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

        // Normally the whole line sits on screen for as long as it takes to
        // say - a prompt reads as one sentence, so it should look like one.
        // Only when that would be too much text at once does the caption
        // follow the line part by part, and sentence by sentence inside a
        // part. The panel shrinks text to fit and then truncates it, so a
        // caption that is too long loses its ending silently.
        bool perPart = caption && u.Caption.Length > maxCaptionChars;
        if (!caption) ShowCaption(null);
        else if (!perPart) ShowCaption(u.Caption);

        for (int i = 0; i < u.parts.Count; i++)
        {
            Part part = u.parts[i];
            float duration;

            if (part.clip != null)
            {
                audioSource.pitch = part.pitch;
                audioSource.clip = part.clip;
                audioSource.Play();

                // Timed rather than polled: a clip can report itself finished
                // a frame early, and its length is what the pacing was
                // designed around either way.
                duration = part.clip.length / Mathf.Max(0.1f, part.pitch);
            }
            else
            {
                duration = SilentDuration(part.text);
            }

            if (perPart) yield return HoldCaption(part.text, duration);
            else yield return new WaitForSeconds(duration);

            if (part.clip != null)
            {
                audioSource.Stop();
                audioSource.pitch = 1f;
            }

            if (i < u.parts.Count - 1 && gapBetweenParts > 0f)
                yield return new WaitForSeconds(gapBetweenParts);
        }

        ShowCaption(null);
    }

    /// <summary>
    /// Shows a part's text for its whole duration, split into sentence-sized
    /// cards if it is too long for one. Each card gets a share of the time in
    /// proportion to its length, so the captions stay with the voice.
    /// </summary>
    private IEnumerator HoldCaption(string text, float duration)
    {
        List<string> cards = SplitCaption(text);
        if (cards.Count == 0)
        {
            ShowCaption(null);
            yield return new WaitForSeconds(duration);
            yield break;
        }

        int total = 0;
        foreach (string card in cards) total += card.Length;

        for (int i = 0; i < cards.Count; i++)
        {
            ShowCaption(cards[i]);

            // The last card takes whatever is left, so rounding can never add
            // up to more than the part's own duration.
            float share = i == cards.Count - 1
                ? duration
                : duration * cards[i].Length / Mathf.Max(1, total);
            duration -= share;

            yield return new WaitForSeconds(Mathf.Max(0f, share));
        }
    }

    /// <summary>
    /// Breaks a line at sentence ends into pieces that each fit a caption. A
    /// single sentence longer than the limit is broken at a space rather than
    /// mid-word.
    /// </summary>
    private List<string> SplitCaption(string text)
    {
        List<string> cards = new List<string>();
        if (string.IsNullOrEmpty(text)) return cards;

        string rest = text.Trim();
        while (rest.Length > maxCaptionChars)
        {
            // A card that stops on a full stop reads as a complete thought,
            // which is the whole point of splitting at all. Failing that,
            // break on a space rather than through the middle of a word.
            int cut = LastSentenceEnd(rest, maxCaptionChars);
            if (cut <= 0) cut = LastSpace(rest, maxCaptionChars);
            if (cut <= 0) cut = maxCaptionChars;

            string card = rest.Substring(0, cut).Trim();
            if (card.Length > 0) cards.Add(card);
            rest = rest.Substring(cut).TrimStart();
        }

        if (rest.Length > 0) cards.Add(rest);
        return cards;
    }

    /// <summary>Index just past the last sentence end at or before limit, or 0 for none.</summary>
    private static int LastSentenceEnd(string text, int limit)
    {
        int max = Mathf.Min(limit, text.Length);
        for (int i = max - 1; i >= 0; i--)
        {
            char c = text[i];
            if (c != '.' && c != '!' && c != '?') continue;
            if (i + 1 < text.Length && text[i + 1] != ' ') continue;   // "3.5", not a stop
            return i + 1;
        }
        return 0;
    }

    /// <summary>Index of the last space at or before limit, or 0 for none.</summary>
    private static int LastSpace(string text, int limit)
    {
        int max = Mathf.Min(limit, text.Length);
        for (int i = max - 1; i > 0; i--) if (text[i] == ' ') return i;
        return 0;
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
        CurrentCaption = string.IsNullOrEmpty(text) ? null : text;

        if (captionPresenter != null)
        {
            // The built-in bar was built before anyone registered, so make sure
            // it is not sitting behind the panel that replaced it.
            if (captionCanvas != null && captionCanvas.gameObject.activeSelf)
                captionCanvas.gameObject.SetActive(false);

            captionPresenter.ShowCaption(text);
            return;
        }

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
