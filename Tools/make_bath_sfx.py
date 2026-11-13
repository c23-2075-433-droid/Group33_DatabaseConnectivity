"""Synthesise the water Kylo tips over himself when the child says "Maligo".

Made here rather than downloaded, like the wake-up sounds, the UI click and
the toothbrush: a dial beats whatever a stranger recorded, and nothing
downloaded means nothing to credit to anybody but us.

It is cut to the length of the thing it is under. fx_bath fades in over
0.5s, holds 1.2s and clears over 0.9s, so the sound pours for 2.6s and no
longer - the water should not still be running over a dry bathroom.

WHAT IT IS MADE OF. Three layers, because water is not one sound:

  splash     the first half second. Broadband noise with a fast decay, the
             moment the tabo-ful lands on him.
  pour       the body. Noise through a bandpass that drifts slowly, which
             is what separates a pour from radio static: real water wanders
             in pitch as the stream breaks up.
  plinks     six droplets, each a short sine sweeping UP in pitch with a
             quick decay. Counter-intuitive, but a drip rises - the note is
             the bubble shrinking - and it is the only part of this that
             says "water" rather than "noise".

Nothing below 500 Hz. SoundEffects' own note explains why that matters:
this game listens to the child almost continuously and the microphone hears
the device's speaker. Effects get away with it because of WHEN they play -
during the window the objective controller holds open after a word lands,
when nothing is being scored - but keeping out of the band a child's voice
lives in costs nothing here and makes the sound brighter anyway.

    python3 Tools/make_bath_sfx.py

Writes Assets/Resources/Sfx/bath_water.wav, found by the id "bath_water".
"""
import os
import wave

import numpy as np

RATE = 44100
OUT = os.path.join('Assets', 'Resources', 'Sfx')
NAME = 'bath_water'

# fx_bath: 0.5 in, 1.2 hold, 0.9 out
LENGTH = 2.6
SPLASH = 0.5
LOW_HZ = 500.0
HIGH_HZ = 9000.0
PLINKS = 6


def bandpass(x, low, high):
    spectrum = np.fft.rfft(x)
    freqs = np.fft.rfftfreq(len(x), 1.0 / RATE)
    spectrum[(freqs < low) | (freqs > high)] = 0.0
    return np.fft.irfft(spectrum, n=len(x))


def main():
    n = int(RATE * LENGTH)
    t = np.arange(n) / float(RATE)
    rng = np.random.default_rng(33)

    noise = rng.standard_normal(n)

    # --- the pour: two bands crossfaded by a slow wander, so the stream
    #     breaks up instead of sitting still like static
    low = bandpass(noise, LOW_HZ, 2600.0)
    high = bandpass(noise, 2600.0, HIGH_HZ)
    for band in (low, high):
        band /= max(float(np.max(np.abs(band))), 1e-9)
    wander = 0.5 + 0.5 * np.sin(2 * np.pi * 0.8 * t + 1.1)
    pour = (1.0 - 0.4 * wander) * low + (0.25 + 0.4 * wander) * high

    swell = np.clip(t / 0.35, 0.0, 1.0)
    settle = np.clip((LENGTH - t) / 0.8, 0.0, 1.0)
    y = pour * swell * settle * 0.42

    # --- the splash that starts it
    splash_n = int(RATE * SPLASH)
    burst = bandpass(rng.standard_normal(splash_n), 700.0, HIGH_HZ)
    burst /= max(float(np.max(np.abs(burst))), 1e-9)
    y[:splash_n] += burst * np.exp(-np.arange(splash_n) / (RATE * 0.11)) * 0.55

    # --- droplets, each a short rising chirp
    for k in range(PLINKS):
        at = float(rng.uniform(0.45, LENGTH - 0.35))
        start = int(RATE * at)
        length = int(RATE * float(rng.uniform(0.05, 0.1)))
        if start + length >= n:
            continue
        local = np.arange(length) / float(RATE)
        f0 = float(rng.uniform(900.0, 1500.0))
        f1 = f0 * float(rng.uniform(1.6, 2.3))
        phase = 2 * np.pi * (f0 * local + 0.5 * (f1 - f0) / (length / RATE) * local ** 2)
        decay = np.exp(-local / 0.022)
        y[start:start + length] += 0.3 * decay * np.sin(phase)

    y = np.tanh(1.15 * y) / np.tanh(1.15)
    y *= 0.88 / max(float(np.max(np.abs(y))), 1e-9)
    # a hair of fade at each end so nothing clicks
    edge = int(RATE * 0.01)
    y[:edge] *= np.linspace(0.0, 1.0, edge)
    y[-edge:] *= np.linspace(1.0, 0.0, edge)

    os.makedirs(OUT, exist_ok=True)
    path = os.path.join(OUT, NAME + '.wav')
    s = np.clip(np.rint(y * 32767.0), -32768, 32767).astype('<i2')
    with wave.open(path, 'wb') as w:
        w.setnchannels(1)
        w.setsampwidth(2)
        w.setframerate(RATE)
        w.writeframes(s.tobytes())
    print('%s  %.2f s, splash + pour + %d droplets, %.0f Hz up'
          % (path, len(s) / float(RATE), PLINKS, LOW_HZ))


if __name__ == '__main__':
    main()
