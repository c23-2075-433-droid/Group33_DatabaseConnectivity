# The validation list vs. the game as built

Checked the list on the validation form against all fifteen `.unity` scene files, their builders,
and `Docs/VoiceOverScript.csv`. Code at `4c0ac41`.

**The form is not the word list the game currently has.** Approving it as written commits the group
to editing 10 of the 15 scenes, building a sixteenth, recording roughly 40 new audio lines, and
changing the vocabulary numbers in Chapter 3 for a third time. That may well be the right call —
but it should be a decision, not a surprise after the teacher has signed.

---

## 1. Scene by scene

| Scene | Build | Form | Change |
|---|---|---|---|
| 1 Umaga Na | 3 | 4 | **+ unat** |
| 2 Banyo | 9 | 5 | **− tabo, sabon, sipilyo, tuwalya** |
| 3 Maligo | 3 | 5 | **+ tuwalya, sabon, sipilyo** · **− kaliwa** |
| 4 Damit | 5 | 4 | **− kunin** |
| 5 Almusal | 5 | 5 | unchanged |
| *(new)* Leaving for School | — | 5 | **new scene:** alis, bukas, paalam, lakad, **tara** |
| 6 Paaralan | 5 | 5 | **+ bati** · **− kumusta** |
| 7 Klasrum | 4 | 5 | **+ bati** |
| 8 Aralin | 5 | 4 | **− tanong** |
| 9 Recess | 5 | 4 | **− tawa** |
| 10 Uwian | 5 | 5 | unchanged |
| 11 Parke | 5 | 4 | **− tuwa** |
| 12 Palaruan | 5 | 5 | unchanged |
| 13 Kaibigan | 5 | 5 | **+ paalam** · **− kumusta** |
| 14 Piknik | 5 | 5 | unchanged |
| 15 Uwi | 5 | 5 | unchanged |

**Five words leave the game:** kumusta, kunin, tabo, tawa, tuwa.
**Two words enter:** unat, tara.
**Three words move** from Banyo to Maligo: sabon, sipilyo, tuwalya.

**New totals**

|  | now | form, 15 scenes | form, 16 scenes |
|---|---|---|---|
| objectives | 74 | **70** | **75** |
| distinct words | 53 | **49** | **50** |
| re-asked words | 13 | **12** | 12 |
| repeat objectives | 21 | **21** | 25 |

All the English meanings on the form match the glosses already in `VoiceOverScript.csv`. Only
*unat* and *tara* have no gloss, because they are new.

---

## 2. Two words I would defend before she signs

**Tabo.** This is the one I would fight for. It is the most culturally specific word in the whole
game, it is already fully recorded, and section 3.2.3 of the paper rests its *worldbuilding*
argument on it: *"The bathroom scene teaches Tabo, the dipper, which has no single English
equivalent and is immediately recognisable to a Filipino child."* Drop tabo and the paper loses its
single best piece of evidence that SALINLAHI is Filipino rather than translated — a panel will ask
what makes the game culturally rooted, and tabo is the answer. It is also a concrete noun sitting
in a bathroom, which is exactly the kind of word the game teaches best. Worth asking the evaluator
whether it can stay in the bath scene with the other objects.

**Kumusta → bati.** These are not interchangeable, and the difference matters because of how the
game works. *Kumusta* is the greeting itself — the thing a child actually says out loud to Teacher
Rachel. *Bati* is the act of greeting — a label for the action. The mechanic is *say the word to
make it happen*, so asking a child to say "bati" to a teacher is asking them to say the name of
what they are doing instead of doing it. *Kumusta* is the word that works as speech.

My guess is the evaluator chose *bati* as the dictionary headword, which is reasonable in a
word-list sense but not in a spoken-game sense. Worth showing her the scene: Kylo walks up to
Teacher Rachel, and the child says the word. Which word should it be? If the answer is still
*bati*, fine — but the paper's 3.2.3 sentences about Kumusta then need rewriting.

**Related: Opo is in the paper but has never been in the game.** 3.2.2's matching examples are all
built on *Opo* ("opo po", "oho"), and 3.2.3 says Rachel is "the reason Kumusta and Opo have someone
to be said to". Opo is not among the 53 words and never was. Those two passages need fixing
whatever the evaluator decides — either add *opo* to a school scene, which would be an easy and
natural addition to this list, or rewrite the examples around a word that is really there.

---

## 3. What approval would cost in production

**Audio that does not exist yet** (checked against `Assets/Audio/VO`):

- *unat* and *tara* — no word clip, no teach line, no recall line, nothing. Both need recording.
- Rachel's three lines (story, ask, done) are missing for eleven scene-and-word pairs: **bati** in
  scenes 6 and 7, **paalam** in scene 13, **tuwalya / sabon / sipilyo** in Maligo (their lines are
  recorded as `rachel_l1_s2_*`, so moving the words means re-recording them as `l1_s3_*`), and all
  five words of the new Bahay scene 6.

That is roughly **33 new Rachel lines, 2 new word clips and 4 new teach/recall lines**, all of
which have to go through `Docs/VoiceOverScript.csv` first.

**Audio that becomes dead:** 12 files for kumusta, 6 for tabo, plus tawa, tuwa and kunin.

**Code:** scene files and builders for Umaga Na, Banyo, Maligo, Damit, Paaralan, Klasrum, Aralin,
Recess, Parke and Kaibigan — 10 of 15. Plus a whole new scene.

**The sixteenth scene is the big one.** The form includes *Leaving for School*, which is designed
but not built; it is the locked tile already sitting on the journey map. Approving the form commits
the group to building it, and with it Bahay becomes six scenes while the other two levels stay at
five. If there is no time to build it, take that scene off the form before it is signed, rather
than having a validated word list the game does not deliver.

---

## 4. What it does to Chapter 3

Beyond the numbers in section 1, three passages would need rewriting:

- **3.2.4, the scene-length rationale.** *"Umaga Na (3 words) and Maligo (3 words) are short because
  they are transitions… Banyo (9 words) is the longest scene in the game"* — none of that stays
  true. Under the new list Banyo is 5, Maligo is 5, Umaga Na is 4, and six scenes sit at four
  objectives. The paragraph becomes simpler: five objectives per scene, four in six of them.
- **3.2.3, worldbuilding.** The tabo sentence goes if tabo goes.
- **3.2.3, characters and narrative.** The two Kumusta sentences, and the Opo claim.

---

## 5. Two notes on the form itself

**Diacritics.** The form marks stress throughout — *bángon*, *tayô*, *malígò*. The game does not:
the on-screen label is `BANGON` and the files are `word_bangon.wav`, which follows ordinary Filipino
orthography. Worth a line on the form saying the marks are a pronunciation guide for the evaluator
and are not shown to the child, otherwise she may reasonably expect to see them in the game.

**What she is being asked to validate.** The form says the evaluator should review "accuracy,
spelling, meaning, pronunciation, and contextual appropriateness". That is a language judgement.
Which words go in which scene is a design judgement, and the list as written changes that heavily.
If the group wants her signature to mean "this Filipino is correct for Grade 2 and 3", the cleanest
thing is to validate the list the game actually teaches, and treat any word swaps she suggests as
recommendations to act on afterwards — which is also what "Approved with Revisions" is for.
