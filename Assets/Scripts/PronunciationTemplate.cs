using System;
using System.Collections.Generic;
using System.Globalization;

/// <summary>
/// Compares how a player said an AMBIGUOUS word (AmbiguousWords) with Teacher
/// Rachel's actual recording of it, and with recordings of the word's OTHER
/// pronunciation - and says which of the two the answer is closer to.
///
/// WHY A WHOLE-WORD COMPARISON
///
/// PronunciationAssessor first cuts the answer into syllables. On a real voice
/// that cut can fail (it lost the first syllable of every Bángon in the
/// group's own labelled recordings). This class never cuts anything: it lines
/// the whole answer up with the whole reference, moment by moment, and
/// compares the two along that line-up. It is the PRIMARY audio evidence for
/// ambiguous words; the syllable assessor is the second opinion.
///
/// HOW
///
///   1. The word is found in each recording (SyllableAligner.FindWord), and
///      every 10 ms frame described by:
///        - 12 mel-cepstral coefficients, normalised per recording - WHICH
///          sound it is. Used only to line the two recordings up;
///        - pitch, in semitones from the speaker's OWN median pitch;
///        - loudness, in dB below the speaker's OWN loudest moment.
///   2. Dynamic time warping lines the answer up with the reference by sound,
///      so a slower or faster speaker matches moment for moment.
///   3. Along that line-up three differences are averaged, each in units of
///      what one speaker naturally varies by:
///        - the pitch movement (2 semitones),
///        - the loudness movement (4 dB),
///        - the timing - which parts are held longer than the speaker's own
///          average pace (a factor of 1.35).
///      Voice height, voice quality, loudness, distance from the phone and
///      overall speed are all normalised away, so a child, an adult, a
///      quiet or a fast speaker are compared on HOW they said the word only.
///   4. The same distance is taken to each recording of the OTHER
///      pronunciation. The score is
///
///          score = distance to the nearest other pronunciation
///                - distance to Rachel's
///
///      positive when the answer is nearer Rachel's pronunciation.
///   5. Each answer is measured six ways (frame offsets, a little noise); a
///      verdict stands only if all six agree. The Matches and Other lines are
///      calibrated per word (PronunciationTemplateBuilder) from Rachel's
///      recording and the other pronunciation under a dozen simulated phones
///      and voices. Between them: Unclear.
///
/// The reference frames are precomputed (PronunciationTemplateData.cs,
/// generated), so the game needs no access to the audio clips at run time.
/// </summary>
public static partial class PronunciationTemplate
{
    public enum Verdict { Matches, Other, Unclear }

    public struct Result
    {
        public Verdict verdict;
        /// <summary>Lowest and highest score over the six measurements (NaN if none).</summary>
        public float low, high;
        public int measurements, measured;
        public string reason;
    }

    /// <summary>One recording of the word, reduced to what is compared.</summary>
    public sealed class Frames
    {
        public float[][] mfcc;     // normalised per recording
        public float[] pitchSt;    // semitones from the speaker's median; NaN when unvoiced
        public float[] loudDb;     // dB below the loudest frame, floored at -30
        public int Count { get { return loudDb.Length; } }
    }

    public const float PitchUnitSt = 2f, LoudUnitDb = 4f, TimingUnitLn = 0.3f;
    private const float LoudFloorDb = -30f;
    private const float CoreDb = -25f;           // the word's core: within this of its loudest frame
    private const int TimingWindow = 5;          // frames either side
    private const float Band = 0.35f;            // how far the line-up may stray from the diagonal

    /// <summary>The frames of the word in a recording, or null (with why) when no word is found.</summary>
    public static Frames Extract(SpeechFeatures s, out string whyNot)
    {
        int from, to;
        if (!SyllableAligner.FindWord(s, out from, out to, out whyNot)) return null;
        // Only the core of the word: the word finder is generous at the edges
        // (it must not cut a quiet last syllable for the syllable count), but
        // here a stretch of room noise at either end would be compared as if
        // it were part of how the word was said.
        float top = float.MinValue;
        for (int f = from; f <= to; f++) top = Math.Max(top, s.db[f]);
        while (from < to && s.db[from] < top + CoreDb) from++;
        while (to > from && s.db[to] < top + CoreDb) to--;
        if (to - from + 1 < 10) { whyNot = "the word was too short"; return null; }
        int n = to - from + 1;
        Frames fr = new Frames { mfcc = new float[n][], pitchSt = new float[n], loudDb = new float[n] };

        float peak = float.MinValue;
        List<float> voiced = new List<float>();
        for (int f = from; f <= to; f++)
        {
            peak = Math.Max(peak, s.db[f]);
            if (s.f0[f] > 0 && s.periodicity[f] > 0.6f) voiced.Add(s.f0[f]);
        }
        float median = voiced.Count > 0 ? SpeechFeatures.Percentile(voiced, 0.5f) : 0f;

        // Cepstral mean and variance normalisation: removes the microphone's
        // and the vocal tract's fixed colouring, keeping the changes between sounds.
        double[] mean = new double[SpeechFeatures.Cepstra], sq = new double[SpeechFeatures.Cepstra];
        for (int f = from; f <= to; f++)
            for (int c = 0; c < SpeechFeatures.Cepstra; c++) { mean[c] += s.mfcc[f][c]; sq[c] += s.mfcc[f][c] * s.mfcc[f][c]; }
        for (int c = 0; c < SpeechFeatures.Cepstra; c++)
        {
            mean[c] /= n;
            sq[c] = Math.Sqrt(Math.Max(1e-6, sq[c] / n - mean[c] * mean[c]));
        }

        for (int i = 0; i < n; i++)
        {
            int f = from + i;
            fr.mfcc[i] = new float[SpeechFeatures.Cepstra];
            for (int c = 0; c < SpeechFeatures.Cepstra; c++) fr.mfcc[i][c] = (float)((s.mfcc[f][c] - mean[c]) / sq[c]);
            fr.loudDb[i] = Math.Max(LoudFloorDb, s.db[f] - peak);
            float hz = s.f0[f];
            if (hz > 0 && s.periodicity[f] > 0.6f && median > 0)
            {
                while (hz > median * 1.8f) hz *= 0.5f;   // octave jumps of the tracker
                while (hz < median / 1.8f) hz *= 2f;
                fr.pitchSt[i] = (float)(12 * Math.Log(hz / median, 2));
            }
            else fr.pitchSt[i] = float.NaN;
        }
        return fr;
    }

    /// <summary>
    /// How differently the answer was said from the reference: 0 for the same
    /// pitch movement, loudness movement and timing; about 1 for a difference
    /// of one natural unit in each throughout.
    /// </summary>
    public static float Distance(Frames answer, Frames reference)
    {
        int n = answer.Count, m = reference.Count;
        // ---- line the two up by sound ---------------------------------------
        double[,] D = new double[n + 1, m + 1];
        for (int i = 0; i <= n; i++) for (int j = 0; j <= m; j++) D[i, j] = double.PositiveInfinity;
        D[0, 0] = 0;
        for (int i = 1; i <= n; i++)
        {
            for (int j = 1; j <= m; j++)
            {
                if (Math.Abs((double)i / n - (double)j / m) > Band) continue;
                double c = 0;
                float[] a = answer.mfcc[i - 1], b = reference.mfcc[j - 1];
                for (int k = 0; k < a.Length; k++) { double d = a[k] - b[k]; c += d * d; }
                c = Math.Sqrt(c);
                D[i, j] = c + Math.Min(D[i - 1, j - 1], Math.Min(D[i - 1, j], D[i, j - 1]));
            }
        }
        if (double.IsPositiveInfinity(D[n, m])) return float.PositiveInfinity;

        List<int> pi = new List<int>(), pj = new List<int>();
        int x = n, y = m;
        while (x > 0 && y > 0)
        {
            pi.Add(x - 1); pj.Add(y - 1);
            double diag = D[x - 1, y - 1], up = D[x - 1, y], left = D[x, y - 1];
            if (diag <= up && diag <= left) { x--; y--; }
            else if (up <= left) x--;
            else y--;
        }

        // ---- compare HOW it was said along the line-up -------------------------
        double pitch = 0, loud = 0; int pitchN = 0, loudN = 0;
        int[] perRef = new int[m];
        for (int k = 0; k < pi.Count; k++)
        {
            int i = pi[k], j = pj[k];
            perRef[j]++;
            float pa = answer.pitchSt[i], pr = reference.pitchSt[j];
            if (!float.IsNaN(pa) && !float.IsNaN(pr)) { double d = (pa - pr) / PitchUnitSt; pitch += d * d; pitchN++; }
            float la = answer.loudDb[i], lr = reference.loudDb[j];
            if (Math.Max(la, lr) > LoudFloorDb + 5) { double d = (la - lr) / LoudUnitDb; loud += d * d; loudN++; }
        }
        // Timing: where the answer lingers, relative to its own overall pace.
        double pace = (double)n / m, timing = 0;
        for (int j = 0; j < m; j++)
        {
            int a = Math.Max(0, j - TimingWindow), b = Math.Min(m - 1, j + TimingWindow);
            double frames = 0;
            for (int k = a; k <= b; k++) frames += perRef[k];
            double local = Math.Log((frames + 1) / ((b - a + 1) * pace + 1)) / TimingUnitLn;
            timing += local * local;
        }
        timing /= m;
        return (float)((pitchN > 0 ? pitch / pitchN : 0) + (loudN > 0 ? loud / loudN : 0) + timing);
    }

    /// <summary>The score of one answer: nearest other pronunciation minus Rachel's (positive = Rachel's).</summary>
    public static float Score(Frames answer, IList<Frames> rachel, IList<Frames> others)
    {
        float toRachel = float.PositiveInfinity;
        foreach (Frames r in rachel) toRachel = Math.Min(toRachel, Distance(answer, r));
        float toOther = float.PositiveInfinity;
        foreach (Frames o in others) toOther = Math.Min(toOther, Distance(answer, o));
        return toOther - toRachel;
    }

    private static readonly int[] Offsets = { 0, 48, 96 };

    /// <summary>Judges one recording of an ambiguous word against its references.</summary>
    public static Result Assess(float[] samples, int sampleRate, string word)
    {
        return Assess(samples, sampleRate, For(word));
    }

    public static Result Assess(float[] samples, int sampleRate, Reference reference)
    {
        Result r = new Result { low = float.NaN, high = float.NaN, verdict = Verdict.Unclear };
        if (reference == null) { r.reason = "no reference recordings for this word"; return r; }
        if (samples == null || samples.Length < sampleRate / 5) { r.reason = "no audio was recorded"; return r; }

        float[] x16 = SpeechFeatures.Resample(samples, sampleRate, SpeechFeatures.SampleRate);
        float low = float.MaxValue, high = float.MinValue;
        string problem = null;
        for (int pass = 0; pass < Offsets.Length * 2; pass++)
        {
            r.measurements++;
            float[] x = pass >= Offsets.Length ? WithNoise(x16, -58f, 2000 + pass) : x16;
            string why;
            Frames f = Extract(SpeechFeatures.Of(x, SpeechFeatures.SampleRate, Offsets[pass % Offsets.Length]), out why);
            if (f == null) { if (problem == null) problem = why; continue; }
            float score = Score(f, reference.rachel, reference.others);
            if (float.IsNaN(score) || float.IsInfinity(score)) { if (problem == null) problem = "could not line the answer up with the reference"; continue; }
            r.measured++;
            low = Math.Min(low, score);
            high = Math.Max(high, score);
        }
        if (r.measured == 0) { r.reason = problem ?? "could not measure the word"; return r; }
        r.low = low; r.high = high;

        CultureInfo inv = CultureInfo.InvariantCulture;
        string where = " (score " + low.ToString("0.00", inv) + ".." + high.ToString("0.00", inv) +
                       ", Rachel's from " + reference.acceptFrom.ToString("0.00", inv) +
                       ", the other from " + reference.rejectFrom.ToString("0.00", inv) + ")";
        if (r.measured < r.measurements) { r.reason = "the word could not be heard clearly every time" + where; return r; }
        if (low >= reference.acceptFrom) { r.verdict = Verdict.Matches; r.reason = "said like Rachel's " + reference.intended + where; }
        else if (high <= reference.rejectFrom) { r.verdict = Verdict.Other; r.reason = "said like the other word, " + reference.alternative + where; }
        else r.reason = "not clearly either pronunciation" + where;
        return r;
    }

    private static float[] WithNoise(float[] x, float db, int seed)
    {
        Random rng = new Random(seed);
        double amp = Math.Pow(10, db / 20) * Math.Sqrt(3);
        float[] y = new float[x.Length];
        for (int i = 0; i < x.Length; i++) y[i] = x[i] + (float)((rng.NextDouble() * 2 - 1) * amp);
        return y;
    }

    // ---- References -------------------------------------------------------------

    /// <summary>One ambiguous word's references: Rachel's recording, the other pronunciation(s), and the calibrated lines.</summary>
    public sealed class Reference
    {
        public string word, intended, alternative;
        /// <summary>
        /// Rachel's recording, as recorded and raised the way a child's voice
        /// is (x1.25, x1.45): a smaller voice moves every resonance up, and
        /// the line-up by sound is made against the nearest of the three.
        /// </summary>
        public List<Frames> rachel = new List<Frames>();
        /// <summary>The other pronunciation, the same three ways for each recording of it.</summary>
        public List<Frames> others = new List<Frames>();
        public float acceptFrom, rejectFrom;
        public bool trusted;
        public int realAlternatives;
    }

    private static Dictionary<string, Reference> table;

    /// <summary>Overrides a word's reference (the builder calibrates with this).</summary>
    public static void Override(Reference r)
    {
        Load();
        table[r.word] = r;
    }

    public static Reference For(string word)
    {
        Load();
        Reference r;
        return !string.IsNullOrEmpty(word) && table.TryGetValue(word.Trim(), out r) ? r : null;
    }

    private static void Load()
    {
        if (table != null) return;
        Dictionary<string, Reference> t = new Dictionary<string, Reference>(StringComparer.OrdinalIgnoreCase);
        foreach (Row row in Rows)
        {
            AmbiguousWords.Entry e = AmbiguousWords.For(row.word);
            Reference r = new Reference
            {
                word = row.word,
                intended = e != null ? e.intended : row.word,
                alternative = e != null ? e.alternative : "",
                acceptFrom = row.acceptFrom,
                rejectFrom = row.rejectFrom,
                trusted = row.trusted,
                realAlternatives = row.realAlternatives,
            };
            foreach (string o in row.rachel.Split('|')) r.rachel.Add(Decode(o));
            foreach (string o in row.others) foreach (string v in o.Split('|')) r.others.Add(Decode(v));
            t[row.word] = r;
        }
        table = t;
    }

    private struct Row
    {
        public string word, rachel;
        public string[] others;
        public float acceptFrom, rejectFrom;
        public bool trusted;
        public int realAlternatives;

        public Row(string word, float acceptFrom, float rejectFrom, bool trusted, int realAlternatives, string rachel, params string[] others)
        {
            this.word = word; this.acceptFrom = acceptFrom; this.rejectFrom = rejectFrom; this.trusted = trusted;
            this.realAlternatives = realAlternatives; this.rachel = rachel; this.others = others;
        }
    }

    // ---- Compact storage: 14 numbers a frame, as 16-bit integers, base64 ----------

    private const int PerFrame = SpeechFeatures.Cepstra + 2;
    private const float Scale = 1000f;

    public static string Encode(Frames f)
    {
        byte[] bytes = new byte[f.Count * PerFrame * 2];
        int k = 0;
        for (int i = 0; i < f.Count; i++)
        {
            for (int c = 0; c < PerFrame; c++)
            {
                float v = c < SpeechFeatures.Cepstra ? f.mfcc[i][c] : c == SpeechFeatures.Cepstra ? f.pitchSt[i] : f.loudDb[i];
                short q = float.IsNaN(v) ? short.MinValue : (short)Math.Max(-32767, Math.Min(32767, Math.Round(v * Scale)));
                bytes[k++] = (byte)(q & 0xff);
                bytes[k++] = (byte)((q >> 8) & 0xff);
            }
        }
        return Convert.ToBase64String(bytes);
    }

    public static Frames Decode(string s)
    {
        byte[] bytes = Convert.FromBase64String(s);
        int n = bytes.Length / (PerFrame * 2);
        Frames f = new Frames { mfcc = new float[n][], pitchSt = new float[n], loudDb = new float[n] };
        int k = 0;
        for (int i = 0; i < n; i++)
        {
            f.mfcc[i] = new float[SpeechFeatures.Cepstra];
            for (int c = 0; c < PerFrame; c++)
            {
                short q = (short)(bytes[k] | (bytes[k + 1] << 8));
                k += 2;
                float v = q == short.MinValue ? float.NaN : q / Scale;
                if (c < SpeechFeatures.Cepstra) f.mfcc[i][c] = v;
                else if (c == SpeechFeatures.Cepstra) f.pitchSt[i] = v;
                else f.loudDb[i] = v;
            }
        }
        return f;
    }
}
