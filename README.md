# SALINLAHI

**A Voice-Interactive Filipino Language Learning Game**

**Group 33 — BSIT Capstone**

A voice-interactive Filipino vocabulary learning game for Kinder–Grade 3
pupils. The child says a Filipino word out loud and the game responds: speaking
is the control, not a button on screen. Each session's score is kept on the
device and totalled onto a shared online leaderboard.

**Current release: v0.1.0.** See [Releases](#releases).

## Group Members

| Name | Role |
|---|---|
| Daquis, Jhesza Mhei G. | Game Logic Developer |
| Lacida, Kylo Bryan | UI and Testing Lead |
| Manzanero, Kyla Samantha | Documentation Lead |


BSIT Capstone Project — University of Perpetual Help System Laguna

## Basis

This is our defended capstone title, not a separate guided exercise. The game
produces real learning data — which Filipino words a child pronounced correctly
— which is why database connectivity and the leaderboard were extensions of it
rather than add-ons bolted to the side.

## Short Description

The player guides a child character, Kylo, through a day by **speaking Filipino
words out loud**. The game listens through the device microphone, checks the
pronunciation, and advances the story when the word is said correctly.

**15 playable scenes** across three places, five scenes each:

| Place | Scenes |
|---|---|
| **Bahay** (home) | Umaga na, Banyo, Maligo, Damit, Almusal |
| **Paaralan** (school) | Paaralan, Klasrum, Aralin, Recess, Uwian |
| **Parke** (park) | Parke, Palaruan, Kaibigan, Piknik, Uwi |

**49 distinct Filipino words** are taught across **66 spoken objectives** —
among them Bangon, Tayo, Lakad, Kaliwa, Kanan, Bukas, Ilaw, Tuwalya, Sabon,
Maligo, Suklay, Sipilyo, Damit, Medyas, Sapatos, Kunin, Kain, Inom, Bati,
Pasok, Takbo, Lapis, Aklat, Basa, Sulat, Sagot, Akyat, Dausdos, Sakay, Ngiti,
Salamat and Paalam. Each word drives one action.

Pronunciation is judged by Filipino phonology rather than letter-edit distance,
so a wrong vowel is rejected — saying *kaha* for **Kuha** counts as wrong, on
purpose — and words taught in the same scene are not accepted for one another.

At the end of a scene a summary panel shows the score, the stars and the words
learned. Totals are pushed to the shared leaderboard from the main menu.

The Palengke (market) level and a sixth Bahay scene are designed but not built,
and are shown locked on the map.

## Tools and Technologies

| Item | Used |
|---|---|
| Game engine | Unity 6 (6000.6.2f1), built-in render pipeline, 2D |
| Language | C# |
| Speech recognition | UnitySpeechToText (yasirkula) — device speech recognizer, `fil-PH` |
| Database | Supabase (PostgreSQL) via its REST API |
| Score API | Supabase Edge Function (Deno/TypeScript) — `supabase/functions/submit-score` |
| Release target | Android 8.0+ (API 26), ARM64 |
| HTTP client | `UnityWebRequest` (no third-party SDK) |
| Editor | Visual Studio Code / Visual Studio |
| Repository | GitHub |

## Database: What Is Saved and Retrieved

Per-session records go to the Supabase table `player_scores`:

| Field | Type | Description |
|---|---|---|
| `player_id` | uuid (auto) | Unique identifier for the record |
| `player_name` | text | Nickname of the player |
| `score` | integer | Words pronounced correctly |
| `attempts` | integer | Total voice attempts made |
| `level` | text | Level/scene played |
| `remarks` | text | Readable result, e.g. `3/5 correct` |
| `created_at` | timestamptz (auto) | When the record was saved |

**Saved:** one record per completed session, written when the player finishes
all spoken objectives.
**Retrieved:** the five most recent records, newest first.
**Displayed:** on the level-complete panel inside the game, showing this
session's score plus the recent score list.

### Where the save/retrieve code lives

| Function | File |
|---|---|
| Save (HTTP POST) | [`Assets/Scripts/SupabaseClient.cs`](Assets/Scripts/SupabaseClient.cs) → `SaveScore()` |
| Retrieve (HTTP GET) | [`Assets/Scripts/SupabaseClient.cs`](Assets/Scripts/SupabaseClient.cs) → `FetchRecentScores()` |
| When to save / what to display | [`Assets/Scripts/SessionScoreTracker.cs`](Assets/Scripts/SessionScoreTracker.cs) |
| Record structure | [`Assets/Scripts/ScoreRecord.cs`](Assets/Scripts/ScoreRecord.cs) |
| Connection settings | [`Assets/Scripts/SupabaseConfig.cs`](Assets/Scripts/SupabaseConfig.cs) |

### The shared leaderboard

The leaderboard is a second, separate path, and the only thing the game still
sends to Supabase in normal play. It holds one row per player rather than one
per session.

| Piece | Where |
|---|---|
| Table and policies | [`supabase/migrations/20261112_leaderboard.sql`](supabase/migrations/20261112_leaderboard.sql) — `leaderboard`, `leaderboard_owner`, and the `leaderboard_rank` function |
| Write path | [`supabase/functions/submit-score/index.ts`](supabase/functions/submit-score/index.ts), a Deno Edge Function |
| Client | [`Assets/Scripts/LeaderboardClient.cs`](Assets/Scripts/LeaderboardClient.cs) — reads over REST, writes only through the function |
| Display | [`Assets/Scripts/LeaderboardPanel.cs`](Assets/Scripts/LeaderboardPanel.cs) |

The client is never trusted with a direct write to the leaderboard table. It
calls the Edge Function, which issues a per-player token on first submit and
checks it on every later one, so one player cannot overwrite another's row.
A failed upload is retried the next time the main menu opens.

Only four values leave the device: player id, nickname, total points and total
stars. Which words a child got right or wrong stays on the phone.

Setup is in [`Docs/SupabaseSetup.md`](Docs/SupabaseSetup.md).
## How to Run It From Source

### 1. Open the project
Open the folder in Unity **6000.6.2f1** via Unity Hub.

The first open takes several minutes and **needs an internet connection**: Unity
rebuilds `Library/` from scratch, and the speech plugin is not committed — it is
pulled from GitHub by `Packages/manifest.json`
(`com.yasirkula.speechtotext`). Until it finishes downloading, every script that
touches `SpeechToText` will show compile errors. That is expected; let it finish
before judging anything.

### 2. Set up the database
Follow [`Docs/SupabaseSetup.md`](Docs/SupabaseSetup.md) — it contains the SQL to
create the `player_scores` table and its Row Level Security policies.

### 3. Add your credentials (each member, once)
Credentials are **not** in this repository.

1. In Unity: **Tools → SALINLAHI → Create Supabase Config**
2. Select `Assets/Resources/SupabaseConfig.asset`
3. Paste your **Project URL** and **anon public** key from the Supabase
   dashboard (Project Settings → API)

See [`Assets/Resources/SupabaseConfig.example.txt`](Assets/Resources/SupabaseConfig.example.txt)
for the expected values.

### 4. Nothing to build by hand

All 17 scenes are committed as `.unity` files and already registered in Build
Settings, so a fresh clone has nothing to generate. Earlier revisions of this
file told you to run a list of `Tools > SALINLAHI > Build ... Scene` items;
**do not** — those builders regenerate a scene from scratch, and running one
now discards whatever is in the committed scene.

The builders are still there, and are still how a scene gets changed: edit the
builder in `Assets/Editor/`, re-run it, and commit the regenerated scene. That
is why the scene diffs in this repository are large.

One rule worth keeping. The scene that **ends** a level shows the result; every
other scene carries its own `RunScoreCounter`, which adds that scene's attempts
to [`RunScore`](Assets/Scripts/RunScore.cs). Add a counter to any new scene or
its words go uncounted, with nothing in the log to say so.

### 5. Play
Open `Assets/Scenes/MainMenu.unity` and press Play.

On a **desktop/Editor** run, the device speech recognizer is unavailable, so
keyboard shortcuts simulate recognized speech through the exact same code path:

| Key | Simulates |
|---|---|
| `Enter` | **whatever word the scene is currently asking for** |
| `B` / `T` / `L` | "Bangon" / "Tayo" / "Lakad" (Scene 1) |
| `K` / `N` / `O` / `I` | "Kaliwa" / "Kanan" / "Bukas" / "Ilaw" (Scene 2) |
| `W` / `S` / `M` / `C` / `P` | "Tuwalya" / "Sabon" / "Maligo" / "Suklay" / "Sipilyo" (Scene 3, in order) |
| `D` / `Z` / `F` / `V` | "Damit" / "Medyas" / "Sapatos" / "Bag" (Scene 4) |
| `U` | "Kunin" (take — picks the school bag up, Scene 4) |
| `Q` / `E` / `G` | "Kumusta" / "Pasok" / "Takbo" (Paaralan Scene 1) |
| `X` | a wrong answer (counts an attempt, does not advance) |
| `J` | "Talon" (jump) |

**Enter is the one to use** — it reads the current objective and says that
word, so it works in any scene without needing a key per word. Press it
repeatedly to walk through a whole scene.

Scene 1 ends by walking out of the bedroom, which fades into Scene 2. In Scene
2, saying **Bukas** opens the bathroom door, walks Kylo through it and fades
across into the bathroom, which stays dark until **Ilaw**. Scene 2's last word
fades on into Scene 3, the bath, and Scene 3 fades on into Scene 4, getting
dressed. After Scene 4, the completion panel shows your score for the whole
level and the recent records read back from Supabase.

On **Android**, the real microphone is used instead — say the words out loud.

## Releases

**Current release: v0.1.0** (12 November 2026) — a development milestone, not a
stable release. Android only.

Which build you are holding is answered inside the game: main menu, the
settings gear, then **ABOUT**. The version is printed under the title, read
from the build itself at run time.

| Document | What it is |
|---|---|
| [`Docs/RELEASE_PROCESS.md`](Docs/RELEASE_PROCESS.md) | How a release is produced, numbered, tested and tagged. Read this before building one. |
| [`Docs/release/v0.1.0/RELEASE_NOTES.txt`](Docs/release/v0.1.0/RELEASE_NOTES.txt) | What is in v0.1.0, what is fixed, what is broken, what is missing |
| [`Docs/release/v0.1.0/README.txt`](Docs/release/v0.1.0/README.txt) | Install and run instructions for a tester |
| [`Docs/Activity10_Performance.md`](Docs/Activity10_Performance.md) | The measured performance baseline for v0.1.0, what is being watched as the project grows, and the hotfix procedure |

To produce a release build: **Tools > SALINLAHI > Build Release APK**. It reads
the version from Player Settings rather than setting it, collects the enabled
scenes rather than hardcoding them, and refuses to run if the version, the
scenes or that version's release notes are missing. The output is
`Build/Release/SALINLAHI-v<version>/`, containing the APK alongside its README,
release notes and a `BUILD_INFO.txt` naming the commit it came from.

### Measuring it before you release it

Three tools produce the numbers and the checks that
[`Docs/Activity10_Performance.md`](Docs/Activity10_Performance.md) records, and
they are meant to be re run rather than read. Each writes evidence into
`Docs/perf/`.

```
python Tools/check_backend.py                 nine functional checks on the leaderboard path
python Tools/measure_backend.py 20            times every backend call, split into DNS, TCP, TLS and server
dotnet run -c Release --project Tools/perf_probe    per attempt cost of the pronunciation judgement
```

`check_backend.py` and `measure_backend.py` read the gitignored
`Assets/Resources/SupabaseConfig.asset` for the project URL and key, and never
print the key. Neither writes a row: the two write attempts in the functional
checks are there because Row Level Security must refuse them. The probe needs a
.NET SDK, and the one inside the Unity install will do:
`"D:\Unity\6000.6.2f1\Editor\Data\DotNetSdk\dotnet.exe"`.

The baseline to compare against, measured on 13 November 2026 against the
v0.1.0 sources: board fetch median 226.6 ms of which 128.7 ms is connection
setup, voice judgement 5.64 us per attempt, 682 bytes allocated per attempt.

Build from this checkout, not from a git worktree:
`Assets/Resources/SupabaseConfig.asset` is gitignored and exists only here, and
a build without it has a dead leaderboard.

Android only, deliberately — the speech recognizer is Android and iOS only, so
a desktop build cannot be answered by speaking.

Artifacts are never committed; `*.apk` and `Build/` are gitignored.

## Character Art: Two Sets

Kylo is drawn twice, because the school bag is painted into the character art
rather than being a separate layer:

| Files | Used in |
|---|---|
| `lying_down`, `sitting_up`, `standing`, `walk_frame_1..4`, all with `_barefoot` | Scenes 1–3, at home in his green tee, no shoes on yet |
| `talking_towel.png` | Scene 3 onwards, straight out of the bath |
| `talking_barefoot_uniform`, `talking_socks_uniform`, `talking_uniform` | Scene 4, one stage per word while he is dressed |
| the same names with `_uniform_with_bag` | after **Kunin** in Scene 4 |
| `standing.png`, `walk_frame_1..4.png`, `talking.png`, `celebrate.png` | the shod green-tee set the barefoot one is drawn from |
| the same names with `_with_bag` | the green tee plus bag — kept, currently unused |

The plain set was produced from the original drawings by
[`Tools/strip_backpack.py`](Tools/strip_backpack.py); the `_with_bag` files are
the untouched originals. Before this split, Kylo wore his school bag in bed —
the bag appeared the moment the player said "Tayo", while the same bag was also
sitting on the chair prop beside him.

Each word in Scene 4 switches one prop off and swaps one art set for the next,
through [`PickUpItem`](Assets/Scripts/PickUpItem.cs) and
`PlayerMovement.SetAppearance()`: **Damit**, **Medyas**, **Sapatos**, then
**Kunin**, so he ends the level dressed and carrying his bag. The uniform sets
are [`Tools/make_uniform.py`](Tools/make_uniform.py) recolouring the green
shirt rather than new drawings.

**Medyas** and **Sapatos** only mean something because he starts the morning
barefoot, and the original art had him in shoes in every pose — in bed, in the
bath, in the towel. The `_barefoot` set is the same seven poses redrawn
without them, generated from the shod art one pose at a time so the character,
the clothes and the framing stay the same.

Two things were tried first and did not work, which is why the files are
generated rather than computed. Recolouring the shoe leaves an orange sneaker:
the laces, the stitching and the sole all survive the recolour. Drawing a foot
onto the leg instead leaves a visible step where the new shin meets the old
one, and feet that read as flat paddles at playing size. The walk frames come
back with a narrower stride than the shod ones, which is fine — Scenes 1–3
only ever show the barefoot set, so the four frames only have to match each
other, not the originals.

[`Tools/key_ui_sprites.py`](Tools/key_ui_sprites.py) imports them: it cuts the
white background, scales each pose to the body height of the art it stands in
for, and keeps the existing `.meta`, so the sprite keeps its GUID and every
scene that already points at it carries on working.

Scenes 2–4 ask for the barefoot art through
[`CharacterPoses`](Assets/Editor/CharacterPoses.cs) when they are built. Scene
1 has no builder, so **Tools > SALINLAHI > Use Barefoot Poses (Bahay)** edits
all four scenes in place instead.

## Bathroom Objects

The objects in the bathroom are separate sprites rather than part of the
background painting, so a spoken word can take one away:

| Sprite | Word it serves |
|---|---|
| `item_tuwalya.png` | **Tuwalya** (towel) |
| `item_sabon.png` | **Sabon** (soap) |
| `item_suklay.png` | **Suklay** (comb) |
| `item_sipilyo.png` | **Sipilyo** (toothbrush) |
| `item_baso.png` | Baso (cup) — scenery, the toothbrush stands in it |
| `item_tabo.png` | unused — the dipper left the word list |

They were cut out of the original painting by
[`Tools/cut_bathroom_items.py`](Tools/cut_bathroom_items.py), and the gaps
they left were repainted by
[`Tools/fill_bathroom_background.py`](Tools/fill_bathroom_background.py).
`bathroom_background.png` is now the emptied room; the original is kept as
`bathroom_background_with_items.png`. Putting every sprite back in its place
reproduces the original to a mean difference of 0.4 / 255, so the room looks
unchanged until something is picked up.

All four are spoken objectives in **Scene 3**, where the washing actually
happens: saying one takes that object off the shelf or wall, through
[`PickUpItem`](Assets/Scripts/PickUpItem.cs). Scene 2's job is only to get
Kylo into the bathroom and the light on, so it furnishes the room and picks
nothing up. The cup stays as scenery.

The dipper is the exception. `item_tabo.png` is no longer placed in either
scene: **Tabo** was dropped as a word, and the bath happens at the water jar
without the child having to name the thing it is poured with. That sprite
was also the only thing hiding the one hole `fill_bathroom_background.py`
could not close — the dipper straddles the wall and the curved lid, and the
per-row repair that works on a flat shelf does not work on an ellipse.
[`Tools/erase_tabo.py`](Tools/erase_tabo.py) finishes it: the tiles are
copied three tile-widths across so the grout lines up, and the lid is
rebuilt in ellipse coordinates from its own mirror image.

## Teacher Rachel's Portrait

Rachel is the voice of the game, and the branch ring on the narration plaque
is where the child sees her. The art in that ring is nine drawings of one
character, generated in Canva from a single approved front-facing portrait
which every later frame was drawn against, so that only her mouth moves.

| Sprite | Shape | Sound |
|---|---|---|
| `char_rachel_idle.png` | Closed, gentle smile | between lines |
| `char_rachel_ai.png` | Wide open | **a**, **i** |
| `char_rachel_e.png` | Spread, upper teeth showing | **e** |
| `char_rachel_o.png` | Round opening | **o** |
| `char_rachel_u.png` | Small and pursed | **u** |
| `char_rachel_mbp.png` | Lips shut, flat and neutral | **m**, **b**, **p** |
| `char_rachel_fv.png` | Upper teeth on the lower lip | **f**, **v** |
| `char_rachel_l.png` | Open, tongue tip raised | **l** |
| `char_rachel_blink.png` | Eyes closed, mouth unchanged | — |

They are a mouth set for lip-sync, and all seven shapes are now read per
sound. `Assets/Resources/VisemeTracks.json` holds 423 tracks, one per recorded
clip, and [`RachelPortraitAnimator`](Assets/Scripts/RachelPortraitAnimator.cs)
walks the track against `VoiceNarrator.PlaybackTime`.

The old three-shape cycle is kept as the fallback, and still earns its place: a
line with no recording has no track, so it gets the cycle and mouths its caption
instead of freezing under its own subtitle.

The raw generations are kept in `Art/char_rachel_*_raw.png` exactly as they
arrived. [`Tools/make_rachel_visemes.py`](Tools/make_rachel_visemes.py) turns
them into the sprites: it cuts the flat background away, seats her in the
ring, and then REGISTERS every frame onto the idle one by matching silhouette
area and centroid. That last step is the point of the script. Nine
separately generated images are nine slightly different drawings, and a head
that wanders two pixels between frames reads as a glitch rather than as
talking. The finished set overlaps the idle frame at 99.8-100%.

She is sized by HEIGHT, not width: crown just inside the top of the opening,
chest running off the bottom. Width tells you nothing about whether a circle
is filled; where her hair starts and whether her chest reaches the bottom
tell you everything.

### The ring is not a circle

Worth knowing, because it wasted a long time. The portrait had always been
drawn into the largest circle that fits inside the branch. But the opening
the twigs leave is 326 x 352, and its middle sits lower than that circle's -
so the portrait came out too small and about 25px too high, leaving a band of
bare opening under her chin. It looks exactly like badly cropped art, and no
amount of redrawing or zooming the portrait can fix it, because nothing is
drawn in that band at all. `UITheme`'s Ring constants now cover the opening,
measured by [`Tools/measure_narration_ring.py`](Tools/measure_narration_ring.py).
Whatever of her falls outside the opening is hidden by the twigs, which she
is drawn behind anyway.

[`RachelPortraitAnimator`](Assets/Scripts/RachelPortraitAnimator.cs) plays the
stand-in cycle off `VoiceNarrator.IsSpeaking` rather than a duration, so a
line cut short by the back button stops her mouth with it. There is no
Animator Controller: the HUD is built at run time, so there is no GameObject
for one to live on.

The frames are optional. With none loaded she is the still portrait, which is
why **Tools > SALINLAHI > Refresh UI Theme** reports how many it found rather
than treating them as missing art.

## Security Notes

- `Assets/Resources/SupabaseConfig.asset` is **git-ignored**; no keys are
  committed. A sample file documents the expected values instead.
- Only the **anon public** key is used. The `service_role` key is never placed
  in the game, as it bypasses all access rules.
- The anon key is public by design (it ships inside any client build). Access is
  restricted by **Row Level Security** policies that allow the anon role only to
  `insert` and `select` on `player_scores` — there are deliberately no `update`
  or `delete` policies, so the game can add and read records but never modify or
  erase them.
- The `leaderboard` table is **read-only** to the anon role. Writes go through
  the `submit-score` Edge Function, which holds the service key server-side and
  checks a per-player token, so a client cannot overwrite another player's row
  even though its key is public.

## Known Limitations / Unfinished Parts

As released in v0.1.0. The release notes carry the same list in tester's terms.

- **The Palengke (market) level is not built**, nor is a sixth Bahay scene,
  "Leaving for School" — its slot and its two narrator lines exist, the scene
  does not. Both are shown locked on the map.
- **20 of the 443 narrator lines are unrecorded**, and 15 of them are the lines
  heard most often: the three framing lines ("Sabihin", "Ngayon", "Ulitin"),
  the listening cue, two hints, the idle nudge, the mic-blocked line, and the
  correct / incorrect / not-heard replies. A missing clip is not an error —
  the caption is shown and the pause is held — so the game is playable but
  quieter than intended.
- **The opening prompt of a scene holds a long silent pause** before the child
  is allowed to speak.
- **The MINI GAME button does nothing.** It is drawn on the menu and
  deliberately inert.
- **Teacher Rachel's portrait is unassigned** (`UITheme.rachelPortrait`), so
  her captions appear without her picture.
- Word badges sit low on screen in Paaralan 2, Paaralan 5 and Parke 4.
- **Nothing works offline.** Recognition is a network service
  (`preferOfflineRecognition: false`) and the leaderboard is hosted.
- **Speech recognition only works on Android/iOS**; the Editor uses the
  keyboard fallback above, so pressing Play has never exercised the real
  mechanic. This is why releases are tested on a phone.
- Suspend and resume have not been tested on a phone at full speaker volume.
- `Maligo` is a water-and-suds overlay rather than an animation; the character
  art has only standing and walking poses.
- The leaderboard has no per-player filtering, paging or login.
- Testing and release share one Supabase project — there is no separate
  staging database, so a test play-through writes a real row.

## References

- [Supabase REST (PostgREST) documentation](https://supabase.com/docs/guides/api)
- [Supabase Row Level Security](https://supabase.com/docs/guides/database/postgres/row-level-security)
- [Unity `UnityWebRequest` documentation](https://docs.unity3d.com/ScriptReference/Networking.UnityWebRequest.html)
- [UnitySpeechToText plugin (yasirkula)](https://github.com/yasirkula/UnitySpeechToText)
