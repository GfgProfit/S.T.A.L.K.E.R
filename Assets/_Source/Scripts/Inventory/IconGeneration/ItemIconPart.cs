using System;
using UnityEngine;

[Serializable]
public class ItemIconPart
{
    [SerializeField] private GameObject _prefab;
    [SerializeField] private Vector3 _localPosition = Vector3.zero;
    [SerializeField] private Vector3 _localEulerAngles = Vector3.zero;
    [SerializeField] private Vector3 _localScale = Vector3.one;

    public GameObject Prefab => _prefab;
    public Vector3 LocalPosition => _localPosition;
    public Vector3 LocalEulerAngles => _localEulerAngles;
    public Vector3 LocalScale => _localScale == Vector3.zero ? Vector3.one : _localScale;

    public ItemIconPart()
    {
    }

    public ItemIconPart(GameObject prefab)
    {
        _prefab = prefab;
    }

    public ItemIconPart(GameObject prefab, Vector3 localPosition, Vector3 localEulerAngles, Vector3 localScale)
    {
        _prefab = prefab;
        _localPosition = localPosition;
        _localEulerAngles = localEulerAngles;
        _localScale = localScale;
    }

    internal void ApplyTo(Transform target)
    {
        target.SetLocalPositionAndRotation(_localPosition, Quaternion.Euler(_localEulerAngles));
        target.localScale = LocalScale;
    }

    internal int BuildHash()
    {
        unchecked
        {
            int hash = 17;
            hash = hash * 31 + (_prefab == null ? 0 : _prefab.GetInstanceID());
            hash = hash * 31 + HashVector(_localPosition);
            hash = hash * 31 + HashVector(_localEulerAngles);
            hash = hash * 31 + HashVector(LocalScale);
            return hash;
        }
    }

    private static int HashVector(Vector3 value)
    {
        unchecked
        {
            int hash = 17;
            hash = hash * 31 + Mathf.RoundToInt(value.x * 1000f);
            hash = hash * 31 + Mathf.RoundToInt(value.y * 1000f);
            hash = hash * 31 + Mathf.RoundToInt(value.z * 1000f);
            return hash;
        }
    }
}