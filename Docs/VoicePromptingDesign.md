# Voice Prompting & Narration Design

**SALINLAHI: A Voice-Interactive Filipino Language Learning Game**
Group 33 — BSIT Capstone

Scope: how the game **tells the player which Filipino word to say**, and how it
answers back while listening. Nothing here changes a level, a gameplay rule, or
an existing screen. Every piece below either reuses something the project
already has (`SceneObjectiveController`, `VoiceInteractionUI`, the
`word_<word>.wav` clip library) or is a new, self-contained component bolted on
beside it.

---

## 1. Narration approach

**Recommendation: a voice-only narrator (Teacher Rachel) + the word badge that
is already on screen + the character's own action as the payoff.** Three
channels, one message.

| Channel | What it carries | Already exists? |
|---|---|---|
| **Rachel's voice** (audio only) | The instruction: *"Sabihin mo: LAKAD!"* | Partly — `rachel_*` lines are written in `VoiceOverScript.csv`, none recorded |
| **Word badge** (`Prompt_<Word>`: badge + arrow + text) | The spelling of the word, anchored to the thing it acts on | Yes — built by every `Build*Scene*.cs` |
| **Kylo's animation** | Proof the word worked | Yes — `onCorrect` UnityEvents |

**Why voice-only narration rather than an on-screen narrator character.**
Teacher Rachel already exists as art (`char_teacher.png`) but she only stands
in the classroom scenes (Paaralan 2, 3, 5). Putting her body into the bedroom,
the bathroom and the park would mean new art, new layout work in eleven scene
builders, and a character who breaks the fiction of the house. Her *voice*
costs nothing per scene and works in all fifteen. Where she is already on
screen, the same voice reads as her speaking — a free bonus, not a dependency.

**Why not text alone.** The target learner is a young child who may not read
Filipino yet. If the instruction lives only in the badge, the game is testing
reading, not vocabulary. Audio has to be the primary carrier.

**Why not voice alone.** The badge is what the child looks at while repeating
the word, and it is the only channel that survives a muted phone, a noisy
classroom or a defense demo in a quiet room. Keep it.

### Language policy (a decision you need to make explicitly)

The recorded script in `Docs/VoiceOverScript.csv` is currently **English
narration with Filipino target words**. Your prompt wording is **Filipino**.
Both are defensible; mixing them at random is not. Recommended split:

| Line type | Language | Reason |
|---|---|---|
| Word prompt, feedback, hints, listening cues (the ~10 lines heard hundreds of times) | **Filipino** | Short, fixed, always paired with a visible action — the child learns the frame itself (*Sabihin mo…*, *Tama!*) by repetition. This is the immersion the thesis argues for. |
| Scene intro / outro / level-complete (the ~30 story lines) | **English, as already scripted** | They carry plot and are heard once. Keeping them means not re-recording the whole CSV, and gives the child a comprehension anchor. |

This is the standard scaffolding pattern (target language for the drill,
support language for the framing) and it is easy to defend in a panel. If your
adviser wants full Filipino, only the intro/outro rows need rewriting — the
system below does not care which language a clip is in.

---

## 2. Prompting flow

### 2.1 The rule that keeps it quiet

> **Rachel speaks only at a state change — never during an action, never on a timer that the player is already satisfying.**

Your `SceneObjectiveController` already has the exact state machine this needs:
`ShowObjective()` fires when a word becomes active, `AdvanceWhenActionFinishes()`
holds the sequence silent while Kylo is mid-walk, and `SetPaused()` covers
cutaways and door transitions. Narration hangs off those three points and
nothing else.

### 2.2 Flow for one scene

```
SCENE LOADS
  └─ Fade in
     └─ [VOICE] Rachel scene intro (English, existing rachel_lX_sY_intro.wav)
        · mic listening SUSPENDED so she is not recognised as the player
        └─ on clip end → first objective goes live

OBJECTIVE BECOMES ACTIVE  (SceneObjectiveController.ShowObjective)
  ├─ Word badge + arrow appear over the target object      [SEEN]
  ├─ [VOICE] "Sabihin mo:" + word_<word>.wav               [HEARD]
  └─ on clip end → mic resumes, mic icon starts pulsing    [READY TO LISTEN]

PLAYER SPEAKS
  ├─ CORRECT  → badge hides, [VOICE] "Tama!" (short), onCorrect action plays
  │             · silence until the action finishes (already handled)
  │             · then next objective, or scene outro
  ├─ WRONG WORD → [VOICE] "Hindi pa tama. Subukan mo ulit."  (badge stays up)
  │             · 2nd miss on the same word → auto-hint: "Pakinggan mo ulit:" + word, said slowly
  │             · 4th miss → "Pindutin ang asul na buton para marinig ulit." (points at replay)
  └─ NOT HEARD (error code / empty result) → [VOICE] "Hindi kita narinig. Pakiulit."

NO SPEECH FOR 10 SECONDS
  └─ [VOICE] idle nudge, once: "Sabihin mo na. Kaya mo 'yan!"
     · then every 20 s, replay the prompt instead of nudging
     · nudges reset the moment any speech is heard, right or wrong

ALL OBJECTIVES DONE
  └─ [VOICE] Rachel outro (existing rachel_lX_sY_outro.wav) → scene transition
```

### 2.3 When Rachel must *not* speak

- While Kylo is walking, running, climbing or being washed over by an overlay
  (`waitForActionToFinish` already gates this).
- While the sequence is paused (`SetPaused(true)` — door transitions, fades).
- Over herself. One narrator, one `AudioSource`, a queue: a new line either
  waits or cancels the old one, never layers.
- **While the microphone is live.** This is the one genuinely technical rule:
  `VoiceCommand` listens continuously, so on a phone speaker Rachel saying
  *"Sabihin mo: LAKAD"* is heard by the recogniser and scored as the player's
  own attempt. Listening is suspended for the length of every narration clip
  and resumed after. See §6.4.

### 2.4 Cost of the whole flow, in clips

| Clip group | Count | Reused |
|---|---|---|
| Word clips `word_*.wav` | 53 | Already specified; one per vocabulary word, reused across levels |
| Prompt frames (`Sabihin mo:` etc.) | 3 | Every prompt in the game |
| Correct feedback | 3 | Every correct answer |
| Retry / not-heard / hint / nudge / mic-blocked | 6 | Everywhere |
| Scene intro & outro | ~30 | Already scripted |

**12 new recordings** cover every prompt and every reaction in all fifteen
scenes. That is the whole point of splitting the frame from the word: you never
record "Sabihin mo: Lakad", "Sabihin mo: Tabo", "Sabihin mo: Sipilyo"… you
record `"Sabihin mo:"` once and the player hears it joined to a word clip you
already needed.

---

## 3. Sample dialogue

Short, beginner-level, and deliberately repetitive — the frame should become as
familiar as the words it carries. New rows have been added to
`Docs/VoiceOverScript.csv` with these exact file names.

### 3.1 Prompt frames — played immediately before the word clip

| File | Filipino | English gloss | Use |
|---|---|---|---|
| `rachel_frame_sabihin.wav` | **"Sabihin mo:"** | "Say:" | Default, ~80% of prompts |
| `rachel_frame_ngayon.wav` | **"Ngayon naman, sabihin mo:"** | "Now, say:" | Second and later words in a scene, for variety |
| `rachel_frame_ulitin.wav` | **"Pakinggan mo ulit:"** | "Listen again:" | Replay button and the 2-miss hint |

Heard by the player as one sentence: *"Sabihin mo: **LAKAD**."*

If you would rather record whole sentences for the first few words of Level 1
so the game sounds warmer in a demo, the system supports it — a per-objective
`instructionClip` overrides the frame+word pair (see §6.2). Suggested full
lines, in your own phrasing:

- "Sabihin mo ang **LAKAD** para makalakad si Kylo!" — *Say LAKAD so Kylo can walk!*
- "Gising na si Kylo. Sabihin mo: **BANGON**." — *Kylo is awake. Say: BANGON.*
- "Madilim sa banyo. Sabihin mo: **ILAW**." — *It's dark in the bathroom. Say: ILAW.*
- "Kunin natin ang bag. Sabihin mo: **KUNIN**." — *Let's get the bag. Say: KUNIN.*

### 3.2 Listening state

| File | Filipino | English gloss | When |
|---|---|---|---|
| `rachel_cue_listening.wav` | **"Nakikinig ako."** | "I'm listening." | Optional, first objective of a scene only — after that the pulsing mic says it without words |
| *(no clip)* | on-screen: **"Nakikinig…"** | "Listening…" | Small label under the mic while `VoiceCommand.IsListening` is true |

### 3.3 Correct — already in your CSV, keep them

| File | Filipino | English gloss |
|---|---|---|
| `rachel_feedback_correct_1.wav` | **"Tama!"** | "Correct!" |
| `rachel_feedback_correct_2.wav` | **"Magaling!"** | "Well done!" |
| `rachel_feedback_correct_3.wav` | **"Ang galing mo!"** | "You're great!" |

Rotate in order, not at random, so the same one never lands twice in a row.
Keep each under 0.8 s — it plays on top of Kylo starting to move.

### 3.4 Wrong word

| File | Filipino | English gloss | When |
|---|---|---|---|
| `rachel_feedback_incorrect_1.wav` | **"Hindi pa tama. Subukan mo ulit."** | "Not quite. Try again." | 1st miss |
| `rachel_feedback_incorrect_2.wav` | **"Malapit na! Ulitin mo."** | "Almost! Say it again." | 3rd miss and after |
| `rachel_hint_pakinggan.wav` | **"Pakinggan mong mabuti."** | "Listen carefully." | Prefixes the 2-miss auto-hint |

> **Wording note.** Your CSV currently has `rachel_feedback_incorrect_1` as
> *"Mali, subukan muli."* A bare **"Mali!"** is a hard word to hear repeatedly
> at five years old, and the child will hear it often — Tagalog recognition is
> noisy. *"Hindi pa tama"* ("not yet correct") says the same thing and keeps the
> door open. The row has been updated; revert it if your adviser prefers the
> original.

### 3.5 Nothing recognised

| File | Filipino | English gloss | When |
|---|---|---|---|
| `rachel_feedback_nothear_1.wav` | **"Hindi kita narinig. Pakiulit."** | "I didn't hear you. Please repeat." | Recogniser returned an error or empty result |
| `rachel_feedback_nothear_2.wav` | **"Pakilakasan mo ang boses mo."** | "Please speak a bit louder." | Two no-hears in a row |
| `rachel_nudge_idle.wav` | **"Sabihin mo na. Kaya mo 'yan!"** | "Go on, say it. You can do it!" | 10 s of silence |
| `rachel_mic_blocked.wav` | **"Pindutin ang mikropono para magsalita."** | "Press the microphone to speak." | Permission denied, or recognition unavailable |

### 3.6 Scene and level boundaries

Already written in `VoiceOverScript.csv` — `rachel_lX_sY_intro`,
`rachel_lX_sY_outro`, `rachel_lX_complete_summary`,
`rachel_lX_complete_badge`. No changes proposed.

---

## 4. Voice-recognition feedback

One state at a time, shown in three places at once: the mic icon (already
animated), the word badge, and Rachel. No new screen, no new panel.

| State | Audio | Mic icon | Word badge | On-screen text |
|---|---|---|---|---|
| **Prompting** (Rachel speaking) | frame + word clip | still, dimmed to 60% | visible, small pop-in | — |
| **Ready to listen** | *(silence; optional "Nakikinig ako" once per scene)* | **pulsing** — `VoiceInteractionUI.PulseRoutine`, already built | visible, full brightness | "Nakikinig…" |
| **Heard something, checking** | — | pulse stops, icon held bright | visible | the partial text, as now (`OnPartialResultReceived`) |
| **Correct** | "Tama!" / "Magaling!" / "Ang galing mo!" | still | **green flash, then hides** | — |
| **Wrong word** | "Hindi pa tama. Subukan mo ulit." | still, then resumes pulsing | **shakes ~8 px, stays up** | — |
| **Not recognised** | "Hindi kita narinig. Pakiulit." | brief grey tint, then pulses | unchanged, stays up | "Hindi narinig" |
| **Mic blocked / unavailable** | "Pindutin ang mikropono para magsalita." | **red tint, no pulse** | stays up | "Pindutin ang mikropono" |

Notes that matter in practice:

- **Never punish a wrong answer with a loud or slow cue.** Retry audio is
  shorter than correct audio, so the child's second attempt can start sooner.
- **The badge never disappears on a miss.** The one thing a stuck child needs
  is the word still being on screen.
- **Partial results are already displayed** by `VoiceCommand.SetStatusText`.
  Keep that on a small debug label for your defense demo — a panel can see the
  recogniser working — but hide it in the player build.
- **Distinguish "wrong word" from "not heard".** `VoiceCommand.OnAnswerChecked`
  currently fires `(spokenText, false)` for both. Treat an empty/whitespace
  `spokenText`, or an `errorCode` result, as *not heard*; anything else as
  *wrong word*. They need different lines because they need different fixes
  from the child (speak louder vs. say a different word).

---

## 5. Replay / "I forgot the word"

The replay button already exists, is already wired, and already sits beside the
mic (`VoiceUIBuilder`, `VoiceInteractionUI.OnReplayPressed` →
`SceneObjectiveController.PlayCurrentInstruction()`). Four small upgrades,
no new UI:

1. **Replay plays the full prompt**, not just the bare word:
   `"Pakinggan mo ulit:"` + `word_<word>.wav`. One line of change inside
   `PlayCurrentInstruction()`.
2. **Press it twice** (within 3 s) and the word is said **slowly** — the same
   clip played at `AudioSource.pitch = 0.85f`, then repeated once after a
   0.4 s gap. No extra recording.
3. **Automatic replay** after the 2nd miss on a word, so a child who cannot
   reach the button still gets help. This is the single most important
   accessibility behaviour in the whole system.
4. **Replay never counts as an attempt.** `RunScoreCounter` increments on
   `OnAnswerChecked`, and replay does not raise that event, so this already
   holds — keep it that way, and say so in your documentation. Asking for help
   should never cost score.

Optional, if time allows: long-press the badge to hear the English gloss
("LAKAD — walk"). Useful for parents; not required for the capstone.

---

## 6. Implementation plan

Additive. One new script, one new asset type, one small builder helper, and
four short hooks into files that already exist. No scene has to be re-laid out,
no gameplay script changes behaviour.

### 6.1 New file — `Assets/Scripts/VoiceNarrator.cs`

A single `MonoBehaviour` per scene that owns *all* spoken output. One
`AudioSource`, one queue, so two lines can never overlap.

```csharp
public class VoiceNarrator : MonoBehaviour
{
    public NarratorVoiceBank bank;      // ScriptableObject, §6.2
    public VoiceCommand voiceCommand;   // suspended while speaking

    public bool IsSpeaking { get; private set; }
    public event System.Action OnFinishedSpeaking;

    /// Frame + word, e.g. "Sabihin mo:" + word_lakad.wav
    public void SpeakPrompt(string word, AudioClip overrideClip = null);

    /// Rotating "Tama!" / "Magaling!" / "Ang galing mo!"
    public void SpeakCorrect();

    /// missCount decides: retry line, then hint, then "press replay"
    public void SpeakRetry(int missCount);
    public void SpeakNotHeard(int missCount);
    public void SpeakIdleNudge();
    public void SpeakMicBlocked();

    /// Intro/outro and anything else already recorded whole
    public void Speak(AudioClip clip);

    public void StopSpeaking();
}
```

Behaviour: `Speak*` enqueues clips; a coroutine plays them back to back with a
0.15 s gap; `voiceCommand.SuspendListening()` on the first clip and
`ResumeListening()` after the last. A missing clip is skipped silently — the
game must stay playable before a single line is recorded, exactly as
`PlayCurrentInstruction()` does today.

### 6.2 New asset — `NarratorVoiceBank` (ScriptableObject)

Holds the twelve shared clips in named fields (`frameSabihin`, `correct[]`,
`retry[]`, `notHeard[]`, `idleNudge`, `micBlocked`, …), plus a
`AudioClip WordClip(string word)` lookup for the per-word clips.

Why a ScriptableObject and not `Resources.Load`: your scenes are **generated**,
and `VoiceClipLibrary` is an Editor-only class (it uses `AssetDatabase`), so it
cannot run in a build. One asset at `Assets/Audio/VO/NarratorVoiceBank.asset`,
assigned by the builders the same way clips are attached today, survives every
rebuild and shows up in the Inspector where your team can check it.

For the per-word lookup at runtime, the simplest approach that fits your setup:
have the bank hold a serialized `AudioClip[] words` array that an editor button
(`Tools > SALINLAHI > Refresh Voice Bank`) fills from `Assets/Audio/VO/word_*`.
Re-run it after each recording session.

### 6.3 New builder helper — `Assets/Editor/NarratorBuilder.cs`

Mirrors `VoiceUIBuilder` exactly: `AddNarrator(voiceCommand, controller)`
creates the `Narrator` GameObject, assigns the bank via
`AssetDatabase.LoadAssetAtPath`, and reuses an existing one if the scene
already has it. Each `Build*Scene*.cs` gains **one line** next to its existing
`VoiceUIBuilder.BuildVoiceUI(...)` call.

### 6.4 Hooks into existing files

| File | Change | Lines |
|---|---|---|
| `SceneObjectiveController.ShowObjective()` | after setting `currentTargetWord`, call `narrator.SpeakPrompt(objective.word, objective.instructionClip)` | ~2 |
| `SceneObjectiveController.HandleAnswerChecked()` | correct → `narrator.SpeakCorrect()`; the existing `if (!isCorrect) return;` becomes `if (!isCorrect) { missCount++; narrator.SpeakRetry(missCount); return; }`, and `missCount` resets in `ShowObjective` | ~6 |
| `SceneObjectiveController.PlayCurrentInstruction()` | route through the narrator with the `"Pakinggan mo ulit:"` frame instead of its own `PlayOneShot` | ~3 |
| `VoiceCommand` | add `SuspendListening()` / `ResumeListening()` and an `isSuspended` guard in `StartListening()` and in the `listenContinuously` restart | ~15 |
| `VoiceCommand.OnResultReceived` | pass the error/empty case through a new `OnNotHeard` event so *not heard* and *wrong word* can differ | ~4 |
| New `SceneNarration.cs` (tiny) | `introClip` played in `Start()` before the first objective; `outroClip` wired to `onAllObjectivesComplete` | new, ~40 |
| New `IdleNudge.cs` (tiny) | 10 s timer, reset on any `OnAnswerChecked`; calls `narrator.SpeakIdleNudge()` | new, ~35 |

Everything else — the pulsing mic, the badge, the replay button, scoring, the
level-complete panel — is untouched.

### 6.5 Suggested order of work

| Step | Work | Effort |
|---|---|---|
| 1 | `VoiceNarrator` + `NarratorVoiceBank` + refresh button | half a day |
| 2 | Hook into `SceneObjectiveController`; test in Editor with the existing keyboard fallback (`Enter` = say the current word) | half a day |
| 3 | `SuspendListening` / `ResumeListening` in `VoiceCommand` — **test on a real phone with the speaker on**, this is the step that can bite | half a day |
| 4 | Record the 12 shared clips + Level 1's 20 word clips; drop in `Assets/Audio/VO/`; refresh the bank | one session |
| 5 | `SceneNarration` + `IdleNudge`, one scene first (Bahay Scene 1) | half a day |
| 6 | Roll out: one `NarratorBuilder.AddNarrator()` line per scene builder, rebuild all fifteen | half a day |
| 7 | Record the remaining 33 word clips and the intro/outro lines | as available |

The game is demonstrable after step 4 with Level 1 alone.

### 6.6 Recording notes (add to `Assets/Audio/VO/README.txt`)

- Mono, 44.1 kHz, WAV, silence trimmed hard at both ends. A frame clip with
  200 ms of dead air at the end is heard as a stutter before every word.
- Record the **frames** with a rising, unfinished intonation — "Sabihin mo:" —
  so they join naturally to the word that follows. Record them in the same
  session, same mic, same distance as the word clips, or the join will be
  audible.
- Record the **word clips** flat and slightly slow: these are the pronunciation
  model the child copies.
- Keep feedback clips under 0.8 s; they overlap the start of Kylo's action.

### 6.7 Risks

| Risk | Mitigation |
|---|---|
| Recogniser hears Rachel through the speaker and scores it as the player | §6.4 suspend/resume; verify on device with speaker volume at 100% |
| `fil-PH` unavailable on the test device, so prompts and recognition disagree | Already handled — `PronunciationChecker` is fuzzy and `VoiceCommand` falls back to the device language; keep the variants list filled for words that mis-transcribe |
| Narration makes the game feel slow on replay | All prompts are interruptible: any recognised speech calls `StopSpeaking()` |
| Clips not recorded by the defense date | Everything degrades to silence + badge, which is the current behaviour — nothing breaks |
