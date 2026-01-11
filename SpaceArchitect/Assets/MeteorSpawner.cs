using UnityEngine;

public class MeteorSpawner : MonoBehaviour
{
    [Header("核心设置")]
    public GameObject meteorPrefab; // 拖入 LightMeteor 预制体
    public Vector2 spawnArea = new Vector2(100, 50); // 生成区域范围
    public float depthVariance = 5f; // Z轴深度随机范围

    [Header("生成频率与数量")]
    public float minInterval = 3f; // 最小间隔时间
    public float maxInterval = 8f; // 最大间隔时间
    [Range(1, 5)]
    public int minBurst = 1;       // 每次最少生成几个
    [Range(1, 5)]
    public int maxBurst = 3;       // 每次最多生成几个

    [Header("随机变化 (倍率)")]
    [Tooltip("速度随机倍率：比如 0.8 到 1.5 倍")]
    public float minSpeedMult = 0.8f;
    public float maxSpeedMult = 1.5f;

    [Tooltip("存活时间随机倍率")]
    public float minLifeMult = 0.8f;
    public float maxLifeMult = 1.2f;

    [Tooltip("大小随机范围")]
    public float minScale = 0.8f;
    public float maxScale = 1.2f;

    // 内部计时器
    private float timer;

    void Start()
    {
        ResetTimer();
    }

    void Update()
    {
        if (meteorPrefab == null) return;

        timer -= Time.deltaTime;
        if (timer <= 0)
        {
            // --- 核心修改：决定这一次生成几个 ---
            // Random.Range(int, int) 是左闭右开的，所以 maxBurst 要 +1
            int burstCount = Random.Range(minBurst, maxBurst + 1);

            for (int i = 0; i < burstCount; i++)
            {
                SpawnSingleMeteor();
            }

            ResetTimer();
        }
    }

    void SpawnSingleMeteor()
    {
        // 1. 决定左右 (50% 概率)
        bool spawnOnLeft = Random.value > 0.5f;

        // 2. 计算位置
        // 如果一次生成多个，我们在Y轴上额外加一点微小的随机偏移，防止完全重叠
        float xOffset = spawnArea.x / 2 + Random.Range(5f, 15f); // 稍微错开出生的X位置
        float spawnX = spawnOnLeft ? -xOffset : xOffset;

        float spawnY = Random.Range(-spawnArea.y / 2, spawnArea.y / 2);
        float spawnZ = Random.Range(-depthVariance, depthVariance);

        Vector3 spawnPos = transform.position + new Vector3(spawnX, spawnY, spawnZ);

        // 3. 生成
        GameObject newMeteor = Instantiate(meteorPrefab, spawnPos, Quaternion.identity);

        // 4. 设置随机大小
        float randomScale = Random.Range(minScale, maxScale);
        newMeteor.transform.localScale = Vector3.one * randomScale;

        // 5. 获取并修改 ShootingStar 参数
        ShootingStar starScript = newMeteor.GetComponent<ShootingStar>();
        if (starScript != null)
        {
            // --- 速度随机化 ---
            // 获取随机倍率
            float speedMult = Random.Range(minSpeedMult, maxSpeedMult);

            // 基础绝对速度 (取你在Prefab上填的数值)
            float baseSpeedX = Mathf.Abs(starScript.speedX);

            // 如果在左边生，向右飞(+); 右边生，向左飞(-)
            starScript.speedX = (spawnOnLeft ? baseSpeedX : -baseSpeedX) * speedMult;

            // Y轴速度也随机一下
            starScript.speedY = starScript.speedY * Random.Range(0.5f, 1.5f);

            // --- 生命周期随机化 ---
            starScript.lifeTime = starScript.lifeTime * Random.Range(minLifeMult, maxLifeMult);
        }
    }

    void ResetTimer()
    {
        timer = Random.Range(minInterval, maxInterval);
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(1, 0.9f, 0, 0.3f);
        Gizmos.DrawWireCube(transform.position, new Vector3(spawnArea.x, spawnArea.y, depthVariance * 2));
    }
}