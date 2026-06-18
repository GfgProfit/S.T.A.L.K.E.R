using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

public sealed class FirstPersonWeaponRuntimeController : MonoBehaviour
{
    private const float MOVEMENT_INPUT_THRESHOLD = 0.01f;
    private const float DEFAULT_LOOP_ANIMATION_SPEED = 1f;
    private const float CROUCH_WALK_ANIMATION_SPEED = 0.5f;
    private const string WEAPON_RECOIL_OBJECT_NAME = "Weapon Recoil";
    private const string WEAPON_RECOIL_OBJECT_NAME_COMPACT = "WeaponRecoil";

    private InventoryItem _weaponItem;
    private ItemData _weaponItemData;
    private WeaponData _weaponData;
    private InventoryController _inventoryController;
    private IPlayerInput _playerInput;
    private FirstPersonWeaponController _animationController;
    private FirstPersonCameraAllAnimationController _cameraAllAnimationController;
    private FirstPersonWeaponAmmoHudViewModel _ammoHudViewModel;
    private PlayerController _playerController;
    private WeaponRecoilService _weaponRecoilService;
    private CancellationTokenSource _reloadCancellation;
    private ItemData _requestedAmmoData;
    private ItemData _loadedAmmoData;
    private ItemData _reloadAmmoData;
    private WeaponMovementAnimationState _movementAnimationState;
    private int _requestedAmmoIndex;
    private int _loadedAmmoAmount;
    private float _movementAnimationLockUntilTime;
    private float _weaponInputLockUntilTime;
    private float _nextShootTime;
    private bool _initialized;
    private bool _isReloading;
    private bool _isAiming;
    private bool _sprintBlockedByAim;
    private bool _reloadAmmoApplied;

    public ItemData RequestedAmmoData => _requestedAmmoData;
    public ItemData LoadedAmmoData => _loadedAmmoData;
    public int LoadedAmmoAmount => _loadedAmmoAmount;
    public bool IsReloading => _isReloading;
    public bool IsAiming => _isAiming;

    public void Initialize(InventoryItem weaponItem, InventoryController inventoryController, IPlayerInput playerInput, FirstPersonWeaponAmmoHudViewModel ammoHudViewModel)
    {
        CancelReload();
        SetSprintBlockedByAim(false);

        _weaponItem = weaponItem;
        _weaponItemData = weaponItem == null ? null : weaponItem.ItemData;
        _weaponData = _weaponItemData == null ? null : _weaponItemData.WeaponData;
        _inventoryController = inventoryController;
        _playerInput = playerInput;
        _ammoHudViewModel = ammoHudViewModel;
        _animationController = GetComponent<FirstPersonWeaponController>();
        _cameraAllAnimationController = FindCameraAllAnimationController();
        _cameraAllAnimationController?.SetAimActive(false);
        _animationController?.SetAimRootPositionOffsetActive(false, true);
        _playerController = GetComponentInParent<PlayerController>();
        _weaponRecoilService?.Reset();
        _weaponRecoilService = _weaponData == null ? null : new WeaponRecoilService(FindWeaponRecoilTransform());
        RestoreMagazineState();
        _reloadAmmoData = null;
        _movementAnimationState = WeaponMovementAnimationState.None;
        _movementAnimationLockUntilTime = 0f;
        _weaponInputLockUntilTime = 0f;
        _nextShootTime = 0f;
        _isReloading = false;
        _isAiming = false;
        _reloadAmmoApplied = false;
        _initialized = _weaponData != null;

        _animationController?.SetCondition(_loadedAmmoAmount > 0 ? WeaponCondition.Normal : WeaponCondition.Empty);
        SyncMagazineState();
        UpdateAmmoHud();
    }

    private void OnDestroy()
    {
        SetSprintBlockedByAim(false);
        _cameraAllAnimationController?.SetAimActive(false);
        _animationController?.SetAimRootPositionOffsetActive(false, true);
        CancelReload();
        ResetWeaponRecoil();
    }

    private void OnDisable()
    {
        SetSprintBlockedByAim(false);
        _cameraAllAnimationController?.SetAimActive(false);
        _animationController?.SetAimRootPositionOffsetActive(false, true);
        CancelReload();
        ResetWeaponRecoil();
    }

    private void Update()
    {
        if (_initialized == false)
        {
            return;
        }

        UpdateWeaponRecoil();

        if (_inventoryController != null && _inventoryController.IsOpen)
        {
            UpdateAmmoHud();
            return;
        }

        if (_isReloading)
        {
            UpdateAmmoHud();
            return;
        }

        if (IsWeaponInputLocked)
        {
            UpdateAmmoHud();
            return;
        }

        if (_playerInput != null && _playerInput.IsWeaponAmmoTypeChangePressed())
        {
            TryChangeAmmoType();
        }

        bool isSprintInputActive = IsSprintInputActive();
        UpdateAimState(isSprintInputActive);

        if (isSprintInputActive == false && _isAiming == false && _playerInput != null && _playerInput.IsWeaponReloadPressed() && TryReload())
        {
            UpdateAmmoHud();
            return;
        }

        if (isSprintInputActive == false && IsShootInputActive() && TryShoot())
        {
            UpdateAmmoHud();
            return;
        }

        if (IsMovementAnimationLocked == false)
        {
            UpdateMovementAnimation();
        }

        UpdateAmmoHud();
    }

    public bool TryShoot()
    {
        if (_weaponData == null || _isReloading || IsSprintInputActive() || Time.time < _nextShootTime)
        {
            return false;
        }

        if (_loadedAmmoAmount <= 0)
        {
            PlayDryEmptyAnimation();
            return true;
        }

        bool isLastRound = _loadedAmmoAmount == 1;
        _nextShootTime = Time.time + _weaponData.SecondsBetweenShots;
        _loadedAmmoAmount--;
        ApplyDurabilityShotCost();

        FirstPersonWeaponAnimationKey animationKey = _isAiming
            ? (isLastRound ? FirstPersonWeaponAnimationKey.AimShootLast : FirstPersonWeaponAnimationKey.AimShoot)
            : (isLastRound ? FirstPersonWeaponAnimationKey.ShootLast : FirstPersonWeaponAnimationKey.Shoot);

        if (_isAiming)
        {
            _animationController?.PlayAimShoot(isLastRound);
        }
        else
        {
            _animationController?.Shoot(isLastRound);
        }

        _weaponRecoilService?.RecoilShoot(_weaponData.RecoilX, _weaponData.RecoilY, _weaponData.RecoilZ);
        LockMovementAnimation(animationKey);
        _movementAnimationState = WeaponMovementAnimationState.None;

        if (isLastRound)
        {
            _loadedAmmoData = null;
            _animationController?.SetCondition(WeaponCondition.Empty);
        }

        SyncMagazineState();
        RefreshInventoryWeightState();
        return true;
    }

    private void PlayDryEmptyAnimation()
    {
        FirstPersonWeaponAnimationKey animationKey = FirstPersonWeaponAnimationKey.DryEmpty;

        _loadedAmmoData = null;
        _animationController?.SetCondition(WeaponCondition.Empty);
        float animationLength = _animationController == null ? 0f : _animationController.PlayDryEmpty(_isAiming);
        _nextShootTime = Time.time + Mathf.Max(_weaponData.SecondsBetweenShots, animationLength);
        LockMovementAnimation(animationKey);
        _movementAnimationState = WeaponMovementAnimationState.None;
        SyncMagazineState();
    }

    public bool TryReload()
    {
        if (_weaponData == null || _inventoryController == null || _requestedAmmoData == null || _isReloading || _isAiming || IsSprintInputActive())
        {
            return false;
        }

        if (_inventoryController.GetInventoryItemCount(_requestedAmmoData) <= 0)
        {
            return false;
        }

        if (_loadedAmmoData == _requestedAmmoData && _loadedAmmoAmount >= _weaponData.MagazineCapacity)
        {
            return false;
        }

        bool isFullReload = _loadedAmmoAmount <= 0;
        PlayReloadAnimation(isFullReload, _requestedAmmoData);
        UpdateAmmoHud();
        return true;
    }

    public bool TryChangeAmmoType()
    {
        if (_weaponData == null || _inventoryController == null)
        {
            return false;
        }

        int nextAmmoIndex = GetNextAvailableAmmoTypeIndex();

        if (nextAmmoIndex < 0)
        {
            return false;
        }

        _requestedAmmoIndex = nextAmmoIndex;
        _requestedAmmoData = _weaponData.GetCompatibleAmmo(_requestedAmmoIndex);
        SyncMagazineState();
        UpdateAmmoHud();
        return true;
    }

    public bool TryUnloadMagazine()
    {
        if (_weaponData == null || _isReloading || _loadedAmmoData == null || _loadedAmmoAmount <= 0)
        {
            return false;
        }

        ReturnLoadedAmmo();
        _animationController?.SetCondition(WeaponCondition.Empty);
        UpdateAmmoHud();
        return true;
    }

    public void LockWeaponInput(float duration)
    {
        if (duration <= 0f)
        {
            return;
        }

        float lockUntilTime = Time.time + duration;
        _weaponInputLockUntilTime = Mathf.Max(_weaponInputLockUntilTime, lockUntilTime);
        _movementAnimationLockUntilTime = Mathf.Max(_movementAnimationLockUntilTime, lockUntilTime);
    }

    private void UpdateMovementAnimation()
    {
        if (_animationController == null || _playerInput == null)
        {
            return;
        }

        Vector2 movementInput = _playerInput.GetMovementInput();
        bool hasMovement = movementInput.sqrMagnitude > MOVEMENT_INPUT_THRESHOLD;
        bool isSprinting = IsSprintInputActive(movementInput);
        bool isCrouching = _playerInput.IsCrouchingHold();

        if (_isAiming)
        {
            if (hasMovement)
            {
                FirstPersonWeaponAnimationKey aimWalkKey = ResolveAimWalkAnimationKey(movementInput);

                if (_animationController.CurrentAnimationKey != aimWalkKey)
                {
                    _animationController.PlayAimWalk(aimWalkKey);
                }

                _animationController.SetLoopPlaybackSpeed(isCrouching ? CROUCH_WALK_ANIMATION_SPEED : DEFAULT_LOOP_ANIMATION_SPEED);
                _movementAnimationState = WeaponMovementAnimationState.Walking;
                return;
            }

            if (_animationController.CurrentAnimationKey != FirstPersonWeaponAnimationKey.AimIdle)
            {
                _animationController.PlayAimIdle();
            }

            _animationController.SetLoopPlaybackSpeed(DEFAULT_LOOP_ANIMATION_SPEED);
            _movementAnimationState = WeaponMovementAnimationState.Idle;
            return;
        }

        if (isSprinting)
        {
            _animationController.SetLoopPlaybackSpeed(DEFAULT_LOOP_ANIMATION_SPEED);

            if (_animationController.CurrentAnimationKey != FirstPersonWeaponAnimationKey.Sprint &&
                _animationController.CurrentAnimationKey != FirstPersonWeaponAnimationKey.SprintStart)
            {
                _animationController.Play(FirstPersonWeaponAnimationKey.SprintStart);
                LockMovementAnimation(FirstPersonWeaponAnimationKey.SprintStart);
            }

            _movementAnimationState = WeaponMovementAnimationState.Sprinting;
            return;
        }

        if (_movementAnimationState == WeaponMovementAnimationState.Sprinting &&
            _animationController.CurrentAnimationKey == FirstPersonWeaponAnimationKey.Sprint)
        {
            _animationController.Play(FirstPersonWeaponAnimationKey.SprintEnd);
            LockMovementAnimation(FirstPersonWeaponAnimationKey.SprintEnd);
            _movementAnimationState = WeaponMovementAnimationState.None;
            return;
        }

        if (hasMovement)
        {
            if (_animationController.CurrentAnimationKey != FirstPersonWeaponAnimationKey.Walk)
            {
                _animationController.PlayWalk();
            }

            _animationController.SetLoopPlaybackSpeed(isCrouching ? CROUCH_WALK_ANIMATION_SPEED : DEFAULT_LOOP_ANIMATION_SPEED);
            _movementAnimationState = WeaponMovementAnimationState.Walking;
            return;
        }

        if (_animationController.CurrentAnimationKey != FirstPersonWeaponAnimationKey.Idle)
        {
            _animationController.PlayIdle();
        }

        _animationController.SetLoopPlaybackSpeed(DEFAULT_LOOP_ANIMATION_SPEED);
        _movementAnimationState = WeaponMovementAnimationState.Idle;
    }

    private int GetNextAvailableAmmoTypeIndex()
    {
        if (_weaponData == null || _weaponData.HasCompatibleAmmo == false)
        {
            return -1;
        }

        int ammoTypeCount = _weaponData.CompatibleAmmo.Count;

        for (int offset = 1; offset < ammoTypeCount; offset++)
        {
            int ammoIndex = (_requestedAmmoIndex + offset) % ammoTypeCount;
            ItemData ammoData = _weaponData.GetCompatibleAmmo(ammoIndex);

            if (ammoData != null && _inventoryController.GetInventoryItemCount(ammoData) > 0)
            {
                return ammoIndex;
            }
        }

        return -1;
    }

    private void PlayReloadAnimation(bool isFullReload, ItemData reloadAmmoData)
    {
        FirstPersonWeaponAnimationKey animationKey = isFullReload ? FirstPersonWeaponAnimationKey.ReloadFull : FirstPersonWeaponAnimationKey.Reload;
        float applyDelay = GetReloadAmmoApplyDelay(isFullReload, animationKey);
        float animationLength = GetAnimationLength(animationKey);

        CancelReload();
        _reloadAmmoData = reloadAmmoData;
        _reloadAmmoApplied = false;
        _isReloading = true;
        _movementAnimationState = WeaponMovementAnimationState.None;
        _animationController?.Reload(isFullReload);
        LockMovementAnimation(animationKey);

        _reloadCancellation = CancellationTokenSource.CreateLinkedTokenSource(destroyCancellationToken);
        PlayReloadAsync(isFullReload, applyDelay, animationLength, _reloadCancellation).Forget(Debug.LogException);
    }

    private async UniTask PlayReloadAsync(bool isFullReload, float applyDelay, float animationLength, CancellationTokenSource reloadCancellation)
    {
        CancellationToken cancellationToken = reloadCancellation.Token;

        if (await DelaySecondsAsync(applyDelay, cancellationToken))
        {
            return;
        }

        ApplyReloadAmmo();

        float remainingDelay = Mathf.Max(0f, animationLength - applyDelay);

        if (await DelaySecondsAsync(remainingDelay, cancellationToken))
        {
            return;
        }

        if (_reloadCancellation != reloadCancellation)
        {
            return;
        }

        _isReloading = false;
        _reloadAmmoData = null;

        if (isFullReload)
        {
            _animationController?.SetCondition(_loadedAmmoAmount > 0 ? WeaponCondition.Normal : WeaponCondition.Empty);
        }

        _reloadCancellation = null;
        reloadCancellation.Dispose();
        UpdateAmmoHud();
    }

    private async UniTask<bool> DelaySecondsAsync(float delay, CancellationToken cancellationToken)
    {
        if (delay <= 0f)
        {
            return cancellationToken.IsCancellationRequested;
        }

        return await UniTask.Delay(TimeSpan.FromSeconds(delay), cancellationToken: cancellationToken).SuppressCancellationThrow();
    }

    private void ApplyReloadAmmo()
    {
        if (_reloadAmmoApplied || _inventoryController == null || _weaponData == null || _reloadAmmoData == null)
        {
            return;
        }

        if (_inventoryController.GetInventoryItemCount(_reloadAmmoData) <= 0)
        {
            return;
        }

        ReturnLoadedAmmo();

        int availableAmmoAmount = _inventoryController.GetInventoryItemCount(_reloadAmmoData);
        int targetAmmoAmount = Mathf.Min(_weaponData.MagazineCapacity, availableAmmoAmount);
        int consumedAmmoAmount = _inventoryController.ConsumeInventoryItem(_reloadAmmoData, targetAmmoAmount);

        if (consumedAmmoAmount <= 0)
        {
            return;
        }

        _loadedAmmoData = _reloadAmmoData;
        _loadedAmmoAmount = consumedAmmoAmount;
        _reloadAmmoApplied = true;
        SyncMagazineState();
        RefreshInventoryWeightState();
        UpdateAmmoHud();
    }

    private void ReturnLoadedAmmo()
    {
        if (_loadedAmmoData == null || _loadedAmmoAmount <= 0)
        {
            return;
        }

        ItemData loadedAmmoData = _loadedAmmoData;
        int loadedAmmoAmount = _loadedAmmoAmount;
        _loadedAmmoData = null;
        _loadedAmmoAmount = 0;
        SyncMagazineState();
        RefreshInventoryWeightState();

        _inventoryController.TryReturnItemToInventoryOrDrop(loadedAmmoData, loadedAmmoAmount);
    }

    private void RestoreMagazineState()
    {
        _requestedAmmoIndex = 0;
        _requestedAmmoData = _weaponData == null ? null : _weaponData.GetCompatibleAmmo(_requestedAmmoIndex);
        _loadedAmmoData = null;
        _loadedAmmoAmount = 0;

        if (_weaponData == null || _weaponItem == null)
        {
            return;
        }

        FirstPersonWeaponMagazineState magazineState = _weaponItem.WeaponMagazineState;
        int requestedAmmoIndex = magazineState.RequestedAmmoIndex;
        ItemData requestedAmmoData = _weaponData.GetCompatibleAmmo(requestedAmmoIndex);

        if (requestedAmmoData == null)
        {
            requestedAmmoIndex = 0;
            requestedAmmoData = _weaponData.GetCompatibleAmmo(requestedAmmoIndex);
        }

        _requestedAmmoIndex = requestedAmmoIndex;
        _requestedAmmoData = requestedAmmoData;

        if (_weaponData.GetCompatibleAmmoIndex(magazineState.LoadedAmmoData) < 0)
        {
            return;
        }

        _loadedAmmoAmount = Mathf.Clamp(magazineState.LoadedAmmoAmount, 0, _weaponData.MagazineCapacity);

        if (_loadedAmmoAmount <= 0)
        {
            return;
        }

        _loadedAmmoData = magazineState.LoadedAmmoData;
    }

    private void SyncMagazineState()
    {
        if (_weaponItem == null)
        {
            return;
        }

        FirstPersonWeaponMagazineState magazineState = _weaponItem.WeaponMagazineState;
        magazineState.SetRequestedAmmo(_requestedAmmoIndex, _requestedAmmoData);
        magazineState.SetLoadedAmmo(_loadedAmmoData, _loadedAmmoAmount);
    }

    private void RefreshInventoryWeightState()
    {
        _inventoryController?.RefreshInventoryWeightState();
    }

    private bool IsShootInputActive()
    {
        if (_playerInput == null || _weaponData == null)
        {
            return false;
        }

        return _weaponData.FireMode == WeaponFireMode.Auto ? _playerInput.IsWeaponShootHeld() : _playerInput.IsWeaponShootPressed();
    }

    private static FirstPersonWeaponAnimationKey ResolveAimWalkAnimationKey(Vector2 movementInput)
    {
        if (Mathf.Abs(movementInput.x) > Mathf.Abs(movementInput.y))
        {
            return movementInput.x < 0f ? FirstPersonWeaponAnimationKey.AimWalkLeft : FirstPersonWeaponAnimationKey.AimWalkRight;
        }

        return movementInput.y < 0f ? FirstPersonWeaponAnimationKey.AimWalkBackward : FirstPersonWeaponAnimationKey.AimWalk;
    }

    private void UpdateAimState(bool isSprintInputActive)
    {
        if (_animationController == null || IsMovementAnimationLocked)
        {
            return;
        }

        bool shouldAim = isSprintInputActive == false && IsAimInputActive();

        if (shouldAim)
        {
            if (_isAiming == false)
            {
                _isAiming = true;
                SetSprintBlockedByAim(true);
                _cameraAllAnimationController?.SetAimActive(true);
                _animationController.SetAimRootPositionOffsetActive(true);
                _movementAnimationState = WeaponMovementAnimationState.None;
                _animationController.Play(FirstPersonWeaponAnimationKey.AimIn);
                LockMovementAnimation(FirstPersonWeaponAnimationKey.AimIn);
            }

            return;
        }

        if (_isAiming == false)
        {
            return;
        }

        _isAiming = false;
        SetSprintBlockedByAim(false);
        _cameraAllAnimationController?.SetAimActive(false);
        _animationController.SetAimRootPositionOffsetActive(false);
        _movementAnimationState = WeaponMovementAnimationState.None;
        _animationController.Play(FirstPersonWeaponAnimationKey.AimOut);
        LockMovementAnimation(FirstPersonWeaponAnimationKey.AimOut);
    }

    private bool IsSprintInputActive()
    {
        if (_playerInput == null)
        {
            return false;
        }

        return IsSprintInputActive(_playerInput.GetMovementInput());
    }

    private bool IsSprintInputActive(Vector2 movementInput)
    {
        return _playerInput != null &&
               IsAimInputActive() == false &&
               _isAiming == false &&
               _playerInput.IsCrouchingHold() == false &&
               _playerInput.IsSprintHeld() &&
               movementInput.y > MOVEMENT_INPUT_THRESHOLD &&
               movementInput.sqrMagnitude > MOVEMENT_INPUT_THRESHOLD;
    }

    private bool IsAimInputActive()
    {
        return (_animationController != null && _animationController.ForceAim) ||
               (_playerInput != null && _playerInput.IsWeaponAimHeld());
    }

    private void ApplyDurabilityShotCost()
    {
        if (_weaponItem == null || _weaponItem.HasDurability == false || _weaponData == null)
        {
            return;
        }

        float nextDurability = _weaponItem.CurrentDurabilityPercent - _weaponData.DurabilityPercentPerShot;
        _weaponItem.SetDurability(nextDurability);
    }

    private void LockMovementAnimation(FirstPersonWeaponAnimationKey animationKey)
    {
        _movementAnimationLockUntilTime = Time.time + GetCurrentAnimationLength(animationKey);
    }

    private float GetReloadAmmoApplyDelay(bool isFullReload, FirstPersonWeaponAnimationKey animationKey)
    {
        if (_weaponData == null || _animationController == null)
        {
            return 0f;
        }

        int applyFrame = _weaponData.GetReloadAmmoApplyFrame(isFullReload);
        return _animationController.GetNextAnimationFrameTime(animationKey, applyFrame);
    }

    private float GetAnimationLength(FirstPersonWeaponAnimationKey animationKey)
    {
        return _animationController == null ? 0f : Mathf.Max(0f, _animationController.GetNextAnimationLength(animationKey));
    }

    private float GetCurrentAnimationLength(FirstPersonWeaponAnimationKey animationKey)
    {
        return _animationController == null ? 0f : Mathf.Max(0f, _animationController.GetCurrentAnimationLength(animationKey));
    }

    private void UpdateWeaponRecoil()
    {
        if (_weaponData == null)
        {
            return;
        }

        _weaponRecoilService?.Tick(_weaponData.RecoilReturnSpeed, _weaponData.RecoilSnappiness);
    }

    private Transform FindWeaponRecoilTransform()
    {
        Transform current = transform;

        while (current != null)
        {
            if (IsWeaponRecoilTransform(current))
            {
                return current;
            }

            current = current.parent;
        }

        return null;
    }

    private FirstPersonCameraAllAnimationController FindCameraAllAnimationController()
    {
        Transform current = transform;

        while (current.parent != null)
        {
            current = current.parent;
        }

        return current.GetComponentInChildren<FirstPersonCameraAllAnimationController>(true);
    }

    private void ResetWeaponRecoil()
    {
        _weaponRecoilService?.Reset();
        _weaponRecoilService = null;
    }

    private void SetSprintBlockedByAim(bool blocked)
    {
        if (_sprintBlockedByAim == blocked)
        {
            return;
        }

        _sprintBlockedByAim = blocked;
        _playerController?.SetCanSprinting(blocked == false);
    }

    private void CancelReload()
    {
        if (_reloadCancellation == null)
        {
            return;
        }

        _reloadCancellation.Cancel();
        _reloadCancellation.Dispose();
        _reloadCancellation = null;
        _reloadAmmoData = null;
        _reloadAmmoApplied = false;
        _isReloading = false;
    }

    private void UpdateAmmoHud()
    {
        if (_ammoHudViewModel == null)
        {
            return;
        }

        if (_weaponData == null)
        {
            _ammoHudViewModel.Clear();
            return;
        }

        int inventoryAmmoAmount = _inventoryController == null || _requestedAmmoData == null ? 0 : _inventoryController.GetInventoryItemCount(_requestedAmmoData);
        _ammoHudViewModel.SetAmmo(_requestedAmmoData, _loadedAmmoAmount, inventoryAmmoAmount);
    }

    private bool IsMovementAnimationLocked => Time.time < _movementAnimationLockUntilTime;
    private bool IsWeaponInputLocked => Time.time < _weaponInputLockUntilTime;
    private static bool IsWeaponRecoilTransform(Transform target) => target != null && (target.name == WEAPON_RECOIL_OBJECT_NAME || target.name == WEAPON_RECOIL_OBJECT_NAME_COMPACT);
}
