# 修复 Game 视图看不到星星的问题

## 🔍 问题分析

在 Scene 视图中可以看到星星，但在 Game 相机视角看不到，这通常是以下原因之一：

1. **正交相机的视野范围太小**
   - 当前相机的 `Orthographic Size` 是 `5`
   - 这意味着相机只能看到宽度为 10 单位（-5 到 +5）的范围
   - StarSpawner 在 `x: -35.9, z: 16.3`，星星生成范围是 `200x100`
   - 星星可能超出了相机的视野范围

2. **相机位置和星星位置不匹配**
   - 相机位置：`x: 0, y: 1, z: -10`
   - StarSpawner 位置：`x: -35.9, y: 2.87, z: 16.3`
   - 星星生成区域中心在 StarSpawner 位置，范围是 200x100

3. **相机的 Far Clipping Plane**
   - 当前是 1000，应该足够

## ✅ 解决方案

### 方法 1：增大相机的 Orthographic Size（推荐）

1. **在 Unity 编辑器中**：
   - 选中 `Main Camera` GameObject
   - 在 Inspector 中找到 `Camera` 组件
   - 将 `Size`（Orthographic Size）从 `5` 增大到 `50` 或更大
   - 这样相机可以看到更大的范围

### 方法 2：调整相机位置

1. **将相机移动到能看到星星的位置**：
   - 相机位置：`x: -35.9, y: 2.87, z: -10`（与 StarSpawner 的 x, y 对齐）

### 方法 3：调整 StarSpawner 位置

1. **将 StarSpawner 移动到相机视野范围内**：
   - 将 StarSpawner 的 x 坐标改为接近 0（相机 x 位置）
   - 将 StarSpawner 的 z 坐标改为接近 -10（相机 z 位置附近）

## 🎯 推荐的相机设置

根据星星生成范围（200x100），建议：

- **Orthographic Size**: `50` - `100`（取决于你想看到多少星星）
- **Far Clipping Plane**: `1000`（当前值即可）
- **相机位置**: 可以保持当前值，但需要增大 Size

## 📝 快速修复步骤

1. 在 Unity 编辑器中选中 `Main Camera`
2. 在 Inspector 的 `Camera` 组件中
3. 将 `Size` 从 `5` 改为 `50` 或更大
4. 运行游戏测试

## ⚠️ 注意事项

- 增大 Orthographic Size 会让相机看到更大的范围，但可能会让其他物体看起来更小
- 如果游戏中有其他相机（如 GlobalOverviewCamera），也需要检查它们的设置
- 星星的生成是在运行时，确保 StarSpawner GameObject 是激活的






