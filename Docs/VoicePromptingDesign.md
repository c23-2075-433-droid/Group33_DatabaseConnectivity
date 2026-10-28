# Voice Prompting & Narration Design

**SALINLAHI: A Voice-Interactive Filipino Language Learning Game**
Group 33 — BSIT Capstone

Scope: how the game **tells the player which Filipino word to say**, and how it
answers back while listening. Nothing here changes a level, a gameplay rule, or
an existing screen. Every piece below either reuses something the project
already has (`SceneObjectiveController`, `VoiceInteractionUI`, the
`word_<word>.wav` clip library) or is a new, self-contained component bolted on
beside it.

**Status: implemented** (see §6). It runs with no recordings — every line
appears as an on-screen Filipino caption, paced as though it had been spoken —
so the flow below is playable today and the voice-over slots in later without
further code changes.

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
  ├─ Word badge + arrow appear over the target object, FAINT  [SEEN, not yet your turn]
  ├─ [VOICE] "Sabihin mo:" + word_<word>.wav                  [HEARD]
  ├─ subtitle bar carries the same words across the top       [READ]
  └─ on clip end → badge brightens to full, the word starts being
     matched, mic resumes, mic icon starts pulsing            [READY TO LISTEN]

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

The faint badge is the whole point of the hold: the child sees where the word
belongs while Rachel introduces it, and the word only becomes theirs to say
when she stops. Nothing is matched during that window, so an eager answer over
the top of the prompt cannot be scored.

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
| **Prompting** (Rachel speaking) | frame + word clip | still, no pulse | **visible but faint** (`dimmedPromptAlpha`, 0.45) | subtitle: "Sabihin mo: LAKAD" |
| **Ready to listen** | *(silence; optional "Nakikinig ako" once per scene)* | **pulsing** — `VoiceInteractionUI.PulseRoutine`, already built | **brightens to full** | "Nakikinig…" |
| **Heard something, checking** | — | pulse stops, icon held bright | visible | the partial text, as now (`OnPartialResultReceived`) |
| **Correct** | "Tama!" / "Magaling!" / "Ang galing mo!" | still | **green flash, then hides** | subtitle: "Tama!" |
| **Wrong word** | "Hindi pa tama. Subukan mo ulit." | still, then resumes pulsing | **shakes ~8 px, stays up, full brightness** | subtitle: the retry line |
| **Not recognised** | "Hindi kita narinig. Pakiulit." | brief grey tint, then pulses | unchanged, stays up | subtitle: the not-heard line |
| **Mic blocked / unavailable** | "Pindutin ang mikropono para magsalita." | **red tint, no pulse** | stays up | subtitle: "Pindutin ang mikropono…" |

Every spoken line is subtitled across the top of the screen, recorded or not
(`VoiceNarrator.captionMode = Always`). The subtitle is not a stand-in for the
missing voice-over - it stays once the clips are recorded, so the child can read
along with Rachel and the game still works on a muted phone. `WhenNoAudio` is
the opt-out if you ever want them to disappear as the recordings land.

A word that has already been missed stays **bright**: the dimming applies only
while a word is being introduced for the first time. A child who got it wrong
needs to keep seeing the word at full strength.

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
`SceneObjectiveController.PlayCurrentInstruction()`). Four upgrades, all built,
no new UI:

1. **Replay plays the full prompt**, not just the bare word:
   `"Pakinggan mo ulit:"` + `word_<word>.wav`.
2. **Press it twice** within 3 s and the word is said **slowly**, at
   `pitch 0.85`. No extra recording
   (`VoiceInteractionUI.slowReplayWindow`).
3. **Automatic replay** after the 2nd miss on a word, so a child who cannot
   reach the button still gets help. This is the single most important
   accessibility behaviour in the whole system
   (`VoicePromptDirector.missesBeforeHint`).
4. **Replay never counts as an attempt.** `RunScoreCounter` increments on
   `OnAnswerChecked`, and replay does not raise that event. Asking for help
   should never cost score — worth stating in your documentation.

Optional, if time allows: long-press the badge to hear the English gloss
("LAKAD — walk"). Useful for parents; not required for the capstone.

---

## 6. Implementation — built

The system is implemented and in the repository. It runs **with no recordings
at all**: every line Rachel would say appears as an on-screen Filipino caption
and the pause the clip would have taken is still held, so the flow, the pacing
and the vocabulary are all playable and demonstrable today. Dropping the .wav
files in later only adds the sound.

### 6.1 New files

| File | What it does |
|---|---|
| `Assets/Scripts/VoiceNarrator.cs` | Rachel's voice. One `AudioSource`, one queue, so two lines can never overlap. Plays a line and subtitles it. Suspends the microphone while speaking. Builds its own subtitle bar in code. |
| `Assets/Scripts/VoicePromptDirector.cs` | Decides **what** she says and **when** — prompts, feedback, hints, idle nudges, scene intro/outro. All of it hangs off events the scene already raises. Also contains `VoicePromptBootstrap`. |
| `Assets/Scripts/NarratorScript.cs` | The script, compiled in: the id of every line, and the Filipino text of the short reusable ones. This is what makes the game prompt correctly on a fresh checkout with no assets. |
| `Assets/Scripts/NarratorVoiceBank.cs` | A `ScriptableObject` holding every line's text + clip, loaded at runtime from `Assets/Resources/`. |
| `Assets/Editor/NarratorVoiceBankTool.cs` | `Tools > SALINLAHI > Refresh Narrator Voice Bank` — regenerates the bank from `Docs/VoiceOverScript.csv` plus whatever is in `Assets/Audio/VO/`. |

### 6.2 Changes to existing files

Small and additive. No level, no gameplay rule and no existing screen changed.

**`SceneObjectiveController.cs`**
- New `OnObjectiveShown` event, raised when a word actually goes live. This is
  the single thing the narration hangs off, and it is why nothing narrates
  mid-walk: the controller does not raise an objective while Kylo is moving.
- New `CurrentObjective` property, backed by an `objectiveLive` flag.
  `currentIndex` alone could not answer "is a word being asked right now?" —
  `AdvanceWhenActionFinishes` moves it on to the next objective while that
  objective is still waiting to be shown.
- A new word is announced **faint** and only goes live when the narrator stops:
  `ShowObjective()` puts the badge up at `dimmedPromptAlpha`, raises
  `OnObjectiveShown` (which is what makes the director speak), then waits on
  `VoiceNarrator.IsSpeaking` before `GoLive()` brightens the badge and hands the
  word to the recognizer. Dimming uses a `CanvasGroup` added on demand, so the
  badge, its text and its arrow fade together and **no scene builder changes**.
  `maxNarrationWait` caps the wait so a line that never ends cannot strand a
  word unanswerable, and `CancelReveal()` drops a pending reveal when the scene
  is paused or moves on. Turn the whole behaviour off with
  `holdPromptUntilNarrationEnds`.
- `PlayCurrentInstruction()` now goes through the narrator, so the replay
  button says *"Pakinggan mo ulit: LAKAD"* instead of a bare word and cannot
  talk over her. It also no longer replays between words — pressing replay
  mid-walk used to play the **next** word's clip, giving away a word the
  player had not been asked for.

**`VoiceCommand.cs`**
- `SuspendListening()` / `ResumeListening()`, plus guards so a suspended
  recognizer cannot be restarted by the continuous-listening loop and so the
  result `Cancel()` delivers is not mistaken for the player's answer.
- `OnNothingHeard` — raised only when the player **actually spoke** and the
  recognizer could not make it out. Silence does not raise it: the recognizer
  times out every few seconds while nobody is talking, and a game that said
  *"Hindi kita narinig"* every five seconds at a child who is still thinking
  would be unbearable. Pure silence belongs to the idle nudge instead. This
  uses the plugin's `OnBeginningOfSpeech` callback, which was empty before.
- `OnMicrophoneUnavailable` — permission denied or no recognizer on the device.

None of these change what is scored. `OnAnswerChecked` fires exactly as
before, so `RunScoreCounter` and the level-complete panel are untouched.

### 6.3 Why no scene builder changed

The scenes are generated — anything added to one by hand is wiped the next
time someone runs `Build Bahay Scene 2`. So nothing is added by hand:
`VoicePromptBootstrap` creates the director at runtime, with
`[RuntimeInitializeOnLoadMethod]` plus `sceneLoaded`, in every scene that has
a `SceneObjectiveController`. It runs after every `Awake` and before every
`Start`, so it is subscribed in time for the scene's first objective. The
narrator creates itself on first use. Nothing in a saved scene refers to
either, so a rebuild cannot break them.

### 6.4 Running it now, before any recordings

Open any chapter scene and press Play. Rachel's lines appear as captions
across the top of the screen in the right places and with the right pacing.
In the Editor, the existing keyboard fallback drives the whole loop:

- watch a new word appear faint while Rachel introduces it, then brighten —
  **Enter** during the faint window reports no active objective, which is the
  on-device behaviour too (the mic is suspended while she speaks)
- **Enter** — say the word currently being asked (correct answer → *"Tama!"*)
- **X** — a deliberately wrong answer (→ *"Hindi pa tama. Subukan mo ulit."*,
  then the automatic slow repeat on the second miss)
- wait 10 s — the idle nudge, then the prompt repeated every 20 s
- the replay button — *"Pakinggan mo ulit: LAKAD"*

The voice bank is optional at this stage; `NarratorScript` carries the text.

### 6.5 Adding the recordings

1. Record and drop the files in `Assets/Audio/VO/`, named as in
   `Docs/VoiceOverScript.csv` (`rachel_frame_sabihin.wav`, `word_lakad.wav`, …).
2. Run **Tools > SALINLAHI > Refresh Narrator Voice Bank**.
3. That is all. Each line switches from subtitle-only to subtitle-plus-audio
   the moment its clip exists. Subtitles stay on by default
   (`VoiceNarrator.captionMode = Always`) — they are a feature, not a
   stand-in. Set the mode to `WhenNoAudio` if you ever want them to disappear
   as the recordings arrive, or `Never` to turn them off.

Start with the 12 shared lines and Level 1's 20 words — that is enough for a
fully voiced Level 1 demo.

### 6.6 Tuning

Everything worth arguing about is a field in the Inspector, on the
`VoicePromptDirector` and `VoiceNarrator` objects that appear in the hierarchy
at runtime: `missesBeforeHint`, `missesBeforeReplayHint`, `idleNudgeSeconds`,
`idleRepromptSeconds`, `speakSceneLines`, `cueListeningOnFirstWord`,
`captionMode`, `captionBarHeight`, `slowPitch`, `tailPause`. The hold itself is
on `SceneObjectiveController`, which is part of each scene:
`holdPromptUntilNarrationEnds`, `dimmedPromptAlpha`, `maxNarrationWait`.

To change a default permanently, edit the field initialiser in the script —
the objects are created at runtime, so Inspector edits last only for that Play
session.

### 6.7 Still to do

- **Record the clips.** Nothing else in the plan is blocked on anything.
- **Test suspend/resume on a real phone with the speaker at full volume.**
  This is the one step that can misbehave on device and cannot be verified in
  the Editor, where the speech plugin does not run.
- **Scene intro/outro lines** stay silent until the voice bank is generated —
  they are long, per-scene English story lines, and a caption bar holding the
  screen for one is worse than nothing. The short Filipino prompting lines do
  not have this restriction.
- Optional: long-press the badge for the English gloss.

### 6.8 Recording notes

See `Assets/Audio/VO/README.txt`, which now also covers the prompt frames:
record them in the same session, same mic and same distance as the word clips
or the join is audible; give them a rising, unfinished intonation; trim the
tail hard, because 200 ms of air at the end of a frame is a stutter before
every single prompt.

### 6.9 Risks

| Risk | Mitigation |
|---|---|
| Recognizer hears Rachel through the speaker and scores it as the player | Listening is suspended for the whole run of queued lines and results arriving while suspended are discarded. Still verify on device at full volume. |
| `Cancel()` delivers its result late, after listening resumed | The window is far shorter than the shortest line, so in practice it lands while still suspended. If a phantom wrong answer ever shows up right after a prompt, this is the first place to look. |
| `fil-PH` unavailable on the test device | Already handled — `PronunciationChecker` is fuzzy and `VoiceCommand` falls back to the device language. Keep the per-word `variants` list filled for words that mis-transcribe. |
| Narration feels slow on replay | `VoiceNarrator.StopSpeaking()` drops everything mid-clip; `tailPause` and `secondsPerWordWhenSilent` tune the pacing. |
| Clips not recorded by the defense date | The game prompts in Filipino on screen regardless. Nothing is blocked. |
