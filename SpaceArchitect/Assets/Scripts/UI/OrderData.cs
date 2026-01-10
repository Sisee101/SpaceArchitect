using UnityEngine;

/// <summary>
/// 订单数据类
/// 存储订单的基本信息
/// </summary>
[System.Serializable]
public class OrderData
{
    public int orderId;          // 订单ID
    public Sprite orderImage;    // 订单图片（包含所有文字和内容）
    public bool isCompleted;     // 是否已完成（运行时状态）
    
    public OrderData(int id, Sprite image)
    {
        orderId = id;
        orderImage = image;
        isCompleted = false;
    }
}

