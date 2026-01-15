using UnityEngine;

/// <summary>
/// 行星解锁管理器
/// 管理13个行星的解锁状态，并提供持久化功能
/// </summary>
public class PlanetUnlockManager : MonoBehaviour
{
    private static PlanetUnlockManager _instance;
    private static bool[] unlockedPlanets = new bool[13]; // 13个行星的解锁状态
    private const string PLAYER_PREFS_KEY_PREFIX = "PlanetUnlockStatus_";
    
    [Header("行星解锁状态（Inspector显示）")]
    [Tooltip("13个行星的解锁状态，可在Inspector中查看和编辑（仅用于调试，运行时以PlayerPrefs为准）")]
    [SerializeField] private bool[] planetUnlockStates = new bool[13]; // 暴露在Inspector中的解锁状态
    
    [Header("调试选项")]
    [Tooltip("是否在加载时自动同步Inspector显示")]
    [SerializeField] private bool autoSyncToInspector = true; // 是否自动同步到Inspector
    
    /// <summary>
    /// 获取PlanetUnlockManager单例
    /// </summary>
    public static PlanetUnlockManager Instance
    {
        get
        {
            if (_instance == null)
            {
                // 首先尝试查找已存在的实例
                _instance = FindObjectOfType<PlanetUnlockManager>();
                
                // 如果找不到，创建新实例（全局单例，使用DontDestroyOnLoad）
                // PlanetUnlockManager是全局状态管理器，可以在任何场景中创建
                if (_instance == null)
                {
                    GameObject go = new GameObject("PlanetUnlockManager");
                    _instance = go.AddComponent<PlanetUnlockManager>();
                    DontDestroyOnLoad(go);
                    Debug.Log($"PlanetUnlockManager: 创建新的全局实例");
                }
            }
            return _instance;
        }
    }
    
    void Awake()
    {
        // 确保单例
        if (_instance == null)
        {
            _instance = this;
            DontDestroyOnLoad(gameObject);
            
            // 初始化Inspector数组
            if (planetUnlockStates == null || planetUnlockStates.Length != 13)
            {
                planetUnlockStates = new bool[13];
            }
            
            // 加载已保存的解锁状态
            LoadUnlockStates();
            
            // 同步到Inspector显示
            if (autoSyncToInspector)
            {
                SyncToInspector();
            }
            
            Debug.Log($"PlanetUnlockManager: 实例已初始化（场景: {gameObject.scene.name}）");
        }
        else if (_instance != this)
        {
            // 如果已存在实例，且当前实例不是单例，销毁当前实例
            // 这样可以确保全局只有一个实例
            Debug.LogWarning($"检测到多个PlanetUnlockManager实例，销毁重复的实例（场景: {gameObject.scene.name}，已有实例在场景: {_instance.gameObject.scene.name}）");
            Destroy(gameObject);
        }
    }
    
    void OnDestroy()
    {
        // 如果当前实例被销毁，且它是单例，清除引用
        if (_instance == this)
        {
            _instance = null;
        }
    }
    
    /// <summary>
    /// 检查指定索引的行星是否已解锁
    /// </summary>
    /// <param name="index">行星索引（0-12）</param>
    /// <returns>是否已解锁</returns>
    public static bool IsPlanetUnlocked(int index)
    {
        if (index < 0 || index >= 13)
        {
            Debug.LogWarning($"PlanetUnlockManager: 索引 {index} 超出范围（0-12）！");
            return false;
        }
        return unlockedPlanets[index];
    }
    
    /// <summary>
    /// 解锁指定索引的行星
    /// </summary>
    /// <param name="index">行星索引（0-12）</param>
    public static void UnlockPlanet(int index)
    {
        if (index < 0 || index >= 13)
        {
            Debug.LogWarning($"PlanetUnlockManager: 索引 {index} 超出范围（0-12）！");
            return;
        }
        
        // 如果已经解锁，直接返回
        if (unlockedPlanets[index])
        {
            Debug.Log($"PlanetUnlockManager: 行星 {index} 已经解锁，无需重复解锁");
            return;
        }
        
        // 解锁行星
        unlockedPlanets[index] = true;
        
        // 保存到PlayerPrefs
        SaveUnlockState(index);
        
        // 同步到Inspector显示
        if (_instance != null && _instance.autoSyncToInspector)
        {
            _instance.SyncToInspector();
        }
        
        // 触发事件（延迟访问EventManager，避免在场景加载过程中过早创建EventManager实例）
        // 使用协程延迟一帧，确保EventManager已经正确初始化
        if (_instance != null)
        {
            _instance.StartCoroutine(_instance.TriggerUnlockEventDelayed(index));
        }
        else
        {
            // 如果没有实例，直接尝试触发事件（这种情况应该很少见）
            if (EventManager.Instance != null)
            {
                EventManager.Instance.TriggerPlanetUnlocked(index);
                Debug.Log($"PlanetUnlockManager: 行星 {index} 已解锁并触发事件");
            }
            else
            {
                Debug.LogWarning("PlanetUnlockManager: EventManager实例不存在，无法触发解锁事件！");
            }
        }
    }
    
    /// <summary>
    /// 延迟触发解锁事件（协程）
    /// 避免在场景加载过程中过早访问EventManager.Instance
    /// </summary>
    private System.Collections.IEnumerator TriggerUnlockEventDelayed(int index)
    {
        // 等待一帧，确保场景加载完成
        yield return null;
        
        // 现在安全地访问EventManager
        if (EventManager.Instance != null)
        {
            EventManager.Instance.TriggerPlanetUnlocked(index);
            Debug.Log($"PlanetUnlockManager: 行星 {index} 已解锁并触发事件");
        }
        else
        {
            Debug.LogWarning("PlanetUnlockManager: EventManager实例不存在，无法触发解锁事件！");
        }
    }
    
    /// <summary>
    /// 从PlayerPrefs加载所有解锁状态
    /// </summary>
    private static void LoadUnlockStates()
    {
        for (int i = 0; i < 13; i++)
        {
            string key = PLAYER_PREFS_KEY_PREFIX + i;
            unlockedPlanets[i] = PlayerPrefs.GetInt(key, 0) == 1;
        }
        
        // 统计已解锁的行星数量
        int unlockedCount = 0;
        for (int i = 0; i < 13; i++)
        {
            if (unlockedPlanets[i]) unlockedCount++;
        }
        
        Debug.Log($"PlanetUnlockManager: 已加载解锁状态，共 {unlockedCount}/13 个行星已解锁");
    }
    
    /// <summary>
    /// 保存指定索引的行星解锁状态到PlayerPrefs
    /// </summary>
    /// <param name="index">行星索引（0-12）</param>
    private static void SaveUnlockState(int index)
    {
        string key = PLAYER_PREFS_KEY_PREFIX + index;
        PlayerPrefs.SetInt(key, unlockedPlanets[index] ? 1 : 0);
        PlayerPrefs.Save(); // 立即保存
    }
    
    /// <summary>
    /// 重置所有行星的解锁状态（用于测试或新游戏）
    /// </summary>
    public static void ResetAllUnlocks()
    {
        for (int i = 0; i < 13; i++)
        {
            unlockedPlanets[i] = false;
            string key = PLAYER_PREFS_KEY_PREFIX + i;
            PlayerPrefs.DeleteKey(key);
        }
        PlayerPrefs.Save();
        
        // 同步到Inspector显示
        if (_instance != null && _instance.autoSyncToInspector)
        {
            _instance.SyncToInspector();
        }
        
        // 触发重置事件（延迟访问EventManager，避免在场景加载过程中过早创建EventManager实例）
        if (_instance != null)
        {
            _instance.StartCoroutine(_instance.TriggerResetEventDelayed());
        }
        else
        {
            // 如果没有实例，直接尝试触发事件（这种情况应该很少见）
            if (EventManager.Instance != null)
            {
                EventManager.Instance.TriggerAllPlanetsReset();
                Debug.Log("PlanetUnlockManager: 已重置所有行星解锁状态并触发事件");
            }
            else
            {
                Debug.LogWarning("PlanetUnlockManager: EventManager实例不存在，无法触发重置事件！");
                Debug.Log("PlanetUnlockManager: 已重置所有行星解锁状态（但未触发事件）");
            }
        }
    }
    
    /// <summary>
    /// 延迟触发重置事件（协程）
    /// 避免在场景加载过程中过早访问EventManager.Instance
    /// </summary>
    private System.Collections.IEnumerator TriggerResetEventDelayed()
    {
        // 等待一帧，确保场景加载完成
        yield return null;
        
        // 现在安全地访问EventManager
        if (EventManager.Instance != null)
        {
            EventManager.Instance.TriggerAllPlanetsReset();
            Debug.Log("PlanetUnlockManager: 已重置所有行星解锁状态并触发事件");
        }
        else
        {
            Debug.LogWarning("PlanetUnlockManager: EventManager实例不存在，无法触发重置事件！");
            Debug.Log("PlanetUnlockManager: 已重置所有行星解锁状态（但未触发事件）");
        }
    }
    
    /// <summary>
    /// 重置所有解锁状态（实例方法，用于ContextMenu）
    /// </summary>
    [ContextMenu("重置所有解锁状态")]
    private void ResetAllUnlocksInstance()
    {
        ResetAllUnlocks();
    }
    
    /// <summary>
    /// 获取所有解锁状态（用于调试）
    /// </summary>
    [ContextMenu("显示解锁状态")]
    public void ShowUnlockStates()
    {
        Debug.Log("=== 行星解锁状态 ===");
        for (int i = 0; i < 13; i++)
        {
            Debug.Log($"行星 {i}: {(unlockedPlanets[i] ? "已解锁" : "未解锁")}");
        }
        Debug.Log("==================");
    }
    
    /// <summary>
    /// 将静态数组同步到Inspector显示数组
    /// </summary>
    private void SyncToInspector()
    {
        if (planetUnlockStates == null || planetUnlockStates.Length != 13)
        {
            planetUnlockStates = new bool[13];
        }
        
        for (int i = 0; i < 13; i++)
        {
            planetUnlockStates[i] = unlockedPlanets[i];
        }
    }
    
    /// <summary>
    /// 从Inspector数组同步到静态数组（用于编辑器测试，仅在编辑器中有效）
    /// 注意：此方法会覆盖PlayerPrefs中的值，请谨慎使用
    /// </summary>
    [ContextMenu("从Inspector同步到运行时（覆盖PlayerPrefs）")]
    private void SyncFromInspector()
    {
        if (planetUnlockStates == null || planetUnlockStates.Length != 13)
        {
            Debug.LogWarning("PlanetUnlockManager: Inspector数组未正确初始化！");
            return;
        }
        
        Debug.LogWarning("PlanetUnlockManager: 正在从Inspector同步到运行时状态，这将覆盖PlayerPrefs中的值！");
        
        for (int i = 0; i < 13; i++)
        {
            unlockedPlanets[i] = planetUnlockStates[i];
            SaveUnlockState(i); // 保存到PlayerPrefs
        }
        
        // 触发事件，通知所有PlanetCard更新显示（延迟访问EventManager）
        if (_instance != null)
        {
            StartCoroutine(TriggerSyncFromInspectorEvents());
        }
        else
        {
            // 如果没有实例，直接尝试触发事件（这种情况应该很少见）
            if (EventManager.Instance != null)
            {
                EventManager.Instance.TriggerAllPlanetsReset();
                for (int i = 0; i < 13; i++)
                {
                    if (unlockedPlanets[i])
                    {
                        EventManager.Instance.TriggerPlanetUnlocked(i);
                    }
                }
            }
        }
        
        Debug.Log("PlanetUnlockManager: 已从Inspector同步到运行时状态并保存到PlayerPrefs");
    }
    
    /// <summary>
    /// 延迟触发从Inspector同步的事件（协程）
    /// </summary>
    private System.Collections.IEnumerator TriggerSyncFromInspectorEvents()
    {
        // 等待一帧，确保场景加载完成
        yield return null;
        
        // 现在安全地访问EventManager
        if (EventManager.Instance != null)
        {
            // 触发所有行星的重置事件，然后触发已解锁行星的解锁事件
            EventManager.Instance.TriggerAllPlanetsReset();
            for (int i = 0; i < 13; i++)
            {
                if (unlockedPlanets[i])
                {
                    EventManager.Instance.TriggerPlanetUnlocked(i);
                }
            }
        }
    }
    
    /// <summary>
    /// 强制刷新Inspector显示（用于调试）
    /// </summary>
    [ContextMenu("刷新Inspector显示")]
    private void RefreshInspectorDisplay()
    {
        SyncToInspector();
        Debug.Log("PlanetUnlockManager: 已刷新Inspector显示");
    }
}
