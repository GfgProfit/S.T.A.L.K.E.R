using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

public sealed class FirstPersonWeaponRuntimeController : MonoBehaviour
{
    private const float MOVEMENT_INPUT_THRESHOLD = 0.01f;
    private const float DEFAULT_LOOP_ANIMATION_SPEED = 1f;
    private const float CROUCH_WALK_ANIMATION_SPEED = 0.5f;
    private const float MINUTES_PER_DEGREE = 60f;
    private const string WEAPON_RECOIL_OBJECT_NAME = "Weapon Recoil";
    private const string WEAPON_RECOIL_OBJECT_NAME_COMPACT = "WeaponRecoil";
    private const string MUZZLE_OBJECT_NAME = "Muzzle";

    [SerializeField] private Transform _muzzle;
    [SerializeField] private WeaponShellEjector _shellEjector;
    [SerializeField] private Renderer _ammoRenderer;

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
    private Material _defaultAmmoMaterial;
    private CancellationTokenSource _reloadCancellation;
    private CancellationTokenSource _jamClearingCancellation;
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
    private bool _isJammed;
    private bool _isClearingJam;
    private bool _jammedOnLastRound;
    private bool _jammedAmmoRemoved;
    private bool _isAiming;
    private bool _sprintBlockedByAim;
    private bool _reloadAmmoApplied;
    private bool _ballisticConfigurationErrorLogged;

    public ItemData RequestedAmmoData => _requestedAmmoData;
    public ItemData LoadedAmmoData => _loadedAmmoData;
    public int LoadedAmmoAmount => _loadedAmmoAmount;
    public bool IsReloading => _isReloading;
    public bool IsJammed => _isJammed;
    public bool IsClearingJam => _isClearingJam;
    public bool IsAiming => _isAiming;
    private int MagazineCapacity => WeaponModuleSupport.GetMagazineCapacity(_weaponData == null ? 1 : _weaponData.MagazineCapacity, _weaponItem == null ? null : _weaponItem.InstalledModules);

    public void Initialize(InventoryItem weaponItem, InventoryController inventoryController, IPlayerInput playerInput, FirstPersonWeaponAmmoHudViewModel ammoHudViewModel)
    {
        CancelReload();
        CancelJamClearing();
        SetSprintBlockedByAim(false);

        _weaponItem = weaponItem;
        _weaponItemData = weaponItem == null ? null : weaponItem.ItemData;
        _weaponData = _weaponItemData == null ? null : _weaponItemData.WeaponData;
        _inventoryController = inventoryController;
        _playerInput = playerInput;
        _ammoHudViewModel = ammoHudViewModel;
        _animationController = GetComponent<FirstPersonWeaponController>();
        _muzzle = FindMuzzleTransform();
        _defaultAmmoMaterial ??= _ammoRenderer == null ? null : _ammoRenderer.sharedMaterial;
        _cameraAllAnimationController = FindCameraAllAnimationController();
        _cameraAllAnimationController?.SetAimActive(false);
        _animationController?.SetAimRootPositionOffsetActive(false, true);
        _playerController = GetComponentInParent<PlayerController>();
        _weaponRecoilService?.Reset();
        _weaponRecoilService = _weaponData == null ? null : new WeaponRecoilService(FindWeaponRecoilTransform());
        RestoreMagazineState();
        ApplyAmmoMaterial(_loadedAmmoData);
        _reloadAmmoData = null;
        _movementAnimationState = WeaponMovementAnimationState.None;
        _movementAnimationLockUntilTime = 0f;
        _weaponInputLockUntilTime = 0f;
        _nextShootTime = 0f;
        _isReloading = false;
        _isJammed = _weaponItem != null && _weaponItem.WeaponMagazineState.IsJammed && _loadedAmmoAmount > 0;
        _isClearingJam = false;
        _jammedOnLastRound = _isJammed && _loadedAmmoAmount == 1;
        _jammedAmmoRemoved = false;
        _isAiming = false;
        _reloadAmmoApplied = false;
        _ballisticConfigurationErrorLogged = false;
        _initialized = _weaponData != null;

        WeaponCondition initialCondition = _isJammed
            ? WeaponCondition.Jammed
            : _loadedAmmoAmount > 0 ? WeaponCondition.Normal : WeaponCondition.Empty;
        _animationController?.SetCondition(initialCondition);
        SyncMagazineState();
        UpdateAmmoHud();
    }

    private void OnDestroy()
    {
        SetSprintBlockedByAim(false);
        _cameraAllAnimationController?.SetAimActive(false);
        _animationController?.SetAimRootPositionOffsetActive(false, true);
        CancelReload();
        CancelJamClearing();
        ResetWeaponRecoil();
    }

    private void OnDisable()
    {
        SetSprintBlockedByAim(false);
        _cameraAllAnimationController?.SetAimActive(false);
        _animationController?.SetAimRootPositionOffsetActive(false, true);
        CancelReload();
        CancelJamClearing();
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

        if (_isReloading || _isClearingJam)
        {
            UpdateAmmoHud();
            return;
        }

        if (IsWeaponInputLocked)
        {
            UpdateAmmoHud();
            return;
        }

        if (_isJammed)
        {
            bool jammedSprintInputActive = IsSprintInputActive();
            UpdateAimState(jammedSprintInputActive);

            if (_playerInput != null && _playerInput.IsWeaponShootPressed())
            {
                ShowJammedActionText();
            }

            if (_playerInput != null && _playerInput.IsWeaponReloadPressed() && TryClearJam())
            {
                UpdateAmmoHud();
                return;
            }

            if (IsMovementAnimationLocked == false)
            {
                UpdateMovementAnimation();
            }

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

        if (isSprintInputActive == false && IsShootInputActive() && TryHandleShootInput())
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
        if (_weaponData == null || _isReloading || _isJammed || _isClearingJam || IsSprintInputActive() || Time.time < _nextShootTime)
        {
            return false;
        }

        if (_loadedAmmoAmount <= 0)
        {
            PlayDryEmptyAnimation();
            return true;
        }

        ItemData firedAmmoData = _loadedAmmoData;

        if (TrySpawnProjectile(firedAmmoData) == false)
        {
            return false;
        }

        bool isLastRound = _loadedAmmoAmount == 1;
        _nextShootTime = Time.time + _weaponData.SecondsBetweenShots;
        _loadedAmmoAmount--;
        ApplyDurabilityShotCost(firedAmmoData);
        _shellEjector?.Eject(firedAmmoData == null ? null : firedAmmoData.AmmoMaterial);

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

        float recoilMultiplier = GetRecoilMultiplier(firedAmmoData);
        _weaponRecoilService?.RecoilShoot(_weaponData.RecoilX * recoilMultiplier, _weaponData.RecoilY * recoilMultiplier, _weaponData.RecoilZ * recoilMultiplier);
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

    private bool TryHandleShootInput()
    {
        if (_weaponData == null || _isReloading || _isJammed || _isClearingJam || IsSprintInputActive() || Time.time < _nextShootTime)
        {
            return false;
        }

        if (_loadedAmmoAmount > 0 && TryJamWeapon())
        {
            return true;
        }

        return TryShoot();
    }

    private bool TryJamWeapon()
    {
        if (_weaponItem == null || _weaponItem.HasDurability == false)
        {
            return false;
        }

        float jammedChancePercent = _weaponData.GetJammedChancePercent(_weaponItem.CurrentDurabilityPercent);

        if (jammedChancePercent <= 0f || UnityEngine.Random.value * 100f >= jammedChancePercent)
        {
            return false;
        }

        EnterJammedState();
        return true;
    }

    private void EnterJammedState()
    {
        bool jammedWhileAiming = _isAiming;
        _isJammed = true;
        _isClearingJam = false;
        _jammedOnLastRound = _loadedAmmoAmount == 1;
        _jammedAmmoRemoved = false;
        _movementAnimationState = WeaponMovementAnimationState.None;
        _animationController?.SetConditionInstant(WeaponCondition.Jammed);
        float animationLength = _animationController == null ? 0f : _animationController.PlayDryEmpty(jammedWhileAiming, true);
        _nextShootTime = Time.time + Mathf.Max(_weaponData.SecondsBetweenShots, animationLength);
        LockMovementAnimation(jammedWhileAiming ? FirstPersonWeaponAnimationKey.AimDry : FirstPersonWeaponAnimationKey.DryEmpty);
        SyncMagazineState();
        ShowJammedActionText();
    }

    private void ShowJammedActionText()
    {
        if (_inventoryController == null || _playerInput == null)
        {
            return;
        }

        string reloadKey = _playerInput.WeaponReloadKeyDisplayName;
        string actionColor = GameProjectSettings.LoadDefault().ActionColorHtml;
        _inventoryController.ShowMiniActionText($"Оружие заклинило! Нажмите [<color={actionColor}>{reloadKey}</color>] что бы устранить неполадку.");
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
        if (_weaponData == null || _inventoryController == null || _requestedAmmoData == null || _isReloading || _isJammed || _isClearingJam || _isAiming || IsSprintInputActive())
        {
            return false;
        }

        if (_inventoryController.GetInventoryItemCount(_requestedAmmoData) <= 0)
        {
            return false;
        }

        if (_loadedAmmoData == _requestedAmmoData && _loadedAmmoAmount >= MagazineCapacity)
        {
            return false;
        }

        bool isFullReload = _loadedAmmoAmount <= 0;
        PlayReloadAnimation(isFullReload, _requestedAmmoData);
        UpdateAmmoHud();
        return true;
    }

    public bool TryClearJam()
    {
        if (_weaponData == null || _isJammed == false || _isClearingJam || _isAiming || _loadedAmmoAmount <= 0)
        {
            return false;
        }

        FirstPersonWeaponAnimationKey animationKey = _jammedOnLastRound ? FirstPersonWeaponAnimationKey.RevivalLast : FirstPersonWeaponAnimationKey.Revival;
        float ammoRemovalDelay = GetJammedAmmoRemovalDelay(animationKey);
        float animationLength = GetAnimationLength(animationKey);

        _isClearingJam = true;
        _movementAnimationState = WeaponMovementAnimationState.None;
        _animationController?.PlayRevival(_jammedOnLastRound);
        LockMovementAnimation(animationKey);

        _jamClearingCancellation = CancellationTokenSource.CreateLinkedTokenSource(destroyCancellationToken);
        ClearJamAsync(ammoRemovalDelay, animationLength, _jamClearingCancellation).Forget(Debug.LogException);
        return true;
    }

    public bool TryChangeAmmoType()
    {
        if (_weaponData == null || _inventoryController == null || _isJammed || _isClearingJam)
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
        if (_weaponData == null || _isReloading || _isJammed || _isClearingJam || _loadedAmmoData == null || _loadedAmmoAmount <= 0)
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
        float ammoApplyDelay = GetReloadAmmoApplyDelay(isFullReload, animationKey);
        float materialApplyDelay = isFullReload ? 0f : GetReloadAmmoMaterialApplyDelay(animationKey);
        float animationLength = GetAnimationLength(animationKey);

        CancelReload();
        _reloadAmmoData = reloadAmmoData;
        _reloadAmmoApplied = false;
        _isReloading = true;
        _movementAnimationState = WeaponMovementAnimationState.None;

        if (isFullReload)
        {
            ApplyReloadAmmoMaterial();
        }

        _animationController?.Reload(isFullReload);
        LockMovementAnimation(animationKey);

        _reloadCancellation = CancellationTokenSource.CreateLinkedTokenSource(destroyCancellationToken);
        PlayReloadAsync(isFullReload, ammoApplyDelay, materialApplyDelay, animationLength, _reloadCancellation).Forget(Debug.LogException);
    }

    private async UniTask PlayReloadAsync(bool isFullReload, float ammoApplyDelay, float materialApplyDelay, float animationLength, CancellationTokenSource reloadCancellation)
    {
        CancellationToken cancellationToken = reloadCancellation.Token;
        float lastEventDelay;

        if (isFullReload)
        {
            if (await DelaySecondsAsync(ammoApplyDelay, cancellationToken))
            {
                return;
            }

            ApplyReloadAmmo();
            lastEventDelay = ammoApplyDelay;
        }
        else if (materialApplyDelay <= ammoApplyDelay)
        {
            if (await DelaySecondsAsync(materialApplyDelay, cancellationToken))
            {
                return;
            }

            ApplyReloadAmmoMaterial();

            if (await DelaySecondsAsync(ammoApplyDelay - materialApplyDelay, cancellationToken))
            {
                return;
            }

            ApplyReloadAmmo();
            lastEventDelay = ammoApplyDelay;
        }
        else
        {
            if (await DelaySecondsAsync(ammoApplyDelay, cancellationToken))
            {
                return;
            }

            ApplyReloadAmmo();

            if (await DelaySecondsAsync(materialApplyDelay - ammoApplyDelay, cancellationToken))
            {
                return;
            }

            ApplyReloadAmmoMaterial();
            lastEventDelay = materialApplyDelay;
        }

        float remainingDelay = Mathf.Max(0f, animationLength - lastEventDelay);

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

    private async UniTask ClearJamAsync(float ammoRemovalDelay, float animationLength, CancellationTokenSource jamClearingCancellation)
    {
        CancellationToken cancellationToken = jamClearingCancellation.Token;

        if (await DelaySecondsAsync(ammoRemovalDelay, cancellationToken))
        {
            return;
        }

        RemoveJammedAmmo();

        float remainingDelay = Mathf.Max(0f, animationLength - ammoRemovalDelay);

        if (await DelaySecondsAsync(remainingDelay, cancellationToken))
        {
            return;
        }

        if (_jamClearingCancellation != jamClearingCancellation)
        {
            return;
        }

        _isClearingJam = false;
        _isJammed = false;
        _jammedOnLastRound = false;
        _jammedAmmoRemoved = false;
        SyncMagazineState();
        _animationController?.SetCondition(_loadedAmmoAmount > 0 ? WeaponCondition.Normal : WeaponCondition.Empty);

        _jamClearingCancellation = null;
        jamClearingCancellation.Dispose();
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
        int targetAmmoAmount = Mathf.Min(MagazineCapacity, availableAmmoAmount);
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

    private void RemoveJammedAmmo()
    {
        if (_isJammed == false || _jammedAmmoRemoved || _loadedAmmoAmount <= 0)
        {
            return;
        }

        _loadedAmmoAmount--;

        if (_loadedAmmoAmount <= 0)
        {
            _loadedAmmoAmount = 0;
            _loadedAmmoData = null;
        }

        _jammedAmmoRemoved = true;
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

    private void ApplyReloadAmmoMaterial()
    {
        if (_reloadAmmoData == null)
        {
            return;
        }

        if (_reloadAmmoApplied == false && (_inventoryController == null || _inventoryController.GetInventoryItemCount(_reloadAmmoData) <= 0))
        {
            return;
        }

        ApplyAmmoMaterial(_reloadAmmoData);
    }

    private void ApplyAmmoMaterial(ItemData ammoData)
    {
        Material ammoMaterial = ammoData == null || ammoData.AmmoMaterial == null ? _defaultAmmoMaterial : ammoData.AmmoMaterial;

        if (_ammoRenderer == null || _ammoRenderer.sharedMaterial == ammoMaterial)
        {
            return;
        }

        _ammoRenderer.sharedMaterial = ammoMaterial;
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

        _loadedAmmoAmount = Mathf.Clamp(magazineState.LoadedAmmoAmount, 0, MagazineCapacity);

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
        magazineState.SetJammed(_isJammed);
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

    private void ApplyDurabilityShotCost(ItemData ammoData)
    {
        if (_weaponItem == null || _weaponItem.HasDurability == false || _weaponData == null)
        {
            return;
        }

        float durabilityLossPercentModifier = ammoData == null ? 0f : ammoData.AmmoWeaponDurabilityLossPercentModifier;
        durabilityLossPercentModifier += WeaponModuleSupport.GetDurabilityLossPercentModifier(_weaponItem.InstalledModules);
        float durabilityLossMultiplier = GetPercentModifierMultiplier(durabilityLossPercentModifier);
        float durabilityLoss = _weaponData.DurabilityPercentPerShot * durabilityLossMultiplier;
        float nextDurability = _weaponItem.CurrentDurabilityPercent - durabilityLoss;
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

    private float GetReloadAmmoMaterialApplyDelay(FirstPersonWeaponAnimationKey animationKey)
    {
        if (_weaponData == null || _animationController == null)
        {
            return 0f;
        }

        return _animationController.GetNextAnimationFrameTime(animationKey, _weaponData.GetReloadAmmoMaterialApplyFrame());
    }

    private float GetJammedAmmoRemovalDelay(FirstPersonWeaponAnimationKey animationKey)
    {
        if (_weaponData == null || _animationController == null)
        {
            return 0f;
        }

        return _animationController.GetNextAnimationFrameTime(animationKey, _weaponData.GetJammedAmmoRemovalFrame());
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

    private float GetRecoilMultiplier(ItemData ammoData)
    {
        float recoilPercentModifier = ammoData == null ? 0f : ammoData.AmmoWeaponRecoilPercentModifier;
        recoilPercentModifier += WeaponModuleSupport.GetRecoilPercentModifier(_weaponItem == null ? null : _weaponItem.InstalledModules);

        if (_playerController != null && _playerController.IsCrouching)
        {
            recoilPercentModifier -= _weaponData.CrouchRecoilReductionPercent;
        }

        return GetPercentModifierMultiplier(recoilPercentModifier);
    }

    private static float GetPercentModifierMultiplier(float percentModifier) => Mathf.Max(0f, 1f + percentModifier * 0.01f);

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

    private Transform FindMuzzleTransform()
    {
        if (_muzzle != null)
        {
            return _muzzle;
        }

        Transform[] childTransforms = GetComponentsInChildren<Transform>(true);

        for (int i = 0; i < childTransforms.Length; i++)
        {
            if (childTransforms[i].name == MUZZLE_OBJECT_NAME)
            {
                return childTransforms[i];
            }
        }

        return null;
    }

    private bool TrySpawnProjectile(ItemData ammoData)
    {
        _muzzle = FindMuzzleTransform();

        if (_muzzle == null)
        {
            return ReportBallisticConfigurationError($"Runtime weapon requires a {MUZZLE_OBJECT_NAME} transform at the barrel end.");
        }

        if (_weaponData.BulletPrefab == null)
        {
            return ReportBallisticConfigurationError($"{_weaponData.name} has no bullet prefab assigned.");
        }

        float bulletVelocity = _weaponData.GetBulletVelocityMetersPerSecond(ammoData);

        if (bulletVelocity <= 0f)
        {
            return ReportBallisticConfigurationError($"{ammoData?.name ?? "Loaded ammo"} has no bullet velocity for {_weaponData.name}.");
        }

        Vector3 launchDirection = GetBallisticLaunchDirection();
        GameObject projectileObject = Instantiate(_weaponData.BulletPrefab, _muzzle.position, _muzzle.rotation);
        BallisticProjectile projectile = projectileObject.GetComponent<BallisticProjectile>();

        if (projectile == null)
        {
            projectile = projectileObject.AddComponent<BallisticProjectile>();
        }

        projectile.enabled = true;
        projectile.Initialize(
            _weaponData,
            ammoData,
            _muzzle.position,
            launchDirection,
            bulletVelocity,
            ResolveBallisticOwnerRoot(),
            transform);
        return true;
    }

    private Vector3 GetBallisticLaunchDirection()
    {
        float dispersionDiameterMinutes = WeaponModuleSupport.GetAccuracyMinutesOfAngle(_weaponData, _weaponItem == null ? null : _weaponItem.InstalledModules);

        if (dispersionDiameterMinutes <= 0f)
        {
            return _muzzle.forward;
        }

        float dispersionRadiusRadians = dispersionDiameterMinutes * 0.5f / MINUTES_PER_DEGREE * Mathf.Deg2Rad;
        float dispersionRadiusTangent = Mathf.Tan(dispersionRadiusRadians);
        Vector2 dispersion = UnityEngine.Random.insideUnitCircle * dispersionRadiusTangent;

        return (_muzzle.forward + _muzzle.right * dispersion.x + _muzzle.up * dispersion.y).normalized;
    }

    private bool ReportBallisticConfigurationError(string message)
    {
        if (_ballisticConfigurationErrorLogged == false)
        {
            Debug.LogError($"[{nameof(FirstPersonWeaponRuntimeController)}] {message}", this);
            _ballisticConfigurationErrorLogged = true;
        }

        return false;
    }

    private Transform ResolveBallisticOwnerRoot()
    {
        if (_playerController != null)
        {
            return _playerController.transform;
        }

        FirstPersonWeaponHolderController weaponHolder = GetComponentInParent<FirstPersonWeaponHolderController>();
        return weaponHolder == null ? null : weaponHolder.transform;
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

    private void CancelJamClearing()
    {
        if (_jamClearingCancellation != null)
        {
            _jamClearingCancellation.Cancel();
            _jamClearingCancellation.Dispose();
            _jamClearingCancellation = null;
        }

        _isClearingJam = false;
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
