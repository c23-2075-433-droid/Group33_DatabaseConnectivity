using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Measures frames per second, frame time and memory ON THE PHONE, while the
/// game is actually being played, and writes them to a CSV beside the voice
/// attempt log.
///
/// WHY THIS EXISTS AND NOT THE UNITY PROFILER
///
/// Activity 10 lists FPS, startup time, scene load time and resident memory as
/// unmeasured, and plans to measure them "with the Unity Profiler on a build".
/// That turned out to cost more than it looks: attaching the profiler wants a
/// development build with Autoconnect Profiler, which is not the build that
/// ships, and Activity 9 recorded `enableInternalProfiler: 0` as a reviewed and
/// deliberate setting. Measuring a different binary from the released one is
/// weak evidence, and flipping a setting the security review signed off is
/// worse.
///
/// So the numbers are taken from inside the game instead. This samples frame
/// times the engine is already computing, which costs one float per frame and a
/// sort plus a file append every ten seconds, and it runs in the SAME release
/// build that is handed to a tester. The build configuration is untouched.
///
/// WHAT IT CANNOT DO, AND WHAT COVERS THAT
///
/// Resident memory - the "resident memory" the course asks for, meaning what
/// the operating system says this process holds - cannot be read from inside
/// the process. Unity's managed heap is in the CSV, which is the part this
/// project's own code controls, but the resident figure has to come from
/// outside:
///
///   adb shell dumpsys meminfo ph.edu.uphsl.group33.salinlahi
///
/// Cold startup is the same story: this class can only see the engine, so its
/// startup row begins at the first frame it is alive for. The real
/// launch-to-first-frame figure comes from outside too:
///
///   adb shell am start -W -n ph.edu.uphsl.group33.salinlahi/com.unity3d.player.UnityPlayerActivity
///
/// Docs/perf/DeviceMeasurement.md is the whole procedure, and says which number
/// in the paper comes from which command.
///
/// WHERE THE FILES GO
///
///   Android: /sdcard/Android/data/ph.edu.uphsl.group33.salinlahi/files/
///   Editor:  ~/Library/Application Support/Group33/SALINLAHI/
///
///   salinlahi_performance.csv         one row per sampling window
///   salinlahi_performance_device.txt  what the measurement ran on
///
/// The second file matters as much as the first: an FPS figure with no device
/// named is not a measurement, and the paper has to say which phone.
///
/// Static bootstrap, like MusicPlayerBootstrap and GameplayHUDBootstrap, for
/// the reason they give: the chapter scenes are generated, so a sampler added
/// to a saved scene would be destroyed the next time someone ran its builder.
/// </summary>
public class PerfSampler : MonoBehaviour
{
    /// <summary>
    /// Set false to stop sampling without removing anything. Left on because
    /// the measurement the course asks for has to come off a real play session,
    /// and a session nobody remembered to enable is a session wasted. The cost
    /// is one float per frame and one short file append every ten seconds.
    /// </summary>
    public static bool Enabled = true;

    public const string FileName = "salinlahi_performance.csv";
    public const string DeviceFileName = "salinlahi_performance_device.txt";

    /// <summary>
    /// Seconds per row. Ten is long enough that one hitch does not dominate the
    /// average and short enough to tell the menu from a chapter scene.
    /// </summary>
    public const float WindowSeconds = 10f;

    private const string Header =
        "timestamp,scene,note,windowSeconds,frames,avgFps," +
        "medianFrameMs,p95FrameMs,p99FrameMs,maxFrameMs," +
        "managedHeapMb,unityAllocatedMb,unityReservedMb";

    public static PerfSampler Instance { get; private set; }

    private readonly List<float> frameMs = new List<float>(1024);
    private float windowElapsed;

    // The scene the window's frames were sampled in. Captured with the first
    // frame of the window rather than read at write time: a window is flushed
    // when a scene loads, and by then the active scene is already the new one,
    // so asking then would file every load row under the wrong scene.
    private string windowScene;

    private bool startupRowWritten;
    private string pendingNote;        // written with the next row, then cleared
    private bool reportSceneLoadFrame; // the next frame holds a load stall

    private static bool writeFailed;
    private static bool pathReported;

    public static string FilePath
    {
        get { return Path.Combine(Application.persistentDataPath, FileName); }
    }

    public static string DeviceFilePath
    {
        get { return Path.Combine(Application.persistentDataPath, DeviceFileName); }
    }

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
        WriteDeviceDescription();
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        if (Instance == this) Instance = null;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // A synchronous SceneManager.LoadScene stalls one frame, and that frame
        // is the load cost. Flush what has been gathered so far so the stall
        // lands in a row of its own rather than being averaged into the scene
        // that is ending.
        Flush("ended_by_load_of:" + scene.name);
        reportSceneLoadFrame = true;
    }

    void Update()
    {
        if (!Enabled) return;

        float ms = Time.unscaledDeltaTime * 1000f;

        // The engine's own time to the first frame this object is alive for.
        // NOT cold startup - the operating system's launch, the Unity player's
        // own initialisation and the splash come before this - which is why the
        // note says engine_first_frame and the real figure comes from
        // `adb shell am start -W`.
        if (!startupRowWritten)
        {
            startupRowWritten = true;
            WriteRow(SceneManager.GetActiveScene().name, "engine_first_frame", Time.realtimeSinceStartup, 1,
                     Time.realtimeSinceStartup > 0f ? 1f / Time.realtimeSinceStartup : 0f,
                     Time.realtimeSinceStartup * 1000f, Time.realtimeSinceStartup * 1000f,
                     Time.realtimeSinceStartup * 1000f, Time.realtimeSinceStartup * 1000f);
        }

        if (reportSceneLoadFrame)
        {
            reportSceneLoadFrame = false;
            // One row, one frame: the frame that contained the load.
            WriteRow(SceneManager.GetActiveScene().name, "scene_load_frame",
                     ms / 1000f, 1, ms > 0f ? 1000f / ms : 0f, ms, ms, ms, ms);
            // That frame is not representative of play, so it is deliberately
            // left out of the window below.
            return;
        }

        if (frameMs.Count == 0) windowScene = SceneManager.GetActiveScene().name;
        frameMs.Add(ms);
        windowElapsed += Time.unscaledDeltaTime;

        if (windowElapsed >= WindowSeconds) Flush(null);
    }

    void OnApplicationPause(bool paused)
    {
        // Leaving the app is the last chance to keep a partial window, and on
        // Android it is also the only reliable "we are going away" callback.
        if (paused) Flush("paused");
    }

    void OnApplicationQuit()
    {
        Flush("quit");
    }

    /// <summary>
    /// Writes the gathered window and starts a new one. A note names why the
    /// row was written early; a full window has none.
    /// </summary>
    public void Flush(string note)
    {
        if (frameMs.Count == 0)
        {
            windowElapsed = 0f;
            return;
        }

        frameMs.Sort();

        float total = 0f;
        for (int i = 0; i < frameMs.Count; i++) total += frameMs[i];

        float seconds = total / 1000f;
        float avgFps = seconds > 0f ? frameMs.Count / seconds : 0f;

        WriteRow(windowScene, note ?? pendingNote, seconds, frameMs.Count, avgFps,
                 Percentile(0.50f), Percentile(0.95f), Percentile(0.99f),
                 frameMs[frameMs.Count - 1]);

        pendingNote = null;
        frameMs.Clear();
        windowElapsed = 0f;
    }

    /// <summary>
    /// The frame time at a percentile of the sorted window. p95 and p99 are
    /// here because an average hides stutter: 60 fps average with a 120 ms
    /// worst frame is a game that visibly hitches, and a child notices the
    /// hitch rather than the average.
    /// </summary>
    private float Percentile(float fraction)
    {
        if (frameMs.Count == 0) return 0f;
        int index = Mathf.Clamp(Mathf.CeilToInt(fraction * frameMs.Count) - 1, 0, frameMs.Count - 1);
        return frameMs[index];
    }

    private void WriteRow(string scene, string note, float windowSeconds, int frames, float avgFps,
                          float medianMs, float p95Ms, float p99Ms, float maxMs)
    {
        if (writeFailed) return;

        try
        {
            string path = FilePath;
            bool needsHeader = !File.Exists(path);

            StringBuilder row = new StringBuilder();
            if (needsHeader) row.AppendLine(Header);

            row.Append(DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture)).Append(',')
               .Append(Escape(scene)).Append(',')
               .Append(Escape(note)).Append(',')
               .Append(Num(windowSeconds, "0.###")).Append(',')
               .Append(frames.ToString(CultureInfo.InvariantCulture)).Append(',')
               .Append(Num(avgFps, "0.##")).Append(',')
               .Append(Num(medianMs, "0.##")).Append(',')
               .Append(Num(p95Ms, "0.##")).Append(',')
               .Append(Num(p99Ms, "0.##")).Append(',')
               .Append(Num(maxMs, "0.##")).Append(',')
               .Append(Num(GC.GetTotalMemory(false) / (1024f * 1024f), "0.##")).Append(',')
               .Append(UnityAllocatedMb()).Append(',')
               .Append(UnityReservedMb());
            row.AppendLine();

            File.AppendAllText(path, row.ToString(), Encoding.UTF8);

            if (!pathReported)
            {
                pathReported = true;
                Debug.Log("PerfSampler: writing performance samples to " + path);
            }
        }
        catch (Exception e)
        {
            writeFailed = true;
            Debug.LogWarning("PerfSampler: couldn't write the performance log, carrying on without it: " + e.Message);
        }
    }

    // Unity's own allocator totals are only compiled into development builds.
    // In the release build - the one that is actually tested - these columns are
    // empty on purpose, and `adb shell dumpsys meminfo` supplies the resident
    // figure instead. An empty column is honest; a zero would read as "no
    // memory used".
    private static string UnityAllocatedMb()
    {
#if ENABLE_PROFILER
        return Num(UnityEngine.Profiling.Profiler.GetTotalAllocatedMemoryLong() / (1024f * 1024f), "0.##");
#else
        return "";
#endif
    }

    private static string UnityReservedMb()
    {
#if ENABLE_PROFILER
        return Num(UnityEngine.Profiling.Profiler.GetTotalReservedMemoryLong() / (1024f * 1024f), "0.##");
#else
        return "";
#endif
    }

    /// <summary>
    /// Records what the measurement ran on. An FPS number without a device name
    /// is not evidence, and this is the file to quote in the paper's method.
    /// Overwritten each run, so it always describes the latest session.
    /// </summary>
    private void WriteDeviceDescription()
    {
        try
        {
            StringBuilder text = new StringBuilder();
            text.AppendLine("SALINLAHI performance measurement - device and settings");
            text.AppendLine("written " + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture));
            text.AppendLine();
            text.AppendLine("app version      " + Application.version);
            text.AppendLine("platform         " + Application.platform);
            text.AppendLine("device model     " + SystemInfo.deviceModel);
            text.AppendLine("device name      " + SystemInfo.deviceName);
            text.AppendLine("operating system " + SystemInfo.operatingSystem);
            text.AppendLine("processor        " + SystemInfo.processorType +
                            " x" + SystemInfo.processorCount +
                            " @ " + SystemInfo.processorFrequency + " MHz");
            text.AppendLine("system memory    " + SystemInfo.systemMemorySize + " MB");
            text.AppendLine("graphics         " + SystemInfo.graphicsDeviceName +
                            " (" + SystemInfo.graphicsDeviceType + ")");
            text.AppendLine("graphics memory  " + SystemInfo.graphicsMemorySize + " MB");
            text.AppendLine("screen           " + Screen.width + "x" + Screen.height +
                            " @ " + Screen.currentResolution.refreshRateRatio.value.ToString("0.#", CultureInfo.InvariantCulture) + " Hz");
            text.AppendLine("target frame rate " + Application.targetFrameRate +
                            " (vSyncCount " + QualitySettings.vSyncCount + ")");
            text.AppendLine("development build " + Debug.isDebugBuild);
            text.AppendLine();
            text.AppendLine("The refresh rate above is the ceiling: a 60 Hz screen cannot show");
            text.AppendLine("more than 60 fps, so read the CSV against this number and not");
            text.AppendLine("against 60 by assumption.");

            File.WriteAllText(DeviceFilePath, text.ToString(), Encoding.UTF8);
            Debug.Log("PerfSampler: device description written to " + DeviceFilePath);
        }
        catch (Exception e)
        {
            Debug.LogWarning("PerfSampler: couldn't write the device description: " + e.Message);
        }
    }

    private static string Num(float value, string format)
    {
        return value.ToString(format, CultureInfo.InvariantCulture);
    }

    private static string Escape(string field)
    {
        if (string.IsNullOrEmpty(field)) return "";
        if (field.IndexOfAny(new[] { ',', '"', '\n', '\r' }) < 0) return field;
        return "\"" + field.Replace("\"", "\"\"") + "\"";
    }
}

/// <summary>
/// Puts a PerfSampler in the game once, in every scene, without any scene
/// needing to know about it - the same shape as MusicPlayerBootstrap, and for
/// the same reason: the chapter scenes are generated by builder scripts.
/// </summary>
public static class PerfSamplerBootstrap
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Install()
    {
        if (!PerfSampler.Enabled) return;
        if (PerfSampler.Instance != null) return;

        GameObject go = new GameObject("PerfSampler");
        go.AddComponent<PerfSampler>();
    }
}
