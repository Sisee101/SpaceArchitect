# PlanetUnlockManager 配置指南

## 📋 问题说明

你遇到的问题：
- **01_MainHub** 场景中图鉴都没解锁
- 点击 **settlement面板上的nextdaybutton** 后，**02_MainHub** 中涉及的星球全部解锁了

## 🔍 问题根源

1. **01_MainHub场景**：没有 `PlanetUnlockManager` 实例
2. **02_MainHub场景**：有一个 `PlanetUnlockManager` 实例（名称拼写错误：`PlanetUnlockManger`）
3. **PlayerPrefs数据**：可能之前测试时保存了所有解锁状态

当从 `01_MainHub` 切换到 `02_MainHub` 时：
- `02_MainHub` 中的 `PlanetUnlockManager` 会在 `Awake()` 时加载 `PlayerPrefs` 中的解锁状态
- 如果 `PlayerPrefs` 中已保存所有解锁状态，就会显示所有星球已解锁

## ✅ 解决方案

### 步骤1：在01_MainHub场景中添加PlanetUnlockManager

1. 打开 `01_MainHub` 场景
2. 在 Hierarchy 中创建一个空 GameObject：
   - 右键点击 Hierarchy → `Create Empty`
   - 命名为 `PlanetUnlockManager`（注意拼写，不是 `PlanetUnlockManger`）
3. 添加 `PlanetUnlockManager` 组件：
   - 选中刚创建的 GameObject
   - 在 Inspector 中点击 `Add Component`
   - 搜索并添加 `PlanetUnlockManager` 脚本

### 步骤2：修正02_MainHub场景中的拼写错误

1. 打开 `02_MainHub` 场景
2. 找到名为 `PlanetUnlockManger` 的 GameObject（拼写错误）
3. 将其重命名为 `PlanetUnlockManager`（正确拼写）

### 步骤3：清理PlayerPrefs数据（重置解锁状态）

有两种方法：

#### 方法1：在Unity编辑器中重置（推荐）

1. 在 `01_MainHub` 或 `02_MainHub` 场景中运行游戏
2. 在 Hierarchy 中找到 `PlanetUnlockManager` GameObject
3. 在 Inspector 中，右键点击 `PlanetUnlockManager` 组件
4. 选择 `重置所有解锁状态`（Context Menu）
5. 这会清除所有 PlayerPrefs 中的解锁数据

#### 方法2：通过代码重置

在 Unity 编辑器中，打开 Console 窗口，运行以下代码：
```csharp
PlanetUnlockManager.ResetAllUnlocks();
```

### 步骤4：配置Inspector显示（可选，用于调试）

`PlanetUnlockManager` 现在支持在 Inspector 中查看和编辑解锁状态：

1. 选中 `PlanetUnlockManager` GameObject
2. 在 Inspector 中，你会看到：
   - **行星解锁状态（Inspector显示）**：13个布尔值，显示当前解锁状态
   - **调试选项**：
     - `Auto Sync To Inspector`：是否自动同步到Inspector显示（默认开启）

## 🛠️ Inspector功能说明

### 自动同步（Auto Sync To Inspector）

- **开启**（默认）：每次解锁/重置时，Inspector中的显示会自动更新
- **关闭**：Inspector显示不会自动更新，需要手动刷新

### Context Menu 功能

在 Inspector 中右键点击 `PlanetUnlockManager` 组件，可以使用以下功能：

1. **显示解锁状态**：在 Console 中打印所有行星的解锁状态
2. **重置所有解锁状态**：清除所有解锁状态并保存到 PlayerPrefs
3. **从Inspector同步到运行时（覆盖PlayerPrefs）**：⚠️ **谨慎使用**
   - 将 Inspector 中的值同步到运行时状态
   - **会覆盖 PlayerPrefs 中的数据**
   - 用于测试时快速设置解锁状态
4. **刷新Inspector显示**：手动刷新 Inspector 中的显示（当 Auto Sync 关闭时使用）

## 📝 使用建议

### 正常游戏流程

1. **首次运行**：所有行星都是未解锁状态
2. **解锁行星**：通过游戏逻辑调用 `PlanetUnlockManager.UnlockPlanet(index)` 解锁
3. **状态持久化**：解锁状态自动保存到 PlayerPrefs
4. **场景切换**：`PlanetUnlockManager` 使用 `DontDestroyOnLoad`，状态在场景切换时保持

### 调试和测试

1. **查看解锁状态**：
   - 在 Inspector 中查看 `planetUnlockStates` 数组
   - 或使用 Context Menu → `显示解锁状态`

2. **测试解锁功能**：
   - 使用 `PlanetUnlockButton` 组件（如果场景中有）
   - 或通过代码：`PlanetUnlockManager.UnlockPlanet(0)` （解锁第0个行星）

3. **重置测试数据**：
   - 使用 Context Menu → `重置所有解锁状态`

4. **快速设置测试状态**：
   - 在 Inspector 中手动勾选 `planetUnlockStates` 数组中的值
   - 使用 Context Menu → `从Inspector同步到运行时（覆盖PlayerPrefs）`

## ⚠️ 注意事项

1. **Inspector中的值仅用于显示和调试**：
   - 运行时以 `PlayerPrefs` 中的数据为准
   - Inspector 中的值不会自动保存到 PlayerPrefs（除非使用同步功能）

2. **场景配置**：
   - 建议在 `01_MainHub` 和 `02_MainHub` 场景中都添加 `PlanetUnlockManager`
   - 虽然 `PlanetUnlockManager` 使用 `DontDestroyOnLoad`，但添加实例可以避免场景切换时的初始化问题

3. **PlayerPrefs 清理**：
   - 如果遇到解锁状态异常，先尝试重置所有解锁状态
   - PlayerPrefs 数据存储在系统注册表（Windows）或 plist 文件（Mac）中

## 🔧 故障排除

### 问题：Inspector中显示的状态与游戏中的不一致

**解决方案**：
1. 检查 `Auto Sync To Inspector` 是否开启
2. 如果关闭，使用 Context Menu → `刷新Inspector显示`
3. 确认 PlayerPrefs 中的数据是否正确（使用 `显示解锁状态` 查看）

### 问题：切换场景后所有星球都解锁了

**解决方案**：
1. 检查 PlayerPrefs 中是否保存了所有解锁状态
2. 使用 Context Menu → `重置所有解锁状态` 清除数据
3. 确认场景中只有一个 `PlanetUnlockManager` 实例

### 问题：解锁状态没有保存

**解决方案**：
1. 确认 `PlanetUnlockManager` 实例存在且已初始化
2. 检查 `UnlockPlanet()` 方法是否被正确调用
3. 查看 Console 中的日志，确认是否有错误信息

## 📚 相关文件

- `PlanetUnlockManager.cs`：行星解锁管理器脚本
- `PlanetUnlockButton.cs`：解锁按钮脚本（可选）
- `PlanetEncyclopediaPanel.cs`：行星图鉴面板（使用解锁状态）
