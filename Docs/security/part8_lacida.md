---

## Part VIII. Individual Security Contribution

**Student Name:** Lacida, Kylo Bryan

**Group Number:** 33

**Security Area Reviewed:** Access control at the database and function layer,
and the supply chain — who is allowed to write what, and where the code in our
build comes from.

**Component / File / Configuration:**

- `supabase/migrations/20261112_player_scores_hardening.sql` (new)
- `supabase/functions/submit-score/index.ts`
- `Packages/manifest.json`, `Packages/packages-lock.json`
- `Docs/SupabaseSetup.md`
- The live access-control tests, T1 through T16

**Specific Contribution**

My responsibility from Activity 8 was integration and repository
configuration — making sure `main` holds what we claim it holds. The security
version of that question is who is allowed to change it, and that applies to
the database as much as the repository.

- **Testing the access rules against the live project instead of reading them.**
  Sixteen tests, every one chosen so that a pass writes nothing and a failure
  writes nothing: the refusals are refused before any write, the update test
  sets a column to the value it already held, and the delete test targets an id
  that does not exist. The board's one row was re-read afterwards and was
  untouched. The leaderboard's own design held up — token hashes unreadable,
  direct writes refused by Row Level Security, a wrong token refused with a 403.
- **The lesson in T4.** A direct `PATCH` of an existing board row answered
  `HTTP 200`, not `403`. Row Level Security does not refuse an update it has no
  policy for; it makes the row invisible to the update, so the statement
  succeeds at changing nothing and PostgREST returns an empty array. A test
  that only checked the status code would have logged that as a breach. The
  proof is the empty body and the unchanged row.
- **`player_scores` was never narrowed after Activity 5.** Its insert policy is
  `with check (true)` — the public key may insert any row, including
  `score = -500` — and its select policy is table-wide, which `select=*` was
  using: T16 returned all seven columns of every child's run, name, score,
  attempts, level and remarks, to anyone holding a key that ships inside the
  APK. Not a secret leaked, but more about more children than the feature ever
  needed.
- **The migration, and why `NOT VALID`.** Bounds on everything the game writes,
  then `revoke all ... from anon` and column grants: four columns on select,
  five on insert, `player_id` and `created_at` left out so a client cannot
  choose its own id or backdate a row. The constraints are added `NOT VALID` so
  they apply to every new row while leaving rows already saved alone — a
  migration that fails on live data is a migration nobody runs. Row Level
  Security decides rows; column privileges are the part of Postgres that
  decides columns, and that was the part `select=*` was walking through.
- **The dependency that was not pinned.** `com.yasirkula.speechtotext` was a
  bare git URL, so every clone and every rebuild resolved to whatever upstream
  HEAD was — on three machines, across months, with an upstream force-push able
  to change our build with no commit here to show it. It is pinned to
  `27a99b18`, the revision `packages-lock.json` showed we were already using, in
  both files. The version does not change: it is the same commit, now named.
- **The one I did not pin.** The Edge Function imports
  `npm:@supabase/supabase-js@2`, a floating major resolved at deploy time. The
  right fix is an exact version, and the right order is to read the resolved
  version from the deploy output first — guessing a version and redeploying
  would risk the only writer the leaderboard has, which is what the activity
  warns about. Recorded as a planned control rather than claimed as a done one.
- **Documenting the deployment steps so they are repeatable.** The function
  secret, the migration and three new troubleshooting rows in
  `Docs/SupabaseSetup.md`, including the one that will catch somebody out:
  if the project's publishable key is ever rotated, the function's secret has
  to be updated in the same sitting or every submission is refused with a 401.

**Security Improvement or Test Performed**

Improvements 4, 5 and 6 in Part V, and the live access-control and
authentication tests T1–T16 in Part VI.

**Repository / Changeset / Commit Evidence**

| Item | Reference |
|---|---|
| Implementation commit | `46702b1` "Treat the save file, the key and the package as untrusted" on `claude/capstone-security-review-f3aa1d` |
| Database hardening | `supabase/migrations/20261112_player_scores_hardening.sql` |
| Function gate and logging | `supabase/functions/submit-score/index.ts` — `keyAccepted()`, `refuse()` |
| Dependency pinned | `Packages/manifest.json`, `Packages/packages-lock.json` — `#27a99b18ba3d10477b26b91c6c3e8f2b0c391150` |
| Deployment steps | `Docs/SupabaseSetup.md` sections 2 and 2b |
| Prior ownership of this area | Integration verification and repository configuration for v0.1.0, including `.gitattributes` and the merge configuration, per `Docs/RELEASE_PROCESS.md` |

### What security consideration will I personally check while developing succeeding features?

What the feature is allowed to write, and whether anything but our own code is
enforcing that. The pattern in both of this review's real access-control
findings was the same: a rule written so a feature would work, with `true` where
a condition belonged, and never revisited. So for every table, policy or
function I add after this, I will write down the narrowest grant that makes the
feature work before I write the feature, and then try the operation I expect to
be refused — because a policy I have only read is a policy I am guessing about.
And I will pin anything we pull from outside this repository to a revision, not
a branch.
