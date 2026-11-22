"""Figure 3.5 Use Case Diagram (v0.2.0). A4 portrait layout (1000 x 1010 units).
UML: «include» points from the base use case to the included one; «extend» from the extension to the base."""
from svglib import SVG, LINE
s = SVG(1000, 1010)
F = 14
s.add('<rect x="150" y="40" width="735" height="950" rx="14" fill="#f8fafc" stroke="#4a6a8a" stroke-width="2"/>')
s.text(517, 66, "SALINLAHI (Unity application on the phone)", 16, "bold", color="#4a6a8a")
P = 330
prim = [(130, "Set or change a nickname"), (220, "Manage profiles\n(switch, create, rename, delete)"),
        (310, "View the shared leaderboard"), (400, "Browse the journey map"), (490, "Play a scene"),
        (580, "View the scene summary\nand words learned"), (670, "Replay a scene"), (760, "Adjust the settings"), (850, "View the About panel")]
s.actor(62, 490, "Bata\n(child)", size=F)
for y, t in prim:
    s.path([(86, 470), (172, y)], arrow=False, color="#4b5563", sw=1.5)
for y, t in prim:
    s.ellipse(P, y, 160, 36, t, size=F)
def dep(a, b, label, lx, ly, anchor="middle"):
    s.add(f'<path d="M {a[0]} {a[1]} L {b[0]} {b[1]}" fill="none" stroke="{LINE}" stroke-width="1.8" stroke-dasharray="7 5" marker-end="url(#open)"/>')
    s.label(lx, ly, label, size=12, anchor=anchor, weight="normal")
# includes and extends
dep((490, 130), (528, 130), "«include»", 509, 112)
dep((470, 208), (548, 152), "«include» (create, rename)", 552, 190, anchor="start")
dep((478, 300), (532, 262), "«include»", 520, 296, anchor="start")
dep((560, 386), (452, 470), "«extend» [5th scene]", 560, 432, anchor="start")
dep((490, 490), (528, 490), "«include»", 509, 472)
dep((645, 525), (602, 604), "«include»", 612, 560, anchor="end")
dep((715, 525), (760, 604), "«include»", 745, 560, anchor="start")
dep((632, 776), (592, 678), "«extend»\n[one of the six words]", 622, 716, anchor="start")
sec = [(690, 130, 160, 36, "Check nickname\navailability"), (690, 240, 160, 36, "Submit totals and\nload the leaderboard"),
       (690, 360, 160, 36, "Save the level record\n[after the fifth scene]"), (690, 490, 160, 36, "Say the Filipino word"),
       (575, 640, 100, 36, "Match the word\n(recognized text)"), (785, 640, 95, 36, "Recognise\nthe speech"),
       (650, 820, 115, 46, "Verify the stress\n(Tayo, Upo, Bukas,\nBasa, Bati, Pasok)")]
for x, y, rx, ry, t in sec:
    s.ellipse(x, y, rx, ry, t, kind="warn", size=F)
# external actors
s.actor(950, 250, "Supabase\nbackend", size=F)
s.actor(950, 640, "Speech\nservice\n(may be\nonline)", size=F)
for y in (130, 240, 360):
    s.path([(850, y), (926, 236)], arrow=False, color="#4b5563", sw=1.5)
s.path([(880, 640), (926, 628)], arrow=False, color="#4b5563", sw=1.5)
s.text(517, 935, ["Unshaded use cases are started by the child; shaded ones are pulled in by another use case.",
                  "The stress check runs on the device and extends word matching only for the six words.",
                  "MINI GAME is an unwired placeholder and is not a use case."], 13, color="#4b5563")
s.save("../Figure_3_5_Use_Case_Diagram.svg")
