using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class MoneyManager : MonoBehaviour
{
    // 玩家余额，可在整个游戏项目中访问
    public static int money = 20000;

    // TextMeshPro 文本组件引用
    private TextMeshProUGUI moneyText;
    
    // 上一次的money值，用于检测变化
    private int lastMoneyValue;

    [Header("引用配置")]
    [Tooltip("任务管理器引用")]
    [SerializeField] private TaskManager taskManager;
    
    [Tooltip("订单数据配置引用")]
    [SerializeField] private SphereOrderDataConfig orderDataConfig;
    
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
        
        // 初始化显示
        UpdateMoneyDisplay();
        lastMoneyValue = money;
        
        // 订阅任务完成事件
        if (taskManager != null)
        {
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
        
        // 验证订单数据配置
        if (orderDataConfig == null)
        {
            Debug.LogWarning("MoneyManager: SphereOrderDataConfig引用未配置！无法获取订单金额");
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
    /// 任务完成事件处理
    /// </summary>
    /// <param name="taskId">完成的任务ID</param>
    private void OnTaskCompleted(int taskId)
    {
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
