"""Split one recording of the Filipino word list into the game's word clips.

Why these are recorded rather than generated. Every other line in this game
is Rachel narrating in English and a synthetic voice does that well. The
Filipino words are different: they are the only thing a child is asked to
COPY, and Tagalog distinguishes words by stress - tayô (stand up) against
táyo (we). A text-to-speech model given "Tayo" has no way to know which one
is meant, and gets it wrong in a way that is still intelligible, which is
exactly what makes it easy to ship by mistake. One speaker, five minutes,
and the problem is gone for good.

Give it one file with the words said in the order of
Recordings/filipino_words.txt, separated by roughly a second of silence. It
finds the gaps, takes the LAST take of each word - so a fluffed word can just
be said again - trims, levels and writes word_<word>.wav into Assets/Audio/VO.

    python3 Tools/split_word_recording.py Recordings/filipino_words.m4a
    python3 Tools/split_word_recording.py Recordings/first_three.m4a --only bangon,tayo,lakad
    python3 Tools/split_word_recording.py ... --dry-run     # just report what it found

Plain python3, not `python3 -I`: that flag drops the user site directory,
where numpy lives.
"""
import argparse
import csv
import os
import subprocess
import sys
import wave

import numpy as np

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
CSV = os.path.join(ROOT, "Docs", "VoiceOverScript.csv")
OUT = os.path.join(ROOT, "Assets", "Audio", "VO")
STAGE = os.path.join(ROOT, "Recordings", "split")

TARGET_DBFS = -20.0
FLOOR = 0.012          # below this counts as silence
MIN_GAP = 0.45         # a gap this long separates two words
MIN_WORD = 0.18        # anything shorter is a click, not a word


def main():
    ap = argparse.ArgumentParser(description=__doc__,
                                 formatter_class=argparse.RawDescriptionHelpFormatter)
    ap.add_argument("recording")
    ap.add_argument("--only", help="comma-separated words, if the file holds only some")
    ap.add_argument("--dry-run", action="store_true", help="report the split, write nothing")
    args = ap.parse_args()

    words = [r["Script / Word"].strip() for r in csv.DictReader(open(CSV, encoding="utf-8"))
             if r["Category"].strip() == "Word"]
    if args.only:
        want = [w.strip().lower() for w in args.only.split(",")]
        words = [w for w in words if w.lower() in want]
        words.sort(key=lambda w: want.index(w.lower()))

    if not os.path.exists(args.recording):
        sys.exit("No such file: " + args.recording)

    os.makedirs(STAGE, exist_ok=True)
    wav = os.path.join(STAGE, "_source.wav")
    subprocess.run(["/usr/bin/afconvert", "-f", "WAVE", "-d", "LEI16@44100", "-c", "1",
                    args.recording, wav], check=True, capture_output=True)

    with wave.open(wav, "rb") as w:
        rate, n = w.getframerate(), w.getnframes()
        x = np.frombuffer(w.readframes(n), dtype="<i2").astype(np.float32) / 32768.0

    takes = find(x, rate)
    print("%d take(s) in %.1f s of recording; %d word(s) expected"
          % (len(takes), len(x) / rate, len(words)))

    if len(takes) < len(words):
        sys.exit("Fewer takes than words. Leave a clear pause between each, or use --only.")

    # The LAST len(words) takes, so a false start at the top is dropped and a
    # repeated word keeps its retake.
    takes = takes[-len(words):]

    os.makedirs(OUT, exist_ok=True)
    for word, (a, b) in zip(words, takes):
        seg = x[a:b]
        print("  %-14s %5.2f s" % (word, len(seg) / rate), end="")
        if args.dry_run:
            print()
            continue
        dst = os.path.join(OUT, "word_" + word.lower() + ".wav")
        write(seg, rate, dst)
        print("  -> %s" % os.path.basename(dst))

    if not args.dry_run:
        print("\nNow run Tools > SALINLAHI > Refresh Narrator Voice Bank in Unity.")
    return 0


def find(x, rate):
    """Start and end sample of every take, split on silence."""
    win = int(0.02 * rate)
    usable = len(x) // win * win
    rms = np.sqrt(np.mean(np.square(x[:usable].reshape(-1, win)), axis=1))
    loud = rms > FLOOR

    takes, start = [], None
    quiet = 0
    for i, on in enumerate(loud):
        if on:
            if start is None:
                start = i
            quiet = 0
        elif start is not None:
            quiet += 1
            if quiet * 0.02 >= MIN_GAP:
                end = i - quiet
                if (end - start) * 0.02 >= MIN_WORD:
                    takes.append((start * win, end * win))
                start, quiet = None, 0
    if start is not None and (len(loud) - start) * 0.02 >= MIN_WORD:
        takes.append((start * win, usable))
    return takes


def write(seg, rate, dst):
    """Trim tight and level, to the same spec the generated clips use.

    The head matters more than it looks: the two-miss hint replays a word at
    slowPitch 0.85, which stretches any silence in front of it by 18%.
    """
    loud = np.nonzero(np.abs(seg) > FLOOR * 0.5)[0]
    if len(loud):
        seg = seg[max(0, loud[0] - int(0.010 * rate)):
                  min(len(seg), loud[-1] + int(0.040 * rate))]

    rms = float(np.sqrt(np.mean(np.square(seg)))) or 1e-9
    gain = min((10.0 ** (TARGET_DBFS / 20.0)) / rms,
               0.891 / (float(np.max(np.abs(seg))) or 1e-9))
    y = np.clip(np.rint(seg * gain * 32768.0), -32768, 32767).astype("<i2")

    with wave.open(dst, "wb") as w:
        w.setnchannels(1)
        w.setsampwidth(2)
        w.setframerate(rate)
        w.writeframes(y.tobytes())


if __name__ == "__main__":
    sys.exit(main())
