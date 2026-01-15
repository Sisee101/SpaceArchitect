# 诊断：添加 Normalize 后冲击波没有效果

## ⚠️ 可能的问题

添加 `Normalize` 节点后没有效果，可能是以下原因：

### 问题 1：Normalize 改变了数值范围 ❌

**问题：**
- `Normalize` 会将向量归一化，使其长度为 1
- 但 `Length` 节点期望的是**未归一化的向量**来计算实际距离
- 如果 `Normalize` → `Length`，`Length` 的输出永远是 1（或接近 1），导致后续计算错误

**错误连接：**
```
Screen Position → Subtract (CenterPoint) → Normalize → Length → ...
```
❌ 这样 `Length` 的输出永远是 1，无法正确计算距离

**正确理解：**
- `Normalize` 在原始版本中可能**不是**用在距离计算上
- 或者原始版本的逻辑与我们理解的不同

---

### 问题 2：Normalize 的位置不对

**可能的情况：**
- `Normalize` 应该用在**其他地方**，而不是在距离计算路径上
- 或者原始版本根本没有用 `Normalize` 来计算距离

---

## ✅ 解决方案

### 方案 1：移除 Normalize，恢复原来的连接（推荐先试这个）

**如果添加 Normalize 后没有效果，先恢复到之前能工作的版本：**

1. **移除 `Normalize` 节点**
2. **恢复原来的连接：**
   ```
   Screen Position → Subtract (CenterPoint) → Length → Subtract (RippleDistanceFromCenter) → Absolute → Smoothstep
   ```

3. **测试是否恢复效果**

---

### 方案 2：检查原始版本的真正逻辑

**重新检查原始版本，看看 `Normalize` 到底用在哪里：**

从原始版本的连接来看，`Normalize` 的输出连接到了 `Multiply` 的 B 输入，这很奇怪。可能：
- 原始版本的逻辑与我们理解的不同
- 或者 `Normalize` 用于其他目的（不是距离计算）

---

### 方案 3：用其他方法改善边缘平滑度（不依赖 Normalize）

**如果 Normalize 导致问题，可以用以下方法改善边缘：**

#### 方法 A：增加 Smoothstep 的过渡区间

**当前（边缘硬）：**
- `Edge1` = `RippleWidth`
- `Edge2` = `RippleWidth + 0.01`（过渡区间很小 = 0.01）

**改进（边缘平滑）：**
- `Edge1` = `RippleWidth * 0.9`
- `Edge2` = `RippleWidth * 1.1`（过渡区间 = `RippleWidth * 0.2`，更大）

**或者：**
- `Edge1` = `RippleDistanceFromCenter - RippleWidth * 0.5`
- `Edge2` = `RippleDistanceFromCenter + RippleWidth * 0.5`

#### 方法 B：使用双重 Smoothstep

1. 第一个 `Smoothstep` 输出连接到第二个 `Smoothstep` 的 `In`
2. 这样可以获得更平滑的曲线

#### 方法 C：调整 RippleWidth 的值

- 如果 `RippleWidth` 太小（如 0.01），边缘会很硬
- 尝试增加到 0.05 或 0.1

---

## 🔧 快速修复步骤

### 步骤 1：先恢复能工作的版本

1. **删除 `Normalize` 节点**
2. **恢复连接：**
   - `Subtract (Screen Position - CenterPoint)` → `Length`（直接连接，不经过 Normalize）
3. **测试是否恢复效果**

### 步骤 2：如果恢复了，尝试改善边缘（不依赖 Normalize）

1. **增加 Smoothstep 的过渡区间：**
   - 创建 `Add` 节点：`RippleWidth + 0.05`（而不是 0.01）
   - `Edge1` = `RippleWidth`
   - `Edge2` = `RippleWidth + 0.05`

2. **或者调整 Edge 的计算：**
   - `Edge1` = `RippleDistanceFromCenter - RippleWidth`
   - `Edge2` = `RippleDistanceFromCenter + RippleWidth`

### 步骤 3：测试效果

- 运行游戏，按 E 键
- 观察边缘是否更平滑

---

## 📋 检查清单

在添加 Normalize 之前，确保：

- [ ] **所有基本连接都正确：**
  - `Screen Position` → `Scene Color (UV)`
  - `Screen Position` → `Subtract (CenterPoint)` → `Length` → `Subtract (Distance)` → `Absolute` → `Smoothstep` → `Multiply (Strength)` → `Combine` → `Add (Scene Color)`

- [ ] **Smoothstep 的 Edge 参数正确：**
  - `Edge1` = `RippleWidth`
  - `Edge2` = `RippleWidth + 0.01`（或更大的值）

- [ ] **Multiply 的 B 输入连接 `RippleStrength`**（不是 CenterPoint）

- [ ] **Combine 的 R、G、B 都连接 `Multiply` 的输出**

- [ ] **最终的 Add 节点：**
  - `A` = `Scene Color`
  - `B` = `Combine` 的输出

---

## 🎯 建议

**如果添加 Normalize 后没有效果：**

1. **先移除 Normalize，恢复到能工作的版本**
2. **然后尝试增加 Smoothstep 的过渡区间来改善边缘**
3. **如果还不够平滑，可以尝试双重 Smoothstep**

**关键：先让效果能工作，再优化平滑度！**



