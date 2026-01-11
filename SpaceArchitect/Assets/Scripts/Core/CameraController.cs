using UnityEngine;

/// <summary>
/// 相机控制器
/// 支持鼠标左键拖动旋转、鼠标中键平移、滚轮缩放、WASD移动
/// </summary>
public class CameraController : MonoBehaviour
{
    [Header("相机引用")]
    [SerializeField] private Camera targetCamera; // 目标相机（如果为空，使用当前GameObject上的Camera）
    
    [Header("鼠标控制")]
    [SerializeField] private bool enableMouseRotation = true; // 启用鼠标旋转
    [SerializeField] private float mouseSensitivity = 2f; // 鼠标灵敏度
    [SerializeField] private KeyCode mouseRotateKey = KeyCode.Mouse0; // 鼠标旋转按键（左键）
    
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
    
    [Header("旋转限制（可选）")]
    [SerializeField] private bool limitVerticalRotation = true; // 限制垂直旋转
    [SerializeField] private float minVerticalAngle = -80f; // 最小垂直角度（向上）
    [SerializeField] private float maxVerticalAngle = 80f; // 最大垂直角度（向下）
    
    // 私有变量
    private float rotationX = 0f; // 垂直旋转角度
    private float rotationY = 0f; // 水平旋转角度
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
        }
    }
    
    void Update()
    {
        if (targetCamera == null) return;
        
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
            isRotating = true;
            lastMousePosition = Input.mousePosition;
        }
        
        // 如果按住鼠标按键，进行旋转
        if (isRotating && Input.GetKey(mouseRotateKey))
        {
            Vector3 mouseDelta = Input.mousePosition - lastMousePosition;
            
            // 计算旋转增量
            rotationY += mouseDelta.x * mouseSensitivity;
            rotationX -= mouseDelta.y * mouseSensitivity; // 注意这里是减号，因为鼠标向上移动应该让相机向下看
            
            // 限制垂直旋转
            if (limitVerticalRotation)
            {
                rotationX = Mathf.Clamp(rotationX, minVerticalAngle, maxVerticalAngle);
            }
            
            // 应用旋转
            transform.rotation = Quaternion.Euler(rotationX, rotationY, 0f);
            
            lastMousePosition = Input.mousePosition;
        }
        
        // 松开鼠标按键时停止旋转
        if (Input.GetKeyUp(mouseRotateKey))
        {
            isRotating = false;
        }
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
            // 鼠标向右移动 = 相机向右移动（沿相机的right方向）
            // 鼠标向上移动 = 相机向上移动（沿相机的up方向）
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
            if (targetCamera.orthographic)
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
    /// 设置目标点（用于缩放的参考点）
    /// </summary>
    public void SetTargetPoint(Vector3 targetPoint)
    {
        // 可以扩展这个方法，让缩放围绕指定点进行
        // 目前缩放是沿相机前方向移动
    }
}
