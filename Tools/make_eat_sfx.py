"""Cut Kylo's eating sound out of a recording of someone eating popcorn.

WHY THIS ONE IS NOT SYNTHESISED, when the brushing and the bell are.

Tools/make_brush_sfx.py argues for synthesis: a dial beats whatever a
stranger recorded, and nothing downloaded means nothing to credit to
anybody but us. That argument held for three attempts at this sound and
none of them worked. Filtered noise was wrong, because chewing is not
noise. Resonant smacks were right about the physics and came out dry.
A voiced "nom nom nom" read as eating but put 54% of its energy in the
200-2000 Hz speech band, which is the one thing make_brush_sfx exists to
avoid: the microphone is open and the recogniser hears the speaker.

A real mouth settles all three. Kyla chose the recording:

    Eating Sound Effects (Chomp, Bite, Chewing), by BorzSounds
    https://freesound.org/people/BorzSounds/sounds/843304/
    Creative Commons 0 - public domain, commercial use, no attribution
    required. Credited anyway, because it is somebody's work.

The source is kept in Art/sfx_source/ rather than under Assets/, so Unity
never imports the whole fourteen seconds into the game.

WHICH 2.9 SECONDS, AND WHY THOSE. The recording mixes loud single chomps
with quieter continuous chewing. The chomps peak at 1.0 and read as a
cartoon bite; this scene wants a child working through a plate of rice.
The passage from 10.55 s is the gentle one - eight distinct chews in 2.9
seconds, peaking at 0.11 - so that is the window.

It is also the safest part of the file for this game. Its spectral centroid
is 3.6 kHz with 43% of its energy above 2 kHz, which puts most of it clear
of the band a child's voice lives in. The synthesised "nom" could not do
that. Popcorn can.

WHAT IS ADDED. Only the spoon. The recording is mouth and nothing else, and
the scene has a spoon and a plate in shot, so a clink goes in as he lifts
it and at each mouthful - well under the chewing, which is the sound.

LENGTH IS NOT A FREE CHOICE. 2.90 s is the length of the animation it plays
over, the way brush_teeth.wav is 4.75 s because the brushing is:

    lead-in   1 frame  / 4 fps   = 0.25 s   the spoon comes up
    loop      3 x 2    / 2.5 fps = 2.40 s   three mouthfuls
    tail      1 frame  / 4 fps   = 0.25 s   the last swallow
                                  = 2.90 s

Change one and change the other, or he chews on in silence.

Needs afconvert, which ships with macOS, to decode the mp3 the first time.

Writes Assets/Resources/Sfx/eat_rice.wav, where SoundEffects.Play finds it.

    python3 Tools/make_eat_sfx.py
"""
import os
import subprocess
import wave

import numpy as np

RATE = 44100
OUT = os.path.join('Assets', 'Resources', 'Sfx')
NAME = 'eat_rice'

SOURCE_MP3 = os.path.join('Art', 'sfx_source',
                          'freesound_843304_eating_borzsounds.mp3')
SOURCE_WAV = os.path.join('Art', 'sfx_source',
                          'freesound_843304_eating_borzsounds.wav')

# The gentle chewing passage, measured off the recording.
WINDOW_START = 10.55
TOTAL = 2.90
LEAD, LOOP = 0.25, 2.40
BITES = 3

CHEW_GAIN = 0.80       # the chewing is the sound
CLINK_GAIN = 0.16      # the spoon is seasoning


def decode():
    """mp3 -> mono 44.1 kHz wav, once, with macOS's own converter."""
    if not os.path.exists(SOURCE_WAV):
        subprocess.run(['afconvert', '-f', 'WAVE', '-d', 'LEI16@%d' % RATE,
                        '-c', '1', SOURCE_MP3, SOURCE_WAV], check=True)
    with wave.open(SOURCE_WAV) as w:
        return (np.frombuffer(w.readframes(w.getnframes()), dtype='<i2')
                .astype(float) / 32768.0)


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
    body = bandpass(rng.normal(size=n), 2500.0, 7000.0) * np.exp(-t / 0.018)
    ring = (np.sin(2 * np.pi * 3150 * t) * 0.35
            + np.sin(2 * np.pi * 4720 * t) * 0.2) * np.exp(-t / 0.055)
    out = body / max(float(np.max(np.abs(body))), 1e-9) + ring
    return out / max(float(np.max(np.abs(out))), 1e-9)


def main():
    src = decode()
    start = int(WINDOW_START * RATE)
    chew = np.array(src[start:start + int(TOTAL * RATE)])
    if len(chew) < int(TOTAL * RATE):
        raise SystemExit('source too short for a %.2f s window' % TOTAL)

    # The passage is quiet by design; bring it up to where it can be heard.
    chew = chew / max(float(np.max(np.abs(chew))), 1e-9)

    # A few milliseconds of fade at each end so the cut is inaudible.
    edge = int(0.012 * RATE)
    chew[:edge] *= np.linspace(0.0, 1.0, edge)
    chew[-edge:] *= np.linspace(1.0, 0.0, edge)

    y = np.zeros(int(TOTAL * RATE))
    add(y, 0.0, chew, CHEW_GAIN)

    # The spoon: once off the plate, once per mouthful.
    add(y, 0.02, clink(1), CLINK_GAIN * 1.3)
    step = LOOP / BITES
    for i in range(BITES):
        add(y, LEAD + i * step, clink(10 + i, 0.13), CLINK_GAIN)

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
    print('%s  %.2f s, recorded chewing from %.2fs + %d spoon clinks'
          % (path, len(s) / float(RATE), WINDOW_START, BITES + 1))


if __name__ == '__main__':
    main()
