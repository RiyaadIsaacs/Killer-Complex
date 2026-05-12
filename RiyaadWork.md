# Individual contribution log — Riyaad

**Module:** GADS7331 — Game Design 3A (Part 2)  
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

---

## Backlog (personal)

- [ ] *(Add tasks you own next.)*
