using UnityEngine;

public enum RotationAxis
{
    X,
    Y,
    Z,
    Custom
}

public class AxisRotateObject : MonoBehaviour
{
    [Header("旋转设置")]
    [Tooltip("旋转持续时间（秒）")]
    public float rotationDuration = 5f;

    [Tooltip("旋转方向")]
    public bool clockwise = true;

    [Header("旋转轴选择")]
    [Tooltip("选择旋转轴")]
    public RotationAxis rotationAxis = RotationAxis.Y;

    [Tooltip("自定义旋转轴（当选择Custom时生效）")]
    public Vector3 customAxis = Vector3.up;

    [Header("旋转控制")]
    [Tooltip("是否自动开始旋转")]
    public bool autoStart = true;

    [Tooltip("是否循环旋转")]
    public bool loopRotation = false;

    // 私有变量
    private Quaternion startRotation;
    private float elapsedTime = 0f;
    private bool isRotating = false;
    private Vector3 currentAxis;

    void Start()
    {
        UpdateRotationAxis();

        if (autoStart)
        {
            StartRotation();
        }
    }

    void Update()
    {
        if (isRotating)
        {
            elapsedTime += Time.deltaTime;

            if (elapsedTime <= rotationDuration)
            {
                // 计算旋转进度
                float progress = elapsedTime / rotationDuration;

                // 计算目标角度
                float targetAngle = clockwise ? 360f : -360f;

                // 应用旋转
                float currentAngle = Mathf.Lerp(0f, targetAngle, progress);
                transform.rotation = startRotation * Quaternion.AngleAxis(currentAngle, currentAxis);
            }
            else
            {
                // 旋转完成
                if (loopRotation)
                {
                    // 重置并继续旋转
                    ResetRotation();
                }
                else
                {
                    StopRotation();
                    Debug.Log("旋转完成！");
                }
            }
        }
    }

    // 更新旋转轴
    void UpdateRotationAxis()
    {
        switch (rotationAxis)
        {
            case RotationAxis.X:
                currentAxis = Vector3.right;
                break;
            case RotationAxis.Y:
                currentAxis = Vector3.up;
                break;
            case RotationAxis.Z:
                currentAxis = Vector3.forward;
                break;
            case RotationAxis.Custom:
                currentAxis = customAxis.normalized;
                break;
        }
    }

    // 开始旋转
    public void StartRotation()
    {
        startRotation = transform.rotation;
        elapsedTime = 0f;
        isRotating = true;
        UpdateRotationAxis();

        Debug.Log($"开始绕 {currentAxis} 轴旋转");
    }

    // 停止旋转
    public void StopRotation()
    {
        isRotating = false;
    }

    // 重置旋转
    public void ResetRotation()
    {
        transform.rotation = startRotation;
        elapsedTime = 0f;
    }

    // 切换旋转方向
    public void ToggleDirection()
    {
        clockwise = !clockwise;
        StartRotation();
    }

    // 设置自定义轴（通过代码）
    public void SetCustomAxis(Vector3 axis)
    {
        customAxis = axis.normalized;
        if (rotationAxis == RotationAxis.Custom)
        {
            UpdateRotationAxis();
        }
    }

    // 在Inspector中自定义轴改变时调用
    void OnValidate()
    {
        UpdateRotationAxis();
    }

    // 调试：显示旋转轴
    void OnDrawGizmosSelected()
    {
        if (!Application.isPlaying)
        {
            UpdateRotationAxis();
        }

        Gizmos.color = Color.red;
        Gizmos.DrawLine(transform.position, transform.position + currentAxis * 2f);
        Gizmos.DrawSphere(transform.position + currentAxis * 2f, 0.1f);
    }
}