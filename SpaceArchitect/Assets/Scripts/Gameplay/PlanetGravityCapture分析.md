# PlanetGravityCapture 脚本详细分析

## 📋 脚本概述

`PlanetGravityCapture` 是一个**辅助轨道引导脚本**，用于帮助飞船更容易进入行星轨道。它**不产生引力**，而是通过微调飞船速度来辅助轨道捕获。

---

## 🔄 整体工作流程

```
启动 (Start)
  ↓
每帧更新 (Update)
  ├─→ 检测附近飞船 (DetectNearbySpaceships)
  │     ├─ 查找所有带 "Spaceship" 标签的物体
  │     ├─ 计算距离（仅XY平面）
  │     ├─ 进入 captureRadius → 添加到捕获列表
  │     └─ 离开 releaseRadius → 从捕获列表移除
  │
  └─→ 引导已捕获飞船 (GuideCapturedSpaceships)
        ├─ 计算相对位置和距离
        ├─ 分解速度（径向 + 切向）
        ├─ 计算理想轨道速度
        ├─ 微调速度使其接近理想轨道
        └─ 应用新速度到 GravityEngine
```

---

## 📊 核心参数说明

### 1. 捕获范围参数
- **`captureRadius`** (默认 15f)
  - 飞船进入此距离时开始捕获引导
  - 使用 `Vector2.Distance` 计算（**仅考虑XY平面**）

- **`releaseRadius`** (默认 30f)
  - 飞船离开此距离时停止引导
  - 提供滞后机制，避免频繁进出

### 2. 引导强度参数
- **`orbitGuidanceStrength`** (默认 0.05f, 范围 0.001-0.5)
  - 控制速度调整的强度
  - **值越小越自然**，推荐 0.01-0.1
  - 使用 `Lerp` 插值，每次只调整一小部分

- **`targetOrbitRadius`** (默认 12f)
  - 目标轨道半径（仅用于可视化，实际计算使用当前距离）

### 3. 检测参数
- **`spaceshipTag`** (默认 "Spaceship")
  - 用于识别飞船的标签
  - 使用 `FindGameObjectsWithTag` 查找

- **`detectionInterval`** (默认 0.1f)
  - 检测间隔，降低性能消耗
  - 每 0.1 秒检测一次

---

## 🔍 详细逻辑分析

### 阶段一：检测飞船 (DetectNearbySpaceships)

```csharp
// 1. 查找所有飞船（通过标签）
GameObject[] spaceships = GameObject.FindGameObjectsWithTag(spaceshipTag);

// 2. 计算距离（仅XY平面，忽略Z）
float distance = Vector2.Distance(
    new Vector2(transform.position.x, transform.position.y),
    new Vector2(ship.transform.position.x, ship.transform.position.y)
);
```

**关键点：**
- ✅ 使用 `Vector2.Distance`，**只考虑XY平面**（适合2D游戏）
- ✅ 检查飞船是否有 `NBody` 组件
- ✅ 使用列表管理多个飞船的捕获状态

**状态转换：**
```
未捕获 → 距离 ≤ captureRadius → 已捕获
已捕获 → 距离 > releaseRadius → 未捕获
```

---

### 阶段二：轨道引导 (GuideCapturedSpaceships)

这是脚本的核心逻辑，分为以下步骤：

#### 步骤1：计算相对位置和距离

```csharp
Vector3 relativePos = info.spaceship.position - transform.position;
float currentRadius = Mathf.Max(relativePos.magnitude, 0.1f);
```

- 计算飞船相对于行星的位置
- 使用 `Mathf.Max(..., 0.1f)` 防止除零错误

#### 步骤2：获取当前速度

```csharp
Vector3 currentVelocity = ge.GetVelocity(info.nbody);
```

- 从 GravityEngine 获取飞船的当前物理速度

#### 步骤3：计算理想轨道速度

```csharp
// 径向方向（指向行星中心）
Vector3 radialDir = relativePos.normalized;

// 切向方向（垂直于径向，用于轨道运动）
Vector3 tangent = GetOrbitTangent(radialDir);

// 圆形轨道速度公式: v = √(GM/r)
float idealOrbitSpeed = Mathf.Sqrt(planetMass / Mathf.Max(currentRadius, 0.1f));
Vector3 idealTangentialVel = tangent * idealOrbitSpeed;
```

**物理公式：**
- 圆形轨道速度：`v = √(GM/r)`
  - G = 重力常数（在 GravityEngine 中通常为 1）
  - M = 行星质量
  - r = 轨道半径

#### 步骤4：速度分解

```csharp
// 将速度分解为径向和切向分量
float radialSpeed = Vector3.Dot(currentVelocity, radialDir);
Vector3 radialVel = radialDir * radialSpeed;        // 朝向/远离行星
Vector3 tangentialVel = currentVelocity - radialVel; // 切向运动
```

**速度分解示意图：**
```
当前速度 = 径向速度 + 切向速度
         ↓           ↓
    朝向行星    轨道运动方向
```

#### 步骤5：微调速度

```csharp
// 调整切向速度，使其接近理想轨道速度
Vector3 adjustedTangentialVel = Vector3.Lerp(
    tangentialVel.normalized * Mathf.Max(tangentialVel.magnitude, 0.1f),
    idealTangentialVel,
    orbitGuidanceStrength  // 每次只调整 5%
);

// 衰减径向速度（减少朝向/远离行星的运动）
Vector3 adjustedRadialVel = Vector3.Lerp(
    radialVel, 
    Vector3.zero, 
    orbitGuidanceStrength * 0.5f  // 径向衰减更快（2.5%）
);
```

**调整策略：**
- ✅ **切向速度**：逐渐调整到理想轨道速度
- ✅ **径向速度**：逐渐衰减到零（减少径向运动）
- ✅ 使用 `Lerp` 插值，**每次只调整一小部分**，避免突变

#### 步骤6：应用新速度

```csharp
Vector3 newVelocity = adjustedRadialVel + adjustedTangentialVel;

if (IsVelocityValid(newVelocity))
{
    ge.SetVelocity(info.nbody, newVelocity);
}
```

---

## 🎯 核心算法：GetOrbitTangent

```csharp
Vector3 GetOrbitTangent(Vector3 radialDir)
{
    // 方法1：使用叉积计算切向（3D）
    Vector3 tangent = Vector3.Cross(Vector3.forward, radialDir).normalized;
    
    // 方法2：如果无效，使用2D平面计算
    if (无效) {
        tangent = new Vector3(-radialDir.y, radialDir.x, 0f).normalized;
    }
    
    // 方法3：如果还是无效，使用默认方向
    if (仍然无效) {
        tangent = Vector3.up;
    }
    
    return tangent;
}
```

**切向向量计算：**
- 在XY平面上，切向向量垂直于径向向量
- 公式：`tangent = (-radialDir.y, radialDir.x, 0)`
- 这确保了轨道运动在XY平面内

---

## ⚠️ 潜在问题和限制

### 1. **仅考虑XY平面**
```csharp
float distance = Vector2.Distance(...);  // 只使用XY坐标
```
- ✅ 适合2D游戏
- ⚠️ 如果飞船有Z方向运动，距离计算可能不准确

### 2. **圆形轨道假设**
```csharp
float idealOrbitSpeed = Mathf.Sqrt(planetMass / currentRadius);
```
- 假设目标是**圆形轨道**
- 对于椭圆轨道，这个计算可能不够精确

### 3. **性能考虑**
```csharp
GameObject[] spaceships = GameObject.FindGameObjectsWithTag(spaceshipTag);
```
- 每 0.1 秒查找一次所有飞船
- 如果飞船数量很多，可能影响性能
- 建议：使用对象池或缓存机制

### 4. **与 lockToXYPlane 的兼容性**
- ✅ 脚本使用 `Vector2.Distance`，只考虑XY平面
- ✅ 与 `lockToXYPlane` 功能兼容
- ⚠️ 但速度调整可能会被 `lockToXYPlane` 的Z=0约束覆盖

---

## 🔧 优化建议

### 1. 使用对象池或缓存
```csharp
// 缓存飞船列表，避免频繁查找
private List<GameObject> cachedSpaceships = new List<GameObject>();
```

### 2. 考虑椭圆轨道
```csharp
// 根据当前轨道参数计算理想速度，而不是假设圆形
float idealOrbitSpeed = CalculateIdealOrbitSpeed(currentRadius, eccentricity);
```

### 3. 添加距离检查优化
```csharp
// 只处理距离合理的飞船
if (distance > releaseRadius * 1.5f) continue;  // 提前跳过
```

### 4. 使用物理更新而不是每帧更新
```csharp
void FixedUpdate()  // 在物理更新中处理
{
    // 与 GravityEngine 的更新同步
}
```

---

## 📈 效果预期

### 正常情况：
1. 飞船进入捕获范围 → 开始微调速度
2. 切向速度逐渐接近理想值 → 形成轨道运动
3. 径向速度逐渐衰减 → 减少朝向/远离行星的运动
4. 最终形成稳定的圆形轨道

### 参数调整建议：
- **`orbitGuidanceStrength = 0.05`**：温和引导，适合大多数情况
- **`orbitGuidanceStrength = 0.1`**：较强引导，飞船更快进入轨道
- **`orbitGuidanceStrength = 0.01`**：非常温和，几乎不影响自然运动

---

## 🎮 使用场景

### 适用场景：
- ✅ 帮助玩家更容易进入轨道
- ✅ 自动调整飞船速度，减少手动操作
- ✅ 提供更平滑的轨道捕获体验

### 不适用场景：
- ❌ 需要精确物理模拟的场景
- ❌ 需要完全自然的重力系统
- ❌ 多体问题（多个行星同时影响）

---

## 📝 总结

`PlanetGravityCapture` 是一个**辅助工具脚本**，通过以下方式帮助飞船进入轨道：

1. **检测机制**：使用距离检测，管理飞船的捕获状态
2. **速度分解**：将速度分为径向和切向分量
3. **微调策略**：使用 Lerp 插值，逐渐调整速度
4. **轨道引导**：计算理想轨道速度，引导飞船进入圆形轨道

**关键特点：**
- ✅ 不产生引力（引力由 GravityEngine 计算）
- ✅ 只调整速度，辅助轨道捕获
- ✅ 使用温和的插值，避免突变
- ✅ 适合2D平面游戏（只考虑XY平面）


