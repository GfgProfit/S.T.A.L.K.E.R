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

    public FirstPersonWeaponAnimationClipPair GetPair(FirstPersonWeaponAnimationKey key, WeaponCondition condition)
    {
        return key switch
        {
            FirstPersonWeaponAnimationKey.Reload => new FirstPersonWeaponAnimationClipPair(_weaponReload, _handsReload, cameraClip: _cameraReload),
            FirstPersonWeaponAnimationKey.ReloadFull => new FirstPersonWeaponAnimationClipPair(_weaponReloadFull, _handsReloadFull, cameraClip: _cameraReloadFull),
            FirstPersonWeaponAnimationKey.Misfire => new FirstPersonWeaponAnimationClipPair(_weaponMisfire, _handsReloadMisfire),
            FirstPersonWeaponAnimationKey.Revival => new FirstPersonWeaponAnimationClipPair(_weaponRevival, _handsRevival, cameraClip: _cameraRevival),
            FirstPersonWeaponAnimationKey.RevivalLast => new FirstPersonWeaponAnimationClipPair(_weaponRevivalLast, _handsRevivalLast, cameraClip: _cameraRevivalLast),
            FirstPersonWeaponAnimationKey.Shoot => new FirstPersonWeaponAnimationClipPair(_weaponShoot, _handsShoot),
            FirstPersonWeaponAnimationKey.ShootLast => new FirstPersonWeaponAnimationClipPair(_weaponShootLast, _handsShoot),
            FirstPersonWeaponAnimationKey.DryEmpty => CreateIdleWeaponPair(condition, _handsDryEmpty, _cameraDry),
            FirstPersonWeaponAnimationKey.Draw => CreateIdleWeaponPair(condition, _handsDraw, _cameraDraw),
            FirstPersonWeaponAnimationKey.Headflash => CreateIdleWeaponPair(condition, _handsHeadflash, _cameraHeadflash),
            FirstPersonWeaponAnimationKey.Hide => CreateIdleWeaponPair(condition, _handsHide, _cameraHide),
            FirstPersonWeaponAnimationKey.NvgOff => CreateIdleWeaponPair(condition, _handsNvgOff, _cameraNvgOff),
            FirstPersonWeaponAnimationKey.NvgOn => CreateIdleWeaponPair(condition, _handsNvgOn, _cameraNvgOn),
            FirstPersonWeaponAnimationKey.Sprint => CreateIdleWeaponPair(condition, _handsSprint),
            FirstPersonWeaponAnimationKey.SprintEnd => CreateIdleWeaponPair(condition, _handsSprintEnd),
            FirstPersonWeaponAnimationKey.SprintStart => CreateIdleWeaponPair(condition, _handsSprintStart),
            FirstPersonWeaponAnimationKey.Walk => CreateIdleWeaponPair(condition, _handsWalk),
            FirstPersonWeaponAnimationKey.AimIn => CreateIdleWeaponPair(condition, _handsAimIn, _cameraAimIn),
            FirstPersonWeaponAnimationKey.AimOut => CreateIdleWeaponPair(condition, _handsAimOut, _cameraAimOut),
            FirstPersonWeaponAnimationKey.AimIdle => CreateIdleWeaponPair(condition, _handsAimIdle),
            FirstPersonWeaponAnimationKey.AimShoot => new FirstPersonWeaponAnimationClipPair(_weaponAimShoot, _handsAimShoot),
            FirstPersonWeaponAnimationKey.AimShootLast => new FirstPersonWeaponAnimationClipPair(_weaponAimShootLast, _handsAimShoot),
            FirstPersonWeaponAnimationKey.AimDry => CreateIdleWeaponPair(condition, _handsAimDry, _cameraDry),
            FirstPersonWeaponAnimationKey.AimWalk => CreateIdleWeaponPair(condition, _handsAimWalkForward != null ? _handsAimWalkForward : _handsAimWalk),
            FirstPersonWeaponAnimationKey.AimWalkBackward => CreateIdleWeaponPair(condition, _handsAimWalkBackward),
            FirstPersonWeaponAnimationKey.AimWalkLeft => CreateIdleWeaponPair(condition, _handsAimWalkLeft),
            FirstPersonWeaponAnimationKey.AimWalkRight => CreateIdleWeaponPair(condition, _handsAimWalkRight),
            _ => CreateIdleWeaponPair(condition, _handsIdle)
        };
    }

    public float GetLength(FirstPersonWeaponAnimationKey key, WeaponCondition condition)
    {
        FirstPersonWeaponAnimationClipPair pair = GetPair(key, condition);

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

    public float GetFrameTime(FirstPersonWeaponAnimationKey key, WeaponCondition condition, int frame)
    {
        if (frame <= 0)
        {
            return 0f;
        }

        FirstPersonWeaponAnimationClipPair pair = GetPair(key, condition);
        AnimationClip clip = pair.HandsClip != null ? pair.HandsClip : pair.WeaponClip != null ? pair.WeaponClip : pair.CameraClip;

        if (clip == null || clip.frameRate <= 0f)
        {
            return 0f;
        }

        return Mathf.Min(frame / clip.frameRate, GetLength(key, condition));
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
}
