# ShipLauncher.cs 逻辑说明

## 📋 脚本概述

`ShipLauncher` 实现**弹弓式发射机制**：玩家拖拽鼠标产生反向力量来发射飞船。

---

## 🎯 核心功能

### 1. 弹弓式发射
- **拖拽方向**：从飞船位置向外拖拽
- **发射方向**：与拖拽方向相反（像弹弓一样）
- **力量计算**：拖拽距离越长，发射速度越快

### 2. 视觉反馈
- **箭头显示**：拖拽时显示箭头，指示发射方向
- **箭头长度**：根据拖拽距离动态调整
- **箭头宽度**：根据拖拽距离动态调整

---

## 🔄 完整执行流程

### 阶段1：初始化 (Awake)

```
Awake()
  ↓
获取组件引用
  ├─ shipState (ShipState 组件)
  ├─ mainCamera (主相机)
  └─ 初始化箭头 (LineRenderer)
```

**关键点：**
- 检查必要的组件是否存在
- 如果没有提供 LineRenderer，自动创建一个

---

### 阶段2：游戏循环 (Update)

```
Update() (每帧)
  ↓
检查飞船状态
  ├─ 如果不在 PreLaunch 状态 → 取消拖拽，返回
  └─ 如果在 PreLaunch 状态 → 处理输入
  ↓
HandleInput()
  ├─ 鼠标按下 → StartDrag()
  ├─ 鼠标拖拽中 → UpdateDrag()
  └─ 鼠标松开 → EndDrag()
```

**关键点：**
- 只有在 `PreLaunch` 状态下才能拖拽
- 如果状态改变，自动取消拖拽

---

### 阶段3：拖拽交互

#### 3.1 开始拖拽 (StartDrag)

```
StartDrag()
  ↓
计算鼠标到飞船的屏幕距离
  ├─ 如果距离 < 100 像素
  │   ├─ 检查是否点击在 Core 上（防止冲突）
  │   ├─ 如果点击在 Core 上且距离 > 50 像素 → 跳过
  │   └─ 否则 → 开始拖拽
  └─ 如果距离 >= 100 像素 → 不处理
  ↓
设置拖拽状态
  ├─ isDragging = true
  ├─ 保存拖拽起点 (dragStartPosition)
  └─ 显示箭头
```

**关键点：**
- 使用屏幕坐标距离检测（100 像素范围内）
- 防止与 Core 拖拽冲突（如果点击在 Core 上，跳过飞船拖拽）

#### 3.2 更新拖拽 (UpdateDrag)

```
UpdateDrag() (拖拽中每帧)
  ↓
计算拖拽向量
  ├─ dragVector = dragStartPosition - currentMousePosition
  └─ 限制最大拖拽距离 (maxDragDistance)
  ↓
更新箭头显示
  └─ UpdateArrowVisual(dragVector)
```

**关键点：**
- 拖拽向量 = 起点 - 当前点（反向）
- 限制最大拖拽距离，防止过度拖拽
- 实时更新箭头显示

#### 3.3 结束拖拽 (EndDrag)

```
EndDrag()
  ↓
计算拖拽向量（与 UpdateDrag 相同）
  ↓
计算发射速度
  ├─ 屏幕坐标 → 世界坐标
  ├─ 计算世界方向
  ├─ 计算拖拽比例 (0-1)
  └─ 应用力量系数
  ↓
发射飞船
  ├─ 调用 shipState.Launch(launchVelocity)
  └─ 切换到 Flying 状态
  ↓
清理
  ├─ isDragging = false
  └─ 隐藏箭头
```

**关键点：**
- 发射速度 = 世界方向 × 拖拽比例 × 力量系数
- 只保留 XY 平面的速度（Z = 0）

---

## 🔧 核心方法详解

### 1. CalculateLaunchVelocity()

```csharp
private Vector3 CalculateLaunchVelocity(Vector3 dragVector)
{
    // 1. 屏幕坐标 → 世界坐标
    Vector3 startWorldPos = ScreenToWorldPoint(起点);
    Vector3 endWorldPos = ScreenToWorldPoint(终点);
    
    // 2. 计算世界方向
    Vector3 worldDirection = endWorldPos - startWorldPos;
    worldDirection.z = 0f; // 只在 XY 平面
    
    // 3. 计算拖拽比例
    float dragRatio = dragDistance / maxDragDistance; // 0-1
    
    // 4. 应用力量系数
    Vector3 velocity = worldDirection.normalized * dragRatio * launchForceMultiplier;
    
    return velocity;
}
```

**工作原理：**
- **拖拽方向**：从飞船向外拖拽
- **发射方向**：与拖拽方向相同（`endWorldPos - startWorldPos`）
- **速度大小**：拖拽距离越长，速度越快

**示例：**
- 拖拽 100 像素（maxDragDistance = 200）→ dragRatio = 0.5
- launchForceMultiplier = 5
- 最终速度 = 方向 × 0.5 × 5 = 方向 × 2.5

### 2. UpdateArrowVisual()

```csharp
private void UpdateArrowVisual(Vector3 dragVector)
{
    // 1. 计算箭头终点（世界坐标）
    Vector3 arrowEndWorld = transform.position + arrowDirection;
    
    // 2. 设置箭头位置
    arrowLine.SetPosition(0, transform.position); // 起点：飞船位置
    arrowLine.SetPosition(1, arrowEndWorld);      // 终点：拖拽方向
    
    // 3. 根据拖拽距离调整箭头宽度
    float dragRatio = dragDistance / maxDragDistance;
    arrowLine.startWidth = arrowWidth * (0.5f + dragRatio);
    arrowLine.endWidth = arrowWidth * (1f + dragRatio * 2f);
}
```

**视觉效果：**
- 箭头从飞船指向拖拽方向
- 拖拽距离越长，箭头越粗
- 箭头长度 = 拖拽距离（世界单位）

---

## 📊 状态管理

### 拖拽状态

```
未拖拽 (isDragging = false)
  ↓ StartDrag()
拖拽中 (isDragging = true)
  ↓ UpdateDrag() (每帧)
持续拖拽 (isDragging = true)
  ↓ EndDrag() 或 CancelDrag()
未拖拽 (isDragging = false)
```

### 与 ShipState 的交互

```
PreLaunch 状态
  ↓
可以拖拽
  ↓
拖拽 → 发射
  ↓
Flying 状态
  ↓
不能拖拽（自动取消）
```

---

## 🎮 使用流程

### 玩家操作流程

```
1. 游戏开始
   └─ 飞船处于 PreLaunch 状态
   ↓
2. 鼠标移动到飞船附近（100 像素内）
   └─ 准备拖拽
   ↓
3. 鼠标按下
   └─ StartDrag() → 开始拖拽，显示箭头
   ↓
4. 拖拽鼠标（向外拖拽）
   └─ UpdateDrag() → 更新箭头显示
   ↓
5. 鼠标松开
   └─ EndDrag() → 计算速度，发射飞船
   ↓
6. 飞船发射
   └─ 切换到 Flying 状态，开始受重力影响
```

---

## ⚙️ 关键参数

### 发射设置

- **`launchForceMultiplier`** (默认 5f)
  - 拖拽力量的缩放系数
  - 值越大，发射速度越快

- **`maxDragDistance`** (默认 200f)
  - 最大拖拽距离（屏幕像素）
  - 超过此距离，速度不再增加

### 视觉反馈

- **`arrowColor`** (默认黄色)
  - 箭头颜色

- **`arrowWidth`** (默认 0.1f)
  - 箭头基础宽度
  - 拖拽时会动态调整

---

## 🔍 关键设计决策

### 1. 为什么使用屏幕坐标距离检测？

**原因：**
- 简单直接，不需要 Collider
- 不依赖 Unity 事件系统
- 性能更好

**缺点：**
- 需要手动计算距离
- 可能不够精确

### 2. 为什么拖拽方向与发射方向相同？

**原因：**
- 更直观：向外拖拽 = 向外发射
- 符合弹弓的物理直觉

**实现：**
```csharp
Vector3 worldDirection = endWorldPos - startWorldPos;
// endWorldPos 是拖拽终点，startWorldPos 是飞船位置
// 所以方向是从飞船指向拖拽终点
```

### 3. 为什么限制最大拖拽距离？

**原因：**
- 防止过度拖拽导致速度过大
- 提供更好的游戏平衡
- 避免数值溢出

---

## 🐛 潜在问题

### 问题1：拖拽检测不准确

**可能原因：**
- 屏幕距离检测阈值太小（100 像素）
- 飞船太小，难以点击

**解决：**
- 增大检测范围
- 使用射线检测替代距离检测

### 问题2：与 Core 拖拽冲突

**当前解决：**
- 检查是否点击在 Core 上
- 如果点击在 Core 上且距离飞船较远，跳过飞船拖拽

### 问题3：发射速度计算错误

**可能原因：**
- 屏幕到世界的坐标转换不准确
- 相机距离计算错误

**解决：**
- 使用 `ScreenToWorldPoint` 时提供正确的深度值
- 使用相机到飞船的实际距离

---

## 📝 总结

### 核心逻辑：

1. **检测拖拽**：使用屏幕坐标距离（100 像素）
2. **计算速度**：拖拽距离 → 拖拽比例 → 发射速度
3. **视觉反馈**：箭头显示发射方向和力量
4. **发射飞船**：调用 `ShipState.Launch()`

### 关键特性：

- ✅ **弹弓式发射**：拖拽产生反向力量
- ✅ **视觉反馈**：箭头显示发射方向
- ✅ **状态管理**：只在 PreLaunch 状态下工作
- ✅ **冲突处理**：防止与 Core 拖拽冲突

### 工作流程：

```
鼠标按下 → 检测距离 → 开始拖拽 → 显示箭头
  ↓
拖拽中 → 更新箭头 → 计算拖拽向量
  ↓
鼠标松开 → 计算速度 → 发射飞船 → 隐藏箭头
```

