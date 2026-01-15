# Renderer Feature 中 GameObject 的处理说明

## 📋 重要说明

**使用 Renderer Feature 后，不再需要 GameObject 来渲染冲击波效果！**

## 🎯 关键区别

### 旧实现（使用 GameObject）

```
ShockwaveRing GameObject
  ├── Mesh Renderer（使用 Material 渲染）
  └── ShockwaveController（控制 Material 属性）
```

- **需要**：GameObject 的 Mesh Renderer 使用 Material
- **问题**：GameObject 会遮挡其他物体

### 新实现（使用 Renderer Feature）

```
Renderer Feature
  └── 直接使用 Material 在全屏上渲染效果
  └── ShockwaveController（控制 Material 属性）
```

- **不需要**：GameObject 的 Mesh Renderer
- **优点**：不会遮挡任何物体

## ✅ 正确的做法

### 1. ShockwaveController 的 Material 引用

**需要改**：`ShockwaveController.cs` 中的 `shockwaveMaterial` 引用
- 改为新的 Material（使用 `SG_Shockwave_RendererFeature` shader）

### 2. GameObject 的 Mesh Renderer

**不需要改**，而且应该：

#### 选项 A：禁用 Mesh Renderer 组件（推荐）

1. 选择 `ShockwaveRing` GameObject
2. 在 Inspector 中找到 `Mesh Renderer` 组件
3. **取消勾选**（禁用该组件）

**优点**：
- 保留 GameObject 和 `ShockwaveController` 脚本
- 只需要禁用 Renderer，不影响脚本运行

#### 选项 B：禁用整个 GameObject

1. 选择 `ShockwaveRing` GameObject
2. 在 Inspector 顶部，**取消勾选** GameObject 的激活状态

**优点**：
- 完全禁用，不会造成任何影响

#### 选项 C：删除 GameObject（如果不需要）

如果完全不需要 GameObject，可以删除它。

**注意**：如果删除 GameObject，需要确保 `ShockwaveController` 脚本在其他地方运行，或者创建一个空的 GameObject 来挂载脚本。

## 🔧 推荐方案

### 方案 1：保留 GameObject，禁用 Renderer（最简单）

1. **禁用 Mesh Renderer**：
   - 选择 `ShockwaveRing` GameObject
   - 取消勾选 `Mesh Renderer` 组件

2. **保留 ShockwaveController**：
   - `ShockwaveController` 脚本继续运行
   - 控制 Material 的属性（距离、强度等）
   - Material 由 Renderer Feature 使用，而不是 Mesh Renderer

3. **结果**：
   - ✅ Renderer Feature 使用 Material 渲染效果
   - ✅ GameObject 不渲染（不会遮挡物体）
   - ✅ `ShockwaveController` 继续控制效果

### 方案 2：创建空的 GameObject 挂载脚本

如果不想保留原来的 GameObject：

1. **创建新的 GameObject**：
   - 右键点击 Hierarchy → `Create Empty`
   - 命名为 `ShockwaveController`

2. **添加脚本**：
   - 添加 `ShockwaveController` 组件
   - 设置 `shockwaveMaterial` 引用为新 Material

3. **删除或禁用旧的 GameObject**：
   - 删除或禁用 `ShockwaveRing` GameObject

## 📝 总结

### 需要做的：

1. ✅ **ShockwaveController 的 Material 引用**：改为新的 Material
2. ✅ **禁用或删除 GameObject 的 Mesh Renderer**：不再需要它来渲染

### 不需要做的：

1. ❌ **不需要改 Mesh Renderer 的 Material**：因为 Renderer 会被禁用
2. ❌ **不需要删除 ShockwaveController 脚本**：它仍然需要控制 Material 属性

## ⚠️ 重要提示

1. **Renderer Feature 使用 Material**：
   - Renderer Feature 直接从 `ShockwaveController` 控制的 Material 读取属性
   - 不需要通过 GameObject 的 Renderer

2. **Material 属性控制**：
   - `ShockwaveController` 通过 `shockwaveMaterial.SetFloat()` 等方法设置属性
   - 这些属性会被 Renderer Feature 的 shader 读取

3. **双重渲染问题**：
   - 如果不禁用 GameObject 的 Renderer，可能会造成双重渲染
   - 效果会叠加，看起来不正常

## 🎯 最终配置

```
ShockwaveRing GameObject（Mesh Renderer 已禁用）
  └── ShockwaveController 脚本
      └── shockwaveMaterial = 新的 Material（SG_Shockwave_RendererFeature）

Renderer Feature
  └── 使用同一个 Material 渲染全屏效果
```

这样配置后：
- ✅ Renderer Feature 渲染效果
- ✅ GameObject 不渲染（不遮挡物体）
- ✅ `ShockwaveController` 控制效果参数




