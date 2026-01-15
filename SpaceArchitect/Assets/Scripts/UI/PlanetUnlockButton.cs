using UnityEngine;

/// <summary>
/// 行星解锁按钮脚本
/// 可以拖拽到按钮的onClick事件上，用于解锁指定的行星
/// </summary>
public class PlanetUnlockButton : MonoBehaviour
{
    [Header("解锁配置")]
    [Tooltip("要解锁的行星索引（0-12）。可以在Inspector中直接选择")]
    [Range(0, 12)]
    [SerializeField] private int planetIndex = 0;
    
    /// <summary>
    /// 解锁配置的行星
    /// 此方法可以拖拽到按钮的onClick事件上
    /// </summary>
    public void UnlockPlanet()
    {
        if (planetIndex < 0 || planetIndex >= 13)
        {
            Debug.LogWarning($"PlanetUnlockButton: 行星索引 {planetIndex} 超出范围（0-12）！");
            return;
        }
        
        // 调用PlanetUnlockManager解锁行星
        PlanetUnlockManager.UnlockPlanet(planetIndex);
        
        Debug.Log($"PlanetUnlockButton: 已触发解锁行星 {planetIndex}");
    }
    
    /// <summary>
    /// 设置要解锁的行星索引（可选，用于动态设置）
    /// </summary>
    /// <param name="index">行星索引（0-12）</param>
    public void SetPlanetIndex(int index)
    {
        if (index < 0 || index >= 13)
        {
            Debug.LogWarning($"PlanetUnlockButton: 行星索引 {index} 超出范围（0-12）！");
            return;
        }
        
        planetIndex = index;
        Debug.Log($"PlanetUnlockButton: 已设置行星索引为 {index}");
    }
    
    /// <summary>
    /// 获取当前配置的行星索引
    /// </summary>
    public int GetPlanetIndex()
    {
        return planetIndex;
    }
}
