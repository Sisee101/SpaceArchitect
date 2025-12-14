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

    [Header("调试")]
    [Tooltip("显示调试信息")]
    [SerializeField] private bool showDebugLogs = false;

    private Vector3 initialPosition; // 保存初始位置（用于保持Y和Z不变）
    private Camera cameraComponent; // 摄像机组件引用
    private float initialOrthographicSize; // 初始正交大小（用于正交摄像机）
    private float initialFieldOfView; // 初始视野角度（用于透视摄像机）
    private bool isOrthographic; // 是否为正交摄像机
    private float manualMoveOffset = 0f; // 手动移动的累积偏移量
    private ShipState shipState; // 飞船状态组件引用（用于检查飞船状态）

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
                Debug.LogWarning("CameraFollowShip: 飞船没有ShipState组件，将无法检测飞船状态，缩放和移动功能将始终可用。");
            }
        }
    }

    void LateUpdate()
    {
        // 检查飞船是否处于Flying状态（如果处于Flying状态，禁用缩放和移动）
        bool isShipFlying = IsShipFlying();

        // 处理鼠标滚轮缩放（仅在飞船非Flying状态时可用）
        if (enableZoom && cameraComponent != null && !isShipFlying)
        {
            HandleZoom();
        }

        // 处理手动移动（A/D键） - 仅在飞船非Flying状态时可用
        float currentManualOffset = 0f;
        if (enableManualMove && !isShipFlying)
        {
            currentManualOffset = HandleManualMove();
        }
        else if (isShipFlying)
        {
            // 飞船在Flying状态时，清除手动移动偏移（平滑回到跟随位置）
            if (Mathf.Abs(manualMoveOffset) > 0.01f)
            {
                manualMoveOffset = Mathf.Lerp(manualMoveOffset, 0f, followSpeed * Time.deltaTime);
            }
            else
            {
                manualMoveOffset = 0f;
            }
            currentManualOffset = manualMoveOffset;
        }

        // 如果飞船引用为空，跳过跟随
        if (shipTransform == null)
        {
            return;
        }

        // 计算目标X坐标（飞船X坐标 + 偏移量 + 手动移动偏移）
        float targetX = shipTransform.position.x + xOffset + currentManualOffset;

        // 获取当前位置
        Vector3 currentPos = transform.position;

        // 计算新位置（只改变X坐标）
        Vector3 newPosition;

        if (useSmoothing && followSpeed > 0f)
        {
            // 平滑跟随：使用Lerp插值
            float smoothedX = Mathf.Lerp(currentPos.x, targetX, followSpeed * Time.deltaTime);
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
            Debug.Log($"摄像机跟随：飞船X={shipTransform.position.x:F2}, 目标X={targetX:F2}, 当前X={transform.position.x:F2}, 手动偏移={currentManualOffset:F2}, 飞船状态={GetShipStateName()}");
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

        // 如果有输入，累积偏移量
        if (Mathf.Abs(moveInput) > 0.01f)
        {
            float moveDistance = moveInput * manualMoveSpeed * Time.deltaTime;
            manualMoveOffset += moveDistance; // 累积偏移量

            if (showDebugLogs)
            {
                Debug.Log($"手动移动：方向={moveInput}, 距离={moveDistance:F2}, 累积偏移={manualMoveOffset:F2}");
            }
        }
        else
        {
            // 没有按键输入时，逐渐衰减偏移量（平滑回到跟随位置）
            if (Mathf.Abs(manualMoveOffset) > 0.01f)
            {
                manualMoveOffset = Mathf.Lerp(manualMoveOffset, 0f, followSpeed * Time.deltaTime);
            }
            else
            {
                manualMoveOffset = 0f; // 完全归零
            }
        }

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
}

