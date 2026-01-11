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
    private ShipShieldSkill shieldSkill; // 防撞技能组件引用

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

        // 获取防撞技能组件（可选，如果没有也不报错）
        shieldSkill = GetComponent<ShipShieldSkill>();
    }

    void Start()
    {
        // 确保碰撞体配置正确
        SetupCollider();
        
        // 确保Rigidbody配置正确（用于碰撞检测）
        SetupRigidbody();
    }

    /// <summary>
    /// 配置碰撞体
    /// </summary>
    private void SetupCollider()
    {
        if (capsuleCollider == null) return;

        // 确保碰撞体不是触发器（需要真实物理碰撞）
        capsuleCollider.isTrigger = false;
    }

    /// <summary>
    /// 配置Rigidbody（确保能够检测碰撞）
    /// </summary>
    private void SetupRigidbody()
    {
        if (rb == null) return;

        // 确保Rigidbody配置正确以支持物理碰撞检测
        rb.useGravity = false;
        rb.collisionDetectionMode = CollisionDetectionMode.Continuous;
        
        // 注意：isKinematic会在ShipState中根据状态设置
        // 在Flying状态下，ShipState会设置为非Kinematic以支持碰撞检测
    }

    /// <summary>
    /// Unity物理碰撞检测（OnCollisionEnter）
    /// 当两个非触发器的Collider发生真实物理碰撞时调用
    /// 这是唯一可靠的碰撞检测方法
    /// </summary>
    void OnCollisionEnter(Collision collision)
    {
        if (showDebugLogs)
        {
            Debug.Log($"[Crash] OnCollisionEnter 被调用！碰撞对象: {collision.gameObject.name}, Tag: {collision.gameObject.tag}, 接触点数: {collision.contactCount}");
        }

        // 如果已经坠毁，不再处理
        if (hasCrashed)
        {
            if (showDebugLogs)
            {
                Debug.Log($"[Crash] 已坠毁，忽略碰撞");
            }
            return;
        }

        // 验证碰撞信息有效性
        if (collision == null || collision.gameObject == null || collision.contactCount == 0)
        {
            if (showDebugLogs)
            {
                Debug.LogWarning($"[Crash] 碰撞信息无效，忽略");
            }
            return;
        }

        // 检查碰撞对象的标签
        GameObject otherObject = collision.gameObject;
        string otherTag = otherObject.tag;

        if (showDebugLogs)
        {
            Debug.Log($"[Crash] 碰撞对象标签: {otherTag}");
        }

        // 检查是否是Destination（成功）
        if (otherTag == "Destination")
        {
            HandleDestinationReached(collision);
            return;
        }

        // 检查防撞技能：如果技能激活且碰撞对象是小行星带，则忽略碰撞
        if (shieldSkill != null && shieldSkill.CanIgnoreCollision(otherTag))
        {
            if (showDebugLogs)
            {
                Debug.Log($"[Crash] 防撞技能激活，忽略小行星带碰撞（Tag: {otherTag}）");
            }
            return; // 忽略碰撞，不触发坠毁
        }

        // 检查是否是Planet或Obstacle（失败）
        if (otherTag == "Planet" || otherTag == "Obstacle")
        {
            HandleCrash(collision);
        }
        else
        {
            if (showDebugLogs)
            {
                Debug.Log($"[Crash] 碰撞对象标签不是Planet或Obstacle，忽略碰撞");
            }
        }
    }

    /// <summary>
    /// 处理碰撞（仅在真实物理碰撞时调用）
    /// </summary>
    /// <param name="collision">碰撞信息</param>
    private void HandleCrash(Collision collision)
    {
        // 验证碰撞信息
        if (collision == null || collision.gameObject == null || collision.contactCount == 0)
        {
            if (showDebugLogs)
            {
                Debug.LogWarning($"[Crash] HandleCrash: 碰撞信息无效");
            }
            return;
        }

        if (hasCrashed || shipState == null)
        {
            if (showDebugLogs)
            {
                Debug.Log($"[Crash] HandleCrash: 已坠毁或shipState为null，忽略");
            }
            return;
        }

        // 检查是否在Flying或Captured状态（只有这些状态下才会碰撞）
        if (shipState.CurrentState != ShipState.State.Flying && 
            shipState.CurrentState != ShipState.State.Captured)
        {
            if (showDebugLogs)
            {
                Debug.Log($"[Crash] HandleCrash: 飞船不在Flying或Captured状态（当前状态: {shipState.CurrentState}），忽略碰撞");
            }
            return;
        }

        // 验证碰撞体是否真的接触（额外安全检查）
        if (!ValidateCollision(collision))
        {
            if (showDebugLogs)
            {
                Debug.LogWarning($"[Crash] HandleCrash: 碰撞验证失败，可能是误检测");
            }
            return;
        }

        // 标记为已坠毁（防止重复触发）
        hasCrashed = true;

        if (showDebugLogs)
        {
            Debug.Log($"<color=red>[Crash] 飞船真实碰撞！碰撞对象: {collision.gameObject.name}, 标签: {collision.gameObject.tag}, 接触点: {collision.contacts[0].point}</color>");
        }

        // 切换到Crashed状态
        shipState.SetState(ShipState.State.Crashed);

        // 计算碰撞点
        Vector3 contactPoint = collision.contacts[0].point;

        // 通过EventManager触发碰撞事件
        if (EventManager.Instance != null)
        {
            EventManager.Instance.TriggerShipCrashed(collision.gameObject, contactPoint, gameObject);
        }

        // 处理物理碰撞效果
        HandlePhysicsCollision(collision);

        // 播放效果
        PlayCrashEffects(collision);
    }

    /// <summary>
    /// 验证碰撞是否真实（额外安全检查）
    /// </summary>
    private bool ValidateCollision(Collision collision)
    {
        if (collision == null || collision.contactCount == 0)
        {
            return false;
        }

        // 检查接触点是否有效
        ContactPoint firstContact = collision.contacts[0];
        if (firstContact.point == Vector3.zero && collision.contactCount == 1)
        {
            // 如果只有一个接触点且为原点，可能是无效碰撞
            return false;
        }

        // 检查碰撞对象是否有有效的Collider
        Collider otherCollider = collision.collider;
        if (otherCollider == null || !otherCollider.enabled)
        {
            return false;
        }

        // 检查碰撞对象是否有Rigidbody（虽然不是必须的，但有助于验证）
        // 注意：静态物体也可以有Collider但没有Rigidbody

        // 检查距离是否合理（接触点应该在两个碰撞体之间）
        float distanceToContact = Vector3.Distance(transform.position, firstContact.point);
        float distanceToOther = Vector3.Distance(transform.position, collision.gameObject.transform.position);
        
        // 如果接触点距离飞船太远，可能是误检测
        if (distanceToContact > distanceToOther * 1.5f)
        {
            if (showDebugLogs)
            {
                Debug.LogWarning($"[Crash] 碰撞验证失败：接触点距离异常 (接触点距离: {distanceToContact:F2}, 对象距离: {distanceToOther:F2})");
            }
            return false;
        }

        return true;
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
    /// 处理到达目的地
    /// </summary>
    /// <param name="collision">碰撞信息</param>
    private void HandleDestinationReached(Collision collision)
    {
        if (shipState == null)
        {
            return;
        }

        // 检查是否在Flying或Captured状态（只有这些状态下才能成功）
        if (shipState.CurrentState != ShipState.State.Flying && 
            shipState.CurrentState != ShipState.State.Captured)
        {
            return;
        }

        // 计算碰撞点
        Vector3 contactPoint = collision.contacts[0].point;

        if (showDebugLogs)
        {
            Debug.Log($"飞船到达目的地！目的地: {collision.gameObject.name}");
        }

        // 通过EventManager触发成功事件
        if (EventManager.Instance != null)
        {
            EventManager.Instance.TriggerShipSucceed(collision.gameObject, gameObject);
        }

        // 可以在这里添加成功效果（如粒子、音效等）
    }


    /// <summary>
    /// 检查是否已坠毁
    /// </summary>
    public bool IsCrashed()
    {
        return hasCrashed;
    }
}
