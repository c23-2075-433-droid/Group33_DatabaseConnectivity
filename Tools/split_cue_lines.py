#!/usr/bin/env python3
"""
Cuts one recording of the fifteen cue lines into the game's cue clips.

The fifty single words already have two splitters. Neither can do these.
split_word_recording.py cuts on silence and assigns the pieces by position,
which breaks the moment a line is retaken or skipped. split_by_transcript.py
asks the recogniser what it heard and looks each WORD up by name - but a cue
line is a sentence, several of them mix Filipino and English in one breath
("Hindi kita narinig. I did not hear you - try once more."), and no single
word identifies which line it belongs to. "Sabihin mo:" and "Ngayon naman,
sabihin mo:" share every word one of them has.

So this matches whole utterances against whole lines. The recording is split
into utterances on the pauses between them, each utterance is scored against
all fifteen scripted lines, and it is assigned to the line it matches best.
That makes the mapping independent of the order they were said in: a line can
be skipped and recorded later, the list can be read out of order, and a
fluffed line can simply be said again - the LAST utterance that matches a line
is the one kept, the same rule the word splitters use.

A line that nothing matches well enough is reported rather than guessed at.
That matters more here than for the words: these clips are Teacher Rachel
telling a child they were right or wrong, and a mislabelled one tells them the
opposite of what happened.

    python3 Tools/split_cue_lines.py Recordings/cue_lines.m4a --dry-run
    python3 Tools/split_cue_lines.py Recordings/cue_lines.m4a
    python3 Tools/split_cue_lines.py ... --only rachel_feedback_correct_1

Plain python3, not `python3 -I`: that flag drops the user site directory,
where numpy lives.

Exits 0 when every line was found, 1 otherwise, listing all the problems in
one run rather than stopping at the first.
"""

import argparse
import csv
import difflib
import json
import os
import subprocess
import sys
import urllib.error
import wave

import numpy as np

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import generate_voiceover as vo
import split_by_transcript as sbt

STAGE = os.path.join(vo.ROOT, "Recordings", "cue_split")

# The lines this tool is for. Taken from the CSV rather than listed here, so
# adding a sixteenth cue line to the script is all it takes.
SEGMENTS = ("Cue", "Feedback")

# A silence at least this long between two recognised words ends an utterance.
# Part 2 of Docs/RecordingScript.md asks for about a second between lines. The
# longest pause INSIDE one of these lines is the full stop in "Nakikinig ako.
# I am listening!", which reads at about a quarter of a second, so 0.6 s
# separates the lines without splitting one in half.
UTTERANCE_GAP = 0.60

# How close an utterance has to be to a scripted line to count as that line,
# measured on the normalised text so punctuation and case do not count.
#
# A single threshold is the wrong instrument here. Set it high enough to keep
# the confusable pairs apart and it rejects good takes whose transcript is
# merely imperfect; set it low enough to tolerate the recogniser and two
# similar lines become interchangeable. So there are two tests, and a line is
# only claimed when it passes both: close enough in absolute terms, AND
# clearly closer than whatever came second.
#
# The margin is sized from the list itself. Scored against each other, the
# nearest pairs are:
#
#     0.647  rachel_frame_ulitin   vs rachel_feedback_correct_3
#     0.621  rachel_frame_sabihin  vs rachel_frame_ngayon
#     0.553  rachel_hint_replay    vs rachel_mic_blocked
#
# so a correct match has to beat its runner-up by more than the noise in a
# transcript, not by more than 0.647.
MATCH_MIN = 0.70
MATCH_MARGIN = 0.08

# Kept either side of the utterance, as split_by_transcript does: the
# recogniser marks where the vowel is confident, which clips the quiet start
# of a p or a k and the release at the end.
PAD_HEAD = 0.10
PAD_TAIL = 0.18


def main():
    ap = argparse.ArgumentParser(description=__doc__,
                                 formatter_class=argparse.RawDescriptionHelpFormatter)
    ap.add_argument("recording")
    ap.add_argument("--dry-run", action="store_true",
                    help="Report the mapping and write no audio. Still transcribes.")
    ap.add_argument("--only", help="comma-separated file names, if the recording holds some")
    ap.add_argument("--language", default="fil", help="Recogniser language code.")
    ap.add_argument("--reuse-transcript", action="store_true",
                    help="Use the transcript already in Recordings/cue_split, if there is one.")
    args = ap.parse_args()

    if not os.path.exists(args.recording):
        return "No such file: " + args.recording

    lines = scripted_lines()
    if args.only:
        want = {s.strip() for s in args.only.split(",") if s.strip()}
        lines = [l for l in lines if l["file"] in want]
        unknown = want - {l["file"] for l in lines}
        if unknown:
            return "Not cue lines in the script: " + ", ".join(sorted(unknown))
    if not lines:
        return "No Cue or Feedback lines found in %s" % os.path.relpath(vo.CSV, vo.ROOT)

    os.makedirs(STAGE, exist_ok=True)
    source = os.path.join(STAGE, "_source.wav")
    subprocess.run(["/usr/bin/afconvert", "-f", "WAVE", "-d", "LEI16@44100", "-c", "1",
                    args.recording, source], check=True, capture_output=True)

    cache = os.path.join(STAGE, "_transcript.json")
    if args.reuse_transcript and os.path.exists(cache):
        payload = json.load(open(cache, encoding="utf-8"))
        print("transcript: reused from %s" % os.path.relpath(cache, vo.ROOT))
    else:
        key = vo.read_key()
        if key is None:
            return 1
        try:
            payload = sbt.transcribe(key, source, args.language)
        except urllib.error.HTTPError as e:
            return "Transcription failed: HTTP %s %s\n%s" % (
                e.code, e.reason, e.read()[:400].decode("utf-8", "replace"))
        json.dump(payload, open(cache, "w", encoding="utf-8"), ensure_ascii=False)

    tokens = [t for t in payload.get("words", []) if t.get("type") == "word"]
    if not tokens:
        return "The recogniser heard no words in %s" % args.recording
    utterances = group(tokens)

    with wave.open(source, "rb") as w:
        rate, n = w.getframerate(), w.getnframes()
        x = np.frombuffer(w.readframes(n), dtype="<i2").astype(np.float32) / 32768.0

    # Every utterance goes to the line it matches best; the last one to claim
    # a line wins, so a retake simply replaces the take before it.
    claimed, loose = {}, []
    for u in utterances:
        ranked = sorted(((similar(u["text"], l["norm"]), l) for l in lines),
                        key=lambda p: p[0], reverse=True)
        score, best = ranked[0]
        runner = ranked[1][0] if len(ranked) > 1 else 0.0
        if score >= MATCH_MIN and (score - runner) >= MATCH_MARGIN:
            claimed.setdefault(best["file"], []).append((u, score))
        else:
            why = "too unlike anything" if score < MATCH_MIN else (
                "too close to %s (%.2f)" % (ranked[1][1]["file"], runner))
            loose.append((u, best, score, why))

    print("\n%d utterance(s) in %.1f s, %d of %d line(s) matched\n"
          % (len(utterances), len(x) / rate, len(claimed), len(lines)))
    print("%-34s %5s %6s  %7s %7s" % ("FILE", "TAKES", "MATCH", "AT", "LENGTH"))

    os.makedirs(vo.OUT, exist_ok=True)
    written, missing = 0, []
    for l in lines:
        takes = claimed.get(l["file"])
        if not takes:
            missing.append(l)
            continue
        u, score = takes[-1]
        a = max(0, int((u["start"] - PAD_HEAD) * rate))
        b = min(len(x), int((u["end"] + PAD_TAIL) * rate))
        seg = sbt.level(trim_sentence(x[a:b], rate))
        mark = "  (take %d)" % len(takes) if len(takes) > 1 else ""
        print("%-34s %5d  %5.2f  %6.2fs %6.2fs%s"
              % (l["file"], len(takes), score, u["start"], len(seg) / float(rate), mark))
        if args.dry_run:
            continue
        sbt.write(seg, rate, os.path.join(vo.OUT, l["file"] + ".wav"))
        written += 1

    # Everything wrong in one run, the way check_voiceover_script.py reports.
    for l in missing:
        sys.stderr.write("NOT HEARD  %-34s %s\n" % (l["file"], l["text"]))
    for u, best, score, why in loose:
        near = (" - best %s at %.2f, %s" % (best["file"], score, why)) if best else ""
        sys.stderr.write("UNMATCHED  %.2fs  \"%s\"%s\n"
                         % (u["start"], u["text"][:58], near))

    if args.dry_run:
        print("\n--dry-run: no audio written.")
    elif written:
        print("\n%d clip(s) -> %s" % (written, os.path.relpath(vo.OUT, vo.ROOT)))
        print("Now run Tools > SALINLAHI > Refresh Narrator Voice Bank in Unity.")
    return 1 if (missing or loose) else 0


def scripted_lines():
    """The Cue and Feedback rows, as {file, text, norm}."""
    out = []
    for r in csv.DictReader(open(vo.CSV, encoding="utf-8")):
        if (r.get("Segment") or "").strip() not in SEGMENTS:
            continue
        name = (r.get("File Name") or "").strip()
        if not name:
            continue
        if name.endswith(".wav"):
            name = name[:-4]
        text = (r.get("Script / Word") or "").strip()
        out.append({"file": name, "text": text, "norm": sbt.norm(text)})
    return out


def group(tokens):
    """Split the recognised words into utterances on the pauses between them."""
    out, cur = [], []
    for t in tokens:
        if cur and float(t["start"]) - float(cur[-1]["end"]) >= UTTERANCE_GAP:
            out.append(utterance(cur))
            cur = []
        cur.append(t)
    if cur:
        out.append(utterance(cur))
    return out


def utterance(tokens):
    return {"start": float(tokens[0]["start"]),
            "end": float(tokens[-1]["end"]),
            "text": sbt.norm(" ".join(t.get("text", "") for t in tokens))}


def similar(heard, scripted):
    """0 to 1 on the normalised text. Letters only, so punctuation cannot count."""
    if not heard or not scripted:
        return 0.0
    return difflib.SequenceMatcher(None, heard, scripted).ratio()


def trim_sentence(seg, rate):
    """
    Cut to the sound at each end, and nothing in the middle.

    split_by_transcript.trim() stops at the first 0.35 s gap, because a token
    that ran past its word should keep only the word. A sentence is the
    opposite case: the pause after "Hindi pa tama." is part of the line, and
    stopping there would deliver half of it.
    """
    peak = float(np.max(np.abs(seg))) if len(seg) else 0.0
    if peak <= 0:
        return seg
    loud = np.nonzero(np.abs(seg) > peak * sbt.TRIM_REL)[0]
    if not len(loud):
        return seg
    head = max(0, loud[0] - int(0.015 * rate))
    tail = min(len(seg), loud[-1] + int(0.060 * rate))
    return seg[head:tail]


if __name__ == "__main__":
    sys.exit(main())
