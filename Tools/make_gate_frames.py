"""Swing the school gate open, from the two backgrounds the scene already has.

THE PROBLEM. "Bukas" in Paaralan Scene 1 replaces paaralan_gate_background_
closed with paaralan_gate_background in a single frame. The gate is shut, then
it is open, and nothing moves between - which is the one thing the animation
brief rules out: "the gate must visibly move. Do not simply replace the closed
gate with an already-open gate."

WHY NO NEW ART IS NEEDED. The two backgrounds were generated separately, so
18% of their pixels differ and at first glance they look like two different
pictures. They are not. Differencing them and thresholding hard shows the
strong differences are confined to the gate panels - cols 476..1302 - and
everything else, the arch, the lettering, the trees, differs only along thin
edges, which is registration noise between two generations. The scene is the
same scene. So the panels can be taken from one image and moved against the
other.

HOW A 2D DOOR OPENS. A panel swinging away from the camera foreshortens
towards its hinge: its far edge stays put and its near edge travels towards
it, so the panel reads as narrower and narrower until it is edge-on. That is
a horizontal squash towards the hinge, which is the oldest trick in the book
and costs nothing. The left panel squashes towards the left jamb, the right
towards the right.

WHAT IS BEHIND. Each squashed panel is composited over the OPEN background,
so whatever the panel stops covering is the courtyard - already painted, in
the right perspective, correctly lit. The reveal comes free.

AND WHERE IT ENDS. At full open the squashed panels have vanished and what
remains is the open background itself, with its own panels drawn back against
the walls. The last frame of the animation IS paaralan_gate_background, so the
sequence lands exactly where the scene already expected to be.

Writes  Assets/Sprites/paaralan_gate_frame_1..3.png

Run from the repository root:  python Tools/make_gate_frames.py
"""
from PIL import Image

SHUT = "Assets/Sprites/paaralan_gate_background_closed.png"
OPEN = "Assets/Sprites/paaralan_gate_background.png"
OUT = "Assets/Sprites/paaralan_gate_frame_{}.png"

# THE JAMBS, which is where each panel is hinged and therefore the one
# measurement the whole effect rests on.
#
# These were wrong, and wrong in a way that showed. COL_RIGHT was 1302, taken
# from a loose difference of the two backgrounds that swept in registration
# noise either side of the gate. The right-hand jamb is actually at 1196:
# 1302 is 106px PAST it, inside the pillar. The panel was therefore anchored
# to a point on the wall, so as it swung it slid across the pillar and over
# the window beyond - reported from the game as the gate going through the
# wall and looking like a glitch.
#
# Measured again, properly, two independent ways that agree:
#
#   the shut gate's own blue runs from col 478 to col 1196
#   the pixels that actually CHANGE between shut and open, after opening and
#   closing the mask to throw away thin edges, span 502..1174 - inside those
#   jambs, as they must be, because the hinge ends barely move
#
# CENTRE follows from them rather than being carried over: the gate is 478 to
# 1196, so the two panels meet at 837. The old 889 split it 52px off-centre,
# which made the left panel wider than the right and the pair close unevenly.
COL_LEFT, COL_RIGHT = 478, 1196
ROW_TOP, ROW_BOTTOM = 395, 790
CENTRE = (COL_LEFT + COL_RIGHT) // 2

# HOW FAR EACH PANEL TRAVELS, and why the two differ.
#
# A panel swinging away from the camera stops when it is flat against its
# wall, NOT when it has vanished - and the open background shows exactly how
# wide each one is when it gets there: the left reads 96px, the right 80px,
# against a half-gate of 359. So the left ends 1 - 96/359 = 0.733 of the way
# squashed and the right 1 - 80/359 = 0.777. They are not the same number
# because the two panels are not drawn at the same angle.
#
# The old list ran to 0.88 for both, which squashed them NARROWER than the
# open background's own panels - so on the last step the gate stopped
# shrinking and jumped back out to full width. That was the second half of
# the glitch.
LEFT_END = 1.0 - 96.0 / ((COL_RIGHT - COL_LEFT) / 2.0)
RIGHT_END = 1.0 - 80.0 / ((COL_RIGHT - COL_LEFT) / 2.0)

# The SHAPE of the swing, as a fraction of each panel's own travel. Not
# evenly spaced: a swinging door covers most of its arc early and slows as it
# reaches the wall, and a door that opens at a constant rate reads as
# mechanical. Ending on 1.0 means the last drawn frame lands exactly on the
# open background's panel width, so handing over to it is seamless.
SWING = [0.35, 0.68, 1.0]


def main():
    shut = Image.open(SHUT).convert("RGBA")
    open_ = Image.open(OPEN).convert("RGBA")
    if shut.size != open_.size:
        raise SystemExit("the two backgrounds are different sizes: %s vs %s"
                         % (shut.size, open_.size))

    left = shut.crop((COL_LEFT, ROW_TOP, CENTRE, ROW_BOTTOM))
    right = shut.crop((CENTRE, ROW_TOP, COL_RIGHT, ROW_BOTTOM))
    h = ROW_BOTTOM - ROW_TOP

    for i, k in enumerate(SWING, start=1):
        frame = open_.copy()

        # Each panel on its own travel - see LEFT_END / RIGHT_END.
        tl = k * LEFT_END
        tr = k * RIGHT_END

        # Left panel: hinged at COL_LEFT, so its right edge travels left.
        lw = max(1, int(round(left.size[0] * (1.0 - tl))))
        frame.alpha_composite(left.resize((lw, h), Image.LANCZOS),
                              (COL_LEFT, ROW_TOP))

        # Right panel: hinged at COL_RIGHT, so its left edge travels right.
        rw = max(1, int(round(right.size[0] * (1.0 - tr))))
        frame.alpha_composite(right.resize((rw, h), Image.LANCZOS),
                              (COL_RIGHT - rw, ROW_TOP))

        path = OUT.format(i)
        frame.save(path)
        print("%s  left %d%% open (%dpx, hinge %d)  right %d%% open (%dpx, hinge %d)"
              % (path, int(tl * 100), lw, COL_LEFT,
                 int(tr * 100), rw, COL_RIGHT))


if __name__ == "__main__":
    main()
