# Team rules — Killer Complex

## Repository

- **Branching:** Use feature branches (`feature/ollama-client`, `fix/delivery-softlock`, etc.); merge via PR or paired review as agreed.
- **Never commit:** `Library/`, `Temp/`, `Logs/`, `UserSettings/` — already in `.gitignore`.
- **Unity version:** Match `ProjectSettings/ProjectVersion.txt` (currently **6000.3.15f1**).

## Before every push to GitHub (required checklist)

1. **`prompts-used.md`** — Append any **new or changed** prompts:
   - In-game Ollama **system** / **user** templates and variants you tested.
   - Optional: major **Cursor / AI-assistant** prompts that shaped code or design (short excerpt + outcome).
2. **`refinements-changes.md`** — One dated entry for this session: scope changes, bug fixes, what was AI-assisted vs hand-written.
3. **`ollama-plan.md`** — Update if model, endpoint, parsing, data flow, or prompt **contract** changed.
4. **`README.md`** — Bump if install steps, controls, or dependencies changed.
5. **`feedback-summary.md`** — Note which meetup items were addressed when closing feedback loops.

If a file did not change this session, note “no change” in `refinements-changes.md` or skip only when truly nothing documentation-relevant happened.

## Readme naming

- Canonical project readme: **`README.md`** (GitHub default).
- If submission requires lowercase **`readme.md`**, duplicate or rename **only in the submission zip**, unless the team standardises on one filename after confirming with the lecturer.

## Academic integrity (IIE / POE)

- Do **not** paste large chunks of the official assessment PDF into the repo.
- **`refinements-changes.md`**: be honest about AI-assisted edits and what you validated in Unity/Ollama.
- **LLM Integration Report** (separate deliverable): your own analysis; tools assist, you own the reflection.

## Code style

- Match existing C# in the project (naming, no drive-by refactors unrelated to the task).
- New LLM code: prefer `Assets/Scripts/LLM/` *(create when implementing)*.
