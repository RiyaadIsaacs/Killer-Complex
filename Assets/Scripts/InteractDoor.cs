using System.Collections;
using UnityEngine;

// Hinge door interaction.
public class InteractDoor : MonoBehaviour
{
    [Header("Pivot")]
    [SerializeField] private Transform doorPivot;

    [Header("Local rotation (pivot space)")]
    [Tooltip("Euler angles when the door is fully closed.")]
    [SerializeField] private Vector3 closedEulerAngles = Vector3.zero;

    [Tooltip("Euler angles when the door is fully open.")]
    [SerializeField] private Vector3 openEulerAngles = new Vector3(0f, 90f, 0f);

    [Header("Motion")]
    [Tooltip("Seconds to reach the target rotation. Set to 0 for an instant snap.")]
    [SerializeField] private float rotationDuration = 0.35f;

    [Tooltip("If true, the door begins in the open pose.")]
    [SerializeField] private bool startOpen;

    [Header("Bad ending")]
    [Tooltip("Mark the player's apartment door (e.g. Room 204). Used with BadEndingOrchestrator after the final delivery.")]
    [SerializeField] private bool myApartmentDoor;

    [Tooltip("When the bad-ending trap starts, play a short knock sequence at this door (3D).")]
    [SerializeField] private bool playKnockOnBadEndingStart = true;

    [Tooltip("Knock SFX at the door. If unset, the first SoundManager's Door Knock clip is used (assign there or here).")]
    [SerializeField] private AudioClip apartmentDoorKnockClip;

    [SerializeField, Min(1)] private int apartmentKnockCount = 3;

    [SerializeField, Min(0f)] private float apartmentKnockSpacingSeconds = 0.14f;

    [SerializeField, Min(0f)] private float apartmentKnockStartDelaySeconds = 0.55f;

    [SerializeField, Range(0f, 2f)] private float apartmentKnockVolumeScale = 1f;

    [Header("Open / close SFX")]
    [Tooltip("Played when the player opens this door. If unset, SoundManager Door Open is used.")]
    [SerializeField] private AudioClip doorOpenClip;

    [Tooltip("Played when the player closes this door. If unset, SoundManager Door Close is used.")]
    [SerializeField] private AudioClip doorCloseClip;

    [SerializeField, Range(0f, 2f)] private float doorOpenCloseVolumeScale = 1f;

    [Header("Room numbers")]
    [Tooltip("Reparents modular letter room numbers under the door pivot and clears Static flags so they rotate with the door.")]
    [SerializeField] private bool autoFixRoomNumberLabels = true;

    [Tooltip("Optional parent object holding all digit prefabs. If unset, auto-finds children whose names contain \"block number\" or \"number \" under this door.")]
    [SerializeField] private Transform roomNumbersRoot;

    [Header("Compatibility")]
    [Tooltip("Many kit doors use an Animator on the same object as the mesh. That Animator overwrites localRotation every frame and blocks this script. Disable those animators on the pivot and its children so this rotation can apply.")]
    [SerializeField] private bool disableAnimatorsUnderPivot = true;

    private Quaternion closedRotation;
    private Quaternion openRotation;
    private Quaternion fromRotation;
    private Quaternion targetRotation;
    private float blend;
    private bool isOpen;

    public bool IsOpen => isOpen;

    public bool IsMyApartmentDoor => myApartmentDoor;

    /// <summary>Transform that rotates when the door opens (Inspector <c>doorPivot</c> or this object).</summary>
    public Transform DoorPivotTransform
    {
        get
        {
            if (doorPivot == null)
                doorPivot = transform;
            return doorPivot;
        }
    }

    /// <summary>
    /// Closes every <see cref="InteractDoor"/> with <see cref="IsMyApartmentDoor"/> (runs when the bad-ending Ollama request starts).
    /// Does not require <see cref="BadEndingOrchestrator"/>; re-disables kit animators under the pivot and snaps fully closed so another controller cannot leave the door visibly open.
    /// </summary>
    public static void CloseMarkedApartmentDoorsForBadEnding()
    {
        int count = 0;
        foreach (var door in Object.FindObjectsByType<InteractDoor>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (door == null || !door.IsMyApartmentDoor)
                continue;
            door.DisableAnimatorsUnderPivotIfConfigured();
            door.ForceSetClosedImmediate();
            count++;
        }

        if (count == 0)
        {
            Debug.LogWarning(
                $"{nameof(InteractDoor)}: Bad-ending door close found no doors with \"My Apartment Door\" enabled. " +
                $"Enable it on your unit's {nameof(InteractDoor)} (the object that receives Interact), not only on a parent.",
                null);
        }
    }

    /// <summary>
    /// Starts the apartment knock coroutine on each marked door (after bad-ending door close). Safe to call with no clips (no-op).
    /// </summary>
    public static void BeginBadEndingApartmentKnocks()
    {
        var host = ResolveKnockCoroutineHost();
        foreach (var door in Object.FindObjectsByType<InteractDoor>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (door == null || !door.IsMyApartmentDoor || !door.playKnockOnBadEndingStart)
                continue;

            if (host != null)
                host.StartCoroutine(door.BadEndingKnockSequenceRoutine());
            else if (door.isActiveAndEnabled)
                door.StartCoroutine(door.BadEndingKnockSequenceRoutine());
        }
    }

    static MonoBehaviour ResolveKnockCoroutineHost()
    {
        if (BadEndingOrchestrator.Instance != null)
            return BadEndingOrchestrator.Instance;
        return FindFirstObjectByType<OllamaConnector>(FindObjectsInactive.Include);
    }

    IEnumerator BadEndingKnockSequenceRoutine()
    {
        if (apartmentKnockStartDelaySeconds > 0f)
            yield return new WaitForSeconds(apartmentKnockStartDelaySeconds);

        var clip = apartmentDoorKnockClip;
        SoundManager smKnock = null;
        if (clip == null)
        {
            smKnock = FindFirstObjectByType<SoundManager>(FindObjectsInactive.Include);
            if (smKnock != null)
                clip = smKnock.DoorKnockClip;
        }

        if (clip == null)
        {
            if (!_warnedMissingKnockClip)
            {
                _warnedMissingKnockClip = true;
                Debug.LogWarning(
                    $"{nameof(InteractDoor)}: Bad-ending knock skipped — assign Apartment Door Knock Clip on this door or Door Knock Clip on a SoundManager.",
                    this);
            }
            yield break;
        }

        var pos = GetKnockWorldPosition();
        var vol = apartmentKnockVolumeScale;
        if (apartmentDoorKnockClip == null && smKnock != null)
            vol *= smKnock.DoorKnockVolumeScale;

        for (var i = 0; i < apartmentKnockCount; i++)
        {
            SoundManager.PlayOneShotWorld(clip, pos, vol);
            if (i < apartmentKnockCount - 1)
                yield return new WaitForSeconds(apartmentKnockSpacingSeconds);
        }
    }

    static bool _warnedMissingKnockClip;

    Vector3 GetKnockWorldPosition()
    {
        if (doorPivot == null)
            doorPivot = transform;
        return doorPivot.position;
    }

    void PlayDoorOpenCloseSound(bool opening)
    {
        var clip = opening ? doorOpenClip : doorCloseClip;
        SoundManager.TryPlayDoorOpenCloseAt(GetKnockWorldPosition(), opening, clip, doorOpenCloseVolumeScale);
    }

    void DisableAnimatorsUnderPivotIfConfigured()
    {
        if (doorPivot == null)
            doorPivot = transform;
        if (!disableAnimatorsUnderPivot)
            return;
        foreach (var animator in doorPivot.GetComponentsInChildren<Animator>(true))
        {
            if (animator != null)
                animator.enabled = false;
        }
    }

    /// <summary>Snaps to closed in one frame (bad ending).</summary>
    void ForceSetClosedImmediate()
    {
        if (doorPivot == null)
            doorPivot = transform;

        closedRotation = Quaternion.Euler(closedEulerAngles);
        openRotation = Quaternion.Euler(openEulerAngles);

        isOpen = false;
        doorPivot.localRotation = closedRotation;
        fromRotation = closedRotation;
        targetRotation = closedRotation;
        blend = 1f;
    }

    /// <summary>True when deliveries are finished for the run (see <see cref="DeliveryManager.FinishedAllConfiguredDeliveryLegs"/>). The bad-ending door also requires <see cref="BadEndingOrchestrator.IsBadEndingDoorPhase"/>.</summary>
    public bool PlayerReachedBadEndingDeliveryThreshold
    {
        get
        {
            var dm = FindFirstObjectByType<DeliveryManager>(FindObjectsInactive.Include);
            return dm != null && dm.FinishedAllConfiguredDeliveryLegs;
        }
    }

    private void Awake()
    {
        if (doorPivot == null)
            doorPivot = transform;

        DisableAnimatorsUnderPivotIfConfigured();

        closedRotation = Quaternion.Euler(closedEulerAngles);
        openRotation = Quaternion.Euler(openEulerAngles);

        if (Quaternion.Angle(closedRotation, openRotation) < 0.5f)
            Debug.LogWarning($"{nameof(InteractDoor)} on {name}: closed and open local rotations are almost the same — check Closed/Open Euler Angles on the pivot.", this);

        isOpen = startOpen;
        Quaternion initial = isOpen ? openRotation : closedRotation;
        doorPivot.localRotation = initial;
        fromRotation = initial;
        targetRotation = initial;
        blend = 1f;

        if (autoFixRoomNumberLabels)
            EnsureRoomNumbersFollowPivot();
    }

    /// <summary>
    /// Modular letter prefabs are static by default; static children do not move when the pivot rotates.
    /// </summary>
    public void EnsureRoomNumbersFollowPivot()
    {
        if (doorPivot == null)
            doorPivot = transform;

        if (roomNumbersRoot != null)
        {
            AttachRoomNumberRoot(roomNumbersRoot);
            return;
        }

        var labels = GetComponentsInChildren<DoorRoomNumberLabel>(true);
        if (labels.Length > 0)
        {
            foreach (var label in labels)
            {
                if (label != null)
                    label.Apply();
            }
            return;
        }

        foreach (var t in GetComponentsInChildren<Transform>(true))
        {
            if (t == null || t == doorPivot || t.IsChildOf(doorPivot))
                continue;
            if (!DoorRoomNumberLabel.NameLooksLikeRoomNumber(t.name))
                continue;
            AttachRoomNumberRoot(t);
        }

        foreach (var t in doorPivot.GetComponentsInChildren<Transform>(true))
        {
            if (t == null || t == doorPivot)
                continue;
            if (!DoorRoomNumberLabel.NameLooksLikeRoomNumber(t.name))
                continue;
            DoorRoomNumberLabel.ClearStaticFlags(t.gameObject);
        }
    }

    void AttachRoomNumberRoot(Transform root)
    {
        if (root == null)
            return;

        if (root.GetComponent<DoorRoomNumberLabel>() == null)
            root.gameObject.AddComponent<DoorRoomNumberLabel>();

        if (root.parent != doorPivot)
            root.SetParent(doorPivot, true);

        DoorRoomNumberLabel.ClearStaticFlags(root.gameObject);
    }

    private void LateUpdate()
    {
        if (blend >= 1f)
            return;

        if (rotationDuration <= 0f)
        {
            doorPivot.localRotation = targetRotation;
            blend = 1f;
            return;
        }

        blend += Time.deltaTime / rotationDuration;
        float t = Mathf.Clamp01(blend);
        doorPivot.localRotation = Quaternion.Slerp(fromRotation, targetRotation, t);
    }

    // Called by PlayerController through SendMessage when the player presses 'interact'.
    public void Interact()
    {
        if (GoodEndingOrchestrator.Instance != null &&
            GoodEndingOrchestrator.Instance.TryHandleMyApartmentDoorInteract(this))
            return;

        if (BadEndingOrchestrator.Instance != null &&
            BadEndingOrchestrator.Instance.TryHandleMyApartmentDoorInteract(this))
            return;

        MoveDoor();
    }

    /// <summary>Forces the door into the fully closed pose (used when the bad-ending sequence starts).</summary>
    public void ForceSetClosed() => ForceSetOpen(false);

    /// <summary>Forces open or closed pose without toggling.</summary>
    public void ForceSetOpen(bool open)
    {
        if (doorPivot == null)
            doorPivot = transform;
        DisableAnimatorsUnderPivotIfConfigured();

        isOpen = open;
        fromRotation = doorPivot.localRotation;
        targetRotation = isOpen ? openRotation : closedRotation;
        blend = 0f;

        if (rotationDuration <= 0f)
            doorPivot.localRotation = targetRotation;
    }

    private void MoveDoor()
    {
        isOpen = !isOpen;
        fromRotation = doorPivot.localRotation;
        targetRotation = isOpen ? openRotation : closedRotation;
        blend = 0f;

        if (rotationDuration <= 0f)
            doorPivot.localRotation = targetRotation;

        PlayDoorOpenCloseSound(isOpen);
    }
}
