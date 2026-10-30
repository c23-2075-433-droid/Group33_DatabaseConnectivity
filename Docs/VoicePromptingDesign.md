# Voice Prompting & Narration Design

**SALINLAHI: A Voice-Interactive Filipino Language Learning Game**
Group 33 — BSIT Capstone

Scope: how the game **teaches the player a Filipino word and asks them to say
it**, and how it answers back while listening. Nothing here changes a level, a
gameplay rule, or an existing screen. Every piece below either reuses something
the project already has (`SceneObjectiveController`, `VoiceInteractionUI`, the
`word_<word>.wav` clip library) or is a new, self-contained component bolted on
beside it.

Each word is a beat of Kylo's story rather than an item on a word list:

> **Story** — "Good morning! Kylo is still sleeping. It is time to wake up!"
> **Teach** — "In Filipino, 'Bangon' means to get up, or to rise from bed."
> **Ask** — "Let's help Kylo out of bed! Say **BANGON** to wake him up!"
> **Praise** — "Great job! Kylo is finally awake."
> **Continue** — the next word's story carries straight on.

Explaining the word in English before asking for it is the point: the target
learner reads English better than Filipino, and a prompt they only repeat is a
pronunciation drill, not vocabulary teaching. A word gets the full explanation
the first time it is met in a sitting and a one-line reminder after that, so
meeting `Lakad` for the fourth time does not stop the game to define it again.

**Status: implemented** (see §6). It runs with no recordings — every line
appears as an on-screen caption, paced as though it had been spoken — so the
flow below is playable today and the voice-over slots in later without further
code changes.

---

## 1. Narration approach

**A narrating Teacher Rachel (voice + her subtitle panel) + the word badge that
is already on screen + the character's own action as the payoff.** Three
channels, one message.

| Channel | What it carries | Already exists? |
|---|---|---|
| **Rachel's voice**, subtitled in her panel | The story, the word's English meaning, and the instruction: *"Let's help Kylo out of bed! Say **BANGON**!"* | Partly — all `rachel_*` lines are written in `VoiceOverScript.csv`, none recorded |
| **Word badge** (`Prompt_<Word>`: badge + arrow + text) and the signpost | The spelling of the word and its English gloss, anchored to the thing it acts on | Yes — built by every `Build*Scene*.cs` |
| **Kylo's animation** | Proof the word worked | Yes — `onCorrect` UnityEvents |

**Why a narrator's voice and portrait rather than a character in the scene.**
Teacher Rachel already exists as scene art (`char_teacher.png`) but she only
stands in the classroom scenes (Paaralan 2, 3, 5). Putting her body into the
bedroom, the bathroom and the park would mean new art, new layout work in
eleven scene builders, and a character who breaks the fiction of the house.
Her *voice* costs nothing per scene and works in all fifteen, and the HUD
gives it a face without putting her in the room: a portrait in the ring at the
edge of her panel (§7.2). Where she is already on screen in a classroom, the
same voice reads as her speaking — a free bonus, not a dependency.

**Why not text alone.** The target learner is a young child who may not read
Filipino yet. If the instruction lives only in the badge, the game is testing
reading, not vocabulary. Audio has to be the primary carrier.

**Why not voice alone.** The badge is what the child looks at while repeating
the word, and it is the only channel that survives a muted phone, a noisy
classroom or a defense demo in a quiet room. Keep it.

### Language policy — settled

An earlier draft of this document recommended keeping the prompt itself in
Filipino (*"Sabihin mo: LAKAD"*) and reserving English for the scene intros,
on the argument that a short fixed frame heard hundreds of times is learned by
repetition. **That is no longer what the game does**, and the reason is worth
recording: a frame the child learns by repetition still never tells them what
`Lakad` *means*. A five-year-old who reads English better than Filipino was
being asked to repeat sounds, which is a pronunciation drill, not vocabulary
teaching.

The split now is:

| Line type | Language | Reason |
|---|---|---|
| Story, meaning, instruction, praise, scene intro/outro (the narration) | **English** | This is the scaffolding. It has to be understood on the first hearing, because it is what explains the Filipino word. |
| The target word itself | **Filipino** | The thing being taught. Always spoken from its own `word_<word>.wav`, so it sounds the same everywhere it appears. |
| The handful of reaction cues — *Tama!*, *Magaling!*, *Hindi pa tama*, *Pakinggan mong mabuti* | **Filipino, with a short English clause** | "Tama! That's right!" — these are heard hundreds of times and are always paired with a visible result, so they are learned by repetition without costing comprehension. This is where the immersion argument still holds. |

The scaffolding pattern is unchanged — target language for the item, support
language for the framing — but the frame is now a story rather than a command,
and the support language carries the meaning instead of only the plot.

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
  ├─ [VOICE] A — the story:   "Kylo is still sleeping. Time to wake up!"
  ├─ [VOICE] B — the meaning: "In Filipino, 'Bangon' means to get up."
  │            · full explanation the FIRST time this word is met in a
  │              sitting; a one-line reminder every time after
  │              (VocabularyMemory)
  ├─ [VOICE] C — the ask:     "Let's help Kylo up! Say BANGON!"
  │            · the word itself still comes from word_<word>.wav; the
  │              written line says where it falls with a {word} placeholder
  ├─ each of A, B, C is its own line, so each gets its OWN subtitle card
  │  in Rachel's panel rather than arriving as one paragraph   [READ]
  └─ on the last one ending → badge brightens to full, the word starts being
     matched, mic resumes, mic icon starts pulsing            [READY TO LISTEN]

PLAYER SPEAKS
  ├─ CORRECT  → badge hides, onCorrect action plays
  │             · [VOICE] "Tama! That's right!" (short, over the action)
  │             · [VOICE] D — what it did: "Great job! Kylo is finally awake."
  │             · silence until the action finishes (already handled)
  │             · then the next word's A, which carries E — the transition
  │               into the next activity — or the scene outro on the last word
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

The fifteen scenes ask for **73 words in all** — 53 distinct ones, thirteen of
which come round again in a later scene.

| Clip group | File name | Count | Reused |
|---|---|---|---|
| The words themselves | `word_<word>.wav` | 53 | One per vocabulary word, reused everywhere it appears |
| A — story | `rachel_<scene>_<word>_story.wav` | 73 | Per objective: the situation depends on where you are |
| B — full meaning | `rachel_word_<word>_teach.wav` | 53 | Per word, played on its first meeting in a sitting |
| B′ — reminder | `rachel_word_<word>_recall.wav` | 53 | Per word, played every meeting after that |
| C — the ask | `rachel_<scene>_<word>_ask.wav` | 73 | Per objective; holds a `{word}` placeholder |
| D — praise | `rachel_<scene>_<word>_done.wav` | 73 | Per objective: says what the word actually did |
| E — transition | the next word's A, or `rachel_lX_sY_outro.wav` | — | Not a family of its own, see below |
| Scene intro & outro | `rachel_lX_sY_intro/_outro.wav` | 30 | One pair per scene |
| Prompt frames (`Sabihin mo:` etc.) | `rachel_frame_*.wav` | 3 | Fallback only, when a word has no `_ask` line |
| Reactions: correct, retry, not-heard, hint, nudge, mic | `rachel_feedback_*`, `rachel_hint_*`, … | 12 | Everywhere |

That is **434 rows**, of which 379 are Rachel. Considerably more than the 12
the frame-and-word design needed — which is the real cost of story narration,
and worth stating plainly rather than discovering in the recording booth.

Three things keep it affordable:

- **Nothing has to be recorded for the game to work.** An unrecorded line is
  captioned in Rachel's panel and held for as long as it would have taken to
  say, so the whole flow plays, demos and tests today. Recordings can land one
  file at a time.
- **E costs nothing.** "Now he needs to get ready for school" *is* the next
  objective's A, so the transition is carried by a line that already exists.
  Only at the end of a scene does it need its own line, and that is the
  `_outro` row the script already had.
- **If it is still too many, cut A and D first.** Drop the `_story` and
  `_done` families and the game falls back to the generic `Tama!` cue with no
  code change — 219 rows become 73, and the teaching in B and C survives
  intact.

---

## 3. Sample dialogue

Simple English sentences, read the way you would read a picture book to a
five-year-old. Every line is in `Docs/VoiceOverScript.csv` under these exact
file names, and `Tools/check_voiceover_script.py` fails the build if one is
missing.

### 3.1 One word, start to finish

This is Bangon, the first word in the game — `Chapter1_Level1_UmagaNa`:

| Beat | File | Line |
|---|---|---|
| A story | `rachel_l1_s1_bangon_story.wav` | "Good morning! Kylo is still sleeping. It is time to wake up and start his day!" |
| B teach | `rachel_word_bangon_teach.wav` | "In Filipino, 'Bangon' means to get up, or to rise from bed." |
| C ask | `rachel_l1_s1_bangon_ask.wav` | "Let's help Kylo out of bed! Say {word} to wake him up!" |
| — | | *child says* **BANGON** |
| cue | `rachel_feedback_correct_1.wav` | "Tama! That's right!" |
| D praise | `rachel_l1_s1_bangon_done.wav` | "Great job! Kylo is finally awake." |
| E next | `rachel_l1_s1_tayo_story.wav` | "Kylo is awake, but he is still sitting on his bed. He needs to stand up first." |

Five cards in Rachel's panel, one at a time. The `{word}` placeholder in C is
replaced with **BANGON** and spoken from `word_bangon.wav`, so the instruction
can lead into the word — *"Say {word} to wake him up!"* — or carry on past it,
which is what lets a noun be taught as a noun and used as a verb:

> **Damit** — "In Filipino, 'Damit' means clothes, like Kylo's school uniform."
> then "Help him put them on — say **DAMIT**!"

### 3.1.1 Met again, later

Lakad is taught in the bedroom and comes back four more times. The second
meeting onward replaces B with its reminder and nothing else changes:

| Beat | Line |
|---|---|
| A | "It is a sunny afternoon, and Kylo has nowhere else he needs to be." |
| B′ | "Remember, 'Lakad' means to walk." |
| C | "Start him off — say **LAKAD**!" |

`VocabularyMemory` decides which of the two plays, and forgets everything when
the game closes — a child coming back after a few days is taught the word
properly again rather than handed a reminder of something they no longer know.

### 3.1.2 Prompt frames — fallback only

| File | Filipino | English gloss | Use |
|---|---|---|---|
| `rachel_frame_sabihin.wav` | **"Sabihin mo:"** | "Say:" | Only when a word has no `_ask` line written |
| `rachel_frame_ngayon.wav` | **"Ngayon naman, sabihin mo:"** | "Now, say:" | As above, for the 2nd and later words |
| `rachel_frame_ulitin.wav` | **"Pakinggan mo ulit:"** | "Listen again:" | Replay button and the 2-miss hint |

The first two are what the game said everywhere before the story beats existed.
They are kept so a half-written script still prompts rather than falling
silent, and so `tellTheStory` can be switched off on `VoicePromptDirector` to
hear the original drill for comparison. The third is still in live use: a child
who has missed twice needs the word again, slowly, not the story again.

### 3.2 Listening state

| File | Filipino | English gloss | When |
|---|---|---|---|
| `rachel_cue_listening.wav` | **"Nakikinig ako."** | "I'm listening." | Optional, first objective of a scene only — after that the pulsing mic says it without words |
| *(no clip)* | on-screen: **"Nakikinig…"** | "Listening…" | Small label under the mic while `VoiceCommand.IsListening` is true |

### 3.3 Correct

| File | Line |
|---|---|
| `rachel_feedback_correct_1.wav` | **"Tama!** That's right!" |
| `rachel_feedback_correct_2.wav` | **"Magaling!** Well done!" |
| `rachel_feedback_correct_3.wav` | **"Ang galing mo!** You did it!" |

Rotate in order, not at random, so the same one never lands twice in a row.
Keep each under 1 s — it plays on top of Kylo starting to move, and the
scene's own praise line (D) follows straight after it.

### 3.4 Wrong word

| File | Line | When |
|---|---|---|
| `rachel_feedback_incorrect_1.wav` | **"Hindi pa tama.** Let's try that one again." | 1st miss |
| `rachel_feedback_incorrect_2.wav` | **"Malapit na!** You are very close. One more try." | 3rd miss and after |
| `rachel_hint_pakinggan.wav` | **"Pakinggan mong mabuti.** Listen carefully." | Prefixes the 2-miss auto-hint |

> **Wording note — decided, keep.** The script originally had
> `rachel_feedback_incorrect_1` as *"Mali, subukan muli."* A bare **"Mali!"**
> is a hard word to hear repeatedly at five years old, and the child will hear
> it often — Tagalog recognition is noisy. *"Hindi pa tama"* ("not yet
> correct") says the same thing and keeps the door open.
>
> Three feedback lines differ from the original script for this reason, and
> Kyla confirmed on 29 Oct 2026 that they stay:
>
> | id | original | now |
> |---|---|---|
> | `rachel_feedback_incorrect_1` | "Mali, subukan muli." | "Hindi pa tama. Subukan mo ulit." |
> | `rachel_feedback_incorrect_2` | "Hindi pa tama, subukan ulit." | "Malapit na! Ulitin mo." |
> | `rachel_feedback_correct_3` | "Ang galing!" | "Ang galing mo!" |
>
> The third is not a rewrite but a correction: the CSV and
> `NarratorScript.Defaults` disagreed, and the game was already saying "Ang
> galing mo!". Worth having the reasoning to hand if a panel asks why the
> recording script does not match the written one.
>
> **Extended 30 Oct 2026, with the move to story narration.** Each of these
> lines keeps its Filipino opening and gains a short English clause — "Tama!
> That's right!", "Hindi pa tama. Let's try that one again." The Filipino is
> what the child learns by repetition; the English makes sure a first-time
> player knows whether they got it right. Two lines went fully English,
> because neither is a reaction the child is meant to learn — they are
> instructions about the device: `rachel_hint_replay` ("Press the blue button
> to hear the word again") and `rachel_mic_blocked`.

### 3.5 Nothing recognised

| File | Line | When |
|---|---|---|
| `rachel_feedback_nothear_1.wav` | **"Hindi kita narinig.** I did not hear you - try once more." | Recogniser returned an error or empty result |
| `rachel_feedback_nothear_2.wav` | **"Pakilakasan mo ang boses mo.** A little louder, please!" | Two no-hears in a row |
| `rachel_nudge_idle.wav` | **"Kaya mo 'yan!** Go on, you can do it." | 10 s of silence |
| `rachel_hint_replay.wav` | "Press the blue button to hear the word again." | 4th miss |
| `rachel_mic_blocked.wav` | "Press the microphone button so Kylo can hear you." | Permission denied, or recognition unavailable |

### 3.6 Scene and level boundaries

`rachel_lX_sY_intro`, `rachel_lX_sY_outro`, `rachel_lX_complete_summary`,
`rachel_lX_complete_badge`, all in `VoiceOverScript.csv`.

The outro carries beat E at the end of a scene, where there is no next word to
carry it — "Nice work! Kylo is out of bed. Now let's help him get to the
bathroom." Four scenes had no outro at all and now have one: `l2_s5`, `l3_s1`,
`l3_s3`, `l3_s5`.

The intros were also rewritten not to repeat their own first story beat. The
old pair opened "Good morning! Kylo needs to get ready for school" and then
"Good morning! Kylo is still sleeping", which sounded like a skip.

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
| `Assets/Scripts/VocabularyMemory.cs` | Which words have already been explained in this sitting, so the second meeting gets a reminder instead of the whole explanation. Static, like `RunScore`. |
| `Tools/check_voiceover_script.py` | Reads the word list out of the fifteen scenes and fails if the script does not cover it. This is what stops the CSV drifting away from the build again. |

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

**The story-narration change on top of that (30 Oct 2026)** is smaller than it
sounds, because the pieces above already carried it:

- `VoicePromptDirector.HandleObjectiveShown` queues three lines (story, meaning,
  ask) where it used to queue one, and `HandleObjectiveCompleted` queues the
  scene's praise line after the generic cue. Everything else — misses, hints,
  idle nudges, scene lines — is untouched.
- `VoiceNarrator.SpeakAsk` is the one new mechanism: it splits an instruction
  on its `{word}` placeholder so the word is still spoken from its own
  recording wherever it falls in the sentence. `SpeakPrompt` stays as the
  fallback. A `maxCaptionChars` guard breaks an over-long caption at sentence
  ends rather than letting the panel truncate it; with the current script the
  longest caption is 78 characters, so it never fires.
- `NarratorScript` gains `StoryId` / `AskId` / `DoneId` / `TeachId` /
  `RecallId`, built from the same scene token `SceneLineId` already used.
- `SceneObjectiveController.maxNarrationWait` went from 15 s to 30 s. Three
  unrecorded lines paced by their captions, plus the scene intro ahead of the
  first word, run to 20.9 s at the worst point in the script (Bangon, the very
  first word of the game). The cap firing mid-sentence would hand the child a
  word while Rachel was still talking, so it sits well clear of that.
- One line in `MainMenuController` resets `VocabularyMemory` when a new run
  starts.

No scene file changed, and no scene needs rebuilding: none of them serialize
`maxNarrationWait`, and the director is still installed at run time.

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
- **Enter** — say the word currently being asked (correct answer → *"Tama!
  That's right!"*, then the scene's own praise line)
- **X** — a deliberately wrong answer (→ *"Hindi pa tama. Let's try that one
  again."*, then the automatic slow repeat on the second miss)
- wait 10 s — the idle nudge, then the prompt repeated every 20 s
- the replay button — *"Pakinggan mo ulit: LAKAD"*

The voice bank is **not** optional for the story beats. `NarratorScript`
carries only the short reusable lines; the story, meaning, instruction and
praise lines live in the CSV, so run **Tools > SALINLAHI > Refresh Narrator
Voice Bank** once before playing or Rachel will fall back to *"Sabihin mo:
LAKAD"* and skip the teaching. The console should report 434 lines, 0
recorded.

To hear a word taught in full and then again as a reminder in one sitting,
play `Chapter1_Level1_UmagaNa` (Lakad is taught there) and carry on into
`Chapter1_Level2_Banyo`, which opens on Lakad.

### 6.5 Adding the recordings

1. Record and drop the files in `Assets/Audio/VO/`, named as in
   `Docs/VoiceOverScript.csv` (`rachel_frame_sabihin.wav`, `word_lakad.wav`, …).
2. Run **Tools > SALINLAHI > Refresh Narrator Voice Bank**.
3. That is all. Each line switches from subtitle-only to subtitle-plus-audio
   the moment its clip exists. Subtitles stay on by default
   (`VoiceNarrator.captionMode = Always`) — they are a feature, not a
   stand-in. Set the mode to `WhenNoAudio` if you ever want them to disappear
   as the recordings arrive, or `Never` to turn them off.

Start with the 12 shared reaction lines and everything Level 1's first scene
needs — `word_bangon`, `word_tayo`, `word_lakad`, their three `_teach` lines,
and the nine `rachel_l1_s1_*` story, ask and praise lines, plus that scene's
intro and outro. Twenty-three files gives a fully voiced opening scene, which
is what a defence demo actually needs.

Run `python3 -I Tools/check_voiceover_script.py` after editing the CSV. It
reads the word list straight out of the fifteen scenes and fails if a line is
missing, a pronoun slipped back to "she", a line is too long for the panel, or
an instruction lost its `{word}`.

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
- **Every story beat stays silent until the voice bank is generated** — scene
  intros and outros, and each word's story, meaning, instruction and praise
  line. They live in the CSV, not in `NarratorScript`, so run **Refresh
  Narrator Voice Bank** once before playing. The short Filipino cues do not
  have this restriction.
- **Watch the pacing of the opening.** The first word of a scene now follows
  the scene intro plus three story beats — 20.9 s unrecorded at the worst
  point, which is Bangon, the very first word of the game. That is a long
  time to hold a five-year-old before they may speak. Recorded, it depends
  entirely on how fast the lines are read, and it may be worth folding the
  scene intro into the first story beat for the opening scene if it drags in
  testing. Measure it on a real child before deciding.
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
| Clips not recorded by the defense date | Every line is captioned in Rachel's panel and held for as long as it would have taken to say, so the whole story flow plays. Nothing is blocked. |
| A child loses patience during a 20 s introduction | The badge is visibly faint throughout, so the wait reads as "not your turn yet" rather than as the game ignoring them. If it still drags, cut the scene intro on scene 1 or shorten the `_story` lines — neither needs a code change. |
| A story line too long for Rachel's panel | The panel shrinks text to 24pt and then truncates silently. `Tools/check_voiceover_script.py` rejects any line over 90 characters, and `VoiceNarrator.maxCaptionChars` breaks one at a sentence end as a second line of defence. The longest caption in the current script is 78. |
| The CSV and the built scenes drift apart again | The validator reads the word list out of `Build*Scene*.cs` and `Chapter1_Level1_UmagaNa.unity` rather than from a list kept by hand, so adding a word to a scene without scripting it fails the check. |

---

## 7. The gameplay HUD

The prompting flow above decides *what* the game says. This section covers
where it is drawn: the back button, Teacher Rachel's plaque, the menu button,
the place and listening planks, and the signpost carrying the word and its
meaning.

### 7.1 Built at run time, like the director

`GameplayHUDBootstrap` is a copy of `VoicePromptBootstrap` and exists for the
same reason (§6.3): the fifteen chapter scenes are generated, Chapter 1 Level 1
is maintained by hand, and a HUD added to a saved scene would be destroyed the
next time anyone ran its builder. **No scene file and no scene builder changed.**
Re-run `Build Bahay Scene 2` and the HUD still comes back.

### 7.2 Rachel's plaque is the caption bar, re-skinned

`VoiceNarrator` still owns *what* is said, *when*, and whether it is captioned —
`captionMode`, the queue, the microphone suspension, all untouched. It now also
has a `CaptionPresenter`, and `GameplayHUD` registers itself as one. With no
presenter it draws the plain black bar exactly as before, so a scene without a
HUD still subtitles.

Assigning the presenter hands it whatever caption is on screen at that instant.
That is not politeness: the director queues the scene intro in its own `Awake`,
both bootstraps run in the same pass with no order between them, and without
the replay, whichever happened to run first would decide whether the first line
of every scene appeared.

### 7.3 The listening panel, and why it does not flicker

`VoiceStatusMachine` is a plain class with no UI in it, because the flicker
rules are the only genuinely fiddly part here and they are easier to change
after a phone test when they are not tangled in RectTransforms.

Two things make the naive version strobe. `VoiceCommand` listens
*continuously*, so `IsListening` drops and returns every few seconds while the
child is simply thinking — that must be hidden. And the narrator *suspends* the
microphone for every line — that must be shown, because the child really should
not talk over Rachel. So: Rachel outranks the microphone states; a drop in
listening is disbelieved for `listeningGrace` unless she caused it; and any
state holds for `minDwell` before another may replace it, except an answer,
which is immediate on the way in and holds `feedbackHold` on the way out.

`PROCESSING...` needed one additive event. `OnBeginningOfSpeech` was an empty
callback; it now raises `VoiceCommand.OnSpeechBegan`. Nothing about recognition
or scoring changed. Note it cannot be seen in the Editor — `SimulateSpeech`
goes straight to a result — so it is device-only to verify.

The microphone icon and the plank under it are driven from the *same* decision,
via `VoiceInteractionUI.SetVisual`. Two state machines fed by the same events
with their own timers is how you get a mic pulsing while the label says TRY
AGAIN.

### 7.4 The word signpost

Driven by `OnObjectiveShown`, not `CurrentObjective`, because that property is
deliberately null while a word is still being introduced (§6.2) — which is
right for the listening panel and wrong for the signpost, which must show the
word Rachel is saying. It follows the badge's own `dimmedPromptAlpha`, so the
two cannot drift apart.

The English meaning comes from the CSV's **English Gloss** column, which
`NarratorVoiceBankTool` used to discard. It now carries it into
`NarratorLine.gloss`, and `NarratorScript.GlossFor` reads it. With no voice
bank generated the meaning line is simply left out and the word still shows.

### 7.5 Art, and why there is a UITheme asset

A runtime script cannot call `UIFont.Get()` or `AssetDatabase.LoadAssetAtPath` —
both are Editor-only. `Assets/Resources/UITheme.asset` holds *references* to the
fonts and sprites, generated by `Tools > SALINLAHI > Refresh UI Theme`, the same
shape `NarratorVoiceBank` uses for clips. Nothing is copied into Resources.

The panels were cut from six flat 1920x1080 sheets by
`Tools/cut_hud_assets.py`, which keys the white out, recovers the soft edges,
crops to the art, and measures where Rachel's face goes inside the branch ring
so the layout does not have to be told.

The microphone is the one the scene builder already placed: `GameplayHUD` moves
and re-skins it at run time rather than drawing a second one, so
`VoiceInteractionUI` keeps driving it and no scene is touched. The replay button
moves with it — it is not in the design sketch, but it is how a child who has
forgotten the word hears it again without being scored for asking.

### 7.6 Settings

Master volume and Rachel's own volume. There is no music and no sound effects
in the project, and no AudioMixer, so those are the only two channels that
exist; `GameplayHUDLayout.BuildSettings` marks where more would go.

Opening the panel pauses the sequence through `SetPaused(true)` — the same path
a door transition uses — so the child is not still being asked for a word, and
the recognizer not still listening to the room, while the sliders are open.
The sliders change what is *heard*; they cannot mute the microphone, and the
panel says so.

`VolumeSettings` now holds the keys that were private to `MainMenuController`,
which also fixes a real bug: the saved volume used to be applied only in that
one `Start()`, so entering a gameplay scene directly ignored it.

### 7.7 Still to do

- **Teacher Rachel's portrait.** `UITheme.rachelPortrait` is optional and
  currently unset, so the branch ring frames the room behind it. The panel
  reads without her; she is a drop-in once the sprite exists.
- The signpost sits low on the right, and the word badges in a few scenes
  (Paaralan 5, Parke 4, Paaralan 2) reach down to y = -350. Worth a look in
  those scenes; moving a badge in its builder is cheaper than moving the HUD.
