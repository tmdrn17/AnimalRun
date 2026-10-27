"""quirky_animal_kit 시험용 돼지 생성 -> Pig.blend 저장 + Unity 로 FBX/텍스처 내보내기

blender -b --factory-startup --python ArtSource/Blender/tools/make_test_pig.py
"""
import math
import shutil
import sys
from pathlib import Path

import bmesh
import bpy
from mathutils import Matrix, Vector

HERE = Path(__file__).resolve().parent
BLENDER_DIR = HERE.parent
REPO = BLENDER_DIR.parent.parent
sys.path.insert(0, str(BLENDER_DIR))
import quirky_animal_kit as kit  # noqa: E402

UNITY_DIR = REPO / "Assets" / "Animal Run" / "Models" / "Runners" / "Pig"
NAME = "Pig"

# 팔레트 (슬롯 1~6 위). 6~8 아래/7~8 위는 Quirky 공용색
PALETTE_TOP = ["#f6b3b4", "#fcd3cf", "#e98d98", "#c46b78", "#6e3b45", "#a84556", "#f9f8f2", "#292939"]
SLOT_MAIN, SLOT_LIGHT, SLOT_SNOUT, SLOT_DARK, SLOT_HOOF, SLOT_NOSTRIL = 0, 1, 2, 3, 4, 5

# 눈 키트 배치 (Horse 키트 기준)
EYE_KIT_CENTER = Vector((0.0, -0.44, 0.615))
EYE_KIT_SCALE = (0.78, 0.9, 0.9)
EYE_TURN_DEG = 40
EYE_PUSH = 0.012

kit.register()
bpy.ops.wm.read_factory_settings(use_empty=True)
blend_path = BLENDER_DIR / f"{NAME}.blend"
bpy.ops.wm.save_as_mainfile(filepath=str(blend_path))
ctx = bpy.context
scene = ctx.scene
qak = scene.qak
qak.reference = "Horse"
qak.animal_name = NAME
qak.export_dir = str(UNITY_DIR) + "\\"

# ---------------------------------------------------------------- 1. 리그
bpy.ops.qak.start_from_reference()
rig = bpy.data.objects["Rig"]
S = rig.scale.x  # 0.4 : 월드 -> 아마추어 좌표


def arm(*v):
    return Vector(v) / S


BONES = {  # 월드 좌표(m) head, tail
    "body": ((0, 0, 0.42), (0, 0, 0.82)),
    "ear.L": ((0.12, -0.34, 0.64), (0.21, -0.40, 0.80)),
    "ear.R": ((-0.12, -0.34, 0.64), (-0.21, -0.40, 0.80)),
    "tail.01": ((0, 0.44, 0.47), (0, 0.52, 0.44)),
    "tail.02": ((0, 0.52, 0.44), (0, 0.56, 0.38)),
    "leg.F.L": ((0.15, -0.17, 0.34), (0.15, -0.17, 0.0)),
    "leg.F.R": ((-0.15, -0.17, 0.34), (-0.15, -0.17, 0.0)),
    "leg.B.L": ((0.15, 0.25, 0.34), (0.15, 0.25, 0.0)),
    "leg.B.R": ((-0.15, 0.25, 0.34), (-0.15, 0.25, 0.0)),
}
ctx.view_layer.objects.active = rig
bpy.ops.object.mode_set(mode="EDIT")
eb = rig.data.edit_bones
for name, (h, t) in BONES.items():
    b = eb[name]
    b.use_connect = False
    b.head, b.tail = arm(*h), arm(*t)
eb["tail.02"].use_connect = True
bpy.ops.object.mode_set(mode="OBJECT")

# ---------------------------------------------------------------- 2. 모델
def ellipsoid(name, loc, radii, segs=48, rings=24):
    bpy.ops.mesh.primitive_uv_sphere_add(segments=segs, ring_count=rings, radius=1, location=loc)
    o = ctx.active_object
    o.name = name
    o.scale = radii
    bpy.ops.object.transform_apply(scale=True)
    return o


def cylinder(name, loc, r, depth, rot=(0, 0, 0), verts=16):
    bpy.ops.mesh.primitive_cylinder_add(vertices=verts, radius=r, depth=depth, location=loc, rotation=rot)
    o = ctx.active_object
    o.name = name
    return o


def cone(name, loc, r, depth, rot):
    bpy.ops.mesh.primitive_cone_add(vertices=12, radius1=r, radius2=0.004, depth=depth, location=loc, rotation=rot)
    o = ctx.active_object
    o.name = name
    return o


def bevel_sub(o, w=0.012, levels=2):
    m = o.modifiers.new("Bevel", "BEVEL")
    m.width, m.segments = w, 2
    s = o.modifiers.new("Sub", "SUBSURF")
    s.levels = levels
    ctx.view_layer.objects.active = o
    for mod in list(o.modifiers):
        bpy.ops.object.modifier_apply(modifier=mod.name)


blob_parts = [
    ellipsoid("body", (0, 0.06, 0.45), (0.27, 0.40, 0.25)),
    ellipsoid("head", (0, -0.30, 0.52), (0.21, 0.19, 0.19)),
    cylinder("snout", (0, -0.48, 0.48), 0.085, 0.08, rot=(math.radians(90), 0, 0), verts=24),
    cone("ear.L", (0.12, -0.33, 0.70), 0.065, 0.13, (math.radians(-15), math.radians(28), 0)),
    cone("ear.R", (-0.12, -0.33, 0.70), 0.065, 0.13, (math.radians(-15), math.radians(-28), 0)),
]
for o in blob_parts:
    o.select_set(True)
ctx.view_layer.objects.active = blob_parts[0]
bpy.ops.object.join()
blob = ctx.active_object
blob.name = "Mesh_LOD0"  # 표정 anim(Eyes_*) 이 Mesh_LOD0 경로를 씀
rm = blob.modifiers.new("Remesh", "REMESH")
rm.mode, rm.voxel_size, rm.adaptivity = "VOXEL", 0.012, 0.0
sm = blob.modifiers.new("Smooth", "CORRECTIVE_SMOOTH")
sm.iterations, sm.use_only_smooth = 6, True
dec = blob.modifiers.new("Decimate", "DECIMATE")
bpy.ops.object.modifier_apply(modifier="Remesh")
bpy.ops.object.modifier_apply(modifier="Smooth")
dec.ratio = min(1.0, 3600 / max(1, sum(len(p.vertices) - 2 for p in blob.data.polygons)))
bpy.ops.object.modifier_apply(modifier="Decimate")

legs = {}
for bone in ("leg.F.L", "leg.F.R", "leg.B.L", "leg.B.R"):
    (x, y, z), _ = BONES[bone]
    leg = cylinder(bone, (x, y, 0.20), 0.055, 0.40, verts=12)
    bevel_sub(leg, w=0.02, levels=1)
    legs[bone] = leg

bpy.ops.curve.primitive_bezier_curve_add()
tail = ctx.active_object
sp = tail.data.splines[0]
sp.bezier_points[0].co, sp.bezier_points[0].handle_left, sp.bezier_points[0].handle_right = (0, 0.42, 0.50), (0, 0.38, 0.50), (0, 0.50, 0.52)
sp.bezier_points[1].co, sp.bezier_points[1].handle_left, sp.bezier_points[1].handle_right = (0, 0.55, 0.42), (0.06, 0.58, 0.50), (-0.05, 0.52, 0.36)
tail.data.bevel_depth, tail.data.bevel_resolution, tail.data.resolution_u = 0.018, 3, 8
tail.data.use_fill_caps = True
tail.data.dimensions = "3D"
bpy.ops.object.convert(target="MESH")
tail.name = "tail"

nostrils = []
for sx in (1, -1):
    n = ellipsoid(f"nostril{sx}", (0.028 * sx, -0.522, 0.48), (0.014, 0.008, 0.022), segs=12, rings=8)
    nostrils.append(n)


def weight_all(o, group):
    vg = o.vertex_groups.new(name=group)
    vg.add(range(len(o.data.vertices)), 1.0, "REPLACE")


# 가중치: 파트별 강체 + 귀/꼬리만 그라데이션
vg_body = blob.vertex_groups.new(name="body")
vg_ears = {s: blob.vertex_groups.new(name=f"ear.{s}") for s in "LR"}
for v in blob.data.vertices:
    co = v.co
    ear = 0.0
    if co.z > 0.6 and abs(co.x) > 0.04 and co.y < -0.2:  # 머리 타원체 밖으로 튀어나온 만큼 귀
        d = math.sqrt((co.x / 0.21) ** 2 + ((co.y + 0.30) / 0.19) ** 2 + ((co.z - 0.52) / 0.19) ** 2)
        ear = max(0.0, min(1.0, (d - 1.0) / 0.12))
    if ear > 0:
        vg_ears["L" if co.x > 0 else "R"].add([v.index], ear, "REPLACE")
    vg_body.add([v.index], 1.0 - ear, "REPLACE")
for bone, leg in legs.items():
    weight_all(leg, bone)
for n in nostrils:
    weight_all(n, "body")
vg1, vg2 = tail.vertex_groups.new(name="tail.01"), tail.vertex_groups.new(name="tail.02")
for v in tail.data.vertices:
    t = max(0.0, min(1.0, (v.co.y - 0.47) / 0.07))
    vg1.add([v.index], 1 - t, "REPLACE")
    vg2.add([v.index], t, "REPLACE")

# ---------------------------------------------------------------- 3. 팔레트
kit.ensure_palette(qak)
for i, h in enumerate(PALETTE_TOP):
    qak.palette[i].top = kit._hex(h)
    if i < 5:
        qak.palette[i].bottom = kit._hex(h)


def paint(o, fn):
    me = o.data
    if not me.uv_layers:
        me.uv_layers.new(name="UVMap")
    uv = me.uv_layers[0].data
    for p in me.polygons:
        c = o.matrix_world @ p.center
        n = (o.matrix_world.to_3x3() @ p.normal).normalized()
        u, v = kit.slot_uv(fn(c, n), "TOP")
        for li in p.loop_indices:
            uv[li].uv = (u, v)


def outside_head(c):
    return math.sqrt((c.x / 0.21) ** 2 + ((c.y + 0.30) / 0.19) ** 2 + ((c.z - 0.52) / 0.19) ** 2)


def blob_slot(c, n):
    if c.y < -0.45 and math.hypot(c.x, c.z - 0.48) < 0.092:
        return SLOT_SNOUT
    if c.y < -0.2 and c.z > 0.62 and outside_head(c) > 1.08 and n.y < -0.3:  # 귀 안쪽
        return SLOT_SNOUT
    return SLOT_MAIN


paint(blob, blob_slot)
for leg in legs.values():
    paint(leg, lambda c, n: SLOT_HOOF if c.z < 0.05 else SLOT_MAIN)
paint(tail, lambda c, n: SLOT_MAIN)
for n_ in nostrils:
    paint(n_, lambda c, n: SLOT_NOSTRIL)

for o in [blob, tail] + nostrils + list(legs.values()):
    o.select_set(True)
ctx.view_layer.objects.active = blob
bpy.ops.object.join()
bpy.ops.object.shade_smooth()
for o in ctx.selected_objects:
    o.select_set(False)
blob.select_set(True)
bpy.ops.qak.build_palette()

# ---------------------------------------------------------------- 4. 표정 키트
qak.reference = "Horse"
bpy.ops.qak.import_face_kit()
fk = ctx.active_object
# 말 키트(눈) 중심을 돼지 머리 앞쪽으로 이동
fk_center = sum((fk.matrix_world @ Vector(c) for c in fk.bound_box), Vector()) / 8
fk.matrix_world = (Matrix.Translation(EYE_KIT_CENTER) @ Matrix.Diagonal((*EYE_KIT_SCALE, 1))
                   @ Matrix.Translation(-fk_center) @ fk.matrix_world)
# 셰이프키까지 월드 좌표로 굳힘 (Mesh.transform 은 셰이프키를 바꾸지 않음)
keys = fk.data.shape_keys.key_blocks
mw = fk.matrix_world.copy()
for kb in keys:
    for d in kb.data:
        d.co = mw @ d.co
fk.matrix_world = Matrix.Identity(4)

# 말 눈은 옆을 보므로 좌우 눈을 각각 앞(-Y)쪽으로 돌린다 (모든 셰이프키에 같은 변환)
for side in (1, -1):
    idx = [v.index for v in fk.data.vertices if v.co.x * side > 0]
    pivot = sum((keys[0].data[i].co for i in idx), Vector()) / len(idx)
    rot = Matrix.Rotation(math.radians(-EYE_TURN_DEG * side), 3, "Z")
    push = Vector((0.0, -EYE_PUSH, 0.0))
    for kb in keys:
        for i in idx:
            kb.data[i].co = pivot + rot @ (kb.data[i].co - pivot) + push
    for i in idx:
        fk.data.vertices[i].co = keys[0].data[i].co
fk.select_set(True)
blob.select_set(True)
ctx.view_layer.objects.active = blob
bpy.ops.qak.join_face_kit()

# 리그 연결
mod = blob.modifiers.new("Armature", "ARMATURE")
mod.object = rig
blob.parent = rig
blob.matrix_parent_inverse = rig.matrix_world.inverted()

# 참조 메시 삭제
for o in list(bpy.data.objects):
    if o.name.startswith(kit.PREFIX_REF):
        bpy.data.objects.remove(o)

kit.set_action(rig, bpy.data.actions["Idle_A"])
for lvl, msg in kit.validate(ctx):
    print("[VALIDATE]", lvl, msg)
bpy.ops.wm.save_mainfile()

# ---------------------------------------------------------------- 5. 내보내기
UNITY_DIR.mkdir(parents=True, exist_ok=True)
bpy.ops.qak.export_fbx(filepath=str(UNITY_DIR / f"{NAME}.fbx"))
shutil.copy(BLENDER_DIR / "textures" / f"T_{NAME}.png", UNITY_DIR / f"T_{NAME}.png")

# ---------------------------------------------------------------- 6. 선택 UI 아이콘 (256px, 투명, 3/4 뷰)
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
cam.data.type, cam.data.ortho_scale = "ORTHO", 1.45
center = Vector((0, -0.02, 0.40))
cam.location = center + Vector((-1.0, -1.4, 0.7)).normalized() * 10
cam.rotation_euler = (center - cam.location).to_track_quat("-Z", "Y").to_euler()
scene.collection.objects.link(cam)
scene.camera = cam
scene.frame_set(1)
scene.render.filepath = str(ICON)
bpy.ops.render.render(write_still=True)
for o in (sun, cam):
    bpy.data.objects.remove(o)

print("tris", sum(len(p.vertices) - 2 for p in blob.data.polygons), "dims", tuple(round(x, 3) for x in blob.dimensions))
print("DONE")
