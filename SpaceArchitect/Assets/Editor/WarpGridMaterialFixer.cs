using UnityEngine;
using UnityEditor;

/// <summary>
/// 编辑器工具：将 WarpGrid 的材质从材质变体改为使用 background 材质
/// </summary>
public class WarpGridMaterialFixer : EditorWindow
{
    [MenuItem("Tools/修复 WarpGrid 材质")]
    public static void ShowWindow()
    {
        GetWindow<WarpGridMaterialFixer>("修复 WarpGrid 材质");
    }

    private Material backgroundMaterial;

    void OnGUI()
    {
        GUILayout.Label("修复 WarpGrid 材质设置", EditorStyles.boldLabel);
        GUILayout.Space(10);

        // 材质选择
        backgroundMaterial = (Material)EditorGUILayout.ObjectField(
            "Background 材质",
            backgroundMaterial,
            typeof(Material),
            false
        );

        GUILayout.Space(10);

        if (GUILayout.Button("自动查找 Background 材质"))
        {
            string[] guids = AssetDatabase.FindAssets("background t:Material");
            if (guids.Length > 0)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[0]);
                backgroundMaterial = AssetDatabase.LoadAssetAtPath<Material>(path);
                EditorUtility.DisplayDialog("成功", $"找到材质: {path}", "确定");
            }
            else
            {
                EditorUtility.DisplayDialog("错误", "未找到 background 材质", "确定");
            }
        }

        GUILayout.Space(10);

        if (GUILayout.Button("修复场景中所有 WarpGrid 的材质"))
        {
            if (backgroundMaterial == null)
            {
                EditorUtility.DisplayDialog("错误", "请先选择或查找 Background 材质", "确定");
                return;
            }

            FixAllWarpGridMaterials();
        }

        GUILayout.Space(10);
        GUILayout.Label("说明：", EditorStyles.boldLabel);
        GUILayout.Label("1. 点击'自动查找'找到 background 材质");
        GUILayout.Label("2. 点击'修复'将所有 WarpGrid 的材质改为 background");
        GUILayout.Label("3. 或者手动在 Inspector 中将 Material 改为 background");
    }

    private void FixAllWarpGridMaterials()
    {
        // 查找场景中所有有 WarpGridController 组件的 GameObject
        WarpGridController[] warpGrids = FindObjectsOfType<WarpGridController>();
        
        if (warpGrids.Length == 0)
        {
            EditorUtility.DisplayDialog("提示", "场景中未找到 WarpGrid 对象", "确定");
            return;
        }

        int fixedCount = 0;

        foreach (WarpGridController warpGrid in warpGrids)
        {
            SpriteRenderer renderer = warpGrid.GetComponent<SpriteRenderer>();
            if (renderer != null && renderer.sharedMaterial != backgroundMaterial)
            {
                Undo.RecordObject(renderer, "更改 WarpGrid 材质");
                renderer.sharedMaterial = backgroundMaterial;
                fixedCount++;
                EditorUtility.SetDirty(renderer);
            }
        }

        EditorUtility.DisplayDialog("完成", 
            $"已修复 {fixedCount} 个 WarpGrid 的材质设置", "确定");
    }
}













