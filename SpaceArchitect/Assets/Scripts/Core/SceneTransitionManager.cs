using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections.Generic;

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
    
    // 当前加载的游戏场景名称（Additive模式）
    private static string currentLoadedGameScene = null;
    
    // MainHub场景的Canvas引用字典（场景名称 -> Canvas引用）
    private static Dictionary<string, Canvas> mainHubCanvasMap = new Dictionary<string, Canvas>();
    
    // 当前隐藏的MainHub场景名称（用于恢复）
    private static string currentMainHubSceneName = null;
    
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
    
    /// <summary>
    /// 根据场景名称加载场景（通用方法）
    /// </summary>
    /// <param name="sceneName">场景名称（必须在Build Settings中）</param>
    public void LoadSceneByName(string sceneName)
    {
        if (string.IsNullOrEmpty(sceneName))
        {
            Debug.LogError("SceneTransitionManager: 场景名称为空！");
            return;
        }
        
        // 检查场景是否存在
        if (!SceneExists(sceneName))
        {
            Debug.LogError($"SceneTransitionManager: 场景 {sceneName} 不存在或未添加到Build Settings！");
            return;
        }
        
        Time.timeScale = 1f; // 确保时间正常
        
        // 根据场景名称更新状态（如果匹配已知场景）
        if (sceneName == MAIN_MENU_SCENE)
        {
            currentState = GameState.MainMenu;
        }
        else if (sceneName == MAIN_HUB_SCENE)
        {
            currentState = GameState.MainHub;
        }
        else if (sceneName == GAMEPLAY_SCENE)
        {
            currentState = GameState.Playing;
        }
        // 其他场景保持当前状态或设置为Playing
        
        Debug.Log($"SceneTransitionManager: 加载场景 {sceneName}");
        SceneManager.LoadScene(sceneName);
    }
    
    /// <summary>
    /// 检查场景是否存在
    /// </summary>
    private bool SceneExists(string sceneName)
    {
        for (int i = 0; i < SceneManager.sceneCountInBuildSettings; i++)
        {
            string scenePath = SceneUtility.GetScenePathByBuildIndex(i);
            string sceneNameInBuild = System.IO.Path.GetFileNameWithoutExtension(scenePath);
            
            if (sceneNameInBuild == sceneName)
            {
                return true;
            }
        }
        return false;
    }
    
    /// <summary>
    /// 使用Additive模式加载场景（覆盖在MainHub上方）
    /// </summary>
    /// <param name="sceneName">场景名称（必须在Build Settings中）</param>
    public void LoadSceneAdditive(string sceneName)
    {
        if (string.IsNullOrEmpty(sceneName))
        {
            Debug.LogError("SceneTransitionManager: 场景名称为空！");
            return;
        }
        
        // 检查场景是否存在
        if (!SceneExists(sceneName))
        {
            Debug.LogError($"SceneTransitionManager: 场景 {sceneName} 不存在或未添加到Build Settings！");
            return;
        }
        
        // 检查场景是否已经加载（避免重复加载）
        if (IsSceneLoaded(sceneName))
        {
            Debug.LogWarning($"SceneTransitionManager: 场景 {sceneName} 已经加载，跳过重复加载");
            return;
        }
        
        Time.timeScale = 1f; // 确保时间正常
        
        // 更新游戏状态
        currentState = GameState.Playing;
        
        // 隐藏MainHub场景的Canvas，避免游戏场景中看到MainHub的UI
        HideMainHubCanvas();
        
        // 使用Additive模式异步加载场景
        SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Additive);
        
        // 保存当前加载的游戏场景名称
        currentLoadedGameScene = sceneName;
        
        Debug.Log($"SceneTransitionManager: 使用Additive模式加载场景 {sceneName}，覆盖在MainHub上方，MainHub UI已隐藏");
    }
    
    /// <summary>
    /// 卸载当前加载的游戏场景（返回MainHub）
    /// </summary>
    public void UnloadGameScene()
    {
        if (string.IsNullOrEmpty(currentLoadedGameScene))
        {
            Debug.LogWarning("SceneTransitionManager: 没有已加载的游戏场景，无法卸载");
            return;
        }
        
        // 检查场景是否真的已加载
        if (!IsSceneLoaded(currentLoadedGameScene))
        {
            Debug.LogWarning($"SceneTransitionManager: 场景 {currentLoadedGameScene} 未加载，清空引用");
            currentLoadedGameScene = null;
            return;
        }
        
        // 恢复时间缩放
        Time.timeScale = 1f;
        
        // 更新游戏状态为MainHub
        currentState = GameState.MainHub;
        
        // 异步卸载场景
        SceneManager.UnloadSceneAsync(currentLoadedGameScene);
        
        string sceneToUnload = currentLoadedGameScene;
        currentLoadedGameScene = null;
        
        // 恢复MainHub场景的Canvas显示
        ShowMainHubCanvas();
        
        Debug.Log($"SceneTransitionManager: 已卸载游戏场景 {sceneToUnload}，返回MainHub，MainHub UI已恢复显示");
    }
    
    /// <summary>
    /// 检查指定场景是否已加载
    /// </summary>
    /// <param name="sceneName">场景名称</param>
    /// <returns>是否已加载</returns>
    private bool IsSceneLoaded(string sceneName)
    {
        for (int i = 0; i < SceneManager.sceneCount; i++)
        {
            Scene scene = SceneManager.GetSceneAt(i);
            if (scene.name == sceneName && scene.isLoaded)
            {
                return true;
            }
        }
        return false;
    }
    
    /// <summary>
    /// 检查是否有游戏场景已加载
    /// </summary>
    /// <returns>是否有游戏场景已加载</returns>
    public bool IsGameSceneLoaded()
    {
        return !string.IsNullOrEmpty(currentLoadedGameScene);
    }
    
    /// <summary>
    /// 获取当前加载的游戏场景名称
    /// </summary>
    /// <returns>场景名称，如果没有则返回null</returns>
    public string GetCurrentLoadedGameScene()
    {
        return currentLoadedGameScene;
    }
    
    /// <summary>
    /// 注册MainHub场景的Canvas引用
    /// </summary>
    /// <param name="sceneName">场景名称（如"01_MainHub"）</param>
    /// <param name="canvas">Canvas引用</param>
    public static void RegisterMainHubCanvas(string sceneName, Canvas canvas)
    {
        if (string.IsNullOrEmpty(sceneName))
        {
            Debug.LogWarning("SceneTransitionManager: 场景名称为空，无法注册Canvas");
            return;
        }
        
        if (canvas == null)
        {
            Debug.LogWarning($"SceneTransitionManager: Canvas引用为空，无法注册场景 {sceneName} 的Canvas");
            return;
        }
        
        // 注册或更新Canvas引用
        mainHubCanvasMap[sceneName] = canvas;
        Debug.Log($"SceneTransitionManager: 已注册场景 {sceneName} 的Canvas: {canvas.name}");
    }
    
    /// <summary>
    /// 注销MainHub场景的Canvas引用
    /// </summary>
    /// <param name="sceneName">场景名称</param>
    public static void UnregisterMainHubCanvas(string sceneName)
    {
        if (string.IsNullOrEmpty(sceneName))
        {
            return;
        }
        
        if (mainHubCanvasMap.ContainsKey(sceneName))
        {
            mainHubCanvasMap.Remove(sceneName);
            Debug.Log($"SceneTransitionManager: 已注销场景 {sceneName} 的Canvas引用");
        }
    }
    
    /// <summary>
    /// 隐藏MainHub场景的Canvas
    /// </summary>
    private void HideMainHubCanvas()
    {
        // 获取当前激活的MainHub场景名称
        string currentSceneName = SceneManager.GetActiveScene().name;
        
        // 检查是否是MainHub场景（01_MainHub, 02_MainHub, 03_MainHub, 04_MainHub, 05_MainHub）
        bool isMainHubScene = currentSceneName.StartsWith("0") && currentSceneName.Contains("_MainHub");
        
        if (!isMainHubScene)
        {
            // 如果不是MainHub场景，尝试从已加载的场景中查找MainHub场景
            for (int i = 0; i < SceneManager.sceneCount; i++)
            {
                Scene scene = SceneManager.GetSceneAt(i);
                if (scene.isLoaded && scene.name.StartsWith("0") && scene.name.Contains("_MainHub"))
                {
                    currentSceneName = scene.name;
                    isMainHubScene = true;
                    break;
                }
            }
        }
        
        if (!isMainHubScene)
        {
            Debug.LogWarning($"SceneTransitionManager: 当前场景 {currentSceneName} 不是MainHub场景，无法隐藏Canvas");
            return;
        }
        
        // 从字典中查找对应的Canvas
        Canvas targetCanvas = null;
        if (mainHubCanvasMap.ContainsKey(currentSceneName))
        {
            targetCanvas = mainHubCanvasMap[currentSceneName];
            
            // 验证Canvas引用是否有效
            if (targetCanvas == null)
            {
                Debug.LogWarning($"SceneTransitionManager: 场景 {currentSceneName} 的Canvas引用已失效，从字典中移除");
                mainHubCanvasMap.Remove(currentSceneName);
            }
        }
        
        // 如果未找到，输出警告
        if (targetCanvas == null)
        {
            Debug.LogWarning($"SceneTransitionManager: 未找到场景 {currentSceneName} 的Canvas引用。请确保该场景的OrderDetailPanel已配置Canvas引用。");
            return;
        }
        
        // 隐藏Canvas并记录当前MainHub场景名称
        targetCanvas.gameObject.SetActive(false);
        currentMainHubSceneName = currentSceneName;
        
        Debug.Log($"SceneTransitionManager: 已隐藏场景 {currentSceneName} 的Canvas: {targetCanvas.name}");
    }
    
    /// <summary>
    /// 恢复MainHub场景的Canvas显示
    /// </summary>
    private void ShowMainHubCanvas()
    {
        // 如果没有记录当前MainHub场景名称，尝试从已加载的场景中查找
        if (string.IsNullOrEmpty(currentMainHubSceneName))
        {
            for (int i = 0; i < SceneManager.sceneCount; i++)
            {
                Scene scene = SceneManager.GetSceneAt(i);
                if (scene.isLoaded && scene.name.StartsWith("0") && scene.name.Contains("_MainHub"))
                {
                    currentMainHubSceneName = scene.name;
                    break;
                }
            }
        }
        
        if (string.IsNullOrEmpty(currentMainHubSceneName))
        {
            Debug.LogWarning("SceneTransitionManager: 未找到MainHub场景名称，无法恢复Canvas显示");
            return;
        }
        
        // 从字典中查找对应的Canvas
        Canvas targetCanvas = null;
        if (mainHubCanvasMap.ContainsKey(currentMainHubSceneName))
        {
            targetCanvas = mainHubCanvasMap[currentMainHubSceneName];
            
            // 验证Canvas引用是否有效
            if (targetCanvas == null)
            {
                Debug.LogWarning($"SceneTransitionManager: 场景 {currentMainHubSceneName} 的Canvas引用已失效，从字典中移除");
                mainHubCanvasMap.Remove(currentMainHubSceneName);
            }
        }
        
        // 如果未找到，输出警告
        if (targetCanvas == null)
        {
            Debug.LogWarning($"SceneTransitionManager: 未找到场景 {currentMainHubSceneName} 的Canvas引用。请确保该场景的OrderDetailPanel已配置Canvas引用。");
            // 清空记录，避免下次仍然失败
            currentMainHubSceneName = null;
            return;
        }
        
        // 恢复Canvas显示
        targetCanvas.gameObject.SetActive(true);
        Debug.Log($"SceneTransitionManager: 已恢复场景 {currentMainHubSceneName} 的Canvas显示: {targetCanvas.name}");
        
        // 清空记录，为下次隐藏做准备
        currentMainHubSceneName = null;
    }
}

