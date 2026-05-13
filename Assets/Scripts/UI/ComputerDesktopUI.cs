using System;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

/// <summary>
/// Desktop-style UI: opens messenger or hacking terminal panels from icon buttons;
/// each panel has a close (X) control that hides only that panel.
/// Optional shutdown control closes the whole computer session via <see cref="ComputerTerminal.CloseTerminal"/>.
/// </summary>
public class ComputerDesktopUI : MonoBehaviour
{
    [Header("Icon buttons")]
    [SerializeField] private Button messengerIconButton;
    [FormerlySerializedAs("deliveriesIconButton")]
    [SerializeField] private Button hackingTerminalIconButton;

    [Header("Panels")]
    [SerializeField] private GameObject messengerPanel;
    [FormerlySerializedAs("deliveriesPanel")]
    [SerializeField] private GameObject hackingTerminalPanel;

    [Header("Close (X) buttons")]
    [SerializeField] private Button messengerCloseButton;
    [FormerlySerializedAs("deliveriesCloseButton")]
    [SerializeField] private Button hackingTerminalCloseButton;

    [Header("Leave computer")]
    [Tooltip("Top-corner control that closes open panels and exits the computer (same as Escape).")]
    [SerializeField] private Button shutdownComputerButton;
    [Tooltip("If unset, a ComputerTerminal is searched in parents (works when the canvas is under the desk object).")]
    [SerializeField] private ComputerTerminal computerTerminal;

    private void Awake()
    {
        ApplyCrispTextCanvasSettings();

        if (messengerPanel != null)
            messengerPanel.SetActive(false);
        if (hackingTerminalPanel != null)
            hackingTerminalPanel.SetActive(false);

        if (messengerIconButton != null)
            messengerIconButton.onClick.AddListener(OpenMessengerPanel);
        if (hackingTerminalIconButton != null)
            hackingTerminalIconButton.onClick.AddListener(OpenHackingTerminalPanel);

        if (messengerCloseButton != null)
            messengerCloseButton.onClick.AddListener(CloseMessengerPanel);
        if (hackingTerminalCloseButton != null)
            hackingTerminalCloseButton.onClick.AddListener(CloseHackingTerminalPanel);

        if (shutdownComputerButton != null)
            shutdownComputerButton.onClick.AddListener(OnShutdownComputerClicked);
    }

    /// <summary>
    /// TextMeshPro SDF on UI needs extra canvas vertex channels; pixel snapping reduces soft edges from fractional scale.
    /// </summary>
    static void ApplyCrispTextCanvasSettings(Canvas canvas)
    {
        if (canvas == null)
            return;
        canvas.pixelPerfect = true;
        canvas.additionalShaderChannels |= AdditionalCanvasShaderChannels.TexCoord1
                                           | AdditionalCanvasShaderChannels.Normal
                                           | AdditionalCanvasShaderChannels.Tangent;
    }

    void ApplyCrispTextCanvasSettings() => ApplyCrispTextCanvasSettings(GetComponent<Canvas>());

    private void OnDestroy()
    {
        if (messengerIconButton != null)
            messengerIconButton.onClick.RemoveListener(OpenMessengerPanel);
        if (hackingTerminalIconButton != null)
            hackingTerminalIconButton.onClick.RemoveListener(OpenHackingTerminalPanel);
        if (messengerCloseButton != null)
            messengerCloseButton.onClick.RemoveListener(CloseMessengerPanel);
        if (hackingTerminalCloseButton != null)
            hackingTerminalCloseButton.onClick.RemoveListener(CloseHackingTerminalPanel);
        if (shutdownComputerButton != null)
            shutdownComputerButton.onClick.RemoveListener(OnShutdownComputerClicked);
    }

    public void OpenMessengerPanel()
    {
        if (messengerPanel != null)
            messengerPanel.SetActive(true);
    }

    public void CloseMessengerPanel()
    {
        if (messengerPanel != null)
            messengerPanel.SetActive(false);
    }

    public void OpenHackingTerminalPanel()
    {
        if (hackingTerminalPanel != null)
            hackingTerminalPanel.SetActive(true);
    }

    public void CloseHackingTerminalPanel()
    {
        if (hackingTerminalPanel != null)
            hackingTerminalPanel.SetActive(false);
    }

    [Obsolete("Use " + nameof(OpenHackingTerminalPanel) + " instead.")]
    public void OpenDeliveriesPanel() => OpenHackingTerminalPanel();

    [Obsolete("Use " + nameof(CloseHackingTerminalPanel) + " instead.")]
    public void CloseDeliveriesPanel() => CloseHackingTerminalPanel();

    private void OnShutdownComputerClicked()
    {
        CloseMessengerPanel();
        CloseHackingTerminalPanel();

        var terminal = computerTerminal != null ? computerTerminal : GetComponentInParent<ComputerTerminal>();
        if (terminal != null)
            terminal.CloseTerminal();
        else
            Debug.LogWarning(
                $"{nameof(ComputerDesktopUI)} on {name}: Assign {nameof(computerTerminal)} so the shutdown button can exit the computer.",
                this);
    }
}
