# Voice-over still missing

**15 of the 432 script lines have no clip**, and all fifteen need a human
voice. Re-check at any time with `python3 -I Tools/completeness_report.py`.

| Segment | Voiced | Missing |
|---|---|---|
| Cue | 0 / 8 | all |
| Feedback | 0 / 7 | all |
| everything else | 417 / 417 | none |

These are the loudest gap left in the game, because none of them belongs to a
single scene: the framing before every word, and the reply to every answer
whether right or wrong, are silent everywhere.

They are also **already written out as Part 2 of `Docs/RecordingScript.md`,
numbers 51-65** — nothing new to write. The table below adds the filename each
one has to become, and when the game reaches for it.

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

## How to record them

Your normal teaching voice, quiet room, phone about a hand's width away and
off the desk. Several lines mix Filipino and English in one breath — say each
as one sentence, the way a teacher would, not as two halves.

**There is no tool that cuts these apart.** `split_word_recording.py` cuts on
silence and assigns the pieces by position; `split_by_transcript.py` asks the
recogniser which word it heard. Both exist for the 50 single words and neither
knows these fifteen sentence filenames. So record each line as its own file
and save it straight into `Assets/Audio/VO` under the name in the table —
fifteen takes, no tooling, nothing that can mislabel a clip.

Then in Unity: **Tools > SALINLAHI > Refresh Narrator Voice Bank**, which is
what links a new clip to its line.

**Do not run `voice_changer.py --words`,** which `RecordingScript.md` still
ends on. That predates the decision of 4 November to keep your own voice
rather than re-render it in Rachel's cloned one.

## What was here before

This list held 24 lines. The other nine — Story, Prompt and Praise for
Paaralan Scenes 1 and 2 — were generated and merged in PR #14, which also
wired up 233 clips that were already on disk but unreachable because the
voice bank had been left half-linked after PR #11 rewrote the asset GUIDs.
