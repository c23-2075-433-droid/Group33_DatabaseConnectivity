using System;
using System.Collections.Generic;

/// <summary>
/// The per-frame measurements PronunciationAssessor works from: loudness,
/// pitch, how periodic (voiced) the sound is, and how hissy it is. One frame
/// every 10 ms, computed at 16 kHz whatever the input rate.
///
/// Every measurement here is later used only RELATIVE to the rest of the same
/// utterance (dB below the word's loudest frame, semitones above or below the
/// speaker's own median pitch), so none of it depends on how loud the player
/// is, how far the phone is, or how high their voice is.
///
/// Plain C#: the same code runs on the phone, in the Editor and in
/// Tools/stresstests.
/// </summary>
public sealed class SpeechFeatures
{
    public const int SampleRate = 16000;
    public const int FrameMs = 10;
    public const int Hop = SampleRate * FrameMs / 1000;     // 160 samples
    private const int Window = Hop * 4;                    // 40 ms, enough for 75 Hz
    private const float MinPitchHz = 75f;
    private const float MaxPitchHz = 600f;                 // a small child can reach 500+
    private const float YinThreshold = 0.35f;

    /// <summary>Loudness of each frame, dB (full scale).</summary>
    public float[] db;
    /// <summary>Pitch of each frame in Hz, 0 when the frame is not voiced.</summary>
    public float[] f0;
    /// <summary>0..1: how periodic the frame is (1 - YIN's best dip). Vowels are near 1, hiss near 0.</summary>
    public float[] periodicity;
    /// <summary>
    /// Energy of the sample-to-sample difference over the energy itself, in
    /// dB: a cheap high-frequency share. Vowels and nasals sit far below 0;
    /// an s sits near or above it.
    /// </summary>
    public float[] hiss;
    /// <summary>
    /// 12 mel-cepstral coefficients per frame (c1..c12): the shape of the
    /// spectrum, i.e. WHICH sound is being made. PronunciationTemplate uses
    /// them only to line an answer up in time with Rachel's recording; they
    /// are never compared for voice.
    /// </summary>
    public float[][] mfcc;

    public int Count { get { return db.Length; } }

    /// <summary>Seconds from the start of the (resampled) audio to the start of frame f.</summary>
    public static float FrameSeconds(int f) { return f * FrameMs / 1000f; }

    /// <summary>
    /// Measures mono audio at any rate. offsetSamples drops that many samples
    /// (at 16 kHz) from the start first: the assessor measures each answer at a
    /// few slightly shifted framings to check its verdict does not hinge on
    /// where the 10 ms frames happen to fall.
    /// </summary>
    public static SpeechFeatures Of(float[] samples, int sampleRate, int offsetSamples = 0)
    {
        float[] x = sampleRate == SampleRate ? samples : Resample(samples, sampleRate, SampleRate);
        if (offsetSamples > 0 && offsetSamples < x.Length)
        {
            float[] y = new float[x.Length - offsetSamples];
            Array.Copy(x, offsetSamples, y, 0, y.Length);
            x = y;
        }

        int frames = Math.Max(0, (x.Length - Window) / Hop);
        SpeechFeatures s = new SpeechFeatures
        {
            db = new float[frames],
            f0 = new float[frames],
            periodicity = new float[frames],
            hiss = new float[frames],
            mfcc = new float[frames][],
        };

        // Pitch is tracked at 8 kHz: a voice's fundamental is far below 4 kHz,
        // and it makes the tracker four times cheaper on a phone.
        float[] half = new float[x.Length / 2];
        for (int i = 0; i < half.Length; i++)
        {
            float a0 = x[Math.Max(0, 2 * i - 1)], a1 = x[2 * i], a2 = x[Math.Min(x.Length - 1, 2 * i + 1)];
            half[i] = 0.25f * a0 + 0.5f * a1 + 0.25f * a2;
        }

        for (int f = 0; f < frames; f++)
        {
            // Loudness and hiss over the middle 20 ms of the 40 ms window, so a
            // frame describes the moment its pitch is measured for.
            int a = f * Hop + Hop, b = a + 2 * Hop;
            double e = 0, d = 0;
            for (int i = a; i < b; i++)
            {
                e += x[i] * x[i];
                double diff = x[i] - x[i - 1];
                d += diff * diff;
            }
            s.db[f] = (float)(10 * Math.Log10(e / (b - a) + 1e-12));
            s.hiss[f] = (float)(10 * Math.Log10((d + 1e-12) / (e + 1e-12)));

            float aperiodic;
            s.f0[f] = Yin(half, f * Hop / 2, Window / 2, PitchRate, out aperiodic);
            s.periodicity[f] = Clamp01(1f - aperiodic);
            s.mfcc[f] = Mfcc(x, f * Hop + (Window - MfccWindow) / 2);
        }
        return s;
    }

    private const int MfccWindow = 400;   // 25 ms
    private const int Fft = 512;
    private const int MelBands = 24;
    public const int Cepstra = 12;
    private static float[][] melFilters;
    private static float[] hamming;

    /// <summary>c1..c12 of one 25 ms window.</summary>
    private static float[] Mfcc(float[] x, int start)
    {
        if (melFilters == null) BuildMel();
        double[] re = new double[Fft], im = new double[Fft];
        for (int i = 0; i < MfccWindow; i++)
        {
            int k = start + i;
            float v = k >= 0 && k < x.Length ? x[k] : 0f;
            float prev = k - 1 >= 0 && k - 1 < x.Length ? x[k - 1] : 0f;
            re[i] = (v - 0.97f * prev) * hamming[i];   // pre-emphasis
        }
        FftInPlace(re, im);
        double[] power = new double[Fft / 2 + 1];
        for (int i = 0; i <= Fft / 2; i++) power[i] = re[i] * re[i] + im[i] * im[i];

        double[] logMel = new double[MelBands];
        for (int b = 0; b < MelBands; b++)
        {
            double e = 0;
            float[] w = melFilters[b];
            for (int i = 0; i < w.Length; i++) e += w[i] * power[i];
            logMel[b] = Math.Log(e + 1e-10);
        }
        float[] c = new float[Cepstra];
        for (int n = 1; n <= Cepstra; n++)
        {
            double sum = 0;
            for (int b = 0; b < MelBands; b++) sum += logMel[b] * Math.Cos(Math.PI * n * (b + 0.5) / MelBands);
            c[n - 1] = (float)sum;
        }
        return c;
    }

    private static void BuildMel()
    {
        hamming = new float[MfccWindow];
        for (int i = 0; i < MfccWindow; i++) hamming[i] = (float)(0.54 - 0.46 * Math.Cos(2 * Math.PI * i / (MfccWindow - 1)));
        Func<double, double> mel = hz => 2595 * Math.Log10(1 + hz / 700);
        Func<double, double> hz_ = m => 700 * (Math.Pow(10, m / 2595) - 1);
        double lo = mel(100), hi = mel(7000);
        double[] edges = new double[MelBands + 2];
        for (int i = 0; i < edges.Length; i++) edges[i] = hz_(lo + (hi - lo) * i / (MelBands + 1)) * Fft / SampleRate;
        float[][] f = new float[MelBands][];
        for (int b = 0; b < MelBands; b++)
        {
            f[b] = new float[Fft / 2 + 1];
            for (int i = 0; i <= Fft / 2; i++)
            {
                double w = 0;
                if (i >= edges[b] && i <= edges[b + 1]) w = (i - edges[b]) / (edges[b + 1] - edges[b]);
                else if (i > edges[b + 1] && i <= edges[b + 2]) w = (edges[b + 2] - i) / (edges[b + 2] - edges[b + 1]);
                f[b][i] = (float)w;
            }
        }
        melFilters = f;
    }

    /// <summary>Iterative radix-2 FFT.</summary>
    private static void FftInPlace(double[] re, double[] im)
    {
        int n = re.Length;
        for (int i = 1, j = 0; i < n; i++)
        {
            int bit = n >> 1;
            for (; (j & bit) != 0; bit >>= 1) j ^= bit;
            j ^= bit;
            if (i < j) { double t = re[i]; re[i] = re[j]; re[j] = t; t = im[i]; im[i] = im[j]; im[j] = t; }
        }
        for (int len = 2; len <= n; len <<= 1)
        {
            double ang = -2 * Math.PI / len;
            double wr = Math.Cos(ang), wi = Math.Sin(ang);
            for (int i = 0; i < n; i += len)
            {
                double cr = 1, ci = 0;
                for (int k = 0; k < len / 2; k++)
                {
                    int a = i + k, b = a + len / 2;
                    double tr = re[b] * cr - im[b] * ci, ti = re[b] * ci + im[b] * cr;
                    re[b] = re[a] - tr; im[b] = im[a] - ti;
                    re[a] += tr; im[a] += ti;
                    double ncr = cr * wr - ci * wi;
                    ci = cr * wi + ci * wr;
                    cr = ncr;
                }
            }
        }
    }

    /// <summary>
    /// YIN pitch of one window, 0 when it is not voiced. aperiodic is the
    /// depth of the best dip of the normalised difference function (0 = a
    /// perfectly periodic sound, 1 = noise).
    /// </summary>
    private const int PitchRate = SampleRate / 2;

    private static float Yin(float[] x, int start, int window, int rate, out float aperiodic)
    {
        aperiodic = 1f;
        int minLag = (int)(rate / MaxPitchHz);
        int maxLag = Math.Min(window / 2, (int)(rate / MinPitchHz));
        int len = window - maxLag;
        if (len <= 0 || start + window > x.Length) return 0f;

        double energy = 0;
        for (int i = 0; i < window; i++) energy += x[start + i] * x[start + i];
        if (energy < 1e-9 * window) return 0f;

        double[] d = new double[maxLag + 1];
        for (int lag = 1; lag <= maxLag; lag++)
        {
            double sum = 0;
            for (int i = 0; i < len; i++)
            {
                double diff = x[start + i] - x[start + i + lag];
                sum += diff * diff;
            }
            d[lag] = sum;
        }

        double running = 0;
        for (int lag = 1; lag <= maxLag; lag++)
        {
            running += d[lag];
            d[lag] = running > 0 ? d[lag] * lag / running : 1;
        }

        double best = 1;
        for (int lag = minLag; lag < maxLag; lag++) best = Math.Min(best, d[lag]);
        aperiodic = (float)best;

        for (int lag = minLag; lag < maxLag; lag++)
        {
            if (d[lag] < YinThreshold)
            {
                while (lag + 1 < maxLag && d[lag + 1] < d[lag]) lag++;
                double p = d[lag - 1], q = d[lag], r = d[lag + 1];
                double shift = (p - 2 * q + r) != 0 ? 0.5 * (p - r) / (p - 2 * q + r) : 0;
                return (float)(rate / (lag + shift));
            }
        }
        return 0f;
    }

    /// <summary>
    /// Resamples with a windowed-sinc low-pass when going down (so a 44.1 kHz
    /// recording's hiss above 8 kHz does not fold back into the band the
    /// measurements look at), linear interpolation when going up.
    /// </summary>
    public static float[] Resample(float[] x, int from, int to)
    {
        if (x == null || x.Length == 0 || from == to) return x;
        double ratio = (double)from / to;
        int n = (int)(x.Length / ratio);
        float[] y = new float[n];

        if (ratio <= 1.0)
        {
            for (int i = 0; i < n; i++)
            {
                double pos = i * ratio;
                int k = (int)pos;
                double t = pos - k;
                float a = x[Math.Min(k, x.Length - 1)], b = x[Math.Min(k + 1, x.Length - 1)];
                y[i] = (float)(a + (b - a) * t);
            }
            return y;
        }

        const int HalfTaps = 12;
        double cutoff = 0.95 / ratio;   // a little under the new Nyquist
        for (int i = 0; i < n; i++)
        {
            double centre = i * ratio;
            int c = (int)centre;
            int span = (int)Math.Ceiling(HalfTaps * ratio);
            double sum = 0, norm = 0;
            for (int k = c - span; k <= c + span; k++)
            {
                if (k < 0 || k >= x.Length) continue;
                double t = (k - centre) * cutoff;
                double sinc = Math.Abs(t) < 1e-9 ? 1 : Math.Sin(Math.PI * t) / (Math.PI * t);
                double w = 0.5 + 0.5 * Math.Cos(Math.PI * (k - centre) / (span + 1));
                double h = sinc * w;
                sum += x[k] * h;
                norm += h;
            }
            y[i] = norm != 0 ? (float)(sum / norm) : 0f;
        }
        return y;
    }

    /// <summary>The p-th percentile (0..1) of a list, which it sorts.</summary>
    public static float Percentile(List<float> values, float p)
    {
        if (values == null || values.Count == 0) return float.NaN;
        values.Sort();
        int i = (int)Math.Round(p * (values.Count - 1));
        return values[Math.Max(0, Math.Min(values.Count - 1, i))];
    }

    public static float Clamp01(float v) { return v < 0 ? 0 : v > 1 ? 1 : v; }

    public static float Sigmoid(float v) { return (float)(1.0 / (1.0 + Math.Exp(-v))); }
}
