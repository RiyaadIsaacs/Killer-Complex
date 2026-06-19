# Final Reflection Report — Part 3

**Module:** GADS7331 — Game Design 3A  
**Project:** Killer Complex  
**Student:** Riyaad Isaacs *(pair project with Joshua Moonsamy)*  
**Word count:** ~780  
**Submit:** Export this file to PDF or DOC for ARC upload.

---

## Professional engagement

For Part 3 I attended the lecturer-approved **Joburg Game Dev Meetup** and demonstrated **Killer Complex** to three people I had not met before: a 3D artist, a hobbyist game developer, and a programmer. Presenting to strangers outside the classroom was closer to a real indie playtest than a rubric review. I was nervous about live **Ollama** inference — replies can take several seconds on a laptop CPU — but I chose to demo the actual product rather than a scripted video, because the module asks us to engage with emerging technology honestly.

Before each session I explained that **H**’s dialogue is **machine-generated** from a local model, that the hostage scenario is **fiction**, and that no player data leaves the machine. That disclosure set expectations: testers watched for weird or stiff lines, but they also judged whether the AI loop felt like gameplay or a gimmick. The meetup confirmed that external audiences are less interested in lore essays and more interested in whether they can **complete a delivery leg** without confusion. That was uncomfortable at first — I had spent weeks tuning prompts — but it was exactly the kind of friction professional playtests surface early.

---

## Feedback integration

The feedback that changed the project most was practical rather than conceptual. All three attendees independently asked for a **longer delivery timer** (we shipped **160 seconds** per leg), **mouse sensitivity** in a settings menu, and a **smaller/faster local model** (**`llama3.2:3b`** instead of **`mistral:7b-instruct`**). Attendee C (programmer) also wanted clearer objectives after closing the PC, which led to the **delivery objective HUD** and stronger **CONTEXT** wiring so **H**’s apartment numbers match gameplay state. Attendee A surfaced **build-only restart bugs** — LLM disconnect after **Main Menu → Play**, hacking UI unlocking too early, maze state not resetting — issues that Editor play had hidden. Fixing those through **DontDestroyOnLoad** session resets and scene rebinding was more valuable than any single prompt tweak.

I integrated feedback that improved the vertical slice without diluting the design. I **declined** the suggestion to open the messenger on a **Tab phone UI**, because our core fiction is that the player is trapped at a home computer; a phone layer would duplicate UX and break that pillar. I **deferred** a player-height art pass because it was lower impact than timer, HUD, and stability work before submission. Documenting why I declined feedback was as important as implementing the 160s timer — the brief rewards critical judgement, not compliance. A later polish pass also addressed computer discoverability (arrow hints at the desk, hidden after first use) and audio feedback (door knock on successful delivery, walking loop while moving), which improved onboarding without changing the core loop.

---

## Collaboration with AI

Meetup feedback pushed me to treat the LLM as a **systems problem**, not a writing problem. When **H** said “lobby” but the package spawned elsewhere, testers lost trust faster than they cared about menacing prose. The fix was **authoritative `[CONTEXT]`** in `OllamaConnector` — pickup labels, destination apartments, delivery progress — plus HUD support so players do not have to memorise room numbers from chat alone. Prompt rules still matter (tone, hostage pronouns, no quest-log replies), but **game state must be in code**, not implied in the system string.

I used **Cursor** and local **Ollama** heavily after the meetup to implement priorities quickly: timer pause at the PC, settings menu, DDOL merge on scene reload, messenger display normalisation. AI-assisted coding sped up iteration, but it did not replace verification. Several fixes only appeared when I tested a **Windows build** and the **Main Menu → Play** round-trip — the same path meetup testers used. That changed how I work: prompts and generated C# both need a human pass, and “works in Editor” is not evidence for a POE.

---

## Ethics and professional considerations

**Killer Complex** uses coercion and hostage fiction. I do not treat that lightly. The meetup showed that testers focused on task clarity rather than ethical debate in a short demo, but as a designer I still owe players a **content warning** and clear **AI disclosure** (included in our README and stated at the demo). Running **Ollama locally** avoids sending player messages to a cloud API, which supports privacy and reproducibility for academic submission; it trades away the speed of hosted models, which meetup feedback made obvious.

In a studio context I would disclose generated dialogue in credits or settings, keep fiction separate from real-world harm, and avoid implying that model output is human-written. I own the **prompt design** and **CONTEXT schema**; the model owns unpredictable phrasing. Post-processing (stripping leaked context, correcting pronoun slips) is shipping hygiene, not deception — it keeps the fiction consistent when a 3B model drifts.

---

## Speculative technology

Local LLMs are a plausible indie toolchain for dialogue-heavy prototypes, but only when outputs are **grounded in game state**. Killer Complex treats the model as a reactive layer over a deterministic delivery sim, not as the sim itself. Looking forward, tools like on-device inference or richer context windows could reduce latency and drift; alternative UX (phone terminals, MR overlays) could widen access but would change the fiction we deliberately kept narrow. Part 3 convinced me that speculative tech in games is judged first on **reliability and clarity**, and only second on novelty.

---

*Related evidence: `feedback-summary.md`, `critical-feedback.md`, `refinements-changes.md` (2026-06-18 entries).*
