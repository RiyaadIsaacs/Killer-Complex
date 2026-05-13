using UnityEngine;

/// <summary>
/// World pickup (e.g. reception package). Starts hidden; <see cref="DeliveryManager"/> calls
/// <see cref="ActivateForDelivery"/> when a new job is issued (scripted AI beat or first delivery on play).
/// Put a collider in front of the player ray and assign <see cref="deliveryManager"/>; the player must
/// <b>Interact</b> (same as computer/doors) so <see cref="DeliveryManager.TryRegisterPackagePickup"/> runs before any drop-off zone accepts the package.
/// </summary>
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

    /// <summary>
    /// Invoked by <see cref="PlayerController"/> raycast + SendMessageUpwards when the player uses interact on this collider hierarchy.
    /// </summary>
    public void Interact()
    {
        if (deliveryManager == null)
            return;
        deliveryManager.TryRegisterPackagePickup(this);
    }
}
