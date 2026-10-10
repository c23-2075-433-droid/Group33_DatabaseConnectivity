"""Recolour Kylo into a classmate for Paaralan Scene 4.

Recess needs somebody to play with. Three of the scene's five words - Laro,
Tawa and Tulong - are about another child, and Scene 1's own comment already
makes the argument for why that child has to be on screen: greeting an empty
yard "teaches the word in a vacuum", which is why a guard was drawn for the
gate.

There is no second child in the project, so one is made the way the uniform
itself was made - by recolouring, not by new art. See Tools/make_uniform.py,
which turned Kylo's green shirt white by the same method.

Two things change, and only two:

  hair    near-black -> warm brown
  shorts  navy -> maroon, which reads as PE shorts at break time

The white shirt stays white: a Filipino school uniform is a white shirt, so
giving the classmate a different one would look like a different school
rather than a different child.

Shading is remapped rather than flattened. Each pixel's brightness is carried
across from the old colour's range onto the new one, so every fold and
shadow stays where the artist put it - a flat fill turns the hair into a
silhouette.

BE HONEST ABOUT WHAT THIS IS. It is Kylo with brown hair, mirrored, and he
keeps his glasses. At the size he appears on screen - about a fifth of the
frame, across the yard - that reads as another child in the same uniform.
Close up it would not. A drawn classmate is the real answer when there is
time for one.

Reads   Assets/Sprites/talking_uniform.png, celebrate_uniform.png,
        sitting_desk_uniform.png
Writes  Assets/Sprites/char_classmate.png, char_classmate_cheer.png,
        char_classmate_seated.png

Run from the repository root:  python Tools/make_classmate.py
"""
from PIL import Image
import numpy as np

# One entry per pose the classmate needs - greeting and cheering in Parke
# Scene 3, seated for the picnic in Scene 4. All from the same recolour, so he
# is one child throughout rather than three different-looking ones.
POSES = [
    ("talking_uniform", "char_classmate"),
    ("celebrate_uniform", "char_classmate_cheer"),
    ("sitting_desk_uniform", "char_classmate_seated"),
]

# Measured off the source, not guessed - see the sampling run in the commit
# that added this. Hair spans 0..71 across all channels; the navy spans
# roughly 22..89, brightest in blue.
HAIR_LO, HAIR_HI = 0.0, 71.0
HAIR_DARK = (46, 26, 12)        # warm brown, in shadow
HAIR_LIGHT = (156, 98, 52)      # the same brown, lit

SHORTS_LO, SHORTS_HI = 22.0, 89.0
SHORTS_DARK = (58, 20, 26)      # maroon, in shadow
SHORTS_LIGHT = (146, 58, 62)    # maroon, lit


def remap(values, lo, hi, dark, light):
    """Carry a pixel's brightness from one colour's range onto another's."""
    t = np.clip((values - lo) / max(hi - lo, 1.0), 0.0, 1.0)[..., None]
    return np.array(dark) + (np.array(light) - np.array(dark)) * t


def convert(src_name, dst_name):
    img = Image.open("Assets/Sprites/%s.png" % src_name).convert("RGBA")
    a = np.asarray(img).astype(float).copy()
    rgb, alpha = a[..., :3], a[..., 3]
    r, g, b = rgb[..., 0], rgb[..., 1], rgb[..., 2]
    opaque = alpha > 40
    height = a.shape[0]

    # Hair: the darkest pixels, and only in the top third. Without the band
    # the shoes and the glasses frames recolour too, and he ends up in brown
    # shoes with tortoiseshell glasses.
    band = np.zeros_like(opaque)
    band[: int(height * 0.36)] = True
    hair = opaque & band & (r < 72) & (g < 72) & (b < 82)

    # Shorts: navy is the only thing on him where blue leads by a clear
    # margin. The white shirt has blue in it too, but not more than red.
    lower = np.zeros_like(opaque)
    lower[int(height * 0.42) : int(height * 0.82)] = True
    shorts = opaque & lower & (b > r + 22) & (b > 55) & (b < 175) & (r < 115)

    brightness = rgb.mean(axis=2)
    a[..., :3][hair] = remap(brightness[hair], HAIR_LO, HAIR_HI, HAIR_DARK, HAIR_LIGHT)
    a[..., :3][shorts] = remap(brightness[shorts], SHORTS_LO, SHORTS_HI,
                                SHORTS_DARK, SHORTS_LIGHT)

    out = Image.fromarray(np.clip(a, 0, 255).astype("uint8"), "RGBA")
    # Mirrored, so he does not stand in the same attitude as Kylo beside him.
    out = out.transpose(Image.FLIP_LEFT_RIGHT)
    out.save("Assets/Sprites/%s.png" % dst_name)
    print("%-26s %s (hair %d px, shorts %d px)"
          % (dst_name + ".png", out.size, hair.sum(), shorts.sum()))


def main():
    import os
    for src_name, dst_name in POSES:
        if not os.path.exists("Assets/Sprites/%s.png" % src_name):
            print("  skipped, no", src_name)
            continue
        convert(src_name, dst_name)


if __name__ == "__main__":
    main()
