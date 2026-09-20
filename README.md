# SALINLAHI: A Voice-Interactive Filipino Language Learning Game
### Prototype: SALINLAHI Mini-Level (Database Connectivity Activity)

## Group Information
- **Group Number:** 33
- **Members:**
  - Daquis, Jhesza Mhei
  - Lacida, Kylo Bryan
  - Manzanero, Kyla Samantha


## Short Description
SALINLAHI is a 2D educational game that helps young Filipino learners who mainly use English build their Filipino vocabulary and confidence. This repository contains a small working prototype of our capstone project that demonstrates database connectivity.

The player enters a nickname, chooses an environment (Home, School, Park, or Community Store), and plays that environment's 3 levels. In each level, the player learns new Filipino words and then chooses the correct word for each challenge. Level 3 of each environment uses sequence challenges (for example, *lakad → hinto → talon*) where the character performs the chosen commands in order. Passing a level unlocks the next one.

The game saves each finished level to a cloud database, loads the player's saved progress when the same nickname is used again, and displays the player's records and the top scores inside the game.

## Tools and Technologies Used
- HTML, CSS, and JavaScript (browser-based game)
- Visual Studio Code with the Live Server extension
- Firebase Firestore (database)
- Firebase JavaScript SDK (loaded from Google's CDN)
- Git and GitHub Desktop (version control)
- Original SVG illustrations created for this project (in the `images` folder)

## Database or Storage Option Used
**Firebase Firestore**, using one collection named `player_scores`.

## Data Saved and Retrieved

**Saved:** one record every time a level is finished.

| Field | Description |
|---|---|
| player_id | Unique ID of the record (auto-generated) |
| player_name | Nickname of the player |
| environment | Home, School, Park, or Community Store |
| level | Level number (1 to 3) |
| score | Points earned in the level |
| correct_answers | Number of correct challenges |
| total_questions | Number of challenges in the level |
| remarks | Passed or Failed |
| created_at | Date and time the record was saved |

**Retrieved and displayed:**
- When a nickname is entered, the player's earlier records are loaded to restore unlocked levels.
- The **My Progress** screen shows the player's recent records and the top scores of all players.

## How to Run the Prototype
The game must be served through a local web server (opening `index.html` by double-clicking will not load the Firebase code).

1. Clone or download this repository.
2. Create your own Firebase project and a Firestore database (test mode is fine for the class activity), then register a web app to get your settings.
3. In the project folder, copy `firebase-config-example.js` and rename the copy to `firebase-config.js`.
4. Open `firebase-config.js` and replace the placeholder values with your own Firebase settings. Do not upload this file (it is listed in `.gitignore`).
5. Open the folder in Visual Studio Code, install the **Live Server** extension, right-click `index.html`, and choose **Open with Live Server**.
6. In the browser, check that the console shows "Firebase connected", then enter a nickname and play.

## Repository Files
```
├── index.html                    Game page and screens
├── style.css                     Game styling
├── script.js                     Game logic, saving, and displaying records
├── firebase-db.js                Firestore connection, save and retrieve functions
├── firebase-config-example.js    Sample settings file (no real credentials)
├── images/                       SVG illustrations
├── .gitignore                    Keeps firebase-config.js out of the repository
└── README.md
```

## Known Limitations and Unfinished Parts
- Voice command recognition is not included yet. Players choose words with on-screen buttons. Voice input is planned for the full capstone.
- Only the first three levels of each of the four environments are implemented. The final challenge, pre-test, and post-test are planned features.
- Players are identified by nickname only (no login), so two players using the same nickname share the same progress.
- The Firestore database is in test mode for this activity and needs stricter security rules before real use.
- The Filipino vocabulary is subject to validation by our language validator.

## References
- Firebase documentation: https://firebase.google.com/docs/firestore
- Firebase Web setup: https://firebase.google.com/docs/web/setup
- [Add any tutorials, videos, or AI tools your group used, and how]