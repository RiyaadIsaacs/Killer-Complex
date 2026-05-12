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

    private void Awake()
    {
        if (doorPivot == null)
            doorPivot = transform;

        if (disableAnimatorsUnderPivot)
        {
            foreach (var animator in doorPivot.GetComponentsInChildren<Animator>(true))
            {
                if (animator != null)
                    animator.enabled = false;
            }
        }

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
        isOpen = !isOpen;
        fromRotation = doorPivot.localRotation;
        targetRotation = isOpen ? openRotation : closedRotation;
        blend = 0f;

        if (rotationDuration <= 0f)
            doorPivot.localRotation = targetRotation;
    }
}
