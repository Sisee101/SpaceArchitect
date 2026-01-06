using UnityEngine;

public class MoveOnXAxis : MonoBehaviour
{
    [Header("移动设置")]
    [Tooltip("移动速度（正数向右，负数向左）")]
    public float moveSpeed = 5f;

    [Tooltip("是否在开始时自动移动")]
    public bool autoStart = true;

    [Header("边界限制（可选）")]
    [Tooltip("是否启用边界限制")]
    public bool useBoundary = false;

    [Tooltip("左边界")]
    public float leftBoundary = -10f;

    [Tooltip("右边界")]
    public float rightBoundary = 10f;

    private bool isMoving = true;
    private Vector3 startPosition;

    void Start()
    {
        startPosition = transform.position;
        isMoving = autoStart;
    }

    void Update()
    {
        if (!isMoving) return;

        MoveObject();
    }

    void MoveObject()
    {
        // 计算移动距离
        float moveDistance = moveSpeed * Time.deltaTime;

        // 计算新位置
        Vector3 newPosition = transform.position + new Vector3(moveDistance, 0f, 0f);

        // 检查边界
        if (useBoundary)
        {
            newPosition.x = Mathf.Clamp(newPosition.x, leftBoundary, rightBoundary);

            // 如果到达边界，自动反向
            if (newPosition.x == leftBoundary || newPosition.x == rightBoundary)
            {
                moveSpeed = -moveSpeed;
            }
        }

        // 应用移动
        transform.position = newPosition;
    }

    // 开始移动
    public void StartMoving()
    {
        isMoving = true;
    }

    // 停止移动
    public void StopMoving()
    {
        isMoving = false;
    }

    // 切换移动状态
    public void ToggleMoving()
    {
        isMoving = !isMoving;
    }

    // 设置移动速度
    public void SetSpeed(float speed)
    {
        moveSpeed = speed;
    }

    // 反转移动方向
    public void ReverseDirection()
    {
        moveSpeed = -moveSpeed;
    }

    // 重置位置
    public void ResetPosition()
    {
        transform.position = startPosition;
    }
}