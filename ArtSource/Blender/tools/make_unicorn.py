"""노란 유니콘 생성 -> Unicorn.blend 저장 + Unity 로 FBX/텍스처/아이콘 내보내기

페가수스 LOD0 는 이미 뿔이 있는 말 모양이라, 날개만 떼고 팔레트를 바꿔 유니콘으로 만든다.
(뿔은 팔레트 슬롯 5 아래 칸을 따로 쓰므로 갈기와 다른 색을 줄 수 있음)

blender -b --factory-startup --python ArtSource/Blender/tools/make_unicorn.py
"""
import math
import shutil
import sys
from pathlib import Path

import bmesh
import bpy
from mathutils import Vector

HERE = Path(__file__).resolve().parent
BLENDER_DIR = HERE.parent
REPO = BLENDER_DIR.parent.parent
sys.path.insert(0, str(BLENDER_DIR))
import quirky_animal_kit as kit  # noqa: E402

UNITY_DIR = REPO / "Assets" / "Animal Run" / "Models" / "Runners" / "Unicorn"
NAME = "Unicorn"
WING_BONES = ("wing.L", "wing.R")

# 페가수스 슬롯 용도: 1 몸, 2 다리·귀 안쪽, 3 발굽, 4 갈기·꼬리, 5 갈기 그림자(+날개), 5아래 뿔
PALETTE_TOP = ["#ffd84f", "#f5b731", "#8a5a2b", "#ff8fc4", "#c27ddc", "#ffffff", "#f9f8f2", "#292939"]
PALETTE_BOTTOM = ["#ffd84f", "#f5b731", "#8a5a2b", "#ff8fc4", "#fff4dc", "#cc7d95", "#fff263", "#63cdff"]

kit.register()
bpy.ops.wm.read_factory_settings(use_empty=True)
blend_path = BLENDER_DIR / f"{NAME}.blend"
bpy.ops.wm.save_as_mainfile(filepath=str(blend_path))
ctx = bpy.context
scene = ctx.scene
qak = scene.qak
qak.reference = "Pegasus"
qak.animal_name = NAME
qak.export_dir = str(UNITY_DIR) + "\\"

# ---------------------------------------------------------------- 1. 리그 + 애니메이션
bpy.ops.qak.start_from_reference()
rig = bpy.data.objects["Rig"]

# ---------------------------------------------------------------- 2. 모델: 페가수스 LOD0 (셰이프키 29개 포함)
new = kit.import_fbx(kit.quirky_root(ctx) / kit.REFERENCES["Pegasus"][1])
mesh = next(o for o in new if o.type == "MESH")
mw = mesh.matrix_world.copy()
mesh.parent = rig
mesh.matrix_world = mw
for m in mesh.modifiers:
    if m.type == "ARMATURE":
        m.object = rig
for o in new:
    if o is not mesh:
        bpy.data.objects.remove(o)
for c in list(mesh.users_collection):
    c.objects.unlink(mesh)
scene.collection.objects.link(mesh)
mesh.name = "Mesh_LOD0"  # 표정 anim(Eyes_*) 이 Mesh_LOD0 경로를 씀

# 날개 섬 삭제 (날개 본이 주 가중치인 버텍스)
me = mesh.data
wing_groups = {mesh.vertex_groups[b].index for b in WING_BONES}
bm = bmesh.new()
bm.from_mesh(me)
dl = bm.verts.layers.deform.active
wing_verts = [v for v in bm.verts if v[dl] and max(v[dl].items(), key=lambda kv: kv[1])[0] in wing_groups]
bmesh.ops.delete(bm, geom=wing_verts, context="VERTS")
bm.to_mesh(me)  # 셰이프키 레이어도 같이 유지됨
bm.free()
me.update()
for b in WING_BONES:
    mesh.vertex_groups.remove(mesh.vertex_groups[b])

# 날개 본과 그 애니메이션 커브 삭제 -> 본 10개 표준
ctx.view_layer.objects.active = rig
bpy.ops.object.mode_set(mode="EDIT")
for b in WING_BONES:
    rig.data.edit_bones.remove(rig.data.edit_bones[b])
bpy.ops.object.mode_set(mode="OBJECT")
for act in bpy.data.actions:
    bags = [bag for layer in act.layers for strip in layer.strips for bag in strip.channelbags] \
        if hasattr(act, "layers") and len(act.layers) else [act]
    for bag in bags:
        for fc in [fc for fc in bag.fcurves if any(f'"{b}"' in fc.data_path for b in WING_BONES)]:
            bag.fcurves.remove(fc)

# 참조 메시 삭제
for o in list(bpy.data.objects):
    if o.name.startswith(kit.PREFIX_REF):
        bpy.data.objects.remove(o)

# ---------------------------------------------------------------- 3. 팔레트 (UV 는 원본 그대로, 색만 교체)
kit.ensure_palette(qak)
for i in range(8):
    qak.palette[i].top = kit._hex(PALETTE_TOP[i])
    qak.palette[i].bottom = kit._hex(PALETTE_BOTTOM[i])
for o in ctx.selected_objects:
    o.select_set(False)
mesh.select_set(True)
ctx.view_layer.objects.active = mesh
bpy.ops.qak.build_palette()

kit.set_action(rig, bpy.data.actions["Idle_A"])
for lvl, msg in kit.validate(ctx):
    print("[VALIDATE]", lvl, msg)
bpy.ops.wm.save_mainfile()

# ---------------------------------------------------------------- 4. 내보내기
UNITY_DIR.mkdir(parents=True, exist_ok=True)
bpy.ops.qak.export_fbx(filepath=str(UNITY_DIR / f"{NAME}.fbx"))
shutil.copy(BLENDER_DIR / "textures" / f"T_{NAME}.png", UNITY_DIR / f"T_{NAME}.png")

# ---------------------------------------------------------------- 5. 선택 UI 아이콘 (256px, 투명, 3/4 뷰)
ICON = REPO / "Assets" / "Resources" / "Animal Run" / "Sprites" / "Runners" / f"{NAME}_Image.png"
scene.render.engine = "BLENDER_EEVEE"
scene.render.film_transparent = True
scene.render.resolution_x = scene.render.resolution_y = 256
scene.render.image_settings.file_format = "PNG"
scene.render.image_settings.color_mode = "RGBA"
scene.view_settings.view_transform = "Standard"
scene.world = bpy.data.worlds.new("IconWorld")
scene.world.color = (0.6, 0.6, 0.6)
sun = bpy.data.objects.new("IconSun", bpy.data.lights.new("IconSun", "SUN"))
sun.data.energy = 3.0
sun.rotation_euler = (math.radians(40), math.radians(-20), math.radians(-30))
scene.collection.objects.link(sun)
cam = bpy.data.objects.new("IconCam", bpy.data.cameras.new("IconCam"))
cam.data.type, cam.data.ortho_scale = "ORTHO", 2.3
center = Vector((0, -0.1, 0.78))
cam.location = center + Vector((-1.0, -1.4, 0.7)).normalized() * 10
cam.rotation_euler = (center - cam.location).to_track_quat("-Z", "Y").to_euler()
scene.collection.objects.link(cam)
scene.camera = cam
scene.frame_set(1)
scene.render.filepath = str(ICON)
bpy.ops.render.render(write_still=True)
for o in (sun, cam):
    bpy.data.objects.remove(o)

print("tris", sum(len(p.vertices) - 2 for p in me.polygons), "dims", tuple(round(x, 3) for x in mesh.dimensions),
      "keys", len(me.shape_keys.key_blocks) - 1, "bones", len(rig.data.bones))
print("DONE")
