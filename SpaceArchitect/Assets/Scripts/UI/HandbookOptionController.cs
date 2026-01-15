using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections.Generic;
using DG.Tweening;

/// <summary>
/// 员工手册选项控制器
/// 管理三个选项卡片的显示和点击事件
/// </summary>
public class HandbookOptionController : MonoBehaviour
{
    [Header("选项按钮引用")]
    [Tooltip("三个选项按钮（按顺序：入职指南、业务流程、系统架构）")]
    [SerializeField] private List<Button> optionButtons = new List<Button>();
    
    [Header("选项图标（可选）")]
    [Tooltip("选项图标（与按钮顺序对应）")]
    [SerializeField] private List<Image> optionIcons = new List<Image>();
    
    [Header("选项标题（可选）")]
    [Tooltip("选项标题文本（与按钮顺序对应）")]
    [SerializeField] private List<Text> optionTitles = new List<Text>();
    
    [Header("悬浮动画效果")]
    [Tooltip("是否启用按钮悬浮放大效果")]
    [SerializeField] private bool enableHoverScale = true;
    
    [Tooltip("悬浮时的缩放倍数（1.0为原始大小，1.1表示放大10%）")]
    [SerializeField] private float hoverScaleAmount = 1.1f;
    
    [Tooltip("缩放动画时长（秒）")]
    [SerializeField] private float hoverAnimationDuration = 0.2f;
    
    [Tooltip("缩放动画缓动类型")]
    [SerializeField] private Ease hoverEaseType = Ease.OutBack;
    
    [Header("调试")]
    [SerializeField] private bool enableDebugLog = false;
    
    // 选项点击事件（参数：选项索引）
    public System.Action<int> OnOptionClicked;
    
    private bool isInitialized = false;
    
    void Start()
    {
        if (!isInitialized)
        {
            InitializeOptions();
            isInitialized = true;
        }
    }
    
    /// <summary>
    /// 初始化选项按钮
    /// </summary>
    private void InitializeOptions()
    {
        // 绑定按钮点击事件和悬浮效果
        for (int i = 0; i < optionButtons.Count; i++)
        {
            int index = i; // 闭包变量
            if (optionButtons[i] != null)
            {
                optionButtons[i].onClick.RemoveAllListeners();
                optionButtons[i].onClick.AddListener(() => OnOptionButtonClicked(index));
                
                // 设置悬浮放大效果
                if (enableHoverScale)
                {
                    SetupButtonHoverScale(optionButtons[i]);
                }
            }
        }
        
        if (enableDebugLog)
        {
            Debug.Log($"HandbookOptionController: 已初始化 {optionButtons.Count} 个选项按钮");
            if (enableHoverScale)
            {
                Debug.Log($"HandbookOptionController: 悬浮放大效果已启用，缩放倍数={hoverScaleAmount}, 动画时长={hoverAnimationDuration}秒");
            }
        }
    }
    
    /// <summary>
    /// 选项按钮点击事件
    /// </summary>
    private void OnOptionButtonClicked(int index)
    {
        if (enableDebugLog)
        {
            Debug.Log($"HandbookOptionController: 选项 {index} 被点击");
        }
        
        // 触发选项点击事件
        OnOptionClicked?.Invoke(index);
    }
    
    /// <summary>
    /// 显示选项列表
    /// </summary>
    public void Show()
    {
        gameObject.SetActive(true);
    }
    
    /// <summary>
    /// 隐藏选项列表
    /// </summary>
    public void Hide()
    {
        gameObject.SetActive(false);
    }
    
    /// <summary>
    /// 为按钮设置悬浮放大效果
    /// </summary>
    /// <param name="button">目标按钮</param>
    private void SetupButtonHoverScale(Button button)
    {
        if (button == null) return;
        
        // 获取或添加 EventTrigger 组件
        EventTrigger eventTrigger = button.gameObject.GetComponent<EventTrigger>();
        if (eventTrigger == null)
        {
            eventTrigger = button.gameObject.AddComponent<EventTrigger>();
        }
        
        // 保存按钮的原始缩放值
        Vector3 originalScale = button.transform.localScale;
        
        // 检查是否已经存在 PointerEnter 事件（避免重复添加）
        bool hasPointerEnter = false;
        bool hasPointerExit = false;
        
        foreach (EventTrigger.Entry entry in eventTrigger.triggers)
        {
            if (entry.eventID == EventTriggerType.PointerEnter)
            {
                hasPointerEnter = true;
            }
            if (entry.eventID == EventTriggerType.PointerExit)
            {
                hasPointerExit = true;
            }
        }
        
        // 如果不存在 PointerEnter 事件，则添加
        if (!hasPointerEnter)
        {
            EventTrigger.Entry entryEnter = new EventTrigger.Entry();
            entryEnter.eventID = EventTriggerType.PointerEnter;
            entryEnter.callback.AddListener((eventData) => OnButtonPointerEnter(button.transform, originalScale));
            eventTrigger.triggers.Add(entryEnter);
        }
        
        // 如果不存在 PointerExit 事件，则添加
        if (!hasPointerExit)
        {
            EventTrigger.Entry entryExit = new EventTrigger.Entry();
            entryExit.eventID = EventTriggerType.PointerExit;
            entryExit.callback.AddListener((eventData) => OnButtonPointerExit(button.transform, originalScale));
            eventTrigger.triggers.Add(entryExit);
        }
        
        if (enableDebugLog)
        {
            Debug.Log($"HandbookOptionController: 已为按钮 {button.name} 设置悬浮放大效果");
        }
    }
    
    /// <summary>
    /// 按钮鼠标进入事件（放大）
    /// </summary>
    /// <param name="buttonTransform">按钮的Transform</param>
    /// <param name="originalScale">原始缩放值</param>
    private void OnButtonPointerEnter(Transform buttonTransform, Vector3 originalScale)
    {
        if (buttonTransform == null) return;
        
        // 停止当前动画（如果有）
        buttonTransform.DOKill();
        
        // 计算目标缩放值
        Vector3 targetScale = originalScale * hoverScaleAmount;
        
        // 执行放大动画
        buttonTransform.DOScale(targetScale, hoverAnimationDuration)
            .SetEase(hoverEaseType);
        
        if (enableDebugLog)
        {
            Debug.Log($"HandbookOptionController: 按钮 {buttonTransform.name} 鼠标进入，开始放大动画");
        }
    }
    
    /// <summary>
    /// 按钮鼠标离开事件（恢复）
    /// </summary>
    /// <param name="buttonTransform">按钮的Transform</param>
    /// <param name="originalScale">原始缩放值</param>
    private void OnButtonPointerExit(Transform buttonTransform, Vector3 originalScale)
    {
        if (buttonTransform == null) return;
        
        // 停止当前动画（如果有）
        buttonTransform.DOKill();
        
        // 执行恢复动画
        buttonTransform.DOScale(originalScale, hoverAnimationDuration)
            .SetEase(hoverEaseType);
        
        if (enableDebugLog)
        {
            Debug.Log($"HandbookOptionController: 按钮 {buttonTransform.name} 鼠标离开，开始恢复动画");
        }
    }
    
    void OnDestroy()
    {
        // 清理所有按钮的DOTween动画，防止内存泄漏
        if (optionButtons != null)
        {
            foreach (Button button in optionButtons)
            {
                if (button != null && button.transform != null)
                {
                    button.transform.DOKill();
                }
            }
        }
    }
}
