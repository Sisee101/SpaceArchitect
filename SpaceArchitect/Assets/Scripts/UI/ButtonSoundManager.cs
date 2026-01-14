using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// 按钮音效管理器（场景级）
/// 统一管理场景中所有面板的按钮音效配置和播放
/// 支持按面板分组配置，支持自动查找按钮，最便于操作和统一管理
/// </summary>
public class ButtonSoundManager : MonoBehaviour
{
    private static ButtonSoundManager _instance;
    
    /// <summary>
    /// 获取ButtonSoundManager单例（场景级）
    /// </summary>
    public static ButtonSoundManager Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindObjectOfType<ButtonSoundManager>();
            }
            return _instance;
        }
    }
    
    [Header("音频源配置")]
    [Tooltip("音频源组件（如果为空，自动创建）")]
    [SerializeField] private AudioSource audioSource;
    
    [Header("默认音效")]
    [Tooltip("默认点击音效（所有按钮共用，除非单独配置）")]
    [SerializeField] private AudioClip defaultClickSound;
    
    [Tooltip("默认悬停音效（所有按钮共用，除非单独配置，可选）")]
    [SerializeField] private AudioClip defaultHoverSound;
    
    [Header("面板配置")]
    [Tooltip("按面板分组的按钮音效配置")]
    [SerializeField] private List<PanelSoundConfig> panelConfigs = new List<PanelSoundConfig>();
    
    [Header("调试")]
    [SerializeField] private bool enableDebugLog = false;
    
    // 按钮到配置的快速查找字典（运行时构建）
    private Dictionary<Button, ButtonSoundConfig> buttonConfigMap = new Dictionary<Button, ButtonSoundConfig>();
    
    void Awake()
    {
        // 确保单例
        if (_instance == null)
        {
            _instance = this;
        }
        else if (_instance != this)
        {
            Debug.LogWarning("检测到多个ButtonSoundManager实例，销毁重复的实例");
            Destroy(gameObject);
            return;
        }
    }
    
    void Start()
    {
        // 初始化音频源
        InitializeAudioSource();
        
        // 构建快速查找字典
        BuildButtonConfigMap();
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
                if (enableDebugLog)
                {
                    Debug.Log("ButtonSoundManager: 自动创建AudioSource组件");
                }
            }
        }
    }
    
    /// <summary>
    /// 构建按钮配置快速查找字典
    /// </summary>
    private void BuildButtonConfigMap()
    {
        buttonConfigMap.Clear();
        
        foreach (var panelConfig in panelConfigs)
        {
            if (panelConfig == null || panelConfig.buttonConfigs == null)
                continue;
            
            foreach (var buttonConfig in panelConfig.buttonConfigs)
            {
                if (buttonConfig != null && buttonConfig.button != null)
                {
                    // 如果字典中已存在该按钮，使用最新的配置（后添加的覆盖先添加的）
                    buttonConfigMap[buttonConfig.button] = buttonConfig;
                }
            }
        }
        
        if (enableDebugLog)
        {
            Debug.Log($"ButtonSoundManager: 已构建按钮配置字典，共 {buttonConfigMap.Count} 个按钮");
        }
    }
    
    /// <summary>
    /// 播放按钮点击音效
    /// </summary>
    /// <param name="button">按钮引用</param>
    public void PlayButtonClick(Button button)
    {
        if (button == null)
        {
            if (enableDebugLog)
            {
                Debug.LogWarning("ButtonSoundManager: 按钮引用为空，无法播放音效");
            }
            return;
        }
        
        // 查找按钮配置
        ButtonSoundConfig config = null;
        if (buttonConfigMap.ContainsKey(button))
        {
            config = buttonConfigMap[button];
        }
        
        // 确定使用的音效
        AudioClip soundToPlay = null;
        if (config != null && config.clickSound != null)
        {
            soundToPlay = config.clickSound;
        }
        else if (defaultClickSound != null)
        {
            soundToPlay = defaultClickSound;
        }
        
        // 播放音效
        if (soundToPlay != null && audioSource != null)
        {
            audioSource.PlayOneShot(soundToPlay);
            if (enableDebugLog)
            {
                Debug.Log($"ButtonSoundManager: 播放按钮点击音效 - 按钮: {button.name}, 音效: {soundToPlay.name}");
            }
        }
        else if (enableDebugLog)
        {
            Debug.LogWarning($"ButtonSoundManager: 按钮 {button.name} 没有配置音效，且默认音效也未配置");
        }
    }
    
    /// <summary>
    /// 播放按钮悬停音效
    /// </summary>
    /// <param name="button">按钮引用</param>
    public void PlayButtonHover(Button button)
    {
        if (button == null)
        {
            return;
        }
        
        // 查找按钮配置
        ButtonSoundConfig config = null;
        if (buttonConfigMap.ContainsKey(button))
        {
            config = buttonConfigMap[button];
        }
        
        // 确定使用的音效
        AudioClip soundToPlay = null;
        if (config != null && config.hoverSound != null)
        {
            soundToPlay = config.hoverSound;
        }
        else if (defaultHoverSound != null)
        {
            soundToPlay = defaultHoverSound;
        }
        
        // 播放音效
        if (soundToPlay != null && audioSource != null)
        {
            audioSource.PlayOneShot(soundToPlay);
            if (enableDebugLog)
            {
                Debug.Log($"ButtonSoundManager: 播放按钮悬停音效 - 按钮: {button.name}, 音效: {soundToPlay.name}");
            }
        }
    }
    
    /// <summary>
    /// 自动查找场景中所有面板的按钮（编辑器中使用）
    /// </summary>
    [ContextMenu("自动查找所有按钮")]
    public void AutoFindAllButtons()
    {
        if (enableDebugLog)
        {
            Debug.Log("ButtonSoundManager: 开始自动查找所有按钮...");
        }
        
        // 清空现有配置（可选，也可以保留并更新）
        // panelConfigs.Clear();
        
        // 查找各个面板的按钮
        AutoFindMainHubButtons();
        AutoFindOrderDetailPanelButtons();
        AutoFindEmployeeHandbookPanelButtons();
        AutoFindStationLevelPanelButtons();
        AutoFindPlanetEncyclopediaPanelButtons();
        
        // 重新构建查找字典
        BuildButtonConfigMap();
        
        if (enableDebugLog)
        {
            Debug.Log($"ButtonSoundManager: 自动查找完成，共配置 {panelConfigs.Count} 个面板");
        }
        
        #if UNITY_EDITOR
        // 标记场景为已修改，需要保存
        UnityEditor.EditorUtility.SetDirty(this);
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(gameObject.scene);
        #endif
    }
    
    /// <summary>
    /// 自动查找MainHubController的按钮
    /// </summary>
    private void AutoFindMainHubButtons()
    {
        MainHubController mainHub = FindObjectOfType<MainHubController>();
        if (mainHub == null)
        {
            if (enableDebugLog)
            {
                Debug.LogWarning("ButtonSoundManager: 未找到MainHubController");
            }
            return;
        }
        
        // 查找或创建面板配置
        PanelSoundConfig panelConfig = panelConfigs.FirstOrDefault(p => p.panelName == "MainHub");
        if (panelConfig == null)
        {
            panelConfig = new PanelSoundConfig("MainHub", mainHub.gameObject);
            panelConfigs.Add(panelConfig);
        }
        
        // 清空现有按钮配置（避免重复）
        panelConfig.buttonConfigs.Clear();
        
        // 使用反射访问私有字段
        var employeeHandbookButtonField = typeof(MainHubController).GetField("employeeHandbookButton", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var stationLevelButtonField = typeof(MainHubController).GetField("stationLevelButton", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var planetEncyclopediaButtonField = typeof(MainHubController).GetField("planetEncyclopediaButton", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var returnToMenuButtonField = typeof(MainHubController).GetField("returnToMenuButton", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        
        // 添加按钮配置
        if (employeeHandbookButtonField != null)
        {
            Button button = employeeHandbookButtonField.GetValue(mainHub) as Button;
            if (button != null)
            {
                panelConfig.buttonConfigs.Add(new ButtonSoundConfig("Employee Handbook Button", button));
            }
        }
        
        if (stationLevelButtonField != null)
        {
            Button button = stationLevelButtonField.GetValue(mainHub) as Button;
            if (button != null)
            {
                panelConfig.buttonConfigs.Add(new ButtonSoundConfig("Station Level Button", button));
            }
        }
        
        if (planetEncyclopediaButtonField != null)
        {
            Button button = planetEncyclopediaButtonField.GetValue(mainHub) as Button;
            if (button != null)
            {
                panelConfig.buttonConfigs.Add(new ButtonSoundConfig("Planet Encyclopedia Button", button));
            }
        }
        
        if (returnToMenuButtonField != null)
        {
            Button button = returnToMenuButtonField.GetValue(mainHub) as Button;
            if (button != null)
            {
                panelConfig.buttonConfigs.Add(new ButtonSoundConfig("Return To Menu Button", button));
            }
        }
        
        if (enableDebugLog)
        {
            Debug.Log($"ButtonSoundManager: MainHub - 找到 {panelConfig.buttonConfigs.Count} 个按钮");
        }
    }
    
    /// <summary>
    /// 自动查找OrderDetailPanel的按钮
    /// </summary>
    private void AutoFindOrderDetailPanelButtons()
    {
        OrderDetailPanel panel = FindObjectOfType<OrderDetailPanel>();
        if (panel == null)
        {
            if (enableDebugLog)
            {
                Debug.LogWarning("ButtonSoundManager: 未找到OrderDetailPanel");
            }
            return;
        }
        
        // 使用反射获取私有字段（因为按钮字段是私有的）
        var resetButtonField = typeof(OrderDetailPanel).GetField("resetVisitOrderButton", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var nextDayButtonField = typeof(OrderDetailPanel).GetField("nextDayButton", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        
        // 查找或创建面板配置
        PanelSoundConfig panelConfig = panelConfigs.FirstOrDefault(p => p.panelName == "OrderDetailPanel");
        if (panelConfig == null)
        {
            panelConfig = new PanelSoundConfig("OrderDetailPanel", panel.gameObject);
            panelConfigs.Add(panelConfig);
        }
        
        panelConfig.buttonConfigs.Clear();
        
        // 添加按钮配置
        if (resetButtonField != null)
        {
            Button resetButton = resetButtonField.GetValue(panel) as Button;
            if (resetButton != null)
            {
                panelConfig.buttonConfigs.Add(new ButtonSoundConfig("Reset Button", resetButton));
            }
        }
        
        if (nextDayButtonField != null)
        {
            Button nextDayButton = nextDayButtonField.GetValue(panel) as Button;
            if (nextDayButton != null)
            {
                panelConfig.buttonConfigs.Add(new ButtonSoundConfig("Next Day Button", nextDayButton));
            }
        }
        
        if (enableDebugLog)
        {
            Debug.Log($"ButtonSoundManager: OrderDetailPanel - 找到 {panelConfig.buttonConfigs.Count} 个按钮");
        }
    }
    
    /// <summary>
    /// 自动查找EmployeeHandbookPanel的按钮
    /// </summary>
    private void AutoFindEmployeeHandbookPanelButtons()
    {
        EmployeeHandbookPanel panel = FindObjectOfType<EmployeeHandbookPanel>();
        if (panel == null)
        {
            if (enableDebugLog)
            {
                Debug.LogWarning("ButtonSoundManager: 未找到EmployeeHandbookPanel");
            }
            return;
        }
        
        // 查找或创建面板配置
        PanelSoundConfig panelConfig = panelConfigs.FirstOrDefault(p => p.panelName == "EmployeeHandbookPanel");
        if (panelConfig == null)
        {
            panelConfig = new PanelSoundConfig("EmployeeHandbookPanel", panel.gameObject);
            panelConfigs.Add(panelConfig);
        }
        
        panelConfig.buttonConfigs.Clear();
        
        // 查找返回按钮
        var backButtonField = typeof(EmployeeHandbookPanel).GetField("backButton", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        if (backButtonField != null)
        {
            Button backButton = backButtonField.GetValue(panel) as Button;
            if (backButton != null)
            {
                panelConfig.buttonConfigs.Add(new ButtonSoundConfig("Back Button", backButton));
            }
        }
        
        // 查找HandbookOptionController的按钮
        HandbookOptionController optionController = panel.GetComponentInChildren<HandbookOptionController>();
        if (optionController != null)
        {
            var optionButtonsField = typeof(HandbookOptionController).GetField("optionButtons", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (optionButtonsField != null)
            {
                List<Button> optionButtons = optionButtonsField.GetValue(optionController) as List<Button>;
                if (optionButtons != null)
                {
                    for (int i = 0; i < optionButtons.Count; i++)
                    {
                        if (optionButtons[i] != null)
                        {
                            panelConfig.buttonConfigs.Add(new ButtonSoundConfig($"Option Button {i + 1}", optionButtons[i]));
                        }
                    }
                }
            }
        }
        
        // 查找HandbookImageViewer的按钮
        HandbookImageViewer imageViewer = panel.GetComponentInChildren<HandbookImageViewer>();
        if (imageViewer != null)
        {
            var leftArrowField = typeof(HandbookImageViewer).GetField("leftArrowButton", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var rightArrowField = typeof(HandbookImageViewer).GetField("rightArrowButton", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var imageViewerBackButtonField = typeof(HandbookImageViewer).GetField("backButton", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            
            if (leftArrowField != null)
            {
                Button leftArrow = leftArrowField.GetValue(imageViewer) as Button;
                if (leftArrow != null)
                {
                    panelConfig.buttonConfigs.Add(new ButtonSoundConfig("Image Viewer Left Arrow", leftArrow));
                }
            }
            
            if (rightArrowField != null)
            {
                Button rightArrow = rightArrowField.GetValue(imageViewer) as Button;
                if (rightArrow != null)
                {
                    panelConfig.buttonConfigs.Add(new ButtonSoundConfig("Image Viewer Right Arrow", rightArrow));
                }
            }
            
            if (imageViewerBackButtonField != null)
            {
                Button backButton = imageViewerBackButtonField.GetValue(imageViewer) as Button;
                if (backButton != null)
                {
                    panelConfig.buttonConfigs.Add(new ButtonSoundConfig("Image Viewer Back Button", backButton));
                }
            }
        }
        
        if (enableDebugLog)
        {
            Debug.Log($"ButtonSoundManager: EmployeeHandbookPanel - 找到 {panelConfig.buttonConfigs.Count} 个按钮");
        }
    }
    
    /// <summary>
    /// 自动查找StationLevelPanel的按钮
    /// </summary>
    private void AutoFindStationLevelPanelButtons()
    {
        StationLevelPanel panel = FindObjectOfType<StationLevelPanel>();
        if (panel == null)
        {
            if (enableDebugLog)
            {
                Debug.LogWarning("ButtonSoundManager: 未找到StationLevelPanel");
            }
            return;
        }
        
        // 查找或创建面板配置
        PanelSoundConfig panelConfig = panelConfigs.FirstOrDefault(p => p.panelName == "StationLevelPanel");
        if (panelConfig == null)
        {
            panelConfig = new PanelSoundConfig("StationLevelPanel", panel.gameObject);
            panelConfigs.Add(panelConfig);
        }
        
        panelConfig.buttonConfigs.Clear();
        
        // 查找返回按钮
        var backButtonField = typeof(StationLevelPanel).GetField("backButton", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        if (backButtonField != null)
        {
            Button backButton = backButtonField.GetValue(panel) as Button;
            if (backButton != null)
            {
                panelConfig.buttonConfigs.Add(new ButtonSoundConfig("Back Button", backButton));
            }
        }
        
        if (enableDebugLog)
        {
            Debug.Log($"ButtonSoundManager: StationLevelPanel - 找到 {panelConfig.buttonConfigs.Count} 个按钮");
        }
    }
    
    /// <summary>
    /// 自动查找PlanetEncyclopediaPanel的按钮
    /// </summary>
    private void AutoFindPlanetEncyclopediaPanelButtons()
    {
        PlanetEncyclopediaPanel panel = FindObjectOfType<PlanetEncyclopediaPanel>();
        if (panel == null)
        {
            if (enableDebugLog)
            {
                Debug.LogWarning("ButtonSoundManager: 未找到PlanetEncyclopediaPanel");
            }
            return;
        }
        
        // 查找或创建面板配置
        PanelSoundConfig panelConfig = panelConfigs.FirstOrDefault(p => p.panelName == "PlanetEncyclopediaPanel");
        if (panelConfig == null)
        {
            panelConfig = new PanelSoundConfig("PlanetEncyclopediaPanel", panel.gameObject);
            panelConfigs.Add(panelConfig);
        }
        
        panelConfig.buttonConfigs.Clear();
        
        // 使用反射访问私有字段（字段是private的，使用[SerializeField]）
        var backButtonField = typeof(PlanetEncyclopediaPanel).GetField("backButton", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var leftArrowField = typeof(PlanetEncyclopediaPanel).GetField("leftArrow", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var rightArrowField = typeof(PlanetEncyclopediaPanel).GetField("rightArrow", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        
        if (backButtonField != null)
        {
            Button backButton = backButtonField.GetValue(panel) as Button;
            if (backButton != null)
            {
                panelConfig.buttonConfigs.Add(new ButtonSoundConfig("Back Button", backButton));
            }
        }
        
        if (leftArrowField != null)
        {
            Button leftArrow = leftArrowField.GetValue(panel) as Button;
            if (leftArrow != null)
            {
                panelConfig.buttonConfigs.Add(new ButtonSoundConfig("Left Arrow", leftArrow));
            }
        }
        
        if (rightArrowField != null)
        {
            Button rightArrow = rightArrowField.GetValue(panel) as Button;
            if (rightArrow != null)
            {
                panelConfig.buttonConfigs.Add(new ButtonSoundConfig("Right Arrow", rightArrow));
            }
        }
        
        if (enableDebugLog)
        {
            Debug.Log($"ButtonSoundManager: PlanetEncyclopediaPanel - 找到 {panelConfig.buttonConfigs.Count} 个按钮");
        }
    }
    
    /// <summary>
    /// 手动添加按钮配置（供外部调用）
    /// </summary>
    public void AddButtonConfig(string panelName, string buttonName, Button button, AudioClip clickSound = null, AudioClip hoverSound = null)
    {
        // 查找或创建面板配置
        PanelSoundConfig panelConfig = panelConfigs.FirstOrDefault(p => p.panelName == panelName);
        if (panelConfig == null)
        {
            panelConfig = new PanelSoundConfig(panelName);
            panelConfigs.Add(panelConfig);
        }
        
        // 检查按钮是否已存在
        ButtonSoundConfig existingConfig = panelConfig.buttonConfigs.FirstOrDefault(b => b.button == button);
        if (existingConfig != null)
        {
            // 更新现有配置
            existingConfig.buttonName = buttonName;
            existingConfig.clickSound = clickSound;
            existingConfig.hoverSound = hoverSound;
        }
        else
        {
            // 添加新配置
            ButtonSoundConfig newConfig = new ButtonSoundConfig(buttonName, button);
            newConfig.clickSound = clickSound;
            newConfig.hoverSound = hoverSound;
            panelConfig.buttonConfigs.Add(newConfig);
        }
        
        // 重新构建查找字典
        BuildButtonConfigMap();
    }
    
    void OnDestroy()
    {
        if (_instance == this)
        {
            _instance = null;
        }
    }
}
