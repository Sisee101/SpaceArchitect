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
    [Tooltip("如果设置为(0,0)，将自动计算位置使高亮块显示在按钮正下方。如果需要自定义位置，可以手动设置偏移量。")]
    [SerializeField] private Vector2 highlightOffset = Vector2.zero; // 高亮块相对按钮的偏移量（如果为(0,0)则自动计算）
    
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
            
            // 自动计算偏移
            Vector2 calculatedOffset = CalculateAutoOffset(targetButton);
            
            // 设置高亮块的位置（X轴和Y轴都对齐到按钮）
            highlightBlock.anchoredPosition = new Vector2(
                targetButton.anchoredPosition.x + calculatedOffset.x,
                targetButton.anchoredPosition.y + calculatedOffset.y
            );
            
            // 保留高亮块自己的大小（不强制设置宽度）
            // highlightBlock.sizeDelta 保持不变
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
    /// 自动计算位置，使高亮块显示在按钮正下方
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
        
        // 自动计算位置（使高亮块显示在按钮正下方，X轴和Y轴都对齐）
        Vector2 calculatedOffset = CalculateAutoOffset(targetButton);
        
        // 直接设置位置（保留高亮块自己的大小，不强制设置宽度）
        highlightBlock.anchoredPosition = new Vector2(
            targetButton.anchoredPosition.x + calculatedOffset.x,
            targetButton.anchoredPosition.y + calculatedOffset.y
        );
        
        // 不再强制设置宽度，保留用户在Inspector中设置的高亮块大小
        // highlightBlock.sizeDelta 保持不变
        
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
    /// 自动计算高亮块的偏移量，使高亮块显示在按钮正下方
    /// X轴和Y轴都对齐到按钮中心
    /// </summary>
    /// <param name="targetButton">目标按钮的 RectTransform</param>
    /// <returns>计算得到的偏移量</returns>
    private Vector2 CalculateAutoOffset(RectTransform targetButton)
    {
        // 如果手动设置了偏移（highlightOffset不为零），使用手动偏移
        if (highlightOffset != Vector2.zero)
        {
            return highlightOffset;
        }
        
        // 自动计算偏移
        // X方向：对齐到按钮中心（不需要偏移，因为使用相同的anchoredPosition.x）
        float offsetX = 0f;
        
        // Y方向：计算使高亮块显示在按钮正下方
        // 使用rect.height获取实际渲染高度（比sizeDelta.y更准确，特别是使用Layout Group时）
        // 确保Layout已经更新
        Canvas.ForceUpdateCanvases();
        
        float buttonHeight = targetButton.rect.height;
        float highlightHeight = highlightBlock.rect.height;
        float spacing = 2f; // 高亮块与按钮之间的间距（像素）
        
        // 计算Y偏移（负数表示向下）
        // 按钮底部位置 = 按钮中心Y - 按钮高度/2
        // 高亮块应该放在按钮底部下方，所以：
        // 高亮块中心Y = 按钮中心Y - 按钮高度/2 - 高亮块高度/2 - 间距
        float offsetY = -(buttonHeight / 2f + highlightHeight / 2f + spacing);
        
        // 调试信息（启用后可以在运行时查看Console中的计算结果）
        Debug.Log($"[MenuHighlightController] CalculateAutoOffset: buttonHeight={buttonHeight:F2}, highlightHeight={highlightHeight:F2}, spacing={spacing:F2}, offsetY={offsetY:F2}");
        
        return new Vector2(offsetX, offsetY);
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