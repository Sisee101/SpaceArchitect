using UnityEngine;
using System.Collections.Generic;
using System.Reflection;

/// <summary>
/// 动画播放调试助手
/// 用于诊断任务完成后动画不播放的问题
/// 将此脚本挂载到01_MainHub场景中的一个GameObject上
/// </summary>
public class AnimationDebugHelper : MonoBehaviour
{
    [Header("引用")]
    [SerializeField] private SphereOrderDataConfig orderDataConfig;
    [SerializeField] private TaskCompletionHandler taskCompletionHandler;
    
    [Header("调试设置")]
    [SerializeField] private bool autoCheckOnStart = true;
    [SerializeField] private bool continuousCheck = false;
    [SerializeField] private float checkInterval = 2f;
    
    private float lastCheckTime = 0f;
    
    void Start()
    {
        if (autoCheckOnStart)
        {
            // 延迟一帧，等待所有组件初始化
            Invoke("PerformDiagnostics", 0.5f);
        }
    }
    
    void Update()
    {
        if (continuousCheck && Time.time - lastCheckTime >= checkInterval)
        {
            PerformDiagnostics();
            lastCheckTime = Time.time;
        }
    }
    
    /// <summary>
    /// 执行完整诊断
    /// </summary>
    [ContextMenu("执行完整诊断")]
    public void PerformDiagnostics()
    {
        Debug.Log("==================== 动画播放诊断开始 ====================");
        
        CheckOrderCompleteAndLoadSceneButton();
        CheckTaskCompletionHandler();
        CheckOrderDataConfig();
        CheckStaticVariables();
        CheckSceneSetup();
        
        Debug.Log("==================== 动画播放诊断结束 ====================");
    }
    
    /// <summary>
    /// 检查 OrderCompleteAndLoadSceneButton 的静态变量
    /// </summary>
    private void CheckOrderCompleteAndLoadSceneButton()
    {
        Debug.Log(">>> 1. 检查 OrderCompleteAndLoadSceneButton 静态变量");
        
        // 使用反射获取静态变量
        System.Type type = System.Type.GetType("OrderCompleteAndLoadSceneButton");
        if (type == null)
        {
            Debug.LogError("   ❌ 未找到 OrderCompleteAndLoadSceneButton 类！");
            return;
        }
        
        // 获取静态字段
        FieldInfo pendingTaskIdsField = type.GetField("pendingCompletedTaskIds", BindingFlags.NonPublic | BindingFlags.Static);
        FieldInfo pendingSphereNamesField = type.GetField("pendingCompletedSphereNames", BindingFlags.NonPublic | BindingFlags.Static);
        
        if (pendingTaskIdsField != null)
        {
            var pendingTaskIds = pendingTaskIdsField.GetValue(null) as HashSet<int>;
            if (pendingTaskIds != null)
            {
                Debug.Log($"   ✓ pendingCompletedTaskIds 存在，当前包含 {pendingTaskIds.Count} 个任务ID");
                foreach (var taskId in pendingTaskIds)
                {
                    Debug.Log($"     - 待播放动画的任务ID: {taskId}");
                }
            }
            else
            {
                Debug.LogWarning("   ⚠ pendingCompletedTaskIds 为 null");
            }
        }
        else
        {
            Debug.LogError("   ❌ 无法获取 pendingCompletedTaskIds 字段！");
        }
        
        if (pendingSphereNamesField != null)
        {
            var pendingSphereNames = pendingSphereNamesField.GetValue(null) as HashSet<string>;
            if (pendingSphereNames != null)
            {
                Debug.Log($"   ✓ pendingCompletedSphereNames 存在，当前包含 {pendingSphereNames.Count} 个Sphere名称");
                foreach (var sphereName in pendingSphereNames)
                {
                    Debug.Log($"     - 待播放动画的Sphere名称: {sphereName}");
                }
            }
            else
            {
                Debug.LogWarning("   ⚠ pendingCompletedSphereNames 为 null");
            }
        }
        else
        {
            Debug.LogError("   ❌ 无法获取 pendingCompletedSphereNames 字段！");
        }
    }
    
    /// <summary>
    /// 检查 TaskCompletionHandler 配置
    /// </summary>
    private void CheckTaskCompletionHandler()
    {
        Debug.Log(">>> 2. 检查 TaskCompletionHandler 配置");
        
        if (taskCompletionHandler == null)
        {
            taskCompletionHandler = FindObjectOfType<TaskCompletionHandler>();
        }
        
        if (taskCompletionHandler == null)
        {
            Debug.LogError("   ❌ 场景中未找到 TaskCompletionHandler！");
            Debug.LogError("   解决方案：请在01_MainHub场景中添加 TaskCompletionHandler 组件");
            return;
        }
        
        Debug.Log($"   ✓ 找到 TaskCompletionHandler: {taskCompletionHandler.gameObject.name}");
        
        // 使用反射检查私有字段
        System.Type type = taskCompletionHandler.GetType();
        
        // 检查 taskManager
        var taskManagerField = type.GetField("taskManager", BindingFlags.NonPublic | BindingFlags.Instance);
        if (taskManagerField != null)
        {
            var taskManager = taskManagerField.GetValue(taskCompletionHandler);
            if (taskManager != null)
            {
                Debug.Log($"   ✓ taskManager 已配置: {(taskManager as MonoBehaviour)?.gameObject.name}");
            }
            else
            {
                Debug.LogError("   ❌ taskManager 未配置！");
            }
        }
        
        // 检查 iconManager
        var iconManagerField = type.GetField("iconManager", BindingFlags.NonPublic | BindingFlags.Instance);
        if (iconManagerField != null)
        {
            var iconManager = iconManagerField.GetValue(taskCompletionHandler);
            if (iconManager != null)
            {
                Debug.Log($"   ✓ iconManager 已配置: {(iconManager as MonoBehaviour)?.gameObject.name}");
            }
            else
            {
                Debug.LogError("   ❌ iconManager 未配置！");
            }
        }
        
        // 检查 victoryFeedbackDisplayMono
        var victoryFeedbackField = type.GetField("victoryFeedbackDisplayMono", BindingFlags.NonPublic | BindingFlags.Instance);
        if (victoryFeedbackField != null)
        {
            var victoryFeedback = victoryFeedbackField.GetValue(taskCompletionHandler);
            if (victoryFeedback != null)
            {
                Debug.Log($"   ✓ victoryFeedbackDisplayMono 已配置: {(victoryFeedback as MonoBehaviour)?.gameObject.name}");
            }
            else
            {
                Debug.LogWarning("   ⚠ victoryFeedbackDisplayMono 未配置（可选）");
            }
        }
        
        // 检查 orderDataConfig
        var orderDataConfigField = type.GetField("orderDataConfig", BindingFlags.NonPublic | BindingFlags.Instance);
        if (orderDataConfigField != null)
        {
            var config = orderDataConfigField.GetValue(taskCompletionHandler);
            if (config != null)
            {
                Debug.Log($"   ✓ orderDataConfig 已配置");
            }
            else
            {
                Debug.LogError("   ❌ orderDataConfig 未配置！");
            }
        }
        
        // 检查 enableDebugLog
        var enableDebugLogField = type.GetField("enableDebugLog", BindingFlags.NonPublic | BindingFlags.Instance);
        if (enableDebugLogField != null)
        {
            bool enableDebugLog = (bool)enableDebugLogField.GetValue(taskCompletionHandler);
            if (enableDebugLog)
            {
                Debug.Log("   ✓ enableDebugLog 已启用");
            }
            else
            {
                Debug.LogWarning("   ⚠ enableDebugLog 未启用，建议启用以便查看详细日志");
            }
        }
        
        // 检查静态变量 globallyProcessedTaskIds
        var globallyProcessedField = type.GetField("globallyProcessedTaskIds", BindingFlags.NonPublic | BindingFlags.Static);
        if (globallyProcessedField != null)
        {
            var globallyProcessed = globallyProcessedField.GetValue(null) as HashSet<int>;
            if (globallyProcessed != null)
            {
                Debug.Log($"   ✓ globallyProcessedTaskIds 包含 {globallyProcessed.Count} 个已播放的任务ID");
                foreach (var taskId in globallyProcessed)
                {
                    Debug.Log($"     - 已播放动画的任务ID: {taskId}");
                }
            }
        }
    }
    
    /// <summary>
    /// 检查 OrderDataConfig
    /// </summary>
    private void CheckOrderDataConfig()
    {
        Debug.Log(">>> 3. 检查 OrderDataConfig 配置");
        
        if (orderDataConfig == null)
        {
            Debug.LogError("   ❌ OrderDataConfig 未配置！请在Inspector中设置");
            return;
        }
        
        Debug.Log($"   ✓ OrderDataConfig 已配置: {orderDataConfig.name}");
        
        if (orderDataConfig.orderDataList == null || orderDataConfig.orderDataList.Count == 0)
        {
            Debug.LogError("   ❌ OrderDataConfig.orderDataList 为空！");
            return;
        }
        
        Debug.Log($"   ✓ orderDataList 包含 {orderDataConfig.orderDataList.Count} 个订单");
        
        // 检查每个订单的状态
        for (int i = 0; i < orderDataConfig.orderDataList.Count; i++)
        {
            var orderInfo = orderDataConfig.orderDataList[i];
            if (orderInfo != null)
            {
                string status = orderInfo.CompleteOrder ? "已完成 ✓" : "未完成";
                Debug.Log($"   订单 {i}: {orderInfo.sphereName} (taskId={orderInfo.taskId}) - {status}");
                
                // 检查是否有胜利反馈配置
                if (orderInfo.orderImage != null)
                {
                    Debug.Log($"     - 配置了 orderImage: {orderInfo.orderImage.name}");
                }
                else if (orderInfo.victoryVideoClip != null)
                {
                    Debug.Log($"     - 配置了 victoryVideoClip: {orderInfo.victoryVideoClip.name}");
                }
                else
                {
                    Debug.LogWarning($"     ⚠ 未配置 orderImage 或 victoryVideoClip");
                }
            }
        }
    }
    
    /// <summary>
    /// 检查静态变量状态
    /// </summary>
    private void CheckStaticVariables()
    {
        Debug.Log(">>> 4. 检查静态变量跨场景传递");
        
        // 检查是否有待播放的动画
        bool hasPendingAnimations = false;
        
        System.Type buttonType = System.Type.GetType("OrderCompleteAndLoadSceneButton");
        if (buttonType != null)
        {
            // 调用静态方法检查
            MethodInfo isPendingMethod = buttonType.GetMethod("IsPendingForAnimation", new[] { typeof(int) });
            if (isPendingMethod != null && orderDataConfig != null)
            {
                foreach (var orderInfo in orderDataConfig.orderDataList)
                {
                    if (orderInfo != null && orderInfo.taskId >= 0)
                    {
                        bool isPending = (bool)isPendingMethod.Invoke(null, new object[] { orderInfo.taskId });
                        if (isPending)
                        {
                            hasPendingAnimations = true;
                            Debug.Log($"   ✓ 任务 {orderInfo.taskId} ({orderInfo.sphereName}) 在待播放列表中");
                        }
                    }
                }
            }
        }
        
        if (!hasPendingAnimations)
        {
            Debug.LogWarning("   ⚠ 没有任务在待播放列表中");
            Debug.LogWarning("   可能原因：");
            Debug.LogWarning("   1. 按钮点击时没有正确添加到静态列表");
            Debug.LogWarning("   2. 场景切换过程中静态变量被清空");
            Debug.LogWarning("   3. TaskCompletionHandler.Start() 已经处理并清空了列表");
        }
    }
    
    /// <summary>
    /// 检查场景设置
    /// </summary>
    private void CheckSceneSetup()
    {
        Debug.Log(">>> 5. 检查场景设置");
        
        // 检查 UIManager
        var uiManager = FindObjectOfType<UIManager>();
        if (uiManager != null)
        {
            Debug.Log($"   ✓ 找到 UIManager: {uiManager.gameObject.name}");
        }
        else
        {
            Debug.LogWarning("   ⚠ 未找到 UIManager");
        }
        
        // 检查 SceneTransitionManager
        if (SceneTransitionManager.Instance != null)
        {
            Debug.Log("   ✓ SceneTransitionManager 存在");
        }
        else
        {
            Debug.LogWarning("   ⚠ SceneTransitionManager 不存在");
        }
        
        // 检查当前场景名称
        string sceneName = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
        Debug.Log($"   当前场景: {sceneName}");
        
        if (!sceneName.Contains("MainHub"))
        {
            Debug.LogWarning($"   ⚠ 当前场景 '{sceneName}' 不是 MainHub 场景！");
        }
    }
    
    /// <summary>
    /// 手动模拟完成一个任务（用于测试）
    /// </summary>
    [ContextMenu("模拟完成任务 0")]
    public void SimulateCompleteTask0()
    {
        SimulateCompleteTask(0);
    }
    
    /// <summary>
    /// 手动模拟完成指定任务
    /// </summary>
    public void SimulateCompleteTask(int taskId)
    {
        Debug.Log($"==================== 模拟完成任务 {taskId} ====================");
        
        System.Type buttonType = System.Type.GetType("OrderCompleteAndLoadSceneButton");
        if (buttonType != null)
        {
            // 获取静态字段
            FieldInfo pendingTaskIdsField = buttonType.GetField("pendingCompletedTaskIds", BindingFlags.NonPublic | BindingFlags.Static);
            if (pendingTaskIdsField != null)
            {
                var pendingTaskIds = pendingTaskIdsField.GetValue(null) as HashSet<int>;
                if (pendingTaskIds != null)
                {
                    pendingTaskIds.Add(taskId);
                    Debug.Log($"✓ 已将任务 {taskId} 添加到待播放列表");
                    
                    // 设置 CompleteOrder
                    if (orderDataConfig != null)
                    {
                        var orderInfo = orderDataConfig.GetOrderInfoByTaskId(taskId);
                        if (orderInfo != null)
                        {
                            orderInfo.CompleteOrder = true;
                            Debug.Log($"✓ 已将订单 {orderInfo.sphereName} 的 CompleteOrder 设置为 true");
                            
                            // 触发 TaskCompletionHandler 检查
                            Debug.Log("提示：TaskCompletionHandler 会在下一次 Update 时检测到变化并播放动画");
                        }
                    }
                }
            }
        }
    }
    
    /// <summary>
    /// 清空所有静态变量（用于重置测试）
    /// </summary>
    [ContextMenu("清空所有静态变量")]
    public void ClearAllStaticVariables()
    {
        Debug.Log("==================== 清空所有静态变量 ====================");
        
        System.Type buttonType = System.Type.GetType("OrderCompleteAndLoadSceneButton");
        if (buttonType != null)
        {
            MethodInfo clearMethod = buttonType.GetMethod("ClearPendingOrders");
            if (clearMethod != null)
            {
                clearMethod.Invoke(null, null);
                Debug.Log("✓ 已清空 OrderCompleteAndLoadSceneButton 的待播放列表");
            }
        }
        
        // 清空 TaskCompletionHandler 的静态变量
        System.Type handlerType = System.Type.GetType("TaskCompletionHandler");
        if (handlerType != null)
        {
            FieldInfo globallyProcessedField = handlerType.GetField("globallyProcessedTaskIds", BindingFlags.NonPublic | BindingFlags.Static);
            if (globallyProcessedField != null)
            {
                var globallyProcessed = globallyProcessedField.GetValue(null) as HashSet<int>;
                if (globallyProcessed != null)
                {
                    globallyProcessed.Clear();
                    Debug.Log("✓ 已清空 TaskCompletionHandler 的已播放列表");
                }
            }
        }
        
        Debug.Log("提示：你可以再次运行诊断来确认清空成功");
    }
}

