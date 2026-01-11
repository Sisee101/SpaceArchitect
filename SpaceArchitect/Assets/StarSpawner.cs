using UnityEngine;
using System.Collections.Generic;

public class GalaxyDistributorXY : MonoBehaviour
{
    [Header("全局设置 (XY平面)")]
    // 决定星系在屏幕上的宽和高
    public Vector2 areaSize = new Vector2(100, 50);
    // Z轴深度范围 (制造3D层次感，防止物体完全重叠)
    public float depthVariance = 5.0f;

    // --- 配置类定义 ---

    [System.Serializable]
    public class CorePlanetSetting
    {
        public string label = "核心行星 (P1/P2)";
        public GameObject prefab;
        public int count = 5;
        [Range(0.1f, 10f)] public float minScale = 2.0f;
        [Range(0.1f, 10f)] public float maxScale = 4.0f;
    }

    [System.Serializable]
    public class DustLayerSetting
    {
        public string label = "尘埃层级 (P3)";
        public GameObject prefab;
        public int count = 200;
        [Range(0.01f, 5f)] public float minScale = 0.2f;
        [Range(0.01f, 5f)] public float maxScale = 0.6f;
        [Tooltip("最大分布半径")]
        public float clusterRadius = 10f;
        [Tooltip("最小避让半径 (防止生成在星球内部)")]
        public float minRadius = 2.5f;
    }

    [System.Serializable]
    public class CometSetting
    {
        public bool enableComets = true;
        [Tooltip("必须挂载 CometMover 脚本的预制体")]
        public GameObject prefab;
        public float minSpawnInterval = 5f; // 最快多久刷一个
        public float maxSpawnInterval = 15f; // 最慢多久刷一个

        [Header("飞行参数")]
        public float minSpeed = 8f;
        public float maxSpeed = 15f;
        public float minScale = 0.5f;
        public float maxScale = 1.0f;
    }

    // --- 在Inspector中显示的配置列表 ---

    [Header("星体配置")]
    public List<CorePlanetSetting> corePlanets; // 优先级 1 & 2
    public List<DustLayerSetting> dustLayers;   // 优先级 3 (多种尘埃)

    [Header("动态彗星配置")]
    public CometSetting cometSetting;

    // --- 内部变量 ---
    private List<Transform> validAnchors = new List<Transform>(); // 存储核心行星位置
    private float nextCometTime = 0f; // 彗星计时器

    // --- 生命周期 ---

    void Start()
    {
        GenerateGalaxy();
        ResetCometTimer();
    }

    void Update()
    {
        HandleCometSpawning();
    }

    // --- 主要逻辑 ---

    public void GenerateGalaxy()
    {
        // 1. 清理旧物体 (倒序删除防止索引错误)
        int childCount = transform.childCount;
        for (int i = childCount - 1; i >= 0; i--)
        {
            // 只删除静态生成的物体，避免删除正在飞行的彗星(如果你希望重置时全部清空，可以去掉判断)
            // 这里我们粗暴一点，全部清空
            if (Application.isPlaying)
                Destroy(transform.GetChild(i).gameObject);
            else
                DestroyImmediate(transform.GetChild(i).gameObject);
        }
        validAnchors.Clear();

        // 2. 生成核心行星
        SpawnCorePlanets();

        // 3. 生成尘埃层
        SpawnDustLayers();
    }

    void HandleCometSpawning()
    {
        if (cometSetting.enableComets && cometSetting.prefab != null)
        {
            if (Time.time >= nextCometTime)
            {
                SpawnComet();
                ResetCometTimer();
            }
        }
    }

    // --- 具体生成方法 ---

    void SpawnCorePlanets()
    {
        foreach (var setting in corePlanets)
        {
            if (setting.prefab == null) continue;

            for (int i = 0; i < setting.count; i++)
            {
                Vector3 pos = GetRandomPosInXYPlane(areaSize);

                GameObject obj = Instantiate(setting.prefab, pos, GetRandomRotation(), transform);

                float scale = Random.Range(setting.minScale, setting.maxScale);
                obj.transform.localScale = Vector3.one * scale;

                validAnchors.Add(obj.transform);
            }
        }
    }

    void SpawnDustLayers()
    {
        if (validAnchors.Count == 0) return;

        foreach (var layer in dustLayers)
        {
            if (layer.prefab == null) continue;

            for (int i = 0; i < layer.count; i++)
            {
                // A. 随机选一个核心行星
                Transform targetCenter = validAnchors[Random.Range(0, validAnchors.Count)];

                // B. 计算圆环位置 (XY平面)
                Vector2 direction = Random.insideUnitCircle.normalized;
                // 在最小半径和最大半径之间插值
                float distance = Mathf.Lerp(layer.minRadius, layer.clusterRadius, Random.value);

                Vector2 pos2D = direction * distance;

                // Z轴微调
                float randomDepth = Random.Range(-depthVariance / 4, depthVariance / 4);

                Vector3 finalPos = targetCenter.position + new Vector3(pos2D.x, pos2D.y, randomDepth);

                GameObject obj = Instantiate(layer.prefab, finalPos, GetRandomRotation(), transform);

                float scale = Random.Range(layer.minScale, layer.maxScale);
                obj.transform.localScale = Vector3.one * scale;
            }
        }
    }

    void SpawnComet()
    {
        // 1. 决定生成位置（屏幕边缘外）
        // 50%概率左边出，50%概率右边出
        bool startFromLeft = Random.value > 0.5f;
        float spawnX = startFromLeft ? -areaSize.x / 2 - 10f : areaSize.x / 2 + 10f;

        float spawnY = Random.Range(-areaSize.y / 2, areaSize.y / 2);
        float spawnZ = Random.Range(-depthVariance, depthVariance);

        Vector3 spawnPos = transform.position + new Vector3(spawnX, spawnY, spawnZ);

        // 2. 决定目标点（屏幕另一侧的随机位置）
        float targetX = startFromLeft ? areaSize.x / 2 + 10f : -areaSize.x / 2 - 10f;
        float targetY = Random.Range(-areaSize.y / 2, areaSize.y / 2); // 随机高度
        Vector3 targetPos = transform.position + new Vector3(targetX, targetY, spawnZ);

        // 3. 生成
        GameObject comet = Instantiate(cometSetting.prefab, spawnPos, Quaternion.identity);

        // 4. 计算旋转 (让头部朝向目标)
        Vector3 direction = targetPos - spawnPos;
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        comet.transform.rotation = Quaternion.Euler(0, 0, angle);

        // 5. 缩放
        float scale = Random.Range(cometSetting.minScale, cometSetting.maxScale);
        comet.transform.localScale = Vector3.one * scale;

        // 6. 设置速度组件
        CometMover mover = comet.GetComponent<CometMover>();
        if (mover != null)
        {
            mover.speed = Random.Range(cometSetting.minSpeed, cometSetting.maxSpeed);

            // 计算需要飞多远
            float distance = Vector3.Distance(spawnPos, targetPos);
            // 设置生命周期：(距离 / 速度) * 保险系数
            mover.lifeTime = (distance / mover.speed) * 2.0f;
        }
        else
        {
            Debug.LogWarning("注意：你的彗星 Prefab 没有挂载 CometMover 脚本，它不会动！");
        }
    }

    void ResetCometTimer()
    {
        nextCometTime = Time.time + Random.Range(cometSetting.minSpawnInterval, cometSetting.maxSpawnInterval);
    }

    // --- 辅助工具 ---

    Vector3 GetRandomPosInXYPlane(Vector2 size)
    {
        float x = Random.Range(-size.x / 2, size.x / 2);
        float y = Random.Range(-size.y / 2, size.y / 2);
        float z = Random.Range(-depthVariance, depthVariance);
        return transform.position + new Vector3(x, y, z);
    }

    Quaternion GetRandomRotation()
    {
        return Quaternion.Euler(Random.Range(0, 360), Random.Range(0, 360), Random.Range(0, 360));
    }

    // 在Scene视图绘制范围框
    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireCube(transform.position, new Vector3(areaSize.x, areaSize.y, depthVariance * 2));
    }
}