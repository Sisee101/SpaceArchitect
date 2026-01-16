using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections.Generic;

/// <summary>
/// 场景调试工具 - 帮助验证和排除错误
/// 用于对比不同场景的配置差异
/// </summary>
public class SceneDebugger : MonoBehaviour
{
    [Header("调试选项")]
    [Tooltip("是否在启动时自动运行检查")]
    [SerializeField] private bool autoCheckOnStart = true;
    
    [Tooltip("是否检查 NBody 对象")]
    [SerializeField] private bool checkNBody = true;
    
    [Tooltip("是否检查 GravityEngine")]
    [SerializeField] private bool checkGravityEngine = true;
    
    [Tooltip("是否检查相机")]
    [SerializeField] private bool checkCamera = true;
    
    void Start()
    {
        if (autoCheckOnStart)
        {
            CheckCurrentScene();
        }
    }
    
    /// <summary>
    /// 检查当前场景
    /// </summary>
    [ContextMenu("检查当前场景")]
    public void CheckCurrentScene()
    {
        Scene currentScene = gameObject.scene;
        Debug.Log($"=== 场景检查开始: {currentScene.name} ===");
        
        if (checkGravityEngine)
        {
            CheckGravityEngine(currentScene);
        }
        
        if (checkNBody)
        {
            CheckNBodyObjects(currentScene);
        }
        
        if (checkCamera)
        {
            CheckCameras(currentScene);
        }
        
        Debug.Log($"=== 场景检查完成: {currentScene.name} ===");
    }
    
    /// <summary>
    /// 检查 GravityEngine
    /// </summary>
    private void CheckGravityEngine(Scene scene)
    {
        Debug.Log($"\n[GravityEngine 检查]");
        
        // 查找所有场景中的 GravityEngine
        GravityEngine[] allGravityEngines = FindObjectsOfType<GravityEngine>();
        List<GravityEngine> sceneGravityEngines = new List<GravityEngine>();
        
        foreach (GravityEngine ge in allGravityEngines)
        {
            if (ge.gameObject.scene == scene)
            {
                sceneGravityEngines.Add(ge);
            }
        }
        
        Debug.Log($"  - 当前场景 ({scene.name}) 中的 GravityEngine 数量: {sceneGravityEngines.Count}");
        Debug.Log($"  - 所有场景中的 GravityEngine 数量: {allGravityEngines.Length}");
        
        foreach (GravityEngine ge in sceneGravityEngines)
        {
            Debug.Log($"  - GravityEngine: {ge.name} (场景: {ge.gameObject.scene.name}, 启用: {ge.enabled}, 已设置: {ge.IsSetup()})");
        }
        
        // 检查是否有其他场景的 GravityEngine
        foreach (GravityEngine ge in allGravityEngines)
        {
            if (ge.gameObject.scene != scene)
            {
                Debug.LogWarning($"  - ⚠️ 发现其他场景的 GravityEngine: {ge.name} (场景: {ge.gameObject.scene.name})");
            }
        }
    }
    
    /// <summary>
    /// 检查 NBody 对象
    /// </summary>
    private void CheckNBodyObjects(Scene scene)
    {
        Debug.Log($"\n[NBody 对象检查]");
        
        // 查找所有场景中的 NBody
        NBody[] allNbodies = FindObjectsOfType<NBody>();
        List<NBody> sceneNbodies = new List<NBody>();
        List<NBody> nbodiesWithEngineRef = new List<NBody>();
        
        foreach (NBody nbody in allNbodies)
        {
            if (nbody.gameObject.scene == scene)
            {
                sceneNbodies.Add(nbody);
                if (nbody.engineRef != null)
                {
                    nbodiesWithEngineRef.Add(nbody);
                }
            }
        }
        
        Debug.Log($"  - 当前场景 ({scene.name}) 中的 NBody 数量: {sceneNbodies.Count}");
        Debug.Log($"  - 所有场景中的 NBody 数量: {allNbodies.Length}");
        Debug.Log($"  - 当前场景中已有 engineRef 的 NBody 数量: {nbodiesWithEngineRef.Count}");
        
        // 列出所有有 engineRef 的对象（这可能是问题的根源）
        if (nbodiesWithEngineRef.Count > 0)
        {
            Debug.LogWarning($"  - ⚠️ 发现 {nbodiesWithEngineRef.Count} 个已有 engineRef 的对象：");
            foreach (NBody nbody in nbodiesWithEngineRef)
            {
                Debug.LogWarning($"    * {nbody.gameObject.name} (场景: {nbody.gameObject.scene.name}, " +
                                $"engineRef.index: {nbody.engineRef.index}, " +
                                $"engineRef.bodyType: {nbody.engineRef.bodyType})");
            }
        }
        
        // 检查是否有重复名称的对象
        Dictionary<string, List<NBody>> nameMap = new Dictionary<string, List<NBody>>();
        foreach (NBody nbody in allNbodies)
        {
            string name = nbody.gameObject.name;
            if (!nameMap.ContainsKey(name))
            {
                nameMap[name] = new List<NBody>();
            }
            nameMap[name].Add(nbody);
        }
        
        foreach (var kvp in nameMap)
        {
            if (kvp.Value.Count > 1)
            {
                Debug.LogWarning($"  - ⚠️ 发现重复名称的对象: {kvp.Key} (共 {kvp.Value.Count} 个)");
                foreach (NBody nbody in kvp.Value)
                {
                    Debug.LogWarning($"    * {nbody.gameObject.name} (场景: {nbody.gameObject.scene.name}, " +
                                    $"已有engineRef: {nbody.engineRef != null})");
                }
            }
        }
    }
    
    /// <summary>
    /// 检查相机
    /// </summary>
    private void CheckCameras(Scene scene)
    {
        Debug.Log($"\n[相机检查]");
        
        Camera[] allCameras = FindObjectsOfType<Camera>();
        List<Camera> sceneCameras = new List<Camera>();
        
        foreach (Camera cam in allCameras)
        {
            if (cam.gameObject.scene == scene)
            {
                sceneCameras.Add(cam);
            }
        }
        
        Debug.Log($"  - 当前场景 ({scene.name}) 中的相机数量: {sceneCameras.Count}");
        Debug.Log($"  - 所有场景中的相机数量: {allCameras.Length}");
        
        foreach (Camera cam in sceneCameras)
        {
            Debug.Log($"  - 相机: {cam.name} (场景: {cam.gameObject.scene.name}, " +
                     $"Tag: {cam.tag}, 启用: {cam.enabled}, Active: {cam.gameObject.activeInHierarchy})");
        }
        
        // 检查 Camera.main
        if (Camera.main != null)
        {
            Debug.Log($"  - Camera.main: {Camera.main.name} (场景: {Camera.main.gameObject.scene.name})");
            if (Camera.main.gameObject.scene != scene)
            {
                Debug.LogWarning($"  - ⚠️ Camera.main 不属于当前场景！");
            }
        }
    }
    
    /// <summary>
    /// 对比两个场景的差异（在编辑器中调用）
    /// </summary>
    [ContextMenu("对比 level1 和 level2 的差异")]
    public void CompareScenes()
    {
        Debug.Log("=== 场景对比功能需要在运行时使用 ===");
        Debug.Log("请在运行时分别加载 level1 和 level2 场景，然后查看日志输出");
    }
}

