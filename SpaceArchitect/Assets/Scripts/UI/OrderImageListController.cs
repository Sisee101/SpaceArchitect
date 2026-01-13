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
        
        // 初始化占位图片
        InitializePlaceholderImages();
        
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
