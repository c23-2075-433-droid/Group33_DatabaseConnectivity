using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Turns a play session into a saved database record, then shows it back to
/// the player. This is the piece Activity 5 grades: SAVE, RETRIEVE, DISPLAY.
///
/// Flow:
///   1. Every voice attempt raises VoiceCommand.OnAnswerChecked, so this
///      counts attempts and correct answers as the player goes.
///   2. When SceneObjectiveController finishes its last objective, the
///      session result is POSTed to Supabase (SAVE).
///   3. Immediately after a successful save, recent records are fetched back
///      (RETRIEVE) and written into the level-complete panel's text
///      (DISPLAY), which is then shown.
///
/// Saving on "all objectives complete" rather than on reaching the exit
/// trigger is deliberate - it's guaranteed to fire once the words are said,
/// so a record is never lost just because the player didn't walk far enough.
/// </summary>
public class SessionScoreTracker : MonoBehaviour
{
    [Header("References")]
    public VoiceCommand voiceCommand;
    public SceneObjectiveController objectiveController;
    public SupabaseClient supabaseClient;

    [Header("Level Complete UI")]
    [Tooltip("The panel shown when the session ends (LevelComplete_Canvas).")]
    public GameObject levelCompletePanel;
    [Tooltip("Shows this session's result.")]
    public Text scoreText;
    [Tooltip("Shows records retrieved back from the database.")]
    public Text recordsText;

    [Header("Session")]
    [Tooltip("Stored in the 'level' column so records can be told apart later.")]
    public string levelName = "Bahay - Scene 1: Umaga na!";
    [Tooltip("How many past records to pull back and list.")]
    public int recentRecordsToShow = 5;

    private int correctAnswers = 0;
    private int totalAttempts = 0;
    private bool alreadySaved = false;

    void OnEnable()
    {
        if (voiceCommand != null) voiceCommand.OnAnswerChecked += HandleAnswerChecked;
        if (objectiveController != null) objectiveController.onAllObjectivesComplete.AddListener(HandleSessionComplete);
    }

    void OnDisable()
    {
        if (voiceCommand != null) voiceCommand.OnAnswerChecked -= HandleAnswerChecked;
        if (objectiveController != null) objectiveController.onAllObjectivesComplete.RemoveListener(HandleSessionComplete);
    }

    // ---- 1. Collect the data the session generates ----

    private void HandleAnswerChecked(string spokenText, bool isCorrect)
    {
        totalAttempts++;
        if (isCorrect) correctAnswers++;
        Debug.Log("SessionScoreTracker: " + correctAnswers + " correct / " + totalAttempts + " attempts.");
    }

    // ---- 2. SAVE ----

    private void HandleSessionComplete()
    {
        // onAllObjectivesComplete can fire again if a scene re-runs its
        // sequence; only ever save one record per playthrough.
        if (alreadySaved) return;
        alreadySaved = true;

        ScoreInsert record = new ScoreInsert
        {
            player_name = NicknameManager.Nickname,
            score = correctAnswers,
            attempts = totalAttempts,
            level = levelName,
            remarks = correctAnswers + "/" + totalAttempts + " correct",
        };

        ShowPanel();
        SetText(scoreText, "Player: " + record.player_name +
                            "\nScore: " + record.score + " / " + record.attempts + " attempts");
        SetText(recordsText, "Saving your result...");

        if (supabaseClient == null)
        {
            SetText(recordsText, "No SupabaseClient assigned - result not saved.");
            return;
        }

        supabaseClient.SaveScore(record, (success, error) =>
        {
            if (!success)
            {
                SetText(recordsText, "Could not save result.\n" + error);
                return;
            }
            FetchAndDisplayRecords();
        });
    }

    // ---- 3. RETRIEVE + DISPLAY ----

    private void FetchAndDisplayRecords()
    {
        SetText(recordsText, "Loading recent scores...");

        supabaseClient.FetchRecentScores(recentRecordsToShow, (records, error) =>
        {
            if (error != null)
            {
                SetText(recordsText, "Saved, but could not load recent scores.\n" + error);
                return;
            }
            SetText(recordsText, FormatRecords(records));
        });
    }

    private string FormatRecords(List<ScoreRecord> records)
    {
        if (records == null || records.Count == 0) return "No saved scores yet.";

        StringBuilder sb = new StringBuilder();
        sb.AppendLine("RECENT SCORES");
        foreach (ScoreRecord r in records)
        {
            sb.AppendLine(r.player_name + " - " + r.score + "/" + r.attempts +
                           "  (" + FormatDate(r.created_at) + ")");
        }
        return sb.ToString();
    }

    /// <summary>Trims Postgres' full timestamp down to something readable on a panel.</summary>
    private static string FormatDate(string timestamp)
    {
        if (string.IsNullOrEmpty(timestamp)) return "";
        return System.DateTime.TryParse(timestamp, out System.DateTime parsed)
            ? parsed.ToString("MMM d, h:mm tt")
            : timestamp;
    }

    private void ShowPanel()
    {
        if (levelCompletePanel != null) levelCompletePanel.SetActive(true);
    }

    private static void SetText(Text target, string value)
    {
        if (target != null) target.text = value;
    }
}
