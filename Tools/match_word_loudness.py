"""Sits the recorded Filipino word at the loudness of the line it was put into.

WHAT IS WRONG, MEASURED OVER ALL 176 SPLICED LINES. Comparing SPOKEN frames
only - silence excluded on both sides, or a sentence full of pauses scores
quieter than a single word for no reason - her word comes out:

    median  +3.1 dB louder than Rachel's speech in the same file
    louder in 151 of 176 lines (86%)
    worst   +9.3 dB  (rachel_l2_s5_bag_ask, 'Bag')

A word that arrives 3 dB up lurches forward mid-sentence; at 9 dB it is a
different recording shouting. This is most of what "the Filipino word doesn't
blend" actually is, and unlike the voice itself it is arithmetic.

Tools/splice_word_lines.py does try to match - its own note says a word
normalised on its own sits louder than the same word inside a sentence - but
it matched against the whole line, pauses included, and a line is mostly not
speech. Measuring speech against speech is what this fixes.

NOT TO ZERO. The target is +1 dB, not 0. The Filipino word is the thing the
child is meant to catch and repeat; levelled flat into the sentence it stops
being the word being taught. A dB forward reads as emphasis. Three reads as a
join.

Nothing else is touched: same voice, same recording, same timing, one gain on
the region the log says her word occupies, crossfaded at the edges so there is
no step. No API key, no credits, nothing regenerated.

Nothing is overwritten without --apply; by default the result lands in
Recordings/vo_levelled/ to be heard against the original first.

    python3 Tools/match_word_loudness.py --limit 5
    python3 Tools/match_word_loudness.py
    python3 Tools/match_word_loudness.py --apply

Plain python3, not `python3 -I`: that flag drops the user site directory,
where numpy lives.
"""
import argparse
import os
import re
import shutil
import sys
import wave

import numpy as np

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
VO = os.path.join(ROOT, "Assets", "Audio", "VO")
LOG = os.path.join(ROOT, "Recordings", "splice_run.log")
OUT = os.path.join(ROOT, "Recordings", "vo_levelled")
BEFORE = os.path.join(ROOT, "Recordings", "vo_before")

EDGE = 0.015        # seconds of crossfade back onto the original
FRAME = 0.02        # seconds per measurement frame


def main():
    ap = argparse.ArgumentParser(description=__doc__,
                                 formatter_class=argparse.RawDescriptionHelpFormatter)
    ap.add_argument("--target", type=float, default=1.0,
                    help="dB the word should sit ABOVE the line's speech (default 1)")
    ap.add_argument("--max-cut", type=float, default=9.0,
                    help="never pull a word down by more than this")
    ap.add_argument("--max-lift", type=float, default=3.0,
                    help="never push a quiet word up by more than this")
    ap.add_argument("--apply", action="store_true", help="replace the game's clips")
    ap.add_argument("--limit", type=int, default=0)
    args = ap.parse_args()

    os.makedirs(OUT, exist_ok=True)
    spots = positions()
    if args.limit:
        spots = spots[:args.limit]
    print("%d line(s) with a logged word position\n" % len(spots))

    before, after, touched = [], [], 0
    for name, word, at in spots:
        line = os.path.join(VO, name + ".wav")
        clip = os.path.join(VO, "word_%s.wav" % word.lower())
        if not (os.path.exists(line) and os.path.exists(clip)):
            print("  %-34s missing a file, skipped" % name)
            continue

        rate, x = read(line)
        _, w = read(clip)
        a = int(at * rate)
        b = min(len(x), a + len(w))
        if b - a < int(0.1 * rate):
            print("  %-34s word region too short, skipped" % name)
            continue

        hers = speech_db(x[a:b], rate)
        rachel = speech_db(np.concatenate([x[:a], x[b:]]), rate)
        if hers is None or rachel is None:
            print("  %-34s not enough speech to measure, skipped" % name)
            continue

        offset = hers - rachel
        gain_db = np.clip((rachel + args.target) - hers, -args.max_cut, args.max_lift)

        y = x.copy()
        scaled = x[a:b] * (10.0 ** (gain_db / 20.0))
        y[a:b] = blend(x[a:b], scaled, int(EDGE * rate))

        peak = float(np.abs(y).max())
        if peak > 0.99:                     # a lift must never clip the line
            y *= 0.99 / peak

        now = speech_db(y[a:b], rate) - speech_db(
            np.concatenate([y[:a], y[b:]]), rate)
        before.append(offset)
        after.append(now)
        touched += 1
        print("  %-34s '%-9s %+5.1f dB -> %+4.1f dB  (gain %+5.1f)"
              % (name, word + "'", offset, now, gain_db))

        write(os.path.join(OUT, name + ".wav"), rate, y)
        if args.apply:
            os.makedirs(BEFORE, exist_ok=True)
            kept = os.path.join(BEFORE, name + ".wav")
            if not os.path.exists(kept):
                shutil.copy2(line, kept)
            write(line, rate, y)

    if touched:
        print("\n%d line(s): median offset %+.1f dB -> %+.1f dB   (target %+.1f)"
              % (touched, np.median(before), np.median(after), args.target))
    print("Written to %s" % os.path.relpath(OUT, ROOT))
    if not args.apply:
        print("Listen against the originals, then re-run with --apply.")
    elif touched:
        print("Originals kept in %s. Then in Unity: Assets > Refresh."
              % os.path.relpath(BEFORE, ROOT))
    return 0


def speech_db(x, rate):
    """Loudness of the spoken frames only, so pauses do not count as quiet."""
    n = int(FRAME * rate)
    if len(x) < n * 4:
        return None
    frames = x[:len(x) // n * n].reshape(-1, n)
    energy = np.sqrt((frames ** 2).mean(axis=1))
    loud = energy[energy > max(np.percentile(energy, 70), energy.max() * 0.15)]
    if not len(loud):
        return None
    return 20 * np.log10(max(float(np.sqrt((loud ** 2).mean())), 1e-9))


def blend(old, new, fade):
    """Ease the gain in and out, so the change is not a step at the join."""
    out = new.copy()
    fade = max(1, min(fade, len(out) // 3))
    ramp = np.linspace(0.0, 1.0, fade)
    out[:fade] = old[:fade] * (1 - ramp) + new[:fade] * ramp
    out[-fade:] = old[-fade:] * ramp[::-1] + new[-fade:] * (1 - ramp[::-1])
    return out


def positions():
    """(line, word, seconds) for every splice, from the run log."""
    if not os.path.exists(LOG):
        sys.exit("No %s - that log is what says where each word landed." % LOG)
    found = []
    for line in open(LOG, encoding="utf-8"):
        m = re.match(r"\s+(\S+)\s+([\d.]+)s\s+\((\S+) at ([\d.]+)s\)", line)
        if m:
            found.append((m.group(1), m.group(3), float(m.group(4))))
    return found


def read(path):
    with wave.open(path, "rb") as w:
        rate, n, ch = w.getframerate(), w.getnframes(), w.getnchannels()
        x = np.frombuffer(w.readframes(n), dtype="<i2").astype(np.float64) / 32768.0
    return rate, x.reshape(-1, ch).mean(axis=1) if ch > 1 else x


def write(path, rate, x):
    y = np.clip(np.rint(x * 32767.0), -32768, 32767).astype("<i2")
    with wave.open(path, "wb") as w:
        w.setnchannels(1)
        w.setsampwidth(2)
        w.setframerate(rate)
        w.writeframes(y.tobytes())


if __name__ == "__main__":
    sys.exit(main())
