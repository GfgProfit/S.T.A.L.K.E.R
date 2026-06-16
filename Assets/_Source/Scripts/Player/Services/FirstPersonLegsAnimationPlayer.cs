using System;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;

internal sealed class FirstPersonLegsAnimationPlayer : IDisposable
{
    private const int INPUT_COUNT = 2;

    private readonly Animator _animator;
    private readonly FirstPersonLegsAnimationClipSet _clips;
    private readonly float _crossFadeDuration;
    private readonly AnimationClipPlayable[] _clipPlayables = new AnimationClipPlayable[INPUT_COUNT];

    private PlayableGraph _graph;
    private AnimationMixerPlayable _mixer;
    private FirstPersonLegsAnimationKey _currentKey;
    private int _activeInput;
    private int _fadingInput;
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

        EnsureGraph();
        int nextInput = _hasActiveClip ? 1 - _activeInput : _activeInput;
        SetClip(nextInput, clip);

        if (_hasActiveClip == false || _crossFadeDuration <= 0f)
        {
            CompleteImmediate(nextInput, key);
            return;
        }

        _fadingInput = nextInput;
        _fadeElapsed = 0f;
        _fadeDuration = _crossFadeDuration;
        _currentKey = key;
        _isFading = true;
        _mixer.SetInputWeight(_fadingInput, 0f);
    }

    public void Tick(float deltaTime)
    {
        if (_isFading == false || _mixer.IsValid() == false)
        {
            return;
        }

        _fadeElapsed += Mathf.Max(0f, deltaTime);
        float weight = _fadeDuration <= 0f ? 1f : Mathf.Clamp01(_fadeElapsed / _fadeDuration);

        _mixer.SetInputWeight(_activeInput, 1f - weight);
        _mixer.SetInputWeight(_fadingInput, weight);

        if (weight >= 1f)
        {
            CompleteFade();
        }
    }

    public void Dispose()
    {
        if (_graph.IsValid())
        {
            _graph.Destroy();
        }

        _hasActiveClip = false;
        _isFading = false;
    }

    private void EnsureGraph()
    {
        if (_graph.IsValid())
        {
            return;
        }

        _graph = PlayableGraph.Create($"{_animator.name} First Person Legs");
        _graph.SetTimeUpdateMode(DirectorUpdateMode.GameTime);

        _mixer = AnimationMixerPlayable.Create(_graph, INPUT_COUNT);
        AnimationPlayableOutput output = AnimationPlayableOutput.Create(_graph, "First Person Legs", _animator);
        output.SetSourcePlayable(_mixer);

        _graph.Play();
    }

    private void SetClip(int inputIndex, AnimationClip clip)
    {
        DisconnectInput(inputIndex);

        AnimationClipPlayable playable = AnimationClipPlayable.Create(_graph, clip);
        playable.SetTime(0d);
        playable.SetSpeed(1d);
        playable.SetApplyFootIK(false);

        _clipPlayables[inputIndex] = playable;
        _graph.Connect(playable, 0, _mixer, inputIndex);
    }

    private void CompleteImmediate(int inputIndex, FirstPersonLegsAnimationKey key)
    {
        if (_hasActiveClip && inputIndex != _activeInput)
        {
            DisconnectInput(_activeInput);
        }

        _activeInput = inputIndex;
        _currentKey = key;
        _hasActiveClip = true;
        _isFading = false;

        for (int i = 0; i < INPUT_COUNT; i++)
        {
            _mixer.SetInputWeight(i, i == _activeInput ? 1f : 0f);
        }
    }

    private void CompleteFade()
    {
        DisconnectInput(_activeInput);
        _activeInput = _fadingInput;
        _hasActiveClip = true;
        _isFading = false;

        for (int i = 0; i < INPUT_COUNT; i++)
        {
            _mixer.SetInputWeight(i, i == _activeInput ? 1f : 0f);
        }
    }

    private void DisconnectInput(int inputIndex)
    {
        if (_mixer.IsValid())
        {
            _mixer.DisconnectInput(inputIndex);
            _mixer.SetInputWeight(inputIndex, 0f);
        }

        if (_clipPlayables[inputIndex].IsValid())
        {
            _clipPlayables[inputIndex].Destroy();
        }
    }
}