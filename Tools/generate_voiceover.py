"""Generate Teacher Rachel's voice-over with the ElevenLabs text-to-speech API.

Reads Docs/VoiceOverScript.csv - the same 434-row file the game and
Tools/check_voiceover_script.py read - and writes <id>.wav into
Assets/Audio/VO/, which is all NarratorVoiceBankTool needs to wire a line up.

Two details of this script that are not obvious:

  * {word} is SUBSTITUTED, not left in. Assets/Audio/VO/README.txt is explicit:
    unrecorded, the game plays the two halves around word_<word>.wav; recorded,
    "say the whole sentence INCLUDING the word". VoiceNarrator.SpeakAsk plays a
    recorded _ask clip whole for exactly that reason. The Target Word is sent
    as written - Bangon, not BANGON - because the capitals in the caption are
    a reading aid, and an all-caps word risks being read as an initialism.

  * Nothing reaches Assets/ until every selected line has succeeded. A failure
    at clip 17 leaves the project untouched rather than half-voiced.

Rachel narrates in ENGLISH by design (Docs/VoicePromptingDesign.md, "Language
policy - settled"); only the word_* clips are Filipino. Those three are the
pronunciation model a five-year-old copies, so they are worth a native
speaker's ear whatever voice is used for the rest.

Run from the repository root:

    python3 Tools/generate_voiceover.py --scene l1_s1 --dry-run
    python3 Tools/generate_voiceover.py --audition --voice <id> [--voice <id> ...]
    python3 Tools/generate_voiceover.py --scene l1_s1 --voice <id>

Plain python3, not `python3 -I`: that flag drops the user site directory,
where numpy lives.
"""
import argparse
import base64
import csv
import json
import os
import subprocess
import sys
import urllib.error
import urllib.request
import wave

import numpy as np

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
CSV = os.path.join(ROOT, "Docs", "VoiceOverScript.csv")
OUT = os.path.join(ROOT, "Assets", "Audio", "VO")
STAGE = os.path.join(ROOT, "Recordings", "vo")
KEY_FILE = os.path.join(ROOT, "Tools", "elevenlabs.key")

TTS = "https://api.elevenlabs.io/v1/text-to-speech/"
TRACKS = os.path.join(ROOT, "Assets", "Resources", "VisemeTracks.json")

# Which mouth each letter wants. The sprite set is Idle, AI, E, O, U, MBP, FV
# and L - see Assets/Resources/Sfx and the README - so this maps sounds onto
# the eight drawings that exist rather than onto a phoneme inventory.
#
# It reads CHARACTERS, not phonemes, which is crude: "th" gets two letters and
# a silent "e" gets a mouth it should not have. At 178 px in a branch ring,
# played at the speed of speech, what reads is the mouth OPENING AND CLOSING
# in time with the voice. Getting every vowel exactly right matters far less
# than never moving when she is silent, which is the actual complaint.
VISEME = {
    "a": "AI", "i": "AI", "y": "AI",
    "e": "E",
    "o": "O",
    "u": "U", "w": "U",
    "m": "MBP", "b": "MBP", "p": "MBP",
    "f": "FV", "v": "FV",
    "l": "L",
}
# Everything else that is voiced gets a small neutral opening.
CONSONANT = "E"
SUBSCRIPTION = "https://api.elevenlabs.io/v1/user/subscription"
# eleven_v4 with language_code "fil", and both halves of that matter.
#
# multilingual_v2 with no language set reads "Bangon" with English phonetics -
# in a cloned Filipino voice, which sounds worse than a stranger getting it
# wrong. The clone carries the voice; the MODEL decides pronunciation, and it
# has to be told what language the word is.
#
# "fil" is set for every line, including Rachel's English narration, not just
# the Filipino words. That is deliberate: her _ask lines are English sentences
# with one Filipino word inside them, and a model that says the word correctly
# alone can still anglicise it mid-sentence. Tested on the real line - "Let's
# help Kylo out of bed! Say Bangon to wake him up!" - and it holds. It also
# gives her English a Filipina teacher's accent, which is what she is.
#
# Phonetic respelling ("Ba-ngon") was tried and is WORSE. Don't.
MODEL = "eleven_v4"
LANGUAGE = "fil"

# Speech, not music: louder, and mono as Assets/Audio/VO/README.txt requires.
TARGET_DBFS = -20.0
FLOOR = 0.004

# The two lines a voice is judged on: one English sentence of Rachel's, and
# the bare Filipino word - the hardest thing to get right and the one a child
# actually copies.
AUDITION = ["rachel_word_bangon_teach", "word_bangon"]


def main():
    ap = argparse.ArgumentParser(description=__doc__,
                                 formatter_class=argparse.RawDescriptionHelpFormatter)
    ap.add_argument("--scene", help="scene token, e.g. l1_s1")
    ap.add_argument("--id", action="append", default=[], help="one id; repeatable")
    ap.add_argument("--ids-from", help="file with one id per line; '#' comments allowed. "
                                       "Use this rather than hundreds of --id flags.")
    ap.add_argument("--voice", action="append", default=[], help="voice id")
    ap.add_argument("--dry-run", action="store_true", help="print the bill; no key, nothing billed")
    ap.add_argument("--audition", action="store_true", help="two lines per voice, into Recordings/vo/audition")
    ap.add_argument("--force", action="store_true", help="replace clips that already exist")
    args = ap.parse_args()

    rows = load()
    if args.audition:
        chosen = [r for r in rows if r["id"] in AUDITION]
    else:
        ids = list(args.id)
        if args.ids_from:
            if not os.path.exists(args.ids_from):
                sys.exit("No such file: " + args.ids_from)
            for line in open(args.ids_from, encoding="utf-8"):
                line = line.split("#", 1)[0].strip()
                if line:
                    ids.append(line)
        chosen = select(rows, args.scene, ids)

    if not chosen:
        sys.exit("Nothing selected. Use --scene l1_s1, --id <id>, or --audition.")

    total = sum(len(r["say"]) for r in chosen)
    for r in chosen:
        print("%-32s %3d  %s" % (r["id"], len(r["say"]), r["say"][:64]))
    print("\n%d line(s), %d characters." % (len(chosen), total))

    if args.dry_run:
        return 0

    key = read_key()
    if key is None:
        return 2
    if not args.voice:
        sys.exit("Need --voice <id>.")

    left = credits_left(key)
    need = total * len(args.voice) if args.audition else total
    if left is not None and left < need:
        sys.exit("Not enough credits: need %d, have %d." % (need, left))

    os.makedirs(STAGE, exist_ok=True)

    if args.audition:
        d = os.path.join(STAGE, "audition")
        os.makedirs(d, exist_ok=True)
        for v in args.voice:
            for r in chosen:
                dst = os.path.join(d, "%s__%s.wav" % (v, r["id"]))
                speak(key, v, r["say"], dst)
        print("\nAuditions in %s" % os.path.relpath(d, ROOT))
        return 0

    os.makedirs(OUT, exist_ok=True)
    voice = args.voice[0]
    done = []
    for r in chosen:
        final = os.path.join(OUT, r["id"] + ".wav")
        if os.path.exists(final) and not args.force:
            print("%s already recorded - skipping." % r["id"])
            continue
        tmp = os.path.join(STAGE, r["id"] + ".wav")
        print("\n%s" % r["id"])
        speak(key, voice, r["say"], tmp)
        done.append((r["id"], tmp, getattr(speak, "track", [])))

    for name, tmp, _ in done:
        os.replace(tmp, os.path.join(OUT, name + ".wav"))
    save_tracks({name: track for name, _, track in done})
    print("\n%d clip(s) -> %s" % (len(done), os.path.relpath(OUT, ROOT)))
    print("Now run Tools > SALINLAHI > Refresh Narrator Voice Bank in Unity.")
    return 0


def save_tracks(new):
    """
    Merges the new mouth tracks into Assets/Resources/VisemeTracks.json.

    One file rather than one per clip: Unity loads it once as a TextAsset, and
    434 tiny files in Resources would each carry a .meta and each be a separate
    asset in the build.
    """
    tracks = {}
    if os.path.exists(TRACKS):
        try:
            tracks = json.load(open(TRACKS, encoding="utf-8")).get("tracks", {})
        except ValueError:
            pass
    tracks.update({k: v for k, v in new.items() if v})

    os.makedirs(os.path.dirname(TRACKS), exist_ok=True)
    with open(TRACKS, "w", encoding="utf-8") as f:
        json.dump({"tracks": tracks}, f, separators=(",", ":"), sort_keys=True)
    print("mouth tracks: %d line(s) in %s" % (len(tracks), os.path.relpath(TRACKS, ROOT)))


def load():
    rows = []
    with open(CSV, encoding="utf-8", newline="") as f:
        for r in csv.DictReader(f):
            if "(Unused)" in (r.get("Category") or ""):
                continue
            text = (r["Script / Word"] or "").strip()
            word = (r.get("Target Word") or "").strip()
            rows.append({"id": r["File Name"].rsplit(".", 1)[0],
                         "say": text.replace("{word}", word),
                         "word": word})
    return rows


def select(rows, scene, ids):
    by_id = {r["id"]: r for r in rows}
    out = []
    if scene:
        beats = [r for r in rows if r["id"].startswith("rachel_" + scene)]
        out += beats
        for w in dict.fromkeys(r["word"].lower() for r in beats if r["word"]):
            for extra in ("word_%s" % w, "rachel_word_%s_teach" % w, "rachel_word_%s_recall" % w):
                if extra in by_id:
                    out.append(by_id[extra])
    for i in ids:
        if i not in by_id:
            sys.exit("Unknown id: " + i)
        out.append(by_id[i])
    seen, keep = set(), []
    for r in out:
        if r["id"] not in seen:
            seen.add(r["id"])
            keep.append(r)
    return keep


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
        sys.stderr.write("[SALINLAHI] No ElevenLabs API key. See Tools/elevenlabs.key.example.txt\n"
                         "  Nothing was requested and nothing was billed.\n")
        return None
    print("key: %s" % src)
    return key


def credits_left(key):
    try:
        req = urllib.request.Request(SUBSCRIPTION, headers={"xi-api-key": key})
        d = json.load(urllib.request.urlopen(req, timeout=30))
        left = d["character_limit"] - d["character_count"]
        print("tier %s, %d credits left" % (d.get("tier"), left))
        return left
    except Exception:
        return None


def speak(key, voice, text, dst):
    """
    Fetches one line WITH its per-character timings.

    The timestamped endpoint costs the same characters as the plain one and
    returns the alignment alongside the audio, so there is no reason to use
    the plain one: without this, Rachel's mouth can only be animated on a
    timer, which is what made it move at random.
    """
    body = json.dumps({
        "text": text,
        "model_id": MODEL,
        "language_code": LANGUAGE,
        "voice_settings": {"stability": 0.5, "similarity_boost": 0.75,
                           "style": 0.0, "use_speaker_boost": True},
    }).encode()
    req = urllib.request.Request(
        TTS + voice + "/with-timestamps?output_format=mp3_44100_128", data=body,
        headers={"xi-api-key": key, "Content-Type": "application/json"})
    try:
        with urllib.request.urlopen(req, timeout=300) as r:
            payload = json.load(r)
    except urllib.error.HTTPError as e:
        sys.exit("  HTTP %d: %s" % (e.code, e.read().decode("utf-8", "replace")[:300]))

    audio = base64.b64decode(payload["audio_base64"])
    if not audio:
        sys.exit("  The API did not return audio.")

    speak.track = mouth_track(payload.get("alignment") or {})

    mp3 = dst + ".mp3"
    open(mp3, "wb").write(audio)
    subprocess.run(["/usr/bin/afconvert", "-f", "WAVE", "-d", "LEI16@44100", "-c", "1",
                    mp3, dst], check=True, capture_output=True)
    os.remove(mp3)
    shape(dst)


def mouth_track(alignment):
    """
    Turns per-character timings into a list of (time, viseme) changes.

    Collapses runs of the same mouth, drops anything under 40 ms - faster than
    that is a flicker rather than a shape - and closes the mouth on silence,
    which is the part that makes it look like speech rather than chewing.
    """
    chars = alignment.get("characters") or []
    starts = alignment.get("character_start_times_seconds") or []
    ends = alignment.get("character_end_times_seconds") or []
    if not chars:
        return []

    raw = []
    for c, a, b in zip(chars, starts, ends):
        low = c.lower()
        if not low.strip() or low in ",.!?;:-\u2014\"'":
            v = "Idle"
        elif low in VISEME:
            v = VISEME[low]
        elif low.isalpha():
            v = CONSONANT
        else:
            v = "Idle"
        raw.append((round(float(a), 3), v, round(float(b), 3)))

    track, last = [], None
    for a, v, b in raw:
        if v == last:
            continue
        if track and a - track[-1][0] < 0.04:
            track[-1] = [round(a, 3), v]      # too brief to see; replace it
        else:
            track.append([round(a, 3), v])
        last = v

    if track and track[-1][1] != "Idle":
        track.append([round(raw[-1][2], 3), "Idle"])

    # Replacing a too-brief entry above can leave it equal to the one before,
    # so the run is collapsed once more at the end rather than trusted.
    clean = []
    for t, v in track:
        if clean and clean[-1][1] == v:
            continue
        clean.append([t, v])
    return clean


def shape(path):
    """Trim hard and level. The README is emphatic about dead air, and
    slowPitch 0.85 stretches any silence at the head of a word clip by 18%."""
    with wave.open(path, "rb") as w:
        rate, n = w.getframerate(), w.getnframes()
        x = np.frombuffer(w.readframes(n), dtype="<i2").astype(np.float32) / 32768.0

    loud = np.nonzero(np.abs(x) > FLOOR)[0]
    cut = 0.0
    if len(loud):
        head = max(0, loud[0] - int(0.010 * rate))
        cut = head / float(rate)
        x = x[head:min(len(x), loud[-1] + int(0.030 * rate))]

    # The clip just lost `cut` seconds off its front. The track describes the
    # UNTRIMMED audio, so without this every mouth shape fires late by exactly
    # the silence that was removed.
    if cut and getattr(speak, "track", None):
        speak.track = [[max(0.0, round(t - cut, 3)), v] for t, v in speak.track]

    rms = float(np.sqrt(np.mean(np.square(x)))) or 1e-9
    gain = min((10.0 ** (TARGET_DBFS / 20.0)) / rms,
               0.891 / (float(np.max(np.abs(x))) or 1e-9))
    y = np.clip(np.rint(x * gain * 32768.0), -32768, 32767).astype("<i2")

    with wave.open(path, "wb") as w:
        w.setnchannels(1); w.setsampwidth(2); w.setframerate(rate)
        w.writeframes(y.tobytes())
    print("  %.2f s, mono %d Hz" % (len(y) / float(rate), rate))


if __name__ == "__main__":
    sys.exit(main())
