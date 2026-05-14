using UnityEngine;

/// <summary>
/// Optional override for world interact prompts. Add to the same hierarchy as the collider the player looks at
/// (or a parent). When present, it wins over built-in labels for doors, computers, and delivery items.
/// </summary>
public class Interactable : MonoBehaviour
{
    [SerializeField] private string promptText = "[E] Interact";
    [Tooltip("If set, the prompt is anchored here; otherwise the ray hit point + surface offset is used.")]
    [SerializeField] private Transform worldAnchor;

    public string PromptText => promptText;

    public Vector3 GetWorldPromptPosition(RaycastHit surfaceHit) =>
        worldAnchor != null ? worldAnchor.position : surfaceHit.point + surfaceHit.normal * 0.12f;
}
