using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 订单面板控制器
/// 处理订单面板的显示和交互
/// </summary>
public class OrderPanel : MonoBehaviour
{
    [Header("UI引用")]
    [SerializeField] private Button goToDeliveryButton;  // 前往配送按钮
    [SerializeField] private Button cancelButton;        // 取消配送按钮
    
    [Header("订单内容显示")]
    [SerializeField] private Text orderTitleText;        // 订单标题文本（可选）
    [SerializeField] private Text orderContentText;      // 订单内容文本
    [SerializeField] private Text orderDetailText;       // 订单详情文本（可选）
    
    private bool isInitialized = false;
    
    void Awake()
    {
        // 初始化订单内容（占位数据）
        if (orderContentText != null)
        {
            orderContentText.text = "订单详情功能开发中...\n\n这里将显示订单的相关信息\n配送地点：未指定\n配送时间：待定";
        }
        
        if (orderTitleText != null)
        {
            orderTitleText.text = "订单详情";
        }
    }
    
    void OnEnable()
    {
        // 当面板被激活时，确保按钮事件已绑定
        if (!isInitialized)
        {
            InitializePanel();
            isInitialized = true;
        }
        
        // 每次显示时重新绑定（防止事件丢失）
        if (goToDeliveryButton != null)
        {
            goToDeliveryButton.onClick.RemoveListener(OnGoToDeliveryClicked);
            goToDeliveryButton.onClick.AddListener(OnGoToDeliveryClicked);
        }
        
        if (cancelButton != null)
        {
            cancelButton.onClick.RemoveListener(OnCancelClicked);
            cancelButton.onClick.AddListener(OnCancelClicked);
        }
        
        // 每次显示时更新订单内容（如果有实际的订单数据）
        UpdateOrderContent();
    }
    
    void Start()
    {
        // 如果面板在场景中默认是激活的，在这里初始化
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
        // 绑定按钮事件
        if (goToDeliveryButton != null)
        {
            goToDeliveryButton.onClick.RemoveListener(OnGoToDeliveryClicked);
            goToDeliveryButton.onClick.AddListener(OnGoToDeliveryClicked);
        }
        
        if (cancelButton != null)
        {
            cancelButton.onClick.RemoveListener(OnCancelClicked);
            cancelButton.onClick.AddListener(OnCancelClicked);
        }
    }
    
    /// <summary>
    /// 显示订单面板
    /// </summary>
    public void Show()
    {
        // 总是确保初始化完成
        if (!isInitialized)
        {
            InitializePanel();
            isInitialized = true;
        }
        else
        {
            // 即使已经初始化，也确保按钮事件已绑定（防止事件丢失）
            if (goToDeliveryButton != null)
            {
                goToDeliveryButton.onClick.RemoveListener(OnGoToDeliveryClicked);
                goToDeliveryButton.onClick.AddListener(OnGoToDeliveryClicked);
            }
            
            if (cancelButton != null)
            {
                cancelButton.onClick.RemoveListener(OnCancelClicked);
                cancelButton.onClick.AddListener(OnCancelClicked);
            }
        }
        
        // 激活面板
        if (!gameObject.activeSelf)
        {
            gameObject.SetActive(true);
        }
        
        // 更新订单内容
        UpdateOrderContent();
    }
    
    /// <summary>
    /// 隐藏订单面板
    /// </summary>
    public void Hide()
    {
        gameObject.SetActive(false);
    }
    
    /// <summary>
    /// 更新订单内容（供外部调用，用于显示实际订单信息）
    /// </summary>
    private void UpdateOrderContent()
    {
        // TODO: 从订单系统获取实际订单数据
        // 目前使用占位数据
        if (orderContentText != null)
        {
            // 这里可以替换为实际的订单数据
            orderContentText.text = "订单详情功能开发中...\n\n这里将显示订单的相关信息\n配送地点：未指定\n配送时间：待定";
        }
    }
    
    /// <summary>
    /// 前往配送按钮点击事件
    /// </summary>
    private void OnGoToDeliveryClicked()
    {
        Debug.Log("前往配送 - 跳转到游戏场景");
        
        // 恢复时间，避免场景切换时时间仍为0
        Time.timeScale = 1f;
        
        // 加载游戏场景
        if (SceneTransitionManager.Instance != null)
        {
            SceneTransitionManager.Instance.LoadGameScene();
        }
        else
        {
            Debug.LogError("OrderPanel: SceneTransitionManager未找到！");
        }
    }
    
    /// <summary>
    /// 取消配送按钮点击事件
    /// </summary>
    private void OnCancelClicked()
    {
        Debug.Log("取消配送 - 返回主界面");
        
        // 隐藏订单面板，返回主界面
        Hide();
        
        // 确保主界面显示
        if (UIManager.Instance != null)
        {
            UIManager.Instance.ReturnToMainHub();
        }
    }
    
    /// <summary>
    /// 设置订单内容（供外部调用，用于更新订单信息）
    /// </summary>
    /// <param name="orderInfo">订单信息</param>
    public void SetOrderContent(string orderInfo)
    {
        if (orderContentText != null)
        {
            orderContentText.text = orderInfo;
        }
    }
    
    /// <summary>
    /// 设置订单标题（供外部调用）
    /// </summary>
    /// <param name="title">订单标题</param>
    public void SetOrderTitle(string title)
    {
        if (orderTitleText != null)
        {
            orderTitleText.text = title;
        }
    }
}

