# SALINLAHI — The Five-Minute Demonstration

Five minutes is short. The script below is **4:40 of content and 20 seconds of
slack**, so a slow scene load does not cost you the ending.

**One decision first: demo on the phone.** In the Editor the mechanic is a
keyboard shortcut, and "so where is the voice recognition?" is a question you
would be handing them. If the phone is genuinely impossible, see *Plan B*.

---

## Before you walk in

| Check | Why |
|---|---|
| Phone charged, screen timeout **off**, Do Not Disturb **on** | a notification mid-demo is a lost minute |
| Media volume at max; speaker not covered by a case | the panel has to hear Rachel |
| **Internet working on the phone** | recognition is a network service — no signal, no demo |
| Test the room's Wi-Fi, or bring mobile data / a hotspot | the room's network is the single likeliest failure |
| A **fresh profile** created, or progress reset | a locked map or a half-finished run breaks the story |
| `Chapter1_Level1_UmagaNa` **unlocked and unplayed** | |
| App opened once already so the mic permission is granted | the permission dialog costs 15 seconds and looks unrehearsed |
| Second phone with the same APK, loaded and ready | the fastest possible recovery |
| Screen mirroring tested *in the room* | AirPlay/Vysor failing on stage is a classic |
| Laptop open on the Editor, on `MainMenu.unity`, as Plan B | |
| Terminal open at the repo root, ready to run the completeness report | for the question that follows the demo |
| Rehearsed **three times on the demo phone**, out loud, with a timer | |

**Practise saying the words loudly and slowly.** You are not speaking to a
panel, you are speaking to a phone across a room. Over-articulate.

---

## The script

### 0:00 – 0:30 · Frame it (30 s)

> "SALINLAHI is a Filipino vocabulary game for Kindergarten to Grade 3. The
> difference from every other Filipino learning app is one sentence: **the child
> does not tap an answer, the child says the word, and saying the word is what
> makes the game move.** Nothing on this screen advances by touch except the
> menu buttons.
>
> I will show you four things in five minutes: a word spoken correctly, a word
> spoken wrongly, what the child sees at the end of a scene, and where that
> result goes."

*Hold up the phone. Main menu is already on screen.*

**Do not** read the title slide. **Do not** re-explain the objectives. The
chapters presentation has already done that.

### 0:30 – 1:00 · Menu → map → scene (30 s)

Tap **ADVENTURE**.

> "This is the journey map. Three places — Bahay, Paaralan, Parke — fifteen
> scenes. Each tile opens when the one before it is finished, and the stars
> under a tile are the best that scene has ever been played, not the last."

Tap scene 1, **Umaga Na**.

> "One scene is one situation. This one is three words."

*(Pick this scene deliberately: three words fits the clock, and it is the one
scene with a bespoke opening — the snore, the sit-up, the stretch.)*

### 1:00 – 2:00 · The teaching sequence, then a correct word (60 s)

Let Rachel run. **Do not talk over her.** Narrate only in the gaps.

> "Three lines, in this order, every time. What is happening. What the word
> means in English. Then what to say. Notice the word on the badge is **faint**
> right now — it is not the child's turn until she finishes, so a child cannot
> answer over the teacher."

When the badge brightens:

> "Now it is live."

Say clearly: **"BANGON."**

Kylo sits up.

> "That is the whole mechanic. The word was the control. The microphone has been
> listening the whole time — there is no button to press, which matters when
> your user is five."

Say **"TAYO."** He stands.

> "And notice the game waited for him to finish sitting up before it asked for
> the next word. Without that, the next prompt arrives while he is still moving."

### 2:00 – 2:45 · A wrong word, on purpose (45 s)

> "Now the part that actually matters for a language game."

Say a deliberately wrong word — **"MESA."**

> "No penalty. No lives, no timer, no buzzer. The scene just waits. That is a
> design decision: the user is four to eight and speaking a second language, and
> punishing a wrong attempt punishes exactly the behaviour we want."

Say it wrong a second time. Rachel repeats the word slowly, unasked.

> "Two misses and she says it again, slowly, without being asked. That is the
> most important accessibility behaviour in the game — a child who cannot reach
> the replay button still gets help."

Then, **the sentence that pre-empts the hardest question:**

> "And here is the honest part, which I would rather say than have you find.
> Our matcher is strict about vowels on purpose — 'kaha' must not pass for
> 'Kuha'. It does that correctly in our own tests. On a real device we found
> that a mispronounced word is still sometimes accepted, because Android's
> Filipino dictation service returns the nearest real Filipino word rather than
> the sounds the child made — so we are handed an already-corrected string. That
> is exactly the limitation Santiago et al. report for Filipino children's
> speech, it is the first known issue in our release notes, and it is what
> defines the next version."

*Say this calmly and move on. Do not apologise, do not elaborate unless asked.*

### 2:45 – 3:15 · Finish the scene (30 s)

Say **"LAKAD."** Kylo walks to the door; the scene ends.

> "Third word, scene over."

### 3:15 – 4:00 · The summary panel (45 s)

> "This is what the child and the adult beside them see. Words said correctly,
> how many tries it took, the stars — and this row, which is the one that
> matters: **every Filipino word they said**, listed, with nothing truncated.
> Two numbers say how well it went. This says what was learned, and that is what
> a parent or a teacher actually asks about.
>
> Stars are earned on accuracy, never on speed. And the game keeps the best run
> a scene has ever had, so a child who three-starred this and stumbles on a
> replay does not lose it."

> "Finishing the fifth scene of a place writes one record to the database — the
> name, the score, the attempts, the level — and earns that level's badge."

### 4:00 – 4:40 · The leaderboard, and how it is protected (40 s)

Back to the main menu, open the leaderboard.

> "This is the one thing that leaves the phone, and it is four values: a player
> id, the nickname, total points, total stars. Which words the child found hard,
> how many tries each took, which scenes they played — a child's learning record
> — never leaves the device.
>
> And the game cannot write this table. It posts to a server-side function,
> which is the only thing holding a key that may write. The first submission
> registers the player and the server hands back a token; only its hash is
> stored, and every later submission has to present it. So one child's id does
> not let anyone post as them."

> "That is SALINLAHI. Fifteen scenes, seventy-two spoken objectives, fifty-three
> Filipino words — and every one of them is said out loud."

**Stop. Hands off the phone. Do not fill the silence.**

---

## Where the spare 20 seconds goes

Hold it in reserve. If everything has run clean and you still have it, add one
line only:

> "Every number I just gave you is read out of the repository by a script rather
> than remembered — `Tools/completeness_report.py`. I can run it now if you want."

That is an invitation, not a demo. If they say yes, it is 15 seconds and it is
the strongest thing in your slot.

---

## If something breaks

**Rule: narrate once, recover in ten seconds, never debug on stage.**

| Failure | Do this |
|---|---|
| **A word is not recognised** | Say it once more, louder and slower. Still nothing: *"That is the recognizer, and it is the limitation I mentioned — let me use the replay."* Tap replay, try once more, then **move on** to the next word. Never try three times. |
| **No network** | *"The recognizer is a network service — that is in our known limitations."* Switch to the hotspot. If that fails, go to Plan B and say so plainly. |
| **App crashes or freezes** | Second phone. Already unlocked, already on the main menu. Do not relaunch the first one. |
| **Mic permission dialog appears** | Accept it and keep talking: *"First launch asks for the microphone — a child does this once."* |
| **Rachel is silent on a line** | *"Fifteen of her most frequent lines are not recorded yet — the caption shows and the pause is held, so the pacing is identical. It is on our list."* This is a prepared answer, not a save. |
| **Screen mirroring dies** | Hold the phone up and keep going. Never stop to reconnect. |
| **You are over time at 4:00** | Skip the leaderboard. Jump to the closing sentence. The summary panel is worth more than the leaderboard. |
| **A panelist interrupts mid-demo** | Answer in one sentence and say *"I'll come back to that in the open forum"* — you have 45 minutes for questions and 5 for this. |

---

## Plan B — the Editor, if the phone is impossible

Open `MainMenu.unity`, press Play, and **say this first, before anything else**:

> "Speech recognition only exists on Android and iOS — the plugin binds to the
> device's native service and has no desktop build. So in the Editor the only
> thing that changes is *where the text comes from*: pressing Enter feeds the
> current word through `SimulateSpeech`, which is the exact same code path as a
> real recognizer result. The matching, the scoring and the events all behave
> identically. What you will not see is the recognition itself."

Then run the same script, pressing `Enter` where you would speak and `X` for the
deliberate wrong answer. Everything else — the hold on the faint badge, the
two-miss slow repeat, the pacing, the summary — is real.

Saying that paragraph **before** they notice is the difference between a
constraint and a dodge.

---

## What not to do

- Do not open Visual Studio. Code goes in the open forum, not the demo.
- Do not tap **MINI GAME**. It is deliberately inert; the tooltip in the code
  explains why, and a panelist tapping it is far better than you tapping it.
- Do not demo a scene you have not played on that phone this week.
- Do not say "it should work" or "it works on my machine".
- Do not apologise more than once for the recognizer. Say it, own it, move on.
- Do not run over. The clock is part of what is being graded.
