using System;
using Animancer;
using UnityEngine;

internal sealed class FirstPersonWeaponAnimationPlayer : IDisposable
{
    private readonly Animator _weaponAnimator;
    private readonly Animator _handsAnimator;
    private readonly float _defaultCrossFadeDuration;

    private AnimancerComponent _weaponAnimancer;
    private AnimancerComponent _handsAnimancer;
    private AnimancerState _activeWeaponState;
    private AnimancerState _activeHandsState;
    private AnimancerState _fadingWeaponState;
    private AnimancerState _fadingHandsState;
    private FirstPersonWeaponAnimationClipPair _activePair;
    private FirstPersonWeaponAnimationClipPair _fadingPair;
    private FirstPersonWeaponAnimationClipPair _currentPair;
    private FirstPersonWeaponAnimationKey _activeKey;
    private FirstPersonWeaponAnimationKey _fadingKey;
    private FirstPersonWeaponAnimationKey _currentKey;
    private float _fadeElapsed;
    private float _fadeDuration;
    private double _loopPlaybackSpeed = 1d;
    private bool _hasActivePair;
    private bool _isFading;

    public FirstPersonWeaponAnimationPlayer(Animator weaponAnimator, Animator handsAnimator, float crossFadeDuration)
    {
        _weaponAnimator = weaponAnimator;
        _handsAnimator = handsAnimator;
        _defaultCrossFadeDuration = Mathf.Max(0f, crossFadeDuration);
    }

    public bool HasActivePair => _hasActivePair;

    public void SetLoopPlaybackSpeed(float speed)
    {
        _loopPlaybackSpeed = Mathf.Max(0.01f, speed);
        ApplyLoopPlaybackSpeed();
    }

    public void Play(FirstPersonWeaponAnimationKey key, FirstPersonWeaponAnimationClipPair pair, bool restartIfSame)
    {
        Play(key, pair, restartIfSame, _defaultCrossFadeDuration);
    }

    public void Play(FirstPersonWeaponAnimationKey key, FirstPersonWeaponAnimationClipPair pair, bool restartIfSame, float crossFadeDuration)
    {
        if ((_weaponAnimator == null && _handsAnimator == null) || pair.HasAnyClip == false)
        {
            return;
        }

        if (_hasActivePair && restartIfSame == false && _currentKey == key && _currentPair.IsSameAs(pair))
        {
            return;
        }

        EnsureAnimancers();

        float resolvedCrossFadeDuration = Mathf.Max(0f, crossFadeDuration);
        float animancerFadeDuration = _hasActivePair ? resolvedCrossFadeDuration : 0f;
        double weaponStartTime = GetStartTime(key, pair.WeaponClip, false);
        double handsStartTime = GetStartTime(key, pair.HandsClip, true);
        bool delayClipStart = _hasActivePair && resolvedCrossFadeDuration > 0f && ShouldDelayClipStart(key);
        AnimancerState weaponState = PlayClip(_weaponAnimancer, pair.WeaponClip, key, weaponStartTime, restartIfSame, animancerFadeDuration, delayClipStart);
        AnimancerState handsState = PlayClip(_handsAnimancer, pair.HandsClip, key, handsStartTime, restartIfSame, animancerFadeDuration, delayClipStart);

        if (_hasActivePair == false || resolvedCrossFadeDuration <= 0f)
        {
            CompleteImmediate(key, pair, weaponState, handsState);
            return;
        }

        _fadingPair = pair;
        _fadingKey = key;
        _fadingWeaponState = weaponState;
        _fadingHandsState = handsState;
        _fadeElapsed = 0f;
        _fadeDuration = resolvedCrossFadeDuration;
        _currentPair = pair;
        _currentKey = key;
        _isFading = true;
    }

    public void Tick(float deltaTime)
    {
        if (_isFading == false)
        {
            return;
        }

        _fadeElapsed += Mathf.Max(0f, deltaTime);
        float weight = _fadeDuration <= 0f ? 1f : Mathf.Clamp01(_fadeElapsed / _fadeDuration);

        if (weight >= 1f)
        {
            CompleteFade();
        }
    }

    public void Dispose()
    {
        DestroyGraph(_weaponAnimancer);
        DestroyGraph(_handsAnimancer);

        _hasActivePair = false;
        _isFading = false;
        _activeWeaponState = null;
        _activeHandsState = null;
        _fadingWeaponState = null;
        _fadingHandsState = null;
    }

    private void EnsureAnimancers()
    {
        _weaponAnimancer ??= GetOrCreateAnimancer(_weaponAnimator);
        _handsAnimancer ??= GetOrCreateAnimancer(_handsAnimator);
    }

    private AnimancerState PlayClip(AnimancerComponent animancer, AnimationClip clip, FirstPersonWeaponAnimationKey key, double startTime, bool restartIfSame, float crossFadeDuration, bool delayClipStart)
    {
        if (animancer == null || clip == null)
        {
            return null;
        }

        AnimancerState state = crossFadeDuration <= 0f
            ? animancer.Play(clip)
            : animancer.Play(clip, crossFadeDuration, restartIfSame ? FadeMode.FromStart : FadeMode.FixedDuration);

        state.ApplyFootIK = false;
        state.TimeD = startTime;
        state.Speed = delayClipStart ? 0f : (float)GetPlaybackSpeed(key);
        return state;
    }

    private double GetStartTime(FirstPersonWeaponAnimationKey nextKey, AnimationClip nextClip, bool hands)
    {
        if (_hasActivePair == false)
        {
            return 0d;
        }

        FirstPersonWeaponAnimationKey sourceKey = GetDominantKey();

        if (CanSynchronizePhase(sourceKey, nextKey) == false)
        {
            return 0d;
        }

        FirstPersonWeaponAnimationClipPair sourcePair = GetDominantPair();
        AnimationClip sourceClip = hands ? sourcePair.HandsClip : sourcePair.WeaponClip;
        AnimancerState sourceState = hands ? GetDominantHandsState() : GetDominantWeaponState();

        if (sourceClip == null || nextClip == null || sourceClip.length <= 0f || nextClip.length <= 0f || sourceState == null)
        {
            return 0d;
        }

        double normalizedTime = sourceState.TimeD / sourceClip.length;
        normalizedTime -= Math.Floor(normalizedTime);

        return normalizedTime * nextClip.length;
    }

    private FirstPersonWeaponAnimationKey GetDominantKey()
    {
        if (_isFading == false)
        {
            return _activeKey;
        }

        return GetFadeWeight() > 0.5f ? _fadingKey : _activeKey;
    }

    private FirstPersonWeaponAnimationClipPair GetDominantPair()
    {
        if (_isFading == false)
        {
            return _activePair;
        }

        return GetFadeWeight() > 0.5f ? _fadingPair : _activePair;
    }

    private AnimancerState GetDominantWeaponState()
    {
        if (_isFading == false)
        {
            return _activeWeaponState;
        }

        return GetFadeWeight() > 0.5f ? _fadingWeaponState : _activeWeaponState;
    }

    private AnimancerState GetDominantHandsState()
    {
        if (_isFading == false)
        {
            return _activeHandsState;
        }

        return GetFadeWeight() > 0.5f ? _fadingHandsState : _activeHandsState;
    }

    private float GetFadeWeight() => _fadeDuration <= 0f ? 1f : Mathf.Clamp01(_fadeElapsed / _fadeDuration);

    private void CompleteImmediate(FirstPersonWeaponAnimationKey key, FirstPersonWeaponAnimationClipPair pair, AnimancerState weaponState, AnimancerState handsState)
    {
        _activePair = pair;
        _activeKey = key;
        _activeWeaponState = weaponState;
        _activeHandsState = handsState;
        _currentPair = pair;
        _currentKey = key;
        _hasActivePair = true;
        _isFading = false;
        _fadingWeaponState = null;
        _fadingHandsState = null;
        SetStateSpeed(_activeWeaponState, GetPlaybackSpeed(key));
        SetStateSpeed(_activeHandsState, GetPlaybackSpeed(key));
    }

    private void CompleteFade()
    {
        _activePair = _fadingPair;
        _activeKey = _fadingKey;
        _activeWeaponState = _fadingWeaponState;
        _activeHandsState = _fadingHandsState;
        _currentPair = _activePair;
        _currentKey = _activeKey;
        _hasActivePair = true;
        _isFading = false;
        _fadingWeaponState = null;
        _fadingHandsState = null;
        SetStateSpeed(_activeWeaponState, GetPlaybackSpeed(_activeKey));
        SetStateSpeed(_activeHandsState, GetPlaybackSpeed(_activeKey));
    }

    private void ApplyLoopPlaybackSpeed()
    {
        if (_hasActivePair == false || IsLoopLike(_currentKey) == false)
        {
            return;
        }

        if (_activeKey == _currentKey)
        {
            SetStateSpeed(_activeWeaponState, _loopPlaybackSpeed);
            SetStateSpeed(_activeHandsState, _loopPlaybackSpeed);
        }

        if (_isFading && _fadingKey == _currentKey)
        {
            SetStateSpeed(_fadingWeaponState, _loopPlaybackSpeed);
            SetStateSpeed(_fadingHandsState, _loopPlaybackSpeed);
        }
    }

    private static void SetStateSpeed(AnimancerState state, double speed)
    {
        if (state != null)
        {
            state.Speed = (float)speed;
        }
    }

    private static bool CanSynchronizePhase(FirstPersonWeaponAnimationKey sourceKey, FirstPersonWeaponAnimationKey targetKey)
    {
        return IsLoopLike(sourceKey) && IsLoopLike(targetKey);
    }

    private static bool ShouldDelayClipStart(FirstPersonWeaponAnimationKey key) => IsLoopLike(key) == false;

    private double GetPlaybackSpeed(FirstPersonWeaponAnimationKey key) => IsLoopLike(key) ? _loopPlaybackSpeed : 1d;

    private static AnimancerComponent GetOrCreateAnimancer(Animator animator)
    {
        if (animator == null)
        {
            return null;
        }

        if (animator.TryGetComponent(out AnimancerComponent animancer) == false)
        {
            animancer = animator.gameObject.AddComponent<AnimancerComponent>();
        }

        animancer.Animator = animator;
        return animancer;
    }

    private static void DestroyGraph(AnimancerComponent animancer)
    {
        if (animancer != null && animancer.IsGraphInitialized)
        {
            animancer.Graph.Destroy();
        }
    }

    private static bool IsLoopLike(FirstPersonWeaponAnimationKey key)
    {
        return key == FirstPersonWeaponAnimationKey.Idle ||
               key == FirstPersonWeaponAnimationKey.Walk ||
               key == FirstPersonWeaponAnimationKey.Sprint ||
               key == FirstPersonWeaponAnimationKey.AimIdle ||
               key == FirstPersonWeaponAnimationKey.AimWalk;
    }
}
