# Critical engagement with feedback — Part 3

**Project:** Killer Complex  
**Event:** Joburg Game Dev Meetup  
**Word count:** ~580

---

## What I expected

Before the meetup I assumed external testers would focus on **LLM tone** (H feeling repetitive or breaking character), **maze difficulty**, and whether the hostage-fiction was clear. I expected at least one comment on narrative discomfort, and I thought the hacking terminal would read as a strong differentiator with little criticism.

## What surprised me

The most common feedback was **practical pacing and comfort**: a **160-second** delivery budget, **mouse sensitivity**, and a **smaller Ollama model** — all three attendees raised these independently. I did not expect restart-only bugs (hacking icon unlocked early, maze state, LLM disconnect in **build**) to surface so clearly; Editor play had masked some DDOL/session issues. Attendees also cared less about insulting H and more about **whether they could complete a leg without forgetting the apartment number** after closing the PC.

Critique also landed on strengths I underplayed: several people said the **AI loop itself** worked and that **E**-interact and the **control hints** helped — my worry about “too much UI” was wrong for this audience.

## Alignment with expectations

**Aligned:** friction at the computer (reading H, then leaving desk) mattered; deferring the urgency timer until after the PC session was validated indirectly when Attendee B said the timer felt unfair during hacking.

**Misaligned:** I expected LLM personality to dominate; instead **ground truth** mattered — when H said “lobby” but the package spawned in the kitchen, trust broke faster than a stiff sentence. That pushed **authoritative pickup labels in CONTEXT** and an **objective HUD**, not just prompt tuning.

## What I implemented

| Feedback | Action |
|--------|--------|
| Timer too short → **160s** base | `DeliveryUrgencyTimer.initialLegTimeSeconds = 160` |
| Forget room / pickup location | `DeliveryObjectiveHud` + `CurrentPickupLocationLabel` in LLM CONTEXT |
| H says lobby vs random spawn | Removed lobby/reception default; spawn label per leg |
| Mouse sensitivity (×3 attendees) | `GameSettingsMenu` via **Esc → Pause → Settings** |
| Smaller/faster model | **`llama3.2:3b`** default; documented in `ollama-plan.md` |
| Package delivered hard to see | Center-screen banner on `GlobalNotificationHud` |
| Package hard to find | `DeliveryItem` spin/bob while active |
| Restart: hacking / maze / LLM | `GameplaySessionReset` on scene load; desktop + maze reset |
| Timer after hack while at PC | `TryCommitDeferredLegCountdown` waits if computer still open |
| Hack notification | Existing toast flags audited on maze/hack paths |

## What I declined or deferred

**Tab phone terminal (Attendee C):** Would re-architect input, break the “trapped at home computer” fiction, and duplicate messenger UX. Documented as future exploration, not this POE scope.

**Player height (Attendee A):** Art/camera pass — quick tweak possible but lower priority than timer/HUD/restart fixes before submission.

**Per-leg timer still shrinks** after completed legs: I extended the **base** to 160s but kept escalation — attendees asked for “more time,” not removal of pressure entirely.

## Feasibility

All implemented items fit **Unity + local Ollama**: no cloud API, no new packages. `llama3.2:3b` trades some menace in prose for speed and ~2 GB footprint — acceptable with **grounded CONTEXT**. DDOL `DeliveryManager` on `GlobalNotificationHUD` required explicit `ResetRunStateForNewPlaySession` and desktop/maze hooks — feasible but easy to miss without external playtest.

## Final judgement

The meetup feedback **re-prioritized polish over novelty**: clarity (objective HUD, timer, sensitivity) and **trust** (pickup location sync) beat feature creep (phone UI). I integrated what improved the vertical slice without diluting the computer-hub design. External critique was blunter and more **task-focused** than academic rubric feedback — which is closer to industry playtests. Disagreeing with Tab-phone feedback, with clear design rationale, was as important as implementing 160s. This process changed how I treat LLM output: **game state must be in CONTEXT**, not implied in the system prompt alone.
