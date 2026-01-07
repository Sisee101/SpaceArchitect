using UnityEngine;

/// <summary>
/// 引力枢纽偏转器 - 只对飞船进行方向偏转和速度微调
/// 放在引力枢纽物体上，用于辅助飞船改变方向，而不是直接吸引
/// </summary>
public class GravityHubDeflector : MonoBehaviour
{
    [Header("偏转范围设置")]
    [Tooltip("飞船进入此距离时开始偏转引导")]
    public float deflectRadius = 20f;
    [Tooltip("飞船离开此距离时停止偏转")]
    public float releaseRadius = 30f;
    
    [Header("偏转强度")]
    [Tooltip("方向偏转的强度，值越小越自然（0.01-0.1推荐）")]
    [Range(0.001f, 0.5f)]
    public float deflectionStrength = 0.05f;
    [Tooltip("速度微调的强度（相对于方向偏转）")]
    [Range(0.0f, 1.0f)]
    public float speedAdjustmentStrength = 0.2f;
    
    [Header("速度限制")]
    [Tooltip("最大允许的速度变化（防止弹飞）")]
    public float maxVelocityChange = 5f;
    [Tooltip("最小速度保持（防止速度过小）")]
    public float minVelocity = 0.5f;
    
    [Header("检测设置")]
    [Tooltip("检测飞船的标签")]
    public string spaceshipTag = "Spaceship";
    [Tooltip("检测间隔（秒），降低性能消耗")]
    public float detectionInterval = 0.1f;
    
    private NBody hubNBody;
    private GravityEngine ge;
    private float lastDetectionTime;
    
    void Start()
    {
        hubNBody = GetComponent<NBody>();
        if (hubNBody == null)
        {
            Debug.LogWarning($"引力枢纽 {gameObject.name} 没有 NBody 组件");
        }
        
        ge = GravityEngine.Instance();
    }
    
    void Update()
    {
        if (Time.time - lastDetectionTime < detectionInterval)
            return;
            
        lastDetectionTime = Time.time;
        
        // 检测并偏转附近的飞船
        DeflectNearbySpaceships();
    }
    
    void DeflectNearbySpaceships()
    {
        if (hubNBody == null || ge == null) return;
        
        // 查找所有飞船
        GameObject[] spaceships = GameObject.FindGameObjectsWithTag(spaceshipTag);
        
        foreach (GameObject ship in spaceships)
        {
            if (ship == null) continue;
            
            NBody shipNBody = ship.GetComponent<NBody>();
            if (shipNBody == null) continue;
            
            // 计算距离（仅XY平面）
            Vector2 hubPos2D = new Vector2(transform.position.x, transform.position.y);
            Vector2 shipPos2D = new Vector2(ship.transform.position.x, ship.transform.position.y);
            float distance = Vector2.Distance(hubPos2D, shipPos2D);
            
            // 检查是否在偏转范围内
            if (distance > deflectRadius && distance <= releaseRadius)
            {
                // 在释放范围内，但不进行偏转（逐渐释放）
                continue;
            }
            else if (distance <= deflectRadius)
            {
                // 在偏转范围内，进行方向偏转
                ApplyDeflection(ship, shipNBody, distance);
            }
        }
    }
    
    void ApplyDeflection(GameObject ship, NBody shipNBody, float distance)
    {
        // 获取当前速度
        Vector3 currentVelocity = ge.GetVelocity(shipNBody);
        float currentSpeed = currentVelocity.magnitude;
        
        // 如果速度太小，不进行偏转
        if (currentSpeed < minVelocity)
        {
            return;
        }
        
        // 计算相对位置（仅XY平面）
        Vector3 relativePos = ship.transform.position - transform.position;
        relativePos.z = 0f; // 确保Z=0
        Vector3 radialDir = relativePos.normalized;
        
        // 计算切向方向（垂直于径向）
        Vector3 tangent = GetTangent(radialDir);
        
        // 计算当前速度的方向
        Vector3 currentDir = currentVelocity.normalized;
        
        // 计算理想偏转方向
        // 理想方向：切向方向，但保持当前速度大小
        Vector3 idealDirection = tangent;
        
        // 如果飞船正在远离，偏转方向应该使其更接近切向
        float radialSpeed = Vector3.Dot(currentVelocity, radialDir);
        if (radialSpeed < 0)
        {
            // 正在远离，偏转使其更接近切向
            idealDirection = tangent;
        }
        else
        {
            // 正在接近，偏转使其更接近切向（避免直接碰撞）
            idealDirection = tangent;
        }
        
        // 计算理想速度（保持当前速度大小，但改变方向）
        Vector3 idealVelocity = idealDirection * currentSpeed;
        
        // 轻微调整方向（使用 Lerp）
        Vector3 adjustedVelocity = Vector3.Lerp(
            currentVelocity,
            idealVelocity,
            deflectionStrength
        );
        
        // 限制速度变化（防止弹飞）
        Vector3 velocityChange = adjustedVelocity - currentVelocity;
        if (velocityChange.magnitude > maxVelocityChange)
        {
            velocityChange = velocityChange.normalized * maxVelocityChange;
            adjustedVelocity = currentVelocity + velocityChange;
        }
        
        // 确保速度不会太小
        if (adjustedVelocity.magnitude < minVelocity)
        {
            adjustedVelocity = adjustedVelocity.normalized * minVelocity;
        }
        
        // 确保Z=0（与lockToXYPlane兼容）
        adjustedVelocity.z = 0f;
        
        // 应用速度调整
        if (IsVelocityValid(adjustedVelocity))
        {
            ge.SetVelocity(shipNBody, adjustedVelocity);
        }
    }
    
    Vector3 GetTangent(Vector3 radialDir)
    {
        // 计算垂直于径向的切向向量（2D平面）
        // 在XY平面上，切向向量 = (-radialDir.y, radialDir.x, 0)
        Vector3 tangent = new Vector3(-radialDir.y, radialDir.x, 0f).normalized;
        
        // 如果切向向量无效，使用默认方向
        if (float.IsNaN(tangent.x) || tangent.magnitude < 0.1f)
        {
            tangent = Vector3.up;
        }
        
        return tangent;
    }
    
    bool IsVelocityValid(Vector3 velocity)
    {
        return !float.IsNaN(velocity.x) && !float.IsNaN(velocity.y) &&
               !float.IsInfinity(velocity.x) && !float.IsInfinity(velocity.y) &&
               velocity.magnitude < 1000f;
    }
    
    void OnDrawGizmosSelected()
    {
        // 在编辑器中显示偏转范围
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, deflectRadius);
        
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, releaseRadius);
    }
}



