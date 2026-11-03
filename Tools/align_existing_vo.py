#!/usr/bin/env python3
"""
Gives a mouth track to a recording that already exists.

Tools/generate_voiceover.py asks ElevenLabs to SPEAK a line and gets the
timings back in the same reply, so anything it makes is lip-synced for free.
The first batch of clips predates that, and it shows: Level 1 Scene 1 - the
very first scene a child plays - has 19 clips with no track at all, so Teacher
Rachel's mouth falls back to a timer for the whole opening. She moves while
she talks instead of moving WITH what she says, which is the thing the tracks
exist to stop.

Regenerating those 19 would fix it and cost a few thousand characters, but it
would also hand back nineteen NEW readings - different pacing, different
stress on the Filipino words, and a fresh chance for "tayo" to come out wrong
again. The audio is fine. Only the timings are missing.

So this aligns the audio that is already there. ElevenLabs' forced-alignment
endpoint takes a recording plus the words it contains and returns when each
character was spoken, which is the same shape the TTS endpoint hands back and
feeds the same mouth_track(). Nothing is re-voiced and no clip is touched.

It is also the tool for the Filipino words once they are recorded: a human
reading into a phone has no alignment either, and this is how it gets one.

    python3 Tools/align_existing_vo.py --dry-run     # what it would align
    python3 Tools/align_existing_vo.py               # every clip missing one
    python3 Tools/align_existing_vo.py --ids-from untracked.txt
"""

import argparse
import json
import mimetypes
import os
import sys
import urllib.error
import urllib.request
import uuid

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import generate_voiceover as vo

ALIGN = "https://api.elevenlabs.io/v1/forced-alignment"


def main():
    ap = argparse.ArgumentParser(description=__doc__,
                                 formatter_class=argparse.RawDescriptionHelpFormatter)
    ap.add_argument("--dry-run", action="store_true",
                    help="List what would be aligned. No key is read and nothing is billed.")
    ap.add_argument("--ids-from", metavar="FILE",
                    help="A file of line ids, one per line, instead of every untracked clip.")
    args = ap.parse_args()

    # vo.load() already fills the {word} hole in a prompt - "Say {word} to
    # wake him up!" - with the line's target word, which is what these clips
    # actually say. Aligning the unfilled text would shift every timing after
    # the hole.
    rows = {r["id"]: r for r in vo.load()}
    tracked = read_tracks()

    if args.ids_from:
        wanted = [l.strip() for l in open(args.ids_from, encoding="utf-8")
                  if l.strip() and not l.startswith("#")]
    else:
        wanted = sorted(name for name in rows
                        if name not in tracked and os.path.exists(clip_path(name)))

    jobs, skipped = [], []
    for name in wanted:
        row = rows.get(name)
        if row is None:
            skipped.append((name, "not in the script"))
            continue
        path = clip_path(name)
        if not os.path.exists(path):
            skipped.append((name, "no recording yet"))
            continue
        text = (row.get("say") or "").strip()
        if not text:
            skipped.append((name, "no text to align against"))
            continue
        jobs.append((name, path, text))

    for name, why in skipped:
        print("skip  %-30s %s" % (name, why))
    if not jobs:
        print("Nothing to align - every recording already has a mouth track.")
        return 0

    print("\n%d clip(s) to align:" % len(jobs))
    for name, _, text in jobs:
        print("  %-30s %s" % (name, text[:58] + ("..." if len(text) > 58 else "")))

    if args.dry_run:
        print("\n--dry-run: no key was read, nothing was sent and nothing was billed.")
        return 0

    key = vo.read_key()
    if key is None:
        return 1

    done, failed = {}, []
    for name, path, text in jobs:
        try:
            track = vo.mouth_track(align(key, path, text))
        except urllib.error.HTTPError as e:
            failed.append((name, "HTTP %s %s" % (e.code, e.reason)))
            continue
        except (urllib.error.URLError, ValueError, OSError) as e:
            failed.append((name, str(e)))
            continue

        if not track:
            failed.append((name, "aligned, but produced no mouth changes"))
            continue
        done[name] = track
        print("  %-30s %d mouth changes" % (name, len(track)))

    # Every problem, not the first one - the same way check_voiceover_script.py
    # reports, so one run tells you everything that needs a second look.
    for name, why in failed:
        sys.stderr.write("FAILED %-28s %s\n" % (name, why))

    if done:
        vo.save_tracks(done)
        print("\nNow press Play in Unity - Resources reload on their own, so "
              "there is no menu item to run for this one.")
    return 1 if failed else 0


def read_tracks():
    if not os.path.exists(vo.TRACKS):
        return {}
    try:
        return json.load(open(vo.TRACKS, encoding="utf-8")).get("tracks", {})
    except ValueError:
        return {}


def clip_path(name):
    return os.path.join(vo.OUT, name + ".wav")


def align(key, path, text):
    """
    Posts one recording and its words, and returns the character timings in
    the shape mouth_track() reads.

    The TTS endpoint hands back three parallel lists; this one hands back a
    list of objects. Same information, so it is reshaped here rather than
    teaching mouth_track() two formats.
    """
    body, content_type = multipart({"text": text}, "file", path)
    req = urllib.request.Request(ALIGN, data=body, method="POST",
                                 headers={"xi-api-key": key, "Content-Type": content_type})
    with urllib.request.urlopen(req, timeout=300) as r:
        payload = json.load(r)

    chars = payload.get("characters") or []
    return {
        "characters": [c.get("text", "") for c in chars],
        "character_start_times_seconds": [float(c.get("start", 0.0)) for c in chars],
        "character_end_times_seconds": [float(c.get("end", 0.0)) for c in chars],
    }


def multipart(fields, file_field, path):
    """One file and some text fields, hand-built so there is no dependency."""
    boundary = uuid.uuid4().hex
    sep = ("--" + boundary).encode("utf-8")
    out = []

    for name, value in fields.items():
        out.append(sep)
        out.append(('Content-Disposition: form-data; name="%s"' % name).encode("utf-8"))
        out.append(b"")
        out.append(str(value).encode("utf-8"))

    mime = mimetypes.guess_type(path)[0] or "application/octet-stream"
    out.append(sep)
    out.append(('Content-Disposition: form-data; name="%s"; filename="%s"'
                % (file_field, os.path.basename(path))).encode("utf-8"))
    out.append(("Content-Type: %s" % mime).encode("utf-8"))
    out.append(b"")
    out.append(open(path, "rb").read())

    out.append(("--" + boundary + "--").encode("utf-8"))
    out.append(b"")
    return b"\r\n".join(out), "multipart/form-data; boundary=" + boundary


if __name__ == "__main__":
    sys.exit(main())
