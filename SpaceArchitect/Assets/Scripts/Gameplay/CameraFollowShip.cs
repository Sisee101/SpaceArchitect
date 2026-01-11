using System.Collections;
using UnityEngine;

/// <summary>
/// 摄像机水平跟随飞船脚本
/// 只改变摄像机的X坐标，保持Y和Z坐标不变
/// </summary>
public class CameraFollowShip : MonoBehaviour
{
    [Header("跟随设置")]
    [Tooltip("要跟随的飞船GameObject（如果为空，将自动查找）")]
    [SerializeField] private Transform shipTransform;

    [Tooltip("跟随的平滑速度（0表示立即跟随，值越大越平滑）")]
    [SerializeField] private float followSpeed = 5f;

    [Tooltip("是否使用平滑跟随")]
    [SerializeField] private bool useSmoothing = true;

    [Header("偏移设置")]
    [Tooltip("X轴偏移量（摄像机和飞船的X坐标差值）")]
    [SerializeField] private float xOffset = 0f;
    
    [Tooltip("Y轴偏移量（摄像机和飞船的Y坐标差值）")]
    [SerializeField] private float yOffset = 0f;
    
    [Header("Y轴跟随边界设置")]
    [Tooltip("是否启用Y轴跟随边界限制（飞行状态下）")]
    [SerializeField] private bool enableYBoundary = true;
    
    [Tooltip("Y轴跟随的最小值（相机不会跟随到比这更低的Y坐标）")]
    [SerializeField] private float minYBoundary = -50f;
    
    [Tooltip("Y轴跟随的最大值（相机不会跟随到比这更高的Y坐标）")]
    [SerializeField] private float maxYBoundary = 50f;

    [Header("手动移动设置")]
    [Tooltip("是否启用A/D键手动移动摄像机")]
    [SerializeField] private bool enableManualMove = true;

    [Tooltip("手动移动速度（单位/秒）")]
    [SerializeField] private float manualMoveSpeed = 10f;

    [Tooltip("左移按键（默认A键）")]
    [SerializeField] private KeyCode leftMoveKey = KeyCode.A;

    [Tooltip("右移按键（默认D键）")]
    [SerializeField] private KeyCode rightMoveKey = KeyCode.D;

    [Header("缩放设置")]
    [Tooltip("是否启用鼠标滚轮缩放")]
    [SerializeField] private bool enableZoom = true;

    [Tooltip("缩放速度（滚轮滚动时的缩放倍数）")]
    [SerializeField] private float zoomSpeed = 2f;

    [Tooltip("最小缩放值（正交摄像机的orthographicSize最小值，或透视摄像机的fieldOfView最大值）")]
    [SerializeField] private float minZoom = 0f; // 0表示使用自动计算

    [Tooltip("最大缩放值（正交摄像机的orthographicSize最大值，或透视摄像机的fieldOfView最小值）")]
    [SerializeField] private float maxZoom = 0f; // 0表示使用自动计算

    [Header("READY按钮镜头动画设置")]
    [Tooltip("总览全局时的缩放倍数（相对于初始大小，大于1表示缩小看全局）\n如果指定了总览范围对象，此值将被忽略")]
    [SerializeField] private float overviewZoomScale = 3f;

    [Tooltip("总览范围模式：\n- ManualObject: 使用手动指定的GameObject或Collider范围\n- AutoCalculate: 自动计算Station和Destination范围\n- FixedScale: 使用固定的缩放倍数")]
    [SerializeField] private OverviewRangeMode overviewRangeMode = OverviewRangeMode.ManualObject;

    [Tooltip("手动指定的总览范围对象（GameObject或Collider）\n如果指定了，相机将显示这个对象的范围\n优先级最高")]
    [SerializeField] private GameObject overviewRangeObject;

    [Tooltip("Station的Tag（用于自动计算模式）")]
    [SerializeField] private string stationTag = "Station";

    [Tooltip("Destination的Tag（用于自动计算模式）")]
    [SerializeField] private string destinationTag = "Destination";

    [Tooltip("总览时的边距（在计算出的范围基础上增加边距，确保不会贴边）")]
    [SerializeField] private float overviewPadding = 2f;

    /// <summary>
    /// 总览范围模式枚举
    /// </summary>
    public enum OverviewRangeMode
    {
        ManualObject,    // 使用手动指定的GameObject或Collider
        AutoCalculate,   // 自动计算Station和Destination
        FixedScale       // 使用固定的缩放倍数
    }

    [Tooltip("缩小到总览的动画时间（秒）")]
    [SerializeField] private float zoomOutDuration = 1f;

    [Tooltip("总览停留时间（秒）")]
    [SerializeField] private float overviewHoldDuration = 1.5f;

    [Tooltip("回到聚焦的动画时间（秒）")]
    [SerializeField] private float zoomInDuration = 1f;

    [Tooltip("缩放动画的缓动曲线")]
    [SerializeField] private AnimationCurve zoomCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

    [Header("调试")]
    [Tooltip("显示调试信息")]
    [SerializeField] private bool showDebugLogs = false;

    private Vector3 initialPosition; // 保存初始位置（用于保持Y和Z不变）
    private Camera cameraComponent; // 摄像机组件引用
    private float initialOrthographicSize; // 初始正交大小（用于正交摄像机，这是聚焦飞船时的值）
    private float initialFieldOfView; // 初始视野角度（用于透视摄像机，这是聚焦飞船时的值）
    private bool isOrthographic; // 是否为正交摄像机
    private float manualMoveOffset = 0f; // 手动移动的累积偏移量
    private ShipState shipState; // 飞船状态组件引用（用于检查飞船状态）
    private bool isAnimating = false; // 是否正在执行动画（动画期间禁用手动缩放）
    private Coroutine readySequenceCoroutine; // READY序列协程
    private bool hasReadyClicked = false; // 是否已经点击过READY按钮（READY后禁用手动移动和缩放）

    void Awake()
    {
        // 获取摄像机组件
        cameraComponent = GetComponent<Camera>();
        if (cameraComponent == null)
        {
            Debug.LogError("CameraFollowShip: 该GameObject上没有Camera组件！");
            return;
        }

        // 保存初始位置（Y和Z坐标）
        initialPosition = transform.position;

        // 保存初始缩放值
        isOrthographic = cameraComponent.orthographic;
        if (isOrthographic)
        {
            // 正交摄像机：控制 orthographicSize（值越大，看到的范围越大）
            initialOrthographicSize = cameraComponent.orthographicSize;
            
            // 如果maxZoom未设置（为0），自动设置为初始值的20倍，这样可以zoom out看更大的全局视野
            if (maxZoom <= 0)
            {
                maxZoom = initialOrthographicSize * 20f; // 20倍初始值，可以zoom out看更大的全局视野
            }
            
            // 如果minZoom未设置（为0），自动设置为初始值（不放大，保持正常大小即可）
            if (minZoom <= 0)
            {
                minZoom = initialOrthographicSize; // 等于初始值，保持正常视野
            }
            
            // 确保minZoom < maxZoom
            if (minZoom >= maxZoom)
            {
                minZoom = maxZoom * 0.2f;
            }
            
            if (showDebugLogs)
            {
                Debug.Log($"摄像机缩放范围初始化：初始orthographicSize={initialOrthographicSize:F2}, minZoom={minZoom:F2}, maxZoom={maxZoom:F2}");
            }
        }
        else
        {
            // 透视摄像机：控制 fieldOfView（值越小，看到的范围越小，物体越大）
            initialFieldOfView = cameraComponent.fieldOfView;
            
            // 对于透视摄像机：fieldOfView越小=视野越窄=放大
            // 所以minZoom应该是较小的fieldOfView值（放大），maxZoom是较大的fieldOfView值（看全局）
            
            // 如果未设置，自动计算
            if (maxZoom <= 0 || maxZoom >= 180)
            {
                maxZoom = Mathf.Min(initialFieldOfView * 2f, 120f); // 最大120度视野（看全局）
            }
            if (minZoom <= 0)
            {
                minZoom = Mathf.Max(initialFieldOfView * 0.3f, 10f); // 最小10度视野（放大）
            }
            
            // 确保minZoom < maxZoom（对于透视摄像机，minZoom是较小的FOV值）
            if (minZoom >= maxZoom)
            {
                minZoom = maxZoom * 0.3f;
            }
            
            if (showDebugLogs)
            {
                Debug.Log($"摄像机缩放范围初始化：初始fieldOfView={initialFieldOfView:F2}, minZoom={minZoom:F2}, maxZoom={maxZoom:F2}");
            }
        }
    }

    void Start()
    {
        // 如果没有指定飞船，尝试自动查找
        if (shipTransform == null)
        {
            FindShip();
        }

        if (shipTransform == null)
        {
            Debug.LogWarning("CameraFollowShip: 未找到飞船，跟随功能将不可用。请在Inspector中手动指定飞船Transform。");
        }
        else
        {
            // 获取飞船的ShipState组件，用于检查飞船状态
            shipState = shipTransform.GetComponent<ShipState>();
            if (shipState == null && showDebugLogs)
            {
                Debug.LogWarning("CameraFollowShip: 飞船没有ShipState组件，将无法检测飞船状态。");
            }
        }

        // 订阅游戏重置事件
        if (EventManager.Instance != null)
        {
            EventManager.Instance.OnGameReset += OnGameReset;
        }

        // 初始状态：设置为总览全局的缩放值，显示整个场景
        // 这样进入场景时就可以看到所有内容，方便摆放行星
        SetOverviewZoom();

        if (showDebugLogs)
        {
            Debug.Log("CameraFollowShip: 初始状态已设置为总览全局，可以手动移动和缩放");
        }
    }

    void OnEnable()
    {
        // 如果EventManager已存在，订阅游戏重置事件
        if (EventManager.Instance != null)
        {
            EventManager.Instance.OnGameReset += OnGameReset;
        }
    }

    void OnDisable()
    {
        // 取消订阅游戏重置事件
        if (EventManager.Instance != null)
        {
            EventManager.Instance.OnGameReset -= OnGameReset;
        }
    }

    /// <summary>
    /// 游戏重置事件回调
    /// 重置相机到初始Setup状态（完全回到游戏开始时的状态）
    /// </summary>
    private void OnGameReset()
    {
        if (showDebugLogs)
        {
            Debug.Log("CameraFollowShip: 收到游戏重置事件，重置相机到初始Setup状态");
        }

        // 1. 停止任何正在进行的动画
        if (readySequenceCoroutine != null)
        {
            StopCoroutine(readySequenceCoroutine);
            readySequenceCoroutine = null;
        }
        isAnimating = false;

        // 2. 重置READY标志，允许手动移动和缩放（回到Setup状态）
        hasReadyClicked = false;

        // 3. 清除手动移动偏移
        manualMoveOffset = 0f;

        // 4. 重置到总览全局的缩放值（显示整个场景）
        SetOverviewZoom();

        // 5. 重置相机位置到初始位置（Y和Z保持初始值，X跟随飞船初始位置）
        // 在Setup状态下，相机不自动跟随飞船，但应该在一个合理的初始位置
        if (shipTransform != null)
        {
            float targetX = shipTransform.position.x + xOffset;
            transform.position = new Vector3(targetX, initialPosition.y, initialPosition.z);
        }
        else
        {
            // 如果没有飞船引用，重置到保存的初始位置
            transform.position = initialPosition;
        }

        if (showDebugLogs)
        {
            Debug.Log("CameraFollowShip: 相机已完全重置到Setup状态 - 可以摆放引力枢纽，可以手动移动相机（A/D键），可以缩放相机（鼠标滚轮）");
        }
    }

    /// <summary>
    /// 设置为总览全局的缩放值（用于初始状态和READY序列）
    /// </summary>
    private void SetOverviewZoom()
    {
        if (cameraComponent == null) return;

        float overviewZoomValue;
        if (isOrthographic)
        {
            overviewZoomValue = initialOrthographicSize * overviewZoomScale;
            overviewZoomValue = Mathf.Clamp(overviewZoomValue, minZoom, maxZoom);
            cameraComponent.orthographicSize = overviewZoomValue;
        }
        else
        {
            overviewZoomValue = initialFieldOfView * overviewZoomScale;
            overviewZoomValue = Mathf.Clamp(overviewZoomValue, minZoom, maxZoom);
            cameraComponent.fieldOfView = overviewZoomValue;
        }

        if (showDebugLogs)
        {
            Debug.Log($"CameraFollowShip: 已设置为总览全局缩放值: {overviewZoomValue:F2}");
        }
    }

    void LateUpdate()
    {
        // 重要：如果正在执行动画，不要干扰相机位置和缩放（让动画协程完全控制）
        if (isAnimating)
        {
            return;
        }

        // 状态检查：
        // 1. 如果已经点击过READY（hasReadyClicked = true）：禁用手动移动和缩放，开始跟随飞船
        // 2. 初始状态（hasReadyClicked = false）：允许手动移动和缩放，不跟随飞船（保持在当前位置）

        // 处理鼠标滚轮缩放（仅在READY前可用，且不在执行动画时）
        if (enableZoom && cameraComponent != null && !hasReadyClicked && !isAnimating)
        {
            HandleZoom();
        }

        // 处理手动移动（A/D键） - 仅在READY前可用
        float currentManualOffset = 0f;
        if (enableManualMove && !hasReadyClicked)
        {
            currentManualOffset = HandleManualMove();
        }

        // READY后才开始跟随飞船
        if (hasReadyClicked && shipTransform != null)
        {
            // 检查飞船是否处于Flying状态
            bool isFlying = IsShipFlying();
            
            // 计算目标位置
            // 如果飞船在Flying状态，需要同时跟随X和Y轴（时停放大会导致飞船在Y轴上移动）
            // 如果飞船不在Flying状态（PreLaunch等），只跟随X轴（保持Y轴不变）
            float targetX = shipTransform.position.x + xOffset;
            float targetY;
            
            if (isFlying)
            {
                // Flying状态：跟随Y轴，但受边界限制
                float shipYWithOffset = shipTransform.position.y + yOffset;
                
                if (enableYBoundary)
                {
                    // 应用Y轴边界限制
                    targetY = Mathf.Clamp(shipYWithOffset, minYBoundary, maxYBoundary);
                }
                else
                {
                    // 不限制边界，直接跟随
                    targetY = shipYWithOffset;
                }
            }
            else
            {
                // 非Flying状态：保持初始Y
                targetY = initialPosition.y;
            }
            
            float targetZ = initialPosition.z; // Z轴始终保持初始值

            // 获取当前位置
            Vector3 currentPos = transform.position;

            // 计算新位置
            Vector3 newPosition;

            if (useSmoothing && followSpeed > 0f)
            {
                // 平滑跟随：使用Lerp插值（使用未缩放时间，确保时停时也能平滑）
                float smoothedX = Mathf.Lerp(currentPos.x, targetX, followSpeed * Time.unscaledDeltaTime);
                float smoothedY = isFlying ? Mathf.Lerp(currentPos.y, targetY, followSpeed * Time.unscaledDeltaTime) : currentPos.y;
                newPosition = new Vector3(smoothedX, smoothedY, targetZ);
            }
            else
            {
                // 立即跟随
                newPosition = new Vector3(targetX, targetY, targetZ);
            }

            // 应用新位置
            transform.position = newPosition;

            if (showDebugLogs)
            {
                bool yClamped = isFlying && enableYBoundary && 
                               (shipTransform.position.y + yOffset < minYBoundary || 
                                shipTransform.position.y + yOffset > maxYBoundary);
                Debug.Log($"摄像机跟随：飞船位置=({shipTransform.position.x:F2}, {shipTransform.position.y:F2}), 目标位置=({targetX:F2}, {targetY:F2}), 当前位置=({transform.position.x:F2}, {transform.position.y:F2}), 飞船状态={GetShipStateName()}, 跟随Y轴={isFlying}, Y轴边界限制={enableYBoundary}, Y轴被限制={yClamped}");
            }
        }
        else if (!hasReadyClicked)
        {
            // Setup状态：不跟随飞船，但需要应用手动移动偏移
            // 计算目标X坐标（飞船初始位置 + 偏移量 + 手动移动偏移）
            float baseX;
            if (shipTransform != null)
            {
                baseX = shipTransform.position.x + xOffset;
            }
            else
            {
                baseX = initialPosition.x;
            }

            float targetX = baseX + currentManualOffset;

            // 获取当前位置
            Vector3 currentPos = transform.position;

            // 计算新位置（只改变X坐标，Y和Z保持当前位置）
            Vector3 newPosition;

            if (useSmoothing && followSpeed > 0f)
            {
                // 平滑移动：使用Lerp插值
                float smoothedX = Mathf.Lerp(currentPos.x, targetX, followSpeed * Time.unscaledDeltaTime);
                newPosition = new Vector3(smoothedX, currentPos.y, currentPos.z);
            }
            else
            {
                // 立即移动（保持Y和Z不变）
                newPosition = new Vector3(targetX, currentPos.y, currentPos.z);
            }

            // 应用新位置
            transform.position = newPosition;

            if (showDebugLogs)
            {
                Debug.Log($"摄像机Setup状态：基础X={baseX:F2}, 手动偏移={currentManualOffset:F2}, 目标X={targetX:F2}, 当前X={transform.position.x:F2}");
            }
        }
    }

    /// <summary>
    /// 检查飞船是否处于Flying状态
    /// </summary>
    /// <returns>如果飞船处于Flying状态返回true，否则返回false</returns>
    private bool IsShipFlying()
    {
        if (shipState == null)
        {
            return false; // 如果没有ShipState组件，默认允许操作
        }

        return shipState.CurrentState == ShipState.State.Flying;
    }

    /// <summary>
    /// 获取飞船状态名称（用于调试）
    /// </summary>
    private string GetShipStateName()
    {
        if (shipState == null)
        {
            return "Unknown";
        }
        return shipState.CurrentState.ToString();
    }

    /// <summary>
    /// 处理手动移动（A/D键左右移动）
    /// 返回手动移动的偏移量，用于叠加到跟随目标上
    /// 松开按键时相机停在当前位置，偏移量保持不变
    /// </summary>
    /// <returns>手动移动的累积偏移量</returns>
    private float HandleManualMove()
    {
        float moveInput = 0f;

        // 检测按键输入
        if (Input.GetKey(leftMoveKey))
        {
            moveInput = -1f; // 左移（负方向）
        }
        else if (Input.GetKey(rightMoveKey))
        {
            moveInput = 1f; // 右移（正方向）
        }

        // 如果有输入，累积偏移量（使用未缩放时间，确保时停时也能响应）
        if (Mathf.Abs(moveInput) > 0.01f)
        {
            float moveDistance = moveInput * manualMoveSpeed * Time.unscaledDeltaTime;
            manualMoveOffset += moveDistance; // 累积偏移量

            if (showDebugLogs)
            {
                Debug.Log($"手动移动：方向={moveInput}, 距离={moveDistance:F2}, 累积偏移={manualMoveOffset:F2}");
            }
        }
        // 如果没有输入，不改变manualMoveOffset，保持当前值（相机停在当前位置）

        return manualMoveOffset; // 返回累积的偏移量
    }

    /// <summary>
    /// 处理鼠标滚轮缩放
    /// </summary>
    private void HandleZoom()
    {
        // 获取鼠标滚轮输入（向上滚动返回正值，向下滚动返回负值）
        float scroll = Input.GetAxis("Mouse ScrollWheel");

        // 如果没有滚动输入，直接返回
        if (Mathf.Abs(scroll) < 0.01f)
        {
            return;
        }

        if (isOrthographic)
        {
            // 正交摄像机：控制 orthographicSize
            // 向上滚动（scroll > 0）：缩小 orthographicSize，放大画面（看到更少内容）
            // 向下滚动（scroll < 0）：增大 orthographicSize，缩小画面（看到更多内容）
            float newSize = cameraComponent.orthographicSize - scroll * zoomSpeed;
            newSize = Mathf.Clamp(newSize, minZoom, maxZoom);
            cameraComponent.orthographicSize = newSize;

            if (showDebugLogs)
            {
                Debug.Log($"缩放：orthographicSize = {newSize:F2} (滚轮: {scroll:F2})");
            }
        }
        else
        {
            // 透视摄像机：控制 fieldOfView
            // 向上滚动（scroll > 0）：减小 fieldOfView，放大画面（视野变窄）
            // 向下滚动（scroll < 0）：增大 fieldOfView，缩小画面（视野变广）
            float newFOV = cameraComponent.fieldOfView - scroll * zoomSpeed * 10f; // fieldOfView范围更大，需要更大的倍数
            newFOV = Mathf.Clamp(newFOV, minZoom, maxZoom);
            cameraComponent.fieldOfView = newFOV;

            if (showDebugLogs)
            {
                Debug.Log($"缩放：fieldOfView = {newFOV:F2} (滚轮: {scroll:F2})");
            }
        }
    }

    /// <summary>
    /// 自动查找飞船对象
    /// </summary>
    private void FindShip()
    {
        // 方法1：通过Tag查找
        GameObject shipByTag = GameObject.FindGameObjectWithTag("Spaceship");
        if (shipByTag != null)
        {
            shipTransform = shipByTag.transform;
            if (showDebugLogs)
            {
                Debug.Log($"CameraFollowShip: 通过Tag找到了飞船: {shipByTag.name}");
            }
            return;
        }

        // 方法2：通过ShipState组件查找
        ShipState shipState = FindObjectOfType<ShipState>();
        if (shipState != null)
        {
            shipTransform = shipState.transform;
            if (showDebugLogs)
            {
                Debug.Log($"CameraFollowShip: 通过ShipState组件找到了飞船: {shipState.name}");
            }
            return;
        }

        // 方法3：通过名称查找（备用方案）
        GameObject shipByName = GameObject.Find("Ship");
        if (shipByName != null)
        {
            shipTransform = shipByName.transform;
            if (showDebugLogs)
            {
                Debug.Log($"CameraFollowShip: 通过名称找到了飞船: {shipByName.name}");
            }
            return;
        }
    }

    /// <summary>
    /// 手动设置要跟随的飞船Transform
    /// </summary>
    /// <param name="ship">飞船的Transform</param>
    public void SetShipTransform(Transform ship)
    {
        shipTransform = ship;
        if (ship == null)
        {
            Debug.LogWarning("CameraFollowShip: 设置的飞船Transform为空");
        }
        else
        {
            Debug.Log($"CameraFollowShip: 已设置跟随目标: {ship.name}");
        }
    }

    /// <summary>
    /// 立即跳转到飞船位置（不使用平滑）
    /// </summary>
    public void SnapToShip()
    {
        if (shipTransform != null)
        {
            float targetX = shipTransform.position.x + xOffset;
            transform.position = new Vector3(targetX, initialPosition.y, initialPosition.z);
            if (showDebugLogs)
            {
                Debug.Log($"CameraFollowShip: 立即跳转到飞船位置，X={targetX:F2}");
            }
        }
    }

    /// <summary>
    /// 重置摄像机到初始位置
    /// </summary>
    public void ResetToInitialPosition()
    {
        transform.position = initialPosition;
        if (showDebugLogs)
        {
            Debug.Log($"CameraFollowShip: 已重置到初始位置");
        }
    }

    /// <summary>
    /// 开始READY按钮的镜头动画序列
    /// 先缩小总览全局，然后回到聚焦飞船
    /// 这个方法可以被UI按钮的OnClick事件调用
    /// </summary>
    public void StartReadySequence()
    {
        // 如果正在执行动画，先停止之前的动画
        if (readySequenceCoroutine != null)
        {
            StopCoroutine(readySequenceCoroutine);
        }

        readySequenceCoroutine = StartCoroutine(ReadySequenceCoroutine());
    }

    /// <summary>
    /// 计算总览范围（优先使用手动指定的对象，否则使用自动计算）
    /// </summary>
    /// <param name="overviewPosition">输出的总览相机位置</param>
    /// <param name="overviewZoom">输出的总览缩放值</param>
    /// <returns>是否成功计算</returns>
    private bool CalculateOverviewRange(out Vector3 overviewPosition, out float overviewZoom)
    {
        overviewPosition = transform.position;
        overviewZoom = isOrthographic ? initialOrthographicSize * overviewZoomScale : initialFieldOfView * overviewZoomScale;

        // 优先级1：使用手动指定的总览范围对象
        if (overviewRangeMode == OverviewRangeMode.ManualObject && overviewRangeObject != null)
        {
            return CalculateRangeFromObject(overviewRangeObject, out overviewPosition, out overviewZoom);
        }

        // 优先级2：自动计算Station和Destination
        if (overviewRangeMode == OverviewRangeMode.AutoCalculate)
        {
            return CalculateRangeFromStationAndDestination(out overviewPosition, out overviewZoom);
        }

        // 优先级3：使用固定缩放倍数
        if (overviewRangeMode == OverviewRangeMode.FixedScale)
        {
            overviewPosition = transform.position;
            overviewZoom = isOrthographic ? initialOrthographicSize * overviewZoomScale : initialFieldOfView * overviewZoomScale;
            return true;
        }

        return false;
    }

    /// <summary>
    /// 从指定的GameObject或Collider计算总览范围
    /// </summary>
    private bool CalculateRangeFromObject(GameObject rangeObject, out Vector3 overviewPosition, out float overviewZoom)
    {
        overviewPosition = transform.position;
        overviewZoom = isOrthographic ? initialOrthographicSize * overviewZoomScale : initialFieldOfView * overviewZoomScale;

        if (rangeObject == null)
        {
            return false;
        }

        Bounds bounds;
        Collider col = rangeObject.GetComponent<Collider>();
        if (col != null)
        {
            // 使用Collider的bounds
            bounds = col.bounds;
        }
        else
        {
            // 尝试从子对象获取Collider
            col = rangeObject.GetComponentInChildren<Collider>();
            if (col != null)
            {
                bounds = col.bounds;
            }
            else
            {
                // 如果没有Collider，使用Transform位置作为中心点，创建一个默认范围
                Vector3 pos = rangeObject.transform.position;
                bounds = new Bounds(pos, Vector3.one * 10f); // 默认10单位范围
                
                if (showDebugLogs)
                {
                    Debug.LogWarning($"CameraFollowShip: 总览范围对象 {rangeObject.name} 没有Collider，使用默认范围");
                }
            }
        }

        // 计算范围（只考虑XY平面）
        float width = bounds.size.x + overviewPadding * 2f;
        float height = bounds.size.y + overviewPadding * 2f;
        Vector3 center = bounds.center;
        // 总览时：相机移动到范围中心（包括X和Y），以便完整显示范围
        overviewPosition = new Vector3(center.x, center.y, initialPosition.z);

        if (showDebugLogs)
        {
            Debug.Log($"CameraFollowShip: 从对象计算总览范围 - 对象: {rangeObject.name}");
            Debug.Log($"CameraFollowShip: Collider Bounds - 中心: {bounds.center}, 大小: {bounds.size}");
            Debug.Log($"CameraFollowShip: 计算后的范围 - 宽度: {width:F2}, 高度: {height:F2}, 中心: {overviewPosition}");
            Debug.Log($"CameraFollowShip: 总览位置允许Y轴移动: {overviewPosition.y} (初始Y: {initialPosition.y})");
        }

        // 计算缩放值
        return CalculateZoomFromSize(width, height, overviewPosition, out overviewZoom);
    }

    /// <summary>
    /// 根据宽度和高度计算缩放值（通用方法）
    /// </summary>
    private bool CalculateZoomFromSize(float width, float height, Vector3 centerPosition, out float overviewZoom)
    {
        overviewZoom = isOrthographic ? initialOrthographicSize * overviewZoomScale : initialFieldOfView * overviewZoomScale;

        // 使用相机的实际宽高比（更准确）
        float cameraAspect = cameraComponent.aspect;
        
        if (isOrthographic)
        {
            // 正交相机：orthographicSize是视口高度的一半
            // 视口高度 = orthographicSize * 2
            // 视口宽度 = orthographicSize * 2 * aspect
            // 需要确保height和width都能被覆盖
            float requiredSizeForHeight = height * 0.5f; // 高度需要：height <= orthographicSize * 2
            float requiredSizeForWidth = (width * 0.5f) / cameraAspect; // 宽度需要：width <= orthographicSize * 2 * aspect
            
            // 取较大的值，确保两个方向都能覆盖
            overviewZoom = Mathf.Max(requiredSizeForHeight, requiredSizeForWidth);
            
            // 限制在有效范围内
            overviewZoom = Mathf.Clamp(overviewZoom, minZoom, maxZoom);
            
        if (showDebugLogs)
        {
            Debug.Log($"CameraFollowShip: 正交相机计算 - 相机宽高比={cameraAspect:F3}, 宽度={width:F2}, 高度={height:F2}");
            Debug.Log($"CameraFollowShip: 高度需求={requiredSizeForHeight:F2}, 宽度需求={requiredSizeForWidth:F2}, 最终缩放={overviewZoom:F2}");
            Debug.Log($"CameraFollowShip: 当前 orthographicSize={cameraComponent.orthographicSize:F2}, 初始值={initialOrthographicSize:F2}");
            Debug.Log($"CameraFollowShip: 缩放限制范围: minZoom={minZoom:F2}, maxZoom={maxZoom:F2}");
            
            // 验证计算：orthographicSize * 2 应该 >= height，orthographicSize * 2 * aspect 应该 >= width
            float actualHeight = overviewZoom * 2f;
            float actualWidth = actualHeight * cameraAspect;
            Debug.Log($"CameraFollowShip: 验证 - 实际显示高度={actualHeight:F2} (需要>={height:F2}), 实际显示宽度={actualWidth:F2} (需要>={width:F2})");
        }
        }
        else
        {
            // 透视相机：需要根据距离和视野角度计算
            // 使用相机的Z位置作为距离（相机通常朝向Z轴负方向）
            float distance = Mathf.Abs(transform.position.z - centerPosition.y);
            if (distance < 0.01f) 
            {
                // 如果距离太小，使用相机到中心点的实际距离
                distance = Vector3.Distance(transform.position, centerPosition);
                if (distance < 0.01f) distance = 10f; // 最后的默认值
            }
            
            // 使用三角函数计算所需的fieldOfView
            // tan(fov/2) = (height/2) / distance
            float halfHeight = height * 0.5f;
            float halfFOV = Mathf.Atan2(halfHeight, distance) * Mathf.Rad2Deg;
            float requiredFOV = halfFOV * 2f;
            
            // 考虑相机宽高比，确保宽度也能覆盖
            // 对于透视相机，水平FOV = 2 * atan(tan(垂直FOV/2) * aspect)
            float halfWidth = width * 0.5f;
            float halfFOVWidth = Mathf.Atan2(halfWidth / cameraAspect, distance) * Mathf.Rad2Deg;
            float requiredFOVWidth = halfFOVWidth * 2f;
            
            overviewZoom = Mathf.Max(requiredFOV, requiredFOVWidth);
            
            // 限制在有效范围内
            overviewZoom = Mathf.Clamp(overviewZoom, minZoom, maxZoom);
            
            if (showDebugLogs)
            {
                Debug.Log($"CameraFollowShip: 透视相机计算 - 相机宽高比={cameraAspect:F3}, 距离={distance:F2}, 宽度={width:F2}, 高度={height:F2}, 高度FOV需求={requiredFOV:F2}, 宽度FOV需求={requiredFOVWidth:F2}, 最终FOV={overviewZoom:F2}");
            }
        }

        if (showDebugLogs)
        {
            Debug.Log($"CameraFollowShip: 总览范围 - 宽度={width:F2}, 高度={height:F2}, 中心位置={centerPosition}, 缩放值={overviewZoom:F2}");
        }

        return true;
    }

    /// <summary>
    /// 从Station和Destination自动计算总览范围
    /// </summary>
    private bool CalculateRangeFromStationAndDestination(out Vector3 overviewPosition, out float overviewZoom)
    {
        overviewPosition = transform.position;
        overviewZoom = isOrthographic ? initialOrthographicSize * overviewZoomScale : initialFieldOfView * overviewZoomScale;

        // 查找Station和Destination
        GameObject station = GameObject.FindGameObjectWithTag(stationTag);
        GameObject destination = GameObject.FindGameObjectWithTag(destinationTag);

        if (station == null || destination == null)
        {
            if (showDebugLogs)
            {
                if (station == null)
                    Debug.LogWarning($"CameraFollowShip: 未找到Tag为 '{stationTag}' 的Station对象，将使用默认总览缩放");
                if (destination == null)
                    Debug.LogWarning($"CameraFollowShip: 未找到Tag为 '{destinationTag}' 的Destination对象，将使用默认总览缩放");
            }
            return false;
        }

        Vector3 stationPos = station.transform.position;
        Vector3 destPos = destination.transform.position;

        // 计算两个点的边界框
        float minX = Mathf.Min(stationPos.x, destPos.x);
        float maxX = Mathf.Max(stationPos.x, destPos.x);
        float minY = Mathf.Min(stationPos.y, destPos.y);
        float maxY = Mathf.Max(stationPos.y, destPos.y);

        // 添加边距
        float width = (maxX - minX) + overviewPadding * 2f;
        float height = (maxY - minY) + overviewPadding * 2f;

        // 计算中心位置
        // 总览时：相机移动到Station和Destination的中心（包括X和Y），以便完整显示范围
        float centerX = (minX + maxX) * 0.5f;
        float centerY = (minY + maxY) * 0.5f;
        overviewPosition = new Vector3(centerX, centerY, initialPosition.z);
        
        if (showDebugLogs)
        {
            Debug.Log($"CameraFollowShip: 自动计算总览位置 - Station: {stationPos}, Destination: {destPos}");
            Debug.Log($"CameraFollowShip: 总览中心位置: {overviewPosition} (允许Y轴移动以完整显示范围)");
        }

        // 根据相机类型计算所需的缩放值
        // 使用相机的实际宽高比（更准确）
        float cameraAspect = cameraComponent.aspect;
        
        if (isOrthographic)
        {
            // 正交相机：orthographicSize是视口高度的一半
            // 视口高度 = orthographicSize * 2
            // 视口宽度 = orthographicSize * 2 * aspect
            // 需要确保height和width都能被覆盖
            float requiredSizeForHeight = height * 0.5f; // 高度需要：height <= orthographicSize * 2
            float requiredSizeForWidth = (width * 0.5f) / cameraAspect; // 宽度需要：width <= orthographicSize * 2 * aspect
            
            // 取较大的值，确保两个方向都能覆盖
            overviewZoom = Mathf.Max(requiredSizeForHeight, requiredSizeForWidth);
            
            // 限制在有效范围内
            overviewZoom = Mathf.Clamp(overviewZoom, minZoom, maxZoom);
            
            if (showDebugLogs)
            {
                Debug.Log($"CameraFollowShip: 正交相机计算 - 相机宽高比={cameraAspect:F3}, 高度需求={requiredSizeForHeight:F2}, 宽度需求={requiredSizeForWidth:F2}, 最终缩放={overviewZoom:F2}");
            }
        }
        else
        {
            // 透视相机：需要根据距离和视野角度计算
            // 使用相机的Z位置作为距离（相机通常朝向Z轴负方向）
            float distance = Mathf.Abs(transform.position.z - centerY);
            if (distance < 0.01f) 
            {
                // 如果距离太小，使用相机到中心点的实际距离
                distance = Vector3.Distance(transform.position, overviewPosition);
                if (distance < 0.01f) distance = 10f; // 最后的默认值
            }
            
            // 使用三角函数计算所需的fieldOfView
            // tan(fov/2) = (height/2) / distance
            float halfHeight = height * 0.5f;
            float halfFOV = Mathf.Atan2(halfHeight, distance) * Mathf.Rad2Deg;
            float requiredFOV = halfFOV * 2f;
            
            // 考虑相机宽高比，确保宽度也能覆盖
            // 对于透视相机，水平FOV = 2 * atan(tan(垂直FOV/2) * aspect)
            float halfWidth = width * 0.5f;
            float halfFOVWidth = Mathf.Atan2(halfWidth / cameraAspect, distance) * Mathf.Rad2Deg;
            float requiredFOVWidth = halfFOVWidth * 2f;
            
            overviewZoom = Mathf.Max(requiredFOV, requiredFOVWidth);
            
            // 限制在有效范围内
            overviewZoom = Mathf.Clamp(overviewZoom, minZoom, maxZoom);
            
            if (showDebugLogs)
            {
                Debug.Log($"CameraFollowShip: 透视相机计算 - 相机宽高比={cameraAspect:F3}, 距离={distance:F2}, 高度FOV需求={requiredFOV:F2}, 宽度FOV需求={requiredFOVWidth:F2}, 最终FOV={overviewZoom:F2}");
            }
        }

        if (showDebugLogs)
        {
            Debug.Log($"CameraFollowShip: 计算总览范围 - Station位置={stationPos}, Destination位置={destPos}");
            Debug.Log($"CameraFollowShip: 总览范围 - 宽度={width:F2}, 高度={height:F2}, 中心位置={overviewPosition}, 缩放值={overviewZoom:F2}");
        }

        return true;
    }

    /// <summary>
    /// READY序列协程：缩小总览 -> 等待 -> 回到聚焦
    /// </summary>
    private IEnumerator ReadySequenceCoroutine()
    {
        isAnimating = true;

        if (cameraComponent == null)
        {
            Debug.LogError("CameraFollowShip: 摄像机组件为空，无法执行READY序列");
            isAnimating = false;
            yield break;
        }

        // 保存当前缩放值和位置
        float startZoomValue;
        float overviewZoomValue;
        float targetZoomValue;
        Vector3 startCameraPosition = transform.position;
        Vector3 overviewCameraPosition;
        float elapsed = 0f; // 声明elapsed变量，在多个步骤中复用

        // 计算总览范围
        bool success = CalculateOverviewRange(out overviewCameraPosition, out overviewZoomValue);
        if (!success)
        {
            // 如果计算失败，使用默认值
            overviewCameraPosition = startCameraPosition;
            if (isOrthographic)
            {
                overviewZoomValue = initialOrthographicSize * overviewZoomScale;
            }
            else
            {
                overviewZoomValue = initialFieldOfView * overviewZoomScale;
            }
            
            if (showDebugLogs)
            {
                Debug.LogWarning("CameraFollowShip: 总览范围计算失败，使用默认值");
            }
        }

        // 获取当前缩放值
        if (isOrthographic)
        {
            startZoomValue = cameraComponent.orthographicSize;
            targetZoomValue = initialOrthographicSize;
        }
        else
        {
            startZoomValue = cameraComponent.fieldOfView;
            targetZoomValue = initialFieldOfView;
        }

        // 确保overviewZoomValue在有效范围内
        if (isOrthographic)
        {
            overviewZoomValue = Mathf.Clamp(overviewZoomValue, minZoom, maxZoom);
        }
        else
        {
            overviewZoomValue = Mathf.Clamp(overviewZoomValue, minZoom, maxZoom);
        }

        if (showDebugLogs)
        {
            Debug.Log($"CameraFollowShip: 开始READY序列 - 当前缩放={startZoomValue:F2}, 总览缩放={overviewZoomValue:F2}, 目标缩放={targetZoomValue:F2}");
            Debug.Log($"CameraFollowShip: 当前位置={startCameraPosition}, 总览位置={overviewCameraPosition}");
        }

        // 第一步：缩小到总览全局（同时移动相机到总览位置）
        // 如果当前已经在总览状态（或非常接近），跳过这一步，立即响应
        float zoomDifference = Mathf.Abs(startZoomValue - overviewZoomValue);
        float positionDifference = Vector3.Distance(startCameraPosition, overviewCameraPosition);
        float zoomTolerance = overviewZoomValue * 0.05f; // 5%的容差，如果差异小于5%就认为已经在总览状态
        float positionTolerance = 0.5f; // 位置容差

        if (zoomDifference > zoomTolerance || positionDifference > positionTolerance)
        {
            // 需要动画：从当前缩放值和位置过渡到总览值
            // 使用平滑插值，避免抖动
            elapsed = 0f; // 重置elapsed，从0开始
            
            // 使用平滑插值的速度变量（用于减少抖动）
            float currentZoom = startZoomValue;
            Vector3 currentPosition = startCameraPosition;
            
            while (elapsed < zoomOutDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / zoomOutDuration); // 确保t在0-1范围内
                float curveValue = zoomCurve.Evaluate(t);

                // 使用Lerp进行平滑插值
                currentZoom = Mathf.Lerp(startZoomValue, overviewZoomValue, curveValue);
                currentPosition = Vector3.Lerp(startCameraPosition, overviewCameraPosition, curveValue);

                // 在同一帧内同时更新，避免不同步导致的抖动
                if (isOrthographic)
                {
                    cameraComponent.orthographicSize = currentZoom;
                }
                else
                {
                    cameraComponent.fieldOfView = currentZoom;
                }
                transform.position = currentPosition;

                yield return null;
            }
            
            // 确保最终值精确
            if (isOrthographic)
            {
                cameraComponent.orthographicSize = overviewZoomValue;
            }
            else
            {
                cameraComponent.fieldOfView = overviewZoomValue;
            }
            transform.position = overviewCameraPosition;
        }

        // 确保到达总览缩放值和位置（无论是否执行了动画）
        if (isOrthographic)
        {
            cameraComponent.orthographicSize = overviewZoomValue;
        }
        else
        {
            cameraComponent.fieldOfView = overviewZoomValue;
        }
        transform.position = overviewCameraPosition;

        // 第二步：总览停留
        // 如果已经在总览状态（跳过了第一步动画），缩短停留时间，让用户更快看到响应
        float actualHoldDuration = zoomDifference <= zoomTolerance ? overviewHoldDuration * 0.3f : overviewHoldDuration;
        if (actualHoldDuration > 0.01f) // 只有停留时间大于0.01秒才等待
        {
            yield return new WaitForSecondsRealtime(actualHoldDuration);
        }

        // 第三步：回到聚焦飞船（画面放大的同时，相机逐渐移动到飞船位置）
        // 记录开始聚焦时的相机位置（此时应该是总览位置，可能包含Y轴偏移）
        Vector3 focusStartPosition = transform.position;
        
        // 计算目标位置（飞船位置 + 偏移量）
        // 注意：聚焦时相机回到只跟随X轴的模式，Y和Z回到初始值
        Vector3 targetCameraPosition;
        if (shipTransform != null)
        {
            float targetX = shipTransform.position.x + xOffset;
            targetCameraPosition = new Vector3(targetX, initialPosition.y, initialPosition.z);
        }
        else
        {
            // 如果没有飞船，回到初始位置（包括Y和Z）
            targetCameraPosition = new Vector3(focusStartPosition.x, initialPosition.y, initialPosition.z);
        }
        
        if (showDebugLogs)
        {
            Debug.Log($"CameraFollowShip: 聚焦动画 - 从总览位置 {focusStartPosition} 到目标位置 {targetCameraPosition}");
        }

        elapsed = 0f; // 重置elapsed变量用于第三步动画
        float focusZoom = overviewZoomValue;
        Vector3 focusPosition = focusStartPosition;
        
        // 使用固定时间步长来减少抖动
        float fixedDeltaTime = 0.016f; // 约60fps
        int frameCount = Mathf.CeilToInt(zoomInDuration / fixedDeltaTime);
        
        if (showDebugLogs)
        {
            Debug.Log($"CameraFollowShip: 开始聚焦动画 - 从缩放 {overviewZoomValue:F2} 到 {targetZoomValue:F2}, 从位置 {focusStartPosition} 到 {targetCameraPosition}, 帧数: {frameCount}");
        }
        
        for (int i = 0; i <= frameCount; i++)
        {
            float t = (float)i / frameCount;
            t = Mathf.Clamp01(t); // 确保t在0-1范围内
            float curveValue = zoomCurve.Evaluate(t);

            // 同时进行缩放和位置插值，使用相同的曲线值确保同步
            focusZoom = Mathf.Lerp(overviewZoomValue, targetZoomValue, curveValue);
            focusPosition = Vector3.Lerp(focusStartPosition, targetCameraPosition, curveValue);

            // 在同一帧内同时更新，避免不同步导致的抖动
            if (isOrthographic)
            {
                cameraComponent.orthographicSize = focusZoom;
            }
            else
            {
                cameraComponent.fieldOfView = focusZoom;
            }
            transform.position = focusPosition;

            // 使用固定时间步长等待
            yield return new WaitForSecondsRealtime(fixedDeltaTime);
        }
        
        // 确保最终值精确
        if (isOrthographic)
        {
            cameraComponent.orthographicSize = targetZoomValue;
        }
        else
        {
            cameraComponent.fieldOfView = targetZoomValue;
        }
        transform.position = targetCameraPosition;

        // 确保到达目标缩放值和位置
        if (isOrthographic)
        {
            cameraComponent.orthographicSize = targetZoomValue;
        }
        else
        {
            cameraComponent.fieldOfView = targetZoomValue;
        }
        transform.position = targetCameraPosition;

        // READY序列完成：
        // 1. 飞船状态从Setup转换到PreLaunch（检查阶段完成，进入准备发射阶段）
        if (shipState != null)
        {
            shipState.ReadyToPreLaunch();
        }
        
        // 2. 清除手动移动偏移
        manualMoveOffset = 0f;
        
        // 3. 设置READY标志，禁用手动移动和缩放，开始自动跟随飞船
        hasReadyClicked = true;
        
        // 4. 确保立即聚焦到飞船位置（如果之前位置有偏差）
        SnapToShip();

        if (showDebugLogs)
        {
            Debug.Log("CameraFollowShip: READY序列完成，飞船已从Setup转换到PreLaunch状态，相机已聚焦飞船，手动移动和缩放已禁用，开始自动跟随");
        }

        isAnimating = false;
        readySequenceCoroutine = null;
    }
}

