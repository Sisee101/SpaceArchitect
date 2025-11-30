# PlanetGravityCapture 与 GravityEngine 的交互分析

## 🔄 执行顺序和交互方式

### Unity 更新循环顺序

```
每帧执行顺序：
1. FixedUpdate (物理更新)
   └─ GravityEngine.PhysicsLoop()
      ├─ EvolveByTimestep()  ← 计算重力，更新速度
      └─ UpdateGameObjects() ← 更新位置

2. Update (逻辑更新)
   └─ PlanetGravityCapture.Update()
      ├─ DetectNearbySpaceships()
      └─ GuideCapturedSpaceships()
         └─ ge.SetVelocity() ← 调整速度
```

### 关键发现：**不是屏蔽，而是持续干预**

`PlanetGravityCapture` **不会屏蔽** GravityEngine 的重力计算，而是**在重力计算之后持续干预速度**。

---

## 📊 详细交互流程

### 第 N 帧：

```
1. GravityEngine.FixedUpdate()
   ├─ 读取当前速度（可能已被上一帧的 PlanetGravityCapture 调整过）
   ├─ 计算所有物体之间的引力
   ├─ 根据引力更新速度：v_new = v_old + a * dt
   └─ 更新位置：pos_new = pos_old + v_new * dt

2. PlanetGravityCapture.Update()
   ├─ 读取 GravityEngine 刚计算出的速度
   ├─ 分析速度（分解为径向和切向）
   ├─ 计算理想轨道速度
   ├─ 微调速度（Lerp 5%）
   └─ 写回 GravityEngine：ge.SetVelocity(newVelocity)
```

### 第 N+1 帧：

```
1. GravityEngine.FixedUpdate()
   ├─ 读取速度（已被 PlanetGravityCapture 调整过）
   ├─ 计算引力（仍然正常计算）
   ├─ 更新速度（基于调整后的速度 + 新的引力）
   └─ 更新位置

2. PlanetGravityCapture.Update()
   └─ 再次微调速度...
```

---

## 🔍 核心机制分析

### 1. SetVelocity 的实现

```csharp
// GravityEngine.SetVelocity()
public void SetVelocity(NBody nbody, Vector3 velocity) {
    worldState.SetVelocity3d(nbody, new Vector3d(velocity));
}
```

**作用：**
- 直接修改 GravityEngine 内部存储的速度值
- 这个值会被下一帧的重力计算使用

### 2. 速度的"竞争"关系

```
每帧的速度变化 = 重力影响 + 捕获脚本干预
                ↓           ↓
            (持续作用)   (持续微调)
```

**关键点：**
- ✅ 重力**持续作用**：每帧都会根据引力更新速度
- ✅ 捕获脚本**持续微调**：每帧都会调整速度，使其接近理想轨道
- ✅ 两者**同时作用**，形成动态平衡

---

## 🎯 实际效果

### 场景1：飞船接近行星

```
初始状态：
- 飞船速度：v = (10, 0, 0)  // 水平飞行
- 距离行星：r = 20

第1帧：
1. GravityEngine：计算引力，速度变为 v = (10, -0.5, 0)  // 被拉向行星
2. PlanetGravityCapture：检测到进入范围，微调速度
   - 理想切向速度：v_tangent = (0, 8, 0)
   - 微调后：v = Lerp((10, -0.5, 0), (0, 8, 0), 0.05) = (9.5, -0.025, 0)

第2帧：
1. GravityEngine：基于 v = (9.5, -0.025, 0) 计算，速度变为 v = (9.5, -0.6, 0)
2. PlanetGravityCapture：继续微调，v = (9.0, 0.3, 0)

...持续这个过程...

最终：
- 切向速度逐渐接近理想值
- 径向速度逐渐衰减
- 形成稳定的圆形轨道
```

### 场景2：飞船已在轨道上

```
如果飞船已经在理想轨道上：
- GravityEngine：维持轨道（引力 = 向心力）
- PlanetGravityCapture：检测到速度已接近理想值，微调很小
- 结果：稳定轨道，两个系统和谐工作
```

---

## ⚠️ 潜在问题和冲突

### 1. **执行顺序依赖**

```csharp
// PlanetGravityCapture 在 Update 中运行
void Update() {
    // 假设 GravityEngine 的 FixedUpdate 已经执行
    ge.SetVelocity(...);  // 修改速度
}
```

**问题：**
- 如果 GravityEngine 的 `updateMode = UPDATE`，两者都在 Update 中运行
- 执行顺序不确定，可能导致冲突

**解决方案：**
- 确保 GravityEngine 使用 `FIXED_UPDATE` 模式（默认）
- 或者使用 `ScriptExecutionOrder` 设置执行顺序

### 2. **速度覆盖问题**

```csharp
// PlanetGravityCapture 每帧都设置速度
ge.SetVelocity(info.nbody, newVelocity);

// 但如果飞船同时被其他脚本控制（如玩家输入）
// 可能会产生冲突
```

**影响：**
- 如果玩家手动控制飞船，捕获脚本的速度调整可能会被覆盖
- 或者捕获脚本会覆盖玩家的输入

### 3. **与 lockToXYPlane 的交互**

```csharp
// GravityEngine.UpdateGameObjects()
if (nbody.lockToXYPlane) {
    velocity.z = 0f;  // 强制 Z=0
    worldState.SetVelocity3d(nbody, vel3d);
}

// PlanetGravityCapture 之后在 Update 中运行
ge.SetVelocity(info.nbody, newVelocity);  // 可能包含 Z 分量
```

**结果：**
- `lockToXYPlane` 在 FixedUpdate 中强制 Z=0
- `PlanetGravityCapture` 在 Update 中可能设置 Z≠0 的速度
- 但下一帧 FixedUpdate 又会强制 Z=0
- **实际上 Z 分量会被持续清零，不影响 XY 平面的引导**

---

## 🔧 优化建议

### 1. 使用 FixedUpdate 而不是 Update

```csharp
// 当前实现
void Update() {
    // 在逻辑更新中运行
}

// 建议改为
void FixedUpdate() {
    // 在物理更新中运行，与 GravityEngine 同步
}
```

**好处：**
- 与 GravityEngine 的物理更新同步
- 避免执行顺序问题
- 更符合物理系统的设计

### 2. 检查执行顺序

```csharp
// 在 PlanetGravityCapture 中
void Update() {
    // 确保在 GravityEngine 更新之后执行
    if (ge.GetPhysicalTime() == lastPhysicsTime) {
        return;  // 物理还没更新，跳过
    }
    lastPhysicsTime = ge.GetPhysicalTime();
    // ... 执行引导逻辑
}
```

### 3. 考虑使用速度增量而不是直接设置

```csharp
// 当前：直接设置速度
ge.SetVelocity(info.nbody, newVelocity);

// 建议：添加速度增量
Vector3 velocityDelta = newVelocity - currentVelocity;
ge.AddVelocity(info.nbody, velocityDelta);  // 需要 GravityEngine 支持
```

---

## 📈 性能影响

### 当前实现的影响：

1. **每帧查找飞船**：
   ```csharp
   GameObject[] spaceships = GameObject.FindGameObjectsWithTag(spaceshipTag);
   ```
   - 每 0.1 秒执行一次
   - 如果飞船数量多，可能影响性能

2. **每帧设置速度**：
   ```csharp
   ge.SetVelocity(info.nbody, newVelocity);
   ```
   - 直接修改 GravityEngine 的内部状态
   - 对性能影响较小

3. **速度计算**：
   - 速度分解、Lerp 插值等计算
   - 计算量小，影响可忽略

---

## 🎮 实际使用建议

### 适用场景：
- ✅ 帮助玩家更容易进入轨道
- ✅ 自动调整飞船速度，减少手动操作
- ✅ 提供更平滑的轨道捕获体验

### 不适用场景：
- ❌ 需要精确物理模拟（会干扰自然重力）
- ❌ 需要完全自然的重力系统
- ❌ 多体问题（多个捕获脚本可能冲突）

### 参数调整：
- **`orbitGuidanceStrength = 0.05`**：温和引导，适合大多数情况
- **`orbitGuidanceStrength = 0.01`**：非常温和，几乎不影响自然运动
- **`orbitGuidanceStrength = 0.1`**：较强引导，飞船更快进入轨道

---

## 📝 总结

### PlanetGravityCapture 对 GravityEngine 的处理方式：

1. **不是屏蔽**：重力计算正常进行
2. **持续干预**：每帧微调速度，使其接近理想轨道
3. **动态平衡**：重力和引导同时作用，形成稳定轨道
4. **执行顺序**：在 GravityEngine 物理更新之后执行

### 关键机制：

```
重力计算 → 更新速度 → 捕获脚本微调 → 下一帧重力计算 → ...
   ↓           ↓            ↓              ↓
持续作用    正常更新    持续干预        基于调整后的速度
```

### 核心特点：

- ✅ 重力系统**完全正常工作**
- ✅ 捕获脚本**辅助调整**，不干扰重力
- ✅ 两者**和谐共存**，形成动态平衡
- ⚠️ 需要注意执行顺序和可能的冲突


