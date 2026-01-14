# Screen Position 节点说明

## 📋 什么是 Screen Position？

`Screen Position` 是 Unity Shader Graph 中的一个输入节点，它输出当前像素在屏幕空间中的位置。

## 🔢 Screen Position 的输出

`Screen Position` 节点输出一个 **Vector4**（4个分量）：
- **X**: 屏幕空间的 X 坐标
- **Y**: 屏幕空间的 Y 坐标
- **Z**: 深度值（通常用于深度测试）
- **W**: 透视除法后的值

## 🎯 什么是 `XY`？

`XY` 指的是 **X 和 Y 两个分量**，也就是屏幕空间的 **UV 坐标**。

### 为什么需要 XY？

在 Shader Graph 中，`Screen Position` 输出的是 Vector4，但我们需要的是 Vector2（只有 X 和 Y）来作为 UV 坐标采样纹理。

## 📐 坐标范围

`Screen Position` 的 `XY` 坐标范围取决于你选择的模式：

### 1. **Default 模式**（推荐）
- 坐标范围：**0 到 1**
- (0, 0) = 屏幕左下角
- (1, 1) = 屏幕右上角
- 这是标准的 UV 坐标范围，可以直接用于采样纹理

### 2. **Raw 模式**
- 坐标范围：**0 到屏幕分辨率**
- (0, 0) = 屏幕左下角
- (width, height) = 屏幕右上角
- 需要除以屏幕分辨率才能得到 0-1 的范围

## 🔧 在 Shader Graph 中如何使用

### 方法 1：使用 Split 节点（推荐）

1. 添加 `Screen Position` 节点（选择 Default 模式）
2. 添加 `Split` 节点
3. 将 `Screen Position` 的输出连接到 `Split` 的输入
4. `Split` 节点会分离出 **R, G, B, A** 四个分量
   - **重要**：对于 Vector4，R = X, G = Y, B = Z, A = W
5. 使用 `Split` 的 **R** 和 **G** 输出（这就是 X 和 Y）
6. 添加 `Combine` 节点，将 R 和 G 组合成 Vector2（作为 UV 坐标）

### 方法 2：直接使用 Vector2 输出（如果节点支持）

在较新版本的 Unity Shader Graph 中，`Screen Position` 节点可能直接有 `XY` 输出端口，可以直接连接使用。但大多数情况下需要使用 Split 节点。

## 📝 实际应用示例

### 示例：采样屏幕纹理

```
Screen Position (Default) 
  → Split 节点 
  → R 输出（X 坐标）和 G 输出（Y 坐标）
  → Combine 节点（组合成 Vector2）
  → Sample Texture 2D 的 UV 输入
```

**详细步骤**：
1. `Screen Position` → `Split` 节点
2. `Split` 的 **R** → `Combine` 的 **R**（X 坐标）
3. `Split` 的 **G** → `Combine` 的 **G**（Y 坐标）
4. `Combine` 输出 Vector2 → `Sample Texture 2D` 的 `UV` 输入

## ⚠️ 重要提示

1. **使用 Default 模式**：
   - 坐标范围是 0-1，可以直接用于采样纹理
   - 不需要额外的计算

2. **不要使用 Raw 模式**（除非有特殊需求）：
   - 坐标范围是 0 到屏幕分辨率
   - 需要除以屏幕分辨率才能得到正确的 UV 坐标

3. **在 Renderer Feature 中使用**：
   - `Screen Position` 的 `XY` 用于采样 `_MainTex`（屏幕内容）
   - 确保使用 Default 模式，坐标范围是 0-1

## 🎯 在你的冲击波 Shader 中

在创建 `SG_Shockwave_RendererFeature` 时：

1. 添加 `Screen Position` 节点
2. 选择 **Default** 模式
3. 添加 `Split` 节点，将 `Screen Position` 的输出连接到 `Split`
4. 使用 `Split` 的 **R**（X 坐标）和 **G**（Y 坐标）输出
5. 添加 `Combine` 节点，将 R 和 G 组合成 Vector2
6. 将 `Combine` 的输出连接到 `Sample Texture 2D` 的 `UV` 输入
7. 这样就能正确采样屏幕内容了

## 📸 可视化说明

```
屏幕坐标系统（Default 模式）：
┌─────────────────┐
│ (0,1)    (1,1)  │  ← 屏幕顶部
│                 │
│                 │
│ (0,0)    (1,0)  │  ← 屏幕底部
└─────────────────┘
```

`Screen Position` 的 `XY` 就是每个像素在这个坐标系中的位置（0-1 范围）。

