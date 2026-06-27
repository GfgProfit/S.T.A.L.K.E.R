using UnityEngine;

internal interface IPlayerStaminaSettings
{
    float MaxStamina { get; }
    float StaminaDrainWeightThreshold { get; }
    float StaminaDrainPerSecondAtThreshold { get; }
    float StaminaDrainPerSecondAtMaxWeight { get; }
    float StaminaRecoveryPerSecond { get; }
    float WalkingRecoveryMultiplier { get; }
    float JumpStaminaCostPercent { get; }
    float SprintResumeStamina { get; }
    float LowStaminaWalkSpeedMultiplier { get; }
    float ExtraWeightWalkingDrainPerSecond { get; }
    float ExtraWeightDrainMultiplier { get; }
    float WalkingRecoveryMaxWeightPercent { get; }
}

internal readonly struct PlayerStaminaTickContext
{
    public PlayerStaminaTickContext(float currentWeight, float maxCarryWeight, float movementBlockWeight, bool isMovementBlockedByWeight, bool isSprinting, bool isWalking)
    {
        CurrentWeight = currentWeight;
        MaxCarryWeight = maxCarryWeight;
        MovementBlockWeight = movementBlockWeight;
        IsMovementBlockedByWeight = isMovementBlockedByWeight;
        IsSprinting = isSprinting;
        IsWalking = isWalking;
    }

    public float CurrentWeight { get; }
    public float MaxCarryWeight { get; }
    public float MovementBlockWeight { get; }
    public bool IsMovementBlockedByWeight { get; }
    public bool IsSprinting { get; }
    public bool IsWalking { get; }
}

internal sealed class PlayerStaminaService
{
    private float _currentStamina;

    public float CurrentStamina => _currentStamina;
    public float MaxStamina { get; private set; }
    public bool IsSprintBlockedByStamina { get; private set; }

    public void Reset(IPlayerStaminaSettings settings)
    {
        MaxStamina = settings.MaxStamina;
        _currentStamina = MaxStamina;
        RefreshSprintBlockState(settings);
    }

    public void Tick(IPlayerStaminaSettings settings, PlayerStaminaTickContext context, float deltaTime)
    {
        MaxStamina = settings.MaxStamina;

        if (MaxStamina <= 0f)
        {
            _currentStamina = 0f;
            RefreshSprintBlockState(settings);
            return;
        }

        if (ShouldDrainStamina(settings, context))
        {
            _currentStamina -= CalculateDrainPerSecond(settings, context) * deltaTime;
        }
        else if (CanRecoverStamina(settings, context))
        {
            _currentStamina += CalculateRecoveryPerSecond(settings, context) * deltaTime;
        }

        _currentStamina = Mathf.Clamp(_currentStamina, 0f, MaxStamina);
        RefreshSprintBlockState(settings);
    }

    public void ApplyJumpCost(IPlayerStaminaSettings settings)
    {
        MaxStamina = settings.MaxStamina;
        _currentStamina = Mathf.Clamp(_currentStamina - GetJumpStaminaCost(settings), 0f, MaxStamina);
        RefreshSprintBlockState(settings);
    }

    public void ClearSprintBlock()
    {
        IsSprintBlockedByStamina = false;
    }

    public bool CanJump(IPlayerStaminaSettings settings) => settings.MaxStamina > 0f && _currentStamina >= GetJumpStaminaCost(settings);
    public float GetNormalizedStamina(IPlayerStaminaSettings settings) => settings.MaxStamina <= 0f ? 0f : Mathf.Clamp01(_currentStamina / settings.MaxStamina);
    public float GetWalkSpeedMultiplier(IPlayerStaminaSettings settings) => _currentStamina <= settings.SprintResumeStamina ? settings.LowStaminaWalkSpeedMultiplier : 1f;

    private bool ShouldDrainStamina(IPlayerStaminaSettings settings, PlayerStaminaTickContext context)
    {
        if (context.CurrentWeight < settings.StaminaDrainWeightThreshold || context.IsMovementBlockedByWeight)
        {
            return false;
        }

        if (context.IsSprinting)
        {
            return true;
        }

        return context.IsWalking && IsInExtraWeightRange(context);
    }

    private bool CanRecoverStamina(IPlayerStaminaSettings settings, PlayerStaminaTickContext context)
    {
        if (_currentStamina >= settings.MaxStamina || context.IsMovementBlockedByWeight)
        {
            return false;
        }

        if (context.IsWalking == false)
        {
            return true;
        }

        float maxWeight = Mathf.Max(0f, context.MaxCarryWeight);
        return maxWeight <= 0f || context.CurrentWeight <= maxWeight * settings.WalkingRecoveryMaxWeightPercent;
    }

    private float CalculateDrainPerSecond(IPlayerStaminaSettings settings, PlayerStaminaTickContext context)
    {
        if (context.IsSprinting == false && context.IsWalking && IsInExtraWeightRange(context))
        {
            return settings.ExtraWeightWalkingDrainPerSecond;
        }

        float maxWeight = Mathf.Max(context.MaxCarryWeight, settings.StaminaDrainWeightThreshold);
        float weightProgress = Mathf.Approximately(maxWeight, settings.StaminaDrainWeightThreshold) ? 1f : Mathf.InverseLerp(settings.StaminaDrainWeightThreshold, maxWeight, context.CurrentWeight);
        float drainPerSecond = Mathf.Lerp(settings.StaminaDrainPerSecondAtThreshold, settings.StaminaDrainPerSecondAtMaxWeight, weightProgress);

        if (IsInExtraWeightRange(context))
        {
            drainPerSecond *= settings.ExtraWeightDrainMultiplier;
        }

        return Mathf.Max(0f, drainPerSecond);
    }

    private float CalculateRecoveryPerSecond(IPlayerStaminaSettings settings, PlayerStaminaTickContext context)
    {
        float movementBlockWeight = Mathf.Max(0f, context.MovementBlockWeight);

        if (movementBlockWeight <= 0f)
        {
            return settings.StaminaRecoveryPerSecond;
        }

        float weightMultiplier = Mathf.Clamp01(1f - context.CurrentWeight / movementBlockWeight);
        float movementMultiplier = context.IsWalking ? settings.WalkingRecoveryMultiplier : 1f;
        return settings.StaminaRecoveryPerSecond * weightMultiplier * movementMultiplier;
    }

    private void RefreshSprintBlockState(IPlayerStaminaSettings settings)
    {
        if (settings.MaxStamina <= 0f || _currentStamina <= 0f)
        {
            IsSprintBlockedByStamina = true;
        }
        else if (IsSprintBlockedByStamina && _currentStamina >= Mathf.Min(settings.MaxStamina, settings.SprintResumeStamina))
        {
            IsSprintBlockedByStamina = false;
        }
    }

    private float GetJumpStaminaCost(IPlayerStaminaSettings settings) => settings.MaxStamina * settings.JumpStaminaCostPercent;
    private static bool IsInExtraWeightRange(PlayerStaminaTickContext context) => context.MaxCarryWeight > 0f && context.CurrentWeight >= context.MaxCarryWeight && context.CurrentWeight < context.MovementBlockWeight;
}
