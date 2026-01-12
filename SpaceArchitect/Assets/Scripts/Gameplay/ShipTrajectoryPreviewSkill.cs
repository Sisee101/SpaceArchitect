using UnityEngine;

/// <summary>
/// 飞船轨道预览技能
/// 在发射前（PreLaunch状态）按Y键显示轨迹预览，帮助玩家规划发射路径
/// </summary>
[RequireComponent(typeof(ShipState))]
public class ShipTrajectoryPreviewSkill : MonoBehaviour
{
    [Header("输入设置")]
    [Tooltip("触发预览的按键（按Y键显示预测线）")]
    [SerializeField] private KeyCode previewKey = KeyCode.Y;
    
    [Header("预览设置")]
    [Tooltip("是否在发射后自动隐藏预览")]
    [SerializeField] private bool hideOnLaunch = true;
    
    [Tooltip("预览更新间隔（秒），越小越实时但性能消耗越大）")]
    [SerializeField] private float previewUpdateInterval = 0.05f;
    
    [Header("轨迹预测器设置")]
    [Tooltip("轨迹预测器组件（如果为空，将自动查找）")]
    [SerializeField] private TrajectoryPredictor trajectoryPredictor;
    
    [Tooltip("如果找不到TrajectoryPredictor，是否自动创建")]
    [SerializeField] private bool autoCreatePredictor = false;
    
    [Header("发射速度设置")]
    [Tooltip("预览时使用的默认发射速度（如果发射器未提供）")]
    [SerializeField] private Vector3 defaultLaunchVelocity = new Vector3(5f, 0f, 0f);
    
    [Tooltip("是否从发射器获取当前发射速度（ShipLauncher或ShipSimpleLauncher）")]
    [SerializeField] private bool useLauncherVelocity = true;
    
    [Header("调试")]
    [Tooltip("显示调试信息")]
    [SerializeField] private bool showDebugLogs = false;
    
    // 内部状态
    private ShipState shipState;
    private ShipLauncher shipLauncher;
    private ShipSimpleLauncher shipSimpleLauncher;
    private Camera mainCamera;
    private bool isPreviewActive = false;
    private float lastPreviewUpdateTime = 0f;
    private Vector3 lastPreviewVelocity = Vector3.zero;
    
    void Awake()
    {
        // 获取组件
        shipState = GetComponent<ShipState>();
        if (shipState == null)
        {
            Debug.LogError($"ShipTrajectoryPreviewSkill: {gameObject.name} 缺少 ShipState 组件！");
        }
        
        shipLauncher = GetComponent<ShipLauncher>();
        shipSimpleLauncher = GetComponent<ShipSimpleLauncher>();
        
        if (shipLauncher == null && shipSimpleLauncher == null && showDebugLogs)
        {
            Debug.LogWarning($"ShipTrajectoryPreviewSkill: {gameObject.name} 没有找到发射器组件（ShipLauncher 或 ShipSimpleLauncher），将使用默认发射速度");
        }
        else if (showDebugLogs)
        {
            if (shipLauncher != null)
            {
                Debug.Log($"ShipTrajectoryPreviewSkill: 找到 ShipLauncher 组件");
            }
            if (shipSimpleLauncher != null)
            {
                Debug.Log($"ShipTrajectoryPreviewSkill: 找到 ShipSimpleLauncher 组件");
            }
        }
        
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
                    Debug.Log($"ShipTrajectoryPreviewSkill: 已自动创建 TrajectoryPredictor 组件");
                }
            }
        }
        
        if (trajectoryPredictor == null)
        {
            Debug.LogWarning($"ShipTrajectoryPreviewSkill: {gameObject.name} 没有找到 TrajectoryPredictor 组件，预览功能将不可用。请在Inspector中手动指定或启用 autoCreatePredictor。");
        }
        else
        {
            // 设置 TrajectoryPredictor 为预览模式
            trajectoryPredictor.SetPredictionMode(TrajectoryPredictor.PredictionMode.Preview);
            
            if (showDebugLogs)
            {
                Debug.Log($"ShipTrajectoryPreviewSkill: 已找到 TrajectoryPredictor 组件，预览功能已启用");
            }
        }
        
        // 订阅发射事件和状态变化事件，在发射后隐藏预览
        if (EventManager.Instance != null)
        {
            EventManager.Instance.OnShipLaunched += OnShipLaunched;
            EventManager.Instance.OnShipStateChanged += OnShipStateChanged;
        }
    }
    
    void OnEnable()
    {
        // 订阅发射事件和状态变化事件
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
        // 只在 PreLaunch 状态下响应
        if (shipState == null || shipState.CurrentState != ShipState.State.PreLaunch)
        {
            // 如果不在 PreLaunch 状态，隐藏预览
            if (isPreviewActive)
            {
                HidePreview();
            }
            return;
        }
        
        // 检测 Y 键输入（显示预览）
        if (Input.GetKeyDown(previewKey))
        {
            if (!isPreviewActive)
            {
                // 如果预览未激活，显示预览
                ShowPreview();
            }
            // 注意：按 Y 键只显示，不隐藏（隐藏由发射触发）
        }
        
        // 如果预览激活，实时更新预览轨迹（跟随鼠标方向）
        if (isPreviewActive && trajectoryPredictor != null)
        {
            // 按更新间隔更新预览轨迹
            if (Time.time - lastPreviewUpdateTime >= previewUpdateInterval)
            {
                UpdatePreviewTrajectory();
                lastPreviewUpdateTime = Time.time;
            }
        }
    }
    
    /// <summary>
    /// 显示预览
    /// </summary>
    public void ShowPreview()
    {
        if (trajectoryPredictor == null)
        {
            if (showDebugLogs)
            {
                Debug.LogWarning("ShipTrajectoryPreviewSkill: 无法显示预览，TrajectoryPredictor 为空");
            }
            return;
        }
        
        if (shipState == null || shipState.CurrentState != ShipState.State.PreLaunch)
        {
            if (showDebugLogs)
            {
                Debug.LogWarning($"ShipTrajectoryPreviewSkill: 无法显示预览，飞船不在 PreLaunch 状态（当前状态: {shipState?.CurrentState}）");
            }
            return;
        }
        
        // 更新状态
        isPreviewActive = true;
        lastPreviewUpdateTime = Time.time;
        
        // 立即更新一次预览轨迹
        UpdatePreviewTrajectory();
        
        if (showDebugLogs)
        {
            Debug.Log("ShipTrajectoryPreviewSkill: 预览已显示（按鼠标左键发射，发射后自动隐藏）");
        }
    }
    
    /// <summary>
    /// 隐藏预览
    /// </summary>
    public void HidePreview()
    {
        if (trajectoryPredictor != null)
        {
            trajectoryPredictor.HideTrajectory();
            trajectoryPredictor.ClearPreviewTrajectory();
        }
        
        isPreviewActive = false;
        lastPreviewUpdateTime = 0f;
        lastPreviewVelocity = Vector3.zero;
        
        if (showDebugLogs)
        {
            Debug.Log("ShipTrajectoryPreviewSkill: 预览已隐藏");
        }
    }
    
    /// <summary>
    /// 更新预览轨迹（实时跟随鼠标方向）
    /// </summary>
    private void UpdatePreviewTrajectory()
    {
        if (trajectoryPredictor == null || shipState == null || shipState.CurrentState != ShipState.State.PreLaunch)
        {
            return;
        }
        
        // 获取当前发射位置和速度（跟随鼠标方向）
        Vector3 launchPosition = transform.position;
        Vector3 launchVelocity = GetLaunchVelocity();
        
        // 如果速度变化超过阈值，更新预览轨迹
        if (Vector3.Distance(launchVelocity, lastPreviewVelocity) > 0.01f || lastPreviewVelocity.magnitude < 0.01f)
        {
            // 设置预览轨迹
            trajectoryPredictor.SetPreviewTrajectory(launchPosition, launchVelocity);
            
            // 确保轨迹可见
            trajectoryPredictor.ShowTrajectory();
            
            lastPreviewVelocity = launchVelocity;
            
            if (showDebugLogs && Time.frameCount % 30 == 0) // 每30帧输出一次，避免日志过多
            {
                Debug.Log($"ShipTrajectoryPreviewSkill: 预览轨迹已更新 - 速度: {launchVelocity.magnitude:F2}, 方向: {launchVelocity.normalized}");
            }
        }
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
                
                if (dragStartPositionField != null)
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
        
        // 发射后隐藏预览
        if (hideOnLaunch && isPreviewActive)
        {
            HidePreview();
            
            if (showDebugLogs)
            {
                Debug.Log("ShipTrajectoryPreviewSkill: 飞船已发射，预览已自动隐藏");
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
        
        // 如果从 PreLaunch 转换到 Flying 状态，自动隐藏预览
        if (oldState == ShipState.State.PreLaunch && newState == ShipState.State.Flying)
        {
            if (hideOnLaunch && isPreviewActive)
            {
                HidePreview();
                
                if (showDebugLogs)
                {
                    Debug.Log("ShipTrajectoryPreviewSkill: 飞船状态从 PreLaunch 变为 Flying，预览已自动隐藏");
                }
            }
        }
        // 如果不在 PreLaunch 状态，也隐藏预览
        else if (newState != ShipState.State.PreLaunch && isPreviewActive)
        {
            HidePreview();
            
            if (showDebugLogs)
            {
                Debug.Log($"ShipTrajectoryPreviewSkill: 飞船状态变为 {newState}，预览已自动隐藏");
            }
        }
    }
    
    /// <summary>
    /// 检查预览是否激活
    /// </summary>
    public bool IsPreviewActive()
    {
        return isPreviewActive;
    }
    
    /// <summary>
    /// 设置预览按键
    /// </summary>
    public void SetPreviewKey(KeyCode key)
    {
        previewKey = key;
    }
}

