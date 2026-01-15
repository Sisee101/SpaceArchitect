using UnityEngine;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// CoreDeflector 分析工具
/// 检测场景中所有可能影响飞船的 CoreDeflector，检查重叠和冲突
/// </summary>
public class CoreDeflectorAnalyzer : MonoBehaviour
{
    [Header("分析设置")]
    [Tooltip("要分析的飞船（如果为空，将查找场景中第一个带ShipState的物体）")]
    [SerializeField] private GameObject targetShip;
    
    [Tooltip("飞船标签（用于匹配CoreDeflector的检测标签）")]
    [SerializeField] private string spaceshipTag = "Spaceship";
    
    [Tooltip("分析时是否显示详细信息")]
    [SerializeField] private bool showDetailedInfo = true;
    
    [Header("可视化设置")]
    [Tooltip("是否在Scene视图中绘制影响范围")]
    [SerializeField] private bool drawGizmos = true;
    
    [Tooltip("重叠区域的颜色")]
    [SerializeField] private Color overlapColor = new Color(1f, 0f, 0f, 0.3f);
    
    [Tooltip("单个影响范围的颜色")]
    [SerializeField] private Color singleColor = new Color(0f, 1f, 0f, 0.2f);
    
    // 分析结果
    private List<CoreDeflectorInfo> allDeflectors = new List<CoreDeflectorInfo>();
    private List<OverlapInfo> overlaps = new List<OverlapInfo>();
    private Vector3 shipPosition;
    private bool shipInMultipleRanges = false;
    
    /// <summary>
    /// CoreDeflector 信息结构
    /// </summary>
    public struct CoreDeflectorInfo
    {
        public CoreDeflector deflector;
        public Vector3 position;
        public float triggerRadius;
        public Bounds triggerBounds;
        public bool hasValidCollider;
        public float coreEffectiveMass;
        public float guidanceStrength;
        public float maxAngularVelocity;
        public float targetOrbitRadius;
        public float physicsUpdateInterval;
        public string gameObjectName;
    }
    
    /// <summary>
    /// 重叠信息结构
    /// </summary>
    public struct OverlapInfo
    {
        public CoreDeflectorInfo deflector1;
        public CoreDeflectorInfo deflector2;
        public float overlapDistance; // 两个中心之间的距离
        public float combinedRadius; // 两个半径之和
        public bool isOverlapping; // 是否真正重叠
        public Vector3 overlapCenter; // 重叠区域中心（如果重叠）
    }
    
    void Start()
    {
        AnalyzeScene();
    }
    
    /// <summary>
    /// 分析场景中的所有 CoreDeflector
    /// </summary>
    public void AnalyzeScene()
    {
        allDeflectors.Clear();
        overlaps.Clear();
        
        // 查找所有 CoreDeflector
        CoreDeflector[] deflectors = FindObjectsOfType<CoreDeflector>();
        
        if (deflectors.Length == 0)
        {
            Debug.Log("[CoreDeflector分析] 场景中没有找到 CoreDeflector");
            return;
        }
        
        Debug.Log($"[CoreDeflector分析] 找到 {deflectors.Length} 个 CoreDeflector");
        
        // 获取飞船位置
        if (targetShip == null)
        {
            ShipState shipState = FindObjectOfType<ShipState>();
            if (shipState != null)
            {
                targetShip = shipState.gameObject;
            }
        }
        
        if (targetShip != null)
        {
            shipPosition = targetShip.transform.position;
        }
        else
        {
            shipPosition = Vector3.zero;
            Debug.LogWarning("[CoreDeflector分析] 未找到飞船，将使用原点作为参考位置");
        }
        
        // 分析每个 CoreDeflector
        foreach (CoreDeflector deflector in deflectors)
        {
            CoreDeflectorInfo info = AnalyzeDeflector(deflector);
            allDeflectors.Add(info);
        }
        
        // 检查重叠
        CheckOverlaps();
        
        // 检查飞船是否在多个范围内
        CheckShipInRanges();
        
        // 输出分析结果
        PrintAnalysisResults();
    }
    
    /// <summary>
    /// 分析单个 CoreDeflector
    /// </summary>
    private CoreDeflectorInfo AnalyzeDeflector(CoreDeflector deflector)
    {
        CoreDeflectorInfo info = new CoreDeflectorInfo();
        info.deflector = deflector;
        info.position = deflector.transform.position;
        info.gameObjectName = deflector.gameObject.name;
        
        // 获取参数（通过反射）
        var coreEffectiveMassField = typeof(CoreDeflector).GetField("coreEffectiveMass", 
            System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
        var guidanceStrengthField = typeof(CoreDeflector).GetField("guidanceStrength", 
            System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
        var maxAngularVelocityField = typeof(CoreDeflector).GetField("maxAngularVelocity", 
            System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
        var targetOrbitRadiusField = typeof(CoreDeflector).GetField("targetOrbitRadius", 
            System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
        var physicsUpdateIntervalField = typeof(CoreDeflector).GetField("physicsUpdateInterval", 
            System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
        
        info.coreEffectiveMass = coreEffectiveMassField != null ? (float)coreEffectiveMassField.GetValue(deflector) : 0f;
        info.guidanceStrength = guidanceStrengthField != null ? (float)guidanceStrengthField.GetValue(deflector) : 0f;
        info.maxAngularVelocity = maxAngularVelocityField != null ? (float)maxAngularVelocityField.GetValue(deflector) : 0f;
        info.targetOrbitRadius = targetOrbitRadiusField != null ? (float)targetOrbitRadiusField.GetValue(deflector) : 0f;
        info.physicsUpdateInterval = physicsUpdateIntervalField != null ? (float)physicsUpdateIntervalField.GetValue(deflector) : 0.02f;
        
        // 分析 Collider
        Collider collider = deflector.GetComponent<Collider>();
        if (collider != null && collider.isTrigger)
        {
            info.hasValidCollider = true;
            info.triggerBounds = collider.bounds;
            
            // 计算 trigger 半径
            if (collider is SphereCollider)
            {
                SphereCollider sphere = collider as SphereCollider;
                float scale = Mathf.Max(collider.transform.lossyScale.x, collider.transform.lossyScale.y, collider.transform.lossyScale.z);
                info.triggerRadius = sphere.radius * scale * 1.15f; // 加上安全边距
            }
            else if (collider is BoxCollider)
            {
                BoxCollider box = collider as BoxCollider;
                Vector3 extents = collider.bounds.extents;
                info.triggerRadius = extents.magnitude * 1.15f; // 加上安全边距
            }
            else
            {
                Vector3 extents = collider.bounds.extents;
                info.triggerRadius = extents.magnitude * 1.15f;
            }
        }
        else
        {
            info.hasValidCollider = false;
            info.triggerRadius = 0f;
            Debug.LogWarning($"[CoreDeflector分析] {deflector.gameObject.name} 没有有效的 Trigger Collider！");
        }
        
        return info;
    }
    
    /// <summary>
    /// 检查 CoreDeflector 之间的重叠
    /// </summary>
    private void CheckOverlaps()
    {
        for (int i = 0; i < allDeflectors.Count; i++)
        {
            for (int j = i + 1; j < allDeflectors.Count; j++)
            {
                CoreDeflectorInfo info1 = allDeflectors[i];
                CoreDeflectorInfo info2 = allDeflectors[j];
                
                if (!info1.hasValidCollider || !info2.hasValidCollider)
                {
                    continue;
                }
                
                float distance = Vector3.Distance(info1.position, info2.position);
                float combinedRadius = info1.triggerRadius + info2.triggerRadius;
                bool isOverlapping = distance < combinedRadius;
                
                // 计算重叠区域中心（如果重叠）
                Vector3 overlapCenter = Vector3.zero;
                if (isOverlapping)
                {
                    float t = info1.triggerRadius / combinedRadius;
                    overlapCenter = Vector3.Lerp(info1.position, info2.position, t);
                }
                
                OverlapInfo overlap = new OverlapInfo
                {
                    deflector1 = info1,
                    deflector2 = info2,
                    overlapDistance = distance,
                    combinedRadius = combinedRadius,
                    isOverlapping = isOverlapping,
                    overlapCenter = overlapCenter
                };
                
                overlaps.Add(overlap);
            }
        }
    }
    
    /// <summary>
    /// 检查飞船是否在多个 CoreDeflector 的影响范围内
    /// </summary>
    private void CheckShipInRanges()
    {
        int count = 0;
        List<CoreDeflectorInfo> affectingDeflectors = new List<CoreDeflectorInfo>();
        
        foreach (var info in allDeflectors)
        {
            if (!info.hasValidCollider)
            {
                continue;
            }
            
            float distance = Vector3.Distance(shipPosition, info.position);
            if (distance <= info.triggerRadius)
            {
                count++;
                affectingDeflectors.Add(info);
            }
        }
        
        shipInMultipleRanges = count > 1;
        
        if (count > 0)
        {
            Debug.Log($"[CoreDeflector分析] ⚠️ 飞船位置 ({shipPosition.x:F2}, {shipPosition.y:F2}) 在 {count} 个 CoreDeflector 的影响范围内：");
            foreach (var info in affectingDeflectors)
            {
                float distance = Vector3.Distance(shipPosition, info.position);
                Debug.Log($"  - {info.gameObjectName} (距离: {distance:F2}, 半径: {info.triggerRadius:F2})");
            }
        }
    }
    
    /// <summary>
    /// 输出分析结果
    /// </summary>
    private void PrintAnalysisResults()
    {
        Debug.Log("=== CoreDeflector 分析结果 ===");
        Debug.Log($"场景中共有 {allDeflectors.Count} 个 CoreDeflector");
        
        // 输出每个 CoreDeflector 的详细信息
        for (int i = 0; i < allDeflectors.Count; i++)
        {
            var info = allDeflectors[i];
            Debug.Log($"\n[{i + 1}] {info.gameObjectName}:");
            Debug.Log($"  位置: ({info.position.x:F2}, {info.position.y:F2}, {info.position.z:F2})");
            Debug.Log($"  Trigger半径: {info.triggerRadius:F2}");
            Debug.Log($"  Core有效质量: {info.coreEffectiveMass:F1}");
            Debug.Log($"  引导强度: {info.guidanceStrength:F3} ({(info.guidanceStrength * 100):F1}%)");
            Debug.Log($"  最大角度速度: {info.maxAngularVelocity:F1} 度/秒");
            Debug.Log($"  目标轨道半径: {info.targetOrbitRadius:F2}");
            Debug.Log($"  物理更新间隔: {info.physicsUpdateInterval:F4} 秒");
            Debug.Log($"  有效Collider: {(info.hasValidCollider ? "是" : "否")}");
            
            if (info.hasValidCollider)
            {
                float distanceToShip = Vector3.Distance(shipPosition, info.position);
                bool shipInRange = distanceToShip <= info.triggerRadius;
                Debug.Log($"  到飞船距离: {distanceToShip:F2} {(shipInRange ? "✅ 在范围内" : "❌ 不在范围内")}");
            }
        }
        
        // 输出重叠信息
        if (overlaps.Count > 0)
        {
            Debug.Log($"\n=== 重叠检测结果 ===");
            int overlappingCount = 0;
            foreach (var overlap in overlaps)
            {
                if (overlap.isOverlapping)
                {
                    overlappingCount++;
                    Debug.Log($"⚠️ 重叠 #{overlappingCount}:");
                    Debug.Log($"  {overlap.deflector1.gameObjectName} 和 {overlap.deflector2.gameObjectName}");
                    Debug.Log($"  中心距离: {overlap.overlapDistance:F2}");
                    Debug.Log($"  半径之和: {overlap.combinedRadius:F2}");
                    Debug.Log($"  重叠中心: ({overlap.overlapCenter.x:F2}, {overlap.overlapCenter.y:F2})");
                    Debug.Log($"  ⚠️ 警告：这两个 CoreDeflector 的影响范围重叠，可能同时影响飞船！");
                }
            }
            
            if (overlappingCount == 0)
            {
                Debug.Log("✅ 没有发现重叠的 CoreDeflector");
            }
        }
        else
        {
            Debug.Log("\n✅ 没有发现重叠的 CoreDeflector");
        }
        
        // 输出飞船位置分析
        Debug.Log($"\n=== 飞船位置分析 ===");
        if (shipInMultipleRanges)
        {
            Debug.LogWarning($"⚠️ 警告：飞船当前位置在多个 CoreDeflector 的影响范围内！");
            Debug.LogWarning($"这可能导致轨迹预测不准确，因为多个 CoreDeflector 会同时影响飞船。");
        }
        else
        {
            Debug.Log("✅ 飞船当前位置不在多个 CoreDeflector 的重叠区域内");
        }
        
        Debug.Log("=== 分析完成 ===");
    }
    
    /// <summary>
    /// 在 Scene 视图中绘制可视化
    /// </summary>
    void OnDrawGizmos()
    {
        if (!drawGizmos)
        {
            return;
        }
        
        // 绘制每个 CoreDeflector 的影响范围
        foreach (var info in allDeflectors)
        {
            if (!info.hasValidCollider)
            {
                continue;
            }
            
            // 检查是否与其他重叠
            bool isOverlapping = false;
            foreach (var overlap in overlaps)
            {
                if (overlap.isOverlapping && 
                    (overlap.deflector1.gameObjectName == info.gameObjectName || 
                     overlap.deflector2.gameObjectName == info.gameObjectName))
                {
                    isOverlapping = true;
                    break;
                }
            }
            
            Gizmos.color = isOverlapping ? overlapColor : singleColor;
            Gizmos.DrawWireSphere(info.position, info.triggerRadius);
            
            // 绘制到飞船的连线（如果飞船在范围内）
            if (targetShip != null)
            {
                float distance = Vector3.Distance(shipPosition, info.position);
                if (distance <= info.triggerRadius)
                {
                    Gizmos.color = Color.yellow;
                    Gizmos.DrawLine(info.position, shipPosition);
                }
            }
        }
        
        // 绘制重叠区域
        Gizmos.color = overlapColor;
        foreach (var overlap in overlaps)
        {
            if (overlap.isOverlapping)
            {
                // 绘制两个中心之间的连线
                Gizmos.DrawLine(overlap.deflector1.position, overlap.deflector2.position);
                // 绘制重叠中心
                Gizmos.DrawWireSphere(overlap.overlapCenter, 0.5f);
            }
        }
    }
    
    /// <summary>
    /// 获取所有 CoreDeflector 信息（供外部调用）
    /// </summary>
    public List<CoreDeflectorInfo> GetAllDeflectors()
    {
        return allDeflectors;
    }
    
    /// <summary>
    /// 获取所有重叠信息（供外部调用）
    /// </summary>
    public List<OverlapInfo> GetOverlaps()
    {
        return overlaps;
    }
    
    /// <summary>
    /// 检查飞船是否在多个范围内（供外部调用）
    /// </summary>
    public bool IsShipInMultipleRanges()
    {
        return shipInMultipleRanges;
    }
}







