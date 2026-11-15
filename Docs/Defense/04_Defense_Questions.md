# SALINLAHI — 52 Panel Questions

Each entry: **Q** the question · **A** what to say · **T** the technical layer if
they push · **↳** likely follow-ups · **📂** the evidence to have open.

The ones marked **⚠** are the ones that can hurt. Rehearse those first.

---

## A. Background, problem, objectives (Q1–Q8)

### Q1. In one sentence, what is SALINLAHI?
**A.** A voice-interactive Filipino vocabulary game for Kindergarten to Grade 3,
in which the child says a Filipino word out loud and that word is what makes the
game move — speaking is the controller, not the answer to a quiz.
**T.** Unity 6000.6.2f1, Android 8+, 15 scenes, 72 spoken objectives, 53 distinct
Filipino words; device speech recognition at `fil-PH` with a Filipino-phonology
pronunciation checker on top.
**↳** Why Filipino? Why that age? Why a game?
**📂** Main menu on the demo phone.

### Q2. What problem does it solve?
**A.** English-dominant Filipino children can *recognise* Filipino words but
cannot *produce* them. Most learning apps ask them to tap, so the child can be
right without ever saying anything. We made saying it the only way forward.
**T.** Chapter 1 §1.3, Specific Problems 1–5. Grounded in Amarilla et al. (2025),
who found English-mother-tongue pupils struggling with Filipino because of
limited exposure and no one at home encouraging use.
**↳** Isn't tapping easier for a child? How do you know recognition ≠ production?
**📂** Chapter 1 §1.2–1.3.

### Q3. What is your theoretical basis?
**A.** Three. Swain's Output Hypothesis (1985) — producing language forces deeper
processing than understanding it; that is why the word is the control. Krashen's
Input Hypothesis (1982) — we introduce each word's meaning before asking for it.
And digital game-based language learning.
**T.** Swain → the mechanic. Krashen → the teach line before the ask line
(`NarratorScript.TeachId` precedes `AskId`). DGBLL → Zou et al. (2021), Dixon et
al. (2022), Şimşek & Şimşek (2025, g = 0.729 across 1,759 preschoolers).
**↳** Where exactly is Krashen implemented? **(Answer: `VoicePromptDirector.HandleObjectiveShown` — story, then teach/recall, then ask, three separate lines.)**
**📂** Chapter 2 §2.1; `VoicePromptDirector.cs`.

### Q4. ⚠ What makes this different from Ibigkas! or Duolingo?
**A.** Ibigkas! and Ibigkas! Filipino are Filipino but drill-based and aimed at
Grades 4–6, and the voice is not the control. Wizardly and Studycat VoicePlay are
voice-controlled but English. Nobody in the reviewed work has a Kindergarten-to-
Grade-3 learner speaking a Filipino word that directly performs the action.
**T.** Table 2.1 in Chapter 2 compares seven systems on four axes: language,
target learners, voice-as-control, Filipino vocabulary. SALINLAHI is the only row
with yes on both of the last two.
**↳** Is "nobody has done it" enough of a contribution? What about commercial apps you didn't review?
**📂** Table 2.1, Chapter 2 §2.3 and §2.5.

### Q5. Who exactly are your users?
**A.** Children Kindergarten to Grade 3, roughly 4 to 8, English-dominant at home,
including children who cannot yet read. Teachers and parents are stakeholders,
not users.
**T.** Three design assumptions follow: pre-readers (every instruction is voice +
picture, text is a label for the adult), developing speech (variants accepted, no
penalty), assisted play (progress saved per scene so an adult can stop anywhere).
**↳** How did you validate the age range? Have children actually played it?
**📂** Chapter 1 §1.7; Chapter 3 §3.2.1.

### Q6. ⚠ Have real children tested this?
**A.** Not yet as a formal evaluation. v0.1.0 was tested on a physical device by
the group, which is where we found the pronunciation issue. The pupil evaluation
of functionality, usability and usefulness is Objective 5 and belongs to Chapter 4.
**Do not** overclaim here. Say what is scheduled and with whom.
**T.** Chapter 1 §1.4 scopes Objective 5 to functionality, usability and
usefulness — deliberately **not** a learning-gain claim, because Muñoz et al.
(2025) found a well-received game producing no measurable gain in public-school
pupils.
**↳** When? How many? What instrument? Ethics clearance?
**📂** `Docs/release/v0.1.0/RELEASE_NOTES.txt`; your Chapter 4 plan.

### Q7. Why is it called SALINLAHI?
**A.** It means lineage — handing down across generations. The vocabulary is the
everyday household and school Filipino that passes from a parent or grandparent
to a child in conversation, not from a classroom word list.
**↳** Does the game teach anything cultural beyond words? **(Yes: the banig at the picnic, the public-school classroom, the tabo that used to be in the bathroom.)**

### Q8. What are your scope and delimitations?
**A.** Android only; three environments; a selected word set, not the language;
vocabulary practice, not grammar or writing; supplementary, not a replacement for
instruction; and explicitly **not** accessible to children who cannot produce
clear speech, because the mechanic requires it.
**T.** Android-only is forced by the mechanic — the speech plugin binds to native
Android/iOS services and has no desktop implementation. That is a technical
constraint, not a preference.
**↳** Isn't excluding children with speech impairments a problem for an educational tool? **(Acknowledge it plainly; it is in §1.5; the honest answer is that it is a limitation of this study and a direction for future work, not something we papered over.)**
**📂** Chapter 1 §1.5.

---

## B. Architecture and implementation (Q9–Q17)

### Q9. Describe your system architecture.
**A.** Four parts on the device — presentation, game logic, speech input, local
data — plus a backend. The scenes and UI show it; the logic decides it; the
plugin hears it; a JSON file remembers it; Supabase holds the shared leaderboard.
They talk through C# events, so no part reaches into another's internals.
**T.** Draw the diagram in `01_System_Reviewer.md` §3. Name the event seam:
`VoiceCommand` does not know `SceneObjectiveController` exists, it raises
`OnAnswerChecked`, and three unrelated listeners subscribe.
**↳** Why not MVC? Is it layered or event-driven? What happens if two listeners disagree?
**📂** `01_System_Reviewer.md` §3; Part1_FinalSystemArchitecture.pdf.

### Q10. ⚠ Walk me through what happens when a child says "Bangon".
**A.** The recognizer is already listening. It transcribes to text and calls
`OnResultReceived`. `VoiceCommand.ProcessRecognizedText` passes it to
`PronunciationChecker.IsCorrect` against the current target word.
`OnAnswerChecked(text, true)` fires. `SceneObjectiveController` hides the prompt
and invokes that objective's `onCorrect` UnityEvent, which calls
`PlayerMovement.Bangon()` — Kylo sits up. `RunScoreCounter` counts the attempt.
`VoicePromptDirector` queues "Tama!" then the story line. Then the controller
waits for the sit-up to finish before asking the next word.
**T.** Be able to name each of those as a file. This is the question that proves
you wrote the system.
**↳** What if it fires twice? Where does the score go? What if the animation hangs? **(`maxActionWait = 4 s`.)**
**📂** `VoiceCommand.cs:ProcessRecognizedText`, `SceneObjectiveController.cs:HandleAnswerChecked`.

### Q11. Why Unity 6 and C#?
**A.** 2D sprite work, a mature Android pipeline, and — the deciding factor — the
speech plugin we needed is a Unity package that binds to Android's native
recognizer. C# because that is Unity's language.
**T.** 6000.6.2f1 specifically, pinned in `ProjectSettings/ProjectVersion.txt` so
all three members build identically. Built-in render pipeline, 2D. No third-party
HTTP SDK — plain `UnityWebRequest`.
**↳** Why not Godot/Flutter/native Android? Why built-in and not URP?

### Q12. How does a new word or scene get added?
**A.** A word is a list entry, not code. A `SceneObjective` holds the word, its
accepted variants, its prompt UI, its instruction clip, and the UnityEvent that
fires when it is said. One controller drives all fifteen scenes.
**T.** `SceneObjectiveController.SceneObjective`. Fourteen of the fifteen scenes
are *generated* by editor builders in `Assets/Editor/Build*.cs`, so the real
workflow is: edit the builder, re-run it, commit the regenerated scene. That is
why scene diffs in the repo are large.
**↳** Isn't regenerating scenes risky? **(Yes — and it is why anything cross-cutting is installed at runtime instead: `VoicePromptBootstrap`, `VoiceNarrator.Instance`.)**
**📂** `BuildPaaralanScene3.cs` around `MakeObjective(...)`.

### Q13. Which design patterns did you use?
**A.** Observer throughout — C# events and UnityEvents are the main seam.
Singleton for `VoiceNarrator` (one mouth, so two lines can never overlap).
Static utility for `PronunciationChecker`, `RunScore`, `LevelProgress`,
`JourneyMap` — things that are data or a pure function, not objects with a
lifetime. Strategy-by-data for objectives.
**T.** Justify the statics: "`RunScore` is two ints and a list that must outlive
a scene load. A `DontDestroyOnLoad` GameObject would be a MonoBehaviour that
exists only to not be destroyed."
**↳** Aren't singletons and statics an anti-pattern? **(Defend each on its own terms; `PronunciationChecker` being static is precisely what makes it unit-testable without the engine.)**

### Q14. ⚠⚠ Why is `WakeUpSequence.cs` 1,118 lines?
**A.** Scene 1 is the only scene with no builder — it is kept by hand and patched
in place — and it carries the game's bespoke opening: the snore loop, the sit-up,
the stretch, the pose settling into the bed. It is the first thing I would
refactor, and I would split it into the pose sequence, the audio loop and the
scene choreography.
**Say the refactor plan unprompted.** A known weakness with a plan reads as
engineering judgement; the same weakness defended reads as denial.
**↳** Why didn't you build Scene 1 with a builder like the others?

### Q15. How do scripts communicate?
**A.** Five C# events on `VoiceCommand`, two on `SceneObjectiveController`, and
UnityEvents on each objective. Nothing polls and nothing reaches across.
**T.** Name the subtle one: `OnObjectiveCompleted` passes the **word** rather than
letting listeners read `VoiceCommand.currentTargetWord`, because both branches of
`HandleAnswerChecked` clear or advance that field synchronously — so whether a
listener saw the answered word or the next one came down to subscription order.
That is a real race that was found and closed.
**↳** How do you avoid memory leaks from events? **(Every subscriber unsubscribes in `OnDisable`/`OnDestroy`.)**

### Q16. How do you prevent a result arriving after the object is gone?
**A.** `isShuttingDown` is checked at the top of every recognizer callback, and
`OnDestroy` cancels any queued restart and tells the recognizer to stop.
**T.** The speech plugin holds the MonoBehaviour as its listener, so stopping Play
or loading a scene mid-recognition used to throw `MissingReferenceException`.
**↳** Why not just null-check? **(A destroyed MonoBehaviour is not null in the C# sense — Unity's `==` lies. The explicit flag is the reliable guard.)**

### Q17. How is the HUD built?
**A.** At run time, from a `UITheme` ScriptableObject, by `GameplayHUD` and
`GameplayHUDLayout`. Not placed in each scene.
**T.** Because 14 of 15 scenes are regenerated, anything hand-placed would be
lost. Building at run time also means one change updates fifteen scenes.
**↳** Doesn't that cost startup time? What if `UITheme.asset` is missing? **(Frames are optional — Refresh UI Theme reports how many it found rather than treating them as missing art.)**

---

## C. Voice recognition (Q18–Q27) — the heart of the defense

### Q18. How does the voice recognition actually work?
**A.** We do not do recognition ourselves. We ask Android's own speech service —
the one behind Gboard voice typing — to listen in Filipino and give us back text.
We then judge that text. Building a recognizer was never in scope; judging
Filipino correctly was.
**T.** `com.yasirkula.speechtotext`, Git-pinned to commit `27a99b18…`, binding to
Android `SpeechRecognizer` / iOS `SFSpeechRecognizer`. Locale `fil-PH`,
free-form (dictation) model, `preferOfflineRecognition: false`.
**↳** Why not Vosk / Whisper / a trained model? (→ Q26) What if `fil-PH` is unavailable? **(The plugin falls back to the device default rather than throwing; `isInitialized` guards everything.)**

### Q19. ⚠⚠ Why not just use string matching or edit distance?
**A.** We did, and it was wrong for Filipino. Filipino words are short and
separated by a single vowel, so "allow one edit" accepts every possible vowel
error. "Kaha" was scored correct for **Kuha**. "Kanin" was scored correct for
**Kain** — and both are taught in the breakfast scene, so each was accepted for
the other. A game that teaches pronunciation cannot reward a wrong vowel.
**T.** So we inverted it: instead of tolerating arbitrary edits, we **canonicalize
away only the variation that is genuinely free in Filipino** and then require an
exact match. e/i and o/u really alternate (*babae/babai*, *noon/nuon*) and the
recognizer returns either — those fold. a/u never alternates — so "kaha" fails.
**↳** Give me another example. What about a lisp? Isn't exact matching too strict for a child?
**📂** `PronunciationChecker.cs` class docstring — it names the bugs by example.

### Q20. ⚠ Walk me through the canonicalization.
**A.** Seven steps on both sides: strip accents, strip punctuation, map
Spanish-era spellings to Filipino sounds (`qu→k`, `ll→ly`, `x→ks`, then
`c→k z→s f→p v→b j→h`), fold `e→i` and `o→u`, fold the `uw/w` glide, collapse
doubled letters, collapse whitespace. Then drop 21 filler words.
**T.** Digraphs first, deliberately, so `ll` is not eaten by the later single-letter
pass. Filler is dropped **unless it is all that was said**, so a target that looks
like a filler word is still answerable. The concatenation test uses *unfiltered*
tokens, so the filter does not eat the "po" that is part of **Upo**.
**↳** Why `uw→w`? **(*tuwalya* and *twalya* are one word.)** Why collapse doubles? **("oppo" is what the recognizer returns for Upo.)**

### Q21. ⚠⚠ Prove two taught words cannot be confused.
**A.** Folding all 53 taught words gives 53 **distinct** canonical forms. Exact
matching therefore cannot make two of them ambiguous, by construction. And the
editor tool **Tools > SALINLAHI > Check Pronunciation Matching** re-proves it
every time it runs, over every cross pair — it also refuses any variant entry
that equals another taught word.
**T.** Offer to run it live. That is the strongest 20 seconds available to you in
the whole defense.
**↳** What if a new word collides? **(The tool fails and names it; that is the gate.)** What about variants? **(Same check — "a variant must never be another taught word", because listing "kanen" under Kanan would score a child correctly saying Kanin as TAMA for Kanan.)**
**📂** The editor tool, run live.

### Q22. You said exact matching. Where is the tolerance, then?
**A.** In three places, all narrow and all deliberate. The variants table, which
is measured rather than guessed. The fragment handling, for when the recognizer
splits one word in two. And one consonant slip, only on words of seven or more
canonical letters, and only when every vowel still lines up.
**T.** `ConsonantSlipMinLength = 7`. `IsConsonantSlip` = Levenshtein ≤ 1 **and**
identical `VowelSkeleton`. It absorbs genuine noise on *Ibahagi, Kumusta,
Salamat, Sapatos, Sipilyo, Dausdos* while leaving a vowel error impossible.
**↳** Why 7? **(Below that a single consonant *is* the word — on a four-letter word one consonant of slack is a different word.)** Isn't that arbitrary? **(It is a threshold, chosen so that no taught word under it can absorb an edit; the cross-check proves the result.)**

### Q23. What is the fragment guard?
**A.** The recognizer sometimes splits one word into two — "pag kain" for
**Pagkain**. Without a guard the "kain" inside it would answer **Kain**, and those
two words are asked one after the other in the breakfast scene. So before
accepting any single token, we check whether the whole utterance run together is
some *other* taught word; if it is, its pieces are halves of that word, not an
answer.
**T.** `IsSomeOtherTaughtWord(string.Concat(allTokens), canonicalForm)` against
`TaughtForms`. Same guard stops "ka nin"/Kunin, "u wi"/Upo, "ti ngin"/Bati.
**↳** How is `TaughtForms` built? **(From `WordVariants.Words`.)** → which exposes the real gap: **four taught words are missing from that table**, so the guard does not cover them. Know this; have the fix ready.

### Q24. How did you build the variants table?
**A.** The entries in the repo today are seeds — likely Filipino spellings plus
the English words the recognizer substitutes ("i love" for **Ilaw**, "lines" for
**Linis**, "oppo" for **Upo**). The real list comes from the attempt log: play
with logging on, pull the CSV, read the rows that were marked wrong but where the
child said the word right, add those exact strings.
**T.** `VoiceAttemptLog` writes target, raw transcription, canonical form and
verdict per attempt. `PronunciationChecker.Canonicalize` is public precisely so
the log can record it beside the raw text — "kaha" next to "kuha" is how a
mishearing gets spotted.
**↳** ⚠ How many variants came from real playtests rather than guesses? **(Be honest: the table is still mostly seeded. The pipeline exists and is the next thing the data feeds. Bringing one real CSV to the defense converts this from a weakness to a method.)**

### Q25. ⚠⚠ Does it actually reject a mispronounced word on a phone?
**This is the hardest question you will get. Answer it before they ask it.**

**A.** In our own tests, yes, every time. On a device, not reliably — and we know
why. Android's Filipino dictation service is a language model: handed a child's
approximate "ka-ha", it returns the nearest real Filipino word, which may well be
"kuha". Our checker is handed an already-corrected string, so the error never
reaches us. That cannot be fixed inside the checker; it needs a different
recognition approach.
**T.** This is exactly what the literature predicts. Bhardwaj et al. (2022) and
Santiago et al. (2025) both report adult-trained recognizers performing poorly on
children's speech — Santiago's adult-Filipino model scored 39.70% WER on
children, and their recommendation is to embed a *fine-tuned* recognizer in an
interactive learning platform. We are that platform, using a stock recognizer,
and we found the problem they predict. It is recorded as the first known issue in
v0.1.0 and it is why v0.1.0 is a testing build.
**↳** So the game doesn't work? **(No — the game teaches, prompts, repeats and scaffolds all 53 words, and the loop is complete. What is not yet reliable is the *correction* of a mispronunciation.)** What will you do? **(Three things: test the search language model instead of dictation, which returns more literal text for a one-word answer; collect the attempt log as evidence; and, for 1.0, a fine-tuned children's model, which is what Santiago recommends.)**
**📂** `RELEASE_NOTES.txt` Known Issues, first entry. The attempt-log CSV if you have it.

### Q26. Why not train your own model on Filipino children's speech?
**A.** Out of scope for a one-year capstone, and the corpora that would make it
possible — BK3AT, the Philippine Languages Database, Tanglaw — are recent and
mostly not openly redistributable. We chose to make the *game* right and treat
recognition as a replaceable component.
**T.** And it genuinely is replaceable: `VoiceCommand` is the only script that
knows `SpeechToText` exists. Swapping the recognizer touches one file.
**↳** Is that a cop-out? **(Point at the seam: that is an architectural answer, not an excuse. One file.)**

### Q27. What if the child speaks while Rachel is talking?
**A.** Listening is suspended for the whole line. On a phone speaker the
recognizer hears Rachel say "Sabihin mo: Lakad" perfectly well and would score it
as the child's own attempt.
**T.** `VoiceNarrator` calls `SuspendListening()` / `ResumeListening()`. Suspend
calls `SpeechToText.Cancel()`, which itself delivers a result — so
`OnResultReceived` discards any result arriving while `isSuspended`, otherwise
our own interruption would be scored.
**↳** Isn't suspending bad for a child who answers early? **(The badge is faint and `currentTargetWord` is empty until she finishes — the child is being shown it is not their turn yet. And the same rule applies to the Editor shortcut.)**

---

## D. Gameplay, UX, pedagogy (Q28–Q35)

### Q28. Why is there no score penalty or timer?
**A.** Because the user is a four-to-eight-year-old speaking a second language.
Timing a child who is sounding out a word punishes exactly the behaviour we want.
Wrong answers cost nothing; the scene simply waits.
**T.** Grounded, not just kind: Zargham et al. (2022) found that a voice game
acting on its own when a command is not recognised *worsens* the experience when
the guessed action is wrong — so we use the simpler repetition approach. Getman
et al. (2023) note their models were deliberately lenient, which they described
as helpful for encouraging learners.
**↳** Then how do you motivate? **(Points, stars, badges, and the words-learned list — rewards for doing it, not punishments for not.)**

### Q29. How are stars and points calculated?
**A.** Points are 10 per word said correctly plus up to 50 for accuracy. Stars are
earned on accuracy only: three at 95% or better, two at 70%, otherwise one. Never
on speed.
**T.** `RunScore.PointsFor(correct, attempts) = correct × 10 + round(accuracy × 50)`.
Deliberately tuned so the points row and the stars row tell a child the same story
about the same run. `LevelProgress` keeps the **best** run per scene, never the
last — a child who three-starred a scene and stumbles on a replay keeps the stars.
**↳** Why 10 and 50? **(10 so the number grows visibly with effort; 50 so the accuracy bonus is worth about five words and the two rows agree.)** Why 5 stars internally and 3 on the tile? **(`StarsScaledTo` rounds up, so a scene that earned something never reads empty at tile size.)**

### Q30. How does a child who cannot read play this?
**A.** Every instruction is a voice line plus a picture. Text on screen is a label
for the adult sitting beside them, never the only carrier of an instruction. The
badge shows the object, an arrow points at it, Rachel says what to say.
**T.** Follows Getman et al. (2023), whose app used audio and visual cues with no
textual explanation so pre-readers could play. Our `captionMode` can be set to
`WhenNoAudio` so captions disappear as recordings arrive.
**↳** ⚠ But 12 of your words are currently taught by caption only. **(Acknowledge it: Alok, Apir, Hugas, Kinig, Ligpit, Linis, Padyak, Pagkain, Salamin, Sali, Suot, Tapon. It is on the must-fix list and it matters precisely because the users are pre-readers.)**

### Q31. What happens after repeated failures?
**A.** A ladder, not a wall. Two misses and Rachel says the word again slowly,
without being asked — that is the most important accessibility behaviour in the
game, because a child who cannot reach the replay button still gets help. Four
misses and she points at the replay button. Ten seconds of silence gets
encouragement; after that the whole prompt repeats every twenty seconds.
**T.** `VoicePromptDirector.missesBeforeHint = 2`, `missesBeforeReplayHint = 4`,
`idleNudgeSeconds = 10`, `idleRepromptSeconds = 20`. "Said something I couldn't
make out" is a different event from "said the wrong word", gets different lines,
and is **never** counted as a miss.
**↳** Why does silence not count? **(The recognizer times out every few seconds whether or not anyone is talking. A game that said "I didn't hear you" every five seconds at a thinking child would be unusable.)**

### Q32. How do you keep a child engaged across 72 objectives?
**A.** The vocabulary is a day, not a list. When the game asks for **Sipilyo**, the
child is standing at a sink holding one. Five words per scene — short enough to
finish before losing interest, long enough that the scene has a shape.
**T.** Three scenes break the five-word rule deliberately: UmagaNa (3) because
waking up is a transition, Aralin (4), and Maligo (5). And 19 of the 72
objectives re-ask a word met earlier, in a different room with different art, so
the child retrieves it from the word rather than from the picture — spaced
repetition built into the level design.
**↳** Give an example of a re-ask. **(Lakad: bedroom, hallway, the road to the park, the walk home.)**

### Q33. How does the game handle repeated words without being boring?
**A.** `VocabularyMemory`. The first time a word comes up the child gets the full
English explanation; every time after, in the same sitting, a one-line reminder.
**T.** Marked as taught when the word is first **asked**, not first said correctly
— the explanation was played either way, and a child who needed six tries has
heard it six times over. The memory is cleared when the game closes, deliberately:
a child coming back after a few days should be taught properly again, not handed
a reminder of something they no longer remember.
**↳** Why not persist it? **(That is the reason — and the lasting record is separate: `PlayerProfile.wordsLearned`.)**

### Q34. Why English narration for a Filipino game?
**A.** The learners are English-dominant — that is the whole premise. The
*instruction* is in the language they already have; the *word* they must produce
is Filipino. Teaching the meaning in a language they do not yet have would defeat
the point.
**T.** And the split is literal in the audio: Rachel's English narration is
ElevenLabs TTS; the Filipino words the child is asked to copy were **recorded by a
group member**, because Tagalog distinguishes words by stress — *tayô* (stand up)
against *táyo* (we) — and a TTS model has no way of knowing which is meant. Those
clips are the only thing a child imitates, so they had to come from a Filipino
speaker.
**↳** Is AI-generated narration acceptable in a capstone? **(Disclose it plainly — it is already in Chapter 3 §3.4.2. The pedagogically load-bearing audio is human.)**

### Q35. What are those accent marks on some words?
**A.** Six of the 53 words are minimal pairs where the plain spelling is two
different words — *tayô* stand up vs *táyo* we, *upô* sit vs *úpo* the gourd,
*bukás* open vs *búkas* tomorrow, *bása* read vs *basâ* wet, *talón* jump vs
*tálon* a waterfall, *bátì* greeting vs *batî* beaten. Those six carry their marks
on screen so the game is specifying what it is asking for.
**T.** `WordStress.cs` — **display only**. Nothing reaches the matcher, the variant
table or the clip filenames, so it is a lookup rather than a field on every
objective and no scene had to be rebuilt.
**↳** Why not mark all of them? **(A mark on a word that does not need one teaches a distinction that is not there — and it would make the marks that do matter invisible.)** Who validated the six? **(Say honestly: it is the group's reading, pending the validating teacher. Two more — *kuha* and *sulat* — are deliberately unmarked until she rules.)**

---

## E. Data and database (Q36–Q42)

### Q36. What is stored, and where?
**A.** Two things in two places, on purpose. What the child is learning — which
words, which scenes, how many stars — stays on the phone in app-private storage.
Four values go online: a player id, the nickname, total points, total stars,
because one leaderboard has to show every player.
**T.** `players.json` at `Application.persistentDataPath` via
`PlayerProfileStore`; `player_scores` and `leaderboard` in Supabase/PostgreSQL
over PostgREST with plain `UnityWebRequest`.
**↳** Why not cloud-sync everything? **(It is children's learning data. Minimising what leaves the device is a privacy decision, not a technical limitation.)**

### Q37. Show me the save and retrieve code.
**A.** `SupabaseClient.SaveScore()` is the POST, `FetchRecentScores()` is the GET,
`SessionScoreTracker` decides when, `ScoreRecord`/`ScoreInsert` is the shape.
**T.** `ScoreInsert` deliberately omits `player_id` and `created_at` so the
database fills them with `gen_random_uuid()` and `now()` instead of us sending
blanks — and the anon role has **no grant** on those two columns, so a client
could not choose its own id or backdate a row even if it tried.
**↳** Why `Prefer: return=representation`? **(So the response confirms exactly what was stored.)** Why name the columns instead of `select=*`? → Q39.

### Q38. Why Supabase?
**A.** Managed PostgreSQL with a REST interface, Row Level Security, and Edge
Functions — so we could put a real server-side check in front of the leaderboard
without running a server. And it works from Unity with no SDK.
**↳** Why not Firebase? Why not your own PHP API? What happens if the free tier runs out?

### Q39. ⚠ How is the database secured when the API key ships inside the APK?
**A.** The key is public by design — any client key is. What protects the data is
what the key is *allowed* to do. The anon role may insert and select on
`player_scores` and select on `leaderboard`, and nothing else. There are no
update or delete policies and no update or delete grants.
**T.** Three layers, and name all three:
1. **Row Level Security** — which rows. `leaderboard_owner` has no policy *and* an
   explicit `revoke`, so the token hashes are unreadable twice over.
2. **Column grants** — which columns. `revoke all … from anon`, then
   `grant select (player_name, score, attempts, created_at)` and
   `grant insert (player_name, score, attempts, level, remarks)`. A build that
   went back to `select=*` is now refused by the database.
3. **CHECK constraints** — what a value may be. Score 0–1000, nickname 1–16 chars
   with no control characters, level ≤120 chars.
**↳** Isn't the anon key still a risk? **(It is a gate, not authentication — and we say so in the code comments. The service key is server-side only and never in Unity, the repo or a build.)**
**📂** `supabase/migrations/20261112_player_scores_hardening.sql` — read its header aloud if you want a strong moment.

### Q40. ⚠ What stops a child cheating the leaderboard?
**A.** The client cannot write the leaderboard at all. It POSTs to an Edge
Function, which holds the only key that may write. The first submission for a
player id registers it and the server issues a 32-byte token; only its SHA-256 is
stored. Every later submission must present that token, compared in constant
time. So learning another child's player id buys nothing.
**T.** Plus bounds: stars ≤ 200, point gain ≤ 1000 per submission, at least five
seconds between updates of one row, totals only ever move up. Every refusal is
logged with its reason and the first eight characters of the id — enough to see a
hundred "token does not own this player" in a minute, not enough to write a
child's identifier into a log.
**↳** ⚠⚠ But the totals are still the client's word, aren't they? **(Yes. Say it first: nothing on a server can replay a child speaking into a microphone, so no server can know the true score. What it can do is refuse the absurd and bound what a forged submission is worth. That is an honest statement of what the design buys, and it is a better answer than a claim.)**
**📂** `supabase/functions/submit-score/index.ts`.

### Q41. What is your data privacy position?
**A.** Minimisation. Four values leave the device. Which words a child found hard,
how many tries each took, which scenes they played — a child's learning record —
never leaves the phone. No email, no birthdate, no account, no login. The
nickname is chosen by the child and capped at 16 characters.
**T.** The `leaderboard` migration header says it in one line: "One row per
player, and nothing else." Deleting a profile deliberately leaves the leaderboard
row alone — it belongs to the child, not the install, and deleting it from the
client would mean trusting the client to delete other people's rows too.
**↳** Is there parental consent? Can a parent delete the row? **(Be honest: not yet. A deletion path is a real gap and naming it as future work is better than inventing one.)**

### Q42. What if the phone is offline?
**A.** Saving never blocks. The run is recorded to the device *before* the network
is touched, the panel says "Your result is saved on this device, we will send it
when you are online", and that is true. The leaderboard retries next time the main
menu opens.
**T.** `PlayerProfile.NeedsLeaderboardSync()` compares `synced*` against the real
totals; `PushIfNeeded()` makes no request at all when they match.
**↳** ⚠ But the child cannot play offline at all, can they? **(Correct, and it is the bigger limitation: recognition itself is a network service — `preferOfflineRecognition: false`. For classroom tablets with no signal that is a real constraint, it is in the known limitations, and offline recognition is a 0.2.0 question.)**

---

## F. Testing, evaluation, methodology (Q43–Q47)

### Q43. How did you test this?
**A.** Four layers. Compile verification outside Unity with warnings as errors.
An editor self-check that re-proves no two taught words can be confused. Nine
functional checks against the live backend, including two writes that must be
*refused*. And device testing on a real Android phone, which is where the
pronunciation issue was found.
**T.** `Tools/compile_check.sh` (both assemblies, exit 0 as of today),
**Check Pronunciation Matching**, `Tools/check_backend.py`,
`Tools/measure_backend.py`, `Tools/perf_probe`, `Tools/completeness_report.py`.
**↳** ⚠ Do you have automated unit tests? **(Be honest: no NUnit test suite. What exists is an editor self-check that proves the matching property, a compile gate, and live backend checks. Saying "a proper test suite is the gap" is better than calling the self-check a unit test.)**

### Q44. What is your development methodology?
**A.** Iterative and incremental, with version control as the backbone — 351
commits across three members, feature branches merged to main, and a documented
release process that produces a numbered artifact tied to a commit.
**T.** `Docs/RELEASE_PROCESS.md`, `Docs/VersionControl_Group33.md`. The build
refuses to run if the version, the enabled scenes or that version's release notes
are missing, and writes `BUILD_INFO.txt` naming the commit it came from.
**↳** Agile? Scrum? Sprints? **(Say what you actually did, not a textbook name.)** Who did what? **(Daquis — game logic; Lacida — UI and testing; Manzanero — documentation.)**

### Q45. How will you evaluate the game?
**A.** On functionality, usability and usefulness as a supplementary Filipino
vocabulary tool — Objective 5 — with selected respondents. Deliberately **not** a
learning-gain claim.
**T.** That restraint is argued in Chapter 2 §2.4: Muñoz et al. (2025) found a
well-received game producing no significant post-test gain in public-school
pupils, which is why this study is positioned as supplementary and scoped to
those three criteria.
**↳** ⚠ Why no pre-test/post-test? **(Because a learning-gain design needs a control group, a longer intervention and an instrument we have not validated — and claiming a gain we cannot support is worse than not claiming one. Note: Chapter 2's conceptual framework still lists a post-test under OUTPUT. Reconcile that before the defense — see `07_Paper_vs_Build.md`.)**

### Q46. What is your performance baseline?
**A.** Measured, not estimated. The leaderboard fetch has a median of 226.6 ms, of
which 128.7 ms is connection setup. The pronunciation judgement costs 5.64 µs and
682 bytes per attempt.
**T.** `Tools/measure_backend.py 20` splits each call into DNS, TCP, TLS and
server time; `Tools/perf_probe` is a .NET harness over the checker. Baseline taken
13 November 2026 against the v0.1.0 sources, recorded in
`Docs/Activity10_Performance.md`. All of it is re-runnable.
**↳** Is 226 ms acceptable? **(Yes — it is a menu fetch, not in the gameplay loop; and more than half of it is one-time connection setup.)**

### Q47. What were your biggest technical challenges?
**A.** Three, and each has a story. One: the matcher accepting wrong vowels, which
forced the rewrite from edit distance to canonicalization. Two: the recognizer
hearing Rachel through the speaker and scoring her as the child, which forced the
suspend/resume design. Three: the recognizer returning the nearest real word on a
device, which is the open one.
**T.** A fourth, smaller and worth having ready because it shows debugging: the
newer `sb_publishable_` Supabase keys are not JWTs, so sending one as a bearer
token made PostgREST 401 everything. `SupabaseClient.ApplyHeaders` now sets
`Authorization` only for legacy `eyJ…` keys.
**↳** How did you find each? **(Playtesting, device testing, and reading the 401 body in the log — which is itself why the log carries the body and the UI does not.)**

---

## G. Limitations and future work (Q48–Q52)

### Q48. What are the limitations of your system?
**A.** Lead with the real one. Pronunciation correction is not yet reliable on a
device. Then: Android only; nothing works offline; one of sixteen designed scenes
is unbuilt; fifteen of the most-heard narrator lines are unrecorded; twelve words
are still taught by caption; no mini game; no offline mode; and the game cannot be
played by a child who cannot produce clear speech.
**↳** Which of those worries you most? **(The first one, and say why.)**

### Q49. What would you do with another six months?
**A.** In order: a children's-speech recognizer, which is what Santiago et al.
recommend and what would actually close the gap; the remaining recordings; Bahay
scene 6; offline recognition; a mini game; and a proper evaluation with pupils.
**T.** And the architecture is ready for the first one — `VoiceCommand` is the only
script that knows the recognizer exists.
**↳** Which is most important? Which is most realistic?

### Q50. ⚠ Is this actually 60% complete?
**A.** By our own stated criteria it is 82%, weighted across eight dimensions, and
I can recompute it in front of you —
`python3 Tools/completeness_report.py` reads the repository rather than our
memory. The weighting is a judgement; the arithmetic is not. Fifteen of sixteen
scenes, all 72 objectives wired, 401 of 436 voice lines recorded, the full data
layer live.
**T.** And the deliberately low score inside it: 25% on "pronunciation correctness
on a device", which is the one that matters most and why the overall figure is not
higher.
**↳** Isn't 82% convenient? **(Offer the criteria for challenge: "disagree with the weights and I'll recompute — here they are.")**
**📂** Run the report live.

### Q51. ⚠ Why should a school use this instead of a teacher?
**A.** It should not, and we never claim it should. One teacher handling forty
pupils gives each child a few chances to say a word out loud in a lesson. This
gives one child a hundred chances in ten minutes, with nobody listening and
nothing at stake. It is practice between lessons, which is the thing that is
missing — Chapter 1 §1.5 says it is supplementary and not a replacement.
**↳** What evidence that practice helps? **(Swain 1985 on output; Kumar et al. 2012 found vocalising words outperformed receptive training; Bashori et al. 2024 found ASR practice improved vocabulary and pronunciation together.)**

### Q52. ⚠ What is the single weakest part of your system?
**Answer this honestly. A rehearsed non-answer is worse than the weakness.**

**A.** The pronunciation correction on a real device. Everything around it — the
teaching, the prompting, the repetition, the scoring, the data — works and can be
shown. The judgement itself is sound in our tests and unreliable in the field,
and we know the mechanism. That is the finding of this phase and it is what
defines the next version.
**↳** Does that invalidate the project? **(No. The contribution is a voice-controlled Filipino game for pre-readers, which does not exist in the reviewed work, plus an empirical finding about why stock recognizers are not sufficient for it — which is the gap Santiago et al. name and which nobody had tested inside a game.)**

---

## The five answers to have word-perfect

1. **"kaha for Kuha"** — why edit distance failed for Filipino.
2. **"53 words fold to 53 distinct canonical forms"** — why exact matching is safe.
3. **"the recognizer hands us an already-corrected word"** — the device problem, named before anyone asks.
4. **"four values leave the device"** — id, nickname, points, stars.
5. **"the client cannot write the leaderboard"** — Edge Function, token, SHA-256, constant-time.
