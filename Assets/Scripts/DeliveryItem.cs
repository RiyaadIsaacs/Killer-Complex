using UnityEngine;

// Pickup item for delivery: when the player interacts with this item, it registers a pickup with the manager.
public class DeliveryItem : MonoBehaviour
{
    [Tooltip("If true, this GameObject is disabled in Awake so nothing shows until ActivateForDelivery.")]
    [SerializeField] private bool startInactive = true;

    [Tooltip("Optional child mesh/collider to toggle instead of the whole GameObject.")]
    [SerializeField] private GameObject visualRoot;

    [Tooltip("When false, only the optional visual root is toggled; this GameObject stays active.")]
    [SerializeField] private bool affectWholeGameObject = true;

    [Tooltip("Same manager that references this item as its reception package (required for Interact pickup).")]
    [SerializeField] private DeliveryManager deliveryManager;

    public bool IsAssignedTo(DeliveryManager manager) => deliveryManager == manager;

    public void BindDeliveryManager(DeliveryManager manager)
    {
        if (manager != null)
            deliveryManager = manager;
    }

    [Header("Highlight while active")]
    [SerializeField] private bool spinWhileActive = true;
    [SerializeField] private float spinDegreesPerSecond = 45f;
    [SerializeField] private float bobAmplitude = 0.08f;
    [SerializeField] private float bobFrequency = 1.6f;

    Transform _highlightTransform;
    Vector3 _baseLocalPosition;
    bool _highlightActive;

    private void Awake()
    {
        if (startInactive)
            Deactivate();
    }

    public void ActivateForDelivery()
    {
        if (affectWholeGameObject)
            gameObject.SetActive(true);

        if (visualRoot != null)
            visualRoot.SetActive(true);

        _highlightTransform = visualRoot != null ? visualRoot.transform : transform;
        _baseLocalPosition = _highlightTransform.localPosition;
        _highlightActive = true;
    }

    public void Deactivate()
    {
        _highlightActive = false;

        if (visualRoot != null)
            visualRoot.SetActive(false);

        if (affectWholeGameObject)
            gameObject.SetActive(false);
    }

    void Update()
    {
        if (!_highlightActive || _highlightTransform == null)
            return;

        if (spinWhileActive)
            _highlightTransform.Rotate(Vector3.up, spinDegreesPerSecond * Time.deltaTime, Space.World);

        if (bobAmplitude > 0f)
        {
            var p = _baseLocalPosition;
            p.y += Mathf.Sin(Time.time * bobFrequency * Mathf.PI * 2f) * bobAmplitude;
            _highlightTransform.localPosition = p;
        }
    }

    // Sends a message via a raycast to call interact.
    public void Interact()
    {
        if (deliveryManager == null)
            return;
        deliveryManager.TryRegisterPackagePickup(this);
    }
}
