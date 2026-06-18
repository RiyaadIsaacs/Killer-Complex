using UnityEngine;

/// <summary>
/// Marks a <see cref="DeliveryManager"/> package spawn empty with a human-readable label for LLM CONTEXT and HUD.
/// </summary>
public class DeliveryPickupSpawnPoint : MonoBehaviour
{
    [Tooltip("Shown to the model and objective HUD (e.g. \"the kitchen\", \"your bedroom\"). If empty, derived from this object's name.")]
    [SerializeField] private string llmPickupLabel;

    public string GetPickupLabelForLlm()
    {
        if (!string.IsNullOrWhiteSpace(llmPickupLabel))
            return llmPickupLabel.Trim();

        return DeriveLabelFromObjectName(gameObject.name);
    }

    public static string DeriveLabelFromObjectName(string objectName)
    {
        if (string.IsNullOrWhiteSpace(objectName))
            return "somewhere in the apartment complex";

        var n = objectName.Trim().ToLowerInvariant();
        n = n.Replace("spawn", "").Trim();
        n = n.Replace("floor 1", "first floor hallway");
        n = n.Replace("floor 2", "second floor hallway");
        n = n.Replace("floor 3", "third floor hallway");

        if (n.Contains("bedroom"))
            return "your bedroom";
        if (n.Contains("kitchen"))
            return "the kitchen";
        if (n.Contains("living room"))
            return "the living room";
        if (n.Contains("dining"))
            return "the dining room";
        if (n.Contains("bathroom"))
            return "the bathroom";

        return "the " + n.Trim();
    }
}
