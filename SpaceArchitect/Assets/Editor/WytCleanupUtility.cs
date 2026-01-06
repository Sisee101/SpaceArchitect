#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public static class WytCleanupUtility
{
    private const string WytRoot = "Assets/Scripts/wyt";
    private const string ScriptsRoot = WytRoot + "/Scripts";

    private static readonly HashSet<string> KeepScriptFolders = new HashSet<string>
    {
        ScriptsRoot + "/Engine",
        ScriptsRoot + "/GEConsole",
        ScriptsRoot + "/Math",
        ScriptsRoot + "/Orbits",
        ScriptsRoot + "/SolarSystem"
    };

    [MenuItem("Tools/WYT/移除非核心内容", priority = 1000)]
    public static void TrimToCore()
    {
        if (!AssetDatabase.IsValidFolder(WytRoot))
        {
            Debug.LogWarning($"未找到 {WytRoot}。");
            return;
        }

        const string dialogTitle = "移除 WYT 非核心脚本";
        const string dialogMessage =
            "该操作将删除 wyt 中与 NBody/GravityEngine 无关的示例脚本、场景和 SG4P 资源，只保留核心运行所需的脚本。\n\n是否继续？";

        if (!EditorUtility.DisplayDialog(dialogTitle, dialogMessage, "确认删除", "取消"))
            return;

        AssetDatabase.StartAssetEditing();
        try
        {
            DeleteFolderIfExists($"{WytRoot}/Editor");
            DeleteFolderIfExists($"{WytRoot}/Scenes");
            DeleteFolderIfExists($"{WytRoot}/SGP4");
            CleanupScriptsFolder();
        }
        finally
        {
            AssetDatabase.StopAssetEditing();
            AssetDatabase.Refresh();
        }

        Debug.Log("已移除非核心 wyt 脚本。");
    }

    private static void CleanupScriptsFolder()
    {
        if (!AssetDatabase.IsValidFolder(ScriptsRoot))
            return;

        foreach (var subFolder in AssetDatabase.GetSubFolders(ScriptsRoot))
        {
            if (!KeepScriptFolders.Contains(subFolder))
            {
                AssetDatabase.DeleteAsset(subFolder);
            }
        }
    }

    private static void DeleteFolderIfExists(string assetPath)
    {
        if (AssetDatabase.IsValidFolder(assetPath))
        {
            AssetDatabase.DeleteAsset(assetPath);
        }
    }
}
#endif

