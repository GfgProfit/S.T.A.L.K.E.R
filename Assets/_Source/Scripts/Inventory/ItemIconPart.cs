using System;
using UnityEngine;

[Serializable]
public class ItemIconPart
{
    [SerializeField] private GameObject prefab;
    [SerializeField] private Vector3 localPosition = Vector3.zero;
    [SerializeField] private Vector3 localEulerAngles = Vector3.zero;
    [SerializeField] private Vector3 localScale = Vector3.one;

    public GameObject Prefab => prefab;
    public Vector3 LocalPosition => localPosition;
    public Vector3 LocalEulerAngles => localEulerAngles;
    public Vector3 LocalScale => localScale == Vector3.zero ? Vector3.one : localScale;

    public ItemIconPart()
    {
    }

    public ItemIconPart(GameObject prefab)
    {
        this.prefab = prefab;
    }

    public ItemIconPart(GameObject prefab, Vector3 localPosition, Vector3 localEulerAngles, Vector3 localScale)
    {
        this.prefab = prefab;
        this.localPosition = localPosition;
        this.localEulerAngles = localEulerAngles;
        this.localScale = localScale;
    }

    internal void ApplyTo(Transform target)
    {
        target.localPosition = localPosition;
        target.localRotation = Quaternion.Euler(localEulerAngles);
        target.localScale = LocalScale;
    }

    internal int BuildHash()
    {
        unchecked
        {
            int hash = 17;
            hash = hash * 31 + (prefab == null ? 0 : prefab.GetInstanceID());
            hash = hash * 31 + HashVector(localPosition);
            hash = hash * 31 + HashVector(localEulerAngles);
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
