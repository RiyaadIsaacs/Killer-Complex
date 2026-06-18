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
    [Tooltip("If true, prepares the first delivery one frame after a gameplay scene load. Hooked from " +
             nameof(DeliveryUrgencyTimer) + " on load (not Start), since this component may persist on DontDestroyOnLoad.")]
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

    /// <summary>Human-readable pickup site for the active leg (LLM CONTEXT + objective HUD).</summary>
    public string CurrentPickupLocationLabel { get; private set; } = string.Empty;

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

    int _destinationAnnouncedForDropPointId = -1;

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

    /// <summary>Sorted apartment numbers for drop zones registered in the current scene (for player-facing HUD).</summary>
    public string GetRegisteredApartmentRoomsListForDisplay()
    {
        var rooms = new List<int>();
        foreach (var id in _registeredDropPointIds)
        {
            if (TryGetApartmentRoomForDropPoint(id, out int room))
                rooms.Add(room);
        }

        rooms.Sort();
        if (rooms.Count == 0)
            return MappedApartmentsListForPrompt;

        return string.Join(", ", rooms);
    }

    /// <summary>One-shot center HUD toast for the active leg's destination when the player leaves the computer (deduped per <see cref="ActiveDropPointId"/>).</summary>
    public void AnnounceDestinationForActiveLegIfNeeded()
    {
        if (ActiveDropPointId < 0 || CurrentLegDestinationApartment < 0)
            return;
        if (_destinationAnnouncedForDropPointId == ActiveDropPointId)
            return;

        _destinationAnnouncedForDropPointId = ActiveDropPointId;
        GlobalNotificationHud.ShowDeliveryDestinationAnnouncement(CurrentLegDestinationApartment);
    }

    public event Action<int> OnDeliveryCompleted;

    /// <summary>Fired when <see cref="PrepareNextDeliveryFromAi"/> rolls a new active leg (pickup + destination ready).</summary>
    public event Action OnDeliveryLegPrepared;

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
    bool _pendingDestinationAnnouncementForLlm;

    /// <summary>True until H's next messenger reply after a new leg is rolled — forces drop-off apartment into LLM CONTEXT.</summary>
    public bool PendingDestinationAnnouncementForLlm => _pendingDestinationAnnouncementForLlm;

    public void ClearPendingDestinationAnnouncementForLlm() => _pendingDestinationAnnouncementForLlm = false;

    /// <summary>Copies scene-bound pickup/spawn references from a freshly loaded HUD instance.</summary>
    public void CopySceneBindingsFrom(DeliveryManager sceneInstance)
    {
        if (sceneInstance == null || sceneInstance == this)
            return;

        if (sceneInstance.receptionDeliveryItem != null)
        {
            receptionDeliveryItem = sceneInstance.receptionDeliveryItem;
            receptionDeliveryItem.BindDeliveryManager(this);
        }

        if (sceneInstance.spawnPoints != null && sceneInstance.spawnPoints.Length > 0)
            spawnPoints = sceneInstance.spawnPoints;
    }

    void TryRebindSceneReferencesIfNeeded()
    {
        if (receptionDeliveryItem == null)
        {
            foreach (var item in FindObjectsByType<DeliveryItem>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (item != null && item.IsAssignedTo(this))
                {
                    receptionDeliveryItem = item;
                    break;
                }
            }
        }

        if (NeedsSpawnPointRebind())
        {
            var pickupPoints = FindObjectsByType<DeliveryPickupSpawnPoint>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            if (pickupPoints.Length > 0)
            {
                spawnPoints = new GameObject[pickupPoints.Length];
                for (var i = 0; i < pickupPoints.Length; i++)
                    spawnPoints[i] = pickupPoints[i].gameObject;
            }
        }
    }

    bool NeedsSpawnPointRebind()
    {
        if (spawnPoints == null || spawnPoints.Length == 0)
            return true;

        foreach (var point in spawnPoints)
        {
            if (point == null)
                return true;
        }

        return false;
    }

    void SafeDeactivateReceptionItem()
    {
        if (receptionDeliveryItem != null)
            receptionDeliveryItem.Deactivate();
    }

    void ClearSceneBindings()
    {
        receptionDeliveryItem = null;
        spawnPoints = System.Array.Empty<GameObject>();
    }

    /// <summary>Re-scans scene <see cref="DeliveryZone"/> objects after reload (DDOL manager survives; zones re-enable in the new scene).</summary>
    public void RefreshDropPointRegistrationsFromScene()
    {
        _registeredDropPointIds.Clear();
        foreach (var zone in FindObjectsByType<DeliveryZone>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (zone == null)
                continue;

            int id = zone.DropPointId;
            if (id >= 0)
                RegisterDropPoint(id);
        }
    }

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
        ctx.Append("(e.g. someone at the door, a building call, security on the radio, \"back in a minute\") — tone brusque, treating the player as an irritation. ");
        ctx.Append("You may use casual South African brush-off lines such as \"wait a bit\" or \"I'm coming now\" as impatient texture, not warmth. ");
        ctx.Append("This is the in-fiction lull when the player has time alone at their machine; ");
        ctx.Append("do NOT mention a new courier run, pickup, drop-off, apartment unit number, or any fresh task — only the excuse. ");
        ctx.Append("Do not recap or preview the next job; the next assignment will be CONTEXT on a later turn only when a leg is active.");
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

    /// <summary>
    /// Arms the bad-ending Ollama path: quota exceeded, active leg cleared, post-drop beat set (e.g. suspicion hit 100%).
    /// </summary>
    public void ForceSuspicionMaxBadEndingState()
    {
        currentDeliveryID = TotalDeliveryLegs + 1;
        ActiveDropPointId = -1;
        CurrentLegDestinationApartment = -1;
        CurrentPickupLocationLabel = string.Empty;
        hasPickedUpCurrentPackage = false;
        SafeDeactivateReceptionItem();
        _postDeliveryStepAwayBeatPending = true;
        _pendingDestinationAnnouncementForLlm = false;
        _destinationAnnouncedForDropPointId = -1;
    }

    void OnValidate()
    {
        totalDeliveryLegs = Mathf.Max(1, totalDeliveryLegs);
    }

    /// <summary>
    /// Clears job progress when a new session should start. Required because this component lives on the same
    /// DontDestroyOnLoad object as <see cref="GlobalNotificationHud"/> and survives <c>SceneManager.LoadScene</c>.
    /// </summary>
    /// <param name="queueDeferredFirstPrepare">
    /// When true and <see cref="prepareFirstDeliveryAfterSceneTick"/> is enabled, runs the same one-frame defer as
    /// <see cref="Start"/> so restarts behave like a cold play (Start is not called again on a persisted component).
    /// </param>
    public void ResetRunStateForNewPlaySession(bool queueDeferredFirstPrepare)
    {
        StopAllCoroutines();
        GlobalNotificationHud.ResetTransientNotificationsForSessionOnHud();

        currentDeliveryID = 0;
        ActiveDropPointId = -1;
        CurrentLegDestinationApartment = -1;
        CurrentPickupLocationLabel = string.Empty;
        hasPickedUpCurrentPackage = false;
        _postDeliveryStepAwayBeatPending = false;
        _pendingDestinationAnnouncementForLlm = false;
        _destinationAnnouncedForDropPointId = -1;
        SafeDeactivateReceptionItem();

        if (queueDeferredFirstPrepare)
        {
            TryRebindSceneReferencesIfNeeded();
            RefreshDropPointRegistrationsFromScene();
        }
        else
            ClearSceneBindings();

        if (queueDeferredFirstPrepare && prepareFirstDeliveryAfterSceneTick && Application.isPlaying)
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
        // Next job must not spawn until the step-away messenger beat is consumed (see ChatManager defer path).
        if (_postDeliveryStepAwayBeatPending)
            return;

        TryRebindSceneReferencesIfNeeded();
        RefreshDropPointRegistrationsFromScene();

        if (receptionDeliveryItem == null || spawnPoints == null || spawnPoints.Length == 0)
        {
            Debug.LogWarning(
                $"{nameof(DeliveryManager)} on {name}: Cannot prepare delivery — reception package or spawn points are missing. " +
                "Ensure the gameplay scene wires them on the HUD prefab instance.",
                this);
            return;
        }

        int randomIndex = UnityEngine.Random.Range(0, spawnPoints.Length);
        var spawnGo = spawnPoints[randomIndex];
        if (spawnGo == null)
        {
            Debug.LogWarning($"{nameof(DeliveryManager)} on {name}: Selected spawn point was destroyed.", this);
            return;
        }

        receptionDeliveryItem.transform.position = spawnGo.transform.position;
        CurrentPickupLocationLabel = ResolvePickupLabel(spawnGo);

        hasPickedUpCurrentPackage = false;
        RollNextRandomDropPoint();
        if (receptionDeliveryItem != null)
            receptionDeliveryItem.ActivateForDelivery();

        if (ActiveDropPointId >= 0)
        {
            if (CurrentLegDestinationApartment >= 0)
                _pendingDestinationAnnouncementForLlm = true;
            OnDeliveryLegPrepared?.Invoke();
        }
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
        CurrentPickupLocationLabel = string.Empty;
        _pendingDestinationAnnouncementForLlm = false;
        _destinationAnnouncedForDropPointId = -1;

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
        {
            var loc = CurrentPickupLocationLabel;
            return string.IsNullOrWhiteSpace(loc)
                ? "Pick up the package first."
                : $"Pick up the package in {loc} first.";
        }
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

    static string ResolvePickupLabel(GameObject spawnGo)
    {
        if (spawnGo == null)
            return string.Empty;

        var point = spawnGo.GetComponent<DeliveryPickupSpawnPoint>();
        if (point != null)
            return point.GetPickupLabelForLlm();

        return DeliveryPickupSpawnPoint.DeriveLabelFromObjectName(spawnGo.name);
    }
}
