# 连接到 BaseColor 的说明

## 📋 你的连接分析

根据你的截图，我看到：

### ✅ 正确的部分

1. **纹理采样部分**：
   - `MainTex` → `Sample Texture 2D` 的 `Texture` ✓
   - `Screen Position` → `Split` → R, G → `Combine` → `Sample Texture 2D` 的 `UV` ✓
   - `Sample Texture 2D` 的 `RGBA` 输出 → `Add` 节点 ✓

2. **冲击波计算部分**：
   - 距离计算 → `Smoothstep` → `Multiply` → `RippleStrength` → `Normalize` → `Multiply` ✓

### ⚠️ 需要修改的部分

**问题**：你使用了 `Scene Color` 节点，但这是 Renderer Feature，应该使用 `Sample Texture 2D` 的输出！

## 🔧 正确的连接方法

### 最终输出连接

```
Sample Texture 2D (RGBA 输出)
  ↓
Add 节点的 A 输入（或 B 输入）

冲击波效果 (Vector3)
  ↓
Add 节点的 B 输入（或 A 输入）

Add 节点 (Out 输出)
  ↓
Fragment 的 Base Color 输入
```

## 📝 详细步骤

### 1. 移除 Scene Color 节点

**删除** `Scene Color` 节点，因为 Renderer Feature 使用 `_MainTex` 来接收屏幕内容。

### 2. 连接 Add 节点

**Add 节点的两个输入**：

1. **A 输入**（或 B 输入）：
   - 连接 `Sample Texture 2D` 的 `RGBA` 输出
   - 这是屏幕内容（包括透明物体）

2. **B 输入**（或 A 输入）：
   - 连接冲击波效果的输出（Vector3）
   - 这是从 `Multiply` 节点输出的冲击波强度

### 3. 冲击波效果的连接

你的冲击波计算：
```
Smoothstep → Multiply (RippleStrength) → Normalize → Multiply
```

**问题**：`Normalize` 节点可能不需要，或者需要调整。

**建议的连接**：
```
Smoothstep → Multiply (RippleStrength) → Multiply (标量转 Vector3)
```

### 4. 将标量转换为 Vector3

如果 `Multiply` 输出的是标量（单个数值），需要转换为 Vector3：

```
Multiply (标量输出)
  ↓
Combine 节点
  - R 输入：连接 Multiply 的输出
  - G 输入：连接 Multiply 的输出（复制）
  - B 输入：连接 Multiply 的输出（复制）
  ↓
Combine 的 Out 输出（Vector3 RGB）
  ↓
Add 节点的 B 输入
```

### 5. 最终连接到 Base Color

```
Add 节点 (Out 输出)
  ↓
Fragment 的 Base Color 输入
```

## 🎯 完整的连接流程

```
1. 屏幕内容采样：
   Screen Position → Split → Combine → Sample Texture 2D (UV)
   MainTex → Sample Texture 2D (Texture)
   Sample Texture 2D (RGBA) → Add 节点的 A 输入

2. 冲击波效果计算：
   Screen Position (Raw) → Subtract → Length → Subtract → Absolute
   → Smoothstep → Multiply (RippleStrength) → Multiply (标量)
   → Combine (R, G, B) → Vector3
   → Add 节点的 B 输入

3. 最终输出：
   Add 节点 (Out) → Fragment 的 Base Color
```

## ⚠️ 重要提示

1. **不要使用 Scene Color 节点**：
   - Renderer Feature 使用 `_MainTex` 接收屏幕内容
   - `Scene Color` 节点在 Renderer Feature 中不起作用

2. **Add 节点的输入顺序**：
   - A 输入：屏幕内容（RGBA）
   - B 输入：冲击波效果（Vector3）
   - 输出：混合后的颜色（RGBA）

3. **标量转 Vector3**：
   - 如果冲击波效果是标量，需要用 `Combine` 节点转换为 Vector3
   - 将同一个值复制到 R, G, B 三个分量

## 🔍 检查清单

- [ ] 移除了 `Scene Color` 节点
- [ ] `Sample Texture 2D` 的 `RGBA` 连接到 `Add` 节点
- [ ] 冲击波效果（Vector3）连接到 `Add` 节点
- [ ] `Add` 节点的 `Out` 连接到 `Fragment` 的 `Base Color`
- [ ] `Fragment` 的 `Alpha` 连接到值 `1`（或从 `Sample Texture 2D` 的 `A` 输出）

## 📸 可视化连接

```
┌─────────────────┐
│ Sample Texture  │
│     2D          │───RGBA──→┌─────┐
└─────────────────┘          │ Add │───Out──→ Base Color
                              └─────┘
┌─────────────────┐              ↑
│ 冲击波效果      │              │
│ (Vector3)       │──────────────┘
└─────────────────┘
```

