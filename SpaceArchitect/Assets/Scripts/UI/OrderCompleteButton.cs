using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 订单完成按钮脚本
/// 可复用的按钮组件，用于在 Inspector 中选择订单并点击按钮修改对应订单的 CompleteOrder 状态
/// </summary>
public class OrderCompleteButton : MonoBehaviour
{
    [Header("按钮引用")]
    [Tooltip("用于触发订单完成操作的 Unity UI Button")]
    [SerializeField] private Button completeButton;
    
    [Header("数据配置")]
    [Tooltip("订单数据配置（ScriptableObject），包含所有订单信息")]
    [SerializeField] private SphereOrderDataConfig orderDataConfig;
    
    [Header("订单选择")]
    [Tooltip("在 Inspector 中通过下拉框选择要修改的订单（索引对应 orderDataList 中的顺序）")]
    [OrderSelector]
    [SerializeField] private int selectedOrderIndex = 0;
    
    [Header("任务管理器（可选）")]
    [Tooltip("如果配置了 TaskManager，点击按钮时也会调用 TaskManager.CompleteTask() 来同步任务状态")]
    [SerializeField] private TaskManager taskManager;
    
    [Header("调试")]
    [Tooltip("是否启用调试日志")]
    [SerializeField] private bool enableDebugLog = true;
    
    void Start()
    {
        // 自动查找 TaskManager（如果未手动配置）
        if (taskManager == null)
        {
            taskManager = FindObjectOfType<TaskManager>();
        }
        
        // 绑定按钮点击事件
        if (completeButton != null)
        {
            completeButton.onClick.AddListener(OnCompleteButtonClicked);
            
            if (enableDebugLog)
            {
                Debug.Log($"OrderCompleteButton: 已绑定按钮点击事件，当前选中订单索引: {selectedOrderIndex}");
            }
        }
        else
        {
            Debug.LogWarning("OrderCompleteButton: Complete Button 未配置！请在 Inspector 中设置按钮引用。");
        }
        
        // 验证配置
        if (orderDataConfig == null)
        {
            Debug.LogWarning("OrderCompleteButton: Order Data Config 未配置！请在 Inspector 中设置订单数据配置。");
        }
    }
    
    void OnDestroy()
    {
        // 取消绑定按钮点击事件
        if (completeButton != null)
        {
            completeButton.onClick.RemoveListener(OnCompleteButtonClicked);
        }
    }
    
    /// <summary>
    /// 按钮点击事件处理
    /// </summary>
    private void OnCompleteButtonClicked()
    {
        // 验证配置
        if (orderDataConfig == null)
        {
            Debug.LogError("OrderCompleteButton: Order Data Config 未配置，无法完成订单！");
            return;
        }
        
        if (orderDataConfig.orderDataList == null || orderDataConfig.orderDataList.Count == 0)
        {
            Debug.LogError("OrderCompleteButton: 订单数据列表为空，无法完成订单！");
            return;
        }
        
        // 验证索引有效性
        if (selectedOrderIndex < 0 || selectedOrderIndex >= orderDataConfig.orderDataList.Count)
        {
            Debug.LogError($"OrderCompleteButton: 选中的订单索引 {selectedOrderIndex} 超出范围（有效范围: 0-{orderDataConfig.orderDataList.Count - 1}）！");
            return;
        }
        
        // 获取选中的订单信息
        var orderInfo = orderDataConfig.orderDataList[selectedOrderIndex];
        if (orderInfo == null)
        {
            Debug.LogError($"OrderCompleteButton: 索引 {selectedOrderIndex} 对应的订单信息为空！");
            return;
        }
        
        // 检查订单是否已完成
        if (orderInfo.CompleteOrder)
        {
            if (enableDebugLog)
            {
                Debug.LogWarning($"OrderCompleteButton: 订单 {orderInfo.sphereName} (索引: {selectedOrderIndex}) 已经完成，无需重复操作。");
            }
            return;
        }
        
        // 设置 CompleteOrder 为 true
        orderInfo.CompleteOrder = true;
        
        if (enableDebugLog)
        {
            Debug.Log($"OrderCompleteButton: 已将订单 {orderInfo.sphereName} (索引: {selectedOrderIndex}, TaskId: {orderInfo.taskId}) 的 CompleteOrder 状态设置为 true");
        }
        
        // 如果订单有 taskId 且配置了 TaskManager，也调用 TaskManager 来同步任务状态
        if (orderInfo.taskId >= 0 && taskManager != null)
        {
            taskManager.CompleteTask(orderInfo.taskId);
            
            if (enableDebugLog)
            {
                Debug.Log($"OrderCompleteButton: 已通过 TaskManager 同步任务 {orderInfo.taskId} 的完成状态");
            }
        }
        else if (orderInfo.taskId >= 0 && taskManager == null)
        {
            if (enableDebugLog)
            {
                Debug.LogWarning($"OrderCompleteButton: 订单 {orderInfo.sphereName} 有 taskId ({orderInfo.taskId})，但未配置 TaskManager，任务状态可能不同步。");
            }
        }
    }
    
    /// <summary>
    /// 手动设置选中的订单索引（可在运行时调用）
    /// </summary>
    /// <param name="index">订单索引</param>
    public void SetSelectedOrderIndex(int index)
    {
        if (orderDataConfig == null || orderDataConfig.orderDataList == null)
        {
            Debug.LogWarning("OrderCompleteButton: Order Data Config 未配置，无法设置订单索引！");
            return;
        }
        
        if (index < 0 || index >= orderDataConfig.orderDataList.Count)
        {
            Debug.LogWarning($"OrderCompleteButton: 索引 {index} 超出范围（有效范围: 0-{orderDataConfig.orderDataList.Count - 1}）！");
            return;
        }
        
        selectedOrderIndex = index;
        
        if (enableDebugLog)
        {
            var orderInfo = orderDataConfig.orderDataList[index];
            Debug.Log($"OrderCompleteButton: 已设置选中订单索引为 {index} ({orderInfo?.sphereName ?? "未知"})");
        }
    }
    
    /// <summary>
    /// 根据 Sphere 名称设置选中的订单索引（可在运行时调用）
    /// </summary>
    /// <param name="sphereName">Sphere 名称</param>
    public void SetSelectedOrderBySphereName(string sphereName)
    {
        if (orderDataConfig == null || orderDataConfig.orderDataList == null)
        {
            Debug.LogWarning("OrderCompleteButton: Order Data Config 未配置，无法设置订单！");
            return;
        }
        
        for (int i = 0; i < orderDataConfig.orderDataList.Count; i++)
        {
            var orderInfo = orderDataConfig.orderDataList[i];
            if (orderInfo != null && orderInfo.sphereName == sphereName)
            {
                selectedOrderIndex = i;
                
                if (enableDebugLog)
                {
                    Debug.Log($"OrderCompleteButton: 已根据 Sphere 名称 {sphereName} 设置选中订单索引为 {i}");
                }
                return;
            }
        }
        
        Debug.LogWarning($"OrderCompleteButton: 未找到 Sphere 名称为 {sphereName} 的订单！");
    }
}
