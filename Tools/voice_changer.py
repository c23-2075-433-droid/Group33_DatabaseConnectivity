#!/usr/bin/env python3
"""
Re-renders a recording in Teacher Rachel's cloned voice, keeping the delivery.

The Filipino words have to be said by a person. Six attempts failed to make
the text-to-speech model put the stress in the right place - "tayo" (us) kept
coming out as "taYO" (get up) - and a cloned voice getting Filipino stress
wrong sounds worse than a stranger getting it wrong, because the listener
believes it.

But a phone recording spliced into a generated English sentence does not sit
right either. The generated half is studio-clean at a fixed level; the recorded
half carries a room, a different microphone and a different distance. The join
lands in the middle of a sentence, which is exactly where an ear notices.

ElevenLabs' voice changer closes that gap. It takes the recording and renders
it again in the cloned voice while keeping the original's timing, stress and
delivery - the docs put it as "the audio file which holds the content and
emotion that will control the generated speech". The stress stays hers; the
rendering matches the generated lines around it.

Nothing is overwritten by default. Converted clips land in Recordings/changed/
so both versions can be listened to side by side, because which one sounds
right is a judgement only a Filipino speaker can make. --apply is the step that
replaces the clips in the game, and it keeps the originals in Recordings/raw/.

    python3 Tools/voice_changer.py --dry-run        # what it would convert
    python3 Tools/voice_changer.py --words          # convert, write side by side
    python3 Tools/voice_changer.py --words --apply  # replace the game's clips
"""

import argparse
import glob
import json
import mimetypes
import os
import shutil
import subprocess
import sys
import urllib.error
import urllib.request
import uuid

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import generate_voiceover as vo

CHANGER = "https://api.elevenlabs.io/v1/voice-changer/"
VOICES = "https://api.elevenlabs.io/v1/voices"
VOICE_FILE = os.path.join(vo.ROOT, "Tools", "elevenlabs.voice")
CHANGED = os.path.join(vo.ROOT, "Recordings", "changed")
RAW = os.path.join(vo.ROOT, "Recordings", "raw")

# Multilingual, because the input is Filipino. The endpoint's own default is
# eleven_english_sts_v2, which is the wrong model for every clip this tool
# exists to convert.
MODEL = "eleven_multilingual_sts_v2"


def main():
    ap = argparse.ArgumentParser(description=__doc__,
                                 formatter_class=argparse.RawDescriptionHelpFormatter)
    ap.add_argument("--words", action="store_true",
                    help="Every word_*.wav in Assets/Audio/VO.")
    ap.add_argument("--ids-from", metavar="FILE",
                    help="A file of clip ids, one per line.")
    ap.add_argument("--apply", action="store_true",
                    help="Replace the game's clips. Originals are kept in Recordings/raw.")
    ap.add_argument("--dry-run", action="store_true",
                    help="List what would be converted. No key is read and nothing is billed.")
    ap.add_argument("--voice", help="Voice id. Default: the one cloned voice on the account.")
    ap.add_argument("--keep-noise", action="store_true",
                    help="Skip background-noise removal. On by default - these are phone recordings.")
    args = ap.parse_args()

    if args.ids_from:
        ids = [l.strip() for l in open(args.ids_from, encoding="utf-8")
               if l.strip() and not l.startswith("#")]
    elif args.words:
        ids = sorted(os.path.splitext(os.path.basename(p))[0]
                     for p in glob.glob(os.path.join(vo.OUT, "word_*.wav")))
    else:
        return ap.error("Nothing selected. Pass --words or --ids-from.")

    jobs, skipped = [], []
    for name in ids:
        path = os.path.join(vo.OUT, name + ".wav")
        (jobs if os.path.exists(path) else skipped).append((name, path))

    for name, _ in skipped:
        print("skip  %-28s no recording in Assets/Audio/VO" % name)
    if not jobs:
        print("Nothing to convert.")
        return 0

    print("\n%d clip(s) to convert:" % len(jobs))
    for name, _ in jobs:
        print("  " + name)
    print("\n%s" % ("REPLACING the game's clips (originals -> Recordings/raw)"
                    if args.apply else
                    "writing side by side into Recordings/changed, nothing replaced"))

    if args.dry_run:
        print("\n--dry-run: no key was read, nothing was sent and nothing was billed.")
        return 0

    key = vo.read_key()
    if key is None:
        return 1
    voice = pick_voice(key, args.voice)
    if voice is None:
        return 1

    os.makedirs(CHANGED, exist_ok=True)
    done, failed = [], []
    for name, path in jobs:
        dst = os.path.join(CHANGED, name + ".wav")
        try:
            convert(key, voice, path, dst, not args.keep_noise)
        except urllib.error.HTTPError as e:
            failed.append((name, "HTTP %s %s" % (e.code, e.reason)))
            continue
        except (urllib.error.URLError, OSError, ValueError) as e:
            failed.append((name, str(e)))
            continue
        print("  %-28s" % name, end=" ")
        vo.shape(dst)
        done.append((name, path, dst))

    # Every problem rather than the first, the way check_voiceover_script.py
    # reports - one run should tell you everything that needs a second look.
    for name, why in failed:
        sys.stderr.write("FAILED %-26s %s\n" % (name, why))

    if args.apply and done:
        os.makedirs(RAW, exist_ok=True)
        for name, path, dst in done:
            keep = os.path.join(RAW, name + ".wav")
            if not os.path.exists(keep):
                shutil.copy2(path, keep)
            shutil.copy2(dst, path)
        print("\n%d clip(s) replaced. Originals in %s"
              % (len(done), os.path.relpath(RAW, vo.ROOT)))
        print("Mouth timings changed with the audio - run:")
        print("  python3 Tools/align_existing_vo.py --ids-from <the same ids>")
    elif done:
        print("\n%d clip(s) in %s. Listen to both, then re-run with --apply."
              % (len(done), os.path.relpath(CHANGED, vo.ROOT)))

    return 1 if failed else 0


def pick_voice(key, given):
    """
    The voice id, from the argument, the file beside the key, or the account.

    Looking it up is the useful case: there is one cloned voice on this
    account and typing a twenty-character id by hand invites a silent mistake,
    because a wrong-but-valid id returns perfectly good audio in a stranger's
    voice.
    """
    if given:
        return given
    if os.path.exists(VOICE_FILE):
        for line in open(VOICE_FILE, encoding="utf-8"):
            if line.strip() and not line.startswith("#"):
                return line.strip()

    req = urllib.request.Request(VOICES, headers={"xi-api-key": key})
    try:
        voices = json.load(urllib.request.urlopen(req, timeout=30)).get("voices", [])
    except (urllib.error.URLError, ValueError) as e:
        sys.stderr.write("Could not list the voices: %s\n" % e)
        return None

    cloned = [v for v in voices if v.get("category") == "cloned"]
    if len(cloned) == 1:
        print("voice: %s (the one cloned voice on the account)" % cloned[0].get("name"))
        return cloned[0].get("voice_id")
    sys.stderr.write("Found %d cloned voices. Pass --voice <id>:\n" % len(cloned))
    for v in cloned:
        sys.stderr.write("  %-30s %s\n" % (v.get("name"), v.get("voice_id")))
    return None


def convert(key, voice, src, dst, denoise):
    """One recording in, the same delivery in Rachel's voice out."""
    fields = {"model_id": MODEL}
    if denoise:
        fields["remove_background_noise"] = "true"

    body, content_type = multipart(fields, "audio", src)
    req = urllib.request.Request(
        CHANGER + voice + "?output_format=mp3_44100_128",
        data=body, method="POST",
        headers={"xi-api-key": key, "Content-Type": content_type})
    with urllib.request.urlopen(req, timeout=300) as r:
        audio = r.read()

    mp3 = dst[:-4] + ".mp3"
    open(mp3, "wb").write(audio)
    # Mono 44.1 kHz 16-bit, matching every other clip in Assets/Audio/VO.
    subprocess.run(["/usr/bin/afconvert", "-f", "WAVE", "-d", "LEI16@44100", "-c", "1",
                    mp3, dst], check=True, capture_output=True)
    os.remove(mp3)


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
