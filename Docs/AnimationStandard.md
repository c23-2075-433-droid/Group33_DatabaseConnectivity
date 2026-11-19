# SALINLAHI animation standard

Every action Kylo performs gets a drawn animation: a starting pose, a
preparation movement, the action itself, and an ending pose. He never snaps
from one drawing to another.

This document is the standard and the backlog. It exists because the rule is
easy to state and the work is not — see **How big this is**.

## The rules

1. **No instant pose changes.** A new drawing replacing an old one in a single
   frame is the thing this standard exists to prevent.
2. **Four parts to every action**: starting pose → preparation → action →
   ending pose.
3. **Intermediate frames**, so the movement reads as movement.
4. **Natural body movement** — arms, hands, head, facial expression where it
   helps.
5. **Objects meet his hands.** A prop appears as the hand reaches it, not
   before and not after.
6. **No floating props, no clipping, no stiffness, no teleporting.**
   - **A prop STANDING on a surface needs a shadow, a contact and a
     perspective**, or it is a sticker on the glass however exactly it is
     placed. A cut-out lifted out of a character sheet is lit evenly,
     drawn dead square on and has nothing under it — three things no
     object in a room has. Give it the room's light (dark away from the
     window, a warm line towards it), a soft wide cast shadow on the
     surface plus a tighter darker one where it touches, a few degrees of
     lean, a few per cent of taper so the far end goes away from us, and a
     little spread where it takes its own weight. Keep its own colours and
     drawing: this is lighting, not redrawing.
   - **And check the cut for borrowed pixels.** A prop found as a region
     and grown to pick up the line round it has grown that far into
     whatever it was lying against — here his white shirt, invisible
     against pale lockers and a white rind down one side against dark
     wood. Fill the thing's own outline and drop every solid pixel outside
     it, rather than hunting pale colours, which takes the zip off too.
   - **CUT, THEN SCALE, IS THE WRONG ORDER.** A cut-out's transparent
     pixels still carry whatever RGB was there before, and no resampling
     filter knows that alpha means "ignore this colour". Enlarge a cut-out
     and the filter drags that colour out across every edge of it: the
     worn backpack came out with an orange fringe down both straps and a
     dotted line for an outline, which is what "broken" looked like on
     screen. Either scale the source and cut at the final size, or replace
     every soft pixel's colour with its nearest solid neighbour's first.
   - **A THING HIS OWN ARM COVERS IS NOT A DRAWING YOU HAVE.** In
     `talking_uniform_with_bag` his forearm lies across the right strap, so
     the difference that gives the bag has a hole in it, and the file that
     built it filled the hole by laying one row of strap along a
     hand-measured path. That band — too wide, splayed, not the shape
     anyone drew — was the navy mass swallowing his sleeve. His LEFT strap
     is drawn whole, because his left arm is raised in that pose, so the
     right one is **the left one mirrored**: same band, same buckle, same
     outline weight. Take the mirror axis from the two drawn straps, row by
     row over the rows they share, and NOT from his silhouette — his
     outline at chest height has a raised arm in it, which puts the middle
     of him nineteen pixels out and crosses the straps in the centre of his
     chest. Mirror what is behind him too, or the bag peeks over one
     shoulder only and reads as a fin stuck to it.
7. **Consistent proportions and appearance** in every frame of every action.
8. **Smooth joins between consecutive actions**: one action's ending pose is
   the next one's starting pose.
9. **Return to idle** after an action, or run straight into the next one.
10. **Loop only what repeats in life.**
    - *Loop*: walking, running, writing, reading, idle breathing.
    - *Play once*: sitting down, standing up, picking up, putting down,
      drinking, raising a hand.

### Voice

An animation starts when the child says the Tagalog word and the recogniser
accepts it — never on a timer, and never skipped to the final pose. In the
builders that means wiring `PoseSequence.Play` to the word's
`objective.onCorrect`, and leaving `playOnEnable` **off** for any pose whose
object is already on screen.

### Character

Use the existing Kylo. Do not change his clothes, hairstyle, facial features,
body proportions or design. Reference images are for movement only.

## How the code expresses this

`Assets/Scripts/PoseSequence.cs` is the component every animated action uses.
Its four fields are rules 2 and 10 made concrete:

| field | part of the action | plays |
|---|---|---|
| `frames` | starting pose + preparation | once |
| `loopFrames` | the action itself | `loopCycles` times, or until `Stop()` |
| `tailFrames` | the ending movement | once |
| `destinationSprite` | the idle pose | rests here |

Leave `loopFrames` empty for a play-once action; that is also exactly how the
component behaved before looping existed, so nothing already built changes.
`loopCycles` of 0 or less repeats until `Stop()` — that is for walking, which
ends when the character arrives rather than after a set count.

### Loops that end on a word

Rule 10 says loop what repeats in life, and reading repeats — but a child
does not read for three tidy cycles, they read until something stops them.
Paaralan 3's "Basa" is the first beat in the game whose END COMES FROM
OUTSIDE: `loopCycles` of 0, which runs until `Stop()`, and the thing that
calls `Stop()` is the next word being recognised.

Three pieces make that work, and any future beat that waits on the player
wants all three:

- **`loopCycles = 0`** on the looping action.
- **`PoseSequence.interrupts`** on the action that takes over. Setting it
  makes `Play()` call `Stop()` on the other one and wait for its
  `tailFrames` before starting — so the book is closed and back on the desk
  before the pencil comes out. Without it, `Play()` would seize the renderer
  mid-loop and the book would simply vanish from his hands. This is rule 8,
  smooth joins between consecutive actions, for two beats that overlap in
  time rather than merely follow one another.
- **`PoseSequence.IsHoldingLoop`**, which `SceneObjectiveController` checks
  so it does not *wait* for an endless loop. It otherwise holds the next word
  back until `maxActionWait` expires — four seconds of a child being asked
  nothing while waiting for a loop whose end is that very word.

`fireOnFrame` / `onFrameReached` is rule 5: it makes a prop appear on a chosen
frame of the lead-in, so the thing he puts down arrives as his hand reaches the
desk. One frame late and it vanishes from his hand and pops into place, which
is the seam the whole component is meant to hide.

Two other components, neither of which is a substitute:

- `PoseSettle` eases the *transform* so a pose arrives rather than appears. It
  is the stand-in for when the drawings do not exist yet. Its own comment calls
  it "the cheap nine-tenths".
- `IdleBreath` wobbles the transform about 1% at roughly one breath every three
  seconds. That is rule 10's "idle breathing" and it needs no art.

Scenes scale their timings by a single `Pace` constant (Paaralan Scene 2 uses
`0.65f`) rather than setting frame rates directly, so one number slows a whole
scene. Anything measured in seconds divides by `Pace`; anything per-second
multiplies by it. Walk frame rate must scale **with** move speed or he skates.

## How big this is

Counted from the builders, not estimated:

- **70 word-actions** across 14 gameplay scenes
- **49 distinct words**
- **10 of them animated** — Bati, Upo, Kinig, Kuha, Lapis in Paaralan Scene 2;
  Aklat, Basa, Sulat, Sagot in Paaralan Scene 3; Sipilyo in Bahay 3

| scene | words | drawn frames? |
|---|---|---|
| Bahay 2 | Lakad Kaliwa Kanan Bukas Ilaw | no |
| Bahay 3 | Tuwalya Sabon Maligo **Sipilyo** Suklay | Sipilyo only |
| Bahay 4 | Damit Medyas Sapatos Bag Kunin | no |
| Bahay 5 | Upo Kanin Itlog Kain Inom | no |
| Paaralan 1 | Lakad Bukas Kumusta Pasok Takbo | no — snaps |
| **Paaralan 2** | **Bati Upo Kinig Kuha Lapis** | **yes — all five** |
| **Paaralan 3** | **Aklat Basa Sulat Sagot** | **yes — all four** |
| Paaralan 4 | Labas Kain Laro Tawa Tulong | no — snaps |
| **Paaralan 5** | **Ligpit Tayo Kunin Paalam Alis** | **yes — all five** |
| Parke 1 | Lakad Takbo Tingin Tuwa Pasok | no — snaps |
| Parke 2 | Akyat Dausdos Sakay Tulak Talon | no |
| Parke 3 | Kumusta Ngiti Tanong Laro Bati | no — snaps |
| Parke 4 | Upo Kain Ibahagi Inom Salamat | no |
| Parke 5 | Paalam Lakad Tingin Pagod Uwi | no — snaps |

"snaps" means the scene uses `PoseSwitch` or `SpriteSwap`, which changes the
drawing in one frame — rule 1's exact failure.

### What that costs

At four to six frames per action, 49 distinct actions is roughly **250
drawings**. Canva generates about three usable figures per strip, and the API
returns only ~150–200px thumbnails, so every strip needs a manual
full-resolution download from Canva Uploads, and generations are rate limited
to roughly one per cooldown window.

So this is on the order of **80 generations and 80 manual downloads**, plus the
cutting and fitting for each. Paaralan Scene 2's four actions took most of a
working session. This is not a task that finishes in one sitting, and planning
around that is more useful than pretending otherwise.

### What makes it cheaper

- **Words repeat.** 70 actions, 49 distinct — Lakad appears in four scenes, Upo
  in three, Pasok, Kain, Inom, Tingin, Kumusta, Tanong, Laro and Bag in two.
  Animate the word once and reuse it.
- **Some poses already exist.** `lapis_frame_1-3` + `pencil_ready_uniform` is a
  pencil being raised, which is the lead-in for Sulat. `kuha_frame_*`,
  `place_frame_*`, `sit_frame_*`, `walk_*` likewise.
- **Reuse the hair-cap trick.** A head cannot change size between poses, so
  normalising every frame to the base sprite's hair height is what keeps the
  character consistent across separately generated strips. See
  `Tools/fit_seated_pose.py` and `Tools/make_sit_frames.py`.
- **Some frames are a part of him moved, not a new drawing of him.** A head
  tilting, a head nodding, a forearm waving, a body turning — in each the body
  is the shape it already was with one piece of it somewhere else, and a piece
  can be cut out and moved in a script. `Tools/make_kinig_frames.py` swings his
  head about his neck and `Tools/make_bati_front_frames.py` swings his forearm
  about his elbow; both are seconds to run and seconds to re-tune, where a
  strip is a generation and a manual download.

  Rule 7 holds by construction this way: it is the same drawing, so his
  proportions cannot drift. Two things make the cut invisible — put it where
  the body is NARROW (the neck is 86 pixels across against 355 at the
  shoulders), and clear the body a little further back than you cut the piece,
  so the wedge a rotation opens lands on the body underneath instead of on the
  room behind it.

  **Cut at the right joint.** A wave is the WRIST, not the elbow. Rotating the
  forearm moves the hand sideways and tears away from the sleeve past about 20
  degrees, and it can never raise the hand, because rotating a bent arm about
  its elbow does not extend it. The narrow cross-section finds the joint for
  you: 34 pixels at the wrist against 63 at the elbow.

  **What it cannot do** is invent. There is no three-quarter view of his face,
  so he cannot turn his head to look at someone; there is no arm drawn at a new
  angle, so a hand cannot go somewhere the existing hand does not reach. Moving
  pixels is for a pose he is already in. A pose he is not in is still a
  generation.

- **The object in his hands is not him.** A whole action can be reused by
  changing only what he is carrying. Paaralan 3's "Aklat" is Paaralan 2's
  "Kuha" — the same reach into the same bag — with the notepad replaced by a
  textbook: `Tools/make_aklat_frames.py` lays the book over the notepad's own
  footprint and then puts his fingers back on top of it, found by skin
  colour. Seven frames for one prop drawing. The limit is that it cannot
  change his GRIP: the new object has to be the size and angle the old one
  was, because that is where his fingers close.

- **A prop is four shapes, so draw it.** The Canva API returns a ~150px
  thumbnail and the full-size file has to be fetched by hand from Canva
  Uploads. That is unavoidable for a drawing of a boy. It is not for a book,
  a pencil or a notebook — use the generation as the DESIGN and redraw it in
  code at whatever size is wanted, which is what `make_classroom_props.py`,
  `make_open_notebook.py` and `make_aklat_book.py` all do.

- **He can look at what he is doing.** `Tools/look_down.py` drops his head on
  its own neck and slides his irises down inside his glasses, which is a
  front-facing drawing's honest version of a bowed head. Both "Basa" and the
  "Sulat" fix lean on it.

- **CHECK THAT THE POSE HAS HANDS IN IT.** `fix_seated_hands.py` cuts every
  seated pose off below the sleeve and replaces the hands with a forearm
  cross-section, so the arms read as running down behind the desk. That is
  right for sitting and wrong for anything the hands do. `sulat_frame_1..3`
  were generated specifically to write, got the pencil point onto the page,
  and inherited the cut — so what shipped was a pencil floating beside a
  forearm with nothing holding it. The fix was not more head movement; it
  was `lapis_frame_1`, which has his fingers closed round a pencil because
  the beat it was drawn for is picking one up.

- **A seated drawing and a standing one DO NOT JOIN.** "Sagot" was first
  built by grafting `talking_uniform`'s hips onto the seated torso at the
  desk line and easing the two widths into each other. It gave him a white
  band round his waist like a sash and arms ending in mid-air: the seated
  figure is 365px across at that line because his ARMS are in the number,
  and the shirt stretched to meet it was a bare 270. No easing invents the
  arm that should be in the gap. Use a WHOLE standing drawing and graft only
  at a narrow joint — the shoulder — the way the rest of this section says.


### His sprites do not line up sideways

Every pose of Kylo's is 745 pixels tall, crown on the first row and feet on the
last, so swapping one for another never moves him up or down. **Nothing makes
that true across the canvas**, and measuring where his feet sit relative to the
middle of each canvas:

| sprite | feet, from the canvas centre |
|---|---|
| `standing_uniform_with_bag` | +28.5 px |
| `talking_uniform_with_bag` | +16.5 px |
| `walk_frame_1..4_uniform_with_bag` | +1.5 px |

A sprite draws centred on its object, so those offsets are where his feet land.
At the size Paaralan Scene 2 draws him, 27 pixels is **0.35 world units** — he
jumps a third of a unit sideways the instant a walk starts, and a sixth of one
when he turns to wave. In a walk the body is moving anyway and it mostly hides;
in a turn on the spot it reads as a glitch, which is how it was found.

`Tools/make_bati_front_frames.py` pads its own frames to the standing pose's
offset, so that beat is square. **The four walk frames are still out**, in every
scene that walks. Fixing them is padding, not drawing — the same `align_feet` —
but they are shared by fourteen scenes, so it wants doing deliberately rather
than as a side effect of something else.

### Order worth doing it in

1. ~~**Paaralan 3**~~ — **done**. Aklat, Basa, Sulat and Sagot are all drawn,
   and between them they cost ONE Canva generation: a book. Everything else
   is Paaralan 2's poses and Tanong's hand-raise, moved. That is the method
   below working, and it is worth reading what each one actually took before
   budgeting a generation for the next scene:

   | word | what it reuses | what was new |
   |---|---|---|
   | Aklat | Paaralan 2's whole Kuha run | the object in his hands |
   | Basa | the frame Aklat carries the book in | an open book, and his gaze |
   | Sulat | lapis_frame_1, the pencil pick-up | his gaze, and the hand travelling |
   | Sagot | talking_uniform + Tanong's raised arm | a graft at the shoulder |

   Two things came out of it that the next scene gets for free:
   `Tools/look_down.py`, which drops his head and his gaze onto whatever is
   in his hands, and `PoseSequence`'s endless loop with a clean hand-over -
   see **Loops that end on a word** above.
2. ~~**Paaralan 4**~~ — **done**. Ligpit, Kuha, Kain, Inom and Tapon are all
   drawn, and between them they cost NO Canva generation at all: every pose
   is one this project already had with something else in the hands, and
   every object is drawn in code by `Tools/make_recess_props.py`.

   | word | what it reuses | what was new |
   |---|---|---|
   | Ligpit | Paaralan 2's whole Kuha run, **reversed**, plus lapis_frame_1/2 and `PropCarry` for the pencil | nothing |
   | Kuha | the same run forwards | a lunchbox over the notepad |
   | Kain | lapis_frame_1/2/3, the pencil being raised | the pencil erased, a hotdog in its place, and **a mouth that opens, bites and chews** |
   | Inom | the same three | a bottle, turned to reach his mouth, and lips that purse and swallow |
   | Tapon | the same three, plus PropToss | a wrapper, and a bin |

   Five things came out of it that the scenes after it get for free:

   - **An action can be run backwards.** Taking a flat object out of a bag
     and putting one in are the same frames in the other order. Scene 4's
     Ligpit is Scene 2's Kuha with the list reversed and not one new sprite.
   - **`PropToss`** — a transform on an arc, for the object in an action
     whose whole meaning is the object's journey. PoseSequence swaps
     sprites, which is right for a character and absurd for a thrown
     wrapper. Any later "throw" or "drop" can use it as it stands — but
     only once the object has been LET GO of. The next bullet is the other
     half, and Scene 4 found out the hard way which half is which.
   - **`PropCarry`** — the same idea for an object that is still IN A HAND,
     which is what a "put in" actually is. The difference is the clock. A
     toss is given a duration of its own and arrives when that says to; a
     carry is given a **waypoint per drawn frame** and the pose's own
     `framesPerSecond`, and spends one frame's worth of time on each leg,
     so it is wherever the drawn hand is on every frame and the two cannot
     drift however the scene's Pace is changed. Scene 4's Ligpit was
     written as a toss first, and the pencil reached the bag after his hand
     had been in and come back out again. Paaralan 5's Ligpit and any later
     "put in" wants this one.
   - **An object can be taken OUT of a pose, not only put into one.**
     `Tools/make_recess_frames.py` erases the drawn pencil out of
     lapis_frame_1..3 and leaves an empty grip to put anything in. What
     that cost is written up there at length and is worth reading before
     the next erase, because the obvious method is the wrong one. Only the
     barrel of a pencil can be found by colour - the cone is within two
     points of his skin, the point is the same black as every outline, the
     ferrule is his glasses and the eraser is his lip - so the first three
     versions FLOODED outwards from the barrel through "anything that is
     not him". The flood does not stop at the pencil, because the drawn
     lines of his own hand are not "him" by any colour test either: it
     walked every crease in his fist and returned a mask the size and
     shape of his hand, which was then erased and filled with the average
     of its surroundings. Every frame of that scene had a smeared,
     edgeless mitten in it for three passes. **A rigid object is found by
     geometry, not by connectivity** - fit an axis to the part you CAN see
     and take the capsule round it - and the hand is put back as a REGION
     rather than as a list of pixels that passed a colour test, or it
     comes back without its own outline.

     **The capsule erases the BACKGROUND. It does not restore the HAND**,
     and using it for both is the second mistake, which survived three
     passes after the first was fixed. A capsule is a straight band, and
     his fingers are drawn in FRONT of the barrel: through the fist, 3,175
     of its 4,281 pixels are his own skin and his own outlines and barely
     a hundred are pencil at all. Taking his hand from the erased copy
     over that whole band put a pale straight-sided slab down the back of
     it - no creases, no knuckle, no edge on one side - in every frame
     that lifts food to his face. His hand comes back from the DRAWING,
     untouched, and the only thing painted out of it is the pencil where
     it is actually drawn: the sliver of barrel between thumb and
     forefinger and the eraser below his little finger, which is all of a
     pencil you can see once a fist closes round it. Three other things
     fell out of the same confusion and are worth knowing by name: an
     eraser left behind because the "pencil" stopped at the end of the
     BARREL and the pink is forty pixels further on, inside his palm; a
     skin-coloured spike standing above his knuckles because a hole-filler
     that calls anything 8px from transparency "inside him" believes a
     50px-wide trench open to the sky; and a faceted, folded-paper palm
     because a spread creeping in from both sides of a hole that wide
     meets in the middle, and the two sides of a drawn hand are not the
     same colour. Fill a slot the width of the OBJECT, not the width of
     the search, and ask whether the drawing is still there on BOTH sides
     across it before filling anything at all.
   - **His face can be redrawn, which is new.** `Tools/mouth.py` paints the
     drawn smile out and puts an open mouth, a chewing one, pursed lips or
     a swallow in its place, and warps the face for a full cheek or a
     dropped chin. Rule 4 asks for facial expression "where it helps", and
     eating is the case where the face IS the action - a boy holding food
     against a closed smile is not eating, however well the arm moves. The
     colours and the proportions are sampled from `eat_bite_bahay`, the one
     drawing in the project of this boy with his mouth open, so a mouth
     drawn in code and one that was generated are the same mouth. Any later
     Kain, Inom, Tawa or Kanta can use it.

   One limit found and not solved: a swap cannot change the SHAPE of the
   footprint, only what is drawn in it. A lunchbox will not cover the tall
   thin notepad in `place_frame_1`, so that frame is missing from Scene 4's
   Kuha and the box goes straight from the bag to his chest.
3. **Paaralan 5** — **all five, and not one new drawing.** Every word in the
   scene is drawn and none of them cost art, not even a code-drawn prop:
   Ligpit is Scene 4's pack-away chain entire, `PropCarry` and all; Tayo is a
   figure assembled out of three drawings this desk already had; and Kunin,
   Paalam and Alis are that figure cut at its joints and turned.

   Tayo began as Scene 3's rise with the hand-raise lead-in dropped — the
   hand goes up for "Sagot", not for going home — and that was wrong in the
   way a borrowed ending usually is. Scene 3's rise ends on
   `sagot_stand_uniform`, which is `talking_uniform` entire: hands up by his
   chest and his mouth open. He stood up to go home and then held the pose of
   a boy mid-sentence for the three words after it. What standing up should
   leave is **his idle, facing forward**, and no such drawing existed.

   The scene was moved into Scene 4's room in the same change. It used to
   open on `klasrum_background`, the wide shot of the whole room, so Scene 4
   faded from a boy at his desk to a boy across a different classroom with
   nothing in the story having moved him. Every placement in
   `BuildPaaralanScene5.cs` is now Scene 4's, copied the way Scene 4 copied
   Scene 3's.

   | word | what it reuses | what was new |
   |---|---|---|
   | Ligpit | Scene 4's four chained legs and its `PropCarry`, whole | nothing |
   | Tayo | `sitting_front_uniform` + `talking_uniform`'s shorts + `standing_uniform`'s hand | nothing |
   | Kunin | Tayo's figure, cut at the elbow and turned; `talking_uniform_with_bag`'s backpack | nothing |
   | Paalam | the same arm, turned further, over the worn bag | nothing |
   | Alis | the same figure, cut at the hems of his shorts; `WalkOffAndFade` | nothing |

   `Tools/make_tayo_frames.py` builds it, and two things came out of it that
   the scenes after it get for free:

   - **GRAFT WHERE TWO DRAWINGS ARE THE SAME PART OF HIM**, which is the
     rule the Sagot note above was reaching for and did not quite state.
     Sagot tried to join a seated figure to a standing one at the DESK LINE,
     where the seated one is 407 pixels across because his ARMS are in that
     number against talking_uniform's 264 hips, and got a white sash and
     arms ending in mid-air. Join the same two drawings at the SHIRT, 745
     rows below his crown, and they measure within six pixels of each other
     at every row — because his shirt is his shirt whether he is sitting or
     standing, and his arms are not. His own forearms then carry down out of
     the seated drawing: they are vertical and a constant 67 pixels wide for
     its last hundred rows, so continuing them is copying a row.

     Two things the join still needs. **The two drawings are not lit the
     same** — the seated shirt averages (254,254,254) and
     `talking_uniform`'s (231,232,236) — so the graft shows up as a tone
     step across his waist even when the silhouettes line up, and the fix is
     a per-channel GAIN on pale neutral pixels, which keeps the hem's
     shading as shading. And **redraw the outside line rather than rescue
     it**: whatever survives of a drawn outline through a cut is thinner
     than the one on the limb it joins, so stroke the finished silhouette at
     the forearm's own colour and width instead. Pad the mask upward before
     eroding it, or the stroke closes across the wrist as a bracelet.

   - **A PROFILE'S HAND IS NOT A PROFILE.** A hand hanging loose at a side
     is drawn broad-face-on even in a side view — fingers down, the back of
     it to the camera — so `standing_uniform`'s is usable front on, and it
     is the only relaxed hanging hand in the project. Scale it by hair cap,
     not by matching the forearm's WIDTH: the profile forearm is narrow
     because it is seen edge-on and the hand beside it is not, and matching
     widths makes the hand half again too big.

   And one thing that did not have to be solved at all: **his fingertips go
   behind the desk.** Standing, the desk crosses him 910 rows below his
   crown and his fingertips land at 945, so the last third of each hand was
   never drawn and never needed to be.

   **A LIMB THIS PROJECT DREW CAN BE DRAWN GOING SOMEWHERE ELSE**, and that
   is Kunin, which was written off in the paragraph this one replaces.

   The claim was that `tayo_stand_uniform` is a pose to start and end on and
   nothing more, because the method that built it can only put him where
   some drawing already has him, and nothing in the project has him reaching
   on his feet at this scale. Both halves are still true of the DRAWINGS.
   What they miss is that **his forearms are not a drawing**: the seated
   sprite stops flat at the desk's far edge, so `make_tayo_frames.py` had to
   synthesise them out of one copied row, a taper and a borrowed hand just
   to get him standing. `Tools/make_kunin_frames.py` lays that same row
   along a path that bends away from his side and turns the same hand
   rigidly to point along it, and he reaches.

   Four things make that work, and they generalise to any reach from any
   pose this project assembles rather than imports:

   - **Bend where nothing is drawn to contradict.** The bend starts 24 rows
     INSIDE the seated drawing's own arm, so the corner it opens is under
     pixels composited over it afterwards, and the angle eases in over the
     next fifty rows instead of switching on. Nothing is cut, so the
     20-degree tearing limit further up this document — which is about
     rotating a DRAWN forearm inside a drawing — does not apply: 52 degrees
     is fine.
   - **A limb of horizontal strips must be cut off square to ITSELF.** Each
     strip is widened by 1/cos of the angle, or the arm goes thin as it
     swings. It then ends on a horizontal line while the hand on the end of
     it has been turned, which leaves a wedge of nothing on the outside of
     the bend — so the strips run past the wrist and everything beyond the
     line ACROSS the arm there is erased.
   - **A pose with and without a thing is that thing.** `talking_uniform`
     and `talking_uniform_with_bag` are one drawing with and without the
     backpack, so subtracting them is the whole bag — straps, buckles and
     all — and no part of it has to be drawn. Split it by HIS OWN
     silhouette in the plain drawing and you get the two layers a worn bag
     needs: inside it goes over him, outside it goes behind him. Composited
     as one, the bag sits on top of the arm hanging in front of it.
   - **What the subtraction cannot give, lay down the same way as the
     limb.** His own forearm crosses the right strap in the source, so the
     difference has a hole in it where his arms are DOWN. One row of the
     strap, laid along a path read off the drawing above and below his arm,
     fills it — and the line down each side is REDRAWN at the project's own
     outline colour and width rather than stretched, because one pixel of
     outline stretched to a band's width comes out as a grey smear.

   **AND THEN THE BEND TURNED OUT TO BE THE WRONG TOOL.** The paragraph
   above ends by saying that a wave and a turn away are what is left of
   Paaralan 5, because a wave is above his shoulder and the bend is below
   his elbow. That was right about the bend and wrong about the scene, and
   the thing that showed it was Kunin itself failing in play.

   The bend starts at the desk line, which on a STANDING boy is the top of
   his shorts — two thirds of the way down his forearm. So "52 degrees of
   reach" was 52 degrees of WRIST: his hand moved about twenty pixels, the
   bag slid a third of a unit sideways and a fifth of one DOWNWARD behind
   the desk, and the frame after that had the backpack on his shoulders.
   Every number in it was right and the beat was unreadable.

   `Tools/make_uwian_frames.py` replaces it, and the finding is one line:

   > **CUT HIM AT THE JOINT AND TURN THE PIECE WHOLE.** A bend can only go
   > where nothing is drawn to contradict it. A rotation about a real joint
   > can go anywhere, because nothing inside a rigidly rotated piece moves
   > relative to anything else inside it.

   That is `make_bati_front_frames.py`'s technique — written up there for
   the wide-shot figure — brought to this desk, and it reaches every pose
   the bend could not:

   - **An arm turned about ONE point is ONE BONE**, whatever the point is
     called, and the first version of this file called it an elbow. It
     reached out and down to a bag on a desk at 48 degrees and lifted it
     clear at 76, and both of those were fine, because a child reaching
     across a desk does straighten his arm. Past that it was not: at the
     130 degrees the wave asked for, the drawing that arrived was a single
     straight stick running from his armpit to his fingertips with a
     RELAXED HANGING HAND on the end of it, upside down, thumb outboard.
     That is the "broken arm" the revision notes describe, and no angle
     fixes it, because the drawing in those frames is a drawing of an arm
     at rest.

     **TWO BONES AND TWO BALLS.** The arm is cut at the hem of his sleeve
     and again at the crease of his elbow, and each piece turns about its
     own joint — abduction at the shoulder, flexion at the elbow. Each
     joint is a DISC centred on the point of rotation, cut out of both
     pieces at the same radius, because a disc about its own centre is the
     one shape a rotation leaves exactly where it was: the two bones mate
     at every angle with no wedge to hide and no seam to ease. Three things
     this cost an afternoon each, and all three generalise:

     - **Measure the joint on the SKIN, not on the grown piece.** The piece
       carries six pixels of slack on each side so the drawn line comes
       away with the limb; a radius read off it is a dozen pixels too big,
       and here that was the difference between two joints and one — the
       elbow's disc swallowed the shoulder's and the upper bone with it.
     - **The shoulder's width is measured BELOW the hem and the disc is
       centred back up ON it.** At the hem the cloth is over the arm, so
       the skin there is only as wide as the gap in the sleeve, and a disc
       cut to that leaves the outer corner of the hem with nothing under
       it — a grey triangle that opens the moment he reaches.
     - **Six pixels above the top of a bare arm is not the line round the
       arm. It is the SLEEVE.** Carried along with the piece and turned, a
       sleeve hem is a black bar across his shoulder at every angle but
       zero. Start the drawn limb a few rows clear of the cloth and let the
       ball fill what is left.

   - **A WAVE IS NOT AN ANGLE OF A HANGING ARM. IT IS A DIFFERENT
     DRAWING.** `talking_uniform` is this same boy at this same scale with
     one arm already up: a drawn sleeve, a drawn upper arm, a drawn elbow
     bent square, and a drawn open palm with the fingers apart. Mirrored
     and head-matched it lands on the standing figure's shoulders within a
     few pixels, because `make_tayo_frames` already grafts that drawing's
     lower body on at the waist. Graft its ARM on as well and the wave is
     one rotation of the forearm about a joint somebody drew — which is
     `make_bati_front_frames`' mechanism, on the drawing it was written
     for. The sweep is a loop of three positions of it, which is what makes
     it a wave rather than a hand held up: the project's rules list waving
     among the things that REPEAT, and a raised hand that does not come
     back is a salute.

     **GRAFT ONLY WHAT DIFFERS.** Two drawings of one boy in one white
     shirt are the same colour nearly everywhere, and a rectangle of one
     laid over the other shows up as a tone step across his chest. Take the
     union of what is NOT shirt-white in either, close it, fill it, and add
     the sleeve's own rows; everywhere else there was nothing to carry
     across. Then match the white once more against the chest the graft
     will actually sit next to — two hundredths of a stop, and the
     difference between a seam you cannot find and one you can.

     **AND A FILL THAT HUNTS HAS TO BE LEASHED.** Filling what a lifted arm
     leaves behind from its own edges is right where the arm lay against
     his body and nonsense where it hung in the open: with no limit the
     search finds the nearest solid pixel however far away it is, and the
     room fills with fans of smeared shirt and a lump of his own hair,
     hanging in the air beside his hand. Past a dozen pixels the hole is a
     hole, because past a dozen pixels there was nothing behind the arm.
   - **Legs turned about the HEMS OF HIS SHORTS** walk. The hem is the
     joint to use rather than the hip inside him: a leg turned about the hip
     slides sideways where it leaves the cloth and leaves a step in it,
     while a leg turned about the hem keeps the cloth where the cloth was,
     which is what a loose school short does over a moving leg anyway.
   - **ASSEMBLE THE BODY WITHOUT THE LIMB; DO NOT CUT THE LIMB OUT OF THE
     BODY.** `make_bati_front_frames` lifts the arm off a finished drawing
     and fills what it was covering from its own edges, because on an
     imported drawing there is nothing else to do — and the fill smears
     shirt-white out into the room wherever the limb hung against nothing.
     A figure this project ASSEMBLES can simply be assembled without that
     limb, and then what is behind it is the thing that was behind it. The
     first version of this file had a white block beside his hip in every
     frame of the reach; that is the whole of the fix.
   - **A front view turns by getting narrower**, which is that file's cheat
     again and is used here for ONE frame — the moment he turns to go. Held
     for the whole walk it stops being a boy who has turned and becomes a
     boy with different proportions.

   One thing the walk needed that the turning did not give: **his legs had
   to reach the bottom of the picture.** The `tayo_*` canvas ends at world y
   -4.84, half a unit above this camera's bottom edge — invisible while the
   desk is in front of him, and a flat cut across his shins the moment he
   walks out from behind it. The `uwian_*` canvas is 120 rows taller at each
   end and his shins are carried down out of frame by copying their own last
   row, which is `make_tayo_frames`' forearm trick applied to the other end
   of him. Pad SYMMETRICALLY or the figure slides sideways, and keep the
   crown the same distance above the canvas centre as the older frames have
   it — do both and sprites of different sizes register pixel for pixel and
   a `PoseSequence` can cut between them.

   **AND THEN ALL OF THAT WAS REPLACED BY DRAWINGS.** In play, Kunin, Paalam
   and Alis still read as what they were: an arm pasted on at a seam, strap
   ends standing up off his shoulders like wings because the straps were
   painted on in code, and a "walk" that was two legs swinging under a boy
   who never turned round. Moving pieces of one front-on drawing cannot put
   a bag on a back or turn a body. `Tools/make_uwian_canva_frames.py` cuts
   them from six Canva generations instead - four poses each, from the
   Paaralan sprites as the reference - and `make_uwian_frames.py` is gone.
   What it took, all of which generalises:

   - **Four poses in one generation, in a 16:9 strip.** One pose per image
     gives a better head size but four images of four slightly different
     boys; a strip of four matches itself, and at 1680x944 each head still
     comes out about 170px. Feed the previous strip back in as a reference
     and the next one matches that too.
   - **The full-size file comes out through a design.** A copy of "SALINLAHI
     Art Export" resized to the generation's size, its picture swapped,
     saved and exported as PNG - `make_guard_push_frames.py` has the steps.
     The generation itself is only ever a 200px thumbnail. Canva also cools
     down after every generation, for a few minutes, so a strip is a sitting.
   - **Measure a strip by a pose with its hands down.** `hair_cap` takes the
     first ink from the top as the crown, and on the wave that was his
     fingertips: the strip came out 4.6% too big. Size the first strip by
     head, to the frames before it, and every later one by BODY HEIGHT to
     that, because separate generations do not quite agree on proportion.
   - **Anchor by whatever is SEEN.** Stood by his feet, the reach - which
     leans his whole body out over planted feet - slid his torso a fifth of
     a unit sideways, and his feet are behind the desk in this shot. Stood
     by the top of his shorts, he holds still and leans.
   - **A drawn prop has the generator's size, not the scene's.** The bag it
     drew is 2.9 units; the one Ligpit packs on the desk is 1.66, and that
     one cannot change. So the frame where one bag replaces the other draws
     his at 2.0, and the swing onto his shoulder a frame later takes it the
     rest of the way.
   - **Background shut inside the figure survives the flood fill** - between
     a raised arm and his head, between an arm and a bag held out. Here the
     background is 254 and his shirt 251, so the test that clears it has to
     be strict, and even then it is only safe per strip: in the walk strip
     the lit side of his sleeve is 254 too, and clearing pockets there cut
     holes in his shirt. `POCKET_STRIPS` lists where it runs.
   - **The generator draws the movement; Kylo's head and skin are his own.**
     Canva redrew his hair a different shape, his face rounder and his skin
     paler, and the group's rule is that those do not change. So
     `Tools/kylo_own_head.py` takes each generated head off by its outline
     and puts back `tayo_stand_uniform`'s (front) or
     `standing_uniform_with_bag`'s (profile) at the neck, and matches the
     skin to his per channel. Arms and hands beside the head are kept and
     laid back over it. Do this for any generated pose of him, not only
     these.
4. **The repeated words** — Lakad, Upo, Pasok, Takbo. Each one animated here
   pays off in two to four scenes. Kain and Inom are drawn now, in Paaralan
   4, and the frames are seated - Bahay 5 and Parke 4 sit at a table and a
   picnic mat, so they may take them as they are.
5. **The scenes that snap** — Bahay 3, Paaralan 1, Parke 1, Parke 3,
   Parke 5. These actively break rule 1 today.
6. **Everything else.**

## Pipeline for one action

1. Generate a strip of 3 frames in Canva from the existing sprite as the design
   reference, so the character comes back as himself.
2. Download the full-resolution PNG from Canva Uploads (the API thumbnail is
   too small to use).
3. Cut and fit with `Tools/fit_seated_pose.py` — `cut_background` knows not to
   eat his white shirt, and `HALO_ROUNDS = 40` clears the white slivers between
   his hair spikes. Do not lower it; do not push it towards 200, which eats the
   shirt.
4. Normalise by hair cap so the head is the same size as in the base sprite.
5. Wire into the builder: `frames` / `loopFrames` / `tailFrames` /
   `destinationSprite`, with `Play` on the word's `onCorrect`.
6. Commit each new `.png` **with its `.meta`**, or it gets a fresh GUID on
   another machine and every scene reference to it breaks.

### Resolution: ask for a SQUARE image, and one figure at a time

`check_pose_strip.py` refuses any figure whose hair cap is under 120px, and
the sprite a Bahay Scene 5 frame has to match, `talking_uniform_with_bag`, is
drawn at 142px. Measure hair as `bottom - crown` from `hair_cap`; its third
return value is a centre COLUMN, not a height, and mistaking the two will
tell you a strip passed when it did not.

A generator that returns ~1024px on the long side cannot meet that in a wide
strip, and the limit is the image's HEIGHT, not how many figures are in it.
Measured on Gemini, 2026-11-14:

| ask | image | figures | hair height |
|---|---|---|---|
| 4 figures, 16:9 | 1024x572 | 4 | 95-97px — rejected |
| 2 figures, 16:9 | 1024x572 | 2 | 108-109px — still rejected |
| 2 figures, square | 1024x1024 | 2 | 192px — passes |
| 1 figure, square | 1024x1024 | 1 | 228px — comfortable |

Halving the figure count bought almost nothing, because each figure was
already full-height in a 572px-tall frame. The square frame is what fixed it.

### Ask for ONE figure per image, not a strip

Filling the frame and showing a descent are in direct conflict. Asked for two
figures filling a square, the generator drew both standing at exactly the same
height - crown drop of **0px**, measured - because it composes for the frame.
Told twice, in plain words, to put the second head a head-height lower, it did
it again.

Asked for **one figure alone** in the square, it drew the crouch correctly
first time: knees bent, thighs sloping, hips back, arms at his sides.

This costs nothing, because **the crown drop does not have to come from the
art**. `make_sit_frames.py` already normalises every frame by hair cap and
then places each one so the crown sits at a chosen world height - that is
where the descent comes from. The generator only has to supply the POSE. So:

- one square generation per pose, the figure filling the frame
- frame 1 is free - it is the existing standing sprite
- the pipeline stacks them and steps the crowns down

One limit found the hard way: it will not reliably draw a pose *lower than
the one before*. Asked for a deeper crouch it returned the previous crouch
again; asked for a finished sit with horizontal thighs it returned a deeper
crouch with the hands on the thighs. Describe each pose from scratch in
absolute terms rather than as "lower than the last one".
