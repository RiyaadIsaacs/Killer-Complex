# Killer Complex — project plan (Part 2 POE)

Living document for **milestones**, **scope**, and **ownership**. Technical LLM details belong in **`ollama-plan.md`**.

---

## Vision (short)

First-person thriller: an **Ollama-driven** online persona (**H**) coerces the player through chat (fiction-only “personal” data), assigns **package deliveries** across a **three-floor** complex, and sometimes hints at **illicit** contents. The player returns to a **home computer** to wait for the next job and can run **hacking mini-games** (trace persona, delete data, police evidence). **Dual endings:** delivery quota vs escape blackmail.

---

## Vertical slice (priority)

1. **Ollama at computer** — reliable `POST /api/chat`, UI, loading/error states (most play time for the slice).
2. **Mission loop** — pickup (window / entrance / rooftop) → deliver to rolled unit → computer.
3. **One floor slice** acceptable at first; scale to three floors when stable.
4. **Hacking** — minimal playable proof, then depth.
5. **Dual endings** — wire quotas and hacking thresholds; document priority if both could fire.

---

## Milestones (edit dates as you go)

| Target | Deliverable | Owner |
|--------|-------------|-------|
| M1 | `OllamaChatClient` + test UI in scene | TBD |
| M2 | Mission state + pickup/delivery interactables | TBD |
| M3 | Computer hub + prompt iteration (`prompts-used.md`) | TBD |
| M4 | Hacking vertical slice | TBD |
| M5 | Endings + polish + Windows build | TBD |
| M6 | Videos + ARC zip | TBD |

---

## Partner links

- Individual logs: **`RiyaadWork.md`**, partner file *(add name)*.
- Before every **GitHub push**: update **`prompts-used.md`** (any new/changed prompts), **`refinements-changes.md`** (what changed today), and skim **`ollama-plan.md`** if integration changed.

---

## Open decisions

- [ ] First-package flow: in-hand vs first pickup spawn (see `ollama-plan.md` when decided).
- [ ] Ending priority if delivery quota and escape both satisfied.
- [ ] Exact delivery count for Ending A (**`DeliveryManager.TotalDeliveryLegs`**) and hacking thresholds for Ending B.
