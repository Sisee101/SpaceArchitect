using UnityEngine;
using System.Linq;

/// <summary>
/// 飞船轨迹预测技能
/// 在PreLaunch状态下按Y键显示/隐藏飞船的预测轨道线
/// </summary>
[RequireComponent(typeof(ShipState))]
public class ShipTrajectorySkill : MonoBehaviour
{
    [Header("输入设置")]
    [Tooltip("触发轨迹显示的按键（默认Y键）")]
    [SerializeField] private KeyCode toggleKey = KeyCode.Y;
    
    [Header("轨迹预测器设置")]
    [Tooltip("轨迹预测器组件（如果为空，将自动查找）")]
    [SerializeField] private TrajectoryPredictor trajectoryPredictor;
    
    [Tooltip("如果找不到TrajectoryPredictor，是否自动创建")]
    [SerializeField] private bool autoCreatePredictor = true;
    
    [Tooltip("只考虑 CoreDeflector，忽略其他 NBody 的基础引力\n启用后，预测轨迹将只计算 CoreDeflector 的影响")]
    [SerializeField] private bool onlyConsiderCoreDeflector = false;
    
    [Header("发射速度设置")]
    [Tooltip("预览时使用的默认发射速度（如果发射器未提供）")]
    [SerializeField] private Vector3 defaultLaunchVelocity = new Vector3(5f, 0f, 0f);
    
    [Tooltip("是否从发射器获取当前发射速度")]
    [SerializeField] private bool useLauncherVelocity = true;
    
    [Header("其他设置")]
    [Tooltip("是否在发射后自动隐藏轨迹")]
    [SerializeField] private bool hideOnLaunch = true;
    
    [Tooltip("显示调试信息")]
    [SerializeField] private bool showDebugLogs = false;
    
    [Tooltip("是否在显示轨迹时检查多个CoreDeflector的影响")]
    [SerializeField] private bool checkMultipleDeflectors = true;
    
    // 内部状态
    private ShipState shipState;
    private ShipLauncher shipLauncher;
    private ShipSimpleLauncher shipSimpleLauncher;
    private Camera mainCamera;
    private bool isTrajectoryVisible = false;
    private Vector3 lastPreviewVelocity = Vector3.zero;
    private CoreDeflectorAnalyzer deflectorAnalyzer;
    
    void Awake()
    {
        // 获取组件
        shipState = GetComponent<ShipState>();
        if (shipState == null)
        {
            Debug.LogError($"ShipTrajectorySkill: {gameObject.name} 缺少 ShipState 组件！");
        }
        
        shipLauncher = GetComponent<ShipLauncher>();
        shipSimpleLauncher = GetComponent<ShipSimpleLauncher>();
        
        mainCamera = Camera.main;
        if (mainCamera == null)
        {
            mainCamera = FindObjectOfType<Camera>();
        }
    }
    
    void Start()
    {
        // 查找或创建 TrajectoryPredictor
        if (trajectoryPredictor == null)
        {
            trajectoryPredictor = GetComponent<TrajectoryPredictor>();
            
            if (trajectoryPredictor == null && autoCreatePredictor)
            {
                // 尝试创建 TrajectoryPredictor
                trajectoryPredictor = gameObject.AddComponent<TrajectoryPredictor>();
                
                // 添加 LineRenderer（TrajectoryPredictor 需要）
                LineRenderer lineRenderer = GetComponent<LineRenderer>();
                if (lineRenderer == null)
                {
                    lineRenderer = gameObject.AddComponent<LineRenderer>();
                }
                
                if (showDebugLogs)
                {
                    Debug.Log($"ShipTrajectorySkill: 已自动创建 TrajectoryPredictor 组件");
                }
            }
        }
        
        if (trajectoryPredictor == null)
        {
            Debug.LogWarning($"ShipTrajectorySkill: {gameObject.name} 没有找到 TrajectoryPredictor 组件，轨迹显示功能将不可用。请在Inspector中手动指定或启用 autoCreatePredictor。");
        }
        else
        {
            // 设置预测模式为预览模式
            trajectoryPredictor.SetPredictionMode(TrajectoryPredictor.PredictionMode.Preview);
            
            // 设置是否只考虑 CoreDeflector
            trajectoryPredictor.onlyConsiderCoreDeflector = onlyConsiderCoreDeflector;
            
            // 重要：刷新引力源缓存，确保CoreDeflector等组件被正确识别
            trajectoryPredictor.RefreshGravitySources();
            
            if (showDebugLogs)
            {
                string modeInfo = onlyConsiderCoreDeflector ? "（仅CoreDeflector模式）" : "";
                Debug.Log($"ShipTrajectorySkill: 已找到 TrajectoryPredictor 组件，轨迹显示功能已启用{modeInfo}，已刷新引力源缓存（包括CoreDeflector）");
            }
        }
        
        // 订阅发射事件，在发射后隐藏轨迹
        if (EventManager.Instance != null)
        {
            EventManager.Instance.OnShipLaunched += OnShipLaunched;
            EventManager.Instance.OnShipStateChanged += OnShipStateChanged;
        }
        
        // 创建或查找 CoreDeflectorAnalyzer（用于检查多个CoreDeflector的影响）
        if (checkMultipleDeflectors)
        {
            deflectorAnalyzer = FindObjectOfType<CoreDeflectorAnalyzer>();
            if (deflectorAnalyzer == null)
            {
                // 创建一个临时的分析器（不添加到场景中）
                GameObject analyzerObj = new GameObject("CoreDeflectorAnalyzer_Temp");
                deflectorAnalyzer = analyzerObj.AddComponent<CoreDeflectorAnalyzer>();
                deflectorAnalyzer.enabled = false; // 不自动运行，只在需要时调用
            }
        }
    }
    
    void OnEnable()
    {
        // 订阅事件
        if (EventManager.Instance != null)
        {
            EventManager.Instance.OnShipLaunched += OnShipLaunched;
            EventManager.Instance.OnShipStateChanged += OnShipStateChanged;
        }
    }
    
    void OnDisable()
    {
        // 取消订阅事件
        if (EventManager.Instance != null)
        {
            EventManager.Instance.OnShipLaunched -= OnShipLaunched;
            EventManager.Instance.OnShipStateChanged -= OnShipStateChanged;
        }
    }
    
    void Update()
    {
        // 只在 PreLaunch 状态下响应Y键
        if (shipState == null || shipState.CurrentState != ShipState.State.PreLaunch)
        {
            // 如果不在 PreLaunch 状态，隐藏轨迹
            if (isTrajectoryVisible)
            {
                HideTrajectory();
            }
            return;
        }
        
        // 检测 Y 键输入（切换显示/隐藏）
        if (Input.GetKeyDown(toggleKey))
        {
            ToggleTrajectory();
        }
        
        // 如果轨迹可见，实时更新轨迹（跟随发射器方向）
        if (isTrajectoryVisible && trajectoryPredictor != null)
        {
            UpdateTrajectory();
        }
    }
    
    /// <summary>
    /// 切换轨迹显示/隐藏
    /// </summary>
    public void ToggleTrajectory()
    {
        if (isTrajectoryVisible)
        {
            HideTrajectory();
        }
        else
        {
            ShowTrajectory();
        }
    }
    
    /// <summary>
    /// 显示轨迹
    /// </summary>
    public void ShowTrajectory()
    {
        if (trajectoryPredictor == null)
        {
            if (showDebugLogs)
            {
                Debug.LogWarning("ShipTrajectorySkill: 无法显示轨迹，TrajectoryPredictor 为空");
            }
            return;
        }
        
        if (shipState == null || shipState.CurrentState != ShipState.State.PreLaunch)
        {
            if (showDebugLogs)
            {
                Debug.LogWarning($"ShipTrajectorySkill: 无法显示轨迹，飞船不在 PreLaunch 状态（当前状态: {shipState?.CurrentState}）");
            }
            return;
        }
        
        // 重要：在显示轨迹前刷新引力源缓存，确保CoreDeflector等组件的最新参数被正确识别
        // 这样可以确保预测轨迹准确反映场景中所有引力源（包括CoreDeflector）的影响
        trajectoryPredictor.RefreshGravitySources();
        
        // 检查是否有多个 CoreDeflector 同时影响飞船
        if (checkMultipleDeflectors && deflectorAnalyzer != null)
        {
            CheckMultipleCoreDeflectors();
        }
        
        // 更新状态
        isTrajectoryVisible = true;
        
        // 立即更新一次轨迹
        UpdateTrajectory();
        
        // 确保轨迹可见
        trajectoryPredictor.SetTrajectoryVisible(true);
        
        if (showDebugLogs)
        {
            Debug.Log("ShipTrajectorySkill: 轨迹已显示（按Y键可隐藏），已刷新引力源缓存以确保CoreDeflector影响被正确计算");
        }
    }
    
    /// <summary>
    /// 隐藏轨迹
    /// </summary>
    public void HideTrajectory()
    {
        if (trajectoryPredictor != null)
        {
            trajectoryPredictor.SetTrajectoryVisible(false);
            trajectoryPredictor.ClearPreviewTrajectory();
        }
        
        isTrajectoryVisible = false;
        lastPreviewVelocity = Vector3.zero;
        
        if (showDebugLogs)
        {
            Debug.Log("ShipTrajectorySkill: 轨迹已隐藏");
        }
    }
    
    /// <summary>
    /// 更新轨迹（实时跟随发射器方向）
    /// </summary>
    private void UpdateTrajectory()
    {
        if (trajectoryPredictor == null || shipState == null || shipState.CurrentState != ShipState.State.PreLaunch)
        {
            return;
        }
        
        // 获取当前发射位置和速度
        Vector3 launchPosition = transform.position;
        Vector3 launchVelocity = GetLaunchVelocity();
        
        // 如果速度发生变化，更新轨迹
        // 注意：TrajectoryPredictor内部会考虑CoreDeflector的影响，因为它已经缓存了所有引力源
        if (Vector3.Distance(launchVelocity, lastPreviewVelocity) > 0.01f)
        {
            trajectoryPredictor.SetPreviewTrajectory(launchPosition, launchVelocity);
            lastPreviewVelocity = launchVelocity;
            
            if (showDebugLogs)
            {
                Debug.Log($"ShipTrajectorySkill: 轨迹已更新 - 位置: {launchPosition}, 速度: {launchVelocity}, 速度大小: {launchVelocity.magnitude:F2}");
            }
        }
        
        // 确保轨迹可见
        trajectoryPredictor.SetTrajectoryVisible(true);
    }
    
    /// <summary>
    /// 获取发射速度（从发射器或使用默认值）
    /// </summary>
    private Vector3 GetLaunchVelocity()
    {
        if (!useLauncherVelocity)
        {
            return defaultLaunchVelocity;
        }
        
        // 优先使用 ShipSimpleLauncher（如果存在）
        if (shipSimpleLauncher != null)
        {
            // 通过反射获取 ShipSimpleLauncher 的 currentLaunchDirection 和 launchSpeed
            var currentLaunchDirectionField = typeof(ShipSimpleLauncher).GetField("currentLaunchDirection", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var launchSpeedField = typeof(ShipSimpleLauncher).GetField("launchSpeed", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            
            if (currentLaunchDirectionField != null && launchSpeedField != null)
            {
                Vector3 direction = (Vector3)currentLaunchDirectionField.GetValue(shipSimpleLauncher);
                float speed = (float)launchSpeedField.GetValue(shipSimpleLauncher);
                
                Vector3 velocity = direction * speed;
                velocity.z = 0f; // 确保在XY平面
                
                if (velocity.magnitude > 0.01f)
                {
                    return velocity;
                }
            }
        }
        
        // 如果 ShipSimpleLauncher 不存在，尝试使用 ShipLauncher（拖拽方式）
        if (shipLauncher != null)
        {
            // 尝试通过反射获取 ShipLauncher 的当前拖拽状态
            var isDraggingField = typeof(ShipLauncher).GetField("isDragging", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            
            bool isDragging = false;
            if (isDraggingField != null)
            {
                isDragging = (bool)isDraggingField.GetValue(shipLauncher);
            }
            
            if (isDragging)
            {
                // 如果正在拖拽，获取拖拽向量并计算发射速度
                var dragStartPositionField = typeof(ShipLauncher).GetField("dragStartPosition", 
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                
                if (dragStartPositionField != null && mainCamera != null)
                {
                    Vector3 dragStartPosition = (Vector3)dragStartPositionField.GetValue(shipLauncher);
                    Vector3 currentMousePosition = Input.mousePosition;
                    Vector3 dragVector = dragStartPosition - currentMousePosition;
                    
                    if (dragVector.magnitude > 0.01f)
                    {
                        return CalculateLaunchVelocityFromDrag(dragVector);
                    }
                }
            }
        }
        
        // 如果无法从发射器获取，使用默认值
        return defaultLaunchVelocity;
    }
    
    /// <summary>
    /// 从拖拽向量计算发射速度（需要与 ShipLauncher 的逻辑一致）
    /// </summary>
    private Vector3 CalculateLaunchVelocityFromDrag(Vector3 dragVector)
    {
        if (shipLauncher == null || mainCamera == null)
        {
            return defaultLaunchVelocity;
        }
        
        // 获取 ShipLauncher 的参数（通过反射）
        var maxDragDistanceField = typeof(ShipLauncher).GetField("maxDragDistance", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var launchForceMultiplierField = typeof(ShipLauncher).GetField("launchForceMultiplier", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var shipScreenPositionField = typeof(ShipLauncher).GetField("shipScreenPosition", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        
        float maxDragDistance = 200f; // 默认值
        float launchForceMultiplier = 5f; // 默认值
        Vector3 shipScreenPos = mainCamera.WorldToScreenPoint(transform.position);
        
        if (maxDragDistanceField != null)
        {
            maxDragDistance = (float)maxDragDistanceField.GetValue(shipLauncher);
        }
        
        if (launchForceMultiplierField != null)
        {
            launchForceMultiplier = (float)launchForceMultiplierField.GetValue(shipLauncher);
        }
        
        if (shipScreenPositionField != null)
        {
            shipScreenPos = (Vector3)shipScreenPositionField.GetValue(shipLauncher);
        }
        
        // 使用与 ShipLauncher.CalculateLaunchVelocity 相同的逻辑
        Vector2 dragVector2D = new Vector2(dragVector.x, dragVector.y);
        float distanceToCamera = Vector3.Distance(mainCamera.transform.position, transform.position);
        
        // 转换为世界坐标（与 ShipLauncher 的逻辑一致）
        Vector3 startWorldPos = mainCamera.ScreenToWorldPoint(
            new Vector3(shipScreenPos.x, shipScreenPos.y, distanceToCamera)
        );
        
        Vector3 endWorldPos = mainCamera.ScreenToWorldPoint(
            new Vector3(shipScreenPos.x + dragVector.x, shipScreenPos.y + dragVector.y, distanceToCamera)
        );
        
        Vector3 worldDirection = endWorldPos - startWorldPos;
        worldDirection.z = 0f; // 限制在XY平面
        
        float dragRatio = Mathf.Clamp01(dragVector2D.magnitude / maxDragDistance);
        Vector3 velocity = worldDirection.normalized * dragRatio * launchForceMultiplier;
        
        return velocity;
    }
    
    /// <summary>
    /// 飞船发射事件回调
    /// </summary>
    private void OnShipLaunched(Vector3 launchVelocity, GameObject ship)
    {
        // 只处理自己的飞船
        if (ship != gameObject)
        {
            return;
        }
        
        // 发射后隐藏轨迹
        if (hideOnLaunch && isTrajectoryVisible)
        {
            HideTrajectory();
            
            if (showDebugLogs)
            {
                Debug.Log("ShipTrajectorySkill: 飞船已发射，轨迹已自动隐藏");
            }
        }
    }
    
    /// <summary>
    /// 飞船状态变化事件回调
    /// </summary>
    private void OnShipStateChanged(ShipState.State oldState, ShipState.State newState, GameObject ship)
    {
        // 只处理自己的飞船
        if (ship != gameObject)
        {
            return;
        }
        
        // 如果从 PreLaunch 转换到其他状态，自动隐藏轨迹
        if (oldState == ShipState.State.PreLaunch && newState != ShipState.State.PreLaunch)
        {
            if (isTrajectoryVisible)
            {
                HideTrajectory();
                
                if (showDebugLogs)
                {
                    Debug.Log($"ShipTrajectorySkill: 飞船状态从 PreLaunch 变为 {newState}，轨迹已自动隐藏");
                }
            }
        }
    }
    
    /// <summary>
    /// 检查轨迹是否可见
    /// </summary>
    public bool IsTrajectoryVisible()
    {
        return isTrajectoryVisible;
    }
    
    /// <summary>
    /// 设置触发按键
    /// </summary>
    public void SetToggleKey(KeyCode key)
    {
        toggleKey = key;
    }
    
    /// <summary>
    /// 检查是否有多个 CoreDeflector 同时影响飞船
    /// </summary>
    private void CheckMultipleCoreDeflectors()
    {
        if (deflectorAnalyzer == null || shipState == null)
        {
            return;
        }
        
        // 设置分析器的目标飞船
        var targetShipField = typeof(CoreDeflectorAnalyzer).GetField("targetShip", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        if (targetShipField != null)
        {
            targetShipField.SetValue(deflectorAnalyzer, gameObject);
        }
        
        // 执行分析
        deflectorAnalyzer.AnalyzeScene();
        
        // 检查结果
        bool shipInMultipleRanges = deflectorAnalyzer.IsShipInMultipleRanges();
        var overlaps = deflectorAnalyzer.GetOverlaps();
        int overlappingCount = overlaps.Count(o => o.isOverlapping);
        
        if (shipInMultipleRanges)
        {
            Debug.LogWarning("⚠️ [ShipTrajectorySkill] 检测到飞船当前位置在多个 CoreDeflector 的影响范围内！");
            Debug.LogWarning("这可能导致轨迹预测不准确，因为多个 CoreDeflector 会同时影响飞船。");
            Debug.LogWarning("建议：调整 CoreDeflector 的位置或 trigger 范围，避免重叠。");
        }
        
        if (overlappingCount > 0)
        {
            Debug.LogWarning($"⚠️ [ShipTrajectorySkill] 检测到 {overlappingCount} 对重叠的 CoreDeflector！");
            Debug.LogWarning("重叠的 CoreDeflector 可能同时影响飞船，导致轨迹预测不准确。");
        }
        
        if (showDebugLogs && !shipInMultipleRanges && overlappingCount == 0)
        {
            Debug.Log("✅ [ShipTrajectorySkill] CoreDeflector 检查通过：没有发现重叠或冲突");
        }
    }
}

