# Gravity Core 引力问题解决方案

## 🔍 问题诊断

### 问题现象
Gravity Core 添加了 `NBody` 和 `CoreDragger` 脚本后，在场景中**无法产生引力**，无法影响飞船。

### 根本原因
**`CoreDragger` 脚本会持续将 Gravity Core 从 GravityEngine 中移除**，导致无法产生引力。

原代码逻辑：
```csharp
void FixedUpdate()
{
    // 持续确保物体不在引力引擎中（防止被自动添加）
    if (nBody.engineRef != null)
    {
        RemoveFromGravityEngine();  // ❌ 这导致无法产生引力
    }
}
```

---

## ✅ 解决方案

### 已修复的 CoreDragger 脚本

现在 `CoreDragger` 支持**可选的引力功能**：

#### 新增功能：
1. **`produceGravity` 选项**（默认 `true`）
   - 如果为 `true`：物体保持在 GravityEngine 中，产生引力
   - 如果为 `false`：物体不在 GravityEngine 中，不产生引力

2. **自动添加 FixedObject 组件**
   - 当启用引力时，自动添加 `FixedObject` 组件
   - `FixedObject` 使物体位置固定，但质量仍然影响其他物体

3. **位置同步**
   - 拖拽时，自动更新 GravityEngine 中的位置
   - 确保引力中心跟随拖拽位置

---

## 🎮 使用方法

### 步骤1：检查组件

确保 Gravity Core 上有以下组件：
- ✅ `NBody` 组件
  - `mass > 0`（例如：50-100）
  - `lockToXYPlane = true`（如果使用）
- ✅ `CoreDragger` 组件
  - **`produceGravity = true`**（重要！）
- ✅ Tag 设置为 `"Core"`

### 步骤2：检查飞船状态

确保飞船处于 `Flying` 状态：
- 飞船必须已经发射（`ShipState.CurrentState == Flying`）
- 只有 `Flying` 状态的飞船才受引力影响

### 步骤3：测试

1. 运行游戏
2. 发射飞船（进入 `Flying` 状态）
3. 拖拽 Gravity Core
4. 观察飞船是否被引力影响

---

## 🔧 配置说明

### CoreDragger 参数

```csharp
[Header("引力设置")]
public bool produceGravity = true;  // ✅ 必须为 true 才能产生引力
```

**重要：**
- ✅ `produceGravity = true`：产生引力，可以拖拽
- ❌ `produceGravity = false`：不产生引力，只能拖拽

### NBody 参数

```csharp
public float mass = 50f;  // 质量，越大引力越强
```

**建议值：**
- 引力枢纽：50-100
- 行星：100-500
- 飞船：1-10

---

## 📊 工作原理

### 修复后的工作流程

```
游戏启动
  ↓
CoreDragger.Start()
  ├─ 检查 produceGravity
  ├─ 如果 true → EnsureInGravityEngine()
  │   ├─ 添加 FixedObject 组件
  │   └─ 添加到 GravityEngine
  └─ 如果 false → RemoveFromGravityEngine()

每帧更新 (FixedUpdate)
  ↓
如果 produceGravity = true
  ├─ 确保在 GravityEngine 中
  └─ 更新位置（如果被拖拽）

拖拽时 (OnMouseDrag)
  ↓
更新 Transform 位置
  ↓
更新 GravityEngine 中的位置
  └─ 确保引力中心跟随拖拽
```

### FixedObject 的作用

`FixedObject` 组件使物体：
- ✅ **位置固定**：不受其他物体引力影响
- ✅ **产生引力**：质量仍然影响其他物体
- ✅ **可拖拽**：位置由 `CoreDragger` 控制

---

## 🐛 调试步骤

### 1. 检查是否在 GravityEngine 中

```csharp
// 在 Console 中检查
Debug.Log($"Gravity Core engineRef: {nBody.engineRef != null}");
// 应该是 true
```

### 2. 检查质量

```csharp
Debug.Log($"Gravity Core 质量: {nBody.mass}");
// 应该 > 0
```

### 3. 检查飞船状态

```csharp
Debug.Log($"飞船状态: {shipState.CurrentState}");
// 应该是 Flying
```

### 4. 检查 produceGravity 设置

在 Inspector 中检查 `CoreDragger` 组件：
- `Produce Gravity` 应该勾选 ✅

---

## ⚠️ 常见问题

### 问题1：仍然无法产生引力

**检查清单：**
1. ✅ `produceGravity = true`？
2. ✅ `NBody.mass > 0`？
3. ✅ 飞船处于 `Flying` 状态？
4. ✅ GravityEngine 已初始化？
5. ✅ Gravity Core 的 Tag 是 `"Core"`？

### 问题2：拖拽后引力中心不更新

**原因：** 位置更新可能延迟

**解决：** 脚本已经在 `OnMouseDrag` 和 `FixedUpdate` 中更新位置，应该能正常工作。

### 问题3：飞船不受影响

**可能原因：**
1. 飞船质量太大，引力影响不明显
2. 距离太远，引力太弱
3. 飞船速度太快，引力影响被忽略

**解决：**
- 增大 Gravity Core 的质量
- 减小飞船质量
- 调整飞船初始速度

---

## 📝 技术细节

### 位置更新机制

```csharp
// 世界坐标 → 物理坐标
Vector3 physPos = worldPos / gravityEngine.physToWorldFactor;

// 更新 NBody
nBody.initialPhysPosition = physPos;

// 更新 FixedObject
fixedObject.SetPositionDouble(new Vector3d(physPos));

// 更新 GravityEngine 内部状态
gravityEngine.SetPositionDoubleV3(nBody, physPos3d);
```

### FixedObject 的工作原理

1. `PreEvolve()`：从 `nBody.initialPhysPosition` 读取位置
2. `Evolve()`：返回固定位置（不移动）
3. 质量仍然参与引力计算

---

## 🎯 推荐配置

### Gravity Core 设置

```
NBody:
- mass = 50f
- lockToXYPlane = true（如果使用）

CoreDragger:
- produceGravity = true ✅
- Tag = "Core"
```

### 测试场景

1. 创建一个 Gravity Core（质量 50）
2. 创建一个飞船（质量 1）
3. 发射飞船，速度 (5, 0, 0)
4. 拖拽 Gravity Core 到飞船附近
5. 观察飞船是否被引力影响

---

## 📈 效果预期

### 正常情况：
1. Gravity Core 产生引力 ✅
2. 飞船被引力影响，轨迹偏转 ✅
3. 拖拽 Gravity Core 时，引力中心跟随移动 ✅
4. 飞船持续受引力影响 ✅

### Console 日志：
```
Gravity Core GravityCore 已添加到引力引擎，质量: 50
```

---

## 🔄 与 GravityHubDeflector 的区别

| 特性 | CoreDragger | GravityHubDeflector |
|------|------------|---------------------|
| 产生引力 | ✅ 是（通过 GravityEngine） | ❌ 否（只调整速度） |
| 可拖拽 | ✅ 是 | ❌ 否 |
| 速度调整 | ❌ 否 | ✅ 是（方向偏转） |
| 使用场景 | 引力枢纽（可拖拽） | 引力枢纽（固定位置） |

**建议：**
- 需要**拖拽**的引力枢纽 → 使用 `CoreDragger`（`produceGravity = true`）
- 需要**方向偏转**的引力枢纽 → 使用 `GravityHubDeflector`
- 两者可以**同时使用**（但通常不需要）

---

## 📝 总结

### 修复内容：
1. ✅ 添加 `produceGravity` 选项
2. ✅ 自动添加 `FixedObject` 组件
3. ✅ 位置同步机制
4. ✅ 保持物体在 GravityEngine 中

### 使用方法：
1. 设置 `produceGravity = true`
2. 确保 `NBody.mass > 0`
3. 确保飞船处于 `Flying` 状态
4. 拖拽 Gravity Core，观察飞船被引力影响

### 关键点：
- **`produceGravity = true`** 是关键设置
- `FixedObject` 组件会自动添加
- 位置会自动同步到 GravityEngine

