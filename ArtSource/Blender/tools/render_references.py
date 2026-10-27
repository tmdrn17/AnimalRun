"""Quirky 원본 동물 참고 이미지 렌더 (references/ 폴더 갱신용)

blender -b --factory-startup --python ArtSource/Blender/tools/render_references.py
"""
import math
import sys
from pathlib import Path

import bpy
import numpy as np
from mathutils import Vector

HERE = Path(__file__).resolve().parent
sys.path.insert(0, str(HERE.parent))
import quirky_animal_kit as kit  # noqa: E402

ROOT = kit._search_up(HERE)
OUT = HERE.parent / "references"
OUT.mkdir(exist_ok=True)
SIZE = 512


def setup_scene():
    bpy.ops.wm.read_factory_settings(use_empty=True)
    sc = bpy.context.scene
    sc.render.engine = "BLENDER_WORKBENCH"
    sc.display.shading.light = "STUDIO"
    sc.display.shading.color_type = "TEXTURE"
    sc.display.shading.show_cavity = False
    sc.render.film_transparent = False
    sc.world = bpy.data.worlds.new("W")
    sc.world.color = (0.93, 0.93, 0.93)
    sc.render.resolution_x = sc.render.resolution_y = SIZE
    sc.render.image_settings.file_format = "PNG"
    sc.render.fps = 24
    return sc


def import_animal(name, fbx_index):
    rel = kit.REFERENCES[name][fbx_index] or kit.REFERENCES[name][0]
    new = kit.import_fbx(ROOT / rel)
    img = bpy.data.images.load(str(ROOT / kit.REFERENCES[name][2]))
    mesh = next(o for o in new if o.type == "MESH")
    rig = next(o for o in new if o.type == "ARMATURE")
    mat = bpy.data.materials.new("M")
    tex = mat.node_tree.nodes.new("ShaderNodeTexImage")
    tex.image, tex.interpolation = img, "Closest"
    bsdf = mat.node_tree.nodes["Principled BSDF"]
    mat.node_tree.links.new(tex.outputs["Color"], bsdf.inputs["Base Color"])
    mesh.data.materials.clear()
    mesh.data.materials.append(mat)
    return mesh, rig


def add_camera(sc, mesh, direction, ortho_pad=1.25):
    bb = [mesh.matrix_world @ Vector(c) for c in mesh.bound_box]
    center = sum(bb, Vector()) / 8
    size = max((max(v[i] for v in bb) - min(v[i] for v in bb)) for i in range(3))
    cam_data = bpy.data.cameras.new("Cam")
    cam_data.type = "ORTHO"
    cam_data.ortho_scale = size * ortho_pad
    cam = bpy.data.objects.new("Cam", cam_data)
    sc.collection.objects.link(cam)
    cam.location = center + Vector(direction).normalized() * 10
    cam.rotation_euler = (center - cam.location).to_track_quat("-Z", "Y").to_euler()
    sc.camera = cam
    return cam


def render_to_array(sc, path):
    sc.render.filepath = str(path)
    bpy.ops.render.render(write_still=True)
    img = bpy.data.images.load(str(path))
    arr = np.array(img.pixels[:], dtype=np.float32).reshape(img.size[1], img.size[0], 4)
    bpy.data.images.remove(img)
    return arr


def save_array(arr, path):
    h, w = arr.shape[:2]
    img = bpy.data.images.new(path.stem, w, h, alpha=True)
    img.pixels = arr.ravel()
    img.filepath_raw = str(path)
    img.file_format = "PNG"
    img.save()


def set_frame_action(rig, action_name, frame):
    act = next(a for a in bpy.data.actions if a.name.split("|")[-1] == action_name)
    kit.set_action(rig, act)
    bpy.context.scene.frame_set(frame)


# 1) 동물별 3/4 뷰 + 측면 (게임 정면 = 블렌더 -Y)
tiles = []
for name in kit.REFERENCES:
    sc = setup_scene()
    mesh, rig = import_animal(name, 1)
    add_camera(sc, mesh, (-1.0, -1.4, 0.7))
    a = render_to_array(sc, OUT / f"{name}_34.png")
    add_camera(sc, mesh, (-1, 0, 0.05))
    b = render_to_array(sc, OUT / f"{name}_side.png")
    (OUT / f"{name}_34.png").unlink()
    (OUT / f"{name}_side.png").unlink()
    tiles.append(np.concatenate([a, b], axis=1))
rows = [np.concatenate(tiles[i:i + 3], axis=1) for i in range(0, 9, 3)]
save_array(np.concatenate(rows[::-1], axis=0), OUT / "animals_overview.png")

# 2) 말 애니메이션 11프레임 스트립 (측면)
SIZE = 256
for clip in ("Run", "Idle_A", "Walk", "Jump", "Attack", "Death"):
    sc = setup_scene()
    sc.render.resolution_x = sc.render.resolution_y = SIZE
    mesh, rig = import_animal("Horse", 0)
    add_camera(sc, mesh, (-1, 0, 0.05), ortho_pad=1.8)
    frames = []
    for f in range(1, 12):
        set_frame_action(rig, clip, f)
        frames.append(render_to_array(sc, OUT / "_tmp.png"))
    (OUT / "_tmp.png").unlink()
    save_array(np.concatenate(frames, axis=1), OUT / f"Horse_{clip}_strip.png")

# 3) 팔레트 모음 (16x4 를 32배 확대, 위에서부터 REFERENCES 순서)
pals = []
for name in kit.REFERENCES:
    img = bpy.data.images.load(str(ROOT / kit.REFERENCES[name][2]))
    arr = np.array(img.pixels[:], dtype=np.float32).reshape(4, 16, 4)
    pals.append(np.repeat(np.repeat(arr, 16, axis=0), 32, axis=1))
    gap = np.ones((8, 512, 4), dtype=np.float32)
    pals.append(gap)
save_array(np.concatenate(pals[::-1], axis=0), OUT / "palettes.png")
print("DONE")
