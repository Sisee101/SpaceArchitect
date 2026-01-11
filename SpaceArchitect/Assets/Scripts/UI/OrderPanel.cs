using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

/// <summary>
/// 订单面板控制器
/// 处理订单面板的显示和交互
/// </summary>
public class OrderPanel : MonoBehaviour
{
    [Header("UI引用")]
    [SerializeField] private Button goToDeliveryButton;  // 前往配送按钮
    [SerializeField] private Button cancelButton;        // 取消配送按钮
    
    [Header("订单图片和盖章")]
    [SerializeField] private Image orderImage;           // 订单图片（包含所有文字和内容）
    [SerializeField] private GameObject stampOverlay;    // 盖章图层GameObject
    [SerializeField] private Image stampImage;           // 盖章图片（可选，如果需要在代码中控制）
    
    [Header("其他组件")]
    [SerializeField] private CanvasGroup canvasGroup;    // CanvasGroup组件（用于淡入淡出）
    
    private bool isInitialized = false;
    
    /// <summary>
    /// 获取CanvasGroup组件（供外部访问）
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
    }
    
    /// <summary>
    /// 隐藏订单面板
    /// </summary>
    public void Hide()
    {
        gameObject.SetActive(false);
    }
    
    /// <summary>
    /// 设置订单图片
    /// </summary>
    /// <param name="image">订单图片</param>
    public void SetOrderImage(Sprite image)
    {
        if (orderImage != null && image != null)
        {
            orderImage.sprite = image;
        }
        else if (orderImage == null)
        {
            Debug.LogWarning("OrderPanel: orderImage引用为空，无法设置订单图片");
        }
    }
    
    /// <summary>
    /// 显示盖章图层
    /// </summary>
    public void ShowStamp()
    {
        if (stampOverlay != null)
        {
            stampOverlay.SetActive(true);
            
            // 可选：添加淡入动画
            if (stampImage != null)
            {
                stampImage.color = new Color(stampImage.color.r, stampImage.color.g, stampImage.color.b, 0f);
                stampImage.DOFade(1f, 0.3f).SetEase(Ease.OutQuad);
            }
        }
        else
        {
            Debug.LogWarning("OrderPanel: stampOverlay引用为空，无法显示盖章");
        }
    }
    
    /// <summary>
    /// 隐藏盖章图层
    /// </summary>
    public void HideStamp()
    {
        if (stampOverlay != null)
        {
            stampOverlay.SetActive(false);
        }
    }
    
    /// <summary>
    /// 重置位置（用于准备下次切换）
    /// </summary>
    /// <param name="x">X坐标</param>
    public void ResetPosition(float x)
    {
        Vector3 pos = transform.localPosition;
        transform.localPosition = new Vector3(x, pos.y, pos.z);
    }
    
    /// <summary>
    /// 前往配送按钮点击事件
    /// </summary>
    private void OnGoToDeliveryClicked()
    {
        Debug.Log("前往配送 - 跳转到游戏场景");
        
        // 保存面板状态（面板是打开的）
        OrderTransitionController transitionController = FindObjectOfType<OrderTransitionController>();
        if (transitionController != null)
        {
            transitionController.SavePanelState();
        }
        
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
    
}

