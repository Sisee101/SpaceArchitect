using UnityEngine;

/// <summary>
/// 飞船轨道状态组件，用于标记飞船的轨道状态
/// </summary>
public class SpaceshipOrbitState : MonoBehaviour
{
    private bool isOrbiting = false;
    private OrbitCalculator currentOrbit = null;

    // 新增：位置锁定相关状态
    private bool isPositionLocked = false;
    private SpaceshipLauncher launcher;

    void Start()
    {
        // 获取发射器组件
        launcher = GetComponent<SpaceshipLauncher>();
        if (launcher == null)
        {
            Debug.LogWarning("SpaceshipOrbitState: 未找到SpaceshipLauncher组件！");
        }
    }

    void Update()
    {
        // 新增：如果处于轨道状态，确保Y轴位置稳定
        if (isOrbiting && currentOrbit != null)
        {
            MaintainOrbitalStability();
        }
    }

    public void SetOrbiting(bool orbiting, OrbitCalculator orbit)
    {
        isOrbiting = orbiting;
        currentOrbit = orbit;
    }

    public bool IsOrbiting()
    {
        return isOrbiting;
    }

    public OrbitCalculator GetCurrentOrbit()
    {
        return currentOrbit;
    }

    /// <summary>
    /// 修复：添加缺失的方法 - 维持轨道稳定性
    /// </summary>
    private void MaintainOrbitalStability()
    {
        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb != null)
        {
            // 确保Y轴速度为零
            if (Mathf.Abs(rb.velocity.y) > 0.1f)
            {
                Vector3 horizontalVelocity = new Vector3(rb.velocity.x, 0f, rb.velocity.z);
                rb.velocity = horizontalVelocity;
            }

            // 限制Y轴位置漂移
            Vector3 currentPosition = transform.position;
            if (Mathf.Abs(currentPosition.y - rb.position.y) > 0.5f)
            {
                transform.position = new Vector3(currentPosition.x, rb.position.y, currentPosition.z);
            }
        }
    }

    /// <summary>
    /// 新增：获取位置锁定状态
    /// </summary>
    public bool IsPositionLocked()
    {
        if (launcher != null)
        {
            // 修复：调用正确的方法名
            return launcher.IsPositionLocked();
        }
        return isPositionLocked;
    }
}