using UnityEngine;
using DG.Tweening;
using UnityEngine.UI;

/// <summary>
/// 订单组合控制器
/// 管理一个订单的按钮和面板组合（一起移动）
/// </summary>
public class OrderSet : MonoBehaviour
{
    [Header("订单按钮（右上角）")]
    [SerializeField] private GameObject orderButton;  // 订单按钮GameObject
    
    [Header("订单面板")]
    [SerializeField] private OrderPanel orderPanel;   // 订单面板
    
    [Header("其他组件")]
    [SerializeField] private CanvasGroup canvasGroup; // CanvasGroup组件（用于淡入淡出）
    
    /// <summary>
    /// 获取订单面板（供外部访问）
    /// </summary>
    public OrderPanel Panel
    {
        get { return orderPanel; }
    }
    
    /// <summary>
    /// 获取订单按钮（供外部访问）
    /// </summary>
    public GameObject Button
    {
        get { return orderButton; }
    }
    
    /// <summary>
    /// 获取CanvasGroup组件
    /// </summary>
    public CanvasGroup CanvasGroupComponent
    {
        get
        {
            if (canvasGroup == null)
            {
                canvasGroup = GetComponent<CanvasGroup>();
                if (canvasGroup == null)
                {
                    canvasGroup = gameObject.AddComponent<CanvasGroup>();
                }
            }
            return canvasGroup;
        }
    }
    
    void Awake()
    {
        // 确保CanvasGroup存在
        if (canvasGroup == null)
        {
            canvasGroup = GetComponent<CanvasGroup>();
            if (canvasGroup == null)
            {
                canvasGroup = gameObject.AddComponent<CanvasGroup>();
            }
        }
        
        // 确保初始透明度正确
        if (canvasGroup != null)
        {
            canvasGroup.alpha = gameObject.activeSelf ? 1f : 0f;
        }
    }
    
    /// <summary>
    /// 设置订单图片
    /// </summary>
    public void SetOrderImage(Sprite image)
    {
        if (orderPanel != null)
        {
            orderPanel.SetOrderImage(image);
        }
    }
    
    /// <summary>
    /// 显示盖章图层
    /// </summary>
    public void ShowStamp()
    {
        if (orderPanel != null)
        {
            orderPanel.ShowStamp();
        }
    }
    
    /// <summary>
    /// 隐藏盖章图层
    /// </summary>
    public void HideStamp()
    {
        if (orderPanel != null)
        {
            orderPanel.HideStamp();
        }
    }
    
    /// <summary>
    /// 显示订单组合（按钮和面板都显示）
    /// </summary>
    public void Show()
    {
        gameObject.SetActive(true);
        
        // 显示按钮（如果面板未展开，按钮显示；如果面板已展开，按钮可以隐藏或保持显示）
        if (orderButton != null)
        {
            orderButton.SetActive(true);
        }
        
        // 订单面板的显示由用户点击按钮控制，这里不自动显示
    }
    
    /// <summary>
    /// 隐藏订单组合（按钮和面板都隐藏）
    /// </summary>
    public void Hide()
    {
        // 先隐藏面板
        if (orderPanel != null)
        {
            orderPanel.Hide();
        }
        
        // 隐藏按钮
        if (orderButton != null)
        {
            orderButton.SetActive(false);
        }
    }
    
    /// <summary>
    /// 执行移出动画（向右滑出屏幕）
    /// </summary>
    /// <param name="offsetX">X偏移量（目标位置）</param>
    /// <param name="duration">动画时长</param>
    /// <returns>动画Sequence</returns>
    public Sequence AnimateMoveOut(float offsetX, float duration)
    {
        Sequence moveOutSequence = DOTween.Sequence();
        
        // 移动整个组合
        moveOutSequence.Join(transform.DOLocalMoveX(offsetX, duration).SetEase(Ease.InCubic));
        
        // 淡出效果
        if (CanvasGroupComponent != null)
        {
            moveOutSequence.Join(CanvasGroupComponent.DOFade(0, duration));
        }
        
        return moveOutSequence;
    }
    
    /// <summary>
    /// 执行移入动画（从左侧滑入屏幕）
    /// </summary>
    /// <param name="targetX">目标X位置</param>
    /// <param name="duration">动画时长</param>
    /// <param name="delay">延迟时间</param>
    /// <returns>动画Sequence</returns>
    public Sequence AnimateMoveIn(float targetX, float duration, float delay = 0f)
    {
        Sequence moveInSequence = DOTween.Sequence();
        
        // 移动整个组合
        moveInSequence.Join(transform.DOLocalMoveX(targetX, duration).SetEase(Ease.OutCubic).SetDelay(delay));
        
        // 淡入效果
        if (CanvasGroupComponent != null)
        {
            moveInSequence.Join(CanvasGroupComponent.DOFade(1, duration).SetDelay(delay));
        }
        
        return moveInSequence;
    }
    
    /// <summary>
    /// 重置位置（用于准备下次切换）
    /// </summary>
    /// <param name="x">X坐标</param>
    public void ResetPosition(float x)
    {
        Vector3 pos = transform.localPosition;
        transform.localPosition = new Vector3(x, pos.y, pos.z);
        
        if (CanvasGroupComponent != null)
        {
            CanvasGroupComponent.alpha = 0f;
        }
    }
    
    /// <summary>
    /// 获取当前X位置（用于计算目标位置）
    /// </summary>
    public float GetCurrentX()
    {
        return transform.localPosition.x;
    }
}

