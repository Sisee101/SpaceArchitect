using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 暂停面板控制器
/// 处理游戏暂停时的UI显示和交互
/// </summary>
public class PausePanel : MonoBehaviour
{
    [Header("按钮引用")]
    [SerializeField] private Button resumeButton;
    [SerializeField] private Button mainMenuButton;
    [SerializeField] private Button restartButton;
    
    private bool isPaused = false;
    
    void Start()
    {
        // 绑定按钮事件
        if (resumeButton != null)
        {
            resumeButton.onClick.AddListener(OnResumeClicked);
        }
        
        if (mainMenuButton != null)
        {
            mainMenuButton.onClick.AddListener(OnMainMenuClicked);
        }
        
        if (restartButton != null)
        {
            restartButton.onClick.AddListener(OnRestartClicked);
        }
        
        // 默认隐藏暂停面板
        Hide();
    }
    
    void Update()
    {
        // 监听ESC键（可选功能）
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (isPaused)
            {
                ResumeGame();
            }
            else
            {
                PauseGame();
            }
        }
    }
    
    /// <summary>
    /// 显示暂停面板
    /// </summary>
    public void Show()
    {
        gameObject.SetActive(true);
    }
    
    /// <summary>
    /// 隐藏暂停面板
    /// </summary>
    public void Hide()
    {
        gameObject.SetActive(false);
    }
    
    /// <summary>
    /// 暂停游戏
    /// </summary>
    public void PauseGame()
    {
        if (!isPaused)
        {
            isPaused = true;
            Time.timeScale = 0f; // 暂停游戏时间
            Show();
            
            // 更新游戏状态
            if (SceneTransitionManager.Instance != null)
            {
                SceneTransitionManager.Instance.SetGameState(SceneTransitionManager.GameState.Paused);
            }
            
            Debug.Log("游戏已暂停");
        }
    }
    
    /// <summary>
    /// 恢复游戏
    /// </summary>
    public void ResumeGame()
    {
        if (isPaused)
        {
            isPaused = false;
            Time.timeScale = 1f; // 恢复游戏时间
            Hide();
            
            // 更新游戏状态
            if (SceneTransitionManager.Instance != null)
            {
                SceneTransitionManager.Instance.SetGameState(SceneTransitionManager.GameState.Playing);
            }
            
            Debug.Log("游戏已恢复");
        }
    }
    
    /// <summary>
    /// 切换暂停状态
    /// </summary>
    public void TogglePause()
    {
        if (isPaused)
        {
            ResumeGame();
        }
        else
        {
            PauseGame();
        }
    }
    
    /// <summary>
    /// 继续游戏按钮点击事件
    /// </summary>
    private void OnResumeClicked()
    {
        ResumeGame();
    }
    
    /// <summary>
    /// 回到主页面按钮点击事件
    /// </summary>
    private void OnMainMenuClicked()
    {
        // 恢复时间，避免场景切换时时间仍为0
        Time.timeScale = 1f;
        
        // 加载主菜单场景
        if (SceneTransitionManager.Instance != null)
        {
            SceneTransitionManager.Instance.LoadMainMenuScene();
        }
        else
        {
            Debug.LogError("SceneTransitionManager未找到！");
        }
    }
    
    /// <summary>
    /// 重玩游戏按钮点击事件
    /// </summary>
    private void OnRestartClicked()
    {
        // 恢复时间，避免场景切换时时间仍为0
        Time.timeScale = 1f;
        
        // 重新加载当前场景
        if (SceneTransitionManager.Instance != null)
        {
            SceneTransitionManager.Instance.RestartGame();
        }
        else
        {
            Debug.LogError("SceneTransitionManager未找到！");
        }
    }
    
    /// <summary>
    /// 获取当前暂停状态
    /// </summary>
    public bool IsPaused()
    {
        return isPaused;
    }
}

