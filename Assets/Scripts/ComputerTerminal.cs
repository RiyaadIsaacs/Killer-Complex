using UnityEngine;
using UnityEngine.InputSystem;

// Computer terminal: when the player interacts, opens a UI screen and disables player movement until closed.
public class ComputerTerminal : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private PlayerController player;
    [SerializeField] private GameObject computerScreenRoot;

    [Header("Cursor")]
    [Tooltip("When opening the terminal, unlock the cursor and show it; restore previous state on close.")]
    [SerializeField] private bool unlockCursorWhenOpen = true;

    private bool isOpen;
    private CursorLockMode savedLockMode;
    private bool savedCursorVisible;

    public bool IsOpen => isOpen;

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
    }

    public void CloseTerminal()
    {
        if (!isOpen)
            return;

        isOpen = false;

        if (computerScreenRoot != null)
            computerScreenRoot.SetActive(false);

        if (player != null)
            player.enabled = true;

        if (unlockCursorWhenOpen)
        {
            Cursor.lockState = savedLockMode;
            Cursor.visible = savedCursorVisible;
        }
    }
}
