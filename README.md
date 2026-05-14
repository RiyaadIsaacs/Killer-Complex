# Killer Complex

First-person Unity project (**URP**, **Input System**) for **Game Design 3A — Part 2**: a local **Ollama** LLM drives an in-fiction online persona (**H**, a threatening hacker) who coerces the player through chat, assigns **package deliveries** in a multi-floor complex, and reacts while the player uses a **home computer** (hacking mini-games, waiting for the next job). **Fiction only** — no real personal data is collected; see content note below.

**Unity version:** **6000.3.15f1** (authoritative: `ProjectSettings/ProjectVersion.txt`)

---

## Content note

This prototype includes **pressure / blackmail / thriller** themes and optional references to **illicit packages** as **ambiguous fiction**. Generated text may be unsettling. The game uses a **local LLM**; players should be aware output is **machine-generated** and may be inconsistent. Not suitable for young children.

---

## Documentation index (POE + team)

| File | Purpose |
|------|---------|
| [ollama-plan.md](ollama-plan.md) | **Required** — model, data flow, prompts summary, risks, inference timing |
| [plan.md](plan.md) | Milestones, slice scope, ownership |
| [setup.md](setup.md) | Unity + Ollama install, troubleshooting |
| [rules.md](rules.md) | Git discipline, **pre-push checklist** (includes `prompts-used.md`) |
| [refinements-changes.md](refinements-changes.md) | Dated changelog and AI-assisted decisions |
| [prompts-used.md](prompts-used.md) | **All prompts tested** — update before every push |
| [RiyaadWork.md](RiyaadWork.md) | Riyaad’s contribution log *(partner: add your own file)* |

**Also required by the brief (add when ready):** High Concept document, **LLM Integration Report** (600–800 words, IEEE-style references as per module rules).

---

## Requirements

- **Unity Hub** + editor **6000.3.15f1**
- **Ollama** installed locally for Part 2 LLM features ([ollama.com](https://ollama.com)) — see [setup.md](setup.md)

---

## Getting started

1. Clone / open this folder in **Unity Hub** → Add project.
2. Open **`Assets/Scenes/Main Game.unity`** for the primary playtest layout, or **`Assets/Scenes/Tester scene.unity`** for a compact scene used to validate new scripts and UI.
3. Install and run **Ollama**; pull the model your team documents in `ollama-plan.md`.
4. Wire **Messenger → Ollama** in the scene: on `ChatManager`, assign **`OllamaConnector`**; on `OllamaConnector`, assign **`ChatManager`** and (optionally) **`DeliveryManager`** so the hidden LLM **`[CONTEXT: …]`** matches delivery progress (**`currentDeliveryID` / `TotalDeliveryLegs`**), **valid apartment list**, **current destination apartment** when a leg is active, and **pickup state** when a reception **`DeliveryItem`** is used. Optionally assign **`Typing Indicator Text`** on `ChatManager` for a pulsing “H is typing…” label (otherwise a temporary line is appended to the feed). Run **Ollama** with the model named in `ollama-plan.md`, then press **Play** and send a chat message to trigger a generate call. Model replies appear as **`[H]: …`** in the messenger. **Delivery flow:** the opening **H** intro does **not** assign a job by default. When **`prepareDeliveryOnMessengerSendWhenIdle`** is on (Inspector; Unity migrates the old **`prepareFirstDeliveryOnFirstPlayerMessage`** value), **each** messenger SEND **while no leg is active** and runs remain calls **`PrepareNextDeliveryFromAi`** before Ollama — so the player talks to **H** before the next reception package and drop-off roll appear (unless you use **`prepareFirstDeliveryAfterSceneTick`** on `DeliveryManager` for an automatic **first** leg only). **Drop-off:** **`DeliveryZone`** uses **`Interact`** (same raycast as doors); use a **non-trigger** collider on the door/object. **Reception package:** player must **Interact** on **`DeliveryItem`** before **`DeliveryZone`** accepts drop-off when that item is assigned on **`DeliveryManager`**. On **`ComputerDesktopUI`**, assign **`Shutdown Computer Button`** and **`Computer Terminal`** so shutdown exits the session. **Hacking maze:** when the breach maze is open, **controls and how-to-play** are shown in a **narrow panel to the left of the terminal window** (built at runtime by **`HackingMazeMinigame`**) so the maze grid is not clipped by footer text. **HUD:** **`GlobalNotificationHud`** shows delivery success/failure toasts (`ShowDeliveryFeedback`); use **GameObject → UI → Global Notification HUD (persistent)** when needed. World use: put **`ComputerTerminal`** (and optionally **`ComputerInteract`**) on the computer object the player looks at; **Interact** (often **E**) raycasts via **`PlayerController`**.

---

## Controls

| Action   | Keyboard   |
|----------|------------|
| Move     | WASD       |
| Look     | Mouse      |
| Sprint   | Left Shift |
| Jump     | Space      |

Sprint is **toggle**: press once to sprint, press again to stop.  
**Interact:** see `Assets/InputSystem_Actions.inputactions` (commonly **E** once wired).

---

## Tech stack

- **Unity** 6000.3.15f1  
- **Universal Render Pipeline (URP)**  
- **Input System** — `Assets/InputSystem_Actions.inputactions`  
- **CharacterController** — `Assets/Scripts/PlayerController.cs`  
- **Ollama** — local HTTP **`/api/generate`** via `Assets/Scripts/OllamaConnector.cs` (see `ollama-plan.md` for prompt contract and context block)

---

## Project structure

| Path | Description |
|------|-------------|
| `Assets/Scenes/` | Scenes |
| `Assets/Scripts/` | Gameplay scripts |
| `Assets/Settings/` | URP / rendering settings |
| `Assets/InputSystem_Actions.inputactions` | Input bindings |

---

## AI tools used (edit as appropriate)

- **Cursor** — planning, documentation, future code assistance *(list others: ChatGPT, etc.)*
- **Ollama** — in-game local LLM via `OllamaConnector` + `ChatManager` send hook

---

## Credits

- *(Team names, assets, licences)*

---

## License

All rights reserved.
