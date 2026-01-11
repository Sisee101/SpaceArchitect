# 删除Button子对象操作指南

## 问题说明

如果你之前从"Create UI"菜单创建了一个独立的 Button GameObject（作为 PlanetCard 的子对象），现在需要删除它，因为代码已经改为使用 `IPointerClickHandler` 接口，不再需要 Button 组件或 Button GameObject。

## 操作步骤

### 方法 1：在场景中删除（如果预制体还没创建）

1. **在 Hierarchy 中找到 Button 子对象**：
   - 展开 `PlanetCard` GameObject
   - 找到 `Button (Legacy)` 或 `Button` 子对象

2. **删除 Button 子对象**：
   - 右键点击 `Button (Legacy)` → `Delete`（或选中后按 Delete 键）
   - 或者选中 `Button (Legacy)` 后直接按 Delete 键

3. **确保 PlanetCard 的 Image 组件配置正确**：
   - 选中 `PlanetCard` GameObject（父对象）
   - 在 Inspector 中找到 `Image` 组件
   - 确保 `Raycast Target` 已勾选（**必须**，否则点击无效）

### 方法 2：在预制体中删除（如果预制体已创建）

1. **打开预制体**：
   - 在 Project 窗口找到 `Assets/Prefeb/PlanetCard.prefab`
   - **双击**预制体文件（进入预制体编辑模式）
   - 或者选中预制体，在 Inspector 中点击 `Open Prefab` 按钮

2. **删除 Button 子对象**：
   - 在 Hierarchy 中（预制体模式下），展开 `PlanetCard`
   - 找到 `Button (Legacy)` 或 `Button` 子对象
   - 右键点击 → `Delete`（或选中后按 Delete 键）

3. **确保 PlanetCard 的 Image 组件配置正确**：
   - 选中 `PlanetCard` GameObject（父对象，预制体根对象）
   - 在 Inspector 中找到 `Image` 组件
   - 确保 `Raycast Target` 已勾选（**必须**）

4. **保存预制体**：
   - 按 `Ctrl + S` 保存
   - 或者点击 Hierarchy 窗口顶部的 `< PlanetCard` 按钮返回场景视图，Unity 会提示保存更改
   - 或者点击 `Overrides` → `Apply All`（如果有提示）

### 方法 3：如果场景中有多个测试卡片

如果场景中有多个测试卡片（每个都有 Button 子对象），需要：

1. **逐个删除**：
   - 对每个 `PlanetCard` 实例，展开它并删除 `Button` 子对象

2. **或者删除所有测试卡片**（推荐）：
   - 如果这些只是测试用的，可以直接删除所有测试 `PlanetCard` 实例
   - 因为代码会在运行时自动创建卡片（从预制体实例化）
   - 只保留预制体本身即可

## 最终检查

删除 Button 子对象后，`PlanetCard` 的结构应该是：

```
PlanetCard (GameObject)
├── Image 组件（在 PlanetCard 上）
├── LayoutElement 组件（在 PlanetCard 上）
├── PlanetCard 脚本组件（在 PlanetCard 上）
└── （没有子对象，特别是没有 Button 子对象）
```

## 重要提示

1. **不需要 Button 组件或 Button GameObject**：
   - 代码使用 `IPointerClickHandler` 接口处理点击
   - 卡片本身任何部位都可以点击（只要在 Image 范围内）

2. **必须确保 Image 的 Raycast Target 已勾选**：
   - 这是点击检测的关键
   - 如果不勾选，点击将无效

3. **点击效果**：
   - 点击卡片任意部位 → 展开介绍卡片
   - 再次点击同一卡片 → 收起介绍卡片
   - 点击其他卡片 → 移除旧的，插入新的介绍卡片

## 如果遇到问题

- **点击无效**：检查 Image 组件的 `Raycast Target` 是否已勾选
- **仍有 Button 显示**：确保已删除 Button 子对象，并且预制体已保存
- **场景中还有 Button**：如果是测试用的卡片，可以删除整个测试卡片（代码会从预制体创建）
