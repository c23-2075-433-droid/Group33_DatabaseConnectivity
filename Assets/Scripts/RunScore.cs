using System.Collections.Generic;

/// <summary>
/// The running total for one playthrough of a level, kept across its scenes.
///
/// A level is split over several scenes - Bahay is the bedroom, then the
/// hallway and bathroom, with more to come - but the child plays it as one
/// go, so the result they are shown at the end has to be the whole level's.
/// Counting inside a single scene gave "5 / 8" at the end of the bathroom,
/// which quietly dropped Bangon, Tayo and Lakad from the bedroom before it.
///
/// Static rather than a DontDestroyOnLoad object, because there is nothing to
/// keep alive: two ints that need to outlive a scene load. RunScoreCounter
/// adds to this in each scene; SessionScoreTracker reads it when it saves.
/// </summary>
public static class RunScore
{
    /// <summary>Words said correctly so far this run.</summary>
    public static int Correct { get; private set; }

    /// <summary>Every voice attempt so far this run, right or wrong.</summary>
    public static int Attempts { get; private set; }

    private static readonly List<string> practised = new List<string>();

    /// <summary>
    /// The Filipino words said correctly this run, in the order they were
    /// met and without repeats. This is what the level-complete panel shows
    /// and what a parent or teacher would actually want to know - two counts
    /// say how well it went, this says what was learned.
    /// </summary>
    public static IList<string> Practised => practised.AsReadOnly();

    /// <summary>
    /// How many of the attempts landed, 0..1. Used for the star rating, which
    /// is why it reports 0 rather than dividing by zero on an empty run.
    /// </summary>
    public static float Accuracy => Attempts > 0 ? (float)Correct / Attempts : 0f;

    /// <summary>Records one voice attempt.</summary>
    public static void Add(bool wasCorrect)
    {
        Attempts++;
        if (wasCorrect) Correct++;
    }

    /// <summary>
    /// Notes a word the child got right. Kept separate from Add because the
    /// two arrive from different places: the count comes from the recogniser's
    /// verdict, the word from whichever objective was being asked, which only
    /// SceneObjectiveController knows for certain.
    /// </summary>
    public static void NotePractised(string word)
    {
        if (string.IsNullOrWhiteSpace(word)) return;

        string trimmed = word.Trim();
        foreach (string seen in practised)
        {
            if (string.Equals(seen, trimmed, System.StringComparison.OrdinalIgnoreCase)) return;
        }
        practised.Add(trimmed);
    }

    /// <summary>
    /// Starts a fresh run. Called at the start of a level's first scene, and
    /// again once a result has been saved, so replaying never adds to the
    /// previous run's total.
    /// </summary>
    public static void Reset()
    {
        Correct = 0;
        Attempts = 0;
        practised.Clear();
    }
}
