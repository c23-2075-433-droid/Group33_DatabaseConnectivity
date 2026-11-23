"""Figure 3.1 Spoken-Word Objective and Retry Flowchart (v0.2.0), print edition.
820 x 1215 units, shown 425 pt wide in the paper (US Letter text area 484 x 678 pt),
so 15.5-unit text prints at about 8 pt."""
from svglib import SVG
X = 410; F = 16.5; L = 15.5
s = SVG(820, 1215)
LEFT, RIGHT = 8, 812          # the two retry loops
# top
s.term(X, 26, 300, 40, "Objective becomes live", size=F)
s.circle(595, 26, 16, "B", size=L); s.path([(579, 26), (562, 26)])
s.box(X, 80, 400, 40, "Teacher Rachel introduces the word", size=F)
s.path([(X, 46), (X, 58)])
s.box(X, 134, 400, 40, "Speaking turn opens (SPEAK NOW)", size=F)
s.path([(X, 100), (X, 112)])
# listening path
s.diamond(X, 216, 380, 104, "Six-word target, and\nthe audio bridge works\n(Android 13+)?", size=F)
s.path([(X, 154), (X, 162)])
s.box(140, 312, 240, 78, "Audio bridge records\nthe answer in memory\nand passes it on", size=L)
s.box(680, 312, 240, 78, "UnitySpeechToText:\ntext and loudness\nreadings, no audio", size=L)
s.path([(220, 216), (140, 216), (140, 271)]); s.label(180, 200, "Yes", size=L)
s.path([(600, 216), (680, 216), (680, 271)]); s.label(640, 200, "No", size=L)
s.path([(140, 351), (140, 366), (X, 366), (X, 378)]); s.path([(680, 351), (680, 366), (X, 366)], arrow=False)
# recognised?
s.diamond(X, 420, 290, 80, "Recognizer\nreturned a word?", size=F)
s.box(690, 420, 220, 80, "Nothing heard: not\nscored; Rachel asks\nagain, nudges at 10 s", kind="note", size=L)
s.path([(555, 420), (578, 420)]); s.label(566, 402, "No", size=L)
s.path([(800, 420), (RIGHT, 420)], arrow=False)
s.path([(X, 460), (X, 480)]); s.label(422, 470, "Yes", anchor="start", size=L)
# word identity
s.diamond(X, 522, 290, 80, "Text matches\nthe target word?", size=F)
s.box(130, 522, 220, 100, "**Wrong word: a miss**\nTRY AGAIN + incorrect\nsound; hint after 2\nmisses, replay after 4", kind="bad", size=L)
s.path([(265, 522), (242, 522)]); s.label(254, 504, "No", size=L)
s.path([(20, 522), (LEFT, 522)], arrow=False)
s.path([(X, 562), (X, 582)]); s.label(422, 572, "Yes", anchor="start", size=L)
# six words?
s.diamond(X, 624, 290, 80, "One of the six\nstress words?", size=F)
s.path([(555, 624), (578, 624)]); s.circle(595, 624, 16, "A", size=L)
s.label(618, 624, "No: the text\nmatch decides", anchor="start", size=L)
s.path([(X, 664), (X, 684)]); s.label(422, 674, "Yes", anchor="start", size=L)
# context
s.diamond(X, 726, 290, 80, "Said on\nits own?", size=F)
s.path([(265, 726), (130, 726), (130, 574)]); s.label(200, 708, "In a sentence", size=L)
s.path([(555, 726), (578, 726)]); s.circle(595, 726, 16, "U", size=L)
s.label(618, 726, "With filler\n(e.g. “po”)", anchor="start", size=L)
s.path([(X, 766), (X, 786)]); s.label(422, 776, "Yes", anchor="start", size=L)
# audio?
s.diamond(X, 828, 290, 80, "Learner's audio\nrecorded?", size=F)
s.box(160, 928, 280, 80, "On-device classifier\n(PronunciationModel), checked\nby the safety-net judges", kind="good", size=L)
s.box(660, 928, 280, 80, "Fallback check on the\nrecognizer's loudness\nreadings; none → uncertain", kind="warn", size=L)
s.path([(265, 828), (160, 828), (160, 886)]); s.label(212, 810, "Yes", size=L)
s.path([(555, 828), (660, 828), (660, 886)]); s.label(608, 810, "No", size=L)
s.path([(160, 968), (160, 982), (X, 982), (X, 994)]); s.path([(660, 968), (660, 982), (X, 982)], arrow=False)
# decision
s.diamond(X, 1036, 290, 80, "Pronunciation\ndecision", size=F)
s.box(145, 1150, 246, 106, "**Wrong stress: a miss**\nTRY AGAIN + incorrect\nsound; Rachel: “Malapit\nna!” and the word again", kind="bad", size=L)
s.box(X, 1150, 252, 106, "**Correct**\nCORRECT! + Kylo's action;\nnext objective (B), or the\nscene summary (Fig. 3.2b)", kind="good", size=L)
s.box(676, 1150, 246, 106, "**Uncertain: not scored**\nTRY AGAIN, no incorrect\nsound; Rachel repeats\nthe instruction", kind="warn", size=L)
s.path([(265, 1036), (145, 1036), (145, 1095)]); s.label(190, 1018, "Other way", size=L)
s.path([(555, 1036), (676, 1036), (676, 1095)]); s.label(630, 1018, "Uncertain", size=L)
s.path([(X, 1076), (X, 1095)]); s.label(422, 1083, "Taught", anchor="start", size=L)
s.circle(316, 1068, 13, "A", size=15.5); s.path([(316, 1081), (316, 1095)])
s.circle(760, 1068, 13, "U", size=15.5); s.path([(760, 1081), (760, 1095)])
# retry loops
s.path([(22, 1150), (LEFT, 1150), (LEFT, 134), (208, 134)]); s.label(110, 120, "Retry", size=L)
s.path([(676, 1203), (676, 1209), (RIGHT, 1209), (RIGHT, 134), (612, 134)]); s.label(710, 120, "Retry", size=L)
s.save("../Figure_3_1_Spoken_Word_Objective_and_Retry.svg", display_pt=425)
