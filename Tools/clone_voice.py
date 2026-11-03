"""Clone a voice for Teacher Rachel with ElevenLabs Instant Voice Cloning.

Why clone rather than pick. Rachel narrates in English, but she says the
Filipino words, and those three clips are the pronunciation model a
five-year-old copies. Every premade voice on this account is American or
British, and an English-leaning voice misplaces the stress in Bangon and Tayo
in a way that is still intelligible - which is exactly what makes it easy to
approve by mistake. A Filipino speaker's clone gets it right for free.

CONSENT IS NOT A FORMALITY. Clone your own voice, or one whose owner has
agreed in writing and knows what it is for. In the Philippines a voice is
personal data under the Data Privacy Act, and there is civil exposure for use
of likeness on top of the terms you accept here. Keep the permission with the
project.

Run from the repository root:

    python3 Tools/clone_voice.py Recordings/voice_sample.m4a --name "Teacher Rachel"
    python3 Tools/clone_voice.py --list

It prints the new voice id. Put that in the generator:

    python3 Tools/generate_voiceover.py --scene l1_s1 --voice <id>
"""
import argparse
import json
import mimetypes
import os
import sys
import urllib.request
import uuid

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
KEY_FILE = os.path.join(ROOT, "Tools", "elevenlabs.key")
ADD = "https://api.elevenlabs.io/v1/voices/add"
VOICES = "https://api.elevenlabs.io/v1/voices"
SUBSCRIPTION = "https://api.elevenlabs.io/v1/user/subscription"


def main():
    ap = argparse.ArgumentParser(description=__doc__,
                                 formatter_class=argparse.RawDescriptionHelpFormatter)
    ap.add_argument("sample", nargs="*", help="one or more recordings of the same speaker")
    ap.add_argument("--name", default="Teacher Rachel")
    ap.add_argument("--description", default="Warm Filipina teacher narrating a children's "
                                             "vocabulary game in English, speaking Filipino words.")
    ap.add_argument("--list", action="store_true", help="show the voices on this account")
    args = ap.parse_args()

    key = read_key()
    if key is None:
        return 2

    if args.list:
        for v in get(VOICES, key)["voices"]:
            print("%-28s %-10s %s" % (v["name"][:28], v.get("category", ""), v["voice_id"]))
        return 0

    if not args.sample:
        sys.exit("Need at least one recording. See Recordings/voice_sample_script.txt")

    sub = get(SUBSCRIPTION, key)
    if not sub.get("can_use_instant_voice_cloning"):
        sys.exit("This plan (%s) does not allow instant voice cloning." % sub.get("tier"))
    used, limit = sub.get("voice_slots_used", 0), sub.get("voice_limit", 0)
    print("tier %s, %d of %d voice slots used" % (sub.get("tier"), used, limit))
    if limit and used >= limit:
        sys.exit("No voice slots left. Delete one at elevenlabs.io/app/voice-lab.")

    for p in args.sample:
        if not os.path.exists(p):
            sys.exit("No such file: " + p)
        mb = os.path.getsize(p) / 1e6
        print("  %-40s %.1f MB" % (os.path.relpath(p, ROOT), mb))
        if mb > 10:
            print("    (large; ElevenLabs takes up to ~10 MB per file)")

    body, ctype = multipart({"name": args.name, "description": args.description},
                            args.sample)
    req = urllib.request.Request(ADD, data=body,
                                 headers={"xi-api-key": key, "Content-Type": ctype})
    try:
        with urllib.request.urlopen(req, timeout=600) as r:
            out = json.load(r)
    except urllib.error.HTTPError as e:
        sys.exit("HTTP %d: %s" % (e.code, e.read().decode("utf-8", "replace")[:400]))

    vid = out.get("voice_id")
    print("\nCloned. voice id: %s" % vid)
    print("\nNext:\n  python3 Tools/generate_voiceover.py --scene l1_s1 --voice %s" % vid)
    return 0


def read_key():
    key = os.environ.get("ELEVENLABS_API_KEY", "").strip()
    src = "$ELEVENLABS_API_KEY"
    if not key and os.path.exists(KEY_FILE):
        src = os.path.relpath(KEY_FILE, ROOT)
        for line in open(KEY_FILE, encoding="utf-8"):
            if line.strip() and not line.startswith("#"):
                key = line.strip()
                break
    if not key:
        sys.stderr.write("[SALINLAHI] No ElevenLabs API key. "
                         "See Tools/elevenlabs.key.example.txt\n")
        return None
    print("key: %s" % src)
    return key


def get(url, key):
    return json.load(urllib.request.urlopen(
        urllib.request.Request(url, headers={"xi-api-key": key}), timeout=30))


def multipart(fields, paths):
    """A minimal multipart body - urllib has no form encoder and this needs no
    dependency to be added to a Tools folder that has none."""
    boundary = uuid.uuid4().hex
    out = b""
    for k, v in fields.items():
        out += (("--%s\r\nContent-Disposition: form-data; name=\"%s\"\r\n\r\n%s\r\n"
                 % (boundary, k, v)).encode("utf-8"))
    for p in paths:
        mime = mimetypes.guess_type(p)[0] or "application/octet-stream"
        out += ("--%s\r\nContent-Disposition: form-data; name=\"files\"; filename=\"%s\"\r\n"
                "Content-Type: %s\r\n\r\n" % (boundary, os.path.basename(p), mime)).encode("utf-8")
        out += open(p, "rb").read() + b"\r\n"
    out += ("--%s--\r\n" % boundary).encode("utf-8")
    return out, "multipart/form-data; boundary=" + boundary


if __name__ == "__main__":
    sys.exit(main())
