using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// 场景切换管理器（单例）
/// 负责场景切换、全局状态管理等核心功能
/// 为了避免与可能的MainMap GameManager冲突，使用此名称
/// </summary>
public class SceneTransitionManager : MonoBehaviour
{
    private static SceneTransitionManager _instance;
    
    /// <summary>
    /// 获取SceneTransitionManager单例
    /// </summary>
    public static SceneTransitionManager Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindObjectOfType<SceneTransitionManager>();
                
                if (_instance == null)
                {
                    GameObject go = new GameObject("SceneTransitionManager");
                    _instance = go.AddComponent<SceneTransitionManager>();
                    DontDestroyOnLoad(go);
                }
            }
            return _instance;
        }
    }
    
    /// <summary>
    /// 游戏状态枚举
    /// </summary>
    public enum GameState
    {
        MainMenu,    // 主菜单（开始页面）
        MainHub,     // 主界面
        Playing,     // 游戏中
        Paused       // 暂停
    }
    
    [SerializeField] private GameState currentState = GameState.MainMenu;
    
    // 场景名称常量
    private const string MAIN_MENU_SCENE = "00_MainMenu";
    private const string MAIN_HUB_SCENE = "01_MainHub";
    private const string GAMEPLAY_SCENE = "scene02";
    
    void Awake()
    {
        // 确保单例
        if (_instance == null)
        {
            _instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else if (_instance != this)
        {
            Debug.LogWarning("检测到多个SceneTransitionManager实例，销毁重复的实例");
            Destroy(gameObject);
            return;
        }
    }
    
    void Start()
    {
        // 根据当前场景设置状态
        string currentSceneName = SceneManager.GetActiveScene().name;
        if (currentSceneName == MAIN_MENU_SCENE)
        {
            currentState = GameState.MainMenu;
        }
        else if (currentSceneName == MAIN_HUB_SCENE)
        {
            currentState = GameState.MainHub;
        }
        else if (currentSceneName == GAMEPLAY_SCENE)
        {
            currentState = GameState.Playing;
        }
    }
    
    /// <summary>
    /// 加载主菜单场景
    /// </summary>
    public void LoadMainMenuScene()
    {
        currentState = GameState.MainMenu;
        Time.timeScale = 1f; // 确保时间正常
        SceneManager.LoadScene(MAIN_MENU_SCENE);
    }
    
    /// <summary>
    /// 加载主界面场景
    /// </summary>
    public void LoadMainHubScene()
    {
        currentState = GameState.MainHub;
        Time.timeScale = 1f; // 确保时间正常
        SceneManager.LoadScene(MAIN_HUB_SCENE);
    }
    
    /// <summary>
    /// 加载游戏场景（Scene02）
    /// </summary>
    public void LoadGameScene()
    {
        currentState = GameState.Playing;
        Time.timeScale = 1f; // 确保时间正常
        SceneManager.LoadScene(GAMEPLAY_SCENE);
    }
    
    /// <summary>
    /// 重新开始当前游戏场景
    /// </summary>
    public void RestartGame()
    {
        currentState = GameState.Playing;
        Time.timeScale = 1f; // 确保时间正常
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }
    
    /// <summary>
    /// 退出游戏
    /// </summary>
    public void QuitGame()
    {
        #if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
        #else
        Application.Quit();
        #endif
    }
    
    /// <summary>
    /// 获取当前游戏状态
    /// </summary>
    public GameState GetCurrentState()
    {
        return currentState;
    }
    
    /// <summary>
    /// 设置游戏状态
    /// </summary>
    public void SetGameState(GameState newState)
    {
        currentState = newState;
    }
}

