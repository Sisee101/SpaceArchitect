using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// 动态文字控制器
/// 管理右下订单信息的文字显示
/// </summary>
public class DynamicTextController : MonoBehaviour
{
    [Header("UI引用")]
    [SerializeField] private Text dynamicText;           // 传统Text组件（二选一）
    [SerializeField] private TextMeshProUGUI dynamicTextTMP; // TextMeshPro组件（二选一）
    
    [Header("占位文字")]
    [Tooltip("占位文字，当前版本显示订单ID")]
    [SerializeField] private string placeholderText = "订单ID: 0";
    
    [Header("文字格式")]
    [Tooltip("订单ID显示格式，{0}会被替换为订单ID")]
    [SerializeField] private string orderIdFormat = "订单ID: {0}";
    
    [Header("调试")]
    [SerializeField] private bool enableDebugLog = true;
    
    void Start()
    {
        // 初始化：显示占位文字
        SetText(placeholderText);
    }
    
    /// <summary>
    /// 设置文字内容（供后续代码调用）
    /// </summary>
    /// <param name="text">文字内容</param>
    public void SetText(string text)
    {
        if (dynamicTextTMP != null)
        {
            dynamicTextTMP.text = text;
        }
        else if (dynamicText != null)
        {
            dynamicText.text = text;
        }
        else
        {
            Debug.LogWarning("DynamicTextController: Text和TextMeshPro都未配置！");
        }
    }
    
    /// <summary>
    /// 设置订单ID（便捷方法，供后续代码调用）
    /// </summary>
    /// <param name="orderId">订单ID</param>
    public void SetOrderId(int orderId)
    {
        string text = string.Format(orderIdFormat, orderId);
        SetText(text);
        
        if (enableDebugLog)
        {
            Debug.Log($"DynamicTextController: 订单ID已更新为 {orderId}");
        }
    }
    
    /// <summary>
    /// 格式化文字（供后续代码调用）
    /// </summary>
    /// <param name="format">格式字符串</param>
    /// <param name="args">参数</param>
    public void UpdateText(string format, params object[] args)
    {
        string text = string.Format(format, args);
        SetText(text);
    }
    
    /// <summary>
    /// 获取当前文字内容
    /// </summary>
    public string GetText()
    {
        if (dynamicTextTMP != null)
        {
            return dynamicTextTMP.text;
        }
        else if (dynamicText != null)
        {
            return dynamicText.text;
        }
        return "";
    }
}
