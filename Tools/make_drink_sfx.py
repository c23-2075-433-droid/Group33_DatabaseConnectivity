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

WHAT IT IS MADE OF, and what it is NOT made of.

The first version made the drinking out of a swept noise band - liquid
drawn in through the lips. It was too subtle to read as anything, and what
this scene wants is a GULP: the sound of swallowing, not of sipping.

A gulp is not noise at all. It is a small resonant cavity collapsing - the
throat closing round a mouthful - and the ear hears that as a short tone
whose pitch drops fast. Three of them in a row is the whole effect, and is
why a cartoon writes it "glug glug glug".

  pickup   the glass leaving the wood. A soft low knock, 120-700 Hz, very
           short - a tumbler lifted, not a mug banged down.
  glug     a sine whose pitch falls from about 520 to 150 Hz inside a
           tenth of a second, with a second partial and a touch of
           throat resonance around 700 Hz. Three of them, each a little
           lower and a little slower than the last, the way a real
           swallow tires. A two-millisecond click opens each one so it
           starts crisply instead of fading in.
  setdown  the glass returning to the table, brighter and shorter than
           the pickup, with a small ring so it reads as glass on wood
           rather than a knuckle.

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

GULPS = 3                      # three swallows across the loop


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


def glug(seed, top_hz=520.0, bottom_hz=150.0, length_s=0.13):
    """One swallow: a short tone falling fast, which is what a gulp is."""
    rng = np.random.default_rng(seed)
    n = int(length_s * RATE)
    t = np.arange(n) / RATE
    # Exponential fall, not linear - a throat closes quickly then settles.
    f = bottom_hz + (top_hz - bottom_hz) * np.exp(-t / (length_s * 0.28))
    phase = 2 * np.pi * np.cumsum(f) / RATE
    tone = np.sin(phase) + 0.3 * np.sin(2 * phase)
    # A little throat around it, and a click to open it crisply.
    throat = np.sin(2 * np.pi * 700.0 * t) * np.exp(-t / 0.012) * 0.25
    click = np.zeros(n)
    k = int(0.002 * RATE)
    click[:k] = rng.normal(size=k)
    out = tone * np.exp(-t / (length_s * 0.42)) + throat + 0.2 * bandpass(
        click, 200.0, 3000.0)
    return out / max(float(np.max(np.abs(out))), 1e-9)


def main():
    y = np.zeros(int(TOTAL * RATE))

    # Lifting it off the table.
    add(y, 0.03, knock(1, 120.0, 700.0, 0.030), 0.30)

    # Three swallows across the looping part, each lower and slower than
    # the last - a real gulp sequence runs down, it does not repeat.
    step = LOOP / GULPS
    for i in range(GULPS):
        at = LEAD + 0.10 + i * step
        add(y, at,
            glug(10 + i,
                 top_hz=520.0 - 55.0 * i,
                 bottom_hz=150.0 - 12.0 * i,
                 length_s=0.13 + 0.015 * i),
            0.42 - 0.04 * i)

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
    print('%s  %.2f s, %d gulps'
          % (path, len(s) / float(RATE), GULPS))


if __name__ == '__main__':
    main()
