"""Split one recording of the 15 cue and feedback lines into the game's clips.

These are the lines in Docs/Defense/08_Recording_List.md - the prompting frame
and the replies - and they are the last 15 of the 436 script lines that were
still captions with a held pause.

Tools/split_word_recording.py cannot do this job. It assigns bursts to the
script by position, and the 15 lines are not one list: the recording list
prints them as two sections, Cue (8) and Feedback (7), and a reader can
reasonably start with either. Getting that backwards is the one mistake this
project cannot ship, because it ends with Rachel saying "Hindi pa tama" when a
five-year-old gets the word RIGHT.

So the section order is not assumed, it is measured. Each spoken line is cut on
silence, then every line's internal phrase structure - how many chunks it
breaks into and how long each one is - is scored against the script text, and
the two sections are tried in both orders. The script's own punctuation is what
makes this work: "Malapit na! You are very close. One more try." is three
chunks and nothing else in the list is, "Tama! That's right!" opens with a
chunk under half a second, and hint_replay and mic_blocked are the only two
lines with no Filipino in them at all.

    python3 Tools/split_cue_feedback_recording.py Recordings/fifteen_lines.mp4 --dry-run
    python3 Tools/split_cue_feedback_recording.py Recordings/fifteen_lines.mp4
    python3 Tools/split_cue_feedback_recording.py ... --review   # also write a numbered review WAV

--review writes Recordings/split/_review.wav: every clip in recording order
with a spoken number in front of it, so the mapping can be confirmed by ear in
one listen instead of opening 15 files.

Plain python3, not `python3 -I`: that flag drops the user site directory,
where numpy lives.
"""
import argparse
import csv
import os
import subprocess
import sys
import uuid
import wave

import numpy as np

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
CSV_PATH = os.path.join(ROOT, "Docs", "VoiceOverScript.csv")
OUT = os.path.join(ROOT, "Assets", "Audio", "VO")
STAGE = os.path.join(ROOT, "Recordings", "split")

TARGET_DBFS = -20.0
TRIM_REL = 0.06        # trim relative to the clip's own peak, not an absolute floor
LINE_GAP = 2.0         # silence this long separates two spoken LINES
PHRASE_GAP = 0.15      # silence this long separates two phrases inside one line
MIN_CHUNK = 0.08       # shorter than this is a click or a breath, not speech
HEAD, TAIL = 0.010, 0.040

# The two sections of Docs/Defense/08_Recording_List.md, in the order printed.
CUE = ["rachel_frame_sabihin", "rachel_frame_ngayon", "rachel_frame_ulitin",
       "rachel_cue_listening", "rachel_hint_pakinggan", "rachel_hint_replay",
       "rachel_nudge_idle", "rachel_mic_blocked"]
FEEDBACK = ["rachel_feedback_correct_1", "rachel_feedback_correct_2",
            "rachel_feedback_correct_3", "rachel_feedback_incorrect_1",
            "rachel_feedback_incorrect_2", "rachel_feedback_nothear_1",
            "rachel_feedback_nothear_2"]

META = """fileFormatVersion: 2
guid: %s
AudioImporter:
  externalObjects: {}
  serializedVersion: 8
  defaultSettings:
    serializedVersion: 2
    loadType: 0
    sampleRateSetting: 0
    sampleRateOverride: 44100
    compressionFormat: 1
    quality: 1
    conversionMode: 0
    preloadAudioData: 0
  platformSettingOverrides: {}
  forceToMono: 0
  normalize: 1
  loadInBackground: 0
  ambisonic: 0
  3D: 1
  userData: 
  assetBundleName: 
  assetBundleVariant: 
"""


def main():
    ap = argparse.ArgumentParser(description=__doc__,
                                 formatter_class=argparse.RawDescriptionHelpFormatter)
    ap.add_argument("recording")
    ap.add_argument("--dry-run", action="store_true", help="report the split, write nothing")
    ap.add_argument("--review", action="store_true", help="also write a numbered review WAV")
    ap.add_argument("--order", choices=["auto", "cue-first", "feedback-first"], default="auto")
    args = ap.parse_args()

    if not os.path.exists(args.recording):
        sys.exit("No such file: " + args.recording)

    script = {r["File Name"].strip()[:-4]: r["Script / Word"].strip()
              for r in csv.DictReader(open(CSV_PATH, encoding="utf-8"))}
    missing = [n for n in CUE + FEEDBACK if n not in script]
    if missing:
        sys.exit("Not in Docs/VoiceOverScript.csv: " + ", ".join(missing))

    os.makedirs(STAGE, exist_ok=True)
    src = os.path.join(STAGE, "_source.wav")
    subprocess.run(["/usr/bin/afconvert", "-f", "WAVE", "-d", "LEI16@44100", "-c", "1",
                    args.recording, src], check=True, capture_output=True)
    with wave.open(src, "rb") as w:
        rate = w.getframerate()
        x = np.frombuffer(w.readframes(w.getnframes()), dtype="<i2").astype(np.float32) / 32768.0

    lines = find(x, rate, LINE_GAP)
    print("%d spoken line(s) in %.1f s of recording; 15 expected"
          % (len(lines), len(x) / rate))
    if len(lines) != 15:
        sys.exit("Expected 15 lines. Leave at least %.1f s of silence between each, "
                 "and no more than that inside one line." % LINE_GAP)

    shapes = [shape(x[a:b], rate) for a, b in lines]

    if args.order == "auto":
        order, score, runner = choose(shapes, script)
        print("\nSection order: %s (fit %.2f, other way round %.2f)"
              % (order, score, runner))
        if score < runner * 1.25:
            print("The two orders fit about equally well, so this is a guess. "
                  "Re-run with --review and check by ear before committing.")
    else:
        order = args.order
    names = (CUE + FEEDBACK) if order == "cue-first" else (FEEDBACK + CUE)

    print()
    for name, (a, b), sh in zip(names, lines, shapes):
        chunks = " + ".join("%.2f" % c for c in sh)
        print("  %-28s %5.2f s  %-22s %s"
              % (name, (b - a) / rate, chunks, script[name]))

    if args.dry_run:
        print("\nNothing written (--dry-run).")
        return 0

    os.makedirs(OUT, exist_ok=True)
    print()
    for name, (a, b) in zip(names, lines):
        dst = os.path.join(OUT, name + ".wav")
        write(x[a:b], rate, dst)
        meta = dst + ".meta"
        if not os.path.exists(meta):
            open(meta, "w", encoding="utf-8").write(META % uuid.uuid4().hex)
        print("  -> Assets/Audio/VO/%s.wav" % name)

    if args.review:
        review(x, rate, lines)

    print("\nNow in Unity: Assets > Refresh, then "
          "Tools > SALINLAHI > Rebuild Narrator Voice Bank.")
    return 0


def find(x, rate, gap):
    """Start and end sample of every burst, split on `gap` seconds of silence."""
    win = int(0.01 * rate)
    usable = len(x) // win * win
    rms = np.sqrt(np.mean(np.square(x[:usable].reshape(-1, win)), axis=1))
    loud = rms > max(TRIM_REL * 0.5 * float(np.max(np.abs(x))), 0.004)

    out, start, quiet = [], None, 0
    for i, on in enumerate(loud):
        if on:
            if start is None:
                start = i
            quiet = 0
        elif start is not None:
            quiet += 1
            if quiet * 0.01 >= gap:
                end = i - quiet
                if (end - start) * 0.01 >= MIN_CHUNK:
                    out.append((start * win, end * win))
                start, quiet = None, 0
    if start is not None:
        out.append((start * win, usable))
    return out


def shape(seg, rate):
    """Durations of the phrases inside one spoken line."""
    return [(b - a) / rate for a, b in find(seg, rate, PHRASE_GAP)
            if (b - a) / rate >= MIN_CHUNK]


def expected(text):
    """What the script says this line should look like: a duration per phrase.

    Phrases are the script's own punctuation. Filipino runs a little quicker
    per syllable than the English does, and both are slow here because the
    listener is five.
    """
    import re
    parts = [p for p in re.split(r"[.!?]+|\s-\s|,", text) if p.strip()]
    out = []
    for p in parts:
        syl = max(1, len(re.findall(r"[aeiouAEIOU]+", p)))
        out.append(syl * (0.21 if is_filipino(p) else 0.25))
    return out


FIL = set("sabihin mo ngayon naman pakinggan ulit nakikinig ako mong mabuti kaya "
          "yan tama magaling ang galing hindi pa malapit na kita narinig "
          "pakilakasan boses".split())


def is_filipino(p):
    import re
    w = re.findall(r"[A-Za-z']+", p.lower())
    return sum(t.strip("'") in FIL for t in w) * 2 >= max(1, len(w))


def choose(shapes, script):
    """Score both section orders against the script and return the better one."""
    def fit(names):
        total = 0.0
        for name, sh in zip(names, shapes):
            exp = expected(script[name])
            # Chunk count is the strong signal; punctuation is deliberate here.
            total += 1.0 if len(sh) == len(exp) else 0.0
            n = min(len(sh), len(exp))
            if n:
                d = sum(abs(sh[i] - exp[i]) for i in range(n)) / n
                total += max(0.0, 1.0 - d)
        return total / len(names)

    a = fit(CUE + FEEDBACK)
    b = fit(FEEDBACK + CUE)
    return ("cue-first", a, b) if a >= b else ("feedback-first", b, a)


def write(seg, rate, dst):
    """Trim tight and level, to the same spec the other clips use.

    The head matters more than it looks: the frame lines join straight onto a
    word clip, and the two-miss hint replays both at slowPitch 0.85, which
    stretches any silence in front by 18%.
    """
    peak = float(np.max(np.abs(seg))) or 1e-9
    loud = np.nonzero(np.abs(seg) > TRIM_REL * peak)[0]
    if len(loud):
        seg = seg[max(0, loud[0] - int(HEAD * rate)):
                  min(len(seg), loud[-1] + int(TAIL * rate))]

    rms = float(np.sqrt(np.mean(np.square(seg)))) or 1e-9
    gain = min((10.0 ** (TARGET_DBFS / 20.0)) / rms,
               0.891 / (float(np.max(np.abs(seg))) or 1e-9))
    y = np.clip(np.rint(seg * gain * 32768.0), -32768, 32767).astype("<i2")

    with wave.open(dst, "wb") as w:
        w.setnchannels(1)
        w.setsampwidth(2)
        w.setframerate(rate)
        w.writeframes(y.tobytes())


def review(x, rate, lines):
    """One WAV: a spoken number, then that clip, fifteen times over."""
    dst = os.path.join(STAGE, "_review.wav")
    pieces = []
    for i, (a, b) in enumerate(lines, 1):
        tts = os.path.join(STAGE, "_n%02d.wav" % i)
        subprocess.run(["/usr/bin/say", "-o", tts, "--data-format=LEI16@44100",
                        "--channels=1", "number %d" % i], check=True, capture_output=True)
        with wave.open(tts, "rb") as w:
            pieces.append(np.frombuffer(w.readframes(w.getnframes()),
                                        dtype="<i2").astype(np.float32) / 32768.0)
        os.remove(tts)
        pieces.append(np.zeros(int(0.35 * rate), np.float32))
        pieces.append(x[a:b])
        pieces.append(np.zeros(int(0.7 * rate), np.float32))
    y = np.clip(np.rint(np.concatenate(pieces) * 32768.0), -32768, 32767).astype("<i2")
    with wave.open(dst, "wb") as w:
        w.setnchannels(1)
        w.setsampwidth(2)
        w.setframerate(rate)
        w.writeframes(y.tobytes())
    print("\n  -> Recordings/split/_review.wav   (%.0f s; afplay it to check the order)"
          % (len(y) / rate))


if __name__ == "__main__":
    sys.exit(main())
