#!/bin/sh
# Measure SALINLAHI on an Android phone: cold start, resident memory, frame
# times and the voice recognition wait.
#
#   Tools/measure_device.sh
#
# These are the four figures Activity 10 lists as unmeasured and the course
# feedback asks for by name. Three of them cannot be read from inside the
# process, so they come from adb; the fourth, the voice wait, is written by the
# game itself and is pulled off the device at the end.
#
# WHAT YOU NEED
#
#   - the phone connected by USB with USB debugging on, and the "Allow USB
#     debugging?" dialog accepted on the phone itself
#   - the app installed, built from a commit that contains PerfSampler and the
#     latency columns in VoiceAttemptLog (anything older writes no CSV)
#   - adb, which does NOT need installing: Unity ships it, and this script
#     finds it there
#
# The script stops and waits for you at the points where someone has to play
# the game. Read what it asks before pressing Return - a capture taken in the
# wrong scene is a wrong number, not a missing one.
#
# Everything lands in Docs/perf/ as a dated evidence file plus the two CSVs the
# game wrote, which is the same shape as the Activity 10 evidence already there.

set -u

PKG="ph.edu.uphsl.group33.salinlahi"
UNITY_ADB="/Applications/Unity/Hub/Editor/6000.6.2f1/PlaybackEngines/AndroidPlayer/SDK/platform-tools/adb"
ROOT=$(cd "$(dirname "$0")/.." && pwd)
OUTDIR="$ROOT/Docs/perf"
STAMP=$(date +%Y%m%d)
OUT="$OUTDIR/${STAMP}_device_performance.txt"

COLD_RUNS=5
MENU_SETTLE=20      # seconds of menu to sample before the first capture

# ---- adb -------------------------------------------------------------------

if command -v adb >/dev/null 2>&1; then
  ADB=adb
elif [ -x "$UNITY_ADB" ]; then
  ADB="$UNITY_ADB"
else
  echo "measure_device: no adb found."
  echo "  Looked on PATH and at $UNITY_ADB"
  exit 2
fi

COUNT=$("$ADB" devices | grep -cw 'device$')
if [ "$COUNT" -eq 0 ]; then
  echo "measure_device: no phone connected."
  echo "  Plug it in, turn on USB debugging, and accept the dialog on the phone."
  "$ADB" devices
  exit 2
fi
if [ "$COUNT" -gt 1 ]; then
  echo "measure_device: more than one device connected; unplug the others."
  "$ADB" devices
  exit 2
fi

if ! "$ADB" shell pm list packages | grep -q "$PKG"; then
  echo "measure_device: $PKG is not installed on this phone."
  exit 2
fi

ACTIVITY=$("$ADB" shell cmd package resolve-activity --brief "$PKG" 2>/dev/null | tail -1 | tr -d '\r')
case "$ACTIVITY" in
  */*) : ;;
  *)   ACTIVITY="$PKG/com.unity3d.player.UnityPlayerActivity" ;;
esac

mkdir -p "$OUTDIR"

say() { echo "$@" | tee -a "$OUT"; }
cap() { echo "" >>"$OUT"; echo "\$ $*" >>"$OUT"; "$@" >>"$OUT" 2>&1; }
pause() {
  echo ""
  echo "  >> $1"
  printf '  >> press Return when ready: '
  read _ignored
}

: >"$OUT"
say "SALINLAHI on-device performance measurement"
say "date       $(date '+%Y-%m-%d %H:%M:%S')"
say "package    $PKG"
say "activity   $ACTIVITY"
say "adb        $ADB"
say "commit     $(cd "$ROOT" && git rev-parse --short HEAD 2>/dev/null)"
say ""

# ---- 1. what we are measuring on -------------------------------------------
#
# An FPS figure with no phone named is not evidence. This section is what the
# paper's method has to quote.

say "== 1. Device =="
for prop in ro.product.manufacturer ro.product.model ro.build.version.release \
            ro.build.version.sdk ro.product.cpu.abi ro.sf.lcd_density; do
  say "$prop = $("$ADB" shell getprop $prop | tr -d '\r')"
done
say "total RAM  = $("$ADB" shell grep MemTotal /proc/meminfo | tr -d '\r')"
say "app        = $("$ADB" shell dumpsys package "$PKG" | grep -m1 versionName | tr -d '\r ')"

# ---- 2. cold start ---------------------------------------------------------
#
# `am start -W` reports TotalTime: from the launch to the app drawing its first
# frame. Force-stopping first is what makes it cold; without that you measure a
# resume, which is a different and much smaller number.
#
# Repeated because the first launch after an install is always the slowest - it
# pays for the one-time dexopt and the permission dialog - and quoting that one
# as the startup time would be unfair to the project.

say ""
say "== 2. Cold start, $COLD_RUNS runs =="
say "TotalTime is launch to first frame drawn, in milliseconds."
say "Run 1 is expected to be the slowest; quote the median of the rest."
say ""
RUN=1
while [ "$RUN" -le "$COLD_RUNS" ]; do
  "$ADB" shell am force-stop "$PKG" >/dev/null 2>&1
  sleep 3
  LINE=$("$ADB" shell am start -W -n "$ACTIVITY" 2>&1 | tr -d '\r' | grep -E 'TotalTime|WaitTime')
  say "run $RUN: $(echo "$LINE" | tr '\n' ' ')"
  RUN=$((RUN + 1))
  sleep 2
done

# ---- 3. resident memory ----------------------------------------------------
#
# This is the figure the course calls resident memory, and it cannot be read
# from inside the process: Unity can report its own heap, but not what the
# operating system says the process holds. dumpsys meminfo is the source.
#
# Read the TOTAL PSS line. PSS, proportional set size, is the honest one for a
# single app: it counts shared pages at their share, so it does not double count
# the framework's libraries.

say ""
say "== 3. Resident memory (dumpsys meminfo, read TOTAL PSS) =="
pause "The app is running from the last cold start. Leave it on the MAIN MENU and do not touch it."
sleep "$MENU_SETTLE"
say ""
say "-- at the main menu --"
cap "$ADB" shell dumpsys meminfo "$PKG"

pause "Now play a CHAPTER SCENE - a bedroom or school scene with the voice prompt running - and answer a few words out loud. Leave it playing."
say ""
say "-- during a chapter scene, with voice recognition running --"
cap "$ADB" shell dumpsys meminfo "$PKG"

pause "Now move through SEVERAL scenes in a row, finish a chapter, and come back. This is the growth check Activity 10 is watching for."
say ""
say "-- after several scene loads --"
cap "$ADB" shell dumpsys meminfo "$PKG"
say ""
say "NOTE: three TOTAL PSS figures that stay level mean no leak across scenes."
say "      A figure that climbs every capture is the growth Activity 10 names"
say "      as the one suspected concern, and is worth a Memory Profiler session."

# ---- 4. frames per second --------------------------------------------------
#
# The in-game PerfSampler is the primary source: it samples the frame times the
# engine already computes, in the release build, and writes percentiles. These
# two commands are the independent cross-check from outside the process.
#
# SurfaceFlinger's --latency gives real presentation timestamps for the app's
# surface, which is the only outside view of a Unity app's frame rate that
# works. gfxinfo is tried as well but is usually empty for Unity: Unity draws
# to its own surface rather than through the Android HWUI pipeline that gfxinfo
# reports on. An empty gfxinfo section is expected and is not a failure.

say ""
say "== 4. Frames per second, cross-check from outside the process =="
LAYER=$("$ADB" shell dumpsys SurfaceFlinger --list 2>/dev/null | tr -d '\r' | grep "$PKG" | head -1)
say "surface layer = ${LAYER:-not found}"

if [ -n "$LAYER" ]; then
  "$ADB" shell dumpsys SurfaceFlinger --latency-clear >/dev/null 2>&1
  pause "Play normally for about 15 seconds without pausing, then come back."
  say ""
  say "-- SurfaceFlinger frame timestamps (nanoseconds; three columns per frame) --"
  say "   The middle column is when the frame was presented. Differences between"
  say "   consecutive middle values are the frame intervals; 16.7 ms is 60 fps."
  cap "$ADB" shell dumpsys SurfaceFlinger --latency "$LAYER"
else
  say "SurfaceFlinger did not list a layer for this package, which happens on"
  say "some Android versions. The in-game CSV below is then the only FPS source,"
  say "and the paper should say so rather than quote a number with no method."
fi

say ""
say "-- gfxinfo (expected to be thin or empty for a Unity app) --"
cap "$ADB" shell dumpsys gfxinfo "$PKG" framestats

# ---- 5. the game's own logs ------------------------------------------------
#
# The voice wait and the frame time percentiles come from here. PerfSampler
# flushes a partial window when the app is paused, so backgrounding the app is
# what makes the last rows appear.

say ""
say "== 5. The game's own measurements =="
pause "Last step: press the phone's HOME button to background the app. That makes PerfSampler write out its final window."
sleep 2

FILES="/sdcard/Android/data/$PKG/files"
for f in salinlahi_performance.csv salinlahi_performance_device.txt salinlahi_voice_attempts.csv; do
  if "$ADB" shell "test -f $FILES/$f" 2>/dev/null; then
    "$ADB" pull "$FILES/$f" "$OUTDIR/${STAMP}_$f" >/dev/null 2>&1 \
      && say "pulled $f -> Docs/perf/${STAMP}_$f" \
      || say "FAILED to pull $f"
  else
    say "$f is not on the device."
    say "  Either this build predates PerfSampler and the latency columns, or"
    say "  nothing wrote to it yet. Check 'adb logcat | grep -E \"PerfSampler|VoiceAttemptLog\"'."
  fi
done

say ""
say "== Done =="
say "Evidence file: $OUT"
say ""
say "What to read, and from where:"
say "  startup           section 2, TotalTime, median of runs 2..$COLD_RUNS"
say "  resident memory   section 3, TOTAL PSS in each of the three captures"
say "  frames per second ${STAMP}_salinlahi_performance.csv, avgFps and p99FrameMs"
say "                    cross-checked against section 4"
say "  voice wait        ${STAMP}_salinlahi_voice_attempts.csv,"
say "                    lastPartialToResultMs where partialCount > 0"
say "  what it ran on    section 1 and ${STAMP}_salinlahi_performance_device.txt"
