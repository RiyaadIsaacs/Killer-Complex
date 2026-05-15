using System.Collections;
using UnityEngine;

/// <summary>
/// Runs after the final delivery: when the bad-ending Ollama request is dispatched, closes the apartment door and
/// locks the desktop to shutdown-only; the marked <see cref="InteractDoor"/> opens the bad-end overlay on interact.
/// </summary>
public class BadEndingOrchestrator : MonoBehaviour
{
    public static BadEndingOrchestrator Instance { get; private set; }

    [Header("UI")]
    [Tooltip("Assign the black full-screen overlay with \"Bad Ending\" copy. Starts inactive.")]
    [SerializeField] private GameObject badEndingCanvasRoot;

    [Tooltip("If unset, the first ComputerDesktopUI in loaded scenes is used.")]
    [SerializeField] private ComputerDesktopUI computerDesktopUi;

    [Header("Bad ending canvas")]
    [Tooltip("Played whenever the bad-ending overlay is shown (e.g. apartment door interact). Non-spatial, at the listener.")]
    [SerializeField] private AudioClip badEndingCanvasGunshotClip;

    [SerializeField, Range(0f, 2f)]
    [Tooltip("Volume scale for the bad-ending canvas gunshot.")]
    private float badEndingCanvasGunshotVolumeScale = 1f;

    [Header("Door audio")]
    [Tooltip("After the first knock burst when the bad-ending door phase starts, replay the same burst on this interval until the player opens the apartment door.")]
    [SerializeField, Min(0.1f)]
    private float apartmentKnockRepeatIntervalSeconds = 8f;

    [Tooltip("If off, apartment knocks only play once at the start of the phase.")]
    [SerializeField] private bool repeatApartmentKnocksDuringBadEnding = true;

    bool _doorPhaseActive;
    bool _badEndDoorResolved;
    Coroutine _knockRepeatCoroutine;

    /// <summary>True after the trap Ollama message is sent until the apartment door interaction fires.</summary>
    public bool IsBadEndingDoorPhase => _doorPhaseActive && !_badEndDoorResolved;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Debug.LogWarning($"{nameof(BadEndingOrchestrator)}: Multiple instances — keeping first, destroying {name}.", this);
            Destroy(gameObject);
            return;
        }

        Instance = this;
        if (badEndingCanvasRoot != null)
            badEndingCanvasRoot.SetActive(false);
    }

    void OnDestroy()
    {
        StopApartmentKnockRepeatIfAny();
        if (Instance == this)
            Instance = null;
    }

    ComputerDesktopUI ResolveDesktop()
    {
        if (computerDesktopUi != null)
            return computerDesktopUi;
        computerDesktopUi = FindFirstObjectByType<ComputerDesktopUI>(FindObjectsInactive.Include);
        return computerDesktopUi;
    }

    /// <summary>Called the moment the bad-ending Ollama HTTP request is started (before the reply returns).</summary>
    public void StartBadEnding()
    {
        StopApartmentKnockRepeatIfAny();

        _doorPhaseActive = true;
        _badEndDoorResolved = false;

        InteractDoor.CloseMarkedApartmentDoorsForBadEnding();
        InteractDoor.BeginBadEndingApartmentKnocks();

        if (repeatApartmentKnocksDuringBadEnding)
            _knockRepeatCoroutine = StartCoroutine(ApartmentKnockRepeatRoutine());

        ResolveDesktop()?.EnterBadEndingComputerMode();
    }

    IEnumerator ApartmentKnockRepeatRoutine()
    {
        while (IsBadEndingDoorPhase)
        {
            yield return new WaitForSeconds(apartmentKnockRepeatIntervalSeconds);
            if (!IsBadEndingDoorPhase)
                yield break;
            InteractDoor.BeginBadEndingApartmentKnocks();
        }

        _knockRepeatCoroutine = null;
    }

    void StopApartmentKnockRepeatIfAny()
    {
        if (_knockRepeatCoroutine == null)
            return;
        StopCoroutine(_knockRepeatCoroutine);
        _knockRepeatCoroutine = null;
    }

    /// <summary>
    /// If this door is the apartment door and the bad-ending phase is active, opens the door and shows the overlay.
    /// </summary>
    public bool TryHandleMyApartmentDoorInteract(InteractDoor door)
    {
        if (!IsBadEndingDoorPhase || door == null || !door.IsMyApartmentDoor)
            return false;

        var dm = FindFirstObjectByType<DeliveryManager>(FindObjectsInactive.Include);
        if (dm != null && !dm.FinishedAllConfiguredDeliveryLegs)
            return false;

        door.ForceSetOpen(true);
        _badEndDoorResolved = true;
        _doorPhaseActive = false;

        StopApartmentKnockRepeatIfAny();

        RevealBadEndingCanvas();

        return true;
    }

    /// <summary>
    /// Called when a world trap (or similar) catches the player: optional full bad-ending setup, then the overlay.
    /// </summary>
    public void TriggerPlayerCaughtByTrap(bool runDoorAndDesktopSetup = true, bool revealOverlay = true)
    {
        if (runDoorAndDesktopSetup && !_doorPhaseActive && !_badEndDoorResolved)
            StartBadEnding();

        if (revealOverlay)
            RevealBadEndingCanvas();
    }

    /// <summary>
    /// Shows the bad-ending canvas (if assigned) and plays the configured gunshot stinger. Safe to call multiple times (e.g. re-showing the overlay).
    /// </summary>
    public void RevealBadEndingCanvas()
    {
        if (badEndingCanvasRoot != null)
            badEndingCanvasRoot.SetActive(true);

        if (badEndingCanvasGunshotClip != null)
            SoundManager.PlayOneShotNonSpatial(badEndingCanvasGunshotClip, badEndingCanvasGunshotVolumeScale);
    }
}
