"""Figure 3.1 Spoken-Word Objective and Retry Flowchart (v0.2.0), print edition.
930 x 1050 units shown 484 x 546 pt, which fits under its two caption lines in the paper's
page body (about 615 pt below the university header); 15.5-unit text prints at 8 pt."""
from svglib import SVG
W, H = 930, 1050
X = 465; F = 16.5; L = 15.5
s = SVG(W, H)
LEFT, RIGHT = 6, 924          # the two retry loops
DW = 360                      # diamond width
# top
s.term(X, 24, 300, 38, "Objective becomes live", size=F)
s.circle(650, 24, 15, "B", size=L); s.path([(635, 24), (617, 24)])
s.box(X, 72, 420, 38, "Teacher Rachel introduces the word", size=F)
s.path([(X, 43), (X, 52)])
s.box(X, 120, 420, 38, "Speaking turn opens (SPEAK NOW)", size=F)
s.path([(X, 91), (X, 100)])
# listening path
s.diamond(X, 194, 440, 96, "Six-word target, and\nthe audio bridge works\n(Android 13+)?", size=F)
s.path([(X, 139), (X, 145)])
s.box(150, 278, 280, 58, "Audio bridge records the answer\nin memory and passes it on", size=L)
s.box(780, 278, 280, 58, "UnitySpeechToText: text and\nloudness readings, no audio", size=L)
s.path([(245, 194), (150, 194), (150, 248)]); s.label(196, 178, "Yes", size=L)
s.path([(685, 194), (780, 194), (780, 248)]); s.label(733, 178, "No", size=L)
s.path([(150, 307), (150, 318), (X, 318), (X, 327)]); s.path([(780, 307), (780, 318), (X, 318)], arrow=False)
# recognised?
s.diamond(X, 362, DW, 66, "Recognizer\nreturned a word?", size=F)
s.box(800, 372, 230, 76, "Nothing heard: not\nscored; Rachel asks\nagain, nudges at 10 s", kind="note", size=L)
s.path([(X + DW/2, 362), (685, 362)]); s.label(670, 345, "No", size=L)
s.path([(915, 372), (RIGHT, 372)], arrow=False)
s.path([(X, 395), (X, 415)]); s.label(477, 405, "Yes", anchor="start", size=L)
# word identity
s.diamond(X, 450, DW, 66, "Text matches\nthe target word?", size=F)
s.box(135, 450, 240, 98, "**Wrong word: a miss**\nTRY AGAIN + incorrect\nsound; hint after 2\nmisses, replay after 4", kind="bad", size=L)
s.path([(X - DW/2, 450), (257, 450)]); s.label(270, 433, "No", size=L)
s.path([(15, 450), (LEFT, 450)], arrow=False)
s.path([(X, 483), (X, 505)]); s.label(477, 494, "Yes", anchor="start", size=L)
# six words?
s.diamond(X, 540, DW, 66, "One of the six\nstress words?", size=F)
s.path([(X + DW/2, 540), (668, 540)]); s.circle(684, 540, 15, "A", size=L)
s.label(706, 540, "No: the text\nmatch decides", anchor="start", size=L)
s.path([(X, 573), (X, 595)]); s.label(477, 584, "Yes", anchor="start", size=L)
# context
s.diamond(X, 630, DW, 66, "Said on\nits own?", size=F)
s.path([(X - DW/2, 630), (135, 630), (135, 501)]); s.label(215, 613, "In a sentence", size=L)
s.path([(X + DW/2, 630), (668, 630)]); s.circle(684, 630, 15, "U", size=L)
s.label(706, 630, "With filler\n(e.g. “po”)", anchor="start", size=L)
s.path([(X, 663), (X, 685)]); s.label(477, 674, "Yes", anchor="start", size=L)
# audio?
s.diamond(X, 720, DW, 66, "Learner's audio\nrecorded?", size=F)
s.box(200, 802, 370, 58, "On-device classifier (PronunciationModel),\nchecked by the safety-net judges", kind="good", size=L)
s.box(730, 802, 370, 58, "Fallback check on the loudness\nreadings; none → uncertain", kind="warn", size=L)
s.path([(X - DW/2, 720), (200, 720), (200, 771)]); s.label(240, 703, "Yes", size=L)
s.path([(X + DW/2, 720), (730, 720), (730, 771)]); s.label(690, 703, "No", size=L)
s.path([(200, 831), (200, 842), (X, 842), (X, 851)]); s.path([(730, 831), (730, 842), (X, 842)], arrow=False)
# decision
s.diamond(X, 886, DW, 66, "Pronunciation\ndecision", size=F)
BY = 990; BH = 80
s.box(160, BY, 296, BH, "**Wrong stress: a miss**\nTRY AGAIN + incorrect sound;\nRachel: “Malapit na!”, the word", kind="bad", size=L)
s.box(X, BY, 296, BH, "**Correct**\nCORRECT! + Kylo's action; next\nobjective (B) or the scene summary", kind="good", size=L)
s.box(770, BY, 296, BH, "**Uncertain: not scored**\nTRY AGAIN, no incorrect sound;\nRachel repeats the instruction", kind="warn", size=L)
top = BY - BH / 2 - 2
s.path([(X - DW/2, 886), (160, 886), (160, top)]); s.label(215, 869, "Other way", size=L)
s.path([(X + DW/2, 886), (770, 886), (770, top)]); s.label(715, 869, "Uncertain", size=L)
s.path([(X, 919), (X, top)]); s.label(477, 933, "Taught", anchor="start", size=L)
s.circle(360, 922, 13, "A", size=L); s.path([(360, 935), (360, top)])
s.circle(870, 922, 13, "U", size=L); s.path([(870, 935), (870, top)])
# retry loops
s.path([(12, BY), (LEFT, BY), (LEFT, 120), (253, 120)]); s.label(120, 106, "Retry", size=L)
s.path([(770, BY + BH/2), (770, BY + BH/2 + 9), (RIGHT, BY + BH/2 + 9), (RIGHT, 120), (677, 120)]); s.label(800, 106, "Retry", size=L)
s.save("../Figure_3_1_Spoken_Word_Objective_and_Retry.svg", display_pt=484)
