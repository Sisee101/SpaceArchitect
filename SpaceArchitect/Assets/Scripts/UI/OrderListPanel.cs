using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// 订单列表面板控制器
/// 管理订单列表的显示、滚动和交互
/// </summary>
public class OrderListPanel : MonoBehaviour
{
    [Header("UI引用")]
    [SerializeField] private ScrollRect scrollRect;           // 滚动容器
    [SerializeField] private RectTransform contentTransform;  // 内容区域
    [Header("订单项模板（可选，用于动态生成）")]
    [SerializeField] private GameObject orderItemPrefab;      // 订单项预制体（如果需要动态生成）
    
    [Header("动画参数")]
    [SerializeField] private float stampDisplayDuration = 1.5f; // 盖章显示时长
    [SerializeField] private float expandDuration = 0.3f; // 展开动画时长（与 OrderListItem 保持一致）
    
    private List<OrderListItem> orderItems = new List<OrderListItem>();
    private OrderListItem currentExpandedItem = null; // 当前展开的订单项
    private OrderListItem currentSelectedItem = null; // 当前选中的订单项（用于空格键）
    
    void Start()
    {
        // 初始化订单列表
        InitializeOrderList();
        
        // 确保滚动容器已配置
        if (scrollRect == null)
        {
            scrollRect = GetComponentInChildren<ScrollRect>();
        }
        
        if (contentTransform == null && scrollRect != null)
        {
            contentTransform = scrollRect.content;
        }
        
        // 配置滚动：允许鼠标滚轮和拖拽
        if (scrollRect != null)
        {
            scrollRect.vertical = true;  // 允许垂直滚动
            scrollRect.horizontal = false; // 不允许水平滚动
        }
    }
    
    void Update()
    {
        // 检测空格键按下，完成当前选中的订单
        if (Input.GetKeyDown(KeyCode.Space))
        {
            CompleteCurrentOrder();
        }
    }
    
    /// <summary>
    /// 初始化订单列表
    /// </summary>
    private void InitializeOrderList()
    {
        if (OrderManager.Instance == null)
        {
            Debug.LogError("OrderListPanel: OrderManager未找到！");
            return;
        }
        
        // 获取所有订单数据
        List<OrderData> orders = GetOrdersFromManager();
        
        // 查找场景中已存在的订单项（手动创建的情况）
        OrderListItem[] existingItems = GetComponentsInChildren<OrderListItem>();
        
        if (existingItems.Length > 0)
        {
            // 使用场景中已存在的订单项
            orderItems.Clear();
            orderItems.AddRange(existingItems);
            
            // 初始化每个订单项
            for (int i = 0; i < orderItems.Count && i < orders.Count; i++)
            {
                if (orderItems[i] != null)
                {
                    orderItems[i].Initialize(orders[i]);
                    // 订阅展开事件，确保同时只有一个订单项展开
                    orderItems[i].OnOrderCompleted += OnOrderItemCompleted;
                }
            }
        }
        else if (orderItemPrefab != null)
        {
            // 动态生成订单项
            foreach (var orderData in orders)
            {
                CreateOrderItem(orderData);
            }
        }
        else
        {
            Debug.LogWarning("OrderListPanel: 未找到订单项模板，也无法找到场景中已存在的订单项！");
        }
        
        // 默认选中第一个订单项
        if (orderItems.Count > 0)
        {
            currentSelectedItem = orderItems[0];
        }
    }
    
    /// <summary>
    /// 从 OrderManager 获取订单列表
    /// </summary>
    private List<OrderData> GetOrdersFromManager()
    {
        if (OrderManager.Instance == null)
        {
            Debug.LogWarning("OrderListPanel: OrderManager未找到，返回空列表");
            return new List<OrderData>();
        }
        
        // 直接获取所有订单
        return OrderManager.Instance.GetAllOrders();
    }
    
    /// <summary>
    /// 创建订单项（动态生成）
    /// </summary>
    private void CreateOrderItem(OrderData orderData)
    {
        if (orderItemPrefab == null || contentTransform == null) return;
        
        GameObject itemObj = Instantiate(orderItemPrefab, contentTransform);
        OrderListItem item = itemObj.GetComponent<OrderListItem>();
        
        if (item != null)
        {
            item.Initialize(orderData);
            item.OnOrderCompleted += OnOrderItemCompleted;
            orderItems.Add(item);
        }
    }
    
    /// <summary>
    /// 处理订单项展开（确保同时只有一个展开）
    /// </summary>
    public void OnOrderItemExpanded(OrderListItem expandedItem)
    {
        // 如果其他订单项是展开的，收起它们
        foreach (var item in orderItems)
        {
            if (item != expandedItem && item.IsExpanded())
            {
                item.Collapse();
            }
        }
        
        currentExpandedItem = expandedItem;
    }
    
    /// <summary>
    /// 完成当前选中的订单（空格键触发）
    /// </summary>
    private void CompleteCurrentOrder()
    {
        // 优先使用当前展开的订单项，否则使用当前选中的订单项
        OrderListItem targetItem = currentExpandedItem != null ? currentExpandedItem : currentSelectedItem;
        
        if (targetItem == null)
        {
            Debug.LogWarning("OrderListPanel: 没有选中的订单项，无法完成订单！");
            return;
        }
        
        // 如果订单项未展开，先展开它
        if (!targetItem.IsExpanded())
        {
            targetItem.Expand();
            // 等待展开动画完成后再执行完成动画
            // expandDuration 应该与 OrderListItem 的 expandDuration 一致（默认 0.3 秒）
            StartCoroutine(CompleteOrderAfterDelay(targetItem, expandDuration + 0.1f)); // 稍微多等一点，确保展开完成
            return;
        }
        
        // 执行完成动画
        ExecuteCompleteAnimation(targetItem);
    }
    
    /// <summary>
    /// 延迟执行完成动画（等待展开动画完成）
    /// </summary>
    private System.Collections.IEnumerator CompleteOrderAfterDelay(OrderListItem item, float delay)
    {
        yield return new WaitForSeconds(delay);
        ExecuteCompleteAnimation(item);
    }
    
    /// <summary>
    /// 执行完成动画
    /// </summary>
    private void ExecuteCompleteAnimation(OrderListItem item)
    {
        // 显示盖章（如果有）
        if (item != null)
        {
            item.ShowStamp(); // 在 OrderListItem 中实现
        }
        
        // 等待盖章显示时间后再执行滑出动画
        StartCoroutine(CompleteOrderWithDelay(item, stampDisplayDuration));
    }
    
    /// <summary>
    /// 延迟完成订单（等待盖章显示）
    /// </summary>
    private System.Collections.IEnumerator CompleteOrderWithDelay(OrderListItem item, float delay)
    {
        yield return new WaitForSeconds(delay);
        
        // 执行滑出动画
        item.CompleteOrderWithAnimation(() => {
            // 动画完成后的回调
            Debug.Log($"订单 {item.GetOrderData()?.orderId} 已完成并滑出");
        });
    }
    
    /// <summary>
    /// 订单项完成事件处理
    /// </summary>
    private void OnOrderItemCompleted(OrderListItem completedItem)
    {
        // 从列表中移除
        orderItems.Remove(completedItem);
        
        // 更新当前选中项
        if (currentSelectedItem == completedItem)
        {
            currentSelectedItem = orderItems.Count > 0 ? orderItems[0] : null;
        }
        
        if (currentExpandedItem == completedItem)
        {
            currentExpandedItem = null;
        }
        
        // 销毁游戏对象（或回收到对象池）
        Destroy(completedItem.gameObject);
        
        // 重建布局（确保列表正确调整）
        LayoutRebuilder.ForceRebuildLayoutImmediate(contentTransform);
    }
    
    /// <summary>
    /// 显示订单列表面板
    /// </summary>
    public void Show()
    {
        gameObject.SetActive(true);
    }
    
    /// <summary>
    /// 隐藏订单列表面板
    /// </summary>
    public void Hide()
    {
        gameObject.SetActive(false);
    }
    
    void OnDestroy()
    {
        // 清理事件订阅
        foreach (var item in orderItems)
        {
            if (item != null)
            {
                item.OnOrderCompleted -= OnOrderItemCompleted;
            }
        }
    }
}

