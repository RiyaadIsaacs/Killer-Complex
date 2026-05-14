using UnityEngine;

/// <summary>
/// Plays UI / notification SFX. Assign an <see cref="AudioClip"/> from <c>Assets/SFX</c> (or anywhere) in the Inspector.
/// Uses an <see cref="AudioSource"/> on the same GameObject (adds one at runtime if missing).
/// </summary>
public class SoundManager : MonoBehaviour
{
    [SerializeField]
    [Tooltip("Played by PlayNotificationSound() — e.g. a clip under Assets/SFX.")]
    private AudioClip notificationSound;

    AudioSource audioSource;
    bool warnedMissingClip;

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
}
