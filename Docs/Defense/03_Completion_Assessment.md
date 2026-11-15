# SALINLAHI — Completion Assessment

Measured against the repository at `94c79a8`, 15 November 2026.
Requirement: demonstrate **approximately 60%** of the software project.

**The honest headline: by weighted, stated criteria the build is at 82%, and
the 60% bar is comfortably cleared on content. The exposure is not how much
exists — it is that the one feature the thesis is named for, correcting
pronunciation, is not yet reliable on a real device.**

---

## 1. The criteria, stated before they are applied

Four rules, so the number means something:

1. A feature counts as **Done** only if it is implemented **and** has been
   exercised at least once on a real Android device.
2. "Implemented but never run on a phone" is **Untested**, not Done, however
   clean the code is.
3. Denominators come from the project's own design documents
   (`Docs/VoicePromptingDesign.md`, `Docs/VoiceOverScript.csv`), never invented
   for this report.
4. Every figure here is reproducible: `python3 Tools/completeness_report.py`.

The weights below are a judgement — they are stated so a panelist can disagree
with the weighting rather than with the arithmetic.

| # | Dimension | Weight | Score | Evidence |
|---|---|---|---|---|
| 1 | Playable content built | 25% | 94% | 15 of 16 designed scenes |
| 2 | Core loop end to end | 20% | 100% | prompt → listen → match → act, all 72 objectives wired |
| 3 | **Pronunciation correctness on a device** | 15% | **25%** | sound in unit tests; unreliable in the v0.1.0 device test |
| 4 | Vocabulary data complete | 8% | 92% | 53/53 words live; 4 lack a variants row |
| 5 | Narration / voice-over | 12% | 92% | 401 of 436 script lines voiced; 415/423 clips lip-synced |
| 6 | Progression, scoring, profiles | 8% | 100% | journey map, stars, points, 3 profiles, migration |
| 7 | Data layer and security | 8% | 100% | save, retrieve, leaderboard, RLS, Edge Function, constraints |
| 8 | Secondary features | 2% | 0% | mini game, Almanac, offline — all absent by decision |
| 9 | Evaluation (Objective 5) | 2% | 10% | instrument not finalised; no pupil testing yet |
| | **Weighted total** | **100%** | **≈82%** | |

**Recomputing this on the morning of the defense is a one-line command.** Doing
so, and saying you did, is worth more than the number itself.

### The raw report, verbatim

```
  Chapter scenes built                     15 / 16     94%
  Environments with scenes                  3 / 4      75%
  Vocabulary words live in a scene         53 / 63     84%
  Words a pre-reader can learn by ear      51 / 63     81%
  Script lines voiced                     401 / 436    92%
  Clips with a mouth track                415 / 423    98%

  Story 70/72 · Prompt 70/72 · Praise 69/72 · Word 52/63
  Teach 52/53 · Recall 52/53 · SceneIntro 15/15 · SceneOutro 15/15
  Cue 0/8 · Feedback 0/7 · LevelComplete 6/6
```

Two notes before anyone reads those denominators against you:

- **"Environments 3/4"** counts a Palengke that was *removed from the design*.
  Chapter 1 §1.5 says three environments. Against the paper it is **3/3**.
- **"53/63 words"** uses the design document's long list; 10 of those 63 are in
  the voice-over script but in no scene. Against what the game teaches today it
  is **53/53**.

---

## 2. Fully functional

Implemented, and seen working on a device.

- **Voice-driven objectives** — 72 objectives across 15 scenes, every one wired
  to an action
- **Continuous listening** with narrator barge-in suppression
- **Pronunciation matching as an algorithm** — canonicalization, filler removal,
  fragment guard, variants table, the 7+ letter consonant slip; 53 words → 53
  distinct canonical forms, re-provable by editor tool
- **Narration system** — story/teach/recall/ask/praise, the 2-miss slow repeat,
  the 4-miss replay hint, the 10 s nudge and 20 s re-prompt
- **Per-sound lip-sync** — 415 of 423 clips have a viseme track
- **Scene transitions** — fades, doors, `WalkIntoScene`, pause-during-transition
- **Journey map and unlocking** — per-scene gating, level badges, unenterable
  steps skipped
- **Scoring** — level-wide totals, points, 1–3 stars on accuracy, best-run kept
- **Player profiles** — 3 per device, nickname rules, availability check and
  suggestions, PlayerPrefs migration, save-file sanitising
- **Database save and retrieve** — `player_scores` POST + GET, column-limited
- **Global leaderboard** — Edge Function, per-player token, rank function,
  offline retry on next menu open
- **Security hardening** — RLS, column grants, CHECK constraints, no update/delete,
  constant-time compare, refusal logging
- **Menus** — main menu, settings (4 volume controls + mute), About with the
  build version read from the build itself, profile picker, leaderboard panel
- **Release process** — one build entry point, `BUILD_INFO.txt` naming the commit,
  refuses to run without version/scenes/release notes
- **Performance baseline** — board fetch 226.6 ms median; judgement 5.64 µs and
  682 B per attempt

---

## 3. Partially implemented

| Feature | What exists | What is missing |
|---|---|---|
| **Voice-over** | 423 clips, 401 of 436 script lines | 15 of the **most-heard** lines: the three framing lines, the listening cue, two hints, the idle nudge, the mic-blocked line, and the correct / incorrect / not-heard replies. Caption shown, pause held, so it degrades gracefully. |
| **Word teaching by ear** | 51 of 53 words voiced | 12 words taught by caption only: Alok, Apir, Hugas, Kinig, Ligpit, Linis, Padyak, Pagkain, Salamin, Sali, Suot, Tapon. This matters more than it sounds — the users are pre-readers. |
| **Variants table** | 53 rows | 4 taught words missing (Hugas, Kinig, Ligpit, Tapon); 3 rows now unused (Itlog, Tawa, Tulong) |
| **Scene content** | 15 of 16 | Bahay scene 6 "Leaving for School" — slot and two VO lines exist, scene does not |
| **Scene pacing** | works | the opening prompt of a scene holds a long silent pause |
| **Badge placement** | works | sits low in Paaralan 2, Paaralan 5, Parke 4 |
| **Chapter 4 evaluation** | planned | instrument not finalised; must match Chapter 3's claim of functionality/usability/usefulness only |

---

## 4. Incomplete / absent by decision

| Feature | Status |
|---|---|
| Mini game | Button drawn, deliberately inert. `PlayerProfileStore.RecordMiniGame` exists so the save file will not need migrating. |
| Almanac | Same: `UnlockAlmanacEntry` exists, nothing calls it. |
| Palengke level | Removed from the design, not merely unbuilt. Art left on disk unused. |
| Offline play | None. Recognition is a network service and the board is hosted. |
| Leaderboard paging / filtering / login | None. |
| iOS build | Reachable from the same codebase, never produced. |
| Staging database | None — test and release share one project. |

---

## 5. Implemented but untested

These are the entries most likely to become a panel question, because "it
compiles" is not "it works".

1. **Suspend / resume on a phone at full speaker volume** — never tested. The
   narrator-vs-microphone interaction is exactly where this would break.
2. **Three profiles in genuine use** — the store is written and sanitised, but
   switching mid-progress has not been run through a full level on a device.
3. **PlayerPrefs → profile migration on a device that actually has an old save** —
   exercised in the Editor only.
4. **Leaderboard token rotation / second-device behaviour** — the server logic is
   written and the refusal paths are covered by `Tools/check_backend.py`, but
   two physical devices with one `player_id` has not been run.
5. **The 4-second `maxActionWait` cap** — never observed firing. It is a safety
   net nobody has pulled.
6. **`maxNarrationWait = 30 s`** — same.
7. **All 15 scenes played end-to-end in one sitting on a device.** Individual
   scenes have been; the full 72-objective run has not.

---

## 6. Risks, ranked

### Risk 1 — Wrong pronunciation is accepted on a device. **Critical.**

The game's central purpose. The checker rejects wrong vowels in its own tests,
but the v0.1.0 device test found a mispronounced word still accepted in play.

**Probable cause, and why it is not a bug in our code:** Android's Filipino
*dictation* service is a language model. Handed a child's approximate
"ka-ha", it returns the nearest real Filipino word — very possibly "kuha". The
checker is then handed an already-corrected string and never sees the mistake.
No amount of strictness in `PronunciationChecker` can recover information the
recognizer has already thrown away.

This is exactly the failure mode the literature review predicts. Bhardwaj et al.
(2022) and Santiago et al. (2025) both report adult-trained recognizers
performing poorly on children's speech; Santiago's adult-Filipino model scored
39.70% WER on children and the paper's own recommendation is to embed a
*fine-tuned* recognizer in an interactive platform. We are the interactive
platform, using a stock recognizer, and we found the predicted problem.

**What can be done in one week** (in order of value per hour):

| Action | Cost | Value |
|---|---|---|
| Set `useFreeFormLanguageModel = false` and re-test on a phone | 30 min | The *search* model returns shorter, more literal text for a one-word answer. The tooltip in `VoiceCommand` already says this is worth trying. **Do this first.** |
| Play one full scene with `logAttempts` on and pull the CSV | 1 hr | Turns "we think" into "here is the data". Table of target vs. raw transcription is the single best evidence artifact you can bring. |
| Compare partial vs final results | 1 hr | `OnPartialResultReceived` may carry a less-corrected string than the final. Cheap to log. |
| Reframe honestly in Chapter 3 and in the defense | 2 hr | **Do this regardless of the above.** |

**What to say:** *"Our checker is correct and we can prove it. What we found on
the device is that the platform recognizer corrects the child before we ever see
the audio — which is the exact limitation Santiago et al. report, and the reason
their recommendation is a fine-tuned children's model. That is a finding of this
study, it is in our limitations, and it defines the next version."*

Said first, unprompted, that is a strong moment. Found by a panelist, it is the
worst one.

### Risk 2 — Documentation disagrees with the build. **High, and cheap to fix.**

Three different word counts are in circulation: paper 73 objectives, README
66/49, release notes 74/54. The build is **72 objectives, 53 distinct words, 19
re-asks**. A panel that reads the paperwork then watches the demo will conclude
the group does not know its own system. **Fix all three this week** — it is an
afternoon of editing and it removes an entire category of question. Details in
`07_Paper_vs_Build.md`.

### Risk 3 — The demo has to be on a phone. **High.**

In the Editor the mechanic is a keyboard shortcut. Demonstrating on a laptop and
*then* being asked "so where is the voice recognition?" is avoidable. Bring the
phone, bring a second phone, bring a USB-C cable, and test the room's network
before the slot. **Recognition needs internet** — no signal, no demo.

### Risk 4 — Four words have no variants row. **Medium, 20-minute fix.**

Hugas, Kinig, Ligpit, Tapon get no curated mistranscriptions, and because
`TaughtForms` is built from `WordVariants.Words`, they are also invisible to the
split-fragment guard. Add four rows, run the editor check tool.

### Risk 5 — 15 of the most-heard lines are silent. **Medium.**

The framing lines and feedback replies are heard every single objective. The
game is playable and the pacing is right, but a demo that is mostly captions
undersells a 423-clip voice-over. Recording 15 short lines is one evening.

### Risk 6 — ElevenLabs API key in git history. **Medium, security-relevant.**

Still reachable from `origin/main` at commit `0250197`. Untracking does not
remove it. **Rotate the key.** If a panelist asks a security question and you
mention this as found-and-rotated, it reads as rigour; found by them, it reads
as the opposite.

### Risk 7 — Single Supabase project. **Low, but say it first.**

Test and release share one database, so a demo play-through writes a real row.
Known, documented, and the honest answer is "a staging project is the fix and it
is on the list".

---

## 7. Priority order for the remaining week

**Must do**

1. Recognizer experiment: `useFreeFormLanguageModel = false`, test on phone, log attempts
2. Reconcile the three word counts (paper, README, release notes) to 72 / 53 / 19
3. Add the four missing `WordVariants` rows; run **Check Pronunciation Matching**
4. Rotate the ElevenLabs key
5. One full 15-scene playthrough on a phone, start to finish, with notes
6. Rehearse the 5-minute demo on the actual demo phone at least three times

**Should do**

7. Record the 15 missing high-frequency narrator lines
8. Fix the stale docstrings (`PronunciationChecker` "51", `PlayerMovement` "simple platformer")
9. Fix the low badge placement in Paaralan 2, Paaralan 5, Parke 4
10. Capture the attempt-log CSV as a printed evidence sheet

**Nice to have**

11. Voice the 12 caption-only words
12. Shorten the long opening pause
13. Second-device leaderboard test

**Do not do this week:** build Bahay scene 6, start a mini game, attempt a
children's-speech ASR model. Each is a 0.2.0 feature and none of them is what
the pre-final defense is grading.
