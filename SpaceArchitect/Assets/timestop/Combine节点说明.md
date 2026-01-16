# Combine 节点说明

## 📋 什么是 Combine 节点？

`Combine` 节点用于将多个标量（单个数值）组合成向量（Vector2, Vector3, 或 Vector4）。

## 🔢 Combine 节点的输入和输出

### 输入端口

`Combine` 节点有多个输入端口：
- **R**（或 X）：第一个分量
- **G**（或 Y）：第二个分量
- **B**（或 Z）：第三个分量（可选）
- **A**（或 W）：第四个分量（可选）

### 输出端口

**`Combine` 节点只有一个输出端口，叫做 `Out`**

输出的类型取决于你连接了多少个输入：
- 只连接 **R** 和 **G** → 输出 **Vector2**（RG）
- 连接 **R, G, B** → 输出 **Vector3**（RGB）
- 连接 **R, G, B, A** → 输出 **Vector4**（RGBA）

## 🎯 在你的冲击波 Shader 中的使用

### 示例 1：组合 XY 坐标（用于 UV）

```
Split 节点
  → R 输出（X 坐标） → Combine 的 R 输入
  → G 输出（Y 坐标） → Combine 的 G 输入
  → Combine 的 Out 输出（Vector2） → Sample Texture 2D 的 UV 输入
```

**结果**：Combine 输出 Vector2（RG），这就是 UV 坐标。

### 示例 2：组合冲击波效果（RGB）

```
Multiply 节点（标量输出）
  → Combine 的 R 输入
  → Combine 的 G 输入（复制同样的值）
  → Combine 的 B 输入（复制同样的值）
  → Combine 的 Out 输出（Vector3 RGB） → Add 节点
```

**结果**：Combine 输出 Vector3（RGB），用于冲击波效果的颜色。

## ⚠️ 重要提示

1. **输出端口只有一个**：
   - 叫做 `Out`
   - 输出类型取决于连接的输入数量

2. **不需要选择输出类型**：
   - Unity 会自动根据你连接的输入数量决定输出类型
   - 连接 2 个输入 → Vector2
   - 连接 3 个输入 → Vector3
   - 连接 4 个输入 → Vector4

3. **在 Shader Graph 中**：
   - 右键点击 `Combine` 节点
   - 可以看到输出端口只有一个：`Out`
   - 这个 `Out` 端口会根据输入自动调整类型

## 📝 实际连接示例

### 用于 UV 坐标（Vector2）

```
Screen Position → Split
  Split R → Combine R
  Split G → Combine G
  Combine Out → Sample Texture 2D UV
```

### 用于冲击波效果（Vector3）

```
Multiply（标量） → Combine R
Multiply（标量） → Combine G（复制）
Multiply（标量） → Combine B（复制）
Combine Out（Vector3） → Add 节点
```

## 🔍 如何查看输出类型

在 Shader Graph 中：
1. 将鼠标悬停在 `Combine` 节点的 `Out` 端口上
2. 会显示输出类型：`Vector2`、`Vector3` 或 `Vector4`
3. 或者查看连接线的颜色：
   - 绿色 = Vector2
   - 蓝色 = Vector3
   - 紫色 = Vector4

## ✅ 总结

- **输出端口**：只有一个，叫做 `Out`
- **输出类型**：自动根据输入数量决定（Vector2/Vector3/Vector4）
- **使用方法**：直接连接 `Out` 端口到下一个节点即可






