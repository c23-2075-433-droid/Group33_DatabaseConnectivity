"""Cut reach_bag_uniform and pencil_ready_uniform out of one two-figure strip.

These two are not part of any run - they are the poses the Kuha and Lapis
sequences land on - so they were the last of the fifteen seated sprites still
wearing the polo collar the rest of the set lost. Both came from one Canva
generation rather than two, which is one cooldown instead of two and, more
usefully, two poses drawn in one pass so they match.

CROWN HEIGHTS. Both at +1.735, the same as every other seated pose. Nothing
about either pose raises or lowers his head.

Writes  Assets/Sprites/reach_bag_uniform.png
        Assets/Sprites/pencil_ready_uniform.png

Run from the repository root:  python Tools/make_standalone_tee_poses.py
"""
import os
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from fit_seated_pose import fit_strip                        # noqa: E402

RAW = "Art/standalone_tee_poses_raw.png"
CROWNS = [1.735, 1.735]
TARGETS = ["Assets/Sprites/reach_bag_uniform.png",
           "Assets/Sprites/pencil_ready_uniform.png"]

if __name__ == "__main__":
    # fit_strip names its outputs from a {} pattern; these two have unrelated
    # names, so they are written to a temporary pattern and then moved.
    tmp = "Art/_standalone_{}.png"
    written = fit_strip(RAW, tmp, CROWNS)
    for src, dst in zip(written, TARGETS):
        os.replace(src, dst)
        print(f"  -> {dst}")
