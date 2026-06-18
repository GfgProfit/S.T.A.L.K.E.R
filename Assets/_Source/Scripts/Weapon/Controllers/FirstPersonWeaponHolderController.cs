using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

public sealed class FirstPersonWeaponHolderController : MonoBehaviour
{
    [SerializeField] private Transform _weaponHolder;
    [SerializeField] private InventoryController _inventoryController;
    [SerializeField] private FirstPersonWeaponAmmoHudView _ammoHudView;
    [SerializeField] private FirstPersonWeaponAimPointView _aimPointView;
    [SerializeField] private bool _clearExistingHolderChildrenOnAwake = true;

    [Inject] private IPlayerInput _playerInput = null;

    private FirstPersonWeaponAmmoHudViewModel _ammoHudViewModel;
    private IPlayerInput _fallbackPlayerInput;
    private InventoryItem _currentWeaponItem;
    private ItemData _currentWeaponItemData;
    private ItemData _equippedArmorItemData;
    private GameObject _spawnedWeapon;
    private FirstPersonWeaponRuntimeController _spawnedWeaponRuntimeController;
    private CancellationTokenSource _switchCancellation;
    private bool _isSwitchingWeapon;

    public ItemData CurrentWeaponItemData => _currentWeaponItemData;
    public InventoryItem CurrentWeaponItem => _currentWeaponItem;
    public bool IsSwitchingWeapon => _isSwitchingWeapon;
    public bool IsWeaponChangeLocked => _isSwitchingWeapon || IsCurrentWeaponReloading;

    private IPlayerInput PlayerInput
    {
        get
        {
            if (_playerInput != null)
            {
                return _playerInput;
            }

            _fallbackPlayerInput ??= new LegacyPlayerInput();
            return _fallbackPlayerInput;
        }
    }

    private void Awake()
    {
        _ammoHudViewModel = new FirstPersonWeaponAmmoHudViewModel();
        _ammoHudView?.Bind(_ammoHudViewModel);
        _ammoHudViewModel.Clear();
        _aimPointView?.SetAimActive(false, true);

        if (_clearExistingHolderChildrenOnAwake)
        {
            ClearHolderChildren();
        }
    }

    private void OnDestroy()
    {
        CancelWeaponSwitch();
        ClearSpawnedWeapon();
        _ammoHudView?.Unbind();
        _ammoHudViewModel?.Dispose();
    }

    private void Update()
    {
        _aimPointView?.SetAimActive(_spawnedWeaponRuntimeController != null && _spawnedWeaponRuntimeController.IsAiming);
    }

    public bool SetWeapon(InventoryItem weaponItem)
    {
        if (_currentWeaponItem == weaponItem)
        {
            return true;
        }

        if (IsWeaponChangeLocked)
        {
            return false;
        }

        StartWeaponSwitch(weaponItem).Forget(Debug.LogException);
        return true;
    }

    public bool ClearWeapon()
    {
        if (_currentWeaponItem == null && _spawnedWeapon == null)
        {
            _ammoHudViewModel?.Clear();
            return true;
        }

        if (IsWeaponChangeLocked)
        {
            return false;
        }

        StartWeaponSwitch(null).Forget(Debug.LogException);
        return true;
    }

    public void SetEquippedArmor(ItemData armorItemData)
    {
        _equippedArmorItemData = armorItemData != null && armorItemData.ItemType == ItemType.Armor ? armorItemData : null;
        ApplyEquippedArmorToSpawnedWeapon();
    }

    public bool TryUnloadCurrentWeapon()
    {
        if (_spawnedWeapon == null)
        {
            return false;
        }

        FirstPersonWeaponRuntimeController runtimeController = _spawnedWeapon.GetComponent<FirstPersonWeaponRuntimeController>();
        return runtimeController != null && runtimeController.TryUnloadMagazine();
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
        spawnedTransform.localScale = _currentWeaponItemData == null ? Vector3.one : _currentWeaponItemData.FirstPersonWeaponSpawnScale;

        FirstPersonWeaponRuntimeController runtimeController = _spawnedWeapon.GetComponent<FirstPersonWeaponRuntimeController>();
        _spawnedWeaponRuntimeController = runtimeController;
        runtimeController?.Initialize(_currentWeaponItem, _inventoryController, PlayerInput, _ammoHudViewModel);
        ApplyEquippedArmorToSpawnedWeapon();
    }

    private async UniTask StartWeaponSwitch(InventoryItem weaponItem)
    {
        CancelWeaponSwitch();
        _switchCancellation = CancellationTokenSource.CreateLinkedTokenSource(destroyCancellationToken);
        _isSwitchingWeapon = true;

        try
        {
            await SwitchWeaponAsync(weaponItem, _switchCancellation.Token);
        }
        catch (OperationCanceledException)
        {
        }
        finally
        {
            _isSwitchingWeapon = false;
            _switchCancellation?.Dispose();
            _switchCancellation = null;
        }
    }

    private async UniTask SwitchWeaponAsync(InventoryItem weaponItem, CancellationToken cancellationToken)
    {
        await PlayHideAndClearAsync(cancellationToken);

        cancellationToken.ThrowIfCancellationRequested();

        _currentWeaponItem = weaponItem;
        _currentWeaponItemData = weaponItem == null ? null : weaponItem.ItemData;

        if (_currentWeaponItemData == null)
        {
            _ammoHudViewModel?.Clear();
            return;
        }

        if (_currentWeaponItemData.FirstPersonWeaponPrefab == null)
        {
            Debug.LogWarning($"[{nameof(FirstPersonWeaponHolderController)}] {_currentWeaponItemData.name} has no first person weapon prefab.", this);
            _ammoHudViewModel?.Clear();
            return;
        }

        SpawnWeapon(_currentWeaponItemData.FirstPersonWeaponPrefab);
        await PlayDrawAsync(cancellationToken);
    }

    private async UniTask PlayHideAndClearAsync(CancellationToken cancellationToken)
    {
        if (_spawnedWeapon == null)
        {
            return;
        }

        FirstPersonWeaponController weaponController = _spawnedWeapon.GetComponent<FirstPersonWeaponController>();
        FirstPersonWeaponRuntimeController runtimeController = _spawnedWeapon.GetComponent<FirstPersonWeaponRuntimeController>();
        float duration = weaponController == null ? 0f : weaponController.PlayHide();

        runtimeController?.LockWeaponInput(duration);
        await DelaySecondsAsync(duration, cancellationToken);
        ClearSpawnedWeapon();
    }

    private async UniTask PlayDrawAsync(CancellationToken cancellationToken)
    {
        if (_spawnedWeapon == null)
        {
            return;
        }

        FirstPersonWeaponController weaponController = _spawnedWeapon.GetComponent<FirstPersonWeaponController>();
        FirstPersonWeaponRuntimeController runtimeController = _spawnedWeapon.GetComponent<FirstPersonWeaponRuntimeController>();
        float duration = weaponController == null ? 0f : weaponController.PlayStartup();

        runtimeController?.LockWeaponInput(duration);
        await DelaySecondsAsync(duration, cancellationToken);
    }

    private async UniTask DelaySecondsAsync(float delay, CancellationToken cancellationToken)
    {
        if (delay <= 0f)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return;
        }

        bool isCanceled = await UniTask.Delay(TimeSpan.FromSeconds(delay), cancellationToken: cancellationToken).SuppressCancellationThrow();

        if (isCanceled)
        {
            cancellationToken.ThrowIfCancellationRequested();
        }
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
        _spawnedWeaponRuntimeController = null;
        _aimPointView?.SetAimActive(false);
    }

    private void ClearSpawnedWeapon()
    {
        if (_spawnedWeapon == null)
        {
            return;
        }

        Destroy(_spawnedWeapon);
        _spawnedWeapon = null;
        _spawnedWeaponRuntimeController = null;
        _aimPointView?.SetAimActive(false);
    }

    private void CancelWeaponSwitch()
    {
        if (_switchCancellation == null)
        {
            return;
        }

        _switchCancellation.Cancel();
        _switchCancellation.Dispose();
        _switchCancellation = null;
        _isSwitchingWeapon = false;
    }

    private void ApplyEquippedArmorToSpawnedWeapon()
    {
        if (_spawnedWeapon == null)
        {
            return;
        }

        FirstPersonWeaponController weaponController = _spawnedWeapon.GetComponent<FirstPersonWeaponController>();
        weaponController?.SetEquippedArmor(_equippedArmorItemData);
    }

    private bool IsCurrentWeaponReloading
    {
        get
        {
            if (_spawnedWeapon == null)
            {
                return false;
            }

            FirstPersonWeaponRuntimeController runtimeController = _spawnedWeapon.GetComponent<FirstPersonWeaponRuntimeController>();
            return runtimeController != null && runtimeController.IsReloading;
        }
    }
}
