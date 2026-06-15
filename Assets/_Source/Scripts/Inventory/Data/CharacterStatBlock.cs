public sealed class CharacterStatBlock
{
    private readonly float[] _values = new float[CharacterStatUtility.StatCount];

    public float Get(CharacterStatType statType) => _values[CharacterStatUtility.ToIndex(statType)];
    public void Set(CharacterStatType statType, float value) => _values[CharacterStatUtility.ToIndex(statType)] = value;
    public void Add(CharacterStatType statType, float value) => _values[CharacterStatUtility.ToIndex(statType)] += value;

    public void Add(CharacterStatBlock source)
    {
        if (source == null)
        {
            return;
        }

        for (int i = 0; i < _values.Length; i++)
        {
            _values[i] += source._values[i];
        }
    }

    public void CopyFrom(CharacterStatBlock source)
    {
        Clear();

        if (source == null)
        {
            return;
        }

        for (int i = 0; i < _values.Length; i++)
        {
            _values[i] = source._values[i];
        }
    }

    public void Clear()
    {
        for (int i = 0; i < _values.Length; i++)
        {
            _values[i] = 0f;
        }
    }

    public bool HasAnyNonZeroValue()
    {
        for (int i = 0; i < _values.Length; i++)
        {
            if (CharacterStatUtility.IsNonZero(_values[i]))
            {
                return true;
            }
        }

        return false;
    }
}