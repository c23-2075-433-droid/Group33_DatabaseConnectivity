"""Figures 3.6-3.9: wireframes of the four main screens of v0.2.0, for Section 3.5.2.

Low-fidelity on purpose: boxes and labels, no artwork, so they read as layouts
next to the screenshots in 3.4.4. Positions come from the scene builders
(BuildMainMenuScene, ProfileUIBuilder, BuildLevelSelectScene,
SceneResultPanelBuilder: Unity canvas 1920 x 1080, origin at the centre) halved
onto a 960 x 540 screen; the gameplay screen is taken from the v0.2.0 device
screenshots. Dashed outline = decoration, placeholder, or shown only on a tap.

Shown 484 pt wide in the paper, so 16-unit text prints at 8 pt."""
from svglib import SVG, FONT
from xml.sax.saxutils import escape

INK = "#1f2937"; EDGE = "#374151"; SCREEN = "#f3f4f6"; FILLB = "#ffffff"; SOFT = "#e5e7eb"
F = 16          # every label
DISPLAY = 484


def u(x, y):
    """Unity canvas point (1920 x 1080, centre origin) -> 960 x 540 screen."""
    return 480 + x / 2, 270 - y / 2


def screen(s, x=0, y=0, w=960, h=540, label=None):
    s.add(f'<rect x="{x+2}" y="{y+2}" width="{w-4}" height="{h-4}" rx="10" fill="{SCREEN}" stroke="{EDGE}" stroke-width="2.5"/>')
    if label:
        s.text(x + w - 14, y + h - 18, label, F, color="#6b7280", anchor="end", italic=True)


def wbox(s, cx, cy, w, h, lines, dash=False, fill=FILLB, bold_first=False, r=6, size=F):
    d = ' stroke-dasharray="7 5"' if dash else ''
    s.add(f'<rect x="{cx-w/2:.1f}" y="{cy-h/2:.1f}" width="{w:.1f}" height="{h:.1f}" rx="{r}" fill="{fill}" stroke="{EDGE}" stroke-width="2"{d}/>')
    if lines:
        if isinstance(lines, str): lines = lines.split("\n")
        if bold_first: lines = ["**" + lines[0] + "**"] + lines[1:]
        from svglib import _fit
        s.text(cx, cy, lines, size, color=INK, lh=1.25)
        _fit(lines, size, w - 12, "wireframe box")


def circle(s, cx, cy, r, label="", dash=False):
    d = ' stroke-dasharray="6 4"' if dash else ''
    s.add(f'<circle cx="{cx}" cy="{cy}" r="{r}" fill="{FILLB}" stroke="{EDGE}" stroke-width="2"{d}/>')
    if label: s.text(cx, cy, label, F, color=INK)


def note(s, x, y, text, anchor="start"):
    s.text(x, y, text, F, color="#4b5563", anchor=anchor, italic=True)


# ---- Figure 3.6 Main menu ---------------------------------------------------------
s = SVG(960, 540)
screen(s, label="Classroom background")
tx, ty = u(-361.9, 232.8)
wbox(s, tx, ty, 455, 200, "SALINLAHI title\n(logo and subtitle)", fill=SOFT)
for r, name in enumerate(["ADVENTURE", "MINI GAME", "SETTINGS", "ABOUT"]):
    bx, by = u(499.2, 204.5 - 194.2 * r)
    wbox(s, bx, by, 240, 72 if r else 72, name if r != 1 else "MINI GAME\n(not wired)", dash=(r == 1))
hx, hy = u(707, 432)
wbox(s, hx - 12, hy, 230, 58, "Nickname plate\n(who is playing)")
bkx, bky = u(-754.6, -364.3)
wbox(s, bkx, bky - 8, 116, 74, "Books\n(decoration)", dash=True)
# what the plate opens
wbox(s, 468, 30, 410, 34, "Plate tap: CHANGE PROFILE, LEADERBOARDS", dash=True)
s.path([(673, 30), (704, 30)], dash=True)
s.save("../Figure_3_6_Main_Menu_Wireframe.svg", display_pt=DISPLAY)

# ---- Figure 3.7 Journey map: the two boards --------------------------------------------
s = SVG(960, 300)
SW, SH = 470, 264
k = SW / 1920
def m(x, y, ox):
    return ox + SW / 2 + x * k, 18 + SH / 2 - y * k
for i, ox in enumerate((5, 485)):
    screen(s, ox, 18, SW, SH)
    bx, by = m(-852, 429, ox); wbox(s, bx + 6, by + 6, 40, 32, "◀")
    mx, my = m(829, 429, ox); wbox(s, mx - 6, my + 6, 40, 32, "≡")
s.text(5 + SW / 2, 8, "1  Choose a place", F, "bold", color=INK)
s.text(485 + SW / 2, 8, "2  Choose a scene", F, "bold", color=INK)
# board 1: picker
tx, ty = m(-12, 278, 5); wbox(s, tx, ty + 4, 300, 34, "YOUR ADVENTURE AWAITS!", fill=SOFT)
for j, (name, locked) in enumerate([("HOME", False), ("SCHOOL", True), ("PARK", True)]):
    px, py = m((j - 1) * 612, -114, 5)
    wbox(s, px, py + 8, 140, 124, f"{name}\npicture" + ("\n(locked)" if locked else ""), dash=locked)
# board 2: one place's scenes
bx, by = m(-9, -27, 485); wbox(s, bx, by + 14, 330, 214, "", fill="#fafafa")
tx, ty = m(-23, 415, 485); wbox(s, tx, ty + 14, 200, 32, "HOME banner", fill=SOFT)
pitch = 247 * k * 1.25
for n in range(5):
    row, col = divmod(n, 4)
    cnt = 4 if row == 0 else 1
    cx = 485 + SW / 2 + (col - (cnt - 1) / 2) * pitch
    cy = 112 + row * 82
    wbox(s, cx, cy, 50, 42, str(n + 1) if n < 3 else "🔒" if False else str(n + 1), dash=(n >= 3))
    s.text(cx, cy + 34, "★★★" if n < 3 else "☆☆☆", F, color=INK)
note(s, 485 + SW / 2 + 85, 262, "dashed = locked", anchor="middle")
s.save("../Figure_3_7_Journey_Map_Wireframe.svg", display_pt=DISPLAY)

# ---- Figure 3.8 Gameplay scene -------------------------------------------------------------
s = SVG(960, 540)
screen(s, label="Illustrated scene")
wbox(s, 50, 40, 60, 40, "◀")
circle(s, 912, 40, 24, "≡")
circle(s, 330, 58, 36, "Rachel")
wbox(s, 520, 58, 300, 56, "Narration plaque:\nTeacher Rachel's line")
wbox(s, 180, 330, 90, 220, "Kylo\n(player\ncharacter)", fill=SOFT)
wbox(s, 560, 330, 150, 96, "Picture of\nthe object\nor action", fill=SOFT)
wbox(s, 560, 248, 130, 34, "Word label")
s.path([(560, 266), (560, 280)])
s.add(f'<path d="M 650 330 L 700 330" stroke="{EDGE}" stroke-width="2.5" marker-start="url(#arr)"/>')
note(s, 708, 330, "arrow")
wbox(s, 850, 440, 170, 86, "WORD TO SAY:\nLÁKAD\nWalk")
circle(s, 470, 440, 30, "Mic")
circle(s, 540, 440, 22, "↻")
note(s, 570, 440, "replay")
wbox(s, 470, 500, 250, 36, "Listening panel: SPEAK NOW …")
wbox(s, 110, 500, 180, 36, "BAHAY: LEVEL 1")
s.save("../Figure_3_8_Gameplay_Scene_Wireframe.svg", display_pt=DISPLAY)

# ---- Figure 3.9 Summary panel -------------------------------------------------------------
s = SVG(960, 540)
screen(s, label="The scene, out of focus")
PW, PH = 370, 439
px, py = 480, 270
wbox(s, px, py, PW, PH, "", fill=FILLB)
top = py - PH / 2
wbox(s, px - 5, top + 8, 240, 44, "LEVEL COMPLETE\nor TRY AGAIN!", fill=SOFT)
circle(s, px + 0.395 * PW, top + 0.105 * PH - 10, 20, "✕")
s.text(px, top + 96, "★ ★ ★ ★ ★", F + 6, color=INK)
s.text(px, top + 128, "up to five stars, from accuracy", F, color="#4b5563", italic=True)
s.text(px, top + 172, ["GREAT JOB, [NICKNAME]!", "You completed [scene and level]"], F, color=INK)
wbox(s, px, top + 242, PW - 50, 40, "TOTAL POINTS:  [n]", fill=SOFT)
wbox(s, px, top + 292, PW - 50, 40, "TOTAL ATTEMPTS:  [n]", fill=SOFT)
wbox(s, px - 86, top + 380, 140, 42, "REPLAY")
wbox(s, px + 86, top + 380, 140, 42, "CONTINUE")
s.save("../Figure_3_9_Summary_Panel_Wireframe.svg", display_pt=DISPLAY)
