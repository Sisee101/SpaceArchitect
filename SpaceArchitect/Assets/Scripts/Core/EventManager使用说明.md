# EventManager 使用说明

## 概述

`EventManager` 是一个单例事件管理器，用于统一管理游戏中的所有事件。它提供了类型安全的事件系统，让不同脚本之间可以松耦合地通信。

## 快速开始

### 1. 创建 EventManager

EventManager 会自动创建，无需手动设置：

- 如果场景中没有 EventManager，会在首次访问时自动创建
- EventManager 会自动设置为 `DontDestroyOnLoad`，在场景切换时保持存在

### 2. 订阅事件

在任何脚本中订阅事件：

```csharp
using UnityEngine;

public class MyScript : MonoBehaviour
{
    void Start()
    {
        // 订阅飞船状态改变事件
        EventManager.Instance.OnShipStateChanged += HandleShipStateChanged;
        
        // 订阅飞船发射事件
        EventManager.Instance.OnShipLaunched += HandleShipLaunched;
    }
    
    void OnDestroy()
    {
        // 重要：取消订阅，防止内存泄漏
        EventManager.Instance.OnShipStateChanged -= HandleShipStateChanged;
        EventManager.Instance.OnShipLaunched -= HandleShipLaunched;
    }
    
    private void HandleShipStateChanged(ShipState.State oldState, ShipState.State newState, GameObject ship)
    {
        Debug.Log($"飞船状态改变: {oldState} -> {newState}");
    }
    
    private void HandleShipLaunched(Vector3 launchVelocity, GameObject ship)
    {
        Debug.Log($"飞船发射！速度: {launchVelocity}");
    }
}
```

## 可用事件列表

### 飞船事件

#### OnShipStateChanged
飞船状态改变时触发
```csharp
EventManager.Instance.OnShipStateChanged += (oldState, newState, ship) => {
    // oldState: 旧状态
    // newState: 新状态
    // ship: 飞船GameObject
};
```

#### OnShipLaunched
飞船发射时触发
```csharp
EventManager.Instance.OnShipLaunched += (launchVelocity, ship) => {
    // launchVelocity: 发射速度
    // ship: 飞船GameObject
};
```

#### OnShipCrashed
飞船碰撞时触发
```csharp
EventManager.Instance.OnShipCrashed += (collisionObject, collisionPoint, ship) => {
    // collisionObject: 碰撞的对象
    // collisionPoint: 碰撞点
    // ship: 飞船GameObject
};
```

#### OnShipCaptured
飞船被行星捕获时触发
```csharp
EventManager.Instance.OnShipCaptured += (planet, ship) => {
    // planet: 行星GameObject
    // ship: 飞船GameObject
};
```

#### OnShipReleased
飞船脱离捕获时触发
```csharp
EventManager.Instance.OnShipReleased += (planet, ship) => {
    // planet: 行星GameObject
    // ship: 飞船GameObject
};
```

#### OnShipBoosted
飞船加速时触发
```csharp
EventManager.Instance.OnShipBoosted += (boostDirection, boostForce, ship) => {
    // boostDirection: 加速方向
    // boostForce: 加速力度
    // ship: 飞船GameObject
};
```

### 行星事件

#### OnPlanetCaptureStart
行星开始捕获飞船时触发
```csharp
EventManager.Instance.OnPlanetCaptureStart += (planet, ship) => {
    // planet: 行星GameObject
    // ship: 飞船GameObject
};
```

#### OnPlanetCaptureEnd
行星结束捕获飞船时触发
```csharp
EventManager.Instance.OnPlanetCaptureEnd += (planet, ship) => {
    // planet: 行星GameObject
    // ship: 飞船GameObject
};
```

### Core物体事件

#### OnCoreDragStart
Core物体开始拖拽时触发
```csharp
EventManager.Instance.OnCoreDragStart += (core) => {
    // core: Core GameObject
};
```

#### OnCoreDrag
Core物体拖拽中时触发（每帧触发）
```csharp
EventManager.Instance.OnCoreDrag += (core, newPosition) => {
    // core: Core GameObject
    // newPosition: 新位置
};
```

#### OnCoreDragEnd
Core物体拖拽结束时触发
```csharp
EventManager.Instance.OnCoreDragEnd += (core, finalPosition) => {
    // core: Core GameObject
    // finalPosition: 最终位置
};
```

### 游戏流程事件

#### OnGameStart
游戏开始时触发
```csharp
EventManager.Instance.OnGameStart += () => {
    Debug.Log("游戏开始！");
};
```

#### OnGameEnd
游戏结束时触发
```csharp
EventManager.Instance.OnGameEnd += (isVictory) => {
    // isVictory: 是否胜利
    if (isVictory)
        Debug.Log("游戏胜利！");
    else
        Debug.Log("游戏失败！");
};
```

#### OnGameReset
游戏重置时触发
```csharp
EventManager.Instance.OnGameReset += () => {
    Debug.Log("游戏重置！");
};
```

## 完整示例

### 示例1：UI更新器

```csharp
using UnityEngine;
using UnityEngine.UI;

public class ShipStatusUI : MonoBehaviour
{
    public Text statusText;
    public Text velocityText;
    
    void Start()
    {
        // 订阅飞船状态改变事件
        EventManager.Instance.OnShipStateChanged += UpdateStatus;
        EventManager.Instance.OnShipLaunched += OnLaunch;
    }
    
    void OnDestroy()
    {
        EventManager.Instance.OnShipStateChanged -= UpdateStatus;
        EventManager.Instance.OnShipLaunched -= OnLaunch;
    }
    
    private void UpdateStatus(ShipState.State oldState, ShipState.State newState, GameObject ship)
    {
        statusText.text = $"状态: {newState}";
    }
    
    private void OnLaunch(Vector3 launchVelocity, GameObject ship)
    {
        velocityText.text = $"速度: {launchVelocity.magnitude:F2}";
    }
}
```

### 示例2：音效管理器

```csharp
using UnityEngine;

public class SoundManager : MonoBehaviour
{
    public AudioClip launchSound;
    public AudioClip crashSound;
    public AudioClip boostSound;
    
    private AudioSource audioSource;
    
    void Start()
    {
        audioSource = GetComponent<AudioSource>();
        
        EventManager.Instance.OnShipLaunched += (vel, ship) => PlaySound(launchSound);
        EventManager.Instance.OnShipCrashed += (obj, point, ship) => PlaySound(crashSound);
        EventManager.Instance.OnShipBoosted += (dir, force, ship) => PlaySound(boostSound);
    }
    
    void OnDestroy()
    {
        // 注意：使用匿名函数时无法直接取消订阅
        // 建议使用方法引用或使用 RemoveAllListeners
        EventManager.Instance.ClearAllEvents();
    }
    
    private void PlaySound(AudioClip clip)
    {
        if (clip != null && audioSource != null)
        {
            audioSource.PlayOneShot(clip);
        }
    }
}
```

### 示例3：成就系统

```csharp
using UnityEngine;

public class AchievementSystem : MonoBehaviour
{
    private int launchCount = 0;
    private int boostCount = 0;
    
    void Start()
    {
        EventManager.Instance.OnShipLaunched += OnLaunch;
        EventManager.Instance.OnShipBoosted += OnBoost;
        EventManager.Instance.OnShipCrashed += OnCrash;
    }
    
    void OnDestroy()
    {
        EventManager.Instance.OnShipLaunched -= OnLaunch;
        EventManager.Instance.OnShipBoosted -= OnBoost;
        EventManager.Instance.OnShipCrashed -= OnCrash;
    }
    
    private void OnLaunch(Vector3 vel, GameObject ship)
    {
        launchCount++;
        if (launchCount >= 10)
        {
            Debug.Log("成就解锁：发射10次！");
        }
    }
    
    private void OnBoost(Vector3 dir, float force, GameObject ship)
    {
        boostCount++;
        if (boostCount >= 50)
        {
            Debug.Log("成就解锁：加速50次！");
        }
    }
    
    private void OnCrash(GameObject obj, Vector3 point, GameObject ship)
    {
        Debug.Log("成就解锁：首次碰撞！");
    }
}
```

## 注意事项

1. **内存泄漏预防**：务必在 `OnDestroy()` 中取消订阅事件，防止内存泄漏
2. **空引用检查**：使用 `EventManager.Instance` 前检查是否为 null（虽然单例会自动创建）
3. **性能考虑**：某些事件（如 `OnCoreDrag`）每帧触发，订阅者应该避免执行耗时操作
4. **事件顺序**：事件触发顺序不保证，不要依赖事件触发的顺序
5. **线程安全**：EventManager 不是线程安全的，只能在主线程使用

## 调试功能

在 Unity Inspector 中选中 EventManager，右键点击组件，选择"显示事件订阅统计"可以查看当前所有事件的订阅数量。

## 已集成的事件

以下脚本已经自动通过 EventManager 发送事件：

- ✅ `ShipState` - 状态改变、发射
- ✅ `Crash` - 碰撞
- ✅ `ShipBoost` - 加速
- ✅ `PlanetGravityCapture` - 捕获开始/结束
- ✅ `CoreDragger` - 拖拽开始/中/结束

## 扩展事件

如果需要添加新的事件，在 `EventManager.cs` 中：

1. 定义事件：`public event Action<参数类型> OnNewEvent;`
2. 添加触发方法：`public void TriggerNewEvent(参数类型 param) { OnNewEvent?.Invoke(param); }`
3. 在相关脚本中调用触发方法

