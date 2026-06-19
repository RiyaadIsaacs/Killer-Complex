# Feedback summary — Part 3 (Joburg Game Dev Meetup)

**Project:** Killer Complex (GADS7331 Part 2 → Part 3 refinements)  
**Event:** Joburg Game Dev Meetup (lecturer-approved)  
**Format:** Live playtest demo (~5–10 min): PC messenger, delivery loop, hacking maze, local Ollama **H** persona.  
**Checklist:** [`part3-poe-checklist.md`](part3-poe-checklist.md) · **Critical analysis:** [`critical-feedback.md`](critical-feedback.md)

---

## Event and purpose

*(Fill before submission: **date**, **venue**, **lecturer approval** reference if required.)*

We attended the **Joburg Game Dev Meetup** as our lecturer-approved external engagement for Part 3. The event gathers Johannesburg-area developers, artists, and hobbyists — a realistic critique environment outside the classroom. We attended to **stress-test Killer Complex with strangers** (not classmates or close friends), observe how technically literate players react to a **local LLM-driven antagonist**, and collect actionable feedback before final POE submission. Presenting live with **Ollama** on a laptop also tested whether “emerging tech” demo friction (latency, restarts) would overshadow design intent.

**Why this event (not only online feedback):** Part 3 requires in-person industry-adjacent engagement; a meetup mirrors how indie teams pitch vertical slices — short demo, immediate questions, unfiltered reactions. We disclosed that dialogue is **machine-generated** during the demo.

---

## Attendance evidence

| Evidence type | Location / notes | Status |
|---------------|------------------|--------|
| Photo at event | Add to `Docs/Part3/Evidence/` *(e.g. `meetup-attendance.jpg`)* — link or embed in ARC PDF | ☐ **Team: add** |
| Event name | Joburg Game Dev Meetup | ☑ |
| Demo format | Live Windows/Editor playtest, ~5–10 min per attendee | ☑ |
| Raw session notes | This file + team notebooks | ☑ partial |

---

## Feedback providers

*Module requirement: identify who gave feedback; confirm they are **not personal acquaintances**.*

| ID | Role | Relationship to team | Name *(optional — consent)* |
|----|------|----------------------|----------------------------|
| A | 3D artist | Met at meetup; first interaction | *(fill)* |
| B | Hobbyist game developer | Met at meetup; first interaction | *(fill)* |
| C | Programmer | Met at meetup; first interaction | *(fill)* |

---

## Session notes (engagement)

- Demonstrated: apartment hub → computer → messenger with **H** → package pickup → drop-off → hacking maze unlock path.  
- Discussed: local vs cloud LLM, restart behaviour, timer fairness at PC.  
- Captured quotes in table below same day / immediately after event.

---

## Feedback log

| # | Quote / paraphrase | Role | Category | Project area |
|---|-------------------|------|----------|--------------|
| 1 | Player feels too short | A | Gameplay / feel | Player camera / scale |
| 2 | AI bugs out on restart — disconnects from LLM in build | A | Technical / LLM | Ollama session, scene reload |
| 3 | Hacking terminal should not be available after restart until earned again | A | Bug / UX | `ComputerDesktopUI`, session reset |
| 4 | Maze bugs out on restart | A | Bug | `HackingMazeMinigame`, DDOL vs scene |
| 5 | Timer is too short | A, B, C | Gameplay pacing | `DeliveryUrgencyTimer` |
| 6 | Hacking notification doesn't always appear when H returns after hacking | B | UI / LLM | `DesktopMessengerNotification` |
| 7 | Timer didn't pause when H returns with new delivery after hack; delivery starts immediately | B | UX / pacing | Timer defer at PC / maze close |
| 8 | Extend timer to **160 seconds** | B, C | Gameplay pacing | `DeliveryUrgencyTimer` |
| 9 | Mouse sensitivity setting | A, B, C | Accessibility / controls | `PlayerController`, settings menu |
| 10 | Smaller / faster local AI model | A, B, C | Performance | Ollama model (`llama3.2:3b`) |
| 11 | Terminal more accessible (e.g. Tab phone) | C | UX / scope | Declined — computer-as-hub design |
| 12 | Objective HUD for delivery room | C | UX | `DeliveryObjectiveHud` |
| 13 | Package delivered popup more noticeable (center, bigger) | C | UI | `GlobalNotificationHud` |
| 14 | Package pickup should spin / highlight so it's easier to spot | C | UX | `DeliveryItem` |
| 15 | Players forget room number when PC closes — timer + objective HUD | C | UX | Objective + 160s timer |
| 16 | *(Observed during playtest)* H sometimes says package is in lobby but spawn is randomized | Team | LLM accuracy | `OllamaConnector` CONTEXT vs `DeliveryManager` spawns — **addressed 2026-06-18** (pickup hidden from player; authoritative spawn in CONTEXT only) |
| 17 | Computer should be more obvious to find | C | UX / onboarding | Scene arrow hints at desk; hidden after first **`ComputerTerminal`** interact — **addressed 2026-06-18 (polish)** |

**Status after 2026-06-18 (settings & audio):** Rows **2–4**, **7**, **9–10**, **12–17** targeted in code/docs. Latest: pause **mouse + SFX volume** settings, door open/close SFX, editable settings panel layout. Re-test in **Windows build** before final submission video.

---

## Aspects addressed (feedback → project area → action)

| Project aspect | Feedback IDs | Addressed? | Implementation / doc |
|--------------|--------------|------------|----------------------|
| Delivery pacing / timer | 5, 7, 8, 15 | Yes | `DeliveryUrgencyTimer` 160s; pause at PC; objective under timer |
| Controls / comfort | 9 | Yes | `GameSettingsMenu` — Esc → Pause → Settings (mouse **0.1×–2×**, SFX volume **0–100%**) |
| LLM performance | 10 | Yes | Default `llama3.2:3b` — `ollama-plan.md` |
| LLM accuracy / trust | 16 | Yes | `CurrentPickupLocationLabel` in CONTEXT; destination in orders |
| Objective clarity | 12, 15 | Yes | `DeliveryObjectiveHud` |
| Delivery UI feedback | 13 | Yes | Center `GlobalNotificationHud` banner |
| Package discoverability | 14 | Yes | `DeliveryItem` spin/bob |
| Build / session stability | 2, 3, 4 | Yes | DDOL merge, `GameplaySessionReset`, maze/desktop reset |
| Hack / messenger UX | 6, 7 | Yes | Timer defer/pause; notification audit |
| Computer discoverability | 17 | Yes | Arrow hints; hide on first PC use; top-left HUD hidden at PC |
| Computer-as-hub design | 11 | **Declined** | `critical-feedback.md` — Tab phone out of scope |
| Player scale / feel | 1 | Deferred | Art pass — backlog |

---

## Recurring themes

1. **Timer length (160s)** — all three attendees.  
2. **Mouse sensitivity** — all three.  
3. **Smaller/faster Ollama model** — all three.  
4. **Restart / build stability** — hacking icon, maze, LLM (Attendee A).  
5. **Objective clarity** — room number + pickup location (Attendee C; aligns with LLM location bug).  
6. **Strong AI-in-loop** — praised separately (see below).

---

## Initial reactions (while receiving feedback)

- **Surprised** that timer length dominated over narrative/LLM tone critique.  
- **Expected** more comments on maze difficulty; got stability and pacing instead.  
- **Agreed quickly** with objective HUD and 160s — low cost, high clarity.  
- **Defensive then convinced** on Tab-phone idea — valid for other games, wrong for this slice’s fiction.  
- **Embarrassed but grateful** about restart bugs — common with DDOL + scene reload; meetup made them priority.

---

## Positive feedback (preserve)

- AI interaction with the player feels meaningful.  
- **E** to interact works well.  
- Bottom-left control hints and package-delivered toast appreciated.  
- AI is part of the gameplay loop, not cosmetic.  
- Level layout — easy to orient in the environment.
