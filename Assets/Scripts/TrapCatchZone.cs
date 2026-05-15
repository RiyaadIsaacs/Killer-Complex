using UnityEngine;

/// <summary>
/// Put on a child of the trap prefab with a <b>trigger</b> collider (larger than the mesh).
/// When any part of this zone overlaps the player, notifies <see cref="FallingTrap"/>.
/// </summary>
[RequireComponent(typeof(Collider))]
public class TrapCatchZone : MonoBehaviour
{
    [Tooltip("If unset, uses FallingTrap on a parent.")]
    [SerializeField] private FallingTrap trap;

    [SerializeField] private string playerTag = "Player";

    void Awake()
    {
        if (trap == null)
            trap = GetComponentInParent<FallingTrap>();

        var col = GetComponent<Collider>();
        if (col != null)
            col.isTrigger = true;
    }

    void Reset()
    {
        var col = GetComponent<Collider>();
        if (col != null)
            col.isTrigger = true;
    }

    void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag(playerTag))
            return;

        if (trap == null)
        {
            Debug.LogWarning($"{nameof(TrapCatchZone)} on {name}: No {nameof(FallingTrap)} on parent.", this);
            return;
        }

        trap.OnPlayerCaught(other);
    }

    void OnTriggerStay(Collider other)
    {
        // Catches fast movement / thin triggers when Enter is missed for a frame.
        if (!other.CompareTag(playerTag))
            return;

        if (trap != null)
            trap.OnPlayerCaught(other);
    }
}
