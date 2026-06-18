using System;
using Animancer;
using UnityEngine;

internal sealed class FirstPersonWeaponAnimationPlayer : IDisposable
{
    private readonly Animator _weaponAnimator;
    private readonly Animator _handsAnimator;
    private readonly Animator _cameraAnimator;
    private readonly float _defaultCrossFadeDuration;

    private AnimancerComponent _weaponAnimancer;
    private AnimancerComponent _handsAnimancer;
    private AnimancerComponent _cameraAnimancer;
    private AnimancerState _activeWeaponState;
    private AnimancerState _activeHandsState;
    private AnimancerState _activeCameraState;
    private AnimancerState _fadingWeaponState;
    private AnimancerState _fadingHandsState;
    private AnimancerState _fadingCameraState;
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

    public FirstPersonWeaponAnimationPlayer(Animator weaponAnimator, Animator handsAnimator, Animator cameraAnimator, float crossFadeDuration)
    {
        _weaponAnimator = weaponAnimator;
        _handsAnimator = handsAnimator;
        _cameraAnimator = cameraAnimator;
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
        if ((_weaponAnimator == null && _handsAnimator == null && _cameraAnimator == null) || pair.HasAnyClip == false)
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
        double weaponStartTime = GetStartTime(key, pair.WeaponClip, FirstPersonWeaponAnimationChannel.Weapon);
        double handsStartTime = GetStartTime(key, pair.HandsClip, FirstPersonWeaponAnimationChannel.Hands);
        double cameraStartTime = GetStartTime(key, pair.CameraClip, FirstPersonWeaponAnimationChannel.Camera);
        bool delayClipStart = _hasActivePair && resolvedCrossFadeDuration > 0f && ShouldDelayClipStart(key);
        AnimancerState weaponState = PlayClip(_weaponAnimancer, pair.WeaponClip, key, weaponStartTime, restartIfSame, animancerFadeDuration, delayClipStart);
        AnimancerState handsState = PlayClip(_handsAnimancer, pair.HandsClip, key, handsStartTime, restartIfSame, animancerFadeDuration, delayClipStart);
        AnimancerState cameraState = PlayClip(_cameraAnimancer, pair.CameraClip, key, cameraStartTime, restartIfSame, animancerFadeDuration, delayClipStart);

        if (_hasActivePair == false || resolvedCrossFadeDuration <= 0f)
        {
            CompleteImmediate(key, pair, weaponState, handsState, cameraState);
            return;
        }

        _fadingPair = pair;
        _fadingKey = key;
        _fadingWeaponState = weaponState;
        _fadingHandsState = handsState;
        _fadingCameraState = cameraState;
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
        DestroyGraph(_cameraAnimancer);

        _hasActivePair = false;
        _isFading = false;
        _activeWeaponState = null;
        _activeHandsState = null;
        _activeCameraState = null;
        _fadingWeaponState = null;
        _fadingHandsState = null;
        _fadingCameraState = null;
    }

    private void EnsureAnimancers()
    {
        _weaponAnimancer ??= GetOrCreateAnimancer(_weaponAnimator);
        _handsAnimancer ??= GetOrCreateAnimancer(_handsAnimator);
        _cameraAnimancer ??= GetOrCreateAnimancer(_cameraAnimator);
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

    private double GetStartTime(FirstPersonWeaponAnimationKey nextKey, AnimationClip nextClip, FirstPersonWeaponAnimationChannel channel)
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
        AnimationClip sourceClip = GetClip(sourcePair, channel);
        AnimancerState sourceState = GetDominantState(channel);

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

    private AnimancerState GetDominantCameraState()
    {
        if (_isFading == false)
        {
            return _activeCameraState;
        }

        return GetFadeWeight() > 0.5f ? _fadingCameraState : _activeCameraState;
    }

    private AnimancerState GetDominantState(FirstPersonWeaponAnimationChannel channel)
    {
        return channel switch
        {
            FirstPersonWeaponAnimationChannel.Hands => GetDominantHandsState(),
            FirstPersonWeaponAnimationChannel.Camera => GetDominantCameraState(),
            _ => GetDominantWeaponState()
        };
    }

    private float GetFadeWeight() => _fadeDuration <= 0f ? 1f : Mathf.Clamp01(_fadeElapsed / _fadeDuration);

    private void CompleteImmediate(FirstPersonWeaponAnimationKey key, FirstPersonWeaponAnimationClipPair pair, AnimancerState weaponState, AnimancerState handsState, AnimancerState cameraState)
    {
        _activePair = pair;
        _activeKey = key;
        _activeWeaponState = weaponState;
        _activeHandsState = handsState;
        _activeCameraState = cameraState;
        _currentPair = pair;
        _currentKey = key;
        _hasActivePair = true;
        _isFading = false;
        _fadingWeaponState = null;
        _fadingHandsState = null;
        _fadingCameraState = null;
        SetStateSpeed(_activeWeaponState, GetPlaybackSpeed(key));
        SetStateSpeed(_activeHandsState, GetPlaybackSpeed(key));
        SetStateSpeed(_activeCameraState, GetPlaybackSpeed(key));
    }

    private void CompleteFade()
    {
        _activePair = _fadingPair;
        _activeKey = _fadingKey;
        _activeWeaponState = _fadingWeaponState;
        _activeHandsState = _fadingHandsState;
        _activeCameraState = _fadingCameraState;
        _currentPair = _activePair;
        _currentKey = _activeKey;
        _hasActivePair = true;
        _isFading = false;
        _fadingWeaponState = null;
        _fadingHandsState = null;
        _fadingCameraState = null;
        SetStateSpeed(_activeWeaponState, GetPlaybackSpeed(_activeKey));
        SetStateSpeed(_activeHandsState, GetPlaybackSpeed(_activeKey));
        SetStateSpeed(_activeCameraState, GetPlaybackSpeed(_activeKey));
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
            SetStateSpeed(_activeCameraState, _loopPlaybackSpeed);
        }

        if (_isFading && _fadingKey == _currentKey)
        {
            SetStateSpeed(_fadingWeaponState, _loopPlaybackSpeed);
            SetStateSpeed(_fadingHandsState, _loopPlaybackSpeed);
            SetStateSpeed(_fadingCameraState, _loopPlaybackSpeed);
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
               key == FirstPersonWeaponAnimationKey.AimWalk ||
               key == FirstPersonWeaponAnimationKey.AimWalkBackward ||
               key == FirstPersonWeaponAnimationKey.AimWalkLeft ||
               key == FirstPersonWeaponAnimationKey.AimWalkRight;
    }

    private static AnimationClip GetClip(FirstPersonWeaponAnimationClipPair pair, FirstPersonWeaponAnimationChannel channel)
    {
        return channel switch
        {
            FirstPersonWeaponAnimationChannel.Hands => pair.HandsClip,
            FirstPersonWeaponAnimationChannel.Camera => pair.CameraClip,
            _ => pair.WeaponClip
        };
    }

    private enum FirstPersonWeaponAnimationChannel
    {
        Weapon = 0,
        Hands = 1,
        Camera = 2
    }
}
