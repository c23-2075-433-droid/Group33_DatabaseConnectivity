"""Draw the objects Paaralan Scene 4 ("Recess") needs and nothing else has.

Scene 4 is the lesson desk from Scene 3 at break time: he packs the lesson
away, takes his baon out of his bag, eats, drinks and throws the wrapper in
the bin. Five of those six nouns already exist in Assets/Sprites - the
notebook, the textbook, the pencil, the bag and the desk are all Scene 2's
and Scene 3's, and Scene 4 reuses them exactly as they are. These are the
ones that do not:

  prop_basurahan      the classroom bin, OPEN - no lid, and seen from a
                      little above so the opening is an ellipse the child
                      can see into. The whole point of "Tapon" is watching
                      the wrapper go IN, so a bin whose mouth is edge-on to
                      the camera would lose the beat.
  prop_bote           his water bottle, standing on the desk.
  prop_bote_held      the same bottle, drawn LYING ALONG ITS LENGTH, for the
                      frames where it is in his hand on the way to his mouth
                      - see the note on angles below.
  prop_baon           his lunchbox, shut, as it comes out of the bag.
  prop_baon_open      the same box open with a sandwich in it, on the desk.
  prop_baon_empty     open and empty, after he has eaten.
  prop_sandwich       his hotdog, whole, for his hand. Still called
                      "sandwich" because the scene, the frames and the
                      Unity GUID all point at that name and a rename buys
                      nothing but broken references.
  prop_sandwich_bitten  the same hotdog with a mouthful out of the end,
                      for every frame of the eating loop.
  prop_sandwich_stub  what is left of it on the way back down.
  prop_basura         the crumpled wrapper - the thing that gets thrown.

WHY THESE ARE DRAWN RATHER THAN GENERATED. Docs/AnimationStandard.md's "a
prop is four shapes, so draw it": a generation is worth it for a drawing of a
boy and not for a bin, and redrawing in code means the size can be chosen to
fit the frame instead of fitted to whatever came back. make_classroom_props,
make_open_notebook and make_aklat_book all do the same, and the palette here
is taken from those so the new objects sit in the same light as the old ones.

TWO SIZES OF THE SAME OBJECT, which is the one thing worth knowing before
changing anything here. A prop that stands on the desk is drawn upright. A
prop that goes in his HAND is drawn along the axis his fist points down, and
long enough to reach his mouth from it, because his drawn hand is about 175
pixels to the side of his drawn mouth and nothing in this project can move it
there - see the long note in Tools/make_recess_frames.py. So the held bottle
and the sandwich are both drawn as horizontal objects: the swap rotates them
into his grip, and a drawing made upright would arrive end-on.

Writes, overwriting, into Assets/Sprites/.

Run from the repository root:  python Tools/make_recess_props.py
"""
import math

from PIL import Image, ImageChops, ImageDraw, ImageFilter

OUT = "Assets/Sprites/"

# The classroom palette, from Tools/make_classroom_props.py, plus the few
# colours these objects need that nothing else in the room has.
PAGE = (246, 241, 229)
PAGE_EDGE = (222, 214, 196)
SHADOW = (60, 40, 24, 70)
OUTLINE = (58, 48, 44)

# The bin. Grey-blue moulded plastic, which is what a school bin is and what
# sits quietly against the cream lockers behind it without becoming the thing
# you look at.
BIN = (132, 145, 155)
BIN_LIGHT = (184, 192, 192)    # where the window catches it - warmed a little
BIN_DARK = (86, 99, 111)
BIN_RIM = (108, 122, 134)
BIN_IN = (84, 93, 101)         # the far wall inside, which is lit
BIN_IN_LOW = (38, 43, 50)      # the near wall, which is not
BIN_IN_FLOOR = (28, 32, 38)    # and the bottom, a good way down

# The bottle. Clear plastic reads as a pale blue-white with a bright edge;
# the cap is the one saturated colour on it, so the object has a top.
BOTTLE = (206, 226, 236)
BOTTLE_LIGHT = (238, 248, 252)
BOTTLE_DARK = (168, 196, 212)
WATER = (156, 206, 228)
WATER_DARK = (124, 182, 210)
CAP = (58, 122, 176)
CAP_DARK = (40, 92, 138)
LABEL = (244, 248, 250)

# The lunchbox. Scene 2's bag is navy and Scene 3's textbook is deep red, so
# the baon is green - the third colour in the room, and the one that is not
# already spoken for.
BOX = (92, 150, 108)
BOX_LIGHT = (122, 180, 136)
BOX_DARK = (66, 114, 80)
BOX_IN = (226, 232, 226)

# The food: a hotdog in a bun.
#
# WHY A HOTDOG AND NOT THE SANDWICH THIS USED TO DRAW. Two things, and the
# second is the one that mattered. A sandwich seen from the side is a stack
# of flat bands, and at the ninety screen pixels this is drawn at the bands
# merge into a tan block - the old one needed a dome, a lettuce ruffle and a
# cheese overhang before it stopped reading as a painted ruler. A hotdog is
# two ROUND things lying in each other, and roundness survives being small:
# the sausage turning over along its top and the bun swelling under it are
# still legible when the detail on them is not. It is also the food a child
# in this scene would actually have in their baon.
#
# The bun browns from the top down and the sausage is the one warm red in
# the room that is not the textbook, so the two read apart at any size.
BUN_LIGHT = (252, 232, 190)       # the top of the bun, facing the window
BUN_PALE = (246, 214, 156)
BUN_BODY = (232, 188, 126)
BUN_TOAST = (198, 143, 82)        # the underside, where it sat on the pan
BUN_BACK_LIGHT = (224, 176, 118)  # the far wall, which the sausage shades
BUN_BACK = (206, 156, 100)
BUN_BACK_DARK = (176, 126, 76)
BUN_CRUMB = (250, 236, 206)       # what a bite exposes
BUN_TOAST_DEEP = (168, 112, 58)
BUN_CREASE = (158, 106, 58)       # where the sausage shadows the bun
BUN_SHEEN = (255, 246, 216)
SAUSAGE_TOP = (238, 152, 118)
SAUSAGE_LIGHT = (218, 116, 86)
SAUSAGE = (188, 80, 56)
SAUSAGE_DARK = (132, 48, 34)
SAUSAGE_GLOSS = (255, 212, 186, 215)
SAUSAGE_CHAR = (104, 36, 28)      # the grill marks
SAUSAGE_CUT = (226, 158, 134)     # the cross-section a bite leaves
SAUSAGE_CUT_DARK = (178, 96, 74)
MUSTARD = (250, 198, 44)
MUSTARD_DARK = (206, 148, 22)

# The canvas every length of the hotdog is drawn on. One size for all three
# so a bite cannot move his fist - see the note in _hotdog.
#
# NEARLY FOUR TO ONE, and the ratio is forced rather than chosen. His drawn
# fist is 189 pixels from his drawn mouth and nothing in this project can
# move it closer, so the food has to be long enough to span that gap from
# wherever he is holding it - about 260 pixels of hotdog. The only free
# choice left is how DEEP 260 pixels of it are, and the first pass drew it
# at three to one: 87 pixels deep, which at his mouth covered his chin, his
# jaw and the bottom half of the open mouth it was going into. A hotdog in
# a bun is about three and a half to one in life and reads thinner still
# because the sausage tapers past the bread, so four to one is both honest
# and the thing that lets the mouth under it be seen.
W_DOG, H_DOG = 480, 122

# The wrapper. A crumpled snack wrapper, warm red.
#
# NOT PALE PAPER, which is what the first three passes drew. It has to read
# in THREE places and pale paper only works in one of them: in his fist it is
# against his WHITE SHIRT, on the desk it is against pale wood, and in the
# bin it is against near-black. A near-white object disappears into the first
# two and shouts in the third. A mid-value warm red has contrast against all
# three, and a red biscuit wrapper is what is actually in a child's baon.
WRAP = (212, 94, 76)
WRAP_SHADE = (176, 68, 54)
WRAP_DARK = (144, 50, 40)
WRAP_LIGHT = (240, 148, 126)
WRAP_PRINT = (248, 240, 228)


def shadow_under(img, box, blur=9, inset=6, drop=10):
    """A soft contact shadow, so the prop sits on the desk instead of floating."""
    layer = Image.new("RGBA", img.size, (0, 0, 0, 0))
    d = ImageDraw.Draw(layer)
    x0, y0, x1, y1 = box
    d.ellipse([x0 + inset, y1 - 14, x1 - inset, y1 + drop], fill=SHADOW)
    layer = layer.filter(ImageFilter.GaussianBlur(blur))
    return Image.alpha_composite(layer, img)


def outline(d, shape, kind="polygon", width=5, **kw):
    """Draw a shape and give it the character art's dark edge.

    Every drawing in this project is outlined - the boy, the bag, the books -
    and an un-outlined prop beside them reads as pasted on from somewhere
    else. One helper so the width cannot drift between objects.
    """
    getattr(d, kind)(shape, outline=OUTLINE, width=width, **kw)


# ---------------------------------------------------------------- the bin

def make_bin():
    """
    The classroom bin: no lid, wide open mouth, seen from above the rim.

    THE CAMERA ANGLE IS THE WHOLE DESIGN. Scene 3's view looks down on the
    desk far enough to see its top surface, so the bin beside it is seen from
    the same height and its mouth is a wide ellipse rather than a line. That
    ellipse is drawn dark, with a darker floor inside it, so a pale wrapper
    landing in it is plainly INSIDE something.

    It tapers towards the bottom, which is how a stacking bin is moulded and
    also what stops the shape reading as a bucket.
    """
    W, H = 460, 640
    img = Image.new("RGBA", (W, H), (0, 0, 0, 0))
    d = ImageDraw.Draw(img)

    rim_cy = 112               # the centre of the opening
    rim_rx, rim_ry = 196, 74   # how open the mouth looks - see above
    foot_y = H - 70
    foot_rx = 146

    left_top, right_top = W // 2 - rim_rx, W // 2 + rim_rx
    left_bot, right_bot = W // 2 - foot_rx, W // 2 + foot_rx

    # --- the body, a tapering tub, shaded as a CYLINDER ---
    #
    # NOT TWO FLAT SIDE PANELS, which is what the first pass did and which
    # came out as a grey slab with a lighter stripe down one edge. A round
    # thing has no edges to its shading: the value runs continuously from
    # dark on the shadow side, through the base colour, to a bright band
    # where the window catches it, and back down. Sixteen vertical slices of
    # the tub, each one a step along that ramp, is the cheapest way to say so
    # and reads as round at any size.
    ramp = [BIN_DARK, BIN, BIN_LIGHT, BIN]
    stops = [0.0, 0.42, 0.74, 1.0]            # where each lands across the tub

    def shade(t):
        """The colour t of the way across the tub, interpolated along `ramp`."""
        for i in range(len(stops) - 1):
            if t <= stops[i + 1]:
                k = (t - stops[i]) / (stops[i + 1] - stops[i])
                a, b = ramp[i], ramp[i + 1]
                return tuple(int(a[c] + (b[c] - a[c]) * k) for c in range(3))
        return ramp[-1]

    # Enough slices that the steps between them are invisible. Twenty left
    # faint vertical banding down the lower half of the tub, where the body
    # is narrow and each slice is therefore wide in value terms.
    slices = 64
    for i in range(slices):
        t0, t1 = i / slices, (i + 1) / slices
        d.polygon([(left_top + (right_top - left_top) * t0, rim_cy),
                   (left_top + (right_top - left_top) * t1, rim_cy),
                   (left_bot + (right_bot - left_bot) * t1, foot_y),
                   (left_bot + (right_bot - left_bot) * t0, foot_y)],
                  fill=shade((t0 + t1) / 2))

    # Two moulded ribs round the tub, drawn as a dark line with a light one
    # under it - a groove has both a shadow and a lip, and one line alone
    # vanished against the shading above.
    for t in (0.40, 0.68):
        y = rim_cy + (foot_y - rim_cy) * t
        rx = rim_rx + (foot_rx - rim_rx) * t
        d.arc([W // 2 - rx, y - 30, W // 2 + rx, y + 30], 0, 180,
              fill=BIN_DARK, width=6)
        d.arc([W // 2 - rx, y - 22, W // 2 + rx, y + 38], 0, 180,
              fill=BIN_LIGHT, width=4)

    # The rounded base, so it stands on something.
    d.ellipse([left_bot, foot_y - 34, right_bot, foot_y + 34], fill=BIN_DARK)
    d.ellipse([left_bot, foot_y - 40, right_bot, foot_y + 26], fill=BIN)

    # --- the inside, seen through the mouth ---
    #
    # THREE DEPTHS, NOT ONE FLAT DISC. A single dark ellipse is a hole cut in
    # a piece of card; what makes it a container is that the FAR wall is lit
    # and the near one is not, so the darkness has a shape. The back wall is
    # the top crescent, a good deal lighter than the floor at the bottom.
    inner = [W // 2 - rim_rx + 14, rim_cy - rim_ry + 12,
             W // 2 + rim_rx - 14, rim_cy + rim_ry - 10]
    d.ellipse(inner, fill=BIN_IN_LOW)
    # The lit far wall, a crescent across the top of the opening. ONE chord
    # and no second one cutting it back: the first pass drew an inset chord
    # in the near-wall colour over the top of this and ate all but a hairline
    # of it, so the opening went back to being a flat dark disc.
    d.chord(inner, 182, 358, fill=BIN_IN)
    # And the floor, a good way down, so the opening has depth in it.
    d.ellipse([W // 2 - rim_rx + 62, rim_cy + 6,
               W // 2 + rim_rx - 62, rim_cy + rim_ry - 16], fill=BIN_IN_FLOOR)

    # --- the rim itself, drawn last so it sits over both ---
    d.ellipse([W // 2 - rim_rx, rim_cy - rim_ry, W // 2 + rim_rx, rim_cy + rim_ry],
              outline=BIN_RIM, width=18)
    # The near lip catches the window light and the far one does not, which
    # is what stops the rim reading as a flat ring drawn on top.
    d.arc([W // 2 - rim_rx + 4, rim_cy - rim_ry + 4,
           W // 2 + rim_rx - 4, rim_cy + rim_ry - 4], 10, 170,
          fill=BIN_LIGHT, width=9)
    d.arc([W // 2 - rim_rx + 4, rim_cy - rim_ry + 4,
           W // 2 + rim_rx - 4, rim_cy + rim_ry - 4], 196, 344,
          fill=BIN_DARK, width=7)
    d.ellipse([W // 2 - rim_rx, rim_cy - rim_ry, W // 2 + rim_rx, rim_cy + rim_ry],
              outline=OUTLINE, width=5)
    d.ellipse(inner, outline=OUTLINE, width=4)

    # The body's own outline, over the top of the shading.
    d.line([(left_top, rim_cy), (left_bot, foot_y)], fill=OUTLINE, width=5)
    d.line([(right_top, rim_cy), (right_bot, foot_y)], fill=OUTLINE, width=5)
    d.arc([left_bot, foot_y - 40, right_bot, foot_y + 34], 0, 180,
          fill=OUTLINE, width=5)

    img = shadow_under(img, (left_bot, rim_cy, right_bot, foot_y + 30),
                       blur=13, inset=-10, drop=18)
    img.save(OUT + "prop_basurahan.png")
    return img.size


# ------------------------------------------------------------- the bottle

def _bottle_body(d, x0, y0, x1, y1):
    """A water bottle drawn upright inside the given box."""
    w = x1 - x0
    neck_w = int(w * 0.42)
    nx0 = x0 + (w - neck_w) // 2
    nx1 = nx0 + neck_w
    cap_h = int((y1 - y0) * 0.09)
    neck_y = y0 + cap_h + int((y1 - y0) * 0.06)
    shoulder_y = neck_y + int((y1 - y0) * 0.09)

    # The body, with the water inside it and an air gap at the top.
    d.rounded_rectangle([x0, shoulder_y - 10, x1, y1], radius=26, fill=BOTTLE)
    water_top = shoulder_y + int((y1 - shoulder_y) * 0.22)
    d.rounded_rectangle([x0 + 7, water_top, x1 - 7, y1 - 6], radius=20, fill=WATER)
    d.rounded_rectangle([x0 + 7, y1 - 40, x1 - 7, y1 - 6], radius=18, fill=WATER_DARK)

    # The shoulder, from the body up to the neck.
    d.polygon([(x0, shoulder_y + 6), (nx0, neck_y), (nx1, neck_y),
               (x1, shoulder_y + 6)], fill=BOTTLE)
    d.rectangle([nx0, y0 + cap_h, nx1, neck_y + 4], fill=BOTTLE)

    # A plain white label band - the room has enough going on without text on
    # a bottle the size of a thumbnail.
    d.rounded_rectangle([x0 + 4, water_top + 34, x1 - 4, water_top + 110],
                        radius=10, fill=LABEL)
    d.line([(x0 + 4, water_top + 34), (x1 - 4, water_top + 34)],
           fill=PAGE_EDGE, width=3)
    d.line([(x0 + 4, water_top + 110), (x1 - 4, water_top + 110)],
           fill=PAGE_EDGE, width=3)

    # Round it off: a bright edge on the window side, a shaded one away.
    d.rounded_rectangle([x1 - 24, shoulder_y + 4, x1 - 6, y1 - 14],
                        radius=9, fill=BOTTLE_DARK)
    d.rounded_rectangle([x0 + 8, shoulder_y + 4, x0 + 24, y1 - 14],
                        radius=9, fill=BOTTLE_LIGHT)

    # The cap.
    d.rounded_rectangle([nx0 - 8, y0, nx1 + 8, y0 + cap_h], radius=7, fill=CAP)
    d.rounded_rectangle([nx0 - 8, y0 + cap_h - 7, nx1 + 8, y0 + cap_h],
                        radius=5, fill=CAP_DARK)
    for rx in range(nx0 - 4, nx1 + 6, 10):
        d.line([(rx, y0 + 4), (rx, y0 + cap_h - 8)], fill=CAP_DARK, width=2)

    # Outlines last.
    d.rounded_rectangle([x0, shoulder_y - 10, x1, y1], radius=26,
                        outline=OUTLINE, width=5)
    d.line([(x0, shoulder_y + 4), (nx0, neck_y)], fill=OUTLINE, width=5)
    d.line([(x1, shoulder_y + 4), (nx1, neck_y)], fill=OUTLINE, width=5)
    d.line([(nx0, y0 + cap_h), (nx0, neck_y + 2)], fill=OUTLINE, width=5)
    d.line([(nx1, y0 + cap_h), (nx1, neck_y + 2)], fill=OUTLINE, width=5)
    d.rounded_rectangle([nx0 - 8, y0, nx1 + 8, y0 + cap_h], radius=7,
                        outline=OUTLINE, width=5)


def make_bottle():
    """The bottle standing on the desk, cap up."""
    W, H = 220, 560
    img = Image.new("RGBA", (W, H), (0, 0, 0, 0))
    d = ImageDraw.Draw(img)
    _bottle_body(d, 26, 24, W - 26, H - 40)
    img = shadow_under(img, (26, 24, W - 26, H - 40))
    img.save(OUT + "prop_bote.png")
    return img.size


def make_bottle_held():
    """
    The same bottle lying along its length, cap to the RIGHT.

    This is the one that goes in his hand. See the note at the top: his fist
    is drawn well to the side of his mouth, so the bottle has to span the gap,
    which means it is drawn horizontally and rotated into the grip rather than
    stood upright and tipped. Cap to the right because his mouth is to the
    right of the hand that holds it.

    No contact shadow on this one - it is in the air, not on a surface, and a
    shadow under a held object is the kind of thing that reads as grime.
    """
    W, H = 560, 220
    upright = Image.new("RGBA", (220, 560), (0, 0, 0, 0))
    _bottle_body(ImageDraw.Draw(upright), 26, 24, 220 - 26, 560 - 40)
    img = Image.new("RGBA", (W, H), (0, 0, 0, 0))
    img.alpha_composite(upright.rotate(-90, expand=True), (0, 0))
    img.save(OUT + "prop_bote_held.png")
    return img.size


# ----------------------------------------------------------- the lunchbox

def _ramp_colour(stops, t):
    """The colour `t` of the way along a list of (position, colour) stops."""
    for k in range(len(stops) - 1):
        p0, c0 = stops[k]
        p1, c1 = stops[k + 1]
        if t <= p1 or k == len(stops) - 2:
            u = 0.0 if p1 <= p0 else (t - p0) / (p1 - p0)
            u = min(1.0, max(0.0, u))
            return tuple(int(round(c0[j] + (c1[j] - c0[j]) * u)) for j in range(3))
    return stops[-1][1]


def _shaded(size, box, stops):
    """A vertical colour ramp, as an image the size of the whole canvas.

    make_bin() shades its tub by drawing sixty-four vertical slices, each one
    a step along a list of colours, and that works because a tub is a
    polygon with straight sides. A sausage and a bun are CAPSULES - rounded
    at both ends - and a rectangular slice would square those ends off.

    So the same ramp is built as a picture instead and pasted THROUGH the
    capsule's own shape. Identical shading, any silhouette, and the rounded
    end keeps its colour from the row it is on rather than from the slice it
    happens to fall in.
    """
    x0, y0, x1, y1 = box
    h = max(1, int(y1 - y0))
    strip = Image.new("RGB", (1, h))
    px = strip.load()
    for i in range(h):
        px[0, i] = _ramp_colour(stops, i / max(1, h - 1))
    out = Image.new("RGB", size, stops[0][1])
    out.paste(strip.resize((size[0], h), Image.BILINEAR), (0, int(y0)))
    return out


def _capsule_mask(size, box, grow=0):
    """A capsule - a rectangle with semicircular ends - as an alpha mask."""
    x0, y0, x1, y1 = box
    m = Image.new("L", size, 0)
    r = (y1 - y0) / 2.0 + grow
    ImageDraw.Draw(m).rounded_rectangle(
        [x0 - grow, y0 - grow, x1 + grow, y1 + grow], radius=r, fill=255)
    return m


def _lay(img, box, stops, grow=0):
    """Paste a shaded capsule onto `img`, and hand back its mask."""
    m = _capsule_mask(img.size, box, grow)
    img.paste(_shaded(img.size, box, stops), (0, 0), m)
    return m


def _stroke_capsule(d, box, width):
    """The character art's dark edge, round a capsule."""
    x0, y0, x1, y1 = box
    d.rounded_rectangle([x0, y0, x1, y1], radius=(y1 - y0) / 2.0,
                        outline=OUTLINE, width=width)


def _union(*masks):
    """One mask covering all of them."""
    out = masks[0].copy()
    for m in masks[1:]:
        out = ImageChops.lighter(out, m)
    return out


def _bun_mask(size, x0, x1, y_mid, y_end, y_bot, right_end=True):
    """The near wall of a split bun: a loaf whose ENDS RISE round the sausage.

    THE TOP LINE OF A BUN IS NOT STRAIGHT, and the first pass of this drawing
    found out what happens when it is. A plain capsule has a flat top with
    the ends curving DOWN, so a sausage laid over one came out as a red bar
    resting on a tan bar - two stacked slabs, which is a pencil case.

    A sliced bun does the opposite: the cut faces at the two ends stand UP
    round the sausage, and the middle of the bun is where it sags. That one
    curve is most of what says the sausage is IN something.

    It is built as a union rather than drawn as one outline, because the
    shape is exactly a capsule with a bigger circle stuck on each end, and a
    union of three masks is shorter to read than eighty points of polygon.
    `right_end` drops the right circle, for the end a mouthful has gone from.
    """
    body = _capsule_mask(size, (x0, y_mid, x1, y_bot))
    r = (y_bot - y_end) / 2.0
    parts = [body]
    cl = Image.new("L", size, 0)
    ImageDraw.Draw(cl).ellipse([x0, y_bot - 2 * r, x0 + 2 * r, y_bot], fill=255)
    parts.append(cl)
    if right_end:
        cr = Image.new("L", size, 0)
        ImageDraw.Draw(cr).ellipse([x1 - 2 * r, y_bot - 2 * r, x1, y_bot], fill=255)
        parts.append(cr)
    return _union(*parts)


def _through(img, layer, mask, blur=0):
    """Composite `layer` onto `img`, clipped to `mask`.

    Everything soft in this drawing - the gloss on the sausage, the shadow
    it casts into the bun, the toasting underneath - is painted on its own
    transparent sheet and then pushed through the shape it belongs to. Drawn
    straight onto the picture instead, a blurred streak runs off the rounded
    end of the thing it is meant to be lying on, and what that looks like is
    a scratch hanging in the air beside the food.
    """
    if blur:
        layer = layer.filter(ImageFilter.GaussianBlur(blur))
    img.paste(layer, (0, 0),
              Image.composite(layer.split()[3], Image.new("L", img.size, 0), mask))


def _outline_mask(d_size, mask, width):
    """The dark edge of a shape, as a mask: the shape minus itself eroded."""
    inner = mask.filter(ImageFilter.MinFilter(max(3, int(width) | 1)))
    return ImageChops.subtract(mask, inner)


def _hotdog(box_w, box_h, eaten=0.0, ss=3):
    """A hotdog in a split bun, lying along its length, ANCHORED LEFT.

    WHY A HOTDOG IS DRAWN DIFFERENTLY FROM THE SANDWICH IT REPLACES. The
    sandwich this file used to make was three flat bands with a domed lid -
    a stack of slabs, which is what a sandwich is. A hotdog is not a stack.
    It is two ROUND things lying in each other, and everything that makes it
    read as food at ninety screen pixels is roundness: the sausage turning
    over along its top, the bun swelling under it, the shadow one casts into
    the other. Flat bands give a yellow stripe on a tan stripe.

    So nothing here is a flat fill. Each piece is a shape filled through a
    VERTICAL COLOUR RAMP - bright along the top where the classroom window
    is, the body colour across the middle, dark underneath - and the pieces
    are stacked so the bun hides the sausage's lower half:

      the far bun wall   darker, behind everything, showing as a rim above
                         the bun line where the sausage curves down towards
                         its ends. One sliver of it is the difference
                         between a sausage IN a bun and one lying on top.
      the sausage        poking out past both ends of the bun, because a
                         hotdog that stops where its bun stops is a
                         baguette. Grill marks across the lit half only - a
                         mark on the shaded underside of a round thing is
                         not visible from here - and one long highlight.
      the mustard        a zigzag down the length. The one saturated yellow
                         in the scene, and what breaks the two long parallel
                         lines the sausage and the bun would otherwise draw.
      the near bun wall  over the sausage's lower half, with its two cut
                         ends standing up round it (see _bun_mask), a
                         toasted underside, and the sausage's own shadow
                         laid into the crease where they meet.

    LEFT-ANCHORED, which is what lets a bite change the drawing without
    moving his hand. Tools/make_recess_frames.py places this canvas by its
    CENTRE, so if the food were centred too, a shorter hotdog would pull its
    own held end out of his fist and the fist would close on air. Drawn from
    the left margin instead, the held end is at the same canvas pixel in all
    three states and only the mouth end moves - which is the end the bites
    are coming out of.

    `eaten` is how much of the length is gone, 0 to 1. Anything above zero
    gives the right-hand end a bitten edge instead of a round one.

    DRAWN AT THREE TIMES SIZE AND REDUCED. Every curve here is either a
    circle or a blur, and both alias badly at the size the canvas is kept
    at. Supersampling is cheaper than anti-aliasing each of them by hand.
    """
    S = ss
    W, H = box_w * S, box_h * S
    img = Image.new("RGBA", (W, H), (0, 0, 0, 0))

    m = int(8 * S)
    full = W - 2 * m
    body = full * (1.0 - eaten)
    left = m
    right = m + body
    bitten = eaten > 0.001

    poke = 0.060 * full                 # how far the sausage clears the bun
    bun_x0 = left + poke
    bun_x1 = (right - poke) if not bitten else right
    line_w = max(2, int(round(1.8 * S)))

    def fy(t):
        return H * t

    sau_box = (left, fy(0.05), right, fy(0.56))
    back_box = (bun_x0 + poke * 0.5, fy(0.26), bun_x1 - poke * 0.5, fy(0.76))
    bun_mid, bun_end, bun_bot = fy(0.49), fy(0.425), fy(0.95)

    zero = Image.new("L", (W, H), 0)

    def sheet():
        return Image.new("RGBA", (W, H), (0, 0, 0, 0))

    def ink(mask, width=None):
        """Lay the character art's dark edge round a mask."""
        edge = _outline_mask((W, H), mask, width or (line_w * 2 + 1))
        img.paste(Image.new("RGBA", (W, H), OUTLINE + (255,)), (0, 0), edge)

    # --- the far wall of the bun -------------------------------------
    back = _capsule_mask((W, H), back_box)
    img.paste(_shaded((W, H), back_box,
                      [(0.0, BUN_BACK_LIGHT), (0.5, BUN_BACK), (1.0, BUN_BACK_DARK)]),
              (0, 0), back)
    ink(back)

    # --- the sausage --------------------------------------------------
    sau = _capsule_mask((W, H), sau_box)
    img.paste(_shaded((W, H), sau_box,
                      [(0.0, SAUSAGE_TOP), (0.16, SAUSAGE_LIGHT),
                       (0.44, SAUSAGE), (0.80, SAUSAGE_DARK), (1.0, SAUSAGE_DARK)]),
              (0, 0), sau)

    sy0, sy1 = sau_box[1], sau_box[3]
    # Grill marks. Slanted hard, because a mark that is nearly vertical on a
    # nearly horizontal sausage reads as a seam in the plastic - which is
    # what the first pass of these looked like at game size.
    marks = sheet()
    md = ImageDraw.Draw(marks)
    for i in range(6):
        mx = left + body * (0.12 + 0.148 * i)
        md.line([(mx - 7.0 * S, sy0 + (sy1 - sy0) * 0.10),
                 (mx + 7.0 * S, sy0 + (sy1 - sy0) * 0.62)],
                fill=SAUSAGE_CHAR + (120,), width=int(3.4 * S))
    _through(img, marks, sau, blur=1.6 * S)

    # The long highlight along the top, where the sausage turns over.
    gloss = sheet()
    gd = ImageDraw.Draw(gloss)
    gy = sy0 + (sy1 - sy0) * 0.21
    gd.rounded_rectangle([left + 0.12 * body, gy - 2.2 * S,
                          right - 0.13 * body, gy + 2.2 * S],
                         radius=2.2 * S, fill=SAUSAGE_GLOSS)
    _through(img, gloss, sau, blur=1.8 * S)

    # --- the mustard --------------------------------------------------
    zig = sheet()
    zd = ImageDraw.Draw(zig)
    pts = []
    n = 11
    for i in range(n + 1):
        t = i / n
        zx = left + 0.06 * body + body * 0.88 * t
        zy = sy0 + (sy1 - sy0) * (0.16 if i % 2 == 0 else 0.52)
        pts.append((zx, zy))
    zd.line(pts, fill=MUSTARD, width=int(4.2 * S), joint="curve")
    for p in pts:
        zd.ellipse([p[0] - 2.1 * S, p[1] - 2.1 * S,
                    p[0] + 2.1 * S, p[1] + 2.1 * S], fill=MUSTARD)
    # A darker underside on the zigzag, so the mustard is a bead sitting on
    # the sausage rather than a decal printed on it.
    shade = sheet()
    sd = ImageDraw.Draw(shade)
    sd.line([(x, y + 1.6 * S) for x, y in pts], fill=MUSTARD_DARK + (190,),
            width=int(4.2 * S), joint="curve")
    _through(img, shade, sau, blur=1.1 * S)
    _through(img, zig, sau)
    ink(sau)

    # --- the near wall of the bun ---------------------------------------
    bun = _bun_mask((W, H), bun_x0, bun_x1, bun_mid, bun_end, bun_bot,
                    right_end=not bitten)
    img.paste(_shaded((W, H), (0, bun_end, 0, bun_bot),
                      [(0.0, BUN_LIGHT), (0.20, BUN_PALE),
                       (0.58, BUN_BODY), (1.0, BUN_TOAST)]),
              (0, 0), bun)

    # The toasted underside: a soft dark band low in the loaf, so it browns
    # where it sat on the pan instead of all over.
    crust = sheet()
    ImageDraw.Draw(crust).rounded_rectangle(
        [bun_x0 + 3 * S, bun_mid + (bun_bot - bun_mid) * 0.58,
         bun_x1 - 3 * S, bun_bot + 3 * S],
        radius=(bun_bot - bun_mid) * 0.3, fill=BUN_TOAST_DEEP + (175,))
    _through(img, crust, bun, blur=3.4 * S)

    # The shadow the sausage casts into the bun, in the crease where the two
    # meet. Without it the sausage sits ON the bun; with it, it sits IN it.
    crease = sheet()
    ImageDraw.Draw(crease).rounded_rectangle(
        [bun_x0 + 6 * S, bun_mid - 5 * S, bun_x1 - 6 * S, bun_mid + 9 * S],
        radius=7 * S, fill=BUN_CREASE + (205,))
    _through(img, crease, bun, blur=3.0 * S)

    # And a pale lip along the very top of the cut face, which is the one
    # part of a bun that catches a hard light.
    lip = sheet()
    ImageDraw.Draw(lip).rounded_rectangle(
        [bun_x0 + 14 * S, bun_mid + 12 * S, bun_x1 - 14 * S, bun_mid + 16 * S],
        radius=2 * S, fill=BUN_SHEEN + (95,))
    _through(img, lip, bun, blur=3.4 * S)
    ink(bun)

    # --- the bite -------------------------------------------------------
    if bitten:
        img = _bite_off(img, right, fy(0.05), bun_bot, full, line_w,
                        (sy0, sy1), S)

    return img.resize((box_w, box_h), Image.LANCZOS)


def _bite_off(img, right, y0, y1, full, line_w, sausage_band, S):
    """Take a mouthful out of the right-hand end.

    THE SILHOUETTE IS WHAT SELLS IT. A hotdog that has simply been drawn
    shorter is a shorter hotdog; one whose end is CONCAVE, in a couple of
    rounded scoops, is one somebody has had a bite of. So the edge is a
    cosine with a lobe and a quarter in it, the picture is cut off along
    that line, and what the cut exposes is painted in behind it: pale dry
    crumb where the bun was and a ring of cut meat where the sausage was.

    Done with masks rather than shapes, for the same reason the pieces are
    shaded through masks: the exposed face has to follow the scallops
    exactly, and a polygon approximating them leaves a hairline of the
    outside colour showing past the cut.
    """
    W, H = img.size
    depth = 0.105 * full

    def x_at(t):
        return right - depth * (0.5 + 0.5 * math.cos(2.0 * math.pi * 2.1 * t + 0.6))

    curve = [(x_at(i / 72.0), y0 + (y1 - y0) * (i / 72.0)) for i in range(73)]
    skirt = [(0, y1 + 8 * S), (0, y0 - 8 * S)]

    def side(dx):
        m = Image.new("L", (W, H), 0)
        ImageDraw.Draw(m).polygon(
            [(0, y0 - 8 * S)] + [(x + dx, y) for x, y in curve] + skirt, fill=255)
        return m

    keep = side(0)
    inner = side(-11.0 * S)

    # What the bite exposed, as a flat picture the cut band is taken from.
    face = Image.new("RGBA", (W, H), BUN_CRUMB + (255,))
    fd = ImageDraw.Draw(face)
    sy0, sy1 = sausage_band
    fd.rectangle([0, sy0 + 2 * S, W, sy1], fill=SAUSAGE_CUT)
    fd.rectangle([0, sy0 + 2 * S, W, sy0 + 7 * S], fill=SAUSAGE_CUT_DARK)
    fd.rectangle([0, sy1 - 5 * S, W, sy1], fill=SAUSAGE_CUT_DARK)

    solid = img.split()[3].point(lambda v: 255 if v > 40 else 0)
    band = ImageChops.subtract(keep, inner)
    out = img.copy()
    out.paste(face, (0, 0), ImageChops.multiply(band, solid))

    # Cut the silhouette, then run the dark edge back along the cut.
    out.putalpha(ImageChops.multiply(out.split()[3], keep))
    edge = Image.new("RGBA", (W, H), (0, 0, 0, 0))
    ImageDraw.Draw(edge).line(curve, fill=OUTLINE + (255,),
                              width=line_w * 2, joint="curve")
    out.paste(edge, (0, 0), ImageChops.multiply(
        edge.split()[3], out.split()[3].point(lambda v: 255 if v > 40 else 0)))
    return out


def make_hotdog():
    """Three lengths of the same hotdog, for his hand.

    Drawn horizontally for the same reason the held bottle is - it is
    rotated into his grip by Tools/make_recess_frames.py and has to reach
    from his fist to his mouth.

    THREE OF THEM BECAUSE HE IS EATING IT. The scene used to put one whole
    sandwich at his mouth, loop it forever, and then hand him half of one on
    the way down. A hotdog that is still whole on the ninth bite is the same
    problem rule 5 is about at the other end: food that does not change is
    food nobody is eating. So the lead-in holds the whole one, every frame of
    the loop holds the bitten one, and the tail holds the stub.

    THE CANVAS DOES NOT CHANGE SIZE between them, and the drawing stays
    pinned to its left margin, so his fist closes on the same canvas pixel in
    all three. See the note in _hotdog.
    """
    sizes = {}
    # TWELVE PER CENT, AND THE NUMBER IS NOT ABOUT APPETITE. The bitten one
    # has to reach his mouth from the SAME DRAWN FIST as the whole one, and
    # the only thing that can give is where along the food his hand is -
    # 25% of the way up the whole one, 15% of the way up the bitten one,
    # which is a regrip between mouthfuls and is what people do. Past about
    # an eighth gone there is no grip left that reaches, and the frame comes
    # out as a boy biting the air six inches short of his lunch. The stub
    # never goes near his mouth - it is the frame his hand comes DOWN on -
    # so it can lose as much as it likes.
    for name, eaten in (("prop_sandwich", 0.0),
                        ("prop_sandwich_bitten", 0.12),
                        ("prop_sandwich_stub", 0.45)):
        img = _hotdog(W_DOG, H_DOG, eaten)
        img.save(OUT + name + ".png")
        sizes[name] = img.size
    return sizes


def _box_shell(d, x0, y0, x1, y1, open_lid):
    """The lunchbox seen from a little above, shut or open."""
    front_y = y0 + int((y1 - y0) * 0.44)

    if open_lid:
        # The lid folded back on its hinge. It is drawn FROM the back edge of
        # the tray rather than floating above it - the first pass left a gap
        # between the two and the lid read as a second object parked behind
        # the box.
        # LEANING AWAY AND SHOWING ITS INSIDE, which the first pass did
        # not. That one stood the lid up as a plain green rectangle, and at
        # the size the game draws this it read as a second panel parked
        # behind the box with no way to tell it was attached. A lid folded
        # back past the vertical says three things at once: it is narrower
        # at the top, because it is leaning away; its UNDERSIDE is in view
        # and is a different green from its outside; and the hinge barrels
        # break the line where it meets the tray.
        hinge_l, hinge_r = x0 + 26, x1 - 26
        lid_top = y0 - 104
        d.polygon([(hinge_l, y0 + 2), (hinge_r, y0 + 2),
                   (x1 - 62, lid_top), (x0 + 62, lid_top)], fill=BOX_DARK)
        d.polygon([(hinge_l + 22, y0 - 10), (hinge_r - 22, y0 - 10),
                   (x1 - 76, lid_top + 22), (x0 + 76, lid_top + 22)], fill=BOX)
        d.polygon([(hinge_l, y0 + 2), (hinge_r, y0 + 2),
                   (x1 - 62, lid_top), (x0 + 62, lid_top)],
                  outline=OUTLINE, width=5)
        d.polygon([(hinge_l + 22, y0 - 10), (hinge_r - 22, y0 - 10),
                   (x1 - 76, lid_top + 22), (x0 + 76, lid_top + 22)],
                  outline=OUTLINE, width=4)
        # The hinge itself, two barrels at the fold. Big enough to see.
        for hx in (hinge_l + 30, hinge_r - 30):
            d.rounded_rectangle([hx - 22, y0 - 16, hx + 22, y0 + 10],
                                radius=9, fill=BOX_LIGHT, outline=OUTLINE,
                                width=4)
        # The open tray: a dark rim and a pale moulded inside, deep enough
        # that the inside is a tray and not a line.
        d.polygon([(x0, front_y), (x0 + 30, y0), (x1 - 30, y0), (x1, front_y)],
                  fill=BOX_DARK)
        d.polygon([(x0 + 16, front_y + 6), (x0 + 40, y0 + 14),
                   (x1 - 40, y0 + 14), (x1 - 16, front_y + 6)], fill=BOX_IN)
        # A shadow inside the far wall of the tray, so it has a depth.
        d.polygon([(x0 + 40, y0 + 14), (x1 - 40, y0 + 14),
                   (x1 - 46, y0 + 34), (x0 + 46, y0 + 34)], fill=(202, 210, 202))
        d.polygon([(x0 + 16, front_y + 6), (x0 + 40, y0 + 14),
                   (x1 - 40, y0 + 14), (x1 - 16, front_y + 6)],
                  outline=OUTLINE, width=4)
    else:
        # The shut lid, its top face catching the window light.
        d.polygon([(x0, front_y), (x0 + 30, y0), (x1 - 30, y0), (x1, front_y)],
                  fill=BOX_LIGHT)
        d.line([(x0 + 22, front_y - 10), (x0 + 48, y0 + 12)],
               fill=BOX, width=4)

    # The front of the box, under either.
    d.rounded_rectangle([x0, front_y, x1, y1], radius=14, fill=BOX)
    d.rounded_rectangle([x0, y1 - 26, x1, y1], radius=12, fill=BOX_DARK)
    # The catch on the front, which is what says "lunchbox" rather than "tub".
    d.rounded_rectangle([(x0 + x1) // 2 - 34, front_y + 10,
                         (x0 + x1) // 2 + 34, front_y + 44],
                        radius=8, fill=BOX_LIGHT, outline=OUTLINE, width=4)

    d.rounded_rectangle([x0, front_y, x1, y1], radius=14, outline=OUTLINE, width=5)
    d.polygon([(x0, front_y), (x0 + 30, y0), (x1 - 30, y0), (x1, front_y)],
              outline=OUTLINE, width=5)


def make_lunchbox():
    """Three states of the baon: shut, open with food, open and empty.

    THE SHUT ONE IS DEEP AND THE OPEN ONES ARE SHALLOW, and that is a
    constraint from the animation rather than a choice about lunchboxes.

    The shut box is the one he CARRIES, and Tools/make_recess_frames.py gets
    it into his hands by laying it over the notepad that Paaralan Scene 2
    drew there. That notepad's footprint is 246 wide by 284 tall - an upright
    rectangle - and the new object has to cover it, or a white rim of pad is
    left showing round a green box, which is exactly what the first pass of
    those frames looked like. A wide flat lunchbox scaled until it is 284
    tall is 341 across, wider than his shoulders.

    So the shut box is drawn nearly square, like a deep tub with a clip-on
    lid, which is a real lunchbox and fits an upright footprint. The open
    ones only ever sit on the desk, where nothing constrains them, so they
    stay wide and shallow and show what is inside.
    """
    sizes = {}

    # Shut: deep, and on a canvas with little margin, so the sizes the frame
    # tool asks for are nearly the size of the drawing itself.
    W, H = 380, 400
    img = Image.new("RGBA", (W, H), (0, 0, 0, 0))
    x0, y0, x1, y1 = 20, 40, W - 20, H - 44
    _box_shell(ImageDraw.Draw(img), x0, y0, x1, y1, False)
    img = shadow_under(img, (x0, y0, x1, y1))
    img.save(OUT + "prop_baon.png")
    sizes["prop_baon"] = img.size

    for name, food in (("prop_baon_open", True), ("prop_baon_empty", False)):
        W, H = 440, 400
        img = Image.new("RGBA", (W, H), (0, 0, 0, 0))
        d = ImageDraw.Draw(img)
        x0, y0, x1, y1 = 40, 150, W - 40, H - 46
        _box_shell(d, x0, y0, x1, y1, True)
        if food:
            # The same hotdog that goes in his hand, lying in the tray.
            # Pasted rather than redrawn: two drawings of one object drift
            # apart the first time either is touched, and the child watches
            # this one come out of that box.
            food_w = int((x1 - x0) - 124)
            food = _hotdog(food_w, int(food_w * H_DOG / W_DOG))
            # Inside the tray, not across its hinge: the opening runs from
            # y0 down to front_y, and a hotdog pasted level with y0 sits on
            # the fold with its sausage over the lid.
            img.alpha_composite(food, (x0 + 62, y0 + 14))
        img = shadow_under(img, (x0, y0, x1, y1))
        img.save(OUT + name + ".png")
        sizes[name] = img.size
    return sizes


# ---------------------------------------------------------- the rubbish

def make_wrapper():
    """
    The crumpled wrapper - what "Tapon" throws.

    PALE ON PURPOSE. It is seen three times and the last of them is against
    the dark inside of the bin, which is the frame that has to read: a dark
    wrapper arriving in a dark bin is a wrapper that vanished. Crumpled waxed
    paper is pale anyway, so nothing is being bent to make it work.

    Small, too. It is in his hand for four frames beside a drawn fist, and a
    ball of paper the size of the fist holding it reads as a snowball.
    """
    W, H = 240, 220
    img = Image.new("RGBA", (W, H), (0, 0, 0, 0))
    d = ImageDraw.Draw(img)

    # A BALL OF PAPER SCREWED UP IN A FIST, and nothing more symmetrical
    # than that.
    #
    # Two passes of this were drawn with the two neat twisted ends a sweet
    # wrapper keeps, one at each side, and at the size the game shows it -
    # about forty screen pixels in his hand and smaller still dropping into
    # the bin - a pale middle with a matching point on either side reads as
    # a BOW TIE. It did with the ends darker than the body and it did with
    # them the same colour.
    #
    # What a thing crumpled in a hand actually looks like is lumpy and
    # lopsided: a rough ball with a dozen little planes on it, one corner
    # sticking out further than the others, and no two sides the same. So
    # the symmetry is gone and the silhouette does the work.
    lump = [(56, 104), (64, 74), (92, 50), (128, 42), (166, 52),
            (196, 76), (204, 112), (192, 148), (160, 172), (118, 176),
            (80, 160), (60, 134)]
    # One torn corner, off to one side only, OVERLAPPING the ball well
    # enough to be part of it - the first version started at the ball's own
    # edge and came out as a little diamond floating beside it.
    d.polygon([(186, 100), (238, 84), (234, 124), (184, 128)], fill=WRAP_SHADE)
    d.polygon([(186, 100), (238, 84), (234, 124), (184, 128)],
              outline=OUTLINE, width=4)

    d.polygon(lump, fill=WRAP)
    # Creased facets: a lit side, a shaded side, and one deep fold. Three
    # values rather than one, because a red this saturated can carry them
    # where the pale paper could not.
    for facet, tone in (([(64, 74), (92, 50), (116, 92), (82, 108)], WRAP_SHADE),
                        ([(128, 42), (166, 52), (150, 94), (116, 92)], WRAP_LIGHT),
                        ([(196, 76), (204, 112), (168, 126), (150, 94)], WRAP_SHADE),
                        ([(118, 176), (80, 160), (96, 128), (134, 136)], WRAP_DARK),
                        ([(192, 148), (160, 172), (150, 136), (176, 124)], WRAP_SHADE),
                        ([(82, 108), (116, 92), (134, 136), (96, 128)], WRAP_LIGHT)):
        d.polygon(facet, tone and tone or WRAP)
        d.polygon(facet, fill=tone)

    # The printing, which is what says WRAPPER rather than a ball of foil.
    # It follows a facet instead of lying flat across the object, because a
    # straight bar on a crumpled thing reads as a label stuck on it.
    d.polygon([(100, 126), (132, 133), (128, 152), (98, 145)], fill=WRAP_PRINT)

    # The fold lines, thin, and the edge.
    for a, b in (((92, 50), (116, 92)), ((116, 92), (82, 108)),
                 ((116, 92), (150, 94)), ((150, 94), (168, 126)),
                 ((168, 126), (134, 136)), ((134, 136), (96, 128)),
                 ((96, 128), (82, 108)), ((150, 136), (160, 172))):
        d.line([a, b], fill=OUTLINE, width=3)
    d.polygon(lump, outline=OUTLINE, width=5)

    img.save(OUT + "prop_basura.png")
    return img.size


if __name__ == "__main__":
    print("prop_basurahan.png ", make_bin())
    print("prop_bote.png      ", make_bottle())
    print("prop_bote_held.png ", make_bottle_held())
    for k, v in make_hotdog().items():
        print("%-19s" % (k + ".png"), v)
    for k, v in make_lunchbox().items():
        print("%-19s" % (k + ".png"), v)
    print("prop_basura.png    ", make_wrapper())
