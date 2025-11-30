using UnityEngine;
using System.Collections;

public class SpeedBoostEnhanced : MonoBehaviour
{
    [Header("加速设置")]
    public float boostSpeed = 20f;
    public float minSpeedToBoost = 0.1f;
    public float boostCooldown = 2f;

    [Header("尾气粒子效果")]
    public GameObject exhaustParticlePrefab; // 尾气粒子预制体
    public float exhaustDuration = 1.5f;     // 尾气效果持续时间
    public float exhaustEmissionRate = 50f;  // 尾气粒子发射速率

    [Header("视觉效果")]
    public ParticleSystem boostParticles;     // 加速粒子效果
    public Light boostLight;                 // 加速灯光效果
    public float lightDuration = 0.5f;       // 灯光持续时间

    [Header("音效")]
    public AudioClip boostSound;             // 加速音效
    public AudioSource audioSource;

    private Rigidbody spaceshipRb;
    private SpaceshipGravity spaceshipGravity;
    private bool canBoost = true;
    private float cooldownTimer = 0f;
    private float lightTimer = 0f;
    private float originalLightIntensity;
    private ParticleSystem activeExhaustParticles; // 当前活动的尾气粒子系统

    void Start()
    {
        spaceshipRb = GetComponent<Rigidbody>();
        spaceshipGravity = GetComponent<SpaceshipGravity>();

        // 初始化音效组件
        if (audioSource == null)
            audioSource = GetComponent<AudioSource>();

        // 保存原始灯光强度
        if (boostLight != null)
            originalLightIntensity = boostLight.intensity;
    }

    void Update()
    {
        HandleBoostCooldown();
        HandleBoostEffects();

        if (Input.GetKeyDown(KeyCode.E) && canBoost)
        {
            ApplySpeedBoost();
        }

        // 更新现有尾气粒子的方向
        UpdateExhaustDirection();
    }

    void HandleBoostCooldown()
    {
        if (!canBoost)
        {
            cooldownTimer -= Time.deltaTime;
            if (cooldownTimer <= 0f)
            {
                canBoost = true;
                cooldownTimer = 0f;
            }
        }
    }

    void HandleBoostEffects()
    {
        // 处理加速灯光效果
        if (boostLight != null && lightTimer > 0f)
        {
            lightTimer -= Time.deltaTime;
            float intensityRatio = lightTimer / lightDuration;
            boostLight.intensity = originalLightIntensity * intensityRatio;

            if (lightTimer <= 0f)
            {
                boostLight.intensity = 0f;
            }
        }
    }

    void ApplySpeedBoost()
    {
        if (spaceshipRb == null) return;

        Vector3 currentVelocity = spaceshipRb.velocity;
        Vector3 boostDirection;

        if (currentVelocity.magnitude < minSpeedToBoost)
        {
            boostDirection = transform.forward;
        }
        else
        {
            boostDirection = currentVelocity.normalized;
        }

        // 应用速度提升
        Vector3 boostVelocity = boostDirection * boostSpeed;
        spaceshipRb.velocity += boostVelocity;

        // 触发冷却
        canBoost = false;
        cooldownTimer = boostCooldown;

        // 生成尾气粒子效果
        CreateExhaustEffect(-boostDirection); // 方向与速度方向相反

        // 播放原有粒子效果
        if (boostParticles != null)
        {
            boostParticles.Play();
        }

        // 播放音效
        if (audioSource != null && boostSound != null)
        {
            audioSource.PlayOneShot(boostSound);
        }

        // 激活灯光效果
        if (boostLight != null)
        {
            boostLight.intensity = originalLightIntensity;
            lightTimer = lightDuration;
        }

        // 确保飞船处于发射状态
        if (spaceshipGravity != null)
        {
            spaceshipGravity.SetLaunched(true);
        }
    }

    void CreateExhaustEffect(Vector3 exhaustDirection)
    {
        if (exhaustParticlePrefab == null)
        {
            Debug.LogWarning("未设置尾气粒子预制体！");
            return;
        }

        // 清除现有的尾气效果
        if (activeExhaustParticles != null)
        {
            Destroy(activeExhaustParticles.gameObject);
        }

        // 创建新的尾气粒子系统
        GameObject exhaustObj = Instantiate(exhaustParticlePrefab, transform.position, Quaternion.identity);
        exhaustObj.transform.SetParent(transform); // 设置为飞船的子物体
        activeExhaustParticles = exhaustObj.GetComponent<ParticleSystem>();

        // 配置粒子系统参数
        if (activeExhaustParticles != null)
        {
            ConfigureExhaustParticles(activeExhaustParticles, exhaustDirection);

            // 播放粒子效果
            activeExhaustParticles.Play();

            // 设置自动销毁
            StartCoroutine(DestroyExhaustAfterDelay(exhaustObj, exhaustDuration));
        }
    }

    void ConfigureExhaustParticles(ParticleSystem ps, Vector3 direction)
    {
        var main = ps.main;
        var emission = ps.emission;
        var shape = ps.shape;
        var velocityOverLifetime = ps.velocityOverLifetime;

        // 设置粒子方向
        ps.transform.rotation = Quaternion.LookRotation(direction);

        // 主模块设置[2,5](@ref)
        main.startLifetime = exhaustDuration * 0.8f;
        main.startSpeed = 5f;
        main.startSize = 0.5f;
        main.simulationSpace = ParticleSystemSimulationSpace.World;

        // 发射模块[5](@ref)
        emission.rateOverTime = exhaustEmissionRate;

        // 形状模块 - 使用锥形模拟尾气喷射[1,2](@ref)
        shape.shapeType = ParticleSystemShapeType.Cone;
        shape.angle = 15f; // 锥形角度
        shape.radius = 0.1f; // 基础半径

        // 生命周期内的速度控制[2](@ref)
        if (velocityOverLifetime.enabled)
        {
            velocityOverLifetime.space = ParticleSystemSimulationSpace.Local;
            velocityOverLifetime.z = -2f; // 向后推力
        }

        // 颜色随时间变化 - 模拟尾气消散效果[2,5](@ref)
        var colorOverLifetime = ps.colorOverLifetime;
        if (colorOverLifetime.enabled)
        {
            Gradient gradient = new Gradient();
            gradient.SetKeys(
                new GradientColorKey[] {
                    new GradientColorKey(Color.white, 0.0f),
                    new GradientColorKey(Color.gray, 0.5f),
                    new GradientColorKey(Color.black, 1.0f)
                },
                new GradientAlphaKey[] {
                    new GradientAlphaKey(1.0f, 0.0f),
                    new GradientAlphaKey(0.5f, 0.3f),
                    new GradientAlphaKey(0.0f, 1.0f)
                }
            );
            colorOverLifetime.color = gradient;
        }

        // 大小随时间变化[5](@ref)
        var sizeOverLifetime = ps.sizeOverLifetime;
        if (sizeOverLifetime.enabled)
        {
            sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(1f,
                new AnimationCurve(new Keyframe(0, 0.3f), new Keyframe(0.5f, 1f), new Keyframe(1, 1.5f)));
        }
    }

    void UpdateExhaustDirection()
    {
        // 更新现有尾气粒子的方向，使其始终与速度方向相反
        if (activeExhaustParticles != null && spaceshipRb != null)
        {
            Vector3 currentVelocity = spaceshipRb.velocity;
            if (currentVelocity.magnitude > 0.1f)
            {
                Vector3 exhaustDirection = -currentVelocity.normalized;
                activeExhaustParticles.transform.rotation = Quaternion.LookRotation(exhaustDirection);
            }
        }
    }

    IEnumerator DestroyExhaustAfterDelay(GameObject exhaustObj, float delay)
    {
        yield return new WaitForSeconds(delay);

        if (exhaustObj != null)
        {
            // 先停止粒子发射，然后淡出
            ParticleSystem ps = exhaustObj.GetComponent<ParticleSystem>();
            if (ps != null)
            {
                var emission = ps.emission;
                emission.enabled = false; // 停止发射新粒子

                // 等待所有粒子自然消亡
                yield return new WaitForSeconds(ps.main.startLifetime.constantMax);
            }

            Destroy(exhaustObj);
            if (exhaustObj == activeExhaustParticles?.gameObject)
            {
                activeExhaustParticles = null;
            }
        }
    }

    void OnGUI()
    {
        GUILayout.Label($"当前速度: {(spaceshipRb != null ? spaceshipRb.velocity.magnitude.ToString("F2") : "0")}");
        GUILayout.Label($"加速状态: {(canBoost ? "就绪" : $"冷却中 {cooldownTimer:F1}s")}");
        GUILayout.Label("按 E 键沿当前速度方向加速并产生尾气");
    }

    public void TriggerBoost()
    {
        if (canBoost)
        {
            ApplySpeedBoost();
        }
    }

    public void ResetBoost()
    {
        canBoost = true;
        cooldownTimer = 0f;

        // 重置效果
        if (boostLight != null)
        {
            boostLight.intensity = 0f;
            lightTimer = 0f;
        }

        // 清除尾气效果
        if (activeExhaustParticles != null)
        {
            Destroy(activeExhaustParticles.gameObject);
            activeExhaustParticles = null;
        }
    }
}