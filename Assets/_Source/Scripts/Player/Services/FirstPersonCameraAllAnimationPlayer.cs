using System;
using Animancer;
using UnityEngine;

internal sealed class FirstPersonCameraAllAnimationPlayer : IDisposable
{
    private readonly Animator _animator;
    private readonly float _crossFadeDuration;

    private AnimancerComponent _animancer;
    private AnimancerState _activeState;
    private AnimationClip _activeClip;
    private FirstPersonCameraAllAnimationKey _activeKey;
    private bool _hasActiveState;

    public FirstPersonCameraAllAnimationPlayer(Animator animator, float crossFadeDuration)
    {
        _animator = animator;
        _crossFadeDuration = Mathf.Max(0f, crossFadeDuration);
    }

    public void Play(FirstPersonCameraAllAnimationKey key, AnimationClip clip)
    {
        if (_animator == null || clip == null)
        {
            return;
        }

        EnsureAnimancer();

        if (_hasActiveState && _activeKey == key && _activeClip == clip)
        {
            return;
        }

        _activeState = _hasActiveState && _crossFadeDuration > 0f ? _animancer.Play(clip, _crossFadeDuration, FadeMode.FixedDuration) : _animancer.Play(clip);
        _activeState.ApplyFootIK = false;
        _activeClip = clip;
        _activeKey = key;
        _hasActiveState = true;
    }

    public void Stop()
    {
        if (_animancer != null)
        {
            _animancer.Stop();
        }

        _activeState = null;
        _activeClip = null;
        _hasActiveState = false;
        _activeKey = FirstPersonCameraAllAnimationKey.None;
    }

    public void Dispose()
    {
        if (_animancer != null && _animancer.IsGraphInitialized)
        {
            _animancer.Graph.Destroy();
        }

        _activeState = null;
        _activeClip = null;
        _hasActiveState = false;
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
}
