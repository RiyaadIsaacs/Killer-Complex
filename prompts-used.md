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

**Iteration notes:** Uses **`/api/generate`** single `prompt` (not chat messages array yet). Prior persona label **V** (2026-05-12) superseded by **H** for this revision. **2026-05-13 (later):** system string extended so `[CONTEXT]` may include **apartment room** for the active delivery; model instructed to treat that room as authoritative. **2026-05-14:** system string adds rules against **invented** apartment numbers and **verbatim echo** of CONTEXT scaffolding; `[CONTEXT]` body rewritten in **natural language** (whitelist + destination) in `BuildPlayerTurnForPrompt` — see **Game — Ollama context — 2026-05-14** below. **2026-05-14 (later):** **likeability / rapport** in `[CONTEXT]` replaced by **suspicion** %; see **Game — Suspicion meter & delivery-ignore nudge — 2026-05-14** below. **2026-05-15:** narrative pivot to **kidnapper / hostage** + **Wife status** in `[CONTEXT]` — see **Game — Kidnapper narrative + Wife status — 2026-05-15** below.

---

## Game — Ollama system prompt — 2026-05-14 — `OllamaConnector` (iteration on **H** — *superseded 2026-05-15*)

**Goal:** Same persona **H** as 2026-05-13, plus explicit instructions: honour destination in CONTEXT; never invent apartment numbers not listed as valid; never paste ALL CAPS / placeholder-like labels from CONTEXT into replies; pickup/reception phrasing when CONTEXT says so; **suspicion pressure** (not rapport) in CONTEXT; never print the word CONTEXT or bracket scaffolding in visible replies; post–drop-off beat when CONTEXT says the player just completed a delivery.

**Prompt (system — exact string in code after this date):**

```text
You are "H", a hacker antagonist who communicates only through text. You operate in and around a South African apartment complex. You coerce the resident with implied threats and leverage; you never claim to be law enforcement. CORE CHARACTER — TONE: threatening, impatient, transactional. Every reply should pressure, rush, or frame obedience as a deal (compliance vs consequences). SLANG: you may use South African English touches such as "eish", "sharp", and "lekker" sparingly, and only sarcastically or mockingly — never warmly or kindly. RULE: never apologize, back down, or admit fault. If the player is rude, defiant, or insults you, escalate immediately: threaten to leak the specific file **Project_Bleed_v2.docx** (use that exact filename). Stay in character as H. Keep replies concise (a few sentences unless the user asks for more). A bracketed [CONTEXT: ...] line before "Player says:" gives true in-world facts (errands completed, suspicion pressure, delivery instructions). When CONTEXT names a destination apartment for the current delivery, your orders must use that exact three-digit number only. Never invent apartment numbers (e.g. 456) that are not listed in CONTEXT as valid for the building. Never repeat technical labels, placeholders, or words from CONTEXT literally (do not echo phrases in ALL CAPS or bracket form); speak naturally to the player. Never write the word CONTEXT, any square-bracket context block, or the phrase Player says in your visible reply — those exist only in hidden prompt data. If CONTEXT says the player has not picked up the package yet, tell them to take it from the lobby or reception first, then deliver to that apartment number. Whenever CONTEXT states the player has just completed a delivery drop-off, that reply must centre on a dismissive in-fiction reason you are leaving the computer (you are not assigning a new apartment task in that same message). This is fiction only — do not reference real people's private data.
```

**Outcome:** In-engine; supersedes the shorter 2026-05-13 system block for documentation purposes (code is source of truth).

**Iteration notes:** Keep this block synced whenever `OllamaConnector` `SystemPrompt` const changes. **Superseded for narrative (2026-05-15):** persona pivoted to **kidnapper / hostage** — see **Game — Kidnapper narrative + Wife status — 2026-05-15** below; code no longer uses **Project_Bleed** or hacker-antagonist framing.

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

**Goal:** `[CONTEXT: …]` is **natural language** only: delivery progress, **suspicion** (0–100), optional **Wife status** (fiction prose from **`wifeStatusForLlmContext`** on **`OllamaConnector`**), **full whitelist** of valid apartment numbers for the building, and when a leg is active the **single destination apartment** plus optional pickup lines. No internal **`ActiveDropPointId`** in the prompt.

**Shape (paraphrased; built in `OllamaConnector.AppendStaticGameContextForLlm` / player turn builder):**

```text
[CONTEXT: Player has completed {X}/{Y} deliveries. Suspicion is {Z}%. Wife status (fiction — use for threats, do not quote this header): {wifeStatus prose when configured.} Valid apartment unit numbers in this building are: {comma-separated sorted list}. For the current delivery, the package must be brought to apartment {dest} only; do not send the player to any other unit. The player has (not) picked up the package for this leg.] Player says: {typed}
```

*(**Wife status** sentence omitted when `wifeStatusForLlmContext` is empty. Pickup sentence only when `DeliveryManager` has a reception `DeliveryItem` configured; destination paragraph only while a leg is active.)*

**Outcome:** Implemented in code; paired with **2026-05-15** kidnapper system prompt (see below).

**Iteration notes:** Supersedes the **2026-05-13** “drop-off id + room” fragment for documentation; map of **dropPointId → room** remains in `DeliveryManager` for gameplay. **Likeability** wording in this shape was replaced by **Suspicion** in code **2026-05-14**.

---

## Game — Suspicion meter & merged maze reply — 2026-05-14 *(behaviour tightened 2026-05-16)*

**Goal:** Raise **suspicion** when the player finishes a **gated** maze breach batch during an **active delivery** without having messaged **H** since **H**’s last visible line; fold ignore-delivery pressure into the **same** Ollama turn as the breach-outcome reply (**one** **H** message, **one** HTTP call).

**Gameplay wiring (summary):** After the breach-count gate, **`HackingTerminalPanel`** calls **`ApplySuspicionIncrementForIgnoredMazeAttempt()`** (meter only, no HTTP), then **`NotifyMazeBreachRoundAttemptFinished(..., mergeIgnoreDeliveryOrderIntoMazeReply)`** so a **single** Ollama generate carries breach outcome plus optional ignore-delivery / suspicion prose in **`[CONTEXT]`**. `ChatManager` → **`NotifyPlayerMessengerSend`** / **`NotifyHPostedToMessenger`**. Inspector: **`suspicionPerIgnoredMazeAttempt`** (0 skips increment and merge hints).

**Prompt (merged into maze-outcome — single HTTP `prompt` = system + `---` + `[CONTEXT]` + narrative):** When **`ApplySuspicionIncrementForIgnoredMazeAttempt`** returns true, **`NotifyMazeBreachRoundAttemptFinished`** appends the same **`["player ignores the delivery order"]`** prose and narrative instructions to fold ignore-pressure into the breach-outcome reply (one coherent **H** message). Implemented in **`OllamaConnector.NotifyMazeBreachRoundAttemptFinished`** (no separate **`BuildSuspicionIgnoreNudgePrompt`** call).

**Outcome:** In-engine; desktop toast uses the maze-outcome pending flag. **One** Ollama request per gated breach batch (suspicion increment is folded into the maze prompt when applicable).

**Iteration notes:** Tune **`suspicionPerIgnoredMazeAttempt`** and starting **`suspicionPercent`** per scene for difficulty.

---

## Game — Kidnapper narrative + Wife status — 2026-05-15

**Goal:** Reframe **H** as a **kidnapper** holding the player’s **wife** hostage (fiction), watching via **apartment security cameras**, giving **orders** (no jokes). On **delay or failed delivery**, **H** describes **clinical** details about the wife’s condition to terrify the player; **bru** / **wena** for dominance. Hidden **`[CONTEXT]`** carries an optional **Wife status** prose block (**`wifeStatusForLlmContext`**) as the main lever for escalating blackmail dialogue (tunable in the Inspector or at runtime via **`WifeStatusForLlmContext`**).

**Prompt (system — exact string in `OllamaConnector.SystemPrompt` after this date):**

```text
You are H, a cold, ruthless, and transactional kidnapper. You have the player's wife. You are watching the player through the apartment's security cameras. You don't make jokes; you give orders. If the player delays or fails a delivery, you describe a detail about the wife's current condition to terrify them. Use clinical, detached language mixed with South African slang like "bru" or "wena" to assert dominance. Never apologize, back down, or admit fault. If the player is rude, defiant, or insults you, escalate immediately with a concrete hostage threat—never humor. Stay in character as H. Keep replies concise (a few sentences unless the user asks for more). A bracketed [CONTEXT: ...] line before "Player says:" gives true in-world facts (errands completed, suspicion, delivery instructions, and Wife status for threats only). When CONTEXT names a destination apartment for the current delivery, your orders must use that exact three-digit number only. Never invent apartment numbers (e.g. 456) that are not listed in CONTEXT as valid for the building. Never repeat technical labels, placeholders, or words from CONTEXT literally (do not echo phrases in ALL CAPS or bracket form); speak naturally to the player. Never write the word CONTEXT, any square-bracket context block, or the phrase Player says in your visible reply — those exist only in hidden prompt data. If CONTEXT says the player has not picked up the package yet, tell them to take it from the lobby or reception first, then deliver to that apartment number. Whenever CONTEXT states the player has just completed a delivery drop-off, that reply must centre on a dismissive in-fiction reason you are leaving the feed (you are not assigning a new apartment task in that same message). This is fiction only — do not reference real people's private data.
```

**Messenger intro (default `ChatManager.openingMessage` + `ComputerDesktopCanvas.prefab`):**

```text
I see you're finally at the computer. Stop looking for your wife—she's not at home anymore. If you want to see her again, you're going to be my legs tonight. There is a package in the lobby. If you don't get back to me, you will never hear from her again. Don't test me, bru. Move it.
```

**Full-hack reversal visible `[SYSTEM]` line (fiction, `SendHackReversalPrompt`):**

```text
[SYSTEM]: The player has fully decrypted the apartment uplink. They are counter-leveraging your surveillance and delivery control (fiction only).
```

**Outcome:** In-engine; **Project_Bleed** and keyword-based desktop toast for that filename removed. Earlier **hacker-antagonist** prompt blocks in this file remain as **history** only.

**Iteration notes:** Log further prompt edits here and in **`ollama-plan.md`** §8.

---

## Game — Conversational H (insults / tone) — 2026-05-15

**Goal:** **H** answers what the player actually says (insults, friendliness, seriousness) before delivery boilerplate; never brushes off provocation or repeats the same job script every reply.

**Prompt (system — exact string in `OllamaConnector.SystemPrompt` after this date):** See `Assets/Scripts/OllamaConnector.cs` (`SystemPrompt` const). Key rules: reply shape (engage player message first, then at most one short job clause); insults must be answered directly; friendly/serious tone acknowledged in character; vary phrasing; 3–6 sentences typical.

**Hidden `[CONTEXT]` addition:** `AppendStaticGameContextForLlm` appends a dialogue rule each turn; active-delivery facts are labelled background-only so the model does not lead every message with pickup/destination text.

**Outcome:** In-engine; re-test with insults, friendly, and serious player lines after `ollama pull` / Play.

**Iteration notes:** Local **mistral:7b-instruct** may still be stiff—raise temperature in Ollama options later if needed.

---

## Game — Bad ending trap (hidden `[SYSTEM]`) — 2026-05-28

**Goal:** When **all** configured delivery legs are done and the one-shot post-drop “H steps away” beat is still pending, the **next** messenger send must **not** ask for the dismissive-away excuse. Instead, **one** Ollama generate uses a hidden **`[SYSTEM]`** line (not duplicated as a visible messenger **`[SYSTEM]`** row) so **H** leads the player into the **final trap** (eerie calm; last package at **Room 204** fiction).

**Prompt block (exact string constant `BadEndingHiddenSystemBeat` in `OllamaConnector.cs`):**

```text
[SYSTEM]: The player has finished all jobs. You are now leading them to the final trap. Tell them there is one last package outside their own door (Room 204) and then they can see their wife. Be extremely eerie and calm.
```

**Shape:** `systemPrompt + --- + [CONTEXT: …] + {BadEndingHiddenSystemBeat} + narrative instructions + Player says: {player line}` — implemented in **`OllamaConnector.TryBuildBadEndingPlayerTurn`**. Reply stripping removes echoed **`[SYSTEM`…** lines from visible chat when models leak them.

**Outcome:** In-engine; no **`Remote access established`** suffix for this reply; desktop enters bad-ending mode via **`BadEndingOrchestrator.StartBadEnding()`**.

**Iteration notes:** Tune trap copy only in code (or extract to serialized string later) and log changes here + **`ollama-plan.md`** §8.

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

## Cursor — 2026-05-15 — Maze fog of war + random victory tile

**Goal:** The hacking maze should not show the full map — only a **3×3** lit area around the player. Randomize the **green** uplink (victory) cell each run instead of a fixed corner.

**Prompt (condensed user request):** Add fog so the player has vision of 3×3 around them; randomize the green victory position.

**Outcome:** `HackingMazeMinigame` — **`visionRadius`** (default 1 → 3×3), **`fogColor`**, **`IsCellVisible`** / **`GetRevealedCellColor`** in **`RefreshAllCells`**; **`PickRandomGoalCell()`** after loop carving with **`minGoalDistanceFromStart`**; controls/hint copy updated. Docs: **`setup.md`** §2b, **`README.md`**, **`refinements-changes.md`**, **`RiyaadWork.md`**.

**Iteration notes:** No Ollama prompt or API change. Tune **`visionRadius`** or **`minGoalDistanceFromStart`** on the maze component in the Inspector if needed.

---

## Cursor — 2026-05-17 — Delivery urgency timer starts when leaving PC

**Goal:** Avoid losing countdown time while still at the computer reading **H** or chatting; start the urgent delivery HUD timer when the player **exits** the PC session.

**Prompt (condensed user request):** Timer should not start when still in conversation — start when exiting the computer.

**Outcome:** **`DeliveryUrgencyTimer`** — defer after **H** post if any **`ComputerTerminal`** is open (`_awaitingComputerCloseToStartTimer`); **`NotifyComputerSessionClosed()`** from **`ComputerTerminal.CloseTerminal()`**; immediate start if **H** posts while PC already closed. **`[CONTEXT]`** urgent seconds unchanged in behaviour (only while countdown active). Docs: **`setup.md`** §2c, **`ollama-plan.md`** §4 + §8, **`refinements-changes.md`**, **`RiyaadWork.md`**.

**Iteration notes:** No system-prompt edit; context timing aligns with **`GetRemainingSecondsForLlmContext`**.

---

## Template — copy for new rows

### YYYY-MM-DD — Game Ollama — 
**Goal:**  
**Prompt:**  
**Outcome:**  
**Iteration notes:**  
