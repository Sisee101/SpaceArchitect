# 检查 Shader Graph 连接

## ✅ 正确的连接（根据你的截图）

### 1. 屏幕内容采样部分 ✓

```
MainTex → Sample Texture 2D (Texture 输入)
Split (R, G) → Combine (RG) → Sample Texture 2D (UV 输入)
Sample Texture 2D (RGBA) → Add 节点的 A 输入
```

**这部分看起来正确！**

### 2. 冲击波效果部分 ✓

```
Multiply → Multiply → Combine (R, G, B 都连接同一个值)
Combine (RGBA) → Add 节点的 B 输入
```

**这部分也看起来正确！**

### 3. 最终输出 ✓

```
Add (Out) → Fragment 的 Base Color
```

**这部分也正确！**

## ⚠️ 需要注意的问题

### 问题 1：底部 Combine 节点显示黑色

**观察**：底部 `Combine` 节点的预览显示**黑色**

**可能的原因**：
1. 冲击波强度为 0（初始状态，正常）
2. 冲击波效果计算有问题

**解决方法**：
- 如果冲击波强度为 0，这是正常的（初始状态）
- 按 B 键触发冲击波后，应该会显示效果
- 如果触发后还是黑色，检查冲击波计算部分

### 问题 2：检查冲击波计算流程

确保冲击波计算流程完整：

```
Screen Position (Raw) → Subtract (CenterPoint) → Length
  → Subtract (RippleDistanceFromCenter) → Absolute
  → Smoothstep (Edge1: 0, Edge2: RippleWidth)
  → Multiply (RippleStrength)
  → Multiply (如果需要)
  → Combine (R, G, B)
```

**检查点**：
- 确保 `Smoothstep` 的 `Edge1` 和 `Edge2` 正确设置
- 确保 `Multiply` 的 `B` 输入连接了 `RippleStrength`
- 确保 `Combine` 的 R, G, B 都连接了**同一个值**

## 🔍 快速检查清单

### 连接检查

- [x] MainTex → Sample Texture 2D (Texture)
- [x] Split (R, G) → Combine → Sample Texture 2D (UV)
- [x] Sample Texture 2D (RGBA) → Add (A)
- [x] Multiply → Multiply → Combine (R, G, B)
- [x] Combine (RGBA) → Add (B)
- [x] Add (Out) → Fragment (Base Color)

### 需要确认

- [ ] 冲击波计算流程是否完整（Screen Position → Length → Smoothstep → Multiply）
- [ ] `Smoothstep` 的 `Edge1` 和 `Edge2` 是否正确
- [ ] `Multiply` 的 `B` 输入是否连接了 `RippleStrength`
- [ ] `Combine` 的 R, G, B 是否都连接了**同一个值**

## 🎯 测试方法

1. **保存 Shader Graph**
2. **运行游戏**
3. **按 B 键触发冲击波**
4. **观察效果**：
   - 如果显示正常：连接正确 ✓
   - 如果还是彩色：检查 `Combine` 节点的 R, G, B 输入
   - 如果没效果：检查冲击波计算流程

## 📝 总结

根据你的截图，**连接看起来基本正确**！

主要检查点：
1. ✅ `Combine` 的 R, G, B 都连接了同一个值（你已经做到了）
2. ✅ `Add` 节点正确连接了屏幕内容和冲击波效果
3. ✅ 最终输出连接到 `Fragment` 的 `Base Color`

如果底部 `Combine` 节点显示黑色，可能是：
- 初始状态（强度为 0），这是正常的
- 触发冲击波后应该会显示效果

**如果触发后效果正常，说明连接完全正确！**

