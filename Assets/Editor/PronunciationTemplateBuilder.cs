using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;

/// <summary>
/// Builds PronunciationTemplateData.cs: for every ambiguous word
/// (AmbiguousWords), the frames of Teacher Rachel's recording, the frames of
/// the word's OTHER pronunciation, and the word's calibrated Matches / Other
/// lines.
///
/// THE OTHER PRONUNCIATION comes from real recordings when there are any
/// (Assets/Audio/VO/alt/word_WORD_alt*.wav), and always also from Rachel's
/// own recording with its stress moved, two different ways.
///
/// CALIBRATION, per word, under every simulated phone and voice
/// (PronunciationReferenceBuilder.Conditions):
///   right  Rachel's recording;
///   wrong  the other pronunciation at full and three-quarter strength (both
///          ways), and every real alternative recording;
///   half   the stress moved only halfway - neither word clearly.
/// Matches starts above the highest score any wrong or half example reached;
/// Other starts below the lowest score Rachel's own reached. Trusted when
/// Rachel passes everywhere and no wrong example is ever accepted.
///
/// Plain C#, so Tools/stresstests runs it without Unity.
/// </summary>
public static class PronunciationTemplateBuilder
{
    public const float WrongMargin = 0.15f, HalfMargin = 0.05f;

    public sealed class WordResult
    {
        public PronunciationTemplate.Reference reference;
        public int conditions, ownMatched, wrongOther, wrongAccepted, wrongTotal, halfAccepted;
        public float ownLowest, wrongHighest;
        /// <summary>The other pronunciation's frames, one group (all voice heights) per recording.</summary>
        public List<List<PronunciationTemplate.Frames>> otherGroups;
    }

    public sealed class Result
    {
        public SortedDictionary<string, WordResult> rows = new SortedDictionary<string, WordResult>(StringComparer.OrdinalIgnoreCase);
        public List<string> problems = new List<string>();
    }

    public static string AltDirectory(string voDirectory) { return Path.Combine(voDirectory, "alt"); }

    public static List<string> RealAlternatives(string voDirectory, string word)
    {
        string dir = AltDirectory(voDirectory);
        if (!Directory.Exists(dir)) return new List<string>();
        return Directory.GetFiles(dir, "word_" + word.ToLowerInvariant() + "_alt*.wav").OrderBy(f => f).ToList();
    }

    private static PronunciationTemplate.Frames Frames(float[] x, int sr)
    {
        string why;
        return PronunciationTemplate.Extract(SpeechFeatures.Of(x, sr), out why);
    }

    /// <summary>Voice heights each reference is kept at: as recorded, and raised like a child's.</summary>
    public static readonly float[] VoiceHeights = { 1f, 1.25f, 1.45f };

    /// <summary>A recording's frames at every voice height (null if the word cannot be found in it).</summary>
    private static List<PronunciationTemplate.Frames> AtHeights(float[] x, int sr)
    {
        List<PronunciationTemplate.Frames> list = new List<PronunciationTemplate.Frames>();
        foreach (float h in VoiceHeights)
        {
            PronunciationTemplate.Frames f = Frames(h == 1f ? x : StressReferenceBuilder.Resample(x, h), sr);
            if (f == null) return null;
            list.Add(f);
        }
        return list;
    }

    public static Result Build(string voDirectory)
    {
        Result result = new Result();
        foreach (AmbiguousWords.Entry e in AmbiguousWords.All)
        {
            string path = StressReferenceBuilder.WavPath(voDirectory, e.word);
            if (!File.Exists(path)) { result.problems.Add(e.word + ": no recording of Rachel"); continue; }
            int sr;
            float[] audio = WavLevelStream.ReadMono(path, out sr);
            List<float[]> real = new List<float[]>();
            foreach (string f in RealAlternatives(voDirectory, e.word))
            {
                int r2;
                float[] a = WavLevelStream.ReadMono(f, out r2);
                real.Add(SpeechFeatures.Resample(a, r2, sr));
            }
            string why;
            WordResult w = BuildWord(e, audio, sr, real, out why);
            if (w == null) { result.problems.Add(e.word + ": " + why); continue; }
            result.rows[e.word] = w;
        }
        return result;
    }

    /// <summary>The other pronunciation made from Rachel's recording, at the given strength, both ways.</summary>
    public static List<float[]> MovedVersions(float[] audio, int sr, StressPattern p, float strength)
    {
        List<float[]> list = new List<float[]>();
        foreach (int other in PronunciationAssessor.Neighbours(p))
        {
            float[] m = PronunciationReferenceBuilder.MoveStress(audio, sr, p, other, strength);
            if (m != null) list.Add(m);
        }
        // The older way of moving the stress cuts the syllables at the deepest
        // dip, which lands in the wrong place on some words (Sulat, Kumusta);
        // there its "other pronunciation" is not one. Used only where its own
        // references show it finds the syllables.
        if (RawStressReferences.For(p).enforced) list.Add(StressReferenceBuilder.MoveStressRaw(audio, sr, p, strength));
        return list;
    }

    public static WordResult BuildWord(AmbiguousWords.Entry e, float[] audio, int sr, List<float[]> realAlternatives, out string whyNot)
    {
        whyNot = null;
        StressPattern p = StressPattern.For(e.word);
        if (p == null) { whyNot = "no stress pattern"; return null; }

        List<List<PronunciationTemplate.Frames>> otherGroups = new List<List<PronunciationTemplate.Frames>>();
        PronunciationTemplate.Reference reference = new PronunciationTemplate.Reference
        {
            word = e.word, intended = e.intended, alternative = e.alternative,
            rachel = AtHeights(audio, sr),
            realAlternatives = realAlternatives.Count,
            // Wide open while measuring: every answer comes back Unclear, with its scores.
            acceptFrom = float.PositiveInfinity, rejectFrom = float.NegativeInfinity,
        };
        if (reference.rachel == null) { whyNot = "the word could not be found in Rachel's recording"; return null; }

        List<float[]> fullMoves = MovedVersions(audio, sr, p, 1f);
        foreach (float[] m in fullMoves.Concat(realAlternatives))
        {
            List<PronunciationTemplate.Frames> f = AtHeights(m, sr);
            if (f != null) { reference.others.AddRange(f); otherGroups.Add(f); }
        }
        if (reference.others.Count == 0) { whyNot = "no other pronunciation could be made"; return null; }

        List<float[]> wrong = new List<float[]>(fullMoves);
        wrong.AddRange(MovedVersions(audio, sr, p, 0.75f));
        wrong.AddRange(realAlternatives);
        List<float[]> half = MovedVersions(audio, sr, p, 0.5f);

        WordResult w = new WordResult { reference = reference, ownLowest = float.MaxValue, wrongHighest = float.MinValue, otherGroups = otherGroups };
        float halfHighest = float.MinValue;
        PronunciationTemplate.Override(reference);
        foreach (PronunciationReferenceBuilder.Condition c in PronunciationReferenceBuilder.Conditions)
        {
            var own = PronunciationTemplate.Assess(PronunciationReferenceBuilder.Apply(audio, sr, c), sr, reference);
            if (!float.IsNaN(own.high)) w.ownLowest = Math.Min(w.ownLowest, own.high);
            foreach (float[] x in wrong)
            {
                var m = PronunciationTemplate.Assess(PronunciationReferenceBuilder.Apply(x, sr, c), sr, reference);
                if (!float.IsNaN(m.low)) w.wrongHighest = Math.Max(w.wrongHighest, m.low);
            }
            foreach (float[] x in half)
            {
                var m = PronunciationTemplate.Assess(PronunciationReferenceBuilder.Apply(x, sr, c), sr, reference);
                if (!float.IsNaN(m.low)) halfHighest = Math.Max(halfHighest, m.low);
            }
        }
        reference.acceptFrom = Math.Max(0.05f, Math.Max(w.wrongHighest + WrongMargin, halfHighest + HalfMargin));
        reference.rejectFrom = Math.Min(-0.05f, w.ownLowest - WrongMargin);

        // Judge the examples with the calibrated lines.
        foreach (PronunciationReferenceBuilder.Condition c in PronunciationReferenceBuilder.Conditions)
        {
            w.conditions++;
            if (PronunciationTemplate.Assess(PronunciationReferenceBuilder.Apply(audio, sr, c), sr, reference).verdict == PronunciationTemplate.Verdict.Matches)
                w.ownMatched++;
            foreach (float[] x in fullMoves.Concat(realAlternatives))
            {
                w.wrongTotal++;
                var v = PronunciationTemplate.Assess(PronunciationReferenceBuilder.Apply(x, sr, c), sr, reference).verdict;
                if (v == PronunciationTemplate.Verdict.Other) w.wrongOther++;
                if (v == PronunciationTemplate.Verdict.Matches) w.wrongAccepted++;
            }
            foreach (float[] x in half)
                if (PronunciationTemplate.Assess(PronunciationReferenceBuilder.Apply(x, sr, c), sr, reference).verdict == PronunciationTemplate.Verdict.Matches)
                    w.halfAccepted++;
        }
        reference.trusted = w.ownMatched == w.conditions && w.wrongAccepted == 0 && w.halfAccepted == 0;
        return w;
    }

    public static string Generate(Result r)
    {
        CultureInfo inv = CultureInfo.InvariantCulture;
        StringBuilder sb = new StringBuilder();
        sb.AppendLine("// GENERATED by PronunciationTemplateBuilder from Assets/Audio/VO/word_*.wav (and alt/).");
        sb.AppendLine("// Do not edit by hand: re-run Tools/test_stress_rules.sh generate (or Tools > SALINLAHI >");
        sb.AppendLine("// Rebuild Stress References) after re-recording a word. See PronunciationTemplate.cs.");
        sb.AppendLine();
        sb.AppendLine("public static partial class PronunciationTemplate");
        sb.AppendLine("{");
        sb.AppendLine("    // word, Matches from, Other from, trusted, real alternative recordings, Rachel's frames, other pronunciations' frames");
        sb.AppendLine("    private static readonly Row[] Rows =");
        sb.AppendLine("    {");
        foreach (KeyValuePair<string, WordResult> kv in r.rows)
        {
            WordResult w = kv.Value;
            PronunciationTemplate.Reference x = w.reference;
            sb.AppendLine("        // Rachel matched " + w.ownMatched + "/" + w.conditions + ", other pronunciation caught " + w.wrongOther + "/" +
                          w.wrongTotal + ", accepted " + w.wrongAccepted + ", half accepted " + w.halfAccepted);
            sb.Append("        new Row(\"" + kv.Key + "\", " + x.acceptFrom.ToString("0.000", inv) + "f, " + x.rejectFrom.ToString("0.000", inv) + "f, " +
                      (x.trusted ? "true" : "false") + ", " + x.realAlternatives + ",\n            \"" +
                      string.Join("|", x.rachel.Select(PronunciationTemplate.Encode)) + "\"");
            foreach (List<PronunciationTemplate.Frames> g in w.otherGroups)
                sb.Append(",\n            \"" + string.Join("|", g.Select(PronunciationTemplate.Encode)) + "\"");
            sb.AppendLine("),");
        }
        sb.AppendLine("    };");
        sb.AppendLine("}");
        return sb.ToString();
    }
}
