#!/usr/bin/env python3
"""
Counts how much of SALINLAHI exists, by reading the project rather than asking.

Written because the documentation drifted badly from the build. README.md said
"No voice-over audio yet" with 188 clips on disk, and "the first three scenes
of Level 1 are playable" with fifteen scenes built and fade-chained. The
baseline ledger had Parke down as Planned after all five of its scenes had
been built. Every one of those numbers was true when it was written.

A panel that reads the paperwork and then watches the demo will conclude the
group does not know its own system, which is a worse impression than being
behind. So the number should come from the repository, and it should be
re-runnable, so it can be recomputed the morning of the defense instead of
remembered from a fortnight before.

Denominators are the design's own, not invented here: sixteen scenes and 53
distinct words from Docs/VoicePromptingDesign.md, 434 script lines from
Docs/VoiceOverScript.csv.

    python3 Tools/completeness_report.py
    python3 Tools/completeness_report.py --markdown    # to paste into the manuscript
"""

import argparse
import collections
import csv
import glob
import json
import os
import re
import sys

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
CSV = os.path.join(ROOT, "Docs", "VoiceOverScript.csv")
VO = os.path.join(ROOT, "Assets", "Audio", "VO")
SCENES = os.path.join(ROOT, "Assets", "Scenes")
TRACKS = os.path.join(ROOT, "Assets", "Resources", "VisemeTracks.json")

# The design document lays out sixteen scenes across four environments.
# Bahay's sixth, "Leaving for School", has a slot on the journey map
# (JourneyMap.cs) and no scene behind it; Palengke is a marker with no scenes
# at all. Both are counted as missing rather than quietly dropped.
DESIGNED_SCENES = 16
DESIGNED_ENVIRONMENTS = 4

# The segments that carry the teaching, as opposed to the narration around it.
# A pre-reader cannot read a caption, so a word whose Prompt, Teach, Recall and
# Word lines are unrecorded is not being taught - whatever else is voiced.
TEACHING = ("Prompt", "Teach", "Recall", "Word")


def main():
    ap = argparse.ArgumentParser(description=__doc__,
                                 formatter_class=argparse.RawDescriptionHelpFormatter)
    ap.add_argument("--markdown", action="store_true", help="A table to paste into the manuscript.")
    args = ap.parse_args()

    rows = [r for r in csv.DictReader(open(CSV, encoding="utf-8"))
            if "(Unused)" not in (r.get("Category") or "")]
    have = {os.path.splitext(os.path.basename(p))[0] for p in glob.glob(os.path.join(VO, "*.wav"))}
    voiced = [r for r in rows if os.path.splitext(r["File Name"])[0] in have]

    scenes = sorted(glob.glob(os.path.join(SCENES, "Chapter*.unity")))
    in_scenes = collections.Counter()
    for path in scenes:
        for m in re.finditer(r"^\s*-\s*word:\s*(\S+)\s*$", open(path, encoding="utf-8",
                                                                errors="ignore").read(), re.M):
            in_scenes[m.group(1).strip().lower()] += 1

    placed = sorted({(r.get("Target Word") or "").strip() for r in rows
                     if (r.get("Target Word") or "").strip()})
    live = [w for w in placed if w.lower() in in_scenes]

    taught = []
    for word in placed:
        need = [r for r in rows if (r.get("Target Word") or "").strip() == word
                and r["Segment"] in TEACHING]
        if need and all(os.path.splitext(r["File Name"])[0] in have for r in need):
            taught.append(word)

    tracks = {}
    if os.path.exists(TRACKS):
        try:
            tracks = json.load(open(TRACKS, encoding="utf-8")).get("tracks", {})
        except ValueError:
            pass

    table = [
        ("Chapter scenes built", len(scenes), DESIGNED_SCENES),
        ("Environments with scenes", len({os.path.basename(p).split("_")[0] for p in scenes}),
         DESIGNED_ENVIRONMENTS),
        ("Vocabulary words live in a scene", len(live), len(placed)),
        ("Words a pre-reader can learn by ear", len(taught), len(placed)),
        ("Script lines voiced", len(voiced), len(rows)),
        ("Clips with a mouth track", sum(1 for n in have if n in tracks), len(have)),
    ]

    if args.markdown:
        print("| Measure | Built | Designed | |")
        print("|---|---:|---:|---:|")
        for label, got, total in table:
            print("| %s | %d | %d | %d%% |" % (label, got, total, pct(got, total)))
    else:
        print("\nSALINLAHI completeness, read from the repository\n")
        for label, got, total in table:
            print("  %-38s %4d / %-4d  %3d%%  %s"
                  % (label, got, total, pct(got, total), bar(got, total)))

    print()
    by = collections.Counter()
    tot = collections.Counter()
    for r in rows:
        tot[r["Segment"]] += 1
        if os.path.splitext(r["File Name"])[0] in have:
            by[r["Segment"]] += 1
    print("Voice-over by segment:")
    for seg in sorted(tot, key=lambda s: -tot[s]):
        flag = "  <- the teaching itself" if seg in TEACHING and by[seg] < tot[seg] else ""
        print("  %-14s %3d / %-3d %s%s" % (seg, by[seg], tot[seg], bar(by[seg], tot[seg]), flag))

    missing = [w for w in placed if w not in taught]
    if missing:
        print("\n%d word(s) still taught by caption rather than voice:" % len(missing))
        print("  " + ", ".join(missing))

    unplaced = [w for w in placed if w.lower() not in in_scenes]
    if unplaced:
        print("\nIn the script but in no scene: %s" % ", ".join(unplaced))
    return 0


def pct(got, total):
    return int(round(100.0 * got / total)) if total else 0


def bar(got, total, width=18):
    n = int(round(width * got / float(total))) if total else 0
    return "#" * n + "." * (width - n)


if __name__ == "__main__":
    sys.exit(main())
