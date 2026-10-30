"""Cut Teacher Rachel out of the HUD mockup, for the narration panel.

The panel art (Assets/Sprites/ui_narration_panel.png) has an empty branch
ring: the design draws Rachel inside it with the room showing through around
her, so she is a separate sprite rather than part of the plaque. The only
drawing of her that exists is the mockup, where she is already composited
over the bedroom - so she has to be lifted off it.

She cannot be lifted the way cut_hud_assets.py lifts the panels. There is no
flat backdrop behind her, and the two tricks that usually work both fail
here:

  * Flooding OUT from inside her leaks into the room: her hair is drawn in
    the same dark brown as her outline, so the flood never has a wall to
    stop against.
  * Flooding IN from the room leaks into her hair for the same reason, and
    the room inside the ring is sealed off by the branch, so there is
    nowhere outside to start from anyway.

What does separate cleanly is colour, for the two large pale areas - and
they are most of the problem:

  * the window is near-white and almost colourless
  * the floor and the bed are yellow, where green runs level with red, while
    skin and hair drop a third or more below it

Keying those two leaves the branch and the warm wall behind her, which are
nowhere near her, so a loose hand-traced silhouette finishes the job. That
division of labour is the point: the colour key does the precise work right
up against her hair and her pointing finger, and the polygon only has to be
roughly right.

Two details worth knowing:

  * Her teeth and the whites of her eyes are the same near-white as the
    window. They are kept by putting back any keyed-out patch that does not
    reach the rim of the circle - sealed inside her face, they never do.
  * The cut stops a little inside the branch (CLIP). The branch covers that
    band when the panel is drawn over her, and stopping short takes the last
    of the room's edge with it. Going much further starts cutting her chin
    and the top of her hair.

The sprite is written on the ring's own square rather than cropped to her,
so UITheme's RingCentreX / RingCentreY / RingDiameter place her with no
further measuring: drawn at the hollow's size, centred on the hollow, she
lands where she was drawn.

Writes:  Assets/Sprites/char_rachel_portrait.png

Run from the repository root:  python3 Tools/cut_rachel_portrait.py
"""
import os
import sys
from collections import deque

import numpy as np
from PIL import Image, ImageDraw, ImageFilter

SRC = os.path.expanduser("~/Downloads/Robo/1.png")
DST = "Assets/Sprites/char_rachel_portrait.png"

# The branch ring's inner circle in the mockup, in source pixels.
CX, CY, R = 594, 175, 118

# Stop this far inside it. See the note above.
CLIP = 0.95

# The two pale backgrounds, as colour tests.
WINDOW_LUM, WINDOW_CHROMA = 205, 70
YELLOW_LUM, YELLOW_GREEN, YELLOW_BLUE = 180, 0.80, 180

# Her silhouette, clockwise from the top of her head. Loose on purpose: the
# colour key handles everything that touches her, and this only has to keep
# the branch and the wall out.
SIL = [
    (566, 78), (594, 77), (618, 86), (636, 102), (646, 124), (649, 148),
    (648, 168), (642, 188), (636, 208), (632, 232), (631, 258), (631, 310),
    (505, 310), (505, 252), (498, 222), (487, 197), (485, 170), (494, 150),
    (504, 136), (516, 118), (530, 101), (546, 86),
]


def main():
    if not os.path.exists(SRC):
        sys.exit("Mockup not found at " + SRC)

    im = Image.open(SRC).convert("RGB")
    pad = 8
    x0, y0 = CX - R - pad, CY - R - pad
    box = (x0, y0, CX + R + pad, CY + R + pad)
    a = np.asarray(im.crop(box)).astype(int)
    h, w, _ = a.shape

    r, g, b = a[:, :, 0], a[:, :, 1], a[:, :, 2]
    lum = a.max(axis=2)
    chroma = lum - a.min(axis=2)

    window = (lum > WINDOW_LUM) & (chroma < WINDOW_CHROMA)
    yellow = (lum > YELLOW_LUM) & (g > r * YELLOW_GREEN) & (b < YELLOW_BLUE)
    background = window | yellow

    yy, xx = np.mgrid[0:h, 0:w]
    d2 = (xx - (CX - x0)) ** 2 + (yy - (CY - y0)) ** 2
    circle = d2 <= R * R
    rim = d2 >= (R - 3) ** 2

    remove = background & circle
    restored = keep_enclosed(remove, rim)

    polygon = trace(SIL, w, h, x0, y0)
    mask = circle & (d2 <= (R * CLIP) ** 2) & ~remove & polygon

    alpha = Image.fromarray((mask * 255).astype(np.uint8))
    alpha = alpha.filter(ImageFilter.GaussianBlur(0.8))

    out = im.convert("RGBA").crop(box)
    out.putalpha(alpha)
    out = out.crop((CX - R - x0, CY - R - y0, CX + R - x0, CY + R - y0))

    os.makedirs(os.path.dirname(DST), exist_ok=True)
    out.save(DST)
    print("%s  %dx%d, %.0f%% of the circle is her, %d patches put back "
          "(teeth and eyes)" % (DST, out.width, out.height,
                                100.0 * mask.sum() / circle.sum(), restored))
    print("\nNow run Tools > SALINLAHI > Refresh UI Theme in Unity, so the "
          "narration panel picks her up.")


def keep_enclosed(remove, rim):
    """
    Puts back any keyed-out patch that never reaches the rim of the circle.

    Her teeth and the whites of her eyes are the same near-white as the
    window and key out with it. What tells them apart is that they are
    sealed inside her face: the window runs to the edge, they cannot.
    Modifies remove in place; returns how many patches were put back.
    """
    h, w = remove.shape
    seen = np.zeros((h, w), bool)
    restored = 0

    for sy in range(h):
        for sx in range(w):
            if not remove[sy, sx] or seen[sy, sx]:
                continue
            patch = [(sy, sx)]
            seen[sy, sx] = True
            q = deque(patch)
            touches = False
            while q:
                y, x = q.popleft()
                if rim[y, x]:
                    touches = True
                for dy, dx in ((1, 0), (-1, 0), (0, 1), (0, -1)):
                    ny, nx = y + dy, x + dx
                    if 0 <= ny < h and 0 <= nx < w and remove[ny, nx] and not seen[ny, nx]:
                        seen[ny, nx] = True
                        patch.append((ny, nx))
                        q.append((ny, nx))
            if not touches:
                for y, x in patch:
                    remove[y, x] = False
                restored += 1
    return restored


def trace(points, w, h, x0, y0, scale=4):
    """The hand-traced silhouette, drawn large and scaled down so its edge is smooth."""
    big = Image.new("L", (w * scale, h * scale), 0)
    ImageDraw.Draw(big).polygon(
        [((x - x0) * scale, (y - y0) * scale) for x, y in points], fill=255)
    return np.asarray(big.resize((w, h), Image.LANCZOS)) > 127


if __name__ == "__main__":
    main()
