using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

/// <summary>
/// In-world hacking UI: <b>Hack</b> opens a procedural maze breach sim. Each maze exit adds
/// <see cref="mazeWinProgressPercent"/> to the decryption bar; at 100% <see cref="OnHackSuccessful"/> runs.
/// </summary>
public class HackingTerminalPanel : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private Slider decryptionSlider;
    [SerializeField] private TMP_Text decryptionStatusLabel;
    [SerializeField] private Button hackButton;
    [SerializeField] private TMP_Text consoleOutput;

    [Header("Behaviour")]
    [SerializeField, Range(1f, 40f)] private float hackProgressPercentPerClick = 8f;
    [Tooltip("Decryption slider increase for each maze completed without hitting a bomb.")]
    [SerializeField, Range(1f, 50f)] private float mazeWinProgressPercent = 10f;
    [SerializeField] private OllamaConnector ollamaConnector;

    [Header("Events")]
    [SerializeField] private UnityEvent onHackSuccessful;

    private bool _hackComplete;
    private HackingMazeMinigame _mazeMinigame;

    private void Awake()
    {
        _mazeMinigame = GetComponent<HackingMazeMinigame>();
        if (_mazeMinigame == null)
            _mazeMinigame = gameObject.AddComponent<HackingMazeMinigame>();
        _mazeMinigame.Initialize(this);

        if (decryptionSlider != null)
        {
            decryptionSlider.minValue = 0f;
            decryptionSlider.maxValue = 100f;
            decryptionSlider.wholeNumbers = true;
            decryptionSlider.value = 0f;
        }

        if (hackButton != null)
            hackButton.onClick.AddListener(OnHackClicked);

        RefreshStatusLabel();
        AppendConsoleLine("> Session opened.");
        AppendConsoleLine("> Awaiting breach sim (Hack).");
    }

    private void OnDestroy()
    {
        if (hackButton != null)
            hackButton.onClick.RemoveListener(OnHackClicked);
    }

    private void OnHackClicked()
    {
        if (_hackComplete)
            return;

        if (_mazeMinigame != null)
        {
            if (_mazeMinigame.IsOpen)
                return;
            _mazeMinigame.OpenAndRegenerate();
            return;
        }

        if (decryptionSlider == null)
            return;

        float next = Mathf.Min(100f, decryptionSlider.value + hackProgressPercentPerClick);
        decryptionSlider.value = next;
        AppendConsoleLine($"> Inject… {next:F0}% decrypted.");

        RefreshStatusLabel();

        if (next >= 100f)
            OnHackSuccessful();
    }

    private void RefreshStatusLabel()
    {
        if (decryptionStatusLabel == null || decryptionSlider == null)
            return;

        decryptionStatusLabel.text = $"Decryption Status: {decryptionSlider.value:F0}%";
    }

    /// <summary>Current decryption percent (0–100). Used by the maze to scale difficulty per round.</summary>
    public float GetHackProgressPercent()
    {
        return decryptionSlider != null ? decryptionSlider.value : 0f;
    }

    /// <summary>Called when the player clears a maze round. Adds progress; at 100% triggers full hack completion.</summary>
    public void ApplyMazeRoundWin()
    {
        if (_hackComplete || decryptionSlider == null)
            return;

        var next = Mathf.Min(100f, decryptionSlider.value + mazeWinProgressPercent);
        decryptionSlider.value = next;
        RefreshStatusLabel();
        AppendConsoleLine($"> Segment decrypted +{mazeWinProgressPercent:F0}% (now {next:F0}%).");

        if (next >= 100f)
            OnHackSuccessful();
    }

    /// <summary>Maze difficulty step based on completed progress (0 before first win, 1 after first +10%, …).</summary>
    public int GetMazeTier()
    {
        if (decryptionSlider == null)
            return 0;
        var step = Mathf.Max(1f, mazeWinProgressPercent);
        return Mathf.Clamp(Mathf.FloorToInt(decryptionSlider.value / step), 0, 99);
    }

    public float GetMazeWinStep() => mazeWinProgressPercent;

    public void AppendConsoleLine(string line)
    {
        if (consoleOutput == null || string.IsNullOrEmpty(line))
            return;

        if (string.IsNullOrEmpty(consoleOutput.text))
            consoleOutput.text = line;
        else
            consoleOutput.text += "\n" + line;

        consoleOutput.ForceMeshUpdate();
    }

    /// <summary>
    /// Called when decryption reaches 100%. Invokes <see cref="onHackSuccessful"/> and sends the reversal beat to Ollama.
    /// </summary>
    public void OnHackSuccessful()
    {
        if (_hackComplete)
            return;

        _hackComplete = true;

        if (decryptionSlider != null)
            decryptionSlider.value = 100f;

        if (hackButton != null)
            hackButton.interactable = false;

        RefreshStatusLabel();
        AppendConsoleLine("> DECRYPTION COMPLETE — uplink sealed.");

        onHackSuccessful?.Invoke();

        if (ollamaConnector != null)
            ollamaConnector.SendHackReversalPrompt();
        else
            Debug.LogWarning($"{nameof(HackingTerminalPanel)}: Assign OllamaConnector to push the reversal prompt.", this);
    }
}
