using UnityEngine;

/// <summary>
/// 事件监听器 - 用于调试和查看所有事件触发
/// 将此脚本添加到场景中的任意GameObject即可
/// </summary>
public class EventListener : MonoBehaviour
{
    [Header("监听设置")]
    [Tooltip("是否启用事件监听")]
    [SerializeField] private bool enableListening = true;
    
    [Tooltip("是否显示详细日志")]
    [SerializeField] private bool showDetailedLogs = true;

    void Start()
    {
        if (!enableListening) return;

        // 订阅所有事件
        SubscribeToAllEvents();
    }

    void OnDestroy()
    {
        // 取消订阅所有事件
        UnsubscribeFromAllEvents();
    }

    private void SubscribeToAllEvents()
    {
        if (EventManager.Instance == null)
        {
            Debug.LogWarning("EventListener: EventManager 实例不存在，无法订阅事件");
            return;
        }

        EventManager.Instance.OnShipStateChanged += OnShipStateChanged;
        EventManager.Instance.OnShipLaunched += OnShipLaunched;
        EventManager.Instance.OnShipCrashed += OnShipCrashed;
        EventManager.Instance.OnShipCaptured += OnShipCaptured;
        EventManager.Instance.OnShipReleased += OnShipReleased;
        EventManager.Instance.OnShipBoosted += OnShipBoosted;
        EventManager.Instance.OnShipSucceed += OnShipSucceed;
        EventManager.Instance.OnShipFailed += OnShipFailed;
        EventManager.Instance.OnShipStartOverheating += OnShipStartOverheating;
        EventManager.Instance.OnShipOverheated += OnShipOverheated;
        EventManager.Instance.OnPlanetCaptureStart += OnPlanetCaptureStart;
        EventManager.Instance.OnPlanetCaptureEnd += OnPlanetCaptureEnd;
        EventManager.Instance.OnCoreDragStart += OnCoreDragStart;
        EventManager.Instance.OnCoreDrag += OnCoreDrag;
        EventManager.Instance.OnCoreDragEnd += OnCoreDragEnd;
        EventManager.Instance.OnGameStart += OnGameStart;
        EventManager.Instance.OnGameEnd += OnGameEnd;
        EventManager.Instance.OnGameReset += OnGameReset;
    }

    private void UnsubscribeFromAllEvents()
    {
        if (EventManager.Instance == null) return;

        EventManager.Instance.OnShipStateChanged -= OnShipStateChanged;
        EventManager.Instance.OnShipLaunched -= OnShipLaunched;
        EventManager.Instance.OnShipCrashed -= OnShipCrashed;
        EventManager.Instance.OnShipCaptured -= OnShipCaptured;
        EventManager.Instance.OnShipReleased -= OnShipReleased;
        EventManager.Instance.OnShipBoosted -= OnShipBoosted;
        EventManager.Instance.OnShipSucceed -= OnShipSucceed;
        EventManager.Instance.OnShipFailed -= OnShipFailed;
        EventManager.Instance.OnShipStartOverheating -= OnShipStartOverheating;
        EventManager.Instance.OnShipOverheated -= OnShipOverheated;
        EventManager.Instance.OnPlanetCaptureStart -= OnPlanetCaptureStart;
        EventManager.Instance.OnPlanetCaptureEnd -= OnPlanetCaptureEnd;
        EventManager.Instance.OnCoreDragStart -= OnCoreDragStart;
        EventManager.Instance.OnCoreDrag -= OnCoreDrag;
        EventManager.Instance.OnCoreDragEnd -= OnCoreDragEnd;
        EventManager.Instance.OnGameStart -= OnGameStart;
        EventManager.Instance.OnGameEnd -= OnGameEnd;
        EventManager.Instance.OnGameReset -= OnGameReset;
    }

    // ========== 事件处理方法 ==========

    private void OnShipStateChanged(ShipState.State oldState, ShipState.State newState, GameObject ship)
    {
        Debug.Log($"<color=cyan>[事件]</color> 飞船状态改变: {oldState} → {newState} | 飞船: {ship.name}");
    }

    private void OnShipLaunched(Vector3 launchVelocity, GameObject ship)
    {
        Debug.Log($"<color=green>[事件]</color> 飞船发射！速度: {launchVelocity.magnitude:F2} | 飞船: {ship.name}");
    }

    private void OnShipCrashed(GameObject collisionObject, Vector3 collisionPoint, GameObject ship)
    {
        Debug.Log($"<color=red>[事件]</color> 飞船碰撞！碰撞对象: {collisionObject.name} | 飞船: {ship.name}");
    }

    private void OnShipCaptured(GameObject planet, GameObject ship)
    {
        Debug.Log($"<color=yellow>[事件]</color> 飞船被捕获！行星: {planet.name} | 飞船: {ship.name}");
    }

    private void OnShipReleased(GameObject planet, GameObject ship)
    {
        Debug.Log($"<color=yellow>[事件]</color> 飞船脱离捕获！行星: {planet.name} | 飞船: {ship.name}");
    }

    private void OnShipBoosted(Vector3 boostDirection, float boostForce, GameObject ship)
    {
        if (showDetailedLogs)
        {
            Debug.Log($"<color=magenta>[事件]</color> 飞船加速！力度: {boostForce:F2} | 飞船: {ship.name}");
        }
    }

    private void OnShipSucceed(GameObject destination, GameObject ship)
    {
        Debug.Log($"<color=green>[事件] ✓ 成功！</color> 飞船到达目的地: {destination.name} | 飞船: {ship.name}");
    }

    private void OnShipFailed(ShipState.State failureReason, GameObject ship)
    {
        string reason = failureReason == ShipState.State.Crashed ? "坠毁" : "逃离";
        Debug.Log($"<color=red>[事件] ✗ 失败！</color> 原因: {reason} | 飞船: {ship.name}");
    }

    private void OnShipStartOverheating(GameObject firePlanet, GameObject ship)
    {
        if (showDetailedLogs)
        {
            Debug.Log($"<color=orange>[事件]</color> 飞船开始过热 | 火行星: {firePlanet.name} | 飞船: {ship.name}");
        }
    }

    private void OnShipOverheated(GameObject firePlanet, GameObject ship)
    {
        Debug.Log($"<color=red>[事件] ✗ 失败！</color> 原因: 过热 | 飞船: {ship.name}");
    }

    private void OnPlanetCaptureStart(GameObject planet, GameObject ship)
    {
        if (showDetailedLogs)
        {
            Debug.Log($"<color=cyan>[事件]</color> 行星开始捕获 | 行星: {planet.name} | 飞船: {ship.name}");
        }
    }

    private void OnPlanetCaptureEnd(GameObject planet, GameObject ship)
    {
        if (showDetailedLogs)
        {
            Debug.Log($"<color=cyan>[事件]</color> 行星结束捕获 | 行星: {planet.name} | 飞船: {ship.name}");
        }
    }

    private void OnCoreDragStart(GameObject core)
    {
        if (showDetailedLogs)
        {
            Debug.Log($"<color=blue>[事件]</color> Core开始拖拽: {core.name}");
        }
    }

    private void OnCoreDrag(GameObject core, Vector3 newPosition)
    {
        // 拖拽中事件太频繁，默认不显示（避免日志刷屏）
        // 如果需要查看，可以取消下面的注释
        // if (showDetailedLogs)
        // {
        //     Debug.Log($"<color=blue>[事件]</color> Core拖拽中: {core.name} | 位置: {newPosition}");
        // }
    }

    private void OnCoreDragEnd(GameObject core, Vector3 finalPosition)
    {
        if (showDetailedLogs)
        {
            Debug.Log($"<color=blue>[事件]</color> Core拖拽结束: {core.name} | 位置: {finalPosition}");
        }
    }

    private void OnGameStart()
    {
        Debug.Log($"<color=lime>[事件]</color> 游戏开始！");
    }

    private void OnGameEnd(bool isVictory)
    {
        string result = isVictory ? "胜利" : "失败";
        Debug.Log($"<color=lime>[事件]</color> 游戏结束！结果: {result}");
    }

    private void OnGameReset()
    {
        Debug.Log($"<color=lime>[事件]</color> 游戏重置！");
    }
}

