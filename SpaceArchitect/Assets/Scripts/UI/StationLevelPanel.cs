using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 基站等级面板控制器
/// 处理当前基站等级界面的显示和交互
/// </summary>
public class StationLevelPanel : MonoBehaviour
{
    [Header("UI引用")]
    [SerializeField] private Button backButton;
    
    [Header("内容显示（临时占位）")]
    [SerializeField] private Text levelText;
    [SerializeField] private Text descriptionText;
    
    private bool isInitialized = false;
    
    void Awake()
    {
        // 初始化文本内容（不依赖于GameObject的激活状态）
        if (levelText != null)
        {
            levelText.text = "当前基站等级: --";
        }
        
        if (descriptionText != null)
        {
            descriptionText.text = "基站等级功能开发中...\n\n这里将显示当前基站等级及相关信息";
        }
    }
    
    void OnEnable()
    {
        // 当面板被激活时，确保按钮事件已绑定
        if (!isInitialized)
        {
            InitializePanel();
            isInitialized = true;
        }
        
        // 每次显示时重新绑定（防止事件丢失）
        if (backButton != null)
        {
            backButton.onClick.RemoveListener(OnBackClicked); // 先移除，避免重复绑定
            backButton.onClick.AddListener(OnBackClicked);
        }
    }
    
    void Start()
    {
        // 如果面板在场景中默认是激活的，在这里初始化
        if (!isInitialized)
        {
            InitializePanel();
            isInitialized = true;
        }
        
        // 如果面板在场景中默认是激活的，在这里隐藏
        if (gameObject.activeSelf)
        {
            Hide();
        }
    }
    
    /// <summary>
    /// 初始化面板
    /// </summary>
    private void InitializePanel()
    {
        // 绑定返回按钮
        if (backButton != null)
        {
            backButton.onClick.RemoveListener(OnBackClicked); // 先移除，避免重复绑定
            backButton.onClick.AddListener(OnBackClicked);
        }
    }
    
    /// <summary>
    /// 显示基站等级面板
    /// </summary>
    public void Show()
    {
        if (!gameObject.activeSelf)
        {
            gameObject.SetActive(true);
        }
        UpdateLevelDisplay(); // 显示时更新等级信息
    }
    
    /// <summary>
    /// 隐藏基站等级面板
    /// </summary>
    public void Hide()
    {
        gameObject.SetActive(false);
    }
    
    /// <summary>
    /// 更新等级显示（供外部调用，用于更新基站等级信息）
    /// </summary>
    private void UpdateLevelDisplay()
    {
        // TODO: 从游戏数据中获取实际基站等级
        // 目前使用占位数据
        if (levelText != null)
        {
            levelText.text = "当前基站等级: 1"; // 占位数据
        }
    }
    
    /// <summary>
    /// 返回按钮点击事件
    /// </summary>
    private void OnBackClicked()
    {
        Debug.Log("返回主界面");
        if (UIManager.Instance != null)
        {
            UIManager.Instance.ReturnToMainHub();
        }
    }
}

