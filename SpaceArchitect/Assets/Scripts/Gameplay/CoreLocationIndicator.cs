using UnityEngine;

/// <summary>
/// 引力枢纽位置提示脚本
/// 在发射基地附近显示提示，提醒玩家引力枢纽的位置
/// </summary>
public class CoreLocationIndicator : MonoBehaviour
{
    [Header("提示设置")]
    [Tooltip("基地的Tag（用于查找基地对象）")]
    public string stationTag = "Station";
    
    [Tooltip("提示显示距离（当引力枢纽距离基地小于此距离时显示提示）")]
    public float indicatorDistance = 15f;
    
    [Tooltip("提示持续时间（秒），0表示持续显示")]
    public float indicatorDuration = 5f;
    
    [Header("视觉提示")]
    [Tooltip("提示图标（可选，如果为空则只显示文字）")]
    public GameObject indicatorIcon;
    
    [Tooltip("提示文字（显示在Console中）")]
    public string indicatorText = "引力枢纽在这里！";
    
    [Tooltip("提示颜色")]
    public Color indicatorColor = Color.yellow;
    
    [Header("调试")]
    [Tooltip("显示调试信息")]
    public bool showDebugLogs = false;
    
    private GameObject stationObject;
    private float distanceToStation;
    private bool hasShownIndicator = false;
    private float indicatorStartTime = 0f;
    private bool isIndicatorActive = false;
    
    void Start()
    {
        // 查找基地对象
        FindStation();
        
        // 如果找到了基地，检查初始距离
        if (stationObject != null)
        {
            CheckDistanceAndShowIndicator();
        }
    }
    
    void Update()
    {
        if (stationObject == null) return;
        
        // 计算到基地的距离
        distanceToStation = Vector3.Distance(transform.position, stationObject.transform.position);
        
        // 检查是否需要显示提示
        if (distanceToStation <= indicatorDistance)
        {
            if (!hasShownIndicator || !isIndicatorActive)
            {
                ShowIndicator();
            }
        }
        else
        {
            if (isIndicatorActive)
            {
                HideIndicator();
            }
        }
        
        // 检查提示是否应该自动隐藏
        if (isIndicatorActive && indicatorDuration > 0f)
        {
            if (Time.time - indicatorStartTime >= indicatorDuration)
            {
                HideIndicator();
            }
        }
    }
    
    /// <summary>
    /// 查找基地对象
    /// </summary>
    private void FindStation()
    {
        GameObject[] stations = GameObject.FindGameObjectsWithTag(stationTag);
        if (stations.Length > 0)
        {
            stationObject = stations[0];
            if (stations.Length > 1 && showDebugLogs)
            {
                Debug.LogWarning($"CoreLocationIndicator: 找到多个Tag为 '{stationTag}' 的对象，使用第一个: {stationObject.name}");
            }
        }
        else
        {
            Debug.LogWarning($"CoreLocationIndicator: 未找到Tag为 '{stationTag}' 的基地对象！请确保基地的Tag设置为 '{stationTag}'");
        }
    }
    
    /// <summary>
    /// 检查距离并显示提示
    /// </summary>
    private void CheckDistanceAndShowIndicator()
    {
        distanceToStation = Vector3.Distance(transform.position, stationObject.transform.position);
        
        if (distanceToStation <= indicatorDistance)
        {
            ShowIndicator();
        }
    }
    
    /// <summary>
    /// 显示提示
    /// </summary>
    private void ShowIndicator()
    {
        if (isIndicatorActive) return;
        
        isIndicatorActive = true;
        hasShownIndicator = true;
        indicatorStartTime = Time.time;
        
        // 显示图标（如果存在）
        if (indicatorIcon != null)
        {
            indicatorIcon.SetActive(true);
        }
        
        // 输出提示文字
        Debug.Log($"<color=yellow>{indicatorText}</color> 距离基地: {distanceToStation:F1} 单位");
        
        if (showDebugLogs)
        {
            Debug.Log($"CoreLocationIndicator: 显示提示 - 距离基地 {distanceToStation:F1} 单位");
        }
    }
    
    /// <summary>
    /// 隐藏提示
    /// </summary>
    private void HideIndicator()
    {
        if (!isIndicatorActive) return;
        
        isIndicatorActive = false;
        
        // 隐藏图标（如果存在）
        if (indicatorIcon != null)
        {
            indicatorIcon.SetActive(false);
        }
        
        if (showDebugLogs)
        {
            Debug.Log("CoreLocationIndicator: 隐藏提示");
        }
    }
    
    /// <summary>
    /// 在编辑器中显示提示范围
    /// </summary>
    void OnDrawGizmosSelected()
    {
        if (stationObject == null) return;
        
        // 绘制提示范围
        Gizmos.color = indicatorColor;
        Gizmos.DrawWireSphere(stationObject.transform.position, indicatorDistance);
        
        // 绘制从引力枢纽到基地的连线
        Gizmos.color = Color.cyan;
        Gizmos.DrawLine(transform.position, stationObject.transform.position);
        
        // 如果距离在提示范围内，显示特殊标记
        float distance = Vector3.Distance(transform.position, stationObject.transform.position);
        if (distance <= indicatorDistance)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(transform.position, 0.5f);
        }
    }
}














