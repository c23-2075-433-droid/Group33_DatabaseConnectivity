# Voice-over still missing

24 of the 432 script lines have no clip. None was ever recorded — these are
gaps in the original pass, not something a merge removed. Re-check at any time
with `python3 -I Tools/completeness_report.py`.

| Segment | Voiced | Missing |
|---|---|---|
| Cue | 0 / 8 | all |
| Feedback | 0 / 7 | all |
| Story | 71 / 74 | 3 |
| Prompt | 71 / 74 | 3 |
| Praise | 71 / 74 | 3 |

---

## Group 1 — 15 lines that need your voice

These are **already written up as Part 2 of `Docs/RecordingScript.md`, numbers
51-65**; nothing new to write, they simply were never recorded. They are the
loudest gap in the game, because none belongs to a single scene: the framing
before every word and the reply to every answer, right or wrong, are silent
everywhere.

| # | Say this | File it becomes | When the game uses it |
|---|---|---|---|
| 51 | Sabihin mo: | `rachel_frame_sabihin.wav` | Fallback frame, used only when a scene has no story-based _ask line. Joins straight onto word_<word>.wav. |
| 52 | Ngayon naman, sabihin mo: | `rachel_frame_ngayon.wav` | Fallback frame for the 2nd and later words of a scene. Same join as above. |
| 53 | Pakinggan mo ulit: | `rachel_frame_ulitin.wav` | Used by the replay button and the automatic 2-miss hint. Same join as above. |
| 54 | Nakikinig ako. I am listening! | `rachel_cue_listening.wav` | Optional - first objective of a scene only. After that the pulsing mic says it without words. |
| 55 | Pakinggan mong mabuti. Listen carefully. | `rachel_hint_pakinggan.wav` | Prefixes the automatic hint after the 2nd miss on the same word. |
| 56 | Press the blue button to hear the word again. | `rachel_hint_replay.wav` | After the 4th miss - points the child at the existing replay button. |
| 57 | Kaya mo 'yan! Go on, you can do it. | `rachel_nudge_idle.wav` | After 10 s of silence, once. Then the prompt is replayed every 20 s instead. |
| 58 | Press the microphone button so Kylo can hear you. | `rachel_mic_blocked.wav` | Permission denied or recognition unavailable. |
| 59 | Tama! That's right! | `rachel_feedback_correct_1.wav` | Right-answer cue. Rotated with the other two, then the scene's own _done line follows. |
| 60 | Magaling! Well done! | `rachel_feedback_correct_2.wav` | Right-answer cue. Rotated with correct_1 and correct_3. |
| 61 | Ang galing mo! You did it! | `rachel_feedback_correct_3.wav` | Right-answer cue. Rotated with correct_1 and correct_2. |
| 62 | Hindi pa tama. Let's try that one again. | `rachel_feedback_incorrect_1.wav` | Wrong-answer cue, 1st miss. A bare "Mali" lands hard on a five-year-old - see Docs/VoicePromptingDesign.md. |
| 63 | Malapit na! You are very close. One more try. | `rachel_feedback_incorrect_2.wav` | Wrong-answer cue, 3rd miss and after. Rotated with incorrect_1. |
| 64 | Hindi kita narinig. I did not hear you - try once more. | `rachel_feedback_nothear_1.wav` | Nothing recognised at all - distinct from a wrong word, which gets incorrect_1/2. |
| 65 | Pakilakasan mo ang boses mo. A little louder, please! | `rachel_feedback_nothear_2.wav` | Two no-hears in a row. |

Record them in your normal teaching voice, the way Part 2 of that script
describes: quiet room, phone a hand's width away, each line said as one
sentence rather than as a Filipino half and an English half.

**There is no tool that cuts these apart yet.** `split_word_recording.py` and
`split_by_transcript.py` both exist for the 50 single words — one cuts on
silence and assigns by position, the other asks the recogniser which word it
heard. Neither knows these fifteen sentence filenames. So either record each
line as its own file and save it straight into `Assets/Audio/VO` under the
name in the table, which needs no tooling at all, or record one pass and ask
for a splitter to be written for them.

**Skip the `voice_changer.py --words` step that `RecordingScript.md` ends on.**
That instruction predates the decision of 4 November to keep your own voice
rather than re-render it in Rachel's cloned one.

---

## Group 2 — 9 Paaralan lines that are generated, not recorded

English narration for the two scenes that came over from Lacida's history
(`Chapter2_Level1_Paaralan`, `Chapter2_Level2_Klasrum`). The scenes arrived;
their narration never did. This is why `completeness_report.py` lists **Bukas**
and **Lakad** as taught by caption rather than by voice — both words are
voiced fine back in Bahay, but silent here.

The Filipino words themselves are already recorded
(`rachel_word_bukas_*`, `rachel_word_lakad_*`), so nothing here needs your
voice — only the English frame, with the existing word spliced in.

| File | Scene | Word | Line |
|---|---|---|---|
| `rachel_l2_s1_bukas_ask.wav` | S1 | Bukas | Ask him to open it — say {word}! |
| `rachel_l2_s1_bukas_done.wav` | S1 | Bukas | The gate swings open! |
| `rachel_l2_s1_bukas_story.wav` | S1 | Bukas | The guard is standing beside the gate, waiting to let him in. |
| `rachel_l2_s1_lakad_ask.wav` | S1 | Lakad | Walk him up to it — say {word}! |
| `rachel_l2_s1_lakad_done.wav` | S1 | Lakad | There he is, right at the gate. |
| `rachel_l2_s1_lakad_story.wav` | S1 | Lakad | Kylo can see his school now. The gate up ahead is still shut. |
| `rachel_l2_s2_lakad_ask.wav` | S2 | Lakad | Walk him over to it — say {word}! |
| `rachel_l2_s2_lakad_done.wav` | S2 | Lakad | There he is, right beside his desk. |
| `rachel_l2_s2_lakad_story.wav` | S2 | Lakad | Kylo is inside the classroom, and his own chair is across the room by the window. |

Put the nine ids in a file, say `Recordings/missing_ids.txt`, one per line,
then:

```
python3 Tools/generate_voiceover.py --ids-from Recordings/missing_ids.txt --dry-run
```

`--dry-run` prints the bill and touches neither the key nor the account, so
run that first. Without `--force`, the generator skips any clip that already
exists, so it cannot overwrite the 405 that are already right. Then:

```
python3 Tools/generate_voiceover.py --ids-from Recordings/missing_ids.txt
python3 Tools/splice_word_lines.py --ids-from Recordings/missing_ids.txt
python3 Tools/match_word_loudness.py
```

**Rotate the ElevenLabs key first.** `Tools/generate_voiceover.py` reads
`Tools/elevenlabs.key`, which is still tracked in `main` (commit `0250197`) and
must be treated as exposed. Generating against it spends a compromised
credential.

Run `match_word_loudness.py` last, or these nine will sit ~3 dB above the rest.
