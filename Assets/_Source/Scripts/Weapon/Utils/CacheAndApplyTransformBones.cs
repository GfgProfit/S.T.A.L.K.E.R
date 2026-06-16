using NaughtyAttributes;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class CacheAndApplyTransformBones : MonoBehaviour
{
    [SerializeField] private Transform _bonesParent;

    [Space(10)]
    [SerializeField] private List<Transform> _bones = new();

    [Space(10)]
    [SerializeField] private List<Vector3> _bonesPositions = new();
    [SerializeField] private List<Quaternion> _bonesRotations = new();

    [Button("Cache Bone Transforms")]
    private void Cache()
    {
        if (_bonesParent == null)
        {
            Debug.LogWarning("[CacheAndApplyTransformBones] _bonesParent is not assigned.");
            return;
        }

        List<Transform> childBones = _bonesParent.GetComponentsInChildren<Transform>().ToList();

        _bones.Clear();
        _bonesPositions.Clear();
        _bonesRotations.Clear();

        foreach (Transform bone in childBones)
        {
            _bones.Add(bone);
            _bonesPositions.Add(bone.position);
            _bonesRotations.Add(bone.rotation);
        }
    }

    [Button("Apply Cached Transforms")]
    private void ApplyTransformsToBones()
    {
        if (_bones.Count != _bonesPositions.Count || _bones.Count != _bonesRotations.Count)
        {
            Debug.LogError("[CacheAndApplyTransformBones] Mismatch in bones data. Ensure Cache() has been called.");
            return;
        }

        for (int i = 0; i < _bones.Count; i++)
        {
            _bones[i].SetPositionAndRotation(_bonesPositions[i], _bonesRotations[i]);
        }
    }
}
