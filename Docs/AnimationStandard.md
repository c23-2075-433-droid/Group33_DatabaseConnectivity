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
- **5 of them animated** — Lakad, Upo, Kuha, Lapis in Paaralan Scene 2;
  Sipilyo in Bahay 3

| scene | words | drawn frames? |
|---|---|---|
| Bahay 2 | Lakad Kaliwa Kanan Bukas Ilaw | no |
| Bahay 3 | Tuwalya Sabon Maligo **Sipilyo** Suklay | Sipilyo only |
| Bahay 4 | Damit Medyas Sapatos Bag Kunin | no |
| Bahay 5 | Upo Kanin Itlog Kain Inom | no |
| Paaralan 1 | Lakad Bukas Kumusta Pasok Takbo | no — snaps |
| **Paaralan 2** | **Lakad Upo Kuha Lapis** | **yes** |
| Paaralan 3 | Sulat Basa Tanong Inom | in progress |
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

### Order worth doing it in

1. **Paaralan 3** — Sulat, Basa, Tanong, Inom. The scene in hand.
2. **The repeated words** — Lakad, Upo, Pasok, Kain, Inom, Takbo. Each one
   animated here pays off in two to four scenes.
3. **The scenes that snap** — Bahay 3, Paaralan 1, Paaralan 4, Parke 1,
   Parke 3, Parke 5. These actively break rule 1 today.
4. **Everything else.**

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
