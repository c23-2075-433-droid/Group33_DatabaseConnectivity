"""Synthesise the UI click - the small sound a button makes when it is pressed.

Every other effect in the game was downloaded from OpenGameArt and credited in
Assets/Resources/Sfx/CREDITS.txt. This one is made here instead, for a reason
worth writing down: a button click is the sound the child hears most in this
game - the menu, the level map, every Continue on every results panel - and a
library click is either a hard mouse-switch "clack" from an office UI pack or a
cartoon "boing". Neither suits a six-year-old pressing LARO. Synthesising it
means the pitch, the length and the softness are all dials rather than whatever
a stranger recorded, and the licence question disappears: nothing was
downloaded, so nothing has to be credited to anybody but us.

What it makes is a short upward blip - a soft "tup". Upward because the button
it answers for mostly moves the child FORWARD (play, next, continue), and an
upward sweep reads as going somewhere where a downward one reads as a cancel.
Three parts, which is as simple as a pleasant click gets:

  1. a sine that sweeps 760 -> 1180 Hz in the first 40 ms, the body;
  2. its octave underneath at a third of the level, so it has a little wood
     in it rather than sounding like a test tone;
  3. 3 ms of noise at the very front, the fingertip - without it the sound
     starts too politely to register as a press.

Run from the repository root, then bring the result in the way every other
effect arrives, so it gets trimmed, levelled and written into CREDITS.txt:

    python3 Tools/make_ui_click.py
    python3 Tools/import_sfx.py ui_click Recordings/sfx/ui_click_raw.wav \
        --licence CC0 --title "UI click (synthesised)" \
        --author "SALINLAHI (Tools/make_ui_click.py)"

Plain python3, not `python3 -I`: that flag drops the user site directory,
where numpy lives.
"""
import os
import wave

import numpy as np

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
STAGE = os.path.join(ROOT, "Recordings", "sfx")
DST = os.path.join(STAGE, "ui_click_raw.wav")

RATE = 44100
LENGTH = 0.11          # seconds of file; the sound itself is gone by ~70 ms
START_HZ = 760.0
END_HZ = 1180.0
SWEEP = 0.040          # seconds to reach END_HZ, then it holds
DECAY = 0.022          # seconds for the body to fall to 1/e
ATTACK = 0.0015        # seconds of ramp in, so the waveform starts at zero


def main():
    os.makedirs(STAGE, exist_ok=True)

    t = np.arange(int(LENGTH * RATE), dtype=np.float64) / RATE

    # Phase by integration rather than sin(2*pi*f(t)*t): the second form
    # sweeps at twice the rate it looks like it does and lands an octave out.
    f = START_HZ + (END_HZ - START_HZ) * np.clip(t / SWEEP, 0.0, 1.0)
    phase = 2.0 * np.pi * np.cumsum(f) / RATE

    body = np.sin(phase) * np.exp(-t / DECAY)
    # The octave decays faster, so it colours the attack and not the tail.
    body += 0.33 * np.sin(2.0 * phase) * np.exp(-t / (DECAY * 0.45))

    # The fingertip. Seeded, so re-running the script produces the same file
    # and a re-import is not a silent change to a committed asset.
    noise = np.random.default_rng(33).standard_normal(len(t))
    noise = np.diff(noise, prepend=0.0)          # a crude high-pass; keeps it a tick, not a thud
    body += 0.10 * noise * np.exp(-t / 0.0025)

    body *= np.clip(t / ATTACK, 0.0, 1.0)        # no click from the click starting mid-cycle
    body = np.tanh(1.3 * body) / np.tanh(1.3)    # rounds the peak instead of letting it clip square

    fade = int(0.006 * RATE)                     # and none from it stopping mid-cycle either
    body[-fade:] *= np.linspace(1.0, 0.0, fade)

    body *= 0.9 / max(float(np.max(np.abs(body))), 1e-9)

    y = np.clip(np.rint(body * 32767.0), -32768, 32767).astype("<i2")
    with wave.open(DST, "wb") as w:
        w.setnchannels(1)
        w.setsampwidth(2)
        w.setframerate(RATE)
        w.writeframes(y.tobytes())

    print("  -> %s" % os.path.relpath(DST, ROOT))
    print("  %.3f s, mono %d Hz" % (len(y) / float(RATE), RATE))
    print("  now: python3 Tools/import_sfx.py ui_click %s --licence CC0 ..."
          % os.path.relpath(DST, ROOT))


if __name__ == "__main__":
    main()
