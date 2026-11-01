"""Bring a library music track into the game, and record where it came from.

The soundtrack does not have to be generated. A CC0 or CC-BY track from a
library is free, instant, and - for a capstone that has to say where every
asset came from - easier to defend than an AI generation. This script does the
same job Tools/generate_music.py does from the API end: fetch, convert, make
it loop, set the level, install it under the name MusicPlayer looks for.

What it will not do is let the licence go unrecorded. Every import appends to
Assets/Resources/Music/CREDITS.txt, which is committed, so the attribution a
CC-BY track needs exists before anyone has to remember it. A CC0 track needs
no attribution and is recorded anyway - "we checked, and it is public domain"
is the answer a panel actually wants.

Run from the repository root:

    python3 Tools/import_music.py --list
    python3 Tools/import_music.py music_bahay <url> --title "..." --author "..." --licence CC0 --source <page-url>

Plain python3, not `python3 -I`: that flag drops the user site directory,
where numpy lives.
"""
import argparse
import os
import sys
import urllib.request

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from generate_music import OUT, STAGE, ROOT, TRACKS, decode, finish  # noqa: E402

CREDITS = os.path.join(OUT, "CREDITS.txt")

HEADER = """SALINLAHI - background music credits
====================================

Every track in Assets/Resources/Music, where it came from, and the licence it
is used under. Written by Tools/import_music.py; do not edit by hand, re-run
the import instead.

A CC-BY track MUST be credited in the game's own credits screen as well as
here - this file is the record, not the attribution.

"""


def main():
    ap = argparse.ArgumentParser(description=__doc__,
                                 formatter_class=argparse.RawDescriptionHelpFormatter)
    ap.add_argument("name", nargs="?", help="track id, e.g. music_bahay")
    ap.add_argument("url", nargs="?", help="direct URL of the .ogg/.mp3/.wav")
    ap.add_argument("--title", default="")
    ap.add_argument("--author", default="")
    ap.add_argument("--licence", default="", help="CC0, CC-BY 4.0, ...")
    ap.add_argument("--source", default="", help="the page it was found on")
    ap.add_argument("--no-loop", action="store_true",
                    help="for the results sting, which must not loop")
    ap.add_argument("--list", action="store_true", help="what the game asks for")
    args = ap.parse_args()

    if args.list:
        for name, seconds, _ in TRACKS:
            have = os.path.exists(os.path.join(OUT, name + ".wav"))
            print("%-16s %3d s  %s" % (name, seconds, "present" if have else "-"))
        return 0

    if not args.name or not args.url:
        sys.exit("Need a track id and a URL. --list shows the ids.")

    known = {t[0] for t in TRACKS}
    if args.name not in known:
        sys.exit("Unknown track %r. Known: %s" % (args.name, ", ".join(sorted(known))))
    if not args.licence:
        sys.exit("Refusing to import without --licence. An untracked licence is "
                 "the one thing that cannot be fixed later.")

    os.makedirs(STAGE, exist_ok=True)
    os.makedirs(OUT, exist_ok=True)

    ext = os.path.splitext(args.url.split("?")[0])[1].lower() or ".mp3"
    raw = os.path.join(STAGE, args.name + ext)
    fetch(args.url, raw)

    wav = os.path.join(STAGE, args.name + ".wav")
    if ext == ".wav":
        decode(raw, wav)          # still normalise the format through afconvert
    else:
        decode(raw, wav)

    finish(wav, loop=not args.no_loop)

    dst = os.path.join(OUT, args.name + ".wav")
    os.replace(wav, dst)
    print("  -> %s" % os.path.relpath(dst, ROOT))

    credit(args)
    return 0


def fetch(url, dst):
    req = urllib.request.Request(url, headers={"User-Agent": "SALINLAHI-import/1.0"})
    with urllib.request.urlopen(req, timeout=120) as r:
        data = r.read()
    if len(data) < 2048:
        sys.exit("  That URL returned %d bytes - not an audio file." % len(data))
    tmp = dst + ".part"
    with open(tmp, "wb") as f:
        f.write(data)
        f.flush()
        os.fsync(f.fileno())
    os.replace(tmp, dst)
    print("  %.1f KB from %s" % (len(data) / 1024.0, url.split("/")[2])) 


def credit(args):
    lines = []
    if os.path.exists(CREDITS):
        lines = [l for l in open(CREDITS, encoding="utf-8").read().splitlines()]
        # drop any previous entry for this track, so a re-import replaces it
        out, skip = [], False
        for l in lines:
            if l.startswith(args.name + " "):
                skip = True
            elif skip and (l.startswith("  ") or not l.strip()):
                continue
            else:
                skip = False
            if not skip:
                out.append(l)
        body = "\n".join(out).rstrip() + "\n\n"
    else:
        body = HEADER

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
