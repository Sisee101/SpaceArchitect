using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

/// <summary>
/// Sphere图标管理器
/// 管理Sphere右上角图标的显示、隐藏和位置更新
/// </summary>
public class SphereIconManager : MonoBehaviour
{
    [Header("Sphere引用（需要显示图标的Sphere）")]
    [SerializeField] private GameObject sphere1; // Sphere 1
    [SerializeField] private GameObject sphere2; // Sphere 2
    [SerializeField] private GameObject sphere4; // Sphere 4
    
    [Header("UI引用")]
    [SerializeField] private Canvas worldSpaceCanvas; // World Space Canvas（如果为空，会自动查找或创建）
    [SerializeField] private GameObject iconPrefab; // Icon预制体（必须配置）
    
    [Header("图标位置设置")]
    [SerializeField] private float iconOffsetMultiplier = 1.2f; // 右上角偏移倍数（相对于Sphere半径）
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
    
    // 私有变量
    private Dictionary<GameObject, GameObject> sphereIconMap; // Sphere到Icon的映射字典
    private bool iconsVisible = false; // 图标是否显示
    private float lastUpdateTime = 0f; // 上次更新时间
    
    void Start()
    {
        // 初始化字典
        sphereIconMap = new Dictionary<GameObject, GameObject>();
        
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
        
        if (sphere1 == null || sphere2 == null || sphere4 == null)
        {
            Debug.LogWarning("SphereIconManager: 部分Sphere引用未配置，请确保所有Sphere引用都已设置。");
        }
        
        if (orderDataConfig == null)
        {
            Debug.LogWarning("SphereIconManager: orderDataConfig未配置！请在Inspector中指定SphereOrderDataConfig资源。");
        }
        
        if (infoPanel == null)
        {
            Debug.LogWarning("SphereIconManager: infoPanel未配置！请在Inspector中指定SphereInfoPanel引用。");
        }
    }
    
    void Update()
    {
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
    /// 显示所有图标
    /// </summary>
    private void ShowIcons()
    {
        if (iconPrefab == null || worldSpaceCanvas == null)
        {
            Debug.LogError("SphereIconManager: iconPrefab或worldSpaceCanvas未配置！");
            return;
        }
        
        Debug.Log($"SphereIconManager: 开始显示图标，Canvas: {worldSpaceCanvas.name}, RenderMode: {worldSpaceCanvas.renderMode}, Canvas Scale: {worldSpaceCanvas.transform.localScale}");
        
        // 为每个Sphere创建图标
        CreateIconForSphere(sphere1);
        CreateIconForSphere(sphere2);
        CreateIconForSphere(sphere4);
        
        iconsVisible = true;
        Debug.Log($"SphereIconManager: 图标已显示，共创建 {sphereIconMap.Count} 个图标");
    }
    
    /// <summary>
    /// 隐藏所有图标
    /// </summary>
    private void HideIcons()
    {
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
            Camera mainCamera = null;
            if (worldSpaceCanvas != null && worldSpaceCanvas.worldCamera != null)
            {
                mainCamera = worldSpaceCanvas.worldCamera;
            }
            else
            {
                mainCamera = Camera.main;
            }
            
            if (mainCamera != null)
            {
                Vector3 directionToCamera = mainCamera.transform.position - iconObj.transform.position;
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
    }
    
    /// <summary>
    /// 计算Sphere右上角的世界坐标
    /// </summary>
    private Vector3 CalculateTopRightPosition(GameObject sphere)
    {
        if (sphere == null) return Vector3.zero;
        
        // 获取Sphere的Bounds（考虑MeshRenderer或Collider）
        Bounds bounds = GetSphereBounds(sphere);
        
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
        
        // 计算偏移量（使用Sphere的半径）
        float radius = Mathf.Max(bounds.extents.x, bounds.extents.y, bounds.extents.z);
        Vector3 rightOffset = rightDirection * radius * iconOffsetMultiplier;
        Vector3 upOffset = upDirection * radius * iconOffsetMultiplier;
        Vector3 topRightOffset = rightOffset + upOffset;
        
        // 返回世界坐标
        Vector3 finalPosition = sphere.transform.position + topRightOffset;
        
        // 调试信息
        if (iconsVisible)
        {
            Debug.Log($"SphereIconManager: {sphere.name} 位置计算 - Sphere位置: {sphere.transform.position}, 半径: {radius}, 偏移倍数: {iconOffsetMultiplier}, 最终位置: {finalPosition}");
        }
        
        return finalPosition;
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
    /// 更新所有图标位置和朝向
    /// </summary>
    private void UpdateIconPositions()
    {
        // 获取相机（优先使用World Space Canvas的相机，否则使用主相机）
        Camera mainCamera = null;
        if (worldSpaceCanvas != null && worldSpaceCanvas.worldCamera != null)
        {
            mainCamera = worldSpaceCanvas.worldCamera;
        }
        else
        {
            mainCamera = Camera.main;
        }
        
        if (mainCamera == null) return;
        
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
                    Vector3 directionToCamera = mainCamera.transform.position - icon.transform.position;
                    
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
                    float distanceToCamera = Vector3.Distance(icon.transform.position, mainCamera.transform.position);
                    
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
    
    void OnDestroy()
    {
        // 清理所有图标
        HideIcons();
    }
}
