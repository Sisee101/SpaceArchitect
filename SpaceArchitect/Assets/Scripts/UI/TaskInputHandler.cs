using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// 任务输入处理器
/// 检测键盘输入（1, 2, 3...），触发对应的任务完成信号
/// </summary>
public class TaskInputHandler : MonoBehaviour
{
    [Header("数据配置")]
    [SerializeField] private SphereOrderDataConfig orderDataConfig;
    
    [Header("引用")]
    [SerializeField] private TaskManager taskManager;
    
    [Header("调试")]
    [SerializeField] private bool enableDebugLog = true;
    
    // 缓存所有任务列表（按配置顺序）
    private List<SphereOrderDataConfig.SphereOrderInfo> allTasks = new List<SphereOrderDataConfig.SphereOrderInfo>();
    
    void Start()
    {
        // 验证引用
        if (orderDataConfig == null)
        {
            Debug.LogError("TaskInputHandler: orderDataConfig未配置！");
            return;
        }
        
        if (taskManager == null)
        {
            Debug.LogError("TaskInputHandler: taskManager未配置！");
            return;
        }
        
        // 获取所有已配置任务的列表
        RefreshTaskList();
    }
    
    void Update()
    {
        // 检测键盘输入 1-9
        for (int i = 1; i <= 9; i++)
        {
            if (Input.GetKeyDown(KeyCode.Alpha0 + i)) // Alpha1 = 1, Alpha2 = 2, ...
            {
                CompleteTaskByIndex(i - 1); // 索引从0开始，所以减1
                break; // 一次只处理一个按键
            }
        }
    }
    
    /// <summary>
    /// 刷新任务列表（当数据配置改变时调用）
    /// </summary>
    public void RefreshTaskList()
    {
        if (orderDataConfig == null)
        {
            allTasks.Clear();
            return;
        }
        
        allTasks = orderDataConfig.GetAllTaskOrders();
        
        if (enableDebugLog)
        {
            Debug.Log($"TaskInputHandler: 已加载 {allTasks.Count} 个任务");
            for (int i = 0; i < allTasks.Count; i++)
            {
                Debug.Log($"  任务 {i + 1} (键盘{i + 1}): taskId={allTasks[i].taskId}, Sphere={allTasks[i].sphereName}");
            }
        }
    }
    
    /// <summary>
    /// 根据索引完成任务（键盘输入作为索引）
    /// </summary>
    /// <param name="index">任务索引（0-based）</param>
    private void CompleteTaskByIndex(int index)
    {
        if (index < 0 || index >= allTasks.Count)
        {
            if (enableDebugLog)
            {
                Debug.LogWarning($"TaskInputHandler: 索引 {index} 超出范围（共 {allTasks.Count} 个任务）");
            }
            return;
        }
        
        SphereOrderDataConfig.SphereOrderInfo taskInfo = allTasks[index];
        
        if (taskInfo == null)
        {
            Debug.LogError($"TaskInputHandler: 索引 {index} 的任务信息为空！");
            return;
        }
        
        int taskId = taskInfo.taskId;
        
        if (enableDebugLog)
        {
            Debug.Log($"TaskInputHandler: 键盘 {index + 1} 按下，完成任务 taskId={taskId}, Sphere={taskInfo.sphereName}");
        }
        
        // 通过TaskManager完成任务
        if (taskManager != null)
        {
            taskManager.CompleteTask(taskId);
        }
        else
        {
            Debug.LogError("TaskInputHandler: taskManager未配置！");
        }
    }
    
    /// <summary>
    /// 根据taskId直接完成任务（备用方法）
    /// </summary>
    /// <param name="taskId">任务ID</param>
    public void CompleteTaskById(int taskId)
    {
        if (taskManager != null)
        {
            taskManager.CompleteTask(taskId);
        }
    }
}
