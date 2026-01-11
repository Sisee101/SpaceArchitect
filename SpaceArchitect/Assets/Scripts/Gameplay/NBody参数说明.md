# NBody 参数说明

## 📋 概述

`NBody` 是重力引擎的核心组件，用于定义参与 N 体物理模拟的物体的属性。每个需要受重力影响或产生重力的物体都需要挂载此组件。

---

## 🎯 参数详解

### 1. **Mass（质量）**

```csharp
public float mass;
```

**作用：**
- 物体的质量，用于计算重力相互作用
- 质量越大，产生的引力越强，受其他物体影响也越大

**单位：**
- 会根据 `GravityEngine` 的 `massScale` 进行缩放

**示例：**
- `mass = 500`：物体质量为 500（经过缩放后的实际质量）

**注意事项：**
- 质量必须大于 0
- 质量为 0 的物体被视为无质量粒子（不受重力影响，但可以产生重力）

---

### 2. **Vel（初始速度）**

```csharp
public Vector3 vel;
```

**作用：**
- 物体在编辑器或初始化时的初始速度
- 使用 `GravityEngine` 选择的单位系统（米或天文单位）

**单位：**
- 根据 `GravityEngine` 的单位系统进行缩放
- 如果单位是 `DIMENSIONLESS`，则直接使用世界单位

**示例：**
- `vel = (0, 0, 0)`：物体初始静止
- `vel = (10, 0, 0)`：物体初始以 10 单位/秒的速度沿 X 轴移动

**注意事项：**
- 这个值在编辑器中被设置，但实际物理计算中使用的是 `vel_phys`
- 如果物体有轨道组件（如 `OrbitEllipse`），轨道组件会覆盖此值

---

### 3. **Vel_phys（物理速度）**

```csharp
public Vector3 vel_phys;
```

**作用：**
- 物体在重力引擎内部使用的物理速度（经过缩放的内部单位）
- 这是实际参与物理计算的速度值

**单位：**
- 使用重力引擎的内部缩放单位
- 由 `vel` 经过 `velocityScale` 缩放得到

**示例：**
- `vel_phys = (0, 0, 0)`：物体在物理空间中静止

**注意事项：**
- ⚠️ **不要手动修改此值**（代码注释说这是一个糟糕的设计，需要重构）
- 此值由 `GravityEngine` 自动更新
- 如果需要更新速度，使用 `UpdateVelocity()` 方法

---

### 4. **Initial Pos（初始位置）**

```csharp
public Vector3 initialPos;
```

**作用：**
- 物体在编辑器中的初始位置（使用 `GravityEngine` 的活跃单位）
- 如果单位是 `DIMENSIONLESS`，此字段不活跃，直接使用 `transform.position`

**单位：**
- 如果单位是 `m` 或 `AU`，会受到 `lengthScale` 影响
- 如果单位是 `DIMENSIONLESS`，不受缩放影响

**示例：**
- `initialPos = (0, 0, 0)`：物体初始位置在原点

**注意事项：**
- 在编辑器中修改 `transform.position` 时，此值会自动更新
- 如果物体有轨道组件，轨道组件会覆盖此值

---

### 5. **Initial Phys Position（初始物理位置）**

```csharp
public Vector3 initialPhysPosition;
```

**作用：**
- 物体在重力引擎内部使用的物理位置（经过缩放后的位置）
- 这是实际参与物理计算的位置值

**单位：**
- 使用重力引擎的内部缩放单位
- 由 `initialPos` 经过 `lengthScale` 缩放得到

**示例：**
- `initialPhysPosition = (0, 0, 0)`：物体在物理空间中的初始位置

**注意事项：**
- ⚠️ **不要手动修改此值**（代码注释说这是公开的但不应该在检查器中设置）
- 此值由 `GravityEngine` 在初始化时自动计算
- 如果 `lockToXYPlane` 为 true，Z 值会被强制设为 0

---

### 6. **Init With Double（使用双精度初始化）**

```csharp
public bool initWithDouble = false;
```

**作用：**
- 如果启用，使用双精度浮点数（`Vector3d`）来存储初始位置和速度
- 提供更高的精度，适用于大尺度或长时间模拟

**相关字段：**
- `initialPhysPositionV3`：双精度初始位置
- `vel_physV3`：双精度物理速度

**使用场景：**
- 大尺度模拟（如太阳系）
- 需要高精度计算的场景

**注意事项：**
- 如果启用，必须同时设置 `initialPhysPositionV3` 和 `vel_physV3`
- 为了向后兼容，`initialPhysPosition` 仍会被设置

---

### 7. **Automatic Particle Capture（自动粒子捕获）**

```csharp
public bool automaticParticleCapture = true;
```

**作用：**
- 如果启用，自动从子物体的 `MeshFilter` 检测粒子捕获半径
- 如果禁用，使用手动设置的 `size` 值

**工作原理：**
- 在 `Awake()` 时，遍历所有子物体
- 查找第一个有 `MeshFilter` 的子物体
- 使用该子物体的 `localScale.x / 2` 作为捕获半径

**示例：**
- 如果子物体是一个半径为 1 的球体，`size` 会被自动设置为 0.5

**注意事项：**
- 如果找不到 `MeshFilter`，默认使用 `size = 1`
- 复合物体可能没有顶层网格，需要手动设置

---

### 8. **Size（粒子捕获半径）**

```csharp
public double size = 0.1;
```

**作用：**
- 粒子捕获半径，用于碰撞检测
- 当其他物体距离小于此半径时，会被标记为"捕获"或"碰撞"

**单位：**
- 使用物理单位（经过缩放）

**示例：**
- `size = 0.1`：捕获半径为 0.1 物理单位
- 如果物体是一个半径为 1 的球体，`size` 应该设置为 0.5（半径）

**注意事项：**
- 如果 `automaticParticleCapture = true`，此值会被自动覆盖
- 此值用于判断物体是否"碰撞"或"捕获"其他物体

---

### 9. **Rotate Frame（旋转框架）**

```csharp
public bool rotateFrame;
```

**作用：**
- 如果启用，物体的本地坐标系会随着运动方向旋转
- 物体的 Z 轴会始终指向运动方向

**工作原理：**
- 在 `GEUpdate()` 中，计算当前速度与上一帧速度的旋转差
- 将旋转差应用到物体的 `transform.rotation`

**使用场景：**
- 需要物体始终朝向运动方向的场景
- 例如：飞船、导弹等

**注意事项：**
- 只影响物体的旋转，不影响位置
- 如果 `lockToXYPlane = true`，旋转也会被限制在 XY 平面

---

### 10. **Lock To XY Plane（锁定到 XY 平面）**

```csharp
public bool lockToXYPlane = false;
```

**作用：**
- 如果启用，物体的 Z 位置和速度 Z 分量会被强制设为 0
- 物体只能在 XY 平面上运动

**工作原理：**
- 在 `GEUpdate()` 中：`position.z = 0f`，`velocity.z = 0f`
- 在 `InitPosition()` 中：`initialPhysPosition.z = 0f`
- 在 `GravityEngine.UpdateGameObjects()` 中也会强制 Z = 0

**使用场景：**
- 2D 游戏或 2.5D 游戏
- 需要限制物体在平面内运动的场景

**注意事项：**
- 启用后，物体无法在 Z 轴上移动
- 所有与 Z 相关的物理计算都会被忽略

---

## 🔧 内部参数（不可见）

### **Engine Ref（引擎引用）**

```csharp
public GravityEngine.EngineRef engineRef;
```

**作用：**
- 重力引擎维护的不透明数据，用于跟踪物体在引擎中的状态
- ⚠️ **不要手动修改**

**包含信息：**
- 物体在引擎中的索引
- 物体类型（固定、动态、无质量等）
- 其他内部状态

---

## 📊 参数关系图

```
编辑器设置
  ├─ mass (质量)
  ├─ vel (初始速度) ──[velocityScale]──> vel_phys (物理速度)
  ├─ initialPos (初始位置) ──[lengthScale]──> initialPhysPosition (物理位置)
  ├─ size (捕获半径)
  └─ 选项
      ├─ automaticParticleCapture (自动检测大小)
      ├─ rotateFrame (旋转框架)
      └─ lockToXYPlane (锁定到XY平面)
```

---

## 🎮 使用示例

### 示例1：创建一个行星

```csharp
// 在编辑器中设置：
mass = 1000;              // 质量 1000
vel = (0, 0, 0);          // 初始静止
initialPos = (10, 0, 0);   // 初始位置在 (10, 0, 0)
size = 1.0;                // 捕获半径 1.0
automaticParticleCapture = false;  // 手动设置大小
rotateFrame = false;       // 不旋转
lockToXYPlane = true;      // 限制在 XY 平面
```

### 示例2：创建一个运动的物体

```csharp
// 在编辑器中设置：
mass = 1;                  // 质量 1
vel = (5, 0, 0);           // 初始速度沿 X 轴 5 单位/秒
initialPos = (0, 0, 0);    // 初始位置在原点
size = 0.1;                // 捕获半径 0.1
automaticParticleCapture = true;   // 自动检测大小
rotateFrame = true;        // 朝向运动方向
lockToXYPlane = true;      // 限制在 XY 平面
```

---

## ⚠️ 注意事项

### 1. **不要手动修改内部参数**

以下参数由 `GravityEngine` 自动管理，不要手动修改：
- `vel_phys`
- `initialPhysPosition`
- `engineRef`

### 2. **单位系统**

- 如果 `GravityEngine` 使用 `DIMENSIONLESS` 单位，`initialPos` 不活跃，直接使用 `transform.position`
- 如果使用 `m` 或 `AU`，所有位置和速度都会经过缩放

### 3. **轨道组件优先级**

如果物体有轨道组件（如 `OrbitEllipse`），轨道组件会覆盖：
- `initialPos`
- `vel`
- `initialPhysPosition`

### 4. **自动检测大小**

如果 `automaticParticleCapture = true`：
- 在 `Awake()` 时自动检测
- 如果找不到 `MeshFilter`，使用默认值 `size = 1`

### 5. **锁定到 XY 平面**

如果 `lockToXYPlane = true`：
- 所有 Z 值都会被强制设为 0
- 物体无法在 Z 轴上移动
- 适用于 2D 游戏

---

## 📝 总结

### 核心参数：

1. **`mass`**：质量，决定引力大小
2. **`vel`**：初始速度（编辑器设置）
3. **`initialPos`**：初始位置（编辑器设置）
4. **`size`**：捕获半径，用于碰撞检测

### 选项参数：

1. **`automaticParticleCapture`**：自动检测大小
2. **`rotateFrame`**：旋转框架
3. **`lockToXYPlane`**：锁定到 XY 平面
4. **`initWithDouble`**：使用双精度（高级）

### 内部参数（只读）：

1. **`vel_phys`**：物理速度（由引擎管理）
2. **`initialPhysPosition`**：物理位置（由引擎管理）
3. **`engineRef`**：引擎引用（由引擎管理）








