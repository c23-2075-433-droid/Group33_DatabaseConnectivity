"""Synthesise the sound of Kylo eating his breakfast.

Made here rather than downloaded, for the reasons Tools/make_brush_sfx.py
and Tools/make_ui_click.py give: a dial beats whatever a stranger recorded,
and nothing downloaded means nothing to credit to anybody but us.

LENGTH IS NOT A FREE CHOICE. It is the length of the animation it plays
over, the same way brush_teeth.wav is 4.75 s because the brushing is:

    lead-in   1 frame  / 4 fps   = 0.25 s   the spoon comes up
    loop      3 x 2    / 2.5 fps = 2.40 s   three mouthfuls
    tail      1 frame  / 4 fps   = 0.25 s   he swallows the last
                                  = 2.90 s

Change one and change the other, or he goes on chewing in silence.

WHAT IT IS MADE OF, and the two things it has already not been.

First it was filtered noise, the way the brushing is built. That was wrong:
brushing IS noise - bristles scrubbing - but chewing is not, and noise in
that band is just static. Then it was resonant smacks, which is what a real
mouth does but came out dry and easy to miss.

What it is now is a gentle "nom nom nom", which is asked for rather than
deduced, and is the right answer for a children's game: it is legible as
EATING in a way a realistic chew is not.

  nom      a voiced syllable, built the way the mouth builds one. "N" is
           the lips closed and the sound coming down the nose, so it
           starts dark - everything above about 900 Hz rolled off. The
           vowel opens: the filter sweeps up and the second formant comes
           in around 1100 Hz. Then "M" closes it again, back to dark. One
           amplitude swell and one brightness swell, slightly out of step,
           which is the whole trick.
           Three of them, a little lower and a little softer each time, so
           it is a child chewing rather than a loop.
  clink    the spoon against the china, once as he lifts it and once as
           each mouthful starts. Noise through a 2.5-7 kHz bandpass with a
           very fast decay, plus two faint partials so it rings like a
           plate rather than clicking like a switch. Kept well under the
           voice, which is now the point of the effect.
  mmm      the contented note at the end, unchanged.

GENTLE IS A LEVEL, NOT A SHAPE. Soft attacks, nothing above 2 kHz in the
voiced part, and the whole thing mixed low enough to sit under Rachel.

THE VOICED PARTS ARE A KNOWN RISK, taken with eyes open. make_brush_sfx.py
keeps its whole effect above 1.6 kHz precisely so the recogniser cannot
mistake it for a child's voice, and "nom nom nom" cannot do that - it lives
exactly where speech lives, and there is now more of it than the old hum.
What protects it is WHEN it plays, the argument SoundEffects.cs makes: Kylo
only moves after a word has landed, while the controller holds the next one
off and nothing is being scored. If a stray "nom" is ever heard as an
answer, this is the first thing to look at, and dropping NOM_GAIN is the
first thing to try.

Writes Assets/Resources/Sfx/eat_rice.wav, where SoundEffects.Play finds it.

    python3 Tools/make_eat_sfx.py
"""
import os
import wave

import numpy as np

RATE = 44100
OUT = os.path.join('Assets', 'Resources', 'Sfx')
NAME = 'eat_rice'

LEAD = 0.25
LOOP = 2.40
TAIL = 0.25
TOTAL = LEAD + LOOP + TAIL

BITES = 3                      # loopCycles in BuildBahayScene5
CLINK_LOW, CLINK_HIGH = 2500.0, 7000.0


def bandpass(x, low, high):
    """Brick-wall in the frequency domain - clean, and the edges do not matter."""
    spectrum = np.fft.rfft(x)
    freqs = np.fft.rfftfreq(len(x), 1.0 / RATE)
    spectrum[(freqs < low) | (freqs > high)] = 0.0
    return np.fft.irfft(spectrum, n=len(x))


def add(y, start_s, piece, gain=1.0):
    start = int(start_s * RATE)
    end = min(len(y), start + len(piece))
    if end > start:
        y[start:end] += gain * piece[:end - start]


def clink(seed, length_s=0.16):
    """Spoon on china: a short bright scatter with two faint partials."""
    rng = np.random.default_rng(seed)
    n = int(length_s * RATE)
    t = np.arange(n) / RATE
    body = bandpass(rng.normal(size=n), CLINK_LOW, CLINK_HIGH)
    body *= np.exp(-t / 0.018)
    ring = (np.sin(2 * np.pi * 3150 * t) * 0.35
            + np.sin(2 * np.pi * 4720 * t) * 0.2) * np.exp(-t / 0.055)
    out = body / max(float(np.max(np.abs(body))), 1e-9) + ring
    return out / max(float(np.max(np.abs(out))), 1e-9)


NOM_GAIN = 0.30        # the dial to turn down if the recogniser hears it


def nom(seed, f0=250.0, length_s=0.26):
    """One gentle "nom": nose, vowel, nose - dark, open, dark again."""
    rng = np.random.default_rng(seed)
    n = int(length_s * RATE)
    t = np.arange(n) / RATE
    frac = np.linspace(0.0, 1.0, n)

    # A voiced buzz with a little natural drift, falling slightly.
    f = f0 * (1.0 - 0.07 * frac) * (1.0 + 0.004 * np.sin(2 * np.pi * 5.5 * t))
    phase = 2 * np.pi * np.cumsum(f) / RATE
    buzz = (np.sin(phase) + 0.38 * np.sin(2 * phase)
            + 0.16 * np.sin(3 * phase) + 0.07 * np.sin(4 * phase))

    # "N" and "M" are the same gesture: lips shut, sound down the nose, so
    # nothing bright gets out. The vowel in the middle opens up. Crossfade a
    # dark copy with a brighter one rather than sweeping a filter in slices.
    dark = bandpass(buzz, 90.0, 900.0)
    open_ = bandpass(buzz, 90.0, 2000.0)
    openness = np.sin(np.pi * frac) ** 2          # shut, open, shut
    body = dark * (1.0 - openness) + open_ * openness

    # Amplitude swells a touch LATER than the mouth opens, which is what
    # stops it sounding like a note and starts it sounding like a syllable.
    env = np.sin(np.pi * np.clip(frac * 1.08 - 0.04, 0.0, 1.0)) ** 1.25
    out = body * env * float(rng.uniform(0.92, 1.0))
    return out / max(float(np.max(np.abs(out))), 1e-9)


def mmm(length_s=0.45):
    """A contented hum: a fundamental and two harmonics, gliding down a tone."""
    n = int(length_s * RATE)
    t = np.arange(n) / RATE
    f = np.linspace(252.0, 225.0, n)              # a tone down, satisfied
    phase = 2 * np.pi * np.cumsum(f) / RATE
    tone = (np.sin(phase)
            + 0.34 * np.sin(2 * phase)
            + 0.12 * np.sin(3 * phase))
    # Lips closed, so nothing bright gets out.
    tone = bandpass(tone, 80.0, 900.0)
    frac = np.linspace(0.0, 1.0, n)
    env = np.sin(np.pi * frac) ** 1.5
    out = tone * env
    return out / max(float(np.max(np.abs(out))), 1e-9)


def main():
    y = np.zeros(int(TOTAL * RATE))

    # He lifts the spoon off the plate.
    add(y, 0.02, clink(1), 0.38)

    # Three mouthfuls across the looping part of the animation. Each nom
    # sits a little lower and a little quieter than the one before.
    step = LOOP / BITES
    for i in range(BITES):
        at = LEAD + i * step
        add(y, at, clink(10 + i, 0.13), 0.16)        # spoon meets the plate
        add(y, at + 0.12, nom(20 + i, f0=250.0 - 11.0 * i),
            NOM_GAIN - 0.03 * i)

    # The contented hum over the tail.
    add(y, TOTAL - 0.52, mmm(), 0.18)

    y = np.tanh(1.2 * y) / np.tanh(1.2)
    y *= 0.72 / max(float(np.max(np.abs(y))), 1e-9)

    os.makedirs(OUT, exist_ok=True)
    path = os.path.join(OUT, NAME + '.wav')
    s = np.clip(np.rint(y * 32767.0), -32768, 32767).astype('<i2')
    with wave.open(path, 'wb') as w:
        w.setnchannels(1)
        w.setsampwidth(2)
        w.setframerate(RATE)
        w.writeframes(s.tobytes())
    print('%s  %.2f s, %d gentle noms'
          % (path, len(s) / float(RATE), BITES))


if __name__ == '__main__':
    main()
