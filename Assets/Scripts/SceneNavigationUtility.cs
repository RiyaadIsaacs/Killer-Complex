using UnityEngine;

/// <summary>
/// Shared prep before <see cref="UnityEngine.SceneManagement.SceneManager.LoadScene"/> so time scale
/// and (in the Editor) Inspector selection do not reference objects that are about to be unloaded.
/// </summary>
public static class SceneNavigationUtility
{
    public static void PrepareForSceneLoad()
    {
        Time.timeScale = 1f;
#if UNITY_EDITOR
        // Avoid SerializedObjectNotCreatableException when the Inspector still targets Main Menu UI
        // or a duplicate scene HUD that is destroyed on load.
        UnityEditor.Selection.activeObject = null;
#endif
    }
}
