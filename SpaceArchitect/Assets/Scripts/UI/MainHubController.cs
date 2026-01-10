using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 主界面控制器
/// 管理主界面的按钮和面板切换
/// </summary>
public class MainHubController : MonoBehaviour
{
    [Header("底部按钮")]
    [SerializeField] private Button employeeHandbookButton;
    [SerializeField] private Button stationLevelButton;
    [SerializeField] private Button planetEncyclopediaButton;
    
    [Header("主界面面板（默认显示）")]
    [SerializeField] private GameObject mainHubPanel;
    
    void Start()
    {
        // 绑定按钮事件
        if (employeeHandbookButton != null)
        {
            employeeHandbookButton.onClick.AddListener(OnEmployeeHandbookClicked);
        }
        
        if (stationLevelButton != null)
        {
            stationLevelButton.onClick.AddListener(OnStationLevelClicked);
        }
        
        if (planetEncyclopediaButton != null)
        {
            planetEncyclopediaButton.onClick.AddListener(OnPlanetEncyclopediaClicked);
        }
        
        // 确保主界面面板默认显示
        if (mainHubPanel != null)
        {
            mainHubPanel.SetActive(true);
        }
    }
    
    /// <summary>
    /// 员工手册按钮点击事件
    /// </summary>
    private void OnEmployeeHandbookClicked()
    {
        Debug.Log("打开员工手册");
        if (UIManager.Instance != null)
        {
            UIManager.Instance.ShowEmployeeHandbook();
        }
        else
        {
            Debug.LogError("UIManager未找到！");
        }
    }
    
    /// <summary>
    /// 基站等级按钮点击事件
    /// </summary>
    private void OnStationLevelClicked()
    {
        Debug.Log("打开基站等级界面");
        if (UIManager.Instance != null)
        {
            UIManager.Instance.ShowStationLevel();
        }
        else
        {
            Debug.LogError("UIManager未找到！");
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
    /// 返回主界面（从子面板返回）
    /// 注意：这个方法只负责显示主界面面板，不应该再调用UIManager，避免无限递归
    /// </summary>
    public void ReturnToMainHub()
    {
        if (mainHubPanel != null)
        {
            mainHubPanel.SetActive(true);
        }
        // 移除了对 UIManager.Instance.ReturnToMainHub() 的调用，避免无限递归
        // UIManager.ReturnToMainHub() 会调用这个方法，不应该反向调用
    }
}

