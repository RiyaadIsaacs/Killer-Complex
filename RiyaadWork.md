# Individual contribution log — Riyaad

**Module:** GADS7331 — Game Design 3A (Part 2 + Part 3)  
**Repo:** Killer Complex (pair project — partner should maintain a parallel file)

---

## How to use

- Add a **dated section** when you work a session.
- List **concrete** items: files touched, scenes built, prompts tuned, docs written, bugs fixed.
- Keep it factual for peer review / lecturer dispute resolution.

---

## 2026-05-11

- **Documentation:** Authored/updated team docs: `plan.md`, `rules.md`, `setup.md`, `refinements-changes.md`, `prompts-used.md`, `RiyaadWork.md` (this file); coordinated content with existing `ollama-plan.md` and expanded `README.md`.
- **Design alignment:** Helped consolidate POE direction (Ollama-heavy computer hub, delivery loop, dual endings, ethical guardrails) into planning docs.
- **Code:** No C# / Unity feature implementation this session.

---

## 2026-05-12

- **Scenes:** Primary slice scene **`Assets/Scenes/Main Game.unity`**; added **`Assets/Scenes/Tester scene.unity`** as a lightweight scene for exercising new scripts (player, doors, computer UI) without the full apartment layout. Removed unused `SampleScene` from the repo (replaced by Main Game).
- **Player / interaction:** `PlayerController` — interact ray debug, `SendMessageUpwards("Interact")`, `Interact` action behaviour aligned with Input System SendMessages + `InputValue`; package `com.unity.textmeshpro` in `Packages/manifest.json`.
- **World / UI scripts:** `InteractDoor` (hinge + optional animator disable), `ComputerTerminal` (disable player, show canvas, Escape), `ComputerDesktopUI` + Editor builders for overlay desktop, `ChatManager`, `DeliveryManager`, `MessengerChatUIBuilder`, `DeliveryPanelUIBuilder`; prefab `Assets/Prefabs/ComputerDesktopCanvas.prefab` (regenerate via **GameObject → Computer Desktop Canvas** after TMP essentials import).
- **Docs pushed with code:** Updated `RiyaadWork.md`, `prompts-used.md`, `setup.md`, `README.md`, `refinements-changes.md` to match repo state.
- **Ollama + context (same day, follow-up):** `OllamaConnector.cs` — local **`/api/generate`**, system prompt for **V**, hidden **`[CONTEXT: …] Player says:`** line (delivery progress from optional `DeliveryManager`; rapport was later replaced by a **suspicion** meter — see **2026-05-14 — Suspicion meter** below); `ChatManager` optional reference to trigger sends; team docs (`ollama-plan.md`, `setup.md`, `README.md`, `refinements-changes.md`, `prompts-used.md`) updated and pushed to GitHub.

---

## 2026-05-13

- **LLM / UI:** `OllamaConnector.cs` — persona **H** *(2026-05-13: hacker tone + **Project_Bleed** — later **2026-05-15** kidnapper / hostage pivot; see **`prompts-used.md`**)*; `UpdateChatFeed` sender **`H`**. `ChatManager.cs` — opening intro line on first enable; typing indicator (`ShowTypingIndicator` / `HideTypingIndicator`, optional `TMP_Text` pulse or feed line); defaults **H** for intro + typing copy; `BuildTypingFeedLine` uses `introSenderName`.
- **Documentation:** `prompts-used.md` (2026-05-13 **H** prompt + archived **V**), `ollama-plan.md` (**H** persona / flow / fallback), `README.md`, `setup.md` (typing + **H**), `plan.md` (persona **H**), `refinements-changes.md`, `RiyaadWork.md` (this entry).
- **Pushed:** Commit to GitHub with the above files and script changes.

---

## 2026-05-13 (documentation + delivery pipeline push)

- **Deliveries & chat gate:** `DeliveryManager` — pickup required before zone when `DeliveryItem` set; id→room **0–6**→**201–208**; no scene-start first job by default. `ChatManager` — intro without package copy; first player send prepares job + scripted **H** follow-up. `DeliveryItem` — `Interact` pickup. `DeliveryZone` / HUD / editor repair menu aligned with TMP-on-child pattern.
- **LLM:** `OllamaConnector` — `[CONTEXT: …]` includes drop id, apartment room, pickup; system prompt references room authority.
- **Computer UI:** Crisp TMP canvas settings on `ComputerDesktopUI` / creator prefab.
- **Documentation:** `README.md`, `setup.md`, `ollama-plan.md`, `prompts-used.md`, `refinements-changes.md`, `RiyaadWork.md` (this entry) updated; committed and pushed to GitHub.

---

## 2026-05-14

- **Documentation & delivery pipeline:** Updated `README.md`, `setup.md`, `ollama-plan.md`, `prompts-used.md`, `refinements-changes.md` for messenger-gated next legs, **`TotalDeliveryLegs`**, interact **`DeliveryZone`**, prose **`[CONTEXT]`**, and HUD toasts; aligned with current `ChatManager` / `DeliveryManager` / `OllamaConnector` behaviour.
- **Pushed:** Commit to GitHub with code + docs from this session.

---

## 2026-05-14 (later) — Hacking maze controls dock + docs sync

- **UI:** `HackingMazeMinigame.cs` — **controls / instructions** moved to a **left dock** outside the terminal viewport; **`LayoutControlsDock`**, **`HideMazeUi`**, **`OnDestroy`** cleanup for **`_controlsDockRoot`**; layout tweaks for expanded body text in the dock.
- **Documentation:** `refinements-changes.md` (new subsection), `README.md` (getting started note), `setup.md` (**§2b**), `prompts-used.md` (Cursor session row), this file.
- **Git:** Pulled **`origin/main`** (partner **`refinements-changes.md`** update) before committing and pushing with the rest of the session’s tracked changes.

---

## 2026-05-14 (audio / messenger toast)

- **Added:** `DesktopMessengerNotification.cs`, `SoundManager.cs`, `Assets/SFX/` notification clip + meta; **`refinements-changes.md`** entry for this feature.
- **Pushed:** Commit to GitHub (previously untracked assets now tracked).

---

## 2026-05-14 — Suspicion meter (replaces likeability)

- **LLM / gameplay:** `OllamaConnector.cs` — **`SuspicionPercent`** (0–100, starts at 0 in scenes), serialized **`suspicionPerIgnoredMazeAttempt`**; **`[CONTEXT: …]`** reports **Suspicion is …%** instead of likeability. *(Initial implementation used a separate suspicion **`/api/generate`**; **2026-05-16** merged ignore-delivery into **`NotifyMazeBreachRoundAttemptFinished`** — see entry below.)* **`ChatManager`** calls **`NotifyPlayerMessengerSend`** / **`NotifyHPostedToMessenger`**; after the breach-count gate, **`ApplySuspicionIncrementForIgnoredMazeAttempt`** + **`NotifyMazeBreachRoundAttemptFinished(..., mergeIgnore…)`**.
- **Documentation:** `ollama-plan.md`, `prompts-used.md`, `refinements-changes.md`, this file; historical **likeability** references in older log lines left annotated where relevant.
- **Pushed:** Commit to GitHub with C# / scene updates and doc refresh.

---

## 2026-05-15 — Kidnapper narrative + Wife status in LLM context

- **LLM / copy:** `OllamaConnector.cs` — **kidnapper** system prompt (wife hostage, cameras, clinical escalation, **bru**/**wena**); **`wifeStatusForLlmContext`** appended to **`[CONTEXT]`**; **`SendHackReversalPrompt`** retuned to uplink/surveillance fiction. **`ChatManager.cs`** + **`ComputerDesktopCanvas.prefab`** — new opening messenger line (lobby package, wife leverage).
- **Documentation:** `prompts-used.md`, `ollama-plan.md`, `README.md`, `plan.md`, `setup.md`, `refinements-changes.md`, this file.
- **Pushed:** Commit to GitHub with doc refresh and narrative-aligned code.

---

## 2026-05-16 — Merged maze suspicion + hacking icon gate

- **LLM:** **`OllamaConnector`** — removed second **`/api/generate`** for ignore-delivery; **`ApplySuspicionIncrementForIgnoredMazeAttempt()`** bumps **`SuspicionPercent`** then **`NotifyMazeBreachRoundAttemptFinished(..., mergeIgnoreDeliveryOrderIntoMazeReply)`** folds **`["player ignores the delivery order"]`** into the maze-outcome prompt. **`_pendingSuspicionIgnoreDesktopToast`** / **`BuildSuspicionIgnoreNudgePrompt`** removed.
- **Maze gate:** **`HackingTerminalPanel`** — **`mazeBreachesBeforeMessengerJob`** minimum **2**; **`RunMazeRoundOllamaHooks`** applies suspicion then single notify. Prefab **`mazeBreachesBeforeMessengerJob: 2`**.
- **Desktop UI:** **`ComputerDesktopUI`** — hacking dock icon **hidden** until **`NotifyRemoteAccessEstablished`** (after post–drop-off reply + **`Remote access established`** line); **`ComputerTerminal`** **`OnComputerSessionOpened` / `OnComputerSessionClosed`**.
- **Docs:** **`setup.md`**, **`ollama-plan.md`** §8, **`prompts-used.md`** (suspicion section goal), **`refinements-changes.md`** (this entry + **2026-05-14** bullet refresh), **`RiyaadWork.md`**.
- **AI-assisted:** Cursor; verify in Editor: second gated breach → **one** **H** message; icon appears only after remote-access line; leaving PC hides icon.

---

## 2026-05-28 — Bad ending, spatial knocks, canvas + audio

- **LLM / gameplay:** **`OllamaConnector.TryBuildBadEndingPlayerTurn`** — hidden **`[SYSTEM]`** final-trap prompt after all **`TotalDeliveryLegs`** complete; **`BadEndingOrchestrator`**, **`InteractDoor`** (marked apartment door close + knock bursts + repeat), **`RevealBadEndingCanvas`** gunshot (**`SoundManager.PlayOneShotNonSpatial`**).
- **Audio:** **`SoundManager`** — **`PlayOneShotWorld`** (3D knocks at door), **`PlayOneShotNonSpatial`** (UI/cutscene stinger), optional **door knock** fallback clip.
- **Editor / assets:** **`BadEndingCanvasCreator`** menu item for **`BadEndingCanvas`** prefab (user assigns SFX clips under **`Assets/SFX`**).
- **Documentation:** **`setup.md`** §3b, **`ollama-plan.md`**, **`prompts-used.md`**, **`refinements-changes.md`**, **`README.md`**, this file.
- **Pushed:** Commit to GitHub with code + doc refresh.

---

## 2026-05-15 — Maze fog of war + random goal

- **Gameplay:** `HackingMazeMinigame.cs` — **3×3 vision** (`visionRadius`, `fogColor`); **`PickRandomGoalCell()`** randomizes green uplink placement with **`minGoalDistanceFromStart`**.
- **Documentation:** `setup.md` §2b, `README.md`, `refinements-changes.md`, `prompts-used.md`, this file.
- **Pushed:** Commit to GitHub with maze changes and doc refresh.

---

## 2026-05-17 — Urgency timer starts when leaving computer

- **UX / systems:** **`DeliveryUrgencyTimer`** defers per-leg countdown until **`ComputerTerminal.CloseTerminal()`** if **H** posted while the desk session was open; **`NotifyComputerSessionClosed`**; immediate start if PC already closed.
- **Documentation:** **`setup.md`** §2c, **`ollama-plan.md`** (CONTEXT note + changelog), **`refinements-changes.md`**, this file.

---

## 2026-05-15 — Timer/maze/bad-ending follow-ups (DDOL restart, trap message UX)

- **Systems:** Urgency timer second-leg + suppress reorder + maze **`TryResumeDeferredCountdownAfterMazeClosed`**; **`DeliveryManager.ResetRunStateForNewPlaySession`** on load (pause restart); maze **`IsHazardAt`** / layout refresh; **`PlayerController`** viewport interact ray.
- **LLM / UX:** Step-away **CONTEXT** hardening; bad-ending trap defers **`ComputerDesktopUI`** lock until **`CloseTerminal`** so **H**’s final line shows in messenger.
- **Documentation:** **`setup.md`**, **`prompts-used.md`**, **`ollama-plan.md`**, **`refinements-changes.md`**, this file; pushed with code.

---

## 2026-06-18 — Part 3: Joburg Game Dev Meetup feedback integration

- **Event:** Joburg Game Dev Meetup (lecturer-approved) — live demo to 3 external attendees (3D artist, hobbyist, programmer); feedback captured in **`feedback-summary.md`**; critical analysis in **`critical-feedback.md`**; reflection scaffold **`part3-reflection.md`**.
- **Timer:** **`DeliveryUrgencyTimer`** — per-leg base budget **160s** (was 90s); **`GlobalNotificationHUD.prefab`** + **`GlobalNotificationHudCreator`** placeholder text updated.
- **Objective HUD:** **`DeliveryObjectiveHud.cs`** — top-center “Find package” / “Deliver to: Room …” while a leg is active; auto-added on **`GlobalNotificationHud`**.
- **LLM pickup sync:** **`DeliveryPickupSpawnPoint.cs`**, **`DeliveryManager.CurrentPickupLocationLabel`**; **`OllamaConnector`** CONTEXT uses authoritative pickup site (removed lobby/reception default); system prompt rule against inventing lobby.
- **Model:** Default **`llama3.2:3b`** on **`OllamaConnector`**, **`Main Game.unity`**, **`setup.md`**, **`ollama-plan.md`** §2 (meetup feedback: faster/smaller local model).
- **Settings:** **`GameSettingsMenu.cs`** + **`PauseScreen`** — **Esc → Pause → Settings** (runtime **Settings** button if missing); **`PlayerController`** mouse sensitivity via **`PlayerPrefs`** (0.5×–3×).
- **UX polish:** **`GlobalNotificationHud`** center-screen package-delivered banner; **`DeliveryItem`** spin/bob highlight while active.
- **Restart / build stability:** **`GameplaySessionReset.cs`** on gameplay scene load — **`HackingMazeMinigame.ForceCloseForSessionReset`**, **`ComputerDesktopUI.ResetForNewGameplaySession`**, **`OllamaConnector.ResetSessionStateForSceneLoad`**; **`DeliveryUrgencyTimer.TryCommitDeferredLegCountdown`** waits if PC session still open after maze.
- **Declined (documented):** Tab-phone terminal — conflicts with computer-as-hub design; see **`critical-feedback.md`**.
- **Documentation:** **`feedback-summary.md`**, **`critical-feedback.md`**, **`part3-reflection.md`**, **`refinements-changes.md`**, **`README.md`**, **`setup.md`**, **`ollama-plan.md`**, **`prompts-used.md`**, this file.
- **Pushed:** Commit **`0c732e7`** (Part 3 refinements) + this **`RiyaadWork.md`** update to **`origin/main`**.

---

## 2026-06-18 (continued) — DDOL restart, settings, LLM & timer polish

- **DDOL / scene reload:** `GlobalNotificationHud.MergeSceneInstanceReferences`, `DeliveryManager.CopySceneBindingsFrom` / `RefreshDropPointRegistrationsFromScene`, `DeliveryZone.ResolveDeliveryManager`, `GameplaySessionReset` + `ResetTransientNotificationsForSessionOnHud`; `OllamaConnector.ResetSessionStateForSceneLoad` clears stale refs; `ChatManager.ResolveOllamaConnector`.
- **Bugs fixed:** Main-menu retry — destroyed delivery refs, missing drop points, LLM disconnect, stale package-delivered toast, intro coroutine on inactive HUD.
- **UX:** `DeliveryObjectiveHud` under timer; timer **pauses** at open PC (`DeliveryUrgencyTimer` + `ComputerTerminal.NotifyComputerSessionOpened`); pause **Settings** menu (`GameSettingsMenu`, `PauseMenuUiFactory`, editor setup menu).
- **LLM:** Greeting/hostage-pronoun/quest-log rules in `SystemPrompt`; CONTEXT destination announcement; reply strip/normalize; bold **H:** messenger labels.
- **Input:** `BpsMouseInput.cs` + 15 BPS scripts for Unity 6 Input System.
- **Documentation:** All team docs updated; pushed to **`origin/main`** with code.

---

## 2026-06-18 (polish) — Intro, computer UX, audio, main-menu load

- **Intro:** `GameSceneIntroPanel` body — wife missing + quoted **H** threat; prefab + scene overrides.
- **Computer UX (Attendee C):** Arrow hints at desk; `ComputerTerminal` hides hints on first interact; `TopLeftNotifications` hidden while PC open.
- **Main menu:** `SceneNavigationUtility` + intro merge on DDOL HUD reload; Editor Inspector selection cleared before scene load.
- **Audio:** `DeliveryZone` door knock on successful delivery; `PlayerMovementAudio` + `SoundManager.WalkingLoopClip` (`Assets/SFX/Walking.mp3`, `Door Knock.mp3` on desktop canvas).
- **Documentation:** `refinements-changes.md`, `setup.md`, `README.md`, `feedback-summary.md`, this file.

---

## Backlog (personal)

- [ ] *(Add tasks you own next.)*
