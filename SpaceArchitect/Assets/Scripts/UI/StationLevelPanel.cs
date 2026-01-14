using UnityEngine;
using UnityEngine.UI;
using System.Collections;

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
        
        // 面板激活时，延迟更新显示（确保UI元素已准备好）
        // 使用协程延迟一帧，确保所有UI元素都已完全初始化
        if (this != null && gameObject.activeInHierarchy)
        {
            StartCoroutine(DelayedUpdateOnEnable());
        }
    }
    
    /// <summary>
    /// 延迟更新（在OnEnable中调用，确保UI元素已准备好）
    /// </summary>
    private System.Collections.IEnumerator DelayedUpdateOnEnable()
    {
        // 等待一帧，确保所有UI元素都已准备好
        yield return null;
        
        // 再次检查，确保面板仍然激活且对象仍然存在
        if (this != null && gameObject != null && gameObject.activeInHierarchy && levelText != null)
        {
            UpdateLevelDisplay();
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
        // 总是确保初始化完成
        if (!isInitialized)
        {
            InitializePanel();
            isInitialized = true;
        }
        else
        {
            // 即使已经初始化，也确保按钮事件已绑定（防止事件丢失）
            if (backButton != null)
            {
                backButton.onClick.RemoveListener(OnBackClicked);
                backButton.onClick.AddListener(OnBackClicked);
            }
        }
        
        // 激活面板（与员工手册保持完全一致的逻辑）
        if (!gameObject.activeSelf)
        {
            gameObject.SetActive(true);
            // OnEnable会自动调用DelayedUpdateOnEnable()来更新显示
        }
        else
        {
            // 如果面板已经是激活的，也触发更新（使用协程确保UI元素已准备好）
            if (this != null && gameObject.activeInHierarchy)
            {
                StartCoroutine(DelayedUpdateOnEnable());
            }
        }
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
        
        // 延迟关闭面板，确保音效播放完成
        StartCoroutine(DelayedHide());
    }
    
    /// <summary>
    /// 延迟关闭面板（确保音效播放完成）
    /// </summary>
    private IEnumerator DelayedHide()
    {
        // 等待一小段时间，确保音效开始播放
        // 0.1秒足够音效开始播放，但不会让用户感觉延迟
        yield return new WaitForSeconds(0.1f);
        
        // 关闭面板
        if (UIManager.Instance != null)
        {
            UIManager.Instance.ReturnToMainHub();
        }
    }
}

