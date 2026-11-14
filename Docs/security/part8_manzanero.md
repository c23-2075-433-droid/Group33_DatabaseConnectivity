---

## Part VIII. Individual Security Contribution

**Student Name:** Manzanero, Kyla Samantha

**Group Number:** 33

**Security Area Reviewed:** Input validation and what the player-facing layer
discloses — everything a child types or the game shows back, and the save file
the player layer reads it from.

**Component / File / Configuration:**

- `Assets/Scripts/NicknameRules.cs` (new)
- `Assets/Scripts/PlayerProfileStore.cs`
- `Assets/Scripts/PlayerProfile.cs`
- `Assets/Scripts/SessionScoreTracker.cs`
- `Assets/Scripts/SupabaseClient.cs`
- `Tools/test_security_rules.sh`, `Tools/securitytests/SecurityRuleTests.cs` (new)

**Specific Contribution**

This is the part of SALINLAHI I built, which is why it is the part I reviewed.
The nickname dialog, the nickname suggester, the profile store behind them and
the chapter-end summary panel are mine from earlier activities, and every
finding below is in my own code.

- **Treating the save file as input.** `players.json` is ordinary JSON in the
  app's storage, and my profile store was reading it as though the game had
  just written it: a player id was accepted whatever shape it was, a nickname
  whatever length, and `totalPoints: -999` was read back and submitted to the
  leaderboard as fact. Ids are now checked against the UUID shape the game
  actually writes and replaced if they do not match, nicknames go through the
  same cleaning as one just typed, and totals and star ratings are clamped on
  load and at the setter.
- **Finding that the validation was only at the far end.** The Edge Function
  has stripped control characters, collapsed whitespace and capped nicknames at
  16 since the leaderboard was built. My client did none of it — the input
  field's `characterLimit` hid the gap in ordinary play, and a name read from a
  file never passes through an input field. The consequence was quiet: the
  device kept one name and the board showed a shorter one, with nothing saying
  so.
- **`NicknameRules`, and why it has no `UnityEngine` in it.** I put the three
  rules in one file deliberately free of Unity types, so the real shipped rules
  can be compiled and asserted against without opening the editor. A validation
  rule nobody can run a test on tends to stop being true.
- **Nineteen negative tests.** `Tools/test_security_rules.sh` compiles
  `NicknameRules.cs` itself — not a copy of it — and tries a 500-character
  name, a newline inside a name, a NUL byte, an ANSI escape sequence, a name of
  nothing but control characters, a name cut mid-word at the limit, and seven
  shapes of player id including `x","p_player_id":"y` and `' or 1=1 --`. All
  nineteen pass, and they run in about two seconds on every release.
- **The summary panel was printing the server's reply to a six-year-old.** My
  chapter-end panel did `SetText(recordsText, "Could not save result.\n" +
  error)`, and `error` was the status code, the transport error and Supabase's
  whole response body — a key hint on a 401, a column and a constraint name on
  a database error. It now says the result is safe on this device, which is
  true, because the run was written locally before the network was involved.
  The detail goes to the log, where a developer can read it.
- **Least privilege in the panel's own query.** The recent-scores list shows a
  name, a score out of attempts and a date. It was asking for `select=*`, which
  returned seven columns of every child's run, including which level they
  played. It asks for four now — and I raised it with the group rather than
  stopping there, which is what led to the column grants in the migration, so
  the database refuses the wide query rather than relying on my code to keep
  asking nicely.

**Security Improvement or Test Performed**

Improvements 2 and 3 in Part V, the client half of improvement 5, and the nine
input-validation and disclosure tests in Part VI — T16b, and T17 through T24,
which are nineteen assertions in the runner.

**Repository / Changeset / Commit Evidence**

| Item | Reference |
|---|---|
| Implementation commit | `46702b1` "Treat the save file, the key and the package as untrusted" on `claude/capstone-security-review-f3aa1d` |
| Validation rules | `Assets/Scripts/NicknameRules.cs` |
| Save file treated as input | `Assets/Scripts/PlayerProfileStore.cs` — `Repair()` and `Clean()` |
| Disclosure fixed | `Assets/Scripts/SessionScoreTracker.cs`, `Assets/Scripts/SupabaseClient.cs` — `Log()` |
| Negative tests | `Tools/test_security_rules.sh` — 19 passed, 0 failed |
| Prior ownership of this area | `523a602` "Offer a free nickname when the one typed is taken", `a514bd0` "Record which words were practised, not just how many", `639908b` "Rebuild the level summary from the group's UI kit" — all in v0.1.0 |

### What security consideration will I personally check while developing succeeding features?

Where the data came from, before I decide whether to trust it. The save file
taught me that: I wrote it, so I read it back as though it were still mine, and
it is not — it is a file on a device I do not control. For every feature after
this one, the almanac, the mini games, the teacher's view if we build it, I will
ask the same three questions in the same order. What boundary is this value
crossing? Is it checked on the far side of that boundary as well as here? And
if it is wrong, what does the child see — because a status code on a panel in a
Grade 2 classroom is my bug, not the server's.
