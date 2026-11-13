"""Synthesise the sound of Kylo brushing his teeth.

Made here rather than downloaded, for the reasons Tools/make_wakeup_sfx.py
and Tools/make_ui_click.py give: a dial beats whatever a stranger recorded,
and nothing downloaded means nothing to credit to anybody but us.

There is a third reason here. This sound plays for five seconds while the
microphone is open - longer than any other effect in the game - so it has to
be well clear of the band a child's voice lives in, or the recogniser will
hear the game brushing and score it as speech. Bristles are naturally high
and sibilant, so the whole effect sits above 1.6 kHz and nothing below that
is allowed through.

WHAT IT IS MADE OF

  bristles   white noise through a bandpass, 1.6-7 kHz. That IS the sound of
             a toothbrush; everything else here only shapes it.
  strokes    the amplitude swells and falls twice a second, once per stroke,
             and the stroke is asymmetric - a fast scrub out, a slower drag
             back - because an even sine reads as a machine rather than a
             hand. The rate matches the animation: two frames alternating at
             four a second is two strokes a second.
  tilt       each stroke's centre frequency rides up and down a little, so
             consecutive strokes are not the same noise burst twice.
  knock      a faint, very short low thud at the start of a few strokes, the
             brush meeting teeth. Kept quiet and sparse; it is seasoning.

The shape across the whole clip matches what the player sees: a moment while
he lifts the brush, four seconds of brushing, then a short tail as he lowers
it.

    python3 Tools/make_brush_sfx.py

Writes Assets/Resources/Sfx/brush_teeth.wav, where SoundEffects.Play finds
it by the id "brush_teeth".
"""
import os
import wave

import numpy as np

RATE = 44100
OUT = os.path.join('Assets', 'Resources', 'Sfx')
NAME = 'brush_teeth'

LIFT = 0.50             # seconds before the first stroke - he is raising it
BRUSH = 4.00            # seconds of brushing
TAIL = 0.25             # seconds of fade as he lowers it
STROKES_PER_SEC = 2.0   # matches two frames alternating at four a second

LOW_HZ = 1600.0         # nothing below this: a child's voice lives down there
HIGH_HZ = 7000.0


def bandpass(x, low, high):
    """Brick-wall in the frequency domain - clean, and the edges do not matter."""
    spectrum = np.fft.rfft(x)
    freqs = np.fft.rfftfreq(len(x), 1.0 / RATE)
    spectrum[(freqs < low) | (freqs > high)] = 0.0
    return np.fft.irfft(spectrum, n=len(x))


def stroke_envelope(t):
    """Two strokes a second, each a fast scrub out and a slower drag back."""
    phase = (t * STROKES_PER_SEC) % 1.0
    out = np.clip(phase / 0.35, 0.0, 1.0)              # fast rise
    back = np.clip((1.0 - phase) / 0.65, 0.0, 1.0)     # slower fall
    env = np.minimum(out, back) ** 1.4
    return 0.18 + 0.82 * env                           # never quite silent


def main():
    n = int(RATE * (LIFT + BRUSH + TAIL))
    t = np.arange(n) / float(RATE)
    rng = np.random.default_rng(33)                    # Group 33, and repeatable

    noise = rng.standard_normal(n)
    bristles = bandpass(noise, LOW_HZ, HIGH_HZ)
    bristles /= max(float(np.max(np.abs(bristles))), 1e-9)

    # A second, brighter copy faded in and out by a slow wander gives each
    # stroke a slightly different colour without a filter sweep per stroke.
    bright = bandpass(noise, 3800.0, HIGH_HZ)
    bright /= max(float(np.max(np.abs(bright))), 1e-9)
    tilt = 0.5 + 0.5 * np.sin(2 * np.pi * 0.37 * t)
    body = (1.0 - 0.45 * tilt) * bristles + 0.45 * tilt * bright

    env = stroke_envelope(t - LIFT)
    env[t < LIFT] = 0.0
    fade_in = np.clip((t - LIFT) / 0.12, 0.0, 1.0)
    fade_out = np.clip((LIFT + BRUSH + TAIL - t) / TAIL, 0.0, 1.0)
    y = body * env * fade_in * fade_out * 0.55

    # the brush meeting teeth, on every third stroke, barely there
    knock_period = 3.0 / STROKES_PER_SEC
    for k in range(int(BRUSH / knock_period) + 1):
        start = int(RATE * (LIFT + k * knock_period))
        length = int(RATE * 0.03)
        if start + length >= n:
            break
        decay = np.exp(-np.arange(length) / (RATE * 0.006))
        tone = np.sin(2 * np.pi * 240.0 * np.arange(length) / RATE)
        y[start:start + length] += 0.05 * decay * tone

    y = np.tanh(1.2 * y) / np.tanh(1.2)
    y *= 0.85 / max(float(np.max(np.abs(y))), 1e-9)

    os.makedirs(OUT, exist_ok=True)
    path = os.path.join(OUT, NAME + '.wav')
    s = np.clip(np.rint(y * 32767.0), -32768, 32767).astype('<i2')
    with wave.open(path, 'wb') as w:
        w.setnchannels(1)
        w.setsampwidth(2)
        w.setframerate(RATE)
        w.writeframes(s.tobytes())
    print('%s  %.2f s, %d strokes, %.0f-%.0f Hz'
          % (path, len(s) / float(RATE), BRUSH * STROKES_PER_SEC, LOW_HZ, HIGH_HZ))


if __name__ == '__main__':
    main()
