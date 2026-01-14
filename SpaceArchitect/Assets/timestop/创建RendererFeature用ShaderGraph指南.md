# 创建 Renderer Feature 用 Shader Graph 指南

## 🎯 目标

创建一个新的 Shader Graph，用于 Renderer Feature，可以读取完整的屏幕内容（包括透明物体）。

## 📋 步骤

### 1. 创建新的 Shader Graph

1. **在 Unity 编辑器中**：
   - 右键点击 `Assets/timestop/` 文件夹
   - 选择 `Create` → `Shader Graph` → `URP` → `Unlit Shader Graph`
   - 命名为 `SG_Shockwave_RendererFeature`

### 2. 设置 Shader Graph 属性

#### 2.1 设置 Surface Type

1. 在 **Graph Inspector** 中：
   - **Surface**: `Transparent`
   - **Blend**: `Alpha`

#### 2.2 设置 Depth

1. 在 **Graph Inspector** 中：
   - **Depth Write**: `Force Disabled`（禁用深度写入）
   - **Depth Test**: `Always`（或 `Disabled`，如果选项中有的话）

### 3. 添加节点

#### 3.1 添加 Texture2D 属性（用于接收屏幕内容）

1. **创建属性**：
   - 右键点击 Graph → `Create Node` → `Property` → `Texture2D`
   - 命名为 `_MainTex` 或 `_ScreenTexture`
   - 这是 Renderer Feature 传入的屏幕内容

#### 3.2 添加 Screen Position 节点

1. **创建节点**：
   - 右键点击 Graph → `Create Node` → `Input` → `Screen Position`
   - 选择 `Default` 模式

#### 3.3 添加 Sample Texture 2D 节点

1. **创建节点**：
   - 右键点击 Graph → `Create Node` → `Texture` → `Sample Texture 2D`
   - 将 `_MainTex` 属性连接到 `Texture` 输入
   - 将 `Screen Position` 的 `XY` 输出连接到 `UV` 输入

#### 3.4 复制现有的冲击波逻辑

从 `SG_Shockwave.shadergraph` 复制以下节点和连接：
- 距离计算逻辑（使用 Screen Position）
- Smoothstep 节点
- Multiply 节点
- Combine 节点
- Add 节点

#### 3.5 连接最终输出

1. **BaseColor**：
   - `Sample Texture 2D` 的 `RGBA` 输出 → `Add` 节点 → `BaseColor`

### 4. 添加 Shader 属性

确保以下属性存在（与 `ShockwaveController.cs` 中的属性 ID 对应）：
- `_RippleDistanceFromCenter` (Float)
- `_RippleStrength` (Float)
- `_RippleWidth` (Float)
- `_CenterPoint` (Vector4)

### 5. 保存 Shader Graph

保存后，Unity 会自动编译。

## ⚠️ 重要提示

1. **不要使用 Scene Color Node**：
   - Renderer Feature 会通过 Blit 传入屏幕内容
   - 使用 Texture2D 属性接收

2. **UV 坐标**：
   - 使用 `Screen Position` 的 `XY` 输出作为 UV
   - 确保坐标范围是 0-1

3. **测试**：
   - 创建 Material 使用新的 Shader Graph
   - 在 Renderer Feature 中引用这个 Material

## 📝 下一步

创建完 Shader Graph 后：
1. 创建 Material 使用新的 Shader Graph
2. 在 Renderer Feature 中引用这个 Material
3. 在 URP Renderer 中添加 Renderer Feature

