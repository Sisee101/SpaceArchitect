using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;

/// <summary>
/// Sphere点击处理器
/// 处理Sphere的点击事件，显示订单信息面板
/// </summary>
public class SphereClickHandler : MonoBehaviour
{
    [Header("订单数据配置")]
    [SerializeField] private SphereOrderDataConfig orderDataConfig; // 订单数据集引用（必须配置）
    
    [Header("面板引用")]
    [SerializeField] private SphereInfoPanel infoPanel; // 信息面板引用（场景中的实例或预制体）
    [SerializeField] private GameObject infoPanelPrefab; // 面板预制体（如果场景中没有实例，会从预制体创建）
    
    [Header("气泡管理器引用")]
    [Tooltip("气泡图标管理器（用于检查气泡是否存在，如果为空则自动查找）")]
    [SerializeField] private SphereIconManager iconManager; // 气泡图标管理器引用
    
    [Header("点击行为设置")]
    [Tooltip("是否只在存在气泡时才允许点击显示订单面板")]
    [SerializeField] private bool requireBubbleToClick = true; // 是否要求气泡存在才能点击
    
    [Header("射线检测设置")]
    [SerializeField] private Camera raycastCamera; // 用于射线检测的相机（如果为空，使用Main Camera）
    [SerializeField] private float maxRaycastDistance = 1000f; // 最大射线检测距离
    [SerializeField] private LayerMask raycastLayerMask = -1; // 射线检测层级（默认所有层级）
    
    [Header("音效")]
    [Tooltip("音频源组件（如果为空，会自动获取或创建）")]
    [SerializeField] private AudioSource audioSource;
    [Tooltip("Sphere点击音效")]
    [SerializeField] private AudioClip sphereClickSound;
    
    [Header("调试")]
    [SerializeField] private bool enableDebugLog = true; // 是否启用调试日志
    
    void Start()
    {
        // 初始化音频源
        InitializeAudioSource();
        
        // 如果没有指定相机，优先查找关卡场景中的相机（避免MainHub相机冲突）
        if (raycastCamera == null)
        {
            raycastCamera = FindGameplayCamera();
            if (raycastCamera == null)
            {
                // 如果找不到关卡场景的相机，使用Camera.main作为备用
                raycastCamera = Camera.main;
                if (raycastCamera == null)
                {
                    Debug.LogError("SphereClickHandler: 未找到可用的相机！请在Inspector中指定raycastCamera。");
                }
                else
                {
                    Debug.LogWarning($"SphereClickHandler: 使用Camera.main作为备用相机: {Camera.main.name} (场景: {Camera.main.gameObject.scene.name})");
                }
            }
            else
            {
                Debug.Log($"SphereClickHandler: 找到关卡场景相机: {raycastCamera.name} (场景: {raycastCamera.gameObject.scene.name})");
            }
        }
        
        // 验证必要引用
        if (orderDataConfig == null)
        {
            Debug.LogError("SphereClickHandler: orderDataConfig未配置！请在Inspector中指定SphereOrderDataConfig资源。");
        }
        
        // 如果没有指定iconManager，尝试自动查找
        if (iconManager == null)
        {
            iconManager = FindObjectOfType<SphereIconManager>();
            if (iconManager == null)
            {
                if (enableDebugLog)
                {
                    Debug.LogWarning("SphereClickHandler: 未找到SphereIconManager，如果requireBubbleToClick为true，将无法检查气泡是否存在。");
                }
            }
            else
            {
                if (enableDebugLog)
                {
                    Debug.Log("SphereClickHandler: 已自动找到SphereIconManager");
                }
            }
        }
        
        // 如果场景中没有面板实例，尝试从预制体创建
        if (infoPanel == null)
        {
            if (infoPanelPrefab != null)
            {
                // 从预制体创建面板实例
                GameObject panelInstance = Instantiate(infoPanelPrefab);
                
                // 确保面板在正确的Canvas下（MainHubCanvas）
                Canvas mainCanvas = FindObjectOfType<Canvas>();
                if (mainCanvas != null && mainCanvas.renderMode == RenderMode.ScreenSpaceOverlay)
                {
                    panelInstance.transform.SetParent(mainCanvas.transform, false);
                }
                
                // 获取SphereInfoPanel组件
                infoPanel = panelInstance.GetComponent<SphereInfoPanel>();
                
                if (infoPanel == null)
                {
                    Debug.LogError("SphereClickHandler: 预制体没有SphereInfoPanel组件！");
                }
                else
                {
                    Debug.Log("SphereClickHandler: 已从预制体创建面板实例");
                }
            }
            else
            {
                Debug.LogError("SphereClickHandler: infoPanel和infoPanelPrefab都未配置！请至少配置一个。");
            }
        }
    }
    
    // 用于区分单击和拖动的变量
    private Vector3 mouseDownPosition;
    private bool isMouseDown = false;
    private const float clickDragThreshold = 10f; // 鼠标移动超过10像素认为是拖动，不是点击（增大阈值，避免误判）
    
    void Update()
    {
        // 场景检查：只在 MainHub 场景中工作
        // 检查此脚本所在的场景是否为 MainHub 场景（而不是检查激活场景，因为Additive加载时激活场景可能是关卡场景）
        string mySceneName = gameObject.scene.name;
        bool isMainHubScene = mySceneName.StartsWith("0") && mySceneName.Contains("_MainHub");
        
        // 如果此脚本不在 MainHub 场景中，禁用此脚本的功能
        if (!isMainHubScene)
        {
            return;
        }
        
        // 额外检查：如果有关卡场景已加载（Additive模式），说明已经进入游戏，不应该处理Sphere点击
        if (SceneTransitionManager.Instance != null && SceneTransitionManager.Instance.IsGameSceneLoaded())
        {
            return;
        }
        
        // 检测鼠标左键按下
        if (Input.GetMouseButtonDown(0))
        {
            // 记录按下时的鼠标位置
            mouseDownPosition = Input.mousePosition;
            isMouseDown = true;
            
            if (enableDebugLog)
            {
                Debug.Log($"SphereClickHandler: 鼠标按下 - 位置: {mouseDownPosition}");
            }
        }
        
        // 检测鼠标左键抬起（只处理单击，不处理拖动）
        if (Input.GetMouseButtonUp(0) && isMouseDown)
        {
            // 计算鼠标移动距离
            float mouseMoveDistance = Vector3.Distance(Input.mousePosition, mouseDownPosition);
            
            if (enableDebugLog)
            {
                Debug.Log($"SphereClickHandler: 鼠标抬起 - 位置: {Input.mousePosition}, 移动距离: {mouseMoveDistance:F2}, 阈值: {clickDragThreshold}");
            }
            
            // 如果移动距离小于阈值，认为是单击
            if (mouseMoveDistance < clickDragThreshold)
            {
                if (enableDebugLog)
                {
                    Debug.Log($"SphereClickHandler: 判定为单击（移动距离 {mouseMoveDistance:F2} < 阈值 {clickDragThreshold}），开始处理点击");
                }
                HandleMouseClick();
            }
            else
            {
                if (enableDebugLog)
                {
                    Debug.Log($"SphereClickHandler: 检测到拖动（移动距离: {mouseMoveDistance:F2} >= 阈值 {clickDragThreshold}），跳过点击处理");
                }
            }
            
            isMouseDown = false;
        }
    }
    
    /// <summary>
    /// 处理鼠标点击
    /// </summary>
    private void HandleMouseClick()
    {
        if (enableDebugLog)
        {
            Debug.Log($"SphereClickHandler: ====== 开始处理点击 ======");
        }
        
        // 检查是否点击在UI上（Screen Space Canvas）
        if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
        {
            // 如果是UI元素，不处理Sphere点击（避免与UI按钮冲突）
            if (enableDebugLog)
            {
                Debug.Log("SphereClickHandler: 点击在UI上，跳过Sphere点击处理");
            }
            return;
        }
        
        if (raycastCamera == null)
        {
            if (enableDebugLog)
            {
                Debug.LogError("SphereClickHandler: raycastCamera 为空！无法执行射线检测");
            }
            return;
        }
        
        if (enableDebugLog)
        {
            Debug.Log($"SphereClickHandler: 射线检测 - 相机: {raycastCamera.name}, 鼠标位置: {Input.mousePosition}, 最大距离: {maxRaycastDistance}, LayerMask: {raycastLayerMask.value}");
        }
        
        // 创建从相机到鼠标位置的射线
        Ray ray = raycastCamera.ScreenPointToRay(Input.mousePosition);
        
        // 关键修复：使用 RaycastAll 检测所有击中的物体，然后优先选择 Sphere
        // 这样可以穿透基站等前面的物体，检测到后面的 Sphere
        RaycastHit[] hits = Physics.RaycastAll(ray, maxRaycastDistance, raycastLayerMask);
        
        if (hits.Length > 0)
        {
            if (enableDebugLog)
            {
                Debug.Log($"SphereClickHandler: 射线击中 {hits.Length} 个物体，开始查找 Sphere...");
            }
            
            // 按距离排序（从近到远）
            System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));
            
            // 遍历所有击中的物体，优先查找 Sphere
            GameObject sphereObject = null;
            string detectionMethod = "";
            
            foreach (RaycastHit hit in hits)
            {
                GameObject hitObject = hit.collider.gameObject;
                
                if (enableDebugLog)
                {
                    Debug.Log($"SphereClickHandler: 检查物体: {hitObject.name}, Tag: {hitObject.tag}, Layer: {hitObject.layer}, 距离: {hit.distance:F2}");
                }
                
                // 检查是否是Sphere（优先级：Tag > 订单配置 > 名称）
                bool isSphere = false;
                
                // 方法1：通过Tag判断（最可靠，推荐使用）
                if (hitObject.CompareTag("Sphere"))
                {
                    isSphere = true;
                    detectionMethod = "Tag";
                }
                // 方法2：通过订单配置判断（支持中文名称，如"霜沧星-1", "古寂星-1"等）
                else if (orderDataConfig != null)
                {
                    var orderInfo = orderDataConfig.GetOrderInfoBySphereName(hitObject.name);
                    if (orderInfo != null)
                    {
                        isSphere = true;
                        detectionMethod = "订单配置";
                        if (enableDebugLog)
                        {
                            Debug.Log($"SphereClickHandler: 通过订单配置识别为Sphere: {hitObject.name}");
                        }
                    }
                }
                // 方法3：通过名称判断（向后兼容，支持英文名称，如"Sphere1", "Sphere2"等）
                else if (hitObject.name.StartsWith("Sphere") || hitObject.name.Contains("Sphere"))
                {
                    isSphere = true;
                    detectionMethod = "名称";
                }
                
                if (isSphere)
                {
                    sphereObject = hitObject;
                    if (enableDebugLog)
                    {
                        Debug.Log($"SphereClickHandler: ✅ 找到 Sphere（方法: {detectionMethod}）: {hitObject.name}, 距离: {hit.distance:F2}");
                    }
                    break; // 找到第一个 Sphere 就停止
                }
            }
            
            // 如果找到了 Sphere，处理点击
            if (sphereObject != null)
            {
                OnSphereClicked(sphereObject);
                return;
            }
            else
            {
                if (enableDebugLog)
                {
                    Debug.LogWarning($"SphereClickHandler: 射线击中了 {hits.Length} 个物体，但没有找到 Sphere");
                    foreach (var hit in hits)
                    {
                        Debug.LogWarning($"  - {hit.collider.gameObject.name} (Tag: {hit.collider.gameObject.tag}, Layer: {hit.collider.gameObject.layer})");
                    }
                }
            }
        }
        else
        {
            if (enableDebugLog)
            {
                Debug.LogWarning($"SphereClickHandler: ❌ 射线检测未击中任何物体 - 鼠标位置: {Input.mousePosition}, 射线方向: {ray.direction}, 最大距离: {maxRaycastDistance}, LayerMask: {raycastLayerMask.value}");
                
                // 尝试不限制LayerMask的射线检测，看看是否能击中物体
                RaycastHit[] allHits = Physics.RaycastAll(ray, maxRaycastDistance);
                if (allHits.Length > 0)
                {
                    Debug.LogWarning($"SphereClickHandler: ⚠️ 使用全LayerMask检测到 {allHits.Length} 个物体，但当前LayerMask不包含这些Layer");
                    foreach (var hit in allHits)
                    {
                        Debug.LogWarning($"  - {hit.collider.gameObject.name} (Layer: {hit.collider.gameObject.layer})");
                    }
                }
            }
        }
    }
    
    /// <summary>
    /// Sphere被点击时的处理
    /// </summary>
    private void OnSphereClicked(GameObject sphere)
    {
        if (sphere == null)
        {
            Debug.LogWarning("SphereClickHandler: 点击的Sphere为空！");
            return;
        }
        
        string sphereName = sphere.name;
        
        if (enableDebugLog)
        {
            Debug.Log($"SphereClickHandler: ====== Sphere被点击：{sphereName} =====");
        }
        
        // 检查是否要求气泡存在才能点击
        if (requireBubbleToClick)
        {
            if (iconManager == null)
            {
                if (enableDebugLog)
                {
                    Debug.LogWarning("SphereClickHandler: requireBubbleToClick为true，但iconManager未配置，无法检查气泡是否存在。跳过点击处理。");
                }
                return;
            }
            
            // 检查该Sphere是否有气泡存在
            bool hasBubble = iconManager.HasIconForSphere(sphere);
            
            if (!hasBubble)
            {
                if (enableDebugLog)
                {
                    Debug.Log($"SphereClickHandler: Sphere {sphereName} 上不存在气泡，点击被忽略（requireBubbleToClick=true）");
                }
                return;
            }
            
            if (enableDebugLog)
            {
                Debug.Log($"SphereClickHandler: Sphere {sphereName} 上存在气泡，允许显示订单面板");
            }
        }
        
        // 检查数据配置和面板引用
        if (orderDataConfig == null)
        {
            Debug.LogError("SphereClickHandler: orderDataConfig未配置！无法显示订单面板。");
            return;
        }
        
        if (infoPanel == null)
        {
            Debug.LogError("SphereClickHandler: infoPanel未配置！无法显示订单面板。");
            return;
        }
        
        // 播放点击音效（在验证通过后，显示面板之前）
        PlaySphereClickSound();
        
        // 从数据配置中获取订单信息
        var orderInfo = orderDataConfig.GetOrderInfoBySphereName(sphereName);
        
        if (orderInfo != null)
        {
            // 如果面板已显示，先关闭（点击另一个Sphere时关闭当前面板）
            if (infoPanel.IsVisible())
            {
                infoPanel.Hide();
            }
            
            // 显示新面板
            infoPanel.Show(orderInfo.orderImage, orderInfo.targetSceneName);
            
            if (enableDebugLog)
            {
                Debug.Log($"SphereClickHandler: 显示订单面板 - Sphere: {sphereName}, Scene: {orderInfo.targetSceneName}");
            }
        }
        else
        {
            if (enableDebugLog)
            {
                Debug.LogWarning($"SphereClickHandler: 未找到 {sphereName} 的订单数据！请检查SphereOrderDataConfig配置。");
            }
        }
    }
    
    /// <summary>
    /// 查找相机（SphereClickHandler 只在 MainHub 场景中工作，所以应该使用 MainHub 场景的相机）
    /// </summary>
    private Camera FindGameplayCamera()
    {
        // SphereClickHandler 只在 MainHub 场景中工作，所以应该使用 MainHub 场景的相机
        Camera[] cameras = FindObjectsOfType<Camera>();
        
        // 优先查找 MainHub 场景中的相机（因为 SphereClickHandler 只在 MainHub 场景中工作）
        string mySceneName = gameObject.scene.name;
        foreach (Camera cam in cameras)
        {
            string sceneName = cam.gameObject.scene.name;
            bool isMainHubScene = sceneName.StartsWith("0") && sceneName.Contains("_MainHub");
            
            // 优先使用与当前脚本相同场景的相机
            if (sceneName == mySceneName && 
                cam.CompareTag("MainCamera") && 
                cam.enabled)
            {
                return cam;
            }
        }
        
        // 如果没找到同场景的相机，查找其他 MainHub 场景的相机
        foreach (Camera cam in cameras)
        {
            string sceneName = cam.gameObject.scene.name;
            bool isMainHubScene = sceneName.StartsWith("0") && sceneName.Contains("_MainHub");
            
            if (isMainHubScene && 
                cam.CompareTag("MainCamera") && 
                cam.enabled)
            {
                return cam;
            }
        }
        
        // 如果没找到，返回null（让调用者使用Camera.main作为备用）
        return null;
    }
    
    /// <summary>
    /// 初始化音频源
    /// </summary>
    private void InitializeAudioSource()
    {
        if (audioSource == null)
        {
            audioSource = GetComponent<AudioSource>();
            if (audioSource == null)
            {
                audioSource = gameObject.AddComponent<AudioSource>();
                audioSource.playOnAwake = false;
                
                if (enableDebugLog)
                {
                    Debug.Log("SphereClickHandler: 已自动创建 AudioSource 组件");
                }
            }
        }
    }
    
    /// <summary>
    /// 播放Sphere点击音效
    /// </summary>
    private void PlaySphereClickSound()
    {
        if (sphereClickSound != null && audioSource != null)
        {
            audioSource.PlayOneShot(sphereClickSound);
            
            if (enableDebugLog)
            {
                Debug.Log("SphereClickHandler: 播放Sphere点击音效");
            }
        }
    }
}
