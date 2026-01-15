# 修复 Shader Graph 连接问题

## 🔍 发现的问题

根据你的 Shader Graph，我发现了一个**关键问题**：

### ❌ 错误的连接

```
Smoothstep → Multiply (与 MainTex 相乘?) → Multiply (RippleStrength) → Combine
```

**问题**：
- 第一个 `Multiply` 节点将 `Smoothstep` 的输出与 `MainTex` 相乘
- 这是**错误的**！冲击波效果不应该与 MainTex 相乘
- 这会导致冲击波效果被纹理内容影响，产生颜色问题

### ✅ 正确的连接应该是

```
Smoothstep → Multiply (RippleStrength) → Combine (R, G, B) → Add
```

## 🔧 修复方法

### 步骤 1：移除错误的 Multiply 节点

1. **找到第一个 Multiply 节点**：
   - 它连接了 `Smoothstep` 的输出和 `MainTex`
   - **删除这个连接**

2. **正确的连接**：
   - `Smoothstep` 的输出 → 直接连接到第二个 `Multiply` 的 `A` 输入
   - 第二个 `Multiply` 的 `B` 输入 → 连接 `RippleStrength`

### 步骤 2：正确的连接流程

```
冲击波计算：
  Screen Position (Raw) → Subtract (0.5, 0.5) → Length
    → Subtract (RippleDistanceFromCenter) → Absolute
    → Smoothstep (RippleWidth)
    → Multiply (RippleStrength)  ← 直接连接，不要与 MainTex 相乘
    → Combine (R, G, B 都连接同一个值)
    → Add 节点的 B 输入

屏幕内容采样：
  Screen Position (Default) → Split → Combine (R, G) → Sample Texture 2D (MainTex)
    → Add 节点的 A 输入

最终输出：
  Add (Out) → Fragment (Base Color)
```

## 📝 详细修复步骤

### 1. 断开错误的连接

1. **找到第一个 Multiply 节点**（连接 Smoothstep 和 MainTex 的那个）
2. **断开 MainTex 到 Multiply 的连接**
3. **将 Smoothstep 的输出直接连接到第二个 Multiply 的 A 输入**

### 2. 确保正确的连接

```
Smoothstep (Out)
  ↓
Multiply (A 输入)
  ↓
Multiply (B 输入) ← 连接 RippleStrength
  ↓
Combine (R, G, B 都连接 Multiply 的输出)
  ↓
Add (B 输入)
```

### 3. 屏幕内容连接

```
Sample Texture 2D (RGBA)
  ↓
Add (A 输入)
```

## ⚠️ 关键点

1. **不要将 Smoothstep 与 MainTex 相乘**：
   - 冲击波效果是独立的，不应该受纹理内容影响
   - 直接使用 Smoothstep 的输出

2. **冲击波效果流程**：
   - Smoothstep → Multiply (RippleStrength) → Combine (R, G, B) → Add

3. **屏幕内容流程**：
   - Sample Texture 2D → Add

4. **最终混合**：
   - Add 节点将屏幕内容和冲击波效果相加

## 🎯 修复后的完整流程

```
1. 冲击波计算（独立计算，不受纹理影响）：
   Screen Position (Raw) → Subtract → Length → Subtract → Absolute
   → Smoothstep → Multiply (RippleStrength) → Combine (R, G, B)
   → Add (B 输入)

2. 屏幕内容采样：
   Screen Position (Default) → Split → Combine → Sample Texture 2D
   → Add (A 输入)

3. 最终输出：
   Add (Out) → Fragment (Base Color)
```

## 🔍 检查清单

修复后检查：
- [ ] Smoothstep 的输出直接连接到 Multiply（不与 MainTex 相乘）
- [ ] Multiply 的 B 输入连接了 RippleStrength
- [ ] Combine 的 R, G, B 都连接了同一个值（Multiply 的输出）
- [ ] Sample Texture 2D 的 RGBA 连接到 Add 的 A 输入
- [ ] Combine 的 RGBA 连接到 Add 的 B 输入
- [ ] Add 的 Out 连接到 Fragment 的 Base Color

## 📝 总结

**主要问题**：
- 第一个 Multiply 节点错误地将 Smoothstep 与 MainTex 相乘
- 这会导致冲击波效果受纹理内容影响，产生颜色问题

**解决方法**：
- 断开 MainTex 到第一个 Multiply 的连接
- 将 Smoothstep 的输出直接连接到 Multiply（与 RippleStrength 相乘）
- 确保冲击波效果是独立计算的，不受纹理影响

修复后，冲击波应该是单色的（白色/灰色），不会因为屏幕内容而显示为不同颜色。




