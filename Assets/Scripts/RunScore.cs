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

    /// <summary>Records one voice attempt.</summary>
    public static void Add(bool wasCorrect)
    {
        Attempts++;
        if (wasCorrect) Correct++;
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
    }
}
