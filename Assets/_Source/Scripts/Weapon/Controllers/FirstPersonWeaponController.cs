using UnityEngine;
using UnityEngine.Serialization;

public sealed class FirstPersonWeaponController : MonoBehaviour
{
    [SerializeField] private Animator _handsAnimator;
    [SerializeField] private Animator _weaponAnimator;
    [SerializeField] private Transform _weaponFollowSource;
    [SerializeField] private Transform _weaponFollowTarget;
    [SerializeField] private string _weaponFollowSourceName = "lead_gun";
    [SerializeField] private string _weaponFollowTargetName = "wpn_body";
    [SerializeField] private bool _followHands = true;
    [SerializeField] private Vector3 _weaponFollowPositionOffset;
    [SerializeField] private Vector3 _weaponFollowRotationOffset;
    [SerializeField] private FirstPersonWeaponAnimationClipSet _clips = new();
    [SerializeField] [Min(0f)] private float _crossFadeDuration = 0.05f;
    [SerializeField] private WeaponCondition _weaponCondition = WeaponCondition.Normal;
    [SerializeField] private FirstPersonWeaponAnimationKey _startupAnimation = FirstPersonWeaponAnimationKey.Draw;
    [SerializeField] private bool _playStartupAnimation = true;

    private FirstPersonWeaponAnimationPlayer _animationPlayer;
    private FirstPersonWeaponFollower _weaponFollower;
    private FirstPersonWeaponAnimationKey _currentAnimationKey = FirstPersonWeaponAnimationKey.Idle;
    private FirstPersonWeaponAnimationKey _returnAnimationKey = FirstPersonWeaponAnimationKey.Idle;
    private float _returnTime;
    private bool _hasPendingReturn;

    public WeaponCondition Condition => _weaponCondition;
    public FirstPersonWeaponAnimationKey CurrentAnimationKey => _currentAnimationKey;

    private void Awake()
    {
        EnsureInitialized();
    }

    private void OnEnable()
    {
        EnsureInitialized();

        if (_playStartupAnimation)
        {
            Play(_startupAnimation);
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
    public void Shoot(bool lastRound = false) => Play(lastRound ? FirstPersonWeaponAnimationKey.ShootLast : FirstPersonWeaponAnimationKey.Shoot);
    public void Reload(bool full = false) => Play(full ? FirstPersonWeaponAnimationKey.ReloadFull : FirstPersonWeaponAnimationKey.Reload);
    public void PlayMisfire() => Play(FirstPersonWeaponAnimationKey.Misfire);
    public void PlayRevival(bool lastRound = false) => Play(lastRound ? FirstPersonWeaponAnimationKey.RevivalLast : FirstPersonWeaponAnimationKey.Revival);

    public void PlayTransient(FirstPersonWeaponAnimationKey key, FirstPersonWeaponAnimationKey returnKey)
    {
        PlayInternal(key, true);
        _returnAnimationKey = returnKey;
        _returnTime = Time.time + Mathf.Max(_crossFadeDuration, _clips == null ? 0f : _clips.GetLength(key, _weaponCondition));
        _hasPendingReturn = true;
    }

    private void PlayContinuous(FirstPersonWeaponAnimationKey key)
    {
        PlayInternal(key, false);
        _hasPendingReturn = false;
    }

    private void PlayInternal(FirstPersonWeaponAnimationKey key, bool restartIfSame)
    {
        EnsureInitialized();

        if (_animationPlayer == null || _clips == null)
        {
            return;
        }

        FirstPersonWeaponAnimationClipPair pair = _clips.GetPair(key, _weaponCondition);

        if (pair.HasAnyClip == false)
        {
            Debug.LogWarning($"[{nameof(FirstPersonWeaponController)}] No clips assigned for {key}.", this);
            return;
        }

        _animationPlayer.Play(pair, restartIfSame);
        _currentAnimationKey = key;
    }

    private void EnsureInitialized()
    {
        if (_handsAnimator == null)
        {
            _handsAnimator = transform.Find("Hands")?.GetComponent<Animator>();
        }

        if (_weaponAnimator == null)
        {
            _weaponAnimator = transform.Find("Weapon")?.GetComponent<Animator>();
        }

        if (_weaponFollowSource == null)
        {
            _weaponFollowSource = FindChildByName(_handsAnimator == null ? null : _handsAnimator.transform, _weaponFollowSourceName);
        }

        if (_weaponFollowTarget == null)
        {
            _weaponFollowTarget = FindChildByName(_weaponAnimator == null ? null : _weaponAnimator.transform, _weaponFollowTargetName);
        }

        if (_weaponFollower == null && _weaponFollowSource != null && _weaponFollowTarget != null)
        {
            _weaponFollower = new FirstPersonWeaponFollower(_weaponFollowSource, _weaponFollowTarget);
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

    private static Transform FindChildByName(Transform root, string childName)
    {
        if (root == null || string.IsNullOrWhiteSpace(childName))
        {
            return null;
        }

        Transform[] children = root.GetComponentsInChildren<Transform>(true);

        for (int i = 0; i < children.Length; i++)
        {
            if (children[i].name == childName)
            {
                return children[i];
            }
        }

        return null;
    }

    private static FirstPersonWeaponAnimationKey GetDefaultReturnKey(FirstPersonWeaponAnimationKey key)
    {
        return key == FirstPersonWeaponAnimationKey.SprintStart ? FirstPersonWeaponAnimationKey.Sprint : FirstPersonWeaponAnimationKey.Idle;
    }

    private static bool IsContinuous(FirstPersonWeaponAnimationKey key)
    {
        return key == FirstPersonWeaponAnimationKey.Idle ||
               key == FirstPersonWeaponAnimationKey.Walk ||
               key == FirstPersonWeaponAnimationKey.Sprint;
    }

    private static bool IsConditionSensitive(FirstPersonWeaponAnimationKey key)
    {
        return key == FirstPersonWeaponAnimationKey.Idle ||
               key == FirstPersonWeaponAnimationKey.Walk ||
               key == FirstPersonWeaponAnimationKey.Sprint;
    }
}
