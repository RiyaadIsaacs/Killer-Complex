using UnityEngine;

/// <summary>
/// Place on a trigger volume in a hallway (or similar). When the player enters, spawns a
/// <see cref="FallingTrap"/> prefab above and lets it drop.
/// </summary>
[RequireComponent(typeof(Collider))]
public class TrapTriggerZone : MonoBehaviour
{
    [Header("Trap spawn")]
    [Tooltip("Prefab with FallingTrap + child TrapCatchZone (trigger volume). Rigidbody optional.")]
    [SerializeField] private GameObject trapPrefab;

    [Tooltip("If set, the trap spawns here. Otherwise uses this object's position + Spawn Height Offset.")]
    [SerializeField] private Transform spawnPoint;

    [SerializeField] private float spawnHeightOffset = 4f;

    [Header("Behaviour")]
    [SerializeField] private bool onlyTriggerOnce = true;

    [Tooltip("Tag on the player collider (must match your Player object).")]
    [SerializeField] private string playerTag = "Player";

    bool _hasTriggered;

    void Reset()
    {
        var col = GetComponent<Collider>();
        if (col != null)
            col.isTrigger = true;
    }

    void OnTriggerEnter(Collider other)
    {
        if (onlyTriggerOnce && _hasTriggered)
            return;

        if (!other.CompareTag(playerTag))
            return;

        if (trapPrefab == null)
        {
            Debug.LogWarning($"{nameof(TrapTriggerZone)} on {name}: Assign a trap prefab.", this);
            return;
        }

        _hasTriggered = true;
        var spawnPos = spawnPoint != null
            ? spawnPoint.position
            : transform.position + Vector3.up * spawnHeightOffset;

        Instantiate(trapPrefab, spawnPos, trapPrefab.transform.rotation);
    }
}
