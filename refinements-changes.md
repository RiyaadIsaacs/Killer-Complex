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

## 2026-05-17 — Delivery urgency: start timer when leaving PC

- **Gameplay / UX:** **`DeliveryUrgencyTimer`** — after **H** posts for an active leg, the HUD countdown **waits** until the player **closes the computer session** (Esc / shutdown → **`ComputerTerminal.CloseTerminal`** → **`NotifyComputerSessionClosed`**). If **H**’s line arrives while the PC is **already** closed, **`TryStartCountdownAfterHMessage`** starts **immediately**. Lets players read and keep messaging **H** without the clock draining at the desk.
- **LLM context:** **`GetRemainingSecondsForLlmContext`** stays inactive until the countdown runs, so **`[CONTEXT]`** omits urgent seconds until after leave-PC (aligned with **`AppendStaticGameContextForLlm`** in **`OllamaConnector`**).
- **Code:** **`ComputerTerminal`** end of **`CloseTerminal`** calls **`DeliveryUrgencyTimer.NotifyComputerSessionClosed()`**; deferred state **`_awaitingComputerCloseToStartTimer`**; new leg prep clears stale defer flags.
- **Docs:** **`setup.md`** §2c, **`ollama-plan.md`** §4 + §8, this file, **`RiyaadWork.md`**.
- **AI-assisted:** Cursor; verify: **H** replies at PC → no countdown until exit; drop-off flow + step-away clears unchanged.

---

## 2026-05-15 — Timer reliability, maze fixes, step-away CONTEXT, bad-ending messenger defer, DDOL restart

- **Delivery urgency:** Second-leg and reorder fixes — **`TryStartCountdownAfterHMessage`** recovery when a leg is active but the HUD never armed; **`NotifyHPostedToMessenger`** does not drop **`TryStart`** when the suppress flag was meant for an older step-away reply but a job is already active; maze **`HideMazeUi`** → **`TryResumeDeferredCountdownAfterMazeClosed`**; shared **`TryCommitDeferredLegCountdown`** with **`NotifyComputerSessionClosed`**; stale step-away cannot **`StopCountdown`** while **`ActiveDropPointId >= 0`**.
- **Pause restart:** **`DeliveryManager.ResetRunStateForNewPlaySession`** ( **`StopAllCoroutines`**, reset leg/progress fields) from **`DeliveryUrgencyTimer`** on gameplay/menu load — DDOL manager no longer keeps **`currentDeliveryID`** across **`LoadScene`**; **`prepareFirstDeliveryAfterSceneTick`** defer is queued from that hook instead of **`Start()`** only.
- **Hacking maze:** Null **`_obstacle` / `_bomb`** at **`GenerateMaze`** start; bounds-safe **`IsHazardAt`**; second **`ApplyMazeChromeLayout`** + deferred **`LayoutRebuilder`** pass after reopen to fix fog/grid misalignment; **`IndexOutOfRangeException`** on tier change prevented.
- **Interact:** **`PlayerController`** — **`Camera.ViewportPointToRay(0.5,0.5)`** when the look transform has a **`Camera`**; optional **`interactRayOriginOverride`**.
- **LLM / UX:** Post-drop **step-away** turns — extra **`[CONTEXT]`** guard + system prompt clause so **H** does not assign a new unit/job in that message; **`AppendAndClear`** examples avoid “check the package” priming. **`stepAwayBeatThisTurn`** omits the active-leg paragraph even if state races.
- **Bad ending:** **`StartBadEnding(deferRestrictedDesktopUntilComputerClosed: true)` from **`OllamaConnector`** — **`ComputerDesktopUI`** arms shutdown-only layout on **`OnComputerSessionClosed`** so the trap **H** line is visible first; **`TriggerPlayerCaughtByTrap`** keeps **immediate** lock.
- **Docs:** **`setup.md`**, **`prompts-used.md`**, **`ollama-plan.md`** §8, **`RiyaadWork.md`**, this file — updated with this push.
- **AI-assisted:** Cursor; verify: restart from pause → timer budget resets; final trap line at PC → read messenger → leave desk → icons lock; maze reopen + multi-leg chat.

---

## 2026-05-15 — Main menu, pause menu, build settings

- **Scripts:** **`MainMenuScreen`** — **`PlayGame()`** loads **`Main Game`**; **`QuitGame()`** exits play / build. **`PauseScreen`** — **Esc** toggles pause (**`IsGameplayPaused`** static); **`Resume`**, **`RestartGame`**, **`GoToMainMenu`**, **`QuitGame`**; respects **`GameSceneIntroPanel.BlocksGameplay`**, open **`ComputerTerminal`**, and **`HackingMazeMinigame.TryConsumeEscape()`** so maze / PC get Esc first.
- **Scenes / build:** **`Assets/Scenes/Main Menu.unity`** added; **`ProjectSettings/EditorBuildSettings.asset`** — index **0** = Main Menu, **1** = Main Game.
- **Editor setup (manual):** Wire menu / pause buttons to public methods; keep **`pausePanel`** on an always-active parent so **`PauseScreen`** still receives Esc when the panel is hidden.
- **AI-assisted:** Cursor; verify: Play from main menu → gameplay; Esc pause/resume; restart / main menu / quit from pause panel.

---

## 2026-05-15 — Movement controls HUD (gameplay)

- **UI:** **`MovementControlsHud`** — runtime bottom-left panel (WASD move, **SPACE** jump, **SHIFT — toggle sprint**, **ESC — pause**); optional assigned panel or auto-built overlay canvas; hides while **`PauseScreen.IsGameplayPaused`** or **`GameSceneIntroPanel.BlocksGameplay`**.
- **Setup:** Add component on **`Temp-Player`** (or a **GameUI** empty) in **`Main Game`** only — not on Main Menu.
- **AI-assisted:** Cursor; verify: hints visible during play, hidden on pause / intro; sprint copy matches toggle behaviour on **`PlayerController`**.

---

## 2026-05-15 — Falling trap → bad ending

- **World:** **`TrapTriggerZone`** — hallway trigger spawns **`trapPrefab`** above the player (once or repeatable). **`FallingTrap`** — drops (kinematic move or **Rigidbody** gravity); bad ending when child **`TrapCatchZone`** (trigger collider) overlaps the player — not the root mesh collider.
- **Prefab layout:** **`TrapRoot` (`FallingTrap`)** → child **`TrapCatchZone`** (trigger + script). **`catchZone`** on **`FallingTrap`** can stay empty (auto-finds child).
- **Bad ending:** **`FallingTrap.OnPlayerCaught()`** → **`BadEndingOrchestrator.TriggerPlayerCaughtByTrap()`** (optional door/desktop setup + **`RevealBadEndingCanvas()`**); optional **`OllamaConnector`** trap line when deliveries are complete.
- **AI-assisted:** Cursor; verify: walk into trigger → trap falls → catch zone hits player → bad-ending canvas / knock flow as configured.

---

## 2026-05-15 — Hacking maze UI: full-panel overlay, status bar, Abort breach fix

- **Layout / host:** Maze dimmer + **`MazeBox`** now parent under **`PanelHackingTerminal`** (not **`ConsoleScrollView/Viewport`** or **`HackingTerminalContent`** — that object’s **`VerticalLayoutGroup`** was squashing the overlay and breaking clicks). Overlay uses **`LayoutElement.ignoreLayout`**; **`BringMazeOverlayToFront()`** keeps it above terminal chrome (controls dock is **not** forced on top).
- **While maze open:** **`ConsoleScrollView`** hidden so log text does not show through; restored on **`HideMazeUi`**. Maze box uses ~**97%** of terminal panel size (caps **~1120×860**); grid gets remaining height after title / button / status chrome.
- **Status line:** Bottom **light strip** + **dark text** (tier, hazards, vision, position) — replaces low-contrast footer on the dark panel; **`raycastTarget`** off so it does not block the button.
- **Abort breach:** Fixed non-clickable button — overlay reparent + dock / status bar / button label raycast fixes; **`btnRow`** minimum height preserved.
- **Code:** `HackingMazeMinigame` — **`EnsureOverlayOnMazeHost`**, **`SetConsoleScrollVisible`**, **`CreateMazeStatusBar`**, **`MazeChromeVerticalReserve`** / sizing tweaks.
- **AI-assisted:** Cursor; verify: **Hack** → large readable maze; status readable at bottom; **Abort breach** and **Esc** close without progress; controls dock still readable on the left.

---

## 2026-06-18 — Part 3: Joburg Game Dev Meetup refinements

**Feedback source:** Joburg Game Dev Meetup — see **`feedback-summary.md`**, **`critical-feedback.md`**.

- **Timer:** Per-leg base budget **160s** (`DeliveryUrgencyTimer`); meetup consensus (was 90s).
- **Objective HUD:** `DeliveryObjectiveHud` — find package / deliver room while leg active.
- **LLM pickup sync:** `DeliveryPickupSpawnPoint` + `DeliveryManager.CurrentPickupLocationLabel`; `OllamaConnector` CONTEXT uses authoritative location (removed lobby/reception default); system prompt rule against inventing lobby.
- **Settings:** `GameSettingsMenu` — **Esc → Pause → Settings** — mouse sensitivity via `PlayerPrefs` on `PlayerController`.
- **UX:** Center-screen package-delivered banner (`GlobalNotificationHud`); `DeliveryItem` spin/bob highlight.
- **Restart/build:** `GameplaySessionReset` on gameplay scene load — maze force-close, `ComputerDesktopUI.ResetForNewGameplaySession`, `OllamaConnector.ResetSessionStateForSceneLoad`; timer defer until PC closed after maze if desk still open.
- **Model:** Default **`llama3.2:3b`** (`OllamaConnector`, `setup.md`, `ollama-plan.md` §2).
- **Declined:** Tab-phone terminal (scope + computer-hub design) — documented in `critical-feedback.md`.
- **AI-assisted:** Cursor; verify in Editor/build: restart, objective HUD, settings, one full delivery leg with Ollama.

---

## 2026-06-18 (continued) — DDOL restart, pause/settings, LLM polish, timer at PC

**Follow-up after meetup push (`0c732e7`)** — playtest loop **Main Game → Main Menu → Main Game**, hacking messenger replies, and in-editor settings.

- **DDOL session reset:** `GlobalNotificationHud` merges scene `DeliveryManager` bindings (package + spawn points) before destroying duplicate HUD instances; `DeliveryZone` / `DeliveryCompletionChatNotifier` always register with persistent `DeliveryManager`; `RefreshDropPointRegistrationsFromScene()` on gameplay load; `OllamaConnector` + `ChatManager` re-resolve connectors/managers after reload; `GameplaySessionReset` clears transient HUD toasts.
- **Main menu retry bugs:** Fixed `MissingReferenceException` on destroyed `DeliveryItem`; `NullReferenceException` in `PrepareNextDeliveryFromAi`; false **Package Delivered** toast after restart (`ResetTransientNotificationsForSession`); intro coroutine on inactive HUD (`GameSceneIntroPanel` + `EnsureRootActiveForSession`).
- **Delivery objective HUD:** `DeliveryObjectiveHud` stacked under urgent timer; pickup copy **“Find the package in the complex.”** (no room); visible only while `DeliveryUrgencyTimer.IsCountdownActive`.
- **Timer at computer:** Active countdown **pauses** while any `ComputerTerminal` session is open (`NotifyComputerSessionOpened` / `Update` guard), not only deferred start on first **H** line.
- **Pause / settings:** `GameSettingsMenu` on pause canvas (mouse sensitivity via `PlayerPrefs`); `PauseMenuUiFactory` + editor **Killer Complex → UI → Setup Pause Settings Menu**; pause chrome hides when settings overlay open.
- **LLM / messenger:** Stronger **H** tone (greetings, destination apartment in orders, hidden pickup in CONTEXT only); `StripEchoedContextFromModelReply` + `NormalizeMessengerReplyForDisplay` (no `Job:` / `[CONTEXT]` leaks); **hostage pronouns** (`my wife` → `your wife` in prompt + post-process); `ChatManager` shows **bold `H:`** via TMP (`<noparse>` body).
- **Input System:** `BpsMouseInput` + BPS interact scripts use **Input System** mouse (`Mouse.current`) instead of legacy `Input.GetMouseButtonDown`.
- **Docs:** `prompts-used.md`, `ollama-plan.md`, `setup.md`, `README.md`, `feedback-summary.md`, `RiyaadWork.md`, `plan.md`, this file.
- **AI-assisted:** Cursor; verify: menu round-trip, messenger after hack, timer frozen at desk, settings from pause, one full delivery with Ollama.

---

## 2026-06-18 (polish) — Intro, computer UX, audio, main-menu load

**Meetup follow-up (Attendee C):** computer discoverability — scene **arrow hints** near desk; **`ComputerTerminal`** hides them on first interact; **`ResetDiscoveryHintsForNewSession`** on gameplay reload.

- **Intro copy:** `GameSceneIntroPanel.DefaultBody` — wife-missing opener + quoted **H** message; prefab + **Main Game** overrides aligned (`GlobalNotificationHUD.prefab`).
- **Computer HUD:** `GlobalNotificationHud.SetTopLeftNotificationsVisible` — **TopLeftNotifications** (timer, objective, delivery row) hidden while **`ComputerTerminal`** is open; restored on **`CloseTerminal`**.
- **Main Menu → Play:** `SceneNavigationUtility.PrepareForSceneLoad` clears Editor selection before **`LoadScene`** (avoids Inspector errors when Main Menu unloads); used by **`MainMenuScreen`** and **`PauseScreen`** scene changes. **`GlobalNotificationHud.MergeFromSceneInstance`** copies intro copy/player wiring from duplicate scene HUD.
- **Delivery SFX:** Successful **`DeliveryZone`** drop-off plays **`Door Knock.mp3`** at the door via **`SoundManager.TryPlayDoorKnockAt`** (fallback clip on **`ComputerDesktopCanvas`** **`SoundManager`**).
- **Walking SFX:** **`PlayerMovementAudio`** on player (auto-added from **`PlayerController`**) loops **`Walking.mp3`** while grounded and moving; pauses during intro/pause/computer; sprint pitch bump. Clip resolved from **`SoundManager.WalkingLoopClip`** on desktop canvas.
- **Assets:** `Assets/SFX/Walking.mp3`, `Assets/SFX/Door Knock.mp3` on **`SoundManager`** in **`ComputerDesktopCanvas.prefab`**.
- **Docs:** `setup.md`, `README.md`, `RiyaadWork.md`, `feedback-summary.md`, this file.
- **AI-assisted:** Cursor; verify: deliver at door (knock), walk/sprint loop, PC session hides top-left HUD, Main Menu → Play (Editor + build).

---

## 2026-06-18 (settings & audio) — Mouse/SFX sliders, door SFX, editable settings panel

**Follow-up:** meetup **mouse sensitivity** still too high at minimum; request for **master SFX volume**; settings UI overlap/readability in pause overlay.

- **Mouse sensitivity:** `PlayerController` base **40**; slider range **0.1×–2×** (`MouseSensitivityMultiplier` in `PlayerPrefs`); fixed mismatch where UI allowed lower values but **Apply** clamped to **0.5×** minimum.
- **SFX volume:** `SoundManager.SfxVolume` master scale (**0–100%**, `SfxVolume` in `PlayerPrefs`) applied to notifications, world one-shots, walking loop, ending stingers.
- **Door open/close:** `InteractDoor.MoveDoor()` plays **`Open Door.mp3`** / **`Close Door.mp3`** via `SoundManager.TryPlayDoorOpenCloseAt` (per-door or desktop canvas fallbacks).
- **Settings UI:** `GameSettingsMenu` — second slider for SFX; light-on-dark label colours; bottom-anchored layout (sliders above Apply/Back); scene-persistent panel via **Killer Complex → UI → Rebuild Pause Settings Panel**; `PauseMenuSettingsSetup` wires serialized refs for Inspector editing.
- **Assets:** `Assets/SFX/Open Door.mp3`, `Close Door.mp3` on **`ComputerDesktopCanvas`** **`SoundManager`**.
- **Docs:** `README.md`, `setup.md` §3d, `feedback-summary.md`, `RiyaadWork.md`, `prompts-used.md`, this file.
- **AI-assisted:** Cursor; verify: pause settings Apply, min mouse sens, SFX mute/low, door toggle sounds, rebuild panel + save scene.

---

## 2026-06-18 (menu UI polish) — Title screen + settings button art

**Feedback-driven follow-up:** meetup **mouse sensitivity / settings** access (see **`feedback-summary.md`**, **`critical-feedback.md`**) plus clearer **first-run branding** on the title flow.

- **Main menu screen:** **`Main Menu.unity`** — new full-screen **`Main Menu screen`** canvas; **`Panel`** uses baked title art **`Assets/Prefabs/killer complex title screen.png`** (game title visible on load). **Play Game** / **Quit Game** keep baked sprite labels with blank **TMP** children (same pattern as pause buttons).
- **Pause settings button:** **`Main Game.unity`** — **Settings** child under **Pause Menu** (between **Restart** and **Main Menu**); new baked sprite **`Assets/Prefabs/Gemini_Generated_Image_ajxdouajxdouajxd.png`** on the button **Image**; **TMP** label left blank (label in art only, matching **Resume** / **Restart** / **Main Menu**).
- **Wiring:** **Settings** → **`PauseScreen.OpenSettings`**; **`GameSettingsMenu`** on pause panel; **`PauseMenuContent`** references **`settingsMenu`** in the Inspector.
- **Code:** **`PauseScreen`** / **`PauseMenuSettingsSetup`** no longer overlay a runtime **"Settings"** TMP string on scene buttons; **`PauseMenuSettingsSetup.SetupMainGamePauseSettingsBatch`** for batch scene setup when Unity is closed.
- **Manual / hand-authored:** Title screen layout and button art in Editor; save **`Main Menu`** + **`Main Game`** scenes after changes.
- **Verify:** Main menu shows title art; **Esc → Pause → Settings** opens overlay; button reads clearly at 1920×1080; no duplicate TMP label over sprite.

---

## 2026-06-18 (maze + delivery toast) — Playtest UX follow-up

**Feedback-driven:** meetup **objective clarity after closing the PC** (Attendee C — see **`feedback-summary.md`**, **`critical-feedback.md`**); playtest fixes for **maze readability**, **Abort breach**, and **H catching mid-hack**.

### Hacking maze

- **Layout / readability:** Maze overlay parents to **`PanelHackingTerminal`** (avoids **`VerticalLayoutGroup`** squashing on **`HackingTerminalContent`**). Larger **`MazeBox`** / grid sizing; bottom **light status strip** with dark text; **`ConsoleScrollView`** hidden while the maze is open.
- **Abort breach:** Raycast / sibling / reparent fixes so **Abort breach** is clickable again; **`LayoutElement.ignoreLayout`** + **`BringMazeOverlayToFront()`** on the overlay host.
- **H returns mid-breach:** **`HackingRemoteAccessController`** — any **H** messenger line **except** “Remote access established” calls **`ComputerDesktopUI.RevokeRemoteAccess()`** (hide **HACKING**, close terminal panel). If the maze is open: **`HackingMazeMinigame.ForceCloseBecauseHReturned()`** / **`AbortBecauseHReturned()`** (no decryption % gain); **`OllamaConnector.ApplySuspicionIncrementForCaughtHacking()`** (default **+10%**, **`suspicionPerCaughtHacking`**); caught-hacking line appended to **H**’s reply when needed. Remote hacking available again only after **H** steps away (“Remote access established”), as before.
- **Code:** `HackingMazeMinigame`, `HackingRemoteAccessController`, `ComputerDesktopUI`, `ChatManager` (remote-access flag on **`UpdateChatFeed`**), `OllamaConnector`.

### Delivery room notification (leave PC)

- **UX:** Center-screen banner **`Deliver to Room {N}`** when the player **closes the computer session** with an active delivery leg — so the destination is obvious after reading **H** at the desk, without re-opening messenger.
- **Timing:** Fired from **`ComputerTerminal.CloseTerminal`** → **`DeliveryUrgencyTimer.NotifyComputerSessionClosed()`** → **`DeliveryManager.AnnounceDestinationForActiveLegIfNeeded()`**. **Not** shown on job assign or when **H** first posts while still at the PC.
- **Copy:** Destination **room number only** (e.g. **Room 204**) — not the full list of valid apartments.
- **Dedup:** One toast per active drop point per leg (**`_destinationAnnouncedForDropPointId`**).
- **Code:** `DeliveryManager`, `GlobalNotificationHud.ShowDeliveryDestinationAnnouncement`, `DeliveryUrgencyTimer`; removed announce calls from **`PrepareNextDeliveryFromAi`** and **`ChatManager`** on **H** post.
- **AI-assisted:** Cursor; verify: **H** assigns job at PC → leave desk → room toast; no toast while still at PC; maze **Abort breach** + **Esc**; **H** returns during maze → breach closes + suspicion bump; hack locked until remote access again.

---
