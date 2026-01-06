using UnityEngine;
using System.Collections.Generic;

public class OrbitCalculator : MonoBehaviour
{
    [Header("轨道计算设置")]
    [Tooltip("是否实时更新轨道计算")]
    public bool realtimeUpdate = true;

    [Tooltip("轨道分段数量（越多越平滑）")]
    [Range(32, 256)]
    public int orbitSegments = 128;

    [Header("轨道可视化")]
    public Color orbitColor = new Color(0.2f, 0.8f, 1f, 0.7f);
    public float lineWidth = 0.05f;
    public Material orbitMaterial;

    [Header("轨道捕获设置")]
    [Tooltip("轨道捕获的垂直容差距离")]
    public float captureVerticalTolerance = 5f;

    [Tooltip("轨道捕获的水平容差距离（轨道半径的百分比）")]
    [Range(0.01f, 0.1f)]
    public float captureHorizontalTolerance = 0.05f;

    [Tooltip("速度方向与轨道切线方向的最大允许角度差（度）")]
    [Range(1f, 90f)]
    public float captureAngleTolerance = 30f;

    [Tooltip("平滑过渡到轨道的时间（秒）")]
    public float smoothTransitionTime = 2.0f;

    [Header("调试信息")]
    [Tooltip("是否显示调试信息")]
    public bool showDebugInfo = true;

    // 运行时变量
    private Planet planet;
    private LineRenderer orbitRenderer;
    private List<Rigidbody> spaceships = new List<Rigidbody>();
    private float currentOrbitRadius;
    private bool hasValidOrbit = false;
    private bool isInitialized = false;

    // 轨道捕获状态变量
    private bool isSpaceshipCaptured = false;
    private Rigidbody capturedSpaceship = null;
    private float orbitAngularVelocity = 0f;
    private Vector3 orbitCenter;
    private Vector3 orbitNormal = Vector3.up;

    // 新增：平滑过渡相关变量
    private bool isTransitioningToOrbit = false;
    private float transitionProgress = 0f;
    private Coroutine transitionCoroutine;

    void Start()
    {
        InitializeOrbitCalculator();
    }

    /// <summary>
    /// 初始化轨道计算器
    /// </summary>
    private void InitializeOrbitCalculator()
    {
        // 获取同一物体上的Planet组件
        planet = GetComponent<Planet>();
        if (planet == null)
        {
            Debug.LogError($"OrbitCalculator: 在 {gameObject.name} 上未找到Planet组件！");
            return;
        }

        // 清理可能存在的冲突组件
        CleanupExistingComponents();

        // 初始化轨道渲染器
        InitializeOrbitRenderer();

        // 查找所有飞船
        FindAllSpaceships();

        isInitialized = true;
        if (showDebugInfo)
        {
            Debug.Log($"OrbitCalculator 初始化完成 - 行星: {gameObject.name}");
        }
    }

    /// <summary>
    /// 清理可能冲突的组件
    /// </summary>
    private void CleanupExistingComponents()
    {
        // 查找所有LineRenderer组件，只保留一个
        LineRenderer[] existingRenderers = GetComponents<LineRenderer>();
        for (int i = existingRenderers.Length - 1; i > 0; i--)
        {
            if (Application.isPlaying)
                Destroy(existingRenderers[i]);
            else
                DestroyImmediate(existingRenderers[i]);
        }
    }

    /// <summary>
    /// 初始化轨道渲染器
    /// </summary>
    private void InitializeOrbitRenderer()
    {
        // 添加或获取LineRenderer组件
        orbitRenderer = gameObject.GetComponent<LineRenderer>();
        if (orbitRenderer == null)
        {
            orbitRenderer = gameObject.AddComponent<LineRenderer>();
        }

        // 配置LineRenderer
        orbitRenderer.positionCount = orbitSegments + 1;
        orbitRenderer.loop = true;
        orbitRenderer.useWorldSpace = true;

        // 设置材质
        if (orbitMaterial == null)
        {
            // 创建默认材质
            Shader lineShader = Shader.Find("Sprites/Default");
            if (lineShader != null)
            {
                orbitMaterial = new Material(lineShader);
            }
            else
            {
                // 备用方案
                orbitMaterial = new Material(Shader.Find("Standard"));
            }
        }
        orbitRenderer.material = new Material(orbitMaterial); // 创建实例副本

        // 设置线条属性
        orbitRenderer.startColor = orbitColor;
        orbitRenderer.endColor = orbitColor;
        orbitRenderer.startWidth = lineWidth;
        orbitRenderer.endWidth = lineWidth;

        // 设置独特的渲染队列避免冲突
        orbitRenderer.material.renderQueue = 3000 + GetInstanceID() % 1000;

        orbitRenderer.enabled = false;
    }

    /// <summary>
    /// 查找所有飞船
    /// </summary>
    private void FindAllSpaceships()
    {
        GameObject[] spaceshipObjects = GameObject.FindGameObjectsWithTag("spaceship");
        spaceships.Clear();

        foreach (GameObject spaceship in spaceshipObjects)
        {
            Rigidbody rb = spaceship.GetComponent<Rigidbody>();
            if (rb != null)
            {
                spaceships.Add(rb);
                if (showDebugInfo)
                {
                    Debug.Log($"行星 {gameObject.name} 找到飞船: {spaceship.name}");
                }
            }
        }

        if (spaceships.Count == 0 && showDebugInfo)
        {
            Debug.LogWarning($"行星 {gameObject.name} 未找到带有Rigidbody组件的飞船物体！");
        }
    }

    void Update()
    {
        if (!isInitialized || planet == null) return;

        // 定期检查飞船引用（每60帧检查一次，避免性能问题）
        if (spaceships.Count == 0 && Time.frameCount % 60 == 0)
        {
            FindAllSpaceships();
            return;
        }

        if (spaceships.Count == 0)
        {
            // 没有飞船时隐藏轨道
            if (orbitRenderer != null)
            {
                orbitRenderer.enabled = false;
            }
            return;
        }

        // 实时更新模式
        if (realtimeUpdate && !isSpaceshipCaptured && !isTransitioningToOrbit)
        {
            CalculateAndUpdateOrbit();
        }

        // 更新轨道可视化位置（跟随行星移动）
        UpdateOrbitPosition();

        // 检测轨道捕获
        if (!isSpaceshipCaptured && !isTransitioningToOrbit)
        {
            CheckOrbitCapture();
        }

        // 处理轨道脱离输入
        HandleOrbitEscapeInput();

        // 如果飞船被捕获，维持圆周运动
        if (isSpaceshipCaptured && capturedSpaceship != null)
        {
            MaintainCircularOrbit();
        }
    }

    /// <summary>
    /// 计算并更新轨道
    /// </summary>
    public void CalculateAndUpdateOrbit()
    {
        if (spaceships.Count == 0 || isSpaceshipCaptured || isTransitioningToOrbit) return;

        // 暂时只处理第一个飞船
        Rigidbody spaceship = spaceships[0];
        if (spaceship == null) return;

        // 获取飞船速度大小
        float spaceshipSpeed = spaceship.velocity.magnitude;

        // 获取行星质量
        float planetMass = planet.mass;

        // 计算轨道半径
        currentOrbitRadius = CalculateOrbitRadius(spaceshipSpeed, planetMass);

        // 更新轨道可视化
        UpdateOrbitVisualization(currentOrbitRadius);

        hasValidOrbit = currentOrbitRadius > 0;

        // 显示调试信息
        if (showDebugInfo && hasValidOrbit)
        {
            Debug.Log($"{gameObject.name} - 速度: {spaceshipSpeed:F2} m/s, 质量: {planetMass:F0}, 轨道半径: {currentOrbitRadius:F2} m");
        }
    }

    /// <summary>
    /// 计算匀速圆周运动轨道半径
    /// </summary>
    private float CalculateOrbitRadius(float speed, float mass)
    {
        if (speed <= 0.01f) return 0;

        // 匀速圆周运动公式: v = sqrt(G * M / r) => r = G * M / v²
        float orbitRadius = (planet.gravitationalConstant * mass) / (speed * speed);

        // 限制最大最小半径
        orbitRadius = Mathf.Clamp(orbitRadius, 0.1f, 10000f);

        return orbitRadius;
    }

    /// <summary>
    /// 更新轨道可视化
    /// </summary>
    private void UpdateOrbitVisualization(float radius)
    {
        if (orbitRenderer == null || radius <= 0)
        {
            if (orbitRenderer != null)
            {
                orbitRenderer.enabled = false;
            }
            return;
        }

        orbitRenderer.enabled = true;

        Vector3 center = transform.position;
        float angleIncrement = 360f / orbitSegments;

        // 生成圆形轨道点
        for (int i = 0; i <= orbitSegments; i++)
        {
            float angle = i * angleIncrement * Mathf.Deg2Rad;
            float x = Mathf.Sin(angle) * radius;
            float z = Mathf.Cos(angle) * radius;
            Vector3 worldPosition = center + new Vector3(x, 0.1f, z); // 稍微抬高避免z-fighting
            orbitRenderer.SetPosition(i, worldPosition);
        }
    }

    /// <summary>
    /// 更新轨道位置（跟随行星移动）
    /// </summary>
    private void UpdateOrbitPosition()
    {
        if (orbitRenderer == null || !orbitRenderer.enabled || !hasValidOrbit)
            return;

        // 如果轨道半径有效，重新生成轨道点以确保跟随行星移动
        if (hasValidOrbit && currentOrbitRadius > 0)
        {
            UpdateOrbitVisualization(currentOrbitRadius);
        }
    }

    /// <summary>
    /// 检测飞船是否进入可捕获轨道范围
    /// </summary>
    private void CheckOrbitCapture()
    {
        if (spaceships.Count == 0 || isSpaceshipCaptured || !hasValidOrbit || isTransitioningToOrbit) return;

        foreach (Rigidbody spaceship in spaceships)
        {
            if (spaceship == null) continue;

            // 计算飞船与行星中心的水平距离
            Vector3 spaceshipPos = spaceship.position;
            Vector3 planetPos = transform.position;

            // 水平距离计算（忽略Y轴高度差）
            Vector2 horizontalSpaceshipPos = new Vector2(spaceshipPos.x, spaceshipPos.z);
            Vector2 horizontalPlanetPos = new Vector2(planetPos.x, planetPos.z);
            float horizontalDistance = Vector2.Distance(horizontalSpaceshipPos, horizontalPlanetPos);

            // 垂直距离计算（Y轴高度差）
            float verticalDistance = Mathf.Abs(spaceshipPos.y - planetPos.y);

            // 检查是否满足距离捕获条件
            float horizontalTolerance = currentOrbitRadius * captureHorizontalTolerance;

            bool distanceCondition = Mathf.Abs(horizontalDistance - currentOrbitRadius) <= horizontalTolerance &&
                                   verticalDistance <= captureVerticalTolerance;

            // 新增：检查速度方向条件
            bool angleCondition = CheckVelocityAlignment(spaceship, spaceshipPos);

            // 只有同时满足距离条件和角度条件才触发捕获
            if (distanceCondition && angleCondition)
            {
                StartSmoothOrbitCapture(spaceship);
                break; // 一次只捕获一个飞船
            }
        }
    }

    /// <summary>
    /// 新增：检查飞船速度方向与轨道切线方向的匹配度
    /// </summary>
    private bool CheckVelocityAlignment(Rigidbody spaceship, Vector3 spaceshipPos)
    {
        if (spaceship.velocity.magnitude < 0.1f) return false;

        Vector3 planetPos = transform.position;

        // 计算飞船相对于行星的方向向量（水平面）
        Vector3 toSpaceship = spaceshipPos - planetPos;
        Vector3 horizontalToSpaceship = new Vector3(toSpaceship.x, 0, toSpaceship.z).normalized;

        if (horizontalToSpaceship.magnitude < 0.01f) return false;

        // 计算轨道切线方向（垂直于径向方向）
        Vector3 tangentDirection = new Vector3(-horizontalToSpaceship.z, 0, horizontalToSpaceship.x).normalized;

        // 计算飞船速度方向（水平面）
        Vector3 spaceshipVelocity = spaceship.velocity;
        Vector3 horizontalVelocity = new Vector3(spaceshipVelocity.x, 0, spaceshipVelocity.z).normalized;

        if (horizontalVelocity.magnitude < 0.01f) return false;

        // 计算速度方向与切线方向的夹角
        float dotProduct = Vector3.Dot(horizontalVelocity, tangentDirection);
        float angle = Mathf.Acos(Mathf.Clamp(dotProduct, -1f, 1f)) * Mathf.Rad2Deg;

        // 考虑两个可能的切线方向（顺时针和逆时针）
        Vector3 oppositeTangent = -tangentDirection;
        float dotProductOpposite = Vector3.Dot(horizontalVelocity, oppositeTangent);
        float angleOpposite = Mathf.Acos(Mathf.Clamp(dotProductOpposite, -1f, 1f)) * Mathf.Rad2Deg;

        // 取较小的角度
        float minAngle = Mathf.Min(angle, angleOpposite);

        // 显示调试信息
        if (showDebugInfo && minAngle <= captureAngleTolerance * 1.5f) // 只显示接近条件的情况
        {
            Debug.Log($"速度方向检测 - 最小角度: {minAngle:F1}°, 容差: {captureAngleTolerance:F1}°, 符合条件: {minAngle <= captureAngleTolerance}");
        }

        return minAngle <= captureAngleTolerance;
    }

    /// <summary>
    /// 开始平滑轨道捕获
    /// </summary>
    private void StartSmoothOrbitCapture(Rigidbody spaceship)
    {
        if (isSpaceshipCaptured || isTransitioningToOrbit) return;

        isTransitioningToOrbit = true;
        capturedSpaceship = spaceship;
        orbitCenter = transform.position;
        transitionProgress = 0f;

        // 计算轨道参数
        CalculateOrbitParameters(spaceship);

        // 设置飞船轨道状态
        SpaceshipOrbitState orbitState = spaceship.GetComponent<SpaceshipOrbitState>();
        if (orbitState == null)
        {
            orbitState = spaceship.gameObject.AddComponent<SpaceshipOrbitState>();
        }
        orbitState.SetOrbiting(true, this);

        // 开始平滑过渡
        if (transitionCoroutine != null)
            StopCoroutine(transitionCoroutine);
        transitionCoroutine = StartCoroutine(SmoothTransitionToOrbit());

        // 可视化反馈
        SetOrbitColor(Color.yellow); // 黄色表示正在过渡

        if (showDebugInfo)
        {
            Debug.Log($"开始平滑轨道捕获，过渡时间: {smoothTransitionTime}秒");
        }
    }

    /// <summary>
    /// 计算轨道参数
    /// </summary>
    private void CalculateOrbitParameters(Rigidbody spaceship)
    {
        Vector3 spaceshipPos = spaceship.position;

        // 计算当前轨道半径（水平距离）
        Vector2 horizontalSpaceshipPos = new Vector2(spaceshipPos.x, spaceshipPos.z);
        Vector2 horizontalPlanetPos = new Vector2(orbitCenter.x, orbitCenter.z);
        float currentRadius = Vector2.Distance(horizontalSpaceshipPos, horizontalPlanetPos);

        // 使用当前半径或计算的理论半径
        float targetRadius = hasValidOrbit ? currentOrbitRadius : currentRadius;

        // 计算轨道速度
        float orbitalSpeed = Mathf.Sqrt(planet.gravitationalConstant * planet.mass / targetRadius);
        orbitAngularVelocity = orbitalSpeed / targetRadius;
    }

    /// <summary>
    /// 平滑过渡到轨道的协程
    /// </summary>
    private System.Collections.IEnumerator SmoothTransitionToOrbit()
    {
        float startTime = Time.time;
        Vector3 initialPosition = capturedSpaceship.position;
        Vector3 initialVelocity = capturedSpaceship.velocity;

        while (transitionProgress < 1.0f && capturedSpaceship != null)
        {
            transitionProgress = (Time.time - startTime) / smoothTransitionTime;
            float smoothT = Mathf.SmoothStep(0, 1, transitionProgress);

            // 平滑调整位置到理论轨道
            Vector3 currentPos = capturedSpaceship.position;
            Vector3 toCenter = currentPos - orbitCenter;
            Vector3 horizontalToCenter = new Vector3(toCenter.x, 0, toCenter.z);

            if (horizontalToCenter.magnitude > 0.01f)
            {
                Vector3 targetHorizontalPos = horizontalToCenter.normalized * currentOrbitRadius;
                Vector3 newHorizontalPos = Vector3.Lerp(horizontalToCenter, targetHorizontalPos, smoothT);

                // 保持当前高度
                float currentHeight = currentPos.y;
                Vector3 newPos = orbitCenter + new Vector3(newHorizontalPos.x, 0, newHorizontalPos.z);
                newPos.y = Mathf.Lerp(currentPos.y, orbitCenter.y, smoothT * 0.5f); // 缓慢调整高度

                capturedSpaceship.MovePosition(newPos);
            }

            // 平滑调整速度到轨道速度
            Vector3 toSpaceship = capturedSpaceship.position - orbitCenter;
            Vector3 horizontalToSpaceship = new Vector3(toSpaceship.x, 0, toSpaceship.z).normalized;
            Vector3 tangentDirection = new Vector3(-horizontalToSpaceship.z, 0, horizontalToSpaceship.x).normalized;

            // 确定正确的轨道方向（与当前速度方向匹配）
            Vector3 currentHorizontalVelocity = new Vector3(initialVelocity.x, 0, initialVelocity.z);
            if (Vector3.Dot(tangentDirection, currentHorizontalVelocity.normalized) < 0)
            {
                tangentDirection = -tangentDirection;
            }

            float targetSpeed = Mathf.Sqrt(planet.gravitationalConstant * planet.mass / currentOrbitRadius);
            Vector3 targetVelocity = tangentDirection * targetSpeed;

            capturedSpaceship.velocity = Vector3.Lerp(initialVelocity, targetVelocity, smoothT);

            yield return null;
        }

        // 过渡完成
        CompleteOrbitCapture();
    }

    /// <summary>
    /// 完成轨道捕获
    /// </summary>
    private void CompleteOrbitCapture()
    {
        isTransitioningToOrbit = false;
        isSpaceshipCaptured = true;

        if (capturedSpaceship != null)
        {
            // 设置精确的轨道速度
            Vector3 toSpaceship = capturedSpaceship.position - orbitCenter;
            Vector3 horizontalToSpaceship = new Vector3(toSpaceship.x, 0, toSpaceship.z).normalized;
            Vector3 tangentDirection = new Vector3(-horizontalToSpaceship.z, 0, horizontalToSpaceship.x).normalized;

            // 确定正确的轨道方向
            Vector3 currentHorizontalVelocity = new Vector3(capturedSpaceship.velocity.x, 0, capturedSpaceship.velocity.z);
            if (Vector3.Dot(tangentDirection, currentHorizontalVelocity.normalized) < 0)
            {
                tangentDirection = -tangentDirection;
            }

            float orbitalSpeed = Mathf.Sqrt(planet.gravitationalConstant * planet.mass / currentOrbitRadius);
            capturedSpaceship.velocity = tangentDirection * orbitalSpeed;
        }

        // 可视化反馈
        SetOrbitColor(Color.green); // 绿色表示已捕获

        if (showDebugInfo)
        {
            Debug.Log("轨道捕获完成！");
        }
    }

    /// <summary>
    /// 维持圆周运动
    /// </summary>
    private void MaintainCircularOrbit()
    {
        if (capturedSpaceship == null)
        {
            isSpaceshipCaptured = false;
            return;
        }

        Vector3 spaceshipPos = capturedSpaceship.position;
        Vector3 centerToShip = spaceshipPos - orbitCenter;

        // 计算当前角度
        float currentAngle = Mathf.Atan2(centerToShip.z, centerToShip.x);

        // 应用角速度更新角度
        currentAngle += orbitAngularVelocity * Time.deltaTime;

        // 计算新的位置（保持圆形轨道）
        float newX = orbitCenter.x + Mathf.Cos(currentAngle) * currentOrbitRadius;
        float newZ = orbitCenter.z + Mathf.Sin(currentAngle) * currentOrbitRadius;

        // 保持当前高度
        float newY = spaceshipPos.y;

        Vector3 newPosition = new Vector3(newX, newY, newZ);

        // 直接设置位置（绕过物理引擎）
        capturedSpaceship.MovePosition(newPosition);

        // 计算切向速度方向
        Vector3 tangentDirection = new Vector3(-Mathf.Sin(currentAngle), 0, Mathf.Cos(currentAngle));
        Vector3 orbitalVelocity = tangentDirection * (orbitAngularVelocity * currentOrbitRadius);

        // 保持垂直速度不变，只设置水平速度
        capturedSpaceship.velocity = new Vector3(orbitalVelocity.x, capturedSpaceship.velocity.y, orbitalVelocity.z);
    }

    /// <summary>
    /// 处理轨道脱离输入
    /// </summary>
    private void HandleOrbitEscapeInput()
    {
        if (isSpaceshipCaptured && Input.GetKeyDown(KeyCode.E))
        {
            ReleaseSpaceship();
        }
    }

    /// <summary>
    /// 释放飞船从轨道中脱离
    /// </summary>
    private void ReleaseSpaceship()
    {
        if (!isSpaceshipCaptured || capturedSpaceship == null) return;

        // 移除轨道状态标记
        SpaceshipOrbitState orbitState = capturedSpaceship.GetComponent<SpaceshipOrbitState>();
        if (orbitState != null)
        {
            orbitState.SetOrbiting(false, null);
        }

        if (showDebugInfo)
        {
            Debug.Log($"飞船已从 {gameObject.name} 的轨道脱离！");
        }

        // 重置状态
        isSpaceshipCaptured = false;
        capturedSpaceship = null;

        // 恢复轨道颜色
        SetOrbitColor(new Color(0.2f, 0.8f, 1f, 0.7f));
    }

    /// <summary>
    /// 设置轨道颜色
    /// </summary>
    public void SetOrbitColor(Color newColor)
    {
        orbitColor = newColor;
        if (orbitRenderer != null)
        {
            orbitRenderer.startColor = orbitColor;
            orbitRenderer.endColor = orbitColor;
        }
    }

    /// <summary>
    /// 手动刷新飞船列表
    /// </summary>
    public void RefreshSpaceshipsList()
    {
        FindAllSpaceships();
    }

    /// <summary>
    /// 切换轨道显示/隐藏
    /// </summary>
    public void ToggleOrbitVisibility()
    {
        if (orbitRenderer != null)
        {
            orbitRenderer.enabled = !orbitRenderer.enabled;
        }
    }

    /// <summary>
    /// 获取当前计算的轨道半径
    /// </summary>
    public float GetCurrentOrbitRadius()
    {
        return currentOrbitRadius;
    }

    /// <summary>
    /// 检查是否有有效的轨道计算
    /// </summary>
    public bool HasValidOrbit()
    {
        return hasValidOrbit;
    }

    /// <summary>
    /// 检查飞船是否被当前行星捕获
    /// </summary>
    public bool IsSpaceshipCaptured()
    {
        return isSpaceshipCaptured;
    }

    // Inspector值变化回调
    private void OnValidate()
    {
        if (orbitRenderer != null && Application.isPlaying)
        {
            // 更新线段数量
            if (orbitRenderer.positionCount != orbitSegments + 1)
            {
                orbitRenderer.positionCount = orbitSegments + 1;
                if (hasValidOrbit)
                {
                    UpdateOrbitVisualization(currentOrbitRadius);
                }
            }

            // 更新颜色和宽度
            orbitRenderer.startColor = orbitColor;
            orbitRenderer.endColor = orbitColor;
            orbitRenderer.startWidth = lineWidth;
            orbitRenderer.endWidth = lineWidth;
        }
    }

    // 在Scene视图中绘制Gizmos
    private void OnDrawGizmosSelected()
    {
        if (hasValidOrbit && currentOrbitRadius > 0)
        {
            Gizmos.color = new Color(orbitColor.r, orbitColor.g, orbitColor.b, 0.3f);
            Gizmos.DrawWireSphere(transform.position, currentOrbitRadius);
        }
    }

    // 清理资源
    private void OnDestroy()
    {
        if (transitionCoroutine != null)
        {
            StopCoroutine(transitionCoroutine);
        }
    }
}