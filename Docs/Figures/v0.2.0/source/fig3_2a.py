"""Figure 3.2a Application Launch and Nickname Validation Flowchart (v0.2.0).
Laid out for the A4 portrait text area (1000 x 960 units)."""
from svglib import SVG
F = 15; L = 14
s = SVG(1000, 960)
s.term(500, 40, 280, 48, "Application opens", size=F)
s.diamond(500, 150, 380, 120, "Does the playing profile\nalready have a nickname?", size=F)
s.path([(500, 64), (500, 88)])
s.box(850, 150, 200, 60, "Main menu", size=16)
s.path([(690, 150), (748, 150)]); s.label(719, 133, "Yes", size=L)
s.term(850, 40, 240, 48, "Journey map opens", kind="good", size=F)
s.path([(850, 120), (850, 66)]); s.label(862, 93, "ADVENTURE", anchor="start", size=L)
s.box(500, 300, 360, 80, "Nickname dialog\n(first launch; also used to\ncreate or rename a profile)", size=F)
s.path([(500, 210), (500, 258)]); s.label(512, 234, "No", anchor="start", size=L)
s.path([(680, 300), (800, 300), (800, 182)]); s.label(740, 283, "CANCEL", size=L)
s.diamond(500, 450, 360, 110, "Is the name used by\nanother profile on\nthis device?", size=F)
s.path([(500, 340), (500, 393)]); s.label(512, 367, "OKAY", anchor="start", size=L)
s.diamond(500, 610, 340, 120, "Online claim of the name\n(Supabase nickname_claims)", size=F)
s.path([(500, 505), (500, 548)]); s.label(512, 527, "No", anchor="start", size=L)
s.box(155, 610, 230, 120, "Name is taken (on this\nor another device): a free\nalternative is filled in;\nthe dialog stays open", kind="warn", size=L)
s.path([(320, 450), (155, 450), (155, 548)]); s.label(240, 433, "Yes", size=L)
s.path([(330, 610), (272, 610)]); s.label(301, 592, "Taken", size=13)
s.path([(40, 610), (15, 610), (15, 300), (318, 300)])
s.box(500, 770, 340, 64, "Nickname saved on the local\nprofile (players.json)", kind="good", size=F)
s.path([(500, 670), (500, 736)]); s.label(512, 703, "Claimed, or the check failed: accepted", anchor="start", size=L)
s.path([(670, 770), (900, 770), (900, 182)])
s.box(500, 890, 960, 84, None, kind="note", r=8)
s.text(500, 890, ["CANCEL leaves the profile unnamed (shown as “Bata”); the dialog opens again at the next launch.",
                  "The device check runs first, then the online claim, which sends the nickname and a random installation ID.",
                  "If Supabase cannot be reached, the name is accepted, so a child is never blocked from starting the game."], size=L)
s.save("../Figure_3_2a_Application_Launch_and_Nickname_Validation.svg")
