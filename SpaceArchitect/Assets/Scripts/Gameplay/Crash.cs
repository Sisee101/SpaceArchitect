using UnityEngine;

/// <summary>
/// 碰撞检测脚本
/// 检测飞船与Planet或Obstacle的碰撞，并切换到Crashed状态
/// </summary>
[RequireComponent(typeof(CapsuleCollider))]
public class Crash : MonoBehaviour
{
    [Header("碰撞设置")]
    [Tooltip("碰撞时是否播放音效")]
    [SerializeField] private bool playCrashSound = true;

    [Tooltip("碰撞时的粒子效果Prefab（可选）")]
    [SerializeField] private GameObject crashParticlesPrefab;

    [Tooltip("碰撞音效（可选）")]
    [SerializeField] private AudioSource crashSound;

    [Header("调试")]
    [Tooltip("显示碰撞调试信息")]
    [SerializeField] private bool showDebugLogs = true;

    private ShipState shipState;
    private CapsuleCollider capsuleCollider;
    private Rigidbody rb;
    private bool hasCrashed = false;

    void Awake()
    {
        // 获取组件
        shipState = GetComponent<ShipState>();
        if (shipState == null)
        {
            Debug.LogError($"Crash: {gameObject.name} 缺少 ShipState 组件！");
        }

        capsuleCollider = GetComponent<CapsuleCollider>();
        if (capsuleCollider == null)
        {
            Debug.LogError($"Crash: {gameObject.name} 缺少 CapsuleCollider 组件！");
        }

        rb = GetComponent<Rigidbody>();
        if (rb == null)
        {
            Debug.LogError($"Crash: {gameObject.name} 缺少 Rigidbody 组件！");
        }
    }

    void Start()
    {
        // 确保碰撞体配置正确
        SetupCollider();
    }

    /// <summary>
    /// 配置碰撞体
    /// </summary>
    private void SetupCollider()
    {
        if (capsuleCollider == null) return;

        // 确保碰撞体不是触发器（需要真实物理碰撞）
        capsuleCollider.isTrigger = false;

        // 确保Rigidbody配置正确
        if (rb != null)
        {
            // 在Flying状态下，Rigidbody应该是Kinematic（由GravityEngine控制位置）
            // 但在碰撞时，我们需要让它能够进行物理碰撞
            // 注意：GravityEngine通常不使用Unity的Rigidbody，所以这里可能需要特殊处理
        }
    }

    /// <summary>
    /// Unity物理碰撞检测（OnCollisionEnter）
    /// 当两个非触发器的Collider发生碰撞时调用
    /// </summary>
    void OnCollisionEnter(Collision collision)
    {
        // 如果已经坠毁，不再处理
        if (hasCrashed)
        {
            return;
        }

        // 检查碰撞对象的标签
        GameObject otherObject = collision.gameObject;
        string otherTag = otherObject.tag;

        // 检查是否是Planet或Obstacle
        if (otherTag == "Planet" || otherTag == "Obstacle")
        {
            HandleCrash(collision);
        }
    }

    /// <summary>
    /// 处理碰撞
    /// </summary>
    /// <param name="collision">碰撞信息</param>
    private void HandleCrash(Collision collision)
    {
        if (hasCrashed || shipState == null)
        {
            return;
        }

        // 检查是否在Flying状态（只有Flying状态下才会碰撞）
        if (shipState.CurrentState != ShipState.State.Flying)
        {
            return;
        }

        // 标记为已坠毁
        hasCrashed = true;

        if (showDebugLogs)
        {
            Debug.Log($"飞船碰撞！碰撞对象: {collision.gameObject.name}, 标签: {collision.gameObject.tag}");
        }

        // 切换到Crashed状态
        shipState.SetState(ShipState.State.Crashed);

        // 处理物理碰撞效果
        HandlePhysicsCollision(collision);

        // 播放效果
        PlayCrashEffects(collision);
    }

    /// <summary>
    /// 处理物理碰撞效果
    /// </summary>
    /// <param name="collision">碰撞信息</param>
    private void HandlePhysicsCollision(Collision collision)
    {
        if (rb == null) return;

        // 获取碰撞时的速度（从GravityEngine）
        GravityEngine gravityEngine = GravityEngine.instance;
        NBody nBody = GetComponent<NBody>();

        Vector3 crashVelocity = Vector3.zero;
        if (gravityEngine != null && nBody != null && nBody.engineRef != null)
        {
            // 获取碰撞前的速度
            crashVelocity = gravityEngine.GetVelocity(nBody);
        }

        // 如果GravityEngine没有速度，尝试从Rigidbody获取
        if (crashVelocity.magnitude < 0.1f && rb != null)
        {
            crashVelocity = rb.velocity;
        }

        // 计算碰撞点
        Vector3 contactPoint = collision.contacts[0].point;
        Vector3 contactNormal = collision.contacts[0].normal;

        // 计算反弹速度（可选，根据需求调整）
        // 这里我们让飞船在碰撞后停止，或者根据碰撞法线反弹
        float bounceFactor = 0.3f; // 反弹系数（0-1，0表示完全停止，1表示完全反弹）
        Vector3 bounceVelocity = Vector3.Reflect(crashVelocity, contactNormal) * bounceFactor;

        // 将飞船从GravityEngine控制切换到Unity物理引擎控制
        // 注意：这需要在ShipState的Crashed状态处理中完成
        // 这里我们只是确保Rigidbody可以响应物理碰撞

        if (showDebugLogs)
        {
            Debug.Log($"碰撞速度: {crashVelocity.magnitude:F2}, 碰撞点: {contactPoint}, 法线: {contactNormal}");
        }
    }

    /// <summary>
    /// 播放碰撞效果
    /// </summary>
    /// <param name="collision">碰撞信息</param>
    private void PlayCrashEffects(Collision collision)
    {
        // 播放粒子效果
        if (crashParticlesPrefab != null)
        {
            // 在碰撞点实例化并播放粒子效果
            Vector3 contactPoint = collision.contacts[0].point;
            GameObject particlesInstance = Instantiate(crashParticlesPrefab, contactPoint, Quaternion.identity);
            
            // 获取粒子系统组件并播放
            ParticleSystem particleSystem = particlesInstance.GetComponent<ParticleSystem>();
            if (particleSystem != null)
            {
                particleSystem.Play();
            }
            else
            {
                // 如果根对象没有ParticleSystem，尝试在子对象中查找
                particleSystem = particlesInstance.GetComponentInChildren<ParticleSystem>();
                if (particleSystem != null)
                {
                    particleSystem.Play();
                }
                else if (showDebugLogs)
                {
                    Debug.LogWarning($"Crash: 粒子效果Prefab {crashParticlesPrefab.name} 中没有找到ParticleSystem组件！");
                }
            }
            
            // 如果粒子系统播放完成后会自动销毁，则不需要手动销毁
            // 否则可以在粒子播放完成后销毁实例（通过ParticleSystem的duration判断）
            if (particleSystem != null && !particleSystem.main.loop)
            {
                // 粒子播放完成后自动销毁
                Destroy(particlesInstance, particleSystem.main.duration + particleSystem.main.startLifetime.constantMax);
            }
        }

        // 播放音效
        if (playCrashSound && crashSound != null)
        {
            crashSound.Play();
        }
    }

    /// <summary>
    /// 重置碰撞状态（用于重新开始游戏）
    /// </summary>
    public void ResetCrashState()
    {
        hasCrashed = false;
    }

    /// <summary>
    /// 检查是否已坠毁
    /// </summary>
    public bool IsCrashed()
    {
        return hasCrashed;
    }
}
