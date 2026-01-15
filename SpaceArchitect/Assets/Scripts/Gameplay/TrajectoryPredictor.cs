using UnityEngine;
using System.Collections.Generic;
using System.Reflection;
using System;
using System.Linq;

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
    [Tooltip("预测的时间步长（秒），必须与实际 FixedUpdate 一致（0.02秒）\n⚠️ 重要：使用 0.02 秒才能与实际游戏完全一致！")]
    [SerializeField] private float predictionTimeStep = 0.02f; // 修复：改为 0.02 秒，与实际 FixedUpdate 一致
    
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
    
    [Tooltip("是否使用虚线显示")]
    [SerializeField] private bool useDashedLine = true;
    
    [Tooltip("虚线参数：每段实线的长度（单位：Unity单位）")]
    [SerializeField] private float dashLength = 0.5f;
    
    [Tooltip("虚线参数：每段空白的长度（单位：Unity单位）")]
    [SerializeField] private float gapLength = 0.3f;
    
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
    
    // 速度缩放功能已移除（简化预测系统，提高准确性）
    // [Header("物理匹配设置")]
    // [Tooltip("考虑飞船的速度缩放系数（从 ShipState 读取）")]
    // [SerializeField] private bool useSpeedMultiplier = true;
    
    [Tooltip("引力常数调整系数（如果预测不准，可以微调此值）")]
    [SerializeField] private float gravityMultiplier = 1.0f;
    
    [Header("安全限制")]
    [Tooltip("最大预测距离（单位：Unity单位），防止飞船被弹射到无穷远导致死循环")]
    [SerializeField] private float maxPredictionDistance = 1000f;
    
    [Tooltip("最大预测步数（硬限制），防止无限循环")]
    [SerializeField] private int maxPredictionStepsHardLimit = 10000;
    
    [Tooltip("层级掩码（如果设置，只受特定 Layer 的行星影响）")]
    [SerializeField] private LayerMask gravitySourceLayerMask = -1; // -1 表示所有层
    
    [Header("引力源过滤")]
    [Tooltip("只考虑 CoreDeflector，忽略其他 NBody 的基础引力\n启用后，预测轨迹将只计算 CoreDeflector 的影响，不考虑行星等基础引力源")]
    [SerializeField] public bool onlyConsiderCoreDeflector = false;
    
    [Header("说明")]
    [Tooltip("Core（引力核心）= 可移动的星球\n通过 NBody.mass 统一计算引力\n预测准确度: 100%")]
    [SerializeField] private string _info = "Core 和 Planet 使用相同的引力计算";

    [Header("预测数据记录")]
    [Tooltip("是否启用预测数据记录（用于调试和对比）")]
    [SerializeField] private bool enablePredictionLogging = false;
    
    [Header("CoreDeflector 激活时机控制")]
    [Tooltip("Unity Trigger 的延迟帧数（用于模拟 OnTriggerEnter 到 FixedUpdate 应用效果的延迟）\n" +
             "⚠️ 关键修复：根据实际日志，Unity trigger 激活非常快，设为 0 测试\n" +
             "如果预测中激活太早，增大此值；如果激活太晚，减小此值")]
    [SerializeField] private int triggerDelayFrames = 0; // 关键修复：设为 0，因为实际激活非常快
    
    [Tooltip("预测日志文件路径（相对于项目根目录）")]
    [SerializeField] private string predictionLogFilePath = "PredictionDataLog.txt";
    
    [Tooltip("预测日志记录间隔（步数），越小越详细但文件越大")]
    [SerializeField] private int predictionLogInterval = 1; // 每步都记录

    private LineRenderer lineRenderer;
    private ShipState shipState;
    private NBody shipNBody;
    private GravityEngine gravityEngine;
    
    private bool isTrajectoryVisible = false;
    private float lastUpdateTime = 0f;
    
    // 预测数据记录
    private System.Text.StringBuilder predictionLogBuffer = new System.Text.StringBuilder();
    private bool isLoggingPrediction = false;
    
    /// <summary>
    /// 标记是否应该记录预测日志（只在实际发射时记录）
    /// </summary>
    private bool shouldLogPrediction = false;
    private float predictionStartTime = 0f;
    
    // 核心数据快照（性能优化：避免在循环中调用 Unity 组件）
    public struct GravitySourceSnapshot
    {
        public Vector3 position;
        public float nbodyEffectiveMass; // mass * massScale * physToWorldFactor³
        public bool hasDeflector;
        public DeflectorParams deflector;
        public bool hasHubDeflector;
        public HubDeflectorParams hubDeflector;
        public bool hasCapture;
        public CaptureParams capture;
        public ColliderBounds triggerBounds; // CoreDeflector 的 trigger 范围
    }
    
    public struct DeflectorParams
    {
        public Vector3 corePosition;
        public float coreEffectiveMass;
        public float guidanceStrength;
        public float targetOrbitRadius;
        public float maxAngularVelocity;
        public float minDistance;
        public float physicsUpdateInterval; // CoreDeflector 的更新间隔
        public float triggerRadius; // 关键修复：存储计算出的 trigger 半径，用于准确的检测
    }
    
    public struct HubDeflectorParams
    {
        public Vector3 hubPosition;
        public float deflectRadius;
        public float releaseRadius;
        public float deflectionStrength;
        public float maxVelocityChange;
        public float minVelocity;
    }
    
    public struct CaptureParams
    {
        public Vector3 planetPosition;
        public float planetMass;
        public float captureRadius;
        public float releaseRadius;
        public float orbitGuidanceStrength;
        public bool forceCounterClockwise;
    }
    
    public struct ColliderBounds
    {
        public Vector3 center;
        public Vector3 size;
        public bool isValid;
    }
    
    // 缓存的快照数据（纯数学数据，无 Unity 组件依赖）
    private List<GravitySourceSnapshot> cachedSources = new List<GravitySourceSnapshot>();
    
    // 保留原始组件引用（仅用于初始缓存，不在预测循环中使用）
    private List<NBody> gravitySources = new List<NBody>();
    private List<CoreDeflector> coreDeflectors = new List<CoreDeflector>();
    private List<GravityHubDeflector> gravityHubDeflectors = new List<GravityHubDeflector>();
    private List<PlanetGravityCapture> planetCaptures = new List<PlanetGravityCapture>();
    
    // 预览模式缓存的轨迹（发射前固定不变）
    private List<Vector3> cachedPreviewTrajectory = null;
    private Vector3 cachedLaunchVelocity = Vector3.zero;
    
    // 缓存Core位置，用于检测位置变化
    private Dictionary<CoreDeflector, Vector3> cachedCorePositions = new Dictionary<CoreDeflector, Vector3>();
    
    // 速度缩放功能已移除
    // private FieldInfo speedMultiplierField = null;
    // private bool hasCheckedSpeedMultiplierField = false;
    
    // 缓存反射字段信息（CoreDeflector、GravityHubDeflector 和 PlanetGravityCapture）
    private Dictionary<CoreDeflector, Dictionary<string, FieldInfo>> coreDeflectorFields = new Dictionary<CoreDeflector, Dictionary<string, FieldInfo>>();
    private Dictionary<GravityHubDeflector, Dictionary<string, FieldInfo>> gravityHubDeflectorFields = new Dictionary<GravityHubDeflector, Dictionary<string, FieldInfo>>();
    private Dictionary<PlanetGravityCapture, Dictionary<string, FieldInfo>> planetCaptureFields = new Dictionary<PlanetGravityCapture, Dictionary<string, FieldInfo>>();
    
    // FixedUpdate 模拟参数
    private float fixedUnscaledDeltaTime = 0.02f; // 默认 FixedUpdate 时间步长
    
    // 飞船半径（用于 trigger 检测补偿）
    private float shipRadius = 0.5f; // 默认值，将在 Start 中从 Collider 获取

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
        
        // 关键修复：获取飞船半径（用于 trigger 检测补偿）
        // Unity 的 trigger 检测基于 Rigidbody 边缘，而不是中心点
        Collider shipCollider = GetComponent<Collider>();
        if (shipCollider != null)
        {
            Bounds shipBounds = shipCollider.bounds;
            // 使用 bounds 的最大尺寸作为飞船半径
            shipRadius = Mathf.Max(shipBounds.size.x, shipBounds.size.y, shipBounds.size.z) * 0.5f;
            Debug.Log($"[预测] 飞船半径: {shipRadius:F3} (从 Collider bounds 获取)");
        }
        else
        {
            // 如果没有 Collider，尝试从子对象获取
            shipCollider = GetComponentInChildren<Collider>();
            if (shipCollider != null)
            {
                Bounds shipBounds = shipCollider.bounds;
                shipRadius = Mathf.Max(shipBounds.size.x, shipBounds.size.y, shipBounds.size.z) * 0.5f;
                Debug.Log($"[预测] 飞船半径: {shipRadius:F3} (从子对象 Collider 获取)");
            }
            else
            {
                Debug.LogWarning($"[预测] 无法获取飞船 Collider，使用默认半径 {shipRadius:F3}");
            }
        }
        
        // 设置初始可见性
        isTrajectoryVisible = showOnStart;
        lineRenderer.enabled = isTrajectoryVisible;
        
        // 缓存场景中的引力源
        CacheGravitySources();
        
        // 如果是预览模式且飞船未发射，自动切换到预览模式
        if (predictionMode == PredictionMode.Preview && shipState != null && shipState.CurrentState == ShipState.State.PreLaunch)
        {
            // 预览模式会在用户设置发射速度时更新
            Debug.Log("TrajectoryPredictor: 预览模式 - 等待发射参数");
        }
        
        // 订阅发射事件，确保使用实际发射速度重新预测
        if (EventManager.Instance != null)
        {
            EventManager.Instance.OnShipLaunched += OnShipLaunched;
            EventManager.Instance.OnShipStateChanged += OnShipStateChanged;
        }
    }
    
    /// <summary>
    /// 飞船状态变化事件回调
    /// 当飞船重置到 Setup 状态时，自动刷新引力源缓存（包括 Core 位置和 trigger 范围）
    /// </summary>
    private void OnShipStateChanged(ShipState.State oldState, ShipState.State newState, GameObject ship)
    {
        // 只处理自己的飞船
        if (ship != gameObject)
        {
            return;
        }
        
        // 当飞船重置到 Setup 状态时，刷新缓存
        // 这样可以确保 Core 的位置和 trigger 范围被重新定位
        if (newState == ShipState.State.Setup)
        {
            RefreshGravitySources();
            Debug.Log("[预测] 飞船已重置到 Setup 状态，已刷新引力源缓存（包括 Core 位置和 trigger 范围）");
        }
    }
    
    /// <summary>
    /// 飞船发射事件处理：使用实际发射速度重新预测
    /// 关键修复：确保预测数据和实际数据使用完全相同的初始条件
    /// </summary>
    private void OnShipLaunched(Vector3 actualLaunchVelocity, GameObject ship)
    {
        // 只处理自己的飞船
        if (ship != gameObject) return;
        
        // 关键修复：使用实际发射速度重新预测
        // 注意：actualLaunchVelocity 是**未缩放**的速度（与实际游戏一致）
        // 速度缩放会在 PredictFullTrajectory 中每一步应用，与实际游戏的 FixedUpdate 逻辑一致
        Vector3 launchPosition = transform.position;
        
        // 关键修复：使用更高精度记录实际发射速度，确保完全一致
        Debug.Log($"[预测] 飞船已发射！使用实际发射速度（未缩放）重新预测 - 位置: ({launchPosition.x:F6}, {launchPosition.y:F6}, {launchPosition.z:F6}), 速度: ({actualLaunchVelocity.x:F6}, {actualLaunchVelocity.y:F6}, {actualLaunchVelocity.z:F6}), 速度大小: {actualLaunchVelocity.magnitude:F6}");
        
        // 关键修复：先标记应该记录预测日志，然后再调用 SetPreviewTrajectory
        // 因为 SetPreviewTrajectory 会立即调用 PredictFullTrajectory，需要在此之前设置标志
        if (enablePredictionLogging)
        {
            // 停止之前的记录（如果有，可能是预览时的记录）
            if (isLoggingPrediction)
            {
                StopPredictionLogging();
            }
            // 关键修复：先设置标志，然后再调用 SetPreviewTrajectory
            // 这样 PredictFullTrajectory 就能检测到 shouldLogPrediction = true
            shouldLogPrediction = true;
            Debug.Log($"[预测] 已标记使用实际发射速度（未缩放）记录 - 位置: {launchPosition}, 速度: {actualLaunchVelocity}");
        }
        
        // 关键修复：使用实际发射速度（未缩放）重新预测
        // 速度缩放会在 PredictFullTrajectory 中每一步应用，与实际游戏的 FixedUpdate 逻辑一致
        SetPreviewTrajectory(launchPosition, actualLaunchVelocity);
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
        
        // 如果启用虚线，创建虚线材质
        if (useDashedLine)
        {
            CreateDashedLineMaterial();
        }
        
        // 设置其他属性
        lineRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        lineRenderer.receiveShadows = false;
        lineRenderer.alignment = LineAlignment.View; // 面向摄像机
    }
    
    /// <summary>
    /// 创建虚线材质（使用纹理平铺实现虚线效果）
    /// </summary>
    private void CreateDashedLineMaterial()
    {
        // 创建虚线纹理（1D纹理，用于平铺）
        int textureWidth = 128; // 增加分辨率，让虚线更平滑
        Texture2D dashTexture = new Texture2D(textureWidth, 1, TextureFormat.RGBA32, false);
        dashTexture.filterMode = FilterMode.Point; // 使用点过滤，确保清晰的虚线边缘
        dashTexture.wrapMode = TextureWrapMode.Repeat; // 重复模式，用于平铺
        
        // 计算实线和空白的像素比例
        float totalLength = dashLength + gapLength;
        int dashPixels = Mathf.RoundToInt((dashLength / totalLength) * textureWidth);
        int gapPixels = textureWidth - dashPixels;
        
        // 填充纹理：实线部分为白色（不透明），空白部分为透明
        Color[] pixels = new Color[textureWidth];
        for (int i = 0; i < textureWidth; i++)
        {
            if (i < dashPixels)
            {
                // 实线部分：白色（不透明）
                pixels[i] = Color.white;
            }
            else
            {
                // 空白部分：透明
                pixels[i] = Color.clear;
            }
        }
        
        dashTexture.SetPixels(pixels);
        dashTexture.Apply();
        
        // 创建材质（使用支持纹理平铺和透明的Shader）
        // 优先使用 Unlit/Transparent，如果不支持则使用 Sprites/Default
        Shader shader = Shader.Find("Unlit/Transparent");
        if (shader == null)
        {
            shader = Shader.Find("Sprites/Default");
        }
        if (shader == null)
        {
            shader = Shader.Find("Standard");
        }
        
        Material dashMaterial = new Material(shader);
        dashMaterial.mainTexture = dashTexture;
        dashMaterial.color = lineColor;
        
        // 设置纹理平铺（根据虚线参数调整）
        // 平铺值越大，虚线越密集；平铺值越小，虚线越稀疏
        // 使用 1 / totalLength 作为基础平铺值，让虚线长度与实际参数对应
        float baseTiling = 1f / totalLength; // 基础平铺值
        float tiling = baseTiling * 5f; // 放大倍数，可以根据视觉效果调整
        dashMaterial.mainTextureScale = new Vector2(tiling, 1f);
        
        // 如果Shader支持，设置渲染模式为透明
        if (dashMaterial.HasProperty("_Mode"))
        {
            dashMaterial.SetFloat("_Mode", 3); // 3 = Transparent
            dashMaterial.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            dashMaterial.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            dashMaterial.SetInt("_ZWrite", 0);
            dashMaterial.DisableKeyword("_ALPHATEST_ON");
            dashMaterial.EnableKeyword("_ALPHABLEND_ON");
            dashMaterial.DisableKeyword("_ALPHAPREMULTIPLY_ON");
            dashMaterial.renderQueue = 3000;
        }
        
        // 应用材质
        lineRenderer.material = dashMaterial;
        
        Debug.Log($"[轨迹预测] 已创建虚线材质 - 实线长度: {dashLength}, 空白长度: {gapLength}, 平铺: {tiling:F2}");
    }

    /// <summary>
    /// 缓存场景中的所有引力源和相关组件，并创建快照（性能优化）
    /// </summary>
    private void CacheGravitySources()
    {
        gravitySources.Clear();
        coreDeflectors.Clear();
        gravityHubDeflectors.Clear();
        planetCaptures.Clear();
        cachedSources.Clear();
        
        // 获取 GravityEngine 参数（一次性获取，避免重复调用）
        float massScale = gravityEngine != null ? gravityEngine.massScale : 1.0f;
        float physToWorldFactor = gravityEngine != null ? gravityEngine.physToWorldFactor : 1.0f;
        float factor3 = physToWorldFactor * physToWorldFactor * physToWorldFactor;
        
        // 查找场景中所有的 NBody 对象（排除飞船自己）
        NBody[] allNBodies = FindObjectsOfType<NBody>();
        
        foreach (NBody nbody in allNBodies)
        {
            // 跳过飞船自己
            if (nbody == shipNBody)
            {
                continue;
            }
            
            // 检查层级掩码
            if (gravitySourceLayerMask != -1 && (gravitySourceLayerMask & (1 << nbody.gameObject.layer)) == 0)
            {
                continue;
            }
            
            // 如果只考虑 CoreDeflector，先检查是否有 CoreDeflector
            CoreDeflector deflector = nbody.GetComponent<CoreDeflector>();
            if (onlyConsiderCoreDeflector && deflector == null)
            {
                // 跳过没有 CoreDeflector 的 NBody
                continue;
            }
            
            // ========== 检查 Core 是否在基地范围内（如果 Core 在基地范围内，不纳入预测计算）==========
            // 这与实际游戏行为一致：CoreGravityDisabler 会禁用 CoreDeflector.enabled = false
            // 所以在预测系统中，我们也应该排除这些 Core，不纳入预测计算
            if (deflector != null)
            {
                CoreGravityDisabler disabler = nbody.GetComponent<CoreGravityDisabler>();
                if (disabler != null && IsCoreInStationRange(disabler))
                {
                    // Core 在基地范围内，跳过（不纳入预测计算）
                    // 这与实际游戏行为一致：CoreGravityDisabler 会禁用 CoreDeflector.enabled = false
                    if (enablePredictionLogging)
                    {
                        Debug.Log($"[预测] 跳过 Core {nbody.gameObject.name} - 在基地范围内，不纳入预测计算");
                    }
                    continue;
                }
            }
            
            // 只处理有质量的物体（或者有 CoreDeflector 的物体）
            if (nbody.mass <= 0.001f && deflector == null)
            {
                continue;
            }
            
            gravitySources.Add(nbody);
            
            // 创建快照
            GravitySourceSnapshot snapshot = new GravitySourceSnapshot();
            snapshot.position = nbody.transform.position;
            
            // 如果只考虑 CoreDeflector，将 NBody 的基础引力设为 0
            if (onlyConsiderCoreDeflector)
            {
                snapshot.nbodyEffectiveMass = 0f; // 忽略 NBody 的基础引力
            }
            else
            {
                snapshot.nbodyEffectiveMass = nbody.mass * massScale * gravityMultiplier * factor3;
            }
            
            snapshot.hasDeflector = false;
            snapshot.hasHubDeflector = false;
            snapshot.hasCapture = false;
            
            // 缓存 CoreDeflector 参数
            if (deflector != null)
            {
                coreDeflectors.Add(deflector);
                snapshot.hasDeflector = true;
                float physicsInterval = GetPrivateField<float>(deflector, "physicsUpdateInterval");
                if (physicsInterval <= 0f) physicsInterval = 0.02f; // 默认0.02秒
                
                snapshot.deflector = new DeflectorParams
                {
                    corePosition = deflector.transform.position,
                    coreEffectiveMass = GetPrivateField<float>(deflector, "coreEffectiveMass"),
                    guidanceStrength = GetPrivateField<float>(deflector, "guidanceStrength"),
                    targetOrbitRadius = GetPrivateField<float>(deflector, "targetOrbitRadius"),
                    maxAngularVelocity = GetPrivateField<float>(deflector, "maxAngularVelocity"),
                    minDistance = GetPrivateField<float>(deflector, "minDistance"),
                    physicsUpdateInterval = physicsInterval,
                    triggerRadius = 0f // 将在下面计算并设置
                };
                
                // 缓存 trigger 范围
                Collider trigger = deflector.GetComponent<Collider>();
                if (trigger != null)
                {
                    Bounds bounds = trigger.bounds;
                    
                    // 关键修复：根据 Collider 类型使用更准确的半径计算
                    // 使用 bounds 中心到各个顶点的最大距离，确保覆盖所有情况
                    float triggerRadius = 0f;
                    if (trigger is SphereCollider)
                    {
                        SphereCollider sphere = trigger as SphereCollider;
                        // 考虑缩放
                        float scale = Mathf.Max(trigger.transform.lossyScale.x, trigger.transform.lossyScale.y, trigger.transform.lossyScale.z);
                        triggerRadius = sphere.radius * scale;
                        // 关键修复：根据实际日志，安全边距 50% 太大，改为 15%
                        // 实际激活距离 8.26，计算半径约 7.04，差距约 17%，所以 15% 应该足够
                        triggerRadius *= 1.15f;
                    }
                    else if (trigger is BoxCollider)
                    {
                        BoxCollider box = trigger as BoxCollider;
                        // 关键修复：使用 bounds 的对角线长度的一半，而不是简单的 size.magnitude
                        // 这样可以确保覆盖 BoxCollider 的所有角落
                        Vector3 extents = bounds.extents; // 这是 size 的一半
                        // 计算中心到最远顶点的距离（对角线长度的一半）
                        triggerRadius = extents.magnitude;
                        // 关键修复：根据实际日志，安全边距 50% 太大，改为 15%
                        // 实际激活距离 8.26，计算半径约 7.04，差距约 17%，所以 15% 应该足够
                        triggerRadius *= 1.15f;
                    }
                    else
                    {
                        // 默认使用 bounds 的最大距离（中心到最远顶点的距离）
                        Vector3 extents = bounds.extents;
                        triggerRadius = extents.magnitude;
                        // 关键修复：根据实际日志，安全边距 50% 太大，改为 15%
                        triggerRadius *= 1.15f;
                    }
                    
                    snapshot.triggerBounds = new ColliderBounds
                    {
                        center = bounds.center,
                        size = bounds.size,
                        isValid = true
                    };
                    
                    // 关键修复：存储计算出的半径 + 飞船半径补偿，用于准确的检测
                    // Unity 的 trigger 检测基于 Rigidbody 边缘，所以需要加上飞船半径
                    snapshot.deflector.triggerRadius = triggerRadius + shipRadius;
                    
                    // 调试：输出 trigger 范围信息
                    if (Time.frameCount % 60 == 0)
                    {
                        Debug.Log($"[预测] CoreDeflector trigger 范围 - 中心: {bounds.center}, 大小: {bounds.size}, 计算半径: {triggerRadius:F2}");
                    }
                }
                else
                {
                    Debug.LogWarning($"[预测] CoreDeflector {deflector.gameObject.name} 没有 Collider 组件！");
                }
            }
            
            // 缓存 GravityHubDeflector 参数
            GravityHubDeflector hubDeflector = nbody.GetComponent<GravityHubDeflector>();
            if (hubDeflector != null)
            {
                gravityHubDeflectors.Add(hubDeflector);
                snapshot.hasHubDeflector = true;
                snapshot.hubDeflector = new HubDeflectorParams
                {
                    hubPosition = hubDeflector.transform.position,
                    deflectRadius = GetPrivateField<float>(hubDeflector, "deflectRadius"),
                    releaseRadius = GetPrivateField<float>(hubDeflector, "releaseRadius"),
                    deflectionStrength = GetPrivateField<float>(hubDeflector, "deflectionStrength"),
                    maxVelocityChange = GetPrivateField<float>(hubDeflector, "maxVelocityChange"),
                    minVelocity = GetPrivateField<float>(hubDeflector, "minVelocity")
                };
            }
            
            // 缓存 PlanetGravityCapture 参数
            PlanetGravityCapture capture = nbody.GetComponent<PlanetGravityCapture>();
            if (capture != null)
            {
                planetCaptures.Add(capture);
                snapshot.hasCapture = true;
                snapshot.capture = new CaptureParams
                {
                    planetPosition = capture.transform.position,
                    planetMass = nbody.mass, // 使用 NBody 的质量
                    captureRadius = GetPrivateField<float>(capture, "captureRadius"),
                    releaseRadius = GetPrivateField<float>(capture, "releaseRadius"),
                    orbitGuidanceStrength = GetPrivateField<float>(capture, "orbitGuidanceStrength"),
                    forceCounterClockwise = GetPrivateField<bool>(capture, "forceCounterClockwise")
                };
            }
            
            cachedSources.Add(snapshot);
        }
        
        // 获取 FixedUpdate 时间步长
        fixedUnscaledDeltaTime = Time.fixedUnscaledDeltaTime;
        if (fixedUnscaledDeltaTime <= 0f)
        {
            fixedUnscaledDeltaTime = 0.02f; // 默认值
        }
        
        int coreDeflectorCount = cachedSources.Count(s => s.hasDeflector);
        string modeInfo = onlyConsiderCoreDeflector ? "（仅CoreDeflector模式）" : "";
        Debug.Log($"[预测] 已缓存 {cachedSources.Count} 个引力源快照{modeInfo}，其中 {coreDeflectorCount} 个有 CoreDeflector");
        
        // 更新 Core 位置缓存（用于实时检测位置变化）
        UpdateCorePositionCache();
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
    /// 设置轨迹可见性
    /// </summary>
    /// <param name="visible">是否可见</param>
    public void SetTrajectoryVisible(bool visible)
    {
        isTrajectoryVisible = visible;
        lineRenderer.enabled = visible;
        
        if (visible)
        {
            // 如果设置为可见，立即更新一次轨迹
            UpdateTrajectory();
        }
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
        // 关键修复：实时检测 Core 位置变化
        // 如果 Core 位置发生变化，立即刷新缓存（确保 trigger 范围圆圈实时更新）
        bool corePositionChanged = CheckCorePositionsChanged();
        if (corePositionChanged)
        {
            CacheGravitySources();
            Debug.Log("[预测] 检测到 Core 位置变化，已刷新缓存（包括 trigger 范围）");
        }
        else
        {
            // 如果没有位置变化，按间隔刷新（性能优化）
            int refreshInterval = (shipState != null && shipState.CurrentState == ShipState.State.PreLaunch) ? 30 : 60;
            if (Time.frameCount % refreshInterval == 0)
            {
                CacheGravitySources();
            }
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
    /// 完整预测逻辑（纯数学运算，无 Unity 组件依赖）
    /// 严格按照执行顺序：GravityEngine → CoreDeflector → ShipState → Capture/Hub
    /// </summary>
    private List<Vector3> SimulateTrajectory(Vector3 startPosition, Vector3 startVelocity)
    {
        return PredictFullTrajectory(startPosition, startVelocity, predictionSteps, predictionTimeStep);
    }
    
    /// <summary>
    /// 完整预测逻辑实现（纯数学运算）
    /// </summary>
    private List<Vector3> PredictFullTrajectory(Vector3 startPos, Vector3 startVel, int maxSteps, float dt)
    {
        // 关键修复：强制使用 0.02 秒，确保与实际 FixedUpdate 完全一致
        float actualDt = 0.02f; // FixedUpdate 的时间步长
        if (Mathf.Abs(dt - actualDt) > 0.001f)
        {
            Debug.LogWarning($"[预测] PredictFullTrajectory 时间步长不一致！传入的 dt={dt}，强制使用 {actualDt}。");
            dt = actualDt;
        }
        
        List<Vector3> predictionPoints = new List<Vector3>();
        
        Vector3 currentPos = startPos;
        Vector3 currentVel = startVel;
        
        // 速度缩放功能已移除
        // float speedMultiplier = GetSpeedMultiplier();
        float speedMultiplier = 1.0f; // 固定为1.0，不再应用速度缩放
        
        // 计算实际的步数（不超过最大时间和硬限制）
        int actualSteps = Mathf.Min(
            maxSteps, 
            Mathf.FloorToInt(maxPredictionTime / dt),
            maxPredictionStepsHardLimit
        );
        
        // 计算 FixedUpdate 和 Update 的调用频率
        int fixedUpdateInterval = Mathf.Max(1, Mathf.RoundToInt(fixedUnscaledDeltaTime / dt));
        float detectionInterval = 0.1f; // PlanetGravityCapture 默认检测间隔
        int updateInterval = Mathf.Max(1, Mathf.RoundToInt(detectionInterval / dt));
        
        // 修复：跟踪每个 CoreDeflector 的状态
        // 1. 是否已进入trigger（用于延迟应用，模拟Unity Trigger的延迟）
        // 2. 进入trigger的步数（用于计算延迟）
        // 3. 上次应用的步数（用于physicsUpdateInterval检查）
        Dictionary<int, bool> deflectorEntered = new Dictionary<int, bool>(); // 是否已进入trigger
        Dictionary<int, int> deflectorEnterStep = new Dictionary<int, int>(); // 进入trigger的步数
        Dictionary<int, int> deflectorLastUpdateStep = new Dictionary<int, int>(); // 上次应用的步数
        int sourceIndex = 0;
        foreach (var source in cachedSources)
        {
            if (source.hasDeflector)
            {
                deflectorEntered[sourceIndex] = false; // 初始未进入
                deflectorEnterStep[sourceIndex] = -1; // 初始未进入
                deflectorLastUpdateStep[sourceIndex] = -1; // 初始未应用
            }
            sourceIndex++;
        }
        
        // 记录起始位置，用于距离检查
        Vector3 startPosition = startPos;
        
        // 关键修复：只在 shouldLogPrediction 为 true 时才开始记录
        // shouldLogPrediction 只在 OnShipLaunched 事件中设置为 true
        // 这样可以确保预测日志记录使用的是实际发射速度，而不是预览速度
        if (enablePredictionLogging && shouldLogPrediction && !isLoggingPrediction)
        {
            StartPredictionLogging(startPos, startVel, dt);
            // 记录后重置标志，避免重复记录
            shouldLogPrediction = false;
        }
        
        // 调试：输出初始状态（每60帧输出一次，避免日志过多）
        if (Time.frameCount % 60 == 0)
        {
            Debug.Log($"[预测] 开始预测 - 起始位置: {startPos}, 起始速度: {startVel}, 速度大小: {startVel.magnitude:F3}");
            Debug.Log($"[预测] 参数 - dt: {dt}, fixedUpdateInterval: {fixedUpdateInterval}, speedMultiplier: {speedMultiplier}");
            Debug.Log($"[预测] 引力源数量: {cachedSources.Count}, fixedUnscaledDeltaTime: {fixedUnscaledDeltaTime}");
        }
        
        // 添加起始点
        predictionPoints.Add(currentPos);
        
        // 迭代模拟
        // ⚠️ 关键修复：改用半隐式欧拉法，而不是 LeapFrog
        // 原因：LeapFrog 与 CoreDeflector 的叠加逻辑冲突，导致物理计算不一致
        // 半隐式欧拉法：v1 = v0 + a * dt, r1 = r0 + v1 * dt
        
        // ========== 执行顺序说明 ==========
        // 1. GravityEngine（基础引力，FixedUpdate）- 全局，始终生效
        //    新优先级规则：如果飞船在 CoreDeflector trigger 范围内，NBody 基础引力会被减弱（保留30%）
        // 2. CoreDeflector（叠加引导，FixedUpdate）- 仅在trigger范围内，叠加效果
        //    如果生效，设置标志：isInAnyCoreDeflectorTrigger = true
        //    新优先级规则：在应用 CoreDeflector 时，会减弱之前应用的 NBody 基础引力影响
        // 3. 更新位置
        // 4. PlanetGravityCapture（覆盖引导，FixedUpdate）- 在capture范围内，覆盖效果（Lerp）
        //    检查标志：如果 isInAnyCoreDeflectorTrigger = true，则跳过
        // 
        // ========== 新优先级规则：CoreDeflector 优先级高于 PlanetGravityCapture 和 NBody ==========
        // 核心规则：
        //   1. 如果飞船在 CoreDeflector 的 trigger 范围内，PlanetGravityCapture 会被跳过
        //   2. 如果飞船在 CoreDeflector 的 trigger 范围内，NBody 基础引力会被减弱（保留30%，减弱70%）
        //   3. 这样可以确保 CoreDeflector 的效果更明显，优先级更高
        // 
        // 情况1：飞船在叠加区域内（既在 Core trigger 内，也在 Planet captureRadius 内）
        //   → NBody 基础引力被减弱（步骤1，保留30%，减弱70%）
        //   → CoreDeflector 生效（步骤2，叠加效果，弹弓偏转）
        //   → PlanetGravityCapture 被跳过（步骤4，因为 isInAnyCoreDeflectorTrigger = true）
        //   → 最终效果：CoreDeflector 的弹弓偏转效果占主导，NBody 引力影响较小
        // 
        // 情况2：飞船只在 Planet 区域内（不在 Core trigger 内，但在 Planet captureRadius 内）
        //   → NBody 基础引力正常生效（步骤1，100%）
        //   → CoreDeflector 不生效（不在 trigger 范围内）
        //   → PlanetGravityCapture 生效（步骤4，覆盖效果，轨道引导）
        //   → 最终效果：PlanetGravityCapture 的轨道引导，NBody 引力正常
        // 
        // 情况3：飞船只在 Core 区域内（在 Core trigger 内，但不在 Planet captureRadius 内）
        //   → NBody 基础引力被减弱（步骤1，保留30%，减弱70%）
        //   → CoreDeflector 生效（步骤2，叠加效果）
        //   → PlanetGravityCapture 不生效（不在 captureRadius 内）
        //   → 最终效果：CoreDeflector 的弹弓偏转效果占主导，NBody 引力影响较小
        // 
        // 设计意图：
        //   - CoreDeflector 优先：在叠加区域内，弹弓偏转优先于轨道引导和 NBody 引力
        //   - 避免冲突：防止多个系统同时作用导致轨迹不稳定
        //   - 保留原逻辑：如果不在 Core trigger 范围内，PlanetGravityCapture 和 NBody 正常生效
        // 
        // 如果启用 onlyConsiderCoreDeflector，则跳过步骤1和4，只执行步骤2
        
        for (int i = 0; i < actualSteps; i++)
        {
            // ========== 区域叠加判定标志 ==========
            // 记录当前步骤是否在任何 CoreDeflector 的 trigger 范围内
            // 用于决定是否应用 PlanetGravityCapture 和 NBody 基础引力
            // 新优先级规则：CoreDeflector 优先级高于 PlanetGravityCapture 和 NBody
            // 如果此标志为 true：
            //   - PlanetGravityCapture 会被跳过（步骤4）
            //   - NBody 基础引力会被减弱或跳过（步骤1，可选）
            bool isInAnyCoreDeflectorTrigger = false;
            
            // ========== 1. GravityEngine 阶段（基础引力，FixedUpdate）==========
            // 作用：计算所有NBody的基础引力（全局，始终生效）
            // 优先级：最低（基础层），其他系统在此基础上叠加或覆盖
            // 
            // 新优先级规则：如果飞船在 CoreDeflector trigger 范围内，减弱 NBody 基础引力
            // 这样可以确保 CoreDeflector 的效果更明显
            // 
            // 注意：如果 onlyConsiderCoreDeflector = true，跳过此步骤
            Vector3 gravityAcc = Vector3.zero;
            if (!onlyConsiderCoreDeflector)
            {
                // 先计算基础引力（在检查 CoreDeflector 之前）
                // 但我们需要先检查是否在 CoreDeflector trigger 范围内，以决定是否减弱 NBody 引力
                // 这里先计算，后面会根据 isInAnyCoreDeflectorTrigger 调整
                foreach (var source in cachedSources)
                {
                    Vector3 toSource = source.position - currentPos;
                    float distSq = toSource.sqrMagnitude;
                    if (distSq < 0.01f) continue;
                    
                    float mag = source.nbodyEffectiveMass / distSq;
                    gravityAcc += toSource.normalized * mag;
                }
                
                // 半隐式欧拉法：v1 = v0 + a * dt
                // 注意：如果飞船在 CoreDeflector trigger 范围内，NBody 引力会在步骤2之后被调整
                currentVel += gravityAcc * dt;
            }
            
            // 限制在 XY 平面
            currentVel.z = 0f;
            
            // 记录 CoreDeflector 效果（用于日志）
            Dictionary<int, string> coreDeflectorInfo = new Dictionary<int, string>();
            
            // ========== 2. CoreDeflector 阶段（叠加引导，FixedUpdate）==========
            // 作用：在Core的trigger范围内，叠加引力和引导效果（弹弓偏转）
            // 作用方式：叠加操作（增量） - newVelocity = oldVelocity + velocityChange
            // 作用范围：仅在Core的Trigger Collider范围内
            // 优先级：中等（在基础引力之后，叠加效果）
            // 
            // 关键：CoreDeflector 在 GravityEngine 计算完加速度并更新速度后执行
            // 它读取的是 GravityEngine 已经更新后的速度，然后叠加自己的加速度
            // 重要修复：CoreDeflector 只在 FixedUpdate 中执行（每 fixedUnscaledDeltaTime 一次）
            // 在预测中，我们只在 fixedUpdateInterval 的倍数步骤中应用，使用 fixedUnscaledDeltaTime 作为时间步长
            // 
            // 区域叠加处理：如果飞船在 trigger 范围内，设置 isInAnyCoreDeflectorTrigger = true
            // 这个标志会在步骤4中用于决定是否跳过 PlanetGravityCapture
            if (i % fixedUpdateInterval == 0)
            {
                // 关键修复：按固定顺序处理多个 CoreDeflector（按位置排序，确保确定性）
                // 这样可以避免 Unity Trigger 调用顺序不确定导致的随机性
                var deflectorSources = new List<(int index, GravitySourceSnapshot source, float sortKey)>();
                sourceIndex = 0;
                foreach (var source in cachedSources)
                {
                    if (source.hasDeflector)
                    {
                        // 使用位置作为排序键（先按 X，再按 Y），确保顺序固定
                        float sortKey = source.deflector.corePosition.x * 1000f + source.deflector.corePosition.y;
                        deflectorSources.Add((sourceIndex, source, sortKey));
                    }
                    sourceIndex++;
                }
                
                // 按排序键排序，确保处理顺序固定
                deflectorSources.Sort((a, b) => a.sortKey.CompareTo(b.sortKey));
                
                // 按固定顺序处理
                foreach (var (idx, source, _) in deflectorSources)
                {
                    // 关键修复：优先使用 AABB 检测（与实际游戏一致）
                    // Unity 的 trigger 检测基于 Collider 的 bounds，所以应该优先使用 AABB 检测
                    bool inTrigger = false;
                    float distToCore = 0f;
                    float triggerRadius = source.deflector.triggerRadius;
                    
                    // 关键修复：计算到 Core 的距离时，考虑飞船半径补偿
                    // Unity 的 trigger 检测基于 Rigidbody 边缘，而不是中心点
                    // 所以需要从"飞船边缘到 Core 边缘"的距离来判断
                    Vector3 toCore = source.deflector.corePosition - currentPos;
                    distToCore = toCore.magnitude;
                    
                    // 关键修复：优先使用 AABB 检测，但考虑飞船半径
                    // 实际游戏中，Unity 的 trigger.bounds.Contains() 检测的是 Rigidbody 的 bounds
                    // 所以飞船的边缘碰到 trigger 的 bounds 就会触发
                    if (source.triggerBounds.isValid)
                    {
                        // 检查飞船边缘（中心点 ± 半径）是否在 trigger bounds 内
                        // 简化：只检查中心点，但 trigger 半径已经包含了飞船半径补偿
                        inTrigger = IsInTriggerBounds(currentPos, source.triggerBounds);
                    }
                    
                    // 如果 AABB 检测失败，使用距离检测作为备用方案
                    // 关键修复：triggerRadius 已经包含了飞船半径补偿（在 CacheGravitySources 中）
                    if (!inTrigger)
                    {
                        if (triggerRadius <= 0f)
                        {
                            // 如果未设置，回退到使用 bounds 的最大值（兼容旧代码）
                            if (source.triggerBounds.isValid)
                            {
                                Vector3 extents = source.triggerBounds.size * 0.5f;
                                triggerRadius = extents.magnitude * 1.15f + shipRadius; // 关键修复：加上飞船半径补偿
                            }
                            else
                            {
                                triggerRadius = 10f + shipRadius; // 默认值 + 飞船半径
                            }
                        }
                        // 关键修复：距离检测时，triggerRadius 已经包含了飞船半径补偿
                        inTrigger = distToCore <= triggerRadius;
                    }
                    
                    // 调试：如果两种检测方法结果不一致，记录警告
                    if (enablePredictionLogging && i % (predictionLogInterval * 10) == 0 && source.triggerBounds.isValid)
                    {
                        bool aabbResult = IsInTriggerBounds(currentPos, source.triggerBounds);
                        bool distResult = distToCore <= triggerRadius;
                        if (aabbResult != distResult)
                        {
                            Debug.LogWarning($"[预测] CoreDeflector[{idx}] 检测方法不一致 - AABB: {aabbResult}, 距离: {distResult}, 距离值: {distToCore:F2}, trigger半径: {triggerRadius:F2}");
                        }
                    }
                    
                    // 调试：记录 trigger 检测的详细信息（仅在启用日志记录且满足间隔时）
                    if (enablePredictionLogging && i % predictionLogInterval == 0)
                    {
                        bool wasEntered = deflectorEntered.ContainsKey(idx) && deflectorEntered[idx];
                        string triggerStatus = inTrigger ? "IN" : "OUT";
                        string enteredStatus = wasEntered ? "ENTERED" : "NOT_ENTERED";
                        // 注意：这里不直接输出到日志，而是记录到 coreDeflectorInfo 中，在 LogPredictionStep 中统一输出
                    }
                    
                    // ========== 简化版本：移除复杂的延迟机制 ==========
                    // 关键修复：根据实际日志，Unity trigger 激活非常快，移除延迟机制
                    // 如果进入 trigger，立即检查是否应该应用（基于 physicsUpdateInterval）
                    
                    // 计算这个 CoreDeflector 的更新间隔（以步数为单位，与实际游戏一致）
                    int deflectorUpdateInterval = Mathf.Max(1, Mathf.RoundToInt(source.deflector.physicsUpdateInterval / fixedUnscaledDeltaTime));
                    
                    // 关键修复：简化逻辑，移除复杂的延迟机制
                    // 如果进入 trigger，立即检查是否应该应用（基于 physicsUpdateInterval）
                    if (inTrigger)
                    {
                        // ========== 区域叠加处理：设置标志 ==========
                        // 记录：当前飞船在任何 CoreDeflector 的 trigger 范围内
                        // 这个标志会在步骤4中用于决定是否跳过 PlanetGravityCapture
                        // 如果飞船在叠加区域内（既在 Core trigger 内，也在 Planet captureRadius 内），
                        // PlanetGravityCapture 会被跳过，只有 CoreDeflector 的弹弓偏转效果生效
                        isInAnyCoreDeflectorTrigger = true;
                        
                        // 检测 trigger 状态变化（用于日志）
                        bool wasInTrigger = deflectorEntered.ContainsKey(idx) && deflectorEntered[idx];
                        
                        if (!wasInTrigger)
                        {
                            // 模拟 OnTriggerEnter：飞船刚进入 trigger
                            deflectorEntered[idx] = true;
                            deflectorEnterStep[idx] = i;
                            deflectorLastUpdateStep[idx] = -1; // 初始未应用
                            
                            // 调试：记录进入 trigger 的信息
                            if (enablePredictionLogging)
                            {
                                float currentTime = i * dt;
                                Debug.Log($"[预测] 🔵 CoreDeflector[{idx}] OnTriggerEnter - 步数: {i}, 时间: {currentTime:F4}, 位置: ({currentPos.x:F3}, {currentPos.y:F3}), 距离: {distToCore:F2}, trigger半径: {triggerRadius:F2}");
                            }
                        }
                        
                        // 检查是否满足更新间隔
                        int lastUpdateStep = deflectorLastUpdateStep.ContainsKey(idx) ? deflectorLastUpdateStep[idx] : -1;
                        int stepsSinceLastUpdate = (lastUpdateStep < 0) ? 0 : (i - lastUpdateStep);
                        
                        // 关键修复：移除 triggerDelayFrames，立即应用（如果满足更新间隔）
                        if (lastUpdateStep < 0 || stepsSinceLastUpdate >= deflectorUpdateInterval)
                        {
                            // ========== 新优先级规则：CoreDeflector 优先于 NBody 基础引力 ==========
                            // 如果飞船在 CoreDeflector trigger 范围内，减弱之前应用的 NBody 基础引力
                            // 这样可以确保 CoreDeflector 的效果更明显，优先级更高
                            // 方法：在应用 CoreDeflector 之前，先抵消一部分 NBody 基础引力的影响
                            if (!onlyConsiderCoreDeflector && gravityAcc.magnitude > 0.001f)
                            {
                                // 计算 NBody 基础引力对速度的影响
                                Vector3 nbodyVelocityChange = gravityAcc * dt;
                                // 减弱 NBody 引力的影响（保留 30%，减弱 70%）
                                // 这样 CoreDeflector 的效果会更明显
                                float nbodyReductionFactor = 0.3f; // 保留 30% 的 NBody 引力
                                Vector3 reducedNbodyChange = nbodyVelocityChange * nbodyReductionFactor;
                                // 调整速度：先减去完整的 NBody 影响，再加上减弱后的影响
                                currentVel = currentVel - nbodyVelocityChange + reducedNbodyChange;
                            }
                            
                            // 应用 CoreDeflector 效果（使用 fixedUnscaledDeltaTime，与实际游戏一致）
                            Vector3 oldVel = currentVel;
                            currentVel = ApplyCoreDeflector(currentPos, currentVel, source.deflector, fixedUnscaledDeltaTime);
                            deflectorLastUpdateStep[idx] = i; // 更新上次应用的步数
                            
                            // 记录效果信息（用于日志）
                            float gravAccMag = source.deflector.coreEffectiveMass / (distToCore * distToCore);
                            Vector3 velChange = currentVel - oldVel;
                            float velChangeMag = velChange.magnitude;
                            Vector3 accel = velChange / fixedUnscaledDeltaTime;
                            float accelMag = accel.magnitude;
                            
                            coreDeflectorInfo[idx] = $"Core[{GetCoreDeflectorName(idx)}]:dist={distToCore:F2},guidance={source.deflector.guidanceStrength:F2},velChange={velChangeMag:F3},accel={accelMag:F3},ACTIVE";
                            
                            // 调试日志
                            if (enablePredictionLogging && i % predictionLogInterval == 0)
                            {
                                float currentTime = i * dt;
                                Debug.Log($"[预测] ✅ CoreDeflector[{idx}] FixedUpdate应用 - 步数: {i}, 时间: {currentTime:F4}, 位置: ({currentPos.x:F3}, {currentPos.y:F3}), 距离: {distToCore:F2}, 速度变化: {velChangeMag:F3}, 加速度: {accelMag:F3}");
                            }
                        }
                    }
                    else
                    {
                        // 离开 trigger
                        if (deflectorEntered.ContainsKey(idx) && deflectorEntered[idx])
                        {
                            deflectorEntered[idx] = false;
                            deflectorEnterStep[idx] = -1;
                            deflectorLastUpdateStep[idx] = -1;
                            
                            // 调试：记录离开 trigger 的信息
                            if (enablePredictionLogging)
                            {
                                float currentTime = i * dt;
                                Debug.Log($"[预测] 🔴 CoreDeflector[{idx}] OnTriggerExit - 步数: {i}, 时间: {currentTime:F4}, 位置: ({currentPos.x:F3}, {currentPos.y:F3}), 距离: {distToCore:F2}");
                            }
                        }
                    }
                }
            }
            
            // ========== 3. ShipState 阶段（速度缩放已移除）==========
            // 速度缩放功能已移除（简化预测系统，提高准确性）
            // 如果需要调整游戏速度，可以使用 GravityEngine 的 timeZoom 或 massScale 参数
            // 不再需要处理速度缩放逻辑
            
            // ========== 4. 更新位置（半隐式欧拉法：r1 = r0 + v1 * dt）==========
            // 关键修复：位置更新在速度更新之后
            // 半隐式欧拉法：先更新速度（v1 = v0 + a * dt），再更新位置（r1 = r0 + v1 * dt）
            currentPos += currentVel * dt;
            currentPos.z = 0f; // 限制在 XY 平面
            
            // ========== 4. PlanetGravityCapture 阶段（覆盖引导，FixedUpdate）==========
            // 作用：在行星的capture范围内，强制引导飞船进入轨道
            // 作用方式：覆盖操作（Lerp） - newVelocity = Lerp(currentVelocity, idealVelocity, strength)
            // 作用范围：在 captureRadius 和 releaseRadius 之间
            // 优先级：最高（最后执行，覆盖之前的所有效果）
            // 
            // ========== 区域叠加处理：检查 CoreDeflector 标志 ==========
            // 核心规则：如果飞船在 CoreDeflector 的 trigger 范围内（isInAnyCoreDeflectorTrigger = true），
            // PlanetGravityCapture 会被跳过，只有 CoreDeflector 的弹弓偏转效果生效
            // 
            // 情况1：飞船在叠加区域内（既在 Core trigger 内，也在 Planet captureRadius 内）
            //   → isInAnyCoreDeflectorTrigger = true → 跳过 PlanetGravityCapture
            // 
            // 情况2：飞船只在 Planet 区域内（不在 Core trigger 内，但在 Planet captureRadius 内）
            //   → isInAnyCoreDeflectorTrigger = false → 应用 PlanetGravityCapture
            // 
            // 情况3：飞船只在 Core 区域内（在 Core trigger 内，但不在 Planet captureRadius 内）
            //   → 不进入此步骤（不在 captureRadius 内）
            // 
            // 注意：如果 onlyConsiderCoreDeflector = true，跳过此步骤
            // 
            // 关键修复：PlanetGravityCapture 在 FixedUpdate 中执行
            // 但考虑到它的 detectionInterval（默认 0.1 秒），我们使用 detectionInterval 来控制频率
            if (!onlyConsiderCoreDeflector && i % fixedUpdateInterval == 0)
            {
                // 计算 detectionInterval 对应的步数
                int detectionIntervalSteps = Mathf.Max(1, Mathf.RoundToInt(detectionInterval / dt));
                
                // 只在达到 detectionInterval 时应用（模拟实际的检测间隔）
                if (i % detectionIntervalSteps == 0)
                {
                    // PlanetGravityCapture（覆盖操作：Lerp）
                    // 关键修复：按固定顺序处理，确保确定性
                    var captureSources = new List<(int index, GravitySourceSnapshot source, float sortKey)>();
                    sourceIndex = 0;
                    foreach (var source in cachedSources)
                    {
                        if (source.hasCapture)
                        {
                            float sortKey = source.capture.planetPosition.x * 1000f + source.capture.planetPosition.y;
                            captureSources.Add((sourceIndex, source, sortKey));
                        }
                        sourceIndex++;
                    }
                    captureSources.Sort((a, b) => a.sortKey.CompareTo(b.sortKey));
                    
                    foreach (var (idx, source, _) in captureSources)
                    {
                        // ========== 区域叠加判定：检查飞船是否在 Planet 捕获范围内 ==========
                        if (IsInCaptureRange(currentPos, source.capture))
                        {
                            // ========== 区域叠加处理：检查是否在 CoreDeflector trigger 范围内 ==========
                            // 核心规则：如果飞船在 CoreDeflector 的 trigger 范围内，跳过 PlanetGravityCapture
                            // 
                            // 情况1：飞船在叠加区域内（既在 Core trigger 内，也在 Planet captureRadius 内）
                            //   → isInAnyCoreDeflectorTrigger = true → 跳过 PlanetGravityCapture
                            //   → 最终效果：只有 CoreDeflector 的弹弓偏转
                            // 
                            // 情况2：飞船只在 Planet 区域内（不在 Core trigger 内，但在 Planet captureRadius 内）
                            //   → isInAnyCoreDeflectorTrigger = false → 应用 PlanetGravityCapture
                            //   → 最终效果：只有 PlanetGravityCapture 的轨道引导
                            // 
                            // 保留原有逻辑：如果 isInAnyCoreDeflectorTrigger = false，则按原逻辑执行
                            // 如需恢复原逻辑（允许两个系统同时生效），只需将下面的 if (!isInAnyCoreDeflectorTrigger) 条件移除即可
                            if (!isInAnyCoreDeflectorTrigger)
                            {
                                // 应用 PlanetGravityCapture：覆盖操作，使用Lerp强制引导，会覆盖CoreDeflector的叠加效果
                                currentVel = ApplyCaptureLerp(currentPos, currentVel, source.capture, fixedUnscaledDeltaTime);
                            }
                            else
                            {
                                // 跳过 PlanetGravityCapture：飞船在 CoreDeflector trigger 范围内，不应用覆盖效果
                                // 最终效果：只有 CoreDeflector 的弹弓偏转效果生效
                                // 调试日志（可选）
                                if (enablePredictionLogging && i % predictionLogInterval == 0)
                                {
                                    Debug.Log($"[预测] ⚠️ PlanetGravityCapture[{idx}] 被跳过 - 飞船在 CoreDeflector trigger 范围内（区域叠加），步数: {i}, 位置: ({currentPos.x:F3}, {currentPos.y:F3})");
                                }
                            }
                        }
                    }
                    
                    // GravityHubDeflector（覆盖操作：Lerp）
                    // 作用：在引力枢纽范围内，偏转飞船方向
                    // 作用方式：覆盖操作（Lerp）
                    // 优先级：与PlanetGravityCapture相同（最后执行）
                    // 关键修复：按固定顺序处理，确保确定性
                    var hubDeflectorSources = new List<(int index, GravitySourceSnapshot source, float sortKey)>();
                    sourceIndex = 0;
                    foreach (var source in cachedSources)
                    {
                        if (source.hasHubDeflector)
                        {
                            float sortKey = source.hubDeflector.hubPosition.x * 1000f + source.hubDeflector.hubPosition.y;
                            hubDeflectorSources.Add((sourceIndex, source, sortKey));
                        }
                        sourceIndex++;
                    }
                    hubDeflectorSources.Sort((a, b) => a.sortKey.CompareTo(b.sortKey));
                    
                    foreach (var (idx, source, _) in hubDeflectorSources)
                    {
                        if (IsInHubDeflectorRange(currentPos, source.hubDeflector))
                        {
                            // 覆盖操作：使用Lerp偏转方向
                            currentVel = ApplyHubDeflectorLerp(currentPos, currentVel, source.hubDeflector, fixedUnscaledDeltaTime);
                        }
                    }
                }
            }
            
            // ========== 安全检查 ==========
            // 检查最大距离限制
            float distanceFromStart = Vector3.Distance(currentPos, startPosition);
            if (distanceFromStart > maxPredictionDistance)
            {
                Debug.LogWarning($"[预测] 轨迹预测超过最大距离 {maxPredictionDistance}，在步数 {i} 处停止");
                break;
            }
            
            // 检查速度是否异常
            if (currentVel.magnitude > 10000f || float.IsNaN(currentVel.magnitude) || float.IsInfinity(currentVel.magnitude))
            {
                Debug.LogWarning($"[预测] 速度异常 {currentVel.magnitude}，在步数 {i} 处停止");
                break;
            }
            
            predictionPoints.Add(currentPos);
            
            // 记录预测数据（如果启用）
            if (enablePredictionLogging && isLoggingPrediction && (i + 1) % predictionLogInterval == 0)
            {
                float currentTime = (i + 1) * dt; // 从开始预测的时间
                // 关键修复：使用当前步骤的 gravityAcc，而不是 newGravityAcc（新位置的重力）
                // 因为我们已经改用半隐式欧拉法，gravityAcc 是当前步骤计算的
                LogPredictionStep(i + 1, currentTime, currentPos, currentVel, gravityAcc, coreDeflectorInfo, 1.0f); // 速度缩放已移除，固定为1.0
            }
        }
        
        // 结束预测数据记录（只在实际记录时才停止）
        if (enablePredictionLogging && isLoggingPrediction)
        {
            StopPredictionLogging();
            // 重置标志
            shouldLogPrediction = false;
        }
        
        return predictionPoints;
    }
    
    /// <summary>
    /// 获取 CoreDeflector 的名称（用于日志）
    /// </summary>
    private string GetCoreDeflectorName(int index)
    {
        int count = 0;
        foreach (var source in cachedSources)
        {
            if (source.hasDeflector)
            {
                if (count == index)
                {
                    // 尝试从缓存中获取名称（需要存储）
                    return $"CoreDeflector[{index}]";
                }
                count++;
            }
        }
        return $"CoreDeflector[{index}]";
    }
    
    /// <summary>
    /// 开始预测数据记录
    /// </summary>
    private void StartPredictionLogging(Vector3 startPos, Vector3 startVel, float dt)
    {
        isLoggingPrediction = true;
        predictionStartTime = 0f; // 预测时间从0开始
        predictionLogBuffer.Clear();
        
        // 记录头部信息
        predictionLogBuffer.AppendLine("=== 预测数据记录 ===");
        predictionLogBuffer.AppendLine($"开始时间: {System.DateTime.Now:yyyy-MM-dd HH:mm:ss}");
        // 关键修复：强制使用 0.02 秒，确保与实际游戏一致
        float actualDt = 0.02f; // 强制使用 FixedUpdate 的时间步长
        if (Mathf.Abs(dt - actualDt) > 0.001f)
        {
            Debug.LogWarning($"[预测] 时间步长不一致！传入的 dt={dt}，但应该使用 {actualDt}。已强制修正。");
            dt = actualDt; // 修正 dt
        }
        predictionLogBuffer.AppendLine($"预测时间步长: {dt} 秒 (predictionTimeStep字段={predictionTimeStep})");
        predictionLogBuffer.AppendLine($"飞船初始位置: ({startPos.x:F2}, {startPos.y:F2}, {startPos.z:F2})");
        predictionLogBuffer.AppendLine($"飞船初始速度: ({startVel.x:F2}, {startVel.y:F2}, {startVel.z:F2})");
        // 关键修复：使用更高精度记录初始速度，确保与实际数据一致
        Debug.Log($"[预测] 初始速度（高精度）: X={startVel.x:F6}, Y={startVel.y:F6}, Z={startVel.z:F6}, 大小={startVel.magnitude:F6}");
        predictionLogBuffer.AppendLine($"GravityEngine 参数:");
        if (gravityEngine != null)
        {
            predictionLogBuffer.AppendLine($"  - physToWorldFactor: {gravityEngine.physToWorldFactor}");
            predictionLogBuffer.AppendLine($"  - massScale: {gravityEngine.massScale}");
            predictionLogBuffer.AppendLine($"  - fixedDeltaTime: {Time.fixedUnscaledDeltaTime}");
        }
        predictionLogBuffer.AppendLine($"CoreDeflector 数量: {GetCoreDeflectorCount()}");
        int coreIndex = 0;
        foreach (var source in cachedSources)
        {
            if (source.hasDeflector)
            {
                predictionLogBuffer.AppendLine($"  - CoreDeflector[{coreIndex}]: 位置={source.deflector.corePosition}, 质量={source.deflector.coreEffectiveMass}, 引导强度={source.deflector.guidanceStrength}, trigger半径={source.deflector.triggerRadius:F2}");
                coreIndex++;
            }
        }
        predictionLogBuffer.AppendLine("---");
        predictionLogBuffer.AppendLine("格式: [步数] [时间] [位置] [速度] [速度大小] [GravityEngine加速度] [CoreDeflector效果] [ShipState缩放] [其他效果]");
        predictionLogBuffer.AppendLine("---");
        
        // 调试：输出初始速度信息
        Debug.Log($"[预测数据记录] 初始位置: ({startPos.x:F2}, {startPos.y:F2}, {startPos.z:F2}), 初始速度: ({startVel.x:F2}, {startVel.y:F2}, {startVel.z:F2}), 速度大小: {startVel.magnitude:F3}");
    }
    
    /// <summary>
    /// 记录预测步骤数据
    /// </summary>
    private void LogPredictionStep(int step, float time, Vector3 pos, Vector3 vel, Vector3 gravAcc, Dictionary<int, string> coreDeflectorInfo, float speedMultiplier)
    {
        float speed = vel.magnitude;
        string coreDeflectorStr = "";
        foreach (var kvp in coreDeflectorInfo)
        {
            coreDeflectorStr += kvp.Value + " ";
        }
        // 如果没有 CoreDeflector 信息，记录空字符串（而不是不记录）
        if (string.IsNullOrEmpty(coreDeflectorStr))
        {
            coreDeflectorStr = " ";
        }
        // 速度缩放已移除，不再记录
        string speedMultiplierStr = "";
        
        predictionLogBuffer.AppendLine($"[{step}] t={time:F4} pos=({pos.x:F3},{pos.y:F3},{pos.z:F3}) vel=({vel.x:F3},{vel.y:F3},{vel.z:F3}) speed={speed:F3} " +
                                      $"gravAcc=({gravAcc.x:F3},{gravAcc.y:F3},{gravAcc.z:F3}){coreDeflectorStr}{speedMultiplierStr}");
    }
    
    /// <summary>
    /// 停止预测数据记录并保存到文件
    /// </summary>
    private void StopPredictionLogging()
    {
        if (!isLoggingPrediction) return;
        
        isLoggingPrediction = false;
        
        // 保存日志到文件
        if (predictionLogBuffer.Length > 0)
        {
            try
            {
                string fullPath = System.IO.Path.Combine(Application.dataPath, "..", predictionLogFilePath);
                System.IO.File.WriteAllText(fullPath, predictionLogBuffer.ToString());
                Debug.Log($"[预测数据记录] 日志已保存到: {fullPath}");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[预测数据记录] 保存日志失败: {e.Message}");
            }
        }
    }
    
    /// <summary>
    /// 获取 CoreDeflector 数量
    /// </summary>
    private int GetCoreDeflectorCount()
    {
        int count = 0;
        foreach (var source in cachedSources)
        {
            if (source.hasDeflector)
            {
                count++;
            }
        }
        return count;
    }

    // 注意：CalculateGravityAcceleration 已被 PredictFullTrajectory 中的内联计算替代
    // 保留此方法仅用于向后兼容（如果其他地方调用）
    private Vector3 CalculateGravityAcceleration(Vector3 position)
    {
        Vector3 totalAcceleration = Vector3.zero;
        foreach (var source in cachedSources)
        {
            Vector3 toSource = source.position - position;
            float distSq = toSource.sqrMagnitude;
            if (distSq < 0.01f) continue;
            
            float mag = source.nbodyEffectiveMass / distSq;
            totalAcceleration += toSource.normalized * mag;
        }
        return totalAcceleration;
    }

    /// <summary>
    /// 检查 Core 位置是否发生变化
    /// </summary>
    private bool CheckCorePositionsChanged()
    {
        bool changed = false;
        
        // 检查所有已缓存的 CoreDeflector 位置
        foreach (var deflector in coreDeflectors)
        {
            if (deflector == null) continue;
            
            Vector3 currentPosition = deflector.transform.position;
            
            if (cachedCorePositions.ContainsKey(deflector))
            {
                Vector3 cachedPosition = cachedCorePositions[deflector];
                // 如果位置变化超过阈值（0.01单位），认为位置已变化
                if (Vector3.Distance(currentPosition, cachedPosition) > 0.01f)
                {
                    changed = true;
                    cachedCorePositions[deflector] = currentPosition;
                }
            }
            else
            {
                // 新发现的 CoreDeflector，需要刷新
                changed = true;
                cachedCorePositions[deflector] = currentPosition;
            }
        }
        
        // 清理已不存在的 CoreDeflector
        var keysToRemove = new List<CoreDeflector>();
        foreach (var kvp in cachedCorePositions)
        {
            if (kvp.Key == null || !coreDeflectors.Contains(kvp.Key))
            {
                keysToRemove.Add(kvp.Key);
            }
        }
        foreach (var key in keysToRemove)
        {
            cachedCorePositions.Remove(key);
        }
        
        return changed;
    }
    
    /// <summary>
    /// 更新 Core 位置缓存
    /// </summary>
    private void UpdateCorePositionCache()
    {
        cachedCorePositions.Clear();
        foreach (var deflector in coreDeflectors)
        {
            if (deflector != null)
            {
                cachedCorePositions[deflector] = deflector.transform.position;
            }
        }
    }
    
    /// <summary>
    /// 强制刷新引力源缓存（当场景中的物体发生变化时调用）
    /// </summary>
    public void RefreshGravitySources()
    {
        CacheGravitySources();
        // CacheGravitySources 内部已经调用了 UpdateCorePositionCache
    }

    /// <summary>
    /// 设置预览模式（发射前固定轨迹）
    /// 用于发射器在用户调整发射参数时调用
    /// </summary>
    /// <param name="launchPosition">发射位置</param>
    /// <param name="launchVelocity">发射速度</param>
    public void SetPreviewTrajectory(Vector3 launchPosition, Vector3 launchVelocity)
    {
        // 关键修复：不在初始速度中应用速度缩放！
        // 实际游戏中，速度缩放是在 FixedUpdate 中每一步应用的，不是在初始速度中应用的
        // 如果在这里应用缩放，然后在 PredictFullTrajectory 中又应用缩放，会导致双重缩放
        // 所以这里直接使用传入的原始速度（未缩放），速度缩放会在 PredictFullTrajectory 中正确应用
        
        // 缓存发射速度（未缩放，与实际游戏一致）
        cachedLaunchVelocity = launchVelocity;
        
        // 计算并缓存轨迹（使用未缩放的速度，速度缩放会在预测过程中应用）
        cachedPreviewTrajectory = SimulateTrajectory(launchPosition, launchVelocity);
        
        // 如果轨迹可见，立即更新显示
        if (isTrajectoryVisible)
        {
            UpdateTrajectory();
        }
        
        Debug.Log($"[预测] 预览轨迹已更新 - 输入速度（未缩放）: ({launchVelocity.x:F6}, {launchVelocity.y:F6}, {launchVelocity.z:F6}), 速度大小: {launchVelocity.magnitude:F6}");
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
    /// 设置是否使用虚线
    /// </summary>
    public void SetUseDashedLine(bool useDashed)
    {
        useDashedLine = useDashed;
        if (lineRenderer != null)
        {
            SetupLineRenderer();
        }
    }
    
    /// <summary>
    /// 设置虚线参数
    /// </summary>
    public void SetDashedLineParams(float dashLen, float gapLen)
    {
        dashLength = dashLen;
        gapLength = gapLen;
        if (useDashedLine && lineRenderer != null)
        {
            CreateDashedLineMaterial();
        }
    }

    /// <summary>
    /// 获取飞船的速度缩放系数（速度缩放功能已移除）
    /// </summary>
    private float GetSpeedMultiplier()
    {
        // 速度缩放功能已移除，始终返回1.0
        return 1.0f;
    }
    
    // 注意：旧的积分方法已移除，现在统一在 SimulateTrajectory 中使用 Verlet 积分
    // 这样可以确保所有效果（GravityEngine、CoreDeflector、PlanetGravityCapture、速度缩放）
    // 都按照正确的顺序和频率应用

    // 注意：ApplyCoreDeflectorEffects 已被纯数学版本的 ApplyCoreDeflector 替代
    // 旧方法已移除，现在使用快照数据和纯数学运算
    
    /// <summary>
    /// CoreDeflector 叠加逻辑（纯数学运算，增量操作：v = v + a*dt）
    /// </summary>
    private Vector3 ApplyCoreDeflector(Vector3 pos, Vector3 vel, DeflectorParams p, float dt)
    {
        // 关键修复：必须与实际代码完全一致
        // CoreDeflector.cs 第253行：Vector3 coreToShip = shipPosition - transform.position;
        // 这是从Core指向飞船的向量，所以 radialDirection 应该从Core指向飞船
        Vector3 coreToShip = pos - p.corePosition; // 从Core指向飞船（与实际代码一致）
        float dist = coreToShip.magnitude;
        
        if (dist < p.minDistance || dist < 0.01f) return vel;
        
        // 关键修复：radialDirection 必须从Core指向飞船（与实际代码完全一致）
        // CoreDeflector.cs 第262行：Vector3 radialDirection = coreToShip.normalized;
        Vector3 radialDirection = coreToShip.normalized; // 从Core指向飞船
        
        // 1. 计算叠加引力（Radial Gravity）
        // 引力方向：从飞船指向Core（负radialDirection）
        float gravAccMag = p.coreEffectiveMass / (dist * dist);
        Vector3 radialGravity = -radialDirection * gravAccMag; // 负号表示指向Core
        
        // 2. 计算引导速度（Guidance）
        // 关键：实际代码中，guidanceAcceleration = velocityToTangential * guidanceStrength / fixedUnscaledDeltaTime
        // 这意味着加速度是为了在 fixedUnscaledDeltaTime 时间内完成转向而设计的
        // 在预测中，我们传入的 dt 应该是 fixedUnscaledDeltaTime（因为只在达到physicsUpdateInterval时应用）
        // 所以这里直接使用 dt，它应该等于 fixedUnscaledDeltaTime
        // 关键修复：传入radialDirection（从Core指向飞船），与实际代码一致
        Vector3 tangentDir = GetTangentialDirection(radialDirection, vel.normalized);
        Vector3 idealTangentialVel = tangentDir * vel.magnitude;
        Vector3 velocityToTangential = idealTangentialVel - vel;
        // 使用传入的 dt（应该是 fixedUnscaledDeltaTime）
        // 注意：这里除以 dt 是为了计算加速度，使得在 dt 时间内完成转向
        Vector3 guidanceAcc = velocityToTangential * p.guidanceStrength / dt;
        
        // 3. 轨道半径修正
        // CoreDeflector.cs 第286-290行：如果有目标轨道半径，添加径向调整
        if (p.targetOrbitRadius > 0f)
        {
            float radiusError = dist - p.targetOrbitRadius;
            // radialDirection 是从Core指向飞船，所以 -radialDirection 是从飞船指向Core
            // 如果 radiusError > 0（距离太远），radiusCorrection 指向Core（拉近），这是正确的
            Vector3 radiusCorrection = -radialDirection * radiusError * p.guidanceStrength * 0.5f;
            guidanceAcc += radiusCorrection;
        }
        
        // 4. 混合加速度并叠加（增量操作）
        Vector3 totalAcc = radialGravity * (1f - p.guidanceStrength) + guidanceAcc * p.guidanceStrength;
        Vector3 newVel = vel + totalAcc * dt; // ← 增量操作
        
        // 5. 角度限制（MaxAngularVelocity）
        return LimitAngleChange(vel, newVel, p.maxAngularVelocity, dt);
    }
    
    /// <summary>
    /// 限制角度改变速度
    /// </summary>
    private Vector3 LimitAngleChange(Vector3 currentVel, Vector3 targetVel, float maxAngularVelocity, float dt)
    {
        if (maxAngularVelocity <= 0f) return targetVel;
        
        float currentSpeed = currentVel.magnitude;
        if (currentSpeed < 0.01f) return targetVel;
        
        float maxRotationPerFrame = maxAngularVelocity * Mathf.Deg2Rad * dt;
        Vector3 currentDir = currentVel.normalized;
        Vector3 targetDir = targetVel.normalized;
        float angleChange = Vector3.Angle(currentDir, targetDir) * Mathf.Deg2Rad;
        
        if (angleChange > maxRotationPerFrame)
        {
            Vector3 rotationAxis = Vector3.Cross(currentDir, targetDir);
            if (rotationAxis.magnitude < 0.001f)
            {
                rotationAxis = Vector3.forward;
            }
            rotationAxis = rotationAxis.normalized;
            
            Quaternion limitedRotation = Quaternion.AngleAxis(maxAngularVelocity * dt, rotationAxis);
            Vector3 limitedDir = limitedRotation * currentDir;
            return limitedDir * currentSpeed;
        }
        
        return targetVel;
    }
    
    /// <summary>
    /// GravityHubDeflector 的 Lerp 修正（覆盖操作：v = Lerp(v, target)）
    /// </summary>
    private Vector3 ApplyHubDeflectorLerp(Vector3 pos, Vector3 vel, HubDeflectorParams p, float dt)
    {
        Vector3 toHub = p.hubPosition - pos;
        float distance = toHub.magnitude;
        
        if (distance < 0.01f || distance > p.deflectRadius) return vel;
        
        float currentSpeed = vel.magnitude;
        if (currentSpeed < p.minVelocity) return vel;
        
        Vector3 radialDir = toHub.normalized;
        Vector3 tangent = GetHubTangent(radialDir);
        
        // 计算理想速度（保持速度大小，改变方向）
        Vector3 idealVelocity = tangent * currentSpeed;
        
        // Lerp 调整方向（覆盖操作）
        // 注意：需要根据 dt 调整强度，防止预测步长改变导致引导变强/变弱
        float stepStrength = AdjustLerpStrengthForTimeStep(p.deflectionStrength, dt);
        Vector3 adjustedVelocity = Vector3.Lerp(vel, idealVelocity, stepStrength);
        
        // 限制速度变化
        Vector3 velocityChange = adjustedVelocity - vel;
        if (velocityChange.magnitude > p.maxVelocityChange)
        {
            velocityChange = velocityChange.normalized * p.maxVelocityChange;
            adjustedVelocity = vel + velocityChange;
        }
        
        // 确保速度不会太小
        if (adjustedVelocity.magnitude < p.minVelocity)
        {
            adjustedVelocity = adjustedVelocity.normalized * p.minVelocity;
        }
        
        adjustedVelocity.z = 0f;
        return adjustedVelocity;
    }
    
    /// <summary>
    /// 获取引力枢纽的切向方向（与 GravityHubDeflector 的逻辑一致）
    /// </summary>
    private Vector3 GetHubTangent(Vector3 radialDir)
    {
        Vector3 tangent = new Vector3(-radialDir.y, radialDir.x, 0f).normalized;
        
        if (float.IsNaN(tangent.x) || tangent.magnitude < 0.1f)
        {
            tangent = Vector3.up;
        }
        
        return tangent;
    }
    
    /// <summary>
    /// PlanetCapture 的 Lerp 修正（覆盖操作：v = Lerp(v, target)）
    /// </summary>
    private Vector3 ApplyCaptureLerp(Vector3 pos, Vector3 vel, CaptureParams p, float dt)
    {
        Vector3 toPlanet = p.planetPosition - pos;
        float distance = toPlanet.magnitude;
        
        if (distance < 0.01f || distance > p.captureRadius || distance > p.releaseRadius) return vel;
        
        // 计算理想轨道速度
        Vector3 radialDir = toPlanet.normalized;
        Vector3 tangent = GetOrbitTangent(radialDir, vel, p.forceCounterClockwise);
        float idealOrbitSpeed = Mathf.Sqrt(p.planetMass / Mathf.Max(distance, 0.1f));
        
        // 分解速度
        float radialSpeed = Vector3.Dot(vel, radialDir);
        Vector3 radialVel = radialDir * radialSpeed;
        Vector3 tangentialVel = vel - radialVel;
        
        // 计算理想切向速度
        Vector3 idealTangentialVel = tangent * idealOrbitSpeed;
        
        // Lerp 调整（覆盖操作）
        // 注意：需要根据 dt 调整强度
        float stepStrength = AdjustLerpStrengthForTimeStep(p.orbitGuidanceStrength, dt);
        Vector3 adjustedTangentialVel = Vector3.Lerp(
            tangentialVel.normalized * Mathf.Max(tangentialVel.magnitude, 0.1f),
            idealTangentialVel,
            stepStrength
        );
        
        Vector3 adjustedRadialVel = Vector3.Lerp(radialVel, Vector3.zero, stepStrength * 0.5f);
        
        return adjustedRadialVel + adjustedTangentialVel;
    }
    
    /// <summary>
    /// 根据时间步长调整 Lerp 强度（防止预测步长改变导致引导变强/变弱）
    /// </summary>
    private float AdjustLerpStrengthForTimeStep(float originalStrength, float dt)
    {
        // 如果 dt 等于 Time.fixedDeltaTime，不需要调整
        // 如果 dt 不同，需要数学补偿
        // 公式：stepStrength = 1 - (1 - strength)^(dt / fixedDeltaTime)
        float fixedDeltaTime = fixedUnscaledDeltaTime;
        if (Mathf.Approximately(dt, fixedDeltaTime))
        {
            return originalStrength;
        }
        
        // 数学补偿：确保不同 dt 下的累积效果一致
        return 1f - Mathf.Pow(1f - originalStrength, dt / fixedDeltaTime);
    }
    
    /// <summary>
    /// 获取切向方向（与 CoreDeflector 的逻辑一致）
    /// </summary>
    private Vector3 GetTangentialDirection(Vector3 radialDirection, Vector3 velocityDirection)
    {
        Vector3 tangent1 = new Vector3(-radialDirection.y, radialDirection.x, 0f).normalized;
        Vector3 tangent2 = new Vector3(radialDirection.y, -radialDirection.x, 0f).normalized;
        
        float dot1 = Vector3.Dot(velocityDirection, tangent1);
        float dot2 = Vector3.Dot(velocityDirection, tangent2);
        
        return (dot1 > dot2) ? tangent1 : tangent2;
    }
    
    /// <summary>
    /// 获取轨道切向方向（与 PlanetGravityCapture 的逻辑一致）
    /// </summary>
    private Vector3 GetOrbitTangent(Vector3 radialDir, Vector3 currentVelocity, bool forceCounterClockwise)
    {
        Vector3 counterClockwise = Vector3.Cross(Vector3.forward, radialDir).normalized;
        Vector3 clockwise = -counterClockwise;
        
        if (forceCounterClockwise)
        {
            return counterClockwise;
        }
        
        if (currentVelocity.magnitude < 0.1f)
        {
            return counterClockwise;
        }
        
        float radialSpeed = Vector3.Dot(currentVelocity, radialDir);
        Vector3 currentTangential = (currentVelocity - radialDir * radialSpeed).normalized;
        
        if (currentTangential.magnitude < 0.1f)
        {
            return counterClockwise;
        }
        
        float dotCounterClockwise = Vector3.Dot(currentTangential, counterClockwise);
        float dotClockwise = Vector3.Dot(currentTangential, clockwise);
        
        return (dotCounterClockwise > dotClockwise) ? counterClockwise : clockwise;
    }
    
    /// <summary>
    /// 检查点是否在 Collider 触发器内（纯数学运算）
    /// </summary>
    private bool IsInTriggerBounds(Vector3 point, ColliderBounds bounds)
    {
        if (!bounds.isValid) return false;
        
        Vector3 min = bounds.center - bounds.size * 0.5f;
        Vector3 max = bounds.center + bounds.size * 0.5f;
        
        return point.x >= min.x && point.x <= max.x &&
               point.y >= min.y && point.y <= max.y &&
               point.z >= min.z && point.z <= max.z;
    }
    
    /// <summary>
    /// 检查点是否在捕获范围内（纯数学运算）
    /// </summary>
    private bool IsInCaptureRange(Vector3 pos, CaptureParams capture)
    {
        float distance = Vector3.Distance(pos, capture.planetPosition);
        return distance <= capture.captureRadius && distance <= capture.releaseRadius;
    }
    
    /// <summary>
    /// 检查 Core 是否在基地范围内（如果 Core 在基地范围内，不纳入预测计算）
    /// 这与 CoreGravityDisabler 的逻辑一致：当 Core 在基地范围内时，CoreDeflector 会被禁用
    /// </summary>
    private bool IsCoreInStationRange(CoreGravityDisabler disabler)
    {
        if (disabler == null) return false;
        
        // 获取 stationTag 和 stationTriggerRadius（字段是 public，可以直接访问）
        string stationTag = disabler.stationTag;
        if (string.IsNullOrEmpty(stationTag))
        {
            stationTag = "Station"; // 默认值
        }
        
        float stationTriggerRadius = disabler.stationTriggerRadius;
        if (stationTriggerRadius <= 0f)
        {
            stationTriggerRadius = 10f; // 默认值
        }
        
        // 查找基地对象（通过 Tag）
        GameObject[] stations = GameObject.FindGameObjectsWithTag(stationTag);
        if (stations.Length == 0)
        {
            // 没有找到基地，Core 不在基地范围内
            return false;
        }
        
        GameObject stationObject = stations[0];
        Vector3 corePosition = disabler.transform.position;
        
        // 方法1：检查 station 是否有 Trigger Collider
        Collider[] colliders = stationObject.GetComponentsInChildren<Collider>();
        Collider stationTriggerCollider = null;
        foreach (Collider col in colliders)
        {
            if (col.isTrigger)
            {
                stationTriggerCollider = col;
                break;
            }
        }
        
        // 方法2：如果有 Trigger Collider，使用 bounds 检查
        if (stationTriggerCollider != null)
        {
            Bounds triggerBounds = stationTriggerCollider.bounds;
            return triggerBounds.Contains(corePosition);
        }
        else
        {
            // 方法3：如果没有 Trigger Collider，使用距离检测
            float distance = Vector3.Distance(corePosition, stationObject.transform.position);
            return distance <= stationTriggerRadius;
        }
    }
    
    /// <summary>
    /// 检查点是否在 HubDeflector 范围内（纯数学运算）
    /// </summary>
    private bool IsInHubDeflectorRange(Vector3 pos, HubDeflectorParams hub)
    {
        float distance = Vector3.Distance(pos, hub.hubPosition);
        return distance <= hub.deflectRadius;
    }
    
    /// <summary>
    /// 通过反射获取私有字段值（带缓存）
    /// </summary>
    private T GetPrivateField<T>(object obj, string fieldName)
    {
        if (obj == null) return default(T);
        
        Type objType = obj.GetType();
        FieldInfo field = null;
        
        // 尝试从缓存获取
        if (objType == typeof(CoreDeflector))
        {
            CoreDeflector deflector = obj as CoreDeflector;
            if (coreDeflectorFields.ContainsKey(deflector) && coreDeflectorFields[deflector].ContainsKey(fieldName))
            {
                field = coreDeflectorFields[deflector][fieldName];
            }
            else
            {
                field = objType.GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Public);
                if (!coreDeflectorFields.ContainsKey(deflector))
                {
                    coreDeflectorFields[deflector] = new Dictionary<string, FieldInfo>();
                }
                coreDeflectorFields[deflector][fieldName] = field;
            }
        }
        else if (objType == typeof(GravityHubDeflector))
        {
            GravityHubDeflector hubDeflector = obj as GravityHubDeflector;
            if (gravityHubDeflectorFields.ContainsKey(hubDeflector) && gravityHubDeflectorFields[hubDeflector].ContainsKey(fieldName))
            {
                field = gravityHubDeflectorFields[hubDeflector][fieldName];
            }
            else
            {
                field = objType.GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Public);
                if (!gravityHubDeflectorFields.ContainsKey(hubDeflector))
                {
                    gravityHubDeflectorFields[hubDeflector] = new Dictionary<string, FieldInfo>();
                }
                gravityHubDeflectorFields[hubDeflector][fieldName] = field;
            }
        }
        else if (objType == typeof(PlanetGravityCapture))
        {
            PlanetGravityCapture capture = obj as PlanetGravityCapture;
            if (planetCaptureFields.ContainsKey(capture) && planetCaptureFields[capture].ContainsKey(fieldName))
            {
                field = planetCaptureFields[capture][fieldName];
            }
            else
            {
                field = objType.GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Public);
                if (!planetCaptureFields.ContainsKey(capture))
                {
                    planetCaptureFields[capture] = new Dictionary<string, FieldInfo>();
                }
                planetCaptureFields[capture][fieldName] = field;
            }
        }
        else
        {
            field = objType.GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Public);
        }
        
        if (field != null)
        {
            return (T)field.GetValue(obj);
        }
        
        return default(T);
    }

    void OnDestroy()
    {
        // 取消订阅事件
        if (EventManager.Instance != null)
        {
            EventManager.Instance.OnShipLaunched -= OnShipLaunched;
            EventManager.Instance.OnShipStateChanged -= OnShipStateChanged;
        }
        
        // 清理资源
        gravitySources.Clear();
        coreDeflectors.Clear();
        gravityHubDeflectors.Clear();
        planetCaptures.Clear();
        coreDeflectorFields.Clear();
        gravityHubDeflectorFields.Clear();
        planetCaptureFields.Clear();
    }

    void OnDrawGizmosSelected()
    {
        // 在编辑器中显示引力源（用于调试）
        if (gravitySources != null && gravitySources.Count > 0)
        {
            Gizmos.color = Color.yellow;
            foreach (NBody source in gravitySources)
            {
                if (source != null)
                {
                    Gizmos.DrawWireSphere(source.transform.position, 0.5f);
                }
            }
        }
    }
}
