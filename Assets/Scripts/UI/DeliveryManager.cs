using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Tracks delivery progress, picks a <b>random registered drop point</b> per leg, and toggles an optional
/// <see cref="DeliveryItem"/> when a new job is ready (first play + after scripted H follow-up).
/// Assignment copy is driven by chat / AI; this component only holds gameplay state.
/// </summary>
public class DeliveryManager : MonoBehaviour
{
    [Header("Random drop points")]
    [Tooltip("If true, prepares the first delivery one frame after play starts. Leave false when the first leg should start from the messenger (see ChatManager prepare-on-first-send) or from scripted AI.")]
    [SerializeField] private bool prepareFirstDeliveryAfterSceneTick;

    [Header("Reception / pickup item")]
    [Tooltip("Shown when <see cref=\"PrepareNextDeliveryFromAi\"/> runs (new job ready). When set, the player must use Interact on this item before any <see cref=\"DeliveryZone\"/> will accept a drop-off.")]
    [SerializeField] private DeliveryItem receptionDeliveryItem;

    /// <summary>
    /// True after <see cref="TryRegisterPackagePickup"/> succeeds for the active leg (same object as <see cref="receptionDeliveryItem"/>).
    /// </summary>
    bool _hasPickedUpCurrentPackage;

    /// <summary>
    /// Increments each time the player completes the current delivery (starts at 0).
    /// </summary>
    public int currentDeliveryID;

    /// <summary>
    /// The drop point id for the <b>current</b> delivery leg, or <c>-1</c> if none is active (pick up not issued yet).
    /// Each <see cref="DeliveryZone"/> registers a stable id (project convention: <c>0</c>–<c>6</c> → rooms 201–208 via <see cref="TryGetApartmentRoomForDropPoint"/>).
    /// </summary>
    public int ActiveDropPointId { get; private set; } = -1;

    /// <summary>
    /// Maps <see cref="DeliveryZone"/> <c>dropPointId</c> to in-fiction apartment room numbers for LLM / UI copy.
    /// </summary>
    static readonly Dictionary<int, int> ApartmentRoomByDropPointId = new()
    {
        [0] = 201,
        [1] = 202,
        [2] = 203,
        [3] = 205,
        [4] = 206,
        [5] = 207,
        [6] = 208,
    };

    /// <summary>
    /// Resolves the apartment room (e.g. 201) for a registered drop-off id, when that id is in the project table.
    /// </summary>
    public static bool TryGetApartmentRoomForDropPoint(int dropPointId, out int apartmentRoomNumber) =>
        ApartmentRoomByDropPointId.TryGetValue(dropPointId, out apartmentRoomNumber);

    /// <summary>Room for <see cref="ActiveDropPointId"/> when a leg is active and the id is in the table.</summary>
    public bool TryGetApartmentRoomForActiveDrop(out int apartmentRoomNumber) =>
        TryGetApartmentRoomForDropPoint(ActiveDropPointId, out apartmentRoomNumber);

    /// <summary>
    /// Raised synchronously right after a step completes. Argument is the completed step index (0-based).
    /// When this runs, <see cref="currentDeliveryID"/> has already advanced to the next active step (or <c>3</c> if none remain).
    /// </summary>
    public event Action<int> OnDeliveryCompleted;

    /// <summary>Whether a reception <see cref="DeliveryItem"/> is configured (pickup required before drop-off).</summary>
    public bool RequiresPhysicalPickup => receptionDeliveryItem != null;

    /// <summary>Player has interacted with the reception package for the current active leg.</summary>
    public bool HasPickedUpCurrentPackage => _hasPickedUpCurrentPackage;

    private readonly HashSet<int> _registeredDropPointIds = new HashSet<int>();

    private void Start()
    {
        if (prepareFirstDeliveryAfterSceneTick && Application.isPlaying)
            StartCoroutine(DeferredPrepareFirstDelivery());
    }

    private IEnumerator DeferredPrepareFirstDelivery()
    {
        yield return null;
        if (currentDeliveryID >= 3)
            yield break;
        if (ActiveDropPointId >= 0)
            yield break;

        PrepareNextDeliveryFromAi();
    }

    /// <summary>Called by <see cref="DeliveryZone"/> when it enables.</summary>
    public void RegisterDropPoint(int dropPointId)
    {
        if (dropPointId < 0)
            return;
        _registeredDropPointIds.Add(dropPointId);
    }

    /// <summary>Called by <see cref="DeliveryZone"/> when it disables.</summary>
    public void UnregisterDropPoint(int dropPointId)
    {
        _registeredDropPointIds.Remove(dropPointId);
    }

    /// <summary>
    /// Call when the narrative says a new job is ready (e.g. after H's scripted line). Picks a random registered
    /// <see cref="ActiveDropPointId"/> and activates the reception <see cref="DeliveryItem"/>.
    /// </summary>
    public void PrepareNextDeliveryFromAi()
    {
        if (currentDeliveryID >= 3)
            return;
        if (ActiveDropPointId >= 0)
            return;

        _hasPickedUpCurrentPackage = false;
        RollNextRandomDropPoint();
        receptionDeliveryItem?.ActivateForDelivery();
    }

    /// <summary>
    /// Call from <see cref="DeliveryItem.Interact"/> when the player looks at the reception package. Succeeds only for
    /// the configured <see cref="receptionDeliveryItem"/> while a delivery leg is active.
    /// </summary>
    public bool TryRegisterPackagePickup(DeliveryItem item)
    {
        if (item == null || receptionDeliveryItem == null || item != receptionDeliveryItem)
            return false;
        if (ActiveDropPointId < 0)
            return false;
        if (_hasPickedUpCurrentPackage)
            return false;

        _hasPickedUpCurrentPackage = true;
        receptionDeliveryItem.Deactivate();
        return true;
    }

    private void RollNextRandomDropPoint()
    {
        if (_registeredDropPointIds.Count == 0)
        {
            Debug.LogWarning(
                $"{nameof(DeliveryManager)} on {name}: No drop points registered. Add {nameof(DeliveryZone)} objects that reference this manager.",
                this);
            ActiveDropPointId = -1;
            return;
        }

        var list = new List<int>(_registeredDropPointIds);
        ActiveDropPointId = list[UnityEngine.Random.Range(0, list.Count)];
    }

    /// <summary>
    /// Completes the active delivery only if <paramref name="dropPointId"/> matches <see cref="ActiveDropPointId"/>.
    /// </summary>
    public bool TryCompleteDeliveryAtDropPoint(int dropPointId)
    {
        if (currentDeliveryID >= 3)
            return false;
        if (ActiveDropPointId < 0 || dropPointId != ActiveDropPointId)
            return false;
        if (receptionDeliveryItem != null && !_hasPickedUpCurrentPackage)
            return false;

        CompleteCurrentDeliveryStep();
        ActiveDropPointId = -1;
        receptionDeliveryItem?.Deactivate();
        return true;
    }

    private void CompleteCurrentDeliveryStep()
    {
        if (currentDeliveryID >= 3)
            return;

        int completedId = currentDeliveryID;
        currentDeliveryID++;
        OnDeliveryCompleted?.Invoke(completedId);

        if (currentDeliveryID >= 3)
        {
            ActiveDropPointId = -1;
            receptionDeliveryItem?.Deactivate();
        }
    }
}
