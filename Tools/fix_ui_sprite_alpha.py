"""Give a UI sprite back the transparency it was saved without.

ui_arrow.png and ui_word_badge.png are both 1024x559 RGB - no alpha channel
at all. They were saved out of an editor as flat pictures, so what the game
draws is not an arrow and a badge, it is an arrow and a badge each inside a
rectangle: the badge on white, and the arrow on the grey CHECKERBOARD its
editor was using to show transparency, baked in as real pixels. Every one of
the fifteen scenes puts those rectangles on screen next to every word.

Two kinds of background, one method. Both are found by flooding inward from
the border rather than by testing every pixel, so nothing enclosed by the
drawing is touched - the white inside a letter stays white.

  checkerboard  the two greys are neutral (red, green and blue within a few
                points of each other) and mid-to-light. The art is orange
                and near-black, so neutrality alone separates them.
  plain white   anything close to white.

Then the background grows a few rounds into whatever pale pixels touch it,
which takes off the anti-aliased rim, and the drawing's own colour is bled
outwards so a later resize blends art into art rather than art into
background. Both of those are the same treatment fit_seated_pose.py gives
the character art, for the same reason.

    python3 Tools/fix_ui_sprite_alpha.py Assets/Sprites/ui_word_badge.png
    python3 Tools/fix_ui_sprite_alpha.py Assets/Sprites/ui_arrow.png

Writes in place, keeping the file name and therefore the GUID, so no scene
reference moves and no .meta has to change.
"""
import os
import sys

import numpy as np
from PIL import Image
from scipy import ndimage

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from fit_seated_pose import bleed_edges  # noqa: E402

NEUTRAL = 8             # max channel spread for a pixel to count as grey
CHECKER_MIN = 140       # the darker checker grey is about 173
WHITE = 232
HALO_LUMA = 205
HALO_ROUNDS = 12        # fewer than the character art: UI edges are crisper
FEATHER = 0.6


def background_mask(rgb):
    spread = rgb.max(axis=2) - rgb.min(axis=2)
    lum = rgb.mean(axis=2)
    neutral = (spread <= NEUTRAL) & (lum >= CHECKER_MIN)
    white = (rgb >= WHITE).all(2)
    candidate = neutral | white

    labels, _ = ndimage.label(candidate)
    edge = np.concatenate([labels[0, :], labels[-1, :],
                           labels[:, 0], labels[:, -1]])
    outside = np.unique(edge)
    outside = outside[outside != 0]
    return np.isin(labels, outside)


def main():
    if len(sys.argv) != 2:
        raise SystemExit(__doc__)
    path = sys.argv[1]

    im = Image.open(path)
    rgb = np.asarray(im.convert('RGB'), dtype=int)
    background = background_mask(rgb)

    pale = rgb.mean(axis=2) > HALO_LUMA
    for _ in range(HALO_ROUNDS):
        touching = (np.roll(background, 1, 0) | np.roll(background, -1, 0)
                    | np.roll(background, 1, 1) | np.roll(background, -1, 1))
        grown = background | (touching & pale)
        if not grown[~background].any():
            break
        background = grown

    alpha = ndimage.gaussian_filter(np.where(background, 0.0, 255.0), FEATHER)
    out = bleed_edges(Image.fromarray(
        np.dstack([rgb, alpha]).astype(np.uint8)))

    box = out.getbbox()
    if box is None:
        raise SystemExit('nothing left - the whole image looked like background')
    out = out.crop(box)
    out.save(path)
    print('%s  %dx%d, %.0f%% transparent'
          % (path, out.width, out.height,
             100.0 * (np.asarray(out)[:, :, 3] < 8).mean()))


if __name__ == '__main__':
    main()
