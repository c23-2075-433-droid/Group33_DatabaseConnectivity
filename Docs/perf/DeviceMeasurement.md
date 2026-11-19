# Measuring SALINLAHI on an Android phone

This is the procedure for the four figures Activity 10 lists as unmeasured and
the course feedback on that activity asks for by name: frames per second,
resident memory, startup time, and the actual speech recognition latency. The
feedback's wording is the requirement — **direct target-device measurement** —
so nothing here is an editor reading, an estimate, or arithmetic from a file
header.

Run it with:

```bash
Tools/measure_device.sh
```

The script stops and waits at the points where someone has to play the game. It
writes one dated evidence file into `Docs/perf/`, alongside the CSVs it pulls
off the phone, which is the same shape as the Activity 10 evidence already in
that folder.

## Where each number comes from

| Figure | Source | Why that source |
|---|---|---|
| Startup time | `adb shell am start -W`, `TotalTime` | Launch to first frame drawn, measured from outside the process. Nothing inside the app can see the operating system's launch or the Unity player's own initialisation |
| Resident memory | `adb shell dumpsys meminfo`, `TOTAL PSS` | This is what "resident" means: what the OS says the process holds. Unity can report its own heap and nothing more |
| Frames per second | `salinlahi_performance.csv`, written by `PerfSampler` in the build | Sampled from the frame times the engine already computes, in the release build, during real play. Cross-checked against `dumpsys SurfaceFlinger --latency` |
| Speech recognition latency | `salinlahi_voice_attempts.csv`, the `lastPartialToResultMs` column | Measured in `VoiceCommand` from the recognizer's own callbacks, per attempt |
| What it ran on | `salinlahi_performance_device.txt` and section 1 of the evidence file | An FPS figure with no phone named is not a measurement |

## Two things the earlier plan got wrong

Activity 10's monitoring plan is corrected by this procedure on two points, and
both corrections are the reason there is new code rather than just new commands.

**The voice round trip could not be read from the attempt log.** The plan says
to take it "from the timestamps in `salinlahi_voice_attempts.csv`". That file
had one wall-clock stamp per row, which gives the gap *between* attempts, not
the delay *inside* one. `VoiceAttemptLog` now has five latency columns that
`VoiceCommand` fills from the recognizer's callbacks, so the plan's own
threshold — 1.5 seconds from speaking to a verdict — is now something the file
can actually be checked against.

**The Unity Profiler was the wrong instrument here.** The plan says to measure
"with the Unity Profiler on a build". Attaching it wants a development build
with Autoconnect Profiler, which is not the binary that ships, and Activity 9
recorded `enableInternalProfiler: 0` as a reviewed, deliberate setting.
Measuring a different binary from the released one is weak evidence, and
flipping a setting the security review signed off is worse. `PerfSampler`
samples from inside the release build instead, and the build configuration is
untouched — `enableInternalProfiler` is still `0`.

One smaller correction: Activity 10 gives "no Android toolchain on the
measuring machine" as a reason these were unmeasured. `adb` is in fact already
present, inside Unity's own Android SDK, and `Tools/measure_device.sh` finds it
there. Nothing needs installing.

## Reading the voice latency honestly

Five columns, because the one number everybody wants — how long the child waits
after speaking — is not directly observable. The plugin gives no end-of-speech
callback, so the columns bracket it instead:

- **`lastPartialToResultMs`** is the figure to quote. Partial transcripts stop
  arriving when the talking stops, so the last one is the closest honest mark
  for "the child finished speaking". Only valid where `partialCount` is above
  0 — some devices send no partials at all.
- **`speechToResultMs`** is the fallback for those rows, and it is an **upper
  bound**: it still includes the time spent saying the word.
- **`verdictMs`** is this project's own code — canonicalisation and the variant
  match — and nothing else. Expect microseconds. `Tools/perf_probe` measures
  5.64 µs on a desktop.

The gap between the first and the last is the finding. If the wait is long, it
is the device's recognizer and its network call, not SALINLAHI's code, and the
paper should say which.

Rows with `nothing_heard` are counted too. The time spent before a failure is
time the child waited, and leaving those rows out of the average would flatter
the result.

## Thresholds to judge against

These are Activity 10's own, not new ones, so a measurement can be reported as
pass or concern rather than as a bare number:

| Figure | Concern above |
|---|---|
| Startup to the menu | about 3 seconds |
| Scene load to a chapter scene | about 1.5 seconds |
| Resident memory | any growth across repeated scenes; audio above about 60 MB |
| Speech recognition wait | about 1.5 seconds, which is where a child gives up and taps |
| Voice judgement per attempt | about 1 ms on a device |

Frames per second had no stated threshold, because it had never been measured.
Judge it against the screen's refresh rate, which
`salinlahi_performance_device.txt` records — a 60 Hz panel cannot show more than
60 fps, so the ceiling is a fact about the phone and not about the game. Read
`p99FrameMs` next to `avgFps`: a 60 fps average with a 120 ms worst frame is a
game that visibly hitches, and a child notices the hitch rather than the
average.

## What a build has to contain

The phone must be running a build made from a commit that has `PerfSampler` and
the latency columns. An older APK — v0.1.1 included — writes no performance CSV
and no latency columns, and the script will say so rather than quietly pull an
old file.

Startup and resident memory are the exception: those two come from `adb` alone,
so they can be measured on **any** installed build, v0.1.1 as it stands
included. Frames per second and the voice latency need the new build.

## The honest limit on all of it

One phone is one phone. The v0.1.1 device test is already reported as a
single-device result, and these figures are the same: they describe the handset
named in section 1 of the evidence file and nothing else. Report them that way.
A second handset, especially a slower or lower-memory one, is worth more than
repeating the run on the first.
