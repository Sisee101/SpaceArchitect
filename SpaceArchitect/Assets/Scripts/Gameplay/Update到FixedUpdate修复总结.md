# Update 到 FixedUpdate 修复总结

## 问题根源

**核心问题：Update vs FixedUpdate 混用导致的随机性**

### 问题描述

1. **PlanetGravityCapture** 和 **GravityHubDeflector** 在 `Update()` 中执行
2. **GravityEngine** 和 **CoreDeflector** 在 `FixedUpdate()` 中执行
3. `Update()` 的频率受帧率影响（60fps、120fps 等），`FixedUpdate()` 固定为 50fps（0.02秒）
4. 帧率波动会导致 `Update()` 的执行次数不同，从而产生不一致的结果

### 影响

- 每次运行游戏，由于帧率波动，`Update()` 的执行次数可能不同
- 导致 `PlanetGravityCapture` 和 `GravityHubDeflector` 的应用频率不一致
- 最终导致飞行轨迹的随机性

## 修复内容

### 1. PlanetGravityCapture.cs

**修改前：**
```csharp
void Update()
{
    if (Time.time - lastDetectionTime < detectionInterval)
        return;
    lastDetectionTime = Time.time;
    // ...
}
```

**修改后：**
```csharp
void FixedUpdate()
{
    // 使用帧计数器，确保确定性
    int detectionIntervalFrames = Mathf.Max(1, Mathf.RoundToInt(detectionInterval / Time.fixedUnscaledDeltaTime));
    frameCounter++;
    if (frameCounter % detectionIntervalFrames != 0)
        return;
    // ...
}
```

**关键改进：**
- 从 `Update()` 改为 `FixedUpdate()`
- 使用帧计数器而不是 `Time.time`，确保确定性
- 使用 `Time.fixedUnscaledDeltaTime` 计算检测间隔

### 2. GravityHubDeflector.cs

**修改前：**
```csharp
void Update()
{
    if (Time.time - lastDetectionTime < detectionInterval)
        return;
    lastDetectionTime = Time.time;
    // ...
}
```

**修改后：**
```csharp
void FixedUpdate()
{
    // 使用帧计数器，确保确定性
    int detectionIntervalFrames = Mathf.Max(1, Mathf.RoundToInt(detectionInterval / Time.fixedUnscaledDeltaTime));
    frameCounter++;
    if (frameCounter % detectionIntervalFrames != 0)
        return;
    // ...
}
```

**关键改进：**
- 从 `Update()` 改为 `FixedUpdate()`
- 使用帧计数器而不是 `Time.time`，确保确定性
- 使用 `Time.fixedUnscaledDeltaTime` 计算检测间隔

### 3. TrajectoryPredictor.cs

**修改前：**
```csharp
// ========== 4. 引导/偏转阶段（模拟 Update 中的 Lerp）==========
// 注意：Update 的频率可能与 FixedUpdate 不同
if (i % updateInterval == 0)
{
    // PlanetGravityCapture 和 GravityHubDeflector 应用
}
```

**修改后：**
```csharp
// ========== 4. 引导/偏转阶段（FixedUpdate，与 PlanetGravityCapture 和 GravityHubDeflector 一致）==========
// 关键修复：PlanetGravityCapture 和 GravityHubDeflector 现在也在 FixedUpdate 中执行
if (i % fixedUpdateInterval == 0)
{
    int detectionIntervalSteps = Mathf.Max(1, Mathf.RoundToInt(detectionInterval / dt));
    if (i % detectionIntervalSteps == 0)
    {
        // 按固定顺序处理，确保确定性
        // PlanetGravityCapture 和 GravityHubDeflector 应用
    }
}
```

**关键改进：**
- 从 `updateInterval` 改为 `fixedUpdateInterval`
- 使用 `detectionInterval` 来模拟实际的检测间隔
- 按固定顺序处理多个效果源，确保确定性

## 执行顺序（修复后）

### FixedUpdate 阶段（所有物理相关修改都在这里）

```
1. GravityEngine.FixedUpdate()
   └─> 计算所有 NBody 之间的引力
   └─> 更新所有物体的位置和速度

2. CoreDeflector.FixedUpdate()
   └─> 应用 CoreDeflector 的引力加速度和引导加速度

3. ShipState.FixedUpdate()
   └─> 应用速度缩放

4. PlanetGravityCapture.FixedUpdate()  ← 修复：从 Update 改为 FixedUpdate
   └─> 应用轨道引导（Lerp 调整）

5. GravityHubDeflector.FixedUpdate()  ← 修复：从 Update 改为 FixedUpdate
   └─> 应用偏转（Lerp 调整）
```

## 预期效果

### 修复前
- 每次运行游戏，由于帧率波动，`Update()` 的执行次数不同
- 导致飞行轨迹的随机性

### 修复后
- 所有物理相关修改都在 `FixedUpdate()` 中执行
- 使用固定时间步长（0.02秒），不受帧率影响
- 使用帧计数器，确保确定性
- 飞行轨迹应该更加一致和可预测

## 验证方法

1. **多次运行测试**：
   - 使用相同的初始条件（位置、速度）
   - 记录多次运行的飞行轨迹
   - 比较轨迹的差异

2. **日志分析**：
   - 检查 `PlanetGravityCapture` 和 `GravityHubDeflector` 的应用频率
   - 确认它们在 `FixedUpdate` 中执行

3. **预测准确性**：
   - 比较预测轨迹和实际轨迹
   - 修复后，预测应该更加准确

## 注意事项

1. **性能影响**：
   - `FixedUpdate()` 固定为 50fps，比 `Update()` 可能更频繁或更不频繁
   - 如果 `detectionInterval` 很大，可能不会每帧都检测

2. **向后兼容**：
   - 如果其他脚本依赖 `PlanetGravityCapture` 或 `GravityHubDeflector` 在 `Update()` 中执行，可能需要调整

3. **测试建议**：
   - 在不同帧率下测试（30fps、60fps、120fps）
   - 确认飞行轨迹的一致性

## 相关文件

- `Assets/Scripts/Gameplay/PlanetGravityCapture.cs` - 已修复
- `Assets/Scripts/Gameplay/GravityHubDeflector.cs` - 已修复
- `Assets/Scripts/Gameplay/TrajectoryPredictor.cs` - 已更新以匹配新的执行时机
- `Assets/Scripts/Gameplay/轨迹预测随机性问题分析.md` - 问题分析文档
- `Assets/Scripts/Gameplay/飞行随机性问题分析.md` - 详细分析文档











