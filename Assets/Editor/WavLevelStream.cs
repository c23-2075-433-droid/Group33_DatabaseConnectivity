using System;
using System.Collections.Generic;
using System.IO;

/// <summary>
/// Turns a narrator recording into the level readings a phone's speech
/// recognizer would report while hearing it, so a recording can be measured
/// by StressAnalyzer exactly like a child's attempt.
///
/// Plain C# with no UnityEditor dependency on purpose: the stand-alone tests in
/// Tools/stresstests compile this same file, so the references the game uses
/// and the numbers the tests check are produced by one piece of code.
///
/// WHAT IT IMITATES
///
/// Android's recognizer reports rmsdB per audio buffer, roughly -2 for silence
/// up to about 10 for a loud voice, and the plugin turns that into
/// Clamp01(0.1 * 10^(dB / 10)). So each hop of audio becomes its RMS level in
/// decibels, placed so the loudest hop sits at peakDb and clamped to the
/// recognizer's -2..10 window, then scaled the plugin's way. Half a second of
/// quiet room noise goes either side, because a real session always has the
/// moments before and after the child speaks.
/// </summary>
public static class WavLevelStream
{
    public const float DefaultHopMs = 50f;
    public const float DefaultPeakDb = 9f;

    /// <summary>Level readings for a WAV file, Android-scaled.</summary>
    public static List<float> FromWav(string path, float hopMs = DefaultHopMs, float peakDb = DefaultPeakDb)
    {
        int sampleRate;
        float[] samples = ReadMono(path, out sampleRate);
        return FromSamples(samples, sampleRate, hopMs, peakDb);
    }

    /// <summary>Level readings for mono samples in -1..1, Android-scaled.</summary>
    public static List<float> FromSamples(float[] samples, int sampleRate, float hopMs = DefaultHopMs, float peakDb = DefaultPeakDb)
    {
        int pad = sampleRate / 2;
        float[] x = new float[samples.Length + 2 * pad];
        Array.Copy(samples, 0, x, pad, samples.Length);

        // Quiet room noise, deterministic so the references never wobble.
        Random rng = new Random(12345);
        const double noise = 0.0018;   // about -55 dBFS
        for (int i = 0; i < x.Length; i++) x[i] += (float)((rng.NextDouble() * 2 - 1) * noise);

        int hop = Math.Max(1, (int)(sampleRate * hopMs / 1000f));
        List<double> dbfs = new List<double>();
        for (int s = 0; s + hop <= x.Length; s += hop)
        {
            double sq = 0;
            for (int i = s; i < s + hop; i++) sq += x[i] * x[i];
            dbfs.Add(20 * Math.Log10(Math.Sqrt(sq / hop) + 1e-9));
        }

        double peak = double.MinValue;
        foreach (double d in dbfs) peak = Math.Max(peak, d);

        List<float> levels = new List<float>(dbfs.Count);
        foreach (double d in dbfs)
        {
            double rms = d - peak + peakDb;
            if (rms < -2) rms = -2;
            if (rms > 10) rms = 10;
            double level = 0.1 * Math.Pow(10, rms / 10);
            levels.Add((float)Math.Min(1.0, level));
        }
        return levels;
    }

    /// <summary>Reads a PCM WAV (8/16/24/32-bit int or 32-bit float), mixed to mono, in -1..1.</summary>
    public static float[] ReadMono(string path, out int sampleRate)
    {
        using (BinaryReader r = new BinaryReader(File.OpenRead(path)))
        {
            if (new string(r.ReadChars(4)) != "RIFF") throw new InvalidDataException(path + ": not a RIFF file");
            r.ReadInt32();
            if (new string(r.ReadChars(4)) != "WAVE") throw new InvalidDataException(path + ": not a WAVE file");

            int format = 0, channels = 0, bits = 0;
            sampleRate = 0;
            while (r.BaseStream.Position + 8 <= r.BaseStream.Length)
            {
                string id = new string(r.ReadChars(4));
                int size = r.ReadInt32();
                long next = r.BaseStream.Position + size + (size & 1);

                if (id == "fmt ")
                {
                    format = r.ReadInt16();
                    channels = r.ReadInt16();
                    sampleRate = r.ReadInt32();
                    r.ReadInt32();
                    r.ReadInt16();
                    bits = r.ReadInt16();
                    if (format == unchecked((short)0xFFFE) || format == -2)
                    {
                        // WAVE_FORMAT_EXTENSIBLE: the real format is in the sub-format GUID.
                        r.ReadInt16(); r.ReadInt16(); r.ReadInt32();
                        format = r.ReadInt16();
                    }
                }
                else if (id == "data")
                {
                    if (channels <= 0 || bits <= 0) throw new InvalidDataException(path + ": data before fmt");
                    int bytesPer = bits / 8;
                    int frames = size / (bytesPer * channels);
                    float[] mono = new float[frames];
                    for (int f = 0; f < frames; f++)
                    {
                        double sum = 0;
                        for (int c = 0; c < channels; c++) sum += ReadSample(r, format, bits);
                        mono[f] = (float)(sum / channels);
                    }
                    return mono;
                }
                r.BaseStream.Position = Math.Min(next, r.BaseStream.Length);
            }
            throw new InvalidDataException(path + ": no data chunk");
        }
    }

    private static double ReadSample(BinaryReader r, int format, int bits)
    {
        if (format == 3 && bits == 32) return r.ReadSingle();
        switch (bits)
        {
            case 8: return (r.ReadByte() - 128) / 128.0;
            case 16: return r.ReadInt16() / 32768.0;
            case 24:
                int b = r.ReadByte() | (r.ReadByte() << 8) | (r.ReadByte() << 16);
                if ((b & 0x800000) != 0) b |= unchecked((int)0xFF000000);
                return b / 8388608.0;
            case 32: return r.ReadInt32() / 2147483648.0;
            default: throw new InvalidDataException("unsupported bit depth " + bits);
        }
    }
}
