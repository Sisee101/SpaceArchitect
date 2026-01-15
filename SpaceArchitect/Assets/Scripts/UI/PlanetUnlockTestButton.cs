using UnityEngine;

/// <summary>
/// 行星解锁测试按钮脚本
/// 可以拖拽到按钮的onClick事件上，用于一键重置所有行星的解锁状态
/// 主要用于测试，发布版本可以考虑移除或隐藏
/// </summary>
public class PlanetUnlockTestButton : MonoBehaviour
{
    [Header("测试配置")]
    [Tooltip("是否在重置前显示确认提示（在Console中）")]
    [SerializeField] private bool showConfirmation = false;
    
    /// <summary>
    /// 重置所有行星解锁状态
    /// 此方法可以拖拽到按钮的onClick事件上
    /// </summary>
    public void ResetAllUnlocks()
    {
        if (showConfirmation)
        {
            Debug.LogWarning("PlanetUnlockTestButton: 即将重置所有行星解锁状态！");
        }
        
        // 调用PlanetUnlockManager重置所有解锁状态
        PlanetUnlockManager.ResetAllUnlocks();
        
        Debug.Log("PlanetUnlockTestButton: 已触发重置所有行星解锁状态");
    }
}
