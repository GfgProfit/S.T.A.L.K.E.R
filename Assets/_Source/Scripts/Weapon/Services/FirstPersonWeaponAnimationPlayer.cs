using System;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;

internal sealed class FirstPersonWeaponAnimationPlayer : IDisposable
{
    private const int INPUT_COUNT = 2;

    private readonly Animator _weaponAnimator;
    private readonly Animator _handsAnimator;
    private readonly float _crossFadeDuration;
    private readonly AnimationClipPlayable[] _weaponClipPlayables = new AnimationClipPlayable[INPUT_COUNT];
    private readonly AnimationClipPlayable[] _handsClipPlayables = new AnimationClipPlayable[INPUT_COUNT];
    private readonly FirstPersonWeaponAnimationClipPair[] _inputPairs = new FirstPersonWeaponAnimationClipPair[INPUT_COUNT];

    private PlayableGraph _graph;
    private AnimationMixerPlayable _weaponMixer;
    private AnimationMixerPlayable _handsMixer;
    private FirstPersonWeaponAnimationClipPair _currentPair;
    private int _activeInput;
    private int _fadingInput;
    private float _fadeElapsed;
    private float _fadeDuration;
    private bool _hasActivePair;
    private bool _isFading;

    public FirstPersonWeaponAnimationPlayer(Animator weaponAnimator, Animator handsAnimator, float crossFadeDuration)
    {
        _weaponAnimator = weaponAnimator;
        _handsAnimator = handsAnimator;
        _crossFadeDuration = Mathf.Max(0f, crossFadeDuration);
    }

    public void Play(FirstPersonWeaponAnimationClipPair pair, bool restartIfSame)
    {
        if ((_weaponAnimator == null && _handsAnimator == null) || pair.HasAnyClip == false)
        {
            return;
        }

        if (_hasActivePair && restartIfSame == false && _currentPair.IsSameAs(pair))
        {
            return;
        }

        EnsureGraph();

        int nextInput = _hasActivePair ? 1 - _activeInput : _activeInput;
        SetPair(nextInput, pair);

        if (_hasActivePair == false || _crossFadeDuration <= 0f)
        {
            CompleteImmediate(nextInput, pair);
            return;
        }

        _fadingInput = nextInput;
        _fadeElapsed = 0f;
        _fadeDuration = _crossFadeDuration;
        _currentPair = pair;
        _isFading = true;
        SetInputWeight(_fadingInput, 0f);
    }

    public void Tick(float deltaTime)
    {
        if (_isFading == false)
        {
            return;
        }

        _fadeElapsed += Mathf.Max(0f, deltaTime);
        float weight = _fadeDuration <= 0f ? 1f : Mathf.Clamp01(_fadeElapsed / _fadeDuration);

        SetInputWeight(_activeInput, 1f - weight);
        SetInputWeight(_fadingInput, weight);

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

        _hasActivePair = false;
        _isFading = false;
    }

    private void EnsureGraph()
    {
        if (_graph.IsValid())
        {
            return;
        }

        _graph = PlayableGraph.Create("First Person Weapon");
        _graph.SetTimeUpdateMode(DirectorUpdateMode.GameTime);

        if (_weaponAnimator != null)
        {
            _weaponMixer = AnimationMixerPlayable.Create(_graph, INPUT_COUNT);
            AnimationPlayableOutput weaponOutput = AnimationPlayableOutput.Create(_graph, "Weapon", _weaponAnimator);
            weaponOutput.SetSourcePlayable(_weaponMixer);
        }

        if (_handsAnimator != null)
        {
            _handsMixer = AnimationMixerPlayable.Create(_graph, INPUT_COUNT);
            AnimationPlayableOutput handsOutput = AnimationPlayableOutput.Create(_graph, "Hands", _handsAnimator);
            handsOutput.SetSourcePlayable(_handsMixer);
        }

        _graph.Play();
    }

    private void SetPair(int inputIndex, FirstPersonWeaponAnimationClipPair pair)
    {
        DisconnectInput(inputIndex);
        ConnectClip(_weaponMixer, _weaponClipPlayables, inputIndex, pair.WeaponClip);
        ConnectClip(_handsMixer, _handsClipPlayables, inputIndex, pair.HandsClip);
        _inputPairs[inputIndex] = pair;
    }

    private void ConnectClip(AnimationMixerPlayable mixer, AnimationClipPlayable[] playables, int inputIndex, AnimationClip clip)
    {
        if (mixer.IsValid() == false || clip == null)
        {
            return;
        }

        AnimationClipPlayable playable = AnimationClipPlayable.Create(_graph, clip);
        playable.SetTime(0d);
        playable.SetSpeed(1d);
        playable.SetApplyFootIK(false);

        playables[inputIndex] = playable;
        _graph.Connect(playable, 0, mixer, inputIndex);
    }

    private void CompleteImmediate(int inputIndex, FirstPersonWeaponAnimationClipPair pair)
    {
        if (_hasActivePair && inputIndex != _activeInput)
        {
            DisconnectInput(_activeInput);
        }

        _activeInput = inputIndex;
        _currentPair = pair;
        _hasActivePair = true;
        _isFading = false;

        for (int i = 0; i < INPUT_COUNT; i++)
        {
            SetInputWeight(i, i == _activeInput ? 1f : 0f);
        }
    }

    private void CompleteFade()
    {
        DisconnectInput(_activeInput);
        _activeInput = _fadingInput;
        _currentPair = _inputPairs[_activeInput];
        _hasActivePair = true;
        _isFading = false;

        for (int i = 0; i < INPUT_COUNT; i++)
        {
            SetInputWeight(i, i == _activeInput ? 1f : 0f);
        }
    }

    private void SetInputWeight(int inputIndex, float weight)
    {
        if (_weaponMixer.IsValid())
        {
            _weaponMixer.SetInputWeight(inputIndex, weight);
        }

        if (_handsMixer.IsValid())
        {
            _handsMixer.SetInputWeight(inputIndex, weight);
        }
    }

    private void DisconnectInput(int inputIndex)
    {
        DisconnectClip(_weaponMixer, _weaponClipPlayables, inputIndex);
        DisconnectClip(_handsMixer, _handsClipPlayables, inputIndex);
        _inputPairs[inputIndex] = default;
    }

    private void DisconnectClip(AnimationMixerPlayable mixer, AnimationClipPlayable[] playables, int inputIndex)
    {
        if (mixer.IsValid())
        {
            mixer.DisconnectInput(inputIndex);
            mixer.SetInputWeight(inputIndex, 0f);
        }

        if (playables[inputIndex].IsValid())
        {
            playables[inputIndex].Destroy();
        }

        playables[inputIndex] = default;
    }
}
