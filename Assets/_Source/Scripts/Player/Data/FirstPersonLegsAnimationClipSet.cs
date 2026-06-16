using System;
using UnityEngine;

[Serializable]
public sealed class FirstPersonLegsAnimationClipSet
{
    [SerializeField] private AnimationClip _standIdle;
    [SerializeField] private AnimationClip _crouchIdle;
    [SerializeField] private AnimationClip _turnLeft;
    [SerializeField] private AnimationClip _turnRight;

    [Header("Walk")]
    [SerializeField] private AnimationClip _walkForward;
    [SerializeField] private AnimationClip _walkForwardLeft;
    [SerializeField] private AnimationClip _walkForwardRight;
    [SerializeField] private AnimationClip _walkBack;
    [SerializeField] private AnimationClip _walkBackLeft;
    [SerializeField] private AnimationClip _walkBackRight;
    [SerializeField] private AnimationClip _walkLeft;
    [SerializeField] private AnimationClip _walkRight;

    [Header("Crouch")]
    [SerializeField] private AnimationClip _crouchForward;
    [SerializeField] private AnimationClip _crouchForwardLeft;
    [SerializeField] private AnimationClip _crouchForwardRight;
    [SerializeField] private AnimationClip _crouchBack;
    [SerializeField] private AnimationClip _crouchBackLeft;
    [SerializeField] private AnimationClip _crouchBackRight;
    [SerializeField] private AnimationClip _crouchLeft;
    [SerializeField] private AnimationClip _crouchRight;

    [Header("Sprint")]
    [SerializeField] private AnimationClip _sprintForward;
    [SerializeField] private AnimationClip _sprintForwardLeft;
    [SerializeField] private AnimationClip _sprintForwardRight;

    [Header("Jump")]
    [SerializeField] private AnimationClip _jumpStart;
    [SerializeField] private AnimationClip _jumpLoop;
    [SerializeField] private AnimationClip _jumpEnd;

    public AnimationClip GetClip(FirstPersonLegsAnimationKey key)
    {
        return key switch
        {
            FirstPersonLegsAnimationKey.StandIdle => _standIdle,
            FirstPersonLegsAnimationKey.CrouchIdle => _crouchIdle,
            FirstPersonLegsAnimationKey.TurnLeft => _turnLeft,
            FirstPersonLegsAnimationKey.TurnRight => _turnRight,
            FirstPersonLegsAnimationKey.WalkForward => _walkForward,
            FirstPersonLegsAnimationKey.WalkForwardLeft => _walkForwardLeft,
            FirstPersonLegsAnimationKey.WalkForwardRight => _walkForwardRight,
            FirstPersonLegsAnimationKey.WalkBack => _walkBack,
            FirstPersonLegsAnimationKey.WalkBackLeft => _walkBackLeft,
            FirstPersonLegsAnimationKey.WalkBackRight => _walkBackRight,
            FirstPersonLegsAnimationKey.WalkLeft => _walkLeft,
            FirstPersonLegsAnimationKey.WalkRight => _walkRight,
            FirstPersonLegsAnimationKey.CrouchForward => _crouchForward,
            FirstPersonLegsAnimationKey.CrouchForwardLeft => _crouchForwardLeft,
            FirstPersonLegsAnimationKey.CrouchForwardRight => _crouchForwardRight,
            FirstPersonLegsAnimationKey.CrouchBack => _crouchBack,
            FirstPersonLegsAnimationKey.CrouchBackLeft => _crouchBackLeft,
            FirstPersonLegsAnimationKey.CrouchBackRight => _crouchBackRight,
            FirstPersonLegsAnimationKey.CrouchLeft => _crouchLeft,
            FirstPersonLegsAnimationKey.CrouchRight => _crouchRight,
            FirstPersonLegsAnimationKey.SprintForward => _sprintForward,
            FirstPersonLegsAnimationKey.SprintForwardLeft => _sprintForwardLeft,
            FirstPersonLegsAnimationKey.SprintForwardRight => _sprintForwardRight,
            FirstPersonLegsAnimationKey.JumpStart => _jumpStart,
            FirstPersonLegsAnimationKey.JumpLoop => _jumpLoop,
            FirstPersonLegsAnimationKey.JumpEnd => _jumpEnd,
            _ => _standIdle
        };
    }

    public float GetLength(FirstPersonLegsAnimationKey key)
    {
        AnimationClip clip = GetClip(key);
        return clip == null ? 0f : Mathf.Max(0f, clip.length);
    }
}
