using System.Collections.Generic;

/// <summary>
/// How Teacher Rachel stresses each stress-checked word, as the loudness
/// centroid StressAnalyzer measures (0 = the start of the word, 1 = its end).
///
/// The numbers live in StressReferenceData.cs, which is GENERATED from the
/// narrator's own recordings (Assets/Audio/VO/word_&lt;word&gt;.wav) by
/// Tools > SALINLAHI > Rebuild Stress References - or, without opening Unity,
/// by Tools/test_stress_rules.sh generate. Each recording is turned into the
/// level readings a phone's recognizer would report, at several report rates
/// and loudnesses, and measured exactly as a child's attempt is.
///
/// Per word there are two numbers - Rachel's own stress, and the same
/// recording with the stress moved to the other syllable - and a flag:
///
///   enforced   true when, across every simulated device setting, her own
///              recording always lands on the right side of the halfway mark
///              AND the moved one always lands on the wrong side. A word that
///              fails that is one the loudness alone cannot judge reliably
///              from her recording, and enforcing it would mark children
///              wrong for saying it right. Such a word is still measured and
///              logged on every attempt, but its letters decide - until it is
///              re-recorded (more clearly stressed) and passes.
///
/// A stress-checked word with no recording at all has nothing to calibrate
/// against: it falls back to the class averages, unenforced, and the rebuild
/// tool names it.
/// </summary>
public static partial class StressReferences
{
    public struct Entry
    {
        public float reference;
        public float moved;
        public bool enforced;
        public bool measured;
    }

    private static Dictionary<string, Entry> table;

    public static Entry For(StressPattern pattern)
    {
        if (table == null)
        {
            Dictionary<string, Entry> t = new Dictionary<string, Entry>(System.StringComparer.OrdinalIgnoreCase);
            for (int i = 0; i < Rows.Length; i++) t[Rows[i].word] = Rows[i].ToEntry();
            table = t;
        }

        Entry e;
        if (table.TryGetValue(pattern.Word, out e)) return e;

        // No recording: the class averages, never enforced.
        e = new Entry();
        if (pattern.IsFinal) { e.reference = AverageFinal; e.moved = AverageInitial; }
        else { e.reference = AverageInitial; e.moved = AverageFinal; }
        e.enforced = false;
        e.measured = false;
        return e;
    }

    /// <summary>One generated row.</summary>
    private struct Row
    {
        public string word;
        public float reference;
        public float moved;
        public bool enforced;

        public Row(string word, float reference, float moved, bool enforced)
        {
            this.word = word;
            this.reference = reference;
            this.moved = moved;
            this.enforced = enforced;
        }

        public Entry ToEntry()
        {
            return new Entry { reference = reference, moved = moved, enforced = enforced, measured = true };
        }
    }
}
