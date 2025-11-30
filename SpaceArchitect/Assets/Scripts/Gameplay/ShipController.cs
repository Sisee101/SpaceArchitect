using UnityEngine;

/// <summary>
/// 飞船控制器
/// 控制飞船在Flying状态下的旋转，使船头始终指向速度方向
/// </summary>
public class ShipController : MonoBehaviour
{
    [Header("旋转设置")]
    [Tooltip("旋转平滑速度（度/秒），0表示立即旋转")]
    [SerializeField] private float rotationSpeed = 360f;

    [Tooltip("最小速度阈值，低于此速度时不旋转（避免抖动）")]
    [SerializeField] private float minVelocityThreshold = 0.1f;

    [Tooltip("船头方向偏移（度）。如果船头默认指向Y轴正方向，设置为-90；如果指向X轴正方向，设置为0")]
    [SerializeField] private float shipForwardOffset = -90f;

    private ShipState shipState;
    private NBody nBody;
    private GravityEngine gravityEngine;
    private Vector3 lastVelocity = Vector3.zero;

    void Awake()
    {
        // 获取组件
        shipState = GetComponent<ShipState>();
        if (shipState == null)
        {
            Debug.LogError($"ShipController: {gameObject.name} 缺少 ShipState 组件！");
        }

        nBody = GetComponent<NBody>();
        if (nBody == null)
        {
            Debug.LogError($"ShipController: {gameObject.name} 缺少 NBody 组件！");
        }
    }

    void Start()
    {
        // 获取GravityEngine实例
        gravityEngine = GravityEngine.instance;
        if (gravityEngine == null)
        {
            Debug.LogError("ShipController: 场景中没有找到 GravityEngine！");
        }
    }

    void LateUpdate()
    {
        // 只在Flying状态下更新旋转
        if (shipState == null || shipState.CurrentState != ShipState.State.Flying)
        {
            return;
        }

        // 检查必要组件
        if (nBody == null || gravityEngine == null)
        {
            return;
        }

        // 检查NBody是否已添加到引擎
        if (nBody.engineRef == null)
        {
            return;
        }

        // 获取当前速度
        Vector3 currentVelocity = gravityEngine.GetVelocity(nBody);

        // 检查速度是否足够大（避免在静止或极慢时抖动）
        if (currentVelocity.magnitude < minVelocityThreshold)
        {
            return;
        }

        // 更新旋转
        UpdateRotation(currentVelocity);
    }

    /// <summary>
    /// 更新飞船旋转，使船头指向速度方向
    /// </summary>
    /// <param name="velocity">当前速度向量</param>
    private void UpdateRotation(Vector3 velocity)
    {
        // 确保速度在XY平面（Z=0）
        velocity.z = 0f;

        // 如果速度为零或太小，不旋转
        if (velocity.magnitude < minVelocityThreshold)
        {
            return;
        }

        // 计算目标旋转
        // 在2D游戏中，使用角度计算更简单直接
        // Atan2返回的是从X轴正方向到向量的角度（弧度）
        float angle = Mathf.Atan2(velocity.y, velocity.x) * Mathf.Rad2Deg;
        
        // 应用船头方向偏移
        // 例如：如果船头默认指向Y轴正方向（向上），需要减去90度
        angle += shipForwardOffset;
        
        // 转换为Quaternion（2D游戏使用Z轴旋转）
        Quaternion targetRotation = Quaternion.Euler(0, 0, angle);

        // 根据是否有旋转速度设置，选择立即旋转或平滑旋转
        if (rotationSpeed > 0f)
        {
            // 平滑旋转
            transform.rotation = Quaternion.RotateTowards(
                transform.rotation,
                targetRotation,
                rotationSpeed * Time.deltaTime
            );
        }
        else
        {
            // 立即旋转
            transform.rotation = targetRotation;
        }

        // 保存当前速度供下次使用
        lastVelocity = velocity;
    }

    /// <summary>
    /// 获取当前速度（用于调试）
    /// </summary>
    public Vector3 GetCurrentVelocity()
    {
        if (nBody != null && gravityEngine != null && nBody.engineRef != null)
        {
            return gravityEngine.GetVelocity(nBody);
        }
        return Vector3.zero;
    }

    /// <summary>
    /// 获取当前速度大小
    /// </summary>
    public float GetCurrentSpeed()
    {
        return GetCurrentVelocity().magnitude;
    }
}

