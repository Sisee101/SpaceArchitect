using UnityEngine;
using System.Collections.Generic;

public class PolygonHoleSpawner : MonoBehaviour
{
    [Header("必须设置")]
    [Tooltip("请设置多个形状，按F键切换")]
    public PolygonCollider2D[] holePolygons;
    public GameObject prefab;

    [Header("生成区域")]
    public Vector2 areaSize = new Vector2(50, 50);

    [Header("密度与随机")]
    [Range(1f, 10f)]
    public float gridSpacing = 2.0f;
    [Range(0f, 1f)]
    public float spawnChance = 0.8f;
    [Range(0f, 1f)]
    public float positionJitter = 0.5f;

    [Header("调试")]
    public bool autoUpdate = true;

    private List<GameObject> spawnedObjects = new List<GameObject>();
    private int currentHoleIndex = 0;

    void Start()
    {
        Generate();
    }

    void Update()
    {
        // 监听 F 键，直接硬切
        if (Input.GetKeyDown(KeyCode.F))
        {
            SwitchHoleShape();
        }
    }

    void SwitchHoleShape()
    {
        if (holePolygons == null || holePolygons.Length == 0) return;

        // 切换索引
        currentHoleIndex = (currentHoleIndex + 1) % holePolygons.Length;

        // 立即重新生成
        Generate();
    }

    [ContextMenu("生成分布")]
    public void Generate()
    {
        // 1. 瞬间清除旧的
        ClearOldObjects();

        if (prefab == null || holePolygons == null || holePolygons.Length == 0) return;

        PolygonCollider2D activeHole = holePolygons[currentHoleIndex];
        if (activeHole == null) return;

        float startX = transform.position.x - areaSize.x / 2;
        float startY = transform.position.y - areaSize.y / 2;
        float endX = transform.position.x + areaSize.x / 2;
        float endY = transform.position.y + areaSize.y / 2;

        // 2. 瞬间生成新的
        for (float x = startX; x < endX; x += gridSpacing)
        {
            for (float y = startY; y < endY; y += gridSpacing)
            {
                // 随机跳过
                if (Random.value > spawnChance) continue;

                Vector3 candidatePos = new Vector3(x, y, 0);

                // 加上抖动
                candidatePos.x += Random.Range(-positionJitter, positionJitter);
                candidatePos.y += Random.Range(-positionJitter, positionJitter);

                // 核心判断：如果点不在当前选中的多边形内，则生成
                if (!IsPointInPolygon(candidatePos, activeHole))
                {
                    SpawnObject(candidatePos);
                }
            }
        }
    }

    // --- 数学算法 ---
    bool IsPointInPolygon(Vector3 worldPos, PolygonCollider2D targetHole)
    {
        Vector2 localPoint = targetHole.transform.InverseTransformPoint(worldPos);
        Vector2[] polyPoints = targetHole.points;

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
            var children = new List<GameObject>();
            foreach (Transform child in transform) children.Add(child.gameObject);
            children.ForEach(child => DestroyImmediate(child));
        }
        spawnedObjects.Clear();
    }

    void OnDrawGizmos()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireCube(transform.position, new Vector3(areaSize.x, areaSize.y, 1));
    }

    void OnValidate()
    {
        if (autoUpdate && !Application.isPlaying && holePolygons != null && holePolygons.Length > 0)
        {
            UnityEditor.EditorApplication.delayCall += () => { if (this != null) Generate(); };
        }
    }
}