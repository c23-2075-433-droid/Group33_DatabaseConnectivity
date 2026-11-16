"""Draw the "Sagot" frames - Kylo puts his hand up and stands to answer.

THE BEAT. He is sitting at his desk. His hand goes up, he rises to his feet
with it still up, he answers, and his hand comes down. Four parts, exactly
the shape PoseSequence is built around, and the first third of it is already
drawn: tanong_frame_1..3_uniform are his hand rising from the desk to
straight up, generated for the word "Tanong" and fitted by
Tools/make_tanong_frames.py. A hand going up is a hand going up.

WHAT STANDING NEEDS, AND THE VERSION OF THIS THAT WAS WRONG.

Every seated pose in this scene is cut off flat at the desk's far edge, world
-3.009. Raise one and the cut rises out from behind the desk with him. The
first version of this file answered that by grafting talking_uniform's hips
onto the seated torso at that cut and blending the two widths into each
other. In the editor it looked fine. In the game it did not: the seated
torso is 365 pixels across at the cut because his ARMS are in that number,
hanging at his sides, and the shirt being stretched to meet it was a bare
270. He came out with a white band round his waist like a sash, and his arms
ended in mid-air above it. It read as a paper doll.

The lesson is worth keeping, because it will come up again for Paaralan 1, 4
and the Parke scenes: A SEATED DRAWING AND A STANDING ONE DO NOT JOIN. They
disagree about where his arms are, and no amount of easing one width into the
other invents the arm that should be in the gap.

SO THE STANDING FRAMES ARE A WHOLE DRAWING. talking_uniform is Kylo standing,
front on, in the same tee and shorts, complete from his hair to his shoes,
with his hands up and his mouth open - which happens to be a child ANSWERING
A QUESTION, which is what this word means. Nothing about his body is
constructed here. It is scaled until its head matches the seated poses' head
(hair cap, the same normalisation every generated strip goes through) and
placed so the desk crosses him at the top of his shorts.

NOTHING IS GRAFTED ONTO IT. Standing with his hand still up was tried and
abandoned - the long note at the foot of main() has the whole of why - so
the standing frames are talking_uniform and nothing else, at four heights.
The hand-raise happens in the chair, where it is drawn.

THE SHAPE OF THE BEAT, then: his hand goes up in the chair over three drawn
frames, he gets to his feet over three more, and he answers standing with a
lean towards the front of the class and a nod. The pose changes once, on the
first frame of the rise, in the middle of the fastest movement in the beat -
which is where a change is least seen.

HOW FAR HE RISES. 146 pixels, 0.89 world units. The cap is the frame, not
anatomy: his crown sits at +1.735 seated and the narration plaque owns the
shot from about +2.79 up, so this puts it at +2.62, clear underneath. At that
height the desk's far edge crosses him at the top of his shorts, so his shirt
hem and a band of navy come into view that were never visible before. That is
what makes the rise read as standing rather than as floating.

HIS LEGS AND FEET ARE NOT IN THIS SHOT. The desk is drawn in front of him
from -3.009 down and nothing below that line is ever seen. They are in the
sprite because talking_uniform has them.

WHAT THIS STILL CANNOT DO is turn him to face the teacher - there is no
three-quarter view of his face anywhere in the project. "Faces the teacher"
is carried the way Paaralan Scene 2's "Kinig" carries it: a few degrees of
tilt towards the front of the class.

CANVAS. 663x1521: the seated canvas with 300 rows added at each end. Both
ends, so the middle of the canvas stays where it was and the figure does not
move - a sprite draws centred on its object, so padding one end only would
slide him sideways.

Writes  Assets/Sprites/sagot_rise_1..2_uniform.png   getting up
        Assets/Sprites/sagot_stand_uniform.png       up, hand up
        Assets/Sprites/sagot_answer_1..2_uniform.png the answering loop

Run from the repository root:  python Tools/make_sagot_frames.py
"""
import os
import sys

import numpy as np
from PIL import Image

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from fit_seated_pose import hair_cap                         # noqa: E402
from look_down import SPRITES, head_moved, meta_for          # noqa: E402

BASE = "sitting_front_uniform"        # the canvas and the head size to match
STAND = "talking_uniform"             # the whole standing drawing

PAD = 300
HEIGHT = 921 + 2 * PAD                # 1521
WIDTH = 663

# The seated canvas's own geometry, which every row here is measured against.
SEAT_CROWN_ROW = 142 + PAD            # where +1.735 lands on this canvas
DESK_ROW = 921 + PAD                  # where -3.009 does

# 146 rows up. See the note above for why it is not more.
RISE = 146

# Which way he leans while answering, and how deep the nod. Same convention
# as Tools/make_kinig_frames.py: negative tilts his crown towards the right
# of the frame, the front of the class.
TILT = -3.0
NOD = 12





# How far down talking_uniform's own left arm is cleared, as rows below his
# crown. It starts at his shoulder and his hand is the lowest of it.
CLEAR_ROWS = (430, 760)


def load(name):
    return Image.open(os.path.join(SPRITES, name + ".png")).convert("RGBA")



def standing_body(crown_row):
    """talking_uniform, head-matched to the seated poses and placed."""
    base = load(BASE)
    b_crown, b_bottom, _ = hair_cap(base)

    body = load(STAND)
    c, b, cx = hair_cap(body)
    scale = (b_bottom - b_crown) / float(b - c)
    body = body.resize((int(round(body.size[0] * scale)),
                        int(round(body.size[1] * scale))), Image.LANCZOS)
    c = int(round(c * scale))
    cx = int(round(cx * scale))

    out = Image.new("RGBA", (WIDTH, HEIGHT), (0, 0, 0, 0))
    out.alpha_composite(body, (WIDTH // 2 - cx, crown_row - c))
    return out, crown_row





def neck_of(crown_row):
    """His neck, 410 rows below the crown - see Tools/make_sagot_frames note.

    Not searched for: find_neck takes the narrowest row of ink, and with an
    arm up beside his head every row from the shoulders up measures from the
    arm to his ear. Every pose in this scene shares a head once it is
    normalised, so the neck is a fixed distance below the crown.
    """
    return (crown_row + 410, WIDTH // 2, 86)


def write(im, name):
    assert im.size == (WIDTH, HEIGHT), (name, im.size)
    im.save(os.path.join(SPRITES, name + ".png"))
    meta_for(name, BASE)


def main():
    crown = SEAT_CROWN_ROW - RISE
    print("standing up to answer: crown %d rows up (%0.2f world units), "
          "desk crosses him %d rows below the crown"
          % (RISE, RISE * 0.0060902, DESK_ROW - crown))

    # --- getting up ---
    # Three steps, not two. The pose changes on the first of them - seated
    # with his hand up, to standing and talking - and the more of the rise
    # there is around that change, the less it reads as a change.
    for i, part in enumerate((0.26, 0.55, 0.80), start=1):
        row = SEAT_CROWN_ROW - int(RISE * part)
        body, _ = standing_body(row)
        write(body, "sagot_rise_%d_uniform" % i)
        print("  %-26s %d rows up" % ("sagot_rise_%d_uniform.png" % i,
                                      SEAT_CROWN_ROW - row))

    stand, _ = standing_body(crown)
    write(stand, "sagot_stand_uniform")
    print("  %-26s on his feet, answering" % "sagot_stand_uniform.png")

    # --- answering: a lean towards the front of the class, and a nod ---
    neck = neck_of(crown)
    write(head_moved(stand, degrees=TILT, neck=neck), "sagot_answer_1_uniform")
    write(head_moved(stand, degrees=TILT, drop=NOD, neck=neck),
          "sagot_answer_2_uniform")
    print("  %-26s %+0.1f deg, and the same with a %d px nod"
          % ("sagot_answer_1..2.png", TILT, NOD))

    # --- HE DOES NOT STAND WITH HIS HAND UP, and that is a limit ---
    #
    # It was tried, twice. The arm itself cuts out of tanong_frame_3 cleanly
    # enough - it is the only thing on that side of a line from above his
    # fingers down to his armpit - and once both figures are normalised by
    # hair cap their shoulders land within a few pixels of each other. What
    # does not work is getting talking_uniform's OWN left arm out of the way
    # first:
    #
    #   his bare arm comes off by colour, but the black outline it shares
    #   with the shirt comes with it, and his side is left soft and ragged
    #   where the line should be;
    #   his SLEEVE cannot come off at all. It is white, it is attached to his
    #   shoulder, and it hangs down and out to the left - entirely below the
    #   raised arm pasted over it - so it survives as an empty sleeve stub
    #   sticking off his shoulder. Cutting it flush with his side means
    #   drawing a shoulder outline that is not in any sprite.
    #
    # Both are visible at the size this scene draws him, and a torn shirt is
    # worse than a hand that is already down. So the hand-raise stays in the
    # chair, where it is drawn, and standing up is him getting to his feet to
    # answer - which is what the word means.
    #
    # The hand could not have come down afterwards either, for what it is
    # worth. Swinging the arm about the shoulder puts it outside the sprite
    # past about twenty degrees - the arm is 592 pixels long and his shoulder
    # is at x 205 of 663 - and widening the canvas only moves the problem,
    # because a hand at arm's length sideways is most of a metre from his
    # body in world terms. That is a boy pointing.
    #
    # The drawn alternative does not work either, which is the more
    # interesting half. tanong_frame_1 and _2 have the arm at lower heights -
    # that IS the hand coming down, played backwards - but the cut that
    # isolates the arm in frame 3 does not isolate it in those: in frame 2 it
    # takes the hand and leaves the forearm behind, and in frame 1 the arm is
    # against his side with no straight line that separates the two. Each
    # would need a cut measured by hand, for a beat nothing asks for.
    #
    # So the loop ends with his hand still up and the sequence rests there. A
    # child who has been called on is usually still holding it up, and the
    # brief this was built from asks him to stand "while keeping his hand
    # raised" and never asks him to lower it.

    print("\nWire them in BuildPaaralanScene3 as the 'Sagot' PoseSequence:")
    print("  frames      tanong_1, tanong_2, tanong_3, rise_1, rise_2, rise_3, stand")
    print("  loopFrames  answer_1, answer_2, answer_1, stand   loopCycles 2")
    print("  tailFrames  (none - he stays standing)")
    print("  destination sagot_stand_uniform")


main()
