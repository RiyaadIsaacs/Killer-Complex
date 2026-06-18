using System;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

// Desktop computer UI.
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
    [Tooltip("If unset, the first ComputerTerminal is searched in parents (works when the canvas is under the desk object).")]
    [SerializeField] private ComputerTerminal computerTerminal;

    bool _badEndingDesktopMode;
    bool _badEndingDesktopLockPendingOnSessionClose;

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

        SetHackingTerminalIconAvailable(false);
    }

    // Configures the canvas for canvas text.
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
        if (_badEndingDesktopMode)
            return;
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
        if (hackingTerminalIconButton == null || !hackingTerminalIconButton.gameObject.activeSelf || !hackingTerminalIconButton.interactable)
            return;
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

    /// <summary>
    /// Called when the player sits at the PC. The hacking icon is hidden until <see cref="NotifyRemoteAccessEstablished"/> runs
    /// (after H's post-delivery "step away" Ollama line plus <c>Remote access established</c>).
    /// </summary>
    public void OnComputerSessionOpened()
    {
        if (_badEndingDesktopMode)
        {
            CloseHackingTerminalPanel();
            CloseMessengerPanel();
            ApplyBadEndingDesktopLayout();
            return;
        }

        CloseHackingTerminalPanel();
        SetHackingTerminalIconAvailable(false);
    }

    /// <summary>Called when the player leaves the PC; locks hacking again for the next session.</summary>
    public void OnComputerSessionClosed()
    {
        CloseHackingTerminalPanel();
        SetHackingTerminalIconAvailable(false);

        if (_badEndingDesktopLockPendingOnSessionClose)
        {
            _badEndingDesktopLockPendingOnSessionClose = false;
            _badEndingDesktopMode = true;
            CloseMessengerPanel();
            ApplyBadEndingDesktopLayout();
        }
    }

    /// <summary>Shows and enables the hacking terminal icon after H's break / step-away reply (see <c>OllamaConnector</c>).</summary>
    public void NotifyRemoteAccessEstablished()
    {
        if (_badEndingDesktopMode)
            return;
        SetHackingTerminalIconAvailable(true);
    }

    /// <summary>Locks the hacking dock when H returns to the messenger (closes panel and any active breach UI).</summary>
    public void RevokeRemoteAccess()
    {
        if (_badEndingDesktopMode)
            return;
        CloseHackingTerminalPanel();
        SetHackingTerminalIconAvailable(false);
    }

    /// <summary>
    /// After the final delivery trap message: restrict the desktop to shutdown-only. When <paramref name="deferRestrictedLayoutUntilPlayerClosesComputer"/> is true,
    /// messenger and icons stay available until the player closes the computer session so the in-flight AI line can appear in chat first.
    /// </summary>
    public void EnterBadEndingComputerMode(bool deferRestrictedLayoutUntilPlayerClosesComputer = false)
    {
        if (deferRestrictedLayoutUntilPlayerClosesComputer)
        {
            _badEndingDesktopLockPendingOnSessionClose = true;
            return;
        }

        _badEndingDesktopMode = true;
        _badEndingDesktopLockPendingOnSessionClose = false;
        CloseHackingTerminalPanel();
        CloseMessengerPanel();
        ApplyBadEndingDesktopLayout();
    }

    void ApplyBadEndingDesktopLayout()
    {
        if (messengerIconButton != null)
        {
            messengerIconButton.gameObject.SetActive(false);
            messengerIconButton.interactable = false;
        }

        if (hackingTerminalIconButton != null)
        {
            hackingTerminalIconButton.gameObject.SetActive(false);
            hackingTerminalIconButton.interactable = false;
        }

        if (shutdownComputerButton != null)
        {
            shutdownComputerButton.gameObject.SetActive(true);
            shutdownComputerButton.interactable = true;
        }
    }

    void SetHackingTerminalIconAvailable(bool available)
    {
        if (hackingTerminalIconButton == null)
            return;
        hackingTerminalIconButton.gameObject.SetActive(available);
        hackingTerminalIconButton.interactable = available;
    }

    /// <summary>Fresh gameplay session — hide hacking until remote access is earned again.</summary>
    public void ResetForNewGameplaySession()
    {
        _badEndingDesktopMode = false;
        _badEndingDesktopLockPendingOnSessionClose = false;
        CloseMessengerPanel();
        CloseHackingTerminalPanel();
        SetHackingTerminalIconAvailable(false);

        if (messengerIconButton != null)
        {
            messengerIconButton.gameObject.SetActive(true);
            messengerIconButton.interactable = true;
        }

        if (shutdownComputerButton != null)
        {
            shutdownComputerButton.gameObject.SetActive(true);
            shutdownComputerButton.interactable = true;
        }
    }

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
