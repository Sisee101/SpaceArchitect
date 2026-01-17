using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.IO;

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
    
    [Tooltip("是否将日志输出到文件（Build 后调试用）")]
    [SerializeField] private bool logToFile = true;

    private GameObject overviewPanel;
    private Image backgroundImage;
    private Image borderImage;
    private static string logFilePath = "";

    void Awake()
    {
        // 初始化日志文件路径
        if (logToFile && string.IsNullOrEmpty(logFilePath))
        {
            logFilePath = Path.Combine(Application.persistentDataPath, "GlobalOverviewUI_Log.txt");
            // 清空旧日志
            if (File.Exists(logFilePath))
            {
                File.Delete(logFilePath);
            }
            WriteLog("=== GlobalOverviewUI 日志开始 ===");
        }
        
        if (showDebugLog)
        {
            Debug.Log("[GlobalOverviewUI] Awake() 开始执行");
        }
        WriteLog("[GlobalOverviewUI] Awake() 开始执行");
        
        // 自动查找GlobalOverviewCamera
        if (overviewCamera == null)
        {
            overviewCamera = FindObjectOfType<GlobalOverviewCamera>();
            if (overviewCamera == null)
            {
                Debug.LogWarning("[GlobalOverviewUI] 未找到GlobalOverviewCamera，请确保场景中有GlobalOverviewCamera组件。");
            }
            else if (showDebugLog)
            {
                Debug.Log($"[GlobalOverviewUI] 找到GlobalOverviewCamera: {overviewCamera.name}");
            }
        }

        // 关键修复：不在 Awake() 中创建 UI 元素，延迟到 Start() 中执行
        // 这样可以确保 Canvas 已经初始化完成
        // CreateUIElements() 将在 Start() 中调用
        
        if (showDebugLog)
        {
            Debug.Log("[GlobalOverviewUI] Awake() 完成，UI 元素将在 Start() 中创建");
        }
        WriteLog("[GlobalOverviewUI] Awake() 完成，UI 元素将在 Start() 中创建");
    }

    void Start()
    {
        if (showDebugLog)
        {
            Debug.Log($"[GlobalOverviewUI] Start() 开始执行，showOnStart={showOnStart}");
        }
        WriteLog($"[GlobalOverviewUI] Start() 开始执行，showOnStart={showOnStart}");
        
        // 关键修复：使用协程延迟执行，确保 Canvas 完全初始化
        StartCoroutine(InitializeUICoroutine());
        
        // 订阅成功事件
        if (EventManager.Instance != null)
        {
            EventManager.Instance.OnShipSucceed += OnShipSucceed;
            if (showDebugLog)
            {
                Debug.Log("[GlobalOverviewUI] 已订阅 OnShipSucceed 事件");
            }
        }
        else if (showDebugLog)
        {
            Debug.LogWarning("[GlobalOverviewUI] EventManager.Instance 为 null，无法订阅事件");
        }
    }
    
    /// <summary>
    /// 协程：延迟初始化 UI，确保 Canvas 已经初始化
    /// </summary>
    private IEnumerator InitializeUICoroutine()
    {
        // 等待一帧，确保所有 Awake() 和 Start() 都执行完毕
        yield return null;
        
        if (showDebugLog)
        {
            Debug.Log("[GlobalOverviewUI] InitializeUICoroutine() 开始执行");
        }
        WriteLog("[GlobalOverviewUI] InitializeUICoroutine() 开始执行");
        
        // 关键修复：无论 overviewPanel 是否存在，都检查并确保 UI 元素正确创建
        // 如果 overviewPanel 存在但不在正确的 Canvas 下，或者已损坏，重新创建
        bool needCreate = false;
        if (overviewPanel == null)
        {
            WriteLog("[GlobalOverviewUI] overviewPanel 为 null，需要创建");
            needCreate = true;
        }
        else
        {
            WriteLog($"[GlobalOverviewUI] overviewPanel 已存在: {overviewPanel.name}, activeSelf={overviewPanel.activeSelf}, parent={(overviewPanel.transform.parent != null ? overviewPanel.transform.parent.name : "null")}");
            
            // 检查 overviewPanel 是否在正确的 Canvas 下
            Canvas parentCanvas = overviewPanel.GetComponentInParent<Canvas>();
            if (parentCanvas == null)
            {
                WriteLog("[GlobalOverviewUI] overviewPanel 不在任何 Canvas 下，需要重新创建");
                Destroy(overviewPanel);
                overviewPanel = null;
                needCreate = true;
            }
            else
            {
                WriteLog($"[GlobalOverviewUI] overviewPanel 在 Canvas 下: {parentCanvas.name}");
            }
        }
        
        // 如果需要创建，现在创建
        if (needCreate)
        {
            WriteLog("[GlobalOverviewUI] 开始调用 CreateUIElements()");
            CreateUIElements();
            WriteLog($"[GlobalOverviewUI] CreateUIElements() 执行完成，overviewPanel={(overviewPanel != null ? overviewPanel.name : "null")}");
        }
        
        // 再次检查，确保创建成功
        if (overviewPanel == null)
        {
            string errorMsg = "[GlobalOverviewUI] CreateUIElements() 失败！overviewPanel 仍为 null！";
            Debug.LogError(errorMsg);
            WriteLog(errorMsg);
            yield break;
        }
        
        WriteLog($"[GlobalOverviewUI] overviewPanel 验证通过: {overviewPanel.name}, activeSelf={overviewPanel.activeSelf}");
        
        WriteLog("[GlobalOverviewUI] 开始设置 RenderTexture");
        // 设置RenderTexture
        SetupRenderTexture();
        WriteLog("[GlobalOverviewUI] RenderTexture 设置完成");

        // 设置初始显示状态
        if (showOnStart)
        {
            if (showDebugLog)
            {
                Debug.Log("[GlobalOverviewUI] showOnStart=true，调用 Show()");
            }
            WriteLog("[GlobalOverviewUI] showOnStart=true，调用 Show()");
            Show();
        }
        else
        {
            if (showDebugLog)
            {
                Debug.Log("[GlobalOverviewUI] showOnStart=false，调用 Hide()");
            }
            WriteLog("[GlobalOverviewUI] showOnStart=false，调用 Hide()");
            Hide();
        }
        
        if (showDebugLog)
        {
            Debug.Log($"[GlobalOverviewUI] InitializeUICoroutine() 完成，overviewPanel.activeSelf={(overviewPanel != null ? overviewPanel.activeSelf.ToString() : "null")}");
        }
        WriteLog($"[GlobalOverviewUI] InitializeUICoroutine() 完成，overviewPanel.activeSelf={(overviewPanel != null ? overviewPanel.activeSelf.ToString() : "null")}");
    }

    void Update()
    {
        // 检查按键切换
        if (allowToggle && Input.GetKeyDown(toggleKey))
        {
            if (showDebugLog)
            {
                Debug.Log($"[GlobalOverviewUI] Tab 键被按下，准备切换显示。overviewPanel={(overviewPanel != null ? overviewPanel.name : "null")}, overviewPanel.activeSelf={(overviewPanel != null ? overviewPanel.activeSelf.ToString() : "null")}");
            }
            Toggle();
        }
    }

    /// <summary>
    /// 创建UI元素
    /// </summary>
    private void CreateUIElements()
    {
        if (showDebugLog)
        {
            Debug.Log("[GlobalOverviewUI] CreateUIElements() 开始");
        }
        WriteLog("[GlobalOverviewUI] CreateUIElements() 开始");
        
        // 关键修复：查找 Canvas，如果没找到就创建一个
        // 使用多种方法查找，确保在 Build 后也能找到
        Canvas canvas = null;
        
        // 方法1：优先通过名称查找 GameCanvas（最可靠）
        WriteLog("[GlobalOverviewUI] 方法1：使用 GameObject.Find('GameCanvas') 查找");
        GameObject gameCanvasObj = GameObject.Find("GameCanvas");
        if (gameCanvasObj != null)
        {
            WriteLog($"[GlobalOverviewUI] 找到 GameCanvas GameObject: {gameCanvasObj.name}");
            canvas = gameCanvasObj.GetComponent<Canvas>();
            if (canvas == null)
            {
                WriteLog("[GlobalOverviewUI] GameCanvas 没有 Canvas 组件，尝试 GetComponentInChildren");
                canvas = gameCanvasObj.GetComponentInChildren<Canvas>();
            }
            if (canvas != null)
            {
                // 检查是否是屏幕空间 Canvas
                if (canvas.renderMode == RenderMode.ScreenSpaceOverlay || canvas.renderMode == RenderMode.ScreenSpaceCamera)
                {
                    WriteLog($"[GlobalOverviewUI] 方法1成功：找到屏幕空间 Canvas: {canvas.name}, renderMode={canvas.renderMode}");
                }
                else
                {
                    WriteLog($"[GlobalOverviewUI] GameCanvas 不是屏幕空间 Canvas (renderMode={canvas.renderMode})，继续查找其他 Canvas");
                    canvas = null; // 重置，继续查找
                }
            }
        }
        else
        {
            WriteLog("[GlobalOverviewUI] 方法1失败：未找到 GameCanvas GameObject");
        }
        
        // 方法2：查找所有 Canvas，筛选出屏幕空间的
        if (canvas == null)
        {
            WriteLog("[GlobalOverviewUI] 方法2：查找所有屏幕空间 Canvas");
            Canvas[] allCanvases = FindObjectsOfType<Canvas>();
            foreach (Canvas c in allCanvases)
            {
                if (c.renderMode == RenderMode.ScreenSpaceOverlay || c.renderMode == RenderMode.ScreenSpaceCamera)
                {
                    canvas = c;
                    WriteLog($"[GlobalOverviewUI] 方法2成功：找到屏幕空间 Canvas: {canvas.name}, renderMode={canvas.renderMode}");
                    break;
                }
                else
                {
                    WriteLog($"[GlobalOverviewUI] 跳过非屏幕空间 Canvas: {c.name}, renderMode={c.renderMode}");
                }
            }
            if (canvas == null)
            {
                WriteLog("[GlobalOverviewUI] 方法2失败：未找到屏幕空间 Canvas");
            }
        }
        
        // 方法3：如果还是没找到，创建一个新的
        if (canvas == null)
        {
            WriteLog("[GlobalOverviewUI] 方法3：创建新的 Canvas");
            GameObject canvasObj = new GameObject("GlobalOverviewCanvas");
            canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 100; // 设置较高的排序顺序，确保显示在最上层
            CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            canvasObj.AddComponent<GraphicRaycaster>();
            
            if (showDebugLog)
            {
                Debug.Log("[GlobalOverviewUI] 已创建新的Canvas用于全局视窗");
            }
            WriteLog("[GlobalOverviewUI] 已创建新的Canvas用于全局视窗");
        }
        else
        {
            // 确保 Canvas 的排序顺序足够高，不会被其他 UI 遮挡
            if (canvas.sortingOrder < 100)
            {
                canvas.sortingOrder = 100;
                if (showDebugLog)
                {
                    Debug.Log($"[GlobalOverviewUI] 已提高 Canvas 的 sortingOrder 到 100: {canvas.name}");
                }
            }
            
            if (showDebugLog)
            {
                Debug.Log($"[GlobalOverviewUI] 找到现有Canvas: {canvas.name}, renderMode={canvas.renderMode}, sortingOrder={canvas.sortingOrder}");
            }
        }
        
        if (canvas == null)
        {
            Debug.LogError("[GlobalOverviewUI] 无法找到或创建 Canvas！UI 元素创建失败！");
            return;
        }

        // 创建Panel（作为容器）
        WriteLog("[GlobalOverviewUI] 开始创建 overviewPanel GameObject");
        overviewPanel = new GameObject("GlobalOverviewPanel");
        WriteLog($"[GlobalOverviewUI] overviewPanel GameObject 已创建: {overviewPanel.name}");
        
        WriteLog($"[GlobalOverviewUI] 设置 overviewPanel 的父对象为 Canvas: {canvas.name}");
        overviewPanel.transform.SetParent(canvas.transform, false);
        WriteLog($"[GlobalOverviewUI] overviewPanel 的父对象已设置: {(overviewPanel.transform.parent != null ? overviewPanel.transform.parent.name : "null")}");
        
        WriteLog("[GlobalOverviewUI] 添加 RectTransform 组件");
        RectTransform panelRect = overviewPanel.AddComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0f, 1f); // 左上角
        panelRect.anchorMax = new Vector2(0f, 1f);
        panelRect.pivot = new Vector2(0f, 1f);
        panelRect.anchoredPosition = Vector2.zero;
        WriteLog($"[GlobalOverviewUI] RectTransform 已配置: anchorMin={panelRect.anchorMin}, anchorMax={panelRect.anchorMax}, pivot={panelRect.pivot}");

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
        WriteLog("[GlobalOverviewUI] 开始创建 OverviewImage GameObject");
        GameObject imageObj = new GameObject("OverviewImage");
        imageObj.transform.SetParent(overviewPanel.transform, false);
        WriteLog($"[GlobalOverviewUI] OverviewImage GameObject 已创建: {imageObj.name}");
        
        overviewImage = imageObj.AddComponent<RawImage>();
        WriteLog($"[GlobalOverviewUI] RawImage 组件已添加: {(overviewImage != null ? "成功" : "失败")}");
        RectTransform imageRect = overviewImage.GetComponent<RectTransform>();
        imageRect.anchorMin = Vector2.zero;
        imageRect.anchorMax = Vector2.one;
        imageRect.sizeDelta = Vector2.zero;
        imageRect.anchoredPosition = Vector2.zero;
        WriteLog($"[GlobalOverviewUI] OverviewImage RectTransform 已配置");

        // 如果有边框，创建边框Image
        if (showBorder)
        {
            CreateBorder();
        }

        // 初始设置为隐藏
        overviewPanel.SetActive(false);
        
        // 关键修复：确保 Panel 在 Canvas 的最上层显示
        overviewPanel.transform.SetAsLastSibling();

        if (showDebugLog)
        {
            Debug.Log($"[GlobalOverviewUI] UI元素已创建 - overviewPanel={overviewPanel.name}, overviewImage={overviewImage.name}, Canvas={canvas.name}, Panel父对象={overviewPanel.transform.parent.name}, Panel在Canvas中的位置={overviewPanel.transform.GetSiblingIndex()}");
        }
        WriteLog($"[GlobalOverviewUI] UI元素已创建 - overviewPanel={overviewPanel.name}, overviewImage={overviewImage.name}, Canvas={canvas.name}, Panel父对象={overviewPanel.transform.parent.name}, Panel在Canvas中的位置={overviewPanel.transform.GetSiblingIndex()}");
        
        // 关键检查：确保 Panel 已正确创建
        if (overviewPanel == null)
        {
            string errorMsg = "[GlobalOverviewUI] CreateUIElements() 失败：overviewPanel 为 null！";
            Debug.LogError(errorMsg);
            WriteLog(errorMsg);
        }
        if (overviewImage == null)
        {
            string errorMsg = "[GlobalOverviewUI] CreateUIElements() 失败：overviewImage 为 null！";
            Debug.LogError(errorMsg);
            WriteLog(errorMsg);
        }
        
        // 验证 Panel 是否在正确的 Canvas 下
        if (overviewPanel != null && overviewPanel.transform.parent != canvas.transform)
        {
            string errorMsg = $"[GlobalOverviewUI] Panel 的父对象不正确！应该是 {canvas.name}，但实际是 {overviewPanel.transform.parent.name}";
            Debug.LogError(errorMsg);
            WriteLog(errorMsg);
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
        if (showDebugLog)
        {
            Debug.Log("[GlobalOverviewUI] SetupRenderTexture() 开始");
        }
        
        if (overviewCamera == null)
        {
            Debug.LogWarning("[GlobalOverviewUI] overviewCamera为null，无法设置RenderTexture");
            return;
        }

        RenderTexture rt = overviewCamera.GetRenderTexture();
        if (rt == null)
        {
            Debug.LogWarning("[GlobalOverviewUI] RenderTexture为null，请确保GlobalOverviewCamera已正确初始化");
            return;
        }

        if (overviewImage != null)
        {
            overviewImage.texture = rt;
            
            // 计算并设置视窗大小
            UpdateViewportSize();
            
            if (showDebugLog)
            {
                Debug.Log($"[GlobalOverviewUI] 已设置RenderTexture ({rt.width}x{rt.height})，overviewImage.texture={overviewImage.texture.name}");
            }
        }
        else
        {
            Debug.LogError("[GlobalOverviewUI] SetupRenderTexture() 失败：overviewImage 为 null！");
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
                Debug.Log($"[GlobalOverviewUI] Show() 已调用，overviewPanel.activeSelf={overviewPanel.activeSelf}, overviewImage={(overviewImage != null ? overviewImage.name : "null")}, overviewImage.texture={(overviewImage != null && overviewImage.texture != null ? overviewImage.texture.name : "null")}");
            }
        }
        else
        {
            Debug.LogError("[GlobalOverviewUI] Show() 失败：overviewPanel 为 null！");
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
            if (showDebugLog)
            {
                Debug.Log($"[GlobalOverviewUI] Toggle() 被调用，当前状态: isActive={isActive}");
            }
            if (isActive)
            {
                Hide();
            }
            else
            {
                Show();
            }
        }
        else
        {
            Debug.LogError("[GlobalOverviewUI] Toggle() 失败：overviewPanel 为 null！UI 元素可能没有创建成功。");
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

    /// <summary>
    /// 飞船成功事件处理（隐藏全局视窗）
    /// </summary>
    private void OnShipSucceed(GameObject destination, GameObject ship)
    {
        Hide();
        
        if (showDebugLog)
        {
            Debug.Log("GlobalOverviewUI: 飞船成功，已隐藏全局视窗");
        }
    }

    /// <summary>
    /// 写入日志到文件
    /// </summary>
    private void WriteLog(string message)
    {
        if (!logToFile || string.IsNullOrEmpty(logFilePath)) return;
        
        try
        {
            string timestamp = System.DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
            string logMessage = $"[{timestamp}] {message}\n";
            File.AppendAllText(logFilePath, logMessage);
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[GlobalOverviewUI] 写入日志文件失败: {e.Message}");
        }
    }
    
    void OnDestroy()
    {
        WriteLog("=== GlobalOverviewUI 日志结束 ===");
        
        // 取消订阅成功事件
        if (EventManager.Instance != null)
        {
            EventManager.Instance.OnShipSucceed -= OnShipSucceed;
        }
        
        // 清理UI元素（如果需要）
        if (overviewPanel != null)
        {
            Destroy(overviewPanel);
        }
    }
}

