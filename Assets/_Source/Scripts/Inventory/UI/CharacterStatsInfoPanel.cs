using System;
using System.Collections.Generic;
using R3;
using UnityEngine;

public class CharacterStatsInfoPanel : MonoBehaviour, IView<CharacterStatsInfoPanelViewModel>
{
    [SerializeField] private GameObject _panelRoot;
    [SerializeField] private List<CharacterStatRow> _rows = new();

    private CharacterStatsInfoPanelViewModel _viewModel;
    private IDisposable _rootActiveSubscription;
    private IDisposable _rowsSubscription;

    public void RenderItemStats(ItemTooltipData item, Color currentValueColor, Color fullDurabilityValueColor, bool hideRootWhenEmpty)
    {
        EnsureViewModel();
        _viewModel.RenderItemStats(GetRowStatTypes(), item, currentValueColor, fullDurabilityValueColor, hideRootWhenEmpty);
    }

    public void RenderCharacterStats(CharacterStatBlock stats, Color currentValueColor, bool hideRootWhenEmpty, bool showAllStats)
    {
        EnsureViewModel();
        _viewModel.RenderCharacterStats(GetRowStatTypes(), stats, currentValueColor, hideRootWhenEmpty, showAllStats);
    }

    public void Bind(CharacterStatsInfoPanelViewModel viewModel)
    {
        Unbind();
        _viewModel = viewModel;

        if (_viewModel == null)
        {
            return;
        }

        _rootActiveSubscription = _viewModel.RootActive.Subscribe(SetRootActive);
        _rowsSubscription = _viewModel.Rows.Subscribe(RenderRows);
    }

    public void Unbind()
    {
        _rootActiveSubscription?.Dispose();
        _rowsSubscription?.Dispose();
        _rootActiveSubscription = null;
        _rowsSubscription = null;
    }

    private void OnDestroy()
    {
        Unbind();
        DisposeRows();
        _viewModel?.Dispose();
        _viewModel = null;
    }

    private void EnsureViewModel()
    {
        if (_viewModel != null)
        {
            return;
        }

        Bind(InventoryViewModelFactory.CreateCharacterStatsInfoPanel());
    }

    private IReadOnlyList<CharacterStatType> GetRowStatTypes()
    {
        CharacterStatType[] statTypes = new CharacterStatType[_rows.Count];

        for (int i = 0; i < _rows.Count; i++)
        {
            statTypes[i] = _rows[i].StatType;
        }

        return statTypes;
    }

    private void RenderRows(IReadOnlyList<CharacterStatRowState> states)
    {
        int count = Mathf.Min(_rows.Count, states == null ? 0 : states.Count);

        for (int i = 0; i < count; i++)
        {
            CharacterStatRow row = _rows[i];
            CharacterStatRowState state = states[i];
            row.Render(state);
        }

        for (int i = count; i < _rows.Count; i++)
        {
            _rows[i].Hide();
        }
    }

    private void SetRootActive(bool active)
    {
        GameObject root = _panelRoot == null ? gameObject : _panelRoot;
        root.SetActive(active);
    }

    private void DisposeRows()
    {
        for (int i = 0; i < _rows.Count; i++)
        {
            _rows[i]?.Dispose();
        }
    }
}
