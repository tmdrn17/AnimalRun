"""AnimalRun - Quirky Animal Kit

Quirky Series 동물(말·얼룩말·낙타 등)과 같은 규격/스타일로 새 동물을 만들기 위한 블렌더 애드온.
3D 뷰포트 > N 패널 > "Quirky" 탭에서 사용한다.

설치: Edit > Preferences > Add-ons > (우측 상단 v) Install from Disk... 로 이 파일 선택
또는 Text Editor 에서 열고 Run Script (현재 세션에만 등록됨)
"""

bl_info = {
    "name": "Quirky Animal Kit (AnimalRun)",
    "author": "AnimalRun",
    "version": (1, 0, 0),
    "blender": (4, 2, 0),
    "location": "View3D > Sidebar > Quirky",
    "description": "Quirky Series 스타일 동물 모델링/팔레트 텍스처/애니메이션/FBX 내보내기 도구",
    "category": "Import-Export",
}

import os
from pathlib import Path

import bmesh
import bpy
from bpy.props import EnumProperty, FloatVectorProperty, IntProperty, PointerProperty, StringProperty, CollectionProperty

# ---------------------------------------------------------------------------
# 원본 에셋 분석으로 얻은 규격
# ---------------------------------------------------------------------------

QUIRKY_DIR = Path("Assets") / "ThirdParty" / "Quirky Series Ultimate"

# 이름: (애니메이션 FBX, LOD0 FBX(없으면 None), 팔레트 텍스처)
REFERENCES = {
    "Horse": ("Mega Pack Vol.3/Farm Vol.2/Animations/Horse_Animations.fbx", "Mega Pack Vol.3/Farm Vol.2/Models/Horse_LOD0.fbx", "Mega Pack Vol.3/Farm Vol.2/Textures/T_Horse.png"),
    "Zebra": ("Mega Pack Vol.1/Safari Vol.1/Animations/Zebra_Animations.fbx", "Mega Pack Vol.1/Safari Vol.1/Models/Zebra_LOD0.fbx", "Mega Pack Vol.1/Safari Vol.1/Textures/T_Zebra.png"),
    "Donkey": ("Mega Pack Vol.1/Farm Vol.1/Animations/Donkey_Animations.fbx", None, "Mega Pack Vol.1/Farm Vol.1/Textures/T_Donkey.png"),
    "Camel": ("Mega Pack Vol.2/Desert Vol.1/Animations/Camel_Animations.fbx", "Mega Pack Vol.2/Desert Vol.1/Models/Camel_LOD0.fbx", "Mega Pack Vol.2/Desert Vol.1/Textures/T_Camel.png"),
    "Llama": ("Mega Pack Vol.3/Farm Vol.2/Animations/Llama_Animations.fbx", "Mega Pack Vol.3/Farm Vol.2/Models/Llama_LOD0.fbx", "Mega Pack Vol.3/Farm Vol.2/Textures/T_Llama.png"),
    "Wildebeest": ("Mega Pack Vol.3/Safari Vol.2/Animations/Wildebeest_Animations.fbx", "Mega Pack Vol.3/Safari Vol.2/Models/Wildebeest_LOD0.fbx", "Mega Pack Vol.3/Safari Vol.2/Textures/T_Wildebeest.png"),
    "Mousedeer": ("Mega Pack Vol.4/Jungle Vol.2/Animations/Mousedeer_Animations.fbx", "Mega Pack Vol.4/Jungle Vol.2/Models/Mousedeer_LOD0.fbx", "Mega Pack Vol.4/Jungle Vol.2/Textures/T_Mousedeer.png"),
    "Kirin": ("Fantasy Vol.1/Animations/Kirin_Animations.fbx", "Fantasy Vol.1/Models/Kirin_LOD0.fbx", "Fantasy Vol.1/Textures/T_Kirin.png"),
    "Pegasus": ("Fantasy Vol.1/Animations/Pegasus_Animations.fbx", "Fantasy Vol.1/Models/Pegasus_LOD0.fbx", "Fantasy Vol.1/Textures/T_Pegasus.png"),
}

STANDARD_BONES = ["root", "body", "ear.L", "ear.R", "tail.01", "tail.02", "leg.F.L", "leg.B.L", "leg.F.R", "leg.B.R"]
OPTIONAL_BONES = ["wing.L", "wing.R"]

CLIPS = ["Attack", "Bounce", "Clicked", "Death", "Eat", "Fear", "Fly", "Hit", "Idle_A", "Idle_B", "Idle_C",
         "Jump", "Roll", "Run", "Sit", "Spin", "Swim", "Walk"]
NON_LOOP_CLIPS = {"Death"}
REQUIRED_CLIPS = {"Idle_A", "Run"}  # 게임 AnimatorController 가 실제로 쓰는 클립
CLIP_FRAMES = (1, 11)  # 24fps, 11프레임 (원본 FBX 기준 0~10)

SHAPE_KEYS = ["eyes.blink", "eyes.happy", "eyes.sad", "eyes.sleep", "eyes.annoyed", "eyes.squint", "eyes.shrink",
              "eyes.dead", "eyes.lookOut", "eyes.lookIn", "eyes.lookUp", "eyes.lookDown", "eyes.excited-1",
              "eyes.excited-2", "eyes.rabid", "eyes.spin-1", "eyes.spin-2", "eyes.spin-3", "eyes.cry-1", "eyes.cry-2",
              "eyes.trauma", "teardrop-1.L", "teardrop-2.L", "sweat-1.L", "sweat-2.L", "teardrop-1.R", "teardrop-2.R",
              "sweat-1.R", "sweat-2.R"]

# 16x4 팔레트: 2px 폭 슬롯 8개 x (위 2줄 / 아래 2줄)
SLOT_LABELS = ["몸 1", "몸 2", "몸 3", "몸 4", "몸 5", "포인트 / 입속", "눈 흰자 / 땀", "눈동자 / 눈물"]
DEFAULT_TOP = ["#f9ac68", "#fdc498", "#a2704f", "#a66551", "#ab5a59", "#6e4a3a", "#f9f8f2", "#292939"]  # Horse
DEFAULT_BOTTOM = ["#f9ac68", "#fdc498", "#a2704f", "#a66551", "#ab5a59", "#cc7d95", "#fff263", "#63cdff"]

PREFIX_REF = "REF_"
PREFIX_FACEKIT = "FaceKit_"


def _hex(h):
    h = h.lstrip("#")
    return tuple(int(h[i:i + 2], 16) / 255 for i in (0, 2, 4))


def slot_uv(slot, half):
    """슬롯 중앙 UV. 원본처럼 면의 모든 UV 를 한 점에 모은다."""
    u = (slot * 2 + 1) / 16
    v = 0.875 if half == "TOP" else 0.125
    return u, v


def uv_to_slot(u, v):
    slot = min(7, max(0, int(u * 8)))
    half = "TOP" if v >= 0.5 else "BOTTOM"
    return slot, half


# ---------------------------------------------------------------------------
# 경로
# ---------------------------------------------------------------------------

def _search_up(start):
    p = Path(start).resolve()
    for parent in [p] + list(p.parents):
        cand = parent / QUIRKY_DIR
        if cand.is_dir():
            return cand
    return None


def quirky_root(context):
    s = context.scene.qak
    if s.quirky_root:
        p = Path(bpy.path.abspath(s.quirky_root))
        if p.is_dir():
            return p
    for start in (bpy.data.filepath and os.path.dirname(bpy.data.filepath), os.path.dirname(__file__)):
        if start:
            found = _search_up(start)
            if found:
                return found
    return None


def ref_path(context, index):
    root = quirky_root(context)
    if root is None:
        raise RuntimeError("Quirky Series Ultimate 폴더를 찾을 수 없습니다. 패널의 'Quirky 폴더' 경로를 지정하세요.")
    rel = REFERENCES[context.scene.qak.reference][index]
    return root / rel if rel else None


def import_fbx(filepath):
    before = set(bpy.data.objects)
    bpy.ops.import_scene.fbx(filepath=str(filepath), use_anim=True)
    return [o for o in bpy.data.objects if o not in before]


def set_action(obj, action):
    ad = obj.animation_data or obj.animation_data_create()
    ad.action = action
    if hasattr(ad, "action_slot") and ad.action_slot is None and getattr(action, "slots", None):
        ad.action_slot = action.slots[0]


def action_curves(action):
    if hasattr(action, "layers") and len(action.layers):
        for layer in action.layers:
            for strip in layer.strips:
                for bag in strip.channelbags:
                    yield from bag.fcurves
    else:
        yield from action.fcurves


def find_rig(context):
    ob = context.active_object
    if ob and ob.type == "ARMATURE":
        return ob
    if ob and ob.type == "MESH" and ob.find_armature():
        return ob.find_armature()
    rigs = [o for o in context.scene.objects if o.type == "ARMATURE"]
    return rigs[0] if len(rigs) == 1 else None


def export_meshes(rig):
    return [o for o in bpy.data.objects
            if o.type == "MESH" and o.find_armature() == rig
            and not o.name.startswith((PREFIX_REF, PREFIX_FACEKIT))]


# ---------------------------------------------------------------------------
# 속성
# ---------------------------------------------------------------------------

def _ref_items(self, context):
    return [(k, k, "") for k in REFERENCES]


class QAK_PaletteSlot(bpy.types.PropertyGroup):
    top: FloatVectorProperty(name="위", subtype="COLOR_GAMMA", size=3, min=0, max=1)
    bottom: FloatVectorProperty(name="아래", subtype="COLOR_GAMMA", size=3, min=0, max=1)


class QAK_Settings(bpy.types.PropertyGroup):
    quirky_root: StringProperty(name="Quirky 폴더", subtype="DIR_PATH",
                                description="비워두면 블렌드 파일/애드온 위치에서 Assets/ThirdParty/Quirky Series Ultimate 를 자동으로 찾음")
    reference: EnumProperty(name="참조 동물", items=_ref_items)
    animal_name: StringProperty(name="새 동물 이름", default="NewAnimal")
    palette: CollectionProperty(type=QAK_PaletteSlot)
    export_dir: StringProperty(name="내보낼 폴더", subtype="DIR_PATH", default="//")


def ensure_palette(s):
    while len(s.palette) < 8:
        i = len(s.palette)
        slot = s.palette.add()
        slot.top = _hex(DEFAULT_TOP[i])
        slot.bottom = _hex(DEFAULT_BOTTOM[i])


# ---------------------------------------------------------------------------
# 1. 시작
# ---------------------------------------------------------------------------

class QAK_OT_start_from_reference(bpy.types.Operator):
    """참조 동물의 Rig(본 10~12개 + 18개 애니메이션)를 가져온다. 메시는 REF_ 로 남겨 비교용으로 쓴다"""
    bl_idname = "qak.start_from_reference"
    bl_label = "참조 리그로 새 동물 시작"
    bl_options = {"REGISTER", "UNDO"}

    def execute(self, context):
        s = context.scene.qak
        if any(o.type == "ARMATURE" and o.name == "Rig" for o in context.scene.objects):
            self.report({"ERROR"}, "씬에 이미 'Rig' 가 있습니다. 새 파일에서 실행하세요.")
            return {"CANCELLED"}
        try:
            path = ref_path(context, 0)
        except RuntimeError as e:
            self.report({"ERROR"}, str(e))
            return {"CANCELLED"}
        if bpy.context.mode != "OBJECT":
            bpy.ops.object.mode_set(mode="OBJECT")
        new = import_fbx(path)

        scene = context.scene
        scene.render.fps = 24
        scene.render.fps_base = 1
        scene.frame_start, scene.frame_end = CLIP_FRAMES

        ref_col = bpy.data.collections.get("QAK_Reference") or bpy.data.collections.new("QAK_Reference")
        if ref_col.name not in scene.collection.children:
            scene.collection.children.link(ref_col)

        rig = next(o for o in new if o.type == "ARMATURE")
        rig.name = "Rig"
        rig.show_in_front = True
        for ob in new:
            if ob.type == "MESH":
                ob.name = f"{PREFIX_REF}{s.reference}_Mesh"
                ob.display_type = "WIRE"
                ob.hide_render = True
                for c in list(ob.users_collection):
                    c.objects.unlink(ob)
                ref_col.objects.link(ob)

        # 임포트된 액션 정리: "Rig|Run" -> "Run", 페이크 유저, 루프 구간
        for act in bpy.data.actions:
            short = act.name.split("|")[-1]
            if short in CLIPS:
                act.name = short
            act.use_fake_user = True
            act.use_frame_range = True
            act.frame_start, act.frame_end = CLIP_FRAMES
            act.use_cyclic = act.name not in NON_LOOP_CLIPS
        if "Idle_A" in bpy.data.actions:
            set_action(rig, bpy.data.actions["Idle_A"])

        if s.animal_name == "NewAnimal":
            s.animal_name = f"My{s.reference}"
        self.report({"INFO"}, f"{s.reference} 리그와 애니메이션 {len(bpy.data.actions)}개를 가져왔습니다.")
        return {"FINISHED"}


# ---------------------------------------------------------------------------
# 2. 팔레트 텍스처 / 머티리얼 / UV 칠하기
# ---------------------------------------------------------------------------

class QAK_OT_load_reference_palette(bpy.types.Operator):
    """참조 동물의 T_*.png 팔레트 색을 패널로 불러온다"""
    bl_idname = "qak.load_reference_palette"
    bl_label = "참조 팔레트 불러오기"
    bl_options = {"REGISTER", "UNDO"}

    def execute(self, context):
        s = context.scene.qak
        ensure_palette(s)
        try:
            path = ref_path(context, 2)
        except RuntimeError as e:
            self.report({"ERROR"}, str(e))
            return {"CANCELLED"}
        img = bpy.data.images.load(str(path), check_existing=False)
        w, h = img.size
        px = list(img.pixels)
        for i, slot in enumerate(s.palette):
            top = ((h - 1) * w + i * 2) * 4
            bottom = (i * 2) * 4
            slot.top = px[top:top + 3]
            slot.bottom = px[bottom:bottom + 3]
        bpy.data.images.remove(img)
        return {"FINISHED"}


class QAK_OT_build_palette(bpy.types.Operator):
    """패널 색으로 16x4 팔레트 텍스처(T_이름.png)와 머티리얼(M_이름)을 만들고 선택한 메시에 적용"""
    bl_idname = "qak.build_palette"
    bl_label = "팔레트 텍스처/머티리얼 생성"
    bl_options = {"REGISTER", "UNDO"}

    def execute(self, context):
        s = context.scene.qak
        ensure_palette(s)
        name = s.animal_name
        img_name = f"T_{name}"
        img = bpy.data.images.get(img_name)
        if img is None or tuple(img.size) != (16, 4):
            if img:
                bpy.data.images.remove(img)
            img = bpy.data.images.new(img_name, 16, 4, alpha=False)
        px = [0.0] * (16 * 4 * 4)
        for y in range(4):
            for x in range(16):
                slot = s.palette[x // 2]
                col = slot.top if y >= 2 else slot.bottom
                i = (y * 16 + x) * 4
                px[i:i + 4] = (*col, 1.0)
        img.pixels = px

        if bpy.data.filepath:
            out = Path(bpy.path.abspath("//textures")) / f"{img_name}.png"
            out.parent.mkdir(parents=True, exist_ok=True)
            img.filepath_raw = str(out)
            img.file_format = "PNG"
            img.save()
        else:
            img.pack()
            self.report({"WARNING"}, "블렌드 파일이 저장되지 않아 텍스처를 파일 안에 포장했습니다. 저장 후 다시 누르면 textures/ 에 PNG 로 저장됩니다.")

        mat = bpy.data.materials.get(f"M_{name}") or bpy.data.materials.new(f"M_{name}")
        if hasattr(mat, "use_nodes") and not mat.use_nodes:
            mat.use_nodes = True
        nt = mat.node_tree
        nt.nodes.clear()
        out_node = nt.nodes.new("ShaderNodeOutputMaterial")
        bsdf = nt.nodes.new("ShaderNodeBsdfPrincipled")
        tex = nt.nodes.new("ShaderNodeTexImage")
        tex.image = img
        tex.interpolation = "Closest"
        out_node.location, bsdf.location, tex.location = (300, 0), (0, 0), (-350, 0)
        bsdf.inputs["Roughness"].default_value = 1.0
        if "Specular IOR Level" in bsdf.inputs:
            bsdf.inputs["Specular IOR Level"].default_value = 0.0
        nt.links.new(tex.outputs["Color"], bsdf.inputs["Base Color"])
        nt.links.new(bsdf.outputs["BSDF"], out_node.inputs["Surface"])

        for ob in context.selected_objects:
            if ob.type == "MESH" and not ob.name.startswith(PREFIX_REF):
                if not ob.data.materials:
                    ob.data.materials.append(mat)
                else:
                    ob.data.materials[0] = mat
                if not ob.data.uv_layers:
                    ob.data.uv_layers.new(name="UVMap")
        return {"FINISHED"}


class QAK_OT_paint_slot(bpy.types.Operator):
    """편집 모드에서 선택한 면의 UV 를 팔레트 슬롯 한 칸으로 모은다 (= 그 색으로 칠하기)"""
    bl_idname = "qak.paint_slot"
    bl_label = "선택 면 칠하기"
    bl_options = {"REGISTER", "UNDO"}

    slot: IntProperty(min=0, max=7)
    half: EnumProperty(items=[("TOP", "위", ""), ("BOTTOM", "아래", "")])

    @classmethod
    def poll(cls, context):
        return context.mode == "EDIT_MESH"

    def execute(self, context):
        u, v = slot_uv(self.slot, self.half)
        count = 0
        for ob in context.objects_in_mode:
            me = ob.data
            bm = bmesh.from_edit_mesh(me)
            uv = bm.loops.layers.uv.verify()
            for f in bm.faces:
                if f.select:
                    count += 1
                    for loop in f.loops:
                        loop[uv].uv = (u, v)
            bmesh.update_edit_mesh(me)
        self.report({"INFO"}, f"면 {count}개 -> 슬롯 {self.slot + 1} {'위' if self.half == 'TOP' else '아래'}")
        return {"FINISHED"}


class QAK_OT_select_slot(bpy.types.Operator):
    """편집 모드에서 해당 팔레트 슬롯을 쓰는 면을 선택한다"""
    bl_idname = "qak.select_slot"
    bl_label = "슬롯 면 선택"
    bl_options = {"REGISTER", "UNDO"}

    slot: IntProperty(min=0, max=7)
    half: EnumProperty(items=[("TOP", "위", ""), ("BOTTOM", "아래", "")])

    @classmethod
    def poll(cls, context):
        return context.mode == "EDIT_MESH"

    def execute(self, context):
        for ob in context.objects_in_mode:
            bm = bmesh.from_edit_mesh(ob.data)
            uv = bm.loops.layers.uv.verify()
            for f in bm.faces:
                cu = sum(l[uv].uv.x for l in f.loops) / len(f.loops)
                cv = sum(l[uv].uv.y for l in f.loops) / len(f.loops)
                if uv_to_slot(cu, cv) == (self.slot, self.half):
                    f.select_set(True)
            bm.select_flush_mode()
            bmesh.update_edit_mesh(ob.data)
        return {"FINISHED"}


# ---------------------------------------------------------------------------
# 3. 눈/표정 키트 (셰이프키 29개)
# ---------------------------------------------------------------------------

class QAK_OT_import_face_kit(bpy.types.Operator):
    """참조 동물 LOD0 에서 셰이프키로 움직이는 부분(눈, 눈물, 땀)만 떼어 FaceKit_ 오브젝트로 가져온다"""
    bl_idname = "qak.import_face_kit"
    bl_label = "표정 키트 가져오기"
    bl_options = {"REGISTER", "UNDO"}

    def execute(self, context):
        s = context.scene.qak
        try:
            path = ref_path(context, 1)
        except RuntimeError as e:
            self.report({"ERROR"}, str(e))
            return {"CANCELLED"}
        if path is None:  # Donkey 는 LOD0(셰이프키) 파일이 없어 Horse 것을 쓴다
            path = quirky_root(context) / REFERENCES["Horse"][1]
        if bpy.context.mode != "OBJECT":
            bpy.ops.object.mode_set(mode="OBJECT")
        actions_before = set(bpy.data.actions)
        new = import_fbx(path)
        mesh = next(o for o in new if o.type == "MESH" and o.data.shape_keys)
        for ob in new:
            if ob is not mesh:
                bpy.data.objects.remove(ob)
        for act in set(bpy.data.actions) - actions_before:
            bpy.data.actions.remove(act)

        mw = mesh.matrix_world.copy()
        mesh.parent = None
        mesh.matrix_world = mw
        for m in list(mesh.modifiers):
            mesh.modifiers.remove(m)

        me = mesh.data
        keys = me.shape_keys.key_blocks
        basis = keys[0].data
        moved = set()
        for kb in keys[1:]:
            for i, (a, b) in enumerate(zip(basis, kb.data)):
                if (a.co - b.co).length_squared > 1e-12:
                    moved.add(i)

        bm = bmesh.new()
        bm.from_mesh(me)
        bm.verts.ensure_lookup_table()
        keep = set()
        stack = [bm.verts[i] for i in moved]
        while stack:  # 움직이는 버텍스와 연결된 섬 전체
            v = stack.pop()
            if v.index in keep:
                continue
            keep.add(v.index)
            for e in v.link_edges:
                o = e.other_vert(v)
                if o.index not in keep:
                    stack.append(o)
        bmesh.ops.delete(bm, geom=[v for v in bm.verts if v.index not in keep], context="VERTS")
        bm.to_mesh(me)
        bm.free()
        me.update()

        mesh.name = f"{PREFIX_FACEKIT}{s.reference}"
        context.view_layer.objects.active = mesh
        for o in context.selected_objects:
            o.select_set(False)
        mesh.select_set(True)
        self.report({"INFO"}, f"표정 키트: 버텍스 {len(me.vertices)}개, 셰이프키 {len(keys) - 1}개. 새 머리에 맞게 이동/회전/크기 조절하세요.")
        return {"FINISHED"}


class QAK_OT_join_face_kit(bpy.types.Operator):
    """선택한 FaceKit_ 을 활성 본체 메시에 합친다 (셰이프키 유지, 머티리얼 1개로 통일)"""
    bl_idname = "qak.join_face_kit"
    bl_label = "표정 키트를 본체에 합치기"
    bl_options = {"REGISTER", "UNDO"}

    def execute(self, context):
        body = context.active_object
        kits = [o for o in context.selected_objects if o.name.startswith(PREFIX_FACEKIT)]
        if not body or body.type != "MESH" or body.name.startswith(PREFIX_FACEKIT) or not kits:
            self.report({"ERROR"}, "FaceKit_ 오브젝트와 본체를 함께 선택하고, 본체를 활성(마지막 클릭)으로 두세요.")
            return {"CANCELLED"}
        if context.mode != "OBJECT":
            bpy.ops.object.mode_set(mode="OBJECT")
        if body.data.shape_keys is None:
            body.shape_key_add(name="Basis", from_mix=False)
        with context.temp_override(active_object=body, object=body,
                                   selected_objects=[body] + kits, selected_editable_objects=[body] + kits):
            bpy.ops.object.join()
        me = body.data
        if len(me.materials) > 1:
            for p in me.polygons:
                p.material_index = 0
            while len(me.materials) > 1:
                me.materials.pop()
        self.report({"INFO"}, f"합치기 완료. 셰이프키 {len(me.shape_keys.key_blocks) - 1}개")
        return {"FINISHED"}


# ---------------------------------------------------------------------------
# 4. 검사 / 내보내기
# ---------------------------------------------------------------------------

def validate(context):
    """(level, message) 목록"""
    out = []
    scene = context.scene
    rig = find_rig(context)
    if rig is None:
        return [("ERROR", "Rig(아마추어)를 찾지 못했습니다. Rig 를 선택하세요.")]
    if rig.name != "Rig":
        out.append(("WARNING", f"아마추어 이름이 '{rig.name}' 입니다. 원본과 같게 'Rig' 를 권장합니다."))

    bones = [b.name for b in rig.data.bones]
    missing = [b for b in STANDARD_BONES if b not in bones]
    extra = [b for b in bones if b not in STANDARD_BONES + OPTIONAL_BONES]
    if missing:
        out.append(("WARNING", f"표준 본 없음: {', '.join(missing)} (Mousedeer 처럼 tail.02 가 없는 경우는 괜찮음)"))
    if extra:
        out.append(("INFO", f"추가 본: {', '.join(extra)}"))

    if scene.render.fps != 24:
        out.append(("WARNING", f"씬 FPS 가 {scene.render.fps} 입니다. 원본은 24fps."))

    meshes = export_meshes(rig)
    if not meshes:
        out.append(("ERROR", "Rig 에 연결된(아마추어 모디파이어) 메시가 없습니다."))
    elif "Mesh_LOD0" not in [o.name for o in meshes]:
        out.append(("WARNING", "메시 이름을 'Mesh_LOD0' 로 하세요. 표정 anim(Eyes_*)이 이 경로의 블렌드셰이프를 움직입니다."))
    for ob in meshes:
        me = ob.data
        tris = sum(len(p.vertices) - 2 for p in me.polygons)
        tag = ob.name
        if not (1000 <= tris <= 8000):
            out.append(("WARNING", f"{tag}: 삼각형 {tris}개 (원본 LOD0 3,000~7,800 / 애니용 1,500~3,000)"))
        if len(me.materials) != 1:
            out.append(("WARNING", f"{tag}: 머티리얼 {len(me.materials)}개 (원본은 1개)"))
        if not me.uv_layers:
            out.append(("ERROR", f"{tag}: UV 없음"))
        else:
            uv = me.uv_layers[0].data
            spread = 0
            for p in me.polygons:
                us = [uv[i].uv.x for i in p.loop_indices]
                vs = [uv[i].uv.y for i in p.loop_indices]
                spread = max(spread, max(us) - min(us), max(vs) - min(vs))
            if spread > 0.1:
                out.append(("WARNING", f"{tag}: 한 면의 UV 가 팔레트 칸을 넘어갑니다(최대 폭 {spread:.2f}). '선택 면 칠하기'로 칸에 모으세요."))
        flat = sum(not p.use_smooth for p in me.polygons)
        if flat:
            out.append(("INFO", f"{tag}: 플랫 셰이딩 면 {flat}개 (원본은 거의 전부 Smooth)"))
        if tuple(round(x, 4) for x in ob.scale) != (1, 1, 1):
            out.append(("WARNING", f"{tag}: 스케일 미적용 {tuple(ob.scale)} (Ctrl+A > Scale)"))
        groups = {g.name for g in ob.vertex_groups}
        unweighted = sum(1 for v in me.vertices if not any(g.weight > 0 and ob.vertex_groups[g.group].name in bones for g in v.groups))
        if unweighted:
            out.append(("WARNING", f"{tag}: 본 가중치 없는 버텍스 {unweighted}개"))
        stray = groups - set(bones)
        if stray:
            out.append(("INFO", f"{tag}: 본과 이름이 다른 버텍스 그룹 {sorted(stray)}"))
        keys = [k.name for k in me.shape_keys.key_blocks[1:]] if me.shape_keys else []
        missing_keys = [k for k in SHAPE_KEYS if k not in keys]
        if missing_keys:
            out.append(("WARNING", f"{tag}: 표정 셰이프키 {len(missing_keys)}개 없음 -> _Animations/Shapekey/*.anim 재사용 불가"))

    names = {a.name for a in bpy.data.actions}
    for c in sorted(REQUIRED_CLIPS - names):
        out.append(("ERROR", f"필수 애니메이션 '{c}' 없음 (게임에서 Idle_A / Run 사용)"))
    for a in bpy.data.actions:
        if a.name in CLIPS:
            s, e = a.frame_range
            if (round(s), round(e)) != CLIP_FRAMES:
                out.append(("INFO", f"액션 {a.name}: 프레임 {s:g}~{e:g} (원본 규격 1~11, 11프레임)"))
    if not out:
        out.append(("INFO", "규격 검사 통과"))
    return out


class QAK_OT_validate(bpy.types.Operator):
    """원본 Quirky 규격(본 이름, 팔레트 UV, 셰이프키, 클립 길이 등)과 비교"""
    bl_idname = "qak.validate"
    bl_label = "규격 검사"

    def execute(self, context):
        results = validate(context)
        for level, msg in results:
            print(f"[QAK] {level}: {msg}")

        def draw(menu, _ctx):
            for level, msg in results:
                menu.layout.label(text=msg, icon={"ERROR": "ERROR", "WARNING": "INFO"}.get(level, "CHECKMARK"))
        context.window_manager.popup_menu(draw, title="Quirky 규격 검사", icon="CHECKMARK")
        return {"FINISHED"}


class QAK_OT_export_fbx(bpy.types.Operator):
    """Rig + 메시 + 셰이프키 + 모든 액션을 Unity 용 FBX 하나로 내보낸다 (REF_/FaceKit_ 제외)"""
    bl_idname = "qak.export_fbx"
    bl_label = "Unity 용 FBX 내보내기"

    filepath: StringProperty(subtype="FILE_PATH", options={"SKIP_SAVE"})

    def execute(self, context):
        s = context.scene.qak
        rig = find_rig(context)
        if rig is None:
            self.report({"ERROR"}, "Rig 를 찾지 못했습니다.")
            return {"CANCELLED"}
        meshes = export_meshes(rig)
        path = self.filepath or os.path.join(bpy.path.abspath(s.export_dir or "//"), f"{s.animal_name}.fbx")
        if not os.path.isabs(path):
            self.report({"ERROR"}, "블렌드 파일을 먼저 저장하거나 '내보낼 폴더'를 절대 경로로 지정하세요.")
            return {"CANCELLED"}
        os.makedirs(os.path.dirname(path), exist_ok=True)

        if context.mode != "OBJECT":
            bpy.ops.object.mode_set(mode="OBJECT")
        prev_sel = list(context.selected_objects)
        prev_act = rig.animation_data.action if rig.animation_data else None
        for o in prev_sel:
            o.select_set(False)
        for o in [rig] + meshes:
            o.select_set(True)
        try:
            bpy.ops.export_scene.fbx(
                filepath=path,
                use_selection=True,
                object_types={"ARMATURE", "MESH"},
                apply_unit_scale=True,
                apply_scale_options="FBX_SCALE_NONE",
                axis_forward="-Z",
                axis_up="Y",
                use_mesh_modifiers=False,  # 셰이프키 보존
                mesh_smooth_type="FACE",
                use_tspace=False,
                add_leaf_bones=False,
                primary_bone_axis="Y",
                secondary_bone_axis="X",
                armature_nodetype="NULL",
                bake_anim=True,
                bake_anim_use_all_bones=True,
                bake_anim_use_nla_strips=False,
                bake_anim_use_all_actions=True,
                bake_anim_force_startend_keying=True,
                bake_anim_step=1.0,
                bake_anim_simplify_factor=0.0,
                path_mode="AUTO",
            )
        finally:
            for o in context.selected_objects:
                o.select_set(False)
            for o in prev_sel:
                o.select_set(True)
            if prev_act:
                set_action(rig, prev_act)
        self.report({"INFO"}, f"내보내기 완료: {path}")
        return {"FINISHED"}


# ---------------------------------------------------------------------------
# 패널
# ---------------------------------------------------------------------------

class QAK_PT_main(bpy.types.Panel):
    bl_label = "Quirky Animal Kit"
    bl_space_type = "VIEW_3D"
    bl_region_type = "UI"
    bl_category = "Quirky"

    def draw(self, context):
        s = context.scene.qak
        layout = self.layout
        col = layout.column(align=True)
        col.prop(s, "quirky_root")
        root = quirky_root(context)
        col.label(text=str(root) if root else "Quirky 폴더를 찾지 못함", icon="CHECKMARK" if root else "ERROR")
        col.prop(s, "reference")
        col.prop(s, "animal_name")

        box = layout.box()
        box.label(text="1. 시작", icon="ARMATURE_DATA")
        box.operator("qak.start_from_reference", icon="IMPORT")


class QAK_PT_palette(bpy.types.Panel):
    bl_label = "2. 팔레트 텍스처"
    bl_space_type = "VIEW_3D"
    bl_region_type = "UI"
    bl_category = "Quirky"
    bl_parent_id = "QAK_PT_main"

    def draw(self, context):
        s = context.scene.qak
        layout = self.layout
        row = layout.row(align=True)
        row.operator("qak.load_reference_palette", icon="EYEDROPPER")
        row.operator("qak.build_palette", icon="TEXTURE")
        if len(s.palette) < 8:
            layout.label(text="'참조 팔레트 불러오기' 또는 '생성'을 누르면 슬롯이 채워집니다.")
            return
        editing = context.mode == "EDIT_MESH"
        grid = layout.column(align=True)
        for i, slot in enumerate(s.palette):
            row = grid.row(align=True)
            row.label(text=f"{i + 1}. {SLOT_LABELS[i]}")
            for half, prop in (("TOP", "top"), ("BOTTOM", "bottom")):
                row.prop(slot, prop, text="")
                op = row.operator("qak.paint_slot", text="", icon="BRUSH_DATA")
                op.slot, op.half = i, half
                op = row.operator("qak.select_slot", text="", icon="RESTRICT_SELECT_OFF")
                op.slot, op.half = i, half
        if not editing:
            layout.label(text="칠하기/선택 버튼은 편집 모드(면 선택)에서 동작", icon="INFO")


class QAK_PT_face(bpy.types.Panel):
    bl_label = "3. 눈 / 표정 셰이프키"
    bl_space_type = "VIEW_3D"
    bl_region_type = "UI"
    bl_category = "Quirky"
    bl_parent_id = "QAK_PT_main"

    def draw(self, context):
        col = self.layout.column(align=True)
        col.operator("qak.import_face_kit", icon="SHAPEKEY_DATA")
        col.operator("qak.join_face_kit", icon="AUTOMERGE_OFF")


class QAK_PT_export(bpy.types.Panel):
    bl_label = "4. 검사 / 내보내기"
    bl_space_type = "VIEW_3D"
    bl_region_type = "UI"
    bl_category = "Quirky"
    bl_parent_id = "QAK_PT_main"

    def draw(self, context):
        s = context.scene.qak
        col = self.layout.column(align=True)
        col.operator("qak.validate", icon="CHECKMARK")
        col.prop(s, "export_dir")
        col.operator("qak.export_fbx", icon="EXPORT")


classes = (
    QAK_PaletteSlot, QAK_Settings,
    QAK_OT_start_from_reference, QAK_OT_load_reference_palette, QAK_OT_build_palette,
    QAK_OT_paint_slot, QAK_OT_select_slot, QAK_OT_import_face_kit, QAK_OT_join_face_kit,
    QAK_OT_validate, QAK_OT_export_fbx,
    QAK_PT_main, QAK_PT_palette, QAK_PT_face, QAK_PT_export,
)


def register():
    for c in classes:
        bpy.utils.register_class(c)
    bpy.types.Scene.qak = PointerProperty(type=QAK_Settings)


def unregister():
    del bpy.types.Scene.qak
    for c in reversed(classes):
        bpy.utils.unregister_class(c)


if __name__ == "__main__":
    register()
