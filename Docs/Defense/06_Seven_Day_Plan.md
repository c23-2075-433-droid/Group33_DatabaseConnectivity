# Seven-Day Plan and Defense Requirements Checklist

Defense: **20–23 November 2026 (Biñan)**. Today is **Sunday 15 November**.
That is five full working days plus the weekend you are in.

Everything is sized for three people. Owners are suggested from the roles in the
README — Daquis (game logic), Lacida (UI and testing), Manzanero (documentation)
— change them to suit.

---

## Part 1 — The requirements checklist, from the LMS post

Read off the Fin-Exam instructions. **Items 1–6 are not yours to leave to the
last day.**

| # | Requirement | Owner | Status |
|---|---|---|---|
| 1 | Check **tcpms.base44.app** for your slot and panel | all | ☐ **do today** |
| 2 | **Defense fee** ₱3,000 per group, divided by members. Biñan collector is by section — Anahaw (CS01–IT11 day 1), Mane (IT12–IT22), Subion (IT23–IT33), Soquino (EM34). **You are IT33 → Subion.** Amount per head is in the Google Chat post | all | ☐ |
| 3 | SOA will state the fee "next week" — watch for it | all | ☐ |
| 4 | **Formal / corporate attire** | all | ☐ |
| 5 | **Supporting documents**, attached to the Fin-Exam post **as a link**: logbooks, datasets, forms, picture evidence, sheets or soft copies, **approval letter**, **preliminary survey** | Manzanero | ☐ |
| 6 | **Chapters I–III with title page**, attached as a link | Manzanero | ☐ |
| 7 | **Slide presentation** uploaded to the post as a link, by the **document editor**, following the attached slide-content template | Manzanero | ☐ |
| 8 | **60% system demonstration** | Daquis | ☐ |
| 9 | **Do not read the slides.** Present from knowledge | all | ☐ |

**The slot, in full:** 5 min preliminary setup · 5 min slides (Chapters I and
III) · 5 min system demo · **45 min** comments, recommendations and deliberation.

Note the shape of that: **45 of the 60 minutes are questions.** The demo and the
slides are the smaller half. Prepare accordingly — `04_Defense_Questions.md` is
the document that matters most.

Also note the slide deck is **Chapters I and III only**. No literature review
walk-through. Chapter I is the problem and the objectives; Chapter III is the
design and the architecture.

### Documents you already have that count as evidence

Have these ready as soft copies, because several of them directly answer panel
questions:

- `Docs/Vocabulary_Validation_Form_ToSend.md` / `Vocabulary_Validation_Review.md` — the validating teacher
- `Docs/release/v0.1.0/RELEASE_NOTES.txt` — honest known issues
- `Docs/Activity9_Security.md` + the two SQL migrations — security
- `Docs/Activity10_Performance.md` — the measured baseline
- `Docs/VersionControl_Group33.md` + 351 commits — process
- `Docs/VoiceOverScript.csv` — 436 scripted lines
- A printed `Tools/completeness_report.py` output, dated

---

## Part 2 — The seven days

### Day 1 — Sunday 15 Nov · Find out where you stand

| Task | Owner | Time |
|---|---|---|
| Check tcpms.base44.app for the slot, date, room and panel names | all | 15 m |
| Build the current APK and install it on **two** phones | Daquis | 1 h |
| **Play all 15 scenes end to end on a phone**, taking notes — this has never been done in one sitting | Lacida | 1.5 h |
| Run `Tools/compile_check.sh`, `Tools/check_backend.py`, `python3 Tools/completeness_report.py`. Save the output with today's date | Daquis | 30 m |
| Read `01_System_Reviewer.md` §12 — the ten things — out loud, twice | all | 30 m |
| Confirm who collects the fee (Subion) and the per-head amount from Google Chat | Manzanero | 15 m |

**End of day you must know:** your slot, whether the full game is playable on a
phone, and which scenes misbehave.

---

### Day 2 — Monday 16 Nov · The recognizer experiment

This is the most important technical day of the week. The question is not "can
we fix it" — it is "can we say something precise about it".

| Task | Owner | Time |
|---|---|---|
| **Set `VoiceCommand.useFreeFormLanguageModel = false`**, rebuild, test one scene on the phone | Daquis | 1 h |
| Play Banyo and Almusal with `logAttempts` on, both model settings, **deliberately mispronouncing** — "kaha" for Kuha, "kanin" for Kain, "tabo" for Takbo | Lacida | 1 h |
| Pull the attempt CSV off the phone (`adb pull`, command is in `VoiceAttemptLog`) and read the raw transcription column | Daquis | 45 m |
| Also log `OnPartialResultReceived` — the partial may be less "corrected" than the final | Daquis | 45 m |
| Write one page: *what we tried, what came back, what we conclude* | Manzanero | 45 m |
| Add the four missing `WordVariants` rows — **Hugas, Kinig, Ligpit, Tapon** — plus any real mistranscriptions from the CSV. Run **Tools > SALINLAHI > Check Pronunciation Matching** | Daquis | 30 m |
| **Rotate the ElevenLabs API key.** It is still reachable in git history at commit `0250197` | Daquis | 20 m |

**Outcome:** whichever way the experiment goes, you walk into the defense with
*data* about the recognizer instead of an apology. If the search model is
better, ship it. If it is not, you have a table that proves the problem is the
platform and not your checker — which is the stronger story anyway.

---

### Day 3 — Tuesday 17 Nov · Fix the paper

`07_Paper_vs_Build.md` §5 is the ordered edit list. All of it, today.

| Task | Owner | Time |
|---|---|---|
| §3.2.4 scene table + totals → **72 / 53 / 19**, regenerated from the report | Manzanero | 1 h |
| §3.2.2 + §3.4.2 matching rule → canonicalization, not edit distance. Replace the Opo examples | Manzanero + Daquis | 1 h |
| Add the device-recognizer limitation, cited to Bhardwaj and Santiago | Manzanero | 30 m |
| Add the leaderboard + Edge Function + token + RLS to §3.4 | Manzanero + Daquis | 45 m |
| Fix §3.5.1 (per level / per scene, both ways round) and §3.2.3 (Rachel, Tabo, Opo) | Manzanero | 30 m |
| Remove the pre/post-test from Chapter 2's IPO model | Manzanero | 20 m |
| Reconcile the age range across §1.1/§1.7/§3.2.1 | Manzanero | 15 m |
| Answer the two `[CONFIRM:]` tags — installed size off the phone, device table | Lacida | 30 m |
| Fix the README (49/66 → 53/72; Rachel's portrait is assigned) and the two stale docstrings | Daquis | 20 m |
| Export a fresh PDF of Chapters I–III with the title page | Manzanero | 15 m |

**Outcome:** no number in the paper contradicts the build. This removes an
entire category of question.

---

### Day 4 — Wednesday 18 Nov · Slides and the demo

| Task | Owner | Time |
|---|---|---|
| Build the deck against the provided template — **Chapters I and III only** | Manzanero | 2 h |
| One architecture slide, drawn from `01_System_Reviewer.md` §3 | Daquis | 45 m |
| One slide of real numbers: 15 scenes · 72 objectives · 53 words · 401/436 lines voiced · 82% by stated criteria | Manzanero | 20 m |
| **Rehearse the 5-minute demo on the demo phone, three times, timed** | Daquis | 1 h |
| Set up and test screen mirroring, then test it again on a different network | Lacida | 45 m |
| Record a 5-minute screen capture of a clean run **as insurance** | Lacida | 30 m |
| Upload the deck and the Chapters link to the Fin-Exam post | Manzanero | 20 m |

**Outcome:** the deck is submitted, the demo is under 5:00 three times running,
and there is a video on a USB stick if the phone dies.

**Record the backup video even if everything works.** It costs 30 minutes and it
is the only thing that saves a demo slot when the room has no Wi-Fi.

---

### Day 5 — Thursday 19 Nov · Questions

| Task | Owner | Time |
|---|---|---|
| Each member reads all 52 questions in `04_Defense_Questions.md` | all | 1.5 h |
| Each member owns a block: Daquis → C (voice) + B (architecture); Lacida → D (UX) + F (testing); Manzanero → A (background) + E (data) + G (limitations) | all | — |
| **Mock defense round 1** — 20 questions, out loud, timed, no notes. Score each answer 1–10 | all | 1.5 h |
| Rewrite the answers that scored below 7 **in your own words**, not mine | all | 1 h |
| Drill the five word-perfect answers at the end of `04_Defense_Questions.md` | all | 30 m |
| Print: completeness report, the recogniser experiment page, release notes known issues, the hardening SQL | Manzanero | 30 m |

**Outcome:** nobody has to look anything up. Everyone can answer in any block —
a panel will not politely ask the right person.

---

### Day 6 — Friday 20 Nov · Dress rehearsal

**If your slot is today, this is the morning-of list instead — skip to Part 3.**

| Task | Owner | Time |
|---|---|---|
| Full run: 5 min slides + 5 min demo + 30 min questions, timed, in corporate attire | all | 1.5 h |
| **Mock defense round 2** — the hardest 15 questions only: Q14, Q19, Q21, Q24, Q25, Q30, Q40, Q43, Q45, Q50, Q52 | all | 1 h |
| Re-run `completeness_report.py` and reprint it with today's date | Daquis | 15 m |
| Charge both phones; load the backup video onto a USB stick | Lacida | 20 m |
| Pack: both phones, chargers, USB-C cable, HDMI/mirroring adapter, printed evidence, hard copies of Chapters I–III, the signed approval letter and the preliminary survey | all | 30 m |
| Confirm the fee is paid and the SOA is settled | Manzanero | — |
| **Stop working by 21:00.** Sleep is worth more than one more fix | all | — |

**Freeze the build today.** No code changes after this point. A last-minute
change you have not played through on the phone is how a working demo breaks.

---

### Day 7 — Defense day

See Part 3.

---

## Part 3 — Defense morning

**Two hours before**

- Charge both phones to 100%. Screen timeout off. Do Not Disturb on.
- Open the app on both, so the mic permission dialog is already past.
- Create or reset the demo profile; confirm `Umaga Na` is unlocked and unplayed.
- Test the recognizer once on the room's network. **No internet, no demo** —
  find out now, not in the slot.
- Run `python3 Tools/completeness_report.py`, print it, date it.

**The five minutes of preliminary setup are yours.** Use them to connect the
mirroring, open the app, and test one spoken word quietly. Do not use them to
open your slides and talk.

**During the 45 minutes**

- Answer in one or two sentences, then stop. Let them ask for more.
- If you do not know: *"I don't have that number in front of me — it is in
  [the file], and I can pull it up."* Never guess a number.
- If a panelist is wrong about your system, correct it once, politely, with the
  evidence. Do not argue twice.
- If they find something you already know about: *"Yes — that is in our known
  issues, and here is why."* That is the best possible moment for a known weakness.
- Whoever owns a block answers it. Do not all talk at once, and do not let one
  person answer everything — panels notice.

**Say the recognizer limitation yourselves, in the demo, before anyone asks.**
It is the single highest-leverage thing in this whole plan.

---

## Part 4 — If you only have three days

Cut to these, in order:

1. The recognizer experiment and the attempt log (Day 2)
2. The paper's scene table, totals, and matching rule (Day 3, items 1–2)
3. Three timed demo rehearsals on the phone (Day 4)
4. Mock defense on the eleven hardest questions (Day 5)
5. The backup video

Everything else is improvement. These five are the difference between a defense
you control and one you survive.
