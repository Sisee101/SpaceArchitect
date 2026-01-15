# 修复 Renderer Feature 只显示一半的问题

## 🔍 问题分析

如果所有节点预览都正常，Fragment 的 Alpha 也是 1，但游戏中只显示右半边，问题可能在：

### 1. Renderer Feature 的 Blit 方式

**可能的问题**：
- Blit 的方式可能不正确
- 或者需要使用全屏 Quad 而不是 Blit

### 2. UV 坐标的问题

**可能的问题**：
- 屏幕内容采样的 UV 坐标可能有问题
- 或者 Screen Position 的提取有问题

### 3. 临时纹理的问题

**可能的问题**：
- 临时纹理的创建可能有问题
- 或者 Blit 回源纹理时有问题

## 🔧 解决方法

### 方法 1：使用全屏 Quad 渲染（推荐）

修改 Renderer Feature，使用全屏 Quad 而不是 Blit：

```csharp
// 使用全屏 Quad 渲染
cmd.DrawMesh(RenderingUtils.fullscreenMesh, Matrix4x4.identity, shockwaveMaterial, 0, 0);
```

### 方法 2：检查 Blit 的方式

确保 Blit 使用正确的 pass index：

```csharp
Blit(cmd, source, tempTexture.Identifier(), shockwaveMaterial, 0);
```

### 方法 3：检查 UV 坐标

确保 Screen Position 的 XY 正确提取和使用。

## 📝 修改 Renderer Feature

让我修改 Renderer Feature 的代码，使用全屏 Quad 渲染：



