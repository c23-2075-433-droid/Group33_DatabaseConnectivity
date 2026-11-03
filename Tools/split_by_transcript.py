#!/usr/bin/env python3
"""
Cuts a recording of the Filipino words apart by listening to it.

Tools/split_word_recording.py cuts on silence and then assigns the pieces to
the script's words by position: first burst is the first word, and so on. That
works when the recording holds exactly the words the script lists, in order,
and nothing else. It breaks the moment it does not - a retake in the middle, a
word skipped, a cough - because every file after the mistake is given the wrong
name, and a mislabelled clip is worse than a missing one. The game will happily
teach a child that "Sabon" is pronounced "suklay".

It also depends on a fixed silence threshold, which assumes the room. At
0.012 - about -38 dBFS - a recording made in a room whose own noise sits near
-40 has almost no margin, and the splitter either hears one long word or
sixty-five.

This asks the recogniser what was said and when, and cuts on that instead. The
mapping stops being positional: each word is found by name, so the order does
not matter, a missing word is reported rather than silently shifting everything
after it, and a word said twice keeps its last take.

    python3 Tools/split_by_transcript.py Recordings/filipino_words.mov --dry-run
    python3 Tools/split_by_transcript.py Recordings/filipino_words.mov
"""

import argparse
import csv
import json
import mimetypes
import os
import re
import subprocess
import sys
import unicodedata
import urllib.error
import urllib.request
import uuid
import wave

import numpy as np

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import generate_voiceover as vo

STT = "https://api.elevenlabs.io/v1/speech-to-text"
STAGE = os.path.join(vo.ROOT, "Recordings", "split")

# How much to keep either side of the recogniser's word boundary. It marks
# where the vowel is confident, which clips the quiet start of a p or a k and
# the release at the end.
PAD_HEAD = 0.08
PAD_TAIL = 0.12

# Trimming is relative to each clip's OWN peak, not an absolute dBFS floor.
# A phone recording in an ordinary room sits near -40 dBFS between words -
# above generate_voiceover's -48 floor - so an absolute threshold trims
# nothing and leaves room tone at the head, which VoiceNarrator's 0.85
# slowPitch then stretches by 18%.
TRIM_REL = 0.06
TARGET_DBFS = -20.0

# What the recogniser writes against how the script spells it. Filipino
# orthography is not the recogniser's strong suit and these three come back
# consistently misspelt rather than wrong.
ALIAS = {
    "kamusta": "kumusta",
    "akiyat": "akyat",
    "daosdos": "dausdos",
}


def main():
    ap = argparse.ArgumentParser(description=__doc__,
                                 formatter_class=argparse.RawDescriptionHelpFormatter)
    ap.add_argument("recording")
    ap.add_argument("--dry-run", action="store_true",
                    help="Report the mapping and write no audio. Still transcribes.")
    ap.add_argument("--language", default="fil", help="Recogniser language code.")
    ap.add_argument("--reuse-transcript", action="store_true",
                    help="Use the transcript already in Recordings/split, if there is one.")
    args = ap.parse_args()

    if not os.path.exists(args.recording):
        return "No such file: " + args.recording

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
            payload = transcribe(key, source, args.language)
        except urllib.error.HTTPError as e:
            return "Transcription failed: HTTP %s %s\n%s" % (
                e.code, e.reason, e.read()[:400].decode("utf-8", "replace"))
        json.dump(payload, open(cache, "w", encoding="utf-8"), ensure_ascii=False)

    heard = {}
    for t in payload.get("words", []):
        if t.get("type") != "word":
            continue
        k = norm(t.get("text", ""))
        heard.setdefault(ALIAS.get(k, k), []).append(t)

    words = [r["Script / Word"].strip() for r in
             csv.DictReader(open(vo.CSV, encoding="utf-8")) if r["Segment"] == "Word"]

    with wave.open(source, "rb") as w:
        rate, n = w.getframerate(), w.getnframes()
        x = np.frombuffer(w.readframes(n), dtype="<i2").astype(np.float32) / 32768.0

    found, missing = [], []
    for word in words:
        takes = heard.get(norm(word))
        (found if takes else missing).append((word, takes))

    print("\n%d of %d word(s) heard in %.1f s\n" % (len(found), len(words), len(x) / rate))
    print("%-14s %5s  %7s %7s" % ("WORD", "TAKES", "AT", "LENGTH"))

    os.makedirs(vo.OUT, exist_ok=True)
    written = 0
    for word, takes in found:
        t = takes[-1]                       # the last take is the one she meant
        a = max(0, int((float(t["start"]) - PAD_HEAD) * rate))
        b = min(len(x), int((float(t["end"]) + PAD_TAIL) * rate))
        seg = level(trim(x[a:b]))
        mark = "  (take %d)" % len(takes) if len(takes) > 1 else ""
        print("%-14s %5d  %6.2fs %6.2fs%s"
              % (word, len(takes), float(t["start"]), len(seg) / float(rate), mark))
        if args.dry_run:
            continue
        write(seg, rate, os.path.join(vo.OUT, "word_" + word.lower() + ".wav"))
        written += 1

    # Every problem at once rather than the first, the way
    # check_voiceover_script.py reports - one run should tell you everything
    # that needs saying again.
    for word, _ in missing:
        sys.stderr.write("NOT HEARD  %s\n" % word)

    extra = sorted(set(heard) - {norm(w) for w in words})
    if extra:
        sys.stderr.write("heard but not in the script: %s\n" % ", ".join(extra))

    if args.dry_run:
        print("\n--dry-run: no audio written.")
    elif written:
        print("\n%d clip(s) -> %s" % (written, os.path.relpath(vo.OUT, vo.ROOT)))
        print("Next:")
        print("  python3 Tools/voice_changer.py --words")
        print("  python3 Tools/align_existing_vo.py")
    return 1 if missing else 0


def norm(s):
    """Letters only, lowercase, accents dropped - so Tayô matches tayo."""
    s = unicodedata.normalize("NFKD", s)
    s = "".join(c for c in s if not unicodedata.combining(c))
    return re.sub(r"[^a-z]", "", s.lower())


def trim(seg):
    """Cut to the sound, measured against this clip's own peak."""
    peak = float(np.max(np.abs(seg))) if len(seg) else 0.0
    if peak <= 0:
        return seg
    loud = np.nonzero(np.abs(seg) > peak * TRIM_REL)[0]
    if not len(loud):
        return seg
    head = max(0, loud[0] - int(0.010 * 44100))
    tail = min(len(seg), loud[-1] + int(0.040 * 44100))
    return seg[head:tail]


def level(seg):
    """To -20 dBFS RMS, with a ceiling so a hard consonant cannot clip."""
    if not len(seg):
        return seg
    rms = float(np.sqrt(np.mean(np.square(seg)))) or 1e-9
    peak = float(np.max(np.abs(seg))) or 1e-9
    gain = min((10.0 ** (TARGET_DBFS / 20.0)) / rms, 0.891 / peak)
    return seg * gain


def write(seg, rate, dst):
    y = np.clip(np.rint(seg * 32768.0), -32768, 32767).astype("<i2")
    with wave.open(dst, "wb") as w:
        w.setnchannels(1)
        w.setsampwidth(2)
        w.setframerate(rate)
        w.writeframes(y.tobytes())


def transcribe(key, path, language):
    """One file in, words with timings out."""
    fields = {"model_id": "scribe_v1", "language_code": language,
              "timestamps_granularity": "word"}
    boundary = uuid.uuid4().hex
    sep = ("--" + boundary).encode("utf-8")
    out = []
    for name, value in fields.items():
        out += [sep,
                ('Content-Disposition: form-data; name="%s"' % name).encode("utf-8"),
                b"", str(value).encode("utf-8")]
    mime = mimetypes.guess_type(path)[0] or "application/octet-stream"
    out += [sep,
            ('Content-Disposition: form-data; name="file"; filename="%s"'
             % os.path.basename(path)).encode("utf-8"),
            ("Content-Type: %s" % mime).encode("utf-8"),
            b"", open(path, "rb").read()]
    out += [("--" + boundary + "--").encode("utf-8"), b""]

    req = urllib.request.Request(
        STT, data=b"\r\n".join(out), method="POST",
        headers={"xi-api-key": key,
                 "Content-Type": "multipart/form-data; boundary=" + boundary})
    with urllib.request.urlopen(req, timeout=600) as r:
        return json.load(r)


if __name__ == "__main__":
    sys.exit(main())
