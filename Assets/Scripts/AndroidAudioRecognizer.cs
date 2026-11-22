using System;
using UnityEngine;

/// <summary>
/// Unity side of Assets/Plugins/Android/SalinlahiAudioRecognizer.java: one
/// listening round in which the GAME records the child and hands that same
/// audio to Android's speech recognizer (Android 13+). The recognizer says
/// which word was said; the audio goes to the pronunciation model, which says
/// how. Used by VoiceCommand for the six strictly judged words only.
///
/// Elsewhere (the Editor, iOS, Android below 13) IsSupported is false and
/// nothing here does anything.
///
/// The plugin talks back with UnitySendMessage, so this lives on its own
/// GameObject with a fixed, unique name.
/// </summary>
public class AndroidAudioRecognizer : MonoBehaviour
{
    private const string ObjectName = "SalinlahiAudioRecognizerBridge";
    private const string JavaClass = "ph.edu.uphsl.group33.salinlahi.SalinlahiAudioRecognizer";

    public event Action Ready;
    public event Action SpeechBegan;
    public event Action<string> Result;
    public event Action<string> Error;

    private static AndroidAudioRecognizer instance;

    /// <summary>The one bridge object, created on first use.</summary>
    public static AndroidAudioRecognizer Instance
    {
        get
        {
            if (instance == null)
            {
                GameObject go = GameObject.Find(ObjectName);
                if (go == null)
                {
                    go = new GameObject(ObjectName);
                    DontDestroyOnLoad(go);
                }
                instance = go.GetComponent<AndroidAudioRecognizer>();
                if (instance == null) instance = go.AddComponent<AndroidAudioRecognizer>();
            }
            return instance;
        }
    }

    private static int supported = -1;

    /// <summary>True on Android 13+ with a speech recognizer installed.</summary>
    public static bool IsSupported
    {
        get
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            if (supported < 0)
            {
                try
                {
                    using (AndroidJavaClass c = new AndroidJavaClass(JavaClass))
                        supported = c.CallStatic<bool>("isSupported") ? 1 : 0;
                }
                catch (Exception e)
                {
                    Debug.LogWarning("AndroidAudioRecognizer: plugin not available - " + e.Message);
                    supported = 0;
                }
            }
            return supported == 1;
#else
            return false;
#endif
        }
    }

    /// <summary>Starts one listening round. onDevice asks for the phone's on-device recognizer.</summary>
    public void StartListening(string language, bool onDevice)
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        using (AndroidJavaClass c = new AndroidJavaClass(JavaClass))
            c.CallStatic("start", ObjectName, language ?? "", onDevice);
#endif
    }

    public void Cancel()
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        try
        {
            using (AndroidJavaClass c = new AndroidJavaClass(JavaClass)) c.CallStatic("cancel");
        }
        catch (Exception e) { Debug.LogWarning("AndroidAudioRecognizer: cancel failed - " + e.Message); }
#endif
    }

    /// <summary>Keeps what has been recorded so far as the round's audio (for a round that timed out).</summary>
    public void KeepNow()
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        try
        {
            using (AndroidJavaClass c = new AndroidJavaClass(JavaClass)) c.CallStatic("keepNow");
        }
        catch (Exception e) { Debug.LogWarning("AndroidAudioRecognizer: keepNow failed - " + e.Message); }
#endif
    }

    /// <summary>The audio of the round that just ended (mono, -1..1), or null.</summary>
    public float[] TakeAudio(out int sampleRate)
    {
        sampleRate = 16000;
#if UNITY_ANDROID && !UNITY_EDITOR
        try
        {
            using (AndroidJavaClass c = new AndroidJavaClass(JavaClass))
            {
                sampleRate = c.CallStatic<int>("sampleRate");
                float[] a = c.CallStatic<float[]>("takeAudio");
                return a != null && a.Length > 0 ? a : null;
            }
        }
        catch (Exception e) { Debug.LogWarning("AndroidAudioRecognizer: could not take the audio - " + e.Message); }
#endif
        return null;
    }

    /// <summary>A short on-screen message (for testing on a phone without a cable).</summary>
    public static void Toast(string text)
    {
        Debug.Log("AndroidAudioRecognizer: " + text);
#if UNITY_ANDROID && !UNITY_EDITOR
        try
        {
            using (AndroidJavaClass c = new AndroidJavaClass(JavaClass)) c.CallStatic("toast", text);
        }
        catch (Exception) { }
#endif
    }

    // ---- On-screen diagnostics (testing only) -----------------------------------------

    /// <summary>Shows a box with the last listening rounds at the top of the screen (VoiceCommand.showAudioBridgeMessages).</summary>
    public static bool ShowDiagnostics;

    private static readonly System.Collections.Generic.List<string> notes = new System.Collections.Generic.List<string>();

    /// <summary>Adds a line to the diagnostics box (and the log).</summary>
    public static void Note(string line)
    {
        string stamped = DateTime.Now.ToString("HH:mm:ss") + "  " + line;
        Debug.Log("AudioBridge: " + stamped);
        notes.Add(stamped);
        if (notes.Count > 12) notes.RemoveAt(0);
        if (ShowDiagnostics) { var unused = Instance; }
    }

    /// <summary>Android's SpeechRecognizer error codes, by name.</summary>
    public static string ErrorName(string code)
    {
        int c;
        if (!int.TryParse(code, out c)) return code;
        switch (c)
        {
            case 1: return "1 NETWORK_TIMEOUT";
            case 2: return "2 NETWORK";
            case 3: return "3 AUDIO";
            case 4: return "4 SERVER";
            case 5: return "5 CLIENT";
            case 6: return "6 SPEECH_TIMEOUT";
            case 7: return "7 NO_MATCH";
            case 8: return "8 RECOGNIZER_BUSY";
            case 9: return "9 INSUFFICIENT_PERMISSIONS";
            case 10: return "10 TOO_MANY_REQUESTS";
            case 11: return "11 SERVER_DISCONNECTED";
            case 12: return "12 LANGUAGE_NOT_SUPPORTED";
            case 13: return "13 LANGUAGE_UNAVAILABLE";
            case 14: return "14 CANNOT_CHECK_SUPPORT";
            default: return code;
        }
    }

    private GUIStyle style;

    void OnGUI()
    {
        if (!ShowDiagnostics || !Debug.isDebugBuild || notes.Count == 0) return;
        if (style == null)
        {
            style = new GUIStyle(GUI.skin.box) { alignment = TextAnchor.UpperLeft, wordWrap = true };
            style.fontSize = Mathf.Max(14, Screen.height / 45);
            style.normal.textColor = Color.white;
        }
        float w = Screen.width * 0.62f;
        float h = style.fontSize * 1.35f * (notes.Count + 1) + 16;
        GUI.Box(new Rect(8, 8, w, h), "Audio bridge (test)\n" + string.Join("\n", notes.ToArray()), style);
    }

    // ---- Called by the plugin (UnitySendMessage) --------------------------------------

    private void OnBridgeReady(string unused) { Ready?.Invoke(); }
    private void OnBridgeSpeechBegan(string unused) { SpeechBegan?.Invoke(); }
    private void OnBridgeResult(string text) { Result?.Invoke(text ?? ""); }
    private void OnBridgeError(string code) { Error?.Invoke(code ?? ""); }
    private void OnBridgeNote(string text) { Note(text ?? ""); }

    void OnDestroy()
    {
        Cancel();
        if (instance == this) instance = null;
    }
}
