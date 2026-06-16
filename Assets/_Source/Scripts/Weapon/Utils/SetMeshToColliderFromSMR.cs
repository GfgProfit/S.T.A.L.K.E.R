using NaughtyAttributes;
using UnityEngine;

public class SetMeshToColliderFromSMR : MonoBehaviour
{
    [SerializeField] private SkinnedMeshRenderer[] skinnedMeshRenderers;

    [Button]
    private void SetMeshToCollider()
    {
        foreach (SkinnedMeshRenderer smr in skinnedMeshRenderers)
        {
            if (!smr.gameObject.TryGetComponent(out MeshCollider meshCollider))
            {
                meshCollider = smr.gameObject.AddComponent<MeshCollider>();
            }

            meshCollider.sharedMesh = smr.sharedMesh;
        }
    }
}