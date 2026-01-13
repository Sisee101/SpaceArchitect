using UnityEngine;
using UnityEditor;

/// <summary>
/// Editor 脚本：用于快速创建 EmployeeHandbookData 资源文件
/// </summary>
public class CreateEmployeeHandbookData : EditorWindow
{
    [MenuItem("Tools/Create Employee Handbook Data")]
    public static void CreateAsset()
    {
        // 创建资源实例
        EmployeeHandbookDataConfig asset = ScriptableObject.CreateInstance<EmployeeHandbookDataConfig>();
        
        // 设置默认数据（3个选项）
        asset.sections = new System.Collections.Generic.List<EmployeeHandbookDataConfig.HandbookSection>
        {
            new EmployeeHandbookDataConfig.HandbookSection
            {
                sectionName = "入职指南"
            },
            new EmployeeHandbookDataConfig.HandbookSection
            {
                sectionName = "业务流程"
            },
            new EmployeeHandbookDataConfig.HandbookSection
            {
                sectionName = "系统架构"
            }
        };
        
        // 确保 Data 文件夹存在
        string folderPath = "Assets/Data";
        if (!AssetDatabase.IsValidFolder(folderPath))
        {
            AssetDatabase.CreateFolder("Assets", "Data");
        }
        
        // 创建资源文件
        string assetPath = folderPath + "/EmployeeHandbookData.asset";
        AssetDatabase.CreateAsset(asset, assetPath);
        AssetDatabase.SaveAssets();
        
        // 选中创建的资源
        EditorUtility.FocusProjectWindow();
        Selection.activeObject = asset;
        
        Debug.Log($"已创建员工手册数据配置文件: {assetPath}");
    }
}
