using UnityEngine;

/// <summary>
/// Plays UI / notification SFX and optional world one-shots (e.g. door knocks). Assign clips under <c>Assets/SFX</c> (or anywhere).
/// Uses an <see cref="AudioSource"/> on the same GameObject (adds one at runtime if missing) for 2D UI sounds.
/// </summary>
public class SoundManager : MonoBehaviour
{
    [SerializeField]
    [Tooltip("Played by PlayNotificationSound() — e.g. a clip under Assets/SFX.")]
    private AudioClip notificationSound;

    [Header("Door (optional fallback)")]
    [Tooltip("Used when the apartment InteractDoor has no knock clip assigned — bad-ending knocks still play at the door position.")]
    [SerializeField] private AudioClip doorKnockClip;

    [SerializeField, Range(0f, 2f)]
    [Tooltip("Volume scale for PlayDoorKnockWorld and for InteractDoor fallback knocks.")]
    private float doorKnockVolumeScale = 1f;

    [Header("Player movement")]
    [Tooltip("Looped while the player walks. Used by PlayerMovementAudio when its clip is unset.")]
    [SerializeField] private AudioClip walkingLoopClip;

    [SerializeField, Range(0f, 1f)] private float walkingLoopVolume = 0.45f;

    AudioSource audioSource;
    bool warnedMissingClip;

    /// <summary>Optional knock used by <see cref="InteractDoor"/> when its own clip is unset.</summary>
    public AudioClip DoorKnockClip => doorKnockClip;

    /// <inheritdoc cref="doorKnockVolumeScale"/>
    public float DoorKnockVolumeScale => doorKnockVolumeScale;

    public AudioClip WalkingLoopClip => walkingLoopClip;

    public float WalkingLoopVolume => walkingLoopVolume;

    /// <summary>First <see cref="SoundManager"/> in loaded scenes (desktop canvas, etc.).</summary>
    public static SoundManager FindInstance() =>
        Object.FindFirstObjectByType<SoundManager>(FindObjectsInactive.Include);

    void Awake()
    {
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
            audioSource = gameObject.AddComponent<AudioSource>();

        audioSource.playOnAwake = false;
        audioSource.spatialBlend = 0f;
        audioSource.volume = 1f;
        audioSource.mute = false;
        audioSource.loop = false;
        audioSource.priority = 0;
    }

    public void PlayNotificationSound()
    {
        if (audioSource == null)
            return;

        if (notificationSound == null)
        {
            if (!warnedMissingClip)
            {
                warnedMissingClip = true;
                Debug.LogWarning(
                    $"{nameof(SoundManager)} on '{name}': assign {nameof(notificationSound)} in the Inspector (e.g. an AudioClip under Assets/SFX).",
                    this);
            }
            return;
        }

        audioSource.PlayOneShot(notificationSound, 1f);
    }

    /// <summary>Plays <see cref="doorKnockClip"/> at a world position with 3D attenuation (no clip = no-op).</summary>
    public void PlayDoorKnockWorld(Vector3 worldPosition)
    {
        PlayOneShotWorld(doorKnockClip, worldPosition, doorKnockVolumeScale);
    }

    /// <summary>Resolves the first <see cref="SoundManager"/> and plays its door knock at <paramref name="worldPosition"/>.</summary>
    public static void TryPlayDoorKnockAt(Vector3 worldPosition)
    {
        var sm = Object.FindFirstObjectByType<SoundManager>(FindObjectsInactive.Include);
        if (sm == null)
            return;

        sm.PlayDoorKnockWorld(worldPosition);
    }

    /// <summary>
    /// Plays a door knock at <paramref name="worldPosition"/> using <paramref name="clipOverride"/> or the first SoundManager fallback.
    /// </summary>
    public static void TryPlayDoorKnockAt(Vector3 worldPosition, AudioClip clipOverride, float volumeScale = 1f)
    {
        var clip = clipOverride;
        SoundManager sm = null;
        var vol = Mathf.Clamp(volumeScale, 0f, 2f);

        if (clip == null)
        {
            sm = Object.FindFirstObjectByType<SoundManager>(FindObjectsInactive.Include);
            if (sm != null)
            {
                clip = sm.DoorKnockClip;
                vol *= sm.DoorKnockVolumeScale;
            }
        }

        PlayOneShotWorld(clip, worldPosition, vol);
    }

    /// <summary>
    /// Spawns a short-lived <see cref="AudioSource"/> at <paramref name="worldPosition"/> for spatial knock / impact SFX.
    /// </summary>
    public static void PlayOneShotWorld(
        AudioClip clip,
        Vector3 worldPosition,
        float volumeScale = 1f,
        float minDistance = 0.75f,
        float maxDistance = 35f)
    {
        if (clip == null)
            return;

        volumeScale = Mathf.Clamp(volumeScale, 0f, 2f);
        var go = new GameObject("One-shot audio (world)");
        go.transform.position = worldPosition;
        var src = go.AddComponent<AudioSource>();
        src.playOnAwake = false;
        src.clip = clip;
        src.spatialBlend = 1f;
        src.volume = volumeScale;
        src.minDistance = minDistance;
        src.maxDistance = maxDistance;
        src.rolloffMode = AudioRolloffMode.Logarithmic;
        src.dopplerLevel = 0f;
        src.priority = 64;
        src.Play();
        Object.Destroy(go, clip.length + 0.1f);
    }

    /// <summary>
    /// Full-volume stinger at the active <see cref="AudioListener"/> (or main camera). Use for UI / cutscene hits such as a bad-ending gunshot.
    /// </summary>
    public static void PlayOneShotNonSpatial(AudioClip clip, float volumeScale = 1f)
    {
        if (clip == null)
            return;

        volumeScale = Mathf.Clamp(volumeScale, 0f, 2f);
        Vector3 pos = Vector3.zero;
        var listener = Object.FindFirstObjectByType<AudioListener>(FindObjectsInactive.Include);
        if (listener != null)
            pos = listener.transform.position;
        else if (Camera.main != null)
            pos = Camera.main.transform.position;

        var go = new GameObject("One-shot audio (non-spatial)");
        go.transform.position = pos;
        var src = go.AddComponent<AudioSource>();
        src.playOnAwake = false;
        src.clip = clip;
        src.spatialBlend = 0f;
        src.volume = volumeScale;
        src.dopplerLevel = 0f;
        src.priority = 32;
        src.Play();
        Object.Destroy(go, clip.length + 0.1f);
    }
}
