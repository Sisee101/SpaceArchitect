using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
using System;

/// <summary>
/// 订单项控制器
/// 管理单个订单项的展开/收起、数据显示和动画
/// </summary>
public class OrderListItem : MonoBehaviour
{
    [Header("UI引用")]
    [SerializeField] private Button headerButton;      // 头部按钮（整个头部可点击）
    [SerializeField] private GameObject orderContent;   // 订单内容区域
    [SerializeField] private Text orderNumberText;      // 订单编号文本
    [SerializeField] private Image expandIcon;          // 展开/收起图标
    [SerializeField] private Image orderImage;          // 订单图片
    [SerializeField] private Button startDeliveryButton; // 开始配送按钮
    
    [Header("动画参数")]
    [SerializeField] private float expandDuration = 0.3f; // 展开/收起动画时长
    
    private bool isExpanded = false;
    private OrderData orderData;
    private RectTransform contentRectTransform;
    private LayoutElement contentLayoutElement;
    private float expandedHeight = 400f; // 展开后的高度（根据实际情况调整）
    
    /// <summary>
    /// 订单完成事件（当空格键完成订单时触发）
    /// </summary>
    public event Action<OrderListItem> OnOrderCompleted;
    
    void Awake()
    {
        // 获取或添加 LayoutElement 组件（用于控制高度）
        if (orderContent != null)
        {
            contentRectTransform = orderContent.GetComponent<RectTransform>();
            contentLayoutElement = orderContent.GetComponent<LayoutElement>();
            if (contentLayoutElement == null)
            {
                contentLayoutElement = orderContent.AddComponent<LayoutElement>();
            }
        }
    }
    
    void Start()
    {
        // 绑定头部按钮事件（点击展开/收起）
        if (headerButton != null)
        {
            headerButton.onClick.RemoveAllListeners();
            headerButton.onClick.AddListener(ToggleExpand);
        }
        
        // 绑定开始配送按钮事件
        if (startDeliveryButton != null)
        {
            startDeliveryButton.onClick.RemoveAllListeners();
            startDeliveryButton.onClick.AddListener(OnStartDeliveryClicked);
        }
        
        // 初始状态：收起
        if (orderContent != null)
        {
            orderContent.SetActive(false);
            if (contentLayoutElement != null)
            {
                contentLayoutElement.preferredHeight = 0f;
            }
        }
        
        // 初始图标状态（收起状态，箭头向下）
        if (expandIcon != null)
        {
            expandIcon.transform.localRotation = Quaternion.Euler(0, 0, -90f); // 箭头向下
        }
    }
    
    /// <summary>
    /// 初始化订单数据
    /// </summary>
    public void Initialize(OrderData data)
    {
        orderData = data;
        
        // 显示订单编号
        if (orderNumberText != null && data != null)
        {
            orderNumberText.text = $"订单{data.orderId}";
        }
        
        // 显示订单图片
        if (orderImage != null && data != null && data.orderImage != null)
        {
            orderImage.sprite = data.orderImage;
        }
    }
    
    /// <summary>
    /// 切换展开/收起状态
    /// </summary>
    public void ToggleExpand()
    {
        if (isExpanded)
        {
            Collapse();
        }
        else
        {
            Expand();
        }
    }
    
    /// <summary>
    /// 展开订单内容
    /// </summary>
    public void Expand()
    {
        if (isExpanded) return;
        
        isExpanded = true;
        
        // 激活内容区域
        if (orderContent != null)
        {
            orderContent.SetActive(true);
            
            // 使用 DOTween 动画展开高度
            if (contentLayoutElement != null)
            {
                DOTween.To(
                    () => contentLayoutElement.preferredHeight,
                    x => contentLayoutElement.preferredHeight = x,
                    expandedHeight,
                    expandDuration
                ).SetEase(Ease.OutCubic)
                .OnUpdate(() => {
                    // 更新布局（确保列表正确调整）
                    LayoutRebuilder.ForceRebuildLayoutImmediate(transform.parent as RectTransform);
                });
            }
        }
        
        // 旋转图标（箭头向上）
        if (expandIcon != null)
        {
            expandIcon.transform.DORotate(new Vector3(0, 0, 90f), expandDuration).SetEase(Ease.OutCubic);
        }
        
        // 通知列表面板（确保同时只有一个订单项展开）
        OrderListPanel panel = GetComponentInParent<OrderListPanel>();
        if (panel != null)
        {
            panel.OnOrderItemExpanded(this);
        }
    }
    
    /// <summary>
    /// 收起订单内容
    /// </summary>
    public void Collapse()
    {
        if (!isExpanded) return;
        
        isExpanded = false;
        
        // 使用 DOTween 动画收起高度
        if (contentLayoutElement != null)
        {
            DOTween.To(
                () => contentLayoutElement.preferredHeight,
                x => contentLayoutElement.preferredHeight = x,
                0f,
                expandDuration
            ).SetEase(Ease.InCubic)
            .OnUpdate(() => {
                // 更新布局（确保列表正确调整）
                LayoutRebuilder.ForceRebuildLayoutImmediate(transform.parent as RectTransform);
            })
            .OnComplete(() => {
                // 动画完成后隐藏内容区域
                if (orderContent != null)
                {
                    orderContent.SetActive(false);
                }
            });
        }
        
        // 旋转图标（箭头向下）
        if (expandIcon != null)
        {
            expandIcon.transform.DORotate(new Vector3(0, 0, -90f), expandDuration).SetEase(Ease.InCubic);
        }
        
        // 通知列表面板（更新当前展开项）
        OrderListPanel panel = GetComponentInParent<OrderListPanel>();
        if (panel != null)
        {
            // 如果当前收起的是当前展开项，清空引用
            // 这个逻辑由 OrderListPanel 自己处理，这里不需要额外操作
        }
    }
    
    /// <summary>
    /// 检查是否已展开
    /// </summary>
    public bool IsExpanded()
    {
        return isExpanded;
    }
    
    /// <summary>
    /// 完成订单并执行滑出动画（空格键触发）
    /// 注意：盖章显示已在外部（OrderListPanel）处理，这里只执行滑出动画
    /// </summary>
    public void CompleteOrderWithAnimation(Action onComplete = null)
    {
        // 向右滑出屏幕
        RectTransform rectTransform = GetComponent<RectTransform>();
        if (rectTransform != null)
        {
            float screenRight = Screen.width + 200f;
            
            // 脱离滚动容器，避免被裁剪
            Transform originalParent = transform.parent;
            Canvas canvas = GetComponentInParent<Canvas>();
            if (canvas != null)
            {
                transform.SetParent(canvas.transform, true);
            }
            
            // 获取或添加 CanvasGroup（用于淡出效果）
            CanvasGroup canvasGroup = GetComponent<CanvasGroup>();
            if (canvasGroup == null)
            {
                canvasGroup = gameObject.AddComponent<CanvasGroup>();
            }
            
            // 执行滑出动画
            Sequence completeSequence = DOTween.Sequence();
            completeSequence.Append(rectTransform.DOLocalMoveX(screenRight, 0.6f).SetEase(Ease.InCubic));
            completeSequence.Join(canvasGroup.DOFade(0, 0.6f));
            
            completeSequence.OnComplete(() => {
                // 动画完成后的回调
                OnOrderCompleted?.Invoke(this);
                onComplete?.Invoke();
            });
        }
    }
    
    /// <summary>
    /// 显示盖章图层（供外部调用）
    /// </summary>
    public void ShowStamp()
    {
        // TODO: 如果有盖章图层，在这里显示
        // 目前可以在 OrderImage 上叠加一个盖章图片
        // 如果有 stampOverlay，可以在这里激活它
        Debug.Log($"OrderListItem: 显示订单 {orderData?.orderId} 的盖章");
    }
    
    /// <summary>
    /// 开始配送按钮点击事件
    /// </summary>
    private void OnStartDeliveryClicked()
    {
        Debug.Log($"开始配送订单: {orderData?.orderId}");
        
        // 保存面板状态（如果需要在返回后保持展开）
        // ...
        
        // 恢复时间
        Time.timeScale = 1f;
        
        // 跳转到游戏场景
        if (SceneTransitionManager.Instance != null)
        {
            SceneTransitionManager.Instance.LoadGameScene();
        }
        else
        {
            Debug.LogError("OrderListItem: SceneTransitionManager未找到！");
        }
    }
    
    /// <summary>
    /// 获取订单数据
    /// </summary>
    public OrderData GetOrderData()
    {
        return orderData;
    }
    
    void OnDestroy()
    {
        // 清理 DOTween 动画
        DOTween.Kill(transform);
        if (contentLayoutElement != null)
        {
            DOTween.Kill(contentLayoutElement);
        }
        if (expandIcon != null)
        {
            DOTween.Kill(expandIcon.transform);
        }
    }
}

