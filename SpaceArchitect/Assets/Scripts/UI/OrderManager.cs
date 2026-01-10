using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// 订单管理器（单例）
/// 管理所有订单的列表和当前订单状态
/// </summary>
public class OrderManager : MonoBehaviour
{
    private static OrderManager _instance;
    
    /// <summary>
    /// 获取OrderManager单例
    /// </summary>
    public static OrderManager Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindObjectOfType<OrderManager>();
                
                if (_instance == null)
                {
                    GameObject go = new GameObject("OrderManager");
                    _instance = go.AddComponent<OrderManager>();
                    DontDestroyOnLoad(go);
                }
            }
            return _instance;
        }
    }
    
    [Header("订单图片列表（在Inspector中配置）")]
    [SerializeField] private List<Sprite> orderImages = new List<Sprite>();
    
    private List<OrderData> orders = new List<OrderData>();
    private int currentOrderIndex = 0;
    
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
            Debug.LogWarning("检测到多个OrderManager实例，销毁重复的实例");
            Destroy(gameObject);
            return;
        }
        
        // 初始化订单列表
        InitializeOrders();
    }
    
    /// <summary>
    /// 初始化订单列表
    /// </summary>
    private void InitializeOrders()
    {
        orders.Clear();
        
        if (orderImages != null && orderImages.Count > 0)
        {
            for (int i = 0; i < orderImages.Count; i++)
            {
                if (orderImages[i] != null)
                {
                    orders.Add(new OrderData(i + 1, orderImages[i]));
                }
            }
        }
        
        currentOrderIndex = 0;
        
        Debug.Log($"OrderManager: 初始化了 {orders.Count} 个订单");
    }
    
    /// <summary>
    /// 获取当前待配送订单
    /// </summary>
    public OrderData GetCurrentOrder()
    {
        if (orders.Count == 0)
        {
            Debug.LogWarning("OrderManager: 没有订单数据");
            return null;
        }
        
        if (currentOrderIndex >= 0 && currentOrderIndex < orders.Count)
        {
            return orders[currentOrderIndex];
        }
        
        return null;
    }
    
    /// <summary>
    /// 获取下一个待配送订单
    /// </summary>
    public OrderData GetNextPendingOrder()
    {
        // 如果当前订单已完成，移动到下一个
        if (currentOrderIndex < orders.Count && orders[currentOrderIndex].isCompleted)
        {
            currentOrderIndex++;
        }
        
        // 检查是否还有订单
        if (currentOrderIndex >= orders.Count)
        {
            Debug.Log("OrderManager: 所有订单已完成");
            return null;
        }
        
        return orders[currentOrderIndex];
    }
    
    /// <summary>
    /// 标记当前订单为已完成
    /// </summary>
    public void MarkCurrentOrderAsCompleted()
    {
        if (currentOrderIndex >= 0 && currentOrderIndex < orders.Count)
        {
            orders[currentOrderIndex].isCompleted = true;
            Debug.Log($"OrderManager: 订单 {orders[currentOrderIndex].orderId} 已标记为完成");
            
            // 移动到下一个订单
            currentOrderIndex++;
        }
    }
    
    /// <summary>
    /// 检查是否还有未完成的订单
    /// </summary>
    public bool HasMoreOrders()
    {
        // 检查是否还有未完成的订单
        return currentOrderIndex < orders.Count;
    }
    
    /// <summary>
    /// 重置到第一个订单（如果需要）
    /// </summary>
    public void ResetToFirstOrder()
    {
        currentOrderIndex = 0;
        foreach (var order in orders)
        {
            order.isCompleted = false;
        }
        Debug.Log("OrderManager: 已重置到第一个订单");
    }
    
    /// <summary>
    /// 获取订单总数
    /// </summary>
    public int GetOrderCount()
    {
        return orders.Count;
    }
}

