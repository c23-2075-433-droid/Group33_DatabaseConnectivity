"""Figure 3.1 Spoken-Word Objective and Retry Flowchart (revised 22 Nov 2026).
Release v0.2.0. Laid out for the A4 portrait text area (1000 x 1400 units, aspect 0.71)."""
from svglib import SVG
X = 500; F = 15; L = 14
s = SVG(1000, 1400)
# top
s.term(X, 32, 300, 44, "Objective becomes live", size=F)
s.circle(690, 32, 17, "B", size=L); s.path([(673, 32), (652, 32)])
s.box(X, 92, 440, 44, "Teacher Rachel introduces the word", size=F)
s.path([(X, 54), (X, 68)])
s.box(X, 150, 440, 42, "Speaking turn opens (SPEAK NOW)", size=F)
s.path([(X, 114), (X, 127)])
# listening path
s.diamond(X, 240, 380, 104, "Six-word target, and the\naudio bridge works on\nthis phone (Android 13+)?", size=F)
s.path([(X, 171), (X, 186)])
s.box(165, 340, 280, 80, "Audio bridge records the\nanswer in memory and sends\nthe same audio to the recognizer", size=L)
s.box(835, 340, 280, 80, "UnitySpeechToText: the\nrecognizer returns text and\nloudness readings, no audio", size=L)
s.path([(310, 240), (165, 240), (165, 298)]); s.label(235, 224, "Yes", size=L)
s.path([(690, 240), (835, 240), (835, 298)]); s.label(765, 224, "No", size=L)
s.path([(165, 380), (165, 400), (X, 400), (X, 418)]); s.path([(835, 380), (835, 400), (X, 400)], arrow=False)
# recognised?
s.diamond(X, 465, 300, 90, "Recognizer\nreturned a word?", size=F)
s.box(835, 465, 280, 74, "Nothing recognised: not\nscored; Rachel prompts again\nor nudges after 10 s", kind="note", size=L)
s.path([(650, 465), (693, 465)]); s.label(670, 448, "No", size=L)
s.path([(975, 465), (990, 465)], arrow=False)
s.path([(X, 510), (X, 538)]); s.label(512, 524, "Yes", anchor="start", size=L)
# word identity
s.diamond(X, 585, 300, 90, "Text matches\nthe target word?", size=F)
s.box(165, 585, 280, 100, "**Wrong word: a miss**\nTRY AGAIN + incorrect sound;\nhint after 2 misses,\nreplay after 4", kind="bad", size=L)
s.path([(350, 585), (307, 585)]); s.label(330, 568, "No", size=L)
s.path([(25, 585), (10, 585)], arrow=False)
s.path([(X, 630), (X, 658)]); s.label(512, 644, "Yes", anchor="start", size=L)
# six words?
s.diamond(X, 705, 300, 90, "One of the six\nstress words?", size=F)
s.path([(650, 705), (723, 705)]); s.circle(742, 705, 17, "A", size=L)
s.label(768, 705, "No: the text match decides", anchor="start", size=L)
s.path([(X, 750), (X, 778)]); s.label(512, 764, "Yes", anchor="start", size=L)
# context
s.diamond(X, 825, 300, 90, "Said on\nits own?", size=F)
s.path([(350, 825), (165, 825), (165, 637)]); s.label(258, 808, "In a sentence", size=L)
s.path([(650, 825), (723, 825)]); s.circle(742, 825, 17, "U", size=L)
s.label(768, 825, "With filler (e.g. “po”)", anchor="start", size=L)
s.path([(X, 870), (X, 898)]); s.label(512, 884, "Yes", anchor="start", size=L)
# audio?
s.diamond(X, 945, 300, 90, "Learner's audio\nrecorded?", size=F)
s.box(200, 1040, 340, 80, "On-device classifier\n(PronunciationModel) checked\nby the safety-net judges", kind="good", size=L)
s.box(800, 1040, 340, 80, "Fallback check on the\nrecognizer's loudness readings;\nno readings → uncertain", kind="warn", size=L)
s.path([(350, 945), (200, 945), (200, 998)]); s.label(275, 928, "Yes", size=L)
s.path([(650, 945), (800, 945), (800, 998)]); s.label(725, 928, "No", size=L)
s.path([(200, 1080), (200, 1098), (X, 1098), (X, 1113)]); s.path([(800, 1080), (800, 1098), (X, 1098)], arrow=False)
# decision
s.diamond(X, 1160, 300, 90, "Pronunciation\ndecision", size=F)
s.box(150, 1310, 260, 110, "**Wrong stress: a miss**\nTRY AGAIN + incorrect\nsound; Rachel: “Malapit\nna!” and the word again", kind="bad", size=L)
s.box(X, 1310, 280, 110, "**Correct**\nCORRECT! + Kylo's action;\nnext objective (B), or the\nscene summary (Fig. 3.2b)", kind="good", size=L)
s.box(850, 1310, 260, 110, "**Uncertain: not scored**\nTRY AGAIN, no incorrect\nsound; Rachel repeats\nthe instruction", kind="warn", size=L)
s.path([(350, 1160), (150, 1160), (150, 1253)]); s.label(255, 1143, "Other pronunciation", size=L)
s.path([(650, 1160), (850, 1160), (850, 1253)]); s.label(750, 1143, "Uncertain", size=L)
s.path([(X, 1205), (X, 1253)]); s.label(512, 1228, "Taught", anchor="start", size=L)
s.circle(395, 1222, 15, "A", size=13); s.path([(395, 1237), (395, 1253)])
s.circle(935, 1222, 15, "U", size=13); s.path([(935, 1237), (935, 1253)])
# retry loops
s.path([(20, 1310), (10, 1310), (10, 150), (278, 150)]); s.label(140, 136, "Retry", size=L)
s.path([(850, 1365), (850, 1385), (990, 1385), (990, 150), (722, 150)]); s.label(860, 136, "Retry", size=L)
s.save("../Figure_3_1_Spoken_Word_Objective_and_Retry.svg")
