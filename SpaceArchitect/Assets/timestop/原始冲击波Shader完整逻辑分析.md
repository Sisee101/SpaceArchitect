# 原始冲击波 Shader Graph 完整逻辑分析

## 🔍 关键发现

通过分析原始的 `SG_Shockwave.shadergraph`，发现了以下**关键特性**：

### 1. **两个 Screen Position 节点**

原始版本使用了**两个不同的 Screen Position 节点**：

- **Screen Position (Raw)** - `m_ScreenSpaceType: 1`
  - 用于计算距离（连接到 Subtract → Normalize → Multiply）
  
- **Screen Position (Default)** - `m_ScreenSpaceType: 0`
  - 用于 Scene Color 的 UV（连接到 Scene Color 的 UV 输入）

### 2. **Normalize 节点的特殊用法**

**Normalize 的输出连接到 Multiply 的 B 输入**，而不是用于距离计算！

**连接流程：**
```
Screen Position (Raw) → Subtract (CenterPoint) → Normalize → Multiply (B)
                                                              ↑
                                                      Smoothstep → Multiply (A)
```

这意味着：
- `Normalize` 的输出（归一化的方向向量）被用作 `Multiply` 的 B 输入
- `Smoothstep` 的输出被用作 `Multiply` 的 A 输入
- 结果：冲击波效果会沿着方向向量进行某种变换

### 3. **完整的节点连接流程**

**路径 1：计算距离（用于 Smoothstep）**
```
Screen Position (Raw)
└─> Subtract (A)
    └─> CenterPoint (B)
        └─> Subtract (Out) → Length (In) → Length (Out)
            └─> Subtract (A)
                └─> RippleDistanceFromCenter (B)
                    └─> Subtract (Out) → Absolute (In) → Absolute (Out)
                        └─> Smoothstep (In)
                            ├─> Edge1: RippleWidth
                            └─> Edge2: RippleWidth (可能通过某种计算)
                                └─> Smoothstep (Out) → Multiply (A)
```

**路径 2：归一化方向向量（用于 Multiply）**
```
Screen Position (Raw)
└─> Subtract (A)
    └─> CenterPoint (B)
        └─> Subtract (Out) → Normalize (In) → Normalize (Out)
            └─> Multiply (B)
```

**路径 3：读取背景**
```
Screen Position (Default)
└─> Scene Color (UV) → Scene Color (Out)
    └─> Add (A)
```

**路径 4：最终混合**
```
Multiply (A: Smoothstep, B: Normalize) → Add (B)
                                          ↑
                                    Scene Color → Add (A)
                                        └─> Add (Out) → Fragment (Base Color)
```

### 4. **关键设置**

- **Surface Type**: `Transparent` (m_SurfaceType: 1)
- **Blending Mode**: `Alpha` (m_AlphaMode: 0)
- **Render Face**: `Both` (m_RenderFace: 2)
- **ZTest**: `LEqual` (m_ZTestMode: 4)
- **ZWrite**: `Off` (m_ZWriteControl: 0)

---

## 🎯 原始逻辑的核心思想

**原始版本的冲击波效果不是简单的距离计算，而是：**

1. **计算距离** → 用于 `Smoothstep` 创建环形遮罩
2. **归一化方向向量** → 用于 `Multiply` 创建方向性效果
3. **将两者相乘** → 创建既有距离衰减又有方向性的效果
4. **叠加到背景** → 最终显示

**这种设计让冲击波效果更加自然和动态！**

---

## 📋 如果要完全照搬原始版本

需要确保：

1. ✅ **使用两个 Screen Position 节点**
   - 一个 Raw 模式（用于计算）
   - 一个 Default 模式（用于 Scene Color）

2. ✅ **Normalize 的输出连接到 Multiply 的 B**
   - 不是用于距离计算
   - 而是用于创建方向性效果

3. ✅ **完整的连接流程**（如上所述）

4. ✅ **Graph Settings**
   - Surface Type: Transparent
   - Blending Mode: Alpha
   - Render Face: Both
   - ZTest: LEqual
   - ZWrite: Off

---

## ⚠️ 重要提示

**原始版本的逻辑比较复杂，Normalize 的用法很特殊。**

如果你想要完全照搬，需要：
1. 在 Unity 编辑器中打开原始的 `SG_Shockwave.shadergraph`
2. 检查所有节点连接
3. 确保 Material 使用这个 shader

**或者，我可以帮你创建一个完全按照原始逻辑的新版本！**

