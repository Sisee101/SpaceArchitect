# 如何查看当前使用的 URP Renderer

## 🔍 方法 1：通过 Graphics Settings 查看（最简单）

### 步骤：

1. **打开 Project Settings**：
   - 菜单栏：`Edit` → `Project Settings`
   - 或者快捷键：`Ctrl + ,`（Windows）或 `Cmd + ,`（Mac）

2. **打开 Graphics 设置**：
   - 在左侧列表中找到 `Graphics`
   - 点击打开

3. **查看 Scriptable Render Pipeline Settings**：
   - 在 `Graphics` 设置页面中
   - 找到 `Scriptable Render Pipeline Settings` 字段
   - 这里显示的是 **URP Asset**（如 `URP-Performant`）

4. **查看 Renderer List**：
   - 点击 `Scriptable Render Pipeline Settings` 字段中的 URP Asset
   - 在 Inspector 中查看 `Renderer List`
   - 这里会显示使用的 **Renderer Asset**（如 `URP-Performant-Renderer`）

## 🔍 方法 2：通过 Quality Settings 查看

### 步骤：

1. **打开 Project Settings**：
   - 菜单栏：`Edit` → `Project Settings`

2. **打开 Quality 设置**：
   - 在左侧列表中找到 `Quality`
   - 点击打开

3. **查看 Render Pipeline Asset**：
   - 在 `Quality` 设置页面中
   - 找到当前质量等级（如 `High`、`Medium`、`Low`）
   - 查看该质量等级的 `Render Pipeline Asset` 字段
   - 这里显示的是 **URP Asset**

4. **查看 Renderer List**：
   - 点击 URP Asset
   - 在 Inspector 中查看 `Renderer List`
   - 这里会显示使用的 **Renderer Asset**

## 🔍 方法 3：直接查看 URP Asset

### 步骤：

1. **在 Project 窗口中查找 URP Asset**：
   - 导航到 `Assets/Settings/` 文件夹
   - 查找以下文件之一：
     - `URP-Performant.asset`
     - `URP-Balanced.asset`
     - `URP-HighFidelity.asset`

2. **打开 URP Asset**：
   - 双击打开
   - 在 Inspector 中查看 `Renderer List`

3. **查看使用的 Renderer**：
   - `Renderer List` 中会显示一个或多个 Renderer Asset
   - `Default Renderer Index` 显示默认使用的 Renderer 索引
   - 根据索引找到对应的 Renderer Asset

## 📋 快速检查清单

### 在 Graphics Settings 中：

1. `Edit` → `Project Settings` → `Graphics`
2. 查看 `Scriptable Render Pipeline Settings` 字段
3. 点击该字段中的 URP Asset
4. 在 Inspector 中查看 `Renderer List`
5. 找到 `Default Renderer Index` 对应的 Renderer Asset

### 示例：

```
Graphics Settings
  └── Scriptable Render Pipeline Settings: URP-Performant
      └── Renderer List:
          [0] URP-Performant-Renderer  ← 这个就是当前使用的！
      └── Default Renderer Index: 0
```

## 🎯 找到后该做什么？

一旦找到当前使用的 Renderer Asset（如 `URP-Performant-Renderer`）：

1. **打开该 Renderer Asset**：
   - 在 Project 窗口中双击打开
   - 或在 Inspector 中点击 `Renderer List` 中的 Renderer Asset

2. **添加 Renderer Feature**：
   - 在 Inspector 中找到 `Renderer Features` 列表
   - 点击 `+` 按钮
   - 选择 `Shockwave Renderer Feature`

3. **设置 Material**：
   - 在 `Shockwave Renderer Feature` 组件中
   - 将 Material 拖拽到 `Shockwave Material` 字段

## ⚠️ 重要提示

1. **可能有多个质量等级**：
   - 不同的质量等级可能使用不同的 URP Asset
   - 检查当前使用的质量等级（在 `Quality` 设置中）

2. **可能有多个 Renderer**：
   - 一个 URP Asset 可能有多个 Renderer Asset
   - 查看 `Default Renderer Index` 确定当前使用的

3. **如果找不到**：
   - 检查 `Assets/Settings/` 文件夹是否存在
   - 尝试在 Project 窗口中搜索 "URP" 或 "Renderer"

## 📝 总结

**最简单的方法**：
1. `Edit` → `Project Settings` → `Graphics`
2. 查看 `Scriptable Render Pipeline Settings` 字段
3. 点击 URP Asset，查看 `Renderer List`
4. 找到 `Default Renderer Index` 对应的 Renderer Asset

这就是你当前使用的 Renderer Asset！







