using UnityEngine;
using UnityEngine.EventSystems; // 添加用于UI检测
using UnityEngine.UI; // 添加用于GraphicRaycaster
using UnityEngine.SceneManagement; // 添加用于场景检查
using System.Collections.Generic; // 引用 List

/// <summary>
/// 相机控制器
/// 支持鼠标左键拖动旋转、鼠标中键平移、滚轮缩放、WASD移动
/// 新增：支持分别锁定X轴和Y轴旋转
/// </summary>
public class CameraController : MonoBehaviour
{
    [Header("相机引用")]
    [SerializeField] private Camera targetCamera; // 目标相机（如果为空，使用当前GameObject上的Camera）

    [Header("鼠标控制")]
    [SerializeField] private bool enableMouseRotation = true; // 启用鼠标旋转
    [SerializeField] private float mouseSensitivity = 2f; // 鼠标灵敏度
    [SerializeField] private KeyCode mouseRotateKey = KeyCode.Mouse0; // 鼠标旋转按键（左键）

    [Header("旋转轴锁定 (新增功能)")]
    [SerializeField] private bool lockRotationX = false; // 锁定X轴（禁止上下旋转）
    [SerializeField] private bool lockRotationY = false; // 锁定Y轴（禁止左右旋转）

    [Header("鼠标中键平移")]
    [SerializeField] private bool enableMousePan = true; // 启用鼠标中键平移
    [SerializeField] private float panSensitivity = 0.1f; // 平移灵敏度（根据相机距离调整，参考Unity场景视图）

    [Header("滚轮控制")]
    [SerializeField] private bool enableMouseWheelZoom = true; // 启用滚轮缩放
    [SerializeField] private float zoomSpeed = 5f; // 缩放速度
    [SerializeField] private float minZoomDistance = 2f; // 最小缩放距离（透视相机）或最小Orthographic Size（正交相机）
    [SerializeField] private float maxZoomDistance = 50f; // 最大缩放距离（透视相机）或最大Orthographic Size（正交相机）

    [Header("WASD移动")]
    [SerializeField] private bool enableWASDMovement = true; // 启用WASD移动
    [SerializeField] private float moveSpeed = 5f; // 移动速度
    [SerializeField] private float fastMoveMultiplier = 2f; // 按住Shift时的快速移动倍数

    [Header("旋转角度限制")]
    [SerializeField] private bool limitVerticalRotation = true; // 限制垂直旋转范围
    [SerializeField] private float minVerticalAngle = -80f; // 最小垂直角度（向上）
    [SerializeField] private float maxVerticalAngle = 80f; // 最大垂直角度（向下）

    [Header("轨道相机模式（围绕目标点旋转）")]
    [SerializeField] private bool enableOrbitMode = false; // 启用轨道模式（围绕目标点旋转）
    [SerializeField] private Transform orbitTarget; // 轨道目标点（如果为空，使用世界原点）
    [SerializeField] private Vector3 orbitTargetPosition = Vector3.zero; // 轨道目标点位置（当orbitTarget为空时使用）
    [SerializeField] private float orbitRadius = 10f; // 轨道半径（相机到目标点的距离）
    [SerializeField] private bool autoCalculateRadius = true; // 自动计算初始半径（基于当前相机位置）

    // 私有变量
    private float rotationX = 0f; // 垂直旋转角度
    private float rotationY = 0f; // 水平旋转角度
    private float currentOrbitRadius = 10f; // 当前轨道半径（用于滚轮缩放）
    private Vector3 lastMousePosition; // 上一帧鼠标位置（用于旋转）
    private bool isRotating = false; // 是否正在旋转
    private Vector3 lastPanMousePosition; // 上一帧鼠标位置（用于平移）
    private bool isPanning = false; // 是否正在平移

    void Start()
    {
        // 如果没有指定相机，使用当前GameObject上的Camera组件
        if (targetCamera == null)
        {
            targetCamera = GetComponent<Camera>();
            if (targetCamera == null)
            {
                Debug.LogError("CameraController: 未找到Camera组件！请在Inspector中指定targetCamera或确保此脚本挂载在有Camera组件的GameObject上。");
            }
        }

        // 初始化旋转角度（使用当前相机的旋转）
        if (targetCamera != null)
        {
            rotationY = transform.eulerAngles.y;
            rotationX = transform.eulerAngles.x;

            // 规范化角度
            if (rotationX > 180f)
            {
                rotationX -= 360f;
            }

            // 初始化轨道模式
            if (enableOrbitMode)
            {
                // 获取目标点位置
                Vector3 targetPos = GetOrbitTargetPosition();

                // 如果启用自动计算半径，根据当前相机位置计算
                if (autoCalculateRadius)
                {
                    currentOrbitRadius = Vector3.Distance(transform.position, targetPos);
                    // 关键修复：确保计算出的半径在滚轮缩放的范围内
                    currentOrbitRadius = Mathf.Clamp(currentOrbitRadius, minZoomDistance, maxZoomDistance);
                    orbitRadius = currentOrbitRadius;
                }
                else
                {
                    // 关键修复：确保手动设置的orbitRadius在滚轮缩放的范围内
                    orbitRadius = Mathf.Clamp(orbitRadius, minZoomDistance, maxZoomDistance);
                    currentOrbitRadius = orbitRadius;
                }

                // 根据当前相机位置计算旋转角度
                Vector3 directionToCamera = (transform.position - targetPos).normalized;
                rotationY = Mathf.Atan2(directionToCamera.x, directionToCamera.z) * Mathf.Rad2Deg;
                rotationX = -Mathf.Asin(directionToCamera.y) * Mathf.Rad2Deg;

                // 更新相机位置和朝向
                UpdateOrbitCamera();
            }
        }
    }

    void Update()
    {
        if (targetCamera == null) return;
        
        // 关键修复：如果相机被禁用，或者游戏场景已加载（叠加场景），不处理输入
        // 这样可以避免MainHub场景的CameraController在游戏场景加载时干扰
        if (!targetCamera.enabled)
        {
            return;
        }
        
        // 检查是否在MainHub场景中，如果不是，不处理输入（避免干扰游戏场景）
        string currentSceneName = gameObject.scene.name;
        bool isMainHubScene = currentSceneName.StartsWith("0") && currentSceneName.Contains("_MainHub");
        if (!isMainHubScene)
        {
            return;
        }
        
        // 如果游戏场景已加载（叠加场景），不处理输入
        if (SceneTransitionManager.Instance != null && SceneTransitionManager.Instance.IsGameSceneLoaded())
        {
            return;
        }

        // 鼠标旋转
        if (enableMouseRotation)
        {
            HandleMouseRotation();
        }

        // 鼠标中键平移
        if (enableMousePan)
        {
            HandleMousePan();
        }

        // 滚轮缩放
        if (enableMouseWheelZoom)
        {
            HandleMouseWheelZoom();
        }

        // WASD移动
        if (enableWASDMovement)
        {
            HandleWASDMovement();
        }
    }

    /// <summary>
    /// 处理鼠标旋转
    /// </summary>
    private void HandleMouseRotation()
    {
        // 如果正在平移，不处理旋转（避免冲突）
        if (isPanning) return;

        // 检查是否点击了鼠标旋转按键
        if (Input.GetKeyDown(mouseRotateKey))
        {
            // 如果鼠标在UI上，不处理旋转（避免与UI点击冲突）
            if (IsPointerOverUI())
            {
                return;
            }

            // 关键修复：检查是否点击在 Sphere 上，如果是则不开始旋转（让 SphereClickHandler 处理）
            if (IsClickingOnSphere())
            {
                return;
            }

            isRotating = true;
            lastMousePosition = Input.mousePosition;
        }

        // 如果按住鼠标按键，进行旋转
        if (isRotating && Input.GetKey(mouseRotateKey))
        {
            Vector3 mouseDelta = Input.mousePosition - lastMousePosition;

            // --- 修改部分开始：增加轴锁定判断 ---

            // 只有在未锁定Y轴时，才计算水平旋转
            if (!lockRotationY)
            {
                rotationY += mouseDelta.x * mouseSensitivity;
            }

            // 只有在未锁定X轴时，才计算垂直旋转
            if (!lockRotationX)
            {
                rotationX -= mouseDelta.y * mouseSensitivity; // 注意这里是减号，因为鼠标向上移动应该让相机向下看
            }

            // --- 修改部分结束 ---

            // 限制垂直旋转范围
            if (limitVerticalRotation)
            {
                rotationX = Mathf.Clamp(rotationX, minVerticalAngle, maxVerticalAngle);
            }

            // 根据模式应用旋转
            if (enableOrbitMode)
            {
                // 轨道模式：更新相机位置和朝向
                UpdateOrbitCamera();
            }
            else
            {
                // 普通模式：只旋转相机角度
                transform.rotation = Quaternion.Euler(rotationX, rotationY, 0f);
            }

            lastMousePosition = Input.mousePosition;
        }

        // 松开鼠标按键时停止旋转
        if (Input.GetKeyUp(mouseRotateKey))
        {
            isRotating = false;
        }
    }

    /// <summary>
    /// 获取轨道目标点位置
    /// </summary>
    private Vector3 GetOrbitTargetPosition()
    {
        if (orbitTarget != null)
        {
            return orbitTarget.position;
        }
        return orbitTargetPosition;
    }

    /// <summary>
    /// 更新轨道相机的位置和朝向
    /// </summary>
    private void UpdateOrbitCamera()
    {
        Vector3 targetPos = GetOrbitTargetPosition();

        // 将角度转换为弧度
        float radX = rotationX * Mathf.Deg2Rad;
        float radY = rotationY * Mathf.Deg2Rad;

        // 计算相机位置（球坐标系）
        // X轴旋转影响垂直角度，Y轴旋转影响水平角度
        float cosX = Mathf.Cos(radX);
        float sinX = Mathf.Sin(radX);
        float cosY = Mathf.Cos(radY);
        float sinY = Mathf.Sin(radY);

        // 计算相机相对于目标点的偏移
        Vector3 offset = new Vector3(
            currentOrbitRadius * cosX * sinY,  // X轴：水平方向
            currentOrbitRadius * sinX,         // Y轴：垂直方向（上下）
            currentOrbitRadius * cosX * cosY    // Z轴：前后方向
        );

        // 设置相机位置
        transform.position = targetPos + offset;

        // 相机朝向目标点
        transform.LookAt(targetPos);
    }

    /// <summary>
    /// 处理鼠标中键平移（类似Unity场景视图的相机平移）
    /// </summary>
    private void HandleMousePan()
    {
        // 如果正在旋转，不处理平移（避免冲突）
        if (isRotating) return;

        // 检查是否按下鼠标中键
        if (Input.GetKeyDown(KeyCode.Mouse2))
        {
            // 如果鼠标在UI上，不处理平移（避免与UI点击冲突）
            if (IsPointerOverUI())
            {
                return;
            }

            isPanning = true;
            lastPanMousePosition = Input.mousePosition;
        }

        // 如果按住鼠标中键，进行平移
        if (isPanning && Input.GetKey(KeyCode.Mouse2))
        {
            Vector3 mouseDelta = Input.mousePosition - lastPanMousePosition;

            // 计算平移距离（参考Unity场景视图，根据相机距离调整平移量）
            float distanceMultiplier = panSensitivity;

            // 对于透视相机，根据相机到原点的距离调整平移速度（可选）
            if (!targetCamera.orthographic)
            {
                float distanceToOrigin = Vector3.Distance(transform.position, Vector3.zero);
                distanceMultiplier = panSensitivity * distanceToOrigin * 0.01f; // 距离越远，平移速度越快
            }

            // 对于正交相机，根据Orthographic Size调整平移速度
            else
            {
                distanceMultiplier = panSensitivity * targetCamera.orthographicSize * 0.01f;
            }

            // 计算平移方向（沿相机平面的XY方向）
            Vector3 panDirection = -transform.right * mouseDelta.x * distanceMultiplier
                                 + transform.up * mouseDelta.y * distanceMultiplier;

            // 应用平移
            transform.position += panDirection;

            lastPanMousePosition = Input.mousePosition;
        }

        // 松开鼠标中键时停止平移
        if (Input.GetKeyUp(KeyCode.Mouse2))
        {
            isPanning = false;
        }
    }

    /// <summary>
    /// 处理滚轮缩放
    /// </summary>
    private void HandleMouseWheelZoom()
    {
        float scroll = Input.GetAxis("Mouse ScrollWheel");

        if (Mathf.Abs(scroll) > 0.01f)
        {
            if (enableOrbitMode)
            {
                // 轨道模式：调整轨道半径
                currentOrbitRadius -= scroll * zoomSpeed;
                currentOrbitRadius = Mathf.Clamp(currentOrbitRadius, minZoomDistance, maxZoomDistance);
                orbitRadius = currentOrbitRadius; // 同步更新配置值

                // 更新相机位置
                UpdateOrbitCamera();
            }
            else if (targetCamera.orthographic)
            {
                // 正交相机：调整Orthographic Size
                float newSize = targetCamera.orthographicSize - scroll * zoomSpeed;
                targetCamera.orthographicSize = Mathf.Clamp(newSize, minZoomDistance, maxZoomDistance);
            }
            else
            {
                // 透视相机：沿相机前方向移动
                Vector3 forward = transform.forward;
                float currentDistance = Vector3.Distance(transform.position, Vector3.zero); // 可以改为其他参考点

                // 计算新的距离
                float newDistance = currentDistance - scroll * zoomSpeed;
                newDistance = Mathf.Clamp(newDistance, minZoomDistance, maxZoomDistance);

                // 移动相机位置
                transform.position = transform.position + forward * (scroll * zoomSpeed);
            }
        }
    }

    /// <summary>
    /// 处理WASD移动
    /// </summary>
    private void HandleWASDMovement()
    {
        // 获取输入方向
        Vector3 moveDirection = Vector3.zero;

        if (Input.GetKey(KeyCode.W))
        {
            moveDirection += transform.forward; // 向前（相机朝向）
        }
        if (Input.GetKey(KeyCode.S))
        {
            moveDirection -= transform.forward; // 向后
        }
        if (Input.GetKey(KeyCode.A))
        {
            moveDirection -= transform.right; // 向左
        }
        if (Input.GetKey(KeyCode.D))
        {
            moveDirection += transform.right; // 向右
        }

        // 如果有输入，进行移动
        if (moveDirection != Vector3.zero)
        {
            // 计算移动速度（按住Shift时加速）
            float currentMoveSpeed = moveSpeed;
            if (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift))
            {
                currentMoveSpeed *= fastMoveMultiplier;
            }

            // 规范化方向向量（保持速度一致）
            moveDirection.Normalize();

            // 移动相机（只移动X和Z，保持Y不变，或者可以允许Y移动）
            Vector3 movement = moveDirection * currentMoveSpeed * Time.deltaTime;

            // 可选：锁定Y轴移动（如果需要的话）
            // movement.y = 0;

            transform.position += movement;
        }
    }

    /// <summary>
    /// 检查鼠标是否在UI元素上（包括World Space Canvas）
    /// </summary>
    private bool IsPointerOverUI()
    {
        // 方法1：检查EventSystem（对Screen Space Canvas有效）
        if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
        {
            return true;
        }

        // 方法2：使用GraphicRaycaster检测所有Canvas（包括World Space Canvas）
        GraphicRaycaster[] raycasters = FindObjectsOfType<GraphicRaycaster>();

        if (raycasters != null && raycasters.Length > 0)
        {
            PointerEventData pointerData = new PointerEventData(EventSystem.current);
            pointerData.position = Input.mousePosition;

            foreach (GraphicRaycaster raycaster in raycasters)
            {
                if (raycaster == null || !raycaster.enabled) continue;

                // 对于World Space Canvas，需要指定Event Camera
                // 注意：如果Canvas没有指定WorldCamera，Raycast可能不准确，这里简单处理

                List<RaycastResult> results = new List<RaycastResult>();

                // 执行射线检测
                raycaster.Raycast(pointerData, results);

                // 如果检测到UI元素，返回true
                if (results.Count > 0)
                {
                    // 检查结果中是否有可交互的UI元素（Button、Image等）
                    foreach (RaycastResult result in results)
                    {
                        if (result.gameObject != null)
                        {
                            // 检查是否有可交互的组件
                            Selectable selectable = result.gameObject.GetComponent<Selectable>();
                            Image image = result.gameObject.GetComponent<Image>();

                            // 如果RaycastTarget启用，认为是可以点击的UI
                            if (image != null && image.raycastTarget)
                            {
                                return true;
                            }

                            if (selectable != null && selectable.interactable)
                            {
                                return true;
                            }
                        }
                    }
                }
            }
        }

        return false;
    }

    /// <summary>
    /// 检查是否点击在 Sphere 上（用于避免与 SphereClickHandler 冲突）
    /// </summary>
    private bool IsClickingOnSphere()
    {
        if (targetCamera == null) return false;

        // 创建从相机到鼠标位置的射线
        Ray ray = targetCamera.ScreenPointToRay(Input.mousePosition);
        RaycastHit hit;

        // 执行射线检测（使用所有Layer）
        if (Physics.Raycast(ray, out hit, 1000f))
        {
            GameObject hitObject = hit.collider.gameObject;

            // 检查是否是 Sphere（通过 Tag 或名称）
            if (hitObject.CompareTag("Sphere") || 
                hitObject.name.StartsWith("Sphere") || 
                hitObject.name.Contains("Sphere"))
            {
                return true;
            }
        }

        return false;
    }
}