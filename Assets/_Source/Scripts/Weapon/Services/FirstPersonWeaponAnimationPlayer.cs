using System;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;

internal sealed class FirstPersonWeaponAnimationPlayer : IDisposable
{
    private const int INPUT_COUNT = 2;

    private readonly Animator _weaponAnimator;
    private readonly Animator _handsAnimator;
    private readonly float _defaultCrossFadeDuration;
    private readonly AnimationClipPlayable[] _weaponClipPlayables = new AnimationClipPlayable[INPUT_COUNT];
    private readonly AnimationClipPlayable[] _handsClipPlayables = new AnimationClipPlayable[INPUT_COUNT];
    private readonly FirstPersonWeaponAnimationClipPair[] _inputPairs = new FirstPersonWeaponAnimationClipPair[INPUT_COUNT];
    private readonly FirstPersonWeaponAnimationKey[] _inputKeys = new FirstPersonWeaponAnimationKey[INPUT_COUNT];

    private PlayableGraph _graph;
    private AnimationMixerPlayable _weaponMixer;
    private AnimationMixerPlayable _handsMixer;
    private FirstPersonWeaponAnimationClipPair _currentPair;
    private FirstPersonWeaponAnimationKey _currentKey;
    private int _activeInput;
    private int _fadingInput;
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

        EnsureGraph();

        float resolvedCrossFadeDuration = Mathf.Max(0f, crossFadeDuration);
        int nextInput = _hasActivePair ? 1 - _activeInput : _activeInput;
        SetPair(nextInput, key, pair);

        if (_hasActivePair == false || resolvedCrossFadeDuration <= 0f)
        {
            CompleteImmediate(nextInput, key, pair);
            return;
        }

        _fadingInput = nextInput;
        _fadeElapsed = 0f;
        _fadeDuration = resolvedCrossFadeDuration;
        _currentPair = pair;
        _currentKey = key;
        _isFading = true;
        SetInputWeight(_fadingInput, 0f);

        if (ShouldDelayClipStart(key))
        {
            SetInputSpeed(_fadingInput, 0d);
        }
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

    private void SetPair(int inputIndex, FirstPersonWeaponAnimationKey key, FirstPersonWeaponAnimationClipPair pair)
    {
        double weaponStartTime = GetStartTime(key, pair.WeaponClip, false);
        double handsStartTime = GetStartTime(key, pair.HandsClip, true);

        DisconnectInput(inputIndex);
        ConnectClip(_weaponMixer, _weaponClipPlayables, inputIndex, pair.WeaponClip, weaponStartTime);
        ConnectClip(_handsMixer, _handsClipPlayables, inputIndex, pair.HandsClip, handsStartTime);
        _inputPairs[inputIndex] = pair;
        _inputKeys[inputIndex] = key;
    }

    private void ConnectClip(AnimationMixerPlayable mixer, AnimationClipPlayable[] playables, int inputIndex, AnimationClip clip, double startTime)
    {
        if (mixer.IsValid() == false || clip == null)
        {
            return;
        }

        AnimationClipPlayable playable = AnimationClipPlayable.Create(_graph, clip);
        playable.SetTime(startTime);
        playable.SetSpeed(1d);
        playable.SetApplyFootIK(false);

        playables[inputIndex] = playable;
        _graph.Connect(playable, 0, mixer, inputIndex);
    }

    private double GetStartTime(FirstPersonWeaponAnimationKey nextKey, AnimationClip nextClip, bool hands)
    {
        if (_hasActivePair == false)
        {
            return 0d;
        }

        int sourceInput = GetDominantInput();
        FirstPersonWeaponAnimationKey sourceKey = _inputKeys[sourceInput];

        if (CanSynchronizePhase(sourceKey, nextKey) == false)
        {
            return 0d;
        }

        AnimationClip sourceClip = hands ? _inputPairs[sourceInput].HandsClip : _inputPairs[sourceInput].WeaponClip;
        AnimationClipPlayable sourcePlayable = hands ? _handsClipPlayables[sourceInput] : _weaponClipPlayables[sourceInput];

        if (sourceClip == null || nextClip == null || sourceClip.length <= 0f || nextClip.length <= 0f || sourcePlayable.IsValid() == false)
        {
            return 0d;
        }

        double normalizedTime = sourcePlayable.GetTime() / sourceClip.length;
        normalizedTime -= Math.Floor(normalizedTime);

        return normalizedTime * nextClip.length;
    }

    private int GetDominantInput()
    {
        if (_isFading == false)
        {
            return _activeInput;
        }

        float fadingWeight = GetInputWeight(_fadingInput);
        float activeWeight = GetInputWeight(_activeInput);
        return fadingWeight > activeWeight ? _fadingInput : _activeInput;
    }

    private void CompleteImmediate(int inputIndex, FirstPersonWeaponAnimationKey key, FirstPersonWeaponAnimationClipPair pair)
    {
        if (_hasActivePair && inputIndex != _activeInput)
        {
            DisconnectInput(_activeInput);
        }

        _activeInput = inputIndex;
        _currentPair = pair;
        _currentKey = key;
        _hasActivePair = true;
        _isFading = false;
        SetInputSpeed(_activeInput, GetPlaybackSpeed(key));

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
        _currentKey = _inputKeys[_activeInput];
        _hasActivePair = true;
        _isFading = false;
        SetInputSpeed(_activeInput, GetPlaybackSpeed(_currentKey));

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

    private float GetInputWeight(int inputIndex)
    {
        if (_handsMixer.IsValid())
        {
            return _handsMixer.GetInputWeight(inputIndex);
        }

        return _weaponMixer.IsValid() ? _weaponMixer.GetInputWeight(inputIndex) : 0f;
    }

    private void ApplyLoopPlaybackSpeed()
    {
        if (_hasActivePair == false || IsLoopLike(_currentKey) == false)
        {
            return;
        }

        ApplyLoopPlaybackSpeed(_activeInput);

        if (_isFading)
        {
            ApplyLoopPlaybackSpeed(_fadingInput);
        }
    }

    private void ApplyLoopPlaybackSpeed(int inputIndex)
    {
        if (_inputKeys[inputIndex] == _currentKey)
        {
            SetInputSpeed(inputIndex, _loopPlaybackSpeed);
        }
    }

    private void SetInputSpeed(int inputIndex, double speed)
    {
        if (_weaponClipPlayables[inputIndex].IsValid())
        {
            _weaponClipPlayables[inputIndex].SetSpeed(speed);
        }

        if (_handsClipPlayables[inputIndex].IsValid())
        {
            _handsClipPlayables[inputIndex].SetSpeed(speed);
        }
    }

    private void DisconnectInput(int inputIndex)
    {
        DisconnectClip(_weaponMixer, _weaponClipPlayables, inputIndex);
        DisconnectClip(_handsMixer, _handsClipPlayables, inputIndex);
        _inputPairs[inputIndex] = default;
        _inputKeys[inputIndex] = default;
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

    private static bool CanSynchronizePhase(FirstPersonWeaponAnimationKey sourceKey, FirstPersonWeaponAnimationKey targetKey)
    {
        return IsLoopLike(sourceKey) && IsLoopLike(targetKey);
    }

    private static bool ShouldDelayClipStart(FirstPersonWeaponAnimationKey key) => IsLoopLike(key) == false;

    private double GetPlaybackSpeed(FirstPersonWeaponAnimationKey key) => IsLoopLike(key) ? _loopPlaybackSpeed : 1d;

    private static bool IsLoopLike(FirstPersonWeaponAnimationKey key)
    {
        return key == FirstPersonWeaponAnimationKey.Idle ||
               key == FirstPersonWeaponAnimationKey.Walk ||
               key == FirstPersonWeaponAnimationKey.Sprint;
    }
}
