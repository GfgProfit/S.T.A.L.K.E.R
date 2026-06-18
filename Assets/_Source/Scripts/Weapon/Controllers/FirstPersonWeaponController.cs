using UnityEngine;

public sealed class FirstPersonWeaponController : MonoBehaviour
{
    private const float STARTUP_CROSS_FADE_DURATION = 0f;
    private const float ROOT_POSITION_OFFSET_EPSILON = 0.000001f;
    private const float ROOT_ROTATION_OFFSET_EPSILON = 0.01f;
    private const string CAMERA_BONE_SOURCE_NAME = "camera_bone";
    private const string CAMERA_ALL_BONE_OBJECT_NAME = "Camera All Bone";
    private const string CAMERA_BONE_TARGET_OBJECT_NAME = "Camera Bone";

    [SerializeField] private Animator _handsAnimator;
    [SerializeField] private Animator _weaponAnimator;
    [SerializeField] private Animator _cameraAnimator;
    [SerializeField] private Transform _handsMeshRoot;
    [SerializeField] private string _defaultHandsMeshName = "default";
    [SerializeField] private Transform _weaponFollowSource;
    [SerializeField] private Transform _weaponFollowTarget;
    [SerializeField] private Transform _cameraBoneSource;
    [SerializeField] private Transform _cameraBoneTarget;
    [SerializeField] private bool _followHands = true;
    [SerializeField] private bool _forceAim;
    [SerializeField] private Vector3 _rootPositionOffset;
    [SerializeField] private Vector3 _aimRootPositionOffset;
    [SerializeField] private Vector3 _aimRootRotationOffset;
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
    private Quaternion _rootBaseRotation;
    private Vector3 _cameraBoneTargetBasePosition;
    private Quaternion _cameraBoneTargetBaseRotation;
    private bool _hasCameraBoneTargetBasePose;

    public WeaponCondition Condition => _weaponCondition;
    public FirstPersonWeaponAnimationKey CurrentAnimationKey => _currentAnimationKey;
    public bool ForceAim => _forceAim;

    private void Awake()
    {
        _rootBaseRotation = transform.localRotation;
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
        UpdateCameraBoneFollow();
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
    public void PlayAimWalk(FirstPersonWeaponAnimationKey key) => PlayContinuous(IsAimWalkKey(key) ? key : FirstPersonWeaponAnimationKey.AimWalk);
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
        FirstPersonWeaponAnimationKey key = returnToAim ? FirstPersonWeaponAnimationKey.AimDry : FirstPersonWeaponAnimationKey.DryEmpty;
        PlayTransient(key, returnToAim ? FirstPersonWeaponAnimationKey.AimIdle : FirstPersonWeaponAnimationKey.Idle);
        return GetCurrentAnimationLength(key);
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
        ResolveCameraBoneTransforms();

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

        if (_animationPlayer == null && (_handsAnimator != null || _weaponAnimator != null || _cameraAnimator != null))
        {
            _animationPlayer = new FirstPersonWeaponAnimationPlayer(_weaponAnimator, _handsAnimator, _cameraAnimator, _crossFadeDuration);
        }
    }

    private void DisposeAnimationPlayer()
    {
        _weaponFollower?.SetEnabled(false);
        RestoreCameraBoneTargetPose();
        _animationPlayer?.Dispose();
        _animationPlayer = null;
    }

    private void UpdateRootPositionOffset(float deltaTime)
    {
        Vector3 targetPosition = GetActiveRootPositionOffset();
        Quaternion targetRotation = GetActiveRootRotationOffset();

        if (_rootPositionOffsetLerpSpeed <= 0f || deltaTime <= 0f)
        {
            transform.SetLocalPositionAndRotation(targetPosition, targetRotation);
            return;
        }

        float t = 1f - Mathf.Exp(-_rootPositionOffsetLerpSpeed * deltaTime);
        Vector3 nextPosition = Vector3.Lerp(transform.localPosition, targetPosition, t);
        Quaternion nextRotation = Quaternion.Slerp(transform.localRotation, targetRotation, t);

        if ((nextPosition - targetPosition).sqrMagnitude <= ROOT_POSITION_OFFSET_EPSILON)
        {
            nextPosition = targetPosition;
        }

        if (Quaternion.Angle(nextRotation, targetRotation) <= ROOT_ROTATION_OFFSET_EPSILON)
        {
            nextRotation = targetRotation;
        }

        transform.SetLocalPositionAndRotation(nextPosition, nextRotation);
    }

    private void SnapRootPositionOffset() => transform.SetLocalPositionAndRotation(GetActiveRootPositionOffset(), GetActiveRootRotationOffset());

    private Vector3 GetActiveRootPositionOffset() => _useAimRootPositionOffset ? _aimRootPositionOffset : _rootPositionOffset;
    private Quaternion GetActiveRootRotationOffset() => _rootBaseRotation * Quaternion.Euler(_useAimRootPositionOffset ? _aimRootRotationOffset : Vector3.zero);

    private void UpdateCameraBoneFollow()
    {
        if (_cameraBoneSource == null || _cameraBoneTarget == null)
        {
            return;
        }

        CacheCameraBoneTargetBasePose();
        _cameraBoneTarget.SetLocalPositionAndRotation(_cameraBoneSource.localPosition, _cameraBoneSource.localRotation);
    }

    private void ResolveCameraBoneTransforms()
    {
        if (_cameraBoneSource == null)
        {
            _cameraBoneSource = FindChildRecursive(transform, CAMERA_BONE_SOURCE_NAME);
        }

        if (_cameraBoneTarget == null)
        {
            Transform root = GetRootTransform(transform);
            Transform cameraAllBone = FindChildRecursive(root, CAMERA_ALL_BONE_OBJECT_NAME);
            _cameraBoneTarget = cameraAllBone == null ? FindChildRecursive(root, CAMERA_BONE_TARGET_OBJECT_NAME) : FindChildRecursive(cameraAllBone, CAMERA_BONE_TARGET_OBJECT_NAME);
        }

        if (_cameraAnimator == null && _cameraBoneSource != null)
        {
            _cameraAnimator = _cameraBoneSource.GetComponent<Animator>();
        }

        if (_cameraAnimator == null && _cameraBoneTarget != null)
        {
            _cameraAnimator = _cameraBoneTarget.GetComponent<Animator>();
        }

        CacheCameraBoneTargetBasePose();
    }

    private void CacheCameraBoneTargetBasePose()
    {
        if (_cameraBoneTarget == null || _hasCameraBoneTargetBasePose)
        {
            return;
        }

        _cameraBoneTargetBasePosition = _cameraBoneTarget.localPosition;
        _cameraBoneTargetBaseRotation = _cameraBoneTarget.localRotation;
        _hasCameraBoneTargetBasePose = true;
    }

    private void RestoreCameraBoneTargetPose()
    {
        if (_cameraBoneTarget == null || _hasCameraBoneTargetBasePose == false)
        {
            return;
        }

        _cameraBoneTarget.SetLocalPositionAndRotation(_cameraBoneTargetBasePosition, _cameraBoneTargetBaseRotation);
    }

    private static FirstPersonWeaponAnimationKey GetDefaultReturnKey(FirstPersonWeaponAnimationKey key)
    {
        return key switch
        {
            FirstPersonWeaponAnimationKey.SprintStart => FirstPersonWeaponAnimationKey.Sprint,
            FirstPersonWeaponAnimationKey.AimIn => FirstPersonWeaponAnimationKey.AimIdle,
            FirstPersonWeaponAnimationKey.AimShoot => FirstPersonWeaponAnimationKey.AimIdle,
            FirstPersonWeaponAnimationKey.AimShootLast => FirstPersonWeaponAnimationKey.AimIdle,
            FirstPersonWeaponAnimationKey.AimDry => FirstPersonWeaponAnimationKey.AimIdle,
            _ => FirstPersonWeaponAnimationKey.Idle
        };
    }

    private static bool IsContinuous(FirstPersonWeaponAnimationKey key)
    {
        return key == FirstPersonWeaponAnimationKey.Idle ||
               key == FirstPersonWeaponAnimationKey.Walk ||
               key == FirstPersonWeaponAnimationKey.Sprint ||
               key == FirstPersonWeaponAnimationKey.AimIdle ||
               IsAimWalkKey(key);
    }

    private static bool IsConditionSensitive(FirstPersonWeaponAnimationKey key)
    {
        return key == FirstPersonWeaponAnimationKey.Idle ||
               key == FirstPersonWeaponAnimationKey.Walk ||
               key == FirstPersonWeaponAnimationKey.Sprint ||
               key == FirstPersonWeaponAnimationKey.AimIdle ||
               IsAimWalkKey(key);
    }

    private void ApplyEquippedArmorHandsMesh() => _handsMeshSwitcher?.SetMesh(_equippedArmor == null ? string.Empty : _equippedArmor.FirstPersonHandsMeshName);
    private float GetCrossFadeDuration(FirstPersonWeaponAnimationKey key) => key == FirstPersonWeaponAnimationKey.Walk || IsAimWalkKey(key) ? _walkCrossFadeDuration : _crossFadeDuration;
    private float GetNextAnimationStartDelay(FirstPersonWeaponAnimationKey key) => GetNextAnimationStartDelay(key, GetCrossFadeDuration(key));
    private float GetNextAnimationStartDelay(FirstPersonWeaponAnimationKey key, float crossFadeDuration) => _animationPlayer != null && _animationPlayer.HasActivePair && ShouldDelayAnimationStart(key) ? Mathf.Max(0f, crossFadeDuration) : 0f;
    private bool IsRuntimeControlled => TryGetComponent(out FirstPersonWeaponRuntimeController _);
    private static bool ShouldDelayAnimationStart(FirstPersonWeaponAnimationKey key) => IsContinuous(key) == false;

    private static bool IsAimWalkKey(FirstPersonWeaponAnimationKey key)
    {
        return key == FirstPersonWeaponAnimationKey.AimWalk ||
               key == FirstPersonWeaponAnimationKey.AimWalkBackward ||
               key == FirstPersonWeaponAnimationKey.AimWalkLeft ||
               key == FirstPersonWeaponAnimationKey.AimWalkRight;
    }

    private static Transform GetRootTransform(Transform target)
    {
        Transform current = target;

        while (current.parent != null)
        {
            current = current.parent;
        }

        return current;
    }

    private static Transform FindChildRecursive(Transform root, string childName)
    {
        if (root == null)
        {
            return null;
        }

        if (root.name == childName)
        {
            return root;
        }

        for (int i = 0; i < root.childCount; i++)
        {
            Transform child = FindChildRecursive(root.GetChild(i), childName);

            if (child != null)
            {
                return child;
            }
        }

        return null;
    }

    private float PlayTransitionAndGetDuration(FirstPersonWeaponAnimationKey key)
    {
        if (_currentAnimationKey != key)
        {
            Play(key);
        }

        return GetCurrentAnimationLength(key);
    }
}
