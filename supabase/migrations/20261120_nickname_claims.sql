-- Nicknames are claimed when they are chosen, not when a level is finished.
--
-- The nickname check shipped in v0.1.1 asked player_scores whether anyone had
-- saved a score under the typed name. That is a question about the past, and
-- it was the wrong question in two ways:
--
--   * A name only reached player_scores once that child had finished a scene.
--     Two children who picked "Kylo" minutes apart were both told it was free,
--     because for those minutes it was: the name existed nowhere but in one
--     tablet's save file.
--
--   * Even with the rows there, a lookup's answer is stale the moment it is
--     sent. Two tablets can ask at the same time, both be told yes, and both
--     be right.
--
-- This table answers it the other way round. The game does not ask whether a
-- name is free; it inserts the name and lets Postgres decide. The unique index
-- on lower(nickname) means exactly one insert wins, and the loser is told so
-- by a 409 - which, to the game, is a check that worked.
--
-- Run it once in the Supabase dashboard's SQL Editor. It is safe to run again.
--
-- Paste it from this file rather than from anything the SQL Editor offers:
-- the editor opens on a template, and running the template reports success
-- without having run any of this.

-- -------------------------------------------------------------- the claims
--
-- Three columns and nothing else. A claim is a name and whoever filed it; the
-- game has no use for a fourth thing and the table is public to the anon key,
-- so every column it does not have is one that cannot be read off it.
--
-- device_id is an installation, not a child and not a profile. The name is
-- settled before the profile that will carry it exists, so there is no profile
-- id to file it under - and renaming away from a name and back again must not
-- be refused by the row this device put there itself. See
-- PlayerProfileStore.DeviceId, which is a random UUID in the save file and
-- means nothing to anything else.
create table if not exists public.nickname_claims (
  nickname   text        not null,
  device_id  uuid        not null,
  created_at timestamptz not null default now()
);

-- What makes the whole thing work. lower() because "kylo" and "Kylo" are the
-- same child as far as a six-year-old is concerned, and the game's own
-- matching has always ignored case (NicknameSuggester).
create unique index if not exists nickname_claims_name_key
  on public.nickname_claims (lower(nickname));

-- Bounds, not business logic: 16 is NicknameRules.MaxLength, which is also the
-- input field's characterLimit and the submit-score function's MAX_NICKNAME.
-- A row carrying anything else did not come from the game.
alter table public.nickname_claims
  drop constraint if exists nickname_claims_nickname_sane;
alter table public.nickname_claims
  add constraint nickname_claims_nickname_sane
  check (char_length(btrim(nickname)) between 1 and 16);

-- ------------------------------------------------------------ who may do what
alter table public.nickname_claims enable row level security;

drop policy if exists "anon can claim a nickname" on public.nickname_claims;
create policy "anon can claim a nickname"
  on public.nickname_claims for insert
  to anon
  with check (char_length(btrim(nickname)) between 1 and 16);

-- Read, because a 409 on its own does not say whether the name is this
-- device's already, and because the names are what a suggestion has to avoid.
drop policy if exists "anon can read claims" on public.nickname_claims;
create policy "anon can read claims"
  on public.nickname_claims for select
  to anon
  using (true);

-- No update and no delete, by not granting them and by there being no policy
-- for them. A claimed name therefore cannot be taken off the device that
-- claimed it, however the public key is used - which is the one property this
-- table exists to have.
revoke all on public.nickname_claims from anon;
grant select (nickname, device_id) on public.nickname_claims to anon;
grant insert (nickname, device_id) on public.nickname_claims to anon;

-- ------------------------------------------------- the names already in use
--
-- Without this, every name a child has already earned a score under would be
-- claimable by the next child to type it, and the fix would hand out exactly
-- the duplicate it was written to prevent.
--
-- The zero UUID is "no device": these names belong to nobody who can come
-- back and reclaim them. That is on purpose. A child whose profile already
-- carries one of them keeps it - the check runs when a name is chosen, not
-- when the game starts - but a reinstall would be offered Kylo01 rather than
-- Kylo, which is the honest price of not knowing which device earned which
-- row.
insert into public.nickname_claims (nickname, device_id)
select distinct on (lower(btrim(player_name)))
       btrim(player_name),
       '00000000-0000-0000-0000-000000000000'::uuid
  from public.player_scores
 where char_length(btrim(coalesce(player_name, ''))) between 1 and 16
 order by lower(btrim(player_name))
    on conflict do nothing;

insert into public.nickname_claims (nickname, device_id)
select distinct on (lower(btrim(nickname)))
       btrim(nickname),
       '00000000-0000-0000-0000-000000000000'::uuid
  from public.leaderboard
 where char_length(btrim(coalesce(nickname, ''))) between 1 and 16
 order by lower(btrim(nickname))
    on conflict do nothing;
