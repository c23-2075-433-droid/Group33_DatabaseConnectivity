#!/usr/bin/env python3
"""
Speaks a line in English and puts the Filipino word back in her voice.

Every line left unvoiced has a Filipino word in it, and that is the whole
problem. Generating the line gives good English and a mangled Filipino word -
"tayo" (us) comes out "taYO" (get up), which is a different word, taught wrong
to a child who cannot yet read the caption. Recording the line instead means
recording four hundred of them.

The narrator can already splice: SpeakAsk splits a prompt at {word} and plays
word_<word>.wav in the gap. But the halves either side of the gap have no
audio - MakePart(null, before) - so a spliced prompt is her word with silence
around it. Fixing that needs a clip per fragment, and a fragment generated on
its own has the intonation of a fragment: "Let's help Kylo out of bed! Say"
trails off where it should lean forward.

So generate the WHOLE line, word and all, and then replace the word. The
intonation is the intonation of a complete sentence, because it was one. The
timestamped endpoint returns where every character landed, which is exactly
where to cut, and costs the same as the plain one.

The mouth track is rebuilt rather than re-requested: the changes before the
word are already right, her word's own track is dropped in at the cut, and
everything after shifts by the difference in length.

    python3 Tools/splice_word_lines.py --dry-run
    python3 Tools/splice_word_lines.py --limit 3     # try a few first
    python3 Tools/splice_word_lines.py
"""

import argparse
import base64
import json
import os
import re
import subprocess
import sys
import urllib.error
import urllib.request
import wave

import numpy as np

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import generate_voiceover as vo

STAGE = os.path.join(vo.ROOT, "Recordings", "spliced")

# Long enough to hide the join, short enough not to eat the consonant that
# starts her word.
FADE = 0.015

# A gap this far either side of the swap, so the word does not run into the
# English on either side the way two tracks butted together do.
GAP = 0.04


def main():
    ap = argparse.ArgumentParser(description=__doc__,
                                 formatter_class=argparse.RawDescriptionHelpFormatter)
    ap.add_argument("--dry-run", action="store_true",
                    help="List what would be spoken. No key is read, nothing is billed.")
    ap.add_argument("--limit", type=int, help="Only the first N lines. Try a few first.")
    ap.add_argument("--ids-from", metavar="FILE", help="A file of line ids, one per line.")
    ap.add_argument("--voice", help="Voice id. Default: the one cloned voice on the account.")
    args = ap.parse_args()

    rows = {r["id"]: r for r in vo.load()}
    have = recorded()
    tracks = read_tracks()

    words = sorted({(r.get("word") or "").strip() for r in rows.values()
                    if (r.get("word") or "").strip()}, key=len, reverse=True)
    finder = re.compile(r"\b(" + "|".join(re.escape(w) for w in words) + r")\b", re.I)

    if args.ids_from:
        wanted = [l.strip() for l in open(args.ids_from, encoding="utf-8")
                  if l.strip() and not l.startswith("#")]
    else:
        wanted = [i for i in rows if i not in have]

    jobs, skipped = [], []
    for line_id in sorted(wanted):
        row = rows.get(line_id)
        if row is None:
            skipped.append((line_id, "not in the script"))
            continue
        text = (row.get("say") or "").strip()
        if not text:
            skipped.append((line_id, "no text"))
            continue
        m = finder.search(text)
        if not m:
            skipped.append((line_id, "no Filipino word - plain generation will do"))
            continue
        said = m.group(1)
        clip = os.path.join(vo.OUT, "word_" + said.lower() + ".wav")
        if not os.path.exists(clip):
            skipped.append((line_id, "no recording of '%s' yet" % said))
            continue
        jobs.append((line_id, text, said, m.start(1), m.end(1), clip))

    for line_id, why in skipped:
        print("skip  %-32s %s" % (line_id, why))
    if args.limit:
        jobs = jobs[:args.limit]
    if not jobs:
        print("\nNothing to splice.")
        return 0

    print("\n%d line(s) to speak and splice:" % len(jobs))
    for line_id, text, said, _, _, _ in jobs:
        print("  %-32s [%s] %s" % (line_id, said, text[:46] + ("..." if len(text) > 46 else "")))
    chars = sum(len(t) for _, t, _, _, _, _ in jobs)
    print("\n%d characters -> about %d credits" % (chars, chars))

    if args.dry_run:
        print("\n--dry-run: no key was read, nothing was sent and nothing was billed.")
        return 0

    key = vo.read_key()
    if key is None:
        return 1
    voice = args.voice or sole_cloned_voice(key)
    if voice is None:
        return 1

    os.makedirs(STAGE, exist_ok=True)
    made, failed = {}, []
    for line_id, text, said, a, b, clip in jobs:
        try:
            audio, alignment = speak(key, voice, text)
        except urllib.error.HTTPError as e:
            failed.append((line_id, "HTTP %s %s" % (e.code, e.reason)))
            continue
        except (urllib.error.URLError, ValueError) as e:
            failed.append((line_id, str(e)))
            continue

        gen = decode(audio, os.path.join(STAGE, line_id + ".gen.wav"))
        span = char_span(alignment, a, b)
        if span is None:
            failed.append((line_id, "could not find '%s' in the alignment" % said))
            continue

        hers = read_wav(clip)
        out, shift = swap(gen, span, hers)
        track = retime(vo.mouth_track(alignment), span,
                       tracks.get("word_" + said.lower()), shift)

        write_wav(out, os.path.join(vo.OUT, line_id + ".wav"))
        made[line_id] = track
        print("  %-32s %.2fs  (%s at %.2fs)" % (line_id, len(out) / 44100.0, said, span[0]))

    # Every problem rather than the first, the way check_voiceover_script.py
    # reports - one run should say everything that needs a second look.
    for line_id, why in failed:
        sys.stderr.write("FAILED %-30s %s\n" % (line_id, why))

    if made:
        vo.save_tracks(made)
        print("\n%d line(s) -> %s" % (len(made), os.path.relpath(vo.OUT, vo.ROOT)))
        print("Now run Tools > SALINLAHI > Refresh Narrator Voice Bank in Unity.")
    return 1 if failed else 0


def recorded():
    return {os.path.splitext(f)[0] for f in os.listdir(vo.OUT) if f.endswith(".wav")} \
        if os.path.isdir(vo.OUT) else set()


def read_tracks():
    if not os.path.exists(vo.TRACKS):
        return {}
    try:
        return json.load(open(vo.TRACKS, encoding="utf-8")).get("tracks", {})
    except ValueError:
        return {}


def sole_cloned_voice(key):
    req = urllib.request.Request("https://api.elevenlabs.io/v1/voices",
                                 headers={"xi-api-key": key})
    try:
        voices = json.load(urllib.request.urlopen(req, timeout=30)).get("voices", [])
    except (urllib.error.URLError, ValueError) as e:
        sys.stderr.write("Could not list the voices: %s\n" % e)
        return None
    cloned = [v for v in voices if v.get("category") == "cloned"]
    if len(cloned) == 1:
        print("voice: %s" % cloned[0].get("name"))
        return cloned[0].get("voice_id")
    sys.stderr.write("Found %d cloned voices - pass --voice <id>.\n" % len(cloned))
    return None


def speak(key, voice, text):
    """The whole line, with the timings of every character in it."""
    body = json.dumps({
        "text": text,
        "model_id": vo.MODEL,
        "language_code": vo.LANGUAGE,
        "voice_settings": {"stability": 0.5, "similarity_boost": 0.75,
                           "style": 0.0, "use_speaker_boost": True},
    }).encode()
    req = urllib.request.Request(
        vo.TTS + voice + "/with-timestamps?output_format=mp3_44100_128",
        data=body, headers={"xi-api-key": key, "Content-Type": "application/json"})
    with urllib.request.urlopen(req, timeout=300) as r:
        payload = json.load(r)
    return base64.b64decode(payload["audio_base64"]), (payload.get("alignment") or {})


def char_span(alignment, a, b):
    """When the characters between a and b were spoken."""
    starts = alignment.get("character_start_times_seconds") or []
    ends = alignment.get("character_end_times_seconds") or []
    if not starts or b > len(starts):
        return None
    return float(starts[a]), float(ends[b - 1])


def swap(gen, span, hers):
    """
    Her word in place of the generated one, crossfaded at both joins.

    Her clip is matched to the loudness of the line it is going into rather
    than left at its own level: both were normalised, but to their own
    content, and a single word normalised alone sits louder than the same word
    inside a sentence.
    """
    rate = 44100
    a = max(0, int(span[0] * rate))
    b = min(len(gen), int(span[1] * rate))

    head, tail = gen[:a], gen[b:]
    spoken = gen[a:b]
    if len(spoken):
        here = float(np.sqrt(np.mean(np.square(spoken)))) or 1e-9
        mine = float(np.sqrt(np.mean(np.square(hers)))) or 1e-9
        hers = hers * min(here / mine, 4.0)

    gap = np.zeros(int(GAP * rate), dtype=np.float32)
    fade = int(FADE * rate)
    out = concat([head, gap, hers, gap, tail], fade)
    peak = float(np.max(np.abs(out))) or 1e-9
    if peak > 0.95:
        out = out * (0.95 / peak)

    # Four joins, each one overlapping FADE seconds, so the piece is shorter
    # than the sum of its parts by that much. Measured and confirmed: her clip
    # lands 10 ms after the cut, which is GAP less two fades.
    added = (len(gap) * 2 + len(hers) - 4 * fade) / float(rate)
    return out, added - (span[1] - span[0])


def concat(pieces, fade):
    """Join with a short linear crossfade so no edge clicks."""
    out = pieces[0].astype(np.float32)
    for piece in pieces[1:]:
        piece = piece.astype(np.float32)
        n = min(fade, len(out), len(piece))
        if n <= 0:
            out = np.concatenate([out, piece])
            continue
        ramp = np.linspace(0.0, 1.0, n, dtype=np.float32)
        joined = out[-n:] * (1.0 - ramp) + piece[:n] * ramp
        out = np.concatenate([out[:-n], joined, piece[n:]])
    return out


def retime(track, span, word_track, shift):
    """
    The line's mouth changes, with her word's dropped in at the cut.

    Everything before the word is untouched, the word's own shapes are laid in
    where it now sits, and everything after moves by the difference between
    the two words' lengths.
    """
    start, end = span
    out = [c for c in track if c[0] < start - 1e-6]
    at = start + GAP - 2 * FADE        # where her word actually begins
    for t, mouth in (word_track or [[0.0, "E"]]):
        out.append([round(at + float(t), 3), mouth])
    out.append([round(end + shift, 3), "Idle"])
    for t, mouth in track:
        if t > end + 1e-6:
            out.append([round(float(t) + shift, 3), mouth])

    clean = []
    for t, mouth in out:
        if clean and clean[-1][1] == mouth:
            continue
        if clean and t - clean[-1][0] < 0.04:
            clean[-1] = [t, mouth]
        else:
            clean.append([t, mouth])
    return clean


def decode(audio, tmp):
    mp3 = tmp + ".mp3"
    open(mp3, "wb").write(audio)
    subprocess.run(["/usr/bin/afconvert", "-f", "WAVE", "-d", "LEI16@44100", "-c", "1",
                    mp3, tmp], check=True, capture_output=True)
    os.remove(mp3)
    x = read_wav(tmp)
    os.remove(tmp)
    return x


def read_wav(path):
    with wave.open(path, "rb") as w:
        n = w.getnframes()
        return np.frombuffer(w.readframes(n), dtype="<i2").astype(np.float32) / 32768.0


def write_wav(x, path):
    y = np.clip(np.rint(x * 32768.0), -32768, 32767).astype("<i2")
    with wave.open(path, "wb") as w:
        w.setnchannels(1)
        w.setsampwidth(2)
        w.setframerate(44100)
        w.writeframes(y.tobytes())


if __name__ == "__main__":
    sys.exit(main())
