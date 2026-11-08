"""Cut the three "Tanong" hand-raise frames out of one generated strip.

WHAT THEY ARE. A whole action from a single generation, which is why this
pose was done first of the three Scene 3 still needed. The three frames are
the hand rising - just off the desk, at head height, straight up - so read
forwards they are the lead-in, the last one held is the action, and read
backwards they are the ending movement. Raising a hand plays ONCE; the
project's rules are explicit that it is not a thing that repeats.

WHERE THE ART CAME FROM. Art/tanong_frames_raw.png is one Canva generation
made from TWO references: sitting_front_uniform for the character and
pencil_ready_uniform for the hair and the raised arm, so the arm is drawn
from a pose the project already has rather than invented. The Canva API
returns only a ~200px thumbnail, so the full-size file is downloaded by hand
out of Canva Uploads, which is why the raw lives in Art/.

It is the first strip to pass Tools/check_pose_strip.py on colour, at a
drift of 11-12 against a tolerance of 26. The rejected Sulat strip measured
138, and the difference is the two references.

FLUSH TO THE BOTTOM, not fitted by hair alone. The three figures come back
reaching 2.74, 2.76 and 2.77 heads where the base reaches 2.79 - between half
a percent and one and a half percent short. Fitted by hair that would leave
his arms a few pixels above the desk with background under them. Fitted to
both the crown and the desk edge, the only cost is a head the same fraction
too large, which at one percent is invisible. Six percent is not, which is
what got the Sulat strip rejected rather than corrected.

ON THE CROWN HEIGHTS. All three sit at +1.735, the same as every other seated
frame in Paaralan 2 and 3. Putting a hand up does not move your head, and a
crown that drifted between frames would make the gesture bob.

THIS POSE IS WHY hair_cap HAS A FALLBACK. It finds the head by scanning down
from the topmost opaque row, which is the tip of a hair spike in every other
pose in the project - and is a FINGERTIP here. See the long comment in
fit_seated_pose.hair_cap; the short version is that it now finds the hair
mass first, takes the head's columns from that, and only then looks upward
for the spike tips, ignoring the columns the arm occupies.

Writes  Assets/Sprites/tanong_frame_1..3_uniform.png

Run from the repository root:  python Tools/make_tanong_frames.py
"""
import os
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from fit_seated_pose import fit_strip                        # noqa: E402

RAW = "Art/tanong_frames_raw.png"
OUT = "Assets/Sprites/tanong_frame_{}_uniform.png"

# Level: putting a hand up does not move your head.
CROWNS = [1.735, 1.735, 1.735]

# Room above the head for the raised arm, added to BOTH ends so the figure
# does not move - see the note in fit_strip. The arm reaches about 180 rows
# above the crown and the seated canvas has 142, so the fingertips were being
# cut off; 140 leaves margin without making the sprite needlessly large.
CANVAS_PAD = 140

if __name__ == "__main__":
    fit_strip(RAW, OUT, CROWNS, flush_to_bottom=True, canvas_pad=CANVAS_PAD)
