using UnityEngine;

public class RotateLocalY : MonoBehaviour
{
    [Header("旋转设置")]
    [Tooltip("旋转总时间（秒）")]
    [SerializeField] private float rotationDuration = 2f;

    [Tooltip("目标旋转角度（绕本地Y轴）")]
    [SerializeField] private float targetAngle = 90f;

    [Header("状态")]
    [Tooltip("是否开始旋转")]
    [SerializeField] private bool startRotating = false;

    private float currentRotationTime = 0f;
    private float startAngle = 0f;
    private bool isRotating = false;

    void Start()
    {
        // 初始化起始角度为当前本地Y轴旋转角度
        startAngle = transform.localEulerAngles.y;
    }

    void Update()
    {
        if (!isRotating && startRotating)
        {
            StartRotation();
        }

        if (isRotating)
        {
            PerformRotation();
        }
    }

    /// <summary>
    /// 开始旋转
    /// </summary>
    public void StartRotation()
    {
        isRotating = true;
        currentRotationTime = 0f;
        startAngle = transform.localEulerAngles.y;
    }

    /// <summary>
    /// 执行旋转
    /// </summary>
    private void PerformRotation()
    {
        // 更新时间
        currentRotationTime += Time.deltaTime;

        // 计算插值比例
        float t = Mathf.Clamp01(currentRotationTime / rotationDuration);

        // 使用线性插值计算当前角度
        float currentAngle = Mathf.Lerp(startAngle, startAngle + targetAngle, t);

        // 只改变local Y值，保持X和Z不变
        Vector3 currentEulerAngles = transform.localEulerAngles;
        currentEulerAngles.y = currentAngle;
        transform.localEulerAngles = currentEulerAngles;

        // 旋转完成
        if (currentRotationTime >= rotationDuration)
        {
            isRotating = false;
            startRotating = false;

            // 确保最终角度精确
            Vector3 finalAngles = transform.localEulerAngles;
            finalAngles.y = startAngle + targetAngle;
            transform.localEulerAngles = finalAngles;
        }
    }

    /// <summary>
    /// 从外部触发旋转
    /// </summary>
    /// <param name="duration">旋转时间（秒）</param>
    /// <param name="angle">旋转角度（度）</param>
    public void Rotate(float duration, float angle)
    {
        rotationDuration = duration;
        targetAngle = angle;
        startRotating = true;
    }

    /// <summary>
    /// 停止旋转
    /// </summary>
    public void StopRotation()
    {
        isRotating = false;
        startRotating = false;
    }

    /// <summary>
    /// 重置旋转到起始状态
    /// </summary>
    public void ResetRotation()
    {
        StopRotation();
        Vector3 angles = transform.localEulerAngles;
        angles.y = startAngle;
        transform.localEulerAngles = angles;
    }
}