using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

/// <summary>
/// Finds each syllable's VOWEL in a recording of one known word, using the
/// word's spelling - so the stress of each syllable can be measured on the
/// vowel that carries it.
///
/// WHY NOT "THE DEEPEST DIP"
///
/// RawStressAnalyzer split a word at the deepest dip in its loudness. That
/// guesses: a sonorant (the n of kanan, the y of tayo) leaves almost no dip, a
/// burst can be louder than a vowel, and a final s or t can look like a
/// syllable of its own. On Teacher Rachel's own studio recordings it cut
/// Kumusta's last syllable down to 30 ms at -38 dB and gave Sulat a 30 ms
/// second vowel - so what it called her stress was not her stress. Worse, which
/// dip wins can change with a few milliseconds of difference between two
/// recordings, so the SAME way of saying a word could be split differently
/// and judged differently. That is one root of "sometimes accepted,
/// sometimes not".
///
/// HOW
///
/// Filipino spelling is phonemic, so the spelling says exactly how many
/// vowels the word has and what sits between them: tayo is C V C V with a
/// glide (y) between the vowels, kain is C V V with a glottal stop, sapatos is
/// C V C V C V C. The aligner looks for exactly that sequence:
///
///   1. Every 10 ms frame gets a "vowelness" between 0 and 1: loud for this
///      utterance, periodic (voiced), and not hissy. Vowels are near 1,
///      closures and s near 0, nasals and glides in between.
///   2. Dynamic programming lays the word's segments over the frames -
///      optional onset, vowel, consonants, vowel, ..., optional coda - so that
///      vowel segments cover vowel-like frames and consonant segments the
///      rest, each with a minimum length that depends on what the spelling
///      says is there (an obstruent like t or s takes at least 20 ms, a glide
///      or nasal 10 ms, two vowels in a row may meet with no gap at all).
///   3. The result is the best fit of the WHOLE word at once, not one dip at a
///      time, so it moves smoothly when the recording moves.
///
/// The same aligner is used on Rachel's recordings (to build the references)
/// and on the player's answer, so both are cut the same way.
/// </summary>
public static class SyllableAligner
{
    public enum Gap
    {
        /// <summary>Two vowels side by side (ka-in, pa-alam): a glottal stop, maybe no dip at all.</summary>
        Glottal,
        /// <summary>Only m, n, ng, l, r, y, w between the vowels: voiced, a shallow dip.</summary>
        Sonorant,
        /// <summary>A stop or fricative (p t k b d g s h ...): a clear dip.</summary>
        Obstruent,
    }

    public struct Syllable
    {
        /// <summary>First and last frame of the vowel.</summary>
        public int from, to;
        /// <summary>Vowel length, ms: the syllable's frames, each counted by how vowel-like it is.</summary>
        public float lengthMs;
        /// <summary>Loudness of the vowel (mean of its three loudest frames), dB.</summary>
        public float loudDb;
        /// <summary>Median pitch of the vowel's voiced frames, Hz (0 if none).</summary>
        public float pitchHz;
        /// <summary>How vowel-like the segment is on average, 0..1.</summary>
        public float vowelness;
    }

    public struct Alignment
    {
        public bool ok;
        public string whyNot;
        public Syllable[] syllables;
        /// <summary>First and last frame of the whole word.</summary>
        public int wordFrom, wordTo;
        /// <summary>
        /// The longest run (ms) of clearly vowel-like frames the aligner had
        /// to put in a CONSONANT slot - a sign the recording holds more
        /// syllables than the word (another word, an extra syllable).
        /// </summary>
        public float extraVowelMs;
        /// <summary>The least vowel-like vowel segment (0..1) - a sign a syllable is missing.</summary>
        public float weakestVowel;
        /// <summary>The speaker's median pitch over the word, Hz (0 if unvoiced).</summary>
        public float medianPitchHz;
    }

    private const float Theta = 0.5f;
    private const int MinVowelFrames = 3;
    private const int MaxInnerConsonantFrames = 30;
    private const float WordWindowDb = 35f;
    private const float WordAboveFloorDb = 9f;
    private const int MaxGapInsideWordFrames = 25;   // a stop closure, or a glottal stop
    private const int MinWordFrames = 12;
    private const int DipReachFrames = 15;           // a valley is judged against 150 ms either side

    /// <summary>The gaps between consecutive vowels of a word, read off its spelling.</summary>
    public static Gap[] GapsOf(string word)
    {
        string w = Plain(word);
        List<int> vowels = new List<int>();
        for (int i = 0; i < w.Length; i++) if (IsVowel(w[i])) vowels.Add(i);

        Gap[] gaps = new Gap[Math.Max(0, vowels.Count - 1)];
        for (int k = 0; k + 1 < vowels.Count; k++)
        {
            string between = w.Substring(vowels[k] + 1, vowels[k + 1] - vowels[k] - 1);
            if (between.Length == 0) { gaps[k] = Gap.Glottal; continue; }
            bool sonorant = true;
            foreach (char c in between)
                if ("mnlrywg".IndexOf(c) < 0) { sonorant = false; break; }
            // A g only counts as sonorant as part of ng.
            if (sonorant && between.Replace("ng", "").IndexOf('g') >= 0) sonorant = false;
            gaps[k] = sonorant ? Gap.Sonorant : Gap.Obstruent;
        }
        return gaps;
    }

    /// <summary>Lower case, no stress marks, letters only.</summary>
    public static string Plain(string word)
    {
        if (string.IsNullOrEmpty(word)) return "";
        string d = word.ToLowerInvariant().Normalize(NormalizationForm.FormD);
        StringBuilder sb = new StringBuilder();
        foreach (char c in d)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(c) == UnicodeCategory.NonSpacingMark) continue;
            if (char.IsLetter(c)) sb.Append(c);
        }
        return sb.ToString();
    }

    private static bool IsVowel(char c) { return "aeiou".IndexOf(c) >= 0; }

    /// <summary>
    /// Not a low rumble: breath on the microphone, handling noise and mains
    /// hum put almost all their energy below a couple of hundred hertz, which
    /// no speech sound does - even a man's "u" has its first formant higher.
    /// </summary>
    public static bool IsSpeechLike(SpeechFeatures s, int f)
    {
        return s.hiss[f] > -22f;
    }

    /// <summary>How vowel-like one frame is, 0..1, relative to the word's loudest frame.</summary>
    public static float Vowelness(SpeechFeatures s, int f, float peakDb)
    {
        float energy = SpeechFeatures.Sigmoid((s.db[f] - peakDb + 15f) / 2.5f);
        float voiced = SpeechFeatures.Clamp01((s.periodicity[f] - 0.15f) / 0.45f);
        float notHiss = SpeechFeatures.Sigmoid((-4f - s.hiss[f]) / 1.5f);
        // A final vowel is often creaky or breathy and YIN calls it
        // half-periodic at best; loud and not hissy still makes it at least
        // half a vowel.
        return energy * notHiss * Math.Max(voiced, 0.6f);
    }

    /// <summary>
    /// Where the spoken word is in a recording: the run of speech-like frames
    /// around the loudest one, gaps of up to 250 ms (a stop, a glottal stop)
    /// included. False, with a reason, when there is no clear voice.
    /// </summary>
    public static bool FindWord(SpeechFeatures s, out int start, out int end, out string whyNot)
    {
        start = end = 0;
        whyNot = null;
        if (s == null || s.Count < MinWordFrames) { whyNot = "the recording was too short"; return false; }
        List<float> all = new List<float>(s.db);
        int loudest = -1;
        for (int f = 0; f < s.Count; f++)
            if (IsSpeechLike(s, f) && (loudest < 0 || s.db[f] > s.db[loudest])) loudest = f;
        if (loudest < 0) { whyNot = "no clear voice in the recording"; return false; }
        float peak = s.db[loudest];
        // A tightly trimmed clip has no silence to measure the floor on, so the
        // floor is never taken as closer than 45 dB under the voice.
        float floor = Math.Min(SpeechFeatures.Percentile(all, 0.1f), peak - 45f);
        if (peak < -62f || peak - floor < 15f) { whyNot = "no clear voice in the recording"; return false; }

        float level = Math.Max(peak - WordWindowDb, floor + WordAboveFloorDb);
        start = loudest; end = loudest;
        for (int f = loudest - 1, gap = 0; f >= 0; f--)
        {
            if (s.db[f] > level && IsSpeechLike(s, f)) { start = f; gap = 0; } else if (++gap > MaxGapInsideWordFrames) break;
        }
        for (int f = loudest + 1, gap = 0; f < s.Count; f++)
        {
            if (s.db[f] > level && IsSpeechLike(s, f)) { end = f; gap = 0; } else if (++gap > MaxGapInsideWordFrames) break;
        }
        if (end - start + 1 < MinWordFrames) { whyNot = "the word was too short to hear its syllables"; return false; }
        return true;
    }

    /// <summary>Aligns a recording of the given word (syllables from its spelling).</summary>
    public static Alignment Align(SpeechFeatures s, string word)
    {
        Alignment a = new Alignment();
        Gap[] gaps = GapsOf(word);
        int n = gaps.Length + 1;
        if (n < 2) { a.whyNot = "a one-syllable word has no syllables to compare"; return a; }
        if (s == null || s.Count < MinWordFrames) { a.whyNot = "the recording was too short"; return a; }

        // ---- 1. Where the word is ------------------------------------------
        int start, end;
        string why;
        if (!FindWord(s, out start, out end, out why)) { a.whyNot = why; return a; }
        int T = end - start + 1;
        a.wordFrom = start;
        a.wordTo = end;

        // The word's loudness peak is taken over voiced frames: a burst or an s
        // can out-shout a vowel and would make every vowel look weak.
        float vpeak = float.MinValue;
        for (int f = start; f <= end; f++) if (s.periodicity[f] > 0.6f) vpeak = Math.Max(vpeak, s.db[f]);
        if (vpeak == float.MinValue) { a.whyNot = "no voiced sound in the recording"; return a; }

        // Evidence, frame by frame, for each kind of segment the spelling can
        // put there. Vowelness alone saturates on voiced sounds: the l of
        // talon or the n of tanong is nearly as voiced and loud as a vowel, so
        // what marks them is a VALLEY - in loudness, or in the share of high
        // frequencies (a nasal's murmur is far duller than the vowels either
        // side of it). A vowel fading out at the end of the word is no valley,
        // since nothing louder follows it.
        float[] vowelGain = new float[T], obstruent = new float[T], sonorant = new float[T],
                glottal = new float[T], silence = new float[T];
        for (int i = 0; i < T; i++)
        {
            int f = start + i;
            float dip = Valley(s.db, f, start, end);
            float dull = Valley(s.hiss, f, start, end);
            float dipness = SpeechFeatures.Sigmoid((dip - 4f) / 1.5f);
            float dullness = SpeechFeatures.Sigmoid((dull - 5f) / 1.5f);
            float hissy = SpeechFeatures.Sigmoid((s.hiss[f] + 4f) / 1.5f);
            float closure = SpeechFeatures.Sigmoid((vpeak - 25f - s.db[f]) / 2.5f);
            float vow = Vowelness(s, f, vpeak);

            vowelGain[i] = vow * (1f - Math.Max(dipness, 0.8f * dullness)) - Theta;
            obstruent[i] = Math.Max(Math.Max(hissy, closure), dipness) - Theta;
            sonorant[i] = Math.Max(Math.Max(dipness, dullness), 0.5f * Math.Max(hissy, closure)) - Theta;
            glottal[i] = Math.Max(dipness, Math.Max(closure, 1f - s.periodicity[f])) - Theta;
            silence[i] = Math.Max(closure, 1f - vow) - Theta;
        }

        // ---- 2. Lay the segments over the frames -----------------------------
        // Segments: C0 V1 C1 V2 ... Vn Cn. Even index = consonant, odd = vowel.
        string plain = Plain(word);
        bool onset = plain.Length > 0 && !IsVowel(plain[0]);
        bool coda = plain.Length > 0 && !IsVowel(plain[plain.Length - 1]);
        int segs = 2 * n + 1;
        double[][] prefix = new double[segs][];
        for (int j = 0; j < segs; j++)
        {
            float[] gain;
            if ((j & 1) == 1) gain = vowelGain;
            else if (j == 0) gain = onset ? obstruent : silence;
            else if (j == segs - 1) gain = coda ? obstruent : silence;
            else
            {
                Gap g = gaps[j / 2 - 1];
                gain = g == Gap.Glottal ? glottal : g == Gap.Sonorant ? sonorant : obstruent;
            }
            prefix[j] = new double[T + 1];
            for (int i = 0; i < T; i++) prefix[j][i + 1] = prefix[j][i] + gain[i];
        }

        double[,] best = new double[segs, T + 1];
        int[,] from = new int[segs, T + 1];
        for (int j = 0; j < segs; j++)
            for (int t = 0; t <= T; t++) best[j, t] = double.NegativeInfinity;

        for (int j = 0; j < segs; j++)
        {
            bool vowel = (j & 1) == 1;
            bool edge = j == 0 || j == segs - 1;
            int minLen, maxLen;
            if (vowel) { minLen = MinVowelFrames; maxLen = T; }
            else if (edge) { minLen = 0; maxLen = T; }
            else
            {
                Gap g = gaps[j / 2 - 1];
                minLen = g == Gap.Glottal ? 0 : g == Gap.Sonorant ? 1 : 2;
                maxLen = MaxInnerConsonantFrames;
            }

            for (int t = 0; t <= T; t++)
            {
                for (int len = minLen; len <= Math.Min(maxLen, t); len++)
                {
                    int t0 = t - len;
                    double prev = j == 0 ? (t0 == 0 ? 0 : double.NegativeInfinity) : best[j - 1, t0];
                    if (double.IsNegativeInfinity(prev)) continue;
                    double gain = prefix[j][t] - prefix[j][t0];
                    if (prev + gain > best[j, t]) { best[j, t] = prev + gain; from[j, t] = t0; }
                }
            }
        }
        if (double.IsNegativeInfinity(best[segs - 1, T])) { a.whyNot = "the word was too short to hear all " + n + " syllables"; return a; }

        int[] segStart = new int[segs], segEnd = new int[segs];
        int at = T;
        for (int j = segs - 1; j >= 0; j--)
        {
            int t0 = from[j, at];
            segStart[j] = t0;
            segEnd[j] = at;   // exclusive
            at = t0;
        }

        // ---- 3. Describe each vowel ------------------------------------------
        List<float> voicedPitch = new List<float>();
        for (int f = start; f <= end; f++) if (s.f0[f] > 0 && s.periodicity[f] > 0.6f) voicedPitch.Add(s.f0[f]);
        float median = voicedPitch.Count > 0 ? SpeechFeatures.Percentile(voicedPitch, 0.5f) : 0f;
        a.medianPitchHz = median;

        // Each syllable's share of the word: from halfway through the
        // consonants before its vowel to halfway through those after it.
        int[] spanFrom = new int[n], spanTo = new int[n];
        for (int k = 0; k < n; k++)
        {
            int before = 2 * k, after = 2 * k + 2;
            spanFrom[k] = k == 0 ? 0 : (segStart[before] + segEnd[before]) / 2;
            spanTo[k] = k == n - 1 ? T : (segStart[after] + segEnd[after]) / 2;
        }

        a.syllables = new Syllable[n];
        a.weakestVowel = 1f;
        for (int k = 0; k < n; k++)
        {
            int j = 2 * k + 1;
            int f0 = start + segStart[j], f1 = start + segEnd[j] - 1;
            // The vowel's length is SOFT: every frame of the syllable counts by
            // how vowel-like it is. A hard boundary snaps between two nearby
            // candidates from one recording to the next and the length with
            // it; frames near a boundary are consonant-like and count for
            // little here, so where exactly it falls hardly matters.
            double soft = 0;
            for (int i = spanFrom[k]; i < spanTo[k]; i++) soft += Vowelness(s, start + i, vpeak);
            Syllable sy = new Syllable { from = f0, to = f1, lengthMs = (float)Math.Max(SpeechFeatures.FrameMs, soft * SpeechFeatures.FrameMs) };

            List<float> loud = new List<float>();
            List<float> pitch = new List<float>();
            double vsum = 0;
            for (int f = f0; f <= f1; f++)
            {
                loud.Add(s.db[f]);
                vsum += Vowelness(s, f, vpeak);
                if (s.f0[f] > 0 && s.periodicity[f] > 0.6f) pitch.Add(FoldOctave(s.f0[f], median));
            }
            loud.Sort();
            int top = Math.Min(3, loud.Count);
            float sum = 0;
            for (int i = 0; i < top; i++) sum += loud[loud.Count - 1 - i];
            sy.loudDb = sum / top;
            sy.pitchHz = pitch.Count > 0 ? SpeechFeatures.Percentile(pitch, 0.5f) : 0f;
            sy.vowelness = (float)(vsum / (f1 - f0 + 1));
            a.syllables[k] = sy;
            a.weakestVowel = Math.Min(a.weakestVowel, sy.vowelness);
        }

        // Clearly vowel-like frames left in consonant slots.
        int longestRun = 0;
        for (int j = 0; j < segs; j += 2)
        {
            int run = 0;
            for (int i = segStart[j]; i < segEnd[j]; i++)
            {
                if (vowelGain[i] + Theta > 0.7f) { run++; longestRun = Math.Max(longestRun, run); } else run = 0;
            }
        }
        a.extraVowelMs = longestRun * SpeechFeatures.FrameMs;
        a.ok = true;
        return a;
    }

    /// <summary>How far frame f sits below the higher of what lies either side of it (0 when it is no valley).</summary>
    private static float Valley(float[] x, int f, int start, int end)
    {
        float left = float.MinValue, right = float.MinValue;
        for (int k = Math.Max(start, f - DipReachFrames); k < f; k++) left = Math.Max(left, x[k]);
        for (int k = f + 1; k <= Math.Min(end, f + DipReachFrames); k++) right = Math.Max(right, x[k]);
        if (left == float.MinValue || right == float.MinValue) return 0f;
        return Math.Max(0f, Math.Min(left, right) - x[f]);
    }

    /// <summary>Moves a pitch reading into the octave around the median (pitch trackers jump octaves).</summary>
    private static float FoldOctave(float hz, float median)
    {
        if (median <= 0) return hz;
        while (hz > median * 1.8f) hz *= 0.5f;
        while (hz < median / 1.8f) hz *= 2f;
        return hz;
    }

    /// <summary>"vowels 120/180 ms, 60-72 / 75-93, -20/-18 dB, 150/176 Hz" for the console.</summary>
    public static string Describe(Alignment a)
    {
        if (!a.ok) return a.whyNot;
        string[] len = new string[a.syllables.Length], db = new string[a.syllables.Length], hz = new string[a.syllables.Length];
        for (int i = 0; i < a.syllables.Length; i++)
        {
            len[i] = a.syllables[i].lengthMs.ToString("0", CultureInfo.InvariantCulture);
            db[i] = a.syllables[i].loudDb.ToString("0", CultureInfo.InvariantCulture);
            hz[i] = a.syllables[i].pitchHz.ToString("0", CultureInfo.InvariantCulture);
        }
        return "vowels " + string.Join("/", len) + " ms, " + string.Join("/", db) + " dB, " + string.Join("/", hz) + " Hz";
    }
}
