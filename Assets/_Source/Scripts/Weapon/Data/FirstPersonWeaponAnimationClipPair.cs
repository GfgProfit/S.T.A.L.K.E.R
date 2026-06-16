using UnityEngine;

public readonly struct FirstPersonWeaponAnimationClipPair
{
    public FirstPersonWeaponAnimationClipPair(AnimationClip weaponClip, AnimationClip handsClip, bool weaponUsesIdleClip = false)
    {
        WeaponClip = weaponClip;
        HandsClip = handsClip;
        WeaponUsesIdleClip = weaponUsesIdleClip;
    }

    public AnimationClip WeaponClip { get; }
    public AnimationClip HandsClip { get; }
    public bool WeaponUsesIdleClip { get; }
    public bool HasAnyClip => WeaponClip != null || HandsClip != null;

    public bool IsSameAs(FirstPersonWeaponAnimationClipPair other)
    {
        return WeaponClip == other.WeaponClip &&
               HandsClip == other.HandsClip &&
               WeaponUsesIdleClip == other.WeaponUsesIdleClip;
    }
}
