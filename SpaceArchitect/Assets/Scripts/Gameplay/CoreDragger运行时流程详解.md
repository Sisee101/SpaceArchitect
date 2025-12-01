# CoreDragger 运行时流程详解

## 🎮 点击 Play 后的完整执行流程

### Unity 生命周期执行顺序

```
点击 Play 按钮
  ↓
场景加载
  ↓
所有 GameObject 的 Awake() 执行
  ↓
所有 GameObject 的 Start() 执行
  ↓
GravityEngine.Start() 执行
  ├─ 调用 Setup()
  ├─ 自动检测所有 NBody（如果 detectNbodies = true）
  └─ 初始化物理系统
  ↓
CoreDragger.Start() 执行
  ↓
进入游戏循环
  ├─ FixedUpdate()（物理更新）
  ├─ Update()（逻辑更新）
  └─ OnMouseDown/Drag/Up()（鼠标事件）
```

---

## 📋 详细执行步骤

### 阶段1：场景初始化（Awake 阶段）

**时间点：** 所有脚本的 `Awake()` 执行

**CoreDragger 状态：**
- ❌ `CoreDragger` 的 `Awake()` **不存在**，不执行
- ✅ 其他脚本的 `Awake()` 可能执行（如 GravityEngine）

**GravityEngine 状态：**
- GravityEngine 的 `Awake()` 执行
- 初始化单例 `instance`
- 初始化内部列表

---

### 阶段2：脚本初始化（Start 阶段）

**时间点：** 所有脚本的 `Start()` 执行

#### 2.1 GravityEngine.Start() 先执行

```
GravityEngine.Start()
  ↓
检查是否已初始化
  ↓
调用 Setup()
  ├─ 如果 detectNbodies = true
  │   └─ SetupAutoDetect()
  │       └─ 查找所有 NBody 组件
  │           └─ 自动添加到引擎
  └─ 初始化物理系统
```

**关键点：**
- 如果 `detectNbodies = true`，GravityEngine 会**自动检测**所有 NBody
- 此时 Gravity Core 可能**已经被自动添加**到引擎

#### 2.2 CoreDragger.Start() 执行

```
CoreDragger.Start()
  ↓
获取组件引用
  ├─ coreRb = GetComponent<Rigidbody>()
  ├─ nBody = GetComponent<NBody>()
  ├─ gravityEngine = GravityEngine.instance
  └─ fixedObject = GetComponent<FixedObject>()（可能为 null）
  ↓
检查 produceGravity
  ├─ 如果 true
  │   └─ EnsureInGravityEngine()
  │       ├─ 检查 nBody.engineRef
  │       ├─ 如果为 null（还没被添加）
  │       │   ├─ 添加 FixedObject 组件
  │       │   └─ 调用 gravityEngine.AddBody()
  │       └─ 如果已存在（已被自动添加）
  │           └─ 确保有 FixedObject 组件
  └─ 如果 false
      └─ RemoveFromGravityEngine()
          └─ 确保不在引擎中
  ↓
设置 Rigidbody
  └─ isKinematic = true
```

**实际执行示例：**

假设 `produceGravity = true`，`detectNbodies = true`：

```
1. GravityEngine.Start()
   └─ 自动检测到 Gravity Core 的 NBody
   └─ 添加到引擎（nBody.engineRef != null）

2. CoreDragger.Start()
   └─ 检查 nBody.engineRef
   └─ 发现已存在（被自动添加）
   └─ 检查是否有 FixedObject
   └─ 如果没有，添加 FixedObject 组件
   └─ 确保物体在引擎中
```

**Console 输出：**
```
Gravity Core GravityCore 已添加到引力引擎，质量: 50
```

---

### 阶段3：游戏循环（运行时）

#### 3.1 FixedUpdate() - 每物理帧执行

**执行频率：** 默认 50 次/秒（Time.fixedDeltaTime = 0.02s）

```
FixedUpdate()（每物理帧）
  ↓
检查 produceGravity
  ├─ 如果 true
  │   ├─ EnsureInGravityEngine()
  │   │   └─ 确保物体在引擎中（防止被其他脚本移除）
  │   └─ UpdateGravityEnginePosition()
  │       ├─ 获取当前 Transform.position
  │       ├─ 转换为物理坐标
  │       ├─ 更新 nBody.initialPhysPosition
  │       ├─ 更新 fixedObject 位置
  │       └─ 更新 GravityEngine 内部状态
  └─ 如果 false
      └─ RemoveFromGravityEngine()
          └─ 确保不在引擎中
  ↓
确保 Rigidbody 是运动学的
  └─ 防止被其他系统改变
```

**作用：**
- 持续监控物体状态
- 同步位置到 GravityEngine
- 确保 Rigidbody 设置正确

---

#### 3.2 鼠标交互（用户操作时）

**前提条件：**
- 物体 Tag = "Core"
- 物体有 Collider 组件
- 鼠标点击在 Collider 上

##### OnMouseDown（鼠标按下）

```
用户点击 Gravity Core
  ↓
OnMouseDown() 触发
  ↓
检查 Tag = "Core"？
  └─ 如果不是，直接返回
  ↓
检查是否有 Collider？
  └─ 如果没有，直接返回
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

**关键数据：**
- `mouseZCoord`：鼠标到相机的深度（用于坐标转换）
- `offset`：鼠标点击位置与物体中心的偏移

##### OnMouseDrag（拖拽中）

```
用户拖拽鼠标
  ↓
OnMouseDrag() 每帧触发
  ↓
检查拖拽状态
  ├─ isDragging = true？
  └─ Tag = "Core"？
  ↓
计算新位置
  ├─ 获取鼠标世界坐标
  ├─ 加上偏移量
  └─ 保持 Z 轴不变
  ↓
更新 Transform.position
  └─ transform.position = newPosition
  ↓
如果启用引力，立即同步
  └─ UpdateGravityEnginePosition()
      └─ 确保引力中心跟随拖拽
```

**执行频率：** 每帧执行（如果正在拖拽）

**效果：**
- 物体跟随鼠标移动
- 引力中心实时更新
- 飞船等物体立即感受到引力变化

##### OnMouseUp（鼠标松开）

```
用户松开鼠标
  ↓
OnMouseUp() 触发
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
      └─ 确保不在引擎中
```

---

## 🔄 完整时间线示例

### 场景：游戏开始，用户拖拽 Gravity Core

```
时间轴：

T=0.0s（点击 Play）
  ├─ GravityEngine.Awake()
  ├─ GravityEngine.Start()
  │   └─ Setup() → 自动检测 NBody
  │       └─ Gravity Core 被自动添加到引擎
  └─ CoreDragger.Start()
      ├─ 发现 nBody.engineRef != null（已被添加）
      ├─ 添加 FixedObject 组件
      └─ 确保在引擎中

T=0.02s（第一帧 FixedUpdate）
  └─ CoreDragger.FixedUpdate()
      ├─ EnsureInGravityEngine()（已存在，跳过）
      └─ UpdateGravityEnginePosition()
          └─ 同步初始位置

T=0.5s（用户点击 Gravity Core）
  └─ CoreDragger.OnMouseDown()
      ├─ 检查 Tag = "Core" ✅
      ├─ 检查 Collider ✅
      ├─ EnsureInGravityEngine()（已存在）
      ├─ isDragging = true
      ├─ 计算 mouseZCoord
      └─ 计算 offset

T=0.52s（用户拖拽）
  └─ CoreDragger.OnMouseDrag()
      ├─ 计算新位置
      ├─ 更新 transform.position
      └─ UpdateGravityEnginePosition()
          └─ 同步位置到 GravityEngine

T=0.54s（继续拖拽）
  └─ CoreDragger.OnMouseDrag()
      └─ 继续更新位置...

T=0.56s（用户松开）
  └─ CoreDragger.OnMouseUp()
      ├─ isDragging = false
      └─ UpdateGravityEnginePosition()
          └─ 更新最终位置

T=0.58s（下一帧 FixedUpdate）
  └─ CoreDragger.FixedUpdate()
      └─ 持续同步位置...
```

---

## 🎯 关键执行点

### 1. Start() 阶段的决策

**情况A：GravityEngine 自动检测（detectNbodies = true）**

```
GravityEngine.Start()
  └─ 自动检测到 Gravity Core
  └─ 添加到引擎（nBody.engineRef != null）

CoreDragger.Start()
  └─ 发现已被添加
  └─ 添加 FixedObject 组件（如果不存在）
  └─ 确保状态正确
```

**情况B：GravityEngine 不自动检测（detectNbodies = false）**

```
GravityEngine.Start()
  └─ 不自动检测
  └─ Gravity Core 未被添加（nBody.engineRef == null）

CoreDragger.Start()
  └─ 发现未被添加
  └─ 添加 FixedObject 组件
  └─ 调用 gravityEngine.AddBody()
  └─ 添加到引擎
```

### 2. FixedUpdate() 的持续监控

**为什么需要每帧检查？**

1. **防止被移除**：其他脚本可能移除物体
2. **位置同步**：确保 GravityEngine 中的位置正确
3. **状态维护**：确保 Rigidbody 设置正确

### 3. 拖拽时的实时同步

**为什么需要立即更新？**

```
拖拽改变 Transform.position
  ↓
立即调用 UpdateGravityEnginePosition()
  ↓
同步到 GravityEngine
  ↓
下一帧物理计算使用新位置
  ↓
飞船等物体立即感受到引力变化
```

---

## 📊 状态变化图

### Gravity Core 的状态

```
游戏开始
  ↓
Start()
  ├─ produceGravity = true
  │   └─ 添加到 GravityEngine
  │       └─ 状态：在引擎中，产生引力
  └─ produceGravity = false
      └─ 不在 GravityEngine
          └─ 状态：不在引擎中，不产生引力
  ↓
FixedUpdate()（每帧）
  └─ 持续监控和同步
  ↓
用户拖拽
  ├─ OnMouseDown → 开始拖拽
  ├─ OnMouseDrag → 更新位置 + 同步
  └─ OnMouseUp → 结束拖拽 + 最终同步
```

---

## 🔍 调试信息

### 正常启动时的 Console 输出

```
Gravity Core GravityCore 已添加到引力引擎，质量: 50
```

### 如果 produceGravity = false

```
（无输出，物体不在引擎中）
```

### 拖拽时的输出

```
（通常无输出，除非添加了 Debug.Log）
```

---

## ⚠️ 重要注意事项

### 1. 执行顺序依赖

**潜在问题：**
- 如果 `CoreDragger.Start()` 在 `GravityEngine.Start()` 之前执行
- `GravityEngine.instance` 可能为 `null`

**解决：**
- Unity 的 `Start()` 执行顺序不确定
- 脚本使用 `GravityEngine.instance`（单例），在第一次访问时初始化

### 2. 自动检测的冲突

**如果 detectNbodies = true：**
- GravityEngine 会自动添加所有 NBody
- CoreDragger 需要检查是否已被添加
- 如果已被添加，只需确保有 FixedObject

**如果 detectNbodies = false：**
- GravityEngine 不会自动添加
- CoreDragger 需要手动添加

### 3. FixedObject 的作用

**为什么需要 FixedObject？**

```
没有 FixedObject：
  └─ 物体会被其他物体的引力影响
  └─ 拖拽时可能被拉走

有 FixedObject：
  └─ 物体位置固定（不受引力影响）
  └─ 但质量影响其他物体
  └─ 完美适合可拖拽的引力源
```

---

## 📝 总结

### 点击 Play 后的执行流程：

1. **初始化阶段（Start）**
   - 获取组件引用
   - 根据 `produceGravity` 决定是否添加到 GravityEngine
   - 自动添加 `FixedObject` 组件（如果启用引力）
   - 设置 Rigidbody 为运动学

2. **运行时阶段（FixedUpdate）**
   - 每物理帧监控状态
   - 持续同步位置到 GravityEngine
   - 确保 Rigidbody 设置正确

3. **交互阶段（鼠标事件）**
   - `OnMouseDown`：开始拖拽
   - `OnMouseDrag`：更新位置并同步
   - `OnMouseUp`：结束拖拽并最终同步

### 关键特性：

- ✅ **自动管理**：自动添加到 GravityEngine
- ✅ **实时同步**：拖拽时立即更新引力中心
- ✅ **状态维护**：持续确保物体在正确状态
- ✅ **双重模式**：支持引力模式和纯拖拽模式

