using System.Collections;
using UnityEngine;

/// <summary>
/// 飞船时停瞄准加速器
/// 按下B键时，触发时停效果，显示扇形瞄准区域和方向箭头，鼠标点击确认后执行boost加速
/// </summary>
public class ShipBoostAim : MonoBehaviour
{
    [Header("时停设置")]
    [Tooltip("时停时的时间缩放（0-1，越小越慢）")]
    [SerializeField] private float timeScale = 0.1f;

    [Tooltip("时停最大持续时间（秒，防止卡死）")]
    [SerializeField] private float maxAimTime = 10f;

    [Header("加速设置")]
    [Tooltip("加速力度（速度增量）")]
    [SerializeField] private float boostForce = 5f;

    [Tooltip("加速持续时间（秒）")]
    [SerializeField] private float boostDuration = 0.5f;

    [Tooltip("加速冷却时间（秒），0表示无冷却")]
    [SerializeField] private float cooldownTime = 2f;

    [Tooltip("最小速度阈值，低于此速度时无法加速")]
    [SerializeField] private float minVelocityForBoost = 0.5f;

    [Header("扇形设置")]
    [Tooltip("扇形角度范围（度，左右各45度）")]
    [SerializeField] private float sectorAngle = 45f;

    [Tooltip("扇形半径（用于显示）")]
    [SerializeField] private float sectorRadius = 5f;

    [Tooltip("扇形分段数（用于绘制）")]
    [SerializeField] private int sectorSegments = 20;

    [Header("可视化设置")]
    [Tooltip("是否显示可视化")]
    [SerializeField] private bool showVisualization = true;

    [Tooltip("扇形颜色")]
    [SerializeField] private Color sectorColor = new Color(1f, 1f, 0f, 0.3f);

    [Tooltip("扇形边框颜色")]
    [SerializeField] private Color sectorBorderColor = new Color(1f, 1f, 0f, 1f);

    [Tooltip("箭头颜色")]
    [SerializeField] private Color arrowColor = Color.red;

    [Tooltip("箭头长度")]
    [SerializeField] private float arrowLength = 3f;

    [Tooltip("箭头宽度")]
    [SerializeField] private float arrowWidth = 0.2f;

    [Header("输入设置")]
    [Tooltip("触发时停的按键")]
    [SerializeField] private KeyCode aimKey = KeyCode.B;

    [Tooltip("确认boost的鼠标按键（0=左键，1=右键，2=中键）")]
    [SerializeField] private int confirmMouseButton = 0;

    [Header("使用次数限制")]
    [Tooltip("Boost等级（用于升级系统，1级=1次，2级=2次，3级=3次）")]
    [SerializeField] private int boostLevel = 1;
    
    [Tooltip("每局最大使用次数（根据等级自动计算，也可以手动设置）\n等级1=1次，等级2=3次，等级3=5次，等级4=7次...")]
    [SerializeField] private int maxUsesPerRound = 1;
    
    [Tooltip("当前已使用次数（运行时自动更新，游戏重置时自动清零）")]
    [SerializeField] private int currentUses = 0;

    [Header("视觉效果（可选）")]
    [Tooltip("加速时的粒子效果")]
    [SerializeField] private ParticleSystem boostParticles;

    [Tooltip("加速时的音效")]
    [SerializeField] private AudioSource boostSound;

    [Tooltip("冲击波效果控制器")]
    [SerializeField] private ShockwaveController shockwaveController;

    // 内部状态
    private ShipState shipState;
    private NBody nBody;
    private GravityEngine gravityEngine;
    private Camera mainCamera;
    
    private bool isAiming = false; // 是否正在瞄准
    private bool isBoosting = false; // 是否正在加速
    private bool isOnCooldown = false; // 是否在冷却中
    private float cooldownTimer = 0f;
    private float originalTimeScale = 1f; // 保存原始时间缩放
    
    private Vector3 currentVelocityDirection = Vector3.up; // 当前飞行方向
    private Vector3 selectedBoostDirection = Vector3.up; // 选中的boost方向
    private Vector3 mouseWorldPosition; // 鼠标世界坐标位置

    // LineRenderer 组件用于绘制可视化
    private LineRenderer sectorLineRenderer;
    private LineRenderer arrowLineRenderer;
    private GameObject sectorVisualObject;
    private GameObject arrowVisualObject;

    void Awake()
    {
        // 获取组件
        shipState = GetComponent<ShipState>();
        if (shipState == null)
        {
            Debug.LogError($"ShipBoostAim: {gameObject.name} 缺少 ShipState 组件！");
        }

        nBody = GetComponent<NBody>();
        if (nBody == null)
        {
            Debug.LogError($"ShipBoostAim: {gameObject.name} 缺少 NBody 组件！");
        }

        // 获取主摄像机
        mainCamera = Camera.main;
        if (mainCamera == null)
        {
            mainCamera = FindObjectOfType<Camera>();
        }
    }

    void Start()
    {
        // 获取GravityEngine实例
        gravityEngine = GravityEngine.instance;
        if (gravityEngine == null)
        {
            Debug.LogError("ShipBoostAim: 场景中没有找到 GravityEngine！");
        }

        // 确保扇形角度至少为45度（防止Inspector中的旧值覆盖）
        if (sectorAngle < 45f)
        {
            Debug.LogWarning($"ShipBoostAim: sectorAngle ({sectorAngle}) 小于45度，已自动设置为45度。请在Inspector中检查并更新。");
            sectorAngle = 45f;
        }

        // 保存原始时间缩放
        originalTimeScale = Time.timeScale;

        // 根据等级计算最大使用次数（如果未手动设置）
        UpdateMaxUsesFromLevel();

        // 重置使用次数
        ResetUsageCount();

        // 订阅游戏重置事件
        if (EventManager.Instance != null)
        {
            EventManager.Instance.OnGameReset += HandleGameReset;
        }

        // 配置粒子系统使用未缩放时间（这样在时停状态下也能正常播放）
        ConfigureParticleSystemForTimeStop();

        // 创建可视化对象和LineRenderer
        CreateVisualizationObjects();
    }

    /// <summary>
    /// 创建可视化对象和LineRenderer组件
    /// </summary>
    private void CreateVisualizationObjects()
    {
        // 创建扇形可视化对象
        sectorVisualObject = new GameObject("SectorVisualization");
        sectorVisualObject.transform.SetParent(transform);
        sectorVisualObject.transform.localPosition = Vector3.zero;
        sectorLineRenderer = sectorVisualObject.AddComponent<LineRenderer>();
        sectorLineRenderer.material = new Material(Shader.Find("Sprites/Default"));
        sectorLineRenderer.startColor = sectorColor;
        sectorLineRenderer.endColor = sectorColor;
        sectorLineRenderer.startWidth = 0.1f;
        sectorLineRenderer.endWidth = 0.1f;
        sectorLineRenderer.useWorldSpace = true;
        sectorLineRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        sectorLineRenderer.receiveShadows = false;
        sectorLineRenderer.positionCount = 0; // 初始化为空
        sectorLineRenderer.enabled = false;

        // 创建箭头可视化对象
        arrowVisualObject = new GameObject("ArrowVisualization");
        arrowVisualObject.transform.SetParent(transform);
        arrowVisualObject.transform.localPosition = Vector3.zero;
        arrowLineRenderer = arrowVisualObject.AddComponent<LineRenderer>();
        arrowLineRenderer.material = new Material(Shader.Find("Sprites/Default"));
        arrowLineRenderer.startColor = arrowColor;
        arrowLineRenderer.endColor = arrowColor;
        arrowLineRenderer.startWidth = 0.15f;
        arrowLineRenderer.endWidth = 0.05f;
        arrowLineRenderer.useWorldSpace = true;
        arrowLineRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        arrowLineRenderer.receiveShadows = false;
        arrowLineRenderer.positionCount = 0; // 初始化为空
        arrowLineRenderer.enabled = false;
    }

    void Update()
    {
        // 更新冷却时间（使用未缩放时间）
        if (isOnCooldown)
        {
            cooldownTimer -= Time.unscaledDeltaTime;
            if (cooldownTimer <= 0f)
            {
                isOnCooldown = false;
                cooldownTimer = 0f;
            }
        }

        // 检查是否按下B键开始瞄准
        if (Input.GetKeyDown(aimKey) && !isAiming && !isBoosting && !isOnCooldown && HasRemainingUses())
        {
            StartAiming();
        }

        // 如果正在瞄准，处理瞄准逻辑
        if (isAiming)
        {
            HandleAiming();
        }
    }

    void OnDisable()
    {
        // 确保在禁用时恢复时间缩放
        if (isAiming)
        {
            EndAiming(false);
        }
        
        // 取消订阅事件
        if (EventManager.Instance != null)
        {
            EventManager.Instance.OnGameReset -= HandleGameReset;
        }
    }

    void OnDestroy()
    {
        // 清理可视化对象
        if (sectorVisualObject != null)
        {
            Destroy(sectorVisualObject);
        }
        if (arrowVisualObject != null)
        {
            Destroy(arrowVisualObject);
        }
    }

    /// <summary>
    /// 开始瞄准（触发时停）
    /// </summary>
    private void StartAiming()
    {
        // 检查是否在Flying或Captured状态
        if (shipState == null || 
            (shipState.CurrentState != ShipState.State.Flying && 
             shipState.CurrentState != ShipState.State.Captured))
        {
            Debug.Log($"瞄准失败：飞船不在Flying或Captured状态，当前状态: {shipState?.CurrentState}");
            return;
        }

        // 检查必要组件
        if (nBody == null || gravityEngine == null)
        {
            Debug.LogError("瞄准失败：缺少必要组件");
            return;
        }

        // 检查NBody是否已添加到引擎
        if (nBody.engineRef == null)
        {
            Debug.LogError("瞄准失败：飞船未添加到引力引擎");
            return;
        }

        // 获取当前速度方向
        Vector3 currentVelocity = gravityEngine.GetVelocity(nBody);
        
        // 在Captured状态下，如果速度太小，也允许瞄准（用于脱离捕获）
        bool isCaptured = shipState.CurrentState == ShipState.State.Captured;
        if (!isCaptured && currentVelocity.magnitude < minVelocityForBoost)
        {
            Debug.Log($"瞄准失败：速度过小（{currentVelocity.magnitude:F2} < {minVelocityForBoost}）");
            return;
        }

        // 计算当前飞行方向
        if (currentVelocity.magnitude > 0.01f)
        {
            currentVelocityDirection = currentVelocity.normalized;
            currentVelocityDirection.z = 0f; // 确保在XY平面
        }
        else
        {
            // 如果速度太小，使用飞船朝向
            currentVelocityDirection = transform.up;
            currentVelocityDirection.z = 0f;
        }

        // 初始化选中方向为当前飞行方向
        selectedBoostDirection = currentVelocityDirection;

        // ========== 同时触发以下效果（在同一帧内） ==========
        
        // 1. 触发时停
        isAiming = true;
        originalTimeScale = Time.timeScale;
        Time.timeScale = timeScale;

        // 2. 触发时停开始事件（通知其他系统，如相机效果）
        if (EventManager.Instance != null)
        {
            EventManager.Instance.TriggerTimeStopStart(gameObject, timeScale);
        }

        // 3. 显示扇形和箭头可视化（同时显示）
        if (showVisualization)
        {
            UpdateVisualization();
            if (sectorLineRenderer != null) sectorLineRenderer.enabled = true;
            if (arrowLineRenderer != null) arrowLineRenderer.enabled = true;
        }

        // 4. 播放粒子效果（同时播放一次）
        PlayTimeStopParticleEffect();

        // 5. 触发冲击波效果（以飞船屏幕位置为中心）
        if (shockwaveController != null && mainCamera != null)
        {
            Vector3 viewportPos = mainCamera.WorldToViewportPoint(transform.position);
            shockwaveController.TriggerShockwave(new Vector2(viewportPos.x, viewportPos.y));
        }
        
        // ========== 以上效果同时发生 ==========

        Debug.Log($"开始瞄准！当前飞行方向: {currentVelocityDirection}, 时停缩放: {timeScale}");
        
        // 启动超时协程
        StartCoroutine(AimTimeoutCoroutine());
    }

    /// <summary>
    /// 处理瞄准逻辑
    /// </summary>
    private void HandleAiming()
    {
        // 更新鼠标世界坐标位置
        UpdateMouseWorldPosition();

        // 计算从飞船到鼠标的方向
        Vector3 toMouse = mouseWorldPosition - transform.position;
        toMouse.z = 0f; // 确保在XY平面

        if (toMouse.magnitude > 0.01f)
        {
            Vector3 mouseDirection = toMouse.normalized;

            // 计算鼠标方向与当前飞行方向的夹角
            float angle = Vector3.SignedAngle(currentVelocityDirection, mouseDirection, Vector3.forward);

            // 限制在扇形范围内（±sectorAngle度）
            if (Mathf.Abs(angle) <= sectorAngle)
            {
                // 在扇形内，直接使用鼠标方向
                selectedBoostDirection = mouseDirection;
            }
            else
            {
                // 超出扇形范围，限制到扇形边界
                float clampedAngle = Mathf.Clamp(angle, -sectorAngle, sectorAngle);
                selectedBoostDirection = Quaternion.Euler(0f, 0f, clampedAngle) * currentVelocityDirection;
            }
        }

        // 更新可视化
        if (showVisualization)
        {
            UpdateVisualization();
        }

        // 检查鼠标点击确认
        if (Input.GetMouseButtonDown(confirmMouseButton))
        {
            ConfirmBoost();
        }
    }

    /// <summary>
    /// 更新鼠标世界坐标位置
    /// </summary>
    private void UpdateMouseWorldPosition()
    {
        if (mainCamera == null)
        {
            return;
        }

        Vector3 mouseScreenPos = Input.mousePosition;
        mouseScreenPos.z = mainCamera.nearClipPlane + 1f; // 设置一个合适的深度

        // 将屏幕坐标转换为世界坐标
        mouseWorldPosition = mainCamera.ScreenToWorldPoint(mouseScreenPos);
        mouseWorldPosition.z = transform.position.z; // 确保Z坐标一致
    }

    /// <summary>
    /// 确认boost方向并执行加速
    /// </summary>
    private void ConfirmBoost()
    {
        if (!isAiming)
        {
            return;
        }

        // 结束瞄准（恢复时间）
        EndAiming(true);

        // 执行boost
        ExecuteBoost();
    }

    /// <summary>
    /// 结束瞄准（恢复时间）
    /// </summary>
    /// <param name="confirmed">是否确认了boost方向</param>
    private void EndAiming(bool confirmed)
    {
        if (!isAiming)
        {
            return;
        }

        isAiming = false;
        Time.timeScale = originalTimeScale;

        // 触发时停结束事件
        if (EventManager.Instance != null)
        {
            EventManager.Instance.TriggerTimeStopEnd(gameObject);
        }

        // 禁用可视化并清理位置数据
        if (sectorLineRenderer != null)
        {
            sectorLineRenderer.enabled = false;
            sectorLineRenderer.positionCount = 0; // 清除位置数据，避免留下痕迹
        }
        if (arrowLineRenderer != null)
        {
            arrowLineRenderer.enabled = false;
            arrowLineRenderer.positionCount = 0; // 清除位置数据，避免留下痕迹
        }

        if (!confirmed)
        {
            Debug.Log("瞄准已取消");
        }
    }

    /// <summary>
    /// 执行boost加速
    /// </summary>
    private void ExecuteBoost()
    {
        if (isBoosting)
        {
            return;
        }

        // 检查是否还有剩余使用次数
        if (!HasRemainingUses())
        {
            Debug.LogWarning($"Boost使用次数已达上限（{maxUsesPerRound}次），无法使用！");
            return;
        }

        isBoosting = true;

        // 增加使用次数
        currentUses++;
        Debug.Log($"Boost使用次数: {currentUses}/{maxUsesPerRound}");

        // 计算加速增量
        Vector3 boostVelocity = selectedBoostDirection * boostForce;

        // 应用加速
        ApplyBoost(boostVelocity);

        // 通过EventManager触发加速事件
        if (EventManager.Instance != null)
        {
            EventManager.Instance.TriggerShipBoosted(selectedBoostDirection, boostForce, gameObject);
        }

        // 启动加速协程
        StartCoroutine(BoostCoroutine());

        // 播放视觉效果和音效
        PlayBoostEffects();

        Debug.Log($"Boost启动！方向: {selectedBoostDirection}, 速度增量: {boostVelocity}, 剩余次数: {GetRemainingUses()}");
    }

    /// <summary>
    /// 应用加速（直接修改速度）
    /// </summary>
    /// <param name="boostVelocity">加速速度增量</param>
    private void ApplyBoost(Vector3 boostVelocity)
    {
        if (nBody == null || gravityEngine == null || nBody.engineRef == null)
        {
            return;
        }

        // 获取当前速度
        Vector3 currentVelocity = gravityEngine.GetVelocity(nBody);

        // 计算新速度
        Vector3 newVelocity = currentVelocity + boostVelocity;

        // 更新速度
        gravityEngine.SetVelocity(nBody, newVelocity);

        Debug.Log($"速度更新: {currentVelocity} -> {newVelocity}");
    }

    /// <summary>
    /// 瞄准超时协程
    /// </summary>
    private IEnumerator AimTimeoutCoroutine()
    {
        // 使用未缩放时间等待
        yield return new WaitForSecondsRealtime(maxAimTime);

        // 如果还在瞄准状态，自动取消
        if (isAiming)
        {
            Debug.Log("瞄准超时，自动取消");
            EndAiming(false);
        }
    }

    /// <summary>
    /// 加速协程（处理持续时间和冷却）
    /// </summary>
    private IEnumerator BoostCoroutine()
    {
        // 等待加速持续时间
        yield return new WaitForSeconds(boostDuration);

        // 加速结束
        isBoosting = false;

        // 启动冷却
        if (cooldownTime > 0f)
        {
            isOnCooldown = true;
            cooldownTimer = cooldownTime;
        }

        // 停止视觉效果
        StopBoostEffects();

        Debug.Log("加速结束");
    }

    /// <summary>
    /// 播放加速效果
    /// </summary>
    private void PlayBoostEffects()
    {
        // 播放粒子效果
        if (boostParticles != null && !boostParticles.isPlaying)
        {
            boostParticles.Play();
        }

        // 播放音效
        if (boostSound != null && !boostSound.isPlaying)
        {
            boostSound.Play();
        }
    }

    /// <summary>
    /// 停止加速效果
    /// </summary>
    private void StopBoostEffects()
    {
        // 停止粒子效果
        if (boostParticles != null && boostParticles.isPlaying)
        {
            boostParticles.Stop();
        }

        // 停止音效
        if (boostSound != null && boostSound.isPlaying)
        {
            boostSound.Stop();
        }
    }

    /// <summary>
    /// 配置粒子系统以在时停状态下正常工作，并设置为爆发式技能特效
    /// </summary>
    private void ConfigureParticleSystemForTimeStop()
    {
        if (boostParticles == null)
        {
            return;
        }

        // 设置粒子系统使用未缩放时间，这样在时停状态下也能正常播放
        var main = boostParticles.main;
        main.useUnscaledTime = true;
        
        // 确保粒子系统位置在飞船中心
        if (boostParticles.transform.parent != transform)
        {
            // 如果粒子系统不是飞船的子对象，设置其位置
            boostParticles.transform.position = transform.position;
        }
        else
        {
            // 如果是子对象，确保本地位置为0（在飞船中心）
            boostParticles.transform.localPosition = Vector3.zero;
        }
        
        Debug.Log("ShipBoostAim: 粒子系统已配置为使用未缩放时间（useUnscaledTime = true）");
    }

    /// <summary>
    /// 播放时停触发的粒子效果（点击B键时触发）
    /// 以飞船为中心向外爆发式发射粒子
    /// </summary>
    private void PlayTimeStopParticleEffect()
    {
        // 播放粒子效果（时停触发时播放一次）
        if (boostParticles == null)
        {
            Debug.LogWarning("ShipBoostAim: boostParticles 未赋值！请在Inspector中分配粒子系统。");
            return;
        }

        // 检查粒子系统是否被禁用
        if (!boostParticles.gameObject.activeInHierarchy)
        {
            Debug.LogWarning("ShipBoostAim: 粒子系统GameObject被禁用，无法播放。");
            return;
        }

        // 确保粒子系统位置在飞船中心
        if (boostParticles.transform.parent == transform)
        {
            // 如果是飞船的子对象，确保在中心
            boostParticles.transform.localPosition = Vector3.zero;
        }
        else
        {
            // 如果不是子对象，设置世界位置为飞船位置
            boostParticles.transform.position = transform.position;
        }

        // 确保粒子系统使用未缩放时间（防止时停影响粒子播放）
        var main = boostParticles.main;
        if (!main.useUnscaledTime)
        {
            main.useUnscaledTime = true;
        }

        // 如果粒子系统正在播放，先停止并清除所有粒子
        if (boostParticles.isPlaying || boostParticles.isPaused)
        {
            boostParticles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        }

        // 播放粒子效果（使用 true 参数清除之前的粒子）
        // 这会触发一次爆发式发射
        boostParticles.Play(true);
        
        Debug.Log($"ShipBoostAim: 技能特效已触发！粒子从飞船中心 ({transform.position}) 向外爆发发射");
    }

    /// <summary>
    /// 绘制可视化（在Scene视图中）
    /// </summary>
    void OnDrawGizmos()
    {
        if (!showVisualization)
        {
            return;
        }

        // 只在运行时且正在瞄准时绘制
        if (!Application.isPlaying || !isAiming)
        {
            return;
        }

        // 绘制扇形
        DrawSector();

        // 绘制箭头
        DrawArrow();
    }

    /// <summary>
    /// 绘制扇形
    /// </summary>
    private void DrawSector()
    {
        Vector3 shipPos = transform.position;
        shipPos.z = 0f;

        // 计算扇形的左右边界角度
        float leftAngle = -sectorAngle;
        float rightAngle = sectorAngle;

        // 计算当前飞行方向的角度
        float baseAngle = Mathf.Atan2(currentVelocityDirection.y, currentVelocityDirection.x) * Mathf.Rad2Deg;

        // 绘制扇形边界线
        Gizmos.color = sectorBorderColor;
        
        // 左边界
        Vector3 leftDir = Quaternion.Euler(0f, 0f, baseAngle + leftAngle) * Vector3.right;
        Gizmos.DrawLine(shipPos, shipPos + leftDir * sectorRadius);

        // 右边界
        Vector3 rightDir = Quaternion.Euler(0f, 0f, baseAngle + rightAngle) * Vector3.right;
        Gizmos.DrawLine(shipPos, shipPos + rightDir * sectorRadius);

        // 绘制扇形弧线
        Gizmos.color = sectorColor;
        Vector3 prevPoint = shipPos + leftDir * sectorRadius;
        for (int i = 1; i <= sectorSegments; i++)
        {
            float t = (float)i / sectorSegments;
            float angle = Mathf.Lerp(leftAngle, rightAngle, t);
            Vector3 dir = Quaternion.Euler(0f, 0f, baseAngle + angle) * Vector3.right;
            Vector3 currentPoint = shipPos + dir * sectorRadius;
            
            Gizmos.DrawLine(shipPos, currentPoint);
            Gizmos.DrawLine(prevPoint, currentPoint);
            
            prevPoint = currentPoint;
        }
    }

    /// <summary>
    /// 绘制方向箭头
    /// </summary>
    private void DrawArrow()
    {
        Vector3 shipPos = transform.position;
        shipPos.z = 0f;

        Vector3 arrowStart = shipPos;
        Vector3 arrowEnd = shipPos + selectedBoostDirection * arrowLength;

        // 绘制箭头主线
        Gizmos.color = arrowColor;
        Gizmos.DrawLine(arrowStart, arrowEnd);

        // 绘制箭头头部（三角形）
        Vector3 arrowHeadSize = selectedBoostDirection * arrowWidth;
        Vector3 perpendicular = new Vector3(-selectedBoostDirection.y, selectedBoostDirection.x, 0f) * arrowWidth;

        Vector3 headTip = arrowEnd;
        Vector3 headLeft = arrowEnd - arrowHeadSize + perpendicular;
        Vector3 headRight = arrowEnd - arrowHeadSize - perpendicular;

        Gizmos.DrawLine(headTip, headLeft);
        Gizmos.DrawLine(headTip, headRight);
        Gizmos.DrawLine(headLeft, headRight);
    }

    /// <summary>
    /// 更新可视化（使用LineRenderer）
    /// </summary>
    private void UpdateVisualization()
    {
        if (!isAiming || sectorLineRenderer == null || arrowLineRenderer == null)
        {
            return;
        }

        UpdateSectorVisualization();
        UpdateArrowVisualization();
    }

    /// <summary>
    /// 更新扇形可视化
    /// </summary>
    private void UpdateSectorVisualization()
    {
        Vector3 shipPos = transform.position;
        shipPos.z = 0f;

        float baseAngle = Mathf.Atan2(currentVelocityDirection.y, currentVelocityDirection.x) * Mathf.Rad2Deg;
        float leftAngle = -sectorAngle;
        float rightAngle = sectorAngle;

        // 计算扇形顶点：中心点 -> 左边界 -> 弧线点 -> 右边界 -> 中心点（形成闭合扇形）
        int pointCount = sectorSegments + 4; // 中心点 + 左边界 + 弧线点 + 右边界 + 回到中心
        Vector3[] positions = new Vector3[pointCount];

        int index = 0;

        // 第一个点是中心点
        positions[index++] = shipPos;

        // 左边界点
        Vector3 leftDir = Quaternion.Euler(0f, 0f, baseAngle + leftAngle) * Vector3.right;
        positions[index++] = shipPos + leftDir * sectorRadius;

        // 弧线点（从左到右）
        for (int i = 1; i <= sectorSegments; i++)
        {
            float t = (float)i / sectorSegments;
            float angle = Mathf.Lerp(leftAngle, rightAngle, t);
            Vector3 dir = Quaternion.Euler(0f, 0f, baseAngle + angle) * Vector3.right;
            positions[index++] = shipPos + dir * sectorRadius;
        }

        // 右边界点
        Vector3 rightDir = Quaternion.Euler(0f, 0f, baseAngle + rightAngle) * Vector3.right;
        positions[index++] = shipPos + rightDir * sectorRadius;

        // 最后回到中心点（形成闭合）
        positions[index++] = shipPos;

        sectorLineRenderer.positionCount = pointCount;
        sectorLineRenderer.SetPositions(positions);
        sectorLineRenderer.startColor = sectorColor;
        sectorLineRenderer.endColor = sectorColor;
        sectorLineRenderer.startWidth = 0.15f;
        sectorLineRenderer.endWidth = 0.15f;
        sectorLineRenderer.loop = false;
    }

    /// <summary>
    /// 更新箭头可视化
    /// </summary>
    private void UpdateArrowVisualization()
    {
        Vector3 shipPos = transform.position;
        shipPos.z = 0f;

        Vector3 arrowStart = shipPos;
        Vector3 arrowEnd = shipPos + selectedBoostDirection * arrowLength;

        // 箭头头部三角形
        Vector3 arrowHeadSize = selectedBoostDirection * arrowWidth;
        Vector3 perpendicular = new Vector3(-selectedBoostDirection.y, selectedBoostDirection.x, 0f) * arrowWidth;

        Vector3 headTip = arrowEnd;
        Vector3 headLeft = arrowEnd - arrowHeadSize + perpendicular;
        Vector3 headRight = arrowEnd - arrowHeadSize - perpendicular;

        // 箭头由4个点组成：起点 -> 头部尖端 -> 头部左 -> 头部尖端 -> 头部右
        // 这样形成一条主线 + 三角形头部
        Vector3[] positions = new Vector3[5];
        positions[0] = arrowStart;
        positions[1] = headTip; // 主线到尖端
        positions[2] = headLeft; // 尖端到左
        positions[3] = headTip; // 回到尖端
        positions[4] = headRight; // 尖端到右

        arrowLineRenderer.positionCount = 5;
        arrowLineRenderer.SetPositions(positions);
        arrowLineRenderer.startColor = arrowColor;
        arrowLineRenderer.endColor = arrowColor;
        arrowLineRenderer.startWidth = 0.2f;
        arrowLineRenderer.endWidth = 0.2f;
        arrowLineRenderer.loop = false;
    }

    /// <summary>
    /// 获取冷却剩余时间（用于UI显示）
    /// </summary>
    public float GetCooldownRemaining()
    {
        return Mathf.Max(0f, cooldownTimer);
    }

    /// <summary>
    /// 获取冷却进度（0-1，用于UI显示）
    /// </summary>
    public float GetCooldownProgress()
    {
        if (cooldownTime <= 0f)
        {
            return 1f; // 无冷却，始终可用
        }
        return 1f - (cooldownTimer / cooldownTime);
    }

    /// <summary>
    /// 检查是否可以boost
    /// </summary>
    public bool CanBoost()
    {
        if (shipState == null || 
            (shipState.CurrentState != ShipState.State.Flying && 
             shipState.CurrentState != ShipState.State.Captured))
        {
            return false;
        }

        if (isAiming || isBoosting || isOnCooldown)
        {
            return false;
        }

        // 检查是否还有剩余使用次数
        if (!HasRemainingUses())
        {
            return false;
        }

        if (nBody == null || gravityEngine == null || nBody.engineRef == null)
        {
            return false;
        }

        // 在Captured状态下，即使速度很小也允许boost（用于脱离）
        bool isCaptured = shipState.CurrentState == ShipState.State.Captured;
        if (isCaptured)
        {
            return true;
        }

        Vector3 currentVelocity = gravityEngine.GetVelocity(nBody);
        return currentVelocity.magnitude >= minVelocityForBoost;
    }

    /// <summary>
    /// 根据等级计算最大使用次数
    /// 等级1=1次，等级2=3次，等级3=5次，等级4=7次...（公式：2*level-1）
    /// </summary>
    private void UpdateMaxUsesFromLevel()
    {
        // 如果maxUsesPerRound已经被手动设置过（不为默认值1），则不自动计算
        // 否则根据等级自动计算
        if (maxUsesPerRound == 1 && boostLevel > 1)
        {
            maxUsesPerRound = CalculateMaxUsesFromLevel(boostLevel);
        }
    }

    /// <summary>
    /// 根据等级计算最大使用次数
    /// </summary>
    /// <param name="level">Boost等级</param>
    /// <returns>最大使用次数</returns>
    private int CalculateMaxUsesFromLevel(int level)
    {
        // 等级1=1次，等级2=2次，等级3=3次...
        // 公式：直接等于等级
        return Mathf.Max(1, level);
    }

    /// <summary>
    /// 检查是否还有剩余使用次数
    /// </summary>
    public bool HasRemainingUses()
    {
        return currentUses < maxUsesPerRound;
    }

    /// <summary>
    /// 获取剩余使用次数
    /// </summary>
    public int GetRemainingUses()
    {
        return Mathf.Max(0, maxUsesPerRound - currentUses);
    }

    /// <summary>
    /// 获取当前已使用次数
    /// </summary>
    public int GetCurrentUses()
    {
        return currentUses;
    }

    /// <summary>
    /// 获取最大使用次数
    /// </summary>
    public int GetMaxUses()
    {
        return maxUsesPerRound;
    }

    /// <summary>
    /// 重置使用次数（游戏重置时调用）
    /// </summary>
    public void ResetUsageCount()
    {
        currentUses = 0;
        Debug.Log($"Boost使用次数已重置，当前等级: {boostLevel}, 最大次数: {maxUsesPerRound}");
    }

    /// <summary>
    /// 设置Boost等级（升级时调用）
    /// </summary>
    /// <param name="newLevel">新等级</param>
    public void SetBoostLevel(int newLevel)
    {
        if (newLevel < 1)
        {
            Debug.LogWarning($"ShipBoostAim: 尝试设置无效的等级 {newLevel}，已设置为1");
            newLevel = 1;
        }

        boostLevel = newLevel;
        maxUsesPerRound = CalculateMaxUsesFromLevel(boostLevel);
        
        Debug.Log($"Boost等级已升级到 {boostLevel}，最大使用次数: {maxUsesPerRound}");
    }

    /// <summary>
    /// 获取当前Boost等级
    /// </summary>
    public int GetBoostLevel()
    {
        return boostLevel;
    }

    /// <summary>
    /// 游戏重置事件处理
    /// </summary>
    private void HandleGameReset()
    {
        ResetUsageCount();
        Debug.Log("ShipBoostAim: 游戏重置，Boost使用次数已清零");
    }
}

