# Supabase Setup (Activity 5 — Database Connectivity)

One-time setup for the SALINLAHI prototype's database connectivity. Do steps
1–2 once for the group; every member does step 3 locally.

## 1. Create the project

1. Sign up / log in at <https://supabase.com> and create a new project.
2. Note the **Project URL** and the **anon public** key from
   **Project Settings → API**. You'll need both in step 3.

> The anon key is a *public* key — it ships inside the game and can always be
> read by a determined user. That is expected. The Row Level Security policies
> in step 2 are what actually protect the data, by limiting the anon role to
> inserting and reading this one table and nothing else.

## 2. Create the table and access rules

In the Supabase dashboard, open **SQL Editor** and run:

```sql
-- Table that stores one row per completed play session.
create table player_scores (
  player_id   uuid        primary key default gen_random_uuid(),
  player_name text        not null,
  score       integer     not null default 0,
  attempts    integer     not null default 0,
  level       text        not null,
  remarks     text,
  created_at  timestamptz not null default now()
);

-- Row Level Security: nothing is readable or writable until a policy says so.
alter table player_scores enable row level security;

-- The game (anon role) may add new records...
create policy "anon can insert scores"
  on player_scores for insert
  to anon
  with check (true);

-- ...and read them back to show recent scores in-game.
create policy "anon can read scores"
  on player_scores for select
  to anon
  using (true);
```

Note there is deliberately **no update or delete policy** — the game can only
add and read records, never modify or erase existing ones.

> **Superseded by the Activity 9 security review.** The two policies above are
> what the Activity 5 demonstration needed, and they are wider than the feature
> is: `with check (true)` lets the game's public key insert *any* row — a score
> of −500, a nickname of fifty thousand characters — and the table-wide select
> returned every column of every child's row. Run
> `supabase/migrations/20261112_player_scores_hardening.sql` after the SQL
> above. It adds bounds on every column the game writes and narrows `anon` to
> the four columns the summary panel actually shows. See Part V of
> `Docs/Activity9_Security.md`.

### Field reference (for Part III of the report)

| Field Name | Data Type | Description |
|---|---|---|
| `player_id` | uuid (auto) | Unique identifier for the record |
| `player_name` | text | Nickname entered by the player |
| `score` | integer | Number of words pronounced correctly |
| `attempts` | integer | Total voice attempts made in the session |
| `level` | text | Which level/scene was played |
| `remarks` | text | Human-readable result, e.g. "3/5 words correct" |
| `created_at` | timestamptz (auto) | When the record was saved |

Unique identifier: `player_id`. Required before saving: `player_name`, `level`
(the rest have database defaults).

## 2b. The global leaderboard

The leaderboard is the one thing SALINLAHI still keeps in Supabase. Everything
a child learns — which words they practised, how many tries each took, which
scenes they have played, how many stars each earned — lives in a JSON save file
on the device (`PlayerProfileStore`, written to `Application.persistentDataPath`,
which on Android is inside the app's own private storage). Four values go up,
and only so that one board can show every player:

| Column | Type | Description |
|---|---|---|
| `player_id` | uuid | The player's own id, made once on the device |
| `nickname` | text | Display name. Changeable, and never the identity |
| `total_points` | integer | Points accumulated across every run |
| `total_stars` | integer | Stars accumulated across every scene |
| `updated_at` | timestamptz | Last score update |

### Run the SQL

In **SQL Editor**, run `supabase/migrations/20261112_leaderboard.sql` from this
repository. It is safe to run twice. It creates:

* `leaderboard` — one row per player. `player_id` is the primary key, which is
  what makes a duplicate record impossible: a nickname change is an *update* to
  that row, never a second one.
* `leaderboard_owner` — the hash of each player's submission token, in a table
  of its own so it is not one careless `select *` away from being public.
* `leaderboard_rank(p_player_id uuid)` — counts how many players are ahead of
  one player, so someone outside the top of the board can still be shown their
  place without the phone downloading every row.

Row Level Security:

* `leaderboard` has a **select** policy for `anon` and nothing else. There is
  deliberately no insert, update or delete policy, so the game's key cannot
  change the board however it is used.
* `leaderboard_owner` has **no policy at all**, and RLS is on — so `anon`
  cannot read or write a single row of it.
* The only writer is the Edge Function below, which connects with the service
  role and is therefore not subject to these policies.

### Deploy the Edge Function

`supabase/functions/submit-score/index.ts` is what validates and stores a
submission. No CLI needed — the dashboard can do it:

1. **Edge Functions** in the left sidebar.
2. **Deploy a new function** → **Via Editor**.
3. Name it **`submit-score`**. The name has to match, because it is what the
   game's `SupabaseConfig.submitScoreFunction` points at.
4. Replace the editor's contents with the whole of `index.ts` from this
   repository. **Select all first** — the editor opens on a Hello World
   template, and pasting without clearing it leaves the template in charge.
5. **Deploy function** at the bottom. It takes 10–30 seconds.

To change it afterwards, the **Code** tab on the function's page is the same
editor, and the button there says **Deploy updates**.

It needs nothing configured afterwards. `SUPABASE_URL` and the key that may
write are both set by the Edge runtime on their own, and the function reads
whichever key scheme the project is on — `SUPABASE_SERVICE_ROLE_KEY` on older
projects, the `default` entry of `SUPABASE_SECRET_KEYS` on newer ones. **The
service key is never in Unity, never in the repository and never in a build.**

**Turn JWT verification off.** On the function's page, open **Settings** and
switch off **Verify JWT with legacy secret**, then **Save changes**.

That setting requires a JWT signed by the project's *legacy* secret in the
`Authorization` header. A legacy `anon` key is one and satisfies it; an
`sb_publishable_…` key is not a JWT at all, so a project on the newer key
scheme gets `401` on every call with this left on. SupabaseClient only sends
an `Authorization` header when the key is a JWT, for the PostgREST reason
documented there, so with a publishable key it sends none.

Turning it off costs nothing here, and it is what the dashboard itself
recommends for a function that does its own checking — which this one does.
The platform's check was never what protected a row; the token described
below is. (With the CLI the same thing is `verify_jwt = false` under
`[functions.submit-score]` in `supabase/config.toml`, or
`supabase functions deploy submit-score --no-verify-jwt`.)

**Set the publishable-key secret.** With JWT verification off, the platform
checks nothing about who is calling, and this is a public URL: a request with
no key at all reaches the function (Activity 9 test T15). The token design
holds under that — a stranger cannot change a row they have no token for — but
*registering* a new player needs no token by definition, so without a gate a
stranger can put rows on a board that children read.

So the function checks the game's own key itself. On the function's page open
**Secrets** (Edge Functions → Secrets) and add:

| Name | Value |
|---|---|
| `SALINLAHI_PUBLISHABLE_KEY` | the same `sb_publishable_…` key the game's config asset holds |

This is a gate, not authentication: the key ships inside the APK and anyone who
unpacks one can read it. What it stops is traffic that never had the app.

While the secret is unset the gate stays open and writes `{"event":"gate-open"}`
to the function log on every call, so deploying the code cannot lock the game
out of its own leaderboard. Setting the secret switches the check on. **If the
project's publishable key is ever rotated, this secret has to be updated in the
same sitting**, or every submission is refused with `401`.

To check it is live without putting a row on the board, use **Test** on the
function's page with method `POST` and a deliberately bad id:

```json
{"player_id":"not-a-uuid","nickname":"test","total_points":0,"total_stars":0,"token":""}
```

`{"ok":false,"error":"player_id must be a UUID."}` is the right answer. It
proves the function is up, that it is running this code rather than a
template, and that the platform let the call through — and it writes nothing.

Two other replies mean something specific:

* `{"message":"Hello undefined!"}` with a `200` — the Hello World template is
  still deployed. The paste did not replace it. Go to the **Code** tab, select
  all, paste again, **Deploy updates**.
* `401` — the JWT setting above is still on. Note that the dashboard's own
  **Test** panel supplies authorization for you, so it can return `200` on a
  function the game would still be refused by; the game is the real test.

### How a player is identified, and what that is worth

There is no account and no password in SALINLAHI — children of this age should
not be asked for one — so **the locally generated UUID is not authentication**
and is not treated as one. It is an identifier, and identifiers are not secret.

So the first submission for a player id *registers* it, and the function hands
back a token. The game keeps that token in the local save file and presents it
on every submission afterwards; the server stores only a SHA-256 of it. Knowing
another child's `player_id` therefore buys nothing: without the token, the
server refuses to change that row.

What the server still cannot do is verify a score. Nothing in the cloud hears a
child say "paa" into a microphone. What it can do is refuse the absurd, and it
does:

* `total_stars` cannot exceed what the game has stars to give.
* `total_points` may only go up, and by at most 1000 in one submission —
  roughly what a long offline stretch of play can honestly earn. A forged
  submission can inch a score up; it cannot jump it to a million.
* The same row cannot be rewritten more than once every five seconds.
* Nicknames are stripped of control characters and capped at 16 characters,
  the same limit the game's input field uses.

### Offline

Being offline is an ordinary state here, not an error: these are classroom
tablets. Play never waits on the network, and nothing is lost. The profile
records what the run earned, notes that it is ahead of what the server has been
told, and `LeaderboardClient.PushIfNeeded()` tries again the next time the main
menu opens with a connection. Opening the board with no connection says so in a
line a child can read.

## 3. Add your local credentials (each member, once)

Credentials are **not** in the repository — `Assets/Resources/SupabaseConfig.asset`
is git-ignored on purpose.

1. In Unity, run **Tools → SALINLAHI → Create Supabase Config**.
2. Select the created asset at `Assets/Resources/SupabaseConfig.asset`.
3. Paste the **Project URL** and **anon key** from step 1 into the Inspector.

That's it — `SupabaseClient` reads from that asset at runtime.

## Troubleshooting

| Symptom | Likely cause |
|---|---|
| `401` / "Invalid API key" | Wrong or missing anon key in the config asset |
| `404` | Project URL wrong, or the table name doesn't match `player_scores` |
| Save returns `403` / empty error | RLS is on but the insert policy from step 2 wasn't created |
| Fetch returns `[]` | No rows saved yet — play a level through to the end first |
| Works in Editor, fails on Android | No internet on the device, or cleartext/HTTPS issue — Supabase is HTTPS so this is normally just connectivity |
| Leaderboard says "not set up on this device" | No `SupabaseConfig.asset`, or its URL/key are blank |
| Leaderboard opens empty | Nobody has finished a level yet, or the SQL in 2b has not been run |
| Submissions return `401` | Either the platform's JWT check rejected the key — turn JWT verification off for `submit-score` (see 2b) — or the function's own gate did: `SALINLAHI_PUBLISHABLE_KEY` is set to a key the game is no longer sending |
| Saves return `400` with a constraint name | Working as intended: the hardening migration refused a value outside what the game can produce. The constraint name says which |
| Saves or the summary panel return `403 permission denied for column` | A query asked for a column `anon` was not granted after the hardening migration. The panel's query is `select=player_name,score,attempts,created_at` |
| Submissions return `404` | The function is not deployed, or its name is not `submit-score` |
| Submissions return `403` | The device's token does not own that `player_id`. Expected if the save file was copied between devices |
| Submissions return `429` | Two submissions less than five seconds apart. Harmless; the next one lands |
| Scores never reach the board | Open the main menu while online — that is when a pending submission is retried |
