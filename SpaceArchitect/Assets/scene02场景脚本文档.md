# scene02场景脚本文档

本文档包含用于游戏场景（scene02）的所有脚本代码。

---

## 1. PausePanel.cs - 暂停面板控制器

**位置**：`Assets/Scripts/UI/PausePanel.cs`

**功能**：处理游戏暂停时的UI显示和交互

### 完整代码：

```csharp
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
```

### 功能说明：
- **暂停/恢复游戏**：使用 `Time.timeScale` 控制游戏时间
- **三个按钮功能**：
  - 继续游戏：恢复时间并隐藏面板
  - 回到主页面：加载主菜单场景
  - 重玩游戏：重新加载当前场景
- **ESC键支持**：按ESC键可以切换暂停状态

---

## 2. PauseButton.cs - 暂停按钮控制器

**位置**：`Assets/Scripts/UI/PauseButton.cs`

**功能**：处理暂停按钮的点击事件

### 完整代码：

```csharp
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
```

### 功能说明：
- **自动查找暂停面板**：如果没有手动指定，会自动查找场景中的 `PausePanel`
- **点击切换暂停**：点击按钮会调用 `PausePanel.TogglePause()`

---

## 3. SceneTransitionManager.cs - 场景切换管理器

**位置**：`Assets/Scripts/Core/SceneTransitionManager.cs`

**功能**：负责场景切换、全局状态管理等核心功能

### 完整代码：

```csharp
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
        MainMenu,    // 主菜单
        Playing,     // 游戏中
        Paused       // 暂停
    }
    
    [SerializeField] private GameState currentState = GameState.MainMenu;
    
    // 场景名称常量
    private const string MAIN_MENU_SCENE = "00_MainMenu";
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
        // 如果当前场景是主菜单，设置状态
        if (SceneManager.GetActiveScene().name == MAIN_MENU_SCENE)
        {
            currentState = GameState.MainMenu;
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
    /// 加载游戏场景
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
```

### 功能说明：
- **单例模式**：确保全局只有一个实例
- **场景切换**：
  - `LoadMainMenuScene()` - 加载主菜单
  - `LoadGameScene()` - 加载游戏场景
  - `RestartGame()` - 重新开始当前场景
- **游戏状态管理**：管理主菜单、游戏中、暂停三种状态

---

## 使用说明

### 在scene02场景中搭建暂停功能：

1. **创建Canvas**（如果不存在）
   - 在Hierarchy中右键 → `UI` → `Canvas`
   - 重命名为 `GameplayCanvas`

2. **创建暂停按钮**
   - 在Canvas下右键 → `UI` → `Button`
   - 重命名为 `PauseButton`
   - 添加 `PauseButton` 脚本组件
   - 设置位置到右上角

3. **创建暂停面板**
   - 在Canvas下右键 → `UI` → `Panel`
   - 重命名为 `PausePanel`
   - 添加 `PausePanel` 脚本组件
   - 默认隐藏（取消勾选Active）

4. **创建三个按钮**
   - 在PausePanel下创建三个Button：
     - `ResumeButton`（继续游戏）
     - `MainMenuButton`（回到主页面）
     - `RestartButton`（重玩游戏）

5. **配置脚本引用**
   - 在PausePanel的Inspector中，将三个按钮拖拽到对应字段
   - 在PauseButton的Inspector中，将PausePanel拖拽到对应字段（或留空，会自动查找）

6. **确保SceneTransitionManager存在**
   - 检查场景中是否有SceneTransitionManager GameObject
   - 如果没有，代码会自动创建（单例模式）

---

## 注意事项

1. **Time.timeScale**：
   - 暂停时设置为0，恢复时设置为1
   - 场景切换前必须恢复为1，避免新场景时间仍为0

2. **单例警告**：
   - 如果看到"检测到多个SceneTransitionManager实例"的警告，这是正常的
   - 单例模式会自动销毁重复的实例
   - 确保只在主菜单场景中放置SceneTransitionManager

3. **场景名称**：
   - 确保场景名称与代码中的常量一致：
     - `00_MainMenu` - 主菜单场景
     - `scene02` - 游戏场景

---

## 文件位置参考

根据你的项目结构，脚本应该放在：
- `Assets/Scripts/UI/PausePanel.cs`
- `Assets/Scripts/UI/PauseButton.cs`
- `Assets/Scripts/Core/SceneTransitionManager.cs`

---

## 总结

这三个脚本是scene02场景暂停功能的核心：

1. **PausePanel** - 暂停面板，包含三个按钮的功能实现
2. **PauseButton** - 暂停按钮，用于触发暂停
3. **SceneTransitionManager** - 场景切换管理器，用于场景切换和状态管理

所有代码都已包含在本文档中，你可以直接复制使用。

