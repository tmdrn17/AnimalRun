"""사양서(JSON)로 Quirky 스타일 동물 생성 -> <Name>.blend + Unity FBX/텍스처/아이콘 + Animals.tsv 행

blender -b --factory-startup --python ArtSource/Blender/tools/make_animal.py -- ArtSource/Blender/specs/<Name>.json
사양서 형식: ArtSource/Blender/specs/FORMAT.md
"""
import json
import math
import shutil
import struct
import sys
import zlib
from pathlib import Path

import bmesh
import bpy
from mathutils import Euler, Matrix, Vector

HERE = Path(__file__).resolve().parent
BLENDER_DIR = HERE.parent
REPO = BLENDER_DIR.parent.parent
sys.path.insert(0, str(BLENDER_DIR))
import quirky_animal_kit as kit  # noqa: E402

MODELS_DIR = REPO / "Assets" / "Animal Run" / "Models" / "Runners"
ICON_DIR = REPO / "Assets" / "Resources" / "Animal Run" / "Sprites" / "Runners"
TSV = REPO / "Assets" / "StreamingAssets" / "Table" / "Animal Run" / "Animals.tsv"
PREVIEW_DIR = BLENDER_DIR / "previews"


def load_spec():
    args = sys.argv[sys.argv.index("--") + 1:] if "--" in sys.argv else []
    if not args:
        raise SystemExit("사양서 경로가 필요합니다: ... make_animal.py -- ArtSource/Blender/specs/<Name>.json")
    path = Path(args[0])
    if not path.is_absolute():
        path = Path.cwd() / path
    return json.loads(path.read_text(encoding="utf-8"))


# ---------------------------------------------------------------------------
# 공용 도우미
# ---------------------------------------------------------------------------

def slot_of(p, prefix=""):
    """사양서의 1~8 슬롯 -> kit 의 (0~7, TOP/BOTTOM)"""
    return p.get(f"{prefix}slot", 1) - 1, p.get(f"{prefix}half", "top").upper()


def part_matrix(p):
    """부품 로컬(단위 도형) -> 월드. 이 행렬의 역으로 정규화 거리를 잰다"""
    s = p.get("size", (0.1, 0.1, 0.1))
    depth_half = s[2] / 2 if p["shape"] in ("cylinder", "cone") else s[2]
    rot = Euler([math.radians(a) for a in p.get("rot", (0, 0, 0))]).to_matrix().to_4x4()
    return Matrix.Translation(Vector(p.get("loc", (0, 0, 0)))) @ rot @ Matrix.Diagonal((s[0], s[1], depth_half, 1))


def select_only(ctx, objs, active):
    for o in ctx.selected_objects:
        o.select_set(False)
    for o in objs:
        o.select_set(True)
    ctx.view_layer.objects.active = active


def apply_modifiers(ctx, o):
    ctx.view_layer.objects.active = o
    for mod in list(o.modifiers):
        bpy.ops.object.modifier_apply(modifier=mod.name)


def make_shape(ctx, p):
    shape = p["shape"]
    loc = Vector(p.get("loc", (0, 0, 0)))
    rot = [math.radians(a) for a in p.get("rot", (0, 0, 0))]
    size = p.get("size", (0.1, 0.1, 0.1))
    if shape == "ellipsoid":
        bpy.ops.mesh.primitive_uv_sphere_add(segments=p.get("segments", 32), ring_count=p.get("rings", 16),
                                             radius=1, location=loc, rotation=rot)
        ctx.active_object.scale = size
    elif shape == "cylinder":
        bpy.ops.mesh.primitive_cylinder_add(vertices=p.get("segments", 16), radius=1, depth=1, location=loc, rotation=rot)
        ctx.active_object.scale = size
    elif shape == "cone":
        bpy.ops.mesh.primitive_cone_add(vertices=p.get("segments", 12), radius1=1, radius2=p.get("tip", 0.05), depth=1,
                                        location=loc, rotation=rot)
        ctx.active_object.scale = size
    elif shape == "tube":
        curve = bpy.data.curves.new(p.get("name", "tube"), "CURVE")
        curve.dimensions = "3D"
        curve.bevel_depth = p["radius"]
        curve.bevel_resolution = p.get("bevel_resolution", 3)
        curve.resolution_u = p.get("resolution", 8)
        curve.use_fill_caps = True
        spline = curve.splines.new("BEZIER")
        pts = p["points"]
        spline.bezier_points.add(len(pts) - 1)
        taper = p.get("taper", 1.0)
        for i, (bp, co) in enumerate(zip(spline.bezier_points, pts)):
            bp.co = co
            bp.handle_left_type = bp.handle_right_type = "AUTO"
            bp.radius = 1 + (taper - 1) * i / max(1, len(pts) - 1)
        o = bpy.data.objects.new(p.get("name", "tube"), curve)
        ctx.scene.collection.objects.link(o)
        select_only(ctx, [o], o)
        bpy.ops.object.convert(target="MESH")
        # flat: 경로가 놓인 평면의 법선 방향으로 단면을 눌러 칼날처럼 납작하게 (경로 평면 안의 폭은 그대로)
        if "flat" in p:
            pv = [Vector(c) for c in pts]
            normal = (pv[1] - pv[0]).cross(pv[-1] - pv[0])
            if normal.length > 1e-6:
                normal.normalize()
                center = sum(pv, Vector()) / len(pv)
                for v in o.data.vertices:
                    v.co -= normal * (1 - p["flat"]) * (v.co - center).dot(normal)
    else:
        raise ValueError(f"알 수 없는 shape: {shape}")
    o = ctx.active_object
    o.name = p.get("name", shape)
    bpy.ops.object.transform_apply(location=False, rotation=True, scale=True)
    return o


def finish_separate(ctx, p):
    """따로 붙는 부품: 모서리 정리 + 가중치 + 색"""
    o = make_shape(ctx, p)
    is_cyl = p["shape"] == "cylinder"
    bevel = p.get("bevel", 0.02 if is_cyl else 0)
    sub = p.get("subdivide", 1 if is_cyl else 0)
    if bevel:
        m = o.modifiers.new("Bevel", "BEVEL")
        m.width, m.segments = bevel, 2
    if sub:
        o.modifiers.new("Sub", "SUBSURF").levels = sub
    apply_modifiers(ctx, o)
    for poly in o.data.polygons:
        poly.use_smooth = True

    bones = p.get("bones")
    if bones and p["shape"] == "tube":
        pts = [Vector(c) for c in p["points"]]
        lengths = [0.0]
        for a, b in zip(pts, pts[1:]):
            lengths.append(lengths[-1] + (b - a).length)
        vg1, vg2 = o.vertex_groups.new(name=bones[0]), o.vertex_groups.new(name=bones[1])
        for v in o.data.vertices:
            co = o.matrix_world @ v.co
            best, t = None, 0.0
            for i, (a, b) in enumerate(zip(pts, pts[1:])):
                ab = b - a
                f = max(0.0, min(1.0, (co - a).dot(ab) / max(ab.length_squared, 1e-9)))
                d = (a + ab * f - co).length
                if best is None or d < best:
                    best, t = d, (lengths[i] + ab.length * f) / lengths[-1]
            w = max(0.0, min(1.0, (t - 0.35) / 0.3))
            vg1.add([v.index], 1 - w, "REPLACE")
            vg2.add([v.index], w, "REPLACE")
    else:
        o.vertex_groups.new(name=p.get("bone", "body")).add(range(len(o.data.vertices)), 1.0, "REPLACE")

    main = slot_of(p)
    tip = slot_of(p, "tip_") if "tip_slot" in p else None
    tip_h = p.get("tip_height", 0.0)
    paint(o, lambda c, n: tip if tip and c.z < tip_h else main)
    return o


def paint(o, fn):
    """면마다 fn(월드 중심, 월드 노멀) -> (slot, half) 칸으로 UV 를 모은다"""
    me = o.data
    if not me.uv_layers:
        me.uv_layers.new(name="UVMap")
    uv = me.uv_layers[0].data
    rot = o.matrix_world.to_3x3()
    for poly in me.polygons:
        u, v = kit.slot_uv(*fn(o.matrix_world @ poly.center, (rot @ poly.normal).normalized()))
        for li in poly.loop_indices:
            uv[li].uv = (u, v)


def join_into(ctx, target, objs):
    """target(셰이프키 있어도 됨)에 objs 를 합치고 머티리얼을 1개로"""
    if not objs:
        return
    uv_name = target.data.uv_layers[0].name if target.data.uv_layers else "UVMap"
    # 원본 LOD0 의 정점색(Col)이 있으면 새 부품도 흰색으로 채운다. 없으면 join 후 0(검정)이 되어 Unity Particles/Lit 에서 까맣게 나온다
    col = target.data.color_attributes.get("Col")
    for o in objs:
        if o.data.uv_layers:
            o.data.uv_layers[0].name = uv_name
        if col:
            attr = o.data.color_attributes.new(col.name, col.data_type, col.domain)
            for d in attr.data:
                d.color = (1.0, 1.0, 1.0, 1.0)
    select_only(ctx, [target] + objs, target)
    bpy.ops.object.join()
    me = target.data
    for poly in me.polygons:
        poly.material_index = 0
    while len(me.materials) > 1:
        me.materials.pop()


def dominant_group(obj, v):
    if not v.groups:
        return None
    return obj.vertex_groups[max(v.groups, key=lambda g: g.weight).group].name


def strip_bone_curves(names):
    for act in bpy.data.actions:
        bags = [bag for layer in act.layers for strip in layer.strips for bag in strip.channelbags] \
            if hasattr(act, "layers") and len(act.layers) else [act]
        for bag in bags:
            for fc in [fc for fc in bag.fcurves if any(f'"{n}"' in fc.data_path for n in names)]:
                bag.fcurves.remove(fc)


def remove_bones(ctx, rig, names):
    names = [n for n in names if n in rig.data.bones]
    if not names:
        return
    select_only(ctx, [rig], rig)
    bpy.ops.object.mode_set(mode="EDIT")
    for n in names:
        rig.data.edit_bones.remove(rig.data.edit_bones[n])
    bpy.ops.object.mode_set(mode="OBJECT")
    strip_bone_curves(names)


def set_bones(ctx, rig, bones):
    S = rig.scale.x
    select_only(ctx, [rig], rig)
    bpy.ops.object.mode_set(mode="EDIT")
    eb = rig.data.edit_bones
    for name, (h, t) in bones.items():
        b = eb[name]
        b.use_connect = False
        b.head, b.tail = Vector(h) / S, Vector(t) / S
    if "tail.02" in eb and "tail.02" in bones:
        eb["tail.02"].use_connect = True
    bpy.ops.object.mode_set(mode="OBJECT")


def bind_to_rig(mesh, rig):
    if not any(m.type == "ARMATURE" for m in mesh.modifiers):
        mesh.modifiers.new("Armature", "ARMATURE")
    for m in mesh.modifiers:
        if m.type == "ARMATURE":
            m.object = rig
    mw = mesh.matrix_world.copy()
    mesh.parent = rig
    mesh.matrix_world = mw


# ---------------------------------------------------------------------------
# reference_mesh 모드: 원본 LOD0 를 고쳐 쓴다
# ---------------------------------------------------------------------------

def build_from_reference(ctx, spec, rig):
    base = spec["base"]
    ref = base["reference"]
    lod = kit.REFERENCES[ref][1]
    if lod is None:
        raise SystemExit(f"{ref} 는 LOD0(셰이프키) 파일이 없어 reference_mesh 모드를 쓸 수 없습니다. procedural 로 하세요.")
    new = kit.import_fbx(kit.quirky_root(ctx) / lod)
    mesh = next(o for o in new if o.type == "MESH" and o.data.shape_keys)
    bind_to_rig(mesh, rig)
    for o in new:
        if o is not mesh:
            bpy.data.objects.remove(o)
    for c in list(mesh.users_collection):
        c.objects.unlink(mesh)
    ctx.scene.collection.objects.link(mesh)
    mesh.name = "Mesh_LOD0"
    me = mesh.data

    # 부위 삭제 (bmesh 는 셰이프키 레이어를 유지한다). remove_mesh 는 메시만 지우고 본·동작은 남겨 새 부품을 붙인다
    remove = set(base.get("remove_parts", [])) | set(base.get("remove_mesh", []))
    if remove:
        idx = {mesh.vertex_groups[n].index for n in remove if n in mesh.vertex_groups}
        bm = bmesh.new()
        bm.from_mesh(me)
        dl = bm.verts.layers.deform.active
        doomed = [v for v in bm.verts if v[dl] and max(v[dl].items(), key=lambda kv: kv[1])[0] in idx]
        bmesh.ops.delete(bm, geom=doomed, context="VERTS")
        bm.to_mesh(me)
        bm.free()
        me.update()
        for n in remove:
            if n in mesh.vertex_groups:
                mesh.vertex_groups.remove(mesh.vertex_groups[n])
        remove_bones(ctx, rig, [n for n in base.get("remove_parts", []) if n in kit.OPTIONAL_BONES])

    # 메시를 지운 부위의 본을 새 부품 자리로 (남은 원본 메시가 붙은 본은 옮기지 않는다)
    if spec.get("bones"):
        set_bones(ctx, rig, spec["bones"])

    # 팔레트 칸으로 부위 삭제 (페가수스 뿔처럼 본이 따로 없는 부위)
    cells = {(s - 1, h.upper()) for s, h in base.get("remove_slots", [])}
    if cells:
        bm = bmesh.new()
        bm.from_mesh(me)
        uv = bm.loops.layers.uv.active
        doomed = [f for f in bm.faces if kit.uv_to_slot(*f.loops[0][uv].uv) in cells]
        bmesh.ops.delete(bm, geom=doomed, context="FACES")
        bm.to_mesh(me)
        bm.free()
        me.update()

    # 부위 크기 (본 머리 기준, 셰이프키 전부)
    mw, mw_inv = mesh.matrix_world, mesh.matrix_world.inverted()
    for bone, scale in base.get("part_scale", {}).items():
        pivot = rig.matrix_world @ rig.data.bones[bone].head_local
        ids = [v.index for v in me.vertices if dominant_group(mesh, v) == bone]
        for kb in me.shape_keys.key_blocks:
            for i in ids:
                w = mw @ kb.data[i].co
                kb.data[i].co = mw_inv @ (pivot + Vector(scale) * (w - pivot))
        for i in ids:
            me.vertices[i].co = me.shape_keys.key_blocks[0].data[i].co

    # 팔레트 칸 옮기기
    rules = base.get("repaint", [])
    if rules:
        uv = me.uv_layers[0].data
        doomed = set()
        for poly in me.polygons:
            cur = kit.uv_to_slot(*uv[poly.loop_indices[0]].uv)
            group = dominant_group(mesh, me.vertices[poly.vertices[0]])
            z = (mw @ poly.center).z
            for r in rules:
                src = (r["from"][0] - 1, r["from"][1].upper())
                if (cur == src and r.get("group") in (None, group)
                        and r.get("z_min", -1e9) <= z <= r.get("z_max", 1e9)):
                    if r.get("delete"):
                        doomed.add(poly.index)
                        break
                    u, v = kit.slot_uv(r["to"][0] - 1, r["to"][1].upper())
                    for li in poly.loop_indices:
                        uv[li].uv = (u, v)
                    break
        # delete 규칙: 같은 칸·같은 본이라 remove_slots 로 못 가르는 부위(Kirin 뿔)를 높이로 골라 지운다
        if doomed:
            bm = bmesh.new()
            bm.from_mesh(me)
            bm.faces.ensure_lookup_table()
            bmesh.ops.delete(bm, geom=[bm.faces[i] for i in doomed], context="FACES")
            bm.to_mesh(me)
            bm.free()
            me.update()

    join_into(ctx, mesh, [finish_separate(ctx, p) for p in spec.get("parts", [])])
    return mesh


# ---------------------------------------------------------------------------
# procedural 모드: 도형으로 새로 만든다
# ---------------------------------------------------------------------------

def build_procedural(ctx, spec, rig):
    base = spec["base"]
    set_bones(ctx, rig, spec["bones"])
    parts = spec.get("parts", [])
    merged = [p for p in parts if p.get("merge", True)]
    separate = [p for p in parts if not p.get("merge", True)]
    if not merged:
        raise SystemExit("procedural 모드에는 merge 부품(몸통)이 하나 이상 필요합니다.")

    pieces = [make_shape(ctx, p) for p in merged]
    select_only(ctx, pieces, pieces[0])
    bpy.ops.object.join()
    blob = ctx.active_object
    blob.name = "Mesh_LOD0"
    # 원점이 첫 부품 위치에 남아 있으면 아래에서 matrix_basis 를 단위행렬로 바꿀 때 메시가 그만큼 밀린다
    bpy.ops.object.transform_apply(location=True, rotation=False, scale=False)
    rm = blob.modifiers.new("Remesh", "REMESH")
    rm.mode, rm.voxel_size, rm.adaptivity = "VOXEL", base.get("voxel", 0.012), 0.0
    sm = blob.modifiers.new("Smooth", "CORRECTIVE_SMOOTH")
    sm.iterations, sm.use_only_smooth = 6, True
    apply_modifiers(ctx, blob)
    dec = blob.modifiers.new("Decimate", "DECIMATE")
    dec.ratio = min(1.0, base.get("target_tris", 3600) / max(1, sum(len(p.vertices) - 2 for p in blob.data.polygons)))
    apply_modifiers(ctx, blob)

    # 가장 가까운 merge 부품의 본·색 (부품 크기로 정규화한 거리)
    inv = [part_matrix(p).inverted() for p in merged]

    def nearest(co):
        return merged[min(range(len(merged)), key=lambda i: (inv[i] @ co).length)]

    groups = {}
    for v in blob.data.vertices:
        bone = nearest(blob.matrix_world @ v.co).get("bone", "body")
        if bone not in groups:
            groups[bone] = blob.vertex_groups.new(name=bone)
        groups[bone].add([v.index], 1.0, "REPLACE")
    paint(blob, lambda c, n: slot_of(nearest(c)))

    join_into(ctx, blob, [finish_separate(ctx, p) for p in separate])
    select_only(ctx, [blob], blob)
    bpy.ops.object.shade_smooth()
    # 눈 키트(원본 LOD0)에는 정점색 Col 이 있어, 몸에 없으면 합칠 때 몸이 0(검정)이 되어 Unity Particles/Lit 에서 까맣게 나온다
    if "Col" not in blob.data.color_attributes:
        col = blob.data.color_attributes.new("Col", "BYTE_COLOR", "CORNER")
        for d in col.data:
            d.color = (1.0, 1.0, 1.0, 1.0)
    attach_face_kit(ctx, spec["face_kit"], blob)
    bind_to_rig(blob, rig)
    # 리그 스케일(Pegasus 0.56 등)을 부모 역행렬로 흡수해 메시 자체 스케일은 1 로 (Ctrl+P 와 같음)
    blob.matrix_parent_inverse = rig.matrix_world.inverted()
    blob.matrix_basis = Matrix.Identity(4)
    return blob


def attach_face_kit(ctx, fk_spec, body):
    qak = ctx.scene.qak
    prev_ref = qak.reference
    qak.reference = fk_spec.get("reference", "Horse")
    bpy.ops.qak.import_face_kit()
    qak.reference = prev_ref
    fk = ctx.active_object
    center = sum((fk.matrix_world @ Vector(c) for c in fk.bound_box), Vector()) / 8
    fk.matrix_world = (Matrix.Translation(Vector(fk_spec["center"])) @ Matrix.Diagonal((*fk_spec.get("scale", (1, 1, 1)), 1))
                       @ Matrix.Translation(-center) @ fk.matrix_world)
    # 셰이프키까지 월드 좌표로 굳힘 (Mesh.transform 은 셰이프키를 바꾸지 않음)
    keys = fk.data.shape_keys.key_blocks
    mw = fk.matrix_world.copy()
    for kb in keys:
        for d in kb.data:
            d.co = mw @ d.co
    fk.matrix_world = Matrix.Identity(4)
    # 옆을 보는 눈을 앞(-Y)으로 돌린다 (모든 셰이프키에 같은 변환)
    turn, push = fk_spec.get("turn_deg", 0), Vector((0.0, -fk_spec.get("push", 0.0), 0.0))
    for side in (1, -1):
        idx = [v.index for v in fk.data.vertices if v.co.x * side > 0]
        if not idx:
            continue
        pivot = sum((keys[0].data[i].co for i in idx), Vector()) / len(idx)
        rot = Matrix.Rotation(math.radians(-turn * side), 3, "Z")
        for kb in keys:
            for i in idx:
                kb.data[i].co = pivot + rot @ (kb.data[i].co - pivot) + push
        for i in idx:
            fk.data.vertices[i].co = keys[0].data[i].co
    select_only(ctx, [fk, body], body)
    bpy.ops.qak.join_face_kit()


# ---------------------------------------------------------------------------
# 비늘 셰이더 (사양서 scales)
# ---------------------------------------------------------------------------

def bake_rest_uv(mesh):
    """기본 자세의 위치·노멀(월드, m)을 UV 2~4 에 굽는다. Unity Animal Run/Quirky Scales 가 이걸로 비늘을 그려
    뛰어도 무늬가 피부에 붙어 있다. UV2=(px,py) UV3=(pz,nx) UV4=(ny,nz)"""
    me = mesh.data
    mw = mesh.matrix_world
    nm = mw.to_3x3().inverted().transposed()
    layers = []
    for n in ("RestA", "RestB", "RestC"):
        if n in me.uv_layers:
            me.uv_layers.remove(me.uv_layers[n])
        layers.append(me.uv_layers.new(name=n))
    corner_normals = me.corner_normals
    for li, loop in enumerate(me.loops):
        p = mw @ me.vertices[loop.vertex_index].co
        n = (nm @ corner_normals[li].vector).normalized()
        layers[0].data[li].uv = (p.x, p.y)
        layers[1].data[li].uv = (p.z, n.x)
        layers[2].data[li].uv = (n.y, n.z)
    me.uv_layers.active_index = 0


def write_scales_settings(spec, unity_dir):
    """RunnerPrefabBuilder 가 읽는 <Name>.scales.json. 사양서에 scales 가 없으면 지워 기본 셰이더로 돌린다"""
    path = unity_dir / f"{spec['name']}.scales.json"
    scales = spec.get("scales")
    if not scales:
        if path.exists():
            path.unlink()
        return
    slots = [0.0] * 8
    for s in scales.get("slots", [1]):
        slots[s - 1] = 1.0
    path.write_text(json.dumps({
        "density": scales.get("density", 16),
        "strength": scales.get("strength", 0.35),
        "slots": slots,
    }), encoding="utf-8")


# ---------------------------------------------------------------------------
# 반투명 팔레트 (사양서 alpha)
# ---------------------------------------------------------------------------

def apply_palette_alpha(name, alpha):
    """팔레트 칸별 알파를 T_<name>.png 에 넣고 블렌더 머티리얼을 반투명으로. Unity 빌더는 알파가 있으면 Transparent 로 만든다"""
    img = bpy.data.images[f"T_{name}"]
    px = list(img.pixels)
    for y in range(4):
        values = alpha.get("top" if y >= 2 else "bottom") or []
        for x in range(16):
            a = values[x // 2] if x // 2 < len(values) else None
            px[(y * 16 + x) * 4 + 3] = 1.0 if a is None else float(a)
    # alpha=False 이미지는 블렌더 저장 때 알파가 빠지므로 PNG 를 직접 쓴다 (블렌더 픽셀은 아래 줄부터)
    rows = b"".join(
        b"\0" + bytes(round(max(0.0, min(1.0, v)) * 255) for v in px[y * 64:(y + 1) * 64])
        for y in reversed(range(4)))

    def chunk(tag, data):
        return struct.pack(">I", len(data)) + tag + data + struct.pack(">I", zlib.crc32(tag + data))
    png = (b"\x89PNG\r\n\x1a\n" + chunk(b"IHDR", struct.pack(">IIBBBBB", 16, 4, 8, 6, 0, 0, 0))
           + chunk(b"IDAT", zlib.compress(rows)) + chunk(b"IEND", b""))
    out = BLENDER_DIR / "textures" / f"T_{name}.png"
    out.write_bytes(png)
    # 쓴 PNG 를 다시 읽어 메모리 이미지에도 알파를 넣는다 (픽셀을 직접 고치면 저장 때 RGB 로 덮어씀)
    img.filepath_raw = str(out)
    img.source = "FILE"
    img.reload()
    img.alpha_mode = "STRAIGHT"

    mat = bpy.data.materials[f"M_{name}"]
    nt = mat.node_tree
    tex = next(n for n in nt.nodes if n.type == "TEX_IMAGE")
    bsdf = next(n for n in nt.nodes if n.type == "BSDF_PRINCIPLED")
    nt.links.new(tex.outputs["Alpha"], bsdf.inputs["Alpha"])
    if hasattr(mat, "surface_render_method"):
        mat.surface_render_method = "BLENDED"
    else:
        mat.blend_method = "BLEND"
    mat.use_transparency_overlap = False  # Unity 처럼 ZWrite 켠 반투명 (몸 안쪽 면은 안 보임)


# ---------------------------------------------------------------------------
# 렌더 (아이콘 / 비교용 미리보기)
# ---------------------------------------------------------------------------

def render_views(ctx, mesh, rig, views, size, path_fn, transparent):
    scene = ctx.scene
    scene.render.engine = "BLENDER_EEVEE"
    scene.render.film_transparent = transparent
    scene.render.resolution_x = scene.render.resolution_y = size
    scene.render.image_settings.file_format = "PNG"
    scene.render.image_settings.color_mode = "RGBA"
    scene.view_settings.view_transform = "Standard"
    scene.world = scene.world or bpy.data.worlds.new("RenderWorld")
    scene.world.color = (0.6, 0.6, 0.6)
    sun = bpy.data.objects.new("RenderSun", bpy.data.lights.new("RenderSun", "SUN"))
    sun.data.energy = 3.0
    sun.rotation_euler = (math.radians(40), math.radians(-20), math.radians(-30))
    scene.collection.objects.link(sun)
    cam = bpy.data.objects.new("RenderCam", bpy.data.cameras.new("RenderCam"))
    cam.data.type = "ORTHO"
    scene.collection.objects.link(cam)
    scene.camera = cam
    bb = [mesh.matrix_world @ Vector(c) for c in mesh.bound_box]
    center = sum(bb, Vector()) / 8
    cam.data.ortho_scale = max(max(v[i] for v in bb) - min(v[i] for v in bb) for i in range(3)) * 1.3
    for key, direction, action, frame in views:
        kit.set_action(rig, bpy.data.actions[action])
        scene.frame_set(frame)
        cam.location = center + Vector(direction).normalized() * 10
        cam.rotation_euler = (center - cam.location).to_track_quat("-Z", "Y").to_euler()
        scene.render.filepath = str(path_fn(key))
        bpy.ops.render.render(write_still=True)
    kit.set_action(rig, bpy.data.actions["Idle_A"])
    for o in (sun, cam):
        bpy.data.objects.remove(o)


# ---------------------------------------------------------------------------
# Animals.tsv
# ---------------------------------------------------------------------------

def register_table(spec):
    name = spec["name"]
    prefab_key = f"Animal Run/Prefabs/Runners/{name}"
    text = TSV.read_bytes().decode("utf-8")
    nl = "\r\n" if "\r\n" in text else "\n"
    rows = [r.split("\t") for r in text.split(nl)[1:] if r.strip()]
    if any(len(r) > 2 and r[2] == prefab_key for r in rows):
        print(f"[TABLE] {prefab_key} 행이 이미 있음")
        return
    row = "\t".join([str(max(int(r[0]) for r in rows) + 1), spec["display_name"], prefab_key,
                     f"Animal Run/Sprites/Runners/{name}_Image", spec.get("kind", "Medium")])
    TSV.write_bytes((text.rstrip("\r\n") + nl + row).encode("utf-8"))
    print(f"[TABLE] 추가: {row}")


# ---------------------------------------------------------------------------
# 실행
# ---------------------------------------------------------------------------

def main():
    spec = load_spec()
    name = spec["name"]
    unity_dir = MODELS_DIR / name

    kit.register()
    bpy.ops.wm.read_factory_settings(use_empty=True)
    bpy.ops.wm.save_as_mainfile(filepath=str(BLENDER_DIR / f"{name}.blend"))
    ctx = bpy.context
    qak = ctx.scene.qak
    qak.reference = spec["base"]["reference"]
    qak.animal_name = name
    qak.export_dir = str(unity_dir) + "\\"

    bpy.ops.qak.start_from_reference()
    rig = bpy.data.objects["Rig"]

    mode = spec["base"].get("mode", "reference_mesh")
    if mode == "reference_mesh":
        mesh = build_from_reference(ctx, spec, rig)
    elif mode == "procedural":
        mesh = build_procedural(ctx, spec, rig)
    else:
        raise SystemExit(f"알 수 없는 base.mode: {mode}")

    for o in list(bpy.data.objects):
        if o.name.startswith(kit.PREFIX_REF):
            bpy.data.objects.remove(o)
    if spec.get("scales"):
        bake_rest_uv(mesh)
    rig.scale *= spec.get("size", 1.0)

    # 팔레트: 원본 색에서 시작해 사양서 값으로 덮어쓰기
    bpy.ops.qak.load_reference_palette()
    pal = spec.get("palette", {})
    for i, slot in enumerate(qak.palette):
        for half in ("top", "bottom"):
            colors = pal.get(half) or []
            if i < len(colors) and colors[i]:
                setattr(slot, half, kit._hex(colors[i]))
    select_only(ctx, [mesh], mesh)
    bpy.ops.qak.build_palette()
    if spec.get("alpha"):
        apply_palette_alpha(name, spec["alpha"])

    kit.set_action(rig, bpy.data.actions["Idle_A"])
    for lvl, msg in kit.validate(ctx):
        print("[VALIDATE]", lvl, msg)
    bpy.ops.wm.save_mainfile()

    unity_dir.mkdir(parents=True, exist_ok=True)
    write_scales_settings(spec, unity_dir)
    bpy.ops.qak.export_fbx(filepath=str(unity_dir / f"{name}.fbx"))
    shutil.copy(BLENDER_DIR / "textures" / f"T_{name}.png", unity_dir / f"T_{name}.png")

    render_views(ctx, mesh, rig, [("icon", (-1.0, -1.4, 0.7), "Idle_A", 1)], 256,
                 lambda key: ICON_DIR / f"{name}_Image.png", transparent=True)
    PREVIEW_DIR.mkdir(exist_ok=True)
    render_views(ctx, mesh, rig, [
        ("34", (-1.0, -1.4, 0.7), "Idle_A", 1),
        ("side", (1.0, 0.0, 0.15), "Idle_A", 1),
        ("front", (0.0, -1.0, 0.1), "Idle_A", 1),
        ("run", (1.0, 0.0, 0.15), "Run", 4),
    ], 512, lambda key: PREVIEW_DIR / f"{name}_{key}.png", transparent=False)

    register_table(spec)
    me = mesh.data
    print("tris", sum(len(p.vertices) - 2 for p in me.polygons), "dims", tuple(round(x, 3) for x in mesh.dimensions),
          "keys", len(me.shape_keys.key_blocks) - 1 if me.shape_keys else 0, "bones", len(rig.data.bones))
    print("DONE")


main()
