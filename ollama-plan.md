# Ollama integration plan — Killer Complex (Part 2)

This file is the **canonical** LLM/Ollama specification for the POE (required filename).  
Team schedule and ownership live in **`plan.md`**. Update this document whenever the model, prompts, or data flow change.

---

## 1. Role of the LLM in the game loop

- **Persona:** Local Ollama plays an in-fiction online hacker antagonist **H** (pressure, fictional “personal” details, delivery orders).
- **Primary slice:** Most **vertical-slice** time is spent at the **computer** interacting with Ollama (messages, missions, tone), not roaming a large map.
- **World actions:** Deliveries to random units across **three floors**; package **pickup** at one of: **room window**, **front entrance**, **rooftop** (spawn choice can be code-driven; model may reference it in prose).
- **Parallel beat:** At the computer, player can **wait** for the next delivery and/or play **hacking mini-games** (trace persona, delete data, police evidence) — mostly scripted unless you add optional LLM lines.
- **Dual endings:**
  - **Ending A:** Complete a **set number of deliveries** (quota — define exact count in game data).
  - **Ending B:** **Escape blackmail** via hacking objectives (thresholds for trace / scrub / evidence — define in game data).
- **Conflict rule:** Document here which ending wins if both conditions could be met in the same session.

*(Fill in concrete numbers, scene names, and script/class names as you implement.)*

---

## 2. Model and hardware

| Item | Value |
|------|--------|
| Ollama version | *(e.g. 0.x.x)* |
| Model name | *(e.g. llama3.2, mistral)* |
| Host / port | Default `http://127.0.0.1:11434` unless overridden |
| Dev machine specs | *(CPU, RAM, GPU if used — both partners)* |

**Marker note:** Game expects Ollama running locally with the model **pulled** before Play.

---

## 3. Inference timing

| When | Where | Purpose |
|------|--------|---------|
| **Runtime** | Unity → Ollama `POST /api/generate` (`OllamaConnector`) | Messenger replies from persona **H**; each send includes hidden game **context** + player line (see §4–5) |
| **Preprocessing** *(if any)* | *(Editor / batch — or “none”)* | *(e.g. baked test strings only — state explicitly)* |

---

## 4. Data flow (Unity ↔ Ollama)

```text
ChatManager (player SEND)
  → visible TMP: "[Player]: {typed}"
  → OllamaConnector.SendToOllama(typed plain text)

OllamaConnector
  → BuildPlayerTurnForPrompt: "[CONTEXT: …] Player says: {typed}"
  → fullPrompt = systemPrompt + separator + player turn
  → POST /api/generate { model, prompt, stream: false }
  → parse JSON "response" → ChatManager.UpdateChatFeed("H", reply)
```

- **History:** Each request is **stateless** for now (no prior turns in `prompt`). Sliding-window or chat API migration should be documented here when added.
- **Structured output:** Not required for the current generate path; assistant text is plain prose. If you add JSON mission fields later, document parse + **fallback** here.
- **Context sources today:** Optional **`DeliveryManager`** → completed count = `currentDeliveryID` clamped to **`TotalDeliveryLegs`** (Inspector, default **3**). While a delivery leg is active: **`CurrentLegDestinationApartment`**, a comma-separated **whitelist of valid apartment numbers** from the project map (e.g. **201–208**, **301–308** — see `TryGetApartmentRoomForDropPoint`), and optional **pickup state** when a **`DeliveryItem`** is configured. The internal **`ActiveDropPointId`** is **not** sent to the model (avoids “apartment 6” style confusion). **`LikeabilityPercent`** (0–100, default **50**) on `OllamaConnector`; other scripts can set `LikeabilityPercent` at runtime.

---

## 5. Prompt structure (summary)

- **System prompt:** Hardcoded in `OllamaConnector` — persona **H** (hacker antagonist: threatening, impatient, transactional; sarcastic SA slang only; never apologize; rude player → threaten leak of **Project_Bleed_v2.docx**), South African complex setting, concise replies, fiction-only / no real PII, instruction to treat **`[CONTEXT: …]`** before **`Player says:`** as true in-world state. Also: use only apartment numbers named in CONTEXT; do not invent units; do not echo technical/placeholder wording from CONTEXT in replies.
- **User-side of prompt (single string after system + `---`):**  
  `[CONTEXT: …] Player says: {player message}`  
  The bracketed block is built in **plain prose** (delivery progress, likeability, valid apartments list, current destination when a leg is active, pickup lines). It is **not** shown in the messenger UI; it exists only in the HTTP `prompt` field.
- **Delivery timing:** Default is **not** to prepare the first leg on scene start (`DeliveryManager.prepareFirstDeliveryAfterSceneTick` off). **`ChatManager`** calls **`PrepareNextDeliveryFromAi`** on **each** player SEND while **no leg is active** and **`currentDeliveryID < TotalDeliveryLegs`** (`prepareDeliveryOnMessengerSendWhenIdle`, default on) — so the **LLM turn** follows the messenger line that **starts** the job (context includes the new destination). Optional one-frame deferred first leg remains available via **`prepareFirstDeliveryAfterSceneTick`** on `DeliveryManager`. **`DeliveryCompletionChatNotifier`** may still append a scripted **H** line after a completion; it does **not** roll the next job.
- **Future:** Mission phase, pickup site, hacking flags — extend `BuildPlayerTurnForPrompt` or a dedicated context builder and log changes in **`prompts-used.md`**.
- **Safety / failures:** On network or parse failure, `OllamaConnector` logs to Console and appends a **fallback** `[H]` line in the feed (see code).

*(Exact system string and dated experiments: **`prompts-used.md`**.)*

---

## 6. Risks and mitigations

| Risk | Mitigation |
|------|------------|
| Latency / UI freeze | Async requests, loading UI, timeout seconds = *(n)* |
| Invalid JSON / wrong format | Retry template, parse fallbacks, dev logging |
| Model refusal (blackmail theme) | Softer framing in system prompt; logged in `prompts-used.md` |
| Marker machine too weak | Document minimum spec; optional dev fallback *(not shown as “real LLM” in video)* |

---

## 7. Local vs cloud (short comparison)

*(Add after you test: one cloud tool vs Ollama — latency, control, cost/privacy, output quality. A paragraph or two is enough; the full discussion can also appear in the LLM Integration Report.)*

---

## 8. Changelog

| Date | Change |
|------|--------|
| 2026-05-11 | Initial skeleton for POE submission structure |
| 2026-05-11 | Team docs added (`plan.md`, `rules.md`, `setup.md`, `refinements-changes.md`, `prompts-used.md`, `RiyaadWork.md`); `README.md` expanded with doc index |
| 2026-05-12 | **`OllamaConnector`** + **`ChatManager`** hook; **`POST /api/generate`**; hidden **`[CONTEXT: …] Player says:`** prefix (deliveries + likeability); docs (`README`, `setup`, §4–5 here) aligned |
| 2026-05-14 | **Context** — prose whitelist + destination apartment only (no internal drop id in prompt); **delivery pacing** — next leg from **`ChatManager`** messenger send when idle; **`TotalDeliveryLegs`**; **`DeliveryZone`** interact + HUD toasts; **`DeliveryCompletionChatNotifier`** chat-only |
