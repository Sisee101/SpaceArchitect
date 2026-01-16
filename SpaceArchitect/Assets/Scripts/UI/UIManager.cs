using UnityEngine;
using System.Collections; // 添加用于 IEnumerator 协程
using System.Collections.Generic; // 添加用于 HashSet
using UnityEngine.SceneManagement; // 添加用于 SceneManager

/// <summary>
/// UI管理器（场景级单例）
/// 统一管理当前场景所有UI面板的显示和隐藏
/// 每个场景拥有独立的UIManager实例，随场景加载和销毁
/// </summary>
public class UIManager : MonoBehaviour
{
    private static UIManager _instance;
    
    /// <summary>
    /// 获取当前场景的UIManager实例
    /// </summary>
    public static UIManager Instance
    {
        get
        {
            // 如果实例不存在或已被销毁，尝试在当前场景中查找
            if (_instance == null)
            {
                _instance = FindObjectOfType<UIManager>();
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
    
    [Header("员工手册面板")]
    [SerializeField] private EmployeeHandbookPanel employeeHandbookPanel;
    
    [Header("基站等级面板")]
    [SerializeField] private StationLevelPanel stationLevelPanel;
    
    [Header("邮箱面板")]
    [SerializeField] private MailPanel mailPanel;
    
    // 初始化标志，用于防止初始化期间的时序冲突
    private bool isInitializationComplete = false;
    
    // 记录用户主动显示的面板，避免在初始化期间被DelayedHidePanels隐藏
    private System.Collections.Generic.HashSet<MonoBehaviour> userActivatedPanels = new System.Collections.Generic.HashSet<MonoBehaviour>();
    
    void Awake()
    {
        // 设置当前场景的UIManager实例
        // 如果场景中已有其他UIManager实例，销毁当前重复的实例
        if (_instance != null && _instance != this)
        {
            Debug.LogWarning($"场景中检测到多个UIManager实例，销毁重复的实例: {gameObject.name}");
            Destroy(gameObject);
            return;
        }
        
        // 设置当前实例为场景级单例
        _instance = this;
        
        // 查找当前场景中的MainHubController
        if (mainHubController == null)
        {
            mainHubController = FindObjectOfType<MainHubController>();
        }
    }
    
    void OnDestroy()
    {
        // 当前实例被销毁时，清除静态引用（如果是当前实例）
        if (_instance == this)
        {
            _instance = null;
        }
    }
    
    void Start()
    {
        // 在Start中初始化面板，确保所有对象的Awake和OnEnable都已执行
        // 使用协程延迟，确保所有对象的Start()都已执行
        StartCoroutine(DelayedInitializePanels());
    }
    
    /// <summary>
    /// 延迟初始化面板，确保所有对象都已完全初始化
    /// </summary>
    private System.Collections.IEnumerator DelayedInitializePanels()
    {
        // 等待一帧，确保场景中的所有对象的Start()都已执行
        yield return null;
        
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
        else if (currentSceneName.Contains("_MainHub"))
        {
            // 所有MainHub场景（01_MainHub, 02_MainHub, 03_MainHub等）：隐藏所有子面板，显示主界面
            // 使用Contains检查，支持多个MainHub场景
            if (mainHubController != null)
            {
                // 主界面控制器会自动处理显示
            }
            else
            {
                // 如果mainHubController未配置，尝试自动查找当前场景中的MainHubController
                mainHubController = FindObjectOfType<MainHubController>();
                if (mainHubController != null)
                {
                    Debug.Log($"UIManager: 在场景 {currentSceneName} 中自动找到MainHubController");
                }
                else
                {
                    Debug.LogWarning($"UIManager: 场景 {currentSceneName} 中未找到MainHubController！");
                }
            }
        }
        
        // 使用协程延迟隐藏面板，确保所有对象的初始化都已完成
        StartCoroutine(DelayedHidePanels());
    }
    
    /// <summary>
    /// 延迟隐藏面板，确保所有对象的初始化都已完成
    /// </summary>
    private System.Collections.IEnumerator DelayedHidePanels()
    {
        // 等待一帧，确保所有对象的Start()都已执行
        yield return null;
        
        // 隐藏所有子面板（由各自场景控制显示）
        // 但不隐藏用户主动显示的面板（通过userActivatedPanels记录）
        if (planetEncyclopediaPanel != null && planetEncyclopediaPanel.gameObject.activeSelf && !userActivatedPanels.Contains(planetEncyclopediaPanel))
        {
            planetEncyclopediaPanel.Hide();
        }
        
        if (employeeHandbookPanel != null && employeeHandbookPanel.gameObject.activeSelf && !userActivatedPanels.Contains(employeeHandbookPanel))
        {
            employeeHandbookPanel.Hide();
        }
        
        if (stationLevelPanel != null && stationLevelPanel.gameObject.activeSelf && !userActivatedPanels.Contains(stationLevelPanel))
        {
            stationLevelPanel.Hide();
        }
        
        if (mailPanel != null && mailPanel.gameObject.activeSelf && !userActivatedPanels.Contains(mailPanel))
        {
            mailPanel.Hide();
        }
        
        // 标记初始化完成（在这之后用户点击按钮，面板不会被延迟隐藏）
        isInitializationComplete = true;
        
        // 延迟清空用户激活记录，避免与同一帧的用户点击冲突
        // 再等待一帧，确保这一帧内的用户点击都被处理
        StartCoroutine(ClearUserActivatedPanelsAfterDelay());
    }
    
    /// <summary>
    /// 延迟清空用户激活记录（确保初始化完成后再清空，避免与用户点击冲突）
    /// </summary>
    private System.Collections.IEnumerator ClearUserActivatedPanelsAfterDelay()
    {
        // 再等待一帧，确保所有初始化都已完成，包括用户可能在这一帧点击的按钮
        yield return null;
        userActivatedPanels.Clear();
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
        
        if (employeeHandbookPanel != null && employeeHandbookPanel.gameObject.activeSelf)
        {
            employeeHandbookPanel.Hide();
        }
        
        if (stationLevelPanel != null && stationLevelPanel.gameObject.activeSelf)
        {
            stationLevelPanel.Hide();
        }
        
        if (mailPanel != null && mailPanel.gameObject.activeSelf)
        {
            mailPanel.Hide();
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
        
        if (employeeHandbookPanel != null && employeeHandbookPanel != exceptPanel && employeeHandbookPanel.gameObject.activeSelf)
        {
            employeeHandbookPanel.Hide();
        }
        
        if (stationLevelPanel != null && stationLevelPanel != exceptPanel && stationLevelPanel.gameObject.activeSelf)
        {
            stationLevelPanel.Hide();
        }
        
        if (mailPanel != null && mailPanel != exceptPanel && mailPanel.gameObject.activeSelf)
        {
            mailPanel.Hide();
        }
    }
    
    /// <summary>
    /// 显示员工手册
    /// </summary>
    public void ShowEmployeeHandbook()
    {
        if (employeeHandbookPanel != null)
        {
            // 如果初始化还未完成，等待初始化完成后再显示（避免与DelayedHidePanels冲突）
            if (!isInitializationComplete)
            {
                StartCoroutine(ShowEmployeeHandbookAfterInit());
            }
            else
            {
                // 初始化已完成，直接显示
                HideAllPanelsExcept(employeeHandbookPanel);
                employeeHandbookPanel.Show();
            }
        }
        else
        {
            Debug.LogWarning("UIManager: EmployeeHandbookPanel未配置");
        }
    }
    
    /// <summary>
    /// 等待初始化完成后显示员工手册面板
    /// </summary>
    private System.Collections.IEnumerator ShowEmployeeHandbookAfterInit()
    {
        // 先立即显示面板，给用户即时反馈
        if (employeeHandbookPanel != null)
        {
            employeeHandbookPanel.Show();
            // 记录这是用户主动显示的面板，避免被DelayedHidePanels隐藏
            userActivatedPanels.Add(employeeHandbookPanel);
        }
        
        // 等待初始化完成（DelayedHidePanels执行完毕）
        while (!isInitializationComplete)
        {
            yield return null;
        }
        
        // 再等待一帧，确保DelayedHidePanels已经完全执行完毕
        yield return null;
        
        // 初始化完成后，再隐藏其他面板（面板已经在上面显示了）
        if (employeeHandbookPanel != null && employeeHandbookPanel.gameObject.activeInHierarchy)
        {
            HideAllPanelsExcept(employeeHandbookPanel);
        }
    }
    
    /// <summary>
    /// 显示基站等级
    /// </summary>
    public void ShowStationLevel()
    {
        if (stationLevelPanel != null)
        {
            // 如果初始化还未完成，等待初始化完成后再显示（避免与DelayedHidePanels冲突）
            if (!isInitializationComplete)
            {
                StartCoroutine(ShowStationLevelAfterInit());
            }
            else
            {
                // 初始化已完成，直接显示
                HideAllPanelsExcept(stationLevelPanel);
                stationLevelPanel.Show();
            }
        }
        else
        {
            Debug.LogWarning("UIManager: StationLevelPanel未配置");
        }
    }
    
    /// <summary>
    /// 等待初始化完成后显示基站等级面板
    /// </summary>
    private System.Collections.IEnumerator ShowStationLevelAfterInit()
    {
        // 先立即显示面板，给用户即时反馈
        if (stationLevelPanel != null)
        {
            stationLevelPanel.Show();
            // 记录这是用户主动显示的面板，避免被DelayedHidePanels隐藏
            userActivatedPanels.Add(stationLevelPanel);
        }
        
        // 等待初始化完成（DelayedHidePanels执行完毕）
        while (!isInitializationComplete)
        {
            yield return null;
        }
        
        // 再等待一帧，确保DelayedHidePanels已经完全执行完毕
        yield return null;
        
        // 初始化完成后，再隐藏其他面板（面板已经在上面显示了）
        if (stationLevelPanel != null && stationLevelPanel.gameObject.activeInHierarchy)
        {
            HideAllPanelsExcept(stationLevelPanel);
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
    /// 显示邮箱
    /// </summary>
    public void ShowMail()
    {
        if (mailPanel != null)
        {
            // 如果初始化还未完成，等待初始化完成后再显示（避免与DelayedHidePanels冲突）
            if (!isInitializationComplete)
            {
                StartCoroutine(ShowMailAfterInit());
            }
            else
            {
                // 初始化已完成，直接显示
                HideAllPanelsExcept(mailPanel);
                mailPanel.Show();
            }
        }
        else
        {
            Debug.LogWarning("UIManager: MailPanel未配置");
        }
    }
    
    /// <summary>
    /// 等待初始化完成后显示邮箱面板
    /// </summary>
    private System.Collections.IEnumerator ShowMailAfterInit()
    {
        // 先立即显示面板，给用户即时反馈
        if (mailPanel != null)
        {
            mailPanel.Show();
            // 记录这是用户主动显示的面板，避免被DelayedHidePanels隐藏
            userActivatedPanels.Add(mailPanel);
        }
        
        // 等待初始化完成（DelayedHidePanels执行完毕）
        while (!isInitializationComplete)
        {
            yield return null;
        }
        
        // 再等待一帧，确保DelayedHidePanels已经完全执行完毕
        yield return null;
        
        // 初始化完成后，再隐藏其他面板（面板已经在上面显示了）
        if (mailPanel != null && mailPanel.gameObject.activeInHierarchy)
        {
            HideAllPanelsExcept(mailPanel);
        }
    }
    
    /// <summary>
    /// 返回主界面（从其他面板返回，用于主界面场景）
    /// </summary>
    public void ReturnToMainHub()
    {
        HideAllPanels();
        
        // 如果mainHubController未配置，尝试自动查找
        if (mainHubController == null)
        {
            mainHubController = FindObjectOfType<MainHubController>();
            if (mainHubController != null)
            {
                Debug.Log("UIManager: 自动找到MainHubController引用");
            }
        }
        
        if (mainHubController != null)
        {
            mainHubController.ReturnToMainHub();
        }
        else
        {
            Debug.LogWarning("UIManager: MainHubController未配置，无法返回主界面。面板已隐藏。");
        }
    }
}
