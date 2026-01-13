using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections;

/// <summary>
/// Sphere信息面板控制器
/// 管理订单信息面板的显示、隐藏和按钮事件
/// </summary>
public class SphereInfoPanel : MonoBehaviour
{
    [Header("UI引用")]
    [SerializeField] private Image panelImage;      // 显示订单图片的Image组件
    [SerializeField] private Button closeButton;    // 关闭按钮
    [SerializeField] private Button jumpButton;     // 跳转场景按钮（前往配送）
    
    [Header("数据配置")]
    [Tooltip("订单数据配置（用于更新订单状态）")]
    [SerializeField] private SphereOrderDataConfig orderDataConfig; // 订单数据配置引用
    
    [Header("高亮控制器")]
    [SerializeField] private MenuHighlightController highlightController; // 高亮跟随控制器
    
    [Header("音效")]
    [SerializeField] private AudioSource audioSource;          // 音频源组件
    [SerializeField] private AudioClip buttonHoverSound;       // 按钮悬停音效
    [SerializeField] private AudioClip buttonClickSound;       // 按钮点击音效
    [SerializeField] private float clickSoundDelay = 0.15f;    // 点击音效播放后的延迟时间（秒），用于确保音效播放完成再执行后续操作
    
    [Header("调试")]
    [SerializeField] private bool enableDebugLog = true; // 是否启用调试日志
    
    private string currentTargetSceneName; // 当前面板的目标场景名称
    private SphereOrderDataConfig.SphereOrderInfo currentOrderInfo; // 当前显示的订单信息
    
    void Start()
    {
        // 如果未手动指定 AudioSource，尝试自动获取
        if (audioSource == null)
        {
            audioSource = GetComponent<AudioSource>();
            // 如果还是没有，自动添加一个
            if (audioSource == null)
            {
                audioSource = gameObject.AddComponent<AudioSource>();
            }
        }
        
        // 绑定按钮事件
        if (closeButton != null)
        {
            closeButton.onClick.AddListener(OnCloseClicked);
            // 添加鼠标悬停高亮效果
            SetupButtonHoverHighlight(closeButton);
        }
        else
        {
            Debug.LogError("SphereInfoPanel: closeButton未配置！");
        }
        
        if (jumpButton != null)
        {
            jumpButton.onClick.AddListener(OnJumpClicked);
            // 添加鼠标悬停高亮效果
            SetupButtonHoverHighlight(jumpButton);
        }
        else
        {
            Debug.LogError("SphereInfoPanel: jumpButton未配置！");
        }
        
        if (panelImage == null)
        {
            Debug.LogError("SphereInfoPanel: panelImage未配置！");
        }
        
        // 默认隐藏面板
        gameObject.SetActive(false);
    }
    
    /// <summary>
    /// 为按钮设置鼠标悬停高亮效果
    /// </summary>
    /// <param name="button">目标按钮</param>
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
        bool alreadyExists = false;
        foreach (EventTrigger.Entry entry in eventTrigger.triggers)
        {
            if (entry.eventID == EventTriggerType.PointerEnter)
            {
                alreadyExists = true;
                break;
            }
        }
        
        // 如果不存在，则添加
        if (!alreadyExists)
        {
            // 创建 PointerEnter 事件（鼠标进入）
            EventTrigger.Entry entryEnter = new EventTrigger.Entry();
            entryEnter.eventID = EventTriggerType.PointerEnter;
            entryEnter.callback.AddListener((eventData) => OnButtonHovered(button.GetComponent<RectTransform>()));
            
            // 添加到 EventTrigger
            eventTrigger.triggers.Add(entryEnter);
        }
    }
    
    /// <summary>
    /// 按钮鼠标悬停事件（通过按钮引用）
    /// </summary>
    /// <param name="buttonRect">按钮的 RectTransform</param>
    private void OnButtonHovered(RectTransform buttonRect)
    {
        if (highlightController != null && buttonRect != null)
        {
            highlightController.MoveToButton(buttonRect);
        }
        
        // 播放悬停音效
        PlayButtonHoverSound();
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
    
    /// <summary>
    /// 显示面板
    /// </summary>
    /// <param name="image">订单图片</param>
    /// <param name="sceneName">目标场景名称</param>
    public void Show(Sprite image, string sceneName)
    {
        if (enableDebugLog)
        {
            Debug.Log($"SphereInfoPanel: Show方法被调用 - image: {(image != null ? image.name : "null")}, sceneName: {sceneName}");
        }
        
        if (panelImage == null)
        {
            Debug.LogError("SphereInfoPanel: panelImage未配置，无法显示面板！");
            return;
        }
        
        if (image == null)
        {
            Debug.LogWarning("SphereInfoPanel: 订单图片为空！");
        }
        
        if (string.IsNullOrEmpty(sceneName))
        {
            Debug.LogWarning("SphereInfoPanel: 目标场景名称为空！");
        }
        
        // 设置图片
        panelImage.sprite = image;
        
        // 保存目标场景名称
        currentTargetSceneName = sceneName;
        
        // 查找并保存当前订单信息（用于更新订单状态）
        UpdateCurrentOrderInfo(image);
        
        // 显示面板
        Debug.Log($"SphereInfoPanel: 准备激活GameObject - 当前状态: {gameObject.activeSelf}, 父对象: {(transform.parent != null ? transform.parent.name : "null")}");
        
        // 确保父对象已启用
        if (transform.parent != null && !transform.parent.gameObject.activeSelf)
        {
            Debug.LogWarning($"SphereInfoPanel: 父对象 {transform.parent.name} 被禁用，正在启用...");
            transform.parent.gameObject.SetActive(true);
        }
        
        // 确保Canvas已启用并设置最高层级
        Canvas canvas = GetComponentInParent<Canvas>();
        if (canvas != null)
        {
            Debug.Log($"SphereInfoPanel: 找到Canvas - {canvas.name}, Active: {canvas.gameObject.activeSelf}, RenderMode: {canvas.renderMode}, SortOrder: {canvas.sortingOrder}");
            if (!canvas.gameObject.activeSelf)
            {
                Debug.LogWarning($"SphereInfoPanel: Canvas {canvas.name} 被禁用，正在启用...");
                canvas.gameObject.SetActive(true);
            }
            
            // 确保Canvas在最上层（设置一个很高的Sort Order）
            if (canvas.sortingOrder < 100)
            {
                canvas.sortingOrder = 100;
                Debug.Log($"SphereInfoPanel: 已设置Canvas Sort Order为 {canvas.sortingOrder}，确保面板在最上层");
            }
        }
        
        gameObject.SetActive(true);
        Debug.Log($"SphereInfoPanel: GameObject已激活 - 新状态: {gameObject.activeSelf}, 激活层级: {gameObject.activeInHierarchy}");
        
        // 检查面板位置和Canvas设置
        RectTransform rectTransform = GetComponent<RectTransform>();
        if (rectTransform != null)
        {
            Debug.Log($"SphereInfoPanel: 面板位置 - Pos: {rectTransform.anchoredPosition}, Size: {rectTransform.sizeDelta}, Active: {gameObject.activeSelf}, ActiveInHierarchy: {gameObject.activeInHierarchy}");
            
            // 检查Canvas的Sort Order
            if (canvas != null)
            {
                Debug.Log($"SphereInfoPanel: Canvas Sort Order: {canvas.sortingOrder}");
            }
        }
        
        // 强制刷新Canvas
        if (canvas != null)
        {
            Canvas.ForceUpdateCanvases();
            Debug.Log("SphereInfoPanel: 已强制刷新Canvas");
        }
        
        // 初始化高亮位置（对齐到第一个按钮）
        if (highlightController != null && closeButton != null)
        {
            RectTransform firstButtonRect = closeButton.GetComponent<RectTransform>();
            if (firstButtonRect != null)
            {
                highlightController.MoveToButton(firstButtonRect);
            }
        }
        
        if (enableDebugLog)
        {
            Debug.Log($"SphereInfoPanel: 显示面板完成，场景: {sceneName}");
        }
    }
    
    /// <summary>
    /// 隐藏面板
    /// </summary>
    public void Hide()
    {
        gameObject.SetActive(false);
        
        if (enableDebugLog)
        {
            Debug.Log("SphereInfoPanel: 隐藏面板");
        }
    }
    
    /// <summary>
    /// 关闭按钮点击事件
    /// </summary>
    private void OnCloseClicked()
    {
        // 播放点击音效
        PlayButtonClickSound();
        
        // 直接隐藏面板（无需延迟）
        Hide();
    }
    
    /// <summary>
    /// 跳转场景按钮点击事件（前往配送）
    /// </summary>
    private void OnJumpClicked()
    {
        // 更新订单的访问状态
        MarkOrderAsVisited();
        
        // 播放点击音效并延迟执行场景跳转，确保音效能够播放
        StartCoroutine(PlayClickSoundAndLoadScene());
    }
    
    /// <summary>
    /// 更新当前订单信息（通过订单图片查找）
    /// </summary>
    /// <param name="orderImage">订单图片</param>
    private void UpdateCurrentOrderInfo(Sprite orderImage)
    {
        currentOrderInfo = null;
        
        if (orderDataConfig == null)
        {
            if (enableDebugLog)
            {
                Debug.LogWarning("SphereInfoPanel: orderDataConfig未配置，无法更新订单状态。请在Inspector中配置Order Data Config引用。");
            }
            return;
        }
        
        if (orderImage == null)
        {
            if (enableDebugLog)
            {
                Debug.LogWarning("SphereInfoPanel: 订单图片为空，无法查找订单信息。");
            }
            return;
        }
        
        if (orderDataConfig.orderDataList == null || orderDataConfig.orderDataList.Count == 0)
        {
            if (enableDebugLog)
            {
                Debug.LogWarning("SphereInfoPanel: 订单数据列表为空，无法查找订单信息。");
            }
            return;
        }
        
        if (enableDebugLog)
        {
            Debug.Log($"SphereInfoPanel: 开始查找订单信息 - 图片名称: {orderImage.name}, 订单总数: {orderDataConfig.orderDataList.Count}");
        }
        
        // 遍历所有订单，查找匹配的订单图片
        foreach (var orderInfo in orderDataConfig.orderDataList)
        {
            if (orderInfo != null)
            {
                // 先比较引用，如果引用相同，直接匹配
                if (orderInfo.orderImage == orderImage)
                {
                    currentOrderInfo = orderInfo;
                    if (enableDebugLog)
                    {
                        Debug.Log($"SphereInfoPanel: 找到当前订单信息 - Sphere: {orderInfo.sphereName}, TaskId: {orderInfo.taskId}");
                    }
                    return;
                }
                
                // 如果引用不同，比较名称（备用方案）
                if (orderInfo.orderImage != null && orderImage != null && 
                    orderInfo.orderImage.name == orderImage.name)
                {
                    currentOrderInfo = orderInfo;
                    if (enableDebugLog)
                    {
                        Debug.Log($"SphereInfoPanel: 通过名称匹配找到订单信息 - Sphere: {orderInfo.sphereName}, TaskId: {orderInfo.taskId} (图片引用不同，但名称相同)");
                    }
                    return;
                }
            }
        }
        
        if (enableDebugLog)
        {
            Debug.LogWarning($"SphereInfoPanel: 未找到匹配的订单信息（图片: {orderImage.name}）。请检查SphereOrderDataConfig中是否有订单使用了此图片。");
            // 输出所有订单的图片名称，帮助调试
            Debug.LogWarning("SphereInfoPanel: 当前配置的订单图片列表：");
            for (int i = 0; i < orderDataConfig.orderDataList.Count; i++)
            {
                var info = orderDataConfig.orderDataList[i];
                if (info != null)
                {
                    string imgName = info.orderImage != null ? info.orderImage.name : "null";
                    Debug.LogWarning($"  订单 {i}: Sphere={info.sphereName}, 图片={imgName}");
                }
            }
        }
    }
    
    /// <summary>
    /// 标记当前订单为已访问
    /// </summary>
    private void MarkOrderAsVisited()
    {
        if (currentOrderInfo == null)
        {
            if (enableDebugLog)
            {
                Debug.LogWarning("SphereInfoPanel: 当前订单信息为空，无法更新访问状态。可能原因：1) orderDataConfig未配置 2) 订单图片匹配失败");
            }
            return;
        }
        
        // 设置访问状态为真
        bool oldValue = currentOrderInfo.VisitOrder;
        currentOrderInfo.VisitOrder = true;
        
        if (enableDebugLog)
        {
            Debug.Log($"SphereInfoPanel: 订单 {currentOrderInfo.sphereName} (TaskId: {currentOrderInfo.taskId}) 已标记为已访问 (从 {oldValue} 变为 true)");
        }
    }
    
    /// <summary>
    /// 播放点击音效并延迟加载场景（协程）
    /// </summary>
    private IEnumerator PlayClickSoundAndLoadScene()
    {
        if (string.IsNullOrEmpty(currentTargetSceneName))
        {
            Debug.LogError("SphereInfoPanel: 目标场景名称为空，无法跳转！");
            yield break;
        }
        
        // 播放点击音效
        PlayButtonClickSound();
        
        // 等待一小段时间，让音效有时间播放
        yield return new WaitForSeconds(clickSoundDelay);
        
        if (enableDebugLog)
        {
            Debug.Log($"SphereInfoPanel: 跳转到场景: {currentTargetSceneName}");
        }
        
        // 使用SceneTransitionManager加载场景
        if (SceneTransitionManager.Instance != null)
        {
            SceneTransitionManager.Instance.LoadSceneByName(currentTargetSceneName);
        }
        else
        {
            Debug.LogError("SphereInfoPanel: SceneTransitionManager未找到！");
        }
    }
    
    /// <summary>
    /// 检查面板是否显示
    /// </summary>
    public bool IsVisible()
    {
        return gameObject.activeSelf;
    }
}
