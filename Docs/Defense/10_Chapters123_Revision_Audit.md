# Chapters 1–3 — Revision Audit

Google Doc `IT33 - Manuscript Draft` (revision read 17 November 2026, all 331 paragraphs
plus the 4 inline figures and 5 positioned header images), checked against the repository
at `0a3d6c7` (branch `main`).

**There is no `12_Manuscript_Revisions_Final.md` in this repository or anywhere on this
Mac.** The previous revision instructions are in three files, and this audit covers all of
them:

- `Docs/Defense/07_Paper_vs_Build.md` — the 10-item edit list in its §5 (most recent)
- `Docs/Paper_Open_Revisions.md` — what was still open after the last pass
- `Docs/Paper_Chapter3_Confirmations.md` — the seven `[CONFIRM:]` answers and the 3.5 rewrite

**Verdict:** the paper is in good shape. Nine of the ten items on the previous edit list are
fully applied, and two of them were resolved better than they were suggested. The one serious
problem is not in the paper at all — **your last commit broke the game's Parke vocabulary, and
the paper is the thing that is right.** Everything else is seven small, local fixes.

---

## 1. Completed — do not revisit these

| # | Previous instruction | Status | Verified how |
|---|---|---|---|
| 1 | §3.2.4 scene table rebuilt from the build | **DONE** | Bahay and Paaralan rows match the scene files word for word |
| 1b | All three totals recomputed and the derived sentences fixed | **DONE** | Recomputed from your own table: 72 objectives, 51 distinct words, 21 re-asks, 13 repeated targets, 10 cross-environment targets, Bahay 23 / Paaralan 24 / Parke 25. Every figure in §3.2.3, §3.2.4 and §3.6 agrees |
| 2 | §3.2.2 and §3.4.2 matching rule — drop the edit-distance tests | **DONE** | Now canonicalization + canonical phrase / single token / joined input / 7-character one-edit exception with vowels preserved. The collision rule ("pag kain" ≠ Kain) is there too |
| 2b | Replace the Opo examples | **DONE** | Opo is gone from the paper. Examples are now "u po" → Upo and kaha ✗ Kuha |
| 3 | Add the device-level mispronunciation limitation | **DONE** | §3.1 and §3.4.2 both state the failed wrong-pronunciation check |
| 4 | §3.4 — add the leaderboard, token, second network path | **DONE** | §3.4.2 and §3.6: separate endpoint, UUID, token issued at registration, hash stored, ownership and score bounds validated, public read policy |
| 5 | §3.5.1 — record per level, unlock per scene, progress per scene | **DONE** | All three corrected |
| 6 | §3.2.3 — Rachel is the narrator; drop Tabo and Opo | **DONE** | Rachel narrates and the panel identifies her; 423 clips with mouth tracks cited. Tabo is out of worldbuilding and §3.6 says plainly that it stays in the script and assets but has no objective — the honest version |
| 7 | Chapter 2 IPO — remove the pre/post-test | **DONE** | Gone from §1.6, §1.8 and the IPO model. It survives only in the Muñoz et al. citation in §2.2.2, which is correct and should stay |
| 8 | Make the age range agree across the three chapters | **DONE, and better than suggested** | You went to Grades 2–3 (7–9) everywhere rather than K–G3, and rebuilt the justification on MATATAG — Filipino is not a subject until Grade 2. §1.1, §1.5, §1.7, §2.3, §3.2.1 all agree, and Kylo at about eight fits inside it |
| 9 | The seven `[CONFIRM:]` tags | **DONE** (two caveats in §3) | No `[CONFIRM:` string remains anywhere in the document |
| 10 | Bundle id | **DONE** | `ph.edu.uphsl.group33.salinlahi` in `ProjectSettings.asset` |
| — | Figures must be inline, not wrap-text | **DONE** | All four figures (3.1, 3.2a, 3.2b, 3.3) are inline objects. No floating figure remains — the five positioned images are the letterhead and logos |
| — | Palengke and Bahay scene 6 | **DONE** | "State of the Build" declares both outside the final scope, and §3.5.2 says the map shows only the three playable environments |
| — | Where the raw art came from (my open question) | **ANSWERED** | §3.4.2: Canva AI image generator for Kylo and Rachel, then group-edited; Canva element library for backgrounds, props, badges and UI. This was the omission a panel asks about, and it is closed |
| — | APK size and release identity | **VERIFIED CORRECT** | 151.3 MB matches `Docs/Activity8_Manzanero.md`; tag `v0.1.0` does resolve to `c035c8f` |

---

## 2. Needs fixing

### P1 — Your last commit broke the game's Parke vocabulary. The paper is right.

**This is a build regression, not a paper error. Do not edit the paper for it.**

Commit `0a3d6c7` ("Voice Over (ligpit, pagkain, tapon, and kinig)") re-saved all five Parke
scenes from their builder scripts and overwrote their objective lists:

| Scene | Paper says, and the build had until `08d16e3` | Build now, at `0a3d6c7` |
|---|---|---|
| 11 Parke | Lakad, Takbo, Tingin, Tuwa, Pasok (5) | Takbo, Pasok **(2)** |
| 12 Palaruan | Akyat, Dausdos, Sakay, Tulak, **Talon** (5) | Akyat, Dausdos, Sakay, **Padyak** (4) |
| 13 Kaibigan | **Kumusta**, Ngiti, Tanong, **Laro**, Bati (5) | Bati, Ngiti, Tanong, **Sali**, Paalam (5) |
| 14 Piknik | Upo, Kain, **Ibahagi**, Inom, Salamat (5) | Upo, Kain, **Alok**, Inom, Salamat (5) |
| 15 Uwi | Paalam, Lakad, Tingin, **Pagod**, Uwi (5) | Paalam, **Apir**, Uwi **(3)** |

- Parke drops from **25 objectives to 19**. The whole build is now **66 objectives, 47 distinct
  words, 19 re-asks** instead of the 72 / 51 / 21 the paper states.
- Eight words are now in no scene at all: Ibahagi, Kumusta, Laro, Pagod, **Talon**, Tingin,
  Tulak, Tuwa.
- The four that replaced them — Padyak, Sali, Alok, Apir — have **no `word_*.wav` recording**.
  `Tools/completeness_report.py` lists all four under "taught by caption rather than voice".
  A commit whose purpose was voice-over coverage has made Parke's coverage worse.

**Scope note.** The pre-final defense requires 60% of the system, and the adviser's required
finished parts are Home and School. Those are **10 of 15 scenes (67%) and 47 objectives**, and
they match the paper exactly — so Parke is not needed for the demo. Parke still matters for the
paper only, because Chapter 3's stated counts include it. See §5.

What it breaks in the paper if the build is left as it is:

- §3.2.2's general jump command, which says Talon can fire "outside its own objective"; Talon
  no longer has an objective anywhere. The *behaviour* is intact — all fifteen scenes carry a
  `PlayerMovement` component, so Talon still makes Kylo jump in any scene, and §1.1's worked
  example is demonstrable in Home and School. Only the phrase "its own objective" is now wrong
- §3.2.3 — "In Kaibigan, *Kumusta* opens an interaction with children at the park" and
  "*Ibahagi* and *Salamat* connect the park's vocabulary to sharing and courtesy"
- §3.2.4's Palaruan row, and the long-word-exception example, which is Ibahagi
- Every total in §3.2.3, §3.2.4 and §3.6

**The paper's table is the validated list — confirmed against the signed validation.**
`Docs/Vocabulary_Validation_Addendum.html` (Addendum A, submitted after the form was signed on
12 November by Ms. Princess Diana Celebre) presents nine words to the evaluator as words **the
built game asks for**, and seven of them are the Parke words this commit removed: Tingín
(Parke 1 and 5), Tuwâ (Parke 1), Tulák (Parke 2), Kumustá (Parke 3), Larô (Parke 3), Ibahági
(Parke 4) and Pagód (Parke 5). Addendum A Part 2 also discusses **Talón** as a word "the game
currently shows to the child". Addendum A, the paper's §3.2.4 table, and the scene files as of
`08d16e3` describe one and the same word list. The build is the only thing that moved.

**Why it happened.** The builder scripts were stale relative to the scene files, and re-running
them clobbered the scenes. Three commits on 9–12 November restructured the builders to a shorter
list — `8c24497`, `3a09702` and `dfc9aad` — but that restructure was superseded: the scene files
were kept on the fuller list, and Addendum A was then written from the scenes. Nobody brought the
builders back. `BuildParkeScene2.cs` still showed the seam: its header said *"Teaches five words:
Akyat, Dausdos, Sakay, Tulak, Talon"* and line 39 still explained Talon's physics, while the code
below built four objectives ending in Padyak.

**Fixed on `claude/salinlahi-chapters-revision-audit-1eab98`.** All five builders are restored to
their `5f96c0e` state, which is the list above. For scenes 1, 2, 4 and 5 the restructure commits
were reverted; for scene 3 the only later commit (`7e047c2`) turned out to touch nothing but
comments and the log line, so that file was taken back wholesale. Verified after the change:

| Builder | Objectives now declared |
|---|---|
| Parke 1 | Lakad, Takbo, Tingin, Tuwa, Pasok |
| Palaruan | Akyat, Dausdos, Sakay, Tulak, Talon |
| Kaibigan | Kumusta, Ngiti, Tanong, Laro, Bati |
| Piknik | Upo, Kain, Ibahagi, Inom, Salamat |
| Uwi | Paalam, Lakad, Tingin, Pagod, Uwi |

No reference to Padyak, Sali, Alok or Apir remains in any Parke builder. Every sprite the restored
code loads resolves on disk, `celebrate_uniform_with_bag` for the Tuwa pose included, and every
method it wires up (`PlayerMovement.Jump/Takbo/WalkForward`, `TimedOverlay.Play`,
`PoseSwitch.Apply`) still exists. `Assets/Scripts/*.cs` and `Assets/Editor/*.cs` compile together
clean under `-warnaserror+`.

**Still to do, and only you can do it:** merge this branch into the main checkout, run
**Tools > SALINLAHI > Build Parke Scene 1** through **5** from the Unity menu, and do
**Assets > Refresh** first so the builders do not fall back silently again. The scene files are
not changed by this branch — only the builders that generate them.

**Why the fix went into the builders, not the scenes.** Restoring the scene files from `08d16e3`
would have been wrong: `0a3d6c7` rebuilt those scenes wholesale (3,812 changed lines in Kaibigan
alone) and carried in the new summary panel — the TOTAL ATTEMPTS plate replacing `Star_3`, the
resized figures, the new anchored positions. Checking the scenes out would have thrown that away
along with the words. Fixing the builders and re-running them keeps both.

After the rebuild, verify with:

```bash
cd /Users/sugardaquis/Documents/SALINLAHI && python3 -I Tools/completeness_report.py --markdown
```

If instead these four new words were a deliberate vocabulary change after the 10 November
validation, then the paper's §3.2.4 table and all three totals have to be rebuilt around them —
but the half-edited builder comments say this was not deliberate.

### P2 — §3.2.1 and "State of the Build" contradict each other

§3.2.1 says the targets with no recording of the word itself are **Kinig, Ligpit, Pagkain and
Tapon**. "State of the Build" says **Kinig, Hugas, Tapon and Ligpit**.

**§3.2.1 is the correct one.** Hugas is in `VoiceOverScript.csv` but in no scene —
`completeness_report.py` lists it under "In the script but in no scene", and no scene file
contains it. Pagkain is objective 2 of Almusal.

**Fix:** in "State of the Build", change *Hugas* to *Pagkain*.

### P3 — "Linis has assets but no playable objective" is false

Linis is objective 5 of Almusal — it is in `Chapter1_Level5_Almusal.unity` and in your own
§3.2.4 table.

**Fix:** delete that sentence from "State of the Build".

### P4 — Figure 3.4 is cited but is not in the document

§3.5.1 ends: "The components themselves, and the two network paths between the device and the
online database, are shown in Figure 3.4." There is no Figure 3.4 in the Doc — only 3.1, 3.2a,
3.2b and 3.3 are inserted.

The image already exists: `Docs/Figures/Figure_SystemArchitecture.png` (and `.svg`), from commit
`2a49a5f`.

**Fix:** insert it in §3.4 above §3.4.1, inline, captioned *Figure 3.4 System Architecture and
the Two Network Paths* — or cut the clause. It is the figure that answers "how do the two network
paths differ", so inserting it is the better half-hour.

### P5 — "Supabase" is never named anywhere in the paper

The service appears only as "the online database", "the `player_scores` REST path" and "a separate
endpoint". `Paper_Chapter3_Confirmations.md` A3 answered this and the name was lost when §3.4 was
rewritten. Likewise Row Level Security and the Edge Function are described by what they do but
never named.

**Fix:** in §3.4.2, "Online database" — name it once: *"Supabase, reached over its PostgREST
interface."* In §3.6 "Leaderboard", name Row Level Security and the Edge Function alongside the
behaviour you already describe. These are the words a panel will use in the question; the paper
should use them first.

### P6 — "State of the Build" is formatted as a heading

Both the heading and the entire body paragraph beneath it are Heading 2. The paragraph will be
pulled into the table of contents as a wall of text, and the heading has no section number while
every other heading in Chapter 3 has one.

**Fix:** set the paragraph to Normal text, and number the heading — 3.4.3 State of the Build —
or fold it into the end of §3.4.2.

### P7 — Four missing spaces where text was pasted together

- §3.2.2 — "…is supported.**F**igure 3.1 shows how the game prompts…"
- §3.2.4 — "…and 51 distinct words.**D**eliberate repetition."
- §3.2.4 — "…a familiar target in a different scene.**P**rogression and gating."
- §3.2.4 — "…the summary panel's CONTINUE button leads on.**D**ifficulty curve."

In each case a bold lead-in has been welded onto the end of the previous sentence. Add the space
and the paragraph break.

---

## 3. Needs verification — only you can close these

| Item | Where | What is needed |
|---|---|---|
| **Minimum RAM** | §3.4.1 | Still absent. Free storage is answered (500 MB — your own estimate, and still highlighted), but RAM is not stated at all. The `[CONFIRM:]` tag was removed without the number being supplied. Read it off the demo phone and round up |
| **Appendix E device table** | — | The tag is gone, but there is no device table and Appendix E is never referenced. Confirm whether your template requires it; if it does, it is three rows (development computers and test devices) |
| **Appendices A, B, F exist** | §3.3, §3.5.2, §3.6 | Your own highlights flag these. Appendix F is the screenshot set whose re-shoot is deliberately deferred — make sure the Doc does not promise what is not attached at submission |
| **Chapter 4 agreement** | Chapter 4 | Not in this Doc, so nothing can be checked. It must evaluate functionality, usability and usefulness only — no learning-gain claim, since §1.5, §2.4 and §3.1 all argue against one via Muñoz et al. — and the instrument must not ask about anything the build lacks: no pre/post test, no teacher dashboard, no class roster |
| **Figure widths** | §3.2, §3.3 | Figures 3.2a, 3.2b and 3.3 are 484.56 pt; Figure 3.1 is 438.81 pt, so its edges do not line up with the others. Cosmetic only — the wrap-text problem is fixed |

---

## 4. Priority

1. **P1 — reconcile Parke.** Not for the demo: Home and School already clear the 60%
   requirement at 67%. It matters because Chapter 3's counts include Parke, so one of the two
   routes in §5 has to be taken.
2. **P2 and P3** — two factual self-contradictions inside Chapter 3, five minutes each.
3. **P4 and P5** — the missing figure and the unnamed database. Both are direct panel questions.
4. **P6 and P7** — formatting; fifteen minutes, and the heading one is visible in the table of
   contents.
5. **Minimum RAM**, and the Appendix E decision.
6. **Chapter 4**, against the constraints in §3.

Nothing in Chapters 1 and 2 needs further work. Chapter 3's prose is consistent with the build
everywhere except the five Parke rows, and those are the build's fault.

---

## 5. Parke, given that the defense only requires 60%

**For the demo: no, Parke is not needed.** Home and School are 10 of 15 scenes (67%) and 47
objectives, and both environments match the paper's §3.2.4 table word for word. They clear the
adviser's requirement on their own, and Talon is demonstrable in any of them.

**For the paper: Parke still has to be settled**, because Chapter 3 states counts that include
it and those counts are now false against the build. Two legitimate routes:

**Route A — fix the build, change nothing in the paper.** Put the missing objectives back in the
five builder scripts and re-run them. Chapter 3 then needs no edits at all, you keep the
15-scene / three-environment claim, and you are over-delivering against 60%.

**Route B — leave Parke reduced, restate the paper as built.** Honest, and consistent with how
the paper already handles Palengke and Bahay scene 6. But it is the larger job, because these
all have to change together:

| Where | Currently says | Would become |
|---|---|---|
| §3.2.4 table | 5 Parke rows | the built rows (2 / 4 / 5 / 5 / 3) |
| §3.2.4, §3.6 totals | 72 objectives, 51 distinct words, 21 re-asks | **66 / 47 / 19** |
| §3.2.3 environment table | Parke 25 | **Parke 19** |
| §3.2.4 repetition bullets | Lakad in scenes 1, 2, 6, 11, 15; Bati in 6, 7, 13; 13 repeated targets; 10 cross-environment; Tingin repeats within Parke | all recomputed — Parke now has **1** re-ask |
| §3.2.3 worldbuilding | "*Kumusta* opens an interaction"; "*Ibahagi* and *Salamat* connect the park's vocabulary" | both words are gone from the build |
| §3.2.4 difficulty table | Environment 3 example "Ibahagi, Salamat" | Ibahagi is gone |
| §3.2.4 long-word example | Ibahagi | pick a built 7+ character word |
| §3.2.2 general jump command | Talon "outside its own objective" | Talon has no objective |
| State of the Build | — | say Parke is partially built, as Palengke is handled |

**Route A was taken** (see P1). The builders are fixed and compile clean; the paper needs no
vocabulary edits, and once the Parke scenes are rebuilt in Unity the build, the paper's §3.2.4
table and Addendum A all state the same list again. Route B is recorded here only so the choice
is on the record.

**One separate mismatch Route A does not touch.** Addendum A also lists **Hugás** and **Labás**
in Paaralan Scene 4 (Recess), and neither the paper's table nor any scene file has them —
Recess is Ligpit, Kuha, Kain, Inom, Tapon in both. That gap predates `0a3d6c7` and was not caused
by it. Decide with your evaluator whether those two words are being added to Recess or withdrawn
from the addendum, because Addendum A is the document she signs.
