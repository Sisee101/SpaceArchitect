using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class MultiGravityTrajectory : MonoBehaviour
{
    [Header("轨迹设置")]
    public int points = 150;
    public float timeStep = 0.1f;
    public Color lineColor = Color.green;

    [Header("行星管理")]
    public bool autoDetectPlanets = true;
    public List<GameObject> manualPlanets = new List<GameObject>();

    [Header("调试信息")]
    public bool isReady = false;
    public string statusMessage = "初始化中...";
    public int detectedPlanets = 0;

    private LineRenderer lineRenderer;
    private NBody shipNBody;
    private GravityEngine ge;
    private List<NBody> allPlanets = new List<NBody>();
    private Vector3 lastPosition;

    void Start()
    {
        SetupMultiGravityVisualizer();
    }

    void SetupMultiGravityVisualizer()
    {
        // 获取基础组件
        shipNBody = GetComponent<NBody>();
        ge = FindObjectOfType<GravityEngine>();

        if (shipNBody == null)
        {
            statusMessage = "错误：飞船缺少 NBody 组件";
            Debug.LogError(statusMessage);
            return;
        }

        if (ge == null)
        {
            statusMessage = "错误：场景中未找到 GravityEngine";
            Debug.LogError(statusMessage);
            return;
        }

        // 发现所有行星
        FindAllPlanets();

        // 创建轨迹渲染器
        CreateLineRenderer();

        // 延迟启动
        StartCoroutine(DelayedStart());
    }

    void FindAllPlanets()
    {
        allPlanets.Clear();

        if (autoDetectPlanets)
        {
            // 自动发现场景中所有有 NBody 的行星
            NBody[] allBodies = FindObjectsOfType<NBody>();
            foreach (NBody body in allBodies)
            {
                if (body.gameObject != gameObject && body.mass > 0.1f)
                {
                    allPlanets.Add(body);
                }
            }
        }

        // 添加手动指定的行星
        foreach (GameObject planetObj in manualPlanets)
        {
            if (planetObj != null)
            {
                NBody planetNBody = planetObj.GetComponent<NBody>();
                if (planetNBody != null && !allPlanets.Contains(planetNBody))
                {
                    allPlanets.Add(planetNBody);
                }
            }
        }

        detectedPlanets = allPlanets.Count;
        statusMessage = $"发现 {detectedPlanets} 个行星";

        if (detectedPlanets == 0)
        {
            Debug.LogWarning("未发现任何行星！轨迹预测将不准确。");
        }
    }

    void CreateLineRenderer()
    {
        lineRenderer = GetComponent<LineRenderer>();
        if (lineRenderer == null)
        {
            lineRenderer = gameObject.AddComponent<LineRenderer>();
        }

        lineRenderer.positionCount = points;

        // 创建简单材质
        Shader defaultShader = Shader.Find("Sprites/Default");
        if (defaultShader != null)
        {
            lineRenderer.material = new Material(defaultShader);
        }
        else
        {
            // 备用方案：使用任何可用的着色器
            lineRenderer.material = new Material(Shader.Find("Standard"));
        }

        lineRenderer.startColor = lineColor;
        lineRenderer.endColor = lineColor;
        lineRenderer.startWidth = 0.08f;
        lineRenderer.endWidth = 0.08f;
        lineRenderer.useWorldSpace = true;

        // 初始化为空轨迹
        Vector3[] emptyPositions = new Vector3[points];
        for (int i = 0; i < points; i++)
        {
            emptyPositions[i] = transform.position;
        }
        lineRenderer.SetPositions(emptyPositions);
    }

    System.Collections.IEnumerator DelayedStart()
    {
        statusMessage = "等待 Gravity Engine 初始化...";

        int maxWaitTime = 50; // 最多等待5秒
        int waitedFrames = 0;

        while (waitedFrames < maxWaitTime)
        {
            // 检查引擎是否就绪
            if (ge != null && ge.isActiveAndEnabled)
            {
                isReady = true;
                statusMessage = $"轨迹可视化就绪！跟踪 {detectedPlanets} 个行星";
                Debug.Log(statusMessage);
                yield break;
            }

            waitedFrames++;
            yield return new WaitForSeconds(0.1f);
        }

        statusMessage = "警告：Gravity Engine 初始化超时，但仍将继续尝试";
        isReady = true;
    }

    void Update()
    {
        if (!isReady) return;

        UpdateMultiGravityTrajectory();
    }

    void UpdateMultiGravityTrajectory()
    {
        if (ge == null || !ge.isActiveAndEnabled || shipNBody == null)
            return;

        Vector3[] trajectory = new Vector3[points];
        Vector3 currentPosition = transform.position;
        Vector3 currentVelocity = GetShipVelocity();

        // 多体引力轨迹预测
        for (int i = 0; i < points; i++)
        {
            trajectory[i] = currentPosition;

            // 计算所有行星的引力影响
            Vector3 totalGravity = CalculateTotalGravity(currentPosition);

            // 更新速度和位置
            currentVelocity += totalGravity * timeStep;
            currentPosition += currentVelocity * timeStep;
        }

        // 更新轨迹显示
        if (lineRenderer != null)
        {
            lineRenderer.SetPositions(trajectory);
        }
    }

    Vector3 CalculateTotalGravity(Vector3 position)
    {
        Vector3 totalGravity = Vector3.zero;

        foreach (NBody planet in allPlanets)
        {
            if (planet == null) continue;

            Vector3 toPlanet = planet.transform.position - position;
            float distance = toPlanet.magnitude;

            // 避免除零和过近距离的数值问题
            if (distance < 0.5f) continue;

            // 引力公式: a = G * M / r^2
            // 注意：这里使用了简化的引力计算
            float forceMagnitude = planet.mass / (distance * distance);
            totalGravity += toPlanet.normalized * forceMagnitude;
        }

        return totalGravity;
    }

    Vector3 GetShipVelocity()
    {
        // 方法1：使用 Rigidbody 的速度（如果有）
        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb != null)
        {
            return rb.velocity;
        }

        // 方法2：基于位置变化估算速度
        return (transform.position - lastPosition) / Time.deltaTime;
    }

    void LateUpdate()
    {
        // 保存当前位置供下一帧计算速度使用
        lastPosition = transform.position;
    }

    // 公共方法
    public void AddPlanet(GameObject planet)
    {
        if (planet != null)
        {
            NBody planetNBody = planet.GetComponent<NBody>();
            if (planetNBody != null && !allPlanets.Contains(planetNBody))
            {
                allPlanets.Add(planetNBody);
                detectedPlanets = allPlanets.Count;
                statusMessage = $"已添加行星，现在跟踪 {detectedPlanets} 个行星";
            }
        }
    }

    public void RemovePlanet(GameObject planet)
    {
        if (planet != null)
        {
            NBody planetNBody = planet.GetComponent<NBody>();
            if (planetNBody != null && allPlanets.Contains(planetNBody))
            {
                allPlanets.Remove(planetNBody);
                detectedPlanets = allPlanets.Count;
                statusMessage = $"已移除行星，现在跟踪 {detectedPlanets} 个行星";
            }
        }
    }

    public void RefreshPlanetList()
    {
        FindAllPlanets();
        statusMessage = $"已刷新，跟踪 {detectedPlanets} 个行星";
    }

    public void ShowTrajectory(bool show)
    {
        if (lineRenderer != null)
        {
            lineRenderer.enabled = show;
        }
    }

    public void SetTrajectoryColor(Color newColor)
    {
        lineColor = newColor;
        if (lineRenderer != null)
        {
            lineRenderer.startColor = newColor;
            lineRenderer.endColor = newColor;
        }
    }

    // 在 Inspector 中提供便捷按钮
    [ContextMenu("刷新行星列表")]
    void RefreshPlanetsContext()
    {
        RefreshPlanetList();
    }

    [ContextMenu("显示轨迹")]
    void ShowTrajectoryContext()
    {
        ShowTrajectory(true);
    }

    [ContextMenu("隐藏轨迹")]
    void HideTrajectoryContext()
    {
        ShowTrajectory(false);
    }

    // 调试显示
    void OnGUI()
    {
#if UNITY_EDITOR
        if (!Application.isPlaying) return;
        
        GUIStyle style = new GUIStyle(GUI.skin.label);
        style.normal.textColor = isReady ? Color.green : Color.yellow;
        
        GUILayout.BeginArea(new Rect(10, 100, 400, 100));
        GUILayout.Label($"多体轨迹状态: {statusMessage}", style);
        GUILayout.Label($"跟踪行星数: {detectedPlanets}", style);
        GUILayout.EndArea();
#endif
    }
}