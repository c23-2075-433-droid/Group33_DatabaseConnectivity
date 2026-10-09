# Mid-Act7 — 10% System Build

**SALINLAHI: A Voice-Interactive Filipino Language Learning Game**
Group 33 — Daquis, Jhesza Mhei G. · Lacida, Kylo Bryan · Manzanero, Kyla Samantha
BSIT Capstone — University of Perpetual Help System Laguna – JONELTA

## What is in this folder

| File | What it is |
|---|---|
| `SALINLAHI.apk` | Installable Android build of the current system |
| `SALINLAHI_Source.zip` | Unity 6 project source (scripts, scenes, art, no build cache) |
| `System Project Screenshot1.jpg` | Screenshot of the system running |
| `MidAct7_ProjectStatus.pdf` | This file |

## How the build answers the proposed objectives

| Specific objective (Ch. 1, §1.4.2) | What is already built |
|---|---|
| Develop a game that uses selected Filipino vocabulary as voice commands | 22 Filipino words are wired as voice commands: Bangon, Tayo, Lakad, Talon, Kaliwa, Kanan, Bukas, Ilaw, Tabo, Sabon, Sipilyo, Tuwalya, Maligo, Suklay, Damit, Medyas, Sapatos, Bag, Kunin, Kumusta, Pasok, Takbo. Recognition runs through the device speech recognizer at `fil-PH`. |
| Implement voice commands that let players perform specific actions | Each word drives one action — standing up, walking, turning, opening the bathroom door, switching the light on, taking an object off the shelf, bathing, dressing one garment at a time, picking up the school bag. |
| Provide gameplay activities where students practice and apply the words | Chapter 1 (Bahay) runs as four connected scenes — waking up, walking to the bathroom, the bath, getting dressed — plus Chapter 2 Scene 1 (Paaralan, arriving at school). Scenes fade into each other as one continuous morning routine. |
| Develop a simple, user-friendly interface for Kinder–Grade 3, including pre-readers | Main menu, nickname entry, level-select map with four level badges, and per-scene picture-and-prompt objectives rather than text-heavy menus. |
| Evaluate the game for functionality, usability and usefulness | Every session's score and attempts are saved to a Supabase (PostgreSQL) database and read back on the completion panel, so playtest data is collected for the evaluation phase. |

## Scope covered so far

- **Chapter 1 — Bahay (Home):** Scenes 1–4 playable end to end.
- **Chapter 2 — Paaralan (School):** Scene 1 built.
- **Parke and Palengke:** designed, locked on the map.
- **Database connectivity:** save and retrieve working (`player_scores` table, Row Level Security, anon key only).

## Not yet built

- Voice-over audio (recording script written, clips not recorded).
- Remaining Paaralan, Parke and Palengke scenes.
- Formatted leaderboard and per-player score filtering.

## How to run

- **Android:** install the APK and say the words out loud.
- **Editor/desktop:** open `Assets/Scenes/MainMenu.unity` in Unity 6000.6.2f1 and press Play. The device recognizer is unavailable on desktop, so pressing **Enter** simulates saying whatever word the scene is currently asking for.
