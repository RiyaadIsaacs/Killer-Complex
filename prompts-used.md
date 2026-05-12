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

## Game — Ollama system prompt — 2026-05-12 — `OllamaConnector` (first ship)

**Goal:** Persona **V** + fiction guardrails + instruct model to honour **`[CONTEXT: …]`** before **`Player says:`**.

**Prompt (system — exact string in code, `OllamaConnector.cs`):**

```text
You are "V", a cold, manipulative blackmailer who communicates only through text. You operate in and around a South African apartment complex. You pressure the resident with implied threats and leverage; you never claim to be law enforcement. Stay in character as V. Keep replies concise (a few sentences unless the user asks for more). A bracketed [CONTEXT: ...] line before "Player says:" gives true in-world facts (errands completed, rapport); use them naturally when applying pressure. This is fiction only — do not reference real people's private data.
```

**Outcome:** In-engine; paired with dynamic context line (below). Iterate here on every system-string edit.

**Iteration notes:** Uses **`/api/generate`** single `prompt` (not chat messages array yet).

---

## Game — Ollama “user” turn (appended after system + separator) — 2026-05-12

**Goal:** Hidden state for LLM-driven behaviour (deliveries, likeability) without showing it in the messenger UI.

**Template (exact shape; values are runtime):**

```text
[CONTEXT: Player has completed {X}/{Y} deliveries. Likeability is {Z}%.] Player says: {player typed message}
```

**Example:** Player types `I'm busy.` with 0/3 deliveries and 50% likeability:

```text
[CONTEXT: Player has completed 0/3 deliveries. Likeability is 50%.] Player says: I'm busy.
```

**Outcome:** Implemented in `BuildPlayerTurnForPrompt`; full HTTP `prompt` = system + `\n\n---\n\n` + template.

**Iteration notes:** `Y` default **3** (`totalDeliveries` on component); `X` from optional `DeliveryManager.currentDeliveryID`; `Z` from `LikeabilityPercent`.

---

## Design elicitation (Cursor) — 2026-05-11 — Project direction

**Goal:** Lock Part 2 gameplay: AI persona blackmail, deliveries, computer hub, hacking, dual endings, scope strategy.

**Prompt (paraphrased user requirements fed into planning):**  
Ollama as online persona blackmailing with fictional personal info; designates apartment deliveries; hints at illegal package contents; player returns to computer to wait; mini-games to hack persona location, delete data, gather police evidence; loop: blackmail + first package → deliver to random room on three floors → computer wait/hack → pickup from window / entrance / rooftop → repeat; dual endings: N deliveries vs escape blackmail.

**Outcome:** Success — captured in `plan.md`, `ollama-plan.md`, and Cursor plan file.

**Iteration notes:** Next entries should be **actual JSON/messages** sent to Ollama from Unity once built.

---

## Cursor — 2026-05-12 — Interact door + computer terminal + UI builders

**Goal:** Hinge door script with pivot; fix E interact (raycast / SendMessage); computer terminal opens UI and restores on Escape; desktop with MESSENGER/DELIVERIES, chat scroll + TMP input, delivery list + complete button; sharper UI (TMP labels, point-filter sprite, scaler).

**Prompt (condensed):** Multiple user requests across the session: `InteractDoor`; interact debug + `SendMessageUpwards`; `ComputerTerminal`; plan then implement computer canvas with icons/panels; `ChatManager` + TMP scroll/input/SEND + `UpdateChatFeed`; `DeliveryManager` + three assignment lines + `OnDeliveryCompleted`; clarity pass on text/icons; update contribution MDs and push with note about test scene.

**Outcome:** Success — scripts and Editor menu builders in `Assets/Scripts` / `Assets/Scripts/Editor` / `Assets/Scripts/UI`; scenes `Main Game.unity`, `Tester scene.unity`; TMP package; prefab path documented.

**Iteration notes:** Re-run **GameObject → Computer Desktop Canvas (Screen Space Overlay)** after changing builder code; TMP Essential Resources required once per machine.

---

## Template — copy for new rows

### YYYY-MM-DD — Game Ollama — 
**Goal:**  
**Prompt:**  
**Outcome:**  
**Iteration notes:**  
