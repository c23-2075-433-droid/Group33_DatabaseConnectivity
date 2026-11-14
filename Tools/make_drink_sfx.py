"""Synthesise the sound of Kylo drinking from his glass.

Made here rather than downloaded, for the reasons Tools/make_brush_sfx.py
and Tools/make_ui_click.py give: a dial beats whatever a stranger recorded,
and nothing downloaded means nothing to credit to anybody but us.

LENGTH IS NOT A FREE CHOICE. It is the length of the animation it plays
over, the same way brush_teeth.wav is 4.75 s because the brushing is:

    lead-in   1 frame  / 4 fps   = 0.25 s   the glass comes up
    loop      2 x 2    / 2.5 fps = 1.60 s   two tips of the glass
    tail      1 frame  / 4 fps   = 0.25 s   he lowers it
                                  = 2.10 s

Change one and change the other, or he drinks on in silence.

WHAT IT IS MADE OF

  pickup   the glass leaving the wood. A soft low knock, 120-700 Hz, very
           short - a tumbler lifted, not a mug banged down.
  sip      noise through a 900-3500 Hz bandpass whose centre rises across
           each sip, which is what makes it read as liquid drawn in rather
           than as air. Two of them, one per tip of the glass.
  swallow  a short resonant blip falling from about 190 to 115 Hz after
           each sip. This is the part that makes the sip sound swallowed
           instead of merely sucked; without it the effect is oddly empty.
  setdown  the glass returning to the table, brighter and shorter than the
           pickup, with a small ring on top so it reads as glass against
           wood rather than a knuckle.

It is kept gentle throughout. The brief that asked for these said cute and
child-friendly, and a loud gulp in a children's game is comedy, which is
not what this scene is doing.

Writes Assets/Resources/Sfx/drink_water.wav, where SoundEffects.Play finds
it.

    python3 Tools/make_drink_sfx.py
"""
import os
import wave

import numpy as np

RATE = 44100
OUT = os.path.join('Assets', 'Resources', 'Sfx')
NAME = 'drink_water'

LEAD = 0.25
LOOP = 1.60
TAIL = 0.25
TOTAL = LEAD + LOOP + TAIL

SIPS = 2                       # loopCycles in BuildBahayScene5
SIP_LOW, SIP_HIGH = 900.0, 3500.0


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


def knock(seed, low, high, decay, length_s=0.14, ring_hz=None):
    """Glass meeting a surface: a short filtered thud, optionally ringing."""
    rng = np.random.default_rng(seed)
    n = int(length_s * RATE)
    t = np.arange(n) / RATE
    body = bandpass(rng.normal(size=n), low, high) * np.exp(-t / decay)
    out = body / max(float(np.max(np.abs(body))), 1e-9)
    if ring_hz:
        out = out + 0.3 * np.sin(2 * np.pi * ring_hz * t) * np.exp(-t / 0.05)
    return out / max(float(np.max(np.abs(out))), 1e-9)


def sip(seed, length_s):
    """Liquid drawn in: a noise band whose centre climbs as the glass tips."""
    rng = np.random.default_rng(seed)
    n = int(length_s * RATE)
    frac = np.linspace(0.0, 1.0, n)
    # Two overlapping bands crossfaded, which is a cheap way to sweep a
    # brick-wall filter without filtering in slices.
    noise = rng.normal(size=n)
    low_band = bandpass(noise, SIP_LOW, SIP_LOW + 1100.0)
    high_band = bandpass(noise, SIP_LOW + 700.0, SIP_HIGH)
    body = low_band * (1.0 - frac) + high_band * frac
    env = np.minimum(np.clip(frac / 0.3, 0, 1),
                     np.clip((1.0 - frac) / 0.45, 0, 1)) ** 1.2
    out = body * env
    return out / max(float(np.max(np.abs(out))), 1e-9)


def swallow(length_s=0.13):
    """The small falling blip that makes a sip read as swallowed."""
    n = int(length_s * RATE)
    t = np.arange(n) / RATE
    f = np.linspace(190.0, 115.0, n)
    phase = 2 * np.pi * np.cumsum(f) / RATE
    tone = np.sin(phase) + 0.25 * np.sin(2 * phase)
    out = tone * np.exp(-t / 0.045)
    return out / max(float(np.max(np.abs(out))), 1e-9)


def main():
    y = np.zeros(int(TOTAL * RATE))

    # Lifting it off the table.
    add(y, 0.03, knock(1, 120.0, 700.0, 0.030), 0.30)

    # Two tips of the glass across the looping part.
    step = LOOP / SIPS
    for i in range(SIPS):
        at = LEAD + i * step
        add(y, at + 0.05, sip(10 + i, step * 0.55), 0.26)
        add(y, at + 0.05 + step * 0.55, swallow(), 0.22)

    # Setting it down again, on the tail frame.
    add(y, TOTAL - 0.22, knock(2, 700.0, 6000.0, 0.012, ring_hz=2600.0), 0.26)

    y = np.tanh(1.2 * y) / np.tanh(1.2)
    y *= 0.70 / max(float(np.max(np.abs(y))), 1e-9)

    os.makedirs(OUT, exist_ok=True)
    path = os.path.join(OUT, NAME + '.wav')
    s = np.clip(np.rint(y * 32767.0), -32768, 32767).astype('<i2')
    with wave.open(path, 'wb') as w:
        w.setnchannels(1)
        w.setsampwidth(2)
        w.setframerate(RATE)
        w.writeframes(s.tobytes())
    print('%s  %.2f s, %d sips, sip band %.0f-%.0f Hz'
          % (path, len(s) / float(RATE), SIPS, SIP_LOW, SIP_HIGH))


if __name__ == '__main__':
    main()
