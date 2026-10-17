using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor.AssetImporters;
using UnityEngine;
using XRay.Importer.Core.IO;
using XRay.Importer.Core.Ogf;

namespace XRay.Importer.Editor
{
    /// <summary>Imports X-Ray *.ogf models (static, hierarchical and skeletal) as prefabs.</summary>
    [ScriptedImporter(IMPORTER_VERSION, "ogf")]
    public sealed class XRayOgfImporter : ScriptedImporter
    {
        // Bump when the generated output changes so existing assets are reimported.
        public const int IMPORTER_VERSION = 1;

        [Tooltip("Uniform scale applied to vertex positions, bone offsets and motion translations. X-Ray units are meters.")]
        [SerializeField] private float _scale = 1.0f;
        [SerializeField] private bool _importMaterials = true;
        [Tooltip("Project folder that mirrors gamedata/textures, e.g. Assets/XRay/textures. Texture names are resolved relative to it.")]
        [SerializeField] private string _texturesFolder = string.Empty;
        [Tooltip("If a texture is not found under the textures folder, search the whole project by file name.")]
        [SerializeField] private bool _searchTexturesByName = true;
        [SerializeField] private bool _importEmbeddedMotions = true;
        [SerializeField] private XRayLoopMode _embeddedLoopMode = XRayLoopMode.FromMotionFlags;
        [SerializeField] private bool _addAnimator = true;
        [SerializeField] private bool _readableMeshes;
        [SerializeField] private bool _updateWhenOffscreen;

        public float Scale => _scale;

        public override void OnImportAsset(AssetImportContext ctx)
        {
            string fileName = Path.GetFileName(ctx.assetPath);
            string rootName = Path.GetFileNameWithoutExtension(ctx.assetPath);
            if (_scale <= 0.0f)
            {
                ctx.LogImportError($"{fileName}: scale must be positive (got {_scale}).");
                SetPlaceholder(ctx, rootName);
                return;
            }

            OgfModel model;
            try
            {
                model = OgfReader.ReadFile(File.ReadAllBytes(ctx.assetPath), fileName);
            }
            catch (XRayFormatException exception)
            {
                ctx.LogImportError($"{fileName}: {exception.Message}");
                SetPlaceholder(ctx, rootName);
                return;
            }
            catch (Exception exception)
            {
                ctx.LogImportError($"{fileName}: failed to read OGF: {exception}");
                SetPlaceholder(ctx, rootName);
                return;
            }

            foreach (string warning in model.Warnings)
            {
                ctx.LogImportWarning($"{fileName}: {warning}");
            }

            try
            {
                Build(ctx, model, fileName, rootName);
            }
            catch (Exception exception)
            {
                ctx.LogImportError($"{fileName}: failed to build the Unity model: {exception}");
                SetPlaceholder(ctx, rootName);
            }
        }

        private void Build(AssetImportContext ctx, OgfModel model, string fileName, string rootName)
        {
            var warnings = new List<string>();
            var materials = new XRayMaterialBuilder(ctx, _importMaterials, _texturesFolder, _searchTexturesByName, warnings);
            var settings = new XRayModelBuildSettings { Scale = _scale, AddAnimator = _addAnimator, ReadableMeshes = _readableMeshes, UpdateWhenOffscreen = _updateWhenOffscreen };
            XRayModelBuildResult result = XRayModelBuilder.Build(model, rootName, settings, materials, warnings);

            for (int i = 0; i < result.Meshes.Count; i++)
            {
                ctx.AddObjectToAsset("mesh:" + i, result.Meshes[i]);
            }

            XRayMotionLibrary embedded = null;
            if (_importEmbeddedMotions && model.EmbeddedMotions != null && result.RigPaths != null)
            {
                foreach (string warning in model.EmbeddedMotions.Warnings)
                {
                    warnings.Add("Embedded motions: " + warning);
                }

                embedded = XRayMotionLibraryBuilder.Build(ctx, model.EmbeddedMotions, fileName, rootName, result.RigPaths, _scale, _embeddedLoopMode, null);
                embedded.name = rootName + " motions";
                ctx.AddObjectToAsset("motions", embedded);
            }

            var boneNames = new string[model.Bones.Count];
            for (int i = 0; i < boneNames.Length; i++)
            {
                boneNames[i] = model.Bones[i].Name;
            }

            XRayModel info = result.Root.AddComponent<XRayModel>();
            info.SetImportData(fileName, model.Root.FormatVersion, model.Root.Kind.ToString(), _scale, result.Bones, boneNames, model.MotionRefs.ToArray(), model.UserData, model.LodReference, embedded, result.Meshes.Count, result.VertexCount, result.TriangleCount, result.Textures.ToArray());

            foreach (string warning in warnings)
            {
                ctx.LogImportWarning($"{fileName}: {warning}");
            }

            ctx.AddObjectToAsset("root", result.Root);
            ctx.SetMainObject(result.Root);
        }

        private static void SetPlaceholder(AssetImportContext ctx, string rootName)
        {
            // A main object keeps the asset selectable and its importer inspector usable after a failed import.
            var placeholder = new GameObject(rootName);
            ctx.AddObjectToAsset("root", placeholder);
            ctx.SetMainObject(placeholder);
        }
    }
}
