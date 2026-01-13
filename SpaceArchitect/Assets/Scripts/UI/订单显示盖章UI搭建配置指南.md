# 订单显示盖章UI搭建配置指南

## 概述

本指南将详细说明如何搭建和配置任务完成后的订单图片显示+印章功能。当玩家按下按键1、2、3完成任务后，会显示订单图片，0.5秒后显示印章，2秒后同时消失。

## 功能效果

- **T=0.0s**: 订单图片淡入显示
- **T=0.5s**: 印章图片缩放+淡入显示（盖在订单图片上方）
- **T=2.5s**: 订单图片和印章同时淡出消失
- **T=2.5s+**: 开始气泡消失动画

## 第一部分：UI结构搭建

### 步骤1：创建Canvas（如果还没有）

1. 在 Hierarchy 中右键点击 → **UI → Canvas**
2. 命名为 `VictoryDisplayCanvas`
3. 在 Inspector 中配置：
   - **Render Mode**: `Screen Space - Overlay`（覆盖模式，显示在最上层）
   - **Canvas Scaler**: 确保已添加，用于适配不同分辨率

### 步骤2：创建显示面板（DisplayPanel）

1. 在 `VictoryDisplayCanvas` 下右键 → **UI → Panel**
2. 命名为 `DisplayPanel`
3. 在 Inspector 中配置 `RectTransform`：
   - **Anchor Presets**: 按住 Alt 键，选择 `Stretch Stretch`（全屏拉伸）
   - **Left**: `0`
   - **Right**: `0`
   - **Top**: `0`
   - **Bottom**: `0`
   - 这样面板会覆盖整个屏幕

4. 配置 `Image` 组件（Panel 自带）：
   - **Color**: `(0, 0, 0, 0)` 或 `(0, 0, 0, 200)`（可选：半透明黑色背景）
   - 如果不需要背景，可以禁用 `Image` 组件

5. **重要**：初始状态设置为 **Inactive**（取消勾选 GameObject 左侧的复选框）
   - 这样面板默认是隐藏的，只有在显示时才激活

### 步骤3：创建订单图片显示组件（ImageDisplay）

1. 在 `DisplayPanel` 下右键 → **UI → Image**
2. 命名为 `ImageDisplay`
3. 在 Inspector 中配置 `RectTransform`：
   - **Anchor Presets**: 按住 Alt 键，选择 `Middle Center`（居中）
   - **Width**: `800`（根据订单图片尺寸调整）
   - **Height**: `600`（根据订单图片尺寸调整）
   - **Pos X**: `0`
   - **Pos Y**: `0`
   - **Pos Z**: `0`

4. 配置 `Image` 组件：
   - **Source Image**: 暂时留空（运行时由脚本设置）
   - **Color**: `(255, 255, 255, 255)`（白色，不透明）
   - **Preserve Aspect**: ✅ **勾选**（保持图片宽高比）

5. **重要**：初始状态设置为 **Inactive**

### 步骤4：创建印章图片显示组件（StampImageDisplay）

1. 在 `DisplayPanel` 下右键 → **UI → Image**
2. 命名为 `StampImageDisplay`
3. **重要**：确保 `StampImageDisplay` 在 Hierarchy 中位于 `ImageDisplay` **之后**（下方）
   - 这样印章会显示在订单图片的上方（Z-order 更高）

4. 在 Inspector 中配置 `RectTransform`：
   - **Anchor Presets**: 按住 Alt 键，选择 `Middle Center`（居中）
   - **Width**: `300`（根据印章图片尺寸调整，建议比订单图片小）
   - **Height**: `300`（根据印章图片尺寸调整）
   - **Pos X**: `0`
   - **Pos Y**: `0`
   - **Pos Z**: `0`
   - 这样印章会居中显示，与订单图片重叠

5. 配置 `Image` 组件：
   - **Source Image**: 暂时留空（运行时由脚本设置）
   - **Color**: `(255, 255, 255, 255)`（白色，不透明）
   - **Preserve Aspect**: ✅ **勾选**（保持图片宽高比）

6. **重要**：初始状态设置为 **Inactive**

### 步骤5：创建 VictoryImageDisplay GameObject

1. 在 Hierarchy 中创建一个空的 GameObject（不在 Canvas 下）
2. 命名为 `VictoryImageDisplay`
3. 添加 `VictoryImageDisplay` 脚本组件

## 第二部分：配置 VictoryImageDisplay 组件

### 步骤1：配置 UI 引用

在 `VictoryImageDisplay` GameObject 的 Inspector 中，找到 `Victory Image Display` 组件：

#### 1.1 UI 引用部分

- **Display Panel**: 
  - 拖拽 `DisplayPanel` GameObject 到该字段

- **Image Display**: 
  - 拖拽 `ImageDisplay` GameObject 的 **Image 组件**到该字段
  - 注意：不是拖拽 GameObject，而是拖拽 Image 组件

- **Stamp Image Display**: 
  - 拖拽 `StampImageDisplay` GameObject 的 **Image 组件**到该字段
  - 注意：不是拖拽 GameObject，而是拖拽 Image 组件

#### 1.2 印章配置部分

- **Stamp Sprite**: 
  - 拖拽印章图片资源（Sprite）到该字段
  - 这是所有任务共用的印章图片，只需配置一次
  - 建议尺寸：300x300 或 400x400 像素
  - 格式：PNG（支持透明背景）

#### 1.3 时间参数部分（可选调整）

- **Stamp Appear Delay**: `0.5`
  - 印章出现延迟时间（秒）
  - 从订单图片显示后开始计算

- **Display Duration**: `2.0`
  - 显示时长（秒）
  - 从印章出现后开始计算
  - 这是图片和印章保持显示的时间

- **Fade In Duration**: `0.3`
  - 订单图片淡入动画时长（秒）

- **Fade Out Duration**: `0.3`
  - 图片和印章淡出动画时长（秒）

- **Stamp Appear Duration**: `0.5`
  - 印章出现动画时长（秒）
  - 包括缩放和淡入效果

#### 1.4 动画缓动类型部分（可选调整）

- **Fade In Ease**: `OutQuad`
  - 图片淡入的缓动效果
  - 推荐值：`OutQuad`（平滑淡入）

- **Fade Out Ease**: `InQuad`
  - 图片和印章淡出的缓动效果
  - 推荐值：`InQuad`（平滑淡出）

- **Stamp Appear Ease**: `OutBack`
  - 印章出现的缓动效果
  - 推荐值：`OutBack`（带弹性效果，更有冲击力）

#### 1.5 调试部分

- **Enable Debug Log**: ✅ **勾选**
  - 启用调试日志，方便排查问题
  - 开发阶段建议开启，发布时可以关闭

## 第三部分：配置 TaskCompletionHandler

### 步骤1：找到 TaskCompletionHandler

在场景中找到包含 `Task Completion Handler` 组件的 GameObject（通常在 MainHub 场景中）。

### 步骤2：更新引用

在 Inspector 中找到 `Task Completion Handler` 组件：

1. **Victory Feedback Display Mono**:
   - 拖拽 `VictoryImageDisplay` GameObject 到该字段
   - 注意：拖拽的是 GameObject，不是组件

2. **其他引用**（确保已配置）:
   - **Task Manager**: 已配置
   - **Icon Manager**: 已配置（SphereIconManager）
   - **Order Data Config**: 已配置（SphereOrderDataConfig）

3. **移除旧引用**（如果存在）:
   - 如果还有 `Video Player` 字段，可以清空或移除

## 第四部分：准备图片资源

### 订单图片（Order Image）

1. 在 `SphereOrderDataConfig` ScriptableObject 中配置
2. 每个订单需要配置一张订单图片
3. 建议尺寸：800x600 或 1024x768 像素
4. 格式：PNG 或 JPG
5. 在 Unity 中导入后，确保 Texture Type 设置为 `Sprite (2D and UI)`

### 印章图片（Stamp Sprite）

1. 准备一张印章图片（所有任务共用）
2. 建议尺寸：300x300 或 400x400 像素
3. 格式：PNG（支持透明背景）
4. 在 Unity 中导入后，确保 Texture Type 设置为 `Sprite (2D and UI)`
5. 在 `VictoryImageDisplay` 组件的 `Stamp Sprite` 字段中配置

## 第五部分：完整 Hierarchy 结构示例

```
Canvas (VictoryDisplayCanvas)
└── DisplayPanel (初始状态: Inactive)
    ├── ImageDisplay (初始状态: Inactive)
    │   └── Image 组件
    └── StampImageDisplay (初始状态: Inactive)
        └── Image 组件

VictoryImageDisplay (GameObject)
└── VictoryImageDisplay 脚本组件
```

## 第六部分：配置检查清单

在开始测试前，请确认以下所有项都已正确配置：

### UI 结构检查
- [ ] Canvas 已创建并配置为 Screen Space - Overlay
- [ ] DisplayPanel 已创建，初始状态为 Inactive
- [ ] ImageDisplay 已创建，初始状态为 Inactive
- [ ] StampImageDisplay 已创建，初始状态为 Inactive
- [ ] StampImageDisplay 在 Hierarchy 中位于 ImageDisplay 之后（确保显示在上方）

### VictoryImageDisplay 组件检查
- [ ] Display Panel 引用已配置（拖拽 DisplayPanel GameObject）
- [ ] Image Display 引用已配置（拖拽 ImageDisplay 的 Image 组件）
- [ ] Stamp Image Display 引用已配置（拖拽 StampImageDisplay 的 Image 组件）
- [ ] Stamp Sprite 已配置（拖拽印章图片资源）

### TaskCompletionHandler 组件检查
- [ ] Victory Feedback Display Mono 已配置（拖拽 VictoryImageDisplay GameObject）
- [ ] Task Manager 已配置
- [ ] Icon Manager 已配置
- [ ] Order Data Config 已配置

### 资源检查
- [ ] 订单图片已准备并在 SphereOrderDataConfig 中配置
- [ ] 印章图片已准备并在 VictoryImageDisplay 中配置
- [ ] 所有图片的 Texture Type 都设置为 Sprite (2D and UI)

## 第七部分：测试步骤

### 1. 运行游戏

点击 Unity 编辑器顶部的 **Play** 按钮。

### 2. 触发任务完成

在 MainHub 场景中，按下按键 **1**、**2** 或 **3**（根据任务配置）。

### 3. 观察效果

应该看到以下效果：

1. **图片显示**（T=0.0s）:
   - 订单图片从透明淡入显示
   - 图片居中显示在屏幕中央

2. **印章显示**（T=0.5s）:
   - 印章图片从中心缩放+淡入显示
   - 印章显示在订单图片的上方（重叠）
   - 有弹性效果（OutBack 缓动）

3. **保持显示**（T=0.5s ~ T=2.5s）:
   - 图片和印章保持显示 2 秒

4. **同时消失**（T=2.5s）:
   - 图片和印章同时淡出消失
   - 动画时长 0.3 秒

5. **气泡消失**（T=2.5s+）:
   - 气泡开始消失动画（保持和之前不变）

### 4. 检查 Console

打开 Unity Console 窗口，查看是否有错误或警告信息。

## 第八部分：常见问题排查

### Q1: 图片不显示

**可能原因：**
- DisplayPanel 未激活
- ImageDisplay 的 Image 组件未配置
- VictoryImageDisplay 组件的引用未正确配置

**解决方法：**
1. 检查 DisplayPanel 的 activeSelf 状态（应该由脚本控制，初始为 Inactive）
2. 检查 ImageDisplay 的 Image 组件是否存在
3. 检查 VictoryImageDisplay 组件的 UI 引用是否正确
4. 查看 Console 中的错误信息

### Q2: 印章不显示

**可能原因：**
- StampSprite 未配置
- StampImageDisplay 的 Image 组件未配置
- StampImageDisplay 在 Hierarchy 中的顺序不正确

**解决方法：**
1. 检查 Stamp Sprite 是否已配置
2. 检查 StampImageDisplay 的 Image 组件是否存在
3. 确保 StampImageDisplay 在 Hierarchy 中位于 ImageDisplay 之后
4. 检查 Console 中的警告信息

### Q3: 印章显示在图片下方

**解决方法：**
1. 调整 Hierarchy 中的顺序，确保 StampImageDisplay 在 ImageDisplay 之后
2. 或者调整 Canvas 的 Sorting Order（如果使用多个 Canvas）

### Q4: 图片尺寸不合适

**解决方法：**
1. 调整 ImageDisplay 的 RectTransform 的 Width 和 Height
2. 确保 Image 组件的 Preserve Aspect 已勾选
3. 根据实际订单图片尺寸调整

### Q5: 动画时间不对

**解决方法：**
1. 在 VictoryImageDisplay 组件的 Inspector 中调整时间参数
2. 主要参数：
   - `Stamp Appear Delay`: 控制印章出现延迟
   - `Display Duration`: 控制显示时长
   - `Fade In/Out Duration`: 控制淡入淡出速度

### Q6: 图片位置不对

**解决方法：**
1. 调整 ImageDisplay 和 StampImageDisplay 的 RectTransform
2. 使用 Anchor Presets 设置锚点
3. 调整 Pos X、Pos Y 值

### Q7: 背景遮挡其他UI

**解决方法：**
1. 如果不需要背景，禁用 DisplayPanel 的 Image 组件
2. 或者将 Image 组件的 Color Alpha 设置为 0
3. 或者调整 Canvas 的 Sorting Order

## 第九部分：高级配置

### 自定义动画效果

如果需要自定义动画效果，可以在 VictoryImageDisplay 组件中调整：

1. **缓动类型**:
   - `Fade In Ease`: 图片淡入效果
   - `Fade Out Ease`: 淡出效果
   - `Stamp Appear Ease`: 印章出现效果
   - 可选值：`Linear`, `InQuad`, `OutQuad`, `InOutQuad`, `OutBack`, `InBack` 等

2. **时间参数**:
   - 所有时间参数都可以在 Inspector 中实时调整
   - 修改后需要重新运行游戏才能生效

### 多场景支持

如果需要在多个场景中使用：

1. 将 `VictoryImageDisplay` GameObject 设置为 **Prefab**
2. 在每个需要的场景中实例化该 Prefab
3. 或者使用 DontDestroyOnLoad（如果需要在场景切换时保持）

### 性能优化

1. **对象池**:
   - 如果频繁显示/隐藏，可以考虑使用对象池
   - 当前实现每次都是激活/禁用，性能已经很好

2. **图片压缩**:
   - 确保图片资源已压缩
   - 使用合适的图片格式（PNG 用于透明，JPG 用于不透明）

## 第十部分：时间线详解

### 完整时间线

```
T=0.0s:  开始显示订单图片（淡入动画开始）
T=0.3s:  订单图片淡入完成（Fade In Duration）
T=0.5s:  开始显示印章（缩放+淡入动画开始，Stamp Appear Delay）
T=1.0s:  印章显示完成（Stamp Appear Duration）
T=3.0s:  开始淡出（Display Duration 结束，Fade Out Duration 开始）
T=3.3s:  图片和印章完全消失（Fade Out Duration 结束）
T=3.3s+: 开始气泡消失动画
```

### 总时长计算

**总时长** = Fade In Duration + Stamp Appear Delay + Stamp Appear Duration + Display Duration + Fade Out Duration

**默认总时长** = 0.3 + 0.5 + 0.5 + 2.0 + 0.3 = **3.6秒**

如果需要调整总时长，主要修改 **Display Duration** 参数。

## 总结

按照本指南完成配置后，任务完成时会自动显示订单图片和印章。所有配置都在 Unity Inspector 中完成，无需修改代码。

如果遇到问题，请：
1. 检查配置检查清单中的所有项
2. 查看 Console 中的错误信息
3. 参考常见问题排查部分
4. 确保 Enable Debug Log 已开启，查看详细日志
