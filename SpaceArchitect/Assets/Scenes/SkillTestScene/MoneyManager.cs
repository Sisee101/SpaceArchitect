using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class MoneyManager : MonoBehaviour
{
    // 玩家余额，可在整个游戏项目中访问
    public static int money = 10000;
    
    // 静态标志：确保money只初始化一次
    private static bool moneyInitialized = false;

    // TextMeshPro 文本组件引用
    private TextMeshProUGUI moneyText;
    
    // 上一次的money值，用于检测变化
    private int lastMoneyValue;

    [Header("引用配置")]
    [Tooltip("任务管理器引用（如果为空，将自动查找）")]
    [SerializeField] private TaskManager taskManager;
    
    [Tooltip("订单数据配置引用（如果为空，将自动从Resources加载）")]
    [SerializeField] private SphereOrderDataConfig orderDataConfig;
    
    [Header("自动查找设置")]
    [Tooltip("是否在Start时自动查找丢失的引用（推荐保持为true）")]
    [SerializeField] private bool autoFindOnStart = true;
    
    [Header("调试")]
    [SerializeField] private bool enableDebugLog = true;
    
    // 标志：是否已经订阅过事件
    private bool hasSubscribed = false;

    // 在Awake中提前初始化（比Start更早执行）
    void Awake()
    {
        // 确保money只初始化一次
        if (!moneyInitialized)
        {
            money = 10000;
            moneyInitialized = true;
            if (enableDebugLog)
            {
                Debug.Log($"MoneyManager: 初始化money值为 {money}");
            }
        }
        
        // 获取TextMeshProUGUI组件
        moneyText = GetComponent<TextMeshProUGUI>();
        if (moneyText == null)
        {
            moneyText = GetComponentInChildren<TextMeshProUGUI>();
        }
        
        // 提前查找引用（在Awake中执行，确保在Start前完成）
        if (autoFindOnStart)
        {
            FindMissingReferences();
        }
    }

    // Start is called before the first frame update
    void Start()
    {
        // 初始化显示
        UpdateMoneyDisplay();
        lastMoneyValue = money;
        
        // 在Start中订阅事件（确保TaskManager已经初始化）
        if (autoFindOnStart && (taskManager == null || orderDataConfig == null))
        {
            FindMissingReferences();
        }
        SubscribeToTaskManager();
    }
    
    // 使用LateStart确保在所有Start执行完后再次检查订阅
    void OnEnable()
    {
        // 延迟订阅，确保TaskManager已经完全初始化
        StartCoroutine(LateSubscribe());
    }
    
    private System.Collections.IEnumerator LateSubscribe()
    {
        // 等待一帧，确保所有Start都已执行
        yield return null;
        
        // 如果还没有订阅成功，再次尝试
        if (!hasSubscribed || taskManager == null)
        {
            if (enableDebugLog)
            {
                Debug.Log("MoneyManager: 延迟订阅，重新查找引用...");
            }
            
            if (autoFindOnStart)
            {
                FindMissingReferences();
            }
            SubscribeToTaskManager();
        }
    }
    
    void OnDestroy()
    {
        // 取消订阅事件，避免内存泄漏
        if (taskManager != null)
        {
            taskManager.OnTaskCompleted -= OnTaskCompleted;
        }
    }
    
    /// <summary>
    /// 查找丢失的引用（用于场景重新加载后重新查找）
    /// 此方法可以在场景切换后手动调用
    /// </summary>
    public void FindMissingReferences()
    {
        // 查找TaskManager
        if (taskManager == null)
        {
            // 方法1：查找场景中的所有TaskManager（包括未激活的）
            TaskManager[] allTaskManagers = Resources.FindObjectsOfTypeAll<TaskManager>();
            
            // 优先查找场景中的TaskManager（不是预制体的）
            foreach (TaskManager tm in allTaskManagers)
            {
                // 检查是否是场景中的对象（不是预制体资源）
                if (tm.gameObject.scene.isLoaded)
                {
                    taskManager = tm;
                    if (enableDebugLog)
                    {
                        Debug.Log($"MoneyManager: 成功找到TaskManager（场景对象: {tm.gameObject.name}）");
                    }
                    break;
                }
            }
            
            // 如果还没有找到，尝试普通的FindObjectOfType（只查找激活的）
            if (taskManager == null)
            {
                taskManager = FindObjectOfType<TaskManager>();
                if (taskManager != null && enableDebugLog)
                {
                    Debug.Log($"MoneyManager: 成功找到TaskManager（场景对象: {taskManager.gameObject.name}）");
                }
            }
            
            // 如果仍然找不到，输出警告
            if (taskManager == null)
            {
                Debug.LogWarning("MoneyManager: 未找到TaskManager，请在场景中添加TaskManager或手动指定引用");
            }
        }
        
        // 查找SphereOrderDataConfig
        if (orderDataConfig == null)
        {
            // 方法1：尝试从Resources文件夹加载
            orderDataConfig = Resources.Load<SphereOrderDataConfig>("SphereOrderDataConfig");
            if (orderDataConfig != null && enableDebugLog)
            {
                Debug.Log("MoneyManager: 成功从Resources加载SphereOrderDataConfig");
            }
            
            // 方法2：如果Resources加载失败，尝试查找场景中使用该配置的对象
            if (orderDataConfig == null)
            {
                // 查找场景中使用SphereOrderDataConfig的对象（如TaskManager等）
                TaskManager[] taskManagers = FindObjectsOfType<TaskManager>();
                foreach (TaskManager tm in taskManagers)
                {
                    // 尝试通过反射或直接访问（需要根据TaskManager的实际实现）
                    // 这里使用反射尝试获取orderDataConfig字段
                    var field = typeof(TaskManager).GetField("orderDataConfig", 
                        System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                    if (field != null)
                    {
                        var config = field.GetValue(tm) as SphereOrderDataConfig;
                        if (config != null)
                        {
                            orderDataConfig = config;
                            if (enableDebugLog)
                            {
                                Debug.Log($"MoneyManager: 从TaskManager获取到SphereOrderDataConfig引用");
                            }
                            break;
                        }
                    }
                }
            }
            
            // 如果仍然找不到，输出警告
            if (orderDataConfig == null)
            {
                Debug.LogWarning("MoneyManager: 未找到SphereOrderDataConfig，请确保Resources文件夹中有SphereOrderDataConfig资源，或手动指定引用");
            }
        }
        
        // 重新订阅TaskManager（如果找到了新的引用）
        if (taskManager != null)
        {
            SubscribeToTaskManager();
        }
    }
    
    /// <summary>
    /// 订阅TaskManager的任务完成事件
    /// </summary>
    private void SubscribeToTaskManager()
    {
        if (taskManager != null)
        {
            // 先取消订阅（防止重复订阅）
            taskManager.OnTaskCompleted -= OnTaskCompleted;
            
            // 重新订阅
            taskManager.OnTaskCompleted += OnTaskCompleted;
            
            hasSubscribed = true;
            
            if (enableDebugLog)
            {
                Debug.Log("MoneyManager: 已订阅TaskManager的任务完成事件");
            }
        }
        else
        {
            hasSubscribed = false;
            if (enableDebugLog)
            {
                Debug.LogWarning("MoneyManager: TaskManager引用未配置！无法监听任务完成事件");
            }
        }
    }
    
    /// <summary>
    /// 任务完成事件处理
    /// </summary>
    /// <param name="taskId">完成的任务ID</param>
    private void OnTaskCompleted(int taskId)
    {
        if (enableDebugLog)
        {
            Debug.Log($"<color=cyan>MoneyManager: 收到任务完成事件，taskId={taskId}</color>");
        }
        
        // 如果orderDataConfig丢失，尝试重新查找
        if (orderDataConfig == null)
        {
            Debug.LogWarning($"MoneyManager: orderDataConfig引用丢失，尝试重新查找...");
            FindMissingReferences();
            
            // 等待一帧后再次尝试
            if (orderDataConfig == null)
            {
                Debug.LogError($"MoneyManager: 无法处理任务 {taskId} 完成事件，orderDataConfig未配置！");
                Debug.LogError($"MoneyManager: 请检查 Resources/SphereOrderDataConfig.asset 是否存在！");
                return;
            }
        }
        
        // 根据taskId获取订单信息
        var orderInfo = orderDataConfig.GetOrderInfoByTaskId(taskId);
        
        if (orderInfo == null)
        {
            Debug.LogError($"MoneyManager: 任务 {taskId} 对应的订单信息不存在！");
            Debug.LogError($"MoneyManager: 请检查 SphereOrderDataConfig 中是否配置了 taskId={taskId} 的订单");
            return;
        }
        
        // 获取订单金额
        int orderAmount = orderInfo.orderAmount;
        
        // 输出详细的调试信息（在金额变化前）
        if (enableDebugLog)
        {
            Debug.Log($"<color=yellow>MoneyManager: 准备更新金额</color>");
            Debug.Log($"  - 任务ID: {taskId}");
            Debug.Log($"  - 订单名称: {orderInfo.sphereName}");
            Debug.Log($"  - 订单金额: {orderAmount}");
            Debug.Log($"  - 当前money值: {money}");
        }
        
        // 更新金钱
        int previousMoney = money;
        money += orderAmount;
        
        // 输出金额变化后的调试信息
        if (enableDebugLog)
        {
            Debug.Log($"<color=green>MoneyManager: 任务完成！金额已更新</color>");
            Debug.Log($"  - 金钱变化: {previousMoney} → {money} (增加: +{orderAmount})");
        }
        
        // 强制更新显示（立即响应）
        UpdateMoneyDisplay();
        lastMoneyValue = money;
        
        // 额外的验证：确保money值确实被更新了
        if (money == previousMoney)
        {
            Debug.LogError($"MoneyManager: 警告！money值未更新！taskId={taskId}, orderAmount={orderAmount}");
        }
    }

    // Update is called once per frame
    void Update()
    {
        // 检测money值是否发生变化
        if (money != lastMoneyValue)
        {
            UpdateMoneyDisplay();
            lastMoneyValue = money;
        }
    }

    // 更新Money显示
    private void UpdateMoneyDisplay()
    {
        if (moneyText != null)
        {
            moneyText.text = money.ToString();
        }
    }
    
    /// <summary>
    /// 诊断方法：打印当前状态（用于调试打包后的问题）
    /// 可以在Unity编辑器的Inspector中通过按钮调用，或在代码中手动调用
    /// </summary>
    [ContextMenu("诊断MoneyManager状态")]
    public void DiagnoseState()
    {
        Debug.Log("========== MoneyManager 诊断信息 ==========");
        Debug.Log($"当前money值: {money}");
        Debug.Log($"moneyInitialized: {moneyInitialized}");
        Debug.Log($"lastMoneyValue: {lastMoneyValue}");
        Debug.Log($"hasSubscribed: {hasSubscribed}");
        Debug.Log($"taskManager: {(taskManager != null ? taskManager.name : "null")}");
        Debug.Log($"orderDataConfig: {(orderDataConfig != null ? orderDataConfig.name : "null")}");
        Debug.Log($"moneyText: {(moneyText != null ? moneyText.name : "null")}");
        Debug.Log($"enableDebugLog: {enableDebugLog}");
        
        if (orderDataConfig != null)
        {
            Debug.Log($"orderDataConfig.orderDataList.Count: {orderDataConfig.orderDataList.Count}");
            
            // 检查taskId=1的订单
            var order1 = orderDataConfig.GetOrderInfoByTaskId(1);
            if (order1 != null)
            {
                Debug.Log($"taskId=1 的订单信息:");
                Debug.Log($"  - sphereName: {order1.sphereName}");
                Debug.Log($"  - orderAmount: {order1.orderAmount}");
                Debug.Log($"  - CompleteOrder: {order1.CompleteOrder}");
            }
            else
            {
                Debug.LogError("taskId=1 的订单信息不存在！");
            }
        }
        else
        {
            Debug.LogError("orderDataConfig 为 null！");
        }
        
        Debug.Log("==========================================");
    }
    
    /// <summary>
    /// 重置money值（用于测试）
    /// </summary>
    [ContextMenu("重置Money为10000")]
    public void ResetMoney()
    {
        money = 10000;
        moneyInitialized = true;
        UpdateMoneyDisplay();
        lastMoneyValue = money;
        Debug.Log($"MoneyManager: Money已重置为 {money}");
    }
}
