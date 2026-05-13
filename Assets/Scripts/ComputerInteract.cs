using UnityEngine;

/// <summary>
/// Raycast interact entry point for a world computer. Put on the object the player looks at (with a collider),
/// or a parent of that collider. <see cref="PlayerController"/> uses <c>SendMessageUpwards("Interact")</c>.
/// Forwards to <see cref="ComputerTerminal"/> on the same GameObject, in parents, or via an assigned reference.
/// </summary>
public class ComputerInteract : MonoBehaviour
{
    [SerializeField] private ComputerTerminal computerTerminal;

    public void Interact()
    {
        if (computerTerminal == null)
            computerTerminal = GetComponent<ComputerTerminal>();
        if (computerTerminal == null)
            computerTerminal = GetComponentInParent<ComputerTerminal>();

        if (computerTerminal == null)
        {
            Debug.LogWarning($"{nameof(ComputerInteract)} on {name}: Assign or add a {nameof(ComputerTerminal)}.", this);
            return;
        }

        computerTerminal.Interact();
    }
}
