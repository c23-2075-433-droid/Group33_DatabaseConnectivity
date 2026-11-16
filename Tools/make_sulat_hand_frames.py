"""Give Kylo a hand to write with.

WHAT WAS WRONG, and it is worth being exact because the frames look fine in a
thumbnail. sulat_frame_1..3_uniform are the only poses in the project with a
pencil point on the bottom edge of the canvas, which is the notebook's far
edge - that part was right, and it is why they were drawn. But there is NO
HAND in them. Tools/fix_seated_hands.py cuts every seated pose off below the
sleeve and replaces the hands with a forearm cross-section, so the arms read
as running down behind the desk, and these three inherited that. What is left
is a pencil hanging in the air beside his forearm with nothing holding it.

Seen at the size the game draws him, that is a boy sitting still while a
pencil floats next to him. It is not writing, and no amount of head movement
was going to make it writing.

WHAT IS USED INSTEAD. lapis_frame_1_uniform - the first frame of Paaralan
Scene 2's "Lapis", where he picks the pencil up off the desk. It has
everything the sulat frames do not:

  - his fingers CLOSED ROUND the pencil, drawn, with knuckles and a grip;
  - the pencil held point-down and leaning right, which is how a hand holds
    one to write rather than how it holds one to show it to someone
    (lapis_frame_2 and 3 both have it point-UP, past his chest - those are
    the pencil being raised, and rotating one into a writing grip turns the
    hand upside down with it);
  - the whole thing already at the BOTTOM of the canvas, around x 320, which
    at this scale is world 2.79 - the middle of the desk in front of him,
    where the notebook is.

So he is already holding a pencil over the notebook in a drawing the project
has had all along. What was missing was only that he was looking straight out
of the frame while he did it, and that nothing moved.

WHAT THIS ADDS

  the look      Tools/look_down.py: his head down on its own neck. Deeper
                than "Basa" uses, because a notebook flat on the desk is
                further below his chin than a book held at his chest. His
                EYES are left as drawn - that file explains why at length.
  the stroke    his hand travels. The rows below his elbow slide sideways -
                14 pixels each way, eased in over the 90 rows above them so
                the forearm follows the hand instead of shearing off it.
                Played 1-2-3-2 the point crosses the page and comes back.

FOURTEEN PIXELS IS 0.085 WORLD UNITS, about the width of a letter. It is
small on purpose: this loop runs four times over three seconds, and a hand
sweeping half the page per stroke reads as crossing something out.

Writes  Assets/Sprites/sulat_hand_1..3_uniform.png

Run from the repository root:  python Tools/make_sulat_hand_frames.py
"""
import os
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from look_down import load, look_down, save                  # noqa: E402

SRC = "lapis_frame_1_uniform"



HEAD_DROP = 24          # his eyes stay as drawn - see Tools/look_down.py


#       name                   tilt   what it is
FRAMES = [
    ("sulat_hand_1_uniform",    1.5, "head over the page, leaning in"),
    ("sulat_hand_2_uniform",    0.0, "square to it"),
    ("sulat_hand_3_uniform",   -1.5, "and across"),
]





def main():
    src = load(SRC)
    print("writing, from %s:" % SRC)
    for name, tilt, what in FRAMES:
        out = look_down(src, head_drop=HEAD_DROP, tilt=tilt)
        assert out.size == src.size, (name, out.size, src.size)
        save(out, name, SRC)
        print("  %-26s head %+0.1f deg   %s" % (name + ".png", tilt, what))

    print("\nWire them in BuildPaaralanScene3 as the 'Sulat' loop:")
    print("  loopFrames  sulat_hand_1, 2, 3, 2")


main()
