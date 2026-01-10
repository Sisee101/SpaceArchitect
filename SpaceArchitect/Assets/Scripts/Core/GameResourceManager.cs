using UnityEngine;
using System;

/// <summary>
/// 游戏资源管理器（占位版本）
/// 负责管理金币、燃料等游戏资源
/// TODO: 等待完整实现
/// </summary>
public class GameResourceManager : MonoBehaviour
{
    private static GameResourceManager _instance;
    
    /// <summary>
    /// 获取GameResourceManager单例
    /// </summary>
    public static GameResourceManager Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindObjectOfType<GameResourceManager>();
                
                if (_instance == null)
                {
                    GameObject go = new GameObject("GameResourceManager");
                    _instance = go.AddComponent<GameResourceManager>();
                    DontDestroyOnLoad(go);
                }
            }
            return _instance;
        }
    }
    
    // 占位属性（默认值）
    [Header("资源数值（占位）")]
    [SerializeField] private int gold = 1000;
    [SerializeField] private float fuel = 100f;
    
    /// <summary>
    /// 当前金币数量
    /// </summary>
    public int Gold 
    { 
        get => gold;
        private set
        {
            gold = value;
            OnGoldChanged?.Invoke(gold);
        }
    }
    
    /// <summary>
    /// 当前燃料数量
    /// </summary>
    public float Fuel 
    { 
        get => fuel;
        private set
        {
            fuel = value;
            OnFuelChanged?.Invoke(fuel);
        }
    }
    
    // 资源变化事件
    public event Action<int> OnGoldChanged;
    public event Action<float> OnFuelChanged;
    
    void Awake()
    {
        // 确保单例
        if (_instance == null)
        {
            _instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else if (_instance != this)
        {
            Debug.LogWarning("检测到多个GameResourceManager实例，销毁重复的实例");
            Destroy(gameObject);
            return;
        }
    }
    
    void Start()
    {
        // 初始化时触发一次事件，让UI更新显示
        OnGoldChanged?.Invoke(gold);
        OnFuelChanged?.Invoke(fuel);
    }
    
    /// <summary>
    /// 增加金币（占位方法）
    /// TODO: 等待完整实现
    /// </summary>
    public void AddGold(int amount)
    {
        if (amount > 0)
        {
            Gold += amount;
            Debug.Log($"增加金币: {amount}, 当前金币: {Gold}");
        }
    }
    
    /// <summary>
    /// 减少金币（占位方法）
    /// TODO: 等待完整实现
    /// </summary>
    public bool SpendGold(int amount)
    {
        if (amount > 0 && Gold >= amount)
        {
            Gold -= amount;
            Debug.Log($"消耗金币: {amount}, 剩余金币: {Gold}");
            return true;
        }
        Debug.LogWarning($"金币不足，无法消费: {amount}, 当前金币: {Gold}");
        return false;
    }
    
    /// <summary>
    /// 设置金币（占位方法）
    /// TODO: 等待完整实现
    /// </summary>
    public void SetGold(int amount)
    {
        Gold = Mathf.Max(0, amount);
    }
    
    /// <summary>
    /// 增加燃料（占位方法）
    /// TODO: 等待完整实现
    /// </summary>
    public void AddFuel(float amount)
    {
        if (amount > 0)
        {
            Fuel += amount;
            Debug.Log($"增加燃料: {amount:F2}, 当前燃料: {Fuel:F2}");
        }
    }
    
    /// <summary>
    /// 消耗燃料（占位方法）
    /// TODO: 等待完整实现
    /// </summary>
    public bool ConsumeFuel(float amount)
    {
        if (amount > 0 && Fuel >= amount)
        {
            Fuel -= amount;
            Debug.Log($"消耗燃料: {amount:F2}, 剩余燃料: {Fuel:F2}");
            return true;
        }
        Debug.LogWarning($"燃料不足，无法消耗: {amount:F2}, 当前燃料: {Fuel:F2}");
        return false;
    }
    
    /// <summary>
    /// 设置燃料（占位方法）
    /// TODO: 等待完整实现
    /// </summary>
    public void SetFuel(float amount)
    {
        Fuel = Mathf.Max(0f, amount);
    }
}

