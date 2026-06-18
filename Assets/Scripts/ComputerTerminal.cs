using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

// Computer terminal: when the player interacts, opens a UI screen and disables player movement until closed.
public class ComputerTerminal : MonoBehaviour
{
    static readonly string[] DefaultArrowHintNames = { "Arrow", "Arrow (2)", "Arrow(1)" };

    [Header("References")]
    [SerializeField] private PlayerController player;
    [SerializeField] private GameObject computerScreenRoot;

    [Header("Discovery hints")]
    [Tooltip("World objects (e.g. arrows) hidden after the player uses this computer.")]
    [SerializeField] private GameObject[] discoveryHints;
    [Tooltip("When no hints are assigned, finds root objects named Arrow / Arrow (2) / Arrow(1) near this terminal.")]
    [SerializeField] private bool autoFindNamedArrowHints = true;
    [SerializeField] private float autoFindMaxDistance = 20f;

    [Header("Cursor")]
    [Tooltip("When opening the terminal, unlock the cursor and show it; restore previous state on close.")]
    [SerializeField] private bool unlockCursorWhenOpen = true;

    private bool isOpen;
    private bool discoveryHintsHidden;
    private CursorLockMode savedLockMode;
    private bool savedCursorVisible;

    public bool IsOpen => isOpen;

    private void Awake()
    {
        ResolveDiscoveryHints();
    }

    private void Update()
    {
        if (!isOpen || Keyboard.current == null)
            return;

        if (Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            if (HackingMazeMinigame.TryConsumeEscape())
                return;
            CloseTerminal();
        }
    }

    // Sends a message via a raycast to call interact.
    public void Interact()
    {
        if (isOpen)
            return;

        if (player == null)
        {
            Debug.LogWarning($"{nameof(ComputerTerminal)} on {name}: PlayerController is not assigned.", this);
            return;
        }

        if (computerScreenRoot == null)
        {
            Debug.LogWarning($"{nameof(ComputerTerminal)} on {name}: Computer screen root is not assigned.", this);
            return;
        }

        isOpen = true;

        if (unlockCursorWhenOpen)
        {
            savedLockMode = Cursor.lockState;
            savedCursorVisible = Cursor.visible;
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        player.enabled = false;
        computerScreenRoot.SetActive(true);

        DeliveryUrgencyTimer.NotifyComputerSessionOpened();
        GlobalNotificationHud.SetTopLeftNotificationsVisibleOnHud(false);

        var desktopUi = computerScreenRoot.GetComponentInChildren<ComputerDesktopUI>(true);
        desktopUi?.OnComputerSessionOpened();

        HideDiscoveryHints();
    }

    public void ResetDiscoveryHintsForNewSession()
    {
        ResolveDiscoveryHints();
        discoveryHintsHidden = false;

        if (discoveryHints == null)
            return;

        foreach (var hint in discoveryHints)
        {
            if (hint != null)
                hint.SetActive(true);
        }
    }

    void HideDiscoveryHints()
    {
        if (discoveryHintsHidden)
            return;

        ResolveDiscoveryHints();
        if (discoveryHints == null || discoveryHints.Length == 0)
            return;

        foreach (var hint in discoveryHints)
        {
            if (hint != null)
                hint.SetActive(false);
        }

        discoveryHintsHidden = true;
    }

    void ResolveDiscoveryHints()
    {
        if (discoveryHints != null && discoveryHints.Length > 0)
            return;

        if (!autoFindNamedArrowHints)
            return;

        var found = new List<GameObject>();
        var maxSq = autoFindMaxDistance * autoFindMaxDistance;
        var origin = transform.position;
        var scene = gameObject.scene;

        if (!scene.IsValid())
            scene = SceneManager.GetActiveScene();

        foreach (var root in scene.GetRootGameObjects())
        {
            if (!IsDefaultArrowHintName(root.name))
                continue;

            if ((root.transform.position - origin).sqrMagnitude > maxSq)
                continue;

            found.Add(root);
        }

        if (found.Count > 0)
            discoveryHints = found.ToArray();
    }

    static bool IsDefaultArrowHintName(string objectName)
    {
        for (var i = 0; i < DefaultArrowHintNames.Length; i++)
        {
            if (objectName == DefaultArrowHintNames[i])
                return true;
        }

        return false;
    }

    public void CloseTerminal()
    {
        if (!isOpen)
            return;

        isOpen = false;

        if (computerScreenRoot != null && computerScreenRoot.activeSelf)
        {
            var desktopUi = computerScreenRoot.GetComponentInChildren<ComputerDesktopUI>(true);
            desktopUi?.OnComputerSessionClosed();
        }

        if (computerScreenRoot != null)
            computerScreenRoot.SetActive(false);

        if (player != null)
            player.enabled = true;

        if (unlockCursorWhenOpen)
        {
            Cursor.lockState = savedLockMode;
            Cursor.visible = savedCursorVisible;
        }

        DeliveryUrgencyTimer.NotifyComputerSessionClosed();
        GlobalNotificationHud.SetTopLeftNotificationsVisibleOnHud(true);
    }
}
