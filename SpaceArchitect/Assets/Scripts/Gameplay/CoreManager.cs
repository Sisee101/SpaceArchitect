using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Core 管理器
/// 根据 SkillManager 的 Core1, Core2, Core3 布尔值来控制场景中 Core（引力枢纽）的激活数量
/// 三个布尔值的和就是场景中 Core 的 active 个数
/// </summary>
public class CoreManager : MonoBehaviour
{
    [Header("Core 设置")]
    [Tooltip("是否自动查找场景中所有 Tag 为 'Core' 的 GameObject（如果为 false，需要手动指定 coreList）")]
    [SerializeField] private bool autoFindCores = true;
    
    [Tooltip("Core GameObject 列表（如果 autoFindCores 为 false，需要手动拖入）")]
    [SerializeField] private List<GameObject> coreList = new List<GameObject>();
    
    [Tooltip("是否按名称排序 Core（如果为 true，会按 GameObject 名称排序后激活）")]
    [SerializeField] private bool sortCoresByName = true;
    
    [Header("调试设置")]
    [Tooltip("显示调试信息")]
    [SerializeField] private bool showDebugLogs = true;

    // 缓存的 Core 列表（排序后的）
    private List<GameObject> sortedCoreList = new List<GameObject>();

    void Awake()
    {
        // 在 Awake 中初始化，确保在其他脚本 Start 之前完成
        UpdateCoreActivation();
    }

    void Start()
    {
        // 在 Start 中再次更新，确保所有对象都已初始化
        UpdateCoreActivation();
        
        // 订阅游戏重置事件
        if (EventManager.Instance != null)
        {
            EventManager.Instance.OnGameReset += HandleGameReset;
            if (showDebugLogs)
            {
                Debug.Log("CoreManager: 已订阅游戏重置事件");
            }
        }
    }

    void OnEnable()
    {
        // 对象激活时更新 Core 状态
        UpdateCoreActivation();
    }

    void OnDestroy()
    {
        // 取消订阅游戏重置事件
        if (EventManager.Instance != null)
        {
            EventManager.Instance.OnGameReset -= HandleGameReset;
        }
    }

    /// <summary>
    /// 游戏重置事件处理
    /// </summary>
    private void HandleGameReset()
    {
        if (showDebugLogs)
        {
            Debug.Log("CoreManager: 收到游戏重置事件，重新更新 Core 激活状态");
        }
        
        // 重新从 SkillManager 读取技能状态并更新 Core 激活状态
        UpdateCoreActivation();
    }

    /// <summary>
    /// 更新 Core 的激活状态
    /// 根据 SkillManager 的 Core1, Core2, Core3 布尔值来决定激活多少个 Core
    /// </summary>
    private void UpdateCoreActivation()
    {
        // 收集 Core 列表
        CollectCores();
        
        if (sortedCoreList.Count == 0)
        {
            if (showDebugLogs)
            {
                Debug.LogWarning("CoreManager: 场景中没有找到任何 Core GameObject！");
            }
            return;
        }

        // 从 SkillManager 读取 Core 技能状态
        bool core1 = SkillManager.Core1;
        bool core2 = SkillManager.Core2;
        bool core3 = SkillManager.Core3;
        
        // 计算解锁的技能数量（true=1, false=0）
        int unlockedCount = 0;
        if (core1) unlockedCount++;
        if (core2) unlockedCount++;
        if (core3) unlockedCount++;
        
        // 计算需要激活的 Core 数量
        int activeCount = Mathf.Min(unlockedCount, sortedCoreList.Count);
        
        if (showDebugLogs)
        {
            Debug.Log($"CoreManager: 从 SkillManager 读取 Core 技能状态 - Core1={core1}, Core2={core2}, Core3={core3}, 解锁数量={unlockedCount}, 需要激活的 Core 数量={activeCount}");
        }
        
        // 激活对应数量的 Core
        for (int i = 0; i < sortedCoreList.Count; i++)
        {
            if (sortedCoreList[i] == null)
            {
                continue;
            }
            
            bool shouldBeActive = i < activeCount;
            
            if (sortedCoreList[i].activeSelf != shouldBeActive)
            {
                sortedCoreList[i].SetActive(shouldBeActive);
                
                if (showDebugLogs)
                {
                    Debug.Log($"CoreManager: {(shouldBeActive ? "激活" : "禁用")} Core: {sortedCoreList[i].name}");
                }
            }
        }
        
        if (showDebugLogs)
        {
            Debug.Log($"CoreManager: Core 激活状态更新完成 - 已激活 {activeCount}/{sortedCoreList.Count} 个 Core");
        }
    }

    /// <summary>
    /// 收集场景中的 Core GameObject
    /// </summary>
    private void CollectCores()
    {
        sortedCoreList.Clear();
        
        if (autoFindCores)
        {
            // 自动查找场景中所有 Tag 为 "Core" 的 GameObject
            GameObject[] cores = GameObject.FindGameObjectsWithTag("Core");
            
            foreach (GameObject core in cores)
            {
                if (core != null)
                {
                    sortedCoreList.Add(core);
                }
            }
            
            if (showDebugLogs)
            {
                Debug.Log($"CoreManager: 自动查找到 {sortedCoreList.Count} 个 Core GameObject");
            }
        }
        else
        {
            // 使用手动指定的列表
            foreach (GameObject core in coreList)
            {
                if (core != null)
                {
                    sortedCoreList.Add(core);
                }
            }
            
            if (showDebugLogs)
            {
                Debug.Log($"CoreManager: 使用手动指定的 Core 列表，共 {sortedCoreList.Count} 个");
            }
        }
        
        // 如果启用排序，按名称排序
        if (sortCoresByName && sortedCoreList.Count > 0)
        {
            sortedCoreList.Sort((a, b) => string.Compare(a.name, b.name));
            
            if (showDebugLogs)
            {
                Debug.Log("CoreManager: 已按名称排序 Core 列表");
            }
        }
    }

    /// <summary>
    /// 刷新 Core 激活状态（从 SkillManager 重新读取）
    /// 当技能解锁后，可以调用此方法来更新 Core 的激活状态
    /// </summary>
    public void RefreshCoreActivation()
    {
        if (showDebugLogs)
        {
            Debug.Log("CoreManager: 手动刷新 Core 激活状态");
        }
        
        UpdateCoreActivation();
    }

    /// <summary>
    /// 获取当前激活的 Core 数量
    /// </summary>
    /// <returns>激活的 Core 数量</returns>
    public int GetActiveCoreCount()
    {
        int count = 0;
        foreach (GameObject core in sortedCoreList)
        {
            if (core != null && core.activeSelf)
            {
                count++;
            }
        }
        return count;
    }

    /// <summary>
    /// 获取场景中所有 Core 的数量
    /// </summary>
    /// <returns>所有 Core 的数量</returns>
    public int GetTotalCoreCount()
    {
        return sortedCoreList.Count;
    }

    /// <summary>
    /// 获取所有 Core 的列表（只读）
    /// </summary>
    /// <returns>Core 列表的只读副本</returns>
    public List<GameObject> GetCoreList()
    {
        return new List<GameObject>(sortedCoreList);
    }
}

