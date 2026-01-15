using UnityEngine;
using UnityEngine.EventSystems;

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
        
        // 如果没有指定相机，使用主相机
        if (raycastCamera == null)
        {
            raycastCamera = Camera.main;
            if (raycastCamera == null)
            {
                Debug.LogError("SphereClickHandler: 未找到Main Camera！请在Inspector中指定raycastCamera。");
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
    
    void Update()
    {
        // 检测鼠标左键点击
        if (Input.GetMouseButtonDown(0))
        {
            HandleMouseClick();
        }
    }
    
    /// <summary>
    /// 处理鼠标点击
    /// </summary>
    private void HandleMouseClick()
    {
        // 检查是否点击在UI上（Screen Space Canvas）
        if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
        {
            // 如果是UI元素，不处理Sphere点击（避免与UI按钮冲突）
            return;
        }
        
        if (raycastCamera == null) return;
        
        // 创建从相机到鼠标位置的射线
        Ray ray = raycastCamera.ScreenPointToRay(Input.mousePosition);
        RaycastHit hit;
        
        // 执行射线检测
        if (Physics.Raycast(ray, out hit, maxRaycastDistance, raycastLayerMask))
        {
            GameObject hitObject = hit.collider.gameObject;
            
            if (enableDebugLog)
            {
                Debug.Log($"SphereClickHandler: 射线击中物体: {hitObject.name}, Tag: {hitObject.tag}");
            }
            
            // 检查是否是Sphere（优先级：Tag > 订单配置 > 名称）
            bool isSphere = false;
            string detectionMethod = "";
            
            // 方法1：通过Tag判断（最可靠，推荐使用）
            // 物体可以保留中文名，只要Tag设置为"Sphere"即可
            if (hitObject.CompareTag("Sphere"))
            {
                isSphere = true;
                detectionMethod = "Tag";
            }
            // 方法2：通过订单配置判断（支持中文名称，如"霜沧星-1", "古寂星-1"等）
            // 如果orderDataConfig已配置，检查是否能找到对应的订单信息
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
                if (enableDebugLog)
                {
                    Debug.Log($"SphereClickHandler: 识别为Sphere（方法: {detectionMethod}）: {hitObject.name}");
                }
                OnSphereClicked(hitObject);
                return;
            }
            else
            {
                if (enableDebugLog)
                {
                    Debug.Log($"SphereClickHandler: 物体 {hitObject.name} 不是Sphere，跳过点击处理");
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
