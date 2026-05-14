using UnityEngine;

/// <summary>
/// Maps a physics hit to a prompt string and world-space anchor for <see cref="InteractPromptHud"/>.
/// </summary>
public static class InteractPromptResolver
{
    public static bool TryResolve(RaycastHit hit, out string text, out Vector3 worldPos)
    {
        text = null;
        worldPos = hit.point + hit.normal * 0.12f;

        var interactable = hit.transform.GetComponentInParent<Interactable>();
        if (interactable != null)
        {
            text = interactable.PromptText;
            worldPos = interactable.GetWorldPromptPosition(hit);
            return !string.IsNullOrEmpty(text);
        }

        if (hit.transform.GetComponentInParent<ComputerInteract>() != null
            || hit.transform.GetComponentInParent<ComputerTerminal>() != null)
        {
            text = "[E] Use computer";
            return true;
        }

        if (hit.transform.GetComponentInParent<InteractDoor>() != null)
        {
            text = "[E] Door";
            return true;
        }

        if (hit.transform.GetComponentInParent<DeliveryItem>() != null)
        {
            text = "[E] Package";
            return true;
        }

        return false;
    }
}
