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
   ollama pull llama3.2:3b
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
- **Limited vision:** A square window around the player is revealed each step (**`visionRadius`** on the component — default may be **2** for a 5×5 window; check the Inspector). All other cells render as **fog** until the player moves into range. The **green uplink** goal is hidden until it enters that window.
- **Random goal:** After the maze and loop passages are carved, **`PickRandomGoalCell()`** places the green exit on a random walkable floor cell at least **`minGoalDistanceFromStart`** BFS steps from the start (default **6**), instead of a fixed corner tile. Hazard grids are cleared before goal BFS; **`IsHazardAt`** is bounds-safe so reopening the maze at a new tier cannot throw. Reopening the overlay runs a one-frame deferred layout rebuild so the grid and fog align with the host **`RectTransform`**.
- **Delivery timer:** When the urgency countdown is deferred because **H** posted while the PC was open, closing the **maze overlay** (win, fail, or abort) also calls **`DeliveryUrgencyTimer.TryResumeDeferredCountdownAfterMazeClosed()`** so the HUD can start without a full terminal exit.

---

## 2c. Delivery urgency timer

- **Script:** `Assets/Scripts/DeliveryUrgencyTimer.cs` — per-leg HUD countdown toward a timeout bad-ending; budget from **`initialLegTimeSeconds`** minus **`secondsReducedPerCompletedLeg`** × legs already completed (optional **`minimumLegTimeSeconds`** floor).
- **When it starts:** After **H** posts for an active delivery leg, the countdown **does not** tick while the **computer session is open** (`ComputerTerminal` / desk UI). It **starts when the player leaves the PC** (Escape or shutdown), so you can read **H**’s reply and keep chatting without losing time. If a countdown is **already running**, it **pauses** while the PC is open and **resumes** when the session closes (`NotifyComputerSessionOpened` / `NotifyComputerSessionClosed`). **`HackingMazeMinigame`** closing also flushes deferred start so hacking mid-job does not strand the HUD. If **H**’s reply arrives **after** the session is already closed, the timer **starts immediately**.
- **Wiring:** `ComputerTerminal.CloseTerminal()` calls **`DeliveryUrgencyTimer.NotifyComputerSessionClosed()`** (shared commit path as maze-close flush). **`OllamaConnector.NotifyHPostedToMessenger()`** arms the leg via **`TryStartCountdownAfterHMessage()`** (reorder-safe suppress handling for overlapping replies). Post-drop **step away** uses **`NotifyHSteppedAwayFromComputer()`** only when there is **no** active gameplay leg, so a late step-away line cannot kill a timer for a job that already rolled.
- **Scene restart:** `DeliveryManager` lives on the same **DontDestroyOnLoad** HUD as this timer — **`ResetRunStateForNewPlaySession`** runs from **`DeliveryUrgencyTimer`** on gameplay (and menu) load so **pause → Restart** or **Main Menu → Play** resets **`currentDeliveryID`**, rebinds scene package/spawn/drop zones, and restores the **first-leg** budget (**160s** default). **`GlobalNotificationHud.ResetTransientNotificationsForSession`** clears stale delivery toasts. If **`prepareFirstDeliveryAfterSceneTick`** is on, the one-frame first-leg prepare is queued from that load path (not only from **`DeliveryManager.Start`**, which does not run again on a persisted object).

---

## 3. Unity ↔ Ollama

- **Script:** `Assets/Scripts/OllamaConnector.cs` — `UnityWebRequest` **POST** to **`http://localhost:11434/api/generate`** (override in the Inspector on the component). JSON body: `model`, `prompt`, `stream: false`. Default model field: **`llama3.2:3b`** (change to match what you pulled).
- **Timeout:** `requestTimeoutSeconds` on the component (default **180**).
- **Chat hook:** `Assets/Scripts/UI/ChatManager.cs` has an optional **`OllamaConnector`** reference. When set, each player send appends the visible **`Player:`** line (bold sender in TMP), shows a **typing indicator** (optional dedicated `TMP_Text`, or a temporary line in the feed), then calls **`SendToOllama`** with the same plain text. The indicator hides when the HTTP response returns; then **H**’s reply is appended (**bold `H:`** label + body in `<noparse>`). The **prompt** sent to Ollama is **not** the raw line only: see **`ollama-plan.md`** (hidden `[CONTEXT: …] Player says: …` block). **Persona and tone** for **H** (kidnapper / hostage fiction) are defined in the system string in `OllamaConnector.cs` (mirrored in **`prompts-used.md`**). **`OllamaConnector`** strips leaked CONTEXT / quest-log lines and corrects **`my wife` → `your wife`** before posting. Hidden context can include **`wifeStatusForLlmContext`** on **`OllamaConnector`** (tunable **Wife status** prose for threats; see **`prompts-used.md`**).
- **Desktop / hacking gate:** **`ComputerDesktopUI`** hides the **HACKING** dock icon until **`NotifyRemoteAccessEstablished`** (post–drop-off Ollama reply appends **`Remote access established`**). **`ComputerTerminal`** calls **`OnComputerSessionOpened` / `OnComputerSessionClosed`**. After the maze **breach-count gate**, **`ApplySuspicionIncrementForIgnoredMazeAttempt`** (meter only) and **`NotifyMazeBreachRoundAttemptFinished(..., mergeIgnore…)`** perform **one** `/api/generate` for breach + optional ignore beat (see **`ollama-plan.md`** §4).
- **References to assign in the scene:** `OllamaConnector` → **Chat Manager**, optional **Delivery Manager** (for `X/Y` delivery quota, **valid apartment list**, **destination apartment** for the active leg, **pickup** in context). `ChatManager` → **Ollama Connector** (optional; leave empty to skip LLM calls during playtest). **`DeliveryManager.prepareFirstDeliveryAfterSceneTick`** is **off** by default — first and **subsequent** legs are normally started from **`ChatManager`** on each messenger SEND while idle (`prepareDeliveryOnMessengerSendWhenIdle`; Unity maps the old field name **`prepareFirstDeliveryOnFirstPlayerMessage`**). When **`prepareFirstDeliveryAfterSceneTick`** is **on**, auto-first-leg defer is also triggered on **gameplay scene load** via **`DeliveryUrgencyTimer`** so scene **Restart** still prepares a job. **`DeliveryItem`** needs the same **Delivery Manager** reference and a collider on **interactable** layers so **Interact** registers pickup before zones accept drop-off.

---

## 3b. Bad ending (quota finished) + door + audio

- **Flow:** After the **last** delivery leg, the next messenger send uses a hidden **`[SYSTEM]`** trap prompt in **`OllamaConnector.TryBuildBadEndingPlayerTurn`** (not posted as a separate visible `[SYSTEM]` line). **`BadEndingOrchestrator`** (scene object) runs **`StartBadEnding(deferRestrictedDesktopUntilComputerClosed: …)`**: closes **`InteractDoor`** instances with **My apartment door** checked, starts **3D** knock bursts (**`SoundManager.PlayOneShotWorld`**) with optional **repeat every N seconds**. For the **Ollama** trap request, desktop lock (**messenger / hacking hidden, shutdown only**) is **deferred** until **`ComputerTerminal.CloseTerminal()`** so the player can read the final **H** line; **world traps** still apply the lock **immediately**. **`Interact`** on that door opens it and shows the bad-end canvas; **`RevealBadEndingCanvas()`** plays a **non-spatial** gunshot via **`SoundManager.PlayOneShotNonSpatial`** when configured.
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
| Interact | *(check `InputSystem_Actions` — often E)* — **`PlayerController`** prefers a **viewport-center** ray from the gameplay **Camera** when assigned; optional **`interactRayOriginOverride`** for a custom pivot. |

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
