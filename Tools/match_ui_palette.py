"""Turn the restyled UI sheet's red wood into SALINLAHI's brown.

The generated sheet came back with its planks at hue 6 degrees - a wine red -
while the game's buttons sit at 21 degrees, a warm cinnamon brown. Asking the
image model to fix that cost two rounds and overshot into gold both times,
because "warmer" is not a number to it. This is a number.

Only red pixels are touched. The leaves sit near 110 degrees, the cream outline
is almost unsaturated, and the amber lettering is already past 30, so all three
are left exactly as they are - which is the point: nothing is re-drawn, so no
word can come back misspelled.

Run from the repository root:
    python3 Tools/match_ui_palette.py <in.png> <out.png>
"""
from PIL import Image
import numpy as np
import sys

TARGET_HUE = 21.2      # degrees, sampled from btn_play
SOURCE_HUE = 6.0       # where the sheet's planks actually sit
TAPER_TO = 20.0        # hues above this are already brown; leave them
MIN_SAT = 0.25         # below this it is the cream outline, not wood
VALUE_LIFT = 1.10      # the red is also a little darker than the target


def rgb_to_hsv(a):
    r, g, b = a[..., 0], a[..., 1], a[..., 2]
    mx, mn = a.max(-1), a.min(-1)
    d = mx - mn
    h = np.zeros_like(mx)
    nz = d > 1e-6
    rm, gm, bm = (mx == r) & nz, (mx == g) & nz, (mx == b) & nz
    h[rm] = ((g - b)[rm] / d[rm]) % 6.0
    h[gm] = ((b - r)[gm] / d[gm]) + 2.0
    h[bm] = ((r - g)[bm] / d[bm]) + 4.0
    h *= 60.0
    s = np.where(mx > 1e-6, d / np.maximum(mx, 1e-6), 0.0)
    return h, s, mx


def hsv_to_rgb(h, s, v):
    h = h % 360.0
    i = np.floor(h / 60.0).astype(int) % 6
    f = h / 60.0 - np.floor(h / 60.0)
    p, q, t = v * (1 - s), v * (1 - f * s), v * (1 - (1 - f) * s)
    out = np.zeros(h.shape + (3,))
    for k, (R, G, B) in enumerate([(v, t, p), (q, v, p), (p, v, t),
                                   (p, q, v), (t, p, v), (v, p, q)]):
        m = i == k
        out[m] = np.stack([R, G, B], -1)[m]
    return out


def main(src, dst):
    im = Image.open(src).convert('RGBA')
    a = np.asarray(im).astype(float) / 255.0
    h, s, v = rgb_to_hsv(a[..., :3])

    # Reds wrap past 360, so fold them below zero before comparing.
    hw = np.where(h > 300.0, h - 360.0, h)

    # Full strength on the reddest wood, fading out as it approaches the hue
    # the background planks already have. Without the taper the mid-browns
    # would be pushed past brown and the sheet would go orange.
    weight = np.clip((TAPER_TO - hw) / (TAPER_TO - SOURCE_HUE), 0.0, 1.0)
    weight[s < MIN_SAT] = 0.0
    weight[hw < -25.0] = 0.0          # magentas, if any, are not wood

    h2 = hw + (TARGET_HUE - SOURCE_HUE) * weight
    v2 = np.clip(v * (1.0 + (VALUE_LIFT - 1.0) * weight), 0.0, 1.0)

    out = a.copy()
    out[..., :3] = hsv_to_rgb(h2, s, v2)
    Image.fromarray(np.clip(out * 255.0, 0, 255).astype(np.uint8)).save(dst)

    moved = (weight > 0.01).mean() * 100.0
    print('wrote %s  (%d x %d, %.1f%% of pixels shifted)'
          % (dst, im.width, im.height, moved))


if __name__ == '__main__':
    main(sys.argv[1], sys.argv[2])
