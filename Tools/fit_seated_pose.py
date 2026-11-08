"""Fit a generated seated pose of Kylo onto the shared 663x921 sprite canvas.

WHY THIS EXISTS. BuildPaaralanScene2 draws every seated pose from one canvas
and pins its BOTTOM EDGE to the desk's far edge, so a pose only sits right if
it is the correct size and its crown is on the correct row. Get either wrong
and he either changes size mid-scene or sinks behind the desk - both of which
this scene has already suffered once.

WHERE THE ART COMES FROM. Poses are generated through the Canva connector
from two references: sitting_front_uniform.png for Kylo's design, and the
group's storyboard panel for the pose. The Canva API only ever returns a
~200px thumbnail, so the full-size file has to be downloaded by hand out of
Canva Uploads; that is why this reads a local raw from Art/ rather than
fetching anything.

WHAT IT DOES.
  1. Cuts the white background by flood-filling inward from the border. A
     plain "delete white" would punch holes in his polo shirt and in any
     paper he is holding; only background reachable from the edge goes.
  2. Scales the figure so its HAIR CAP matches sitting_front_uniform's. A
     head cannot change size between two poses of the same boy, so matching
     it is what stops him growing or shrinking when the sprite swaps.
  3. Lands his crown on the base pose's crown row with his head over the same
     column, then crops at the canvas bottom - the desk's far edge. Whatever
     falls below that is hidden by the desk, which is what the storyboard
     panels show.

Writes in place over the named sprite, keeping its GUID, so no scene
reference moves and no new .meta has to be committed.

Usage, from the repository root:
    python Tools/fit_seated_pose.py <raw-in-Art> <sprite-out>

    python Tools/fit_seated_pose.py Art/reach_bag_uniform_raw.png \
                                    Assets/Sprites/reach_bag_uniform.png
    python Tools/fit_seated_pose.py Art/pencil_ready_uniform_raw.png \
                                    Assets/Sprites/pencil_ready_uniform.png
"""
import os
import sys

from PIL import Image, ImageDraw, ImageFilter
import numpy as np

BASE = "Assets/Sprites/sitting_front_uniform.png"
FILL_TOLERANCE = 8
WALL = 2
GAP_TOLERANCE = 25      # rows the hair scan may fail before it gives up
HALO_LUMA = 205         # a pixel this pale on the silhouette is background, not drawing
HALO_ROUNDS = 4         # how far the background may grow into the halo


def cut_background(path):
    """Alpha-cut the plain background, without eating the white shirt.

    The naive version of this - flood-fill inward from the border - ate the
    whole torso of the "Lapis" pose, and the reason is worth recording.

    His polo is (250,250,250) and the background (254,255,255): fourteen
    apart, well inside any tolerance loose enough to catch the anti-aliased
    halo round the outline. Everywhere the outline encloses him that does not
    matter, because the dark line stops the fill. But these poses are CUT OFF
    by the bottom of the frame, so along that edge his shirt has no outline -
    it is just white meeting white. The fill ran along the bottom border from
    the corner, stepped into the shirt, and hollowed him out from below. The
    "Kuha" pose survived the same code only by luck: his navy trousers happen
    to sit on that edge and blocked it.

    Walling off the border turned out NOT to be enough on its own - his shirt
    is within tolerance of the background wherever the two touch, so any loose
    fill crosses into it. So two things guard it. The border is walled off, so
    the fill cannot run along an edge where the figure is cut; and the
    tolerance is tight enough (8, against the 14 that separates shirt from
    background) that it cannot step across the boundary anywhere else.
    """
    src = Image.open(path).convert("RGB")
    W, H = src.size
    work = src.copy()
    sentinel = (255, 0, 255)

    # The wall. Two pixels is enough to stop 4-connected propagation.
    d = ImageDraw.Draw(work)
    d.rectangle([0, 0, W - 1, H - 1], outline=(0, 0, 0), width=WALL)

    inside = WALL + 1
    seeds = [(inside, inside), (W - 1 - inside, inside),
             (inside, H - 1 - inside), (W - 1 - inside, H - 1 - inside)]
    probe = np.asarray(src).astype(int)
    used = 0
    for x, y in seeds:
        # Only seed where it really is background; a corner the figure
        # reaches must not become a hole.
        if probe[y, x].min() < 225:
            continue
        ImageDraw.floodfill(work, (x, y), sentinel, thresh=FILL_TOLERANCE)
        used += 1
    if used == 0:
        raise SystemExit(f"{path}: no corner looks like background - check the raw")

    a = np.asarray(work).astype(int)
    is_bg = (a[:, :, 0] > 245) & (a[:, :, 1] < 20) & (a[:, :, 2] > 245)

    # Resolve the walled band from the pixel just inside it.
    is_bg[:inside, :] = is_bg[inside, :]
    is_bg[-inside:, :] = is_bg[-inside - 1, :]
    is_bg[:, :inside] = is_bg[:, inside][:, None]
    is_bg[:, -inside:] = is_bg[:, -inside - 1][:, None]

    # A tolerance of 8 is deliberately tight - see the note above - and the
    # price is a halo of anti-aliased background left clinging to the outline,
    # two or three pixels wide. Against the classroom it reads as a pale rim
    # round the whole figure, like a sticker.
    #
    # A flat erosion would take it off, but it would equally take a bite out
    # of fingers, a pencil, the arm of a pair of glasses. So the background
    # grows into the halo INSTEAD: a few rounds of spreading, allowed only
    # into pixels pale enough to be background rather than drawing. The black
    # outline is nowhere near that pale, so the spread stops dead at the ink
    # and his white shirt, which sits behind the outline, is never reached.
    lum = np.asarray(src).astype(int).mean(axis=2)
    pale = lum > HALO_LUMA
    for _ in range(HALO_ROUNDS):
        touching = (np.roll(is_bg, 1, 0) | np.roll(is_bg, -1, 0)
                    | np.roll(is_bg, 1, 1) | np.roll(is_bg, -1, 1))
        grown = is_bg | (touching & pale)
        if not grown[~is_bg].any():
            break
        is_bg = grown

    keep = ~is_bg
    hard = np.where(keep, 255, 0).astype(np.uint8)
    im = Image.fromarray(np.dstack([np.asarray(src), hard]), "RGBA")
    # Half a pixel of softness, so the cut does not read as a sticker edge.
    im.putalpha(Image.fromarray(hard).filter(ImageFilter.GaussianBlur(0.6)))
    return im


def hair_cap(im):
    """Crown row, bottom of the dense hair mass, and that mass's centre column.

    Dense mass, not merely dark pixels: this art is outlined in black, so
    every row contains some dark and a naive scan runs all the way down.

    Tolerant of gaps, because the first version was not and it cost an hour.
    The crown is the tip of a hair spike, a few anti-aliased pixels wide and
    too pale to count as dark, and there are more thin rows a little below it
    where the spikes separate. Stopping at the first row that fails the test
    measured a "head" twelve pixels tall and asked for a 40x enlargement. So
    the scan only gives up after a RUN of failures.
    """
    a = np.asarray(im).astype(int)
    r, g, b, al = a[:, :, 0], a[:, :, 1], a[:, :, 2], a[:, :, 3]
    opaque = al > 60
    dark = opaque & (r < 78) & (g < 78) & (b < 78)
    rows = np.where(opaque.any(axis=1))[0]
    crown, last = rows[0], rows[-1]
    ow, dw = opaque.sum(axis=1), dark.sum(axis=1)

    bottom, misses = crown, 0
    for y in range(crown, last + 1):
        if ow[y] > 0 and dw[y] >= 0.45 * ow[y]:
            bottom, misses = y, 0
        else:
            misses += 1
            if misses > GAP_TOLERANCE:
                break

    span = bottom - crown
    if span < 0.08 * (last - crown):
        raise SystemExit(
            f"hair cap came out {span}px on a {last - crown}px figure, which "
            f"cannot be right - the scale factor would be nonsense. Look at "
            f"the cut-out before trusting this.")

    cols = np.where(dark[crown:bottom + 1].any(axis=0))[0]
    return crown, bottom, (cols[0] + cols[-1]) // 2


def fit(raw_path, out_path):
    base = Image.open(BASE).convert("RGBA")
    b_crown, b_bottom, b_cx = hair_cap(base)

    pose = cut_background(raw_path)
    p_crown, p_bottom, p_cx = hair_cap(pose)
    scale = (b_bottom - b_crown) / (p_bottom - p_crown)

    a = np.asarray(pose)
    opaque = a[:, :, 3] > 60
    rows = np.where(opaque.any(axis=1))[0]
    cols = np.where(opaque.any(axis=0))[0]
    content = pose.crop((cols[0], rows[0], cols[-1] + 1, rows[-1] + 1))
    scaled = content.resize((int(round(content.size[0] * scale)),
                             int(round(content.size[1] * scale))), Image.LANCZOS)

    canvas = Image.new("RGBA", base.size, (0, 0, 0, 0))
    x = int(round(b_cx - (p_cx - cols[0]) * scale))
    canvas.alpha_composite(scaled, (x, b_crown))
    canvas.save(out_path)

    hidden = b_crown + scaled.size[1] - base.size[1]
    print(f"{out_path}: scaled {scale:.4f}, placed at ({x},{b_crown}), "
          f"{hidden}px hidden below the desk line, canvas {base.size}")


def split_columns(img):
    """Column spans of separate figures in a strip, found on the empty gaps."""
    a = np.asarray(img.convert("RGB")).astype(int)
    ink = (a.min(axis=2) < 225).sum(axis=0)
    spans, start = [], None
    for x, v in enumerate(ink):
        if v > 2 and start is None:
            start = x
        if v <= 2 and start is not None:
            if x - start > 40:
                spans.append((start, x - 1))
            start = None
    if start is not None and len(ink) - start > 40:
        spans.append((start, len(ink) - 1))
    return spans


def fit_strip(raw_path, out_pattern, crowns, seat_feet_y=-3.009,
              seated_height=5.609):
    """Cut a row of generated poses out of one image and fit each to the canvas.

    One Canva generation gives three poses in a row, which is three times
    less cooldown than three generations and - more usefully - three poses
    drawn in one pass, so they match each other.

    Each is background-cut, scaled so its HAIR CAP matches the base seated
    pose (the generator draws them at slightly different sizes, and a head
    that changes size between frames is what gives an animation away), then
    placed so its crown sits at the world height named in `crowns`.
    """
    src = Image.open(raw_path).convert("RGB")
    spans = split_columns(src)
    if len(spans) != len(crowns):
        sys.exit(f"expected {len(crowns)} figures in {raw_path}, "
                 f"found {len(spans)}: {spans}")

    base = Image.open(BASE).convert("RGBA")
    b_crown, b_bottom, b_cx = hair_cap(base)
    base_hair = b_bottom - b_crown
    unit = seated_height / base.size[1]
    canvas_top = seat_feet_y + seated_height

    written = []
    for i, (x0, x1) in enumerate(spans):
        # Pad with white: cut_background walls off the border, and that wall
        # needs background to bite on rather than the figure itself.
        crop = src.crop((max(0, x0 - 30), 0,
                         min(src.size[0], x1 + 31), src.size[1]))
        padded = Image.new("RGB", (crop.size[0], crop.size[1] + 60),
                           (255, 255, 255))
        padded.paste(crop, (0, 30))
        tmp = f"{raw_path}.__frame{i}.png"
        padded.save(tmp)
        try:
            cut = cut_background(tmp)
        finally:
            os.remove(tmp)

        c, b, cx = hair_cap(cut)
        scale = base_hair / (b - c)

        a = np.asarray(cut)
        opaque = a[:, :, 3] > 60
        rows = np.where(opaque.any(axis=1))[0]
        cols = np.where(opaque.any(axis=0))[0]
        content = cut.crop((cols[0], rows[0], cols[-1] + 1, rows[-1] + 1))
        scaled = content.resize((int(round(content.size[0] * scale)),
                                 int(round(content.size[1] * scale))),
                                Image.LANCZOS)

        out = Image.new("RGBA", base.size, (0, 0, 0, 0))
        x = int(round(b_cx - (cx - cols[0]) * scale))
        y = int(round((canvas_top - crowns[i]) / unit))
        out.alpha_composite(scaled, (x, y))
        path = out_pattern.format(i + 1)
        out.save(path)
        written.append(path)
        print(f"{path}: hair {b - c} -> {base_hair}px (x{scale:.3f}), "
              f"crown at world {crowns[i]:+.3f} (row {y})")
    return written


if __name__ == "__main__":
    if len(sys.argv) != 3:
        sys.exit(__doc__)
    fit(sys.argv[1], sys.argv[2])
