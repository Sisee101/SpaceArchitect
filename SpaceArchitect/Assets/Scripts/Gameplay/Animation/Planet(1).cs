using UnityEngine;
using System.Collections.Generic;

public class Planet : MonoBehaviour
{
    [Header("行星属性")]
    public float mass = 100f;
    [HideInInspector]
    public float planetRadius = 30f;
    public Color planetColor = Color.white;

    [Header("质量-半径关系")]
    [Tooltip("质量对半径的影响系数，值越大半径随质量增长越快")]
    public float massToRadiusFactor = 0.5f;
    [Tooltip("基准半径，用于控制整体大小比例")]
    public float baseRadius = 1f;

    [Header("引力设置")]
    [Tooltip("引力常数，控制引力强度")]
    public float gravitationalConstant = 0.6674f;
    [Tooltip("引力作用的最大距离")]
    public float maxGravityDistance = 1000f;

    [Header("运行时可视化设置")]
    [Tooltip("是否在运行时显示引力范围")]
    public bool showRuntimeGravityRange = true;
    [Tooltip("引力范围可视化颜色")]
    public Color gravityRangeColor = new Color(0f, 1f, 0f, 0.3f);
    [Tooltip("可视化圆的线段数量（越多越平滑）")]
    [Range(8, 128)]
    public int circleSegments = 64;
    [Tooltip("可视化线宽度")]
    public float lineWidth = 2f;

    [Header("选中和交互设置")]
    [Tooltip("选中时的高亮颜色")]
    public Color selectedColor = new Color(1f, 0.8f, 0f, 1f);
    [Tooltip("选中时的发光强度")]
    public float selectedEmissionIntensity = 2f;
    [Tooltip("质量调整步长")]
    public float massAdjustStep = 10f;
    [Tooltip("最小质量限制")]
    public float minMass = 10f;
    [Tooltip("最大质量限制")]
    public float maxMass = 1000f;

    [Header("引用")]
    public Rigidbody planetRigidbody;
    private Renderer planetRenderer;

    private List<Rigidbody> spaceships = new List<Rigidbody>();
    private bool hasFoundSpaceships = false;

    // 运行时可视化相关变量
    private LineRenderer gravityRangeRenderer;
    private Material lineMaterial;

    // 用于检测值变化的变量
    private float lastMaxGravityDistance;
    private bool needsVisualUpdate = false;

    // 新增：用于世界坐标系下的可视化
    private Vector3 lastPlanetPosition;

    // 新增：选中状态相关变量
    private bool isSelected = false;
    private Material originalMaterial;
    private Material selectedMaterial;
    private Color originalEmissionColor;
    private static Planet currentlySelectedPlanet = null;

    // 新增：鼠标点击检测
    private Camera mainCamera;
    private RaycastHit hitInfo;

    void Start()
    {
        // 获取或添加组件
        planetRigidbody = GetComponent<Rigidbody>();
        planetRenderer = GetComponent<Renderer>();
        mainCamera = Camera.main;

        // 初始化运行时引力范围可视化
        InitializeRuntimeVisualization();

        // 根据初始质量计算初始半径
        UpdateRadiusFromMass();

        // 初始化行星外观
        UpdatePlanetVisual();

        // 设置物理属性
        if (planetRigidbody != null)
        {
            planetRigidbody.mass = mass;
            planetRigidbody.useGravity = false;
            planetRigidbody.constraints = RigidbodyConstraints.FreezeAll;
        }

        // 记录初始值用于变化检测
        lastMaxGravityDistance = maxGravityDistance;
        lastPlanetPosition = transform.position;

        // 查找所有飞船
        FindAllSpaceships();

        // 初始化选中状态材质
        InitializeSelectionMaterials();
    }

    void FixedUpdate()
    {
        // 实时计算并应用引力
        ApplyGravityToSpaceships();
    }

    void Update()
    {
        // 检测Max Gravity Distance值是否发生变化
        if (Mathf.Abs(maxGravityDistance - lastMaxGravityDistance) > 0.01f)
        {
            lastMaxGravityDistance = maxGravityDistance;
            needsVisualUpdate = true;
        }

        // 检测行星位置是否发生变化
        if (Vector3.Distance(transform.position, lastPlanetPosition) > 0.01f)
        {
            lastPlanetPosition = transform.position;
            needsVisualUpdate = true;
        }

        // 更新运行时可视化
        UpdateRuntimeVisualization();

        // 如果需要更新可视化，立即执行
        if (needsVisualUpdate)
        {
            UpdateGravityCircle();
            needsVisualUpdate = false;
        }

        // 新增：鼠标点击检测
        HandleMouseClick();

        // 新增：键盘输入处理（仅当被选中时）
        if (isSelected)
        {
            HandleKeyboardInput();
        }
    }

    /// <summary>
    /// 初始化运行时引力范围可视化
    /// </summary>
    private void InitializeRuntimeVisualization()
    {
        // 添加或获取LineRenderer组件
        gravityRangeRenderer = gameObject.GetComponent<LineRenderer>();
        if (gravityRangeRenderer == null)
        {
            gravityRangeRenderer = gameObject.AddComponent<LineRenderer>();
        }

        // 配置LineRenderer属性
        gravityRangeRenderer.positionCount = circleSegments + 1;
        gravityRangeRenderer.loop = true;
        gravityRangeRenderer.useWorldSpace = true;

        // 设置材质
        lineMaterial = new Material(Shader.Find("Sprites/Default"));
        gravityRangeRenderer.material = lineMaterial;

        // 设置线条属性
        gravityRangeRenderer.startColor = gravityRangeColor;
        gravityRangeRenderer.endColor = gravityRangeColor;
        gravityRangeRenderer.startWidth = lineWidth;
        gravityRangeRenderer.endWidth = lineWidth;

        // 初始更新可视化
        UpdateGravityCircle();
        gravityRangeRenderer.enabled = showRuntimeGravityRange;
    }

    /// <summary>
    /// 更新引力范围圆形
    /// </summary>
    private void UpdateGravityCircle()
    {
        if (gravityRangeRenderer == null) return;

        Vector3 center = transform.position;
        float angleIncrement = 360f / circleSegments;

        for (int i = 0; i <= circleSegments; i++)
        {
            float angle = i * angleIncrement * Mathf.Deg2Rad;
            float x = Mathf.Sin(angle) * maxGravityDistance;
            float z = Mathf.Cos(angle) * maxGravityDistance;
            Vector3 worldPosition = center + new Vector3(x, 0.1f, z);
            gravityRangeRenderer.SetPosition(i, worldPosition);
        }

        gravityRangeRenderer.enabled = showRuntimeGravityRange;
    }

    /// <summary>
    /// 更新运行时可视化
    /// </summary>
    private void UpdateRuntimeVisualization()
    {
        if (gravityRangeRenderer == null) return;

        gravityRangeRenderer.enabled = showRuntimeGravityRange;

        if (showRuntimeGravityRange)
        {
            gravityRangeRenderer.startColor = gravityRangeColor;
            gravityRangeRenderer.endColor = gravityRangeColor;
            gravityRangeRenderer.startWidth = lineWidth;
            gravityRangeRenderer.endWidth = lineWidth;

            if (gravityRangeRenderer.positionCount != circleSegments + 1)
            {
                gravityRangeRenderer.positionCount = circleSegments + 1;
                UpdateGravityCircle();
            }
        }
    }

    /// <summary>
    /// 根据质量更新半径
    /// </summary>
    private void UpdateRadiusFromMass()
    {
        planetRadius = baseRadius * Mathf.Pow(mass, massToRadiusFactor);
    }

    /// <summary>
    /// 更新行星视觉表现
    /// </summary>
    public void UpdatePlanetVisual()
    {
        transform.localScale = Vector3.one * planetRadius * 0.1f;

        if (planetRenderer != null)
        {
            Material material = planetRenderer.material;
            if (material == null)
            {
                material = new Material(Shader.Find("Standard"));
                planetRenderer.material = material;
            }
            material.color = planetColor;
            material.EnableKeyword("_EMISSION");
            material.SetColor("_EmissionColor", planetColor * 0.3f);
        }

        needsVisualUpdate = true;
    }

    /// <summary>
    /// 初始化选中状态材质
    /// </summary>
    private void InitializeSelectionMaterials()
    {
        if (planetRenderer != null)
        {
            // 保存原始材质
            originalMaterial = planetRenderer.material;

            // 创建选中材质（基于原始材质）
            selectedMaterial = new Material(originalMaterial);
            originalEmissionColor = originalMaterial.GetColor("_EmissionColor");
        }
    }

    /// <summary>
    /// 处理鼠标点击
    /// </summary>
    private void HandleMouseClick()
    {
        if (Input.GetMouseButtonDown(0)) // 左键点击
        {
            Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);

            if (Physics.Raycast(ray, out hitInfo))
            {
                Planet clickedPlanet = hitInfo.collider.GetComponent<Planet>();

                if (clickedPlanet != null && clickedPlanet == this)
                {
                    // 取消之前选中的行星
                    if (currentlySelectedPlanet != null && currentlySelectedPlanet != this)
                    {
                        currentlySelectedPlanet.DeselectPlanet();
                    }

                    // 选中当前行星
                    SelectPlanet();
                    currentlySelectedPlanet = this;
                }
                else if (clickedPlanet == null && currentlySelectedPlanet == this)
                {
                    // 点击空白处取消选中
                    DeselectPlanet();
                    currentlySelectedPlanet = null;
                }
            }
            else if (currentlySelectedPlanet == this)
            {
                // 点击空白处取消选中
                DeselectPlanet();
                currentlySelectedPlanet = null;
            }
        }
    }

    /// <summary>
    /// 处理键盘输入
    /// </summary>
    private void HandleKeyboardInput()
    {
        if (Input.GetKeyDown(KeyCode.UpArrow))
        {
            IncreaseMass();
        }
        else if (Input.GetKeyDown(KeyCode.DownArrow))
        {
            DecreaseMass();
        }

        // 连续按键支持（按住不放）
        if (Input.GetKey(KeyCode.UpArrow) && Time.frameCount % 5 == 0)
        {
            IncreaseMass();
        }
        else if (Input.GetKey(KeyCode.DownArrow) && Time.frameCount % 5 == 0)
        {
            DecreaseMass();
        }
    }

    /// <summary>
    /// 选中行星
    /// </summary>
    public void SelectPlanet()
    {
        if (isSelected) return;

        isSelected = true;

        if (planetRenderer != null && selectedMaterial != null)
        {
            // 应用选中材质
            planetRenderer.material = selectedMaterial;

            // 增强发光效果
            selectedMaterial.EnableKeyword("_EMISSION");
            selectedMaterial.SetColor("_EmissionColor", selectedColor * selectedEmissionIntensity);
        }

        Debug.Log($"选中行星: {gameObject.name}, 质量: {mass}");
    }

    /// <summary>
    /// 取消选中行星
    /// </summary>
    public void DeselectPlanet()
    {
        if (!isSelected) return;

        isSelected = false;

        if (planetRenderer != null && originalMaterial != null)
        {
            // 恢复原始材质
            planetRenderer.material = originalMaterial;

            // 恢复原始发光设置
            originalMaterial.SetColor("_EmissionColor", originalEmissionColor);
        }

        Debug.Log($"取消选中行星: {gameObject.name}");
    }

    /// <summary>
    /// 增加质量
    /// </summary>
    public void IncreaseMass()
    {
        float newMass = Mathf.Min(mass + massAdjustStep, maxMass);
        if (newMass != mass)
        {
            UpdateMass(newMass);
            Debug.Log($"增加质量: {mass}");
        }
    }

    /// <summary>
    /// 减少质量
    /// </summary>
    public void DecreaseMass()
    {
        float newMass = Mathf.Max(mass - massAdjustStep, minMass);
        if (newMass != mass)
        {
            UpdateMass(newMass);
            Debug.Log($"减少质量: {mass}");
        }
    }

    /// <summary>
    /// 更新质量
    /// </summary>
    public void UpdateMass(float newMass)
    {
        mass = newMass;
        UpdateRadiusFromMass();

        if (planetRigidbody != null)
        {
            planetRigidbody.mass = mass;
        }
        UpdatePlanetVisual();
    }

    /// <summary>
    /// 查找所有tag为spaceship的物体
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

                // 检查飞船是否已有轨道状态组件，没有则添加
                SpaceshipOrbitState orbitState = spaceship.GetComponent<SpaceshipOrbitState>();
                if (orbitState == null)
                {
                    orbitState = spaceship.AddComponent<SpaceshipOrbitState>();
                }
            }
        }

        hasFoundSpaceships = spaceships.Count > 0;

        if (!hasFoundSpaceships)
        {
            Debug.LogWarning($"行星 {gameObject.name} 未找到有效的飞船物体！");
        }
        else
        {
            Debug.Log($"行星 {gameObject.name} 找到 {spaceships.Count} 个飞船");
        }
    }

    /// <summary>
    /// 对所有飞船应用引力
    /// </summary>
    private void ApplyGravityToSpaceships()
    {
        if (!hasFoundSpaceships) return;

        foreach (Rigidbody spaceship in spaceships)
        {
            if (spaceship == null) continue;

            // 检查飞船是否处于轨道状态
            SpaceshipOrbitState orbitState = spaceship.GetComponent<SpaceshipOrbitState>();
            if (orbitState != null && orbitState.IsOrbiting())
            {
                // 如果飞船正在绕某行星轨道运行，且不是绕当前行星，则跳过引力计算
                if (orbitState.GetCurrentOrbit() != null && orbitState.GetCurrentOrbit().gameObject != this.gameObject)
                {
                    continue;
                }
                // 如果飞船正在绕当前行星轨道运行，也跳过引力计算（由轨道计算器控制）
                else if (orbitState.GetCurrentOrbit() != null && orbitState.GetCurrentOrbit().gameObject == this.gameObject)
                {
                    continue;
                }
            }

            // 应用引力
            ApplyGravity(spaceship);
        }
    }

    /// <summary>
    /// 对单个飞船应用引力
    /// </summary>
    private void ApplyGravity(Rigidbody spaceship)
    {
        Vector3 gravityDirection = (spaceship.position - planetRigidbody.position).normalized;
        float distance = Vector3.Distance(spaceship.position, planetRigidbody.position);

        if (distance > maxGravityDistance || distance < 0.1f) return;

        float gravityForce = gravitationalConstant * (planetRigidbody.mass * spaceship.mass) / Mathf.Pow(distance, 2);
        Vector3 horizontalForce = new Vector3(gravityDirection.x, 0f, gravityDirection.z).normalized * gravityForce;

        spaceship.AddForce(horizontalForce);
        planetRigidbody.AddForce(-horizontalForce);
    }

    /// <summary>
    /// 手动刷新飞船列表
    /// </summary>
    public void RefreshSpaceshipsList()
    {
        FindAllSpaceships();
    }

    // Inspector中修改值时自动更新
    private void OnValidate()
    {
        if (Application.isPlaying)
        {
            UpdateMass(mass);

            if (Mathf.Abs(maxGravityDistance - lastMaxGravityDistance) > 0.01f)
            {
                lastMaxGravityDistance = maxGravityDistance;
                UpdateGravityCircle();
            }
        }

        gravityRangeColor.a = Mathf.Clamp01(gravityRangeColor.a);
    }

    // Scene视图中的可视化
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, maxGravityDistance);

        Gizmos.color = new Color(1f, 0.5f, 0f, 0.3f);
        Gizmos.DrawWireSphere(transform.position, planetRadius * 2f);

        // 选中状态可视化
        if (isSelected)
        {
            Gizmos.color = selectedColor;
            Gizmos.DrawWireSphere(transform.position, planetRadius * 0.15f);
        }
    }

    // 清理资源
    private void OnDestroy()
    {
        if (lineMaterial != null)
        {
            DestroyImmediate(lineMaterial);
        }

        // 如果当前选中的行星被销毁，清空静态引用
        if (currentlySelectedPlanet == this)
        {
            currentlySelectedPlanet = null;
        }
    }
}