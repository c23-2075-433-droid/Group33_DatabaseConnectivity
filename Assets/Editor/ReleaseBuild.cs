// Assets/Editor/ReleaseBuild.cs
//
// The one way this project produces a release APK.
//
// Before this existed, builds came out of the editor's Build Settings dialog by
// hand, which is why the repository root holds SALINLAHI_VoiceTest.apk, _v2 and
// _v3: three artifacts nobody can now tie to a commit. Everything a release
// needs to be identifiable is therefore decided here, in code, and recorded
// beside the artifact.
//
// Run it either way:
//   Editor    Tools > SALINLAHI > Build Release APK
//   Terminal  <unity> -batchmode -nographics -quit -projectPath <project> \
//               -executeMethod ReleaseBuild.BuildAndroid -logFile <log>
//
// The version is never set here. It is read from Player Settings (Edit >
// Project Settings > Player > Version), so that field is the single source of
// truth and the APK's name, its BUILD_INFO.txt and the About screen cannot
// disagree with each other.
//
// Android only, deliberately. Voice is the mechanic and the speech recognizer
// (com.yasirkula.speechtotext) exists only on Android and iOS, so a desktop
// build has no way to answer a prompt.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;
using Debug = UnityEngine.Debug;

public static class ReleaseBuild
{
    private const string ArtifactPrefix = "SALINLAHI-v";
    private const string OutputRoot = "Build/Release";

    [MenuItem("Tools/SALINLAHI/Build Release APK")]
    public static void BuildAndroid()
    {
        string projectRoot = Directory.GetParent(Application.dataPath).FullName;
        string version = (PlayerSettings.bundleVersion ?? "").Trim();

        // Pre-flight. Each of these would otherwise be discovered either after
        // a half-hour build or, worse, by whoever is handed the package.
        if (version.Length == 0)
        {
            Fail("Player Settings > Version is empty. Set it to the release version (e.g. 0.1.0) before building.");
            return;
        }

        List<string> scenes = EnabledScenes();
        if (scenes.Count == 0)
        {
            Fail("No scenes are enabled in Build Settings, so the APK would install and open to nothing.");
            return;
        }

        string notesDir = Path.Combine(projectRoot, "Docs/release/v" + version);
        string readme = Path.Combine(notesDir, "README.txt");
        string notes = Path.Combine(notesDir, "RELEASE_NOTES.txt");
        if (!File.Exists(readme) || !File.Exists(notes))
        {
            Fail("Docs/release/v" + version + " must hold README.txt and RELEASE_NOTES.txt before v" + version +
                 " can be packaged. A release nobody can install or read the notes for is not a release.");
            return;
        }

        string artifactName = ArtifactPrefix + version;
        string outDir = Path.Combine(projectRoot, OutputRoot, artifactName);
        string apkPath = Path.Combine(outDir, artifactName + ".apk");
        Directory.CreateDirectory(outDir);

        // APK, not AAB: this is sideloaded onto test phones, not uploaded to
        // the Play Store.
        EditorUserBuildSettings.buildAppBundle = false;
        EditorUserBuildSettings.development = false;
        if (EditorUserBuildSettings.activeBuildTarget != BuildTarget.Android)
        {
            Debug.Log("[SALINLAHI] Switching the active build target to Android. The first switch reimports " +
                      "textures and audio for Android, which takes a while.");
            EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.Android, BuildTarget.Android);
        }

        BuildPlayerOptions options = new BuildPlayerOptions
        {
            scenes = scenes.ToArray(),
            locationPathName = apkPath,
            target = BuildTarget.Android,
            targetGroup = BuildTargetGroup.Android,
            options = BuildOptions.None,
        };

        Debug.Log("[SALINLAHI] Building " + artifactName + ".apk from " + scenes.Count + " scenes -> " + apkPath);
        BuildReport report = BuildPipeline.BuildPlayer(options);
        BuildSummary summary = report.summary;

        if (summary.result != BuildResult.Succeeded)
        {
            Fail("Build " + summary.result + " with " + summary.totalErrors + " error(s) after " +
                 summary.totalTime + ". The errors are above this line in the log.");
            return;
        }

        WriteBuildInfo(outDir, projectRoot, version, artifactName, apkPath, scenes, summary);
        File.Copy(readme, Path.Combine(outDir, "README.txt"), true);
        File.Copy(notes, Path.Combine(outDir, "RELEASE_NOTES.txt"), true);

        Debug.Log("[SALINLAHI] Build succeeded in " + summary.totalTime + ".\n" +
                  "  Package : " + outDir + "\n" +
                  "  APK     : " + artifactName + ".apk (" + Megabytes(apkPath) + ")\n" +
                  "  Also in the folder: README.txt, RELEASE_NOTES.txt, BUILD_INFO.txt\n" +
                  "  Next: zip the folder as " + artifactName + ".zip and attach it to the v" + version + " release.");
    }

    /// <summary>The scenes Build Settings has ticked, in their listed order.</summary>
    private static List<string> EnabledScenes()
    {
        List<string> scenes = new List<string>();
        foreach (EditorBuildSettingsScene scene in EditorBuildSettings.scenes)
        {
            if (scene.enabled && !string.IsNullOrEmpty(scene.path))
            {
                scenes.Add(scene.path);
            }
        }
        return scenes;
    }

    /// <summary>
    /// The file that answers "which source produced this APK?". Written next to
    /// the artifact so the two travel together: a version number on its own
    /// cannot be traced back if the package is renamed or re-zipped.
    /// </summary>
    private static void WriteBuildInfo(string outDir, string projectRoot, string version, string artifactName,
                                       string apkPath, List<string> scenes, BuildSummary summary)
    {
        string dirty = Git("status --porcelain", projectRoot);
        bool clean = dirty.Length == 0 && dirty != "unavailable";

        StringBuilder info = new StringBuilder();
        info.AppendLine("SALINLAHI - build information");
        info.AppendLine("=============================");
        info.AppendLine();
        info.AppendLine("Version            : " + version);
        info.AppendLine("Android versionCode: " + PlayerSettings.Android.bundleVersionCode);
        info.AppendLine("Package id         : " + Application.identifier);
        info.AppendLine("Artifact           : " + artifactName + ".apk");
        info.AppendLine("Artifact size      : " + Megabytes(apkPath));
        info.AppendLine("Built (UTC)        : " + DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss"));
        info.AppendLine("Build time         : " + summary.totalTime);
        info.AppendLine("Unity              : " + Application.unityVersion);
        info.AppendLine("Built on           : " + SystemInfo.operatingSystem);
        info.AppendLine();
        info.AppendLine("Source");
        info.AppendLine("------");
        info.AppendLine("Commit             : " + Git("rev-parse HEAD", projectRoot));
        info.AppendLine("Short commit       : " + Git("rev-parse --short HEAD", projectRoot));
        info.AppendLine("Branch             : " + Git("rev-parse --abbrev-ref HEAD", projectRoot));
        info.AppendLine("Tag                : " + DescribeTag(projectRoot));
        info.AppendLine("Working tree       : " + (clean
            ? "clean - this APK is reproducible from the commit above"
            : "MODIFIED - uncommitted changes were present, so the commit above does NOT fully describe this APK"));
        info.AppendLine();
        info.AppendLine("Scenes included (" + scenes.Count + ", in build order)");
        info.AppendLine("--------------------------------------");
        foreach (string scene in scenes)
        {
            info.AppendLine("  " + scene);
        }

        File.WriteAllText(Path.Combine(outDir, "BUILD_INFO.txt"), info.ToString());
    }

    private static string DescribeTag(string projectRoot)
    {
        string tag = Git("describe --tags --exact-match", projectRoot);
        return tag.Length == 0 ? "(none on this commit - tag it after the release is tested)" : tag;
    }

    private static string Megabytes(string path)
    {
        if (!File.Exists(path))
        {
            return "missing";
        }
        double mb = new FileInfo(path).Length / (1024d * 1024d);
        return mb.ToString("0.0") + " MB";
    }

    /// <summary>
    /// git, for the traceability fields. A missing or unhappy git must not fail
    /// a build, so every failure here degrades to a recorded "unavailable"
    /// rather than throwing.
    /// </summary>
    private static string Git(string args, string workingDir)
    {
        try
        {
            string exe = File.Exists("/usr/bin/git") ? "/usr/bin/git" : "git";
            ProcessStartInfo psi = new ProcessStartInfo(exe, args)
            {
                WorkingDirectory = workingDir,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
            };
            using (Process proc = Process.Start(psi))
            {
                string stdout = proc.StandardOutput.ReadToEnd();
                proc.StandardError.ReadToEnd();
                proc.WaitForExit(15000);
                return stdout.Trim();
            }
        }
        catch (Exception e)
        {
            Debug.LogWarning("[SALINLAHI] Could not run 'git " + args + "': " + e.Message);
            return "unavailable";
        }
    }

    /// <summary>
    /// Stops the release. In batchmode this must also set a non-zero exit code:
    /// Unity otherwise exits 0 on a failed build and a script around it would
    /// happily go on to package and publish nothing.
    /// </summary>
    private static void Fail(string message)
    {
        Debug.LogError("[SALINLAHI] Release build stopped. " + message);
        if (Application.isBatchMode)
        {
            EditorApplication.Exit(1);
        }
    }
}
