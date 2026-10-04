# SALINLAHI — Database Connectivity Prototype

**Group 33 — Activity 5: Database Connectivity**

A voice-interactive Filipino vocabulary learning game for children, extended
with database connectivity so that each play session is saved to, retrieved
from, and displayed back from an online database.

## Group Members

| Name | Role |
|---|---|
| Daquis, Jhesza Mhei G. | Game Logic Developer |
| Lacida, Kylo Bryan | UI and Testing Lead |
| Manzanero, Kyla Samantha | Documentation Lead |


BSIT Capstone Project — University of Perpetual Help System Laguna

## Prototype Direction

**Capstone-Based Prototype (Option 1).** The prototype is built on our defended
capstone title, SALINLAHI, rather than a separate guided game. The game already
produces meaningful learning data — which Filipino words a child pronounced
correctly — so adding database connectivity was a natural extension rather than
an artificial add-on.

## Short Description

The player guides a child character, Kylo, through her morning routine by
**speaking Filipino words out loud**. The game listens through the device
microphone, checks the pronunciation, and advances the story when the word is
said correctly.

Level 1 ("Bahay — Umaga na!") asks the player to say, across two scenes:

1. **Bangon** — get up
2. **Tayo** — stand up
3. **Lakad** — walk
4. **Kaliwa** / **Kanan** — left / right
5. **Bukas** — open (the bathroom door)
6. **Ilaw** — turn the light on
7. **Tabo**, **Sabon**, **Sipilyo**, **Tuwalya** — dipper, soap, toothbrush, towel
8. **Maligo**, **Suklay** — take a bath, comb

When the sequence is finished, the session result is saved to the database, and
the most recent scores are retrieved and displayed on the completion panel.

## Tools and Technologies

| Item | Used |
|---|---|
| Game engine | Unity 6 (6000.6.2f1), 2D URP |
| Language | C# |
| Speech recognition | UnitySpeechToText (yasirkula) — device speech recognizer, `fil-PH` |
| Database | Supabase (PostgreSQL) via its REST API |
| HTTP client | `UnityWebRequest` (no third-party SDK) |
| Editor | Visual Studio Code / Visual Studio |
| Repository | GitHub |

## Database: What Is Saved and Retrieved

Records go to a single Supabase table, `player_scores`:

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

## How to Run the Prototype

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

### 4. Build and wire the scenes
In Unity, run in order:

Only Scene 1, Scene 2, the main menu and the map are committed as `.unity`
files. Scenes 3 and 4 are **built from their scripts**, so a fresh clone has to
run these once, in order:

1. **Tools → SALINLAHI → Add Exit Trigger To Level 1**
2. **Tools → SALINLAHI → Add Run Score Counter To Level 1**
3. **Tools → SALINLAHI → Build Bahay Scene 4**
4. **Tools → SALINLAHI → Build Bahay Scene 3**
5. **Tools → SALINLAHI → Build Bahay Scene 2**
6. **Tools → SALINLAHI → Link Scene 1 To Scene 2**
7. **Tools → SALINLAHI → Build Level Select Scene**

Build the scenes back to front (4, then 3, then 2). Each one registers itself in
Build Settings, and the scene before it fades into it by name — build them the
other way round and the earlier scene has nowhere to go.

Level 1 runs Scene 1 → 2 → 3 → 4. The scene that **ends** a level owns the
database demo and shows the result; every other scene carries its own
`RunScoreCounter`. Keep that rule when adding Scene 5, or its words will vanish
from the score with nothing in the log to say so.

### 5. Play
Open `Assets/Scenes/MainMenu.unity` and press Play.

On a **desktop/Editor** run, the device speech recognizer is unavailable, so
keyboard shortcuts simulate recognized speech through the exact same code path:

| Key | Simulates |
|---|---|
| `Enter` | **whatever word the scene is currently asking for** |
| `B` / `T` / `L` | "Bangon" / "Tayo" / "Lakad" (Scene 1) |
| `K` / `N` / `O` / `I` | "Kaliwa" / "Kanan" / "Bukas" / "Ilaw" (Scene 2) |
| `A` / `S` / `P` / `W` | "Tabo" / "Sabon" / "Sipilyo" / "Tuwalya" (Scene 2) |
| `M` / `C` | "Maligo" / "Suklay" (Scene 3) |
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
| `item_tabo.png` | **Tabo** (dipper) |
| `item_sabon.png` | **Sabon** (soap) |
| `item_sipilyo.png` | **Sipilyo** (toothbrush) |
| `item_tuwalya.png` | **Tuwalya** (towel) |
| `item_suklay.png`, `item_baso.png` | Suklay (comb), Baso (cup) — for later scenes |

They were cut out of the original painting by
[`Tools/cut_bathroom_items.py`](Tools/cut_bathroom_items.py), and the gaps
they left were repainted by
[`Tools/fill_bathroom_background.py`](Tools/fill_bathroom_background.py).
`bathroom_background.png` is now the emptied room; the original is kept as
`bathroom_background_with_items.png`. Putting every sprite back in its place
reproduces the original to a mean difference of 0.4 / 255, so the room looks
unchanged until something is picked up.

Tabo, Sabon, Sipilyo and Tuwalya are spoken objectives in Scene 2, after the
light is on: saying one takes that object off the shelf or wall, through
[`PickUpItem`](Assets/Scripts/PickUpItem.cs). They have no voice-over yet, so
the on-screen prompt carries them. Suklay and Baso stay as scenery — combing
belongs with getting dressed in Scene 4.

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

## Known Limitations / Unfinished Parts

- The **first three scenes of Level 1** are playable: the wake-up sequence, the
  walk to the bathroom, and the bath. The remaining Bahay scenes (dressing,
  breakfast, leaving) are designed but not yet built, and Paaralan, Parke and
  Palengke are locked on the map.
- Scene 3's **Maligo** is a water-and-suds overlay rather than an animation;
  the character art has only standing and walking poses.
- The result is saved once, at the end of the last scene, and covers the
  whole level: [`RunScoreCounter`](Assets/Scripts/RunScoreCounter.cs) adds
  each scene's attempts to [`RunScore`](Assets/Scripts/RunScore.cs), and
  [`SessionScoreTracker`](Assets/Scripts/SessionScoreTracker.cs) saves that
  total. Add a counter to any new scene or its words go uncounted.
- **No voice-over audio yet.** The instruction-clip slots are wired and the full
  recording script exists at [`Docs/VoiceOverScript.csv`](Docs/VoiceOverScript.csv),
  but the audio files are not recorded, so the replay button logs a warning
  instead of playing a clip.
- Speech recognition only works on **Android/iOS**; the Editor uses the keyboard
  fallback above.
- Retrieved records are shown as plain text, not a formatted leaderboard.
- Scores are global — there is no per-player filtering or login.

## References

- [Supabase REST (PostgREST) documentation](https://supabase.com/docs/guides/api)
- [Supabase Row Level Security](https://supabase.com/docs/guides/database/postgres/row-level-security)
- [Unity `UnityWebRequest` documentation](https://docs.unity3d.com/ScriptReference/Networking.UnityWebRequest.html)
- [UnitySpeechToText plugin (yasirkula)](https://github.com/yasirkula/UnitySpeechToText)
