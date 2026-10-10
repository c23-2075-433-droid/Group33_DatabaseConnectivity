"""Put the celebrate pose into the school uniform.

Parke Scene 1 teaches "Tuwa" - being glad - and a feeling has to be carried by
the character rather than by something lighting up. The one pose in the set
that reads as delight is celebrate_with_bag, arms thrown up.

It was never given a uniform. Tools/make_uniform.py converted standing,
talking and the four walk frames, so celebrate is still wearing the original
green shirt - and Kylo would have changed clothes halfway through the scene,
white at the start of the walk and green the moment he was pleased about it.

This applies that same conversion to the two celebrate sprites. It is a
separate script rather than another entry in make_uniform.py for one reason,
which that file states plainly: the green-to-white brightness range is
measured across a FIXED list of poses and must stay fixed, because
re-measuring it shifts the white slightly on every sprite already converted.
So the range is measured here from exactly that list, and only the celebrate
pair is written.

Reads   Assets/Sprites/celebrate.png, celebrate_with_bag.png
        (and the RANGE_POSES, to measure the range)
Writes  Assets/Sprites/celebrate_uniform.png, celebrate_uniform_with_bag.png

Run from the repository root:  python Tools/make_celebrate_uniform.py
"""
import os
from PIL import Image
import numpy as np

SPRITES = "Assets/Sprites"

# Copied from make_uniform.py, deliberately. Importing them would be tidier,
# but that module needs scipy for work this script does not do.
RANGE_POSES = ["standing", "talking", "talking_barefoot", "talking_socks",
               "walk_frame_1", "walk_frame_2", "walk_frame_3", "walk_frame_4"]
SHADOW, HIGHLIGHT = 168.0, 250.0

TARGETS = [("celebrate", "celebrate_uniform"),
           ("celebrate_with_bag", "celebrate_uniform_with_bag")]


def load(name):
    path = os.path.join(SPRITES, name + ".png")
    return np.asarray(Image.open(path).convert("RGBA")).astype(float)


def shirt_mask(a):
    """The green tee is the only green on him - nothing else leads in green."""
    rgb, al = a[..., :3], a[..., 3]
    r, g, b = rgb[..., 0], rgb[..., 1], rgb[..., 2]
    return (al > 128) & (g > r + 8) & (g > b + 8)


def green_range():
    """
    Where the shirt's brightness sits across the fixed pose list.

    Percentiles rather than the outright darkest and lightest pixel: a few
    anti-aliased pixels on the shirt's edge run almost white, and letting one
    of them set the top squashes the whole shirt into the grey half.
    """
    values = []
    for pose in RANGE_POSES:
        for suffix in ("", "_with_bag"):
            name = pose + suffix
            if not os.path.exists(os.path.join(SPRITES, name + ".png")):
                continue
            a = load(name)
            m = shirt_mask(a)
            if m.any():
                values.append(a[..., :3].mean(2)[m])
    allv = np.concatenate(values)
    return float(np.percentile(allv, 2)), float(np.percentile(allv, 98))


def recolour(src, dst, lo, hi):
    a = load(src)
    shirt = shirt_mask(a)
    if not shirt.any():
        print("  !! no green found in", src)
        return 0

    lum = a[..., :3].mean(2)
    t = np.clip((lum - lo) / max(hi - lo, 1e-6), 0, 1)
    white = SHADOW + t * (HIGHLIGHT - SHADOW)

    out = a.copy()
    # A touch cooler in the folds, like cotton in shade.
    out[..., 0] = np.where(shirt, white * 0.995, out[..., 0])
    out[..., 1] = np.where(shirt, white * 0.998, out[..., 1])
    out[..., 2] = np.where(shirt, np.minimum(255, white * 1.012), out[..., 2])

    Image.fromarray(np.clip(out, 0, 255).astype(np.uint8)).save(
        os.path.join(SPRITES, dst + ".png"))
    return int(shirt.sum())


def main():
    lo, hi = green_range()
    print("green range from the fixed pose list: %.1f .. %.1f" % (lo, hi))
    for src, dst in TARGETS:
        if not os.path.exists(os.path.join(SPRITES, src + ".png")):
            print("  skipped, no", src)
            continue
        n = recolour(src, dst, lo, hi)
        print("  %-28s %d shirt pixels" % (dst + ".png", n))


if __name__ == "__main__":
    main()
