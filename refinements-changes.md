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

---

## 2026-05-13 — Persona **H**, messenger UX, docs

- **LLM persona:** **`OllamaConnector`** system prompt retargeted from **V** to **H** (hacker antagonist): threatening / impatient / transactional tone; South Africanisms (*eish*, *sharp*, *lekker*) only sarcastically; never apologize; rude player → threaten leak of **`Project_Bleed_v2.docx`**. Replies and errors use **`[H]: …`** in the feed (`HackerSenderLabel`).
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
