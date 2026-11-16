# The 15 Lines to Record

Teacher Rachel, English, same voice and room tone as the existing 423 clips.
These are the most frequently heard lines in the game — every one of them fires
on almost every objective — and all 15 are currently captions with a held pause.

Save as **16-bit WAV** into `Assets/Audio/VO/` using the exact file name below.
Nothing has to be assigned by hand: the bank picks up a clip by name.

Verified missing from `Assets/Audio/VO/` at commit `5daa6b7`.

---

## Cue lines (8) — the prompting frame

| # | File name | Say |
|---|---|---|
| 1 | `rachel_frame_sabihin.wav` | Sabihin mo: |
| 2 | `rachel_frame_ngayon.wav` | Ngayon naman, sabihin mo: |
| 3 | `rachel_frame_ulitin.wav` | Pakinggan mo ulit: |
| 4 | `rachel_cue_listening.wav` | Nakikinig ako. I am listening! |
| 5 | `rachel_hint_pakinggan.wav` | Pakinggan mong mabuti. Listen carefully. |
| 6 | `rachel_hint_replay.wav` | Press the blue button to hear the word again. |
| 7 | `rachel_nudge_idle.wav` | Kaya mo 'yan! Go on, you can do it. |
| 8 | `rachel_mic_blocked.wav` | Press the microphone button so Kylo can hear you. |

**Lines 1–3 join straight onto a `word_<word>.wav` clip**, so they must end
cleanly on the colon with no falling intonation and no trailing breath — the
word follows immediately. Record them with a slight upward lift, as if the next
word is coming, because it is.

---

## Feedback lines (7) — the replies

| # | File name | Say |
|---|---|---|
| 9 | `rachel_feedback_correct_1.wav` | Tama! That's right! |
| 10 | `rachel_feedback_correct_2.wav` | Magaling! Well done! |
| 11 | `rachel_feedback_correct_3.wav` | Ang galing mo! You did it! |
| 12 | `rachel_feedback_incorrect_1.wav` | Hindi pa tama. Let's try that one again. |
| 13 | `rachel_feedback_incorrect_2.wav` | Malapit na! You are very close. One more try. |
| 14 | `rachel_feedback_nothear_1.wav` | Hindi kita narinig. I did not hear you — try once more. |
| 15 | `rachel_feedback_nothear_2.wav` | Pakilakasan mo ang boses mo. A little louder, please! |

**9–11 are rotated**, so they must sound like three different reactions rather
than three takes of one. Vary the energy.

**12–13 are the wrong-answer cues and the tone is the whole point.** A bare
"Mali" lands hard on a five-year-old — that is why the script says "Hindi pa
tama" (not yet right) rather than "wrong". Warm, unbothered, already moving on.
If these two sound disappointed the game feels punishing, which is the one
thing the design is built to avoid.

**14–15 are not wrong answers.** Nothing was recognised at all. The child may
have been too quiet or too far from the phone, so these are helpful and
practical, not corrective.

---

## Recording notes

- **12 are short** (under 2 seconds). Budget one evening including retakes.
- Keep the same mic, distance and room as the existing clips, or Rachel changes
  voice mid-scene.
- Leave ~100 ms of silence at the head and tail. `VoiceNarrator` adds its own
  `gapBetweenParts = 0.12s`, so do not pad further.
- The replay button plays lines 3, 5 and the word clip **at pitch 0.85** when
  pressed twice. Record at normal speed — the slow version is generated.
- Say the Filipino at Filipino speed and the English at English speed. These are
  bilingual lines on purpose; the Filipino carries the instruction and the
  English carries the reassurance.

## After recording

1. Drop the files into `Assets/Audio/VO/`
2. In Unity: **Assets > Refresh**
3. **Tools > SALINLAHI > Rebuild Narrator Voice Bank**
4. Re-run `python3 Tools/completeness_report.py` — "Script lines voiced" should
   move from **401 / 436** to **416 / 436**, and Cue and Feedback should both
   read full.
5. Play one scene and confirm the captions stop appearing for these lines.

Step 4 is worth doing for its own sake: it gives you a dated, re-runnable number
to quote in the defense instead of a claim.

---

## Recorded — 17 November 2026

All 15 are in `Assets/Audio/VO/`, cut from one 96-second take by
`Tools/split_cue_feedback_recording.py` and levelled to the same −20 dBFS RMS
as the other 424 clips. `Tools/completeness_report.py` now reads:

| Measure | Before | After |
|---|---:|---:|
| Script lines voiced | 401 / 444 | **416 / 444** |
| Cue segment | 0 / 8 | **8 / 8** |
| Feedback segment | 0 / 7 | **7 / 7** |

The take holds the two sections in the order **Feedback (9–15) first, then Cue
(1–8)** — not the order printed above. The splitter does not assume it: it
scores each line's phrase structure against the script's own punctuation and
picks the better of the two section orders, which is why the three-chunk
`incorrect_2`, the em-dash pause in `nothear_1` and the two English-only lines
all land where they do. It reported a fit of 1.77 against 0.87 for the other
way round.

These 15 have no mouth track, because tracks come from the text-to-speech
alignment and these are recordings. `RachelPortraitAnimator` falls back to the
talking cycle, which is the same thing the recorded `sabon`, `sipilyo` and
`tuwalya` lines already do.
