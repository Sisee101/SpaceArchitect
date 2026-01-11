using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Sphere订单数据配置（ScriptableObject）
/// 存储所有Sphere的订单信息，包括订单图片和目标场景
/// 类似行星图鉴的数据集结构，便于配置和扩展
/// </summary>
[CreateAssetMenu(fileName = "SphereOrderData", menuName = "Game/Sphere Order Data")]
public class SphereOrderDataConfig : ScriptableObject
{
    [System.Serializable]
    public class SphereOrderInfo
    {
        [Header("Sphere信息")]
        [Tooltip("Sphere GameObject的名称（必须与场景中的Sphere名称完全一致，区分大小写）")]
        public string sphereName;        // Sphere名称（如"Sphere1", "Sphere2", "Sphere4"）
        
        [Header("订单信息")]
        [Tooltip("订单面板显示的图片")]
        public Sprite orderImage;        // 订单图片
        
        [Tooltip("点击前往配送按钮后跳转的场景名称（必须在Build Settings中）")]
        public string targetSceneName;   // 目标场景名称（如"scene02", "scene03"）
    }
    
    [Header("订单数据列表")]
    [Tooltip("配置所有Sphere的订单信息。每个Sphere对应一个订单图片和一个目标场景。")]
    public List<SphereOrderInfo> orderDataList = new List<SphereOrderInfo>();
    
    /// <summary>
    /// 根据Sphere名称获取订单信息
    /// </summary>
    /// <param name="sphereName">Sphere GameObject的名称</param>
    /// <returns>找到的订单信息，如果不存在返回null</returns>
    public SphereOrderInfo GetOrderInfoBySphereName(string sphereName)
    {
        if (string.IsNullOrEmpty(sphereName))
        {
            Debug.LogWarning("SphereOrderDataConfig: sphereName为空！");
            return null;
        }
        
        if (orderDataList == null || orderDataList.Count == 0)
        {
            Debug.LogWarning("SphereOrderDataConfig: orderDataList为空！请配置订单数据。");
            return null;
        }
        
        foreach (var info in orderDataList)
        {
            if (info != null && info.sphereName == sphereName)
            {
                return info;
            }
        }
        
        Debug.LogWarning($"SphereOrderDataConfig: 未找到名称为 {sphereName} 的Sphere订单数据！");
        return null;
    }
    
    /// <summary>
    /// 检查数据配置是否完整（用于编辑器验证）
    /// </summary>
    public void ValidateData()
    {
        if (orderDataList == null || orderDataList.Count == 0)
        {
            Debug.LogWarning("SphereOrderDataConfig: 订单数据列表为空！");
            return;
        }
        
        for (int i = 0; i < orderDataList.Count; i++)
        {
            var info = orderDataList[i];
            if (info == null)
            {
                Debug.LogWarning($"SphereOrderDataConfig: 第 {i} 个订单信息为空！");
                continue;
            }
            
            if (string.IsNullOrEmpty(info.sphereName))
            {
                Debug.LogWarning($"SphereOrderDataConfig: 第 {i} 个订单信息的Sphere名称为空！");
            }
            
            if (info.orderImage == null)
            {
                Debug.LogWarning($"SphereOrderDataConfig: 第 {i} 个订单信息（{info.sphereName}）的订单图片未配置！");
            }
            
            if (string.IsNullOrEmpty(info.targetSceneName))
            {
                Debug.LogWarning($"SphereOrderDataConfig: 第 {i} 个订单信息（{info.sphereName}）的目标场景名称为空！");
            }
        }
    }
}
