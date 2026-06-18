using System;
using UnityEngine;

[Serializable]
public sealed class FirstPersonCameraAllAnimationClipSet
{
    [SerializeField] private AnimationClip _cameraAllWalkForward;
    [SerializeField] private AnimationClip _cameraAllWalkBackward;
    [SerializeField] private AnimationClip _cameraAllWalkLeft;
    [SerializeField] private AnimationClip _cameraAllWalkRight;
    [SerializeField] private AnimationClip _cameraAllWalkAimLeft;
    [SerializeField] private AnimationClip _cameraAllWalkAimRight;
    [SerializeField] private AnimationClip _cameraAllSprint;
    [SerializeField] private AnimationClip _cameraAllJump;
    [SerializeField] private AnimationClip _cameraAllLanding;
    [SerializeField] private AnimationClip _cameraAllCrouchDown;
    [SerializeField] private AnimationClip _cameraAllCrouchUp;

    public AnimationClip GetClip(FirstPersonCameraAllAnimationKey key)
    {
        return key switch
        {
            FirstPersonCameraAllAnimationKey.WalkForward => _cameraAllWalkForward,
            FirstPersonCameraAllAnimationKey.WalkBackward => _cameraAllWalkBackward,
            FirstPersonCameraAllAnimationKey.WalkLeft => _cameraAllWalkLeft,
            FirstPersonCameraAllAnimationKey.WalkRight => _cameraAllWalkRight,
            FirstPersonCameraAllAnimationKey.WalkAimLeft => _cameraAllWalkAimLeft,
            FirstPersonCameraAllAnimationKey.WalkAimRight => _cameraAllWalkAimRight,
            FirstPersonCameraAllAnimationKey.Sprint => _cameraAllSprint,
            FirstPersonCameraAllAnimationKey.Jump => _cameraAllJump,
            FirstPersonCameraAllAnimationKey.Landing => _cameraAllLanding,
            FirstPersonCameraAllAnimationKey.CrouchDown => _cameraAllCrouchDown,
            FirstPersonCameraAllAnimationKey.CrouchUp => _cameraAllCrouchUp,
            _ => null
        };
    }

    public float GetLength(FirstPersonCameraAllAnimationKey key)
    {
        AnimationClip clip = GetClip(key);
        return clip == null ? 0f : Mathf.Max(0f, clip.length);
    }
}
