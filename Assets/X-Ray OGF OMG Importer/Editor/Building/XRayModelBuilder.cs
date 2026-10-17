using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.Rendering;
using XRay.Importer.Core;
using XRay.Importer.Core.Ogf;
using XRay.Importer.Core.Skeleton;

namespace XRay.Importer.Editor
{
    public sealed class XRayModelBuildSettings
    {
        public float Scale { get; set; } = 1.0f;

        public bool AddAnimator { get; set; } = true;

        public bool ReadableMeshes { get; set; }

        public bool UpdateWhenOffscreen { get; set; }
    }

    public sealed class XRayModelBuildResult
    {
        public GameObject Root { get; set; }

        public XRaySkeleton Skeleton { get; set; }

        public XRayRigPaths RigPaths { get; set; }

        public Transform[] Bones { get; set; } = new Transform[0];

        public List<Mesh> Meshes { get; } = new List<Mesh>();

        public int VertexCount { get; set; }

        public int TriangleCount { get; set; }

        public List<string> Textures { get; } = new List<string>();
    }

    /// <summary>
    /// Builds the Unity hierarchy of an OGF: bone transforms with the X-Ray rest pose as local TRS,
    /// one child GameObject per mesh visual (SkinnedMeshRenderer for skinned visuals), bind poses
    /// computed from the very transforms that the animation clips drive.
    /// </summary>
    public static class XRayModelBuilder
    {
        public static XRayModelBuildResult Build(OgfModel model, string rootName, XRayModelBuildSettings settings, XRayMaterialBuilder materials, List<string> warnings)
        {
            var result = new XRayModelBuildResult { Root = new GameObject(rootName) };
            Transform root = result.Root.transform;

            if (model.HasSkeleton)
            {
                BuildSkeleton(model, settings.Scale, result, warnings);
                if (settings.AddAnimator)
                {
                    Animator animator = result.Root.AddComponent<Animator>();
                    animator.applyRootMotion = false;
                    animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
                }
            }

            var usedNames = new HashSet<string>();
            if (result.RigPaths != null)
            {
                foreach (string boneName in result.RigPaths.ObjectNames)
                {
                    usedNames.Add(boneName);
                }
            }

            List<OgfVisual> visuals = model.GetMeshVisuals();
            for (int i = 0; i < visuals.Count; i++)
            {
                OgfVisual visual = visuals[i];
                string objectName = UniqueName(MakeMeshName(i, visual), usedNames);
                var meshObject = new GameObject(objectName);
                meshObject.transform.SetParent(root, false);

                Mesh mesh = CreateMesh(visual, objectName, settings.Scale);
                Material material = materials.Get(visual.Texture, visual.Shader);
                if (!string.IsNullOrEmpty(visual.Texture) && !result.Textures.Contains(visual.Texture))
                {
                    result.Textures.Add(visual.Texture);
                }

                if (visual.IsSkinned)
                {
                    SetupSkinned(meshObject, mesh, visual, result, material, settings);
                }
                else
                {
                    mesh.RecalculateBounds();
                    meshObject.AddComponent<MeshFilter>().sharedMesh = mesh;
                    meshObject.AddComponent<MeshRenderer>().sharedMaterial = material;
                }

                if (!settings.ReadableMeshes)
                {
                    mesh.UploadMeshData(true);
                }

                result.Meshes.Add(mesh);
                result.VertexCount += visual.VertexCount;
                result.TriangleCount += visual.TriangleCount;
            }

            return result;
        }

        private static void BuildSkeleton(OgfModel model, float scale, XRayModelBuildResult result, List<string> warnings)
        {
            var skeleton = new XRaySkeleton(model.Bones);
            var paths = new XRayRigPaths(skeleton, warnings);
            var bones = new Transform[skeleton.Count];
            for (int i = 0; i < skeleton.Count; i++)
            {
                bones[i] = new GameObject(paths.ObjectNames[i]).transform;
            }

            for (int i = 0; i < skeleton.Count; i++)
            {
                int parent = skeleton.Parents[i];
                bones[i].SetParent(parent >= 0 ? bones[parent] : result.Root.transform, false);
                bones[i].localPosition = XRayUnitySpace.Position(skeleton.LocalPositions[i], scale);
                bones[i].localRotation = XRayUnitySpace.Rotation(skeleton.LocalRotations[i]);
                bones[i].localScale = Vector3.one;
            }

            result.Skeleton = skeleton;
            result.RigPaths = paths;
            result.Bones = bones;
        }

        private static Mesh CreateMesh(OgfVisual visual, string name, float scale)
        {
            int count = visual.VertexCount;
            var vertices = new Vector3[count];
            var normals = new Vector3[count];
            var uv = new Vector2[count];
            for (int i = 0; i < count; i++)
            {
                vertices[i] = XRayUnitySpace.Position(visual.Positions[i], scale);
                normals[i] = XRayUnitySpace.Direction(visual.Normals[i]);

                // D3D texture space has V pointing down.
                uv[i] = new Vector2((float)visual.U[i], 1.0f - (float)visual.V[i]);
            }

            var mesh = new Mesh { name = name };
            mesh.indexFormat = count > 65535 ? IndexFormat.UInt32 : IndexFormat.UInt16;
            mesh.vertices = vertices;
            mesh.normals = normals;
            mesh.uv = uv;

            // X-Ray and Unity both treat clockwise triangles as front faces in a left-handed space,
            // so the index order is kept as stored.
            mesh.SetTriangles(visual.Indices, 0, true);
            mesh.RecalculateTangents();
            return mesh;
        }

        private static void SetupSkinned(GameObject meshObject, Mesh mesh, OgfVisual visual, XRayModelBuildResult result, Material material, XRayModelBuildSettings settings)
        {
            var weights = new BoneWeight[visual.VertexCount];
            for (int i = 0; i < visual.VertexCount; i++)
            {
                OgfVertexWeights source = visual.Weights[i];
                weights[i] = new BoneWeight
                {
                    boneIndex0 = source.Bone0,
                    boneIndex1 = source.Count > 1 ? source.Bone1 : 0,
                    boneIndex2 = source.Count > 2 ? source.Bone2 : 0,
                    boneIndex3 = source.Count > 3 ? source.Bone3 : 0,
                    weight0 = (float)source.Weight0,
                    weight1 = source.Count > 1 ? (float)source.Weight1 : 0.0f,
                    weight2 = source.Count > 2 ? (float)source.Weight2 : 0.0f,
                    weight3 = source.Count > 3 ? (float)source.Weight3 : 0.0f
                };
            }

            // Bind pose = inverse rest world matrix of the bone relative to the mesh object, taken from the
            // Unity transforms themselves so skinning at rest reproduces the stored vertices exactly.
            Transform[] bones = result.Bones;
            var bindPoses = new Matrix4x4[bones.Length];
            Matrix4x4 meshToWorld = meshObject.transform.localToWorldMatrix;
            for (int i = 0; i < bones.Length; i++)
            {
                bindPoses[i] = bones[i].worldToLocalMatrix * meshToWorld;
            }

            mesh.boneWeights = weights;
            mesh.bindposes = bindPoses;
            mesh.RecalculateBounds();

            Transform rootBone = FindRootBone(result);
            var renderer = meshObject.AddComponent<SkinnedMeshRenderer>();
            renderer.sharedMesh = mesh;
            renderer.bones = bones;
            renderer.rootBone = rootBone;
            renderer.sharedMaterial = material;
            renderer.quality = SkinQuality.Bone4;
            renderer.updateWhenOffscreen = settings.UpdateWhenOffscreen;
            renderer.localBounds = ComputeLocalBounds(mesh, rootBone != null ? rootBone.worldToLocalMatrix * meshToWorld : Matrix4x4.identity);
        }

        private static Transform FindRootBone(XRayModelBuildResult result)
        {
            for (int i = 0; i < result.Skeleton.Count; i++)
            {
                if (result.Skeleton.Parents[i] < 0)
                {
                    return result.Bones[i];
                }
            }

            return null;
        }

        private static Bounds ComputeLocalBounds(Mesh mesh, Matrix4x4 meshToRootBone)
        {
            Bounds source = mesh.bounds;
            var bounds = new Bounds(meshToRootBone.MultiplyPoint3x4(source.center), Vector3.zero);
            for (int i = 0; i < 8; i++)
            {
                Vector3 corner = source.center + Vector3.Scale(source.extents, new Vector3((i & 1) == 0 ? -1 : 1, (i & 2) == 0 ? -1 : 1, (i & 4) == 0 ? -1 : 1));
                bounds.Encapsulate(meshToRootBone.MultiplyPoint3x4(corner));
            }

            return bounds;
        }

        private static string MakeMeshName(int index, OgfVisual visual)
        {
            string texture = string.IsNullOrEmpty(visual.Texture) ? "mesh" : Path.GetFileName(visual.Texture.Replace('\\', '/'));
            return $"mesh_{index:00}_{texture}".Replace('/', '_');
        }

        private static string UniqueName(string name, HashSet<string> used)
        {
            string candidate = name;
            for (int i = 1; !used.Add(candidate); i++)
            {
                candidate = name + "_" + i;
            }

            return candidate;
        }
    }
}
