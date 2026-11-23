"""Figure 3.2b Scene Progression and Level Completion (v0.2.0), print edition.
Same steps as the figure first placed in the paper, redrawn without the title baked into the
image (the caption carries it). 1000 x 600 units shown 460 pt wide: 18-unit text prints at 8.3 pt."""
from svglib import SVG
F = 18
s = SVG(1000, 600)
X = 470
s.box(X, 50, 440, 66, "Choose an unlocked scene\non the journey map", kind="start", size=F)
s.box(X, 148, 440, 48, "Complete the spoken objectives", size=F)
s.path([(X, 83), (X, 122)])
s.diamond(X, 250, 440, 100, "Fifth scene of the level?", size=F)
s.path([(X, 172), (X, 198)])
L, R = 235, 735
s.box(L, 372, 420, 70, "Show the scene summary\nwith the whole-level totals", size=F)
s.box(L, 466, 420, 70, "Record the level locally: points,\nwords and progression", kind="good", size=F)
s.box(L, 560, 420, 56, "Attempt the online level-record submission", kind="net", size=F)
s.box(R, 372, 420, 70, "Show the scene summary;\nsave the scene stars on the device", size=F)
s.box(R, 466, 420, 70, "Continue to the next scene;\ncompletion recorded in the transition", size=F)
s.path([(250, 250), (L, 250), (L, 335)]); s.label(205, 236, "Yes", size=F)
s.path([(690, 250), (R, 250), (R, 335)]); s.label(765, 236, "No", size=F)
s.path([(L, 407), (L, 429)]); s.path([(L, 501), (L, 530)])
s.path([(R, 407), (R, 429)])
s.path([(R + 210, 466), (985, 466), (985, 148), (692, 148)]); s.label(972, 250, "Next\nscene", anchor="end", size=F)
s.save("../Figure_3_2b_Scene_Progression_and_Level_Completion.svg", display_pt=460)
