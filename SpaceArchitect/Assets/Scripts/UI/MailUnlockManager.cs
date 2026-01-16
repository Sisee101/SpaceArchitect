using UnityEngine;

/// <summary>
/// 邮件解锁管理器
/// 监听订单完成事件，根据配置解锁并显示对应的邮件
/// </summary>
public class MailUnlockManager : MonoBehaviour
{
    [Header("引用")]
    [Tooltip("任务管理器")]
    [SerializeField] private TaskManager taskManager;
    
    [Tooltip("邮件数据配置")]
    [SerializeField] private MailDataConfig mailDataConfig;
    
    [Tooltip("邮件面板")]
    [SerializeField] private MailPanel mailPanel;
    
    [Header("调试")]
    [SerializeField] private bool enableDebugLog = true;
    
    void Start()
    {
        // 验证引用
        if (taskManager == null)
        {
            Debug.LogError("MailUnlockManager: TaskManager未配置！");
            return;
        }
        
        if (mailDataConfig == null)
        {
            Debug.LogError("MailUnlockManager: MailDataConfig未配置！");
            return;
        }
        
        if (mailPanel == null)
        {
            Debug.LogError("MailUnlockManager: MailPanel未配置！");
            return;
        }
        
        // 订阅任务完成事件
        taskManager.OnTaskCompleted += OnTaskCompleted;
        
        if (enableDebugLog)
        {
            Debug.Log("MailUnlockManager: 已订阅任务完成事件");
        }
    }
    
    void OnDestroy()
    {
        // 取消订阅任务完成事件
        if (taskManager != null)
        {
            taskManager.OnTaskCompleted -= OnTaskCompleted;
            
            if (enableDebugLog)
            {
                Debug.Log("MailUnlockManager: 已取消订阅任务完成事件");
            }
        }
    }
    
    /// <summary>
    /// 任务完成事件处理
    /// </summary>
    /// <param name="taskId">完成的任务ID（订单ID）</param>
    private void OnTaskCompleted(int taskId)
    {
        if (enableDebugLog)
        {
            Debug.Log($"MailUnlockManager: 收到任务完成事件，taskId={taskId}");
        }
        
        // 查找需要解锁的邮件
        var mailToUnlock = FindMailByUnlockOrderId(taskId);
        
        if (mailToUnlock != null)
        {
            // 尝试插入邮件
            bool success = mailPanel.InsertMailById(mailToUnlock.mailId);
            
            if (success)
            {
                if (enableDebugLog)
                {
                    Debug.Log($"MailUnlockManager: 成功解锁并显示邮件 mailId={mailToUnlock.mailId}（订单ID={taskId}）");
                }
            }
            else
            {
                if (enableDebugLog)
                {
                    Debug.LogWarning($"MailUnlockManager: 邮件 mailId={mailToUnlock.mailId} 插入失败（可能已存在）");
                }
            }
        }
        else
        {
            if (enableDebugLog)
            {
                Debug.Log($"MailUnlockManager: 订单ID={taskId} 没有对应的邮件需要解锁");
            }
        }
    }
    
    /// <summary>
    /// 根据解锁订单ID查找对应的邮件
    /// </summary>
    /// <param name="unlockOrderId">解锁订单ID</param>
    /// <returns>找到的邮件信息，如果不存在返回null</returns>
    private MailDataConfig.MailInfo FindMailByUnlockOrderId(int unlockOrderId)
    {
        if (mailDataConfig == null || mailDataConfig.mailDataList == null)
        {
            return null;
        }
        
        foreach (var mailInfo in mailDataConfig.mailDataList)
        {
            if (mailInfo != null && mailInfo.unlockOrderId == unlockOrderId)
            {
                return mailInfo;
            }
        }
        
        return null;
    }
}
