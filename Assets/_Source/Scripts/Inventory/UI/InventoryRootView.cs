using System;
using R3;
using TMPro;
using UnityEngine;

internal sealed class InventoryRootView : IView<InventoryViewModel>, IDisposable
{
    private readonly GameObject _inventoryRoot;
    private readonly CanvasGroup _inventoryCanvasGroup;
    private readonly TMP_Text _weightText;
    private readonly GameProjectSettings _settings;
    private IDisposable _weightTextSubscription;
    private IDisposable _openStateSubscription;

    public InventoryRootView(GameObject inventoryRoot, CanvasGroup inventoryCanvasGroup, TMP_Text weightText, GameProjectSettings settings)
    {
        _inventoryRoot = inventoryRoot;
        _inventoryCanvasGroup = inventoryCanvasGroup;
        _weightText = weightText;
        _settings = settings == null ? GameProjectSettings.LoadDefault() : settings;
    }

    public void Bind(InventoryViewModel viewModel)
    {
        Unbind();

        if (viewModel == null)
        {
            return;
        }

        _weightTextSubscription = viewModel.WeightText.Subscribe(SetWeightText);
        _openStateSubscription = viewModel.IsOpen.Subscribe(ApplyInventoryOpenVisualState);
    }

    public void Unbind()
    {
        _weightTextSubscription?.Dispose();
        _openStateSubscription?.Dispose();
        _weightTextSubscription = null;
        _openStateSubscription = null;
    }

    public void Dispose()
    {
        Unbind();
    }

    private void ApplyInventoryOpenVisualState(bool isOpen)
    {
        ApplyInventoryRootState(isOpen);
        ApplyInventoryCanvasGroupState(isOpen);
    }

    private void ApplyInventoryRootState(bool isOpen)
    {
        if (_inventoryRoot != null && _inventoryCanvasGroup == null)
        {
            _inventoryRoot.SetActive(isOpen);
        }
        else if (_inventoryRoot != null && _inventoryRoot.activeSelf == false)
        {
            _inventoryRoot.SetActive(true);
        }
    }

    private void ApplyInventoryCanvasGroupState(bool isOpen)
    {
        if (_inventoryCanvasGroup == null)
        {
            return;
        }

        _inventoryCanvasGroup.alpha = isOpen ? 1f : 0f;
        _inventoryCanvasGroup.interactable = isOpen;
        _inventoryCanvasGroup.blocksRaycasts = isOpen;
    }

    private void SetWeightText(string weightText)
    {
        if (_weightText == null)
        {
            return;
        }

        _weightText.raycastTarget = false;
        _weightText.richText = true;
        _weightText.color = _settings.NormalWeightColor;
        _weightText.text = weightText;
    }
}
