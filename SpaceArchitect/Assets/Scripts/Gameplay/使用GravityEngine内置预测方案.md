# 使用 GravityEngine 内置轨迹预测方案

## 一、为什么当前方法不准确

### 1.1 根本问题

**手动模拟的局限性**：
1. 无法完全复制 GravityEngine 的内部逻辑
2. 时间步长不一致（预测用0.01秒，实际用0.02秒）
3. 组件执行顺序和时机难以完全一致
4. 数值精度和累积误差

### 1.2 日志对比发现的问题

1. **时间步长不同**：预测用0.01秒，实际用0.02秒
2. **CoreDeflector激活时机不同**：距离8.73 vs 9.44
3. **速度变化幅度不同**：实际更剧烈
4. **位置差异随时间累积**：后期完全偏离

## 二、更好的方案：使用 GravityEngine 内置轨迹预测

### 2.1 GravityEngine 的内置功能

**GravityEngine 已经有完整的轨迹预测系统**：
- `trajectoryPrediction` - 启用/禁用标志
- `trajectoryState` - 克隆的物理状态（`new GravityState(worldState)`）
- `EvolveTrajectory()` - 使用**相同的积分器**向前演化
- `Trajectory` 组件 - 自动记录和显示预测点

**关键优势**：
1. ✅ **使用完全相同的物理引擎**
2. ✅ **使用完全相同的积分方法**（LeapFrog）
3. ✅ **使用完全相同的时间步长**
4. ✅ **自动处理所有组件**（如果组件实现了 `GEExternalAcceleration`）

### 2.2 实现方式

#### 方案A：直接使用 Trajectory 组件（最简单）

**步骤**：
1. 在飞船 GameObject 上添加 `Trajectory` 组件
2. 在 `GravityEngine` 上启用 `trajectoryPrediction`
3. `Trajectory` 组件会自动显示预测轨迹

**优点**：
- 最简单，几乎不需要代码
- 自动更新

**缺点**：
- 可能无法自定义显示方式
- 需要检查是否支持自定义组件（CoreDeflector）

#### 方案B：使用 trajectoryState 手动提取点（推荐）

**步骤**：
1. 启用 `GravityEngine.trajectoryPrediction`
2. 在需要时调用 `GravityEngine.TrajectoryRestart()` 重置预测
3. 访问 `trajectoryState` 获取预测状态（需要反射或公开API）
4. 从预测状态中提取位置点

**优点**：
- 完全控制预测过程
- 可以自定义显示
- 可以记录预测数据

**缺点**：
- 需要访问 `trajectoryState`（可能需要反射）
- 需要手动提取点

#### 方案C：使用 GetGravityStateCopy 手动演化（最灵活）

**步骤**：
1. 获取当前状态副本：`GravityEngine.GetGravityStateCopy()`
2. 使用相同的积分器向前演化（需要访问积分器）
3. 记录演化过程中的位置

**优点**：
- 完全控制
- 可以处理自定义组件

**缺点**：
- 最复杂
- 需要访问积分器（可能需要反射）

## 三、关键问题：CoreDeflector 是否会被预测

### 3.1 检查 CoreDeflector 的实现

**问题**：`CoreDeflector` 是否实现了 `GEExternalAcceleration`？

**如果实现了**：
- ✅ GravityEngine 的轨迹预测会自动处理
- ✅ 预测会完全准确

**如果没有实现**：
- ❌ 需要手动处理
- ❌ 或者让 CoreDeflector 实现 `GEExternalAcceleration`

### 3.2 解决方案

**方案1：让 CoreDeflector 实现 GEExternalAcceleration**
- 这样 GravityEngine 的轨迹预测会自动处理
- 预测会完全准确

**方案2：在预测后手动应用 CoreDeflector 效果**
- 使用 GravityEngine 的预测作为基础
- 然后手动应用 CoreDeflector 的效果

## 四、推荐实现方案

### 方案1：最简单 - 使用 Trajectory 组件

**如果 CoreDeflector 实现了 GEExternalAcceleration**：
1. 在飞船 GameObject 上添加 `Trajectory` 组件
2. 启用 `GravityEngine.trajectoryPrediction`
3. 完成！

**如果 CoreDeflector 没有实现**：
- 需要先让 CoreDeflector 实现 `GEExternalAcceleration`

### 方案2：改进当前方法（如果必须手动模拟）

**关键改进**：
1. **使用完全相同的时间步长**（0.02秒，而不是0.01秒）
2. **完全复制 LeapFrog 积分代码**（从 `LeapFrogIntegrator.cs`）
3. **更精确的 trigger 检测**（可能需要更复杂的逻辑）
4. **更精确的组件执行顺序**（确保与实际完全一致）

## 五、建议

**强烈推荐：先检查 CoreDeflector 是否可以实现 GEExternalAcceleration**

**原因**：
1. 如果可以实现，使用 GravityEngine 的内置预测会**完全准确**
2. 不需要维护复杂的预测代码
3. 自动处理所有边界情况

**如果无法实现**：
- 使用方案2改进当前方法
- 或者考虑重构 CoreDeflector 以支持 GEExternalAcceleration






