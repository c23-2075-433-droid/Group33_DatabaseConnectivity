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
