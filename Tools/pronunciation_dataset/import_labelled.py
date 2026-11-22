"""Add the Editor's labelled takes (hold R, then F5 / F6) to the dataset.

In Unity, every hold-R take of a word that you then label with F5 (said the
taught way) or F6 (said the other way) is kept under
Recordings/Labelled/WORD/correct|wrong_stress/. This copies the takes of the
six strictly judged words into Recordings/Dataset/SPEAKER/ as
WORD_correct_N.wav / WORD_alternate_N.wav, the names the trainer reads.
F7 takes (a sound wrong) are not pronunciation examples and are skipped.

    python Tools/pronunciation_dataset/import_labelled.py --speaker kl
    python Tools/pronunciation_dataset/import_labelled.py --speaker kl --since 20261122_1200
    python Tools/pronunciation_dataset/import_labelled.py --speaker kl --until 20261122_1313
    python Tools/pronunciation_dataset/import_labelled.py --speaker sp5 --since 20261122_1313

When several people record on the same computer, their takes share the same
folders: give each person's session its own time range (--since / --until,
compared with the file name yyyymmdd_hhmmss) and its own --speaker.

The speaker's folder is replaced, not added to, so re-running is safe. Use a
different --speaker for each person who recorded. Then train:

    sh Tools/test_stress_rules.sh dataset-train

A speaker only trains a word once they have takes of BOTH pronunciations of it
(see PronunciationModelBuilder.Train), so label some takes with F6 as well.
"""
import argparse
import os
import shutil
import sys

ROOT = os.path.dirname(os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
LABELLED = os.path.join(ROOT, "Recordings", "Labelled")
DATASET = os.path.join(ROOT, "Recordings", "Dataset")
WORDS = ["Tayo", "Upo", "Bukas", "Basa", "Bati", "Pasok"]
CLASSES = {"correct": "correct", "wrong_stress": "alternate"}


def main():
    ap = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    ap.add_argument("--speaker", required=True, help="short pseudonym of who recorded the takes")
    ap.add_argument("--since", default="", help="only takes saved at or after this time (yyyymmdd_hhmmss prefix)")
    ap.add_argument("--until", default="", help="only takes saved before this time (yyyymmdd_hhmmss prefix)")
    args = ap.parse_args()
    if not args.speaker.replace("_", "").replace("-", "").isalnum():
        sys.exit("speaker name must be letters, digits, - or _")

    found = []
    for word in WORDS:
        for label, cls in CLASSES.items():
            d = os.path.join(LABELLED, word, label)
            if not os.path.isdir(d):
                continue
            for f in sorted(os.listdir(d)):
                if f.lower().endswith(".wav") and f >= args.since and (not args.until or f < args.until):
                    found.append((word, cls, os.path.join(d, f)))
    if not found:
        sys.exit("no labelled takes of " + ", ".join(WORDS) + " in " + LABELLED)

    out = os.path.join(DATASET, args.speaker)
    if os.path.isdir(out):
        shutil.rmtree(out)
    os.makedirs(out)
    count = {}
    for word, cls, src in found:
        key = (word, cls)
        count[key] = count.get(key, 0) + 1
        shutil.copyfile(src, os.path.join(out, "%s_%s_%d.wav" % (word.lower(), cls, count[key])))

    print("wrote %d takes to %s" % (len(found), out))
    for word in WORDS:
        c, a = count.get((word, "correct"), 0), count.get((word, "alternate"), 0)
        note = "" if c and a else "   (not used for training until it has both)"
        print("  %-6s correct %2d, alternate %2d%s" % (word, c, a, note))
    return 0


if __name__ == "__main__":
    sys.exit(main())
