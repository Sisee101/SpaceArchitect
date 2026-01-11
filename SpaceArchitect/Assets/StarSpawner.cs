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

    // --- 在Inspector中显示的配置列表 ---

    [Header("星体配置")]
    public List<CorePlanetSetting> corePlanets; // 优先级 1 & 2
    public List<DustLayerSetting> dustLayers;   // 优先级 3 (多种尘埃)

    // --- 内部变量 ---
    private List<Transform> validAnchors = new List<Transform>(); // 存储核心行星位置

    // --- 生命周期 ---

    void Start()
    {
        GenerateGalaxy();
    }

    // Update被移除了，因为之前它只用来检测彗星生成

    // --- 主要逻辑 ---

    public void GenerateGalaxy()
    {
        // 1. 清理旧物体 (倒序删除防止索引错误)
        int childCount = transform.childCount;
        for (int i = childCount - 1; i >= 0; i--)
        {
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