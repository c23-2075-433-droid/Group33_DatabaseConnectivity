"""Synthesise the four sounds of Kylo waking up: two snores, a yawn, a stretch.

Made here rather than downloaded, for the reason Tools/make_ui_click.py gives
and one more besides. The reason it gives: a dial beats whatever a stranger
recorded, and nothing downloaded means nothing to credit to anybody but us. The
extra reason: a snore is the one sound in this game that plays WHILE the child
is being listened to. It sits under the microphone for as long as it takes a
six-year-old to find the word "Bangon", so it has to be soft, short in the
spectrum where a child's voice is not, and free of the sudden peaks a sampled
snore is full of. That is a specification, not a search term.

Four files, because the snore is two of them. A loop baked into one file either
breathes mechanically or carries its own silence - and Tools/import_sfx.py
trims silence off every effect it brings in, as it should. So the rhythm lives
in WakeUpSequence.cs, which plays the inhale, waits, plays the exhale, rests,
and wanders the pitch a little each time round:

  snore_in   the inhale: a soft buzz around 100-130 Hz, fluttering at 19 Hz.
             The flutter is the whole sound - a steady buzz is a fridge, and a
             fridge is not funny. Rises in pitch across its length, the way
             breathing in does.
  snore_out  the exhale: a thin whistle falling 820 -> 430 Hz. This is the half
             that makes a cartoon snore read as cute rather than as a large man
             on a sofa.
  yawn       a vowel opening and closing - "aaah-ooohh" - built as a harmonic
             stack behind a lowpass that sweeps shut from 1700 to 650 Hz. The
             sweep IS the vowel; the pitch merely follows it.
  stretch    a slide-whistle up, 420 -> 1500 Hz, with a sproing wobbling off
             the top of it. The oldest joke in the cartoon sound book.

Everything is seeded, so a re-run produces the same files and re-importing is
not a silent change to a committed asset.

Run from the repository root, then bring each one in the way every other effect
arrives, so it is trimmed, levelled and written into CREDITS.txt:

    python3 Tools/make_wakeup_sfx.py
    for n in snore_in snore_out yawn stretch; do
        python3 Tools/import_sfx.py $n Recordings/sfx/${n}_raw.wav \
            --licence CC0 --title "Kylo waking up ($n, synthesised)" \
            --author "SALINLAHI (Tools/make_wakeup_sfx.py)"
    done

Plain python3, not `python3 -I`: that flag drops the user site directory,
where numpy lives.
"""
import os
import wave

import numpy as np

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
STAGE = os.path.join(ROOT, "Recordings", "sfx")

RATE = 44100
SEED = 33


def seconds(n):
    """The time axis for a clip of n seconds."""
    return np.arange(int(n * RATE), dtype=np.float64) / RATE


def phase_of(f):
    """Phase from a per-sample frequency curve.

    By integration, never sin(2*pi*f(t)*t) - make_ui_click.py learned that one
    the hard way: the second form sweeps at twice the rate it looks like it
    does and lands an octave out.
    """
    return 2.0 * np.pi * np.cumsum(f) / RATE


def harmonics(phase, count, weights=None):
    """A buzzy tone: `count` harmonics at 1/n, or at given weights.

    Summed harmonics rather than a true sawtooth because a sawtooth generated
    by sampling aliases, and aliasing on a 100 Hz drone is a shrill ring that
    no amount of filtering afterwards removes.
    """
    out = np.zeros_like(phase)
    norm = 0.0
    for n in range(1, count + 1):
        a = (1.0 / n) if weights is None else weights[n - 1]
        out += a * np.sin(n * phase)
        norm += abs(a)
    return out / max(norm, 1e-9)


def lowpass(x, hz):
    """A fixed one-pole lowpass, as a convolution with its own impulse response.

    Vectorised rather than looped because the cutoff does not move. Where it
    does move, use sweep_lowpass below and pay for the loop.
    """
    tau = RATE / (2.0 * np.pi * hz)
    n = int(min(len(x), max(8.0, tau * 6.0)))
    k = np.exp(-np.arange(n, dtype=np.float64) / tau)
    k /= k.sum()
    return np.convolve(x, k)[:len(x)]


def sweep_lowpass(x, hz):
    """A one-pole lowpass whose cutoff moves per sample.

    This is the yawn's vowel, so it is worth the Python loop: fifty thousand
    iterations is a few milliseconds, and there is no numpy spelling of a
    filter whose coefficient changes with every sample.
    """
    a = np.clip(2.0 * np.pi * np.asarray(hz) / RATE, 0.0, 1.0)
    y = np.empty_like(x)
    z = 0.0
    for i in range(len(x)):
        z += a[i] * (x[i] - z)
        y[i] = z
    return y


def noise(n, rng):
    return rng.standard_normal(n)


def smoothstep(x):
    x = np.clip(x, 0.0, 1.0)
    return x * x * (3.0 - 2.0 * x)


def finish(y, attack=0.004, release=0.010):
    """Ramp both ends to zero, round the peaks, normalise.

    The ramps are not cosmetic: a clip that starts or stops mid-cycle clicks,
    and import_sfx.py trims the silence that would otherwise hide it.
    """
    n = len(y)
    a = max(1, int(attack * RATE))
    r = max(1, int(release * RATE))
    y = y.copy()
    y[:a] *= np.linspace(0.0, 1.0, a)
    y[n - r:] *= np.linspace(1.0, 0.0, r)
    y = np.tanh(1.25 * y) / np.tanh(1.25)
    return y * (0.9 / max(float(np.max(np.abs(y))), 1e-9))


def write(name, y):
    path = os.path.join(STAGE, name + "_raw.wav")
    s = np.clip(np.rint(y * 32767.0), -32768, 32767).astype("<i2")
    with wave.open(path, "wb") as w:
        w.setnchannels(1)
        w.setsampwidth(2)
        w.setframerate(RATE)
        w.writeframes(s.tobytes())
    print("  -> %-28s %.3f s" % (os.path.relpath(path, ROOT), len(s) / float(RATE)))


# ----------------------------------------------------------------------
# the inhale
# ----------------------------------------------------------------------

def snore_in(rng):
    t = seconds(1.00)
    k = t / t[-1]

    # A child's snore, not a grown man's. 100 Hz rather than the 60-70 Hz a
    # sampled adult snore sits at: an octave up is the difference between
    # funny and unpleasant, and it also keeps the sound clear of the band a
    # six-year-old's vowels live in, which the recognizer needs.
    f0 = 100.0 + 32.0 * k
    body = harmonics(phase_of(f0), 7)

    # The flutter. 19 Hz is slow enough to hear as a rattle rather than as
    # roughness in the tone, and the depth is most of the way to full so the
    # sound genuinely stutters.
    flutter = 0.55 + 0.45 * np.sin(2.0 * np.pi * 19.0 * t - np.pi / 2.0)

    # Air. Band-passed by subtracting one lowpass from another, so it is
    # breath and not hiss.
    air = noise(len(t), rng)
    air = lowpass(air, 1100.0) - lowpass(air, 240.0)

    # Slow in, slower out: breathing in has no attack transient at all.
    env = np.minimum(smoothstep(t / 0.30), smoothstep((t[-1] - t) / 0.34))

    return finish((0.85 * body * flutter + 0.30 * air) * env, attack=0.010, release=0.030)


# ----------------------------------------------------------------------
# the exhale
# ----------------------------------------------------------------------

def snore_out(rng):
    t = seconds(0.72)
    k = t / t[-1]

    # Falling, and easing as it falls - a held note that slides down at a
    # constant rate reads as a machine winding down.
    eased = 1.0 - (1.0 - k) ** 2
    f = 820.0 - 390.0 * eased
    f *= 1.0 + 0.016 * np.sin(2.0 * np.pi * 5.5 * t)

    phase = phase_of(f)
    body = np.sin(phase) + 0.12 * np.sin(2.0 * phase)

    air = lowpass(noise(len(t), rng), 2400.0)

    # A bell, so it fades in and out of nothing. The exponent leans the peak
    # slightly early, which is where the breath is strongest.
    env = np.sin(np.pi * k) ** 1.25

    return finish((0.80 * body + 0.16 * air) * env, attack=0.012, release=0.030)


# ----------------------------------------------------------------------
# the yawn
# ----------------------------------------------------------------------

def yawn(rng):
    t = seconds(1.20)
    k = t / t[-1]

    # The pitch arches: up into the yawn, down out of it.
    f0 = np.where(k < 0.45,
                  185.0 + 115.0 * smoothstep(k / 0.45),
                  300.0 - 150.0 * smoothstep((k - 0.45) / 0.55))

    # A tremble at the end, which is what makes a yawn comic rather than sad.
    f0 *= 1.0 + 0.035 * np.sin(2.0 * np.pi * 7.0 * t) * smoothstep((k - 0.62) / 0.38)

    body = harmonics(phase_of(f0), 10)

    # The vowel. The mouth opens wide and closes again, and in a spectrum that
    # is a lowpass sweeping open and then shut - 1700 Hz at the widest, 650 at
    # the end. The pitch above merely follows; this is the part the ear hears
    # as "aaah-ooohh".
    cutoff = np.where(k < 0.40,
                      900.0 + 800.0 * smoothstep(k / 0.40),
                      1700.0 - 1050.0 * smoothstep((k - 0.40) / 0.60))
    body = sweep_lowpass(body, cutoff)

    # The intake of air, loudest early, gone by the end.
    air = lowpass(noise(len(t), rng), 1300.0) * np.exp(-k / 0.45)

    # Swells rather than starts, and lets go rather than stops.
    env = np.minimum(smoothstep(t / 0.34), smoothstep((t[-1] - t) / 0.48))

    return finish((1.00 * body + 0.26 * air) * env, attack=0.012, release=0.040)


# ----------------------------------------------------------------------
# the stretch
# ----------------------------------------------------------------------

def stretch(rng):
    t = seconds(0.78)
    rise = 0.42

    k = np.clip(t / rise, 0.0, 1.0)
    eased = 1.0 - (1.0 - k) ** 2                 # fast off the mark, settling at the top

    f = 420.0 + 1080.0 * eased
    f *= 1.0 + 0.012 * np.sin(2.0 * np.pi * 6.5 * t)

    # The sproing. Once the slide arrives it overshoots and wobbles off, which
    # is the whole joke; without it the sound is a kettle.
    after = np.clip(t - rise, 0.0, None)
    f += -320.0 * smoothstep(after / 0.14) \
        + 190.0 * np.exp(-after / 0.11) * np.sin(2.0 * np.pi * 13.0 * after) * (t >= rise)

    phase = phase_of(f)
    body = np.sin(phase) + 0.18 * np.sin(2.0 * phase)

    # Rubber under tension, only while it is being pulled.
    creak = lowpass(noise(len(t), rng), 3200.0)
    creak *= (0.5 + 0.5 * np.sin(2.0 * np.pi * 31.0 * t)) * (t < rise)

    env = np.minimum(smoothstep(t / 0.018), 1.0)
    env = env * np.where(t < rise, 1.0, np.exp(-after / 0.17))

    return finish((0.88 * body + 0.10 * creak) * env, attack=0.003, release=0.020)


def main():
    os.makedirs(STAGE, exist_ok=True)

    for name, make in (("snore_in", snore_in),
                       ("snore_out", snore_out),
                       ("yawn", yawn),
                       ("stretch", stretch)):
        write(name, make(np.random.default_rng(SEED)))

    print("  now import each one - see the docstring at the top of this file")


if __name__ == "__main__":
    main()
