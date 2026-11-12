-- SALINLAHI global leaderboard
--
-- One row per player, and nothing else. What a child is learning - which
-- words they found hard, how many tries each took, which scenes they have
-- played - never leaves the device: it lives in the local save file that
-- PlayerProfileStore writes. Four columns go up, and only so that one board
-- can show every player.
--
-- Run this once, in the Supabase dashboard's SQL Editor. It is safe to run
-- again: everything is "if not exists" or "or replace".

-- ---------------------------------------------------------------- the board

create table if not exists public.leaderboard (
  player_id    uuid        primary key,
  nickname     text        not null,
  total_points integer     not null default 0,
  total_stars  integer     not null default 0,
  updated_at   timestamptz not null default now(),
  created_at   timestamptz not null default now()
);

-- player_id is the primary key, so a player can only ever have one row. That
-- is what stops a nickname change from leaving a duplicate behind: a rename
-- is an update to this row, not a new one.

create index if not exists leaderboard_order_idx
  on public.leaderboard (total_points desc, total_stars desc, updated_at asc);

-- ------------------------------------------------ who is allowed to write it
--
-- A player_id is made on the device and is not a secret: anyone who learned
-- another child's id could otherwise post scores as them. So the first
-- submission registers the player and the Edge Function hands back a token,
-- which the game keeps and presents every time after that. Only its hash is
-- stored, and in a table of its own so that it is not one careless
-- "select *" away from being public.

create table if not exists public.leaderboard_owner (
  player_id  uuid        primary key
               references public.leaderboard(player_id) on delete cascade,
  token_hash text        not null,
  created_at timestamptz not null default now()
);

-- -------------------------------------------------------- Row Level Security

alter table public.leaderboard       enable row level security;
alter table public.leaderboard_owner enable row level security;

-- The game may read the board, and that is all it may do to it.
drop policy if exists "anon can read the leaderboard" on public.leaderboard;
create policy "anon can read the leaderboard"
  on public.leaderboard for select
  to anon, authenticated
  using (true);

-- There is deliberately NO insert, update or delete policy. The only writer
-- is the submit-score Edge Function, which connects with the service role and
-- so is not subject to these policies at all. A score the client simply
-- asserts never reaches this table.

-- leaderboard_owner has no policy of any kind, so with RLS on, the anon and
-- authenticated roles cannot read or write a single row of it. The grants
-- below say the same thing a second way, in case RLS is ever turned off by
-- accident.
revoke all on public.leaderboard_owner from anon, authenticated;

-- ------------------------------------------------------------------ ranking
--
-- A player outside the page the board shows still wants to know where they
-- are. Counting that in SQL is one round trip; doing it in the game would
-- mean pulling every row down to the phone.

create or replace function public.leaderboard_rank(p_player_id uuid)
returns table (rank integer, total_points integer, total_stars integer)
language sql
stable
security definer
set search_path = public
as $$
  select (select count(*)::integer + 1
            from public.leaderboard ahead
           where (ahead.total_points, ahead.total_stars)
               > (me.total_points, me.total_stars)) as rank,
         me.total_points,
         me.total_stars
    from public.leaderboard me
   where me.player_id = p_player_id;
$$;

grant execute on function public.leaderboard_rank(uuid) to anon, authenticated;
