# Pronunciation dataset: Tayo, Upo, Bukas, Basa, Bati, Pasok

This folder holds the tools that turn the group's own recordings into the
classifier the game uses for the six strictly judged words
(`Assets/Scripts/PronunciationModel.cs`). Background and results are in
`Docs/PronunciationChecking.md`, section 2c.

## What each speaker records

One file per speaker. For each word, **in this order**:

1. **Tayo**: tayô (stand up) ×3, then táyo (we) ×3
2. **Upo**: upô (sit down) ×3, then úpo (bottle gourd) ×3
3. **Bukas**: bukás (open) ×3, then búkas (tomorrow) ×3
4. **Basa**: bása (read) ×3, then basâ (wet) ×3
5. **Bati**: báti (greet) ×3, then batî (beaten) ×3
6. **Pasok**: pások (enter) ×3, then pasók (inserted) ×3

That makes 36 takes. Leave about a second of silence between takes; a longer
pause is fine. Say every take on its own, in a normal voice. Don't exaggerate
the other pronunciation: the model should learn how people really say it.

**Consent.** Record only people who have agreed to their voice being used for
this, and for a child, a parent or guardian too. The audio stays on the
computer that processes it (`Recordings/` is git-ignored). Only the timing
manifest and the trained numbers are committed. Never upload the recordings to
Supabase.

## Adding a speaker

Needs Python 3 with `numpy`, `scipy` and `soundfile` (`pip install --user soundfile`).
MP3, WAV, FLAC and OGG all work. Convert `.m4a` to WAV first.

1. **Cut the takes:**

   ```
   python Tools/pronunciation_dataset/segment.py D:/path/to/SPEAKER.mp3 --speaker SPEAKER
   ```

   Use a short pseudonym for `SPEAKER` (letters, digits, `-`, `_`), not a full
   name. The script writes:
   - `Tools/pronunciation_dataset/manifests/SPEAKER.csv`: timings and labels.
   - `Recordings/Dataset/SPEAKER/*.wav`: one 16 kHz clip per take.
   - `Recordings/Dataset/SPEAKER/review.html`: a waveform view with every take playable.
2. **Check the takes.** Open `review.html` and listen to a few takes of each
   word, especially the first and last of each group of three. A take in the
   wrong group means a wrong label.
3. **If the script says `NOT LABELLED`** (it found more or fewer than 36 takes),
   edit the manifest:
   - set `keep` to `0` on a cough, a breath or a false start;
   - change `start_s` / `end_s` to move a boundary;
   - add a row to split two takes that ran together.

   Then re-run with `--from-manifest`. That uses your boundaries exactly and
   never detects again. Use the same flag after any manual change, even when the
   count was right.
4. **Evaluate and train** (Git Bash, from the project root):

   ```
   sh Tools/test_stress_rules.sh dataset-eval     # report only
   sh Tools/test_stress_rules.sh dataset-train    # report, then rewrite Assets/Scripts/PronunciationModelData.cs
   ```

   Both print a leave-one-speaker-out report. Every speaker is tested by a model
   trained without them. Look at:
   - `alternate ACCEPTED` and `correct REJECTED`: these should stay at or near 0;
   - the `BASELINE` lines: the older Rachel-only check on the same takes.
5. **Commit** the new manifest and `PronunciationModelData.cs`. Never commit
   `Recordings/`.

Training takes 3–5 minutes for three speakers and grows with each one. Set
`PM_QUICK=1` to skip the feature-set comparison, and `PM_L2=0.02` to try a
weaker weight penalty (the default is 0.05).

## Adding takes recorded in Unity (F5 / F6)

You can also calibrate from inside the game. In the Editor, hold **R**, say the
word, and let go. Then press **F5** if you said it the taught way or **F6** if
you said it the other way. Label every take, and record both ways for each word:
a speaker trains a word only when they have takes of both pronunciations. Say
the other way first, because a correct answer moves the scene on to the next word.

```
python Tools/pronunciation_dataset/import_labelled.py --speaker kl
sh Tools/test_stress_rules.sh dataset-train
```

To see how much a speaker's own takes help, without testing on what the model
was trained on, run this. It trains on half of that speaker's takes and tests on
the other half:

```
PM_QUICK=1 PM_ONLY_SPLIT=kl sh Tools/test_stress_rules.sh dataset-eval
```

## Quick checks

These run without Unity:

- **Speed:** how long the full check takes on one answer, padded to 8 s like
  the phone's recording:

  ```
  PM_ONLY_TIME=1 sh Tools/test_stress_rules.sh dataset-eval
  ```

- **Cropping:** every take judged as recorded and inside 8 s of quiet, to
  confirm that cutting long recordings to the word (`AnswerJudge.CropToWord`)
  doesn't change verdicts:

  ```
  PM_ONLY_CROP=1 sh Tools/test_stress_rules.sh dataset-eval
  ```

## Retuning

- The confidence lines (`DefaultAccept` / `DefaultReject` in
  `Assets/Editor/PronunciationModelBuilder.cs`, now 0.6 / 0.3, applied to the median p) were chosen
  from four adult speakers. Recheck them when children's recordings arrive: the
  report prints 0.7/0.3, 0.8/0.2 and 0.9/0.1 side by side.
- To add or remove a strictly judged word, change `AmbiguousWords.cs` and
  `WORDS` in `segment.py` together, and record every speaker again for the new
  word.
