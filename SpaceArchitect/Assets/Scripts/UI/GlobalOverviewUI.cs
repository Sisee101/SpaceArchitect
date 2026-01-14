using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 全局总览UI脚本
/// 在画面左上角显示全局总览摄像机的小视窗
/// </summary>
public class GlobalOverviewUI : MonoBehaviour
{
    [Header("UI引用")]
    [Tooltip("显示全局视窗的RawImage组件（如果为空，将自动查找）")]
    [SerializeField] private RawImage overviewImage;

    [Tooltip("全局总览摄像机脚本（如果为空，将自动查找）")]
    [SerializeField] private GlobalOverviewCamera overviewCamera;

    [Header("显示设置")]
    [Tooltip("是否在游戏开始时自动显示")]
    [SerializeField] private bool showOnStart = true;

    [Tooltip("是否可以通过按键切换显示/隐藏")]
    [SerializeField] private bool allowToggle = true;

    [Tooltip("切换显示/隐藏的按键（默认Tab键）")]
    [SerializeField] private KeyCode toggleKey = KeyCode.Tab;

    [Header("位置和大小设置")]
    [Tooltip("视窗在屏幕上的位置（0-1，相对于屏幕）")]
    [SerializeField] private Vector2 screenPosition = new Vector2(0.01f, 0.99f); // 左上角，更靠近边缘

    [Tooltip("视窗大小（相对于屏幕宽度的比例，0-1）")]
    [SerializeField] private float sizeRatio = 0.25f; // 屏幕宽度的25%

    [Tooltip("视窗宽高比（宽度/高度，如果为0则使用RenderTexture的宽高比）")]
    [SerializeField] private float aspectRatio = 0f; // 0表示使用RenderTexture的宽高比

    [Header("外观设置")]
    [Tooltip("视窗背景图片（优先级高于背景颜色，如果设置了图片则使用图片）")]
    [SerializeField] private Sprite backgroundSprite;

    [Tooltip("视窗背景颜色（仅在未设置背景图片时使用，如果为透明则无背景）")]
    [SerializeField] private Color backgroundColor = new Color(0f, 0f, 0f, 0.5f);

    [Tooltip("是否显示边框")]
    [SerializeField] private bool showBorder = true;

    [Tooltip("边框颜色")]
    [SerializeField] private Color borderColor = Color.white;

    [Tooltip("边框宽度（像素）")]
    [SerializeField] private float borderWidth = 2f;

    [Header("调试")]
    [Tooltip("是否显示调试信息")]
    [SerializeField] private bool showDebugLog = true;

    private GameObject overviewPanel;
    private Image backgroundImage;
    private Image borderImage;

    void Awake()
    {
        // 自动查找GlobalOverviewCamera
        if (overviewCamera == null)
        {
            overviewCamera = FindObjectOfType<GlobalOverviewCamera>();
            if (overviewCamera == null)
            {
                Debug.LogWarning("GlobalOverviewUI: 未找到GlobalOverviewCamera，请确保场景中有GlobalOverviewCamera组件。");
            }
        }

        // 创建UI元素
        CreateUIElements();
    }

    void Start()
    {
        // 设置RenderTexture
        SetupRenderTexture();

        // 设置初始显示状态
        if (showOnStart)
        {
            Show();
        }
        else
        {
            Hide();
        }
    }

    void Update()
    {
        // 检查按键切换
        if (allowToggle && Input.GetKeyDown(toggleKey))
        {
            Toggle();
        }
    }

    /// <summary>
    /// 创建UI元素
    /// </summary>
    private void CreateUIElements()
    {
        // 查找或创建Canvas
        Canvas canvas = FindObjectOfType<Canvas>();
        if (canvas == null)
        {
            // 如果没有Canvas，创建一个
            GameObject canvasObj = new GameObject("GlobalOverviewCanvas");
            canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvasObj.AddComponent<CanvasScaler>();
            canvasObj.AddComponent<GraphicRaycaster>();
            
            if (showDebugLog)
            {
                Debug.Log("GlobalOverviewUI: 已创建新的Canvas用于全局视窗");
            }
        }

        // 创建Panel（作为容器）
        overviewPanel = new GameObject("GlobalOverviewPanel");
        overviewPanel.transform.SetParent(canvas.transform, false);
        
        RectTransform panelRect = overviewPanel.AddComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0f, 1f); // 左上角
        panelRect.anchorMax = new Vector2(0f, 1f);
        panelRect.pivot = new Vector2(0f, 1f);
        panelRect.anchoredPosition = Vector2.zero;

        // 添加背景Image（可选）
        // 优先级：背景图片 > 背景颜色 > 无背景
        if (backgroundSprite != null)
        {
            // 使用背景图片
            backgroundImage = overviewPanel.AddComponent<Image>();
            backgroundImage.sprite = backgroundSprite;
            backgroundImage.type = Image.Type.Sliced; // 使用Sliced类型，支持9-slice缩放
            backgroundImage.color = Color.white; // 使用白色，保持图片原始颜色
            
            if (showDebugLog)
            {
                Debug.Log($"GlobalOverviewUI: 已设置背景图片: {backgroundSprite.name}");
            }
        }
        else if (backgroundColor.a > 0f)
        {
            // 使用背景颜色
            backgroundImage = overviewPanel.AddComponent<Image>();
            backgroundImage.color = backgroundColor;
            
            if (showDebugLog)
            {
                Debug.Log("GlobalOverviewUI: 已设置背景颜色");
            }
        }

        // 创建RawImage用于显示RenderTexture
        GameObject imageObj = new GameObject("OverviewImage");
        imageObj.transform.SetParent(overviewPanel.transform, false);
        
        overviewImage = imageObj.AddComponent<RawImage>();
        RectTransform imageRect = overviewImage.GetComponent<RectTransform>();
        imageRect.anchorMin = Vector2.zero;
        imageRect.anchorMax = Vector2.one;
        imageRect.sizeDelta = Vector2.zero;
        imageRect.anchoredPosition = Vector2.zero;

        // 如果有边框，创建边框Image
        if (showBorder)
        {
            CreateBorder();
        }

        // 初始设置为隐藏
        overviewPanel.SetActive(false);

        if (showDebugLog)
        {
            Debug.Log("GlobalOverviewUI: UI元素已创建");
        }
    }

    /// <summary>
    /// 创建边框
    /// </summary>
    private void CreateBorder()
    {
        // 创建边框GameObject（使用4个Image作为边框的4条边）
        // 这里简化处理，只创建一个外边框
        GameObject borderObj = new GameObject("Border");
        borderObj.transform.SetParent(overviewPanel.transform, false);
        
        borderImage = borderObj.AddComponent<Image>();
        borderImage.color = borderColor;
        
        RectTransform borderRect = borderImage.GetComponent<RectTransform>();
        borderRect.anchorMin = Vector2.zero;
        borderRect.anchorMax = Vector2.one;
        borderRect.sizeDelta = new Vector2(borderWidth * 2f, borderWidth * 2f);
        borderRect.anchoredPosition = Vector2.zero;
        
        // 将边框放在最底层（在背景和图像下方）
        borderObj.transform.SetAsFirstSibling();
    }

    /// <summary>
    /// 设置RenderTexture
    /// </summary>
    private void SetupRenderTexture()
    {
        if (overviewCamera == null)
        {
            Debug.LogWarning("GlobalOverviewUI: overviewCamera为null，无法设置RenderTexture");
            return;
        }

        RenderTexture rt = overviewCamera.GetRenderTexture();
        if (rt == null)
        {
            Debug.LogWarning("GlobalOverviewUI: RenderTexture为null，请确保GlobalOverviewCamera已正确初始化");
            return;
        }

        if (overviewImage != null)
        {
            overviewImage.texture = rt;
            
            // 计算并设置视窗大小
            UpdateViewportSize();
            
            if (showDebugLog)
            {
                Debug.Log($"GlobalOverviewUI: 已设置RenderTexture ({rt.width}x{rt.height})");
            }
        }
    }

    /// <summary>
    /// 更新视窗大小和位置
    /// </summary>
    private void UpdateViewportSize()
    {
        if (overviewPanel == null || overviewImage == null) return;

        RectTransform panelRect = overviewPanel.GetComponent<RectTransform>();
        if (panelRect == null) return;

        // 获取屏幕尺寸
        float screenWidth = Screen.width;
        float screenHeight = Screen.height;

        // 计算视窗大小
        float viewportWidth = screenWidth * sizeRatio;
        float viewportHeight;

        // 计算高度（根据宽高比）
        if (aspectRatio > 0f)
        {
            viewportHeight = viewportWidth / aspectRatio;
        }
        else
        {
            // 使用RenderTexture的宽高比
            RenderTexture rt = overviewCamera != null ? overviewCamera.GetRenderTexture() : null;
            if (rt != null)
            {
                float rtAspect = (float)rt.width / rt.height;
                viewportHeight = viewportWidth / rtAspect;
            }
            else
            {
                viewportHeight = viewportWidth; // 默认1:1
            }
        }

        // 设置Panel大小
        panelRect.sizeDelta = new Vector2(viewportWidth, viewportHeight);

        // 设置Panel位置（左上角）
        panelRect.anchoredPosition = new Vector2(
            screenWidth * screenPosition.x,
            -screenHeight * (1f - screenPosition.y)
        );

        if (showDebugLog && Time.frameCount % 60 == 0) // 每60帧打印一次
        {
            Debug.Log($"GlobalOverviewUI: 视窗大小: {viewportWidth}x{viewportHeight}, 位置: {panelRect.anchoredPosition}");
        }
    }

    /// <summary>
    /// 显示全局视窗
    /// </summary>
    public void Show()
    {
        if (overviewPanel != null)
        {
            overviewPanel.SetActive(true);
            
            if (showDebugLog)
            {
                Debug.Log("GlobalOverviewUI: 全局视窗已显示");
            }
        }
    }

    /// <summary>
    /// 隐藏全局视窗
    /// </summary>
    public void Hide()
    {
        if (overviewPanel != null)
        {
            overviewPanel.SetActive(false);
            
            if (showDebugLog)
            {
                Debug.Log("GlobalOverviewUI: 全局视窗已隐藏");
            }
        }
    }

    /// <summary>
    /// 切换显示/隐藏
    /// </summary>
    public void Toggle()
    {
        if (overviewPanel != null)
        {
            bool isActive = overviewPanel.activeSelf;
            if (isActive)
            {
                Hide();
            }
            else
            {
                Show();
            }
        }
    }

    /// <summary>
    /// 设置视窗大小比例
    /// </summary>
    public void SetSizeRatio(float ratio)
    {
        sizeRatio = Mathf.Clamp01(ratio);
        UpdateViewportSize();
    }

    /// <summary>
    /// 设置视窗位置
    /// </summary>
    public void SetScreenPosition(Vector2 position)
    {
        screenPosition = new Vector2(
            Mathf.Clamp01(position.x),
            Mathf.Clamp01(position.y)
        );
        UpdateViewportSize();
    }

    /// <summary>
    /// 设置背景图片
    /// </summary>
    public void SetBackgroundSprite(Sprite sprite)
    {
        backgroundSprite = sprite;
        
        if (backgroundImage != null)
        {
            if (sprite != null)
            {
                backgroundImage.sprite = sprite;
                backgroundImage.type = Image.Type.Sliced;
                backgroundImage.color = Color.white;
                
                if (showDebugLog)
                {
                    Debug.Log($"GlobalOverviewUI: 已更新背景图片: {sprite.name}");
                }
            }
            else
            {
                // 如果没有图片，使用背景颜色
                backgroundImage.sprite = null;
                backgroundImage.color = backgroundColor;
                
                if (showDebugLog)
                {
                    Debug.Log("GlobalOverviewUI: 已移除背景图片，使用背景颜色");
                }
            }
        }
    }

    /// <summary>
    /// 设置背景颜色（仅在未设置背景图片时生效）
    /// </summary>
    public void SetBackgroundColor(Color color)
    {
        backgroundColor = color;
        
        if (backgroundImage != null && backgroundSprite == null)
        {
            backgroundImage.color = color;
            
            if (showDebugLog)
            {
                Debug.Log($"GlobalOverviewUI: 已更新背景颜色: {color}");
            }
        }
    }

    void OnDestroy()
    {
        // 清理UI元素（如果需要）
        if (overviewPanel != null)
        {
            Destroy(overviewPanel);
        }
    }
}

