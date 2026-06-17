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
    [SerializeField] private AnimationClip _handsAimWalk;

    public FirstPersonWeaponAnimationClipPair GetPair(FirstPersonWeaponAnimationKey key, WeaponCondition condition)
    {
        return key switch
        {
            FirstPersonWeaponAnimationKey.Reload => new FirstPersonWeaponAnimationClipPair(_weaponReload, _handsReload),
            FirstPersonWeaponAnimationKey.ReloadFull => new FirstPersonWeaponAnimationClipPair(_weaponReloadFull, _handsReloadFull),
            FirstPersonWeaponAnimationKey.Misfire => new FirstPersonWeaponAnimationClipPair(_weaponMisfire, _handsReloadMisfire),
            FirstPersonWeaponAnimationKey.Revival => new FirstPersonWeaponAnimationClipPair(_weaponRevival, _handsRevival),
            FirstPersonWeaponAnimationKey.RevivalLast => new FirstPersonWeaponAnimationClipPair(_weaponRevivalLast, _handsRevivalLast),
            FirstPersonWeaponAnimationKey.Shoot => new FirstPersonWeaponAnimationClipPair(_weaponShoot, _handsShoot),
            FirstPersonWeaponAnimationKey.ShootLast => new FirstPersonWeaponAnimationClipPair(_weaponShootLast, _handsShoot),
            FirstPersonWeaponAnimationKey.DryEmpty => CreateIdleWeaponPair(condition, _handsDryEmpty),
            FirstPersonWeaponAnimationKey.Draw => CreateIdleWeaponPair(condition, _handsDraw),
            FirstPersonWeaponAnimationKey.Headflash => CreateIdleWeaponPair(condition, _handsHeadflash),
            FirstPersonWeaponAnimationKey.Hide => CreateIdleWeaponPair(condition, _handsHide),
            FirstPersonWeaponAnimationKey.NvgOff => CreateIdleWeaponPair(condition, _handsNvgOff),
            FirstPersonWeaponAnimationKey.NvgOn => CreateIdleWeaponPair(condition, _handsNvgOn),
            FirstPersonWeaponAnimationKey.Sprint => CreateIdleWeaponPair(condition, _handsSprint),
            FirstPersonWeaponAnimationKey.SprintEnd => CreateIdleWeaponPair(condition, _handsSprintEnd),
            FirstPersonWeaponAnimationKey.SprintStart => CreateIdleWeaponPair(condition, _handsSprintStart),
            FirstPersonWeaponAnimationKey.Walk => CreateIdleWeaponPair(condition, _handsWalk),
            FirstPersonWeaponAnimationKey.AimIn => CreateIdleWeaponPair(condition, _handsAimIn),
            FirstPersonWeaponAnimationKey.AimOut => CreateIdleWeaponPair(condition, _handsAimOut),
            FirstPersonWeaponAnimationKey.AimIdle => CreateIdleWeaponPair(condition, _handsAimIdle),
            FirstPersonWeaponAnimationKey.AimShoot => new FirstPersonWeaponAnimationClipPair(_weaponAimShoot, _handsAimShoot),
            FirstPersonWeaponAnimationKey.AimShootLast => new FirstPersonWeaponAnimationClipPair(_weaponAimShootLast, _handsAimShoot),
            FirstPersonWeaponAnimationKey.AimWalk => CreateIdleWeaponPair(condition, _handsAimWalk),
            _ => CreateIdleWeaponPair(condition, _handsIdle)
        };
    }

    public float GetLength(FirstPersonWeaponAnimationKey key, WeaponCondition condition)
    {
        FirstPersonWeaponAnimationClipPair pair = GetPair(key, condition);

        if (pair.WeaponUsesIdleClip && pair.HandsClip != null)
        {
            return Mathf.Max(0f, pair.HandsClip.length);
        }

        float weaponLength = pair.WeaponClip == null ? 0f : pair.WeaponClip.length;
        float handsLength = pair.HandsClip == null ? 0f : pair.HandsClip.length;
        return Mathf.Max(weaponLength, handsLength);
    }

    public float GetFrameTime(FirstPersonWeaponAnimationKey key, WeaponCondition condition, int frame)
    {
        if (frame <= 0)
        {
            return 0f;
        }

        FirstPersonWeaponAnimationClipPair pair = GetPair(key, condition);
        AnimationClip clip = pair.HandsClip != null ? pair.HandsClip : pair.WeaponClip;

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

    private FirstPersonWeaponAnimationClipPair CreateIdleWeaponPair(WeaponCondition condition, AnimationClip handsClip) => new(GetIdleWeaponClip(condition), handsClip, true);
}
