-- Hardening the player_scores table (Activity 9 - Software Security)
--
-- player_scores is the table from Activity 5, and it is still live: the
-- chapter-ending scenes save a row to it and the summary panel reads recent
-- rows back. Its access rules were written to demonstrate connectivity, and
-- they show it:
--
--   * "anon can insert scores" is `with check (true)` - the game's public key
--     may insert ANY row. score = -500, attempts = -1, a nickname of fifty
--     thousand characters: all accepted. Nothing in the database said what a
--     score is allowed to be.
--
--   * "anon can read scores" is `using (true)` with table-wide select, and
--     the game asked for `select=*`. That returned every column of every
--     child's row - including which level they played - to anyone holding a
--     key that ships inside the APK.
--
-- Neither is an injection or a stolen credential. They are the ordinary case:
-- rules written to let a feature work, never revisited to say what the feature
-- actually needs.
--
-- This migration says it. Run it once in the Supabase dashboard's SQL Editor;
-- it is safe to run again.

-- ------------------------------------------------- what a score may contain
--
-- These are bounds, not business logic. The game cannot produce a negative
-- score or a thousand-character level name, so a row that carries one did not
-- come from the game, and the database is the one place that can say no to it
-- however the row arrives.
--
-- NOT VALID is deliberate: it applies every constraint to new rows while
-- leaving the rows already saved alone. Those were written by the game and are
-- fine, but a migration that fails on live data is a migration nobody runs.
-- To check the existing rows too, run
--   alter table public.player_scores validate constraint <name>;
-- and read what it complains about.

alter table public.player_scores
  drop constraint if exists player_scores_score_sane;
alter table public.player_scores
  add constraint player_scores_score_sane
  check (score >= 0 and score <= 1000) not valid;

alter table public.player_scores
  drop constraint if exists player_scores_attempts_sane;
alter table public.player_scores
  add constraint player_scores_attempts_sane
  check (attempts >= 0 and attempts <= 1000) not valid;

-- 16 characters is the nickname input field's characterLimit, the
-- NicknameSuggester's MaxLength, PlayerProfileStore.MaxNicknameLength and the
-- submit-score function's MAX_NICKNAME. This is the fifth place that number
-- is written down and the only one a client cannot skip.
alter table public.player_scores
  drop constraint if exists player_scores_name_sane;
alter table public.player_scores
  add constraint player_scores_name_sane
  check (
    char_length(player_name) between 1 and 16
    -- No control characters: a newline or an escape sequence in a name that is
    -- printed onto a panel and into a log is not a name.
    and player_name ~ '^[^[:cntrl:]]+$'
  ) not valid;

alter table public.player_scores
  drop constraint if exists player_scores_level_sane;
alter table public.player_scores
  add constraint player_scores_level_sane
  check (char_length(level) between 1 and 120) not valid;

alter table public.player_scores
  drop constraint if exists player_scores_remarks_sane;
alter table public.player_scores
  add constraint player_scores_remarks_sane
  check (remarks is null or char_length(remarks) <= 120) not valid;

-- --------------------------------------------- only the columns it needs
--
-- Row Level Security decides which ROWS a role may touch. It has nothing to
-- say about columns, and `select=*` was reading all of them. Column
-- privileges are the part of Postgres that does say, so least privilege for
-- this table is written here.
--
-- The game, after this activity, asks for exactly these four columns on the
-- summary panel (SupabaseClient.FetchRecentScores) and for player_name alone
-- on the nickname check (FetchNamesLike). That is a choice the client makes;
-- the grant below is what makes it a rule. A build that went back to
-- `select=*` would now be refused by the database rather than quietly handed
-- every column again.

revoke all on public.player_scores from anon;

grant select (player_name, score, attempts, created_at)
  on public.player_scores to anon;

-- The five columns the game actually sends. player_id and created_at are left
-- out on purpose: they have database defaults, so a client that could write
-- them could choose its own id and backdate a row.
grant insert (player_name, score, attempts, level, remarks)
  on public.player_scores to anon;

-- No update and no delete, by not granting them and by there being no policy
-- for either. A saved result is a record of something that happened; the game
-- has no reason to be able to rewrite one.

-- ------------------------------------------------------------------ checking
--
-- What should happen after this runs, from the game's own key:
--
--   insert score = -500                  -> 400, constraint player_scores_score_sane
--   insert player_name of 200 characters -> 400, constraint player_scores_name_sane
--   select=*                             -> 403, permission denied for column player_id
--   select=player_name,score,attempts,created_at -> 200, the panel's own query
--   delete                               -> 403 / 0 rows, no policy and no grant
--
-- Part VI of Docs/Activity9_Security.md records these as run.
