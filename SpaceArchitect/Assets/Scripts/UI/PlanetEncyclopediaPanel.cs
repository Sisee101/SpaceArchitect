using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 行星图鉴面板控制器（基础版）
/// 处理行星图鉴的显示和交互
/// </summary>
public class PlanetEncyclopediaPanel : MonoBehaviour
{
    [Header("UI引用")]
    [SerializeField] private Button backButton;
    
    [Header("提示文本（临时）")]
    [SerializeField] private Text placeholderText;
    
    void Start()
    {
        // 绑定返回按钮
        if (backButton != null)
        {
            backButton.onClick.AddListener(OnBackClicked);
        }
        
        // 显示占位文本
        if (placeholderText != null)
        {
            placeholderText.text = "行星图鉴功能开发中...\n\n这里将显示所有已解锁的行星信息";
        }
    }
    
    /// <summary>
    /// 显示行星图鉴面板
    /// </summary>
    public void Show()
    {
        gameObject.SetActive(true);
    }
    
    /// <summary>
    /// 隐藏行星图鉴面板
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
        Debug.Log("返回主菜单");
        if (UIManager.Instance != null)
        {
            UIManager.Instance.ReturnToMainMenu();
        }
    }
}

