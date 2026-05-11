# Ollama integration plan — Killer Complex (Part 2)

This file is the **canonical** LLM/Ollama specification for the POE (required filename).  
Team schedule and ownership live in **`plan.md`**. Update this document whenever the model, prompts, or data flow change.

---

## 1. Role of the LLM in the game loop

- **Persona:** Local Ollama plays an in-fiction online blackmailer (pressure, fictional “personal” details, delivery orders).
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
| **Runtime** | Unity → Ollama `POST /api/chat` | Blackmail + mission text, ongoing conversation |
| **Preprocessing** *(if any)* | *(Editor / batch — or “none”)* | *(e.g. baked test strings only — state explicitly)* |

---

## 4. Data flow (Unity ↔ Ollama)

```text
[Computer UI / Mission state] → build messages[] (system + user + optional history)
       → OllamaChatClient (UnityWebRequest JSON)
       → parse assistant content → update UI + mission fields (floor/unit, flavour text)
```

- **Structured output:** Target machine-readable fields (JSON or rigid line format). Document **fallback** if the model drifts (regex, retry prompt, safe defaults).
- **State:** What is sent each call (full history vs sliding window — pick one and note token/latency impact).

---

## 5. Prompt structure (summary)

- **System prompt:** Persona rules, fiction-only PII, tone, max length, no real-world instruction for crime, output format instructions.
- **User prompt:** Current mission phase, pickup site (if rolled), prior delivery count, hacking flags if they should influence taunts.
- **Safety:** If the model refuses or sanitizes heavily, what does the player see?

*(Link detailed prompt text and iterations in **`prompts-used.md`**.)*

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
