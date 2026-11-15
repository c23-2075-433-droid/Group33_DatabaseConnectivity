# SALINLAHI — The Scripts, and How to Describe Them

67 runtime scripts, 14,379 lines. These are the sixteen a panelist is likely to
open, plus the backend. For each: what it does, the parts worth naming, who it
talks to, what could be asked, and **one sentence to say out loud** if a
panelist points at it.

Format of each entry:

> **Say this:** the single sentence to lead with.

---

## Tier 1 — the four that *are* the project

### `VoiceCommand.cs` (446 lines) — the ear

> **Say this:** "VoiceCommand owns the device's speech recognizer. It turns raw
> transcribed text into one of five events, and it is the only script in the
> project that knows the speech plugin exists."

**Key members**

| Member | Why it matters |
|---|---|
| `preferredLanguage = "fil-PH"` | the locale requested from the device recognizer |
| `currentTargetWord` / `currentTargetWordVariants` | written by `SceneObjectiveController`; empty means nothing is being asked |
| `triggerWord = "talon"` | the always-on jump, checked independently of any objective |
| `listenContinuously`, `restartDelay = 0.5f` | why the child never presses anything |
| `useFreeFormLanguageModel` | dictation vs search model — a real tuning knob, worth mentioning |
| `IsListening` + `OnListeningStateChanged` | drives the mic button's pulse |
| `SuspendListening()` / `ResumeListening()` | called by `VoiceNarrator` around every line |
| `isShuttingDown`, `isSuspended`, `isInitialized` | the three guards that make async callbacks safe |
| `SimulateSpeech(text)` | the Editor fallback's entry point — **same path as real speech** |
| `ProcessRecognizedText(text)` | the single funnel: trigger word, then objective check, then log, then raise |

**Events raised:** `OnAnswerChecked(text, bool)`, `OnNothingHeard`,
`OnMicrophoneUnavailable`, `OnSpeechBegan`, `OnListeningStateChanged`.

**Talks to:** `PronunciationChecker` (judgement), `VoiceAttemptLog` (CSV),
`PlayerMovement` (the Talon jump only). Subscribers: `SceneObjectiveController`,
`RunScoreCounter`, `VoicePromptDirector`, `VoiceInteractionUI`, `GameplayHUD`.

**Likely questions:** Why a plugin instead of your own recognizer? Why does
listening restart automatically? What happens if the child talks over Rachel?
Why is `Talon` checked separately? Why does the Editor key go through
`SimulateSpeech`?

**Watch out:** `enableKeyboardFallback` is `#if UNITY_EDITOR`, so it is *not* in
the APK — say so before anyone suspects the demo is faked.

---

### `PronunciationChecker.cs` (342 lines) — the judge

> **Say this:** "This is the right/wrong decision. It does not allow mistakes —
> it folds away the variation that is genuinely free in Filipino and then
> requires an exact match, because in a language where words differ by one
> vowel, 'one letter of slack' means accepting every vowel error."

**Key members:** `IsCorrect(spoken, target, variants)`, `Canonicalize(text)`
(public, because the attempt log records it beside the raw text),
`CanonicalForm`, `Tokenize`, `MatchesForm`, `IsConsonantSlip`, `VowelSkeleton`,
`TaughtForms` (the fragment guard), `ConsonantSlipMinLength = 7`,
`RawFillerWords` (21 entries).

**Plain C#, no MonoBehaviour** — deliberately, so it is callable from the editor
self-check tool and testable without the engine. Worth saying: it costs
**5.64 µs and 682 bytes per attempt** (measured, `Tools/perf_probe`).

**Talks to:** `WordVariants` (per-word mistranscriptions and the taught-word set).
Called by `VoiceCommand` and by the editor check tool.

**Likely questions:** Why not Levenshtein? Prove two taught words can't collide.
What if the child has a lisp? Isn't 7+ letters arbitrary? What is the vowel
skeleton for?

**Watch out — fix before the defense:** the docstring still says "51 taught
words" / "51×50 cross pairs". It is 53 now. The property still holds (verified:
53 distinct canonical forms, no variant collides with another taught word) but
the comment is wrong on the page.

---

### `SceneObjectiveController.cs` (524 lines) — the conductor

> **Say this:** "One controller runs all fifteen scenes. A scene's words are
> *data* — a list of objectives, each holding a word, its variants, its prompt
> UI and the UnityEvent that fires when it is said — so adding a word is adding
> a list entry, not writing code."

**The nested `SceneObjective` class is the single most defence-relevant data
structure in the project:** `word`, `variants[]`, `promptRoot`,
`instructionClip`, `onShown`, `onCorrect`.

**Key members**

| Member | Why it matters |
|---|---|
| `ShowObjective(i)` | raises a word at `dimmedPromptAlpha = 0.45` |
| `GoLive(i)` | brightens it and writes `currentTargetWord` — *this* is "the child's turn" |
| `RevealWhenNarrationEnds` | the hold; capped at `maxNarrationWait = 30 s` |
| `HandleAnswerChecked` | the one place a correct answer advances a scene |
| `AdvanceWhenActionFinishes` | waits on `PlayerMovement.IsActionPlaying`, any `PoseSequence.IsPlaying`, any `WalkIntoScene.IsMoving`; `maxActionWait = 4 s`, then `settlePause = 0.25 s` |
| `SetPaused(bool)` + `completionPending` | nothing can be answered behind a fade, and the results panel cannot open over one |
| `PlayCurrentInstruction[Slowly]()` | the replay button; refuses between words so it cannot leak the *next* word |

**Events:** `OnObjectiveShown(objective)`, `OnObjectiveCompleted(word)`,
`onAllObjectivesComplete` (a `UnityEvent`, constructed in the field initializer
because `AddComponent` leaves UnityEvent fields null and the builders use
`AddComponent`).

**Likely questions:** Why hold the word while Rachel talks? What if an animation
hangs? Why does the next word not appear immediately? How do you add a scene?

---

### `VoiceNarrator.cs` (847 lines) — the mouth

> **Say this:** "Everything spoken goes through one object, so two lines can
> never overlap and the microphone is never open while Rachel is talking. A line
> with no recording is not skipped — its caption is shown and the pause it would
> have taken is still held, so the pacing is identical with or without the audio."

**Key members:** `Instance` (creates itself — no scene wiring),
`SpeakLine(id)`, `SpeakAsk`, `SpeakPrompt`, `SpeakReplay(word, clip, slowly)`,
`SpeakHint`, `SpeakCorrect`, `CanSay(id)`, `StopSpeaking()`, `IsSpeaking`,
`PlaybackTime`, `CurrentClip`, `CaptionPresenter` (an `ICaptionPresenter` —
`GameplayHUD` registers its wooden plaque as a skin over the default bar),
`captionMode`, `maxCaptionChars = 110`, `slowPitch = 0.85f`,
`secondsPerWordWhenSilent = 0.32f`.

**Talks to:** `NarratorScript` (line ids), `NarratorVoiceBank` (clip lookup by
name), `VoiceCommand` (suspend/resume), `RachelPortraitAnimator` (reads
`PlaybackTime` and `IsSpeaking`), `GameplayHUD`.

**Likely questions:** Why captions if the users can't read? (Answer: they are for
the supervising adult, for a muted phone, and for lines not yet recorded — and
`captionMode` can hide them once recording is complete.) What happens with no
clip? How do you stop her talking over herself?

---

## Tier 2 — the ones that make it a game

### `VoicePromptDirector.cs` (333 lines) — *what* Rachel says and *when*

> **Say this:** "VoiceNarrator says it; this decides it. Every line hangs off an
> event the scene already raises, so the design rule — Rachel speaks at a state
> change and is silent otherwise — holds by construction rather than by
> discipline."

Three lines per word: **story** ("Kylo is still sleeping") → **teach or recall**
(full English explanation the first time, one-line reminder after —
`VocabularyMemory`) → **ask** ("Say BANGON"). Separate lines so each becomes its
own subtitle card.

Help ladder: 2 misses → say it again slowly, unasked (the most important
accessibility behaviour — a child who cannot reach the replay button still gets
help); 4 misses → point at the replay button. Silence: 10 s nudge, then re-prompt
every 20 s. `OnNothingHeard` gets its own lines and is never counted as a miss.

`VoicePromptBootstrap` installs it with `[RuntimeInitializeOnLoadMethod(AfterSceneLoad)]`
— because scenes are generated and anything hand-added would be wiped.

### `PlayerMovement.cs` (572 lines) — the body

> **Say this:** "Kylo is moved by a timer, not by input. A spoken word sets a
> direction and a duration; while that timer runs, FixedUpdate forces the
> velocity instead of reading the keyboard — which is how voice control works
> on a phone with no keyboard at all."

`Jump()`, `Walk/WalkLeft/WalkRight/WalkForward`, `RunInDirection`, `Takbo()`,
`Bangon()`, `TayoUp()`, `SetAppearance(standing, walkFrames)`,
`IsVoiceWalking`, `IsPosing`, `IsActionPlaying`, `OnWakeStageChanged`,
`walkTarget` + `arrivalEaseDistance` + `arrivalEaseFloor`.

**Watch out:** the class docstring still describes it as "Simple 2D platformer
movement… Move with A/D". That is the original header and it undersells the
script badly. A one-line rewrite is worth doing.

### `WakeUpSequence.cs` (1,118 lines) — the largest script

Scene 1's bespoke choreography: the snore loop, the sit-up, the stretch, the
light, the pose settling into the bed (`sittingPoseSeatDrop = 0.43`). It is large
because Scene 1 is the only scene with **no builder** — it is kept by hand and
patched in place by `MigrateLevel1ObjectiveSystem.cs`. If asked why one script is
three times the size of the next, that is the honest answer, and "it would be the
first thing I'd refactor" is a good one to volunteer.

### `GameplayHUD.cs` (656) + `GameplayHUDLayout.cs` (561) + `UITheme.cs` (225)

The in-game furniture, built at run time from a `UITheme` ScriptableObject
(`Assets/Resources/UITheme.asset`): Rachel's branch-ring plaque, the caption,
the mic button, the replay button, the place label (`JourneyMap.PlaceLabel`).
Built at run time precisely so the 15 generated scenes do not each carry a copy.

### `RachelPortraitAnimator.cs` (312) + `VisemeTracks.cs` (149)

Nine mouth shapes, 423 viseme tracks in `Assets/Resources/VisemeTracks.json`,
walked against `VoiceNarrator.PlaybackTime`. Falls back to a three-shape cycle
driven by `IsSpeaking` when a line has no track, so an unrecorded line mouths its
caption instead of freezing. There is no Animator Controller — the HUD is built
at run time, so there is no GameObject for one to live on.

### `SceneResultPanel.cs` (400) + `WordsLearnedPanel.cs` (221)

The summary board: score, stars, and **every** word practised, uncapped. The
comment in the code is the defence: "the whole point of this line is for a child
to see what they said and a parent to see what was learned, and '+1 more'
withholds exactly that."

---

## Tier 3 — progress and data

### `RunScore.cs` (106) · `RunScoreCounter.cs` (56)

> **Say this:** "A level is five scenes but one playthrough, so the score has to
> span all five. Counting inside one scene gave '5 of 8' at the end of the
> bathroom and silently dropped the three words from the bedroom."

`PointsFor(correct, attempts) = correct × 10 + round(accuracy × 50)`.
`resetOnStart` only in a level's first scene, and in `Awake` not `Start`.
Static rather than `DontDestroyOnLoad` because there is nothing to keep alive —
two ints and a list.

### `LevelProgress.cs` (176) · `JourneyMap.cs` (175)

> **Say this:** "There is one number: how far along the journey you have reached.
> Every other question — is this scene unlocked, is this level complete, what
> opens next — is derived from it, so there is one number to get wrong instead of
> several."

Know the three bugs in `01_System_Reviewer.md` §9: skipping unenterable steps,
first-*scene* vs first-*marker*, and the end of the journey being `Steps.Length`.

### `PlayerProfileStore.cs` (469) · `PlayerProfile.cs` (153)

> **Say this:** "Progress used to be PlayerPrefs keys, which cannot hold a second
> child's record and has nowhere to keep a player id. It is now one JSON file in
> the app's private storage, with up to ten profiles — and because that file is
> ordinary JSON on the device, everything read from it is treated as input."

Name `Repair()` specifically: non-UUID id replaced (and its token discarded),
nickname re-cleaned, negative totals zeroed, stars clamped, corrupt file treated
as no file. And `Migrate()`: the old PlayerPrefs save is carried over once,
flagged, old keys left in place so a rollback still finds its save.

### `SupabaseClient.cs` (252) · `SupabaseConfig.cs` (66) · `ScoreRecord.cs` (33)

> **Say this:** "Plain `UnityWebRequest` against Supabase's REST interface — no
> SDK. Two operations: POST a record when a level is finished, GET the recent
> ones back."

Three details worth volunteering:
- `FetchRecentScores` names its four columns rather than `select=*` — "a request
  that cannot ask for a column cannot leak it", and the database now **refuses**
  `select=*` by column grant.
- `ApplyHeaders` sets `Authorization: Bearer` **only** for legacy JWT keys
  (`eyJ…`). The newer `sb_publishable_` keys are not JWTs, and sending one as a
  bearer makes PostgREST 401 everything. This is a real bug that was found and
  fixed; it is a good "what went wrong" story.
- `Log()` sends the status code, transport error and response body **to the log
  only**. Callbacks get a child-readable sentence. "A 401 with a key hint tells
  an attacker where to push and tells a six-year-old nothing."

### `SessionScoreTracker.cs` (257)

> **Say this:** "This is SAVE → RETRIEVE → DISPLAY in one place. It fires on *all
> objectives complete*, not on reaching an exit, so a record is never lost
> because the child did not walk far enough."

Order matters and is deliberate: capture totals → `RecordRun` to the device →
`RunScore.Reset()` → mark progress → show the panel → *then* the network call.
Guarded by `alreadySaved`.

### `LeaderboardClient.cs` (304) · `LeaderboardPanel.cs` (168) · `LeaderboardEntry.cs` (77)

> **Say this:** "Reading and writing go different ways round on purpose. Reads
> come straight off the table, because the anon role has a select policy and
> nothing else. Writes go through an Edge Function, because a score the client
> simply asserts is worth nothing."

`PushIfNeeded()` makes no request at all when `NeedsLeaderboardSync()` is false.
Offline is treated as normal, not exceptional: nothing blocks, nothing is lost,
the profile stays marked and retries next time the menu opens.

---

## The backend

### `supabase/functions/submit-score/index.ts` (Deno/TypeScript)

The full check list is in `01_System_Reviewer.md` §10. For the defense, four
sentences:

1. "A `player_id` is made on the device, so it is an identifier and not a secret —
   and it is not treated as one."
2. "The first submission registers the player and the server issues a 32-byte
   token; only its SHA-256 is stored, in a table the anon role cannot read."
3. "Every later submission must present that token, compared in constant time."
4. "The totals are still the client's word — nothing here can replay a child
   speaking. What it can do is refuse the absurd, and bound what a forged
   submission is worth."

Also know why `Verify JWT with legacy secret` is **off**: the project uses
`sb_publishable_` keys, which are not JWTs, so with that setting on the function
401s everything. Turning it off means the platform checks nothing, which is why
the function checks the publishable key itself (`keyAccepted`) — a gate against
traffic that never had the app, explicitly *not* authentication, because the key
ships inside the APK.

### `supabase/migrations/20261112_leaderboard.sql` · `…_player_scores_hardening.sql`

The hardening migration is the single best artifact to show for a security
question, because its header is an honest confession: the original Activity-5
policies were `with check (true)` and `using (true)` — "rules written to let a
feature work, never revisited to say what the feature actually needs." Then it
fixes them: CHECK constraints, column-level grants, no update, no delete. `NOT
VALID` is deliberate so a migration that would fail on live data is still a
migration someone runs.

---

## Dependencies

| Dependency | Risk |
|---|---|
| `com.yasirkula.speechtotext`, Git-pinned to `27a99b18…` | **Highest.** Not in the repo; a fresh clone needs internet or every script touching `SpeechToText` fails to compile. Pinning to a commit is the mitigation. |
| Device speech recognizer (fil-PH) | Out of our control. It may not be installed, and it returns the nearest real word rather than the sounds made — see Risk 1. |
| Network | Recognition is online (`preferOfflineRecognition: false`) **and** the leaderboard is hosted. No offline mode at all. |
| Supabase free tier | One project serves testing *and* release — a test play-through writes a real row. |
| `UnityWebRequest`, `JsonUtility` | Built-in. `JsonUtility` cannot parse a top-level array, which is why `ParseRecords` wraps PostgREST's response in `{"items": …}`. |

---

## Known weaknesses a panelist could find

Listed so none of them is a surprise. Fixes and priorities are in
`03_Completion_Assessment.md`.

1. **Wrong pronunciation accepted on a device** — the central one. See Risk 1.
2. **Four taught words have no `WordVariants` row:** `Hugas`, `Kinig`, `Ligpit`,
   `Tapon`. They get no curated mistranscriptions *and* they are absent from
   `TaughtForms`, so the split-fragment guard does not cover them.
3. **Four `WordVariants` rows are for words no longer taught:** `Itlog`, `Kanin`,
   `Tawa`, `Tulong`. (`Kanin` is deliberate and the comment says so — it keeps
   the Kain/Kanin collision test honest. The other three are drift.)
4. **Stale doc comments:** `PronunciationChecker` says 51 words; `PlayerMovement`
   calls itself a simple platformer; the README says 49 words / 66 objectives
   (actually 53 / 72); the release notes say 54 / 74.
5. **15 of the most-heard narrator lines are unrecorded** — the framing lines, the
   listening cue, the hints, the idle nudge, the correct/incorrect/not-heard
   replies. Caption shown, pause held, so it degrades gracefully.
6. **12 words are taught by caption rather than voice:** Alok, Apir, Hugas, Kinig,
   Ligpit, Linis, Padyak, Pagkain, Salamin, Sali, Suot, Tapon.
7. **The MINI GAME button does nothing.** Deliberate and documented in the
   tooltip — "a button that loads a stub screen would be a worse lie than one
   that sits still" — but say so before it is clicked.
8. **Word badges sit low** in Paaralan 2, Paaralan 5, Parke 4.
9. **Nothing offline.**
10. **One Supabase project for test and release** — no staging database.
11. **`WakeUpSequence` is 1,118 lines** and Scene 1 has no builder.
12. **The Editor has never exercised the real mechanic.**

## Security posture, in one table

| Concern | Status |
|---|---|
| Secrets in the repo | **Clean today.** `SupabaseConfig.asset` git-ignored, service key server-only. *An ElevenLabs API key is still reachable in git history (commit `0250197` on `origin/main`) — rotate it, untracking is not enough.* |
| Client trusted with writes | No. Edge Function + per-player token. |
| RLS | On, both leaderboard tables; `leaderboard_owner` has no policy *and* an explicit revoke. |
| Column-level least privilege | Yes, `player_scores` — `select=*` is now refused. |
| Input validation | Three layers: C# (`NicknameRules`), Edge Function (`cleanNickname`, `wholeNumber`), Postgres CHECK constraints. |
| Error disclosure | Detail to the log; child-safe sentence to the UI. |
| PII minimisation | Only id + nickname + two totals leave the device. Learning data never does. |
| Rate limiting | 5 s between updates of one row; ≤1000 points per submission. |
| Logging | Refusals logged with reason + 8-char id prefix; never the token, never the nickname. |
| Timing attacks | Constant-time token comparison. |
