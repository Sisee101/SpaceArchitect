using UnityEngine;

/// <summary>
/// Core引力弹弓 - 仅在trigger范围内产生引力效果
/// 这样不会影响飞船被Planet的捕获，只在飞船接近Core时产生引力弹弓偏转
/// 需要Collider（IsTrigger = true）来定义影响范围
/// </summary>
[RequireComponent(typeof(Collider))]
public class CoreDeflector : MonoBehaviour
{
    [Header("检测设置")]
    [Tooltip("飞船标签")]
    public string spaceshipTag = "Spaceship";
    
    [Header("引力设置")]
    [Tooltip("Core的有效质量（用于计算引力，越大引力越强）\n这个质量只用于计算，不会影响全局引力")]
    public float coreEffectiveMass = 1000f;
    
    [Tooltip("引力常数（通常保持默认值，如果需要调整引力强度，修改上面的质量即可）")]
    public float gravitationalConstant = 6.67e-11f; // 实际值，但会被缩放
    
    [Tooltip("最小有效距离（防止数值过大）")]
    public float minDistance = 0.5f;
    
    [Header("偏转引导设置")]
    [Tooltip("引导强度（0-1），0=纯引力（不可控），1=完全引导（可预测）\n推荐值：0.3-0.7，既能保持引力弹弓感觉又能控制方向")]
    [Range(0f, 1f)]
    public float guidanceStrength = 0.5f;
    
    [Tooltip("最大偏转角度（度/秒），限制每秒钟速度方向最多改变多少度\n这样可以防止飞船被拉得太快，让手感更平滑")]
    [Range(0f, 360f)]
    public float maxAngularVelocity = 120f;
    
    [Tooltip("目标轨道半径（如果>0，会引导飞船到切向方向，形成轨道偏转）")]
    public float targetOrbitRadius = 0f; // 0表示不强制轨道半径
    
    [Header("触发设置")]
    [Tooltip("物理更新间隔（秒）")]
    public float physicsUpdateInterval = 0.02f;
    
    [Header("调试")]
    [Tooltip("显示调试信息")]
    public bool showDebugLogs = false;
    
    private GravityEngine gravityEngine;
    private Collider triggerCollider;
    
    // 跟踪已进入trigger的飞船
    private System.Collections.Generic.Dictionary<GameObject, float> shipsInTrigger = 
        new System.Collections.Generic.Dictionary<GameObject, float>();
    
    void Start()
    {
        gravityEngine = GravityEngine.instance;
        if (gravityEngine == null)
        {
            Debug.LogError($"CoreDeflector: 无法找到 GravityEngine 实例！");
        }
        
        triggerCollider = GetComponent<Collider>();
        if (triggerCollider == null)
        {
            Debug.LogError($"CoreDeflector: {gameObject.name} 缺少 Collider 组件！");
        }
        else if (!triggerCollider.isTrigger)
        {
            Debug.LogWarning($"CoreDeflector: {gameObject.name} 的 Collider 不是 Trigger，请设置为 IsTrigger = true");
        }
    }
    
    void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag(spaceshipTag))
        {
            return;
        }
        
        GameObject ship = other.gameObject;
        NBody shipNBody = GetShipNBody(ship);
        
        if (shipNBody == null)
        {
            return;
        }
        
        GameObject shipRoot = shipNBody.gameObject;
        shipsInTrigger[shipRoot] = Time.time;
        
        if (showDebugLogs)
        {
            Debug.Log($"[CoreDeflector] 飞船 {ship.name} 进入trigger，开始受到Core引力影响");
        }
    }
    
    void OnTriggerStay(Collider other)
    {
        if (!other.CompareTag(spaceshipTag))
        {
            return;
        }
        
        GameObject ship = other.gameObject;
        NBody shipNBody = GetShipNBody(ship);
        
        if (shipNBody == null)
        {
            return;
        }
        
        GameObject shipRoot = shipNBody.gameObject;
        if (!shipsInTrigger.ContainsKey(shipRoot))
        {
            shipsInTrigger[shipRoot] = Time.time;
        }
    }
    
    void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag(spaceshipTag))
        {
            return;
        }
        
        GameObject ship = other.gameObject;
        NBody shipNBody = GetShipNBody(ship);
        
        if (shipNBody != null)
        {
            shipsInTrigger.Remove(shipNBody.gameObject);
        }
        else
        {
            shipsInTrigger.Remove(ship);
        }
        
        if (showDebugLogs)
        {
            Debug.Log($"[CoreDeflector] 飞船 {ship.name} 离开trigger，不再受Core引力影响");
        }
    }
    
    void FixedUpdate()
    {
        if (gravityEngine == null || shipsInTrigger.Count == 0)
        {
            return;
        }
        
        // 复制keys避免迭代时修改
        var shipsToProcess = new System.Collections.Generic.List<GameObject>(shipsInTrigger.Keys);
        
        foreach (GameObject shipRoot in shipsToProcess)
        {
            if (shipRoot == null)
            {
                shipsInTrigger.Remove(shipRoot);
                continue;
            }
            
            // 检查更新间隔
            float lastUpdateTime = shipsInTrigger[shipRoot];
            if (Time.time - lastUpdateTime < physicsUpdateInterval)
            {
                continue;
            }
            
            NBody shipNBody = shipRoot.GetComponent<NBody>();
            if (shipNBody == null || shipNBody.engineRef == null)
            {
                // 检查飞船状态
                ShipState shipState = shipRoot.GetComponent<ShipState>();
                if (shipState == null || 
                    (shipState.CurrentState != ShipState.State.Flying && 
                     shipState.CurrentState != ShipState.State.Captured))
                {
                    shipsInTrigger.Remove(shipRoot);
                }
                continue;
            }
            
            // 应用引力加速度
            ApplyGravityAcceleration(shipRoot, shipNBody);
            shipsInTrigger[shipRoot] = Time.time;
        }
    }
    
    /// <summary>
    /// 获取飞船的NBody组件（从当前对象或父对象）
    /// </summary>
    NBody GetShipNBody(GameObject ship)
    {
        NBody shipNBody = ship.GetComponent<NBody>();
        if (shipNBody == null)
        {
            shipNBody = ship.GetComponentInParent<NBody>();
        }
        if (shipNBody == null && ship.transform.parent != null)
        {
            shipNBody = ship.transform.parent.GetComponent<NBody>();
        }
        return shipNBody;
    }
    
    /// <summary>
    /// 应用引力加速度（仅在trigger范围内）
    /// </summary>
    void ApplyGravityAcceleration(GameObject ship, NBody shipNBody)
    {
        // 获取飞船当前速度和位置
        Vector3 shipVelocity = gravityEngine.GetVelocity(shipNBody);
        Vector3 shipPosition = ship.transform.position;
        
        // 计算Core到飞船的向量
        Vector3 coreToShip = shipPosition - transform.position;
        coreToShip.z = 0f; // 确保Z=0（XY平面）
        float distance = coreToShip.magnitude;
        
        if (distance < minDistance)
        {
            return;
        }
        
        Vector3 radialDirection = coreToShip.normalized; // 从Core指向飞船
        Vector3 velocityDirection = shipVelocity.normalized;
        
        // 1. 计算纯引力加速度（径向，指向Core）
        float gravitationalAcceleration = coreEffectiveMass / (distance * distance);
        Vector3 radialGravity = -radialDirection * gravitationalAcceleration; // 负号表示指向Core
        
        // 2. 计算引导加速度（切向，让飞船沿轨道偏转）
        Vector3 tangentialDirection = GetTangentialDirection(radialDirection, velocityDirection);
        Vector3 guidanceAcceleration = Vector3.zero;
        
        if (guidanceStrength > 0f)
        {
            // 计算理想切向速度（垂直于径向）
            Vector3 idealTangentialVel = tangentialDirection * shipVelocity.magnitude;
            
            // 计算需要转向切向的加速度
            Vector3 velocityToTangential = idealTangentialVel - shipVelocity;
            guidanceAcceleration = velocityToTangential * guidanceStrength / physicsUpdateInterval;
            
            // 如果有目标轨道半径，添加径向调整
            if (targetOrbitRadius > 0f)
            {
                float radiusError = distance - targetOrbitRadius;
                Vector3 radiusCorrection = -radialDirection * radiusError * guidanceStrength * 0.5f;
                guidanceAcceleration += radiusCorrection;
            }
        }
        
        // 3. 混合引力和引导：最终加速度 = 引力 * (1-引导强度) + 引导 * 引导强度
        Vector3 totalAcceleration = radialGravity * (1f - guidanceStrength) + guidanceAcceleration * guidanceStrength;
        
        // 4. 应用加速度
        Vector3 velocityChange = totalAcceleration * physicsUpdateInterval;
        Vector3 newVelocity = shipVelocity + velocityChange;
        newVelocity.z = 0f;
        
        // 5. 限制最大角度改变速度（让手感更平滑可控）
        if (maxAngularVelocity > 0f)
        {
            float currentSpeed = shipVelocity.magnitude;
            float maxRotationPerFrame = maxAngularVelocity * Mathf.Deg2Rad * physicsUpdateInterval; // 转换为弧度
            
            Vector3 currentDir = velocityDirection;
            Vector3 targetDir = newVelocity.normalized;
            
            float angleChange = Vector3.Angle(currentDir, targetDir) * Mathf.Deg2Rad;
            
            if (angleChange > maxRotationPerFrame)
            {
                // 限制旋转角度
                Vector3 rotationAxis = Vector3.Cross(currentDir, targetDir);
                if (rotationAxis.magnitude < 0.001f)
                {
                    rotationAxis = Vector3.forward; // 默认绕Z轴
                }
                rotationAxis = rotationAxis.normalized;
                
                Quaternion limitedRotation = Quaternion.AngleAxis(maxAngularVelocity * physicsUpdateInterval, rotationAxis);
                Vector3 limitedDir = limitedRotation * currentDir;
                newVelocity = limitedDir * currentSpeed;
            }
        }
        
        // 验证并应用
        if (IsVelocityValid(newVelocity))
        {
            gravityEngine.SetVelocity(shipNBody, newVelocity);
            
            if (showDebugLogs && Time.frameCount % 30 == 0)
            {
                float angleChange = Vector3.Angle(shipVelocity.normalized, newVelocity.normalized);
                Debug.Log($"[CoreDeflector] 飞船 {ship.name} - 距离: {distance:F2}, " +
                         $"引力: {gravitationalAcceleration:F3}, 引导强度: {guidanceStrength:F2}, " +
                         $"角度改变: {angleChange:F1}°");
            }
        }
    }
    
    /// <summary>
    /// 获取切向方向（垂直于径向，选择与速度方向更接近的那个）
    /// </summary>
    Vector3 GetTangentialDirection(Vector3 radialDirection, Vector3 velocityDirection)
    {
        // 计算两个可能的切向方向
        Vector3 tangent1 = new Vector3(-radialDirection.y, radialDirection.x, 0f).normalized;
        Vector3 tangent2 = new Vector3(radialDirection.y, -radialDirection.x, 0f).normalized;
        
        // 选择与速度方向更接近的切向
        float dot1 = Vector3.Dot(velocityDirection, tangent1);
        float dot2 = Vector3.Dot(velocityDirection, tangent2);
        
        return (dot1 > dot2) ? tangent1 : tangent2;
    }
    
    /// <summary>
    /// 验证速度是否有效
    /// </summary>
    bool IsVelocityValid(Vector3 velocity)
    {
        return !float.IsNaN(velocity.x) && !float.IsNaN(velocity.y) && !float.IsNaN(velocity.z) &&
               !float.IsInfinity(velocity.x) && !float.IsInfinity(velocity.y) && !float.IsInfinity(velocity.z);
    }
}
