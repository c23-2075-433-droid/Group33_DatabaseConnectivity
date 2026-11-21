using System.Collections.Generic;
using System.Text;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Tools > SALINLAHI > Check Stress Validation: drives a real VoiceCommand
/// through the five cases the stress fix has to get right, using Teacher
/// Rachel's own word_tayo.wav as the correct tayô and the same recording with
/// its stress moved as táyo, and checks what VoiceCommand tells the rest of
/// the game (OnAnswerChecked, OnWrongStress, OnStressUnclear, OnNothingHeard).
///
/// Also runs headless, for a check without opening the Editor:
///
///   Unity -batchmode -nographics -quit -projectPath . \
///         -executeMethod StressCheckTool.RunBatch -logFile -
///
/// Exit code 0 means every case passed.
/// </summary>
public static class StressCheckTool
{
    private const string VoFolder = "Assets/Audio/VO";

    private sealed class Events
    {
        public readonly List<string> log = new List<string>();
        public void Clear() { log.Clear(); }
        public override string ToString() { return log.Count == 0 ? "(nothing)" : string.Join(", ", log.ToArray()); }
    }

    [MenuItem("Tools/SALINLAHI/Check Stress Validation")]
    public static void Run()
    {
        int failures = RunChecks(out string report);
        if (failures == 0) Debug.Log(report);
        else Debug.LogError(report);
    }

    public static void RunBatch()
    {
        int failures = RunChecks(out string report);
        Debug.Log(report);
        EditorApplication.Exit(failures == 0 ? 0 : 1);
    }

    private static int RunChecks(out string report)
    {
        StringBuilder sb = new StringBuilder();
        sb.AppendLine("SALINLAHI stress validation check (VoiceCommand, end to end)");
        int failures = 0;

        int rate;
        float[] tayo = WavLevelStream.ReadMono(StressReferenceBuilder.WavPath(VoFolder, "tayo"), out rate);
        float[] standUp = StressReferenceBuilder.RawCondition(tayo, rate, -50f, 1.25f);
        float[] we = StressReferenceBuilder.RawCondition(
            StressReferenceBuilder.MoveStress(tayo, rate, true, 1f, false, StressReferenceBuilder.MovedPitchSemitones),
            rate, -50f, 1.25f);
        float[] silence = StressReferenceBuilder.RawCondition(new float[rate], rate, -60f, 1f);

        GameObject go = new GameObject("StressCheck VoiceCommand");
        go.hideFlags = HideFlags.HideAndDontSave;
        try
        {
            VoiceCommand vc = go.AddComponent<VoiceCommand>();
            vc.logAttempts = false;
            vc.listenContinuously = false;   // no queued restarts in a check
            vc.currentTargetWord = "Tayo";

            Events ev = new Events();
            vc.OnAnswerChecked += (said, ok) => ev.log.Add(ok ? "correct" : "wrong");
            vc.OnWrongStress += (word, p) => ev.log.Add("wrong-stress");
            vc.OnStressUnclear += (word, why) => ev.log.Add("unclear");
            vc.OnNothingHeard += () => ev.log.Add("nothing-heard");

            failures += Case(sb, ev, "1. Tayô (stand up) -> Correct",
                () => vc.SimulateSpeech("tayo", standUp, rate), "correct");
            failures += Case(sb, ev, "2. Táyo (we) -> Wrong, with the wrong-stress feedback",
                () => vc.SimulateSpeech("tayo", we, rate), "wrong, wrong-stress");
            failures += Case(sb, ev, "2b. \"kain tayo\" -> Wrong",
                () => vc.SimulateSpeech("kain tayo", standUp, rate), "wrong");
            failures += Case(sb, ev, "3. Incorrect pronunciation (\"tayong\") -> Wrong",
                () => vc.SimulateSpeech("tayong", standUp, rate), "wrong");
            failures += Case(sb, ev, "4. No recognized speech -> asked to try again, not scored",
                () => ((ISpeechToTextListener)vc).OnBeginningOfSpeech(), "");
            ev.Clear();
            ((ISpeechToTextListener)vc).OnBeginningOfSpeech();
            ((ISpeechToTextListener)vc).OnResultReceived("", 7);   // ERROR_NO_MATCH
            failures += Expect(sb, "   ...the recognizer's no-match error -> nothing-heard", ev, "nothing-heard");
            failures += Case(sb, ev, "4b. Words heard but the recording has no voice -> balanced: the word decides",
                () => vc.SimulateSpeech("tayo", silence, rate), "correct");
            failures += Case(sb, ev, "5a. Wrong stress first...",
                () => vc.SimulateSpeech("tayo", we, rate), "wrong, wrong-stress");
            failures += Case(sb, ev, "5b. ...then correct on the second attempt -> Correct",
                () => vc.SimulateSpeech("tayo", standUp, rate), "correct");

            vc.currentTargetWord = "Bangon";
            failures += Case(sb, ev, "Balanced (default): Bangon with no audio -> the word decides",
                () => vc.SimulateSpeech("bangon"), "correct");
            vc.stressCheck = VoiceCommand.StressCheck.StrictOnEveryWord;
            failures += Case(sb, ev, "Strict on every word: Bangon typed with no audio -> asked again",
                () => vc.SimulateSpeech("bangon"), "unclear");
            vc.stressCheck = VoiceCommand.StressCheck.Balanced;
            failures += Case(sb, ev, "Unpaired word, wrong word -> Wrong",
                () => vc.SimulateSpeech("tayo"), "wrong");
        }
        finally
        {
            Object.DestroyImmediate(go);
        }

        sb.AppendLine(failures == 0 ? "RESULT: all checks passed." : "RESULT: " + failures + " check(s) FAILED.");
        report = sb.ToString();
        return failures;
    }

    private static int Case(StringBuilder sb, Events ev, string name, System.Action act, string expected)
    {
        ev.Clear();
        act();
        return Expect(sb, name, ev, expected);
    }

    private static int Expect(StringBuilder sb, string name, Events ev, string expected)
    {
        string got = string.Join(", ", ev.log.ToArray());
        bool ok = got == expected;
        sb.AppendLine((ok ? "  PASS  " : "  FAIL  ") + name + (ok ? "" : "   expected [" + expected + "], got [" + got + "]"));
        return ok ? 0 : 1;
    }
}
