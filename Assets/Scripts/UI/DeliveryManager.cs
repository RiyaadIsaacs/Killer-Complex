using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Manages delivery jobs and state for multiple delivery legs. Each leg starts when the narrative says a new job is ready.
public class DeliveryManager : MonoBehaviour
{
    [Header("Job count")]
    [SerializeField, Min(1)]
    [Tooltip("How many delivery legs (pick up → drop off) in a full run.")]
    private int totalDeliveryLegs = 3;

    [Header("Random drop points")]
    [Tooltip("If true, prepares the first delivery one frame after play starts. Leave false when the first leg should start from the messenger (see ChatManager prepare-on-first-send) or from scripted AI.")]
    [SerializeField] private bool prepareFirstDeliveryAfterSceneTick;

    [Header("Reception / pickup item")]
    [Tooltip("Shown when <see cref=\"PrepareNextDeliveryFromAi\"/> runs (new job ready). When set, the player must use Interact on this item before any <see cref=\"DeliveryZone\"/> will accept a drop-off.")]
    [SerializeField] private DeliveryItem receptionDeliveryItem;

    bool hasPickedUpCurrentPackage;

    public int currentDeliveryID;

    public int ActiveDropPointId { get; private set; } = -1;

    /// <summary>
    /// Three-digit apartment for the active leg, set when the drop point is rolled. Matches gameplay; use this for LLM context.
    /// </summary>
    public int CurrentLegDestinationApartment { get; private set; } = -1;

    static readonly Dictionary<int, int> ApartmentRoomByDropPointId = new()
    {
        [0] = 201,
        [1] = 202,
        [2] = 203,
        [3] = 205,
        [4] = 206,
        [5] = 207,
        [7] = 208,
        [8] = 301,
        [9] = 302,
        [10] = 303,
        [11] = 304,
        [12] = 305,
        [13] = 306,
        [14] = 307,
        [15] = 308,
    };

    public static bool TryGetApartmentRoomForDropPoint(int dropPointId, out int apartmentRoomNumber) =>
        ApartmentRoomByDropPointId.TryGetValue(dropPointId, out apartmentRoomNumber);

    public bool TryGetApartmentRoomForActiveDrop(out int apartmentRoomNumber) =>
        TryGetApartmentRoomForDropPoint(ActiveDropPointId, out apartmentRoomNumber);

    /// <summary>Comma-separated sorted list of every apartment number in <see cref="ApartmentRoomByDropPointId"/> (for LLM constraints).</summary>
    public static string MappedApartmentsListForPrompt { get; } = BuildMappedApartmentsListForPrompt();

    static string BuildMappedApartmentsListForPrompt()
    {
        var set = new HashSet<int>();
        foreach (var n in ApartmentRoomByDropPointId.Values)
            set.Add(n);
        var arr = new int[set.Count];
        set.CopyTo(arr);
        Array.Sort(arr);
        return string.Join(", ", arr);
    }

    public event Action<int> OnDeliveryCompleted;

    public bool RequiresPhysicalPickup => receptionDeliveryItem != null;

    public bool HasPickedUpCurrentPackage => hasPickedUpCurrentPackage;

    /// <summary>Configured number of delivery legs (minimum 1).</summary>
    public int TotalDeliveryLegs => Mathf.Max(1, totalDeliveryLegs);

    private readonly HashSet<int> _registeredDropPointIds = new HashSet<int>();

    void OnValidate()
    {
        totalDeliveryLegs = Mathf.Max(1, totalDeliveryLegs);
    }

    private void Start()
    {
        if (prepareFirstDeliveryAfterSceneTick && Application.isPlaying)
            StartCoroutine(DeferredPrepareFirstDelivery());
    }

    private IEnumerator DeferredPrepareFirstDelivery()
    {
        yield return null;
        if (currentDeliveryID >= TotalDeliveryLegs)
            yield break;
        if (ActiveDropPointId >= 0)
            yield break;

        PrepareNextDeliveryFromAi();
    }

    public void RegisterDropPoint(int dropPointId)
    {
        if (dropPointId < 0)
            return;
        _registeredDropPointIds.Add(dropPointId);
    }

    public void UnregisterDropPoint(int dropPointId)
    {
        _registeredDropPointIds.Remove(dropPointId);
    }

    /// <summary>
    /// Rolls the next drop-off and shows the reception package when <see cref="ActiveDropPointId"/> is idle.
    /// After a completed drop-off, the next call is expected from <see cref="ChatManager"/> when the player sends a messenger line (unless <see cref="prepareFirstDeliveryAfterSceneTick"/> prepared the very first leg).
    /// </summary>
    public void PrepareNextDeliveryFromAi()
    {
        if (currentDeliveryID >= TotalDeliveryLegs)
            return;
        if (ActiveDropPointId >= 0)
            return;

        hasPickedUpCurrentPackage = false;
        RollNextRandomDropPoint();
        receptionDeliveryItem?.ActivateForDelivery();
    }

    public bool TryRegisterPackagePickup(DeliveryItem item)
    {
        if (item == null || receptionDeliveryItem == null || item != receptionDeliveryItem)
            return false;
        if (ActiveDropPointId < 0)
            return false;
        if (hasPickedUpCurrentPackage)
            return false;

        hasPickedUpCurrentPackage = true;
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
            CurrentLegDestinationApartment = -1;
            return;
        }

        var list = new List<int>(_registeredDropPointIds);
        ActiveDropPointId = list[UnityEngine.Random.Range(0, list.Count)];
        CurrentLegDestinationApartment = TryGetApartmentRoomForDropPoint(ActiveDropPointId, out int room) ? room : -1;
    }

    public bool TryCompleteDeliveryAtDropPoint(int dropPointId)
    {
        if (!CanInteractDropPoint(dropPointId))
            return false;

        // Clear the finished leg before OnDeliveryCompleted so listeners see no active drop.
        ActiveDropPointId = -1;
        CurrentLegDestinationApartment = -1;

        CompleteCurrentDeliveryStep();
        return true;
    }

    /// <summary>
    /// True when interact at this drop zone would succeed (same checks as <see cref="TryCompleteDeliveryAtDropPoint"/>).
    /// </summary>
    public bool CanInteractDropPoint(int dropPointId)
    {
        if (currentDeliveryID >= TotalDeliveryLegs)
            return false;
        if (ActiveDropPointId < 0 || dropPointId != ActiveDropPointId)
            return false;
        if (receptionDeliveryItem != null && !hasPickedUpCurrentPackage)
            return false;
        return true;
    }

    /// <summary>
    /// When non-null, explains why a drop-off cannot complete at <paramref name="dropPointId"/> (for player feedback).
    /// </summary>
    public string GetDeliveryDropFailureReason(int dropPointId)
    {
        if (currentDeliveryID >= TotalDeliveryLegs)
            return "No more deliveries.";
        if (ActiveDropPointId < 0)
            return "No active delivery.";
        if (dropPointId != ActiveDropPointId)
            return "Wrong drop-off for this job.";
        if (receptionDeliveryItem != null && !hasPickedUpCurrentPackage)
            return "Pick up the package at reception first.";
        return null;
    }

    private void CompleteCurrentDeliveryStep()
    {
        if (currentDeliveryID >= TotalDeliveryLegs)
            return;

        int completedId = currentDeliveryID;
        currentDeliveryID++;
        OnDeliveryCompleted?.Invoke(completedId);

        if (currentDeliveryID >= TotalDeliveryLegs)
        {
            ActiveDropPointId = -1;
            CurrentLegDestinationApartment = -1;
            receptionDeliveryItem?.Deactivate();
            return;
        }

        // Next leg starts when the player sends on the messenger (see ChatManager), not automatically here.
    }
}
