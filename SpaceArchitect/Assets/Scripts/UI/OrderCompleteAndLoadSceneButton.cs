using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// 订单完成并卸载游戏场景按钮脚本
/// 结合订单完成和场景卸载功能：
/// 1. 在 Inspector 中选择订单，修改对应订单的 CompleteOrder 状态为 true
/// 2. 卸载当前游戏场景，返回MainHub
/// 3. 订单状态会通过 TaskManager 持久化保存（跨场景和跨游戏会话）
/// </summary>
public class OrderCompleteAndLoadSceneButton : MonoBehaviour
{
    [Header("按钮引用")]
    [Tooltip("用于触发订单完成和场景加载操作的 Unity UI Button")]
    [SerializeField] private Button actionButton;
    
    [Header("数据配置")]
    [Tooltip("订单数据配置（ScriptableObject），包含所有订单信息")]
    [SerializeField] private SphereOrderDataConfig orderDataConfig;
    
    [Header("订单选择")]
    [Tooltip("在 Inspector 中通过下拉框选择要完成的订单（索引对应 orderDataList 中的顺序）")]
    [OrderSelector]
    [SerializeField] private int selectedOrderIndex = 0;
    
    // 注意：已移除场景配置，因为卸载场景不需要指定场景名称
    
    [Header("任务管理器（可选）")]
    [Tooltip("如果配置了 TaskManager，点击按钮时会调用 TaskManager.CompleteTask() 来同步任务状态和持久化")]
    [SerializeField] private TaskManager taskManager;
    
    [Header("持久化设置")]
    [Tooltip("是否使用PlayerPrefs保存订单完成状态（跨场景和跨游戏会话持久化）。如果订单有taskId，会通过TaskManager持久化；如果没有taskId，会直接保存到PlayerPrefs")]
    [SerializeField] private bool usePersistentStorage = true;
    
    [Header("音效（可选）")]
    [Tooltip("音频源组件（如果未指定，会自动获取或创建）")]
    [SerializeField] private AudioSource audioSource;
    
    [Tooltip("按钮点击音效（可选，如果未配置则直接执行操作）")]
    [SerializeField] private AudioClip buttonClickSound;
    
    [Tooltip("点击音效播放后的延迟时间（秒），用于确保音效播放完成再执行后续操作")]
    [SerializeField] private float clickSoundDelay = 0.15f;
    
    [Header("调试")]
    [Tooltip("是否启用调试日志")]
    [SerializeField] private bool enableDebugLog = true;
    
    // PlayerPrefs键名前缀（用于没有taskId的订单）
    private const string ORDER_COMPLETED_KEY_PREFIX = "OrderCompleted_";
    
    // 静态变量：存储场景切换时新完成的订单信息（用于在新场景中播放动画）
    // 使用 taskId 作为键（如果有），否则使用 sphereName
    private static HashSet<int> pendingCompletedTaskIds = new HashSet<int>();
    private static HashSet<string> pendingCompletedSphereNames = new HashSet<string>();
    
    void Start()
    {
        // 自动查找 TaskManager（如果未手动配置）
        if (taskManager == null)
        {
            taskManager = FindObjectOfType<TaskManager>();
        }
        
        // 绑定按钮点击事件
        if (actionButton != null)
        {
            actionButton.onClick.AddListener(OnButtonClicked);
            
            if (enableDebugLog)
            {
                Debug.Log($"OrderCompleteAndLoadSceneButton: 已绑定按钮点击事件，当前选中订单索引: {selectedOrderIndex}，将卸载游戏场景并返回MainHub");
            }
        }
        else
        {
            Debug.LogWarning("OrderCompleteAndLoadSceneButton: Action Button 未配置！请在 Inspector 中设置按钮引用。");
        }
        
        // 验证配置
        if (orderDataConfig == null)
        {
            Debug.LogWarning("OrderCompleteAndLoadSceneButton: Order Data Config 未配置！请在 Inspector 中设置订单数据配置。");
        }
        
        // 如果未手动指定 AudioSource，尝试自动获取
        if (audioSource == null && buttonClickSound != null)
        {
            audioSource = GetComponent<AudioSource>();
            // 如果还是没有，自动添加一个
            if (audioSource == null)
            {
                audioSource = gameObject.AddComponent<AudioSource>();
            }
        }
    }
    
    void OnDestroy()
    {
        // 取消绑定按钮点击事件
        if (actionButton != null)
        {
            actionButton.onClick.RemoveListener(OnButtonClicked);
        }
    }
    
    /// <summary>
    /// 按钮点击事件处理
    /// </summary>
    private void OnButtonClicked()
    {
        // 验证配置
        if (orderDataConfig == null)
        {
            Debug.LogError("OrderCompleteAndLoadSceneButton: Order Data Config 未配置，无法完成订单！");
            return;
        }
        
        if (orderDataConfig.orderDataList == null || orderDataConfig.orderDataList.Count == 0)
        {
            Debug.LogError("OrderCompleteAndLoadSceneButton: 订单数据列表为空，无法完成订单！");
            return;
        }
        
        // 验证索引有效性
        if (selectedOrderIndex < 0 || selectedOrderIndex >= orderDataConfig.orderDataList.Count)
        {
            Debug.LogError($"OrderCompleteAndLoadSceneButton: 选中的订单索引 {selectedOrderIndex} 超出范围（有效范围: 0-{orderDataConfig.orderDataList.Count - 1}）！");
            return;
        }
        
        // 获取选中的订单信息
        var orderInfo = orderDataConfig.orderDataList[selectedOrderIndex];
        if (orderInfo == null)
        {
            Debug.LogError($"OrderCompleteAndLoadSceneButton: 索引 {selectedOrderIndex} 对应的订单信息为空！");
            return;
        }
        
        // 检查订单是否已完成
        if (orderInfo.CompleteOrder)
        {
            if (enableDebugLog)
            {
                Debug.LogWarning($"OrderCompleteAndLoadSceneButton: 订单 {orderInfo.sphereName} (索引: {selectedOrderIndex}) 已经完成，但仍会卸载游戏场景");
            }
            // 即使已完成，也继续卸载场景（允许重复操作）
        }
        
        // 如果配置了音效，播放音效并延迟执行
        if (buttonClickSound != null && audioSource != null)
        {
            StartCoroutine(PlaySoundAndCompleteOrder(orderInfo));
        }
        else
        {
            // 直接执行订单完成和场景卸载
            CompleteOrderAndUnloadScene(orderInfo);
        }
    }
    
    /// <summary>
    /// 播放音效并完成订单（协程）
    /// </summary>
    private IEnumerator PlaySoundAndCompleteOrder(SphereOrderDataConfig.SphereOrderInfo orderInfo)
    {
        // 播放点击音效
        if (audioSource != null && buttonClickSound != null)
        {
            audioSource.PlayOneShot(buttonClickSound);
            
            if (enableDebugLog)
            {
                Debug.Log($"OrderCompleteAndLoadSceneButton: 播放点击音效，延迟 {clickSoundDelay} 秒后完成订单并卸载游戏场景");
            }
        }
        
        // 等待延迟时间
        yield return new WaitForSeconds(clickSoundDelay);
        
        // 完成订单并卸载游戏场景
        CompleteOrderAndUnloadScene(orderInfo);
    }
    
    /// <summary>
    /// 完成订单并卸载游戏场景
    /// </summary>
    private void CompleteOrderAndUnloadScene(SphereOrderDataConfig.SphereOrderInfo orderInfo)
    {
        // 设置 CompleteOrder 为 true
        if (!orderInfo.CompleteOrder)
        {
            orderInfo.CompleteOrder = true;
            
            if (enableDebugLog)
            {
                Debug.Log($"OrderCompleteAndLoadSceneButton: 已将订单 {orderInfo.sphereName} (索引: {selectedOrderIndex}, TaskId: {orderInfo.taskId}) 的 CompleteOrder 状态设置为 true");
            }
        }
        
        // 将订单信息存储到静态变量，以便在新场景中播放动画
        if (orderInfo.taskId >= 0)
        {
            pendingCompletedTaskIds.Add(orderInfo.taskId);
            if (enableDebugLog)
            {
                Debug.Log($"OrderCompleteAndLoadSceneButton: 已将订单 taskId={orderInfo.taskId} 添加到待播放动画列表");
            }
        }
        else
        {
            pendingCompletedSphereNames.Add(orderInfo.sphereName);
            if (enableDebugLog)
            {
                Debug.Log($"OrderCompleteAndLoadSceneButton: 已将订单 sphereName={orderInfo.sphereName} 添加到待播放动画列表（无taskId）");
            }
        }
        
        // 如果订单有 taskId 且配置了 TaskManager，通过 TaskManager 完成任务（会自动持久化）
        if (orderInfo.taskId >= 0 && taskManager != null)
        {
            taskManager.CompleteTask(orderInfo.taskId);
            
            if (enableDebugLog)
            {
                Debug.Log($"OrderCompleteAndLoadSceneButton: 已通过 TaskManager 完成任务 {orderInfo.taskId}（已持久化），准备卸载游戏场景并返回MainHub");
            }
        }
        // 如果订单没有 taskId，直接保存到 PlayerPrefs（如果启用持久化）
        else if (orderInfo.taskId < 0 && usePersistentStorage)
        {
            string key = ORDER_COMPLETED_KEY_PREFIX + orderInfo.sphereName + "_" + selectedOrderIndex;
            PlayerPrefs.SetInt(key, 1);
            PlayerPrefs.Save();
            
            if (enableDebugLog)
            {
                Debug.Log($"OrderCompleteAndLoadSceneButton: 订单 {orderInfo.sphereName} 没有 taskId，已直接保存到 PlayerPrefs (键: {key})，准备卸载游戏场景并返回MainHub");
            }
        }
        else if (orderInfo.taskId >= 0 && taskManager == null)
        {
            if (enableDebugLog)
            {
                Debug.LogWarning($"OrderCompleteAndLoadSceneButton: 订单 {orderInfo.sphereName} 有 taskId ({orderInfo.taskId})，但未配置 TaskManager，任务状态可能不会持久化。");
            }
        }
        
        // 卸载游戏场景（返回MainHub）
        UnloadGameScene();
    }
    
    /// <summary>
    /// 检查指定 taskId 是否在待播放动画列表中（供 TaskCompletionHandler 调用）
    /// </summary>
    /// <param name="taskId">任务ID</param>
    /// <returns>如果在列表中返回true，否则返回false</returns>
    public static bool IsPendingForAnimation(int taskId)
    {
        return pendingCompletedTaskIds.Contains(taskId);
    }
    
    /// <summary>
    /// 检查指定 sphereName 是否在待播放动画列表中（供 TaskCompletionHandler 调用）
    /// </summary>
    /// <param name="sphereName">Sphere名称</param>
    /// <returns>如果在列表中返回true，否则返回false</returns>
    public static bool IsPendingForAnimation(string sphereName)
    {
        return pendingCompletedSphereNames.Contains(sphereName);
    }
    
    /// <summary>
    /// 从待播放动画列表中移除指定 taskId（供 TaskCompletionHandler 调用）
    /// </summary>
    /// <param name="taskId">任务ID</param>
    public static void RemovePendingTaskId(int taskId)
    {
        pendingCompletedTaskIds.Remove(taskId);
    }
    
    /// <summary>
    /// 从待播放动画列表中移除指定 sphereName（供 TaskCompletionHandler 调用）
    /// </summary>
    /// <param name="sphereName">Sphere名称</param>
    public static void RemovePendingSphereName(string sphereName)
    {
        pendingCompletedSphereNames.Remove(sphereName);
    }
    
    /// <summary>
    /// 清除所有待播放动画的订单（用于清理，避免残留数据）
    /// </summary>
    public static void ClearPendingOrders()
    {
        pendingCompletedTaskIds.Clear();
        pendingCompletedSphereNames.Clear();
    }
    
    /// <summary>
    /// 卸载游戏场景（返回MainHub）
    /// </summary>
    private void UnloadGameScene()
    {
        // 恢复时间，避免场景切换时时间仍为0
        Time.timeScale = 1f;
        
        // 使用 SceneTransitionManager 卸载游戏场景
        if (SceneTransitionManager.Instance != null)
        {
            if (enableDebugLog)
            {
                Debug.Log("OrderCompleteAndLoadSceneButton: 正在通过 SceneTransitionManager 卸载游戏场景并返回MainHub");
            }
            
            SceneTransitionManager.Instance.UnloadGameScene();
        }
        else
        {
            Debug.LogError("OrderCompleteAndLoadSceneButton: SceneTransitionManager 未找到！无法卸载游戏场景。");
        }
    }
    
    /// <summary>
    /// 手动设置选中的订单索引（可在运行时调用）
    /// </summary>
    /// <param name="index">订单索引</param>
    public void SetSelectedOrderIndex(int index)
    {
        if (orderDataConfig == null || orderDataConfig.orderDataList == null)
        {
            Debug.LogWarning("OrderCompleteAndLoadSceneButton: Order Data Config 未配置，无法设置订单索引！");
            return;
        }
        
        if (index < 0 || index >= orderDataConfig.orderDataList.Count)
        {
            Debug.LogWarning($"OrderCompleteAndLoadSceneButton: 索引 {index} 超出范围（有效范围: 0-{orderDataConfig.orderDataList.Count - 1}）！");
            return;
        }
        
        selectedOrderIndex = index;
        
        if (enableDebugLog)
        {
            var orderInfo = orderDataConfig.orderDataList[index];
            Debug.Log($"OrderCompleteAndLoadSceneButton: 已设置选中订单索引为 {index} ({orderInfo?.sphereName ?? "未知"})");
        }
    }
    
    /// <summary>
    /// 根据 Sphere 名称设置选中的订单索引（可在运行时调用）
    /// </summary>
    /// <param name="sphereName">Sphere 名称</param>
    public void SetSelectedOrderBySphereName(string sphereName)
    {
        if (orderDataConfig == null || orderDataConfig.orderDataList == null)
        {
            Debug.LogWarning("OrderCompleteAndLoadSceneButton: Order Data Config 未配置，无法设置订单！");
            return;
        }
        
        for (int i = 0; i < orderDataConfig.orderDataList.Count; i++)
        {
            var orderInfo = orderDataConfig.orderDataList[i];
            if (orderInfo != null && orderInfo.sphereName == sphereName)
            {
                selectedOrderIndex = i;
                
                if (enableDebugLog)
                {
                    Debug.Log($"OrderCompleteAndLoadSceneButton: 已根据 Sphere 名称 {sphereName} 设置选中订单索引为 {i}");
                }
                return;
            }
        }
        
        Debug.LogWarning($"OrderCompleteAndLoadSceneButton: 未找到 Sphere 名称为 {sphereName} 的订单！");
    }
    
    // 注意：已移除 SetTargetSceneName 和 GetTargetSceneName 方法，因为卸载场景不需要指定场景名称
}
