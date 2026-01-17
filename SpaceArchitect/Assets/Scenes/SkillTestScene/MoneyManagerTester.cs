using UnityEngine;
using TMPro;

/// <summary>
/// MoneyManager 测试工具
/// 用于在编辑器和打包后测试 MoneyManager 的功能
/// </summary>
public class MoneyManagerTester : MonoBehaviour
{
    [Header("测试设置")]
    [Tooltip("是否在启动时自动运行测试")]
    [SerializeField] private bool autoTestOnStart = false;
    
    [Tooltip("测试的任务ID")]
    [SerializeField] private int testTaskId = 1;
    
    [Header("引用")]
    [SerializeField] private TaskManager taskManager;
    [SerializeField] private MoneyManager moneyManager;
    
    [Header("显示")]
    [SerializeField] private TextMeshProUGUI statusText;
    
    void Start()
    {
        // 自动查找引用
        if (taskManager == null)
        {
            taskManager = FindObjectOfType<TaskManager>();
        }
        
        if (moneyManager == null)
        {
            moneyManager = FindObjectOfType<MoneyManager>();
        }
        
        if (autoTestOnStart)
        {
            Invoke("RunTest", 2f); // 延迟2秒后运行测试
        }
    }
    
    /// <summary>
    /// 运行测试（可以在Inspector中通过按钮调用）
    /// </summary>
    [ContextMenu("运行测试")]
    public void RunTest()
    {
        Debug.Log("========== MoneyManager 测试开始 ==========");
        
        // 测试1: 检查引用
        TestReferences();
        
        // 测试2: 检查money初始值
        TestMoneyInitialization();
        
        // 测试3: 模拟任务完成
        TestTaskCompletion();
        
        Debug.Log("========== MoneyManager 测试结束 ==========");
        
        UpdateStatusDisplay();
    }
    
    /// <summary>
    /// 测试引用是否正确
    /// </summary>
    private void TestReferences()
    {
        Debug.Log("--- 测试1: 检查引用 ---");
        
        bool passed = true;
        
        if (taskManager == null)
        {
            Debug.LogError("❌ TaskManager 引用为空！");
            passed = false;
        }
        else
        {
            Debug.Log($"✅ TaskManager 引用正常: {taskManager.name}");
        }
        
        if (moneyManager == null)
        {
            Debug.LogError("❌ MoneyManager 引用为空！");
            passed = false;
        }
        else
        {
            Debug.Log($"✅ MoneyManager 引用正常: {moneyManager.name}");
        }
        
        if (passed)
        {
            Debug.Log("<color=green>✅ 测试1通过：所有引用正常</color>");
        }
        else
        {
            Debug.LogError("<color=red>❌ 测试1失败：存在空引用</color>");
        }
    }
    
    /// <summary>
    /// 测试money初始化
    /// </summary>
    private void TestMoneyInitialization()
    {
        Debug.Log("--- 测试2: 检查money初始化 ---");
        
        int currentMoney = MoneyManager.money;
        Debug.Log($"当前money值: {currentMoney}");
        
        if (currentMoney == 10000)
        {
            Debug.Log("<color=green>✅ 测试2通过：money初始值正确 (10000)</color>");
        }
        else
        {
            Debug.LogWarning($"<color=yellow>⚠️ 测试2警告：money值不是10000，当前值为 {currentMoney}</color>");
        }
    }
    
    /// <summary>
    /// 测试任务完成
    /// </summary>
    private void TestTaskCompletion()
    {
        Debug.Log($"--- 测试3: 模拟任务 {testTaskId} 完成 ---");
        
        if (taskManager == null)
        {
            Debug.LogError("❌ 无法测试：TaskManager 为空");
            return;
        }
        
        // 记录完成前的money值
        int moneyBefore = MoneyManager.money;
        Debug.Log($"完成任务前的money值: {moneyBefore}");
        
        // 模拟任务完成
        taskManager.CompleteTask(testTaskId);
        
        // 等待一帧后检查money值
        StartCoroutine(CheckMoneyAfterTaskCompletion(moneyBefore));
    }
    
    /// <summary>
    /// 检查任务完成后的money值
    /// </summary>
    private System.Collections.IEnumerator CheckMoneyAfterTaskCompletion(int moneyBefore)
    {
        yield return null; // 等待一帧
        
        int moneyAfter = MoneyManager.money;
        int difference = moneyAfter - moneyBefore;
        
        Debug.Log($"完成任务后的money值: {moneyAfter}");
        Debug.Log($"money变化: {difference}");
        
        if (difference > 0)
        {
            Debug.Log($"<color=green>✅ 测试3通过：money增加了 {difference}</color>");
        }
        else if (difference == 0)
        {
            Debug.LogError($"<color=red>❌ 测试3失败：money没有变化！这可能是bug！</color>");
            Debug.LogError("可能的原因：");
            Debug.LogError("1. MoneyManager 未订阅 TaskManager 的事件");
            Debug.LogError("2. orderDataConfig 为空");
            Debug.LogError($"3. taskId={testTaskId} 的订单不存在或已完成");
            
            // 运行诊断
            if (moneyManager != null)
            {
                moneyManager.DiagnoseState();
            }
        }
        else
        {
            Debug.LogWarning($"<color=yellow>⚠️ 测试3警告：money减少了 {Mathf.Abs(difference)}</color>");
        }
    }
    
    /// <summary>
    /// 更新状态显示
    /// </summary>
    private void UpdateStatusDisplay()
    {
        if (statusText != null)
        {
            statusText.text = $"Money: {MoneyManager.money}\n" +
                             $"TaskManager: {(taskManager != null ? "✓" : "✗")}\n" +
                             $"MoneyManager: {(moneyManager != null ? "✓" : "✗")}";
        }
    }
    
    /// <summary>
    /// 重置测试环境
    /// </summary>
    [ContextMenu("重置测试环境")]
    public void ResetTest()
    {
        if (moneyManager != null)
        {
            moneyManager.ResetMoney();
        }
        
        Debug.Log("测试环境已重置");
    }
    
    void Update()
    {
        // 按下T键运行测试
        if (Input.GetKeyDown(KeyCode.T))
        {
            RunTest();
        }
        
        // 按下R键重置
        if (Input.GetKeyDown(KeyCode.R))
        {
            ResetTest();
        }
        
        // 按下D键运行诊断
        if (Input.GetKeyDown(KeyCode.D))
        {
            if (moneyManager != null)
            {
                moneyManager.DiagnoseState();
            }
        }
    }
}

