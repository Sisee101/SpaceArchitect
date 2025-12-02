using System.Collections;
using UnityEngine;

/// <summary>
/// 飞船加速器
/// 按下E键时，飞船获得一个朝当前速度方向的短时加速
/// </summary>
public class ShipBoost : MonoBehaviour
{
    [Header("加速设置")]
    [Tooltip("加速力度（速度增量）")]
    [SerializeField] private float boostForce = 5f;

    [Tooltip("加速持续时间（秒）")]
    [SerializeField] private float boostDuration = 0.5f;

    [Tooltip("加速冷却时间（秒），0表示无冷却")]
    [SerializeField] private float cooldownTime = 2f;

    [Tooltip("最小速度阈值，低于此速度时无法加速")]
    [SerializeField] private float minVelocityForBoost = 0.5f;

    [Header("视觉效果（可选）")]
    [Tooltip("加速时的粒子效果")]
    [SerializeField] private ParticleSystem boostParticles;

    [Tooltip("加速时的音效")]
    [SerializeField] private AudioSource boostSound;

    [Header("捕获脱离设置")]
    [Tooltip("在Captured状态下，加速方向是否远离最近的捕获行星（如果为false，使用速度方向）")]
    [SerializeField] private bool escapeFromPlanetInCaptured = true;
    
    [Tooltip("捕获脱离时的加速力度倍数（相对于正常加速）")]
    [SerializeField] private float escapeBoostMultiplier = 1.5f;

    // 内部状态
    private ShipState shipState;
    private NBody nBody;
    private GravityEngine gravityEngine;
    private bool isBoosting = false;
    private bool isOnCooldown = false;
    private float cooldownTimer = 0f;
    private PlanetGravityCapture currentCapturingPlanet = null;

    void Awake()
    {
        // 获取组件
        shipState = GetComponent<ShipState>();
        if (shipState == null)
        {
            Debug.LogError($"ShipBoost: {gameObject.name} 缺少 ShipState 组件！");
        }

        nBody = GetComponent<NBody>();
        if (nBody == null)
        {
            Debug.LogError($"ShipBoost: {gameObject.name} 缺少 NBody 组件！");
        }
    }

    void Start()
    {
        // 获取GravityEngine实例
        gravityEngine = GravityEngine.instance;
        if (gravityEngine == null)
        {
            Debug.LogError("ShipBoost: 场景中没有找到 GravityEngine！");
        }
    }

    void Update()
    {
        // 更新冷却时间
        if (isOnCooldown)
        {
            cooldownTimer -= Time.deltaTime;
            if (cooldownTimer <= 0f)
            {
                isOnCooldown = false;
                cooldownTimer = 0f;
            }
        }

        // 检查输入
        if (Input.GetKeyDown(KeyCode.E))
        {
            TryBoost();
        }
    }

    /// <summary>
    /// 尝试执行加速
    /// </summary>
    private void TryBoost()
    {
        // 检查是否在Flying或Captured状态
        if (shipState == null || 
            (shipState.CurrentState != ShipState.State.Flying && 
             shipState.CurrentState != ShipState.State.Captured))
        {
            Debug.Log($"加速失败：飞船不在Flying或Captured状态，当前状态: {shipState?.CurrentState}");
            return;
        }

        // 检查是否正在加速
        if (isBoosting)
        {
            Debug.Log("加速失败：正在加速中");
            return;
        }

        // 检查是否在冷却中
        if (isOnCooldown)
        {
            Debug.Log($"加速失败：冷却中（剩余 {cooldownTimer:F1} 秒）");
            return;
        }

        // 检查必要组件
        if (nBody == null || gravityEngine == null)
        {
            Debug.LogError("加速失败：缺少必要组件");
            return;
        }

        // 检查NBody是否已添加到引擎
        if (nBody.engineRef == null)
        {
            Debug.LogError("加速失败：飞船未添加到引力引擎");
            return;
        }

        // 获取当前速度
        Vector3 currentVelocity = gravityEngine.GetVelocity(nBody);

        // 在Captured状态下，如果速度太小，也允许加速（用于脱离捕获）
        bool isCaptured = shipState.CurrentState == ShipState.State.Captured;
        if (!isCaptured && currentVelocity.magnitude < minVelocityForBoost)
        {
            Debug.Log($"加速失败：速度过小（{currentVelocity.magnitude:F2} < {minVelocityForBoost}）");
            return;
        }

        // 执行加速
        StartBoost(currentVelocity, isCaptured);
    }

    /// <summary>
    /// 开始加速
    /// </summary>
    /// <param name="currentVelocity">当前速度</param>
    /// <param name="isCaptured">是否在捕获状态</param>
    private void StartBoost(Vector3 currentVelocity, bool isCaptured = false)
    {
        isBoosting = true;

        Vector3 boostDirection;
        
        // 在Captured状态下，如果启用逃离行星，计算远离行星的方向
        if (isCaptured && escapeFromPlanetInCaptured && currentCapturingPlanet != null)
        {
            // 计算从行星到飞船的方向（径向向外）
            Vector3 toShip = transform.position - currentCapturingPlanet.transform.position;
            toShip.z = 0f; // 确保在XY平面
            
            if (toShip.magnitude > 0.01f)
            {
                boostDirection = toShip.normalized;
                Debug.Log($"捕获状态加速：使用远离行星方向 {boostDirection}");
            }
            else
            {
                // 如果距离太近，使用速度方向
                boostDirection = currentVelocity.magnitude > 0.01f ? currentVelocity.normalized : Vector3.up;
            }
        }
        else
        {
            // 正常情况：使用速度方向
            boostDirection = currentVelocity.magnitude > 0.01f ? currentVelocity.normalized : Vector3.up;
            boostDirection.z = 0f; // 确保在XY平面
        }

        // 计算加速增量（在Captured状态下使用倍数）
        float actualBoostForce = isCaptured ? boostForce * escapeBoostMultiplier : boostForce;
        Vector3 boostVelocity = boostDirection * actualBoostForce;

        // 应用加速
        ApplyBoost(boostVelocity);

        // 通过EventManager触发加速事件
        if (EventManager.Instance != null)
        {
            EventManager.Instance.TriggerShipBoosted(boostDirection, actualBoostForce, gameObject);
        }

        // 启动加速协程
        StartCoroutine(BoostCoroutine());

        // 播放视觉效果和音效
        PlayBoostEffects();

        Debug.Log($"加速启动！速度增量: {boostVelocity}, 当前速度: {currentVelocity}");
    }

    /// <summary>
    /// 应用加速（直接修改速度）
    /// </summary>
    /// <param name="boostVelocity">加速速度增量</param>
    private void ApplyBoost(Vector3 boostVelocity)
    {
        if (nBody == null || gravityEngine == null || nBody.engineRef == null)
        {
            return;
        }

        // 获取当前速度
        Vector3 currentVelocity = gravityEngine.GetVelocity(nBody);

        // 计算新速度
        Vector3 newVelocity = currentVelocity + boostVelocity;

        // 更新速度
        gravityEngine.SetVelocity(nBody, newVelocity);

        Debug.Log($"速度更新: {currentVelocity} -> {newVelocity}");
    }

    /// <summary>
    /// 加速协程（处理持续时间和冷却）
    /// </summary>
    private IEnumerator BoostCoroutine()
    {
        // 等待加速持续时间
        yield return new WaitForSeconds(boostDuration);

        // 加速结束
        isBoosting = false;

        // 启动冷却
        if (cooldownTime > 0f)
        {
            isOnCooldown = true;
            cooldownTimer = cooldownTime;
        }

        // 停止视觉效果
        StopBoostEffects();

        Debug.Log("加速结束");
    }

    /// <summary>
    /// 播放加速效果
    /// </summary>
    private void PlayBoostEffects()
    {
        // 播放粒子效果
        if (boostParticles != null && !boostParticles.isPlaying)
        {
            boostParticles.Play();
        }

        // 播放音效
        if (boostSound != null && !boostSound.isPlaying)
        {
            boostSound.Play();
        }
    }

    /// <summary>
    /// 停止加速效果
    /// </summary>
    private void StopBoostEffects()
    {
        // 停止粒子效果
        if (boostParticles != null && boostParticles.isPlaying)
        {
            boostParticles.Stop();
        }

        // 停止音效
        if (boostSound != null && boostSound.isPlaying)
        {
            boostSound.Stop();
        }
    }

    /// <summary>
    /// 获取冷却剩余时间（用于UI显示）
    /// </summary>
    public float GetCooldownRemaining()
    {
        return Mathf.Max(0f, cooldownTimer);
    }

    /// <summary>
    /// 获取冷却进度（0-1，用于UI显示）
    /// </summary>
    public float GetCooldownProgress()
    {
        if (cooldownTime <= 0f)
        {
            return 1f; // 无冷却，始终可用
        }
        return 1f - (cooldownTimer / cooldownTime);
    }

    /// <summary>
    /// 设置当前捕获的行星（由PlanetGravityCapture调用）
    /// </summary>
    public void SetCapturingPlanet(PlanetGravityCapture planet)
    {
        currentCapturingPlanet = planet;
    }

    /// <summary>
    /// 清除捕获行星引用
    /// </summary>
    public void ClearCapturingPlanet()
    {
        currentCapturingPlanet = null;
    }

    /// <summary>
    /// 检查是否可以加速
    /// </summary>
    public bool CanBoost()
    {
        if (shipState == null || 
            (shipState.CurrentState != ShipState.State.Flying && 
             shipState.CurrentState != ShipState.State.Captured))
        {
            return false;
        }

        if (isBoosting || isOnCooldown)
        {
            return false;
        }

        if (nBody == null || gravityEngine == null || nBody.engineRef == null)
        {
            return false;
        }

        // 在Captured状态下，即使速度很小也允许加速（用于脱离）
        bool isCaptured = shipState.CurrentState == ShipState.State.Captured;
        if (isCaptured)
        {
            return true; // Captured状态下总是可以尝试加速脱离
        }

        Vector3 currentVelocity = gravityEngine.GetVelocity(nBody);
        return currentVelocity.magnitude >= minVelocityForBoost;
    }

    /// <summary>
    /// 强制停止加速（用于特殊情况）
    /// </summary>
    public void ForceStopBoost()
    {
        if (isBoosting)
        {
            StopCoroutine(BoostCoroutine());
            isBoosting = false;
            StopBoostEffects();
        }
    }
}
