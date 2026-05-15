using UnityEngine;

/// <summary>
/// Drops onto the player. Bad ending fires when a child <see cref="TrapCatchZone"/> overlaps the player
/// (add a child with a trigger collider sized around the trap). Spawned by <see cref="TrapTriggerZone"/>.
/// </summary>
public class FallingTrap : MonoBehaviour
{
    [Header("Fall")]
    [Tooltip("Used when there is no Rigidbody, or Rigidbody is kinematic.")]
    [SerializeField] private float fallSpeed = 14f;

    [SerializeField] private bool useGravityWhenRigidbodyPresent = true;

    [Header("Catch zone")]
    [Tooltip("Optional. If unset, the first TrapCatchZone under this object is used.")]
    [SerializeField] private TrapCatchZone catchZone;

    [Header("Bad ending")]
    [SerializeField] private bool runBadEndingDoorSetup = true;

    [SerializeField] private bool revealBadEndingOverlay = true;

    [Tooltip("If deliveries are finished and Ollama is in scene, send the trap line so H can post the final beat.")]
    [SerializeField] private bool sendOllamaTrapMessageWhenEligible = true;

    [SerializeField] private string trapOllamaPlayerLine = "Your trap got me.";

    Rigidbody _rigidbody;
    bool _badEndingFired;

    void Awake()
    {
        _rigidbody = GetComponent<Rigidbody>();
        if (_rigidbody != null && useGravityWhenRigidbodyPresent)
        {
            _rigidbody.useGravity = true;
            _rigidbody.isKinematic = false;
        }

        if (catchZone == null)
            catchZone = GetComponentInChildren<TrapCatchZone>(true);
    }

    void Update()
    {
        if (_badEndingFired)
            return;

        if (_rigidbody != null && !_rigidbody.isKinematic)
            return;

        transform.position += Vector3.down * (fallSpeed * Time.deltaTime);
    }

    /// <summary>Called by <see cref="TrapCatchZone"/> when the catch volume touches the player.</summary>
    public void OnPlayerCaught(Collider playerCollider)
    {
        if (_badEndingFired)
            return;

        _badEndingFired = true;

        var player = playerCollider != null
            ? playerCollider.GetComponentInParent<PlayerController>()
            : null;
        if (player == null)
            player = FindFirstObjectByType<PlayerController>();

        if (player != null)
            player.enabled = false;

        Time.timeScale = 1f;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        var orchestrator = BadEndingOrchestrator.Instance
            ?? FindFirstObjectByType<BadEndingOrchestrator>(FindObjectsInactive.Include);

        if (orchestrator != null)
            orchestrator.TriggerPlayerCaughtByTrap(runBadEndingDoorSetup, revealBadEndingOverlay);
        else
            Debug.LogWarning(
                $"{nameof(FallingTrap)}: No {nameof(BadEndingOrchestrator)} in scene — add one with the bad-ending canvas assigned.",
                this);

        if (sendOllamaTrapMessageWhenEligible)
            TrySendTrapOllamaMessage();

        if (_rigidbody != null)
            _rigidbody.isKinematic = true;
    }

    void TrySendTrapOllamaMessage()
    {
        var dm = FindFirstObjectByType<DeliveryManager>(FindObjectsInactive.Include);
        if (dm == null || !dm.FinishedAllConfiguredDeliveryLegs)
            return;

        var ollama = FindFirstObjectByType<OllamaConnector>(FindObjectsInactive.Include);
        if (ollama == null)
            return;

        ollama.SendToOllama(trapOllamaPlayerLine);
    }
}
