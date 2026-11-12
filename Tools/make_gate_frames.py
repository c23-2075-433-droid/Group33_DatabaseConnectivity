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

# The panels, measured by differencing the two backgrounds and keeping only
# the strong differences. Cols from that bounding box; rows from where the
# difference is WIDE rather than a thin outline, which separates the panels
# from the arch above them.
COL_LEFT, COL_RIGHT = 476, 1302
ROW_TOP, ROW_BOTTOM = 395, 790
CENTRE = (COL_LEFT + COL_RIGHT) // 2

# How far open each frame is. Not evenly spaced: a swinging door covers most
# of its arc early and slows as it reaches the wall, and a door that opens at
# a constant rate reads as mechanical.
OPENNESS = [0.35, 0.68, 0.88]


def main():
    shut = Image.open(SHUT).convert("RGBA")
    open_ = Image.open(OPEN).convert("RGBA")
    if shut.size != open_.size:
        raise SystemExit("the two backgrounds are different sizes: %s vs %s"
                         % (shut.size, open_.size))

    left = shut.crop((COL_LEFT, ROW_TOP, CENTRE, ROW_BOTTOM))
    right = shut.crop((CENTRE, ROW_TOP, COL_RIGHT, ROW_BOTTOM))
    h = ROW_BOTTOM - ROW_TOP

    for i, t in enumerate(OPENNESS, start=1):
        frame = open_.copy()

        # Left panel: hinged at COL_LEFT, so its right edge travels left.
        lw = max(1, int(round(left.size[0] * (1.0 - t))))
        frame.alpha_composite(left.resize((lw, h), Image.LANCZOS),
                              (COL_LEFT, ROW_TOP))

        # Right panel: hinged at COL_RIGHT, so its left edge travels right.
        rw = max(1, int(round(right.size[0] * (1.0 - t))))
        frame.alpha_composite(right.resize((rw, h), Image.LANCZOS),
                              (COL_RIGHT - rw, ROW_TOP))

        path = OUT.format(i)
        frame.save(path)
        print("%s  %d%% open  (left panel %dpx, right %dpx)"
              % (path, int(t * 100), lw, rw))


if __name__ == "__main__":
    main()
