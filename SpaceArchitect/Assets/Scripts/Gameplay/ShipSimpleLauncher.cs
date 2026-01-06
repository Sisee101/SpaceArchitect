using UnityEngine;

/// <summary>
/// 简单飞船发射器
/// 按L键发射飞船，使用预设的初始速度
/// </summary>
public class ShipSimpleLauncher : MonoBehaviour
{
    [Header("发射设置")]
    [Tooltip("初始发射速度（世界空间）")]
    [SerializeField] private Vector3 launchVelocity = new Vector3(5f, 0f, 0f);

    [Tooltip("是否使用飞船当前朝向作为发射方向")]
    [SerializeField] private bool useShipForward = false;

    [Tooltip("如果使用飞船朝向，发射速度大小")]
    [SerializeField] private float launchSpeed = 5f;

    [Tooltip("发射键（默认L键）")]
    [SerializeField] private KeyCode launchKey = KeyCode.L;

    [Header("调试")]
    [Tooltip("是否显示调试信息")]
    [SerializeField] private bool showDebugLog = true;

    private ShipState shipState;

    void Awake()
    {
        // 获取ShipState组件
        shipState = GetComponent<ShipState>();
        if (shipState == null)
        {
            Debug.LogError("ShipSimpleLauncher: 缺少 ShipState 组件！");
        }
    }

    void Update()
    {
        // 检查是否按下发射键
        if (Input.GetKeyDown(launchKey))
        {
            LaunchShip();
        }
    }

    /// <summary>
    /// 发射飞船
    /// </summary>
    private void LaunchShip()
    {
        // 检查ShipState组件
        if (shipState == null)
        {
            Debug.LogError("ShipSimpleLauncher: ShipState 组件未找到，无法发射！");
            return;
        }

        // 检查是否可以发射
        if (!shipState.CanLaunch())
        {
            if (showDebugLog)
            {
                Debug.LogWarning($"ShipSimpleLauncher: 飞船当前状态不允许发射（当前状态: {shipState.CurrentState}）");
            }
            return;
        }

        // 计算发射速度
        Vector3 velocity = CalculateLaunchVelocity();

        // 调用ShipState.Launch()发射飞船
        // 这会自动执行以下步骤：
        // 1. 设置 NBody.vel = velocity
        // 2. 切换到 Flying 状态
        // 3. 将飞船添加到 GravityEngine
        // 4. 延迟一帧后通过 GravityEngine.SetVelocity() 确保速度正确应用
        shipState.Launch(velocity);

        if (showDebugLog)
        {
            Debug.Log($"ShipSimpleLauncher: 飞船已发射！速度: {velocity}, 大小: {velocity.magnitude}");
        }
    }

    /// <summary>
    /// 计算发射速度
    /// </summary>
    /// <returns>发射速度向量</returns>
    private Vector3 CalculateLaunchVelocity()
    {
        if (useShipForward)
        {
            // 使用飞船当前朝向作为发射方向
            Vector3 forward = transform.forward;
            forward.z = 0f; // 确保在XY平面
            forward = forward.normalized;
            return forward * launchSpeed;
        }
        else
        {
            // 使用预设的发射速度
            Vector3 velocity = launchVelocity;
            velocity.z = 0f; // 确保在XY平面
            return velocity;
        }
    }

    /// <summary>
    /// 在编辑器中显示发射方向（Gizmos）
    /// </summary>
    void OnDrawGizmosSelected()
    {
        // 计算发射速度用于可视化
        Vector3 velocity;
        if (Application.isPlaying)
        {
            velocity = CalculateLaunchVelocity();
        }
        else
        {
            // 在编辑器中，使用当前设置计算
            if (useShipForward)
            {
                Vector3 forward = transform.forward;
                forward.z = 0f;
                forward = forward.normalized;
                velocity = forward * launchSpeed;
            }
            else
            {
                velocity = launchVelocity;
                velocity.z = 0f;
            }
        }

        // 绘制发射方向
        Gizmos.color = Color.green;
        Vector3 startPos = transform.position;
        Vector3 endPos = startPos + velocity.normalized * 2f; // 绘制2单位长度的箭头
        Gizmos.DrawLine(startPos, endPos);

        // 绘制箭头头部
        Vector3 arrowDir = (endPos - startPos).normalized;
        Vector3 arrowRight = Vector3.Cross(arrowDir, Vector3.forward) * 0.3f;
        Gizmos.DrawLine(endPos, endPos - arrowDir * 0.5f + arrowRight);
        Gizmos.DrawLine(endPos, endPos - arrowDir * 0.5f - arrowRight);
    }
}


