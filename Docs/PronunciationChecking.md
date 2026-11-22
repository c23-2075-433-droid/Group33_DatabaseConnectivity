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
3. **Stress only matters for six words** (since 2026-11-22): tayô/táyo,
   upô/úpo, bukás/búkas, bása/basâ, báti/batî and pások/pasók. These get the
   strict hybrid check in section 2a. Talón, lákad, kúha, sulát and línis also
   have a second pronunciation, but the group chose to judge them as ordinary
   words, so the right word passes whatever its stress.
   - (Superseded by section 2a for the six:) a pair is refused only when **both** audio checks agree the stress is on the
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

## 2a. Strict hybrid check for ambiguous words (2026-11-22)

**Why balanced mode was inconsistent on the pairs.** In
`AnswerJudge.JudgeBalanced`, a pair word was accepted whenever the stress check
was *not decisive*. That covered an unclear measurement, the two judges
disagreeing, "po" around the word, and no recording at all. On the phone there
is never a recording (`recordOwnVoice` is off), so a borderline loudness reading
passed. The same wrong *táyo* could therefore pass on one try and fail on the
next: 61 of 374 wrong stresses were accepted, with 2 flips. Nothing asynchronous
was involved; the verdict is computed synchronously when the recognizer's result
arrives.

**What decides which words are strict.** `AmbiguousWords.cs` lists the six words
the group chose: Tayo, Upo, Bukas, Basa, Bati and Pasok. Each entry holds the
meaning and stress mark the game teaches (Rachel's) and the other meaning and
mark, e.g. tayô "stand up" / táyo "we". These six go to
`AnswerJudge.JudgeAmbiguous`; every other word, including Talon, Lakad, Kuha,
Sulat and Linis, stays balanced. Adding or removing a strict word means editing
this list and the matching row in `WordStress.Table`, then running
`Tools/test_stress_rules.sh generate`.

**How the hybrid check decides** (`JudgeAmbiguous`):

- **The letters must be right first.** Audio never turns another word (*kayo*)
  into the target.
- **Judge T, the whole-word template comparison** (`PronunciationTemplate`):
  - It lines the answer up with Rachel's actual recording using dynamic time
    warping on MFCC features, so speed differences don't matter.
  - Along that line-up it compares pitch movement (relative to the speaker's
    own median), loudness movement (relative to the speaker's own peak) and
    timing (relative to the speaker's own pace). Voice height, loudness and
    overall speed cancel out.
  - It does the same against the other pronunciation; the score says which one
    the answer is nearer.
  - References are stored at three voice heights (adult, ×1.25, ×1.45, i.e.
    child-like) and precomputed into `PronunciationTemplateData.cs`.
  - Real recordings of the other pronunciation can be added in
    `Assets/Audio/VO/alt/word_WORD_alt*.wav` and are used automatically.
- **Judges A, the syllable-based stress check** (`PronunciationAssessor`), with
  `RawStressAnalyzer` as its second opinion.
- **The verdict:**
  - **Correct** when T says Rachel's and A doesn't disagree, or when A, on a
    trusted word, and its second opinion both say Rachel's and T doesn't
    disagree.
  - **WrongStress** only when **two** judges say it's the other word.
  - **Ask again** in every other case: disagreement, an unclear recording,
    "po" around the word, or no audio. An ambiguous word is **never accepted
    on its letters alone**.

**Results on held-out material** (`Tools/test_stress_rules.sh hybrid`; none of
these voices, noise levels or stress strengths were used to set the lines):

| Check | Result |
|---|---|
| Other pronunciation accepted | 0 of 110 |
| Rachel's pronunciation refused | 0 of 55 |
| Same answer flipping between accepted and refused | 0 |
| Same recording getting a different verdict | 0 |
| Rachel's pronunciation accepted | 44 of 55 (the rest asked again) |
| Other pronunciation refused | 91 of 110 (the rest asked again) |

The table above is from the eleven-word version. Talon (always asked again)
and Linis (2 of 5) were the weakest, and both are now ordinary words.

**Six-word version (2026-11-22), held-out material:**

| Check | Result |
|---|---|
| Other pronunciation accepted | 0 of 60 |
| Rachel's pronunciation refused | 0 of 30 |
| Flips between accepted and refused | 0 |
| Same recording getting a different verdict | 0 |
| Rachel's pronunciation accepted | 27 of 30 |
| Other pronunciation refused | 53 of 60 |

Per word:

| Word | Rachel's accepted | Other refused |
|---|---|---|
| Tayo (trusted template) | 5/5 | 10/10 |
| Upo | 5/5 | 9/10 |
| Bukas | 5/5 | 7/10 |
| Basa | 5/5 | 8/10 |
| Bati | 4/5 | 10/10 |
| Pasok | 3/5 | 9/10 |

Ordinary words: none of Rachel's 714 own deliveries refused, and no word refused
for its stress (0 of 1,564). Full suite: 222 checks pass; the one failure is in
the optional old strict mode, which the game does not use.

**Results on real recordings. This method is not yet validated on real voices.**

| Real recordings | Result |
|---|---|
| Developer's 8 correct *Bángon* takes (Bangon set up as if ambiguous, as an experiment) | Template alone: "other word" 3, unclear 5, Rachel's 0 |
| Developer's 25 unlabelled Tayo takes (one of them is actually Lakad) | Hybrid: 4 accepted, 11 refused, 14 asked again; accuracy unknown without labels |

The held-out tests are all made from Rachel's own recording, so they only test
her voice against her voice with the stress moved. On a real speaker the
template cannot yet tell "a different speaker" from "a different stress". That
is why a refusal needs two judges. Before the strict hybrid check can be trusted
with children, it needs **real labelled recordings** of both pronunciations of
each word from several speakers, children included, to recalibrate. Record in
the Editor with hold-R plus F5 or F6, and put real alternative pronunciations in
`Assets/Audio/VO/alt/`.

## 2c. The classifier trained on our own speakers (2026-11-22)

Section 2a's judges were built from Rachel's voice alone, with the "other"
pronunciation made by moving her stress, and they were never checked on real
voices. The group then recorded a dataset: **three speakers (la, gar, olyk).
Each said every word the taught way three times, then the other way three
times.** That is 108 takes. The six words now have a classifier trained on
these recordings (`PronunciationModel`), and the older judges stay on only as
a safety net.

**What the recordings showed.** All six contrasts are stress contrasts.
Tagalog stress is mostly vowel **length**, plus pitch.

| Word | Taught | Other | What separates them in the recordings |
|---|---|---|---|
| Tayo | tayô | táyo | 1st vowel ~110 ms vs ~220 ms; pitch higher on the 1st syllable in táyo |
| Upo | upô | úpo | 1st syllable +6 to +11 semitones in úpo |
| Bukas | bukás | búkas | length, and pitch up to +12 semitones on bú- |
| Basa | bása | basâ | long 1st vowel in bása |
| Bati | báti | batî | **weakest**: both end in a glottal stop and both fall in pitch; only the length differs, not always clearly |
| Pasok | pások | pasók | length, loudness and pitch, consistently |

**How it works.**

- **Dataset.** `Tools/pronunciation_dataset/segment.py` finds the takes by
  silence and labels them by their order. It writes an editable manifest
  (`manifests/SPEAKER.csv`), 16 kHz clips in `Recordings/Dataset/` (git-ignored)
  and a page to listen to every take. It refuses to label a file that doesn't
  give exactly 36 takes. You then fix the manifest by hand and re-run with
  `--from-manifest`. gar's file had one 0.15 s noise before Bukas, which it
  drops and shows as a dropped row.
- **Features.** All five are measured within the child's own word, so
  absolute pitch, loudness and speed never count:
  - vowel-length ratio;
  - pitch difference in semitones;
  - loudness difference in dB;
  - pitch fall inside the last vowel;
  - the whole-word DTW template score against every recorded speaker's taught
    and other takes (stored at three voice heights).
- **Model.** One logistic regression per word, on standardised features, with
  a strong weight penalty. Every training take is also heard under the 12
  simulated phones and voices (noise; voice ×1.25 and ×1.45; slower and faster
  speech). A training take's template score is measured only against *other*
  speakers.
- **Verdict.** p is the probability of the taught pronunciation, measured six
  ways per answer:
  - **Correct** when every p ≥ 0.7;
  - **Incorrect** (WrongStress) when every p ≤ 0.3;
  - **Uncertain** (asked again, not scored) otherwise, or when the syllables
    can't be found.
- **Safety net** (`AnswerJudge.DecideWithModel`):
  - an acceptance turns into "ask again" only when the template *and* a
    syllable judge both say the other word;
  - a refusal turns into "ask again" when either the template or a trusted
    syllable judge says Rachel's;
  - when the classifier is unsure, all three older judges agreeing still
    decides. This keeps Rachel's synthetic táyo refused, as the test suite
    requires.
- **Switch.** `VoiceCommand.useLearnedPronunciationModel` (on by default)
  turns it off. A word with no trained model falls back to section 2a.

**Results, leave one speaker out** (`Tools/test_stress_rules.sh dataset-eval`).
Every take is judged by a model trained without its speaker.

| Whole game check (AnswerJudge), held-out takes | As recorded (108) | Under 12 simulated phones/voices (1,296) |
|---|---|---|
| Correct pronunciation accepted | 47 / 54 | 475 / 648 |
| Alternate pronunciation rejected | 39 / 54 | 460 / 648 |
| **Alternate mistakenly accepted** | **0** | 8 |
| **Correct mistakenly rejected** | **0** | 6 |
| Uncertain (asked again) | 22 | 347 |
| Accuracy (all takes) | 80 % | 72 % |
| *Old Rachel-only check, same takes: wrong accepts / wrong rejects* | *1 / 2* | *23 / 39* |

On the same real voices the new check makes no errors on the recorded takes,
where the old check made 3. Under simulated conditions it makes 14 errors,
against 62 for the old check. It asks again about as often (22 against 19 of
108). The classifier alone, without the safety net, gets 81 %
of the recorded takes right with no errors, and asks again on 20.

Per word, classifier alone, as recorded:

| Word | Correct accepted | Alternate rejected | Asked again |
|---|---|---|---|
| Tayo | 7/9 | 8/9 | 3 |
| Upo | 9/9 | 7/9 | 2 |
| Bukas | 9/9 | 7/9 | 2 |
| Basa | 6/9 | 6/9 | 6 |
| Bati | 5/9 | 9/9 | 4 |
| Pasok | 9/9 | 6/9 | 3 |

**Limits. Read these before quoting the numbers.**

- **Three speakers** is enough to fit five weights per word. It is not
  enough to give a reliable accuracy figure: each test is one voice, 18 takes
  per word. The speakers' ages were not recorded. **No child has been recorded.** The ×1.25 and ×1.45 voice raises
  only approximate a child's voice.
- The speakers exaggerate the other pronunciation (pitch jumps of up to 12
  semitones). A child who says it naturally may land in "ask again" more often.
- To aim for: **at least 10 speakers, 5 or more of them children of the
  players' age, mixed gender**, plus a separate group of children used only
  for the final test.
- **Phones: not yet.** The game records no audio on a phone (`recordOwnVoice`
  is off), so there the six words are still judged from the recognizer's
  loudness readings (`StressAnalyzer.JudgeBanded`). The classifier runs in the
  Editor, on desktop, and on a phone only with Record Own Voice turned on. Fixing
  this needs one of:
  - a native plugin that records the audio and feeds it to the recognizer
    (Android 13 and later);
  - recording first with Unity's microphone for these six words.
- `PronunciationModelData.cs` is about 500 KB of source, because it stores the
  reference frames of every take.

**Testing inside Unity.**

1. Open a scene that asks one of the six words (for example Bahay scene 1 for
   Tayo).
2. Hold **R**, say the word, and let go. The Console shows the verdict with
   `model: Correct/Incorrect/Uncertain`, p, the features, and what the other
   judges said.
3. Press **F5** (said the taught way), **F6** (said the other way) or **F7** (a
   sound wrong). The take is kept under `Recordings/Labelled/WORD/LABEL/`.
4. Run `Tools/test_stress_rules.sh labelled` to judge every labelled take with
   the current model. It exits 1 if anything labelled wrong was accepted.
5. To compare with the older check, untick **Use Learned Pronunciation Model** on
   the scene's VoiceCommand.

### 2d. Calibrated to natural speech (2026-11-22, later the same day)

**What went wrong in testing.** A developer tried the six words in the Editor,
saying them naturally. Correct answers were mostly asked again, and a word was
accepted only when it was exaggerated. Bati was accepted 2 times in about 19
tries. There were two causes:

- **Exaggerated training data.** The three speakers had exaggerated the
  contrast, so natural speech scored in the middle (p 0.3–0.6).
- **An all-six rule.** A verdict needed all six measurements to succeed and
  all six to clear the line, so one stray measurement meant "ask again".

**What changed.**

- **Your session as training data.** The developer's 38 session takes became a
  fourth speaker, `kl`: 32 taught takes and 6 marked as the other way with F6.
- **Median rule.** At least 4 of the 6 measurements must find the word. The
  verdict uses their median p, and no single measurement may sit past the
  opposite line.
- **New lines.** Correct from p ≥ 0.60 (was 0.70); Incorrect at p ≤ 0.30 (unchanged).
- **Protection against voice bias.**
  - A speaker trains a word only if they recorded *both* pronunciations of it.
  - Each speaker's correct takes and alternate takes carry equal total weight.

  Without this, kl's 22 correct and one alternate Bati takes taught the model
  "this voice means correct", and kl's batî crept from p 0.06 to 0.43.
- **Spelling.** Bati is now written **Báti** (BÁ-ti). The group chose the mark
  on the a only (to be validated: see section 2h).

**Results.**

| | Accepted / refused, as recorded |
|---|---|
| Whole game check, leave one speaker out (4 speakers, 146 takes) | correct accepted 63/86, alternate refused 43/60, **0 wrong accepts, 0 wrong refusals**, asked again 40 |
| Old Rachel-only check, same takes | correct accepted 55/86, alternate refused 48/60, 1 wrong accept, 4 wrong refusals |
| kl held out entirely (the model never heard kl) | correct accepted 11/32 (classifier alone) |
| kl split in half (trained on the other half of kl's takes) | correct accepted 18/32, alternate refused 5/6, 0 wrong accepts |

So a speaker's own natural takes help with their other takes. A new voice that
nobody has recorded still meets the older, more cautious behaviour, and it is
asked again rather than refused. kl recorded only one alternate per word, which
limits how much of kl's style the model can learn. A full 3 + 3 per word from kl,
and from children, is the next step. `PronunciationModelData.cs` is now about
780 KB.

### 2e. The developer's labelled takes, and two more safeguards (2026-11-22)

**Data.** The developer recorded 88 labelled takes in the Editor (F5 / F6).
`Tools/pronunciation_dataset/import_labelled.py` copied them into the dataset
as speaker `kl`, replacing the unlabelled session takes:

| | Tayo | Upo | Bukas | Basa | Bati | Pasok |
|---|---|---|---|---|---|---|
| Taught way | 6 | 7 | 11 | 4 | 20 | 6 |
| Other way | 5 | 4 | 8 | 4 | 8 | 5 |

Two takes the model disagreed with were checked by ear:

- One Tayo take had been pressed F6 by mistake. It was moved to `correct`.
- One Basa F6 take and two Pasok F5 takes were labelled right. They are real
  near-misses: a basâ that sounds close to bása, and two pások the model had
  never heard from this speaker.

**Safeguards added after the simulated-voice tests.** The test suite plays
Rachel's moved-stress recordings as a slower, child-like voice in noise. With
the 0.60 line, it accepted 4 of 60 of those. The fixes:

- **Both syllable judges** agreeing it is the other word now block an
  acceptance (`AnswerJudge.DecideWithModel`). Before, the template also had to
  agree.
- **No measurement below 0.40** may be part of a Correct verdict
  (`PronunciationModel.MinLowForCorrect`).

With both, the test accepts **0 of 60**. On real takes the floor cost 2 of 108
acceptances when the model had never heard the speaker, and none when it had.

**Results.** These are the game check's numbers, before the 0.40 floor; the
floor lowers "correct accepted" by about 2.

| | Correct accepted | Other way refused | Wrong accepts | Wrong refusals |
|---|---|---|---|---|
| Leave one speaker out, whole game check (4 speakers, 196 takes) | 82/108 | 65/88 | 1 | 2 |
| Old Rachel-only check, same takes | 68/108 | 66/88 | 2 | 5 |
| kl split in half (the model knows half of kl's takes) | 46/54 | 26/34 | 0 | 0 |

The three remaining errors are the three near-miss takes above. They only
occur when the model has never heard the speaker.

Full suite: everything passes except the old optional strict mode's 5 of 800,
which predates this work and is not used by the game.
`PronunciationModelData.cs` is now about 1.2 MB, because it stores reference
frames for every take. Keeping fewer references per speaker would shrink it.

### 2f. First test on a voice the model had never heard (2026-11-22, 13:13–13:21)

A second person (`sp5`), who had not recorded anything for the model, played
the six words in the Editor. They said each word naturally, both ways, and
labelled every take with F5 or F6. The model in use was trained on la, gar,
olyk and kl, and had the 0.60 / 0.30 lines and both safeguards from section 2e.
These are the live verdicts from the Console:

| Word | Taught way (F5) | Other way (F6) |
|---|---|---|
| Tayo | 1/1 accepted | 1 asked again |
| Upo | 1/1 accepted | 3/3 refused |
| Bukas | 2/2 accepted | 3 refused, 3 asked again |
| Basa | 1/1 accepted | 2 refused, 1 asked again |
| Bati | 2/3 accepted, 1 asked again | 5 refused, 1 asked again |
| Pasok | 1/1 accepted | 3/3 refused |
| **All** | **8/9 accepted, 0 refused** | **16 refused, 6 asked again, 0 accepted** |

There was no wrong verdict in either direction. This is still one voice (age
not recorded, and not a Grade 2–3 learner), and few correct takes per word. The takes were then added to the dataset as
speaker `sp5`, so this exact test can't be repeated on them. The next unheard
voice, ideally a child's, is the next test.

### 2g. Getting the child's audio on a phone (2026-11-22)

**The problem.** On a phone, Android's speech recognizer holds the
microphone, and it gives the game text only. Text can't show the stress, so the
pronunciation model had nothing to judge there. Two approaches were tried
first and failed:

- **The game records while the recognizer listens** (a switch tried and then
  removed). On a Galaxy A03s (Android 13) the recognizer then heard nothing.
  The give-up counter it relied on was fixed and kept, for the original
  `recordOwnVoice` option: it had missed failures after the phone stopped the
  game's recording, and empty results. It now always switches recording off
  after 3 failed listens.
- **The game listens itself and checks the word by sound.** A sound-matching
  check couldn't tell words apart across voices. To pass every real take of a
  word, it also passed about two-thirds of 1,469 wrong-word recordings (Rachel's
  other words and the other strict words). It was removed.

**The approach now: an audio bridge (Android 13+).**
`Assets/Plugins/Android/SalinlahiAudioRecognizer.java`, with
`AndroidAudioRecognizer.cs` as its Unity side:

- **One recorder.** For the six words, the game records the child with
  Android's AudioRecord. Each block of audio is kept for the model **and**
  written into a pipe. Android's speech recognizer is told to read its audio
  from that pipe (`RecognizerIntent.EXTRA_AUDIO_SOURCE`, API 33). So the
  recognizer checks **which** word and the model checks **how**, on the same
  sound, and only the game ever opens the microphone.
- **Fallback.** Whether a phone's recognizer accepts audio this way is up to
  the phone. VoiceCommand tries the standard recognizer, then the on-device one.
  A failure only counts when the child actually spoke, so silence never counts.
  After two counted failures in each mode, the bridge is given up for the
  session and the six words use the normal recognizer with loudness readings.
- **Switches.** `VoiceCommand.listenWithAudioBridge` turns it on (the default).
  `showAudioBridgeMessages` shows the test box and short on-screen messages
  ("works on this phone" / "trying the on-device recognizer" / "not supported",
  each round, error codes). It is **off by default**, and works only in a
  **Development Build**. A normal (release) APK never shows them, even with the
  switch ticked. To test again, tick both the switch and *Development Build*.
- **Privacy.** The pronunciation classifier runs entirely on the phone, and
  SALINLAHI never writes the audio to storage or uploads it: the game's copy
  stays in memory only until it is judged. **But the same audio is given to
  Android's speech-recognition service** to find out which word was said.
  SALINLAHI asks for the standard recognizer, and the on-device one is not
  available on the A03s, so that service may process the audio online (for
  example on Google's servers), under the phone's own speech-service terms.
  This is no change from before: every voice answer in the game already went
  through the same service. The manuscript's privacy statement should say
  exactly this, and not "audio never leaves the phone".

**Tried on a Galaxy A03s (Android 13), four rounds of fixes:**

1. **Deaf, never falling back.** Timeouts and instant refusals were not
   counted as failures. Now they are.
2. **"Speech began", but never an answer.** A recognizer reading from a pipe
   answers only when the stream ends. The plugin now detects the end of the
   word itself and closes the stream.
3. **`partial: tayo`, but an empty final result.** The last partial is used
   when the final result is empty, or ends in "no match" after a partial.
4. **It worked, but slowly.**
   - The word only "ended" after about 5 s, because the game's music kept the
     room from sounding quiet. Now the recognizer's own end of speech ends
     the round, as soon as it has named a word.
   - The check itself measured the whole 8 s recording, six times over, in
     each judge: about 0.7 s on a PC, several seconds on the phone.
     `AnswerJudge.CropToWord` now cuts the recording to the word ± 0.5 s
     first, which takes about 0.2 s on a PC.
   - Judged as recorded and inside 8 s of quiet, 225 of 229 real takes got the
     same verdict. The other 4 moved between decided and "ask again". None
     moved between accepted and refused.

After fix 3, on the A03s, Tayo and Bukas were recognized and judged by the
model. The wrong pronunciation was refused, and the right one accepted. The
on-device recognizer is not available on this phone
(`UnsupportedOperationException`), so only the standard recognizer is used.

### 2h. Verified results, five speakers (re-run on the merged code, 2026-11-22)

These figures were re-run on `origin/main` after pull request #24 was merged
(commit `f5e9ebb`), from a clean copy of the repository and the git-ignored
`Recordings/Dataset`. They are the figures to quote, with the caveats below.
Earlier tables in sections 2c–2e are from earlier rounds with fewer speakers.

Reproduce them with:

```
PM_QUICK=1 sh Tools/test_stress_rules.sh dataset-eval
sh Tools/test_stress_rules.sh
```

**Data.** 229 takes from five speakers. Each speaker is judged by a model
trained without them (leave one speaker out).

| Speaker | Source | Taught takes | Other-way takes |
|---|---|---|---|
| la, gar, olyk | one recording each, 3 + 3 per word | 18 each | 18 each |
| kl | labelled takes in the Editor (F5 / F6) | 54 | 34 |
| sp5 | labelled takes in the Editor (F5 / F6) | 9 | 24 |

The speakers' ages were not recorded. None of them is a Grade 2–3 learner.

**Two different results, which measure different things:**

| 229 takes | Classifier alone | **Whole game check** (what a player gets) | Earlier Rachel-only check |
|---|---|---|---|
| Correct pronunciation accepted | 96 / 117 | **95 / 117** | 75 / 117 |
| Other pronunciation refused | 88 / 112 | **81 / 112** | 87 / 112 |
| Other pronunciation wrongly accepted | 3 | **2** | 2 |
| Correct pronunciation wrongly refused | 3 | **2** | 5 |
| Asked to repeat | 39 | **49** | 60 |
| Accuracy, all takes | 80 % | **77 %** | 71 % |
| Accuracy, decided takes only | 97 % | 98 % | 96 % |

- The **classifier alone** is `PronunciationModel` by itself.
- The **whole game check** is `AnswerJudge`: the classifier plus the older
  judges as a safety net, which turn some verdicts into "ask again". It is the
  result to quote for the system.
- "Accuracy, all takes" counts "asked to repeat" as not decided correctly.
- Under the 12 simulated phones and voices (2,748 versions of the same takes),
  the whole game check decided 66 % correctly, against 57 % for the earlier
  check. It wrongly accepted 39 and wrongly refused 83.

Classifier alone, per held-out speaker (the tool prints these per speaker;
the whole-game totals are only printed overall):

| Speaker | Correct accepted | Other refused | Wrong accepts | Wrong refusals |
|---|---|---|---|---|
| la | 18/18 | 18/18 | 0 | 0 |
| olyk | 17/18 | 15/18 | 0 | 0 |
| gar | 15/18 | 13/18 | 0 | 1 |
| sp5 | 9/9 | 19/24 | 0 | 0 |
| kl | 37/54 | 23/34 | 3 | 2 |

**Caveats. State these with the figures.**

- **Thresholds tuned on the same recordings.** The decision lines (Correct
  from p ≥ 0.60 with no measurement below 0.40; Incorrect at p ≤ 0.30) were
  chosen while looking at these same 229 takes. The figures are therefore
  likely somewhat optimistic. A fresh group of speakers, ideally children, is
  needed for an unbiased figure.
- **Not independently verified.** The figures come from the project's own test
  tool, run by the developers. They have not been reproduced by anyone outside
  the group.
- **No Grade 2–3 learners** were recorded or tested. Accuracy for children has
  not been established.
- **Five speakers** is a small sample, and kl contributed more takes than
  everyone else together.

**Automated test suite** (same commit): **222 checks passed, 1 failed.** The
failure is in the optional "strict on every word" mode, which the game does not
use: it accepted 5 of 800 simulated wrong stresses. It failed the same way
before this work.

**Vocabulary still to validate.**

- **Báti.** The group chose to write the greeting **Báti**, with the stress
  mark on the *a* only. In standard Filipino marking, the greeting is **bátì**:
  the grave accent marks its final glottal stop, and the group's own
  Vocabulary Validation Form lists "bátì – to greet". Written without it, the
  word would normally be read without the glottal stop. A Filipino language
  expert should confirm which form the game shows.
- **Pasok.** "pások – to enter" matches the validation form. The meaning given
  for the other pronunciation, "pasók, inserted / included", was never
  validated, because the form lists only the taught words.

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
