"""Cut the settings panel and its controls out of the Canva exports.

The settings screen was redrawn in Canva and arrived as three 2000x1125 PNGs
with a real alpha channel: the panel on its own, a sheet of loose widgets, and
a mockup of the two composed, which is reference only and is never cut. The
panel keeps its baked row plates, icons and text and becomes a single sprite;
only the five things a finger touches are cut out separately.

Four of those five are straight crops. The slider is not, and most of this
file is about why.

A Unity slider is three overlapping images - a background, a fill that grows
from the left, and a handle that rides on top - but the sheet draws the
slider twice as finished pictures, once nearly full and once nearly empty. In
neither of them is any one layer fully visible: the knob covers the bar where
it sits, the orange covers the dark track to its left, and the dark track
covers nothing but is never shown with both of its rounded ends free. Cutting
the three layers out of those two pictures is not possible without inventing
the hidden parts.

So each layer is made the way its own paint allows, which measurement decides
rather than preference:

  - The orange fill is flat. Sampled down any column it is #e28f36 with zero
    variance, so a drawn capsule is the same pixels as a cut one would be,
    and drawing it means choosing a clean width and a cap radius that 9-slice
    can hold. That matters more than it sounds: the fill is the one piece
    whose width changes from nothing to full, and a stretched bitmap cap
    collapses into a sliver at the low end. The MS. RACHEL row in the mockup
    sits at about six percent, so the low end is not hypothetical.

  - The knob is flat too - 93.9% exactly #422011, the rest the anti-aliased
    rim. Drawn as a disc it is clean; cut, it drags in whatever of the bar
    and the capsule showed around its corners.

  - The grey capsule is the one piece with a gradient, and it runs along the
    bar rather than down it: every column is one flat grey, and that grey
    climbs steadily from #adb1b4 at the left end to #d9d9dc at the right.
    Fitting a line through the 951 columns that are unambiguously grey - no
    transparency, no knob, no anti-aliasing - leaves a worst residual of 2.8
    levels and an rms under 1. A ramp that straight is a described thing, not
    a texture, so the capsule is rebuilt from its two endpoint colours. That
    is reproducing the art, not inventing it: cutting it instead would mean
    mirroring one clean end to stand in for the other and then repainting the
    ramp across the join anyway, which is the same decision taken less
    honestly.

Everything drawn here is supersampled 4x and reduced, so the caps and the
disc get their anti-aliasing from the geometry rather than from a filter.

Reads:
    Art/settings_panel_raw.png      the panel, with rows, icons and text baked in
    Art/settings_widgets_raw.png    the loose widgets, on the same 2000x1125 canvas

Writes:
    Assets/Sprites/ui_settings_panel.png
    Assets/Sprites/ui_settings_toggle_on.png
    Assets/Sprites/ui_settings_toggle_off.png
    Assets/Sprites/ui_settings_slider_track.png
    Assets/Sprites/ui_settings_slider_fill.png
    Assets/Sprites/ui_settings_slider_knob.png
    Assets/Sprites/btn_settings_close.png
"""

import os

import numpy as np
from PIL import Image, ImageDraw

PANEL_RAW = "Art/settings_panel_raw.png"
WIDGETS_RAW = "Art/settings_widgets_raw.png"
OUT = "Assets/Sprites"

# Boxes measured off the widget sheet, left, upper, right, lower, end-exclusive.
CLOSE_BOX = (1512, 154, 1749, 392)
TOGGLE_ON_BOX = (404, 536, 803, 707)
TOGGLE_OFF_BOX = (404, 801, 803, 972)

# The slider, measured down a clean column of the full-value drawing. The bands
# are, top to bottom: 15 clear, 25 grey, 10 dark, 27 orange, 10 dark, 25 grey,
# 15 clear - so the grey capsule is 97 tall, its dark inner 47, the orange 27,
# and the knob a 125 disc that deliberately overhangs the capsule.
CAPSULE_H = 97
INNER_H = 47
FILL_H = 27
KNOB_D = 125
TRACK_W = 1126

# The grey ramp, fitted across the capsule's own bounds.
GREY_LEFT = (173, 177, 180)
GREY_RIGHT = (217, 217, 220)
DARK = (66, 32, 17)
ORANGE = (226, 143, 54)

# Drawn wider than it is ever shown, so 9-slice has a middle to repeat.
FILL_W = 400

SS = 4  # supersample factor


def capsule(size, radius, fill):
    """A rounded bar, anti-aliased by drawing it large and reducing it."""
    w, h = size
    big = Image.new("RGBA", (w * SS, h * SS), (0, 0, 0, 0))
    ImageDraw.Draw(big).rounded_rectangle(
        (0, 0, w * SS - 1, h * SS - 1), radius=radius * SS, fill=fill)
    return big.resize((w, h), Image.LANCZOS)


def disc(diameter, fill):
    """A filled circle, anti-aliased the same way."""
    big = Image.new("RGBA", (diameter * SS, diameter * SS), (0, 0, 0, 0))
    ImageDraw.Draw(big).ellipse((0, 0, diameter * SS - 1, diameter * SS - 1), fill=fill)
    return big.resize((diameter, diameter), Image.LANCZOS)


def ramp(width, height, left, right):
    """A horizontal linear ramp between two colours, as an RGB array."""
    t = np.linspace(0.0, 1.0, width, dtype=np.float32)[None, :, None]
    a = np.array(left, dtype=np.float32)[None, None, :]
    b = np.array(right, dtype=np.float32)[None, None, :]
    row = a + (b - a) * t
    return np.repeat(row, height, axis=0)


def build_track():
    """The grey capsule with its dark inner groove, both ends free."""
    outer = capsule((TRACK_W, CAPSULE_H), CAPSULE_H // 2, (255, 255, 255, 255))

    # The groove follows the capsule's contour, inset by the grey border all
    # round - 25 px, which is exactly what takes a 97 bar down to a 47 one.
    inset = (CAPSULE_H - INNER_H) // 2
    inner = capsule((TRACK_W - 2 * inset, INNER_H), INNER_H // 2, (255, 255, 255, 255))

    grey = np.dstack([ramp(TRACK_W, CAPSULE_H, GREY_LEFT, GREY_RIGHT),
                      np.array(outer.getchannel("A"), dtype=np.float32)[..., None]])
    out = Image.fromarray(np.clip(grey, 0, 255).astype(np.uint8), "RGBA")

    groove = Image.new("RGBA", (TRACK_W, CAPSULE_H), (0, 0, 0, 0))
    groove.paste(Image.merge("RGBA", (
        *[Image.new("L", inner.size, c) for c in DARK], inner.getchannel("A"))),
        (inset, inset))
    out.alpha_composite(groove)
    return out


def build_fill():
    """The orange bar, flat, drawn wide enough to 9-slice."""
    return capsule((FILL_W, FILL_H), FILL_H // 2, (*ORANGE, 255))


def build_knob():
    return disc(KNOB_D, (*DARK, 255))


def crop(img, box):
    return img.crop(box)


def trim(img):
    """Crop to the art's own bounds - a sprite padded with nothing cannot be placed."""
    bbox = img.getchannel("A").point(lambda v: 255 if v > 8 else 0).getbbox()
    return img.crop(bbox) if bbox else img


def save(img, name, note):
    path = os.path.join(OUT, name + ".png")
    img.save(path, "PNG", optimize=True)
    bbox = img.getchannel("A").point(lambda v: 255 if v > 8 else 0).getbbox()
    print(f"  {name:30s} {img.size[0]:5d} x {img.size[1]:4d}   alpha {bbox}   {note}")
    return path


def main():
    for p in (PANEL_RAW, WIDGETS_RAW):
        if not os.path.exists(p):
            raise SystemExit(f"missing {p} - stage the Canva exports in Art/ first")
    os.makedirs(OUT, exist_ok=True)

    panel = Image.open(PANEL_RAW).convert("RGBA")
    sheet = Image.open(WIDGETS_RAW).convert("RGBA")

    print("cut from the exports:")
    save(trim(panel), "ui_settings_panel", "frame, rows, icons and text, baked")
    save(crop(sheet, CLOSE_BOX), "btn_settings_close", "wooden X, textured so cut whole")
    save(crop(sheet, TOGGLE_ON_BOX), "ui_settings_toggle_on", "flat, baked ON text")
    save(crop(sheet, TOGGLE_OFF_BOX), "ui_settings_toggle_off", "flat, baked OFF text")

    print("rebuilt from measurements:")
    save(build_track(), "ui_settings_slider_track", "grey ramp + dark groove, both ends free")
    save(build_fill(), "ui_settings_slider_fill", f"flat orange, 9-slice border {FILL_H // 2}")
    save(build_knob(), "ui_settings_slider_knob", "flat disc")

    print("\nNow run Tools > SALINLAHI > Refresh UI Theme in Unity. It fixes the")
    print("import settings, sets the fill's 9-slice border, and repoints the theme.")


if __name__ == "__main__":
    main()
