using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 显示结局图片面板按钮脚本
/// 可以附加到任意按钮上，点击后显示结局图片面板
/// 这是一个低耦合的按钮脚本，保持独立性
/// </summary>
public class ShowEndingImageButton : MonoBehaviour
{
    [Header("面板引用")]
    [Tooltip("结局图片面板的引用")]
    [SerializeField] private EndingImagePanel endingImagePanel;
    
    [Header("调试")]
    [Tooltip("是否启用调试日志")]
    [SerializeField] private bool enableDebugLog = true;
    
    private Button button;
    
    void Start()
    {
        // 获取Button组件
        button = GetComponent<Button>();
        
        // 如果按钮存在，绑定点击事件
        if (button != null)
        {
            button.onClick.AddListener(OnButtonClicked);
        }
        else
        {
            Debug.LogWarning("ShowEndingImageButton: 当前GameObject没有Button组件！");
        }
        
        // 如果没有手动指定面板，尝试自动查找
        if (endingImagePanel == null)
        {
            FindEndingImagePanel();
        }
        
        // 验证面板引用
        if (endingImagePanel == null)
        {
            Debug.LogError("ShowEndingImageButton: endingImagePanel 未分配且无法自动找到！请在Inspector中分配面板引用，或确保场景中存在EndingImagePanel。");
        }
    }
    
    /// <summary>
    /// 查找场景中的EndingImagePanel（用于自动查找）
    /// </summary>
    private void FindEndingImagePanel()
    {
        // 查找场景中的所有EndingImagePanel（包括未激活的）
        EndingImagePanel[] allPanels = Resources.FindObjectsOfTypeAll<EndingImagePanel>();
        
        // 优先查找场景中的面板（不是预制体资源）
        foreach (EndingImagePanel panel in allPanels)
        {
            // 检查是否是场景中的对象（不是预制体资源）
            if (panel.gameObject.scene.isLoaded)
            {
                endingImagePanel = panel;
                if (enableDebugLog)
                {
                    Debug.Log($"ShowEndingImageButton: 自动找到EndingImagePanel（来自场景: {panel.gameObject.scene.name}）");
                }
                return;
            }
        }
        
        // 如果没有找到场景对象，尝试普通的FindObjectOfType（只查找激活的）
        EndingImagePanel foundPanel = FindObjectOfType<EndingImagePanel>();
        if (foundPanel != null)
        {
            endingImagePanel = foundPanel;
            if (enableDebugLog)
            {
                Debug.Log("ShowEndingImageButton: 自动找到EndingImagePanel（激活状态）");
            }
        }
    }
    
    /// <summary>
    /// 按钮点击事件
    /// </summary>
    private void OnButtonClicked()
    {
        if (endingImagePanel != null)
        {
            endingImagePanel.Show();
            
            if (enableDebugLog)
            {
                Debug.Log("ShowEndingImageButton: 点击按钮，显示结局图片面板");
            }
        }
        else
        {
            Debug.LogError("ShowEndingImageButton: endingImagePanel 为null，无法显示面板！");
        }
    }
    
    /// <summary>
    /// 手动设置面板引用（可在运行时调用）
    /// </summary>
    /// <param name="panel">结局图片面板</param>
    public void SetEndingImagePanel(EndingImagePanel panel)
    {
        endingImagePanel = panel;
    }
}
