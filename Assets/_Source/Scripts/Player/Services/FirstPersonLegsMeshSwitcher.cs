using System;
using System.Collections.Generic;
using UnityEngine;

internal sealed class FirstPersonLegsMeshSwitcher
{
    private const string LEGS_PREFIX = "legs_";

    private readonly Transform _meshesRoot;
    private readonly string _defaultMeshName;
    private readonly Dictionary<string, Transform> _meshByName = new(StringComparer.OrdinalIgnoreCase);
    private readonly HashSet<string> _loggedMissingMeshNames = new(StringComparer.OrdinalIgnoreCase);

    private Transform _activeMesh;

    public FirstPersonLegsMeshSwitcher(Transform meshesRoot, string defaultMeshName)
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

        string resolvedMeshName = NormalizeMeshName(meshName);
		
        if (string.IsNullOrWhiteSpace(resolvedMeshName))
        {
            resolvedMeshName = _defaultMeshName;
        }

        if (TryGetMesh(resolvedMeshName, out Transform targetMesh) == false)
        {
            LogMissingMesh(resolvedMeshName);

            if (TryGetMesh(_defaultMeshName, out targetMesh) == false)
            {
                return;
            }
        }

        if (_activeMesh == targetMesh)
        {
            return;
        }

        for (int i = 0; i < _meshesRoot.childCount; i++)
        {
            Transform child = _meshesRoot.GetChild(i);
            child.gameObject.SetActive(child == targetMesh);
        }

        _activeMesh = targetMesh;
    }

    private void CacheMeshes()
    {
        if (_meshesRoot == null)
        {
            return;
        }

        _meshByName.Clear();

        for (int i = 0; i < _meshesRoot.childCount; i++)
        {
            Transform child = _meshesRoot.GetChild(i);
            RegisterMeshName(child.name, child);
            RegisterMeshName($"{LEGS_PREFIX}{child.name}", child);
        }
    }

    private void RegisterMeshName(string meshName, Transform mesh)
    {
        meshName = NormalizeMeshName(meshName);

        if (string.IsNullOrWhiteSpace(meshName) || _meshByName.ContainsKey(meshName))
        {
            return;
        }

        _meshByName.Add(meshName, mesh);
    }

    private bool TryGetMesh(string meshName, out Transform mesh)
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

        Debug.LogWarning($"First person legs mesh '{meshName}' was not found under '{_meshesRoot.name}'.");
    }

    private static string NormalizeMeshName(string meshName)
    {
        if (string.IsNullOrWhiteSpace(meshName))
        {
            return string.Empty;
        }

        meshName = meshName.Trim();

        return meshName.StartsWith(LEGS_PREFIX, StringComparison.OrdinalIgnoreCase) ? meshName.Substring(LEGS_PREFIX.Length) : meshName;
    }
}
