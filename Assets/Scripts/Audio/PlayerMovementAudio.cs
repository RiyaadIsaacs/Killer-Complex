using UnityEngine;

/// <summary>
/// Loops footstep / walking SFX on the player while grounded and moving.
/// </summary>
[RequireComponent(typeof(CharacterController))]
public class PlayerMovementAudio : MonoBehaviour
{
    [SerializeField] private AudioClip walkingClip;
    [SerializeField, Range(0f, 1f)] private float walkingVolume = 0.45f;
    [SerializeField, Range(0.75f, 1.5f)] private float sprintPitch = 1.12f;
    [SerializeField] private float minHorizontalSpeed = 0.15f;

    CharacterController _controller;
    PlayerController _player;
    AudioSource _source;

    void Awake()
    {
        _controller = GetComponent<CharacterController>();
        _player = GetComponent<PlayerController>();
        EnsureAudioSource();
        ResolveWalkingClip();
    }

    void ResolveWalkingClip()
    {
        if (walkingClip != null)
            return;

        var sm = SoundManager.FindInstance();
        if (sm != null)
        {
            walkingClip = sm.WalkingLoopClip;
            if (walkingVolume <= 0f)
                walkingVolume = sm.WalkingLoopVolume;
        }
    }

    void EnsureAudioSource()
    {
        if (_source != null)
            return;

        _source = GetComponent<AudioSource>();
        if (_source == null)
            _source = gameObject.AddComponent<AudioSource>();

        _source.playOnAwake = false;
        _source.loop = true;
        _source.spatialBlend = 1f;
        _source.minDistance = 0.5f;
        _source.maxDistance = 18f;
        _source.rolloffMode = AudioRolloffMode.Logarithmic;
        _source.dopplerLevel = 0f;
        _source.priority = 128;
    }

    void LateUpdate()
    {
        if (walkingClip == null)
            ResolveWalkingClip();

        if (_controller == null || _source == null || walkingClip == null)
            return;

        if (_player != null && !_player.enabled)
        {
            StopWalking();
            return;
        }

        if (PauseScreen.IsGameplayPaused || GameSceneIntroPanel.BlocksGameplay)
        {
            StopWalking();
            return;
        }

        if (!_controller.isGrounded)
        {
            StopWalking();
            return;
        }

        var horizontal = new Vector3(_controller.velocity.x, 0f, _controller.velocity.z);
        if (horizontal.sqrMagnitude < minHorizontalSpeed * minHorizontalSpeed)
        {
            StopWalking();
            return;
        }

        _source.clip = walkingClip;
        _source.volume = walkingVolume;
        _source.pitch = _player != null && _player.IsSprinting ? sprintPitch : 1f;

        if (!_source.isPlaying)
            _source.Play();
    }

    void StopWalking()
    {
        if (_source != null && _source.isPlaying)
            _source.Stop();
    }
}
