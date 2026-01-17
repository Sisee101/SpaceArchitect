# Build 问题排查指南

## 问题一：Build 后 GlobalOverviewCamera 的 UI 画面不见了

### 问题描述
- Build 后，Main Camera 正常显示
- 但 GlobalOverviewCamera 的 UI 画面（左上角的小视窗）不见了
- 场景中有 GameCanvas，GlobalOverviewUI 会使用它

### 根本原因分析

**代码逻辑：**
1. `GlobalOverviewUI` 在 `Awake()` 中调用 `CreateUIElements()`
2. `CreateUIElements()` 使用 `FindObjectOfType<Canvas>()` 查找 Canvas（会找到 GameCanvas）
3. 创建的 `overviewPanel` 初始被设置为 `SetActive(false)`
4. 在 `Start()` 中，如果 `showOnStart` 为 true，调用 `Show()` 激活 Panel

**可能的问题：**
1. **脚本执行顺序问题**：`GlobalOverviewUI.Awake()` 可能在 `GameCanvas` 初始化之前执行
2. **FindObjectOfType 找不到 Canvas**：Build 后执行顺序不同，可能找不到 GameCanvas
3. **Canvas 设置问题**：GameCanvas 的 Render Mode 或 Sorting Order 可能影响显示
4. **Panel 被遮挡**：创建的 Panel 可能被其他 UI 元素遮挡

### 排查步骤

#### 1. 检查 Inspector 设置（最重要）
在 Unity Editor 中，选中 `GlobalOverviewUI` GameObject：
- ✅ `Show On Start`：**必须勾选**
- ✅ `Show Debug Log`：**建议勾选**（查看日志）
- `Overview Image`：显示为 "None" 是**正常的**（运行时动态创建）

#### 2. 检查 Build 后的 Console 日志
Build 后运行游戏，查看 Console 是否有以下日志：
- ✅ `GlobalOverviewUI: UI元素已创建`
- ✅ `GlobalOverviewUI: 已设置RenderTexture`
- ✅ `GlobalOverviewUI: 全局视窗已显示`

**如果缺少这些日志，说明初始化失败！**

#### 3. 检查 Hierarchy 中的 UI 元素
Build 后运行游戏时，在 Hierarchy 中搜索：
- `GlobalOverviewPanel`（应该在 GameCanvas 下）
- 如果不存在，说明 `CreateUIElements()` 没有执行

#### 4. 检查 GameCanvas 设置
选中 `GameCanvas` GameObject，检查：
- **Render Mode**：应该是 `Screen Space - Overlay` 或 `Screen Space - Camera`
- **Sort Order**：如果 GlobalOverviewPanel 被其他 UI 遮挡，可能需要调整
- **Canvas Scaler**：确保已正确配置

#### 5. 手动测试：按 Tab 键
Build 后运行游戏，按 `Tab` 键（默认切换键）：
- 如果按 Tab 后出现，说明是 `Show On Start` 或初始化顺序的问题
- 如果按 Tab 后仍不出现，说明 UI 元素创建有问题

#### 6. 设置 Script Execution Order（推荐）
可能 `GlobalOverviewUI` 的 `Awake()` 在 `GameCanvas` 初始化之前执行。

**设置方法：**
1. `Edit` → `Project Settings` → `Script Execution Order`
2. 添加 `GlobalOverviewCamera`，设置 Order 为 `-100`（更早执行）
3. 添加 `GlobalOverviewUI`，设置 Order 为 `-50`（在 GlobalOverviewCamera 之后，但早于默认）
4. 确保 `GameCanvas` 相关的脚本（如果有）也在早期执行

### 最可能的原因
根据代码逻辑，最可能是：
1. **脚本执行顺序问题**：`GlobalOverviewUI.Awake()` 在 `GameCanvas` 初始化之前执行，`FindObjectOfType<Canvas>()` 找不到 Canvas
2. **Build 后 FindObjectOfType 失效**：Build 后执行顺序不同，可能找不到 GameCanvas

### 建议的检查顺序
1. ✅ **先检查 Inspector 中的 `Show On Start` 是否勾选**（最重要）
2. ✅ **设置 Script Execution Order**（确保 GlobalOverviewCamera 和 GlobalOverviewUI 早于默认执行）
3. ✅ **Build 后查看 Console 日志**，确认初始化流程
4. ✅ **检查 Hierarchy 中是否有 `GlobalOverviewPanel`**
5. ✅ **尝试按 Tab 键切换显示**

---

## 问题二：Build 后只显示一个相机（旧问题，已解决）

### 1. 检查 UNITY_EDITOR 宏

**什么是 UNITY_EDITOR 宏？**
- `#if UNITY_EDITOR` 和 `#endif` 之间的代码**只在编辑器运行**，Build 后会被完全忽略
- 如果相机的启用/禁用逻辑被包裹在这个宏中，Build 后就不会执行

**如何检查：**
1. 在代码中搜索 `UNITY_EDITOR` 关键词
2. 检查是否有相机相关的代码（如 `camera.enabled = true`、`SetActive(true)`）被包裹在宏中

**已检查结果：**
✅ 已检查所有相机相关脚本，**未发现**相机启用/禁用逻辑被包裹在 `UNITY_EDITOR` 宏中

### 2. 检查相机设置

**可能的原因：**

#### A. 相机深度（Depth）设置
- Unity 中，**深度值越高的相机渲染在上层**
- 如果两个相机的深度相同，可能只显示一个
- **检查方法：**
  1. 在 Hierarchy 中找到两个相机
  2. 查看 Inspector 中的 `Depth` 值
  3. 确保两个相机的深度不同（例如：主相机 Depth = 0，全局总览相机 Depth = -1）

#### B. 相机启用状态
- 检查 Build 后相机是否被禁用
- **检查方法：**
  1. 在场景中检查两个相机的 `Camera` 组件是否勾选（Enabled）
  2. 检查相机的 GameObject 是否激活（Active）

#### C. 全局总览相机（GlobalOverviewCamera）
- 全局总览相机的深度设置为 `-1`（在 `GlobalOverviewCamera.cs` 第86行）
- 这个相机的画面是通过 RenderTexture 显示在 UI 上的，不是直接渲染到屏幕
- **如果 Build 后看不到全局总览画面，检查：**
  1. `GlobalOverviewUI` 组件是否存在
  2. `GlobalOverviewUI` 的 `Show On Start` 是否勾选
  3. RenderTexture 是否正确设置

### 3. 检查场景中的相机配置

**步骤：**
1. 打开有问题的场景
2. 在 Hierarchy 中搜索 "Camera"
3. 检查每个相机的设置：
   - **Camera 组件**：是否启用（Enabled）
   - **GameObject**：是否激活（Active）
   - **Depth**：深度值是多少
   - **Culling Mask**：渲染哪些 Layer
   - **Target Display**：是否设置为 Display 1（多显示器时）

### 4. 检查脚本初始化

**可能的问题：**
- 某些脚本在 `Awake()` 或 `Start()` 中禁用相机
- Build 后脚本执行顺序可能不同

**检查方法：**
1. 在 `GlobalOverviewCamera.cs` 的 `Awake()` 中添加日志：
```csharp
void Awake()
{
    Debug.Log($"[GlobalOverviewCamera] Awake: 相机 {overviewCamera.name} 启用状态: {overviewCamera.enabled}");
    // ... 现有代码
}
```

2. Build 后运行游戏，查看日志

---

## 问题二：导出分辨率设置（1920x1080）

### Unity Build Settings 设置

#### 1. 打开 Build Settings
- **菜单栏**：`File` → `Build Settings...`
- 或按快捷键：`Ctrl+Shift+B`（Windows） / `Cmd+Shift+B`（Mac）

#### 2. 设置分辨率（Player Settings）
1. 点击 `Player Settings...` 按钮
2. 在 Inspector 中找到 **Resolution and Presentation** 部分

#### 3. 关键设置项：

**A. Default Canvas Scalers（Canvas 缩放器）**
- 如果使用 UI Canvas，确保 Canvas Scaler 设置为 `Scale With Screen Size`
- Reference Resolution：`1920 x 1080`

**B. Resolution（分辨率）**
- **Default Resolution**：选择 `1920 x 1080`
- **Fullscreen Mode**：根据需要选择
  - `Full Screen Window`：全屏窗口
  - `Exclusive Fullscreen`：独占全屏
  - `Windowed`：窗口模式

**C. Standalone Player Settings（独立平台设置）**
- **Default Screen Width**：`1920`
- **Default Screen Height**：`1080`
- **Default Screen Mode**：根据需要选择（Fullscreen / Windowed）

#### 4. 代码中设置分辨率（可选）

如果需要在运行时动态设置分辨率，可以在脚本中添加：

```csharp
using UnityEngine;

public class ResolutionSetter : MonoBehaviour
{
    void Start()
    {
        // 设置分辨率为 1920x1080，全屏窗口模式
        Screen.SetResolution(1920, 1080, FullScreenMode.FullScreenWindow);
        
        // 或者窗口模式
        // Screen.SetResolution(1920, 1080, FullScreenMode.Windowed);
    }
}
```

#### 5. Canvas Scaler 设置（UI 适配）

如果游戏中有 UI，确保 Canvas 的 Canvas Scaler 组件设置正确：

1. 选中 Canvas GameObject
2. 在 Inspector 中找到 `Canvas Scaler` 组件
3. 设置：
   - **UI Scale Mode**：`Scale With Screen Size`
   - **Reference Resolution**：`X: 1920, Y: 1080`
   - **Screen Match Mode**：`Match Width Or Height`（推荐）
   - **Match**：`0.5`（平衡宽度和高度）

---

## 快速检查清单

### Build 前检查：
- [ ] 所有相机的 `Camera` 组件已启用
- [ ] 所有相机的 GameObject 已激活
- [ ] 相机深度设置正确（主相机 Depth = 0，其他相机 Depth ≠ 0）
- [ ] 没有相机启用/禁用逻辑被包裹在 `UNITY_EDITOR` 宏中
- [ ] `GlobalOverviewUI` 的 `Show On Start` 已勾选（如果使用全局总览）

### Build 设置检查：
- [ ] Default Resolution 设置为 `1920 x 1080`
- [ ] Canvas Scaler 的 Reference Resolution 设置为 `1920 x 1080`
- [ ] Fullscreen Mode 已正确设置

---

## 如果问题仍然存在

### 调试步骤：
1. **添加日志**：在相机相关的 `Awake()` 和 `Start()` 中添加 `Debug.Log`，查看 Build 后是否执行
2. **检查 Build 日志**：查看 Build 过程中是否有警告或错误
3. **简化测试**：创建一个新场景，只放两个相机，测试是否能正常显示
4. **检查平台差异**：某些平台（如 WebGL）对多相机支持有限制

### 常见问题：
- **WebGL 平台**：可能不支持多显示器，某些相机设置可能不生效
- **移动平台**：分辨率设置可能被系统覆盖
- **多场景加载**：如果使用 Additive 场景加载，确保相机在正确的场景中

