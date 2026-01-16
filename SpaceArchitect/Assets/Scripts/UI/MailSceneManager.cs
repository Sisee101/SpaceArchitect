using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections.Generic;

/// <summary>
/// 邮件场景管理器（全局单例）
/// 监听场景切换，当进入指定场景时，检查暂存列表中的邮件，如果场景匹配则从暂存移动到正式列表并添加到邮箱
/// 使用DontDestroyOnLoad确保在所有场景中持续存在
/// </summary>
public class MailSceneManager : MonoBehaviour
{
    private static MailSceneManager _instance;
    
    /// <summary>
    /// 获取MailSceneManager单例
    /// </summary>
    public static MailSceneManager Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindObjectOfType<MailSceneManager>();
                
                if (_instance == null)
                {
                    GameObject go = new GameObject("MailSceneManager");
                    _instance = go.AddComponent<MailSceneManager>();
                    DontDestroyOnLoad(go);
                }
            }
            return _instance;
        }
    }
    
    [Header("引用")]
    [Tooltip("邮件数据配置")]
    [SerializeField] private MailDataConfig mailDataConfig;
    
    [Tooltip("任务管理器（用于检查订单是否完成）")]
    [SerializeField] private TaskManager taskManager;
    
    [Tooltip("邮件面板（用于添加邮件）")]
    [SerializeField] private MailPanel mailPanel;
    
    [Header("调试")]
    [SerializeField] private bool enableDebugLog = true;
    
    void Awake()
    {
        // 确保单例
        if (_instance == null)
        {
            _instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else if (_instance != this)
        {
            Debug.LogWarning("检测到多个MailSceneManager实例，销毁重复的实例");
            Destroy(gameObject);
            return;
        }
        
        // 订阅场景加载完成事件
        SceneManager.sceneLoaded += OnSceneLoaded;
    }
    
    void OnDestroy()
    {
        // 取消订阅场景加载完成事件
        SceneManager.sceneLoaded -= OnSceneLoaded;
        
        // 当前实例被销毁时，清除静态引用（如果是当前实例）
        if (_instance == this)
        {
            _instance = null;
        }
    }
    
    void Start()
    {
        // 自动查找引用（如果未配置）
        InitializeReferences();
        
        // 检查当前场景
        CheckCurrentScene();
    }
    
    void OnEnable()
    {
        // 当对象启用时，重新查找场景级的引用（场景切换后可能需要重新查找）
        InitializeReferences();
    }
    
    /// <summary>
    /// 初始化引用
    /// </summary>
    private void InitializeReferences()
    {
        if (mailDataConfig == null)
        {
            mailDataConfig = Resources.FindObjectsOfTypeAll<MailDataConfig>()[0];
            if (mailDataConfig == null)
            {
                Debug.LogWarning("MailSceneManager: 未找到MailDataConfig，请手动配置或在Resources文件夹中放置MailDataConfig资源");
            }
        }
        
        // TaskManager和MailPanel是场景级的，每次场景切换后需要重新查找
        // 使用FindObjectsOfType而不是FindObjectOfType，确保找到所有实例
        TaskManager[] taskManagers = FindObjectsOfType<TaskManager>(true); // true表示包括未激活的对象
        if (taskManagers.Length > 0)
        {
            taskManager = taskManagers[0];
            if (enableDebugLog)
            {
                Debug.Log($"MailSceneManager: 找到TaskManager: {taskManager.gameObject.name}");
            }
        }
        else
        {
            taskManager = null;
            if (enableDebugLog)
            {
                Debug.LogWarning("MailSceneManager: 当前场景中未找到TaskManager");
            }
        }
        
        MailPanel[] mailPanels = FindObjectsOfType<MailPanel>(true); // true表示包括未激活的对象
        if (mailPanels.Length > 0)
        {
            mailPanel = mailPanels[0];
            if (enableDebugLog)
            {
                Debug.Log($"MailSceneManager: 找到MailPanel: {mailPanel.gameObject.name}");
            }
        }
        else
        {
            mailPanel = null;
            if (enableDebugLog)
            {
                Debug.LogWarning("MailSceneManager: 当前场景中未找到MailPanel，将无法添加邮件");
            }
        }
    }
    
    /// <summary>
    /// 场景加载完成时的回调
    /// </summary>
    /// <param name="scene">加载的场景</param>
    /// <param name="loadSceneMode">加载模式</param>
    private void OnSceneLoaded(Scene scene, LoadSceneMode loadSceneMode)
    {
        // 场景切换时，清空场景级引用（因为它们指向已销毁的对象）
        taskManager = null;
        mailPanel = null;
        
        if (enableDebugLog)
        {
            Debug.Log($"MailSceneManager: 场景 {scene.name} 加载完成，已清空场景级引用，准备重新查找");
        }
        
        // 延迟检查，确保所有系统都已初始化
        StartCoroutine(CheckSceneAfterFrame(scene.name));
    }
    
    /// <summary>
    /// 延迟一帧后检查场景
    /// </summary>
    private System.Collections.IEnumerator CheckSceneAfterFrame(string sceneName)
    {
        // 等待多帧，确保场景完全加载，所有GameObject都已初始化
        yield return null;
        yield return null;
        
        // 场景切换后，重新查找场景级的引用
        InitializeReferences();
        
        if (enableDebugLog)
        {
            Debug.Log($"MailSceneManager: 场景加载完成，检查场景: {sceneName}");
            if (mailPanel == null)
            {
                Debug.LogWarning($"MailSceneManager: 场景 {sceneName} 中未找到MailPanel，无法添加邮件");
            }
        }
        
        CheckMailsForScene(sceneName);
    }
    
    /// <summary>
    /// 检查当前场景
    /// </summary>
    private void CheckCurrentScene()
    {
        string currentSceneName = SceneManager.GetActiveScene().name;
        if (enableDebugLog)
        {
            Debug.Log($"MailSceneManager: 检查当前场景: {currentSceneName}");
        }
        
        CheckMailsForScene(currentSceneName);
    }
    
    /// <summary>
    /// 检查指定场景对应的邮件
    /// </summary>
    /// <param name="sceneName">场景名称</param>
    private void CheckMailsForScene(string sceneName)
    {
        if (mailDataConfig == null || mailDataConfig.mailDataList == null)
        {
            if (enableDebugLog)
            {
                Debug.LogWarning("MailSceneManager: mailDataConfig未配置或数据为空！");
            }
            return;
        }
        
        // 如果mailPanel为null，尝试重新查找
        if (mailPanel == null)
        {
            InitializeReferences();
        }
        
        if (mailPanel == null)
        {
            if (enableDebugLog)
            {
                Debug.LogWarning($"MailSceneManager: 场景 {sceneName} 中未找到MailPanel，无法处理邮件！");
            }
            // 如果找不到MailPanel，延迟重试
            StartCoroutine(RetryCheckMailsForScene(sceneName));
            return;
        }
        
        // 获取暂存列表中的邮件ID
        List<int> pendingMailIds = mailPanel.GetPendingMailIds();
        
        if (pendingMailIds.Count == 0)
        {
            if (enableDebugLog)
            {
                Debug.Log($"MailSceneManager: 暂存列表为空，场景 {sceneName} 没有待处理的邮件");
            }
            return;
        }
        
        if (enableDebugLog)
        {
            Debug.Log($"MailSceneManager: 场景 {sceneName}，暂存列表中有 {pendingMailIds.Count} 个邮件待处理: [{string.Join(", ", pendingMailIds)}]");
        }
        
        // 检查暂存列表中的每个邮件
        foreach (int mailId in pendingMailIds)
        {
            // 获取邮件信息
            var mailInfo = mailDataConfig.GetMailInfoById(mailId);
            if (mailInfo == null)
            {
                if (enableDebugLog)
                {
                    Debug.LogWarning($"MailSceneManager: 未找到mailId={mailId}的邮件数据，跳过");
                }
                continue;
            }
            
            // 检查邮件的firstAddSceneName是否匹配当前场景
            if (string.IsNullOrEmpty(mailInfo.firstAddSceneName))
            {
                if (enableDebugLog)
                {
                    Debug.LogWarning($"MailSceneManager: 邮件 mailId={mailId} 的firstAddSceneName未配置，跳过");
                }
                continue;
            }
            
            if (enableDebugLog)
            {
                Debug.Log($"MailSceneManager: 检查邮件 mailId={mailId}，目标场景={mailInfo.firstAddSceneName}，当前场景={sceneName}");
            }
            
            if (mailInfo.firstAddSceneName != sceneName)
            {
                // 场景不匹配，跳过
                if (enableDebugLog)
                {
                    Debug.Log($"MailSceneManager: 邮件 mailId={mailId} 的场景不匹配，跳过（目标：{mailInfo.firstAddSceneName}，当前：{sceneName}）");
                }
                continue;
            }
            
            // 场景匹配，将邮件从暂存移动到正式列表
            if (enableDebugLog)
            {
                Debug.Log($"MailSceneManager: 场景匹配！准备将邮件 mailId={mailId} 从暂存移动到正式列表");
            }
            
            bool success = mailPanel.MoveMailFromPendingToShown(mailId);
            
            if (success)
            {
                if (enableDebugLog)
                {
                    Debug.Log($"MailSceneManager: ✓ 成功！已将邮件 mailId={mailId} 从暂存列表移动到正式列表并添加到邮箱");
                }
            }
            else
            {
                if (enableDebugLog)
                {
                    Debug.LogWarning($"MailSceneManager: ✗ 失败！邮件 mailId={mailId} 从暂存移动到正式列表失败");
                }
            }
        }
    }
    
    /// <summary>
    /// 重试检查场景邮件（当MailPanel未找到时）
    /// </summary>
    private System.Collections.IEnumerator RetryCheckMailsForScene(string sceneName)
    {
        // 等待更长时间，让场景完全加载
        yield return new WaitForSeconds(0.5f);
        
        // 再次尝试查找MailPanel
        InitializeReferences();
        
        if (mailPanel != null)
        {
            if (enableDebugLog)
            {
                Debug.Log($"MailSceneManager: 重试成功，找到MailPanel，继续检查场景 {sceneName}");
            }
            CheckMailsForScene(sceneName);
        }
        else
        {
            if (enableDebugLog)
            {
                Debug.LogWarning($"MailSceneManager: 重试失败，场景 {sceneName} 中仍未找到MailPanel");
            }
        }
    }
    
    
    /// <summary>
    /// 可序列化的List包装类（用于JsonUtility）
    /// </summary>
    [System.Serializable]
    private class SerializableList<T>
    {
        public List<T> list;
        
        public SerializableList(List<T> list)
        {
            this.list = list;
        }
    }
}
