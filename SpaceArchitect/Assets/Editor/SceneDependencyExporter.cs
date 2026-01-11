using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;
using System.IO;

/// <summary>
/// 场景依赖导出工具
/// 自动查找场景的所有依赖资源，帮助精确导出Package
/// </summary>
public class SceneDependencyExporter : EditorWindow
{
    private List<Object> selectedScenes = new List<Object>();
    private List<string> dependencyPaths = new List<string>();
    private Vector2 scrollPosition;
    private bool showDependencies = true;
    
    [MenuItem("Tools/场景依赖导出工具")]
    public static void ShowWindow()
    {
        GetWindow<SceneDependencyExporter>("场景依赖导出工具");
    }
    
    void OnGUI()
    {
        GUILayout.Label("场景依赖导出工具", EditorStyles.boldLabel);
        GUILayout.Space(10);
        
        // 场景选择
        EditorGUILayout.LabelField("选择要导出的场景：", EditorStyles.boldLabel);
        
        if (GUILayout.Button("从Project窗口选择场景"))
        {
            selectedScenes.Clear();
            dependencyPaths.Clear();
            
            Object[] selected = Selection.objects;
            foreach (Object obj in selected)
            {
                if (obj is SceneAsset)
                {
                    selectedScenes.Add(obj);
                }
            }
            
            if (selectedScenes.Count > 0)
            {
                AnalyzeDependencies();
            }
            else
            {
                EditorUtility.DisplayDialog("提示", "请先在Project窗口中选择场景文件！", "确定");
            }
        }
        
        // 显示选中的场景
        if (selectedScenes.Count > 0)
        {
            EditorGUILayout.LabelField($"已选择 {selectedScenes.Count} 个场景：", EditorStyles.boldLabel);
            foreach (Object scene in selectedScenes)
            {
                EditorGUILayout.LabelField("  • " + scene.name);
            }
        }
        
        GUILayout.Space(10);
        
        // 依赖列表
        if (dependencyPaths.Count > 0)
        {
            showDependencies = EditorGUILayout.Foldout(showDependencies, 
                $"依赖资源列表 ({dependencyPaths.Count} 个)", true);
            
            if (showDependencies)
            {
                scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition, GUILayout.Height(300));
                
                // 按类型分组显示
                var grouped = dependencyPaths.GroupBy(p => Path.GetExtension(p).ToLower())
                    .OrderBy(g => g.Key);
                
                foreach (var group in grouped)
                {
                    string typeName = group.Key;
                    if (string.IsNullOrEmpty(typeName))
                        typeName = "其他";
                    
                    EditorGUILayout.LabelField($"\n{typeName} ({group.Count()}):", EditorStyles.boldLabel);
                    
                    foreach (string path in group.OrderBy(p => p))
                    {
                        EditorGUILayout.BeginHorizontal();
                        EditorGUILayout.LabelField("  • " + Path.GetFileName(path), GUILayout.Width(400));
                        EditorGUILayout.LabelField(path, EditorStyles.miniLabel);
                        
                        if (GUILayout.Button("定位", GUILayout.Width(50)))
                        {
                            Object obj = AssetDatabase.LoadAssetAtPath<Object>(path);
                            if (obj != null)
                            {
                                Selection.activeObject = obj;
                                EditorGUIUtility.PingObject(obj);
                            }
                        }
                        
                        EditorGUILayout.EndHorizontal();
                    }
                }
                
                EditorGUILayout.EndScrollView();
            }
            
            GUILayout.Space(10);
            
            // 导出按钮
            if (GUILayout.Button("导出 Package", GUILayout.Height(30)))
            {
                ExportPackage();
            }
        }
        else if (selectedScenes.Count > 0)
        {
            EditorGUILayout.HelpBox("点击上方按钮分析依赖", MessageType.Info);
        }
    }
    
    /// <summary>
    /// 分析依赖
    /// </summary>
    void AnalyzeDependencies()
    {
        dependencyPaths.Clear();
        HashSet<string> visited = new HashSet<string>();
        
        foreach (Object scene in selectedScenes)
        {
            string scenePath = AssetDatabase.GetAssetPath(scene);
            dependencyPaths.Add(scenePath); // 包含场景本身
            
            // 获取所有依赖
            string[] dependencies = AssetDatabase.GetDependencies(scenePath, true);
            
            foreach (string dep in dependencies)
            {
                // 排除场景本身
                if (dep == scenePath)
                    continue;
                
                // 排除Unity内置资源
                if (dep.StartsWith("Library/") || dep.StartsWith("ProjectSettings/"))
                    continue;
                
                // 排除.cs.meta文件
                if (dep.EndsWith(".meta"))
                    continue;
                
                if (!visited.Contains(dep))
                {
                    visited.Add(dep);
                    dependencyPaths.Add(dep);
                }
            }
        }
        
        Debug.Log($"找到 {dependencyPaths.Count} 个依赖资源");
    }
    
    /// <summary>
    /// 导出Package
    /// </summary>
    void ExportPackage()
    {
        if (dependencyPaths.Count == 0)
        {
            EditorUtility.DisplayDialog("错误", "没有可导出的资源！", "确定");
            return;
        }
        
        // 选择保存位置
        string fileName = "ScenesPackage.unitypackage";
        if (selectedScenes.Count == 1)
        {
            fileName = selectedScenes[0].name + ".unitypackage";
        }
        
        string path = EditorUtility.SaveFilePanel("导出Package", "", fileName, "unitypackage");
        
        if (string.IsNullOrEmpty(path))
            return;
        
        // 导出
        try
        {
            AssetDatabase.ExportPackage(dependencyPaths.ToArray(), path, ExportPackageOptions.Recurse);
            EditorUtility.DisplayDialog("成功", $"Package已导出到：\n{path}", "确定");
            Debug.Log($"Package已导出：{path}");
        }
        catch (System.Exception e)
        {
            EditorUtility.DisplayDialog("错误", $"导出失败：\n{e.Message}", "确定");
            Debug.LogError($"导出失败：{e}");
        }
    }
}
