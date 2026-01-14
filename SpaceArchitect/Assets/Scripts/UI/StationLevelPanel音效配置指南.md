# StationLevelPanel 音效配置指南

## 概述

`StationLevelPanelSoundManager` 是一个低耦合的音效管理系统，专门为 `StationLevelPanel` 设计。它通过配置方式管理升级按键和确认键的音效，完全独立于面板的业务逻辑。

## 设计特点

- **低耦合**：音效管理器与面板逻辑完全分离，可以独立添加/移除
- **易配置**：通过 Inspector 可视化配置，无需修改代码
- **自动绑定**：自动查找和绑定按钮，支持手动配置和自动查找
- **灵活扩展**：运行时可以动态添加/移除按钮映射

## 快速开始

### 步骤 1：添加组件

1. 在 Unity 编辑器中，找到 `StationLevelPanel` GameObject
2. 选中 `StationLevelPanel`
3. 点击 `Add Component`
4. 搜索并添加 `Station Level Panel Sound Manager` 组件

### 步骤 2：配置音效文件

在 `Station Level Panel Sound Manager` 组件的 Inspector 中：

1. **Upgrade Button Sound**
   - 拖入升级按键的音效文件（AudioClip）
   - 所有升级按键共用这个音效

2. **Confirm Button Sound**
   - 拖入确认键的音效文件（AudioClip）
   - 确认键使用这个音效

### 步骤 3：配置按钮映射

在 `Button Mappings` 列表中，为每个按钮添加映射：

1. 点击 `+` 添加新条目
2. 为每个条目配置：
   - **Button**：拖入对应的 Button 组件（推荐）
   - **Button Name**：如果 Button 为空，可以填写按钮名称（用于自动查找）
   - **Sound Type**：选择音效类型
     - `Upgrade Button`：升级按键音效
     - `Confirm Button`：确认键音效

### 步骤 4：配置音频源（可选）

- **Audio Source**：如果为空，会自动查找或创建
  - 优先从当前 GameObject 获取
  - 其次从父对象（StationLevelPanel）获取
  - 如果都没有，自动创建一个

### 步骤 5：其他设置

- **Auto Find Buttons**：是否自动查找未配置的按钮（默认开启）
  - 开启后，会自动查找面板中的所有按钮并添加到映射列表
  - 根据按钮名称自动推测音效类型

- **Enable Debug Log**：是否启用调试日志（测试时建议开启）

## 配置示例

### 示例 1：手动配置按钮

假设 `StationLevelPanel` 有 3 个升级按键和 1 个确认键：

```
StationLevelPanel
├── UpgradeButton1 (Button)
├── UpgradeButton2 (Button)
├── UpgradeButton3 (Button)
└── ConfirmButton (Button)
```

**配置步骤：**

1. 添加 `StationLevelPanelSoundManager` 组件
2. 配置音效文件：
   - `Upgrade Button Sound`: `upgrade_sound.ogg`
   - `Confirm Button Sound`: `confirm_sound.ogg`
3. 在 `Button Mappings` 中添加 4 个条目：
   - 条目 1：Button = `UpgradeButton1`, Sound Type = `Upgrade Button`
   - 条目 2：Button = `UpgradeButton2`, Sound Type = `Upgrade Button`
   - 条目 3：Button = `UpgradeButton3`, Sound Type = `Upgrade Button`
   - 条目 4：Button = `ConfirmButton`, Sound Type = `Confirm Button`

### 示例 2：使用自动查找

如果启用 `Auto Find Buttons`：

1. 添加组件并配置音效文件
2. 运行场景，组件会自动：
   - 查找所有按钮
   - 根据按钮名称推测音效类型
   - 添加到映射列表

**按钮名称推测规则：**
- 包含 "confirm"、"ok"、"apply"、"submit"、"确定"、"确认" → `Confirm Button`
- 其他 → `Upgrade Button`

## 运行时 API

### 添加按钮映射

```csharp
// 获取音效管理器
StationLevelPanelSoundManager soundManager = GetComponent<StationLevelPanelSoundManager>();

// 添加新按钮
Button newButton = ...; // 获取按钮引用
soundManager.AddButtonMapping(newButton, StationLevelPanelSoundManager.SoundType.UpgradeButton);
```

### 移除按钮映射

```csharp
soundManager.RemoveButtonMapping(buttonToRemove);
```

### 动态修改音效

```csharp
// 修改升级按键音效
soundManager.SetUpgradeButtonSound(newUpgradeSound);

// 修改确认键音效
soundManager.SetConfirmButtonSound(newConfirmSound);
```

## 测试功能

组件提供了两个测试方法（在 Inspector 中右键点击组件可以看到）：

1. **测试播放升级按键音效**：测试升级按键音效是否正常
2. **测试播放确认键音效**：测试确认键音效是否正常

## 工作原理

### 绑定流程

```
面板激活 (OnEnable)
  ↓
初始化音频源
  ↓
自动查找按钮（如果启用）
  ↓
遍历按钮映射列表
  ↓
为每个按钮绑定点击事件
  ↓
点击时播放对应音效
```

### 音效播放流程

```
按钮点击
  ↓
触发 OnButtonClicked(soundType)
  ↓
根据 soundType 选择音效文件
  ↓
AudioSource.PlayOneShot(clip)
```

## 注意事项

### 1. 按钮引用 vs 按钮名称

- **推荐**：直接拖入 Button 引用（更可靠）
- **备用**：填写按钮名称（用于自动查找）
- 如果两者都为空，该按钮会被跳过

### 2. 返回按钮

自动查找功能会跳过名称包含 "back" 或 "return" 的按钮，因为返回按钮通常由面板自己管理。

### 3. 重复绑定

组件会自动防止重复绑定同一个按钮，使用 `HashSet<Button>` 跟踪已绑定的按钮。

### 4. 事件解绑

由于使用了匿名函数，无法精确解绑。但按钮销毁时会自动清理，不会造成内存泄漏。

### 5. 面板生命周期

- `OnEnable()`：绑定按钮事件
- `OnDisable()`：标记为未绑定（实际解绑在按钮销毁时）

## 常见问题

### Q1: 音效没有播放

**检查清单：**
1. 音效文件是否已配置？
2. AudioSource 是否存在？
3. 按钮是否已添加到映射列表？
4. 按钮的 `Sound Type` 是否正确？
5. 查看 Console 日志（启用 `Enable Debug Log`）

### Q2: 按钮没有自动找到

**可能原因：**
- `Auto Find Buttons` 未启用
- 按钮名称不符合推测规则
- 按钮在面板激活后才创建

**解决方法：**
- 手动添加按钮映射
- 或调用 `AddButtonMapping()` 方法

### Q3: 如何为动态创建的按钮添加音效？

**解决方法：**
```csharp
// 创建按钮后
Button dynamicButton = Instantiate(buttonPrefab, ...);

// 添加到音效管理器
StationLevelPanelSoundManager soundManager = GetComponent<StationLevelPanelSoundManager>();
soundManager.AddButtonMapping(dynamicButton, StationLevelPanelSoundManager.SoundType.UpgradeButton);
```

### Q4: 可以禁用某个按钮的音效吗？

**解决方法：**
- 从 `Button Mappings` 列表中移除该按钮
- 或调用 `RemoveButtonMapping()` 方法

## 与 StationLevelPanel 的耦合度

### 零耦合设计

- `StationLevelPanel` **不需要**知道音效管理器的存在
- `StationLevelPanel` **不需要**修改任何代码
- 音效管理器可以独立添加/移除，不影响面板功能

### 如何实现零耦合？

1. **通过引用而非代码**：音效管理器通过 Inspector 配置按钮引用
2. **自动查找**：如果引用为空，通过名称自动查找
3. **独立组件**：音效管理器是独立的组件，不依赖面板的内部实现

## 扩展建议

如果需要为其他面板添加类似的音效系统：

1. **复制组件**：复制 `StationLevelPanelSoundManager.cs`
2. **重命名**：改为对应的面板名称（如 `MainHubPanelSoundManager`）
3. **调整查找逻辑**：根据新面板的结构调整自动查找逻辑

或者，可以创建一个通用的 `PanelSoundManager` 基类，让各个面板的音效管理器继承它。

## 总结

`StationLevelPanelSoundManager` 提供了一个低耦合、易配置的音效管理方案。通过简单的 Inspector 配置，就可以为 `StationLevelPanel` 的所有按钮添加音效，无需修改面板的业务逻辑代码。
