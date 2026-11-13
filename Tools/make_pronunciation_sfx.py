"""Synthesise the two answers to a spoken word: SALINLAHI_Correct, SALINLAHI_Incorrect.

These are the two sounds the game makes the instant it has decided whether the
child said the Filipino word right. They are the most repeated sounds in
SALINLAHI after the button click - one of them fires on every single attempt at
every word in every level - so they are made here rather than downloaded, for
the reason Tools/make_ui_click.py gives (a dial beats whatever a stranger
recorded, and nothing downloaded means nothing to credit to anybody but us) and
one more that belongs to this pair in particular: the WRONG sound is the one a
six-year-old hears when they have just failed at their own language. A library
"wrong answer" effect is a buzzer, a descending slide-whistle or a sad trombone,
and every one of those is a small punishment. This one has to say "oops, again"
and nothing else. That is a specification, not a search term.

Two files:

  SALINLAHI_Correct    an ascending C-major arpeggio on a soft bell - C5, E5,
                       G5 struck 110 ms apart so the notes overlap into the
                       chord rather than arriving as three separate dings, then
                       four quiet sparkles up in the C6-C7 octave as the tail.
                       Bell-like because the partials are stacked at 1, 2, 3
                       and 4.2 times the note with the upper ones decaying
                       faster, which is roughly what a glockenspiel bar does;
                       ascending because up is the direction of getting it
                       right. The sparkles are the "magical" half of the brief
                       and they are struck at under a third of the arpeggio's
                       level, which measures about 7 dB below the chord in the
                       sparkles' own octave: present, not competing. A sparkle
                       you notice every time becomes a sparkle you hate by the
                       fiftieth word, and a child hears this one a hundred
                       times in a sitting.

  SALINLAHI_Incorrect  two soft toy-horn honks falling G4 -> E4. A descending
                       MINOR THIRD, which is the interval of a called "uh-oh"
                       in every language that has one, and the smallest fall
                       that still reads as a fall: drop further and it turns
                       sad, stay level and it reads as a confirmation. Each
                       honk is a small harmonic stack - the note plus its 2nd,
                       3rd and 4th at 0.30, 0.16 and 0.07 - under a slow 6 Hz
                       vibrato of 2.5%, with a 25 ms swell in and an 80 ms
                       release. The vibrato and the HELD middle are the whole
                       character: a struck note is an instrument, a wobbling
                       held one is a toy, and a toy cannot tell a child off.
                       Two honks 300 ms apart, which is an unhurried walking
                       pace: faster, and the pair reads as an error code. No
                       noise, no buzz, no third honk - two is a shrug, three
                       is a verdict.

                       A sustained sound like this one peaks lower than a
                       struck one at the same loudness (-9 dBFS against the
                       chime's -5), because its waveform spends its whole
                       length near full swing instead of spiking once. Both
                       files carry the same -14 dBFS RMS every other effect in
                       the game is levelled to, so they sound equally loud
                       next to each other; the peak figure is not the volume.

Both end cleanly at zero, so retriggering one before the last has finished -
which happens the moment a child answers fast - cannot click. Neither has to
dodge the frequencies a child's voice lives in, the way the snores in
Tools/make_wakeup_sfx.py do: these play AFTER the microphone has given its
verdict, not under it.

Everything is seeded, so a re-run produces the same files and re-importing is
not a silent change to a committed asset.

Run from the repository root, then bring each one in the way every other effect
arrives, so it is trimmed, levelled and written into CREDITS.txt:

    python3 Tools/make_pronunciation_sfx.py
    for n in SALINLAHI_Correct SALINLAHI_Incorrect; do
        python3 Tools/import_sfx.py $n Recordings/sfx/${n}_raw.wav \
            --licence CC0 --title "Pronunciation feedback ($n, synthesised)" \
            --author "SALINLAHI (Tools/make_pronunciation_sfx.py)"
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

# Equal temperament, A4 = 440. Named rather than numeric so the music above is
# checkable against the code.
C5, E5, G5 = 523.25, 659.25, 783.99
C6, E6, G6, C7 = 1046.50, 1318.51, 1567.98, 2093.00
G4, E4 = 392.00, 329.63


def seconds(n):
    """The time axis for a clip of n seconds."""
    return np.arange(int(n * RATE), dtype=np.float64) / RATE


def add(buf, start, piece):
    """Mix piece into buf at start seconds, clipped to the end of buf."""
    i = int(start * RATE)
    n = min(len(piece), len(buf) - i)
    if n > 0:
        buf[i:i + n] += piece[:n]


def tail(x, seconds=0.015):
    """Fade the last few ms of a piece to zero.

    An exponential decay is never actually finished when the array it lives in
    runs out, and a decay cut off at even 5% of its peak is a click. Everything
    here ends through this.
    """
    n = int(seconds * RATE)
    x[-n:] *= np.linspace(1.0, 0.0, n)
    return x


def bell(freq, length, decay, level=1.0, mallet=0.0):
    """One struck bar: partials at 1, 2, 3 and 4.2x, the top ones dying first.

    The 4.2 is the only inharmonic one and it is quiet - enough to stop the
    note sounding like a test tone, not enough to clang.
    """
    t = seconds(length)
    out = np.zeros_like(t)
    for ratio, amp, haste in ((1.0, 1.00, 1.0), (2.0, 0.42, 1.8),
                              (3.0, 0.16, 2.6), (4.2, 0.07, 3.4)):
        out += amp * np.sin(2.0 * np.pi * freq * ratio * t) * np.exp(-t * haste / decay)

    if mallet:
        # The tap of the mallet on the bar: 2 ms of noise, differenced so it is
        # a tick in the treble rather than a thud under the note.
        rng = np.random.default_rng(SEED + int(freq))
        tap = np.diff(rng.standard_normal(len(t)), prepend=0.0)
        out += mallet * tap * np.exp(-t / 0.0015)

    out *= np.clip(t / 0.003, 0.0, 1.0)      # no edge from starting mid-cycle
    return tail(out) * level


def honk(hz, length=0.26, level=1.0):
    """One soft toy-horn honk: harmonics, a slow vibrato, a held middle.

    Phase by integration, not sin(2*pi*f(t)*t) - the second form would apply
    the vibrato at twice the depth it looks like it does.
    """
    t = seconds(length)
    vib = 1.0 + 0.025 * np.sin(2.0 * np.pi * 6.0 * t)
    phase = 2.0 * np.pi * np.cumsum(hz * vib) / RATE

    out = np.zeros_like(t)
    for harmonic, amp in ((1, 1.00), (2, 0.30), (3, 0.16), (4, 0.07)):
        out += amp * np.sin(harmonic * phase)

    # Swell in, hold, then let go. The release is curved rather than linear so
    # the note sags away instead of being switched off.
    release = int(0.080 * RATE)
    env = np.clip(t / 0.025, 0.0, 1.0)
    env[-release:] *= np.linspace(1.0, 0.0, release) ** 1.5
    return tail(out * env, 0.020) * level


def correct():
    """Ascending C5-E5-G5 on soft bells, with four sparkles as the tail."""
    buf = np.zeros(int(1.35 * RATE))

    for i, (note, when) in enumerate(((C5, 0.00), (E5, 0.11), (G5, 0.22))):
        # The last note rings longest; it is the one the chord settles on.
        add(buf, when, bell(note, 1.2, 0.34 + 0.09 * i, 0.72, mallet=0.02))

    # The sparkle. Up an octave and above, short, and quiet enough to be a
    # shimmer on the end of the chime rather than a fourth and fifth note.
    for pitch, when, level in ((C6, 0.40, 0.22), (G6, 0.47, 0.18),
                               (E6, 0.55, 0.16), (C7, 0.63, 0.12)):
        add(buf, when, bell(pitch, 0.5, 0.12, level))

    return buf


def incorrect():
    """Two honks, G4 then E4 - a called "uh-oh", not a buzzer."""
    buf = np.zeros(int(0.70 * RATE))
    add(buf, 0.00, honk(G4))
    # Keyword, not positional: honk's second parameter is the length.
    add(buf, 0.30, honk(E4, level=0.85))   # the second a little softer: a shrug
    return buf


def finish(x, fade_seconds):
    """Round the peaks, fade the tail to zero and normalise to 0.9.

    The fade is long for the chime and short for the boops. A bell's ring is
    still at a tenth of its peak when the file has to end, and cutting that in
    10 ms is a chop you can hear; 180 ms of fade reads as the note dying.
    """
    x = np.tanh(1.2 * x) / np.tanh(1.2)      # rounds the peak instead of clipping it square
    fade = int(fade_seconds * RATE)
    x[-fade:] *= np.linspace(1.0, 0.0, fade)
    return x * (0.9 / max(float(np.max(np.abs(x))), 1e-9))


def write(name, x, fade_seconds):
    path = os.path.join(STAGE, name + "_raw.wav")
    y = np.clip(np.rint(finish(x, fade_seconds) * 32768.0), -32768, 32767).astype("<i2")
    with wave.open(path, "wb") as w:
        w.setnchannels(1)
        w.setsampwidth(2)
        w.setframerate(RATE)
        w.writeframes(y.tobytes())
    print("  %s  %.3f s" % (os.path.relpath(path, ROOT), len(y) / float(RATE)))


def main():
    os.makedirs(STAGE, exist_ok=True)
    write("SALINLAHI_Correct", correct(), 0.18)
    write("SALINLAHI_Incorrect", incorrect(), 0.03)
    print("Now import each one; see the top of this file for the two commands.")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
