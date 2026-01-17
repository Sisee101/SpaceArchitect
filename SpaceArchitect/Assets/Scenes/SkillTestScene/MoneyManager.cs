using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class MoneyManager : MonoBehaviour
{
    // 玩家余额，可在整个游戏项目中访问
    public static int money = 0;

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

    // Start is called before the first frame update
    void Start()
    {
        // 获取同一GameObject上的TextMeshProUGUI组件
        moneyText = GetComponent<TextMeshProUGUI>();
        
        // 如果找不到组件，尝试获取子对象上的组件
        if (moneyText == null)
        {
            moneyText = GetComponentInChildren<TextMeshProUGUI>();
        }
        
        // 自动查找丢失的引用
        if (autoFindOnStart)
        {
            FindMissingReferences();
        }
        
        // 初始化显示
        UpdateMoneyDisplay();
        lastMoneyValue = money;
        
        // 订阅任务完成事件
        SubscribeToTaskManager();
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
            
            if (enableDebugLog)
            {
                Debug.Log("MoneyManager: 已订阅TaskManager的任务完成事件");
            }
        }
        else
        {
            Debug.LogWarning("MoneyManager: TaskManager引用未配置！无法监听任务完成事件");
        }
    }
    
    /// <summary>
    /// 任务完成事件处理
    /// </summary>
    /// <param name="taskId">完成的任务ID</param>
    private void OnTaskCompleted(int taskId)
    {
        // 如果orderDataConfig丢失，尝试重新查找
        if (orderDataConfig == null)
        {
            if (enableDebugLog)
            {
                Debug.LogWarning($"MoneyManager: orderDataConfig引用丢失，尝试重新查找...");
            }
            FindMissingReferences();
        }
        
        // 如果重新查找后仍然为空，输出错误并返回
        if (orderDataConfig == null)
        {
            Debug.LogError($"MoneyManager: 无法处理任务 {taskId} 完成事件，orderDataConfig未配置！");
            return;
        }
        
        // 根据taskId获取订单信息
        var orderInfo = orderDataConfig.GetOrderInfoByTaskId(taskId);
        
        if (orderInfo == null)
        {
            Debug.LogError($"MoneyManager: 任务 {taskId} 对应的订单信息不存在！");
            return;
        }
        
        // 获取订单金额
        int orderAmount = orderInfo.orderAmount;
        
        // 更新金钱
        int previousMoney = money;
        money += orderAmount;
        
        // 输出调试信息
        if (enableDebugLog)
        {
            Debug.Log($"<color=green>MoneyManager: 任务完成！</color>");
            Debug.Log($"  - 任务ID: {taskId}");
            Debug.Log($"  - 订单名称: {orderInfo.sphereName}");
            Debug.Log($"  - 订单金额: +{orderAmount}");
            Debug.Log($"  - 金钱变化: {previousMoney} → {money}");
        }
        
        // 强制更新显示（立即响应）
        UpdateMoneyDisplay();
        lastMoneyValue = money;
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
}
