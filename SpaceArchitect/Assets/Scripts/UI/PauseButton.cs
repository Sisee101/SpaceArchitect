using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 暂停按钮控制器
/// 处理暂停按钮的点击事件
/// </summary>
public class PauseButton : MonoBehaviour
{
    [Header("暂停面板引用")]
    [SerializeField] private PausePanel pausePanel;
    
    private Button button;
    
    void Start()
    {
        // 获取Button组件
        button = GetComponent<Button>();
        
        // 如果按钮存在，绑定点击事件
        if (button != null)
        {
            button.onClick.AddListener(OnPauseButtonClicked);
        }
        
        // 如果没有手动指定暂停面板，尝试自动查找
        if (pausePanel == null)
        {
            pausePanel = FindObjectOfType<PausePanel>();
            if (pausePanel == null)
            {
                Debug.LogWarning("PauseButton: 未找到PausePanel，请手动指定或在场景中添加PausePanel");
            }
        }
    }
    
    /// <summary>
    /// 暂停按钮点击事件
    /// </summary>
    private void OnPauseButtonClicked()
    {
        if (pausePanel != null)
        {
            pausePanel.TogglePause();
        }
        else
        {
            Debug.LogError("PauseButton: PausePanel未找到！");
        }
    }
}

