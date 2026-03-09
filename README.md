# Killer Complex

A first-person Unity game project built with the Universal Render Pipeline and the New Input System. 

The player recieves a disturbing email with pictures of their friends and family as well as all their personal information. The sender has sent a list of targets that are in the player's complex. The player will have to get rid of them or risk having that info being sent to those targets with the same task.

---

## Requirements

- **Unity:** 6000.3.9f1 (or the exact version in `ProjectSettings/ProjectVersion.txt`)
- Open in Unity Hub and use the recommended editor version for this project.

---

## Getting Started

1. Open the project in **Unity Hub** (Add → select this folder).
2. Open the scene: **Assets → Scenes → SampleScene**.
3. Press **Play** in the Editor.

---

## Controls

| Action   | Keyboard   |
|----------|------------|
| Move     | WASD       |
| Look     | Mouse      |
| Sprint   | Left Shift |
| Jump     | Space      |

Sprint is **toggle**: press once to sprint, press again to stop.

---

## Tech Stack

- **Unity** 6000.3.9f1  
- **Universal Render Pipeline (URP)**  
- **Input System** (new) — `Assets/InputSystem_Actions.inputactions`  
- **CharacterController** — first-person movement in `Assets/Scripts/PlayerController.cs`  

---

## Project Structure

| Folder / File                    | Description                    |
|----------------------------------|--------------------------------|
| `Assets/Scenes/`                 | Game scenes (e.g. SampleScene) |
| `Assets/Scripts/`                | C# scripts (e.g. PlayerController) |
| `Assets/Settings/`               | URP and volume settings       |
| `Assets/InputSystem_Actions.inputactions` | Input actions and bindings |

---

## License

All rights reserved.
