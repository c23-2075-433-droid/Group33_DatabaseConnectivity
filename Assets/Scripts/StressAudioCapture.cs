using UnityEngine;

/// <summary>
/// Records the player's own voice while a stress-pair word (tayô, upô ...) is
/// being asked, so RawStressAnalyzer can hear WHICH syllable they stressed -
/// something the device recognizer, which returns letters only, cannot say.
///
/// HOW IT SHARES THE MICROPHONE
///
/// The recognizer must keep working whatever happens here, so this is careful
/// about who holds the microphone:
///
///   - It records ONLY while a stress-pair word is live. Every other word
///     never touches Unity's Microphone at all.
///   - It starts AFTER the recognizer reports it is listening
///     (OnReadyForSpeech), so the recognizer already has the microphone. On a
///     phone that shares the microphone (Android 10+ with the usual Google
///     recognizer, iOS) both hear the child. On one that does not, it is THIS
///     recording that comes back silent - and a silent recording is detected
///     and simply not used: the recognizer's loudness readings decide instead.
///   - If the recognizer nevertheless starts failing while this is recording
///     (VoiceCommand counts it), VoiceCommand calls DisableForSession and the
///     game goes back to exactly how it listened before.
///
/// Recording runs continuously while the word is live, in a 10-second loop,
/// and each answer is cut out of it from the moment the recognizer started
/// listening to the moment its result arrived. Continuous, because starting a
/// microphone takes a moment, and a child who answers instantly would lose
/// their first syllable - the very one that tells táyo from tayô.
///
/// Nothing recorded here is kept: each answer is analysed and dropped. It is
/// never written to disk and never sent anywhere.
/// </summary>
public class StressAudioCapture : MonoBehaviour
{
    [Tooltip("Sample rate asked of the microphone. 16 kHz is plenty for pitch and length.")]
    public int sampleRate = 16000;

    private const int LoopSeconds = 10;

    /// <summary>A recording whose loudest sample is below this is treated as silence.</summary>
    private const float SilentPeak = 0.0005f;

    private AudioClip clip;
    private string device;
    private int markPosition = -1;
    private bool disabled;
    private int silentInARow;

    /// <summary>True while the microphone is recording.</summary>
    public bool IsRecording { get { return clip != null && Microphone.IsRecording(device); } }

    /// <summary>True once the session has given up on recording (see DisableForSession).</summary>
    public bool IsDisabled { get { return disabled; } }

    /// <summary>Why recording was given up on, for the console and the log.</summary>
    public string DisabledReason { get; private set; }

    /// <summary>Starts the microphone if it is not already running. Safe to call repeatedly.</summary>
    public bool Begin()
    {
        if (disabled) return false;
        if (IsRecording) return true;

        try
        {
            if (Microphone.devices == null || Microphone.devices.Length == 0)
            {
                DisableForSession("no microphone device");
                return false;
            }

            device = null;   // the default device
            clip = Microphone.Start(device, true, LoopSeconds, sampleRate);
            if (clip == null)
            {
                DisableForSession("the microphone would not start");
                return false;
            }
            return true;
        }
        catch (System.Exception e)
        {
            DisableForSession("the microphone threw: " + e.Message);
            return false;
        }
    }

    /// <summary>Marks where the next answer starts: the recognizer has just started listening.</summary>
    public void MarkStart()
    {
        if (!IsRecording) { markPosition = -1; return; }
        markPosition = Microphone.GetPosition(device);
    }

    /// <summary>
    /// The audio from the last MarkStart to now, mono, or null if nothing
    /// usable was recorded (not recording, or the recording is silent because
    /// the phone gave the microphone to the recognizer alone).
    /// </summary>
    public float[] TakeAnswer(out int rate)
    {
        rate = clip != null ? clip.frequency : sampleRate;
        if (!IsRecording || markPosition < 0) return null;

        int end = Microphone.GetPosition(device);
        int total = clip.samples;
        int length = end - markPosition;
        if (length <= 0) length += total;
        if (length <= 0 || length > total) length = total;

        int channels = Mathf.Max(1, clip.channels);
        float[] raw = new float[length * channels];
        int start = markPosition;
        markPosition = -1;

        // A looping clip: read in up to two pieces across the wrap.
        int firstPart = Mathf.Min(length, total - start);
        float[] a = new float[firstPart * channels];
        clip.GetData(a, start);
        System.Array.Copy(a, raw, a.Length);
        if (firstPart < length)
        {
            float[] b = new float[(length - firstPart) * channels];
            clip.GetData(b, 0);
            System.Array.Copy(b, 0, raw, a.Length, b.Length);
        }

        float[] mono = new float[length];
        float peak = 0f;
        for (int i = 0; i < length; i++)
        {
            float sum = 0f;
            for (int c = 0; c < channels; c++) sum += raw[i * channels + c];
            mono[i] = sum / channels;
            peak = Mathf.Max(peak, Mathf.Abs(mono[i]));
        }

        if (peak < SilentPeak)
        {
            // The phone is not sharing the microphone with us. Twice in a row
            // means it never will this session: stop holding it.
            if (++silentInARow >= 2) DisableForSession("the phone gives the microphone to the recognizer only");
            return null;
        }
        silentInARow = 0;
        return mono;
    }

    /// <summary>Stops recording until the next Begin.</summary>
    public void Stop()
    {
        markPosition = -1;
        if (clip == null) return;
        try { Microphone.End(device); } catch (System.Exception) { }
        clip = null;
    }

    /// <summary>Stops for good this session; the game falls back to the recognizer's loudness readings.</summary>
    public void DisableForSession(string reason)
    {
        if (disabled) return;
        disabled = true;
        DisabledReason = reason;
        Debug.LogWarning("StressAudioCapture: recording switched off for this session - " + reason +
                         ". Stress is judged from the recognizer's loudness readings instead.");
        Stop();
    }

    void OnDisable() { Stop(); }
    void OnDestroy() { Stop(); }
}
