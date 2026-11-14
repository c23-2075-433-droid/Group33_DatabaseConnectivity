---

## Part VIII. Individual Security Contribution

**Student Name:** Daquis, Jhesza Mhei G.

**Group Number:** 33

**Security Area Reviewed:** Secrets, release configuration, and the security
gate in the release process — what the group ships, and what must not be inside
it.

**Component / File / Configuration:**

- `Tools/secret_scan.sh`, `Tools/githooks/pre-commit` (new)
- `.gitignore`
- `ProjectSettings/ProjectSettings.asset`
- `Docs/RELEASE_PROCESS.md`
- Part VII of this report

**Specific Contribution**

My release responsibility from Activity 8 was the build and the package, so the
question I took was the one a release has to answer: is there anything in this
repository that should not leave the group's machines?

- **The finding that mattered most in this review.** A real ElevenLabs API key
  was committed on 27 September 2026 in `0250197`, 52 bytes beginning `sk_`, and
  removed from tracking two commits later in `2094726`. Removing a file from
  tracking does not remove it from history: `git branch -r --contains 0250197`
  lists `origin/main` and fourteen other remote branches, so the key is in every
  clone. `.gitignore` was never going to help, because the ignore rule was
  written after the file already existed. **This is escalated rather than
  fixed: the key has to be rotated at ElevenLabs,** and no change in this
  repository substitutes for that.
- **A control, so the next one does not get in.** `Tools/secret_scan.sh` looks
  for the credential shapes this project actually handles — ElevenLabs `sk_`
  keys, Supabase `sb_secret_` keys, JWTs, Google keys, PEM private keys — and
  for filenames that are credentials whatever is in them. It never prints a
  matched line, because the point is to say where to look, not to copy a secret
  into a terminal or a screenshot. `Tools/githooks/pre-commit` runs it on every
  commit; it ran on the commit that introduced it.
- **I tested the control three ways** rather than trusting it: clean on the
  repository as it stands, refusing a planted `sk_` file with a named path and
  exit 1, and run across all 400-plus commits, where it found the real leak
  without being told where it was (T25, T26, T27).
- **Verifying what the build is configured to do.** `insecureHttpOption: 0`, so
  cleartext HTTP is refused and every Supabase call has to be HTTPS;
  `enableInternalProfiler: 0`; the service key absent from Unity, the repository
  and the build, which I checked by scanning rather than assuming. One finding:
  `usePlayerLog: 1` writes `Debug` output to a log on the device, and that
  output included the whole saved row and full server error bodies — which is
  what improvement 3 took out.
- **The APK's integrity.** v0.1.0 is signed with Unity's development key and
  published as a bare APK with no checksum, which the release README already
  states. It is appropriate for a test build and not for 1.0.0, so a managed
  release keystore kept out of the repository and a published SHA-256 are now
  line 12 of the checklist rather than something to remember.
- **Where the security check sits in the release loop.** Between the merge and
  the version bump, in `Docs/RELEASE_PROCESS.md` section 2b — the point where
  there is still time to fix what it finds without unpicking a release. Two of
  the ten lines are commands with a required exit status, because a checklist
  whose every line needs careful human judgement is one that gets skimmed.

**Security Improvement or Test Performed**

Improvement 7 in Part V (the secret scanner and the pre-commit hook), the
release-configuration half of the Part IV review, tests T25–T27, and Part VII.

**Repository / Changeset / Commit Evidence**

| Item | Reference |
|---|---|
| Implementation commit | `46702b1` "Treat the save file, the key and the package as untrusted" on `claude/capstone-security-review-f3aa1d` |
| The leak | `0250197` "Create elevenlabs.key"; `2094726` "Stop tracking the ElevenLabs key" — still reachable from `origin/main` |
| The control | `Tools/secret_scan.sh`, `Tools/githooks/pre-commit` |
| The hook running | `secret_scan: clean.` in the output of commit `46702b1` |
| Release process | `Docs/RELEASE_PROCESS.md` section 2b |
| Prior ownership of this area | Release preparation, build generation and packaging for v0.1.0, per `Docs/RELEASE_PROCESS.md`'s responsibility table |

### What security consideration will I personally check while developing succeeding features?

Whether the feature needs a credential, and if it does, where that credential
lives when I am not looking at it. Every secret this group has handled got into
trouble the same way: it was convenient to put it in a file, and the file was in
the project folder. So before a feature that talks to anything — a text-to-speech
service, an analytics endpoint, a teacher's portal — I will decide where the key
goes before I write the call that uses it, add the path to `.gitignore` in the
same commit, and run `Tools/secret_scan.sh` before pushing. And if a key ever
does get committed again, I will rotate it the same day rather than deleting the
file and hoping.
