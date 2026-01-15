# 01_MainHub 场景索引

## 场景路径
`Assets/UI/Scenes/01_MainHub.unity`

## 场景用途
MainHub UI 场景，用于显示主界面和订单管理

## 主要 GameObject 列表

### 1. **SphereClickHandler**
- **组件**: `SphereClickHandler` (GUID: 3bc85e1703156274ea11ffa444425a09)
- **功能**: 处理球体点击交互
- **配置**: 
  - orderDataConfig
  - infoPanel
  - raycastCamera

### 2. **Main Camera**
- **类型**: Camera
- **功能**: 主摄像机

### 3. **GameResourceManager**
- **功能**: 游戏资源管理

### 4. **WorldSpaceCanvas**
- **类型**: Canvas
- **功能**: 世界空间 UI Canvas

### 5. **VictoryImageDisplay**
- **功能**: 胜利图片显示

### 6. **MainHubCanvas**
- **类型**: Canvas
- **功能**: MainHub 主 UI Canvas

### 7. **IconContainer**
- **功能**: 图标容器

### 8. **SphereIconManager**
- **组件**: `SphereIconManager`
- **功能**: 管理球体图标

### 9. **UIManager**
- **组件**: `UIManager`
- **功能**: UI 管理器

### 10. **TaskCompletionHandler**
- **组件**: `TaskCompletionHandler`
- **功能**: 任务完成处理

### 11. **TaskManager**
- **组件**: `TaskManager`
- **功能**: 任务管理器

### 12. **EventSystem**
- **类型**: EventSystem
- **功能**: Unity UI 事件系统

## ⚠️ 潜在冲突组件

### 单例管理器（可能冲突）

#### **MainHub 场景中：**
- ✅ **有** `UIManager` - **单例，但不使用 DontDestroyOnLoad** ⚠️
- ❌ **没有** `EventManager` 组件
- ❌ **没有** `GameRestartManager` 组件
- ❌ **没有** `EventListener` 组件
- ❌ **没有** `GameManager` GameObject

#### **关卡场景中（level1-level13）：**
- ❌ **没有** `UIManager` 组件
- ✅ **有** `GameManager` GameObject，包含：
  - `EventListener` (GUID: a77b0fff2a0124a46838c602ee15bcde)
  - `EventManager` (GUID: 69c190af5eb711340943fa331a1e4d06) - **单例，使用 DontDestroyOnLoad**
  - `GameRestartManager` (GUID: 4f72a2cef2514e049a8463caf05638da) - **单例，使用 DontDestroyOnLoad**

## 🔍 冲突分析

### 问题原因
当 MainHub 场景和关卡场景同时以 **Additive** 模式加载时：

1. **UIManager 冲突** ⚠️ **最可能的原因**：
   - UIManager 是单例，但**不使用** `DontDestroyOnLoad`
   - MainHub 场景中有 UIManager，关卡场景中没有
   - 当两个场景同时加载时：
     - 如果 MainHub 先加载，UIManager 会被创建
     - 如果关卡场景后加载，可能会查找 UIManager，找到 MainHub 中的实例
     - 但如果场景卸载顺序不对，可能导致 UIManager 被意外销毁
   - **解决方案**：确保 UIManager 在正确的场景中，或者添加 `DontDestroyOnLoad`

2. **EventManager 冲突**：
   - EventManager 是单例，使用 `DontDestroyOnLoad`
   - 如果两个场景都有 EventManager，会导致：
     - 重复实例被销毁（Awake 中的单例检查）
     - 但可能已经订阅了事件，导致事件系统混乱
   - **当前状态**：MainHub 场景中没有 EventManager，所以不会冲突

3. **GameRestartManager 冲突**：
   - GameRestartManager 也是单例，使用 `DontDestroyOnLoad`
   - 如果两个场景都有，会导致：
     - 重复实例被销毁
     - 但可能已经初始化了飞船引用，导致引用丢失
   - **当前状态**：MainHub 场景中没有 GameRestartManager，所以不会冲突

4. **EventListener 冲突**：
   - EventListener 不是单例，但如果有多个实例：
     - 会导致重复的事件日志输出
     - 可能影响性能
   - **当前状态**：MainHub 场景中没有 EventListener，所以不会冲突

### 当前状态
根据检查，**MainHub 场景中没有这些单例组件**，所以理论上不应该有冲突。

但如果仍然出现冲突，可能的原因：
1. 场景加载顺序问题
2. 其他单例组件（如 UIManager、TaskManager）可能也是单例
3. 场景加载时的事件订阅/取消订阅时机问题

## 🔧 建议解决方案

### 方案 1：修复 UIManager 单例问题 ⚠️ **推荐**
**问题**：UIManager 是单例但不使用 `DontDestroyOnLoad`，在 Additive 加载时可能被销毁

**解决方案**：
1. **选项 A**：给 UIManager 添加 `DontDestroyOnLoad`（如果它需要在场景切换时保留）
2. **选项 B**：确保 UIManager 只在 MainHub 场景中，关卡场景不依赖它
3. **选项 C**：修改 UIManager 的单例逻辑，在 Additive 加载时正确处理

### 方案 2：确保单例只在一个场景中
- ✅ **当前状态**：MainHub 场景不包含 EventManager、GameRestartManager
- 关卡场景中的单例会通过 `DontDestroyOnLoad` 保留，即使 MainHub 场景加载也不会冲突

### 方案 3：检查其他可能的单例
检查以下组件是否也是单例：
- ✅ `UIManager` - **已确认是单例，但不使用 DontDestroyOnLoad** ⚠️
- `TaskManager` - 需要检查
- `GameResourceManager` - 需要检查
- `SphereIconManager` - 需要检查

### 方案 4：场景加载顺序
确保加载顺序：
1. 先加载关卡场景（包含单例管理器）
2. 再以 Additive 模式加载 MainHub 场景

### 方案 5：单例初始化检查
在单例的 `Awake` 中加强检查，确保在 Additive 加载时正确处理重复实例

## 📝 需要进一步检查的组件

1. ✅ **UIManager** - **已确认是单例，但不使用 DontDestroyOnLoad** ⚠️ **可能是冲突原因**
2. **TaskManager** - 需要检查是否是单例
3. **GameResourceManager** - 需要检查是否是单例
4. **SceneTransitionManager** - 检查场景加载逻辑和顺序

## 🔗 相关文件

- 关卡场景：`Assets/level/level*.unity`
- EventManager：`Assets/Scripts/Core/EventManager.cs`
- GameRestartManager：`Assets/Scripts/Core/GameManager.cs`（实际上是 GameRestartManager）
- EventListener：`Assets/Scripts/Core/EventListener.cs`
- UIManager：`Assets/Scripts/UI/UIManager.cs` ⚠️ **可能是冲突原因**

## 🎯 冲突原因分析

### **1. 相机冲突** ⚠️ **已修复**

**问题描述**：
- MainHub 场景中有 "Main Camera"（Tag: MainCamera）
- 关卡场景中也有 "Main Camera"（Tag: MainCamera）
- 当两个场景同时以 Additive 模式加载时：
  - Unity 不允许有多个 MainCamera Tag 的相机同时启用
  - 会导致相机冲突，可能影响渲染

**修复方案**：
- ✅ 在 `SceneTransitionManager.LoadGameSceneAdditive()` 中：
  1. 先禁用 MainHub 场景的相机
  2. 等待一帧确保禁用生效
  3. 再加载关卡场景
  4. 场景加载完成后，确保关卡场景的相机是激活的

### **2. EventManager 单例冲突** ⚠️ **需要检查**

**问题描述**：
- MainHub 场景中**没有** EventManager
- 关卡场景中有 EventManager（在 GameManager GameObject 上）
- 但如果 MainHub 场景中的某些组件在 `Start/Awake` 中访问 `EventManager.Instance`：
  - 可能会在关卡场景加载前就创建了一个 EventManager 实例
  - 当关卡场景加载时，GameManager 中的 EventManager 会检测到重复实例并销毁自己
  - 导致关卡场景中的 EventManager 无法正常工作

**需要检查**：
- MainHub 场景中的组件是否在访问 `EventManager.Instance`
- 如果有，需要确保在关卡场景加载后再访问，或者延迟初始化

### **3. UIManager 单例问题**

**问题描述**：
- MainHub 场景中有 `UIManager`（单例，但不使用 `DontDestroyOnLoad`）
- 关卡场景中没有 `UIManager`
- 当使用 Additive 模式加载关卡场景时：
  - MainHub 场景仍然存在（没有被卸载）
  - MainHub 中的 UIManager 仍然存在
  - 如果关卡场景中的代码尝试访问 UIManager，会找到 MainHub 中的实例
  - 但如果场景卸载顺序不当，可能导致 UIManager 被意外销毁

**场景加载流程**（根据 SceneTransitionManager）：
1. MainHub 场景已加载（包含 UIManager）
2. 调用 `LoadGameSceneAdditive(sceneName)` 加载关卡场景
3. 关卡场景以 Additive 模式加载（包含 GameManager）
4. 两个场景同时存在

### **解决方案**

#### ✅ **已修复：相机冲突**
- 修改了 `SceneTransitionManager.LoadGameSceneAdditive()`：
  - 使用协程延迟加载场景
  - 确保在加载关卡场景前，MainHub 的相机已被禁用
  - 场景加载完成后，确保关卡场景的相机是激活的

#### ✅ **已修复：EventManager 冲突**
- 修改了 `EventManager.Instance` 的 getter：
  - 优先查找关卡场景中的 EventManager
  - 如果 MainHub 场景中的组件访问 EventManager，会优先使用关卡场景中的实例
  - 在 MainHub 场景中不会创建新的 EventManager 实例（等待关卡场景加载）

**MainHub 场景中访问 EventManager 的组件**：
- `PlanetUnlockManager` - 触发行星解锁事件
- `PlanetCard` - 订阅行星解锁事件
- `GlobalOverviewUI` - 订阅飞船成功事件
- `ReadyButton` - 订阅飞船状态改变事件
- `MailPanel` - 订阅按键事件
- `MainHubController` - 触发按键事件

这些组件现在会优先使用关卡场景中的 EventManager，不会在 MainHub 场景中创建新实例。

#### **UIManager 单例问题**
1. **给 UIManager 添加 DontDestroyOnLoad**（如果它需要在场景切换时保留）：
   ```csharp
   void Awake()
   {
       if (_instance == null)
       {
           _instance = this;
           DontDestroyOnLoad(gameObject); // 添加这行
       }
       // ...
   }
   ```

2. **或者确保 UIManager 只在 MainHub 场景中**，关卡场景不依赖它

