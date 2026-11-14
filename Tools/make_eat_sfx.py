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

WHAT IT IS MADE OF, and what it is NOT made of.

The first version of this built the chewing out of filtered noise, the way
the brushing is built. That was wrong, and it is worth saying why: brushing
IS noise - bristles scrubbing - but chewing is not. A mouth is a small
resonant box opening and closing, and what you actually hear is a series of
soft SMACKS, each one a brief resonance that dies almost at once. Noise in
that band just sounds like static, which is what it sounded like.

So the chewing here is made of transients, not texture:

  smack    a very short noise click through three damped resonators at
           roughly 420, 980 and 1900 Hz - the mouth's own formants - each
           decaying inside 50 ms. Two or three per mouthful, lightly
           varied in pitch and spacing so the jaw does not sound like a
           metronome.
  clink    the spoon against the china, once as he lifts it and once as
           each mouthful starts. Noise through a 2.5-7 kHz bandpass with a
           very fast decay, plus two faint partials so it rings like a
           plate rather than clicking like a switch.
  mmm      a hummed, contented note near the end: a 250 Hz fundamental
           with a couple of harmonics, gliding down a tone, under a slow
           swell. Quiet on purpose - see below.

THE "MMM" IS KEPT SHORT AND SOFT, and that is deliberate rather than shy.
make_brush_sfx.py keeps its whole effect above 1.6 kHz so the recogniser
cannot mistake it for a child's voice. A hum cannot do that - it lives
exactly where speech lives. What protects it instead is WHEN it plays, the
argument SoundEffects.cs makes: Kylo only moves after a word has landed,
while the controller holds the next one off and nothing is being scored.
So it is allowed, but it is 0.45 s long and well down in the mix.

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


def resonators(n, freqs, decays, gains):
    """Sum of damped sines - a small resonant box rung once."""
    t = np.arange(n) / RATE
    out = np.zeros(n)
    for f, d, g in zip(freqs, decays, gains):
        out += g * np.sin(2 * np.pi * f * t) * np.exp(-t / d)
    return out


def smack(seed, pitch=1.0):
    """One soft chew: the mouth opening and closing, not a hiss of noise."""
    rng = np.random.default_rng(seed)
    n = int(0.09 * RATE)
    # A two-millisecond click to excite the resonances, then the box rings.
    click = np.zeros(n)
    k = int(0.002 * RATE)
    click[:k] = rng.normal(size=k)
    body = resonators(n,
                      [420 * pitch, 980 * pitch, 1900 * pitch],
                      [0.030, 0.018, 0.009],
                      [1.0, 0.55, 0.25])
    out = body + 0.35 * bandpass(click, 200.0, 2600.0)
    out *= np.exp(-np.arange(n) / (RATE * 0.045))
    return out / max(float(np.max(np.abs(out))), 1e-9)


def mouthful(seed, length_s):
    """Two or three smacks, unevenly spaced, inside one mouthful."""
    rng = np.random.default_rng(seed)
    n = int(length_s * RATE)
    out = np.zeros(n)
    count = 3 if rng.random() < 0.6 else 2
    for j in range(count):
        at = (0.04 + j * (0.62 / count) + rng.uniform(-0.02, 0.02)) * length_s
        piece = smack(seed * 17 + j, pitch=float(rng.uniform(0.88, 1.14)))
        start = int(at * RATE)
        end = min(n, start + len(piece))
        if end > start:
            out[start:end] += float(rng.uniform(0.75, 1.0)) * piece[:end - start]
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

    # Three mouthfuls across the looping part of the animation.
    step = LOOP / BITES
    for i in range(BITES):
        at = LEAD + i * step
        add(y, at, clink(10 + i, 0.13), 0.22)        # spoon meets the plate
        add(y, at + 0.10, mouthful(20 + i, step * 0.72), 0.34)

    # The last swallow, and a small pleased hum over the tail.
    add(y, LEAD + LOOP - 0.10, smack(40, 0.92), 0.26)
    add(y, TOTAL - 0.52, mmm(), 0.20)

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
    print('%s  %.2f s, %d mouthfuls of resonant smacks'
          % (path, len(s) / float(RATE), BITES))


if __name__ == '__main__':
    main()
