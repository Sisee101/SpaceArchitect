# 修复 ShockwaveController 在相机镜头内不正常的问题

## 🔍 问题分析

当 ShockwaveRing GameObject 移动到相机镜头内时，冲击波效果不正常。

### 根本原因

1. **Screen Position 节点受 GameObject 位置影响**：
   - Shader 使用 Screen Position 节点计算屏幕空间坐标
   - 当 GameObject 移动到相机镜头内时，Screen Position 的计算会受到 GameObject 位置的影响
   - 如果 GameObject 是一个很大的 Sphere（scale: 121.94），它可能覆盖整个屏幕

2. **Scene Color Node 读取问题**：
   - Scene Color Node 使用 Screen Position (Raw) 作为 UV
   - 当 GameObject 在相机前面时，Scene Color Node 可能读取不到正确的屏幕内容

3. **中心点坐标问题**：
   - 当前代码传入的是 `Vector2`，但 shader 可能需要 `Vector4`
   - 中心点坐标可能不正确

## ✅ 已修复

### 1. 修复中心点坐标传递

**修改前**：
```csharp
shockwaveMaterial.SetVector(centerPropID, viewportCenter);  // Vector2
```

**修改后**：
```csharp
Vector4 centerPoint = new Vector4(viewportCenter.x, viewportCenter.y, 0, 0);
shockwaveMaterial.SetVector(centerPropID, centerPoint);  // Vector4
```

### 2. 添加初始化

在 `Start()` 中初始化中心点：
```csharp
shockwaveMaterial.SetVector(centerPropID, new Vector4(0.5f, 0.5f, 0, 0));
```

### 3. 添加空值检查

添加了 Material 的空值检查，避免空引用错误。

## 🎯 如果问题仍然存在

可能需要：

1. **调整 GameObject 位置**：
   - 将 ShockwaveRing 放在相机后面（z < 0）
   - 或者使用 Quad 而不是 Sphere
   - 或者将 GameObject 的 scale 设置得很小

2. **检查 shader 设置**：
   - 确认 ZTest 设置为 Always (8)
   - 确认 Render Queue 设置正确
   - 确认 Scene Color Node 的 UV 输入正确连接

3. **使用固定的屏幕空间坐标**：
   - 如果冲击波应该是全屏效果，应该始终使用屏幕中心 `(0.5, 0.5)`
   - 不要基于 GameObject 位置计算

## 📋 测试步骤

1. 运行游戏
2. 将 ShockwaveRing GameObject 移动到相机镜头内
3. 按 E 键触发冲击波
4. 观察效果是否正常
5. 如果还是不正常，检查 GameObject 的位置和 scale







