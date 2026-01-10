using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 员工手册面板控制器
/// 处理员工手册的显示和交互
/// </summary>
public class EmployeeHandbookPanel : MonoBehaviour
{
    [Header("UI引用")]
    [SerializeField] private Button backButton;
    
    [Header("内容显示（临时占位）")]
    [SerializeField] private Text contentText;
    
    private bool isInitialized = false;
    
    void Awake()
    {
        // 初始化文本内容（不依赖于GameObject的激活状态）
        if (contentText != null)
        {
            contentText.text = "员工手册功能开发中...\n\n这里将显示员工手册的相关信息";
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
    /// 显示员工手册面板
    /// </summary>
    public void Show()
    {
        // 如果面板还没有初始化，先初始化（无论是否激活）
        if (!isInitialized)
        {
            InitializePanel();
            isInitialized = true;
        }
        
        // 激活面板（如果已经是激活的，这不会触发OnEnable，但我们已经在上面初始化了）
        if (!gameObject.activeSelf)
        {
            gameObject.SetActive(true);
        }
        else
        {
            // 如果面板已经是激活的，确保按钮事件已绑定
            if (backButton != null)
            {
                backButton.onClick.RemoveListener(OnBackClicked);
                backButton.onClick.AddListener(OnBackClicked);
            }
        }
    }
    
    /// <summary>
    /// 隐藏员工手册面板
    /// </summary>
    public void Hide()
    {
        gameObject.SetActive(false);
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

