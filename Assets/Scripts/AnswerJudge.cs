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
/// The rules above are the LENIENT mode (Settings.requireRachelsStress off),
/// kept unchanged so it can be switched back.
///
/// STRICT MODE (requireRachelsStress, on by default) - ONLY RACHEL'S WAY PASSES
///
/// The right letters are never enough on their own for a word with a known
/// stress. The answer is Correct only when its stress was MEASURED, the
/// measurement is clear, and it is on Teacher Rachel's side:
///
///   a. A recording of the answer (StressAudioCapture) is judged by
///      PronunciationAssessor: syllables found from the word's spelling,
///      length / loudness / pitch of the stressed syllable against its
///      neighbours, compared with Rachel's own word recording and with that
///      recording with the stress moved. Only the shape is compared - never
///      the voice. Each answer is measured six ways (frame offsets, a little
///      noise); every one must agree.
///      A second, independent judge - RawStressAnalyzer, which finds the
///      syllables differently - is consulted too. For a word whose reference
///      PronunciationReferences marks trusted (Rachel passes and the moved
///      stress fails under every simulated phone), the assessor decides
///      unless the second judge, where ITS reference is enforced, flatly
///      contradicts it. For every other word both judges must agree. A
///      disagreement is never settled by picking one: it is Unclear.
///   b. With no recording, the recognizer's loudness readings are judged by
///      StressAnalyzer.JudgeBanded: Correct only past every reading the moved
///      stress ever gave on a simulated phone, WrongStress only past every
///      reading Rachel's own gave, Unclear in between.
///   c. No evidence at all, the word inside a sentence, filler around it, or
///      a recording that does not hold the word's syllables: Unclear - the
///      child is asked to say the word again, on its own. Nothing is scored.
///
/// BALANCED MODE (Settings.balanced, the DEFAULT since 2026-11-22) replaces
/// all of the above for normal play: see JudgeBalanced. Strict and lenient
/// remain selectable on VoiceCommand.
///
/// Settings.verifyOnlyTrustedWords (strict mode only) relaxes rule c for the
/// words whose reference is not trusted: there, an answer that would be
/// Unclear passes on its letters, flagged stressUnverified; a clear wrong
/// stress still fails. The trusted words keep every rule above.
///
/// So the SAME answer always gets the same verdict, and an answer near the
/// line between the two stresses is asked again rather than decided by chance.
/// Words with one syllable or no decided stress (Bag, Kinig, Tapon) have only
/// their letters to judge, so rules 1-2 still hold for them.
///
/// Sounds within a syllable (t against k, a against o) are judged by the
/// device recognizer and PronunciationChecker's strict letter matching; the
/// audio judges check the stress and that the recording has the word's
/// syllables (not one more, not one fewer). See PronunciationAssessor.
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
        /// <summary>Strict mode, recorded audio: PronunciationAssessor's verdict.</summary>
        public PronunciationAssessor.Assessment assessment;
        /// <summary>Ambiguous words: the whole-word comparison with Rachel's recording (PronunciationTemplate).</summary>
        public PronunciationTemplate.Result template;
        /// <summary>Strict mode, recorded audio: the second judge (RawStressAnalyzer).</summary>
        public StressAnalyzer.Judgement secondOpinion;
        /// <summary>Ambiguous words: the classifier trained on the group's recorded speakers (PronunciationModel).</summary>
        public PronunciationModel.Result model;
        /// <summary>True when the trained classifier made the decision.</summary>
        public bool decidedByModel;
        /// <summary>A short reason, for the console and the attempt log.</summary>
        public string reason;

        public bool IsCorrect { get { return outcome == Outcome.Correct; } }
    }

    /// <summary>
    /// ON (the default): the six ambiguous words are decided by the classifier
    /// trained on the group's recorded speakers (PronunciationModel), with the
    /// older Rachel-based judges kept only as a safety net - see JudgeAmbiguous.
    /// OFF, or for a word with no trained model: the older hybrid check alone.
    /// </summary>
    public static bool useLearnedModel = true;

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

        /// <summary>
        /// Strict mode only. ON (the default): the strict rules are applied in
        /// full only to the words whose pronunciation reference is TRUSTED
        /// (PronunciationReferences: Rachel's recording passes and every wrong
        /// stress fails under every simulated phone and speaker). For every
        /// other word the stress is still measured and a clear WrongStress
        /// still fails, but an answer that would only have been "unclear" is
        /// accepted on its letters and flagged stressUnverified in the log.
        ///
        /// Why: tried on real labelled voices, the audio check could not find
        /// the syllables of some untrusted words at all (0 of 8 correct Bángon
        /// accepted), so fully strict mode left the child stuck on the very
        /// first word. On those words a wrong stress can sometimes pass; on the
        /// trusted ones it cannot. OFF: every word with a known stress is
        /// strict, as before.
        /// </summary>
        public bool verifyOnlyTrustedWords;

        /// <summary>
        /// BALANCED MODE (the default; overrides the two settings above): the
        /// word comes first, stress second. See JudgeBalanced.
        /// </summary>
        public bool balanced;

        /// <summary>The default: balanced.</summary>
        public static Settings Default
        {
            get
            {
                Settings s = StrictOnTrustedWords;
                s.balanced = true;
                return s;
            }
        }

        /// <summary>Strict mode, applied in full to the trusted words only.</summary>
        public static Settings StrictOnTrustedWords
        {
            get
            {
                return new Settings
                {
                    stressTolerance = 1f,
                    acceptOnLettersWhenNoEvidence = true,
                    requireRachelsStress = true,
                    verifyOnlyTrustedWords = true,
                    balanced = false,
                };
            }
        }
    }

    public static Result Judge(string spokenText, string targetWord, string[] acceptedVariants,
                               Evidence evidence, Settings settings)
    {
        if (settings.balanced)
            return JudgeBalanced(spokenText, targetWord, acceptedVariants, evidence);
        if (!settings.requireRachelsStress)
            return JudgeLenient(spokenText, targetWord, acceptedVariants, evidence, settings);

        Result r = JudgeStrict(spokenText, targetWord, acceptedVariants, evidence);
        if (settings.verifyOnlyTrustedWords && r.pattern != null && r.outcome == Outcome.Unclear &&
            !PronunciationReferences.For(targetWord).trusted)
        {
            // See Settings.verifyOnlyTrustedWords: this word's stress cannot be
            // verified reliably, so "not clear" does not hold the child back.
            r.outcome = Outcome.Correct;
            r.stressUnverified = true;
            r.stressEnforced = false;
            r.reason = "accepted on its letters - the stress of " + r.pattern.Spoken +
                       " cannot be verified reliably (not a trusted reference): " + r.reason;
        }
        return r;
    }

    /// <summary>
    /// BALANCED MODE - the default. The word comes first; stress is supporting
    /// evidence, and only where it changes the word.
    ///
    ///   1. The letters decide whether the right WORD was said. The device
    ///      recognizer hears the sounds, and PronunciationChecker compares its
    ///      letters strictly: a different vowel or a missing consonant is a
    ///      different word (bagon, bango, bangga are not Bangon), while the
    ///      spelling variation that is free in Filipino (e/i, o/u, doubled
    ///      letters, the recognizer's filler words) is forgiven.
    ///   2. For an ordinary word - one whose meaning does not change with its
    ///      stress, like Bangon - that is the whole decision. A different
    ///      stress, speed, pitch, intonation or accent never fails it. The
    ///      stress is still measured and logged, for the teacher.
    ///   3. For the six same-spelling pairs judged strictly (tayo stand-up /
    ///      tayo we, upo, bukas, basa, bati, pasok - AmbiguousWords), the
    ///      strict hybrid check below decides instead (JudgeAmbiguous). The
    ///      text that follows describes this branch's own fallback, which those
    ///      six no longer reach: a different stress IS a different word, so stress is
    ///      consulted - but only to REJECT, and only when the evidence is
    ///      clear: both audio judges agree the stress is on the other syllable
    ///      (or, with no recording, the loudness readings are past every
    ///      reading Rachel's own delivery ever gave). A borderline,
    ///      unmeasurable or slightly different stress passes. A pair said
    ///      inside a sentence ("kain tayo") is the other word and fails.
    ///
    /// Never compares the player's voice with Rachel's.
    /// </summary>
    /// <summary>
    /// STRICT HYBRID CHECK for a word whose meaning depends on how it is
    /// pronounced (AmbiguousWords: tayô "stand up" / táyo "we", ...). The
    /// recognizer's letters have already matched; they say WHICH letters, not
    /// which word, so they never decide here on their own.
    ///
    /// Two independent audio judges, both measured six ways per answer:
    ///   T  PronunciationTemplate - the whole answer lined up with Teacher
    ///      Rachel's own recording and with the other pronunciation, compared
    ///      on speaker-normalised pitch, loudness and timing. The primary judge.
    ///   A  PronunciationAssessor (with RawStressAnalyzer as its second opinion)
    ///      - the answer cut into syllables, the stressed one compared with
    ///      its neighbours.
    ///
    ///   Correct      T says Rachel's pronunciation and A does not say the
    ///                other; or A says Rachel's on a word where A is trusted,
    ///                its second opinion agrees, and T does not say the other.
    ///   WrongStress  two judges say the other pronunciation and none says
    ///                Rachel's: T and A (or A's second opinion), or A and its
    ///                second opinion with T not saying Rachel's.
    ///   Unclear      anything else - the judges disagree, the recording is
    ///                unclear, the word came with other words around it, or
    ///                there is no recording. Asked again, never scored, NEVER
    ///                accepted on its letters.
    ///
    /// With only the recognizer's loudness readings (a phone that does not
    /// share its microphone), StressAnalyzer.JudgeBanded decides the same three
    /// ways from them.
    /// </summary>
    /// <summary>
    /// Cuts a long recording down to the word and CropMarginSeconds either
    /// side, before the judges measure it. A phone records the whole listening
    /// round (8 s and more on the audio bridge) while the word is under a
    /// second, and every judge measures its audio six times over: on a budget
    /// phone that was seconds of waiting. The judges find the word the same
    /// way (SyllableAligner.FindWord), so they still measure the same word.
    /// </summary>
    public static Evidence CropToWord(Evidence evidence)
    {
        if (evidence.audio == null || evidence.audioSampleRate <= 0) return evidence;
        float seconds = evidence.audio.Length / (float)evidence.audioSampleRate;
        if (seconds <= CropAboveSeconds) return evidence;
        float[] x16 = SpeechFeatures.Resample(evidence.audio, evidence.audioSampleRate, SpeechFeatures.SampleRate);
        int from, to;
        string why;
        if (!SyllableAligner.FindWord(SpeechFeatures.Of(x16, SpeechFeatures.SampleRate), out from, out to, out why)) return evidence;
        int margin = (int)(CropMarginSeconds * SpeechFeatures.SampleRate);
        int a = System.Math.Max(0, from * SpeechFeatures.Hop - margin);
        int b = System.Math.Min(x16.Length, (to + 1) * SpeechFeatures.Hop + margin);
        float[] cut = new float[b - a];
        System.Array.Copy(x16, a, cut, 0, cut.Length);
        evidence.audio = cut;
        evidence.audioSampleRate = SpeechFeatures.SampleRate;
        return evidence;
    }

    public const float CropAboveSeconds = 2.5f, CropMarginSeconds = 0.5f;

    private static Result JudgeAmbiguous(Result r, PronunciationChecker.WordContext context, Evidence evidence)
    {
        r.stressEnforced = true;
        if (context != PronunciationChecker.WordContext.Alone)
        {
            r.outcome = Outcome.Unclear;
            r.reason = "said with other words around it - say " + r.pattern.Spoken + " on its own";
            return r;
        }

        evidence = CropToWord(evidence);
        bool hasAudio = evidence.audio != null && evidence.audio.Length > 0 && evidence.audioSampleRate > 0;
        bool hasLevels = evidence.levels != null && evidence.levels.Count > 0;

        if (hasAudio)
        {
            PronunciationTemplate.Result t = PronunciationTemplate.Assess(evidence.audio, evidence.audioSampleRate, r.pattern.Word);
            PronunciationAssessor.Assessment a = PronunciationAssessor.Assess(evidence.audio, evidence.audioSampleRate, r.pattern);
            StressAnalyzer.Judgement second = RawStressAnalyzer.Judge(evidence.audio, evidence.audioSampleRate, r.pattern, 1f);
            r.template = t;
            r.assessment = a;
            r.secondOpinion = second;

            PronunciationModel.Model learned = useLearnedModel ? PronunciationModel.For(r.pattern.Word) : null;
            PronunciationModel.Result m = learned != null
                ? PronunciationModel.Assess(evidence.audio, evidence.audioSampleRate, r.pattern, learned)
                : new PronunciationModel.Result { verdict = PronunciationModel.Verdict.Uncertain, pLow = float.NaN, pHigh = float.NaN, reason = "no trained model" };
            r.model = m;

            if (learned != null && (m.measured > 0 || t.measured > 0 || a.measured > 0 || !hasLevels))
                return DecideWithModel(r, t, a, second, m);

            if (t.measured > 0 || a.measured > 0 || !hasLevels)
            {
                r.source = StressSource.RawAudio;
                r.stressChecked = t.measured > 0 || a.measured > 0;

                bool tRight = t.verdict == PronunciationTemplate.Verdict.Matches;
                bool tOther = t.verdict == PronunciationTemplate.Verdict.Other;
                bool aRight = a.verdict == PronunciationAssessor.Verdict.Matches;
                bool aOther = a.verdict == PronunciationAssessor.Verdict.WrongStress;
                bool secondRight = second.verdict == StressAnalyzer.Verdict.Matches;
                bool secondOther = second.verdict == StressAnalyzer.Verdict.WrongStress;

                bool right = (tRight && !aOther) || (aRight && a.referenceTrusted && secondRight && !tOther);
                // A refusal needs two judges. Tried on the group's real labelled
                // voices, the template alone called 3 of 8 correct answers the
                // other word; with only Rachel-derived references it cannot
                // yet tell "another speaker" from "another stress" by itself.
                bool other = (tOther && (aOther || secondOther) && !aRight) || (aOther && secondOther && !tRight);

                r.stress = new StressAnalyzer.Judgement
                {
                    verdict = right && !other ? StressAnalyzer.Verdict.Matches
                            : other && !right ? StressAnalyzer.Verdict.WrongStress
                            : r.stressChecked ? StressAnalyzer.Verdict.Borderline : StressAnalyzer.Verdict.Unmeasurable,
                    measured = t.low,
                    reference = float.NaN,
                    moved = float.NaN,
                    threshold = PronunciationTemplate.For(r.pattern.Word) != null ? PronunciationTemplate.For(r.pattern.Word).acceptFrom : float.NaN,
                    enforced = true,
                    reason = t.reason,
                };
                string both = "template: " + t.verdict + " - " + t.reason + "; syllables: " + a.verdict + " - " + a.reason +
                              "; second: " + second.verdict;
                if (right && !other) { r.outcome = Outcome.Correct; r.reason = "said like Rachel's " + r.pattern.Spoken + " - " + both; }
                else if (other && !right) { r.outcome = Outcome.WrongStress; r.reason = "said like the other word - " + both; }
                else { r.outcome = Outcome.Unclear; r.reason = "not clear enough which word was said - " + both; }
                return r;
            }
        }

        if (hasLevels)
        {
            StressAnalyzer.Judgement j = StressAnalyzer.JudgeBanded(evidence.levels, evidence.levelIntervalMs, evidence.levelScale, r.pattern);
            r.source = StressSource.VoiceLevels;
            r.stress = j;
            r.stressChecked = j.verdict != StressAnalyzer.Verdict.Unmeasurable;
            r.reason = j.reason + " [loudness readings]";
            r.outcome = j.verdict == StressAnalyzer.Verdict.Matches ? Outcome.Correct
                      : j.verdict == StressAnalyzer.Verdict.WrongStress ? Outcome.WrongStress
                      : Outcome.Unclear;
            return r;
        }

        r.outcome = Outcome.Unclear;
        r.reason = "no audio to tell " + r.pattern.Spoken + " from the other word - say it again";
        return r;
    }

    /// <summary>
    /// The trained classifier decides; the older judges, built from Rachel's
    /// voice alone, can only turn a decision into "ask again", never make one:
    ///
    ///   Correct      the classifier says the taught pronunciation, and no
    ///                two older judges (template + a syllable judge, or both
    ///                syllable judges) say the other.
    ///   WrongStress  the classifier says the other pronunciation, and neither
    ///                the template nor a trusted syllable judge says Rachel's.
    ///   Unclear      everything else, including no clear classifier verdict -
    ///                unless the classifier is unsure and all THREE older
    ///                judges agree, which then decides.
    ///
    /// The veto is lopsided on purpose: tolerance for a child comes first, so
    /// one older judge is enough to stop a refusal, but it takes two to stop
    /// an acceptance.
    /// </summary>
    private static Result DecideWithModel(Result r, PronunciationTemplate.Result t, PronunciationAssessor.Assessment a,
                                          StressAnalyzer.Judgement second, PronunciationModel.Result m)
    {
        r.source = StressSource.RawAudio;
        r.stressChecked = m.measured > 0 || t.measured > 0 || a.measured > 0;
        r.decidedByModel = true;

        bool tRight = t.verdict == PronunciationTemplate.Verdict.Matches;
        bool tOther = t.verdict == PronunciationTemplate.Verdict.Other;
        bool aRight = a.verdict == PronunciationAssessor.Verdict.Matches;
        bool aOther = a.verdict == PronunciationAssessor.Verdict.WrongStress;
        bool secondOther = second.verdict == StressAnalyzer.Verdict.WrongStress;

        bool secondRight = second.verdict == StressAnalyzer.Verdict.Matches;
        bool unsure = m.verdict == PronunciationModel.Verdict.Uncertain;

        // Two older judges saying "the other word" stop an acceptance: the
        // template with either syllable judge, or both syllable judges.
        bool right = m.verdict == PronunciationModel.Verdict.Correct && !(tOther && (aOther || secondOther)) && !(aOther && secondOther);
        bool other = m.verdict == PronunciationModel.Verdict.Incorrect && !tRight && !(aRight && a.referenceTrusted);
        // When the classifier cannot tell, all THREE older judges agreeing
        // still decides (section 2a needed only two).
        if (unsure && tRight && aRight && secondRight) right = true;
        if (unsure && tOther && aOther && secondOther) other = true;

        r.stress = new StressAnalyzer.Judgement
        {
            verdict = right ? StressAnalyzer.Verdict.Matches
                    : other ? StressAnalyzer.Verdict.WrongStress
                    : r.stressChecked ? StressAnalyzer.Verdict.Borderline : StressAnalyzer.Verdict.Unmeasurable,
            measured = m.pLow,
            reference = float.NaN,
            moved = float.NaN,
            threshold = PronunciationModel.For(r.pattern.Word).acceptFrom,
            enforced = true,
            reason = m.reason,
        };
        string all = "model: " + m.verdict + " - " + m.reason + " [" + PronunciationModel.Describe(m.features) + "]" +
                     "; template: " + t.verdict + "; syllables: " + a.verdict + "; second: " + second.verdict;
        if (right) { r.outcome = Outcome.Correct; r.reason = "said like " + r.pattern.Spoken + " - " + all; }
        else if (other) { r.outcome = Outcome.WrongStress; r.reason = "said like the other word - " + all; }
        else
        {
            r.outcome = Outcome.Unclear;
            r.reason = (m.verdict == PronunciationModel.Verdict.Uncertain ? "not clear enough which word was said"
                        : "the model's verdict was not confirmed") + " - " + all;
        }
        return r;
    }

    private static Result JudgeBalanced(string spokenText, string targetWord, string[] acceptedVariants, Evidence evidence)
    {
        Result r = new Result();
        r.pattern = StressPattern.For(targetWord);
        r.stress.measured = float.NaN;

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
            r.reason = "word matched (no stress to check)";
            return r;
        }
        if (context == PronunciationChecker.WordContext.InSentence && r.pattern.IsMinimalPair)
        {
            r.outcome = Outcome.Wrong;
            r.reason = "said inside a sentence, where it means something else";
            return r;
        }
        // A word whose meaning depends on its pronunciation gets the strict
        // hybrid check; every other word stays balanced.
        if (AmbiguousWords.IsAmbiguous(targetWord)) return JudgeAmbiguous(r, context, evidence);

        // Measure the stress whenever the answer is the word on its own, for
        // the log - and, for a pair, to catch a clearly different word.
        bool alone = context == PronunciationChecker.WordContext.Alone;
        bool hasAudio = evidence.audio != null && evidence.audio.Length > 0 && evidence.audioSampleRate > 0;
        bool hasLevels = evidence.levels != null && evidence.levels.Count > 0;
        bool clearlyOtherWord = false;
        string measured = "stress not measured";

        if (alone && hasAudio)
        {
            PronunciationAssessor.Assessment a = PronunciationAssessor.Assess(evidence.audio, evidence.audioSampleRate, r.pattern);
            StressAnalyzer.Judgement second = RawStressAnalyzer.Judge(evidence.audio, evidence.audioSampleRate, r.pattern, 1f);
            r.assessment = a;
            r.secondOpinion = second;
            if (a.measured > 0 || !hasLevels)
            {
                r.source = StressSource.RawAudio;
                r.stressChecked = a.measured > 0;
                r.stress = new StressAnalyzer.Judgement
                {
                    verdict = a.verdict == PronunciationAssessor.Verdict.Matches ? StressAnalyzer.Verdict.Matches
                            : a.verdict == PronunciationAssessor.Verdict.WrongStress ? StressAnalyzer.Verdict.WrongStress
                            : a.measured > 0 ? StressAnalyzer.Verdict.Borderline : StressAnalyzer.Verdict.Unmeasurable,
                    measured = a.position,
                    reference = 1f,
                    moved = -1f,
                    threshold = PronunciationReferences.For(r.pattern.Word).acceptFrom,
                    enforced = r.pattern.IsMinimalPair,
                    reason = a.reason,
                };
                clearlyOtherWord = a.verdict == PronunciationAssessor.Verdict.WrongStress &&
                                   second.verdict == StressAnalyzer.Verdict.WrongStress;
                measured = a.reason + "; second check: " + second.verdict;
                hasLevels = false;   // the recording decided; the readings are not needed
            }
        }
        if (alone && hasLevels)
        {
            StressAnalyzer.Judgement j = StressAnalyzer.JudgeBanded(
                evidence.levels, evidence.levelIntervalMs, evidence.levelScale, r.pattern);
            r.source = StressSource.VoiceLevels;
            r.stress = j;
            r.stressChecked = j.verdict != StressAnalyzer.Verdict.Unmeasurable;
            clearlyOtherWord = j.verdict == StressAnalyzer.Verdict.WrongStress;
            measured = j.reason + " [loudness readings]";
        }

        if (r.pattern.IsMinimalPair && clearlyOtherWord)
        {
            r.outcome = Outcome.WrongStress;
            r.stressEnforced = true;
            r.reason = "the stress makes it the other word - " + measured;
            return r;
        }

        r.outcome = Outcome.Correct;
        r.stressEnforced = false;
        r.stressUnverified = !r.stressChecked;
        r.reason = (r.pattern.IsMinimalPair
                       ? "word matched; stress not clearly the other word's"
                       : "word matched; stress measured for the log only (it does not change this word)") +
                   (context != PronunciationChecker.WordContext.Alone ? ", said with other words around it" : "") +
                   " - " + measured;
        return r;
    }

    /// <summary>Strict mode: see the class comment.</summary>
    private static Result JudgeStrict(string spokenText, string targetWord, string[] acceptedVariants, Evidence evidence)
    {
        Result r = new Result();
        r.pattern = StressPattern.For(targetWord);
        r.stress.measured = float.NaN;

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
            r.reason = "word matched (no stress to check)";
            return r;
        }
        if (context == PronunciationChecker.WordContext.InSentence)
        {
            if (r.pattern.IsMinimalPair)
            {
                r.outcome = Outcome.Wrong;
                r.reason = "said inside a sentence, where it means something else";
            }
            else
            {
                r.outcome = Outcome.Unclear;
                r.reason = "said inside a sentence, where its stress cannot be checked - say " + r.pattern.Spoken + " on its own";
            }
            return r;
        }
        if (context == PronunciationChecker.WordContext.WithFiller)
        {
            r.outcome = Outcome.Unclear;
            r.reason = "said with other words around it - say " + r.pattern.Spoken + " on its own";
            return r;
        }

        bool hasAudio = evidence.audio != null && evidence.audio.Length > 0 && evidence.audioSampleRate > 0;
        bool hasLevels = evidence.levels != null && evidence.levels.Count > 0;
        r.stressEnforced = true;   // strict mode acts on whatever it measures

        if (hasAudio)
        {
            PronunciationAssessor.Assessment a = PronunciationAssessor.Assess(evidence.audio, evidence.audioSampleRate, r.pattern);
            StressAnalyzer.Judgement second = RawStressAnalyzer.Judge(evidence.audio, evidence.audioSampleRate, r.pattern, 1f);
            r.assessment = a;
            r.secondOpinion = second;

            // A recording with nothing usable in it (the phone kept the
            // microphone for the recognizer) falls through to the readings.
            if (a.measured > 0 || !hasLevels)
            {
                r.source = StressSource.RawAudio;
                r.stressChecked = a.measured > 0;
                r.stress = new StressAnalyzer.Judgement
                {
                    verdict = a.verdict == PronunciationAssessor.Verdict.Matches ? StressAnalyzer.Verdict.Matches
                            : a.verdict == PronunciationAssessor.Verdict.WrongStress ? StressAnalyzer.Verdict.WrongStress
                            : a.measured > 0 ? StressAnalyzer.Verdict.Borderline : StressAnalyzer.Verdict.Unmeasurable,
                    measured = a.position,
                    reference = 1f,
                    moved = -1f,
                    threshold = PronunciationReferences.For(r.pattern.Word).acceptFrom,
                    enforced = a.referenceTrusted,
                    reason = a.reason,
                };
                Combine(ref r, a, second);
                return r;
            }
        }

        if (hasLevels)
        {
            StressAnalyzer.Judgement j = StressAnalyzer.JudgeBanded(
                evidence.levels, evidence.levelIntervalMs, evidence.levelScale, r.pattern);
            r.source = StressSource.VoiceLevels;
            r.stress = j;
            r.stressChecked = j.verdict != StressAnalyzer.Verdict.Unmeasurable;
            r.reason = j.reason + " [loudness readings]";
            r.outcome = j.verdict == StressAnalyzer.Verdict.Matches ? Outcome.Correct
                      : j.verdict == StressAnalyzer.Verdict.WrongStress ? Outcome.WrongStress
                      : Outcome.Unclear;
            return r;
        }

        r.outcome = Outcome.Unclear;
        r.reason = "no audio to check the stress of " + r.pattern.Spoken;
        return r;
    }

    /// <summary>The two audio judges together; see rule a of strict mode.</summary>
    private static void Combine(ref Result r, PronunciationAssessor.Assessment a, StressAnalyzer.Judgement second)
    {
        bool secondSaysRight = second.verdict == StressAnalyzer.Verdict.Matches;
        bool secondSaysWrong = second.verdict == StressAnalyzer.Verdict.WrongStress;
        string both = a.reason + "; second check: " + second.verdict + (second.enforced ? "" : " (its reference less certain)");

        if (a.verdict == PronunciationAssessor.Verdict.Matches)
        {
            if (a.referenceTrusted ? !(second.enforced && secondSaysWrong) : secondSaysRight)
            {
                r.outcome = Outcome.Correct;
                r.reason = both;
            }
            else
            {
                r.outcome = Outcome.Unclear;
                r.reason = "the two stress checks disagree - " + both;
            }
        }
        else if (a.verdict == PronunciationAssessor.Verdict.WrongStress)
        {
            if (a.referenceTrusted ? !(second.enforced && secondSaysRight) : secondSaysWrong)
            {
                r.outcome = Outcome.WrongStress;
                r.reason = both;
            }
            else
            {
                r.outcome = Outcome.Unclear;
                r.reason = "the two stress checks disagree - " + both;
            }
        }
        else
        {
            r.outcome = Outcome.Unclear;
            r.reason = a.reason;
        }
    }

    /// <summary>Lenient mode: the rules as they were before strict mode, unchanged.</summary>
    private static Result JudgeLenient(string spokenText, string targetWord, string[] acceptedVariants,
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
