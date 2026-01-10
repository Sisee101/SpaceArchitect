using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 主菜单面板控制器
/// 处理主菜单的所有按钮点击事件
/// </summary>
public class MainMenuPanel : MonoBehaviour
{
    [Header("按钮引用")]
    [SerializeField] private Button startGameButton;
    [SerializeField] private Button planetEncyclopediaButton;
    [SerializeField] private Button settingsButton;
    [SerializeField] private Button quitButton;
    
    void Start()
    {
        // 绑定按钮事件
        if (startGameButton != null)
        {
            startGameButton.onClick.AddListener(OnStartGameClicked);
        }
        
        if (planetEncyclopediaButton != null)
        {
            planetEncyclopediaButton.onClick.AddListener(OnPlanetEncyclopediaClicked);
        }
        
        if (settingsButton != null)
        {
            settingsButton.onClick.AddListener(OnSettingsClicked);
        }
        
        if (quitButton != null)
        {
            quitButton.onClick.AddListener(OnQuitClicked);
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

