# Pronunciation and stress checking: why it was inconsistent, and how it works now

This note covers the voice answer check: the game asks for a word such as
**Tayô** ("stand up"), and the child has to say it with Teacher Rachel's stress,
not as **táyo** ("we"). It explains why the old check accepted wrong stress some
of the time, what replaced it, how it was tested, and what it still cannot do.

## 1. Why the old check was inconsistent

The device speech recognizer only returns letters, and *tayô* and *táyo* are the
same letters. So every stress verdict depends on measuring the sound. Six things
made that measurement unreliable:

1. **Single cut-off, no margin.** Each word had one line. An answer near it
   landed on either side by chance, so two near-identical tries got opposite
   verdicts. This was the main cause of "sometimes accepted, sometimes not".
2. **Syllables found by guessing.** `RawStressAnalyzer` split the word at its
   deepest loudness dip. On Rachel's own studio recordings that cut Kumusta's
   last syllable down to 30 ms at −38 dB and gave Sulat a 30 ms second vowel. It
   also changed its choice of dip between recordings a few milliseconds apart.
   The Tayo sweep in the old tests showed this: moving the stress 20 % was
   marked WRONG, and moving it 30–40 % was marked ok.
3. **Strict mode acted on references the system itself had marked unreliable.**
   For 22 of 37 words, Rachel's recording did not separate the two stresses on
   loudness readings, and the earlier "strict" commit acted on those
   measurements anyway. That is a coin flip.
4. **On phones there was no audio at all.** `recordOwnVoice` is off, because the
   phone would not share the microphone with the recognizer. Stress was judged
   only from the recognizer's 10–20 loudness readings per second. That is one
   number (where the loudness sits in time), so holding the end of "táyooo"
   moves it toward "tayô".
5. **The threshold was measured against Rachel's very emphatic delivery.** It was
   then lowered to "30 % of the way to her score", a guess rather than a
   calibration.
6. **Calibration used only Rachel's voice**, with the stress moved by one method,
   and the tests checked the same recordings the thresholds were set on.

## 2. Balanced mode, the default since 2026-11-22

Strict mode (below) turned out to be too strict for a learning game. The player
had to reproduce Rachel's stress even on words where stress does not change
the meaning. Bangon, the first word of the game, was refused for a stress
slightly different from hers. Balanced mode (`VoiceCommand.stressCheck =
Balanced`, `AnswerJudge.JudgeBalanced`) ranks the evidence like this:

1. **The word comes first.** The device recognizer hears the sounds, and
   `PronunciationChecker` compares its letters strictly.
   - A changed vowel or a missing or extra consonant makes a different word, so
     *bagon*, *bango*, *bangga*, *bang* and *bangin* are refused for Bangon.
   - Spelling variation that is free in Filipino is forgiven: e/i, o/u
     (*bangun*), accent marks, letter case, and filler like "po" or "ang".
   - No taught word is ever accepted for another.
2. **For an ordinary word that is the whole decision.** This covers every word
   except the 11 pairs. Stress, speed, pitch, intonation and accent never fail
   it. The stress is still measured and written to the attempt log for the
   teacher.
3. **Stress only matters where it changes the word.** The 11 same-spelling
   pairs are tayô/táyo, upô/úpo, bukás/búkas, bása/basâ, talón/tálon,
   bátì/batî, lákad/lakád, pások/pasók, kúha/kuhà, sulát/súlat and línis/linís.
   - A pair is refused only when **both** audio checks agree the stress is on the
     other syllable. With no recording, it is refused only when the loudness
     readings fall beyond anything Rachel's own delivery produced.
   - A borderline or unmeasurable stress passes.
   - A pair said inside a sentence ("kain tayo", let's eat) is still refused,
     because there it is the other word.
4. **The player's voice is never compared with Rachel's.**

**Verified** (`Tools/test_stress_rules.sh balanced`, 32 checks, all pass):

- **Bangon is accepted on all 17 simulated phones and voices** in each of these
  forms:
  - natural;
  - stress moved, made two ways;
  - slightly different stress;
  - different pitch;
  - slower;
  - faster.
- It is also accepted with no audio at all, and as "bangon po".
- **Rachel's own delivery of every word is accepted** on every simulated phone
  and voice: 816 of 816.
- **No ordinary word is refused for its stress:** 0 of 1,394.
- **On the pairs, a clearly moved stress is refused 313 of 374 times.** The rest
  pass because the stress was not clear enough to call it the other word.
  Sulat (16/34) and Linis (18/34) are the weakest.
- A moved stress on a pair was refused on one try and passed on another 2 times.
- **Your labelled recordings:** all 8 correct Bangon takes are accepted. Your
  "wrong stress" Bangon is accepted too, which is now intended, because stress
  does not change Bangon.
- **Wrong sounds:** the Editor's hold-R test always passes the target word as
  the heard letters. A wrong sound can only be tested on a phone, where the
  recognizer hears it.

The strict modes described below stay selectable on `VoiceCommand`:
`StrictOnTrustedWords`, `StrictOnEveryWord` and `Lenient`.

## 2b. Strict mode (no longer the default)

`AnswerJudge` now accepts a word with a known stress only when its stress was
**measured**, the measurement is **clear**, and it is on **Rachel's side**.
Otherwise the child is asked again, without a miss and without scoring. Rachel
then says the word again (`VoicePromptDirector.HandleStressUnclear`).

### Recorded audio: `PronunciationAssessor`

* **The spelling sets the syllables** (`SyllableAligner`). Filipino spelling is
  phonemic, so the word itself says how many vowels it has and what sits
  between them: a glide, a nasal, a stop, or a glottal stop. Dynamic
  programming fits that whole sequence to the audio in one pass. Vowel length is
  "soft": each frame counts by how vowel-like it is, so the exact boundary
  matters little.
* **Only the shape of the word is compared, never the voice.** For the stressed
  syllable against each neighbour, three differences are measured *within the
  child's own utterance*: vowel length (log ratio), loudness (dB) and pitch
  (semitones). Voice height, gender, accent, loudness and distance from the
  phone cancel out.
* **Rachel is the reference.** Each word stores two points from her
  `word_*.wav`: her own delivery (R), and her delivery with the stress moved to
  the neighbouring syllable (M). The answer is placed on the line from M (−1) to
  R (+1).
* **Lines are calibrated per word**, from right and wrong examples under 12
  simulated phones and speakers. The conditions are three noise levels; voices
  raised ×1.25 and ×1.45, the way a child's is; slow and fast speech; and a slow
  child. Correct starts above the highest position any wrong-stress example
  reached. The wrong examples are the stress moved by two different methods, and
  moved only halfway. WrongStress starts below the lowest position Rachel's own
  delivery reached. Between the two lines, the child is asked again.
* **Each answer is measured six times**, at three frame offsets, with and without
  a little noise. A verdict stands only if all six agree.
* **A second, independent judge** (the old `RawStressAnalyzer`, which finds
  syllables differently) must agree for any word whose reference is not
  *trusted*. On trusted words it can veto. A disagreement is never settled by
  picking one judge: it means "ask again".
* **Structure check.** A recording with an extra syllable, or with a syllable
  missing or swallowed, is not judged ("ask again").

### Strict on trusted words only (the default since 2026-11-22)

The rules above were tested on real labelled recordings of the developer's own
voice (an adult man) in the Editor. All 8 correct takes of Bángon were refused:
the syllable finder lost the first syllable each time, which would leave a child
stuck on the first word of the game. So the setting
`VoiceCommand.verifyOnlyTrustedWords` (`AnswerJudge.Settings.verifyOnlyTrustedWords`),
on by default, applies the full strict rules only to the words with a
**trusted** reference. There are 15 at present: Basa, Bati, Bukas, Kaliwa, Kuha,
Lakad, Ligpit, Medyas, Pagod, Sabon, Sagot, Tanong, Tayo, Tuwa and Upo.

On every other word the stress is still measured, and a **clear WrongStress
still fails**. An answer that would only have been "ask again" passes on its
letters, and the attempt log flags it **stress-unverified**. On those words a
wrong stress can pass; on the trusted ones it cannot. Turn the setting off to
make every word strict.

The labelled run after the change: all 8 correct Bángon takes accepted. The
wrong-stress and wrong-sound takes were also accepted, flagged unverified.

Full suite with this default: 174 checks pass, 1 fails (the same 5 of 800
verified false accepts). What it costs, on the held-out set:

- 347 wrong stresses on untrusted words pass on their letters, flagged
  unverified.
- A wrong stress on an untrusted word is not always judged the same way: in 4
  cases (Ilaw, Laro, Tingin, Tuwalya) it was caught on one try and passed on
  another.
- Rachel's own delivery never flips, and nothing said on a trusted word flips.

**A note on testing wrong sounds in the Editor.** Holding R uses the target word
as the "recognized" letters, so a wrong *sound* can only be caught there by the
audio check. On a phone, the speech recognizer hears the sounds and catches it.

### Loudness readings only (phones): `StressAnalyzer.JudgeBanded`

Correct only beyond every reading the moved stress gave on any simulated phone.
WrongStress only beyond every reading Rachel's own gave. In between, ask again.

## 3. How it was tested

Run `Tools/test_stress_rules.sh`. It needs no Unity and compiles the game's own
files. The new guarantees are tested on **held-out** phones, speakers and wrong
stresses, none of which were used to set the lines:

* a wrong stress is never accepted, for any word, phone or speaker;
* Rachel's own delivery is never marked wrong (at worst asked again);
* the same recording always gets the same verdict;
* the same delivery in different room noise is never Correct on one try and
  WrongStress on another;
* words in sentences, filler ("tayo po"), wrong letters ("tayong", "kayo"),
  silence and no evidence are never Correct.

**Results on 2026-11-21, after re-recording 29 of Rachel's word clips (honest
status: not fully passing).** On the held-out set (48 words × 5 phones/speakers):

| Check | Result |
|---|---|
| Same recording, same verdict | passes: 0 differences |
| Correct on one noisy retry, WrongStress on another | passes: 0 flips |
| Rachel's own delivery marked wrong | passes: 0 of 240 |
| Wrong stress accepted | **fails: 6 of 790** — Kanan, Kuha, Pagod, Sipilyo ×2, Tuwa |
| Rachel's own delivery accepted | 128 of 240; the rest are asked again |

Before the re-recording the same test gave 9 false accepts and 1 false reject,
and 10 words were trusted. Now 14 are: Basa, Bati, Bukas, Kaliwa, Kuha, Lakad,
Ligpit, Medyas, Sabon, Sagot, Tanong, Tayo, Tuwa and Upo. In every remaining
false accept, the syllable aligner cut the wrong-stress audio in the wrong place
under a voice or speed it was not calibrated on. Run
`Tools/test_stress_rules.sh strict` to list each case.

A second batch replaced Ngiti, Alis, Ibahagi and Paalam. The results above are
unchanged except that Rachel's own delivery is now accepted 127 of 240 times.

A third batch replaced Pagod (now trusted, making 15 trusted words) and Paalam.
Held-out wrong stresses accepted dropped to **5 of 790** (Kanan, Kuha, Sipilyo ×2,
Tuwa), with 0 of 240 of Rachel's own deliveries marked wrong.

**Bangon and Kunin corrected.** The group confirmed they are **Bángon** and
**Kúnin**, with the stress on the first syllable, not Bangón and Kunín.
`WordStress` now marks them that way, which also changes the on-screen sign
from BANGÓN to BÁNGON. The current Bangon clip measured best and was kept. Kunin
now uses take 11 of the third batch.

With those two fixed, the held-out results are: wrong stress accepted on 5 of
800, Rachel's own delivery marked wrong on 0 of 240, and accepted on 134 of 240.

Still open:
- **Paalam:** the new take is clearly pa-**Á**-lam, but the checker cannot find
  its syllables under every simulated condition, so it cannot be judged from
  audio. The pause for the glottal stop is not the problem. The cause is that
  "lam" is about 15 dB quieter than "pá-á" and ends in a long humming m, so the
  l and the m barely show. Two aligner fixes were tried on 2026-11-21 and
  reverted:
  - **Requiring l, m, n, y and w to be voiced** helped four words slightly, but
    made Sulat unpassable and Tuwa lose trust, and did not fix Paalam.
  - **Recognising a voiced consonant at the start or end of a word** made 15
    words unmeasurable.

  A fourth batch (2026-11-22) fixed it with a new take (take 4). Paalam is now
  measurable: Rachel's own delivery is accepted under 5 of 12 simulated
  conditions, and no wrong stress is accepted. It is not trusted, so a child is
  asked again more often than on other words. Every recorded word can now be
  measured. Totals after this batch:

  | Measure | Result |
  |---|---|
  | Trusted words | 15 |
  | Held-out wrong stresses accepted | 5 of 800 |
  | Rachel's own delivery accepted | 135 of 240 |
  | Rachel's own delivery marked wrong | 0 |

In the old system the moved stress was accepted on up to 1 in 9 conditions per
word, and strict mode passed unenforced words on a coin flip. The new system's
errors are much rarer, but they are not zero, and they show the ceiling of a
hand-built syllable finder.

`Tools/test_stress_rules.sh file WORD a.wav ...` judges any recordings.
`Tools/test_stress_rules.sh labelled` judges **real labelled recordings**. In the
Editor, hold **R**, say the word, let go, then press **F5** (said the way Rachel
does), **F6** (stress on the wrong syllable) or **F7** (a sound wrong). The
recording is kept under `Recordings/Labelled/WORD/LABEL/`, which git ignores, so
voices stay on that computer. The command exits 1 if anything labelled wrong was
accepted.

## 4. What it cannot do (limitations)

* **Synthetic wrong stress.** Nobody has recorded *táyo* for the game, so the
  wrong examples are made from Rachel's own recording. Real voices, especially
  children's, must be checked with the labelled-recording workflow above before
  the numbers can be quoted as accuracy.
* **Not every word can be verified from Rachel's recording.** For a word to be
  *trusted*, her delivery must pass and every wrong stress must fail on all 12
  simulated phones. For several words her TTS delivery barely stresses the
  marked syllable. Bangon, for example, has equal length and loudness on both
  syllables, with the pitch lower on the "stressed" one. No measurement can then
  separate right from wrong, so strict mode keeps asking. Re-recording those
  words with a clearly stressed delivery is the fix. The list is in the output
  of `generate`.
* **Sounds within a syllable** (t against k, a against o) are judged by the
  recognizer and `PronunciationChecker`'s strict letter matching. The audio
  judges check stress and syllable structure. Telling phonemes apart from audio
  needs a trained acoustic model.
* **Phones.** Without the child's audio, only the loudness readings exist, and
  they carry no pitch and very coarse timing. Several words then cannot pass at
  all unless the child stresses more clearly than Rachel does.

### What would make it fully reliable

1. **Audio on the phone.** Record the answer with the game's own microphone and
   pass that same audio to the Android recognizer
   (`RecognizerIntent.EXTRA_AUDIO_SOURCE`, Android 13 and later). This needs a
   small native plugin, or a fork of the SpeechToText plugin, so recognizer and
   stress check hear the same sound.
2. **A trained Filipino phoneme model** for sounds within a syllable, for
   example wav2vec2 or Whisper fine-tuned on Tagalog. This could run on a server,
   or on the device through Unity Sentis. Mainstream pronunciation-assessment
   services do not offer Filipino.
3. **Real labelled recordings** from several speakers, including children, of
   both stresses of every pair, to replace the synthetic wrong-stress examples in
   calibration.
