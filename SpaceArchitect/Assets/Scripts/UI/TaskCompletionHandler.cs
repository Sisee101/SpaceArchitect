using UnityEngine;
using System.Collections;

/// <summary>
/// 任务完成处理器
/// 监听任务完成事件，协调视频播放和气泡消失动画
/// 执行顺序：播放胜利视频 → 视频播放完成 → 气泡消失动画
/// </summary>
public class TaskCompletionHandler : MonoBehaviour
{
    [Header("引用")]
    [SerializeField] private TaskManager taskManager;
    [SerializeField] private SphereIconManager iconManager;
    [SerializeField] private VictoryVideoPlayer videoPlayer;
    [SerializeField] private SphereOrderDataConfig orderDataConfig;
    
    [Header("执行顺序")]
    [Tooltip("视频消失后，气泡动画开始前的延迟时间（秒）")]
    [SerializeField] private float bubbleAnimationDelay = 0f;
    
    [Header("调试")]
    [SerializeField] private bool enableDebugLog = true;
    
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
        
        if (videoPlayer == null)
        {
            Debug.LogError("TaskCompletionHandler: videoPlayer未配置！");
            return;
        }
        
        if (orderDataConfig == null)
        {
            Debug.LogError("TaskCompletionHandler: orderDataConfig未配置！");
            return;
        }
        
        // 订阅任务完成事件
        taskManager.OnTaskCompleted += OnTaskCompleted;
        
        if (enableDebugLog)
        {
            Debug.Log("TaskCompletionHandler: 已订阅任务完成事件");
        }
    }
    
    void OnDestroy()
    {
        // 取消订阅
        if (taskManager != null)
        {
            taskManager.OnTaskCompleted -= OnTaskCompleted;
        }
    }
    
    /// <summary>
    /// 任务完成事件处理
    /// 执行顺序：播放胜利视频 → 视频播放完成 → 气泡消失动画
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
        UnityEngine.Video.VideoClip victoryVideo = orderInfo.victoryVideoClip;
        
        if (string.IsNullOrEmpty(sphereName))
        {
            Debug.LogWarning($"TaskCompletionHandler: 任务 {taskId} 没有关联的Sphere名称！");
            return;
        }
        
        if (enableDebugLog)
        {
            Debug.Log($"TaskCompletionHandler: 开始处理任务完成，Sphere={sphereName}");
        }
        
        // 执行流程：先播放视频，视频完成后播放气泡消失动画
        StartCoroutine(HandleTaskCompletionSequence(sphereName, victoryVideo));
    }
    
    /// <summary>
    /// 处理任务完成序列：播放视频 → 视频播放完成 → 视频消失 → 气泡消失动画
    /// </summary>
    /// <param name="sphereName">Sphere名称</param>
    /// <param name="victoryVideo">胜利视频剪辑</param>
    private IEnumerator HandleTaskCompletionSequence(string sphereName, UnityEngine.Video.VideoClip victoryVideo)
    {
        // 步骤1：播放胜利视频（如果有）
        if (victoryVideo != null)
        {
            if (enableDebugLog)
            {
                Debug.Log($"TaskCompletionHandler: 开始播放胜利视频 {victoryVideo.name}");
            }
            
            bool videoFinished = false;
            bool videoHidden = false;
            
            // 订阅视频隐藏完成事件
            videoPlayer.OnVideoHidden += () => {
                videoHidden = true;
                if (enableDebugLog)
                {
                    Debug.Log("TaskCompletionHandler: 胜利视频已消失");
                }
            };
            
            // 播放视频，设置完成回调
            videoPlayer.PlayVideo(victoryVideo, () => {
                videoFinished = true;
                if (enableDebugLog)
                {
                    Debug.Log("TaskCompletionHandler: 胜利视频播放完成");
                }
            });
            
            // 等待视频播放完成
            while (!videoFinished && videoPlayer.IsPlaying())
            {
                yield return null;
            }
            
            // 等待视频面板完全隐藏（视频消失）
            while (!videoHidden)
            {
                yield return null;
            }
            
            if (enableDebugLog)
            {
                Debug.Log("TaskCompletionHandler: 视频已完全消失，准备播放气泡消失动画");
            }
        }
        else
        {
            if (enableDebugLog)
            {
                Debug.LogWarning($"TaskCompletionHandler: 任务没有配置胜利视频，跳过视频播放");
            }
        }
        
        // 步骤2：等待延迟时间（如果有）
        if (bubbleAnimationDelay > 0f)
        {
            yield return new WaitForSeconds(bubbleAnimationDelay);
        }
        
        // 步骤3：播放气泡消失动画
        if (iconManager != null)
        {
            iconManager.HideIconForSphere(sphereName, () => {
                if (enableDebugLog)
                {
                    Debug.Log($"TaskCompletionHandler: 气泡消失动画完成，Sphere={sphereName}");
                }
            });
        }
        else
        {
            Debug.LogError("TaskCompletionHandler: iconManager未配置！");
        }
    }
}
