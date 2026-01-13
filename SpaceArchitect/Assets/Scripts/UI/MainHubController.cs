using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using TMPro;

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
    
    [Header("返回按钮")]
    [SerializeField] private Button returnToMenuButton;            // 返回主菜单按钮
    
    [Header("主界面面板（默认显示）")]
    [SerializeField] private GameObject mainHubPanel;
    
    [Header("结算界面")]
    [Tooltip("结算界面（订单详情面板），在主界面按下空格键后显示")]
    [SerializeField] private OrderDetailPanel settlementPanel;
    
    [Header("音效")]
    [SerializeField] private AudioSource audioSource;              // 音频源组件
    [SerializeField] private AudioClip buttonClickSound;            // 按钮点击音效
    [SerializeField] private float clickSoundDelay = 0.15f;        // 点击音效播放后的延迟时间（秒）
    
    [Header("金钱显示")]
    [Tooltip("显示金钱数值的Text组件（Unity UI Text）")]
    [SerializeField] private Text moneyText;                        // Unity UI Text组件（可选）
    
    [Tooltip("显示金钱数值的TextMeshPro组件（TextMeshPro - Text (UI)）")]
    [SerializeField] private TextMeshProUGUI moneyTextMeshPro;     // TextMeshPro组件（可选）
    
    [Tooltip("是否启用实时更新（每帧检测金钱变化）")]
    [SerializeField] private bool enableRealTimeUpdate = true;     // 是否实时更新
    
    // 上一次的金钱值，用于检测变化
    private int lastMoneyValue = -1;
    
    void Start()
    {
        // 初始化音频源
        InitializeAudioSource();
        
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
        
        // 绑定返回主菜单按钮
        if (returnToMenuButton != null)
        {
            returnToMenuButton.onClick.AddListener(OnReturnToMenuClicked);
        }
        
        // 确保主界面面板默认显示
        if (mainHubPanel != null)
        {
            mainHubPanel.SetActive(true);
        }
        
        // 初始化金钱显示
        InitializeMoneyDisplay();
        
        // 结算界面会在自己的Start()中自动隐藏，这里不需要手动调用
    }
    
    void Update()
    {
        // 监听空格键，显示结算界面
        if (Input.GetKeyDown(KeyCode.Space))
        {
            if (settlementPanel == null)
            {
                Debug.LogWarning("MainHubController: settlementPanel未配置！请在Inspector中配置Settlement Panel引用。");
                return;
            }
            
            // 如果结算界面未显示，显示结算界面
            if (!settlementPanel.IsShowing())
            {
                Debug.Log("MainHubController: 按下空格键，显示结算界面");
                settlementPanel.Show();
            }
            else
            {
                Debug.Log("MainHubController: 结算界面已显示，空格键不处理");
            }
        }
        
        // 监听ESC键，关闭结算界面
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (settlementPanel != null && settlementPanel.IsShowing())
            {
                Debug.Log("MainHubController: 按下ESC键，关闭结算界面");
                settlementPanel.Hide();
            }
        }
        
        // 实时更新金钱显示（如果启用）
        if (enableRealTimeUpdate)
        {
            UpdateMoneyDisplayIfChanged();
        }
    }
    
    /// <summary>
    /// 初始化音频源
    /// </summary>
    private void InitializeAudioSource()
    {
        // 如果未手动指定 AudioSource，尝试自动获取
        if (audioSource == null)
        {
            audioSource = GetComponent<AudioSource>();
            // 如果还是没有，自动添加一个
            if (audioSource == null)
            {
                audioSource = gameObject.AddComponent<AudioSource>();
            }
        }
    }
    
    /// <summary>
    /// 播放按钮点击音效
    /// </summary>
    private void PlayButtonClickSound()
    {
        if (audioSource != null && buttonClickSound != null)
        {
            audioSource.PlayOneShot(buttonClickSound);
        }
    }
    
    /// <summary>
    /// 员工手册按钮点击事件
    /// </summary>
    private void OnEmployeeHandbookClicked()
    {
        // 播放按钮点击音效
        PlayButtonClickSound();
        
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
        // 播放按钮点击音效
        PlayButtonClickSound();
        
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
        // 播放按钮点击音效
        PlayButtonClickSound();
        
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
    /// 返回主菜单按钮点击事件
    /// </summary>
    private void OnReturnToMenuClicked()
    {
        // 播放按钮点击音效并延迟执行返回操作，确保音效播放完成
        StartCoroutine(PlayClickSoundAndReturnToMenu());
    }
    
    /// <summary>
    /// 播放点击音效并延迟返回主菜单（协程）
    /// </summary>
    private IEnumerator PlayClickSoundAndReturnToMenu()
    {
        // 播放按钮点击音效
        PlayButtonClickSound();
        
        // 等待音效播放完成
        yield return new WaitForSeconds(clickSoundDelay);
        
        Debug.Log("返回主菜单");
        
        // 恢复时间，避免场景切换时时间异常
        Time.timeScale = 1f;
        
        // 加载主菜单场景
        if (SceneTransitionManager.Instance != null)
        {
            SceneTransitionManager.Instance.LoadMainMenuScene();
        }
        else
        {
            Debug.LogError("MainHubController: SceneTransitionManager未找到！");
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
        
        // 返回主界面时更新金钱显示
        UpdateMoneyDisplay();
    }
    
    /// <summary>
    /// 初始化金钱显示
    /// </summary>
    private void InitializeMoneyDisplay()
    {
        // 如果两个文本组件都未配置，尝试自动查找
        if (moneyText == null && moneyTextMeshPro == null)
        {
            // 尝试查找子对象中的Text组件
            moneyText = GetComponentInChildren<Text>();
            moneyTextMeshPro = GetComponentInChildren<TextMeshProUGUI>();
        }
        
        // 初始化显示
        UpdateMoneyDisplay();
        lastMoneyValue = MoneyManager.money;
    }
    
    /// <summary>
    /// 更新金钱显示（如果金钱值发生变化）
    /// </summary>
    private void UpdateMoneyDisplayIfChanged()
    {
        if (MoneyManager.money != lastMoneyValue)
        {
            UpdateMoneyDisplay();
            lastMoneyValue = MoneyManager.money;
        }
    }
    
    /// <summary>
    /// 更新金钱显示文本
    /// </summary>
    public void UpdateMoneyDisplay()
    {
        string moneyString = MoneyManager.money.ToString();
        
        // 更新Unity UI Text
        if (moneyText != null)
        {
            moneyText.text = moneyString;
        }
        
        // 更新TextMeshPro
        if (moneyTextMeshPro != null)
        {
            moneyTextMeshPro.text = moneyString;
        }
    }
}

