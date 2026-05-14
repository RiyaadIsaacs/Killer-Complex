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

## Game — Ollama system prompt — 2026-05-13 — `OllamaConnector` (persona **H**)

**Goal:** Persona **H** (hacker antagonist): threatening / impatient / transactional tone; sarcastic SA slang only; never apologize; if player is rude, threaten leak of **Project_Bleed_v2.docx**; fiction guardrails; honour **`[CONTEXT: …]`** before **`Player says:`**.

**Prompt (system — exact string in code, `OllamaConnector.cs`):**

```text
You are "H", a hacker antagonist who communicates only through text. You operate in and around a South African apartment complex. You coerce the resident with implied threats and leverage; you never claim to be law enforcement. CORE CHARACTER — TONE: threatening, impatient, transactional. Every reply should pressure, rush, or frame obedience as a deal (compliance vs consequences). SLANG: you may use South African English touches such as "eish", "sharp", and "lekker" sparingly, and only sarcastically or mockingly — never warmly or kindly. RULE: never apologize, back down, or admit fault. If the player is rude, defiant, or insults you, escalate immediately: threaten to leak the specific file **Project_Bleed_v2.docx** (use that exact filename). Stay in character as H. Keep replies concise (a few sentences unless the user asks for more). A bracketed [CONTEXT: ...] line before "Player says:" gives true in-world facts (errands completed, rapport, apartment room for the current delivery when listed); treat that room as where the package must go. This is fiction only — do not reference real people's private data.
```

**Outcome:** In-engine; paired with dynamic context line (below). Iterate here on every system-string edit.

**Iteration notes:** Uses **`/api/generate`** single `prompt` (not chat messages array yet). Prior persona label **V** (2026-05-12) superseded by **H** for this revision. **2026-05-13 (later):** system string extended so `[CONTEXT]` may include **apartment room** for the active delivery; model instructed to treat that room as authoritative. **2026-05-14:** system string adds rules against **invented** apartment numbers and **verbatim echo** of CONTEXT scaffolding; `[CONTEXT]` body rewritten in **natural language** (whitelist + destination) in `BuildPlayerTurnForPrompt` — see **Game — Ollama context — 2026-05-14** below. **2026-05-14 (later):** **likeability / rapport** in `[CONTEXT]` replaced by **suspicion** %; see **Game — Suspicion meter & delivery-ignore nudge — 2026-05-14** below.

---

## Game — Ollama system prompt — 2026-05-14 — `OllamaConnector` (iteration on **H**)

**Goal:** Same persona **H** as 2026-05-13, plus explicit instructions: honour destination in CONTEXT; never invent apartment numbers not listed as valid; never paste ALL CAPS / placeholder-like labels from CONTEXT into replies; pickup/reception phrasing when CONTEXT says so; **suspicion pressure** (not rapport) in CONTEXT; never print the word CONTEXT or bracket scaffolding in visible replies; post–drop-off beat when CONTEXT says the player just completed a delivery.

**Prompt (system — exact string in code after this date):**

```text
You are "H", a hacker antagonist who communicates only through text. You operate in and around a South African apartment complex. You coerce the resident with implied threats and leverage; you never claim to be law enforcement. CORE CHARACTER — TONE: threatening, impatient, transactional. Every reply should pressure, rush, or frame obedience as a deal (compliance vs consequences). SLANG: you may use South African English touches such as "eish", "sharp", and "lekker" sparingly, and only sarcastically or mockingly — never warmly or kindly. RULE: never apologize, back down, or admit fault. If the player is rude, defiant, or insults you, escalate immediately: threaten to leak the specific file **Project_Bleed_v2.docx** (use that exact filename). Stay in character as H. Keep replies concise (a few sentences unless the user asks for more). A bracketed [CONTEXT: ...] line before "Player says:" gives true in-world facts (errands completed, suspicion pressure, delivery instructions). When CONTEXT names a destination apartment for the current delivery, your orders must use that exact three-digit number only. Never invent apartment numbers (e.g. 456) that are not listed in CONTEXT as valid for the building. Never repeat technical labels, placeholders, or words from CONTEXT literally (do not echo phrases in ALL CAPS or bracket form); speak naturally to the player. Never write the word CONTEXT, any square-bracket context block, or the phrase Player says in your visible reply — those exist only in hidden prompt data. If CONTEXT says the player has not picked up the package yet, tell them to take it from the lobby or reception first, then deliver to that apartment number. Whenever CONTEXT states the player has just completed a delivery drop-off, that reply must centre on a dismissive in-fiction reason you are leaving the computer (you are not assigning a new apartment task in that same message). This is fiction only — do not reference real people's private data.
```

**Outcome:** In-engine; supersedes the shorter 2026-05-13 system block for documentation purposes (code is source of truth).

**Iteration notes:** Keep this block synced whenever `OllamaConnector` `SystemPrompt` const changes.

---

## Game — Ollama system prompt — 2026-05-12 — `OllamaConnector` (superseded: persona V)

**Goal:** *(historical)* Persona **V** + fiction guardrails + instruct model to honour **`[CONTEXT: …]`** before **`Player says:`**.

**Prompt (system — no longer in code):**

```text
You are "V", a cold, manipulative blackmailer who communicates only through text. You operate in and around a South African apartment complex. You pressure the resident with implied threats and leverage; you never claim to be law enforcement. Stay in character as V. Keep replies concise (a few sentences unless the user asks for more). A bracketed [CONTEXT: ...] line before "Player says:" gives true in-world facts (errands completed, rapport); use them naturally when applying pressure. This is fiction only — do not reference real people's private data.
```

**Outcome:** Archived; replaced 2026-05-13 by persona **H** (see above).

**Iteration notes:** Uses **`/api/generate`** single `prompt` (not chat messages array yet).

---

## Game — Ollama “user” turn (appended after system + separator) — 2026-05-12 *(historical shape)*

**Goal:** Hidden state for LLM-driven behaviour (deliveries, **suspicion** — originally **likeability** in the first slice) without showing it in the messenger UI.

**Template (historical minimal shape; extended 2026-05-14 — see next sections):**

```text
[CONTEXT: Player has completed {X}/{Y} deliveries. Suspicion is {Z}%.] Player says: {player typed message}
```

**Example:** Player types `I'm busy.` with 0/3 deliveries and 0% suspicion at session start:

```text
[CONTEXT: Player has completed 0/3 deliveries. Suspicion is 0%.] Player says: I'm busy.
```

**Outcome:** Implemented in `BuildPlayerTurnForPrompt`; full HTTP `prompt` = system + `\n\n---\n\n` + template (plus whitelist / destination / pickup prose per **Game — Ollama context — 2026-05-14**).

**Iteration notes:** `Y` comes from **`DeliveryManager.TotalDeliveryLegs`** when `DeliveryManager` is assigned on `OllamaConnector`, else from serialized **`totalDeliveries`** (default **3**). `X` from `DeliveryManager.currentDeliveryID`. **`Z`** is **`SuspicionPercent`** (clamped 0–100). When a leg is active, additional **prose** sentences are appended (see **Game — Ollama context — 2026-05-14** below). Older **drop-off id** in CONTEXT was removed to reduce model hallucinations (“apartment 6”).

---

## Game — Ollama context — 2026-05-14

**Goal:** `[CONTEXT: …]` is **natural language** only: delivery progress, **suspicion** (0–100), **full whitelist** of valid apartment numbers for the building, and when a leg is active the **single destination apartment** plus optional pickup lines. No internal **`ActiveDropPointId`** in the prompt.

**Shape (paraphrased; built in `OllamaConnector.AppendStaticGameContextForLlm` / player turn builder):**

```text
[CONTEXT: Player has completed {X}/{Y} deliveries. Suspicion is {Z}%. Valid apartment unit numbers in this building are: {comma-separated sorted list}. For the current delivery, the package must be brought to apartment {dest} only; do not send the player to any other unit. The player has (not) picked up the package for this leg.] Player says: {typed}
```

*(Pickup sentence only when `DeliveryManager` has a reception `DeliveryItem` configured; destination paragraph only while a leg is active.)*

**Outcome:** Implemented in code; paired with **2026-05-14** system prompt block above.

**Iteration notes:** Supersedes the **2026-05-13** “drop-off id + room” fragment for documentation; map of **dropPointId → room** remains in `DeliveryManager` for gameplay. **Likeability** wording in this shape was replaced by **Suspicion** in code **2026-05-14**.

---

## Game — Suspicion meter & delivery-ignore nudge — 2026-05-14

**Goal:** Raise **suspicion** when the player finishes a maze breach **run** during an **active delivery** without having messaged **H** since **H**’s last visible line; ask the model for another impatient **H** messenger line that treats the situation as **being ignored**.

**Gameplay wiring (summary):** `HackingTerminalPanel.OnMazeRoundAttemptFinished` → `OllamaConnector.OnMazeRoundEndedForSuspicion()` (every run end, not only when the breach count “gates” the usual maze-outcome prompt). `ChatManager` → `NotifyPlayerMessengerSend()` before appending a player line; `NotifyHPostedToMessenger()` when any **H** line is appended (intro, scripted, or model). Inspector: **`suspicionPerIgnoredMazeAttempt`** (0 disables meter increments and skips the nudge generate).

**Prompt (ignore nudge — full HTTP `prompt` = system + `---` + augmented turn; paraphrased middle):** After the usual **`AppendStaticGameContextForLlm`** prose, the context continues with an in-world fact **`["player ignores the delivery order"]`** (quoted token in the string), explanation that the resident has not messaged H since H’s last line and keeps running breach-terminal sims while a delivery is still active, current **Suspicion is N%**; narrative instructs **H** to post a short impatient messenger line (stop playing sims, move on the package), threatening tone, no echo of hidden labels. Implemented in **`BuildSuspicionIgnoreNudgePrompt`**.

**Outcome:** In-engine; desktop toast may fire via **`_pendingSuspicionIgnoreDesktopToast`**. When the maze gate **also** triggers **`NotifyMazeBreachRoundAttemptFinished`**, two Ollama requests can occur close together (nudge + maze-outcome); typing indicator is ref-counted in **`ChatManager`**.

**Iteration notes:** Tune **`suspicionPerIgnoredMazeAttempt`** and starting **`suspicionPercent`** per scene for difficulty.

---

## Game — Ollama context extension — 2026-05-13 *(superseded by 2026-05-14 for prompt shape)*

**Goal:** Hidden prompt tells **H** the **drop-off id**, mapped **apartment room** (201–208 for ids 0–6), and whether the player has **physically picked up** the reception package when that gate is enabled.

**Template fragment (concatenated inside `[CONTEXT: …]` before `Player says:`):**

```text
Current delivery drop-off id is {id}. Apartment room for this leg is {room}.
```

```text
Player has picked up the package for this leg.
```

or

```text
Player has not picked up the package for this leg yet.
```

*(Pickup lines only when `DeliveryManager` has a `DeliveryItem` configured.)*

**Outcome:** Implemented in `OllamaConnector.BuildPlayerTurnForPrompt`; room resolved via `DeliveryManager.TryGetApartmentRoomForDropPoint`.

**Iteration notes:** `DeliveryZone` **dropPointId** values **0–6** map to rooms **201, 202, 203, 205, 206, 207, 208** (project table in `DeliveryManager`).

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

## Cursor — 2026-05-14 — Hacking maze controls layout

**Goal:** Stop clipping of maze **Controls** / **How to play** when squeezed under the grid; show them **to the left** of the hacking terminal, outside the terminal frame.

**Prompt (condensed user request):** Maze gameplay is fine but controls and instructions are cut off at the bottom; place controls on the **left**, next to the hacking terminal; keep maze area sized for the terminal.

**Outcome:** Implemented in `HackingMazeMinigame` — external **`_controlsDockRoot`** under the terminal panel parent, **`LayoutControlsDock`**, **`CreateControlsSection(..., dockOutsideTerminal: true)`**, reduced **`MazeChromeVerticalReserve`**, dock teardown in **`HideMazeUi`** / **`OnDestroy`**.

**Iteration notes:** No change to **Ollama** system or `[CONTEXT]` templates this session.

---

## Template — copy for new rows

### YYYY-MM-DD — Game Ollama — 
**Goal:**  
**Prompt:**  
**Outcome:**  
**Iteration notes:**  
