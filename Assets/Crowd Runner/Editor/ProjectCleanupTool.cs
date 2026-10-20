using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// 프로젝트 정리용 에디터 도구. 에셋 이동/삭제는 모두 AssetDatabase를 통해 처리해 GUID 참조를 유지한다.
/// 메뉴 순서대로 실행한다: 1. 죽은 스크립트 제거 → 2. 미사용 에셋 리포트 → 3. 미사용 에셋 이동 → 4. 서드파티 폴더 정리
/// </summary>
public static class ProjectCleanupTool
{
    private const string MenuRoot = "Tools/Project Cleanup/";
    private const string UnusedRoot = "Assets/_Unused";
    private const string ThirdPartyRoot = "Assets/ThirdParty";
    private const string ReportPath = "Logs/UnusedAssetsReport.txt";

    // 아무 동작도 하지 않거나 더 이상 쓰지 않는 스크립트
    private static readonly string[] DeadScriptPaths =
    {
        "Assets/Crowd Runner/Scripts/Managers/ShootingManager.cs",
        "Assets/Crowd Runner/Scripts/Weapon/WeaponChange.cs",
        "Assets/Crowd Runner/Scripts/Weapon/BulletCreate.cs",
        "Assets/Crowd Runner/Scripts/RaycastEnemy.cs",
        "Assets/Crowd Runner/Scripts/GrenadeTest.cs",
    };

    // 통째로 옮기면 안 되는 폴더 (특수 폴더, 코드 전용 폴더, 정리 결과 폴더)
    private static readonly string[] ProtectedFolders =
    {
        "Assets/Plugins",
        "Assets/TextMesh Pro",
        "Assets/StreamingAssets",
        "Assets/Crowd Runner/Editor",
        UnusedRoot,
        ThirdPartyRoot,
    };

    // 에셋 참조(GUID)로는 사용 여부를 알 수 없는 코드/인클루드 파일
    private static readonly HashSet<string> CodeExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".cs", ".dll", ".asmdef", ".asmref", ".rsp", ".cginc", ".hlsl", ".glslinc", ".compute",
        ".jslib", ".jspre", ".aar", ".jar", ".so", ".a", ".mm", ".m", ".h", ".c", ".cpp", ".java", ".kt", ".gradle",
    };

    // 사용 중인 부분만 남기고 ThirdParty 폴더로 모을 서드파티 에셋
    private static readonly string[] ThirdPartyFolders =
    {
        "Assets/CasualGameSounds",
        "Assets/Quirky Series Ultimate",
        "Assets/PolygonParticleFX",
        "Assets/_Heathen Engineering",
        "Assets/FatMachines",
    };

    #region 1. Dead Scripts

    [MenuItem(MenuRoot + "1. Remove Dead Scripts")]
    private static void RemoveDeadScripts()
    {
        List<MonoScript> scripts = DeadScriptPaths
            .Select(AssetDatabase.LoadAssetAtPath<MonoScript>)
            .Where(script => script != null)
            .ToList();

        if (scripts.Count == 0)
        {
            Debug.Log("[ProjectCleanup] 제거할 스크립트가 없습니다.");
            return;
        }

        string scriptNames = string.Join("\n", scripts.Select(script => script.name));
        if (EditorUtility.DisplayDialog("Remove Dead Scripts",
                $"아래 스크립트의 컴포넌트를 씬/프리팹에서 제거하고 스크립트 파일을 삭제합니다.\n\n{scriptNames}", "진행", "취소") == false)
            return;

        if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo() == false)
            return;

        List<Type> deadTypes = scripts.Select(script => script.GetClass()).Where(type => type != null).ToList();
        HashSet<string> deadGuids = new(DeadScriptPaths.Select(AssetDatabase.AssetPathToGUID));
        StringBuilder log = new();

        // 빌드 씬에서 제거
        foreach (EditorBuildSettingsScene buildScene in EditorBuildSettings.scenes)
        {
            var scene = EditorSceneManager.OpenScene(buildScene.path, OpenSceneMode.Single);
            int removedCount = 0;

            foreach (GameObject root in scene.GetRootGameObjects())
            {
                foreach (Type type in deadTypes)
                {
                    foreach (Component component in root.GetComponentsInChildren(type, true))
                    {
                        GameObject owner = component.gameObject;
                        bool onlyThisComponent = owner.GetComponents<Component>().Length == 2 && owner.transform.childCount == 0;

                        log.AppendLine($"[Scene] {buildScene.path} : {GetHierarchyPath(owner.transform)} ({type.Name}){(onlyThisComponent ? " → 오브젝트 삭제" : "")}");
                        UnityEngine.Object.DestroyImmediate(onlyThisComponent ? owner : component);
                        removedCount++;
                    }
                }
            }

            if (removedCount > 0)
            {
                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene);
            }
        }

        // 프리팹에서 제거 (원본 프리팹을 먼저 처리해야 Variant에 반영된다)
        List<string> prefabPaths = AssetDatabase.FindAssets("t:Prefab")
            .Select(AssetDatabase.GUIDToAssetPath)
            .Where(path => ContainsAnyGuid(path, deadGuids))
            .OrderBy(path => PrefabUtility.GetPrefabAssetType(AssetDatabase.LoadAssetAtPath<GameObject>(path)) == PrefabAssetType.Variant)
            .ToList();

        foreach (string prefabPath in prefabPaths)
        {
            GameObject contents = PrefabUtility.LoadPrefabContents(prefabPath);
            int removedCount = 0;

            foreach (Type type in deadTypes)
            {
                foreach (Component component in contents.GetComponentsInChildren(type, true))
                {
                    bool removable = PrefabUtility.IsPartOfPrefabInstance(component) == false
                                     || PrefabUtility.IsAddedComponentOverride(component);
                    if (removable == false)
                        continue;

                    log.AppendLine($"[Prefab] {prefabPath} : {GetHierarchyPath(component.transform)} ({type.Name})");
                    UnityEngine.Object.DestroyImmediate(component);
                    removedCount++;
                }
            }

            if (removedCount > 0)
                PrefabUtility.SaveAsPrefabAsset(contents, prefabPath);

            PrefabUtility.UnloadPrefabContents(contents);
        }

        foreach (string scriptPath in DeadScriptPaths)
        {
            if (AssetDatabase.DeleteAsset(scriptPath))
                log.AppendLine($"[Delete] {scriptPath}");
        }

        AssetDatabase.SaveAssets();
        Debug.Log($"[ProjectCleanup] 죽은 스크립트 제거 완료\n{log}");
    }

    private static bool ContainsAnyGuid(string assetPath, HashSet<string> guids)
    {
        string text = File.ReadAllText(assetPath);
        return guids.Any(guid => string.IsNullOrEmpty(guid) == false && text.Contains(guid));
    }

    private static string GetHierarchyPath(Transform target)
    {
        return target.parent == null ? target.name : $"{GetHierarchyPath(target.parent)}/{target.name}";
    }

    #endregion

    #region 2~3. Unused Assets

    private class CleanupPlan
    {
        public readonly List<string> FolderMoves = new();
        public readonly List<string> FileMoves = new();
        public readonly List<string> UnusedCode = new();
        public int UsedCount;
    }

    [MenuItem(MenuRoot + "2. Report Unused Assets")]
    private static void ReportUnusedAssets()
    {
        CleanupPlan plan = BuildPlan();
        WriteReport(plan);

        EditorUtility.RevealInFinder(ReportPath);
        Debug.Log($"[ProjectCleanup] 리포트 작성 완료: {ReportPath} (폴더 {plan.FolderMoves.Count}개, 파일 {plan.FileMoves.Count}개 이동 예정)");
    }

    [MenuItem(MenuRoot + "3. Move Unused Assets")]
    private static void MoveUnusedAssets()
    {
        CleanupPlan plan = BuildPlan();
        WriteReport(plan);

        if (EditorUtility.DisplayDialog("Move Unused Assets",
                $"빌드 씬에서 쓰지 않는 에셋을 {UnusedRoot} 로 옮깁니다. (원래 경로 구조 유지)\n\n폴더: {plan.FolderMoves.Count}개\n파일: {plan.FileMoves.Count}개\n\n자세한 목록: {ReportPath}",
                "이동", "취소") == false)
            return;

        if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo() == false)
            return;

        List<string> sources = plan.FolderMoves.Concat(plan.FileMoves).ToList();

        // 대상 폴더는 배치 편집 전에 미리 만든다.
        foreach (string source in sources)
            EnsureFolder(Path.GetDirectoryName(ToUnusedPath(source)).Replace('\\', '/'));

        List<string> errors = MoveAssets(sources, ToUnusedPath);

        DeleteEmptyFolders("Assets");
        AssetDatabase.Refresh();

        Debug.Log($"[ProjectCleanup] 미사용 에셋 이동 완료: {sources.Count - errors.Count}/{sources.Count}" +
                  (errors.Count > 0 ? $"\n실패:\n{string.Join("\n", errors)}" : ""));
    }

    private static CleanupPlan BuildPlan()
    {
        HashSet<string> used = CollectUsedAssets();
        CleanupPlan plan = new() { UsedCount = used.Count };

        foreach (string topFolder in AssetDatabase.GetSubFolders("Assets"))
        {
            if (IsProtected(topFolder))
                continue;

            List<string> files = AssetDatabase.FindAssets("", new[] { topFolder })
                .Select(AssetDatabase.GUIDToAssetPath)
                .Distinct()
                .Where(path => AssetDatabase.IsValidFolder(path) == false)
                .ToList();

            bool folderUsed = files.Any(used.Contains);
            bool hasContent = files.Any(path => IsCode(path) == false);

            // 사용하는 에셋이 하나도 없는 에셋 팩은 폴더째 옮긴다. (asmdef/Editor 구조가 그대로 유지된다)
            if (folderUsed == false && hasContent)
            {
                plan.FolderMoves.Add(topFolder);
                continue;
            }

            foreach (string file in files)
            {
                if (used.Contains(file) || IsProtected(file) || IsInResourcesFolder(file))
                    continue;

                if (IsCode(file))
                {
                    if (file.StartsWith("Assets/Crowd Runner/"))
                        plan.UnusedCode.Add(file);
                    continue;
                }

                plan.FileMoves.Add(file);
            }
        }

        plan.FileMoves.Sort(StringComparer.Ordinal);
        return plan;
    }

    private static HashSet<string> CollectUsedAssets()
    {
        List<string> roots = new();

        roots.AddRange(EditorBuildSettings.scenes.Where(scene => scene.enabled).Select(scene => scene.path));

        // Resources, StreamingAssets 는 런타임에 경로로 로드되므로 전부 사용 중으로 본다.
        roots.AddRange(AssetDatabase.GetAllAssetPaths()
            .Where(path => path.StartsWith("Assets/") && (IsInResourcesFolder(path) || path.StartsWith("Assets/StreamingAssets/")))
            .Where(path => AssetDatabase.IsValidFolder(path) == false));

        // ProjectSettings(렌더 파이프라인, 아이콘, 프리셋 등)에서 참조하는 에셋
        Regex guidRegex = new("guid: ([0-9a-f]{32})");
        foreach (string settingsPath in Directory.GetFiles("ProjectSettings", "*.asset"))
        {
            foreach (Match match in guidRegex.Matches(File.ReadAllText(settingsPath)))
            {
                string path = AssetDatabase.GUIDToAssetPath(match.Groups[1].Value);
                if (string.IsNullOrEmpty(path) == false && path.StartsWith("Assets/"))
                    roots.Add(path);
            }
        }

        HashSet<string> used = new(roots);
        used.UnionWith(AssetDatabase.GetDependencies(roots.ToArray(), true));
        return used;
    }

    private static void WriteReport(CleanupPlan plan)
    {
        StringBuilder report = new();
        report.AppendLine($"# Unused Assets Report ({DateTime.Now:yyyy-MM-dd HH:mm})");
        report.AppendLine($"사용 중인 에셋: {plan.UsedCount}");
        report.AppendLine();
        report.AppendLine($"## 폴더째 이동 ({plan.FolderMoves.Count})");
        plan.FolderMoves.ForEach(path => report.AppendLine(path));
        report.AppendLine();
        report.AppendLine($"## 파일 이동 ({plan.FileMoves.Count})");
        plan.FileMoves.ForEach(path => report.AppendLine(path));
        report.AppendLine();
        report.AppendLine($"## 에셋에서 참조하지 않는 프로젝트 스크립트 - 이동하지 않음 ({plan.UnusedCode.Count})");
        plan.UnusedCode.ForEach(path => report.AppendLine(path));

        Directory.CreateDirectory(Path.GetDirectoryName(ReportPath));
        File.WriteAllText(ReportPath, report.ToString());
    }

    private static string ToUnusedPath(string assetPath)
    {
        return $"{UnusedRoot}/{assetPath.Substring("Assets/".Length)}";
    }

    private static bool IsProtected(string path)
    {
        return ProtectedFolders.Any(folder => path == folder || path.StartsWith(folder + "/"))
               || path.Count(c => c == '/') == 1 && AssetDatabase.IsValidFolder(path) == false;
    }

    private static bool IsInResourcesFolder(string path)
    {
        return path.Contains("/Resources/");
    }

    private static bool IsCode(string path)
    {
        return CodeExtensions.Contains(Path.GetExtension(path));
    }

    #endregion

    #region 4. Third Party

    [MenuItem(MenuRoot + "4. Organize Third Party Folders")]
    private static void OrganizeThirdPartyFolders()
    {
        List<string> sources = ThirdPartyFolders.Where(AssetDatabase.IsValidFolder).ToList();
        if (sources.Count == 0)
        {
            Debug.Log("[ProjectCleanup] 옮길 서드파티 폴더가 없습니다.");
            return;
        }

        if (EditorUtility.DisplayDialog("Organize Third Party Folders",
                $"아래 폴더를 {ThirdPartyRoot} 아래로 옮깁니다.\n\n{string.Join("\n", sources)}", "이동", "취소") == false)
            return;

        EnsureFolder(ThirdPartyRoot);
        List<string> errors = MoveAssets(sources, source => $"{ThirdPartyRoot}/{Path.GetFileName(source)}");

        Debug.Log($"[ProjectCleanup] 서드파티 폴더 정리 완료: {sources.Count - errors.Count}/{sources.Count}" +
                  (errors.Count > 0 ? $"\n실패:\n{string.Join("\n", errors)}" : ""));
    }

    #endregion

    #region 5. Delete Unreferenced

    // 어떤 에셋/코드에서도 참조하지 않는 파일 (참조 분석 결과)
    private static readonly string[] UnreferencedPaths =
    {
        UnusedRoot,
        "Assets/Plugins/UniTask",
        "Assets/TextMesh Pro/Documentation",
        "Assets/TextMesh Pro/Sprites/EmojiOne.json",
        "Assets/TextMesh Pro/Shaders/TMP_Bitmap-Custom-Atlas.shader",
        "Assets/TextMesh Pro/Shaders/TMP_Bitmap-Mobile.shader",
        "Assets/TextMesh Pro/Shaders/TMP_Bitmap.shader",
        "Assets/TextMesh Pro/Shaders/TMP_SDF Overlay.shader",
        "Assets/TextMesh Pro/Shaders/TMP_SDF SSD.shader",
        "Assets/TextMesh Pro/Shaders/TMP_SDF-Mobile Masking.shader",
        "Assets/TextMesh Pro/Shaders/TMP_SDF-Mobile Overlay.shader",
        "Assets/TextMesh Pro/Shaders/TMP_SDF-Mobile SSD.shader",
        "Assets/TextMesh Pro/Shaders/TMP_SDF-Surface-Mobile.shader",
        "Assets/TextMesh Pro/Shaders/TMP_SDF-Surface.shader",
        "Assets/TextMesh Pro/Shaders/TMPro_Mobile.cginc",
        "Assets/TextMesh Pro/Shaders/TMPro_Surface.cginc",
        "Assets/Crowd Runner/Prefabs/ETC/NewBehaviourScript.cs",
        "Assets/Crowd Runner/Scripts/Data/TSVWriter.cs",
        "Assets/Crowd Runner/Scripts/FireEffect/CFX_AutoRotate.cs",
        "Assets/Crowd Runner/Scripts/FireEffect/CFX_AutodestructWhenNoChildren.cs",
        "Assets/Crowd Runner/Scripts/FireEffect/CFX_InspectorHelp.cs",
        "Assets/Crowd Runner/Scripts/FireEffect/CFX_LightIntensityFade.cs",
        "Assets/Crowd Runner/Scripts/FireEffect/CFX_ShurikenThreadFix.cs",
        "Assets/Crowd Runner/Scripts/FireFieldDamage.cs",
        "Assets/Crowd Runner/Scripts/Player/CrowdCounter.cs",
        "Assets/Crowd Runner/Scripts/Player/PlayerSelector.cs",
        "Assets/Crowd Runner/Scripts/Player/RunnerSelector.cs",
        "Assets/ThirdParty/Quirky Series Ultimate/_Scripts",
    };

    // Unity가 에셋으로 인식하지 않는 폴더 (점으로 시작)
    private const string IdeaFolder = "Assets/Crowd Runner/Scripts/Weapon/.idea";

    [MenuItem(MenuRoot + "5. Delete Unreferenced Assets")]
    private static void DeleteUnreferencedAssets()
    {
        List<string> targets = UnreferencedPaths.Where(path => AssetDatabase.LoadMainAssetAtPath(path) != null || AssetDatabase.IsValidFolder(path)).ToList();
        bool hasIdeaFolder = Directory.Exists(IdeaFolder);

        if (targets.Count == 0 && hasIdeaFolder == false)
        {
            Debug.Log("[ProjectCleanup] 삭제할 파일이 없습니다.");
            return;
        }

        if (EditorUtility.DisplayDialog("Delete Unreferenced Assets",
                $"아래 {targets.Count + (hasIdeaFolder ? 1 : 0)}개 항목을 삭제합니다.\n\n{string.Join("\n", targets)}{(hasIdeaFolder ? "\n" + IdeaFolder : "")}",
                "삭제", "취소") == false)
            return;

        List<string> failed = new();
        AssetDatabase.DeleteAssets(targets.ToArray(), failed);

        if (hasIdeaFolder)
            Directory.Delete(IdeaFolder, true);

        DeleteEmptyFolders("Assets");
        AssetDatabase.Refresh();

        Debug.Log($"[ProjectCleanup] 미참조 파일 삭제 완료: {targets.Count - failed.Count}/{targets.Count}" +
                  (failed.Count > 0 ? $"\n실패:\n{string.Join("\n", failed)}" : ""));
    }

    #endregion

    #region Utility

    private static List<string> MoveAssets(List<string> sources, Func<string, string> getDestination)
    {
        List<string> errors = new();

        try
        {
            AssetDatabase.StartAssetEditing();

            for (int i = 0; i < sources.Count; i++)
            {
                string source = sources[i];
                if (i % 50 == 0)
                    EditorUtility.DisplayProgressBar("Project Cleanup", source, (float)i / sources.Count);

                string error = AssetDatabase.MoveAsset(source, getDestination(source));
                if (string.IsNullOrEmpty(error) == false)
                    errors.Add($"{source} : {error}");
            }
        }
        finally
        {
            AssetDatabase.StopAssetEditing();
            EditorUtility.ClearProgressBar();
        }

        return errors;
    }

    private static void EnsureFolder(string folderPath)
    {
        if (AssetDatabase.IsValidFolder(folderPath))
            return;

        string parent = Path.GetDirectoryName(folderPath).Replace('\\', '/');
        EnsureFolder(parent);
        AssetDatabase.CreateFolder(parent, Path.GetFileName(folderPath));
    }

    private static bool DeleteEmptyFolders(string folderPath)
    {
        bool isEmpty = true;

        foreach (string subFolder in AssetDatabase.GetSubFolders(folderPath))
        {
            if (DeleteEmptyFolders(subFolder) == false)
                isEmpty = false;
        }

        bool hasFiles = Directory.GetFiles(folderPath).Any(file => file.EndsWith(".meta") == false);
        if (isEmpty == false || hasFiles || folderPath == "Assets")
            return false;

        AssetDatabase.DeleteAsset(folderPath);
        return true;
    }

    #endregion
}
