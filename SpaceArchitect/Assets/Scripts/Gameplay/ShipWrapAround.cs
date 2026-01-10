using UnityEngine;

/// <summary>
/// 飞船环绕边界脚本
/// 当飞船飞出镜头时，从镜头的另一边以相同的角度和速度飞回来
/// </summary>
[RequireComponent(typeof(ShipState))]
[RequireComponent(typeof(NBody))]
public class ShipWrapAround : MonoBehaviour
{
    [Header("摄像机设置")]
    [Tooltip("目标摄像机（如果为空，使用主摄像机）")]
    [SerializeField] private Camera targetCamera;
    
    [Tooltip("边界扩展（世界单位），飞船超出此范围时才会传送")]
    [SerializeField] private float boundaryMargin = 5f;
    
    [Header("传送设置")]
    [Tooltip("是否启用环绕功能")]
    [SerializeField] private bool enableWrapAround = true;
    
    [Tooltip("传送时的平滑过渡时间（秒），0表示立即传送")]
    [SerializeField] private float transitionTime = 0f;
    
    // 注意：当前实现总是保持速度方向，此参数保留用于未来扩展
    // [Tooltip("是否在传送时保持速度方向（true）还是反转方向（false）")]
    // [SerializeField] private bool maintainVelocityDirection = true;
    
    [Header("调试")]
    [Tooltip("显示调试信息")]
    [SerializeField] private bool showDebugLogs = false;
    
    private ShipState shipState;
    private NBody nBody;
    private GravityEngine gravityEngine;
    private Vector3 lastPosition;
    
    void Awake()
    {
        shipState = GetComponent<ShipState>();
        if (shipState == null)
        {
            Debug.LogError($"ShipWrapAround: {gameObject.name} 缺少 ShipState 组件！");
        }
        
        nBody = GetComponent<NBody>();
        if (nBody == null)
        {
            Debug.LogError($"ShipWrapAround: {gameObject.name} 缺少 NBody 组件！");
        }
    }
    
    void Start()
    {
        // 获取摄像机
        if (targetCamera == null)
        {
            targetCamera = Camera.main;
        }
        
        if (targetCamera == null)
        {
            Debug.LogWarning("ShipWrapAround: 未找到摄像机，环绕功能将不可用");
        }
        
        // 获取GravityEngine
        gravityEngine = GravityEngine.instance;
        if (gravityEngine == null)
        {
            Debug.LogWarning("ShipWrapAround: 未找到 GravityEngine，速度保持功能可能不可用");
        }
        
        lastPosition = transform.position;
    }
    
    void LateUpdate()
    {
        // 只在飞行状态下检查环绕
        if (!enableWrapAround || shipState == null || 
            (shipState.CurrentState != ShipState.State.Flying && 
             shipState.CurrentState != ShipState.State.Captured))
        {
            return;
        }
        
        if (targetCamera == null)
        {
            return;
        }
        
        // 检查是否需要环绕
        CheckAndWrapAround();
        
        lastPosition = transform.position;
    }
    
    /// <summary>
    /// 检查并执行环绕传送
    /// </summary>
    private void CheckAndWrapAround()
    {
        Vector3 currentPos = transform.position;
        Vector3 viewportPos = targetCamera.WorldToViewportPoint(currentPos);
        
        // 检查是否在摄像机前方（Z > 0）
        if (viewportPos.z < 0)
        {
            // 飞船在摄像机后方，不处理
            return;
        }
        
        // 计算边界阈值（视口坐标范围是 [0, 1]）
        // boundaryMargin 转换为视口坐标的比例
        float width = GetCameraWidth();
        float height = GetCameraHeight();
        float marginX = boundaryMargin / width;  // X轴方向的边界扩展（视口坐标）
        float marginY = boundaryMargin / height;   // Y轴方向的边界扩展（视口坐标）
        
        // 检查是否在视口范围内（考虑边界扩展）
        bool needsWrap = false;
        Vector3 wrapOffset = Vector3.zero;
        
        // 检查X轴（左右边界）
        if (viewportPos.x < -marginX)
        {
            // 从左边飞出，传送到右边
            needsWrap = true;
            wrapOffset.x = width;
            if (showDebugLogs)
            {
                Debug.Log($"[ShipWrapAround] 飞船从左边飞出（视口X: {viewportPos.x:F3}），传送到右边");
            }
        }
        else if (viewportPos.x > 1 + marginX)
        {
            // 从右边飞出，传送到左边
            needsWrap = true;
            wrapOffset.x = -width;
            if (showDebugLogs)
            {
                Debug.Log($"[ShipWrapAround] 飞船从右边飞出（视口X: {viewportPos.x:F3}），传送到左边");
            }
        }
        
        // 检查Y轴（上下边界）
        if (viewportPos.y < -marginY)
        {
            // 从下边飞出，传送到上边
            needsWrap = true;
            wrapOffset.y = height;
            if (showDebugLogs)
            {
                Debug.Log($"[ShipWrapAround] 飞船从下边飞出（视口Y: {viewportPos.y:F3}），传送到上边");
            }
        }
        else if (viewportPos.y > 1 + marginY)
        {
            // 从上边飞出，传送到下边
            needsWrap = true;
            wrapOffset.y = -height;
            if (showDebugLogs)
            {
                Debug.Log($"[ShipWrapAround] 飞船从上边飞出（视口Y: {viewportPos.y:F3}），传送到下边");
            }
        }
        
        // 执行传送
        if (needsWrap)
        {
            WrapAround(wrapOffset);
        }
    }
    
    /// <summary>
    /// 执行环绕传送
    /// </summary>
    /// <param name="offset">传送偏移量</param>
    private void WrapAround(Vector3 offset)
    {
        // 保存当前速度
        Vector3 currentVelocity = Vector3.zero;
        if (gravityEngine != null && nBody != null && nBody.engineRef != null)
        {
            try
            {
                currentVelocity = gravityEngine.GetVelocity(nBody);
            }
            catch
            {
                // 如果无法获取速度，尝试从Rigidbody获取
                Rigidbody rb = GetComponent<Rigidbody>();
                if (rb != null)
                {
                    currentVelocity = rb.velocity;
                }
            }
        }
        else
        {
            // 如果GravityEngine不可用，从Rigidbody获取
            Rigidbody rb = GetComponent<Rigidbody>();
            if (rb != null)
            {
                currentVelocity = rb.velocity;
            }
        }
        
        // 计算新位置
        Vector3 newPosition = transform.position + offset;
        newPosition.z = transform.position.z; // 保持Z轴不变
        
        // 执行传送
        if (transitionTime > 0f)
        {
            // 平滑过渡（可选，通常不需要）
            StartCoroutine(SmoothWrapAround(newPosition, currentVelocity));
        }
        else
        {
            // 立即传送
            transform.position = newPosition;
            
            // 更新GravityEngine中的位置
            if (gravityEngine != null && nBody != null && nBody.engineRef != null)
            {
                UpdateGravityEnginePosition(newPosition);
            }
            
            if (showDebugLogs)
            {
                Debug.Log($"[ShipWrapAround] 飞船已传送到: {newPosition}, 速度: {currentVelocity}");
            }
        }
    }
    
    /// <summary>
    /// 平滑过渡传送（协程）
    /// </summary>
    private System.Collections.IEnumerator SmoothWrapAround(Vector3 targetPosition, Vector3 velocity)
    {
        Vector3 startPosition = transform.position;
        float elapsedTime = 0f;
        
        while (elapsedTime < transitionTime)
        {
            elapsedTime += Time.deltaTime;
            float t = elapsedTime / transitionTime;
            
            transform.position = Vector3.Lerp(startPosition, targetPosition, t);
            
            yield return null;
        }
        
        transform.position = targetPosition;
        
        // 更新GravityEngine中的位置
        if (gravityEngine != null && nBody != null && nBody.engineRef != null)
        {
            UpdateGravityEnginePosition(targetPosition);
        }
    }
    
    /// <summary>
    /// 更新GravityEngine中的位置
    /// </summary>
    private void UpdateGravityEnginePosition(Vector3 worldPosition)
    {
        if (gravityEngine == null || nBody == null || nBody.engineRef == null)
        {
            return;
        }
        
        try
        {
            // 转换为物理空间坐标
            Vector3 physPos = worldPosition / gravityEngine.physToWorldFactor;
            Vector3d physPos3d = new Vector3d(physPos);
            
            // 更新位置
            gravityEngine.SetPositionDoubleV3(nBody, physPos3d);
            
            // 更新NBody的初始位置
            nBody.initialPhysPosition = physPos;
        }
        catch (System.Exception e)
        {
            if (showDebugLogs)
            {
                Debug.LogWarning($"[ShipWrapAround] 更新GravityEngine位置失败: {e.Message}");
            }
        }
    }
    
    /// <summary>
    /// 获取摄像机宽度（世界单位）
    /// </summary>
    private float GetCameraWidth()
    {
        if (targetCamera == null)
        {
            return 20f; // 默认值
        }
        
        if (targetCamera.orthographic)
        {
            // 正交摄像机
            float height = targetCamera.orthographicSize * 2f;
            float width = height * targetCamera.aspect;
            return width;
        }
        else
        {
            // 透视摄像机
            float distance = Vector3.Distance(transform.position, targetCamera.transform.position);
            float height = 2f * distance * Mathf.Tan(targetCamera.fieldOfView * 0.5f * Mathf.Deg2Rad);
            float width = height * targetCamera.aspect;
            return width;
        }
    }
    
    /// <summary>
    /// 获取摄像机高度（世界单位）
    /// </summary>
    private float GetCameraHeight()
    {
        if (targetCamera == null)
        {
            return 20f; // 默认值
        }
        
        if (targetCamera.orthographic)
        {
            // 正交摄像机
            return targetCamera.orthographicSize * 2f;
        }
        else
        {
            // 透视摄像机
            float distance = Vector3.Distance(transform.position, targetCamera.transform.position);
            return 2f * distance * Mathf.Tan(targetCamera.fieldOfView * 0.5f * Mathf.Deg2Rad);
        }
    }
    
    /// <summary>
    /// 在编辑器中绘制边界（Gizmos）
    /// </summary>
    void OnDrawGizmosSelected()
    {
        if (targetCamera == null)
        {
            targetCamera = Camera.main;
        }
        
        if (targetCamera == null)
        {
            return;
        }
        
        // 绘制摄像机视野边界
        Gizmos.color = Color.cyan;
        
        float width = GetCameraWidth();
        float height = GetCameraHeight();
        Vector3 center = targetCamera.transform.position;
        center.z = transform.position.z; // 使用飞船的Z轴
        
        // 绘制边界框
        Vector3 bottomLeft = center + new Vector3(-width / 2f, -height / 2f, 0f);
        Vector3 bottomRight = center + new Vector3(width / 2f, -height / 2f, 0f);
        Vector3 topLeft = center + new Vector3(-width / 2f, height / 2f, 0f);
        Vector3 topRight = center + new Vector3(width / 2f, height / 2f, 0f);
        
        Gizmos.DrawLine(bottomLeft, bottomRight);
        Gizmos.DrawLine(bottomRight, topRight);
        Gizmos.DrawLine(topRight, topLeft);
        Gizmos.DrawLine(topLeft, bottomLeft);
        
        // 绘制扩展边界
        if (boundaryMargin > 0f)
        {
            Gizmos.color = Color.yellow;
            float marginWidth = width + boundaryMargin * 2f;
            float marginHeight = height + boundaryMargin * 2f;
            
            Vector3 marginBottomLeft = center + new Vector3(-marginWidth / 2f, -marginHeight / 2f, 0f);
            Vector3 marginBottomRight = center + new Vector3(marginWidth / 2f, -marginHeight / 2f, 0f);
            Vector3 marginTopLeft = center + new Vector3(-marginWidth / 2f, marginHeight / 2f, 0f);
            Vector3 marginTopRight = center + new Vector3(marginWidth / 2f, marginHeight / 2f, 0f);
            
            Gizmos.DrawLine(marginBottomLeft, marginBottomRight);
            Gizmos.DrawLine(marginBottomRight, marginTopRight);
            Gizmos.DrawLine(marginTopRight, marginTopLeft);
            Gizmos.DrawLine(marginTopLeft, marginBottomLeft);
        }
    }
}

