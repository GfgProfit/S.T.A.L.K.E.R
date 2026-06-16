using System;
using System.Collections.Generic;
using UnityEngine;

internal sealed class FirstPersonHandsMeshSwitcher
{
    private const string HANDS_PREFIX = "wpn_hand_";
    private const string DEFAULT_FALLBACK_MESH_NAME = "default";

    private readonly Transform _meshesRoot;
    private readonly string _defaultMeshName;
    private readonly Dictionary<string, HandsMeshEntry> _meshByName = new(StringComparer.OrdinalIgnoreCase);
    private readonly List<HandsMeshEntry> _meshes = new();
    private readonly HashSet<string> _loggedMissingMeshNames = new(StringComparer.OrdinalIgnoreCase);
    private SkinnedMeshRenderer[] _allRenderers = Array.Empty<SkinnedMeshRenderer>();

    public FirstPersonHandsMeshSwitcher(Transform meshesRoot, string defaultMeshName)
    {
        _meshesRoot = meshesRoot;
        _defaultMeshName = NormalizeMeshName(defaultMeshName);

        CacheMeshes();
    }

    public void SetMesh(string meshName)
    {
        if (_meshesRoot == null)
        {
            return;
        }

        string requestedMeshName = NormalizeMeshName(meshName);
        bool isDefaultRequest = string.IsNullOrWhiteSpace(requestedMeshName);
        string resolvedMeshName = isDefaultRequest ? _defaultMeshName : requestedMeshName;

        if (string.IsNullOrWhiteSpace(resolvedMeshName))
        {
            resolvedMeshName = DEFAULT_FALLBACK_MESH_NAME;
        }

        if (TryResolveMesh(resolvedMeshName, isDefaultRequest == false, out HandsMeshEntry targetMesh) == false)
        {
            return;
        }

        SetAllRenderersVisible(false);

        for (int i = 0; i < _meshes.Count; i++)
        {
            HandsMeshEntry mesh = _meshes[i];

            if (mesh != null)
            {
                mesh.SetVisible(mesh == targetMesh);
            }
        }
    }

    private void CacheMeshes()
    {
        if (_meshesRoot == null)
        {
            return;
        }

        _meshByName.Clear();
        _meshes.Clear();
        _allRenderers = _meshesRoot.GetComponentsInChildren<SkinnedMeshRenderer>(true);

        Transform[] transforms = _meshesRoot.GetComponentsInChildren<Transform>(true);

        for (int i = 0; i < transforms.Length; i++)
        {
            Transform meshRoot = transforms[i];

            if (meshRoot == _meshesRoot || IsHandsMeshRoot(meshRoot) == false)
            {
                continue;
            }

            HandsMeshEntry mesh = new(meshRoot);
            _meshes.Add(mesh);
            RegisterMeshName(meshRoot.name, mesh);
        }
    }

    private void RegisterMeshName(string meshName, HandsMeshEntry mesh)
    {
        meshName = NormalizeMeshName(meshName);

        if (string.IsNullOrWhiteSpace(meshName) || _meshByName.ContainsKey(meshName))
        {
            return;
        }

        _meshByName.Add(meshName, mesh);
    }

    private bool TryResolveMesh(string meshName, bool logMissingRequestedMesh, out HandsMeshEntry mesh)
    {
        if (TryGetMesh(meshName, out mesh))
        {
            return true;
        }

        if (logMissingRequestedMesh)
        {
            LogMissingMesh(meshName);
        }

        if (TryGetMesh(_defaultMeshName, out mesh))
        {
            return true;
        }

        return TryGetMesh(DEFAULT_FALLBACK_MESH_NAME, out mesh);
    }

    private void SetAllRenderersVisible(bool visible)
    {
        for (int i = 0; i < _allRenderers.Length; i++)
        {
            SkinnedMeshRenderer renderer = _allRenderers[i];

            if (renderer != null)
            {
                renderer.enabled = visible;
            }
        }
    }

    private bool TryGetMesh(string meshName, out HandsMeshEntry mesh)
    {
        meshName = NormalizeMeshName(meshName);
        return _meshByName.TryGetValue(meshName, out mesh);
    }

    private void LogMissingMesh(string meshName)
    {
        if (string.IsNullOrWhiteSpace(meshName) || _loggedMissingMeshNames.Add(meshName) == false)
        {
            return;
        }

        Debug.LogWarning($"First person hands mesh '{meshName}' was not found under '{_meshesRoot.name}'.");
    }

    private bool IsHandsMeshRoot(Transform mesh)
    {
        if (mesh == null)
        {
            return false;
        }

        if (mesh.name.StartsWith(HANDS_PREFIX, StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        return mesh.parent == _meshesRoot && mesh.GetComponent<SkinnedMeshRenderer>() != null;
    }

    private static string NormalizeMeshName(string meshName)
    {
        if (string.IsNullOrWhiteSpace(meshName))
        {
            return string.Empty;
        }

        meshName = meshName.Trim();

        return meshName.StartsWith(HANDS_PREFIX, StringComparison.OrdinalIgnoreCase) ? meshName.Substring(HANDS_PREFIX.Length) : meshName;
    }

    private sealed class HandsMeshEntry
    {
        private readonly SkinnedMeshRenderer[] _renderers;

        public HandsMeshEntry(Transform root)
        {
            Root = root;
            _renderers = root == null ? Array.Empty<SkinnedMeshRenderer>() : root.GetComponentsInChildren<SkinnedMeshRenderer>(true);
        }

        public Transform Root { get; }

        public void SetVisible(bool visible)
        {
            if (Root != null)
            {
                Root.gameObject.SetActive(visible);
            }

            for (int i = 0; i < _renderers.Length; i++)
            {
                SkinnedMeshRenderer renderer = _renderers[i];

                if (renderer != null)
                {
                    renderer.enabled = visible;
                }
            }
        }
    }
}
