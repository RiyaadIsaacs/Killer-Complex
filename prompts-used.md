# Prompt archive — Killer Complex

**Rule:** Update this file **before every GitHub push** whenever prompts change (game Ollama, editor tools, or major Cursor sessions that define behaviour).

Use one block per **experiment**. Copy **exact** text where possible.

---

## How to log an entry

```text
### YYYY-MM-DD — [Game Ollama | Cursor | Other] — short title
**Goal:**
**Prompt (system / user / full):**
**Outcome:** (success / partial / fail — what happened?)
**Iteration notes:** (what you changed next)
```

---

## Game — Ollama system prompt *(pending first implementation)*

*(No in-engine prompts committed yet. When `OllamaChatClient` ships, paste the first system template here, then every revision.)*

---

## Game — Ollama user / mission prompts *(pending)*

---

## Design elicitation (Cursor) — 2026-05-11 — Project direction

**Goal:** Lock Part 2 gameplay: AI persona blackmail, deliveries, computer hub, hacking, dual endings, scope strategy.

**Prompt (paraphrased user requirements fed into planning):**  
Ollama as online persona blackmailing with fictional personal info; designates apartment deliveries; hints at illegal package contents; player returns to computer to wait; mini-games to hack persona location, delete data, gather police evidence; loop: blackmail + first package → deliver to random room on three floors → computer wait/hack → pickup from window / entrance / rooftop → repeat; dual endings: N deliveries vs escape blackmail.

**Outcome:** Success — captured in `plan.md`, `ollama-plan.md`, and Cursor plan file.

**Iteration notes:** Next entries should be **actual JSON/messages** sent to Ollama from Unity once built.

---

## Template — copy for new rows

### YYYY-MM-DD — Game Ollama — 
**Goal:**  
**Prompt:**  
**Outcome:**  
**Iteration notes:**  
