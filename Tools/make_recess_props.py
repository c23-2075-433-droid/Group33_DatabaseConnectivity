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
  prop_sandwich       one sandwich, for his hand.
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
from PIL import Image, ImageDraw, ImageFilter

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

# The food. Toast crust and a pale crumb, with a slice of cheese and a leaf
# of lettuce so it is plainly a sandwich at the size this is seen.
CRUST = (206, 154, 84)
CRUST_DARK = (176, 124, 60)
CRUMB = (246, 226, 186)
CHEESE = (240, 198, 86)
LETTUCE = (134, 176, 92)

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

def _sandwich(d, x0, y0, x1, y1):
    """
    A sandwich seen edge on: bottom slice, filling, domed top slice.

    SEEN FROM THE SIDE, not from above, because it spends most of its life
    held in a fist on its way to his mouth and the thing that says "food"
    from the side is the FILLING showing between two slices of bread.

    THE FIRST VERSION OF THIS WAS THREE FLAT BANDS and at the size the game
    draws it - about a hundred and twenty screen pixels, half the width of
    his head - it read as a painted ruler. Three things fix that, and all
    three are about breaking the straight lines:

      the dome      the top slice is a loaf, rounded over, not a rectangle.
                    One curve is the difference between bread and a plank.
      the ruffle    the lettuce is a wave with leaf tips pushing out past
                    the bread on both edges, rather than a green stripe.
      the overhang  the cheese pokes out at one corner and droops.

    Drawn bottom up, so each layer sits over the one below it.
    """
    h = y1 - y0
    top_h = int(h * 0.46)
    fill_h = int(h * 0.20)
    fill_y0 = y0 + top_h
    fill_y1 = fill_y0 + fill_h

    # --- bottom slice ---
    d.rounded_rectangle([x0, fill_y1 - 4, x1, y1], radius=12, fill=CRUMB)
    d.rounded_rectangle([x0, y1 - 13, x1, y1], radius=11, fill=CRUST)

    # --- the filling ---
    # Cheese first, with a corner hanging out past the bread.
    d.polygon([(x0 - 7, fill_y0 + 2), (x1 + 9, fill_y0 - 2),
               (x1 + 16, fill_y1 + 10), (x1 - 30, fill_y1 + 2),
               (x0 - 7, fill_y1 + 4)], fill=CHEESE)
    # Lettuce: a wave along the bottom of the filling with the leaf tips
    # pushing out below the bread line.
    wave = [(x0 - 9, fill_y0 + 6)]
    n = 9
    for i in range(n + 1):
        t = i / n
        lx = x0 - 9 + (x1 - x0 + 20) * t
        wave.append((lx, fill_y1 + (14 if i % 2 else 1)))
    wave.append((x1 + 11, fill_y0 + 2))
    d.polygon(wave, fill=LETTUCE)

    # --- top slice, domed ---
    dome = int(top_h * 0.72)
    d.rounded_rectangle([x0, y0, x1, fill_y0 + 6], radius=dome, fill=CRUMB)
    # The crust is the baked face: along the dome, and a little way down the
    # two ends. Drawn as a ring, then the crumb punched back out of it.
    d.rounded_rectangle([x0, y0, x1, fill_y0 + 6], radius=dome,
                        outline=CRUST, width=11)
    d.rounded_rectangle([x0 + 11, y0 + 11, x1 - 11, fill_y0 + 6],
                        radius=max(4, dome - 11), fill=CRUMB)
    # A lighter band just inside the top crust, so the loaf turns over.
    d.arc([x0 + 14, y0 + 8, x1 - 14, y0 + dome * 2], 190, 350,
          fill=(252, 238, 208), width=7)

    # One outline round each slice. NOT round the whole stack: the filling
    # stands out past the bread on both edges, and a single outline round all
    # of it would draw a line through the lettuce.
    d.rounded_rectangle([x0, y0, x1, fill_y0 + 6], radius=dome,
                        outline=OUTLINE, width=5)
    d.rounded_rectangle([x0, fill_y1 - 4, x1, y1], radius=12,
                        outline=OUTLINE, width=5)


def make_sandwich():
    """One sandwich, long axis across, for his hand.

    Drawn horizontally for the same reason the held bottle is - it is rotated
    into his grip by Tools/make_recess_frames.py and has to reach from his
    fist to his mouth.

    SHORTER AND DEEPER THAN THE FIRST VERSION. That one was drawn 312 by 162,
    nearly two to one, and a sandwich twice as long as it is tall is a
    sandwich nobody made. Three to two reads as food and still spans the gap
    between his fist and his mouth, because the span is set by how big it is
    drawn in the frame and not by the shape of this canvas.
    """
    W, H = 360, 260
    img = Image.new("RGBA", (W, H), (0, 0, 0, 0))
    _sandwich(ImageDraw.Draw(img), 30, 30, W - 30, H - 30)
    img.save(OUT + "prop_sandwich.png")
    return img.size


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
            _sandwich(d, x0 + 62, y0 - 8, x1 - 62, y0 + 74)
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
    print("prop_sandwich.png  ", make_sandwich())
    for k, v in make_lunchbox().items():
        print("%-19s" % (k + ".png"), v)
    print("prop_basura.png    ", make_wrapper())
