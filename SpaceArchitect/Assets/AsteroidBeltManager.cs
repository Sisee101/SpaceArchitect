using UnityEngine;

public class GalaxyManager : MonoBehaviour
{
    [Header("核心资源")]
    [Tooltip("拖入你的岩石/星星预制体")]
    public GameObject[] prefabs;

    [Header("银河形状设置")]
    [Tooltip("生成的总数量")]
    public int itemCount = 1000;

    [Tooltip("银河的半径")]
    public float maxRadius = 20f;

    [Tooltip("旋臂的数量 (银河通常是2条或4条)")]
    [Range(1, 10)]
    public int armCount = 2;

    [Tooltip("扭曲程度 (值越大，旋臂卷得越紧)")]
    public float twistFactor = 3f;

    [Tooltip("旋臂的宽度/散布程度")]
    public float armSpread = 2f;

    [Tooltip("中心密集度曲线")]
    public AnimationCurve densityCurve = new AnimationCurve(new Keyframe(0, 0), new Keyframe(1, 1));

    [Header("可视化与随机性")]
    [Tooltip("改变这个数字可以生成不同的银河变体")]
    public int randomSeed = 42;

    [Tooltip("是否在Scene窗口显示预览点")]
    public bool showGizmos = true;

    [Tooltip("预览点的大小")]
    public float gizmoSize = 0.5f;

    [Tooltip("预览点的颜色")]
    public Color gizmoColor = Color.yellow;

    [Header("缩放微调")]
    [Tooltip("最小随机缩放倍数 (基于Prefab原大小)")]
    public float minScaleMultiplier = 0.5f;
    [Tooltip("最大随机缩放倍数 (基于Prefab原大小)")]
    public float maxScaleMultiplier = 1.5f;

    // 容器
    private GameObject galaxyHolder;

    void Start()
    {
        GenerateGalaxy();
    }

    [ContextMenu("生成银河")]
    public void GenerateGalaxy()
    {
        // 1. 清理旧的
        if (galaxyHolder != null) Destroy(galaxyHolder);
        if (transform.childCount > 0)
        {
            foreach (Transform child in transform) DestroyImmediate(child.gameObject);
        }

        galaxyHolder = new GameObject("Galaxy Holder");
        galaxyHolder.transform.parent = this.transform;
        galaxyHolder.transform.localPosition = Vector3.zero;
        galaxyHolder.transform.localScale = Vector3.one; // 确保父物体缩放正常

        if (prefabs.Length == 0) return;

        // 2. 初始化随机状态
        Random.InitState(randomSeed);

        // 3. 生成
        for (int i = 0; i < itemCount; i++)
        {
            Vector3 pos = CalculateStarPosition(i);

            // 随机选一个 Prefab
            GameObject selectedPrefab = prefabs[Random.Range(0, prefabs.Length)];

            // 实例化
            // 注意：使用 TransformPoint 将局部坐标转为世界坐标，以支持 Manager 的移动
            GameObject instance = Instantiate(selectedPrefab, galaxyHolder.transform.TransformPoint(pos), Quaternion.identity, galaxyHolder.transform);

            // --- 视觉微调 ---

            // A. 随机旋转
            instance.transform.rotation = Random.rotation;

            // B. 随机缩放 (这里是修改过的地方)
            // 获取 Prefab 原本的大小
            Vector3 originalScale = selectedPrefab.transform.localScale;
            // 生成随机倍数
            float randomFactor = Random.Range(minScaleMultiplier, maxScaleMultiplier);
            // 应用缩放：原大小 * 随机倍数
            instance.transform.localScale = originalScale * randomFactor;
        }
    }

    // --- 核心数学逻辑 ---
    Vector3 CalculateStarPosition(int index)
    {
        float distanceT = densityCurve.Evaluate(Random.value);
        float r = distanceT * maxRadius;

        int armIndex = index % armCount;
        float baseAngle = (360f / armCount) * armIndex;
        float spiralAngle = r * twistFactor;
        float theta = (baseAngle + spiralAngle) * Mathf.Deg2Rad;

        float x = Mathf.Cos(theta) * r;
        float y = Mathf.Sin(theta) * r;

        Vector2 randomOffset = Random.insideUnitCircle * armSpread;

        return new Vector3(x + randomOffset.x, y + randomOffset.y, 0);
    }

    // --- 可视化绘图 ---
    void OnDrawGizmos()
    {
        if (!showGizmos) return;

        var oldState = Random.state;
        Random.InitState(randomSeed);
        Gizmos.color = gizmoColor;

        for (int i = 0; i < itemCount; i++)
        {
            Vector3 localPos = CalculateStarPosition(i);
            Vector3 worldPos = transform.TransformPoint(localPos);
            Gizmos.DrawSphere(worldPos, gizmoSize);
        }

        Random.state = oldState;
    }
}