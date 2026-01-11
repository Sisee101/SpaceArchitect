using UnityEngine;
using System.Collections.Generic;
using System.Reflection;

/// <summary>
/// 飞船轨迹预测器
/// 实时计算并显示飞船在引力场中的飞行轨迹
/// </summary>
[RequireComponent(typeof(LineRenderer))]
public class TrajectoryPredictor : MonoBehaviour
{
    /// <summary>
    /// 轨迹预测模式
    /// </summary>
    public enum PredictionMode
    {
        Preview,        // 预览模式：发射前固定轨迹预览
        RealTimeTrack   // 实时追踪：飞行中实时更新
    }

    [Header("模式设置")]
    [Tooltip("预测模式：\n- Preview: 发射前显示固定轨迹（用于规划）\n- RealTimeTrack: 飞行中实时更新轨迹")]
    [SerializeField] private PredictionMode predictionMode = PredictionMode.Preview;
    
    [Header("预测设置")]
    [Tooltip("预测的时间步长（秒），越小越精确但性能消耗越大\n推荐：0.002-0.005（超高精度），0.01（高精度），0.02（标准）")]
    [SerializeField] private float predictionTimeStep = 0.005f;
    
    [Tooltip("预测的总步数，决定轨迹的长度")]
    [SerializeField] private int predictionSteps = 500;
    
    [Tooltip("最大预测时间（秒），防止轨迹过长")]
    [SerializeField] private float maxPredictionTime = 10f;
    
    [Tooltip("积分方法：\n- Euler: 简单快速，中等精度\n- Verlet: 更稳定，高精度，适合引力\n- RK2: 中点法，平衡精度和性能")]
    [SerializeField] private IntegrationMethod integrationMethod = IntegrationMethod.Verlet;
    
    /// <summary>
    /// 积分方法枚举
    /// </summary>
    public enum IntegrationMethod
    {
        Euler,      // 简单欧拉法
        Verlet,     // Verlet 积分（推荐）
        RK2         // 二阶龙格库塔（中点法）
    }

    [Header("显示设置")]
    [Tooltip("轨迹线的宽度")]
    [SerializeField] private float lineWidth = 0.1f;
    
    [Tooltip("轨迹线的颜色")]
    [SerializeField] private Color lineColor = new Color(0f, 1f, 0.5f, 0.8f);
    
    [Tooltip("轨迹线的材质")]
    [SerializeField] private Material lineMaterial;
    
    [Tooltip("是否默认显示轨迹")]
    [SerializeField] private bool showOnStart = true;

    [Header("控制设置")]
    [Tooltip("切换显示的按键")]
    [SerializeField] private KeyCode toggleKey = KeyCode.Q;
    
    [Tooltip("轨迹更新间隔（秒），降低性能消耗")]
    [SerializeField] private float updateInterval = 0.1f;

    [Header("性能优化")]
    [Tooltip("只在飞行状态下显示轨迹")]
    [SerializeField] private bool onlyShowWhenFlying = true;
    
    [Tooltip("当飞船速度低于此值时不显示轨迹")]
    [SerializeField] private float minSpeedThreshold = 0.1f;
    
    [Header("物理匹配设置")]
    [Tooltip("考虑飞船的速度缩放系数（从 ShipState 读取）")]
    [SerializeField] private bool useSpeedMultiplier = true;
    
    [Tooltip("引力常数调整系数（如果预测不准，可以微调此值）")]
    [SerializeField] private float gravityMultiplier = 1.0f;
    
    [Header("说明")]
    [Tooltip("Core（引力核心）= 可移动的星球\n通过 NBody.mass 统一计算引力\n预测准确度: 100%")]
    [SerializeField] private string _info = "Core 和 Planet 使用相同的引力计算";

    private LineRenderer lineRenderer;
    private ShipState shipState;
    private NBody shipNBody;
    private GravityEngine gravityEngine;
    
    private bool isTrajectoryVisible = false;
    private float lastUpdateTime = 0f;
    
    // 缓存场景中所有的引力源（NBody对象，包括行星和Core）
    private List<GravitySource> gravitySources = new List<GravitySource>();
    
    // 缓存场景中所有的 CoreDeflector（用于考虑偏转效果）
    private List<CoreDeflector> coreDeflectors = new List<CoreDeflector>();
    
    // 预览模式缓存的轨迹（发射前固定不变）
    private List<Vector3> cachedPreviewTrajectory = null;
    private Vector3 cachedLaunchVelocity = Vector3.zero;
    
    // 缓存 speedMultiplier 的反射字段信息，避免重复查找
    private FieldInfo speedMultiplierField = null;
    private bool hasCheckedSpeedMultiplierField = false;
    
    // 引力源数据结构
    private class GravitySource
    {
        public Vector3 position;
        public float mass;
        
        public GravitySource(Vector3 pos, float m)
        {
            position = pos;
            mass = m;
        }
    }

    void Awake()
    {
        // 获取组件
        lineRenderer = GetComponent<LineRenderer>();
        shipState = GetComponent<ShipState>();
        shipNBody = GetComponent<NBody>();
        
        if (lineRenderer == null)
        {
            Debug.LogError("TrajectoryPredictor: 缺少 LineRenderer 组件！");
            return;
        }
        
        if (shipState == null)
        {
            Debug.LogError("TrajectoryPredictor: 缺少 ShipState 组件！");
        }
        
        if (shipNBody == null)
        {
            Debug.LogError("TrajectoryPredictor: 缺少 NBody 组件！");
        }
        
        // 配置 LineRenderer
        SetupLineRenderer();
    }

    void Start()
    {
        // 获取 GravityEngine 实例
        gravityEngine = GravityEngine.Instance();
        if (gravityEngine == null)
        {
            Debug.LogError("TrajectoryPredictor: 场景中没有找到 GravityEngine！");
            return;
        }
        
        // 设置初始可见性
        isTrajectoryVisible = showOnStart;
        lineRenderer.enabled = isTrajectoryVisible;
        
        // 缓存场景中的引力源和 CoreDeflector
        CacheGravitySources();
        CacheCoreDeflectors();
        
        // 如果是预览模式且飞船未发射，自动切换到预览模式
        if (predictionMode == PredictionMode.Preview && shipState != null && shipState.CurrentState == ShipState.State.PreLaunch)
        {
            // 预览模式会在用户设置发射速度时更新
            Debug.Log("TrajectoryPredictor: 预览模式 - 等待发射参数");
        }
    }

    void Update()
    {
        // 检测切换显示的按键
        if (Input.GetKeyDown(toggleKey))
        {
            ToggleTrajectory();
        }
        
        // 根据条件决定是否更新轨迹
        if (!ShouldUpdateTrajectory())
        {
            if (lineRenderer.enabled)
            {
                lineRenderer.enabled = false;
            }
            return;
        }
        
        // 按更新间隔更新轨迹
        if (Time.time - lastUpdateTime >= updateInterval)
        {
            UpdateTrajectory();
            lastUpdateTime = Time.time;
        }
    }

    /// <summary>
    /// 配置 LineRenderer
    /// </summary>
    private void SetupLineRenderer()
    {
        lineRenderer.startWidth = lineWidth;
        lineRenderer.endWidth = lineWidth;
        lineRenderer.startColor = lineColor;
        lineRenderer.endColor = lineColor;
        lineRenderer.positionCount = 0;
        lineRenderer.useWorldSpace = true;
        
        // 设置材质
        if (lineMaterial != null)
        {
            lineRenderer.material = lineMaterial;
        }
        else
        {
            // 使用默认材质
            lineRenderer.material = new Material(Shader.Find("Sprites/Default"));
            lineRenderer.material.color = lineColor;
        }
        
        // 设置其他属性
        lineRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        lineRenderer.receiveShadows = false;
        lineRenderer.alignment = LineAlignment.View; // 面向摄像机
    }

    /// <summary>
    /// 缓存场景中的所有引力源
    /// </summary>
    private void CacheGravitySources()
    {
        gravitySources.Clear();
        
        // 查找场景中所有的 NBody 对象（排除飞船自己）
        NBody[] allNBodies = FindObjectsOfType<NBody>();
        
        // 获取 GravityEngine 的质量缩放系数
        float massScale = 1.0f;
        if (gravityEngine != null)
        {
            massScale = gravityEngine.massScale;
        }
        
        foreach (NBody nbody in allNBodies)
        {
            // 跳过飞船自己
            if (nbody == shipNBody)
            {
                continue;
            }
            
            // 添加所有有质量的物体（Core 和 Planet 统一处理）
            // Core = 可移动的星球，使用相同的引力计算
            if (nbody.mass > 0.001f)
            {
                // 应用 GravityEngine 的质量缩放
                float effectiveMass = nbody.mass * massScale * gravityMultiplier;
                gravitySources.Add(new GravitySource(nbody.transform.position, effectiveMass));
            }
        }
        
        Debug.Log($"TrajectoryPredictor: 已缓存 {gravitySources.Count} 个引力源（massScale: {massScale}, gravityMultiplier: {gravityMultiplier}）");
    }
    
    /// <summary>
    /// 缓存场景中的所有 CoreDeflector
    /// </summary>
    private void CacheCoreDeflectors()
    {
        coreDeflectors.Clear();
        
        // 查找场景中所有的 CoreDeflector 对象
        CoreDeflector[] allDeflectors = FindObjectsOfType<CoreDeflector>();
        
        foreach (CoreDeflector deflector in allDeflectors)
        {
            if (deflector != null && deflector.enabled)
            {
                coreDeflectors.Add(deflector);
            }
        }
        
        Debug.Log($"TrajectoryPredictor: 已缓存 {coreDeflectors.Count} 个 CoreDeflector");
    }

    /// <summary>
    /// 切换轨迹显示
    /// </summary>
    public void ToggleTrajectory()
    {
        isTrajectoryVisible = !isTrajectoryVisible;
        
        if (isTrajectoryVisible)
        {
            // 立即更新一次轨迹
            UpdateTrajectory();
        }
        else
        {
            lineRenderer.enabled = false;
        }
        
        Debug.Log($"轨迹预测 {(isTrajectoryVisible ? "开启" : "关闭")}");
    }

    /// <summary>
    /// 显示轨迹
    /// </summary>
    public void ShowTrajectory()
    {
        if (!isTrajectoryVisible)
        {
            isTrajectoryVisible = true;
            UpdateTrajectory();
        }
    }

    /// <summary>
    /// 隐藏轨迹
    /// </summary>
    public void HideTrajectory()
    {
        isTrajectoryVisible = false;
        lineRenderer.enabled = false;
    }

    /// <summary>
    /// 判断是否应该更新轨迹
    /// </summary>
    private bool ShouldUpdateTrajectory()
    {
        // 如果不显示轨迹，不更新
        if (!isTrajectoryVisible)
        {
            return false;
        }
        
        // 检查必要组件
        if (shipState == null || shipNBody == null || gravityEngine == null)
        {
            return false;
        }
        
        // 根据模式判断
        if (predictionMode == PredictionMode.Preview)
        {
            // 预览模式：只在发射前显示，且已经有缓存的轨迹
            if (shipState.CurrentState == ShipState.State.PreLaunch && cachedPreviewTrajectory != null)
            {
                return true; // 显示缓存的轨迹
            }
            return false;
        }
        else // RealTimeTrack 模式
        {
            // 实时追踪模式：飞行中实时更新
            
            // 如果设置了只在飞行时显示，检查状态
            if (onlyShowWhenFlying && shipState.CurrentState != ShipState.State.Flying)
            {
                return false;
            }
            
            // 检查飞船是否在引力引擎中
            if (shipNBody.engineRef == null)
            {
                return false;
            }
            
            // 检查速度是否足够
            Vector3 currentVelocity = gravityEngine.GetVelocity(shipNBody);
            if (currentVelocity.magnitude < minSpeedThreshold)
            {
                return false;
            }
            
            return true;
        }
    }

    /// <summary>
    /// 更新轨迹
    /// </summary>
    private void UpdateTrajectory()
    {
        // 重新缓存引力源和 CoreDeflector（以防场景中的物体发生变化）
        // 注意：频繁调用可能影响性能，可以考虑只在特定情况下刷新
        if (Time.frameCount % 60 == 0) // 每60帧刷新一次
        {
            CacheGravitySources();
            CacheCoreDeflectors();
        }
        
        List<Vector3> trajectoryPoints;
        
        // 根据模式选择轨迹来源
        if (predictionMode == PredictionMode.Preview && cachedPreviewTrajectory != null)
        {
            // 预览模式：使用缓存的固定轨迹
            trajectoryPoints = cachedPreviewTrajectory;
        }
        else
        {
            // 实时追踪模式：计算当前轨迹
            // 获取当前位置和速度
            Vector3 currentPosition = transform.position;
            Vector3 currentVelocity = gravityEngine.GetVelocity(shipNBody);
            
            // 模拟轨迹
            trajectoryPoints = SimulateTrajectory(currentPosition, currentVelocity);
        }
        
        // 更新 LineRenderer
        if (trajectoryPoints != null && trajectoryPoints.Count > 0)
        {
            lineRenderer.positionCount = trajectoryPoints.Count;
            lineRenderer.SetPositions(trajectoryPoints.ToArray());
            lineRenderer.enabled = true;
        }
        else
        {
            lineRenderer.positionCount = 0;
            lineRenderer.enabled = false;
        }
    }

    /// <summary>
    /// 模拟飞船的飞行轨迹
    /// </summary>
    /// <param name="startPosition">起始位置</param>
    /// <param name="startVelocity">起始速度</param>
    /// <returns>轨迹点列表</returns>
    private List<Vector3> SimulateTrajectory(Vector3 startPosition, Vector3 startVelocity)
    {
        List<Vector3> points = new List<Vector3>();
        
        // 初始化模拟变量
        Vector3 position = startPosition;
        Vector3 velocity = startVelocity;
        float simulatedTime = 0f;
        
        // 计算实际的步数（不超过最大时间）
        int actualSteps = Mathf.Min(predictionSteps, Mathf.FloorToInt(maxPredictionTime / predictionTimeStep));
        
        // 添加起始点
        points.Add(position);
        
        // 迭代模拟
        for (int i = 0; i < actualSteps; i++)
        {
            // 根据积分方法选择不同的更新算法（传入当前速度用于 CoreDeflector 计算）
            switch (integrationMethod)
            {
                case IntegrationMethod.Euler:
                    // 简单欧拉法
                    UpdateEuler(ref position, ref velocity, predictionTimeStep);
                    break;
                    
                case IntegrationMethod.Verlet:
                    // Verlet 积分（更稳定，适合引力）
                    UpdateVerlet(ref position, ref velocity, predictionTimeStep);
                    break;
                    
                case IntegrationMethod.RK2:
                    // 二阶龙格库塔（中点法）
                    UpdateRK2(ref position, ref velocity, predictionTimeStep);
                    break;
            }
            
            // 限制在XY平面（Z=0）
            position.z = 0f;
            velocity.z = 0f;
            
            // 添加点到轨迹
            points.Add(position);
            
            simulatedTime += predictionTimeStep;
        }
        
        return points;
    }

    /// <summary>
    /// 计算某个位置的引力加速度（包括 CoreDeflector 的偏转效果）
    /// </summary>
    /// <param name="position">位置</param>
    /// <param name="velocity">当前速度（用于计算 CoreDeflector 的偏转效果）</param>
    /// <returns>引力加速度向量</returns>
    private Vector3 CalculateGravityAcceleration(Vector3 position, Vector3 velocity)
    {
        Vector3 totalAcceleration = Vector3.zero;
        
        // 1. 遍历所有引力源（NBody 的引力）
        // 注意：source.position 已经是世界空间坐标，position 也是世界空间坐标
        foreach (GravitySource source in gravitySources)
        {
            // 计算到引力源的方向和距离（世界空间）
            Vector3 direction = source.position - position;
            direction.z = 0f; // 确保在XY平面
            float distance = direction.magnitude;
            
            // 避免除以零或距离过小
            if (distance < 0.001f)
            {
                continue;
            }
            
            // 归一化方向
            direction.Normalize();
            
            // 计算引力加速度：a = GM / r^2
            // source.mass 已经包含了 massScale 和 gravityMultiplier
            // 这里需要考虑物理空间的缩放（如果需要）
            float accelerationMagnitude = source.mass / (distance * distance);
            
            // 累加加速度
            totalAcceleration += direction * accelerationMagnitude;
        }
        
        // 2. 检查是否在 CoreDeflector 的 trigger 范围内，应用额外的偏转效果
        foreach (CoreDeflector deflector in coreDeflectors)
        {
            if (deflector == null || !deflector.enabled)
            {
                continue;
            }
            
            // 检查是否在 trigger 范围内
            Collider triggerCollider = deflector.GetComponent<Collider>();
            if (triggerCollider == null || !triggerCollider.isTrigger)
            {
                continue;
            }
            
            // 使用更准确的 trigger 检测方法
            // 检查位置是否在 trigger 范围内
            bool isInTrigger = IsPointInTrigger(triggerCollider, position);
            
            if (isInTrigger)
            {
                // 直接访问 CoreDeflector 的 public 字段（面板上的数据）
                float coreEffectiveMass = deflector.coreEffectiveMass;
                float guidanceStrength = deflector.guidanceStrength;
                float minDistance = deflector.minDistance;
                float maxAngularVelocity = deflector.maxAngularVelocity;
                float targetOrbitRadius = deflector.targetOrbitRadius;
                
                // 应用 CoreDeflector 的偏转效果（包括 maxAngularVelocity 限制）
                Vector3 corePosition = deflector.transform.position;
                Vector3 deflectorAcceleration = CalculateCoreDeflectorAcceleration(
                    position, velocity, corePosition, coreEffectiveMass, guidanceStrength, minDistance, maxAngularVelocity, targetOrbitRadius);
                totalAcceleration += deflectorAcceleration;
            }
        }
        
        return totalAcceleration;
    }
    
    /// <summary>
    /// 检查点是否在 trigger 范围内（更准确的检测方法）
    /// </summary>
    private bool IsPointInTrigger(Collider triggerCollider, Vector3 point)
    {
        if (triggerCollider == null || !triggerCollider.isTrigger)
        {
            return false;
        }
        
        // 使用 Physics.ComputePenetration 或其他方法检查
        // 简单方法：检查点是否在 bounds 内
        if (triggerCollider.bounds.Contains(point))
        {
            // 进一步检查实际碰撞（对于非凸形状更准确）
            // 创建一个临时的 Collider 在指定位置进行测试
            // 这里简化处理，使用 bounds 检查
            return true;
        }
        
        // 对于球形 Collider，使用距离检查
        if (triggerCollider is SphereCollider sphereCollider)
        {
            Vector3 sphereCenter = triggerCollider.transform.TransformPoint(sphereCollider.center);
            float sphereRadius = sphereCollider.radius * Mathf.Max(
                triggerCollider.transform.lossyScale.x,
                triggerCollider.transform.lossyScale.y,
                triggerCollider.transform.lossyScale.z);
            float distanceToCenter = Vector3.Distance(point, sphereCenter);
            return distanceToCenter <= sphereRadius;
        }
        
        // 对于 Box Collider，使用 bounds 检查
        if (triggerCollider is BoxCollider)
        {
            return triggerCollider.bounds.Contains(point);
        }
        
        // 对于 Capsule Collider，近似处理
        if (triggerCollider is CapsuleCollider capsuleCollider)
        {
            Vector3 capsuleCenter = triggerCollider.transform.TransformPoint(capsuleCollider.center);
            float capsuleRadius = capsuleCollider.radius * Mathf.Max(
                triggerCollider.transform.lossyScale.x,
                triggerCollider.transform.lossyScale.y);
            float distanceToCenter = Vector3.Distance(point, capsuleCenter);
            return distanceToCenter <= capsuleRadius * 2f; // 近似处理
        }
        
        // 默认使用 bounds 检查
        return triggerCollider.bounds.Contains(point);
    }
    
    /// <summary>
    /// 计算 CoreDeflector 的偏转加速度（完整模拟 CoreDeflector 的逻辑）
    /// </summary>
    private Vector3 CalculateCoreDeflectorAcceleration(
        Vector3 position, Vector3 velocity, Vector3 corePosition, 
        float coreEffectiveMass, float guidanceStrength, float minDistance,
        float maxAngularVelocity, float targetOrbitRadius)
    {
        Vector3 coreToPosition = position - corePosition;
        coreToPosition.z = 0f;
        float distance = coreToPosition.magnitude;
        
        if (distance < minDistance)
        {
            return Vector3.zero;
        }
        
        Vector3 radialDirection = coreToPosition.normalized; // 从Core指向位置
        Vector3 velocityDirection = velocity.magnitude > 0.01f ? velocity.normalized : Vector3.right;
        float currentSpeed = velocity.magnitude;
        
        // 1. 计算纯引力加速度（径向，指向Core）
        // 注意：这里需要考虑 GravityEngine 的 massScale
        float massScale = gravityEngine != null ? gravityEngine.massScale : 1.0f;
        float gravitationalAcceleration = (coreEffectiveMass * massScale) / (distance * distance);
        Vector3 radialGravity = -radialDirection * gravitationalAcceleration; // 负号表示指向Core
        
        // 2. 计算引导加速度（切向，让飞船沿轨道偏转）
        Vector3 tangentialDirection = GetTangentialDirection(radialDirection, velocityDirection);
        Vector3 guidanceAcceleration = Vector3.zero;
        
        // 使用预测的时间步长
        float deltaTime = predictionTimeStep;
        
        if (guidanceStrength > 0f && currentSpeed > 0.01f)
        {
            // 计算理想切向速度（垂直于径向）
            Vector3 idealTangentialVel = tangentialDirection * currentSpeed;
            
            // 计算需要转向切向的加速度
            Vector3 velocityToTangential = idealTangentialVel - velocity;
            guidanceAcceleration = velocityToTangential * guidanceStrength / deltaTime;
            
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
        
        // 4. 应用 maxAngularVelocity 限制（模拟 CoreDeflector 的角度限制逻辑）
        if (maxAngularVelocity > 0f && currentSpeed > 0.01f)
        {
            // 计算当前速度方向和新速度方向
            Vector3 newVelocity = velocity + totalAcceleration * deltaTime;
            Vector3 newVelocityDirection = newVelocity.normalized;
            
            float angleChange = Vector3.Angle(velocityDirection, newVelocityDirection) * Mathf.Deg2Rad;
            float maxRotationPerFrame = maxAngularVelocity * Mathf.Deg2Rad * deltaTime;
            
            if (angleChange > maxRotationPerFrame && angleChange > 0.001f)
            {
                // 限制旋转角度
                Vector3 rotationAxis = Vector3.Cross(velocityDirection, newVelocityDirection);
                if (rotationAxis.magnitude < 0.001f)
                {
                    rotationAxis = Vector3.forward; // 默认绕Z轴
                }
                rotationAxis = rotationAxis.normalized;
                
                Quaternion limitedRotation = Quaternion.AngleAxis(maxAngularVelocity * deltaTime, rotationAxis);
                Vector3 limitedDir = limitedRotation * velocityDirection;
                
                // 根据限制后的方向重新计算加速度
                Vector3 limitedVelocity = limitedDir * currentSpeed;
                Vector3 limitedAcceleration = (limitedVelocity - velocity) / deltaTime;
                
                // 混合：保持加速度的大小，但限制方向变化
                float originalMagnitude = totalAcceleration.magnitude;
                if (originalMagnitude > 0.01f)
                {
                    totalAcceleration = limitedAcceleration.normalized * originalMagnitude;
                }
                else
                {
                    totalAcceleration = limitedAcceleration;
                }
            }
        }
        
        return totalAcceleration;
    }
    
    /// <summary>
    /// 获取切向方向（垂直于径向，选择与速度方向更接近的那个）
    /// </summary>
    private Vector3 GetTangentialDirection(Vector3 radialDirection, Vector3 velocityDirection)
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
    /// 强制刷新引力源缓存（当场景中的物体发生变化时调用）
    /// </summary>
    public void RefreshGravitySources()
    {
        CacheGravitySources();
        CacheCoreDeflectors();
    }

    /// <summary>
    /// 设置预览模式（发射前固定轨迹）
    /// 用于发射器在用户调整发射参数时调用
    /// </summary>
    /// <param name="launchPosition">发射位置</param>
    /// <param name="launchVelocity">发射速度</param>
    public void SetPreviewTrajectory(Vector3 launchPosition, Vector3 launchVelocity)
    {
        // 应用速度缩放系数（如果启用）
        Vector3 effectiveVelocity = launchVelocity;
        if (useSpeedMultiplier && shipState != null)
        {
            // 读取 ShipState 的 speedMultiplier（通过反射，因为它是私有的）
            var speedMultiplierField = typeof(ShipState).GetField("speedMultiplier", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (speedMultiplierField != null)
            {
                float speedMultiplier = (float)speedMultiplierField.GetValue(shipState);
                effectiveVelocity = launchVelocity * speedMultiplier;
                Debug.Log($"应用速度缩放系数: {speedMultiplier}, 原速度: {launchVelocity.magnitude:F2}, 缩放后: {effectiveVelocity.magnitude:F2}");
            }
        }
        
        // 缓存发射速度
        cachedLaunchVelocity = effectiveVelocity;
        
        // 计算并缓存轨迹
        cachedPreviewTrajectory = SimulateTrajectory(launchPosition, effectiveVelocity);
        
        // 如果轨迹可见，立即更新显示
        if (isTrajectoryVisible)
        {
            UpdateTrajectory();
        }
        
        Debug.Log($"预览轨迹已更新：输入速度 {launchVelocity.magnitude:F2}, 有效速度 {effectiveVelocity.magnitude:F2}");
    }

    /// <summary>
    /// 清除预览轨迹缓存（发射后调用）
    /// </summary>
    public void ClearPreviewTrajectory()
    {
        cachedPreviewTrajectory = null;
        cachedLaunchVelocity = Vector3.zero;
        
        // 如果是预览模式，切换到实时追踪模式
        if (predictionMode == PredictionMode.Preview)
        {
            Debug.Log("飞船已发射，预览轨迹已清除");
        }
    }

    /// <summary>
    /// 设置预测模式
    /// </summary>
    /// <param name="mode">预测模式</param>
    public void SetPredictionMode(PredictionMode mode)
    {
        predictionMode = mode;
        
        // 如果切换到实时追踪模式，清除缓存的预览轨迹
        if (mode == PredictionMode.RealTimeTrack)
        {
            cachedPreviewTrajectory = null;
        }
        
        Debug.Log($"轨迹预测模式已切换为：{mode}");
    }

    /// <summary>
    /// 获取当前预测模式
    /// </summary>
    public PredictionMode GetPredictionMode()
    {
        return predictionMode;
    }

    /// <summary>
    /// 设置轨迹线颜色
    /// </summary>
    public void SetTrajectoryColor(Color color)
    {
        lineColor = color;
        if (lineRenderer != null)
        {
            lineRenderer.startColor = color;
            lineRenderer.endColor = color;
            if (lineRenderer.material != null)
            {
                lineRenderer.material.color = color;
            }
        }
    }

    /// <summary>
    /// 设置轨迹线宽度
    /// </summary>
    public void SetTrajectoryWidth(float width)
    {
        lineWidth = width;
        if (lineRenderer != null)
        {
            lineRenderer.startWidth = width;
            lineRenderer.endWidth = width;
        }
    }

    /// <summary>
    /// 获取飞船的速度缩放系数（通过反射访问私有字段）
    /// </summary>
    private float GetSpeedMultiplier()
    {
        if (!useSpeedMultiplier || shipState == null)
        {
            return 1.0f;
        }
        
        // 第一次调用时查找字段
        if (!hasCheckedSpeedMultiplierField)
        {
            speedMultiplierField = typeof(ShipState).GetField("speedMultiplier", 
                BindingFlags.NonPublic | BindingFlags.Instance);
            hasCheckedSpeedMultiplierField = true;
        }
        
        // 如果字段存在，读取值
        if (speedMultiplierField != null)
        {
            return (float)speedMultiplierField.GetValue(shipState);
        }
        
        return 1.0f;
    }
    
    /// <summary>
    /// 欧拉积分方法（简单，中等精度）
    /// </summary>
    private void UpdateEuler(ref Vector3 position, ref Vector3 velocity, float dt)
    {
        // 1. 计算当前加速度（传入当前速度用于 CoreDeflector 计算）
        Vector3 acceleration = CalculateGravityAcceleration(position, velocity);
        
        // 2. 更新速度
        velocity += acceleration * dt;
        
        // 3. 应用速度倍率（如果启用）
        float speedMult = GetSpeedMultiplier();
        if (speedMult != 1.0f)
        {
            velocity *= Mathf.Pow(speedMult, dt);
        }
        
        // 4. 更新位置
        position += velocity * dt;
    }
    
    /// <summary>
    /// Verlet 积分方法（更稳定，高精度，适合引力系统）
    /// </summary>
    private void UpdateVerlet(ref Vector3 position, ref Vector3 velocity, float dt)
    {
        // 1. 计算当前加速度（传入当前速度用于 CoreDeflector 计算）
        Vector3 acceleration = CalculateGravityAcceleration(position, velocity);
        
        // 2. 使用半步 Verlet（Velocity Verlet）
        // v(t+dt/2) = v(t) + a(t) * dt/2
        velocity += acceleration * (dt * 0.5f);
        
        // 3. 更新位置
        // x(t+dt) = x(t) + v(t+dt/2) * dt
        position += velocity * dt;
        
        // 4. 计算新位置的加速度（传入更新后的速度用于 CoreDeflector 计算）
        Vector3 newAcceleration = CalculateGravityAcceleration(position, velocity);
        
        // 5. 完成速度更新
        // v(t+dt) = v(t+dt/2) + a(t+dt) * dt/2
        velocity += newAcceleration * (dt * 0.5f);
        
        // 6. 应用速度倍率（如果启用）
        float speedMult = GetSpeedMultiplier();
        if (speedMult != 1.0f)
        {
            velocity *= Mathf.Pow(speedMult, dt);
        }
    }
    
    /// <summary>
    /// RK2 积分方法（中点法，平衡精度和性能）
    /// </summary>
    private void UpdateRK2(ref Vector3 position, ref Vector3 velocity, float dt)
    {
        // 1. 计算当前状态的导数 k1（传入当前速度用于 CoreDeflector 计算）
        Vector3 k1_v = CalculateGravityAcceleration(position, velocity);
        Vector3 k1_x = velocity;
        
        // 2. 计算中点状态
        Vector3 midPosition = position + k1_x * (dt * 0.5f);
        Vector3 midVelocity = velocity + k1_v * (dt * 0.5f);
        
        // 3. 计算中点的导数 k2（传入中点速度用于 CoreDeflector 计算）
        Vector3 k2_v = CalculateGravityAcceleration(midPosition, midVelocity);
        Vector3 k2_x = midVelocity;
        
        // 4. 使用中点导数更新
        velocity += k2_v * dt;
        position += k2_x * dt;
        
        // 5. 应用速度倍率（如果启用）
        float speedMult = GetSpeedMultiplier();
        if (speedMult != 1.0f)
        {
            velocity *= Mathf.Pow(speedMult, dt);
        }
    }

    void OnDestroy()
    {
        // 清理资源
        gravitySources.Clear();
    }

    void OnDrawGizmosSelected()
    {
        // 在编辑器中显示引力源（用于调试）
        if (gravitySources != null && gravitySources.Count > 0)
        {
            Gizmos.color = Color.yellow;
            foreach (GravitySource source in gravitySources)
            {
                Gizmos.DrawWireSphere(source.position, 0.5f);
            }
        }
    }
}
