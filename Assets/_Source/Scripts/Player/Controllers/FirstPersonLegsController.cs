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
            return time < _jumpStartEndTime ? FirstPersonLegsAnimationKey.JumpStart : FirstPersonLegsAnimationKey.JumpLoop;
        }

        if (time < _landingEndTime)
        {
            return FirstPersonLegsAnimationKey.JumpEnd;
        }

        Vector2 movementInput = _playerController != null && _playerController.IsWalking ? PlayerInput.GetMovementInput() : Vector2.zero;
        bool isSprinting = _playerController != null && _playerController.IsSprinting;
        bool isCrouching = _playerController != null && _playerController.IsCrouching;
        float yawInput = Cursor.lockState == CursorLockMode.Locked ? PlayerInput.GetMouseDelta().x : 0f;

        return FirstPersonLegsAnimationStateResolver.ResolveGrounded(movementInput, isSprinting, isCrouching, yawInput, _movementDeadZone, _turnDeadZone);
    }

    private float GetHoldDuration(FirstPersonLegsAnimationKey key, float minimumDuration) => Mathf.Max(minimumDuration, _clips == null ? 0f : _clips.GetLength(key));

    private void DisposeAnimationPlayer()
    {
        _animationPlayer?.Dispose();
        _animationPlayer = null;
    }
}
