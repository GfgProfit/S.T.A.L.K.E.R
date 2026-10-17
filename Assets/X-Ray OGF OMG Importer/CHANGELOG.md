# Changelog

## 1.0.0

- `ScriptedImporter` for `*.ogf` (v4; v3 read path unverified): static, hierarchical and skeletal models,
  1-4 link skinning, SWI LOD0, HDRP/Lit materials with texture lookup, embedded motions.
- `ScriptedImporter` for `*.omf` (S_SMPARAMS v1-4, S_MOTIONS_2): one AnimationClip per motion,
  rig auto-detection or explicit rig, artifact dependency on the rig OGF, per-motion exclusion.
- Native-space math (X-Ray = Unity axes), verified against Blender 3.6 + blender-xray:
  rest/pose matrices and per-vertex skinned positions.
- Runtime: `XRayModel`, `XRayMotionLibrary`, `XRayRigPlayer` (Playables), `XRayHudAnimator` (two-rig clock).
- EditMode tests: format, Blender reference (matrices, bounds, per-vertex), imported asset end-to-end.
