using UnityEngine;

public sealed class FirstPersonLegsController : MonoBehaviour
{
    [SerializeField] private PlayerController _playerController;
    [SerializeField] private Animator _animator;
    [SerializeField] private Transform _meshesRoot;
    [SerializeField] private string _defaultLegsMeshName = "sviter";
    [SerializeField] private FirstPersonLegsAnimationClipSet _clips = new();
    [SerializeField] [Min(0f)] private float _crossFadeDuration = 0.1f;
    [SerializeField] [Min(0f)] private float _movementDeadZone = 0.1f;
    [SerializeField] [Min(0f)] private float _turnDeadZone = 0.2f;
    [SerializeField] [Min(0f)] private float _movementSmoothTime = 0.06f;
    [SerializeField] [Min(0f)] private float _turnSmoothTime = 0.04f;
    [SerializeField] [Min(0f)] private float _stateMinDuration = 0.12f;
    [SerializeField] [Min(0f)] private float _jumpStartMinDuration = 0.1f;
    [SerializeField] [Min(0f)] private float _landingMinDuration = 0.12f;

    [Inject] private IPlayerInput _playerInput = null;

    private IPlayerInput _fallbackPlayerInput;
    private FirstPersonLegsMeshSwitcher _meshSwitcher;
    private FirstPersonLegsAnimationPlayer _animationPlayer;
    private ItemData _equippedArmor;
    private bool _wasGrounded = true;
    private float _jumpStartEndTime;
    private float _landingEndTime;
    private Vector2 _smoothedMovementInput;
    private Vector2 _movementInputVelocity;
    private float _smoothedYawInput;
    private float _yawInputVelocity;
    private FirstPersonLegsAnimationKey _currentAnimationKey;
    private bool _hasCurrentAnimationKey;
    private float _nextAnimationSwitchTime;

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
        EnsureInitialized();
    }

    private void OnEnable()
    {
        EnsureInitialized();
        _wasGrounded = _playerController == null || _playerController.IsGrounded;
        ResetAnimationState();
        ApplyEquippedArmorMesh();
    }

    private void Update()
    {
        EnsureInitialized();

        if (_animationPlayer == null)
        {
            return;
        }

        _animationPlayer.Play(ResolveAnimationKey());
        _animationPlayer.Tick(Time.deltaTime);
    }

    private void OnDisable() => DisposeAnimationPlayer();
    private void OnDestroy() => DisposeAnimationPlayer();

    public void SetEquippedArmor(ItemData armorItemData)
    {
        _equippedArmor = armorItemData != null && armorItemData.ItemType == ItemType.Armor ? armorItemData : null;
        EnsureInitialized();
        ApplyEquippedArmorMesh();
    }

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

        if (_meshesRoot == null)
        {
            _meshesRoot = transform.Find("Meshes");
        }

        if (_meshSwitcher == null && _meshesRoot != null)
        {
            _meshSwitcher = new FirstPersonLegsMeshSwitcher(_meshesRoot, _defaultLegsMeshName);
        }

        if (_animationPlayer == null && _animator != null)
        {
            _animationPlayer = new FirstPersonLegsAnimationPlayer(_animator, _clips, _crossFadeDuration);
        }
    }

    private void ApplyEquippedArmorMesh() => _meshSwitcher?.SetMesh(_equippedArmor == null ? string.Empty : _equippedArmor.FirstPersonLegsMeshName);

    private FirstPersonLegsAnimationKey ResolveAnimationKey()
    {
        bool isGrounded = _playerController == null || _playerController.IsGrounded;
        float time = Time.time;

        if (_wasGrounded && isGrounded == false)
        {
            _jumpStartEndTime = time + GetHoldDuration(FirstPersonLegsAnimationKey.JumpStart, _jumpStartMinDuration);
        }
        else if (_wasGrounded == false && isGrounded)
        {
            _landingEndTime = time + GetHoldDuration(FirstPersonLegsAnimationKey.JumpEnd, _landingMinDuration);
        }

        _wasGrounded = isGrounded;

        if (isGrounded == false)
        {
            FirstPersonLegsAnimationKey airborneKey = time < _jumpStartEndTime ? FirstPersonLegsAnimationKey.JumpStart : FirstPersonLegsAnimationKey.JumpLoop;
            return SetCurrentAnimationKey(airborneKey, time);
        }

        if (time < _landingEndTime)
        {
            return SetCurrentAnimationKey(FirstPersonLegsAnimationKey.JumpEnd, time);
        }

        Vector2 movementInput = _playerController != null && _playerController.IsWalking ? GetSmoothedMovementInput() : Vector2.zero;
        bool isSprinting = _playerController != null && _playerController.IsSprinting;
        bool isCrouching = _playerController != null && _playerController.IsCrouching;
        float yawInput = GetSmoothedYawInput();

        FirstPersonLegsAnimationKey candidateKey = FirstPersonLegsAnimationStateResolver.ResolveGrounded(movementInput, isSprinting, isCrouching, yawInput, _movementDeadZone, _turnDeadZone);
        return StabilizeAnimationKey(candidateKey, time);
    }

    private float GetHoldDuration(FirstPersonLegsAnimationKey key, float minimumDuration) => Mathf.Max(minimumDuration, _clips == null ? 0f : _clips.GetLength(key));

    private Vector2 GetSmoothedMovementInput()
    {
        Vector2 targetInput = PlayerInput.GetMovementInput();

        if (_movementSmoothTime <= 0f)
        {
            _smoothedMovementInput = targetInput;
            return _smoothedMovementInput;
        }

        _smoothedMovementInput = Vector2.SmoothDamp(_smoothedMovementInput, targetInput, ref _movementInputVelocity, _movementSmoothTime);
        return _smoothedMovementInput;
    }

    private float GetSmoothedYawInput()
    {
        float targetYawInput = Cursor.lockState == CursorLockMode.Locked ? PlayerInput.GetMouseDelta().x : 0f;

        if (_turnSmoothTime <= 0f)
        {
            _smoothedYawInput = targetYawInput;
            return _smoothedYawInput;
        }

        _smoothedYawInput = Mathf.SmoothDamp(_smoothedYawInput, targetYawInput, ref _yawInputVelocity, _turnSmoothTime);
        return _smoothedYawInput;
    }

    private FirstPersonLegsAnimationKey StabilizeAnimationKey(FirstPersonLegsAnimationKey candidateKey, float time)
    {
        if (_hasCurrentAnimationKey == false || candidateKey == _currentAnimationKey)
        {
            return SetCurrentAnimationKey(candidateKey, time);
        }

        if (time < _nextAnimationSwitchTime)
        {
            return _currentAnimationKey;
        }

        return SetCurrentAnimationKey(candidateKey, time);
    }

    private FirstPersonLegsAnimationKey SetCurrentAnimationKey(FirstPersonLegsAnimationKey key, float time)
    {
        _currentAnimationKey = key;
        _hasCurrentAnimationKey = true;
        _nextAnimationSwitchTime = time + Mathf.Max(_stateMinDuration, _crossFadeDuration);
        return _currentAnimationKey;
    }

    private void ResetAnimationState()
    {
        _smoothedMovementInput = Vector2.zero;
        _movementInputVelocity = Vector2.zero;
        _smoothedYawInput = 0f;
        _yawInputVelocity = 0f;
        _hasCurrentAnimationKey = false;
        _nextAnimationSwitchTime = 0f;
    }

    private void DisposeAnimationPlayer()
    {
        _animationPlayer?.Dispose();
        _animationPlayer = null;
    }
}
