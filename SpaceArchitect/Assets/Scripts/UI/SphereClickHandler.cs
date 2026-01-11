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
    
    [Header("射线检测设置")]
    [SerializeField] private Camera raycastCamera; // 用于射线检测的相机（如果为空，使用Main Camera）
    [SerializeField] private float maxRaycastDistance = 1000f; // 最大射线检测距离
    [SerializeField] private LayerMask raycastLayerMask = -1; // 射线检测层级（默认所有层级）
    
    [Header("调试")]
    [SerializeField] private bool enableDebugLog = true; // 是否启用调试日志
    
    void Start()
    {
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
            
            // 检查是否是Sphere（可以通过Tag或名称判断）
            // 方法1：通过Tag判断（如果Sphere有特定的Tag）
            // if (hitObject.CompareTag("Sphere"))
            // {
            //     OnSphereClicked(hitObject);
            //     return;
            // }
            
            // 方法2：通过名称判断（更灵活）
            if (hitObject.name.StartsWith("Sphere") || hitObject.name.Contains("Sphere"))
            {
                OnSphereClicked(hitObject);
                return;
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
}
