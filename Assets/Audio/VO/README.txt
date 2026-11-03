SALINLAHI — voice-over clips
============================

Drop recorded clips in this folder. The scene builders pick them up by name,
so nothing has to be assigned in the Inspector — and nothing is lost when a
scene is rebuilt.

Naming
------
  word_<word>.wav      the word itself, lowercase, no accents
                       e.g. word_bangon.wav, word_sipilyo.wav

  .mp3 and .ogg also work. The names match the File Name column of
  Docs/VoiceOverScript.csv.

After adding files
------------------
  Re-run the scene's builder (Tools > SALINLAHI > Build Bahay Scene N) and the
  clips attach themselves. Until then the replay button logs a warning and
  plays nothing, which is expected.

Recording notes
---------------
  Mono, 44.1 kHz, WAV. Trim the silence at both ends — these play the moment
  a prompt appears, and a half-second of dead air before each word is very
  noticeable when a child is waiting to repeat it.

  Say the word on its own, clearly and a little slowly, in the register you
  would use with a five-year-old. Rachel's lines are full sentences and can be
  warmer and faster.

Rachel's story lines (see Docs/VoicePromptingDesign.md)
-------------------------------------------------------
  Every word is introduced as part of Kylo's story, and each beat is its own
  file so each becomes its own subtitle card:

  rachel_<scene>_<word>_story.wav   A - what is happening right now
  rachel_word_<word>_teach.wav      B - what the word means, in full. Played
                                        the first time the word comes up.
  rachel_word_<word>_recall.wav     B' - the short reminder, every time after
  rachel_<scene>_<word>_ask.wav     C - the instruction
  rachel_<scene>_<word>_done.wav    D - praise, once the word is recognised

  <scene> is the script's name for a scene: l1_s1 is Chapter 1, Level 1.

  The _ask lines are written with a {word} placeholder saying where the word
  falls - "Say {word} to make him put on his clothes!". UNRECORDED, the game
  plays the two halves around word_<word>.wav. RECORDED, say the whole
  sentence INCLUDING the word, and it plays on its own instead.

  Read these the way you would read a picture book to a five-year-old: warm,
  unhurried, and finishing each sentence. They are not drilled, so they can
  be slower and more expressive than the word clips.

Prompt frames and feedback
--------------------------
  rachel_frame_*.wav   a fallback only, for a word whose _ask line has not
                       been written: rachel_frame_sabihin.wav ("Sabihin mo:")
                       joined to word_lakad.wav, heard as "Sabihin mo: LAKAD".
                       rachel_frame_ulitin.wav IS still in live use - it is
                       what the replay button and the 2-miss hint say.

  Record the frames in the same session, same mic and same distance as the
  word clips, or the join is audible. Give them a rising, unfinished
  intonation so they lead into the word. Trim the tail hard - 200 ms of air
  at the end of a frame is a stutter before every single prompt.

  rachel_feedback_*, rachel_hint_*, rachel_nudge_*, rachel_mic_blocked:
  keep under 1 s. They play over the start of Kylo's action.

Keeping the script honest
-------------------------
  Docs/VoiceOverScript.csv is the single source of truth for every file name
  above. After editing it, run:

      python3 -I Tools/check_voiceover_script.py

  It reads the word list straight out of the fifteen scenes and fails if a
  line is missing, too long for Rachel's panel, or refers to Kylo as "she".

  Then run Tools > SALINLAHI > Refresh Narrator Voice Bank so the game picks
  the changes up. Recording a clip is just dropping the file here and running
  that same menu item again.
