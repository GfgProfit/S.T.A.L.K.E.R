using UnityEngine;

public readonly struct FirstPersonWeaponAnimationClipPair
{
    public FirstPersonWeaponAnimationClipPair(AnimationClip weaponClip, AnimationClip handsClip, bool weaponUsesIdleClip = false, AnimationClip cameraClip = null)
    {
        WeaponClip = weaponClip;
        HandsClip = handsClip;
        WeaponUsesIdleClip = weaponUsesIdleClip;
        CameraClip = cameraClip;
    }

    public AnimationClip WeaponClip { get; }
    public AnimationClip HandsClip { get; }
    public AnimationClip CameraClip { get; }
    public bool WeaponUsesIdleClip { get; }
    public bool HasAnyClip => WeaponClip != null || HandsClip != null || CameraClip != null;

    public bool IsSameAs(FirstPersonWeaponAnimationClipPair other)
    {
        return WeaponClip == other.WeaponClip &&
               HandsClip == other.HandsClip &&
               CameraClip == other.CameraClip &&
               WeaponUsesIdleClip == other.WeaponUsesIdleClip;
    }
}
