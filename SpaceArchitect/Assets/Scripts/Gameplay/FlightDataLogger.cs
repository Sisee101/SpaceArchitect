using UnityEngine;
using System.Collections.Generic;
using System.IO;
using System.Text;

/// <summary>
/// 飞船飞行数据记录器
/// 记录飞行过程中的所有关键数据，用于分析轨迹预测问题
/// </summary>
public class FlightDataLogger : MonoBehaviour
{
    [Header("日志设置")]
    [Tooltip("是否启用日志记录")]
    public bool enableLogging = true;
    
    [Tooltip("日志记录间隔（秒），越小越详细但文件越大")]
    public float logInterval = 0.02f; // 每 FixedUpdate 记录一次
    
    [Tooltip("是否记录到文件")]
    public bool logToFile = true;
    
    [Tooltip("日志文件路径（相对于项目根目录）")]
    public string logFilePath = "FlightDataLog.txt";
    
    private ShipState shipState;
    private GravityEngine gravityEngine;
    private NBody shipNBody;
    private List<CoreDeflector> coreDeflectors;
    private List<PlanetGravityCapture> planetCaptures;
    private List<GravityHubDeflector> hubDeflectors;
    
    private float lastLogTime = 0f;
    private StringBuilder logBuffer = new StringBuilder();
    private int frameCount = 0;
    private bool isLogging = false;
    
    // 关键修复：记录上一帧的速度，用于计算速度变化
    private Vector3 lastVelocity = Vector3.zero;
    private bool hasLastVelocity = false;
    
    void Start()
    {
        shipState = GetComponent<ShipState>();
        gravityEngine = GravityEngine.Instance();
        shipNBody = GetComponent<NBody>();
        
        // 查找所有相关组件
        coreDeflectors = new List<CoreDeflector>(FindObjectsOfType<CoreDeflector>());
        planetCaptures = new List<PlanetGravityCapture>(FindObjectsOfType<PlanetGravityCapture>());
        hubDeflectors = new List<GravityHubDeflector>(FindObjectsOfType<GravityHubDeflector>());
        
        if (enableLogging)
        {
            Debug.Log($"[飞行数据记录] 已初始化 - CoreDeflector: {coreDeflectors.Count}, PlanetCapture: {planetCaptures.Count}, HubDeflector: {hubDeflectors.Count}");
        }
    }
    
    void FixedUpdate()
    {
        if (!enableLogging || !isLogging) return;
        
        // 检查是否到了记录时间
        if (Time.unscaledTime - lastLogTime < logInterval) return;
        
        lastLogTime = Time.unscaledTime;
        frameCount++;
        
        // 记录数据
        LogFrameData();
    }
    
    void OnEnable()
    {
        // 监听飞船状态变化和发射事件
        if (EventManager.Instance != null)
        {
            EventManager.Instance.OnShipStateChanged += OnShipStateChanged;
            EventManager.Instance.OnShipLaunched += OnShipLaunched;
        }
    }
    
    void OnDisable()
    {
        // 取消订阅事件
        if (EventManager.Instance != null)
        {
            EventManager.Instance.OnShipStateChanged -= OnShipStateChanged;
            EventManager.Instance.OnShipLaunched -= OnShipLaunched;
        }
        
        // 停止记录
        if (isLogging)
        {
            StopLogging();
        }
    }
    
    void OnShipStateChanged(ShipState.State oldState, ShipState.State newState, GameObject ship)
    {
        // 只处理自己的飞船
        if (ship != gameObject) return;
        
        if (newState == ShipState.State.Flying && oldState != ShipState.State.Flying)
        {
            StartLogging();
        }
        else if ((newState == ShipState.State.Crashed || newState == ShipState.State.Escaped) && isLogging)
        {
            StopLogging();
        }
    }
    
    void OnShipLaunched(Vector3 launchVelocity, GameObject ship)
    {
        // 只处理自己的飞船
        if (ship != gameObject) return;
        
        if (!isLogging)
        {
            StartLogging();
        }
    }
    
    /// <summary>
    /// 开始记录（飞船发射时调用）
    /// </summary>
    public void StartLogging()
    {
        if (!enableLogging) return;
        
        isLogging = true;
        frameCount = 0;
        lastLogTime = Time.unscaledTime;
        logBuffer.Clear();
        
        // 关键修复：重置速度记录
        lastVelocity = Vector3.zero;
        hasLastVelocity = false;
        
        // 记录初始状态
        LogHeader();
        
        Debug.Log("[飞行数据记录] 开始记录飞行数据");
    }
    
    /// <summary>
    /// 停止记录
    /// </summary>
    public void StopLogging()
    {
        if (!isLogging) return;
        
        isLogging = false;
        
        // 保存日志到文件
        if (logToFile && logBuffer.Length > 0)
        {
            SaveLogToFile();
        }
        
        Debug.Log($"[飞行数据记录] 停止记录，共记录 {frameCount} 帧数据");
    }
    
    void LogHeader()
    {
        logBuffer.AppendLine("=== 飞船飞行数据记录 ===");
        logBuffer.AppendLine($"开始时间: {System.DateTime.Now:yyyy-MM-dd HH:mm:ss}");
        logBuffer.AppendLine($"记录间隔: {logInterval} 秒");
        logBuffer.AppendLine($"飞船初始位置: {transform.position}");
        Vector3 initialVelocity = (gravityEngine != null && shipNBody != null) ? gravityEngine.GetVelocity(shipNBody) : Vector3.zero;
        logBuffer.AppendLine($"飞船初始速度: {initialVelocity}");
        logBuffer.AppendLine($"GravityEngine 参数:");
        if (gravityEngine != null)
        {
            logBuffer.AppendLine($"  - physToWorldFactor: {gravityEngine.physToWorldFactor}");
            logBuffer.AppendLine($"  - massScale: {gravityEngine.massScale}");
            logBuffer.AppendLine($"  - fixedDeltaTime: {Time.fixedUnscaledDeltaTime}");
        }
        logBuffer.AppendLine($"CoreDeflector 数量: {coreDeflectors.Count}");
        foreach (var deflector in coreDeflectors)
        {
            logBuffer.AppendLine($"  - {deflector.gameObject.name}: 位置={deflector.transform.position}, 质量={GetPrivateField<float>(deflector, "coreEffectiveMass")}, 引导强度={GetPrivateField<float>(deflector, "guidanceStrength")}");
        }
        logBuffer.AppendLine("---");
        logBuffer.AppendLine("格式: [帧数] [时间] [位置] [速度] [速度大小] [GravityEngine加速度] [CoreDeflector效果] [ShipState缩放] [其他效果]");
        logBuffer.AppendLine("---");
    }
    
    void LogFrameData()
    {
        if (shipNBody == null || gravityEngine == null) return;
        
        Vector3 position = transform.position;
        Vector3 velocity = gravityEngine.GetVelocity(shipNBody);
        float speed = velocity.magnitude;
        
        // 关键修复：计算速度变化（用于详细记录CoreDeflector效果）
        Vector3 velChange = hasLastVelocity ? (velocity - lastVelocity) : Vector3.zero;
        float velChangeMag = velChange.magnitude;
        
        // 更新上一帧速度
        lastVelocity = velocity;
        hasLastVelocity = true;
        
        // 计算 GravityEngine 的引力加速度
        Vector3 gravityAcc = CalculateGravityAcceleration(position);
        
        // 检查 CoreDeflector 效果（传入速度变化）
        string coreDeflectorInfo = GetCoreDeflectorInfo(position, velocity, velChange);
        
        // 速度缩放功能已移除
        // float speedMultiplier = GetSpeedMultiplier();
        // string speedMultiplierInfo = speedMultiplier != 1.0f ? $"speedMult={speedMultiplier:F4}" : "";
        string speedMultiplierInfo = ""; // 速度缩放已移除，不再记录
        
        // 检查其他效果
        string otherEffects = GetOtherEffectsInfo(position, velocity);
        
        // 记录数据
        logBuffer.AppendLine($"[{frameCount}] t={Time.unscaledTime:F4} pos=({position.x:F3},{position.y:F3},{position.z:F3}) vel=({velocity.x:F3},{velocity.y:F3},{velocity.z:F3}) speed={speed:F3} " +
                            $"gravAcc=({gravityAcc.x:F3},{gravityAcc.y:F3},{gravityAcc.z:F3}) " +
                            $"{coreDeflectorInfo} {speedMultiplierInfo} {otherEffects}");
    }
    
    Vector3 CalculateGravityAcceleration(Vector3 position)
    {
        Vector3 totalAcc = Vector3.zero;
        
        if (gravityEngine == null) return totalAcc;
        
        // 获取所有 NBody 对象
        NBody[] allNBodies = FindObjectsOfType<NBody>();
        float massScale = gravityEngine.massScale;
        float physToWorldFactor = gravityEngine.physToWorldFactor;
        float factor3 = physToWorldFactor * physToWorldFactor * physToWorldFactor;
        
        foreach (NBody nbody in allNBodies)
        {
            if (nbody == shipNBody || nbody.mass <= 0.001f) continue;
            
            Vector3 toSource = nbody.transform.position - position;
            float distSq = toSource.sqrMagnitude;
            if (distSq < 0.01f) continue;
            
            float effectiveMass = nbody.mass * massScale * factor3;
            float mag = effectiveMass / distSq;
            totalAcc += toSource.normalized * mag;
        }
        
        return totalAcc;
    }
    
    string GetCoreDeflectorInfo(Vector3 position, Vector3 velocity, Vector3 velChange)
    {
        System.Text.StringBuilder info = new System.Text.StringBuilder();
        
        foreach (var deflector in coreDeflectors)
        {
            Collider trigger = deflector.GetComponent<Collider>();
            if (trigger == null) continue;
            
            // 使用 GravityEngine 的物理位置（与实际代码一致）
            Vector3 shipPhysPos = (gravityEngine != null && shipNBody != null) ? 
                gravityEngine.GetScenePosition(shipNBody) : position;
            
            // 计算到 Core 的距离（在 if 块外声明，避免重复声明）
            Vector3 toCore = deflector.transform.position - shipPhysPos;
            float dist = toCore.magnitude;
            
            // 检查是否在 trigger 内（使用更准确的方法）
            bool inTrigger = trigger.bounds.Contains(shipPhysPos);
            
            // 如果 AABB 检测失败，尝试距离检测
            if (!inTrigger)
            {
                float distToCore = toCore.magnitude;
                
                // 计算 trigger 半径（根据 Collider 类型）
                float triggerRadius = 0f;
                if (trigger is SphereCollider)
                {
                    SphereCollider sphere = trigger as SphereCollider;
                    float scale = Mathf.Max(trigger.transform.lossyScale.x, 
                                          trigger.transform.lossyScale.y, 
                                          trigger.transform.lossyScale.z);
                    triggerRadius = sphere.radius * scale;
                }
                else
                {
                    // 使用 bounds 的最大值作为半径
                    triggerRadius = Mathf.Max(trigger.bounds.size.x, 
                                             trigger.bounds.size.y, 
                                             trigger.bounds.size.z) * 0.5f;
                }
                
                inTrigger = distToCore <= triggerRadius;
            }
            float coreMass = GetPrivateField<float>(deflector, "coreEffectiveMass");
            float guidanceStrength = GetPrivateField<float>(deflector, "guidanceStrength");
            
            // 计算引力加速度
            float gravAccMag = coreMass / (dist * dist);
            
            // 检查 CoreDeflector 是否实际在 shipsInTrigger 中（通过反射）
            bool actuallyInTrigger = IsShipInDeflectorTrigger(deflector);
            
            if (inTrigger || actuallyInTrigger)
            {
                // 关键修复：计算 CoreDeflector 单独的效果（与预测保持一致）
                // 预测中记录的是 CoreDeflector 单独的效果，所以实际中也应该记录单独的效果
                // 而不是总速度变化（总速度变化包含了基础引力等其他因素）
                
                // 计算 CoreDeflector 应该产生的速度变化（与实际代码一致）
                Vector3 coreDeflectorVelChange = CalculateCoreDeflectorVelocityChange(deflector, shipPhysPos, velocity);
                float coreDeflectorVelChangeMag = coreDeflectorVelChange.magnitude;
                
                // 计算 CoreDeflector 的加速度（速度变化 / 时间步长）
                float deltaTime = Time.fixedUnscaledDeltaTime;
                Vector3 coreDeflectorAccel = deltaTime > 0.001f ? (coreDeflectorVelChange / deltaTime) : Vector3.zero;
                float coreDeflectorAccelMag = coreDeflectorAccel.magnitude;
                
                // 详细记录：距离、引力加速度、引导强度、速度变化（CoreDeflector单独）、加速度（CoreDeflector单独）、激活状态
                info.Append($"Core[{deflector.gameObject.name}]:dist={dist:F2},grav={gravAccMag:F3},guidance={guidanceStrength:F2},velChange={coreDeflectorVelChangeMag:F3},accel={coreDeflectorAccelMag:F3}");
                if (actuallyInTrigger)
                {
                    info.Append(",ACTIVE"); // 标记为实际激活
                }
                info.Append(" ");
            }
        }
        
        return info.ToString();
    }
    
    bool IsShipInDeflectorTrigger(CoreDeflector deflector)
    {
        // 通过反射检查 shipsInTrigger 字典
        System.Reflection.FieldInfo field = typeof(CoreDeflector).GetField("shipsInTrigger", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        
        if (field != null)
        {
            var shipsInTrigger = field.GetValue(deflector) as System.Collections.Generic.Dictionary<GameObject, int>;
            if (shipsInTrigger != null && shipNBody != null)
            {
                return shipsInTrigger.ContainsKey(shipNBody.gameObject);
            }
        }
        
        return false;
    }
    
    /// <summary>
    /// 计算 CoreDeflector 单独产生的速度变化（与预测保持一致）
    /// 使用与 CoreDeflector.ApplyGravityAcceleration 相同的计算逻辑
    /// </summary>
    Vector3 CalculateCoreDeflectorVelocityChange(CoreDeflector deflector, Vector3 shipPosition, Vector3 shipVelocity)
    {
        // 获取 CoreDeflector 的参数（通过反射）
        float coreMass = GetPrivateField<float>(deflector, "coreEffectiveMass");
        float guidanceStrength = GetPrivateField<float>(deflector, "guidanceStrength");
        float minDistance = GetPrivateField<float>(deflector, "minDistance");
        float targetOrbitRadius = GetPrivateField<float>(deflector, "targetOrbitRadius");
        
        // 计算Core到飞船的向量
        Vector3 coreToShip = shipPosition - deflector.transform.position;
        coreToShip.z = 0f; // 确保Z=0（XY平面）
        float distance = coreToShip.magnitude;
        
        if (distance < minDistance || distance < 0.01f)
        {
            return Vector3.zero;
        }
        
        Vector3 radialDirection = coreToShip.normalized; // 从Core指向飞船
        Vector3 velocityDirection = shipVelocity.normalized;
        
        // 1. 计算纯引力加速度（径向，指向Core）
        float gravitationalAcceleration = coreMass / (distance * distance);
        Vector3 radialGravity = -radialDirection * gravitationalAcceleration; // 负号表示指向Core
        
        // 2. 计算引导加速度（切向，让飞船沿轨道偏转）
        Vector3 tangentialDirection = GetTangentialDirection(radialDirection, velocityDirection);
        Vector3 guidanceAcceleration = Vector3.zero;
        
        float deltaTime = Time.fixedUnscaledDeltaTime;
        
        if (guidanceStrength > 0f)
        {
            // 计算理想切向速度（垂直于径向）
            Vector3 idealTangentialVel = tangentialDirection * shipVelocity.magnitude;
            
            // 计算需要转向切向的加速度
            Vector3 velocityToTangential = idealTangentialVel - shipVelocity;
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
        
        // 4. 计算速度变化（使用未缩放时间步长）
        Vector3 velocityChange = totalAcceleration * deltaTime;
        Vector3 newVelocity = shipVelocity + velocityChange;
        newVelocity.z = 0f;
        
        // 关键修复：应用角度限制（与预测和实际代码一致）
        // 获取 maxAngularVelocity 参数（通过反射）
        float maxAngularVelocity = GetPrivateField<float>(deflector, "maxAngularVelocity");
        if (maxAngularVelocity > 0f)
        {
            float currentSpeed = shipVelocity.magnitude;
            if (currentSpeed >= 0.01f)
            {
                float maxRotationPerFrame = maxAngularVelocity * Mathf.Deg2Rad * deltaTime;
                Vector3 currentDir = shipVelocity.normalized;
                Vector3 targetDir = newVelocity.normalized;
                float angleChange = Vector3.Angle(currentDir, targetDir) * Mathf.Deg2Rad;
                
                if (angleChange > maxRotationPerFrame)
                {
                    Vector3 rotationAxis = Vector3.Cross(currentDir, targetDir);
                    if (rotationAxis.magnitude < 0.001f)
                    {
                        rotationAxis = Vector3.forward;
                    }
                    rotationAxis = rotationAxis.normalized;
                    
                    Quaternion limitedRotation = Quaternion.AngleAxis(maxAngularVelocity * deltaTime, rotationAxis);
                    Vector3 limitedDir = limitedRotation * currentDir;
                    newVelocity = limitedDir * currentSpeed;
                }
            }
        }
        
        // 返回限制后的速度变化
        return newVelocity - shipVelocity;
    }
    
    /// <summary>
    /// 获取切向方向（垂直于径向，在速度方向上）
    /// 关键修复：使用与 TrajectoryPredictor 相同的实现，确保一致性
    /// </summary>
    Vector3 GetTangentialDirection(Vector3 radialDirection, Vector3 velocityDirection)
    {
        // 关键修复：使用与 TrajectoryPredictor 相同的实现
        // 计算两个可能的切向方向（顺时针和逆时针）
        Vector3 tangent1 = new Vector3(-radialDirection.y, radialDirection.x, 0f).normalized;
        Vector3 tangent2 = new Vector3(radialDirection.y, -radialDirection.x, 0f).normalized;
        
        // 选择与速度方向更接近的切向方向
        float dot1 = Vector3.Dot(velocityDirection, tangent1);
        float dot2 = Vector3.Dot(velocityDirection, tangent2);
        
        return (dot1 > dot2) ? tangent1 : tangent2;
    }
    
    string GetOtherEffectsInfo(Vector3 position, Vector3 velocity)
    {
        System.Text.StringBuilder info = new System.Text.StringBuilder();
        
        // 检查 PlanetGravityCapture
        foreach (var capture in planetCaptures)
        {
            Vector3 toPlanet = capture.transform.position - position;
            float dist = toPlanet.magnitude;
            float captureRadius = GetPrivateField<float>(capture, "captureRadius");
            
            if (dist <= captureRadius)
            {
                info.Append($"Capture:dist={dist:F2},radius={captureRadius:F2} ");
            }
        }
        
        // 检查 GravityHubDeflector
        foreach (var hub in hubDeflectors)
        {
            Vector3 toHub = hub.transform.position - position;
            float dist = toHub.magnitude;
            float deflectRadius = GetPrivateField<float>(hub, "deflectRadius");
            
            if (dist <= deflectRadius)
            {
                info.Append($"Hub:dist={dist:F2},radius={deflectRadius:F2} ");
            }
        }
        
        return info.ToString();
    }
    
    float GetSpeedMultiplier()
    {
        if (shipState == null) return 1.0f;
        
        System.Reflection.FieldInfo field = typeof(ShipState).GetField("speedMultiplier", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        
        if (field != null)
        {
            return (float)field.GetValue(shipState);
        }
        
        return 1.0f;
    }
    
    T GetPrivateField<T>(object obj, string fieldName)
    {
        System.Reflection.FieldInfo field = obj.GetType().GetField(fieldName, 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance | 
            System.Reflection.BindingFlags.Public);
        
        if (field != null)
        {
            return (T)field.GetValue(obj);
        }
        
        return default(T);
    }
    
    void SaveLogToFile()
    {
        try
        {
            string fullPath = Path.Combine(Application.dataPath, "..", logFilePath);
            File.WriteAllText(fullPath, logBuffer.ToString());
            Debug.Log($"[飞行数据记录] 日志已保存到: {fullPath}");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[飞行数据记录] 保存日志失败: {e.Message}");
        }
    }
    
}

