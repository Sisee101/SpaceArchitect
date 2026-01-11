# Sphere图标系统搭建指南

本指南将详细说明如何搭建Sphere右上角图标系统，实现按下T键显示/隐藏图标的功能。

---

## 目录

1. [功能概述](#功能概述)
2. [UI结构搭建](#ui结构搭建)
3. [Icon预制体创建](#icon预制体创建)
4. [脚本配置](#脚本配置)
5. [测试验证](#测试验证)
6. [常见问题](#常见问题)

---

## 功能概述

- **功能**：按下T键时，在Sphere 1、2、4的右上角显示可点击的图标
- **特点**：
  - 图标大小固定，不受相机缩放影响
  - 图标位置始终在Sphere右上角，相对位置不变
  - 图标可以点击，触发事件

---

## UI结构搭建

### 步骤 1：创建 World Space Canvas

1. **在场景中创建Canvas**
   - 打开 `01_MainHub` 场景
   - 在 Hierarchy 中 `右键 → UI → Canvas`
   - 重命名为 `WorldSpaceCanvas`

2. **配置Canvas为World Space模式**
   - 选中 `WorldSpaceCanvas`
   - 在 Inspector 中找到 `Canvas` 组件
   - 设置 `Render Mode` 为 `World Space`

3. **设置Canvas的Transform**
   - 选中 `WorldSpaceCanvas`
   - 在 Inspector 的 `Transform` 组件中：
     ```
     Position: (0, 0, 0)
     Rotation: (0, 0, 0)
     Scale: (0.01, 0.01, 0.01)  ← 重要！World Space模式需要很小的Scale
     ```

4. **添加CanvasScaler组件（可选但推荐）**
   - 选中 `WorldSpaceCanvas`
   - `Add Component → UI → Canvas Scaler`
   - 设置 `UI Scale Mode` 为 `Constant Pixel Size`
   - `Scale Factor` 保持默认值 `1`

5. **添加GraphicRaycaster组件（用于点击检测）**
   - 选中 `WorldSpaceCanvas`
   - `Add Component → UI → Graphic Raycaster`
   - 保持默认设置即可

6. **设置Canvas的Camera引用**
   - 在 `Canvas` 组件中
   - 将 `Main Camera` 从 Hierarchy 拖到 `Event Camera` 字段（可选，但推荐）

---

### 步骤 2：创建Icon容器（可选，用于组织）

1. **创建空GameObject作为容器**
   - 选中 `WorldSpaceCanvas`
   - `右键 → Create Empty`
   - 重命名为 `IconContainer`
   - 这个容器用于组织所有图标，便于管理

2. **设置IconContainer的Transform**
   ```
   Position: (0, 0, 0)
   Rotation: (0, 0, 0)
   Scale: (1, 1, 1)
   ```

---

## Icon预制体创建

### 步骤 1：创建Icon GameObject

1. **在WorldSpaceCanvas下创建Image**
   - 选中 `WorldSpaceCanvas`（或 `IconContainer`）
   - `右键 → UI → Image`
   - 重命名为 `Icon`

2. **配置Icon的RectTransform**
   - 选中 `Icon`
   - 在 Inspector 的 `Rect Transform` 组件中：
     ```
     Anchor: Center
     Pos X: 0
     Pos Y: 0
     Width: 100
     Height: 100
     ```
   - 这些值只是初始值，实际位置会由脚本动态设置

3. **设置Icon的Image组件**
   - 在 Inspector 的 `Image` 组件中：
     - `Source Image`：拖入你的图标图片（Sprite）
     - 如果没有图片，可以暂时使用默认的白色方块
     - `Color`：可以设置为任何颜色（例如白色或彩色）
     - `Raycast Target`：**必须勾选**（用于点击检测）

4. **添加Button组件**
   - 选中 `Icon`
   - `Add Component → UI → Button`
   - 在 `Button` 组件中：
     - `Interactable`：勾选
     - `Transition`：可以选择 `Color Tint`、`Sprite Swap` 或 `Animation`
     - 如果选择 `Color Tint`：
       - `Normal Color`：正常颜色
       - `Highlighted Color`：鼠标悬停颜色
       - `Pressed Color`：按下颜色
       - `Selected Color`：选中颜色

5. **调整Button的Transition Colors（可选）**
   - 设置 `Normal Color` 为白色（或图标原始颜色）
   - 设置 `Highlighted Color` 为浅色（例如浅蓝色）
   - 设置 `Pressed Color` 为深色（例如深蓝色）
   - 这样点击时会有视觉反馈

---

### 步骤 2：制作Icon预制体

1. **创建Prefabs文件夹（如果还没有）**
   - 在 Project 窗口，`Assets` 文件夹下
   - `右键 → Create → Folder`
   - 重命名为 `Prefabs`
   - 在 `Prefabs` 下创建 `UI` 子文件夹（可选）

2. **制作预制体**
   - 在 Hierarchy 中选中 `Icon` GameObject
   - 拖到 Project 窗口的 `Assets/Prefabs/UI/` 文件夹（或你选择的文件夹）
   - 重命名为 `SphereIcon`
   - 现在 `Icon` GameObject 会变成蓝色（表示是Prefab实例）

3. **删除场景中的Icon实例（重要）**
   - 在 Hierarchy 中选中 `Icon` GameObject
   - 按 `Delete` 键删除
   - **注意**：只删除场景中的实例，不要删除Project中的预制体

---

## 脚本配置

### 步骤 1：创建SphereIconManager GameObject

1. **创建空GameObject**
   - 在 Hierarchy 中，`右键 → Create Empty`
   - 重命名为 `SphereIconManager`
   - 可以放在场景根目录或 `UIManager` 下（便于管理）

2. **添加SphereIconManager脚本**
   - 选中 `SphereIconManager`
   - `Add Component → Scripts → Sphere Icon Manager`

---

### 步骤 2：配置脚本引用

1. **配置Sphere引用**
   - 选中 `SphereIconManager`
   - 在 Inspector 的 `Sphere Icon Manager` 脚本组件中：
     - `Sphere 1`：从 Hierarchy 拖入 `Sphere` GameObject
     - `Sphere 2`：从 Hierarchy 拖入 `Sphere (1)` GameObject
     - `Sphere 4`：从 Hierarchy 拖入 `Sphere (4)` GameObject
   
   **注意**：根据你的Hierarchy，Sphere的命名可能不同，请根据实际情况选择正确的Sphere。

2. **配置UI引用**
   - `World Space Canvas`：从 Hierarchy 拖入 `WorldSpaceCanvas` GameObject
   - `Icon Prefab`：从 Project 窗口拖入 `SphereIcon` 预制体

3. **调整图标设置（可选）**
   - `Icon Offset Multiplier`：右上角偏移倍数（默认 1.2）
     - 增大此值，图标会离Sphere更远
     - 减小此值，图标会离Sphere更近
   - `Icon Fixed Scale`：图标固定大小（默认 0.01, 0.01, 0.01）
     - 如果图标太大，减小这些值
     - 如果图标太小，增大这些值

4. **调整控制设置（可选）**
   - `Toggle Key`：切换按键（默认 T）
     - 可以改为其他按键，例如 `KeyCode.F` 等

5. **调整更新设置（可选）**
   - `Update Every Frame`：是否每帧更新（默认 true）
     - 如果Sphere不会移动，可以设为 false
     - 如果设为 false，会按 `Update Interval` 间隔更新
   - `Update Interval`：更新间隔（默认 0.1 秒）

---

## 测试验证

### 步骤 1：运行游戏

1. **启动游戏**
   - 点击 Unity 的 `Play` 按钮
   - 或按 `Ctrl + P`（Windows）或 `Cmd + P`（Mac）

2. **测试T键切换**
   - 按下 `T` 键
   - 应该看到Sphere 1、2、4的右上角出现图标
   - 再次按下 `T` 键，图标应该消失

3. **测试图标位置**
   - 使用相机控制器移动/旋转/缩放相机
   - 图标应该始终保持在Sphere的右上角
   - 图标大小应该保持不变

4. **测试图标点击**
   - 点击图标
   - 查看 Console 窗口，应该看到日志：`SphereIconManager: 点击了Sphere XXX 的图标`
   - 图标应该有视觉反馈（颜色变化）

---

### 步骤 2：调整参数（如果需要）

如果图标位置或大小不合适：

1. **调整图标位置**
   - 在 `SphereIconManager` 脚本中
   - 修改 `Icon Offset Multiplier` 值
   - 例如：如果图标太近，改为 `1.5` 或 `2.0`

2. **调整图标大小**
   - 修改 `Icon Fixed Scale` 值
   - 例如：如果图标太小，改为 `(0.015, 0.015, 0.015)`
   - 如果图标太大，改为 `(0.005, 0.005, 0.005)`

---

## 常见问题

### 问题 1：图标不显示

**可能原因：**
- Icon预制体未配置
- World Space Canvas未配置
- Sphere引用未配置

**解决方法：**
1. 检查 Console 是否有错误信息
2. 确认所有引用都已正确配置
3. 确认按下了正确的按键（默认是T键）

---

### 问题 2：图标位置不正确

**可能原因：**
- `Icon Offset Multiplier` 值不合适
- Sphere的Bounds计算不正确

**解决方法：**
1. 调整 `Icon Offset Multiplier` 值
2. 确认Sphere有 `Renderer` 或 `Collider` 组件（用于计算Bounds）

---

### 问题 3：图标大小不合适

**可能原因：**
- `Icon Fixed Scale` 值不合适

**解决方法：**
1. 调整 `Icon Fixed Scale` 值
2. 增大值 = 图标变大
3. 减小值 = 图标变小

---

### 问题 4：图标点击无反应

**可能原因：**
- Icon预制体没有Button组件
- Image的 `Raycast Target` 未勾选
- World Space Canvas没有GraphicRaycaster组件

**解决方法：**
1. 确认Icon预制体有Button组件
2. 确认Image组件的 `Raycast Target` 已勾选
3. 确认World Space Canvas有GraphicRaycaster组件

---

### 问题 5：图标跟随相机移动（不应该这样）

**可能原因：**
- Canvas的Camera引用设置错误
- Canvas的Scale设置不正确

**解决方法：**
1. 确认Canvas的 `Render Mode` 是 `World Space`
2. 确认Canvas的Scale是 `(0.01, 0.01, 0.01)`
3. 确认图标位置是由脚本动态设置的

---

## 完整Hierarchy结构示例

```
01_MainHub
├── Main Camera
├── Sphere
├── Sphere (1)
├── Sphere (2)
├── Sphere (3)
├── Sphere (4)
├── Sphere (5)
├── MainHubCanvas
│   └── ...（其他UI元素）
├── WorldSpaceCanvas（新创建）
│   └── IconContainer（可选）
│       └── （运行时动态创建的Icon实例）
├── EventSystem
├── GameResourceManager
├── UIManager
└── SphereIconManager（新创建）
```

---

## 总结

完成以上步骤后，你应该能够：

1. ✅ 按下T键显示/隐藏图标
2. ✅ 图标显示在Sphere右上角
3. ✅ 图标大小固定，不受相机缩放影响
4. ✅ 图标位置跟随Sphere，相对位置不变
5. ✅ 图标可以点击，有视觉反馈

如果遇到问题，请检查：
- Console 是否有错误信息
- 所有引用是否已正确配置
- Icon预制体是否正确创建
- World Space Canvas是否正确设置

---

## 下一步（可选扩展）

如果需要扩展功能，可以在 `OnIconClicked` 方法中添加：
- 显示Sphere信息面板
- 高亮Sphere
- 执行特定操作
- 播放音效
- 等等
