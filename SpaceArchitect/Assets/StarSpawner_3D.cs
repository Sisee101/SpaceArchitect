using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// 星系分布器（3D空间版本）
/// 支持在3D空间内生成核心行星和尘埃层，不再限制在XY平面
/// </summary>
public class GalaxyDistributor3D : MonoBehaviour
{
    [Header("全区域分布 (3D空间)")]
    [Tooltip("生成区域的尺寸（X、Y、Z三个维度）")]
    public Vector3 areaSize = new Vector3(100, 50, 50);

    // --- 行星定义 ---

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
        [Tooltip("聚集分布半径")]
        public float clusterRadius = 10f;
        [Tooltip("最小聚集半径 (防止生成在核心行星内部)")]
        public float minRadius = 2.5f;
    }

    // --- 在Inspector中显示的配置列表 ---

    [Header("行星配置")]
    public List<CorePlanetSetting> corePlanets; // 优先级 1 & 2
    public List<DustLayerSetting> dustLayers;   // 优先级 3 (尘埃层)

    // --- 内部变量 ---
    private List<Transform> validAnchors = new List<Transform>(); // 存储核心行星位置

    // --- 初始化方法 ---

    void Start()
    {
        GenerateGalaxy();
    }

    // --- 主要逻辑 ---

    public void GenerateGalaxy()
    {
        // 1. 清理子对象 (防止重复生成)
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

    // --- 行星生成方法 ---

    void SpawnCorePlanets()
    {
        foreach (var setting in corePlanets)
        {
            if (setting.prefab == null) continue;

            for (int i = 0; i < setting.count; i++)
            {
                Vector3 pos = GetRandomPosIn3D(areaSize);

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
                // A. 随机选择一个核心行星作为锚点
                Transform targetCenter = validAnchors[Random.Range(0, validAnchors.Count)];

                // B. 在3D球体内生成位置（不再是XY平面的圆形）
                // 使用球坐标系生成均匀分布的随机方向
                Vector3 direction = Random.onUnitSphere; // 单位球面上的随机方向
                
                // 在最小半径和最大半径之间随机距离
                float distance = Mathf.Lerp(layer.minRadius, layer.clusterRadius, Random.value);

                // 计算最终位置（锚点位置 + 方向 * 距离）
                Vector3 finalPos = targetCenter.position + direction * distance;

                GameObject obj = Instantiate(layer.prefab, finalPos, GetRandomRotation(), transform);

                float scale = Random.Range(layer.minScale, layer.maxScale);
                obj.transform.localScale = Vector3.one * scale;
            }
        }
    }

    // --- 辅助方法 ---

    /// <summary>
    /// 在3D空间内生成随机位置
    /// </summary>
    Vector3 GetRandomPosIn3D(Vector3 size)
    {
        float x = Random.Range(-size.x / 2, size.x / 2);
        float y = Random.Range(-size.y / 2, size.y / 2);
        float z = Random.Range(-size.z / 2, size.z / 2);
        return transform.position + new Vector3(x, y, z);
    }

    Quaternion GetRandomRotation()
    {
        return Quaternion.Euler(Random.Range(0, 360), Random.Range(0, 360), Random.Range(0, 360));
    }

    // 在Scene视图中绘制生成范围
    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireCube(transform.position, areaSize);
    }
}



