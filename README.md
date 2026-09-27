# Arena Last Fight

A 3D, one-on-one sword-fighting game made in Unity for **[Course code / name — e.g. GDD 542 Game Development]**, Project 2.

Two knights face off inside a boxing-ring arena. Unlike classic fighting games where you can only move back and forth on one line, both fighters move freely in every direction and always turn to face each other, so you can circle, sidestep and close the distance from any angle.

> **Play it:** [itch.io link]
> **Gameplay video:** [YouTube link]
> **Source code:** https://github.com/Oceansiam/Arena-Last-Fight

---

## Inspiration

- **Mortal Kombat: Shaolin Monks**, the final Shao Kahn battle, where the fight takes place in an open arena instead of on a flat 2D line.
- **Dueling Grounds** (Roblox): free-movement 1v1 melee duels built around spacing, blocking and timing.
- Classic arcade fighters such as **Street Fighter**, **Tekken** and **Mortal Kombat**: health bars, rounds, and a clear winner screen.

---

## Features

- Local 1v1 multiplayer on one keyboard
- Free 360° movement inside the ring, with fighters automatically facing each other
- Light attack combos, blocking and jumping
- Health bars and a winner announcement at the end of the match
- Main menu, pause menu, and an in-game controls overlay
- Mixamo character animations (idle, walk, attack, block, jump, death)
- Sound effects for hits, blocks, jumps and knockouts, plus background music

---

## Controls

| Action | Player 1 | Player 2 |
|---|---|---|
| Move | W A S D | Arrow keys |
| Jump | Space | Right Shift |
| Light Punch / Attack | V | J |
| Block | B | K |

| Other | Key |
|---|---|
| Pause / Resume | Esc |
| Show / hide controls | C |

---

## How to Play

1. Download the **Windows** or **macOS** build from the itch.io page and unzip it.
2. Run the game. **macOS:** if it's blocked the first time, right-click the app → **Open**.
3. Press **Start** on the main menu.
4. Reduce your opponent's health to zero to win.

---

## Opening the Project in Unity

1. Clone the repository:
   ```
   git clone https://github.com/Oceansiam/Arena-Last-Fight.git
   ```
2. Open the folder in **Unity 6 (6000.5.3f1)** through Unity Hub.
3. Open `Assets/_Project/Scenes/MainMenu.unity` and press Play.

The first time you open it, Unity rebuilds its `Library` folder, so this can take a few minutes.

---

## Built With

- **Unity 6 (6000.5.3f1)**, Universal Render Pipeline (URP)
- **C#**
- **TextMeshPro** for UI text

### Project Structure

```
Assets/_Project/
├── Scenes/          MainMenu, Arenas/MVP1_Arena
├── Scripts/
│   ├── Character/   Fighter + state machine (Idle, Walk, Jump, Attack, Block, Dead)
│   ├── Combat/      Hitbox, Hurtbox, Health, MoveData
│   ├── Input/       Player 1 and Player 2 input readers
│   ├── Core/        MatchManager (win/lose, round flow)
│   ├── Camera/      Combat camera
│   └── UI/          Main menu, pause menu, controls overlay, health bars
├── ScriptableObjects/MoveData   attack timing and damage values
├── Arts/Environment             arena models
└── Audio/                       sound effects and music
```

---

## Team

| Name | Role / Contributions |
|---|---|
| [Krisdipas Kongsakul] | [e.g. animation setup, character integration, main menu, pause menu and controls overlay, input fixes, builds] |
| [TAI-AN CHEN] | [e.g. combat system and state machine, arena, audio integration, block mechanic] |

---

## Known Issues

- [List anything that still doesn't work perfectly, e.g. "Characters can occasionally clip into the ring ropes."]

---

## Licence

### Project code
The C# source code written for this project is released under the **[MIT Licence / choose one]**. See `LICENSE` for details.

### Third-party assets
This project uses assets made by others. All rights remain with their creators.

| Asset | Creator / Source | Licence |
|---|---|---|
| Character animations and base character (X Bot) | Adobe **Mixamo** — https://www.mixamo.com | Mixamo terms of use (free for personal and commercial projects) |
| Knight character model | [Creator / source link] | [Licence] |
| Boxing ring model | [Creator / source link] | [Licence] |
| Retro Fantasy Kit (walls, props) | [Creator, e.g. Kenney — source link] | [Licence, e.g. CC0] |
| Sword clash / armor impact sound effects | **DRAGON-STUDIO** via Pixabay — https://pixabay.com | Pixabay Content Licence |
| Effort and damage grunt sounds | **VoiceBosch** (SoundBiter), via OpenGameArt — https://opengameart.org/node/162884 and https://opengameart.org/node/163052 | **CC-BY-SA 4.0** — credit required |
| Music: "Imposters" | **Solis** — [source link] | [Licence] |
| Music: "Fast Bow" | **FableForte** — [source link] | [Licence] |
| TextMeshPro | Unity Technologies | Unity Companion Licence |

The VoiceBosch sound effects are licensed under Creative Commons Attribution-ShareAlike 4.0 (https://creativecommons.org/licenses/by-sa/4.0/). They are used unmodified [or: "trimmed / edited"].
