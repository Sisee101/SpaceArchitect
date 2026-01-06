using UnityEngine;

/// <summary>
/// 飞船发射器
/// 实现弹弓式发射机制：拖拽鼠标产生反向力量发射飞船
/// </summary>
public class ShipLauncher : MonoBehaviour
{
    [Header("发射设置")]
    [Tooltip("拖拽力量的缩放系数")]
    [SerializeField] private float launchForceMultiplier = 5f;

    [Tooltip("最大拖拽距离（屏幕单位）")]
    [SerializeField] private float maxDragDistance = 200f;

    [Header("视觉反馈")]
    [Tooltip("箭头线条渲染器")]
    [SerializeField] private LineRenderer arrowLine;

    [Tooltip("箭头头部")]
    [SerializeField] private Transform arrowHead;

    [Tooltip("箭头颜色")]
    [SerializeField] private Color arrowColor = Color.yellow;

    [Tooltip("箭头宽度")]
    [SerializeField] private float arrowWidth = 0.1f;

    // 内部状态
    private ShipState shipState;
    private TrajectoryPredictor trajectoryPredictor;
    private Camera mainCamera;
    private bool isDragging = false;
    private Vector3 dragStartPosition;
    private Vector3 shipScreenPosition;

    void Awake()
    {
        shipState = GetComponent<ShipState>();
        if (shipState == null)
        {
            Debug.LogError("ShipLauncher: 缺少 ShipState 组件！");
        }

        trajectoryPredictor = GetComponent<TrajectoryPredictor>();
        if (trajectoryPredictor == null)
        {
            Debug.LogWarning("ShipLauncher: 没有找到 TrajectoryPredictor 组件，轨迹预览功能将不可用");
        }

        mainCamera = Camera.main;
        if (mainCamera == null)
        {
            Debug.LogError("ShipLauncher: 场景中没有主相机！");
        }

        // 初始化LineRenderer
        InitializeArrow();
    }

    void Update()
    {
        // 只有在PreLaunch状态下才能发射
        if (shipState == null || !shipState.CanLaunch())
        {
            if (isDragging)
            {
                CancelDrag();
            }
            return;
        }

        HandleInput();
    }

    /// <summary>
    /// 处理输入
    /// </summary>
    private void HandleInput()
    {
        // 鼠标按下：开始拖拽
        if (Input.GetMouseButtonDown(0))
        {
            StartDrag();
        }

        // 鼠标拖拽中：更新箭头
        if (isDragging && Input.GetMouseButton(0))
        {
            UpdateDrag();
        }

        // 鼠标松开：发射飞船
        if (Input.GetMouseButtonUp(0) && isDragging)
        {
            EndDrag();
        }
    }

    /// <summary>
    /// 开始拖拽
    /// </summary>
    private void StartDrag()
    {
        // 检查鼠标是否在飞船附近
        Vector3 mousePosition = Input.mousePosition;
        shipScreenPosition = mainCamera.WorldToScreenPoint(transform.position);

        // 简单的距离检测（可以根据需要调整）
        float distanceToShip = Vector2.Distance(
            new Vector2(mousePosition.x, mousePosition.y),
            new Vector2(shipScreenPosition.x, shipScreenPosition.y)
        );

        // 如果鼠标在飞船附近，开始拖拽
        if (distanceToShip < 100f) // 100像素范围内
        {
            // 检查是否有Core物体在鼠标位置（防止冲突）
            // 只有当鼠标明确点击在Core上时，才跳过飞船拖拽
            Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);
            RaycastHit hit;
            
            if (Physics.Raycast(ray, out hit))
            {
                // 如果击中的是Core，并且鼠标不在飞船很近的地方，跳过飞船拖拽
                if (hit.collider != null && hit.collider.CompareTag("Core") && distanceToShip > 50f)
                {
                    Debug.Log("鼠标点击在Core上，跳过飞船拖拽");
                    return;
                }
            }

            // 开始飞船拖拽
            isDragging = true;
            dragStartPosition = mousePosition;
            ShowArrow(true);
            Debug.Log($"开始拖拽飞船，距离: {distanceToShip}");
        }
    }

    /// <summary>
    /// 更新拖拽
    /// </summary>
    private void UpdateDrag()
    {
        Vector3 currentMousePosition = Input.mousePosition;
        Vector3 dragVector = dragStartPosition - currentMousePosition;

        // 限制拖拽距离
        float dragDistance = dragVector.magnitude;
        if (dragDistance > maxDragDistance)
        {
            dragVector = dragVector.normalized * maxDragDistance;
        }

        // 更新箭头显示
        UpdateArrowVisual(dragVector);
        
        // 更新轨迹预览
        UpdateTrajectoryPreview(dragVector);
    }

    /// <summary>
    /// 结束拖拽并发射
    /// </summary>
    private void EndDrag()
    {
        Vector3 currentMousePosition = Input.mousePosition;
        Vector3 dragVector = dragStartPosition - currentMousePosition;

        // 限制拖拽距离
        float dragDistance = dragVector.magnitude;
        if (dragDistance > maxDragDistance)
        {
            dragVector = dragVector.normalized * maxDragDistance;
        }

        // 计算发射速度（在XY平面上）
        Vector3 launchVelocity = CalculateLaunchVelocity(dragVector);

        // 发射飞船
        if (shipState != null && launchVelocity.magnitude > 0.1f)
        {
            shipState.Launch(launchVelocity);
            Debug.Log($"飞船发射！速度: {launchVelocity}");
            
            // 清除轨迹预览
            if (trajectoryPredictor != null)
            {
                trajectoryPredictor.ClearPreviewTrajectory();
            }
        }

        // 清理
        isDragging = false;
        ShowArrow(false);
    }

    /// <summary>
    /// 取消拖拽
    /// </summary>
    private void CancelDrag()
    {
        isDragging = false;
        ShowArrow(false);
        
        // 清除轨迹预览
        if (trajectoryPredictor != null)
        {
            trajectoryPredictor.HideTrajectory();
        }
    }
    
    /// <summary>
    /// 更新轨迹预览
    /// </summary>
    /// <param name="dragVector">拖拽向量</param>
    private void UpdateTrajectoryPreview(Vector3 dragVector)
    {
        if (trajectoryPredictor == null)
            return;
        
        // 计算发射速度
        Vector3 launchVelocity = CalculateLaunchVelocity(dragVector);
        
        // 如果速度太小，隐藏轨迹
        if (launchVelocity.magnitude < 0.1f)
        {
            trajectoryPredictor.HideTrajectory();
            return;
        }
        
        // 更新预览轨迹
        trajectoryPredictor.SetPreviewTrajectory(transform.position, launchVelocity);
        
        // 确保轨迹可见
        trajectoryPredictor.ShowTrajectory();
    }

    /// <summary>
    /// 计算发射速度
    /// </summary>
    /// <param name="dragVector">拖拽向量（屏幕空间）</param>
    /// <returns>世界空间速度（Z轴为0）</returns>
    private Vector3 CalculateLaunchVelocity(Vector3 dragVector)
    {
        // 确保dragVector在屏幕空间（只保留x和y）
        Vector2 dragVector2D = new Vector2(dragVector.x, dragVector.y);
        
        // 获取相机到飞船的距离（用于屏幕转世界的深度）
        float distanceToCamera = Vector3.Distance(mainCamera.transform.position, transform.position);

        // 计算拖拽起点和终点在世界空间的位置
        Vector3 startWorldPos = mainCamera.ScreenToWorldPoint(
            new Vector3(shipScreenPosition.x, shipScreenPosition.y, distanceToCamera)
        );
        
        Vector3 endWorldPos = mainCamera.ScreenToWorldPoint(
            new Vector3(shipScreenPosition.x + dragVector.x, shipScreenPosition.y + dragVector.y, distanceToCamera)
        );

        // 计算世界空间中的方向（从起点到终点）
        Vector3 worldDirection = endWorldPos - startWorldPos;

        // 只保留XY平面的速度，Z轴设为0
        worldDirection.z = 0f;

        // 计算拖拽比例（0-1）
        float dragRatio = Mathf.Clamp01(dragVector2D.magnitude / maxDragDistance);

        // 应用力量系数和拖拽比例
        Vector3 velocity = worldDirection.normalized * dragRatio * launchForceMultiplier;

        Debug.Log($"拖拽距离: {dragVector2D.magnitude}, 拖拽比例: {dragRatio}, 世界方向: {worldDirection}, 最终速度: {velocity}");
        
        return velocity;
    }

    /// <summary>
    /// 初始化箭头
    /// </summary>
    private void InitializeArrow()
    {
        // 如果没有提供LineRenderer，创建一个
        if (arrowLine == null)
        {
            GameObject arrowObj = new GameObject("LaunchArrow");
            arrowObj.transform.SetParent(transform);
            arrowLine = arrowObj.AddComponent<LineRenderer>();
            
            arrowLine.startWidth = arrowWidth;
            arrowLine.endWidth = arrowWidth * 1.5f;
            arrowLine.positionCount = 2;
            arrowLine.material = new Material(Shader.Find("Sprites/Default"));
            arrowLine.startColor = arrowColor;
            arrowLine.endColor = arrowColor;
        }

        ShowArrow(false);
    }

    /// <summary>
    /// 更新箭头视觉效果
    /// </summary>
    /// <param name="dragVector">拖拽向量</param>
    private void UpdateArrowVisual(Vector3 dragVector)
    {
        if (arrowLine == null) return;

        // 获取相机到飞船的距离
        float distanceToCamera = Vector3.Distance(mainCamera.transform.position, transform.position);

        // 计算拖拽起点和终点在世界空间的位置
        Vector3 startWorldPos = mainCamera.ScreenToWorldPoint(
            new Vector3(shipScreenPosition.x, shipScreenPosition.y, distanceToCamera)
        );
        
        Vector3 endWorldPos = mainCamera.ScreenToWorldPoint(
            new Vector3(shipScreenPosition.x + dragVector.x, shipScreenPosition.y + dragVector.y, distanceToCamera)
        );

        // 计算箭头终点（从飞船位置出发，沿着拖拽方向）
        Vector3 arrowDirection = endWorldPos - startWorldPos;
        arrowDirection.z = 0f; // 保持在XY平面
        Vector3 arrowEndWorld = transform.position + arrowDirection;

        // 设置箭头位置（从飞船指向拖拽方向）
        arrowLine.SetPosition(0, transform.position);
        arrowLine.SetPosition(1, arrowEndWorld);

        // 根据拖拽距离调整箭头宽度
        float dragRatio = Mathf.Clamp01(dragVector.magnitude / maxDragDistance);
        arrowLine.startWidth = arrowWidth * (0.5f + dragRatio);
        arrowLine.endWidth = arrowWidth * (1f + dragRatio * 2f);

        // 更新箭头头部位置（如果有）
        if (arrowHead != null)
        {
            arrowHead.position = arrowEndWorld;
            // 在2D游戏中使用Quaternion.LookRotation可能需要调整
            Vector3 arrowDir = (arrowEndWorld - transform.position).normalized;
            if (arrowDir.magnitude > 0.01f)
            {
                arrowHead.rotation = Quaternion.LookRotation(Vector3.forward, arrowDir);
            }
        }
    }

    /// <summary>
    /// 显示/隐藏箭头
    /// </summary>
    /// <param name="show">是否显示</param>
    private void ShowArrow(bool show)
    {
        if (arrowLine != null)
        {
            arrowLine.enabled = show;
        }

        if (arrowHead != null)
        {
            arrowHead.gameObject.SetActive(show);
        }
    }

    /// <summary>
    /// 在编辑器中显示拖拽范围
    /// </summary>
    void OnDrawGizmosSelected()
    {
        if (mainCamera == null) return;

        Gizmos.color = Color.yellow;
        Vector3 screenPos = mainCamera.WorldToScreenPoint(transform.position);
        
        // 绘制屏幕空间的拖拽范围（近似）
        Vector3 worldPos = transform.position;
        Gizmos.DrawWireSphere(worldPos, 1f);
    }
}

