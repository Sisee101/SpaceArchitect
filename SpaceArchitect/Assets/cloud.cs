using UnityEngine;
using System.Collections.Generic;

public class PolygonHoleSpawner : MonoBehaviour
{
    [Header("必须设置")]
    [Tooltip("请把挂载了 PolygonCollider2D 的物体拖到这里。这个碰撞器的形状就是'洞'的形状。")]
    public PolygonCollider2D holePolygon; // 用这个来画形状
    public GameObject prefab;

    [Header("生成区域")]
    public Vector2 areaSize = new Vector2(50, 50);

    [Header("密度与随机")]
    [Range(1f, 10f)]
    public float gridSpacing = 2.0f; // 间距
    [Range(0f, 1f)]
    public float spawnChance = 0.8f; // 生成几率
    [Range(0f, 1f)]
    public float positionJitter = 0.5f; // 位置抖动

    [Header("调试")]
    public bool autoUpdate = true;

    private List<GameObject> spawnedObjects = new List<GameObject>();

    void Start()
    {
        Generate();
    }

    [ContextMenu("生成分布")]
    public void Generate()
    {
        ClearOldObjects();

        if (prefab == null || holePolygon == null)
        {
            Debug.LogWarning("请设置 Prefab 和 Hole Polygon！");
            return;
        }

        float startX = transform.position.x - areaSize.x / 2;
        float startY = transform.position.y - areaSize.y / 2;
        float endX = transform.position.x + areaSize.x / 2;
        float endY = transform.position.y + areaSize.y / 2;

        // 循环遍历整个矩形区域
        for (float x = startX; x < endX; x += gridSpacing)
        {
            for (float y = startY; y < endY; y += gridSpacing)
            {
                // 1. 随机跳过（稀疏化）
                if (Random.value > spawnChance) continue;

                // 2. 计算目标位置
                Vector3 candidatePos = new Vector3(x, y, 0);

                // 3. 加上随机抖动
                candidatePos.x += Random.Range(-positionJitter, positionJitter);
                candidatePos.y += Random.Range(-positionJitter, positionJitter);

                // 4. 核心判断：如果点不在多边形内，则生成
                if (!IsPointInPolygon(candidatePos))
                {
                    SpawnObject(candidatePos);
                }
            }
        }
    }

    // --- 数学算法：判断点是否在多边形内 (射线法) ---
    bool IsPointInPolygon(Vector3 worldPos)
    {
        // 将世界坐标转为多边形的局部坐标（这样你移动多边形物体，洞也会跟着动）
        Vector2 localPoint = holePolygon.transform.InverseTransformPoint(worldPos);
        Vector2[] polyPoints = holePolygon.points; // 获取多边形顶点

        int j = polyPoints.Length - 1;
        bool inside = false;

        for (int i = 0; i < polyPoints.Length; i++)
        {
            if ((polyPoints[i].y < localPoint.y && polyPoints[j].y >= localPoint.y ||
                  polyPoints[j].y < localPoint.y && polyPoints[i].y >= localPoint.y) &&
                 (polyPoints[i].x + (localPoint.y - polyPoints[i].y) / (polyPoints[j].y - polyPoints[i].y) * (polyPoints[j].x - polyPoints[i].x) < localPoint.x))
            {
                inside = !inside;
            }
            j = i;
        }

        return inside;
    }

    void SpawnObject(Vector3 pos)
    {
        GameObject obj = Instantiate(prefab, pos, Quaternion.identity, transform);
        obj.transform.Rotate(0, 0, Random.Range(0, 360));
        float scale = Random.Range(0.8f, 1.2f);
        obj.transform.localScale = Vector3.one * scale;
        spawnedObjects.Add(obj);
    }

    [ContextMenu("清除所有")]
    public void ClearOldObjects()
    {
        if (Application.isPlaying)
        {
            foreach (var obj in spawnedObjects) if (obj != null) Destroy(obj);
        }
        else
        {
            // 这是一个安全的清空子物体方法
            var children = new List<GameObject>();
            foreach (Transform child in transform) children.Add(child.gameObject);
            children.ForEach(child => DestroyImmediate(child));
        }
        spawnedObjects.Clear();
    }

    void OnDrawGizmos()
    {
        // 画出生成范围框
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireCube(transform.position, new Vector3(areaSize.x, areaSize.y, 1));
    }

    void OnValidate()
    {
        if (autoUpdate && !Application.isPlaying)
        {
            UnityEditor.EditorApplication.delayCall += () => { if (this != null) Generate(); };
        }
    }
}