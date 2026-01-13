using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

/// <summary>
/// 员工手册面板控制器
/// 处理员工手册的显示和交互，管理多层级视图切换
/// </summary>
public class EmployeeHandbookPanel : MonoBehaviour
{
    /// <summary>
    /// 视图状态枚举
    /// </summary>
    private enum ViewState
    {
        OptionList,  // 选项列表
        ImageView    // 图片浏览
    }
    
    [Header("视图容器")]
    [Tooltip("选项列表视图容器")]
    [SerializeField] private GameObject optionListView;
    
    [Tooltip("图片浏览视图容器")]
    [SerializeField] private GameObject imageView;
    
    [Header("控制器引用")]
    [Tooltip("选项控制器")]
    [SerializeField] private HandbookOptionController optionController;
    
    [Tooltip("图片浏览控制器")]
    [SerializeField] private HandbookImageViewer imageViewer;
    
    [Header("返回按钮")]
    [Tooltip("选项列表视图的返回按钮（用于关闭整个弹窗）")]
    [SerializeField] private Button backButton;
    
    [Header("数据配置")]
    [Tooltip("员工手册数据配置（ScriptableObject）")]
    [SerializeField] private EmployeeHandbookDataConfig dataConfig;
    
    [Header("调试")]
    [SerializeField] private bool enableDebugLog = false;
    
    // 当前视图状态
    private ViewState currentState = ViewState.OptionList;
    
    private bool isInitialized = false;
    
    void Start()
    {
        if (!isInitialized)
        {
            InitializePanel();
            isInitialized = true;
        }
        
        // 如果面板在场景中默认是激活的，在这里隐藏
        if (gameObject.activeSelf)
        {
            Hide();
        }
    }
    
    /// <summary>
    /// 初始化面板
    /// </summary>
    private void InitializePanel()
    {
        // 初始化选项控制器
        if (optionController != null)
        {
            optionController.OnOptionClicked += OnOptionClicked;
        }
        
        // 初始化图片浏览控制器
        if (imageViewer != null)
        {
            imageViewer.OnBackClicked += OnImageViewBackClicked;
        }
        
        // 绑定返回按钮
        if (backButton != null)
        {
            backButton.onClick.RemoveAllListeners();
            backButton.onClick.AddListener(OnBackClicked);
            if (enableDebugLog)
            {
                Debug.Log("EmployeeHandbookPanel: 返回按钮已绑定");
            }
        }
        else
        {
            if (enableDebugLog)
            {
                Debug.LogWarning("EmployeeHandbookPanel: 返回按钮未配置，无法关闭弹窗");
            }
        }
        
        // 初始状态：显示选项列表
        currentState = ViewState.OptionList;
        
        if (enableDebugLog)
        {
            Debug.Log("EmployeeHandbookPanel: 面板初始化完成");
        }
    }
    
    /// <summary>
    /// 显示员工手册面板
    /// </summary>
    public void Show()
    {
        // 总是确保初始化完成
        if (!isInitialized)
        {
            InitializePanel();
            isInitialized = true;
        }
        
        // 激活面板
        if (!gameObject.activeSelf)
        {
            gameObject.SetActive(true);
        }
        
        // 显示选项列表（第一层级）
        ShowOptionList();
    }
    
    /// <summary>
    /// 隐藏员工手册面板
    /// </summary>
    public void Hide()
    {
        gameObject.SetActive(false);
    }
    
    /// <summary>
    /// 显示选项列表（第一层级）
    /// </summary>
    private void ShowOptionList()
    {
        currentState = ViewState.OptionList;
        
        // 显示选项列表视图
        if (optionListView != null)
        {
            optionListView.SetActive(true);
        }
        if (optionController != null)
        {
            optionController.Show();
        }
        
        // 隐藏图片浏览视图
        if (imageView != null)
        {
            imageView.SetActive(false);
        }
        if (imageViewer != null)
        {
            imageViewer.Hide();
        }
        
        if (enableDebugLog)
        {
            Debug.Log("EmployeeHandbookPanel: 显示选项列表");
        }
    }
    
    /// <summary>
    /// 显示图片浏览（第二层级）
    /// </summary>
    private void ShowImageView(int sectionIndex)
    {
        if (dataConfig == null)
        {
            Debug.LogWarning("EmployeeHandbookPanel: dataConfig未配置，无法显示图片");
            return;
        }
        
        // 获取指定选项的图片列表
        List<Sprite> images = dataConfig.GetSectionImages(sectionIndex);
        
        if (images == null || images.Count == 0)
        {
            Debug.LogWarning($"EmployeeHandbookPanel: 选项 {sectionIndex} 没有配置图片");
            return;
        }
        
        currentState = ViewState.ImageView;
        
        // 隐藏选项列表视图
        if (optionListView != null)
        {
            optionListView.SetActive(false);
        }
        if (optionController != null)
        {
            optionController.Hide();
        }
        
        // 显示图片浏览视图
        if (imageView != null)
        {
            imageView.SetActive(true);
        }
        if (imageViewer != null)
        {
            imageViewer.Show();
            imageViewer.ShowSection(sectionIndex, images);
        }
        
        if (enableDebugLog)
        {
            Debug.Log($"EmployeeHandbookPanel: 显示选项 {sectionIndex} 的图片浏览，共 {images.Count} 张图片");
        }
    }
    
    /// <summary>
    /// 选项点击事件（从选项控制器触发）
    /// </summary>
    private void OnOptionClicked(int optionIndex)
    {
        if (enableDebugLog)
        {
            Debug.Log($"EmployeeHandbookPanel: 选项 {optionIndex} 被点击");
        }
        
        // 切换到图片浏览视图
        ShowImageView(optionIndex);
    }
    
    /// <summary>
    /// 图片浏览视图的返回按钮点击事件
    /// </summary>
    private void OnImageViewBackClicked()
    {
        if (enableDebugLog)
        {
            Debug.Log("EmployeeHandbookPanel: 从图片浏览返回选项列表");
        }
        
        // 返回到选项列表
        ShowOptionList();
    }
    
    /// <summary>
    /// 返回按钮点击事件（在主面板层级，用于关闭整个弹窗）
    /// 注意：这个按钮应该在选项列表视图中有，而不是在图片浏览视图中
    /// </summary>
    private void OnBackClicked()
    {
        if (currentState == ViewState.ImageView)
        {
            // 如果在图片浏览状态，返回到选项列表
            OnImageViewBackClicked();
        }
        else
        {
            // 如果在选项列表状态，关闭整个弹窗
            if (enableDebugLog)
            {
                Debug.Log("EmployeeHandbookPanel: 关闭员工手册面板");
            }
            
            if (UIManager.Instance != null)
            {
                UIManager.Instance.ReturnToMainHub();
            }
        }
    }
    
    void OnDestroy()
    {
        // 清理事件订阅
        if (optionController != null)
        {
            optionController.OnOptionClicked -= OnOptionClicked;
        }
        
        if (imageViewer != null)
        {
            imageViewer.OnBackClicked -= OnImageViewBackClicked;
        }
    }
}

