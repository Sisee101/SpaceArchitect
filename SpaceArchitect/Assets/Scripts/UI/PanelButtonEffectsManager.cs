using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections.Generic;
using DG.Tweening;

/// <summary>
/// 面板按钮效果管理器
/// 低耦合脚本，可挂载在任何面板上，自动为所有按钮添加高亮色块跟随效果和音效
/// </summary>
public class PanelButtonEffectsManager : MonoBehaviour
{
    [Header("高亮色块")]
    [Tooltip("高亮色块的RectTransform（显示在按钮下方的sprite）")]
    [SerializeField] private RectTransform highlightBlock;
    
    [Header("按钮查找设置")]
    [Tooltip("是否自动查找子对象中的所有Button组件")]
    [SerializeField] private bool autoFindButtons = true;
    
    [Tooltip("手动指定的按钮列表（如果autoFindButtons为false，使用此列表）")]
    [SerializeField] private List<Button> manualButtons = new List<Button>();
    
    [Header("高亮移动动画参数")]
    [Tooltip("高亮块移动动画时长（秒）")]
    [SerializeField] private float moveDuration = 0.3f;
    
    [Tooltip("高亮块移动动画缓动类型")]
    [SerializeField] private Ease moveEase = Ease.OutQuad;
    
    [Tooltip("高亮块相对按钮的偏移量（如果为(0,0)则自动计算位置使高亮块显示在按钮正下方）")]
    [SerializeField] private Vector2 highlightOffset = Vector2.zero;
    
    [Tooltip("高亮块与按钮之间的间距（像素，仅在自动计算偏移时使用）")]
    [SerializeField] private float highlightSpacing = 2f;
    
    [Header("音效设置")]
    [Tooltip("是否启用音效")]
    [SerializeField] private bool enableSound = true;
    
    [Tooltip("音频源组件（如果为空，会自动获取或创建）")]
    [SerializeField] private AudioSource audioSource;
    
    [Tooltip("按钮悬停音效")]
    [SerializeField] private AudioClip buttonHoverSound;
    
    [Tooltip("按钮点击音效")]
    [SerializeField] private AudioClip buttonClickSound;
    
    [Header("调试选项")]
    [Tooltip("是否显示调试日志")]
    [SerializeField] private bool showDebugLogs = false;
    
    // 存储所有按钮的引用
    private List<Button> allButtons = new List<Button>();
    
    void Awake()
    {
        // 初始化音频源
        InitializeAudioSource();
    }
    
    void Start()
    {
        // 初始化高亮位置
        InitializeHighlightPosition();
        
        // 初始化按钮效果
        InitializeButtons();
    }
    
    /// <summary>
    /// 初始化音频源
    /// </summary>
    private void InitializeAudioSource()
    {
        if (audioSource == null)
        {
            audioSource = GetComponent<AudioSource>();
            if (audioSource == null)
            {
                audioSource = gameObject.AddComponent<AudioSource>();
            }
        }
    }
    
    /// <summary>
    /// 初始化高亮位置（对齐到第一个按钮）
    /// </summary>
    private void InitializeHighlightPosition()
    {
        if (highlightBlock == null)
        {
            if (showDebugLogs)
            {
                Debug.LogWarning("PanelButtonEffectsManager: 高亮色块未配置，将无法显示高亮效果");
            }
            return;
        }
        
        // 等待一帧，确保Layout已更新
        StartCoroutine(InitializeHighlightPositionDelayed());
    }
    
    /// <summary>
    /// 延迟初始化高亮位置的协程
    /// </summary>
    private System.Collections.IEnumerator InitializeHighlightPositionDelayed()
    {
        yield return null;
        Canvas.ForceUpdateCanvases();
        yield return null;
        
        // 获取按钮列表
        List<Button> buttons = GetButtonList();
        
        if (buttons != null && buttons.Count > 0 && buttons[0] != null)
        {
            RectTransform firstButton = buttons[0].GetComponent<RectTransform>();
            if (firstButton != null)
            {
                // 自动计算偏移
                Vector2 calculatedOffset = CalculateAutoOffset(firstButton);
                
                // 设置高亮块的位置
                highlightBlock.anchoredPosition = new Vector2(
                    firstButton.anchoredPosition.x + calculatedOffset.x,
                    firstButton.anchoredPosition.y + calculatedOffset.y
                );
                
                if (showDebugLogs)
                {
                    Debug.Log($"PanelButtonEffectsManager: 高亮块已初始化到第一个按钮位置");
                }
            }
        }
    }
    
    /// <summary>
    /// 初始化所有按钮
    /// </summary>
    private void InitializeButtons()
    {
        allButtons = GetButtonList();
        
        if (showDebugLogs)
        {
            Debug.Log($"PanelButtonEffectsManager: 找到 {allButtons.Count} 个按钮");
        }
        
        // 为每个按钮设置效果
        foreach (Button button in allButtons)
        {
            if (button != null)
            {
                SetupButtonEffects(button);
            }
        }
        
        if (showDebugLogs)
        {
            Debug.Log($"PanelButtonEffectsManager: 已为 {allButtons.Count} 个按钮设置效果");
        }
    }
    
    /// <summary>
    /// 获取按钮列表
    /// </summary>
    private List<Button> GetButtonList()
    {
        List<Button> buttons = new List<Button>();
        
        if (autoFindButtons)
        {
            // 自动查找所有子对象中的Button组件（包括未激活的）
            buttons.AddRange(GetComponentsInChildren<Button>(true));
        }
        else
        {
            // 使用手动指定的按钮列表
            buttons = manualButtons;
        }
        
        return buttons;
    }
    
    /// <summary>
    /// 为单个按钮设置效果
    /// </summary>
    private void SetupButtonEffects(Button button)
    {
        if (button == null) return;
        
        // 设置悬浮高亮效果（高亮块跟随）
        if (highlightBlock != null)
        {
            SetupButtonHoverHighlight(button);
        }
        
        // 设置点击音效
        if (enableSound && buttonClickSound != null)
        {
            SetupButtonClickSound(button);
        }
    }
    
    /// <summary>
    /// 为按钮设置悬浮高亮效果（高亮块跟随）
    /// </summary>
    private void SetupButtonHoverHighlight(Button button)
    {
        if (button == null) return;
        
        // 获取或添加 EventTrigger 组件
        EventTrigger eventTrigger = button.gameObject.GetComponent<EventTrigger>();
        if (eventTrigger == null)
        {
            eventTrigger = button.gameObject.AddComponent<EventTrigger>();
        }
        
        // 检查是否已经存在 PointerEnter 事件（避免重复添加）
        bool hasPointerEnter = false;
        
        foreach (EventTrigger.Entry entry in eventTrigger.triggers)
        {
            if (entry.eventID == EventTriggerType.PointerEnter)
            {
                hasPointerEnter = true;
                break;
            }
        }
        
        // 添加 PointerEnter 事件（鼠标进入）
        if (!hasPointerEnter)
        {
            EventTrigger.Entry entryEnter = new EventTrigger.Entry();
            entryEnter.eventID = EventTriggerType.PointerEnter;
            entryEnter.callback.AddListener((eventData) => OnButtonPointerEnter(button));
            eventTrigger.triggers.Add(entryEnter);
        }
        
        if (showDebugLogs)
        {
            Debug.Log($"PanelButtonEffectsManager: 已为按钮 {button.name} 设置悬浮高亮效果");
        }
    }
    
    /// <summary>
    /// 为按钮设置点击音效
    /// </summary>
    private void SetupButtonClickSound(Button button)
    {
        if (button == null) return;
        
        // 添加点击音效监听器（不干扰原有的点击逻辑）
        button.onClick.AddListener(() => PlayButtonClickSound());
        
        if (showDebugLogs)
        {
            Debug.Log($"PanelButtonEffectsManager: 已为按钮 {button.name} 设置点击音效");
        }
    }
    
    /// <summary>
    /// 按钮鼠标进入事件（高亮块跟随 + 悬停音效）
    /// </summary>
    private void OnButtonPointerEnter(Button button)
    {
        if (button == null) return;
        
        // 播放悬停音效
        if (enableSound && buttonHoverSound != null)
        {
            PlayButtonHoverSound();
        }
        
        // 移动高亮块到按钮位置
        if (highlightBlock != null)
        {
            RectTransform buttonRect = button.GetComponent<RectTransform>();
            if (buttonRect != null)
            {
                MoveHighlightToButton(buttonRect);
            }
        }
        
        if (showDebugLogs)
        {
            Debug.Log($"PanelButtonEffectsManager: 按钮 {button.name} 鼠标进入，高亮块跟随");
        }
    }
    
    /// <summary>
    /// 移动高亮块到指定按钮位置（参考MenuHighlightController的实现）
    /// </summary>
    private void MoveHighlightToButton(RectTransform targetButton)
    {
        if (highlightBlock == null || targetButton == null)
        {
            return;
        }
        
        // 停止之前的动画
        highlightBlock.DOKill();
        
        // 自动计算偏移
        Vector2 calculatedOffset = CalculateAutoOffset(targetButton);
        
        // 获取目标位置
        float targetX = targetButton.anchoredPosition.x + calculatedOffset.x;
        float targetY = targetButton.anchoredPosition.y + calculatedOffset.y;
        
        // 创建动画序列
        Sequence moveSequence = DOTween.Sequence();
        
        // 移动Y位置
        moveSequence.Append(highlightBlock.DOAnchorPosY(targetY, moveDuration).SetEase(moveEase));
        
        // 同时移动X位置（如果需要）
        if (Mathf.Abs(highlightBlock.anchoredPosition.x - targetX) > 0.1f)
        {
            moveSequence.Join(highlightBlock.DOAnchorPosX(targetX, moveDuration).SetEase(moveEase));
        }
        
        if (showDebugLogs)
        {
            Debug.Log($"PanelButtonEffectsManager: 高亮块移动到按钮 {targetButton.name}，目标位置: ({targetX:F2}, {targetY:F2})");
        }
    }
    
    /// <summary>
    /// 自动计算高亮块的偏移量，使高亮块显示在按钮正下方
    /// 参考MenuHighlightController的实现
    /// </summary>
    private Vector2 CalculateAutoOffset(RectTransform targetButton)
    {
        // 如果手动设置了偏移（highlightOffset不为零），使用手动偏移
        if (highlightOffset != Vector2.zero)
        {
            return highlightOffset;
        }
        
        // 自动计算偏移
        // X方向：对齐到按钮中心（不需要偏移）
        float offsetX = 0f;
        
        // Y方向：计算使高亮块显示在按钮正下方
        // 确保Layout已经更新
        Canvas.ForceUpdateCanvases();
        
        float buttonHeight = targetButton.rect.height;
        float highlightHeight = highlightBlock.rect.height;
        float spacing = highlightSpacing;
        
        // 计算Y偏移（负数表示向下）
        // 高亮块中心Y = 按钮中心Y - 按钮高度/2 - 高亮块高度/2 - 间距
        float offsetY = -(buttonHeight / 2f + highlightHeight / 2f + spacing);
        
        if (showDebugLogs)
        {
            Debug.Log($"[PanelButtonEffectsManager] CalculateAutoOffset: buttonHeight={buttonHeight:F2}, highlightHeight={highlightHeight:F2}, spacing={spacing:F2}, offsetY={offsetY:F2}");
        }
        
        return new Vector2(offsetX, offsetY);
    }
    
    /// <summary>
    /// 播放按钮悬停音效
    /// </summary>
    private void PlayButtonHoverSound()
    {
        if (audioSource != null && buttonHoverSound != null)
        {
            audioSource.PlayOneShot(buttonHoverSound);
        }
    }
    
    /// <summary>
    /// 播放按钮点击音效
    /// </summary>
    private void PlayButtonClickSound()
    {
        if (audioSource != null && buttonClickSound != null)
        {
            audioSource.PlayOneShot(buttonClickSound);
        }
    }
    
    void OnDestroy()
    {
        // 清理动画，防止内存泄漏
        if (highlightBlock != null)
        {
            highlightBlock.DOKill();
        }
    }
}
