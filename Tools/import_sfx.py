"""Bring a sound effect into the game, trimmed, levelled and credited.

The same job Tools/import_music.py does, with three differences that matter.
Effects are MONO - they are short transients in a 2D game and stereo doubles
the file for nothing. They are LOUDER than music, because a footstep is a
confirmation the child's word worked and has to be audible under Rachel. And
they are TRIMMED HARD: a footstep with 80 ms of silence in front of it fires
late, and five of those in a walk read as a limp.

Every import appends to Assets/Resources/Sfx/CREDITS.txt, which is committed.
A licence that is not written down when the file arrives is one nobody can
reconstruct later.

Run from the repository root:

    python3 Tools/import_sfx.py step_wood_1 <file-or-url> --licence CC0 \
        --title "..." --author "..." --source <page-url>

Plain python3, not `python3 -I`: that flag drops the user site directory,
where numpy lives.
"""
import argparse
import os
import subprocess
import sys
import urllib.request
import wave

import numpy as np

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
OUT = os.path.join(ROOT, "Assets", "Resources", "Sfx")
STAGE = os.path.join(ROOT, "Recordings", "sfx")
CREDITS = os.path.join(OUT, "CREDITS.txt")

# Louder than the music's -24: these are brief, and they are feedback rather
# than atmosphere.
TARGET_DBFS = -14.0

# Anything below this counts as silence at the ends.
FLOOR = 0.004

HEADER = """SALINLAHI - sound effect credits
================================

Every effect in Assets/Resources/Sfx, where it came from, and its licence.
Written by Tools/import_sfx.py; re-run the import rather than editing it.

A CC-BY effect must also be credited in the game's own credits screen.

"""


def main():
    ap = argparse.ArgumentParser(description=__doc__,
                                 formatter_class=argparse.RawDescriptionHelpFormatter)
    ap.add_argument("name", help="id the game loads it by, e.g. step_wood_1")
    ap.add_argument("src", help="local file, or a URL")
    ap.add_argument("--licence", required=True)
    ap.add_argument("--title", default="")
    ap.add_argument("--author", default="")
    ap.add_argument("--source", default="")
    ap.add_argument("--max-seconds", type=float, default=0.0,
                    help="fade out and cut at this length; 0 keeps it whole")
    args = ap.parse_args()

    os.makedirs(STAGE, exist_ok=True)
    os.makedirs(OUT, exist_ok=True)

    src = args.src
    if src.startswith("http"):
        local = os.path.join(STAGE, args.name + os.path.splitext(src.split("?")[0])[1])
        fetch(src, local)
        src = local
    elif not os.path.exists(src):
        sys.exit("No such file: " + src)

    wav = os.path.join(STAGE, args.name + ".wav")
    subprocess.run(["/usr/bin/afconvert", "-f", "WAVE", "-d", "LEI16@44100", "-c", "1",
                    src, wav], check=True, capture_output=True)

    shape(wav, args.max_seconds)

    dst = os.path.join(OUT, args.name + ".wav")
    os.replace(wav, dst)
    print("  -> %s" % os.path.relpath(dst, ROOT))
    credit(args)
    return 0


def fetch(url, dst):
    req = urllib.request.Request(url, headers={"User-Agent": "SALINLAHI-sfx/1.0"})
    with urllib.request.urlopen(req, timeout=120) as r:
        b = r.read()
    open(dst, "wb").write(b)
    print("  %.1f KB" % (len(b) / 1024.0))


def shape(path, max_seconds):
    with wave.open(path, "rb") as w:
        rate, frames = w.getframerate(), w.getnframes()
        x = np.frombuffer(w.readframes(frames), dtype="<i2").astype(np.float32) / 32768.0

    loud = np.nonzero(np.abs(x) > FLOOR)[0]
    if len(loud):
        head = max(0, loud[0] - int(0.005 * rate))      # keep 5 ms of attack
        tail = min(len(x), loud[-1] + int(0.030 * rate))
        x = x[head:tail]

    if max_seconds > 0 and len(x) > max_seconds * rate:
        keep = int(max_seconds * rate)
        fade = int(min(0.08 * rate, keep * 0.3))
        x = x[:keep].copy()
        x[-fade:] *= np.linspace(1.0, 0.0, fade, dtype=np.float32)

    rms = float(np.sqrt(np.mean(np.square(x)))) or 1e-9
    gain = (10.0 ** (TARGET_DBFS / 20.0)) / rms
    peak = float(np.max(np.abs(x))) or 1e-9
    gain = min(gain, 0.95 / peak)

    y = np.clip(np.rint(x * gain * 32768.0), -32768, 32767).astype("<i2")

    with wave.open(path, "wb") as w:
        w.setnchannels(1)
        w.setsampwidth(2)
        w.setframerate(rate)
        w.writeframes(y.tobytes())

    print("  %.3f s, mono %d Hz, peak %.1f dBFS"
          % (len(y) / float(rate), rate,
             20 * np.log10(max(float(np.max(np.abs(y)) / 32768.0), 1e-9))))


def credit(args):
    body = HEADER
    if os.path.exists(CREDITS):
        kept, skip = [], False
        for l in open(CREDITS, encoding="utf-8").read().splitlines():
            if l.startswith(args.name + " "):
                skip = True
            elif skip and (l.startswith("  ") or not l.strip()):
                continue
            else:
                skip = False
            if not skip:
                kept.append(l)
        body = "\n".join(kept).rstrip() + "\n\n"

    body += "%s  <- %s\n" % (args.name, args.title or "(untitled)")
    if args.author:
        body += "  by       %s\n" % args.author
    body += "  licence  %s\n" % args.licence
    if args.source:
        body += "  source   %s\n" % args.source
    body += "\n"
    open(CREDITS, "w", encoding="utf-8").write(body)
    print("  credited in %s" % os.path.relpath(CREDITS, ROOT))


if __name__ == "__main__":
    sys.exit(main())
