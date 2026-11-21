using System.Collections.Generic;

/// <summary>
/// The whole right/wrong decision for one answer: the letters
/// (PronunciationChecker) and, for words whose meaning depends on their stress,
/// the sound. VoiceCommand calls this for every recognizer result while a word
/// is live.
///
/// Plain C#, so the decision can be tested without Unity (Tools/stresstests)
/// and is the same code on a phone, in the Editor and in the tests.
///
/// THE RULES, IN ORDER
///
///   1. The letters must be the word (PronunciationChecker.IsCorrect).
///      Otherwise: Wrong.
///   2. A word whose stress is not known (WordStress), or with one syllable,
///      is decided by its letters alone, exactly as before: Correct.
///   3. A same-spelling pair (tayô, upô, bukás, bása, talón, bátì, lakád,
///      pások, kúha, súlat, línis) said inside a sentence usually IS the other
///      word - "kain tayo" is "let's eat" - so that is Wrong. Any other word
///      inside a sentence, or with filler around it, is accepted on its
///      letters as before: its stress cannot be measured there, and its
///      meaning cannot change.
///   4. The stress is judged from the best evidence there is:
///        a. the recorded audio of the answer (RawStressAnalyzer: length,
///           loudness and pitch), when StressAudioCapture got it;
///        b. otherwise the recognizer's loudness readings (StressAnalyzer);
///      each against Teacher Rachel's own recording of the word. Right
///      syllable: Correct. Other syllable - the other word: WrongStress.
///      A source is only ACTED ON for words its references mark enforced
///      (her recording passes and the moved one fails under every simulated
///      device); otherwise it is measured and logged and the letters decide.
///   5. For a pair with an enforced source, "tayo po" (filler around it) cannot be
///      judged - the recording holds two words - so the child is asked to say
///      it on its own: Unclear. So is a recording with no clear voice in it.
///   6. If there is no evidence at all (no audio and no level readings ever
///      arrived), the stress can never be measured on this device; refusing
///      every answer would leave the child stuck for good, so the letters
///      decide and the attempt is flagged stress-unverified in the log.
///      Unclear evidence that DOES exist is never waved through.
///
/// STRICT MODE (Settings.requireRachelsStress, on by default) removes every
/// "the letters decide" above for a word with a known stress: the answer is
/// Correct only when its stress was measured and sits on Rachel's side. A word
/// in a sentence or with filler, an unenforced reference, or no evidence at
/// all is Unclear (say it again, on its own); an unenforced measurement that
/// lands on the other syllable is WrongStress. Words with one syllable or no
/// decided stress have nothing but letters to judge, so rules 1-2 still hold.
/// </summary>
public static class AnswerJudge
{
    public enum Outcome
    {
        Correct,
        /// <summary>A different word, or the word inside a sentence that changes its meaning.</summary>
        Wrong,
        /// <summary>The right letters with the stress on the wrong syllable: the other word.</summary>
        WrongStress,
        /// <summary>The stress could not be judged; ask again without counting a miss.</summary>
        Unclear,
    }

    /// <summary>Which evidence the stress verdict came from.</summary>
    public enum StressSource { None, RawAudio, VoiceLevels }

    /// <summary>What was heard besides the letters.</summary>
    public struct Evidence
    {
        /// <summary>Recorded audio of the answer (mono, -1..1), or null.</summary>
        public float[] audio;
        public int audioSampleRate;
        /// <summary>The recognizer's voice-level readings for the session, or null.</summary>
        public IList<float> levels;
        public float levelIntervalMs;
        public StressAnalyzer.LevelScale levelScale;
    }

    public struct Result
    {
        public Outcome outcome;
        /// <summary>The stress pattern that was checked, or null when the word has none.</summary>
        public StressPattern pattern;
        public StressSource source;
        /// <summary>True when a stress measurement was made.</summary>
        public bool stressChecked;
        /// <summary>True when that measurement was acted on.</summary>
        public bool stressEnforced;
        /// <summary>True when the answer passed on its letters because there was no evidence at all.</summary>
        public bool stressUnverified;
        public StressAnalyzer.Judgement stress;
        /// <summary>A short reason, for the console and the attempt log.</summary>
        public string reason;

        public bool IsCorrect { get { return outcome == Outcome.Correct; } }
    }

    public struct Settings
    {
        /// <summary>Moves the stress cut-off; 1 = halfway between Rachel and the moved stress.</summary>
        public float stressTolerance;
        /// <summary>See rule 6. Only ever applies when there is NO evidence at all, and never in strict mode.</summary>
        public bool acceptOnLettersWhenNoEvidence;
        /// <summary>
        /// STRICT MODE (the default): a word with a known stress is only ever
        /// Correct when its stress was MEASURED and matched Teacher Rachel's
        /// recording. Every place the rules above let the letters decide
        /// instead - a word whose reference is not enforced, filler or a
        /// sentence around the word, no evidence at all - becomes Unclear
        /// (asked again, not scored), or WrongStress when the measurement
        /// put the stress on the other syllable. The price: on the words
        /// whose references are not enforced, the measurement is less sure,
        /// so a right answer is sometimes asked for again.
        /// </summary>
        public bool requireRachelsStress;

        public static Settings Default
        {
            get
            {
                return new Settings
                {
                    stressTolerance = 1f,
                    acceptOnLettersWhenNoEvidence = true,
                    requireRachelsStress = true,
                };
            }
        }
    }

    public static Result Judge(string spokenText, string targetWord, string[] acceptedVariants,
                               Evidence evidence, Settings settings)
    {
        Result r = new Result();
        r.pattern = StressPattern.For(targetWord);

        PronunciationChecker.WordContext context =
            PronunciationChecker.ContextOf(spokenText, targetWord, acceptedVariants);

        if (context == PronunciationChecker.WordContext.NotSaid)
        {
            r.outcome = Outcome.Wrong;
            r.reason = "not the word";
            return r;
        }

        if (r.pattern == null)
        {
            r.outcome = Outcome.Correct;
            r.reason = "word matched";
            return r;
        }

        bool strict = settings.requireRachelsStress;

        if (context == PronunciationChecker.WordContext.InSentence)
        {
            if (r.pattern.IsMinimalPair)
            {
                r.outcome = Outcome.Wrong;
                r.reason = "said inside a sentence, where it means something else";
            }
            else if (strict)
            {
                r.outcome = Outcome.Unclear;
                r.reason = "said inside a sentence, where its stress cannot be checked - say " +
                           r.pattern.Spoken + " on its own";
            }
            else
            {
                // The stress of one word inside a sentence cannot be measured,
                // and for a word that is not a pair nothing else is at stake.
                r.outcome = Outcome.Correct;
                r.reason = "word matched inside a sentence; stress not measurable there";
            }
            return r;
        }

        if (context == PronunciationChecker.WordContext.WithFiller && strict)
        {
            // The recording holds more than the word, so its stress cannot be
            // measured against Rachel's: ask for the word on its own.
            r.outcome = Outcome.Unclear;
            r.reason = "said with other words around it - say " + r.pattern.Spoken + " on its own";
            return r;
        }

        if (context == PronunciationChecker.WordContext.WithFiller && !r.pattern.IsMinimalPair)
        {
            // "Bangon po": the same leniency as before for a word whose
            // meaning cannot change. Only the pairs are asked to stand alone.
            r.outcome = Outcome.Correct;
            r.reason = "word matched with filler around it; stress not measurable there";
            return r;
        }

        bool hasAudio = evidence.audio != null && evidence.audio.Length > 0 && evidence.audioSampleRate > 0;
        bool hasLevels = evidence.levels != null && evidence.levels.Count > 0;

        // Pick the source: audio first, readings second, and for each only
        // the enforced flag of THAT source counts.
        StressAnalyzer.Judgement j = new StressAnalyzer.Judgement { verdict = StressAnalyzer.Verdict.Unmeasurable };
        if (hasAudio)
        {
            j = RawStressAnalyzer.Judge(evidence.audio, evidence.audioSampleRate, r.pattern, settings.stressTolerance);
            r.source = StressSource.RawAudio;
        }
        if (strict && hasAudio && hasLevels && j.verdict != StressAnalyzer.Verdict.Unmeasurable && !j.enforced)
        {
            // Strict mode acts on every measurement, so when the recording
            // cannot be trusted for this word but the readings can, the
            // readings decide.
            StressAnalyzer.Judgement fromLevels = StressAnalyzer.Judge(
                evidence.levels, evidence.levelIntervalMs, evidence.levelScale, r.pattern, settings.stressTolerance);
            if (fromLevels.verdict != StressAnalyzer.Verdict.Unmeasurable && fromLevels.enforced)
            {
                j = fromLevels;
                r.source = StressSource.VoiceLevels;
            }
        }
        if (j.verdict == StressAnalyzer.Verdict.Unmeasurable && hasLevels)
        {
            // The recording can come back silent when the phone gives the
            // microphone to the recognizer alone; the readings still hold.
            StressAnalyzer.Judgement fromLevels = StressAnalyzer.Judge(
                evidence.levels, evidence.levelIntervalMs, evidence.levelScale, r.pattern, settings.stressTolerance);
            if (fromLevels.verdict != StressAnalyzer.Verdict.Unmeasurable || !hasAudio)
            {
                j = fromLevels;
                r.source = StressSource.VoiceLevels;
            }
        }

        r.stress = j;
        r.stressEnforced = j.enforced;

        if (!hasAudio && !hasLevels)
        {
            bool anyEnforced = RawStressReferences.For(r.pattern).enforced || StressReferences.For(r.pattern).enforced;
            if (!strict && (settings.acceptOnLettersWhenNoEvidence || !anyEnforced))
            {
                r.outcome = Outcome.Correct;
                r.stressUnverified = true;
                r.reason = "no audio or voice levels from the device, so the stress could not be checked";
            }
            else
            {
                r.outcome = Outcome.Unclear;
                r.stressEnforced = true;
                r.reason = "no audio to check the stress of " + r.pattern.Spoken;
            }
            return r;
        }

        if (!j.enforced && !strict)
        {
            r.stressChecked = j.verdict != StressAnalyzer.Verdict.Unmeasurable;
            r.outcome = Outcome.Correct;
            r.reason = "word matched; stress " + (r.stressChecked ? "measured" : "not measurable") +
                       " but not enforced for this word (" + j.reason + ")";
            return r;
        }

        if (context == PronunciationChecker.WordContext.WithFiller)
        {
            r.outcome = Outcome.Unclear;
            r.reason = "said with other words around it - say " + r.pattern.Spoken + " on its own";
            return r;
        }

        r.stressChecked = j.verdict != StressAnalyzer.Verdict.Unmeasurable;
        r.stressEnforced = true;   // acted on: either enforced for this word, or strict mode
        r.reason = j.reason + (j.enforced ? "" : " (strict mode; this word's reference is less certain)");
        switch (j.verdict)
        {
            case StressAnalyzer.Verdict.Matches: r.outcome = Outcome.Correct; break;
            case StressAnalyzer.Verdict.WrongStress: r.outcome = Outcome.WrongStress; break;
            default: r.outcome = Outcome.Unclear; break;
        }
        return r;
    }
}
