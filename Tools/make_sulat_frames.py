"""Cut writing frames for "Sulat" out of a generated strip.

READ THIS BEFORE RUNNING IT. The strip currently in Art/sulat_loop_raw.png was
REJECTED, and the frames it produces are not in the project. The script is
kept because the pipeline is right and a better generation should drop
straight into it.

WHAT WENT WRONG, so the next prompt can avoid it. The generation was made from
sitting_front_uniform as the design reference and came back close enough to
fool a thumbnail, but wrong in three ways once fitted:

  1. The body was drawn about 7% short relative to the head. Fitting by hair
     cap, which is what fit_strip does and what keeps every other pose on
     model, therefore left the arms 55 rows above the canvas bottom - and that
     bottom edge IS the desk, so he floated with background under his elbows.
  2. Fitting instead to BOTH the crown row and the desk edge closed the gap
     but made the head 7-8% larger than the base sprite's.
  3. Worse than either: it was not the same boy. Rounder face, wider jaw,
     larger eyes, a different hair silhouette. Cutting between that and the
     real art mid-action changes who the character is, which the project's
     animation rules forbid outright. See Docs/AnimationStandard.md.

WHAT THE SCENE DOES INSTEAD. Paaralan Scene 3's "Sulat" is built from poses
that already exist - place_frame_3 and lapis_frame_1..2, Scene 2's pencil
poses. They are the same boy by construction: same pipeline, same base sprite,
crown on row 142 and hair within two pixels across all of them. The trade is a
loop that lifts and returns rather than a hand travelling across the page,
because lapis_frame_1 is the only drawing in the project with a pencil
actually on the paper.

IF YOU REGENERATE. Pin the two things that failed:
  - "the torso must be the same width and length as the reference, and his
     forearms must reach the very bottom edge of the frame"
  - pass pencil_ready_uniform as a SECOND reference alongside
    sitting_front_uniform, so the hair and the pencil grip come from the
    project's own art rather than being reinvented.
Then download the full-size PNG from Canva Uploads - the API returns only a
~200x112 thumbnail - save it over Art/sulat_loop_raw.png, and run this.

ON THE CROWN HEIGHTS. All three sit at +1.735, the same height as the seated
pose and every other seated frame in Paaralan 2 and 3. His head does NOT move
up or down while writing; only the arm does. A crown that drifted between
frames would make the loop bob, and a bobbing loop is far more obvious than a
single bad frame, because it repeats. Compare Tools/make_sit_frames.py, where
the crown really does descend and the stepping-down is the point.

Writes  Assets/Sprites/sulat_frame_1..3_uniform.png

Run from the repository root:  python Tools/make_sulat_frames.py
"""
import os
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from fit_seated_pose import fit_strip                        # noqa: E402

RAW = "Art/sulat_loop_raw.png"
OUT = "Assets/Sprites/sulat_frame_{}_uniform.png"

# Level, because a writing hand moves and a writing head does not.
CROWNS = [1.735, 1.735, 1.735]

if __name__ == "__main__":
    fit_strip(RAW, OUT, CROWNS)
