# Refinements & changes log

Dated log of **scope**, **design**, and **implementation** changes. Mention **AI-assisted** work honestly (Cursor, cloud LLMs, etc.) and what you verified manually.

---

## 2026-05-11

- **Docs bootstrap:** Added `plan.md`, `rules.md`, `setup.md`, `refinements-changes.md`, `prompts-used.md`, `RiyaadWork.md`, expanded `README.md`; `ollama-plan.md` already present from earlier pass.
- **Alignment:** Documented Part 2 direction — Ollama blackmailer persona, delivery loop, three floors, pickups (window / entrance / rooftop), computer hacking beat, dual endings (delivery quota vs escape).
- **Unity version:** README now matches `ProjectSettings/ProjectVersion.txt` (**6000.3.15f1**).
- **Code:** No gameplay/Ollama C# integration yet — pending implementation milestone.

---

## 2026-05-12

- **Scenes:** `SampleScene` removed; **`Main Game.unity`** is the main slice; **`Tester scene.unity`** added as a dedicated scene for exercising new code (player, interact, computer UI) without loading the full environment.
- **Gameplay / UI (AI-assisted, hand-tested in Editor):** `InteractDoor`, `ComputerTerminal`, `PlayerController` interact fixes, `ComputerDesktopUI` + Editor-generated desktop (MESSENGER / DELIVERIES), `ChatManager`, `DeliveryManager`, TMP package, `ComputerDesktopCanvas` prefab workflow.
- **Docs:** `RiyaadWork.md`, `prompts-used.md`, `setup.md`, `README.md` updated alongside this push.

---

## 2026-05-12 (later) — Ollama context + docs push

- **LLM:** New **`OllamaConnector`** (`POST` **`/api/generate`**, default model field `mistral:7b-instruct`). Each player message builds a prompt turn with a hidden **`[CONTEXT: …] Player says: …`** prefix (context not duplicated in visible chat). Optional **`DeliveryManager`** link. *(Original shipped field was **likeability** %; superseded **2026-05-14** by a **suspicion** meter — see entry **“Suspicion meter (replaces likeability)”** below.)*
- **UI wiring:** **`ChatManager`** optional serialized **`OllamaConnector`** — on send, calls **`SendToOllama`** after appending the player line.
- **Docs / compliance:** `ollama-plan.md` (data flow §4, prompt §5, changelog), `setup.md` §3, `README.md` (getting started + stack), `prompts-used.md` (exact system + context template), `RiyaadWork.md` — updated before push per `rules.md`.
- **AI-assisted:** Cursor authored C# + markdown; verify in Editor: references assigned, Ollama running, model pulled.

---

## 2026-05-13 — Persona **H**, messenger UX, docs

- **LLM persona:** **`OllamaConnector`** system prompt retargeted from **V** to **H** *(historical 2026-05-13 framing: hacker antagonist, Project_Bleed escalation — superseded **2026-05-15** by kidnapper / hostage narrative; see **`prompts-used.md`**)*. Replies and errors use **`[H]: …`** in the feed (`HackerSenderLabel`).
- **Messenger:** **`ChatManager`** — scripted opening intro on first UI enable; **`ShowTypingIndicator` / `HideTypingIndicator`** (optional pulsing `TMP_Text` or feed fallback); ref-counted for overlapping requests; defaults **`introSenderName`** / typing copy aligned to **H**.
- **Docs:** `prompts-used.md` (dated **H** system string + archived **V**), `ollama-plan.md` (persona **H**, data flow, prompt summary), `README.md`, `setup.md`, `plan.md`, `refinements-changes.md`, `RiyaadWork.md` updated for this push.
- **AI-assisted:** Cursor; verify in Editor: Ollama wired, model pulled, chat send + typing + **H** replies.

---

## 2026-05-13 — Deliveries, messenger gating, HUD, computer UI

- **Deliveries:** `DeliveryManager` — removed assignment TMP / debug complete button; optional **physical pickup** via `DeliveryItem.Interact` before `DeliveryZone` accepts drop-off when a reception item is set; **drop-off id → apartment room** map (**0–6** → **201–208**) for LLM + docs; default **no** auto-first-delivery on scene start.
- **Messenger:** `ChatManager` — opening intro **without** errands by default; **first player SEND** calls `PrepareNextDeliveryFromAi` and optional scripted **H** follow-up (lobby/package); optional explicit `DeliveryManager` or auto-find. `DeliveryCompletionChatNotifier` still prepares the next leg after scripted completion lines when enabled.
- **HUD / editor:** `GlobalNotificationHud` + `GlobalNotificationHudCreator` — package-delivered row uses **Image** parent + **Text** child (one Graphic per object); **Repair Package Delivered Label (add TMP)** menu. `DeliveryZone` resolves HUD label via manager on shared root when unassigned.
- **Computer UI:** `ComputerDesktopUI` + `ComputerDesktopUICreator` — canvas **pixel perfect** + **additional shader channels** for sharper TMP.
- **LLM:** `OllamaConnector` hidden context includes **drop id**, **room**, **pickup**; system prompt instructs model to honour **apartment room** in context.
- **Docs / compliance:** `README.md`, `setup.md`, `ollama-plan.md`, `prompts-used.md`, `refinements-changes.md`, `RiyaadWork.md` updated for this push.
- **AI-assisted:** Cursor; verify in Editor: first message rolls delivery + context, zones use ids **0–6**, interact pickup + zone **E**, Ollama replies.

---

## 2026-05-14 — Delivery pacing, interact drop-off, LLM context, HUD

- **Delivery pacing:** `DeliveryManager` no longer calls `PrepareNextDeliveryFromAi` automatically when a leg completes; **`ChatManager`** calls it on **each** messenger SEND while **`ActiveDropPointId < 0`** and runs remain (`prepareDeliveryOnMessengerSendWhenIdle`, with **`FormerlySerializedAs`** for the old Inspector field). Ties the next job to **talking to H** before pickup/destination exist again.
- **Completion flow:** `TryCompleteDeliveryAtDropPoint` clears the active leg **before** `OnDeliveryCompleted`; `CompleteCurrentDeliveryStep` no longer chains an immediate second prepare. **`DeliveryCompletionChatNotifier`** posts scripted chat only (no longer prepares the next leg).
- **Interact drop-off:** `DeliveryZone` uses **`Interact`** + **`InteractPromptResolver`**; **`GlobalNotificationHud.ShowDeliveryFeedback`** for success/failure toasts.
- **LLM:** `BuildPlayerTurnForPrompt` uses **prose** `[CONTEXT]` (valid apartment list + destination); system prompt discourages invented units and echoing placeholder-like tokens. **`TotalDeliveryLegs`** on `DeliveryManager` drives quota in **`OllamaConnector`** when linked.
- **Docs:** `README.md`, `setup.md`, `ollama-plan.md`, `prompts-used.md`, `refinements-changes.md`, `RiyaadWork.md` updated for this push.

---

## 2026-05-14 — Hacking maze breach minigame + world **[E]** prompts

- **Scripts touched / added:** `HackingMazeMinigame.cs`, `HackingTerminalPanel.cs`, `ComputerTerminal.cs`, `PlayerController.cs`, `InteractPromptHud.cs`, `InteractPromptResolver.cs`, `Interactable.cs`, `DeliveryManager.cs`, `DeliveryZone.cs`.
- **Hacking terminal → maze:** **`HackingTerminalPanel`** opens a procedural **maze breach** via **`HackingMazeMinigame`** (added at runtime with **`AddComponent`** on the same object as the panel if missing — not serialized on **`ComputerDesktopCanvas.prefab`** by default). **Hack** starts a run; reaching the **green exit** calls **`ApplyMazeRoundWin()`**, which adds **`mazeWinProgressPercent`** (default **10**) to the decryption slider; at **100%** the existing **`OnHackSuccessful()`** path runs (events, **`OllamaConnector.SendHackReversalPrompt`** when assigned).
- **Maze rules / difficulty:** **Tier** from current slider / win step (**`GetMazeTier()`**). Maze size grows with tier; **obstacles** (orange, impassable) and **bombs** (red, fail the run with no % gain) scale up. **Loop carving** after the perfect maze adds **alternate routes**; hazards use a **blended placement** (shortest-path vs longer-path cells, **`corridorHazardBias`**) so they sit on competing paths, not one obvious line. **BFS** keeps a valid bomb-free route whenever hazards are kept.
- **Controls:** **WASD / arrows**, **hold** to repeat steps (**`holdInitialDelay`**, **`holdRepeatInterval`**); **Esc** closes the maze first via **`HackingMazeMinigame.TryConsumeEscape()`** from **`ComputerTerminal`** before exiting the whole computer UI.
- **Maze UI:** Runtime-built overlay (title, live status line, grid, **Abort breach**). **Controls / how-to-play** were moved to a **left dock** beside the terminal (see entry **“Hacking maze: controls dock beside terminal”** below). Panel scaled for readability (**~960×780** box, larger grid min/preferred heights, higher TMP sizes for title / status / button); maze cell layout fallback and **minimum cell pixel size** increased so tiles stay legible.
- **World interact prompts:** **`InteractPromptHud`** (auto-added on **`PlayerController`** if absent) — per-frame **`Offer`** + **`LateUpdate`** picks highest priority. **`PlayerController`** shares **`TryGetInteractRay`** with **`TryInteract`** and calls **`InteractPromptResolver.TryResolve`**. **`Interactable`** optional component overrides label + anchor. Built-in labels for computer, door, package, and **`DeliveryZone.TryGetWorldInteractPrompt`** (deliver / wrong apartment / get package first / generic door). **`DeliveryManager`**: **`CanInteractDropPoint`**, **`GetDeliveryDropFailureReason`**, and **`TryCompleteDeliveryAtDropPoint`** aligned for prompts + **`Interact`** drop-off.
- **AI-assisted:** Cursor; verify in Editor: open hacking app → **Hack** → maze readable, hold-move + hazards, % advances per clear; ray **[E]** prompts near computer / doors / packages / zones; Esc in maze vs Esc closing PC.

---

## 2026-05-14 — Hacking maze: controls dock beside terminal

- **UX:** **Controls** and **How to play** no longer stack under the maze inside the overlay (they were clipping). They now live in a **`_controlsDockRoot`** strip **to the left** of the hacking terminal panel (**sibling under the terminal’s parent**, **`LayoutControlsDock()`** sizes and anchors it using **`ControlsDockWidth`** / **`ControlsDockGap`**). **`MazeChromeVerticalReserve`** was reduced so **`MazeBox`** keeps more height for title, status, grid, and **Abort breach**.
- **Code:** `HackingMazeMinigame` — **`CreateControlsSection(..., dockOutsideTerminal: true)`** for the dock; inner **`VerticalLayoutGroup.childForceExpandHeight`** when docked so body text fills the strip; **`HideMazeUi`** disables dock + overlay; **`OnDestroy`** destroys **`_controlsDockRoot`**.
- **Docs:** `README.md`, `setup.md`, `refinements-changes.md` (this entry), `RiyaadWork.md`, `prompts-used.md` (Cursor session note). **`ollama-plan.md`:** no prompt or API contract change.
- **AI-assisted:** Cursor; verify in Editor: start maze → instructions readable on the **left** of the terminal; maze grid not squashed by footer text.

---

## 2026-05-14 — Desktop messenger toast + notification SFX

- **UI:** `DesktopMessengerNotification.cs` — in-desktop toast for new **H** activity (HUD-style row, optional Animator or default pop). **`ComputerDesktopUICreator`** can add **`SoundManager`** + **`DesktopMessengerNotification`** on the desktop canvas root; **`OllamaConnector`** optional serialized reference / instance lookup and **`MaybeTriggerDesktopMessengerNotificationAfterHReply`** after model replies.
- **Audio:** `Assets/Scripts/Audio/SoundManager.cs` — **`PlayNotificationSound()`** for a configurable clip; **`Assets/SFX/`** includes a notification **MP3** import (assign on **`SoundManager`** in the scene / prefab).
- **AI-assisted:** Cursor; verify in Editor: clip assigned, toast shows when **H** replies (and SFX plays if wired).

---

## 2026-05-15 — Narrative pivot: kidnapper **H**, Wife status in CONTEXT

- **Narrative:** **`OllamaConnector`** — **H** reframed as a **kidnapper** holding the player’s **wife** hostage (fiction), **security cameras**, orders not jokes, **clinical** escalation on delivery delay/failure, **bru** / **wena**; rude player → **hostage threat** (removed **Project_Bleed**). Serialized **`wifeStatusForLlmContext`** + **`WifeStatusForLlmContext`** property append **Wife status** into hidden **`[CONTEXT]`** after suspicion when non-empty.
- **Messenger:** **`ChatManager`** default **`openingMessage`** and **`ComputerDesktopCanvas.prefab`** intro copy aligned to the lobby package / wife leverage beat.
- **Full hack:** **`SendHackReversalPrompt`** visible **`[SYSTEM]`** line and narrative retargeted to **uplink / surveillance** counter-leverage (no photo-blackmail beat).
- **Docs:** **`prompts-used.md`**, **`ollama-plan.md`**, **`README.md`**, **`plan.md`**, **`setup.md`**, **`refinements-changes.md`**, **`RiyaadWork.md`**.
- **AI-assisted:** Cursor; verify in Editor: Ollama replies stay in tone; tune **Wife status** text for your comfort / marker requirements.

---

## 2026-05-14 — Suspicion meter (replaces likeability)

- **LLM / design:** **`OllamaConnector`** — **`SuspicionPercent`** (0–100, Inspector default 0 in scenes) replaces **`likeabilityPercent`**; hidden context says **“Suspicion is …%.”** Serialized **`suspicionPerIgnoredMazeAttempt`** scales how much suspicion rises when the ignore-delivery maze beat applies (0 disables increment and merge hints).
- **Trigger (initial ship):** After the **breach-count gate**, **`ApplySuspicionIncrementForIgnoredMazeAttempt()`** then **`NotifyMazeBreachRoundAttemptFinished(..., mergeIgnoreDeliveryOrderIntoMazeReply)`** — **one** generate; **`ChatManager`** **`NotifyPlayerMessengerSend`** / **`NotifyHPostedToMessenger`**. *(Earlier doc text described a separate **`BuildSuspicionIgnoreNudgePrompt`** coroutine — removed **2026-05-16**.)*
- **Desktop toast:** Maze replies use **`_pendingMazeRoundOutcomeDesktopToast`** only (suspicion-specific toast flag removed).
- **Docs:** `ollama-plan.md`, `prompts-used.md`, `RiyaadWork.md`, `refinements-changes.md` (this entry).
- **AI-assisted:** Cursor; verify in Editor: after **N** gated breach runs (**`mazeBreachesBeforeMessengerJob`**), one **H** line; suspicion rises when delivery ignored.

---

## 2026-05-28 — Bad ending sequence, door audio, canvas prefab

- **LLM / gameplay:** **`OllamaConnector`** — **`TryBuildBadEndingPlayerTurn`** when **`PostDeliveryStepAwayBeatPending`** and **`currentDeliveryID >= TotalDeliveryLegs`**; hidden **`[SYSTEM]`** trap beat (**Room 204** package fiction); **`InteractDoor.CloseMarkedApartmentDoorsForBadEnding`** + **`BadEndingOrchestrator.StartBadEnding()`** (or door-only path if orchestrator missing). No **`Remote access established`** on this reply; beat consumed after successful generate.
- **World / UI:** **`BadEndingOrchestrator`** — knock **repeat** interval (default **8 s**), **`RevealBadEndingCanvas()`** + gunshot; **`InteractDoor`** — **my apartment door**, **3D** knock sequence, **`SoundManager.PlayOneShotWorld`** / **`PlayOneShotNonSpatial`**; extended **`SoundManager`** (door knock fallback, static one-shots).
- **Editor:** **`BadEndingCanvasCreator`** — **Tools → Killer-Complex → Create Bad Ending Canvas Prefab** → **`Assets/Prefabs/BadEndingCanvas.prefab`**.
- **Docs:** **`setup.md`** §3b, **`ollama-plan.md`** §4 + §8, **`prompts-used.md`** (bad-ending **`[SYSTEM]`** block), **`README.md`**, **`RiyaadWork.md`**, this file.
- **AI-assisted:** Cursor; verify in Editor: assign knock + gunshot clips, orchestrator + canvas, **My apartment door** on the unit **`InteractDoor`**.

---

## 2026-05-15 — Maze fog of war + random uplink goal

- **Gameplay:** `HackingMazeMinigame` — **`IsCellVisible`** / **`GetRevealedCellColor`** limit the grid to a **3×3** window around the player (**`visionRadius`**, default **1**); unrevealed cells use **`fogColor`**. The green goal is only drawn when inside that window.
- **Goal placement:** **`PickRandomGoalCell()`** runs after **`CarveRandomLoopPassages()`** and before **`PlaceHazards()`** — random walkable cell with minimum BFS distance from start (**`minGoalDistanceFromStart`**, default **6**). **`IsHazardAt`** null-safe for BFS before hazards exist.
- **UX copy:** Controls dock and live hint mention limited vision and finding the uplink.
- **Docs:** `setup.md` §2b, `README.md`, `prompts-used.md`, `RiyaadWork.md`, this file.
- **AI-assisted:** Cursor; verify in Editor: start maze → only nearby tiles visible; green node appears when explored; goal position changes between runs.

---
