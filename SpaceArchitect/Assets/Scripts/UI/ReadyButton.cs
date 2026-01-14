using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// READY按钮控制器
/// 控制READY按钮的显示/隐藏，根据飞船状态自动管理
/// - Setup状态：显示按钮
/// - PreLaunch及其他状态：隐藏按钮
/// - 点击按钮后：执行READY序列并隐藏按钮
/// </summary>
public class ReadyButton : MonoBehaviour
{
    [Header("READY按钮引用")]
    [Tooltip("READY按钮的GameObject（用于控制显示/隐藏）")]
    [SerializeField] private GameObject readyButtonObject;
    
    [Tooltip("READY按钮的Button组件（可选，如果按钮对象本身有Button组件则不需要）")]
    [SerializeField] private Button readyButton;
    
    [Header("相机控制器引用")]
    [Tooltip("相机跟随脚本（用于调用StartReadySequence）")]
    [SerializeField] private CameraFollowShip cameraFollowShip;
    
    [Header("飞船状态引用")]
    [Tooltip("飞船状态管理器（用于监听状态变化）")]
    [SerializeField] private ShipState shipState;
    
    [Header("调试")]
    [Tooltip("显示调试信息")]
    [SerializeField] private bool showDebugLogs = false;
    
    private void Awake()
    {
        // 如果没有指定按钮对象，尝试使用当前GameObject
        if (readyButtonObject == null)
        {
            readyButtonObject = gameObject;
        }
        
        // 如果没有指定Button组件，尝试获取
        if (readyButton == null)
        {
            readyButton = readyButtonObject.GetComponent<Button>();
        }
        
        // 如果没有指定相机控制器，尝试查找
        if (cameraFollowShip == null)
        {
            cameraFollowShip = FindObjectOfType<CameraFollowShip>();
            if (cameraFollowShip == null && showDebugLogs)
            {
                Debug.LogWarning("ReadyButton: 未找到CameraFollowShip组件，请在Inspector中指定");
            }
        }
        
        // 如果没有指定飞船状态，尝试查找
        if (shipState == null)
        {
            shipState = FindObjectOfType<ShipState>();
            if (shipState == null && showDebugLogs)
            {
                Debug.LogWarning("ReadyButton: 未找到ShipState组件，请在Inspector中指定");
            }
        }
    }
    
    private void Start()
    {
        // 绑定按钮点击事件
        if (readyButton != null)
        {
            readyButton.onClick.AddListener(OnReadyButtonClicked);
        }
        else
        {
            Debug.LogError("ReadyButton: 未找到Button组件，请在Inspector中指定或添加Button组件");
        }
        
        // 订阅飞船状态变化事件
        if (EventManager.Instance != null)
        {
            EventManager.Instance.OnShipStateChanged += OnShipStateChanged;
        }
        else
        {
            Debug.LogError("ReadyButton: EventManager未找到！");
        }
        
        // 根据初始状态设置按钮显示
        UpdateButtonVisibility();
    }
    
    private void OnDestroy()
    {
        // 取消订阅事件
        if (EventManager.Instance != null)
        {
            EventManager.Instance.OnShipStateChanged -= OnShipStateChanged;
        }
        
        // 移除按钮点击事件
        if (readyButton != null)
        {
            readyButton.onClick.RemoveListener(OnReadyButtonClicked);
        }
    }
    
    /// <summary>
    /// READY按钮点击事件
    /// </summary>
    private void OnReadyButtonClicked()
    {
        if (showDebugLogs)
        {
            Debug.Log("ReadyButton: READY按钮被点击");
        }
        
        // 检查飞船状态（只能在Setup状态下点击）
        if (shipState != null && shipState.CurrentState != ShipState.State.Setup)
        {
            if (showDebugLogs)
            {
                Debug.LogWarning($"ReadyButton: 飞船当前状态为 {shipState.CurrentState}，无法执行READY序列。只能在Setup状态下点击。");
            }
            return;
        }
        
        // 立即隐藏按钮（在动画开始前就隐藏，给用户即时反馈）
        HideButton();
        
        // 调用相机控制器的READY序列
        if (cameraFollowShip != null)
        {
            cameraFollowShip.StartReadySequence();
            if (showDebugLogs)
            {
                Debug.Log("ReadyButton: 已调用CameraFollowShip.StartReadySequence()");
            }
        }
        else
        {
            Debug.LogError("ReadyButton: CameraFollowShip未找到，无法执行READY序列！");
        }
    }
    
    /// <summary>
    /// 飞船状态变化事件处理
    /// </summary>
    private void OnShipStateChanged(ShipState.State oldState, ShipState.State newState, GameObject ship)
    {
        // 只处理当前飞船的状态变化
        if (ship != null && shipState != null && ship != shipState.gameObject)
        {
            return;
        }
        
        if (showDebugLogs)
        {
            Debug.Log($"ReadyButton: 飞船状态变化 {oldState} -> {newState}");
        }
        
        // 根据新状态更新按钮显示
        UpdateButtonVisibility();
    }
    
    /// <summary>
    /// 根据飞船状态更新按钮显示/隐藏
    /// </summary>
    private void UpdateButtonVisibility()
    {
        if (shipState == null || readyButtonObject == null)
        {
            return;
        }
        
        // Setup状态：显示按钮
        // 其他状态：隐藏按钮
        bool shouldShow = (shipState.CurrentState == ShipState.State.Setup);
        
        if (readyButtonObject.activeSelf != shouldShow)
        {
            readyButtonObject.SetActive(shouldShow);
            
            if (showDebugLogs)
            {
                Debug.Log($"ReadyButton: 按钮显示状态已更新 - 状态: {shipState.CurrentState}, 显示: {shouldShow}");
            }
        }
    }
    
    /// <summary>
    /// 显示按钮（供外部调用）
    /// </summary>
    public void ShowButton()
    {
        if (readyButtonObject != null)
        {
            readyButtonObject.SetActive(true);
            if (showDebugLogs)
            {
                Debug.Log("ReadyButton: 按钮已显示");
            }
        }
    }
    
    /// <summary>
    /// 隐藏按钮（供外部调用）
    /// </summary>
    public void HideButton()
    {
        if (readyButtonObject != null)
        {
            readyButtonObject.SetActive(false);
            if (showDebugLogs)
            {
                Debug.Log("ReadyButton: 按钮已隐藏");
            }
        }
    }
}




