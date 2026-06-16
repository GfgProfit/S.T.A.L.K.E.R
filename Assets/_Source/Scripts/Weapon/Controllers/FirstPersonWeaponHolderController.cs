using UnityEngine;

public sealed class FirstPersonWeaponHolderController : MonoBehaviour
{
    [SerializeField] private Transform _weaponHolder;
    [SerializeField] private bool _clearExistingHolderChildrenOnAwake = true;

    private ItemData _currentWeaponItemData;
    private GameObject _spawnedWeapon;

    public ItemData CurrentWeaponItemData => _currentWeaponItemData;

    private void Awake()
    {
        if (_clearExistingHolderChildrenOnAwake)
        {
            ClearHolderChildren();
        }
    }

    private void OnDestroy() => ClearSpawnedWeapon();

    public void SetWeapon(ItemData weaponItemData)
    {
        if (_currentWeaponItemData == weaponItemData)
        {
            return;
        }

        ClearSpawnedWeapon();
        _currentWeaponItemData = weaponItemData;

        if (_currentWeaponItemData == null)
        {
            return;
        }

        if (_currentWeaponItemData.FirstPersonWeaponPrefab == null)
        {
            Debug.LogWarning($"[{nameof(FirstPersonWeaponHolderController)}] {weaponItemData.name} has no first person weapon prefab.", this);
            return;
        }

        SpawnWeapon(_currentWeaponItemData.FirstPersonWeaponPrefab);
    }

    public void ClearWeapon()
    {
        ClearSpawnedWeapon();
        _currentWeaponItemData = null;
    }

    private void SpawnWeapon(GameObject weaponPrefab)
    {
        if (_weaponHolder == null)
        {
            Debug.LogWarning($"[{nameof(FirstPersonWeaponHolderController)}] Weapon holder is not assigned.", this);
            return;
        }

        _spawnedWeapon = Instantiate(weaponPrefab, _weaponHolder);
        Transform spawnedTransform = _spawnedWeapon.transform;
        spawnedTransform.localPosition = Vector3.zero;
        spawnedTransform.localRotation = Quaternion.identity;
        spawnedTransform.localScale = Vector3.one;
    }

    private void ClearHolderChildren()
    {
        if (_weaponHolder == null)
        {
            return;
        }

        for (int i = _weaponHolder.childCount - 1; i >= 0; i--)
        {
            Destroy(_weaponHolder.GetChild(i).gameObject);
        }

        _spawnedWeapon = null;
    }

    private void ClearSpawnedWeapon()
    {
        if (_spawnedWeapon == null)
        {
            return;
        }

        Destroy(_spawnedWeapon);
        _spawnedWeapon = null;
    }
}