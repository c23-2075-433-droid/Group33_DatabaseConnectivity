"""Redraws Kylo's shoes and socks as bare feet.

Scenes 1 and 2 are the morning at home: Kylo wakes up, crosses the bedroom and
walks into the bathroom. He should not already be wearing school shoes - those
go on in Scene 4, one word at a time, with "Medyas" and "Sapatos".

Recolouring the shoe does not work: the laces, the sole and the stitching all
survive and you get an orange sneaker. Asking an image model for barefoot walk
frames does not work either - each frame comes back with a different stride, so
the four of them no longer loop. So this works from the art that is already
there, which is the only way the frames stay on-model with each other.

Two steps per foot. The shin is grown downwards by repeating the leg's own
pixels, narrowing a little and following its lean, so the highlight and the
line thickness match the leg above exactly. Then a foot is drawn below it,
sized from the shoe it replaces: the shoe says where the floor is, which way
the toes point and how far forward they reach.

    python3 Tools/make_barefoot.py

Writes Assets/Sprites/<pose>_barefoot.png for the standing pose and the four
walk frames. The lying-down and sitting-up poses are separate art, not derived
here.
"""

import io
import os
import re
import tempfile
import uuid
import numpy as np
from PIL import Image, ImageDraw
from scipy import ndimage

SPRITES = os.path.join(os.path.dirname(__file__), '..', 'Assets', 'Sprites')
# Everything below assumes a leg that runs down the image into a shoe resting
# on the floor. Lying down and sitting up do not, so those two are turned until
# they do, redrawn, and turned back - by the angle of the shins, which is 90
# degrees asleep and about 40 sitting on the edge of the bed. Only the redrawn
# part is turned back onto the original, so the rest of the drawing is never
# resampled.
POSES = [('standing', 0), ('walk_frame_1', 0), ('walk_frame_2', 0),
         ('walk_frame_3', 0), ('walk_frame_4', 0),
         ('sitting_up', -40), ('lying_down', -90)]

SS = 4      # supersampling, so the redrawn outline is not stair-stepped
MEASURE = 26  # how far above the sock to measure the leg's true width


def classify(path):
    a = np.array(Image.open(path).convert('RGBA')).astype(int)
    r, g, b, al = a[..., 0], a[..., 1], a[..., 2], a[..., 3]
    opaque = al > 100
    skin = opaque & (r > 195) & (g > 110) & (g < 220) & (b < 200) & ((r - b) > 55)
    navy = opaque & (b > r + 20) & (b > g + 10) & (r < 130) & (b > 45)
    white = opaque & (r > 190) & (g > 190) & (b > 190)
    return a, opaque, skin, navy, white


def footwear_seed(navy, white):
    """The socks and shoes, told apart from the shorts by how they meet.

    The shorts are the same navy as the shoes, so colour alone cannot separate
    them, and neither can height - sitting down puts the hem and the heels on
    the same rows. What is always true is the join: a shoe meets a white sock,
    and the shorts meet nothing white at all.
    """
    socks = touching(white, navy)
    return socks | touching(navy, socks)


def touching(mask, other):
    """The parts of mask that run up against other."""
    lab, n = ndimage.label(mask, np.ones((3, 3)))
    if n == 0:
        return np.zeros_like(mask)
    near = ndimage.binary_dilation(other, np.ones((5, 5)))
    keep = [v for v in np.unique(lab[near & (lab > 0)])]
    return np.isin(lab, keep) & mask


def footwear_mask(opaque, skin, navy, white):
    """Every pixel of sock, shoe and the dark line drawn around them.

    Only as far as the line, though. Following the outline wherever it leads
    would walk straight up the shoe into the shorts and the shirt, which are
    drawn with the same line - so the reach is a few pixels, not a flood fill.
    """
    seed = footwear_seed(navy, white)
    if not seed.any():
        return seed
    edge = ndimage.binary_dilation(seed, np.ones((9, 9))) & opaque & ~skin
    return ndimage.binary_closing(seed | edge, np.ones((5, 5)))


def run_through(row, x):
    """The unbroken stretch of this row that contains x - never two legs."""
    x = int(round(min(max(x, 0), len(row) - 1)))
    if not row[x]:
        on = np.where(row)[0]
        if len(on) == 0:
            return 0, 0
        x = int(on[np.argmin(np.abs(on - x))])
    a = b = x
    while a > 0 and row[a - 1]:
        a -= 1
    while b < len(row) - 1 and row[b + 1]:
        b += 1
    return a, b


def find_legs(skin, mask):
    """One entry per shin that ends in footwear.

    A leg is a run of skin that stops where the sock starts, so the skin itself
    is what we measure - not the sock, which bulges, and not the shoe, which is
    a different shape altogether. The width is read a little way up the shin,
    because the very bottom of the skin tapers to a point against the sock.
    """
    lab, _ = ndimage.label(skin, np.ones((3, 3)))
    legs = []
    for i, sl in enumerate(ndimage.find_objects(lab), 1):
        if sl is None:
            continue
        part = lab == i
        if part.sum() < 400:
            continue
        bottom = sl[0].stop - 1
        if bottom + 2 >= skin.shape[0] or not mask[bottom + 1:bottom + 14].any():
            continue
        y = max(sl[0].start, bottom - MEASURE)
        bx = np.where(part[bottom - 2])[0] if bottom - 2 >= sl[0].start else np.where(part[bottom])[0]
        x0, x1 = run_through(part[y], float(bx.min() + bx.max()) / 2.0)
        legs.append({'ya': bottom, 'row': y, 'x0': x0, 'x1': x1,
                     'cx': float(x0 + x1) / 2.0, 'top': sl[0].start})
    if not legs:
        return legs

    widths = sorted(l['x1'] - l['x0'] for l in legs)
    typical = widths[len(widths) // 2]
    donor = max(legs, key=lambda l: l['x1'] - l['x0'])
    for leg in legs:
        leg['donor'] = leg
        if (leg['x1'] - leg['x0']) < 0.65 * typical:
            # This leg is mostly behind the other one. Borrow its neighbour's
            # shin, so the hidden part still looks like a leg rather than a
            # sliver, and let convert() find where it really sits.
            leg['donor'] = donor
    return legs


def sole_reach(sole, cx):
    """Which way this foot points, and where its toe is, along the floor."""
    on = np.where(sole)[0]
    if len(on) == 0:
        return 1.0, cx
    x = int(on[np.argmin(np.abs(on - cx))])
    a = b = x
    while a > 0 and sole[a - 1]:
        a -= 1
    while b < len(sole) - 1 and sole[b + 1]:
        b += 1
    return (1.0, float(b)) if (b - cx) >= (cx - a) else (-1.0, float(a))


def split_by_leg(mask, legs):
    """Crossed feet share one blob. Give every pixel to its nearest leg."""
    if len(legs) < 2:
        return [mask]
    ys, xs = np.nonzero(mask)
    cx = np.array([l['cx'] for l in legs])
    cy = np.array([l['ya'] for l in legs], dtype=float)
    # Horizontal distance dominates: the feet sit side by side, not stacked.
    d = (xs[:, None] - cx[None, :]) ** 2 + 0.25 * (ys[:, None] - cy[None, :]) ** 2
    owner = d.argmin(1)
    out = []
    for i in range(len(legs)):
        m = np.zeros_like(mask)
        m[ys[owner == i], xs[owner == i]] = True
        out.append(m)
    return out


def lean_of(skin, leg):
    """How far the shin drifts sideways per row, so the new part keeps going."""
    y0, y1 = leg['row'], max(leg['top'], leg['row'] - 40)
    if y0 - y1 < 8:
        return 0.0
    def centre(y):
        xs = np.where(skin[y])[0]
        xs = xs[np.abs(xs - leg['cx']) < (leg['x1'] - leg['x0'])]
        return float(xs.min() + xs.max()) / 2.0 if len(xs) else None
    a, b = centre(y0), centre(y1)
    return 0.0 if a is None or b is None else (a - b) / float(y0 - y1)


def strip_row(skin, white, navy, leg):
    """The lowest row that is still all leg.

    The sock cuts across the shin at an angle, so there is no clean row right
    above it - the last few are half sock already. Walk up until the leg is
    back to its full width with no sock in it, and copy from there.
    """
    target = leg['x1'] - leg['x0']
    for y in range(int(leg['ya']) - 2, max(int(leg['top']), int(leg['ya']) - 70), -1):
        a, b = run_through(skin[y], leg['cx'])
        if (b - a) >= 0.92 * target and not (white[y, a:b + 1].any() or navy[y, a:b + 1].any()):
            return y, a, b
    return leg['row'], leg['x0'], leg['x1']


def extend_leg(src, a, opaque, skin, white, navy, leg, yj, lean, gap_top):
    """Grows the shin down to the ankle by repeating the leg's own pixels.

    Drawing a new shin never matches: the real one has a highlight down one
    side and a line of its own weight. Copying the last clean row of the leg
    downwards keeps both, and the slight narrowing reads as an ankle.
    """
    donor = leg['donor']
    row, x0, x1 = strip_row(skin, white, navy, donor)
    # Reach past the skin far enough to take the dark line drawn around the
    # leg, and no further. The pixels outside that line are half background,
    # and repeating those down the shin leaves a pale streak beside it.
    def reach(x, step):
        taken = 0
        while 0 <= x + step < opaque.shape[1] and taken < 7:
            nx = x + step
            if not opaque[row, nx] or white[row, nx] or navy[row, nx]:
                break
            if taken >= 2 and a[row, nx, :3].sum() > 330:
                break
            x, taken = nx, taken + 1
        return x

    x0, x1 = reach(x0, -1), reach(x1, 1)

    strip = src.crop((x0, row, x1 + 1, row + 1))
    w0 = x1 - x0 + 1
    # Start above the hole, not just above the sock: the sock cuts in higher on
    # one side of the leg than the other, and a slit of nothing there is worse
    # than a couple of repeated rows.
    top = min(row if donor is leg else int(leg['ya']) - 3, gap_top - 2)
    out = Image.new('RGBA', src.size, (0, 0, 0, 0))
    for y in range(top, int(yj) + 2):
        t = (y - top) / max(1.0, yj - top)
        wy = max(4, int(round(w0 * (1.0 - 0.22 * t))))
        cxx = leg['cx'] + lean * (y - top)
        out.alpha_composite(strip.resize((wy, 1), Image.LANCZOS),
                            (int(round(cxx - wy / 2.0)), y))
    return out, leg['cx'] + lean * (yj - top), w0 * 0.84 / 2.0


def chaikin(points, rounds=3):
    """Rounds a polygon off, so the heel and the toes are not corners."""
    pts = list(points)
    for _ in range(rounds):
        new = []
        for i in range(len(pts)):
            p, q = pts[i], pts[(i + 1) % len(pts)]
            new.append((0.75 * p[0] + 0.25 * q[0], 0.75 * p[1] + 0.25 * q[1]))
            new.append((0.25 * p[0] + 0.75 * q[0], 0.25 * p[1] + 0.75 * q[1]))
        pts = new
    return pts


def foot_outline(cxj, yj, wa, yb, d, reach):
    """A bare foot, hanging off the ankle at (cxj, yj) and standing on yb.

    Everything is measured forward along d, the way the toes point, so the same
    shape serves a foot facing either way.
    """
    fh = max(6.0, yb - yj)
    L = min(max(reach * 0.70, wa * 3.4), wa * 4.6)
    X = lambda f: cxj + d * f
    pts = [
        (X(-wa), yj - 6),                     # back of the ankle
        (X(-wa * 1.40), yj + fh * 0.45),      # the heel swells out behind
        (X(-wa * 1.05), yb),                  # heel on the floor
        (X(L * 0.45), yb),                    # sole
        (X(L * 0.90), yb - fh * 0.05),
        (X(L), yb - fh * 0.33),               # toes
        (X(L * 0.70), yb - fh * 0.66),
        (X(wa * 1.05), yj + fh * 0.10),       # the instep rises to the ankle
        (X(wa), yj - 6),
    ]
    return chaikin(pts), (fh, L)


def toe_lines(cxj, yj, yb, d, fh, L):
    """Three short creases so the end of the foot reads as toes."""
    out = []
    for f, top in ((0.66, 0.38), (0.80, 0.40), (0.91, 0.34)):
        x = cxj + d * L * f
        out.append(((x, yb - fh * top), (x - d * L * 0.03, yb - fh * 0.06)))
    return out


def palette(a, skin):
    fill = np.median(a[..., :3][skin], axis=0)
    edge = ndimage.binary_dilation(skin, np.ones((3, 3))) & ~skin
    dark = edge & (a[..., 3] > 100) & (a[..., :3].sum(2) < 260)
    line = np.median(a[..., :3][dark], axis=0) if dark.sum() > 50 else np.array([60, 32, 24])
    return tuple(int(v) for v in fill), tuple(int(v) for v in line)


def upright(img, turn):
    """Turns a pose so its legs run downwards, on a canvas that survives the
    turn back. Rotating in place about the centre of a square is reversible;
    letting the canvas grow is not."""
    side = int(np.ceil(np.hypot(*img.size)))
    box = Image.new('RGBA', (side, side), (0, 0, 0, 0))
    at = ((side - img.size[0]) // 2, (side - img.size[1]) // 2)
    box.alpha_composite(img, at)
    return box.rotate(turn, Image.BICUBIC), at


def recentre(img, want):
    """Trims back to the original framing, widened if the feet need the room.

    The pivot is the middle of the sprite, so the crop has to stay centred: a
    bare foot that reaches past where the shoe did gets more canvas on both
    sides rather than being cut off or shifted.
    """
    box = img.getbbox()
    cx, cy = img.size[0] / 2.0, img.size[1] / 2.0
    hx = max(want[0] / 2.0, abs(box[0] - cx), abs(box[2] - cx))
    hy = max(want[1] / 2.0, abs(box[1] - cy), abs(box[3] - cy))
    return img.crop((int(cx - hx), int(cy - hy), int(cx + hx), int(cy + hy)))


def convert(pose, turn=0):
    src_path = os.path.join(SPRITES, pose + '.png')
    original = Image.open(src_path).convert('RGBA')
    src, at = (original, (0, 0)) if not turn else upright(original, turn)
    if turn:
        src_path = os.path.join(tempfile.gettempdir(), pose + '_upright.png')
        src.save(src_path)

    a, opaque, skin, navy, white = classify(src_path)
    mask = footwear_mask(opaque, skin, navy, white)
    legs = find_legs(skin, mask)
    if not legs:
        print('  %s: no leg found, skipped' % pose)
        return

    fill, line = palette(a, skin)
    bare = np.array(src).copy()
    # Take two extra pixels: the edge of the sock is anti-aliased into the leg,
    # and leaving those half-white pixels in place draws a seam across the shin.
    hole = ndimage.binary_dilation(mask, np.ones((5, 5)))
    bare[hole] = (0, 0, 0, 0)
    gap_top = int(np.argmax(hole.any(1)))
    img = Image.fromarray(bare)
    # Everything new goes behind the art, into the hole the shoes left. That
    # way the leg we kept is never painted over, so its edge stays a single
    # clean line instead of doubling up against the copy below it.
    below = Image.new('RGBA', img.size, (0, 0, 0, 0))

    H, W = mask.shape
    regions = split_by_leg(mask, legs)
    # Which way the toes point is read along the sole, in the whole mask rather
    # than one leg's share of it. A shoe is lopsided around its ankle and that
    # lopsidedness is the only thing that says which way the foot faces; when
    # two shoes overlap, dividing them up first throws the answer away.
    floor = int(np.nonzero(mask)[0].max())
    sole = mask[max(0, floor - 7):floor + 1].any(0)
    for leg in legs:
        leg['d'], leg['toe'] = sole_reach(sole, leg['cx'])

    # A leg we could barely see was placed by its sliver of skin, which sits
    # behind the near leg rather than above its own foot. Now that the shoes
    # are divided up, move it over its own one and divide them again.
    moved = False
    for i, leg in enumerate(legs):
        if leg['donor'] is leg or not regions[i].any():
            continue
        ys, xs = np.nonzero(regions[i])
        head = xs[ys < ys.min() + 24]
        if len(head):
            leg['cx'] = float(head.min() + head.max()) / 2.0
            moved = True
    if moved:
        regions = split_by_leg(mask, legs)

    # Draw the foot that is further back first, so the near one covers it.
    order = sorted(range(len(legs)),
                   key=lambda i: (legs[i]['donor'] is legs[i],
                                  np.nonzero(regions[i])[0].max() if regions[i].any() else 0))
    lw = max(2, int(round(W / 120.0)))

    for i in order:
        region = regions[i]
        if region.sum() < 200:
            continue
        leg = legs[i]
        ys = np.nonzero(region)[0]
        yb = float(ys.max())
        ya = float(leg['ya'])
        d = leg['d']
        reach = abs(leg['toe'] - leg['cx'])
        half = (leg['donor']['x1'] - leg['donor']['x0']) / 2.0
        fh = min(1.75 * half, (yb - ya) * 0.55)
        yj = yb - fh

        shin, cxj, wa = extend_leg(src, a, opaque, skin, white, navy, leg, yj,
                                   lean_of(skin, leg['donor']), gap_top)

        layer = Image.new('RGBA', (W * SS, H * SS), (0, 0, 0, 0))
        draw = ImageDraw.Draw(layer)
        pts, (fh, L) = foot_outline(cxj, yj, wa, yb, d, reach)
        poly = [(x * SS, y * SS) for x, y in pts]
        draw.polygon(poly, fill=fill + (255,))
        draw.line(poly + [poly[0]], fill=line + (255,), width=lw * SS, joint='curve')
        for p0, p1 in toe_lines(cxj, yj, yb, d, fh, L):
            draw.line([(p0[0] * SS, p0[1] * SS), (p1[0] * SS, p1[1] * SS)],
                      fill=line + (255,), width=max(1, lw - 1) * SS)
        below.alpha_composite(layer.resize((W, H), Image.LANCZOS))
        below.alpha_composite(shin)

    if turn:
        # Put only the new work back on the untouched drawing: the body is
        # never resampled, only the shins and feet that were redrawn anyway.
        gone = Image.fromarray(np.where(hole[..., None], 0, 255).astype(np.uint8).repeat(4, 2))
        gone = np.array(gone.rotate(-turn, Image.BICUBIC))[..., 3] < 128
        kept = Image.new('RGBA', below.size, (0, 0, 0, 0))
        kept.alpha_composite(original, at)
        keep = np.array(kept)
        keep[gone] = (0, 0, 0, 0)
        img = Image.fromarray(keep)
        below = below.rotate(-turn, Image.BICUBIC)

    img = Image.alpha_composite(below, img)
    if turn:
        img = recentre(img, original.size)
    name = pose + '_barefoot'
    img.save(os.path.join(SPRITES, name + '.png'))
    write_meta(pose, name)
    print('  %s -> %s.png (%d feet)' % (pose, name, len(legs)))


def write_meta(model, name):
    """Take the pose's own import settings, so the new art is the same size.

    Character art is 120 pixels to the unit, not Unity's default 100. Letting
    Unity write the .meta itself would stand Kylo a fifth taller than the room.
    """
    out = os.path.join(SPRITES, name + '.png.meta')
    if os.path.exists(out):
        return
    base = io.open(os.path.join(SPRITES, model + '.png.meta'), encoding='utf-8').read()
    t = re.sub(r'^guid: [0-9a-f]{32}$', 'guid: ' + uuid.uuid4().hex, base, count=1, flags=re.M)
    t = t.replace(model + '_0', name + '_0')
    io.open(out, 'w', encoding='utf-8').write(t)


if __name__ == '__main__':
    print('Redrawing bare feet:')
    for pose, turn in POSES:
        convert(pose, turn)
