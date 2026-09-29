# Activity 5 — Database Connectivity

**DRAFT — fill in the blanks marked `[ ... ]`, add the screenshots, then export as PDF.**

Every place you need a screenshot is marked like this:

> **SCREENSHOT:** what to capture

Everything else is already written. Read it once and change any wording you
do not agree with, because you must be able to explain it in the demo.

---

## Cover Page

**Activity 5 — Database Connectivity**

**Game Title:** SALINLAHI — A Journey Through Language

**Group Number:** 33

**Members:**
- Daquis, Jhesza Mhei G. — `[role]`
- Lacida, Kylo Bryan — `[role]`
- Manzanero, Kyla Samantha — `[role]`

**Course / Section:** `[fill in]`
**Instructor:** `[fill in]`
**Date Submitted:** `[fill in]`
**Repository Link:** `[paste GitHub link]`

> Roles to pick from: Game Logic Developer, Database/Storage Developer,
> UI and Testing Lead, Documentation Lead, Repository Manager.

---

## Part I. Game Prototype Overview

| Item | Answer |
|---|---|
| Group Number | 33 |
| Group Members | Daquis, Jhesza Mhei G.; Lacida, Kylo Bryan; Manzanero, Kyla Samantha |
| Prototype Direction | Capstone-Based |
| Game Title | SALINLAHI — A Journey Through Language |
| Game Type or Genre | Educational voice-controlled 2D game |
| Target Users | Filipino children learning basic Tagalog words |
| Platform Used | Android (also runs on desktop for testing) |
| Development Tool Used | Unity 6 (6000.6.2f1), C#, Visual Studio Code |
| Database or Storage Used | Supabase (PostgreSQL) through its REST API |

**What is the main purpose of your game?**

SALINLAHI teaches Filipino words to children. The player must say the word
out loud to make the character move. This helps the child learn the word by
speaking it, not only by reading it.

**What does the player do in the game?**

The player helps a child character named Kylo finish her morning routine. The
game shows a Filipino word on screen. The player says that word into the
microphone. If the word is correct, Kylo does the action. Level 1 has three
words: *Bangon* (get up), *Tayo* (stand), and *Lakad* (walk).

**What data will the game save?**

After the player finishes the level, the game saves one record: the player
name, the score, the number of tries, the level played, a short remark, and
the date and time.

**Why did your group choose this prototype direction?**

We chose the capstone-based direction because our game already makes useful
data. Every time the player says a word, the game already knows if it was
right or wrong. We only needed to save that result instead of throwing it
away. Building a separate game would have wasted that.

---

## Part II. Data Requirement Analysis

| Data Item | Purpose | Example Value |
|---|---|---|
| Player Name | Shows whose score it is | Kyla |
| Score | Number of words said correctly | 3 |
| Attempts | Number of tries the player made | 4 |
| Level or Stage | Shows which part of the game was played | Bahay - Scene 1: Umaga na! |
| Result or Remarks | Short readable result | 3/4 correct |
| Date or Time | Shows when the player played | 2026-09-27 13:46 |
| Player ID | Gives each record its own unique number | (made by the database) |

**Why does your game need to store this data?**

The game needs to remember how well each player did. Without saving, the
score disappears when the game closes. Saving lets the player see their old
scores, and lets a teacher see which words are hard for the children.

**How will the stored data improve the game or user experience?**

After each level, the game shows a list of recent scores. The player can see
their own result and compare it with their classmates. This makes the child
want to try again and get a better score.

**What data should not be stored? Explain why.**

We do not store the child's real full name, address, age, school, or any
voice recording. These are children, so we only keep a short nickname they
type themselves. We also do not store the Supabase secret key inside the
game. Keeping only what we need protects the children's privacy.

---

## Part III. Database Design

**Table name:** `player_scores`

| Field Name | Data Type | Description |
|---|---|---|
| player_id | uuid (auto) | Unique identifier of the record |
| player_name | text | Nickname typed by the player |
| score | integer | Number of words said correctly |
| attempts | integer | Total number of tries |
| level | text | Level or scene that was played |
| remarks | text | Short result, such as "3/4 correct" |
| created_at | timestamp (auto) | Date and time the record was saved |

> **SCREENSHOT:** Supabase → Table Editor → the `player_scores` table showing
> the column names.

**What is the purpose of your table or collection?**

It stores one row for every finished play session, so the game can show past
results later.

**Which field is used as the unique identifier?**

`player_id`. The database makes this value by itself using
`gen_random_uuid()`, so two records can never have the same ID.

**Which fields are required before saving a record?**

`player_name` and `level` are required. The rest have default values:
`player_id` and `created_at` are filled by the database, and `score` and
`attempts` start at 0.

---

## Part IV. Database Connectivity Implementation

**How the connection works (short explanation)**

The game talks to Supabase using its REST API. We did not install any extra
SDK. Unity's built-in `UnityWebRequest` sends normal web requests:

- To **save**, the game sends a `POST` request with the record as JSON.
- To **retrieve**, the game sends a `GET` request and reads back the rows.

Both requests carry the Supabase API key in the `apikey` header so the
server knows the request is allowed.

**Where the code is**

| Function | File |
|---|---|
| Save (POST) | `Assets/Scripts/SupabaseClient.cs` → `SaveScore()` |
| Retrieve (GET) | `Assets/Scripts/SupabaseClient.cs` → `FetchRecentScores()` |
| Decides when to save and what to show | `Assets/Scripts/SessionScoreTracker.cs` |
| Shape of one record | `Assets/Scripts/ScoreRecord.cs` |
| URL and API key settings | `Assets/Scripts/SupabaseConfig.cs` |

**What action triggers the saving of data?**

Saving happens when the player finishes all the words in the level. When the
last word (*Lakad*) is said correctly, `SceneObjectiveController` reports
that the level is complete, and `SessionScoreTracker` sends the record.

**How does the game retrieve stored data?**

Right after a successful save, the game sends a `GET` request for the five
newest rows, sorted by `created_at` from newest to oldest.

**Where is the retrieved data displayed?**

On the level-complete panel, under the heading **RECENT SCORES**. It shows
the player name, the score, and the date and time of each record.

**What problem did your group encounter during database connection?**

We had three real problems:

1. The game could not connect at first. The error said the project URL and
   API key were missing, because we had not filled them in yet.
2. Our Supabase key was a new-style `sb_publishable_` key, which is not a
   JWT. Our code was sending it in the `Authorization: Bearer` header, and
   the server rejected it.
3. The saved record always used the name "Bata", because the game had no
   place for the player to type a name.

**How did your group solve the problem?**

1. We created the config file in Unity and pasted the project URL and the
   anon/publishable key into it.
2. We changed the code to send the key only in the `apikey` header when it
   is not a JWT. Now both old and new key types work.
3. We added an "Enter Nickname" popup on the main menu, so the name the
   player types is the name that gets saved.

> **SCREENSHOT:** the game before saving (the level running, prompt showing).

> **SCREENSHOT:** the level-complete panel right after saving, showing the
> score and the RECENT SCORES list.

> **SCREENSHOT:** Supabase → Table Editor → rows of saved data.

> **SCREENSHOT:** `SupabaseClient.cs` open in VS Code, showing `SaveScore()`
> and `FetchRecentScores()`.

---

## Part V. Repository Information

| Item | Answer |
|---|---|
| Repository Platform | GitHub |
| Repository Name | Group33_DatabaseConnectivity |
| Repository Link | `[paste link]` |
| Repository Visibility | `[Public or Private]` |
| Instructor Access Confirmed | `[Yes or No]` |
| Number of Meaningful Commits | 16 |

**What files are included in the repository?**

The whole Unity project: the `Assets` folder (scripts, sprites, scenes,
fonts), the `ProjectSettings` folder, the `Packages` folder, the `Docs`
folder, the `README.md`, and a `.gitignore`. The `Library` folder is not
included because Unity rebuilds it, and it is about 2.6 GB.

**Where can the database save and retrieve functions be found in the code?**

In `Assets/Scripts/SupabaseClient.cs`. `SaveScore()` does the saving and
`FetchRecentScores()` does the retrieving.

**What does the README file explain?**

It explains the game, the group members, the tools we used, the database we
used, how to run the project, what data is saved and retrieved, where the
code is, the security notes, and the parts that are not finished yet.

**How did your group use the repository to organize or track progress?**

We made one commit for each finished part, such as the main menu, the voice
commands, the database connection, and the UI fixes. The commit messages
explain what changed, so we can see the order we built things in.

> **SCREENSHOT:** the GitHub repository main page.

> **SCREENSHOT:** the commit history list.

> **SCREENSHOT:** the README file shown on GitHub.

---

## Part VI. Application Output Demonstration

**Steps of the demonstration**

1. Open the game. The main menu appears.
2. Press **Play**. The "Enter Nickname" popup opens.
3. Type a nickname and press **OKAY**.
4. The journey map appears. Tap the **Bahay** (house) location.
5. Level 1 starts. The character is asleep in bed.
6. Say **Bangon**, then **Tayo**, then **Lakad** into the microphone.
   (On a computer, press the B, T and L keys instead, because the speech
   plugin only runs on Android.)
7. The level-complete panel appears. It shows the player name, the score,
   and the RECENT SCORES list read back from the database.
8. Open Supabase and show the new row in the `player_scores` table.
9. Press **OKAY** to go back to the journey map.

> **SCREENSHOT:** the main menu.

> **SCREENSHOT:** the "Enter Nickname" popup with a name typed in.

> **SCREENSHOT:** the level being played (word prompt and microphone visible).

> **SCREENSHOT:** the final panel with the RECENT SCORES list.

---

## Part VII. Systems Thinking Explanation

| Component | Role in the Prototype |
|---|---|
| User or Player | The child who types a nickname and says the Filipino words |
| Game Application | The Unity game that shows the words and checks the answers |
| Input | The nickname typed on the menu, and the spoken words from the microphone |
| Process | `PronunciationChecker` compares the spoken word with the target word and decides right or wrong; `SessionScoreTracker` counts the score and attempts |
| Database or Storage | Supabase `player_scores` table, which keeps every finished session |
| Output | The level-complete panel showing the score and the recent scores list |
| Network or API | Supabase REST API, called with `UnityWebRequest` over HTTPS |
| Repository | GitHub, which stores the source code and the history of our changes |
| Development Tool | Unity 6 and Visual Studio Code, used to build and edit the game |

**How does this activity show that a game application is also a system
composed of connected parts?** *(minimum 100 words)*

This activity shows that a game is not only pictures and buttons. It is a
system where many parts work together, and each part depends on the others.
The player gives input by typing a nickname and saying a word. The game
processes that input and decides if the answer is correct. The result then
becomes data, and that data travels over the internet to the database using
an API. The database stores the record safely and gives it back when the
game asks for it. The game then turns that data back into something the
player can read on screen. If one part fails, the whole chain stops. When
our API key was wrong, the game still ran, but nothing was saved and nothing
could be displayed. This proved that the interface, the logic, the network,
and the storage are all one connected system, not separate pieces.

---

## Part VIII. Group Reflection and Technical Learning

> **Please edit this in your own words before submitting.** It must sound
> like your group. Keep the real problems, but say them the way you would.

*(minimum 150 words)*

Our group learned that connecting a game to a database is not as hard as we
thought, but it must be done carefully. We learned that saving data is only
half of the work. Retrieving the data and showing it on screen is what makes
the data useful to the player. Database connectivity is helpful in games
because it lets the game remember the player. Without it, every score is
lost when the game closes, and the player cannot see if they are improving.

The hardest part was the connection itself. Our API key was a new type of
Supabase key, and our code was sending it in the wrong header, so the server
kept refusing it. We also spent time confused when the game showed an error
saying the settings were empty, because we had not pasted the project URL
and key yet. Fixing these taught us to read the error message carefully
instead of guessing.

The repository helped us a lot. Each commit described one finished part, so
we could see our progress and go back if something broke. It also kept our
API key out of the project, because we listed the settings file in
`.gitignore`.

If we had more time, we would show only the best score of each player, add
the recorded Filipino voice-over, and finish the other scenes of Level 1.
This activity will help our capstone because our game already needs to track
which words each child finds difficult, and now we know how to store and
read that data.

---

## Required Screenshots Checklist

Tick each one before exporting the PDF.

- [ ] Game interface (main menu)
- [ ] Player input or action that makes data (nickname popup / level being played)
- [ ] Successful data saving (level-complete panel after saving)
- [ ] Database table with saved records (Supabase Table Editor)
- [ ] Retrieved data displayed in the game (RECENT SCORES list)
- [ ] Code or connection setup (`SupabaseClient.cs`)
- [ ] Repository page (GitHub)
- [ ] Commit history
- [ ] README file

**Tip before taking screenshots:** delete the test rows first, then have all
three members play once using their real names. A list showing three
different names looks much better than one showing "Bata" and "test".

```sql
delete from player_scores where player_name in ('Bata', 'Sugar', 'test');
```
