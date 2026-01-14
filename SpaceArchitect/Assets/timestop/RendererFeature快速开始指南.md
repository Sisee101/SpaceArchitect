# Renderer Feature 快速开始指南

## ✅ 已完成

1. ✅ **Renderer Feature 脚本**：`ShockwaveRendererFeature.cs`

## 📋 接下来需要做的步骤

### 步骤 1：在 Unity 编辑器中创建新的 Shader Graph

由于 Renderer Feature 使用 Blit 传入屏幕内容，需要创建一个新的 Shader Graph。

#### 1.1 创建 Shader Graph

1. 在 Unity 编辑器中：
   - 右键点击 `Assets/timestop/` 文件夹
   - 选择 `Create` → `Shader Graph` → `URP` → `Unlit Shader Graph`
   - 命名为 `SG_Shockwave_RendererFeature`

#### 1.2 设置 Graph Inspector

1. 打开刚创建的 Shader Graph
2. 在右侧 **Graph Inspector** 中设置：
   - **Surface**: `Transparent`
   - **Blend**: `Alpha`
   - **Depth Write**: `Force Disabled`（禁用深度写入）
   - **Depth Test**: `Always`（或 `Disabled`，如果选项中有的话）

#### 1.3 添加属性

在 Shader Graph 中添加以下属性（右键点击 Graph → `Create Node` → `Property`）：

1. **Texture2D 属性**（用于接收屏幕内容）：
   - 类型：`Texture2D`
   - 名称：`MainTex`
   - 引用名称：`_MainTex`（这是 Blit 默认使用的纹理名称）

2. **保留现有属性**（与 `ShockwaveController.cs` 对应）：
   - `RippleDistanceFromCenter` (Float)
   - `RippleStrength` (Float)
   - `RippleWidth` (Float)
   - `CenterPoint` (Vector4)

#### 1.4 添加节点并连接

**节点连接流程**：

1. **Screen Position** (Default 模式)
   - 输出 Vector4 → 需要提取 XY 用于 UV 坐标

2. **Split 节点**（用于提取 XY）
   - `Screen Position` 的输出 → `Split` 的输入
   - `Split` 的 **R 输出** = X 坐标
   - `Split` 的 **G 输出** = Y 坐标
   - 注意：Split 节点显示的是 R, G, B, A，但对于 Vector4，R=X, G=Y, B=Z, A=W

3. **Combine 节点**（组合成 Vector2）
   - `Split` 的 **R** → `Combine` 的 **R**（X 坐标）
   - `Split` 的 **G** → `Combine` 的 **G**（Y 坐标）
   - `Combine` 输出 Vector2 → 这就是 UV 坐标

4. **Sample Texture 2D**
   - `_MainTex` 属性 → `Texture` 输入
   - `Combine` 的输出（Vector2） → `UV` 输入
   - 输出 `RGBA` → 这是屏幕内容

3. **复制现有的冲击波逻辑**（从 `SG_Shockwave.shadergraph`）：
   - 距离计算：`Screen Position` → `Subtract` → `CenterPoint` → `Length`
   - `Length` → `Subtract` → `RippleDistanceFromCenter` → `Absolute`
   - `Absolute` → `Smoothstep` (Edge1: 0, Edge2: `RippleWidth`)
   - `Smoothstep` → `Multiply` → `RippleStrength`
   - `Multiply` → `Combine` (R, G, B) → `Vector3`
   - `Vector3` → `Add` 节点

4. **最终输出**：
   - `Sample Texture 2D` 的 `RGBA` → `Add` 节点的 `A` 输入
   - `Add` 节点输出 → `BaseColor`

### 步骤 2：创建 Material

1. 右键点击 `Assets/timestop/` 文件夹
2. 选择 `Create` → `Material`
3. 命名为 `ShockwaveRing_RendererFeature`
4. 将 Shader 设置为 `Shader Graphs/SG_Shockwave_RendererFeature`

### 步骤 3：配置 URP Renderer

1. **打开 URP Renderer Asset**：
   - 在 Project 窗口中找到你使用的 Renderer（如 `URP-Performant-Renderer.asset`）
   - 双击打开

2. **添加 Renderer Feature**：
   - 在 Inspector 中找到 `Renderer Features` 列表
   - 点击 `+` 按钮
   - 选择 `Shockwave Renderer Feature`

3. **设置 Material**：
   - 在 `Shockwave Renderer Feature` 组件中
   - 将 `Shockwave Material` 字段设置为 `ShockwaveRing_RendererFeature`

### 步骤 4：更新 ShockwaveController

`ShockwaveController.cs` **几乎不需要修改**，只需要：
- 将 `shockwaveMaterial` 引用改为新的 Material（`ShockwaveRing_RendererFeature`）

### 步骤 5：禁用旧的 GameObject（可选）

1. 禁用或删除旧的 `ShockwaveRing` GameObject
2. 因为现在使用 Renderer Feature，不再需要 GameObject

## 🎯 工作原理

1. **渲染顺序**：
   ```
   不透明物体 → 透明物体（星星、玻璃） → Renderer Feature（冲击波效果）
   ```

2. **Blit 过程**：
   ```
   完整屏幕内容 → 通过冲击波材质 → 临时纹理 → 回写到屏幕
   ```

3. **结果**：
   - ✅ 冲击波效果正常
   - ✅ 透明物体正常显示（因为读取的是完整屏幕内容）
   - ✅ 不遮挡任何物体（全屏后处理）

## ⚠️ 重要提示

1. **`_MainTex` 是 Blit 的默认纹理名称**：
   - Renderer Feature 会自动将屏幕内容传入 `_MainTex`
   - 不需要手动设置

2. **UV 坐标**：
   - 使用 `Screen Position` 的 `XY` 输出作为 UV
   - 确保坐标范围是 0-1

3. **测试**：
   - 运行游戏
   - 按 E 键触发冲击波
   - 确认效果正常，且透明物体可见

## 🔧 如果遇到问题

1. **效果不显示**：
   - 检查 Material 是否正确引用
   - 检查 Renderer Feature 是否启用
   - 检查 shader 属性是否正确设置

2. **效果不正确**：
   - 检查 Shader Graph 的连接
   - 检查 UV 坐标是否正确
   - 检查属性值是否正确

3. **透明物体仍然被遮挡**：
   - 确认 Renderer Feature 在 `AfterRenderingTransparents` 阶段执行
   - 确认 Shader Graph 的 Depth Test 设置为 `Always`

## 📝 需要帮助？

如果在创建 Shader Graph 时遇到问题，可以：
1. 参考现有的 `SG_Shockwave.shadergraph` 的节点连接
2. 或者告诉我具体问题，我可以提供更详细的指导

