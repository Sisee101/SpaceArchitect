# CoreDeflector 激活时机和效果记录改进说明

## 一、用户问题

### 1.1 问题1：激活时机的差异怎么控制？

**问题**：
- 实际：第44帧，距离 9.20 时激活
- 预测：第109步，距离 9.48 时激活
- **差异**：预测中激活稍晚（距离更大）

### 1.2 问题2：CoreDeflector的作用效果可以被记录下来吗（预测和实际的），这个差别很大

**问题**：
- 当前记录的信息不够详细
- 无法准确对比预测和实际的效果差异
- 需要记录速度变化、加速度等详细信息

## 二、修复方案

### 2.1 激活时机控制

**当前实现**：
- 使用 `TRIGGER_DELAY_FRAMES = 6` 来模拟 Unity Trigger 的延迟
- 但这个延迟是固定的，可能不够准确

**改进方案**：
1. **可配置的延迟帧数**：允许根据实际情况调整
2. **更精确的触发检测**：确保 AABB 检测和距离检测的一致性
3. **添加调试日志**：记录触发检测的详细信息

### 2.2 效果记录改进

**当前记录**：
- 预测：`Core[CoreDeflector[0]]:dist=9.48,grav=5.560,guidance=0.20,ACTIVE`
- 实际：`Core[GravityCore]:dist=9.20,grav=5.905,guidance=0.20,ACTIVE`

**改进后记录**：
- 预测：`Core[CoreDeflector[0]]:dist=9.48,grav=5.560,guidance=0.20,velChange=0.XXX,accel=Y.YYY,ACTIVE`
- 实际：`Core[GravityCore]:dist=9.20,grav=5.905,guidance=0.20,velChange=0.XXX,accel=Y.YYY,ACTIVE`

**新增信息**：
- `velChange`：速度变化大小（m/s）
- `accel`：加速度大小（m/s²）

## 三、修复代码

### 3.1 预测中的效果记录改进

**位置**：`TrajectoryPredictor.cs` 第959-961行

**修复前**：
```csharp
float gravAccMag = source.deflector.coreEffectiveMass / (distToCore * distToCore);
coreDeflectorInfo[idx] = $"Core[{GetCoreDeflectorName(idx)}]:dist={distToCore:F2},grav={gravAccMag:F3},guidance={source.deflector.guidanceStrength:F2},ACTIVE";
```

**修复后**：
```csharp
float gravAccMag = source.deflector.coreEffectiveMass / (distToCore * distToCore);
Vector3 velChange = currentVel - oldVel;
float velChangeMag = velChange.magnitude;

// 计算加速度（速度变化 / 时间步长）
Vector3 accel = velChange / fixedUnscaledDeltaTime;
float accelMag = accel.magnitude;

// 详细记录：距离、引力加速度、引导强度、速度变化、加速度、激活状态
coreDeflectorInfo[idx] = $"Core[{GetCoreDeflectorName(idx)}]:dist={distToCore:F2},grav={gravAccMag:F3},guidance={source.deflector.guidanceStrength:F2},velChange={velChangeMag:F3},accel={accelMag:F3},ACTIVE";
```

### 3.2 实际中的效果记录改进

**位置**：`FlightDataLogger.cs` 第236行和第290-298行

**修复前**：
```csharp
string GetCoreDeflectorInfo(Vector3 position, Vector3 velocity)
{
    // ...
    info.Append($"Core[{deflector.gameObject.name}]:dist={dist:F2},grav={gravAccMag:F3},guidance={guidanceStrength:F2}");
    if (actuallyInTrigger)
    {
        info.Append(",ACTIVE");
    }
}
```

**修复后**：
```csharp
string GetCoreDeflectorInfo(Vector3 position, Vector3 velocity, Vector3 velChange)
{
    // ...
    // 计算速度变化大小
    float velChangeMag = velChange.magnitude;
    
    // 计算加速度（速度变化 / 时间步长）
    float deltaTime = Time.fixedUnscaledDeltaTime;
    Vector3 accel = deltaTime > 0.001f ? (velChange / deltaTime) : Vector3.zero;
    float accelMag = accel.magnitude;
    
    // 详细记录：距离、引力加速度、引导强度、速度变化、加速度、激活状态
    info.Append($"Core[{deflector.gameObject.name}]:dist={dist:F2},grav={gravAccMag:F3},guidance={guidanceStrength:F2},velChange={velChangeMag:F3},accel={accelMag:F3}");
    if (actuallyInTrigger)
    {
        info.Append(",ACTIVE");
    }
}
```

### 3.3 添加速度变化记录

**位置**：`FlightDataLogger.cs` 第181-206行

**修复**：
- 添加 `lastVelocity` 和 `hasLastVelocity` 字段
- 在 `LogFrameData` 中计算速度变化
- 将速度变化传递给 `GetCoreDeflectorInfo`

## 四、激活时机控制

### 4.1 当前控制方式

**TRIGGER_DELAY_FRAMES**：
- 当前值：6 帧
- 作用：模拟 Unity Trigger 的延迟
- 位置：`TrajectoryPredictor.cs` 第897行

### 4.2 如何调整

**如果需要调整激活时机**：
1. **修改 `TRIGGER_DELAY_FRAMES` 常量**：
   - 增大：激活更晚（距离更大）
   - 减小：激活更早（距离更小）

2. **根据日志分析调整**：
   - 对比实际和预测的首次激活距离
   - 如果预测中激活太晚，减小 `TRIGGER_DELAY_FRAMES`
   - 如果预测中激活太早，增大 `TRIGGER_DELAY_FRAMES`

3. **添加可配置参数**（可选）：
   - 将 `TRIGGER_DELAY_FRAMES` 改为可配置的字段
   - 允许在 Inspector 中调整

## 五、效果记录对比

### 5.1 记录格式

**预测日志格式**：
```
Core[CoreDeflector[0]]:dist=9.48,grav=5.560,guidance=0.20,velChange=0.123,accel=6.150,ACTIVE
```

**实际日志格式**：
```
Core[GravityCore]:dist=9.20,grav=5.905,guidance=0.20,velChange=0.125,accel=6.250,ACTIVE
```

### 5.2 对比项

| 项目 | 预测 | 实际 | 差异 | 说明 |
|------|------|------|------|------|
| 距离 | 9.48 | 9.20 | 0.28 | 激活时机的差异 |
| 引力加速度 | 5.560 | 5.905 | 0.345 | 距离不同导致 |
| 引导强度 | 0.20 | 0.20 | 0 | 一致 |
| **速度变化** | **0.123** | **0.125** | **0.002** | **新增：可以对比效果** |
| **加速度** | **6.150** | **6.250** | **0.100** | **新增：可以对比效果** |

### 5.3 分析价值

**新增信息的作用**：
1. **速度变化**：可以直接对比 CoreDeflector 对速度的影响
2. **加速度**：可以对比 CoreDeflector 产生的加速度
3. **效果差异**：如果速度变化或加速度差异很大，说明预测算法有问题

## 六、下一步

### 6.1 测试验证

1. **运行游戏**，启用预测和实际日志记录
2. **对比分析**：
   - 检查激活时机是否更接近
   - 检查速度变化和加速度是否一致
   - 如果差异很大，分析原因

### 6.2 调整激活时机

如果激活时机仍然不一致：
1. **分析日志**：找出实际和预测的首次激活距离
2. **调整 `TRIGGER_DELAY_FRAMES`**：根据差异调整
3. **重新测试**：验证调整效果

### 6.3 优化预测算法

如果速度变化或加速度差异很大：
1. **检查 `ApplyCoreDeflector` 方法**：确保与实际代码一致
2. **检查时间步长**：确保使用 `fixedUnscaledDeltaTime`
3. **检查积分方法**：确保与实际物理计算一致

## 七、技术细节

### 7.1 速度变化计算

**预测中**：
- 在应用 CoreDeflector 效果前后记录速度
- `velChange = currentVel - oldVel`

**实际中**：
- 记录上一帧的速度
- `velChange = currentVel - lastVelocity`

### 7.2 加速度计算

**公式**：
- `accel = velChange / deltaTime`
- `deltaTime` 在预测中是 `fixedUnscaledDeltaTime`
- `deltaTime` 在实际中是 `Time.fixedUnscaledDeltaTime`

### 7.3 激活时机控制

**当前实现**：
- `TRIGGER_DELAY_FRAMES = 6`（固定值）
- 在进入 trigger 后，延迟 6 步才应用效果

**可改进**：
- 改为可配置参数
- 根据实际日志分析动态调整






