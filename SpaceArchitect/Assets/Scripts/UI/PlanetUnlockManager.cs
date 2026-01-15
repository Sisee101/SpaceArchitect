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
    
    /// <summary>
    /// 获取PlanetUnlockManager单例
    /// </summary>
    public static PlanetUnlockManager Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindObjectOfType<PlanetUnlockManager>();
                
                if (_instance == null)
                {
                    GameObject go = new GameObject("PlanetUnlockManager");
                    _instance = go.AddComponent<PlanetUnlockManager>();
                    DontDestroyOnLoad(go);
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
            // 加载已保存的解锁状态
            LoadUnlockStates();
        }
        else if (_instance != this)
        {
            Debug.LogWarning("检测到多个PlanetUnlockManager实例，销毁重复的实例");
            Destroy(gameObject);
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
        
        // 触发事件
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
    [ContextMenu("重置所有解锁状态")]
    public static void ResetAllUnlocks()
    {
        for (int i = 0; i < 13; i++)
        {
            unlockedPlanets[i] = false;
            string key = PLAYER_PREFS_KEY_PREFIX + i;
            PlayerPrefs.DeleteKey(key);
        }
        PlayerPrefs.Save();
        Debug.Log("PlanetUnlockManager: 已重置所有行星解锁状态");
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
}
