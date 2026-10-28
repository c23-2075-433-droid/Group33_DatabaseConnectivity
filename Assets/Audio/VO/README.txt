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

Prompt frames and feedback (see Docs/VoicePromptingDesign.md)
-------------------------------------------------------------
  rachel_frame_*.wav   the short line played straight before a word clip,
                       e.g. rachel_frame_sabihin.wav ("Sabihin mo:") joined
                       to word_lakad.wav, heard as "Sabihin mo: LAKAD".

  Record the frames in the same session, same mic and same distance as the
  word clips, or the join is audible. Give them a rising, unfinished
  intonation so they lead into the word. Trim the tail hard - 200 ms of air
  at the end of a frame is a stutter before every single prompt.

  rachel_feedback_*, rachel_hint_*, rachel_nudge_*, rachel_mic_blocked:
  keep under 0.8 s. They play over the start of Kylo's action.
