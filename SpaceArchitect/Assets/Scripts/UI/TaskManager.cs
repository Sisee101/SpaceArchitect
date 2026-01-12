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
    
    [Header("调试")]
    [SerializeField] private bool enableDebugLog = true;
    
    // 运行时任务完成状态（不存储在ScriptableObject中）
    private Dictionary<int, bool> taskCompletionStatus = new Dictionary<int, bool>();
    
    // 事件：任务完成
    public event Action<int> OnTaskCompleted;
    
    void Start()
    {
        // 初始化所有任务的完成状态为false
        if (orderDataConfig != null)
        {
            var allTasks = orderDataConfig.GetAllTaskOrders();
            foreach (var task in allTasks)
            {
                taskCompletionStatus[task.taskId] = false;
            }
            
            if (enableDebugLog)
            {
                Debug.Log($"TaskManager: 已初始化 {allTasks.Count} 个任务");
            }
        }
        else
        {
            Debug.LogWarning("TaskManager: orderDataConfig未配置！");
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
        
        if (enableDebugLog)
        {
            Debug.Log($"TaskManager: 任务 {taskId} 已完成");
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
        }
        
        if (enableDebugLog)
        {
            Debug.Log("TaskManager: 所有任务状态已重置");
        }
    }
}
