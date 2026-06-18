# Feedback summary — Part 3 (Joburg Game Dev Meetup)

**Project:** Killer Complex (GADS7331 Part 2 → Part 3 refinements)  
**Event:** Joburg Game Dev Meetup (lecturer-approved)  
**Format:** Live playtest demo (~5–10 min): PC messenger, delivery loop, hacking maze, local Ollama **H** persona.

---

## Attendees (roles only)

| ID | Role |
|----|------|
| A | 3D artist |
| B | Hobbyist game developer |
| C | Programmer |

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

**Status after 2026-06-18 (continued) push:** Rows **2–4**, **7**, **9–10**, **12–16** targeted in code/docs (DDOL restart, 160s timer, objective HUD, settings, LLM reconnect, timer pause at PC, messenger formatting, pickup/destination sync). Re-test in **Windows build** before final submission video.

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
