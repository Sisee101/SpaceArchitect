using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Sphere信息面板控制器
/// 管理订单信息面板的显示、隐藏和按钮事件
/// </summary>
public class SphereInfoPanel : MonoBehaviour
{
    [Header("UI引用")]
    [SerializeField] private Image panelImage;      // 显示订单图片的Image组件
    [SerializeField] private Button closeButton;    // 关闭按钮
    [SerializeField] private Button jumpButton;     // 跳转场景按钮（前往配送）
    
    [Header("调试")]
    [SerializeField] private bool enableDebugLog = true; // 是否启用调试日志
    
    private string currentTargetSceneName; // 当前面板的目标场景名称
    
    void Start()
    {
        // 绑定按钮事件
        if (closeButton != null)
        {
            closeButton.onClick.AddListener(OnCloseClicked);
        }
        else
        {
            Debug.LogError("SphereInfoPanel: closeButton未配置！");
        }
        
        if (jumpButton != null)
        {
            jumpButton.onClick.AddListener(OnJumpClicked);
        }
        else
        {
            Debug.LogError("SphereInfoPanel: jumpButton未配置！");
        }
        
        if (panelImage == null)
        {
            Debug.LogError("SphereInfoPanel: panelImage未配置！");
        }
        
        // 默认隐藏面板
        gameObject.SetActive(false);
    }
    
    /// <summary>
    /// 显示面板
    /// </summary>
    /// <param name="image">订单图片</param>
    /// <param name="sceneName">目标场景名称</param>
    public void Show(Sprite image, string sceneName)
    {
        if (panelImage == null)
        {
            Debug.LogError("SphereInfoPanel: panelImage未配置，无法显示面板！");
            return;
        }
        
        if (image == null)
        {
            Debug.LogWarning("SphereInfoPanel: 订单图片为空！");
        }
        
        if (string.IsNullOrEmpty(sceneName))
        {
            Debug.LogWarning("SphereInfoPanel: 目标场景名称为空！");
        }
        
        // 设置图片
        panelImage.sprite = image;
        
        // 保存目标场景名称
        currentTargetSceneName = sceneName;
        
        // 显示面板
        gameObject.SetActive(true);
        
        if (enableDebugLog)
        {
            Debug.Log($"SphereInfoPanel: 显示面板，场景: {sceneName}");
        }
    }
    
    /// <summary>
    /// 隐藏面板
    /// </summary>
    public void Hide()
    {
        gameObject.SetActive(false);
        
        if (enableDebugLog)
        {
            Debug.Log("SphereInfoPanel: 隐藏面板");
        }
    }
    
    /// <summary>
    /// 关闭按钮点击事件
    /// </summary>
    private void OnCloseClicked()
    {
        Hide();
    }
    
    /// <summary>
    /// 跳转场景按钮点击事件（前往配送）
    /// </summary>
    private void OnJumpClicked()
    {
        if (string.IsNullOrEmpty(currentTargetSceneName))
        {
            Debug.LogError("SphereInfoPanel: 目标场景名称为空，无法跳转！");
            return;
        }
        
        if (enableDebugLog)
        {
            Debug.Log($"SphereInfoPanel: 跳转到场景: {currentTargetSceneName}");
        }
        
        // 使用SceneTransitionManager加载场景
        if (SceneTransitionManager.Instance != null)
        {
            SceneTransitionManager.Instance.LoadSceneByName(currentTargetSceneName);
        }
        else
        {
            Debug.LogError("SphereInfoPanel: SceneTransitionManager未找到！");
        }
    }
    
    /// <summary>
    /// 检查面板是否显示
    /// </summary>
    public bool IsVisible()
    {
        return gameObject.activeSelf;
    }
}
