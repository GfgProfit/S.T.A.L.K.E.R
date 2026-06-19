using System;
using UnityEngine;

[Serializable]
public sealed class FirstPersonWeaponAnimationClipSet
{
    [Header("Weapon")]
    [SerializeField] private AnimationClip _weaponIdle;
    [SerializeField] private AnimationClip _weaponIdleEmpty;
    [SerializeField] private AnimationClip _weaponIdleJammed;
    [SerializeField] private AnimationClip _weaponReload;
    [SerializeField] private AnimationClip _weaponReloadFull;
    [SerializeField] private AnimationClip _weaponMisfire;
    [SerializeField] private AnimationClip _weaponRevival;
    [SerializeField] private AnimationClip _weaponRevivalLast;
    [SerializeField] private AnimationClip _weaponShoot;
    [SerializeField] private AnimationClip _weaponShootLast;
    [SerializeField] private AnimationClip _weaponAimShoot;
    [SerializeField] private AnimationClip _weaponAimShootLast;

    [Header("Camera Bone")]
    [SerializeField] private AnimationClip _cameraDraw;
    [SerializeField] private AnimationClip _cameraHeadflash;
    [SerializeField] private AnimationClip _cameraHide;
    [SerializeField] private AnimationClip _cameraNvgOff;
    [SerializeField] private AnimationClip _cameraNvgOn;
    [SerializeField] private AnimationClip _cameraReload;
    [SerializeField] private AnimationClip _cameraReloadFull;
    [SerializeField] private AnimationClip _cameraRevival;
    [SerializeField] private AnimationClip _cameraRevivalLast;
    [SerializeField] private AnimationClip _cameraDry;
    [SerializeField] private AnimationClip _cameraAimIn;
    [SerializeField] private AnimationClip _cameraAimOut;

    [Header("Hands")]
    [SerializeField] private AnimationClip _handsDraw;
    [SerializeField] private AnimationClip _handsHeadflash;
    [SerializeField] private AnimationClip _handsHide;
    [SerializeField] private AnimationClip _handsIdle;
    [SerializeField] private AnimationClip _handsNvgOff;
    [SerializeField] private AnimationClip _handsNvgOn;
    [SerializeField] private AnimationClip _handsReload;
    [SerializeField] private AnimationClip _handsReloadFull;
    [SerializeField] private AnimationClip _handsReloadMisfire;
    [SerializeField] private AnimationClip _handsRevival;
    [SerializeField] private AnimationClip _handsRevivalLast;
    [SerializeField] private AnimationClip _handsShoot;
    [SerializeField] private AnimationClip _handsDryEmpty;
    [SerializeField] private AnimationClip _handsSprint;
    [SerializeField] private AnimationClip _handsSprintEnd;
    [SerializeField] private AnimationClip _handsSprintStart;
    [SerializeField] private AnimationClip _handsWalk;
    [SerializeField] private AnimationClip _handsAimIn;
    [SerializeField] private AnimationClip _handsAimOut;
    [SerializeField] private AnimationClip _handsAimIdle;
    [SerializeField] private AnimationClip _handsAimShoot;
    [SerializeField] private AnimationClip _handsAimDry;
    [SerializeField] private AnimationClip _handsAimWalk;
    [SerializeField] private AnimationClip _handsAimWalkForward;
    [SerializeField] private AnimationClip _handsAimWalkBackward;
    [SerializeField] private AnimationClip _handsAimWalkLeft;
    [SerializeField] private AnimationClip _handsAimWalkRight;

    [Header("Hands Tactical Grip")]
    [SerializeField] private AnimationClip _handsTacDraw;
    [SerializeField] private AnimationClip _handsTacHeadflash;
    [SerializeField] private AnimationClip _handsTacHide;
    [SerializeField] private AnimationClip _handsTacIdle;
    [SerializeField] private AnimationClip _handsTacNvgOff;
    [SerializeField] private AnimationClip _handsTacNvgOn;
    [SerializeField] private AnimationClip _handsTacReload;
    [SerializeField] private AnimationClip _handsTacReloadFull;
    [SerializeField] private AnimationClip _handsTacReloadMisfire;
    [SerializeField] private AnimationClip _handsTacRevival;
    [SerializeField] private AnimationClip _handsTacRevivalLast;
    [SerializeField] private AnimationClip _handsTacShoot;
    [SerializeField] private AnimationClip _handsTacDryEmpty;
    [SerializeField] private AnimationClip _handsTacSprint;
    [SerializeField] private AnimationClip _handsTacSprintEnd;
    [SerializeField] private AnimationClip _handsTacSprintStart;
    [SerializeField] private AnimationClip _handsTacWalk;
    [SerializeField] private AnimationClip _handsTacAimIn;
    [SerializeField] private AnimationClip _handsTacAimOut;
    [SerializeField] private AnimationClip _handsTacAimIdle;
    [SerializeField] private AnimationClip _handsTacAimShoot;
    [SerializeField] private AnimationClip _handsTacAimDry;
    [SerializeField] private AnimationClip _handsTacAimWalk;
    [SerializeField] private AnimationClip _handsTacAimWalkForward;
    [SerializeField] private AnimationClip _handsTacAimWalkBackward;
    [SerializeField] private AnimationClip _handsTacAimWalkLeft;
    [SerializeField] private AnimationClip _handsTacAimWalkRight;

    public FirstPersonWeaponAnimationClipPair GetPair(FirstPersonWeaponAnimationKey key, WeaponCondition condition, bool useTacticalGripAnimations = false)
    {
        return key switch
        {
            FirstPersonWeaponAnimationKey.Reload => new FirstPersonWeaponAnimationClipPair(_weaponReload, GetHandsClip(_handsReload, _handsTacReload, useTacticalGripAnimations), cameraClip: _cameraReload),
            FirstPersonWeaponAnimationKey.ReloadFull => new FirstPersonWeaponAnimationClipPair(_weaponReloadFull, GetHandsClip(_handsReloadFull, _handsTacReloadFull, useTacticalGripAnimations), cameraClip: _cameraReloadFull),
            FirstPersonWeaponAnimationKey.Misfire => new FirstPersonWeaponAnimationClipPair(_weaponMisfire, GetHandsClip(_handsReloadMisfire, _handsTacReloadMisfire, useTacticalGripAnimations)),
            FirstPersonWeaponAnimationKey.Revival => new FirstPersonWeaponAnimationClipPair(_weaponRevival, GetHandsClip(_handsRevival, _handsTacRevival, useTacticalGripAnimations), cameraClip: _cameraRevival),
            FirstPersonWeaponAnimationKey.RevivalLast => new FirstPersonWeaponAnimationClipPair(_weaponRevivalLast, GetHandsClip(_handsRevivalLast, _handsTacRevivalLast, useTacticalGripAnimations), cameraClip: _cameraRevivalLast),
            FirstPersonWeaponAnimationKey.Shoot => new FirstPersonWeaponAnimationClipPair(_weaponShoot, GetHandsClip(_handsShoot, _handsTacShoot, useTacticalGripAnimations)),
            FirstPersonWeaponAnimationKey.ShootLast => new FirstPersonWeaponAnimationClipPair(_weaponShootLast, GetHandsClip(_handsShoot, _handsTacShoot, useTacticalGripAnimations)),
            FirstPersonWeaponAnimationKey.DryEmpty => CreateIdleWeaponPair(condition, GetHandsClip(_handsDryEmpty, _handsTacDryEmpty, useTacticalGripAnimations), _cameraDry),
            FirstPersonWeaponAnimationKey.Draw => CreateIdleWeaponPair(condition, GetHandsClip(_handsDraw, _handsTacDraw, useTacticalGripAnimations), _cameraDraw),
            FirstPersonWeaponAnimationKey.Headflash => CreateIdleWeaponPair(condition, GetHandsClip(_handsHeadflash, _handsTacHeadflash, useTacticalGripAnimations), _cameraHeadflash),
            FirstPersonWeaponAnimationKey.Hide => CreateIdleWeaponPair(condition, GetHandsClip(_handsHide, _handsTacHide, useTacticalGripAnimations), _cameraHide),
            FirstPersonWeaponAnimationKey.NvgOff => CreateIdleWeaponPair(condition, GetHandsClip(_handsNvgOff, _handsTacNvgOff, useTacticalGripAnimations), _cameraNvgOff),
            FirstPersonWeaponAnimationKey.NvgOn => CreateIdleWeaponPair(condition, GetHandsClip(_handsNvgOn, _handsTacNvgOn, useTacticalGripAnimations), _cameraNvgOn),
            FirstPersonWeaponAnimationKey.Sprint => CreateIdleWeaponPair(condition, GetHandsClip(_handsSprint, _handsTacSprint, useTacticalGripAnimations)),
            FirstPersonWeaponAnimationKey.SprintEnd => CreateIdleWeaponPair(condition, GetHandsClip(_handsSprintEnd, _handsTacSprintEnd, useTacticalGripAnimations)),
            FirstPersonWeaponAnimationKey.SprintStart => CreateIdleWeaponPair(condition, GetHandsClip(_handsSprintStart, _handsTacSprintStart, useTacticalGripAnimations)),
            FirstPersonWeaponAnimationKey.Walk => CreateIdleWeaponPair(condition, GetHandsClip(_handsWalk, _handsTacWalk, useTacticalGripAnimations)),
            FirstPersonWeaponAnimationKey.AimIn => CreateIdleWeaponPair(condition, GetHandsClip(_handsAimIn, _handsTacAimIn, useTacticalGripAnimations), _cameraAimIn),
            FirstPersonWeaponAnimationKey.AimOut => CreateIdleWeaponPair(condition, GetHandsClip(_handsAimOut, _handsTacAimOut, useTacticalGripAnimations), _cameraAimOut),
            FirstPersonWeaponAnimationKey.AimIdle => CreateIdleWeaponPair(condition, GetHandsClip(_handsAimIdle, _handsTacAimIdle, useTacticalGripAnimations)),
            FirstPersonWeaponAnimationKey.AimShoot => new FirstPersonWeaponAnimationClipPair(_weaponAimShoot, GetHandsClip(_handsAimShoot, _handsTacAimShoot, useTacticalGripAnimations)),
            FirstPersonWeaponAnimationKey.AimShootLast => new FirstPersonWeaponAnimationClipPair(_weaponAimShootLast, GetHandsClip(_handsAimShoot, _handsTacAimShoot, useTacticalGripAnimations)),
            FirstPersonWeaponAnimationKey.AimDry => CreateIdleWeaponPair(condition, GetHandsClip(_handsAimDry, _handsTacAimDry, useTacticalGripAnimations), _cameraDry),
            FirstPersonWeaponAnimationKey.AimWalk => CreateIdleWeaponPair(condition, GetAimWalkForwardClip(useTacticalGripAnimations)),
            FirstPersonWeaponAnimationKey.AimWalkBackward => CreateIdleWeaponPair(condition, GetHandsClip(_handsAimWalkBackward, _handsTacAimWalkBackward, useTacticalGripAnimations)),
            FirstPersonWeaponAnimationKey.AimWalkLeft => CreateIdleWeaponPair(condition, GetHandsClip(_handsAimWalkLeft, _handsTacAimWalkLeft, useTacticalGripAnimations)),
            FirstPersonWeaponAnimationKey.AimWalkRight => CreateIdleWeaponPair(condition, GetHandsClip(_handsAimWalkRight, _handsTacAimWalkRight, useTacticalGripAnimations)),
            _ => CreateIdleWeaponPair(condition, GetHandsClip(_handsIdle, _handsTacIdle, useTacticalGripAnimations))
        };
    }

    public float GetLength(FirstPersonWeaponAnimationKey key, WeaponCondition condition, bool useTacticalGripAnimations = false)
    {
        FirstPersonWeaponAnimationClipPair pair = GetPair(key, condition, useTacticalGripAnimations);

        if (pair.WeaponUsesIdleClip)
        {
            float idleHandsLength = pair.HandsClip == null ? 0f : pair.HandsClip.length;
            float idleCameraLength = pair.CameraClip == null ? 0f : pair.CameraClip.length;
            return Mathf.Max(idleHandsLength, idleCameraLength);
        }

        float weaponLength = pair.WeaponClip == null ? 0f : pair.WeaponClip.length;
        float handsLength = pair.HandsClip == null ? 0f : pair.HandsClip.length;
        float cameraLength = pair.CameraClip == null ? 0f : pair.CameraClip.length;
        return Mathf.Max(weaponLength, handsLength, cameraLength);
    }

    public float GetFrameTime(FirstPersonWeaponAnimationKey key, WeaponCondition condition, int frame, bool useTacticalGripAnimations = false)
    {
        if (frame <= 0)
        {
            return 0f;
        }

        FirstPersonWeaponAnimationClipPair pair = GetPair(key, condition, useTacticalGripAnimations);
        AnimationClip clip = pair.HandsClip != null ? pair.HandsClip : pair.WeaponClip != null ? pair.WeaponClip : pair.CameraClip;

        if (clip == null || clip.frameRate <= 0f)
        {
            return 0f;
        }

        return Mathf.Min(frame / clip.frameRate, GetLength(key, condition, useTacticalGripAnimations));
    }

    private AnimationClip GetIdleWeaponClip(WeaponCondition condition)
    {
        return condition switch
        {
            WeaponCondition.Empty => _weaponIdleEmpty != null ? _weaponIdleEmpty : _weaponIdle,
            WeaponCondition.Jammed => _weaponIdleJammed != null ? _weaponIdleJammed : _weaponIdle,
            _ => _weaponIdle
        };
    }

    private FirstPersonWeaponAnimationClipPair CreateIdleWeaponPair(WeaponCondition condition, AnimationClip handsClip, AnimationClip cameraClip = null) => new(GetIdleWeaponClip(condition), handsClip, true, cameraClip);

    private AnimationClip GetAimWalkForwardClip(bool useTacticalGripAnimations)
    {
        AnimationClip handsClip = _handsAimWalkForward != null ? _handsAimWalkForward : _handsAimWalk;
        AnimationClip tacticalHandsClip = _handsTacAimWalkForward != null ? _handsTacAimWalkForward : _handsTacAimWalk;
        return GetHandsClip(handsClip, tacticalHandsClip, useTacticalGripAnimations);
    }

    private static AnimationClip GetHandsClip(AnimationClip handsClip, AnimationClip tacticalHandsClip, bool useTacticalGripAnimations)
    {
        return useTacticalGripAnimations && tacticalHandsClip != null ? tacticalHandsClip : handsClip;
    }
}
