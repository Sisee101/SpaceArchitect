# Renderer Feature 实现步骤详解

## ✅ 已完成

1. ✅ **创建了 Renderer Feature 脚本**：`ShockwaveRendererFeature.cs`

## 📋 接下来需要做的步骤

### 步骤 1：创建新的 Shader Graph（重要！）

由于 Renderer Feature 使用 Blit 传入屏幕内容，需要创建一个新的 Shader Graph，**不使用 Scene Color Node**。

#### 方法 A：在 Unity 编辑器中创建（推荐）

1. **创建 Shader Graph**：
   - 右键点击 `Assets/timestop/` 文件夹
   - 选择 `Create` → `Shader Graph` → `URP` → `Unlit Shader Graph`
   - 命名为 `SG_Shockwave_RendererFeature`

2. **设置 Graph Inspector**：
   - 打开 Shader Graph
   - 在右侧 **Graph Inspector** 中：
     - **Surface**: `Transparent`
     - **Blend**: `Alpha`
     - **Depth Write**: `Force Disabled`（禁用深度写入）
     - **Depth Test**: `Always`（或 `Disabled`，如果选项中有的话）

3. **添加属性**（与现有 shader 相同）：
   - `_RippleDistanceFromCenter` (Float, Default: 0)
   - `_RippleStrength` (Float, Default: 0)
   - `_RippleWidth` (Float, Default: 0.1)
   - `_CenterPoint` (Vector4, Default: (0.5, 0.5, 0, 0))
   - **新增**：`_MainTex` (Texture2D) - 用于接收屏幕内容

4. **添加节点**：
   - **Screen Position** (Default 模式)
   - **Sample Texture 2D**：
     - `_MainTex` → Texture 输入
     - `Screen Position` 的 `XY` → UV 输入
   - **复制现有的冲击波逻辑**：
     - 距离计算（Screen Position 到 CenterPoint）
     - Smoothstep 节点
     - Multiply 节点
     - Combine 节点
     - Add 节点

5. **连接输出**：
   - `Sample Texture 2D` 的 `RGBA` → `Add` 节点 → `BaseColor`

#### 方法 B：我帮你创建 Shader Graph 文件

如果你希望我直接创建 Shader Graph 文件，我可以基于现有的 `SG_Shockwave.shadergraph` 创建一个新版本，将 Scene Color Node 替换为 Texture2D 属性。

### 步骤 2：创建 Material

1. **创建 Material**：
   - 右键点击 `Assets/timestop/` 文件夹
   - 选择 `Create` → `Material`
   - 命名为 `ShockwaveRing_RendererFeature`
   - 将 Shader 设置为 `Shader Graphs/SG_Shockwave_RendererFeature`

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

1. **禁用旧的 ShockwaveRing GameObject**：
   - 因为现在使用 Renderer Feature，不再需要 GameObject
   - 或者保留 GameObject 但禁用 Renderer 组件

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

## ⚠️ 关键区别

### 旧实现（Scene Color Node）
- 只能读取不透明纹理
- 需要 GameObject 渲染
- 可能遮挡透明物体

### 新实现（Renderer Feature）
- 读取完整的屏幕内容（包括透明物体）
- 不需要 GameObject（或可以禁用）
- 不会遮挡任何物体

## ❓ 需要我帮你创建 Shader Graph 吗？

我可以：
1. **直接创建 Shader Graph 文件**（基于现有的，替换 Scene Color Node）
2. **或者提供更详细的节点连接步骤**

你希望我直接创建，还是你自己按照指南创建？

