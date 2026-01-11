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
    [Tooltip("总览全局时的缩放倍数（相对于初始大小，大于1表示缩小看全局）")]
    [SerializeField] private float overviewZoomScale = 3f;

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
            // 计算目标X坐标（飞船X坐标 + 偏移量）
            // READY后不再使用手动移动偏移，只跟随飞船
            float targetX = shipTransform.position.x + xOffset;

            // 获取当前位置
            Vector3 currentPos = transform.position;

            // 计算新位置（只改变X坐标）
            Vector3 newPosition;

            if (useSmoothing && followSpeed > 0f)
            {
                // 平滑跟随：使用Lerp插值（使用未缩放时间，确保时停时也能平滑）
                float smoothedX = Mathf.Lerp(currentPos.x, targetX, followSpeed * Time.unscaledDeltaTime);
                newPosition = new Vector3(smoothedX, initialPosition.y, initialPosition.z);
            }
            else
            {
                // 立即跟随
                newPosition = new Vector3(targetX, initialPosition.y, initialPosition.z);
            }

            // 应用新位置
            transform.position = newPosition;

            if (showDebugLogs)
            {
                Debug.Log($"摄像机跟随：飞船X={shipTransform.position.x:F2}, 目标X={targetX:F2}, 当前X={transform.position.x:F2}, READY状态={hasReadyClicked}");
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

            // 计算新位置（只改变X坐标）
            Vector3 newPosition;

            if (useSmoothing && followSpeed > 0f)
            {
                // 平滑移动：使用Lerp插值
                float smoothedX = Mathf.Lerp(currentPos.x, targetX, followSpeed * Time.unscaledDeltaTime);
                newPosition = new Vector3(smoothedX, initialPosition.y, initialPosition.z);
            }
            else
            {
                // 立即移动
                newPosition = new Vector3(targetX, initialPosition.y, initialPosition.z);
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

        // 保存当前缩放值
        float startZoomValue;
        float overviewZoomValue;
        float targetZoomValue;
        float elapsed = 0f; // 声明elapsed变量，在多个步骤中复用

        if (isOrthographic)
        {
            startZoomValue = cameraComponent.orthographicSize;
            overviewZoomValue = initialOrthographicSize * overviewZoomScale;
            targetZoomValue = initialOrthographicSize;
        }
        else
        {
            startZoomValue = cameraComponent.fieldOfView;
            overviewZoomValue = initialFieldOfView * overviewZoomScale;
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
        }

        // 第一步：缩小到总览全局
        // 如果当前已经在总览状态（或非常接近），跳过这一步，立即响应
        float zoomDifference = Mathf.Abs(startZoomValue - overviewZoomValue);
        float zoomTolerance = overviewZoomValue * 0.05f; // 5%的容差，如果差异小于5%就认为已经在总览状态

        if (zoomDifference > zoomTolerance)
        {
            // 需要动画：从当前缩放值缩放到总览值
            // 立即设置第一帧的值，让用户立即看到响应（而不是等到下一帧）
            float firstFrameZoom = Mathf.Lerp(startZoomValue, overviewZoomValue, zoomCurve.Evaluate(0f));
            if (isOrthographic)
            {
                cameraComponent.orthographicSize = firstFrameZoom;
            }
            else
            {
                cameraComponent.fieldOfView = firstFrameZoom;
            }

            elapsed = Time.unscaledDeltaTime; // 从第一帧的时间开始，而不是0
            while (elapsed < zoomOutDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = elapsed / zoomOutDuration;
                float curveValue = zoomCurve.Evaluate(t);

                float currentZoom = Mathf.Lerp(startZoomValue, overviewZoomValue, curveValue);

                if (isOrthographic)
                {
                    cameraComponent.orthographicSize = currentZoom;
                }
                else
                {
                    cameraComponent.fieldOfView = currentZoom;
                }

                yield return null;
            }
        }

        // 确保到达总览缩放值（无论是否执行了动画）
        if (isOrthographic)
        {
            cameraComponent.orthographicSize = overviewZoomValue;
        }
        else
        {
            cameraComponent.fieldOfView = overviewZoomValue;
        }

        // 第二步：总览停留
        // 如果已经在总览状态（跳过了第一步动画），缩短停留时间，让用户更快看到响应
        float actualHoldDuration = zoomDifference <= zoomTolerance ? overviewHoldDuration * 0.3f : overviewHoldDuration;
        if (actualHoldDuration > 0.01f) // 只有停留时间大于0.01秒才等待
        {
            yield return new WaitForSecondsRealtime(actualHoldDuration);
        }

        // 第三步：回到聚焦飞船（画面放大的同时，相机逐渐移动到飞船位置）
        // 记录开始聚焦时的相机位置
        Vector3 startCameraPosition = transform.position;
        
        // 计算目标位置（飞船位置 + 偏移量）
        Vector3 targetCameraPosition;
        if (shipTransform != null)
        {
            float targetX = shipTransform.position.x + xOffset;
            targetCameraPosition = new Vector3(targetX, initialPosition.y, initialPosition.z);
        }
        else
        {
            targetCameraPosition = startCameraPosition; // 如果没有飞船，保持当前位置
        }

        elapsed = 0f; // 重置elapsed变量用于第三步动画
        while (elapsed < zoomInDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = elapsed / zoomInDuration;
            float curveValue = zoomCurve.Evaluate(t);

            // 同时进行缩放和位置插值
            float currentZoom = Mathf.Lerp(overviewZoomValue, targetZoomValue, curveValue);
            Vector3 currentPosition = Vector3.Lerp(startCameraPosition, targetCameraPosition, curveValue);

            // 应用缩放
            if (isOrthographic)
            {
                cameraComponent.orthographicSize = currentZoom;
            }
            else
            {
                cameraComponent.fieldOfView = currentZoom;
            }

            // 应用位置
            transform.position = currentPosition;

            yield return null;
        }

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

