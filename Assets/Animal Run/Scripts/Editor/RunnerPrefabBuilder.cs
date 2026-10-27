using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace Animal_Run.Scripts.EditorTools
{
    // ArtSource/Blender 의 quirky_animal_kit 로 내보낸 FBX 를 러너 프리팹으로 만든다.
    // FBX 위치 규칙: Assets/Animal Run/Models/Runners/<Name>/<Name>.fbx, 같은 폴더에 T_<Name>.png
    public static class RunnerPrefabBuilder
    {
        private const string ModelsRoot = "Assets/Animal Run/Models/Runners";
        private const string PrefabsRoot = "Assets/Resources/Animal Run/Prefabs/Runners";
        private const string SpritesRoot = "Assets/Resources/Animal Run/Sprites/Runners";
        private const string QuirkyRoot = "Assets/ThirdParty/Quirky Series Ultimate";
        private const string TemplateMaterial = QuirkyRoot + "/Mega Pack Vol.3/Farm Vol.2/Materials/M_Horse.mat";
        private const string TemplateController = QuirkyRoot + "/Mega Pack Vol.3/Farm Vol.2/Animations/AC_Horse.controller";
        private const string TemplatePrefab = PrefabsRoot + "/Mousedeer.prefab";

        [MenuItem("Tools/Animal Run/Build Runner Prefab From Selected FBX")]
        private static void BuildFromSelection()
        {
            string path = AssetDatabase.GetAssetPath(Selection.activeObject);
            if (!path.EndsWith(".fbx"))
            {
                Debug.LogError("[RunnerPrefabBuilder] Models/Runners/<Name>/<Name>.fbx 를 선택하세요.");
                return;
            }
            Build(System.IO.Path.GetFileNameWithoutExtension(path));
        }

        // Unity -batchmode -executeMethod Animal_Run.Scripts.EditorTools.RunnerPrefabBuilder.BuildPigBatch
        public static void BuildPigBatch()
        {
            Build("Pig");
            Verify("Pig");
        }

        public static GameObject Build(string name)
        {
            string dir = $"{ModelsRoot}/{name}";
            string fbxPath = $"{dir}/{name}.fbx";
            string texPath = $"{dir}/T_{name}.png";
            string matPath = $"{dir}/M_{name}.mat";
            string controllerPath = $"{dir}/AC_{name}.controller";
            string iconPath = $"{SpritesRoot}/{name}_Image.png";
            string prefabPath = $"{PrefabsRoot}/{name}.prefab";

            AssetDatabase.Refresh();

            // 팔레트 텍스처: 원본과 같이 Point 필터
            var texImporter = (TextureImporter)AssetImporter.GetAtPath(texPath);
            texImporter.filterMode = FilterMode.Point;
            texImporter.SaveAndReimport();
            var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(texPath);

            var iconImporter = (TextureImporter)AssetImporter.GetAtPath(iconPath);
            if (iconImporter != null)
            {
                iconImporter.textureType = TextureImporterType.Sprite;
                iconImporter.spriteImportMode = SpriteImportMode.Single;
                iconImporter.alphaIsTransparency = true;
                iconImporter.SaveAndReimport();
            }

            // 머티리얼: M_Horse 복제 후 텍스처만 교체
            if (AssetDatabase.LoadAssetAtPath<Material>(matPath) == null)
                AssetDatabase.CopyAsset(TemplateMaterial, matPath);
            var material = AssetDatabase.LoadAssetAtPath<Material>(matPath);
            material.SetTexture("_BaseMap", texture);
            material.SetTexture("_MainTex", texture);
            EditorUtility.SetDirty(material);

            // 모델: Generic 리그, 블렌드셰이프, 클립 이름 "Rig|Run" -> "Run"
            var modelImporter = (ModelImporter)AssetImporter.GetAtPath(fbxPath);
            modelImporter.animationType = ModelImporterAnimationType.Generic;
            modelImporter.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
            modelImporter.importBlendShapes = true;
            modelImporter.importAnimation = true;
            modelImporter.materialImportMode = ModelImporterMaterialImportMode.ImportStandard;
            modelImporter.SaveAndReimport();

            foreach (var source in AssetDatabase.LoadAllAssetsAtPath(fbxPath).OfType<Material>())
                modelImporter.AddRemap(new AssetImporter.SourceAssetIdentifier(typeof(Material), source.name), material);
            ModelImporterClipAnimation[] clips = modelImporter.defaultClipAnimations;
            foreach (var clip in clips)
            {
                clip.name = clip.takeName.Split('|').Last();
                clip.loopTime = clip.name != "Death";
            }
            modelImporter.clipAnimations = clips;
            modelImporter.SaveAndReimport();

            Dictionary<string, AnimationClip> clipByName = AssetDatabase.LoadAllAssetsAtPath(fbxPath)
                .OfType<AnimationClip>()
                .Where(c => !c.name.StartsWith("__preview__"))
                .ToDictionary(c => c.name);
            Avatar avatar = AssetDatabase.LoadAllAssetsAtPath(fbxPath).OfType<Avatar>().First();

            // 컨트롤러: AC_Horse 복제 후 바디 클립(Idle_A, Run)만 교체. 표정 레이어는 그대로 재사용
            if (AssetDatabase.LoadAssetAtPath<AnimatorController>(controllerPath) == null)
                AssetDatabase.CopyAsset(TemplateController, controllerPath);
            var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(controllerPath);
            foreach (AnimatorControllerLayer layer in controller.layers)
            {
                foreach (ChildAnimatorState child in layer.stateMachine.states)
                {
                    if (clipByName.TryGetValue(child.state.name, out AnimationClip clip))
                        child.state.motion = clip;
                }
            }
            EditorUtility.SetDirty(controller);

            // 프리팹: 모델 인스턴스 + Mousedeer 와 같은 컴포넌트 구성 (Mousedeer 와 크기 비슷)
            var template = AssetDatabase.LoadAssetAtPath<GameObject>(TemplatePrefab);
            var model = AssetDatabase.LoadAssetAtPath<GameObject>(fbxPath);
            var root = (GameObject)PrefabUtility.InstantiatePrefab(model);
            root.name = name;
            root.transform.localScale = template.transform.localScale;

            Animator animator = root.GetComponent<Animator>();
            if (animator == null)
                animator = root.AddComponent<Animator>();
            EditorUtility.CopySerialized(template.GetComponent<Animator>(), animator);
            animator.avatar = avatar;
            animator.runtimeAnimatorController = controller;

            EditorUtility.CopySerialized(template.GetComponent<Rigidbody>(), root.AddComponent<Rigidbody>());
            EditorUtility.CopySerialized(template.GetComponent<CapsuleCollider>(), root.AddComponent<CapsuleCollider>());

            Transform templateWeapon = template.GetComponent<Runner>().weaponPosition;
            Transform body = root.transform.Find("Rig/root/body");
            var weapon = new GameObject("Weapon Position").transform;
            weapon.SetParent(body, false);
            weapon.localPosition = templateWeapon.localPosition;
            weapon.localRotation = templateWeapon.localRotation;
            weapon.localScale = templateWeapon.localScale;
            root.AddComponent<Runner>().weaponPosition = weapon;

            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
            Object.DestroyImmediate(root);
            AssetDatabase.SaveAssets();
            Debug.Log($"[RunnerPrefabBuilder] {prefabPath} 생성");
            return prefab;
        }

        private static void Verify(string name)
        {
            string fbxPath = $"{ModelsRoot}/{name}/{name}.fbx";
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>($"{PrefabsRoot}/{name}.prefab");
            var mesh = prefab.GetComponentInChildren<SkinnedMeshRenderer>();
            var animator = prefab.GetComponent<Animator>();
            var controller = (AnimatorController)animator.runtimeAnimatorController;
            var clips = AssetDatabase.LoadAllAssetsAtPath(fbxPath).OfType<AnimationClip>()
                .Where(c => !c.name.StartsWith("__preview__")).ToList();

            Debug.Log($"[Verify] mesh={mesh.name} tris={mesh.sharedMesh.triangles.Length / 3} " +
                      $"blendShapes={mesh.sharedMesh.blendShapeCount} bones={mesh.bones.Length} " +
                      $"material={mesh.sharedMaterial.name} texture={mesh.sharedMaterial.GetTexture("_BaseMap")?.name} " +
                      $"bounds={mesh.bounds.size}");
            Debug.Log($"[Verify] clips={string.Join(",", clips.Select(c => $"{c.name}({c.length:F3}s,loop={c.isLooping})"))}");
            foreach (var layer in controller.layers)
                foreach (var s in layer.stateMachine.states.Where(s => !s.state.name.StartsWith("Eyes_")))
                    Debug.Log($"[Verify] layer={layer.name} state={s.state.name} motion={AssetDatabase.GetAssetPath(s.state.motion)}:{s.state.motion?.name}");

            // 표정 클립이 이 메시의 블렌드셰이프를 실제로 가리키는지
            var blink = AssetDatabase.LoadAssetAtPath<AnimationClip>($"{QuirkyRoot}/_Animations/Shapekey/Eyes_Blink.anim");
            bool blinkBound = AnimationUtility.GetCurveBindings(blink)
                .Any(b => b.path == mesh.name && mesh.sharedMesh.GetBlendShapeIndex(b.propertyName.Replace("blendShape.", "")) >= 0);
            Debug.Log($"[Verify] Eyes_Blink -> {mesh.name} blendShape bound={blinkBound}");

            // 게임과 같은 방식으로 Resources 에서 로드
            var runner = Resources.Load<Runner>($"Animal Run/Prefabs/Runners/{name}");
            var sprite = Resources.Load<Sprite>($"Animal Run/Sprites/Runners/{name}_Image");
            Debug.Log($"[Verify] Resources runner={runner != null} weaponPosition={runner?.weaponPosition?.parent?.name} sprite={sprite != null}");
        }
    }
}
