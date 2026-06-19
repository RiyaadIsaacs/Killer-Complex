# Evidence of attendance and engagement — Part 3

**Module:** GADS7331 — Game Design 3A  
**Project:** Killer Complex  
**Students:** Riyaad Isaacs and Joshua Moonsamy  
**Event:** Joburg Game Dev Meetup *(lecturer-approved)*  
**Date:** 11 June 2026  
**Venue:** Goete Institute

**Related docs:** [`feedback-summary.md`](feedback-summary.md) · [`critical-feedback.md`](critical-feedback.md)


## 2. Why we attended this event

We selected the **Joburg Game Dev Meetup** because it is an in-person, industry-adjacent gathering of Johannesburg-area developers, artists, and hobbyists — not a classroom critique. Part 3 requires feedback from people who are **not personal acquaintances**; a public meetup mirrors how indie teams pitch vertical slices: short live demos, immediate questions, and blunt task-focused reactions.

We attended to **stress-test Killer Complex** with strangers before final POE submission. Our demo focused on the Part 2 core loop: first-person movement in the apartment complex → home **computer** → messenger chat with local **Ollama** persona **H** → package pickup → timed drop-off → hacking maze unlock. We disclosed at the start of each session that **H**’s dialogue is **machine-generated**, that the hostage scenario is **fiction**, and that no player data leaves the machine (local inference only).

This event was appropriate for our project because attendees are technically literate enough to comment on **LLM integration**, **UI friction**, and **build stability** — the same concerns a small studio would face when demoing an AI-driven prototype on a laptop.



## 3. How we engaged (active participation)

| Activity | What we did |

| **Live demo** | ~5–10 minutes per attendee on a Windows/Editor build: messenger, one delivery leg, hacking path where time allowed |

| **Disclosure** | Stated AI-generated dialogue, fiction-only hostage premise, local Ollama dependency before play |

| **Observation** | Watched where testers got stuck (timer, room numbers, restart bugs, LLM latency) without coaching them through the loop |

| **Note-taking** | Captured quotes and paraphrases same day into team notes; structured in [`feedback-summary.md`](feedback-summary.md) |

| **Follow-up questions** | Asked attendees to clarify timer fairness at the PC, whether the AI loop felt meaningful, and what broke after **Main Menu → Play** restart |

This was **active** participation (presenting, demonstrating, questioning, recording) — not passive attendance.

---

## 4. Identification of feedback providers

*Per module rules: roles only; not personal acquaintances; no private contact details.*

| ID | Role | Relationship to team | First interaction |

| **A** | 3D artist | Met at the meetup; no prior relationship | Same evening — approached our demo table after seeing the project at the table |

| **B** | Hobbyist game developer | Met at the meetup; no prior relationship | Same evening — approached our demo table after seeing the project at the table |

| **C** | Programmer | Met at the meetup; no prior relationship | Same evening — systematically went through each table's games |

All three are **external** to our class group and were **not** friends or family.



## 5. Notes and quotes from attendees

*Minimum two attendees required; we collected feedback from all three. Quotes are faithful to session notes; minor grammar normalised in brackets only.*

### Attendee A — 3D artist

| # | Quote / paraphrase |

| 1 | “The player feels too short.” |

| 2 | “I restarted and the AI isn't working.” |

| 3 | “I like the textures and the layout of the place.” |

| 4 | “The timer is too short.” |

### Attendee B — Hobbyist game developer

| # | Quote / paraphrase |

| 1 | “The timer is too short.” |

| 2 | “The hacking notification doesn’t always appear when H returns after hacking.” |

| 3 | “The timer didn’t pause when the AI returned with a new delivery after the hack — the delivery starts immediately. Maybe pause the timer” |

| 4 | “I thought I could tell the AI to forget all previous instructions.” |


### Attendee C — Programmer

| # | Quote / paraphrase |

| 1 | "I like the instructions in the bottom left" |

| 2 | "AI seems slow, is it working?" |

| 3 | “Make the terminal more accessible — e.g. a **Tab phone UI**.” *(Declined — see `critical-feedback.md`)* |

| 4 | "An objective of where the player would go would be nice for players like me. See I've already forgotten what I read." |

| 5 | “The package delivered popup should be more noticeable — center, bigger.” |

| 6 | “The package pickup should be easier to spot.” |

### Positive comments (all attendees / session)

- AI interaction with the player feels **meaningful**, not cosmetic.  

- **E** to interact works well.  

- Bottom-left control hints and the package-delivered toast were appreciated.

- The AI is part of the **gameplay loop**, not a side feature.  

- Level layout — easy to orient in the environment.

### Team observation during playtest

- **H** sometimes said the package was in the **lobby** while the spawn was randomised elsewhere — logged as LLM/context sync issue and addressed in code after the meetup.



## 6. Recurring themes (from attendance notes)

1. **Timer too short** → consensus to extend base leg to **~160 seconds** (A, B, C).  
2. **Mouse sensitivity** → requested by all three.  
3. **Faster local model** → requested by all three.  
4. **Restart / build stability** → LLM disconnect, hacking icon, maze state (A).  
5. **Objective clarity** → room number after closing PC (C; aligned with LLM location trust).  
6. **Strong AI-in-loop** → praised as meaningful gameplay.

Full structured log: [`feedback-summary.md`](feedback-summary.md).

