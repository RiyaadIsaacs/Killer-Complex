# Technical setup — Killer Complex (Part 2 + Ollama)

## 1. Unity

1. Install **Unity Hub**.
2. Install editor version **6000.3.15f1** (see `ProjectSettings/ProjectVersion.txt` if this file drifts).
3. Hub → **Add** → select the `Killer-Complex` folder.
4. Open the project; allow packages to resolve.
5. Open **`Assets/Scenes/Main Game.unity`** for the main apartment slice, or **`Assets/Scenes/Tester scene.unity`** for a smaller scene used to exercise new gameplay/UI code in isolation.

---

## 2. Ollama (local LLM)

1. Download and install **Ollama** from [https://ollama.com](https://ollama.com).
2. Confirm it runs: open a terminal and run `ollama --version`.
3. Pull a model (example — pick one your hardware can run):

   ```bash
   ollama pull llama3.2
   ```

4. Keep the **daemon** running (Ollama runs in the background). Default API: **`http://127.0.0.1:11434`**.

5. Quick test (optional):

   ```bash
   ollama run llama3.2 "Say hello in one sentence."
   ```

Document the **exact model tag** your team ships with in **`ollama-plan.md`**.

---

## 2b. Hacking maze (UI)

- The **maze breach** minigame (`Assets/Scripts/UI/HackingMazeMinigame.cs`) draws a runtime overlay for the grid and status. **Keyboard instructions** (WASD / arrows, hold-to-repeat, Esc) and **how to play** appear in a **dock to the left** of the hacking terminal panel, not under the maze, so text stays readable at typical desktop resolutions.
- **Limited vision:** Only a **3×3** area around the player is revealed each step (**`visionRadius`** = 1 on the component). All other cells render as **fog** until the player moves adjacent. The **green uplink** goal is hidden until it enters that window.
- **Random goal:** After the maze and loop passages are carved, **`PickRandomGoalCell()`** places the green exit on a random walkable floor cell at least **`minGoalDistanceFromStart`** BFS steps from the start (default **6**), instead of a fixed corner tile.

---

## 2c. Delivery urgency timer

- **Script:** `Assets/Scripts/DeliveryUrgencyTimer.cs` — per-leg HUD countdown toward a timeout bad-ending; budget from **`initialLegTimeSeconds`** minus **`secondsReducedPerCompletedLeg`** × legs already completed (optional **`minimumLegTimeSeconds`** floor).
- **When it starts:** After **H** posts for an active delivery leg, the countdown **does not** tick while the **computer session is open** (`ComputerTerminal` / desk UI). It **starts when the player leaves the PC** (Escape or shutdown), so you can read **H**’s reply and keep chatting without losing time. If **H**’s reply arrives **after** the session is already closed, the timer **starts immediately**.
- **Wiring:** `ComputerTerminal.CloseTerminal()` calls **`DeliveryUrgencyTimer.NotifyComputerSessionClosed()`**. **`OllamaConnector.NotifyHPostedToMessenger()`** still arms the leg via **`TryStartCountdownAfterHMessage()`** (defer vs immediate). Post-drop “step away” replies clear pending starts via **`NotifyHSteppedAwayFromComputer()`** as before.

---

## 3. Unity ↔ Ollama

- **Script:** `Assets/Scripts/OllamaConnector.cs` — `UnityWebRequest` **POST** to **`http://localhost:11434/api/generate`** (override in the Inspector on the component). JSON body: `model`, `prompt`, `stream: false`. Default model field: **`mistral:7b-instruct`** (change to match what you pulled).
- **Timeout:** `requestTimeoutSeconds` on the component (default **180**).
- **Chat hook:** `Assets/Scripts/UI/ChatManager.cs` has an optional **`OllamaConnector`** reference. When set, each player send appends the visible `[Player]: …` line, shows a **typing indicator** (optional dedicated `TMP_Text`, or a temporary **`[H]: …`** line in the feed), then calls **`SendToOllama`** with the same plain text. The indicator hides when the HTTP response returns; then **`[H]: …`** is appended for the model reply. The **prompt** sent to Ollama is **not** the raw line only: see **`ollama-plan.md`** (hidden `[CONTEXT: …] Player says: …` block). **Persona and tone** for **H** (kidnapper / hostage fiction) are defined in the system string in `OllamaConnector.cs` (mirrored in **`prompts-used.md`**). Hidden context can include **`wifeStatusForLlmContext`** on **`OllamaConnector`** (tunable **Wife status** prose for threats; see **`prompts-used.md`**).
- **Desktop / hacking gate:** **`ComputerDesktopUI`** hides the **HACKING** dock icon until **`NotifyRemoteAccessEstablished`** (post–drop-off Ollama reply appends **`Remote access established`**). **`ComputerTerminal`** calls **`OnComputerSessionOpened` / `OnComputerSessionClosed`**. After the maze **breach-count gate**, **`ApplySuspicionIncrementForIgnoredMazeAttempt`** (meter only) and **`NotifyMazeBreachRoundAttemptFinished(..., mergeIgnore…)`** perform **one** `/api/generate` for breach + optional ignore beat (see **`ollama-plan.md`** §4).
- **References to assign in the scene:** `OllamaConnector` → **Chat Manager**, optional **Delivery Manager** (for `X/Y` delivery quota, **valid apartment list**, **destination apartment** for the active leg, **pickup** in context). `ChatManager` → **Ollama Connector** (optional; leave empty to skip LLM calls during playtest). **`DeliveryManager.prepareFirstDeliveryAfterSceneTick`** is **off** by default — first and **subsequent** legs are normally started from **`ChatManager`** on each messenger SEND while idle (`prepareDeliveryOnMessengerSendWhenIdle`; Unity maps the old field name **`prepareFirstDeliveryOnFirstPlayerMessage`**). **`DeliveryItem`** needs the same **Delivery Manager** reference and a collider on **interactable** layers so **Interact** registers pickup before zones accept drop-off.

---

## 3b. Bad ending (quota finished) + door + audio

- **Flow:** After the **last** delivery leg, the next messenger send uses a hidden **`[SYSTEM]`** trap prompt in **`OllamaConnector.TryBuildBadEndingPlayerTurn`** (not posted as a separate visible `[SYSTEM]` line). **`BadEndingOrchestrator`** (scene object) runs **`StartBadEnding()`**: closes **`InteractDoor`** instances with **My apartment door** checked, starts **3D** knock bursts (**`SoundManager.PlayOneShotWorld`**) with optional **repeat every N seconds**, locks **`ComputerDesktopUI`** to **shutdown only**. **`Interact`** on that door opens it and shows the bad-end canvas; **`RevealBadEndingCanvas()`** plays a **non-spatial** gunshot via **`SoundManager.PlayOneShotNonSpatial`** when configured.
- **`BadEndingOrchestrator`:** Assign **bad ending canvas root** (inactive overlay). Inspector: **gunshot** clip for canvas reveal; **knock repeat interval** / toggle. **`OllamaConnector`** may assign **`BadEndingOrchestrator`** explicitly; otherwise it is resolved at runtime when present.
- **`InteractDoor`:** Exactly one apartment unit should have **My apartment door** + knock clip (or rely on **`SoundManager`** **Door knock** fallback). **`SoundManager`** (e.g. on desktop canvas): **notification** clip (2D), optional **door knock** clip (fallback for knocks), **`PlayOneShotNonSpatial`** used by the orchestrator for the gunshot.
- **Prefab:** **Tools → Killer-Complex → Create Bad Ending Canvas Prefab** writes **`Assets/Prefabs/BadEndingCanvas.prefab`** (black full-screen + **Bad Ending** TMP). Drag the instance into the scene and wire it to the orchestrator.

---

## 4. Controls (current prototype)

| Action | Binding |
|--------|---------|
| Move | WASD |
| Look | Mouse |
| Sprint | Left Shift (toggle) |
| Jump | Space |
| Interact | *(check `InputSystem_Actions` — often E)* |

---

## 5. Machine specs (fill in for markers)

| Machine | CPU | RAM | GPU | Notes |
|---------|-----|-----|-----|-------|
| Dev A | | | | |
| Dev B | | | | |

---

## 6. Troubleshooting

| Issue | Things to try |
|-------|----------------|
| Unity version mismatch | Install exact version from `ProjectVersion.txt`. |
| Ollama connection refused | Start Ollama; check nothing else uses port **11434**; try `127.0.0.1` not `localhost` if IPv6 oddities. |
| Model too slow | Smaller quant / smaller model; document in `ollama-plan.md`. |
