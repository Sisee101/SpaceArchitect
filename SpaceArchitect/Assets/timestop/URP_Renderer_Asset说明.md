# URP Renderer Asset 说明

## 📋 什么是 URP Renderer Asset？

**URP Renderer Asset** 是 Unity 的 Universal Render Pipeline (URP) 中的一个配置文件，它定义了渲染管线的设置，包括：
- 渲染特性（Renderer Features）
- 渲染顺序
- 后处理设置
- 阴影设置等

## 🔍 如何找到 URP Renderer Asset？

### 方法 1：在 Project 窗口中查找

1. **打开 Unity 编辑器**
2. **在 Project 窗口**（通常在左下角）中：
   - 导航到 `Assets/Settings/` 文件夹
   - 查找以下文件之一：
     - `URP-Performant-Renderer.asset`
     - `URP-Balanced-Renderer.asset`
     - `URP-HighFidelity-Renderer.asset`

### 方法 2：通过 URP Asset 查找

1. **找到 URP Asset**：
   - 在 Project 窗口中查找 `URP-Performant.asset`、`URP-Balanced.asset` 或 `URP-HighFidelity.asset`
   - 通常在 `Assets/Settings/` 文件夹中

2. **打开 URP Asset**：
   - 双击打开
   - 在 Inspector 中查看 `Renderer List`
   - 可以看到使用的 Renderer Asset

### 方法 3：通过 Graphics Settings 查找

1. **打开 Graphics Settings**：
   - 菜单栏：`Edit` → `Project Settings` → `Graphics`
   - 或者：`Edit` → `Project Settings` → `Quality`

2. **查看 Scriptable Render Pipeline Settings**：
   - 找到 `Scriptable Render Pipeline Settings` 字段
   - 这里显示的是 URP Asset
   - 打开 URP Asset，查看 `Renderer List`

## 📁 在你的项目中的位置

根据你的项目结构，URP Renderer Asset 应该在：

```
Assets/
  └── Settings/
      ├── URP-Performant-Renderer.asset  ← 这个就是 Renderer Asset
      ├── URP-Balanced-Renderer.asset
      ├── URP-HighFidelity-Renderer.asset
      ├── URP-Performant.asset  ← 这个是 URP Asset（不是 Renderer Asset）
      ├── URP-Balanced.asset
      └── URP-HighFidelity.asset
```

## 🎯 如何打开和配置

### 步骤 1：找到文件

1. 在 Project 窗口中，导航到 `Assets/Settings/`
2. 找到 `URP-Performant-Renderer.asset`（或你使用的其他 Renderer）
3. **双击打开**

### 步骤 2：添加 Renderer Feature

1. **在 Inspector 窗口中**（通常在右侧）：
   - 找到 `Renderer Features` 列表
   - 点击 `+` 按钮
   - 选择 `Shockwave Renderer Feature`

2. **设置 Material**：
   - 在 `Shockwave Renderer Feature` 组件中
   - 找到 `Shockwave Material` 字段
   - 将 Material 拖拽到该字段（或点击圆圈图标选择）

## ⚠️ 重要提示

1. **Renderer Asset vs URP Asset**：
   - **URP Asset**（如 `URP-Performant.asset`）：定义整个渲染管线的设置
   - **Renderer Asset**（如 `URP-Performant-Renderer.asset`）：定义具体的渲染器设置，包括 Renderer Features

2. **可能有多个 Renderer Asset**：
   - 你的项目可能有多个 Renderer Asset（Performant、Balanced、HighFidelity）
   - 需要找到**当前正在使用的**那个
   - 可以通过 URP Asset 的 `Renderer List` 查看

3. **如果找不到**：
   - 检查 `Assets/Settings/` 文件夹是否存在
   - 检查文件是否被隐藏（查看 `.meta` 文件）
   - 尝试在 Project 窗口中搜索 "Renderer"

## 🔧 快速查找方法

### 在 Unity 编辑器中：

1. **使用搜索功能**：
   - 在 Project 窗口的搜索框中输入 "Renderer"
   - 过滤类型选择 "Asset"
   - 应该能找到所有 Renderer Asset

2. **查看文件类型**：
   - Renderer Asset 的文件图标通常显示为齿轮或设置图标
   - 文件名通常包含 "Renderer"

## 📝 示例路径

在你的项目中，URP Renderer Asset 的完整路径可能是：

```
D:\游戏\empty\SpaceArchitect\SpaceArchitect\Assets\Settings\URP-Performant-Renderer.asset
```

## ✅ 总结

- **位置**：`Assets/Settings/` 文件夹
- **文件名**：`URP-*-Renderer.asset`（如 `URP-Performant-Renderer.asset`）
- **作用**：配置渲染器设置，包括 Renderer Features
- **如何打开**：双击文件，在 Inspector 中配置







