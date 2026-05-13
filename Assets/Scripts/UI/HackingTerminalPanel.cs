using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

/// <summary>
/// In-world hacking mini-game placeholder: raises decryption progress until complete, then
/// <see cref="OnHackSuccessful"/> notifies <see cref="OllamaConnector"/> (special reversal prompt).
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
    [SerializeField] private OllamaConnector ollamaConnector;

    [Header("Events")]
    [SerializeField] private UnityEvent onHackSuccessful;

    private bool _hackComplete;

    private void Awake()
    {
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
        AppendConsole("> Session opened.");
        AppendConsole("> Awaiting manual packet injection (Hack).");
    }

    private void OnDestroy()
    {
        if (hackButton != null)
            hackButton.onClick.RemoveListener(OnHackClicked);
    }

    private void OnHackClicked()
    {
        if (_hackComplete || decryptionSlider == null)
            return;

        float next = Mathf.Min(100f, decryptionSlider.value + hackProgressPercentPerClick);
        decryptionSlider.value = next;
        AppendConsole($"> Inject… {next:F0}% decrypted.");

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

    private void AppendConsole(string line)
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
        AppendConsole("> DECRYPTION COMPLETE — uplink sealed.");

        onHackSuccessful?.Invoke();

        if (ollamaConnector != null)
            ollamaConnector.SendHackReversalPrompt();
        else
            Debug.LogWarning($"{nameof(HackingTerminalPanel)}: Assign OllamaConnector to push the reversal prompt.", this);
    }
}
