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
| Paaralan 5 | Pasok Upo Linis Bag Alis | no |
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
   | Kain | lapis_frame_1/2/3, the pencil being raised | the pencil erased, a sandwich in its place |
   | Inom | the same three | a bottle, turned to reach his mouth |
   | Tapon | the same three, plus PropToss | a wrapper, and a bin |

   Four things came out of it that the scenes after it get for free:

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
     the next erase: the sharpened cone of a pencil is within two points of
     his skin on every channel, so it survives every colour test that lets
     his own fingers through, and it has to be told apart by what it is
     JOINED to instead.

   One limit found and not solved: a swap cannot change the SHAPE of the
   footprint, only what is drawn in it. A lunchbox will not cover the tall
   thin notepad in `place_frame_1`, so that frame is missing from Scene 4's
   Kuha and the box goes straight from the bag to his chest.
3. **The repeated words** — Lakad, Upo, Pasok, Takbo. Each one animated here
   pays off in two to four scenes. Kain and Inom are drawn now, in Paaralan
   4, and the frames are seated - Bahay 5 and Parke 4 sit at a table and a
   picnic mat, so they may take them as they are.
4. **The scenes that snap** — Bahay 3, Paaralan 1, Parke 1, Parke 3,
   Parke 5. These actively break rule 1 today.
5. **Everything else.**

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
