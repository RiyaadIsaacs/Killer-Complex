using UnityEngine;
using UnityEngine.SceneManagement;

#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// Attach to your main menu Canvas (or a child). Wire UI buttons to <see cref="PlayGame"/> and <see cref="QuitGame"/>.
/// </summary>
public class MainMenuScreen : MonoBehaviour
{
    [Header("Scenes")]
    [Tooltip("Scene to load when the player chooses Play (must be in File → Build Settings).")]
    [SerializeField] private string gameSceneName = "Main Game";

    void Start()
    {
        Time.timeScale = 1f;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    /// <summary>Hook to Play / Start button On Click.</summary>
    public void PlayGame()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(gameSceneName);
    }

    /// <summary>Hook to Quit button On Click.</summary>
    public void QuitGame()
    {
        Time.timeScale = 1f;
#if UNITY_EDITOR
        EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}
