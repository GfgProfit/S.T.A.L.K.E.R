using UnityEngine;

public sealed class FirstPersonCameraAllAnimationController : MonoBehaviour
{
    [SerializeField] private PlayerController _playerController;
    [SerializeField] private Animator _animator;
    [SerializeField] private FirstPersonCameraAllAnimationClipSet _clips = new();
    [SerializeField] [Min(0f)] private float _crossFadeDuration = 0.1f;
    [SerializeField] [Min(0f)] private float _movementDeadZone = 0.1f;

    [Inject] private IPlayerInput _playerInput = null;

    private IPlayerInput _fallbackPlayerInput;
    private FirstPersonCameraAllAnimationPlayer _animationPlayer;
    private Vector3 _baseLocalPosition;
    private Quaternion _baseLocalRotation;
    private FirstPersonCameraAllAnimationKey _currentKey = FirstPersonCameraAllAnimationKey.None;
    private bool _wasGrounded = true;
    private bool _wasCrouching;
    private bool _isAiming;
    private bool _hasActiveAnimation;
    private bool _hasAppliedRotationOffset;
    private float _transientEndTime;
    private Quaternion _lastAppliedRotationOffset = Quaternion.identity;

    private IPlayerInput PlayerInput
    {
        get
        {
            if (_playerInput != null)
            {
                return _playerInput;
            }

            _fallbackPlayerInput ??= new LegacyPlayerInput();
            return _fallbackPlayerInput;
        }
    }

    private void Awake()
    {
        CacheBasePose();
        EnsureInitialized();
    }

    private void OnEnable()
    {
        CacheBasePose();
        EnsureInitialized();
        _wasGrounded = _playerController == null || _playerController.IsGrounded;
        _wasCrouching = _playerController != null && _playerController.IsCrouching;
        _transientEndTime = 0f;
        _currentKey = FirstPersonCameraAllAnimationKey.None;
    }

    private void Update()
    {
        EnsureInitialized();

        if (_animationPlayer == null)
        {
            return;
        }

        UpdateAnimationState();
    }

    private void LateUpdate()
    {
        if (_hasActiveAnimation == false)
        {
            transform.SetLocalPositionAndRotation(_baseLocalPosition, _baseLocalRotation);
            return;
        }
    }

    private void OnDisable()
    {
        _animationPlayer?.Stop();
        transform.SetLocalPositionAndRotation(_baseLocalPosition, _baseLocalRotation);
    }

    private void OnDestroy() => DisposeAnimationPlayer();

    public void SetAimActive(bool active) => _isAiming = active;

    private void EnsureInitialized()
    {
        if (_playerController == null)
        {
            _playerController = GetComponentInParent<PlayerController>();
        }

        if (_animator == null)
        {
            _animator = GetComponent<Animator>();
        }

        if (_animationPlayer == null && _animator != null)
        {
            _animationPlayer = new FirstPersonCameraAllAnimationPlayer(_animator, _crossFadeDuration);
        }
    }

    private void UpdateAnimationState()
    {
        bool isGrounded = _playerController == null || _playerController.IsGrounded;
        bool isCrouching = _playerController != null && _playerController.IsCrouching;
        float time = Time.time;

        if (_wasGrounded && isGrounded == false)
        {
            PlayTransient(FirstPersonCameraAllAnimationKey.Jump, time);
        }
        else if (_wasGrounded == false && isGrounded)
        {
            PlayTransient(FirstPersonCameraAllAnimationKey.Landing, time);
        }
        else if (_wasCrouching == false && isCrouching)
        {
            PlayTransient(FirstPersonCameraAllAnimationKey.CrouchDown, time);
        }
        else if (_wasCrouching && isCrouching == false)
        {
            PlayTransient(FirstPersonCameraAllAnimationKey.CrouchUp, time);
        }

        _wasGrounded = isGrounded;
        _wasCrouching = isCrouching;

        if (time < _transientEndTime)
        {
            return;
        }

        PlayContinuous(ResolveContinuousKey());
    }

    private FirstPersonCameraAllAnimationKey ResolveContinuousKey()
    {
        if (_playerController != null && _playerController.IsSprinting)
        {
            return FirstPersonCameraAllAnimationKey.Sprint;
        }

        Vector2 movementInput = _playerController != null && _playerController.IsWalking ? PlayerInput.GetMovementInput() : Vector2.zero;

        if (movementInput.sqrMagnitude <= _movementDeadZone * _movementDeadZone)
        {
            return FirstPersonCameraAllAnimationKey.None;
        }

        if (Mathf.Abs(movementInput.x) > Mathf.Abs(movementInput.y))
        {
            if (_isAiming)
            {
                return movementInput.x < 0f ? FirstPersonCameraAllAnimationKey.WalkAimLeft : FirstPersonCameraAllAnimationKey.WalkAimRight;
            }

            return movementInput.x < 0f ? FirstPersonCameraAllAnimationKey.WalkLeft : FirstPersonCameraAllAnimationKey.WalkRight;
        }

        return movementInput.y < 0f ? FirstPersonCameraAllAnimationKey.WalkBackward : FirstPersonCameraAllAnimationKey.WalkForward;
    }

    private void PlayTransient(FirstPersonCameraAllAnimationKey key, float time)
    {
        AnimationClip clip = _clips == null ? null : _clips.GetClip(key);

        if (clip == null)
        {
            return;
        }

        _animationPlayer.Play(key, clip);
        _currentKey = key;
        _hasActiveAnimation = true;
        _transientEndTime = time + Mathf.Max(0f, clip.length);
    }

    private void PlayContinuous(FirstPersonCameraAllAnimationKey key)
    {
        if (key == FirstPersonCameraAllAnimationKey.None)
        {
            _animationPlayer.Stop();
            _currentKey = FirstPersonCameraAllAnimationKey.None;
            _hasActiveAnimation = false;
            return;
        }

        AnimationClip clip = _clips == null ? null : _clips.GetClip(key);

        if (clip == null)
        {
            _animationPlayer.Stop();
            _currentKey = FirstPersonCameraAllAnimationKey.None;
            _hasActiveAnimation = false;
            return;
        }

        if (_currentKey != key)
        {
            _animationPlayer.Play(key, clip);
            _currentKey = key;
        }

        _hasActiveAnimation = true;
    }

    private void CacheBasePose()
    {
        _baseLocalPosition = transform.localPosition;
        _baseLocalRotation = transform.localRotation;
    }

    private void DisposeAnimationPlayer()
    {
        _animationPlayer?.Dispose();
        _animationPlayer = null;
    }
}
