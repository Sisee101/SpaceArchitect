# MoneyManager 打包后金钱不增加问题 - 修复报告

## 问题描述
在Unity编辑器中运行游戏时，完成订单（如ID为1的订单）可以正常增加金钱，但打包（Build）后完成订单时金钱不增加。

## 根本原因分析

### 核心问题：`Resources.FindObjectsOfTypeAll` 在打包后行为不同

**问题代码位置**：`Assets/Scenes/SkillTestScene/MoneyManager.cs` 第76行

```csharp
TaskManager[] allTaskManagers = Resources.FindObjectsOfTypeAll<TaskManager>();
```

**问题原因**：

1. **在Unity编辑器中**：
   - `Resources.FindObjectsOfTypeAll<T>()` 可以找到场景中的所有对象（包括激活和未激活的）
   - 可以找到预制体资源中的对象
   - 可以找到所有场景中的对象

2. **在打包后的游戏中**：
   - `Resources.FindObjectsOfTypeAll<T>()` **只能找到Resources文件夹中的资源**
   - **无法找到场景中的对象**
   - 这是Unity的设计限制，用于安全和性能考虑

### 问题链条

```
打包后运行
  ↓
Resources.FindObjectsOfTypeAll<TaskManager>() 返回空数组
  ↓
taskManager 引用为 null
  ↓
SubscribeToTaskManager() 无法订阅事件
  ↓
TaskManager.OnTaskCompleted 事件触发时，MoneyManager没有监听
  ↓
OnTaskCompleted() 回调不会被调用
  ↓
金钱不会增加
```

## 修复方案

### 1. 替换对象查找方法

**修改前**（有问题）：
```csharp
TaskManager[] allTaskManagers = Resources.FindObjectsOfTypeAll<TaskManager>();
```

**修改后**（正确）：
```csharp
// 方法1：优先使用FindObjectOfType（在Build中可靠）
taskManager = FindObjectOfType<TaskManager>();

// 方法2：如果方法1失败，使用FindObjectsOfType查找所有（包括未激活的）
if (taskManager == null)
{
    TaskManager[] allTaskManagers = FindObjectsOfType<TaskManager>(true);
    if (allTaskManagers != null && allTaskManagers.Length > 0)
    {
        taskManager = allTaskManagers[0];
    }
}
```

**关键区别**：
- `FindObjectOfType<T>()` - 查找场景中的对象（编辑器和打包后都可用）
- `FindObjectsOfType<T>(true)` - 查找场景中的所有对象，包括未激活的（Unity 2020.1+）
- `Resources.FindObjectsOfTypeAll<T>()` - 只在编辑器中可靠，打包后只能找到Resources文件夹中的资源

### 2. 添加重试机制

由于Unity对象的初始化顺序可能不确定，添加了延迟重试机制：

```csharp
[Tooltip("如果自动查找失败，重试次数")]
[SerializeField] private int retryCount = 3;

[Tooltip("重试间隔（秒）")]
[SerializeField] private float retryInterval = 0.5f;

private IEnumerator RetryFindReferences()
{
    for (int i = 0; i < retryCount; i++)
    {
        yield return new WaitForSeconds(retryInterval);
        
        if (taskManager == null)
        {
            FindMissingReferences();
            
            if (taskManager != null)
            {
                SubscribeToTaskManager();
                yield break;
            }
        }
    }
}
```

### 3. 添加DontDestroyOnLoad支持

为了在场景切换时保持MoneyManager实例：

```csharp
[Header("持久化设置")]
[Tooltip("是否使MoneyManager在场景切换时不被销毁（DontDestroyOnLoad）")]
[SerializeField] private bool persistAcrossScenes = false;

private static MoneyManager instance;

void Awake()
{
    if (persistAcrossScenes)
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else if (instance != this)
        {
            Destroy(gameObject);
            return;
        }
    }
}
```

### 4. 增强调试日志

添加了详细的调试日志，便于排查问题：

```csharp
// 在关键位置添加日志
Debug.Log($"MoneyManager: Start() 开始执行，当前Money值: {money}");
Debug.LogError("MoneyManager: 【严重错误】未找到TaskManager！...");
Debug.Log($"<color=green>MoneyManager: 任务完成！金钱已增加</color>");
```

添加了状态检查方法：
```csharp
public void LogStatus()
{
    Debug.Log("━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━");
    Debug.Log("MoneyManager 状态报告:");
    Debug.Log($"  - 当前Money值: {money}");
    Debug.Log($"  - TaskManager引用: {(taskManager != null ? "已配置" : "null【错误】")}");
    // ... 更多状态信息
}
```

## 使用建议

### 方案A：在Inspector中手动指定引用（推荐）

这是最可靠的方法：

1. 在Unity编辑器中选择MoneyManager对象
2. 在Inspector面板中找到"引用配置"部分
3. 将场景中的TaskManager对象拖拽到"Task Manager"字段
4. 将SphereOrderDataConfig资源拖拽到"Order Data Config"字段

**优点**：
- 100%可靠，不依赖自动查找
- 在编辑器和打包后都能正常工作
- 性能最好（不需要运行时查找）

### 方案B：使用自动查找（已修复）

如果不想手动指定引用，可以使用修复后的自动查找功能：

1. 确保"Auto Find On Start"勾选（默认为true）
2. 可以调整"Retry Count"和"Retry Interval"参数
3. 启用"Enable Debug Log"查看查找过程

**优点**：
- 方便，不需要手动配置
- 适合快速原型开发

**缺点**：
- 依赖运行时查找，有轻微性能开销
- 如果场景中有多个TaskManager，可能找到错误的实例

### 方案C：使用DontDestroyOnLoad（高级选项，需谨慎）

⚠️ **重要提示**：如果你的项目中每个MainHub场景都配置了MoneyManager，**不要启用此选项**！

#### 何时使用DontDestroyOnLoad？

**适用场景**：
- ✅ 只有一个初始场景有MoneyManager
- ✅ 其他场景不需要MoneyManager组件
- ✅ 需要在所有场景中保持同一个MoneyManager实例

**不适用场景**：
- ❌ 每个MainHub场景都有自己的MoneyManager（你的情况）
- ❌ 不同场景需要不同的MoneyManager配置
- ❌ MoneyManager需要绑定场景特定的UI组件

#### 如果启用DontDestroyOnLoad：

1. 勾选"Persist Across Scenes"
2. **只在第一个场景中创建MoneyManager**
3. **删除其他场景中的MoneyManager**
4. 系统会自动在场景切换时重新查找引用

**注意事项**：
- 场景切换后会自动重新查找TaskManager和Text组件
- TextMeshProUGUI组件需要在MoneyManager对象上或其子对象上
- 如果新场景没有TaskManager，金钱功能将不可用

#### 推荐配置（针对你的项目）：

**保持 `persistAcrossScenes = false`（默认值）**

这样：
- ✅ 每个MainHub场景可以有自己的MoneyManager
- ✅ 每个场景的配置相互独立
- ✅ 不会出现引用冲突
- ✅ 符合你当前的项目结构

## 测试建议

### 1. 编辑器测试
1. 运行游戏
2. 打开Console窗口
3. 查找"MoneyManager: Start() 开始执行"日志
4. 确认"TaskManager引用"不为null
5. 完成一个订单，查看是否有"任务完成！金钱已增加"日志

### 2. 打包测试
1. 打包游戏（Build）
2. 运行打包后的可执行文件
3. 查看日志文件（通常在`游戏名_Data/output_log.txt`或`Player.log`）
4. 搜索"MoneyManager"关键字
5. 确认没有"【严重错误】"日志

### 3. 调试方法

如果问题仍然存在，可以使用以下方法调试：

```csharp
// 在任意MonoBehaviour的Start或Update中调用
MoneyManager moneyManager = FindObjectOfType<MoneyManager>();
if (moneyManager != null)
{
    moneyManager.LogStatus(); // 输出状态报告
}

// 手动测试任务完成
moneyManager.TestTaskCompleted(1); // 测试ID为1的任务
```

## 其他可能的问题

### 1. TaskManager未触发事件

检查TaskManager是否正确触发了`OnTaskCompleted`事件：

```csharp
// 在TaskManager.CompleteTask方法中
Debug.Log($"TaskManager: 触发OnTaskCompleted事件，taskId={taskId}");
OnTaskCompleted?.Invoke(taskId);
```

### 2. SphereOrderDataConfig配置错误

确认订单配置正确：
- taskId是否正确（不是-1）
- orderAmount是否大于0
- 订单数据是否在Resources文件夹中或已在Inspector中指定

### 3. 场景切换问题

如果在场景切换后金钱不增加：
- 检查MoneyManager是否被销毁
- 检查TaskManager引用是否丢失
- 考虑使用DontDestroyOnLoad或在新场景中重新订阅事件

## 总结

**核心问题**：使用了`Resources.FindObjectsOfTypeAll`在打包后无法找到场景对象

**核心解决方案**：
1. 使用`FindObjectOfType`或`FindObjectsOfType`替代
2. 在Inspector中手动指定引用（最推荐）
3. 添加重试机制和详细日志

**预期效果**：
- 编辑器和打包后都能正常增加金钱
- 有详细的日志便于排查问题
- 支持场景切换（可选）

## 修改文件列表

- `Assets/Scenes/SkillTestScene/MoneyManager.cs` - 已修复

## 版本信息

- 修复日期：2026-01-17
- Unity版本：通用（2019.4+）
- 修复人：AI Assistant

