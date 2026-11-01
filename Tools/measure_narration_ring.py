"""Measure the opening in the narration plaque's branch ring.

Prints the three numbers UITheme uses to place Teacher Rachel's portrait:
RingCentreX, RingCentreY and RingDiameter, as fractions of the plaque sprite.

Why this exists, and why it does not measure a circle. The branch is drawn as
a ring, so the obvious thing is to fit the largest circle inside it. That is
what the old numbers were, and it is wrong in a way that is hard to see and
easy to blame on the art: the opening the twigs leave is NOT round. In the
current plaque it is 326 wide and 352 tall, and its middle sits lower than
the inscribed circle's. A portrait fitted to that circle therefore comes out
too small AND about 25 pixels too high, leaving a band of bare opening under
the chin. Every attempt to fix that by redrawing or zooming the portrait
fails, because nothing is being drawn in that band at all.

So this measures the OPENING - flood-filled from the middle of the ring, out
to wherever the branch stops it - and reports the circle that covers it. The
portrait is drawn behind the plaque, so whatever of it falls outside the
opening is hidden by the twigs; covering too much is free, covering too
little leaves a hole.

Run from the repository root:  python3 Tools/measure_narration_ring.py
"""
import sys
from collections import deque

import numpy as np
from PIL import Image

PANEL = "Assets/Sprites/ui_narration_panel.png"

# Somewhere inside the ring, to start the flood from. Any transparent pixel
# enclosed by the branch will do; this is the middle of the old circle.
SEED_X, SEED_Y = 0.1319, 0.4744

TRANSPARENT = 40


def main():
    try:
        panel = Image.open(PANEL).convert("RGBA")
    except FileNotFoundError:
        sys.exit("Not found: " + PANEL)

    w, h = panel.size
    clear = np.asarray(panel)[:, :, 3] < TRANSPARENT

    sx, sy = int(SEED_X * w), int(SEED_Y * h)
    if not clear[sy, sx]:
        sys.exit("The seed point is not inside the ring - the plaque art has moved.")

    opening = flood(clear, sy, sx)
    ys, xs = np.nonzero(opening)
    if opening.all():
        sys.exit("The flood escaped the ring; the branch has a gap in it.")

    x0, x1, y0, y1 = xs.min(), xs.max(), ys.min(), ys.max()
    cx, cy = (x0 + x1) / 2.0, (y0 + y1) / 2.0
    diameter = max(x1 - x0 + 1, y1 - y0 + 1)

    print("%s  %dx%d" % (PANEL, w, h))
    print("opening: %d x %d px, from (%d, %d) to (%d, %d)"
          % (x1 - x0 + 1, y1 - y0 + 1, x0, y0, x1, y1))
    print("\nPaste into Assets/Scripts/UITheme.cs:\n")
    print("    public const float RingCentreX = %.4ff;" % (cx / w))
    print("    public const float RingCentreY = %.4ff;" % (cy / h))
    print("    public const float RingDiameter = %.4ff;" % (float(diameter) / w))


def flood(clear, sy, sx):
    """Every transparent pixel reachable from the seed without crossing the branch."""
    h, w = clear.shape
    seen = np.zeros((h, w), bool)
    seen[sy, sx] = True
    q = deque([(sy, sx)])
    while q:
        y, x = q.popleft()
        for dy, dx in ((1, 0), (-1, 0), (0, 1), (0, -1)):
            ny, nx = y + dy, x + dx
            if 0 <= ny < h and 0 <= nx < w and clear[ny, nx] and not seen[ny, nx]:
                seen[ny, nx] = True
                q.append((ny, nx))
    return seen


if __name__ == "__main__":
    main()
