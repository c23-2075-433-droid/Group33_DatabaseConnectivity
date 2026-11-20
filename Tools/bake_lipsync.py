"""Bake Teacher Rachel's mouth keyframes from the recordings themselves.

This is the same idea as Adobe Animate's Auto Lip-Sync: the voice track is
analysed ONCE, the phonemes in it are found, and a mouth-shape keyframe is
laid down for each. RachelPortraitAnimator then just plays those keyframes
against the clip's playback time - no guessing at run time.

The analysis is Rhubarb Lip Sync (github.com/DanielSWolf/rhubarb-lip-sync,
MIT). It listens to the actual WAV, so a re-cut take or a spliced Filipino
word is timed to what is really in the file. Where Docs/VoiceOverScript.csv
has the line's text, that text is passed as the dialog, which makes the
recognition much more accurate. The Filipino word clips use Rhubarb's
language-independent phonetic recogniser, since its dictionary is English.

Rhubarb's shapes are Preston Blair's, which is also what Animate uses. They
map onto the sprites cut by make_rachel_visemes.py:

    A  lips pressed (M B P)          -> MBP
    B  slightly open, teeth (K S T EE) -> E
    C  open (EH AE)                  -> AI
    D  wide open (AA)                -> AI
    E  slightly rounded (AO ER)      -> O
    F  puckered (UW OW W)            -> U
    G  teeth on lip (F V)            -> FV
    H  tongue up (L)                 -> L
    X  rest                          -> Idle

Reads:   Assets/Audio/VO/*.wav, Docs/VoiceOverScript.csv
Writes:  Assets/Resources/VisemeTracks.json
Cache:   Tools/rhubarb/cache.json - a clip is only re-analysed when its WAV
         or its script text changes, so a re-run after one new recording
         takes seconds, not the forty minutes the first one does.

Needs Rhubarb unzipped into Tools/rhubarb/ (git-ignored, 87 MB):
    github.com/DanielSWolf/rhubarb-lip-sync/releases  ->  Windows zip

Run from the repository root:   python Tools/bake_lipsync.py
                                python Tools/bake_lipsync.py rachel_l1_s1_*   (just some)
Then press Play - Unity re-imports the JSON on its own.
"""
import csv
import fnmatch
import glob
import hashlib
import json
import os
import subprocess
import sys
import tempfile
from concurrent.futures import ThreadPoolExecutor

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
VO = os.path.join(ROOT, "Assets", "Audio", "VO")
CSV = os.path.join(ROOT, "Docs", "VoiceOverScript.csv")
TRACKS = os.path.join(ROOT, "Assets", "Resources", "VisemeTracks.json")
CACHE = os.path.join(ROOT, "Tools", "rhubarb", "cache.json")

SHAPES = {"A": "MBP", "B": "E", "C": "AI", "D": "AI", "E": "O",
          "F": "U", "G": "FV", "H": "L", "X": "Idle"}

# Rhubarb is single-file but uses several threads itself; a few at once keeps
# every core busy without the runs fighting each other.
WORKERS = max(1, (os.cpu_count() or 4) // 2)


def rhubarb():
    found = glob.glob(os.path.join(ROOT, "Tools", "rhubarb", "**", "rhubarb.exe"), recursive=True) \
        or glob.glob(os.path.join(ROOT, "Tools", "rhubarb", "**", "rhubarb"), recursive=True)
    if not found:
        sys.exit("Rhubarb is not in Tools/rhubarb/. Download the Windows zip from "
                 "github.com/DanielSWolf/rhubarb-lip-sync/releases and unzip it there.")
    return found[0]


def script_text():
    """Clip id -> what is said in it, with {word} filled in."""
    lines = {}
    with open(CSV, encoding="utf-8", errors="replace", newline="") as f:
        for r in csv.DictReader(f):
            text = (r.get("Script / Word") or "").strip()
            word = (r.get("Target Word") or "").strip()
            if text:
                lines[r["File Name"].rsplit(".", 1)[0]] = text.replace("{word}", word)
    return lines


def digest(path, text, phonetic):
    h = hashlib.sha1()
    with open(path, "rb") as f:
        h.update(f.read())
    h.update(("|%s|%s" % (text, phonetic)).encode("utf-8"))
    return h.hexdigest()


def analyse(exe, path, text, phonetic):
    """Runs Rhubarb on one clip and returns its keyframes as [[time, mouth], ...]."""
    with tempfile.TemporaryDirectory() as tmp:
        out = os.path.join(tmp, "cues.json")
        cmd = [exe, "-q", "-f", "json", "--extendedShapes", "GHX", "-o", out]
        if phonetic:
            cmd += ["-r", "phonetic"]
        if text:
            dialog = os.path.join(tmp, "dialog.txt")
            with open(dialog, "w", encoding="utf-8") as f:
                f.write(text)
            cmd += ["-d", dialog]
        cmd.append(path)
        subprocess.run(cmd, check=True, stdout=subprocess.DEVNULL, stderr=subprocess.PIPE)
        with open(out, encoding="utf-8") as f:
            cues = json.load(f)["mouthCues"]

    track = []
    for cue in cues:
        mouth = SHAPES.get(cue["value"], "Idle")
        if track and track[-1][1] == mouth:
            continue  # two Rhubarb shapes that land on one sprite: one keyframe
        track.append([round(cue["start"], 3), mouth])
    # Close the mouth when the sound ends, whatever the last cue was.
    if cues and (not track or track[-1][1] != "Idle"):
        track.append([round(cues[-1]["end"], 3), "Idle"])
    return track


def main(patterns):
    exe = rhubarb()
    lines = script_text()

    clips = sorted(glob.glob(os.path.join(VO, "*.wav")))
    if patterns:
        clips = [c for c in clips
                 if any(fnmatch.fnmatch(os.path.splitext(os.path.basename(c))[0], p) for p in patterns)]

    cache = {}
    if os.path.exists(CACHE):
        try:
            cache = json.load(open(CACHE, encoding="utf-8"))
        except ValueError:
            cache = {}

    jobs = []
    for path in clips:
        cid = os.path.splitext(os.path.basename(path))[0]
        text = lines.get(cid, "")
        # The word clips are Filipino; Rhubarb's dictionary is English.
        phonetic = cid.startswith("word_")
        key = digest(path, text, phonetic)
        hit = cache.get(cid)
        if hit and hit.get("key") == key:
            continue
        jobs.append((cid, path, text, phonetic, key))

    print("%d clip(s), %d to analyse (%d unchanged), %d at a time"
          % (len(clips), len(jobs), len(clips) - len(jobs), WORKERS))

    failed = []

    def run(job):
        cid, path, text, phonetic, key = job
        try:
            return cid, key, analyse(exe, path, text, phonetic)
        except subprocess.CalledProcessError as e:
            failed.append((cid, (e.stderr or b"").decode("utf-8", "replace").strip()[-200:]))
            return cid, key, None

    with ThreadPoolExecutor(WORKERS) as pool:
        for n, (cid, key, track) in enumerate(pool.map(run, jobs), 1):
            if track is not None:
                cache[cid] = {"key": key, "track": track}
            print("  [%d/%d] %s%s" % (n, len(jobs), cid, "" if track is not None else "  FAILED"))
            if n % 20 == 0:
                save(cache)

    save(cache)

    # The game's file holds every clip that is still on disk - not just the
    # ones analysed this run, and nothing for recordings since deleted.
    on_disk = {os.path.splitext(os.path.basename(p))[0] for p in glob.glob(os.path.join(VO, "*.wav"))}
    tracks = {cid: steady(v["track"]) for cid, v in sorted(cache.items()) if cid in on_disk and v.get("track")}
    with open(TRACKS, "w", encoding="utf-8") as f:
        json.dump({"tracks": tracks}, f, separators=(",", ":"), sort_keys=True)
    print("mouth tracks: %d clip(s) -> %s" % (len(tracks), os.path.relpath(TRACKS, ROOT)))

    for cid, err in failed:
        print("FAILED %s: %s" % (cid, err))
    return 1 if failed else 0


# Shortest a mouth is shown. Rhubarb inserts in-between shapes as brief as a
# hundredth of a second; at 60 fps that is a one-frame flash, read as flicker.
MIN_HOLD = 0.04


def steady(track):
    """Drops keyframes held under MIN_HOLD, letting the previous mouth carry on."""
    out = []
    for i, (t, mouth) in enumerate(track):
        if out and out[-1][1] == mouth:
            continue
        if out and t - out[-1][0] < MIN_HOLD:
            # Too soon after the last mouth that was kept. The closing rest
            # at the very end still wins - she must not be left mid-vowel -
            # otherwise this flash is dropped.
            if i == len(track) - 1:
                out[-1][1] = mouth
                if len(out) > 1 and out[-2][1] == mouth:
                    out.pop()
            continue
        out.append([t, mouth])
    return out


def save(cache):
    os.makedirs(os.path.dirname(CACHE), exist_ok=True)
    with open(CACHE, "w", encoding="utf-8") as f:
        json.dump(cache, f, separators=(",", ":"), sort_keys=True)


if __name__ == "__main__":
    sys.exit(main(sys.argv[1:]))
