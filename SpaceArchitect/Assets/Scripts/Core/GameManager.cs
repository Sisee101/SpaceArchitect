using UnityEngine;

/// <summary>
/// 游戏重启管理器（场景级单例）
/// 管理游戏重启功能，监听失败事件并处理R键重启
/// 注意：每个场景拥有独立的GameRestartManager实例，场景卸载时实例会被销毁
/// </summary>
public class GameRestartManager : MonoBehaviour
{
    private static GameRestartManager _instance;
    private static bool _isQuitting = false;

    /// <summary>
    /// 获取GameRestartManager单例（场景级别）
    /// 每个场景拥有独立的GameRestartManager实例
    /// 注意：需要在场景中手动放置GameRestartManager GameObject，否则返回null
    /// </summary>
    public static GameRestartManager Instance
    {
        get
        {
            // 如果应用正在退出或场景正在卸载，不要查找实例
            if (_isQuitting)
            {
                return null;
            }

            if (_instance == null)
            {
                // 查找当前场景中的GameRestartManager（不自动创建）
                _instance = FindObjectOfType<GameRestartManager>();

                if (_instance == null)
                {
                    Debug.LogWarning($"GameRestartManager: 场景 {UnityEngine.SceneManagement.SceneManager.GetActiveScene().name} 中未找到GameRestartManager实例，请在场景中手动添加GameRestartManager GameObject");
                }
            }
            return _instance;
        }
    }

    [Header("重启设置")]
    [Tooltip("重启游戏按键（当前为R键）")]
    [SerializeField] private KeyCode restartKey = KeyCode.R;

    [Tooltip("飞船GameObject（如果为空，将自动查找场景中的飞船）")]
    [SerializeField] private GameObject shipGameObject;

    [Header("成功UI设置")]
    [Tooltip("游戏成功UI弹窗（拖入成功弹窗的GameObject，如果没有则留空）")]
    [SerializeField] private GameObject successUIPanel;
    
    [Tooltip("成功面板控制器（如果成功面板有SuccessPanel脚本，可以配置此字段以获得更好的控制）")]
    [SerializeField] private SuccessPanel successPanelController;

    private ShipState shipState;
    private bool canRestart = false; // 是否允许重启（只有在失败后才能重启）

    void Awake()
    {
        // 确保场景内单例（允许不同场景有不同实例）
        if (_instance == null)
        {
            _instance = this;
            Debug.Log($"GameRestartManager: 初始化场景 {gameObject.scene.name} 的GameRestartManager实例");
        }
        else if (_instance != this)
        {
            Debug.LogWarning($"GameRestartManager: 场景 {gameObject.scene.name} 中检测到多个GameRestartManager实例，销毁重复的实例");
            Destroy(gameObject);
            return;
        }
    }

    void Start()
    {
        // 初始化飞船引用
        InitializeShipReference();

        // 订阅事件
        if (EventManager.Instance != null)
        {
            EventManager.Instance.OnShipFailed += OnShipFailed;
            EventManager.Instance.OnShipSucceed += OnShipSucceed;
        }
    }

    void Update()
    {
        // 检查重启按键
        if (canRestart && Input.GetKeyDown(restartKey))
        {
            RestartGame();
        }
    }

    void OnDestroy()
    {
        // 场景卸载时重置实例引用
        if (_instance == this)
        {
            _instance = null;
            Debug.Log($"GameRestartManager: 场景 {gameObject.scene.name} 的GameRestartManager实例已销毁");
        }

        // 取消订阅事件
        if (EventManager.Instance != null)
        {
            EventManager.Instance.OnShipFailed -= OnShipFailed;
            EventManager.Instance.OnShipSucceed -= OnShipSucceed;
        }
    }

    void OnApplicationQuit()
    {
        // 应用退出时标记，防止创建新实例
        _isQuitting = true;
    }

    /// <summary>
    /// 初始化飞船引用
    /// </summary>
    private void InitializeShipReference()
    {
        // 如果已经在Inspector中设置了飞船引用，直接使用
        if (shipGameObject != null)
        {
            shipState = shipGameObject.GetComponent<ShipState>();
            if (shipState == null)
            {
                Debug.LogWarning("GameRestartManager: 指定的飞船GameObject没有ShipState组件");
            }
            return;
        }

        // 否则尝试自动查找场景中的飞船
        // 方法1：通过Tag查找
        GameObject shipByTag = GameObject.FindGameObjectWithTag("Spaceship");
        if (shipByTag != null)
        {
            shipGameObject = shipByTag;
            shipState = shipByTag.GetComponent<ShipState>();
            if (shipState != null)
            {
                Debug.Log("GameRestartManager: 通过Tag找到了飞船");
                return;
            }
        }

        // 方法2：通过ShipState组件查找
        ShipState foundShipState = FindObjectOfType<ShipState>();
        if (foundShipState != null)
        {
            shipGameObject = foundShipState.gameObject;
            shipState = foundShipState;
            Debug.Log("GameRestartManager: 通过ShipState组件找到了飞船");
            return;
        }

        Debug.LogWarning("GameRestartManager: 未找到飞船，重启功能将不可用。请确保场景中有飞船对象，或者在Inspector中手动指定飞船引用。");
    }

    /// <summary>
    /// 飞船失败事件回调
    /// </summary>
    private void OnShipFailed(ShipState.State failureReason, GameObject ship)
    {
        // 飞船失败后允许重启
        canRestart = true;
        Debug.Log($"游戏失败（原因: {failureReason}），按 {restartKey} 键重启游戏");
    }

    /// <summary>
    /// 飞船成功事件回调
    /// </summary>
    private void OnShipSucceed(GameObject destination, GameObject ship)
    {
        Debug.Log($"游戏成功！飞船已到达目的地: {destination.name}");
        
        // 立即暂停游戏
        PauseGame();
        
        // 显示成功UI弹窗（预留接口，待UI系统实现）
        ShowSuccessUI();
        
        // 成功后也可以重启（可选，根据需求决定）
        // canRestart = true;
    }
    
    /// <summary>
    /// 暂停游戏
    /// </summary>
    private void PauseGame()
    {
        Time.timeScale = 0f;
        Debug.Log("游戏已暂停（Time.timeScale = 0）");
    }
    
    /// <summary>
    /// 恢复游戏
    /// </summary>
    public void ResumeGame()
    {
        Time.timeScale = 1f;
        Debug.Log("游戏已恢复（Time.timeScale = 1）");
    }
    
    /// <summary>
    /// 显示成功UI弹窗
    /// </summary>
    private void ShowSuccessUI()
    {
        // 优先使用 SuccessPanel 控制器（如果配置了）
        if (successPanelController != null)
        {
            successPanelController.Show();
            Debug.Log("游戏成功UI弹窗已显示（通过SuccessPanel控制器）");
            return;
        }
        
        // 如果没有配置控制器，使用 GameObject 直接激活（兼容旧方式）
        if (successUIPanel != null)
        {
            // 如果UI面板存在，激活它
            successUIPanel.SetActive(true);
            
            // 尝试获取 SuccessPanel 组件并调用 Show 方法
            SuccessPanel panel = successUIPanel.GetComponent<SuccessPanel>();
            if (panel != null)
            {
                panel.Show();
            }
            
            Debug.Log("游戏成功UI弹窗已显示");
        }
        else
        {
            // 如果没有设置UI面板，只输出日志
            Debug.Log("=== 游戏成功UI弹窗 ===");
            Debug.LogWarning("GameRestartManager: 成功UI弹窗未设置，请在Inspector中拖入成功UI Panel GameObject");
        }
    }

    /// <summary>
    /// 重启游戏
    /// 重置飞船到初始状态
    /// </summary>
    public void RestartGame()
    {
        if (shipState == null)
        {
            Debug.LogError("GameRestartManager: 无法重启游戏，飞船引用为空！");
            return;
        }

        Debug.Log("=== 游戏重启 ===");

        // 重置飞船
        shipState.ResetShip();

        // 重置重启标志
        canRestart = false;

        // 触发游戏重置事件（允许其他系统响应重置）
        if (EventManager.Instance != null)
        {
            EventManager.Instance.TriggerGameReset();
        }

        Debug.Log("游戏已重启，飞船已重置到初始位置");
    }

    /// <summary>
    /// 手动设置飞船引用（用于运行时动态设置）
    /// </summary>
    public void SetShipReference(GameObject ship)
    {
        if (ship == null)
        {
            Debug.LogError("GameRestartManager: 设置的飞船引用不能为空");
            return;
        }

        shipGameObject = ship;
        shipState = ship.GetComponent<ShipState>();

        if (shipState == null)
        {
            Debug.LogError("GameRestartManager: 指定的GameObject没有ShipState组件");
        }
        else
        {
            Debug.Log($"GameRestartManager: 已设置飞船引用: {ship.name}");
        }
    }

    /// <summary>
    /// 设置重启按键（用于后续扩展，支持自定义按键）
    /// </summary>
    public void SetRestartKey(KeyCode key)
    {
        restartKey = key;
        Debug.Log($"GameRestartManager: 重启按键已设置为 {key}");
    }

    /// <summary>
    /// 获取当前是否可以重启
    /// </summary>
    public bool CanRestart()
    {
        return canRestart;
    }
}


