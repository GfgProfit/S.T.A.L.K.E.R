using System;
using R3;
using TMPro;
using UnityEngine;

public sealed class QuickUseInventorySlotView : MonoBehaviour, IView<QuickUseInventorySlotViewModel>
{
    [SerializeField] private TMP_Text _keyText;

    private IDisposable _keyTextSubscription;

    private void OnDestroy()
    {
        Unbind();
    }

    public void Bind(QuickUseInventorySlotViewModel viewModel)
    {
        Unbind();

        if (viewModel == null)
        {
            return;
        }

        _keyTextSubscription = viewModel.KeyText.Subscribe(SetKeyText);
    }

    public void Unbind()
    {
        _keyTextSubscription?.Dispose();
        _keyTextSubscription = null;
    }

    public void BringKeyTextToFront()
    {
        if (_keyText == null)
        {
            return;
        }

        _keyText.rectTransform.SetAsLastSibling();
    }

    private void SetKeyText(string keyText)
    {
        if (_keyText == null)
        {
            return;
        }

        _keyText.text = keyText ?? string.Empty;
        BringKeyTextToFront();
    }
}
