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
        }
        return s;
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
