# Paste-Ready Paper Text

Everything below is written to be pasted straight into the Google Doc. Decisions
taken 15 November: **target users are Grades 2–3** (the approval letter says so),
**architecture diagram and leaderboard subsystem only** for §3.4, and the
device-recognizer limitation **goes into the paper**.

---

## A · Grades 2–3 — the ten replacements

Nine passages plus the §3.2.1 range. Full find/replace strings are in the chat
transcript; the short version is that **"Kindergarten to Grade 3"**,
**"Kinder–Grade 3"** and **"Kindergarten – Grade 3"** (Table 2.1, en dash) all
become **"Grades 2 and 3"**, in §1.1, §1.2, §1.4 Obj. 4, §1.5, §1.6, §1.7,
Table 2.1, §2.5, §3.1 and §3.2.1.

### The one that is not a wording change — §3.2.1, audience assumption 1

Your interface is justified throughout on the users being pre-readers. Grade 2–3
pupils can read. Swap the *reason*, not the design.

> **Find:** `1. Pre readers. A child who cannot yet read must not be blocked. Every instruction is delivered as a voice line together with a picture. On screen text is a label for an accompanying adult, never the sole carrier of an instruction.`
>
> **Replace:** `1. Readers of English, not of Filipino. A Grade 2 or Grade 3 pupil can read, but an English-dominant child reading a written Filipino word learns little about how to say it. Filipino is written without stress marks, so the spelling tayo does not indicate whether the word is tayô, to stand up, or táyo, we. Every instruction is therefore delivered as a voice line together with a picture, and the child hears the target word before being asked to produce it. On screen text is a label, never the sole carrier of an instruction.`

Then in **§3.3, design principle 1**, keep the Getman citation but change what it
is cited *for*:

> **Find:** `in line with the use of audio and visual cues without text for pre-readers by Getman et al. (2023)`
>
> **Replace:** `in line with the use of audio and visual cues rather than textual explanation by Getman et al. (2023), whose app was designed for children who learn by imitation`

And in **§3.3, opening sentence**:

> **Find:** `the players are young children, some of whom cannot yet read`
>
> **Replace:** `the players are young children who read English but not yet Filipino with fluency`

**Why this is stronger than the pre-reader argument:** it is demonstrable in
code. `Assets/Scripts/WordStress.cs` marks six of the taught words precisely
because the plain spelling is ambiguous. When a panelist asks why a game for
readers avoids text, you can point at the file.

---

## B · §1.3 — Problem Statement, rewritten

Your current specific problems are design tasks ("How can a voice-interactive
game be developed…"), which a panel will read as a work plan rather than
research questions. Each replacement below names the **learner's constraint**
first and the system as the response.

### General Problem

> **Find:** `Students may have difficulty understanding and remembering Filipino vocabulary, particularly when they have limited exposure to or practice with the language. Traditional learning activities may provide opportunities to study Filipino words, but students may need additional activities that allow them to practice the words in a more active way.`
>
> **Replace:** `Grade 2 and Grade 3 pupils who use English as their main language at home understand spoken Filipino but have few opportunities to produce it. A class period divides one teacher's attention among an entire section, so each pupil says a given word aloud only once or twice, if at all. The materials available outside class — worksheets, flashcards, and tap-based mobile applications — ask the learner to recognise a word rather than pronounce it, so a pupil can complete an activity correctly without ever having said the Filipino word.`

### Specific Problems

> **Replace the five numbered items with:**
>
> `1. How can a learning activity require a pupil to pronounce a Filipino word before progressing, so that the practice is productive rather than receptive?`
>
> `2. How can a spoken Filipino word serve as the control input of a game, so that each target word is bound to an action the pupil can see happen?`
>
> `3. How can repeated, low-pressure speaking practice be made available to a pupil beyond the few speaking turns a class period allows?`
>
> `4. How can an automatic speech recognition service interpret the developing pronunciation of Grade 2 and Grade 3 learners reliably enough to judge a spoken answer as correct or incorrect?`
>
> `5. How can the developed game be evaluated in terms of functionality, usability, and its usefulness as a supplementary Filipino vocabulary learning tool?`

**The §1.4 mapping paragraph still holds** — Objective 1 → Problem 1,
Objective 2 → Problems 2 and 4, Objective 3 → Problem 3, Objective 5 →
Problem 5 — so it does not need rewriting. Check it reads correctly after the
edit.

**Note what Problem 4 now does.** It asks whether recognition *can* reliably
judge children's speech. That is a genuine research question, you investigated
it, and **your answer is partly no.** Section D below is that answer. A problem
whose honest finding is a limitation is still a problem answered.

---

## C · §3.4 — System Architecture, expanded

Replace the existing single paragraph with the following. Insert the figure
between the second and third paragraphs.

> `SALINLAHI is a client application that runs on the player's device, with a hosted database reached over the network. Figure 3.4 shows the arrangement.`
>
> `Four layers run on the device. The presentation layer holds the seventeen Unity scenes, the illustrated scene art, the characters, and the user interface, including the narration panel and the summary panel. The game logic layer holds the objective sequence of each scene, the pronunciation checker, the narration director that decides what the teacher character says and when, and the progression rules for scenes and levels. The speech input layer is a single component that wraps the recognition plugin; it is the only part of the system that is aware the recognizer exists, which is what allows the recognizer to be replaced without touching the rest of the game. The local data layer writes the player profile to the device's own private storage.`
>
> `[FIGURE 3.4 HERE]`
>
> `The layers communicate by events rather than by direct reference. The speech input layer raises an event carrying the recognized text and the verdict; the objective sequence, the score counter and the narration director each subscribe to it independently. No layer holds a reference to the layer above it. This is why the scoring could be changed from per-scene to per-level, and the narration added as a whole, without altering the recognition path.`
>
> `The backend has three parts. The database holds three tables: one record per completed level, one row per player on the shared leaderboard, and a separate table holding the hash of each player's submission token. The game reads and writes the level records over the database's REST interface using a publishable key. The leaderboard is different: the game may read it but may not write it, and every change goes through a server-side function described in Section 3.4.3.`
>
> `Only four values ever leave the device: the player identifier, the nickname, the total points and the total stars. Which words a pupil found difficult, how many attempts each took, and which scenes were played remain in the device's own storage and are not transmitted.`

**Figure caption:**

> `Figure 3.4. SALINLAHI system architecture. Four layers run on the player's device; only the leaderboard path reaches the network. The client is never trusted with a direct write to the leaderboard table.`

**The figure file** is `Docs/Figures/Figure_SystemArchitecture.png` (2360 × 1520,
ready to paste). Set it to **In line**, not wrap-text, and match its width to
Figures 3.1 and 3.3.

---

## D · §3.4.3 — the leaderboard subsystem (new subsection)

This is implemented and the paper does not currently mention it exists. Add it
after §3.4.2.

> `3.4.3 Leaderboard and Submission Control`
>
> `The game keeps a shared leaderboard so that a pupil can see their total against other players. Because a score is produced on the device, the client cannot be trusted to write the leaderboard directly, and it is not permitted to. Row Level Security on the database gives the game's publishable key permission to read the leaderboard table and nothing else. Every change is made by a server-side function, which holds a privileged key that is never included in the game, the source repository, or any build.`
>
> `A player identifier is generated on the device and is therefore an identifier rather than a secret. The first submission for a given identifier registers that player and the server returns a token, which the game stores and must present with every later submission. Only a hash of the token is stored, in a table that the game's key cannot read at all. Learning another player's identifier therefore does not permit submitting scores as that player.`
>
> `The function also applies bounds to what it accepts. Totals must be whole numbers, the star total may not exceed what the game is able to award, points may increase by only a limited amount in a single submission, totals may not decrease, and a single row may not be updated more often than once every few seconds. Every refusal is written to the function's log with its reason.`
>
> `These checks bound what a falsified submission can achieve; they do not establish that a score is genuine. The totals remain the client's account of what happened, because no server can verify a spoken answer after the fact. This is stated as a limitation rather than presented as authentication.`

That last paragraph is deliberate. A panelist who finds the hole themselves
scores a point; a paper that names it first does not have a hole.

---

## E · The recognition limitation — paste at the end of §3.2.2

> `A limitation of device-based recognition. The matching rule above was verified against the full set of taught words, and no two of them can be confused by it. Testing on a physical Android device, however, showed that a mispronounced word is sometimes still accepted during play. The probable cause lies outside the matching rule. The device's Filipino dictation service is a language model, and when it receives an approximate pronunciation it returns the nearest valid Filipino word rather than a transcription of the sounds produced. The checker is therefore given a word that has already been corrected, and the learner's error never reaches it. This cannot be corrected within the matching rule, because the information required to detect the error has been discarded before the rule is applied.`
>
> `This outcome is consistent with the literature reviewed in Section 2.2. Bhardwaj et al. (2022) report that automatic speech recognition research has been built largely on adult speech and performs poorly on children, whose articulatory and acoustic characteristics vary widely; Santiago et al. (2025) measured a word error rate of 39.70% when a recognizer trained on adult Filipino speech was applied to Filipino children's speech, and recommended that a recognizer fine-tuned on children's speech be embedded in an interactive learning platform. The present build relies on the device's general-purpose recognizer, and the result described above is the practical consequence of that choice. The game therefore teaches, prompts and repeats the target vocabulary as designed, but does not yet reliably correct a mispronunciation. Addressing this requires a recognition component trained on children's speech rather than a change to the matching rule, and is identified as the principal direction for further development.`

**Where else to reference it:** add one sentence to §1.5 Scope and Delimitations
noting that the reliability of the right/wrong judgement is bounded by the
device's recognition service, and carry the same point into your Chapter 4
findings.

---

## F · Chapter 4 — the respondents

Decided: Grades 2 and 3, one section, 30 enrolled, at least 20 expected to
attend. Pilot testing on **Wednesday 18 November**.

> `The respondents of this study are the Grade 2 and Grade 3 pupils of [SCHOOL NAME], who constitute the target users described in Section 1.7. Total enumeration was used rather than sampling: all 30 enrolled pupils were included, since the population is small enough to be covered completely and no basis existed for selecting among them. Actual participation was determined by attendance on the day of testing. [N] pupils took part.`
>
> `[NUMBER] teachers handling Filipino in these grade levels also served as respondents for the usefulness criterion, since the game is positioned as a supplementary classroom tool and its fitness for that role is better judged by the teacher than by the learner.`

**Three warnings.**

1. **Do not write "random sampling."** You are not sampling — you are taking the
   whole population. The term is **total enumeration** (or complete enumeration).
   Using "random" for a complete population is the kind of error a research
   panelist catches instantly and remembers.
2. **Fill in `[N]` after Wednesday, not before.** If you write 20 and 17 attend,
   your paper is wrong and your attendance sheet proves it.
3. **Keep the attendance sheet.** It is your evidence that the number is real,
   and it belongs in your supporting documents.

### What to capture on Wednesday

You are testing three days before the defense. That is a significant advantage
over most groups and it is worth preparing for properly.

- **The attempt log from every device.** `VoiceCommand.logAttempts` is on by
  default; pull the CSVs afterwards. That file is the only direct evidence you
  will ever have of how the recognizer behaves with real children's speech, and
  it speaks straight to Problem 4 and to Section E above.
- **Photos.** Required for your supporting documents, and worth more than any
  slide.
- **How many pupils finished a scene unaided**, and **how many needed the slow
  repeat** — the 2-miss hint. One number each.
- **Anything a child said that the game rejected but an adult would accept.**
  Write the word and what they said. Those strings go straight into
  `WordVariants`.
- **Every crash, freeze or confusion**, with the scene name.

Three facts from Wednesday turn "we plan to evaluate" into "we evaluated on
18 November with N pupils, and here is what we found." That is the single
largest upgrade available to your defense this week.
