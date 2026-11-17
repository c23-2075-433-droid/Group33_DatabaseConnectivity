# Chapters 1–3 vs. the Build — Consistency Review

Source compared: **`IT33 - Chapter 3 Draft.pdf`** (34 pages, Chapters 1–3 plus
references), against the repository at `94c79a8`.

**If this PDF is an older export than the live Google Doc, re-check §2 against
the Doc before editing** — `Docs/Paper_Open_Revisions.md` records that earlier
revisions were applied directly to the Doc. Everything below is what the PDF
says today.

**Verdict:** Chapters 1 and 2 are in good shape and are consistent with the
build. **Chapter 3 §3.2.2, §3.2.4 and §3.6 describe an earlier version of the
game** — a different word list and, more seriously, a matching algorithm that
was deliberately removed. These are the findings a panelist can confirm in
thirty seconds by playing one scene, so they are the highest-value fixes of the
week.

---

## 1. What is already consistent — say these with confidence

| Paper | Build |
|---|---|
| Speaking is the control; no tap target for vocabulary | ✅ exactly |
| Four-stage loop: prompt → listen → match → act | ✅ `SceneObjectiveController` |
| Android 8.0 (API 26), ARM64 | ✅ `AndroidMinSdkVersion: 26`, ARM64 |
| Unity 6000.6.2f1 | ✅ `ProjectVersion.txt` |
| `fil-PH` locale; restarts 0.5 s after each result | ✅ `preferredLanguage`, `restartDelay = 0.5f` |
| Plugin pinned to a single commit | ✅ `27a99b18…` in `packages-lock.json` |
| No desktop/WebGL implementation → Android only | ✅ and it is why |
| Editor keyboard fallback, Enter = a correct utterance | ✅ `#if UNITY_EDITOR`, via `SimulateSpeech` |
| Pacing: wait for the action, 4 s cap, 0.25 s delay | ✅ `maxActionWait`, `settlePause` |
| Sequence held during a fade so nothing is answered behind it | ✅ `SetPaused` + `completionPending` |
| Talon always live | ✅ checked independently of the objective |
| No penalty, no lives, no timer | ✅ |
| Progress saved per scene; an adult can stop anywhere | ✅ `MarkSceneComplete` |
| 15 scenes, 3 environments, 5 scenes each | ✅ |
| 53 distinct words | ✅ (but see §2.1 — a *different* 53) |
| Record written at the end of a level's fifth scene | ✅ `SessionScoreTracker` in scenes 5, 10, 15 only |
| Seven fields in the database record | ✅ `ScoreRecord` |
| Supabase over REST | ✅ |
| Art by Python scripts (Pillow/NumPy/SciPy); frame-by-frame sprite swapping | ✅ `Tools/`, `SpriteSwap`, `PoseSequence` |
| Rachel's English narration is ElevenLabs TTS; Filipino word clips recorded by a group member because Tagalog distinguishes by stress | ✅ and it is a strong paragraph — keep it |
| Folder/naming conventions | ✅ `Assets/Scripts`, `Assets/Scenes`, `Assets/Sprites`, `Assets/Audio/VO`, `Assets/Resources` |

---

## 2. Inconsistencies, in priority order

### 2.1 ⚠⚠ The scene word table (§3.2.4) is an older word list

This is the one a panelist can disprove by playing one scene.

| # | Scene | Paper says | **Actually built** |
|---|---|---|---|
| 1 | UmagaNa | Bangon, Tayo, Lakad (3) | ✅ same |
| 2 | Banyo | Lakad, Kaliwa, Kanan, Bukas, Ilaw, **Tabo, Sabon, Sipilyo, Tuwalya** (9) | Lakad, Kaliwa, Kanan, Bukas, Ilaw **(5)** |
| 3 | Maligo | **Kaliwa**, Maligo, **Suklay** (3) | **Tuwalya, Sabon, Maligo, Sipilyo, Suklay (5)** |
| 4 | Damit | Damit, Medyas, Sapatos, Bag, Kunin (5) | ✅ same |
| 5 | Almusal | Upo, **Kanin, Itlog**, Kain, Inom | Upo, **Pagkain**, Kain, Inom, **Linis** |
| 6 | Paaralan | Kumusta, Pasok, Takbo (3) | **Lakad, Bukas, Bati, Pasok, Takbo (5)** |
| 7 | Klasrum | **Bukas, Kumusta**, Upo, Kuha, Lapis | **Bati**, Upo, **Kinig**, Kuha, Lapis |
| 8 | Aralin | Aklat, Basa, Sulat, **Tanong**, Sagot (5) | Aklat, Basa, Sulat, Sagot **(4)** |
| 9 | Recess | Labas, Kain, **Laro, Tawa, Tulong** | **Hugas**, Labas, Kain, **Inom, Tapon** |
| 10 | Uwian | **Pasok**, Upo, **Linis, Bag**, Alis | **Ligpit, Tayo, Kunin, Paalam**, Alis |
| 11–15 | Parke scenes | — | ✅ all five match |

**Consequences that must be fixed with it:**

- **Totals.** Paper: "73 objectives … 20 re-ask a word". `Docs/Paper_Open_Revisions.md`
  says 74 / 21. **The build is 72 objectives, 53 distinct words, 19 re-asks.**
- **Tabo.** The paper teaches it in Banyo and calls it out in §3.2.3 worldbuilding
  as "the dipper, which has no single English equivalent". **Tabo was dropped as
  a word.** `item_tabo.png` is no longer placed in any scene. The worldbuilding
  bullet needs replacing — the banig and the public-school classroom are still
  true and are good examples.
- **The "five objectives per scene" discussion in §3.2.4** names Banyo as "the
  longest scene in the game" at 9 words and Maligo as 3. Both are now 5. The
  exceptions are now **UmagaNa (3)** and **Aralin (4)**, and there is no
  nine-word scene.
- **§3.2.3's words-per-chapter table** (Bahay 25, Paaralan 23, Parke 25) is
  derived from the old list and needs recomputing.

**Recompute all of this with one command rather than by hand:**
`python3 Tools/completeness_report.py --markdown`

### 2.2 ⚠⚠ The matching rule (§3.2.2 and §3.4.2) describes the algorithm that was removed

The paper says:

> "The checker applies four tests in order … Normalised whole word containment ·
> Accepted variants · **Edit distance on the whole phrase** · **Edit distance on
> each spoken token**. Tolerance scales with word length: words of 3 letters or
> fewer must match exactly, 4 to 6 allow 1 edit, 7 or more allow 2."

**That is the old algorithm, and it was deliberately replaced** — precisely
because scaled edit distance accepted "kaha" for **Kuha** and "kanin" for
**Kain**. The current `PronunciationChecker`:

1. **canonicalizes both sides** — strip accents and punctuation; map Spanish-era
   spellings (`qu→k`, `ll→ly`, `x→ks`, `c→k z→s f→p v→b j→h`); fold `e→i` and
   `o→u`; fold the `uw/w` glide; collapse doubled letters
2. drops 21 filler words
3. then requires an **exact** match of the whole phrase, any single content
   token (behind a fragment guard), or everything concatenated
4. plus **one** curated layer, `WordVariants`
5. plus **one** narrow fuzzy allowance: on canonical forms of **7+ letters**, one
   consonant may be garbled **only if every vowel still lines up**
   (`IsConsonantSlip` = Levenshtein ≤ 1 **and** identical vowel skeleton)

The property that makes this safe is that 53 taught words fold to **53 distinct
canonical forms**, re-proved by an editor tool over all cross pairs.

**Why this is the most dangerous inconsistency:** if a panelist reads §3.2.2 and
asks "so a one-letter error passes on a four-letter word?", the paper says yes
and the code says no. Answering correctly then contradicts your own paper.

Also wrong in the same passage: the worked examples use **Opo** ("opo po" for
Opo, "oho" for Opo). **Opo is not a taught word.** The taught word is **Upo**
(to sit). Replace the examples with real ones from `WordVariants`: "oppo" for
**Upo**, "i love" for **Ilaw**, "lines" for **Linis**.

**§3.4.2 repeats the same four tests** and must be corrected with §3.2.2.

### 2.3 ⚠ The leaderboard and its security are absent from Chapter 3

§3.4 describes "the data part" as one online database that receives a record and
is read back. That was true at Activity 5. The build now has a **second, separate
path**: a `leaderboard` table, a `leaderboard_owner` table, a `leaderboard_rank`
SQL function, and a Deno Edge Function `submit-score` that issues and checks a
per-player token.

This is a substantial piece of implemented work and the paper does not mention
it. Worse, it is the part that answers the obvious panel question "what stops a
child cheating the leaderboard?". **Add a subsection to §3.4** — half a page:
the two tables, Row Level Security, the Edge Function as sole writer, the token,
and the four values that leave the device.

### 2.4 ⚠ Teacher Rachel's role is described wrongly

§3.2.3 lists her among the characters: "Teacher Rachel, who appears in the
classroom scenes, and who is the reason Kumusta and Opo have someone to be said
to."

In the build Rachel is **the narrator of the whole game** — her portrait sits in
the branch-ring plaque in every scene's HUD, she has 423 recorded clips, and
`RachelPortraitAnimator` lip-syncs nine mouth shapes against 423 viseme tracks.
That is a bigger and better-implemented feature than "a character in the
classroom", and the paper undersells it. (Also: "Opo" again — the taught word in
Klasrum is **Bati**.)

### 2.5 §3.5.1 says the record is written at the end of every scene

> "When the last objective in a scene is answered … the result is written to the
> online database."

It is written at the end of a **level** — `SessionScoreTracker` is present only
in Chapter1_Level5, Chapter2_Level5 and Chapter3_Level5. §3.2.2 already says this
correctly ("The record for a whole level is written to the database at the end of
that level's fifth scene"), so §3.5.1 simply contradicts §3.2.2.

The same paragraph says "Progress is saved per **level**, so the child can stop
after any level" — elsewhere the paper correctly says per **scene**. Per scene
is right.

### 2.6 §3.5.1 says a map node unlocks when the previous *level* is completed

> "A node unlocks when the previous level is completed."

The build unlocks **per scene** (`JourneyMap.Steps`, `LevelProgress.HighestUnlockedStep`),
which is the better design and is what §3.2.4 says. Fix §3.5.1.

### 2.7 Chapter 2's conceptual framework still lists a post-test

The IPO model has "optional pre-test" under INPUT and "Post-test results showing
vocabulary gain" under OUTPUT. Chapter 1 §1.4 Objective 5, Chapter 2 §2.4 and
Chapter 3 all scope the evaluation to **functionality, usability and usefulness**
and explicitly argue against a learning-gain claim (citing Muñoz et al. 2025).
The framework diagram contradicts that. **Remove the pre/post-test from the IPO
model**, or add a sentence saying it was considered and dropped, and why.

This also matters for Chapter 4 — item 4 in `Docs/Paper_Open_Revisions.md`.

### 2.8 Target-age statements disagree with each other

- Chapter 1 §1.1, §1.5, §1.7: Kindergarten to Grade 3
- Chapter 3 §3.2.1: "children roughly 4 to 7 years old, preschool through Grade 1"
- `Docs/Paper_Open_Revisions.md`: "Kylo is eight, matching the Grades 2–3 players"

Pick one and make the three agree. Kindergarten–Grade 3 (≈5–9) is the one the
rest of the paper, the title and the ethics of the pilot all assume; Chapter 3's
"4 to 7, preschool through Grade 1" is the odd one out.

### 2.9 Two `[CONFIRM:]` tags are still in §3.4.1

- `[CONFIRM: minimum RAM and free storage from device testing]`
- `[CONFIRM: Table of development computers and test devices, Appendix E]`

**Both are yours to answer and both are easy.** On the demo phone:
Settings → Apps → SALINLAHI gives the installed size; the APK size is in
`Build/Release/`. Round up and state a minimum. The device table is three rows.
A `[CONFIRM:]` tag visible to a panel reads as an unfinished paper.

### 2.10 Chapter 3 does not state the known limitation

The paper describes a working forgiving matcher. It does not say that **on a
device a mispronounced word is still accepted**, which your own v0.1.0 release
notes record as the most serious issue.

**Put it in the paper.** One paragraph in §3.2.2 or a limitations note, framed
with Bhardwaj et al. (2022) and Santiago et al. (2025), which Chapter 2 already
cites for exactly this. A limitation you stated is a finding; the same limitation
found by a panelist in your demo is a hole.

---

## 3. Smaller items

| Where | Issue |
|---|---|
| §3.2.1 | "iOS reachable from the same codebase" — true, but no iOS build exists. Add "not yet produced". |
| §3.2.4 | "the summary panel's **Next** button leads on" — true. But the paper does not mention that **every** scene has a summary panel while only every fifth writes a record. Worth one clause. |
| §3.6 | "15 scenes, 73 objectives, 53 distinct words, 20 re-asks" → **72 / 53 / 19**. |
| §3.6 | Player profile described as nickname + furthest step + stars. The build also stores a UUID `playerId`, `completedScenes`, `totalPoints`, `wordsLearned`, and leaderboard sync bookkeeping, in **`players.json`**, not PlayerPrefs. Update it — the UUID in particular, because it is what the leaderboard keys on. |
| §3.6 | "A voice line is named … `word_bangon` for the word itself" — correct; worth adding that 423 clips exist and 401 of 436 script lines are voiced. |
| §3.3 | Says interface text was "translated into English". Fine, but the reason deserves a clause: the learners are English-dominant, so the *instruction* is in the language they have and the *word* is the one they must produce. |
| Ch. 2 | "aBhardwaj et al. (2022)" on page 3 — stray leading "a". Typo. |
| §3.2.4 | "Umaga Na (3 words) and **Maligo** (3 words) are short" — Maligo is now 5. The second short scene is **Aralin (4)**. |

---

## 4. The README and release notes also disagree with the build

Not the panel's problem directly, but they are in the repository you may be asked
to show, and three different numbers is worse than one wrong number.

| Document | Says | Should say |
|---|---|---|
| `README.md` | "49 distinct Filipino words … 66 spoken objectives" | **53 words, 72 objectives** |
| `README.md` known limitations | "Teacher Rachel's portrait is unassigned" | **Now assigned** — `UITheme.asset` has `rachelPortrait` set |
| `Docs/release/v0.1.0/RELEASE_NOTES.txt` | "54 distinct Filipino words … 74 spoken objectives" | 53 / 72 — or leave the release notes alone as a historical record of v0.1.0 and say so |
| `Docs/Paper_Open_Revisions.md` | item 5, bundle id still `com.DefaultCompany.SALINLAHI` | **Already fixed** — `ph.edu.uphsl.group33.salinlahi` |
| `PronunciationChecker.cs` docstring | "51 taught words", "51×50 cross pairs" | **53**, 53×52 |
| `PlayerMovement.cs` docstring | "Simple 2D platformer movement. Move with A/D" | it is the voice-driven character controller |

---

## 5. The edit list, in the order to do it

Roughly four hours of work. Items 1–4 are the ones that change what a panelist
can catch.

1. **§3.2.4 scene table + all three totals (72 / 53 / 19)** — regenerate from
   `Tools/completeness_report.py`, then fix the derived sentences in §3.2.3,
   §3.2.4 and §3.6.
2. **§3.2.2 and §3.4.2 matching rule** — replace the four edit-distance tests
   with canonicalization + exact match + variants + the 7-letter consonant slip.
   Replace the Opo examples.
3. **§3.2.2 or a limitations note** — add the device-level recognizer finding,
   cited to Bhardwaj and Santiago.
4. **§3.4** — add the leaderboard, the Edge Function, the token and RLS.
5. **§3.5.1** — record written per *level*, not per scene; nodes unlock per
   *scene*, not per level; progress saved per scene.
6. **§3.2.3** — Rachel is the narrator of the whole game; drop Tabo and Opo from
   worldbuilding; keep the banig and the classroom.
7. **Chapter 2 IPO** — remove the pre/post-test, or justify dropping it.
8. **Age range** — make §3.2.1 agree with Chapters 1 and 2.
9. **The two `[CONFIRM:]` tags** — measure on the phone, write the device table.
10. **README, and the two stale docstrings** — ten minutes, and they are in the
    repository a panelist may be shown.
