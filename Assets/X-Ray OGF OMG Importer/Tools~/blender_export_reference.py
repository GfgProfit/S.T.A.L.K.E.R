# Development tool: exports independent reference data from Blender + blender-xray.
# Not required by the Unity importer at runtime or import time.
#
# Usage (Blender 3.6 with io_scene_xray installed in the user addons folder):
#   blender -b --factory-startup --python blender_export_reference.py -- \
#       <model.ogf> <motions.omf> <output_prefix> <motion[:f1,f2,...]> [<motion[:frames]> ...]
#
# Frames default to "all keys" when omitted. Writes <output_prefix>.json (metadata, bone matrices)
# and <output_prefix>.bin (float32 little-endian vertex positions, armature space, Blender axes).
#
# Every action is evaluated from a clean pose (all pose bone channels reset), without constraints,
# so the reference contains only what blender-xray derives from the OGF/OMF data.

import json
import os
import struct
import sys

import addon_utils
import bpy


def parse_args():
    argv = sys.argv[sys.argv.index('--') + 1:]
    if len(argv) < 4:
        raise SystemExit('expected: <ogf> <omf> <output_prefix> <motion[:frames]>...')
    ogf_path, omf_path, prefix = argv[0], argv[1], argv[2]
    motions = []
    for spec in argv[3:]:
        if ':' in spec:
            name, frames = spec.split(':', 1)
            motions.append((name, [int(f) for f in frames.split(',') if f]))
        else:
            motions.append((spec, None))
    return ogf_path, omf_path, prefix, motions


def reset_pose(arm):
    for pose_bone in arm.pose.bones:
        pose_bone.location = (0.0, 0.0, 0.0)
        pose_bone.rotation_quaternion = (1.0, 0.0, 0.0, 0.0)
        pose_bone.rotation_euler = (0.0, 0.0, 0.0)
        pose_bone.scale = (1.0, 1.0, 1.0)
        for constraint in list(pose_bone.constraints):
            pose_bone.constraints.remove(constraint)


def matrix_to_list(matrix):
    return [matrix[r][c] for r in range(4) for c in range(4)]


def evaluated_vertices(arm, meshes):
    depsgraph = bpy.context.evaluated_depsgraph_get()
    arm_inv = arm.matrix_world.inverted()
    result = []
    for obj in meshes:
        obj_eval = obj.evaluated_get(depsgraph)
        mesh = obj_eval.to_mesh()
        to_arm = arm_inv @ obj.matrix_world
        for vertex in mesh.vertices:
            co = to_arm @ vertex.co
            result.extend((co.x, co.y, co.z))
        obj_eval.to_mesh_clear()
    return result


def main():
    ogf_path, omf_path, prefix, motions = parse_args()
    addon_utils.enable('io_scene_xray', default_set=True)

    for obj in list(bpy.data.objects):
        bpy.data.objects.remove(obj, do_unlink=True)

    bpy.ops.xray_import.ogf(directory=os.path.dirname(ogf_path), files=[{'name': os.path.basename(ogf_path)}], import_motions=False)
    arm = next(obj for obj in bpy.data.objects if obj.type == 'ARMATURE')
    meshes = sorted((obj for obj in bpy.data.objects if obj.type == 'MESH' and obj.parent == arm), key=lambda o: o.name)

    bpy.context.view_layer.objects.active = arm
    arm.select_set(True)
    bpy.ops.xray_import.omf(directory=os.path.dirname(omf_path), files=[{'name': os.path.basename(omf_path)}], import_motions=True, import_bone_parts=False, add_to_motion_list=False)

    scene = bpy.context.scene
    floats = []
    meta = {
        'blender_version': bpy.app.version_string,
        'ogf': os.path.basename(ogf_path),
        'omf': os.path.basename(omf_path),
        'space': 'Blender armature space (blender-xray axes); vertices float32 xyz triples',
        'bones': [bone.name for bone in arm.data.bones],
        'parents': [bone.parent.name if bone.parent else '' for bone in arm.data.bones],
        'rest': {bone.name: matrix_to_list(bone.matrix_local) for bone in arm.data.bones},
        'meshes': [],
        'frames': [],
    }

    reset_pose(arm)
    arm.animation_data_create()
    arm.animation_data.action = None
    scene.frame_set(0)
    rest_vertices = evaluated_vertices(arm, meshes)
    offset = 0
    for obj in meshes:
        meta['meshes'].append({'name': obj.name, 'vertex_count': len(obj.data.vertices), 'offset': offset})
        offset += len(obj.data.vertices) * 3
    meta['rest_vertex_offset'] = len(floats)
    floats.extend(rest_vertices)

    for name, frames in motions:
        action = bpy.data.actions.get(name)
        if action is None:
            raise SystemExit('motion not found: ' + name)
        start, end = action.frame_range
        if frames is None:
            frames = list(range(int(round(start)), int(round(end)) + 1))
        reset_pose(arm)
        arm.animation_data.action = action
        for frame in frames:
            scene.frame_set(frame)
            entry = {
                'action': name,
                'frame': frame,
                'pose': {pb.name: matrix_to_list(pb.matrix) for pb in arm.pose.bones},
                'basis': {pb.name: matrix_to_list(pb.matrix_basis) for pb in arm.pose.bones},
                'vertex_offset': len(floats),
            }
            floats.extend(evaluated_vertices(arm, meshes))
            meta['frames'].append(entry)

    with open(prefix + '.bin', 'wb') as stream:
        stream.write(struct.pack('<%df' % len(floats), *floats))
    with open(prefix + '.json', 'w', encoding='utf-8') as stream:
        json.dump(meta, stream)
    print('reference written:', prefix, 'frames', len(meta['frames']), 'floats', len(floats))


main()
