using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.AssetImporters;
using UnityEngine;
using XRay.Importer.Core.IO;
using XRay.Importer.Core.Ogf;
using XRay.Importer.Core.Omf;
using XRay.Importer.Core.Skeleton;

namespace XRay.Importer.Editor
{
    /// <summary>
    /// Imports X-Ray *.omf motion files as AnimationClips for one OGF rig. The rig defines the transform
    /// paths and the scale; there are no coordinate settings here, so the clips always match the OGF import.
    /// </summary>
    [ScriptedImporter(IMPORTER_VERSION, "omf")]
    public sealed class XRayOmfImporter : ScriptedImporter
    {
        public const int IMPORTER_VERSION = 1;

        [Tooltip("GUID of the *.ogf whose skeleton these motions animate.")]
        [SerializeField] private string _rigGuid = string.Empty;
        [Tooltip("When no rig is assigned, use the *.ogf in the same folder whose skeleton contains every motion bone.")]
        [SerializeField] private bool _autoDetectRig = true;
        [SerializeField] private XRayLoopMode _loopMode = XRayLoopMode.FromMotionFlags;
        [SerializeField] private List<string> _excludedMotions = new List<string>();

        public string RigGuid => _rigGuid;

        public override void OnImportAsset(AssetImportContext ctx)
        {
            string fileName = Path.GetFileName(ctx.assetPath);
            OmfData data;
            try
            {
                data = OmfReader.ReadFile(File.ReadAllBytes(ctx.assetPath), fileName);
            }
            catch (XRayFormatException exception)
            {
                ctx.LogImportError($"{fileName}: {exception.Message}");
                SetPlaceholder(ctx, fileName);
                return;
            }
            catch (Exception exception)
            {
                ctx.LogImportError($"{fileName}: failed to read OMF: {exception}");
                SetPlaceholder(ctx, fileName);
                return;
            }

            foreach (string warning in data.Warnings)
            {
                ctx.LogImportWarning($"{fileName}: {warning}");
            }

            try
            {
                string rigPath = ResolveRig(ctx, data, fileName, out OgfModel rigModel);
                XRayRigPaths paths = null;
                float scale = 1.0f;
                string rigName = string.Empty;
                if (rigModel != null)
                {
                    scale = LoadRigScale(ctx, rigPath);
                    paths = new XRayRigPaths(new XRaySkeleton(rigModel.Bones), null);
                    rigName = rigPath;
                }
                else
                {
                    ctx.LogImportWarning($"{fileName}: no rig (*.ogf) assigned or detected; motion info is imported but no AnimationClips are generated. Assign the rig in the importer inspector.");
                }

                var excluded = new HashSet<string>(_excludedMotions);
                XRayMotionLibrary library = XRayMotionLibraryBuilder.Build(ctx, data, fileName, rigName, paths, scale, _loopMode, excluded);
                ctx.AddObjectToAsset("library", library);
                ctx.SetMainObject(library);
            }
            catch (XRayFormatException exception)
            {
                ctx.LogImportError($"{fileName}: {exception.Message}");
                SetPlaceholder(ctx, fileName);
            }
            catch (Exception exception)
            {
                ctx.LogImportError($"{fileName}: failed to build motions: {exception}");
                SetPlaceholder(ctx, fileName);
            }
        }

        /// <summary>Returns the rig path and its parsed model, or null when no usable rig exists.</summary>
        private string ResolveRig(AssetImportContext ctx, OmfData data, string fileName, out OgfModel model)
        {
            model = null;
            if (!string.IsNullOrEmpty(_rigGuid))
            {
                string path = AssetDatabase.GUIDToAssetPath(_rigGuid);
                if (string.IsNullOrEmpty(path) || !path.EndsWith(".ogf", StringComparison.OrdinalIgnoreCase))
                {
                    ctx.LogImportError($"{fileName}: assigned rig (GUID {_rigGuid}) is not an *.ogf asset.");
                    return null;
                }

                ctx.DependsOnSourceAsset(path);
                model = OgfReader.ReadFile(File.ReadAllBytes(path), Path.GetFileName(path));
                if (!model.HasSkeleton)
                {
                    ctx.LogImportError($"{fileName}: assigned rig '{path}' has no skeleton.");
                    model = null;
                    return null;
                }

                return path;
            }

            if (!_autoDetectRig)
            {
                return null;
            }

            string directory = Path.GetDirectoryName(ctx.assetPath);
            var candidates = new List<string>(Directory.GetFiles(directory, "*.ogf"));
            candidates.Sort(StringComparer.Ordinal);
            var matches = new List<string>();
            OgfModel firstMatch = null;
            foreach (string candidate in candidates)
            {
                string path = candidate.Replace('\\', '/');
                ctx.DependsOnSourceAsset(path);
                OgfModel candidateModel;
                try
                {
                    candidateModel = OgfReader.ReadFile(File.ReadAllBytes(path), Path.GetFileName(path));
                }
                catch (XRayFormatException)
                {
                    // An unreadable neighbour is reported by its own importer.
                    continue;
                }

                if (ContainsAllBones(candidateModel, data))
                {
                    matches.Add(path);
                    if (firstMatch == null)
                    {
                        firstMatch = candidateModel;
                    }
                }
            }

            if (matches.Count == 0)
            {
                return null;
            }

            if (matches.Count > 1)
            {
                ctx.LogImportWarning($"{fileName}: several rigs match ({string.Join(", ", matches)}); using '{matches[0]}'. Assign the rig explicitly to choose another.");
            }

            model = firstMatch;
            return matches[0];
        }

        /// <summary>
        /// Takes the scale from the imported rig and registers an artifact dependency on it: the OGF artifact
        /// changes whenever the file or its importer settings change, which reimports these clips.
        /// Unity only keeps artifact dependencies whose artifact is actually loaded during the import.
        /// </summary>
        private static float LoadRigScale(AssetImportContext ctx, string rigPath)
        {
            ctx.DependsOnArtifact(rigPath);
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(rigPath);
            XRayModel model = prefab != null ? prefab.GetComponent<XRayModel>() : null;
            if (model != null)
            {
                return model.Scale;
            }

            var importer = AssetImporter.GetAtPath(rigPath) as XRayOgfImporter;
            ctx.LogImportWarning($"Rig '{rigPath}' has no imported model information yet; using its importer scale.");
            return importer != null ? importer.Scale : 1.0f;
        }

        private static bool ContainsAllBones(OgfModel model, OmfData data)
        {
            if (!model.HasSkeleton)
            {
                return false;
            }

            foreach (string bone in data.BoneNames.Values)
            {
                if (model.FindBone(bone) < 0)
                {
                    return false;
                }
            }

            return true;
        }

        private static void SetPlaceholder(AssetImportContext ctx, string fileName)
        {
            var library = ScriptableObject.CreateInstance<XRayMotionLibrary>();
            library.name = fileName;
            ctx.AddObjectToAsset("library", library);
            ctx.SetMainObject(library);
        }
    }
}
