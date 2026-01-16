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
    
    // 当前禁用的MainHub场景相机引用（用于恢复）
    private static Camera currentMainHubCamera = null;
    
    // 保存MainHub相机的Transform状态（位置和旋转）
    private static Vector3 savedCameraPosition;
    private static Quaternion savedCameraRotation;
    
    // 当前禁用的MainHub场景EventSystem引用（用于恢复）
    private static UnityEngine.EventSystems.EventSystem currentMainHubEventSystem = null;
    
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
        
        // 禁用MainHub场景的相机，避免相机冲突影响游戏场景交互
        DisableMainHubCamera();
        
        // 禁用MainHub场景的EventSystem，避免输入冲突影响游戏场景交互
        DisableMainHubEventSystem();
        
        // 关键修复：等待一帧，确保 MainHub 的相机和 EventSystem 已被禁用
        // 然后再加载关卡场景，避免冲突
        StartCoroutine(LoadGameSceneAdditiveCoroutine(sceneName));
        
        Debug.Log($"SceneTransitionManager: 准备使用Additive模式加载场景 {sceneName}，MainHub UI已隐藏，相机已禁用，EventSystem已禁用");
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
        
        // 恢复MainHub场景的相机
        EnableMainHubCamera();
        
        // 恢复MainHub场景的EventSystem
        EnableMainHubEventSystem();
        
        Debug.Log($"SceneTransitionManager: 已卸载游戏场景 {sceneToUnload}，返回MainHub，MainHub UI已恢复显示，相机已恢复，EventSystem已恢复");
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
    
    /// <summary>
    /// 禁用MainHub场景的相机
    /// </summary>
    private void DisableMainHubCamera()
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
            Debug.LogWarning($"SceneTransitionManager: 当前场景 {currentSceneName} 不是MainHub场景，无法禁用相机");
            return;
        }
        
        // 获取MainHub场景
        Scene mainHubScene = SceneManager.GetSceneByName(currentSceneName);
        if (!mainHubScene.IsValid() || !mainHubScene.isLoaded)
        {
            Debug.LogWarning($"SceneTransitionManager: MainHub场景 {currentSceneName} 未加载，无法禁用相机");
            return;
        }
        
        // 查找场景中的所有相机（禁用所有MainHub场景的相机，不仅仅是第一个）
        Camera[] cameras = FindObjectsOfType<Camera>();
        Camera mainHubCamera = null;
        int disabledCount = 0;
        
        foreach (Camera cam in cameras)
        {
            // 检查相机是否属于MainHub场景
            if (cam.gameObject.scene == mainHubScene)
            {
                // 如果相机已启用，禁用它
                if (cam.enabled)
                {
                    cam.enabled = false;
                    disabledCount++;
                    Debug.Log($"SceneTransitionManager: 已禁用MainHub场景的相机: {cam.name} (Tag: {cam.tag})");
                }
                
                // 保存第一个找到的相机作为主相机（用于恢复）
                if (mainHubCamera == null)
                {
                    mainHubCamera = cam;
                }
            }
        }
        
        if (mainHubCamera == null)
        {
            Debug.LogWarning($"SceneTransitionManager: 未找到场景 {currentSceneName} 的相机");
            return;
        }
        
        // 保存主相机的Transform状态（位置和旋转）
        savedCameraPosition = mainHubCamera.transform.position;
        savedCameraRotation = mainHubCamera.transform.rotation;
        
        // 保存主相机引用（用于恢复）
        currentMainHubCamera = mainHubCamera;
        
        Debug.Log($"SceneTransitionManager: 已禁用场景 {currentSceneName} 的 {disabledCount} 个相机，主相机: {mainHubCamera.name}，位置: {savedCameraPosition}，旋转: {savedCameraRotation.eulerAngles}");
    }
    
    /// <summary>
    /// 恢复MainHub场景的相机
    /// </summary>
    private void EnableMainHubCamera()
    {
        if (currentMainHubCamera == null)
        {
            // 如果没有保存的相机引用，尝试从已加载的场景中查找
            for (int i = 0; i < SceneManager.sceneCount; i++)
            {
                Scene scene = SceneManager.GetSceneAt(i);
                if (scene.isLoaded && scene.name.StartsWith("0") && scene.name.Contains("_MainHub"))
                {
                    // 查找场景中的相机
                    Camera[] cameras = FindObjectsOfType<Camera>();
                    foreach (Camera cam in cameras)
                    {
                        if (cam.gameObject.scene == scene)
                        {
                            currentMainHubCamera = cam;
                            break;
                        }
                    }
                    break;
                }
            }
        }
        
        if (currentMainHubCamera == null)
        {
            Debug.LogWarning("SceneTransitionManager: 未找到MainHub场景的相机引用，无法恢复相机");
            return;
        }
        
        // 验证相机引用是否有效
        if (currentMainHubCamera == null || currentMainHubCamera.gameObject == null)
        {
            Debug.LogWarning("SceneTransitionManager: MainHub场景的相机引用已失效");
            currentMainHubCamera = null;
            return;
        }
        
        // 恢复相机的Transform状态（位置和旋转）
        currentMainHubCamera.transform.position = savedCameraPosition;
        currentMainHubCamera.transform.rotation = savedCameraRotation;
        
        // 恢复相机
        currentMainHubCamera.enabled = true;
        string cameraName = currentMainHubCamera.name;
        currentMainHubCamera = null; // 清空引用，为下次禁用做准备
        
        Debug.Log($"SceneTransitionManager: 已恢复MainHub场景的相机显示: {cameraName}，位置: {savedCameraPosition}，旋转: {savedCameraRotation.eulerAngles}");
    }
    
    /// <summary>
    /// 禁用MainHub场景的EventSystem
    /// </summary>
    private void DisableMainHubEventSystem()
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
            Debug.LogWarning($"SceneTransitionManager: 当前场景 {currentSceneName} 不是MainHub场景，无法禁用EventSystem");
            return;
        }
        
        // 获取MainHub场景
        Scene mainHubScene = SceneManager.GetSceneByName(currentSceneName);
        if (!mainHubScene.IsValid() || !mainHubScene.isLoaded)
        {
            Debug.LogWarning($"SceneTransitionManager: MainHub场景 {currentSceneName} 未加载，无法禁用EventSystem");
            return;
        }
        
        // 查找场景中的所有EventSystem
        UnityEngine.EventSystems.EventSystem[] eventSystems = FindObjectsOfType<UnityEngine.EventSystems.EventSystem>();
        UnityEngine.EventSystems.EventSystem mainHubEventSystem = null;
        
        foreach (UnityEngine.EventSystems.EventSystem es in eventSystems)
        {
            // 检查EventSystem是否属于MainHub场景
            if (es.gameObject.scene == mainHubScene)
            {
                mainHubEventSystem = es;
                break;
            }
        }
        
        if (mainHubEventSystem == null)
        {
            Debug.LogWarning($"SceneTransitionManager: 未找到场景 {currentSceneName} 的EventSystem");
            return;
        }
        
        // 禁用EventSystem并保存引用
        mainHubEventSystem.enabled = false;
        currentMainHubEventSystem = mainHubEventSystem;
        
        Debug.Log($"SceneTransitionManager: 已禁用场景 {currentSceneName} 的EventSystem: {mainHubEventSystem.name}");
    }
    
    /// <summary>
    /// 恢复MainHub场景的EventSystem
    /// </summary>
    private void EnableMainHubEventSystem()
    {
        if (currentMainHubEventSystem == null)
        {
            // 如果没有保存的EventSystem引用，尝试从已加载的场景中查找
            for (int i = 0; i < SceneManager.sceneCount; i++)
            {
                Scene scene = SceneManager.GetSceneAt(i);
                if (scene.isLoaded && scene.name.StartsWith("0") && scene.name.Contains("_MainHub"))
                {
                    // 查找场景中的EventSystem
                    UnityEngine.EventSystems.EventSystem[] eventSystems = FindObjectsOfType<UnityEngine.EventSystems.EventSystem>();
                    foreach (UnityEngine.EventSystems.EventSystem es in eventSystems)
                    {
                        if (es.gameObject.scene == scene)
                        {
                            currentMainHubEventSystem = es;
                            break;
                        }
                    }
                    break;
                }
            }
        }
        
        if (currentMainHubEventSystem == null)
        {
            Debug.LogWarning("SceneTransitionManager: 未找到MainHub场景的EventSystem引用，无法恢复EventSystem");
            return;
        }
        
        // 验证EventSystem引用是否有效
        if (currentMainHubEventSystem == null || currentMainHubEventSystem.gameObject == null)
        {
            Debug.LogWarning("SceneTransitionManager: MainHub场景的EventSystem引用已失效");
            currentMainHubEventSystem = null;
            return;
        }
        
        // 恢复EventSystem
        currentMainHubEventSystem.enabled = true;
        string eventSystemName = currentMainHubEventSystem.name;
        currentMainHubEventSystem = null; // 清空引用，为下次禁用做准备
        
        Debug.Log($"SceneTransitionManager: 已恢复MainHub场景的EventSystem显示: {eventSystemName}");
    }
    
    /// <summary>
    /// 协程：延迟加载关卡场景，确保 MainHub 的相机和 EventSystem 已被禁用
    /// </summary>
    private System.Collections.IEnumerator LoadGameSceneAdditiveCoroutine(string sceneName)
    {
        // 等待一帧，确保 MainHub 的相机和 EventSystem 已被禁用
        yield return null;
        
        // 再次确认 MainHub 场景的所有相机已被禁用（防止异步加载时冲突）
        Camera[] allCameras = FindObjectsOfType<Camera>();
        int forceDisabledCount = 0;
        foreach (Camera cam in allCameras)
        {
            // 检查是否是 MainHub 场景的相机
            string camSceneName = cam.gameObject.scene.name;
            if (camSceneName.StartsWith("0") && camSceneName.Contains("_MainHub"))
            {
                if (cam.enabled)
                {
                    Debug.LogWarning($"SceneTransitionManager: 检测到 MainHub 场景的相机 {cam.name} (Tag: {cam.tag}) 仍然启用，强制禁用");
                    cam.enabled = false;
                    forceDisabledCount++;
                }
            }
        }
        if (forceDisabledCount > 0)
        {
            Debug.LogWarning($"SceneTransitionManager: 强制禁用了 {forceDisabledCount} 个MainHub场景的相机");
        }
        
        // 使用Additive模式异步加载场景
        AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Additive);
        
        // 等待场景加载完成
        while (!asyncLoad.isDone)
        {
            yield return null;
        }
        
        // 场景加载完成后，确保关卡场景的相机是激活的，MainHub 的相机是禁用的
        yield return null; // 再等待一帧，确保场景完全初始化
        
        // 查找关卡场景中的相机并确保它是激活的
        Scene gameScene = SceneManager.GetSceneByName(sceneName);
        if (gameScene.IsValid() && gameScene.isLoaded)
        {
            Camera[] gameCameras = FindObjectsOfType<Camera>();
            Camera gameSceneCamera = null;
            
            // 首先查找关卡场景中Tag为MainCamera的相机
            foreach (Camera cam in gameCameras)
            {
                if (cam.gameObject.scene == gameScene && cam.CompareTag("MainCamera"))
                {
                    cam.enabled = true;
                    gameSceneCamera = cam;
                    Debug.Log($"SceneTransitionManager: 已激活关卡场景 {sceneName} 的MainCamera: {cam.name}");
                    break; // 只激活第一个MainCamera
                }
            }
            
            // 如果没找到MainCamera，查找关卡场景中的任何相机
            if (gameSceneCamera == null)
            {
                foreach (Camera cam in gameCameras)
                {
                    if (cam.gameObject.scene == gameScene)
                    {
                        cam.enabled = true;
                        gameSceneCamera = cam;
                        Debug.LogWarning($"SceneTransitionManager: 关卡场景 {sceneName} 中没有MainCamera，已激活备用相机: {cam.name}");
                        break;
                    }
                }
            }
            
            // 确保MainHub场景的所有相机都被禁用
            foreach (Camera cam in gameCameras)
            {
                string camSceneName = cam.gameObject.scene.name;
                if (camSceneName.StartsWith("0") && camSceneName.Contains("_MainHub"))
                {
                    if (cam.enabled)
                    {
                        Debug.LogWarning($"SceneTransitionManager: 检测到MainHub场景的相机 {cam.name} 仍然启用，强制禁用");
                        cam.enabled = false;
                    }
                }
            }
        }
        
        // 保存当前加载的游戏场景名称
        currentLoadedGameScene = sceneName;
        
        Debug.Log($"SceneTransitionManager: 场景 {sceneName} 已加载完成，MainHub 相机已禁用，关卡相机已激活");
    }
}

