# Technical setup — Killer Complex (Part 2 + Ollama)

## 1. Unity

1. Install **Unity Hub**.
2. Install editor version **6000.3.15f1** (see `ProjectSettings/ProjectVersion.txt` if this file drifts).
3. Hub → **Add** → select the `Killer-Complex` folder.
4. Open the project; allow packages to resolve.
5. Open **`Assets/Scenes/Main Game.unity`** for the main apartment slice, or **`Assets/Scenes/Tester scene.unity`** for a smaller scene used to exercise new gameplay/UI code in isolation.

---

## 2. Ollama (local LLM)

1. Download and install **Ollama** from [https://ollama.com](https://ollama.com).
2. Confirm it runs: open a terminal and run `ollama --version`.
3. Pull a model (example — pick one your hardware can run):

   ```bash
   ollama pull llama3.2
   ```

4. Keep the **daemon** running (Ollama runs in the background). Default API: **`http://127.0.0.1:11434`**.

5. Quick test (optional):

   ```bash
   ollama run llama3.2 "Say hello in one sentence."
   ```

Document the **exact model tag** your team ships with in **`ollama-plan.md`**.

---

## 3. Unity ↔ Ollama

- Integration is **not yet in code** as of doc creation — when `OllamaChatClient` exists, this section should list:
  - Endpoint used (`/api/chat` recommended).
  - Any ScriptableObject path for host override.
  - Timeout seconds.

---

## 4. Controls (current prototype)

| Action | Binding |
|--------|---------|
| Move | WASD |
| Look | Mouse |
| Sprint | Left Shift (toggle) |
| Jump | Space |
| Interact | *(check `InputSystem_Actions` — often E)* |

---

## 5. Machine specs (fill in for markers)

| Machine | CPU | RAM | GPU | Notes |
|---------|-----|-----|-----|-------|
| Dev A | | | | |
| Dev B | | | | |

---

## 6. Troubleshooting

| Issue | Things to try |
|-------|----------------|
| Unity version mismatch | Install exact version from `ProjectVersion.txt`. |
| Ollama connection refused | Start Ollama; check nothing else uses port **11434**; try `127.0.0.1` not `localhost` if IPv6 oddities. |
| Model too slow | Smaller quant / smaller model; document in `ollama-plan.md`. |
