# Additive场景加载系统配置指南

## 概述

本指南将帮助您配置Additive场景加载系统。系统支持从MainHub通过jumpButton以Additive模式加载游戏场景，游戏场景会覆盖在MainHub上方，通过暂停弹窗的返回主界面按钮可以卸载游戏场景返回MainHub。

## 功能特性

- **Additive加载**：游戏场景以Additive模式加载，覆盖在MainHub上方
- **场景跟踪**：自动跟踪当前加载的游戏场景名称
- **场景卸载**：从暂停弹窗可以卸载游戏场景，返回MainHub
- **状态管理**：自动管理游戏状态和时间缩放
- **错误处理**：检查场景是否存在，避免重复加载

---

## 一、系统工作原理

### 1.1 场景加载流程

```
[MainHub场景运行中]
    ↓
[点击Sphere → 显示SphereInfoPanel]
    ↓
[点击jumpButton]
    ↓
[SceneTransitionManager.LoadSceneAdditive(sceneName)]
    ↓
[SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Additive)]
    ↓
[游戏场景覆盖在MainHub上方]
    ↓
[保存场景名称到currentLoadedGameScene]
```

### 1.2 场景卸载流程

```
[游戏场景运行中]
    ↓
[打开暂停弹窗]
    ↓
[点击返回主界面按钮]
    ↓
[SceneTransitionManager.UnloadGameScene()]
    ↓
[SceneManager.UnloadSceneAsync(currentLoadedGameScene)]
    ↓
[卸载游戏场景，显示MainHub]
    ↓
[清空currentLoadedGameScene]
```

---

## 二、配置步骤

### 2.1 确认场景配置

1. **确认MainHub场景**
   - MainHub场景名称应为：`01_MainHub`
   - 确保MainHub场景在Build Settings中

2. **确认游戏场景**
   - 检查订单列表（SphereOrderDataConfig）中的 `targetSceneName` 字段
   - 确保所有游戏场景都已添加到Build Settings中

3. **Build Settings配置**
   - 打开 `File → Build Settings`
   - 确认以下场景已添加：
     - `00_MainMenu`（主菜单）
     - `01_MainHub`（主界面，必须）
     - 所有游戏场景（如 `scene02`, `UITRY`, `TRY2` 等）

### 2.2 确认SphereInfoPanel配置

**SphereInfoPanel已自动配置，无需额外设置**：

- `SphereInfoPanel` 的 `jumpButton` 已自动使用Additive模式加载场景
- 场景名称从订单列表（`SphereOrderDataConfig`）的 `targetSceneName` 字段获取

**验证配置**：
1. 选中包含 `SphereInfoPanel` 的GameObject
2. 确认 `Order Data Config` 字段已配置
3. 确认订单列表中的每个订单都配置了 `targetSceneName`

### 2.3 确认PausePanel配置

**PausePanel已自动配置，无需额外设置**：

- `PausePanel` 的返回主界面按钮已自动改为卸载游戏场景

**验证配置**：
1. 在游戏场景中找到 `PausePanel` GameObject
2. 确认 `Main Menu Button` 字段已配置
3. 按钮点击事件会自动调用卸载方法

### 2.4 Canvas层级设置（重要）

**游戏场景的Canvas需要正确配置，确保显示在MainHub上方**：

#### 方法一：设置Sort Order（推荐）

1. **在游戏场景中**：
   - 找到Canvas GameObject
   - 在Inspector中找到 `Canvas` 组件
   - 设置 **Sort Order** 为一个较高的值（例如：`10` 或 `100`）
   - MainHub的Canvas Sort Order通常是 `0`，游戏场景应该更高

2. **Canvas Render Mode**：
   - 确保Canvas的 **Render Mode** 设置为 `Screen Space - Overlay` 或 `Screen Space - Camera`
   - 如果使用 `Screen Space - Camera`，确保Camera引用正确

#### 方法二：使用Screen Space - Overlay

1. **在游戏场景中**：
   - 找到Canvas GameObject
   - 设置 **Render Mode** 为 `Screen Space - Overlay`
   - 这样Canvas会自动显示在最上层

---

## 三、测试和验证

### 3.1 测试场景加载

1. **运行游戏**
   - 进入MainHub场景（`01_MainHub`）
   - 确认MainHub正常显示

2. **测试jumpButton**
   - 点击任意Sphere，显示SphereInfoPanel
   - 点击jumpButton（前往配送按钮）
   - 观察游戏场景是否加载
   - 游戏场景应该覆盖在MainHub上方

3. **验证场景状态**
   - 打开Console，应该看到：
     - "SceneTransitionManager: 使用Additive模式加载场景 XXX，覆盖在MainHub上方"
   - 游戏场景应该正常显示和运行

### 3.2 测试场景卸载

1. **在游戏场景中**
   - 打开暂停弹窗（按ESC键或点击暂停按钮）
   - 点击返回主界面按钮

2. **验证卸载**
   - 观察游戏场景是否被卸载
   - MainHub应该重新显示
   - 打开Console，应该看到：
     - "SceneTransitionManager: 已卸载游戏场景 XXX，返回MainHub"

3. **验证状态**
   - 确认时间缩放已恢复为1
   - 确认游戏状态已更新为MainHub

### 3.3 测试重复加载保护

1. **测试重复点击**
   - 在MainHub中点击jumpButton加载游戏场景
   - 再次点击jumpButton（如果可能）
   - 应该看到警告："场景 XXX 已经加载，跳过重复加载"

---

## 四、常见问题排查

### 4.1 游戏场景不显示

**可能原因**：
- Canvas的Sort Order设置不正确
- Canvas的Render Mode设置错误
- 游戏场景的Canvas被MainHub的Canvas遮挡

**解决方法**：
1. 检查游戏场景Canvas的Sort Order是否高于MainHub的Canvas
2. 尝试将游戏场景Canvas的Render Mode设置为 `Screen Space - Overlay`
3. 检查Console中是否有错误信息

### 4.2 场景加载失败

**可能原因**：
- 场景名称配置错误
- 场景未添加到Build Settings
- 场景名称拼写错误（区分大小写）

**解决方法**：
1. 检查订单列表中的 `targetSceneName` 是否正确
2. 确认场景已添加到Build Settings
3. 检查场景名称是否区分大小写（必须完全匹配）
4. 查看Console中的错误信息

### 4.3 卸载场景后MainHub不显示

**可能原因**：
- MainHub场景被意外卸载
- 场景加载顺序问题

**解决方法**：
1. 确认MainHub场景始终存在（不会被卸载）
2. 检查Console中是否有错误信息
3. 确认 `currentLoadedGameScene` 是否正确跟踪

### 4.4 暂停弹窗无法卸载场景

**可能原因**：
- SceneTransitionManager实例不存在
- 没有已加载的游戏场景
- 场景名称跟踪丢失

**解决方法**：
1. 确认场景中有SceneTransitionManager（会自动创建）
2. 检查Console中是否有警告信息
3. 确认游戏场景确实已加载（使用 `IsGameSceneLoaded()` 方法检查）

---

## 五、技术说明

### 5.1 Additive加载模式

- **LoadSceneMode.Additive**：新场景会添加到当前场景之上，不会替换当前场景
- **场景共存**：MainHub和游戏场景同时存在于内存中
- **场景激活**：新加载的场景默认是激活的

### 5.2 场景跟踪机制

- 使用静态变量 `currentLoadedGameScene` 跟踪当前加载的游戏场景名称
- 卸载时使用该名称卸载场景
- 卸载后清空引用

### 5.3 状态管理

- 加载游戏场景时，游戏状态更新为 `Playing`
- 卸载游戏场景时，游戏状态更新为 `MainHub`
- 时间缩放在加载和卸载时都会恢复为1

### 5.4 错误处理

- 检查场景是否存在（Build Settings中）
- 检查场景是否已加载（避免重复加载）
- 检查场景是否真的已加载（卸载前验证）

---

## 六、API参考

### 6.1 SceneTransitionManager新增方法

#### LoadSceneAdditive(string sceneName)
使用Additive模式加载场景，覆盖在MainHub上方。

**参数**：
- `sceneName`：场景名称（必须在Build Settings中）

**示例**：
```csharp
SceneTransitionManager.Instance.LoadSceneAdditive("scene02");
```

#### UnloadGameScene()
卸载当前加载的游戏场景，返回MainHub。

**示例**：
```csharp
SceneTransitionManager.Instance.UnloadGameScene();
```

#### IsGameSceneLoaded()
检查是否有游戏场景已加载。

**返回值**：`bool` - 是否有游戏场景已加载

**示例**：
```csharp
if (SceneTransitionManager.Instance.IsGameSceneLoaded())
{
    Debug.Log("有游戏场景已加载");
}
```

#### GetCurrentLoadedGameScene()
获取当前加载的游戏场景名称。

**返回值**：`string` - 场景名称，如果没有则返回null

**示例**：
```csharp
string sceneName = SceneTransitionManager.Instance.GetCurrentLoadedGameScene();
if (sceneName != null)
{
    Debug.Log($"当前加载的游戏场景: {sceneName}");
}
```

---

## 七、使用示例

### 7.1 在代码中加载游戏场景

```csharp
using UnityEngine;

public class MyScript : MonoBehaviour
{
    void Start()
    {
        // 使用Additive模式加载游戏场景
        if (SceneTransitionManager.Instance != null)
        {
            SceneTransitionManager.Instance.LoadSceneAdditive("scene02");
        }
    }
}
```

### 7.2 在代码中卸载游戏场景

```csharp
using UnityEngine;

public class MyScript : MonoBehaviour
{
    void OnButtonClick()
    {
        // 卸载游戏场景，返回MainHub
        if (SceneTransitionManager.Instance != null)
        {
            SceneTransitionManager.Instance.UnloadGameScene();
        }
    }
}
```

### 7.3 检查游戏场景是否已加载

```csharp
using UnityEngine;

public class MyScript : MonoBehaviour
{
    void Update()
    {
        if (SceneTransitionManager.Instance != null)
        {
            if (SceneTransitionManager.Instance.IsGameSceneLoaded())
            {
                string sceneName = SceneTransitionManager.Instance.GetCurrentLoadedGameScene();
                Debug.Log($"游戏场景 {sceneName} 已加载");
            }
        }
    }
}
```

---

## 八、完成检查清单

使用以下清单确保系统已正确配置：

### 场景配置
- [ ] MainHub场景（01_MainHub）已添加到Build Settings
- [ ] 所有游戏场景已添加到Build Settings
- [ ] 订单列表中的targetSceneName配置正确

### Canvas配置
- [ ] 游戏场景的Canvas Sort Order高于MainHub的Canvas
- [ ] 游戏场景的Canvas Render Mode设置正确
- [ ] Canvas显示在MainHub上方

### 功能测试
- [ ] 点击jumpButton，游戏场景成功加载并覆盖在MainHub上方
- [ ] 游戏场景可以正常运行
- [ ] 点击暂停弹窗的返回主界面按钮，游戏场景成功卸载
- [ ] 卸载后MainHub正常显示
- [ ] 重复加载保护正常工作

### 调试验证
- [ ] Console中显示正确的加载/卸载日志
- [ ] 没有错误或警告信息
- [ ] 场景状态跟踪正确

---

## 九、注意事项

1. **MainHub场景必须存在**
   - MainHub场景作为基础场景，不能被卸载
   - 确保MainHub场景始终在场景列表中

2. **Canvas层级**
   - 游戏场景的Canvas必须设置更高的Sort Order
   - 或者使用Screen Space - Overlay模式

3. **场景名称**
   - 场景名称必须与Build Settings中的名称完全匹配（区分大小写）
   - 订单列表中的targetSceneName必须正确配置

4. **内存管理**
   - Additive加载会同时保留多个场景在内存中
   - 确保及时卸载不需要的场景

5. **事件订阅**
   - 游戏场景中的脚本如果订阅了事件，卸载场景时会自动取消订阅
   - 不需要手动清理事件订阅

---

**配置完成！现在您可以使用Additive模式加载游戏场景，并通过暂停弹窗返回MainHub了。**
