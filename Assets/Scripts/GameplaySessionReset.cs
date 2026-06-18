using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Resets scene-bound gameplay UI/LLM state when a gameplay scene loads or restarts (DDOL managers may survive).
/// </summary>
public static class GameplaySessionReset
{
    public static void NotifyGameplaySceneLoaded(Scene scene)
    {
        if (string.Equals(scene.name, "Main Menu", System.StringComparison.OrdinalIgnoreCase))
            return;

        foreach (var maze in Object.FindObjectsByType<HackingMazeMinigame>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (maze != null)
                maze.ForceCloseForSessionReset();
        }

        foreach (var desktop in Object.FindObjectsByType<ComputerDesktopUI>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (desktop != null)
                desktop.ResetForNewGameplaySession();
        }

        foreach (var connector in Object.FindObjectsByType<OllamaConnector>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (connector != null)
                connector.ResetSessionStateForSceneLoad();
        }

        foreach (var terminal in Object.FindObjectsByType<ComputerTerminal>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (terminal != null && terminal.IsOpen)
                terminal.CloseTerminal();
        }
    }
}
