"""Cut one speaker's pronunciation-dataset recording into labelled takes.

Each speaker records ONE file: for every word in WORDS, in that order, the
pronunciation the game teaches three times, then the other pronunciation three
times, with about a second of silence between takes. This script finds the
takes, labels them by their position in that sequence and writes

    Tools/pronunciation_dataset/manifests/<speaker>.csv   (committed: timings + labels, no audio)
    Recordings/Dataset/<speaker>/<word>_<class>_<n>.wav  (git-ignored: 16 kHz mono clips)
    Recordings/Dataset/<speaker>/review.html            (git-ignored: listen to every take)

Silence detection is only the first guess. When it does not find exactly
6 takes x 6 words, the script writes the manifest with what it found, labels
nothing and stops. Open the manifest, fix it, and re-run with --from-manifest:

  * keep = 0          drops a row (a cough, a breath, a false start);
  * start_s / end_s   move a boundary (look at review.html for the times);
  * add a row         to split two takes that ran together.

With --from-manifest the boundaries are taken as written and nothing is
detected again, so manual fixes are never overwritten.

    python Tools/pronunciation_dataset/segment.py D:/DOWNLOADS/la.mp3
    python Tools/pronunciation_dataset/segment.py D:/DOWNLOADS/la.mp3 --speaker la --from-manifest
    python Tools/pronunciation_dataset/segment.py new.wav --dry-run

Reads anything libsndfile reads (WAV, FLAC, OGG, MP3); `pip install soundfile`.
Convert .m4a to WAV first. Needs numpy and scipy.
"""
import argparse
import csv
import html
import os
import sys

import numpy as np
import soundfile as sf
from scipy.signal import resample_poly

ROOT = os.path.dirname(os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
MANIFESTS = os.path.join(ROOT, "Tools", "pronunciation_dataset", "manifests")
DATASET = os.path.join(ROOT, "Recordings", "Dataset")

# The recording order. Must match AmbiguousWords.cs (the six strictly judged words).
WORDS = ["Tayo", "Upo", "Bukas", "Basa", "Bati", "Pasok"]
CORRECT_TAKES = 3
ALTERNATE_TAKES = 3
TAKES_PER_WORD = CORRECT_TAKES + ALTERNATE_TAKES

OUT_RATE = 16000
HOP_S = 0.01
MERGE_GAP_S = 0.25     # sound closer than this belongs to the same take (a stop closure)
MIN_TAKE_S = 0.12      # shorter blips are noise
PEAK_WITHIN_DB = 18    # a take's loudest moment must reach within this of the file's loud level
PAD_S = 0.15           # silence kept either side of a take (never past halfway to the next one)

FIELDS = ["index", "start_s", "end_s", "word", "class", "take", "keep", "peak_db", "note"]


def load(path):
    x, sr = sf.read(path, always_2d=True)
    x = x.mean(axis=1).astype(np.float64)
    return x, sr


def frame_db(x, sr):
    hop = int(sr * HOP_S)
    n = len(x) // hop
    rms = np.sqrt((x[: n * hop].reshape(n, hop) ** 2).mean(axis=1))
    return 20 * np.log10(rms + 1e-9)


def detect(x, sr):
    """Takes as [start_s, end_s, peak_db, why_dropped], by loudness against this file's own noise floor."""
    db = frame_db(x, sr)
    n = len(db)
    noise = np.percentile(db, 10)
    loud = np.percentile(db, 99)
    threshold = max(noise + 12, loud - 35)
    on = db > threshold
    runs, i = [], 0
    while i < n:
        if on[i]:
            j = i
            while j < n and on[j]:
                j += 1
            runs.append([i, j])
            i = j
        else:
            i += 1
    merged = []
    for r in runs:
        if merged and r[0] - merged[-1][1] < MERGE_GAP_S / HOP_S:
            merged[-1][1] = r[1]
        else:
            merged.append(r)
    takes = []
    for a, b in merged:
        peak = float(db[a:b].max())
        if (b - a) * HOP_S < 0.05:
            continue   # a click: not worth a row
        why = ""
        if (b - a) * HOP_S < MIN_TAKE_S:
            why = "too short for a take"
        elif peak < loud - PEAK_WITHIN_DB:
            why = "too quiet for a take"
        takes.append([a * HOP_S, b * HOP_S, peak, why])
    info = "noise floor %.1f dB, loud level %.1f dB, threshold %.1f dB" % (noise, loud, threshold)
    return takes, info


def label(rows):
    """Labels the kept rows in recording order. Returns an error message, or None."""
    kept = [r for r in rows if r["keep"] == "1"]
    expected = len(WORDS) * TAKES_PER_WORD
    if len(kept) != expected:
        for r in rows:
            r["word"] = r["class"] = r["take"] = ""
        return "found %d takes, expected %d (%d words x %d)" % (len(kept), expected, len(WORDS), TAKES_PER_WORD)
    for k, r in enumerate(kept):
        w, t = divmod(k, TAKES_PER_WORD)
        r["word"] = WORDS[w]
        r["class"] = "correct" if t < CORRECT_TAKES else "alternate"
        r["take"] = str(t + 1 if t < CORRECT_TAKES else t - CORRECT_TAKES + 1)
    for r in rows:
        if r["keep"] != "1":
            r["word"] = r["class"] = r["take"] = ""
    return None


def read_manifest(path):
    with open(path, newline="", encoding="utf-8") as f:
        lines = [l for l in f if not l.startswith("#")]
    rows = list(csv.DictReader(lines))
    for r in rows:
        for k in FIELDS:
            r.setdefault(k, "")
        r["keep"] = "0" if r["keep"].strip() == "0" else "1"
    rows.sort(key=lambda r: float(r["start_s"]))
    return rows


def write_manifest(path, rows, source, note):
    os.makedirs(os.path.dirname(path), exist_ok=True)
    with open(path, "w", newline="", encoding="utf-8") as f:
        f.write("# source: %s\n" % os.path.basename(source))
        f.write("# %s\n" % note)
        f.write("# Edit start_s/end_s, set keep=0 to drop a row, add rows to split takes; then re-run with --from-manifest.\n")
        w = csv.DictWriter(f, fieldnames=FIELDS)
        w.writeheader()
        for i, r in enumerate(rows):
            r["index"] = str(i)
            w.writerow({k: r.get(k, "") for k in FIELDS})


def clip(x, sr, start, end, lo, hi):
    a = max(lo, start - PAD_S)
    b = min(hi, end + PAD_S)
    y = x[int(a * sr): int(b * sr)]
    g = np.gcd(sr, OUT_RATE)
    y = resample_poly(y, OUT_RATE // g, sr // g)
    peak = np.abs(y).max() if len(y) else 0
    if peak > 0.98:   # only ever turned down, never up: loudness is not a feature
        y = y * (0.98 / peak)
    return y


def write_review(path, speaker, x, sr, rows):
    db = frame_db(x, sr)
    width = 1400
    step = max(1, len(db) // width)
    env = db[: len(db) // step * step].reshape(-1, step).max(axis=1)
    lo, hi = -70.0, 0.0
    pts = " ".join("%d,%.1f" % (i, 120 - 120 * (min(max(v, lo), hi) - lo) / (hi - lo)) for i, v in enumerate(env))
    total = len(x) / sr
    scale = len(env) / total
    rects, items = [], []
    for r in rows:
        s, e = float(r["start_s"]), float(r["end_s"])
        colour = "#888" if r["keep"] != "1" else "#2a7" if r["class"] == "correct" else "#d63"
        rects.append('<rect x="%.1f" y="0" width="%.1f" height="120" fill="%s" opacity="0.25"><title>%s %.2f-%.2f</title></rect>'
                     % (s * scale, max(1, (e - s) * scale), colour, html.escape(r["index"]), s, e))
        name = "%s_%s_%s.wav" % (r["word"].lower(), r["class"], r["take"]) if r["word"] else ""
        audio = '<audio controls preload="none" src="%s"></audio>' % name if name else "(not labelled)"
        items.append("<tr><td>%s</td><td>%.2f&ndash;%.2f</td><td>%s</td><td>%s</td><td>%s</td><td>%s</td><td>%s</td></tr>"
                     % (r["index"], s, e, html.escape(r["word"]), html.escape(r["class"]), r["take"], r["keep"], audio))
    page = """<!doctype html><meta charset="utf-8"><title>%s takes</title>
<style>body{font:14px system-ui;margin:16px}svg{width:100%%;height:140px;background:#f6f6f6}td{padding:2px 8px}</style>
<h2>%s: %d rows</h2><p>Green = correct, orange = alternate, grey = dropped. Times in seconds of the source file.</p>
<svg viewBox="0 0 %d 120" preserveAspectRatio="none">%s<polyline points="%s" fill="none" stroke="#333" stroke-width="1"/></svg>
<table><tr><th>#</th><th>time</th><th>word</th><th>class</th><th>take</th><th>keep</th><th>listen</th></tr>%s</table>
""" % (html.escape(speaker), html.escape(speaker), len(rows), len(env), "".join(rects), pts, "".join(items))
    with open(path, "w", encoding="utf-8") as f:
        f.write(page)


def main():
    ap = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    ap.add_argument("audio")
    ap.add_argument("--speaker", help="short name for the speaker (default: the file name)")
    ap.add_argument("--from-manifest", action="store_true", help="use the manifest's boundaries as written")
    ap.add_argument("--dry-run", action="store_true", help="report what was found, write nothing")
    args = ap.parse_args()

    speaker = args.speaker or os.path.splitext(os.path.basename(args.audio))[0]
    if not speaker.replace("_", "").replace("-", "").isalnum():
        sys.exit("speaker name must be letters, digits, - or _: " + speaker)
    manifest = os.path.join(MANIFESTS, speaker + ".csv")
    x, sr = load(args.audio)

    if args.from_manifest:
        if not os.path.exists(manifest):
            sys.exit("no manifest yet: " + manifest)
        rows = read_manifest(manifest)
        note = "boundaries from the manifest"
    else:
        takes, info = detect(x, sr)
        rows = [{"start_s": "%.2f" % s, "end_s": "%.2f" % e, "keep": "0" if why else "1", "peak_db": "%.1f" % p,
                 "note": why} for s, e, p, why in takes]
        note = "detected: " + info

    error = label(rows)
    print("%s: %d rows, %s" % (speaker, len(rows), note))
    for r in rows:
        print("  %3s %7s-%7s  %-6s %-9s %s %s" % (r.get("index", ""), r["start_s"], r["end_s"], r["word"], r["class"], r["take"],
                                                 "" if r["keep"] == "1" else "(dropped)"))
    if args.dry_run:
        print(error or "labels OK")
        return 0 if error is None else 2

    if not args.from_manifest or error is None:
        write_manifest(manifest, rows, args.audio, note)
        print("wrote " + manifest)

    out = os.path.join(DATASET, speaker)
    os.makedirs(out, exist_ok=True)
    for f in os.listdir(out):
        if f.endswith(".wav"):
            os.remove(os.path.join(out, f))
    if error is None:
        kept = [r for r in rows if r["keep"] == "1"]
        total = len(x) / sr
        for i, r in enumerate(kept):
            s, e = float(r["start_s"]), float(r["end_s"])
            lo = 0.0 if i == 0 else (float(kept[i - 1]["end_s"]) + s) / 2
            hi = total if i == len(kept) - 1 else (e + float(kept[i + 1]["start_s"])) / 2
            y = clip(x, sr, s, e, lo, hi)
            name = "%s_%s_%s.wav" % (r["word"].lower(), r["class"], r["take"])
            sf.write(os.path.join(out, name), y, OUT_RATE, subtype="PCM_16")
        print("wrote %d clips to %s" % (len(kept), out))
    write_review(os.path.join(out, "review.html"), speaker, x, sr, rows)
    print("review: " + os.path.join(out, "review.html"))

    if error:
        print()
        print("NOT LABELLED: " + error)
        print("Fix " + manifest + " (keep=0 for noise, move or add boundaries), then re-run with --from-manifest.")
        return 2
    return 0


if __name__ == "__main__":
    sys.exit(main())
