// SALINLAHI CPU probe for Activity 10.
// Compiles the SHIPPING PronunciationChecker.cs and WordVariants.cs straight
// out of Assets/Scripts - no copy, no edit - and measures what one voice
// attempt costs the CPU, plus the functional checks the judgement must pass.
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

static class Program
{
    static int failures = 0;

    static void Check(string spoken, string target, bool expected, string why)
    {
        bool actual = PronunciationChecker.IsCorrect(spoken, target);
        bool ok = actual == expected;
        if (!ok) failures++;
        Console.WriteLine($"{(ok ? "PASS" : "FAIL")}  said \"{spoken}\" for {target,-9} expected {expected,-5} got {actual,-5}  ({why})");
    }

    static void Main()
    {
        string[] words = WordVariants.Words.OrderBy(w => w, StringComparer.Ordinal).ToArray();

        // ---------------- cold cost: first call pays the static init ----------------
        var cold = Stopwatch.StartNew();
        PronunciationChecker.IsCorrect("bangon", "Bangon");
        cold.Stop();
        Console.WriteLine($"[cold] first IsCorrect call (static init + regex): {cold.Elapsed.TotalMilliseconds:F3} ms");
        Console.WriteLine($"[data] taught words in WordVariants: {words.Length}");
        Console.WriteLine();

        // ---------------- functional validation ----------------
        Console.WriteLine("== functional: the judgement itself ==");
        Check("bangon", "Bangon", true,  "the word, said correctly");
        Check("Kuha mo", "Kuha",  true,  "recognizer added a filler word");
        Check("kanin", "Kain",    false, "a different taught word from the same scene");
        Check("sapatas", "Sapatos", false, "wrong vowel on a long word");
        Check("mali", "Kuha",     false, "an unrelated word");
        Check("", "Kuha",         false, "nothing said");

        Console.WriteLine();
        Console.WriteLine("== functional: every word accepted when said exactly ==");
        var notItself = words.Where(w => !PronunciationChecker.IsCorrect(w, w)).ToList();
        Console.WriteLine(notItself.Count == 0
            ? $"PASS  all {words.Length} taught words accepted when said exactly"
            : $"FAIL  not accepted: {string.Join(", ", notItself)}");
        if (notItself.Count > 0) failures++;

        Console.WriteLine();
        Console.WriteLine("== functional: no taught word accepted for another ==");
        var confusable = new List<string>();
        var sweep = Stopwatch.StartNew();
        int pairs = 0;
        foreach (string spoken in words)
            foreach (string target in words)
            {
                if (spoken == target) continue;
                pairs++;
                if (PronunciationChecker.IsCorrect(spoken, target)) confusable.Add($"{spoken} -> {target}");
            }
        sweep.Stop();
        Console.WriteLine(confusable.Count == 0
            ? $"PASS  {pairs} word pairs checked, none confusable"
            : $"FAIL  {confusable.Count} confusable: {string.Join("; ", confusable.Take(10))}");
        if (confusable.Count > 0) failures++;
        Console.WriteLine($"[perf] full {pairs}-pair sweep: {sweep.Elapsed.TotalMilliseconds:F1} ms " +
                          $"({sweep.Elapsed.TotalMilliseconds * 1000.0 / pairs:F2} us per check)");
        Console.WriteLine();

        // ---------------- per-attempt cost ----------------
        // A real attempt is one recognizer result judged against one live
        // target, sometimes with filler words around it.
        string[] spokenSamples = words.Select(w => w).Concat(words.Select(w => w + " po")).ToArray();
        // warm up
        for (int i = 0; i < 20000; i++) PronunciationChecker.IsCorrect(spokenSamples[i % spokenSamples.Length], words[i % words.Length]);

        Console.WriteLine("== per-attempt CPU cost (accepted and rejected mixed) ==");
        var batchUs = new List<double>();
        const int batches = 10, perBatch = 20000;
        long allocBefore = GC.GetAllocatedBytesForCurrentThread();
        for (int b = 0; b < batches; b++)
        {
            var sw = Stopwatch.StartNew();
            for (int i = 0; i < perBatch; i++)
                PronunciationChecker.IsCorrect(spokenSamples[i % spokenSamples.Length], words[i % words.Length]);
            sw.Stop();
            batchUs.Add(sw.Elapsed.TotalMilliseconds * 1000.0 / perBatch);
        }
        long allocAfter = GC.GetAllocatedBytesForCurrentThread();
        batchUs.Sort();
        Console.WriteLine($"[perf] {batches} batches of {perBatch}: min {batchUs.First():F2} us, " +
                          $"median {batchUs[batches / 2]:F2} us, max {batchUs.Last():F2} us per attempt");
        Console.WriteLine($"[mem ] {(allocAfter - allocBefore) / (double)(batches * perBatch):F0} bytes allocated per attempt");
        Console.WriteLine($"[mem ] gen0 collections during {batches * perBatch} attempts: {GC.CollectionCount(0)}");
        Console.WriteLine();
        Console.WriteLine(failures == 0 ? "RESULT: all functional checks passed" : $"RESULT: {failures} functional check group(s) failed");
    }
}
