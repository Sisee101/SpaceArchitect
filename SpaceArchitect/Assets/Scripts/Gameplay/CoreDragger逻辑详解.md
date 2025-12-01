# CoreDragger.cs 脚本逻辑详解

## 📋 脚本概述

`CoreDragger` 是一个**可拖拽的引力源脚本**，用于 Gravity Core（引力枢纽）物体。它允许物体：
- ✅ 在 XY 平面自由拖拽
- ✅ 产生引力（影响其他物体）
- ✅ 位置由拖拽控制，不受其他物体引力影响

---

## 🎯 核心设计理念

### 双重功能模式

脚本支持两种模式，通过 `produceGravity` 参数控制：

1. **引力模式** (`produceGravity = true`)
   - 物体在 GravityEngine 中，产生引力
   - 使用 `FixedObject` 组件固定位置
   - 位置由拖拽控制，但质量影响其他物体

2. **纯拖拽模式** (`produceGravity = false`)
   - 物体不在 GravityEngine 中
   - 不产生引力
   - 只能拖拽，不影响其他物体

---

## 🔄 完整执行流程

### 阶段1：初始化 (Start)

```
Start()
  ↓
获取组件引用
  ├─ coreRb (Rigidbody)
  ├─ nBody (NBody)
  ├─ gravityEngine (GravityEngine实例)
  └─ fixedObject (FixedObject，可能为空)
  ↓
根据 produceGravity 决定
  ├─ true → EnsureInGravityEngine()
  │   └─ 添加到 GravityEngine，产生引力
  └─ false → RemoveFromGravityEngine()
      └─ 确保不在 GravityEngine 中
  ↓
设置 Rigidbody 为运动学
  └─ isKinematic = true（位置由脚本控制）
```

**关键点：**
- 初始化时根据 `produceGravity` 决定是否添加到 GravityEngine
- Rigidbody 始终是运动学的，确保位置由拖拽控制

---

### 阶段2：持续监控 (FixedUpdate)

```
FixedUpdate() (每物理帧)
  ↓
如果 produceGravity = true
  ├─ EnsureInGravityEngine()
  │   └─ 确保物体在 GravityEngine 中
  └─ UpdateGravityEnginePosition()
      └─ 同步位置到 GravityEngine
  ↓
如果 produceGravity = false
  └─ RemoveFromGravityEngine()
      └─ 确保物体不在 GravityEngine 中
  ↓
确保 Rigidbody 是运动学的
  └─ 防止被其他系统改变
```

**关键点：**
- 每帧确保物体在正确的状态（在或不在 GravityEngine）
- 如果启用引力，每帧同步位置

---

### 阶段3：拖拽交互

#### 3.1 鼠标按下 (OnMouseDown)

```
OnMouseDown() (Unity事件系统)
  ↓
检查 Tag 是否为 "Core"
  └─ 如果不是，直接返回
  ↓
检查是否有 Collider
  └─ OnMouseDown 需要 Collider 才能触发
  ↓
根据 produceGravity 决定
  ├─ true → EnsureInGravityEngine()
  └─ false → RemoveFromGravityEngine()
  ↓
开始拖拽
  ├─ isDragging = true
  ├─ 计算鼠标深度 (mouseZCoord)
  └─ 计算偏移量 (offset)
  ↓
重置 Rigidbody 速度
  └─ 确保没有残留速度
```

**关键点：**
- 使用 Unity 的 `OnMouseDown` 事件（需要 Collider）
- 只处理 Tag 为 "Core" 的物体
- 计算偏移量，用于平滑拖拽

#### 3.2 拖拽中 (OnMouseDrag)

```
OnMouseDrag() (Unity事件系统)
  ↓
检查拖拽状态和 Tag
  └─ 必须是 isDragging = true 且 Tag = "Core"
  ↓
计算新位置
  ├─ 获取鼠标世界坐标
  ├─ 加上偏移量
  └─ 保持 Z 轴不变（只在 XY 平面移动）
  ↓
更新 Transform 位置
  └─ transform.position = newPosition
  ↓
如果启用引力，立即更新 GravityEngine
  └─ UpdateGravityEnginePosition()
      └─ 确保引力中心跟随拖拽
```

**关键点：**
- 实时更新位置
- 如果启用引力，立即同步到 GravityEngine
- 保持在 XY 平面（Z 轴不变）

#### 3.3 鼠标松开 (OnMouseUp)

```
OnMouseUp() (Unity事件系统)
  ↓
结束拖拽
  └─ isDragging = false
  ↓
保持 Rigidbody 为运动学
  └─ 确保位置稳定
  ↓
根据 produceGravity 决定
  ├─ true → UpdateGravityEnginePosition()
  │   └─ 更新最终位置
  └─ false → RemoveFromGravityEngine()
      └─ 确保不在 GravityEngine 中
```

**关键点：**
- 拖拽结束后，更新最终位置
- 确保物体保持在正确的状态

---

## 🔧 核心方法详解

### 1. EnsureInGravityEngine()

```csharp
private void EnsureInGravityEngine()
{
    // 检查必要组件
    if (nBody == null || gravityEngine == null)
        return;

    // 如果还没有添加到引擎
    if (nBody.engineRef == null)
    {
        // 自动添加 FixedObject 组件
        if (fixedObject == null)
        {
            fixedObject = gameObject.AddComponent<FixedObject>();
        }

        // 添加到 GravityEngine
        gravityEngine.AddBody(gameObject);
    }
}
```

**作用：**
- 确保物体在 GravityEngine 中
- 自动添加 `FixedObject` 组件（如果不存在）
- `FixedObject` 使物体位置固定，但质量影响其他物体

**FixedObject 的作用：**
- 位置由脚本控制（不随引力移动）
- 质量参与引力计算（影响其他物体）

---

### 2. UpdateGravityEnginePosition()

```csharp
private void UpdateGravityEnginePosition()
{
    // 世界坐标 → 物理坐标
    Vector3 worldPos = transform.position;
    Vector3 physPos = worldPos / gravityEngine.physToWorldFactor;
    
    // 更新 NBody 的初始位置
    nBody.initialPhysPosition = physPos;
    
    // 更新 FixedObject 的内部位置
    if (fixedObject != null)
    {
        fixedObject.SetPositionDouble(new Vector3d(physPos));
    }
    
    // 更新 GravityEngine 的内部状态
    gravityEngine.SetPositionDoubleV3(nBody, physPos3d);
}
```

**作用：**
- 将世界坐标转换为物理坐标
- 同步位置到三个地方：
  1. `NBody.initialPhysPosition`（NBody 的初始位置）
  2. `FixedObject` 的内部位置
  3. `GravityEngine` 的内部状态

**为什么需要同步三个地方？**
- `NBody.initialPhysPosition`：NBody 组件存储的位置
- `FixedObject`：在 `PreEvolve()` 时读取位置
- `GravityEngine`：物理引擎的内部状态

---

### 3. RemoveFromGravityEngine()

```csharp
private void RemoveFromGravityEngine()
{
    if (nBody != null && gravityEngine != null)
    {
        if (nBody.engineRef != null)
        {
            gravityEngine.RemoveBody(gameObject);
            nBody.engineRef = null;
        }
    }
}
```

**作用：**
- 从 GravityEngine 中移除物体
- 清除 `engineRef` 引用
- 物体不再产生引力

---

### 4. GetMouseWorldPos()

```csharp
Vector3 GetMouseWorldPos()
{
    Vector3 mousePoint = Input.mousePosition;
    mousePoint.z = mouseZCoord;  // 使用保存的深度
    return Camera.main.ScreenToWorldPoint(mousePoint);
}
```

**作用：**
- 将屏幕坐标转换为世界坐标
- 使用 `mouseZCoord`（在 `OnMouseDown` 时保存）作为深度
- 确保拖拽时深度一致

---

## 📊 状态管理

### 关键状态变量

```csharp
private bool isDragging = false;        // 是否正在拖拽
private bool wasInEngine = false;       // 是否曾在引擎中（未使用）
```

### 状态转换

```
未拖拽 (isDragging = false)
  ↓ OnMouseDown
拖拽中 (isDragging = true)
  ↓ OnMouseDrag
持续拖拽 (isDragging = true)
  ↓ OnMouseUp
未拖拽 (isDragging = false)
```

---

## 🎯 关键设计决策

### 1. 为什么使用 FixedObject？

**原因：**
- `FixedObject` 实现 `IFixedOrbit` 接口
- 使物体位置固定（不受其他物体引力影响）
- 但质量仍然参与引力计算（影响其他物体）

**效果：**
- Gravity Core 可以被拖拽到任意位置
- 不会因为其他物体的引力而移动
- 但它的质量会影响飞船等物体

### 2. 为什么每帧同步位置？

**原因：**
- 拖拽时，`Transform.position` 改变
- 但 GravityEngine 使用内部物理坐标
- 需要实时同步，确保引力中心跟随拖拽

**时机：**
- `OnMouseDrag`：拖拽中立即更新
- `FixedUpdate`：每物理帧更新（防止遗漏）

### 3. 为什么 Rigidbody 必须是运动学的？

**原因：**
- 如果 `isKinematic = false`，Unity 物理引擎会控制位置
- 拖拽时会产生冲突
- 设置为运动学，位置完全由脚本控制

---

## ⚠️ 注意事项

### 1. Collider 要求

**必须条件：**
- 物体必须有 `Collider` 组件
- `OnMouseDown`、`OnMouseDrag`、`OnMouseUp` 需要 Collider 才能触发

**建议设置：**
- Collider 可以是 Trigger 或普通 Collider
- 确保 Collider 大小合适（能覆盖物体）

### 2. Tag 要求

**必须条件：**
- 物体的 Tag 必须是 `"Core"`
- 如果不是，所有鼠标事件都会被忽略

### 3. 坐标系统

**世界坐标 vs 物理坐标：**
- `Transform.position`：世界坐标（Unity 单位）
- `GravityEngine` 内部：物理坐标（可能经过缩放）
- 转换公式：`physPos = worldPos / physToWorldFactor`

### 4. Z 轴处理

**限制：**
- 拖拽时保持 Z 轴不变
- 只在 XY 平面移动
- 与 `lockToXYPlane` 功能兼容

---

## 🔄 与 GravityEngine 的交互

### 添加流程

```
EnsureInGravityEngine()
  ↓
检查 nBody.engineRef == null
  ↓
添加 FixedObject 组件
  ↓
调用 gravityEngine.AddBody(gameObject)
  ↓
GravityEngine 内部：
  ├─ 检测到 FixedObject
  ├─ 创建 FixedBody
  ├─ 添加到固定物体列表
  └─ 质量参与引力计算
```

### 位置更新流程

```
拖拽改变 Transform.position
  ↓
UpdateGravityEnginePosition()
  ↓
世界坐标 → 物理坐标
  ↓
更新三个位置：
  ├─ nBody.initialPhysPosition
  ├─ fixedObject.SetPositionDouble()
  └─ gravityEngine.SetPositionDoubleV3()
  ↓
下一帧 GravityEngine 使用新位置计算引力
```

---

## 🎮 使用场景

### 场景1：可拖拽的引力枢纽

```
设置：
- produceGravity = true
- NBody.mass = 50
- Tag = "Core"
- 有 Collider

效果：
- 可以拖拽
- 产生引力
- 影响飞船轨迹
```

### 场景2：纯装饰性物体

```
设置：
- produceGravity = false
- Tag = "Core"
- 有 Collider

效果：
- 可以拖拽
- 不产生引力
- 不影响其他物体
```

---

## 📝 总结

### 核心功能：
1. ✅ **拖拽控制**：使用 Unity 事件系统实现鼠标拖拽
2. ✅ **引力支持**：可选地添加到 GravityEngine 产生引力
3. ✅ **位置同步**：实时同步位置到 GravityEngine
4. ✅ **状态管理**：确保物体在正确的状态（在或不在引擎中）

### 关键特性：
- **双重模式**：通过 `produceGravity` 控制是否产生引力
- **自动管理**：自动添加 `FixedObject` 组件
- **实时同步**：拖拽时立即更新 GravityEngine 位置
- **安全检查**：多重检查确保只处理正确的物体

### 工作流程：
```
初始化 → 根据 produceGravity 决定状态
  ↓
每帧监控 → 确保状态正确
  ↓
鼠标拖拽 → 更新位置 → 同步到 GravityEngine
  ↓
产生引力 → 影响其他物体
```

