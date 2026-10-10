"""Paint the journey road into the map background, along the real node path.

The map's nodes sit at fixed positions worked out in BuildLevelSelectScene.cs.
A road that comes out of an image generator cannot know where those are, so it
wanders off while the nodes sit somewhere else. This draws the road instead,
through the exact points the nodes will occupy, so the levels always follow it.

The road is not a flat ribbon laid on the grass. It is built up the way the
reference art does it - a raised earth bank either side, the bank's shadow
falling onto the track, then the worn tan surface with a paler line down the
middle where the walking has rubbed it thin. Drawing it as a run of circles
rather than one polyline is what lets the width breathe along its length, so it
reads as hand drawn instead of extruded.

Run from the repository root:  python3 Tools/draw_journey_road.py
Reads  Art/journey_map_bg_raw.png   (landscape, no road)
Writes Assets/Sprites/journey_map_bg.png
"""
from PIL import Image, ImageDraw, ImageFilter
import math
import random

SRC = 'Art/journey_map_bg_raw.png'
DST = 'Assets/Sprites/journey_map_bg.png'

# These mirror BuildLevelSelectScene.cs. Canvas is 1920x1080 with the origin
# at the centre, y up; the image has the origin top-left, y down.
STEPS = 20
PER_ROW = 5
FIRST_X = -740.0
COLUMN_GAP = 370.0
TOP_ROW_Y = 250.0
BOTTOM_ROW_Y = -320.0

SS = 2                 # supersample, then shrink: cheap anti-aliasing
SEED = 7               # fixed, so the pebbles land in the same place every run

# Half-widths, in canvas units, measured out from the centre line.
BANK_OUTER = 46.0      # dark lip where the bank meets the grass
BANK_INNER = 38.0      # sunlit top of the bank
ROAD_HALF = 27.0       # the track itself
WORN_HALF = 14.0       # the paler strip worn down the middle

BANK_DARK = (104, 74, 42)
BANK_LIGHT = (150, 111, 66)
ROAD = (212, 178, 123)
ROAD_WORN = (230, 203, 155)
PEBBLE = (191, 157, 107)

SHADOW = (74, 52, 30)
SHADOW_ALPHA = 92
SHADOW_DROP = (5.0, 7.0)   # bank shadow falls down and to the right


def node_positions():
    rows = math.ceil(STEPS / PER_ROW)
    pts = []
    for i in range(STEPS):
        row, col = divmod(i, PER_ROW)
        if row % 2 == 1:
            col = PER_ROW - 1 - col
        y = TOP_ROW_Y + (BOTTOM_ROW_Y - TOP_ROW_Y) * (row / (rows - 1))
        x = FIRST_X + col * COLUMN_GAP
        # Matches PositionOf() in BuildLevelSelectScene.cs. A plain grid of
        # rows reads as a running track; this makes the road wander like a
        # lane. Change one and you must change the other.
        x += 26.0 * math.sin(i * 1.7)
        y += 34.0 * math.sin(i * 0.9 + 1.3)
        pts.append((x, y))
    return pts


def catmull_rom(points, per_segment=40):
    """A smooth curve through every point, not merely near them."""
    p = [points[0]] + list(points) + [points[-1]]
    out = []
    for i in range(len(p) - 3):
        p0, p1, p2, p3 = p[i], p[i + 1], p[i + 2], p[i + 3]
        for s in range(per_segment):
            t = s / per_segment
            t2, t3 = t * t, t * t * t
            out.append((
                0.5 * ((2 * p1[0]) + (-p0[0] + p2[0]) * t
                       + (2 * p0[0] - 5 * p1[0] + 4 * p2[0] - p3[0]) * t2
                       + (-p0[0] + 3 * p1[0] - 3 * p2[0] + p3[0]) * t3),
                0.5 * ((2 * p1[1]) + (-p0[1] + p2[1]) * t
                       + (2 * p0[1] - 5 * p1[1] + 4 * p2[1] - p3[1]) * t2
                       + (-p0[1] + 3 * p1[1] - 3 * p2[1] + p3[1]) * t3)))
    out.append(points[-1])
    return out


def breathe(t):
    """A slow swell along the road, so no two stretches are the same width."""
    return 1.0 + 0.11 * math.sin(t * 7.3) + 0.05 * math.sin(t * 17.1 + 2.0)


def stamp(size, curve, half_width, colour, offset=(0.0, 0.0), scale=1.0):
    """Lay a band of the given half-width along the curve, on its own layer.

    Each layer is stamped opaque and faded afterwards. Stamping translucent
    circles directly would pile the alpha up wherever they overlap, which is
    everywhere, and the band would come out mottled.
    """
    layer = Image.new('RGBA', size, (0, 0, 0, 0))
    d = ImageDraw.Draw(layer)
    n = len(curve)
    for i, (x, y) in enumerate(curve):
        r = half_width * breathe(i / n) * scale
        cx, cy = x + offset[0] * scale, y + offset[1] * scale
        d.ellipse([cx - r, cy - r, cx + r, cy + r], fill=colour + (255,))
    return layer


def fade(layer, alpha):
    a = layer.getchannel('A').point(lambda v: v * alpha // 255)
    layer.putalpha(a)
    return layer


def pebbles(size, curve, scale):
    """Loose stones and scuffs, kept inside the track."""
    rng = random.Random(SEED)
    layer = Image.new('RGBA', size, (0, 0, 0, 0))
    d = ImageDraw.Draw(layer)
    n = len(curve)
    for i in range(0, n - 1, 9):
        x, y = curve[i]
        nx, ny = curve[i + 1]
        # unit normal to the road, so scuffs sit across it rather than anywhere
        dx, dy = nx - x, ny - y
        length = math.hypot(dx, dy) or 1.0
        px, py = -dy / length, dx / length
        for _ in range(rng.randint(0, 2)):
            off = rng.uniform(-0.78, 0.78) * ROAD_HALF * scale
            r = rng.uniform(1.6, 4.2) * scale
            cx, cy = x + px * off, y + py * off
            d.ellipse([cx - r, cy - r * 0.7, cx + r, cy + r * 0.7],
                      fill=PEBBLE + (rng.randint(110, 185),))
    return layer


def main():
    bg = Image.open(SRC).convert('RGBA')
    W, H = bg.size
    size = (W * SS, H * SS)
    # canvas units -> supersampled pixels
    scale = (W * SS) / 1920.0

    def to_px(p):
        return ((p[0] + 960.0) / 1920.0 * W * SS, (540.0 - p[1]) / 1080.0 * H * SS)

    curve = [to_px(p) for p in catmull_rom(node_positions())]

    # Outside in: bank, its shadow on the track, the track, the worn middle.
    layers = [
        stamp(size, curve, BANK_OUTER, BANK_DARK, scale=scale),
        stamp(size, curve, BANK_INNER, BANK_LIGHT, scale=scale),
        fade(stamp(size, curve, ROAD_HALF + 7.0, SHADOW,
                   offset=SHADOW_DROP, scale=scale), SHADOW_ALPHA),
        stamp(size, curve, ROAD_HALF, ROAD, scale=scale),
        fade(stamp(size, curve, WORN_HALF, ROAD_WORN, scale=scale), 150),
        pebbles(size, curve, scale),
    ]

    road = Image.new('RGBA', size, (0, 0, 0, 0))
    for layer in layers:
        road.alpha_composite(layer)

    road = road.resize((W, H), Image.LANCZOS)
    road = road.filter(ImageFilter.GaussianBlur(0.6))   # settle it onto the grass

    bg.alpha_composite(road)
    bg.convert('RGB').save(DST)
    print('wrote %s  (%dx%d, road through %d nodes)' % (DST, W, H, STEPS))


if __name__ == '__main__':
    main()
