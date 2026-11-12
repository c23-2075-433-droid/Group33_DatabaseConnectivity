"""Cut the three "Sulat" writing frames out of one generated strip.

WHAT THEY ARE, and why they had to be drawn at all. These are the loop body
of the writing action - the lead-in and the tail come from Paaralan Scene 2's
pencil poses, which cost nothing and are the same boy by construction.

The loop could not be built that way, for a reason of geometry rather than
taste. The seated sprite is cropped at the desk's far edge, world -3.01, and
the open notebook lies entirely BELOW that line, from -3.84 to -3.01. So
every pencil pose already in the project holds the point in mid-air above the
top edge of the page: lapis_frame_1, the lowest of them, still clears the
book by about half a world unit. Cycling those gave a pencil waving over a
notebook it never touched.

The one thing these three have that no other pose does is THE PENCIL TIP ON
THE BOTTOM EDGE OF THE CANVAS. That edge is -3.01, the notebook's far edge,
so the point lands on the page. They differ only in where along that edge the
tip sits, which is what makes them writing rather than bobbing.

THREE GENERATIONS. Worth recording, because the same two faults will come
back on the next pose:

  1. Short body. Fitting by hair cap left the arms 55 rows above the desk and
     he floated; fitting to the desk made his head 7-8% too big. Rejected.
  2. Right pose at last - the pencil finally reached the bottom edge - but a
     big round puffy hairstyle instead of his tight flat-topped spikes. 323px
     of hair against the real 277, which reads as reach -12%: a bigger head
     leaves less room for body in the same frame. Rejected.
  3. Accepted. Hair 251-252px, head width within 1%, colour drift 14-16
     against a tolerance of 26.

WHAT FINALLY HAD TO BE PINNED, beyond the obvious:

  - TWO references, sitting_front_uniform and lapis_frame_1_uniform, so the
    hair and the pencil grip come from the project's own art.
  - The hair explicitly: "close to his skull, a small neat flat-topped mop
    with short spikes, roughly as tall as his forehead - NOT a big round
    puffy mass". Asking for "the same hair as the reference" was not enough
    twice running.
  - Head size by comparison to something else: "shoulders at least twice as
    wide as his head", "the head taking up only the top third".
  - The pencil: "the tip must touch the very bottom edge of the image,
    angled down about 45 degrees", and the left hand "flat and low at the
    bottom edge, steadying the page".

Fitted by hair cap, NOT flush to the bottom: these come back reaching 3.25
heads against the base's 2.79, so they overshoot, and the excess is simply
clipped below the desk line. Overshoot is harmless; short is not.

ON THE CROWN HEIGHTS. All three sit at +1.735, the same as every other seated
frame in Paaralan 2 and 3. He leans from the shoulders and the lean is drawn
in, so moving the whole figure down as well would read as him sinking through
the chair - and a crown that drifted would make the loop bob, which repeats
and is therefore far more obvious than one bad frame.

WHERE THE ART CAME FROM. Art/sulat_loop_raw.png is one Canva generation. The
API returns only a ~200x112 thumbnail, so the full-size file is downloaded by
hand out of Canva Uploads, which is why the raw lives in Art/. Check any
replacement with Tools/check_pose_strip.py before using it; it refuses to
judge a thumbnail.

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
