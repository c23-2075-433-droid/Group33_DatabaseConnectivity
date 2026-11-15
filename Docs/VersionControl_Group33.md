# Collaborative Version Control — SALINLAHI

**Systems Fundamentals — Group Development Activity with Individual Submission**

| | |
|---|---|
| **Project Title** | SALINLAHI: A Voice-Interactive Filipino Language Learning Game |
| **Group Number** | 33 |
| **Group Members** | Daquis, Jhesza Mhei G. — Game Logic Developer |
| | Lacida, Kylo Bryan — UI and Testing Lead |
| | Manzanero, Kyla Samantha — Documentation Lead |
| **Version-Control Platform** | Git + GitHub |
| **Repository Link** | https://github.com/c23-2075-433-droid/Group33_DatabaseConnectivity |
| **Course** | BSIT Capstone Project — University of Perpetual Help System Laguna – JONELTA |

Every number, hash and date in this report was read directly out of the
repository with `git` and out of GitHub's own records on **12 November 2026**.
Counts are taken from `main` unless stated otherwise, because the repository
also carries a second, unrelated history (`origin/salinlahi-local-main`), and
counting across all branches would report much of the project twice.

---

## Part I. Version-Control Readiness Check

### Project facts

| Item | Value |
|---|---|
| **Project Title** | SALINLAHI: A Voice-Interactive Filipino Language Learning Game |
| **Version-Control Platform** | Git + GitHub |
| **Repository Name** | `Group33_DatabaseConnectivity` |
| **Repository Link / Identifier** | https://github.com/c23-2075-433-droid/Group33_DatabaseConnectivity |
| **Main Branch** | `main` (GitHub's default branch; `origin/HEAD` points at `origin/main`) |
| **Development Platform / Game Engine** | Unity 6, version `6000.6.2f1`, pinned in `ProjectSettings/ProjectVersion.txt`; C# |
| **Repository Visibility / Access Setting** | **Public.** The instructor can open the repository, the commit history and every pull request without being added as a collaborator. |
| **Repository created** | 2 October 2026 |
| **Latest push on `main`** | 12 November 2026, commit `b773db6` |

### Account map

The history is traceable to people only if each account is matched to a member,
so the mapping is recorded here once and used throughout this report:

| Version-control identity | Member | Commits in `main` | Note |
|---|---|---|---|
| `c23-2075-433-droid <c23-2075-433@uphsl.edu.ph>` | Daquis, Jhesza Mhei G. | 199 | School account; owns the repository |
| `Kylo Bryan Lacida <lacidakylo@gmail.com>` | Lacida, Kylo Bryan | 38 | His commit identity. Not registered to his GitHub account, so GitHub shows these 38 commits as unattributed — see the finding below |
| `c23-1203-525-droid` | Lacida, Kylo Bryan | 0 | His GitHub account. Holds collaborator access and opened pull request #16, but authored none of its commits |
| `Kyla Samantha Manzanero <manzanerokyla@gmail.com>` | Manzanero, Kyla Samantha | 13 | GitHub account `Milkyshots` |

### Readiness table

| Requirement | Status | Evidence / Action Taken |
|---|---|---|
| **Repository exists** | **Complete** | GitHub repository `Group33_DatabaseConnectivity`, created 2 October 2026, served over HTTPS as remote `origin`. The root commit of `main` is `6f338bb` ("Score the whole level, not just the scene that ends it", 3 October 2026). An earlier web prototype is preserved on `origin/firebase-web-prototype` instead of being deleted. |
| **All active members have appropriate access** | **Complete** | All three members can push, and all three appear in `main`'s history as authors: Daquis (199 commits, as `c23-2075-433-droid`), Lacida (38 commits) and Manzanero (13 commits). Lacida holds two identities — he commits as `Kylo Bryan Lacida` and reviews from his GitHub account `c23-1203-525-droid`, which opened pull request #16 from the in-repository branch `preview/bangon-plus-typography` for Daquis to merge. So the member who requested the review and the member who merged it were different people, which is the separation Part V depends on. |
| **Main / accepted development branch exists** | **Complete** | `main`, recognised by the whole group as the accepted version. 250 commits spanning 3 October – 12 November 2026, with 19 remote branches alongside it. Nobody develops directly on `main`; it only receives merges. |
| **Project can be retrieved by another member** | **Complete** | Verified on 12 November 2026 by cloning `main` into an empty folder. The clone produced 2,103 files and contained every marker Unity needs to recognise a project rather than a pile of loose files: `ProjectSettings/ProjectVersion.txt`, `ProjectSettings/EditorBuildSettings.asset`, `Packages/manifest.json`, all 18 `.unity` scenes and all 948 `.meta` files. `git ls-remote origin main` resolves to `b773db6`, so the remote is reachable for anyone with the link. |
| **Project opens correctly after retrieval** | **Complete, with one documented manual step** | The editor version is pinned to `6000.6.2f1`, so every member opens the same Unity and no one is silently upgraded. One file is deliberately *not* in the repository and is created locally after cloning: `Assets/Resources/SupabaseConfig.asset`, generated from the editor menu **Tools > SALINLAHI > Create Supabase Config**. The clone test confirmed the file is absent and that its template, `Assets/Resources/SupabaseConfig.example.txt`, is present. The step is written down in `Docs/SupabaseSetup.md` and in the README. |
| **Generated / unnecessary files are handled appropriately** | **Complete** | `.gitignore` carries 47 rules. The clone test confirmed that `Library/`, `Temp/` and `Logs/` do not come down with the project — they are Unity's import cache and scratch space, rebuilt automatically on first open. Also excluded: `Obj/`, `Build/`, `Builds/`, `UserSettings/`, `MemoryCaptures/`, `Recordings/`, the generated IDE files (`*.csproj`, `*.sln`, `.vs/`, `.vscode/`, `.idea/`), build outputs (`*.apk`, `*.aab`, `*.unitypackage`), `__pycache__/` and `.DS_Store`. Two credentials are excluded by design, each with its tracked `.example` template beside it: the Supabase config and `Tools/elevenlabs.key`. |
| **Important project files are tracked** | **Complete** | 2,103 tracked files on `main`: 110 `.cs` scripts, 18 `.unity` scenes, 948 `.meta` import files, 438 `.wav` voice and music clips, 416 `.png` sprites, 27 `.ttf` fonts, 24 `.asset` ScriptableObjects, 69 `.py` asset-pipeline tools, and the `ProjectSettings/` (24 files) and `Packages/` (2 files) configuration. The `.meta` files are tracked deliberately, not tolerated as noise: Unity stores each asset's GUID there and every scene reference points at that GUID, so a sprite committed without its `.meta` hands the next member a project with broken references. |
| **Change history is available** | **Complete** | Every commit records author, date and message. 250 commits on `main`, 16 pull requests — all 16 merged — and the history answers the five questions a version-control log exists to answer: who changed it, what changed, why, who reviewed it, and when it was integrated. |

### Two readiness findings from our previous check, now closed

Our last readiness check, dated 8 November 2026, recorded two problems. Both
have since been fixed inside the repository rather than only in the write-up,
and the fixes are themselves traceable:

1. **A billing credential was tracked.** `Tools/elevenlabs.key` had been added
   through GitHub's web "create file" page, which does not consult
   `.gitignore`, so the path was ignored *and* tracked at the same time. It was
   untracked by commit `2094726` — *"Stop tracking the ElevenLabs key"*,
   authored by **Kylo Bryan Lacida**, 8 November 2026. A clone of `main` today
   brings down only `Tools/elevenlabs.key.example.txt`. The key itself was
   treated as exposed and reissued, because anything pushed lives on in every
   clone regardless of repository visibility.

2. **One member was not traceable as an author on `main`.** Lacida had push
   access and had used it, but his early work reached `main` only as files
   carried across from an unrelated history, so `main` credited someone else as
   its author. That gap is closed: his first directly reachable commit is
   `e515e2f` (*"Animate Paaralan Scene 2, and put Kylo back in his own shirt"*,
   8 November 2026), and he now has **38** commits in `main`'s history,
   including the credential fix above and the Paaralan guard animation work of
   12 November.

### Two items still worth watching

**Lacida's 38 commits are unattributed on GitHub.** Git records him correctly:
every one of those commits carries `Kylo Bryan Lacida <lacidakylo@gmail.com>`
as its author, and `git log --author=lacidakylo` lists them all. GitHub,
however, links a commit to a profile by matching the author's email against the
emails registered on that account, and `lacidakylo@gmail.com` is not registered
to his account `c23-1203-525-droid`. Checked through GitHub's own API on
12 November 2026, commits `e515e2f`, `2094726` and `c53d0a2` all come back with
no linked account, and the repository's Contributors list names only two
people, Daquis and Manzanero.

This matters for this activity specifically, because the history has to identify
the member responsible for each change. The repository is not wrong — the
authorship is in the commits — but anyone reading GitHub's web interface rather
than the commit list would undercount the team. The fix needs no history
rewrite: Lacida adds `lacidakylo@gmail.com` under **Settings > Emails** on his
GitHub account, and GitHub retroactively attributes all 38 commits to him. Until
that is done, his contribution should be evidenced with `git log` output or the
commit pages themselves, where his name and email are plainly visible.

**Git LFS is not configured.** The tracked project is **480.8 MB**, measured as
Git blob bytes on `main`, and binaries are nearly all of it — the five
background music tracks are about 30 MB each on their own. There is no `.gitattributes`, so every clone
pulls the full history of every binary. This is recorded here as a known
condition, not a failure: it is addressed in Part II, where the group's
handling of large assets and the LFS decision are set out.

---
