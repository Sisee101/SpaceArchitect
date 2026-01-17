using UnityEngine;

/// <summary>
/// 邮件解锁管理器（全局单例）
/// 监听订单完成事件，根据配置解锁并显示对应的邮件
/// 使用DontDestroyOnLoad确保在所有场景中持续存在
/// </summary>
public class MailUnlockManager : MonoBehaviour
{
    private static MailUnlockManager _instance;
    
    /// <summary>
    /// 获取MailUnlockManager单例
    /// </summary>
    public static MailUnlockManager Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindObjectOfType<MailUnlockManager>();
                
                if (_instance == null)
                {
                    GameObject go = new GameObject("MailUnlockManager");
                    _instance = go.AddComponent<MailUnlockManager>();
                    DontDestroyOnLoad(go);
                }
            }
            return _instance;
        }
    }
    
    [Header("引用")]
    [Tooltip("任务管理器（如果为空，会在每个场景中自动查找）")]
    [SerializeField] private TaskManager taskManager;
    
    [Tooltip("邮件数据配置")]
    [SerializeField] private MailDataConfig mailDataConfig;
    
    [Tooltip("邮件面板（如果为空，会在每个场景中自动查找）")]
    [SerializeField] private MailPanel mailPanel;
    
    [Header("自动解锁邮件配置")]
    [Tooltip("场景加载时自动解锁的邮件配置")]
    [SerializeField] private SceneMailUnlockConfig[] sceneMailUnlocks = new SceneMailUnlockConfig[]
    {
        new SceneMailUnlockConfig { sceneName = "02_MainHub", mailIds = new int[] { 23, 24 } },
        new SceneMailUnlockConfig { sceneName = "04_MainHub", mailIds = new int[] { 25 } }
    };
    
    [Header("调试")]
    [SerializeField] private bool enableDebugLog = true;
    
    /// <summary>
    /// 场景邮件自动解锁配置
    /// </summary>
    [System.Serializable]
    public class SceneMailUnlockConfig
    {
        [Tooltip("场景名称")]
        public string sceneName;
        
        [Tooltip("该场景加载时自动解锁的邮件ID列表")]
        public int[] mailIds;
    }
    
    void Awake()
    {
        // 确保单例
        if (_instance == null)
        {
            _instance = this;
            DontDestroyOnLoad(gameObject);
            
            // 订阅场景加载事件
            UnityEngine.SceneManagement.SceneManager.sceneLoaded += OnSceneLoaded;
        }
        else if (_instance != this)
        {
            Debug.LogWarning("检测到多个MailUnlockManager实例，销毁重复的实例");
            Destroy(gameObject);
            return;
        }
    }
    
    void Start()
    {
        // 自动查找引用（如果未配置）
        if (mailDataConfig == null)
        {
            mailDataConfig = Resources.FindObjectsOfTypeAll<MailDataConfig>()[0];
            if (mailDataConfig == null)
            {
                Debug.LogWarning("MailUnlockManager: 未找到MailDataConfig，请手动配置或在Resources文件夹中放置MailDataConfig资源");
            }
        }
        
        if (mailPanel == null)
        {
            mailPanel = FindObjectOfType<MailPanel>();
            if (mailPanel == null && enableDebugLog)
            {
                Debug.LogWarning("MailUnlockManager: 未找到MailPanel，将在找到TaskManager时再尝试查找");
            }
        }
        
        // 尝试查找并订阅TaskManager
        SubscribeToTaskManager();
    }
    
    void OnEnable()
    {
        // 当对象启用时，清空场景级引用并重新订阅（场景切换后可能需要重新订阅）
        taskManager = null;
        mailPanel = null;
        SubscribeToTaskManager();
    }
    
    /// <summary>
    /// 场景加载完成时的回调（通过订阅SceneManager.sceneLoaded事件）
    /// </summary>
    void OnSceneLoaded(UnityEngine.SceneManagement.Scene scene, UnityEngine.SceneManagement.LoadSceneMode loadSceneMode)
    {
        // 场景切换时，清空场景级引用并重新订阅
        taskManager = null;
        mailPanel = null;
        
        if (enableDebugLog)
        {
            Debug.Log($"MailUnlockManager: 场景 {scene.name} 加载完成，已清空场景级引用，准备重新订阅");
        }
        
        // 延迟重新订阅，确保场景完全加载
        StartCoroutine(SubscribeAfterSceneLoad(scene.name));
    }
    
    /// <summary>
    /// 延迟订阅TaskManager（场景加载后）
    /// </summary>
    private System.Collections.IEnumerator SubscribeAfterSceneLoad(string sceneName)
    {
        yield return null;
        yield return null; // 等待两帧，确保场景完全加载
        
        // 重新订阅TaskManager
        SubscribeToTaskManager();
        
        // 检查是否需要自动解锁邮件
        AutoUnlockMailsForScene(sceneName);
    }
    
    void OnDestroy()
    {
        // 取消订阅任务完成事件
        UnsubscribeFromTaskManager();
        
        // 取消订阅场景加载事件
        UnityEngine.SceneManagement.SceneManager.sceneLoaded -= OnSceneLoaded;
        
        // 当前实例被销毁时，清除静态引用（如果是当前实例）
        if (_instance == this)
        {
            _instance = null;
        }
    }
    
    /// <summary>
    /// 订阅TaskManager的事件
    /// </summary>
    private void SubscribeToTaskManager()
    {
        // 先取消之前的订阅（如果有）
        UnsubscribeFromTaskManager();
        
        // 如果TaskManager未配置，尝试查找
        if (taskManager == null)
        {
            TaskManager[] taskManagers = FindObjectsOfType<TaskManager>(true); // true表示包括未激活的对象
            if (taskManagers.Length > 0)
            {
                taskManager = taskManagers[0];
                if (enableDebugLog)
                {
                    Debug.Log($"MailUnlockManager: 找到TaskManager: {taskManager.gameObject.name}");
                }
            }
        }
        
        // 验证引用
        if (taskManager == null)
        {
            if (enableDebugLog)
            {
                Debug.LogWarning("MailUnlockManager: 当前场景中未找到TaskManager，将在场景切换后重试");
            }
            return;
        }
        
        if (mailDataConfig == null)
        {
            Debug.LogError("MailUnlockManager: MailDataConfig未配置！");
            return;
        }
        
        if (mailPanel == null)
        {
            MailPanel[] mailPanels = FindObjectsOfType<MailPanel>(true); // true表示包括未激活的对象
            if (mailPanels.Length > 0)
            {
                mailPanel = mailPanels[0];
                if (enableDebugLog)
                {
                    Debug.Log($"MailUnlockManager: 找到MailPanel: {mailPanel.gameObject.name}");
                }
            }
        }
        
        if (mailPanel == null)
        {
            if (enableDebugLog)
            {
                Debug.LogWarning("MailUnlockManager: MailPanel未配置且未找到，将在场景切换后重试");
            }
            return;
        }
        
        // 订阅任务完成事件
        taskManager.OnTaskCompleted += OnTaskCompleted;
        
        if (enableDebugLog)
        {
            Debug.Log("MailUnlockManager: 已订阅任务完成事件");
        }
    }
    
    /// <summary>
    /// 取消订阅TaskManager的事件
    /// </summary>
    private void UnsubscribeFromTaskManager()
    {
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
            // 将邮件添加到暂存列表（已解锁但还不能添加进邮箱）
            // 等待进入对应场景时再由MailSceneManager移动到正式列表
            bool success = mailPanel.AddMailToPending(mailToUnlock.mailId);
            
            if (success)
            {
                if (enableDebugLog)
                {
                    Debug.Log($"MailUnlockManager: 成功解锁邮件 mailId={mailToUnlock.mailId}（订单ID={taskId}），已添加到暂存列表，等待场景 {mailToUnlock.firstAddSceneName} 时添加进邮箱");
                }
            }
            else
            {
                if (enableDebugLog)
                {
                    Debug.LogWarning($"MailUnlockManager: 邮件 mailId={mailToUnlock.mailId} 添加到暂存失败（可能已存在）");
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
    
    /// <summary>
    /// 根据邮件ID查找对应的邮件
    /// </summary>
    /// <param name="mailId">邮件ID</param>
    /// <returns>找到的邮件信息，如果不存在返回null</returns>
    private MailDataConfig.MailInfo FindMailByMailId(int mailId)
    {
        if (mailDataConfig == null || mailDataConfig.mailDataList == null)
        {
            return null;
        }
        
        foreach (var mailInfo in mailDataConfig.mailDataList)
        {
            if (mailInfo != null && mailInfo.mailId == mailId)
            {
                return mailInfo;
            }
        }
        
        return null;
    }
    
    /// <summary>
    /// 为指定场景自动解锁邮件
    /// </summary>
    /// <param name="sceneName">场景名称</param>
    private void AutoUnlockMailsForScene(string sceneName)
    {
        if (sceneMailUnlocks == null || sceneMailUnlocks.Length == 0)
        {
            return;
        }
        
        // 查找匹配的场景配置
        SceneMailUnlockConfig matchedConfig = null;
        foreach (var config in sceneMailUnlocks)
        {
            if (config != null && config.sceneName == sceneName)
            {
                matchedConfig = config;
                break;
            }
        }
        
        if (matchedConfig == null || matchedConfig.mailIds == null || matchedConfig.mailIds.Length == 0)
        {
            // 当前场景没有需要自动解锁的邮件
            return;
        }
        
        // 确保mailPanel已找到
        if (mailPanel == null)
        {
            MailPanel[] mailPanels = FindObjectsOfType<MailPanel>(true);
            if (mailPanels.Length > 0)
            {
                mailPanel = mailPanels[0];
                if (enableDebugLog)
                {
                    Debug.Log($"MailUnlockManager: 找到MailPanel: {mailPanel.gameObject.name}");
                }
            }
        }
        
        if (mailPanel == null)
        {
            Debug.LogWarning($"MailUnlockManager: 场景 {sceneName} 需要自动解锁邮件，但未找到MailPanel！");
            return;
        }
        
        if (mailDataConfig == null)
        {
            Debug.LogError($"MailUnlockManager: 场景 {sceneName} 需要自动解锁邮件，但MailDataConfig未配置！");
            return;
        }
        
        if (enableDebugLog)
        {
            Debug.Log($"<color=cyan>MailUnlockManager: 场景 {sceneName} 开始自动解锁邮件...</color>");
        }
        
        // 解锁所有配置的邮件
        int successCount = 0;
        int skippedCount = 0;
        
        foreach (int mailId in matchedConfig.mailIds)
        {
            // 查找邮件信息
            var mailInfo = FindMailByMailId(mailId);
            
            if (mailInfo == null)
            {
                Debug.LogWarning($"MailUnlockManager: 邮件ID={mailId} 在MailDataConfig中不存在！");
                continue;
            }
            
            // 直接添加到邮件列表（使用InsertMailById方法）
            bool success = mailPanel.InsertMailById(mailId);
            
            if (success)
            {
                successCount++;
                if (enableDebugLog)
                {
                    Debug.Log($"<color=green>✅ MailUnlockManager: 成功自动解锁邮件 mailId={mailId}（场景: {sceneName}）</color>");
                }
            }
            else
            {
                skippedCount++;
                if (enableDebugLog)
                {
                    Debug.Log($"<color=yellow>⚠️ MailUnlockManager: 邮件 mailId={mailId} 已存在，跳过添加</color>");
                }
            }
        }
        
        if (enableDebugLog)
        {
            Debug.Log($"<color=cyan>MailUnlockManager: 场景 {sceneName} 自动解锁完成 - 成功: {successCount}, 跳过: {skippedCount}</color>");
        }
    }
}
