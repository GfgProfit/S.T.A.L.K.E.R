using System;
using R3;
using TMPro;
using UnityEngine;

[Serializable]
public class CharacterStatRow : IView<CharacterStatRowViewModel>, IDisposable
{
    [SerializeField] private CharacterStatType _statType;
    [SerializeField] private GameObject _rowObject;
    [SerializeField] private TMP_Text _valueText;

    private CharacterStatRowViewModel _viewModel;
    private IDisposable _activeSubscription;
    private IDisposable _textSubscription;

    public CharacterStatType StatType => _statType;

    public void Render(CharacterStatRowState state)
    {
        EnsureViewModel();
        _viewModel.Render(state);
    }

    public void Hide()
    {
        EnsureViewModel();
        _viewModel.Hide();
    }

    public void Bind(CharacterStatRowViewModel viewModel)
    {
        Unbind();
        _viewModel?.Dispose();
        _viewModel = viewModel;

        if (_viewModel == null)
        {
            return;
        }

        _activeSubscription = _viewModel.Active.Subscribe(SetActive);
        _textSubscription = _viewModel.Text.Subscribe(SetText);
    }

    public void Unbind()
    {
        _activeSubscription?.Dispose();
        _textSubscription?.Dispose();
        _activeSubscription = null;
        _textSubscription = null;
    }

    public void Dispose()
    {
        Unbind();
        _viewModel?.Dispose();
        _viewModel = null;
    }

    private void EnsureViewModel()
    {
        if (_viewModel != null)
        {
            return;
        }

        Bind(InventoryViewModelFactory.CreateCharacterStatRow());
    }

    private void SetActive(bool active)
    {
        if (_rowObject != null)
        {
            _rowObject.SetActive(active);
        }

        if (_valueText != null)
        {
            _valueText.gameObject.SetActive(active);
        }
    }

    private void SetText(string text)
    {
        if (_valueText == null)
        {
            return;
        }

        _valueText.richText = true;
        _valueText.text = text;
    }
}
