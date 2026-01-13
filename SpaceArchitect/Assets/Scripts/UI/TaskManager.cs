using UnityEngine;
using System;
using System.Collections.Generic;

/// <summary>
/// 任务管理器
/// 管理任务状态和完成事件
/// </summary>
public class TaskManager : MonoBehaviour
{
    [Header("数据配置")]
    [SerializeField] private SphereOrderDataConfig orderDataConfig;
    
    [Header("持久化设置")]
    [Tooltip("是否使用PlayerPrefs保存任务完成状态（跨游戏会话持久化，重启游戏后仍然有效）")]
    [SerializeField] private bool usePersistentStorage = false;
    
    [Header("重置设置")]
    [Tooltip("游戏启动时是否重置所有订单状态（每次重新运行游戏时，所有订单都会重置为未完成状态。场景切换时不会重置）")]
    [SerializeField] private bool resetOnGameStart = true;
    
    [Header("调试")]
    [SerializeField] private bool enableDebugLog = true;
    
    // PlayerPrefs键名前缀
    private const string TASK_COMPLETED_KEY_PREFIX = "TaskCompleted_";
    
    // 运行时任务完成状态（不存储在ScriptableObject中）
    private Dictionary<int, bool> taskCompletionStatus = new Dictionary<int, bool>();
    
    // 静态标志：是否已经初始化过（用于区分游戏启动和场景切换）
    private static bool hasInitialized = false;
    
    // 静态字典：跨场景保持任务完成状态（用于场景切换时保持状态）
    private static Dictionary<int, bool> persistentTaskStatus = new Dictionary<int, bool>();
    
    // 事件：任务完成
    public event Action<int> OnTaskCompleted;
    
    void Start()
    {
        // 初始化所有任务的完成状态
        if (orderDataConfig != null)
        {
            var allTasks = orderDataConfig.GetAllTaskOrders();
            
            // 判断是否是游戏启动（第一次初始化）
            bool isGameStart = !hasInitialized;
            
            if (isGameStart)
            {
                // 游戏启动时，根据resetOnGameStart决定是否重置
                if (resetOnGameStart)
                {
                    ResetAllOrderStates(allTasks);
                    // 初始化所有任务为未完成
                    persistentTaskStatus.Clear(); // 清除静态持久化状态
                    foreach (var task in allTasks)
                    {
                        taskCompletionStatus[task.taskId] = false;
                        persistentTaskStatus[task.taskId] = false;
                    }
                    
                    if (enableDebugLog)
                    {
                        Debug.Log($"TaskManager: 游戏启动，已重置 {allTasks.Count} 个订单状态为未完成");
                    }
                }
                else if (usePersistentStorage)
                {
                    // 从PlayerPrefs加载已完成的任务状态
                    LoadTaskCompletionStatus(allTasks);
                    // 同步到静态持久化状态
                    foreach (var kvp in taskCompletionStatus)
                    {
                        persistentTaskStatus[kvp.Key] = kvp.Value;
                    }
                    
                    if (enableDebugLog)
                    {
                        int completedCount = 0;
                        foreach (var status in taskCompletionStatus.Values)
                        {
                            if (status) completedCount++;
                        }
                        Debug.Log($"TaskManager: 游戏启动，从持久化存储加载 {allTasks.Count} 个任务，其中 {completedCount} 个已完成");
                    }
                }
                else
                {
                    // 不使用持久化，所有任务初始化为未完成
                    persistentTaskStatus.Clear();
                    foreach (var task in allTasks)
                    {
                        taskCompletionStatus[task.taskId] = false;
                        persistentTaskStatus[task.taskId] = false;
                    }
                    
                    if (enableDebugLog)
                    {
                        Debug.Log($"TaskManager: 游戏启动，已初始化 {allTasks.Count} 个任务为未完成");
                    }
                }
                
                // 标记为已初始化
                hasInitialized = true;
            }
            else
            {
                // 场景切换时，从静态持久化状态恢复任务状态
                foreach (var task in allTasks)
                {
                    // 从静态持久化状态恢复
                    if (persistentTaskStatus.ContainsKey(task.taskId))
                    {
                        taskCompletionStatus[task.taskId] = persistentTaskStatus[task.taskId];
                    }
                    else
                    {
                        taskCompletionStatus[task.taskId] = false;
                        persistentTaskStatus[task.taskId] = false;
                    }
                    
                    // 同步CompleteOrder状态（根据taskCompletionStatus）
                    // 这样即使ScriptableObject被重置，也能保持正确的状态
                    bool isCompleted = taskCompletionStatus[task.taskId];
                    if (task.CompleteOrder != isCompleted)
                    {
                        task.CompleteOrder = isCompleted;
                        if (enableDebugLog)
                        {
                            Debug.Log($"TaskManager: 场景切换，从持久化状态恢复订单 {task.sphereName} (taskId={task.taskId}) 的CompleteOrder为{isCompleted}");
                        }
                    }
                }
                
                if (enableDebugLog)
                {
                    int completedCount = 0;
                    foreach (var status in taskCompletionStatus.Values)
                    {
                        if (status) completedCount++;
                    }
                    Debug.Log($"TaskManager: 场景切换，从持久化状态恢复，当前 {allTasks.Count} 个任务中 {completedCount} 个已完成");
                }
            }
        }
        else
        {
            Debug.LogWarning("TaskManager: orderDataConfig未配置！");
        }
    }
    
    /// <summary>
    /// 重置所有订单状态（CompleteOrder和VisitOrder）
    /// </summary>
    private void ResetAllOrderStates(List<SphereOrderDataConfig.SphereOrderInfo> allTasks)
    {
        foreach (var task in allTasks)
        {
            // 重置CompleteOrder
            task.CompleteOrder = false;
            
            // 可选：也重置VisitOrder（如果需要的话）
            // task.VisitOrder = false;
        }
        
        if (enableDebugLog)
        {
            Debug.Log($"TaskManager: 已重置 {allTasks.Count} 个订单的CompleteOrder状态为false");
        }
    }
    
    /// <summary>
    /// 从PlayerPrefs加载任务完成状态
    /// </summary>
    private void LoadTaskCompletionStatus(List<SphereOrderDataConfig.SphereOrderInfo> allTasks)
    {
        foreach (var task in allTasks)
        {
            string key = TASK_COMPLETED_KEY_PREFIX + task.taskId;
            bool isCompleted = PlayerPrefs.GetInt(key, 0) == 1;
            taskCompletionStatus[task.taskId] = isCompleted;
            
            if (enableDebugLog && isCompleted)
            {
                Debug.Log($"TaskManager: 从持久化存储加载任务 {task.taskId} (Sphere: {task.sphereName}) 的完成状态：已完成");
            }
        }
    }
    
    /// <summary>
    /// 完成任务
    /// </summary>
    /// <param name="taskId">任务ID</param>
    public void CompleteTask(int taskId)
    {
        if (!taskCompletionStatus.ContainsKey(taskId))
        {
            Debug.LogWarning($"TaskManager: taskId {taskId} 不存在！");
            return;
        }
        
        if (taskCompletionStatus[taskId])
        {
            if (enableDebugLog)
            {
                Debug.LogWarning($"TaskManager: 任务 {taskId} 已经完成！");
            }
            return;
        }
        
        // 标记任务为已完成
        taskCompletionStatus[taskId] = true;
        persistentTaskStatus[taskId] = true; // 同步到静态持久化状态
        
        // 同步更新订单的CompleteOrder状态
        if (orderDataConfig != null)
        {
            var orderInfo = orderDataConfig.GetOrderInfoByTaskId(taskId);
            if (orderInfo != null)
            {
                orderInfo.CompleteOrder = true;
            }
        }
        
        // 保存到PlayerPrefs（如果启用持久化）
        if (usePersistentStorage)
        {
            string key = TASK_COMPLETED_KEY_PREFIX + taskId;
            PlayerPrefs.SetInt(key, 1);
            PlayerPrefs.Save(); // 立即保存到磁盘
        }
        
        if (enableDebugLog)
        {
            Debug.Log($"TaskManager: 任务 {taskId} 已完成" + (usePersistentStorage ? "（已保存到持久化存储）" : ""));
        }
        
        // 触发任务完成事件
        OnTaskCompleted?.Invoke(taskId);
    }
    
    /// <summary>
    /// 检查任务是否完成
    /// </summary>
    /// <param name="taskId">任务ID</param>
    /// <returns>是否已完成</returns>
    public bool IsTaskCompleted(int taskId)
    {
        return taskCompletionStatus.ContainsKey(taskId) && taskCompletionStatus[taskId];
    }
    
    /// <summary>
    /// 根据任务ID获取关联的Sphere名称
    /// </summary>
    /// <param name="taskId">任务ID</param>
    /// <returns>Sphere名称，如果不存在返回null</returns>
    public string GetSphereNameByTaskId(int taskId)
    {
        if (orderDataConfig == null)
        {
            return null;
        }
        
        var orderInfo = orderDataConfig.GetOrderInfoByTaskId(taskId);
        return orderInfo?.sphereName;
    }
    
    /// <summary>
    /// 重置所有任务状态（用于测试或重新开始）
    /// </summary>
    public void ResetAllTasks()
    {
        var keys = new List<int>(taskCompletionStatus.Keys);
        foreach (var taskId in keys)
        {
            taskCompletionStatus[taskId] = false;
            
            // 清除PlayerPrefs中的记录（如果启用持久化）
            if (usePersistentStorage)
            {
                string key = TASK_COMPLETED_KEY_PREFIX + taskId;
                PlayerPrefs.DeleteKey(key);
            }
        }
        
        // 重置所有订单的CompleteOrder状态
        if (orderDataConfig != null)
        {
            var allTasks = orderDataConfig.GetAllTaskOrders();
            ResetAllOrderStates(allTasks);
        }
        
        if (usePersistentStorage)
        {
            PlayerPrefs.Save(); // 立即保存到磁盘
        }
        
        if (enableDebugLog)
        {
            Debug.Log("TaskManager: 所有任务状态已重置" + (usePersistentStorage ? "（已清除持久化存储）" : ""));
        }
    }
    
    /// <summary>
    /// 重置初始化标志（用于测试，让系统认为这是游戏启动）
    /// </summary>
    public static void ResetInitializationFlag()
    {
        hasInitialized = false;
    }
}
