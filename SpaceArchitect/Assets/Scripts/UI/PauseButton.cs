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
        
        // 初始化时查找PausePanel
        FindPausePanel();
    }
    
    /// <summary>
    /// 查找PausePanel（用于场景重新加载后重新查找）
    /// </summary>
    private void FindPausePanel()
    {
        // 如果没有手动指定暂停面板，尝试自动查找
        if (pausePanel == null)
        {
            // 查找所有PausePanel（包括未激活的）
            PausePanel[] allPausePanels = Resources.FindObjectsOfTypeAll<PausePanel>();
            
            // 优先查找场景中的PausePanel（不是预制体的）
            foreach (PausePanel panel in allPausePanels)
            {
                // 检查是否是场景中的对象（不是预制体资源）
                if (panel.gameObject.scene.isLoaded)
                {
                    pausePanel = panel;
                    Debug.Log("PauseButton: 成功找到PausePanel（场景对象）");
                    return;
                }
            }
            
            // 如果没有找到场景对象，尝试普通的FindObjectOfType（只查找激活的）
            pausePanel = FindObjectOfType<PausePanel>();
            if (pausePanel == null)
            {
                Debug.LogWarning("PauseButton: 未找到PausePanel，请手动指定或在场景中添加PausePanel");
            }
            else
            {
                Debug.Log("PauseButton: 成功找到PausePanel");
            }
        }
    }
    
    /// <summary>
    /// 暂停按钮点击事件
    /// </summary>
    private void OnPauseButtonClicked()
    {
        // 如果pausePanel为null（可能因为场景重新加载导致引用丢失），再次尝试查找
        if (pausePanel == null)
        {
            FindPausePanel();
        }
        
        if (pausePanel != null)
        {
            pausePanel.TogglePause();
        }
        else
        {
            Debug.LogError("PauseButton: PausePanel未找到！请确保场景中存在PausePanel。");
        }
    }
}



