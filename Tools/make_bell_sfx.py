"""Synthesise the school bell that rings when Kylo gets in before it.

WHY IT IS MADE HERE rather than downloaded, which is the same answer
Tools/make_ui_click.py and Tools/make_bath_sfx.py give: a dial beats whatever
a stranger recorded, and nothing downloaded means nothing to credit to
anybody but us. There is a second reason particular to this one. The bell is
not atmosphere, it is the PUNCHLINE of Paaralan Scene 1 - the whole scene is
Kylo hurrying to beat it - so it has to land on a frame we choose, be over
before the summary panel arrives, and never once sound like a fire alarm to
a six-year-old.

WHAT A BELL ACTUALLY IS, and why a sine will not do. A struck bell is
INHARMONIC: its partials are not whole multiples of the fundamental the way
a plucked string's are, which is exactly why a bell sounds like metal and a
flute does not. The ratios used here - 1, 2.02, 2.68, 3.46, 4.30, 5.42 - are
the classic strike-tone series, with the small detune on the second partial
that gives a real bell its slow warble. Each partial also decays at its own
rate, the high ones fastest, which is the other half of the sound: a bell
gets MELLOWER as it rings out, it does not simply get quieter.

THREE STRIKES, not one and not a continuous ring. One is a doorbell. A
continuous electric ring - a clapper buzzing against the gong twenty times a
second - is the sound of a real school bell and also the sound of an alarm,
and this moment is meant to be a happy one. Three spaced strikes read as
"the bell is going" without any of the urgency.

HOW LONG. 1.55 seconds, which has to fit between the end of the run and the
summary panel. The celebration is held well past the last strike, so the
bell finishes and the pose remains rather than the two ending together.

Writes  Assets/Resources/Sfx/school_bell.wav, found by the id "school_bell".

Run from the repository root:  python Tools/make_bell_sfx.py
"""
import os
import wave

import numpy as np

OUT = os.path.join("Assets", "Resources", "Sfx")
NAME = "school_bell"
RATE = 44100
SEED = 33

# The strike-tone series. (ratio, how loud, how long it rings in seconds.)
# High partials are both quieter and shorter, which is what makes the sound
# soften as it decays instead of just fading.
PARTIALS = [
    (1.000, 1.00, 1.30),
    (2.020, 0.70, 0.95),   # the detune against 2.0 is the warble
    (2.680, 0.50, 0.70),
    (3.460, 0.34, 0.50),
    (4.300, 0.22, 0.36),
    (5.420, 0.14, 0.26),
]

F0 = 720.0          # bright, and well clear of a child's speaking range
STRIKES = [0.00, 0.38, 0.76]
LENGTH = 1.55


def seconds(n):
    return np.arange(int(n * RATE), dtype=np.float64) / RATE


def one_strike(t):
    """A single hit: the partials, plus the knock of the clapper itself."""
    y = np.zeros_like(t)
    for ratio, amp, decay in PARTIALS:
        y += amp * np.sin(2.0 * np.pi * F0 * ratio * t) * np.exp(-t / (decay / 4.0))

    # The clapper. Without this the bell starts out of nowhere; metal struck
    # by metal has a click in the first few milliseconds that carries most of
    # the "hit". Shaped noise, gone in 8 ms.
    rng = np.random.default_rng(SEED)
    knock = rng.standard_normal(len(t)) * np.exp(-t / 0.004)
    return y * 0.8 + knock * 0.18


def main():
    t = seconds(LENGTH)
    out = np.zeros_like(t)
    for i, start in enumerate(STRIKES):
        n = int(start * RATE)
        if n >= len(t):
            continue
        tail = t[: len(t) - n]
        # Each strike a little softer than the last, the way a hand-swung
        # bell loses energy. Dead-level repeats sound mechanical.
        out[n:] += one_strike(tail) * (1.0 - 0.18 * i)

    # Ramp both ends: a clip that stops mid-cycle clicks.
    a, r = int(0.003 * RATE), int(0.030 * RATE)
    out[:a] *= np.linspace(0.0, 1.0, a)
    out[-r:] *= np.linspace(1.0, 0.0, r)
    out = np.tanh(1.2 * out) / np.tanh(1.2)
    out *= 0.9 / max(float(np.max(np.abs(out))), 1e-9)

    os.makedirs(OUT, exist_ok=True)
    path = os.path.join(OUT, NAME + ".wav")
    s = np.clip(np.rint(out * 32767.0), -32768, 32767).astype("<i2")
    with wave.open(path, "wb") as w:
        w.setnchannels(1)
        w.setsampwidth(2)
        w.setframerate(RATE)
        w.writeframes(s.tobytes())
    print("%s  %.2f s  %d Hz mono" % (path, len(s) / float(RATE), RATE))


if __name__ == "__main__":
    main()
