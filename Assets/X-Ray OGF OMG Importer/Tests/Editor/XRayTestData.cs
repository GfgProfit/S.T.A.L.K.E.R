using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using XRay.Importer.Core.Ogf;
using XRay.Importer.Core.Omf;
using XRay.Importer.Tests.Verification;

namespace XRay.Importer.Tests
{
    /// <summary>
    /// Real game files and Blender references under Assets/test_xray. Tests that need them are
    /// reported as Ignored (not passed) when the files are absent.
    /// </summary>
    public static class XRayTestData
    {
        public const string FOLDER = "Assets/test_xray";
        public const string HANDS_OGF = FOLDER + "/wpn_hand_bandit_exo.ogf";
        public const string HANDS_OMF = FOLDER + "/wpn_hand_ak74_hud_animation.omf";
        public const string WEAPON_OGF = FOLDER + "/wpn_ak74_ekp_hud.ogf";
        public const string WEAPON_OMF = FOLDER + "/wpn_ak74_hud_animation.omf";
        public const string HANDS_JSON = FOLDER + "/xray_blender_reference_wpn_hand_bandit_exo_ogf.json";
        public const string WEAPON_JSON = FOLDER + "/xray_blender_reference_wpn_ak74_ekp_hud_ogf.json";
        public const string HANDS_VERTEX_REFERENCE = FOLDER + "/blender_reference/hands_bandit_exo";
        public const string WEAPON_VERTEX_REFERENCE = FOLDER + "/blender_reference/weapon_ak74_ekp";

        private static readonly Dictionary<string, object> CACHE = new Dictionary<string, object>();

        public static OgfModel Ogf(string path)
        {
            return Cached(path, () => OgfReader.ReadFile(File.ReadAllBytes(Require(path)), Path.GetFileName(path)));
        }

        public static OmfData Omf(string path)
        {
            return Cached(path, () => OmfReader.ReadFile(File.ReadAllBytes(Require(path)), Path.GetFileName(path)));
        }

        public static BlenderReference Json(string path)
        {
            return Cached(path, () => BlenderReference.Parse(File.ReadAllText(Require(path))));
        }

        public static BlenderVertexReference VertexReference(string prefix)
        {
            return Cached(prefix, () => BlenderVertexReference.Load(File.ReadAllText(Require(prefix + ".json")), File.ReadAllBytes(Require(prefix + ".bin"))));
        }

        public static string Require(string path)
        {
            if (!File.Exists(path))
            {
                Assert.Ignore($"Test data '{path}' is not present in the project.");
            }

            return path;
        }

        private static T Cached<T>(string key, System.Func<T> factory)
        {
            if (!CACHE.TryGetValue(key, out object value))
            {
                value = factory();
                CACHE[key] = value;
            }

            return (T)value;
        }
    }
}
