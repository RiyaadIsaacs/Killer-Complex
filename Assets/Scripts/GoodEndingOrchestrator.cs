using UnityEngine;

/// <summary>
/// After 100% hack decryption: closes the player's apartment door, H posts a defeat line via Ollama,
/// then opening that door shows the good-ending overlay.
/// </summary>
public class GoodEndingOrchestrator : MonoBehaviour
{
    public static GoodEndingOrchestrator Instance { get; private set; }

    [Header("UI")]
    [Tooltip("Full-screen overlay shown when the player opens the apartment door during the good-ending phase. Starts inactive.")]
    [SerializeField] private GameObject goodEndingCanvasRoot;

    [Header("Optional audio")]
    [SerializeField] private AudioClip goodEndingStingerClip;

    [SerializeField, Range(0f, 2f)]
    private float goodEndingStingerVolumeScale = 1f;

    bool _doorPhaseActive;
    bool _goodEndDoorResolved;

    /// <summary>True from hack completion until the apartment door is opened for the good ending.</summary>
    public bool IsGoodEndingDoorPhase => _doorPhaseActive && !_goodEndDoorResolved;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Debug.LogWarning($"{nameof(GoodEndingOrchestrator)}: Multiple instances — keeping first, destroying {name}.", this);
            Destroy(gameObject);
            return;
        }

        Instance = this;
        TryAutoAssignGoodEndingCanvas();
        if (goodEndingCanvasRoot != null)
            goodEndingCanvasRoot.SetActive(false);
    }

    void TryAutoAssignGoodEndingCanvas()
    {
        if (goodEndingCanvasRoot != null)
            return;

        foreach (var tr in Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (tr == null || tr.name != "GoodEndingCanvas")
                continue;
            goodEndingCanvasRoot = tr.gameObject;
            return;
        }
    }

    void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    /// <summary>Called when decryption hits 100% (before H's defeat Ollama reply returns).</summary>
    public void StartGoodEnding()
    {
        if (_goodEndDoorResolved)
            return;

        if (BadEndingOrchestrator.Instance != null && BadEndingOrchestrator.Instance.IsBadEndingDoorPhase)
        {
            Debug.LogWarning($"{nameof(GoodEndingOrchestrator)}: Bad-ending door phase is active — good ending not started.", this);
            return;
        }

        _doorPhaseActive = true;
        _goodEndDoorResolved = false;

        InteractDoor.CloseMarkedApartmentDoorsForBadEnding();
    }

    /// <summary>
    /// If this is the apartment door during the good-ending phase, opens it and shows the victory overlay.
    /// </summary>
    public bool TryHandleMyApartmentDoorInteract(InteractDoor door)
    {
        if (!IsGoodEndingDoorPhase || door == null || !door.IsMyApartmentDoor)
            return false;

        door.ForceSetOpen(true);
        _goodEndDoorResolved = true;
        _doorPhaseActive = false;

        RevealGoodEndingCanvas();
        return true;
    }

    public void RevealGoodEndingCanvas()
    {
        if (goodEndingCanvasRoot != null)
            goodEndingCanvasRoot.SetActive(true);
        else
            Debug.LogWarning($"{nameof(GoodEndingOrchestrator)} on {name}: Good ending canvas root is not assigned.", this);

        if (goodEndingStingerClip != null)
            SoundManager.PlayOneShotNonSpatial(goodEndingStingerClip, goodEndingStingerVolumeScale);
    }
}
