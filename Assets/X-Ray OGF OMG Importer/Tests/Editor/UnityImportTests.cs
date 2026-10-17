using System.Collections.Generic;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using XRay.Importer.Core;
using XRay.Importer.Core.Ogf;
using XRay.Importer.Core.Omf;
using XRay.Importer.Core.Skeleton;
using XRay.Importer.Editor;
using XRay.Importer.Tests.Verification;

namespace XRay.Importer.Tests
{
    /// <summary>
    /// End-to-end checks of the assets produced by the ScriptedImporters: prefab hierarchy, bind poses,
    /// AnimationClips sampled on the real Transforms and SkinnedMeshRenderer.BakeMesh output,
    /// compared against the Core math and directly against Blender.
    /// </summary>
    public sealed class UnityImportTests
    {
        private const double MATRIX_TOLERANCE = 1e-4;
        private const double GEOMETRY_TOLERANCE = 1e-4;

        private readonly List<Object> _cleanup = new List<Object>();

        [TearDown]
        public void TearDown()
        {
            foreach (Object item in _cleanup)
            {
                if (item != null)
                {
                    Object.DestroyImmediate(item);
                }
            }

            _cleanup.Clear();
        }

        [Test]
        public void HandsPrefab_RestPoseReproducesSourceVertices()
        {
            GameObject instance = Instantiate(XRayTestData.HANDS_OGF);
            OgfModel model = XRayTestData.Ogf(XRayTestData.HANDS_OGF);
            XRayModel info = instance.GetComponent<XRayModel>();
            Assert.IsNotNull(info);
            Assert.AreEqual(42, info.Bones.Count);
            Assert.AreEqual(4, info.MeshCount);
            Assert.IsNotNull(instance.GetComponent<Animator>());

            List<XVector3[]> baked = Bake(instance, info.Scale);
            List<OgfVisual> visuals = model.GetMeshVisuals();
            Assert.AreEqual(visuals.Count, baked.Count);
            for (int m = 0; m < visuals.Count; m++)
            {
                Assert.Less(MaxDistance(baked[m], visuals[m].Positions), GEOMETRY_TOLERANCE, visuals[m].Name);
            }
        }

        [Test]
        public void HandsLibrary_HasOneClipPerMotion()
        {
            XRayMotionLibrary library = LoadLibrary(XRayTestData.HANDS_OMF);
            Assert.AreEqual(96, library.Motions.Count);
            StringAssert.EndsWith("wpn_hand_bandit_exo.ogf", library.RigName);
            foreach (XRayMotionEntry entry in library.Motions)
            {
                Assert.IsNotNull(entry.Clip, entry.Name);
                Assert.AreEqual(30.0f, entry.Clip.frameRate);
                Assert.AreEqual((entry.FrameCount - 1) / 30.0f, entry.Clip.length, 1e-4f, entry.Name);
            }

            XRayMotionEntry idle = library.Find("ak74_idle");
            Assert.AreEqual(100, idle.FrameCount);
        }

        [Test]
        public void WeaponLibrary_BindsToWeaponRig()
        {
            XRayMotionLibrary library = LoadLibrary(XRayTestData.WEAPON_OMF);
            Assert.AreEqual(10, library.Motions.Count);
            StringAssert.EndsWith("wpn_ak74_ekp_hud.ogf", library.RigName);
            Assert.IsNotNull(library.Find("idle_gl").Clip);
        }

        [Test]
        public void ClipBindings_ResolveOnOwnRigOnly()
        {
            GameObject hands = Instantiate(XRayTestData.HANDS_OGF);
            GameObject weapon = Instantiate(XRayTestData.WEAPON_OGF);
            AssertBindings(LoadLibrary(XRayTestData.HANDS_OMF), hands, weapon, 42);
            AssertBindings(LoadLibrary(XRayTestData.WEAPON_OMF), weapon, hands, 17);
        }

        [Test]
        public void ForeignClips_DoNotChangeOtherRig()
        {
            GameObject hands = Instantiate(XRayTestData.HANDS_OGF);
            GameObject weapon = Instantiate(XRayTestData.WEAPON_OGF);
            XRayModel handsInfo = hands.GetComponent<XRayModel>();
            XRayModel weaponInfo = weapon.GetComponent<XRayModel>();
            List<XVector3[]> handsRest = Bake(hands, handsInfo.Scale);
            List<XVector3[]> weaponRest = Bake(weapon, weaponInfo.Scale);

            LoadLibrary(XRayTestData.WEAPON_OMF).Find("reload").Clip.SampleAnimation(hands, 1.0f);
            LoadLibrary(XRayTestData.HANDS_OMF).Find("ak74_reload").Clip.SampleAnimation(weapon, 1.0f);

            List<XVector3[]> handsAfter = Bake(hands, handsInfo.Scale);
            List<XVector3[]> weaponAfter = Bake(weapon, weaponInfo.Scale);
            for (int m = 0; m < handsRest.Count; m++)
            {
                Assert.Less(MaxDistance(handsRest[m], handsAfter[m]), 1e-6, "hands mesh " + m);
            }

            for (int m = 0; m < weaponRest.Count; m++)
            {
                Assert.Less(MaxDistance(weaponRest[m], weaponAfter[m]), 1e-6, "weapon mesh " + m);
            }
        }

        [Test]
        public void RigScaleChange_ReimportsDependentMotions()
        {
            XRayTestData.Require(XRayTestData.HANDS_OGF);
            var importer = (XRayOgfImporter)AssetImporter.GetAtPath(XRayTestData.HANDS_OGF);
            float original = importer.Scale;
            float before = ReadFirstKey(XRayTestData.HANDS_OMF, "ak74_idle", "bip01/l_clavicle", "m_LocalPosition.x");
            Assert.AreNotEqual(0.0f, before, "test needs a non-zero translation key");
            try
            {
                SetScale(importer, original * 2.0f);
                float after = ReadFirstKey(XRayTestData.HANDS_OMF, "ak74_idle", "bip01/l_clavicle", "m_LocalPosition.x");
                Assert.AreEqual(before * 2.0f, after, 1e-5f, "OMF clips were not regenerated with the new rig scale");
            }
            finally
            {
                SetScale(importer, original);
            }

            Assert.AreEqual(before, ReadFirstKey(XRayTestData.HANDS_OMF, "ak74_idle", "bip01/l_clavicle", "m_LocalPosition.x"), 1e-6f);
        }

        [Test]
        public void HandsClips_MatchBlenderInUnity()
        {
            VerifySampledFrames(XRayTestData.HANDS_OGF, XRayTestData.HANDS_OMF, XRayTestData.HANDS_VERTEX_REFERENCE);
        }

        [Test]
        public void WeaponClips_MatchBlenderInUnity()
        {
            VerifySampledFrames(XRayTestData.WEAPON_OGF, XRayTestData.WEAPON_OMF, XRayTestData.WEAPON_VERTEX_REFERENCE);
        }

        /// <summary>
        /// For every reference frame: sample the imported clip on the imported prefab, then compare
        /// (1) bone model-space matrices with the Core evaluation and with Blender, and
        /// (2) BakeMesh vertices with Blender's armature-deformed vertices.
        /// </summary>
        private void VerifySampledFrames(string ogfPath, string omfPath, string referencePrefix)
        {
            BlenderVertexReference reference = XRayTestData.VertexReference(referencePrefix);
            OgfModel model = XRayTestData.Ogf(ogfPath);
            OmfData motions = XRayTestData.Omf(omfPath);
            var skeleton = new XRaySkeleton(model.Bones);
            XRayMotionLibrary library = LoadLibrary(omfPath);
            GameObject instance = Instantiate(ogfPath);
            XRayModel info = instance.GetComponent<XRayModel>();
            double scale = info.Scale;
            var report = new System.Text.StringBuilder();

            foreach (BlenderVertexReference.Frame frame in reference.Frames)
            {
                XRayMotionEntry entry = library.Find(frame.Action);
                Assert.IsNotNull(entry, frame.Action);
                OmfMotion motion = motions.FindMotion(frame.Action);
                int key = Mathf.Min(frame.FrameIndex, motion.FrameCount - 1);
                entry.Clip.SampleAnimation(instance, key / 30.0f);
                XMatrix4[] expected = XRayPose.EvaluateWorld(skeleton, motion, key);

                double coreError = 0.0;
                double blenderError = 0.0;
                Matrix4x4 rootInverse = instance.transform.worldToLocalMatrix;
                for (int i = 0; i < info.Bones.Count; i++)
                {
                    XMatrix4 actual = ToNative(rootInverse * info.Bones[i].localToWorldMatrix, scale);
                    coreError = System.Math.Max(coreError, XMatrix4.MaxAbsDifference(actual, expected[i]));
                    blenderError = System.Math.Max(blenderError, XMatrix4.MaxAbsDifference(BlenderSpace.BoneToBlender(actual), frame.Pose[info.BoneNames[i]]));
                }

                List<XVector3[]> baked = Bake(instance, info.Scale);
                double vertexError = VertexReferenceVerifier.CompareDeformedVertices(model, baked, reference, frame, out int compared);
                report.AppendLine($"{frame.Action} f{frame.FrameIndex}: unity-vs-core {coreError:E2}, unity-vs-blender bones {blenderError:E2}, unity-vs-blender vertices {vertexError:E2} ({compared})");

                Assert.Less(coreError, MATRIX_TOLERANCE, $"{frame.Action} f{frame.FrameIndex}: Unity bones vs Core");
                Assert.Less(blenderError, MATRIX_TOLERANCE, $"{frame.Action} f{frame.FrameIndex}: Unity bones vs Blender");
                Assert.Less(vertexError, GEOMETRY_TOLERANCE, $"{frame.Action} f{frame.FrameIndex}: BakeMesh vs Blender");
                Assert.Greater(compared, 0);
            }

            Debug.Log(report.ToString());
        }

        private static void AssertBindings(XRayMotionLibrary library, GameObject ownRig, GameObject otherRig, int bonesPerClip)
        {
            foreach (XRayMotionEntry entry in library.Motions)
            {
                EditorCurveBinding[] bindings = AnimationUtility.GetCurveBindings(entry.Clip);
                var paths = new HashSet<string>();
                foreach (EditorCurveBinding binding in bindings)
                {
                    paths.Add(binding.path);
                    Assert.IsNotNull(ownRig.transform.Find(binding.path), $"{entry.Name}: '{binding.path}' missing on own rig");
                    Assert.IsNull(otherRig.transform.Find(binding.path), $"{entry.Name}: '{binding.path}' also resolves on the other rig");
                }

                Assert.AreEqual(bonesPerClip, paths.Count, entry.Name);
                Assert.AreEqual(bonesPerClip * 7, bindings.Length, entry.Name);
            }
        }

        private static void SetScale(XRayOgfImporter importer, float scale)
        {
            var serialized = new SerializedObject(importer);
            serialized.FindProperty("_scale").floatValue = scale;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            importer.SaveAndReimport();
            AssetDatabase.Refresh();
        }

        private static float ReadFirstKey(string omfPath, string motion, string path, string property)
        {
            AnimationClip clip = LoadLibrary(omfPath).Find(motion).Clip;
            AnimationCurve curve = AnimationUtility.GetEditorCurve(clip, EditorCurveBinding.FloatCurve(path, typeof(Transform), property));
            Assert.IsNotNull(curve, $"{motion}: no curve {path}.{property}");
            return curve.keys[0].value;
        }

        private GameObject Instantiate(string path)
        {
            XRayTestData.Require(path);
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            Assert.IsNotNull(prefab, $"'{path}' was not imported as a GameObject.");
            var instance = Object.Instantiate(prefab);
            _cleanup.Add(instance);
            return instance;
        }

        private static XRayMotionLibrary LoadLibrary(string path)
        {
            XRayTestData.Require(path);
            var library = AssetDatabase.LoadAssetAtPath<XRayMotionLibrary>(path);
            Assert.IsNotNull(library, $"'{path}' was not imported as an XRayMotionLibrary.");
            return library;
        }

        /// <summary>Deformed vertices of every SkinnedMeshRenderer in native units, ordered like OgfModel.GetMeshVisuals.</summary>
        private List<XVector3[]> Bake(GameObject instance, float scale)
        {
            var renderers = new List<SkinnedMeshRenderer>(instance.GetComponentsInChildren<SkinnedMeshRenderer>());
            renderers.Sort((a, b) => string.CompareOrdinal(a.name, b.name));
            var result = new List<XVector3[]>();
            Matrix4x4 rootInverse = instance.transform.worldToLocalMatrix;
            foreach (SkinnedMeshRenderer renderer in renderers)
            {
                var mesh = new Mesh();
                _cleanup.Add(mesh);
                renderer.BakeMesh(mesh, true);
                Vector3[] vertices = mesh.vertices;
                Matrix4x4 toRoot = rootInverse * renderer.transform.localToWorldMatrix;
                var native = new XVector3[vertices.Length];
                for (int i = 0; i < vertices.Length; i++)
                {
                    Vector3 p = toRoot.MultiplyPoint3x4(vertices[i]);
                    native[i] = new XVector3(p.x / scale, p.y / scale, p.z / scale);
                }

                result.Add(native);
            }

            return result;
        }

        private static XMatrix4 ToNative(Matrix4x4 m, double scale)
        {
            return XMatrix4.FromRows(m.m00, m.m01, m.m02, m.m03 / scale, m.m10, m.m11, m.m12, m.m13 / scale, m.m20, m.m21, m.m22, m.m23 / scale, 0, 0, 0, 1);
        }

        private static double MaxDistance(XVector3[] a, XVector3[] b)
        {
            Assert.AreEqual(a.Length, b.Length);
            double max = 0.0;
            for (int i = 0; i < a.Length; i++)
            {
                max = System.Math.Max(max, (a[i] - b[i]).Length);
            }

            return max;
        }
    }
}
