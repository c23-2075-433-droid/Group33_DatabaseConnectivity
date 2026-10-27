using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Turns a play session into a saved database record, then shows it back to
/// the player. This is the piece Activity 5 grades: SAVE, RETRIEVE, DISPLAY.
///
/// Flow:
///   1. RunScoreCounter adds every voice attempt, in every scene of the
///      level, to RunScore. This reads that total rather than counting for
///      itself, so the saved result covers the whole level instead of just
///      the scene it happens to sit in.
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
    public SceneObjectiveController objectiveController;
    public SupabaseClient supabaseClient;

    [Header("Level Complete UI")]
    [Tooltip("The panel shown when the session ends (LevelComplete_Canvas).")]
    public GameObject levelCompletePanel;

    [Tooltip("The scene summary, when the level ends on one. It is handed the " +
             "level's totals and opens itself; levelCompletePanel is then left " +
             "empty so the two do not both try to show something.")]
    public SceneResultPanel resultPanel;
    [Tooltip("Shows who played.")]
    public Text scoreText;
    [Tooltip("Shows records retrieved back from the database.")]
    public Text recordsText;

    [Header("Level Complete Statistics")]
    [Tooltip("Words said correctly, e.g. \"8 of 9\".")]
    public Text wordsValueText;
    [Tooltip("Total voice attempts this run.")]
    public Text triesValueText;
    [Tooltip("The Filipino words practised, listed.")]
    public Text learnedValueText;
    [Tooltip("Star1..Star3, lit according to accuracy. Left empty, no stars are shown.")]
    public Image[] stars;
    [Tooltip("Lit star art.")]
    public Sprite starOnSprite;
    [Tooltip("Unlit star art.")]
    public Sprite starOffSprite;

    [Header("Session")]
    [Tooltip("Stored in the 'level' column so records can be told apart later.")]
    public string levelName = "Bahay - Scene 1: Umaga na!";

    [Tooltip("1-based number of this level on the journey map. Finishing it " +
             "unlocks the next node and earns this level's badge. Leave at 0 " +
             "in a scene that does not end a level.")]
    public int levelIndex = 0;
    [Tooltip("How many past records to pull back and list.")]
    public int recentRecordsToShow = 5;

    private bool alreadySaved = false;

    void OnEnable()
    {
        if (objectiveController != null) objectiveController.onAllObjectivesComplete.AddListener(HandleSessionComplete);
    }

    void OnDisable()
    {
        if (objectiveController != null) objectiveController.onAllObjectivesComplete.RemoveListener(HandleSessionComplete);
    }

    /// <summary>
    /// Fills the three statistic rows and the star rating.
    ///
    /// These replace the "total score / time / gems collected" of the mock-up
    /// this panel is modelled on. None of those exist here: the game keeps no
    /// points, collects nothing, and deliberately does not time a child who is
    /// sounding out a word in a second language. What it does know is how many
    /// words landed, how many tries that took, and which words they were -
    /// and the last of those is the one a parent or a teacher would ask about.
    /// </summary>
    private void FillStatistics(int correct, int attempts, List<string> practised, float accuracy)
    {
        SetText(wordsValueText, correct + " of " + Mathf.Max(correct, practised.Count));
        SetText(triesValueText, attempts.ToString());
        SetText(learnedValueText, DescribePractised(practised));

        if (stars == null || stars.Length == 0) return;

        // Earned on accuracy, not speed. Every word right first time is three
        // stars; the third is worth reaching for without being punishing,
        // because a child who needed a second try still said the word.
        int earned = accuracy >= 0.95f ? 3 : accuracy >= 0.7f ? 2 : 1;
        for (int i = 0; i < stars.Length; i++)
        {
            if (stars[i] == null) continue;
            Sprite wanted = i < earned ? starOnSprite : starOffSprite;
            if (wanted != null) stars[i].sprite = wanted;
        }
    }

    /// <summary>
    /// Every word practised, named. There is no cap: the whole point of this
    /// line is for a child to see what they said and a parent to see what was
    /// learned, and "+1 more" withholds exactly that. The panel gives the row
    /// the lower third of the board to wrap into instead.
    /// </summary>
    private static string DescribePractised(List<string> practised)
    {
        if (practised == null || practised.Count == 0) return "none yet";
        return string.Join(", ", practised.ToArray());
    }

    // ---- 1. SAVE ----

    private void HandleSessionComplete()
    {
        // onAllObjectivesComplete can fire again if a scene re-runs its
        // sequence; only ever save one record per playthrough.
        if (alreadySaved) return;
        alreadySaved = true;

        // The whole level's total, gathered by RunScoreCounter across every
        // scene, not just the words said in this one.
        int correctAnswers = RunScore.Correct;
        int totalAttempts = RunScore.Attempts;
        // Taken now, because Reset() below empties all of this before the
        // panel is filled in.
        List<string> practised = new List<string>(RunScore.Practised);
        float accuracy = RunScore.Accuracy;

        ScoreInsert record = new ScoreInsert
        {
            player_name = NicknameManager.Nickname,
            score = correctAnswers,
            attempts = totalAttempts,
            level = levelName,
            remarks = correctAnswers + "/" + totalAttempts + " correct",
        };

        // Playing again starts from zero rather than adding to this run.
        RunScore.Reset();

        // Finishing the level earns its badge and opens the next node on the
        // map. Nothing called this before, so the map stayed locked however
        // many times Bahay was completed.
        if (levelIndex > 0) LevelProgress.MarkComplete(levelIndex);

        // The last scene of a level does not fade on to another scene, so it
        // marks itself here instead of in SceneFader.
        LevelProgress.MarkSceneComplete(
            UnityEngine.SceneManagement.SceneManager.GetActiveScene().name);

        // The summary is given the figures captured above rather than left to
        // read RunScore, which Reset() has already emptied by this point.
        if (resultPanel != null) resultPanel.ShowWithTotals(correctAnswers, totalAttempts);
        ShowPanel();
        SetText(scoreText, record.player_name);
        FillStatistics(correctAnswers, totalAttempts, practised, accuracy);
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

    // ---- 2. RETRIEVE + DISPLAY ----

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
