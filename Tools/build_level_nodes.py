"""Build the level-select signposts from one shared frame and four scenes.

The map needs four signposts that differ only in the picture inside them. Four
separately generated signposts never match - the frames drift in thickness,
colour and lighting - so one frame is reused for all four and only the picture
changes. The frame is lifted from the existing node art, which already suits
the game's wood-and-bamboo interface, by clearing the disc in its middle.

Each locked signpost is the unlocked one desaturated, so the two always agree.

Run from the repository root:  python3 Tools/build_level_nodes.py
Reads  Art/scene_<level>.png              (the generated illustrations)
Writes Assets/Sprites/node_<level>.png and node_<level>_locked.png
"""
from PIL import Image, ImageDraw, ImageFilter
import numpy as np
import os, sys

SPRITES = 'Assets/Sprites'   # where finished sprites go
ART = 'Art'                  # source illustrations, outside Unity's import path
FRAME_SRC = 'node_bahay_frame_source.png'   # kept so the frame can be rebuilt
LEVELS = ['bahay', 'paaralan', 'parke', 'palengke']

# Measured from the original node art: the disc the picture sits in.
CENTRE = (141.5, 141.0)
INNER_R = 106.0      # frame's inner lip
OUTER_R = 129.5

def load_frame():
    """The signpost with the picture area cleared out."""
    path = os.path.join(SPRITES, FRAME_SRC)
    if not os.path.exists(path):
        path = os.path.join(SPRITES, 'node_bahay.png')
    im = Image.open(path).convert('RGBA')
    a = np.asarray(im).copy()
    h, w = a.shape[:2]
    yy, xx = np.mgrid[0:h, 0:w]
    d = np.hypot(xx - CENTRE[0], yy - CENTRE[1])
    # Feathered so the picture tucks under the frame's lip with no hard seam.
    keep = np.clip((d - (INNER_R - 2.0)) / 2.5, 0, 1)
    a[..., 3] = (a[..., 3] * keep).astype(np.uint8)
    return Image.fromarray(a)

def circular(scene, radius):
    """Scene cropped to the disc, slightly oversized so the frame overlaps it."""
    size = int(radius * 2)
    s = scene.convert('RGB')
    side = min(s.size)
    s = s.crop(((s.width - side) // 2, (s.height - side) // 2,
                (s.width + side) // 2, (s.height + side) // 2))
    s = s.resize((size, size), Image.LANCZOS).convert('RGBA')
    mask = Image.new('L', (size * 4, size * 4), 0)
    ImageDraw.Draw(mask).ellipse((0, 0, size * 4 - 1, size * 4 - 1), fill=255)
    mask = mask.resize((size, size), Image.LANCZOS)   # anti-aliased edge
    s.putalpha(mask)
    return s

def desaturate(im):
    """Locked look: grey, dimmed and slightly flattened."""
    a = np.asarray(im).astype(float)
    grey = a[..., :3] @ [0.299, 0.587, 0.114]
    out = a.copy()
    out[..., :3] = (grey[..., None] * 0.72 + 38)
    return Image.fromarray(np.clip(out, 0, 255).astype(np.uint8))

def with_lock(im):
    """Stamp the padlock over a locked signpost.

    The badge is baked into the locked sprite rather than added as a separate
    object the level-select controller has to show and hide: the controller
    already swaps the whole sprite when a level is locked, so this needs no
    code at all and the two can never disagree.
    """
    lock_path = os.path.join(SPRITES, 'ui_lock.png')
    if not os.path.exists(lock_path):
        return im
    lock = Image.open(lock_path).convert('RGBA')
    h = int(im.height * 0.34)
    lock = lock.resize((max(1, int(lock.width * h / lock.height)), h), Image.LANCZOS)
    out = im.copy()
    out.alpha_composite(lock, (int(CENTRE[0] - lock.width / 2),
                               int(CENTRE[1] - lock.height / 2)))
    return out

def main():
    frame = load_frame()
    made = []
    for name in LEVELS:
        scene_path = os.path.join(ART, 'scene_%s.png' % name)
        if not os.path.exists(scene_path):
            print('  skip %-9s (no %s yet)' % (name, os.path.basename(scene_path)))
            continue
        pic = circular(Image.open(scene_path), INNER_R + 4)
        out = Image.new('RGBA', frame.size, (0, 0, 0, 0))
        out.alpha_composite(pic, (int(CENTRE[0] - pic.width / 2),
                                  int(CENTRE[1] - pic.height / 2)))
        out.alpha_composite(frame)
        out.save(os.path.join(SPRITES, 'node_%s.png' % name))
        with_lock(desaturate(out)).save(os.path.join(SPRITES, 'node_%s_locked.png' % name))
        made.append(name)
        print('  built node_%s.png and node_%s_locked.png' % (name, name))
    if not made:
        print('\nNothing built. Download the generated illustrations into '
              '%s first, named scene_<level>.png' % SPRITES)
    return frame

if __name__ == '__main__':
    main()
