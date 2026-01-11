using UnityEngine;

/// <summary>
/// 引力枢纽基地范围禁用脚本
/// 当引力枢纽在基地的trigger范围内时，禁用引力作用和偏转效果
/// 离开基地trigger范围后，恢复所有作用
/// 需要配合CoreDragger和CoreDeflector使用，不修改它们的代码
/// </summary>
[RequireComponent(typeof(CoreDragger))]
public class CoreGravityDisabler : MonoBehaviour
{
    [Header("基地设置")]
    [Tooltip("基地的Tag（用于查找基地对象）")]
    public string stationTag = "Station";
    
    [Tooltip("基地的Trigger范围（如果基地有Trigger Collider，会自动检测）")]
    public float stationTriggerRadius = 10f;
    
    [Header("调试")]
    [Tooltip("显示调试信息")]
    public bool showDebugLogs = false;
    
    private CoreDragger coreDragger;
    private CoreDeflector coreDeflector;
    private GameObject stationObject;
    private Collider stationTriggerCollider;
    private bool wasGravityEnabled = true;
    private bool wasDeflectorEnabled = true;
    private bool isInStationRange = false;
    
    void Start()
    {
        coreDragger = GetComponent<CoreDragger>();
        if (coreDragger == null)
        {
            Debug.LogError($"CoreGravityDisabler: {gameObject.name} 缺少 CoreDragger 组件！");
            enabled = false;
            return;
        }
        
        // 查找CoreDeflector组件（可选，如果没有也不报错）
        coreDeflector = GetComponent<CoreDeflector>();
        
        // 查找基地对象
        FindStation();
        
        // 记录初始状态
        wasGravityEnabled = coreDragger.produceGravity;
        wasDeflectorEnabled = (coreDeflector != null && coreDeflector.enabled);
    }
    
    void Update()
    {
        // 检查是否在基地范围内
        CheckStationRange();
        
        // 根据是否在基地范围内，控制引力
        UpdateGravityState();
    }
    
    /// <summary>
    /// 查找基地对象
    /// </summary>
    private void FindStation()
    {
        // 方法1：通过Tag查找
        GameObject[] stations = GameObject.FindGameObjectsWithTag(stationTag);
        if (stations.Length > 0)
        {
            stationObject = stations[0];
            if (stations.Length > 1)
            {
                Debug.LogWarning($"CoreGravityDisabler: 找到多个Tag为 '{stationTag}' 的对象，使用第一个: {stationObject.name}");
            }
        }
        
        // 如果找到了基地，查找它的Trigger Collider
        if (stationObject != null)
        {
            // 查找所有Collider，找到第一个Trigger
            Collider[] colliders = stationObject.GetComponentsInChildren<Collider>();
            foreach (Collider col in colliders)
            {
                if (col.isTrigger)
                {
                    stationTriggerCollider = col;
                    if (showDebugLogs)
                    {
                        Debug.Log($"CoreGravityDisabler: 找到基地Trigger Collider: {col.name}");
                    }
                    break;
                }
            }
            
            if (stationTriggerCollider == null && showDebugLogs)
            {
                Debug.LogWarning($"CoreGravityDisabler: 基地 {stationObject.name} 没有Trigger Collider，将使用距离检测");
            }
        }
        else
        {
            Debug.LogWarning($"CoreGravityDisabler: 未找到Tag为 '{stationTag}' 的基地对象！请确保基地的Tag设置为 '{stationTag}'");
        }
    }
    
    /// <summary>
    /// 检查是否在基地范围内
    /// </summary>
    private void CheckStationRange()
    {
        if (stationObject == null)
        {
            isInStationRange = false;
            return;
        }
        
        // 方法1：如果有Trigger Collider，检查是否在Trigger内
        if (stationTriggerCollider != null)
        {
            // 使用Bounds检查（更简单，不需要OnTriggerEnter）
            Bounds triggerBounds = stationTriggerCollider.bounds;
            isInStationRange = triggerBounds.Contains(transform.position);
        }
        else
        {
            // 方法2：使用距离检测
            float distance = Vector3.Distance(transform.position, stationObject.transform.position);
            isInStationRange = distance <= stationTriggerRadius;
        }
    }
    
    /// <summary>
    /// 更新引力状态和偏转效果
    /// </summary>
    private void UpdateGravityState()
    {
        if (coreDragger == null) return;
        
        // 如果在基地范围内，禁用引力和偏转效果
        if (isInStationRange)
        {
            // 禁用引力
            if (coreDragger.produceGravity)
            {
                if (showDebugLogs)
                {
                    Debug.Log($"CoreGravityDisabler: 引力枢纽在基地范围内，禁用引力");
                }
                coreDragger.produceGravity = false;
            }
            
            // 禁用偏转效果
            if (coreDeflector != null && coreDeflector.enabled)
            {
                if (showDebugLogs)
                {
                    Debug.Log($"CoreGravityDisabler: 引力枢纽在基地范围内，禁用偏转效果");
                }
                coreDeflector.enabled = false;
            }
        }
        else
        {
            // 如果不在基地范围内，恢复引力和偏转效果
            // 恢复引力
            if (!coreDragger.produceGravity && wasGravityEnabled)
            {
                if (showDebugLogs)
                {
                    Debug.Log($"CoreGravityDisabler: 引力枢纽离开基地范围，恢复引力");
                }
                coreDragger.produceGravity = true;
            }
            
            // 恢复偏转效果
            if (coreDeflector != null && !coreDeflector.enabled && wasDeflectorEnabled)
            {
                if (showDebugLogs)
                {
                    Debug.Log($"CoreGravityDisabler: 引力枢纽离开基地范围，恢复偏转效果");
                }
                coreDeflector.enabled = true;
            }
        }
    }
    
    /// <summary>
    /// 在编辑器中显示基地范围
    /// </summary>
    void OnDrawGizmosSelected()
    {
        if (stationObject == null) return;
        
        // 绘制基地范围
        Gizmos.color = isInStationRange ? Color.red : Color.yellow;
        
        if (stationTriggerCollider != null)
        {
            // 如果有Trigger Collider，绘制其Bounds
            Gizmos.DrawWireCube(stationTriggerCollider.bounds.center, stationTriggerCollider.bounds.size);
        }
        else
        {
            // 否则绘制球形范围
            Gizmos.DrawWireSphere(stationObject.transform.position, stationTriggerRadius);
        }
        
        // 绘制从引力枢纽到基地的连线
        Gizmos.color = Color.cyan;
        Gizmos.DrawLine(transform.position, stationObject.transform.position);
    }
}

