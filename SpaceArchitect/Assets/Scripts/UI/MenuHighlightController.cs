using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

/// <summary>
/// 菜单高亮跟随控制器
/// 控制高亮色块跟随选中的菜单项移动
/// </summary>
public class MenuHighlightController : MonoBehaviour
{
    [Header("高亮色块")]
    [SerializeField] private RectTransform highlightBlock; // 高亮色块的 RectTransform
    
    [Header("菜单按钮列表")]
    [SerializeField] private RectTransform[] menuButtons; // 所有菜单按钮的 RectTransform 数组
    
    [Header("动画参数")]
    [SerializeField] private float moveDuration = 0.3f; // 移动动画时长
    [SerializeField] private Ease moveEase = Ease.OutQuad; // 缓动曲线
    
    [Header("高亮偏移（可选）")]
    [SerializeField] private Vector2 highlightOffset = Vector2.zero; // 高亮块相对按钮的偏移量
    
    private int currentSelectedIndex = 0; // 当前选中的菜单项索引
    
    void Start()
    {
        // 初始化高亮位置（对齐到第一个菜单项）
        if (highlightBlock != null && menuButtons != null && menuButtons.Length > 0)
        {
            InitializeHighlightPosition();
        }
        else
        {
            Debug.LogWarning("MenuHighlightController: 高亮色块或菜单按钮未配置！");
        }
    }
    
    /// <summary>
    /// 初始化高亮位置（对齐到第一个菜单项）
    /// </summary>
    private void InitializeHighlightPosition()
    {
        if (menuButtons.Length > 0 && menuButtons[0] != null)
        {
            RectTransform targetButton = menuButtons[0];
            
            // 设置高亮块的宽度和X位置与目标按钮一致
            highlightBlock.anchoredPosition = new Vector2(
                targetButton.anchoredPosition.x + highlightOffset.x,
                targetButton.anchoredPosition.y + highlightOffset.y
            );
            
            // 设置高亮块的宽度与按钮一致
            highlightBlock.sizeDelta = new Vector2(
                targetButton.sizeDelta.x,
                highlightBlock.sizeDelta.y
            );
        }
    }
    
    /// <summary>
    /// 移动到指定菜单项
    /// </summary>
    /// <param name="buttonIndex">菜单项索引（从0开始）</param>
    public void MoveToButton(int buttonIndex)
    {
        if (highlightBlock == null || menuButtons == null)
        {
            Debug.LogWarning("MenuHighlightController: 高亮色块或菜单按钮未配置！");
            return;
        }
        
        if (buttonIndex < 0 || buttonIndex >= menuButtons.Length)
        {
            Debug.LogWarning($"MenuHighlightController: 菜单项索引 {buttonIndex} 超出范围！");
            return;
        }
        
        if (menuButtons[buttonIndex] == null)
        {
            Debug.LogWarning($"MenuHighlightController: 菜单项 {buttonIndex} 为空！");
            return;
        }
        
        // 如果点击的是当前已选中的项，不执行移动
        if (buttonIndex == currentSelectedIndex)
        {
            return;
        }
        
        currentSelectedIndex = buttonIndex;
        RectTransform targetButton = menuButtons[buttonIndex];
        
        // 停止之前的动画
        highlightBlock.DOKill();
        
        // 获取目标位置
        float targetY = targetButton.anchoredPosition.y + highlightOffset.y;
        float targetX = targetButton.anchoredPosition.x + highlightOffset.x;
        float targetWidth = targetButton.sizeDelta.x;
        
        // 创建动画序列：同时移动位置和调整宽度
        Sequence moveSequence = DOTween.Sequence();
        
        // 移动Y位置
        moveSequence.Append(highlightBlock.DOAnchorPosY(targetY, moveDuration).SetEase(moveEase));
        
        // 同时移动X位置（如果需要）
        if (Mathf.Abs(highlightBlock.anchoredPosition.x - targetX) > 0.1f)
        {
            moveSequence.Join(highlightBlock.DOAnchorPosX(targetX, moveDuration).SetEase(moveEase));
        }
        
        // 同时调整宽度（如果需要）
        if (Mathf.Abs(highlightBlock.sizeDelta.x - targetWidth) > 0.1f)
        {
            moveSequence.Join(highlightBlock.DOSizeDelta(
                new Vector2(targetWidth, highlightBlock.sizeDelta.y),
                moveDuration
            ).SetEase(moveEase));
        }
    }
    
    /// <summary>
    /// 移动到指定菜单项（通过按钮引用）
    /// 直接定位到按钮位置，无动画
    /// </summary>
    /// <param name="targetButton">目标按钮的 RectTransform</param>
    public void MoveToButton(RectTransform targetButton)
    {
        if (targetButton == null)
        {
            Debug.LogWarning("MenuHighlightController: 目标按钮为空！");
            return;
        }
        
        if (highlightBlock == null)
        {
            Debug.LogWarning("MenuHighlightController: 高亮色块未配置！");
            return;
        }
        
        // 停止所有动画
        highlightBlock.DOKill();
        
        // 直接设置位置和宽度，无动画
        highlightBlock.anchoredPosition = new Vector2(
            targetButton.anchoredPosition.x + highlightOffset.x,
            targetButton.anchoredPosition.y + highlightOffset.y
        );
        
        highlightBlock.sizeDelta = new Vector2(
            targetButton.sizeDelta.x,
            highlightBlock.sizeDelta.y
        );
        
        // 更新当前选中索引（如果按钮在数组中）
        for (int i = 0; i < menuButtons.Length; i++)
        {
            if (menuButtons[i] == targetButton)
            {
                currentSelectedIndex = i;
                return;
            }
        }
    }
    
    /// <summary>
    /// 立即移动到指定菜单项（无动画）
    /// </summary>
    /// <param name="buttonIndex">菜单项索引</param>
    public void MoveToButtonInstant(int buttonIndex)
    {
        if (highlightBlock == null || menuButtons == null || 
            buttonIndex < 0 || buttonIndex >= menuButtons.Length || 
            menuButtons[buttonIndex] == null)
        {
            return;
        }
        
        currentSelectedIndex = buttonIndex;
        RectTransform targetButton = menuButtons[buttonIndex];
        
        // 停止所有动画
        highlightBlock.DOKill();
        
        // 立即设置位置和宽度
        highlightBlock.anchoredPosition = new Vector2(
            targetButton.anchoredPosition.x + highlightOffset.x,
            targetButton.anchoredPosition.y + highlightOffset.y
        );
        
        highlightBlock.sizeDelta = new Vector2(
            targetButton.sizeDelta.x,
            highlightBlock.sizeDelta.y
        );
    }
    
    void OnDestroy()
    {
        // 清理动画
        if (highlightBlock != null)
        {
            highlightBlock.DOKill();
        }
    }
}