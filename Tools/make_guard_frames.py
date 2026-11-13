"""Cut the three guard wave frames out of one generated strip.

WHAT THEY ARE. The school guard at the Paaralan gate raising his hand to wave
back when the child says "Bati": arms down, half raised, waving. "Bati" used
to flip him from char_guard_idle to char_guard in a single frame, so Kylo's
wave was drawn while the man he waved at popped.

WHY ALL THREE ARE GENERATED, rather than two existing sprites with one
in-between. char_guard_idle and char_guard look like the same drawing with
the arm moved, and they are not. Searching every offset for the best
alignment of their legs and torso tops out at 0.66 overlap - they are two
separate generations, so there is no arm to cut out and swing the way
Tools/make_gate_frames.py swings the gate panels.

That left a choice. Keeping the two existing sprites as the endpoints and
using one generated frame between them gives original -> generated ->
original, which pops twice; using all three generated frames gives one
consistent drawing that animates cleanly, at the cost of the guard being a
slightly flatter version of himself. The guard appears in this scene and
nowhere else, so the second was chosen deliberately.

MATCHING THE CANVAS, which is what keeps him from changing size.
CreateStanding in BuildPaaralanScene1 scales a sprite by
worldHeight / bounds.size.y - the CANVAS height, not the ink - and places it
centred, so its feet land on FloorY only if the ink fills the canvas the way
char_guard's does. So these are written at 551x1425 with the figure occupying
rows 3..1420, exactly like the sprites they replace.

WHERE THE ART CAME FROM. Art/guard_frames_raw.png is one Canva generation
from char_guard_idle. His own appearance was described from his own
reference - cap, moustache, epaulettes, belt - rather than carried over from
any other prompt, which is the mistake that cost a generation on Kylo's hair.

Writes  Assets/Sprites/guard_frame_1..3.png

Run from the repository root:  python Tools/make_guard_frames.py
"""
import os
import sys

from PIL import Image, ImageFilter
import numpy as np

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from fit_seated_pose import cut_background, split_columns          # noqa: E402


def split_rows(img):
    """Row spans of separate bands in a grid, found on the empty gaps.

    The mirror of fit_seated_pose.split_columns. A strip of three fits one
    row; six poses do not, and the generator lays them out as two rows of
    three, so the bands have to be found before the figures in them.
    """
    a = np.asarray(img.convert("RGB")).astype(int)
    ink = (a.min(axis=2) < 225).sum(axis=1)
    spans, start = [], None
    for y, v in enumerate(ink):
        if v > 2 and start is None:
            start = y
        if v <= 2 and start is not None:
            if y - start > 40:
                spans.append((start, y - 1))
            start = None
    if start is not None and len(ink) - start > 40:
        spans.append((start, len(ink) - 1))
    return spans


def bleed_edges(im, depth=16):
    """Push the figure's own colour out underneath its transparent rim.

    WHY THIS IS NEEDED. cut_background builds a HARD mask and then blurs it
    by half a pixel so the cut does not read as a stencil - but it never
    touches RGB. The pixels under that soft rim are still the ones it has
    just decided are background, which on these generations is WHITE. So the
    rim draws the silhouette blended towards white, and a pale fringe runs
    all the way round the figure.

    MEASURED, because it is easy to argue about and simple to check. Taking
    the mean luminance of the part-transparent edge pixels against the solid
    artwork within three pixels of them:

        Kylo, standing          edge  14 vs inside  81   ->  -67
        char_guard_idle         edge  44 vs inside  60   ->  -15
        guard_frame_1           edge 161 vs inside  46   -> +115
        guard_push_1            edge 168 vs inside  44   -> +124

    Kylo's edge is DARKER than what it surrounds, which is right: his edge
    is the dark outline the artist drew. The guard's is over a hundred
    brighter - a white ring, not an outline. That ring is what reads as
    blurry, rough and jittery beside him, and it gets worse the more the
    sprite is scaled, because a bilinear filter reaches further past the
    edge for its samples.

    WHAT IT CHANGES, AND WHAT IT DOES NOT. Alpha is not touched anywhere, so
    the silhouette, the shape, the proportions and every visible pixel of
    the design are exactly what they were. Only the colour STORED beneath
    the transparency changes - the colour a filter finds when it samples
    past the edge. Nothing is redesigned, repainted or moved.
    """
    a = np.asarray(im.convert("RGBA")).astype(np.float32)
    rgb = a[:, :, :3].copy()
    alpha = a[:, :, 3]
    known = alpha >= 250
    if not known.any():
        return im

    for _ in range(depth):
        if known.all():
            break
        total = np.zeros_like(rgb)
        count = np.zeros(known.shape, np.float32)
        for dy, dx in ((1, 0), (-1, 0), (0, 1), (0, -1)):
            nb = np.roll(known, (dy, dx), (0, 1))
            total += np.roll(rgb, (dy, dx), (0, 1)) * nb[:, :, None]
            count += nb
        fill = (~known) & (count > 0)
        if not fill.any():
            break
        rgb[fill] = total[fill] / count[fill][:, None]
        known = known | fill

    return Image.fromarray(np.dstack([rgb, alpha]).astype(np.uint8), "RGBA")


def sharpen_linework(im, radius=1.5, percent=90, threshold=2):
    """Put some bite back into outlines that were drawn soft.

    WHY IT IS NEEDED AT ALL. The guard and Kylo are drawn in different
    styles: Kylo is flat cel colour with a hard dark outline, the guard is
    airbrushed, with shading that fades and outlines that are softer to
    begin with. Measured at the size they actually render, the strength of
    the edges inside the figure comes out at 92 for Kylo and 69 for the
    guard, and that gap is what reads as "blurry next to him".

    WHY THE RADIUS IS 1.5. It tracks the gap between how he is STORED and
    how he is DRAWN, and that gap is now 2:1 rather than the 4.7:1 it used
    to be - see CANVAS_H. One and a half pixels here is about one pixel on
    screen, which is the scale the eye is judging. When the canvas was 1425
    this had to be 3 to survive the reduction; shrinking the canvas and
    sharpening afterwards does the same job far better, because the sharpen
    is no longer competing with a filter that is throwing detail away.

    WHY ONLY 70 PERCENT. Tried at 70, 110 and 160 against Kylo. 160 throws
    bright halos round the cap brim and the moustache and makes the shading
    look crunched - which is the same jittery edge this whole pass exists to
    remove, just a different cause. 110 begins to show it on the brim. 70 is
    a clear improvement with nothing ringing.

    The threshold keeps it off the flat areas, so his shirt and the sky-flat
    parts of the uniform are not given texture that was never drawn.

    Alpha is passed through untouched, so the silhouette does not move.
    """
    r, g, b, a = im.convert("RGBA").split()
    rgb = Image.merge("RGB", (r, g, b)).filter(
        ImageFilter.UnsharpMask(radius=radius, percent=percent, threshold=threshold))
    return Image.merge("RGBA", rgb.split() + (a,))


def split_figures(src, count):
    """Every figure's box, whether the sheet is one row or a grid.

    Columns first, because that is the cheap case and the one the wave
    frames use. Only if that does not account for every figure is the sheet
    treated as bands - otherwise a single row of six would be cut into rows
    on the gap under a raised arm.
    """
    spans = split_columns(src)
    if len(spans) == count:
        return [(x0, 0, x1, src.size[1] - 1) for x0, x1 in spans]

    boxes = []
    for y0, y1 in split_rows(src):
        band = src.crop((0, y0, src.size[0], y1 + 1))
        for x0, x1 in split_columns(band):
            boxes.append((x0, y0, x1, y1))
    return boxes

RAW = "Art/guard_frames_raw.png"
REF = "Assets/Sprites/char_guard_idle.png"
OUT = "Assets/Sprites/guard_frame_{}.png"

# THE CANVAS IS DELIBERATELY SMALL, and this is the single thing that most
# affects how he looks on screen.
#
# He is drawn about 300px tall in a 1080p game view. Stored at char_guard's
# own 1425, that is a reduction of nearly five to one - and sprite textures
# here have mipmaps off, so the GPU does not area-average, it takes four
# texels per output pixel wherever they happen to land. At 4.7:1 it misses
# most of the drawing. Simulating exactly that sampling on his own sprite,
# his outlines come out broken and speckled and the shirt placket dissolves
# into noise. That is what reads as "blurry, rough, jittery" - it is
# aliasing, not softness, and no amount of sharpening fixes it because the
# detail is not being sampled in the first place.
#
# Kylo never had the problem: he is stored 745 for the same 300, barely 2.5
# to one, which is inside what bilinear can carry.
#
# So the figure is written at 600 and the reduction becomes 2:1, matching
# him. The downsample is done HERE, once, with LANCZOS, which is a far
# better filter than the one the GPU will use - and the sharpen then lands
# at the scale the eye actually judges instead of being thrown away.
#
# Nothing about his placement changes. CreateStanding divides the world
# height it wants by the sprite's own bounds.size.y, so a canvas half the
# pixels is simply scaled twice as much and he stands exactly where he did.
# What matters is that the ink keeps the same PROPORTION of the canvas, so
# these three move together.
CANVAS_H = 603
INK_TOP = 1          # where the figure starts, matching char_guard's ratio
INK_H = 600          # and how tall it is


def fit_guard_strip(raw, out_pattern, count=3, uniform_scale=False):
    """Cut a strip of figures and write them on char_guard's canvas shape.

    Shared by the wave frames and the gate-push frames, because the thing
    that matters is the same for both: CreateStanding scales the guard by
    bounds.size.y, so every frame has to sit on a canvas the same height as
    the sprites it stands beside or he changes height mid-action.

    uniform_scale picks HOW the figures are sized to that canvas, and the
    two answers differ once a pose stops being upright.

      False  every figure is stretched to the same ink height. Right when
             every pose stands straight, as the three waves do, and what
             the wave frames were fitted with - so it stays the default and
             their output is unchanged to the byte.

      True   every figure is scaled by the SAME factor, taken from the first
             pose, and sits on a shared baseline instead of a shared top.

    The push needs True. Measured on the six-pose sheet, his drawn ink runs
    519px standing but only 469px in the lunge, because a man leaning out
    over a bent knee IS shorter. Stretching that frame to the same height
    scales him up 10.7%, so he swells as he pushes and shrinks as he comes
    back up - worst on the lunge, which is also the frame held longest while
    the gate swings. Scaling them all alike and standing them on one
    baseline keeps his head the size it was drawn and lets him drop as he
    leans, which is what the drawing already says.
    """
    ref = Image.open(REF).convert("RGBA")
    ra = np.asarray(ref)
    rrows = np.where((ra[:, :, 3] > 60).any(axis=1))[0]
    print("reference %s: canvas %s, ink rows %d..%d"
          % (os.path.basename(REF), ref.size, rrows[0], rrows[-1]))
    print()

    src = Image.open(raw).convert("RGB")
    boxes = split_figures(src, count)
    if len(boxes) != count:
        sys.exit("expected %d figures in %s, found %d: %s"
                 % (count, raw, len(boxes), boxes))

    # PASS ONE: cut and scale every figure, and find where its FEET are.
    #
    # The feet matter because CreateStanding places a sprite by the centre of
    # its canvas. Crop each frame to its own ink and the canvas centre moves
    # as the arm extends - measured on the first attempt, his feet slid from
    # x +3.26 to +2.75 across the push, over half a world unit, so he walked
    # sideways while reaching. His feet do not move in the drawing; only the
    # canvas around them did.
    contents = []
    for i, (x0, y0, x1, y1) in enumerate(boxes, start=1):
        crop = src.crop((max(0, x0 - 30), y0,
                         min(src.size[0], x1 + 31), y1 + 1))
        padded = Image.new("RGB", (crop.size[0], crop.size[1] + 60), (255, 255, 255))
        padded.paste(crop, (0, 30))
        tmp = "%s.__guard%d.png" % (raw, i)
        padded.save(tmp)
        try:
            cut = cut_background(tmp)
        finally:
            os.remove(tmp)

        # Take the white ring off before anything is scaled. Resizing a
        # fringed sprite spreads the fringe, so this has to come first.
        cut = bleed_edges(cut)

        a = np.asarray(cut)
        opaque = a[:, :, 3] > 60
        rows = np.where(opaque.any(axis=1))[0]
        cols = np.where(opaque.any(axis=0))[0]
        contents.append(cut.crop((cols[0], rows[0], cols[-1] + 1, rows[-1] + 1)))

    # ONE scale for every pose, or one per pose - see uniform_scale above.
    # The shared factor is taken from the FIRST pose because that is the
    # resting stand, the frame the scene opens on and returns to, so it is
    # the one that has to match the sprites around it.
    shared = float(INK_H) / contents[0].size[1]

    figures = []
    for i, content in enumerate(contents, start=1):
        scale = shared if uniform_scale else float(INK_H) / content.size[1]
        w = max(1, int(round(content.size[0] * scale)))
        h = max(1, int(round(content.size[1] * scale)))
        scaled = content.resize((w, h), Image.LANCZOS)

        # Sharpen at the size the sprite is actually STORED, not before it -
        # a sharpen applied to the raw cut would be half dissolved by the
        # resize that follows. Then bleed again: the unsharp runs over the
        # rim as well, and anything it did there has to be put back under
        # the figure's own colour or the white ring creeps in a second time.
        scaled = sharpen_linework(scaled)
        scaled = bleed_edges(scaled)

        # His feet: the ink in the bottom twelfth, which is shoes and nothing
        # else in any of these poses.
        sa = np.asarray(scaled)
        sop = sa[:, :, 3] > 60
        foot = sop[int(h * 0.92):]
        fcols = np.where(foot.any(axis=0))[0]
        feet_cx = (fcols[0] + fcols[-1]) / 2.0 if len(fcols) else w / 2.0
        figures.append((i, scaled, feet_cx))

    # PASS TWO: one canvas wide enough for every pose, SYMMETRIC about the
    # feet, so the canvas centre is the feet in all of them. The builder
    # centres the canvas on his spot, so his feet land on that spot in every
    # frame and the arm grows out to the side instead of dragging him along.
    half = 0
    for _, scaled, feet_cx in figures:
        half = max(half, feet_cx, scaled.size[0] - feet_cx)
    canvas_w = int(round(half * 2))

    # Stood on a shared BASELINE rather than hung from a shared top. With
    # every figure the same height these are the same row and nothing moves;
    # with a short pose they are not, and the difference is whether he sinks
    # into the pavement or crouches on it.
    floor = INK_TOP + INK_H

    for i, scaled, feet_cx in figures:
        out = Image.new("RGBA", (canvas_w, CANVAS_H), (0, 0, 0, 0))
        top = floor - scaled.size[1]
        out.alpha_composite(scaled, (int(round(half - feet_cx)), top))
        path = out_pattern.format(i)
        out.save(path)
        print("%s  %dx%d  ink rows %d..%d  feet on the canvas centre"
              % (os.path.basename(path), canvas_w, CANVAS_H,
                 top, floor - 1))


if __name__ == "__main__":
    fit_guard_strip(RAW, OUT)
