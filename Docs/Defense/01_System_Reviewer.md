# SALINLAHI — System Reviewer

Written from the repository at commit `94c79a8` (branch `main`), 15 November 2026.
Every number in here was read out of the project, not remembered. Where a number
disagrees with the paper or the README, that is flagged — see
`07_Paper_vs_Build.md`.

Each section is written twice: **In plain English** first, then **For the panel**.

---

## 0. The thirty-second version

SALINLAHI is an Android game for Kindergarten–Grade 3 children in which the
**Filipino word is the controller**. A scene asks for a word, the child says it
into the phone's microphone, the device's Filipino speech recognizer transcribes
it, a pronunciation checker decides whether it counts, and only then does the
character act. Fifteen scenes, 72 spoken objectives, 53 distinct words. Progress
and the child's learning record stay on the phone; four values go to a shared
online leaderboard.

---

## 1. Purpose, target users, main features

### In plain English

Filipino children who speak mostly English at home understand Filipino but do
not *produce* it — they can pick the right picture but cannot say the word.
Most Filipino learning apps ask them to tap. SALINLAHI will not move until the
child speaks. That is the whole idea: speaking is not the answer to a question,
it is the control.

The player is Kylo, a boy going through one ordinary day — waking up, washing,
dressing, eating, school, the park, home. Each thing he does is unlocked by a
Filipino word.

### For the panel

| | |
|---|---|
| **Primary users** | Children, Kindergarten to Grade 3 (design target ~4–8 years), English-dominant at home, pre-readers included |
| **Stakeholders** | Teachers (supplementary tool), parents (progress visibility), pilot schools, future researchers |
| **Platform** | Android 8.0+ (API 26), ARM64. Package `ph.edu.uphsl.group33.salinlahi`, v0.1.0 |
| **Engine** | Unity 6000.6.2f1, built-in render pipeline, 2D |
| **Positioning** | Supplementary to classroom instruction, not a replacement (Ch. 1 §1.5) |

**Main features, as built:**

1. Voice-driven gameplay — 15 scenes, 72 spoken objectives, 53 distinct Filipino words
2. Pronunciation checking tuned to Filipino phonology (not generic edit distance)
3. A narrator, Teacher Rachel, who tells the story, teaches the word's meaning, asks for it, and reacts — 423 recorded clips with per-sound lip-sync
4. Journey map with per-scene unlocking, star ratings and level badges
5. Up to three named player profiles per device, saved locally as JSON
6. Per-level record written to Supabase (`player_scores`); a separate global leaderboard written through a server-side Edge Function
7. Accessibility for pre-readers: every instruction is voice + picture; on-screen text is a label for the adult, never the only carrier
8. No fail state — no timer, no lives, no penalty for a wrong answer

---

## 2. How the game works, start to finish

### In plain English

Open the app → main menu → **ADVENTURE** → the journey map → tap an unlocked
scene → Rachel sets the scene and introduces the first word → the badge for the
word appears faint while she is still talking, then brightens when it is your
turn → say the word → Kylo does the thing → next word → after the last word the
summary panel shows your score, stars and the words you learned → **NEXT** goes
on. After the fifth scene of a place, that level's badge is earned, the record
goes to the database, and the next place opens on the map.

### For the panel

The flow, naming the actual objects:

```
MainMenu.unity
  MainMenuController        nickname dialog (first launch only), ADVENTURE,
                            SETTINGS, ABOUT, MINI GAME (inert by design)
  LeaderboardClient         PushIfNeeded() — retries any unsent totals
      ↓ ADVENTURE
LevelSelect.unity
  LevelSelectController     draws JourneyMap.Steps as nodes;
                            LevelProgress.IsStepUnlocked gates each one
      ↓ a node
Chapter<N>_Level<M>_<Name>.unity
  SceneObjectiveController  the ordered word list for this scene
  VoiceCommand              owns the recognizer, raises OnAnswerChecked
  VoicePromptDirector       decides what Rachel says and when  (auto-installed)
  VoiceNarrator             the only thing that plays speech     (singleton)
  GameplayHUD               caption plaque, Rachel portrait, mic + replay buttons
  RunScoreCounter           adds this scene's attempts to RunScore
  PlayerMovement / PoseSequence / PickUpItem / SpriteSwap   the visible actions
  SceneFader / RoomTransition / ExitDoorTrigger             scene-to-scene
      ↓ last objective answered
  SceneResultPanel          stars, score, words learned
  SessionScoreTracker       (only in a level's 5th scene) POST the record,
                            RecordRun() onto the profile, MarkComplete(level)
```

**The inner loop, one objective** — this is the four-stage loop Chapter 3 calls
the core game loop:

1. **Prompt.** `SceneObjectiveController.ShowObjective(i)` turns the prompt UI on
   at `dimmedPromptAlpha = 0.45`, fires `onShown`, raises `OnObjectiveShown`.
   `VoicePromptDirector` queues three lines on `VoiceNarrator`: *story* →
   *teach/recall* → *ask*.
2. **Hold.** While `VoiceNarrator.IsSpeaking`, the badge stays faint and
   `currentTargetWord` is still empty, so the child cannot answer over Rachel.
   `RevealWhenNarrationEnds` waits, capped at `maxNarrationWait = 30 s`.
3. **Listen.** `GoLive(i)` sets alpha to 1 and writes `voiceCommand.currentTargetWord`.
   The recognizer has been running continuously, restarting 0.5 s after each result.
4. **Match → Act.** `PronunciationChecker.IsCorrect` decides. On true,
   `HandleAnswerChecked` hides the prompt, invokes the objective's `onCorrect`
   UnityEvent (the actual animation), raises `OnObjectiveCompleted(word)`, then
   `AdvanceWhenActionFinishes` waits for the action to end (capped at
   `maxActionWait = 4 s`) plus a `settlePause = 0.25 s` before asking the next word.

---

## 3. System architecture

### In plain English

Four parts. What you see (Unity scenes and UI), what decides (the game logic
scripts), what hears (the speech plugin), and what remembers (a save file on the
phone plus an online database). They talk through C# events, so no part reaches
into another part's internals.

### For the panel

```
┌──────────────────────── DEVICE (Android) ────────────────────────┐
│                                                                  │
│  PRESENTATION        GameplayHUD · SceneResultPanel · UITheme    │
│                      MainMenuController · LevelSelectController  │
│                      RachelPortraitAnimator · VisemeTracks       │
│                              ▲ events                            │
│  GAME LOGIC          SceneObjectiveController ── VoicePromptDirector
│                      PronunciationChecker · WordVariants · WordStress
│                      RunScore · LevelProgress · JourneyMap       │
│                      PlayerMovement · PoseSequence · PickUpItem  │
│                              ▲                                   │
│  SPEECH INPUT        VoiceCommand ──► SpeechToText (yasirkula)   │
│                                        └─► Android SpeechRecognizer
│                                              (fil-PH, online)    │
│                                                                  │
│  LOCAL DATA          PlayerProfileStore → players.json           │
│                      (Application.persistentDataPath, app-private)│
│                      VoiceAttemptLog → attempts CSV              │
└──────────────────────────────┬───────────────────────────────────┘
                               │ HTTPS, UnityWebRequest
          ┌────────────────────┴────────────────────┐
          │                                         │
   PostgREST (anon key)                     Edge Function (Deno)
   • INSERT player_scores                   • submit-score
   • SELECT player_scores (4 cols)          • holds the service key
   • SELECT leaderboard                     • issues/checks per-player token
          │                                         │
          └──────────── Supabase / PostgreSQL ──────┘
             player_scores · leaderboard · leaderboard_owner
             Row Level Security + column grants + CHECK constraints
```

**Three architectural decisions worth naming in the defense:**

1. **Objectives are data, not code.** A `SceneObjective` is `{ word, variants,
   promptRoot, instructionClip, onShown, onCorrect }`. Adding a word is adding a
   list entry, not writing a script. One controller drives all 15 scenes.
2. **Events, not references.** `VoiceCommand` does not know `SceneObjectiveController`
   exists; it raises `OnAnswerChecked`. `RunScoreCounter`, `SceneObjectiveController`
   and `VoicePromptDirector` all subscribe independently. That is why scoring could
   be made level-wide without touching the recognizer.
3. **Runtime bootstrap for cross-cutting pieces.** The 15 scenes are *generated*
   by editor builders (`Assets/Editor/Build*.cs`), so anything hand-added to a
   scene is lost on the next rebuild. `VoicePromptBootstrap` therefore installs
   the director with a `[RuntimeInitializeOnLoadMethod]` after every scene load,
   and `VoiceNarrator.Instance` creates itself on demand. Neither can be wiped.

---

## 4. Scenes, GameObjects, scripts and their relationships

**17 committed scenes** (15 gameplay + `MainMenu` + `LevelSelect`); 18 entries in
Build Settings, of which the legacy `Assets/MainScene.unity` is disabled.

| Level | Scenes | Words asked |
|---|---|---|
| 1 Bahay | UmagaNa, Banyo, Maligo, Damit, Almusal | 3, 5, 5, 5, 5 |
| 2 Paaralan | Paaralan, Klasrum, Aralin, Recess, Uwian | 5, 5, 4, 5, 5 |
| 3 Parke | Parke, Palaruan, Kaibigan, Piknik, Uwi | 5, 5, 5, 5, 5 |

**72 objectives · 53 distinct words · 19 re-asks.**

**Code size:** 67 runtime scripts (14,379 lines) in `Assets/Scripts`, 50 editor
scripts (14,810 lines) in `Assets/Editor`, 96 Python asset-pipeline tools in
`Tools/`. 351 commits.

The scene graph of a gameplay scene, as the builders construct it:

- **Canvas** → prompt roots (one per objective: badge sprite + word label + arrow),
  `GameplayHUD` plaque (Rachel portrait, caption text, mic button, replay button),
  pause/menu button
- **Player** → `PlayerMovement` (Rigidbody2D + Collider2D + SpriteRenderer),
  optionally `PoseSequence`, `IdleBreath`
- **Props** → `PickUpItem` / `SpriteSwap` components, wired into objectives'
  `onCorrect` events
- **Systems GameObject** → `VoiceCommand`, `SceneObjectiveController`,
  `RunScoreCounter`, and in a level's last scene `SessionScoreTracker` + `SupabaseClient`
- **Created at runtime** → `VoicePromptDirector`, `VoiceNarrator`

---

## 5. How voice recognition processes Filipino words

### In plain English

The game does not do speech recognition itself. It asks Android's own speech
service — the same one behind Gboard voice typing — to listen in Filipino and
hand back text. The game then judges that text. Listening never stops: half a
second after each result it starts again, so a child never has to press
anything. It *does* stop while Rachel is talking, otherwise the phone's
microphone hears her say "Sabihin mo: Lakad" and scores it as the child's answer.

### For the panel

- **Plugin:** `com.yasirkula.speechtotext`, pinned to commit `27a99b18…`. It is a
  thin binding to the platform recognizer — Android `SpeechRecognizer` and iOS
  `SFSpeechRecognizer`. No desktop or WebGL implementation, which is *why* the
  game is Android-only.
- **Locale:** `preferredLanguage = "fil-PH"`, requested at `Awake` via
  `SpeechToText.Initialize`. If unsupported, the plugin falls back to the device
  default rather than throwing.
- **Mode:** `useFreeFormLanguageModel = true` (dictation model),
  `preferOfflineRecognition: false` → **network required**.
- **Continuity:** `listenContinuously = true`, `restartDelay = 0.5 s`, re-armed in
  `OnResultReceived` through `Invoke(nameof(StartListening), restartDelay)`.
- **Barge-in prevention:** `VoiceNarrator` calls `SuspendListening()` before every
  line and `ResumeListening()` after. Suspend calls `SpeechToText.Cancel()`, which
  itself delivers a result — so `OnResultReceived` drops any result that arrives
  while `isSuspended`, otherwise the game's own interruption would be scored.
- **Five distinct outcomes**, each raised as its own event so the narrator can
  answer them differently:

| Event | Meaning | Scored? |
|---|---|---|
| `OnSpeechBegan` | recognizer detected speech starting | no |
| `OnAnswerChecked(text, true)` | correct | yes |
| `OnAnswerChecked(text, false)` | said something, wrong word | yes (an attempt) |
| `OnNothingHeard` | spoke, but nothing transcribable | **no** |
| `OnMicrophoneUnavailable` | permission denied / no recognizer | no |

  Pure silence deliberately raises nothing — the recognizer times out every few
  seconds, and a game that said "I didn't hear you" every five seconds at a
  thinking child would be unusable. Silence is the narrator's idle nudge instead
  (10 s), then a full re-prompt (every 20 s).

- **Editor fallback:** wrapped in `#if UNITY_EDITOR`, stripped from builds.
  `Enter` simulates saying the current objective's word; `X` simulates a wrong
  answer. Critically, it calls `SimulateSpeech()` → `ProcessRecognizedText()` —
  the **same** code path as a real result — so matching, scoring and events all
  behave as on device. It does nothing while Rachel is still speaking, exactly as
  the child's microphone would be ignored.

---

## 6. How pronunciation validation works

**This is the technical heart of the project. Know this section cold.**

### In plain English

The obvious way to be forgiving is "allow one or two wrong letters". We did that
first and it was wrong for Filipino. Filipino words are short and are told apart
by one vowel, so one letter of slack accepts every possible vowel mistake —
"kaha" passed for **Kuha**, "kanin" passed for **Kain**, and both of those are
taught in the same scene. A game that teaches pronunciation cannot reward a
wrong vowel.

So instead of allowing mistakes, we **fold away only the variation that is
genuinely free in Filipino** and then demand an exact match. e/i and o/u really
do alternate (*babae/babai*, *noon/nuon*) and the recognizer returns either —
so those fold together. a/u never alternates — so "kaha" still fails.

### For the panel

`Assets/Scripts/PronunciationChecker.cs` — a static class, no MonoBehaviour, so
it is unit-testable and callable from the editor self-check tool.

**Canonicalization** (`CanonicalForm`), applied to *both* sides:

| Step | Rule | Why |
|---|---|---|
| 1 | Unicode NFD, strip combining marks | `ó` → `o` |
| 2 | Strip punctuation | recognizer adds commas |
| 3 | `qu→k`, `ll→ly`, `x→ks`, then `c→k, z→s, f→p, v→b, j→h` | Spanish-era orthography; the recognizer may spell a Filipino word the Spanish way. Digraphs first so `ll` is not eaten |
| 4 | `e→i`, `o→u` | the one genuine free alternation in Filipino |
| 5 | `uw→w` | the glide: *tuwalya* = *twalya* |
| 6 | collapse doubled letters | "oppo" → "upu" matches **Upo** |
| 7 | collapse whitespace | |

**Then tokenize**, dropping 21 filler words (`po mo na ang ay yung ano nga ba
daw eh oh ok okay uh um hmm si ni sa ko`) — unless filler is *all* that was
said, in which case the raw tokens are judged, so a target that looks like a
filler word is still answerable.

**Then `MatchesForm` accepts on the first of these that passes:**

1. the whole filtered phrase equals the canonical target
2. **any single content token** equals it — *unless* the whole utterance run
   together is some **other taught word** (the fragment guard: "pag kain" is
   `Pagkain` badly split, so the "kain" inside it is not an answer to **Kain**;
   same for "ka nin"/Kunin, "u wi"/Upo, "ti ngin"/Bati)
3. all tokens **concatenated** equal it ("u po" → `upu` = **Upo**) — built from
   *unfiltered* tokens, so the filter does not eat the "po" that is part of the word
4. **one consonant slip**, and only on canonical forms of **7+ letters**:
   Levenshtein ≤ 1 **and** identical vowel skeleton. This absorbs recognizer
   noise on *Ibahagi, Kumusta, Salamat, Sapatos, Sipilyo, Dausdos* while leaving
   a vowel error impossible to pass.

**Plus `WordVariants`** — a per-word table of the mistranscriptions the recognizer
actually produces ("i love" for **Ilaw**, "lines" for **Linis**, "oppo" for
**Upo**, "cumin" for **Kunin**). Consulted on every check, so a scene generated
before the table existed still benefits. Grown from the attempt log, not guessed.
**The rule: a variant must never be another taught word.**

**The property that makes exact matching safe:** folding all 53 taught words
gives **53 distinct canonical forms**. Verified — no collisions, and no variant
collides with another taught word. The editor tool
**Tools > SALINLAHI > Check Pronunciation Matching** re-proves it on every run.

> **Stale comment to fix before the defense:** the class docstring still says
> "51 taught words" and "51×50 cross pairs". The property still holds at 53, but
> the number is wrong on the page a panelist might read. One-line fix.

**`WordStress.cs`** is display-only. Six taught words are minimal pairs that the
unmarked spelling cannot disambiguate — *tayô* (stand up) vs *táyo* (we), *upô*
vs *úpo*, *bukás* vs *búkas*, *bása* vs *basâ*, *talón* vs *tálon*, *bátì* vs
*batî*. Those six carry their marks **on screen only**; nothing reaches the
matcher, so the variant table and clip filenames are untouched.

### The honest limitation — say this before a panelist finds it

The checker is sound in its own tests. On a real device the v0.1.0 test found a
**mispronounced word is still accepted**. The probable cause is architectural,
not a bug in our code: Android's Filipino *dictation* service returns the nearest
real Filipino word rather than the sounds the child made, so the checker is
handed an already-corrected string and never sees the error. If confirmed, it
cannot be fixed inside the checker — it needs a different recognition approach
(a phoneme-level or children's-speech model, which is precisely what Santiago et
al. 2025 recommend). See `03_Completion_Assessment.md` §Risk 1 and
`04_Defense_Questions.md` Q14.

---

## 7. How Kylo's animations are triggered

### In plain English

There is no animation controller. Every action is a sprite being swapped on a
timer, and every one of them is started by a UnityEvent hanging off the word the
child just said. If you want to know what "Bangon" does, you look at the Bangon
objective in the Inspector and read its `onCorrect` list.

### For the panel

`SceneObjective.onCorrect` is a `UnityEvent` wired in the scene builder, so the
chain is **word → objective → UnityEvent → method on a component**. Four kinds of
action exist:

| Kind | Script | Example |
|---|---|---|
| Pose change | `PlayerMovement.Bangon()`, `TayoUp()` | Bangon sits him up; Tayo stands him |
| Timed walk/run | `WalkForward/Left/Right(duration)`, `Takbo()` | Lakad, Kaliwa, Kanan, Takbo |
| Frame sequence | `PoseSequence.Play()` | Sulat — a 2-second writing animation on a character who does not move |
| Prop change | `PickUpItem`, `SpriteSwap`, `PlayerMovement.SetAppearance()` | Damit/Medyas/Sapatos each switch a prop off and swap one art set for the next |

- **Walking** is `voiceWalkTimer` driven: `WalkInDirection(dir, duration)` sets a
  timer; while it is > 0, `FixedUpdate` forces horizontal velocity instead of
  reading the keyboard — so voice control works with no keyboard, on a phone.
  `walkTarget` + `arrivalEaseDistance` let him stop at a door rather than overshoot.
- **Walk cycle** is `walkFrames[]` cycled at `walkFramesPerSecond = 8`.
- **`IsActionPlaying`** = `IsVoiceWalking || IsPosing`. This is what
  `AdvanceWhenActionFinishes` waits on, together with any live `PoseSequence` and
  any `WalkIntoScene` — three kinds of "still moving", all three asked, because
  each was found in turn to let the next word go up mid-animation.
- **Teacher Rachel's mouth** is separate: `RachelPortraitAnimator` walks a viseme
  track from `Assets/Resources/VisemeTracks.json` (423 tracks, one per clip)
  against `VoiceNarrator.PlaybackTime`, picking among nine drawn mouth shapes
  (idle, ai, e, o, u, mbp, fv, l, blink). A line with no recording falls back to
  a three-shape cycle driven by `IsSpeaking`, so she mouths her caption instead
  of freezing.

Why no Animator Controller: the HUD and the scenes are generated at run time and
by editor builders, so there is no persistent GameObject for a controller asset
to live on, and a regenerated scene would lose it.

---

## 8. Scoring and progress tracking

### In plain English

A level is five scenes but the child plays it as one go, so the score has to span
all five. Each scene adds its attempts to a running total; the last scene reads
that total, shows it, and saves it. Stars are earned on **accuracy, not speed** —
we never time a child sounding out a word in a second language.

### For the panel

**`RunScore`** (static — two ints and a list that must outlive a scene load):
- `Correct`, `Attempts`, `Practised` (distinct words, in order met)
- `Accuracy = Correct / Attempts` (0 when no attempts)
- `PointsFor(correct, attempts) = correct × 10 + round(accuracy × 50)`
  — 10 per word so the number grows visibly with effort, up to 50 for getting
  them right first time, so the points row and the stars row tell the same story

**`RunScoreCounter`** sits in *every* scene, subscribes to
`VoiceCommand.OnAnswerChecked` (the count) and
`SceneObjectiveController.OnObjectiveCompleted` (which word). `resetOnStart` is
ticked only in a level's **first** scene, in `Awake` not `Start`, because a first
answer can land before Start has run everywhere.

**Stars:** `SceneResultPanel` / `SessionScoreTracker` award 3 at ≥0.95 accuracy,
2 at ≥0.70, otherwise 1. `LevelProgress.MaxStars = 5` is the profile's scale;
`StarsScaledTo(scene, slots)` rounds **up** so a 5-star rating shown in 3 tile
slots never reads empty for a scene that earned something.
`PlayerProfileStore.RecordStars` keeps the **best** run, never the last.

**On level completion** (`SessionScoreTracker.HandleSessionComplete`, guarded by
`alreadySaved`):
1. capture `Correct`, `Attempts`, `Practised`, `Accuracy`
2. `PlayerProfileStore.RecordRun(points, words)` → totals + lasting word list
3. `RunScore.Reset()`
4. `LevelProgress.MarkComplete(levelIndex)` and `MarkSceneComplete(sceneName)`
5. show the summary panel, then POST to `player_scores`, then GET the recent rows

Note the order: **the device save happens before the network call**. A failed
upload says "Your result is saved on this device" and is true.

---

## 9. How levels are unlocked

### In plain English

The map is one list of steps — the three place markers and every scene between
them. The game remembers one number: how far along that list you have got.
Everything else is worked out from it, so there is one number to get wrong
instead of several.

### For the panel

`JourneyMap.Steps` — 19 steps: 3 level markers + 16 scene slots (15 built, one
empty: Bahay scene 6, "Leaving for School"). `LevelProgress.HighestUnlockedStep`
is an index into it, stored on the active profile.

Three subtle points worth being able to explain, because each is a bug that was found:

1. **`StepAfter` skips the unenterable.** A level marker cannot be finished, and
   an unbuilt scene cannot be entered. Opening literally the next step would dam
   the journey behind Bahay scene 6 forever. `NextPlayable` skips both.
2. **`FirstStepOfLevel` returns the first *Scene*, not the marker.** Returning the
   marker left a fresh install on step 0 with scene 1 one step beyond it, so the
   whole game read as locked on every device except the one it was developed on.
3. **The end of the journey is `Steps.Length`,** one past the end — not a real
   step. `IsLevelComplete(i)` asks whether progress has moved *past* a level's
   last step, so Parke (the final level) can only be completed by a value no step
   has. A Palengke marker used to serve that purpose; removing it from the design
   meant the end of the journey had to become a number rather than a place.

---

## 10. How data is stored, retrieved and protected

### In plain English

Two different things are saved in two different places, on purpose. **What the
child is learning** — which words they got right, which scenes they played, how
many stars — stays on the phone and never leaves it. **Four numbers** — an id, a
nickname, total points and total stars — go to a shared leaderboard, because one
board has to show every player.

### For the panel

**On the device.** `PlayerProfileStore` writes `players.json` to
`Application.persistentDataPath` (app-private storage on Android). Up to 3
profiles. Each `PlayerProfile` holds: `playerId` (UUID, made once, the identity),
`nickname`, `highestUnlockedStep`, `completedScenes`, `sceneStars`,
`totalPoints`, `wordsLearned`, `leaderboardToken`, and the `synced*` bookkeeping.
Migration: an older PlayerPrefs save (`SALINLAHI_Nickname`,
`SALINLAHI_HighestUnlockedStep`, `SALINLAHI_Stars_<scene>`) is carried over once,
flagged, and the old keys are left in place so a rollback still finds its save.

**The save file is treated as untrusted input.** `Repair()` runs on every load:
a `playerId` that is not a UUID is replaced (and its leaderboard token discarded
with it, since a row could only ever have been registered under a real UUID); the
nickname goes through the same `NicknameRules.Clean` as one just typed; negative
totals are zeroed; stars are clamped. A corrupt file is treated as no file rather
than refusing to start.

**Table 1 — `player_scores`** (per completed level). Written by
`SupabaseClient.SaveScore`, read by `FetchRecentScores`.

| Column | Protection |
|---|---|
| `player_id` uuid | **not grantable to anon** — DB default `gen_random_uuid()` |
| `player_name` text | CHECK 1–16 chars, no control characters |
| `score` int | CHECK 0–1000 |
| `attempts` int | CHECK 0–1000 |
| `level` text | CHECK 1–120 chars |
| `remarks` text | CHECK ≤120 chars |
| `created_at` | **not grantable** — DB default `now()`, so a client cannot backdate |

`revoke all … from anon`, then `grant select (player_name, score, attempts,
created_at)` and `grant insert (player_name, score, attempts, level, remarks)`.
**No update, no delete** — not by policy and not by grant. A saved record is a
record of something that happened.

**Table 2 — `leaderboard`** (one row per player) and **`leaderboard_owner`**
(the token hash). RLS: anon may `select` the board and **nothing else**;
`leaderboard_owner` has no policy at all *and* an explicit `revoke`, so it is
unreadable twice over.

**The write path — `submit-score` Edge Function (Deno).** The client is never
trusted with a direct write.

| Check | What it stops |
|---|---|
| publishable-key gate (`SALINLAHI_PUBLISHABLE_KEY`) | traffic that never had the app |
| `player_id` must match the UUID regex | malformed ids |
| first submit **registers** and returns a 32-byte token; only its SHA-256 is stored | a stolen `player_id` alone buys nothing |
| every later submit must present the token, compared in **constant time** | finding a token a byte at a time |
| `total_stars ≤ 200`, `total_points` whole and ≥ 0 | absurd totals |
| point gain ≤ 1000 per submission | jumping a score to a million |
| ≥ 5 s between updates of one row | a tight update loop |
| totals only ever move **up** (`Math.max`) | a stale second device lowering a score |
| every refusal logged with reason + 8-char id prefix, never the token or nickname | a flood of forged submits being indistinguishable from an ordinary afternoon |

The **service key exists only on the server** — never in Unity, never in the
repo, never in a build. `SupabaseConfig.asset` is git-ignored; only the
**publishable/anon** key ships, and that key is public by design.

**What is honestly *not* protected:** the totals are still the client's word.
No server can replay a child saying "paa" into a microphone. The function bounds
what a forged submission is *worth*; it does not pretend to certainty that is not
available. Say exactly that if asked — it is a stronger answer than a claim.

---

## 11. Error handling and unexpected situations

| Situation | What happens |
|---|---|
| **Wrong word** | No penalty, no timer, no lives. Attempt is counted. Rachel says a retry line. After 2 misses she says the word again slowly, unasked (`missesBeforeHint`). After 4 she points at the replay button. |
| **Spoke but nothing transcribed** | `OnNothingHeard` → a *different* line from a wrong answer. **Never scored.** Logged via `VoiceAttemptLog.RecordNothingHeard` — a word the recognizer never transcribes needs a clearer prompt, not a new variant. |
| **Silence** | 10 s → encouragement; then every 20 s → the whole prompt replayed. |
| **Mic permission denied / no recognizer** | `OnMicrophoneUnavailable` → Rachel points at the microphone button. The game does not crash or soft-lock; `isInitialized` stays false and every entry point checks it. |
| **Result arrives after the object is destroyed** | `isShuttingDown` guard on every `ISpeechToTextListener` callback; `OnDestroy` cancels queued `Invoke`s and the recognizer. Stopping Play mid-recognition used to throw `MissingReferenceException`. |
| **Narrator audible to the mic** | Listening is suspended for the whole line; a result that arrives while suspended is discarded as our own `Cancel()`. |
| **Narration that never ends** | `maxNarrationWait = 30 s` releases the word anyway, so a missing clip cannot strand a badge dimmed forever. |
| **Animation that never ends** | `maxActionWait = 4 s`. |
| **Answering behind a fade** | `SetPaused(true)` during transitions clears the target word and hides prompts; completion is deferred (`completionPending`) so the results panel cannot open over a fade. |
| **Missing voice clip** | Not an error. The caption is shown and the pause the clip would have taken is still held, so pacing is unchanged. |
| **Save file corrupt / hand-edited** | Treated as no file; `Repair()` sanitizes everything else. |
| **Save file unwritable** | Warning to the log, game continues. "A save that cannot be written is bad; a crash on the results screen is worse." |
| **Database unreachable** | Panel says "Your result is saved on this device. We will send it when you are online." — true, because `RecordRun` already ran. |
| **Leaderboard upload fails** | Profile stays marked `NeedsLeaderboardSync`; `PushIfNeeded()` retries next time the main menu opens. |
| **Database error detail** | Status code, transport error and response body go **to the log only**. A child's panel never shows a 401 or a Postgres constraint name. |
| **Scene not on the journey map** | `MarkSceneComplete` logs a warning naming the scene rather than silently unlocking nothing. |

---

## 12. The ten things to understand before the defense

1. **The mechanic is the thesis.** Speaking is the controller, not the answer to a
   quiz. Everything else follows from that.
2. **Why canonicalization beat edit distance.** Filipino words are short and
   separated by one vowel; "one edit of slack" accepted every vowel error. Be able
   to say "kaha for Kuha" and "kanin for Kain" from memory.
3. **53 words fold to 53 distinct canonical forms.** That is the property that
   makes exact matching safe, and an editor tool re-proves it.
4. **The device-level pronunciation problem.** Android dictation hands back the
   nearest real word, so the checker may never see the error. Know this, name it
   first, and frame it with Santiago et al. (2025).
5. **The real numbers: 15 scenes, 72 objectives, 53 words, 19 re-asks.** The paper
   says 73 and the README says 66/49. Fix both; be ready to be asked.
6. **Objectives are data.** One controller, 15 scenes, no per-scene code.
7. **Two data paths, two reasons.** Learning data stays local; four values go up.
   You chose that; be able to say why.
8. **The client cannot write the leaderboard.** Token issued server-side, SHA-256
   stored, constant-time compare, the service key never ships.
9. **No fail state is a design decision**, grounded in Zargham et al. (2022) and
   Getman et al. (2023), not an unfinished feature.
10. **The Editor has never exercised the real mechanic.** Pressing Play uses the
    keyboard fallback. That is why releases are tested on a phone — demo on the
    phone if you possibly can.
