using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// The words a child just used, read back to them with what each one means.
///
/// The game has been collecting this all along and never showed it. RunScore
/// notes every word said correctly, and NarratorScript.GlossFor already turns
/// "Bangon" into "Get up" for the word signpost - but at the end of a scene the
/// child saw only two numbers and a row of stars, which say how it went and not
/// what it was about.
///
/// Shown after the summary rather than instead of it. The summary is the
/// reward; this is the lesson, and a child who has just been told they did well
/// is in the right frame of mind to read five words back.
///
/// The list is this SCENE's, taken from the objectives it asked for, not
/// RunScore's. RunScore is the whole level's and would repeat Level 1's words
/// at the end of every scene in it. It is also wiped by SessionScoreTracker
/// before a level-ending panel has finished opening, which is the same trap
/// SceneResultPanel.ShowWithTotals exists to avoid.
///
/// Built by WordsLearnedPanelBuilder, which wires the references below.
/// </summary>
public class WordsLearnedPanel : MonoBehaviour
{
    [Header("Source")]
    [Tooltip("The words this scene asked for. If empty, one is looked up.")]
    public SceneObjectiveController objectiveController;

    [Header("Panel")]
    [Tooltip("Switched on when the summary hands over. Starts off.")]
    public GameObject panelRoot;

    /// <summary>
    /// One row of the board: the Filipino word and what it means. Both are
    /// placed on a plate that is painted into the art, so a row with no word
    /// is switched off rather than left showing an empty plate.
    /// </summary>
    [System.Serializable]
    public class Row
    {
        public GameObject root;
        public Text word;
        public Text meaning;
    }

    [Header("Rows")]
    [Tooltip("Five, top to bottom. A scene with fewer words uses fewer.")]
    public Row[] rows;

    [Header("Buttons")]
    public Button continueButton;
    public Button closeButton;

    private readonly List<string> words = new List<string>();
    private int page;
    private System.Action onFinished;
    private System.Action onClose;

    /// <summary>How many rows the board has, and so how many words a page holds.</summary>
    private int PageSize => rows != null ? rows.Length : 0;

    void Awake()
    {
        if (objectiveController == null)
            objectiveController = FindFirstObjectByType<SceneObjectiveController>();

        // The same trap SceneResultPanel guards against, and for the same
        // reason. Pointed at its own GameObject, the line below would fire the
        // moment Show() switched the panel on - Unity runs Awake during that
        // SetActive - and turn it straight back off, leaving the summary hidden
        // and nothing in its place.
        if (panelRoot == gameObject)
        {
            Debug.LogError("[SALINLAHI] WordsLearnedPanel.panelRoot points at its own " +
                           "GameObject. Switching the panel on would run this Awake and " +
                           "switch it off again. Point it at a child instead.");
            panelRoot = null;
        }

        if (panelRoot != null) panelRoot.SetActive(false);
    }

    /// <summary>
    /// Opens on the first page. <paramref name="finished"/> runs when the last
    /// page is dismissed - that is where the summary resumes whatever it was
    /// going to do - and <paramref name="closed"/> when the cross is pressed.
    ///
    /// Both are handed in rather than navigating here, so every route out of a
    /// finished scene still goes through SceneResultPanel.Go and is still
    /// marked complete on the level select.
    /// </summary>
    public void Show(System.Action finished, System.Action closed)
    {
        onFinished = finished;
        onClose = closed;

        CollectWords();

        // Nothing to review - an unusual scene, or one whose objectives were
        // never filled in. Carry straight on rather than opening an empty board.
        if (words.Count == 0 || PageSize == 0)
        {
            if (onFinished != null) onFinished();
            return;
        }

        page = 0;
        WireButtons();
        FillPage();
        if (panelRoot != null) panelRoot.SetActive(true);
    }

    /// <summary>
    /// The scene's words, in the order they were asked, with repeats dropped.
    /// A word can legitimately appear twice in one scene - Banyo asks for Lakad
    /// early and again at the door - and listing it twice would read as two
    /// different lessons.
    /// </summary>
    private void CollectWords()
    {
        words.Clear();
        if (objectiveController == null || objectiveController.objectives == null) return;

        foreach (SceneObjectiveController.SceneObjective objective in objectiveController.objectives)
        {
            if (objective == null || string.IsNullOrWhiteSpace(objective.word)) continue;

            string word = objective.word.Trim();
            bool seen = false;
            foreach (string had in words)
            {
                if (string.Equals(had, word, System.StringComparison.OrdinalIgnoreCase))
                {
                    seen = true;
                    break;
                }
            }
            if (!seen) words.Add(word);
        }
    }

    private int PageCount
    {
        get
        {
            if (PageSize <= 0) return 0;
            return (words.Count + PageSize - 1) / PageSize;
        }
    }

    private bool OnLastPage => page >= PageCount - 1;

    private void FillPage()
    {
        for (int slot = 0; slot < rows.Length; slot++)
        {
            Row row = rows[slot];
            if (row == null) continue;

            int index = page * PageSize + slot;
            bool used = index < words.Count;

            if (row.root != null) row.root.SetActive(used);
            if (!used) continue;

            string word = words[index];
            if (row.word != null) row.word.text = word.ToUpperInvariant();

            // No meaning recorded: show the word on its own rather than leave a
            // gap where English should be. Same rule the word signpost follows.
            if (row.meaning != null)
            {
                string gloss = NarratorScript.GlossFor(word);
                row.meaning.text = gloss.ToUpperInvariant();
                row.meaning.gameObject.SetActive(!string.IsNullOrEmpty(gloss));
            }
        }
    }

    private void WireButtons()
    {
        if (continueButton != null)
        {
            continueButton.onClick.RemoveAllListeners();
            continueButton.onClick.AddListener(Advance);
        }

        if (closeButton != null)
        {
            closeButton.onClick.RemoveAllListeners();
            closeButton.onClick.AddListener(() =>
            {
                if (panelRoot != null) panelRoot.SetActive(false);
                if (onClose != null) onClose();
            });
        }
    }

    /// <summary>
    /// CONTINUE turns the page while there is one, and leaves on the last.
    /// Only Banyo ever turns a page - it teaches nine words against a board of
    /// five - but a scene that grows past five later needs no new code.
    /// </summary>
    private void Advance()
    {
        if (!OnLastPage)
        {
            page++;
            FillPage();
            return;
        }

        if (panelRoot != null) panelRoot.SetActive(false);
        if (onFinished != null) onFinished();
    }
}
