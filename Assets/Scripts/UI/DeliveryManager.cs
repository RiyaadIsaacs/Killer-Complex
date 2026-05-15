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
    private int totalDeliveryLegs = 7;

    [Header("Random drop points")]
    [Tooltip("If true, prepares the first delivery one frame after play starts. Leave false when the first leg should start from the messenger (see ChatManager prepare-on-first-send) or from scripted AI.")]
    [SerializeField] private bool prepareFirstDeliveryAfterSceneTick;

    [Header("Reception / pickup item")]
    [Tooltip("Shown when <see cref=\"PrepareNextDeliveryFromAi\"/> runs (new job ready). When set, the player must use Interact on this item before any <see cref=\"DeliveryZone\"/> will accept a drop-off.")]
    [SerializeField] private DeliveryItem receptionDeliveryItem;

    [SerializeField] private GameObject[] spawnPoints;

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
        [3] = 204,
        [4] = 205,
        [5] = 206,
        [6] = 207,

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

    /// <summary>True after the last configured delivery leg is completed (<c>currentDeliveryID</c> is at the quota). Normal completion never advances past this without a code change.</summary>
    public bool FinishedAllConfiguredDeliveryLegs => currentDeliveryID >= TotalDeliveryLegs;

    /// <summary>Strictly greater than <see cref="TotalDeliveryLegs"/>; unused in the default completion path (quota caps at <see cref="FinishedAllConfiguredDeliveryLegs"/>).</summary>
    public bool CurrentDeliveryIdStrictlyGreaterThanMaxLegs => currentDeliveryID > TotalDeliveryLegs;

    private readonly HashSet<int> _registeredDropPointIds = new HashSet<int>();

    bool _postDeliveryStepAwayBeatPending;

    /// <summary>True before the next messenger turn consumes the one-shot "H steps away" LLM beat (see <see cref="AppendAndClearPostDeliveryStepAwayBeatInstruction"/>).</summary>
    public bool PostDeliveryStepAwayBeatPending => _postDeliveryStepAwayBeatPending;

    /// <summary>
    /// When set, the next <see cref="OllamaConnector"/> prompt should instruct H to give a dismissive away-from-keyboard excuse; then clears the flag.
    /// </summary>
    public void AppendAndClearPostDeliveryStepAwayBeatInstruction(System.Text.StringBuilder ctx)
    {
        if (!_postDeliveryStepAwayBeatPending)
            return;

        _postDeliveryStepAwayBeatPending = false;
        ctx.Append(" The player has just completed a delivery drop-off. ");
        ctx.Append("In this reply H must give a short dismissive excuse for stepping away from the computer ");
        ctx.Append("(e.g. checking on the package, taking a call, dealing with building security) — tone brusque, treating the player as an irritation. ");
        ctx.Append("You may use casual South African brush-off lines such as \"wait a bit\" or \"I'm coming now\" as impatient texture, not warmth. ");
        ctx.Append("This is the in-fiction lull when the player has time alone at their machine; ");
        ctx.Append("do not assign a new apartment delivery or new task numbers in this reply. ");
    }

    /// <summary>Clears the one-shot beat without sending it to the LLM (e.g. no <see cref="OllamaConnector"/> in scene).</summary>
    public void AbandonPostDeliveryStepAwayBeat()
    {
        _postDeliveryStepAwayBeatPending = false;
    }

    /// <summary>Clears the post-drop beat so the next messenger send does not ask for the dismissive-away line (bad-ending Ollama path).</summary>
    public void ConsumePostDeliveryBeatForBadEnding()
    {
        _postDeliveryStepAwayBeatPending = false;
    }

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
    /// After a completed drop-off, the next call is normally from <see cref="ChatManager"/> on messenger SEND or from
    /// <see cref="OllamaConnector.SendHackReversalPrompt"/> after a full hack when idle (unless <see cref="prepareFirstDeliveryAfterSceneTick"/> prepared the very first leg).
    /// </summary>
    public void PrepareNextDeliveryFromAi()
    {
        if (currentDeliveryID >= TotalDeliveryLegs)
            return;
        if (ActiveDropPointId >= 0)
            return;

        int randomIndex = UnityEngine.Random.Range(0, spawnPoints.Length);
        receptionDeliveryItem.transform.position = spawnPoints[randomIndex].transform.position;

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
        _postDeliveryStepAwayBeatPending = true;

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
