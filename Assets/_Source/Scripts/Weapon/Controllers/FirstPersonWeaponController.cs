using UnityEngine;

public sealed class FirstPersonWeaponController : MonoBehaviour
{
    private const float STARTUP_CROSS_FADE_DURATION = 0f;
    private const float ROOT_POSITION_OFFSET_EPSILON = 0.000001f;

    [SerializeField] private Animator _handsAnimator;
    [SerializeField] private Animator _weaponAnimator;
    [SerializeField] private Transform _handsMeshRoot;
    [SerializeField] private string _defaultHandsMeshName = "default";
    [SerializeField] private Transform _weaponFollowSource;
    [SerializeField] private Transform _weaponFollowTarget;
    [SerializeField] private bool _followHands = true;
    [SerializeField] private bool _forceAim;
    [SerializeField] private Vector3 _rootPositionOffset;
    [SerializeField] private Vector3 _aimRootPositionOffset;
    [SerializeField] [Min(0f)] private float _rootPositionOffsetLerpSpeed = 12f;
    [SerializeField] private Vector3 _weaponFollowPositionOffset;
    [SerializeField] private Vector3 _weaponFollowRotationOffset;
    [SerializeField] private FirstPersonWeaponAnimationClipSet _clips = new();
    [SerializeField] [Min(0f)] private float _crossFadeDuration = 0.05f;
    [SerializeField] [Min(0f)] private float _walkCrossFadeDuration = 0.05f;
    [SerializeField] private WeaponCondition _weaponCondition = WeaponCondition.Normal;
    [SerializeField] private FirstPersonWeaponAnimationKey _startupAnimation = FirstPersonWeaponAnimationKey.Draw;
    [SerializeField] private bool _playStartupAnimation = true;

    private FirstPersonWeaponAnimationPlayer _animationPlayer;
    private FirstPersonWeaponFollower _weaponFollower;
    private FirstPersonHandsMeshSwitcher _handsMeshSwitcher;
    private ItemData _equippedArmor;
    private FirstPersonWeaponAnimationKey _currentAnimationKey = FirstPersonWeaponAnimationKey.Idle;
    private FirstPersonWeaponAnimationKey _returnAnimationKey = FirstPersonWeaponAnimationKey.Idle;
    private float _returnTime;
    private float _lastAnimationStartDelay;
    private bool _hasPendingReturn;
    private bool _useAimRootPositionOffset;

    public WeaponCondition Condition => _weaponCondition;
    public FirstPersonWeaponAnimationKey CurrentAnimationKey => _currentAnimationKey;
    public bool ForceAim => _forceAim;

    private void Awake()
    {
        SnapRootPositionOffset();
        EnsureInitialized();
    }

    private void OnEnable()
    {
        EnsureInitialized();

        if (IsRuntimeControlled)
        {
            return;
        }

        if (_playStartupAnimation)
        {
            PlayStartup();
            return;
        }

        PlayIdle();
    }

    private void Update()
    {
        EnsureInitialized();
        _animationPlayer?.Tick(Time.deltaTime);

        if (_hasPendingReturn && Time.time >= _returnTime)
        {
            PlayContinuous(_returnAnimationKey);
        }
    }

    private void LateUpdate()
    {
        UpdateRootPositionOffset(Time.deltaTime);
        _weaponFollower?.SetEnabled(_followHands);
        _weaponFollower?.SetAdditionalOffset(_weaponFollowPositionOffset, _weaponFollowRotationOffset);
        _weaponFollower?.Tick();
    }

    private void OnDisable() => DisposeAnimationPlayer();
    private void OnDestroy() => DisposeAnimationPlayer();

    public void SetCondition(WeaponCondition condition)
    {
        if (_weaponCondition == condition)
        {
            return;
        }

        _weaponCondition = condition;

        if (IsConditionSensitive(_currentAnimationKey))
        {
            Play(_currentAnimationKey);
        }
    }

    public void SetAimRootPositionOffsetActive(bool active, bool instant = false)
    {
        _useAimRootPositionOffset = active;

        if (instant)
        {
            SnapRootPositionOffset();
        }
    }

    public void Play(FirstPersonWeaponAnimationKey key)
    {
        if (IsContinuous(key))
        {
            PlayContinuous(key);
            return;
        }

        PlayTransient(key, GetDefaultReturnKey(key));
    }

    public void PlayIdle() => PlayContinuous(FirstPersonWeaponAnimationKey.Idle);
    public void PlayWalk() => PlayContinuous(FirstPersonWeaponAnimationKey.Walk);
    public void PlaySprint() => PlayContinuous(FirstPersonWeaponAnimationKey.Sprint);
    public void PlayAimIdle() => PlayContinuous(FirstPersonWeaponAnimationKey.AimIdle);
    public void PlayAimWalk() => PlayContinuous(FirstPersonWeaponAnimationKey.AimWalk);
    public void PlayAimShoot(bool lastRound = false)
    {
        Play(lastRound ? FirstPersonWeaponAnimationKey.AimShootLast : FirstPersonWeaponAnimationKey.AimShoot);
    }

    public void SetLoopPlaybackSpeed(float speed) => _animationPlayer?.SetLoopPlaybackSpeed(speed);
    public float PlayStartup()
    {
        if (IsContinuous(_startupAnimation))
        {
            PlayContinuous(_startupAnimation, STARTUP_CROSS_FADE_DURATION);
        }
        else
        {
            PlayTransient(_startupAnimation, GetDefaultReturnKey(_startupAnimation), STARTUP_CROSS_FADE_DURATION);
        }

        return GetCurrentAnimationLength(_startupAnimation);
    }

    public float PlayDraw() => PlayTransitionAndGetDuration(FirstPersonWeaponAnimationKey.Draw);
    public float PlayHide() => PlayTransitionAndGetDuration(FirstPersonWeaponAnimationKey.Hide);
    public void Shoot(bool lastRound = false) => Play(lastRound ? FirstPersonWeaponAnimationKey.ShootLast : FirstPersonWeaponAnimationKey.Shoot);
    public float PlayDryEmpty(bool returnToAim = false)
    {
        PlayTransient(FirstPersonWeaponAnimationKey.DryEmpty, returnToAim ? FirstPersonWeaponAnimationKey.AimIdle : FirstPersonWeaponAnimationKey.Idle);
        return GetCurrentAnimationLength(FirstPersonWeaponAnimationKey.DryEmpty);
    }

    public void Reload(bool full = false) => Play(full ? FirstPersonWeaponAnimationKey.ReloadFull : FirstPersonWeaponAnimationKey.Reload);
    public void PlayMisfire() => Play(FirstPersonWeaponAnimationKey.Misfire);
    public void PlayRevival(bool lastRound = false) => Play(lastRound ? FirstPersonWeaponAnimationKey.RevivalLast : FirstPersonWeaponAnimationKey.Revival);
    public float GetAnimationLength(FirstPersonWeaponAnimationKey key) => _clips == null ? 0f : _clips.GetLength(key, _weaponCondition);
    public float GetAnimationFrameTime(FirstPersonWeaponAnimationKey key, int frame) => _clips == null ? 0f : _clips.GetFrameTime(key, _weaponCondition, frame);
    public float GetNextAnimationLength(FirstPersonWeaponAnimationKey key) => GetNextAnimationStartDelay(key) + GetAnimationLength(key);
    public float GetNextAnimationFrameTime(FirstPersonWeaponAnimationKey key, int frame) => GetNextAnimationStartDelay(key) + GetAnimationFrameTime(key, frame);
    public float GetCurrentAnimationLength(FirstPersonWeaponAnimationKey key) => _lastAnimationStartDelay + GetAnimationLength(key);

    public void SetEquippedArmor(ItemData armorItemData)
    {
        _equippedArmor = armorItemData != null && armorItemData.ItemType == ItemType.Armor ? armorItemData : null;
        EnsureInitialized();
        ApplyEquippedArmorHandsMesh();
    }

    public void PlayTransient(FirstPersonWeaponAnimationKey key, FirstPersonWeaponAnimationKey returnKey)
    {
        PlayTransient(key, returnKey, GetCrossFadeDuration(key));
    }

    private void PlayTransient(FirstPersonWeaponAnimationKey key, FirstPersonWeaponAnimationKey returnKey, float crossFadeDuration)
    {
        if (PlayInternal(key, true, crossFadeDuration) == false)
        {
            return;
        }

        _returnAnimationKey = returnKey;
        _returnTime = Time.time + Mathf.Max(crossFadeDuration, GetCurrentAnimationLength(key));
        _hasPendingReturn = true;
    }

    private void PlayContinuous(FirstPersonWeaponAnimationKey key)
    {
        PlayContinuous(key, GetCrossFadeDuration(key));
    }

    private void PlayContinuous(FirstPersonWeaponAnimationKey key, float crossFadeDuration)
    {
        PlayInternal(key, false, crossFadeDuration);
        _hasPendingReturn = false;
    }

    private bool PlayInternal(FirstPersonWeaponAnimationKey key, bool restartIfSame, float crossFadeDuration)
    {
        EnsureInitialized();

        if (_animationPlayer == null || _clips == null)
        {
            return false;
        }

        FirstPersonWeaponAnimationClipPair pair = _clips.GetPair(key, _weaponCondition);

        if (pair.HasAnyClip == false)
        {
            Debug.LogWarning($"[{nameof(FirstPersonWeaponController)}] No clips assigned for {key}.", this);
            return false;
        }

        _lastAnimationStartDelay = GetNextAnimationStartDelay(key, crossFadeDuration);
        _animationPlayer.Play(key, pair, restartIfSame, crossFadeDuration);
        _currentAnimationKey = key;
        return true;
    }

    private void EnsureInitialized()
    {
        if (_weaponFollower == null && _weaponFollowSource != null && _weaponFollowTarget != null)
        {
            _weaponFollower = new FirstPersonWeaponFollower(_weaponFollowSource, _weaponFollowTarget);
        }

        if (_handsMeshRoot == null && _handsAnimator != null)
        {
            _handsMeshRoot = _handsAnimator.transform;
        }

        if (_handsMeshSwitcher == null && _handsMeshRoot != null)
        {
            _handsMeshSwitcher = new FirstPersonHandsMeshSwitcher(_handsMeshRoot, _defaultHandsMeshName);
            ApplyEquippedArmorHandsMesh();
        }

        if (_animationPlayer == null && (_handsAnimator != null || _weaponAnimator != null))
        {
            _animationPlayer = new FirstPersonWeaponAnimationPlayer(_weaponAnimator, _handsAnimator, _crossFadeDuration);
        }
    }

    private void DisposeAnimationPlayer()
    {
        _weaponFollower?.SetEnabled(false);
        _animationPlayer?.Dispose();
        _animationPlayer = null;
    }

    private void UpdateRootPositionOffset(float deltaTime)
    {
        Vector3 targetPosition = GetActiveRootPositionOffset();

        if (_rootPositionOffsetLerpSpeed <= 0f || deltaTime <= 0f)
        {
            transform.localPosition = targetPosition;
            return;
        }

        float t = 1f - Mathf.Exp(-_rootPositionOffsetLerpSpeed * deltaTime);
        Vector3 nextPosition = Vector3.Lerp(transform.localPosition, targetPosition, t);
        transform.localPosition = (nextPosition - targetPosition).sqrMagnitude <= ROOT_POSITION_OFFSET_EPSILON ? targetPosition : nextPosition;
    }

    private void SnapRootPositionOffset() => transform.localPosition = GetActiveRootPositionOffset();

    private Vector3 GetActiveRootPositionOffset() => _useAimRootPositionOffset ? _aimRootPositionOffset : _rootPositionOffset;

    private static FirstPersonWeaponAnimationKey GetDefaultReturnKey(FirstPersonWeaponAnimationKey key)
    {
        return key switch
        {
            FirstPersonWeaponAnimationKey.SprintStart => FirstPersonWeaponAnimationKey.Sprint,
            FirstPersonWeaponAnimationKey.AimIn => FirstPersonWeaponAnimationKey.AimIdle,
            FirstPersonWeaponAnimationKey.AimShoot => FirstPersonWeaponAnimationKey.AimIdle,
            FirstPersonWeaponAnimationKey.AimShootLast => FirstPersonWeaponAnimationKey.AimIdle,
            _ => FirstPersonWeaponAnimationKey.Idle
        };
    }

    private static bool IsContinuous(FirstPersonWeaponAnimationKey key)
    {
        return key == FirstPersonWeaponAnimationKey.Idle ||
               key == FirstPersonWeaponAnimationKey.Walk ||
               key == FirstPersonWeaponAnimationKey.Sprint ||
               key == FirstPersonWeaponAnimationKey.AimIdle ||
               key == FirstPersonWeaponAnimationKey.AimWalk;
    }

    private static bool IsConditionSensitive(FirstPersonWeaponAnimationKey key)
    {
        return key == FirstPersonWeaponAnimationKey.Idle ||
               key == FirstPersonWeaponAnimationKey.Walk ||
               key == FirstPersonWeaponAnimationKey.Sprint ||
               key == FirstPersonWeaponAnimationKey.AimIdle ||
               key == FirstPersonWeaponAnimationKey.AimWalk;
    }

    private void ApplyEquippedArmorHandsMesh() => _handsMeshSwitcher?.SetMesh(_equippedArmor == null ? string.Empty : _equippedArmor.FirstPersonHandsMeshName);
    private float GetCrossFadeDuration(FirstPersonWeaponAnimationKey key) => key == FirstPersonWeaponAnimationKey.Walk || key == FirstPersonWeaponAnimationKey.AimWalk ? _walkCrossFadeDuration : _crossFadeDuration;
    private float GetNextAnimationStartDelay(FirstPersonWeaponAnimationKey key) => GetNextAnimationStartDelay(key, GetCrossFadeDuration(key));
    private float GetNextAnimationStartDelay(FirstPersonWeaponAnimationKey key, float crossFadeDuration) => _animationPlayer != null && _animationPlayer.HasActivePair && ShouldDelayAnimationStart(key) ? Mathf.Max(0f, crossFadeDuration) : 0f;
    private bool IsRuntimeControlled => TryGetComponent(out FirstPersonWeaponRuntimeController _);
    private static bool ShouldDelayAnimationStart(FirstPersonWeaponAnimationKey key) => IsContinuous(key) == false;

    private float PlayTransitionAndGetDuration(FirstPersonWeaponAnimationKey key)
    {
        if (_currentAnimationKey != key)
        {
            Play(key);
        }

        return GetCurrentAnimationLength(key);
    }
}
