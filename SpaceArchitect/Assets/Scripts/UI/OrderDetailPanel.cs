using UnityEngine;

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
    
    [Header("调试")]
    [SerializeField] private bool enableDebugLog = true;
    
    // 当前是否显示
    private bool isShowing = false;
    
    void Start()
    {
        // 初始化各个子组件
        InitializeComponents();
        
        // 初始隐藏面板（通过代码控制，不依赖Inspector状态）
        Hide();
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
            stampAnimation.PlayStampAnimation();
        }
    }
}
