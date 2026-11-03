"""Generate SALINLAHI's background music with the ElevenLabs Music API.

Six tracks: one per place, plus a sting for the results panel. They are beds,
not songs - this game listens to a five-year-old through the device
microphone, and anything busy enough to be noticed is busy enough to be heard
by the recogniser. See Docs/VoicePromptingDesign.md and the comment in
Assets/Scripts/VoiceCommand.cs about what a phone speaker does to recognition.

Credits are shared with the voice-over. Music runs about 900 credits a
minute, so the full set below is roughly 6,800 of a Starter plan's 30,000 a
month, and the 434-line voice-over is about 17,000 more. --dry-run prints the
bill and needs no key.

Looping is handled here rather than in Unity. The API returns a track with a
beginning and an end; played on AudioSource.loop that is an audible seam every
ninety seconds, which is worse than silence. The last LOOP_TAIL seconds are
crossfaded over the first LOOP_TAIL seconds and the file trimmed to match, so
plain loop = true is seamless with nothing running at playback time.

Run from the repository root:

    python3 Tools/generate_music.py --dry-run
    python3 Tools/generate_music.py --track music_bahay
    python3 Tools/generate_music.py --all

Plain python3, not `python3 -I` like check_voiceover_script.py: that flag also
drops the user site directory, which is where numpy lives. The other numpy
tools in this folder are run the same way.

The API key is read from Tools/elevenlabs.key (gitignored) or
$ELEVENLABS_API_KEY. It is never accepted as an argument - an argv is visible
to every process on the machine - and never printed.
"""
import argparse
import json
import os
import subprocess
import sys
import tempfile
import urllib.error
import urllib.request
import wave

import numpy as np

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
OUT = os.path.join(ROOT, "Assets", "Resources", "Music")
STAGE = os.path.join(ROOT, "Recordings", "music")       # Recordings/ is gitignored
KEY_FILE = os.path.join(ROOT, "Tools", "elevenlabs.key")

API = "https://api.elevenlabs.io/v1/music"
SUBSCRIPTION = "https://api.elevenlabs.io/v1/user/subscription"

CREDITS_PER_MINUTE = 900        # ElevenLabs' own figure, confirm in the web UI

# Crossfaded into the head of the track to make it loop without a seam.
LOOP_TAIL = 3.0

# Three minutes, not the ninety seconds this started as. A loop is heard
# however long the child plays, and at ninety seconds a twenty-minute sitting
# hears the same bar thirteen times. Length is the only real cure for that,
# and on a plan with 600,000 credits a month the whole set costs about two
# per cent of them - so there was never a reason to be short except a budget
# that no longer applies.

# Music sits well under the narration. VoiceNarrator does no normalisation of
# its own - volume is the player's slider - so the levels have to be right in
# the files.
TARGET_DBFS = -24.0

STYLE = ("Soft, warm, unhurried instrumental music for a children's storybook game. "
         "Acoustic nylon guitar, light tuned percussion, gentle strings, "
         "Filipino folk colour. Absolutely no vocals, no lyrics, no drums or "
         "percussion loud enough to mask a young child's speaking voice. "
         "Low energy, spacious, repetitive and undemanding - this plays quietly "
         "underneath a narrator and must never pull attention. ")

TRACKS = [
    ("music_bahay", 180, STYLE +
     "The mood is a sunlit Filipino family home early in the morning: tender, "
     "domestic and sleepy, waking gently into the day."),
    ("music_paaralan", 180, STYLE +
     "The mood is a bright, friendly primary school morning: curious and "
     "orderly, a little brisker than the home theme but still calm."),
    ("music_parke", 180, STYLE +
     "The mood is a green neighbourhood park in the afternoon: open, breezy "
     "and playful, with a little more lift and air than the other themes."),
    ("music_menu", 180, STYLE +
     "The mood is a welcoming title screen: the warmest and most melodic of "
     "the set, inviting a child to begin."),
    ("music_journey", 180, STYLE +
     "The mood is a hand-drawn map of places still to visit: light, curious "
     "and anticipatory, gently looping while the child chooses."),
    ("music_complete", 10, STYLE +
     "A short, warm, encouraging resolution for finishing a level. It must "
     "feel kind rather than triumphant - the same cue plays for a child who "
     "only just scraped through. No fanfare, no cymbals."),
]


def main():
    ap = argparse.ArgumentParser(description=__doc__,
                                 formatter_class=argparse.RawDescriptionHelpFormatter)
    ap.add_argument("--dry-run", action="store_true",
                    help="print the prompts and the bill. No key, no request, nothing billed.")
    ap.add_argument("--probe", action="store_true",
                    help="report the account's tier and remaining credits. Nothing billed.")
    ap.add_argument("--track", action="append", default=[],
                    help="generate one track by id; repeatable.")
    ap.add_argument("--all", action="store_true", help="generate every track.")
    ap.add_argument("--force", action="store_true",
                    help="regenerate a track that already exists.")
    args = ap.parse_args()

    wanted = pick(args)

    if args.dry_run:
        bill(wanted, verbose=True)
        return 0

    key = read_key()
    if key is None:
        return 2

    if args.probe:
        probe(key)
        return 0

    if not wanted:
        sys.exit("Nothing selected. Use --track <id>, --all, or --dry-run.")

    total = bill(wanted, verbose=False)
    left = probe(key)
    if left is not None and left < total:
        sys.exit("Not enough credits: this run needs %d and %d remain." % (total, left))

    os.makedirs(STAGE, exist_ok=True)
    os.makedirs(OUT, exist_ok=True)

    # Everything is staged first. Nothing reaches Assets/ until every selected
    # track has a finished, verified file, so a failure halfway leaves the
    # project exactly as it was.
    done = []
    for name, seconds, prompt in wanted:
        print("\n%s  (%d s)" % (name, seconds))
        raw = compose(key, prompt, seconds, os.path.join(STAGE, name))
        wav = os.path.join(STAGE, name + ".decoded.wav")
        decode(raw, wav)
        loop = seconds > 20            # the sting must not be looped
        finish(wav, loop)
        done.append((name, wav))

    for name, wav in done:
        dst = os.path.join(OUT, name + ".wav")
        os.replace(wav, dst)
        print("  -> %s" % os.path.relpath(dst, ROOT))

    print("\nNow focus Unity so it imports them, then play MainMenu.")
    return 0


def pick(args):
    if args.all or (not args.track and (args.dry_run or args.probe)):
        chosen = list(TRACKS)
    else:
        known = {t[0] for t in TRACKS}
        for t in args.track:
            if t not in known:
                sys.exit("Unknown track %r. Known: %s" % (t, ", ".join(sorted(known))))
        chosen = [t for t in TRACKS if t[0] in set(args.track)]

    if args.dry_run or args.probe or args.force:
        return chosen

    keep = []
    for t in chosen:
        if os.path.exists(os.path.join(OUT, t[0] + ".wav")):
            print("%s already exists - skipping. Use --force to replace it." % t[0])
        else:
            keep.append(t)
    return keep


def bill(tracks, verbose):
    total = 0
    for name, seconds, prompt in tracks:
        cost = int(round(seconds / 60.0 * CREDITS_PER_MINUTE))
        total += cost
        if verbose:
            print("%-16s %3d s  ~%5d credits" % (name, seconds, cost))
            print("    " + prompt[len(STYLE):].strip())
    print("\n%d track(s), ~%d credits at %d/minute." % (len(tracks), total, CREDITS_PER_MINUTE))
    if verbose:
        print("The web UI shows the exact cost before generating - worth checking once.")
    return total


def read_key():
    key = os.environ.get("ELEVENLABS_API_KEY", "").strip()
    source = "$ELEVENLABS_API_KEY"
    if not key and os.path.exists(KEY_FILE):
        source = os.path.relpath(KEY_FILE, ROOT)
        for line in open(KEY_FILE, encoding="utf-8"):
            line = line.strip()
            if line and not line.startswith("#"):
                key = line
                break
        mode = os.stat(KEY_FILE).st_mode
        if mode & 0o077:
            print("Warning: %s is readable by others. chmod 600 it." % source)

    if not key:
        sys.stderr.write(
            "[SALINLAHI] No ElevenLabs API key.\n"
            "  Looked for $ELEVENLABS_API_KEY, then %s\n"
            "  Create it:  printf '%%s\\n' 'YOUR-KEY' > %s && chmod 600 %s\n"
            "  Nothing was requested and nothing was billed.\n"
            "  (--dry-run works without a key.)\n"
            % (os.path.relpath(KEY_FILE, ROOT),
               os.path.relpath(KEY_FILE, ROOT), os.path.relpath(KEY_FILE, ROOT)))
        return None

    print("key: %s" % source)          # the source, never the value
    return key


def probe(key):
    try:
        req = urllib.request.Request(SUBSCRIPTION, headers={"xi-api-key": key})
        with urllib.request.urlopen(req, timeout=30) as r:
            d = json.load(r)
    except urllib.error.HTTPError as e:
        print("Could not read the subscription: HTTP %d" % e.code)
        return None

    left = d.get("character_limit", 0) - d.get("character_count", 0)
    print("tier %s, %d of %d credits left this period"
          % (d.get("tier", "?"), left, d.get("character_limit", 0)))
    return left


def compose(key, prompt, seconds, dst_stem):
    """
    Fetches one track, preferring uncompressed audio.

    WAV and PCM at 44.1 kHz are a Pro-tier format; everything below refuses
    them. Asking first and falling back costs one wasted request at most, and
    on Pro it removes an entire generation of lossy encoding - the track is
    crossfaded, re-levelled and re-encoded to Vorbis by Unity afterwards, and
    mp3 in front of all of that is a loss taken for nothing.
    """
    body = json.dumps({"prompt": prompt, "music_length_ms": int(seconds * 1000)}).encode()

    for fmt, ext, accept in (("wav_44100", ".wav", "audio/wav"),
                             ("mp3_44100_128", ".mp3", "audio/mpeg")):
        req = urllib.request.Request(
            API + "?output_format=" + fmt,
            data=body,
            headers={"xi-api-key": key, "Content-Type": "application/json",
                     "Accept": accept})
        try:
            with urllib.request.urlopen(req, timeout=900) as r:
                audio = r.read()
        except urllib.error.HTTPError as e:
            detail = e.read().decode("utf-8", "replace")[:300]
            if fmt.startswith("wav") and e.code in (401, 403, 422):
                print("  %s not available on this plan, falling back to mp3" % fmt)
                continue
            sys.exit("  HTTP %d from the music API: %s" % (e.code, detail))

        if not audio or audio[:1] == b"{":
            sys.exit("  The API did not return audio.")

        dst = dst_stem + ext
        tmp = dst + ".part"
        with open(tmp, "wb") as f:
            f.write(audio)
            f.flush()
            os.fsync(f.fileno())
        os.replace(tmp, dst)
        print("  %.1f KB as %s" % (len(audio) / 1024.0, fmt))
        return dst

    sys.exit("  Could not fetch the track in any format.")


def decode(mp3, wav):
    """mp3 -> stereo 44.1k 16-bit WAV. Music stays stereo; only the VO is mono."""
    subprocess.run(["/usr/bin/afconvert", "-f", "WAVE", "-d", "LEI16@44100", "-c", "2",
                    mp3, wav], check=True, capture_output=True)


def finish(path, loop):
    """Crossfades the tail over the head so the file loops, then sets the level."""
    with wave.open(path, "rb") as w:
        channels, width, rate, frames = w.getnchannels(), w.getsampwidth(), w.getframerate(), w.getnframes()
        raw = w.readframes(frames)

    if width != 2:
        sys.exit("  Expected 16-bit audio from afconvert, got %d-bit." % (width * 8))

    x = np.frombuffer(raw, dtype="<i2").astype(np.float32).reshape(-1, channels) / 32768.0

    if loop:
        tail = int(LOOP_TAIL * rate)
        if len(x) > tail * 3:
            fade = np.linspace(0.0, 1.0, tail, dtype=np.float32)[:, None]
            head, end = x[:tail], x[-tail:]
            x = np.concatenate([head * fade + end * (1.0 - fade), x[tail:-tail]])
        else:
            print("  Too short to loop cleanly; left as it is.")

    rms = float(np.sqrt(np.mean(np.square(x)))) or 1e-9
    gain = (10.0 ** (TARGET_DBFS / 20.0)) / rms
    peak = float(np.max(np.abs(x))) or 1e-9
    gain = min(gain, 0.891 / peak)            # leave 1 dB of headroom

    y = np.clip(np.rint(x * gain * 32768.0), -32768, 32767).astype("<i2")

    with wave.open(path, "wb") as w:
        w.setnchannels(channels)
        w.setsampwidth(2)
        w.setframerate(rate)
        w.writeframes(y.tobytes())

    print("  %.1f s, %d Hz, %d ch, %.1f dBFS%s"
          % (len(y) / float(rate), rate, channels,
             20 * np.log10(max(float(np.sqrt(np.mean(np.square(y / 32768.0)))), 1e-9)),
             ", looping" if loop else ""))


if __name__ == "__main__":
    sys.exit(main())
