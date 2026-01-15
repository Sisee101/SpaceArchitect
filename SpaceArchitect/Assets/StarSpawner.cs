using UnityEngine;
using System.Collections.Generic;

public class GalaxyDistributorXY : MonoBehaviour
{
    [Header("ȫ������ (XYƽ��)")]
    // ������ϵ����Ļ�ϵĿ��͸�
    public Vector2 areaSize = new Vector2(100, 50);
    public float depthVariance = 5.0f;
    // Z����ȷ�Χ (����3D��θУ���ֹ������ȫ�ص�)

    // --- �����ඨ�� ---

    [System.Serializable]
    public class CorePlanetSetting
    {
        public string label = "�������� (P1/P2)";
        public GameObject prefab;
        public int count = 5;
        [Range(0.1f, 10f)] public float minScale = 2.0f;
        [Range(0.1f, 10f)] public float maxScale = 4.0f;
    }

    [System.Serializable]
    public class DustLayerSetting
    {
        public string label = "�����㼶 (P3)";
        public GameObject prefab;
        public int count = 200;
        [Range(0.01f, 5f)] public float minScale = 0.2f;
        [Range(0.01f, 5f)] public float maxScale = 0.6f;
        [Tooltip("���ֲ��뾶")]
        public float clusterRadius = 10f;
        [Tooltip("��С���ð뾶 (��ֹ�����������ڲ�)")]
        public float minRadius = 2.5f;
    }

    // --- ��Inspector����ʾ�������б� ---

    [Header("��������")]
    public List<CorePlanetSetting> corePlanets; // ���ȼ� 1 & 2
    public List<DustLayerSetting> dustLayers;   // ���ȼ� 3 (���ֳ���)

    // --- �ڲ����� ---
    private List<Transform> validAnchors = new List<Transform>(); // �洢��������λ��

    // --- �������� ---

    void Start()
    {
        GenerateGalaxy();
    }

    // Update���Ƴ��ˣ���Ϊ֮ǰ��ֻ���������������

    // --- ��Ҫ�߼� ---

    public void GenerateGalaxy()
    {
        // 1. ���������� (����ɾ����ֹ��������)
        int childCount = transform.childCount;
        for (int i = childCount - 1; i >= 0; i--)
        {
            if (Application.isPlaying)
                Destroy(transform.GetChild(i).gameObject);
            else
                DestroyImmediate(transform.GetChild(i).gameObject);
        }
        validAnchors.Clear();

        // 2. ���ɺ�������
        SpawnCorePlanets();

        // 3. ���ɳ�����
        SpawnDustLayers();
    }

    // --- �������ɷ��� ---

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
                // A. ���ѡһ����������
                Transform targetCenter = validAnchors[Random.Range(0, validAnchors.Count)];

                // B. ����Բ��λ�� (XYƽ��)
                Vector2 direction = Random.insideUnitCircle.normalized;
                // ����С�뾶�����뾶֮���ֵ
                float distance = Mathf.Lerp(layer.minRadius, layer.clusterRadius, Random.value);

                Vector2 pos2D = direction * distance;

                // Z��΢��
                float randomDepth = Random.Range(-depthVariance / 4, depthVariance / 4);

                Vector3 finalPos = targetCenter.position + new Vector3(pos2D.x, pos2D.y, randomDepth);

                GameObject obj = Instantiate(layer.prefab, finalPos, GetRandomRotation(), transform);

                float scale = Random.Range(layer.minScale, layer.maxScale);
                obj.transform.localScale = Vector3.one * scale;
            }
        }
    }

    // --- �������� ---

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

    // ��Scene��ͼ���Ʒ�Χ��
    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireCube(transform.position, new Vector3(areaSize.x, areaSize.y, depthVariance * 2));
    }
}