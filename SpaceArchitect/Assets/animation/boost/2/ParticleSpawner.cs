using UnityEngine;

public class ParticleSpawner : MonoBehaviour
{
    [Header("需要拖拽的引用")]
    [SerializeField] private GameObject targetPrefab;      // 目标 Prefab
    [SerializeField] private GameObject particleEffect;     // 粒子特效 Prefab

    [Header("位置偏移设置")]
    [SerializeField] private Vector3 positionOffset = Vector3.left;  // 位置偏移
    [SerializeField] private bool useLocalOffset = true;             // 是否使用本地坐标系偏移

    [Header("旋转偏移设置")]  // 新增旋转偏移设置
    [SerializeField] private Vector3 rotationOffset = Vector3.zero;  // 旋转偏移（欧拉角）

    [Header("跟随设置")]
    [SerializeField] private float followDuration = 5f;    // 跟随持续时间

    private GameObject currentParticleInstance;            // 当前实例化的粒子
    private Transform targetTransform;                     // 目标 Transform 引用
    private float followTimer = 0f;                        // 跟随计时器
    private bool isFollowing = false;                      // 是否正在跟随

    void Update()
    {
        // 按下 P 键实例化粒子
        if (Input.GetKeyDown(KeyCode.P))
        {
            SpawnParticleAtTarget();
        }

        // 处理粒子跟随
        if (isFollowing && targetTransform != null && currentParticleInstance != null)
        {
            UpdateParticleTransform();
            UpdateFollowTimer();
        }
    }

    private void SpawnParticleAtTarget()
    {
        // 如果目标 Prefab 未设置，返回
        if (targetPrefab == null || particleEffect == null)
        {
            Debug.LogWarning("请先设置 Target Prefab 和 Particle Effect!");
            return;
        }

        // 停止当前正在跟随的粒子
        if (isFollowing && currentParticleInstance != null)
        {
            StopFollowing();
        }

        // 获取目标 Transform
        targetTransform = targetPrefab.transform;

        // 计算生成位置和旋转
        Vector3 spawnPosition = CalculateSpawnPosition();
        Quaternion spawnRotation = CalculateSpawnRotation();

        // 实例化粒子
        currentParticleInstance = Instantiate(particleEffect, spawnPosition, spawnRotation);

        // 开始跟随
        isFollowing = true;
        followTimer = followDuration;
    }

    private Vector3 CalculateSpawnPosition()
    {
        if (useLocalOffset)
        {
            // 使用本地坐标系偏移
            return targetTransform.position +
                   targetTransform.right * positionOffset.x +
                   targetTransform.up * positionOffset.y +
                   targetTransform.forward * positionOffset.z;
        }
        else
        {
            // 使用世界坐标系偏移
            return targetTransform.position + positionOffset;
        }
    }

    private Quaternion CalculateSpawnRotation()
    {
        // 基础旋转 = 目标物体的旋转
        Quaternion baseRotation = targetTransform.rotation;

        // 应用旋转偏移
        if (rotationOffset != Vector3.zero)
        {
            return baseRotation * Quaternion.Euler(rotationOffset);
        }

        return baseRotation;
    }

    private void UpdateParticleTransform()
    {
        if (currentParticleInstance == null) return;

        // 更新粒子位置
        currentParticleInstance.transform.position = CalculateSpawnPosition();

        // 更新粒子旋转（应用旋转偏移）
        currentParticleInstance.transform.rotation = CalculateSpawnRotation();
    }

    private void UpdateFollowTimer()
    {
        followTimer -= Time.deltaTime;

        if (followTimer <= 0f)
        {
            StopFollowing();
        }
    }

    private void StopFollowing()
    {
        isFollowing = false;

        if (currentParticleInstance != null)
        {
            var particleSystem = currentParticleInstance.GetComponent<ParticleSystem>();
            if (particleSystem != null)
            {
                particleSystem.Stop(true);
                Destroy(currentParticleInstance, particleSystem.main.startLifetime.constantMax + 0.5f);
            }
        }
    }

    // 在 Inspector 中显示调试信息
    void OnDrawGizmosSelected()
    {
        if (targetPrefab != null && targetPrefab.transform != null)
        {
            Gizmos.color = Color.green;
            Vector3 spawnPos = CalculateSpawnPosition();
            Gizmos.DrawWireSphere(spawnPos, 0.2f);
            Gizmos.DrawLine(targetPrefab.transform.position, spawnPos);

            // 绘制旋转方向的指示线
            Gizmos.color = Color.blue;
            Vector3 forwardDir = CalculateSpawnRotation() * Vector3.forward * 0.5f;
            Gizmos.DrawLine(spawnPos, spawnPos + forwardDir);

            Gizmos.color = Color.red;
            Vector3 rightDir = CalculateSpawnRotation() * Vector3.right * 0.3f;
            Gizmos.DrawLine(spawnPos, spawnPos + rightDir);
        }
    }
}