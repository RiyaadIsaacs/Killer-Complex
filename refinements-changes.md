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

- **LLM:** New **`OllamaConnector`** (`POST` **`/api/generate`**, default model field `mistral:7b-instruct`). Each player message builds a prompt turn: **`[CONTEXT: Player has completed X/Y deliveries. Likeability is Z%.] Player says: …`** (context not duplicated in visible chat). Optional **`DeliveryManager`** link; **`LikeabilityPercent`** for future rapport systems.
- **UI wiring:** **`ChatManager`** optional serialized **`OllamaConnector`** — on send, calls **`SendToOllama`** after appending the player line.
- **Docs / compliance:** `ollama-plan.md` (data flow §4, prompt §5, changelog), `setup.md` §3, `README.md` (getting started + stack), `prompts-used.md` (exact system + context template), `RiyaadWork.md` — updated before push per `rules.md`.
- **AI-assisted:** Cursor authored C# + markdown; verify in Editor: references assigned, Ollama running, model pulled.

