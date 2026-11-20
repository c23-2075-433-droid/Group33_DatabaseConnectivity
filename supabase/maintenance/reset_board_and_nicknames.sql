-- Empty the leaderboard and let every nickname go free again.
--
-- This deletes rows. It does not drop a table, a policy, an index or a grant,
-- so nothing here has to be re-run afterwards and no migration is undone - the
-- next child to finish a scene registers again as if the project were new.
--
-- Run it in the Supabase dashboard's SQL Editor, pasted from this file rather
-- than from whatever the editor opens on: the editor's template reports
-- success without having run any of this.
--
-- It is in four steps on purpose, because they are not the same decision.
-- Steps 1 to 3 are the reset. Step 4 is the one that throws away play records,
-- and it is the one to think about before running.
--
-- What this CANNOT reach: the tablets. Every device keeps its own profiles,
-- names, stars and words in a JSON file in the app's private storage, and
-- nothing in Postgres can clear that. A phone that already has a profile
-- called Kylo still has it after this runs, and will claim the name again the
-- next time it is asked for one. To start a device from nothing, delete its
-- profiles in CHANGE PROFILE, or reinstall the app.

-- ------------------------------------------------------------------ 1. tokens
--
-- First, and on its own, because of what it means. A token is the secret the
-- function issued when a player first registered; the device still holds its
-- copy. Removing the hash here does not break that device: with no owner row,
-- the next submission takes the function's registration branch, which issues a
-- fresh token and replaces the old one.
--
-- The cascade on leaderboard would do this anyway. It is written out because a
-- reset that silently invalidates every token is worth seeing in the script.
delete from public.leaderboard_owner;

-- ------------------------------------------------------------------- 2. board
delete from public.leaderboard;

-- --------------------------------------------------------------- 3. nicknames
--
-- Every name is free again after this, including the ones the claims migration
-- seeded from the rows below. If step 4 is skipped, the names in player_scores
-- stay taken in practice: the game falls back to looking that table up when a
-- claim cannot be filed, and re-running the claims migration would seed them
-- straight back in.
delete from public.nickname_claims;

-- ------------------------------------------- 4. the play records themselves
--
-- OPTIONAL, and the only destructive step. player_scores is one row per
-- completed session - the name, the score, the attempts, the level, the date -
-- and it is what the level-complete panel shows as recent scores. Deleting it
-- is what makes "zero nicknames" literally true.
--
-- Do not run this if those rows are evidence for a report. Activity 5's field
-- reference and Activity 10's timings were both taken against this table, and
-- a screenshot cannot be re-taken from an empty one.
--
-- Comment the next line out to keep them.
delete from public.player_scores;

-- ------------------------------------------------------------------ what is left
select 'leaderboard'      as table_name, count(*) as row_count from public.leaderboard
union all
select 'leaderboard_owner', count(*) from public.leaderboard_owner
union all
select 'nickname_claims',   count(*) from public.nickname_claims
union all
select 'player_scores',     count(*) from public.player_scores;
