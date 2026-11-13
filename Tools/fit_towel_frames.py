"""Cut a generated strip of standing towel poses into animation frames.

The seated poses have `fit_seated_pose.py`; this is its standing cousin, and
it reuses that file's hard-won parts rather than copying them: the flood-fill
background cut that knows not to eat white, the halo rounds that clear the
slivers between his hair spikes, the hair-cap measurement, and the column
splitter. Only the fitting differs, because a standing pose is pinned by its
crown rather than by a desk edge.

WHY THE CANVAS IS SHARED. These are animation frames, swapped into one
SpriteRenderer whose sprite pivot is the centre of the image. If the frames
were each trimmed to their own artwork, every swap would shift him sideways
and up - the frames would jitter instead of animate. So every frame is drawn
onto a canvas the exact size of the base pose, with his crown on the base's
crown row and his head over the base's head column. Then nothing moves
between frames except what is meant to.

WHY THE SCALE COMES FROM HIS HAIR. A head cannot change size between two
drawings of the same boy, and the generator does not keep the figure at a
constant size across a strip. Matching the hair cap is what stops him
growing or shrinking mid-action - the same trick the seated poses use.

Usage, from the repository root:

    python3 Tools/fit_towel_frames.py Art/sipilyo_frames_raw.png sipilyo_frame

writes Assets/Sprites/sipilyo_frame_1.png, _2, _3 ... one per figure found,
each with a .meta copied from the base so it imports at 120 pixels per unit.

A third argument names a different base to normalise against:

    python3 Tools/fit_towel_frames.py Art/x_raw.png x standing_barefoot

When a base is named that way it is NOT re-canvased - standing_barefoot is
in a dozen scenes and resizing it to suit one new pose would move Kylo in
all of them. The canvas is the base's own size instead, and anything that
does not fit is reported rather than silently cropped.
"""
import os
import re
import sys
import uuid

import numpy as np
from PIL import Image
from scipy import ndimage

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from fit_seated_pose import bleed_edges, hair_cap, split_columns  # noqa: E402
from import_towel_pose import cut_background  # noqa: E402

BASE = 'Assets/Sprites/standing_towel_messy.png'
OUT_DIR = 'Assets/Sprites'


def keep_figure(pose):
    """Drop everything that is not the boy.

    The generator lays its frames out with faint panel edges between them,
    and those survive the background cut: they are not reachable from the
    tile's border once the figure's own outline is in the way, so the flood
    fill leaves them as pale bars down the side of a frame. Keeping only the
    largest connected piece removes them.

    Connectivity is measured on a slightly grown alpha so a thin object he is
    holding - the toothbrush, a comb - counts as part of him even where the
    anti-aliasing between hand and handle goes transparent for a pixel.
    """
    a = np.array(pose)
    opaque = a[:, :, 3] > 8
    grown = ndimage.binary_dilation(opaque, np.ones((5, 5)))
    labels, count = ndimage.label(grown)
    if count <= 1:
        return pose
    sizes = ndimage.sum(opaque, labels, range(1, count + 1))
    biggest = int(np.argmax(sizes)) + 1
    a[:, :, 3] = np.where(labels == biggest, a[:, :, 3], 0)
    return Image.fromarray(a)


def scale_to_hair(pose, b_hair):
    """The figure resized so its head matches the base's, with its landmarks."""
    p_crown, p_bottom, p_cx = hair_cap(pose)
    scale = b_hair / float(p_bottom - p_crown)
    pose = pose.resize((max(1, round(pose.width * scale)),
                        max(1, round(pose.height * scale))), Image.LANCZOS)
    return pose, round(p_crown * scale), round(p_cx * scale), scale


def place(pose, p_crown, p_cx, width, height, crown_row):
    """Paste one scaled figure onto the shared canvas, head on the centre line."""
    canvas = Image.new('RGBA', (width, height), (0, 0, 0, 0))
    canvas.paste(pose, (width // 2 - p_cx, crown_row - p_crown), pose)
    return canvas


def write_meta(png_path, template):
    meta = png_path + '.meta'
    if os.path.exists(meta):
        return
    with open(template + '.meta', encoding='utf-8') as f:
        text = f.read()
    text = re.sub(r'^guid: [0-9a-f]{32}$', 'guid: ' + uuid.uuid4().hex,
                  text, count=1, flags=re.M)
    with open(meta, 'w', encoding='utf-8') as f:
        f.write(text)


def main():
    if len(sys.argv) not in (3, 4):
        raise SystemExit(__doc__)
    raw, stem = sys.argv[1], sys.argv[2]
    base_path = (os.path.join(OUT_DIR, sys.argv[3] + '.png')
                 if len(sys.argv) == 4 else BASE)
    lock_base = len(sys.argv) == 4

    base = Image.open(base_path).convert('RGBA')
    b_crown, b_bottom, b_cx = hair_cap(base)
    b_hair = b_bottom - b_crown
    print('base %s  hair %dpx, crown row %d, head column %d'
          % (os.path.basename(base_path), b_hair, b_crown, b_cx))

    strip = Image.open(raw).convert('RGB')
    spans = split_columns(strip)
    if not spans:
        raise SystemExit('no figures found in %s' % raw)
    print('%d figure(s) at %s' % (len(spans), spans))

    # Scale every figure first, then size one canvas to hold the widest of
    # them AND the base. A raised arm makes a frame wider than the resting
    # pose, so sizing the canvas to the base clips exactly the frame the
    # animation exists to show.
    poses = []
    for x0, x1 in spans:
        tile = np.asarray(strip.crop((x0, 0, x1 + 1, strip.height)), dtype=int)
        # import_towel_pose's cutter, not the seated one's: these figures sit
        # on their own white card inside the strip, and the seated version -
        # written to protect a white polo shirt - leaves the card behind as a
        # pale panel either side of him.
        # bleed BEFORE the resize: once a resampler has mixed the white
        # margin into the edge pixels there is no getting it back out, and
        # the sprite keeps a pale rim over the bathroom tiles.
        cut = bleed_edges(Image.fromarray(
            np.dstack([tile, cut_background(tile)]).astype(np.uint8)))
        poses.append(scale_to_hair(keep_figure(cut), b_hair))

    # Measured from the head's centre column outwards, because that column is
    # what every frame is lined up on - and, once the canvas is symmetric
    # about it, what the centre sprite pivot lands on too. Pin the head and
    # nothing else can drift.
    half = max(max(cx, im.width - cx) for im, _, cx, _ in poses)
    half = max(half, b_cx, base.width - b_cx) + 2
    width, height = half * 2, base.height
    # A wider canvas does not move him. Every frame puts the head column on
    # the canvas centre, and the sprite pivot is that same centre, so his
    # head lands in the same world position whatever the width - which is
    # what lets a pose carrying a towel be wider than the plain one without
    # the swap shifting him sideways.
    print('shared canvas %dx%d (base was %dx%d)'
          % (width, height, base.width, base.height))

    outs = []
    for i, (im, crown, cx, scale) in enumerate(poses, start=1):
        frame = place(im, crown, cx, width, height, b_crown)
        out = os.path.join(OUT_DIR, '%s_%d.png' % (stem, i))
        frame.save(out)
        write_meta(out, base_path)
        outs.append((out, frame, scale))

    # The base moves onto the same canvas, or it would be the one sprite in
    # the set that jumps when the animation hands back to it. Same path, so
    # it keeps its GUID and no scene reference moves.
    if width != base.width and not lock_base:
        rebased = place(base, b_crown, b_cx, width, height, b_crown)
        rebased.save(BASE)
        print('%s re-canvased to %dx%d' % (BASE, width, height))

    for out, frame, scale in outs:
        opaque = np.asarray(frame)[:, :, 3] > 8
        cols = np.nonzero(opaque.any(0))[0]
        clipped = cols.size and (cols[0] == 0 or cols[-1] == frame.width - 1)
        print('%s  scale x%.3f%s' % (out, scale,
                                     '  WARNING: still touching the edge'
                                     if clipped else ''))


if __name__ == '__main__':
    main()
