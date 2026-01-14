using UnityEngine;

/// <summary>
/// 全局总览摄像机脚本
/// 用于在游戏画面左上角显示大范围的全局视图
/// </summary>
public class GlobalOverviewCamera : MonoBehaviour
{
    [Header("摄像机设置")]
    [Tooltip("全局总览摄像机（如果为空，将使用当前GameObject的Camera组件）")]
    [SerializeField] private Camera overviewCamera;

    [Tooltip("渲染纹理（用于在UI上显示摄像机画面）")]
    [SerializeField] private RenderTexture renderTexture;

    [Tooltip("渲染纹理宽度（像素）")]
    [SerializeField] private int renderTextureWidth = 512;

    [Tooltip("渲染纹理高度（像素）")]
    [SerializeField] private int renderTextureHeight = 512;

    [Header("视图设置")]
    [Tooltip("是否自动计算视图范围（基于Station和Destination）。如果禁用，将保持摄像机原有的position和orthographicSize不变")]
    [SerializeField] private bool autoCalculateViewRange = false;

    [Tooltip("手动指定的视图中心点（如果autoCalculateViewRange为false）")]
    [SerializeField] private Vector3 manualViewCenter = Vector3.zero;

    [Tooltip("手动指定的视图大小（如果autoCalculateViewRange为false）")]
    [SerializeField] private float manualViewSize = 100f;

    [Tooltip("视图边距（在计算出的范围基础上增加边距）")]
    [SerializeField] private float viewPadding = 5f;

    [Tooltip("Station的Tag（用于自动计算模式）")]
    [SerializeField] private string stationTag = "Station";

    [Tooltip("Destination的Tag（用于自动计算模式）")]
    [SerializeField] private string destinationTag = "Destination";

    [Tooltip("未找到对象时是否保持摄像机原始orthographicSize（如果为false，则使用默认值50）")]
    [SerializeField] private bool keepOriginalSizeWhenNoObjects = true;

    [Header("更新设置")]
    [Tooltip("是否每帧更新视图（如果为false，只在Start时计算一次）")]
    [SerializeField] private bool updateEveryFrame = false;

    [Tooltip("视图更新间隔（秒，如果updateEveryFrame为true）")]
    [SerializeField] private float updateInterval = 0.5f;

    [Header("调试")]
    [Tooltip("是否显示调试信息")]
    [SerializeField] private bool showDebugLog = true;

    private float lastUpdateTime = 0f;
    private Vector3 calculatedViewCenter;
    private float calculatedViewSize;
    private bool isRenderTextureCreatedByScript = false; // 标记RenderTexture是否由脚本创建
    private bool foundObjectsForCalculation = false; // 标记是否找到了对象用于计算

    void Awake()
    {
        // 如果没有指定摄像机，尝试获取当前GameObject的Camera组件
        if (overviewCamera == null)
        {
            overviewCamera = GetComponent<Camera>();
            if (overviewCamera == null)
            {
                Debug.LogError("GlobalOverviewCamera: 未找到Camera组件！请在Inspector中指定overviewCamera或确保当前GameObject有Camera组件。");
                enabled = false;
                return;
            }
        }

        // 确保摄像机是正交的（Orthographic）
        if (!overviewCamera.orthographic)
        {
            overviewCamera.orthographic = true;
            if (showDebugLog)
            {
                Debug.Log("GlobalOverviewCamera: 已将摄像机设置为正交模式（Orthographic）");
            }
        }

        // 设置摄像机深度，确保它不会覆盖主摄像机
        overviewCamera.depth = -1; // 比主摄像机低，这样主摄像机渲染在上层

        // 创建或使用现有的RenderTexture
        SetupRenderTexture();
    }

    void Start()
    {
        // 只有在启用自动计算时才更新视图
        if (autoCalculateViewRange)
        {
            // 初始计算视图范围
            CalculateViewRange();
            UpdateCameraView();
        }
        // 如果禁用自动计算，保持摄像机原有的position和orthographicSize不变
    }

    void Update()
    {
        // 如果需要每帧更新或按间隔更新
        if (updateEveryFrame)
        {
            if (Time.time - lastUpdateTime >= updateInterval)
            {
                CalculateViewRange();
                UpdateCameraView();
                lastUpdateTime = Time.time;
            }
        }
    }

    /// <summary>
    /// 设置RenderTexture
    /// </summary>
    private void SetupRenderTexture()
    {
        // 如果没有指定RenderTexture，创建一个新的
        if (renderTexture == null)
        {
            renderTexture = new RenderTexture(renderTextureWidth, renderTextureHeight, 24);
            renderTexture.name = "GlobalOverviewRenderTexture";
            isRenderTextureCreatedByScript = true; // 标记为我们创建的
            
            if (showDebugLog)
            {
                Debug.Log($"GlobalOverviewCamera: 已创建新的RenderTexture ({renderTextureWidth}x{renderTextureHeight})");
            }
        }
        else
        {
            // 如果RenderTexture已经在Inspector中指定，标记为不是我们创建的
            isRenderTextureCreatedByScript = false;
        }

        // 将RenderTexture分配给摄像机
        overviewCamera.targetTexture = renderTexture;

        if (showDebugLog)
        {
            Debug.Log($"GlobalOverviewCamera: 已将RenderTexture分配给摄像机");
        }
    }

    /// <summary>
    /// 计算视图范围
    /// </summary>
    private void CalculateViewRange()
    {
        if (autoCalculateViewRange)
        {
            // 自动计算：查找所有Station和Destination，计算它们的边界
            CalculateViewRangeFromObjects();
        }
        else
        {
            // 使用手动设置
            calculatedViewCenter = manualViewCenter;
            calculatedViewSize = manualViewSize;
        }
    }

    /// <summary>
    /// 从场景中的对象自动计算视图范围
    /// </summary>
    private void CalculateViewRangeFromObjects()
    {
        float minX = float.MaxValue;
        float maxX = float.MinValue;
        float minY = float.MaxValue;
        float maxY = float.MinValue;
        bool foundAnyObject = false;

        // 查找所有Station
        GameObject[] stations = GameObject.FindGameObjectsWithTag(stationTag);
        foreach (GameObject station in stations)
        {
            Vector3 pos = station.transform.position;
            minX = Mathf.Min(minX, pos.x);
            maxX = Mathf.Max(maxX, pos.x);
            minY = Mathf.Min(minY, pos.y);
            maxY = Mathf.Max(maxY, pos.y);
            foundAnyObject = true;
        }

        // 查找所有Destination
        GameObject[] destinations = GameObject.FindGameObjectsWithTag(destinationTag);
        foreach (GameObject destination in destinations)
        {
            Vector3 pos = destination.transform.position;
            minX = Mathf.Min(minX, pos.x);
            maxX = Mathf.Max(maxX, pos.x);
            minY = Mathf.Min(minY, pos.y);
            maxY = Mathf.Max(maxY, pos.y);
            foundAnyObject = true;
        }

        // 如果没有找到任何对象
        if (!foundAnyObject)
        {
            foundObjectsForCalculation = false;
            calculatedViewCenter = Vector3.zero;
            
            if (keepOriginalSizeWhenNoObjects && overviewCamera != null)
            {
                // 保持摄像机当前的orthographicSize（不修改）
                calculatedViewSize = overviewCamera.orthographicSize * 2f; // 仅用于记录，实际不会使用
                
                if (showDebugLog)
                {
                    Debug.LogWarning($"GlobalOverviewCamera: 未找到Station或Destination，保持摄像机原始orthographicSize: {overviewCamera.orthographicSize}");
                }
            }
            else
            {
                // 使用默认值
                calculatedViewSize = 50f;
                foundObjectsForCalculation = true; // 即使没找到对象，也使用默认值更新
                
                if (showDebugLog)
                {
                    Debug.LogWarning("GlobalOverviewCamera: 未找到Station或Destination，使用默认视图范围: 50");
                }
            }
            return;
        }
        
        foundObjectsForCalculation = true;

        // 计算中心点和大小
        calculatedViewCenter = new Vector3(
            (minX + maxX) / 2f,
            (minY + maxY) / 2f,
            0f
        );

        // 计算需要的视图大小（取X和Y方向的最大值，加上边距）
        float width = maxX - minX + viewPadding * 2f;
        float height = maxY - minY + viewPadding * 2f;
        calculatedViewSize = Mathf.Max(width, height);

        if (showDebugLog && Time.frameCount % 60 == 0) // 每60帧打印一次，避免日志过多
        {
            Debug.Log($"GlobalOverviewCamera: 计算视图范围 - 中心: {calculatedViewCenter}, 大小: {calculatedViewSize}");
        }
    }

    /// <summary>
    /// 更新摄像机视图
    /// </summary>
    private void UpdateCameraView()
    {
        if (overviewCamera == null) return;

        // 设置摄像机位置（Z轴保持距离，确保能看到整个视图）
        overviewCamera.transform.position = new Vector3(
            calculatedViewCenter.x,
            calculatedViewCenter.y,
            overviewCamera.transform.position.z
        );

        // 只有在找到对象或keepOriginalSizeWhenNoObjects为false时才更新orthographicSize
        if (foundObjectsForCalculation)
        {
            // 设置正交摄像机的大小（orthographicSize）
            overviewCamera.orthographicSize = calculatedViewSize / 2f;
        }
    }

    /// <summary>
    /// 获取RenderTexture（用于在UI上显示）
    /// </summary>
    public RenderTexture GetRenderTexture()
    {
        return renderTexture;
    }

    /// <summary>
    /// 手动设置视图中心点
    /// </summary>
    public void SetViewCenter(Vector3 center)
    {
        manualViewCenter = center;
        autoCalculateViewRange = false;
        CalculateViewRange();
        UpdateCameraView();
    }

    /// <summary>
    /// 手动设置视图大小
    /// </summary>
    public void SetViewSize(float size)
    {
        manualViewSize = size;
        autoCalculateViewRange = false;
        CalculateViewRange();
        UpdateCameraView();
    }

    /// <summary>
    /// 启用/禁用自动计算视图范围
    /// </summary>
    public void SetAutoCalculateViewRange(bool enabled)
    {
        autoCalculateViewRange = enabled;
        if (enabled)
        {
            CalculateViewRange();
            UpdateCameraView();
        }
    }

    void OnDestroy()
    {
        // 清理RenderTexture（如果是我们创建的）
        if (renderTexture != null && isRenderTextureCreatedByScript)
        {
            // 先释放RenderTexture
            renderTexture.Release();
            
            // 区分编辑器和运行时
            #if UNITY_EDITOR
            // 在编辑器中，使用DestroyImmediate
            if (!Application.isPlaying)
            {
                DestroyImmediate(renderTexture, true);
            }
            else
            {
                // 运行时在编辑器中，使用Destroy
                Destroy(renderTexture);
            }
            #else
            // 在运行时（非编辑器），使用Destroy
            Destroy(renderTexture);
            #endif
        }
    }
}

