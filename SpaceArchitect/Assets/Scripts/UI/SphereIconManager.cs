using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections;
using System.Collections.Generic;
using System;

/// <summary>
/// Sphere图标管理器
/// 管理Sphere右上角图标的显示、隐藏和位置更新
/// </summary>
public class SphereIconManager : MonoBehaviour
{
    [Header("Sphere引用（需要显示图标的Sphere）")]
    [Tooltip("在此列表中添加需要显示图标的Sphere，可以动态调整数量")]
    [SerializeField] private List<GameObject> spheres = new List<GameObject>();
    
    [Header("UI引用")]
    [SerializeField] private Canvas worldSpaceCanvas; // World Space Canvas（如果为空，会自动查找或创建）
    [SerializeField] private GameObject iconPrefab; // Icon预制体（必须配置）
    
    [Header("相机引用")]
    [Tooltip("图标面向的相机（如果为空，优先使用worldSpaceCanvas.worldCamera，否则使用Camera.main）")]
    [SerializeField] private Camera targetCamera; // 目标相机（可选，用于明确指定图标面向的相机）
    
    [Header("图标位置设置")]
    [Tooltip("向左偏移倍数（相对于Sphere半径），用于控制图标在Sphere左侧的距离")]
    [SerializeField] private float leftOffsetMultiplier = 1.2f; // 向左偏移倍数
    
    [Tooltip("向上偏移倍数（相对于Sphere半径），用于控制图标在Sphere上方的距离")]
    [SerializeField] private float upOffsetMultiplier = 1.2f; // 向上偏移倍数
    
    [SerializeField] private Vector3 iconBaseScale = new Vector3(0.01f, 0.01f, 0.01f); // 图标基础大小（World Space模式）
    [SerializeField] private bool useWorldSpaceDirections = true; // 使用世界坐标方向（true）还是Sphere本地方向（false）
    
    [Header("图标视觉大小设置")]
    [SerializeField] private bool maintainVisualSize = true; // 是否保持视觉大小不变（根据相机距离动态调整）
    [SerializeField] private float referenceDistance = 10f; // 参考距离（在此距离时图标使用基础大小）
    
    [Header("控制设置")]
    [SerializeField] private KeyCode toggleKey = KeyCode.T; // 切换显示/隐藏的按键
    
    [Header("更新设置")]
    [SerializeField] private bool updateEveryFrame = true; // 是否每帧更新图标位置（如果Sphere会移动）
    [SerializeField] private float updateInterval = 0.1f; // 更新间隔（如果updateEveryFrame为false）
    
    [Header("图标朝向设置")]
    [SerializeField] private bool faceCamera = true; // 图标是否始终面向相机（Billboard效果）
    
    [Header("订单数据配置")]
    [SerializeField] private SphereOrderDataConfig orderDataConfig; // 订单数据集引用（必须配置）
    
    [Header("面板引用")]
    [SerializeField] private SphereInfoPanel infoPanel; // 信息面板引用（必须配置）
    
    [Header("任务管理器引用")]
    [Tooltip("任务管理器（用于检查任务是否已完成，如果为空则自动查找）")]
    [SerializeField] private TaskManager taskManager; // 任务管理器引用
    
    [Header("气泡消失动画设置")]
    [Tooltip("气泡消失动画的时长（秒）")]
    [SerializeField] private float hideAnimationDuration = 0.5f; // 气泡消失动画时长
    
    [Header("Sphere发光设置")]
    [Tooltip("是否启用Sphere发光效果")]
    [SerializeField] private bool enableSphereGlow = true; // 是否启用Sphere发光
    [Tooltip("发光颜色（HDR颜色，值可以超过1.0以获得更亮的效果）")]
    [SerializeField] private Color glowColor = new Color(0.3f, 0.5f, 1f, 1f); // 发光颜色（默认淡蓝色）
    [Tooltip("发光强度（0-2，值越大越亮）")]
    [SerializeField] private float glowIntensity = 0.3f; // 发光强度
    
    // 私有变量
    private Dictionary<GameObject, GameObject> sphereIconMap; // Sphere到Icon的映射字典
    private Dictionary<GameObject, Material> sphereOriginalMaterials; // Sphere原始材质字典（用于恢复）
    private Dictionary<GameObject, Color> sphereOriginalEmissionColors; // Sphere原始发光颜色字典
    private bool iconsVisible = false; // 图标是否显示
    private float lastUpdateTime = 0f; // 上次更新时间
    
    void Start()
    {
        // 初始化字典
        sphereIconMap = new Dictionary<GameObject, GameObject>();
        sphereOriginalMaterials = new Dictionary<GameObject, Material>();
        sphereOriginalEmissionColors = new Dictionary<GameObject, Color>();
        
        // 如果没有指定World Space Canvas，尝试自动查找或创建
        if (worldSpaceCanvas == null)
        {
            worldSpaceCanvas = FindObjectOfType<Canvas>();
            if (worldSpaceCanvas == null || worldSpaceCanvas.renderMode != RenderMode.WorldSpace)
            {
                // 创建新的World Space Canvas
                CreateWorldSpaceCanvas();
            }
        }
        
        // 验证必要引用
        if (iconPrefab == null)
        {
            Debug.LogError("SphereIconManager: iconPrefab未配置！请在Inspector中指定Icon预制体。");
        }
        
        if (spheres == null || spheres.Count == 0)
        {
            Debug.LogWarning("SphereIconManager: Sphere列表为空，请至少添加一个Sphere引用。");
        }
        else
        {
            // 检查是否有空引用
            for (int i = 0; i < spheres.Count; i++)
            {
                if (spheres[i] == null)
                {
                    Debug.LogWarning($"SphereIconManager: 第 {i + 1} 个Sphere引用为空，请检查配置。");
                }
            }
        }
        
        if (orderDataConfig == null)
        {
            Debug.LogWarning("SphereIconManager: orderDataConfig未配置！请在Inspector中指定SphereOrderDataConfig资源。");
        }
        
        if (infoPanel == null)
        {
            Debug.LogWarning("SphereIconManager: infoPanel未配置！请在Inspector中指定SphereInfoPanel引用。");
        }
        
        // 自动查找TaskManager（如果未配置）
        if (taskManager == null)
        {
            taskManager = FindObjectOfType<TaskManager>();
            if (taskManager == null)
            {
                Debug.LogWarning("SphereIconManager: 未找到TaskManager，将无法检查任务完成状态，所有气泡都会显示");
            }
        }
        
        // 场景加载后自动显示气泡
        // 延迟一帧显示，确保所有初始化完成
        StartCoroutine(AutoShowIconsOnStart());
    }
    
    /// <summary>
    /// 在场景加载后自动显示气泡（延迟一帧，确保所有初始化完成）
    /// </summary>
    private IEnumerator AutoShowIconsOnStart()
    {
        // 等待一帧，确保所有对象的初始化都已完成
        yield return null;
        
        // 场景检查：只在 MainHub 场景中工作
        string currentSceneName = SceneManager.GetActiveScene().name;
        bool isMainHubScene = currentSceneName.StartsWith("0") && currentSceneName.Contains("_MainHub");
        
        // 如果当前场景不是 MainHub，不自动显示气泡
        if (!isMainHubScene)
        {
            yield break;
        }
        
        // 检查必要引用是否都已配置
        if (iconPrefab != null && worldSpaceCanvas != null)
        {
            // 自动显示气泡
            ShowIcons();
            Debug.Log("SphereIconManager: 场景加载完成，气泡已自动显示");
        }
        else
        {
            Debug.LogWarning("SphereIconManager: 无法自动显示气泡，iconPrefab 或 worldSpaceCanvas 未配置");
        }
    }
    
    void Update()
    {
        // 场景检查：只在 MainHub 场景中工作
        string currentSceneName = SceneManager.GetActiveScene().name;
        bool isMainHubScene = currentSceneName.StartsWith("0") && currentSceneName.Contains("_MainHub");
        
        // 如果当前场景不是 MainHub，禁用此脚本的功能
        if (!isMainHubScene)
        {
            return;
        }
        
        // 检测T键按下
        if (Input.GetKeyDown(toggleKey))
        {
            ToggleIcons();
        }
        
        // 更新图标位置
        if (iconsVisible)
        {
            if (updateEveryFrame)
            {
                UpdateIconPositions();
            }
            else
            {
                // 按间隔更新
                if (Time.time - lastUpdateTime >= updateInterval)
                {
                    UpdateIconPositions();
                    lastUpdateTime = Time.time;
                }
            }
        }
    }
    
    /// <summary>
    /// 创建World Space Canvas（如果不存在）
    /// </summary>
    private void CreateWorldSpaceCanvas()
    {
        GameObject canvasObj = new GameObject("WorldSpaceCanvas");
        canvasObj.transform.SetParent(transform);
        
        Canvas canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        canvas.worldCamera = Camera.main;
        
        // 设置Canvas的Scale（World Space模式需要小的Scale）
        RectTransform canvasRect = canvasObj.GetComponent<RectTransform>();
        if (canvasRect != null)
        {
            canvasRect.localScale = new Vector3(0.01f, 0.01f, 0.01f);
        }
        
        // 添加CanvasScaler和GraphicRaycaster（可选，但推荐）
        canvasObj.AddComponent<CanvasScaler>();
        canvasObj.AddComponent<GraphicRaycaster>();
        
        worldSpaceCanvas = canvas;
        Debug.Log("SphereIconManager: 已自动创建World Space Canvas");
    }
    
    /// <summary>
    /// 切换图标显示/隐藏
    /// </summary>
    private void ToggleIcons()
    {
        if (iconsVisible)
        {
            HideIcons();
        }
        else
        {
            ShowIcons();
        }
    }
    
    /// <summary>
    /// 显示所有图标（只显示未完成任务的气泡）
    /// </summary>
    private void ShowIcons()
    {
        if (iconPrefab == null || worldSpaceCanvas == null)
        {
            Debug.LogError("SphereIconManager: iconPrefab或worldSpaceCanvas未配置！");
            return;
        }
        
        Debug.Log($"SphereIconManager: 开始显示图标，Canvas: {worldSpaceCanvas.name}, RenderMode: {worldSpaceCanvas.renderMode}, Canvas Scale: {worldSpaceCanvas.transform.localScale}");
        
        // 为每个Sphere创建图标（只创建未完成任务的气泡）
        foreach (GameObject sphere in spheres)
        {
            if (sphere != null)
            {
                CreateIconForSphereIfNotCompleted(sphere);
            }
        }
        
        iconsVisible = true;
        Debug.Log($"SphereIconManager: 图标已显示，共创建 {sphereIconMap.Count} 个图标（已过滤已完成任务的气泡）");
    }
    
    /// <summary>
    /// 检查Sphere对应的任务是否已完成，如果未完成则创建图标
    /// </summary>
    private void CreateIconForSphereIfNotCompleted(GameObject sphere)
    {
        if (sphere == null)
        {
            return;
        }
        
        // 检查任务是否已完成
        if (IsSphereTaskCompleted(sphere.name))
        {
            Debug.Log($"SphereIconManager: Sphere {sphere.name} 的任务已完成，跳过创建气泡");
            return;
        }
        
        // 任务未完成，创建图标
        CreateIconForSphere(sphere);
    }
    
    /// <summary>
    /// 检查指定Sphere对应的任务是否已完成
    /// </summary>
    /// <param name="sphereName">Sphere名称</param>
    /// <returns>如果任务已完成返回true，否则返回false</returns>
    private bool IsSphereTaskCompleted(string sphereName)
    {
        // 如果没有订单数据配置，默认返回false（显示气泡）
        if (orderDataConfig == null)
        {
            return false;
        }
        
        // 根据Sphere名称获取订单信息
        var orderInfo = orderDataConfig.GetOrderInfoBySphereName(sphereName);
        if (orderInfo == null)
        {
            // 如果找不到订单信息，默认返回false（显示气泡）
            return false;
        }
        
        // 检查任务是否已完成
        int taskId = orderInfo.taskId;
        if (taskId < 0)
        {
            // 如果taskId无效（-1），默认返回false（显示气泡）
            return false;
        }
        
        // 优先检查CompleteOrder状态（这是最可靠的，因为它在场景切换时会被同步）
        if (orderInfo.CompleteOrder)
        {
            Debug.Log($"SphereIconManager: Sphere {sphereName} (taskId={taskId}) 的CompleteOrder为true，任务已完成");
            return true;
        }
        
        // 备用检查：通过TaskManager检查（如果TaskManager存在）
        if (taskManager != null)
        {
            bool isCompleted = taskManager.IsTaskCompleted(taskId);
            if (isCompleted)
            {
                Debug.Log($"SphereIconManager: Sphere {sphereName} (taskId={taskId}) 通过TaskManager检查，任务已完成");
                // 同步CompleteOrder状态
                orderInfo.CompleteOrder = true;
            }
            return isCompleted;
        }
        
        // 如果TaskManager不存在，只检查CompleteOrder（已经在上面检查过了）
        return false;
    }
    
    /// <summary>
    /// 隐藏所有图标
    /// </summary>
    private void HideIcons()
    {
        // 如果字典未初始化，直接返回
        if (sphereIconMap == null)
        {
            return;
        }
        
        // 关闭所有Sphere的发光效果
        if (enableSphereGlow && sphereOriginalMaterials != null)
        {
            foreach (var kvp in sphereIconMap)
            {
                if (kvp.Key != null)
                {
                    DisableSphereGlow(kvp.Key);
                }
            }
        }
        
        // 销毁所有图标
        foreach (var kvp in sphereIconMap)
        {
            if (kvp.Value != null)
            {
                Destroy(kvp.Value);
            }
        }
        
        sphereIconMap.Clear();
        iconsVisible = false;
        Debug.Log("SphereIconManager: 图标已隐藏");
    }
    
    /// <summary>
    /// 为指定Sphere创建图标
    /// </summary>
    private void CreateIconForSphere(GameObject sphere)
    {
        if (sphere == null)
        {
            Debug.LogWarning("SphereIconManager: Sphere引用为空，跳过创建图标");
            return;
        }
        
        // 如果已经存在图标，先销毁
        if (sphereIconMap.ContainsKey(sphere) && sphereIconMap[sphere] != null)
        {
            Destroy(sphereIconMap[sphere]);
        }
        
        // 实例化图标预制体
        GameObject iconObj = Instantiate(iconPrefab, worldSpaceCanvas.transform);
        
        // 设置图标位置（Sphere右上角）
        Vector3 topRightPosition = CalculateTopRightPosition(sphere);
        iconObj.transform.position = topRightPosition;
        
        // 设置图标初始大小（会在Update中根据距离动态调整）
        iconObj.transform.localScale = iconBaseScale;
        
        // 初始朝向相机（如果启用）
        if (faceCamera)
        {
            Camera cameraToFace = GetTargetCamera();
            
            if (cameraToFace != null)
            {
                Vector3 directionToCamera = cameraToFace.transform.position - iconObj.transform.position;
                if (directionToCamera != Vector3.zero)
                {
                    iconObj.transform.rotation = Quaternion.LookRotation(directionToCamera);
                }
            }
        }
        
        // 调试信息
        Debug.Log($"SphereIconManager: 为 {sphere.name} 创建图标，位置: {topRightPosition}, 基础Scale: {iconBaseScale}");
        
        // 绑定点击事件
        Button iconButton = iconObj.GetComponent<Button>();
        if (iconButton != null)
        {
            Debug.Log($"SphereIconManager: 找到Button组件，开始绑定点击事件 - Sphere: {sphere.name}");
            Debug.Log($"SphereIconManager: Button.Interactable = {iconButton.interactable}");
            
            // 获取Sphere的引用，用于点击事件
            GameObject sphereRef = sphere; // 闭包捕获
            iconButton.onClick.AddListener(() => {
                Debug.Log($"SphereIconManager: ====== Button被点击！Sphere: {sphereRef.name} =====");
                OnIconClicked(sphereRef);
            });
            
            Debug.Log($"SphereIconManager: 点击事件绑定成功");
        }
        else
        {
            Debug.LogError($"SphereIconManager: Icon预制体 {iconPrefab.name} 没有Button组件，无法处理点击事件！");
        }
        
        // 检查Image组件
        Image iconImage = iconObj.GetComponent<Image>();
        if (iconImage != null)
        {
            Debug.Log($"SphereIconManager: Icon Image组件存在，Color: {iconImage.color}, Sprite: {(iconImage.sprite != null ? iconImage.sprite.name : "null")}");
        }
        else
        {
            Debug.LogWarning($"SphereIconManager: Icon预制体 {iconPrefab.name} 没有Image组件！");
        }
        
        // 保存到字典
        sphereIconMap[sphere] = iconObj;
        
        // 启用Sphere发光效果
        if (enableSphereGlow)
        {
            EnableSphereGlow(sphere);
        }
    }
    
    /// <summary>
    /// 计算Sphere左上角的世界坐标（相对于相机视角）
    /// 方案1：使用相机坐标系，但偏移量相对于Sphere
    /// </summary>
    private Vector3 CalculateTopRightPosition(GameObject sphere)
    {
        if (sphere == null) return Vector3.zero;
        
        // 获取Sphere的Bounds（考虑MeshRenderer或Collider）
        Bounds bounds = GetSphereBounds(sphere);
        float radius = Mathf.Max(bounds.extents.x, bounds.extents.y, bounds.extents.z);
        
        // ========== 方案1：使用相机坐标系计算左上角（新实现）==========
        Camera camera = GetTargetCamera();
        if (camera != null)
        {
            // 使用相机的本地坐标系（相机的right和up方向）
            Vector3 cameraRight = camera.transform.right;    // 相机视角的右方向
            Vector3 cameraUp = camera.transform.up;           // 相机视角的上方向
            
            // 分别计算左右和上下的偏移量（基于Sphere的半径，相对于Sphere）
            float leftOffsetDistance = radius * leftOffsetMultiplier;
            float upOffsetDistance = radius * upOffsetMultiplier;
            
            // 左上角偏移（向左 + 向上，使用相机坐标系）
            Vector3 leftOffset = -cameraRight * leftOffsetDistance;  // 向左（负右方向）
            Vector3 upOffsetCamera = cameraUp * upOffsetDistance;     // 向上（使用不同变量名避免冲突）
            Vector3 topLeftOffset = leftOffset + upOffsetCamera;
            
            // 最终位置（相对于Sphere，但方向是相机坐标系）
            Vector3 finalPosCamera = sphere.transform.position + topLeftOffset;
            
            // 调试信息
            if (iconsVisible)
            {
                Debug.Log($"SphereIconManager: {sphere.name} 位置计算（相机坐标系） - Sphere位置: {sphere.transform.position}, 半径: {radius}, 左偏移倍数: {leftOffsetMultiplier}, 上偏移倍数: {upOffsetMultiplier}, 相机: {camera.name}, 最终位置: {finalPosCamera}");
            }
            
            return finalPosCamera;
        }
        
        // ========== 原有逻辑（备用，如果相机为空时使用）==========
        // 计算右上角偏移（相对于Sphere中心）
        Vector3 rightDirection;
        Vector3 upDirection;
        
        if (useWorldSpaceDirections)
        {
            // 使用世界坐标方向（确保图标在屏幕右上角方向）
            rightDirection = Vector3.right;
            upDirection = Vector3.up;
        }
        else
        {
            // 使用Sphere的本地方向
            rightDirection = sphere.transform.right;
            upDirection = sphere.transform.up;
        }
        
        // 分别计算左右和上下的偏移量（使用Sphere的半径）
        Vector3 rightOffset = rightDirection * radius * leftOffsetMultiplier;  // 使用leftOffsetMultiplier（向右为正，但这里用于右上角）
        Vector3 upOffsetWorld = upDirection * radius * upOffsetMultiplier;     // 使用upOffsetMultiplier
        Vector3 topRightOffset = rightOffset + upOffsetWorld;
        
        // 返回世界坐标
        Vector3 finalPosWorld = sphere.transform.position + topRightOffset;  // 使用不同变量名避免冲突
        
        // 调试信息
        if (iconsVisible)
        {
            Debug.Log($"SphereIconManager: {sphere.name} 位置计算（世界坐标系，相机为空） - Sphere位置: {sphere.transform.position}, 半径: {radius}, 左偏移倍数: {leftOffsetMultiplier}, 上偏移倍数: {upOffsetMultiplier}, 最终位置: {finalPosWorld}");
        }
        
        return finalPosWorld;
    }
    
    /// <summary>
    /// 获取Sphere的Bounds
    /// </summary>
    private Bounds GetSphereBounds(GameObject sphere)
    {
        // 优先使用Renderer的Bounds
        Renderer renderer = sphere.GetComponent<Renderer>();
        if (renderer != null)
        {
            return renderer.bounds;
        }
        
        // 如果没有Renderer，使用Collider的Bounds
        Collider collider = sphere.GetComponent<Collider>();
        if (collider != null)
        {
            return collider.bounds;
        }
        
        // 如果都没有，使用默认大小（假设是单位Sphere）
        return new Bounds(sphere.transform.position, Vector3.one);
    }
    
    /// <summary>
    /// 获取目标相机（用于图标面向）
    /// 优先级：targetCamera > worldSpaceCanvas.worldCamera > Camera.main
    /// </summary>
    private Camera GetTargetCamera()
    {
        // 优先级1：如果明确指定了targetCamera，使用它
        if (targetCamera != null)
        {
            return targetCamera;
        }
        
        // 优先级2：使用World Space Canvas的相机
        if (worldSpaceCanvas != null && worldSpaceCanvas.worldCamera != null)
        {
            return worldSpaceCanvas.worldCamera;
        }
        
        // 优先级3：使用主相机
        return Camera.main;
    }
    
    /// <summary>
    /// 更新所有图标位置和朝向
    /// </summary>
    private void UpdateIconPositions()
    {
        // 获取目标相机
        Camera cameraToFace = GetTargetCamera();
        
        if (cameraToFace == null)
        {
            Debug.LogWarning("SphereIconManager: 无法获取目标相机，跳过图标位置更新");
            return;
        }
        
        foreach (var kvp in sphereIconMap)
        {
            GameObject sphere = kvp.Key;
            GameObject icon = kvp.Value;
            
            if (sphere != null && icon != null)
            {
                // 计算新的右上角位置
                Vector3 newPosition = CalculateTopRightPosition(sphere);
                icon.transform.position = newPosition;
                
                // 让图标始终面向相机（Billboard效果）
                if (faceCamera)
                {
                    // 计算从图标指向相机的方向
                    Vector3 directionToCamera = cameraToFace.transform.position - icon.transform.position;
                    
                    // 如果方向不为零，让图标面向相机
                    if (directionToCamera != Vector3.zero)
                    {
                        icon.transform.rotation = Quaternion.LookRotation(directionToCamera);
                    }
                }
                
                // 根据相机距离动态调整图标大小，保持视觉大小不变
                if (maintainVisualSize)
                {
                    // 计算图标到相机的距离
                    float distanceToCamera = Vector3.Distance(icon.transform.position, cameraToFace.transform.position);
                    
                    // 根据距离比例调整Scale（距离越远，Scale越大，以保持视觉大小）
                    float scaleMultiplier = distanceToCamera / referenceDistance;
                    icon.transform.localScale = iconBaseScale * scaleMultiplier;
                }
                else
                {
                    // 不使用视觉大小保持，使用固定大小
                    icon.transform.localScale = iconBaseScale;
                }
            }
        }
    }
    
    /// <summary>
    /// 图标点击事件处理
    /// </summary>
    private void OnIconClicked(GameObject sphere)
    {
        if (sphere == null)
        {
            Debug.LogWarning("SphereIconManager: 点击的Sphere为空！");
            return;
        }
        
        string sphereName = sphere.name;
        Debug.Log($"SphereIconManager: 点击了Sphere {sphereName} 的图标");
        
        // 检查数据配置和面板引用
        if (orderDataConfig == null)
        {
            Debug.LogError("SphereIconManager: orderDataConfig未配置！无法显示订单面板。");
            return;
        }
        
        if (infoPanel == null)
        {
            Debug.LogError("SphereIconManager: infoPanel未配置！无法显示订单面板。");
            return;
        }
        
        // 从数据配置中获取订单信息
        var orderInfo = orderDataConfig.GetOrderInfoBySphereName(sphereName);
        
        if (orderInfo != null)
        {
            // 如果面板已显示，先关闭（点击另一个图标时关闭当前面板）
            if (infoPanel.IsVisible())
            {
                infoPanel.Hide();
            }
            
            // 显示新面板
            infoPanel.Show(orderInfo.orderImage, orderInfo.targetSceneName);
        }
        else
        {
            Debug.LogWarning($"SphereIconManager: 未找到 {sphereName} 的订单数据！请检查SphereOrderDataConfig配置。");
        }
    }
    
    /// <summary>
    /// 手动显示图标（供外部调用）
    /// </summary>
    public void ShowIconsManually()
    {
        if (!iconsVisible)
        {
            ShowIcons();
        }
    }
    
    /// <summary>
    /// 手动隐藏图标（供外部调用）
    /// </summary>
    public void HideIconsManually()
    {
        if (iconsVisible)
        {
            HideIcons();
        }
    }
    
    /// <summary>
    /// 检查图标是否显示
    /// </summary>
    public bool AreIconsVisible()
    {
        return iconsVisible;
    }
    
    /// <summary>
    /// 检查指定Sphere是否有气泡存在
    /// </summary>
    /// <param name="sphere">Sphere GameObject</param>
    /// <returns>如果存在气泡返回true，否则返回false</returns>
    public bool HasIconForSphere(GameObject sphere)
    {
        if (sphere == null)
        {
            return false;
        }
        
        // 检查字典中是否存在该Sphere的图标，且图标对象不为空
        if (sphereIconMap.ContainsKey(sphere))
        {
            GameObject icon = sphereIconMap[sphere];
            return icon != null && icon.activeInHierarchy;
        }
        
        return false;
    }
    
    /// <summary>
    /// 根据Sphere名称检查是否有气泡存在
    /// </summary>
    /// <param name="sphereName">Sphere GameObject的名称</param>
    /// <returns>如果存在气泡返回true，否则返回false</returns>
    public bool HasIconForSphere(string sphereName)
    {
        if (string.IsNullOrEmpty(sphereName))
        {
            return false;
        }
        
        // 查找对应的Sphere GameObject
        GameObject targetSphere = null;
        foreach (GameObject sphere in spheres)
        {
            if (sphere != null && sphere.name == sphereName)
            {
                targetSphere = sphere;
                break;
            }
        }
        
        if (targetSphere == null)
        {
            return false;
        }
        
        // 检查是否有气泡
        return HasIconForSphere(targetSphere);
    }
    
    /// <summary>
    /// 根据Sphere名称隐藏图标（播放消失动画）
    /// </summary>
    /// <param name="sphereName">Sphere GameObject的名称</param>
    /// <param name="onComplete">动画完成回调</param>
    public void HideIconForSphere(string sphereName, Action onComplete = null)
    {
        Debug.Log($"SphereIconManager: ====== HideIconForSphere 被调用，sphereName={sphereName} ======");
        
        if (string.IsNullOrEmpty(sphereName))
        {
            Debug.LogWarning("SphereIconManager: sphereName为空！");
            onComplete?.Invoke();
            return;
        }
        
        // 查找对应的Sphere GameObject
        GameObject targetSphere = null;
        int foundIndex = -1;
        for (int i = 0; i < spheres.Count; i++)
        {
            if (spheres[i] != null && spheres[i].name == sphereName)
            {
                targetSphere = spheres[i];
                foundIndex = i;
                Debug.Log($"SphereIconManager: 找到Sphere[{i}]，名称={spheres[i].name}");
                break;
            }
        }
        
        if (targetSphere == null)
        {
            Debug.LogWarning($"SphereIconManager: 未找到名称为 {sphereName} 的Sphere！");
            Debug.LogWarning($"SphereIconManager: 当前Sphere列表包含 {spheres.Count} 个元素");
            for (int i = 0; i < spheres.Count; i++)
            {
                Debug.LogWarning($"SphereIconManager: Sphere[{i}] = {spheres[i]?.name ?? "null"}");
            }
            onComplete?.Invoke();
            return;
        }
        
        // 查找对应的图标
        if (!sphereIconMap.ContainsKey(targetSphere) || sphereIconMap[targetSphere] == null)
        {
            Debug.LogWarning($"SphereIconManager: 未找到 {sphereName} 的图标！");
            Debug.LogWarning($"SphereIconManager: 当前图标字典包含 {sphereIconMap.Count} 个条目");
            foreach (var kvp in sphereIconMap)
            {
                Debug.LogWarning($"SphereIconManager: 字典条目 - Sphere={kvp.Key?.name ?? "null"}, Icon={kvp.Value?.name ?? "null"}");
            }
            onComplete?.Invoke();
            return;
        }
        
        GameObject iconObj = sphereIconMap[targetSphere];
        Debug.Log($"SphereIconManager: 找到图标 {iconObj.name}，开始播放消失动画");
        
        // 播放消失动画
        StartCoroutine(PlayHideAnimation(iconObj, () => {
            Debug.Log($"SphereIconManager: 消失动画完成，Sphere={sphereName}");
            
            // 关闭Sphere的发光效果
            if (enableSphereGlow)
            {
                DisableSphereGlow(targetSphere);
            }
            
            // 从字典中移除
            sphereIconMap.Remove(targetSphere);
            
            // 如果所有图标都隐藏了，更新状态
            if (sphereIconMap.Count == 0)
            {
                iconsVisible = false;
            }
            
            // 调用完成回调
            onComplete?.Invoke();
        }));
    }
    
    /// <summary>
    /// 播放图标消失动画（缩放 + 淡出）
    /// </summary>
    /// <param name="iconObj">图标GameObject</param>
    /// <param name="onComplete">动画完成回调</param>
    private IEnumerator PlayHideAnimation(GameObject iconObj, Action onComplete)
    {
        if (iconObj == null)
        {
            onComplete?.Invoke();
            yield break;
        }
        
        // 获取Image组件（用于淡出效果）
        Image iconImage = iconObj.GetComponent<Image>();
        CanvasGroup canvasGroup = iconObj.GetComponent<CanvasGroup>();
        
        // 如果没有CanvasGroup，添加一个
        if (canvasGroup == null)
        {
            canvasGroup = iconObj.AddComponent<CanvasGroup>();
        }
        
        // 动画参数
        float duration = hideAnimationDuration; // 使用Inspector中配置的动画时长
        float elapsed = 0f;
        Vector3 startScale = iconObj.transform.localScale;
        float startAlpha = canvasGroup.alpha;
        
        // 动画循环
        while (elapsed < duration)
        {
            // 检查对象是否已被销毁（可能在动画过程中被其他地方销毁）
            if (iconObj == null)
            {
                Debug.LogWarning("SphereIconManager: 图标对象在动画过程中被销毁，提前结束动画");
                onComplete?.Invoke();
                yield break;
            }
            
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            
            // 使用缓动函数（easeOut）
            float easeT = 1f - Mathf.Pow(1f - t, 3f);
            
            // 缩放：从1缩放到0
            iconObj.transform.localScale = Vector3.Lerp(startScale, Vector3.zero, easeT);
            
            // 淡出：透明度从1到0
            if (canvasGroup != null)
            {
                canvasGroup.alpha = Mathf.Lerp(startAlpha, 0f, easeT);
            }
            
            yield return null;
        }
        
        // 确保最终状态
        iconObj.transform.localScale = Vector3.zero;
        if (canvasGroup != null)
        {
            canvasGroup.alpha = 0f;
        }
        
        // 销毁图标
        if (iconObj != null)
        {
            Destroy(iconObj);
        }
        
        // 调用完成回调
        onComplete?.Invoke();
    }
    
    /// <summary>
    /// 启用Sphere发光效果
    /// </summary>
    /// <param name="sphere">Sphere GameObject</param>
    private void EnableSphereGlow(GameObject sphere)
    {
        if (sphere == null) return;
        
        Renderer renderer = sphere.GetComponent<Renderer>();
        if (renderer == null)
        {
            Debug.LogWarning($"SphereIconManager: Sphere {sphere.name} 没有Renderer组件，无法启用发光效果");
            return;
        }
        
        Material material = renderer.material;
        if (material == null)
        {
            Debug.LogWarning($"SphereIconManager: Sphere {sphere.name} 的材质为空，无法启用发光效果");
            return;
        }
        
        // 保存原始材质和发光颜色（如果还没有保存）
        if (!sphereOriginalMaterials.ContainsKey(sphere))
        {
            sphereOriginalMaterials[sphere] = new Material(material);
            
            // 尝试获取原始发光颜色
            Color originalEmission = Color.black;
            if (material.HasProperty("_EmissionColor"))
            {
                originalEmission = material.GetColor("_EmissionColor");
            }
            sphereOriginalEmissionColors[sphere] = originalEmission;
        }
        
        // 启用Emission关键字
        material.EnableKeyword("_EMISSION");
        
        // 设置发光颜色（使用HDR颜色，强度由glowIntensity控制）
        if (material.HasProperty("_EmissionColor"))
        {
            Color emissionColor = glowColor * glowIntensity;
            material.SetColor("_EmissionColor", emissionColor);
            
            // 如果是URP材质，可能需要设置不同的属性
            if (material.HasProperty("_Emission"))
            {
                material.SetFloat("_Emission", glowIntensity);
            }
        }
        
        Debug.Log($"SphereIconManager: 已为 {sphere.name} 启用发光效果，颜色: {glowColor}, 强度: {glowIntensity}");
    }
    
    /// <summary>
    /// 关闭Sphere发光效果
    /// </summary>
    /// <param name="sphere">Sphere GameObject</param>
    private void DisableSphereGlow(GameObject sphere)
    {
        if (sphere == null) return;
        
        Renderer renderer = sphere.GetComponent<Renderer>();
        if (renderer == null) return;
        
        Material material = renderer.material;
        if (material == null) return;
        
        // 恢复原始发光颜色
        if (sphereOriginalEmissionColors.ContainsKey(sphere))
        {
            Color originalEmission = sphereOriginalEmissionColors[sphere];
            
            if (material.HasProperty("_EmissionColor"))
            {
                material.SetColor("_EmissionColor", originalEmission);
            }
            
            // 如果原始发光颜色是黑色，禁用Emission关键字
            if (originalEmission == Color.black || originalEmission == new Color(0, 0, 0, 0))
            {
                material.DisableKeyword("_EMISSION");
            }
            
            // 如果是URP材质
            if (material.HasProperty("_Emission"))
            {
                material.SetFloat("_Emission", 0f);
            }
            
            Debug.Log($"SphereIconManager: 已为 {sphere.name} 关闭发光效果");
        }
    }
    
    void OnDestroy()
    {
        // 恢复所有Sphere的原始材质
        if (enableSphereGlow && sphereOriginalMaterials != null)
        {
            foreach (var kvp in sphereOriginalMaterials)
            {
                if (kvp.Key != null)
                {
                    DisableSphereGlow(kvp.Key);
                }
            }
        }
        
        // 清理所有图标
        if (sphereIconMap != null)
        {
            HideIcons();
        }
    }
}
