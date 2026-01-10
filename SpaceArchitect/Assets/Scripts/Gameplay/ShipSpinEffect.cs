using System.Collections;
using UnityEngine;

/// <summary>
/// 飞船旋转喷射特效
/// 按下B键时，飞船围绕自身轴心持续旋转并在尾部喷射粒子效果
/// </summary>
public class ShipSpinEffect : MonoBehaviour
{
    [Header("旋转设置")]
    [Tooltip("旋转速度（度/秒）")]
    [SerializeField] private float spinSpeed = 720f;

    [Tooltip("旋转持续时间（秒）")]
    [SerializeField] private float spinDuration = 1.5f;

    [Tooltip("冷却时间（秒），0表示无冷却")]
    [SerializeField] private float cooldownTime = 3f;

    [Tooltip("旋转轴方向（本地空间）。默认为Y轴，即飞船头尾方向")]
    [SerializeField] private Vector3 spinAxis = Vector3.up;

    [Header("粒子效果")]
    [Tooltip("粒子效果预制体（如Calm Fire）")]
    [SerializeField] private GameObject particlePrefab;

    [Tooltip("粒子生成位置偏移（本地空间，用于调整到飞船尾部）")]
    [SerializeField] private Vector3 particleOffset = new Vector3(0f, -1f, 0f);

    [Tooltip("粒子效果是否跟随飞船移动")]
    [SerializeField] private bool particleFollowShip = true;

    [Header("调试")]
    [Tooltip("是否显示调试信息")]
    [SerializeField] private bool showDebugLog = true;

    // 内部状态
    private ShipState shipState;
    private bool isSpinning = false;
    private bool isOnCooldown = false;
    private float cooldownTimer = 0f;
    private GameObject currentParticleInstance;

    void Awake()
    {
        // 获取ShipState组件
        shipState = GetComponent<ShipState>();
        if (shipState == null)
        {
            Debug.LogError($"ShipSpinEffect: {gameObject.name} 缺少 ShipState 组件！");
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
        if (Input.GetKeyDown(KeyCode.B))
        {
            TrySpin();
        }
    }

    /// <summary>
    /// 尝试执行旋转喷射效果
    /// </summary>
    private void TrySpin()
    {
        // 检查是否在Flying状态
        if (shipState == null || shipState.CurrentState != ShipState.State.Flying)
        {
            if (showDebugLog)
            {
                Debug.Log($"旋转失败：飞船不在Flying状态，当前状态: {shipState?.CurrentState}");
            }
            return;
        }

        // 检查是否正在旋转
        if (isSpinning)
        {
            if (showDebugLog)
            {
                Debug.Log("旋转失败：正在旋转中");
            }
            return;
        }

        // 检查是否在冷却中
        if (isOnCooldown)
        {
            if (showDebugLog)
            {
                Debug.Log($"旋转失败：冷却中（剩余 {cooldownTimer:F1} 秒）");
            }
            return;
        }

        // 开始旋转效果
        StartSpin();
    }

    /// <summary>
    /// 开始旋转喷射效果
    /// </summary>
    private void StartSpin()
    {
        isSpinning = true;

        // 生成粒子效果
        SpawnParticle();

        // 启动旋转协程
        StartCoroutine(SpinCoroutine());

        if (showDebugLog)
        {
            Debug.Log($"旋转喷射启动！持续时间: {spinDuration}秒，旋转速度: {spinSpeed}度/秒");
        }
    }

    /// <summary>
    /// 生成粒子效果
    /// </summary>
    private void SpawnParticle()
    {
        if (particlePrefab == null)
        {
            if (showDebugLog)
            {
                Debug.LogWarning("ShipSpinEffect: 未设置粒子预制体");
            }
            return;
        }

        // 计算粒子生成位置（飞船尾部）
        Vector3 spawnPosition = transform.TransformPoint(particleOffset);

        // 实例化粒子
        currentParticleInstance = Instantiate(particlePrefab, spawnPosition, transform.rotation);

        // 如果需要跟随飞船，设置为子对象
        if (particleFollowShip)
        {
            currentParticleInstance.transform.SetParent(transform);
            currentParticleInstance.transform.localPosition = particleOffset;
        }

        if (showDebugLog)
        {
            Debug.Log($"粒子效果已生成于位置: {spawnPosition}");
        }
    }

    /// <summary>
    /// 旋转协程
    /// </summary>
    private IEnumerator SpinCoroutine()
    {
        float elapsedTime = 0f;
        Vector3 normalizedAxis = spinAxis.normalized;

        // 持续旋转
        while (elapsedTime < spinDuration)
        {
            // 计算本帧旋转角度
            float rotationThisFrame = spinSpeed * Time.deltaTime;

            // 应用旋转（围绕本地轴心）
            transform.Rotate(normalizedAxis, rotationThisFrame, Space.Self);

            elapsedTime += Time.deltaTime;
            yield return null;
        }

        // 旋转结束
        EndSpin();
    }

    /// <summary>
    /// 结束旋转效果
    /// </summary>
    private void EndSpin()
    {
        isSpinning = false;

        // 销毁粒子效果
        DestroyParticle();

        // 启动冷却
        if (cooldownTime > 0f)
        {
            isOnCooldown = true;
            cooldownTimer = cooldownTime;
        }

        if (showDebugLog)
        {
            Debug.Log("旋转喷射结束");
        }
    }

    /// <summary>
    /// 销毁粒子效果
    /// </summary>
    private void DestroyParticle()
    {
        if (currentParticleInstance != null)
        {
            // 获取粒子系统，让其自然消失
            ParticleSystem ps = currentParticleInstance.GetComponent<ParticleSystem>();
            if (ps != null)
            {
                // 停止发射新粒子
                ps.Stop(true, ParticleSystemStopBehavior.StopEmitting);

                // 延迟销毁，让现有粒子播放完毕
                float remainingLifetime = ps.main.startLifetime.constantMax;
                Destroy(currentParticleInstance, remainingLifetime);
            }
            else
            {
                // 没有粒子系统，直接销毁
                Destroy(currentParticleInstance);
            }

            currentParticleInstance = null;
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
    /// 检查是否可以执行旋转
    /// </summary>
    public bool CanSpin()
    {
        if (shipState == null || shipState.CurrentState != ShipState.State.Flying)
        {
            return false;
        }

        if (isSpinning || isOnCooldown)
        {
            return false;
        }

        return true;
    }

    /// <summary>
    /// 强制停止旋转（用于特殊情况）
    /// </summary>
    public void ForceStopSpin()
    {
        if (isSpinning)
        {
            StopAllCoroutines();
            EndSpin();
        }
    }

    /// <summary>
    /// 当对象被禁用或销毁时清理
    /// </summary>
    private void OnDisable()
    {
        if (isSpinning)
        {
            StopAllCoroutines();
            isSpinning = false;
            DestroyParticle();
        }
    }
}

