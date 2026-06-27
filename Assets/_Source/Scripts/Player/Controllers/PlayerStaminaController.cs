using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(PlayerController))]
public class PlayerStaminaController : MonoBehaviour, IPlayerStaminaSettings
{
    [SerializeField] private PlayerController _playerController;
    [SerializeField] private InventoryController _inventoryController;
    [SerializeField] private Image _fillImage;

    [Header("View")]
    [SerializeField] private Gradient _fillColorGradient = CreateDefaultFillColorGradient();
    [SerializeField] [Min(0f)] private float _fillColorTweenDuration = 0.15f;

    [Header("Stamina")]
    [SerializeField] [Min(0f)] private float _maxStamina = 100f;
    [SerializeField] [Min(0f)] private float _staminaDrainWeightThreshold = 15f;
    [SerializeField] [Min(0f)] private float _staminaDrainPerSecondAtThreshold = 4f;
    [SerializeField] [Min(0f)] private float _staminaDrainPerSecondAtMaxWeight = 12f;
    [SerializeField] [Min(0f)] private float _staminaRecoveryPerSecond = 10f;
    [SerializeField] [Range(0f, 1f)] private float _walkingRecoveryMultiplier = 0.5f;
    [SerializeField] [Range(0f, 1f)] private float _jumpStaminaCostPercent = 0.1f;
    [SerializeField] [Min(0f)] private float _sprintResumeStamina = 10f;
    [SerializeField] [Range(0f, 1f)] private float _lowStaminaWalkSpeedMultiplier = 0.5f;
    [SerializeField] [Min(0f)] private float _extraWeightWalkingDrainPerSecond = 4f;
    [SerializeField] [Min(0f)] private float _extraWeightDrainMultiplier = 3f;
    [SerializeField] [Range(0f, 1f)] private float _walkingRecoveryMaxWeightPercent = 0.7f;

    private readonly PlayerStaminaService _staminaService = new();
    private PlayerStaminaViewModel _viewModel;
    private PlayerStaminaView _view;
    private CancellationTokenSource _tickCancellation;
    private bool _isSubscribedToPlayerEvents;

    public float CurrentStamina => _staminaService.CurrentStamina;
    public float MaxStamina => _staminaService.MaxStamina;
    public float NormalizedStamina => _staminaService.GetNormalizedStamina(this);
    float IPlayerStaminaSettings.MaxStamina => Mathf.Max(0f, _maxStamina);
    float IPlayerStaminaSettings.StaminaDrainWeightThreshold => Mathf.Max(0f, _staminaDrainWeightThreshold);
    float IPlayerStaminaSettings.StaminaDrainPerSecondAtThreshold => Mathf.Max(0f, _staminaDrainPerSecondAtThreshold);
    float IPlayerStaminaSettings.StaminaDrainPerSecondAtMaxWeight => Mathf.Max(0f, _staminaDrainPerSecondAtMaxWeight);
    float IPlayerStaminaSettings.StaminaRecoveryPerSecond => Mathf.Max(0f, _staminaRecoveryPerSecond);
    float IPlayerStaminaSettings.WalkingRecoveryMultiplier => Mathf.Clamp01(_walkingRecoveryMultiplier);
    float IPlayerStaminaSettings.JumpStaminaCostPercent => Mathf.Clamp01(_jumpStaminaCostPercent);
    float IPlayerStaminaSettings.SprintResumeStamina => Mathf.Max(0f, _sprintResumeStamina);
    float IPlayerStaminaSettings.LowStaminaWalkSpeedMultiplier => Mathf.Clamp01(_lowStaminaWalkSpeedMultiplier);
    float IPlayerStaminaSettings.ExtraWeightWalkingDrainPerSecond => Mathf.Max(0f, _extraWeightWalkingDrainPerSecond);
    float IPlayerStaminaSettings.ExtraWeightDrainMultiplier => Mathf.Max(0f, _extraWeightDrainMultiplier);
    float IPlayerStaminaSettings.WalkingRecoveryMaxWeightPercent => Mathf.Clamp01(_walkingRecoveryMaxWeightPercent);

    private void Awake()
    {
        ResolveReferences();
        _viewModel = new PlayerStaminaViewModel();
        _viewModel.SetFillColor(CalculateFillColor(1f));
        _view = new PlayerStaminaView(_fillImage, _fillColorTweenDuration);
        _view.Bind(_viewModel);
        _staminaService.Reset(this);
        PublishState();
        ApplyPlayerState();
    }

    private void OnEnable()
    {
        ResolveReferences();
        SubscribeToPlayerEvents();
        StartTickLoop();
        ApplyPlayerState();
    }

    private void OnDisable()
    {
        StopTickLoop();
        UnsubscribeFromPlayerEvents();
        _staminaService.ClearSprintBlock();
        _playerController?.SetSprintBlocked(PlayerSprintBlockSource.Stamina, false);
        _playerController?.SetCanJumping(true);
        _playerController?.SetWalkSpeedMultiplier(1f);
    }

    private void OnDestroy()
    {
        StopTickLoop();
        UnsubscribeFromPlayerEvents();
        _view?.Dispose();
        _viewModel?.Dispose();
    }

    private void OnValidate()
    {
        _maxStamina = Mathf.Max(0f, _maxStamina);
        _staminaDrainWeightThreshold = Mathf.Max(0f, _staminaDrainWeightThreshold);
        _staminaDrainPerSecondAtThreshold = Mathf.Max(0f, _staminaDrainPerSecondAtThreshold);
        _staminaDrainPerSecondAtMaxWeight = Mathf.Max(0f, _staminaDrainPerSecondAtMaxWeight);
        _staminaRecoveryPerSecond = Mathf.Max(0f, _staminaRecoveryPerSecond);
        _sprintResumeStamina = Mathf.Max(0f, _sprintResumeStamina);
        _extraWeightWalkingDrainPerSecond = Mathf.Max(0f, _extraWeightWalkingDrainPerSecond);
        _extraWeightDrainMultiplier = Mathf.Max(0f, _extraWeightDrainMultiplier);
        _fillColorTweenDuration = Mathf.Max(0f, _fillColorTweenDuration);
        _fillColorGradient ??= CreateDefaultFillColorGradient();
    }

    private void ResolveReferences()
    {
        if (_playerController == null)
        {
            _playerController = GetComponent<PlayerController>();
        }

        if (_inventoryController == null)
        {
            _inventoryController = GetComponent<InventoryController>();
        }
    }

    private void StartTickLoop()
    {
        StopTickLoop();
        _tickCancellation = new CancellationTokenSource();
        RunTickLoopAsync(_tickCancellation.Token).Forget(Debug.LogException);
    }

    private void StopTickLoop()
    {
        if (_tickCancellation == null)
        {
            return;
        }

        _tickCancellation.Cancel();
        _tickCancellation.Dispose();
        _tickCancellation = null;
    }

    private async UniTask RunTickLoopAsync(CancellationToken cancellationToken)
    {
        try
        {
            while (cancellationToken.IsCancellationRequested == false)
            {
                await UniTask.Yield(PlayerLoopTiming.LastUpdate, cancellationToken);
                Tick(Time.deltaTime);
            }
        }
        catch (OperationCanceledException)
        {
        }
    }

    private void Tick(float deltaTime)
    {
        ResolveReferences();

        if (_playerController == null || _inventoryController == null)
        {
            return;
        }

        _staminaService.Tick(this, CreateTickContext(), deltaTime);
        PublishState();
        ApplyPlayerState();
    }

    private PlayerStaminaTickContext CreateTickContext()
    {
        return new(_inventoryController.CurrentCarryWeight, _inventoryController.MaxCarryWeight, _inventoryController.MovementBlockWeight, _inventoryController.IsMovementBlockedByWeight, _playerController.IsSprinting, _playerController.IsWalking);
    }

    private void SubscribeToPlayerEvents()
    {
        if (_isSubscribedToPlayerEvents || _playerController == null)
        {
            return;
        }

        _playerController.Jumped += HandleJumped;
        _isSubscribedToPlayerEvents = true;
    }

    private void UnsubscribeFromPlayerEvents()
    {
        if (_isSubscribedToPlayerEvents == false || _playerController == null)
        {
            return;
        }

        _playerController.Jumped -= HandleJumped;
        _isSubscribedToPlayerEvents = false;
    }

    private void HandleJumped()
    {
        _staminaService.ApplyJumpCost(this);
        PublishState();
        ApplyPlayerState();
    }

    private void PublishState()
    {
        float normalizedStamina = _staminaService.GetNormalizedStamina(this);
        _viewModel?.SetNormalizedStamina(normalizedStamina);
        _viewModel?.SetFillColor(CalculateFillColor(normalizedStamina));
    }

    private Color CalculateFillColor(float normalizedStamina)
    {
        return _fillColorGradient == null ? Color.white : _fillColorGradient.Evaluate(Mathf.Clamp01(normalizedStamina));
    }

    private static Gradient CreateDefaultFillColorGradient()
    {
        Gradient gradient = new();
        GradientColorKey[] colorKeys =
        {
            new(new Color(1f, 0.1f, 0.05f, 1f), 0f),
            new(new Color(1f, 0.1f, 0.05f, 1f), 0.1f),
            new(new Color(0f, 0.5461459f, 1f, 1f), 0.3f),
            new(new Color(0f, 0.5461459f, 1f, 1f), 1f)
        };
        GradientAlphaKey[] alphaKeys =
        {
            new(1f, 0f),
            new(1f, 1f)
        };

        gradient.SetKeys(colorKeys, alphaKeys);
        return gradient;
    }

    private void ApplyPlayerState()
    {
        if (_playerController == null)
        {
            return;
        }

        _playerController.SetSprintBlocked(PlayerSprintBlockSource.Stamina, _staminaService.IsSprintBlockedByStamina);
        _playerController.SetWalkSpeedMultiplier(_staminaService.GetWalkSpeedMultiplier(this));
        _playerController.SetCanJumping(_staminaService.CanJump(this));
    }
}
