using UnityEngine;
using UnityEngine.UI;
using System;
using System.Collections.Generic;
using DG.Tweening;

/// <summary>
/// 订单图片列表控制器
/// 管理订单图片的水平滚动列表和切换逻辑
/// </summary>
public class OrderImageListController : MonoBehaviour
{
    [Header("UI引用")]
    [SerializeField] private ScrollRect scrollRect;
    [SerializeField] private RectTransform content;
    [SerializeField] private Button leftArrow;
    [SerializeField] private Button rightArrow;
    
    [Header("数据配置引用")]
    [Tooltip("订单数据配置引用（如果为空，将自动查找）")]
    [SerializeField] private SphereOrderDataConfig orderDataConfig;
    
    [Header("订单图片项")]
    [SerializeField] private List<Image> orderImageItems = new List<Image>(); // 订单图片列表（当前3个）
    
    [Header("布局参数")]
    [SerializeField] private float imageWidth = 400f;      // 订单图片宽度
    [SerializeField] private float imageHeight = 600f;    // 订单图片高度
    [SerializeField] private float imageSpacing = 20f;   // 图片间距
    
    [Header("占位图片（当前使用固定占位）")]
    [SerializeField] private Sprite placeholderImage1;
    [SerializeField] private Sprite placeholderImage2;
    [SerializeField] private Sprite placeholderImage3;
    
    [Header("滚动参数")]
    [SerializeField] private float scrollDuration = 0.3f; // 滚动动画时长
    [SerializeField] private float scrollStep = 0f;      // 滚动步长（0表示自动计算）
    
    [Header("自动查找设置")]
    [Tooltip("是否在Start时自动查找丢失的引用（推荐保持为true）")]
    [SerializeField] private bool autoFindOnStart = true;
    
    [Tooltip("是否从SphereOrderDataConfig中自动加载VisitOrder为真的订单图片")]
    [SerializeField] private bool autoLoadVisitedOrders = true;
    
    [Header("调试")]
    [SerializeField] private bool enableDebugLog = true;
    
    // 当前显示的订单索引
    private int currentOrderIndex = 0;
    
    // 事件：订单索引改变
    public event Action<int> OnOrderIndexChanged;
    
    void Start()
    {
        // 绑定箭头按钮
        if (leftArrow != null)
        {
            leftArrow.onClick.AddListener(OnLeftArrowClicked);
            if (enableDebugLog)
            {
                Debug.Log("OrderImageListController: 左箭头按钮已绑定");
            }
        }
        else
        {
            Debug.LogWarning("OrderImageListController: 左箭头按钮未配置！");
        }
        
        if (rightArrow != null)
        {
            rightArrow.onClick.AddListener(OnRightArrowClicked);
            if (enableDebugLog)
            {
                Debug.Log("OrderImageListController: 右箭头按钮已绑定");
            }
        }
        else
        {
            Debug.LogWarning("OrderImageListController: 右箭头按钮未配置！");
        }
        
        // 验证ScrollRect和Content引用
        if (scrollRect == null)
        {
            Debug.LogError("OrderImageListController: ScrollRect未配置！");
        }
        
        if (content == null)
        {
            Debug.LogError("OrderImageListController: Content未配置！");
        }
        
        // 查找订单数据配置（如果启用自动查找）
        if (autoFindOnStart)
        {
            FindOrderDataConfig();
        }
        
        // 初始化占位图片（仅在未启用自动加载或加载失败时使用）
        if (!autoLoadVisitedOrders)
        {
            InitializePlaceholderImages();
        }
        
        // 初始化滚动步长
        if (scrollStep <= 0f)
        {
            scrollStep = imageWidth + imageSpacing;
        }
        
        // 初始化当前索引为0
        currentOrderIndex = 0;
        
        // 调试：输出订单图片数量
        if (enableDebugLog)
        {
            Debug.Log($"OrderImageListController: 订单图片数量: {orderImageItems.Count}");
            for (int i = 0; i < orderImageItems.Count; i++)
            {
                if (orderImageItems[i] != null)
                {
                    Debug.Log($"  - 索引 {i}: {orderImageItems[i].name}");
                }
                else
                {
                    Debug.LogWarning($"  - 索引 {i}: null（未配置）");
                }
            }
        }
        
        // 如果orderImageItems列表为空或数量不足，尝试自动查找Content下的Image组件
        if (orderImageItems == null || orderImageItems.Count == 0)
        {
            Debug.LogWarning("OrderImageListController: orderImageItems列表为空，尝试自动查找Content下的Image组件");
            AutoFindImageItems();
        }
        
        // 从SphereOrderDataConfig中加载VisitOrder为真的订单图片
        if (autoLoadVisitedOrders)
        {
            LoadVisitedOrderImages();
        }
    }
    
    /// <summary>
    /// 查找订单数据配置（用于场景重新加载后重新查找）
    /// </summary>
    private void FindOrderDataConfig()
    {
        if (orderDataConfig == null)
        {
            // 方法1：尝试从Resources文件夹加载
            orderDataConfig = Resources.Load<SphereOrderDataConfig>("SphereOrderDataConfig");
            if (orderDataConfig != null && enableDebugLog)
            {
                Debug.Log("OrderImageListController: 成功从Resources加载SphereOrderDataConfig");
            }
            
            // 方法2：如果Resources加载失败，尝试查找场景中使用该配置的对象
            if (orderDataConfig == null)
            {
                TaskManager[] taskManagers = FindObjectsOfType<TaskManager>();
                foreach (TaskManager tm in taskManagers)
                {
                    // 尝试通过反射获取orderDataConfig字段
                    var field = typeof(TaskManager).GetField("orderDataConfig", 
                        System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                    if (field != null)
                    {
                        var config = field.GetValue(tm) as SphereOrderDataConfig;
                        if (config != null)
                        {
                            orderDataConfig = config;
                            if (enableDebugLog)
                            {
                                Debug.Log($"OrderImageListController: 从TaskManager获取到SphereOrderDataConfig引用");
                            }
                            break;
                        }
                    }
                }
            }
            
            // 如果仍然找不到，输出警告
            if (orderDataConfig == null)
            {
                Debug.LogWarning("OrderImageListController: 未找到SphereOrderDataConfig，请确保Resources文件夹中有SphereOrderDataConfig资源，或手动指定引用");
            }
        }
    }
    
    /// <summary>
    /// 从SphereOrderDataConfig中加载VisitOrder为真的订单图片
    /// </summary>
    private void LoadVisitedOrderImages()
    {
        // 查找订单数据配置
        if (orderDataConfig == null)
        {
            FindOrderDataConfig();
        }
        
        if (orderDataConfig == null || orderDataConfig.orderDataList == null)
        {
            if (enableDebugLog)
            {
                Debug.LogWarning("OrderImageListController: 无法加载订单图片，orderDataConfig未配置！");
            }
            return;
        }
        
        // 确保orderImageItems列表不为空
        if (orderImageItems == null || orderImageItems.Count == 0)
        {
            if (enableDebugLog)
            {
                Debug.LogWarning("OrderImageListController: orderImageItems列表为空，无法加载订单图片！");
            }
            return;
        }
        
        // 收集所有VisitOrder为真的订单
        List<Sprite> visitedOrderImages = new List<Sprite>();
        foreach (var orderInfo in orderDataConfig.orderDataList)
        {
            if (orderInfo != null && orderInfo.VisitOrder && orderInfo.orderImage != null)
            {
                visitedOrderImages.Add(orderInfo.orderImage);
            }
        }
        
        if (enableDebugLog)
        {
            Debug.Log($"OrderImageListController: 从配置中找到 {visitedOrderImages.Count} 个已访问的订单图片");
        }
        
        // 将订单图片设置到orderImageItems中
        int count = Mathf.Min(visitedOrderImages.Count, orderImageItems.Count);
        for (int i = 0; i < count; i++)
        {
            if (orderImageItems[i] != null && visitedOrderImages[i] != null)
            {
                orderImageItems[i].sprite = visitedOrderImages[i];
                
                if (enableDebugLog)
                {
                    Debug.Log($"OrderImageListController: 已设置订单图片到索引 {i}（{visitedOrderImages[i].name}）");
                }
            }
        }
        
        // 如果已访问的订单数量超过Image组件数量，输出警告
        if (visitedOrderImages.Count > orderImageItems.Count)
        {
            Debug.LogWarning($"OrderImageListController: 已访问的订单数量（{visitedOrderImages.Count}）超过Image组件数量（{orderImageItems.Count}），部分订单图片未显示");
        }
    }
    
    /// <summary>
    /// 自动查找Content下的Image组件
    /// </summary>
    private void AutoFindImageItems()
    {
        if (content == null)
        {
            Debug.LogError("OrderImageListController: Content未配置，无法自动查找Image组件！");
            return;
        }
        
        // 查找Content下的所有Image组件
        Image[] images = content.GetComponentsInChildren<Image>(true);
        
        if (images != null && images.Length > 0)
        {
            orderImageItems = new List<Image>(images);
            
            if (enableDebugLog)
            {
                Debug.Log($"OrderImageListController: 自动查找到 {orderImageItems.Count} 个Image组件");
                for (int i = 0; i < orderImageItems.Count; i++)
                {
                    Debug.Log($"  - 索引 {i}: {orderImageItems[i].name}");
                }
            }
        }
        else
        {
            Debug.LogWarning("OrderImageListController: 未在Content下找到Image组件！");
        }
    }
    
    /// <summary>
    /// 初始化占位图片
    /// </summary>
    private void InitializePlaceholderImages()
    {
        // 设置占位图片
        if (orderImageItems.Count >= 1 && placeholderImage1 != null)
        {
            orderImageItems[0].sprite = placeholderImage1;
        }
        
        if (orderImageItems.Count >= 2 && placeholderImage2 != null)
        {
            orderImageItems[1].sprite = placeholderImage2;
        }
        
        if (orderImageItems.Count >= 3 && placeholderImage3 != null)
        {
            orderImageItems[2].sprite = placeholderImage3;
        }
    }
    
    /// <summary>
    /// 设置订单图片列表（供后续代码调用）
    /// </summary>
    /// <param name="images">订单图片列表</param>
    public void SetOrderImages(List<Sprite> images)
    {
        if (images == null || images.Count == 0)
        {
            Debug.LogWarning("OrderImageListController: 图片列表为空！");
            return;
        }
        
        // 更新订单图片
        int count = Mathf.Min(images.Count, orderImageItems.Count);
        for (int i = 0; i < count; i++)
        {
            if (orderImageItems[i] != null && images[i] != null)
            {
                orderImageItems[i].sprite = images[i];
            }
        }
        
        if (enableDebugLog)
        {
            Debug.Log($"OrderImageListController: 已设置 {count} 张订单图片");
        }
    }
    
    /// <summary>
    /// 滚动到指定索引
    /// </summary>
    /// <param name="index">订单索引（0-based）</param>
    public void ScrollToIndex(int index)
    {
        if (scrollRect == null)
        {
            Debug.LogError("OrderImageListController: ScrollRect未配置，无法滚动！");
            return;
        }
        
        if (content == null)
        {
            Debug.LogError("OrderImageListController: Content未配置，无法滚动！");
            return;
        }
        
        if (index < 0 || index >= orderImageItems.Count)
        {
            Debug.LogWarning($"OrderImageListController: 索引 {index} 超出范围（0-{orderImageItems.Count - 1}）！");
            return;
        }
        
        currentOrderIndex = index;
        
        // 强制更新布局（确保rect值正确）
        Canvas.ForceUpdateCanvases();
        
        // 计算目标位置
        float targetPosition = index * scrollStep;
        float contentWidth = content.rect.width;
        float viewportWidth = scrollRect.viewport.rect.width;
        float scrollableWidth = contentWidth - viewportWidth;
        
        if (enableDebugLog)
        {
            Debug.Log($"OrderImageListController: 滚动到索引 {index}, Content宽度: {contentWidth}, Viewport宽度: {viewportWidth}, 可滚动宽度: {scrollableWidth}");
        }
        
        if (scrollableWidth <= 0)
        {
            Debug.LogWarning("OrderImageListController: 可滚动宽度 <= 0，无法滚动！可能是Content宽度不够或Viewport太大。");
            return;
        }
        
        // 转换为归一化位置
        float normalizedPosition = targetPosition / scrollableWidth;
        normalizedPosition = Mathf.Clamp01(normalizedPosition);
        
        if (enableDebugLog)
        {
            Debug.Log($"OrderImageListController: 目标归一化位置: {normalizedPosition}, 当前归一化位置: {scrollRect.horizontalNormalizedPosition}");
        }
        
        // 执行滚动动画
        DOTween.To(() => scrollRect.horizontalNormalizedPosition,
                   x => scrollRect.horizontalNormalizedPosition = x,
                   normalizedPosition, scrollDuration)
               .SetEase(Ease.OutQuad);
        
        // 触发事件
        OnOrderIndexChanged?.Invoke(currentOrderIndex);
    }
    
    /// <summary>
    /// 获取当前订单索引
    /// </summary>
    public int GetCurrentIndex()
    {
        return currentOrderIndex;
    }
    
    /// <summary>
    /// 左箭头点击事件
    /// </summary>
    private void OnLeftArrowClicked()
    {
        if (enableDebugLog)
        {
            Debug.Log($"OrderImageListController: 左箭头被点击，当前索引: {currentOrderIndex}, 总数量: {orderImageItems.Count}");
        }
        
        // 检查orderImageItems列表是否有效
        if (orderImageItems == null || orderImageItems.Count == 0)
        {
            Debug.LogWarning("OrderImageListController: orderImageItems列表为空，尝试自动查找");
            AutoFindImageItems();
        }
        
        // 再次检查
        if (orderImageItems == null || orderImageItems.Count == 0)
        {
            Debug.LogError("OrderImageListController: orderImageItems列表仍然为空，无法滚动！");
            return;
        }
        
        if (currentOrderIndex > 0)
        {
            ScrollToIndex(currentOrderIndex - 1);
        }
        else
        {
            if (enableDebugLog)
            {
                Debug.Log($"OrderImageListController: 已经是第一个订单（索引 {currentOrderIndex}），无法向左滚动");
            }
            // 如果已经是第一个，可以循环到最后一个（可选）
            // ScrollToIndex(orderImageItems.Count - 1);
        }
    }
    
    /// <summary>
    /// 右箭头点击事件
    /// </summary>
    private void OnRightArrowClicked()
    {
        if (enableDebugLog)
        {
            Debug.Log($"OrderImageListController: 右箭头被点击，当前索引: {currentOrderIndex}, 总数量: {orderImageItems.Count}");
        }
        
        // 检查orderImageItems列表是否有效
        if (orderImageItems == null || orderImageItems.Count == 0)
        {
            Debug.LogWarning("OrderImageListController: orderImageItems列表为空，尝试自动查找");
            AutoFindImageItems();
        }
        
        // 再次检查
        if (orderImageItems == null || orderImageItems.Count == 0)
        {
            Debug.LogError("OrderImageListController: orderImageItems列表仍然为空，无法滚动！");
            return;
        }
        
        if (currentOrderIndex < orderImageItems.Count - 1)
        {
            ScrollToIndex(currentOrderIndex + 1);
        }
        else
        {
            if (enableDebugLog)
            {
                Debug.Log($"OrderImageListController: 已经是最后一个订单（索引 {currentOrderIndex}/{orderImageItems.Count - 1}），无法向右滚动");
            }
            // 如果已经是最后一个，可以循环到第一个（可选）
            // ScrollToIndex(0);
        }
    }
    
    void OnEnable()
    {
        // 重新查找引用（场景重新加载后引用可能丢失）
        if (autoFindOnStart)
        {
            // 重新查找订单数据配置和Image组件
            FindOrderDataConfig();
            
            if (orderImageItems == null || orderImageItems.Count == 0)
            {
                AutoFindImageItems();
            }
        }
        
        // 从SphereOrderDataConfig中重新加载VisitOrder为真的订单图片
        if (autoLoadVisitedOrders)
        {
            LoadVisitedOrderImages();
        }
    }
    
    void Update()
    {
        // 处理鼠标滚轮（可选）
        if (scrollRect == null) return;
        
        float scroll = Input.GetAxis("Mouse ScrollWheel");
        if (Mathf.Abs(scroll) > 0.01f)
        {
            float scrollStep = (imageWidth + imageSpacing) * scroll * 2f;
            float targetPosition = scrollRect.horizontalNormalizedPosition + (scrollStep / content.rect.width);
            targetPosition = Mathf.Clamp01(targetPosition);
            scrollRect.horizontalNormalizedPosition = targetPosition;
        }
    }
}
