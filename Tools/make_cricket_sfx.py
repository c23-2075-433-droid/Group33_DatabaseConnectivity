"""Synthesise the crickets that answer ten seconds of silence.

Made here rather than downloaded, for the reason Tools/make_ui_click.py gives -
a dial beats whatever a stranger recorded, and nothing downloaded means nothing
to credit to anybody but us - and for one that is specific to this effect.

This sound plays WHILE the microphone is open. VoicePromptDirector fires it at
the ten-second idle nudge, and VoiceCommand is listening continuously by then:
whatever comes out of the phone's speaker is heard by the recognizer and could
be scored as the child's own attempt. Two properties keep that from happening,
and both of them are specifications rather than search terms:

  SHORT      0.75 s, against a restartDelay of 0.5 s. GameplayHUD's note on
             SALINLAHI_Incorrect works the same sum out for its 0.58 s cue.
             Only a decaying tail sounds into a reopened microphone, and a
             tail is nothing for a recognizer to match a word against.
  HIGH       the carrier sits at 4.6 kHz. A six-year-old's vowels live below
             about 3 kHz, so this is clear of the band the recognizer is
             actually weighing - unlike a sampled "night ambience", which is
             mostly broadband hiss sitting right on top of it.

A cricket is not a tone. It is a PULSE TRAIN: the insect files one wing over
the other and each stroke is a click, four or five to a chirp, and it is the
pulse rate that the ear hears as a cricket rather than as a whistle. Built
here as a 4.6 kHz carrier gated by a 34 Hz pulse envelope, four pulses to a
chirp, three chirps spaced about 0.28 s apart.

Two insects, not one. "Crickets" is plural, and one cricket on its own reads
as a smoke alarm two rooms away. The second is detuned a little, quieter, and
offset off the first's rhythm so the two never land together.

Nothing else is in here. No night ambience bed, no wind: at this length a bed
would be audible as a burst of hiss switching on and off, and it is exactly
the part that would land in the recognizer's band.

Everything is seeded, so a re-run produces the same file and re-importing is
not a silent change to a committed asset.

Run from the repository root, then bring it in the way every other effect
arrives, so it is trimmed, levelled and written into CREDITS.txt:

    python3 Tools/make_cricket_sfx.py
    python3 Tools/import_sfx.py crickets Recordings/sfx/crickets_raw.wav \
        --licence CC0 --title "Awkward silence crickets (synthesised)" \
        --author "SALINLAHI (Tools/make_cricket_sfx.py)"

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

LENGTH = 0.75


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


def lowpass(x, hz):
    """A fixed one-pole lowpass, as a convolution with its own impulse response."""
    tau = RATE / (2.0 * np.pi * hz)
    n = int(min(len(x), max(8.0, tau * 6.0)))
    k = np.exp(-np.arange(n, dtype=np.float64) / tau)
    k /= k.sum()
    return np.convolve(x, k)[:len(x)]


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


def pulse_gate(t, rate, duty):
    """The wing strokes: a smooth 0..1 gate at `rate` per second.

    Raised cosine rather than a square gate. A square one clicks at both
    edges, and a click at 4.6 kHz is a tick the recognizer's front end sees
    as an onset - which is the one thing this sound must not look like.
    """
    ph = (t * rate) % 1.0
    g = np.where(ph < duty, 0.5 - 0.5 * np.cos(2.0 * np.pi * ph / duty), 0.0)
    return g


def chirp(t, start, carrier, pulses=4, rate=34.0, duty=0.55):
    """One chirp: `pulses` wing strokes of a gated carrier, beginning at `start`.

    The carrier drifts up about 1.5% across the chirp. A cricket's stroke is
    not quite even and a dead-steady carrier reads as electronic - this is the
    same trick the snore's flutter plays, an octave and a half higher up.
    """
    span = pulses / rate
    local = t - start
    live = (local >= 0.0) & (local < span)
    k = np.clip(local / span, 0.0, 1.0)

    f = carrier * (1.0 + 0.015 * k)
    body = np.sin(phase_of(f))

    # A touch of the second harmonic, so the timbre has an edge on it. A pure
    # sine at this frequency is a hearing test.
    body += 0.14 * np.sin(2.0 * phase_of(f))

    gate = pulse_gate(local, rate, duty)

    # The chirp swells and falls across its four strokes rather than starting
    # at full strength: the middle strokes of a real chirp are the loudest.
    arc = np.sin(np.pi * k) ** 0.6

    return body * gate * arc * live


def crickets(rng):
    t = seconds(LENGTH)

    y = np.zeros_like(t)

    # The near cricket. Three chirps, 0.28 s apart, which is about the spacing
    # of a field cricket on a cool night - fast enough to read as "crickets"
    # inside three quarters of a second.
    for i, start in enumerate((0.015, 0.295, 0.575)):
        y += (1.0 - 0.12 * i) * chirp(t, start, 4600.0)

    # The far one. Quieter, detuned down a little so the two are audibly two
    # insects, and deliberately off the first's beat so they never land
    # together. Its own low-passing is what puts it further away.
    far = np.zeros_like(t)
    for start in (0.155, 0.435):
        far += chirp(t, start, 4180.0, pulses=3, rate=31.0)
    far = lowpass(far, 5200.0)
    y += 0.42 * far

    # The faintest breath of air under it, band-passed well above a child's
    # vowels and gone by the end. Enough to stop the chirps sounding as though
    # they are happening in a vacuum; not enough to be a hiss of its own.
    air = rng.standard_normal(len(t))
    air = lowpass(air, 7000.0) - lowpass(air, 3400.0)
    air *= np.exp(-t / 0.42)
    y += 0.05 * air

    # Lets go rather than stops, so the last chirp is already fading by the
    # time the microphone reopens.
    env = np.minimum(smoothstep(t / 0.010), smoothstep((t[-1] - t) / 0.090))

    return finish(y * env, attack=0.004, release=0.030)


def write(name, y):
    path = os.path.join(STAGE, name + "_raw.wav")
    s = np.clip(np.rint(y * 32767.0), -32768, 32767).astype("<i2")
    with wave.open(path, "wb") as w:
        w.setnchannels(1)
        w.setsampwidth(2)
        w.setframerate(RATE)
        w.writeframes(s.tobytes())
    print("  -> %-28s %.3f s" % (os.path.relpath(path, ROOT), len(s) / float(RATE)))


def main():
    os.makedirs(STAGE, exist_ok=True)
    write("crickets", crickets(np.random.default_rng(SEED)))
    print("  now import it - see the docstring at the top of this file")


if __name__ == "__main__":
    main()
