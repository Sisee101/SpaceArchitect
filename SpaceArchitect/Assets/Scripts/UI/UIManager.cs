using UnityEngine;

/// <summary>
/// UI管理器（单例）
/// 统一管理所有UI面板的显示和隐藏
/// </summary>
public class UIManager : MonoBehaviour
{
    private static UIManager _instance;
    
    /// <summary>
    /// 获取UIManager单例
    /// </summary>
    public static UIManager Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindObjectOfType<UIManager>();
                
                if (_instance == null)
                {
                    GameObject go = new GameObject("UIManager");
                    _instance = go.AddComponent<UIManager>();
                }
            }
            return _instance;
        }
    }
    
    [Header("主菜单面板（开始页面）")]
    [SerializeField] private MainMenuPanel mainMenuPanel;
    
    [Header("主界面控制器（主界面场景）")]
    [SerializeField] private MainHubController mainHubController;
    
    [Header("行星图鉴面板")]
    [SerializeField] private PlanetEncyclopediaPanel planetEncyclopediaPanel;
    
    [Header("设置面板")]
    [SerializeField] private SettingsPanel settingsPanel;
    
    [Header("员工手册面板")]
    [SerializeField] private EmployeeHandbookPanel employeeHandbookPanel;
    
    [Header("基站等级面板")]
    [SerializeField] private StationLevelPanel stationLevelPanel;
    
    void Awake()
    {
        // 确保单例
        if (_instance == null)
        {
            _instance = this;
        }
        else if (_instance != this)
        {
            Debug.LogWarning("检测到多个UIManager实例，销毁重复的实例");
            Destroy(gameObject);
            return;
        }
        
        // 初始化：显示主菜单，隐藏其他面板
        InitializePanels();
    }
    
    /// <summary>
    /// 初始化所有面板
    /// </summary>
    private void InitializePanels()
    {
        // 根据当前场景决定显示哪个面板
        string currentSceneName = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
        
        if (currentSceneName == "00_MainMenu")
        {
            // 主菜单场景：显示主菜单面板
            if (mainMenuPanel != null)
            {
                mainMenuPanel.Show();
            }
        }
        else if (currentSceneName == "01_MainHub")
        {
            // 主界面场景：隐藏所有子面板，显示主界面
            if (mainHubController != null)
            {
                // 主界面控制器会自动处理显示
            }
        }
        
        // 隐藏所有子面板（由各自场景控制显示）
        if (planetEncyclopediaPanel != null)
        {
            planetEncyclopediaPanel.Hide();
        }
        
        if (settingsPanel != null)
        {
            settingsPanel.Hide();
        }
        
        if (employeeHandbookPanel != null)
        {
            employeeHandbookPanel.Hide();
        }
        
        if (stationLevelPanel != null)
        {
            stationLevelPanel.Hide();
        }
    }
    
    /// <summary>
    /// 显示主菜单
    /// </summary>
    public void ShowMainMenu()
    {
        HideAllPanels();
        if (mainMenuPanel != null)
        {
            mainMenuPanel.Show();
        }
    }
    
    /// <summary>
    /// 显示行星图鉴
    /// </summary>
    public void ShowPlanetEncyclopedia()
    {
        HideAllPanels();
        if (planetEncyclopediaPanel != null)
        {
            planetEncyclopediaPanel.Show();
        }
    }
    
    /// <summary>
    /// 显示设置面板
    /// </summary>
    public void ShowSettings()
    {
        HideAllPanels();
        if (settingsPanel != null)
        {
            settingsPanel.Show();
        }
    }
    
    /// <summary>
    /// 隐藏所有面板
    /// </summary>
    private void HideAllPanels()
    {
        if (mainMenuPanel != null && mainMenuPanel.gameObject.activeSelf)
        {
            mainMenuPanel.Hide();
        }
        
        if (planetEncyclopediaPanel != null && planetEncyclopediaPanel.gameObject.activeSelf)
        {
            planetEncyclopediaPanel.Hide();
        }
        
        if (settingsPanel != null && settingsPanel.gameObject.activeSelf)
        {
            settingsPanel.Hide();
        }
        
        if (employeeHandbookPanel != null && employeeHandbookPanel.gameObject.activeSelf)
        {
            employeeHandbookPanel.Hide();
        }
        
        if (stationLevelPanel != null && stationLevelPanel.gameObject.activeSelf)
        {
            stationLevelPanel.Hide();
        }
    }
    
    /// <summary>
    /// 隐藏所有面板（除了指定的面板）
    /// </summary>
    private void HideAllPanelsExcept(MonoBehaviour exceptPanel)
    {
        if (mainMenuPanel != null && mainMenuPanel != exceptPanel && mainMenuPanel.gameObject.activeSelf)
        {
            mainMenuPanel.Hide();
        }
        
        if (planetEncyclopediaPanel != null && planetEncyclopediaPanel != exceptPanel && planetEncyclopediaPanel.gameObject.activeSelf)
        {
            planetEncyclopediaPanel.Hide();
        }
        
        if (settingsPanel != null && settingsPanel != exceptPanel && settingsPanel.gameObject.activeSelf)
        {
            settingsPanel.Hide();
        }
        
        if (employeeHandbookPanel != null && employeeHandbookPanel != exceptPanel && employeeHandbookPanel.gameObject.activeSelf)
        {
            employeeHandbookPanel.Hide();
        }
        
        if (stationLevelPanel != null && stationLevelPanel != exceptPanel && stationLevelPanel.gameObject.activeSelf)
        {
            stationLevelPanel.Hide();
        }
    }
    
    /// <summary>
    /// 显示员工手册
    /// </summary>
    public void ShowEmployeeHandbook()
    {
        if (employeeHandbookPanel != null)
        {
            HideAllPanelsExcept(employeeHandbookPanel);
            employeeHandbookPanel.Show();
        }
        else
        {
            Debug.LogWarning("UIManager: EmployeeHandbookPanel未配置");
        }
    }
    
    /// <summary>
    /// 显示基站等级
    /// </summary>
    public void ShowStationLevel()
    {
        if (stationLevelPanel != null)
        {
            HideAllPanelsExcept(stationLevelPanel);
            stationLevelPanel.Show();
        }
        else
        {
            Debug.LogWarning("UIManager: StationLevelPanel未配置");
        }
    }
    
    /// <summary>
    /// 返回主菜单（从其他面板返回，用于主菜单场景）
    /// </summary>
    public void ReturnToMainMenu()
    {
        ShowMainMenu();
    }
    
    /// <summary>
    /// 返回主界面（从其他面板返回，用于主界面场景）
    /// </summary>
    public void ReturnToMainHub()
    {
        HideAllPanels();
        if (mainHubController != null)
        {
            mainHubController.ReturnToMainHub();
        }
        else
        {
            Debug.LogWarning("UIManager: MainHubController未配置");
        }
    }
}
