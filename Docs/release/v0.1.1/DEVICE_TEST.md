# v0.1.1 — Release Testing Record

Step 7 of `Docs/RELEASE_PROCESS.md`. Tested by installing
`SALINLAHI-v0.1.1.apk` from the release package onto an Android phone, not by
pressing Play in the editor: the speech plugin does not exist on desktop, so
`VoiceCommand.cs` substitutes the **Enter** key there and the editor can never
exercise the mechanic this game is built on.

**Tested by:** [CONFIRM: tester's name]

**Tested on:** [CONFIRM: phone model and Android version] — `SALINLAHI-v0.1.1.apk`
sideloaded from `Build/Release/SALINLAHI-v0.1.1.zip`. Not the development
machine, and not the Unity Editor.

**Date:** 17 November 2026

**Build under test:** version 0.1.1, versionCode 3, from commit `9e61574`, built
with a clean working tree. `BUILD_INFO.txt` in the package records the same.

**Result: all ten checks passed.** The tenth — the one v0.1.0 failed — is
written up on its own below.

| Release Test | Expected Result | Actual Result | Status |
|---|---|---|---|
| Application launches | The main menu appears | The main menu appeared | Pass |
| Version is identifiable | SETTINGS > ABOUT reads `v0.1.1` under the title | ABOUT printed `v0.1.1` | Pass |
| Microphone permission | Android asks once; the game listens after it is allowed | Asked once; the game listened once allowed | Pass |
| Main feature works | A spoken Filipino word is accepted and the action happens | Accepted; the action happened | Pass |
| Wrong pronunciation rejected | A wrong vowel is not accepted; the prompt asks again | **A deliberately mispronounced word was refused and the prompt asked again** | **Pass** |
| Required assets load | Backgrounds, characters, music and narration all present | All present | Pass |
| Scene summary works | The summary panel shows the score, stars and words learned | Shown as expected | Pass |
| Leaderboard works | A finished scene's total appears on the board | The total appeared on the board | Pass |
| Save/load works | A player profile survives closing and reopening the app | The profile survived a restart | Pass |
| Application closes normally | The app exits without hanging or crashing | Exited cleanly | Pass |

## The tenth check, which v0.1.0 failed

On 12 November, v0.1.0 accepted a deliberately mispronounced word as correct.
That was the most serious defect in the release, because SALINLAHI exists to
correct how a child says a Filipino word, and it was the reason v0.1.0 shipped
as a testing build with its right/wrong judgement marked unreliable.

On 17 November the same check passed. A word said wrong on purpose — a swapped
vowel, the same shape of mistake — was refused, and the prompt asked again.

### What changed in between

Three things, all in this release:

1. The whole word has to be said. The first syllable used to pass.
2. An accepted variant has to be the whole recognizer result, not a substring
   of it.
3. The editor's own emulator stopped answering on the child's behalf, which had
   been masking failures during development.

### What this does and does not establish

The feared explanation did not bite. It was thought the device's Filipino
dictation service might be returning the nearest real Filipino word rather than
the sounds the child actually made — handing the checker an already-corrected
word, so the mistake never reached it. If that were happening unconditionally,
no amount of strictness in `PronunciationChecker` could help, because the error
would be gone before the checker saw it. On this phone, with this word, the
mistake reached the checker and the checker caught it.

That is a real result and it is the best news in this release. It is not proof
that the system corrects pronunciation. It is **one phone, one recognizer, one
Android version and a handful of words**, and the recognizer's behaviour varies
with the device, the installed language pack and how noisy the room is. The
v0.1.0 failure was found by trying exactly once; so was this pass.

### What to do before the paper claims it

1. Run the check across several words, including the confusable pairs the
   checker's own tests cover — Kain/Kanin, Tabo/Takbo, Tawa/Tuwa, Bati/Bata,
   Kanan/Kanin.
2. Run it on at least one other phone, ideally a different Android version.
3. Read `VoiceAttemptLog` back off the device afterwards. It records what the
   recognizer actually returned for every attempt, which is the only way to see
   whether a rejection came from the checker doing its job or from the
   recognizer happening to return something odd.

Until those are done, Chapter 4 should claim that the game prompts and teaches
the vocabulary and that wrong pronunciation was rejected in testing — not that
the system reliably corrects pronunciation.

## Objective count, settled

Counted against the v0.1.1 scenes on 17 November, after the release notes had
claimed otherwise: **72 spoken objectives, 51 distinct words, 21 re-asks.** The
paper's 3.2.4 and 3.6 and Addendum A are correct and need no change.

The count comes from the `objectives` array on each scene's
`SceneObjectiveController` — the list the game actually walks through:

| Scene | Objectives |
|---|---|
| Bahay 1 UmagaNa | Bangon, Tayo, Lakad |
| Bahay 2 Banyo | Lakad, Kaliwa, Kanan, Bukas, Ilaw |
| Bahay 3 Maligo | Tuwalya, Sabon, Maligo, Sipilyo, Suklay |
| Bahay 4 Damit | Damit, Medyas, Sapatos, Bag, Kunin |
| Bahay 5 Almusal | Upo, Pagkain, Kain, Inom, Linis |
| Paaralan 1 | Lakad, Bukas, Bati, Pasok, Takbo |
| Paaralan 2 Klasrum | Bati, Upo, Kinig, Kuha, Lapis |
| Paaralan 3 Aralin | Aklat, Basa, Sulat, Sagot |
| Paaralan 4 Recess | Ligpit, Kuha, Kain, Inom, Tapon |
| Paaralan 5 Uwian | Ligpit, Tayo, Kunin, Paalam, Alis |
| Parke 1 | Lakad, Takbo, Tingin, Tuwa, Pasok |
| Parke 2 Palaruan | Akyat, Dausdos, Sakay, Tulak, Talon |
| Parke 3 Kaibigan | Kumusta, Ngiti, Tanong, Laro, Bati |
| Parke 4 Piknik | Upo, Kain, Ibahagi, Inom, Salamat |
| Parke 5 Uwi | Paalam, Lakad, Tingin, Pagod, Uwi |

Thirteen scenes of five, two of three and four: Bahay 1 and Paaralan 3.

### Where 73 came from

An earlier count here used the `Prompt_<Word>` GameObjects in each scene file
instead of the objectives array. Bahay 1 holds four of them — Alis, Bangon,
Lakad, Tayo — but only three objectives. `Prompt_Alis` is an orphan: its
fileID is not the `promptRoot` of any objective, so nothing ever activates it,
and it is saved inactive like every other prompt. It has no effect on play, and
Alis is still taught, in Paaralan 5. It is dead weight in the scene and could be
deleted next time Bahay 1 is touched; it is not worth a change to a tagged
release.

The lesson for anyone counting again: count `objectives`, not `Prompt_` objects,
and do not count `Docs/VoiceOverScript.csv`, which is a recording script and
covers words the scenes no longer ask for.

## Artifact size, verified

| | Bytes | MiB | MB as Android and Finder show it |
|---|---|---|---|
| `SALINLAHI-v0.1.1.apk` | 175,995,123 | 167.8 | 176.0 |
| `SALINLAHI-v0.1.1.zip` | 174,487,995 | 166.4 | 174.5 |

`BUILD_INFO.txt` prints `167.8 MB`, which is MiB under a loose label — the same
convention v0.1.0 used when it printed `151.3 MB`. On that like-for-like basis
the release grew **16.5 MiB**, which is the 45 voice clips added since v0.1.0.

Quote one convention and say which. If the manuscript follows Android's own
display, the number is **176.0 MB**.

> `Docs/Activity10_Performance.md` still carries v0.1.0's storage figures — a
> 151.3 MB package and 423 narration clips at 124.7 MB. Those describe the
> previous release. The current numbers are 167.8 MiB and 468 clips. That report
> has been submitted as a PDF, so it is flagged here rather than edited.

## Still open after this test

- Minimum RAM and free storage for 3.4.1 of the paper — readable off the test
  phone, still `[CONFIRM:]` in the manuscript.
- The Appendix E table of development computers and test devices.
- Eleven words still have no recording of the word itself; Rachel teaches them
  by caption.
- The MINI GAME button does nothing, and a word badge sits low in Paaralan 5.
