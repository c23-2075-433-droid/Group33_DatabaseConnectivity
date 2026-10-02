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

Level 1 ("Bahay — Umaga na!") asks the player to say:

1. **Bangon** — get up
2. **Tayo** — stand up
3. **Lakad** — walk

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

1. **Tools → SALINLAHI → Add Exit Trigger To Level 1**
2. **Tools → SALINLAHI → Build Bahay Scene 2**
3. **Tools → SALINLAHI → Link Scene 1 To Scene 2**

Step 3 joins the two scenes with a fade and puts the score and
recent-scores panel at the end of Scene 2, which is where the playable
content currently stops.

### 5. Play
Open `Assets/Scenes/MainMenu.unity` and press Play.

On a **desktop/Editor** run, the device speech recognizer is unavailable, so
keyboard shortcuts simulate recognized speech through the exact same code path:

| Key | Simulates |
|---|---|
| `Enter` | **whatever word the scene is currently asking for** |
| `B` / `T` / `L` | "Bangon" / "Tayo" / "Lakad" (Scene 1) |
| `K` / `N` / `O` / `I` | "Kaliwa" / "Kanan" / "Bukas" / "Ilaw" (Scene 2) |
| `X` | a wrong answer (counts an attempt, does not advance) |
| `J` | "Talon" (jump) |

**Enter is the one to use** — it reads the current objective and says that
word, so it works in any scene without needing a key per word. Press it
repeatedly to walk through a whole scene.

Scene 1 ends by walking out of the bedroom, which fades into Scene 2. After
Scene 2's five words, the completion panel shows your score and the recent
records read back from Supabase.

On **Android**, the real microphone is used instead — say the words out loud.

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

- Only **Scene 1 of Level 1** (the wake-up sequence) is playable. The remaining
  Bahay scenes (bathroom, bath, dressing, breakfast, leaving) are designed but
  not yet built, and Levels 2 (Paaralan) and 3 (Parke) are locked on the map.
- Reaching the exit shows a **placeholder completion panel**; it does not load a
  next scene, because no next scene exists yet.
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
