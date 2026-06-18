# Critical engagement with feedback — Part 3

**Project:** Killer Complex (GADS7331)  
**lecturer-approved Event:** Joburg Game Dev Meetup  
**Authors:** Joshua Trent and Riyaad Isaacs



## Anticipation of feedback

Before the meetup we did not have any clear expectations of the event but we knew there would be **external testers, non-classmate** to behave more like a public playtest scenario rather than a design review: blunt, task-focused, and sensitive to **friction** rather than lore essays.

**What we assumed would draw the most attention:**

1. **LLM persona** — whether **H** felt menacing, repetitive, or broke the kidnapper persona and the immersion.  

2. **Hacking maze** — difficulty, clarity of controls, whether it felt gimmicky.   

3. **Narrative discomfort** — hostage fiction, surveillance, coercion (at least one ethical or tone comment).  

4. **Level readability** — finding apartments and the package in a multi-floor kitbash environment.

We did **not** expect pacing issues (timer length, struggle with narrative immersion) or **build-only restart bugs** to dominate the conversation. We also underestimated how much testers would treat **ground truth**, and turn it into complete misinterpretation (pickup room vs what **H** said). This came off as bad writing.



## Alignment with expectations

| Expected focus | What actually happened |

| LLM interactive tone / character | Some tonal points were noticed; **bigger issues were raised like the factual sync** (LLM communicating the package position[Lobby] vs where it actually is[bathroom]). |

| Maze difficulty and exploration | Maze mentioned mainly as **restart instability**, difficulty with readablity. |

| Narrative discomfort that comes with the thriller genre | Little explicit ethical pushback; testers stayed at the terminal to ask “What if I don't care about my wife?” |

| Environment navigation | **Objective clarity** (What room number after closing PC) became a theme via Attendee C(Programmer) and A(3D Artist). |

| Hacking as possible friction | Praised as part of loop; **no** “remove hacking” feedback. |

**Partially aligned:** Attendee B’s(Hobbyist) comment that the timer felt unfair **during** talks with the LLM. This validated our design intent to defer (and later **pause**) the urgency countdown while the computer session is open — but the implementation was incomplete until post-meetup polish.

**Misaligned:** We prepared mentally to defend **prompt engineering**; we spent more time defending **systems integration and bugs** (DDOL managers, CONTEXT authority, build vs Editor, LLM disconnecting).



## What surprised us

1. **Unanimous practical requests** — all three attendees independently asked for **longer timer (~160s)**, **mouse sensitivity setting**, and **faster respnse times**. That pattern is unusual for a narrative/LLM slice and signaled “comfort and quality of life” over “more game interaction.”

2. **Restart bugs surfaced only in build** — hacking icon available too early, maze state, LLM disconnected after **Main Menu → Play → Main Menu again**. Editor masking is a lesson we will not repeat.

3. **Trust broke miscommunication** — when **H** said “lobby” but the package spawned elsewhere, testers cared more than about that.

4. **Positive feedback on the AI loop itself** — several said the messenger + delivery integration felt **meaningful**, not cosmetic. We had worried the UI was busy; for this audience, **E** interact and bottom-left hints were assets.



## Strong points — critique vs praise

| We thought this was a strength | Attendee response |

| AI-in-the-loop gameplay | **Praised** — AI felt part of and contributed to the gameplay loop. |

| Computer-as-hub fiction | **Challenged once by Attendee C(Programmer)** — Gave a Tab to open a phone terminal idea; not a rejection of the whole game. |

| Control hints / toasts | **Praised** — package-delivered toast and hints helped. |

| Level layout | **Praised** — easy to orient. |

| Local Ollama “wow factor” | **Critiqued indirectly** — too slow; push toward smaller model, **llama3.2:3b**. |

Testers **did critique** a strength we underplayed: **reliability**. The LLM feature is only a strength if it stays connected and matches state after restarts.



## Ignored or underweighted areas

We expected more feedback on:

- **Dual endings** and moral choice — not discussed in the short demo window aince players did not finish the game.  
- **Traps** — players did not rais why they died, they just accepted it.  
- **Insults by H** — less important than completing a delivery.  
- **Extra level scale** — Attendee A(3D Artist) mentioned player height once; did not recur among the others.

Short demo length (~5–10 minutes) likely filtered these out. That is not dismissal — it is **sample bias**. Noted for the final reflection.



## Declined or deferred

| Feedback | Decision | Reason |

| **Tab phone terminal** Attendee(C) | **Declined** | Breaks the “trapped at PC” idea; duplicates messenger UX; breaks flow of gameplay loop. |

| **Player feels too short** (A) | **Declined** | Valid art pass; breaks image of the player being powerless. |

| **Hacking should be locked until completing delivery after restart** Attendee(A) and (B) | **Implemented** via session reset paths (see refinements). |

| **Remove escalating timer shrink per delivery** | **Partial** | Extended **base time** to 160s; kept pressure escalation — attendees asked for more time, not removal of tension. |

### Not feasible in Part 3 window (justified)

- **Full mobile phone UI** — new interaction layer, animations, and narrative justification; conflicts with core pillar of the gameplay loop(return to the player's room).

- **Cloud LLM for speed** — module and POE emphasize **local** Ollama; switching stacks would rewrite ethics/disclosure sections and build assumptions.

- **Perfect LLM prose without CONTEXT** — infeasible with 3B models; we chose **grounded CONTEXT + HUD** instead of chasing 7B with latency.



## Conflicts with design goals

**Core pillars we protected:**

1. **Single computer setup** as the hub for messenger + hacking.  

2. **Local inference** (privacy, reproducibility, brief alignment).  

3. **Coercive fiction** — we did not soften **H** into a helpful quest-giver despite “friendly greeting” slips; we fixed tone via prompt + post-process, not by changing the fantasy to a delivery sim.

**Tab for phone access** directly conflicts with (1). **Faster cloud API** would conflict with (2). **Removing hostage stakes** would conflict with (3).



## Scope too large

| Suggestion | Why “too big” for Part 3 |

| Tab-accessible terminal | New UI mode, input routing, narrative retcon. |

| Full player scale / animation pass | Art pipeline across kit assets; not a one-script fix. |

| Rewriting maze as separate product | Attendees wanted stability, not a new minigame. |

We logged these as **future exploration** in [`plan.md`](plan.md) / reflection, not as sprint items.



## Subjective or contradictory feedback

- **Timer:** Everyone wanted **more** time; no one asked to remove time pressure entirely — subjective “how much” but **directionally consistent**.  

- **AI model:** “Smaller/faster” vs “H must feel scary” — tension resolved by **smaller model + stricter CONTEXT + prompt guards**, accepting slightly blunter prose.  

- **UI density:** We feared too many HUD elements; testers liked hints and wanted **more** objective clarity (objective HUD). Subjective preference favored **clarity over minimalism**.

When feedback conflicts, we prioritized **completion of the core loop** over aesthetic minimalism.

---

## Evaluation of feasibility

| Change | Tools / skills | Performance | Core experience risk |

| 160s timer | Unity Inspector + one script | None | Low — more breathing room, same tension curve |

| Objective HUD | UGUI/TMP, new script | Negligible | Low — supports fiction (remember apartment) |

| Settings menu | UI + `PlayerPrefs` | None | Low — standard FPS affordance |

| `llama3.2:3b` | Ollama pull + default field | **Major win** on CPU laptops | Medium — watch for tone drift; mitigated by CONTEXT |

| Pickup sync in CONTEXT | C# + spawn labels | None | **High value** — fixes trust break |

| DDOL restart rebind | Advanced Unity lifecycle | None if correct | **Critical** — without it, build demo fails |

**Performance:** Meetup machines implied CPU-bound inference. Moving from **mistral:7b-instruct** to **llama3.2:3b** was realistic, documented in [`ollama-plan.md`](ollama-plan.md), and aligned with attendee expectations. We explicitly accept a trade-off: slightly less literary menace for **responsive** chat during a live gameplay.

**Would implementing everything compromise the core experience?** Yes — phone UI and stripping coercion would have turned Killer Complex into a generic delivery game. We implemented **clarity and reliability** layers that reinforce the existing fantasy.



## Final judgement

### Feedback that shaped refinements

| Theme | Shaped changes |

| Timer too short | **160s** base; pause while PC open |

| Forget room / pickup | **DeliveryObjectiveHud** + authoritative pickup in CONTEXT |

| LLM / spawn mismatch | Removed lobby default; spawn label per leg |

| Sensitivity (all attendees) | **Esc → Pause → Settings** |

| Faster model (all attendees) | **`llama3.2:3b`** default |

| Package visibility | Center banner + pickup spin/bob |

| Restart / build | DDOL merge, zone registration, Ollama reconnect, session reset |

See [`refinements-changes.md`](refinements-changes.md) (2026-06-18) for file-level trace.

### Feedback we declined and why

- **Tab phone** — design pillar conflict + scope.  

- **Full player height pass** — Player needs to feel vulnerable.  

- **Narrative softening** — not requested; would weaken POE intent for LLM integration.

Declining feedback with **written rationale** is as important as implementing 160s — the brief rewards critical judgement, not compliance.



## Critique and iteration in AI-driven development

External playtest changed our working model:

1. **Prompts are not authoritative** — **game state in `[CONTEXT]`** is. Testers punished desync harder with playing around with the LLM persona.  

2. **Post-processing is legitimate** — stripping `Job:` quest-log lines and fixing `my wife` → `your wife` is shipping hygiene, this is blackmail not “cheating.”

3. **AI-assisted implementation (Cursor) after critique** — fast iteration on meetup priorities, but **human verification in build** remained mandatory; Editor-only success was a false signal.  

4. **Iteration loop** — meetup → [`feedback-summary.md`](feedback-summary.md) → critical filtering (this doc) → code → [`prompts-used.md`](prompts-used.md) / [`ollama-plan.md`](ollama-plan.md) → re-playtest.
