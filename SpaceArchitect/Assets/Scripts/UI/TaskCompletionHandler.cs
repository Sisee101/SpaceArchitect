using UnityEngine;
using System.Collections;

/// <summary>
/// 任务完成处理器
/// 监听任务完成事件，协调胜利反馈显示和气泡消失动画
/// 执行顺序：显示胜利图片+印章 → 图片和印章消失 → 气泡消失动画
/// </summary>
public class TaskCompletionHandler : MonoBehaviour
{
    [Header("引用")]
    [SerializeField] private TaskManager taskManager;
    [SerializeField] private SphereIconManager iconManager;
    [Tooltip("胜利反馈显示组件（实现IVictoryFeedbackDisplay接口，如VictoryImageDisplay）")]
    [SerializeField] private MonoBehaviour victoryFeedbackDisplayMono; // Unity Inspector不支持接口，使用MonoBehaviour
    [SerializeField] private SphereOrderDataConfig orderDataConfig;
    
    // 运行时转换为接口
    private IVictoryFeedbackDisplay victoryFeedbackDisplay;
    
    [Header("检测设置")]
    [Tooltip("检测CompleteOrder变化的轮询间隔（秒）。如果为0，则每帧检测。")]
    [SerializeField] private float checkInterval = 0.1f; // 检测间隔
    
    [Header("执行顺序")]
    [Tooltip("视频消失后，气泡动画开始前的延迟时间（秒）")]
    [SerializeField] private float bubbleAnimationDelay = 0f;
    
    [Header("调试")]
    [SerializeField] private bool enableDebugLog = true;
    
    // 记录已处理的订单（避免重复处理）
    private System.Collections.Generic.HashSet<int> processedTaskIds = new System.Collections.Generic.HashSet<int>();
    
    // 上次检测时间
    private float lastCheckTime = 0f;
    
    void Start()
    {
        // 验证引用
        if (taskManager == null)
        {
            Debug.LogError("TaskCompletionHandler: taskManager未配置！");
            return;
        }
        
        if (iconManager == null)
        {
            Debug.LogError("TaskCompletionHandler: iconManager未配置！");
            return;
        }
        
        if (orderDataConfig == null)
        {
            Debug.LogError("TaskCompletionHandler: orderDataConfig未配置！");
            return;
        }
        
        // 将MonoBehaviour转换为接口
        if (victoryFeedbackDisplayMono != null)
        {
            victoryFeedbackDisplay = victoryFeedbackDisplayMono as IVictoryFeedbackDisplay;
            if (victoryFeedbackDisplay == null)
            {
                Debug.LogError("TaskCompletionHandler: victoryFeedbackDisplayMono 未实现 IVictoryFeedbackDisplay 接口！请在Inspector中配置 VictoryImageDisplay 组件。");
            }
        }
        else
        {
            if (enableDebugLog)
            {
                Debug.LogWarning("TaskCompletionHandler: victoryFeedbackDisplayMono未配置！任务完成时将不会显示胜利反馈。");
            }
        }
        
        // 场景加载时，将已经完成的订单标记为已处理（避免场景切换时重复播放动画）
        processedTaskIds.Clear();
        if (orderDataConfig != null && orderDataConfig.orderDataList != null)
        {
            foreach (var orderInfo in orderDataConfig.orderDataList)
            {
                if (orderInfo != null && orderInfo.taskId >= 0 && orderInfo.CompleteOrder)
                {
                    processedTaskIds.Add(orderInfo.taskId);
                    if (enableDebugLog)
                    {
                        Debug.Log($"TaskCompletionHandler: 场景加载，订单 {orderInfo.sphereName} (taskId={orderInfo.taskId}) 已完成，已标记为已处理（不会播放动画）");
                    }
                }
            }
        }
        
        // 订阅任务完成事件（作为备用触发方式）
        taskManager.OnTaskCompleted += OnTaskCompleted;
        
        if (enableDebugLog)
        {
            Debug.Log($"TaskCompletionHandler: 已订阅任务完成事件，并开始检测CompleteOrder变化（已标记 {processedTaskIds.Count} 个已完成订单为已处理）");
        }
    }
    
    void Update()
    {
        // 检测CompleteOrder的变化
        if (orderDataConfig != null)
        {
            // 根据checkInterval决定检测频率
            if (checkInterval <= 0 || Time.time - lastCheckTime >= checkInterval)
            {
                CheckCompleteOrderChanges();
                lastCheckTime = Time.time;
            }
        }
    }
    
    /// <summary>
    /// 检测CompleteOrder的变化
    /// </summary>
    private void CheckCompleteOrderChanges()
    {
        if (orderDataConfig == null || orderDataConfig.orderDataList == null)
        {
            return;
        }
        
        // 遍历所有订单，检查CompleteOrder是否变为true
        foreach (var orderInfo in orderDataConfig.orderDataList)
        {
            if (orderInfo == null)
            {
                continue;
            }
            
            // 只处理有taskId的订单（taskId >= 0）
            if (orderInfo.taskId < 0)
            {
                continue;
            }
            
            // 如果CompleteOrder为true且尚未处理
            if (orderInfo.CompleteOrder && !processedTaskIds.Contains(orderInfo.taskId))
            {
                if (enableDebugLog)
                {
                    Debug.Log($"TaskCompletionHandler: 检测到订单 {orderInfo.sphereName} (taskId={orderInfo.taskId}) 的CompleteOrder变为true，开始处理");
                }
                
                // 标记为已处理
                processedTaskIds.Add(orderInfo.taskId);
                
                // 处理任务完成序列
                StartCoroutine(HandleTaskCompletionSequence(orderInfo.sphereName));
            }
        }
    }
    
    void OnDestroy()
    {
        // 取消订阅
        if (taskManager != null)
        {
            taskManager.OnTaskCompleted -= OnTaskCompleted;
        }
        
        if (victoryFeedbackDisplay != null)
        {
            victoryFeedbackDisplay.OnHidden -= OnVideoHidden;
        }
    }
    
    /// <summary>
    /// 任务完成事件处理
    /// 执行顺序：显示胜利图片+印章 → 图片和印章消失 → 气泡消失动画
    /// </summary>
    /// <param name="taskId">任务ID</param>
    private void OnTaskCompleted(int taskId)
    {
        if (enableDebugLog)
        {
            Debug.Log($"TaskCompletionHandler: 收到任务完成事件，taskId={taskId}");
        }
        
        // 根据任务ID获取订单信息
        var orderInfo = orderDataConfig.GetOrderInfoByTaskId(taskId);
        
        if (orderInfo == null)
        {
            Debug.LogWarning($"TaskCompletionHandler: 任务 {taskId} 没有关联的订单信息！");
            return;
        }
        
        string sphereName = orderInfo.sphereName;
        
        if (string.IsNullOrEmpty(sphereName))
        {
            Debug.LogWarning($"TaskCompletionHandler: 任务 {taskId} 没有关联的Sphere名称！");
            return;
        }
        
        if (enableDebugLog)
        {
            Debug.Log($"TaskCompletionHandler: 开始处理任务完成，Sphere={sphereName}");
        }
        
        // 执行流程：显示图片+印章，然后等待消失后播放气泡消失动画
        StartCoroutine(HandleTaskCompletionSequence(sphereName));
    }
    
    /// <summary>
    /// 处理任务完成序列：显示胜利图片+印章 → 图片和印章消失 → 气泡消失动画
    /// </summary>
    /// <param name="sphereName">Sphere名称</param>
    private IEnumerator HandleTaskCompletionSequence(string sphereName)
    {
        if (enableDebugLog)
        {
            Debug.Log($"TaskCompletionHandler: ====== 开始处理任务完成序列，Sphere={sphereName} ======");
        }
        
        // 获取订单信息
        var orderInfo = orderDataConfig.GetOrderInfoBySphereName(sphereName);
        if (orderInfo == null)
        {
            Debug.LogWarning($"TaskCompletionHandler: 未找到 {sphereName} 的订单信息！");
            yield break;
        }
        
        // 步骤1：显示胜利图片+印章（优先使用图片）
        if (orderInfo.orderImage != null && victoryFeedbackDisplay != null)
        {
            if (enableDebugLog)
            {
                Debug.Log($"TaskCompletionHandler: 开始显示胜利图片 {orderInfo.orderImage.name}");
            }
            
            bool imageHidden = false;
            
            // 显示图片和印章（印章由组件内部处理）
            victoryFeedbackDisplay.ShowImage(
                orderInfo.orderImage, 
                () => {
                    imageHidden = true;
                    if (enableDebugLog)
                    {
                        Debug.Log("TaskCompletionHandler: 图片和印章已消失，准备播放气泡消失动画");
                    }
                }
            );
            
            // 等待图片和印章消失
            while (!imageHidden)
            {
                yield return null;
            }
            
            if (enableDebugLog)
            {
                Debug.Log("TaskCompletionHandler: 图片显示序列已结束，准备播放气泡消失动画");
            }
        }
        else if (orderInfo.victoryVideoClip != null && victoryFeedbackDisplay != null)
        {
            // 向后兼容：如果配置了视频，也可以播放
            if (enableDebugLog)
            {
                Debug.Log($"TaskCompletionHandler: 开始播放胜利视频 {orderInfo.victoryVideoClip.name}");
            }
            
            bool videoHidden = false;
            victoryFeedbackDisplay.ShowVideo(orderInfo.victoryVideoClip, () => {
                videoHidden = true;
                if (enableDebugLog)
                {
                    Debug.Log("TaskCompletionHandler: 视频已隐藏，准备播放气泡消失动画");
                }
            });
            
            while (!videoHidden)
            {
                yield return null;
            }
        }
        else
        {
            // 如果没有配置任何反馈，直接触发气泡消失
            if (enableDebugLog)
            {
                if (orderInfo.orderImage == null && orderInfo.victoryVideoClip == null)
                {
                    Debug.LogWarning($"TaskCompletionHandler: 订单 {sphereName} 没有配置胜利反馈（orderImage或victoryVideoClip），直接播放气泡消失动画");
                }
                else if (victoryFeedbackDisplay == null)
                {
                    Debug.LogWarning($"TaskCompletionHandler: victoryFeedbackDisplay未配置，跳过显示，直接播放气泡消失动画");
                }
            }
        }
        
        // 步骤2：等待延迟时间（如果有）
        if (bubbleAnimationDelay > 0f)
        {
            if (enableDebugLog)
            {
                Debug.Log($"TaskCompletionHandler: 等待 {bubbleAnimationDelay} 秒后播放气泡消失动画");
            }
            yield return new WaitForSeconds(bubbleAnimationDelay);
        }
        
        // 步骤3：播放气泡消失动画（保持和之前不变）
        if (iconManager != null)
        {
            if (enableDebugLog)
            {
                Debug.Log($"TaskCompletionHandler: 开始播放气泡消失动画，Sphere={sphereName}");
            }
            
            iconManager.HideIconForSphere(sphereName, () => {
                if (enableDebugLog)
                {
                    Debug.Log($"TaskCompletionHandler: 气泡消失动画完成，Sphere={sphereName}");
                }
            });
        }
        else
        {
            Debug.LogError("TaskCompletionHandler: iconManager未配置！无法播放气泡消失动画");
        }
        
        if (enableDebugLog)
        {
            Debug.Log($"TaskCompletionHandler: ====== 任务完成序列处理结束，Sphere={sphereName} ======");
        }
    }
    
    /// <summary>
    /// 胜利反馈隐藏完成事件处理（当图片/视频完全隐藏后触发）
    /// </summary>
    private void OnVideoHidden()
    {
        // 这个方法现在由 HandleTaskCompletionSequence 中的回调处理
        // 保留这个方法以防其他地方需要直接调用
        if (enableDebugLog)
        {
            Debug.Log("TaskCompletionHandler: 收到胜利反馈隐藏事件");
        }
    }
}
