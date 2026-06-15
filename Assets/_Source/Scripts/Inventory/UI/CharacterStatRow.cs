using System;
using TMPro;
using UnityEngine;

[Serializable]
public class CharacterStatRow
{
    [SerializeField] private CharacterStatType _statType;
    [SerializeField] private GameObject _rowObject;
    [SerializeField] private TMP_Text _valueText;

    public CharacterStatType StatType => _statType;

    public void SetActive(bool active)
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

    public void SetText(string text)
    {
        if (_valueText == null)
        {
            return;
        }

        _valueText.richText = true;
        _valueText.text = text;
    }
}