using UnityEngine;

/// <summary>
/// 飞船位置平滑插值器
/// 用于在时停时保持飞船视觉上的流畅移动，即使物理更新变慢
/// </summary>
public class ShipPositionSmoother : MonoBehaviour
{
    [Header("平滑设置")]
    [Tooltip("是否启用位置平滑（时停时建议开启）")]
    [SerializeField] private bool enableSmoothing = true;

    [Tooltip("平滑速度（值越大越平滑，但可能产生延迟）")]
    [SerializeField] private float smoothSpeed = 20f;

    [Tooltip("最小距离阈值（距离小于此值时不进行插值，避免抖动）")]
    [SerializeField] private float minDistanceThreshold = 0.001f;

    [Tooltip("最大插值距离（距离超过此值时立即跳转，避免延迟过大）")]
    [SerializeField] private float maxInterpolationDistance = 10f;

    [Header("时停检测")]
    [Tooltip("时停时间缩放阈值（低于此值认为处于时停状态）")]
    [SerializeField] private float timeStopThreshold = 0.5f;

    [Tooltip("时停时的平滑速度倍数（时停时使用更快的插值）")]
    [SerializeField] private float timeStopSmoothMultiplier = 2f;

    // 内部状态
    private Vector3 targetPosition; // 目标位置（物理引擎计算的位置）
    private Vector3 smoothedPosition; // 平滑后的位置
    private Vector3 lastTargetPosition; // 上一帧的目标位置（用于计算速度）
    private Vector3 predictedVelocity; // 预测的速度
    private bool isInitialized = false;
    private ShipState shipState;
    private NBody nBody;
    private GravityEngine gravityEngine;

    void Awake()
    {
        shipState = GetComponent<ShipState>();
        nBody = GetComponent<NBody>();
    }

    void Start()
    {
        gravityEngine = GravityEngine.instance;
        
        // 初始化位置
        smoothedPosition = transform.position;
        targetPosition = transform.position;
        lastTargetPosition = transform.position;
        predictedVelocity = Vector3.zero;
        isInitialized = true;
    }

    void LateUpdate()
    {
        if (!enableSmoothing || !isInitialized)
        {
            return;
        }

        // 只在 Flying 或 Captured 状态下进行平滑
        if (shipState == null || 
            (shipState.CurrentState != ShipState.State.Flying && 
             shipState.CurrentState != ShipState.State.Captured))
        {
            // 非飞行状态，直接同步位置
            smoothedPosition = transform.position;
            targetPosition = transform.position;
            return;
        }

        // 获取物理引擎计算的目标位置
        float deltaTime = Time.unscaledDeltaTime; // 统一使用未缩放时间
        
        if (nBody != null && gravityEngine != null && nBody.engineRef != null)
        {
            // GravityEngine 已经更新了 transform.position，这就是目标位置
            targetPosition = transform.position;
            
            // 计算速度（用于预测）
            if (deltaTime > 0.0001f)
            {
                predictedVelocity = (targetPosition - lastTargetPosition) / deltaTime;
            }
            lastTargetPosition = targetPosition;
        }
        else
        {
            targetPosition = transform.position;
            predictedVelocity = Vector3.zero;
        }

        // 计算距离
        float distance = Vector3.Distance(smoothedPosition, targetPosition);

        // 如果距离太大，立即跳转（避免延迟过大）
        if (distance > maxInterpolationDistance)
        {
            smoothedPosition = targetPosition;
            transform.position = smoothedPosition;
            return;
        }

        // 如果距离很小，不进行插值（避免抖动）
        if (distance < minDistanceThreshold)
        {
            smoothedPosition = targetPosition;
            transform.position = smoothedPosition;
            return;
        }

        // 根据时停状态调整平滑速度
        float currentSmoothSpeed = smoothSpeed;
        if (Time.timeScale < timeStopThreshold)
        {
            // 时停时使用更快的插值速度
            currentSmoothSpeed = smoothSpeed * timeStopSmoothMultiplier;
        }

        // 使用未缩放时间进行插值，确保时停时也能平滑
        
        // 使用速度预测来改善平滑效果（在时停时特别有用）
        Vector3 predictedTarget = targetPosition;
        if (Time.timeScale < timeStopThreshold && predictedVelocity.magnitude > 0.01f)
        {
            // 时停时，使用速度预测下一帧的位置，让移动更流畅
            predictedTarget = targetPosition + predictedVelocity * deltaTime * 0.5f;
        }
        
        // 平滑插值到目标位置（或预测位置）
        smoothedPosition = Vector3.Lerp(smoothedPosition, predictedTarget, currentSmoothSpeed * deltaTime);

        // 应用平滑后的位置
        transform.position = smoothedPosition;
    }

    /// <summary>
    /// 立即同步到目标位置（用于状态切换时）
    /// </summary>
    public void SnapToTarget()
    {
        smoothedPosition = transform.position;
        targetPosition = transform.position;
        lastTargetPosition = transform.position;
        predictedVelocity = Vector3.zero;
    }

    /// <summary>
    /// 设置是否启用平滑
    /// </summary>
    public void SetSmoothingEnabled(bool enabled)
    {
        enableSmoothing = enabled;
        if (!enabled)
        {
            SnapToTarget();
        }
    }
}

