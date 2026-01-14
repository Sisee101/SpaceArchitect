using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

/// <summary>
/// 邮件按钮项组件
/// 单个邮件按钮的UI组件，处理按钮点击和选中状态
/// buttonIcon直接显示，无Button交互效果，尺寸以buttonIcon为准
/// </summary>
public class MailButtonItem : MonoBehaviour, IPointerClickHandler
{
    [Header("UI引用")]
    [Tooltip("显示buttonIcon的Image组件。如果不手动指定，会自动获取或添加Image组件")]
    [SerializeField] private Image iconImage;  // 显示buttonIcon的Image组件
    [SerializeField] private Image highlightImage;           // 选中高亮效果（可选）
    
    [Header("状态颜色")]
    [Tooltip("选中时的颜色（如果highlightImage为空，则改变iconImage的颜色）")]
    [SerializeField] private Color selectedColor = Color.white;
    [Tooltip("未选中时的颜色")]
    [SerializeField] private Color normalColor = new Color(0.8f, 0.8f, 0.8f, 1f);
    
    private int mailId;
    private Sprite contentImage;
    private MailPanel mailPanel;
    private bool isSelected = false;
    
    /// <summary>
    /// 初始化按钮
    /// </summary>
    /// <param name="id">邮件ID</param>
    /// <param name="icon">按钮图标</param>
    /// <param name="content">右侧显示的内容图片</param>
    /// <param name="panel">邮箱面板引用</param>
    public void Initialize(int id, Sprite icon, Sprite content, MailPanel panel)
    {
        mailId = id;
        contentImage = content;
        mailPanel = panel;
        
        // 获取或添加Image组件
        if (iconImage == null)
        {
            iconImage = GetComponent<Image>();
            if (iconImage == null)
            {
                iconImage = gameObject.AddComponent<Image>();
            }
        }
        
        // 设置图标图片
        if (iconImage != null && icon != null)
        {
            iconImage.sprite = icon;
            iconImage.type = Image.Type.Simple;
            iconImage.preserveAspect = true; // 保持图片比例
            
            // 根据buttonIcon的原始尺寸设置RectTransform
            RectTransform rectTransform = GetComponent<RectTransform>();
            if (rectTransform != null)
            {
                rectTransform.sizeDelta = new Vector2(icon.texture.width, icon.texture.height);
            }
        }
        
        // 初始状态为未选中
        SetSelected(false);
        
        // 确保可以接收点击事件（需要GraphicRaycaster）
        if (iconImage != null)
        {
            iconImage.raycastTarget = true;
        }
    }
    
    /// <summary>
    /// 实现IPointerClickHandler接口，处理点击事件
    /// </summary>
    public void OnPointerClick(PointerEventData eventData)
    {
        if (mailPanel != null && contentImage != null)
        {
            // 播放点击音效
            mailPanel.PlayMailButtonClickSound();
            
            mailPanel.OnMailButtonClicked(mailId, contentImage);
        }
    }
    
    /// <summary>
    /// 设置选中状态
    /// </summary>
    /// <param name="selected">是否选中</param>
    public void SetSelected(bool selected)
    {
        isSelected = selected;
        
        // 更新高亮效果
        if (highlightImage != null)
        {
            highlightImage.gameObject.SetActive(selected);
        }
        else if (iconImage != null)
        {
            // 如果没有高亮图片，则改变图标颜色
            iconImage.color = selected ? selectedColor : normalColor;
        }
    }
    
    /// <summary>
    /// 获取邮件ID
    /// </summary>
    public int GetMailId()
    {
        return mailId;
    }
}
