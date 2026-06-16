public sealed class FirstPersonWeaponMagazineState
{
    public int RequestedAmmoIndex { get; private set; }
    public ItemData RequestedAmmoData { get; private set; }
    public ItemData LoadedAmmoData { get; private set; }
    public int LoadedAmmoAmount { get; private set; }

    public void SetRequestedAmmo(int requestedAmmoIndex, ItemData requestedAmmoData)
    {
        RequestedAmmoIndex = requestedAmmoIndex < 0 ? 0 : requestedAmmoIndex;
        RequestedAmmoData = requestedAmmoData;
    }

    public void SetLoadedAmmo(ItemData loadedAmmoData, int loadedAmmoAmount)
    {
        if (loadedAmmoData == null || loadedAmmoAmount <= 0)
        {
            ClearLoadedAmmo();
            return;
        }

        LoadedAmmoData = loadedAmmoData;
        LoadedAmmoAmount = loadedAmmoAmount;
    }

    public void ClearLoadedAmmo()
    {
        LoadedAmmoData = null;
        LoadedAmmoAmount = 0;
    }

    public void Clear()
    {
        RequestedAmmoIndex = 0;
        RequestedAmmoData = null;
        ClearLoadedAmmo();
    }
}
