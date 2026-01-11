using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 订单按钮控制器
/// 处理订单按钮点击，跳转到游戏场景（Scene02）
/// </summary>
public class OrderButton : MonoBehaviour
{
    [Header("订单按钮引用")]
    [SerializeField] private Button orderButton;
    
    [Header("订单信息（可选）")]
    [SerializeField] private Text orderInfoText;
    
    void Start()
    {
        // 绑定订单按钮事件
        if (orderButton != null)
        {
            orderButton.onClick.AddListener(OnOrderButtonClicked);
        }
        else
        {
            // 如果按钮引用为空，尝试获取当前GameObject上的Button组件
            orderButton = GetComponent<Button>();
            if (orderButton != null)
            {
                orderButton.onClick.AddListener(OnOrderButtonClicked);
            }
            else
            {
                Debug.LogWarning("OrderButton: 未找到Button组件，请在Inspector中指定或添加Button组件");
            }
        }
        
        // 如果有订单信息文本，可以显示订单信息（可选）
        if (orderInfoText != null)
        {
            orderInfoText.text = "点击查看订单"; // 占位文本
        }
    }
    
    /// <summary>
    /// 订单按钮点击事件
    /// </summary>
    private void OnOrderButtonClicked()
    {
        Debug.Log("点击订单 - 订单功能已暂时禁用");
        
        // 订单功能已移除，暂时禁用
        // 显示订单面板
        // if (UIManager.Instance != null)
        // {
        //     UIManager.Instance.ShowOrderPanel();
        // }
        // else
        // {
        //     Debug.LogError("OrderButton: UIManager未找到！");
        // }
    }
    
    /// <summary>
    /// 更新订单信息显示（供外部调用，用于显示订单详情）
    /// </summary>
    public void UpdateOrderInfo(string orderInfo)
    {
        if (orderInfoText != null)
        {
            orderInfoText.text = orderInfo;
        }
    }
}

