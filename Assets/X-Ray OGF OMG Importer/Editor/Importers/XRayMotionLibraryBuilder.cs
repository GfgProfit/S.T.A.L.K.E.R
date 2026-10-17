using System.Collections.Generic;
using UnityEditor.AssetImporters;
using UnityEngine;
using XRay.Importer.Core.Omf;

namespace XRay.Importer.Editor
{
    /// <summary>Turns parsed motions into clip sub-assets plus an XRayMotionLibrary. Shared by the OGF and OMF importers.</summary>
    public static class XRayMotionLibraryBuilder
    {
        public static XRayMotionLibrary Build(AssetImportContext context, OmfData data, string sourceFile, string rigName, XRayRigPaths rig, float scale, XRayLoopMode loopMode, ICollection<string> excludedMotions)
        {
            var entries = new List<XRayMotionEntry>();
            var usedIds = new HashSet<string>();
            var missing = new HashSet<string>();
            int matchedTracks = 0;
            int totalTracks = 0;

            foreach (OmfMotion motion in data.Motions)
            {
                AnimationClip clip = null;
                bool excluded = excludedMotions != null && excludedMotions.Contains(motion.Name);
                if (rig != null && !excluded)
                {
                    var missingInMotion = new List<string>();
                    clip = XRayClipBuilder.Build(motion, rig, scale, loopMode, missingInMotion);
                    foreach (string bone in missingInMotion)
                    {
                        missing.Add(bone);
                    }

                    totalTracks += motion.Tracks.Count;
                    matchedTracks += motion.Tracks.Count - missingInMotion.Count;

                    string id = "clip:" + motion.Name;
                    for (int suffix = 1; !usedIds.Add(id); suffix++)
                    {
                        id = "clip:" + motion.Name + "#" + suffix;
                    }

                    context.AddObjectToAsset(id, clip);
                }

                entries.Add(CreateEntry(motion, clip));
            }

            if (rig != null && totalTracks > 0 && matchedTracks == 0)
            {
                context.LogImportError($"{sourceFile}: none of the motion bones exist in rig '{rigName}'. The motions belong to a different skeleton.");
            }
            else if (missing.Count > 0)
            {
                context.LogImportWarning($"{sourceFile}: {missing.Count} motion bones are missing in rig '{rigName}' and were skipped: {string.Join(", ", missing)}");
            }

            var boneNames = new List<string>(data.BoneNames.Values);
            var parts = new List<XRayBonePart>();
            foreach (OmfBonePart part in data.BoneParts)
            {
                parts.Add(new XRayBonePart(part.Name, new List<string>(part.BoneNames)));
            }

            var library = ScriptableObject.CreateInstance<XRayMotionLibrary>();
            library.name = sourceFile;
            library.SetImportData(sourceFile, rigName, data.ParamsVersion, (float)OmfFormat.SAMPLE_FPS, boneNames, parts, entries);
            return library;
        }

        private static XRayMotionEntry CreateEntry(OmfMotion motion, AnimationClip clip)
        {
            var marks = new List<XRayMotionMark>();
            OmfMotionParams prm = motion.Params;
            foreach (OmfMotionMark mark in prm.Marks)
            {
                var intervals = new List<Vector2>();
                for (int i = 0; i + 1 < mark.Intervals.Count; i += 2)
                {
                    intervals.Add(new Vector2((float)mark.Intervals[i], (float)mark.Intervals[i + 1]));
                }

                marks.Add(new XRayMotionMark(mark.Name, intervals));
            }

            return new XRayMotionEntry(motion.Name, clip, motion.FrameCount, prm.Flags, prm.BoneOrPart, (float)prm.Speed, (float)prm.Power, (float)prm.Accrue, (float)prm.Falloff, marks);
        }
    }
}
