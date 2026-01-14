# 创建正确的冲击波 Shader Graph 指南

## ⚠️ 重要前提：确保 Scene Color 能读取 wrapgrid

**Scene Color Node 只能读取不透明队列（Opaque Queue）的内容！**

如果你的 wrapgrid 背景使用的是透明队列，需要先修改：

### 修改 wrapgrid 的 Shader（`Assets/Art/Materials/UnlitWarpGrid.shader`）

1. 将 `Queue="Transparent"` 改为 `Queue="Geometry"`
2. 将 `ZWrite Off` 改为 `ZWrite On`
3. 注释掉或删除 `Blend` 行（不透明队列不需要混合）
4. 将 `RenderType="Transparent"` 改为 `RenderType="Opaque"`

### 确保主相机启用 Opaque Texture

1. 在场景中选择 **Main Camera**
2. 在 Inspector 中找到 **Universal Additional Camera Data** 组件
3. 将 **Requires Opaque Texture** 设置为 **On**（不是 Auto）

**注意**：如果 wrapgrid 必须是透明的（比如有半透明效果），这个方案可能不适用，需要其他方法。

---

## 步骤 1：创建新的 Shader Graph

1. 在 Unity 编辑器中，右键点击 `Assets/timestop` 文件夹
2. 选择 `Create > Shader Graph > URP > Unlit Shader Graph`
3. 命名为 `SG_Shockwave_New`

## 步骤 2：设置 Graph Settings

1. 打开新创建的 Shader Graph
2. 在右侧的 **Graph Inspector** 面板中，点击 **Graph Settings** 标签
3. 在 **Universal** 部分设置：
   - **Surface Type**: `Transparent`
   - **Blending Mode**: `Alpha`
   - **Render Face**: `Both`
   - **Depth Write**: `Off`（这就是 ZWrite）
   - **Depth Test**: `LEqual`（这就是 ZTest，LessEqual 的缩写）
   - **Cast Shadows**: 可以取消勾选（屏幕空间效果通常不需要阴影）

## 步骤 3：添加属性（Properties）

在 Blackboard 中添加以下属性：

1. **RippleDistanceFromCenter** (Float)
   - Reference: `_RippleDistanceFromCenter`
   - Default: 0
   - Range: 0 to 2

2. **RippleStrength** (Float)
   - Reference: `_RippleStrength`
   - Default: 0
   - Range: -0.2 to 0.2

3. **RippleWidth** (Float)
   - Reference: `_RippleWidth`
   - Default: 0.1
   - Range: 0 to 0.5

4. **CenterPoint** (Vector2)
   - Reference: `_CenterPoint`
   - Default: (0.5, 0.5)

## 步骤 4：构建 Shader Graph 节点连接

### 4.1 读取背景（Scene Color）

1. 添加 **Screen Position** 节点
   - Mode: `Default`（不是 Raw）
   
2. 添加 **Scene Color** 节点
   - 将 **Screen Position** 的 `Out` 连接到 **Scene Color** 的 `UV` 输入
   - 这样 Scene Color 就能读取相机渲染的画面（包括 wrapgrid）
   - **注意**：Scene Color 只能读取不透明队列的内容，确保 wrapgrid 已改为不透明队列

### 4.2 计算冲击波效果

1. 添加 **Screen Position** 节点（第二个，用于计算距离）
   - Mode: `Default`
   - 将 `Out` 连接到 **Subtract** 节点的 `A` 输入

2. 添加 **Property: CenterPoint** 节点
   - 将 `Out` 连接到 **Subtract** 节点的 `B` 输入

3. 添加 **Subtract** 节点
   - 输出：屏幕坐标相对于中心点的偏移

4. 添加 **Length** 节点
   - 将 **Subtract** 的 `Out` 连接到 **Length** 的 `In`
   - 输出：到中心的距离

5. 添加 **Property: RippleDistanceFromCenter** 节点
   - 连接到第二个 **Subtract** 节点的 `B` 输入

6. 添加第二个 **Subtract** 节点
   - `A`: **Length** 的输出
   - `B`: **RippleDistanceFromCenter**
   - 输出：相对距离

7. 添加 **Absolute** 节点
   - 将第二个 **Subtract** 的输出连接到 **Absolute** 的输入
   - 确保距离为正数

8. 添加 **Property: RippleWidth** 节点
   - 连接到 **Smoothstep** 的 `Edge1` 和 `Edge2` 输入
   - `Edge2` 可以通过 **Add** 节点加上一个小的偏移值（比如 0.01）

9. 添加 **Smoothstep** 节点
   - `In`: **Absolute** 的输出
   - `Edge1`: **RippleWidth**
   - `Edge2`: **RippleWidth + 0.01**（使用 Add 节点）
   - 输出：平滑的冲击波环形遮罩（0-1）

10. 添加 **Property: RippleStrength** 节点
    - 连接到 **Multiply** 节点的 `B` 输入

11. 添加 **Multiply** 节点
    - `A`: **Smoothstep** 的输出
    - `B`: **RippleStrength**
    - 输出：冲击波效果强度

### 4.3 混合效果

1. **将标量转换为 Vector3**：
   - 添加 **Combine** 节点（或 **Vector3** 节点）
   - 将 **Multiply** 的输出（Float，标量值）连接到 **Combine** 的三个输入：
     - `R`: **Multiply** 的输出
     - `G`: **Multiply** 的输出（直接连接，Unity 会自动复制）
     - `B`: **Multiply** 的输出（直接连接，Unity 会自动复制）
   - 或者：将 **Multiply** 的输出连接到 **Combine** 的 `R`，然后 Unity 会自动将同一个值复制到 `G` 和 `B`
   - **Combine** 输出：Vector3（冲击波效果作为颜色值）

2. 添加 **Add** 节点
   - **重要**：`A` 输入连接 **Scene Color** 的 `Out`（RGB，Vector3）- 这是背景
   - **重要**：`B` 输入连接 **Combine** 的输出（Vector3）- 这是冲击波效果
   - 输出：Scene Color + 冲击波效果（背景变亮）
   - **注意**：不要直接将 Float 连接到 Add 节点，必须先用 Combine 转换为 Vector3

3. 将 **Add** 的输出连接到 **Fragment** 的 `Base Color` 输入

4. **Fragment** 的 `Alpha` 输入设置为 1（或根据需求调整）

## ⚠️ 常见问题排查

### 问题 1：冲击波没有效果

**可能原因和解决方法：**

1. **Float 没有转换为 Vector3**
   - ❌ 错误：`Multiply` 输出直接连接到 `Add` 节点
   - ✅ 正确：`Multiply` → `Combine` → `Add`
   - 检查：确保 `Combine` 节点在连接路径中，并且 `Multiply` 的输出连接到 `Combine` 的 `R`、`G`、`B` 输入

2. **Scene Color 的 UV 没有连接**
   - ❌ 错误：`Scene Color` 的 `UV` 输入为空
   - ✅ 正确：`Screen Position (Default)` 的 `Out` 连接到 `Scene Color` 的 `UV` 输入
   - 检查：确保 `Scene Color` 节点有 UV 输入连接

3. **连接顺序错误**
   - ❌ 错误：`Add` 的 `A` 是冲击波，`B` 是 Scene Color
   - ✅ 正确：`Add` 的 `A` 是 Scene Color（背景），`B` 是冲击波效果
   - 检查：`Add` 节点的连接顺序

4. **RippleStrength 值为 0**
   - 检查：在 Material 的 Inspector 中，`_RippleStrength` 的值是否为 0
   - 解决方法：确保 `ShockwaveController` 脚本正确设置了 `strength` 值（默认 0.05）

5. **主相机没有启用 Opaque Texture**
   - 检查：Main Camera 的 `Universal Additional Camera Data` → `Requires Opaque Texture` 是否为 `On`
   - 解决方法：设置为 `On`（不是 Auto）

6. **wrapgrid 使用透明队列**
   - 检查：`UnlitWarpGrid.shader` 的 `Queue` 是否为 `Geometry`（不透明）
   - 解决方法：改为 `Queue="Geometry"` 和 `ZWrite On`

### 问题 2：冲击波效果太弱或太强

- 调整 `RippleStrength` 的值（范围：-0.2 到 0.2）
- 正数 = 变亮，负数 = 变暗
- 默认值 0.05 通常比较合适

## 步骤 5：关键注意事项

1. **Scene Color 的 UV 输入必须连接 Screen Position (Default)**
   - 这样才能正确读取相机渲染的画面

2. **冲击波效果应该是标量值，需要转换为 Vector3 才能与 Scene Color 相加**
   - 使用 **Combine** 节点：`(shockwaveValue, shockwaveValue, shockwaveValue)`

3. **确保主相机的 `Requires Opaque Texture` 设置为 `On`**
   - 这样 Scene Color 才能读取到背景

4. **wrapgrid 的 Render Queue 应该在不透明队列中**
   - 如果 wrapgrid 使用透明材质，Scene Color 可能读取不到

## 步骤 6：测试

1. 保存 Shader Graph
2. 创建新材质，使用这个 Shader
3. 将材质应用到 ShockwaveRing
4. 运行游戏，按 E 键测试

## 正确的逻辑流程总结：

```
Screen Position (Default) → Scene Color (UV) → 读取背景
                ↓
Screen Position (Default) → Subtract (CenterPoint) → Length → Subtract (Distance) → Absolute → Smoothstep → Multiply (Strength) → Combine (标量转Vector3)
                ↓
Scene Color + 冲击波效果 (Vector3) → Add → BaseColor
```

