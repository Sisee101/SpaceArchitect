# 修复 Subtract 节点四象限问题

## 🔍 问题分析

**现象**：
- `Subtract` 节点的预览显示为**四象限**（不是径向渐变）
- 左上：绿色
- 右上：黄绿色
- 左下：黑色
- 右下：红色

**问题**：
- 正确的预览应该是**径向渐变**（从中心向外的圆形渐变）
- 四象限说明输入连接有问题

## 🔧 问题原因

### 原因：输入维度不匹配

**观察**：
- `Subtract` 节点的输入显示为 `A(4)` 和 `B(4)`（Vector4）
- 但用于距离计算应该是 **Vector2**（XY 坐标）

**问题**：
- `Screen Position (Default)` 输出 Vector4
- `CenterPoint` 也是 Vector4
- 直接相减会导致四象限效果，而不是径向渐变

## 🔧 解决方法

### 正确的连接方式

需要提取 XY 坐标，然后进行 Subtract：

```
Screen Position (Default) → Split → Combine (R, G) → Vector2 (XY)
  ↓
Subtract (A 输入)

CenterPoint → Split → Combine (R, G) → Vector2 (XY)
  ↓
Subtract (B 输入)

Subtract (Out) → Length
```

### 详细步骤

#### 步骤 1：提取 Screen Position 的 XY

1. **Screen Position (Default)** 输出 Vector4
2. **添加 Split 节点**：
   - `Screen Position` → `Split` 的输入
   - `Split` 的 **R** 输出 = X 坐标
   - `Split` 的 **G** 输出 = Y 坐标
3. **添加 Combine 节点**：
   - `Split` 的 **R** → `Combine` 的 **R**（X 坐标）
   - `Split` 的 **G** → `Combine` 的 **G**（Y 坐标）
   - `Combine` 输出 Vector2（XY 坐标）

#### 步骤 2：提取 CenterPoint 的 XY

1. **CenterPoint 属性** 是 Vector4
2. **添加 Split 节点**：
   - `CenterPoint` → `Split` 的输入
   - `Split` 的 **R** 输出 = X 坐标
   - `Split` 的 **G** 输出 = Y 坐标
3. **添加 Combine 节点**：
   - `Split` 的 **R** → `Combine` 的 **R**（X 坐标）
   - `Split` 的 **G** → `Combine` 的 **G**（Y 坐标）
   - `Combine` 输出 Vector2（XY 坐标）

#### 步骤 3：连接 Subtract 节点

1. **Subtract 节点**：
   - **A 输入**：Screen Position 的 XY（Vector2）
   - **B 输入**：CenterPoint 的 XY（Vector2）
   - **输出**：Vector2（从中心到当前点的向量）

2. **预览应该显示**：
   - **径向渐变**（从中心向外的圆形渐变）
   - 中心是黑色（距离 0）
   - 边缘是白色（距离最大）

## 📝 完整的正确连接

```
1. Screen Position (Default) → Split → Combine (R, G) → Vector2 (XY)
   ↓
   Subtract (A 输入)

2. CenterPoint → Split → Combine (R, G) → Vector2 (XY)
   ↓
   Subtract (B 输入)

3. Subtract (Out: Vector2) → Length → Subtract (RippleDistanceFromCenter)
   → Absolute → Smoothstep (RippleWidth)
```

## ⚠️ 关键点

1. **不要直接使用 Vector4**：
   - `Screen Position` 和 `CenterPoint` 都是 Vector4
   - 需要提取 XY（Vector2）才能正确计算距离

2. **Subtract 的输入应该是 Vector2**：
   - A 输入：Screen Position 的 XY（Vector2）
   - B 输入：CenterPoint 的 XY（Vector2）
   - 输出：Vector2（从中心到当前点的向量）

3. **预览应该是径向渐变**：
   - 如果显示四象限，说明输入不对
   - 需要提取 XY 坐标

## 🔍 检查清单

修复后检查：
- [ ] Screen Position 使用 Split 提取 R (X) 和 G (Y)
- [ ] Screen Position 的 XY 通过 Combine 组合成 Vector2
- [ ] CenterPoint 使用 Split 提取 R (X) 和 G (Y)
- [ ] CenterPoint 的 XY 通过 Combine 组合成 Vector2
- [ ] Subtract 的 A 输入是 Vector2（Screen Position XY）
- [ ] Subtract 的 B 输入是 Vector2（CenterPoint XY）
- [ ] Subtract 的预览显示**径向渐变**（不是四象限）

## 📝 总结

**问题**：Subtract 节点显示四象限，而不是径向渐变

**原因**：直接使用 Vector4 进行 Subtract，而不是提取 XY (Vector2)

**解决方法**：
1. 提取 Screen Position 的 XY（Vector2）
2. 提取 CenterPoint 的 XY（Vector2）
3. 两个 Vector2 进行 Subtract
4. 预览应该显示径向渐变

修复后，Subtract 节点的预览应该显示**径向渐变**（从中心向外的圆形渐变），冲击波应该从屏幕中心向四周扩散。







