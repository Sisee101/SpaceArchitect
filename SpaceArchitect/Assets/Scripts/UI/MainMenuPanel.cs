using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

/// <summary>
/// 主菜单面板控制器
/// 处理主菜单的所有按钮点击事件和鼠标悬停高亮效果
/// </summary>
public class MainMenuPanel : MonoBehaviour
{
    [Header("按钮引用")]
    [SerializeField] private Button startGameButton;
    [SerializeField] private Button planetEncyclopediaButton;
    [SerializeField] private Button settingsButton;
    [SerializeField] private Button quitButton;
    
    [Header("高亮控制器")]
    [SerializeField] private MenuHighlightController highlightController; // 高亮跟随控制器
    
    void Start()
    {
        // 绑定按钮点击事件
        if (startGameButton != null)
        {
            startGameButton.onClick.AddListener(OnStartGameClicked);
            // 添加鼠标悬停高亮效果
            SetupButtonHoverHighlight(startGameButton);
        }
        
        if (planetEncyclopediaButton != null)
        {
            planetEncyclopediaButton.onClick.AddListener(OnPlanetEncyclopediaClicked);
            // 添加鼠标悬停高亮效果
            SetupButtonHoverHighlight(planetEncyclopediaButton);
        }
        
        if (settingsButton != null)
        {
            settingsButton.onClick.AddListener(OnSettingsClicked);
            // 添加鼠标悬停高亮效果
            SetupButtonHoverHighlight(settingsButton);
        }
        
        if (quitButton != null)
        {
            quitButton.onClick.AddListener(OnQuitClicked);
            // 添加鼠标悬停高亮效果
            SetupButtonHoverHighlight(quitButton);
        }
    }
    
    /// <summary>
    /// 为按钮设置鼠标悬停高亮效果
    /// </summary>
    /// <param name="button">目标按钮</param>
    private void SetupButtonHoverHighlight(Button button)
    {
        if (button == null) return;
        
        // 获取或添加 EventTrigger 组件
        EventTrigger eventTrigger = button.gameObject.GetComponent<EventTrigger>();
        if (eventTrigger == null)
        {
            eventTrigger = button.gameObject.AddComponent<EventTrigger>();
        }
        
        // 检查是否已经存在 PointerEnter 事件（避免重复添加）
        bool alreadyExists = false;
        foreach (EventTrigger.Entry entry in eventTrigger.triggers)
        {
            if (entry.eventID == EventTriggerType.PointerEnter)
            {
                alreadyExists = true;
                break;
            }
        }
        
        // 如果不存在，则添加
        if (!alreadyExists)
        {
            // 创建 PointerEnter 事件（鼠标进入）
            EventTrigger.Entry entryEnter = new EventTrigger.Entry();
            entryEnter.eventID = EventTriggerType.PointerEnter;
            entryEnter.callback.AddListener((eventData) => OnButtonHovered(button.GetComponent<RectTransform>()));
            
            // 添加到 EventTrigger
            eventTrigger.triggers.Add(entryEnter);
        }
    }
    
    /// <summary>
    /// 按钮鼠标悬停事件（通过按钮引用）
    /// </summary>
    /// <param name="buttonRect">按钮的 RectTransform</param>
    private void OnButtonHovered(RectTransform buttonRect)
    {
        if (highlightController != null && buttonRect != null)
        {
            highlightController.MoveToButton(buttonRect);
        }
    }
    
    /// <summary>
    /// 显示主菜单面板
    /// </summary>
    public void Show()
    {
        gameObject.SetActive(true);
    }
    
    /// <summary>
    /// 隐藏主菜单面板
    /// </summary>
    public void Hide()
    {
        gameObject.SetActive(false);
    }
    
    /// <summary>
    /// 开始游戏按钮点击事件
    /// </summary>
    private void OnStartGameClicked()
    {
        Debug.Log("开始游戏 - 跳转到主界面");
        if (SceneTransitionManager.Instance != null)
        {
            SceneTransitionManager.Instance.LoadMainHubScene();
        }
        else
        {
            Debug.LogError("SceneTransitionManager未找到！");
        }
    }
    
    /// <summary>
    /// 行星图鉴按钮点击事件
    /// </summary>
    private void OnPlanetEncyclopediaClicked()
    {
        Debug.Log("打开行星图鉴");
        if (UIManager.Instance != null)
        {
            UIManager.Instance.ShowPlanetEncyclopedia();
        }
        else
        {
            Debug.LogError("UIManager未找到！");
        }
    }
    
    /// <summary>
    /// 设置按钮点击事件
    /// </summary>
    private void OnSettingsClicked()
    {
        Debug.Log("打开设置");
        if (UIManager.Instance != null)
        {
            UIManager.Instance.ShowSettings();
        }
        else
        {
            Debug.LogError("UIManager未找到！");
        }
    }
    
    /// <summary>
    /// 退出游戏按钮点击事件
    /// </summary>
    private void OnQuitClicked()
    {
        Debug.Log("退出游戏");
        if (SceneTransitionManager.Instance != null)
        {
            SceneTransitionManager.Instance.QuitGame();
        }
        else
        {
            Debug.LogError("SceneTransitionManager未找到！");
        }
    }
}

