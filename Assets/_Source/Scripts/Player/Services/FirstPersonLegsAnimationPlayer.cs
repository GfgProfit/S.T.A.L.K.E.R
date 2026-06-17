using System;
using Animancer;
using UnityEngine;

internal sealed class FirstPersonLegsAnimationPlayer : IDisposable
{
    private readonly Animator _animator;
    private readonly FirstPersonLegsAnimationClipSet _clips;
    private readonly float _crossFadeDuration;

    private AnimancerComponent _animancer;
    private AnimancerState _activeState;
    private AnimancerState _fadingState;
    private AnimationClip _activeClip;
    private AnimationClip _fadingClip;
    private FirstPersonLegsAnimationKey _activeKey;
    private FirstPersonLegsAnimationKey _fadingKey;
    private FirstPersonLegsAnimationKey _currentKey;
    private float _fadeElapsed;
    private float _fadeDuration;
    private bool _hasActiveClip;
    private bool _isFading;

    public FirstPersonLegsAnimationPlayer(Animator animator, FirstPersonLegsAnimationClipSet clips, float crossFadeDuration)
    {
        _animator = animator;
        _clips = clips;
        _crossFadeDuration = Mathf.Max(0f, crossFadeDuration);
    }

    public void Play(FirstPersonLegsAnimationKey key)
    {
        if (_animator == null || _clips == null)
        {
            return;
        }

        AnimationClip clip = _clips.GetClip(key);
        if (clip == null)
        {
            return;
        }

        if (_hasActiveClip && _currentKey == key)
        {
            return;
        }

        double startTime = GetStartTime(key, clip);
        EnsureAnimancer();
        float fadeDuration = _hasActiveClip ? _crossFadeDuration : 0f;
        AnimancerState state = PlayClip(clip, startTime, fadeDuration);

        if (_hasActiveClip == false || _crossFadeDuration <= 0f)
        {
            CompleteImmediate(key, clip, state);
            return;
        }

        _fadingKey = key;
        _fadingClip = clip;
        _fadingState = state;
        _fadeElapsed = 0f;
        _fadeDuration = _crossFadeDuration;
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
        if (_animancer != null && _animancer.IsGraphInitialized)
        {
            _animancer.Graph.Destroy();
        }

        _hasActiveClip = false;
        _isFading = false;
        _activeState = null;
        _fadingState = null;
        _activeClip = null;
        _fadingClip = null;
    }

    private void EnsureAnimancer()
    {
        if (_animancer != null)
        {
            return;
        }

        if (_animator.TryGetComponent(out _animancer) == false)
        {
            _animancer = _animator.gameObject.AddComponent<AnimancerComponent>();
        }

        _animancer.Animator = _animator;
    }

    private AnimancerState PlayClip(AnimationClip clip, double startTime, float fadeDuration)
    {
        AnimancerState state = fadeDuration <= 0f
            ? _animancer.Play(clip)
            : _animancer.Play(clip, fadeDuration, FadeMode.FixedDuration);

        state.ApplyFootIK = false;
        state.TimeD = startTime;
        state.Speed = 1f;
        return state;
    }

    private double GetStartTime(FirstPersonLegsAnimationKey nextKey, AnimationClip nextClip)
    {
        if (_hasActiveClip == false)
        {
            return 0d;
        }

        FirstPersonLegsAnimationKey sourceKey = GetDominantKey();

        if (CanSynchronizePhase(sourceKey, nextKey) == false)
        {
            return 0d;
        }

        AnimationClip sourceClip = GetDominantClip();
        AnimancerState sourceState = GetDominantState();

        if (sourceClip == null || nextClip == null || sourceClip.length <= 0f || nextClip.length <= 0f || sourceState == null)
        {
            return 0d;
        }

        double normalizedTime = sourceState.TimeD / sourceClip.length;
        normalizedTime -= Math.Floor(normalizedTime);

        return normalizedTime * nextClip.length;
    }

    private FirstPersonLegsAnimationKey GetDominantKey()
    {
        if (_isFading == false)
        {
            return _activeKey;
        }

        return GetFadeWeight() > 0.5f ? _fadingKey : _activeKey;
    }

    private AnimationClip GetDominantClip()
    {
        if (_isFading == false)
        {
            return _activeClip;
        }

        return GetFadeWeight() > 0.5f ? _fadingClip : _activeClip;
    }

    private AnimancerState GetDominantState()
    {
        if (_isFading == false)
        {
            return _activeState;
        }

        return GetFadeWeight() > 0.5f ? _fadingState : _activeState;
    }

    private float GetFadeWeight() => _fadeDuration <= 0f ? 1f : Mathf.Clamp01(_fadeElapsed / _fadeDuration);

    private void CompleteImmediate(FirstPersonLegsAnimationKey key, AnimationClip clip, AnimancerState state)
    {
        _activeKey = key;
        _activeClip = clip;
        _activeState = state;
        _currentKey = key;
        _hasActiveClip = true;
        _isFading = false;
        _fadingClip = null;
        _fadingState = null;
    }

    private void CompleteFade()
    {
        _activeKey = _fadingKey;
        _activeClip = _fadingClip;
        _activeState = _fadingState;
        _currentKey = _activeKey;
        _hasActiveClip = true;
        _isFading = false;
        _fadingClip = null;
        _fadingState = null;
    }

    private static bool CanSynchronizePhase(FirstPersonLegsAnimationKey sourceKey, FirstPersonLegsAnimationKey targetKey)
    {
        return IsGroundedMove(sourceKey) && IsGroundedMove(targetKey);
    }

    private static bool IsGroundedMove(FirstPersonLegsAnimationKey key)
    {
        return key >= FirstPersonLegsAnimationKey.WalkForward && key <= FirstPersonLegsAnimationKey.SprintForwardRight;
    }
}
