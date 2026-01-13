using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

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
        // 绑定按钮点击事件
        for (int i = 0; i < optionButtons.Count; i++)
        {
            int index = i; // 闭包变量
            if (optionButtons[i] != null)
            {
                optionButtons[i].onClick.RemoveAllListeners();
                optionButtons[i].onClick.AddListener(() => OnOptionButtonClicked(index));
            }
        }
        
        if (enableDebugLog)
        {
            Debug.Log($"HandbookOptionController: 已初始化 {optionButtons.Count} 个选项按钮");
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
}
