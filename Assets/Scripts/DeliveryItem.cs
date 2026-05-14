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
    }

    public void Deactivate()
    {
        if (visualRoot != null)
            visualRoot.SetActive(false);

        if (affectWholeGameObject)
            gameObject.SetActive(false);
    }

    // Sends a message via a raycast to call interact.
    public void Interact()
    {
        if (deliveryManager == null)
            return;
        deliveryManager.TryRegisterPackagePickup(this);
    }
}
