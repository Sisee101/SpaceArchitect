using UnityEngine;

public class SpaceshipLauncher : MonoBehaviour
{
    public GameObject arrowPrefab;
    public float forceMultiplier = 10f;
    public float maxDragDistance = 10f;

    private SpaceshipGravity spaceshipGravity;
    private Rigidbody spaceshipRb;
    private GameObject arrowInstance;
    private Vector3 dragStartPosition;
    private bool isDragging = false;
    private bool hasLaunched = false;
    private Vector3 startPosition;

    // 新增：发射前位置锁定相关变量
    private bool isPositionLocked = true;
    private Vector3 lockedPosition;

    void Start()
    {
        spaceshipGravity = GetComponent<SpaceshipGravity>();
        spaceshipRb = GetComponent<Rigidbody>();
        startPosition = transform.position;
        lockedPosition = startPosition;

        // 新增：初始化位置锁定
        InitializePositionLock();
    }

    void Update()
    {
        HandleMouseInput();

        // 新增：每帧检查位置锁定
        MaintainPositionLock();
    }

    /// <summary>
    /// 新增：初始化位置锁定
    /// </summary>
    private void InitializePositionLock()
    {
        if (spaceshipRb != null)
        {
            // 发射前完全冻结所有物理运动
            spaceshipRb.constraints = RigidbodyConstraints.FreezeAll;
            spaceshipRb.velocity = Vector3.zero;
            spaceshipRb.angularVelocity = Vector3.zero;
            spaceshipRb.isKinematic = true; // 使用运动学模式确保绝对锁定
        }

        // 强制设置到锁定位置
        transform.position = lockedPosition;
        isPositionLocked = true;

        Debug.Log("飞船位置已锁定，等待发射...");
    }

    /// <summary>
    /// 新增：维持位置锁定（防止物理引擎或外部力影响）
    /// </summary>
    private void MaintainPositionLock()
    {
        if (isPositionLocked && !hasLaunched)
        {
            // 双重保险：确保位置不会偏移
            if (Vector3.Distance(transform.position, lockedPosition) > 0.01f)
            {
                transform.position = lockedPosition;
                if (spaceshipRb != null)
                {
                    spaceshipRb.velocity = Vector3.zero;
                    spaceshipRb.angularVelocity = Vector3.zero;
                }
            }
        }
    }

    void HandleMouseInput()
    {
        // 按下鼠标（只有在未发射且位置锁定时才能开始拖拽）
        if (Input.GetMouseButtonDown(0) && !hasLaunched && isPositionLocked)
        {
            StartDrag();
        }

        // 拖拽中
        if (Input.GetMouseButton(0) && isDragging)
        {
            ContinueDrag();
        }

        // 松开鼠标
        if (Input.GetMouseButtonUp(0) && isDragging)
        {
            EndDrag();
        }
    }

    void StartDrag()
    {
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);

        if (Physics.Raycast(ray, out RaycastHit _))
        {
            // 这里我们不需要使用hit变量，所以用下划线忽略
        }

        dragStartPosition = GetMouseWorldPosition();

        // 检查是否点击了有效位置（非行星）
        if (float.IsInfinity(dragStartPosition.x))
        {
            isDragging = false;
            return;
        }

        isDragging = true;

        // 创建箭头
        if (arrowPrefab != null)
        {
            arrowInstance = Instantiate(arrowPrefab, transform.position, Quaternion.identity);
        }

        Debug.Log("开始拖拽，准备发射...");
    }

    void ContinueDrag()
    {
        Vector3 currentMousePos = GetMouseWorldPosition();

        // 检查鼠标位置是否有效
        if (float.IsInfinity(currentMousePos.x))
        {
            return;
        }

        Vector3 dragVector = dragStartPosition - currentMousePos;
        dragVector.y = 0; // 保持水平方向

        // 限制最大拖拽距离
        if (dragVector.magnitude > maxDragDistance)
        {
            dragVector = dragVector.normalized * maxDragDistance;
        }

        // 更新箭头
        if (arrowInstance != null)
        {
            UpdateArrowVisual(dragVector);
        }
    }

    // 新增：专门处理箭头视觉更新的方法
    void UpdateArrowVisual(Vector3 dragVector)
    {
        arrowInstance.transform.position = transform.position;

        // 只有有效方向才更新旋转
        if (dragVector.magnitude > 0.1f)
        {
            arrowInstance.transform.rotation = Quaternion.LookRotation(dragVector);

            // 根据拖拽距离缩放箭头
            float scale = dragVector.magnitude / maxDragDistance;
            arrowInstance.transform.localScale = new Vector3(1, 1, 1 + scale * 2);
        }
    }

    void EndDrag()
    {
        if (arrowInstance != null)
        {
            Destroy(arrowInstance);
        }

        Vector3 currentMousePos = GetMouseWorldPosition();

        // 检查鼠标位置是否有效
        if (float.IsInfinity(currentMousePos.x))
        {
            isDragging = false;
            return;
        }

        Vector3 launchDirection = (dragStartPosition - currentMousePos).normalized;
        launchDirection.y = 0; // 只有水平方向

        float dragDistance = Vector3.Distance(dragStartPosition, currentMousePos);
        dragDistance = Mathf.Min(dragDistance, maxDragDistance);

        // 只有有效拖拽才发射
        if (dragDistance < 0.1f)
        {
            isDragging = false;
            Debug.Log("拖拽距离过小，取消发射");
            return;
        }

        float launchForce = (dragDistance / maxDragDistance) * forceMultiplier;

        // 新增：发射前解除位置锁定，但锁定Y轴移动
        UnlockPositionForLaunch();

        // 应用速度（只在XZ平面）
        Vector3 launchVelocity = launchDirection * launchForce;
        spaceshipRb.velocity = launchVelocity;

        hasLaunched = true;
        spaceshipGravity.SetLaunched(true);

        isDragging = false;

        Debug.Log($"飞船已发射！速度: {launchVelocity.magnitude:F2}, 方向: {launchDirection}");
    }

    /// <summary>
    /// 新增：发射时解除锁定，但限制Y轴移动
    /// </summary>
    private void UnlockPositionForLaunch()
    {
        if (spaceshipRb != null)
        {
            // 解除运动学模式，启用物理模拟
            spaceshipRb.isKinematic = false;

            // 冻结Y轴位置和所有旋转，只允许XZ平面移动
            spaceshipRb.constraints = RigidbodyConstraints.FreezePositionY |
                                     RigidbodyConstraints.FreezeRotationX |
                                     RigidbodyConstraints.FreezeRotationY |
                                     RigidbodyConstraints.FreezeRotationZ;

            // 确保Y轴位置固定
            Vector3 currentPos = transform.position;
            transform.position = new Vector3(currentPos.x, lockedPosition.y, currentPos.z);
        }

        isPositionLocked = false;
        Debug.Log("位置锁定已解除，Y轴移动已禁用");
    }

    Vector3 GetMouseWorldPosition()
    {
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);

        // 检测是否点击在行星上
        if (Physics.Raycast(ray, out RaycastHit hit))
        {
            if (hit.collider.CompareTag("planet"))
            {
                // 如果点击了行星，返回一个无效位置
                return Vector3.positiveInfinity;
            }
        }

        // 创建地平面（与飞船同一高度）
        Plane groundPlane = new Plane(Vector3.up, new Vector3(0, lockedPosition.y, 0));

        float distance;
        if (groundPlane.Raycast(ray, out distance))
        {
            return ray.GetPoint(distance);
        }

        return Vector3.positiveInfinity; // 无效位置
    }

    /// <summary>
    /// 新增：强制锁定位置（可用于外部调用）
    /// </summary>
    public void ForceLockPosition()
    {
        lockedPosition = transform.position;
        InitializePositionLock();
    }

    /// <summary>
    /// 新增：获取当前锁定状态
    /// </summary>
    public bool IsPositionLocked()
    {
        return isPositionLocked;
    }

    /// <summary>
    /// 新增：获取发射状态
    /// </summary>
    public bool HasLaunched()
    {
        return hasLaunched;
    }

    public void ResetLauncher()
    {
        hasLaunched = false;
        isDragging = false;

        // 新增：重置位置锁定
        lockedPosition = startPosition;
        InitializePositionLock();

        if (arrowInstance != null)
        {
            Destroy(arrowInstance);
        }

        // 新增：通知重力系统重置
        if (spaceshipGravity != null)
        {
            spaceshipGravity.SetLaunched(false);
        }

        Debug.Log("发射器已重置，位置重新锁定");
    }

    // 新增：在Scene视图中绘制调试信息
    private void OnDrawGizmos()
    {
        if (Application.isPlaying && isPositionLocked)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(lockedPosition, 0.5f);
            Gizmos.DrawLine(lockedPosition, lockedPosition + Vector3.up * 2f);
        }
    }
}