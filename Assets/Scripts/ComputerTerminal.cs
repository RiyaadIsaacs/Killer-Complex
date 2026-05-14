using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// World computer: interact opens a UI canvas and disables <see cref="PlayerController"/>.
/// Escape (or <see cref="CloseTerminal"/>) closes the UI and re-enables the player.
/// Put on the same hierarchy as the collider the player looks at, or a parent (SendMessageUpwards).
/// You can also use <see cref="ComputerInteract"/> on the collider object to forward to this component.
/// </summary>
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

    /// <summary>
    /// Invoked by <see cref="PlayerController"/> via SendMessageUpwards when the player uses interact.
    /// </summary>
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

    /// <summary>
    /// Closes the terminal. Call from UI buttons or other scripts if needed.
    /// </summary>
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
