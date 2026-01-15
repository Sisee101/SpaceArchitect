using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 游戏事件管理器（单例）
/// 统一管理游戏中的所有事件，提供类型安全的事件系统
/// </summary>
public class EventManager : MonoBehaviour
{
    private static EventManager _instance;
    private static bool _isQuitting = false;
    
    /// <summary>
    /// 获取EventManager单例
    /// </summary>
    public static EventManager Instance
    {
        get
        {
            // 如果应用正在退出或场景正在卸载，不要创建新实例
            if (_isQuitting)
            {
                return null;
            }
            
            if (_instance == null)
            {
                _instance = FindObjectOfType<EventManager>();
                
                if (_instance == null && !_isQuitting)
                {
                    GameObject go = new GameObject("EventManager");
                    _instance = go.AddComponent<EventManager>();
                    DontDestroyOnLoad(go);
                }
            }
            return _instance;
        }
    }

    // ========== 事件定义 ==========
    
    #region 飞船事件
    
    /// <summary>
    /// 飞船状态改变事件
    /// 参数：旧状态，新状态，飞船GameObject
    /// </summary>
    public event Action<ShipState.State, ShipState.State, GameObject> OnShipStateChanged;
    
    /// <summary>
    /// 飞船发射事件
    /// 参数：发射速度，飞船GameObject
    /// </summary>
    public event Action<Vector3, GameObject> OnShipLaunched;
    
    /// <summary>
    /// 飞船碰撞事件
    /// 参数：碰撞对象，碰撞点，飞船GameObject
    /// </summary>
    public event Action<GameObject, Vector3, GameObject> OnShipCrashed;
    
    /// <summary>
    /// 飞船被行星捕获事件
    /// 参数：行星GameObject，飞船GameObject
    /// </summary>
    public event Action<GameObject, GameObject> OnShipCaptured;
    
    /// <summary>
    /// 飞船脱离捕获事件
    /// 参数：行星GameObject，飞船GameObject
    /// </summary>
    public event Action<GameObject, GameObject> OnShipReleased;
    
    /// <summary>
    /// 飞船加速事件
    /// 参数：加速方向，加速力度，飞船GameObject
    /// </summary>
    public event Action<Vector3, float, GameObject> OnShipBoosted;
    
    /// <summary>
    /// 时停开始事件
    /// 参数：飞船GameObject，时停时间缩放
    /// </summary>
    public event Action<GameObject, float> OnTimeStopStart;
    
    /// <summary>
    /// 时停结束事件
    /// 参数：飞船GameObject
    /// </summary>
    public event Action<GameObject> OnTimeStopEnd;
    
    /// <summary>
    /// 飞船成功事件（到达目的地）
    /// 参数：目的地GameObject，飞船GameObject
    /// </summary>
    public event Action<GameObject, GameObject> OnShipSucceed;
    
    /// <summary>
    /// 飞船失败事件（坠毁或逃离）
    /// 参数：失败原因（Crashed或Escaped），飞船GameObject
    /// </summary>
    public event Action<ShipState.State, GameObject> OnShipFailed;
    
    /// <summary>
    /// 飞船开始过热事件（进入加热范围，开始变红效果）
    /// 参数：火行星GameObject，飞船GameObject
    /// </summary>
    public event Action<GameObject, GameObject> OnShipStartOverheating;
    
    /// <summary>
    /// 飞船过热失败事件（过热完成，触发爆炸）
    /// 参数：火行星GameObject，飞船GameObject
    /// </summary>
    public event Action<GameObject, GameObject> OnShipOverheated;
    
    #endregion
    
    #region 行星事件
    
    /// <summary>
    /// 行星捕获开始事件
    /// 参数：行星GameObject，飞船GameObject
    /// </summary>
    public event Action<GameObject, GameObject> OnPlanetCaptureStart;
    
    /// <summary>
    /// 行星捕获结束事件
    /// 参数：行星GameObject，飞船GameObject
    /// </summary>
    public event Action<GameObject, GameObject> OnPlanetCaptureEnd;
    
    /// <summary>
    /// 行星解锁事件
    /// 参数：行星索引（0-12）
    /// </summary>
    public event Action<int> OnPlanetUnlocked;
    
    #endregion
    
    #region Core物体事件
    
    /// <summary>
    /// Core物体开始拖拽事件
    /// 参数：Core GameObject
    /// </summary>
    public event Action<GameObject> OnCoreDragStart;
    
    /// <summary>
    /// Core物体拖拽中事件
    /// 参数：Core GameObject，新位置
    /// </summary>
    public event Action<GameObject, Vector3> OnCoreDrag;
    
    /// <summary>
    /// Core物体拖拽结束事件
    /// 参数：Core GameObject，最终位置
    /// </summary>
    public event Action<GameObject, Vector3> OnCoreDragEnd;
    
    #endregion
    
    #region 游戏流程事件
    
    /// <summary>
    /// 游戏开始事件
    /// </summary>
    public event Action OnGameStart;
    
    /// <summary>
    /// 游戏结束事件
    /// 参数：是否胜利
    /// </summary>
    public event Action<bool> OnGameEnd;
    
    /// <summary>
    /// 游戏重置事件
    /// </summary>
    public event Action OnGameReset;
    
    #endregion
    
    #region 邮箱事件
    
    /// <summary>
    /// M键按下事件（用于插入新邮件）
    /// </summary>
    public event Action OnMKeyPressed;
    
    /// <summary>
    /// C键按下事件（用于清空邮箱并重置到初始状态）
    /// </summary>
    public event Action OnCKeyPressed;
    
    #endregion

    // ========== 事件触发方法 ==========
    
    #region 飞船事件触发
    
    /// <summary>
    /// 触发飞船状态改变事件
    /// </summary>
    public void TriggerShipStateChanged(ShipState.State oldState, ShipState.State newState, GameObject ship)
    {
        OnShipStateChanged?.Invoke(oldState, newState, ship);
    }
    
    /// <summary>
    /// 触发飞船发射事件
    /// </summary>
    public void TriggerShipLaunched(Vector3 launchVelocity, GameObject ship)
    {
        OnShipLaunched?.Invoke(launchVelocity, ship);
    }
    
    /// <summary>
    /// 触发飞船碰撞事件
    /// </summary>
    public void TriggerShipCrashed(GameObject collisionObject, Vector3 collisionPoint, GameObject ship)
    {
        OnShipCrashed?.Invoke(collisionObject, collisionPoint, ship);
    }
    
    /// <summary>
    /// 触发飞船被捕获事件
    /// </summary>
    public void TriggerShipCaptured(GameObject planet, GameObject ship)
    {
        OnShipCaptured?.Invoke(planet, ship);
    }
    
    /// <summary>
    /// 触发飞船脱离捕获事件
    /// </summary>
    public void TriggerShipReleased(GameObject planet, GameObject ship)
    {
        OnShipReleased?.Invoke(planet, ship);
    }
    
    /// <summary>
    /// 触发飞船加速事件
    /// </summary>
    public void TriggerShipBoosted(Vector3 boostDirection, float boostForce, GameObject ship)
    {
        OnShipBoosted?.Invoke(boostDirection, boostForce, ship);
    }
    
    /// <summary>
    /// 触发时停开始事件
    /// </summary>
    public void TriggerTimeStopStart(GameObject ship, float timeScale)
    {
        OnTimeStopStart?.Invoke(ship, timeScale);
    }
    
    /// <summary>
    /// 触发时停结束事件
    /// </summary>
    public void TriggerTimeStopEnd(GameObject ship)
    {
        OnTimeStopEnd?.Invoke(ship);
    }
    
    /// <summary>
    /// 触发飞船成功事件
    /// </summary>
    public void TriggerShipSucceed(GameObject destination, GameObject ship)
    {
        OnShipSucceed?.Invoke(destination, ship);
    }
    
    /// <summary>
    /// 触发飞船失败事件
    /// </summary>
    public void TriggerShipFailed(ShipState.State failureReason, GameObject ship)
    {
        OnShipFailed?.Invoke(failureReason, ship);
    }
    
    /// <summary>
    /// 触发飞船开始过热事件
    /// </summary>
    public void TriggerShipStartOverheating(GameObject firePlanet, GameObject ship)
    {
        OnShipStartOverheating?.Invoke(firePlanet, ship);
    }
    
    /// <summary>
    /// 触发飞船过热失败事件
    /// </summary>
    public void TriggerShipOverheated(GameObject firePlanet, GameObject ship)
    {
        OnShipOverheated?.Invoke(firePlanet, ship);
    }
    
    #endregion
    
    #region 行星事件触发
    
    /// <summary>
    /// 触发行星捕获开始事件
    /// </summary>
    public void TriggerPlanetCaptureStart(GameObject planet, GameObject ship)
    {
        OnPlanetCaptureStart?.Invoke(planet, ship);
    }
    
    /// <summary>
    /// 触发行星捕获结束事件
    /// </summary>
    public void TriggerPlanetCaptureEnd(GameObject planet, GameObject ship)
    {
        OnPlanetCaptureEnd?.Invoke(planet, ship);
    }
    
    /// <summary>
    /// 触发行星解锁事件
    /// </summary>
    public void TriggerPlanetUnlocked(int planetIndex)
    {
        OnPlanetUnlocked?.Invoke(planetIndex);
    }
    
    #endregion
    
    #region Core物体事件触发
    
    /// <summary>
    /// 触发Core物体开始拖拽事件
    /// </summary>
    public void TriggerCoreDragStart(GameObject core)
    {
        OnCoreDragStart?.Invoke(core);
    }
    
    /// <summary>
    /// 触发Core物体拖拽中事件
    /// </summary>
    public void TriggerCoreDrag(GameObject core, Vector3 newPosition)
    {
        OnCoreDrag?.Invoke(core, newPosition);
    }
    
    /// <summary>
    /// 触发Core物体拖拽结束事件
    /// </summary>
    public void TriggerCoreDragEnd(GameObject core, Vector3 finalPosition)
    {
        OnCoreDragEnd?.Invoke(core, finalPosition);
    }
    
    #endregion
    
    #region 游戏流程事件触发
    
    /// <summary>
    /// 触发游戏开始事件
    /// </summary>
    public void TriggerGameStart()
    {
        OnGameStart?.Invoke();
    }
    
    /// <summary>
    /// 触发游戏结束事件
    /// </summary>
    public void TriggerGameEnd(bool isVictory)
    {
        OnGameEnd?.Invoke(isVictory);
    }
    
    /// <summary>
    /// 触发游戏重置事件
    /// </summary>
    public void TriggerGameReset()
    {
        OnGameReset?.Invoke();
    }
    
    #endregion
    
    #region 邮箱事件触发
    
    /// <summary>
    /// 触发M键按下事件
    /// </summary>
    public void TriggerMKeyPressed()
    {
        OnMKeyPressed?.Invoke();
    }
    
    /// <summary>
    /// 触发C键按下事件
    /// </summary>
    public void TriggerCKeyPressed()
    {
        OnCKeyPressed?.Invoke();
    }
    
    #endregion

    // ========== Unity生命周期 ==========
    
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
            Debug.LogWarning("检测到多个EventManager实例，销毁重复的实例");
            Destroy(gameObject);
        }
    }
    
    void OnDestroy()
    {
        // 标记正在销毁，防止在OnDestroy期间重新创建实例
        if (_instance == this)
        {
            _isQuitting = true;
            _instance = null;
        }
        
        // 清理所有事件订阅（防止内存泄漏）
        ClearAllEvents();
    }
    
    void OnApplicationQuit()
    {
        // 应用退出时标记，防止创建新实例
        _isQuitting = true;
    }
    
    /// <summary>
    /// 清理所有事件订阅
    /// </summary>
    public void ClearAllEvents()
    {
        OnShipStateChanged = null;
        OnShipLaunched = null;
        OnShipCrashed = null;
        OnShipCaptured = null;
        OnShipReleased = null;
        OnShipBoosted = null;
        OnTimeStopStart = null;
        OnTimeStopEnd = null;
        OnShipSucceed = null;
        OnShipFailed = null;
        OnShipStartOverheating = null;
        OnShipOverheated = null;
        OnPlanetCaptureStart = null;
        OnPlanetCaptureEnd = null;
        OnPlanetUnlocked = null;
        OnCoreDragStart = null;
        OnCoreDrag = null;
        OnCoreDragEnd = null;
        OnGameStart = null;
        OnGameEnd = null;
        OnGameReset = null;
        OnMKeyPressed = null;
        OnCKeyPressed = null;
    }
    
    /// <summary>
    /// 获取事件订阅数量（用于调试）
    /// </summary>
    [ContextMenu("显示事件订阅统计")]
    public void ShowEventSubscriptions()
    {
        Debug.Log("=== EventManager 事件订阅统计 ===");
        Debug.Log($"OnShipStateChanged: {GetSubscriberCount(OnShipStateChanged)}");
        Debug.Log($"OnShipLaunched: {GetSubscriberCount(OnShipLaunched)}");
        Debug.Log($"OnShipCrashed: {GetSubscriberCount(OnShipCrashed)}");
        Debug.Log($"OnShipCaptured: {GetSubscriberCount(OnShipCaptured)}");
        Debug.Log($"OnShipReleased: {GetSubscriberCount(OnShipReleased)}");
        Debug.Log($"OnShipBoosted: {GetSubscriberCount(OnShipBoosted)}");
        Debug.Log($"OnTimeStopStart: {GetSubscriberCount(OnTimeStopStart)}");
        Debug.Log($"OnTimeStopEnd: {GetSubscriberCount(OnTimeStopEnd)}");
        Debug.Log($"OnShipSucceed: {GetSubscriberCount(OnShipSucceed)}");
        Debug.Log($"OnShipFailed: {GetSubscriberCount(OnShipFailed)}");
        Debug.Log($"OnShipStartOverheating: {GetSubscriberCount(OnShipStartOverheating)}");
        Debug.Log($"OnShipOverheated: {GetSubscriberCount(OnShipOverheated)}");
        Debug.Log($"OnPlanetCaptureStart: {GetSubscriberCount(OnPlanetCaptureStart)}");
        Debug.Log($"OnPlanetCaptureEnd: {GetSubscriberCount(OnPlanetCaptureEnd)}");
        Debug.Log($"OnPlanetUnlocked: {GetSubscriberCount(OnPlanetUnlocked)}");
        Debug.Log($"OnCoreDragStart: {GetSubscriberCount(OnCoreDragStart)}");
        Debug.Log($"OnCoreDrag: {GetSubscriberCount(OnCoreDrag)}");
        Debug.Log($"OnCoreDragEnd: {GetSubscriberCount(OnCoreDragEnd)}");
        Debug.Log($"OnGameStart: {GetSubscriberCount(OnGameStart)}");
        Debug.Log($"OnGameEnd: {GetSubscriberCount(OnGameEnd)}");
        Debug.Log($"OnGameReset: {GetSubscriberCount(OnGameReset)}");
        Debug.Log("================================");
    }
    
    private int GetSubscriberCount(Delegate del)
    {
        if (del == null) return 0;
        return del.GetInvocationList().Length;
    }
}

