using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections;

/// <summary>
/// 订单详情面板主控制器（结算界面）
/// 管理整个结算界面的显示、隐藏和组件协调
/// 在主界面按下空格键后显示此界面
/// </summary>
public class OrderDetailPanel : MonoBehaviour
{
    [Header("子组件引用")]
    [SerializeField] private OrderImageListController orderImageList;
    [SerializeField] private TextImageController textImageController;
    [SerializeField] private DynamicTextController dynamicTextController;
    [SerializeField] private StampAnimationController stampAnimation;
    
    [Header("按钮引用")]
    [Tooltip("重置所有订单访问状态的按钮")]
    [SerializeField] private Button resetVisitOrderButton; // 重置访问状态按钮
    
    [Tooltip("下一天按钮（跳转到下一个主界面场景）")]
    [SerializeField] private Button nextDayButton; // 下一天按钮
    
    [Header("数据配置")]
    [Tooltip("订单数据配置（用于重置订单状态）")]
    [SerializeField] private SphereOrderDataConfig orderDataConfig; // 订单数据配置引用
    
    [Header("场景配置")]
    [Tooltip("下一天按钮跳转的目标场景名称（必须在Build Settings中）")]
    [SerializeField] private string nextDaySceneName = "02_MainHub"; // 下一天场景名称
    
    [Header("MainHub Canvas配置")]
    [Tooltip("当前MainHub场景的Canvas引用（用于在加载游戏场景时隐藏，卸载时恢复）。请在Inspector中手动拖拽配置。")]
    [SerializeField] private Canvas mainHubCanvas; // MainHub场景的Canvas引用
    
    [Header("印章动画延迟")]
    [Tooltip("结算界面显示后，延迟多少秒播放印章动画（秒）")]
    [SerializeField] private float stampAnimationDelay = 1.0f; // 延迟时间（秒）
    
    [Header("调试")]
    [SerializeField] private bool enableDebugLog = true;
    
    // 当前是否显示
    private bool isShowing = false;
    
    void Start()
    {
        // 初始化各个子组件
        InitializeComponents();
        
        // 注册MainHub Canvas到SceneTransitionManager
        RegisterMainHubCanvas();
        
        // 初始隐藏面板（通过代码控制，不依赖Inspector状态）
        Hide();
    }
    
    void OnDestroy()
    {
        // 注销Canvas引用（可选，但建议添加以保持一致性）
        if (mainHubCanvas != null)
        {
            string currentSceneName = SceneManager.GetActiveScene().name;
            SceneTransitionManager.UnregisterMainHubCanvas(currentSceneName);
        }
    }
    
    /// <summary>
    /// 初始化各个子组件
    /// </summary>
    private void InitializeComponents()
    {
        // 验证引用
        if (orderImageList == null)
        {
            Debug.LogWarning("OrderDetailPanel: orderImageList未配置！");
        }
        
        if (textImageController == null)
        {
            Debug.LogWarning("OrderDetailPanel: textImageController未配置！");
        }
        
        if (dynamicTextController == null)
        {
            Debug.LogWarning("OrderDetailPanel: dynamicTextController未配置！");
        }
        
        if (stampAnimation == null)
        {
            Debug.LogWarning("OrderDetailPanel: stampAnimation未配置！");
        }
        
        // 绑定重置按钮事件
        if (resetVisitOrderButton != null)
        {
            resetVisitOrderButton.onClick.AddListener(OnResetVisitOrderClicked);
        }
        else
        {
            if (enableDebugLog)
            {
                Debug.LogWarning("OrderDetailPanel: resetVisitOrderButton未配置！如需重置功能，请在Inspector中配置。");
            }
        }
        
        // 绑定下一天按钮事件
        if (nextDayButton != null)
        {
            nextDayButton.onClick.AddListener(OnNextDayClicked);
        }
        else
        {
            if (enableDebugLog)
            {
                Debug.LogWarning("OrderDetailPanel: nextDayButton未配置！如需下一天功能，请在Inspector中配置。");
            }
        }
        
        // 订阅订单切换事件（如果订单列表控制器支持）
        if (orderImageList != null && dynamicTextController != null)
        {
            // 订单切换时更新文字
            orderImageList.OnOrderIndexChanged += (index) => {
                // 这里可以获取订单ID并更新文字
                // 当前版本：显示索引作为占位
                dynamicTextController.SetOrderId(index);
            };
        }
        
        if (enableDebugLog)
        {
            Debug.Log("OrderDetailPanel: 初始化完成");
        }
    }
    
    /// <summary>
    /// 注册MainHub Canvas到SceneTransitionManager
    /// </summary>
    private void RegisterMainHubCanvas()
    {
        // 获取当前场景名称
        string currentSceneName = SceneManager.GetActiveScene().name;
        
        // 如果Canvas已配置，注册到SceneTransitionManager
        if (mainHubCanvas != null)
        {
            SceneTransitionManager.RegisterMainHubCanvas(currentSceneName, mainHubCanvas);
            
            if (enableDebugLog)
            {
                Debug.Log($"OrderDetailPanel: 已注册场景 {currentSceneName} 的Canvas: {mainHubCanvas.name}");
            }
        }
        else
        {
            if (enableDebugLog)
            {
                Debug.LogWarning($"OrderDetailPanel: MainHub Canvas未配置！场景 {currentSceneName} 的Canvas无法在加载游戏场景时自动隐藏。请在Inspector中配置Main Hub Canvas字段。");
            }
        }
    }
    
    /// <summary>
    /// 显示结算界面
    /// </summary>
    public void Show()
    {
        gameObject.SetActive(true);
        isShowing = true;
        
        // 显示时，启用文字图片的A键切换
        if (textImageController != null)
        {
            textImageController.SetAKeyEnabled(true);
        }
        
        if (enableDebugLog)
        {
            Debug.Log("OrderDetailPanel: 结算界面已显示");
        }
        
        // 延迟播放印章动画
        StartCoroutine(PlayStampAnimationDelayed());
    }
    
    /// <summary>
    /// 延迟播放印章动画（协程）
    /// </summary>
    private IEnumerator PlayStampAnimationDelayed()
    {
        if (stampAnimationDelay > 0f)
        {
            if (enableDebugLog)
            {
                Debug.Log($"OrderDetailPanel: 等待 {stampAnimationDelay} 秒后播放印章动画");
            }
            yield return new WaitForSeconds(stampAnimationDelay);
        }
        
        // 播放印章动画
        PlayStampAnimation();
    }
    
    /// <summary>
    /// 隐藏结算界面
    /// </summary>
    public void Hide()
    {
        gameObject.SetActive(false);
        isShowing = false;
        
        // 隐藏时，禁用文字图片的A键切换
        if (textImageController != null)
        {
            textImageController.SetAKeyEnabled(false);
        }
        
        if (enableDebugLog)
        {
            Debug.Log("OrderDetailPanel: 结算界面已隐藏");
        }
    }
    
    /// <summary>
    /// 切换显示/隐藏状态
    /// </summary>
    public void Toggle()
    {
        if (isShowing)
        {
            Hide();
        }
        else
        {
            Show();
        }
    }
    
    /// <summary>
    /// 获取当前显示状态
    /// </summary>
    public bool IsShowing()
    {
        return isShowing;
    }
    
    /// <summary>
    /// 播放印章动画
    /// </summary>
    public void PlayStampAnimation()
    {
        if (stampAnimation != null)
        {
            if (enableDebugLog)
            {
                Debug.Log("OrderDetailPanel: 调用播放印章动画");
            }
            stampAnimation.PlayStampAnimation();
        }
        else
        {
            Debug.LogWarning("OrderDetailPanel: stampAnimation未配置！请在Inspector中配置Stamp Animation引用。");
        }
    }
    
    /// <summary>
    /// 重置所有订单访问状态按钮点击事件
    /// </summary>
    private void OnResetVisitOrderClicked()
    {
        if (enableDebugLog)
        {
            Debug.Log("OrderDetailPanel: 点击重置访问状态按钮");
        }
        
        ResetAllVisitOrder();
    }
    
    /// <summary>
    /// 重置所有订单的VisitOrder状态为false
    /// </summary>
    public void ResetAllVisitOrder()
    {
        if (orderDataConfig == null)
        {
            Debug.LogWarning("OrderDetailPanel: orderDataConfig未配置！无法重置订单访问状态。");
            return;
        }
        
        if (orderDataConfig.orderDataList == null || orderDataConfig.orderDataList.Count == 0)
        {
            if (enableDebugLog)
            {
                Debug.LogWarning("OrderDetailPanel: 订单数据列表为空，无需重置。");
            }
            return;
        }
        
        int resetCount = 0;
        
        // 遍历所有订单，重置VisitOrder状态
        foreach (var orderInfo in orderDataConfig.orderDataList)
        {
            if (orderInfo != null && orderInfo.VisitOrder)
            {
                orderInfo.VisitOrder = false;
                resetCount++;
            }
        }
        
        if (enableDebugLog)
        {
            Debug.Log($"OrderDetailPanel: 已重置 {resetCount} 个订单的访问状态");
        }
    }
    
    /// <summary>
    /// 下一天按钮点击事件
    /// </summary>
    private void OnNextDayClicked()
    {
        if (enableDebugLog)
        {
            Debug.Log($"OrderDetailPanel: 点击下一天按钮，准备跳转到{nextDaySceneName}场景");
        }
        
        // 跳转到下一个主界面场景
        LoadNextDayScene();
    }
    
    /// <summary>
    /// 加载下一天场景
    /// </summary>
    private void LoadNextDayScene()
    {
        // 检查场景名称是否配置
        if (string.IsNullOrEmpty(nextDaySceneName))
        {
            Debug.LogError("OrderDetailPanel: 下一天场景名称未配置！请在Inspector中设置Next Day Scene Name字段。");
            return;
        }
        
        // 使用SceneTransitionManager（如果存在）
        if (SceneTransitionManager.Instance != null)
        {
            SceneTransitionManager.Instance.LoadSceneByName(nextDaySceneName);
            
            if (enableDebugLog)
            {
                Debug.Log($"OrderDetailPanel: 正在通过SceneTransitionManager加载{nextDaySceneName}场景");
            }
        }
        else
        {
            // 直接使用SceneManager加载场景
            UnityEngine.SceneManagement.SceneManager.LoadScene(nextDaySceneName);
            
            if (enableDebugLog)
            {
                Debug.Log($"OrderDetailPanel: SceneTransitionManager未找到，直接使用SceneManager加载{nextDaySceneName}场景");
            }
        }
    }
}
